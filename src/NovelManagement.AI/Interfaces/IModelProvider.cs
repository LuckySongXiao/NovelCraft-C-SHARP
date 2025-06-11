using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NovelManagement.AI.Interfaces
{
    /// <summary>
    /// AI模型提供者接口
    /// </summary>
    public interface IModelProvider
    {
        /// <summary>
        /// 提供者名称
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// 提供者类型
        /// </summary>
        ModelProviderType ProviderType { get; }

        /// <summary>
        /// 是否可用
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// 配置变更事件
        /// </summary>
        event EventHandler<ModelConfigurationChangedEventArgs>? ConfigurationChanged;

        /// <summary>
        /// 连接状态变更事件
        /// </summary>
        event EventHandler<ConnectionStatusChangedEventArgs>? ConnectionStatusChanged;

        /// <summary>
        /// 初始化提供者
        /// </summary>
        /// <param name="configuration">配置信息</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>初始化结果</returns>
        Task<bool> InitializeAsync(IModelConfiguration configuration, CancellationToken cancellationToken = default);

        /// <summary>
        /// 测试连接
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>连接测试结果</returns>
        Task<ConnectionTestResult> TestConnectionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取可用模型列表
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>模型列表</returns>
        Task<List<ModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 发送聊天请求
        /// </summary>
        /// <param name="request">聊天请求</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>聊天响应</returns>
        Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// 发送流式聊天请求
        /// </summary>
        /// <param name="request">聊天请求</param>
        /// <param name="onChunkReceived">数据块接收回调</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>聊天响应</returns>
        Task<ChatResponse> ChatStreamAsync(ChatRequest request, Action<ChatChunk> onChunkReceived, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取提供者统计信息
        /// </summary>
        /// <returns>统计信息</returns>
        Task<ProviderStatistics> GetStatisticsAsync();

        /// <summary>
        /// 释放资源
        /// </summary>
        void Dispose();
    }

    /// <summary>
    /// 模型提供者类型
    /// </summary>
    public enum ModelProviderType
    {
        /// <summary>
        /// 云端API
        /// </summary>
        CloudAPI,

        /// <summary>
        /// 本地模型
        /// </summary>
        Local,

        /// <summary>
        /// MCP协议
        /// </summary>
        MCP
    }

    /// <summary>
    /// 模型配置接口
    /// </summary>
    public interface IModelConfiguration
    {
        /// <summary>
        /// 提供者名称
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// 验证配置
        /// </summary>
        /// <returns>验证结果</returns>
        bool IsValid();

        /// <summary>
        /// 获取验证错误
        /// </summary>
        /// <returns>错误列表</returns>
        List<string> GetValidationErrors();
    }

    /// <summary>
    /// 模型信息
    /// </summary>
    public class ModelInfo
    {
        /// <summary>
        /// 模型ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 模型名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 模型描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 模型大小（字节）
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// 是否已下载
        /// </summary>
        public bool IsDownloaded { get; set; }

        /// <summary>
        /// 支持的功能
        /// </summary>
        public List<string> Capabilities { get; set; } = new();

        /// <summary>
        /// 模型参数
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// 聊天请求
    /// </summary>
    public class ChatRequest
    {
        /// <summary>
        /// 模型ID
        /// </summary>
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// 消息列表
        /// </summary>
        public List<ChatMessage> Messages { get; set; } = new();

        /// <summary>
        /// 温度参数
        /// </summary>
        public double Temperature { get; set; } = 0.7;

        /// <summary>
        /// 最大令牌数
        /// </summary>
        public int MaxTokens { get; set; } = 1000;

        /// <summary>
        /// 是否流式响应
        /// </summary>
        public bool Stream { get; set; } = false;

        /// <summary>
        /// 系统提示
        /// </summary>
        public string? SystemPrompt { get; set; }

        /// <summary>
        /// 额外参数
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// 聊天消息
    /// </summary>
    public class ChatMessage
    {
        /// <summary>
        /// 角色
        /// </summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// 内容
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 聊天响应
    /// </summary>
    public class ChatResponse
    {
        /// <summary>
        /// 响应ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 模型
        /// </summary>
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// 响应内容
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 完成原因
        /// </summary>
        public string FinishReason { get; set; } = string.Empty;

        /// <summary>
        /// 使用统计
        /// </summary>
        public TokenUsage? Usage { get; set; }

        /// <summary>
        /// 响应时间
        /// </summary>
        public TimeSpan ResponseTime { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; } = true;

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 聊天数据块
    /// </summary>
    public class ChatChunk
    {
        /// <summary>
        /// 数据块ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 内容
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 是否完成
        /// </summary>
        public bool IsComplete { get; set; }

        /// <summary>
        /// 完成原因
        /// </summary>
        public string? FinishReason { get; set; }
    }

    /// <summary>
    /// 令牌使用统计
    /// </summary>
    public class TokenUsage
    {
        /// <summary>
        /// 提示令牌数
        /// </summary>
        public int PromptTokens { get; set; }

        /// <summary>
        /// 完成令牌数
        /// </summary>
        public int CompletionTokens { get; set; }

        /// <summary>
        /// 总令牌数
        /// </summary>
        public int TotalTokens { get; set; }
    }

    /// <summary>
    /// 连接测试结果
    /// </summary>
    public class ConnectionTestResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 响应时间
        /// </summary>
        public TimeSpan ResponseTime { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 服务器信息
        /// </summary>
        public Dictionary<string, object> ServerInfo { get; set; } = new();
    }

    /// <summary>
    /// 提供者统计信息
    /// </summary>
    public class ProviderStatistics
    {
        /// <summary>
        /// 总请求数
        /// </summary>
        public long TotalRequests { get; set; }

        /// <summary>
        /// 成功请求数
        /// </summary>
        public long SuccessfulRequests { get; set; }

        /// <summary>
        /// 失败请求数
        /// </summary>
        public long FailedRequests { get; set; }

        /// <summary>
        /// 平均响应时间
        /// </summary>
        public TimeSpan AverageResponseTime { get; set; }

        /// <summary>
        /// 总令牌使用量
        /// </summary>
        public long TotalTokensUsed { get; set; }

        /// <summary>
        /// 最后请求时间
        /// </summary>
        public DateTime? LastRequestTime { get; set; }
    }

    /// <summary>
    /// 配置变更事件参数
    /// </summary>
    public class ModelConfigurationChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 新配置
        /// </summary>
        public IModelConfiguration NewConfiguration { get; }

        /// <summary>
        /// 旧配置
        /// </summary>
        public IModelConfiguration? OldConfiguration { get; }

        public ModelConfigurationChangedEventArgs(IModelConfiguration newConfiguration, IModelConfiguration? oldConfiguration = null)
        {
            NewConfiguration = newConfiguration;
            OldConfiguration = oldConfiguration;
        }
    }

    /// <summary>
    /// 连接状态变更事件参数
    /// </summary>
    public class ConnectionStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 是否连接
        /// </summary>
        public bool IsConnected { get; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; }

        public ConnectionStatusChangedEventArgs(bool isConnected, string? errorMessage = null)
        {
            IsConnected = isConnected;
            ErrorMessage = errorMessage;
        }
    }
}
