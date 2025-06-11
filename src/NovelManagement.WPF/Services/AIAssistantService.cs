using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Logging;
using NovelManagement.AI.Services.DeepSeek;
using NovelManagement.AI.Services.Ollama;
using NovelManagement.AI.Services;
using NovelManagement.AI.Services.ThinkingChain;
using NovelManagement.AI.Services.ThinkingChain.Models;
using NovelManagement.AI.Agents;
using NovelManagement.AI.Extensions;

namespace NovelManagement.WPF.Services
{
    /// <summary>
    /// AI助手服务接口
    /// </summary>
    public interface IAIAssistantService
    {
        /// <summary>
        /// 生成角色
        /// </summary>
        /// <param name="parameters">生成参数</param>
        /// <returns>生成结果</returns>
        Task<AIAssistantResult> GenerateCharacterAsync(Dictionary<string, object> parameters);

        /// <summary>
        /// 优化角色
        /// </summary>
        /// <param name="parameters">优化参数</param>
        /// <returns>优化结果</returns>
        Task<AIAssistantResult> OptimizeCharacterAsync(Dictionary<string, object> parameters);

        /// <summary>
        /// 分析角色关系
        /// </summary>
        /// <param name="parameters">分析参数</param>
        /// <returns>分析结果</returns>
        Task<AIAssistantResult> AnalyzeCharacterRelationshipsAsync(Dictionary<string, object> parameters);

        /// <summary>
        /// 生成剧情
        /// </summary>
        /// <param name="parameters">生成参数</param>
        /// <returns>生成结果</returns>
        Task<AIAssistantResult> GeneratePlotAsync(Dictionary<string, object> parameters);

        /// <summary>
        /// 优化剧情
        /// </summary>
        /// <param name="parameters">优化参数</param>
        /// <returns>优化结果</returns>
        Task<AIAssistantResult> OptimizePlotAsync(Dictionary<string, object> parameters);

        /// <summary>
        /// 检查剧情连贯性
        /// </summary>
        /// <param name="parameters">检查参数</param>
        /// <returns>检查结果</returns>
        Task<AIAssistantResult> CheckPlotContinuityAsync(Dictionary<string, object> parameters);

        /// <summary>
        /// 获取剧情建议
        /// </summary>
        /// <param name="parameters">请求参数</param>
        /// <returns>建议结果</returns>
        Task<AIAssistantResult> GetPlotSuggestionsAsync(Dictionary<string, object> parameters);

        /// <summary>
        /// 生成章节内容
        /// </summary>
        /// <param name="parameters">生成参数</param>
        /// <returns>生成结果</returns>
        Task<AIAssistantResult> GenerateChapterAsync(Dictionary<string, object> parameters);

        /// <summary>
        /// 续写章节内容
        /// </summary>
        /// <param name="parameters">续写参数</param>
        /// <returns>续写结果</returns>
        Task<AIAssistantResult> ContinueChapterAsync(Dictionary<string, object> parameters);

        /// <summary>
        /// 润色文本内容
        /// </summary>
        /// <param name="parameters">润色参数</param>
        /// <returns>润色结果</returns>
        Task<AIAssistantResult> PolishTextAsync(Dictionary<string, object> parameters);

        /// <summary>
        /// 生成小说大纲
        /// </summary>
        /// <param name="parameters">生成参数</param>
        /// <returns>生成结果</returns>
        Task<AIAssistantResult> GenerateOutlineAsync(Dictionary<string, object> parameters);

        /// <summary>
        /// 获取使用统计信息
        /// </summary>
        /// <param name="timeRange">时间范围</param>
        /// <returns>统计信息</returns>
        AIUsageStatistics GetUsageStatistics(TimeRange timeRange = TimeRange.Today);

        /// <summary>
        /// 获取功能使用排行
        /// </summary>
        /// <param name="timeRange">时间范围</param>
        /// <param name="topCount">返回数量</param>
        /// <returns>功能使用排行</returns>
        List<FunctionUsageRank> GetFunctionUsageRanking(TimeRange timeRange = TimeRange.Today, int topCount = 10);

