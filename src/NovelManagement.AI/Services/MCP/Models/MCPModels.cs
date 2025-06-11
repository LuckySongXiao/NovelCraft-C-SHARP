using System.Text.Json.Serialization;

namespace NovelManagement.AI.Services.MCP.Models
{
    /// <summary>
    /// MCP协议版本
    /// </summary>
    public static class MCPVersion
    {
        public const string Current = "2024-11-05";
    }

    /// <summary>
    /// MCP消息基类
    /// </summary>
    public abstract class MCPMessage
    {
        /// <summary>
        /// JSON-RPC版本
        /// </summary>
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        /// <summary>
        /// 消息ID
        /// </summary>
        [JsonPropertyName("id")]
        public object? Id { get; set; }
    }

    /// <summary>
    /// MCP请求消息
    /// </summary>
    public class MCPRequest : MCPMessage
    {
        /// <summary>
        /// 方法名
        /// </summary>
        [JsonPropertyName("method")]
        public string Method { get; set; } = string.Empty;

        /// <summary>
        /// 参数
        /// </summary>
        [JsonPropertyName("params")]
        public object? Params { get; set; }
    }

    /// <summary>
    /// MCP响应消息
    /// </summary>
    public class MCPResponse : MCPMessage
    {
        /// <summary>
        /// 结果
        /// </summary>
        [JsonPropertyName("result")]
        public object? Result { get; set; }

        /// <summary>
        /// 错误
        /// </summary>
        [JsonPropertyName("error")]
        public MCPError? Error { get; set; }
    }

    /// <summary>
    /// MCP通知消息
    /// </summary>
    public class MCPNotification : MCPMessage
    {
        /// <summary>
        /// 方法名
        /// </summary>
        [JsonPropertyName("method")]
        public string Method { get; set; } = string.Empty;

        /// <summary>
        /// 参数
        /// </summary>
        [JsonPropertyName("params")]
        public object? Params { get; set; }

        public MCPNotification()
        {
            Id = null; // 通知消息没有ID
        }
    }

    /// <summary>
    /// MCP错误
    /// </summary>
    public class MCPError
    {
        /// <summary>
        /// 错误代码
        /// </summary>
        [JsonPropertyName("code")]
        public int Code { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 错误数据
        /// </summary>
        [JsonPropertyName("data")]
        public object? Data { get; set; }
    }

    /// <summary>
    /// MCP初始化请求
    /// </summary>
    public class MCPInitializeRequest
    {
        /// <summary>
        /// 协议版本
        /// </summary>
        [JsonPropertyName("protocolVersion")]
        public string ProtocolVersion { get; set; } = MCPVersion.Current;

        /// <summary>
        /// 客户端信息
        /// </summary>
        [JsonPropertyName("clientInfo")]
        public MCPClientInfo ClientInfo { get; set; } = new();

        /// <summary>
        /// 能力
        /// </summary>
        [JsonPropertyName("capabilities")]
        public MCPClientCapabilities Capabilities { get; set; } = new();
    }

    /// <summary>
    /// MCP初始化响应
    /// </summary>
    public class MCPInitializeResponse
    {
        /// <summary>
        /// 协议版本
        /// </summary>
        [JsonPropertyName("protocolVersion")]
        public string ProtocolVersion { get; set; } = MCPVersion.Current;

        /// <summary>
        /// 服务器信息
        /// </summary>
        [JsonPropertyName("serverInfo")]
        public MCPServerInfo ServerInfo { get; set; } = new();

        /// <summary>
        /// 能力
        /// </summary>
        [JsonPropertyName("capabilities")]
        public MCPServerCapabilities Capabilities { get; set; } = new();
    }

    /// <summary>
    /// MCP客户端信息
    /// </summary>
    public class MCPClientInfo
    {
        /// <summary>
        /// 客户端名称
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = "NovelManagement";

        /// <summary>
        /// 客户端版本
        /// </summary>
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0.0";
    }

    /// <summary>
    /// MCP服务器信息
    /// </summary>
    public class MCPServerInfo
    {
        /// <summary>
        /// 服务器名称
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 服务器版本
        /// </summary>
        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;
    }

    /// <summary>
    /// MCP客户端能力
    /// </summary>
    public class MCPClientCapabilities
    {
        /// <summary>
        /// 根能力
        /// </summary>
        [JsonPropertyName("roots")]
        public MCPRootsCapability? Roots { get; set; }

        /// <summary>
        /// 采样能力
        /// </summary>
        [JsonPropertyName("sampling")]
        public object? Sampling { get; set; }
    }

    /// <summary>
    /// MCP服务器能力
    /// </summary>
    public class MCPServerCapabilities
    {
        /// <summary>
        /// 日志能力
        /// </summary>
        [JsonPropertyName("logging")]
        public object? Logging { get; set; }

        /// <summary>
        /// 提示能力
        /// </summary>
        [JsonPropertyName("prompts")]
        public MCPPromptsCapability? Prompts { get; set; }

