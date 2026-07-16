using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NovelManagement.WPF.Services;

/// <summary>
/// 导出用于排障的诊断包。
/// </summary>
public class DiagnosticBundleService
{
    private readonly IConfiguration _configuration;
    private readonly ProductionHealthCheckService _productionHealthCheckService;
    private readonly StartupConfigurationValidationService _startupConfigurationValidationService;
    private readonly AIConnectivityCheckService _aiConnectivityCheckService;
    private readonly ConfigurationService _configurationService;
    private readonly DatabaseMaintenanceService _databaseMaintenanceService;
    private readonly ILogger<DiagnosticBundleService> _logger;

    public DiagnosticBundleService(
        IConfiguration configuration,
        ProductionHealthCheckService productionHealthCheckService,
        StartupConfigurationValidationService startupConfigurationValidationService,
        AIConnectivityCheckService aiConnectivityCheckService,
        ConfigurationService configurationService,
        DatabaseMaintenanceService databaseMaintenanceService,
        ILogger<DiagnosticBundleService> logger)
    {
        _configuration = configuration;
        _productionHealthCheckService = productionHealthCheckService;
        _startupConfigurationValidationService = startupConfigurationValidationService;
        _aiConnectivityCheckService = aiConnectivityCheckService;
        _configurationService = configurationService;
        _databaseMaintenanceService = databaseMaintenanceService;
        _logger = logger;
    }

    /// <summary>
    /// 导出诊断包到指定 zip 文件。
    /// </summary>
    public async Task<DiagnosticBundleResult> ExportAsync(string zipFilePath)
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"NovelManagement_Diagnostic_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var report = await _productionHealthCheckService.RunAsync();
            var validationItems = _startupConfigurationValidationService.Validate();
            var aiItems = await _aiConnectivityCheckService.CheckAllAsync();
            var runtimeState = await _configurationService.LoadAppStateAsync();

            await File.WriteAllTextAsync(
                Path.Combine(tempDirectory, "health-report.txt"),
                BuildHealthReportText(report),
                Encoding.UTF8);

            await File.WriteAllTextAsync(
                Path.Combine(tempDirectory, "validation-report.txt"),
                BuildValidationReportText(validationItems),
                Encoding.UTF8);

            await File.WriteAllTextAsync(
                Path.Combine(tempDirectory, "ai-connectivity-report.txt"),
                BuildAiReportText(aiItems),
                Encoding.UTF8);

            await File.WriteAllTextAsync(
                Path.Combine(tempDirectory, "environment-summary.txt"),
                BuildEnvironmentSummaryText(report),
                Encoding.UTF8);

            await File.WriteAllTextAsync(
                Path.Combine(tempDirectory, "app-state.json"),
                JsonSerializer.Serialize(runtimeState, new JsonSerializerOptions { WriteIndented = true }),
                Encoding.UTF8);

            await File.WriteAllTextAsync(
                Path.Combine(tempDirectory, "sanitized-config.json"),
                JsonSerializer.Serialize(BuildSanitizedConfigurationSummary(), new JsonSerializerOptions { WriteIndented = true }),
                Encoding.UTF8);

            CopyRecentLogs(tempDirectory);
            CopyRecentBackupsList(tempDirectory);

            Directory.CreateDirectory(Path.GetDirectoryName(zipFilePath)!);
            if (File.Exists(zipFilePath))
            {
                File.Delete(zipFilePath);
            }

            ZipFile.CreateFromDirectory(tempDirectory, zipFilePath, CompressionLevel.Optimal, includeBaseDirectory: false);

            runtimeState.LastDiagnosticBundlePath = zipFilePath;
            runtimeState.LastDiagnosticBundleAt = DateTime.Now;
            await _configurationService.SaveAppStateAsync(runtimeState);

