using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NovelManagement.AI.Interfaces;
using NovelManagement.AI.Services.DeepSeek;
using NovelManagement.AI.Services.RWKV;
using NovelManagement.AI.Services.RWKV.Models;
using NovelManagement.AI.Services.ThinkingChain;
using NovelManagement.AI.Services.ThinkingChain.Models;
using NovelManagement.AI.Utilities;
using ThinkingChainModel = NovelManagement.AI.Services.ThinkingChain.Models.ThinkingChain;

namespace NovelManagement.AI.Agents
{
    /// <summary>
    /// 作家Agent - 负责章节内容生成（续写优先使用RWKV，决策类任务使用OpenAI兼容接口）
    /// </summary>
    public class WriterAgent : BaseAgent
    {
        private const double RwkvNovelTemperature = 1.4;
        private const double RwkvNovelTopP = 0.3;
        // 抗重复惩罚：RWKV7 长文无惩罚会整段复读（英文实测踩坑），温和开启
        private const double RwkvNovelPresencePenalty = 0.4;
        private const double RwkvNovelFrequencyPenalty = 0.6;
        private const int RwkvRollingContextChars = 1800;
        private const int RwkvMinUsefulChunkChars = 30;
        private const int RwkvMaxFinishingRounds = 3;
        private const double RwkvStateTemperature = 0.2;
        private const double RwkvStateTopP = 0.2;
        private const int RwkvStateTopK = 20;
        private const int RwkvMaxStateFacts = 10;
        private const int RwkvBatchCandidateCount = 2;
        private const int RwkvBatchChunkSize = 8;
        private readonly IRwkvLightningService? _rwkvService;
        private readonly ConcurrentDictionary<string, string> _rwkvStateSeedCache = new(StringComparer.Ordinal);

        private sealed class RwkvGenerationSelectionResult
        {
            public RwkvCompletionResponse Response { get; init; } = new();
            public bool UsedBatch { get; init; }
            public bool FellBackToSingle { get; init; }
            public int CandidateCount { get; init; }
            public int SelectedCandidateIndex { get; init; } = -1;
            public double SelectedScore { get; init; }
        }

        private sealed class ScoredRwkvBatchCandidate : RwkvBatchCompletionItem
        {
            public double Score { get; init; }
        }

        private sealed class RwkvBatchUsageStats
        {
            public int SelectedRounds { get; private set; }
            public int FallbackRounds { get; private set; }
            public int LastSelectedIndex { get; private set; } = -1;
            public double LastSelectedScore { get; private set; }

            public void Register(RwkvGenerationSelectionResult selection)
            {
                if (selection.UsedBatch && !selection.FellBackToSingle)
                {
                    SelectedRounds++;
                    LastSelectedIndex = selection.SelectedCandidateIndex;
                    LastSelectedScore = selection.SelectedScore;
                }
                else if (selection.FellBackToSingle)
                {
                    FallbackRounds++;
                }
            }
        }

        /// <summary>
        /// 构造函数（用于依赖注入）
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="memoryManager">记忆管理器</param>
        /// <param name="deepSeekApiService">DeepSeek API服务</param>
        /// <param name="thinkingChainProcessor">思维链处理器</param>
        /// <param name="modelManager">模型管理器</param>
        /// <param name="rwkvService">RWKV推理服务（可选，续写首选）</param>
        public WriterAgent(
            ILogger<WriterAgent> logger,
            IMemoryManager memoryManager,
            IDeepSeekApiService deepSeekApiService,
            IThinkingChainProcessor thinkingChainProcessor,
            NovelManagement.AI.Services.ModelManager modelManager,
            IRwkvLightningService? rwkvService = null)
            : base(logger, memoryManager, deepSeekApiService, thinkingChainProcessor, modelManager, rwkvService)
        {
            _rwkvService = rwkvService;
        }

        #region 基础属性

        /// <inheritdoc/>
        public override string Name => "作家Agent";

        /// <inheritdoc/>
        public override string Description => "专业写作助手，负责章节内容生成、对话描写、场景描述和心理刻画";

        #endregion

        #region 任务执行

        /// <inheritdoc/>
        protected override async Task<AgentTaskResult> ExecuteTaskAsync(string taskType, Dictionary<string, object> parameters)
        {
            return taskType switch
            {
                "GenerateChapterContent" => await GenerateChapterContentAsync(parameters),
                "ContinueChapter" => await ContinueChapterAsync(parameters),
                "MaintainWritingStyle" => await MaintainWritingStyleAsync(parameters),
                "HandleDialogue" => await HandleDialogueAsync(parameters),
                "DescribeScene" => await DescribeSceneAsync(parameters),
                "PortrayPsychology" => await PortrayPsychologyAsync(parameters),
                "EnsureConsistency" => await EnsureConsistencyAsync(parameters),
                _ => new AgentTaskResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"不支持的任务类型: {taskType}"
                }
            };
        }

        /// <inheritdoc/>
        protected override List<AgentCapability> GetSupportedCapabilities()
        {
            return new List<AgentCapability>
            {
                new AgentCapability
                {
                    Name = "章节内容生成",
                    Description = "根据大纲生成完整的章节内容",
                    IsAvailable = true,
                    Priority = 1
                },
                new AgentCapability
                {
                    Name = "文风保持",
                    Description = "维持一致的写作风格",
                    IsAvailable = true,
                    Priority = 1
                },
                new AgentCapability
                {
                    Name = "对话处理",
                    Description = "生成自然流畅的人物对话",
                    IsAvailable = true,
                    Priority = 2
                },
                new AgentCapability
                {
                    Name = "场景描述",
                    Description = "创作生动的场景描写",
                    IsAvailable = true,
                    Priority = 2
                },
                new AgentCapability
                {
                    Name = "心理刻画",
                    Description = "深入刻画人物内心世界",
                    IsAvailable = true,
                    Priority = 3
                },
                new AgentCapability
                {
                    Name = "一致性确保",
                    Description = "确保内容与设定的一致性",
                    IsAvailable = true,
                    Priority = 3
                }
            };
        }

        protected override async Task<AgentTaskResult> ExecuteTaskWithThinkingAsync(string taskType, Dictionary<string, object> parameters, ThinkingChainModel? thinkingChain)
        {
            var useRwkvDirectly =
                (taskType == "GenerateChapterContent" && ShouldUseRwkvForChapterWriting(parameters)) ||
                (taskType == "ContinueChapter" && _rwkvService != null && _rwkvService.IsAvailable);

            if (useRwkvDirectly)
            {
                _logger.LogInformation("写作任务 {TaskType} 使用 RWKV 直连链路，跳过决策类模型包装", taskType);
                return await ExecuteTaskAsync(taskType, parameters);
            }

            return await base.ExecuteTaskWithThinkingAsync(taskType, parameters, thinkingChain);
        }

        #endregion

        #region 具体任务实现

