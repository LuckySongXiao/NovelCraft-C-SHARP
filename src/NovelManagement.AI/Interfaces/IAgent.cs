using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NovelManagement.AI.Interfaces
{
    /// <summary>
    /// Agent状态枚举
    /// </summary>
    public enum AgentStatus
    {
        /// <summary>
        /// 空闲状态
        /// </summary>
        Idle,
        
        /// <summary>
        /// 工作中
        /// </summary>
        Working,
        
        /// <summary>
        /// 等待中
        /// </summary>
        Waiting,
        
        /// <summary>
        /// 错误状态
        /// </summary>
        Error,
        
        /// <summary>
        /// 离线状态
        /// </summary>
        Offline
    }

    /// <summary>
    /// Agent能力描述
    /// </summary>
    public class AgentCapability
    {
        /// <summary>
        /// 能力名称
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// 能力描述
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// 是否可用
        /// </summary>
        public bool IsAvailable { get; set; }
        
        /// <summary>
        /// 优先级
        /// </summary>
        public int Priority { get; set; }
    }

    /// <summary>
    /// Agent状态信息
    /// </summary>
    public class AgentStatusInfo
    {
        /// <summary>
        /// Agent ID
        /// </summary>
        public string AgentId { get; set; } = string.Empty;
        
        /// <summary>
        /// Agent名称
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// 当前状态
        /// </summary>
        public AgentStatus Status { get; set; }
        
        /// <summary>
        /// 状态描述
        /// </summary>
        public string StatusDescription { get; set; } = string.Empty;
        
        /// <summary>
        /// 当前任务
        /// </summary>
        public string? CurrentTask { get; set; }
        
        /// <summary>
        /// 任务进度 (0-100)
        /// </summary>
        public int Progress { get; set; }
        
        /// <summary>
        /// 最后活动时间
        /// </summary>
        public DateTime LastActivity { get; set; }
        
        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Agent任务结果
    /// </summary>
    public class AgentTaskResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }
        
        /// <summary>
        /// 结果数据
        /// </summary>
        public object? Data { get; set; }
        
        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// 执行时间
        /// </summary>
        public TimeSpan ExecutionTime { get; set; }
        
        /// <summary>
        /// 附加信息
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Agent基础接口
    /// </summary>
    public interface IAgent
    {
        /// <summary>
        /// Agent唯一标识
        /// </summary>
        string Id { get; }
        
        /// <summary>
        /// Agent名称
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Agent描述
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// Agent版本
        /// </summary>
        string Version { get; }
        
        /// <summary>
        /// 当前状态
        /// </summary>
        AgentStatus Status { get; }
        
        /// <summary>
        /// 状态变化事件
        /// </summary>
        event EventHandler<AgentStatusInfo>? StatusChanged;
        
        /// <summary>
        /// 任务完成事件
        /// </summary>
        event EventHandler<AgentTaskResult>? TaskCompleted;
        
        /// <summary>
        /// 执行任务
        /// </summary>
        /// <param name="taskType">任务类型</param>
        /// <param name="parameters">任务参数</param>
        /// <returns>任务结果</returns>
        Task<AgentTaskResult> ExecuteAsync(string taskType, Dictionary<string, object> parameters);
        
        /// <summary>
        /// 获取Agent能力列表
        /// </summary>
        /// <returns>能力列表</returns>
        Task<List<AgentCapability>> GetCapabilitiesAsync();
        
        /// <summary>
        /// 获取Agent状态信息
        /// </summary>
        /// <returns>状态信息</returns>
        Task<AgentStatusInfo> GetStatusAsync();
        
        /// <summary>
        /// 初始化Agent
        /// </summary>
        /// <param name="configuration">配置参数</param>
        /// <returns>初始化结果</returns>
        Task<bool> InitializeAsync(Dictionary<string, object> configuration);
        
        /// <summary>
        /// 停止Agent
        /// </summary>
        /// <returns>停止结果</returns>
        Task<bool> StopAsync();
        
        /// <summary>
        /// 重置Agent状态
        /// </summary>
        /// <returns>重置结果</returns>
        Task<bool> ResetAsync();
        
        /// <summary>
        /// 检查Agent健康状态
        /// </summary>
        /// <returns>健康状态</returns>
        Task<bool> HealthCheckAsync();
    }
}
