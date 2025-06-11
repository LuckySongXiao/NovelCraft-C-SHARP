using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NovelManagement.AI.Interfaces;

namespace NovelManagement.AI.Workflow
{
    /// <summary>
    /// 小说工作流引擎
    /// </summary>
    public class NovelWorkflowEngine : IWorkflowEngine
    {
        private readonly ILogger<NovelWorkflowEngine> _logger;
        private readonly TaskQueue _taskQueue;
        private readonly ConcurrentDictionary<string, IAgent> _registeredAgents;
        private readonly ConcurrentDictionary<string, WorkflowDefinition> _activeWorkflows;
        private readonly IMemoryManager? _memoryManager;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="taskQueue">任务队列</param>
        /// <param name="memoryManager">记忆管理器（可选）</param>
        public NovelWorkflowEngine(ILogger<NovelWorkflowEngine> logger, TaskQueue taskQueue, IMemoryManager? memoryManager = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
            _memoryManager = memoryManager;
            _registeredAgents = new ConcurrentDictionary<string, IAgent>();
            _activeWorkflows = new ConcurrentDictionary<string, WorkflowDefinition>();

            // 订阅任务状态变化事件
            _taskQueue.TaskStatusChanged += OnTaskStatusChanged;
        }

        #region 事件

        /// <inheritdoc/>
        public event EventHandler<WorkflowDefinition>? WorkflowStatusChanged;

        /// <inheritdoc/>
        public event EventHandler<WorkflowTask>? TaskStatusChanged;

        #endregion

        #region 工作流管理

