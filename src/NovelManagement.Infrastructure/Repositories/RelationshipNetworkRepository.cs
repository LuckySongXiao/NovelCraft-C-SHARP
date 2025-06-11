using Microsoft.EntityFrameworkCore;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;
using NovelManagement.Infrastructure.Data;
using NovelManagement.Infrastructure.Data.Repositories;

namespace NovelManagement.Infrastructure.Repositories;

/// <summary>
/// 关系网络仓储实现
/// </summary>
public class RelationshipNetworkRepository : BaseRepository<RelationshipNetwork>, IRelationshipNetworkRepository
{
    public RelationshipNetworkRepository(NovelManagementDbContext context) : base(context)
    {
    }

    /// <summary>
    /// 根据项目ID获取关系网络列表
    /// </summary>
    public async Task<IEnumerable<RelationshipNetwork>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await Context.RelationshipNetworks
            .Where(rn => rn.ProjectId == projectId)
            .OrderBy(rn => rn.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据类型获取关系网络列表
    /// </summary>
    public async Task<IEnumerable<RelationshipNetwork>> GetByTypeAsync(Guid projectId, string type, CancellationToken cancellationToken = default)
    {
        return await Context.RelationshipNetworks
            .Where(rn => rn.ProjectId == projectId && rn.Type == type)
            .OrderBy(rn => rn.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据状态获取关系网络列表
    /// </summary>
    public async Task<IEnumerable<RelationshipNetwork>> GetByStatusAsync(Guid projectId, string status, CancellationToken cancellationToken = default)
    {
        return await Context.RelationshipNetworks
            .Where(rn => rn.ProjectId == projectId && rn.Status == status)
            .OrderBy(rn => rn.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据中心人物获取关系网络列表
    /// </summary>
    public async Task<IEnumerable<RelationshipNetwork>> GetByCentralCharacterAsync(Guid centralCharacterId, CancellationToken cancellationToken = default)
    {
        return await Context.RelationshipNetworks
            .Where(rn => rn.CentralCharacterId == centralCharacterId)
            .OrderBy(rn => rn.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取关系网络及其成员
    /// </summary>
    public async Task<RelationshipNetwork?> GetWithMembersAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Context.RelationshipNetworks
            .Include(rn => rn.Members)
            .FirstOrDefaultAsync(rn => rn.Id == id, cancellationToken);
    }

    /// <summary>
    /// 获取关系网络及其关系
    /// </summary>
    public async Task<RelationshipNetwork?> GetWithRelationshipsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Context.RelationshipNetworks
            .Include(rn => rn.Relationships)
            .FirstOrDefaultAsync(rn => rn.Id == id, cancellationToken);
    }

    /// <summary>
    /// 获取完整的关系网络（包含成员和关系）
    /// </summary>
    public async Task<RelationshipNetwork?> GetCompleteNetworkAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Context.RelationshipNetworks
            .Include(rn => rn.Members)
            .Include(rn => rn.Relationships)
            .Include(rn => rn.CentralCharacter)
            .FirstOrDefaultAsync(rn => rn.Id == id, cancellationToken);
    }

    /// <summary>
    /// 根据复杂度范围获取关系网络列表
    /// </summary>
    public async Task<IEnumerable<RelationshipNetwork>> GetByComplexityRangeAsync(Guid projectId, int minComplexity, int maxComplexity, CancellationToken cancellationToken = default)
    {
        return await Context.RelationshipNetworks
            .Where(rn => rn.ProjectId == projectId && rn.Complexity >= minComplexity && rn.Complexity <= maxComplexity)
            .OrderByDescending(rn => rn.Complexity)
            .ThenBy(rn => rn.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据影响力范围获取关系网络列表
    /// </summary>
    public async Task<IEnumerable<RelationshipNetwork>> GetByInfluenceRangeAsync(Guid projectId, int minInfluence, int maxInfluence, CancellationToken cancellationToken = default)
    {
        return await Context.RelationshipNetworks
            .Where(rn => rn.ProjectId == projectId && rn.Influence >= minInfluence && rn.Influence <= maxInfluence)
            .OrderByDescending(rn => rn.Influence)
            .ThenBy(rn => rn.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据成员数量范围获取关系网络列表
    /// </summary>
    public async Task<IEnumerable<RelationshipNetwork>> GetByMemberCountRangeAsync(Guid projectId, int minMemberCount, int maxMemberCount, CancellationToken cancellationToken = default)
    {
        return await Context.RelationshipNetworks
            .Where(rn => rn.ProjectId == projectId && rn.MemberCount >= minMemberCount && rn.MemberCount <= maxMemberCount)
            .OrderByDescending(rn => rn.MemberCount)
            .ThenBy(rn => rn.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据重要性获取关系网络列表
    /// </summary>
    public async Task<IEnumerable<RelationshipNetwork>> GetByImportanceAsync(Guid projectId, int minImportance, CancellationToken cancellationToken = default)
    {
        return await Context.RelationshipNetworks
            .Where(rn => rn.ProjectId == projectId && rn.Importance >= minImportance)
            .OrderByDescending(rn => rn.Importance)
            .ThenBy(rn => rn.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 搜索关系网络
    /// </summary>
    public async Task<IEnumerable<RelationshipNetwork>> SearchAsync(Guid projectId, string searchTerm, CancellationToken cancellationToken = default)
    {
        return await Context.RelationshipNetworks
            .Where(rn => rn.ProjectId == projectId &&
                        (rn.Name.Contains(searchTerm) ||
                         rn.Description!.Contains(searchTerm) ||
                         rn.KeyEvents!.Contains(searchTerm) ||
                         rn.Tags!.Contains(searchTerm)))
            .OrderBy(rn => rn.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取角色参与的关系网络
    /// </summary>
    public async Task<IEnumerable<RelationshipNetwork>> GetByCharacterAsync(Guid characterId, CancellationToken cancellationToken = default)
    {
        return await Context.RelationshipNetworks
            .Where(rn => rn.Members.Any(m => m.Id == characterId) || rn.CentralCharacterId == characterId)
            .OrderBy(rn => rn.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取关系网络统计信息
    /// </summary>
    public async Task<Dictionary<string, object>> GetNetworkStatisticsAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var totalCount = await Context.RelationshipNetworks.CountAsync(rn => rn.ProjectId == projectId, cancellationToken);
        
        var typeStats = await Context.RelationshipNetworks
            .Where(rn => rn.ProjectId == projectId)
            .GroupBy(rn => rn.Type)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Type, x => x.Count, cancellationToken);

        var statusStats = await Context.RelationshipNetworks
            .Where(rn => rn.ProjectId == projectId)
            .GroupBy(rn => rn.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);

        var avgComplexity = await Context.RelationshipNetworks
            .Where(rn => rn.ProjectId == projectId)
            .AverageAsync(rn => (double?)rn.Complexity, cancellationToken) ?? 0;

        return new Dictionary<string, object>
        {
            ["TotalCount"] = totalCount,
            ["TypeStatistics"] = typeStats,
            ["StatusStatistics"] = statusStats,
            ["AverageComplexity"] = avgComplexity
        };
    }
}
