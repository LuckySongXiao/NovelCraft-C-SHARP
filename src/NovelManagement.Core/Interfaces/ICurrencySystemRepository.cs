using NovelManagement.Core.Entities;

namespace NovelManagement.Core.Interfaces;

/// <summary>
/// 货币体系仓储接口
/// </summary>
public interface ICurrencySystemRepository : IRepository<CurrencySystem>
{
    /// <summary>
    /// 根据项目ID获取货币体系列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>货币体系列表</returns>
    Task<IEnumerable<CurrencySystem>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据货币制度获取货币体系列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="monetarySystem">货币制度</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>货币体系列表</returns>
    Task<IEnumerable<CurrencySystem>> GetByMonetarySystemAsync(Guid projectId, string monetarySystem, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据基础货币获取货币体系列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="baseCurrency">基础货币</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>货币体系列表</returns>
    Task<IEnumerable<CurrencySystem>> GetByBaseCurrencyAsync(Guid projectId, string baseCurrency, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据发行机构获取货币体系列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="issuingAuthority">发行机构</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>货币体系列表</returns>
    Task<IEnumerable<CurrencySystem>> GetByIssuingAuthorityAsync(Guid projectId, string issuingAuthority, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取活跃的货币体系列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>货币体系列表</returns>
    Task<IEnumerable<CurrencySystem>> GetActiveSystemsAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取货币体系及其使用势力
    /// </summary>
    /// <param name="id">货币体系ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>货币体系对象</returns>
    Task<CurrencySystem?> GetWithUsingFactionsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据通胀率范围获取货币体系列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="minInflationRate">最小通胀率</param>
    /// <param name="maxInflationRate">最大通胀率</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>货币体系列表</returns>
    Task<IEnumerable<CurrencySystem>> GetByInflationRateRangeAsync(Guid projectId, decimal minInflationRate, decimal maxInflationRate, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据利率范围获取货币体系列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="minInterestRate">最小利率</param>
    /// <param name="maxInterestRate">最大利率</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>货币体系列表</returns>
    Task<IEnumerable<CurrencySystem>> GetByInterestRateRangeAsync(Guid projectId, decimal minInterestRate, decimal maxInterestRate, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据重要性获取货币体系列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="minImportance">最小重要性</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>货币体系列表</returns>
    Task<IEnumerable<CurrencySystem>> GetByImportanceAsync(Guid projectId, int minImportance, CancellationToken cancellationToken = default);

    /// <summary>
    /// 搜索货币体系
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="searchTerm">搜索词</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>货币体系列表</returns>
    Task<IEnumerable<CurrencySystem>> SearchAsync(Guid projectId, string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取货币体系经济统计
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>经济统计</returns>
    Task<Dictionary<string, object>> GetEconomicStatisticsAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据类型获取货币体系列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="type">类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>货币体系列表</returns>
    Task<IEnumerable<CurrencySystem>> GetByTypeAsync(Guid projectId, string type, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据发行势力获取货币体系列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="factionId">势力ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>货币体系列表</returns>
    Task<IEnumerable<CurrencySystem>> GetByIssuingFactionAsync(Guid projectId, Guid factionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据稳定性范围获取货币体系列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="minStability">最小稳定性</param>
    /// <param name="maxStability">最大稳定性</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>货币体系列表</returns>
    Task<IEnumerable<CurrencySystem>> GetByStabilityRangeAsync(Guid projectId, int minStability, int maxStability, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取货币体系及其发行势力
    /// </summary>
    /// <param name="id">货币体系ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>货币体系对象</returns>
    Task<CurrencySystem?> GetWithIssuingFactionAsync(Guid id, CancellationToken cancellationToken = default);
}
