using Microsoft.EntityFrameworkCore;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;
using NovelManagement.Infrastructure.Data;
using NovelManagement.Infrastructure.Data.Repositories;

namespace NovelManagement.Infrastructure.Repositories;

/// <summary>
/// 秘境仓储实现
/// </summary>
public class SecretRealmRepository : BaseRepository<SecretRealm>, ISecretRealmRepository
{
    public SecretRealmRepository(NovelManagementDbContext context) : base(context)
    {
    }

    /// <summary>
    /// 根据项目ID获取秘境列表
    /// </summary>
    public async Task<IEnumerable<SecretRealm>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await Context.SecretRealms
            .Where(sr => sr.ProjectId == projectId)
            .OrderBy(sr => sr.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据类型获取秘境列表
    /// </summary>
    public async Task<IEnumerable<SecretRealm>> GetByTypeAsync(Guid projectId, string type, CancellationToken cancellationToken = default)
    {
        return await Context.SecretRealms
            .Where(sr => sr.ProjectId == projectId && sr.Type == type)
            .OrderBy(sr => sr.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据状态获取秘境列表
    /// </summary>
    public async Task<IEnumerable<SecretRealm>> GetByStatusAsync(Guid projectId, string status, CancellationToken cancellationToken = default)
    {
        return await Context.SecretRealms
            .Where(sr => sr.ProjectId == projectId && sr.Status == status)
            .OrderBy(sr => sr.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据发现者势力获取秘境列表
    /// </summary>
    public async Task<IEnumerable<SecretRealm>> GetByDiscovererFactionAsync(Guid factionId, CancellationToken cancellationToken = default)
    {
        return await Context.SecretRealms
            .Where(sr => sr.DiscovererFactionId == factionId)
            .OrderBy(sr => sr.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据位置获取秘境列表
    /// </summary>
    public async Task<IEnumerable<SecretRealm>> GetByLocationAsync(Guid projectId, string location, CancellationToken cancellationToken = default)
    {
        return await Context.SecretRealms
            .Where(sr => sr.ProjectId == projectId && sr.Location!.Contains(location))
            .OrderBy(sr => sr.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据推荐修为获取秘境列表
    /// </summary>
    public async Task<IEnumerable<SecretRealm>> GetByRecommendedCultivationAsync(Guid projectId, string recommendedCultivation, CancellationToken cancellationToken = default)
    {
        return await Context.SecretRealms
            .Where(sr => sr.ProjectId == projectId && sr.RecommendedCultivation == recommendedCultivation)
            .OrderBy(sr => sr.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取秘境及其相关资源
    /// </summary>
    public async Task<SecretRealm?> GetWithResourcesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Context.SecretRealms
            .Include(sr => sr.RelatedResources)
            .FirstOrDefaultAsync(sr => sr.Id == id, cancellationToken);
    }

    /// <summary>
    /// 获取秘境及其探索者
    /// </summary>
    public async Task<SecretRealm?> GetWithExplorersAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Context.SecretRealms
            .Include(sr => sr.DiscovererFaction)
            .FirstOrDefaultAsync(sr => sr.Id == id, cancellationToken);
    }

    /// <summary>
    /// 根据危险等级范围获取秘境列表
    /// </summary>
    public async Task<IEnumerable<SecretRealm>> GetByDangerLevelRangeAsync(Guid projectId, int minDangerLevel, int maxDangerLevel, CancellationToken cancellationToken = default)
    {
        return await Context.SecretRealms
            .Where(sr => sr.ProjectId == projectId && sr.DangerLevel >= minDangerLevel && sr.DangerLevel <= maxDangerLevel)
            .OrderByDescending(sr => sr.DangerLevel)
            .ThenBy(sr => sr.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据重要性获取秘境列表
    /// </summary>
    public async Task<IEnumerable<SecretRealm>> GetByImportanceAsync(Guid projectId, int minImportance, CancellationToken cancellationToken = default)
    {
        return await Context.SecretRealms
            .Where(sr => sr.ProjectId == projectId && sr.Importance >= minImportance)
            .OrderByDescending(sr => sr.Importance)
            .ThenBy(sr => sr.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据探索次数范围获取秘境列表
    /// </summary>
    public async Task<IEnumerable<SecretRealm>> GetByExplorationCountRangeAsync(Guid projectId, int minCount, int maxCount, CancellationToken cancellationToken = default)
    {
        return await Context.SecretRealms
            .Where(sr => sr.ProjectId == projectId && sr.ExplorationCount >= minCount && sr.ExplorationCount <= maxCount)
            .OrderByDescending(sr => sr.ExplorationCount)
            .ThenBy(sr => sr.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 搜索秘境
    /// </summary>
    public async Task<IEnumerable<SecretRealm>> SearchAsync(Guid projectId, string searchTerm, CancellationToken cancellationToken = default)
    {
        return await Context.SecretRealms
            .Where(sr => sr.ProjectId == projectId &&
                        (sr.Name.Contains(searchTerm) ||
                         sr.Description!.Contains(searchTerm) ||
                         sr.Location!.Contains(searchTerm) ||
                         sr.RecommendedCultivation!.Contains(searchTerm) ||
                         sr.Tags!.Contains(searchTerm)))
            .OrderBy(sr => sr.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取秘境探索统计
    /// </summary>
    public async Task<Dictionary<string, object>> GetExplorationStatisticsAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var totalCount = await Context.SecretRealms.CountAsync(sr => sr.ProjectId == projectId, cancellationToken);
        
        var typeStats = await Context.SecretRealms
            .Where(sr => sr.ProjectId == projectId)
            .GroupBy(sr => sr.Type)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Type, x => x.Count, cancellationToken);

        var statusStats = await Context.SecretRealms
            .Where(sr => sr.ProjectId == projectId)
            .GroupBy(sr => sr.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);

        var avgDangerLevel = await Context.SecretRealms
            .Where(sr => sr.ProjectId == projectId)
            .AverageAsync(sr => (double?)sr.DangerLevel, cancellationToken) ?? 0;

        var exploredCount = await Context.SecretRealms
            .CountAsync(sr => sr.ProjectId == projectId && sr.Status == "已探索", cancellationToken);

        return new Dictionary<string, object>
        {
            ["TotalCount"] = totalCount,
            ["TypeStatistics"] = typeStats,
            ["StatusStatistics"] = statusStats,
            ["AverageDangerLevel"] = avgDangerLevel,
            ["ExploredCount"] = exploredCount,
            ["ExplorationRate"] = totalCount > 0 ? (double)exploredCount / totalCount * 100 : 0
        };
    }
}
