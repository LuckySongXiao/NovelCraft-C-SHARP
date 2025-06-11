using System.ComponentModel.DataAnnotations;

namespace NovelManagement.AI.Services.DeepSeek.Models
{
    /// <summary>
    /// DeepSeek API配置
    /// </summary>
    public class DeepSeekConfiguration
    {
        /// <summary>
        /// API密钥
        /// </summary>
        [Required]
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// API基础URL
        /// </summary>
        public string BaseUrl { get; set; } = "https://api.deepseek.com";

        /// <summary>
        /// 默认模型名称
        /// </summary>
        public string DefaultModel { get; set; } = "deepseek-chat";

        /// <summary>
        /// 请求超时时间（秒）
        /// </summary>
        public int TimeoutSeconds { get; set; } = 60;

        /// <summary>
        /// 最大重试次数
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// 是否启用思维链
        /// </summary>
        public bool EnableThinkingChain { get; set; } = true;

        /// <summary>
        /// 思维链显示延迟（毫秒）
        /// </summary>
        public int ThinkingChainDelay { get; set; } = 100;

        /// <summary>
        /// 默认温度参数
        /// </summary>
        public double DefaultTemperature { get; set; } = 0.7;

        /// <summary>
        /// 默认最大令牌数
        /// </summary>
        public int DefaultMaxTokens { get; set; } = 4000;

        /// <summary>
        /// 是否启用流式响应
        /// </summary>
        public bool EnableStreaming { get; set; } = true;

        /// <summary>
        /// 验证配置是否有效
        /// </summary>
        /// <returns>验证结果</returns>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(ApiKey) &&
                   !string.IsNullOrWhiteSpace(BaseUrl) &&
                   !string.IsNullOrWhiteSpace(DefaultModel) &&
                   TimeoutSeconds > 0 &&
                   MaxRetries >= 0 &&
                   DefaultTemperature >= 0 && DefaultTemperature <= 2 &&
                   DefaultMaxTokens > 0;
        }

        /// <summary>
        /// 获取验证错误信息
        /// </summary>
        /// <returns>错误信息列表</returns>
        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(ApiKey))
                errors.Add("API密钥不能为空");

            if (string.IsNullOrWhiteSpace(BaseUrl))
                errors.Add("API基础URL不能为空");

            if (string.IsNullOrWhiteSpace(DefaultModel))
                errors.Add("默认模型名称不能为空");

            if (TimeoutSeconds <= 0)
                errors.Add("超时时间必须大于0");

            if (MaxRetries < 0)
                errors.Add("最大重试次数不能小于0");

            if (DefaultTemperature < 0 || DefaultTemperature > 2)
                errors.Add("温度参数必须在0-2之间");

            if (DefaultMaxTokens <= 0)
                errors.Add("最大令牌数必须大于0");

            return errors;
        }
    }
}
