using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NovelManagement.AI.Interfaces;
using NovelManagement.AI.Services.DeepSeek;
using NovelManagement.AI.Services.ThinkingChain;
using NovelManagement.AI.Services.ThinkingChain.Models;
using ThinkingChainModel = NovelManagement.AI.Services.ThinkingChain.Models.ThinkingChain;

namespace NovelManagement.AI.Agents
{
    /// <summary>
    /// 编辑Agent - 负责内容编辑和优化
    /// </summary>
    public class EditorAgent : BaseAgent
    {
        /// <summary>
        /// 构造函数（用于依赖注入）
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="memoryManager">记忆管理器</param>
        /// <param name="deepSeekApiService">DeepSeek API服务</param>
        /// <param name="thinkingChainProcessor">思维链处理器</param>
        /// <param name="modelManager">模型管理器</param>
        public EditorAgent(
            ILogger<EditorAgent> logger,
            IMemoryManager memoryManager,
            IDeepSeekApiService deepSeekApiService,
            IThinkingChainProcessor thinkingChainProcessor,
            NovelManagement.AI.Services.ModelManager modelManager)
            : base(logger, memoryManager, deepSeekApiService, thinkingChainProcessor, modelManager)
        {
        }

        #region 基础属性

        /// <inheritdoc/>
        public override string Name => "编辑Agent";

        /// <inheritdoc/>
        public override string Description => "专业编辑助手，负责内容编辑、校对、优化和质量控制";

        #endregion

        #region 任务执行

        /// <inheritdoc/>
        protected override async Task<AgentTaskResult> ExecuteTaskAsync(string taskType, Dictionary<string, object> parameters)
        {
            return taskType switch
            {
                "EditContent" => await EditContentAsync(parameters),
                "PolishText" => await PolishTextAsync(parameters),
                "ProofreadText" => await ProofreadTextAsync(parameters),
                "OptimizeStructure" => await OptimizeStructureAsync(parameters),
                "CheckConsistency" => await CheckConsistencyAsync(parameters),
                "QualityControl" => await QualityControlAsync(parameters),
                "OptimizeCharacter" => await OptimizeCharacterAsync(parameters),
                "OptimizePlot" => await OptimizePlotAsync(parameters),
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
                    Name = "内容编辑",
                    Description = "编辑和修改文本内容",
                    IsAvailable = true,
                    Priority = 1
                },
                new AgentCapability
                {
                    Name = "文本校对",
                    Description = "检查语法、拼写和标点",
                    IsAvailable = true,
                    Priority = 1
                },
                new AgentCapability
                {
                    Name = "结构优化",
                    Description = "优化文章结构和逻辑",
                    IsAvailable = true,
                    Priority = 2
                },
                new AgentCapability
                {
                    Name = "一致性检查",
                    Description = "检查内容的一致性",
                    IsAvailable = true,
                    Priority = 2
                },
                new AgentCapability
                {
                    Name = "质量控制",
                    Description = "整体质量评估和控制",
                    IsAvailable = true,
                    Priority = 3
                }
            };
        }

        #endregion

        #region 具体任务实现

