using NovelManagement.Core.Entities;

namespace NovelManagement.Core.Interfaces;

/// <summary>
/// 种族关系仓储接口
/// </summary>
public interface IRaceRelationshipRepository : IRepository<RaceRelationship>
{
    /// <summary>
    /// 根据项目ID获取种族关系列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>种族关系列表</returns>
    Task<IEnumerable<RaceRelationship>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据源种族ID获取关系列表
    /// </summary>
    /// <param name="sourceRaceId">源种族ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>种族关系列表</returns>
    Task<IEnumerable<RaceRelationship>> GetBySourceRaceAsync(Guid sourceRaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据目标种族ID获取关系列表
    /// </summary>
    /// <param name="targetRaceId">目标种族ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>种族关系列表</returns>
    Task<IEnumerable<RaceRelationship>> GetByTargetRaceAsync(Guid targetRaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据种族ID获取所有相关关系
    /// </summary>
    /// <param name="raceId">种族ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>种族关系列表</returns>
    Task<IEnumerable<RaceRelationship>> GetByRaceAsync(Guid raceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据关系类型获取关系列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="relationshipType">关系类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>种族关系列表</returns>
    Task<IEnumerable<RaceRelationship>> GetByTypeAsync(Guid projectId, string relationshipType, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据关系状态获取关系列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="status">关系状态</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>种族关系列表</returns>
    Task<IEnumerable<RaceRelationship>> GetByStatusAsync(Guid projectId, string status, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据关系强度范围获取关系列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="minStrength">最小强度</param>
    /// <param name="maxStrength">最大强度</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>种族关系列表</returns>
    Task<IEnumerable<RaceRelationship>> GetByStrengthRangeAsync(Guid projectId, int minStrength, int maxStrength, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取特定两个种族之间的关系
    /// </summary>
    /// <param name="sourceRaceId">源种族ID</param>
    /// <param name="targetRaceId">目标种族ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>种族关系</returns>
    Task<RaceRelationship?> GetBetweenRacesAsync(Guid sourceRaceId, Guid targetRaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据重要性获取关系列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="minImportance">最小重要性</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>种族关系列表</returns>
    Task<IEnumerable<RaceRelationship>> GetByImportanceAsync(Guid projectId, int minImportance, CancellationToken cancellationToken = default);

    /// <summary>
    /// 搜索种族关系
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="searchTerm">搜索词</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>种族关系列表</returns>
    Task<IEnumerable<RaceRelationship>> SearchAsync(Guid projectId, string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取种族关系网络数据
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>关系网络数据</returns>
    Task<Dictionary<string, object>> GetNetworkDataAsync(Guid projectId, CancellationToken cancellationToken = default);
}
