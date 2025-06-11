using Microsoft.Extensions.Logging;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;

namespace NovelManagement.Application.Services;

/// <summary>
/// 关系网络服务
/// </summary>
public class RelationshipNetworkService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RelationshipNetworkService> _logger;

    public RelationshipNetworkService(IUnitOfWork unitOfWork, ILogger<RelationshipNetworkService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// 创建关系
    /// </summary>
    public async Task<RelationshipNetwork> CreateRelationshipAsync(RelationshipNetwork relationshipNetwork, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始创建关系网络: {NetworkName}, 项目ID: {ProjectId}", relationshipNetwork.Name, relationshipNetwork.ProjectId);
            
            // 设置创建时间
            relationshipNetwork.CreatedAt = DateTime.UtcNow;
            relationshipNetwork.UpdatedAt = DateTime.UtcNow;
            
            await _unitOfWork.RelationshipNetworks.AddAsync(relationshipNetwork, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("成功创建关系网络: {NetworkName}, ID: {NetworkId}", relationshipNetwork.Name, relationshipNetwork.Id);
            return relationshipNetwork;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建关系网络时发生错误: {NetworkName}", relationshipNetwork.Name);
            throw;
        }
    }

    /// <summary>
    /// 更新关系
    /// </summary>
    public async Task<RelationshipNetwork> UpdateRelationshipAsync(RelationshipNetwork relationshipNetwork, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始更新关系网络: {NetworkName}, ID: {NetworkId}", relationshipNetwork.Name, relationshipNetwork.Id);
            
            // 设置更新时间
            relationshipNetwork.UpdatedAt = DateTime.UtcNow;
            
            await _unitOfWork.RelationshipNetworks.UpdateAsync(relationshipNetwork, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("成功更新关系网络: {NetworkName}, ID: {NetworkId}", relationshipNetwork.Name, relationshipNetwork.Id);
            return relationshipNetwork;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新关系网络时发生错误: {NetworkName}, ID: {NetworkId}", relationshipNetwork.Name, relationshipNetwork.Id);
            throw;
        }
    }

    /// <summary>
    /// 根据ID获取关系网络
    /// </summary>
    public async Task<RelationshipNetwork?> GetRelationshipNetworkByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取关系网络，ID: {NetworkId}", id);
            var network = await _unitOfWork.RelationshipNetworks.GetByIdAsync(id, cancellationToken);
            if (network == null)
            {
                _logger.LogWarning("未找到关系网络，ID: {NetworkId}", id);
            }
            return network;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取关系网络时发生错误，ID: {NetworkId}", id);
            throw;
        }
    }

    /// <summary>
    /// 获取关系网络
    /// </summary>
    public async Task<IEnumerable<RelationshipNetwork>> GetRelationshipNetworkAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取项目关系网络列表，项目ID: {ProjectId}", projectId);
            
            var networks = await _unitOfWork.RelationshipNetworks.GetByProjectIdAsync(projectId, cancellationToken);
            var orderedNetworks = networks.OrderBy(rn => rn.Name).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个关系网络", orderedNetworks.Count);
            return orderedNetworks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取项目关系网络列表时发生错误，项目ID: {ProjectId}", projectId);
            throw;
        }
    }

    /// <summary>
    /// 根据类型获取关系网络
    /// </summary>
    public async Task<IEnumerable<RelationshipNetwork>> GetRelationshipNetworksByTypeAsync(Guid projectId, string type, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始按类型获取关系网络，项目ID: {ProjectId}, 类型: {Type}", projectId, type);
            
            var networks = await _unitOfWork.RelationshipNetworks.GetByTypeAsync(projectId, type, cancellationToken);
            var orderedNetworks = networks.OrderBy(rn => rn.Name).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个 {Type} 类型的关系网络", orderedNetworks.Count, type);
            return orderedNetworks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按类型获取关系网络时发生错误，项目ID: {ProjectId}, 类型: {Type}", projectId, type);
            throw;
        }
    }

    /// <summary>
    /// 根据中心人物获取关系网络
    /// </summary>
    public async Task<IEnumerable<RelationshipNetwork>> GetRelationshipNetworksByCentralCharacterAsync(Guid centralCharacterId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始按中心人物获取关系网络，角色ID: {CharacterId}", centralCharacterId);
            
            var networks = await _unitOfWork.RelationshipNetworks.GetByCentralCharacterAsync(centralCharacterId, cancellationToken);
            var orderedNetworks = networks.OrderBy(rn => rn.Name).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个以该角色为中心的关系网络", orderedNetworks.Count);
            return orderedNetworks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按中心人物获取关系网络时发生错误，角色ID: {CharacterId}", centralCharacterId);
            throw;
        }
    }

    /// <summary>
    /// 根据复杂度范围获取关系网络
    /// </summary>
    public async Task<IEnumerable<RelationshipNetwork>> GetRelationshipNetworksByComplexityRangeAsync(Guid projectId, int minComplexity, int maxComplexity, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始按复杂度获取关系网络，项目ID: {ProjectId}, 复杂度范围: {MinComplexity}-{MaxComplexity}", projectId, minComplexity, maxComplexity);
            
            var networks = await _unitOfWork.RelationshipNetworks.GetByComplexityRangeAsync(projectId, minComplexity, maxComplexity, cancellationToken);
            var orderedNetworks = networks.OrderByDescending(rn => rn.Complexity).ThenBy(rn => rn.Name).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个复杂度在 {MinComplexity}-{MaxComplexity} 范围内的关系网络", orderedNetworks.Count, minComplexity, maxComplexity);
            return orderedNetworks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按复杂度获取关系网络时发生错误，项目ID: {ProjectId}, 复杂度范围: {MinComplexity}-{MaxComplexity}", projectId, minComplexity, maxComplexity);
            throw;
        }
    }

    /// <summary>
    /// 分析关系
    /// </summary>
    public async Task<Dictionary<string, object>> AnalyzeRelationshipAsync(Guid networkId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始分析关系网络，网络ID: {NetworkId}", networkId);
            
            var network = await _unitOfWork.RelationshipNetworks.GetCompleteNetworkAsync(networkId, cancellationToken);
            if (network == null)
            {
                throw new ArgumentException($"关系网络不存在，ID: {networkId}");
            }

            var analysis = new Dictionary<string, object>
            {
                ["NetworkName"] = network.Name,
                ["Type"] = network.Type,
                ["Status"] = network.Status,
                ["Complexity"] = network.Complexity,
                ["Influence"] = network.Influence,
                ["MemberCount"] = network.MemberCount,
                ["Importance"] = network.Importance,
                ["CentralCharacter"] = network.CentralCharacter?.Name ?? "无",
                ["CreatedAt"] = network.CreatedAt,
                ["UpdatedAt"] = network.UpdatedAt
            };

            // 如果有成员信息，添加成员分析
            if (network.Members?.Any() == true)
            {
                analysis["ActualMemberCount"] = network.Members.Count();
                analysis["MemberNames"] = network.Members.Select(m => m.Name).ToList();
            }

            // 如果有关系信息，添加关系分析
            if (network.Relationships?.Any() == true)
            {
                var relationshipTypes = network.Relationships
                    .GroupBy(r => r.RelationshipType)
                    .ToDictionary(g => g.Key, g => g.Count());
                analysis["RelationshipTypes"] = relationshipTypes;
                analysis["TotalRelationships"] = network.Relationships.Count();
            }
            
            _logger.LogInformation("成功分析关系网络，网络ID: {NetworkId}", networkId);
            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分析关系网络时发生错误，网络ID: {NetworkId}", networkId);
            throw;
        }
    }

    /// <summary>
    /// 删除关系网络
    /// </summary>
    public async Task<bool> DeleteRelationshipNetworkAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始删除关系网络，ID: {NetworkId}", id);
            
            var network = await _unitOfWork.RelationshipNetworks.GetByIdAsync(id, cancellationToken);
            if (network == null)
            {
                _logger.LogWarning("要删除的关系网络不存在，ID: {NetworkId}", id);
                return false;
            }
            
            await _unitOfWork.RelationshipNetworks.DeleteAsync(network, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("成功删除关系网络，ID: {NetworkId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除关系网络时发生错误，ID: {NetworkId}", id);
            throw;
        }
    }

    /// <summary>
    /// 搜索关系网络
    /// </summary>
    public async Task<IEnumerable<RelationshipNetwork>> SearchRelationshipNetworksAsync(Guid projectId, string searchTerm, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始搜索关系网络，项目ID: {ProjectId}, 关键词: {SearchTerm}", projectId, searchTerm);
            
            var networks = await _unitOfWork.RelationshipNetworks.SearchAsync(projectId, searchTerm, cancellationToken);
            var orderedNetworks = networks.OrderBy(rn => rn.Name).ToList();
            
            _logger.LogInformation("搜索完成，找到 {Count} 个匹配的关系网络", orderedNetworks.Count);
            return orderedNetworks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索关系网络时发生错误，项目ID: {ProjectId}, 关键词: {SearchTerm}", projectId, searchTerm);
            throw;
        }
    }

    /// <summary>
    /// 获取角色参与的关系网络
    /// </summary>
    public async Task<IEnumerable<RelationshipNetwork>> GetRelationshipNetworksByCharacterAsync(Guid characterId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取角色参与的关系网络，角色ID: {CharacterId}", characterId);
            
            var networks = await _unitOfWork.RelationshipNetworks.GetByCharacterAsync(characterId, cancellationToken);
            var orderedNetworks = networks.OrderBy(rn => rn.Name).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个该角色参与的关系网络", orderedNetworks.Count);
            return orderedNetworks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取角色参与的关系网络时发生错误，角色ID: {CharacterId}", characterId);
            throw;
        }
    }

    /// <summary>
    /// 获取关系网络统计信息
    /// </summary>
    public async Task<Dictionary<string, object>> GetNetworkStatisticsAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取关系网络统计信息，项目ID: {ProjectId}", projectId);
            
            var statistics = await _unitOfWork.RelationshipNetworks.GetNetworkStatisticsAsync(projectId, cancellationToken);
            
            _logger.LogInformation("成功获取关系网络统计信息，项目ID: {ProjectId}", projectId);
            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取关系网络统计信息时发生错误，项目ID: {ProjectId}", projectId);
            throw;
        }
    }

    /// <summary>
    /// 获取关系网络及其成员
    /// </summary>
    public async Task<RelationshipNetwork?> GetRelationshipNetworkWithMembersAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取关系网络及其成员，ID: {NetworkId}", id);
            
            var network = await _unitOfWork.RelationshipNetworks.GetWithMembersAsync(id, cancellationToken);
            if (network == null)
            {
                _logger.LogWarning("未找到关系网络，ID: {NetworkId}", id);
            }
            
            return network;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取关系网络及其成员时发生错误，ID: {NetworkId}", id);
            throw;
        }
    }

    /// <summary>
    /// 获取关系网络及其关系
    /// </summary>
    public async Task<RelationshipNetwork?> GetRelationshipNetworkWithRelationshipsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取关系网络及其关系，ID: {NetworkId}", id);
            
            var network = await _unitOfWork.RelationshipNetworks.GetWithRelationshipsAsync(id, cancellationToken);
            if (network == null)
            {
                _logger.LogWarning("未找到关系网络，ID: {NetworkId}", id);
            }
            
            return network;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取关系网络及其关系时发生错误，ID: {NetworkId}", id);
            throw;
        }
    }
}