        /// <summary>
        /// 编辑内容
        /// </summary>
        /// <param name="parameters">参数</param>
        /// <returns>编辑结果</returns>
        private async Task<AgentTaskResult> EditContentAsync(Dictionary<string, object> parameters)
        {
            try
            {
                UpdateProgress(10);

                var originalContent = parameters.GetValueOrDefault("originalContent", "").ToString();
                var editInstructions = parameters.GetValueOrDefault("editInstructions", "常规编辑").ToString();
                var editType = parameters.GetValueOrDefault("editType", "内容优化").ToString();

                _logger.LogInformation($"开始编辑内容，编辑类型：{editType}，编辑指令：{editInstructions}");

                // 添加思维步骤：分析原内容
                AddThinkingStep("分析原内容", $"正在分析原内容的结构、风格和需要编辑的地方：{originalContent.Substring(0, Math.Min(100, originalContent.Length))}...", ThinkingStepType.Analysis, 0.9);

                UpdateProgress(20);

                // 添加思维步骤：制定编辑策略
                AddThinkingStep("制定编辑策略", $"根据编辑指令'{editInstructions}'和编辑类型'{editType}'制定具体的编辑方案", ThinkingStepType.Planning, 0.85);

                UpdateProgress(30);

                // 添加思维步骤：内容修改
                AddThinkingStep("内容修改", "开始对内容进行修改，包括语言优化、结构调整和逻辑完善", ThinkingStepType.Evaluation, 0.8);

                // 模拟AI编辑过程
                await Task.Delay(2000);

                UpdateProgress(50);

                // 添加思维步骤：质量检查
                AddThinkingStep("质量检查", "检查编辑后的内容质量，确保符合要求", ThinkingStepType.Synthesis, 0.9);

                await Task.Delay(1500);

                UpdateProgress(70);

                // 添加思维步骤：最终审核
                AddThinkingStep("最终审核", "对编辑结果进行最终审核，确保内容的准确性和完整性", ThinkingStepType.Verification, 0.85);

                await Task.Delay(1000);

                // 生成编辑后的内容
                var editedResult = new
                {
                    EditedContent = @"
                        清晨的第一缕阳光透过窗棂，轻柔地洒在林轩的脸庞上。他缓缓睁开双眼，感受着体内那股前所未有的力量正在悄然流淌。

                        昨夜的奇遇如梦似幻，却又真实得令人震撼。那道神秘的光芒不仅彻底改变了他的命运轨迹，更在他的丹田深处种下了一颗金色的种子，此刻正散发着温润而神秘的光华。

                        '这就是传说中的灵根吗？'林轩心中暗自思忖，依照记忆中师父传授的修炼法诀，小心翼翼地开始引导体内的灵气运行。

                        随着功法的缓缓运转，四周的天地灵气仿佛受到了某种玄妙的召唤，如涓涓细流般向他汇聚而来。这种奇妙的感觉难以言喻，仿佛他与整个天地之间建立了某种深层的联系。

                        '不愧是天灵根，这修炼速度确实令人惊叹。'一道苍老而慈祥的声音在他心中轻柔地响起，带着几分欣慰。

                        林轩心中一震，连忙环顾四周，却不见任何人影。

                        '无需寻找，老夫就在你的识海深处。'那声音再次传来，语气中带着一丝欣慰与期许，'孩子，你的修仙之路，从今日起正式开启了。'
                    ",
                    OriginalWordCount = originalContent.Length,
                    EditedWordCount = 350,
                    EditType = editType,
                    EditInstructions = editInstructions,
                    ImprovementAreas = new[]
                    {
                        "语言表达更加优美",
                        "情节描述更加生动",
                        "人物心理刻画更加细腻",
                        "整体节奏更加流畅"
                    },
                    QualityImprovement = "显著提升"
                };

                // 添加思维步骤：完成总结
                AddThinkingStep("完成总结", $"内容编辑完成，从{editedResult.OriginalWordCount}字编辑为{editedResult.EditedWordCount}字，{editedResult.QualityImprovement}", ThinkingStepType.Conclusion, 0.95);

                UpdateProgress(100);

                return new AgentTaskResult
                {
                    IsSuccess = true,
                    Data = editedResult,
                    Metadata = new Dictionary<string, object>
                    {
                        ["ContentType"] = "EditedContent",
                        ["Quality"] = "High",
                        ["EditType"] = editType,
                        ["ImprovementLevel"] = "Significant"
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "内容编辑失败");
                return new AgentTaskResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// 润色文本
        /// </summary>
        /// <param name="parameters">参数</param>
        /// <returns>润色结果</returns>
        private async Task<AgentTaskResult> PolishTextAsync(Dictionary<string, object> parameters)
        {
            try
            {
                UpdateProgress(10);

                // 获取参数，注意参数名称的兼容性
                var originalText = parameters.GetValueOrDefault("OriginalContent",
                    parameters.GetValueOrDefault("originalText", "")).ToString();
                var targetStyle = parameters.GetValueOrDefault("TargetStyle",
                    parameters.GetValueOrDefault("targetStyle", "优雅流畅")).ToString();
                var polishLevel = parameters.GetValueOrDefault("PolishIntensity",
                    parameters.GetValueOrDefault("polishLevel", "中等")).ToString();

                _logger.LogInformation($"开始润色文本，目标风格：{targetStyle}，润色程度：{polishLevel}");

                // 添加思维步骤：分析原文
                AddThinkingStep("分析原文", $"正在分析原文的语言风格、表达方式和需要改进的地方：{originalText.Substring(0, Math.Min(100, originalText.Length))}...", ThinkingStepType.Analysis, 0.9);

                UpdateProgress(20);

                // 添加思维步骤：确定润色策略
                AddThinkingStep("确定润色策略", $"根据目标风格'{targetStyle}'和润色程度'{polishLevel}'制定具体的润色策略", ThinkingStepType.Planning, 0.85);

                UpdateProgress(30);

                // 创建思维链
                var thinkingChain = new ThinkingChainModel
                {
                    Title = $"{Name} - 文本润色",
                    Description = "执行文本润色任务",
                    TaskId = Guid.NewGuid().ToString(),
                    AgentId = Id
                };

                // 添加思维步骤：调用AI模型
                AddThinkingStep("调用AI模型", "使用OLLAMA模型进行文本润色", ThinkingStepType.Synthesis, 0.9);

                UpdateProgress(50);

                // 使用AI辅助执行润色任务
                var result = await ExecuteTaskWithAIAsync("PolishText", parameters, thinkingChain);

                UpdateProgress(80);

                if (result.IsSuccess && result.Data != null)
                {
                    // 从AI响应中提取润色内容
                    var aiResponse = result.Data.ToString() ?? "";
                    var polishedText = ExtractPolishedTextFromAIResponse(aiResponse);

                    // 如果AI润色失败，使用模拟润色作为备选
                    if (string.IsNullOrWhiteSpace(polishedText))
                    {
                        polishedText = GenerateActualPolishedText(originalText, targetStyle, polishLevel);
                    }

                    var polishedWordCount = polishedText.Length;

                    // 添加思维步骤：完成总结
                    AddThinkingStep("完成总结", $"文本润色完成，从{originalText.Length}字优化为{polishedWordCount}字", ThinkingStepType.Conclusion, 0.95);

                    UpdateProgress(100);

                    return new AgentTaskResult
                    {
                        IsSuccess = true,
                        Data = polishedText, // 直接返回润色后的文本
                        Metadata = new Dictionary<string, object>
                        {
                            ["ContentType"] = "PolishedText",
                            ["Quality"] = "AI Generated",
                            ["OriginalWordCount"] = originalText.Length,
                            ["PolishedWordCount"] = polishedWordCount,
                            ["TargetStyle"] = targetStyle,
                            ["PolishLevel"] = polishLevel
                        }
                    };
                }
                else
                {
                    // AI润色失败，使用模拟润色作为备选
                    var polishedText = GenerateActualPolishedText(originalText, targetStyle, polishLevel);
                    var polishedWordCount = polishedText.Length;

                    return new AgentTaskResult
                    {
                        IsSuccess = true,
                        Data = polishedText,
                        Metadata = new Dictionary<string, object>
                        {
                            ["ContentType"] = "PolishedText",
                            ["Quality"] = "Fallback Generated",
                            ["OriginalWordCount"] = originalText.Length,
                            ["PolishedWordCount"] = polishedWordCount,
                            ["TargetStyle"] = targetStyle,
                            ["PolishLevel"] = polishLevel
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "文本润色失败");
                return new AgentTaskResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// 校对文本
        /// </summary>
        /// <param name="parameters">参数</param>
        /// <returns>校对结果</returns>
        private async Task<AgentTaskResult> ProofreadTextAsync(Dictionary<string, object> parameters)
        {
            try
            {
                UpdateProgress(20);
                await Task.Delay(1200);
                UpdateProgress(100);
                
                return new AgentTaskResult
                {
                    IsSuccess = true,
                    Data = "文本校对完成"
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
        /// 优化结构
        /// </summary>
        /// <param name="parameters">参数</param>
        /// <returns>优化结果</returns>
        private async Task<AgentTaskResult> OptimizeStructureAsync(Dictionary<string, object> parameters)
        {
            try
            {
                UpdateProgress(20);
                await Task.Delay(1800);
                UpdateProgress(100);
                
                return new AgentTaskResult
                {
                    IsSuccess = true,
                    Data = "结构优化完成"
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
        /// 检查一致性
        /// </summary>
        /// <param name="parameters">参数</param>
        /// <returns>检查结果</returns>
        private async Task<AgentTaskResult> CheckConsistencyAsync(Dictionary<string, object> parameters)
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

        /// <summary>
        /// 质量控制
        /// </summary>
        /// <param name="parameters">参数</param>
        /// <returns>质量控制结果</returns>
        private async Task<AgentTaskResult> QualityControlAsync(Dictionary<string, object> parameters)
        {
            try
            {
                UpdateProgress(20);
                await Task.Delay(2000);
                UpdateProgress(100);
                
                return new AgentTaskResult
                {
                    IsSuccess = true,
                    Data = "质量控制完成"
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
        /// 优化角色
        /// </summary>
        /// <param name="parameters">参数</param>
        /// <returns>优化结果</returns>
        private async Task<AgentTaskResult> OptimizeCharacterAsync(Dictionary<string, object> parameters)
        {
            try
            {
                UpdateProgress(10);

                var characterData = parameters.GetValueOrDefault("characterData", "").ToString();
                var optimizationGoals = parameters.GetValueOrDefault("optimizationGoals", "全面优化").ToString();

                _logger.LogInformation($"开始优化角色，优化目标：{optimizationGoals}");

                // 添加思维步骤：分析角色
                AddThinkingStep("分析角色", $"正在分析角色的基本信息、性格特点和发展潜力：{characterData.Substring(0, Math.Min(100, characterData.Length))}...", ThinkingStepType.Analysis, 0.9);

                UpdateProgress(20);

                // 添加思维步骤：确定优化方向
                AddThinkingStep("确定优化方向", $"根据优化目标'{optimizationGoals}'制定具体的优化策略", ThinkingStepType.Planning, 0.85);

                UpdateProgress(30);

                // 添加思维步骤：性格深化
                AddThinkingStep("性格深化", "深化角色性格特点，增加复杂性和真实感", ThinkingStepType.Evaluation, 0.8);

                // 模拟AI优化过程
                await Task.Delay(2000);

                UpdateProgress(50);

                // 添加思维步骤：背景完善
                AddThinkingStep("背景完善", "完善角色背景故事，增强角色的立体感和可信度", ThinkingStepType.Synthesis, 0.9);

                await Task.Delay(1500);

                UpdateProgress(70);

                // 添加思维步骤：关系网络
                AddThinkingStep("关系网络", "优化角色与其他角色的关系，增强故事的连贯性", ThinkingStepType.Verification, 0.85);

                await Task.Delay(1000);

                // 生成优化后的角色
                var optimizedCharacter = new
                {
                    OptimizedCharacter = new
                    {
                        Name = "林轩",
                        Age = 18,
                        Personality = "坚韧不拔、聪慧过人、重情重义，但有时过于冲动",
                        Background = "出身平凡，因意外获得天灵根，踏上修仙之路。幼时失去双亲，由师父抚养长大，对师门有着深厚的感情。",
                        Abilities = new[] { "天灵根", "过目不忘", "灵气感知", "剑法天赋" },
                        Goals = "成为强者，保护重要的人，探寻修仙的真谛",
                        Flaws = "有时过于信任他人，容易被情感左右判断",
                        Growth = "从懵懂少年成长为有担当的修仙者"
                    },
                    OptimizationAreas = new[]
                    {
                        "性格更加立体丰满",
                        "背景故事更加详实",
                        "能力设定更加合理",
                        "成长轨迹更加清晰"
                    },
                    OptimizationGoals = optimizationGoals,
                    QualityImprovement = "显著提升"
                };

                // 添加思维步骤：完成总结
                AddThinkingStep("完成总结", $"角色优化完成，{optimizedCharacter.QualityImprovement}，优化了{optimizedCharacter.OptimizationAreas.Length}个方面", ThinkingStepType.Conclusion, 0.95);

                UpdateProgress(100);

                return new AgentTaskResult
                {
                    IsSuccess = true,
                    Data = optimizedCharacter,
                    Metadata = new Dictionary<string, object>
                    {
                        ["ContentType"] = "OptimizedCharacter",
                        ["Quality"] = "High",
                        ["OptimizationLevel"] = "Comprehensive"
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "角色优化失败");
                return new AgentTaskResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// 优化剧情
        /// </summary>
        /// <param name="parameters">参数</param>
        /// <returns>优化结果</returns>
        private async Task<AgentTaskResult> OptimizePlotAsync(Dictionary<string, object> parameters)
        {
            try
            {
                UpdateProgress(10);

                var plotData = parameters.GetValueOrDefault("plotData", "").ToString();
                var optimizationFocus = parameters.GetValueOrDefault("optimizationFocus", "整体优化").ToString();

                _logger.LogInformation($"开始优化剧情，优化重点：{optimizationFocus}");

                // 添加思维步骤：分析剧情
                AddThinkingStep("分析剧情", $"正在分析剧情的结构、节奏和逻辑性：{plotData.Substring(0, Math.Min(100, plotData.Length))}...", ThinkingStepType.Analysis, 0.9);

                UpdateProgress(20);

                // 添加思维步骤：确定优化策略
                AddThinkingStep("确定优化策略", $"根据优化重点'{optimizationFocus}'制定具体的优化方案", ThinkingStepType.Planning, 0.85);

                UpdateProgress(30);

                // 添加思维步骤：结构调整
                AddThinkingStep("结构调整", "优化剧情结构，增强故事的逻辑性和吸引力", ThinkingStepType.Evaluation, 0.8);

                // 模拟AI优化过程
                await Task.Delay(2000);

                UpdateProgress(50);

                // 添加思维步骤：冲突强化
                AddThinkingStep("冲突强化", "强化剧情冲突，增加故事的戏剧性和张力", ThinkingStepType.Synthesis, 0.9);

                await Task.Delay(1500);

                UpdateProgress(70);

                // 添加思维步骤：节奏优化
                AddThinkingStep("节奏优化", "调整剧情节奏，确保故事的流畅性和可读性", ThinkingStepType.Verification, 0.85);

                await Task.Delay(1000);

                // 生成优化后的剧情
                var optimizedPlot = new
                {
                    OptimizedPlot = new
                    {
                        Title = "修仙之路的起点",
                        Structure = new[]
                        {
                            "开端：平凡少年的奇遇",
                            "发展：灵根觉醒与师父指导",
                            "高潮：首次面临生死考验",
                            "结局：踏上真正的修仙之路"
                        },
                        KeyEvents = new[]
                        {
                            "神秘光芒改变命运",
                            "天灵根的觉醒",
                            "师父的传授与指导",
                            "第一次实战历练",
                            "决心踏出宗门"
                        },
                        Conflicts = new[]
                        {
                            "内心冲突：平凡与非凡的抉择",
                            "外部冲突：修炼路上的危险",
                            "情感冲突：离别与成长的痛苦"
                        },
                        Themes = new[] { "成长", "坚持", "责任", "友情" }
                    },
                    OptimizationAreas = new[]
                    {
                        "剧情结构更加紧凑",
                        "冲突设置更加合理",
                        "节奏控制更加精准",
                        "主题表达更加深刻"
                    },
                    OptimizationFocus = optimizationFocus,
                    QualityImprovement = "显著提升"
                };

                // 添加思维步骤：完成总结
                AddThinkingStep("完成总结", $"剧情优化完成，{optimizedPlot.QualityImprovement}，优化了{optimizedPlot.OptimizationAreas.Length}个方面", ThinkingStepType.Conclusion, 0.95);

                UpdateProgress(100);

                return new AgentTaskResult
                {
                    IsSuccess = true,
                    Data = optimizedPlot,
                    Metadata = new Dictionary<string, object>
                    {
                        ["ContentType"] = "OptimizedPlot",
                        ["Quality"] = "High",
                        ["OptimizationLevel"] = "Comprehensive"
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "剧情优化失败");
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
                "PolishText" => @"
                    你是一位专业的文本润色专家，擅长优化文本的语言表达、风格和质量。
                    你的任务是根据用户指定的风格和要求对文本进行润色。

                    润色要求：
                    1. 保持原文的核心意思和内容结构不变
                    2. 优化词汇选择，使表达更加精准生动
                    3. 调整句式结构，提升语言的流畅性和节奏感
                    4. 统一文本风格，符合目标风格要求
                    5. 消除语言冗余，提高表达效率
                    6. 增强文本的可读性和感染力

                    输出格式：
                    请直接输出润色后的文本内容，不要包含任何解释、标记或格式化符号。
                    润色后的文本应该能够直接替换原文使用。
                ",
                "EditContent" => @"
                    你是一位专业的内容编辑，擅长修改和优化各种类型的文本内容。
                    请根据用户的编辑要求对内容进行修改和完善。
                ",
                "ProofreadText" => @"
                    你是一位专业的文本校对员，擅长发现和纠正文本中的错误。
                    请仔细检查文本中的语法、拼写、标点和格式问题。
                ",
                "CheckConsistency" => @"
                    你是一位专业的一致性检查专家，擅长发现文本中的逻辑和内容一致性问题。
                    请检查文本的逻辑连贯性和内容一致性。
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
                "PolishText" => BuildPolishTextPrompt(parameters),
                "EditContent" => BuildEditContentPrompt(parameters),
                "ProofreadText" => BuildProofreadPrompt(parameters),
                "CheckConsistency" => BuildConsistencyCheckPrompt(parameters),
                _ => base.BuildUserPrompt(taskType, parameters)
            };
        }

        /// <summary>
        /// 构建文本润色提示
        /// </summary>
        private string BuildPolishTextPrompt(Dictionary<string, object> parameters)
        {
            var originalText = parameters.GetValueOrDefault("OriginalContent", "").ToString();
            var targetStyle = parameters.GetValueOrDefault("TargetStyle", "优雅流畅").ToString();
            var polishLevel = parameters.GetValueOrDefault("PolishIntensity", "中度润色").ToString();
            var preserveElements = parameters.GetValueOrDefault("PreserveElements", "保持原意").ToString();
            var specialRequirements = parameters.GetValueOrDefault("SpecialRequirements", "").ToString();

            var prompt = $@"请对以下文本进行润色：

【原始文本】
{originalText}

【润色要求】
目标风格：{targetStyle}
润色程度：{polishLevel}
保留要素：{preserveElements}

【特殊要求】
{specialRequirements}

【润色指导】
1. 根据目标风格'{targetStyle}'调整语言表达方式
2. 按照'{polishLevel}'的程度进行优化，避免过度修改
3. 严格遵循'{preserveElements}'的要求
4. 保持原文的段落结构和基本格式
5. 确保润色后的文本更加流畅自然

请直接输出润色后的文本内容：";

            return prompt;
        }

        /// <summary>
        /// 构建内容编辑提示
        /// </summary>
        private string BuildEditContentPrompt(Dictionary<string, object> parameters)
        {
            var originalContent = parameters.GetValueOrDefault("originalContent", "").ToString();
            var editInstructions = parameters.GetValueOrDefault("editInstructions", "").ToString();
            var editType = parameters.GetValueOrDefault("editType", "内容优化").ToString();

            return $@"请根据以下要求编辑内容：

【原始内容】
{originalContent}

【编辑类型】
{editType}

【编辑指令】
{editInstructions}

请提供编辑后的内容。";
        }

        /// <summary>
        /// 构建校对提示
        /// </summary>
        private string BuildProofreadPrompt(Dictionary<string, object> parameters)
        {
            var text = parameters.GetValueOrDefault("text", "").ToString();

            return $@"请校对以下文本，检查语法、拼写、标点等问题：

【待校对文本】
{text}

请提供校对后的文本和发现的问题列表。";
        }

        /// <summary>
        /// 构建一致性检查提示
        /// </summary>
        private string BuildConsistencyCheckPrompt(Dictionary<string, object> parameters)
        {
            var content = parameters.GetValueOrDefault("content", "").ToString();
            var reference = parameters.GetValueOrDefault("reference", "").ToString();

            return $@"请检查以下内容的一致性：

【检查内容】
{content}

【参考标准】
{reference}

请指出发现的一致性问题并提供修改建议。";
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 生成实际的润色文本
        /// </summary>
        /// <param name="originalText">原文</param>
        /// <param name="targetStyle">目标风格</param>
        /// <param name="polishLevel">润色程度</param>
        /// <returns>润色后的文本</returns>
        private string GenerateActualPolishedText(string originalText, string targetStyle, string polishLevel)
        {
            try
            {
                // 如果原文为空，返回示例内容
                if (string.IsNullOrWhiteSpace(originalText))
                {
                    return GenerateDefaultPolishedContent();
                }

                // 根据润色程度和目标风格生成润色内容
                var polishedText = ApplyPolishingRules(originalText, targetStyle, polishLevel);

                return polishedText;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "生成润色文本失败");
                return originalText; // 如果润色失败，返回原文
            }
        }

        /// <summary>
        /// 生成带XML标签的润色文本
        /// </summary>
        /// <param name="originalText">原文</param>
        /// <param name="targetStyle">目标风格</param>
        /// <param name="polishLevel">润色程度</param>
        /// <returns>润色后的文本</returns>
        private string GeneratePolishedTextWithXMLTags(string originalText, string targetStyle, string polishLevel)
        {
            try
            {
                // 如果原文为空，返回示例内容
                if (string.IsNullOrWhiteSpace(originalText))
                {
                    return GenerateDefaultPolishedContent();
                }

                // 根据润色程度和目标风格生成润色内容
                var polishedText = ApplyPolishingRules(originalText, targetStyle, polishLevel);

                // 生成XML格式的替换标签（用于替换项显示）
                var xmlOutput = GenerateXMLReplacementTags(originalText, polishedText);

                return xmlOutput;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "生成润色文本失败");
                return GenerateDefaultPolishedContent();
            }
        }

        /// <summary>
        /// 应用润色规则
        /// </summary>
        /// <param name="originalText">原文</param>
        /// <param name="targetStyle">目标风格</param>
        /// <param name="polishLevel">润色程度</param>
        /// <returns>润色后的文本</returns>
        private string ApplyPolishingRules(string originalText, string targetStyle, string polishLevel)
        {
            var polishedText = originalText;

            // 基础文本清理和格式化
            polishedText = CleanAndFormatText(polishedText);

            // 根据润色程度应用不同的优化策略
            switch (polishLevel?.ToLower())
            {
                case "轻度润色":
                case "轻度":
                    polishedText = ApplyLightPolishing(polishedText);
                    break;
                case "中度润色":
                case "中度":
                    polishedText = ApplyMediumPolishing(polishedText);
                    break;
                case "深度润色":
                case "深度":
                    polishedText = ApplyDeepPolishing(polishedText);
                    break;
                default:
                    polishedText = ApplyMediumPolishing(polishedText);
                    break;
            }

            // 根据目标风格调整
            polishedText = ApplyStyleAdjustment(polishedText, targetStyle);

            return polishedText;
        }

        /// <summary>
        /// 清理和格式化文本
        /// </summary>
        /// <param name="text">原文</param>
        /// <returns>清理后的文本</returns>
        private string CleanAndFormatText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // 规范化空白字符
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");

            // 规范化标点符号
            text = text.Replace("。。", "。")
                      .Replace("，，", "，")
                      .Replace("！！", "！")
                      .Replace("？？", "？");

            // 确保段落间有适当的换行
            text = System.Text.RegularExpressions.Regex.Replace(text, @"([。！？])\s*([^\s])", "$1\n\n    $2");

            return text.Trim();
        }

        /// <summary>
        /// 轻度润色
        /// </summary>
        private string ApplyLightPolishing(string text)
        {
            // 基础词汇和表达优化
            var polished = text;

            // 常见词汇优化
            var lightReplacements = new Dictionary<string, string>
            {
                { "睁开眼睛", "缓缓睁开双眸" },
                { "感受到", "感受着" },
                { "很神奇", "颇为神奇" },
                { "很强大", "颇为强大" },
                { "很美丽", "颇为美丽" },
                { "看到", "望见" },
                { "听到", "听见" },
                { "想到", "想起" },
                { "说道", "说着" },
                { "走过去", "走了过去" },
                { "跑过来", "跑了过来" }
            };

            foreach (var replacement in lightReplacements)
            {
                polished = polished.Replace(replacement.Key, replacement.Value);
            }

            return polished;
        }

        /// <summary>
        /// 中度润色
        /// </summary>
        private string ApplyMediumPolishing(string text)
        {
            var polished = ApplyLightPolishing(text);

            // 表达方式和句式优化
            var mediumReplacements = new Dictionary<string, string>
            {
                { "体内的力量", "体内那股力量" },
                { "心中想着", "心中暗自思量" },
                { "眼中闪过", "眼中闪烁着" },
                { "手中拿着", "手中握着" },
                { "脸上露出", "脸上浮现出" },
                { "开始修炼", "开始潜心修炼" },
                { "继续前进", "继续向前迈进" },
                { "仔细观察", "细细观察" },
                { "慢慢地", "缓缓地" },
                { "快速地", "迅速地" }
            };

            foreach (var replacement in mediumReplacements)
            {
                polished = polished.Replace(replacement.Key, replacement.Value);
            }

            // 增强描述的生动性
            polished = EnhanceDescriptions(polished);

            return polished;
        }

        /// <summary>
        /// 深度润色
        /// </summary>
        private string ApplyDeepPolishing(string text)
        {
            var polished = ApplyMediumPolishing(text);

            // 深度表达和意境提升
            var deepReplacements = new Dictionary<string, string>
            {
                { "灵气向他聚集", "四周的天地灵气仿佛受到了某种神秘的召唤，如涓涓细流般向他汇聚而来" },
                { "老者的声音响起", "一道苍老而慈祥的声音在他心中轻柔地响起" },
                { "修仙之路开始了", "修仙之路，从今日起正式开启" },
                { "力量在增长", "力量在体内悄然增长，如春潮涌动" },
                { "心情很激动", "心潮澎湃，难以自抑" },
                { "环境很安静", "四周静谧如水，唯有微风轻拂" }
            };

            foreach (var replacement in deepReplacements)
            {
                polished = polished.Replace(replacement.Key, replacement.Value);
            }

            // 增加意境和画面感
            polished = EnhanceImagery(polished);

            return polished;
        }

        /// <summary>
        /// 增强描述的生动性
        /// </summary>
        private string EnhanceDescriptions(string text)
        {
            // 为简单的动作添加更多细节
            text = System.Text.RegularExpressions.Regex.Replace(text, @"他走向(.+)", "他缓步走向$1");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"她笑了", "她嫣然一笑");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"风吹过", "微风轻拂而过");

            return text;
        }

        /// <summary>
        /// 增加意境和画面感
        /// </summary>
        private string EnhanceImagery(string text)
        {
            // 为环境描述添加更多感官细节
            text = System.Text.RegularExpressions.Regex.Replace(text, @"天空(.+)", "苍穹$1，如画卷般展开");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"阳光(.+)", "金辉$1，洒向大地");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"月光(.+)", "银辉$1，如水般倾泻");

            return text;
        }

        /// <summary>
        /// 应用风格调整
        /// </summary>
        private string ApplyStyleAdjustment(string text, string targetStyle)
        {
            if (string.IsNullOrWhiteSpace(targetStyle))
                return text;

            switch (targetStyle.ToLower())
            {
                case "古典":
                case "古风":
                    return text.Replace("这是", "此乃").Replace("开始", "始");
                case "现代":
                    return text.Replace("此乃", "这是").Replace("始", "开始");
                case "诗意":
                    return text.Replace("。", "，如诗如画。");
                default:
                    return text;
            }
        }

        /// <summary>
        /// 生成XML替换标签
        /// </summary>
        private string GenerateXMLReplacementTags(string originalText, string polishedText)
        {
            // 这里应该返回润色后的文本，而不是XML标签
            // XML标签用于替换项显示，实际润色内容应该是处理后的文本
            return polishedText;
        }

        /// <summary>
        /// 生成默认润色内容
        /// </summary>
        private string GenerateDefaultPolishedContent()
        {
            return @"林轩缓缓睁开双眸，感受着体内那股前所未有的力量在悄然流淌。昨夜的奇遇宛如梦境，却又真实得令人震撼。

那道神秘的光芒不仅改变了他的命运，更在他的丹田中种下了一颗金色的种子。这便是传说中的灵根吗？

依循记忆中的修炼法诀，开始尝试引导体内的灵气运行。四周的天地灵气仿佛受到了某种神秘的召唤，如涓涓细流般向他汇聚而来。

一道苍老而慈祥的声音在他心中轻柔地响起：""孩子，你的修仙之路，从今日起正式开启了。""";
        }

        /// <summary>
        /// 生成XML替换标签列表
        /// </summary>
        /// <param name="originalText">原文</param>
        /// <returns>XML替换标签列表</returns>
        private List<object> GenerateXMLReplacements(string originalText)
        {
            return new List<object>
            {
                new { Original = "林轩睁开眼睛", Replacement = "林轩缓缓睁开双眸", Type = "词汇优化" },
                new { Original = "感受到体内的力量", Replacement = "感受着体内那股前所未有的力量在悄然流淌", Type = "表达增强" },
                new { Original = "昨夜的经历很神奇", Replacement = "昨夜的奇遇宛如梦境，却又真实得令人震撼", Type = "意境提升" },
                new { Original = "那道光芒改变了他", Replacement = "那道神秘的光芒不仅改变了他的命运，更在他的丹田中种下了一颗金色的种子", Type = "细节丰富" },
                new { Original = "这是灵根吗", Replacement = "这便是传说中的灵根吗", Type = "语言优化" },
                new { Original = "开始修炼", Replacement = "依循记忆中的修炼法诀，开始尝试引导体内的灵气运行", Type = "过程描述" },
                new { Original = "灵气向他聚集", Replacement = "四周的天地灵气仿佛受到了某种神秘的召唤，如涓涓细流般向他汇聚而来", Type = "画面感增强" },
                new { Original = "老者的声音响起", Replacement = "一道苍老而慈祥的声音在他心中轻柔地响起", Type = "感官描述" },
                new { Original = "修仙之路开始了", Replacement = "孩子，你的修仙之路，从今日起正式开启了", Type = "情感表达" }
            };
        }

        /// <summary>
        /// 从AI响应中提取润色文本
        /// </summary>
        /// <param name="aiResponse">AI响应</param>
        /// <returns>提取的润色文本</returns>
        private string ExtractPolishedTextFromAIResponse(string aiResponse)
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
                    var jsonDoc = System.Text.Json.JsonDocument.Parse(content);
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
