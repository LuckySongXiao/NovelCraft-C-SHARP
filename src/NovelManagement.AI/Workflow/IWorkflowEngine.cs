using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NovelManagement.AI.Interfaces;

namespace NovelManagement.AI.Workflow
{
    /// <summary>
    /// 工作流状态枚举
    /// </summary>
    public enum WorkflowStatus
    {
        /// <summary>
        /// 待执行
        /// </summary>
        Pending,
        
        /// <summary>
        /// 执行中
        /// </summary>
        Running,
        
        /// <summary>
        /// 已完成
        /// </summary>
        Completed,
        
        /// <summary>
        /// 已暂停
        /// </summary>
        Paused,
        
        /// <summary>
        /// 已取消
        /// </summary>
        Cancelled,
        
        /// <summary>
        /// 执行失败
        /// </summary>
        Failed
    }

    /// <summary>
    /// 工作流任务
    /// </summary>
    public class WorkflowTask
    {
        /// <summary>
        /// 任务ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// 任务名称
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// 任务类型
        /// </summary>
        public string TaskType { get; set; } = string.Empty;
        
        /// <summary>
        /// 目标Agent ID
        /// </summary>
        public string TargetAgentId { get; set; } = string.Empty;
        
        /// <summary>
        /// 任务参数
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new();
        
        /// <summary>
        /// 任务状态
        /// </summary>
        public WorkflowStatus Status { get; set; } = WorkflowStatus.Pending;
        
        /// <summary>
        /// 任务进度 (0-100)
        /// </summary>
        public int Progress { get; set; } = 0;
        
        /// <summary>
        /// 任务结果
        /// </summary>
        public AgentTaskResult? Result { get; set; }
        
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime? StartedAt { get; set; }
        
        /// <summary>
        /// 完成时间
        /// </summary>
        public DateTime? CompletedAt { get; set; }
        
        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// 依赖的任务ID列表
        /// </summary>
        public List<string> Dependencies { get; set; } = new();
        
        /// <summary>
        /// 优先级 (1-10, 10最高)
        /// </summary>
        public int Priority { get; set; } = 5;
    }

    /// <summary>
    /// 工作流定义
    /// </summary>
    public class WorkflowDefinition
    {
        /// <summary>
        /// 工作流ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// 工作流名称
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// 工作流描述
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// 工作流版本
        /// </summary>
        public string Version { get; set; } = "1.0.0";
        
        /// <summary>
        /// 任务列表
        /// </summary>
        public List<WorkflowTask> Tasks { get; set; } = new();
        
        /// <summary>
        /// 工作流状态
        /// </summary>
        public WorkflowStatus Status { get; set; } = WorkflowStatus.Pending;
        
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime? StartedAt { get; set; }
        
        /// <summary>
        /// 完成时间
        /// </summary>
        public DateTime? CompletedAt { get; set; }
        
        /// <summary>
        /// 总进度 (0-100)
        /// </summary>
        public int Progress { get; set; } = 0;
        
        /// <summary>
        /// 工作流配置
        /// </summary>
        public Dictionary<string, object> Configuration { get; set; } = new();
    }

    /// <summary>
    /// 工作流执行结果
    /// </summary>
    public class WorkflowExecutionResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }
        
        /// <summary>
        /// 工作流ID
        /// </summary>
        public string WorkflowId { get; set; } = string.Empty;
        
        /// <summary>
        /// 执行时间
        /// </summary>
        public TimeSpan ExecutionTime { get; set; }
        
        /// <summary>
        /// 完成的任务数量
        /// </summary>
        public int CompletedTasks { get; set; }
        
        /// <summary>
        /// 失败的任务数量
        /// </summary>
        public int FailedTasks { get; set; }
        
        /// <summary>
        /// 任务结果列表
        /// </summary>
        public List<AgentTaskResult> TaskResults { get; set; } = new();
        
        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// 附加信息
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// 工作流引擎接口
    /// </summary>
    public interface IWorkflowEngine
    {
        /// <summary>
        /// 工作流状态变化事件
        /// </summary>
        event EventHandler<WorkflowDefinition>? WorkflowStatusChanged;
        
        /// <summary>
        /// 任务状态变化事件
        /// </summary>
        event EventHandler<WorkflowTask>? TaskStatusChanged;
        
        /// <summary>
        /// 执行工作流
        /// </summary>
        /// <param name="workflowDefinition">工作流定义</param>
        /// <returns>执行结果</returns>
        Task<WorkflowExecutionResult> ExecuteWorkflowAsync(WorkflowDefinition workflowDefinition);
        
        /// <summary>
        /// 暂停工作流
        /// </summary>
        /// <param name="workflowId">工作流ID</param>
        /// <returns>是否成功</returns>
        Task<bool> PauseWorkflowAsync(string workflowId);
        
        /// <summary>
        /// 恢复工作流
        /// </summary>
        /// <param name="workflowId">工作流ID</param>
        /// <returns>是否成功</returns>
        Task<bool> ResumeWorkflowAsync(string workflowId);
        
        /// <summary>
        /// 取消工作流
        /// </summary>
        /// <param name="workflowId">工作流ID</param>
        /// <returns>是否成功</returns>
        Task<bool> CancelWorkflowAsync(string workflowId);
        
        /// <summary>
        /// 获取工作流状态
        /// </summary>
        /// <param name="workflowId">工作流ID</param>
        /// <returns>工作流定义</returns>
        Task<WorkflowDefinition?> GetWorkflowStatusAsync(string workflowId);
        
        /// <summary>
        /// 获取活动工作流列表
        /// </summary>
        /// <returns>工作流列表</returns>
        Task<List<WorkflowDefinition>> GetActiveWorkflowsAsync();
        
        /// <summary>
        /// 注册Agent
        /// </summary>
        /// <param name="agent">Agent实例</param>
        /// <returns>是否成功</returns>
        Task<bool> RegisterAgentAsync(IAgent agent);
        
        /// <summary>
        /// 注销Agent
        /// </summary>
        /// <param name="agentId">Agent ID</param>
        /// <returns>是否成功</returns>
        Task<bool> UnregisterAgentAsync(string agentId);
        
        /// <summary>
        /// 获取已注册的Agent列表
        /// </summary>
        /// <returns>Agent列表</returns>
        Task<List<IAgent>> GetRegisteredAgentsAsync();
        
        /// <summary>
        /// 创建预定义工作流
        /// </summary>
        /// <param name="workflowType">工作流类型</param>
        /// <param name="parameters">参数</param>
        /// <returns>工作流定义</returns>
        Task<WorkflowDefinition> CreatePredefinedWorkflowAsync(string workflowType, Dictionary<string, object> parameters);
    }
}
