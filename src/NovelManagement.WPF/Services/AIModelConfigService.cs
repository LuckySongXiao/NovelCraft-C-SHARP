using System;
using System.IO;
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
        /// 测试连接
        /// </summary>
        /// <returns>是否连接成功</returns>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                _logger?.LogInformation("开始测试AI模型连接");
                
                // 模拟连接测试
                await Task.Delay(2000);
                
                // 这里应该实际测试各个AI模型的连接
                // 目前返回模拟结果
                var success = new Random().NextDouble() > 0.1; // 90%成功率
                
                _logger?.LogInformation("AI模型连接测试完成，结果: {Success}", success);
                return success;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "AI模型连接测试失败");
                return false;
            }
        }

        /// <summary>
        /// 获取性能指标
        /// </summary>
        /// <returns>性能指标</returns>
        public async Task<PerformanceMetrics> GetPerformanceMetricsAsync()
        {
            try
            {
                await Task.Delay(100); // 模拟数据获取
                
                // 模拟性能数据
                var random = new Random();
                return new PerformanceMetrics
                {
                    AverageResponseTime = 2.0 + random.NextDouble() * 2.0,
                    SuccessRate = 95.0 + random.NextDouble() * 5.0,
                    CacheHitRate = 70.0 + random.NextDouble() * 20.0,
                    TodayRequests = random.Next(100, 300)
                };
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
