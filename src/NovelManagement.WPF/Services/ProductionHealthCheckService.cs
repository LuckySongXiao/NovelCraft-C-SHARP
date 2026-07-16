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

        items.Add(CheckDirectory("应用数据目录", appDataRoot));
        items.Add(CheckDirectory("配置目录", configDirectory));
        items.Add(CheckDirectory("数据目录", dataDirectory));
        items.Add(CheckDirectory("日志目录", logsDirectory));
        items.Add(CheckDirectory("备份目录", backupsDirectory));

        items.Add(File.Exists(databasePath)
            ? new ProductionHealthItem("数据库文件", HealthStatus.Healthy, $"已找到数据库文件: {databasePath}")
            : new ProductionHealthItem("数据库文件", HealthStatus.Warning, $"未找到数据库文件: {databasePath}"));

        items.Add(File.Exists(userConfigPath)
            ? new ProductionHealthItem("用户覆盖配置", HealthStatus.Healthy, $"已找到用户配置: {userConfigPath}")
            : new ProductionHealthItem("用户覆盖配置", HealthStatus.Warning, $"未找到用户配置: {userConfigPath}"));

        var backupFiles = _databaseMaintenanceService.ListBackups();
        items.Add(backupFiles.Count > 0
            ? new ProductionHealthItem("数据库备份", HealthStatus.Healthy, $"当前可用备份数量: {backupFiles.Count}")
            : new ProductionHealthItem("数据库备份", HealthStatus.Warning, "当前尚无数据库备份文件"));

        items.Add(new ProductionHealthItem(
            "运行环境",
            _configuration.GetValue("Application:Environment", "Production").Equals("Production", StringComparison.OrdinalIgnoreCase)
                ? HealthStatus.Healthy
                : HealthStatus.Warning,
            $"当前环境: {_configuration.GetValue("Application:Environment", "Production")}"));

        foreach (var validationItem in _startupConfigurationValidationService.Validate())
        {
            items.Add(new ProductionHealthItem(
                $"配置校验 / {validationItem.Name}",
                validationItem.Severity switch
                {
                    ValidationSeverity.Error => HealthStatus.Unhealthy,
                    ValidationSeverity.Warning => HealthStatus.Warning,
                    _ => HealthStatus.Healthy
                },
                validationItem.Message));
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

    private ProductionHealthItem CheckDirectory(string name, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return new ProductionHealthItem(name, HealthStatus.Unhealthy, "路径配置缺失");
        }

        return Directory.Exists(path)
            ? new ProductionHealthItem(name, HealthStatus.Healthy, $"目录存在: {path}")
            : new ProductionHealthItem(name, HealthStatus.Unhealthy, $"目录不存在: {path}");
    }

    private async Task AppendDatabaseChecksAsync(List<ProductionHealthItem> items)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NovelManagementDbContext>();
            var canConnect = await dbContext.Database.CanConnectAsync();
            items.Add(canConnect
                ? new ProductionHealthItem("数据库连接", HealthStatus.Healthy, "数据库连接正常")
                : new ProductionHealthItem("数据库连接", HealthStatus.Unhealthy, "数据库连接失败"));

            var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();
            items.Add(pendingMigrations.Count == 0
                ? new ProductionHealthItem("数据库迁移", HealthStatus.Healthy, "没有待处理迁移")
                : new ProductionHealthItem("数据库迁移", HealthStatus.Warning, $"存在 {pendingMigrations.Count} 个待处理迁移"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行数据库健康检查失败");
            items.Add(new ProductionHealthItem("数据库健康检查", HealthStatus.Unhealthy, $"检查失败: {ex.Message}"));
        }
    }

    private async Task AppendAiChecksAsync(List<ProductionHealthItem> items)
    {
        try
        {
            foreach (var aiItem in await _aiConnectivityCheckService.CheckAllAsync())
            {
                items.Add(new ProductionHealthItem($"AI 连通性 / {aiItem.Name}", aiItem.Status, aiItem.Message));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行 AI 连通性检测失败");
            items.Add(new ProductionHealthItem("AI 连通性检测", HealthStatus.Warning, $"检测失败: {ex.Message}"));
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
