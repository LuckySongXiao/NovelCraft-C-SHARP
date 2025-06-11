using AutoMapper;
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
    private readonly IMapper _mapper;
    private readonly ILogger<WorldSettingService> _logger;

    public WorldSettingService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<WorldSettingService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<WorldSettingDto>> GetAllAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var worldSettings = await _unitOfWork.WorldSettings.GetByProjectIdAsync(projectId, cancellationToken);
        return _mapper.Map<IEnumerable<WorldSettingDto>>(worldSettings);
    }

    public async Task<WorldSettingDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var worldSetting = await _unitOfWork.WorldSettings.GetByIdAsync(id, cancellationToken);
        return worldSetting != null ? _mapper.Map<WorldSettingDto>(worldSetting) : null;
    }

    public async Task<IEnumerable<WorldSettingDto>> GetByTypeAsync(Guid projectId, string type, CancellationToken cancellationToken = default)
    {
        var worldSettings = await _unitOfWork.WorldSettings.GetByTypeAsync(projectId, type, cancellationToken);
        return _mapper.Map<IEnumerable<WorldSettingDto>>(worldSettings);
    }

    public async Task<IEnumerable<WorldSettingDto>> GetByCategoryAsync(Guid projectId, string category, CancellationToken cancellationToken = default)
    {
        var worldSettings = await _unitOfWork.WorldSettings.GetByCategoryAsync(projectId, category, cancellationToken);
        return _mapper.Map<IEnumerable<WorldSettingDto>>(worldSettings);
    }

    public async Task<WorldSettingDto?> GetWithChildrenAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var worldSetting = await _unitOfWork.WorldSettings.GetWithChildrenAsync(id, cancellationToken);
        return worldSetting != null ? _mapper.Map<WorldSettingDto>(worldSetting) : null;
    }

    public async Task<IEnumerable<WorldSettingDto>> GetRootSettingsAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var worldSettings = await _unitOfWork.WorldSettings.GetRootSettingsAsync(projectId, cancellationToken);
        return _mapper.Map<IEnumerable<WorldSettingDto>>(worldSettings);
    }

    public async Task<IEnumerable<WorldSettingDto>> GetByImportanceAsync(Guid projectId, int minImportance, CancellationToken cancellationToken = default)
    {
        var worldSettings = await _unitOfWork.WorldSettings.GetByImportanceAsync(projectId, minImportance, cancellationToken);
        return _mapper.Map<IEnumerable<WorldSettingDto>>(worldSettings);
    }

    public async Task<IEnumerable<WorldSettingDto>> SearchAsync(Guid projectId, string searchTerm, CancellationToken cancellationToken = default)
    {
        var worldSettings = await _unitOfWork.WorldSettings.SearchAsync(projectId, searchTerm, cancellationToken);
        return _mapper.Map<IEnumerable<WorldSettingDto>>(worldSettings);
    }

    public async Task<WorldSettingDto> CreateAsync(CreateWorldSettingDto createDto, CancellationToken cancellationToken = default)
    {
        var worldSetting = _mapper.Map<WorldSetting>(createDto);
        worldSetting.Id = Guid.NewGuid();
        worldSetting.CreatedAt = DateTime.UtcNow;
        worldSetting.UpdatedAt = DateTime.UtcNow;

        var createdWorldSetting = await _unitOfWork.WorldSettings.AddAsync(worldSetting, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<WorldSettingDto>(createdWorldSetting);
    }

    public async Task<WorldSettingDto> UpdateAsync(UpdateWorldSettingDto updateDto, CancellationToken cancellationToken = default)
    {
        var existingWorldSetting = await _unitOfWork.WorldSettings.GetByIdAsync(updateDto.Id, cancellationToken);
        if (existingWorldSetting == null)
        {
            throw new ArgumentException($"世界设定 {updateDto.Id} 不存在");
        }

        _mapper.Map(updateDto, existingWorldSetting);
        existingWorldSetting.UpdatedAt = DateTime.UtcNow;

        var updatedWorldSetting = await _unitOfWork.WorldSettings.UpdateAsync(existingWorldSetting, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<WorldSettingDto>(updatedWorldSetting);
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

        return _mapper.Map<WorldSettingDto>(createdWorldSetting);
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

        return _mapper.Map<WorldSettingDto>(updatedWorldSetting);
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
}
