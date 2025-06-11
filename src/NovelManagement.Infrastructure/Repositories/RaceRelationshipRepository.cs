using Microsoft.EntityFrameworkCore;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;
using NovelManagement.Infrastructure.Data;
using NovelManagement.Infrastructure.Data.Repositories;

namespace NovelManagement.Infrastructure.Repositories;

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
            .OrderBy(rr => rr.SourceRace!.Name)
            .ThenBy(rr => rr.TargetRace!.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据源种族ID获取关系列表
    /// </summary>
    public async Task<IEnumerable<RaceRelationship>> GetBySourceRaceAsync(Guid sourceRaceId, CancellationToken cancellationToken = default)
    {
        return await Context.RaceRelationships
            .Where(rr => rr.SourceRaceId == sourceRaceId)
            .Include(rr => rr.TargetRace)
            .OrderBy(rr => rr.TargetRace!.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据目标种族ID获取关系列表
    /// </summary>
    public async Task<IEnumerable<RaceRelationship>> GetByTargetRaceAsync(Guid targetRaceId, CancellationToken cancellationToken = default)
    {
        return await Context.RaceRelationships
            .Where(rr => rr.TargetRaceId == targetRaceId)
            .Include(rr => rr.SourceRace)
            .OrderBy(rr => rr.SourceRace!.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据种族ID获取所有相关关系
    /// </summary>
    public async Task<IEnumerable<RaceRelationship>> GetByRaceAsync(Guid raceId, CancellationToken cancellationToken = default)
    {
        return await Context.RaceRelationships
            .Where(rr => rr.SourceRaceId == raceId || rr.TargetRaceId == raceId)
            .Include(rr => rr.SourceRace)
            .Include(rr => rr.TargetRace)
            .OrderBy(rr => rr.RelationshipType)
            .ThenBy(rr => rr.Strength)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据关系类型获取关系列表
    /// </summary>
    public async Task<IEnumerable<RaceRelationship>> GetByTypeAsync(Guid projectId, string relationshipType, CancellationToken cancellationToken = default)
    {
        return await Context.RaceRelationships
            .Where(rr => rr.ProjectId == projectId && rr.RelationshipType == relationshipType)
            .Include(rr => rr.SourceRace)
            .Include(rr => rr.TargetRace)
            .OrderBy(rr => rr.SourceRace!.Name)
            .ThenBy(rr => rr.TargetRace!.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据关系状态获取关系列表
    /// </summary>
    public async Task<IEnumerable<RaceRelationship>> GetByStatusAsync(Guid projectId, string status, CancellationToken cancellationToken = default)
    {
        return await Context.RaceRelationships
            .Where(rr => rr.ProjectId == projectId && rr.Status == status)
            .Include(rr => rr.SourceRace)
            .Include(rr => rr.TargetRace)
            .OrderBy(rr => rr.SourceRace!.Name)
            .ThenBy(rr => rr.TargetRace!.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据关系强度范围获取关系列表
    /// </summary>
    public async Task<IEnumerable<RaceRelationship>> GetByStrengthRangeAsync(Guid projectId, int minStrength, int maxStrength, CancellationToken cancellationToken = default)
    {
        return await Context.RaceRelationships
            .Where(rr => rr.ProjectId == projectId && rr.Strength >= minStrength && rr.Strength <= maxStrength)
            .Include(rr => rr.SourceRace)
            .Include(rr => rr.TargetRace)
            .OrderByDescending(rr => rr.Strength)
            .ThenBy(rr => rr.SourceRace!.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取特定两个种族之间的关系
    /// </summary>
    public async Task<RaceRelationship?> GetBetweenRacesAsync(Guid sourceRaceId, Guid targetRaceId, CancellationToken cancellationToken = default)
    {
        return await Context.RaceRelationships
            .Include(rr => rr.SourceRace)
            .Include(rr => rr.TargetRace)
            .FirstOrDefaultAsync(rr => rr.SourceRaceId == sourceRaceId && rr.TargetRaceId == targetRaceId, cancellationToken);
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
            .ThenBy(rr => rr.SourceRace!.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 搜索种族关系
    /// </summary>
    public async Task<IEnumerable<RaceRelationship>> SearchAsync(Guid projectId, string searchTerm, CancellationToken cancellationToken = default)
    {
        return await Context.RaceRelationships
            .Where(rr => rr.ProjectId == projectId && 
                        (rr.RelationshipType.Contains(searchTerm) || 
                         rr.Description!.Contains(searchTerm) ||
                         rr.SourceRace!.Name.Contains(searchTerm) ||
                         rr.TargetRace!.Name.Contains(searchTerm) ||
                         rr.Tags!.Contains(searchTerm)))
            .Include(rr => rr.SourceRace)
            .Include(rr => rr.TargetRace)
            .OrderBy(rr => rr.SourceRace!.Name)
            .ThenBy(rr => rr.TargetRace!.Name)
            .ToListAsync(cancellationToken);
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

        var typeStats = relationships
            .GroupBy(rr => rr.RelationshipType)
            .ToDictionary(g => g.Key, g => g.Count());

        var statusStats = relationships
            .GroupBy(rr => rr.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        var avgStrength = relationships.Any() ? relationships.Average(rr => rr.Strength) : 0;

        var nodes = relationships
            .SelectMany(rr => new[] { rr.SourceRace, rr.TargetRace })
            .Where(r => r != null)
            .Distinct()
            .Select(r => new { Id = r!.Id, Name = r.Name })
            .ToList();

        var edges = relationships
            .Select(rr => new 
            { 
                Source = rr.SourceRaceId, 
                Target = rr.TargetRaceId, 
                Type = rr.RelationshipType,
                Strength = rr.Strength,
                Status = rr.Status
            })
            .ToList();

        return new Dictionary<string, object>
        {
            ["TotalRelationships"] = relationships.Count,
            ["TypeStatistics"] = typeStats,
            ["StatusStatistics"] = statusStats,
            ["AverageStrength"] = avgStrength,
            ["NetworkNodes"] = nodes,
            ["NetworkEdges"] = edges
        };
    }
}
