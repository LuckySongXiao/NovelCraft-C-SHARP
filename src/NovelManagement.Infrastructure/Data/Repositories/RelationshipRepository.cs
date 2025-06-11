using Microsoft.EntityFrameworkCore;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;

namespace NovelManagement.Infrastructure.Data.Repositories;

/// <summary>
/// 角色关系仓储实现
/// </summary>
public class CharacterRelationshipRepository : BaseRepository<CharacterRelationship>, ICharacterRelationshipRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="context">数据库上下文</param>
    public CharacterRelationshipRepository(NovelManagementDbContext context) : base(context)
    {
    }

    /// <summary>
    /// 根据角色ID获取关系列表
    /// </summary>
    /// <param name="characterId">角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>关系列表</returns>
    public async Task<IEnumerable<CharacterRelationship>> GetByCharacterIdAsync(Guid characterId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(r => r.SourceCharacter)
            .Include(r => r.TargetCharacter)
            .Where(r => r.SourceCharacterId == characterId || r.TargetCharacterId == characterId)
            .OrderByDescending(r => r.Importance)
            .ThenBy(r => r.RelationshipType)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据两个角色ID获取关系
    /// </summary>
    /// <param name="sourceCharacterId">源角色ID</param>
    /// <param name="targetCharacterId">目标角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>关系对象</returns>
    public async Task<CharacterRelationship?> GetByCharacterPairAsync(Guid sourceCharacterId, Guid targetCharacterId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(r => r.SourceCharacter)
            .Include(r => r.TargetCharacter)
            .FirstOrDefaultAsync(r => 
                (r.SourceCharacterId == sourceCharacterId && r.TargetCharacterId == targetCharacterId) ||
                (r.IsBidirectional && r.SourceCharacterId == targetCharacterId && r.TargetCharacterId == sourceCharacterId), 
                cancellationToken);
    }

    /// <summary>
    /// 根据关系类型获取关系列表
    /// </summary>
    /// <param name="characterId">角色ID</param>
    /// <param name="relationshipType">关系类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>关系列表</returns>
    public async Task<IEnumerable<CharacterRelationship>> GetByTypeAsync(Guid characterId, string relationshipType, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(r => r.SourceCharacter)
            .Include(r => r.TargetCharacter)
            .Where(r => (r.SourceCharacterId == characterId || r.TargetCharacterId == characterId) && 
                       r.RelationshipType == relationshipType)
            .OrderByDescending(r => r.Importance)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据项目ID获取所有角色关系
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>关系列表</returns>
    public async Task<IEnumerable<CharacterRelationship>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(r => r.SourceCharacter)
            .Include(r => r.TargetCharacter)
            .Where(r => r.SourceCharacter.ProjectId == projectId)
            .OrderByDescending(r => r.Importance)
            .ThenBy(r => r.RelationshipType)
            .ToListAsync(cancellationToken);
    }
}

/// <summary>
/// 势力关系仓储实现
/// </summary>
public class FactionRelationshipRepository : BaseRepository<FactionRelationship>, IFactionRelationshipRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="context">数据库上下文</param>
    public FactionRelationshipRepository(NovelManagementDbContext context) : base(context)
    {
    }

    /// <summary>
    /// 根据势力ID获取关系列表
    /// </summary>
    /// <param name="factionId">势力ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>关系列表</returns>
    public async Task<IEnumerable<FactionRelationship>> GetByFactionIdAsync(Guid factionId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(r => r.SourceFaction)
            .Include(r => r.TargetFaction)
            .Where(r => r.SourceFactionId == factionId || r.TargetFactionId == factionId)
            .OrderByDescending(r => r.Importance)
            .ThenBy(r => r.RelationshipType)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据两个势力ID获取关系
    /// </summary>
    /// <param name="sourceFactionId">源势力ID</param>
    /// <param name="targetFactionId">目标势力ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>关系对象</returns>
    public async Task<FactionRelationship?> GetByFactionPairAsync(Guid sourceFactionId, Guid targetFactionId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(r => r.SourceFaction)
            .Include(r => r.TargetFaction)
            .FirstOrDefaultAsync(r => 
                (r.SourceFactionId == sourceFactionId && r.TargetFactionId == targetFactionId) ||
                (r.IsBidirectional && r.SourceFactionId == targetFactionId && r.TargetFactionId == sourceFactionId), 
                cancellationToken);
    }

    /// <summary>
    /// 根据关系类型获取关系列表
    /// </summary>
    /// <param name="factionId">势力ID</param>
    /// <param name="relationshipType">关系类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>关系列表</returns>
    public async Task<IEnumerable<FactionRelationship>> GetByTypeAsync(Guid factionId, string relationshipType, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(r => r.SourceFaction)
            .Include(r => r.TargetFaction)
            .Where(r => (r.SourceFactionId == factionId || r.TargetFactionId == factionId) && 
                       r.RelationshipType == relationshipType)
            .OrderByDescending(r => r.Importance)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据项目ID获取所有势力关系
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>关系列表</returns>
    public async Task<IEnumerable<FactionRelationship>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(r => r.SourceFaction)
            .Include(r => r.TargetFaction)
            .Where(r => r.SourceFaction.ProjectId == projectId)
            .OrderByDescending(r => r.Importance)
            .ThenBy(r => r.RelationshipType)
            .ToListAsync(cancellationToken);
    }
}
