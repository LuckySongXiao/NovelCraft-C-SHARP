using Microsoft.EntityFrameworkCore;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;

namespace NovelManagement.Infrastructure.Data.Repositories;

/// <summary>
/// 时间线事件仓储实现
/// </summary>
public class TimelineEventRepository : BaseRepository<TimelineEvent>, ITimelineEventRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="context">数据库上下文</param>
    public TimelineEventRepository(NovelManagementDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TimelineEvent>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(e => e.ProjectId == projectId)
            .Include(e => e.Participants)
            .OrderBy(e => e.EventDate)
            .ThenBy(e => e.DisplayOrder)
            .ThenBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TimelineEvent>> GetByProjectIdReadOnlyAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(e => e.ProjectId == projectId)
            .Include(e => e.Participants)
            .OrderBy(e => e.EventDate)
            .ThenBy(e => e.DisplayOrder)
            .ThenBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TimelineEvent>> GetByCategoryAsync(Guid projectId, string category, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(e => e.ProjectId == projectId && e.Category == category)
            .Include(e => e.Participants)
            .OrderBy(e => e.EventDate)
            .ThenBy(e => e.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TimelineEvent>> GetByDateRangeAsync(Guid projectId, DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(e => e.ProjectId == projectId && e.EventDate >= start && e.EventDate <= end)
            .Include(e => e.Participants)
            .OrderBy(e => e.EventDate)
            .ThenBy(e => e.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<string>> FilterExistingLegacyIdsAsync(Guid projectId, IEnumerable<string> legacyIds, CancellationToken cancellationToken = default)
    {
        var ids = legacyIds?.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList() ?? new List<string>();
        if (ids.Count == 0)
        {
            return new List<string>();
        }

        return await DbSet
            .AsNoTracking()
            .Where(e => e.ProjectId == projectId && e.LegacyId != null && ids.Contains(e.LegacyId))
            .Select(e => e.LegacyId!)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var events = await DbSet
            .Where(e => e.ProjectId == projectId)
            .Include(e => e.Participants)
            .ToListAsync(cancellationToken);

        if (events.Count == 0)
        {
            return;
        }

        foreach (var timelineEvent in events)
        {
            if (timelineEvent.Participants.Count > 0)
            {
                Context.TimelineEventParticipants.RemoveRange(timelineEvent.Participants);
            }
        }

        DbSet.RemoveRange(events);
    }

    /// <inheritdoc />
    public async Task<int> CountByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await DbSet.CountAsync(e => e.ProjectId == projectId, cancellationToken);
    }
}
