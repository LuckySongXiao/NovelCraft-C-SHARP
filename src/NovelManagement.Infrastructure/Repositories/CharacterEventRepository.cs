using Microsoft.EntityFrameworkCore;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;
using NovelManagement.Infrastructure.Data;
using NovelManagement.Infrastructure.Data.Repositories;

namespace NovelManagement.Infrastructure.Repositories;

/// <summary>
/// 角色事件/履历记录仓储实现
/// </summary>
public class CharacterEventRepository : BaseRepository<CharacterEvent>, ICharacterEventRepository
{
    public CharacterEventRepository(NovelManagementDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<CharacterEvent>> GetEventsByCharacterIdAsync(Guid characterId, CancellationToken cancellationToken = default)
    {
        return await Context.CharacterEvents
            .Where(e => e.CharacterId == characterId)
            .OrderBy(e => e.Order)
            .ThenBy(e => e.StoryTime)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<CharacterEvent>> GetEventsByTypeAsync(Guid characterId, string eventType, CancellationToken cancellationToken = default)
    {
        return await Context.CharacterEvents
            .Where(e => e.CharacterId == characterId && e.EventType == eventType)
            .OrderBy(e => e.Order)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<CharacterEvent>> GetRecentEventsAsync(Guid characterId, int count, CancellationToken cancellationToken = default)
    {
        return await Context.CharacterEvents
            .Where(e => e.CharacterId == characterId)
            .OrderByDescending(e => e.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public new async Task AddRangeAsync(IEnumerable<CharacterEvent> events, CancellationToken cancellationToken = default)
    {
        await Context.CharacterEvents.AddRangeAsync(events, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task ReorderEventsAsync(Guid characterId, List<Guid> orderedEventIds, CancellationToken cancellationToken = default)
    {
        var events = await Context.CharacterEvents
            .Where(e => e.CharacterId == characterId)
            .ToListAsync(cancellationToken);

        for (int i = 0; i < orderedEventIds.Count; i++)
        {
            var evt = events.FirstOrDefault(e => e.Id == orderedEventIds[i]);
            if (evt != null)
            {
                evt.Order = i;
            }
        }
    }
}
