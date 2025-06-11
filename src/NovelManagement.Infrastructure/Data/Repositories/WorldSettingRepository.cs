using Microsoft.EntityFrameworkCore;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;

namespace NovelManagement.Infrastructure.Data.Repositories;

/// <summary>
/// 世界设定仓储实现
/// </summary>
public class WorldSettingRepository : BaseRepository<WorldSetting>, IWorldSettingRepository
{
    public WorldSettingRepository(NovelManagementDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<WorldSetting>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await Context.WorldSettings
            .Where(ws => ws.ProjectId == projectId)
            .OrderBy(ws => ws.Order)
            .ThenBy(ws => ws.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WorldSetting>> GetByTypeAsync(Guid projectId, string type, CancellationToken cancellationToken = default)
    {
        return await Context.WorldSettings
            .Where(ws => ws.ProjectId == projectId && ws.Type == type)
            .OrderBy(ws => ws.Order)
            .ThenBy(ws => ws.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WorldSetting>> GetByCategoryAsync(Guid projectId, string category, CancellationToken cancellationToken = default)
    {
        return await Context.WorldSettings
            .Where(ws => ws.ProjectId == projectId && ws.Category == category)
            .OrderBy(ws => ws.Order)
            .ThenBy(ws => ws.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<WorldSetting?> GetWithChildrenAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Context.WorldSettings
            .Include(ws => ws.Children)
            .FirstOrDefaultAsync(ws => ws.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<WorldSetting>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken = default)
    {
        return await Context.WorldSettings
            .Where(ws => ws.ParentId == parentId)
            .OrderBy(ws => ws.Order)
            .ThenBy(ws => ws.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WorldSetting>> GetRootSettingsAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await Context.WorldSettings
            .Where(ws => ws.ProjectId == projectId && ws.ParentId == null)
            .OrderBy(ws => ws.Order)
            .ThenBy(ws => ws.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WorldSetting>> GetByImportanceAsync(Guid projectId, int minImportance, CancellationToken cancellationToken = default)
    {
        return await Context.WorldSettings
            .Where(ws => ws.ProjectId == projectId && ws.Importance >= minImportance)
            .OrderByDescending(ws => ws.Importance)
            .ThenBy(ws => ws.Order)
            .ThenBy(ws => ws.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WorldSetting>> SearchAsync(Guid projectId, string searchTerm, CancellationToken cancellationToken = default)
    {
        var lowerSearchTerm = searchTerm.ToLower();
        return await Context.WorldSettings
            .Where(ws => ws.ProjectId == projectId && 
                        (ws.Name.ToLower().Contains(lowerSearchTerm) ||
                         (ws.Description != null && ws.Description.ToLower().Contains(lowerSearchTerm)) ||
                         (ws.Content != null && ws.Content.ToLower().Contains(lowerSearchTerm)) ||
                         (ws.Tags != null && ws.Tags.ToLower().Contains(lowerSearchTerm))))
            .OrderByDescending(ws => ws.Importance)
            .ThenBy(ws => ws.Order)
            .ThenBy(ws => ws.Name)
            .ToListAsync(cancellationToken);
    }
}
