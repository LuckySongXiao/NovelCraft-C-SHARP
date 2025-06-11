using System.ComponentModel.DataAnnotations;
using NovelManagement.AI.Interfaces;

namespace NovelManagement.AI.Services.Ollama.Models
{
    /// <summary>
    /// Ollama配置
    /// </summary>
    public class OllamaConfiguration : IModelConfiguration
    {
        /// <summary>
        /// 提供者名称
        /// </summary>
        public string ProviderName => "Ollama";

        /// <summary>
        /// Ollama服务器地址
        /// </summary>
        [Required]
        public string BaseUrl { get; set; } = "http://localhost:11434";

        /// <summary>
        /// 默认模型名称
        /// </summary>
        public string DefaultModel { get; set; } = "llama2";

        /// <summary>
        /// 请求超时时间（秒）
        /// </summary>
        [Range(1, 300)]
        public int TimeoutSeconds { get; set; } = 120;

        /// <summary>
        /// 最大重试次数
        /// </summary>
        [Range(0, 10)]
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// 是否启用GPU加速
        /// </summary>
        public bool EnableGPU { get; set; } = true;

        /// <summary>
        /// GPU设备ID（-1表示自动选择）
        /// </summary>
        public int GPUDeviceId { get; set; } = -1;

        /// <summary>
        /// 并发请求数限制
        /// </summary>
        [Range(1, 10)]
        public int MaxConcurrentRequests { get; set; } = 3;

        /// <summary>
        /// 是否启用模型预加载
        /// </summary>
        public bool EnableModelPreload { get; set; } = true;

        /// <summary>
        /// 模型保持时间（分钟，0表示永久保持）
        /// </summary>
        [Range(0, 1440)]
        public int ModelKeepAliveMinutes { get; set; } = 30;

        /// <summary>
        /// 是否启用详细日志
        /// </summary>
        public bool EnableVerboseLogging { get; set; } = false;

        /// <summary>
        /// 自定义模型参数
        /// </summary>
        public Dictionary<string, object> CustomParameters { get; set; } = new();

        /// <summary>
        /// 验证配置
        /// </summary>
        /// <returns>验证结果</returns>
        public bool IsValid()
        {
            var errors = GetValidationErrors();
            return errors.Count == 0;
        }

        /// <summary>
        /// 获取验证错误
        /// </summary>
        /// <returns>错误列表</returns>
        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();

