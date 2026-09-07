using NovelManagement.Application.DTOs;
using NovelManagement.Application.Interfaces;

namespace NovelManagement.Tests;

/// <summary>
/// 修炼体系服务内存 Fake：记录创建调用，供解析逻辑单元测试断言落库结果
/// </summary>
internal sealed class FakeCultivationSystemService : ICultivationSystemService
{
    public List<CreateCultivationSystemDto> CreatedSystems { get; } = new();
    public List<CreateCultivationLevelDto> CreatedLevels { get; } = new();

    /// <summary>当前内存体系（GetAllAsync/GetWithLevelsAsync 的数据源，测试可直接注入 Levels）</summary>
    public CultivationSystemDto? InternalSystem { get; private set; }

    public Task<CultivationSystemDto> CreateAsync(CreateCultivationSystemDto createDto, CancellationToken cancellationToken = default)
    {
        CreatedSystems.Add(createDto);
        InternalSystem = new CultivationSystemDto
        {
            Id = Guid.NewGuid(),
            Name = createDto.Name,
            Type = createDto.Type,
            Description = createDto.Description,
            CultivationMethod = createDto.CultivationMethod,
            RealmDivision = createDto.RealmDivision,
            ProjectId = createDto.ProjectId,
            Importance = createDto.Importance,
            Levels = new List<CultivationLevelDto>()
        };
        return Task.FromResult(InternalSystem);
    }

    public Task<CultivationLevelDto> CreateLevelAsync(CreateCultivationLevelDto createDto, CancellationToken cancellationToken = default)
    {
        CreatedLevels.Add(createDto);
        return Task.FromResult(new CultivationLevelDto
        {
            Id = Guid.NewGuid(),
            Name = createDto.Name,
            OrderIndex = createDto.OrderIndex,
            Description = createDto.Description,
            BreakthroughCondition = createDto.BreakthroughCondition,
            Abilities = createDto.Abilities,
            CultivationSystemId = createDto.CultivationSystemId
        });
    }

    public Task<IEnumerable<CultivationSystemDto>> GetAllAsync(Guid projectId, CancellationToken cancellationToken = default)
        => Task.FromResult(InternalSystem is null
            ? Enumerable.Empty<CultivationSystemDto>()
            : new[] { InternalSystem }.AsEnumerable());

    public Task<CultivationSystemDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(InternalSystem?.Id == id ? InternalSystem : null);

    public Task<CultivationSystemDto?> GetWithLevelsAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(InternalSystem?.Id == id ? InternalSystem : null);

    public Task<IEnumerable<CultivationSystemDto>> GetByTypeAsync(Guid projectId, string type, CancellationToken cancellationToken = default)
        => Task.FromResult(Enumerable.Empty<CultivationSystemDto>());

    public Task<IEnumerable<CultivationSystemDto>> GetByImportanceAsync(Guid projectId, int minImportance, CancellationToken cancellationToken = default)
        => Task.FromResult(Enumerable.Empty<CultivationSystemDto>());

    public Task<IEnumerable<CultivationSystemDto>> SearchAsync(Guid projectId, string searchTerm, CancellationToken cancellationToken = default)
        => Task.FromResult(Enumerable.Empty<CultivationSystemDto>());

    public Task<CultivationSystemDto> UpdateAsync(UpdateCultivationSystemDto updateDto, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<CultivationSystemDto> CopyAsync(Guid id, string newName, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<IEnumerable<string>> GetTypesAsync(Guid projectId, CancellationToken cancellationToken = default)
        => Task.FromResult(Enumerable.Empty<string>());

    public Task<IEnumerable<CultivationLevelDto>> GetLevelsAsync(Guid cultivationSystemId, CancellationToken cancellationToken = default)
        => Task.FromResult(Enumerable.Empty<CultivationLevelDto>());

    public Task<CultivationLevelDto?> GetLevelByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult<CultivationLevelDto?>(null);

    public Task<CultivationLevelDto> UpdateLevelAsync(UpdateCultivationLevelDto updateDto, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<bool> DeleteLevelAsync(Guid id, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<CultivationLevelDto?> GetNextLevelAsync(Guid currentLevelId, CancellationToken cancellationToken = default)
        => Task.FromResult<CultivationLevelDto?>(null);

    public Task<CultivationLevelDto?> GetPreviousLevelAsync(Guid currentLevelId, CancellationToken cancellationToken = default)
        => Task.FromResult<CultivationLevelDto?>(null);
}
