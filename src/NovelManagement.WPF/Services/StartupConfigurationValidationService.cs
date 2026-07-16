using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using NovelManagement.AI.Services.DeepSeek.Models;
using NovelManagement.AI.Services.Ollama.Models;
using NovelManagement.AI.Services.OpenAICompatible.Models;
using NovelManagement.AI.Services.RWKV.Models;

namespace NovelManagement.WPF.Services;

/// <summary>
/// 提供启动阶段的静态配置校验能力。
/// </summary>
public class StartupConfigurationValidationService
{
    private readonly IConfiguration _configuration;

    public StartupConfigurationValidationService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// 执行静态配置校验。
    /// </summary>
    public IReadOnlyList<ConfigurationValidationItem> Validate()
    {
        var items = new List<ConfigurationValidationItem>();

        ValidatePaths(items);
        ValidateApplication(items);
        ValidateAiProviders(items);

        return items;
    }

    private void ValidatePaths(List<ConfigurationValidationItem> items)
    {
        AddDirectoryConfigItem(items, "应用数据根目录", _configuration["Paths:AppDataRoot"]);
        AddDirectoryConfigItem(items, "配置目录", _configuration["Paths:ConfigDirectory"]);
        AddDirectoryConfigItem(items, "数据目录", _configuration["Paths:DataDirectory"]);
        AddDirectoryConfigItem(items, "日志目录", _configuration["Paths:LogsDirectory"]);
        AddDirectoryConfigItem(items, "备份目录", _configuration["Paths:BackupsDirectory"]);

        var userConfigPath = _configuration["Paths:UserConfigurationPath"];
        items.Add(File.Exists(userConfigPath)
            ? new ConfigurationValidationItem("用户覆盖配置文件", ValidationSeverity.Info, $"已发现用户覆盖配置: {userConfigPath}")
            : new ConfigurationValidationItem("用户覆盖配置文件", ValidationSeverity.Warning, $"未发现用户覆盖配置: {userConfigPath}"));
    }

    private void ValidateApplication(List<ConfigurationValidationItem> items)
    {
        var environment = _configuration.GetValue("Application:Environment", "Production");
        items.Add(environment.Equals("Production", StringComparison.OrdinalIgnoreCase)
            ? new ConfigurationValidationItem("应用环境", ValidationSeverity.Info, $"当前环境为 {environment}")
            : new ConfigurationValidationItem("应用环境", ValidationSeverity.Warning, $"当前环境为 {environment}，生产部署建议使用 Production"));

        var backupRetentionDays = _configuration.GetValue("Backup:RetentionDays", 14);
        items.Add(backupRetentionDays > 0
            ? new ConfigurationValidationItem("备份保留策略", ValidationSeverity.Info, $"备份保留天数: {backupRetentionDays}")
            : new ConfigurationValidationItem("备份保留策略", ValidationSeverity.Error, "备份保留天数必须大于 0"));
    }

    private void ValidateAiProviders(List<ConfigurationValidationItem> items)
    {
        var aiEnabled = _configuration.GetValue("Features:EnableAI", true);
        if (!aiEnabled)
        {
            items.Add(new ConfigurationValidationItem("AI 功能开关", ValidationSeverity.Warning, "AI 功能当前已关闭"));
            return;
        }

        items.Add(new ConfigurationValidationItem("AI 功能开关", ValidationSeverity.Info, "AI 功能已启用"));

        var ollamaConfig = new OllamaConfiguration();
        _configuration.GetSection("AI:Providers:Ollama").Bind(ollamaConfig);
        AddModelValidationItems(items, "Ollama 配置", ollamaConfig.GetValidationErrors());

        var rwkvConfig = new RwkvConfiguration();
        _configuration.GetSection("AI:Providers:RWKV").Bind(rwkvConfig);
        AddModelValidationItems(items, "RWKV 配置", rwkvConfig.GetValidationErrors());

        var deepSeekConfig = new DeepSeekConfiguration();
        _configuration.GetSection("AI:Providers:DeepSeek").Bind(deepSeekConfig);
        AddModelValidationItems(items, "DeepSeek 配置", deepSeekConfig.GetValidationErrors());

        var zhipuConfig = new OpenAICompatibleConfiguration();
        _configuration.GetSection("AI:Providers:ZhipuAI").Bind(zhipuConfig);
        AddModelValidationItems(items, "智谱 AI 配置", zhipuConfig.GetValidationErrors());

        var openAiConfig = new OpenAICompatibleConfiguration();
        _configuration.GetSection("AI:Providers:OpenAI").Bind(openAiConfig);
        AddModelValidationItems(items, "OpenAI 兼容配置", openAiConfig.GetValidationErrors());
    }

    private static void AddModelValidationItems(
        List<ConfigurationValidationItem> items,
        string displayName,
        IReadOnlyCollection<string> errors)
    {
        if (errors.Count == 0)
        {
            items.Add(new ConfigurationValidationItem(displayName, ValidationSeverity.Info, "配置合法"));
            return;
        }

        foreach (var error in errors)
        {
            items.Add(new ConfigurationValidationItem(displayName, ValidationSeverity.Warning, error));
        }
    }

    private static void AddDirectoryConfigItem(List<ConfigurationValidationItem> items, string name, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            items.Add(new ConfigurationValidationItem(name, ValidationSeverity.Error, "路径配置缺失"));
            return;
        }

        items.Add(Directory.Exists(path)
            ? new ConfigurationValidationItem(name, ValidationSeverity.Info, $"目录存在: {path}")
            : new ConfigurationValidationItem(name, ValidationSeverity.Warning, $"目录不存在，将在运行时尝试创建: {path}"));
    }
}

/// <summary>
/// 单项配置校验结果。
/// </summary>
public sealed record ConfigurationValidationItem(string Name, ValidationSeverity Severity, string Message);

/// <summary>
/// 配置校验严重级别。
/// </summary>
public enum ValidationSeverity
{
    Info,
    Warning,
    Error
}
