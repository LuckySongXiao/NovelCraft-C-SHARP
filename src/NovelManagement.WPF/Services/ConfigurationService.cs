using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NovelManagement.AI.Services.DeepSeek.Models;

namespace NovelManagement.WPF.Services
{
    /// <summary>
    /// 配置服务
    /// </summary>
    public class ConfigurationService
    {
        private readonly ILogger<ConfigurationService> _logger;
        private readonly string _configDirectory;
        private readonly string _deepSeekConfigPath;
        private readonly string _uiConfigPath;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public ConfigurationService(ILogger<ConfigurationService> logger)
        {
            _logger = logger;
            
            // 配置文件目录
            _configDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "NovelManagement");
            
            _deepSeekConfigPath = Path.Combine(_configDirectory, "deepseek-config.json");
            _uiConfigPath = Path.Combine(_configDirectory, "ui-config.json");

            // 确保配置目录存在
            EnsureConfigDirectoryExists();
        }

        /// <summary>
        /// 保存DeepSeek配置
        /// </summary>
        /// <param name="configuration">配置对象</param>
        /// <returns>是否成功</returns>
        public async Task<bool> SaveDeepSeekConfigurationAsync(DeepSeekConfiguration configuration)
        {
            try
            {
                // 创建安全的配置副本（不包含敏感信息）
                var safeConfig = new
                {
                    BaseUrl = configuration.BaseUrl,
                    DefaultModel = configuration.DefaultModel,
                    DefaultTemperature = configuration.DefaultTemperature,
                    DefaultMaxTokens = configuration.DefaultMaxTokens,
                    EnableThinkingChain = configuration.EnableThinkingChain,
                    EnableStreaming = configuration.EnableStreaming,
                    TimeoutSeconds = configuration.TimeoutSeconds,
                    MaxRetries = configuration.MaxRetries,
                    // 不保存API密钥到文件
                    HasApiKey = !string.IsNullOrEmpty(configuration.ApiKey)
                };

                var json = JsonSerializer.Serialize(safeConfig, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await File.WriteAllTextAsync(_deepSeekConfigPath, json);
                _logger.LogInformation("DeepSeek配置已保存");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存DeepSeek配置失败");
                return false;
            }
        }

        /// <summary>
        /// 加载DeepSeek配置
        /// </summary>
        /// <returns>配置对象</returns>
        public async Task<DeepSeekConfiguration> LoadDeepSeekConfigurationAsync()
        {
            try
            {
                if (!File.Exists(_deepSeekConfigPath))
                {
                    _logger.LogInformation("DeepSeek配置文件不存在，使用默认配置");
                    return new DeepSeekConfiguration();
                }

                var json = await File.ReadAllTextAsync(_deepSeekConfigPath);
                var configData = JsonSerializer.Deserialize<JsonElement>(json);

                var configuration = new DeepSeekConfiguration();

                if (configData.TryGetProperty("BaseUrl", out var baseUrl))
                    configuration.BaseUrl = baseUrl.GetString() ?? configuration.BaseUrl;

                if (configData.TryGetProperty("DefaultModel", out var defaultModel))
                    configuration.DefaultModel = defaultModel.GetString() ?? configuration.DefaultModel;

                if (configData.TryGetProperty("DefaultTemperature", out var temperature))
                    configuration.DefaultTemperature = temperature.GetDouble();

                if (configData.TryGetProperty("DefaultMaxTokens", out var maxTokens))
                    configuration.DefaultMaxTokens = maxTokens.GetInt32();

                if (configData.TryGetProperty("EnableThinkingChain", out var enableThinking))
                    configuration.EnableThinkingChain = enableThinking.GetBoolean();

                if (configData.TryGetProperty("EnableStreaming", out var enableStreaming))
                    configuration.EnableStreaming = enableStreaming.GetBoolean();

                if (configData.TryGetProperty("TimeoutSeconds", out var timeout))
                    configuration.TimeoutSeconds = timeout.GetInt32();

                if (configData.TryGetProperty("MaxRetries", out var maxRetries))
                    configuration.MaxRetries = maxRetries.GetInt32();

                _logger.LogInformation("DeepSeek配置已加载");
                return configuration;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载DeepSeek配置失败，使用默认配置");
                return new DeepSeekConfiguration();
            }
        }

        /// <summary>
        /// 保存UI配置
        /// </summary>
        /// <param name="configuration">UI配置</param>
        /// <returns>是否成功</returns>
        public async Task<bool> SaveUIConfigurationAsync(UIConfiguration configuration)
        {
            try
            {
                var json = JsonSerializer.Serialize(configuration, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await File.WriteAllTextAsync(_uiConfigPath, json);
                _logger.LogInformation("UI配置已保存");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存UI配置失败");
                return false;
            }
        }

        /// <summary>
        /// 加载UI配置
        /// </summary>
        /// <returns>UI配置</returns>
        public async Task<UIConfiguration> LoadUIConfigurationAsync()
        {
            try
            {
                if (!File.Exists(_uiConfigPath))
                {
                    _logger.LogInformation("UI配置文件不存在，使用默认配置");
                    return new UIConfiguration();
                }

                var json = await File.ReadAllTextAsync(_uiConfigPath);
                var configuration = JsonSerializer.Deserialize<UIConfiguration>(json);

                _logger.LogInformation("UI配置已加载");
                return configuration ?? new UIConfiguration();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载UI配置失败，使用默认配置");
                return new UIConfiguration();
            }
        }

        /// <summary>
        /// 删除所有配置文件
        /// </summary>
        /// <returns>是否成功</returns>
        public async Task<bool> ClearAllConfigurationsAsync()
        {
            try
            {
                if (File.Exists(_deepSeekConfigPath))
                    File.Delete(_deepSeekConfigPath);

                if (File.Exists(_uiConfigPath))
                    File.Delete(_uiConfigPath);

                _logger.LogInformation("所有配置文件已清除");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清除配置文件失败");
                return false;
            }
        }

        /// <summary>
        /// 确保配置目录存在
        /// </summary>
        private void EnsureConfigDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(_configDirectory))
                {
                    Directory.CreateDirectory(_configDirectory);
                    _logger.LogInformation($"配置目录已创建: {_configDirectory}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"创建配置目录失败: {_configDirectory}");
            }
        }

        /// <summary>
        /// 获取配置目录路径
        /// </summary>
        /// <returns>配置目录路径</returns>
        public string GetConfigurationDirectory()
        {
            return _configDirectory;
        }

        /// <summary>
        /// 检查配置文件是否存在
        /// </summary>
        /// <param name="configType">配置类型</param>
        /// <returns>是否存在</returns>
        public bool ConfigurationExists(ConfigurationType configType)
        {
            return configType switch
            {
                ConfigurationType.DeepSeek => File.Exists(_deepSeekConfigPath),
                ConfigurationType.UI => File.Exists(_uiConfigPath),
                _ => false
            };
        }
    }

