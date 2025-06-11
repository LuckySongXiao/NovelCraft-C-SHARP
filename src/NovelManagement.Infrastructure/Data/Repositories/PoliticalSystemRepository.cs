using Microsoft.EntityFrameworkCore;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;

namespace NovelManagement.Infrastructure.Data.Repositories;

/// <summary>
/// 政治体系仓储实现
/// </summary>
public class PoliticalSystemRepository : BaseRepository<PoliticalSystem>, IPoliticalSystemRepository
{
    public PoliticalSystemRepository(NovelManagementDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<PoliticalSystem>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await Context.PoliticalSystems
            .Where(ps => ps.ProjectId == projectId)
            .OrderBy(ps => ps.Order)
            .ThenBy(ps => ps.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PoliticalSystem>> GetByTypeAsync(Guid projectId, string type, CancellationToken cancellationToken = default)
    {
        return await Context.PoliticalSystems
            .Where(ps => ps.ProjectId == projectId && ps.Type == type)
            .OrderBy(ps => ps.Order)
            .ThenBy(ps => ps.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<PoliticalSystem?> GetWithPositionsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Context.PoliticalSystems
            .Include(ps => ps.Positions.OrderBy(p => p.Level))
            .FirstOrDefaultAsync(ps => ps.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<PoliticalSystem>> GetByImportanceAsync(Guid projectId, int minImportance, CancellationToken cancellationToken = default)
    {
        return await Context.PoliticalSystems
            .Where(ps => ps.ProjectId == projectId && ps.Importance >= minImportance)
            .OrderByDescending(ps => ps.Importance)
            .ThenBy(ps => ps.Order)
            .ThenBy(ps => ps.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PoliticalSystem>> GetByRulingFactionAsync(Guid factionId, CancellationToken cancellationToken = default)
    {
        // 这里假设政治体系通过某种方式关联到统治势力，可能需要根据实际业务逻辑调整
        // 暂时返回空列表，需要根据具体的业务关联关系来实现
        return await Task.FromResult(new List<PoliticalSystem>());
    }

    public async Task<IEnumerable<PoliticalSystem>> GetByStabilityRangeAsync(Guid projectId, int minStability, int maxStability, CancellationToken cancellationToken = default)
    {
        return await Context.PoliticalSystems
            .Where(ps => ps.ProjectId == projectId && ps.Stability >= minStability && ps.Stability <= maxStability)
            .OrderByDescending(ps => ps.Stability)
            .ThenBy(ps => ps.Order)
            .ThenBy(ps => ps.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PoliticalSystem>> GetByInfluenceRangeAsync(Guid projectId, int minInfluence, int maxInfluence, CancellationToken cancellationToken = default)
    {
        return await Context.PoliticalSystems
            .Where(ps => ps.ProjectId == projectId && ps.Influence >= minInfluence && ps.Influence <= maxInfluence)
            .OrderByDescending(ps => ps.Influence)
            .ThenBy(ps => ps.Order)
            .ThenBy(ps => ps.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<PoliticalSystem?> GetWithRulingFactionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // 这里假设政治体系有统治势力的导航属性，可能需要根据实际业务逻辑调整
        // 暂时返回基本的政治体系对象，需要根据具体的业务关联关系来实现
        return await Context.PoliticalSystems
            .FirstOrDefaultAsync(ps => ps.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<PoliticalSystem>> SearchAsync(Guid projectId, string searchTerm, CancellationToken cancellationToken = default)
    {
        var lowerSearchTerm = searchTerm.ToLower();
        return await Context.PoliticalSystems
            .Where(ps => ps.ProjectId == projectId &&
                        (ps.Name.ToLower().Contains(lowerSearchTerm) ||
                         (ps.Description != null && ps.Description.ToLower().Contains(lowerSearchTerm)) ||
                         (ps.Structure != null && ps.Structure.ToLower().Contains(lowerSearchTerm)) ||
                         (ps.Tags != null && ps.Tags.ToLower().Contains(lowerSearchTerm))))
            .OrderByDescending(ps => ps.Importance)
            .ThenBy(ps => ps.Order)
            .ThenBy(ps => ps.Name)
            .ToListAsync(cancellationToken);
    }
}

/// <summary>
/// 政治职位仓储实现
/// </summary>
public class PoliticalPositionRepository : BaseRepository<PoliticalPosition>, IPoliticalPositionRepository
{
    public PoliticalPositionRepository(NovelManagementDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<PoliticalPosition>> GetBySystemIdAsync(Guid politicalSystemId, CancellationToken cancellationToken = default)
    {
        return await Context.PoliticalPositions
            .Where(pp => pp.PoliticalSystemId == politicalSystemId)
            .OrderBy(pp => pp.Level)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PoliticalPosition>> GetByLevelAsync(Guid politicalSystemId, int level, CancellationToken cancellationToken = default)
    {
        return await Context.PoliticalPositions
            .Where(pp => pp.PoliticalSystemId == politicalSystemId && pp.Level == level)
            .OrderBy(pp => pp.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<PoliticalPosition?> GetHighestLevelAsync(Guid politicalSystemId, CancellationToken cancellationToken = default)
    {
        return await Context.PoliticalPositions
            .Where(pp => pp.PoliticalSystemId == politicalSystemId)
            .OrderByDescending(pp => pp.Level)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PoliticalPosition?> GetLowestLevelAsync(Guid politicalSystemId, CancellationToken cancellationToken = default)
    {
        return await Context.PoliticalPositions
            .Where(pp => pp.PoliticalSystemId == politicalSystemId)
            .OrderBy(pp => pp.Level)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
