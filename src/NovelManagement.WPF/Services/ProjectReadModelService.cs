using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NovelManagement.Application.Interfaces;
using NovelManagement.Application.Services;
using NovelManagement.WPF.Localization;
using NovelManagement.WPF.Models;

namespace NovelManagement.WPF.Services
{
    /// <summary>
    /// 项目只读聚合服务。
    /// </summary>
    public class ProjectReadModelService
    {

        private readonly ProjectService _projectService;
        private readonly CharacterService _characterService;
        private readonly FactionService _factionService;
        private readonly PlotService _plotService;
        private readonly VolumeService _volumeService;
        private readonly ChapterService _chapterService;
        private readonly IWorldSettingService _worldSettingService;
        private readonly ProjectStatisticsService _projectStatisticsService;
        private readonly ILogger<ProjectReadModelService> _logger;

        public ProjectReadModelService(
            ProjectService projectService,
            CharacterService characterService,
            FactionService factionService,
            PlotService plotService,
            VolumeService volumeService,
            ChapterService chapterService,
            IWorldSettingService worldSettingService,
            ProjectStatisticsService projectStatisticsService,
            ILogger<ProjectReadModelService> logger)
        {
            _projectService = projectService;
            _characterService = characterService;
            _factionService = factionService;
            _plotService = plotService;
            _volumeService = volumeService;
            _chapterService = chapterService;
            _worldSettingService = worldSettingService;
            _projectStatisticsService = projectStatisticsService;
            _logger = logger;
        }

