using NovelManagement.Application.Services;
using NovelManagement.Core.Entities;
using NovelManagement.WPF.Views;

namespace NovelManagement.WPF.Services;

/// <summary>
/// 章节写作后的标准更新工艺服务。
/// 负责串联章节上下文同步、关系更新结果汇总与时间线写入。
/// </summary>
public class ChapterUpdateWorkflowService
{
    private readonly ChapterContentSyncService _chapterContentSyncService;
    private readonly TimelineDataService _timelineDataService;
    private readonly VolumeService _volumeService;

    public ChapterUpdateWorkflowService(
        ChapterContentSyncService chapterContentSyncService,
        TimelineDataService timelineDataService,
        VolumeService volumeService)
    {
        _chapterContentSyncService = chapterContentSyncService;
        _timelineDataService = timelineDataService;
        _volumeService = volumeService;
    }

    public async Task<ChapterUpdateWorkflowResult> RunAsync(
        Chapter chapter,
        string? relatedCharactersText = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chapter);

        var syncResult = await _chapterContentSyncService.SyncChapterAsync(chapter, relatedCharactersText, cancellationToken);
        var timelineEventCount = await SyncTimelineAsync(chapter, syncResult, cancellationToken);

        return new ChapterUpdateWorkflowResult
        {
            SyncResult = syncResult,
            UpdatedTimelineEventCount = timelineEventCount
        };
    }

    private async Task<int> SyncTimelineAsync(
        Chapter chapter,
        ChapterContentSyncResult syncResult,
        CancellationToken cancellationToken)
    {
        var volume = chapter.Volume ?? await _volumeService.GetVolumeByIdAsync(chapter.VolumeId, cancellationToken);
        var projectId = volume?.ProjectId ?? Guid.Empty;
        if (projectId == Guid.Empty)
        {
            return 0;
        }

        var events = await _timelineDataService.LoadTimelineEventsAsync(projectId);
        var marker = BuildChapterMarker(chapter);
        var summary = BuildSummaryText(chapter);
        var participants = BuildParticipants(syncResult);

        var existingEvent = events.FirstOrDefault(evt =>
            string.Equals(evt.Category, "剧情事件", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(evt.Location, marker, StringComparison.OrdinalIgnoreCase));

        if (existingEvent == null)
        {
            // EventId 在视图模型构造时自动生成，保存时据此写入数据库；
            // int Id 仅为界面展示序号，由加载过程重新分配。
            existingEvent = new TimelineEventViewModel
            {
                IsNew = true,
                CreatedAt = DateTime.Now
            };
            events.Add(existingEvent);
        }

        existingEvent.Title = chapter.Title ?? "未命名章节";
        existingEvent.Category = "剧情事件";
        existingEvent.EventDate = chapter.UpdatedAt == default ? DateTime.Now : chapter.UpdatedAt.ToLocalTime();
        existingEvent.Location = marker;
        existingEvent.Importance = ResolveImportance(syncResult);
        existingEvent.Status = ResolveStatus(chapter.Status);
        existingEvent.Description = summary;
        existingEvent.Impact = BuildImpact(syncResult);
        existingEvent.Participants = participants;
        existingEvent.ParticipantCount = participants.Count;

        await _timelineDataService.SaveTimelineEventsAsync(projectId, events);
        return 1;
    }

    private static List<EventParticipantViewModel> BuildParticipants(ChapterContentSyncResult syncResult)
    {
        var participants = new List<EventParticipantViewModel>();

        participants.AddRange(syncResult.UpdatedCharacterNames.Select(name => new EventParticipantViewModel
        {
            Name = name,
            Type = "角色",
            Role = "章节参与者"
        }));

        participants.AddRange(syncResult.UpdatedFactionNames.Select(name => new EventParticipantViewModel
        {
            Name = name,
            Type = "势力",
            Role = "章节关联势力"
        }));

        return participants
            .GroupBy(item => $"{item.Type}:{item.Name}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

    private static string BuildImpact(ChapterContentSyncResult syncResult)
    {
        return string.Join(
            "；",
            new[]
            {
                $"人物 {syncResult.UpdatedCharacterCount}",
                $"势力 {syncResult.UpdatedFactionCount}",
                $"剧情 {syncResult.UpdatedPlotCount}",
                $"设定 {syncResult.UpdatedWorldSettingCount}",
                $"人物关系 {syncResult.UpdatedCharacterRelationshipCount}",
                $"势力关系 {syncResult.UpdatedFactionRelationshipCount}"
            });
    }

    private static string BuildChapterMarker(Chapter chapter)
    {
        return $"第{chapter.Order}章《{chapter.Title}》";
    }

    private static string BuildSummaryText(Chapter chapter)
    {
        if (!string.IsNullOrWhiteSpace(chapter.Summary))
        {
            return chapter.Summary.Trim();
        }

        if (!string.IsNullOrWhiteSpace(chapter.Content))
        {
            var content = chapter.Content.Trim();
            return content.Length > 120 ? content[..120] : content;
        }

        return "章节内容已更新";
    }

    private static string ResolveStatus(string? chapterStatus)
    {
        if (string.IsNullOrWhiteSpace(chapterStatus))
        {
            return "进行中";
        }

        return chapterStatus.Contains("完成", StringComparison.OrdinalIgnoreCase)
            ? "已完成"
            : "进行中";
    }

    private static string ResolveImportance(ChapterContentSyncResult syncResult)
    {
        var totalUpdates =
            syncResult.UpdatedCharacterCount +
            syncResult.UpdatedFactionCount +
            syncResult.UpdatedPlotCount +
            syncResult.UpdatedWorldSettingCount +
            syncResult.UpdatedCharacterRelationshipCount +
            syncResult.UpdatedFactionRelationshipCount;

        return totalUpdates switch
        {
            >= 8 => "极高",
            >= 5 => "高",
            >= 2 => "中",
            _ => "低"
        };
    }
}

public sealed class ChapterUpdateWorkflowResult
{
    public ChapterContentSyncResult? SyncResult { get; init; }

    public int UpdatedTimelineEventCount { get; init; }
}