        /// <summary>
        /// 导出统计数据
        /// </summary>
        /// <param name="timeRange">时间范围</param>
        /// <returns>CSV格式的统计数据</returns>
        string ExportStatistics(TimeRange timeRange = TimeRange.Today);

        /// <summary>
        /// 清除统计数据
        /// </summary>
        /// <param name="olderThan">清除早于指定时间的数据</param>
        void ClearStatistics(DateTime? olderThan = null);
    }

    /// <summary>
    /// AI助手服务
    /// </summary>
    public class AIAssistantService : IAIAssistantService
    {
        private readonly ILogger<AIAssistantService> _logger;
        private readonly IDeepSeekApiService _deepSeekApiService;
        private readonly IOllamaApiService _ollamaApiService;
        private readonly ModelManager _modelManager;
        private readonly IThinkingChainProcessor _thinkingChainProcessor;
        private readonly FloatingTextManager _floatingTextManager;
        private readonly IAgentFactory _agentFactory;
        private readonly AIUsageStatisticsService _statisticsService;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="deepSeekApiService">DeepSeek API服务</param>
        /// <param name="ollamaApiService">Ollama API服务</param>
        /// <param name="modelManager">模型管理器</param>
        /// <param name="thinkingChainProcessor">思维链处理器</param>
        /// <param name="floatingTextManager">悬浮文本管理器</param>
        /// <param name="agentFactory">Agent工厂</param>
        /// <param name="statisticsService">统计服务</param>
        public AIAssistantService(
            ILogger<AIAssistantService> logger,
            IDeepSeekApiService deepSeekApiService,
            IOllamaApiService ollamaApiService,
            ModelManager modelManager,
            IThinkingChainProcessor thinkingChainProcessor,
            FloatingTextManager floatingTextManager,
            IAgentFactory agentFactory,
            AIUsageStatisticsService statisticsService)
        {
            _logger = logger;
            _deepSeekApiService = deepSeekApiService;
            _ollamaApiService = ollamaApiService;
            _modelManager = modelManager;
            _thinkingChainProcessor = thinkingChainProcessor;
            _floatingTextManager = floatingTextManager;
            _agentFactory = agentFactory;
            _statisticsService = statisticsService;

            // 异步初始化服务状态检查
            _ = Task.Run(InitializeServiceAsync);
        }

        /// <summary>
        /// 异步初始化服务
        /// </summary>
        private async Task InitializeServiceAsync()
        {
            try
            {
                _logger.LogInformation("开始初始化AI助手服务");

                // 检查各个服务的可用性
                await CheckServiceAvailabilityAsync();

                _logger.LogInformation("AI助手服务初始化完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化AI助手服务失败");
            }
        }

