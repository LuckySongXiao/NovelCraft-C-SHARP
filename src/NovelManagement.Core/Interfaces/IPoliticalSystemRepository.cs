using NovelManagement.Core.Entities;

namespace NovelManagement.Core.Interfaces;

/// <summary>
/// 政治体系仓储接口
/// </summary>
public interface IPoliticalSystemRepository : IRepository<PoliticalSystem>
{
    /// <summary>
    /// 根据项目ID获取政治体系列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>政治体系列表</returns>
    Task<IEnumerable<PoliticalSystem>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据类型获取政治体系列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="type">体系类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>政治体系列表</returns>
    Task<IEnumerable<PoliticalSystem>> GetByTypeAsync(Guid projectId, string type, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取政治体系及其职位
    /// </summary>
    /// <param name="id">体系ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>政治体系对象</returns>
    Task<PoliticalSystem?> GetWithPositionsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据重要性获取政治体系列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="minImportance">最小重要性</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>政治体系列表</returns>
    Task<IEnumerable<PoliticalSystem>> GetByImportanceAsync(Guid projectId, int minImportance, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据统治势力获取政治体系列表
    /// </summary>
    /// <param name="factionId">势力ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>政治体系列表</returns>
    Task<IEnumerable<PoliticalSystem>> GetByRulingFactionAsync(Guid factionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据稳定性范围获取政治体系列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="minStability">最小稳定性</param>
    /// <param name="maxStability">最大稳定性</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>政治体系列表</returns>
    Task<IEnumerable<PoliticalSystem>> GetByStabilityRangeAsync(Guid projectId, int minStability, int maxStability, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据影响力范围获取政治体系列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="minInfluence">最小影响力</param>
    /// <param name="maxInfluence">最大影响力</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>政治体系列表</returns>
    Task<IEnumerable<PoliticalSystem>> GetByInfluenceRangeAsync(Guid projectId, int minInfluence, int maxInfluence, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取政治体系及其统治势力
    /// </summary>
    /// <param name="id">政治体系ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>政治体系对象</returns>
    Task<PoliticalSystem?> GetWithRulingFactionAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 搜索政治体系
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="searchTerm">搜索关键词</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>政治体系列表</returns>
    Task<IEnumerable<PoliticalSystem>> SearchAsync(Guid projectId, string searchTerm, CancellationToken cancellationToken = default);
}

/// <summary>
/// 政治职位仓储接口
/// </summary>
public interface IPoliticalPositionRepository : IRepository<PoliticalPosition>
{
    /// <summary>
    /// 根据政治体系ID获取职位列表
    /// </summary>
    /// <param name="politicalSystemId">政治体系ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>政治职位列表</returns>
    Task<IEnumerable<PoliticalPosition>> GetBySystemIdAsync(Guid politicalSystemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据等级获取职位列表
    /// </summary>
    /// <param name="politicalSystemId">政治体系ID</param>
    /// <param name="level">职位等级</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>政治职位列表</returns>
    Task<IEnumerable<PoliticalPosition>> GetByLevelAsync(Guid politicalSystemId, int level, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取最高等级职位
    /// </summary>
    /// <param name="politicalSystemId">政治体系ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>最高等级职位</returns>
    Task<PoliticalPosition?> GetHighestLevelAsync(Guid politicalSystemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取最低等级职位
    /// </summary>
    /// <param name="politicalSystemId">政治体系ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>最低等级职位</returns>
    Task<PoliticalPosition?> GetLowestLevelAsync(Guid politicalSystemId, CancellationToken cancellationToken = default);
}
