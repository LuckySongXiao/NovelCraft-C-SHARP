using NovelManagement.Core.Entities;

namespace NovelManagement.Core.Interfaces;

/// <summary>
/// 时间线事件仓储接口
/// </summary>
public interface ITimelineEventRepository : IRepository<TimelineEvent>
{
    /// <summary>
    /// 获取项目的全部时间线事件（含参与者，按事件日期排序）
    /// </summary>
    Task<IEnumerable<TimelineEvent>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 以只读方式获取项目的全部时间线事件（不做变更跟踪，适合纯展示与检查场景）
    /// </summary>
    Task<IEnumerable<TimelineEvent>> GetByProjectIdReadOnlyAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取项目指定类别的时间线事件
    /// </summary>
    Task<IEnumerable<TimelineEvent>> GetByCategoryAsync(Guid projectId, string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取项目指定日期区间内的时间线事件
    /// </summary>
    Task<IEnumerable<TimelineEvent>> GetByDateRangeAsync(Guid projectId, DateTime start, DateTime end, CancellationToken cancellationToken = default);

    /// <summary>
    /// 返回指定迁移来源标识中已存在于数据库的子集，用于避免重复迁移
    /// </summary>
    Task<List<string>> FilterExistingLegacyIdsAsync(Guid projectId, IEnumerable<string> legacyIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除项目的全部时间线事件（含参与者）
    /// </summary>
    Task DeleteByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 统计项目的事件数量
    /// </summary>
    Task<int> CountByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
}
