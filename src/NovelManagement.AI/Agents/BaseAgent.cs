using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NovelManagement.AI.Interfaces;
using NovelManagement.AI.Services.DeepSeek;
using NovelManagement.AI.Services.ThinkingChain;
using NovelManagement.AI.Services.ThinkingChain.Models;
using NovelManagement.AI.Services;
using ThinkingChainModel = NovelManagement.AI.Services.ThinkingChain.Models.ThinkingChain;

namespace NovelManagement.AI.Agents
{
    /// <summary>
    /// Agent基础实现类
    /// </summary>
    public abstract class BaseAgent : IAgent
    {
        protected readonly ILogger _logger;
        protected readonly IMemoryManager _memoryManager;
        protected readonly IDeepSeekApiService _deepSeekApiService;
        protected readonly IThinkingChainProcessor _thinkingChainProcessor;
        protected readonly ModelManager _modelManager;
        private AgentStatus _status = AgentStatus.Offline;
        private string _currentTask = string.Empty;
        private int _progress = 0;
        private DateTime _lastActivity = DateTime.Now;
        private ThinkingChainModel? _currentThinkingChain;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="memoryManager">记忆管理器（可选）</param>
        /// <param name="deepSeekApiService">DeepSeek API服务（可选）</param>
        /// <param name="thinkingChainProcessor">思维链处理器（可选）</param>
        /// <param name="modelManager">模型管理器（可选）</param>
        protected BaseAgent(
            ILogger logger,
            IMemoryManager memoryManager,
            IDeepSeekApiService deepSeekApiService,
            IThinkingChainProcessor thinkingChainProcessor,
            ModelManager modelManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _memoryManager = memoryManager;
            _deepSeekApiService = deepSeekApiService;
            _thinkingChainProcessor = thinkingChainProcessor;
            _modelManager = modelManager;
            Id = Guid.NewGuid().ToString();
        }

        #region IAgent 实现

        /// <inheritdoc/>
        public string Id { get; }

        /// <inheritdoc/>
        public abstract string Name { get; }

        /// <inheritdoc/>
        public abstract string Description { get; }

        /// <inheritdoc/>
        public virtual string Version => "1.0.0";

        /// <inheritdoc/>
        public AgentStatus Status 
        { 
            get => _status;
            protected set
            {
                if (_status != value)
                {
                    _status = value;
                    _lastActivity = DateTime.Now;
                    OnStatusChanged();
                }
            }
        }

        /// <inheritdoc/>
        public event EventHandler<AgentStatusInfo>? StatusChanged;

        /// <inheritdoc/>
        public event EventHandler<AgentTaskResult>? TaskCompleted;

        /// <summary>
        /// 思维链更新事件
        /// </summary>
        public event EventHandler<ThinkingChainModel>? ThinkingChainUpdated;

        /// <summary>
        /// 当前思维链
        /// </summary>
        public ThinkingChainModel? CurrentThinkingChain
        {
            get => _currentThinkingChain;
            protected set
            {
                if (_currentThinkingChain != value)
                {
                    _currentThinkingChain = value;
                    OnThinkingChainUpdated(value);
                }
            }
        }

        /// <inheritdoc/>
        public virtual async Task<AgentTaskResult> ExecuteAsync(string taskType, Dictionary<string, object> parameters)
        {
            var startTime = DateTime.Now;

            try
            {
                _logger.LogInformation($"Agent {Name} 开始执行任务: {taskType}");

                // 更新状态
                Status = AgentStatus.Working;
                _currentTask = taskType;
                _progress = 0;

                // 创建思维链（如果启用）
                ThinkingChainModel? thinkingChain = null;
                if (_thinkingChainProcessor != null && ShouldUseThinkingChain(taskType))
                {
                    thinkingChain = new ThinkingChainModel
                    {
                        Title = $"{Name} - {taskType}",
                        Description = $"执行任务: {taskType}",
                        TaskId = Guid.NewGuid().ToString(),
                        AgentId = Id
                    };
                    thinkingChain.Start();
                    CurrentThinkingChain = thinkingChain;
                }

                // 执行具体任务（可能包含思维链）
                var result = await ExecuteTaskWithThinkingAsync(taskType, parameters, thinkingChain);

                // 完成思维链
                if (thinkingChain != null)
                {
                    thinkingChain.FinalOutput = result.Data?.ToString() ?? string.Empty;
                    thinkingChain.Complete();
                }

                // 更新进度
                _progress = 100;
                Status = AgentStatus.Idle;
                _currentTask = string.Empty;
                CurrentThinkingChain = null;

                var executionTime = DateTime.Now - startTime;
                result.ExecutionTime = executionTime;

                _logger.LogInformation($"Agent {Name} 完成任务: {taskType}, 耗时: {executionTime.TotalSeconds:F2}秒");

                // 触发任务完成事件
                OnTaskCompleted(result);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Agent {Name} 执行任务失败: {taskType}");

                // 标记思维链失败
                if (CurrentThinkingChain != null)
                {
                    CurrentThinkingChain.Fail();
                }

                Status = AgentStatus.Error;
                CurrentThinkingChain = null;

                var errorResult = new AgentTaskResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.Now - startTime
                };

                OnTaskCompleted(errorResult);
                return errorResult;
            }
        }

