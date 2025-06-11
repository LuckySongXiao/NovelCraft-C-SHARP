using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NovelManagement.WPF.Services
{
    /// <summary>
    /// 对话生成服务
    /// </summary>
    public class DialogGenerationService
    {
        #region 字段和属性

        private readonly ILogger<DialogGenerationService>? _logger;
        private readonly AICacheService _cacheService;

        /// <summary>
        /// 对话生成结果
        /// </summary>
        public class DialogGenerationResult
        {
            public string Content { get; set; } = string.Empty;
            public double QualityScore { get; set; }
            public string Characters { get; set; } = string.Empty;
            public string Situation { get; set; } = string.Empty;
            public string Emotion { get; set; } = string.Empty;
            public string Style { get; set; } = string.Empty;
            public DateTime GeneratedAt { get; set; }
            public string? ThinkingChainId { get; set; }
        }

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="cacheService">缓存服务</param>
        public DialogGenerationService(ILogger<DialogGenerationService>? logger = null, AICacheService? cacheService = null)
        {
            _logger = logger;
            // 为AICacheService创建专用的Logger
            var cacheLogger = App.ServiceProvider?.GetService(typeof(ILogger<AICacheService>)) as ILogger<AICacheService>;
            _cacheService = cacheService ?? new AICacheService(cacheLogger);
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 生成对话
        /// </summary>
        /// <param name="parameters">生成参数</param>
        /// <returns>生成结果</returns>
        public async Task<DialogGenerationResult> GenerateDialogueAsync(Dictionary<string, object> parameters)
        {
            try
            {
                _logger?.LogInformation("开始生成对话");

                // 提取参数
                var characters = parameters.GetValueOrDefault("characters", "").ToString() ?? "";
                var relationship = parameters.GetValueOrDefault("relationship", "").ToString() ?? "";
                var situation = parameters.GetValueOrDefault("situation", "").ToString() ?? "";
                var purpose = parameters.GetValueOrDefault("purpose", "").ToString() ?? "";
                var emotion = parameters.GetValueOrDefault("emotion", "").ToString() ?? "";
                var style = parameters.GetValueOrDefault("style", "").ToString() ?? "";
                var length = Convert.ToInt32(parameters.GetValueOrDefault("length", 5));

                // 生成缓存键
                var cacheKey = AICacheKeyGenerator.GenerateDialogueKey(characters, situation, emotion, style);

                // 尝试从缓存获取结果
                var cachedResult = _cacheService.Get<DialogGenerationResult>(cacheKey);
                if (cachedResult != null)
                {
                    _logger?.LogInformation("从缓存获取对话结果");
                    return cachedResult;
                }

                // 缓存未命中，生成新对话
                _logger?.LogInformation("缓存未命中，开始生成新对话");

                // 模拟AI生成过程
                await Task.Delay(2000);

                // 生成对话内容
                var dialogueContent = await GenerateDialogueContent(characters, relationship, situation, purpose, emotion, style, length);

                // 评估对话质量
                var qualityScore = await EvaluateDialogueQuality(dialogueContent, parameters);

                var result = new DialogGenerationResult
                {
                    Content = dialogueContent,
                    QualityScore = qualityScore,
                    Characters = characters,
                    Situation = situation,
                    Emotion = emotion,
                    Style = style,
                    GeneratedAt = DateTime.Now,
                    ThinkingChainId = Guid.NewGuid().ToString()
                };

                // 将结果存入缓存（缓存30分钟）
                _cacheService.Set(cacheKey, result, TimeSpan.FromMinutes(30));

                _logger?.LogInformation("对话生成完成，质量评分: {QualityScore}", qualityScore);
                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "对话生成失败");
                throw;
            }
        }

        /// <summary>
        /// 优化对话
        /// </summary>
        /// <param name="existingDialogue">现有对话</param>
        /// <param name="parameters">优化参数</param>
        /// <returns>优化结果</returns>
        public async Task<DialogGenerationResult> OptimizeDialogueAsync(string existingDialogue, Dictionary<string, object> parameters)
        {
            try
            {
                _logger?.LogInformation("开始优化对话");

                // 模拟AI优化过程
                await Task.Delay(1500);

                // 优化对话内容
                var optimizedContent = await OptimizeDialogueContent(existingDialogue, parameters);

                // 评估优化后的质量
                var qualityScore = await EvaluateDialogueQuality(optimizedContent, parameters);

                var result = new DialogGenerationResult
                {
                    Content = optimizedContent,
                    QualityScore = qualityScore,
                    Characters = parameters.GetValueOrDefault("characters", "").ToString() ?? "",
                    Situation = parameters.GetValueOrDefault("situation", "").ToString() ?? "",
                    Emotion = parameters.GetValueOrDefault("emotion", "").ToString() ?? "",
                    Style = parameters.GetValueOrDefault("style", "").ToString() ?? "",
                    GeneratedAt = DateTime.Now,
                    ThinkingChainId = Guid.NewGuid().ToString()
                };

                _logger?.LogInformation("对话优化完成，质量评分: {QualityScore}", qualityScore);
                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "对话优化失败");
                throw;
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 生成对话内容
        /// </summary>
        /// <param name="characters">角色</param>
        /// <param name="relationship">关系</param>
        /// <param name="situation">情境</param>
        /// <param name="purpose">目的</param>
        /// <param name="emotion">情感</param>
        /// <param name="style">风格</param>
        /// <param name="length">长度</param>
        /// <returns>对话内容</returns>
        private async Task<string> GenerateDialogueContent(string characters, string relationship, string situation, string purpose, string emotion, string style, int length)
        {
            await Task.Delay(100); // 模拟处理时间

            var characterList = characters.Split(',', '，', ';', '；').Select(c => c.Trim()).Where(c => !string.IsNullOrEmpty(c)).ToArray();
            var char1 = characterList.Length > 0 ? characterList[0] : "角色A";
            var char2 = characterList.Length > 1 ? characterList[1] : "角色B";

            // 根据参数生成不同风格的对话
            var dialogues = new List<string>();

            if (style.Contains("古典") || style.Contains("仙侠"))
            {
                dialogues.AddRange(GenerateClassicalDialogue(char1, char2, situation, emotion, length));
            }
            else if (style.Contains("现代"))
            {
                dialogues.AddRange(GenerateModernDialogue(char1, char2, situation, emotion, length));
            }
            else
            {
                dialogues.AddRange(GenerateDefaultDialogue(char1, char2, situation, emotion, length));
            }

            return string.Join("\n\n", dialogues);
        }

        /// <summary>
        /// 生成古典风格对话
        /// </summary>
        private List<string> GenerateClassicalDialogue(string char1, string char2, string situation, string emotion, int length)
        {
            var dialogues = new List<string>
            {
                $"{char1}：「{char2}，此番{situation}，你可有何见解？」",
                $"{char2}：「师兄所言甚是，在下以为此事需要慎重考虑。」",
                $"{char1}：「不错，我等修行之人，当以大局为重。」"
            };

            if (emotion.Contains("愤怒"))
            {
                dialogues.Add($"{char2}：「师兄此言差矣！此事岂能如此草率决定？」");
                dialogues.Add($"{char1}：「你竟敢质疑为兄的判断？」");
            }
            else if (emotion.Contains("高兴"))
            {
                dialogues.Add($"{char2}：「哈哈，师兄英明！此计甚妙！」");
                dialogues.Add($"{char1}：「你我师兄弟同心，何愁大事不成？」");
            }

            return dialogues.Take(Math.Max(3, length)).ToList();
        }

        /// <summary>
        /// 生成现代风格对话
        /// </summary>
        private List<string> GenerateModernDialogue(string char1, string char2, string situation, string emotion, int length)
        {
            var dialogues = new List<string>
            {
                $"{char1}：\"关于{situation}这件事，你怎么看？\"",
                $"{char2}：\"我觉得我们需要更多的信息才能做决定。\"",
                $"{char1}：\"你说得对，我们不能匆忙行事。\""
            };

            if (emotion.Contains("紧张"))
            {
                dialogues.Add($"{char2}：\"说实话，我有点担心...\"");
                dialogues.Add($"{char1}：\"别担心，我们会想出办法的。\"");
            }
            else if (emotion.Contains("兴奋"))
            {
                dialogues.Add($"{char2}：\"太棒了！这个计划听起来很不错！\"");
                dialogues.Add($"{char1}：\"是的，我也很期待结果。\"");
            }

            return dialogues.Take(Math.Max(3, length)).ToList();
        }

        /// <summary>
        /// 生成默认风格对话
        /// </summary>
        private List<string> GenerateDefaultDialogue(string char1, string char2, string situation, string emotion, int length)
        {
            var dialogues = new List<string>
            {
                $"{char1}：{situation}的事情，我们需要好好商量一下。",
                $"{char2}：我同意，这确实是个重要的问题。",
                $"{char1}：那我们就从头开始分析吧。"
            };

            return dialogues.Take(Math.Max(3, length)).ToList();
        }

        /// <summary>
        /// 优化对话内容
        /// </summary>
        private async Task<string> OptimizeDialogueContent(string existingDialogue, Dictionary<string, object> parameters)
        {
            await Task.Delay(100); // 模拟处理时间

            // 简单的优化逻辑：添加更多细节和情感表达
            var lines = existingDialogue.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var optimizedLines = new List<string>();

            foreach (var line in lines)
            {
                if (line.Contains("："))
                {
                    // 为对话添加动作描述
                    var parts = line.Split('：', 2);
                    if (parts.Length == 2)
                    {
                        var character = parts[0];
                        var dialogue = parts[1];
                        
                        // 添加简单的动作描述
                        var actions = new[] { "微微一笑", "点了点头", "沉思片刻", "眼中闪过一丝光芒" };
                        var action = actions[new Random().Next(actions.Length)];
                        
                        optimizedLines.Add($"{character}{action}，说道：{dialogue}");
                    }
                    else
                    {
                        optimizedLines.Add(line);
                    }
                }
                else
                {
                    optimizedLines.Add(line);
                }
            }

            return string.Join("\n\n", optimizedLines);
        }

        /// <summary>
        /// 评估对话质量
        /// </summary>
        private async Task<double> EvaluateDialogueQuality(string dialogueContent, Dictionary<string, object> parameters)
        {
            await Task.Delay(50); // 模拟处理时间

            if (string.IsNullOrEmpty(dialogueContent))
                return 1.0;

            var score = 5.0; // 基础分数

            // 长度评估
            if (dialogueContent.Length > 50) score += 1.0;
            if (dialogueContent.Length > 200) score += 0.5;

            // 对话轮次评估
            var dialogueLines = dialogueContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var dialogueCount = dialogueLines.Count(line => line.Contains("：") || line.Contains(":"));
            
            if (dialogueCount >= 2) score += 1.0;
            if (dialogueCount >= 4) score += 0.5;

            // 人物名称一致性检查
            var characters = parameters.GetValueOrDefault("characters", "").ToString();
            if (!string.IsNullOrEmpty(characters))
            {
                var characterNames = characters.Split(',', '，', ';', '；')
                    .Select(name => name.Trim())
                    .Where(name => !string.IsNullOrEmpty(name));

                var mentionedCharacters = characterNames.Count(name => 
                    dialogueContent.Contains(name, StringComparison.OrdinalIgnoreCase));
                
                if (mentionedCharacters > 0) score += 1.0;
            }

            // 情感表达评估
            var emotion = parameters.GetValueOrDefault("emotion", "").ToString();
            if (!string.IsNullOrEmpty(emotion))
            {
                var emotionKeywords = GetEmotionKeywords(emotion);
                var hasEmotionExpression = emotionKeywords.Any(keyword => 
                    dialogueContent.Contains(keyword, StringComparison.OrdinalIgnoreCase));
                
                if (hasEmotionExpression) score += 1.0;
            }

            // 确保分数在1-10范围内
            return Math.Max(1.0, Math.Min(10.0, score));
        }

        /// <summary>
        /// 获取情感关键词
        /// </summary>
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
    }
}
