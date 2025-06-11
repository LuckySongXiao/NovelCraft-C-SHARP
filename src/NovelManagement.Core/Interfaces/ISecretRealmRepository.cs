using NovelManagement.Core.Entities;

namespace NovelManagement.Core.Interfaces;

/// <summary>
/// 秘境仓储接口
/// </summary>
public interface ISecretRealmRepository : IRepository<SecretRealm>
{
    /// <summary>
    /// 根据项目ID获取秘境列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>秘境列表</returns>
    Task<IEnumerable<SecretRealm>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据类型获取秘境列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="type">秘境类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>秘境列表</returns>
    Task<IEnumerable<SecretRealm>> GetByTypeAsync(Guid projectId, string type, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据状态获取秘境列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="status">秘境状态</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>秘境列表</returns>
    Task<IEnumerable<SecretRealm>> GetByStatusAsync(Guid projectId, string status, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据发现者势力获取秘境列表
    /// </summary>
    /// <param name="factionId">势力ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>秘境列表</returns>
    Task<IEnumerable<SecretRealm>> GetByDiscovererFactionAsync(Guid factionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据危险等级范围获取秘境列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="minDangerLevel">最小危险等级</param>
    /// <param name="maxDangerLevel">最大危险等级</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>秘境列表</returns>
    Task<IEnumerable<SecretRealm>> GetByDangerLevelRangeAsync(Guid projectId, int minDangerLevel, int maxDangerLevel, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据推荐修为获取秘境列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="recommendedCultivation">推荐修为</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>秘境列表</returns>
    Task<IEnumerable<SecretRealm>> GetByRecommendedCultivationAsync(Guid projectId, string recommendedCultivation, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取秘境及其相关资源
    /// </summary>
    /// <param name="id">秘境ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>秘境对象</returns>
    Task<SecretRealm?> GetWithResourcesAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取秘境及其探索者
    /// </summary>
    /// <param name="id">秘境ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>秘境对象</returns>
    Task<SecretRealm?> GetWithExplorersAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据位置获取秘境列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="location">位置</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>秘境列表</returns>
    Task<IEnumerable<SecretRealm>> GetByLocationAsync(Guid projectId, string location, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据重要性获取秘境列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="minImportance">最小重要性</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>秘境列表</returns>
    Task<IEnumerable<SecretRealm>> GetByImportanceAsync(Guid projectId, int minImportance, CancellationToken cancellationToken = default);

    /// <summary>
    /// 搜索秘境
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="searchTerm">搜索词</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>秘境列表</returns>
    Task<IEnumerable<SecretRealm>> SearchAsync(Guid projectId, string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取秘境探索统计
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>探索统计</returns>
    Task<Dictionary<string, object>> GetExplorationStatisticsAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据探索次数范围获取秘境列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="minCount">最小探索次数</param>
    /// <param name="maxCount">最大探索次数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>秘境列表</returns>
    Task<IEnumerable<SecretRealm>> GetByExplorationCountRangeAsync(Guid projectId, int minCount, int maxCount, CancellationToken cancellationToken = default);
}
