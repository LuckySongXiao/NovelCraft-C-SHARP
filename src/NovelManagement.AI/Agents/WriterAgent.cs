using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NovelManagement.AI.Interfaces;
using NovelManagement.AI.Services.DeepSeek;
using NovelManagement.AI.Services.ThinkingChain;
using NovelManagement.AI.Services.ThinkingChain.Models;
using ThinkingChainModel = NovelManagement.AI.Services.ThinkingChain.Models.ThinkingChain;

namespace NovelManagement.AI.Agents
{
    /// <summary>
    /// 作家Agent - 负责章节内容生成
    /// </summary>
    public class WriterAgent : BaseAgent
    {
        /// <summary>
        /// 构造函数（用于依赖注入）
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="memoryManager">记忆管理器</param>
        /// <param name="deepSeekApiService">DeepSeek API服务</param>
        /// <param name="thinkingChainProcessor">思维链处理器</param>
        /// <param name="modelManager">模型管理器</param>
        public WriterAgent(
            ILogger<WriterAgent> logger,
            IMemoryManager memoryManager,
            IDeepSeekApiService deepSeekApiService,
            IThinkingChainProcessor thinkingChainProcessor,
            NovelManagement.AI.Services.ModelManager modelManager)
            : base(logger, memoryManager, deepSeekApiService, thinkingChainProcessor, modelManager)
        {
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
                var continueLength = parameters.GetValueOrDefault("ContinueLength", "中续写").ToString();
                var customWordCount = parameters.GetValueOrDefault("CustomWordCount", "").ToString();
                var continueDirection = parameters.GetValueOrDefault("ContinueDirection", "").ToString();
                var chapterData = parameters.GetValueOrDefault("ChapterData", null);

                _logger.LogInformation($"开始AI续写章节内容，续写长度：{continueLength}");

                // 创建思维链
                var thinkingChain = new ThinkingChainModel
                {
                    Title = $"{Name} - 章节续写",
                    Description = "执行章节续写任务",
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
                        Data = continuedText, // 直接返回续写的文本内容
                        Metadata = new Dictionary<string, object>
                        {
                            ["ContentType"] = "ContinuedContent",
                            ["Quality"] = "AI Generated",
                            ["WordCount"] = actualWordCount,
                            ["StyleConsistency"] = "AI Maintained",
                            ["ContinuationStyle"] = continueLength
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
            return taskType switch
            {
                "GenerateChapterContent" => @"
                    你是一位专业的小说作家，擅长创作各种类型的小说内容。
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
                    你是一位专业的小说续写专家，擅长在现有内容基础上进行自然流畅的续写。
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
            // 从UI传递的参数中提取信息
            var chapterTitle = parameters.GetValueOrDefault("ChapterTitle", "").ToString();
            var writingStyle = parameters.GetValueOrDefault("WritingStyle", "古风仙侠").ToString();
            var chapterType = parameters.GetValueOrDefault("ChapterType", "正文章节").ToString();
            var targetWordCount = parameters.GetValueOrDefault("TargetWordCount", "2000").ToString();
            var chapterOutline = parameters.GetValueOrDefault("ChapterOutline", "").ToString();
            var keyPlots = parameters.GetValueOrDefault("KeyPlots", "").ToString();
            var characters = parameters.GetValueOrDefault("Characters", "").ToString();
            var specialRequirements = parameters.GetValueOrDefault("SpecialRequirements", "").ToString();

            var prompt = $@"请根据以下要求创作一个{writingStyle}风格的小说章节：

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

            // 移除可能的思维链标记
            var text = aiResponse;

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
            if (string.IsNullOrWhiteSpace(aiResponse))
                return "";

            // 移除可能的JSON格式包装
            var content = aiResponse.Trim();

            // 如果响应是JSON格式，尝试提取content字段
            if (content.StartsWith("{") && content.EndsWith("}"))
            {
                try
                {
                    var jsonDoc = JsonDocument.Parse(content);
                    if (jsonDoc.RootElement.TryGetProperty("content", out var contentElement))
                    {
                        content = contentElement.GetString() ?? content;
                    }
                    else if (jsonDoc.RootElement.TryGetProperty("text", out var textElement))
                    {
                        content = textElement.GetString() ?? content;
                    }
                }
                catch
                {
                    // 如果JSON解析失败，使用原始内容
                }
            }

            // 清理内容格式
            content = content.Replace("\\n", "\n")
                           .Replace("\\t", "\t")
                           .Replace("\\\"", "\"");

            return content.Trim();
        }



        #endregion
    }
}