        /// <inheritdoc/>
        public virtual async Task<List<AgentCapability>> GetCapabilitiesAsync()
        {
            return await Task.FromResult(GetSupportedCapabilities());
        }

        /// <inheritdoc/>
        public virtual async Task<AgentStatusInfo> GetStatusAsync()
        {
            return await Task.FromResult(new AgentStatusInfo
            {
                AgentId = Id,
                Name = Name,
                Status = Status,
                StatusDescription = GetStatusDescription(),
                CurrentTask = _currentTask,
                Progress = _progress,
                LastActivity = _lastActivity,
                ErrorMessage = Status == AgentStatus.Error ? "Agent执行出错" : null
            });
        }

        /// <inheritdoc/>
        public virtual async Task<bool> InitializeAsync(Dictionary<string, object> configuration)
        {
            try
            {
                _logger.LogInformation($"初始化Agent: {Name}");
                
                var result = await InitializeAgentAsync(configuration);
                
                if (result)
                {
                    Status = AgentStatus.Idle;
                    _logger.LogInformation($"Agent {Name} 初始化成功");
                }
                else
                {
                    Status = AgentStatus.Error;
                    _logger.LogError($"Agent {Name} 初始化失败");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Agent {Name} 初始化异常");
                Status = AgentStatus.Error;
                return false;
            }
        }

        /// <inheritdoc/>
        public virtual async Task<bool> StopAsync()
        {
            try
            {
                _logger.LogInformation($"停止Agent: {Name}");
                
                var result = await StopAgentAsync();
                
                if (result)
                {
                    Status = AgentStatus.Offline;
                    _logger.LogInformation($"Agent {Name} 已停止");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"停止Agent {Name} 时发生异常");
                return false;
            }
        }

        /// <inheritdoc/>
        public virtual async Task<bool> ResetAsync()
        {
            try
            {
                _logger.LogInformation($"重置Agent: {Name}");
                
                var result = await ResetAgentAsync();
                
                if (result)
                {
                    Status = AgentStatus.Idle;
                    _currentTask = string.Empty;
                    _progress = 0;
                    _logger.LogInformation($"Agent {Name} 已重置");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"重置Agent {Name} 时发生异常");
                return false;
            }
        }

        /// <inheritdoc/>
        public virtual async Task<bool> HealthCheckAsync()
        {
            try
            {
                return await PerformHealthCheckAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Agent {Name} 健康检查异常");
                return false;
            }
        }

        #endregion

        #region 记忆管理辅助方法

        /// <summary>
        /// 获取记忆上下文
        /// </summary>
        /// <param name="taskType">任务类型</param>
        /// <param name="scope">记忆范围</param>
        /// <param name="projectId">项目ID</param>
        /// <param name="volumeId">卷宗ID</param>
        /// <param name="chapterId">章节ID</param>
        /// <returns>记忆上下文</returns>
        protected async Task<MemoryContext?> GetMemoryContextAsync(string taskType, MemoryScope scope, Guid projectId, Guid? volumeId = null, Guid? chapterId = null)
        {
            if (_memoryManager == null)
            {
                _logger.LogWarning("记忆管理器未配置，无法获取记忆上下文");
                return null;
            }

            try
            {
                return await _memoryManager.GetContextAsync(taskType, scope, projectId, volumeId, chapterId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取记忆上下文失败: TaskType={taskType}, Scope={scope}");
                return null;
            }
        }

        /// <summary>
        /// 更新记忆
        /// </summary>
        /// <param name="content">内容</param>
        /// <param name="importanceScore">重要性评分</param>
        /// <param name="scope">记忆范围</param>
        /// <param name="projectId">项目ID</param>
        /// <param name="volumeId">卷宗ID</param>
        /// <param name="chapterId">章节ID</param>
        /// <returns>是否成功</returns>
        protected async Task<bool> UpdateMemoryAsync(string content, int importanceScore, MemoryScope scope, Guid projectId, Guid? volumeId = null, Guid? chapterId = null)
        {
            if (_memoryManager == null)
            {
                _logger.LogDebug("记忆管理器未配置，跳过记忆更新");
                return true; // 不是错误，只是没有记忆管理功能
            }

            try
            {
                return await _memoryManager.UpdateMemoryAsync(content, importanceScore, scope, projectId, volumeId, chapterId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新记忆失败: Scope={scope}, ProjectId={projectId}");
                return false;
            }
        }

        /// <summary>
        /// 搜索记忆
        /// </summary>
        /// <param name="query">搜索查询</param>
        /// <param name="scope">记忆范围</param>
        /// <param name="projectId">项目ID</param>
        /// <param name="maxResults">最大结果数</param>
        /// <returns>搜索结果</returns>
        protected async Task<List<MemoryItem>> SearchMemoryAsync(string query, MemoryScope scope, Guid projectId, int maxResults = 10)
        {
            if (_memoryManager == null)
            {
                _logger.LogDebug("记忆管理器未配置，返回空搜索结果");
                return new List<MemoryItem>();
            }

            try
            {
                return await _memoryManager.SearchMemoryAsync(query, scope, projectId, maxResults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"搜索记忆失败: Query={query}, Scope={scope}");
                return new List<MemoryItem>();
            }
        }

        /// <summary>
        /// 记录Agent执行结果到记忆
        /// </summary>
        /// <param name="taskType">任务类型</param>
        /// <param name="result">执行结果</param>
        /// <param name="projectId">项目ID</param>
        /// <param name="volumeId">卷宗ID</param>
        /// <param name="chapterId">章节ID</param>
        /// <returns>是否成功</returns>
        protected async Task<bool> RecordExecutionResultAsync(string taskType, AgentTaskResult result, Guid projectId, Guid? volumeId = null, Guid? chapterId = null)
        {
            if (_memoryManager == null || !result.IsSuccess)
            {
                return true;
            }

            try
            {
                var content = $"Agent {Name} 执行任务 {taskType}：{result.Data?.ToString() ?? "无结果"}";
                var importance = result.IsSuccess ? 6 : 4; // 成功的任务重要性更高
                var scope = chapterId.HasValue ? MemoryScope.Chapter :
                           volumeId.HasValue ? MemoryScope.Volume : MemoryScope.Global;

                return await UpdateMemoryAsync(content, importance, scope, projectId, volumeId, chapterId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录执行结果到记忆失败");
                return false;
            }
        }

        #endregion

        #region 抽象方法 - 子类需要实现

        /// <summary>
        /// 执行具体任务 - 子类实现
        /// </summary>
        /// <param name="taskType">任务类型</param>
        /// <param name="parameters">任务参数</param>
        /// <returns>任务结果</returns>
        protected abstract Task<AgentTaskResult> ExecuteTaskAsync(string taskType, Dictionary<string, object> parameters);

        /// <summary>
        /// 获取支持的能力列表 - 子类实现
        /// </summary>
        /// <returns>能力列表</returns>
        protected abstract List<AgentCapability> GetSupportedCapabilities();

        /// <summary>
        /// 初始化Agent - 子类实现
        /// </summary>
        /// <param name="configuration">配置参数</param>
        /// <returns>初始化结果</returns>
        protected virtual async Task<bool> InitializeAgentAsync(Dictionary<string, object> configuration)
        {
            return await Task.FromResult(true);
        }

        /// <summary>
        /// 停止Agent - 子类实现
        /// </summary>
        /// <returns>停止结果</returns>
        protected virtual async Task<bool> StopAgentAsync()
        {
            return await Task.FromResult(true);
        }

        /// <summary>
        /// 重置Agent - 子类实现
        /// </summary>
        /// <returns>重置结果</returns>
        protected virtual async Task<bool> ResetAgentAsync()
        {
            return await Task.FromResult(true);
        }

        /// <summary>
        /// 执行健康检查 - 子类实现
        /// </summary>
        /// <returns>健康状态</returns>
        protected virtual async Task<bool> PerformHealthCheckAsync()
        {
            return await Task.FromResult(Status != AgentStatus.Error);
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 获取状态描述
        /// </summary>
        /// <returns>状态描述</returns>
        protected virtual string GetStatusDescription()
        {
            return Status switch
            {
                AgentStatus.Idle => "空闲中",
                AgentStatus.Working => $"执行中: {_currentTask}",
                AgentStatus.Waiting => "等待中",
                AgentStatus.Error => "错误状态",
                AgentStatus.Offline => "离线",
                _ => "未知状态"
            };
        }

        /// <summary>
        /// 更新任务进度
        /// </summary>
        /// <param name="progress">进度值 (0-100)</param>
        protected void UpdateProgress(int progress)
        {
            _progress = Math.Max(0, Math.Min(100, progress));
            _lastActivity = DateTime.Now;
            OnStatusChanged();
        }

        /// <summary>
        /// 触发状态变化事件
        /// </summary>
        protected virtual void OnStatusChanged()
        {
            StatusChanged?.Invoke(this, new AgentStatusInfo
            {
                AgentId = Id,
                Name = Name,
                Status = Status,
                StatusDescription = GetStatusDescription(),
                CurrentTask = _currentTask,
                Progress = _progress,
                LastActivity = _lastActivity
            });
        }

        /// <summary>
        /// 触发任务完成事件
        /// </summary>
        /// <param name="result">任务结果</param>
        protected virtual void OnTaskCompleted(AgentTaskResult result)
        {
            TaskCompleted?.Invoke(this, result);
        }

        /// <summary>
        /// 触发思维链更新事件
        /// </summary>
        /// <param name="thinkingChain">思维链</param>
        protected virtual void OnThinkingChainUpdated(ThinkingChainModel? thinkingChain)
        {
            if (thinkingChain != null)
            {
                ThinkingChainUpdated?.Invoke(this, thinkingChain);
            }
        }

        #endregion

        #region DeepSeek API 和思维链辅助方法

        /// <summary>
        /// 判断是否应该使用思维链
        /// </summary>
        /// <param name="taskType">任务类型</param>
        /// <returns>是否使用思维链</returns>
        protected virtual bool ShouldUseThinkingChain(string taskType)
        {
            // 默认对复杂任务启用思维链
            var complexTasks = new[]
            {
                "GenerateOutline", "CreateWorldSetting", "DesignCharacters",
                "GenerateChapterContent", "AnalyzeTheme", "OptimizeOutline",
                "EvaluateChapter", "SuggestImprovements"
            };

            return complexTasks.Contains(taskType);
        }

        /// <summary>
        /// 执行带思维链的任务
        /// </summary>
        /// <param name="taskType">任务类型</param>
        /// <param name="parameters">任务参数</param>
        /// <param name="thinkingChain">思维链</param>
        /// <returns>任务结果</returns>
        protected virtual async Task<AgentTaskResult> ExecuteTaskWithThinkingAsync(string taskType, Dictionary<string, object> parameters, ThinkingChainModel? thinkingChain)
        {
            // 优先使用ModelManager，如果不可用则使用DeepSeek API
            if (_modelManager != null && thinkingChain != null)
            {
                return await ExecuteTaskWithAIAsync(taskType, parameters, thinkingChain);
            }
            else if (_deepSeekApiService != null && thinkingChain != null)
            {
                return await ExecuteTaskWithAIAsync(taskType, parameters, thinkingChain);
            }
            else
            {
                // 否则使用原有的执行方式
                return await ExecuteTaskAsync(taskType, parameters);
            }
        }

        /// <summary>
        /// 使用AI辅助执行任务
        /// </summary>
        /// <param name="taskType">任务类型</param>
        /// <param name="parameters">任务参数</param>
        /// <param name="thinkingChain">思维链</param>
        /// <returns>任务结果</returns>
        protected virtual async Task<AgentTaskResult> ExecuteTaskWithAIAsync(string taskType, Dictionary<string, object> parameters, ThinkingChainModel thinkingChain)
        {
            try
            {
                // 构建系统提示
                var systemPrompt = BuildSystemPrompt(taskType);

                // 构建用户提示
                var userPrompt = BuildUserPrompt(taskType, parameters);

                string aiResponse;

                // 优先使用ModelManager
                if (_modelManager != null)
                {
                    _logger.LogInformation($"使用AI模型管理器执行任务: {taskType}");

                    // 构建聊天请求
                    var chatRequest = new NovelManagement.AI.Interfaces.ChatRequest
                    {
                        // Model留空，让Provider使用默认配置的模型
                        Model = string.Empty, 
                        SystemPrompt = systemPrompt,
                        Messages = new List<NovelManagement.AI.Interfaces.ChatMessage>
                        {
                            new NovelManagement.AI.Interfaces.ChatMessage
                            {
                                Role = "user",
                                Content = userPrompt,
                                Timestamp = DateTime.UtcNow
                            }
                        },
                        Temperature = 0.7,
                        MaxTokens = 4000
                    };

                    // 使用ModelManager调用默认模型
                    var chatResponse = await _modelManager.ChatAsync(chatRequest);

                    if (!chatResponse.IsSuccess)
                    {
                        throw new Exception($"AI模型调用失败: {chatResponse.ErrorMessage}");
                    }

                    aiResponse = chatResponse.Content;

                    if (string.IsNullOrWhiteSpace(aiResponse))
                    {
                        throw new Exception("AI模型返回空响应");
                    }
                }
                else if (_deepSeekApiService != null)
                {
                    _logger.LogInformation($"使用DeepSeek API执行任务: {taskType}");

                    // 回退到DeepSeek API
                    var (response, aiThinkingChain) = await _deepSeekApiService.ChatWithThinkingAsync(
                        userPrompt,
                        systemPrompt,
                        chain => OnThinkingChainUpdated(chain));

                    // 合并AI思维链到当前思维链
                    if (_thinkingChainProcessor != null && aiThinkingChain.Steps.Count > 0)
                    {
                        foreach (var step in aiThinkingChain.Steps)
                        {
                            thinkingChain.AddStep(step);
                        }
                    }

                    aiResponse = response.GetContent();
                }
                else
                {
                    throw new Exception("没有可用的AI服务");
                }

                // 处理AI响应
                var result = await ProcessAIResponseAsync(taskType, aiResponse, parameters);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"AI辅助执行任务失败: {taskType}");

                // 回退到原有执行方式
                return await ExecuteTaskAsync(taskType, parameters);
            }
        }

        /// <summary>
        /// 构建系统提示
        /// </summary>
        /// <param name="taskType">任务类型</param>
        /// <returns>系统提示</returns>
        protected virtual string BuildSystemPrompt(string taskType)
        {
            return $"你是一个专业的{Name}，负责{Description}。请根据用户的要求执行{taskType}任务，并提供详细的思考过程。";
        }

        /// <summary>
        /// 构建用户提示
        /// </summary>
        /// <param name="taskType">任务类型</param>
        /// <param name="parameters">任务参数</param>
        /// <returns>用户提示</returns>
        protected virtual string BuildUserPrompt(string taskType, Dictionary<string, object> parameters)
        {
            var prompt = $"请执行{taskType}任务。";

            if (parameters.Count > 0)
            {
                prompt += "\n参数信息：";
                foreach (var param in parameters)
                {
                    prompt += $"\n- {param.Key}: {param.Value}";
                }
            }

            return prompt;
        }

        /// <summary>
        /// 处理AI响应
        /// </summary>
        /// <param name="taskType">任务类型</param>
        /// <param name="aiResponse">AI响应</param>
        /// <param name="parameters">原始参数</param>
        /// <returns>任务结果</returns>
        protected virtual async Task<AgentTaskResult> ProcessAIResponseAsync(string taskType, string aiResponse, Dictionary<string, object> parameters)
        {
            // 默认实现：将AI响应作为结果返回
            return new AgentTaskResult
            {
                IsSuccess = true,
                Data = aiResponse,
                Metadata = new Dictionary<string, object>
                {
                    ["Message"] = $"AI辅助完成任务: {taskType}",
                    ["TaskType"] = taskType
                }
            };
        }

        /// <summary>
        /// 添加思维步骤
        /// </summary>
        /// <param name="title">步骤标题</param>
        /// <param name="content">步骤内容</param>
        /// <param name="type">步骤类型</param>
        /// <param name="confidence">置信度</param>
        protected void AddThinkingStep(string title, string content, ThinkingStepType type = ThinkingStepType.Reasoning, double confidence = 0.8)
        {
            if (CurrentThinkingChain != null)
            {
                var step = new ThinkingStep
                {
                    Title = title,
                    Content = content,
                    Type = type,
                    Confidence = confidence
                };

                step.Start();
                CurrentThinkingChain.AddStep(step);
                step.Complete();

                CurrentThinkingChain.UpdateProgress();
            }
        }

        #endregion

        #region 静态辅助方法

        #endregion
    }
}
