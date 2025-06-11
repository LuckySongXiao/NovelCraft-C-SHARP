using NovelManagement.AI.Interfaces;
using NovelManagement.AI.Services.MCP.Models;

namespace NovelManagement.AI.Services.MCP
{
    /// <summary>
    /// MCP服务接口
    /// </summary>
    public interface IMCPService : IModelProvider
    {
        /// <summary>
        /// 连接状态变更事件
        /// </summary>
        event EventHandler<MCPConnectionStatusChangedEventArgs>? MCPConnectionStatusChanged;

        /// <summary>
        /// 消息接收事件
        /// </summary>
        event EventHandler<MCPMessageReceivedEventArgs>? MessageReceived;

        /// <summary>
        /// 错误事件
        /// </summary>
        event EventHandler<MCPErrorEventArgs>? ErrorOccurred;

        /// <summary>
        /// 获取当前配置
        /// </summary>
        /// <returns>配置信息</returns>
        MCPConfiguration GetConfiguration();

        /// <summary>
        /// 更新配置
        /// </summary>
        /// <param name="configuration">新配置</param>
        /// <returns>是否成功</returns>
        Task<bool> UpdateConfigurationAsync(MCPConfiguration configuration);

        /// <summary>
        /// 连接到MCP服务器
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>连接结果</returns>
        Task<bool> ConnectAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 断开连接
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>断开结果</returns>
        Task<bool> DisconnectAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 初始化MCP会话
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>初始化结果</returns>
        Task<MCPInitializeResponse?> InitializeSessionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 发送MCP请求
        /// </summary>
        /// <param name="request">请求</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>响应</returns>
        Task<MCPResponse?> SendRequestAsync(MCPRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// 发送MCP通知
        /// </summary>
        /// <param name="notification">通知</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>发送结果</returns>
        Task<bool> SendNotificationAsync(MCPNotification notification, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取可用工具列表
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>工具列表</returns>
        Task<List<MCPTool>> GetToolsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 调用工具
        /// </summary>
        /// <param name="toolCall">工具调用请求</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>工具调用响应</returns>
        Task<MCPToolCallResponse?> CallToolAsync(MCPToolCallRequest toolCall, CancellationToken cancellationToken = default);

        /// <summary>
        /// 发送采样请求
        /// </summary>
        /// <param name="samplingRequest">采样请求</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>采样响应</returns>
        Task<ChatResponse> SampleAsync(MCPSamplingRequest samplingRequest, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取服务器能力
        /// </summary>
        /// <returns>服务器能力</returns>
        MCPServerCapabilities? GetServerCapabilities();

        /// <summary>
        /// 获取连接状态
        /// </summary>
        /// <returns>连接状态</returns>
        MCPConnectionStatus GetConnectionStatus();

        /// <summary>
        /// 获取会话信息
        /// </summary>
        /// <returns>会话信息</returns>
        MCPSessionInfo? GetSessionInfo();

        /// <summary>
        /// 设置上下文
        /// </summary>
        /// <param name="context">上下文数据</param>
        /// <returns>设置结果</returns>
        Task<bool> SetContextAsync(Dictionary<string, object> context);

        /// <summary>
        /// 获取上下文
        /// </summary>
        /// <returns>上下文数据</returns>
        Dictionary<string, object> GetContext();

        /// <summary>
        /// 清除上下文
        /// </summary>
        /// <returns>清除结果</returns>
        Task<bool> ClearContextAsync();
    }

    /// <summary>
    /// MCP连接状态变更事件参数
    /// </summary>
    public class MCPConnectionStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 连接状态
        /// </summary>
        public MCPConnectionStatus Status { get; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; }

        public MCPConnectionStatusChangedEventArgs(MCPConnectionStatus status, string? errorMessage = null)
        {
            Status = status;
            ErrorMessage = errorMessage;
        }
    }

    /// <summary>
    /// MCP消息接收事件参数
    /// </summary>
    public class MCPMessageReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// 消息
        /// </summary>
        public MCPMessage Message { get; }

        /// <summary>
        /// 原始数据
        /// </summary>
        public string RawData { get; }

        public MCPMessageReceivedEventArgs(MCPMessage message, string rawData)
        {
            Message = message;
            RawData = rawData;
        }
    }