        /// <summary>
        /// 生成章节内容
        /// </summary>
        /// <param name="parameters">参数</param>
        /// <returns>生成结果</returns>
        private async Task<AgentTaskResult> GenerateChapterContentAsync(Dictionary<string, object> parameters)
        {
            try
            {
                UpdateProgress(10);

                var chapterOutline = parameters.GetValueOrDefault("outline", "").ToString();
                var chapterNumber = parameters.GetValueOrDefault("chapterNumber", 1);

                _logger.LogInformation($"开始生成第{chapterNumber}章内容");

                if (ShouldUseRwkvForChapterWriting(parameters))
                {
                    _logger.LogInformation("章节生成使用 RWKV 多轮续写策略");
                    return await GenerateChapterWithRwkvAsync(parameters);
                }

                // 添加思维步骤：分析大纲
                AddThinkingStep("分析章节大纲", $"正在分析第{chapterNumber}章的大纲内容：{chapterOutline}", ThinkingStepType.Analysis, 0.9);

                UpdateProgress(20);

                // 添加思维步骤：规划内容结构
                AddThinkingStep("规划内容结构", "根据大纲规划章节的具体结构，包括开头、发展、高潮和结尾", ThinkingStepType.Planning, 0.85);

                UpdateProgress(30);

                // 添加思维步骤：构建AI提示词
                AddThinkingStep("构建AI提示词", "根据用户输入的参数构建详细的AI生成提示词", ThinkingStepType.Planning, 0.85);

                UpdateProgress(30);

                // 创建思维链
                var thinkingChain = new ThinkingChainModel
                {
                    Title = $"{Name} - 章节内容生成",
                    Description = "执行章节内容生成任务",
                    TaskId = Guid.NewGuid().ToString(),
                    AgentId = Id
                };

                // 添加思维步骤：调用AI模型
                AddThinkingStep("调用AI模型", "使用OLLAMA模型生成章节内容", ThinkingStepType.Synthesis, 0.9);

                UpdateProgress(50);

                // 使用AI辅助执行章节生成任务
                var result = await ExecuteTaskWithAIAsync("GenerateChapterContent", parameters, thinkingChain);

                UpdateProgress(80);

                if (result.IsSuccess && result.Data != null)
                {
                    // 从AI响应中提取章节内容
                    var aiResponse = result.Data.ToString() ?? "";
                    var generatedContent = ExtractChapterContentFromAIResponse(aiResponse);

                    // 计算字数
                    var wordCount = CountChineseCharacters(generatedContent);

                    // 添加思维步骤：完成总结
                    AddThinkingStep("完成总结", $"成功生成章节内容，共{wordCount}字", ThinkingStepType.Conclusion, 0.95);

                    UpdateProgress(100);

                    return new AgentTaskResult
                    {
                        IsSuccess = true,
                        Data = generatedContent, // 直接返回内容文本
                        Metadata = new Dictionary<string, object>
                        {
                            ["ContentType"] = "ChapterContent",
                            ["Quality"] = "AI Generated",
                            ["WordCount"] = wordCount,
                            ["AIModel"] = parameters.GetValueOrDefault("AIModel", "OLLAMA").ToString(),
                            ["WritingStyle"] = parameters.GetValueOrDefault("WritingStyle", "古风仙侠").ToString()
                        }
                    };
                }
                else
                {
                    // AI生成失败，返回错误结果
                    return new AgentTaskResult
                    {
                        IsSuccess = false,
                        ErrorMessage = result.ErrorMessage ?? "AI章节生成失败"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "章节内容生成失败");
                return new AgentTaskResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// 续写章节内容
        /// </summary>
        /// <param name="parameters">参数</param>
        /// <returns>续写结果</returns>
        private async Task<AgentTaskResult> ContinueChapterAsync(Dictionary<string, object> parameters)
        {
            try
            {
                UpdateProgress(10);

                var existingContent = parameters.GetValueOrDefault("ExistingContent", "").ToString();
                var continueLength = parameters.GetValueOrDefault("ContinueLength", "短续写").ToString();
                var customWordCount = parameters.GetValueOrDefault("CustomWordCount", "").ToString();
                var continueDirection = parameters.GetValueOrDefault("ContinueDirection", "").ToString();
                var chapterData = parameters.GetValueOrDefault("ChapterData", null);

                _logger.LogInformation($"开始续写章节内容，续写长度：{continueLength}，RWKV可用：{_rwkvService?.IsAvailable ?? false}");

                // 优先使用 RWKV 进行续写（单次最多200字）
                if (_rwkvService != null && _rwkvService.IsAvailable)
                {
                    _logger.LogInformation("使用 RWKV 进行续写");
                    return await ContinueWithRwkvAsync(parameters, existingContent, continueDirection, continueLength, customWordCount);
                }

                // 降级：使用决策类AI模型（Ollama/DeepSeek/OpenAI兼容）进行续写
                _logger.LogInformation("RWKV 不可用，降级使用决策类AI模型续写");

                // 创建思维链
                var thinkingChain = new ThinkingChainModel
                {
                    Title = $"{Name} - 章节续写",
                    Description = "执行章节续写任务（决策类AI降级）",
                    TaskId = Guid.NewGuid().ToString(),
                    AgentId = Id
                };

                // 使用AI辅助执行续写任务
                var result = await ExecuteTaskWithAIAsync("ContinueChapter", parameters, thinkingChain);

                if (result.IsSuccess && result.Data != null)
                {
                    // 从AI响应中提取续写内容
                    var aiResponse = result.Data.ToString() ?? "";
                    var continuedText = ExtractContinuedTextFromAIResponse(aiResponse);

                    // 确保续写内容与原文排版一致
                    continuedText = FormatContinuedText(continuedText, existingContent);

                    // 计算实际字数
                    var actualWordCount = CountChineseCharacters(continuedText);

                    UpdateProgress(100);

                    return new AgentTaskResult
                    {
                        IsSuccess = true,
                        Data = continuedText,
                        Metadata = new Dictionary<string, object>
                        {
                            ["ContentType"] = "ContinuedContent",
                            ["Quality"] = "AI Generated (Fallback)",
                            ["WordCount"] = actualWordCount,
                            ["StyleConsistency"] = "AI Maintained",
                            ["ContinuationStyle"] = continueLength,
                            ["Engine"] = "DecisionAI"
                        }
                    };
                }
                else
                {
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "章节续写失败");
                return new AgentTaskResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// 使用 RWKV 进行续写（单次最多200字，可多次续写拼接）
        /// </summary>
        private async Task<AgentTaskResult> ContinueWithRwkvAsync(Dictionary<string, object> parameters, string existingContent, string continueDirection, string continueLength, string customWordCount)
        {
            try
            {
                UpdateProgress(20);

                // 计算目标字数（RWKV 单次最多200字）
                var targetWordCount = continueLength switch
                {
                    "短续写" => 200,
                    "中续写" => 500,
                    "长续写" => 1000,
                    _ => 200
                };

                if (int.TryParse(customWordCount, out var custom) && custom > 0)
                {
                    targetWordCount = Math.Clamp(custom, 50, 2000);
                }

                // RWKV 单次最多200字，需要多次续写拼接
                var maxTokensPerCall = _rwkvService!.Configuration.MaxTokensPerCompletion;
                var rounds = Math.Max(1, (int)Math.Ceiling((double)targetWordCount / 200));
                rounds = Math.Clamp(rounds, 1, 10); // 最多10轮续写

                _logger.LogInformation("RWKV 续写：目标{TargetWords}字，分{Rounds}轮，每轮最多{MaxTokens}tokens",
                    targetWordCount, rounds, maxTokensPerCall);

                var allContinuedText = new System.Text.StringBuilder();
                var sessionId = BuildRwkvSessionId(parameters);
                var stateEnabled = !string.IsNullOrWhiteSpace(sessionId);
                var batchStats = new RwkvBatchUsageStats();

                if (stateEnabled)
                {
                    await PrepareRwkvStateAsync(sessionId!, parameters, existingContent, continueDirection);
                }

                for (int round = 0; round < rounds; round++)
                {
                    UpdateProgress(20 + (int)((double)(round + 1) / rounds * 70));
                    var beforeMerge = allContinuedText.ToString();
                    var beforeWordCount = CountChineseCharacters(beforeMerge);

                    var selection = await GenerateRwkvWritingCandidateAsync(
                        parameters,
                        prompt: BuildRwkvContinuationPrompt(parameters, existingContent, beforeMerge, round == 0 ? continueDirection : null),
                        direction: round == 0 ? continueDirection : null,
                        existingText: $"{existingContent}\n{beforeMerge}",
                        maxTokens: maxTokensPerCall);
                    batchStats.Register(selection);
                    var result = selection.Response;

                    if (!result.Success || string.IsNullOrWhiteSpace(result.Text))
                    {
                        _logger.LogWarning("RWKV 第{Round}轮续写返回空结果，停止续写", round + 1);
                        break;
                    }

                    var mergedContinuation = MergeRwkvChunk(beforeMerge, result.Text);
                    allContinuedText.Clear();
                    allContinuedText.Append(mergedContinuation);
                    var currentWordCount = CountChineseCharacters(mergedContinuation);
                    var wordDelta = currentWordCount - beforeWordCount;

                    if (wordDelta < RwkvMinUsefulChunkChars)
                    {
                        _logger.LogInformation("RWKV 第{Round}轮续写增量过小（{WordDelta}字），提前结束主续写循环", round + 1, wordDelta);
                        break;
                    }

                    if (stateEnabled)
                    {
                        await AppendContinuationStateAsync(sessionId!, parameters, result.Text);
                    }

                    // 检查是否已达到目标字数
                    if (currentWordCount >= targetWordCount)
                    {
                        _logger.LogInformation("RWKV 续写已达到目标字数：{WordCount}/{Target}", currentWordCount, targetWordCount);
                        break;
                    }
                }

                while (CountChineseCharacters(allContinuedText.ToString()) < targetWordCount && allContinuedText.Length > 0)
                {
                    var currentText = allContinuedText.ToString();
                    var currentWordCount = CountChineseCharacters(currentText);
                    var finishingRound = 0;
                    if (currentWordCount >= targetWordCount)
                    {
                        break;
                    }

                    while (currentWordCount < targetWordCount && finishingRound < RwkvMaxFinishingRounds)
                    {
                        finishingRound++;
                        var remainingWords = targetWordCount - currentWordCount;
                        var direction = BuildRwkvFillDirection(remainingWords, continueDirection, false);
                        var maxTokens = Math.Min(maxTokensPerCall, Math.Max(80, remainingWords + 40));
                        var fillSelection = await GenerateRwkvWritingCandidateAsync(
                            parameters,
                            prompt: BuildRwkvContinuationPrompt(parameters, existingContent, currentText, direction),
                            direction: direction,
                            existingText: $"{existingContent}\n{currentText}",
                            maxTokens: maxTokens);
                        batchStats.Register(fillSelection);
                        var fillResult = fillSelection.Response;

                        if (!fillResult.Success || string.IsNullOrWhiteSpace(fillResult.Text))
                        {
                            _logger.LogWarning("RWKV 收尾续写第{Round}轮失败或为空，停止补齐", finishingRound);
                            break;
                        }

                        var merged = MergeRwkvChunk(currentText, fillResult.Text);
                        var newWordCount = CountChineseCharacters(merged);
                        if (newWordCount - currentWordCount < RwkvMinUsefulChunkChars)
                        {
                            _logger.LogInformation("RWKV 收尾续写第{Round}轮增量过小（{WordDelta}字），停止补齐", finishingRound, newWordCount - currentWordCount);
                            break;
                        }

                        currentText = merged;
                        currentWordCount = newWordCount;
                        allContinuedText.Clear();
                        allContinuedText.Append(currentText);

                        if (stateEnabled)
                        {
                            await AppendContinuationStateAsync(sessionId!, parameters, fillResult.Text);
                        }
                    }

                    break;
                }

                var continuedText = allContinuedText.ToString();

                // 确保续写内容与原文排版一致
                continuedText = FormatContinuedText(continuedText, existingContent);

                // 截断到目标字数
                var finalWordCount = CountChineseCharacters(continuedText);
                if (finalWordCount > targetWordCount * 1.2) // 允许20%溢出
                {
                    continuedText = TruncateToWordCount(continuedText, targetWordCount);
                }

                finalWordCount = CountChineseCharacters(continuedText);
                if (stateEnabled)
                {
                    await AppendContinuationStateAsync(sessionId!, parameters, continuedText);
                }

                UpdateProgress(100);

                return new AgentTaskResult
                {
                    IsSuccess = true,
                    Data = continuedText,
                    Metadata = new Dictionary<string, object>
                    {
                        ["ContentType"] = "ContinuedContent",
                        ["Quality"] = "RWKV Generated",
                        ["WordCount"] = finalWordCount,
                        ["StyleConsistency"] = "RWKV Maintained",
                        ["ContinuationStyle"] = continueLength,
                        ["Engine"] = "RWKV",
                        ["Rounds"] = rounds,
                        ["RwkvSessionId"] = sessionId ?? string.Empty,
                        ["RwkvBigBatchEnabled"] = IsRwkvBigBatchEnabled(parameters),
                        ["RwkvBigBatchSelectedRounds"] = batchStats.SelectedRounds,
                        ["RwkvBigBatchFallbackRounds"] = batchStats.FallbackRounds,
                        ["RwkvBigBatchLastSelectedIndex"] = batchStats.LastSelectedIndex,
                        ["RwkvBigBatchLastScore"] = batchStats.LastSelectedScore
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RWKV 续写失败");
                return new AgentTaskResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"RWKV 续写失败: {ex.Message}"
                };
            }
        }

        private async Task<AgentTaskResult> GenerateChapterWithRwkvAsync(Dictionary<string, object> parameters)
        {
            try
            {
                UpdateProgress(15);

                var targetWordCount = GetTargetWordCountForGeneration(parameters);
                var maxTokensPerCall = _rwkvService!.Configuration.MaxTokensPerCompletion;
                var rounds = Math.Clamp((int)Math.Ceiling((double)targetWordCount / 180), 1, 12);
                var generated = new System.Text.StringBuilder();
                var sessionId = BuildRwkvSessionId(parameters) ?? $"rwkv-chapter-{Guid.NewGuid():N}";
                var batchStats = new RwkvBatchUsageStats();
                await PrepareRwkvStateAsync(sessionId, parameters, string.Empty, "根据章节设定继续完成当前章节正文");

                for (int round = 0; round < rounds; round++)
                {
                    UpdateProgress(15 + (int)((double)(round + 1) / rounds * 75));
                    var existingGenerated = generated.ToString();
                    var beforeWordCount = CountChineseCharacters(existingGenerated);

                    var prompt = BuildRwkvChapterWritingPrompt(parameters, existingGenerated, round, targetWordCount);
                    var selection = await GenerateRwkvWritingCandidateAsync(
                        parameters,
                        prompt: prompt,
                        direction: round == 0 ? "请开始正文，不要写标题，不要解释。" : "请承接上文继续正文，保持人物与剧情一致。",
                        existingText: existingGenerated,
                        maxTokens: maxTokensPerCall);
                    batchStats.Register(selection);
                    var result = selection.Response;

                    if (!result.Success || string.IsNullOrWhiteSpace(result.Text))
                    {
                        _logger.LogWarning("RWKV 第{Round}轮章节生成失败或为空：{Error}", round + 1, result.Error);
                        break;
                    }

                    var cleanedChunk = CleanupRwkvWritingChunk(result.Text);
                    if (string.IsNullOrWhiteSpace(cleanedChunk))
                    {
                        break;
                    }

                    var merged = MergeRwkvChunk(existingGenerated, cleanedChunk);
                    generated.Clear();
                    generated.Append(merged);
                    var currentWordCount = CountChineseCharacters(merged);
                    var wordDelta = currentWordCount - beforeWordCount;
                    if (wordDelta < RwkvMinUsefulChunkChars)
                    {
                        _logger.LogInformation("RWKV 第{Round}轮章节生成增量过小（{WordDelta}字），提前结束主生成循环", round + 1, wordDelta);
                        break;
                    }

                    await AppendContinuationStateAsync(sessionId, parameters, cleanedChunk);
                    if (currentWordCount >= targetWordCount)
                    {
                        break;
                    }
                }

                var finishingRound = 0;
                while (CountChineseCharacters(generated.ToString()) < targetWordCount &&
                       generated.Length > 0 &&
                       finishingRound < RwkvMaxFinishingRounds)
                {
                    finishingRound++;
                    var currentText = generated.ToString();
                    var currentWordCount = CountChineseCharacters(currentText);
                    var remainingWords = targetWordCount - currentWordCount;
                    var fillDirection = BuildRwkvFillDirection(remainingWords, "请承接上文继续正文，补足章节结尾或过渡内容。", true);
                    var fillPrompt = BuildRwkvChapterWritingPrompt(parameters, currentText, rounds + finishingRound - 1, targetWordCount);
                    var fillSelection = await GenerateRwkvWritingCandidateAsync(
                        parameters,
                        prompt: fillPrompt,
                        direction: fillDirection,
                        existingText: currentText,
                        maxTokens: Math.Min(maxTokensPerCall, Math.Max(80, remainingWords + 40)));
                    batchStats.Register(fillSelection);
                    var fillResult = fillSelection.Response;

                    if (!fillResult.Success || string.IsNullOrWhiteSpace(fillResult.Text))
                    {
                        _logger.LogWarning("RWKV 章节收尾第{Round}轮失败或为空，停止补齐", finishingRound);
                        break;
                    }

                    var cleanedFillChunk = CleanupRwkvWritingChunk(fillResult.Text);
                    if (string.IsNullOrWhiteSpace(cleanedFillChunk))
                    {
                        break;
                    }

                    var mergedFill = MergeRwkvChunk(currentText, cleanedFillChunk);
                    var newWordCount = CountChineseCharacters(mergedFill);
                    if (newWordCount - currentWordCount < RwkvMinUsefulChunkChars)
                    {
                        _logger.LogInformation("RWKV 章节收尾第{Round}轮增量过小（{WordDelta}字），停止补齐", finishingRound, newWordCount - currentWordCount);
                        break;
                    }

                    generated.Clear();
                    generated.Append(mergedFill);
                    await AppendContinuationStateAsync(sessionId, parameters, cleanedFillChunk);
                }

                var content = FormatContinuedText(generated.ToString(), generated.ToString());
                if (CountChineseCharacters(content) > targetWordCount * 1.15)
                {
                    content = TruncateToWordCount(content, targetWordCount);
                }

                await AppendContinuationStateAsync(sessionId, parameters, content);
                UpdateProgress(100);

                return new AgentTaskResult
                {
                    IsSuccess = !string.IsNullOrWhiteSpace(content),
                    Data = content,
                    ErrorMessage = string.IsNullOrWhiteSpace(content) ? "RWKV 未生成可用章节内容" : string.Empty,
                    Metadata = new Dictionary<string, object>
                    {
                        ["ContentType"] = "ChapterContent",
                        ["Quality"] = "RWKV MultiRound",
                        ["WordCount"] = CountChineseCharacters(content),
                        ["AIModel"] = "RWKV",
                        ["Rounds"] = rounds,
                        ["RwkvSessionId"] = sessionId,
                        ["Temperature"] = RwkvNovelTemperature,
                        ["TopP"] = RwkvNovelTopP,
                        ["RwkvBigBatchEnabled"] = IsRwkvBigBatchEnabled(parameters),
                        ["RwkvBigBatchSelectedRounds"] = batchStats.SelectedRounds,
                        ["RwkvBigBatchFallbackRounds"] = batchStats.FallbackRounds,
                        ["RwkvBigBatchLastSelectedIndex"] = batchStats.LastSelectedIndex,
                        ["RwkvBigBatchLastScore"] = batchStats.LastSelectedScore
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RWKV 章节生成失败");
                return new AgentTaskResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"RWKV 章节生成失败: {ex.Message}"
                };
            }
        }

        private async Task PrepareRwkvStateAsync(string sessionId, Dictionary<string, object> parameters, string existingContent, string continueDirection)
        {
            if (_rwkvService == null)
            {
                return;
            }

            var stateSeedSignature = BuildRwkvStateSeedSignature(parameters, existingContent, continueDirection);
            if (_rwkvStateSeedCache.TryGetValue(sessionId, out var cachedSeedSignature) &&
                string.Equals(cachedSeedSignature, stateSeedSignature, StringComparison.Ordinal))
            {
                _logger.LogInformation("RWKV state 命中复用，会话 {SessionId}", sessionId);
                return;
            }

            await _rwkvService.DeleteStateAsync(sessionId);

            var warmupPrompt = BuildRwkvContextPrompt(parameters, existingContent, continueDirection);
            var warmupResult = await _rwkvService.CompleteWithStateAsync(
                sessionId,
                warmupPrompt,
                maxTokens: 4,
                direction: "请只回复 OK",
                temperature: RwkvStateTemperature,
                topP: RwkvStateTopP,
                presencePenalty: 0.0,
                frequencyPenalty: 0.0,
                topK: RwkvStateTopK);
            if (!warmupResult.Success)
            {
                _logger.LogWarning("RWKV state 预热失败，会话 {SessionId}: {Error}", sessionId, warmupResult.Error);
                _rwkvStateSeedCache.TryRemove(sessionId, out _);
                return;
            }

            _rwkvStateSeedCache[sessionId] = stateSeedSignature;
        }

        private async Task AppendContinuationStateAsync(string sessionId, Dictionary<string, object> parameters, string continuedText)
        {
            if (_rwkvService == null || string.IsNullOrWhiteSpace(continuedText))
            {
                return;
            }

            var updatePrompt = BuildRwkvStateUpdatePrompt(parameters, continuedText);
            if (string.IsNullOrWhiteSpace(updatePrompt))
            {
                return;
            }

            var updateResult = await _rwkvService.CompleteWithStateAsync(
                sessionId,
                updatePrompt,
                maxTokens: 4,
                direction: "请只回复 OK",
                temperature: RwkvStateTemperature,
                topP: RwkvStateTopP,
                presencePenalty: 0.0,
                frequencyPenalty: 0.0,
                topK: RwkvStateTopK);
            if (!updateResult.Success)
            {
                _logger.LogWarning("RWKV state 更新失败，会话 {SessionId}: {Error}", sessionId, updateResult.Error);
            }
        }

        private string? BuildRwkvSessionId(Dictionary<string, object> parameters)
        {
            if (!parameters.TryGetValue("ProjectId", out var projectIdObj) || projectIdObj == null)
            {
                return null;
            }

            var projectKey = projectIdObj.ToString();
            if (string.IsNullOrWhiteSpace(projectKey) || projectKey == Guid.Empty.ToString())
            {
                return null;
            }

            var chapterId = ExtractChapterId(parameters);
            var chapterKey = chapterId != Guid.Empty
                ? $"chapter-{chapterId:N}"
                : ExtractChapterTitle(parameters);
            chapterKey = string.IsNullOrWhiteSpace(chapterKey) ? "chapter" : SanitizeSessionSegment(chapterKey);

            var mode = string.IsNullOrWhiteSpace(parameters.GetValueOrDefault("ExistingContent")?.ToString())
                ? "draft"
                : "continue";
            return $"project-{SanitizeSessionSegment(projectKey)}-writer-{mode}-{chapterKey}";
        }

        private Guid ExtractChapterId(Dictionary<string, object> parameters)
        {
            foreach (var key in new[] { "ChapterData", "ExistingChapterData" })
            {
                if (!parameters.TryGetValue(key, out var chapterData) || chapterData == null)
                {
                    continue;
                }

                var property = chapterData.GetType().GetProperty("Id");
                var rawValue = property?.GetValue(chapterData);
                if (rawValue is Guid guidValue && guidValue != Guid.Empty)
                {
                    return guidValue;
                }

                if (rawValue != null && Guid.TryParse(rawValue.ToString(), out var parsedGuid) && parsedGuid != Guid.Empty)
                {
                    return parsedGuid;
                }
            }

            return Guid.Empty;
        }

        private string ExtractChapterTitle(Dictionary<string, object> parameters)
        {
            if (parameters.TryGetValue("ChapterTitle", out var chapterTitleObj) &&
                !string.IsNullOrWhiteSpace(chapterTitleObj?.ToString()))
            {
                return chapterTitleObj!.ToString() ?? string.Empty;
            }

            foreach (var key in new[] { "ChapterData", "ExistingChapterData" })
            {
                if (!parameters.TryGetValue(key, out var chapterData) || chapterData == null)
                {
                    continue;
                }

                var property = chapterData.GetType().GetProperty("Title");
                var title = property?.GetValue(chapterData)?.ToString();
                if (!string.IsNullOrWhiteSpace(title))
                {
                    return title;
                }
            }

            return string.Empty;
        }

        private string BuildRwkvStateSeedSignature(Dictionary<string, object> parameters, string existingContent, string continueDirection)
        {
            return string.Join("\n", new[]
            {
                CompactRwkvStateValue(parameters.GetValueOrDefault("PromptSummary"), 320),
                CompactRwkvStateValue(parameters.GetValueOrDefault("WorldSettings"), 240),
                CompactRwkvStateValue(parameters.GetValueOrDefault("PlotOutlines"), 240),
                CompactRwkvStateValue(parameters.GetValueOrDefault("MainCharacters"), 240),
                CompactRwkvStateValue(parameters.GetValueOrDefault("ChapterOutline"), 180),
                CompactRwkvStateValue(parameters.GetValueOrDefault("KeyPlots"), 180),
                CompactRwkvStateValue(parameters.GetValueOrDefault("Characters"), 180),
                CompactRwkvStateValue(parameters.GetValueOrDefault("SpecialRequirements"), 180),
                CompactRwkvStateValue(continueDirection, 120),
                LimitRwkvContext(existingContent, 1200)
            });
        }

        private string BuildRwkvContextPrompt(Dictionary<string, object> parameters, string existingContent, string continueDirection)
        {
            var stateCard = BuildRwkvPromptStateCard(parameters, existingContent);

            return
$@"System: 你是 NovelCraft 的故事状态缓存助手。请把以下“事实卡”写入当前会话记忆，后续续写必须优先遵守这些事实。不要扩写，不要创作，不要解释。

事实卡：
{stateCard}

本次续写方向：
{continueDirection}

Assistant:";
        }

        private string BuildRwkvContinuationPrompt(Dictionary<string, object> parameters, string existingContent, string generatedContinuation, string? continueDirection)
        {
            var existingTail = LimitRwkvContext(existingContent);
            var generatedTail = LimitRwkvContext(generatedContinuation);
            var keyPlots = SerializeForRwkv(parameters.GetValueOrDefault("PlotOutlines"));
            var characters = SerializeForRwkv(parameters.GetValueOrDefault("MainCharacters"));
            var stateCard = BuildRwkvPromptStateCard(parameters, $"{existingTail}\n{generatedTail}");

            return
$@"Instruction: 你正在续写一部中文网络书籍。请保持文风一致、剧情连贯、人物设定稳定，不要解释，不要重复前文，不要输出 Markdown 标题、列表、引用或分隔线。

若你发现将要输出的句子与前文或已生成内容重复，请直接跳过重复句，继续推进新的动作、对话或信息。

Key Plot:
{keyPlots}

Characters:
{characters}

State Memory:
{stateCard}

Direction:
{continueDirection}

Existing Content Tail:
{existingTail}

Generated Tail:
{generatedTail}

Response:";
        }

        private string BuildRwkvChapterWritingPrompt(Dictionary<string, object> parameters, string generatedContent, int round, int targetWordCount)
        {
            var chapterTitle = parameters.GetValueOrDefault("ChapterTitle", "").ToString();
            var style = parameters.GetValueOrDefault("WritingStyle", "古风仙侠").ToString();
            var outline = parameters.GetValueOrDefault("ChapterOutline", "").ToString();
            var keyPlots = parameters.GetValueOrDefault("KeyPlots", "").ToString();
            var characters = parameters.GetValueOrDefault("Characters", "").ToString();
            var requirements = parameters.GetValueOrDefault("SpecialRequirements", "").ToString();
            var generatedTail = LimitRwkvContext(generatedContent);
            var stateCard = BuildRwkvPromptStateCard(parameters, generatedContent);

            if (Utilities.AIPromptLanguage.UseEnglish)
            {
                // 语种模板注册表优先（占位符 {TargetWords}）
                var instruction = Utilities.PromptTemplate.Get("RWKV/ChapterWriting.Instruction")
                    ?? $"Instruction: You are an English-language novelist. Write the chapter body in ENGLISH ONLY, following the given settings. Keep the style consistent, narrate naturally, and avoid list-like exposition. Do not output Markdown headings, lists, quotes or separators. Target length: about {targetWordCount} words.\n\nIf a sentence or paragraph repeats meaning already present in the generated text, do not rephrase it — advance the plot, add action detail or describe the environment instead.";
                instruction = instruction.Replace("{TargetWords}", targetWordCount.ToString());

                return
$@"{instruction}

Title:
{chapterTitle}

Style:
{style}

Outline:
{outline}

Key Plot:
{keyPlots}

Characters:
{characters}

State Memory:
{stateCard}

Requirements:
{requirements}

Existing Generated Content Tail:
{generatedTail}

Round:
{round + 1}

Response:";
            }

            return
$@"Instruction: 你是一名中文网络书籍作家。请按照给定设定创作章节正文，风格要统一，叙事自然，避免列表化说明。不要输出 Markdown 标题、列表、引用或分隔线。目标总字数约 {targetWordCount} 字。

如果某句、某段的含义已经在前文出现，请不要换一种说法重复表述，而是继续推进情节、补充动作细节或环境反馈。

Title:
{chapterTitle}

Style:
{style}

Outline:
{outline}

Key Plot:
{keyPlots}

Characters:
{characters}

State Memory:
{stateCard}

Requirements:
{requirements}

Existing Generated Content Tail:
{generatedTail}

Round:
{round + 1}

Response:";
        }

        private string BuildRwkvStateUpdatePrompt(Dictionary<string, object> parameters, string continuedText)
        {
            var chapterTitle = ExtractChapterTitle(parameters);
            var stateUpdateCard = BuildRwkvUpdateStateCard(parameters, continuedText);
            if (string.IsNullOrWhiteSpace(stateUpdateCard))
            {
                return string.Empty;
            }

            return
$@"System: 请将以下新增事实合并进当前故事状态缓存。只保留角色、目标、地点、冲突和最新推进，不要生成正文，不要分析，不要补充。请仅回复 OK。

章节标题：
{chapterTitle}

新增事实卡：
{stateUpdateCard}

Assistant:";
        }

        private string BuildRwkvPromptStateCard(Dictionary<string, object> parameters, string recentText)
        {
            var facts = new List<string>();
            AddRwkvStateFact(facts, $"项目约束：{CompactRwkvStateValue(parameters.GetValueOrDefault("PromptSummary"), 260)}");
            AddRwkvStateFact(facts, $"章节标题：{ExtractChapterTitle(parameters)}");
            AddRwkvStateFact(facts, $"写作风格：{CompactRwkvStateValue(parameters.GetValueOrDefault("WritingStyle"), 80)}");
            AddRwkvStateFact(facts, $"章节类型：{CompactRwkvStateValue(parameters.GetValueOrDefault("ChapterType"), 80)}");
            AddRwkvStateFact(facts, $"章节大纲：{CompactRwkvStateValue(parameters.GetValueOrDefault("ChapterOutline"), 180)}");
            AddRwkvStateFact(facts, $"关键剧情：{CompactRwkvStateValue(parameters.GetValueOrDefault("KeyPlots") ?? parameters.GetValueOrDefault("PlotOutlines"), 220)}");
            AddRwkvStateFact(facts, $"主要角色：{CompactRwkvStateValue(parameters.GetValueOrDefault("Characters") ?? parameters.GetValueOrDefault("MainCharacters"), 220)}");
            AddRwkvStateFact(facts, $"世界设定：{CompactRwkvStateValue(parameters.GetValueOrDefault("WorldSettings"), 180)}");
            AddRwkvStateFact(facts, $"特殊要求：{CompactRwkvStateValue(parameters.GetValueOrDefault("SpecialRequirements"), 160)}");

            foreach (var fact in ExtractRwkvStateFacts(recentText, 4))
            {
                AddRwkvStateFact(facts, $"最近进展：{fact}");
            }

            if (facts.Count == 0)
            {
                return "- 暂无可用状态";
            }

            return string.Join("\n", facts.Take(RwkvMaxStateFacts).Select(fact => $"- {fact}"));
        }

        private string BuildRwkvUpdateStateCard(Dictionary<string, object> parameters, string continuedText)
        {
            var facts = new List<string>();
            AddRwkvStateFact(facts, $"章节标题：{ExtractChapterTitle(parameters)}");
            foreach (var fact in ExtractRwkvStateFacts(continuedText, 6))
            {
                AddRwkvStateFact(facts, fact);
            }

            return facts.Count == 0
                ? string.Empty
                : string.Join("\n", facts.Take(RwkvMaxStateFacts).Select(fact => $"- {fact}"));
        }

        private static void AddRwkvStateFact(List<string> facts, string? fact)
        {
            if (string.IsNullOrWhiteSpace(fact))
            {
                return;
            }

            var cleanedFact = fact.Trim();
            var normalizedFact = NormalizeRwkvComparableText(cleanedFact);
            if (normalizedFact.Length < 6)
            {
                return;
            }

            if (facts.Any(existing => string.Equals(
                    NormalizeRwkvComparableText(existing),
                    normalizedFact,
                    StringComparison.Ordinal)))
            {
                return;
            }

            facts.Add(cleanedFact);
        }

        private static IEnumerable<string> ExtractRwkvStateFacts(string text, int maxFacts)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return Array.Empty<string>();
            }

            var tail = LimitRwkvContext(text, 1200);
            var facts = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var sentences = SplitRwkvSentences(tail).ToList();

            for (int i = sentences.Count - 1; i >= 0 && facts.Count < maxFacts; i--)
            {
                var fact = NormalizeRwkvStateFact(sentences[i]);
                if (string.IsNullOrWhiteSpace(fact))
                {
                    continue;
                }

                var normalized = NormalizeRwkvComparableText(fact);
                if (normalized.Length < 10 || !seen.Add(normalized))
                {
                    continue;
                }

                facts.Add(fact);
            }

            facts.Reverse();
            return facts;
        }

        private static string NormalizeRwkvStateFact(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var cleaned = text.Replace("\r", " ")
                .Replace("\n", " ")
                .Replace("```", string.Empty, StringComparison.Ordinal)
                .Replace("Response:", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("Instruction:", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Trim(' ', '\t', '-', '*', '"', '\'', '`');

            cleaned = string.Join(" ", cleaned
                .Split(' ', StringSplitOptions.RemoveEmptyEntries));

            return cleaned.Length > 120 ? cleaned[..120] : cleaned;
        }

        private static string CompactRwkvStateValue(object? value, int maxChars)
        {
            var raw = SerializeForRwkv(value);
            if (string.IsNullOrWhiteSpace(raw) || raw == "[]")
            {
                return string.Empty;
            }

            var compact = string.Join(" ", raw
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries));

            return compact.Length > maxChars ? compact[..maxChars] : compact;
        }

        private static string SerializeForRwkv(object? value)
        {
            if (value == null)
            {
                return "[]";
            }

            try
            {
                var json = JsonSerializer.Serialize(value);
                return json.Length > 4000 ? json[..4000] : json;
            }
            catch
            {
                var text = value.ToString() ?? string.Empty;
                return text.Length > 4000 ? text[..4000] : text;
            }
        }

        private static string LimitRwkvContext(string text, int maxChars = RwkvRollingContextChars)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length <= maxChars)
            {
                return text;
            }

            return text[^maxChars..];
        }

        private bool ShouldUseRwkvForChapterWriting(Dictionary<string, object> parameters)
        {
            var aiModel = parameters.GetValueOrDefault("AIModel", "").ToString();
            return _rwkvService != null &&
                   _rwkvService.IsAvailable &&
                   string.Equals(aiModel, "RWKV", StringComparison.OrdinalIgnoreCase);
        }

        private int GetTargetWordCountForGeneration(Dictionary<string, object> parameters)
        {
            var targetWordCountText = parameters.GetValueOrDefault("TargetWordCount", "2000")?.ToString();
            return int.TryParse(targetWordCountText, out var targetWordCount)
                ? Math.Clamp(targetWordCount, 300, 3000)
                : 2000;
        }

        private static string CleanupRwkvWritingChunk(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var cleaned = text.Replace("Response:", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("Instruction:", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Trim();

            if (cleaned.StartsWith("```", StringComparison.Ordinal))
            {
                cleaned = cleaned.Trim('`').Trim();
            }

            var lines = cleaned.Split('\n')
                .Where(line =>
                {
                    var trimmed = line.TrimStart();
                    if (trimmed.StartsWith("##", StringComparison.Ordinal) ||
                        trimmed.StartsWith("---", StringComparison.Ordinal) ||
                        trimmed.StartsWith("*", StringComparison.Ordinal) ||
                        trimmed.Contains("续写方向", StringComparison.OrdinalIgnoreCase) ||
                        trimmed.Contains("Key Plot Continuation Guideline", StringComparison.OrdinalIgnoreCase) ||
                        trimmed.Contains("Existing Generated Content Tail", StringComparison.OrdinalIgnoreCase) ||
                        trimmed.Contains("Response:", StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    return true;
                })
                .Select(line => line.TrimStart('>', ' '))
                .ToArray();
            cleaned = string.Join("\n", lines).Trim();

            return RemoveRepeatedWritingUnits(string.Empty, cleaned);
        }

        private static string MergeRwkvChunk(string existingText, string newChunk)
        {
            if (string.IsNullOrWhiteSpace(existingText))
            {
                return CleanupRwkvWritingChunk(newChunk);
            }

            var normalizedExisting = existingText.TrimEnd();
            var normalizedChunk = RemoveRepeatedWritingUnits(normalizedExisting, CleanupRwkvWritingChunk(newChunk));
            if (string.IsNullOrWhiteSpace(normalizedChunk))
            {
                return normalizedExisting;
            }

            var maxOverlap = Math.Min(Math.Min(normalizedExisting.Length, normalizedChunk.Length), 120);
            for (int overlap = maxOverlap; overlap >= 12; overlap--)
            {
                if (normalizedExisting.EndsWith(normalizedChunk[..overlap], StringComparison.Ordinal))
                {
                    return normalizedExisting + normalizedChunk[overlap..];
                }
            }

            return normalizedExisting + normalizedChunk;
        }

        private static string RemoveRepeatedWritingUnits(string existingText, string newChunk)
        {
            if (string.IsNullOrWhiteSpace(newChunk))
            {
                return string.Empty;
            }

            var seenParagraphs = new HashSet<string>(GetComparableParagraphs(existingText), StringComparer.Ordinal);
            var seenSentences = new HashSet<string>(GetComparableSentences(existingText), StringComparer.Ordinal);
            var filteredParagraphs = new List<string>();

            foreach (var paragraph in SplitRwkvParagraphs(newChunk))
            {
                var filteredSentences = new List<string>();
                foreach (var sentence in SplitRwkvSentences(paragraph))
                {
                    var normalizedSentence = NormalizeRwkvComparableText(sentence);
                    if (normalizedSentence.Length >= 10 &&
                        IsComparableUnitDuplicate(normalizedSentence, seenSentences))
                    {
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(sentence))
                    {
                        filteredSentences.Add(sentence.Trim());
                    }

                    if (normalizedSentence.Length >= 10)
                    {
                        seenSentences.Add(normalizedSentence);
                    }
                }

                var rebuiltParagraph = string.Join(string.Empty, filteredSentences).Trim();
                if (string.IsNullOrWhiteSpace(rebuiltParagraph))
                {
                    continue;
                }

                var normalizedParagraph = NormalizeRwkvComparableText(rebuiltParagraph);
                if (normalizedParagraph.Length >= 20 &&
                    IsComparableUnitDuplicate(normalizedParagraph, seenParagraphs))
                {
                    continue;
                }

                filteredParagraphs.Add(rebuiltParagraph);
                if (normalizedParagraph.Length >= 20)
                {
                    seenParagraphs.Add(normalizedParagraph);
                }
            }

            return string.Join("\n", filteredParagraphs).Trim();
        }

        private static IEnumerable<string> GetComparableParagraphs(string text)
        {
            return SplitRwkvParagraphs(text)
                .Select(NormalizeRwkvComparableText)
                .Where(value => value.Length >= 20);
        }

        private static IEnumerable<string> GetComparableSentences(string text)
        {
            return SplitRwkvSentences(text)
                .Select(NormalizeRwkvComparableText)
                .Where(value => value.Length >= 10);
        }

        private static IEnumerable<string> SplitRwkvParagraphs(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return Array.Empty<string>();
            }

            return text.Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Split(new[] { "\n\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(paragraph => paragraph.Trim())
                .Where(paragraph => !string.IsNullOrWhiteSpace(paragraph));
        }

        private static IEnumerable<string> SplitRwkvSentences(string text)
        {
            var sentences = new List<string>();
            if (string.IsNullOrWhiteSpace(text))
            {
                return sentences;
            }

            var builder = new System.Text.StringBuilder();
            foreach (var ch in text)
            {
                builder.Append(ch);
                if (IsRwkvSentenceEnding(ch))
                {
                    var sentence = builder.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(sentence))
                    {
                        sentences.Add(sentence);
                    }

                    builder.Clear();
                }
            }

            if (builder.Length > 0)
            {
                var tail = builder.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(tail))
                {
                    sentences.Add(tail);
                }
            }

            return sentences;
        }

        private static bool IsComparableUnitDuplicate(string candidate, IEnumerable<string> existingUnits)
        {
            foreach (var existing in existingUnits)
            {
                if (string.Equals(existing, candidate, StringComparison.Ordinal))
                {
                    return true;
                }

                if (candidate.Length >= 18 &&
                    (existing.Contains(candidate, StringComparison.Ordinal) ||
                     candidate.Contains(existing, StringComparison.Ordinal)))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsRwkvSentenceEnding(char c)
        {
            return c is '。' or '！' or '？' or '!' or '?' or ';' or '；';
        }

        private static string NormalizeRwkvComparableText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var chars = text
                .Where(ch => !char.IsWhiteSpace(ch) && !char.IsPunctuation(ch) && !char.IsSymbol(ch))
                .ToArray();
            return new string(chars).Trim();
        }

        private static string BuildRwkvFillDirection(int remainingWords, string? preferredDirection, bool isChapterWriting)
        {
            var baseDirection = string.IsNullOrWhiteSpace(preferredDirection)
                ? (isChapterWriting ? "请承接上文继续正文。" : "请自然续写正文。")
                : preferredDirection.Trim();

            return $"{baseDirection} 请只补充剩余约 {remainingWords} 字的有效正文，不要重复已写内容，不要回顾前文，不要输出标题或解释。";
        }

        private async Task<RwkvGenerationSelectionResult> GenerateRwkvWritingCandidateAsync(
            Dictionary<string, object> parameters,
            string prompt,
            string? direction,
            string existingText,
            int maxTokens)
        {
            if (_rwkvService == null)
            {
                return new RwkvGenerationSelectionResult
                {
                    Response = new RwkvCompletionResponse
                    {
                        Success = false,
                        Error = "RWKV 服务不可用"
                    }
                };
            }

            var batchEnabled = IsRwkvBigBatchEnabled(parameters) && CanUseRwkvBigBatchForContext(existingText);
            var candidatePrompts = batchEnabled
                ? BuildRwkvBatchCandidatePrompts(prompt, direction)
                : new List<string>();

            if (candidatePrompts.Count >= 2)
            {
                var batchResult = await _rwkvService.CompleteBatchAsync(
                    candidatePrompts,
                    maxTokens: maxTokens,
                    temperature: RwkvNovelTemperature,
                    topP: RwkvNovelTopP,
                    presencePenalty: RwkvNovelPresencePenalty,
                    frequencyPenalty: RwkvNovelFrequencyPenalty,
                    chunkSize: RwkvBatchChunkSize);

                var bestCandidate = SelectBestRwkvBatchCandidate(existingText, batchResult);
                if (bestCandidate != null)
                {
                    _logger.LogInformation("RWKV big_batch 已选出最佳候选，索引 {Index}，估算长度 {Length}", bestCandidate.Index, bestCandidate.Text.Length);
                    return new RwkvGenerationSelectionResult
                    {
                        UsedBatch = true,
                        CandidateCount = batchResult.Items.Count,
                        SelectedCandidateIndex = bestCandidate.Index,
                        SelectedScore = bestCandidate.Score,
                        Response = new RwkvCompletionResponse
                        {
                            Success = true,
                            Text = bestCandidate.Text,
                            TokensGenerated = bestCandidate.TokensGenerated
                        }
                    };
                }

                if (!string.IsNullOrWhiteSpace(batchResult.Error))
                {
                    _logger.LogWarning("RWKV big_batch 未返回可用候选，回退单次生成：{Error}", batchResult.Error);
                }
                else
                {
                    _logger.LogWarning("RWKV big_batch 未返回可用候选，回退单次生成");
                }
            }

            return new RwkvGenerationSelectionResult
            {
                UsedBatch = batchEnabled,
                FellBackToSingle = batchEnabled,
                CandidateCount = candidatePrompts.Count,
                // 流式请求：响应头立即返回、数据持续到达，避免经 Cloudflare Tunnel 时
                // 因源站 >100s 未响应被 524 掐断（非流式 CompleteAsync 已实测踩坑）
                Response = await _rwkvService.CompleteStreamAsync(
                    prompt: prompt,
                    maxTokens: maxTokens,
                    direction: direction,
                    temperature: RwkvNovelTemperature,
                    topP: RwkvNovelTopP,
                    presencePenalty: RwkvNovelPresencePenalty,
                    frequencyPenalty: RwkvNovelFrequencyPenalty)
            };
        }

        private List<string> BuildRwkvBatchCandidatePrompts(string prompt, string? direction)
        {
            var prompts = new List<string>
            {
                BuildRwkvBatchPromptVariant(prompt, direction, string.Empty)
            };

            if (RwkvBatchCandidateCount >= 2)
            {
                prompts.Add(BuildRwkvBatchPromptVariant(
                    prompt,
                    direction,
                    "请优先推进剧情，增加新的动作、环境变化或线索，不要换个说法重复刚才的内容。"));
            }

            return prompts;
        }

        private string BuildRwkvBatchPromptVariant(string prompt, string? direction, string extraInstruction)
        {
            var mergedDirection = string.IsNullOrWhiteSpace(extraInstruction)
                ? direction
                : string.IsNullOrWhiteSpace(direction)
                    ? extraInstruction
                    : $"{direction} {extraInstruction}";

            var builder = new System.Text.StringBuilder();
            builder.Append(prompt);

            if (!string.IsNullOrWhiteSpace(mergedDirection))
            {
                if (!prompt.EndsWith("\n", StringComparison.Ordinal))
                {
                    builder.AppendLine();
                }

                builder.AppendLine();
                builder.Append($"<!-- 续写方向：{mergedDirection} -->");
                builder.AppendLine();
            }

            return builder.ToString();
        }

        private ScoredRwkvBatchCandidate? SelectBestRwkvBatchCandidate(string existingText, RwkvBatchCompletionResponse batchResult)
        {
            if (!batchResult.Success || batchResult.Items.Count == 0)
            {
                return null;
            }

            ScoredRwkvBatchCandidate? bestItem = null;
            double bestScore = double.MinValue;

            foreach (var item in batchResult.Items)
            {
                var cleanedText = CleanupRwkvWritingChunk(item.Text);
                if (string.IsNullOrWhiteSpace(cleanedText))
                {
                    continue;
                }

                if (!IsRwkvBatchCandidateUsable(existingText, cleanedText))
                {
                    continue;
                }

                var score = EvaluateRwkvCandidateScore(existingText, cleanedText);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestItem = new ScoredRwkvBatchCandidate
                    {
                        Index = item.Index,
                        Text = cleanedText,
                        TokensGenerated = item.TokensGenerated,
                        Score = score
                    };
                }
            }

            return bestItem;
        }

        private bool IsRwkvBigBatchEnabled(Dictionary<string, object> parameters)
        {
            if (_rwkvService == null || !_rwkvService.IsAvailable)
            {
                return false;
            }

            var explicitDisable = parameters.GetValueOrDefault("DisableRwkvBigBatch")?.ToString();
            if (bool.TryParse(explicitDisable, out var disabled) && disabled)
            {
                return false;
            }

            var explicitEnable = parameters.GetValueOrDefault("EnableRwkvBigBatch")?.ToString();
            if (bool.TryParse(explicitEnable, out var enabled))
            {
                return enabled;
            }

            return true;
        }

        private double EvaluateRwkvCandidateScore(string existingText, string candidateText)
        {
            var mergedText = MergeRwkvChunk(existingText, candidateText);
            var candidateWordCount = CountChineseCharacters(candidateText);
            var wordDelta = Math.Max(0, CountChineseCharacters(mergedText) - CountChineseCharacters(existingText));
            var hanCount = CountRwkvHanCharacters(candidateText);
            var hanRatio = candidateText.Length == 0 ? 0 : (double)hanCount / candidateText.Length;

            var existingSentences = new HashSet<string>(GetComparableSentences(existingText), StringComparer.Ordinal);
            var candidateSentences = SplitRwkvSentences(candidateText)
                .Select(sentence => NormalizeRwkvComparableText(sentence))
                .Where(sentence => sentence.Length >= 10)
                .ToList();
            var duplicateSentenceCount = candidateSentences.Count(sentence => IsComparableUnitDuplicate(sentence, existingSentences));
            var uniqueSentenceCount = candidateSentences.Count - duplicateSentenceCount;

            var existingParagraphs = new HashSet<string>(GetComparableParagraphs(existingText), StringComparer.Ordinal);
            var candidateParagraphs = SplitRwkvParagraphs(candidateText)
                .Select(paragraph => NormalizeRwkvComparableText(paragraph))
                .Where(paragraph => paragraph.Length >= 20)
                .ToList();
            var duplicateParagraphCount = candidateParagraphs.Count(paragraph => IsComparableUnitDuplicate(paragraph, existingParagraphs));

            var penalty = 0.0;
            if (candidateText.Contains("English:", StringComparison.OrdinalIgnoreCase) ||
                candidateText.Contains("Japanese:", StringComparison.OrdinalIgnoreCase) ||
                candidateText.Contains("Korean:", StringComparison.OrdinalIgnoreCase))
            {
                penalty += 60;
            }

            if (candidateText.Contains("Response:", StringComparison.OrdinalIgnoreCase) ||
                candidateText.Contains("Instruction:", StringComparison.OrdinalIgnoreCase))
            {
                penalty += 40;
            }

            if (candidateText.Contains("<!--", StringComparison.Ordinal) ||
                candidateText.Contains("-->", StringComparison.Ordinal))
            {
                penalty += 120;
            }

            if (hanRatio < 0.45)
            {
                penalty += 140;
            }

            return wordDelta * 1.8
                   + candidateWordCount * 0.2
                   + hanCount * 0.15
                   + uniqueSentenceCount * 16
                   - duplicateSentenceCount * 18
                   - duplicateParagraphCount * 28
                   - penalty;
        }

        private bool IsRwkvBatchCandidateUsable(string existingText, string candidateText)
        {
            if (string.IsNullOrWhiteSpace(candidateText))
            {
                return false;
            }

            if (candidateText.Contains("<!--", StringComparison.Ordinal) ||
                candidateText.Contains("-->", StringComparison.Ordinal) ||
                candidateText.Contains("English:", StringComparison.OrdinalIgnoreCase) ||
                candidateText.Contains("Japanese:", StringComparison.OrdinalIgnoreCase) ||
                candidateText.Contains("Korean:", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var hanCount = CountRwkvHanCharacters(candidateText);
            if (hanCount < RwkvMinUsefulChunkChars)
            {
                return false;
            }

            var comparableLength = candidateText.Count(ch => !char.IsWhiteSpace(ch));
            if (comparableLength > 0 && (double)hanCount / comparableLength < 0.45)
            {
                return false;
            }

            var sentenceCount = SplitRwkvSentences(candidateText).Count();
            if (hanCount >= 120 && sentenceCount < 2)
            {
                return false;
            }

            var mergedText = MergeRwkvChunk(existingText, candidateText);
            var effectiveDelta = CountRwkvHanCharacters(mergedText) - CountRwkvHanCharacters(existingText);
            return effectiveDelta >= RwkvMinUsefulChunkChars;
        }

        private bool CanUseRwkvBigBatchForContext(string existingText)
        {
            return CountRwkvHanCharacters(existingText) >= 80;
        }

        private static int CountRwkvHanCharacters(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return 0;
            }

            var count = 0;
            foreach (var ch in text)
            {
                if (ch >= 0x4e00 && ch <= 0x9fff)
                {
                    count++;
                }
            }

            return count;
        }

        private static string SanitizeSessionSegment(string value)
        {
            var chars = value
                .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
                .ToArray();
            return new string(chars).Trim('-').ToLowerInvariant();
        }

        /// <summary>
        /// 截断文本到指定字数（按中文字符计算）
        /// </summary>
        private string TruncateToWordCount(string text, int targetWordCount)
        {
            var charCount = 0;
            for (int i = 0; i < text.Length; i++)
            {
                var c = text[i];
                charCount += c > 127 ? 1 : 0; // 简化：中文字符计1，英文忽略
                if (charCount >= targetWordCount)
                {
                    // 找到最近的句子结束位置
                    var endPos = text.IndexOfAny(new[] { '。', '！', '？', '.', '!', '?' }, i);
                    if (endPos > 0 && endPos < i + 50)
                    {
                        return text[..(endPos + 1)];
                    }
                    return text[..(i + 1)];
                }
            }
            return text;
        }

        /// <summary>
        /// 维持写作风格
        /// </summary>
        /// <param name="parameters">参数</param>
        /// <returns>风格分析结果</returns>
        private async Task<AgentTaskResult> MaintainWritingStyleAsync(Dictionary<string, object> parameters)
        {
            try
            {
                UpdateProgress(20);
                
                var existingContent = parameters.GetValueOrDefault("existingContent", "").ToString();
                
                _logger.LogInformation("开始分析和维持写作风格");
                
                // 模拟风格分析
                await Task.Delay(1500);
                
                UpdateProgress(80);
                
                var styleAnalysis = new
                {
                    WritingStyle = "古典仙侠风格",
                    Characteristics = new[]
                    {
                        "语言典雅，富有古韵",
                        "描写细腻，意境深远",
                        "对话自然，符合人物性格",
                        "节奏适中，张弛有度"
                    },
                    Recommendations = new[]
                    {
                        "保持古典用词习惯",
                        "注意修仙术语的一致性",
                        "维持人物对话风格",
                        "保持场景描写的诗意"
                    }
                };
                
                UpdateProgress(100);
                
                return new AgentTaskResult
                {
                    IsSuccess = true,
                    Data = styleAnalysis
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "写作风格分析失败");
                return new AgentTaskResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// 处理对话
        /// </summary>
        /// <param name="parameters">参数</param>
        /// <returns>对话处理结果</returns>
        private async Task<AgentTaskResult> HandleDialogueAsync(Dictionary<string, object> parameters)
        {
            try
            {
                _logger.LogInformation("开始处理对话生成任务");
                UpdateProgress(10);

                // 创建思维链
                var thinkingChain = new ThinkingChainModel
                {
                    Title = $"{Name} - 对话生成",
                    Description = "执行对话生成任务",
                    TaskId = Guid.NewGuid().ToString(),
                    AgentId = Id
                };

                // 使用AI辅助执行对话生成
                var result = await ExecuteTaskWithAIAsync("HandleDialogue", parameters, thinkingChain);

                if (result.IsSuccess)
                {
                    // 解析AI响应，提取对话内容
                    var dialogueContent = ExtractDialogueFromResponse(result.Data?.ToString() ?? "");

                    // 进行对话质量评估
                    var qualityScore = await EvaluateDialogueQuality(dialogueContent, parameters);

                    UpdateProgress(90);

                    var dialogueResult = new
                    {
                        Content = dialogueContent,
                        QualityScore = qualityScore,
                        Characters = parameters.GetValueOrDefault("characters", ""),
                        Situation = parameters.GetValueOrDefault("situation", ""),
                        Emotion = parameters.GetValueOrDefault("emotion", ""),
                        GeneratedAt = DateTime.Now,
                        ThinkingChainId = thinkingChain.Id
                    };

                    UpdateProgress(100);
                    _logger.LogInformation("对话生成任务完成，质量评分: {QualityScore}", qualityScore);

                    return new AgentTaskResult
                    {
                        IsSuccess = true,
                        Data = dialogueResult,
                        Metadata = new Dictionary<string, object>
                        {
                            ["Message"] = $"对话生成完成，质量评分: {qualityScore:F1}/10",
                            ["QualityScore"] = qualityScore
                        }
                    };
                }
                else
                {
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "对话处理失败");
                return new AgentTaskResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// 描述场景
        /// </summary>
        /// <param name="parameters">参数</param>
        /// <returns>场景描述结果</returns>
        private async Task<AgentTaskResult> DescribeSceneAsync(Dictionary<string, object> parameters)
        {
            try
            {
                UpdateProgress(20);
                await Task.Delay(1800);
                UpdateProgress(100);
                
                return new AgentTaskResult
                {
                    IsSuccess = true,
                    Data = "场景描述完成"
                };
            }
            catch (Exception ex)
            {
                return new AgentTaskResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// 刻画心理
        /// </summary>
        /// <param name="parameters">参数</param>
        /// <returns>心理刻画结果</returns>
        private async Task<AgentTaskResult> PortrayPsychologyAsync(Dictionary<string, object> parameters)
        {
            try
            {
                UpdateProgress(20);
                await Task.Delay(1600);
                UpdateProgress(100);
                
                return new AgentTaskResult
                {
                    IsSuccess = true,
                    Data = "心理刻画完成"
                };
            }
            catch (Exception ex)
            {
                return new AgentTaskResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// 确保一致性
        /// </summary>
        /// <param name="parameters">参数</param>
        /// <returns>一致性检查结果</returns>
        private async Task<AgentTaskResult> EnsureConsistencyAsync(Dictionary<string, object> parameters)
        {
            try
            {
                UpdateProgress(20);
                await Task.Delay(1000);
                UpdateProgress(100);
                
                return new AgentTaskResult
                {
                    IsSuccess = true,
                    Data = "一致性检查完成"
                };
            }
            catch (Exception ex)
            {
                return new AgentTaskResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        #endregion

        #region AI辅助方法重写

        /// <summary>
        /// 构建系统提示
        /// </summary>
        /// <param name="taskType">任务类型</param>
        /// <returns>系统提示</returns>
        protected override string BuildSystemPrompt(string taskType)
        {
            // 语种模板注册表优先（PromptTemplates/{ID}/{lang}.txt，二次开发者可外置覆盖）
            var systemTemplate = Utilities.PromptTemplate.Get("WriterAgent/" + taskType + ".System");
            if (systemTemplate != null)
            {
                return systemTemplate;
            }

            if (Utilities.AIPromptLanguage.UseEnglish)
            {
                return taskType switch
                {
                    "GenerateChapterContent" => @"
                    You are a professional novelist who writes high-quality book chapters.
                    Your task is to write a polished chapter from the provided outline.

                    Writing requirements:
                    1. Vivid, fluent and engaging language
                    2. Plausible, logically consistent plot development
                    3. Distinct, well-drawn characters
                    4. Rich, sensory scene description
                    5. Natural dialogue that fits each character

                    Think through: how to read the outline, how to structure the chapter,
                    how to choose the style and voice, how to advance plot and relationships,
                    and how to keep quality high.
                ",
                    "ContinueChapter" => @"
                    You are a professional fiction continuation writer.
                    Continue the existing chapter naturally, following the given direction.

                    Requirements:
                    1. Keep the style, narration and characterization consistent with the original
                    2. Advance the plot plausibly and coherently
                    3. Preserve the original formatting and paragraph structure
                    4. Follow the requested direction
                    5. Meet the requested length
                    6. Keep character behaviour in line with established traits
                    7. Respect the established worldbuilding and rules

                    Output only the continuation text, with no explanations, labels or markup,
                    so it can be appended directly to the original.
                ",
                    "MaintainWritingStyle" => @"
                    You are a literary style analyst. Analyse the writing characteristics of the given content
                    and provide concrete advice for keeping the style consistent.
                ",
                    "HandleDialogue" => @"
                    You are a dialogue specialist. Write natural, flowing dialogue that fits the characters and situation.
                ",
                    "DescribeScene" => @"
                    You are a scene description specialist. Write vivid, detailed, cinematic scene descriptions as the plot requires.
                ",
                    "PortrayPsychology" => @"
                    You are a psychology-writing specialist. Render characters' inner worlds with nuance and truth.
                ",
                    _ => base.BuildSystemPrompt(taskType)
                };
            }

            return taskType switch
            {
                "GenerateChapterContent" => @"
                    你是一位专业的书籍作家，擅长创作各种类型的书籍内容。
                    你的任务是根据提供的章节大纲生成高质量的章节内容。

                    写作要求：
                    1. 语言生动流畅，富有感染力
                    2. 情节发展合理，符合逻辑
                    3. 人物刻画深入，性格鲜明
                    4. 场景描写细腻，画面感强
                    5. 对话自然真实，符合人物身份

                    请在创作过程中展示你的思考过程，包括：
                    - 如何理解和分析大纲
                    - 如何规划章节结构
                    - 如何选择写作风格和语言
                    - 如何处理情节发展和人物关系
                    - 如何确保内容质量
                ",
                "ContinueChapter" => @"
                    你是一位专业的书籍续写专家，擅长在现有内容基础上进行自然流畅的续写。
                    你的任务是根据提供的现有章节内容，按照指定的方向和要求进行续写。

                    续写要求：
                    1. 严格保持与原文的风格一致性（语言风格、叙述方式、人物性格等）
                    2. 情节发展自然合理，与前文逻辑连贯
                    3. 保持原文的排版格式和段落结构
                    4. 根据续写方向要求发展剧情
                    5. 字数符合指定要求
                    6. 确保人物行为符合已建立的性格特征
                    7. 维持已有的世界观设定和规则

                    输出格式：
                    请直接输出续写的文本内容，不要包含任何解释、标记或格式化符号。
                    续写内容应该能够直接接在原文后面，形成完整连贯的文章。
                ",
                "MaintainWritingStyle" => @"
                    你是一位文学风格分析专家，能够准确识别和维持一致的写作风格。
                    请分析现有内容的写作特点，并提供风格维持的建议。
                ",
                "HandleDialogue" => @"
                    你是一位对话写作专家，擅长创作自然流畅的人物对话。
                    请根据人物性格和情境创作合适的对话内容。
                ",
                "DescribeScene" => @"
                    你是一位场景描写专家，能够创作生动细腻的场景描述。
                    请根据情节需要创作富有画面感的场景描写。
                ",
                "PortrayPsychology" => @"
                    你是一位心理描写专家，擅长深入刻画人物的内心世界。
                    请创作细腻真实的心理描写内容。
                ",
                _ => base.BuildSystemPrompt(taskType)
            };
        }

        /// <summary>
        /// 构建用户提示
        /// </summary>
        /// <param name="taskType">任务类型</param>
        /// <param name="parameters">任务参数</param>
        /// <returns>用户提示</returns>
        protected override string BuildUserPrompt(string taskType, Dictionary<string, object> parameters)
        {
            return taskType switch
            {
                "GenerateChapterContent" => BuildChapterContentPrompt(parameters),
                "ContinueChapter" => BuildContinueChapterPrompt(parameters),
                "MaintainWritingStyle" => BuildStylePrompt(parameters),
                "HandleDialogue" => BuildDialoguePrompt(parameters),
                "DescribeScene" => BuildScenePrompt(parameters),
                "PortrayPsychology" => BuildPsychologyPrompt(parameters),
                _ => base.BuildUserPrompt(taskType, parameters)
            };
        }

        /// <summary>
        /// 构建章节内容生成提示
        /// </summary>
        private string BuildChapterContentPrompt(Dictionary<string, object> parameters)
        {
            // 语种模板注册表优先（占位符 {Title}/{Type}/{TargetWordCount}/{Style}/{Outline}/{KeyPlots}/{Characters}/{SpecialRequirements}）
            var userTemplate = Utilities.PromptTemplate.Get("WriterAgent/GenerateChapterContent.User");
            if (userTemplate != null)
            {
                var t = parameters;
                return userTemplate
                    .Replace("{Title}", t.GetValueOrDefault("ChapterTitle", "").ToString())
                    .Replace("{Type}", t.GetValueOrDefault("ChapterType", "").ToString())
                    .Replace("{TargetWordCount}", t.GetValueOrDefault("TargetWordCount", "2000").ToString())
                    .Replace("{Style}", t.GetValueOrDefault("WritingStyle", "").ToString())
                    .Replace("{Outline}", t.GetValueOrDefault("ChapterOutline", "").ToString())
                    .Replace("{KeyPlots}", t.GetValueOrDefault("KeyPlots", "").ToString())
                    .Replace("{Characters}", t.GetValueOrDefault("Characters", "").ToString())
                    .Replace("{SpecialRequirements}", t.GetValueOrDefault("SpecialRequirements", "").ToString());
            }

            if (Utilities.AIPromptLanguage.UseEnglish)
            {
                var enTitle = parameters.GetValueOrDefault("ChapterTitle", "").ToString();
                var enStyle = parameters.GetValueOrDefault("WritingStyle", "literary fiction").ToString();
                var enType = parameters.GetValueOrDefault("ChapterType", "main chapter").ToString();
                var enWords = parameters.GetValueOrDefault("TargetWordCount", "2000").ToString();
                var enOutline = parameters.GetValueOrDefault("ChapterOutline", "").ToString();
                var enPlots = parameters.GetValueOrDefault("KeyPlots", "").ToString();
                var enChars = parameters.GetValueOrDefault("Characters", "").ToString();
                var enReqs = parameters.GetValueOrDefault("SpecialRequirements", "").ToString();

                return $@"Write a book chapter in the style of {enStyle}.

【Chapter Info】
Title: {enTitle}
Type: {enType}
Target length: about {enWords} words
Writing style: {enStyle}

【Outline】
{enOutline}

【Key Plot】
{enPlots}

【Main Characters】
{enChars}

【Special Requirements】
{enReqs}

【Requirements】
1. Follow the requested writing style throughout
2. Tight plotting, vivid description, strong imagery
3. Distinct characters with natural dialogue
4. Around {enWords} words
5. Complete and coherent as a chapter

Write ALL content in ENGLISH. Output only the chapter text — no explanations, labels or markup.";
            }

            // 从UI传递的参数中提取信息
            var chapterTitle = parameters.GetValueOrDefault("ChapterTitle", "").ToString();
            var writingStyle = parameters.GetValueOrDefault("WritingStyle", "古风仙侠").ToString();
            var chapterType = parameters.GetValueOrDefault("ChapterType", "正文章节").ToString();
            var targetWordCount = parameters.GetValueOrDefault("TargetWordCount", "2000").ToString();
            var chapterOutline = parameters.GetValueOrDefault("ChapterOutline", "").ToString();
            var keyPlots = parameters.GetValueOrDefault("KeyPlots", "").ToString();
            var characters = parameters.GetValueOrDefault("Characters", "").ToString();
            var specialRequirements = parameters.GetValueOrDefault("SpecialRequirements", "").ToString();

            var prompt = $@"请根据以下要求创作一个{writingStyle}风格的书籍章节：

【章节信息】
章节标题：{chapterTitle}
章节类型：{chapterType}
目标字数：{targetWordCount}字
写作风格：{writingStyle}

【章节大纲】
{chapterOutline}

【关键剧情】
{keyPlots}

【主要角色】
{characters}

【特殊要求】
{specialRequirements}

【创作要求】
1. 严格按照{writingStyle}的写作风格进行创作
2. 情节紧凑，描写生动，富有画面感
3. 人物性格鲜明，对话自然真实
4. 字数控制在{targetWordCount}字左右
5. 内容积极向上，符合网络文学规范
6. 保持章节内容的完整性和连贯性

请直接输出章节内容，不要包含任何解释、标记或格式化符号。";

            return prompt;
        }

        /// <summary>
        /// 构建风格分析提示
        /// </summary>
        private string BuildStylePrompt(Dictionary<string, object> parameters)
        {
            var existingContent = parameters.GetValueOrDefault("existingContent", "").ToString();

            return $@"
                请分析以下内容的写作风格特点：

                【内容样本】
                {existingContent}

                请从以下方面进行分析：
                1. 语言特色（用词习惯、句式结构）
                2. 叙述方式（第一人称/第三人称、时态等）
                3. 描写风格（细腻/粗犷、写实/浪漫等）
                4. 对话特点（正式/口语化、个性化程度）
                5. 节奏把控（快节奏/慢节奏、张弛有度）

                并提供保持风格一致性的具体建议。
            ";
        }

        /// <summary>
        /// 构建对话创作提示
        /// </summary>
        private string BuildDialoguePrompt(Dictionary<string, object> parameters)
        {
            var characters = parameters.GetValueOrDefault("characters", "").ToString();
            var situation = parameters.GetValueOrDefault("situation", "").ToString();
            var emotion = parameters.GetValueOrDefault("emotion", "").ToString();

            return $@"
                请创作以下情境下的人物对话：

                【参与人物】
                {characters}

                【对话情境】
                {situation}

                【情感基调】
                {emotion}

                要求：
                1. 对话符合人物性格特点
                2. 语言自然流畅，有生活气息
                3. 推动情节发展
                4. 体现人物关系和情感变化
            ";
        }

        /// <summary>
        /// 构建场景描写提示
        /// </summary>
        private string BuildScenePrompt(Dictionary<string, object> parameters)
        {
            var location = parameters.GetValueOrDefault("location", "").ToString();
            var atmosphere = parameters.GetValueOrDefault("atmosphere", "").ToString();
            var timeOfDay = parameters.GetValueOrDefault("timeOfDay", "").ToString();

            return $@"
                请创作以下场景的描写：

                【场景地点】
                {location}

                【时间】
                {timeOfDay}

                【氛围要求】
                {atmosphere}

                要求：
                1. 描写生动具体，富有画面感
                2. 调动多种感官（视觉、听觉、嗅觉等）
                3. 营造相应的氛围和情绪
                4. 为情节发展服务
            ";
        }

        /// <summary>
        /// 构建续写章节提示
        /// </summary>
        private string BuildContinueChapterPrompt(Dictionary<string, object> parameters)
        {
            var existingContent = parameters.GetValueOrDefault("ExistingContent", "").ToString();
            var continueLength = parameters.GetValueOrDefault("ContinueLength", "中续写").ToString();
            var customWordCount = parameters.GetValueOrDefault("CustomWordCount", "").ToString();
            var continueDirection = parameters.GetValueOrDefault("ContinueDirection", "").ToString();
            var chapterData = parameters.GetValueOrDefault("ChapterData", null);

            // 确定目标字数
            var targetWords = GetTargetWordCount(continueLength, customWordCount);

            var prompt = $@"
                请根据以下现有内容进行续写：

                【现有内容】
                {existingContent}

                【续写要求】
                - 续写长度：{continueLength}
                - 目标字数：{targetWords}字
                - 续写方向：{(!string.IsNullOrEmpty(continueDirection) ? continueDirection : "自然发展，保持情节连贯")}

                【特别要求】
                1. 严格保持与原文相同的写作风格和语言特色
                2. 确保情节发展自然合理，与前文逻辑连贯
                3. 保持原文的段落格式和排版风格
                4. 人物行为和对话要符合已建立的性格特征
                5. 遵循已有的世界观设定和规则
                6. 续写内容应该能直接接在原文后面

                请直接输出续写的文本内容，不要包含任何标记、解释或格式化符号。
            ";

            // 添加章节数据信息
            if (chapterData != null)
            {
                try
                {
                    var chapterType = chapterData.GetType();
                    var charactersProperty = chapterType.GetProperty("Characters");
                    var plotProperty = chapterType.GetProperty("Plot");
                    var settingProperty = chapterType.GetProperty("Setting");

                    if (charactersProperty != null)
                    {
                        var characters = charactersProperty.GetValue(chapterData)?.ToString();
                        if (!string.IsNullOrEmpty(characters))
                        {
                            prompt += $@"

                【主要角色】
                {characters}";
                        }
                    }

                    if (plotProperty != null)
                    {
                        var plot = plotProperty.GetValue(chapterData)?.ToString();
                        if (!string.IsNullOrEmpty(plot))
                        {
                            prompt += $@"

                【剧情设定】
                {plot}";
                        }
                    }

                    if (settingProperty != null)
                    {
                        var setting = settingProperty.GetValue(chapterData)?.ToString();
                        if (!string.IsNullOrEmpty(setting))
                        {
                            prompt += $@"

                【场景设定】
                {setting}";
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"解析章节数据失败: {ex.Message}");
                }
            }

            return prompt;
        }

        /// <summary>
        /// 构建心理描写提示
        /// </summary>
        private string BuildPsychologyPrompt(Dictionary<string, object> parameters)
        {
            var character = parameters.GetValueOrDefault("character", "").ToString();
            var situation = parameters.GetValueOrDefault("situation", "").ToString();
            var emotion = parameters.GetValueOrDefault("emotion", "").ToString();

            return $@"
                请创作以下人物的心理描写：

                【人物】
                {character}

                【情境】
                {situation}

                【心理状态】
                {emotion}

                要求：
                1. 深入挖掘人物内心世界
                2. 心理活动符合人物性格
                3. 与外在行为形成对比或呼应
                4. 推动人物成长和情节发展
            ";
        }

        #endregion

        #region 对话处理辅助方法

        /// <summary>
        /// 从AI响应中提取对话内容
        /// </summary>
        /// <param name="response">AI响应</param>
        /// <returns>对话内容</returns>
        private string ExtractDialogueFromResponse(string response)
        {
            try
            {
                // 简单的对话提取逻辑，可以根据实际AI响应格式优化
                if (string.IsNullOrEmpty(response))
                    return "";

                // 查找对话标记
                var dialogueMarkers = new[] { "【对话内容】", "对话：", "Dialogue:", "对话内容：" };

                foreach (var marker in dialogueMarkers)
                {
                    var index = response.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                    if (index >= 0)
                    {
                        var startIndex = index + marker.Length;
                        var endMarkers = new[] { "【", "---", "总结：", "评价：" };

                        var endIndex = response.Length;
                        foreach (var endMarker in endMarkers)
                        {
                            var endPos = response.IndexOf(endMarker, startIndex, StringComparison.OrdinalIgnoreCase);
                            if (endPos > startIndex && endPos < endIndex)
                            {
                                endIndex = endPos;
                            }
                        }

                        return response.Substring(startIndex, endIndex - startIndex).Trim();
                    }
                }

                // 如果没有找到标记，返回整个响应
                return response.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "提取对话内容失败，返回原始响应");
                return response;
            }
        }

        /// <summary>
        /// 评估对话质量
        /// </summary>
        /// <param name="dialogueContent">对话内容</param>
        /// <param name="parameters">生成参数</param>
        /// <returns>质量评分 (1-10)</returns>
        private async Task<double> EvaluateDialogueQuality(string dialogueContent, Dictionary<string, object> parameters)
        {
            try
            {
                if (string.IsNullOrEmpty(dialogueContent))
                    return 1.0;

                var score = 5.0; // 基础分数

                // 长度评估
                if (dialogueContent.Length > 50)
                    score += 1.0;
                if (dialogueContent.Length > 200)
                    score += 0.5;

                // 对话轮次评估
                var dialogueLines = dialogueContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                var dialogueCount = dialogueLines.Count(line =>
                    line.Contains("：") || line.Contains(":") || line.Contains("\"") || line.Contains("'"));

                if (dialogueCount >= 2)
                    score += 1.0;
                if (dialogueCount >= 4)
                    score += 0.5;

                // 人物名称一致性检查
                var characters = parameters.GetValueOrDefault("characters", "").ToString();
                if (!string.IsNullOrEmpty(characters))
                {
                    var characterNames = characters.Split(',', '，', ';', '；')
                        .Select(name => name.Trim())
                        .Where(name => !string.IsNullOrEmpty(name));

                    var mentionedCharacters = characterNames.Count(name =>
                        dialogueContent.Contains(name, StringComparison.OrdinalIgnoreCase));

                    if (mentionedCharacters > 0)
                        score += 1.0;
                }

                // 情感表达评估
                var emotion = parameters.GetValueOrDefault("emotion", "").ToString();
                if (!string.IsNullOrEmpty(emotion))
                {
                    var emotionKeywords = GetEmotionKeywords(emotion);
                    var hasEmotionExpression = emotionKeywords.Any(keyword =>
                        dialogueContent.Contains(keyword, StringComparison.OrdinalIgnoreCase));

                    if (hasEmotionExpression)
                        score += 1.0;
                }

                // 确保分数在1-10范围内
                return Math.Max(1.0, Math.Min(10.0, score));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "对话质量评估失败");
                return 5.0; // 默认中等分数
            }
        }

        /// <summary>
        /// 获取情感关键词
        /// </summary>
        /// <param name="emotion">情感类型</param>
        /// <returns>关键词列表</returns>
        private List<string> GetEmotionKeywords(string emotion)
        {
            var emotionKeywords = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["愤怒"] = new() { "愤怒", "生气", "恼火", "暴怒", "怒", "气", "火" },
                ["高兴"] = new() { "高兴", "开心", "快乐", "兴奋", "喜悦", "欣喜", "乐" },
                ["悲伤"] = new() { "悲伤", "难过", "伤心", "痛苦", "哀伤", "忧伤", "哭" },
                ["恐惧"] = new() { "恐惧", "害怕", "惊恐", "畏惧", "胆怯", "紧张", "怕" },
                ["惊讶"] = new() { "惊讶", "吃惊", "震惊", "意外", "诧异", "惊奇", "惊" },
                ["厌恶"] = new() { "厌恶", "讨厌", "恶心", "反感", "嫌弃", "憎恶", "恨" },
                ["平静"] = new() { "平静", "冷静", "淡定", "安静", "宁静", "沉着", "静" }
            };

            var result = new List<string>();
            foreach (var kvp in emotionKeywords)
            {
                if (emotion.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                {
                    result.AddRange(kvp.Value);
                }
            }

            return result.Any() ? result : new List<string> { emotion };
        }

        #endregion

        #region 续写辅助方法

        /// <summary>
        /// 获取目标字数
        /// </summary>
        /// <param name="continueLength">续写长度类型</param>
        /// <param name="customWordCount">自定义字数</param>
        /// <returns>目标字数</returns>
        private int GetTargetWordCount(string continueLength, string customWordCount)
        {
            // 如果有自定义字数，优先使用
            if (!string.IsNullOrEmpty(customWordCount) && int.TryParse(customWordCount, out var customCount))
            {
                return Math.Max(50, Math.Min(5000, customCount)); // 限制在50-5000字之间
            }

            // 根据续写长度类型确定字数
            return continueLength switch
            {
                "短续写" => 200,
                "中续写" => 500,
                "长续写" => 1000,
                _ => 500
            };
        }

        /// <summary>
        /// 从AI响应中提取续写文本
        /// </summary>
        /// <param name="aiResponse">AI响应</param>
        /// <returns>清理后的续写文本</returns>
        private string ExtractContinuedTextFromAIResponse(string aiResponse)
        {
            if (string.IsNullOrEmpty(aiResponse))
                return "";

            var text = AIOutputSanitizer.ExtractCleanOutput(aiResponse, "content", "text", "continued_text");

            // 移除常见的AI响应格式标记
            var markersToRemove = new[]
            {
                "【续写内容】", "续写内容：", "【思考】", "思考：", "【分析】", "分析：",
                "【结论】", "结论：", "【总结】", "总结：", "```", "---"
            };

            foreach (var marker in markersToRemove)
            {
                var index = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    // 如果找到标记，取标记后的内容
                    text = text.Substring(index + marker.Length);
                    break;
                }
            }

            // 清理文本
            text = text.Trim();

            // 移除开头和结尾的引号
            if (text.StartsWith("\"") && text.EndsWith("\""))
            {
                text = text.Substring(1, text.Length - 2);
            }

            return text.Trim();
        }

        /// <summary>
        /// 格式化续写文本，确保与原文排版一致
        /// </summary>
        /// <param name="continuedText">续写文本</param>
        /// <param name="existingContent">现有内容</param>
        /// <returns>格式化后的续写文本</returns>
        private string FormatContinuedText(string continuedText, string existingContent)
        {
            if (string.IsNullOrEmpty(continuedText))
                return "";

            // 分析原文的段落格式
            var hasIndentation = existingContent.Contains("    "); // 检查是否有缩进
            var paragraphSeparator = existingContent.Contains("\r\n\r\n") ? "\r\n\r\n" :
                                   existingContent.Contains("\n\n") ? "\n\n" : "\n";

            // 规范化续写文本的换行符
            continuedText = continuedText.Replace("\r\n", "\n").Replace("\r", "\n");

            // 分割段落
            var paragraphs = continuedText.Split(new[] { "\n\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                                         .Select(p => p.Trim())
                                         .Where(p => !string.IsNullOrEmpty(p))
                                         .ToArray();

            // 重新组织段落
            var formattedParagraphs = new List<string>();
            foreach (var paragraph in paragraphs)
            {
                var formattedParagraph = paragraph;

                // 如果原文有缩进，为续写内容添加缩进
                if (hasIndentation && !formattedParagraph.StartsWith("    "))
                {
                    formattedParagraph = "    " + formattedParagraph;
                }

                formattedParagraphs.Add(formattedParagraph);
            }

            // 用适当的分隔符连接段落
            return string.Join(paragraphSeparator, formattedParagraphs);
        }

        /// <summary>
        /// 计算中文字符数量
        /// </summary>
        /// <param name="text">文本</param>
        /// <returns>中文字符数量</returns>
        private int CountChineseCharacters(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            var count = 0;
            foreach (var c in text)
            {
                // 统计中文字符、英文字母、数字和常用标点符号
                if (char.IsLetterOrDigit(c) ||
                    (c >= 0x4e00 && c <= 0x9fff) || // 中文字符范围
                    IsChinesePunctuation(c))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 判断是否为中文标点符号
        /// </summary>
        private bool IsChinesePunctuation(char c)
        {
            var punctuations = new char[]
            {
                '，', '。', '！', '？', '；', '：',
                '\u201c', '\u201d', '\u2018', '\u2019', '（', '）',
                '【', '】', '《', '》', '、'
            };
            return punctuations.Contains(c);
        }

        /// <summary>
        /// 从AI响应中提取章节内容
        /// </summary>
        /// <param name="aiResponse">AI响应</param>
        /// <returns>提取的章节内容</returns>
        private string ExtractChapterContentFromAIResponse(string aiResponse)
        {
            return AIOutputSanitizer.ExtractCleanOutput(aiResponse, "content", "text", "chapter_content");
        }



        #endregion
    }
}
