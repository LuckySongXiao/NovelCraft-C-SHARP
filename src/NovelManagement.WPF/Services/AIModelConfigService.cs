using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace NovelManagement.WPF.Services
{
    /// <summary>
    /// AI模型配置服务
    /// </summary>
    public class AIModelConfigService
    {
        #region 字段和属性

        private readonly ILogger<AIModelConfigService>? _logger;
        private readonly string _configFilePath;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public AIModelConfigService(ILogger<AIModelConfigService>? logger = null)
        {
            _logger = logger;
            _configFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                "NovelManagement", "ai_model_config.json");
            
            // 确保配置目录存在
            var configDir = Path.GetDirectoryName(_configFilePath);
            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir!);
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 加载配置
        /// </summary>
        /// <returns>模型配置</returns>
        public async Task<ModelConfiguration> LoadConfigurationAsync()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    var json = await File.ReadAllTextAsync(_configFilePath);
                    var config = JsonConvert.DeserializeObject<ModelConfiguration>(json);
                    
                    _logger?.LogInformation("AI模型配置加载成功");
                    return config ?? GetDefaultConfiguration();
                }
                else
                {
                    _logger?.LogInformation("配置文件不存在，使用默认配置");
                    return GetDefaultConfiguration();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载AI模型配置失败");
                return GetDefaultConfiguration();
            }
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        /// <param name="config">模型配置</param>
        public async Task SaveConfigurationAsync(ModelConfiguration config)
        {
            try
            {
                var json = JsonConvert.SerializeObject(config, Formatting.Indented);
                await File.WriteAllTextAsync(_configFilePath, json);
                
                _logger?.LogInformation("AI模型配置保存成功");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存AI模型配置失败");
                throw;
            }
        }

        /// <summary>
        /// 导出配置到指定文件。
        /// </summary>
        /// <param name="config">模型配置。</param>
        /// <param name="filePath">目标文件路径。</param>
        public async Task ExportConfigurationAsync(ModelConfiguration config, string filePath)
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonConvert.SerializeObject(config, Formatting.Indented);
                await File.WriteAllTextAsync(filePath, json);
                _logger?.LogInformation("AI模型配置导出成功: {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "导出AI模型配置失败: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// 从指定文件导入配置。
        /// </summary>
        /// <param name="filePath">配置文件路径。</param>
        /// <returns>模型配置。</returns>
        public async Task<ModelConfiguration> ImportConfigurationAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException("未找到配置文件。", filePath);
                }

                var json = await File.ReadAllTextAsync(filePath);
                var config = JsonConvert.DeserializeObject<ModelConfiguration>(json);
                if (config == null)
                {
                    throw new InvalidDataException("配置文件内容无效或为空。");
                }

                _logger?.LogInformation("AI模型配置导入成功: {FilePath}", filePath);
                return config;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "导入AI模型配置失败: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// 测试连接
        /// </summary>
        /// <returns>是否连接成功</returns>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                _logger?.LogInformation("开始测试AI推理服务连接");

                var rwkvService = App.ServiceProvider?.GetService(typeof(NovelManagement.AI.Services.RWKV.IRwkvLightningService)) as NovelManagement.AI.Services.RWKV.IRwkvLightningService;
                if (rwkvService == null)
                {
                    _logger?.LogWarning("未找到 RWKV 推理服务注册，连接测试失败");
                    return false;
                }

                // 真实探测：HTTP GET {BaseUrl}/health（llama-server 标准就绪端点）；探测失败时回退推理服务可用状态
                var baseUrl = string.IsNullOrWhiteSpace(rwkvService.Configuration?.BaseUrl)
                    ? "http://localhost:8000"
                    : rwkvService.Configuration.BaseUrl.TrimEnd('/');
                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                try
                {
                    var response = await httpClient.GetAsync($"{baseUrl}/health");
                    _logger?.LogInformation("AI推理服务连接测试完成: {BaseUrl} → {StatusCode}", baseUrl, (int)response.StatusCode);
                    return response.IsSuccessStatusCode;
                }
                catch (Exception probeEx)
                {
                    _logger?.LogWarning(probeEx, "HTTP 探测 {BaseUrl} 失败，回退推理服务可用状态判断", baseUrl);
                    var available = rwkvService.IsAvailable;
                    _logger?.LogInformation("RWKV 推理服务可用状态: {Available}", available);
                    return available;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "AI模型连接测试失败");
                return false;
            }
        }

        /// <summary>
        /// 获取性能指标（真实数据：来自 AIUsageStatisticsService 的当日使用统计）
        /// </summary>
        /// <returns>性能指标</returns>
        public async Task<PerformanceMetrics> GetPerformanceMetricsAsync()
        {
            try
            {
                var metrics = new PerformanceMetrics();

                var statisticsService = App.ServiceProvider?.GetService(typeof(AIUsageStatisticsService)) as AIUsageStatisticsService;
                if (statisticsService != null)
                {
                    var stats = statisticsService.GetUsageStatistics(TimeRange.Today);
                    metrics.TodayRequests = stats.TotalRequests;
                    metrics.SuccessRate = stats.SuccessRate;
                    metrics.AverageResponseTime = stats.AverageExecutionTime.TotalSeconds;
                }

                // 缓存命中率：应用内各 AICacheService 实例相互独立、无全局统计，如实返回 0（不再伪造随机值）
                metrics.CacheHitRate = 0;

                return metrics;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取性能指标失败");
                return new PerformanceMetrics();
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 获取默认配置
        /// </summary>
        /// <returns>默认配置</returns>
        private ModelConfiguration GetDefaultConfiguration()
        {
            return new ModelConfiguration
            {
                DefaultModel = "DeepSeek-V3",
                ApiKey = "",
                TimeoutSeconds = 30,
                EnableCache = true,
                EnableLogging = false,
                DialogueModel = "DeepSeek-V3",
                DialogueCreativity = 0.7,
                DialogueMaxLength = 800,
                AnalysisModel = "GPT-4",
                FactionModel = "Claude-3"
            };
        }

        #endregion

        #region 嵌套类

        /// <summary>
        /// 模型配置
        /// </summary>
        public class ModelConfiguration
        {
            public string DefaultModel { get; set; } = "DeepSeek-V3";
            public string ApiKey { get; set; } = "";
            public int TimeoutSeconds { get; set; } = 30;
            public bool EnableCache { get; set; } = true;
            public bool EnableLogging { get; set; } = false;
            
            // 对话生成配置
            public string DialogueModel { get; set; } = "DeepSeek-V3";
            public double DialogueCreativity { get; set; } = 0.7;
            public int DialogueMaxLength { get; set; } = 800;
            
            // 分析配置
            public string AnalysisModel { get; set; } = "GPT-4";
            
            // 势力分析配置
            public string FactionModel { get; set; } = "Claude-3";
        }

        /// <summary>
        /// 性能指标
        /// </summary>
        public class PerformanceMetrics
        {
            public double AverageResponseTime { get; set; }
            public double SuccessRate { get; set; }
            public double CacheHitRate { get; set; }
            public int TodayRequests { get; set; }
        }

        #endregion
    }
}
