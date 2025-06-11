using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
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

                // 根据选项生成缺失的数据
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
                    // 如果AI服务不可用，回退到默认生成
                    _logger.LogWarning("AI服务不可用，使用默认生成方式");
                    await GeneratePrerequisitesAsync(projectId);
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
                // 构建AI生成请求
                var prompt = $@"
请为小说项目生成前置数据，要求：
{aiPrompt}

请生成以下内容：
1. 3个剧情大纲（包含主线、情感线、支线等，避免善恶二元对立）
2. 3个主要角色（性格和立场要多样化，避免脸谱化）
3. 5个世界设定（体系要完整合理）
4. 3个势力组织（立场和性质要多元化，不要简单的正邪对立）

注意：
- 势力组织不要简单分为正道邪道，要有复杂的利益关系和立场
- 角色要有深度，避免单一的善恶标签
- 世界观要自洽，有内在逻辑
";

                // 调用AI服务生成内容
                var parameters = new Dictionary<string, object>
                {
                    ["prompt"] = prompt,
                    ["projectId"] = projectId.ToString()
                };
                var aiResponse = await _aiAssistantService.GeneratePlotAsync(parameters);

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
            // 这里可以实现AI响应的解析逻辑
            // 暂时使用默认生成作为示例
            await GenerateDefaultDataAsync(projectId, result);
        }

        /// <summary>
        /// 生成默认数据
        /// </summary>
        private async Task GenerateDefaultDataAsync(Guid projectId, PrerequisiteGenerationResult result)
        {
            var options = new PrerequisiteGenerationOptions();

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
                        CultivationLevel = "练气初期",
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
                        CultivationLevel = "筑基后期",
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
                        CultivationLevel = "大乘期",
                        Appearance = "仙风道骨，白发飘逸，深不可测",
                        Personality = "睿智深沉，慈祥严厉，洞察世事",
                        Background = "隐世高人，曾经的修仙界传奇人物",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };

                foreach (var character in defaultCharacters)
                {
                    await characterService.CreateCharacterAsync(character);
                    result.GeneratedItems.Add($"主要角色: {character.Name} ({character.Type})");
                }

                result.GeneratedCharactersCount = defaultCharacters.Count;
                _logger.LogInformation("为项目 {ProjectId} 生成了 {Count} 个主要角色", projectId, defaultCharacters.Count);
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
                        Content = "修炼等级：练气→筑基→金丹→元婴→化神→炼虚→合体→大乘→渡劫→仙人。每个大境界分为初期、中期、后期、巅峰四个小境界。",
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