        /// <inheritdoc/>
        public async Task<WorkflowExecutionResult> ExecuteWorkflowAsync(WorkflowDefinition workflowDefinition)
        {
            if (workflowDefinition == null)
                throw new ArgumentNullException(nameof(workflowDefinition));

            var startTime = DateTime.Now;
            
            try
            {
                _logger.LogInformation($"开始执行工作流: {workflowDefinition.Name} (ID: {workflowDefinition.Id})");

                // 更新工作流状态
                workflowDefinition.Status = WorkflowStatus.Running;
                workflowDefinition.StartedAt = DateTime.Now;
                _activeWorkflows.TryAdd(workflowDefinition.Id, workflowDefinition);

                OnWorkflowStatusChanged(workflowDefinition);

                // 验证任务的Agent是否已注册
                var validationResult = ValidateWorkflowTasks(workflowDefinition);
                if (!validationResult.IsValid)
                {
                    throw new InvalidOperationException($"工作流验证失败: {validationResult.ErrorMessage}");
                }

                // 将任务加入队列
                _taskQueue.EnqueueTasks(workflowDefinition.Tasks);

                // 等待所有任务完成
                await WaitForWorkflowCompletionAsync(workflowDefinition);

                // 计算执行结果
                var executionTime = DateTime.Now - startTime;
                var completedTasks = workflowDefinition.Tasks.Count(t => t.Status == WorkflowStatus.Completed);
                var failedTasks = workflowDefinition.Tasks.Count(t => t.Status == WorkflowStatus.Failed);

                // 更新工作流状态
                workflowDefinition.Status = failedTasks > 0 ? WorkflowStatus.Failed : WorkflowStatus.Completed;
                workflowDefinition.CompletedAt = DateTime.Now;
                workflowDefinition.Progress = 100;

                OnWorkflowStatusChanged(workflowDefinition);

                var result = new WorkflowExecutionResult
                {
                    IsSuccess = failedTasks == 0,
                    WorkflowId = workflowDefinition.Id,
                    ExecutionTime = executionTime,
                    CompletedTasks = completedTasks,
                    FailedTasks = failedTasks,
                    TaskResults = workflowDefinition.Tasks.Where(t => t.Result != null).Select(t => t.Result!).ToList()
                };

                _logger.LogInformation($"工作流执行完成: {workflowDefinition.Name}, 成功: {result.IsSuccess}, 耗时: {executionTime.TotalSeconds:F2}秒");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"工作流执行失败: {workflowDefinition.Name}");

                workflowDefinition.Status = WorkflowStatus.Failed;
                workflowDefinition.CompletedAt = DateTime.Now;
                OnWorkflowStatusChanged(workflowDefinition);

                return new WorkflowExecutionResult
                {
                    IsSuccess = false,
                    WorkflowId = workflowDefinition.Id,
                    ExecutionTime = DateTime.Now - startTime,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <inheritdoc/>
        public async Task<bool> PauseWorkflowAsync(string workflowId)
        {
            if (_activeWorkflows.TryGetValue(workflowId, out var workflow))
            {
                workflow.Status = WorkflowStatus.Paused;
                OnWorkflowStatusChanged(workflow);
                
                _logger.LogInformation($"工作流已暂停: {workflow.Name} (ID: {workflowId})");
                return await Task.FromResult(true);
            }

            return false;
        }

        /// <inheritdoc/>
        public async Task<bool> ResumeWorkflowAsync(string workflowId)
        {
            if (_activeWorkflows.TryGetValue(workflowId, out var workflow))
            {
                workflow.Status = WorkflowStatus.Running;
                OnWorkflowStatusChanged(workflow);
                
                _logger.LogInformation($"工作流已恢复: {workflow.Name} (ID: {workflowId})");
                return await Task.FromResult(true);
            }

            return false;
        }

        /// <inheritdoc/>
        public async Task<bool> CancelWorkflowAsync(string workflowId)
        {
            if (_activeWorkflows.TryGetValue(workflowId, out var workflow))
            {
                workflow.Status = WorkflowStatus.Cancelled;
                workflow.CompletedAt = DateTime.Now;
                
                // 取消所有相关任务
                foreach (var task in workflow.Tasks.Where(t => t.Status == WorkflowStatus.Pending || t.Status == WorkflowStatus.Running))
                {
                    _taskQueue.CancelTask(task.Id);
                }

                OnWorkflowStatusChanged(workflow);
                
                _logger.LogInformation($"工作流已取消: {workflow.Name} (ID: {workflowId})");
                return await Task.FromResult(true);
            }

            return false;
        }

        /// <inheritdoc/>
        public async Task<WorkflowDefinition?> GetWorkflowStatusAsync(string workflowId)
        {
            _activeWorkflows.TryGetValue(workflowId, out var workflow);
            return await Task.FromResult(workflow);
        }

        /// <inheritdoc/>
        public async Task<List<WorkflowDefinition>> GetActiveWorkflowsAsync()
        {
            return await Task.FromResult(_activeWorkflows.Values.ToList());
        }

        #endregion

        #region Agent管理

        /// <inheritdoc/>
        public async Task<bool> RegisterAgentAsync(IAgent agent)
        {
            if (agent == null)
                throw new ArgumentNullException(nameof(agent));

            var success = _registeredAgents.TryAdd(agent.Id, agent);
            
            if (success)
            {
                _logger.LogInformation($"Agent已注册: {agent.Name} (ID: {agent.Id})");
                
                // 订阅Agent事件
                agent.StatusChanged += OnAgentStatusChanged;
                agent.TaskCompleted += OnAgentTaskCompleted;
            }

            return await Task.FromResult(success);
        }

        /// <inheritdoc/>
        public async Task<bool> UnregisterAgentAsync(string agentId)
        {
            var success = _registeredAgents.TryRemove(agentId, out var agent);
            
            if (success && agent != null)
            {
                _logger.LogInformation($"Agent已注销: {agent.Name} (ID: {agentId})");
                
                // 取消订阅Agent事件
                agent.StatusChanged -= OnAgentStatusChanged;
                agent.TaskCompleted -= OnAgentTaskCompleted;
            }

            return await Task.FromResult(success);
        }

        /// <inheritdoc/>
        public async Task<List<IAgent>> GetRegisteredAgentsAsync()
        {
            return await Task.FromResult(_registeredAgents.Values.ToList());
        }

        #endregion

        #region 预定义工作流

        /// <inheritdoc/>
        public async Task<WorkflowDefinition> CreatePredefinedWorkflowAsync(string workflowType, Dictionary<string, object> parameters)
        {
            return workflowType switch
            {
                "ProjectInitialization" => await CreateProjectInitializationWorkflowAsync(parameters),
                "ChapterCreation" => await CreateChapterCreationWorkflowAsync(parameters),
                "ContentReview" => await CreateContentReviewWorkflowAsync(parameters),
                "ConsistencyCheck" => await CreateConsistencyCheckWorkflowAsync(parameters),
                _ => throw new ArgumentException($"不支持的工作流类型: {workflowType}")
            };
        }

        /// <summary>
        /// 创建项目初始化工作流
        /// </summary>
        /// <param name="parameters">参数</param>
        /// <returns>工作流定义</returns>
        private async Task<WorkflowDefinition> CreateProjectInitializationWorkflowAsync(Dictionary<string, object> parameters)
        {
            var workflow = new WorkflowDefinition
            {
                Name = "项目初始化工作流",
                Description = "创建新项目时的初始化流程",
                Tasks = new List<WorkflowTask>
                {
                    new WorkflowTask
                    {
                        Name = "主题分析",
                        TaskType = "AnalyzeTheme",
                        TargetAgentId = "director",
                        Parameters = parameters,
                        Priority = 10
                    },
                    new WorkflowTask
                    {
                        Name = "生成大纲",
                        TaskType = "GenerateOutline",
                        TargetAgentId = "director",
                        Parameters = parameters,
                        Dependencies = new List<string>(),
                        Priority = 9
                    },
                    new WorkflowTask
                    {
                        Name = "创建世界设定",
                        TaskType = "CreateWorldSetting",
                        TargetAgentId = "director",
                        Parameters = parameters,
                        Priority = 8
                    }
                }
            };

            // 设置任务依赖关系
            workflow.Tasks[1].Dependencies.Add(workflow.Tasks[0].Id); // 大纲生成依赖主题分析
            workflow.Tasks[2].Dependencies.Add(workflow.Tasks[1].Id); // 世界设定依赖大纲生成

            return await Task.FromResult(workflow);
        }

        /// <summary>
        /// 创建章节创建工作流
        /// </summary>
        /// <param name="parameters">参数</param>
        /// <returns>工作流定义</returns>
        private async Task<WorkflowDefinition> CreateChapterCreationWorkflowAsync(Dictionary<string, object> parameters)
        {
            var workflow = new WorkflowDefinition
            {
                Name = "章节创建工作流",
                Description = "创建新章节的完整流程",
                Tasks = new List<WorkflowTask>
                {
                    new WorkflowTask
                    {
                        Name = "生成章节内容",
                        TaskType = "GenerateChapterContent",
                        TargetAgentId = "writer",
                        Parameters = parameters,
                        Priority = 10
                    },
                    new WorkflowTask
                    {
                        Name = "章节总结",
                        TaskType = "SummarizeChapter",
                        TargetAgentId = "summarizer",
                        Parameters = parameters,
                        Priority = 8
                    },
                    new WorkflowTask
                    {
                        Name = "章节评价",
                        TaskType = "EvaluateChapter",
                        TargetAgentId = "reader",
                        Parameters = parameters,
                        Priority = 7
                    }
                }
            };

            // 设置依赖关系
            workflow.Tasks[1].Dependencies.Add(workflow.Tasks[0].Id);
            workflow.Tasks[2].Dependencies.Add(workflow.Tasks[0].Id);

            return await Task.FromResult(workflow);
        }

        /// <summary>
        /// 创建内容审查工作流
        /// </summary>
        /// <param name="parameters">参数</param>
        /// <returns>工作流定义</returns>
        private async Task<WorkflowDefinition> CreateContentReviewWorkflowAsync(Dictionary<string, object> parameters)
        {
            // 简化实现
            return await Task.FromResult(new WorkflowDefinition
            {
                Name = "内容审查工作流",
                Description = "对内容进行全面审查"
            });
        }

        /// <summary>
        /// 创建一致性检查工作流
        /// </summary>
        /// <param name="parameters">参数</param>
        /// <returns>工作流定义</returns>
        private async Task<WorkflowDefinition> CreateConsistencyCheckWorkflowAsync(Dictionary<string, object> parameters)
        {
            // 简化实现
            return await Task.FromResult(new WorkflowDefinition
            {
                Name = "一致性检查工作流",
                Description = "检查内容的一致性"
            });
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 验证工作流任务
        /// </summary>
        /// <param name="workflow">工作流定义</param>
        /// <returns>验证结果</returns>
        private WorkflowValidationResult ValidateWorkflowTasks(WorkflowDefinition workflow)
        {
            foreach (var task in workflow.Tasks)
            {
                if (!_registeredAgents.ContainsKey(task.TargetAgentId))
                {
                    return new WorkflowValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"任务 {task.Name} 的目标Agent {task.TargetAgentId} 未注册"
                    };
                }
            }

            return new WorkflowValidationResult { IsValid = true };
        }

        /// <summary>
        /// 等待工作流完成
        /// </summary>
        /// <param name="workflow">工作流定义</param>
        private async Task WaitForWorkflowCompletionAsync(WorkflowDefinition workflow)
        {
            while (workflow.Tasks.Any(t => t.Status == WorkflowStatus.Pending || t.Status == WorkflowStatus.Running))
            {
                await Task.Delay(1000);
                
                // 更新工作流进度
                var completedTasks = workflow.Tasks.Count(t => t.Status == WorkflowStatus.Completed || t.Status == WorkflowStatus.Failed);
                workflow.Progress = (completedTasks * 100) / workflow.Tasks.Count;
                
                OnWorkflowStatusChanged(workflow);
            }
        }

        /// <summary>
        /// 处理任务状态变化
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="task">任务</param>
        private void OnTaskStatusChanged(object? sender, WorkflowTask task)
        {
            TaskStatusChanged?.Invoke(this, task);
        }

        /// <summary>
        /// 处理Agent状态变化
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="statusInfo">状态信息</param>
        private void OnAgentStatusChanged(object? sender, AgentStatusInfo statusInfo)
        {
            _logger.LogDebug($"Agent状态变化: {statusInfo.Name} - {statusInfo.StatusDescription}");
        }

        /// <summary>
        /// 处理Agent任务完成
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="result">任务结果</param>
        private void OnAgentTaskCompleted(object? sender, AgentTaskResult result)
        {
            _logger.LogInformation($"Agent任务完成: 成功={result.IsSuccess}, 耗时={result.ExecutionTime.TotalSeconds:F2}秒");
        }

        /// <summary>
        /// 触发工作流状态变化事件
        /// </summary>
        /// <param name="workflow">工作流定义</param>
        private void OnWorkflowStatusChanged(WorkflowDefinition workflow)
        {
            WorkflowStatusChanged?.Invoke(this, workflow);
        }

        #endregion
    }

    /// <summary>
    /// 工作流验证结果
    /// </summary>
    internal class WorkflowValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
