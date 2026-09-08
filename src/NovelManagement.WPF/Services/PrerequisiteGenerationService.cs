using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using NovelManagement.AI.Services.RWKV;
using NovelManagement.Application.Services;
using NovelManagement.Application.Interfaces;
using NovelManagement.Application.DTOs;
using NovelManagement.Core.Entities;
using NovelManagement.WPF.Models;
using NovelManagement.WPF.Services;

namespace NovelManagement.WPF.Services
{
    /// <summary>
    /// 前置条件生成服务
    /// 为AI编辑功能自动生成必要的前置数据
    /// </summary>
    public class PrerequisiteGenerationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PrerequisiteGenerationService> _logger;
        private readonly IAIAssistantService? _aiAssistantService;

        public PrerequisiteGenerationService(
            IServiceProvider serviceProvider,
            ILogger<PrerequisiteGenerationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _aiAssistantService = serviceProvider.GetService<IAIAssistantService>();
        }

        /// <summary>
        /// 为项目生成完整的前置条件
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <param name="options">生成选项</param>
        /// <returns>生成结果</returns>
        public async Task<PrerequisiteGenerationResult> GeneratePrerequisitesAsync(Guid projectId, PrerequisiteGenerationOptions? options = null)
        {
            var result = new PrerequisiteGenerationResult { ProjectId = projectId };
            options ??= new PrerequisiteGenerationOptions();

            try
            {
                _logger.LogInformation("开始为项目 {ProjectId} 生成前置条件", projectId);

                // 检查现有数据
                await CheckExistingDataAsync(projectId, result);

                // 根据选项生成缺失的数据（修炼体系最先生成，供角色修为等级联动）
                if (options.GenerateCultivationSystem && result.NeedsCultivationSystem)
                {
                    await EnsureCultivationSystemAsync(projectId, result);
                }

                if (options.GeneratePlotOutlines && result.NeedsPlotOutlines)
                {
                    await GeneratePlotOutlinesAsync(projectId, result);
                }

                if (options.GenerateMainCharacters && result.NeedsMainCharacters)
                {
                    await GenerateMainCharactersAsync(projectId, result);
                }

                if (options.GenerateWorldSettings && result.NeedsWorldSettings)
                {
                    await GenerateWorldSettingsAsync(projectId, result);
                }

                if (options.GenerateFactions && result.NeedsFactions)
                {
                    await GenerateFactionsAsync(projectId, result);
                }

                result.IsSuccess = true;
                result.Message = "前置条件生成完成";

                _logger.LogInformation("项目 {ProjectId} 前置条件生成完成", projectId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成项目 {ProjectId} 前置条件失败", projectId);
                result.IsSuccess = false;
                result.Message = $"生成失败: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 使用AI智能生成前置条件
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <param name="aiPrompt">AI生成提示</param>
        /// <returns>生成结果</returns>
        public async Task<PrerequisiteGenerationResult> GenerateWithAIAsync(Guid projectId, string aiPrompt)
        {
            var result = new PrerequisiteGenerationResult { ProjectId = projectId };

            try
            {
                _logger.LogInformation("开始使用AI为项目 {ProjectId} 生成前置条件", projectId);

                // 检查现有数据
                await CheckExistingDataAsync(projectId, result);

                // 使用AI服务生成个性化内容
                var aiAssistantService = _serviceProvider.GetService<AIAssistantService>();
                if (aiAssistantService != null)
                {
                    await GenerateWithAIAssistantAsync(projectId, result, aiPrompt, aiAssistantService);
                }
                else
                {
                    _logger.LogWarning("AI服务不可用，使用默认生成方式");
                    await GenerateDefaultDataAsync(projectId, result);
                }

                result.IsSuccess = true;
                result.Message = "AI前置条件生成完成";

                _logger.LogInformation("项目 {ProjectId} AI前置条件生成完成", projectId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI生成项目 {ProjectId} 前置条件失败", projectId);
                result.IsSuccess = false;
                result.Message = $"AI生成失败: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 检查现有数据
        /// </summary>
        private async Task CheckExistingDataAsync(Guid projectId, PrerequisiteGenerationResult result)
        {
            try
            {
                // 检查剧情大纲
                var plotService = _serviceProvider.GetService<PlotService>();
                if (plotService != null)
                {
                    var plots = await plotService.GetPlotsByProjectIdAsync(projectId);
                    result.ExistingPlotsCount = plots.Count();
                    result.NeedsPlotOutlines = result.ExistingPlotsCount < 3; // 至少需要3个剧情大纲
                }

                // 检查主要角色
                var characterService = _serviceProvider.GetService<CharacterService>();
                if (characterService != null)
                {
                    var characters = await characterService.GetCharactersByProjectIdAsync(projectId);
                    var mainCharacters = characters.Where(c => c.Importance >= 8);
                    result.ExistingMainCharactersCount = mainCharacters.Count();
                    result.NeedsMainCharacters = result.ExistingMainCharactersCount < 3; // 至少需要3个主要角色
                }

                // 检查世界设定
                var worldSettingService = _serviceProvider.GetService<IWorldSettingService>();
                if (worldSettingService != null)
                {
                    var settings = await worldSettingService.GetByImportanceAsync(projectId, 7);
                    result.ExistingWorldSettingsCount = settings.Count();
                    result.NeedsWorldSettings = result.ExistingWorldSettingsCount < 5; // 至少需要5个重要世界设定
                }

                // 检查势力
                var factionService = _serviceProvider.GetService<FactionService>();
                if (factionService != null)
                {
                    var factions = await factionService.GetFactionsByProjectIdAsync(projectId);
                    result.ExistingFactionsCount = factions.Count();
                    result.NeedsFactions = result.ExistingFactionsCount < 3; // 至少需要3个势力
                }

                // 检查修炼体系（无体系时由 AI 自上而下设计自定义等级体系）
                var cultivationSystemService = _serviceProvider.GetService<ICultivationSystemService>();
                if (cultivationSystemService != null)
                {
                    var systems = await cultivationSystemService.GetAllAsync(projectId);
                    result.ExistingCultivationSystemsCount = systems.Count();
                    result.NeedsCultivationSystem = result.ExistingCultivationSystemsCount == 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "检查现有数据时发生错误");
            }
        }

        /// <summary>
        /// 使用AI助手生成前置条件
        /// </summary>
        private async Task GenerateWithAIAssistantAsync(Guid projectId, PrerequisiteGenerationResult result, string aiPrompt, AIAssistantService aiService)
        {
            try
            {
                var projectContextService = _serviceProvider.GetService<ProjectReadModelService>();
                var projectContext = projectContextService != null
                    ? await projectContextService.BuildAiContextDataAsync(projectId)
                    : new ProjectContextData { ProjectId = projectId };

                // 构建AI生成请求
                var prompt = $@"
请为书籍项目生成前置数据，要求：
{aiPrompt}

{projectContext.PromptSummary}

请生成以下内容：
1. 3个剧情大纲（包含主线、情感线、支线等，避免善恶二元对立）
2. 3个主要角色（性格和立场要多样化，避免脸谱化）
3. 5个世界设定（体系要完整合理）
4. 3个势力组织（立场和性质要多元化，不要简单的正邪对立）
5. 1套修炼/力量等级体系（自上而下原创设计：先定体系名与核心理念，再划分等级）

请严格按以下纯文本结构输出，不要输出 Markdown 代码块、解释或前言：
【剧情大纲】
1.
标题：
类型：
描述：
冲突：
主题：

2.
标题：
类型：
描述：
冲突：
主题：

【主要角色】
1.
姓名：
类型：
性格：
背景：
能力：
修为：
标签：

【世界设定】
1.
名称：
类型：
描述：
内容：
规则：
历史：
关联：
标签：

【势力组织】
1.
名称：
类型：
描述：
历史：
资源：
总部：
领域：
标签：

【修炼体系】
体系名称：
体系类型：
修炼方法：
1.
等级名：
描述：
突破条件：
能力特点：

2.
等级名：
描述：
突破条件：
能力特点：

（按从低到高顺序列出全部等级，共8-12级）

注意：
- 势力组织不要简单分为正道邪道，要有复杂的利益关系和立场
- 角色要有深度，避免单一的善恶标签
- 世界观要自洽，有内在逻辑
- 修炼/力量等级体系必须贴合本书题材与世界观原创设计（命名、进阶逻辑均可自定义），除非题材就是传统修仙，否则禁止照搬「练气/筑基/金丹/元婴」等常见模板；每个等级的突破条件与能力特点要递进自洽
- 若项目已有世界设定或大纲，必须延续其约束，不能推翻现有基础
- 必须遵守生成顺序：项目基本信息 -> 世界观 -> 大纲 -> 角色/势力/配套设定
";

                // 调用AI服务生成内容
                var parameters = new Dictionary<string, object>
                {
                    ["prompt"] = prompt,
                    ["projectId"] = projectId.ToString(),
                    ["projectContext"] = projectContext.PromptSummary
                };
                var aiResponse = await aiService.GeneratePlotAsync(parameters);

                // 解析AI响应并创建数据
                await ParseAndCreateAIGeneratedDataAsync(projectId, result, aiResponse.Data?.ToString() ?? "");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI生成前置条件失败");
                // 回退到默认生成
                await GenerateDefaultDataAsync(projectId, result);
            }
        }

        /// <summary>
        /// 解析AI生成的数据并创建
        /// </summary>
        private async Task ParseAndCreateAIGeneratedDataAsync(Guid projectId, PrerequisiteGenerationResult result, string aiResponse)
        {
            var normalized = AiAutoFillFormatter.Normalize(aiResponse);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                await GenerateDefaultDataAsync(projectId, result);
                return;
            }

            var plotService = _serviceProvider.GetService<PlotService>();
            var characterService = _serviceProvider.GetService<CharacterService>();
            var worldSettingService = _serviceProvider.GetService<IWorldSettingService>();
            var factionService = _serviceProvider.GetService<FactionService>();
            var cultivationSystemService = _serviceProvider.GetService<ICultivationSystemService>();

            // 修炼体系最先生成（角色的修为等级将从体系中取值）
            if (result.NeedsCultivationSystem && cultivationSystemService != null)
            {
                var cultivationSection = AiAutoFillFormatter.ExtractSection(normalized, "修炼体系", "力量体系", "等级体系");
                if (!string.IsNullOrWhiteSpace(cultivationSection))
                {
                    await ParseCultivationSystemSectionAsync(projectId, cultivationSection, result, cultivationSystemService);
                }
            }

            if (result.NeedsPlotOutlines && plotService != null)
            {
                var plotSection = AiAutoFillFormatter.ExtractSection(normalized, "剧情大纲", "大纲");
                var plotBlocks = SplitStructuredItems(plotSection);
                foreach (var block in plotBlocks.Take(3))
                {
                    var title = LimitLength(FirstNonEmpty(
                        AiAutoFillFormatter.ExtractSingleLineValue(block, "标题", "名称"),
                        AiAutoFillFormatter.ExtractFirstMeaningfulLine(block),
                        $"AI剧情大纲{result.GeneratedPlotsCount + 1}"), 200);

                    if (string.IsNullOrWhiteSpace(title))
                    {
                        continue;
                    }

                    var plot = new Plot
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = projectId,
                        Title = title,
                        Type = LimitLength(FirstNonEmpty(
                            AiAutoFillFormatter.ExtractSingleLineValue(block, "类型"),
                            "主线"), 50),
                        Description = NullIfEmpty(AiAutoFillFormatter.ExtractSummary(block, "描述", "简介")),
                        Outline = NullIfEmpty(AiAutoFillFormatter.ExtractSection(block, "描述", "简介")),
                        ConflictElements = NullIfEmpty(AiAutoFillFormatter.ExtractSection(block, "冲突", "核心冲突")),
                        ThemeElements = NullIfEmpty(AiAutoFillFormatter.ExtractSection(block, "主题", "核心主题")),
                        Status = "规划中",
                        Priority = "中",
                        Importance = 8,
                        Tags = LimitLength(AiAutoFillFormatter.ExtractSingleLineValue(block, "标签"), 500),
                        Notes = NullIfEmpty(AiAutoFillFormatter.ExtractSection(block, "备注"))
                    };

                    await plotService.CreatePlotAsync(plot);
                    result.GeneratedPlotsCount++;
                    result.GeneratedItems.Add($"剧情大纲: {plot.Title}");
                }
            }

            if (result.NeedsMainCharacters && characterService != null)
            {
                var characterSection = AiAutoFillFormatter.ExtractSection(normalized, "主要角色", "角色");
                var characterBlocks = SplitStructuredItems(characterSection);
                var projectLevelNames = await GetProjectCultivationLevelNamesAsync(projectId);
                var characterIndex = 0;
                foreach (var block in characterBlocks.Take(3))
                {
                    var name = LimitLength(FirstNonEmpty(
                        AiAutoFillFormatter.ExtractSingleLineValue(block, "姓名", "名称"),
                        AiAutoFillFormatter.ExtractFirstMeaningfulLine(block),
                        $"AI角色{result.GeneratedCharactersCount + 1}"), 100);

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    var character = new Character
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = projectId,
                        Name = name,
                        Type = LimitLength(FirstNonEmpty(
                            AiAutoFillFormatter.ExtractSingleLineValue(block, "类型", "定位", "角色类型"),
                            "主要角色"), 50),
                        Personality = NullIfEmpty(AiAutoFillFormatter.ExtractSection(block, "性格", "性格特点")),
                        Background = NullIfEmpty(AiAutoFillFormatter.ExtractSection(block, "背景", "背景故事")),
                        Abilities = NullIfEmpty(AiAutoFillFormatter.ExtractSection(block, "能力", "能力技能", "技能")),
                        // 修为等级优先取 AI 输出，未给出时按角色次序从项目自定义体系的低阶取值
                        CultivationLevel = LimitLength(FirstNonEmpty(
                            AiAutoFillFormatter.ExtractSingleLineValue(block, "修为", "境界", "修为等级"),
                            projectLevelNames.Count > 0 ? projectLevelNames[Math.Min(characterIndex, projectLevelNames.Count - 1)] : null), 50),
                        Notes = NullIfEmpty(AiAutoFillFormatter.ExtractSection(block, "备注", "立场")),
                        Tags = LimitLength(AiAutoFillFormatter.ExtractSingleLineValue(block, "标签"), 500),
                        Status = "Active",
                        Importance = 8
                    };

                    var created = await characterService.CreateCharacterAsync(character);
                    characterIndex++;
                    if (created.Id == character.Id)
                    {
                        result.GeneratedCharactersCount++;
                        result.GeneratedItems.Add($"主要角色: {character.Name}");
                    }
                    else
                    {
                        result.GeneratedItems.Add($"主要角色: {character.Name}（项目内已存在同名且描述一致的角色，跳过重复创建）");
                    }
                }
            }

            if (result.NeedsWorldSettings && worldSettingService != null)
            {
                var worldSection = AiAutoFillFormatter.ExtractSection(normalized, "世界设定", "世界观", "设定");
                var worldBlocks = SplitStructuredItems(worldSection);
                foreach (var block in worldBlocks.Take(5))
                {
                    var name = LimitLength(FirstNonEmpty(
                        AiAutoFillFormatter.ExtractSingleLineValue(block, "名称", "标题"),
                        AiAutoFillFormatter.ExtractFirstMeaningfulLine(block),
                        $"AI世界设定{result.GeneratedWorldSettingsCount + 1}"), 200);

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    var createDto = new CreateWorldSettingDto
                    {
                        Name = name,
                        ProjectId = projectId,
                        Type = LimitLength(FirstNonEmpty(
                            AiAutoFillFormatter.ExtractSingleLineValue(block, "类型", "分类"),
                            "世界观"), 50),
                        Category = LimitLength(AiAutoFillFormatter.ExtractSingleLineValue(block, "分类"), 50),
                        Description = NullIfEmpty(AiAutoFillFormatter.ExtractSummary(block, "描述", "简介")),
                        Content = NullIfEmpty(AiAutoFillFormatter.ExtractSection(block, "内容", "详细内容")),
                        Rules = NullIfEmpty(AiAutoFillFormatter.ExtractSection(block, "规则")),
                        History = NullIfEmpty(AiAutoFillFormatter.ExtractSection(block, "历史")),
                        RelatedSettings = NullIfEmpty(AiAutoFillFormatter.ExtractSection(block, "关联", "相关设定")),
                        Importance = 8,
                        Tags = LimitLength(AiAutoFillFormatter.ExtractSingleLineValue(block, "标签"), 500),
                        Notes = NullIfEmpty(AiAutoFillFormatter.ExtractSection(block, "备注")),
                        Status = "Active",
                        IsPublic = true
                    };

                    await worldSettingService.CreateAsync(createDto);
                    result.GeneratedWorldSettingsCount++;
                    result.GeneratedItems.Add($"世界设定: {createDto.Name}");
                }
            }

            if (result.NeedsFactions && factionService != null)
            {
                var factionSection = AiAutoFillFormatter.ExtractSection(normalized, "势力组织", "势力");
                var factionBlocks = SplitStructuredItems(factionSection);
                foreach (var block in factionBlocks.Take(3))
                {
                    var name = LimitLength(FirstNonEmpty(
                        AiAutoFillFormatter.ExtractSingleLineValue(block, "名称", "标题"),
                        AiAutoFillFormatter.ExtractFirstMeaningfulLine(block),
                        $"AI势力{result.GeneratedFactionsCount + 1}"), 100);

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    var faction = new Faction
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = projectId,
                        Name = name,
                        Type = LimitLength(FirstNonEmpty(
                            AiAutoFillFormatter.ExtractSingleLineValue(block, "类型"),
                            "复合势力"), 50),
                        Description = NullIfEmpty(AiAutoFillFormatter.ExtractSummary(block, "描述", "简介")),
                        History = NullIfEmpty(AiAutoFillFormatter.ExtractSection(block, "历史")),
                        Resources = NullIfEmpty(AiAutoFillFormatter.ExtractSection(block, "资源")),
                        Headquarters = LimitLength(AiAutoFillFormatter.ExtractSingleLineValue(block, "总部"), 200),
                        Territory = NullIfEmpty(AiAutoFillFormatter.ExtractSection(block, "领域", "控制区域")),
                        Tags = LimitLength(AiAutoFillFormatter.ExtractSingleLineValue(block, "标签"), 500),
                        Notes = NullIfEmpty(AiAutoFillFormatter.ExtractSection(block, "备注")),
                        Status = "Active",
                        Importance = 8
                    };

                    await factionService.CreateFactionAsync(faction);
                    result.GeneratedFactionsCount++;
                    result.GeneratedItems.Add($"势力组织: {faction.Name}");
                }
            }

            await GenerateFallbackDataForUnparsedSectionsAsync(projectId, result);
        }

        private async Task GenerateFallbackDataForUnparsedSectionsAsync(Guid projectId, PrerequisiteGenerationResult result)
        {
            if (result.NeedsPlotOutlines && result.GeneratedPlotsCount == 0)
            {
                await GeneratePlotOutlinesAsync(projectId, result);
            }

            if (result.NeedsMainCharacters && result.GeneratedCharactersCount == 0)
            {
                await GenerateMainCharactersAsync(projectId, result);
            }

            if (result.NeedsWorldSettings && result.GeneratedWorldSettingsCount == 0)
            {
                await GenerateWorldSettingsAsync(projectId, result);
            }

            if (result.NeedsFactions && result.GeneratedFactionsCount == 0)
            {
                await GenerateFactionsAsync(projectId, result);
            }

            if (result.NeedsCultivationSystem && result.GeneratedCultivationSystemsCount == 0)
            {
                await EnsureCultivationSystemAsync(projectId, result);
            }
        }

        private static List<string> SplitStructuredItems(string? section)
        {
            var normalized = AiAutoFillFormatter.Normalize(section);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return new List<string>();
            }

            var items = new List<string>();
            List<string>? currentLines = null;
            var lines = normalized.Replace("\r\n", "\n").Split('\n');

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (Regex.IsMatch(line, @"^\d+[\.\)、]?\s*$"))
                {
                    FlushCurrentItem(items, ref currentLines);
                    currentLines = new List<string>();
                    continue;
                }

                var numberedContentMatch = Regex.Match(line, @"^\d+[\.\)、]\s*(.+)$");
                if (numberedContentMatch.Success)
                {
                    FlushCurrentItem(items, ref currentLines);
                    currentLines = new List<string> { numberedContentMatch.Groups[1].Value.Trim() };
                    continue;
                }

                currentLines ??= new List<string>();
                currentLines.Add(line);
            }

            FlushCurrentItem(items, ref currentLines);

            if (items.Count == 0)
            {
                items = normalized
                    .Split(new[] { $"{Environment.NewLine}{Environment.NewLine}", "\n\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(block => block.Trim())
                    .Where(block => !string.IsNullOrWhiteSpace(block))
                    .ToList();
            }

            return items;
        }

        /// <summary>
        /// 在未 Normalize 的原始文本上按「N.」编号行拆分等级块
        /// （SplitStructuredItems 内部会 Normalize 抹掉行首编号，不能用于编号切分场景）
        /// </summary>
        internal static List<string> SplitNumberedLevelBlocks(string? section)
        {
            var blocks = new List<string>();
            if (string.IsNullOrWhiteSpace(section))
            {
                return blocks;
            }

            List<string>? current = null;
            foreach (var rawLine in section.Replace("\r\n", "\n").Split('\n'))
            {
                var line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (Regex.IsMatch(line, @"^\d+[\.\)、]?\s*$"))
                {
                    if (current is { Count: > 0 })
                    {
                        blocks.Add(string.Join(Environment.NewLine, current));
                    }

                    current = new List<string>();
                    continue;
                }

                var numberedContent = Regex.Match(line, @"^\d+[\.\)、]\s*(.+)$");
                if (numberedContent.Success)
                {
                    if (current is { Count: > 0 })
                    {
                        blocks.Add(string.Join(Environment.NewLine, current));
                    }

                    current = new List<string> { numberedContent.Groups[1].Value.Trim() };
                    continue;
                }

                current ??= new List<string>();
                current.Add(line);
            }

            if (current is { Count: > 0 })
            {
                blocks.Add(string.Join(Environment.NewLine, current));
            }

            return blocks;
        }

        private static void FlushCurrentItem(ICollection<string> items, ref List<string>? currentLines)
        {
            if (currentLines == null || currentLines.Count == 0)
            {
                currentLines = null;
                return;
            }

            var block = string.Join(Environment.NewLine, currentLines).Trim();
            if (!string.IsNullOrWhiteSpace(block))
            {
                items.Add(block);
            }

            currentLines = null;
        }

        private static string FirstNonEmpty(params string?[] values)
        {
            return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;
        }

        private static string? NullIfEmpty(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string? LimitLength(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var trimmed = value.Trim();
            return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
        }

        /// <summary>
        /// 生成修炼体系：优先 AI 自上而下设计自定义等级体系（命名/进阶逻辑可完全自定义），失败回退通用模板
        /// </summary>
        public async Task EnsureCultivationSystemAsync(Guid projectId, PrerequisiteGenerationResult result)
        {
            try
            {
                var cultivationSystemService = _serviceProvider.GetService<ICultivationSystemService>();
                if (cultivationSystemService == null) return;

                // 已有体系则不再生成（顺带回填存量角色的空修为）
                var existing = await cultivationSystemService.GetAllAsync(projectId);
                if (existing.Any())
                {
                    result.NeedsCultivationSystem = false;
                    await BackfillEmptyCultivationLevelsAsync(projectId, result);
                    return;
                }

                // 优先直连 RWKV 设计自定义体系（不走 Agent 剧情任务链：GeneratePlot 会忽略 prompt 参数且做剧情提取，破坏【修炼体系】结构）
                var rwkv = _serviceProvider.GetService<IRwkvLightningService>();
                if (rwkv != null && rwkv.IsAvailable)
                {
                    try
                    {
                        var systemPrompt = "你是资深网文世界观架构师，擅长自上而下原创设计力量/修炼等级体系，严格按要求的纯文本结构输出。";
                        var userPrompt = @"请为书籍项目设计一套修炼/力量等级体系，要求自上而下原创设计：
1. 先确定体系名称、类型与核心修炼方法（须贴合项目题材与世界观，命名可完全自定义）
2. 再从低到高划分 8-12 个等级，每级给出描述、突破条件、能力特点，进阶逻辑要递进自洽
3. 除传统修仙题材外，禁止照搬「练气/筑基/金丹/元婴」等常见模板

请严格按以下纯文本结构输出，不要输出 Markdown 代码块、解释或前言：
【修炼体系】
体系名称：
体系类型：
修炼方法：
1.
等级名：
描述：
突破条件：
能力特点：

2.
等级名：
描述：
突破条件：
能力特点：

（按从低到高顺序列出全部等级）";
                        var rwkvPrompt = "User: " + systemPrompt + "\n" + userPrompt + "\n\nAssistant: <think></think\n";
                        var response = await rwkv.CompleteAsync(rwkvPrompt, 2048);
                        if (response.Success && !string.IsNullOrWhiteSpace(response.Text))
                        {
                            // 传原始输出解析：AiAutoFillFormatter.Normalize 会剥掉「N.」行首编号，
                            // 必须在原始文本上按编号行切分，再对块内做字段提取
                            if (await ParseCultivationSystemSectionAsync(projectId, response.Text, result, cultivationSystemService))
                            {
                                await BackfillEmptyCultivationLevelsAsync(projectId, result);
                                return;
                            }
                            _logger.LogWarning("RWKV 输出解析修炼体系失败，回退默认模板");
                        }
                        else
                        {
                            _logger.LogWarning("RWKV 生成修炼体系失败: {Error}，回退默认模板", response.Error);
                        }
                    }
                    catch (Exception aiEx)
                    {
                        _logger.LogWarning(aiEx, "RWKV 生成修炼体系异常，回退默认模板");
                    }
                }

                // AI 不可用或解析失败：回退通用九阶模板（中性命名，不预设修仙体系）
                var genericLevels = new List<string> { "初窥门径", "登堂入室", "驾轻就熟", "融会贯通", "炉火纯青", "出神入化", "登峰造极", "返璞归真", "超凡入圣" };
                var fallback = await CreateCultivationSystemAsync(
                    projectId, "通用进阶体系", "通用",
                    "以技艺与心境共同打磨的进阶之路，每一阶都是从量变到质变的跃迁。",
                    "以自身领悟淬炼本源之力，境界提升伴随神魂与体魄的双重蜕变。",
                    genericLevels.Select(n => (n, (string?)$"进阶至{n}的关键在于打牢前一级根基，完成一次本源蜕变。", (string?)null, (string?)null)),
                    cultivationSystemService);
                if (fallback != null)
                {
                    result.GeneratedCultivationSystemsCount++;
                    result.GeneratedItems.Add($"修炼体系: {fallback.Name}（默认模板，可在修炼体系管理中调整）");
                    await BackfillEmptyCultivationLevelsAsync(projectId, result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成修炼体系失败");
            }
            finally
            {
                result.NeedsCultivationSystem = false;
            }
        }

        /// <summary>
        /// 修炼体系就绪后，回填项目内修为为空的存量角色（已有修为绝不覆盖）。
        /// 回填失败不阻断体系生成主流程。
        /// </summary>
        private async Task BackfillEmptyCultivationLevelsAsync(Guid projectId, PrerequisiteGenerationResult result)
        {
            try
            {
                var backfillService = _serviceProvider.GetService<CultivationLevelBackfillService>();
                if (backfillService == null) return;

                var assignments = await backfillService.BackfillAsync(projectId);
                foreach (var a in assignments)
                {
                    result.GeneratedItems.Add($"修为回填: {a.CharacterName} = {a.LevelName}（{a.Rule}）");
                }
                if (assignments.Count > 0)
                {
                    _logger.LogInformation("项目 {ProjectId} 回填 {Count} 名角色的空修为等级", projectId, assignments.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "修为等级回填失败");
            }
        }

        /// <summary>
        /// 解析 AI 输出的【修炼体系】板块并创建体系与等级
        /// </summary>
        /// <returns>是否成功创建</returns>
        internal async Task<bool> ParseCultivationSystemSectionAsync(
            Guid projectId, string section, PrerequisiteGenerationResult result, ICultivationSystemService service)
        {
            try
            {
                // 关键：在原始文本上按「N.」编号行切分（AiAutoFillFormatter.Normalize 会剥掉行首编号，
                // 先 Normalize 再切分会丢失编号结构导致等级块无法拆分）
                var firstNumbered = Regex.Match(section, @"(?m)^\s*\d+[\.\)、]?\s*$");
                var header = firstNumbered.Success ? section[..firstNumbered.Index] : section;
                var levelsPart = firstNumbered.Success ? section[firstNumbered.Index..] : section;

                // ExtractSection 逐行匹配字段名，可跳过「【修炼体系】」标题行取到真实字段值；
                // ExtractSingleLineValue 兜底必须校验字段前缀，否则缺失字段时会拿整段首行当值
                var fallbackName = AiAutoFillFormatter.ExtractSingleLineValue(header, "体系名称", "名称");
                var systemName = LimitLength(FirstNonEmpty(
                    AiAutoFillFormatter.ExtractSection(header, "体系名称", "体系名", "名称"),
                    AiAutoFillFormatter.HasFieldPrefix(fallbackName, "体系名称", "名称") ? fallbackName : null), 100);
                var systemType = LimitLength(FirstNonEmpty(
                    AiAutoFillFormatter.ExtractSection(header, "体系类型", "类型"), "通用"), 50);
                var method = NullIfEmpty(AiAutoFillFormatter.ExtractSection(header, "修炼方法", "修炼方式", "核心理念"));

                var levels = new List<(string Name, string? Description, string? Breakthrough, string? Abilities)>();
                foreach (var block in SplitNumberedLevelBlocks(levelsPart))
                {
                    // ExtractSection 逐行匹配「等级名：」字段行，可跳过块内混入的「【修炼体系】」等标题行
                    var levelName = LimitLength(FirstNonEmpty(
                        AiAutoFillFormatter.ExtractSection(block, "等级名", "等级", "境界", "名称")), 100);
                    if (string.IsNullOrWhiteSpace(levelName)) continue;
                    levels.Add((
                        levelName,
                        NullIfEmpty(AiAutoFillFormatter.ExtractSection(block, "描述", "简介")),
                        NullIfEmpty(AiAutoFillFormatter.ExtractSection(block, "突破条件", "突破")),
                        NullIfEmpty(AiAutoFillFormatter.ExtractSection(block, "能力特点", "能力"))));
                }

                if (string.IsNullOrWhiteSpace(systemName) || levels.Count < 2)
                {
                    _logger.LogWarning("修炼体系解析失败：体系名称缺失或等级数量不足");
                    return false;
                }

                var created = await CreateCultivationSystemAsync(projectId, systemName, systemType, null, method, levels, service);
                if (created == null) return false;

                result.GeneratedCultivationSystemsCount++;
                result.GeneratedItems.Add($"修炼体系: {created.Name}（{levels.Count} 级）");
                result.NeedsCultivationSystem = false;
                _logger.LogInformation("为项目 {ProjectId} 创建修炼体系 {Name}，共 {Count} 级", projectId, created.Name, levels.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析修炼体系失败");
                return false;
            }
        }

        /// <summary>
        /// 创建修炼体系及其等级
        /// </summary>
        private async Task<CultivationSystemDto?> CreateCultivationSystemAsync(
            Guid projectId, string name, string type, string? description, string? method,
            IEnumerable<(string Name, string? Description, string? Breakthrough, string? Abilities)> levels,
            ICultivationSystemService service)
        {
            var levelList = levels.Where(l => !string.IsNullOrWhiteSpace(l.Name)).ToList();
            var created = await service.CreateAsync(new CreateCultivationSystemDto
            {
                Name = LimitLength(name, 100),
                Type = LimitLength(type, 50),
                Description = NullIfEmpty(description),
                CultivationMethod = NullIfEmpty(method),
                RealmDivision = string.Join("→", levelList.Select(l => l.Name)),
                ProjectId = projectId,
                Importance = 9
            });

            var order = 1;
            foreach (var (levelName, levelDesc, breakthrough, abilities) in levelList)
            {
                await service.CreateLevelAsync(new CreateCultivationLevelDto
                {
                    CultivationSystemId = created.Id,
                    Name = LimitLength(levelName, 100),
                    OrderIndex = order,
                    Description = levelDesc,
                    BreakthroughCondition = breakthrough,
                    Abilities = abilities
                });
                order++;
            }

            return created;
        }

        /// <summary>
        /// 获取项目第一个修炼体系的等级名列表（由低到高）
        /// </summary>
        public async Task<List<string>> GetProjectCultivationLevelNamesAsync(Guid projectId)
        {
            try
            {
                var service = _serviceProvider.GetService<ICultivationSystemService>();
                if (service == null) return new List<string>();

                var systems = await service.GetAllAsync(projectId);
                var first = systems.FirstOrDefault();
                if (first == null) return new List<string>();

                var withLevels = await service.GetWithLevelsAsync(first.Id);
                var levels = withLevels?.Levels ?? first.Levels;
                return levels.OrderBy(l => l.OrderIndex).Select(l => l.Name).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取项目修炼等级失败");
                return new List<string>();
            }
        }

        /// <summary>
        /// 构建修炼体系摘要文本（用于世界设定默认项；无体系时回退通用文本）
        /// </summary>
        private async Task<string> BuildCultivationSummaryAsync(Guid projectId)
        {
            var levelNames = await GetProjectCultivationLevelNamesAsync(projectId);
            if (levelNames.Count > 0)
            {
                return $"修炼等级：{string.Join("→", levelNames)}。等级由低到高递进，每次突破均需满足对应条件并获得质的飞跃。";
            }

            return "修炼等级：练气→筑基→金丹→元婴→化神→炼虚→合体→大乘→渡劫→仙人。每个大境界分为初期、中期、后期、巅峰四个小境界。";
        }

        /// <summary>
        /// 生成默认数据
        /// </summary>
        private async Task GenerateDefaultDataAsync(Guid projectId, PrerequisiteGenerationResult result)
        {
            var options = new PrerequisiteGenerationOptions();

            // 修炼体系最先生成，供角色修为等级联动
            if (result.NeedsCultivationSystem)
            {
                await EnsureCultivationSystemAsync(projectId, result);
            }

            if (result.NeedsPlotOutlines)
            {
                await GeneratePlotOutlinesAsync(projectId, result);
            }

            if (result.NeedsMainCharacters)
            {
                await GenerateMainCharactersAsync(projectId, result);
            }

            if (result.NeedsWorldSettings)
            {
                await GenerateWorldSettingsAsync(projectId, result);
            }

            if (result.NeedsFactions)
            {
                await GenerateFactionsAsync(projectId, result);
            }
        }

        /// <summary>
        /// 生成剧情大纲
        /// </summary>
        private async Task GeneratePlotOutlinesAsync(Guid projectId, PrerequisiteGenerationResult result)
        {
            try
            {
                var plotService = _serviceProvider.GetService<PlotService>();
                if (plotService == null) return;

                var defaultPlots = new List<Plot>
                {
                    new Plot
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = projectId,
                        Title = "主线剧情：成长之路",
                        Description = "主角从平凡开始，通过不断努力和机遇，逐步成长为强者的主线故事",
                        Type = "主线",
                        Status = "计划中",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Plot
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = projectId,
                        Title = "情感线：人际关系",
                        Description = "主角与重要人物之间的情感发展，包括友情、爱情、师徒情等",
                        Type = "情感线",
                        Status = "计划中",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Plot
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = projectId,
                        Title = "支线剧情：势力纷争",
                        Description = "各方势力之间的复杂关系和利益纠葛，展现世界的多元化",
                        Type = "支线",
                        Status = "计划中",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };

                foreach (var plot in defaultPlots)
                {
                    await plotService.CreatePlotAsync(plot);
                    result.GeneratedItems.Add($"剧情大纲: {plot.Title}");
                }

                result.GeneratedPlotsCount = defaultPlots.Count;
                _logger.LogInformation("为项目 {ProjectId} 生成了 {Count} 个剧情大纲", projectId, defaultPlots.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成剧情大纲失败");
            }
        }

        /// <summary>
        /// 生成主要角色
        /// </summary>
        private async Task GenerateMainCharactersAsync(Guid projectId, PrerequisiteGenerationResult result)
        {
            try
            {
                var characterService = _serviceProvider.GetService<CharacterService>();
                if (characterService == null) return;

                var defaultCharacters = new List<Character>
                {
                    new Character
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = projectId,
                        Name = "林轩",
                        Type = "主角",
                        Gender = "男",
                        Age = 18,
                        Appearance = "相貌英俊，身材修长，眼神坚毅",
                        Personality = "坚韧不拔，正义感强，重情重义",
                        Background = "出身平凡，因机缘巧合获得修仙传承",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Character
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = projectId,
                        Name = "苏雨薇",
                        Type = "女主角",
                        Gender = "女",
                        Age = 17,
                        Appearance = "倾国倾城，气质出尘，如仙子下凡",
                        Personality = "聪慧善良，外柔内刚，冰雪聪明",
                        Background = "名门世家出身，天赋卓绝的修仙天才",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Character
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = projectId,
                        Name = "玄天老祖",
                        Type = "师父",
                        Gender = "男",
                        Age = 800,
                        Appearance = "仙风道骨，白发飘逸，深不可测",
                        Personality = "睿智深沉，慈祥严厉，洞察世事",
                        Background = "隐世高人，曾经的修仙界传奇人物",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };

                // 修为等级从项目自定义修炼体系中按角色定位取值（主角低阶、师父高阶）
                var levelNames = await GetProjectCultivationLevelNamesAsync(projectId);
                if (levelNames.Count > 0)
                {
                    defaultCharacters[0].CultivationLevel = levelNames[0];
                    defaultCharacters[1].CultivationLevel = levelNames[Math.Min(1, levelNames.Count - 1)];
                    defaultCharacters[2].CultivationLevel = levelNames[Math.Min(levelNames.Count - 2, levelNames.Count - 1)];
                }

                var createdCount = 0;
                foreach (var character in defaultCharacters)
                {
                    var created = await characterService.CreateCharacterAsync(character);
                    if (created.Id != character.Id)
                    {
                        result.GeneratedItems.Add($"主要角色: {character.Name} ({character.Type})（项目内已存在同名且描述一致的角色，跳过重复创建）");
                        continue;
                    }
                    createdCount++;
                    result.GeneratedItems.Add($"主要角色: {character.Name} ({character.Type})");
                }

                result.GeneratedCharactersCount = createdCount;
                _logger.LogInformation("为项目 {ProjectId} 生成了 {Count} 个主要角色", projectId, createdCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成主要角色失败");
            }
        }

        /// <summary>
        /// 生成世界设定
        /// </summary>
        private async Task GenerateWorldSettingsAsync(Guid projectId, PrerequisiteGenerationResult result)
        {
            try
            {
                var worldSettingService = _serviceProvider.GetService<IWorldSettingService>();
                if (worldSettingService == null) return;

                var defaultSettings = new List<WorldSetting>
                {
                    new WorldSetting
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = projectId,
                        Name = "修炼体系",
                        Type = "体系设定",
                        Content = await BuildCultivationSummaryAsync(projectId),
                        Importance = 10,
                        Order = 1,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new WorldSetting
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = projectId,
                        Name = "世界地理",
                        Type = "地理设定",
                        Content = "九天仙域分为四大洲：东胜神洲、西牛贺洲、南赡部洲、北俱芦洲。每洲都有独特的地理环境和修炼资源。",
                        Importance = 9,
                        Order = 2,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new WorldSetting
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = projectId,
                        Name = "灵气体系",
                        Type = "能量设定",
                        Content = "天地灵气分为五行灵气（金木水火土）和特殊灵气（雷、冰、风等）。修炼者根据体质吸收不同属性的灵气。",
                        Importance = 8,
                        Order = 3,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new WorldSetting
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = projectId,
                        Name = "法宝等级",
                        Type = "物品设定",
                        Content = "法宝等级：凡器→灵器→宝器→法器→仙器→神器。每个等级又分为下品、中品、上品、极品四个品质。",
                        Importance = 7,
                        Order = 4,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new WorldSetting
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = projectId,
                        Name = "时间设定",
                        Type = "时间设定",
                        Content = "修仙界时间流速与凡间相同，但修炼者寿命大幅延长。练气期寿命200年，筑基期500年，金丹期1000年，以此类推。",
                        Importance = 7,
                        Order = 5,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };

                foreach (var setting in defaultSettings)
                {
                    var createDto = new CreateWorldSettingDto
                    {
                        Name = setting.Name,
                        Type = setting.Type,
                        Category = setting.Category,
                        Description = setting.Description,
                        Content = setting.Content,
                        Rules = setting.Rules,
                        History = setting.History,
                        RelatedSettings = setting.RelatedSettings,
                        Importance = setting.Importance,
                        ProjectId = projectId,
                        ParentId = setting.ParentId,
                        ImagePath = setting.ImagePath,
                        Tags = setting.Tags,
                        Notes = setting.Notes,
                        Status = setting.Status,
                        OrderIndex = setting.Order,
                        IsPublic = setting.IsPublic,
                        Version = setting.Version
                    };
                    await worldSettingService.CreateAsync(createDto);
                    result.GeneratedItems.Add($"世界设定: {setting.Name}");
                }

                result.GeneratedWorldSettingsCount = defaultSettings.Count;
                _logger.LogInformation("为项目 {ProjectId} 生成了 {Count} 个世界设定", projectId, defaultSettings.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成世界设定失败");
            }
        }

        /// <summary>
        /// 生成势力
        /// </summary>
        private async Task GenerateFactionsAsync(Guid projectId, PrerequisiteGenerationResult result)
        {
            try
            {
                var factionService = _serviceProvider.GetService<FactionService>();
                if (factionService == null) return;

                var defaultFactions = new List<Faction>
                {
                    new Faction
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = projectId,
                        Name = "玄天宗",
                        Type = "修仙宗门",
                        PowerLevel = 95,
                        Description = "修仙界历史悠久的大型宗门，拥有强大实力和深厚底蕴，门规严明，注重传承",
                        Territory = "玄天山脉",
                        MemberCount = 50000,
                        Status = "Active",
                        PowerRating = 95,
                        Influence = 90,
                        Importance = 95,
                        Tags = "宗门,修仙,传承,强大",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Faction
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = projectId,
                        Name = "血月宗",
                        Type = "修仙宗门",
                        PowerLevel = 85,
                        Description = "以血系功法闻名的修仙宗门，修炼方式独特，在修仙界有着特殊地位",
                        Territory = "血月峡谷",
                        MemberCount = 30000,
                        Status = "Active",
                        PowerRating = 85,
                        Influence = 70,
                        Importance = 80,
                        Tags = "宗门,血系功法,独特修炼,神秘",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Faction
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = projectId,
                        Name = "万宝商会",
                        Type = "商业组织",
                        PowerLevel = 70,
                        Description = "修仙界最大的商业组织，掌控着大部分修炼资源的流通，保持中立立场",
                        Territory = "各大商城",
                        MemberCount = 20000,
                        Status = "Active",
                        PowerRating = 70,
                        Influence = 80,
                        Importance = 65,
                        Tags = "商业,财富,贸易,中立",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };

                foreach (var faction in defaultFactions)
                {
                    await factionService.CreateFactionAsync(faction);
                    result.GeneratedItems.Add($"势力: {faction.Name} ({faction.Type})");
                }

                result.GeneratedFactionsCount = defaultFactions.Count;
                _logger.LogInformation("为项目 {ProjectId} 生成了 {Count} 个势力", projectId, defaultFactions.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成势力失败");
            }
        }

        #region 默认模板方法

        /// <summary>
        /// 获取默认剧情大纲模板
        /// </summary>
        private List<PlotTemplate> GetDefaultPlotTemplates(PrerequisiteGenerationOptions options)
        {
            return new List<PlotTemplate>
            {
                new PlotTemplate
                {
                    Title = "主线剧情：成长之路",
                    Type = "主线",
                    SummaryTemplate = "主角从平凡开始，通过不断努力和机遇，逐步成长为强者的主线故事",
                    ImportanceLevel = 10,
                    PlotPointTemplates = new List<string> { "起点", "第一次转折", "成长历练", "重大挫折", "突破成就" }
                },
                new PlotTemplate
                {
                    Title = "情感线：人际关系",
                    Type = "情感线",
                    SummaryTemplate = "主角与重要人物之间的情感发展，包括友情、爱情、师徒情等",
                    ImportanceLevel = 8,
                    PlotPointTemplates = new List<string> { "初遇", "了解", "深入交往", "考验", "情感升华" }
                },
                new PlotTemplate
                {
                    Title = "支线剧情：势力纷争",
                    Type = "支线",
                    SummaryTemplate = "各方势力之间的复杂关系和利益纠葛，展现世界的多元化",
                    ImportanceLevel = 7,
                    PlotPointTemplates = new List<string> { "势力介绍", "利益冲突", "立场分化", "博弈较量", "新平衡" }
                }
            };
        }

        /// <summary>
        /// 获取默认角色模板
        /// </summary>
        private List<CharacterTemplate> GetDefaultCharacterTemplates(PrerequisiteGenerationOptions options)
        {
            return new List<CharacterTemplate>
            {
                new CharacterTemplate
                {
                    NameTemplate = "主角",
                    Type = "主角",
                    Gender = "待定",
                    AppearanceTemplate = "外貌特征待完善",
                    PersonalityTemplate = "性格复杂多面，有优点也有缺点，会随剧情发展而成长",
                    BackgroundTemplate = "出身背景待完善，建议设置合理的成长环境",
                    ImportanceLevel = 10
                },
                new CharacterTemplate
                {
                    NameTemplate = "重要配角",
                    Type = "配角",
                    Gender = "待定",
                    AppearanceTemplate = "外貌特征待完善",
                    PersonalityTemplate = "独特的个性和立场，与主角形成有趣的互动关系",
                    BackgroundTemplate = "有自己的故事和动机，不是单纯的功能性角色",
                    ImportanceLevel = 8
                },
                new CharacterTemplate
                {
                    NameTemplate = "引路人",
                    Type = "导师/长辈",
                    Gender = "待定",
                    AppearanceTemplate = "外貌特征待完善",
                    PersonalityTemplate = "智慧与经验并存，但也有自己的局限性和过往",
                    BackgroundTemplate = "丰富的人生阅历，对主角的成长有重要影响",
                    ImportanceLevel = 8
                }
            };
        }

        /// <summary>
        /// 获取默认世界设定模板
        /// </summary>
        private List<WorldSettingTemplate> GetDefaultWorldSettingTemplates(PrerequisiteGenerationOptions options)
        {
            var genre = options.NovelGenre.ToLower();

            if (genre.Contains("修仙") || genre.Contains("仙侠"))
            {
                return GetXianxiaWorldSettingTemplates();
            }
            else if (genre.Contains("都市") || genre.Contains("现代"))
            {
                return GetUrbanWorldSettingTemplates();
            }
            else if (genre.Contains("玄幻") || genre.Contains("奇幻"))
            {
                return GetFantasyWorldSettingTemplates();
            }
            else
            {
                return GetGenericWorldSettingTemplates();
            }
        }

        /// <summary>
        /// 获取修仙类世界设定模板
        /// </summary>
        private List<WorldSettingTemplate> GetXianxiaWorldSettingTemplates()
        {
            return new List<WorldSettingTemplate>
            {
                new WorldSettingTemplate
                {
                    NameTemplate = "修炼体系",
                    Type = "体系设定",
                    ContentTemplate = "修炼等级划分和晋升条件，建议设置合理的成长曲线",
                    Importance = 10
                },
                new WorldSettingTemplate
                {
                    NameTemplate = "世界地理",
                    Type = "地理设定",
                    ContentTemplate = "世界的地理结构和区域划分，各地的特色和资源分布",
                    Importance = 9
                },
                new WorldSettingTemplate
                {
                    NameTemplate = "力量体系",
                    Type = "能量设定",
                    ContentTemplate = "世界中力量的来源、分类和运用方式",
                    Importance = 8
                },
                new WorldSettingTemplate
                {
                    NameTemplate = "物品体系",
                    Type = "物品设定",
                    ContentTemplate = "重要物品的分类、等级和获取方式",
                    Importance = 7
                },
                new WorldSettingTemplate
                {
                    NameTemplate = "社会结构",
                    Type = "社会设定",
                    ContentTemplate = "社会的组织形式、等级制度和运行规则",
                    Importance = 7
                }
            };
        }

        /// <summary>
        /// 获取都市类世界设定模板
        /// </summary>
        private List<WorldSettingTemplate> GetUrbanWorldSettingTemplates()
        {
            return new List<WorldSettingTemplate>
            {
                new WorldSettingTemplate
                {
                    NameTemplate = "现代背景",
                    Type = "背景设定",
                    ContentTemplate = "故事发生的时代背景和社会环境",
                    Importance = 9
                },
                new WorldSettingTemplate
                {
                    NameTemplate = "特殊能力",
                    Type = "能力设定",
                    ContentTemplate = "如果有超自然元素，其表现形式和限制",
                    Importance = 8
                },
                new WorldSettingTemplate
                {
                    NameTemplate = "社会关系",
                    Type = "社会设定",
                    ContentTemplate = "现代社会的人际关系网络和社会结构",
                    Importance = 7
                },
                new WorldSettingTemplate
                {
                    NameTemplate = "经济体系",
                    Type = "经济设定",
                    ContentTemplate = "财富的来源、分配和影响力",
                    Importance = 6
                },
                new WorldSettingTemplate
                {
                    NameTemplate = "科技水平",
                    Type = "科技设定",
                    ContentTemplate = "科技发展水平和对日常生活的影响",
                    Importance = 6
                }
            };
        }

        /// <summary>
        /// 获取玄幻类世界设定模板
        /// </summary>
        private List<WorldSettingTemplate> GetFantasyWorldSettingTemplates()
        {
            return new List<WorldSettingTemplate>
            {
                new WorldSettingTemplate
                {
                    NameTemplate = "魔法体系",
                    Type = "魔法设定",
                    ContentTemplate = "魔法的分类、学习方式和使用限制",
                    Importance = 10
                },
                new WorldSettingTemplate
                {
                    NameTemplate = "种族设定",
                    Type = "种族设定",
                    ContentTemplate = "不同种族的特征、能力和文化差异",
                    Importance = 9
                },
                new WorldSettingTemplate
                {
                    NameTemplate = "世界构造",
                    Type = "世界设定",
                    ContentTemplate = "世界的物理结构和基本规则",
                    Importance = 8
                },
                new WorldSettingTemplate
                {
                    NameTemplate = "神话体系",
                    Type = "神话设定",
                    ContentTemplate = "神祇、传说和超自然存在",
                    Importance = 7
                },
                new WorldSettingTemplate
                {
                    NameTemplate = "文明发展",
                    Type = "文明设定",
                    ContentTemplate = "各文明的发展水平和交流方式",
                    Importance = 7
                }
            };
        }

        /// <summary>
        /// 获取通用世界设定模板
        /// </summary>
        private List<WorldSettingTemplate> GetGenericWorldSettingTemplates()
        {
            return new List<WorldSettingTemplate>
            {
                new WorldSettingTemplate
                {
                    NameTemplate = "基础设定",
                    Type = "基础设定",
                    ContentTemplate = "故事世界的基本规则和特征",
                    Importance = 9
                },
                new WorldSettingTemplate
                {
                    NameTemplate = "社会结构",
                    Type = "社会设定",
                    ContentTemplate = "社会的组织形式和运行机制",
                    Importance = 8
                },
                new WorldSettingTemplate
                {
                    NameTemplate = "文化背景",
                    Type = "文化设定",
                    ContentTemplate = "文化传统、价值观念和行为准则",
                    Importance = 7
                },
                new WorldSettingTemplate
                {
                    NameTemplate = "历史背景",
                    Type = "历史设定",
                    ContentTemplate = "重要的历史事件和发展脉络",
                    Importance = 6
                },
                new WorldSettingTemplate
                {
                    NameTemplate = "环境特色",
                    Type = "环境设定",
                    ContentTemplate = "独特的环境特征和地理条件",
                    Importance = 6
                }
            };
        }

        /// <summary>
        /// 获取默认势力模板
        /// </summary>
        private List<FactionTemplate> GetDefaultFactionTemplates(PrerequisiteGenerationOptions options)
        {
            return new List<FactionTemplate>
            {
                new FactionTemplate
                {
                    NameTemplate = "主要势力A",
                    Type = "组织/宗门",
                    DescriptionTemplate = "具有重要影响力的组织，有自己的理念和目标，立场复杂",
                    PowerLevel = 85,
                    Importance = 80,
                    TagTemplates = new List<string> { "影响力", "传统", "实力" }
                },
                new FactionTemplate
                {
                    NameTemplate = "主要势力B",
                    Type = "组织/宗门",
                    DescriptionTemplate = "另一个重要势力，与势力A有复杂的关系，既有合作也有竞争",
                    PowerLevel = 75,
                    Importance = 70,
                    TagTemplates = new List<string> { "创新", "活力", "变革" }
                },
                new FactionTemplate
                {
                    NameTemplate = "中立组织",
                    Type = "商业/学术组织",
                    DescriptionTemplate = "保持相对中立的立场，专注于自身领域的发展",
                    PowerLevel = 60,
                    Importance = 60,
                    TagTemplates = new List<string> { "中立", "专业", "资源" }
                }
            };
        }

        #endregion
    }
}
