using NovelManagement.Core.Entities;

namespace NovelManagement.Core.Interfaces;

/// <summary>
/// 关系网络仓储接口
/// </summary>
public interface IRelationshipNetworkRepository : IRepository<RelationshipNetwork>
{
    /// <summary>
    /// 根据项目ID获取关系网络列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>关系网络列表</returns>
    Task<IEnumerable<RelationshipNetwork>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据类型获取关系网络列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="type">网络类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>关系网络列表</returns>
    Task<IEnumerable<RelationshipNetwork>> GetByTypeAsync(Guid projectId, string type, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据状态获取关系网络列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="status">网络状态</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>关系网络列表</returns>
    Task<IEnumerable<RelationshipNetwork>> GetByStatusAsync(Guid projectId, string status, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据中心人物获取关系网络列表
    /// </summary>
    /// <param name="centralCharacterId">中心人物ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>关系网络列表</returns>
    Task<IEnumerable<RelationshipNetwork>> GetByCentralCharacterAsync(Guid centralCharacterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取关系网络及其成员
    /// </summary>
    /// <param name="id">关系网络ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>关系网络对象</returns>
    Task<RelationshipNetwork?> GetWithMembersAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取关系网络及其关系
    /// </summary>
    /// <param name="id">关系网络ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>关系网络对象</returns>
    Task<RelationshipNetwork?> GetWithRelationshipsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取完整的关系网络（包含成员和关系）
    /// </summary>
    /// <param name="id">关系网络ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>关系网络对象</returns>
    Task<RelationshipNetwork?> GetCompleteNetworkAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据复杂度范围获取关系网络列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="minComplexity">最小复杂度</param>
    /// <param name="maxComplexity">最大复杂度</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>关系网络列表</returns>
    Task<IEnumerable<RelationshipNetwork>> GetByComplexityRangeAsync(Guid projectId, int minComplexity, int maxComplexity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据影响力范围获取关系网络列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="minInfluence">最小影响力</param>
    /// <param name="maxInfluence">最大影响力</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>关系网络列表</returns>
    Task<IEnumerable<RelationshipNetwork>> GetByInfluenceRangeAsync(Guid projectId, int minInfluence, int maxInfluence, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据成员数量范围获取关系网络列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="minMemberCount">最小成员数量</param>
    /// <param name="maxMemberCount">最大成员数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>关系网络列表</returns>
    Task<IEnumerable<RelationshipNetwork>> GetByMemberCountRangeAsync(Guid projectId, int minMemberCount, int maxMemberCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据重要性获取关系网络列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="minImportance">最小重要性</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>关系网络列表</returns>
    Task<IEnumerable<RelationshipNetwork>> GetByImportanceAsync(Guid projectId, int minImportance, CancellationToken cancellationToken = default);

    /// <summary>
    /// 搜索关系网络
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="searchTerm">搜索词</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>关系网络列表</returns>
    Task<IEnumerable<RelationshipNetwork>> SearchAsync(Guid projectId, string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色参与的关系网络
    /// </summary>
    /// <param name="characterId">角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>关系网络列表</returns>
    Task<IEnumerable<RelationshipNetwork>> GetByCharacterAsync(Guid characterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取关系网络统计信息
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>网络统计信息</returns>
    Task<Dictionary<string, object>> GetNetworkStatisticsAsync(Guid projectId, CancellationToken cancellationToken = default);
}
