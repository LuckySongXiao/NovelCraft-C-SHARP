using Microsoft.Extensions.Logging;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;

namespace NovelManagement.Application.Services;

/// <summary>
/// 势力管理服务
/// </summary>
public class FactionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<FactionService> _logger;

    public FactionService(IUnitOfWork unitOfWork, ILogger<FactionService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// 创建势力
    /// </summary>
    public async Task<Faction> CreateFactionAsync(Faction faction, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始创建势力: {FactionName}, 项目ID: {ProjectId}", faction.Name, faction.ProjectId);
            
            // 设置创建时间
            faction.CreatedAt = DateTime.UtcNow;
            faction.UpdatedAt = DateTime.UtcNow;
            
            await _unitOfWork.Factions.AddAsync(faction, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("成功创建势力: {FactionName}, ID: {FactionId}", faction.Name, faction.Id);
            return faction;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建势力时发生错误: {FactionName}", faction.Name);
            throw;
        }
    }

    /// <summary>
    /// 更新势力信息
    /// </summary>
    public async Task<Faction> UpdateFactionAsync(Faction faction, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始更新势力: {FactionName}, ID: {FactionId}", faction.Name, faction.Id);
            
            // 设置更新时间
            faction.UpdatedAt = DateTime.UtcNow;
            
            await _unitOfWork.Factions.UpdateAsync(faction, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("成功更新势力: {FactionName}, ID: {FactionId}", faction.Name, faction.Id);
            return faction;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新势力时发生错误: {FactionName}, ID: {FactionId}", faction.Name, faction.Id);
            throw;
        }
    }

    /// <summary>
    /// 根据ID获取势力
    /// </summary>
    public async Task<Faction?> GetFactionByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取势力，ID: {FactionId}", id);
            var faction = await _unitOfWork.Factions.GetByIdAsync(id, cancellationToken);
            if (faction == null)
            {
                _logger.LogWarning("未找到势力，ID: {FactionId}", id);
            }
            return faction;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取势力时发生错误，ID: {FactionId}", id);
            throw;
        }
    }

    /// <summary>
    /// 根据项目ID获取所有势力
    /// </summary>
    public async Task<IEnumerable<Faction>> GetFactionsByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取项目势力列表，项目ID: {ProjectId}", projectId);
            
            var factions = await _unitOfWork.Factions.GetByProjectIdAsync(projectId, cancellationToken);
            var orderedFactions = factions.OrderBy(f => f.Name).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个势力", orderedFactions.Count);
            return orderedFactions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取项目势力列表时发生错误，项目ID: {ProjectId}", projectId);
            throw;
        }
    }

    /// <summary>
    /// 按类型获取势力
    /// </summary>
    public async Task<IEnumerable<Faction>> GetFactionsByTypeAsync(Guid projectId, string type, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始按类型获取势力，项目ID: {ProjectId}, 类型: {Type}", projectId, type);
            
            var factions = await _unitOfWork.Factions.GetByTypeAsync(projectId, type, cancellationToken);
            var orderedFactions = factions.OrderBy(f => f.Name).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个 {Type} 类型的势力", orderedFactions.Count, type);
            return orderedFactions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按类型获取势力时发生错误，项目ID: {ProjectId}, 类型: {Type}", projectId, type);
            throw;
        }
    }

    /// <summary>
    /// 获取势力层级
    /// </summary>
    public async Task<IEnumerable<Faction>> GetFactionHierarchyAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取势力层级，项目ID: {ProjectId}", projectId);
            
            var factions = await _unitOfWork.Factions.GetByProjectIdAsync(projectId, cancellationToken);
            
            // 构建层级结构：先获取顶级势力（没有父势力的），然后递归获取子势力
            var topLevelFactions = factions.Where(f => f.ParentFactionId == null).OrderBy(f => f.Name);
            var hierarchicalFactions = new List<Faction>();
            
            foreach (var faction in topLevelFactions)
            {
                hierarchicalFactions.Add(faction);
                AddChildFactions(faction, factions.ToList(), hierarchicalFactions);
            }
            
            _logger.LogInformation("成功获取势力层级，共 {Count} 个势力", hierarchicalFactions.Count);
            return hierarchicalFactions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取势力层级时发生错误，项目ID: {ProjectId}", projectId);
            throw;
        }
    }

    /// <summary>
    /// 获取势力关系
    /// </summary>
    public async Task<IEnumerable<FactionRelationship>> GetFactionRelationshipsAsync(Guid factionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取势力关系，势力ID: {FactionId}", factionId);
            
            var relationships = await _unitOfWork.FactionRelationships.GetByFactionIdAsync(factionId, cancellationToken);
            var orderedRelationships = relationships.OrderBy(r => r.RelationshipType).ToList();

            _logger.LogInformation("成功获取 {Count} 个势力关系", orderedRelationships.Count);
            return orderedRelationships;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取势力关系时发生错误，势力ID: {FactionId}", factionId);
            throw;
        }
    }

    /// <summary>
    /// 删除势力
    /// </summary>
    public async Task<bool> DeleteFactionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始删除势力，ID: {FactionId}", id);
            
            var faction = await _unitOfWork.Factions.GetByIdAsync(id, cancellationToken);
            if (faction == null)
            {
                _logger.LogWarning("要删除的势力不存在，ID: {FactionId}", id);
                return false;
            }
            
            await _unitOfWork.Factions.DeleteAsync(faction, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("成功删除势力，ID: {FactionId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除势力时发生错误，ID: {FactionId}", id);
            throw;
        }
    }

    /// <summary>
    /// 搜索势力
    /// </summary>
    public async Task<IEnumerable<Faction>> SearchFactionsAsync(Guid projectId, string searchTerm, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始搜索势力，项目ID: {ProjectId}, 关键词: {SearchTerm}", projectId, searchTerm);
            
            var factions = await _unitOfWork.Factions.SearchAsync(projectId, searchTerm, cancellationToken);
            var orderedFactions = factions.OrderBy(f => f.Name).ToList();
            
            _logger.LogInformation("搜索完成，找到 {Count} 个匹配的势力", orderedFactions.Count);
            return orderedFactions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索势力时发生错误，项目ID: {ProjectId}, 关键词: {SearchTerm}", projectId, searchTerm);
            throw;
        }
    }

    /// <summary>
    /// 获取势力统计信息
    /// </summary>
    public async Task<Dictionary<string, object>> GetFactionStatisticsAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取势力统计信息，项目ID: {ProjectId}", projectId);
            
            var factions = await _unitOfWork.Factions.GetByProjectIdAsync(projectId, cancellationToken);
            var factionList = factions.ToList();
            
            var typeStats = factionList
                .GroupBy(f => f.Type)
                .ToDictionary(g => g.Key, g => g.Count());
            
            var statusStats = factionList
                .GroupBy(f => f.Status)
                .ToDictionary(g => g.Key, g => g.Count());
            
            var statistics = new Dictionary<string, object>
            {
                ["TotalFactions"] = factionList.Count,
                ["TypeStatistics"] = typeStats,
                ["StatusStatistics"] = statusStats,
                ["TopLevelFactions"] = factionList.Count(f => f.ParentFactionId == null),
                ["SubFactions"] = factionList.Count(f => f.ParentFactionId != null),
                ["AveragePowerLevel"] = factionList.Any() ? factionList.Average(f => f.PowerLevel) : 0,
                ["AverageInfluence"] = factionList.Any() ? factionList.Average(f => f.Influence) : 0,
                ["AverageImportance"] = factionList.Any() ? factionList.Average(f => f.Importance) : 0
            };
            
            _logger.LogInformation("成功获取势力统计信息，项目ID: {ProjectId}", projectId);
            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取势力统计信息时发生错误，项目ID: {ProjectId}", projectId);
            throw;
        }
    }

    /// <summary>
    /// 根据实力等级获取势力
    /// </summary>
    public async Task<IEnumerable<Faction>> GetFactionsByPowerLevelAsync(Guid projectId, int minPowerLevel, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始按实力等级获取势力，项目ID: {ProjectId}, 最小实力等级: {MinPowerLevel}", projectId, minPowerLevel);
            
            var factions = await _unitOfWork.Factions.GetByPowerLevelRangeAsync(projectId, minPowerLevel, int.MaxValue, cancellationToken);
            var orderedFactions = factions.OrderByDescending(f => f.PowerLevel).ThenBy(f => f.Name).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个实力等级 >= {MinPowerLevel} 的势力", orderedFactions.Count, minPowerLevel);
            return orderedFactions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按实力等级获取势力时发生错误，项目ID: {ProjectId}, 最小实力等级: {MinPowerLevel}", projectId, minPowerLevel);
            throw;
        }
    }

    /// <summary>
    /// 递归添加子势力到层级列表中
    /// </summary>
    private void AddChildFactions(Faction parentFaction, List<Faction> allFactions, List<Faction> hierarchicalList)
    {
        var childFactions = allFactions.Where(f => f.ParentFactionId == parentFaction.Id).OrderBy(f => f.Name);
        foreach (var childFaction in childFactions)
        {
            hierarchicalList.Add(childFaction);
            AddChildFactions(childFaction, allFactions, hierarchicalList);
        }
    }
}
