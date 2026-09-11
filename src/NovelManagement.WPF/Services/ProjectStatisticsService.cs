using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static NovelManagement.WPF.Localization.LocalizationManager;
using Microsoft.Extensions.Logging;
using NovelManagement.Application.Interfaces;
using NovelManagement.Application.Services;

namespace NovelManagement.WPF.Services
{
    /// <summary>
    /// 项目统计聚合服务。
    /// </summary>
    public class ProjectStatisticsService
    {
        private readonly ProjectService _projectService;
        private readonly CharacterService _characterService;
        private readonly FactionService _factionService;
        private readonly VolumeService _volumeService;
        private readonly ChapterService _chapterService;
        private readonly PlotService _plotService;
        private readonly IWorldSettingService _worldSettingService;
        private readonly ILogger<ProjectStatisticsService> _logger;

        public ProjectStatisticsService(
            ProjectService projectService,
            CharacterService characterService,
            FactionService factionService,
            VolumeService volumeService,
            ChapterService chapterService,
            PlotService plotService,
            IWorldSettingService worldSettingService,
            ILogger<ProjectStatisticsService> logger)
        {
            _projectService = projectService;
            _characterService = characterService;
            _factionService = factionService;
            _volumeService = volumeService;
            _chapterService = chapterService;
            _plotService = plotService;
            _worldSettingService = worldSettingService;
            _logger = logger;
        }

