using Microsoft.Extensions.Logging;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;

namespace NovelManagement.Application.Services;

/// <summary>
/// 政治体系服务
/// </summary>
public class PoliticalSystemService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PoliticalSystemService> _logger;

    public PoliticalSystemService(IUnitOfWork unitOfWork, ILogger<PoliticalSystemService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// 创建政治体系
    /// </summary>
    public async Task<PoliticalSystem> CreatePoliticalSystemAsync(PoliticalSystem politicalSystem, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始创建政治体系: {PoliticalSystemName}, 项目ID: {ProjectId}", politicalSystem.Name, politicalSystem.ProjectId);
            
            // 设置创建时间
            politicalSystem.CreatedAt = DateTime.UtcNow;
            politicalSystem.UpdatedAt = DateTime.UtcNow;
            
            await _unitOfWork.PoliticalSystems.AddAsync(politicalSystem, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("成功创建政治体系: {PoliticalSystemName}, ID: {PoliticalSystemId}", politicalSystem.Name, politicalSystem.Id);
            return politicalSystem;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建政治体系时发生错误: {PoliticalSystemName}", politicalSystem.Name);
            throw;
        }
    }

    /// <summary>
    /// 更新政治体系
    /// </summary>
    public async Task<PoliticalSystem> UpdatePoliticalSystemAsync(PoliticalSystem politicalSystem, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始更新政治体系: {PoliticalSystemName}, ID: {PoliticalSystemId}", politicalSystem.Name, politicalSystem.Id);
            
            // 设置更新时间
            politicalSystem.UpdatedAt = DateTime.UtcNow;
            
            await _unitOfWork.PoliticalSystems.UpdateAsync(politicalSystem, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("成功更新政治体系: {PoliticalSystemName}, ID: {PoliticalSystemId}", politicalSystem.Name, politicalSystem.Id);
            return politicalSystem;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新政治体系时发生错误: {PoliticalSystemName}, ID: {PoliticalSystemId}", politicalSystem.Name, politicalSystem.Id);
            throw;
        }
    }

    /// <summary>
    /// 根据ID获取政治体系
    /// </summary>
    public async Task<PoliticalSystem?> GetPoliticalSystemByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取政治体系，ID: {PoliticalSystemId}", id);
            var politicalSystem = await _unitOfWork.PoliticalSystems.GetByIdAsync(id, cancellationToken);
            if (politicalSystem == null)
            {
                _logger.LogWarning("未找到政治体系，ID: {PoliticalSystemId}", id);
            }
            return politicalSystem;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取政治体系时发生错误，ID: {PoliticalSystemId}", id);
            throw;
        }
    }

    /// <summary>
    /// 根据项目ID获取所有政治体系
    /// </summary>
    public async Task<IEnumerable<PoliticalSystem>> GetPoliticalSystemsByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取项目政治体系列表，项目ID: {ProjectId}", projectId);
            
            var politicalSystems = await _unitOfWork.PoliticalSystems.GetByProjectIdAsync(projectId, cancellationToken);
            var orderedPoliticalSystems = politicalSystems.OrderBy(ps => ps.Name).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个政治体系", orderedPoliticalSystems.Count);
            return orderedPoliticalSystems;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取项目政治体系列表时发生错误，项目ID: {ProjectId}", projectId);
            throw;
        }
    }

    /// <summary>
    /// 获取政治层级
    /// </summary>
    public async Task<List<string>> GetPoliticalHierarchyAsync(Guid politicalSystemId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取政治层级，政治体系ID: {PoliticalSystemId}", politicalSystemId);
            
            var politicalSystem = await _unitOfWork.PoliticalSystems.GetByIdAsync(politicalSystemId, cancellationToken);
            if (politicalSystem == null)
            {
                throw new ArgumentException($"政治体系不存在，ID: {politicalSystemId}");
            }
            
            // 解析政治层级（假设存储在Hierarchy字段中，以逗号分隔，从高到低）
            var hierarchy = new List<string>();
            if (!string.IsNullOrEmpty(politicalSystem.Hierarchy))
            {
                hierarchy = politicalSystem.Hierarchy.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(h => h.Trim())
                    .ToList();
            }
            
            _logger.LogInformation("成功获取 {Count} 个政治层级", hierarchy.Count);
            return hierarchy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取政治层级时发生错误，政治体系ID: {PoliticalSystemId}", politicalSystemId);
            throw;
        }
    }

    /// <summary>
    /// 根据类型获取政治体系
    /// </summary>
    public async Task<IEnumerable<PoliticalSystem>> GetPoliticalSystemsByTypeAsync(Guid projectId, string type, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始按类型获取政治体系，项目ID: {ProjectId}, 类型: {Type}", projectId, type);
            
            var politicalSystems = await _unitOfWork.PoliticalSystems.GetByTypeAsync(projectId, type, cancellationToken);
            var orderedPoliticalSystems = politicalSystems.OrderBy(ps => ps.Name).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个 {Type} 类型的政治体系", orderedPoliticalSystems.Count, type);
            return orderedPoliticalSystems;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按类型获取政治体系时发生错误，项目ID: {ProjectId}, 类型: {Type}", projectId, type);
            throw;
        }
    }

    /// <summary>
    /// 根据统治势力获取政治体系
    /// </summary>
    public async Task<IEnumerable<PoliticalSystem>> GetPoliticalSystemsByRulingFactionAsync(Guid factionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始按统治势力获取政治体系，势力ID: {FactionId}", factionId);
            
            var politicalSystems = await _unitOfWork.PoliticalSystems.GetByRulingFactionAsync(factionId, cancellationToken);
            var orderedPoliticalSystems = politicalSystems.OrderBy(ps => ps.Name).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个由该势力统治的政治体系", orderedPoliticalSystems.Count);
            return orderedPoliticalSystems;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按统治势力获取政治体系时发生错误，势力ID: {FactionId}", factionId);
            throw;
        }
    }

    /// <summary>
    /// 根据稳定性范围获取政治体系
    /// </summary>
    public async Task<IEnumerable<PoliticalSystem>> GetPoliticalSystemsByStabilityRangeAsync(Guid projectId, int minStability, int maxStability, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始按稳定性获取政治体系，项目ID: {ProjectId}, 稳定性范围: {MinStability}-{MaxStability}", projectId, minStability, maxStability);
            
            var politicalSystems = await _unitOfWork.PoliticalSystems.GetByStabilityRangeAsync(projectId, minStability, maxStability, cancellationToken);
            var orderedPoliticalSystems = politicalSystems.OrderByDescending(ps => ps.Stability).ThenBy(ps => ps.Name).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个稳定性在 {MinStability}-{MaxStability} 范围内的政治体系", orderedPoliticalSystems.Count, minStability, maxStability);
            return orderedPoliticalSystems;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按稳定性获取政治体系时发生错误，项目ID: {ProjectId}, 稳定性范围: {MinStability}-{MaxStability}", projectId, minStability, maxStability);
            throw;
        }
    }

    /// <summary>
    /// 根据影响力范围获取政治体系
    /// </summary>
    public async Task<IEnumerable<PoliticalSystem>> GetPoliticalSystemsByInfluenceRangeAsync(Guid projectId, int minInfluence, int maxInfluence, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始按影响力获取政治体系，项目ID: {ProjectId}, 影响力范围: {MinInfluence}-{MaxInfluence}", projectId, minInfluence, maxInfluence);
            
            var politicalSystems = await _unitOfWork.PoliticalSystems.GetByInfluenceRangeAsync(projectId, minInfluence, maxInfluence, cancellationToken);
            var orderedPoliticalSystems = politicalSystems.OrderByDescending(ps => ps.Influence).ThenBy(ps => ps.Name).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个影响力在 {MinInfluence}-{MaxInfluence} 范围内的政治体系", orderedPoliticalSystems.Count, minInfluence, maxInfluence);
            return orderedPoliticalSystems;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按影响力获取政治体系时发生错误，项目ID: {ProjectId}, 影响力范围: {MinInfluence}-{MaxInfluence}", projectId, minInfluence, maxInfluence);
            throw;
        }
    }

    /// <summary>
    /// 删除政治体系
    /// </summary>
    public async Task<bool> DeletePoliticalSystemAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始删除政治体系，ID: {PoliticalSystemId}", id);
            
            var politicalSystem = await _unitOfWork.PoliticalSystems.GetByIdAsync(id, cancellationToken);
            if (politicalSystem == null)
            {
                _logger.LogWarning("要删除的政治体系不存在，ID: {PoliticalSystemId}", id);
                return false;
            }
            
            await _unitOfWork.PoliticalSystems.DeleteAsync(politicalSystem, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("成功删除政治体系，ID: {PoliticalSystemId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除政治体系时发生错误，ID: {PoliticalSystemId}", id);
            throw;
        }
    }

    /// <summary>
    /// 搜索政治体系
    /// </summary>
    public async Task<IEnumerable<PoliticalSystem>> SearchPoliticalSystemsAsync(Guid projectId, string searchTerm, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始搜索政治体系，项目ID: {ProjectId}, 关键词: {SearchTerm}", projectId, searchTerm);
            
            var politicalSystems = await _unitOfWork.PoliticalSystems.SearchAsync(projectId, searchTerm, cancellationToken);
            var orderedPoliticalSystems = politicalSystems.OrderBy(ps => ps.Name).ToList();
            
            _logger.LogInformation("搜索完成，找到 {Count} 个匹配的政治体系", orderedPoliticalSystems.Count);
            return orderedPoliticalSystems;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索政治体系时发生错误，项目ID: {ProjectId}, 关键词: {SearchTerm}", projectId, searchTerm);
            throw;
        }
    }

    /// <summary>
    /// 获取政治体系统计信息
    /// </summary>
    public async Task<Dictionary<string, object>> GetPoliticalSystemStatisticsAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取政治体系统计信息，项目ID: {ProjectId}", projectId);
            
            var politicalSystems = await _unitOfWork.PoliticalSystems.GetByProjectIdAsync(projectId, cancellationToken);
            var politicalSystemList = politicalSystems.ToList();
            
            var typeStats = politicalSystemList
                .GroupBy(ps => ps.Type)
                .ToDictionary(g => g.Key, g => g.Count());
            
            var statusStats = politicalSystemList
                .GroupBy(ps => ps.Status)
                .ToDictionary(g => g.Key, g => g.Count());
            
            var statistics = new Dictionary<string, object>
            {
                ["TotalPoliticalSystems"] = politicalSystemList.Count,
                ["TypeStatistics"] = typeStats,
                ["StatusStatistics"] = statusStats,
                ["ActiveSystems"] = politicalSystemList.Count(ps => ps.Status == "活跃"),
                ["CollapsedSystems"] = politicalSystemList.Count(ps => ps.Status == "崩溃"),
                ["AverageStability"] = politicalSystemList.Any() ? politicalSystemList.Average(ps => ps.Stability) : 0,
                ["AverageInfluence"] = politicalSystemList.Any() ? politicalSystemList.Average(ps => ps.Influence) : 0,
                ["HighStabilitySystems"] = politicalSystemList.Count(ps => ps.Stability >= 80),
                ["LowStabilitySystems"] = politicalSystemList.Count(ps => ps.Stability <= 30),
                ["HighInfluenceSystems"] = politicalSystemList.Count(ps => ps.Influence >= 80),
                ["AverageImportance"] = politicalSystemList.Any() ? politicalSystemList.Average(ps => ps.Importance) : 0
            };
            
            _logger.LogInformation("成功获取政治体系统计信息，项目ID: {ProjectId}", projectId);
            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取政治体系统计信息时发生错误，项目ID: {ProjectId}", projectId);
            throw;
        }
    }

    /// <summary>
    /// 获取政治体系及其统治势力
    /// </summary>
    public async Task<PoliticalSystem?> GetPoliticalSystemWithRulingFactionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取政治体系及其统治势力，ID: {PoliticalSystemId}", id);
            
            var politicalSystem = await _unitOfWork.PoliticalSystems.GetWithRulingFactionAsync(id, cancellationToken);
            if (politicalSystem == null)
            {
                _logger.LogWarning("未找到政治体系，ID: {PoliticalSystemId}", id);
            }
            
            return politicalSystem;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取政治体系及其统治势力时发生错误，ID: {PoliticalSystemId}", id);
            throw;
        }
    }
}
