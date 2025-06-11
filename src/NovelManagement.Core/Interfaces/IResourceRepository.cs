using NovelManagement.Core.Entities;

namespace NovelManagement.Core.Interfaces;

/// <summary>
/// 资源仓储接口
/// </summary>
public interface IResourceRepository : IRepository<Resource>
{
    /// <summary>
    /// 根据项目ID获取资源列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>资源列表</returns>
    Task<IEnumerable<Resource>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据类型获取资源列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="type">资源类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>资源列表</returns>
    Task<IEnumerable<Resource>> GetByTypeAsync(Guid projectId, string type, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据稀有度获取资源列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="rarity">稀有度</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>资源列表</returns>
    Task<IEnumerable<Resource>> GetByRarityAsync(Guid projectId, string rarity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据状态获取资源列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="status">资源状态</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>资源列表</returns>
    Task<IEnumerable<Resource>> GetByStatusAsync(Guid projectId, string status, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据控制势力获取资源列表
    /// </summary>
    /// <param name="factionId">势力ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>资源列表</returns>
    Task<IEnumerable<Resource>> GetByControllingFactionAsync(Guid factionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据位置获取资源列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="location">位置</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>资源列表</returns>
    Task<IEnumerable<Resource>> GetByLocationAsync(Guid projectId, string location, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据经济价值范围获取资源列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="minValue">最小价值</param>
    /// <param name="maxValue">最大价值</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>资源列表</returns>
    Task<IEnumerable<Resource>> GetByEconomicValueRangeAsync(Guid projectId, int minValue, int maxValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据重要性获取资源列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="minImportance">最小重要性</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>资源列表</returns>
    Task<IEnumerable<Resource>> GetByImportanceAsync(Guid projectId, int minImportance, CancellationToken cancellationToken = default);

    /// <summary>
    /// 搜索资源
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="searchTerm">搜索词</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>资源列表</returns>
    Task<IEnumerable<Resource>> SearchAsync(Guid projectId, string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取资源分布统计
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>资源分布统计</returns>
    Task<Dictionary<string, int>> GetDistributionStatisticsAsync(Guid projectId, CancellationToken cancellationToken = default);
}
