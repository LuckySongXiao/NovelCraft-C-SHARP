using Microsoft.Extensions.Logging;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;

namespace NovelManagement.Application.Services;

/// <summary>
/// 资源管理服务
/// </summary>
public class ResourceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ResourceService> _logger;

    public ResourceService(IUnitOfWork unitOfWork, ILogger<ResourceService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// 创建资源
    /// </summary>
    public async Task<Resource> CreateResourceAsync(Resource resource, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始创建资源: {ResourceName}, 项目ID: {ProjectId}", resource.Name, resource.ProjectId);
            
            // 设置创建时间
            resource.CreatedAt = DateTime.UtcNow;
            resource.UpdatedAt = DateTime.UtcNow;
            
            await _unitOfWork.Resources.AddAsync(resource, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("成功创建资源: {ResourceName}, ID: {ResourceId}", resource.Name, resource.Id);
            return resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建资源时发生错误: {ResourceName}", resource.Name);
            throw;
        }
    }

    /// <summary>
    /// 更新资源信息
    /// </summary>
    public async Task<Resource> UpdateResourceAsync(Resource resource, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始更新资源: {ResourceName}, ID: {ResourceId}", resource.Name, resource.Id);
            
            // 设置更新时间
            resource.UpdatedAt = DateTime.UtcNow;
            
            await _unitOfWork.Resources.UpdateAsync(resource, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("成功更新资源: {ResourceName}, ID: {ResourceId}", resource.Name, resource.Id);
            return resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新资源时发生错误: {ResourceName}, ID: {ResourceId}", resource.Name, resource.Id);
            throw;
        }
    }

    /// <summary>
    /// 根据ID获取资源
    /// </summary>
    public async Task<Resource?> GetResourceByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取资源，ID: {ResourceId}", id);
            var resource = await _unitOfWork.Resources.GetByIdAsync(id, cancellationToken);
            if (resource == null)
            {
                _logger.LogWarning("未找到资源，ID: {ResourceId}", id);
            }
            return resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取资源时发生错误，ID: {ResourceId}", id);
            throw;
        }
    }

    /// <summary>
    /// 根据项目ID获取所有资源
    /// </summary>
    public async Task<IEnumerable<Resource>> GetResourcesByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取项目资源列表，项目ID: {ProjectId}", projectId);
            
            var resources = await _unitOfWork.Resources.GetByProjectIdAsync(projectId, cancellationToken);
            var orderedResources = resources.OrderBy(r => r.Name).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个资源", orderedResources.Count);
            return orderedResources;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取项目资源列表时发生错误，项目ID: {ProjectId}", projectId);
            throw;
        }
    }

    /// <summary>
    /// 按类型获取资源
    /// </summary>
    public async Task<IEnumerable<Resource>> GetResourcesByTypeAsync(Guid projectId, string type, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始按类型获取资源，项目ID: {ProjectId}, 类型: {Type}", projectId, type);
            
            var resources = await _unitOfWork.Resources.GetByTypeAsync(projectId, type, cancellationToken);
            var orderedResources = resources.OrderBy(r => r.Name).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个 {Type} 类型的资源", orderedResources.Count, type);
            return orderedResources;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按类型获取资源时发生错误，项目ID: {ProjectId}, 类型: {Type}", projectId, type);
            throw;
        }
    }

    /// <summary>
    /// 获取资源分布
    /// </summary>
    public async Task<Dictionary<string, int>> GetResourceDistributionAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取资源分布，项目ID: {ProjectId}", projectId);
            
            var distribution = await _unitOfWork.Resources.GetDistributionStatisticsAsync(projectId, cancellationToken);
            
            _logger.LogInformation("成功获取资源分布统计，项目ID: {ProjectId}", projectId);
            return distribution;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取资源分布时发生错误，项目ID: {ProjectId}", projectId);
            throw;
        }
    }

    /// <summary>
    /// 根据稀有度获取资源
    /// </summary>
    public async Task<IEnumerable<Resource>> GetResourcesByRarityAsync(Guid projectId, string rarity, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始按稀有度获取资源，项目ID: {ProjectId}, 稀有度: {Rarity}", projectId, rarity);
            
            var resources = await _unitOfWork.Resources.GetByRarityAsync(projectId, rarity, cancellationToken);
            var orderedResources = resources.OrderBy(r => r.Name).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个 {Rarity} 稀有度的资源", orderedResources.Count, rarity);
            return orderedResources;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按稀有度获取资源时发生错误，项目ID: {ProjectId}, 稀有度: {Rarity}", projectId, rarity);
            throw;
        }
    }

    /// <summary>
    /// 根据控制势力获取资源
    /// </summary>
    public async Task<IEnumerable<Resource>> GetResourcesByControllingFactionAsync(Guid factionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始按控制势力获取资源，势力ID: {FactionId}", factionId);
            
            var resources = await _unitOfWork.Resources.GetByControllingFactionAsync(factionId, cancellationToken);
            var orderedResources = resources.OrderBy(r => r.Name).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个由该势力控制的资源", orderedResources.Count);
            return orderedResources;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按控制势力获取资源时发生错误，势力ID: {FactionId}", factionId);
            throw;
        }
    }

    /// <summary>
    /// 根据位置获取资源
    /// </summary>
    public async Task<IEnumerable<Resource>> GetResourcesByLocationAsync(Guid projectId, string location, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始按位置获取资源，项目ID: {ProjectId}, 位置: {Location}", projectId, location);
            
            var resources = await _unitOfWork.Resources.GetByLocationAsync(projectId, location, cancellationToken);
            var orderedResources = resources.OrderBy(r => r.Name).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个位于 {Location} 的资源", orderedResources.Count, location);
            return orderedResources;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按位置获取资源时发生错误，项目ID: {ProjectId}, 位置: {Location}", projectId, location);
            throw;
        }
    }

    /// <summary>
    /// 根据经济价值范围获取资源
    /// </summary>
    public async Task<IEnumerable<Resource>> GetResourcesByEconomicValueRangeAsync(Guid projectId, int minValue, int maxValue, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始按经济价值获取资源，项目ID: {ProjectId}, 价值范围: {MinValue}-{MaxValue}", projectId, minValue, maxValue);
            
            var resources = await _unitOfWork.Resources.GetByEconomicValueRangeAsync(projectId, minValue, maxValue, cancellationToken);
            var orderedResources = resources.OrderByDescending(r => r.EconomicValue).ThenBy(r => r.Name).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个价值在 {MinValue}-{MaxValue} 范围内的资源", orderedResources.Count, minValue, maxValue);
            return orderedResources;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按经济价值获取资源时发生错误，项目ID: {ProjectId}, 价值范围: {MinValue}-{MaxValue}", projectId, minValue, maxValue);
            throw;
        }
    }

    /// <summary>
    /// 删除资源
    /// </summary>
    public async Task<bool> DeleteResourceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始删除资源，ID: {ResourceId}", id);
            
            var resource = await _unitOfWork.Resources.GetByIdAsync(id, cancellationToken);
            if (resource == null)
            {
                _logger.LogWarning("要删除的资源不存在，ID: {ResourceId}", id);
                return false;
            }
            
            await _unitOfWork.Resources.DeleteAsync(resource, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("成功删除资源，ID: {ResourceId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除资源时发生错误，ID: {ResourceId}", id);
            throw;
        }
    }

    /// <summary>
    /// 搜索资源
    /// </summary>
    public async Task<IEnumerable<Resource>> SearchResourcesAsync(Guid projectId, string searchTerm, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始搜索资源，项目ID: {ProjectId}, 关键词: {SearchTerm}", projectId, searchTerm);
            
            var resources = await _unitOfWork.Resources.SearchAsync(projectId, searchTerm, cancellationToken);
            var orderedResources = resources.OrderBy(r => r.Name).ToList();
            
            _logger.LogInformation("搜索完成，找到 {Count} 个匹配的资源", orderedResources.Count);
            return orderedResources;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索资源时发生错误，项目ID: {ProjectId}, 关键词: {SearchTerm}", projectId, searchTerm);
            throw;
        }
    }

    /// <summary>
    /// 获取资源统计信息
    /// </summary>
    public async Task<Dictionary<string, object>> GetResourceStatisticsAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取资源统计信息，项目ID: {ProjectId}", projectId);
            
            var resources = await _unitOfWork.Resources.GetByProjectIdAsync(projectId, cancellationToken);
            var resourceList = resources.ToList();
            
            var typeStats = resourceList
                .GroupBy(r => r.Type)
                .ToDictionary(g => g.Key, g => g.Count());
            
            var rarityStats = resourceList
                .GroupBy(r => r.Rarity)
                .ToDictionary(g => g.Key, g => g.Count());
            
            var statusStats = resourceList
                .GroupBy(r => r.Status)
                .ToDictionary(g => g.Key, g => g.Count());
            
            var statistics = new Dictionary<string, object>
            {
                ["TotalResources"] = resourceList.Count,
                ["TypeStatistics"] = typeStats,
                ["RarityStatistics"] = rarityStats,
                ["StatusStatistics"] = statusStats,
                ["TotalEconomicValue"] = resourceList.Sum(r => r.EconomicValue),
                ["AverageEconomicValue"] = resourceList.Any() ? resourceList.Average(r => r.EconomicValue) : 0,
                ["HighValueResources"] = resourceList.Count(r => r.EconomicValue >= 1000),
                ["RareResources"] = resourceList.Count(r => r.Rarity == "传说" || r.Rarity == "史诗"),
                ["AverageImportance"] = resourceList.Any() ? resourceList.Average(r => r.Importance) : 0
            };
            
            _logger.LogInformation("成功获取资源统计信息，项目ID: {ProjectId}", projectId);
            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取资源统计信息时发生错误，项目ID: {ProjectId}", projectId);
            throw;
        }
    }
}
