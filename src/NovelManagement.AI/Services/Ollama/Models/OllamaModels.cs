using System.Text.Json.Serialization;

namespace NovelManagement.AI.Services.Ollama.Models
{
    /// <summary>
    /// Ollama聊天请求
    /// </summary>
    public class OllamaChatRequest
    {
        /// <summary>
        /// 模型名称
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// 消息列表
        /// </summary>
        [JsonPropertyName("messages")]
        public List<OllamaChatMessage> Messages { get; set; } = new();

        /// <summary>
        /// 是否流式响应
        /// </summary>
        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = false;

        /// <summary>
        /// 模型选项
        /// </summary>
        [JsonPropertyName("options")]
        public OllamaOptions? Options { get; set; }

        /// <summary>
        /// 格式化选项
        /// </summary>
        [JsonPropertyName("format")]
        public string? Format { get; set; }

        /// <summary>
        /// 保持模型加载时间
        /// </summary>
        [JsonPropertyName("keep_alive")]
        public string? KeepAlive { get; set; }
    }

    /// <summary>
    /// Ollama聊天消息
    /// </summary>
    public class OllamaChatMessage
    {
        /// <summary>
        /// 角色
        /// </summary>
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// 内容
        /// </summary>
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 图片（base64编码）
        /// </summary>
        [JsonPropertyName("images")]
        public List<string>? Images { get; set; }
    }

    /// <summary>
    /// Ollama聊天响应
    /// </summary>
    public class OllamaChatResponse
    {
        /// <summary>
        /// 模型名称
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 消息
        /// </summary>
        [JsonPropertyName("message")]
        public OllamaChatMessage? Message { get; set; }

        /// <summary>
        /// 是否完成
        /// </summary>
        [JsonPropertyName("done")]
        public bool Done { get; set; }

        /// <summary>
        /// 完成原因
        /// </summary>
        [JsonPropertyName("done_reason")]
        public string? DoneReason { get; set; }

        /// <summary>
        /// 总持续时间（纳秒）
        /// </summary>
        [JsonPropertyName("total_duration")]
        public long? TotalDuration { get; set; }

        /// <summary>
        /// 加载持续时间（纳秒）
        /// </summary>
        [JsonPropertyName("load_duration")]
        public long? LoadDuration { get; set; }

        /// <summary>
        /// 提示评估计数
        /// </summary>
        [JsonPropertyName("prompt_eval_count")]
        public int? PromptEvalCount { get; set; }

        /// <summary>
        /// 提示评估持续时间（纳秒）
        /// </summary>
        [JsonPropertyName("prompt_eval_duration")]
        public long? PromptEvalDuration { get; set; }

        /// <summary>
        /// 评估计数
        /// </summary>
        [JsonPropertyName("eval_count")]
        public int? EvalCount { get; set; }

        /// <summary>
        /// 评估持续时间（纳秒）
        /// </summary>
        [JsonPropertyName("eval_duration")]
        public long? EvalDuration { get; set; }
    }

    /// <summary>
    /// Ollama模型选项
    /// </summary>
    public class OllamaOptions
    {
        /// <summary>
        /// 温度参数
        /// </summary>
        [JsonPropertyName("temperature")]
        public double? Temperature { get; set; }

        /// <summary>
        /// Top-p参数
        /// </summary>
        [JsonPropertyName("top_p")]
        public double? TopP { get; set; }

        /// <summary>
        /// Top-k参数
        /// </summary>
        [JsonPropertyName("top_k")]
        public int? TopK { get; set; }

        /// <summary>
        /// 重复惩罚
        /// </summary>
        [JsonPropertyName("repeat_penalty")]
        public double? RepeatPenalty { get; set; }

        /// <summary>
        /// 种子
        /// </summary>
        [JsonPropertyName("seed")]
        public int? Seed { get; set; }

        /// <summary>
        /// 最大令牌数
        /// </summary>
        [JsonPropertyName("num_predict")]
        public int? NumPredict { get; set; }

        /// <summary>
        /// 停止词
        /// </summary>
        [JsonPropertyName("stop")]
        public List<string>? Stop { get; set; }
    }

    /// <summary>
    /// Ollama模型列表响应
    /// </summary>
    public class OllamaModelsResponse
    {
        /// <summary>
        /// 模型列表
        /// </summary>
        [JsonPropertyName("models")]
        public List<OllamaModelInfo> Models { get; set; } = new();
    }

    /// <summary>
    /// Ollama模型信息
    /// </summary>
    public class OllamaModelInfo
    {
        /// <summary>
        /// 模型名称
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 修改时间
        /// </summary>
        [JsonPropertyName("modified_at")]
        public DateTime ModifiedAt { get; set; }

        /// <summary>
        /// 模型大小
        /// </summary>
        [JsonPropertyName("size")]
        public long Size { get; set; }

        /// <summary>
        /// 摘要
        /// </summary>
        [JsonPropertyName("digest")]
        public string Digest { get; set; } = string.Empty;