            // 验证BaseUrl
            if (string.IsNullOrWhiteSpace(BaseUrl))
            {
                errors.Add("BaseUrl不能为空");
            }
            else if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri) || 
                     (uri.Scheme != "http" && uri.Scheme != "https"))
            {
                errors.Add("BaseUrl格式无效，必须是有效的HTTP或HTTPS地址");
            }

            // 验证DefaultModel
            if (string.IsNullOrWhiteSpace(DefaultModel))
            {
                errors.Add("DefaultModel不能为空");
            }

            // 验证TimeoutSeconds
            if (TimeoutSeconds < 1 || TimeoutSeconds > 300)
            {
                errors.Add("TimeoutSeconds必须在1-300秒之间");
            }

            // 验证MaxRetries
            if (MaxRetries < 0 || MaxRetries > 10)
            {
                errors.Add("MaxRetries必须在0-10之间");
            }

            // 验证MaxConcurrentRequests
            if (MaxConcurrentRequests < 1 || MaxConcurrentRequests > 10)
            {
                errors.Add("MaxConcurrentRequests必须在1-10之间");
            }

            // 验证ModelKeepAliveMinutes
            if (ModelKeepAliveMinutes < 0 || ModelKeepAliveMinutes > 1440)
            {
                errors.Add("ModelKeepAliveMinutes必须在0-1440分钟之间");
            }

            return errors;
        }

        /// <summary>
        /// 克隆配置
        /// </summary>
        /// <returns>配置副本</returns>
        public OllamaConfiguration Clone()
        {
            return new OllamaConfiguration
            {
                BaseUrl = BaseUrl,
                DefaultModel = DefaultModel,
                TimeoutSeconds = TimeoutSeconds,
                MaxRetries = MaxRetries,
                EnableGPU = EnableGPU,
                GPUDeviceId = GPUDeviceId,
                MaxConcurrentRequests = MaxConcurrentRequests,
                EnableModelPreload = EnableModelPreload,
                ModelKeepAliveMinutes = ModelKeepAliveMinutes,
                EnableVerboseLogging = EnableVerboseLogging,
                CustomParameters = new Dictionary<string, object>(CustomParameters)
            };
        }

        /// <summary>
        /// 获取默认配置
        /// </summary>
        /// <returns>默认配置</returns>
        public static OllamaConfiguration GetDefault()
        {
            return new OllamaConfiguration();
        }

        /// <summary>
        /// 从字典创建配置
        /// </summary>
        /// <param name="settings">设置字典</param>
        /// <returns>配置实例</returns>
        public static OllamaConfiguration FromDictionary(Dictionary<string, object> settings)
        {
            var config = new OllamaConfiguration();

            if (settings.TryGetValue("BaseUrl", out var baseUrl))
                config.BaseUrl = baseUrl.ToString() ?? config.BaseUrl;

            if (settings.TryGetValue("DefaultModel", out var defaultModel))
                config.DefaultModel = defaultModel.ToString() ?? config.DefaultModel;

            if (settings.TryGetValue("TimeoutSeconds", out var timeout) && int.TryParse(timeout.ToString(), out var timeoutInt))
                config.TimeoutSeconds = timeoutInt;

            if (settings.TryGetValue("MaxRetries", out var retries) && int.TryParse(retries.ToString(), out var retriesInt))
                config.MaxRetries = retriesInt;

            if (settings.TryGetValue("EnableGPU", out var enableGpu) && bool.TryParse(enableGpu.ToString(), out var enableGpuBool))
                config.EnableGPU = enableGpuBool;

            if (settings.TryGetValue("GPUDeviceId", out var gpuId) && int.TryParse(gpuId.ToString(), out var gpuIdInt))
                config.GPUDeviceId = gpuIdInt;

            if (settings.TryGetValue("MaxConcurrentRequests", out var maxConcurrent) && int.TryParse(maxConcurrent.ToString(), out var maxConcurrentInt))
                config.MaxConcurrentRequests = maxConcurrentInt;

            if (settings.TryGetValue("EnableModelPreload", out var preload) && bool.TryParse(preload.ToString(), out var preloadBool))
                config.EnableModelPreload = preloadBool;

            if (settings.TryGetValue("ModelKeepAliveMinutes", out var keepAlive) && int.TryParse(keepAlive.ToString(), out var keepAliveInt))
                config.ModelKeepAliveMinutes = keepAliveInt;

            if (settings.TryGetValue("EnableVerboseLogging", out var verbose) && bool.TryParse(verbose.ToString(), out var verboseBool))
                config.EnableVerboseLogging = verboseBool;

            return config;
        }

        /// <summary>
        /// 转换为字典
        /// </summary>
        /// <returns>设置字典</returns>
        public Dictionary<string, object> ToDictionary()
        {
            var dict = new Dictionary<string, object>
            {
                ["BaseUrl"] = BaseUrl,
                ["DefaultModel"] = DefaultModel,
                ["TimeoutSeconds"] = TimeoutSeconds,
                ["MaxRetries"] = MaxRetries,
                ["EnableGPU"] = EnableGPU,
                ["GPUDeviceId"] = GPUDeviceId,
                ["MaxConcurrentRequests"] = MaxConcurrentRequests,
                ["EnableModelPreload"] = EnableModelPreload,
                ["ModelKeepAliveMinutes"] = ModelKeepAliveMinutes,
                ["EnableVerboseLogging"] = EnableVerboseLogging
            };

            foreach (var kvp in CustomParameters)
            {
                dict[kvp.Key] = kvp.Value;
            }

            return dict;
        }
    }
}
