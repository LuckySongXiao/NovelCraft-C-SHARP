using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NovelManagement.Infrastructure.Data;

namespace NovelManagement.WPF.Services;

/// <summary>
/// 提供生产运行环境的健康检查能力。
/// </summary>
public class ProductionHealthCheckService
{
        private static string T(string key, string fallbackZh) => Localization.LocalizationManager.T(key, fallbackZh);
        private static string TF(string key, string fallbackZh, params object[] args) => Localization.LocalizationManager.TF(key, fallbackZh, args);
        private static string LT(string key, string fallbackZh) => Localization.LocalizationManager.T(key, fallbackZh);

    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly DatabaseMaintenanceService _databaseMaintenanceService;
    private readonly StartupConfigurationValidationService _startupConfigurationValidationService;
    private readonly AIConnectivityCheckService _aiConnectivityCheckService;
    private readonly ILogger<ProductionHealthCheckService> _logger;

    public ProductionHealthCheckService(
        IConfiguration configuration,
        IServiceScopeFactory serviceScopeFactory,
        DatabaseMaintenanceService databaseMaintenanceService,
        StartupConfigurationValidationService startupConfigurationValidationService,
        AIConnectivityCheckService aiConnectivityCheckService,
        ILogger<ProductionHealthCheckService> logger)
    {
        _configuration = configuration;
        _serviceScopeFactory = serviceScopeFactory;
        _databaseMaintenanceService = databaseMaintenanceService;
        _startupConfigurationValidationService = startupConfigurationValidationService;
        _aiConnectivityCheckService = aiConnectivityCheckService;
        _logger = logger;
    }

    /// <summary>
    /// 执行完整健康检查。
    /// </summary>
    public async Task<ProductionHealthReport> RunAsync()
    {
        var items = new List<ProductionHealthItem>();
        var appDataRoot = _configuration["Paths:AppDataRoot"] ?? string.Empty;
        var configDirectory = _configuration["Paths:ConfigDirectory"] ?? string.Empty;
        var dataDirectory = _configuration["Paths:DataDirectory"] ?? string.Empty;
        var logsDirectory = _configuration["Paths:LogsDirectory"] ?? string.Empty;
        var backupsDirectory = _configuration["Paths:BackupsDirectory"] ?? string.Empty;
        var userConfigPath = _configuration["Paths:UserConfigurationPath"] ?? string.Empty;
        var databasePath = _databaseMaintenanceService.GetDatabaseFilePath();

        items.Add(CheckDirectory(LT("HC.Rpt.AppDataDir", "应用数据目录"), appDataRoot));
        items.Add(CheckDirectory(LT("HC.Rpt.ConfigDir", "配置目录"), configDirectory));
        items.Add(CheckDirectory(LT("HC.Rpt.DataDir", "数据目录"), dataDirectory));
        items.Add(CheckDirectory(LT("HC.Rpt.LogsDir", "日志目录"), logsDirectory));
        items.Add(CheckDirectory(LT("HC.Rpt.BackupsDir", "备份目录"), backupsDirectory));

        items.Add(File.Exists(databasePath)
            ? new ProductionHealthItem(LT("HC.Rpt.DbFile", "数据库文件"), HealthStatus.Healthy, TF("HC.Rpt.DbFileFound", "已找到数据库文件: {0}", databasePath))
            : new ProductionHealthItem(LT("HC.Rpt.DbFile", "数据库文件"), HealthStatus.Warning, TF("HC.Rpt.DbFileMissing", "未找到数据库文件: {0}", databasePath)));

        items.Add(File.Exists(userConfigPath)
            ? new ProductionHealthItem(LT("HC.Rpt.UserConfig", "用户覆盖配置"), HealthStatus.Healthy, TF("HC.Rpt.UserConfigFound", "已找到用户配置: {0}", userConfigPath))
            : new ProductionHealthItem(LT("HC.Rpt.UserConfig", "用户覆盖配置"), HealthStatus.Warning, TF("HC.Rpt.UserConfigMissing", "未找到用户配置: {0}", userConfigPath)));

        var backupFiles = _databaseMaintenanceService.ListBackups();
        items.Add(backupFiles.Count > 0
            ? new ProductionHealthItem(LT("HC.Rpt.DbBackup", "数据库备份"), HealthStatus.Healthy, TF("HC.Rpt.BackupsAvailable", "当前可用备份数量: {0}", backupFiles.Count))
            : new ProductionHealthItem(LT("HC.Rpt.DbBackup", "数据库备份"), HealthStatus.Warning, T("HC.Rpt.NoBackupYet", "当前尚无数据库备份文件")));

        items.Add(new ProductionHealthItem(
            LT("HC.Rpt.RuntimeEnv", "运行环境"),
            _configuration.GetValue("Application:Environment", "Production").Equals("Production", StringComparison.OrdinalIgnoreCase)
                ? HealthStatus.Healthy
                : HealthStatus.Warning,
            TF("HC.Rpt.CurrentEnv", "当前环境: {0}", _configuration.GetValue("Application:Environment", "Production"))));

        foreach (var validationItem in _startupConfigurationValidationService.Validate())
        {
            var (itemName, itemMessage) = LocalizeValidationItem(validationItem.Name, validationItem.Message);
            items.Add(new ProductionHealthItem(
                TF("HC.Rpt.ConfigValidationFmt", "配置校验 / {0}", itemName),
                validationItem.Severity switch
                {
                    ValidationSeverity.Error => HealthStatus.Unhealthy,
                    ValidationSeverity.Warning => HealthStatus.Warning,
                    _ => HealthStatus.Healthy
                },
                itemMessage));
        }

        await AppendDatabaseChecksAsync(items);
        await AppendAiChecksAsync(items);

        var overallStatus = items.Any(item => item.Status == HealthStatus.Unhealthy)
            ? HealthStatus.Unhealthy
            : items.Any(item => item.Status == HealthStatus.Warning)
                ? HealthStatus.Warning
                : HealthStatus.Healthy;

        return new ProductionHealthReport(
            overallStatus,
            DateTime.Now,
            items,
            appDataRoot,
            logsDirectory,
            backupsDirectory,
            databasePath);
    }

