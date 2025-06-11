using Microsoft.EntityFrameworkCore;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;
using NovelManagement.Infrastructure.Data;
using NovelManagement.Infrastructure.Data.Repositories;

namespace NovelManagement.Infrastructure.Repositories;

/// <summary>
/// 货币体系仓储实现
/// </summary>
public class CurrencySystemRepository : BaseRepository<CurrencySystem>, ICurrencySystemRepository
{
    public CurrencySystemRepository(NovelManagementDbContext context) : base(context)
    {
    }

    /// <summary>
    /// 根据项目ID获取货币体系列表
    /// </summary>
    public async Task<IEnumerable<CurrencySystem>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await Context.CurrencySystems
            .Where(cs => cs.ProjectId == projectId)
            .OrderBy(cs => cs.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据货币制度获取货币体系列表
    /// </summary>
    public async Task<IEnumerable<CurrencySystem>> GetByMonetarySystemAsync(Guid projectId, string monetarySystem, CancellationToken cancellationToken = default)
    {
        return await Context.CurrencySystems
            .Where(cs => cs.ProjectId == projectId && cs.MonetarySystem == monetarySystem)
            .OrderBy(cs => cs.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据基础货币获取货币体系列表
    /// </summary>
    public async Task<IEnumerable<CurrencySystem>> GetByBaseCurrencyAsync(Guid projectId, string baseCurrency, CancellationToken cancellationToken = default)
    {
        return await Context.CurrencySystems
            .Where(cs => cs.ProjectId == projectId && cs.BaseCurrency == baseCurrency)
            .OrderBy(cs => cs.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据发行机构获取货币体系列表
    /// </summary>
    public async Task<IEnumerable<CurrencySystem>> GetByIssuingAuthorityAsync(Guid projectId, string issuingAuthority, CancellationToken cancellationToken = default)
    {
        return await Context.CurrencySystems
            .Where(cs => cs.ProjectId == projectId && cs.IssuingAuthority != null && cs.IssuingAuthority.Contains(issuingAuthority))
            .OrderBy(cs => cs.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取活跃的货币体系列表
    /// </summary>
    public async Task<IEnumerable<CurrencySystem>> GetActiveSystemsAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await Context.CurrencySystems
            .Where(cs => cs.ProjectId == projectId && cs.IsActive)
            .OrderBy(cs => cs.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取货币体系及其使用势力
    /// </summary>
    public async Task<CurrencySystem?> GetWithUsingFactionsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Context.CurrencySystems
            .Include(cs => cs.UsingFactions)
            .FirstOrDefaultAsync(cs => cs.Id == id, cancellationToken);
    }

    /// <summary>
    /// 根据重要性获取货币体系列表
    /// </summary>
    public async Task<IEnumerable<CurrencySystem>> GetByImportanceAsync(Guid projectId, int minImportance, CancellationToken cancellationToken = default)
    {
        return await Context.CurrencySystems
            .Where(cs => cs.ProjectId == projectId && cs.Importance >= minImportance)
            .OrderByDescending(cs => cs.Importance)
            .ThenBy(cs => cs.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 搜索货币体系
    /// </summary>
    public async Task<IEnumerable<CurrencySystem>> SearchAsync(Guid projectId, string searchTerm, CancellationToken cancellationToken = default)
    {
        return await Context.CurrencySystems
            .Where(cs => cs.ProjectId == projectId && 
                        (cs.Name.Contains(searchTerm) || 
                         cs.Description!.Contains(searchTerm) ||
                         cs.BaseCurrency.Contains(searchTerm) ||
                         cs.MonetarySystem.Contains(searchTerm) ||
                         cs.Tags!.Contains(searchTerm)))
            .OrderBy(cs => cs.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据通胀率范围获取货币体系列表
    /// </summary>
    public async Task<IEnumerable<CurrencySystem>> GetByInflationRateRangeAsync(Guid projectId, decimal minInflationRate, decimal maxInflationRate, CancellationToken cancellationToken = default)
    {
        return await Context.CurrencySystems
            .Where(cs => cs.ProjectId == projectId && cs.InflationRate >= minInflationRate && cs.InflationRate <= maxInflationRate)
            .OrderBy(cs => cs.InflationRate)
            .ThenBy(cs => cs.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据利率范围获取货币体系列表
    /// </summary>
    public async Task<IEnumerable<CurrencySystem>> GetByInterestRateRangeAsync(Guid projectId, decimal minInterestRate, decimal maxInterestRate, CancellationToken cancellationToken = default)
    {
        return await Context.CurrencySystems
            .Where(cs => cs.ProjectId == projectId && cs.InterestRate >= minInterestRate && cs.InterestRate <= maxInterestRate)
            .OrderBy(cs => cs.InterestRate)
            .ThenBy(cs => cs.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取货币体系经济统计
    /// </summary>
    public async Task<Dictionary<string, object>> GetEconomicStatisticsAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var totalCount = await Context.CurrencySystems.CountAsync(cs => cs.ProjectId == projectId, cancellationToken);
        var activeCount = await Context.CurrencySystems.CountAsync(cs => cs.ProjectId == projectId && cs.IsActive, cancellationToken);

        var avgInflationRate = await Context.CurrencySystems
            .Where(cs => cs.ProjectId == projectId)
            .AverageAsync(cs => (decimal?)cs.InflationRate, cancellationToken) ?? 0;

        var avgInterestRate = await Context.CurrencySystems
            .Where(cs => cs.ProjectId == projectId)
            .AverageAsync(cs => (decimal?)cs.InterestRate, cancellationToken) ?? 0;

        var systemStats = await Context.CurrencySystems
            .Where(cs => cs.ProjectId == projectId)
            .GroupBy(cs => cs.MonetarySystem)
            .Select(g => new { System = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.System, x => x.Count, cancellationToken);

        return new Dictionary<string, object>
        {
            ["TotalCount"] = totalCount,
            ["ActiveCount"] = activeCount,
            ["AverageInflationRate"] = avgInflationRate,
            ["AverageInterestRate"] = avgInterestRate,
            ["SystemStatistics"] = systemStats
        };
    }

    /// <summary>
    /// 获取货币体系统计信息
    /// </summary>
    public async Task<Dictionary<string, object>> GetStatisticsAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var totalCount = await Context.CurrencySystems.CountAsync(cs => cs.ProjectId == projectId, cancellationToken);
        var activeCount = await Context.CurrencySystems.CountAsync(cs => cs.ProjectId == projectId && cs.IsActive, cancellationToken);

        var systemStats = await Context.CurrencySystems
            .Where(cs => cs.ProjectId == projectId)
            .GroupBy(cs => cs.MonetarySystem)
            .Select(g => new { System = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.System, x => x.Count, cancellationToken);

        return new Dictionary<string, object>
        {
            ["TotalCount"] = totalCount,
            ["ActiveCount"] = activeCount,
            ["SystemStatistics"] = systemStats
        };
    }

    /// <summary>
    /// 根据类型获取货币体系列表
    /// </summary>
    public async Task<IEnumerable<CurrencySystem>> GetByTypeAsync(Guid projectId, string type, CancellationToken cancellationToken = default)
    {
        return await Context.CurrencySystems
            .Where(cs => cs.ProjectId == projectId && cs.MonetarySystem == type)
            .OrderBy(cs => cs.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据发行势力获取货币体系列表
    /// </summary>
    public async Task<IEnumerable<CurrencySystem>> GetByIssuingFactionAsync(Guid projectId, Guid factionId, CancellationToken cancellationToken = default)
    {
        return await Context.CurrencySystems
            .Where(cs => cs.ProjectId == projectId && cs.UsingFactions.Any(f => f.Id == factionId))
            .OrderBy(cs => cs.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据稳定性范围获取货币体系列表
    /// </summary>
    public async Task<IEnumerable<CurrencySystem>> GetByStabilityRangeAsync(Guid projectId, int minStability, int maxStability, CancellationToken cancellationToken = default)
    {
        return await Context.CurrencySystems
            .Where(cs => cs.ProjectId == projectId && cs.Stability >= minStability && cs.Stability <= maxStability)
            .OrderByDescending(cs => cs.Stability)
            .ThenBy(cs => cs.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取货币体系及其发行势力
    /// </summary>
    public async Task<CurrencySystem?> GetWithIssuingFactionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Context.CurrencySystems
            .Include(cs => cs.UsingFactions)
            .FirstOrDefaultAsync(cs => cs.Id == id, cancellationToken);
    }
}
