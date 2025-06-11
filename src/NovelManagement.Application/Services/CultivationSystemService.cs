using Microsoft.Extensions.Logging;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;

namespace NovelManagement.Application.Services;

/// <summary>
/// 修炼体系服务
/// </summary>
public class CultivationSystemService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CultivationSystemService> _logger;

    public CultivationSystemService(IUnitOfWork unitOfWork, ILogger<CultivationSystemService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// 创建修炼体系
    /// </summary>
    public async Task<CultivationSystem> CreateCultivationSystemAsync(CultivationSystem cultivationSystem, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始创建修炼体系: {CultivationSystemName}, 项目ID: {ProjectId}", cultivationSystem.Name, cultivationSystem.ProjectId);
            
            // 设置创建时间
            cultivationSystem.CreatedAt = DateTime.UtcNow;
            cultivationSystem.UpdatedAt = DateTime.UtcNow;
            
            await _unitOfWork.CultivationSystems.AddAsync(cultivationSystem, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("成功创建修炼体系: {CultivationSystemName}, ID: {CultivationSystemId}", cultivationSystem.Name, cultivationSystem.Id);
            return cultivationSystem;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建修炼体系时发生错误: {CultivationSystemName}", cultivationSystem.Name);
            throw;
        }
    }

    /// <summary>
    /// 更新修炼体系
    /// </summary>
    public async Task<CultivationSystem> UpdateCultivationSystemAsync(CultivationSystem cultivationSystem, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始更新修炼体系: {CultivationSystemName}, ID: {CultivationSystemId}", cultivationSystem.Name, cultivationSystem.Id);
            
            // 设置更新时间
            cultivationSystem.UpdatedAt = DateTime.UtcNow;
            
            await _unitOfWork.CultivationSystems.UpdateAsync(cultivationSystem, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("成功更新修炼体系: {CultivationSystemName}, ID: {CultivationSystemId}", cultivationSystem.Name, cultivationSystem.Id);
            return cultivationSystem;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新修炼体系时发生错误: {CultivationSystemName}, ID: {CultivationSystemId}", cultivationSystem.Name, cultivationSystem.Id);
            throw;
        }
    }

    /// <summary>
    /// 根据ID获取修炼体系
    /// </summary>
    public async Task<CultivationSystem?> GetCultivationSystemByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取修炼体系，ID: {CultivationSystemId}", id);
            var cultivationSystem = await _unitOfWork.CultivationSystems.GetByIdAsync(id, cancellationToken);
            if (cultivationSystem == null)
            {
                _logger.LogWarning("未找到修炼体系，ID: {CultivationSystemId}", id);
            }
            return cultivationSystem;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取修炼体系时发生错误，ID: {CultivationSystemId}", id);
            throw;
        }
    }

    /// <summary>
    /// 根据项目ID获取所有修炼体系
    /// </summary>
    public async Task<IEnumerable<CultivationSystem>> GetCultivationSystemsByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取项目修炼体系列表，项目ID: {ProjectId}", projectId);
            
            var cultivationSystems = await _unitOfWork.CultivationSystems.GetByProjectIdAsync(projectId, cancellationToken);
            var orderedCultivationSystems = cultivationSystems.OrderBy(cs => cs.Name).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个修炼体系", orderedCultivationSystems.Count);
            return orderedCultivationSystems;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取项目修炼体系列表时发生错误，项目ID: {ProjectId}", projectId);
            throw;
        }
    }

    /// <summary>
    /// 获取修炼等级
    /// </summary>
    public async Task<List<string>> GetCultivationLevelsAsync(Guid cultivationSystemId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取修炼等级，修炼体系ID: {CultivationSystemId}", cultivationSystemId);
            
            var cultivationSystem = await _unitOfWork.CultivationSystems.GetWithLevelsAsync(cultivationSystemId, cancellationToken);
            if (cultivationSystem == null)
            {
                throw new ArgumentException($"修炼体系不存在，ID: {cultivationSystemId}");
            }
            
            // 获取修炼等级列表
            var levels = cultivationSystem.Levels
                .OrderBy(l => l.Order)
                .Select(l => l.Name)
                .ToList();
            
            _logger.LogInformation("成功获取 {Count} 个修炼等级", levels.Count);
            return levels;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取修炼等级时发生错误，修炼体系ID: {CultivationSystemId}", cultivationSystemId);
            throw;
        }
    }

    /// <summary>
    /// 验证修炼进度
    /// </summary>
    public async Task<bool> ValidateCultivationProgressAsync(Guid characterId, string currentLevel, string targetLevel, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始验证修炼进度，角色ID: {CharacterId}, 当前等级: {CurrentLevel}, 目标等级: {TargetLevel}", characterId, currentLevel, targetLevel);
            
            var character = await _unitOfWork.Characters.GetByIdAsync(characterId, cancellationToken);
            if (character == null)
            {
                throw new ArgumentException($"角色不存在，ID: {characterId}");
            }
            
            // 获取角色的修炼体系
            var cultivationSystem = await _unitOfWork.CultivationSystems.GetByCharacterAsync(characterId, cancellationToken);
            if (cultivationSystem == null)
            {
                _logger.LogWarning("角色没有关联的修炼体系，角色ID: {CharacterId}", characterId);
                return false;
            }
            
            var levels = await GetCultivationLevelsAsync(cultivationSystem.Id, cancellationToken);
            
            var currentIndex = levels.IndexOf(currentLevel);
            var targetIndex = levels.IndexOf(targetLevel);
            
            if (currentIndex == -1 || targetIndex == -1)
            {
                _logger.LogWarning("修炼等级不存在，当前等级: {CurrentLevel}, 目标等级: {TargetLevel}", currentLevel, targetLevel);
                return false;
            }
            
            // 验证是否可以从当前等级进阶到目标等级（通常只能逐级提升）
            var isValid = targetIndex == currentIndex + 1 || targetIndex == currentIndex;
            
            _logger.LogInformation("修炼进度验证完成，结果: {IsValid}", isValid);
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证修炼进度时发生错误，角色ID: {CharacterId}", characterId);
            throw;
        }
    }

    /// <summary>
    /// 根据类型获取修炼体系
    /// </summary>
    public async Task<IEnumerable<CultivationSystem>> GetCultivationSystemsByTypeAsync(Guid projectId, string type, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始按类型获取修炼体系，项目ID: {ProjectId}, 类型: {Type}", projectId, type);
            
            var cultivationSystems = await _unitOfWork.CultivationSystems.GetByTypeAsync(projectId, type, cancellationToken);
            var orderedCultivationSystems = cultivationSystems.OrderBy(cs => cs.Name).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个 {Type} 类型的修炼体系", orderedCultivationSystems.Count, type);
            return orderedCultivationSystems;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按类型获取修炼体系时发生错误，项目ID: {ProjectId}, 类型: {Type}", projectId, type);
            throw;
        }
    }

    /// <summary>
    /// 根据难度范围获取修炼体系
    /// </summary>
    public async Task<IEnumerable<CultivationSystem>> GetCultivationSystemsByDifficultyRangeAsync(Guid projectId, int minDifficulty, int maxDifficulty, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始按难度获取修炼体系，项目ID: {ProjectId}, 难度范围: {MinDifficulty}-{MaxDifficulty}", projectId, minDifficulty, maxDifficulty);
            
            var cultivationSystems = await _unitOfWork.CultivationSystems.GetByDifficultyRangeAsync(projectId, minDifficulty, maxDifficulty, cancellationToken);
            var orderedCultivationSystems = cultivationSystems.OrderBy(cs => cs.Difficulty).ThenBy(cs => cs.Name).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个难度在 {MinDifficulty}-{MaxDifficulty} 范围内的修炼体系", orderedCultivationSystems.Count, minDifficulty, maxDifficulty);
            return orderedCultivationSystems;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按难度获取修炼体系时发生错误，项目ID: {ProjectId}, 难度范围: {MinDifficulty}-{MaxDifficulty}", projectId, minDifficulty, maxDifficulty);
            throw;
        }
    }

    /// <summary>
    /// 删除修炼体系
    /// </summary>
    public async Task<bool> DeleteCultivationSystemAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始删除修炼体系，ID: {CultivationSystemId}", id);
            
            var cultivationSystem = await _unitOfWork.CultivationSystems.GetByIdAsync(id, cancellationToken);
            if (cultivationSystem == null)
            {
                _logger.LogWarning("要删除的修炼体系不存在，ID: {CultivationSystemId}", id);
                return false;
            }
            
            await _unitOfWork.CultivationSystems.DeleteAsync(cultivationSystem, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("成功删除修炼体系，ID: {CultivationSystemId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除修炼体系时发生错误，ID: {CultivationSystemId}", id);
            throw;
        }
    }

    /// <summary>
    /// 搜索修炼体系
    /// </summary>
    public async Task<IEnumerable<CultivationSystem>> SearchCultivationSystemsAsync(Guid projectId, string searchTerm, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始搜索修炼体系，项目ID: {ProjectId}, 关键词: {SearchTerm}", projectId, searchTerm);
            
            var cultivationSystems = await _unitOfWork.CultivationSystems.SearchAsync(projectId, searchTerm, cancellationToken);
            var orderedCultivationSystems = cultivationSystems.OrderBy(cs => cs.Name).ToList();
            
            _logger.LogInformation("搜索完成，找到 {Count} 个匹配的修炼体系", orderedCultivationSystems.Count);
            return orderedCultivationSystems;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索修炼体系时发生错误，项目ID: {ProjectId}, 关键词: {SearchTerm}", projectId, searchTerm);
            throw;
        }
    }

    /// <summary>
    /// 获取修炼体系统计信息
    /// </summary>
    public async Task<Dictionary<string, object>> GetCultivationSystemStatisticsAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取修炼体系统计信息，项目ID: {ProjectId}", projectId);
            
            var cultivationSystems = await _unitOfWork.CultivationSystems.GetByProjectIdAsync(projectId, cancellationToken);
            var cultivationSystemList = cultivationSystems.ToList();
            
            var typeStats = cultivationSystemList
                .GroupBy(cs => cs.Type)
                .ToDictionary(g => g.Key, g => g.Count());
            
            var statusStats = cultivationSystemList
                .GroupBy(cs => cs.Status)
                .ToDictionary(g => g.Key, g => g.Count());
            
            var statistics = new Dictionary<string, object>
            {
                ["TotalCultivationSystems"] = cultivationSystemList.Count,
                ["TypeStatistics"] = typeStats,
                ["StatusStatistics"] = statusStats,
                ["ActiveSystems"] = cultivationSystemList.Count(cs => cs.Status == "活跃"),
                ["LegendarySystems"] = cultivationSystemList.Count(cs => cs.Status == "传说"),
                ["AverageDifficulty"] = cultivationSystemList.Any() ? cultivationSystemList.Average(cs => cs.Difficulty) : 0,
                ["AverageMaxLevel"] = cultivationSystemList.Any() ? cultivationSystemList.Average(cs => cs.MaxLevel) : 0,
                ["HighDifficultySystems"] = cultivationSystemList.Count(cs => cs.Difficulty >= 80),
                ["LowDifficultySystems"] = cultivationSystemList.Count(cs => cs.Difficulty <= 30),
                ["AverageImportance"] = cultivationSystemList.Any() ? cultivationSystemList.Average(cs => cs.Importance) : 0
            };
            
            _logger.LogInformation("成功获取修炼体系统计信息，项目ID: {ProjectId}", projectId);
            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取修炼体系统计信息时发生错误，项目ID: {ProjectId}", projectId);
            throw;
        }
    }
}
