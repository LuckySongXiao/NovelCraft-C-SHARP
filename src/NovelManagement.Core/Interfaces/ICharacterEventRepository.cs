using NovelManagement.Core.Entities;

namespace NovelManagement.Core.Interfaces;

/// <summary>
/// 角色事件/履历记录仓储接口
/// </summary>
public interface ICharacterEventRepository : IRepository<CharacterEvent>
{
    /// <summary>
    /// 获取角色的所有事件（按时间线排序）
    /// </summary>
    Task<IEnumerable<CharacterEvent>> GetEventsByCharacterIdAsync(Guid characterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色指定类型的事件
    /// </summary>
    Task<IEnumerable<CharacterEvent>> GetEventsByTypeAsync(Guid characterId, string eventType, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色的最新N条事件
    /// </summary>
    Task<IEnumerable<CharacterEvent>> GetRecentEventsAsync(Guid characterId, int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量添加角色事件
    /// </summary>
    Task AddRangeAsync(IEnumerable<CharacterEvent> events, CancellationToken cancellationToken = default);

    /// <summary>
    /// 重新排序角色事件
    /// </summary>
    Task ReorderEventsAsync(Guid characterId, List<Guid> orderedEventIds, CancellationToken cancellationToken = default);
}
