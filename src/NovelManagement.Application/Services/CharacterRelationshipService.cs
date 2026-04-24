using Microsoft.Extensions.Logging;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;

namespace NovelManagement.Application.Services;

/// <summary>
/// 角色关系服务
/// </summary>
public class CharacterRelationshipService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CharacterRelationshipService> _logger;

    public CharacterRelationshipService(IUnitOfWork unitOfWork, ILogger<CharacterRelationshipService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CharacterRelationship> CreateCharacterRelationshipAsync(CharacterRelationship relationship, CancellationToken cancellationToken = default)
    {
        try
        {
            relationship.CreatedAt = DateTime.UtcNow;
            relationship.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.CharacterRelationships.AddAsync(relationship, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return relationship;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建角色关系失败");
            throw;
        }
    }

    public async Task<CharacterRelationship> UpdateCharacterRelationshipAsync(CharacterRelationship relationship, CancellationToken cancellationToken = default)
    {
        try
        {
            relationship.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.CharacterRelationships.UpdateAsync(relationship, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return relationship;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新角色关系失败，RelationshipId: {RelationshipId}", relationship.Id);
            throw;
        }
    }

    public async Task<CharacterRelationship?> GetCharacterRelationshipByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _unitOfWork.CharacterRelationships.GetByIdAsync(id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取角色关系失败，RelationshipId: {RelationshipId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteCharacterRelationshipAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var relationship = await _unitOfWork.CharacterRelationships.GetByIdAsync(id, cancellationToken);
            if (relationship == null)
            {
                return false;
            }

            await _unitOfWork.CharacterRelationships.DeleteAsync(relationship, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除角色关系失败，RelationshipId: {RelationshipId}", id);
            throw;
        }
    }
}
