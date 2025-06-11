using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NovelManagement.AI.Interfaces;

namespace NovelManagement.AI.Workflow
{
    /// <summary>
    /// 任务队列管理器
    /// </summary>
    public class TaskQueue
    {
        private readonly ILogger<TaskQueue> _logger;
        private readonly ConcurrentQueue<WorkflowTask> _taskQueue;
        private readonly ConcurrentDictionary<string, WorkflowTask> _runningTasks;
        private readonly ConcurrentDictionary<string, WorkflowTask> _completedTasks;
        private readonly SemaphoreSlim _semaphore;
        private readonly int _maxConcurrentTasks;
        private volatile bool _isProcessing;

        /// <summary>
        /// 任务状态变化事件
        /// </summary>
        public event EventHandler<WorkflowTask>? TaskStatusChanged;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="maxConcurrentTasks">最大并发任务数</param>
        public TaskQueue(ILogger<TaskQueue> logger, int maxConcurrentTasks = 5)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _maxConcurrentTasks = maxConcurrentTasks;
            _taskQueue = new ConcurrentQueue<WorkflowTask>();
            _runningTasks = new ConcurrentDictionary<string, WorkflowTask>();
            _completedTasks = new ConcurrentDictionary<string, WorkflowTask>();
            _semaphore = new SemaphoreSlim(maxConcurrentTasks, maxConcurrentTasks);
            _isProcessing = false;
        }

        /// <summary>
        /// 添加任务到队列
        /// </summary>
        /// <param name="task">任务</param>
        public void EnqueueTask(WorkflowTask task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            task.Status = WorkflowStatus.Pending;
            _taskQueue.Enqueue(task);
            
            _logger.LogInformation($"任务已加入队列: {task.Name} (ID: {task.Id})");
            
            OnTaskStatusChanged(task);
            
            // 如果队列处理器未运行，启动它
            if (!_isProcessing)
            {
                // 使用Task.Factory.StartNew避免DbContext并发问题
                _ = Task.Factory.StartNew(ProcessQueueAsync, TaskCreationOptions.LongRunning);
            }
        }

        /// <summary>
        /// 批量添加任务
        /// </summary>
        /// <param name="tasks">任务列表</param>
        public void EnqueueTasks(IEnumerable<WorkflowTask> tasks)
        {
            if (tasks == null)
                throw new ArgumentNullException(nameof(tasks));

            foreach (var task in tasks.OrderBy(t => t.Priority).ThenBy(t => t.CreatedAt))
            {
                EnqueueTask(task);
            }
        }

        /// <summary>
        /// 获取队列状态
        /// </summary>
        /// <returns>队列状态信息</returns>
        public TaskQueueStatus GetQueueStatus()
        {
            return new TaskQueueStatus
            {
                PendingTasks = _taskQueue.Count,
                RunningTasks = _runningTasks.Count,
                CompletedTasks = _completedTasks.Count,
                MaxConcurrentTasks = _maxConcurrentTasks,
                IsProcessing = _isProcessing
            };
        }

        /// <summary>
        /// 获取所有任务
        /// </summary>
        /// <returns>任务列表</returns>
        public List<WorkflowTask> GetAllTasks()
        {
            var allTasks = new List<WorkflowTask>();
            
            // 添加队列中的任务
            allTasks.AddRange(_taskQueue.ToArray());
            
            // 添加运行中的任务
            allTasks.AddRange(_runningTasks.Values);
            
            // 添加已完成的任务
            allTasks.AddRange(_completedTasks.Values);
            
            return allTasks.OrderBy(t => t.CreatedAt).ToList();
        }

        /// <summary>
        /// 获取指定状态的任务
        /// </summary>
        /// <param name="status">任务状态</param>
        /// <returns>任务列表</returns>
        public List<WorkflowTask> GetTasksByStatus(WorkflowStatus status)
        {
            return GetAllTasks().Where(t => t.Status == status).ToList();
        }

        /// <summary>
        /// 取消任务
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <returns>是否成功</returns>
        public bool CancelTask(string taskId)
        {
            // 尝试从运行中的任务中取消
            if (_runningTasks.TryGetValue(taskId, out var runningTask))
            {
                runningTask.Status = WorkflowStatus.Cancelled;
                runningTask.CompletedAt = DateTime.Now;
                runningTask.ErrorMessage = "任务被用户取消";
                
                _runningTasks.TryRemove(taskId, out _);
                _completedTasks.TryAdd(taskId, runningTask);
                
                OnTaskStatusChanged(runningTask);
                
                _logger.LogInformation($"任务已取消: {runningTask.Name} (ID: {taskId})");
                return true;
            }

            // 尝试从队列中移除（这里简化处理，实际实现可能需要更复杂的逻辑）
            var queueTasks = _taskQueue.ToArray();
            var taskToCancel = queueTasks.FirstOrDefault(t => t.Id == taskId);
            if (taskToCancel != null)
            {
                taskToCancel.Status = WorkflowStatus.Cancelled;
                taskToCancel.CompletedAt = DateTime.Now;
                taskToCancel.ErrorMessage = "任务被用户取消";
                
                _completedTasks.TryAdd(taskId, taskToCancel);
                
                OnTaskStatusChanged(taskToCancel);
                
                _logger.LogInformation($"队列中的任务已取消: {taskToCancel.Name} (ID: {taskId})");
                return true;
            }

            return false;
        }