        /// <summary>
        /// 检查服务可用性
        /// </summary>
        private async Task CheckServiceAvailabilityAsync()
        {
            // 检查Ollama服务
            if (_ollamaApiService != null)
            {
                try
                {
                    var testResult = await _ollamaApiService.TestConnectionAsync();
                    if (testResult.IsSuccess)
                    {
                        _logger.LogInformation("Ollama服务连接正常");
                    }
                    else
                    {
                        _logger.LogWarning($"Ollama服务连接失败: {testResult.ErrorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "检查Ollama服务状态失败");
                }
            }

            // 检查Agent工厂
            if (_agentFactory != null)
            {
                try
                {
                    var testAgent = _agentFactory.CreateAgent<DirectorAgent>();
                    if (testAgent != null)
                    {
                        _logger.LogInformation("Agent工厂正常工作");
                    }
                    else
                    {
                        _logger.LogWarning("Agent工厂创建Agent失败");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "检查Agent工厂状态失败");
                }
            }
        }

        /// <summary>
        /// 生成角色
        /// </summary>
        /// <param name="parameters">生成参数</param>
        /// <returns>生成结果</returns>
        public async Task<AIAssistantResult> GenerateCharacterAsync(Dictionary<string, object> parameters)
        {
            var startTime = DateTime.Now;
            var functionName = "GenerateCharacter";

            try
            {
                _logger.LogInformation("开始AI生成角色");

                // 创建Director Agent
                var directorAgent = _agentFactory.CreateAgent<DirectorAgent>();

                // 订阅思维链事件
                directorAgent.ThinkingChainUpdated += OnThinkingChainUpdated;

                // 执行角色生成任务
                var result = await directorAgent.ExecuteAsync("GenerateCharacter", parameters);

                var executionTime = DateTime.Now - startTime;
                var message = result.Metadata.TryGetValue("Message", out var msgObj) ? msgObj?.ToString() : "AI生成角色完成";

                var aiResult = new AIAssistantResult
                {
                    IsSuccess = result.IsSuccess,
                    Data = result.Data,
                    Message = message,
                    ThinkingChainId = directorAgent.CurrentThinkingChain?.Id.ToString(),
                    ExecutionTime = executionTime
                };

                // 记录使用统计
                _statisticsService.RecordUsage(functionName, result.IsSuccess, executionTime, 0, message);

                return aiResult;
            }
            catch (Exception ex)
            {
                var executionTime = DateTime.Now - startTime;
                _logger.LogError(ex, "AI生成角色失败");

                // 记录失败统计
                _statisticsService.RecordUsage(functionName, false, executionTime, 0, ex.Message);

                return new AIAssistantResult
                {
                    IsSuccess = false,
                    Message = $"AI生成角色失败: {ex.Message}",
                    ExecutionTime = executionTime
                };
            }
        }

        /// <summary>
        /// 优化角色
        /// </summary>
        /// <param name="parameters">优化参数</param>
        /// <returns>优化结果</returns>
        public async Task<AIAssistantResult> OptimizeCharacterAsync(Dictionary<string, object> parameters)
        {
            try
            {
                _logger.LogInformation("开始AI优化角色");

                var editorAgent = _agentFactory.CreateAgent<EditorAgent>();
                editorAgent.ThinkingChainUpdated += OnThinkingChainUpdated;

                var result = await editorAgent.ExecuteAsync("OptimizeCharacter", parameters);
                var message = result.Metadata.TryGetValue("Message", out var msgObj) ? msgObj?.ToString() : "AI优化角色完成";

                return new AIAssistantResult
                {
                    IsSuccess = result.IsSuccess,
                    Data = result.Data,
                    Message = message,
                    ThinkingChainId = editorAgent.CurrentThinkingChain?.Id.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI优化角色失败");
                return new AIAssistantResult
                {
                    IsSuccess = false,
                    Message = $"AI优化角色失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 分析角色关系
        /// </summary>
        /// <param name="parameters">分析参数</param>
        /// <returns>分析结果</returns>
        public async Task<AIAssistantResult> AnalyzeCharacterRelationshipsAsync(Dictionary<string, object> parameters)
        {
            try
            {
                _logger.LogInformation("开始AI分析角色关系");

                var criticAgent = _agentFactory.CreateAgent<CriticAgent>();
                criticAgent.ThinkingChainUpdated += OnThinkingChainUpdated;

                var result = await criticAgent.ExecuteAsync("AnalyzeCharacterRelationships", parameters);
                var message = result.Metadata.TryGetValue("Message", out var msgObj) ? msgObj?.ToString() : "AI分析角色关系完成";

                return new AIAssistantResult
                {
                    IsSuccess = result.IsSuccess,
                    Data = result.Data,
                    Message = message,
                    ThinkingChainId = criticAgent.CurrentThinkingChain?.Id.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI分析角色关系失败");
                return new AIAssistantResult
                {
                    IsSuccess = false,
                    Message = $"AI分析角色关系失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 生成剧情
        /// </summary>
        /// <param name="parameters">生成参数</param>
        /// <returns>生成结果</returns>
        public async Task<AIAssistantResult> GeneratePlotAsync(Dictionary<string, object> parameters)
        {
            try
            {
                _logger.LogInformation("开始AI生成剧情");

                var directorAgent = _agentFactory.CreateAgent<DirectorAgent>();
                directorAgent.ThinkingChainUpdated += OnThinkingChainUpdated;

                var result = await directorAgent.ExecuteAsync("GeneratePlot", parameters);
                var message = result.Metadata.TryGetValue("Message", out var msgObj) ? msgObj?.ToString() : "AI生成剧情完成";

                return new AIAssistantResult
                {
                    IsSuccess = result.IsSuccess,
                    Data = result.Data,
                    Message = message,
                    ThinkingChainId = directorAgent.CurrentThinkingChain?.Id.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI生成剧情失败");
                return new AIAssistantResult
                {
                    IsSuccess = false,
                    Message = $"AI生成剧情失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 优化剧情
        /// </summary>
        /// <param name="parameters">优化参数</param>
        /// <returns>优化结果</returns>
        public async Task<AIAssistantResult> OptimizePlotAsync(Dictionary<string, object> parameters)
        {
            try
            {
                _logger.LogInformation("开始AI优化剧情");

                var editorAgent = _agentFactory.CreateAgent<EditorAgent>();
                editorAgent.ThinkingChainUpdated += OnThinkingChainUpdated;

                var result = await editorAgent.ExecuteAsync("OptimizePlot", parameters);
                var message = result.Metadata.TryGetValue("Message", out var msgObj) ? msgObj?.ToString() : "AI优化剧情完成";

                return new AIAssistantResult
                {
                    IsSuccess = result.IsSuccess,
                    Data = result.Data,
                    Message = message,
                    ThinkingChainId = editorAgent.CurrentThinkingChain?.Id.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI优化剧情失败");
                return new AIAssistantResult
                {
                    IsSuccess = false,
                    Message = $"AI优化剧情失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 检查剧情连贯性
        /// </summary>
        /// <param name="parameters">检查参数</param>
        /// <returns>检查结果</returns>
        public async Task<AIAssistantResult> CheckPlotContinuityAsync(Dictionary<string, object> parameters)
        {
            try
            {
                _logger.LogInformation("开始AI检查剧情连贯性");

                var criticAgent = _agentFactory.CreateAgent<CriticAgent>();
                criticAgent.ThinkingChainUpdated += OnThinkingChainUpdated;

                var result = await criticAgent.ExecuteAsync("CheckPlotContinuity", parameters);
                var message = result.Metadata.TryGetValue("Message", out var msgObj) ? msgObj?.ToString() : "AI检查剧情连贯性完成";

                return new AIAssistantResult
                {
                    IsSuccess = result.IsSuccess,
                    Data = result.Data,
                    Message = message,
                    ThinkingChainId = criticAgent.CurrentThinkingChain?.Id.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI检查剧情连贯性失败");
                return new AIAssistantResult
                {
                    IsSuccess = false,
                    Message = $"AI检查剧情连贯性失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 获取剧情建议
        /// </summary>
        /// <param name="parameters">请求参数</param>
        /// <returns>建议结果</returns>
        public async Task<AIAssistantResult> GetPlotSuggestionsAsync(Dictionary<string, object> parameters)
        {
            try
            {
                _logger.LogInformation("开始AI获取剧情建议");

                var directorAgent = _agentFactory.CreateAgent<DirectorAgent>();
                directorAgent.ThinkingChainUpdated += OnThinkingChainUpdated;

                var result = await directorAgent.ExecuteAsync("GetPlotSuggestions", parameters);
                var message = result.Metadata.TryGetValue("Message", out var msgObj) ? msgObj?.ToString() : "AI获取剧情建议完成";

                return new AIAssistantResult
                {
                    IsSuccess = result.IsSuccess,
                    Data = result.Data,
                    Message = message,
                    ThinkingChainId = directorAgent.CurrentThinkingChain?.Id.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI获取剧情建议失败");
                return new AIAssistantResult
                {
                    IsSuccess = false,
                    Message = $"AI获取剧情建议失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 生成章节内容
        /// </summary>
        /// <param name="parameters">生成参数</param>
        /// <returns>生成结果</returns>
        public async Task<AIAssistantResult> GenerateChapterAsync(Dictionary<string, object> parameters)
        {
            var startTime = DateTime.Now;

            try
            {
                _logger.LogInformation("开始AI生成章节内容");

                // 创建Writer Agent
                var writerAgent = _agentFactory.CreateAgent<WriterAgent>();

                // 订阅思维链事件
                writerAgent.ThinkingChainUpdated += OnThinkingChainUpdated;

                // 执行章节生成任务
                var result = await writerAgent.ExecuteAsync("GenerateChapterContent", parameters);

                var executionTime = DateTime.Now - startTime;
                var message = result.Metadata.TryGetValue("Message", out var msgObj) ? msgObj?.ToString() : "AI生成章节内容完成";

                var aiResult = new AIAssistantResult
                {
                    IsSuccess = result.IsSuccess,
                    Data = result.Data,
                    Message = message,
                    ThinkingChainId = writerAgent.CurrentThinkingChain?.Id.ToString(),
                    ExecutionTime = executionTime
                };

                // 记录统计信息
                _statisticsService.RecordUsage("GenerateChapter", result.IsSuccess, executionTime);

                return aiResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI生成章节内容失败");
                return new AIAssistantResult
                {
                    IsSuccess = false,
                    Message = $"AI生成章节内容失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 续写章节内容
        /// </summary>
        /// <param name="parameters">续写参数</param>
        /// <returns>续写结果</returns>
        public async Task<AIAssistantResult> ContinueChapterAsync(Dictionary<string, object> parameters)
        {
            var startTime = DateTime.Now;

            try
            {
                _logger.LogInformation("开始AI续写章节内容");

                // 创建Writer Agent
                var writerAgent = _agentFactory.CreateAgent<WriterAgent>();

                // 订阅思维链事件
                writerAgent.ThinkingChainUpdated += OnThinkingChainUpdated;

                // 执行章节续写任务
                var result = await writerAgent.ExecuteAsync("ContinueChapter", parameters);

                var executionTime = DateTime.Now - startTime;
                var message = result.Metadata.TryGetValue("Message", out var msgObj) ? msgObj?.ToString() : "AI续写章节内容完成";

                var aiResult = new AIAssistantResult
                {
                    IsSuccess = result.IsSuccess,
                    Data = result.Data,
                    Message = message,
                    ThinkingChainId = writerAgent.CurrentThinkingChain?.Id.ToString(),
                    ExecutionTime = executionTime
                };

                // 记录统计信息
                _statisticsService.RecordUsage("ContinueChapter", result.IsSuccess, executionTime);

                return aiResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI续写章节内容失败");
                return new AIAssistantResult
                {
                    IsSuccess = false,
                    Message = $"AI续写章节内容失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 润色文本内容
        /// </summary>
        /// <param name="parameters">润色参数</param>
        /// <returns>润色结果</returns>
        public async Task<AIAssistantResult> PolishTextAsync(Dictionary<string, object> parameters)
        {
            var startTime = DateTime.Now;

            try
            {
                _logger.LogInformation("开始AI润色文本内容");

                // 创建Editor Agent
                var editorAgent = _agentFactory.CreateAgent<EditorAgent>();

                // 订阅思维链事件
                editorAgent.ThinkingChainUpdated += OnThinkingChainUpdated;

                // 执行文本润色任务
                var result = await editorAgent.ExecuteAsync("PolishText", parameters);

                var executionTime = DateTime.Now - startTime;
                var message = result.Metadata.TryGetValue("Message", out var msgObj) ? msgObj?.ToString() : "AI润色文本内容完成";

                var aiResult = new AIAssistantResult
                {
                    IsSuccess = result.IsSuccess,
                    Data = result.Data,
                    Message = message,
                    ThinkingChainId = editorAgent.CurrentThinkingChain?.Id.ToString(),
                    ExecutionTime = executionTime
                };

                // 记录统计信息
                _statisticsService.RecordUsage("PolishText", result.IsSuccess, executionTime);

                return aiResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI润色文本内容失败");
                return new AIAssistantResult
                {
                    IsSuccess = false,
                    Message = $"AI润色文本内容失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 生成小说大纲
        /// </summary>
        /// <param name="parameters">生成参数</param>
        /// <returns>生成结果</returns>
        public async Task<AIAssistantResult> GenerateOutlineAsync(Dictionary<string, object> parameters)
        {
            var startTime = DateTime.Now;
            var functionName = "GenerateOutline";

            try
            {
                _logger.LogInformation("开始AI生成小说大纲");

                // 创建Director Agent
                var directorAgent = _agentFactory.CreateAgent<DirectorAgent>();

                // 订阅思维链事件
                directorAgent.ThinkingChainUpdated += OnThinkingChainUpdated;

                // 执行大纲生成任务
                var result = await directorAgent.ExecuteAsync("GenerateOutline", parameters);

                var executionTime = DateTime.Now - startTime;
                var message = result.Metadata.TryGetValue("Message", out var msgObj) ? msgObj?.ToString() : "AI生成小说大纲完成";

                var aiResult = new AIAssistantResult
                {
                    IsSuccess = result.IsSuccess,
                    Data = result.Data,
                    Message = message,
                    ThinkingChainId = directorAgent.CurrentThinkingChain?.Id.ToString(),
                    ExecutionTime = executionTime
                };

                // 记录使用统计
                _statisticsService.RecordUsage(functionName, result.IsSuccess, executionTime, 0, message);

                return aiResult;
            }
            catch (Exception ex)
            {
                var executionTime = DateTime.Now - startTime;
                _logger.LogError(ex, "AI生成小说大纲失败");

                // 记录失败统计
                _statisticsService.RecordUsage(functionName, false, executionTime, 0, ex.Message);

                return new AIAssistantResult
                {
                    IsSuccess = false,
                    Message = $"AI生成小说大纲失败: {ex.Message}",
                    ExecutionTime = executionTime
                };
            }
        }

        /// <summary>
        /// 显示错误消息
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="title">标题</param>
        public void ShowError(string message, string title = "AI助手错误")
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }

        /// <summary>
        /// 显示成功消息
        /// </summary>
        /// <param name="message">成功消息</param>
        /// <param name="title">标题</param>
        public void ShowSuccess(string message, string title = "AI助手")
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }

        /// <summary>
        /// 获取使用统计信息
        /// </summary>
        /// <param name="timeRange">时间范围</param>
        /// <returns>统计信息</returns>
        public AIUsageStatistics GetUsageStatistics(TimeRange timeRange = TimeRange.Today)
        {
            return _statisticsService.GetUsageStatistics(timeRange);
        }

        /// <summary>
        /// 获取功能使用排行
        /// </summary>
        /// <param name="timeRange">时间范围</param>
        /// <param name="topCount">返回数量</param>
        /// <returns>功能使用排行</returns>
        public List<FunctionUsageRank> GetFunctionUsageRanking(TimeRange timeRange = TimeRange.Today, int topCount = 10)
        {
            return _statisticsService.GetFunctionUsageRanking(timeRange, topCount);
        }

        /// <summary>
        /// 导出统计数据
        /// </summary>
        /// <param name="timeRange">时间范围</param>
        /// <returns>CSV格式的统计数据</returns>
        public string ExportStatistics(TimeRange timeRange = TimeRange.Today)
        {
            return _statisticsService.ExportStatistics(timeRange);
        }

        /// <summary>
        /// 清除统计数据
        /// </summary>
        /// <param name="olderThan">清除早于指定时间的数据</param>
        public void ClearStatistics(DateTime? olderThan = null)
        {
            _statisticsService.ClearStatistics(olderThan);
        }

        /// <summary>
        /// 思维链更新事件处理
        /// </summary>
        private void OnThinkingChainUpdated(object? sender, ThinkingChain thinkingChain)
        {
            try
            {
                // 显示思维链
                _floatingTextManager.ShowThinkingChain(thinkingChain);
                _logger.LogDebug("思维链已显示: {Title}", thinkingChain.Title);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "显示思维链失败");
            }
        }
    }

    /// <summary>
    /// AI助手结果
    /// </summary>
    public class AIAssistantResult
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
        /// 消息
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// 思维链ID
        /// </summary>
        public string? ThinkingChainId { get; set; }

        /// <summary>
        /// 执行时间
        /// </summary>
        public TimeSpan ExecutionTime { get; set; }
    }
}