    /// <summary>
    /// UI配置
    /// </summary>
    public class UIConfiguration
    {
        /// <summary>
        /// 悬浮窗口透明度
        /// </summary>
        public double FloatingWindowOpacity { get; set; } = 0.9;

        /// <summary>
        /// 默认悬浮窗口位置
        /// </summary>
        public string DefaultFloatingPosition { get; set; } = "TopRight";

        /// <summary>
        /// 最大悬浮窗口数量
        /// </summary>
        public int MaxFloatingWindows { get; set; } = 3;

        /// <summary>
        /// 是否启用思维链自动显示
        /// </summary>
        public bool EnableAutoShowThinkingChain { get; set; } = true;

        /// <summary>
        /// 思维链窗口自动关闭时间（秒）
        /// </summary>
        public int ThinkingChainAutoCloseSeconds { get; set; } = 30;

        /// <summary>
        /// 主题名称
        /// </summary>
        public string ThemeName { get; set; } = "Default";

        /// <summary>
        /// 语言设置
        /// </summary>
        public string Language { get; set; } = "zh-CN";

        /// <summary>
        /// 是否启用动画效果
        /// </summary>
        public bool EnableAnimations { get; set; } = true;

        /// <summary>
        /// 字体大小
        /// </summary>
        public double FontSize { get; set; } = 14.0;

        /// <summary>
        /// 是否启用声音提示
        /// </summary>
        public bool EnableSoundNotifications { get; set; } = true;

        /// <summary>
        /// 自动保存间隔（分钟）
        /// </summary>
        public int AutoSaveIntervalMinutes { get; set; } = 5;
    }

    /// <summary>
    /// 配置类型
    /// </summary>
    public enum ConfigurationType
    {
        /// <summary>
        /// DeepSeek配置
        /// </summary>
        DeepSeek,

        /// <summary>
        /// UI配置
        /// </summary>
        UI
    }


}
