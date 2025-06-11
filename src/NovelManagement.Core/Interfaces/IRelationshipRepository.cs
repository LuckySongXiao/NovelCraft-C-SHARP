using NovelManagement.Core.Entities;

namespace NovelManagement.Core.Interfaces;

/// <summary>
/// 角色关系仓储接口
/// </summary>
public interface ICharacterRelationshipRepository : IRepository<CharacterRelationship>
{
    /// <summary>
    /// 根据角色ID获取关系列表
    /// </summary>
    /// <param name="characterId">角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>关系列表</returns>
    Task<IEnumerable<CharacterRelationship>> GetByCharacterIdAsync(Guid characterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据两个角色ID获取关系
    /// </summary>
    /// <param name="sourceCharacterId">源角色ID</param>
    /// <param name="targetCharacterId">目标角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>关系对象</returns>
    Task<CharacterRelationship?> GetByCharacterPairAsync(Guid sourceCharacterId, Guid targetCharacterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据关系类型获取关系列表
    /// </summary>
    /// <param name="characterId">角色ID</param>
    /// <param name="relationshipType">关系类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>关系列表</returns>
    Task<IEnumerable<CharacterRelationship>> GetByTypeAsync(Guid characterId, string relationshipType, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据项目ID获取所有角色关系
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>关系列表</returns>
    Task<IEnumerable<CharacterRelationship>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
}

/// <summary>
/// 势力关系仓储接口
/// </summary>
public interface IFactionRelationshipRepository : IRepository<FactionRelationship>
{
    /// <summary>
    /// 根据势力ID获取关系列表
    /// </summary>
    /// <param name="factionId">势力ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>关系列表</returns>
    Task<IEnumerable<FactionRelationship>> GetByFactionIdAsync(Guid factionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据两个势力ID获取关系
    /// </summary>
    /// <param name="sourceFactionId">源势力ID</param>
    /// <param name="targetFactionId">目标势力ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>关系对象</returns>
    Task<FactionRelationship?> GetByFactionPairAsync(Guid sourceFactionId, Guid targetFactionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据关系类型获取关系列表
    /// </summary>
    /// <param name="factionId">势力ID</param>
    /// <param name="relationshipType">关系类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>关系列表</returns>
    Task<IEnumerable<FactionRelationship>> GetByTypeAsync(Guid factionId, string relationshipType, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据项目ID获取所有势力关系
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>关系列表</returns>
    Task<IEnumerable<FactionRelationship>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
}