        public async Task<ProjectWorkspaceReadModel?> GetWorkspaceAsync(Guid projectId)
        {
            try
            {
                var project = await _projectService.GetProjectByIdAsync(projectId);
                if (project == null)
                {
                    return null;
                }

                var statistics = await _projectStatisticsService.GetProjectStatisticsAsync(projectId);
                if (statistics == null)
                {
                    return null;
                }

                var characters = (await _characterService.GetCharactersByProjectIdAsync(projectId))
                    .OrderByDescending(c => c.Importance)
                    .ThenBy(c => c.Name)
                    .Take(5)
                    .ToList();

                var factions = (await _factionService.GetFactionsByProjectIdAsync(projectId))
                    .OrderByDescending(f => f.PowerLevel)
                    .ThenBy(f => f.Name)
                    .Take(5)
                    .ToList();

                var plots = (await _plotService.GetPlotsByProjectIdAsync(projectId))
                    .OrderByDescending(p => p.Importance)
                    .ThenBy(p => p.Title)
                    .Take(3)
                    .ToList();

                var volumes = (await _volumeService.GetVolumeListAsync(projectId)).ToList();
                var chapters = (await _chapterService.GetChaptersByProjectIdAsync(projectId)).ToList();
                var rootSettings = (await _worldSettingService.GetRootSettingsAsync(projectId)).ToList();

                var targetWordCount = volumes.Sum(v => v.EstimatedWordCount ?? 0);
                if (targetWordCount <= 0)
                {
                    targetWordCount = Math.Max(statistics.TotalWordCount, 1);
                }

                var targetChapterCount = Math.Max(chapters.Count, 1);
                var wordProgressPercent = targetWordCount > 0
                    ? Math.Min(100, (int)Math.Round((double)statistics.TotalWordCount / targetWordCount * 100))
                    : 0;
                var chapterProgressPercent = targetChapterCount > 0
                    ? Math.Min(100, (int)Math.Round((double)statistics.CompletedChapterCount / targetChapterCount * 100))
                    : 0;
                var settingCompletionPercent = Math.Min(100, rootSettings.Count * 10);

                return new ProjectWorkspaceReadModel
                {
                    ProjectId = project.Id,
                    ProjectName = project.Name,
                    ProjectDescription = project.Description ?? string.Empty,
                    ProjectType = LocalizationManager.LocalizeStoredValue(project.Type),
                    ProjectStatus = LocalizationManager.LocalizeStoredValue(project.Status),
                    HasProjectBaseInfo = !string.IsNullOrWhiteSpace(project.Name) && !string.IsNullOrWhiteSpace(project.Type),
                    ProjectCreatedAtText = project.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                    ProjectUpdatedAtText = project.UpdatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                    ProjectAuthorText = LocalizationManager.T("PO.NotSet", "未设置"),
                    ProjectTargetWordsText = targetWordCount > 0 ? string.Format(LocalizationManager.T("PO.WordsSuffixFmt", "{0} 字"), targetWordCount.ToString("N0")) : LocalizationManager.T("PO.NotSet", "未设置"),
                    WordCountProgressText = $"{statistics.TotalWordCount:N0} / {targetWordCount:N0}",
                    WordCountProgressPercent = wordProgressPercent,
                    WordCountPercentText = string.Format(LocalizationManager.T("PO.PercentDoneFmt", "{0}% 完成"), wordProgressPercent),
                    ChapterCountProgressText = $"{statistics.CompletedChapterCount} / {targetChapterCount}",
                    ChapterCountProgressPercent = chapterProgressPercent,
                    ChapterCountPercentText = string.Format(LocalizationManager.T("PO.PercentDoneFmt", "{0}% 完成"), chapterProgressPercent),
                    CharacterCountText = string.Format(LocalizationManager.T("PO.CountSuffixFmt", "{0}个"), statistics.CharacterCount),
                    FactionCountText = string.Format(LocalizationManager.T("PO.CountSuffixFmt", "{0}个"), statistics.FactionCount),
                    SettingCompletionText = $"{settingCompletionPercent}%",
                    CharacterOverviewText = characters.Count == 0
                        ? LocalizationManager.T("PO.NoCharacters", "暂无角色信息，请前往角色管理界面创建角色。")
                        : string.Join(Environment.NewLine, characters.Select(c => $"• {c.Name}（{c.Type}）")),
                    FactionOverviewText = factions.Count == 0
                        ? LocalizationManager.T("PO.NoFactions", "暂无势力信息，请在剧情稳定后补齐配套势力。")
                        : string.Join(Environment.NewLine, factions.Select(f => $"• {f.Name}（{f.Type}）")),
                    OutlineOverviewText = plots.Count == 0
                        ? LocalizationManager.T("PO.NoOutline", "暂无大纲内容，请点击AI生成大纲按钮创建大纲，或点击编辑大纲手动编辑。")
                        : string.Join(
                            Environment.NewLine + Environment.NewLine,
                            plots.Select(p =>
                            {
                                var description = string.IsNullOrWhiteSpace(p.Description) ? "暂无描述" : p.Description!;
                                return $"• {p.Title}（{p.Status}）{Environment.NewLine}{description}";
                            })),
                    Statistics = statistics
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "构建项目工作台读模型失败，ProjectId: {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<ProjectExportSelectionReadModel> GetExportSelectionAsync(Guid projectId)
        {
            try
            {
                var volumes = (await _volumeService.GetVolumeListAsync(projectId))
                    .OrderBy(v => v.Order)
                    .Select(v => new ProjectSelectableItemReadModel
                    {
                        Id = v.Id,
                        DisplayName = v.Title
                    })
                    .ToList();

                var chapters = (await _chapterService.GetChaptersByProjectIdAsync(projectId))
                    .OrderBy(c => c.Order)
                    .Select(c => new ProjectSelectableItemReadModel
                    {
                        Id = c.Id,
                        DisplayName = string.IsNullOrWhiteSpace(c.Title) ? "未命名章节" : c.Title
                    })
                    .ToList();

                return new ProjectExportSelectionReadModel
                {
                    VolumeOptions = volumes,
                    ChapterOptions = chapters
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "构建导出选择读模型失败，ProjectId: {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<ProjectContextData> BuildAiContextDataAsync(Guid projectId)
        {
            try
            {
                var project = await _projectService.GetProjectByIdAsync(projectId);
                var contextData = new ProjectContextData
                {
                    ProjectId = projectId,
                    ProjectName = project?.Name ?? string.Empty,
                    ProjectDescription = project?.Description ?? string.Empty,
                    ProjectType = project?.Type ?? string.Empty,
                    ProjectTags = project?.Tags ?? string.Empty,
                    ProjectNotes = project?.Notes ?? string.Empty
                };

                var plots = (await _plotService.GetPlotsByProjectIdAsync(projectId))
                    .OrderByDescending(p => p.Importance)
                    .ThenBy(p => p.Title)
                    .Take(5)
                    .Select(p => $"{p.Title}｜类型：{p.Type}｜{(string.IsNullOrWhiteSpace(p.Description) ? "无摘要" : p.Description)}")
                    .Cast<object>()
                    .ToList();
                var characters = (await _characterService.GetCharactersByProjectIdAsync(projectId))
                    .OrderByDescending(c => c.Importance)
                    .ThenBy(c => c.Name)
                    .Take(10)
                    .Select(c => $"{c.Name}｜类型：{c.Type}｜{(string.IsNullOrWhiteSpace(c.Background) ? "无背景" : c.Background)}")
                    .Cast<object>()
                    .ToList();
                var settings = (await _worldSettingService.GetAllAsync(projectId))
                    .OrderByDescending(s => s.Importance)
                    .ThenBy(s => s.Name)
                    .Take(10)
                    .Select(s => $"{s.Name}｜类型：{s.Type}｜{(string.IsNullOrWhiteSpace(s.Content) ? "无内容" : s.Content)}")
                    .Cast<object>()
                    .ToList();

                contextData.PlotOutlines = plots;
                contextData.MainCharacters = characters;
                contextData.WorldSettings = settings;
                contextData.PromptSummary = BuildPromptSummary(contextData);
                return contextData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "构建 AI 上下文读模型失败，ProjectId: {ProjectId}", projectId);
                throw;
            }
        }

        /// <summary>
        /// 构建子系统 AI 约束上下文：项目级 PromptSummary + 子系统领域限定。
        /// 失败时返回空字符串，不阻断页面局部生成能力。
        /// </summary>
        public async Task<string> BuildSubsystemPromptContextAsync(Guid projectId, string subsystemName, params string[] extraConstraints)
        {
            if (projectId == Guid.Empty || string.IsNullOrWhiteSpace(subsystemName))
            {
                return string.Empty;
            }

            try
            {
                var projectContext = await BuildAiContextDataAsync(projectId);
                var builder = new StringBuilder();
                if (!string.IsNullOrWhiteSpace(projectContext.PromptSummary))
                {
                    builder.AppendLine(projectContext.PromptSummary);
                    builder.AppendLine();
                }

                builder.AppendLine($"【{subsystemName}生成约束】");
                builder.AppendLine($"- 只能生成{subsystemName}相关内容，不得输出正文、完整大纲、角色小传或无关模块。");
                builder.AppendLine("- 必须优先遵循项目基础信息，再遵循已有世界设定和大纲。");
                builder.AppendLine("- 层级顺序必须保持为：项目基础信息 -> 世界观 -> 大纲 -> 配套设定 -> 正文写作。");
                foreach (var constraint in extraConstraints.Where(c => !string.IsNullOrWhiteSpace(c)))
                {
                    builder.AppendLine($"- {constraint}");
                }

                return builder.ToString().Trim();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "构建子系统 AI 约束上下文失败，ProjectId: {ProjectId}, 子系统: {Subsystem}", projectId, subsystemName);
                return string.Empty;
            }
        }

        private static string BuildPromptSummary(ProjectContextData contextData)
        {
            var builder = new StringBuilder();
            builder.AppendLine("【项目基本信息】");
            builder.AppendLine($"项目名称：{EmptyAsPlaceholder(contextData.ProjectName)}");
            builder.AppendLine($"项目类型：{EmptyAsPlaceholder(contextData.ProjectType)}");
            builder.AppendLine($"项目描述：{EmptyAsPlaceholder(contextData.ProjectDescription)}");
            builder.AppendLine($"项目标签：{EmptyAsPlaceholder(contextData.ProjectTags)}");
            builder.AppendLine($"项目备注：{EmptyAsPlaceholder(contextData.ProjectNotes)}");
            builder.AppendLine();

            builder.AppendLine("【已有世界设定】");
            if (contextData.WorldSettings.Count == 0)
            {
                builder.AppendLine("- 暂无");
            }
            else
            {
                foreach (var item in contextData.WorldSettings.Take(5))
                {
                    builder.AppendLine($"- {item}");
                }
            }

            builder.AppendLine();
            builder.AppendLine("【已有剧情大纲】");
            if (contextData.PlotOutlines.Count == 0)
            {
                builder.AppendLine("- 暂无");
            }
            else
            {
                foreach (var item in contextData.PlotOutlines.Take(5))
                {
                    builder.AppendLine($"- {item}");
                }
            }

            builder.AppendLine();
            builder.AppendLine("【主要角色】");
            if (contextData.MainCharacters.Count == 0)
            {
                builder.AppendLine("- 暂无");
            }
            else
            {
                foreach (var item in contextData.MainCharacters.Take(8))
                {
                    builder.AppendLine($"- {item}");
                }
            }

            builder.AppendLine();
            builder.AppendLine("【生成约束】");
            builder.AppendLine("- 新生成内容必须优先遵循项目基本信息。");
            builder.AppendLine("- 大纲必须建立在世界设定之上。");
            builder.AppendLine("- 其他设定必须按“项目基础信息 -> 世界观 -> 大纲 -> 配套设定 -> 正文写作”的顺序保持一致。");
            return builder.ToString().Trim();
        }

        private static string EmptyAsPlaceholder(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? "未设置" : value.Trim();
        }
    }

    /// <summary>
    /// 项目工作台只读模型。
    /// </summary>
    public sealed class ProjectWorkspaceReadModel
    {
        public Guid ProjectId { get; init; }
        public string ProjectName { get; init; } = string.Empty;
        public string ProjectDescription { get; init; } = string.Empty;
        public string ProjectType { get; init; } = string.Empty;
        public string ProjectStatus { get; init; } = string.Empty;
        public bool HasProjectBaseInfo { get; init; }
        public string ProjectCreatedAtText { get; init; } = string.Empty;
        public string ProjectUpdatedAtText { get; init; } = string.Empty;
        public string ProjectAuthorText { get; init; } = string.Empty;
        public string ProjectTargetWordsText { get; init; } = string.Empty;
        public string WordCountProgressText { get; init; } = string.Empty;
        public int WordCountProgressPercent { get; init; }
        public string WordCountPercentText { get; init; } = string.Empty;
        public string ChapterCountProgressText { get; init; } = string.Empty;
        public int ChapterCountProgressPercent { get; init; }
        public string ChapterCountPercentText { get; init; } = string.Empty;
        public string CharacterCountText { get; init; } = string.Empty;
        public string FactionCountText { get; init; } = string.Empty;
        public string SettingCompletionText { get; init; } = string.Empty;
        public string CharacterOverviewText { get; init; } = string.Empty;
        public string FactionOverviewText { get; init; } = string.Empty;
        public string OutlineOverviewText { get; init; } = string.Empty;
        public ProjectStatisticsSummary Statistics { get; init; } = new();
    }

    /// <summary>
    /// 项目导出选择只读模型。
    /// </summary>
    public sealed class ProjectExportSelectionReadModel
    {
        public IReadOnlyList<ProjectSelectableItemReadModel> VolumeOptions { get; init; } = Array.Empty<ProjectSelectableItemReadModel>();
        public IReadOnlyList<ProjectSelectableItemReadModel> ChapterOptions { get; init; } = Array.Empty<ProjectSelectableItemReadModel>();
    }

    /// <summary>
    /// 可选择项只读模型。
    /// </summary>
    public sealed class ProjectSelectableItemReadModel
    {
        public Guid Id { get; init; }
        public string DisplayName { get; init; } = string.Empty;
    }
}
