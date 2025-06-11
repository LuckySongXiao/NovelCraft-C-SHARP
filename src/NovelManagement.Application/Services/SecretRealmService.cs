using Microsoft.Extensions.Logging;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;

namespace NovelManagement.Application.Services;

/// <summary>
/// 秘境管理服务
/// </summary>
public class SecretRealmService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SecretRealmService> _logger;

    public SecretRealmService(IUnitOfWork unitOfWork, ILogger<SecretRealmService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// 创建秘境
    /// </summary>
    public async Task<SecretRealm> CreateSecretRealmAsync(SecretRealm secretRealm, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始创建秘境: {SecretRealmName}, 项目ID: {ProjectId}", secretRealm.Name, secretRealm.ProjectId);
            
            // 设置创建时间
            secretRealm.CreatedAt = DateTime.UtcNow;
            secretRealm.UpdatedAt = DateTime.UtcNow;
            
            await _unitOfWork.SecretRealms.AddAsync(secretRealm, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("成功创建秘境: {SecretRealmName}, ID: {SecretRealmId}", secretRealm.Name, secretRealm.Id);
            return secretRealm;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建秘境时发生错误: {SecretRealmName}", secretRealm.Name);
            throw;
        }
    }

    /// <summary>
    /// 更新秘境信息
    /// </summary>
    public async Task<SecretRealm> UpdateSecretRealmAsync(SecretRealm secretRealm, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始更新秘境: {SecretRealmName}, ID: {SecretRealmId}", secretRealm.Name, secretRealm.Id);
            
            // 设置更新时间
            secretRealm.UpdatedAt = DateTime.UtcNow;
            
            await _unitOfWork.SecretRealms.UpdateAsync(secretRealm, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("成功更新秘境: {SecretRealmName}, ID: {SecretRealmId}", secretRealm.Name, secretRealm.Id);
            return secretRealm;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新秘境时发生错误: {SecretRealmName}, ID: {SecretRealmId}", secretRealm.Name, secretRealm.Id);
            throw;
        }
    }

    /// <summary>
    /// 根据ID获取秘境
    /// </summary>
    public async Task<SecretRealm?> GetSecretRealmByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取秘境，ID: {SecretRealmId}", id);
            var secretRealm = await _unitOfWork.SecretRealms.GetByIdAsync(id, cancellationToken);
            if (secretRealm == null)
            {
                _logger.LogWarning("未找到秘境，ID: {SecretRealmId}", id);
            }
            return secretRealm;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取秘境时发生错误，ID: {SecretRealmId}", id);
            throw;
        }
    }

    /// <summary>
    /// 根据项目ID获取所有秘境
    /// </summary>
    public async Task<IEnumerable<SecretRealm>> GetSecretRealmsByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取项目秘境列表，项目ID: {ProjectId}", projectId);
            
            var secretRealms = await _unitOfWork.SecretRealms.GetByProjectIdAsync(projectId, cancellationToken);
            var orderedSecretRealms = secretRealms.OrderBy(sr => sr.Name).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个秘境", orderedSecretRealms.Count);
            return orderedSecretRealms;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取项目秘境列表时发生错误，项目ID: {ProjectId}", projectId);
            throw;
        }
    }

    /// <summary>
    /// 按类型获取秘境
    /// </summary>
    public async Task<IEnumerable<SecretRealm>> GetSecretRealmsByTypeAsync(Guid projectId, string type, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始按类型获取秘境，项目ID: {ProjectId}, 类型: {Type}", projectId, type);
            
            var secretRealms = await _unitOfWork.SecretRealms.GetByTypeAsync(projectId, type, cancellationToken);
            var orderedSecretRealms = secretRealms.OrderBy(sr => sr.Name).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个 {Type} 类型的秘境", orderedSecretRealms.Count, type);
            return orderedSecretRealms;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按类型获取秘境时发生错误，项目ID: {ProjectId}, 类型: {Type}", projectId, type);
            throw;
        }
    }

    /// <summary>
    /// 根据危险等级范围获取秘境
    /// </summary>
    public async Task<IEnumerable<SecretRealm>> GetSecretRealmsByDangerLevelRangeAsync(Guid projectId, int minDangerLevel, int maxDangerLevel, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始按危险等级获取秘境，项目ID: {ProjectId}, 危险等级范围: {MinDangerLevel}-{MaxDangerLevel}", projectId, minDangerLevel, maxDangerLevel);
            
            var secretRealms = await _unitOfWork.SecretRealms.GetByDangerLevelRangeAsync(projectId, minDangerLevel, maxDangerLevel, cancellationToken);
            var orderedSecretRealms = secretRealms.OrderByDescending(sr => sr.DangerLevel).ThenBy(sr => sr.Name).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个危险等级在 {MinDangerLevel}-{MaxDangerLevel} 范围内的秘境", orderedSecretRealms.Count, minDangerLevel, maxDangerLevel);
            return orderedSecretRealms;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按危险等级获取秘境时发生错误，项目ID: {ProjectId}, 危险等级范围: {MinDangerLevel}-{MaxDangerLevel}", projectId, minDangerLevel, maxDangerLevel);
            throw;
        }
    }

    /// <summary>
    /// 根据推荐修为获取秘境
    /// </summary>
    public async Task<IEnumerable<SecretRealm>> GetSecretRealmsByRecommendedCultivationAsync(Guid projectId, string recommendedCultivation, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始按推荐修为获取秘境，项目ID: {ProjectId}, 推荐修为: {RecommendedCultivation}", projectId, recommendedCultivation);
            
            var secretRealms = await _unitOfWork.SecretRealms.GetByRecommendedCultivationAsync(projectId, recommendedCultivation, cancellationToken);
            var orderedSecretRealms = secretRealms.OrderBy(sr => sr.Name).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个推荐修为为 {RecommendedCultivation} 的秘境", orderedSecretRealms.Count, recommendedCultivation);
            return orderedSecretRealms;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按推荐修为获取秘境时发生错误，项目ID: {ProjectId}, 推荐修为: {RecommendedCultivation}", projectId, recommendedCultivation);
            throw;
        }
    }

    /// <summary>
    /// 根据发现者势力获取秘境
    /// </summary>
    public async Task<IEnumerable<SecretRealm>> GetSecretRealmsByDiscovererFactionAsync(Guid factionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始按发现者势力获取秘境，势力ID: {FactionId}", factionId);
            
            var secretRealms = await _unitOfWork.SecretRealms.GetByDiscovererFactionAsync(factionId, cancellationToken);
            var orderedSecretRealms = secretRealms.OrderBy(sr => sr.Name).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个由该势力发现的秘境", orderedSecretRealms.Count);
            return orderedSecretRealms;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按发现者势力获取秘境时发生错误，势力ID: {FactionId}", factionId);
            throw;
        }
    }

    /// <summary>
    /// 根据位置获取秘境
    /// </summary>
    public async Task<IEnumerable<SecretRealm>> GetSecretRealmsByLocationAsync(Guid projectId, string location, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始按位置获取秘境，项目ID: {ProjectId}, 位置: {Location}", projectId, location);
            
            var secretRealms = await _unitOfWork.SecretRealms.GetByLocationAsync(projectId, location, cancellationToken);
            var orderedSecretRealms = secretRealms.OrderBy(sr => sr.Name).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个位于 {Location} 的秘境", orderedSecretRealms.Count, location);
            return orderedSecretRealms;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按位置获取秘境时发生错误，项目ID: {ProjectId}, 位置: {Location}", projectId, location);
            throw;
        }
    }

    /// <summary>
    /// 删除秘境
    /// </summary>
    public async Task<bool> DeleteSecretRealmAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始删除秘境，ID: {SecretRealmId}", id);
            
            var secretRealm = await _unitOfWork.SecretRealms.GetByIdAsync(id, cancellationToken);
            if (secretRealm == null)
            {
                _logger.LogWarning("要删除的秘境不存在，ID: {SecretRealmId}", id);
                return false;
            }
            
            await _unitOfWork.SecretRealms.DeleteAsync(secretRealm, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("成功删除秘境，ID: {SecretRealmId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除秘境时发生错误，ID: {SecretRealmId}", id);
            throw;
        }
    }

    /// <summary>
    /// 搜索秘境
    /// </summary>
    public async Task<IEnumerable<SecretRealm>> SearchSecretRealmsAsync(Guid projectId, string searchTerm, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始搜索秘境，项目ID: {ProjectId}, 关键词: {SearchTerm}", projectId, searchTerm);
            
            var secretRealms = await _unitOfWork.SecretRealms.SearchAsync(projectId, searchTerm, cancellationToken);
            var orderedSecretRealms = secretRealms.OrderBy(sr => sr.Name).ToList();
            
            _logger.LogInformation("搜索完成，找到 {Count} 个匹配的秘境", orderedSecretRealms.Count);
            return orderedSecretRealms;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索秘境时发生错误，项目ID: {ProjectId}, 关键词: {SearchTerm}", projectId, searchTerm);
            throw;
        }
    }

    /// <summary>
    /// 获取秘境探索统计
    /// </summary>
    public async Task<Dictionary<string, object>> GetExplorationStatisticsAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取秘境探索统计，项目ID: {ProjectId}", projectId);
            
            var statistics = await _unitOfWork.SecretRealms.GetExplorationStatisticsAsync(projectId, cancellationToken);
            
            _logger.LogInformation("成功获取秘境探索统计，项目ID: {ProjectId}", projectId);
            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取秘境探索统计时发生错误，项目ID: {ProjectId}", projectId);
            throw;
        }
    }

    /// <summary>
    /// 获取秘境及其相关资源
    /// </summary>
    public async Task<SecretRealm?> GetSecretRealmWithResourcesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取秘境及其相关资源，ID: {SecretRealmId}", id);
            
            var secretRealm = await _unitOfWork.SecretRealms.GetWithResourcesAsync(id, cancellationToken);
            if (secretRealm == null)
            {
                _logger.LogWarning("未找到秘境，ID: {SecretRealmId}", id);
            }
            
            return secretRealm;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取秘境及其相关资源时发生错误，ID: {SecretRealmId}", id);
            throw;
        }
    }

    /// <summary>
    /// 获取秘境及其探索者
    /// </summary>
    public async Task<SecretRealm?> GetSecretRealmWithExplorersAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取秘境及其探索者，ID: {SecretRealmId}", id);
            
            var secretRealm = await _unitOfWork.SecretRealms.GetWithExplorersAsync(id, cancellationToken);
            if (secretRealm == null)
            {
                _logger.LogWarning("未找到秘境，ID: {SecretRealmId}", id);
            }
            
            return secretRealm;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取秘境及其探索者时发生错误，ID: {SecretRealmId}", id);
            throw;
        }
    }
}
