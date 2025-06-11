using Newtonsoft.Json;

namespace NovelManagement.AI.Services.DeepSeek.Models
{
    /// <summary>
    /// DeepSeek API响应模型
    /// </summary>
    public class DeepSeekResponse
    {
        /// <summary>
        /// 响应ID
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 对象类型
        /// </summary>
        [JsonProperty("object")]
        public string Object { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间戳
        /// </summary>
        [JsonProperty("created")]
        public long Created { get; set; }

        /// <summary>
        /// 模型名称
        /// </summary>
        [JsonProperty("model")]
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// 选择列表
        /// </summary>
        [JsonProperty("choices")]
        public List<DeepSeekChoice> Choices { get; set; } = new();

        /// <summary>
        /// 使用情况
        /// </summary>
        [JsonProperty("usage")]
        public DeepSeekUsage? Usage { get; set; }

        /// <summary>
        /// 系统指纹
        /// </summary>
        [JsonProperty("system_fingerprint")]
        public string? SystemFingerprint { get; set; }

        /// <summary>
        /// 获取第一个选择的内容
        /// </summary>
        /// <returns>内容文本</returns>
        public string GetContent()
        {
            return Choices.FirstOrDefault()?.Message?.Content ?? string.Empty;
        }

        /// <summary>
        /// 获取思维链内容
        /// </summary>
        /// <returns>思维链文本</returns>
        public string GetThinkingContent()
        {
            var choice = Choices.FirstOrDefault();
            if (choice?.Message?.Reasoning != null)
            {
                return choice.Message.Reasoning;
            }
            return string.Empty;
        }

        /// <summary>
        /// 是否包含思维链
        /// </summary>
        /// <returns>是否包含思维链</returns>
        public bool HasThinking()
        {
            return !string.IsNullOrEmpty(GetThinkingContent());
        }
    }

    /// <summary>
    /// DeepSeek选择模型
    /// </summary>
    public class DeepSeekChoice
    {
        /// <summary>
        /// 索引
        /// </summary>
        [JsonProperty("index")]
        public int Index { get; set; }

        /// <summary>
        /// 消息
        /// </summary>
        [JsonProperty("message")]
        public DeepSeekResponseMessage? Message { get; set; }

        /// <summary>
        /// 完成原因
        /// </summary>
        [JsonProperty("finish_reason")]
        public string? FinishReason { get; set; }

        /// <summary>
        /// 日志概率
        /// </summary>
        [JsonProperty("logprobs")]
        public object? LogProbs { get; set; }
    }

    /// <summary>
    /// DeepSeek响应消息模型
    /// </summary>
    public class DeepSeekResponseMessage
    {
        /// <summary>
        /// 角色
        /// </summary>
        [JsonProperty("role")]
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// 内容
        /// </summary>
        [JsonProperty("content")]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 思维链内容（DeepSeek特有）
        /// </summary>
        [JsonProperty("reasoning")]
        public string? Reasoning { get; set; }
    }

    /// <summary>
    /// DeepSeek使用情况模型
    /// </summary>
    public class DeepSeekUsage
    {
        /// <summary>
        /// 提示令牌数
        /// </summary>
        [JsonProperty("prompt_tokens")]
        public int PromptTokens { get; set; }

        /// <summary>
        /// 完成令牌数
        /// </summary>
        [JsonProperty("completion_tokens")]
        public int CompletionTokens { get; set; }

        /// <summary>
        /// 总令牌数
        /// </summary>
        [JsonProperty("total_tokens")]
        public int TotalTokens { get; set; }

        /// <summary>
        /// 思维链令牌数
        /// </summary>
        [JsonProperty("reasoning_tokens")]
        public int? ReasoningTokens { get; set; }
    }

    /// <summary>
    /// DeepSeek流式响应模型
    /// </summary>
    public class DeepSeekStreamResponse
    {
        /// <summary>
        /// 响应ID
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 对象类型
        /// </summary>
        [JsonProperty("object")]
        public string Object { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间戳
        /// </summary>
        [JsonProperty("created")]
        public long Created { get; set; }

        /// <summary>
        /// 模型名称
        /// </summary>
        [JsonProperty("model")]
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// 选择列表
        /// </summary>
        [JsonProperty("choices")]
        public List<DeepSeekStreamChoice> Choices { get; set; } = new();
    }

    /// <summary>
    /// DeepSeek流式选择模型
    /// </summary>
    public class DeepSeekStreamChoice
    {
        /// <summary>
        /// 索引
        /// </summary>
        [JsonProperty("index")]
        public int Index { get; set; }

        /// <summary>
        /// 增量消息
        /// </summary>
        [JsonProperty("delta")]
        public DeepSeekDelta? Delta { get; set; }

        /// <summary>
        /// 完成原因
        /// </summary>
        [JsonProperty("finish_reason")]
        public string? FinishReason { get; set; }
    }

    /// <summary>
    /// DeepSeek增量模型
    /// </summary>
    public class DeepSeekDelta
    {
        /// <summary>
        /// 角色
        /// </summary>
        [JsonProperty("role")]
        public string? Role { get; set; }

        /// <summary>
        /// 内容
        /// </summary>
        [JsonProperty("content")]
        public string? Content { get; set; }

        /// <summary>
        /// 思维链内容
        /// </summary>
        [JsonProperty("reasoning")]
        public string? Reasoning { get; set; }
    }
}