        public async Task<ProjectStatisticsSummary?> GetProjectStatisticsAsync(Guid projectId)
        {
            try
            {
                var project = await _projectService.GetProjectByIdAsync(projectId);
                if (project == null)
                {
                    return null;
                }

                var characters = (await _characterService.GetCharactersByProjectIdAsync(projectId)).ToList();
                var factions = (await _factionService.GetFactionsByProjectIdAsync(projectId)).ToList();
                var volumes = (await _volumeService.GetVolumeListAsync(projectId)).ToList();
                var chapters = (await _chapterService.GetChaptersByProjectIdAsync(projectId)).ToList();
                var plots = (await _plotService.GetPlotsByProjectIdAsync(projectId)).ToList();
                var worldSettings = (await _worldSettingService.GetAllAsync(projectId)).ToList();

                var totalWordCount = chapters.Sum(c => c.WordCount);
                var completedChapterCount = chapters.Count(c => string.Equals(c.Status, "Completed", StringComparison.OrdinalIgnoreCase));
                var draftChapterCount = chapters.Count(c => string.Equals(c.Status, "Draft", StringComparison.OrdinalIgnoreCase));
                var averageChapterWordCount = chapters.Count > 0 ? (int)Math.Round((double)totalWordCount / chapters.Count) : 0;
                var averageVolumeWordCount = volumes.Count > 0 ? (int)Math.Round((double)totalWordCount / volumes.Count) : 0;

                return new ProjectStatisticsSummary
                {
                    ProjectId = project.Id,
                    ProjectName = project.Name,
                    ProjectType = project.Type ?? string.Empty,
                    ProjectStatus = project.Status ?? string.Empty,
                    LastUpdatedAt = project.UpdatedAt.ToLocalTime(),
                    VolumeCount = volumes.Count,
                    ChapterCount = chapters.Count,
                    CompletedChapterCount = completedChapterCount,
                    DraftChapterCount = draftChapterCount,
                    CharacterCount = characters.Count,
                    FactionCount = factions.Count,
                    PlotCount = plots.Count,
                    WorldSettingCount = worldSettings.Count,
                    TotalWordCount = totalWordCount,
                    AverageChapterWordCount = averageChapterWordCount,
                    AverageVolumeWordCount = averageVolumeWordCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取项目统计数据失败，ProjectId: {ProjectId}", projectId);
                throw;
            }
        }

        public Window CreateStatisticsWindow(ProjectStatisticsSummary summary)
        {
            var statisticsWindow = new Window
            {
                Title = TF("STAT.TitleFmt", "项目统计分析 - {0}", summary.ProjectName),
                Width = 1200,
                Height = 800,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            var statisticsContent = new StackPanel
            {
                Margin = new Thickness(24)
            };

            statisticsContent.Children.Add(new TextBlock
            {
                Text = T("STAT.Header", "项目统计分析"),
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20)
            });

            statisticsContent.Children.Add(new TextBlock
            {
                Text = BuildReportText(summary),
                FontSize = 14,
                LineHeight = 20
            });

            statisticsWindow.Content = new ScrollViewer
            {
                Content = statisticsContent
            };

            return statisticsWindow;
        }

        public string BuildReportText(ProjectStatisticsSummary summary)
        {
            var builder = new StringBuilder();
            var wordsFmt = T("STAT.WordsFmt", "{0} 字");
            builder.AppendLine(T("STAT.Section", "项目统计信息"));
            builder.AppendLine();
            builder.AppendLine("• " + T("STAT.Name", "项目名称") + "：" + summary.ProjectName);
            builder.AppendLine("• " + T("STAT.Type", "项目类型") + "：" + LocalizeStoredValue(summary.ProjectType));
            builder.AppendLine("• " + T("STAT.Status", "项目状态") + "：" + LocalizeStoredValue(summary.ProjectStatus));
            builder.AppendLine("• " + T("STAT.LastUpdate", "最后更新") + "：" + summary.LastUpdatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));
            builder.AppendLine("• " + T("STAT.Volumes", "卷数") + "：" + summary.VolumeCount);
            builder.AppendLine("• " + T("STAT.Chapters", "章节数") + "：" + summary.ChapterCount);
            builder.AppendLine("• " + T("STAT.CompletedChapters", "已完成章节") + "：" + summary.CompletedChapterCount);
            builder.AppendLine("• " + T("STAT.DraftChapters", "草稿章节") + "：" + summary.DraftChapterCount);
            builder.AppendLine("• " + T("STAT.Characters", "角色数") + "：" + summary.CharacterCount);
            builder.AppendLine("• " + T("STAT.Factions", "势力数") + "：" + summary.FactionCount);
            builder.AppendLine("• " + T("STAT.Plots", "剧情数") + "：" + summary.PlotCount);
            builder.AppendLine("• " + T("STAT.Settings", "设定数") + "：" + summary.WorldSettingCount);
            builder.AppendLine("• " + T("STAT.TotalWords", "章节总字数") + "：" + string.Format(wordsFmt, summary.TotalWordCount.ToString("N0")));
            builder.AppendLine("• " + T("STAT.AvgChapterWords", "平均章节字数") + "：" + string.Format(wordsFmt, summary.AverageChapterWordCount.ToString("N0")));
            builder.AppendLine("• " + T("STAT.AvgVolumeWords", "平均卷字数") + "：" + string.Format(wordsFmt, summary.AverageVolumeWordCount.ToString("N0")));
            builder.AppendLine();
            builder.AppendLine(T("STAT.Notes", "说明："));
            builder.AppendLine("• " + T("STAT.Note1", "当前统计入口已统一接入项目统计聚合服务"));
            builder.AppendLine("• " + T("STAT.Note2", "后续可继续扩展创作趋势、AI 使用、结构质量等更细分报表"));
            return builder.ToString();
        }
    }

    /// <summary>
    /// 项目统计摘要。
    /// </summary>
    public sealed class ProjectStatisticsSummary
    {
        public Guid ProjectId { get; init; }
        public string ProjectName { get; init; } = string.Empty;
        public string ProjectType { get; init; } = string.Empty;
        public string ProjectStatus { get; init; } = string.Empty;
        public DateTime LastUpdatedAt { get; init; }
        public int VolumeCount { get; init; }
        public int ChapterCount { get; init; }
        public int CompletedChapterCount { get; init; }
        public int DraftChapterCount { get; init; }
        public int CharacterCount { get; init; }
        public int FactionCount { get; init; }
        public int PlotCount { get; init; }
        public int WorldSettingCount { get; init; }
        public int TotalWordCount { get; init; }
        public int AverageChapterWordCount { get; init; }
        public int AverageVolumeWordCount { get; init; }
    }
}
