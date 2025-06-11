using Microsoft.Extensions.Logging;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;

namespace NovelManagement.Application.Services;

/// <summary>
/// 种族管理服务
/// </summary>
public class RaceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RaceService> _logger;

    public RaceService(IUnitOfWork unitOfWork, ILogger<RaceService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// 创建种族
    /// </summary>
    public async Task<Race> CreateRaceAsync(Race race, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始创建种族: {RaceName}, 项目ID: {ProjectId}", race.Name, race.ProjectId);
            
            // 设置创建时间
            race.CreatedAt = DateTime.UtcNow;
            race.UpdatedAt = DateTime.UtcNow;
            
            await _unitOfWork.Races.AddAsync(race, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("成功创建种族: {RaceName}, ID: {RaceId}", race.Name, race.Id);
            return race;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建种族时发生错误: {RaceName}", race.Name);
            throw;
        }
    }

    /// <summary>
    /// 更新种族信息
    /// </summary>
    public async Task<Race> UpdateRaceAsync(Race race, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始更新种族: {RaceName}, ID: {RaceId}", race.Name, race.Id);
            
            // 设置更新时间
            race.UpdatedAt = DateTime.UtcNow;
            
            await _unitOfWork.Races.UpdateAsync(race, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("成功更新种族: {RaceName}, ID: {RaceId}", race.Name, race.Id);
            return race;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新种族时发生错误: {RaceName}, ID: {RaceId}", race.Name, race.Id);
            throw;
        }
    }

    /// <summary>
    /// 根据ID获取种族
    /// </summary>
    public async Task<Race?> GetRaceByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取种族，ID: {RaceId}", id);
            var race = await _unitOfWork.Races.GetByIdAsync(id, cancellationToken);
            if (race == null)
            {
                _logger.LogWarning("未找到种族，ID: {RaceId}", id);
            }
            return race;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取种族时发生错误，ID: {RaceId}", id);
            throw;
        }
    }

    /// <summary>
    /// 根据项目ID获取所有种族
    /// </summary>
    public async Task<IEnumerable<Race>> GetRacesByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取项目种族列表，项目ID: {ProjectId}", projectId);
            
            var races = await _unitOfWork.Races.GetByProjectIdAsync(projectId, cancellationToken);
            var orderedRaces = races.OrderBy(r => r.Name).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个种族", orderedRaces.Count);
            return orderedRaces;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取项目种族列表时发生错误，项目ID: {ProjectId}", projectId);
            throw;
        }
    }

    /// <summary>
    /// 根据类型获取种族
    /// </summary>
    public async Task<IEnumerable<Race>> GetRacesByTypeAsync(Guid projectId, string type, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始按类型获取种族，项目ID: {ProjectId}, 类型: {Type}", projectId, type);
            
            var races = await _unitOfWork.Races.GetByTypeAsync(projectId, type, cancellationToken);
            var orderedRaces = races.OrderBy(r => r.Name).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个 {Type} 类型的种族", orderedRaces.Count, type);
            return orderedRaces;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按类型获取种族时发生错误，项目ID: {ProjectId}, 类型: {Type}", projectId, type);
            throw;
        }
    }

    /// <summary>
    /// 获取种族关系
    /// </summary>
    public async Task<IEnumerable<RaceRelationship>> GetRaceRelationshipsAsync(Guid raceId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取种族关系，种族ID: {RaceId}", raceId);
            
            var relationships = await _unitOfWork.RaceRelationships.GetByRaceAsync(raceId, cancellationToken);
            var orderedRelationships = relationships.OrderBy(r => r.RelationshipType).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个种族关系", orderedRelationships.Count);
            return orderedRelationships;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取种族关系时发生错误，种族ID: {RaceId}", raceId);
            throw;
        }
    }

    /// <summary>
    /// 根据实力等级范围获取种族
    /// </summary>
    public async Task<IEnumerable<Race>> GetRacesByPowerLevelRangeAsync(Guid projectId, int minPowerLevel, int maxPowerLevel, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始按实力等级获取种族，项目ID: {ProjectId}, 实力范围: {MinPowerLevel}-{MaxPowerLevel}", projectId, minPowerLevel, maxPowerLevel);
            
            var races = await _unitOfWork.Races.GetByPowerLevelRangeAsync(projectId, minPowerLevel, maxPowerLevel, cancellationToken);
            var orderedRaces = races.OrderByDescending(r => r.PowerLevel).ThenBy(r => r.Name).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个实力在 {MinPowerLevel}-{MaxPowerLevel} 范围内的种族", orderedRaces.Count, minPowerLevel, maxPowerLevel);
            return orderedRaces;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按实力等级获取种族时发生错误，项目ID: {ProjectId}, 实力范围: {MinPowerLevel}-{MaxPowerLevel}", projectId, minPowerLevel, maxPowerLevel);
            throw;
        }
    }

    /// <summary>
    /// 根据影响力范围获取种族
    /// </summary>
    public async Task<IEnumerable<Race>> GetRacesByInfluenceRangeAsync(Guid projectId, int minInfluence, int maxInfluence, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始按影响力获取种族，项目ID: {ProjectId}, 影响力范围: {MinInfluence}-{MaxInfluence}", projectId, minInfluence, maxInfluence);
            
            var races = await _unitOfWork.Races.GetByInfluenceRangeAsync(projectId, minInfluence, maxInfluence, cancellationToken);
            var orderedRaces = races.OrderByDescending(r => r.Influence).ThenBy(r => r.Name).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个影响力在 {MinInfluence}-{MaxInfluence} 范围内的种族", orderedRaces.Count, minInfluence, maxInfluence);
            return orderedRaces;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按影响力获取种族时发生错误，项目ID: {ProjectId}, 影响力范围: {MinInfluence}-{MaxInfluence}", projectId, minInfluence, maxInfluence);
            throw;
        }
    }

    /// <summary>
    /// 删除种族
    /// </summary>
    public async Task<bool> DeleteRaceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始删除种族，ID: {RaceId}", id);
            
            var race = await _unitOfWork.Races.GetByIdAsync(id, cancellationToken);
            if (race == null)
            {
                _logger.LogWarning("要删除的种族不存在，ID: {RaceId}", id);
                return false;
            }
            
            await _unitOfWork.Races.DeleteAsync(race, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("成功删除种族，ID: {RaceId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除种族时发生错误，ID: {RaceId}", id);
            throw;
        }
    }

    /// <summary>
    /// 搜索种族
    /// </summary>
    public async Task<IEnumerable<Race>> SearchRacesAsync(Guid projectId, string searchTerm, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始搜索种族，项目ID: {ProjectId}, 关键词: {SearchTerm}", projectId, searchTerm);
            
            var races = await _unitOfWork.Races.SearchAsync(projectId, searchTerm, cancellationToken);
            var orderedRaces = races.OrderBy(r => r.Name).ToList();
            
            _logger.LogInformation("搜索完成，找到 {Count} 个匹配的种族", orderedRaces.Count);
            return orderedRaces;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索种族时发生错误，项目ID: {ProjectId}, 关键词: {SearchTerm}", projectId, searchTerm);
            throw;
        }
    }

    /// <summary>
    /// 获取种族统计信息
    /// </summary>
    public async Task<Dictionary<string, object>> GetRaceStatisticsAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取种族统计信息，项目ID: {ProjectId}", projectId);
            
            var statistics = await _unitOfWork.Races.GetStatisticsAsync(projectId, cancellationToken);
            
            _logger.LogInformation("成功获取种族统计信息，项目ID: {ProjectId}", projectId);
            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取种族统计信息时发生错误，项目ID: {ProjectId}", projectId);
            throw;
        }
    }

    /// <summary>
    /// 获取种族及其成员
    /// </summary>
    public async Task<Race?> GetRaceWithMembersAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取种族及其成员，ID: {RaceId}", id);
            
            var race = await _unitOfWork.Races.GetWithMembersAsync(id, cancellationToken);
            if (race == null)
            {
                _logger.LogWarning("未找到种族，ID: {RaceId}", id);
            }
            
            return race;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取种族及其成员时发生错误，ID: {RaceId}", id);
            throw;
        }
    }

    /// <summary>
    /// 获取种族及其关系
    /// </summary>
    public async Task<Race?> GetRaceWithRelationshipsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取种族及其关系，ID: {RaceId}", id);
            
            var race = await _unitOfWork.Races.GetWithRelationshipsAsync(id, cancellationToken);
            if (race == null)
            {
                _logger.LogWarning("未找到种族，ID: {RaceId}", id);
            }
            
            return race;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取种族及其关系时发生错误，ID: {RaceId}", id);
            throw;
        }
    }
}