        /// <summary>
        /// 详细信息
        /// </summary>
        [JsonPropertyName("details")]
        public OllamaModelDetails? Details { get; set; }
    }

    /// <summary>
    /// Ollama模型详细信息
    /// </summary>
    public class OllamaModelDetails
    {
        /// <summary>
        /// 格式
        /// </summary>
        [JsonPropertyName("format")]
        public string Format { get; set; } = string.Empty;

        /// <summary>
        /// 系列
        /// </summary>
        [JsonPropertyName("family")]
        public string Family { get; set; } = string.Empty;

        /// <summary>
        /// 系列列表
        /// </summary>
        [JsonPropertyName("families")]
        public List<string>? Families { get; set; }

        /// <summary>
        /// 参数大小
        /// </summary>
        [JsonPropertyName("parameter_size")]
        public string ParameterSize { get; set; } = string.Empty;

        /// <summary>
        /// 量化级别
        /// </summary>
        [JsonPropertyName("quantization_level")]
        public string QuantizationLevel { get; set; } = string.Empty;
    }

    /// <summary>
    /// Ollama生成请求
    /// </summary>
    public class OllamaGenerateRequest
    {
        /// <summary>
        /// 模型名称
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// 提示
        /// </summary>
        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;

        /// <summary>
        /// 是否流式响应
        /// </summary>
        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = false;

        /// <summary>
        /// 模型选项
        /// </summary>
        [JsonPropertyName("options")]
        public OllamaOptions? Options { get; set; }

        /// <summary>
        /// 系统提示
        /// </summary>
        [JsonPropertyName("system")]
        public string? System { get; set; }

        /// <summary>
        /// 模板
        /// </summary>
        [JsonPropertyName("template")]
        public string? Template { get; set; }

        /// <summary>
        /// 上下文
        /// </summary>
        [JsonPropertyName("context")]
        public List<int>? Context { get; set; }

        /// <summary>
        /// 保持模型加载时间
        /// </summary>
        [JsonPropertyName("keep_alive")]
        public string? KeepAlive { get; set; }
    }

    /// <summary>
    /// Ollama生成响应
    /// </summary>
    public class OllamaGenerateResponse
    {
        /// <summary>
        /// 模型名称
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 响应内容
        /// </summary>
        [JsonPropertyName("response")]
        public string Response { get; set; } = string.Empty;

        /// <summary>
        /// 是否完成
        /// </summary>
        [JsonPropertyName("done")]
        public bool Done { get; set; }

        /// <summary>
        /// 上下文
        /// </summary>
        [JsonPropertyName("context")]
        public List<int>? Context { get; set; }

        /// <summary>
        /// 总持续时间（纳秒）
        /// </summary>
        [JsonPropertyName("total_duration")]
        public long? TotalDuration { get; set; }

        /// <summary>
        /// 加载持续时间（纳秒）
        /// </summary>
        [JsonPropertyName("load_duration")]
        public long? LoadDuration { get; set; }

        /// <summary>
        /// 提示评估计数
        /// </summary>
        [JsonPropertyName("prompt_eval_count")]
        public int? PromptEvalCount { get; set; }

        /// <summary>
        /// 提示评估持续时间（纳秒）
        /// </summary>
        [JsonPropertyName("prompt_eval_duration")]
        public long? PromptEvalDuration { get; set; }

        /// <summary>
        /// 评估计数
        /// </summary>
        [JsonPropertyName("eval_count")]
        public int? EvalCount { get; set; }

        /// <summary>
        /// 评估持续时间（纳秒）
        /// </summary>
        [JsonPropertyName("eval_duration")]
        public long? EvalDuration { get; set; }
    }

    /// <summary>
    /// Ollama错误响应
    /// </summary>
    public class OllamaErrorResponse
    {
        /// <summary>
        /// 错误信息
        /// </summary>
        [JsonPropertyName("error")]
        public string Error { get; set; } = string.Empty;
    }

    /// <summary>
    /// Ollama版本信息
    /// </summary>
    public class OllamaVersionResponse
    {
        /// <summary>
        /// 版本号
        /// </summary>
        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;
    }

    /// <summary>
    /// Ollama拉取模型请求
    /// </summary>
    public class OllamaPullRequest
    {
        /// <summary>
        /// 模型名称
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 是否流式响应
        /// </summary>
        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = true;
    }

    /// <summary>
    /// Ollama拉取模型响应
    /// </summary>
    public class OllamaPullResponse
    {
        /// <summary>
        /// 状态
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 摘要
        /// </summary>
        [JsonPropertyName("digest")]
        public string? Digest { get; set; }

        /// <summary>
        /// 总大小
        /// </summary>
        [JsonPropertyName("total")]
        public long? Total { get; set; }

        /// <summary>
        /// 已完成大小
        /// </summary>
        [JsonPropertyName("completed")]
        public long? Completed { get; set; }
    }
}
