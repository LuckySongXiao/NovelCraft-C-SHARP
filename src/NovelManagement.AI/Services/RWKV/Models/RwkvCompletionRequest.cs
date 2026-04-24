using System.Text.Json.Serialization;

namespace NovelManagement.AI.Services.RWKV.Models
{
    /// <summary>
    /// RWKV 续写请求
    /// </summary>
    public class RwkvCompletionRequest
    {
        /// <summary>
        /// 输入文本（续写的前文）
        /// </summary>
        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;

        /// <summary>
        /// 最大生成 token 数
        /// </summary>
        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; } = 200;

        /// <summary>
        /// 温度参数
        /// </summary>
        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 1.0;

        /// <summary>
        /// Top-P 采样
        /// </summary>
        [JsonPropertyName("top_p")]
        public double TopP { get; set; } = 0.7;

        /// <summary>
        /// Top-K 采样
        /// </summary>
        [JsonPropertyName("top_k")]
        public int TopK { get; set; } = 50;

        /// <summary>
        /// 频率惩罚
        /// </summary>
        [JsonPropertyName("frequency_penalty")]
        public double FrequencyPenalty { get; set; } = 0.0;

        /// <summary>
        /// 出现惩罚
        /// </summary>
        [JsonPropertyName("presence_penalty")]
        public double PresencePenalty { get; set; } = 0.0;

        /// <summary>
        /// 停止词列表
        /// </summary>
        [JsonPropertyName("stop")]
        public List<string> Stop { get; set; } = new();
    }

    /// <summary>
    /// RWKV 续写响应
    /// </summary>
    public class RwkvCompletionResponse
    {
        /// <summary>
        /// 生成的文本
        /// </summary>
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// 生成的 token 数
        /// </summary>
        [JsonPropertyName("tokens_generated")]
        public int TokensGenerated { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; } = true;

        /// <summary>
        /// 错误信息
        /// </summary>
        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    /// <summary>
    /// RWKV 服务状态响应
    /// </summary>
    public class RwkvStatusResponse
    {
        /// <summary>
        /// 是否就绪
        /// </summary>
        [JsonPropertyName("ready")]
        public bool Ready { get; set; }

        /// <summary>
        /// 已加载的模型名称
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// 推理策略
        /// </summary>
        [JsonPropertyName("strategy")]
        public string Strategy { get; set; } = string.Empty;

        /// <summary>
        /// GPU 信息
        /// </summary>
        [JsonPropertyName("gpu_info")]
        public string? GpuInfo { get; set; }
    }
}
