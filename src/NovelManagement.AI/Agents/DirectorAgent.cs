using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NovelManagement.AI.Interfaces;
using NovelManagement.AI.Services.DeepSeek;
using NovelManagement.AI.Services.ThinkingChain;
using NovelManagement.AI.Services.ThinkingChain.Models;
using NovelManagement.AI.Utilities;
using ThinkingChainModel = NovelManagement.AI.Services.ThinkingChain.Models.ThinkingChain;

namespace NovelManagement.AI.Agents
{
    /// <summary>
    /// 编剧Agent - 负责大纲、设定、主线规划
    /// </summary>
    public class DirectorAgent : BaseAgent
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="memoryManager">记忆管理器</param>
        /// <param name="deepSeekApiService">DeepSeek API服务</param>
        /// <param name="thinkingChainProcessor">思维链处理器</param>
        /// <param name="modelManager">模型管理器</param>
        public DirectorAgent(
            ILogger<DirectorAgent> logger,
            IMemoryManager memoryManager,
            IDeepSeekApiService deepSeekApiService,
            IThinkingChainProcessor thinkingChainProcessor,
            NovelManagement.AI.Services.ModelManager modelManager)
            : base(logger, memoryManager, deepSeekApiService, thinkingChainProcessor, modelManager)
        {
        }

        #region 基础属性

        /// <inheritdoc/>
        public override string Name => "编剧Agent";

        /// <inheritdoc/>
        public override string Description => "总体剧情架构师，负责大纲生成、世界设定构建、主线规划和章节安排";

        #endregion

        #region 任务执行

