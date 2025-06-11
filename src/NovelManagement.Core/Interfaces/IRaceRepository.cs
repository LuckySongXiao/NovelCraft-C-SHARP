using NovelManagement.Core.Entities;

namespace NovelManagement.Core.Interfaces;

/// <summary>
/// 种族仓储接口
/// </summary>
public interface IRaceRepository : IRepository<Race>
{
    /// <summary>
    /// 根据项目ID获取种族列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>种族列表</returns>
    Task<IEnumerable<Race>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据类型获取种族列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="type">种族类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>种族列表</returns>
    Task<IEnumerable<Race>> GetByTypeAsync(Guid projectId, string type, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据状态获取种族列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="status">种族状态</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>种族列表</returns>
    Task<IEnumerable<Race>> GetByStatusAsync(Guid projectId, string status, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取种族及其成员
    /// </summary>
    /// <param name="id">种族ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>种族对象</returns>
    Task<Race?> GetWithMembersAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取种族及其关系
    /// </summary>
    /// <param name="id">种族ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>种族对象</returns>
    Task<Race?> GetWithRelationshipsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据实力等级范围获取种族列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="minPowerLevel">最小实力等级</param>
    /// <param name="maxPowerLevel">最大实力等级</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>种族列表</returns>
    Task<IEnumerable<Race>> GetByPowerLevelRangeAsync(Guid projectId, int minPowerLevel, int maxPowerLevel, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据影响力范围获取种族列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="minInfluence">最小影响力</param>
    /// <param name="maxInfluence">最大影响力</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>种族列表</returns>
    Task<IEnumerable<Race>> GetByInfluenceRangeAsync(Guid projectId, int minInfluence, int maxInfluence, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据重要性获取种族列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="minImportance">最小重要性</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>种族列表</returns>
    Task<IEnumerable<Race>> GetByImportanceAsync(Guid projectId, int minImportance, CancellationToken cancellationToken = default);

    /// <summary>
    /// 搜索种族
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="searchTerm">搜索词</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>种族列表</returns>
    Task<IEnumerable<Race>> SearchAsync(Guid projectId, string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取种族统计信息
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>种族统计信息</returns>
    Task<Dictionary<string, object>> GetStatisticsAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据名称获取种族
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="name">种族名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>种族对象</returns>
    Task<Race?> GetByNameAsync(Guid projectId, string name, CancellationToken cancellationToken = default);
}
