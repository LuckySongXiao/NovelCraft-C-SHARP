using Microsoft.EntityFrameworkCore;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;

namespace NovelManagement.Infrastructure.Data.Repositories;

/// <summary>
/// 种族仓储实现
/// </summary>
public class RaceRepository : BaseRepository<Race>, IRaceRepository
{
    public RaceRepository(NovelManagementDbContext context) : base(context)
    {
    }

    /// <summary>
    /// 根据项目ID获取种族列表
    /// </summary>
    public async Task<IEnumerable<Race>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await Context.Races
            .Where(r => r.ProjectId == projectId)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据类型获取种族列表
    /// </summary>
    public async Task<IEnumerable<Race>> GetByTypeAsync(Guid projectId, string type, CancellationToken cancellationToken = default)
    {
        return await Context.Races
            .Where(r => r.ProjectId == projectId && r.Type == type)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据主要领土获取种族列表
    /// </summary>
    public async Task<IEnumerable<Race>> GetByHabitatAsync(Guid projectId, string habitat, CancellationToken cancellationToken = default)
    {
        return await Context.Races
            .Where(r => r.ProjectId == projectId && r.MainTerritory!.Contains(habitat))
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据种族类型获取种族列表
    /// </summary>
    public async Task<IEnumerable<Race>> GetByIntelligenceLevelAsync(Guid projectId, string intelligenceLevel, CancellationToken cancellationToken = default)
    {
        return await Context.Races
            .Where(r => r.ProjectId == projectId && r.Type == intelligenceLevel)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据状态获取种族列表
    /// </summary>
    public async Task<IEnumerable<Race>> GetByStatusAsync(Guid projectId, string status, CancellationToken cancellationToken = default)
    {
        return await Context.Races
            .Where(r => r.ProjectId == projectId && r.Status == status)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取种族及其成员
    /// </summary>
    public async Task<Race?> GetWithMembersAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Context.Races
            .Include(r => r.Members)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    /// <summary>
    /// 根据重要性获取种族列表
    /// </summary>
    public async Task<IEnumerable<Race>> GetByImportanceAsync(Guid projectId, int minImportance, CancellationToken cancellationToken = default)
    {
        return await Context.Races
            .Where(r => r.ProjectId == projectId && r.Importance >= minImportance)
            .OrderByDescending(r => r.Importance)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 搜索种族
    /// </summary>
    public async Task<IEnumerable<Race>> SearchAsync(Guid projectId, string searchTerm, CancellationToken cancellationToken = default)
    {
        return await Context.Races
            .Where(r => r.ProjectId == projectId &&
                       (r.Name.Contains(searchTerm) ||
                        r.Characteristics!.Contains(searchTerm) ||
                        r.MainTerritory!.Contains(searchTerm) ||
                        r.Tags!.Contains(searchTerm)))
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取种族分布统计
    /// </summary>
    public async Task<Dictionary<string, int>> GetDistributionStatisticsAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var typeStats = await Context.Races
            .Where(r => r.ProjectId == projectId)
            .GroupBy(r => r.Type)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Type, x => x.Count, cancellationToken);

        return typeStats;
    }

    /// <summary>
    /// 获取种族及其关系
    /// </summary>
    public async Task<Race?> GetWithRelationshipsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Context.Races
            .Include(r => r.AllyRelationshipsAsSource)
            .Include(r => r.AllyRelationshipsAsTarget)
            .Include(r => r.EnemyRelationshipsAsSource)
            .Include(r => r.EnemyRelationshipsAsTarget)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    /// <summary>
    /// 根据力量等级范围获取种族列表
    /// </summary>
    public async Task<IEnumerable<Race>> GetByPowerLevelRangeAsync(Guid projectId, int minPowerLevel, int maxPowerLevel, CancellationToken cancellationToken = default)
    {
        return await Context.Races
            .Where(r => r.ProjectId == projectId && r.PowerLevel >= minPowerLevel && r.PowerLevel <= maxPowerLevel)
            .OrderByDescending(r => r.PowerLevel)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据影响力范围获取种族列表
    /// </summary>
    public async Task<IEnumerable<Race>> GetByInfluenceRangeAsync(Guid projectId, int minInfluence, int maxInfluence, CancellationToken cancellationToken = default)
    {
        return await Context.Races
            .Where(r => r.ProjectId == projectId && r.Influence >= minInfluence && r.Influence <= maxInfluence)
            .OrderByDescending(r => r.Influence)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取种族统计信息
    /// </summary>
    public async Task<Dictionary<string, object>> GetStatisticsAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var totalCount = await Context.Races.CountAsync(r => r.ProjectId == projectId, cancellationToken);

        var typeStats = await Context.Races
            .Where(r => r.ProjectId == projectId)
            .GroupBy(r => r.Type)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Type, x => x.Count, cancellationToken);

        var avgPowerLevel = await Context.Races
            .Where(r => r.ProjectId == projectId)
            .AverageAsync(r => (double?)r.PowerLevel, cancellationToken) ?? 0;

        var avgInfluence = await Context.Races
            .Where(r => r.ProjectId == projectId)
            .AverageAsync(r => (double?)r.Influence, cancellationToken) ?? 0;

        return new Dictionary<string, object>
        {
            ["TotalCount"] = totalCount,
            ["TypeStatistics"] = typeStats,
            ["AveragePowerLevel"] = avgPowerLevel,
            ["AverageInfluence"] = avgInfluence
        };
    }

    /// <summary>
    /// 根据名称获取种族
    /// </summary>
    public async Task<Race?> GetByNameAsync(Guid projectId, string name, CancellationToken cancellationToken = default)
    {
        return await Context.Races
            .FirstOrDefaultAsync(r => r.ProjectId == projectId && r.Name == name, cancellationToken);
    }
}
