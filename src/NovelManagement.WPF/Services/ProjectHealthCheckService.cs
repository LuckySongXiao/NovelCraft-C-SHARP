using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NovelManagement.Application.Interfaces;
using NovelManagement.Core.Entities;
using NovelManagement.Application.DTOs;
using NovelManagement.Application.Services;
using NovelManagement.WPF.Views;

namespace NovelManagement.WPF.Services
{
    /// <summary>
    /// 项目健康检查模式。
    /// </summary>
    public enum ProjectHealthCheckMode
    {
        /// <summary>一致性检查：数据引用完整性、时间线冲突、关系矛盾等。</summary>
        Consistency,

        /// <summary>质量检查：资料完备度、内容缺失、字数异常等。</summary>
        Quality,

        /// <summary>全部检查。</summary>
        All
    }

    /// <summary>
    /// 项目健康问题。
    /// </summary>
    public sealed class ProjectHealthIssue
    {
        /// <summary>严重级别：错误 / 警告 / 提示。</summary>
        public string Severity { get; init; } = "提示";

        /// <summary>问题分类，如：时间线、人物关系、角色档案。</summary>
        public string Category { get; init; } = string.Empty;

        /// <summary>问题摘要。</summary>
        public string Description { get; init; } = string.Empty;

        /// <summary>问题详细说明与修复建议。</summary>
        public string Detail { get; init; } = string.Empty;

        /// <summary>目标实体类型显示名，如：角色、时间线事件。</summary>
        public string TargetType { get; init; } = string.Empty;

        /// <summary>目标实体名称。</summary>
        public string TargetName { get; init; } = string.Empty;

        /// <summary>目标实体标识。</summary>
        public Guid? TargetId { get; init; }

        /// <summary>检查时间。</summary>
        public DateTime CheckedAt { get; init; } = DateTime.Now;

        /// <summary>严重级别排序权重：错误 0、警告 1、提示 2。</summary>
        public int SeverityRank => Severity switch
        {
            "错误" => 0,
            "警告" => 1,
            _ => 2
        };
    }

    /// <summary>
    /// 项目健康检查服务：基于项目真实数据执行规则化的一致性与质量检查。
    /// 检查结果可定位回具体实体，供报告页跳转复核。
    /// </summary>
    public class ProjectHealthCheckService
    {
        private readonly CharacterService _characterService;
        private readonly CharacterRelationshipService _relationshipService;
        private readonly FactionService _factionService;
        private readonly PlotService _plotService;
        private readonly VolumeService _volumeService;
        private readonly ChapterService _chapterService;
        private readonly IWorldSettingService _worldSettingService;
        private readonly TimelineDataService _timelineDataService;
        private readonly ILogger<ProjectHealthCheckService> _logger;

        public ProjectHealthCheckService(
            CharacterService characterService,
            CharacterRelationshipService relationshipService,
            FactionService factionService,
            PlotService plotService,
            VolumeService volumeService,
            ChapterService chapterService,
            IWorldSettingService worldSettingService,
            TimelineDataService timelineDataService,
            ILogger<ProjectHealthCheckService> logger)
        {
            _characterService = characterService;
            _relationshipService = relationshipService;
            _factionService = factionService;
            _plotService = plotService;
            _volumeService = volumeService;
            _chapterService = chapterService;
            _worldSettingService = worldSettingService;
            _timelineDataService = timelineDataService;
            _logger = logger;
        }