            _logger.LogInformation("诊断包导出完成: {ZipFilePath}", zipFilePath);
            return new DiagnosticBundleResult(true, zipFilePath, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出诊断包失败");
            return new DiagnosticBundleResult(false, null, ex.Message);
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, recursive: true);
                }
            }
            catch
            {
                // 临时目录清理失败不影响主流程
            }
        }
    }

    private object BuildSanitizedConfigurationSummary()
    {
        return new
        {
            Application = new
            {
                Environment = _configuration["Application:Environment"],
                Version = _configuration["Application:Version"]
            },
            Features = new
            {
                EnableAI = _configuration.GetValue("Features:EnableAI", true),
                EnableAutoSave = _configuration.GetValue("Features:EnableAutoSave", true)
            },
            Backup = new
            {
                RetentionDays = _configuration.GetValue("Backup:RetentionDays", 14),
                EnableCleanupOnStartup = _configuration.GetValue("Backup:EnableCleanupOnStartup", true)
            },
            AI = new
            {
                DefaultProvider = _configuration["AI:DefaultProvider"],
                Providers = new
                {
                    Ollama = new
                    {
                        BaseUrl = _configuration["AI:Providers:Ollama:BaseUrl"],
                        DefaultModel = _configuration["AI:Providers:Ollama:DefaultModel"]
                    },
                    RWKV = new
                    {
                        BaseUrl = _configuration["AI:Providers:RWKV:BaseUrl"],
                        ModelName = _configuration["AI:Providers:RWKV:ModelName"]
                    },
                    DeepSeek = new
                    {
                        BaseUrl = _configuration["AI:Providers:DeepSeek:BaseUrl"],
                        DefaultModel = _configuration["AI:Providers:DeepSeek:Model"],
                        HasApiKey = !string.IsNullOrWhiteSpace(_configuration["AI:Providers:DeepSeek:ApiKey"])
                    },
                    ZhipuAI = new
                    {
                        BaseUrl = _configuration["AI:Providers:ZhipuAI:BaseUrl"],
                        DefaultModel = _configuration["AI:Providers:ZhipuAI:DefaultModel"],
                        HasApiKey = !string.IsNullOrWhiteSpace(_configuration["AI:Providers:ZhipuAI:ApiKey"])
                    },
                    OpenAI = new
                    {
                        BaseUrl = _configuration["AI:Providers:OpenAI:BaseUrl"],
                        DefaultModel = _configuration["AI:Providers:OpenAI:DefaultModel"],
                        HasApiKey = !string.IsNullOrWhiteSpace(_configuration["AI:Providers:OpenAI:ApiKey"])
                    }
                }
            }
        };
    }

    private void CopyRecentLogs(string tempDirectory)
    {
        var logsDirectory = _configuration["Paths:LogsDirectory"];
        if (string.IsNullOrWhiteSpace(logsDirectory) || !Directory.Exists(logsDirectory))
        {
            return;
        }

        var targetDirectory = Path.Combine(tempDirectory, "logs");
        Directory.CreateDirectory(targetDirectory);

        foreach (var logFile in Directory.EnumerateFiles(logsDirectory, "*.txt")
                     .OrderByDescending(File.GetLastWriteTime)
                     .Take(5))
        {
            File.Copy(logFile, Path.Combine(targetDirectory, Path.GetFileName(logFile)), overwrite: true);
        }
    }

    private void CopyRecentBackupsList(string tempDirectory)
    {
        var backups = _databaseMaintenanceService.ListBackups()
            .Take(20)
            .Select(path => new
            {
                FileName = Path.GetFileName(path),
                FullPath = path,
                LastWriteTime = File.GetLastWriteTime(path),
                SizeBytes = new FileInfo(path).Length
            })
            .ToList();

        File.WriteAllText(
            Path.Combine(tempDirectory, "backups.json"),
            JsonSerializer.Serialize(backups, new JsonSerializerOptions { WriteIndented = true }),
            Encoding.UTF8);
    }

    private static string BuildHealthReportText(ProductionHealthReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("发布与运维健康检查报告");
        builder.AppendLine(new string('=', 72));
        builder.AppendLine($"生成时间: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine($"总体状态: {report.Status}");
        builder.AppendLine($"应用数据根目录: {report.AppDataRoot}");
        builder.AppendLine($"数据库文件: {report.DatabasePath}");
        builder.AppendLine($"日志目录: {report.LogsDirectory}");
        builder.AppendLine($"备份目录: {report.BackupsDirectory}");
        builder.AppendLine();

        foreach (var item in report.Items)
        {
            builder.AppendLine($"[{item.Status}] {item.Name}");
            builder.AppendLine(item.Message);
        }

        return builder.ToString();
    }

    private static string BuildValidationReportText(System.Collections.Generic.IReadOnlyList<ConfigurationValidationItem> items)
    {
        var builder = new StringBuilder();
        builder.AppendLine("启动配置校验报告");
        builder.AppendLine(new string('=', 72));
        foreach (var item in items)
        {
            builder.AppendLine($"[{item.Severity}] {item.Name}");
            builder.AppendLine(item.Message);
        }
        return builder.ToString();
    }

    private static string BuildAiReportText(System.Collections.Generic.IReadOnlyList<AIConnectivityItem> items)
    {
        var builder = new StringBuilder();
        builder.AppendLine("AI 连通性报告");
        builder.AppendLine(new string('=', 72));
        foreach (var item in items)
        {
            builder.AppendLine($"[{item.Status}] {item.Name}");
            builder.AppendLine(item.Message);
        }
        return builder.ToString();
    }

    private static string BuildEnvironmentSummaryText(ProductionHealthReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"操作系统: {Environment.OSVersion}");
        builder.AppendLine($".NET 版本: {Environment.Version}");
        builder.AppendLine($"机器名: {Environment.MachineName}");
        builder.AppendLine($"当前时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine($"应用数据根目录: {report.AppDataRoot}");
        builder.AppendLine($"数据库路径: {report.DatabasePath}");
        return builder.ToString();
    }
}

/// <summary>
/// 诊断包导出结果。
/// </summary>
public sealed record DiagnosticBundleResult(bool Success, string? ZipFilePath, string? ErrorMessage);
