using Microsoft.Extensions.Logging;
using NovelManagement.Application.DTOs;
using NovelManagement.Application.Interfaces;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;

namespace NovelManagement.Application.Services;

/// <summary>
/// 世界设定服务实现
/// </summary>
public class WorldSettingService : IWorldSettingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WorldSettingService> _logger;

    public WorldSettingService(
        IUnitOfWork unitOfWork,
        ILogger<WorldSettingService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IEnumerable<WorldSettingDto>> GetAllAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var worldSettings = await _unitOfWork.WorldSettings.GetByProjectIdAsync(projectId, cancellationToken);
        return worldSettings.Select(MapToDto).ToList();
    }

    public async Task<WorldSettingDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var worldSetting = await _unitOfWork.WorldSettings.GetByIdAsync(id, cancellationToken);
        return worldSetting != null ? MapToDto(worldSetting) : null;
    }

    public async Task<IEnumerable<WorldSettingDto>> GetByTypeAsync(Guid projectId, string type, CancellationToken cancellationToken = default)
    {
        var worldSettings = await _unitOfWork.WorldSettings.GetByTypeAsync(projectId, type, cancellationToken);
        return worldSettings.Select(MapToDto).ToList();
    }

    public async Task<IEnumerable<WorldSettingDto>> GetByCategoryAsync(Guid projectId, string category, CancellationToken cancellationToken = default)
    {
        var worldSettings = await _unitOfWork.WorldSettings.GetByCategoryAsync(projectId, category, cancellationToken);
        return worldSettings.Select(MapToDto).ToList();
    }

    public async Task<WorldSettingDto?> GetWithChildrenAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var worldSetting = await _unitOfWork.WorldSettings.GetWithChildrenAsync(id, cancellationToken);
        return worldSetting != null ? MapToDto(worldSetting) : null;
    }

    public async Task<IEnumerable<WorldSettingDto>> GetRootSettingsAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var worldSettings = await _unitOfWork.WorldSettings.GetRootSettingsAsync(projectId, cancellationToken);
        return worldSettings.Select(MapToDto).ToList();
    }

    public async Task<IEnumerable<WorldSettingDto>> GetByImportanceAsync(Guid projectId, int minImportance, CancellationToken cancellationToken = default)
    {
        var worldSettings = await _unitOfWork.WorldSettings.GetByImportanceAsync(projectId, minImportance, cancellationToken);
        return worldSettings.Select(MapToDto).ToList();
    }

    public async Task<IEnumerable<WorldSettingDto>> SearchAsync(Guid projectId, string searchTerm, CancellationToken cancellationToken = default)
    {
        var worldSettings = await _unitOfWork.WorldSettings.SearchAsync(projectId, searchTerm, cancellationToken);
        return worldSettings.Select(MapToDto).ToList();
    }

    public async Task<WorldSettingDto> CreateAsync(CreateWorldSettingDto createDto, CancellationToken cancellationToken = default)
    {
        var worldSetting = MapToEntity(createDto);
        worldSetting.Id = Guid.NewGuid();
        worldSetting.CreatedAt = DateTime.UtcNow;
        worldSetting.UpdatedAt = DateTime.UtcNow;

        var createdWorldSetting = await _unitOfWork.WorldSettings.AddAsync(worldSetting, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(createdWorldSetting);
    }

    public async Task<WorldSettingDto> UpdateAsync(UpdateWorldSettingDto updateDto, CancellationToken cancellationToken = default)
    {
        var existingWorldSetting = await _unitOfWork.WorldSettings.GetByIdAsync(updateDto.Id, cancellationToken);
        if (existingWorldSetting == null)
        {
            throw new ArgumentException($"世界设定 {updateDto.Id} 不存在");
        }

        MapToExistingEntity(updateDto, existingWorldSetting);
        existingWorldSetting.UpdatedAt = DateTime.UtcNow;

        var updatedWorldSetting = await _unitOfWork.WorldSettings.UpdateAsync(existingWorldSetting, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(updatedWorldSetting);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var worldSetting = await _unitOfWork.WorldSettings.GetByIdAsync(id, cancellationToken);
        if (worldSetting == null)
        {
            return false;
        }

        await _unitOfWork.WorldSettings.DeleteAsync(worldSetting, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<int> DeleteBatchAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var deletedCount = 0;
        foreach (var id in ids)
        {
            if (await DeleteAsync(id, cancellationToken))
            {
                deletedCount++;
            }
        }
        return deletedCount;
    }

    public async Task<WorldSettingDto> CopyAsync(Guid id, string newName, CancellationToken cancellationToken = default)
    {
        var originalWorldSetting = await _unitOfWork.WorldSettings.GetByIdAsync(id, cancellationToken);
        if (originalWorldSetting == null)
        {
            throw new ArgumentException($"世界设定 {id} 不存在");
        }

        var copiedWorldSetting = new WorldSetting
        {
            Id = Guid.NewGuid(),
            Name = newName,
            Type = originalWorldSetting.Type,
            Category = originalWorldSetting.Category,
            Description = originalWorldSetting.Description,
            Content = originalWorldSetting.Content,
            Rules = originalWorldSetting.Rules,
            History = originalWorldSetting.History,
            RelatedSettings = originalWorldSetting.RelatedSettings,
            Importance = originalWorldSetting.Importance,
            ProjectId = originalWorldSetting.ProjectId,
            ParentId = originalWorldSetting.ParentId,
            ImagePath = originalWorldSetting.ImagePath,
            Tags = originalWorldSetting.Tags,
            Notes = originalWorldSetting.Notes,
            Status = originalWorldSetting.Status,
            Order = originalWorldSetting.Order,
            IsPublic = originalWorldSetting.IsPublic,
            Version = originalWorldSetting.Version,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createdWorldSetting = await _unitOfWork.WorldSettings.AddAsync(copiedWorldSetting, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(createdWorldSetting);
    }

    public async Task<WorldSettingDto> MoveAsync(Guid id, Guid? newParentId, CancellationToken cancellationToken = default)
    {
        var worldSetting = await _unitOfWork.WorldSettings.GetByIdAsync(id, cancellationToken);
        if (worldSetting == null)
        {
            throw new ArgumentException($"世界设定 {id} 不存在");
        }

        worldSetting.ParentId = newParentId;
        worldSetting.UpdatedAt = DateTime.UtcNow;

        var updatedWorldSetting = await _unitOfWork.WorldSettings.UpdateAsync(worldSetting, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(updatedWorldSetting);
    }

    public async Task<bool> UpdateOrderIndexAsync(Guid id, int newOrderIndex, CancellationToken cancellationToken = default)
    {
        var worldSetting = await _unitOfWork.WorldSettings.GetByIdAsync(id, cancellationToken);
        if (worldSetting == null)
        {
            return false;
        }

        worldSetting.Order = newOrderIndex;
        worldSetting.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.WorldSettings.UpdateAsync(worldSetting, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<IEnumerable<string>> GetTypesAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var worldSettings = await _unitOfWork.WorldSettings.GetByProjectIdAsync(projectId, cancellationToken);
        return worldSettings.Select(ws => ws.Type).Distinct().OrderBy(t => t);
    }

    public async Task<IEnumerable<string>> GetCategoriesAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var worldSettings = await _unitOfWork.WorldSettings.GetByProjectIdAsync(projectId, cancellationToken);
        return worldSettings.Where(ws => !string.IsNullOrEmpty(ws.Category))
                           .Select(ws => ws.Category!)
                           .Distinct()
                           .OrderBy(c => c);
    }

    private static WorldSettingDto MapToDto(WorldSetting entity)
    {
        return new WorldSettingDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Type = entity.Type,
            Category = entity.Category,
            Description = entity.Description,
            Content = entity.Content,
            Rules = entity.Rules,
            History = entity.History,
            RelatedSettings = entity.RelatedSettings,
            Importance = entity.Importance,
            ProjectId = entity.ProjectId,
            ParentId = entity.ParentId,
            ImagePath = entity.ImagePath,
            Tags = entity.Tags,
            Notes = entity.Notes,
            Status = entity.Status,
            OrderIndex = entity.Order,
            IsPublic = entity.IsPublic,
            Version = entity.Version,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            ParentName = entity.Parent?.Name,
            Children = entity.Children.Select(MapToDto).ToList()
        };
    }

    private static WorldSetting MapToEntity(CreateWorldSettingDto dto)
    {
        return new WorldSetting
        {
            Name = dto.Name,
            Type = dto.Type,
            Category = dto.Category,
            Description = dto.Description,
            Content = dto.Content,
            Rules = dto.Rules,
            History = dto.History,
            RelatedSettings = dto.RelatedSettings,
            Importance = dto.Importance,
            ProjectId = dto.ProjectId,
            ParentId = dto.ParentId,
            ImagePath = dto.ImagePath,
            Tags = dto.Tags,
            Notes = dto.Notes,
            Status = dto.Status,
            Order = dto.OrderIndex,
            IsPublic = dto.IsPublic,
            Version = dto.Version
        };
    }

    private static void MapToExistingEntity(UpdateWorldSettingDto dto, WorldSetting entity)
    {
        entity.Name = dto.Name;
        entity.Type = dto.Type;
        entity.Category = dto.Category;
        entity.Description = dto.Description;
        entity.Content = dto.Content;
        entity.Rules = dto.Rules;
        entity.History = dto.History;
        entity.RelatedSettings = dto.RelatedSettings;
        entity.Importance = dto.Importance;
        entity.ProjectId = dto.ProjectId;
        entity.ParentId = dto.ParentId;
        entity.ImagePath = dto.ImagePath;
        entity.Tags = dto.Tags;
        entity.Notes = dto.Notes;
        entity.Status = dto.Status;
        entity.Order = dto.OrderIndex;
        entity.IsPublic = dto.IsPublic;
        entity.Version = dto.Version;
    }
}