        /// <summary>
        /// 按指定模式执行健康检查，结果按严重级别排序（错误 > 警告 > 提示）。
        /// </summary>
        public async Task<List<ProjectHealthIssue>> RunChecksAsync(Guid projectId, ProjectHealthCheckMode mode)
        {
            var issues = new List<ProjectHealthIssue>();
            if (projectId == Guid.Empty)
            {
                return issues;
            }

            try
            {
                var characters = (await _characterService.GetCharactersByProjectIdAsync(projectId)).ToList();
                var factions = (await _factionService.GetFactionsByProjectIdAsync(projectId)).ToList();
                var plots = (await _plotService.GetPlotsByProjectIdAsync(projectId)).ToList();
                var chapters = (await _chapterService.GetChaptersByProjectIdAsync(projectId)).ToList();
                var volumes = (await _volumeService.GetVolumeListAsync(projectId)).ToList();
                var settings = (await _worldSettingService.GetAllAsync(projectId)).ToList();
                var timelineEvents = await _timelineDataService.LoadTimelineEventsAsync(projectId);

                var relationships = await LoadProjectRelationshipsAsync(characters);

                if (mode is ProjectHealthCheckMode.Consistency or ProjectHealthCheckMode.All)
                {
                    CheckTimelineConsistency(timelineEvents, issues);
                    CheckCharacterConsistency(characters, factions, relationships, issues);
                    CheckFactionConsistency(factions, characters, issues);
                    CheckPlotConsistency(plots, chapters, issues);
                }

                if (mode is ProjectHealthCheckMode.Quality or ProjectHealthCheckMode.All)
                {
                    CheckCharacterQuality(characters, issues);
                    CheckChapterQuality(chapters, volumes, issues);
                    CheckFactionQuality(factions, issues);
                    CheckPlotQuality(plots, issues);
                    CheckWorldSettingQuality(settings, issues);
                    CheckTimelineQuality(timelineEvents, issues);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行项目健康检查失败，ProjectId: {ProjectId}", projectId);
                throw;
            }

            return issues
                .OrderBy(issue => issue.SeverityRank)
                .ThenBy(issue => issue.Category)
                .ThenBy(issue => issue.Description)
                .ToList();
        }

        /// <summary>
        /// 聚合项目内全部人物关系（按关系标识去重）。
        /// </summary>
        private async Task<List<CharacterRelationship>> LoadProjectRelationshipsAsync(
            IEnumerable<Character> characters)
        {
            var relationshipMap = new Dictionary<Guid, CharacterRelationship>();
            foreach (var character in characters)
            {
                try
                {
                    var relationships = await _characterService.GetCharacterRelationshipsAsync(character.Id);
                    foreach (var relationship in relationships)
                    {
                        relationshipMap[relationship.Id] = relationship;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "加载人物关系失败，CharacterId: {CharacterId}", character.Id);
                }
            }

            return relationshipMap.Values.ToList();
        }

        #region 一致性检查

        private static void CheckTimelineConsistency(
            IReadOnlyList<TimelineEventViewModel> events,
            List<ProjectHealthIssue> issues)
        {
            // 同一参与者在同一天出现在多个事件中，可能存在时间线冲突。
            // 参与者若已关联到项目角色，则按角色标识分组；否则退回名称匹配（同名角色可能误报）。
            var participantGroups = events
                .SelectMany(evt => evt.Participants
                    .Where(participant => !string.IsNullOrWhiteSpace(participant.Name))
                    .Select(participant => new
                    {
                        evt,
                        participant.Name,
                        participant.CharacterId,
                        MatchKey = participant.CharacterId.HasValue
                            ? $"char:{participant.CharacterId.Value:N}"
                            : $"name:{participant.Name!.Trim()}"
                    }))
                .GroupBy(item => (item.MatchKey, item.evt.EventDate.Date))
                .Where(group => group.Count() > 1);

            foreach (var group in participantGroups)
            {
                var displayName = group.First().Name;
                var titles = string.Join("、", group.Select(item => item.evt.Title));
                var isFuzzy = group.First().CharacterId == null;

                issues.Add(new ProjectHealthIssue
                {
                    Severity = "错误",
                    Category = "时间线",
                    Description = $"参与者“{displayName}”在同一天出现在多个事件中",
                    Detail = $"事件日期：{group.Key.Item2:yyyy-MM-dd}；涉及事件：{titles}。请核实事件先后顺序或调整事件日期。"
                             + (isFuzzy ? "（该参与者未关联到项目角色，按名称匹配，若存在同名角色可能误报。）" : string.Empty),
                    TargetType = "时间线事件",
                    TargetName = titles,
                    TargetId = group.First().evt.EventId == Guid.Empty ? null : group.First().evt.EventId
                });
            }

            // 同一日期同一标题的事件可能重复录入
            var duplicateGroups = events
                .GroupBy(evt => (evt.Title, evt.EventDate.Date))
                .Where(group => group.Count() > 1);

            foreach (var group in duplicateGroups)
            {
                issues.Add(new ProjectHealthIssue
                {
                    Severity = "警告",
                    Category = "时间线",
                    Description = $"存在疑似重复的事件：“{group.Key.Title}”",
                    Detail = $"同一天（{group.Key.Item2:yyyy-MM-dd}）记录了 {group.Count()} 条同名事件，请确认是否重复录入。",
                    TargetType = "时间线事件",
                    TargetName = group.Key.Title,
                    TargetId = group.First().EventId == Guid.Empty ? null : group.First().EventId
                });
            }
        }

        private static void CheckCharacterConsistency(
            IReadOnlyList<Character> characters,
            IReadOnlyList<Faction> factions,
            IReadOnlyList<CharacterRelationship> relationships,
            List<ProjectHealthIssue> issues)
        {
            var characterIds = characters.Select(character => character.Id).ToHashSet();
            var factionIds = factions.Select(faction => faction.Id).ToHashSet();
            var characterNameMap = characters.ToDictionary(character => character.Id, character => character.Name);

            // 人物关系自环
            foreach (var relationship in relationships.Where(r => r.SourceCharacterId == r.TargetCharacterId))
            {
                issues.Add(new ProjectHealthIssue
                {
                    Severity = "错误",
                    Category = "人物关系",
                    Description = $"“{characterNameMap.GetValueOrDefault(relationship.SourceCharacterId, "未知角色")}”存在指向自己的关系",
                    Detail = $"关系类型：{relationship.RelationshipType}。角色不能与自身建立关系，请删除或修正该关系。",
                    TargetType = "关系网络",
                    TargetName = relationship.RelationshipType,
                    TargetId = relationship.SourceCharacterId
                });
            }

            // 同一对角色的重复同类型关系
            var duplicateRelationships = relationships
                .Where(r => r.SourceCharacterId != r.TargetCharacterId)
                .GroupBy(r => (r.SourceCharacterId, r.TargetCharacterId, r.RelationshipType))
                .Where(group => group.Count() > 1);

            foreach (var group in duplicateRelationships)
            {
                var sourceName = characterNameMap.GetValueOrDefault(group.Key.SourceCharacterId, "未知角色");
                var targetName = characterNameMap.GetValueOrDefault(group.Key.TargetCharacterId, "未知角色");
                issues.Add(new ProjectHealthIssue
                {
                    Severity = "警告",
                    Category = "人物关系",
                    Description = $"{sourceName} 与 {targetName} 之间存在多条“{group.Key.RelationshipType}”关系",
                    Detail = $"共 {group.Count()} 条同类关系，可能为重复录入，请合并或清理。",
                    TargetType = "关系网络",
                    TargetName = $"{sourceName} ↔ {targetName}",
                    TargetId = group.Key.SourceCharacterId
                });
            }

            // 角色所属势力无效
            foreach (var character in characters.Where(c => c.FactionId.HasValue && !factionIds.Contains(c.FactionId.Value)))
            {
                issues.Add(new ProjectHealthIssue
                {
                    Severity = "错误",
                    Category = "角色档案",
                    Description = $"角色“{character.Name}”所属势力不存在",
                    Detail = "所属势力可能已被删除，请重新为该角色指定势力。",
                    TargetType = "角色",
                    TargetName = character.Name,
                    TargetId = character.Id
                });
            }
        }

        private static void CheckFactionConsistency(
            IReadOnlyList<Faction> factions,
            IReadOnlyList<Character> characters,
            List<ProjectHealthIssue> issues)
        {
            var factionIds = factions.Select(faction => faction.Id).ToHashSet();
            var characterIds = characters.Select(character => character.Id).ToHashSet();
            var characterNameMap = characters.ToDictionary(character => character.Id, character => character.Name);
            var factionNameMap = factions.ToDictionary(faction => faction.Id, faction => faction.Name);

            // 首领无效
            foreach (var faction in factions.Where(f => f.LeaderId.HasValue && !characterIds.Contains(f.LeaderId.Value)))
            {
                issues.Add(new ProjectHealthIssue
                {
                    Severity = "错误",
                    Category = "势力组织",
                    Description = $"势力“{faction.Name}”的首领不存在",
                    Detail = "首领角色可能已被删除，请重新指定首领。",
                    TargetType = "势力",
                    TargetName = faction.Name,
                    TargetId = faction.Id
                });
            }

            // 父势力无效或自引用
            foreach (var faction in factions.Where(f => f.ParentFactionId.HasValue))
            {
                if (faction.ParentFactionId!.Value == faction.Id)
                {
                    issues.Add(new ProjectHealthIssue
                    {
                        Severity = "错误",
                        Category = "势力组织",
                        Description = $"势力“{faction.Name}”的上级势力指向自身",
                        Detail = "上级势力不能是自身，请修正层级关系。",
                        TargetType = "势力",
                        TargetName = faction.Name,
                        TargetId = faction.Id
                    });
                }
                else if (!factionIds.Contains(faction.ParentFactionId!.Value))
                {
                    issues.Add(new ProjectHealthIssue
                    {
                        Severity = "警告",
                        Category = "势力组织",
                        Description = $"势力“{faction.Name}”的上级势力不存在",
                        Detail = "上级势力可能已被删除，该势力将被视为根级势力，请重新指定上级。",
                        TargetType = "势力",
                        TargetName = faction.Name,
                        TargetId = faction.Id
                    });
                }
            }

            // 首领角色与势力归属矛盾：首领不属于该势力
            foreach (var faction in factions.Where(f => f.LeaderId.HasValue && characterIds.Contains(f.LeaderId.Value)))
            {
                var leader = characters.FirstOrDefault(c => c.Id == faction.LeaderId!.Value);
                if (leader == null)
                {
                    continue;
                }

                var leaderFactionName = leader.FactionId.HasValue
                    ? factionNameMap.GetValueOrDefault(leader.FactionId.Value, "未知势力")
                    : "无";
                var leaderName = characterNameMap.GetValueOrDefault(leader.Id, "未知角色");

                if (leader.FactionId.HasValue && leader.FactionId.Value != faction.Id)
                {
                    issues.Add(new ProjectHealthIssue
                    {
                        Severity = "警告",
                        Category = "势力组织",
                        Description = $"势力“{faction.Name}”的首领不在该势力中",
                        Detail = $"首领“{leaderName}”的所属势力为“{leaderFactionName}”，请核对。",
                        TargetType = "势力",
                        TargetName = faction.Name,
                        TargetId = faction.Id
                    });
                }
            }
        }

        private static void CheckPlotConsistency(
            IReadOnlyList<Plot> plots,
            IReadOnlyList<Chapter> chapters,
            List<ProjectHealthIssue> issues)
        {
            var chapterIds = chapters.Select(chapter => chapter.Id).ToHashSet();
            var chapterNameMap = chapters.ToDictionary(chapter => chapter.Id, chapter => chapter.Title);

            foreach (var plot in plots)
            {
                if (plot.Progress < 0 || plot.Progress > 100)
                {
                    issues.Add(new ProjectHealthIssue
                    {
                        Severity = "错误",
                        Category = "剧情大纲",
                        Description = $"剧情“{plot.Title}”进度超出范围",
                        Detail = $"当前进度：{plot.Progress}%（有效范围 0~100）。",
                        TargetType = "剧情",
                        TargetName = plot.Title,
                        TargetId = plot.Id
                    });
                }

                if (plot.StartChapterId.HasValue && !chapterIds.Contains(plot.StartChapterId.Value))
                {
                    issues.Add(new ProjectHealthIssue
                    {
                        Severity = "警告",
                        Category = "剧情大纲",
                        Description = $"剧情“{plot.Title}”的起始章节不存在",
                        Detail = "起始章节可能已被删除，请重新指定。",
                        TargetType = "剧情",
                        TargetName = plot.Title,
                        TargetId = plot.Id
                    });
                }

                if (plot.EndChapterId.HasValue && !chapterIds.Contains(plot.EndChapterId.Value))
                {
                    issues.Add(new ProjectHealthIssue
                    {
                        Severity = "警告",
                        Category = "剧情大纲",
                        Description = $"剧情“{plot.Title}”的结束章节不存在",
                        Detail = "结束章节可能已被删除，请重新指定。",
                        TargetType = "剧情",
                        TargetName = plot.Title,
                        TargetId = plot.Id
                    });
                }
            }

            // 同一卷内章节顺序重复
            var duplicateOrders = chapters
                .GroupBy(chapter => (chapter.VolumeId, chapter.Order))
                .Where(group => group.Count() > 1);

            foreach (var group in duplicateOrders)
            {
                var titles = string.Join("、", group.Select(chapter => chapter.Title));
                issues.Add(new ProjectHealthIssue
                {
                    Severity = "警告",
                    Category = "章节正文",
                    Description = "同一卷内存在重复的章节顺序",
                    Detail = $"顺序号 {group.Key.Order} 被以下章节占用：{titles}。请调整章节顺序。",
                    TargetType = "卷章",
                    TargetName = titles,
                    TargetId = group.First().Id
                });
            }
        }

        #endregion

        #region 质量检查

        private static void CheckCharacterQuality(
            IReadOnlyList<Character> characters,
            List<ProjectHealthIssue> issues)
        {
            foreach (var character in characters)
            {
                var missing = new List<string>();
                if (string.IsNullOrWhiteSpace(character.Background)) missing.Add("背景");
                if (string.IsNullOrWhiteSpace(character.Personality)) missing.Add("性格");
                if (string.IsNullOrWhiteSpace(character.Appearance)) missing.Add("外貌");

                if (character.Importance >= 8 && missing.Count > 0)
                {
                    issues.Add(new ProjectHealthIssue
                    {
                        Severity = "警告",
                        Category = "角色档案",
                        Description = $"重要角色“{character.Name}”资料不全",
                        Detail = $"缺失字段：{string.Join("、", missing)}。重要角色（重要度≥8）应具备完整设定。",
                        TargetType = "角色",
                        TargetName = character.Name,
                        TargetId = character.Id
                    });
                }

                if (character.FirstAppearanceChapterId.HasValue && string.IsNullOrWhiteSpace(character.History))
                {
                    issues.Add(new ProjectHealthIssue
                    {
                        Severity = "提示",
                        Category = "角色档案",
                        Description = $"角色“{character.Name}”缺少人物履历",
                        Detail = "该角色已关联首次出场章节，但履历为空。保存章节后会自动同步履历，也可手动补充。",
                        TargetType = "角色",
                        TargetName = character.Name,
                        TargetId = character.Id
                    });
                }
            }

            // 角色重名
            foreach (var group in characters
                .GroupBy(character => character.Name.Trim())
                .Where(group => group.Count() > 1))
            {
                var types = string.Join("、", group.Select(character => character.Type));
                issues.Add(new ProjectHealthIssue
                {
                    Severity = "警告",
                    Category = "角色档案",
                    Description = $"存在重名角色：“{group.Key}”",
                    Detail = $"共 {group.Count()} 个同名角色（类型：{types}），写作时容易混淆，建议改名或添加区分标记。",
                    TargetType = "角色",
                    TargetName = group.Key,
                    TargetId = group.First().Id
                });
            }
        }

        private static void CheckChapterQuality(
            IReadOnlyList<Chapter> chapters,
            IReadOnlyList<Volume> volumes,
            List<ProjectHealthIssue> issues)
        {
            var volumeNameMap = volumes.ToDictionary(volume => volume.Id, volume => volume.Title);

            foreach (var chapter in chapters)
            {
                var hasContent = !string.IsNullOrWhiteSpace(chapter.Content);

                if (!hasContent && string.Equals(chapter.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                {
                    issues.Add(new ProjectHealthIssue
                    {
                        Severity = "错误",
                        Category = "章节正文",
                        Description = $"章节“{chapter.Title}”已标记完成但无正文",
                        Detail = $"所属卷：{volumeNameMap.GetValueOrDefault(chapter.VolumeId, "未知卷")}。请补写正文或将状态改回草稿。",
                        TargetType = "卷章",
                        TargetName = chapter.Title,
                        TargetId = chapter.Id
                    });
                }

                if (hasContent && string.IsNullOrWhiteSpace(chapter.Summary))
                {
                    issues.Add(new ProjectHealthIssue
                    {
                        Severity = "提示",
                        Category = "章节正文",
                        Description = $"章节“{chapter.Title}”缺少摘要",
                        Detail = "章节摘要是自动同步履历、时间线与关系的重要依据，建议补写。",
                        TargetType = "卷章",
                        TargetName = chapter.Title,
                        TargetId = chapter.Id
                    });
                }

                if (hasContent && chapter.WordCount <= 0)
                {
                    issues.Add(new ProjectHealthIssue
                    {
                        Severity = "提示",
                        Category = "章节正文",
                        Description = $"章节“{chapter.Title}”字数统计异常",
                        Detail = "章节已有正文但字数为 0，保存一次正文即可刷新统计。",
                        TargetType = "卷章",
                        TargetName = chapter.Title,
                        TargetId = chapter.Id
                    });
                }
            }
        }

        private static void CheckFactionQuality(
            IReadOnlyList<Faction> factions,
            List<ProjectHealthIssue> issues)
        {
            foreach (var faction in factions)
            {
                if (faction.Importance >= 8 && string.IsNullOrWhiteSpace(faction.Description))
                {
                    issues.Add(new ProjectHealthIssue
                    {
                        Severity = "警告",
                        Category = "势力组织",
                        Description = $"重要势力“{faction.Name}”缺少描述",
                        Detail = "重要势力（重要度≥8）应至少填写势力描述。",
                        TargetType = "势力",
                        TargetName = faction.Name,
                        TargetId = faction.Id
                    });
                }
            }
        }

        private static void CheckPlotQuality(
            IReadOnlyList<Plot> plots,
            List<ProjectHealthIssue> issues)
        {
            foreach (var plot in plots.Where(plot => string.Equals(plot.Type, "主线", StringComparison.Ordinal)))
            {
                if (string.IsNullOrWhiteSpace(plot.Description) && string.IsNullOrWhiteSpace(plot.Outline))
                {
                    issues.Add(new ProjectHealthIssue
                    {
                        Severity = "警告",
                        Category = "剧情大纲",
                        Description = $"主线剧情“{plot.Title}”缺少描述与大纲",
                        Detail = "主线剧情是全书的骨架，应补充剧情描述或大纲。",
                        TargetType = "剧情",
                        TargetName = plot.Title,
                        TargetId = plot.Id
                    });
                }
            }
        }

        private static void CheckWorldSettingQuality(
            IReadOnlyList<WorldSettingDto> settings,
            List<ProjectHealthIssue> issues)
        {
            foreach (var setting in settings)
            {
                if (setting.Importance >= 8 && string.IsNullOrWhiteSpace(setting.Content))
                {
                    issues.Add(new ProjectHealthIssue
                    {
                        Severity = "警告",
                        Category = "世界设定",
                        Description = $"重要设定“{setting.Name}”缺少内容",
                        Detail = "重要设定（重要度≥8）应补充详细内容，它是大纲与正文的世界观依据。",
                        TargetType = "世界设定",
                        TargetName = setting.Name,
                        TargetId = setting.Id
                    });
                }
            }
        }

        private static void CheckTimelineQuality(
            IReadOnlyList<TimelineEventViewModel> events,
            List<ProjectHealthIssue> issues)
        {
            foreach (var evt in events.Where(evt => string.IsNullOrWhiteSpace(evt.Location)))
            {
                issues.Add(new ProjectHealthIssue
                {
                    Severity = "提示",
                    Category = "时间线",
                    Description = $"事件“{evt.Title}”缺少地点信息",
                    Detail = $"事件日期：{evt.EventDate:yyyy-MM-dd}。补充地点有助于后续场景与地图设定的一致性。",
                    TargetType = "时间线事件",
                    TargetName = evt.Title,
                    TargetId = evt.EventId == Guid.Empty ? null : evt.EventId
                });
            }

            foreach (var evt in events.Where(evt => evt.ParticipantCount <= 0))
            {
                issues.Add(new ProjectHealthIssue
                {
                    Severity = "提示",
                    Category = "时间线",
                    Description = $"事件“{evt.Title}”没有参与者",
                    Detail = "无参与者的事件难以建立人物与事件的关联，建议补充参与者。",
                    TargetType = "时间线事件",
                    TargetName = evt.Title,
                    TargetId = evt.EventId == Guid.Empty ? null : evt.EventId
                });
            }

            // 参与者未关联到项目角色：会导致时间线冲突检测退回名称匹配，也可能只是录入了不存在的角色名
            foreach (var evt in events)
            {
                var unlinked = evt.Participants
                    .Where(participant => participant.CharacterId == null && !string.IsNullOrWhiteSpace(participant.Name))
                    .Select(participant => participant.Name.Trim())
                    .Distinct()
                    .ToList();

                if (unlinked.Count == 0)
                {
                    continue;
                }

                issues.Add(new ProjectHealthIssue
                {
                    Severity = "提示",
                    Category = "时间线",
                    Description = $"事件“{evt.Title}”的参与者未关联到项目角色",
                    Detail = $"未关联：{string.Join("、", unlinked)}。保存事件时会自动按名称匹配同名角色；若角色尚不存在或名称不一致，请先在角色管理中创建或改名。",
                    TargetType = "时间线事件",
                    TargetName = evt.Title,
                    TargetId = evt.EventId == Guid.Empty ? null : evt.EventId
                });
            }
        }

        #endregion
    }
}
