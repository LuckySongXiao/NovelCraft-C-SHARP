using System;
using NovelManagement.Application.Services;

namespace NovelManagement.WPF.Services;

/// <summary>
/// 章节内容同步通知服务。
/// 用于在章节保存并完成上下文同步后，通知已打开的管理界面主动刷新。
/// </summary>
public sealed class ChapterContentSyncNotificationService
{
    /// <summary>
    /// 章节内容同步完成事件。
    /// </summary>
    public event EventHandler<ChapterContentSyncedEventArgs>? ChapterContentSynced;

    /// <summary>
    /// 发布章节同步完成通知。
    /// </summary>
    public void Publish(Guid projectId, Guid chapterId, string chapterTitle, ChapterContentSyncResult result)
    {
        ChapterContentSynced?.Invoke(
            this,
            new ChapterContentSyncedEventArgs(projectId, chapterId, chapterTitle, result));
    }
}

/// <summary>
/// 章节内容同步完成事件参数。
/// </summary>
public sealed class ChapterContentSyncedEventArgs : EventArgs
{
    public ChapterContentSyncedEventArgs(
        Guid projectId,
        Guid chapterId,
        string chapterTitle,
        ChapterContentSyncResult result)
    {
        ProjectId = projectId;
        ChapterId = chapterId;
        ChapterTitle = chapterTitle;
        Result = result;
    }

    public Guid ProjectId { get; }

    public Guid ChapterId { get; }

    public string ChapterTitle { get; }

    public ChapterContentSyncResult Result { get; }
}
