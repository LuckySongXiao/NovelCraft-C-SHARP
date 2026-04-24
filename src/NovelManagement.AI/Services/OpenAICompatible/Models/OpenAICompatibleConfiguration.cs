using NovelManagement.AI.Interfaces;

namespace NovelManagement.AI.Services.OpenAICompatible.Models
{
    /// <summary>
    /// OpenAI 兼容接口配置（支持智谱、Ollama、自定义 OpenAI 端点）
    /// </summary>
    public class OpenAICompatibleConfiguration : IModelConfiguration
    {
        /// <summary>
        /// 提供者名称
        /// </summary>
        public string ProviderName { get; set; } = "OpenAICompatible";

        /// <summary>
        /// 提供者类型标识（ZhipuAI / Ollama / Custom）
        /// </summary>
        public string ProviderKind { get; set; } = "Custom";

        /// <summary>
        /// API 基础地址
        /// </summary>
        /// <remarks>
        /// 智谱: https://open.bigmodel.cn/api/paas/v4
        /// Ollama: http://localhost:11434/v1
        /// 自定义: 用户指定
        /// </remarks>
        public string BaseUrl { get; set; } = "https://open.bigmodel.cn/api/paas/v4";

        /// <summary>
        /// API Key（Ollama 可为空）
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// 默认模型名称
        /// </summary>
        /// <remarks>
        /// 智谱免费: glm-4-flash
        /// Ollama: 用户自定义模型名
        /// </remarks>
        public string DefaultModel { get; set; } = "glm-4-flash";

        /// <summary>
        /// 请求超时（秒）
        /// </summary>
        public int TimeoutSeconds { get; set; } = 120;

        /// <summary>
        /// 最大重试次数
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// 默认温度参数
        /// </summary>
        public double DefaultTemperature { get; set; } = 0.7;

        /// <summary>
        /// 默认最大 token 数
        /// </summary>
        public int DefaultMaxTokens { get; set; } = 4000;

        /// <summary>
        /// 是否启用流式响应
        /// </summary>
        public bool EnableStreaming { get; set; } = true;

        /// <summary>
        /// 自定义请求头
        /// </summary>
        public Dictionary<string, string> CustomHeaders { get; set; } = new();

        /// <inheritdoc/>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(BaseUrl) && GetValidationErrors().Count == 0;
        }

        /// <inheritdoc/>
        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(BaseUrl))
                errors.Add("API 基础地址不能为空");
            // Ollama 不需要 API Key，其他类型需要
            if (ProviderKind != "Ollama" && string.IsNullOrWhiteSpace(ApiKey))
                errors.Add("API Key 不能为空");
            return errors;
        }

        /// <summary>
        /// 创建智谱 AI 配置
        /// </summary>
        public static OpenAICompatibleConfiguration CreateZhipuAIConfig(string apiKey, string model = "glm-4-flash")
        {
            return new OpenAICompatibleConfiguration
            {
                ProviderName = "ZhipuAI",
                ProviderKind = "ZhipuAI",
                BaseUrl = "https://open.bigmodel.cn/api/paas/v4",
                ApiKey = apiKey,
                DefaultModel = model,
                DefaultTemperature = 0.7,
                DefaultMaxTokens = 4000
            };
        }

        /// <summary>
        /// 创建 Ollama 配置
        /// </summary>
        public static OpenAICompatibleConfiguration CreateOllamaConfig(string baseUrl = "http://localhost:11434", string model = "qwen2.5:7b")
        {
            return new OpenAICompatibleConfiguration
            {
                ProviderName = "Ollama",
                ProviderKind = "Ollama",
                BaseUrl = $"{baseUrl.TrimEnd('/')}/v1",
                ApiKey = "ollama", // Ollama v1 接口需要任意非空 key
                DefaultModel = model,
                DefaultTemperature = 0.7,
                DefaultMaxTokens = 4000
            };
        }
    }
}
