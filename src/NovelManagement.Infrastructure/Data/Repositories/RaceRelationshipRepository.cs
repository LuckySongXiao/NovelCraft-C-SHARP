using Microsoft.EntityFrameworkCore;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;

namespace NovelManagement.Infrastructure.Data.Repositories;

/// <summary>
/// 种族关系仓储实现
/// </summary>
public class RaceRelationshipRepository : BaseRepository<RaceRelationship>, IRaceRelationshipRepository
{
    public RaceRelationshipRepository(NovelManagementDbContext context) : base(context)
    {
    }

    /// <summary>
    /// 根据项目ID获取种族关系列表
    /// </summary>
    public async Task<IEnumerable<RaceRelationship>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await Context.RaceRelationships
            .Where(rr => rr.ProjectId == projectId)
            .Include(rr => rr.SourceRace)
            .Include(rr => rr.TargetRace)
            .OrderBy(rr => rr.SourceRace.Name)
            .ThenBy(rr => rr.TargetRace.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据种族获取关系列表
    /// </summary>
    public async Task<IEnumerable<RaceRelationship>> GetByRaceAsync(Guid raceId, CancellationToken cancellationToken = default)
    {
        return await Context.RaceRelationships
            .Where(rr => rr.SourceRaceId == raceId || rr.TargetRaceId == raceId)
            .Include(rr => rr.SourceRace)
            .Include(rr => rr.TargetRace)
            .OrderBy(rr => rr.RelationshipType)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据关系类型获取关系列表
    /// </summary>
    public async Task<IEnumerable<RaceRelationship>> GetByTypeAsync(Guid projectId, string type, CancellationToken cancellationToken = default)
    {
        return await Context.RaceRelationships
            .Where(rr => rr.ProjectId == projectId && rr.RelationshipType == type)
            .Include(rr => rr.SourceRace)
            .Include(rr => rr.TargetRace)
            .OrderBy(rr => rr.SourceRace.Name)
            .ThenBy(rr => rr.TargetRace.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据状态获取关系列表
    /// </summary>
    public async Task<IEnumerable<RaceRelationship>> GetByStatusAsync(Guid projectId, string status, CancellationToken cancellationToken = default)
    {
        return await Context.RaceRelationships
            .Where(rr => rr.ProjectId == projectId && rr.Status == status)
            .Include(rr => rr.SourceRace)
            .Include(rr => rr.TargetRace)
            .OrderBy(rr => rr.SourceRace.Name)
            .ThenBy(rr => rr.TargetRace.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据重要性获取关系列表
    /// </summary>
    public async Task<IEnumerable<RaceRelationship>> GetByImportanceAsync(Guid projectId, int minImportance, CancellationToken cancellationToken = default)
    {
        return await Context.RaceRelationships
            .Where(rr => rr.ProjectId == projectId && rr.Importance >= minImportance)
            .Include(rr => rr.SourceRace)
            .Include(rr => rr.TargetRace)
            .OrderByDescending(rr => rr.Importance)
            .ThenBy(rr => rr.SourceRace.Name)
            .ThenBy(rr => rr.TargetRace.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 搜索种族关系
    /// </summary>
    public async Task<IEnumerable<RaceRelationship>> SearchAsync(Guid projectId, string searchTerm, CancellationToken cancellationToken = default)
    {
        return await Context.RaceRelationships
            .Where(rr => rr.ProjectId == projectId &&
                        (rr.Description!.Contains(searchTerm) ||
                         rr.SourceRace.Name.Contains(searchTerm) ||
                         rr.TargetRace.Name.Contains(searchTerm) ||
                         rr.Tags!.Contains(searchTerm)))
            .Include(rr => rr.SourceRace)
            .Include(rr => rr.TargetRace)
            .OrderBy(rr => rr.SourceRace.Name)
            .ThenBy(rr => rr.TargetRace.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取种族关系统计
    /// </summary>
    public async Task<Dictionary<string, int>> GetRelationshipStatisticsAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var typeStats = await Context.RaceRelationships
            .Where(rr => rr.ProjectId == projectId)
            .GroupBy(rr => rr.RelationshipType)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Type, x => x.Count, cancellationToken);

        return typeStats;
    }

    /// <summary>
    /// 根据源种族获取关系列表
    /// </summary>
    public async Task<IEnumerable<RaceRelationship>> GetBySourceRaceAsync(Guid sourceRaceId, CancellationToken cancellationToken = default)
    {
        return await Context.RaceRelationships
            .Where(rr => rr.SourceRaceId == sourceRaceId)
            .Include(rr => rr.SourceRace)
            .Include(rr => rr.TargetRace)
            .OrderBy(rr => rr.RelationshipType)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据目标种族获取关系列表
    /// </summary>
    public async Task<IEnumerable<RaceRelationship>> GetByTargetRaceAsync(Guid targetRaceId, CancellationToken cancellationToken = default)
    {
        return await Context.RaceRelationships
            .Where(rr => rr.TargetRaceId == targetRaceId)
            .Include(rr => rr.SourceRace)
            .Include(rr => rr.TargetRace)
            .OrderBy(rr => rr.RelationshipType)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据强度范围获取关系列表
    /// </summary>
    public async Task<IEnumerable<RaceRelationship>> GetByStrengthRangeAsync(Guid projectId, int minStrength, int maxStrength, CancellationToken cancellationToken = default)
    {
        return await Context.RaceRelationships
            .Where(rr => rr.ProjectId == projectId && rr.Strength >= minStrength && rr.Strength <= maxStrength)
            .Include(rr => rr.SourceRace)
            .Include(rr => rr.TargetRace)
            .OrderByDescending(rr => rr.Strength)
            .ThenBy(rr => rr.SourceRace.Name)
            .ThenBy(rr => rr.TargetRace.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取两个种族之间的关系
    /// </summary>
    public async Task<RaceRelationship?> GetBetweenRacesAsync(Guid sourceRaceId, Guid targetRaceId, CancellationToken cancellationToken = default)
    {
        return await Context.RaceRelationships
            .Include(rr => rr.SourceRace)
            .Include(rr => rr.TargetRace)
            .FirstOrDefaultAsync(rr => rr.SourceRaceId == sourceRaceId && rr.TargetRaceId == targetRaceId, cancellationToken);
    }

    /// <summary>
    /// 获取种族关系网络数据
    /// </summary>
    public async Task<Dictionary<string, object>> GetNetworkDataAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var relationships = await Context.RaceRelationships
            .Where(rr => rr.ProjectId == projectId)
            .Include(rr => rr.SourceRace)
            .Include(rr => rr.TargetRace)
            .ToListAsync(cancellationToken);

        var nodes = relationships
            .SelectMany(r => new[] { r.SourceRace, r.TargetRace })
            .Distinct()
            .Select(r => new { Id = r.Id, Name = r.Name, Type = r.Type })
            .ToList();

        var edges = relationships
            .Select(r => new {
                Source = r.SourceRaceId,
                Target = r.TargetRaceId,
                Type = r.RelationshipType,
                Strength = r.Strength
            })
            .ToList();

        return new Dictionary<string, object>
        {
            ["Nodes"] = nodes,
            ["Edges"] = edges,
            ["TotalRelationships"] = relationships.Count
        };
    }
}
