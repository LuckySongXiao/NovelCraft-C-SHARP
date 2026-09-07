using Microsoft.Extensions.Logging;
using NovelManagement.Application.DTOs;
using NovelManagement.Application.Interfaces;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;

namespace NovelManagement.Application.Services;

/// <summary>
/// 修炼体系服务
/// </summary>
public class CultivationSystemService : ICultivationSystemService
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

    #region ICultivationSystemService 接口实现（DTO 层）

    private static CultivationSystemDto MapSystem(CultivationSystem e, bool withLevels) => new()
    {
        Id = e.Id,
        Name = e.Name,
        Type = e.Type,
        Description = e.Description,
        CultivationMethod = e.CultivationMethod,
        RealmDivision = e.RealmDivision,
        BreakthroughConditions = e.BreakthroughConditions,
        CultivationResources = e.CultivationResources,
        Characteristics = e.Characteristics,
        Risks = e.Risks,
        ProjectId = e.ProjectId,
        Importance = e.Importance,
        ImagePath = e.ImagePath,
        Tags = e.Tags,
        Notes = e.Notes,
        Status = e.Status,
        OrderIndex = e.Order,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
        Levels = withLevels
            ? (e.Levels ?? new List<CultivationLevel>())
                .OrderBy(l => l.Order).Select(MapLevel).ToList()
            : new List<CultivationLevelDto>()
    };

    private static CultivationLevelDto MapLevel(CultivationLevel l) => new()
    {
        Id = l.Id,
        Name = l.Name,
        OrderIndex = l.Order,
        Description = l.Description,
        BreakthroughCondition = l.BreakthroughCondition,
        Abilities = l.Abilities,
        CultivationTime = l.CultivationTime,
        CultivationSystemId = l.CultivationSystemId,
        Tags = l.Tags,
        Notes = l.Notes,
        CreatedAt = l.CreatedAt,
        UpdatedAt = l.UpdatedAt
    };

    public async Task<IEnumerable<CultivationSystemDto>> GetAllAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var systems = await _unitOfWork.CultivationSystems.GetByProjectIdAsync(projectId, cancellationToken);
        return systems.OrderBy(s => s.Order).ThenBy(s => s.Name).Select(s => MapSystem(s, withLevels: false)).ToList();
    }

    public async Task<CultivationSystemDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.CultivationSystems.GetByIdAsync(id, cancellationToken);
        return e == null ? null : MapSystem(e, withLevels: false);
    }

    public async Task<IEnumerable<CultivationSystemDto>> GetByTypeAsync(Guid projectId, string type, CancellationToken cancellationToken = default)
    {
        var systems = await _unitOfWork.CultivationSystems.GetByTypeAsync(projectId, type, cancellationToken);
        return systems.OrderBy(s => s.Order).Select(s => MapSystem(s, withLevels: false)).ToList();
    }

    public async Task<CultivationSystemDto?> GetWithLevelsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.CultivationSystems.GetWithLevelsAsync(id, cancellationToken);
        return e == null ? null : MapSystem(e, withLevels: true);
    }

    public async Task<IEnumerable<CultivationSystemDto>> GetByImportanceAsync(Guid projectId, int minImportance, CancellationToken cancellationToken = default)
    {
        var systems = await _unitOfWork.CultivationSystems.GetByImportanceAsync(projectId, minImportance, cancellationToken);
        return systems.OrderBy(s => s.Order).Select(s => MapSystem(s, withLevels: false)).ToList();
    }

    public async Task<IEnumerable<CultivationSystemDto>> SearchAsync(Guid projectId, string searchTerm, CancellationToken cancellationToken = default)
    {
        var systems = await _unitOfWork.CultivationSystems.SearchAsync(projectId, searchTerm, cancellationToken);
        return systems.OrderBy(s => s.Name).Select(s => MapSystem(s, withLevels: false)).ToList();
    }

    public async Task<CultivationSystemDto> CreateAsync(CreateCultivationSystemDto createDto, CancellationToken cancellationToken = default)
    {
        var entity = new CultivationSystem
        {
            Name = createDto.Name,
            Type = createDto.Type,
            Description = createDto.Description,
            CultivationMethod = createDto.CultivationMethod,
            RealmDivision = createDto.RealmDivision,
            BreakthroughConditions = createDto.BreakthroughConditions,
            CultivationResources = createDto.CultivationResources,
            Characteristics = createDto.Characteristics,
            Risks = createDto.Risks,
            ProjectId = createDto.ProjectId,
            Importance = createDto.Importance,
            ImagePath = createDto.ImagePath,
            Tags = createDto.Tags,
            Notes = createDto.Notes,
            Status = createDto.Status,
            Order = createDto.OrderIndex
        };
        var created = await _unitOfWork.CultivationSystems.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("创建修炼体系: {Name}, 项目ID: {ProjectId}", created.Name, created.ProjectId);
        return MapSystem(created, withLevels: false);
    }

    public async Task<CultivationSystemDto> UpdateAsync(UpdateCultivationSystemDto updateDto, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.CultivationSystems.GetByIdAsync(updateDto.Id, cancellationToken)
            ?? throw new ArgumentException($"修炼体系不存在，ID: {updateDto.Id}");
        entity.Name = updateDto.Name;
        entity.Type = updateDto.Type;
        entity.Description = updateDto.Description;
        entity.CultivationMethod = updateDto.CultivationMethod;
        entity.RealmDivision = updateDto.RealmDivision;
        entity.BreakthroughConditions = updateDto.BreakthroughConditions;
        entity.CultivationResources = updateDto.CultivationResources;
        entity.Characteristics = updateDto.Characteristics;
        entity.Risks = updateDto.Risks;
        entity.Importance = updateDto.Importance;
        entity.ImagePath = updateDto.ImagePath;
        entity.Tags = updateDto.Tags;
        entity.Notes = updateDto.Notes;
        entity.Status = updateDto.Status;
        entity.Order = updateDto.OrderIndex;
        var updated = await _unitOfWork.CultivationSystems.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapSystem(updated, withLevels: false);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.CultivationSystems.GetByIdAsync(id, cancellationToken);
        if (entity == null) return false;
        await _unitOfWork.CultivationSystems.DeleteAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<CultivationSystemDto> CopyAsync(Guid id, string newName, CancellationToken cancellationToken = default)
    {
        var source = await _unitOfWork.CultivationSystems.GetWithLevelsAsync(id, cancellationToken)
            ?? throw new ArgumentException($"修炼体系不存在，ID: {id}");
        var copy = await CreateAsync(new CreateCultivationSystemDto
        {
            Name = newName,
            Type = source.Type,
            Description = source.Description,
            CultivationMethod = source.CultivationMethod,
            RealmDivision = source.RealmDivision,
            BreakthroughConditions = source.BreakthroughConditions,
            CultivationResources = source.CultivationResources,
            Characteristics = source.Characteristics,
            Risks = source.Risks,
            ProjectId = source.ProjectId,
            Importance = source.Importance,
            ImagePath = source.ImagePath,
            Tags = source.Tags,
            Notes = source.Notes,
            Status = source.Status,
            OrderIndex = source.Order
        }, cancellationToken);
        foreach (var level in source.Levels.OrderBy(l => l.Order))
        {
            await CreateLevelAsync(new CreateCultivationLevelDto
            {
                CultivationSystemId = copy.Id,
                Name = level.Name,
                OrderIndex = level.Order,
                Description = level.Description,
                BreakthroughCondition = level.BreakthroughCondition,
                Abilities = level.Abilities,
                CultivationTime = level.CultivationTime,
                Tags = level.Tags,
                Notes = level.Notes
            }, cancellationToken);
        }
        return copy;
    }

    public async Task<IEnumerable<string>> GetTypesAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var systems = await _unitOfWork.CultivationSystems.GetByProjectIdAsync(projectId, cancellationToken);
        return systems.Select(s => s.Type).Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().ToList();
    }

    public async Task<IEnumerable<CultivationLevelDto>> GetLevelsAsync(Guid cultivationSystemId, CancellationToken cancellationToken = default)
    {
        var levels = await _unitOfWork.CultivationLevels.GetBySystemIdAsync(cultivationSystemId, cancellationToken);
        return levels.OrderBy(l => l.Order).Select(MapLevel).ToList();
    }

    public async Task<CultivationLevelDto?> GetLevelByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.CultivationLevels.GetByIdAsync(id, cancellationToken);
        return e == null ? null : MapLevel(e);
    }

    public async Task<CultivationLevelDto> CreateLevelAsync(CreateCultivationLevelDto createDto, CancellationToken cancellationToken = default)
    {
        var entity = new CultivationLevel
        {
            Name = createDto.Name,
            Order = createDto.OrderIndex,
            Description = createDto.Description,
            BreakthroughCondition = createDto.BreakthroughCondition,
            Abilities = createDto.Abilities,
            CultivationTime = createDto.CultivationTime,
            CultivationSystemId = createDto.CultivationSystemId,
            Tags = createDto.Tags,
            Notes = createDto.Notes
        };
        var created = await _unitOfWork.CultivationLevels.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapLevel(created);
    }

    public async Task<CultivationLevelDto> UpdateLevelAsync(UpdateCultivationLevelDto updateDto, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.CultivationLevels.GetByIdAsync(updateDto.Id, cancellationToken)
            ?? throw new ArgumentException($"修炼等级不存在，ID: {updateDto.Id}");
        entity.Name = updateDto.Name;
        entity.Order = updateDto.OrderIndex;
        entity.Description = updateDto.Description;
        entity.BreakthroughCondition = updateDto.BreakthroughCondition;
        entity.Abilities = updateDto.Abilities;
        entity.CultivationTime = updateDto.CultivationTime;
        entity.Tags = updateDto.Tags;
        entity.Notes = updateDto.Notes;
        var updated = await _unitOfWork.CultivationLevels.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapLevel(updated);
    }

    public async Task<bool> DeleteLevelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.CultivationLevels.GetByIdAsync(id, cancellationToken);
        if (entity == null) return false;
        await _unitOfWork.CultivationLevels.DeleteAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<CultivationLevelDto?> GetNextLevelAsync(Guid currentLevelId, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.CultivationLevels.GetNextLevelAsync(currentLevelId, cancellationToken);
        return e == null ? null : MapLevel(e);
    }

    public async Task<CultivationLevelDto?> GetPreviousLevelAsync(Guid currentLevelId, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.CultivationLevels.GetPreviousLevelAsync(currentLevelId, cancellationToken);
        return e == null ? null : MapLevel(e);
    }

    #endregion
}
