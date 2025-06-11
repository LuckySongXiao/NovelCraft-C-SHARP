using Microsoft.EntityFrameworkCore;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;
using NovelManagement.Infrastructure.Data;
using NovelManagement.Infrastructure.Data.Repositories;

namespace NovelManagement.Infrastructure.Repositories;

/// <summary>
/// 资源仓储实现
/// </summary>
public class ResourceRepository : BaseRepository<Resource>, IResourceRepository
{
    public ResourceRepository(NovelManagementDbContext context) : base(context)
    {
    }

    /// <summary>
    /// 根据项目ID获取资源列表
    /// </summary>
    public async Task<IEnumerable<Resource>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await Context.Resources
            .Where(r => r.ProjectId == projectId)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据类型获取资源列表
    /// </summary>
    public async Task<IEnumerable<Resource>> GetByTypeAsync(Guid projectId, string type, CancellationToken cancellationToken = default)
    {
        return await Context.Resources
            .Where(r => r.ProjectId == projectId && r.Type == type)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据稀有度获取资源列表
    /// </summary>
    public async Task<IEnumerable<Resource>> GetByRarityAsync(Guid projectId, string rarity, CancellationToken cancellationToken = default)
    {
        return await Context.Resources
            .Where(r => r.ProjectId == projectId && r.Rarity == rarity)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据状态获取资源列表
    /// </summary>
    public async Task<IEnumerable<Resource>> GetByStatusAsync(Guid projectId, string status, CancellationToken cancellationToken = default)
    {
        return await Context.Resources
            .Where(r => r.ProjectId == projectId && r.Status == status)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据控制势力获取资源列表
    /// </summary>
    public async Task<IEnumerable<Resource>> GetByControllingFactionAsync(Guid factionId, CancellationToken cancellationToken = default)
    {
        return await Context.Resources
            .Where(r => r.ControllingFactionId == factionId)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据位置获取资源列表
    /// </summary>
    public async Task<IEnumerable<Resource>> GetByLocationAsync(Guid projectId, string location, CancellationToken cancellationToken = default)
    {
        return await Context.Resources
            .Where(r => r.ProjectId == projectId && r.Location!.Contains(location))
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据经济价值范围获取资源列表
    /// </summary>
    public async Task<IEnumerable<Resource>> GetByEconomicValueRangeAsync(Guid projectId, int minValue, int maxValue, CancellationToken cancellationToken = default)
    {
        return await Context.Resources
            .Where(r => r.ProjectId == projectId && r.EconomicValue >= minValue && r.EconomicValue <= maxValue)
            .OrderByDescending(r => r.EconomicValue)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据重要性获取资源列表
    /// </summary>
    public async Task<IEnumerable<Resource>> GetByImportanceAsync(Guid projectId, int minImportance, CancellationToken cancellationToken = default)
    {
        return await Context.Resources
            .Where(r => r.ProjectId == projectId && r.Importance >= minImportance)
            .OrderByDescending(r => r.Importance)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 搜索资源
    /// </summary>
    public async Task<IEnumerable<Resource>> SearchAsync(Guid projectId, string searchTerm, CancellationToken cancellationToken = default)
    {
        return await Context.Resources
            .Where(r => r.ProjectId == projectId &&
                       (r.Name.Contains(searchTerm) ||
                        r.Description!.Contains(searchTerm) ||
                        r.Location!.Contains(searchTerm) ||
                        r.Tags!.Contains(searchTerm)))
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取资源分布统计
    /// </summary>
    public async Task<Dictionary<string, int>> GetDistributionStatisticsAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var typeStats = await Context.Resources
            .Where(r => r.ProjectId == projectId)
            .GroupBy(r => r.Type)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Type, x => x.Count, cancellationToken);

        return typeStats;
    }
}
