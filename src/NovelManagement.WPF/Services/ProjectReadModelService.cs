using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NovelManagement.Application.Interfaces;
using NovelManagement.Application.Services;
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
        private readonly PlotService _plotService;
        private readonly VolumeService _volumeService;
        private readonly ChapterService _chapterService;
        private readonly IWorldSettingService _worldSettingService;
        private readonly ProjectStatisticsService _projectStatisticsService;
        private readonly ILogger<ProjectReadModelService> _logger;

        public ProjectReadModelService(
            ProjectService projectService,
            CharacterService characterService,
            PlotService plotService,
            VolumeService volumeService,
            ChapterService chapterService,
            IWorldSettingService worldSettingService,
            ProjectStatisticsService projectStatisticsService,
            ILogger<ProjectReadModelService> logger)
        {
            _projectService = projectService;
            _characterService = characterService;
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
                    ProjectType = string.IsNullOrWhiteSpace(project.Type) ? "未设置" : project.Type,
                    ProjectStatus = string.IsNullOrWhiteSpace(project.Status) ? "未设置" : project.Status,
                    ProjectCreatedAtText = project.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                    ProjectUpdatedAtText = project.UpdatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                    ProjectAuthorText = "未设置",
                    ProjectTargetWordsText = targetWordCount > 0 ? $"{targetWordCount:N0} 字" : "未设置",
                    WordCountProgressText = $"{statistics.TotalWordCount:N0} / {targetWordCount:N0}",
                    WordCountProgressPercent = wordProgressPercent,
                    WordCountPercentText = $"{wordProgressPercent}% 完成",
                    ChapterCountProgressText = $"{statistics.CompletedChapterCount} / {targetChapterCount}",
                    ChapterCountProgressPercent = chapterProgressPercent,
                    ChapterCountPercentText = $"{chapterProgressPercent}% 完成",
                    CharacterCountText = $"{statistics.CharacterCount}个",
                    SettingCompletionText = $"{settingCompletionPercent}%",
                    CharacterOverviewText = characters.Count == 0
                        ? "暂无角色信息，请前往角色管理界面创建角色。"
                        : string.Join(Environment.NewLine, characters.Select(c => $"• {c.Name}（{c.Type}）")),
                    OutlineOverviewText = plots.Count == 0
                        ? "暂无大纲内容，请点击AI生成大纲按钮创建大纲，或点击编辑大纲手动编辑。"
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
                var contextData = new ProjectContextData
                {
                    ProjectId = projectId
                };

                var plots = (await _plotService.GetPlotsByProjectIdAsync(projectId))
                    .Take(5)
                    .Select(p => new { p.Title, p.Description, Type = p.Type })
                    .Cast<object>()
                    .ToList();
                var characters = (await _characterService.GetCharactersByProjectIdAsync(projectId))
                    .Take(10)
                    .Select(c => new { c.Name, c.Background, c.Type })
                    .Cast<object>()
                    .ToList();
                var settings = (await _worldSettingService.GetAllAsync(projectId))
                    .Take(10)
                    .Select(s => new { s.Name, s.Content, s.Type })
                    .Cast<object>()
                    .ToList();

                contextData.PlotOutlines = plots;
                contextData.MainCharacters = characters;
                contextData.WorldSettings = settings;
                return contextData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "构建 AI 上下文读模型失败，ProjectId: {ProjectId}", projectId);
                throw;
            }
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
        public string SettingCompletionText { get; init; } = string.Empty;
        public string CharacterOverviewText { get; init; } = string.Empty;
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