        /// <inheritdoc/>
        protected override async Task<AgentTaskResult> ExecuteTaskAsync(string taskType, Dictionary<string, object> parameters)
        {
            return taskType switch
            {
                "AnalyzeTheme" => await AnalyzeThemeAsync(parameters),
                "GenerateCharacter" => await GenerateCharacterAsync(parameters),
                "GenerateOutline" => await GenerateOutlineAsync(parameters),
                "GeneratePlot" => await GeneratePlotAsync(parameters),
                "GetPlotSuggestions" => await GetPlotSuggestionsAsync(parameters),
                "CreateWorldSetting" => await CreateWorldSettingAsync(parameters),
                "DesignCharacters" => await DesignCharactersAsync(parameters),
                "PlanChapterStructure" => await PlanChapterStructureAsync(parameters),
                "OptimizeOutline" => await OptimizeOutlineAsync(parameters),
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
                    Name = "主题分析",
                    Description = "分析小说主题和核心要素",
                    IsAvailable = true,
                    Priority = 1
                },
                new AgentCapability
                {
                    Name = "大纲生成",
                    Description = "生成详细的故事大纲",
                    IsAvailable = true,
                    Priority = 1
                },
                new AgentCapability
                {
                    Name = "剧情生成",
                    Description = "生成剧情梗概和关键冲突",
                    IsAvailable = true,
                    Priority = 1
                },
                new AgentCapability
                {
                    Name = "剧情建议",
                    Description = "根据设定提供剧情推进建议",
                    IsAvailable = true,
                    Priority = 2
                },
                new AgentCapability
                {
                    Name = "世界设定创建",
                    Description = "创建完整的世界观设定",
                    IsAvailable = true,
                    Priority = 2
                },
                new AgentCapability
                {
                    Name = "角色设计",
                    Description = "设计主要人物角色",
                    IsAvailable = true,
                    Priority = 2
                },
                new AgentCapability
                {
                    Name = "章节结构规划",
                    Description = "规划章节结构和卷宗安排",
                    IsAvailable = true,
                    Priority = 3
                },
                new AgentCapability
                {
                    Name = "大纲优化",
                    Description = "根据反馈优化大纲",
                    IsAvailable = true,
                    Priority = 3
                }
            };
        }

        #endregion

        #region 具体任务实现

        /// <summary>
        /// 分析主题
        /// </summary>
        /// <param name="parameters">参数</param>
        /// <returns>分析结果</returns>
        private async Task<AgentTaskResult> AnalyzeThemeAsync(Dictionary<string, object> parameters)
        {
            try
            {
                UpdateProgress(10);
                
                var theme = parameters.GetValueOrDefault("theme", "").ToString();
                var genre = parameters.GetValueOrDefault("genre", "").ToString();
                
                _logger.LogInformation($"开始分析主题: {theme}, 类型: {genre}");
                
                UpdateProgress(30);
                
                // 模拟AI分析过程
                await Task.Delay(2000);
                
                UpdateProgress(60);
                
                // 生成分析结果
                var analysis = new
                {
                    CoreTheme = theme,
                    Genre = genre,
                    KeyElements = new[]
                    {
                        "主角成长线",
                        "核心冲突",
                        "世界观基础",
                        "情感主线"
                    },
                    SuggestedStructure = new
                    {
                        Acts = 3,
                        EstimatedChapters = 100,
                        VolumeCount = 5
                    },
                    Recommendations = new[]
                    {
                        "建议采用三幕式结构",
                        "重点突出角色成长",
                        "构建完整的修炼体系",
                        "设计多层次的势力关系"
                    }
                };
                
                UpdateProgress(100);
                
                return new AgentTaskResult
                {
                    IsSuccess = true,
                    Data = analysis,
                    Metadata = new Dictionary<string, object>
                    {
                        ["AnalysisType"] = "ThemeAnalysis",
                        ["Confidence"] = 0.85
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "主题分析失败");
                return new AgentTaskResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// 生成角色
        /// </summary>
        /// <param name="parameters">参数</param>
        /// <returns>角色结果</returns>
        private async Task<AgentTaskResult> GenerateCharacterAsync(Dictionary<string, object> parameters)
        {
            try
            {
                UpdateProgress(10);

                var characterType = parameters.GetValueOrDefault("characterType", "").ToString();
                var faction = parameters.GetValueOrDefault("faction", "").ToString();
                var cultivationLevel = parameters.GetValueOrDefault("cultivationLevel", "").ToString();

                _logger.LogInformation("开始AI生成角色: {CharacterType}", characterType);
                AddThinkingStep("分析角色需求", $"正在分析角色生成需求：类型={characterType}, 势力={faction}, 境界={cultivationLevel}", ThinkingStepType.Analysis, 0.9);

                UpdateProgress(25);

                var thinkingChain = new ThinkingChainModel
                {
                    Title = $"{Name} - 角色生成",
                    Description = "执行角色生成任务",
                    TaskId = Guid.NewGuid().ToString(),
                    AgentId = Id
                };

                AddThinkingStep("调用AI模型", "根据角色条件生成完整角色设定", ThinkingStepType.Synthesis, 0.9);
                UpdateProgress(45);

                var result = await ExecuteTaskWithAIAsync("GenerateCharacter", parameters, thinkingChain);
                UpdateProgress(80);

                if (result.IsSuccess && result.Data != null)
                {
                    AddThinkingStep("完成总结", "成功生成角色设定", ThinkingStepType.Conclusion, 0.95);
                    UpdateProgress(100);

                    return new AgentTaskResult
                    {
                        IsSuccess = true,
                        Data = result.Data,
                        Metadata = new Dictionary<string, object>
                        {
                            ["ContentType"] = "CharacterProfile",
                            ["CharacterType"] = characterType ?? string.Empty,
                            ["Faction"] = faction ?? string.Empty
                        }
                    };
                }

                return new AgentTaskResult
                {
                    IsSuccess = false,
                    ErrorMessage = result.ErrorMessage ?? "AI角色生成失败"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "角色生成失败");
                return new AgentTaskResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// 生成大纲
        /// </summary>
        /// <param name="parameters">参数</param>
        /// <returns>大纲结果</returns>
        private async Task<AgentTaskResult> GenerateOutlineAsync(Dictionary<string, object> parameters)
        {
            try
            {
                UpdateProgress(10);

                var theme = parameters.GetValueOrDefault("theme", "").ToString();
                var requirements = parameters.GetValueOrDefault("requirements", "").ToString();
                var novelType = parameters.GetValueOrDefault("novelType", "").ToString();
                var targetLength = parameters.GetValueOrDefault("targetLength", "").ToString();

                _logger.LogInformation($"开始AI生成大纲: {theme}");

                // 添加思维步骤：分析需求
                AddThinkingStep("分析需求", $"正在分析大纲生成需求：主题={theme}, 类型={novelType}", ThinkingStepType.Analysis, 0.9);

                UpdateProgress(20);

                // 创建思维链
                var thinkingChain = new ThinkingChainModel
                {
                    Title = $"{Name} - 大纲生成",
                    Description = "执行大纲生成任务",
                    TaskId = Guid.NewGuid().ToString(),
                    AgentId = Id
                };

                // 添加思维步骤：调用AI模型
                AddThinkingStep("调用AI模型", "使用OLLAMA模型生成小说大纲", ThinkingStepType.Synthesis, 0.9);

                UpdateProgress(40);

                // 使用AI辅助执行大纲生成任务
                var result = await ExecuteTaskWithAIAsync("GenerateOutline", parameters, thinkingChain);

                UpdateProgress(80);

                if (result.IsSuccess && result.Data != null)
                {
                    // 从AI响应中提取大纲内容
                    var aiResponse = result.Data.ToString() ?? "";
                    var outlineContent = ExtractOutlineFromAIResponse(aiResponse);

                    // 添加思维步骤：完成总结
                    AddThinkingStep("完成总结", "成功生成小说大纲", ThinkingStepType.Conclusion, 0.95);

                    UpdateProgress(100);

                    return new AgentTaskResult
                    {
                        IsSuccess = true,
                        Data = outlineContent, // 直接返回大纲内容
                        Metadata = new Dictionary<string, object>
                        {
                            ["ContentType"] = "Outline",
                            ["Quality"] = "AI Generated",
                            ["Theme"] = theme,
                            ["NovelType"] = novelType
                        }
                    };
                }
                else
                {
                    // AI生成失败，返回错误结果
                    return new AgentTaskResult
                    {
                        IsSuccess = false,
                        ErrorMessage = result.ErrorMessage ?? "AI大纲生成失败"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "大纲生成失败");
                return new AgentTaskResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// 生成剧情梗概
        /// </summary>
        /// <param name="parameters">参数</param>
        /// <returns>剧情结果</returns>
        private async Task<AgentTaskResult> GeneratePlotAsync(Dictionary<string, object> parameters)
        {
            try
            {
                UpdateProgress(10);

                var theme = parameters.GetValueOrDefault("theme", "").ToString();
                var genre = parameters.GetValueOrDefault("genre", "").ToString();
                var requirements = parameters.GetValueOrDefault("requirements", "").ToString();

                _logger.LogInformation("开始AI生成剧情: {Theme}, 类型: {Genre}", theme, genre);

                AddThinkingStep("分析剧情需求", $"正在分析剧情生成需求：主题={theme}, 类型={genre}", ThinkingStepType.Analysis, 0.9);
                UpdateProgress(25);

                var thinkingChain = new ThinkingChainModel
                {
                    Title = $"{Name} - 剧情生成",
                    Description = "执行剧情生成任务",
                    TaskId = Guid.NewGuid().ToString(),
                    AgentId = Id
                };

                AddThinkingStep("调用AI模型", $"根据需求生成剧情梗概：{requirements}", ThinkingStepType.Synthesis, 0.9);
                UpdateProgress(45);

                var aiResponse = await ExecuteCreativeTaskWithAIAsync("GeneratePlot", parameters);
                UpdateProgress(85);

                if (!string.IsNullOrWhiteSpace(aiResponse))
                {
                    var plotContent = ExtractPlotFromAIResponse(aiResponse);
                    AddThinkingStep("完成总结", "成功生成剧情梗概", ThinkingStepType.Conclusion, 0.95);
                    UpdateProgress(100);

                    return new AgentTaskResult
                    {
                        IsSuccess = true,
                        Data = plotContent,
                        Metadata = new Dictionary<string, object>
                        {
                            ["ContentType"] = "Plot",
                            ["Theme"] = theme ?? string.Empty,
                            ["Genre"] = genre ?? string.Empty,
                            ["Message"] = "AI生成剧情完成"
                        }
                    };
                }

                return new AgentTaskResult
                {
                    IsSuccess = false,
                    ErrorMessage = "AI剧情生成失败"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "剧情生成失败");
                return new AgentTaskResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// 获取剧情建议
        /// </summary>
        /// <param name="parameters">参数</param>
        /// <returns>建议结果</returns>
        private async Task<AgentTaskResult> GetPlotSuggestionsAsync(Dictionary<string, object> parameters)
        {
            try
            {
                UpdateProgress(10);

                var theme = parameters.GetValueOrDefault("theme", "").ToString();
                AddThinkingStep("分析现有信息", $"正在根据当前主题生成剧情建议：{theme}", ThinkingStepType.Analysis, 0.85);
                UpdateProgress(30);

                var thinkingChain = new ThinkingChainModel
                {
                    Title = $"{Name} - 剧情建议",
                    Description = "执行剧情建议任务",
                    TaskId = Guid.NewGuid().ToString(),
                    AgentId = Id
                };

                var aiResponse = await ExecuteCreativeTaskWithAIAsync("GetPlotSuggestions", parameters);
                UpdateProgress(85);

                if (!string.IsNullOrWhiteSpace(aiResponse))
                {
                    var suggestionContent = ExtractPlotFromAIResponse(aiResponse);
                    AddThinkingStep("完成总结", "成功生成剧情建议", ThinkingStepType.Conclusion, 0.95);
                    UpdateProgress(100);

                    return new AgentTaskResult
                    {
                        IsSuccess = true,
                        Data = suggestionContent,
                        Metadata = new Dictionary<string, object>
                        {
                            ["ContentType"] = "PlotSuggestions",
                            ["Message"] = "AI获取剧情建议完成"
                        }
                    };
                }

                return new AgentTaskResult
                {
                    IsSuccess = false,
                    ErrorMessage = "AI剧情建议生成失败"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "剧情建议生成失败");
                return new AgentTaskResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// 创建世界设定
        /// </summary>
        /// <param name="parameters">参数</param>
        /// <returns>设定结果</returns>
        private async Task<AgentTaskResult> CreateWorldSettingAsync(Dictionary<string, object> parameters)
        {
            try
            {
                UpdateProgress(10);
                
                _logger.LogInformation("开始创建世界设定");
                
                // 模拟AI创建过程
                await Task.Delay(2500);
                
                UpdateProgress(50);
                
                var worldSetting = new
                {
                    WorldName = "九天仙域",
                    CultivationSystem = new
                    {
                        Levels = new[] { "练气", "筑基", "金丹", "元婴", "化神", "炼虚", "合体", "大乘", "渡劫", "仙人" },
                        Description = "传统修仙体系，以灵气修炼为主"
                    },
                    Geography = new
                    {
                        Continents = new[] { "东胜神洲", "西牛贺洲", "南赡部洲", "北俱芦洲" },
                        SpecialPlaces = new[] { "玄天山脉", "万妖森林", "无尽海域", "九幽深渊" }
                    },
                    Factions = new[]
                    {
                        "玄天宗 - 正道第一大宗",
                        "魔道联盟 - 邪恶势力",
                        "散修联盟 - 中立势力",
                        "妖族部落 - 妖兽势力"
                    }
                };
                
                UpdateProgress(100);
                
                return new AgentTaskResult
                {
                    IsSuccess = true,
                    Data = worldSetting
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "世界设定创建失败");
                return new AgentTaskResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// 设计角色
        /// </summary>
        /// <param name="parameters">参数</param>
        /// <returns>角色设计结果</returns>
        private async Task<AgentTaskResult> DesignCharactersAsync(Dictionary<string, object> parameters)
        {
            try
            {
                UpdateProgress(20);
                await Task.Delay(2000);
                UpdateProgress(100);
                
                return new AgentTaskResult
                {
                    IsSuccess = true,
                    Data = "角色设计完成"
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
        /// 规划章节结构
        /// </summary>
        /// <param name="parameters">参数</param>
        /// <returns>章节结构结果</returns>
        private async Task<AgentTaskResult> PlanChapterStructureAsync(Dictionary<string, object> parameters)
        {
            try
            {
                UpdateProgress(20);
                await Task.Delay(1500);
                UpdateProgress(100);
                
                return new AgentTaskResult
                {
                    IsSuccess = true,
                    Data = "章节结构规划完成"
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
        /// 优化大纲
        /// </summary>
        /// <param name="parameters">参数</param>
        /// <returns>优化结果</returns>
        private async Task<AgentTaskResult> OptimizeOutlineAsync(Dictionary<string, object> parameters)
        {
            try
            {
                UpdateProgress(20);
                await Task.Delay(1800);
                UpdateProgress(100);
                
                return new AgentTaskResult
                {
                    IsSuccess = true,
                    Data = "大纲优化完成"
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
                "GenerateCharacter" => @"
    你是一位专业的小说角色设计师，擅长根据已有设定生成完整、可直接使用的角色资料。
    请结合用户提供的角色类型、势力、境界、性格、背景和能力要求，输出自然、完整、结构清晰的角色设定。
    严禁输出思考过程、分析过程、提示词、Markdown 标题、代码块、列表符号或无关说明。
    仅按以下字段顺序输出，每个字段只输出字段名和正文：
    姓名：
    简介：
    外貌：
    性格：
    背景：
    能力：
    经历：
    关键事件：
                ",
                "GenerateOutline" => @"
                    你是一位专业的小说编剧和策划专家，擅长创作各种类型的小说大纲。
                    你的任务是根据用户提供的主题、类型和要求，创作一个完整、详细、引人入胜的小说大纲。

                    大纲创作要求：
                    1. 结构完整：包含开头、发展、高潮、结尾四个部分
                    2. 情节合理：逻辑清晰，前后呼应，冲突明确
                    3. 人物鲜明：主要角色性格突出，成长轨迹清晰
                    4. 节奏把控：张弛有度，高潮迭起
                    5. 主题深刻：体现积极向上的价值观
                    6. 创新性强：避免俗套，有独特的创意点

                    输出格式：
                    请按照以下结构输出大纲：
                    【小说标题】
                    【主题思想】
                    【故事梗概】
                    【主要角色】
                    【分卷结构】
                    【关键情节】
                ",
                "AnalyzeTheme" => @"
                    你是一位专业的文学主题分析专家，擅长深入分析小说的主题内涵。
                    请根据用户提供的内容进行主题分析。
                ",
                "CreateWorldSetting" => @"
                    你是一位专业的世界观设定专家，擅长创建丰富详细的虚构世界。
                    请根据用户的要求创建完整的世界设定。
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
                "GenerateCharacter" => BuildCharacterGenerationPrompt(parameters),
                "GenerateOutline" => BuildOutlineGenerationPrompt(parameters),
                "AnalyzeTheme" => BuildThemeAnalysisPrompt(parameters),
                "CreateWorldSetting" => BuildWorldSettingPrompt(parameters),
                _ => base.BuildUserPrompt(taskType, parameters)
            };
        }

        /// <summary>
        /// 构建大纲生成提示
        /// </summary>
        private string BuildOutlineGenerationPrompt(Dictionary<string, object> parameters)
        {
            var theme = parameters.GetValueOrDefault("theme", "").ToString();
            var requirements = parameters.GetValueOrDefault("requirements", "").ToString();
            var novelType = parameters.GetValueOrDefault("novelType", "").ToString();
            var targetLength = parameters.GetValueOrDefault("targetLength", "").ToString();
            var mainCharacter = parameters.GetValueOrDefault("mainCharacter", "").ToString();
            var setting = parameters.GetValueOrDefault("setting", "").ToString();

            var prompt = $@"请为我创作一个{novelType}类型的小说大纲：

【创作要求】
主题：{theme}
类型：{novelType}
目标长度：{targetLength}
背景设定：{setting}
主角设定：{mainCharacter}

【特殊要求】
{requirements}

【创作指导】
1. 请确保大纲具有完整的故事结构
2. 主角要有明确的成长轨迹和目标
3. 情节要有足够的冲突和转折
4. 体现积极向上的价值观
5. 避免常见的俗套情节
6. 确保逻辑合理，前后呼应

请按照系统提示中的格式输出完整的小说大纲。";

            return prompt;
        }

        private string BuildCharacterGenerationPrompt(Dictionary<string, object> parameters)
        {
            var name = parameters.GetValueOrDefault("name", "").ToString();
            var characterType = parameters.GetValueOrDefault("characterType", "").ToString();
            var faction = parameters.GetValueOrDefault("faction", "").ToString();
            var cultivationLevel = parameters.GetValueOrDefault("cultivationLevel", "").ToString();
            var description = parameters.GetValueOrDefault("description", "").ToString();
            var appearance = parameters.GetValueOrDefault("appearance", "").ToString();
            var personality = parameters.GetValueOrDefault("personality", "").ToString();
            var background = parameters.GetValueOrDefault("background", "").ToString()
                ?? parameters.GetValueOrDefault("backgroundStory", "").ToString();
            var abilities = parameters.GetValueOrDefault("abilities", "").ToString()
                ?? parameters.GetValueOrDefault("specialAbilities", "").ToString();
            var history = parameters.GetValueOrDefault("history", "").ToString();
            var keyEvents = parameters.GetValueOrDefault("keyEvents", "").ToString();
            var existingCharacters = parameters.GetValueOrDefault("existingCharacters", "").ToString();

            return $@"请生成一个适合小说项目直接使用的角色设定：

【已知信息】
姓名：{name}
角色类型：{characterType}
所属势力：{faction}
修炼境界：{cultivationLevel}
角色描述：{description}
外貌特征：{appearance}
性格特点：{personality}
背景故事：{background}
特殊能力：{abilities}
经历：{history}
关键事件：{keyEvents}

【已有角色参考】
{existingCharacters}

【输出要求】
1. 若姓名为空，请先为角色命名。
2. 输出完整角色资料，重点补全缺失内容。
3. 内容需包含：姓名、外貌、性格、背景、能力、经历、关键事件。
4. 必须严格按以下字段输出：姓名、简介、外貌、性格、背景、能力、经历、关键事件。
5. 不要输出思考过程，不要输出解释，不要输出 JSON，不要输出代码块，不要输出额外符号。";
        }

        /// <summary>
        /// 构建主题分析提示
        /// </summary>
        private string BuildThemeAnalysisPrompt(Dictionary<string, object> parameters)
        {
            var content = parameters.GetValueOrDefault("content", "").ToString();

            return $@"请分析以下内容的主题：

【分析内容】
{content}

请从以下角度进行分析：
1. 核心主题
2. 价值观体现
3. 深层含义
4. 现实意义";
        }

        /// <summary>
        /// 构建世界设定提示
        /// </summary>
        private string BuildWorldSettingPrompt(Dictionary<string, object> parameters)
        {
            var worldType = parameters.GetValueOrDefault("worldType", "").ToString();
            var requirements = parameters.GetValueOrDefault("requirements", "").ToString();

            return $@"请创建一个{worldType}类型的世界设定：

【设定要求】
{requirements}

请包含以下内容：
1. 世界基本设定
2. 地理环境
3. 社会结构
4. 文化背景
5. 特殊规则或法则";
        }

        /// <summary>
        /// 从AI响应中提取大纲内容
        /// </summary>
        /// <param name="aiResponse">AI响应</param>
        /// <returns>提取的大纲内容</returns>
        private string ExtractOutlineFromAIResponse(string aiResponse)
        {
            return AIOutputSanitizer.ExtractCleanOutput(aiResponse, "content", "text", "outline");
        }

        /// <summary>
        /// 直接使用当前默认模型提供者执行剧情类任务，避免AI失败时发生递归回退。
        /// </summary>
        /// <param name="taskType">任务类型</param>
        /// <param name="parameters">任务参数</param>
        /// <returns>AI输出</returns>
        private async Task<string> ExecuteCreativeTaskWithAIAsync(string taskType, Dictionary<string, object> parameters)
        {
            if (_modelManager == null)
            {
                throw new InvalidOperationException("模型管理器未配置，无法执行AI任务。");
            }

            var defaultProvider = _modelManager.GetDefaultProvider();
            if (defaultProvider == null || !defaultProvider.IsAvailable)
            {
                throw new InvalidOperationException("当前没有可用的默认模型提供者。");
            }

            var chatRequest = new ChatRequest
            {
                Model = string.Empty,
                SystemPrompt = BuildSystemPrompt(taskType),
                Messages = new List<ChatMessage>
                {
                    new()
                    {
                        Role = "user",
                        Content = BuildUserPrompt(taskType, parameters),
                        Timestamp = DateTime.UtcNow
                    }
                },
                Temperature = 0.7,
                MaxTokens = 4000
            };

            var chatResponse = await _modelManager.ChatAsync(defaultProvider.ProviderName, chatRequest);
            if (!chatResponse.IsSuccess)
            {
                throw new InvalidOperationException($"{defaultProvider.ProviderName} 模型调用失败: {chatResponse.ErrorMessage}");
            }

            if (string.IsNullOrWhiteSpace(chatResponse.Content))
            {
                throw new InvalidOperationException($"{defaultProvider.ProviderName} 模型返回空响应");
            }

            return chatResponse.Content;
        }

        /// <summary>
        /// 从AI响应中提取剧情/建议内容
        /// </summary>
        /// <param name="aiResponse">AI响应</param>
        /// <returns>提取后的剧情内容</returns>
        private string ExtractPlotFromAIResponse(string aiResponse)
        {
            return AIOutputSanitizer.ExtractCleanOutput(aiResponse, "content", "text", "plot", "story", "suggestions");
        }

        #endregion
    }
}