    /// <summary>
    /// 把启动配置校验 / AI 连通性检测条目的固定中文名称与消息映射为当前语言显示文本（未知文本原样返回）。
    /// </summary>
    private static (string Name, string Message) LocalizeValidationItem(string rawName, string rawMessage)
    {
        const string DirExistsPrefix = "目录存在: ";
        const string DirMissingPrefix = "目录不存在，将在运行时尝试创建: ";
        const string UserCfgFoundPrefix = "已发现用户覆盖配置: ";
        const string UserCfgMissingPrefix = "未发现用户覆盖配置: ";
        const string EnvPrefix = "当前环境为 ";
        const string EnvNonProdSuffix = "，生产部署建议使用 Production";
        const string RetentionPrefix = "备份保留天数: ";
        const string CfgIncompletePrefix = "配置不完整: ";
        const string ConnOkMsPrefix = "连接成功，响应时间 ";

        var name = rawName switch
        {
            "应用数据根目录" => T("HC.Val.AppDataRoot", rawName),
            "配置目录" => T("HC.Val.ConfigDir", rawName),
            "数据目录" => T("HC.Val.DataDir", rawName),
            "日志目录" => T("HC.Val.LogsDir", rawName),
            "备份目录" => T("HC.Val.BackupsDir", rawName),
            "用户覆盖配置文件" => T("HC.Val.UserConfigFile", rawName),
            "应用环境" => T("HC.Val.AppEnv", rawName),
            "备份保留策略" => T("HC.Val.BackupRetention", rawName),
            "AI 功能开关" => T("HC.Val.AiToggle", rawName),
            "AI 总开关" => T("HC.Val.AiToggle", rawName),
            "Ollama 配置" => T("HC.Val.OllamaCfg", rawName),
            "RWKV 配置" => T("HC.Val.RwkvCfg", rawName),
            "DeepSeek 配置" => T("HC.Val.DeepSeekCfg", rawName),
            "智谱 AI 配置" => T("HC.Val.ZhipuCfg", rawName),
            "智谱AI" => T("HC.Val.ZhipuName", rawName),
            "OpenAI 兼容配置" => T("HC.Val.OpenAiCfg", rawName),
            _ => rawName
        };

        var message = rawMessage switch
        {
            var m when m.StartsWith(DirExistsPrefix) => TF("HC.Rpt.DirExists", "目录存在: {0}", m[DirExistsPrefix.Length..]),
            var m when m.StartsWith(DirMissingPrefix) => TF("HC.Val.MsgDirMissing", "目录不存在，将在运行时尝试创建: {0}", m[DirMissingPrefix.Length..]),
            "路径配置缺失" => T("HC.Rpt.PathMissing", rawMessage),
            var m when m.StartsWith(UserCfgFoundPrefix) => TF("HC.Val.MsgUserConfigFound", "已发现用户覆盖配置: {0}", m[UserCfgFoundPrefix.Length..]),
            var m when m.StartsWith(UserCfgMissingPrefix) => TF("HC.Val.MsgUserConfigMissing", "未发现用户覆盖配置: {0}", m[UserCfgMissingPrefix.Length..]),
            var m when m.StartsWith(EnvPrefix) && m.EndsWith(EnvNonProdSuffix) => TF("HC.Val.MsgEnvNonProd", "当前环境为 {0}，生产部署建议使用 Production", m[EnvPrefix.Length..^EnvNonProdSuffix.Length]),
            var m when m.StartsWith(EnvPrefix) => TF("HC.Val.MsgEnvProd", "当前环境为 {0}", m[EnvPrefix.Length..]),
            var m when m.StartsWith(RetentionPrefix) => TF("HC.Val.MsgRetentionOk", "备份保留天数: {0}", m[RetentionPrefix.Length..]),
            "备份保留天数必须大于 0" => T("HC.Val.MsgRetentionInvalid", rawMessage),
            "AI 功能当前已关闭" => T("HC.Val.MsgAiDisabled", rawMessage),
            "AI 功能已启用" => T("HC.Val.MsgAiEnabled", rawMessage),
            "AI 功能已关闭，跳过连通性检测" => T("HC.Val.MsgAiSkipped", rawMessage),
            "配置合法" => T("HC.Val.MsgConfigValid", rawMessage),
            var m when m.StartsWith(ConnOkMsPrefix) && m.EndsWith(" ms") => TF("HC.Val.MsgConnOkMs", "连接成功，响应时间 {0} ms", m[ConnOkMsPrefix.Length..^" ms".Length]),
            "连接成功" => T("HC.Val.MsgConnOk", rawMessage),
            "连接失败" => T("HC.Val.MsgConnFail", rawMessage),
            "连接失败或服务未启动" => T("HC.Val.MsgConnFailOrNotRunning", rawMessage),
            var m when m.StartsWith(CfgIncompletePrefix) => TF("HC.Val.MsgCfgIncomplete", "配置不完整: {0}", m[CfgIncompletePrefix.Length..]),
            _ => rawMessage
        };

        return (name, message);
    }

