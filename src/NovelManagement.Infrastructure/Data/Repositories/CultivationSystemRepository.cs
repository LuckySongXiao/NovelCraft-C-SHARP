using Microsoft.EntityFrameworkCore;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;

namespace NovelManagement.Infrastructure.Data.Repositories;

/// <summary>
/// 修炼体系仓储实现
/// </summary>
public class CultivationSystemRepository : BaseRepository<CultivationSystem>, ICultivationSystemRepository
{
    public CultivationSystemRepository(NovelManagementDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<CultivationSystem>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await Context.CultivationSystems
            .Where(cs => cs.ProjectId == projectId)
            .OrderBy(cs => cs.Order)
            .ThenBy(cs => cs.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CultivationSystem>> GetByTypeAsync(Guid projectId, string type, CancellationToken cancellationToken = default)
    {
        return await Context.CultivationSystems
            .Where(cs => cs.ProjectId == projectId && cs.Type == type)
            .OrderBy(cs => cs.Order)
            .ThenBy(cs => cs.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<CultivationSystem?> GetWithLevelsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Context.CultivationSystems
            .Include(cs => cs.Levels.OrderBy(l => l.Order))
            .FirstOrDefaultAsync(cs => cs.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<CultivationSystem>> GetByImportanceAsync(Guid projectId, int minImportance, CancellationToken cancellationToken = default)
    {
        return await Context.CultivationSystems
            .Where(cs => cs.ProjectId == projectId && cs.Importance >= minImportance)
            .OrderByDescending(cs => cs.Importance)
            .ThenBy(cs => cs.Order)
            .ThenBy(cs => cs.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<CultivationSystem?> GetByCharacterAsync(Guid characterId, CancellationToken cancellationToken = default)
    {
        // 这里假设角色通过某种方式关联到修炼体系，可能需要根据实际业务逻辑调整
        // 暂时返回null，需要根据具体的业务关联关系来实现
        return await Task.FromResult<CultivationSystem?>(null);
    }

    public async Task<IEnumerable<CultivationSystem>> GetByDifficultyRangeAsync(Guid projectId, int minDifficulty, int maxDifficulty, CancellationToken cancellationToken = default)
    {
        return await Context.CultivationSystems
            .Where(cs => cs.ProjectId == projectId && cs.Difficulty >= minDifficulty && cs.Difficulty <= maxDifficulty)
            .OrderBy(cs => cs.Difficulty)
            .ThenBy(cs => cs.Order)
            .ThenBy(cs => cs.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CultivationSystem>> SearchAsync(Guid projectId, string searchTerm, CancellationToken cancellationToken = default)
    {
        var lowerSearchTerm = searchTerm.ToLower();
        return await Context.CultivationSystems
            .Where(cs => cs.ProjectId == projectId &&
                        (cs.Name.ToLower().Contains(lowerSearchTerm) ||
                         (cs.Description != null && cs.Description.ToLower().Contains(lowerSearchTerm)) ||
                         (cs.CultivationMethod != null && cs.CultivationMethod.ToLower().Contains(lowerSearchTerm)) ||
                         (cs.Tags != null && cs.Tags.ToLower().Contains(lowerSearchTerm))))
            .OrderByDescending(cs => cs.Importance)
            .ThenBy(cs => cs.Order)
            .ThenBy(cs => cs.Name)
            .ToListAsync(cancellationToken);
    }
}

/// <summary>
/// 修炼等级仓储实现
/// </summary>
public class CultivationLevelRepository : BaseRepository<CultivationLevel>, ICultivationLevelRepository
{
    public CultivationLevelRepository(NovelManagementDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<CultivationLevel>> GetBySystemIdAsync(Guid cultivationSystemId, CancellationToken cancellationToken = default)
    {
        return await Context.CultivationLevels
            .Where(cl => cl.CultivationSystemId == cultivationSystemId)
            .OrderBy(cl => cl.Order)
            .ToListAsync(cancellationToken);
    }

    public async Task<CultivationLevel?> GetByOrderIndexAsync(Guid cultivationSystemId, int orderIndex, CancellationToken cancellationToken = default)
    {
        return await Context.CultivationLevels
            .FirstOrDefaultAsync(cl => cl.CultivationSystemId == cultivationSystemId && cl.Order == orderIndex, cancellationToken);
    }

    public async Task<CultivationLevel?> GetNextLevelAsync(Guid currentLevelId, CancellationToken cancellationToken = default)
    {
        var currentLevel = await Context.CultivationLevels
            .FirstOrDefaultAsync(cl => cl.Id == currentLevelId, cancellationToken);

        if (currentLevel == null)
            return null;

        return await Context.CultivationLevels
            .Where(cl => cl.CultivationSystemId == currentLevel.CultivationSystemId && cl.Order > currentLevel.Order)
            .OrderBy(cl => cl.Order)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CultivationLevel?> GetPreviousLevelAsync(Guid currentLevelId, CancellationToken cancellationToken = default)
    {
        var currentLevel = await Context.CultivationLevels
            .FirstOrDefaultAsync(cl => cl.Id == currentLevelId, cancellationToken);

        if (currentLevel == null)
            return null;

        return await Context.CultivationLevels
            .Where(cl => cl.CultivationSystemId == currentLevel.CultivationSystemId && cl.Order < currentLevel.Order)
            .OrderByDescending(cl => cl.Order)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
