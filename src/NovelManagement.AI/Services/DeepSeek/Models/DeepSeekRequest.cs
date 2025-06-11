using Newtonsoft.Json;

namespace NovelManagement.AI.Services.DeepSeek.Models
{
    /// <summary>
    /// DeepSeek API请求模型
    /// </summary>
    public class DeepSeekRequest
    {
        /// <summary>
        /// 模型名称
        /// </summary>
        [JsonProperty("model")]
        public string Model { get; set; } = "deepseek-chat";

        /// <summary>
        /// 消息列表
        /// </summary>
        [JsonProperty("messages")]
        public List<DeepSeekMessage> Messages { get; set; } = new();

        /// <summary>
        /// 温度参数 (0-2)
        /// </summary>
        [JsonProperty("temperature")]
        public double Temperature { get; set; } = 0.7;

        /// <summary>
        /// 最大令牌数
        /// </summary>
        [JsonProperty("max_tokens")]
        public int MaxTokens { get; set; } = 4000;

        /// <summary>
        /// 是否启用流式响应
        /// </summary>
        [JsonProperty("stream")]
        public bool Stream { get; set; } = false;

        /// <summary>
        /// Top-p参数
        /// </summary>
        [JsonProperty("top_p")]
        public double TopP { get; set; } = 1.0;

        /// <summary>
        /// 频率惩罚
        /// </summary>
        [JsonProperty("frequency_penalty")]
        public double FrequencyPenalty { get; set; } = 0.0;

        /// <summary>
        /// 存在惩罚
        /// </summary>
        [JsonProperty("presence_penalty")]
        public double PresencePenalty { get; set; } = 0.0;

        /// <summary>
        /// 停止词
        /// </summary>
        [JsonProperty("stop")]
        public List<string>? Stop { get; set; }

        /// <summary>
        /// 用户ID
        /// </summary>
        [JsonProperty("user")]
        public string? User { get; set; }
    }

    /// <summary>
    /// DeepSeek消息模型
    /// </summary>
    public class DeepSeekMessage
    {
        /// <summary>
        /// 角色 (system, user, assistant)
        /// </summary>
        [JsonProperty("role")]
        public string Role { get; set; } = "user";

        /// <summary>
        /// 消息内容
        /// </summary>
        [JsonProperty("content")]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 消息名称（可选）
        /// </summary>
        [JsonProperty("name")]
        public string? Name { get; set; }

        /// <summary>
        /// 创建系统消息
        /// </summary>
        /// <param name="content">消息内容</param>
        /// <returns>系统消息</returns>
        public static DeepSeekMessage CreateSystemMessage(string content)
        {
            return new DeepSeekMessage
            {
                Role = "system",
                Content = content
            };
        }

        /// <summary>
        /// 创建用户消息
        /// </summary>
        /// <param name="content">消息内容</param>
        /// <returns>用户消息</returns>
        public static DeepSeekMessage CreateUserMessage(string content)
        {
            return new DeepSeekMessage
            {
                Role = "user",
                Content = content
            };
        }

        /// <summary>
        /// 创建助手消息
        /// </summary>
        /// <param name="content">消息内容</param>
        /// <returns>助手消息</returns>
        public static DeepSeekMessage CreateAssistantMessage(string content)
        {
            return new DeepSeekMessage
            {
                Role = "assistant",
                Content = content
            };
        }
    }
}