    private ProductionHealthItem CheckDirectory(string name, string path)
    {        if (string.IsNullOrWhiteSpace(path))
        {
            return new ProductionHealthItem(name, HealthStatus.Unhealthy, T("HC.Rpt.PathMissing", "路径配置缺失"));
        }

        return Directory.Exists(path)
            ? new ProductionHealthItem(name, HealthStatus.Healthy, TF("HC.Rpt.DirExists", "目录存在: {0}", path))
            : new ProductionHealthItem(name, HealthStatus.Unhealthy, TF("HC.Rpt.DirMissing", "目录不存在: {0}", path));
    }

    private async Task AppendDatabaseChecksAsync(List<ProductionHealthItem> items)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NovelManagementDbContext>();
            var canConnect = await dbContext.Database.CanConnectAsync();
            items.Add(canConnect
                ? new ProductionHealthItem(T("HC.Rpt.DbConnection", "数据库连接"), HealthStatus.Healthy, T("HC.Rpt.DbOk", "数据库连接正常"))
                : new ProductionHealthItem(T("HC.Rpt.DbConnection", "数据库连接"), HealthStatus.Unhealthy, T("HC.Rpt.DbFail", "数据库连接失败")));

            var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();
            items.Add(pendingMigrations.Count == 0
                ? new ProductionHealthItem(T("HC.Rpt.DbMigrations", "数据库迁移"), HealthStatus.Healthy, T("HC.Rpt.NoMigrations", "没有待处理迁移"))
                : new ProductionHealthItem(T("HC.Rpt.DbMigrations", "数据库迁移"), HealthStatus.Warning, TF("HC.Rpt.PendingMigrations", "存在 {0} 个待处理迁移", pendingMigrations.Count)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行数据库健康检查失败");
            items.Add(new ProductionHealthItem(T("HC.Rpt.DbHealthCheck", "数据库健康检查"), HealthStatus.Unhealthy, TF("HC.Rpt.CheckFailed", "检查失败: {0}", ex.Message)));
        }
    }

    private async Task AppendAiChecksAsync(List<ProductionHealthItem> items)
    {
        try
        {
            foreach (var aiItem in await _aiConnectivityCheckService.CheckAllAsync())
            {
                items.Add(new ProductionHealthItem(TF("HC.Rpt.ConfigValidationFmt", "配置校验 / {0}", aiItem.Name), aiItem.Status, aiItem.Message));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行 AI 连通性检测失败");
            items.Add(new ProductionHealthItem(T("HC.Rpt.AiConnectivity", "AI 连通性检测"), HealthStatus.Warning, TF("HC.Rpt.CheckFailed", "检查失败: {0}", ex.Message)));
        }
    }
}

/// <summary>
/// 生产健康报告。
/// </summary>
public sealed record ProductionHealthReport(
    HealthStatus Status,
    DateTime GeneratedAt,
    IReadOnlyList<ProductionHealthItem> Items,
    string AppDataRoot,
    string LogsDirectory,
    string BackupsDirectory,
    string DatabasePath);

/// <summary>
/// 单项健康检查结果。
/// </summary>
public sealed record ProductionHealthItem(string Name, HealthStatus Status, string Message);

/// <summary>
/// 健康状态。
/// </summary>
public enum HealthStatus
{
    Healthy,
    Warning,
    Unhealthy
}