    /// <summary>
    /// MCP错误事件参数
    /// </summary>
    public class MCPErrorEventArgs : EventArgs
    {
        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// 异常
        /// </summary>
        public Exception? Exception { get; }

        /// <summary>
        /// 错误代码
        /// </summary>
        public int? ErrorCode { get; }

        public MCPErrorEventArgs(string errorMessage, Exception? exception = null, int? errorCode = null)
        {
            ErrorMessage = errorMessage;
            Exception = exception;
            ErrorCode = errorCode;
        }
    }

    /// <summary>
    /// MCP连接状态
    /// </summary>
    public enum MCPConnectionStatus
    {
        /// <summary>
        /// 已断开
        /// </summary>
        Disconnected,

        /// <summary>
        /// 连接中
        /// </summary>
        Connecting,

        /// <summary>
        /// 已连接
        /// </summary>
        Connected,

        /// <summary>
        /// 初始化中
        /// </summary>
        Initializing,

        /// <summary>
        /// 已初始化
        /// </summary>
        Initialized,

        /// <summary>
        /// 断开连接中
        /// </summary>
        Disconnecting,

        /// <summary>
        /// 重连中
        /// </summary>
        Reconnecting,

        /// <summary>
        /// 错误状态
        /// </summary>
        Error
    }

    /// <summary>
    /// MCP会话信息
    /// </summary>
    public class MCPSessionInfo
    {
        /// <summary>
        /// 会话ID
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// 协议版本
        /// </summary>
        public string ProtocolVersion { get; set; } = string.Empty;

        /// <summary>
        /// 服务器信息
        /// </summary>
        public MCPServerInfo? ServerInfo { get; set; }

        /// <summary>
        /// 服务器能力
        /// </summary>
        public MCPServerCapabilities? ServerCapabilities { get; set; }

        /// <summary>
        /// 客户端能力
        /// </summary>
        public MCPClientCapabilities? ClientCapabilities { get; set; }

        /// <summary>
        /// 连接时间
        /// </summary>
        public DateTime ConnectedAt { get; set; }

        /// <summary>
        /// 初始化时间
        /// </summary>
        public DateTime? InitializedAt { get; set; }

        /// <summary>
        /// 最后活动时间
        /// </summary>
        public DateTime LastActivityAt { get; set; }

        /// <summary>
        /// 发送的消息数
        /// </summary>
        public long MessagesSent { get; set; }

        /// <summary>
        /// 接收的消息数
        /// </summary>
        public long MessagesReceived { get; set; }

        /// <summary>
        /// 错误计数
        /// </summary>
        public long ErrorCount { get; set; }

        /// <summary>
        /// 重连次数
        /// </summary>
        public int ReconnectCount { get; set; }

        /// <summary>
        /// 自定义属性
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    /// <summary>
    /// MCP错误代码
    /// </summary>
    public static class MCPErrorCodes
    {
        /// <summary>
        /// 解析错误
        /// </summary>
        public const int ParseError = -32700;

        /// <summary>
        /// 无效请求
        /// </summary>
        public const int InvalidRequest = -32600;

        /// <summary>
        /// 方法未找到
        /// </summary>
        public const int MethodNotFound = -32601;

        /// <summary>
        /// 无效参数
        /// </summary>
        public const int InvalidParams = -32602;

        /// <summary>
        /// 内部错误
        /// </summary>
        public const int InternalError = -32603;

        /// <summary>
        /// 服务器错误范围开始
        /// </summary>
        public const int ServerErrorStart = -32099;

        /// <summary>
        /// 服务器错误范围结束
        /// </summary>
        public const int ServerErrorEnd = -32000;
    }
}