        /// <summary>
        /// 资源能力
        /// </summary>
        [JsonPropertyName("resources")]
        public MCPResourcesCapability? Resources { get; set; }

        /// <summary>
        /// 工具能力
        /// </summary>
        [JsonPropertyName("tools")]
        public MCPToolsCapability? Tools { get; set; }
    }

    /// <summary>
    /// MCP根能力
    /// </summary>
    public class MCPRootsCapability
    {
        /// <summary>
        /// 是否支持列表变更
        /// </summary>
        [JsonPropertyName("listChanged")]
        public bool ListChanged { get; set; }
    }

    /// <summary>
    /// MCP提示能力
    /// </summary>
    public class MCPPromptsCapability
    {
        /// <summary>
        /// 是否支持列表变更
        /// </summary>
        [JsonPropertyName("listChanged")]
        public bool ListChanged { get; set; }
    }

    /// <summary>
    /// MCP资源能力
    /// </summary>
    public class MCPResourcesCapability
    {
        /// <summary>
        /// 是否支持订阅
        /// </summary>
        [JsonPropertyName("subscribe")]
        public bool Subscribe { get; set; }

        /// <summary>
        /// 是否支持列表变更
        /// </summary>
        [JsonPropertyName("listChanged")]
        public bool ListChanged { get; set; }
    }

    /// <summary>
    /// MCP工具能力
    /// </summary>
    public class MCPToolsCapability
    {
        /// <summary>
        /// 是否支持列表变更
        /// </summary>
        [JsonPropertyName("listChanged")]
        public bool ListChanged { get; set; }
    }

    /// <summary>
    /// MCP工具
    /// </summary>
    public class MCPTool
    {
        /// <summary>
        /// 工具名称
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 工具描述
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 输入模式
        /// </summary>
        [JsonPropertyName("inputSchema")]
        public object? InputSchema { get; set; }
    }

    /// <summary>
    /// MCP工具调用请求
    /// </summary>
    public class MCPToolCallRequest
    {
        /// <summary>
        /// 工具名称
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 参数
        /// </summary>
        [JsonPropertyName("arguments")]
        public Dictionary<string, object> Arguments { get; set; } = new();
    }

    /// <summary>
    /// MCP工具调用响应
    /// </summary>
    public class MCPToolCallResponse
    {
        /// <summary>
        /// 内容列表
        /// </summary>
        [JsonPropertyName("content")]
        public List<MCPContent> Content { get; set; } = new();

        /// <summary>
        /// 是否错误
        /// </summary>
        [JsonPropertyName("isError")]
        public bool IsError { get; set; }
    }

    /// <summary>
    /// MCP内容
    /// </summary>
    public class MCPContent
    {
        /// <summary>
        /// 内容类型
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 文本内容
        /// </summary>
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        /// <summary>
        /// 数据
        /// </summary>
        [JsonPropertyName("data")]
        public string? Data { get; set; }

        /// <summary>
        /// MIME类型
        /// </summary>
        [JsonPropertyName("mimeType")]
        public string? MimeType { get; set; }
    }

    /// <summary>
    /// MCP采样请求
    /// </summary>
    public class MCPSamplingRequest
    {
        /// <summary>
        /// 消息列表
        /// </summary>
        [JsonPropertyName("messages")]
        public List<MCPMessage> Messages { get; set; } = new();

        /// <summary>
        /// 模型偏好
        /// </summary>
        [JsonPropertyName("modelPreferences")]
        public MCPModelPreferences? ModelPreferences { get; set; }

        /// <summary>
        /// 系统提示
        /// </summary>
        [JsonPropertyName("systemPrompt")]
        public string? SystemPrompt { get; set; }

        /// <summary>
        /// 包含上下文
        /// </summary>
        [JsonPropertyName("includeContext")]
        public string? IncludeContext { get; set; }

        /// <summary>
        /// 温度
        /// </summary>
        [JsonPropertyName("temperature")]
        public double? Temperature { get; set; }

        /// <summary>
        /// 最大令牌数
        /// </summary>
        [JsonPropertyName("maxTokens")]
        public int? MaxTokens { get; set; }

        /// <summary>
        /// 停止序列
        /// </summary>
        [JsonPropertyName("stopSequences")]
        public List<string>? StopSequences { get; set; }

        /// <summary>
        /// 元数据
        /// </summary>
        [JsonPropertyName("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// MCP模型偏好
    /// </summary>
    public class MCPModelPreferences
    {
        /// <summary>
        /// 成本优先级
        /// </summary>
        [JsonPropertyName("costPriority")]
        public double? CostPriority { get; set; }

        /// <summary>
        /// 速度优先级
        /// </summary>
        [JsonPropertyName("speedPriority")]
        public double? SpeedPriority { get; set; }

        /// <summary>
        /// 智能优先级
        /// </summary>
        [JsonPropertyName("intelligencePriority")]
        public double? IntelligencePriority { get; set; }
    }
}