        /// <summary>
        /// 清空队列
        /// </summary>
        public void ClearQueue()
        {
            while (_taskQueue.TryDequeue(out var task))
            {
                task.Status = WorkflowStatus.Cancelled;
                task.CompletedAt = DateTime.Now;
                task.ErrorMessage = "队列被清空";
                
                _completedTasks.TryAdd(task.Id, task);
                OnTaskStatusChanged(task);
            }
            
            _logger.LogInformation("任务队列已清空");
        }

        /// <summary>
        /// 处理队列中的任务
        /// </summary>
        private async Task ProcessQueueAsync()
        {
            _isProcessing = true;
            
            try
            {
                while (_taskQueue.TryDequeue(out var task) || _runningTasks.Count > 0)
                {
                    if (task != null)
                    {
                        await _semaphore.WaitAsync();
                        
                        // 检查任务依赖
                        if (!AreDependenciesSatisfied(task))
                        {
                            // 依赖未满足，重新加入队列
                            _taskQueue.Enqueue(task);
                            _semaphore.Release();
                            await Task.Delay(1000); // 等待一段时间再检查
                            continue;
                        }
                        
                        // 启动任务处理，使用Task.Factory.StartNew避免DbContext并发问题
                        _ = Task.Factory.StartNew(async () =>
                        {
                            try
                            {
                                await ProcessTaskAsync(task);
                            }
                            finally
                            {
                                _semaphore.Release();
                            }
                        }, TaskCreationOptions.LongRunning).Unwrap();
                    }
                    else
                    {
                        // 队列为空，等待一段时间
                        await Task.Delay(500);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "队列处理过程中发生异常");
            }
            finally
            {
                _isProcessing = false;
            }
        }

        /// <summary>
        /// 处理单个任务
        /// </summary>
        /// <param name="task">任务</param>
        private async Task ProcessTaskAsync(WorkflowTask task)
        {
            try
            {
                task.Status = WorkflowStatus.Running;
                task.StartedAt = DateTime.Now;
                _runningTasks.TryAdd(task.Id, task);
                
                OnTaskStatusChanged(task);
                
                _logger.LogInformation($"开始处理任务: {task.Name} (ID: {task.Id})");
                
                // 模拟任务处理（实际实现中会调用相应的Agent）
                await SimulateTaskProcessingAsync(task);
                
                task.Status = WorkflowStatus.Completed;
                task.CompletedAt = DateTime.Now;
                task.Progress = 100;
                
                _logger.LogInformation($"任务处理完成: {task.Name} (ID: {task.Id})");
            }
            catch (Exception ex)
            {
                task.Status = WorkflowStatus.Failed;
                task.CompletedAt = DateTime.Now;
                task.ErrorMessage = ex.Message;
                
                _logger.LogError(ex, $"任务处理失败: {task.Name} (ID: {task.Id})");
            }
            finally
            {
                _runningTasks.TryRemove(task.Id, out _);
                _completedTasks.TryAdd(task.Id, task);
                
                OnTaskStatusChanged(task);
            }
        }

        /// <summary>
        /// 模拟任务处理过程
        /// </summary>
        /// <param name="task">任务</param>
        private async Task SimulateTaskProcessingAsync(WorkflowTask task)
        {
            try
            {
                var random = new Random();
                var totalSteps = 10;

                _logger.LogInformation($"开始执行任务: {task.Name}, 类型: {task.TaskType}");

                for (int i = 0; i < totalSteps; i++)
                {
                    // 检查任务是否被取消
                    if (task.Status == WorkflowStatus.Cancelled)
                    {
                        _logger.LogInformation($"任务被取消: {task.Name}");
                        return;
                    }

                    await Task.Delay(random.Next(100, 300)); // 减少延迟时间

                    task.Progress = (i + 1) * 100 / totalSteps;
                    OnTaskStatusChanged(task);

                    _logger.LogDebug($"任务进度更新: {task.Name} - {task.Progress}%");
                }

                // 根据任务类型执行不同的逻辑
                await ExecuteTaskByTypeAsync(task);

                _logger.LogInformation($"任务执行完成: {task.Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"任务执行过程中发生异常: {task.Name}");
                throw;
            }
        }

        /// <summary>
        /// 根据任务类型执行具体逻辑
        /// </summary>
        /// <param name="task">任务</param>
        private async Task ExecuteTaskByTypeAsync(WorkflowTask task)
        {
            try
            {
                switch (task.TaskType?.ToLower())
                {
                    case "project_init":
                        await ExecuteProjectInitTaskAsync(task);
                        break;
                    case "chapter_create":
                        await ExecuteChapterCreateTaskAsync(task);
                        break;
                    case "content_review":
                        await ExecuteContentReviewTaskAsync(task);
                        break;
                    case "consistency_check":
                        await ExecuteConsistencyCheckTaskAsync(task);
                        break;
                    default:
                        _logger.LogWarning($"未知的任务类型: {task.TaskType}");
                        // 默认成功完成
                        task.Result = new AgentTaskResult
                        {
                            IsSuccess = true,
                            Data = new Dictionary<string, object>
                            {
                                ["Status"] = "Completed",
                                ["Message"] = $"任务 {task.Name} 已完成"
                            }
                        };
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"执行任务类型 {task.TaskType} 失败");
                throw;
            }
        }

        /// <summary>
        /// 执行项目初始化任务
        /// </summary>
        private async Task ExecuteProjectInitTaskAsync(WorkflowTask task)
        {
            _logger.LogInformation("执行项目初始化任务");

            // 模拟项目初始化过程
            await Task.Delay(500);

            task.Result = new AgentTaskResult
            {
                IsSuccess = true,
                Data = new Dictionary<string, object>
                {
                    ["Status"] = "Success",
                    ["Message"] = "项目初始化完成",
                    ["ProjectId"] = Guid.NewGuid().ToString(),
                    ["CreatedAt"] = DateTime.Now
                }
            };
        }

        /// <summary>
        /// 执行章节创建任务
        /// </summary>
        private async Task ExecuteChapterCreateTaskAsync(WorkflowTask task)
        {
            _logger.LogInformation("执行章节创建任务");

            // 模拟章节创建过程
            await Task.Delay(300);

            task.Result = new AgentTaskResult
            {
                IsSuccess = true,
                Data = new Dictionary<string, object>
                {
                    ["Status"] = "Success",
                    ["Message"] = "章节创建完成",
                    ["ChapterId"] = Guid.NewGuid().ToString(),
                    ["Title"] = "新章节",
                    ["CreatedAt"] = DateTime.Now
                }
            };
        }

        /// <summary>
        /// 执行内容审查任务
        /// </summary>
        private async Task ExecuteContentReviewTaskAsync(WorkflowTask task)
        {
            _logger.LogInformation("执行内容审查任务");

            // 模拟内容审查过程
            await Task.Delay(400);

            task.Result = new AgentTaskResult
            {
                IsSuccess = true,
                Data = new Dictionary<string, object>
                {
                    ["Status"] = "Success",
                    ["Message"] = "内容审查完成",
                    ["ReviewScore"] = 85,
                    ["Issues"] = new List<string>(),
                    ["ReviewedAt"] = DateTime.Now
                }
            };
        }

        /// <summary>
        /// 执行一致性检查任务
        /// </summary>
        private async Task ExecuteConsistencyCheckTaskAsync(WorkflowTask task)
        {
            _logger.LogInformation("执行一致性检查任务");

            // 模拟一致性检查过程
            await Task.Delay(600);

            task.Result = new AgentTaskResult
            {
                IsSuccess = true,
                Data = new Dictionary<string, object>
                {
                    ["Status"] = "Success",
                    ["Message"] = "一致性检查完成",
                    ["ConsistencyScore"] = 92,
                    ["Conflicts"] = new List<string>(),
                    ["CheckedAt"] = DateTime.Now
                }
            };
        }

        /// <summary>
        /// 检查任务依赖是否满足
        /// </summary>
        /// <param name="task">任务</param>
        /// <returns>依赖是否满足</returns>
        private bool AreDependenciesSatisfied(WorkflowTask task)
        {
            if (task.Dependencies == null || task.Dependencies.Count == 0)
                return true;

            foreach (var dependencyId in task.Dependencies)
            {
                if (!_completedTasks.ContainsKey(dependencyId) || 
                    _completedTasks[dependencyId].Status != WorkflowStatus.Completed)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 触发任务状态变化事件
        /// </summary>
        /// <param name="task">任务</param>
        private void OnTaskStatusChanged(WorkflowTask task)
        {
            TaskStatusChanged?.Invoke(this, task);
        }
    }

    /// <summary>
    /// 任务队列状态
    /// </summary>
    public class TaskQueueStatus
    {
        /// <summary>
        /// 待处理任务数
        /// </summary>
        public int PendingTasks { get; set; }
        
        /// <summary>
        /// 运行中任务数
        /// </summary>
        public int RunningTasks { get; set; }
        
        /// <summary>
        /// 已完成任务数
        /// </summary>
        public int CompletedTasks { get; set; }
        
        /// <summary>
        /// 最大并发任务数
        /// </summary>
        public int MaxConcurrentTasks { get; set; }
        
        /// <summary>
        /// 是否正在处理
        /// </summary>
        public bool IsProcessing { get; set; }
    }
}
