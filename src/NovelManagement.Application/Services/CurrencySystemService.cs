using Microsoft.Extensions.Logging;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;

namespace NovelManagement.Application.Services;

/// <summary>
/// 货币体系服务
/// </summary>
public class CurrencySystemService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CurrencySystemService> _logger;

    public CurrencySystemService(IUnitOfWork unitOfWork, ILogger<CurrencySystemService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// 创建货币体系
    /// </summary>
    public async Task<CurrencySystem> CreateCurrencySystemAsync(CurrencySystem currencySystem, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始创建货币体系: {CurrencySystemName}, 项目ID: {ProjectId}", currencySystem.Name, currencySystem.ProjectId);
            
            // 设置创建时间
            currencySystem.CreatedAt = DateTime.UtcNow;
            currencySystem.UpdatedAt = DateTime.UtcNow;
            
            await _unitOfWork.CurrencySystems.AddAsync(currencySystem, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("成功创建货币体系: {CurrencySystemName}, ID: {CurrencySystemId}", currencySystem.Name, currencySystem.Id);
            return currencySystem;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建货币体系时发生错误: {CurrencySystemName}", currencySystem.Name);
            throw;
        }
    }

    /// <summary>
    /// 更新货币体系
    /// </summary>
    public async Task<CurrencySystem> UpdateCurrencySystemAsync(CurrencySystem currencySystem, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始更新货币体系: {CurrencySystemName}, ID: {CurrencySystemId}", currencySystem.Name, currencySystem.Id);
            
            // 设置更新时间
            currencySystem.UpdatedAt = DateTime.UtcNow;
            
            await _unitOfWork.CurrencySystems.UpdateAsync(currencySystem, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("成功更新货币体系: {CurrencySystemName}, ID: {CurrencySystemId}", currencySystem.Name, currencySystem.Id);
            return currencySystem;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新货币体系时发生错误: {CurrencySystemName}, ID: {CurrencySystemId}", currencySystem.Name, currencySystem.Id);
            throw;
        }
    }

    /// <summary>
    /// 根据ID获取货币体系
    /// </summary>
    public async Task<CurrencySystem?> GetCurrencySystemByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取货币体系，ID: {CurrencySystemId}", id);
            var currencySystem = await _unitOfWork.CurrencySystems.GetByIdAsync(id, cancellationToken);
            if (currencySystem == null)
            {
                _logger.LogWarning("未找到货币体系，ID: {CurrencySystemId}", id);
            }
            return currencySystem;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取货币体系时发生错误，ID: {CurrencySystemId}", id);
            throw;
        }
    }

    /// <summary>
    /// 根据项目ID获取所有货币体系
    /// </summary>
    public async Task<IEnumerable<CurrencySystem>> GetCurrencySystemsByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取项目货币体系列表，项目ID: {ProjectId}", projectId);
            
            var currencySystems = await _unitOfWork.CurrencySystems.GetByProjectIdAsync(projectId, cancellationToken);
            var orderedCurrencySystems = currencySystems.OrderBy(cs => cs.Name).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个货币体系", orderedCurrencySystems.Count);
            return orderedCurrencySystems;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取项目货币体系列表时发生错误，项目ID: {ProjectId}", projectId);
            throw;
        }
    }

    /// <summary>
    /// 根据类型获取货币体系
    /// </summary>
    public async Task<IEnumerable<CurrencySystem>> GetCurrencySystemsByTypeAsync(Guid projectId, string type, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始按类型获取货币体系，项目ID: {ProjectId}, 类型: {Type}", projectId, type);
            
            var currencySystems = await _unitOfWork.CurrencySystems.GetByTypeAsync(projectId, type, cancellationToken);
            var orderedCurrencySystems = currencySystems.OrderBy(cs => cs.Name).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个 {Type} 类型的货币体系", orderedCurrencySystems.Count, type);
            return orderedCurrencySystems;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按类型获取货币体系时发生错误，项目ID: {ProjectId}, 类型: {Type}", projectId, type);
            throw;
        }
    }

    /// <summary>
    /// 根据发行势力获取货币体系
    /// </summary>
    public async Task<IEnumerable<CurrencySystem>> GetCurrencySystemsByIssuingFactionAsync(Guid projectId, Guid factionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始按发行势力获取货币体系，势力ID: {FactionId}", factionId);

            var currencySystems = await _unitOfWork.CurrencySystems.GetByIssuingFactionAsync(projectId, factionId, cancellationToken);
            var orderedCurrencySystems = currencySystems.OrderBy(cs => cs.Name).ToList();

            _logger.LogInformation("成功获取 {Count} 个由该势力发行的货币体系", orderedCurrencySystems.Count);
            return orderedCurrencySystems;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按发行势力获取货币体系时发生错误，势力ID: {FactionId}", factionId);
            throw;
        }
    }

    /// <summary>
    /// 根据稳定性范围获取货币体系
    /// </summary>
    public async Task<IEnumerable<CurrencySystem>> GetCurrencySystemsByStabilityRangeAsync(Guid projectId, int minStability, int maxStability, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始按稳定性获取货币体系，项目ID: {ProjectId}, 稳定性范围: {MinStability}-{MaxStability}", projectId, minStability, maxStability);
            
            var currencySystems = await _unitOfWork.CurrencySystems.GetByStabilityRangeAsync(projectId, minStability, maxStability, cancellationToken);
            var orderedCurrencySystems = currencySystems.OrderByDescending(cs => cs.Stability).ThenBy(cs => cs.Name).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个稳定性在 {MinStability}-{MaxStability} 范围内的货币体系", orderedCurrencySystems.Count, minStability, maxStability);
            return orderedCurrencySystems;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按稳定性获取货币体系时发生错误，项目ID: {ProjectId}, 稳定性范围: {MinStability}-{MaxStability}", projectId, minStability, maxStability);
            throw;
        }
    }

    /// <summary>
    /// 计算汇率
    /// </summary>
    public async Task<decimal> CalculateExchangeRateAsync(Guid fromCurrencyId, Guid toCurrencyId, decimal amount, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始计算汇率，从货币ID: {FromCurrencyId}, 到货币ID: {ToCurrencyId}, 金额: {Amount}", fromCurrencyId, toCurrencyId, amount);
            
            var fromCurrency = await _unitOfWork.CurrencySystems.GetByIdAsync(fromCurrencyId, cancellationToken);
            var toCurrency = await _unitOfWork.CurrencySystems.GetByIdAsync(toCurrencyId, cancellationToken);
            
            if (fromCurrency == null)
            {
                throw new ArgumentException($"源货币体系不存在，ID: {fromCurrencyId}");
            }
            
            if (toCurrency == null)
            {
                throw new ArgumentException($"目标货币体系不存在，ID: {toCurrencyId}");
            }
            
            // 简单的汇率计算：基于基础价值比率
            var exchangeRate = toCurrency.BaseValue / fromCurrency.BaseValue;
            var convertedAmount = amount * exchangeRate;
            
            _logger.LogInformation("汇率计算完成，汇率: {ExchangeRate}, 转换后金额: {ConvertedAmount}", exchangeRate, convertedAmount);
            return convertedAmount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "计算汇率时发生错误，从货币ID: {FromCurrencyId}, 到货币ID: {ToCurrencyId}", fromCurrencyId, toCurrencyId);
            throw;
        }
    }

    /// <summary>
    /// 删除货币体系
    /// </summary>
    public async Task<bool> DeleteCurrencySystemAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始删除货币体系，ID: {CurrencySystemId}", id);
            
            var currencySystem = await _unitOfWork.CurrencySystems.GetByIdAsync(id, cancellationToken);
            if (currencySystem == null)
            {
                _logger.LogWarning("要删除的货币体系不存在，ID: {CurrencySystemId}", id);
                return false;
            }
            
            await _unitOfWork.CurrencySystems.DeleteAsync(currencySystem, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("成功删除货币体系，ID: {CurrencySystemId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除货币体系时发生错误，ID: {CurrencySystemId}", id);
            throw;
        }
    }

    /// <summary>
    /// 搜索货币体系
    /// </summary>
    public async Task<IEnumerable<CurrencySystem>> SearchCurrencySystemsAsync(Guid projectId, string searchTerm, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始搜索货币体系，项目ID: {ProjectId}, 关键词: {SearchTerm}", projectId, searchTerm);
            
            var currencySystems = await _unitOfWork.CurrencySystems.SearchAsync(projectId, searchTerm, cancellationToken);
            var orderedCurrencySystems = currencySystems.OrderBy(cs => cs.Name).ToList();
            
            _logger.LogInformation("搜索完成，找到 {Count} 个匹配的货币体系", orderedCurrencySystems.Count);
            return orderedCurrencySystems;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索货币体系时发生错误，项目ID: {ProjectId}, 关键词: {SearchTerm}", projectId, searchTerm);
            throw;
        }
    }

    /// <summary>
    /// 获取货币体系统计信息
    /// </summary>
    public async Task<Dictionary<string, object>> GetCurrencySystemStatisticsAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取货币体系统计信息，项目ID: {ProjectId}", projectId);
            
            var currencySystems = await _unitOfWork.CurrencySystems.GetByProjectIdAsync(projectId, cancellationToken);
            var currencySystemList = currencySystems.ToList();
            
            var typeStats = currencySystemList
                .GroupBy(cs => cs.Type)
                .ToDictionary(g => g.Key, g => g.Count());
            
            var statusStats = currencySystemList
                .GroupBy(cs => cs.Status)
                .ToDictionary(g => g.Key, g => g.Count());
            
            var statistics = new Dictionary<string, object>
            {
                ["TotalCurrencySystems"] = currencySystemList.Count,
                ["TypeStatistics"] = typeStats,
                ["StatusStatistics"] = statusStats,
                ["ActiveSystems"] = currencySystemList.Count(cs => cs.Status == "活跃"),
                ["DeprecatedSystems"] = currencySystemList.Count(cs => cs.Status == "废弃"),
                ["AverageStability"] = currencySystemList.Any() ? currencySystemList.Average(cs => cs.Stability) : 0,
                ["AverageInflation"] = currencySystemList.Any() ? currencySystemList.Average(cs => cs.InflationRate) : 0,
                ["TotalBaseValue"] = currencySystemList.Sum(cs => cs.BaseValue),
                ["AverageBaseValue"] = currencySystemList.Any() ? currencySystemList.Average(cs => cs.BaseValue) : 0,
                ["HighStabilitySystems"] = currencySystemList.Count(cs => cs.Stability >= 80),
                ["LowInflationSystems"] = currencySystemList.Count(cs => cs.InflationRate <= 5)
            };
            
            _logger.LogInformation("成功获取货币体系统计信息，项目ID: {ProjectId}", projectId);
            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取货币体系统计信息时发生错误，项目ID: {ProjectId}", projectId);
            throw;
        }
    }

    /// <summary>
    /// 获取货币体系及其发行势力
    /// </summary>
    public async Task<CurrencySystem?> GetCurrencySystemWithIssuingFactionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取货币体系及其发行势力，ID: {CurrencySystemId}", id);
            
            var currencySystem = await _unitOfWork.CurrencySystems.GetWithIssuingFactionAsync(id, cancellationToken);
            if (currencySystem == null)
            {
                _logger.LogWarning("未找到货币体系，ID: {CurrencySystemId}", id);
            }
            
            return currencySystem;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取货币体系及其发行势力时发生错误，ID: {CurrencySystemId}", id);
            throw;
        }
    }

    /// <summary>
    /// 获取货币体系汇率表
    /// </summary>
    public async Task<Dictionary<string, decimal>> GetExchangeRateTableAsync(Guid baseCurrencyId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取货币体系汇率表，基础货币ID: {BaseCurrencyId}", baseCurrencyId);
            
            var baseCurrency = await _unitOfWork.CurrencySystems.GetByIdAsync(baseCurrencyId, cancellationToken);
            if (baseCurrency == null)
            {
                throw new ArgumentException($"基础货币体系不存在，ID: {baseCurrencyId}");
            }
            
            var allCurrencies = await _unitOfWork.CurrencySystems.GetByProjectIdAsync(baseCurrency.ProjectId, cancellationToken);
            var exchangeRates = new Dictionary<string, decimal>();
            
            foreach (var currency in allCurrencies)
            {
                if (currency.Id != baseCurrencyId)
                {
                    var rate = currency.BaseValue / baseCurrency.BaseValue;
                    exchangeRates[currency.Name] = rate;
                }
            }
            
            _logger.LogInformation("成功获取货币体系汇率表，基础货币ID: {BaseCurrencyId}, 汇率数量: {Count}", baseCurrencyId, exchangeRates.Count);
            return exchangeRates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取货币体系汇率表时发生错误，基础货币ID: {BaseCurrencyId}", baseCurrencyId);
            throw;
        }
    }
}
