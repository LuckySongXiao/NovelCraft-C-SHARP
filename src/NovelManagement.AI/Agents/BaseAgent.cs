using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NovelManagement.AI.Interfaces;
using NovelManagement.AI.Services.DeepSeek;
using NovelManagement.AI.Services.RWKV;
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

        /// <summary>
        /// 本地 RWKV 推理服务（可选）：Agent 化功能的真实推理链路入口，
        /// 优先级高于 Ollama/DeepSeek 远程服务；不可用时各任务回退原有执行方式。
        /// </summary>
        protected readonly IRwkvLightningService? RwkvService;

        /// <summary>防递归降级标记：ExecuteTaskWithAIAsync 失败回退 ExecuteTaskAsync 时，
        /// 若任务内部再次进入 AI 辅助执行则直接失败返回，避免无限递归栈溢出。</summary>
        private bool _isFallbackExecution;

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
        /// <param name="rwkvService">本地 RWKV 推理服务（可选）</param>
        protected BaseAgent(
            ILogger logger,
            IMemoryManager memoryManager,
            IDeepSeekApiService deepSeekApiService,
            IThinkingChainProcessor thinkingChainProcessor,
            ModelManager modelManager,
            IRwkvLightningService? rwkvService = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _memoryManager = memoryManager;
            _deepSeekApiService = deepSeekApiService;
            _thinkingChainProcessor = thinkingChainProcessor;
            _modelManager = modelManager;
            RwkvService = rwkvService;
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
            // 本地 RWKV 真实推理优先（服务未启动时返回 null 走原有链路）
            var rwkvResult = await TryExecuteWithRwkvAsync(taskType, parameters, thinkingChain);
            if (rwkvResult != null)
            {
                return rwkvResult;
            }

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
                // 本地 RWKV 真实推理优先（服务未启动时返回 null 走原有链路）
                var rwkvResult = await TryExecuteWithRwkvAsync(taskType, parameters, thinkingChain);
                if (rwkvResult != null)
                {
                    return rwkvResult;
                }

                // 构建系统提示
                var systemPrompt = BuildSystemPrompt(taskType);

                // 构建用户提示
                var userPrompt = BuildUserPrompt(taskType, parameters);

                string aiResponse;

                // 优先使用已解析出的可用模型提供者
                var defaultProvider = _modelManager?.GetDefaultProvider();
                if (_modelManager != null && defaultProvider != null && defaultProvider.IsAvailable)
                {
                    _logger.LogInformation("使用模型提供者 {ProviderName} 执行任务: {TaskType}", defaultProvider.ProviderName, taskType);

                    // 构建聊天请求
                    var chatRequest = new NovelManagement.AI.Interfaces.ChatRequest
                    {
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

                    // 使用解析后的默认提供者调用聊天接口
                    var chatResponse = await _modelManager.ChatAsync(defaultProvider.ProviderName, chatRequest);

                    if (!chatResponse.IsSuccess)
                    {
                        throw new Exception($"{defaultProvider.ProviderName} 模型调用失败: {chatResponse.ErrorMessage}");
                    }

                    aiResponse = chatResponse.Content;

                    if (string.IsNullOrWhiteSpace(aiResponse))
                    {
                        throw new Exception($"{defaultProvider.ProviderName} 模型返回空响应");
                    }
                }
                else if (_deepSeekApiService != null && _deepSeekApiService.GetConfiguration().IsValid())
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

                // 防递归保护：回退执行的任务内部再次进入 AI 辅助执行失败时直接返回失败，
                // 否则「回退→任务→AI辅助→回退」会无限递归导致栈溢出
                if (_isFallbackExecution)
                {
                    return new AgentTaskResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"AI服务不可用且回退执行失败: {ex.Message}"
                    };
                }

                // 回退到原有执行方式
                _isFallbackExecution = true;
                try
                {
                    return await ExecuteTaskAsync(taskType, parameters);
                }
                finally
                {
                    _isFallbackExecution = false;
                }
            }
        }

        #region 本地 RWKV 真实推理

        /// <summary>
        /// 尝试使用本地 RWKV 推理执行任务（Agent 化功能的真实推理链路）。
        /// 服务未启动时返回 null（调用方继续走 Ollama/DeepSeek/本地回退链路）；
        /// 服务可用但调用失败时返回失败结果（绝不降级到模拟数据，保证结果真实可信）。
        /// </summary>
        /// <param name="taskType">任务类型</param>
        /// <param name="parameters">任务参数</param>
        /// <param name="thinkingChain">思维链（可为 null）</param>
        /// <returns>任务结果；null 表示 RWKV 服务不可用</returns>
        private async Task<AgentTaskResult?> TryExecuteWithRwkvAsync(
            string taskType,
            Dictionary<string, object> parameters,
            ThinkingChainModel? thinkingChain)
        {
            if (RwkvService == null || !RwkvService.IsAvailable)
            {
                return null;
            }

            _logger.LogInformation("使用本地 RWKV 推理执行任务: {TaskType}", taskType);
            if (thinkingChain != null)
            {
                var step = new ThinkingStep
                {
                    Title = "调用本地模型",
                    Content = "使用本地 RWKV 推理服务生成内容",
                    Type = ThinkingStepType.Synthesis,
                    Confidence = 0.9
                };
                step.Start();
                thinkingChain.AddStep(step);
                step.Complete();
                thinkingChain.UpdateProgress();
            }

            try
            {
                // RWKV completion 提示格式（与 ChapterNamingAgent 范例一致，空 think 块适配 RWKV7-G1）
                var systemPrompt = BuildSystemPrompt(taskType);
                var userPrompt = BuildUserPrompt(taskType, parameters);
                var prompt = "User: " + systemPrompt + "\n" + userPrompt + "\n\nAssistant: <think></think\n";

                var response = await RwkvService.CompleteAsync(prompt, ResolveRwkvMaxTokens(taskType));
                if (!response.Success || string.IsNullOrWhiteSpace(response.Text))
                {
                    _logger.LogWarning("RWKV 推理任务 {TaskType} 失败: {Error}", taskType, response.Error);
                    return new AgentTaskResult
                    {
                        IsSuccess = false,
                        ErrorMessage = response.Error ?? "RWKV 推理返回空结果"
                    };
                }

                return await ProcessAIResponseAsync(taskType, response.Text, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RWKV 推理任务 {TaskType} 异常", taskType);
                return new AgentTaskResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"RWKV 推理失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 按任务类型解析 RWKV 生成的最大 token 数（生成类任务更长，分析/总结类任务较短）。
        /// </summary>
        private static int ResolveRwkvMaxTokens(string taskType) => taskType switch
        {
            "GenerateOutline" or "CreateWorldSetting" => 2048,
            "GenerateChapterContent" or "ContinueChapter" => 2048,
            "PolishText" or "OptimizeOutline" or "OptimizePlot" => 1800,
            "GenerateCharacter" or "OptimizeCharacter" or "GeneratePlot" or "GetPlotSuggestions" => 1200,
            "SummarizeChapter" or "SummarizeVolume" => 600,
            _ => 1000
        };

        #endregion

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
