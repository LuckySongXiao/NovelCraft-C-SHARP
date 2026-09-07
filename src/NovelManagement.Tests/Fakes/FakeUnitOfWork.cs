using NovelManagement.Core.Interfaces;

namespace NovelManagement.Tests.Fakes;

/// <summary>
/// 工作单元内存 Fake：Characters/Plots/Volumes/Chapters 与 SaveChanges 生效，其余仓储不参与测试路径
/// </summary>
internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public FakeCharacterRepository CharacterRepo { get; } = new();
    public FakePlotRepository PlotRepo { get; } = new();
    public FakeVolumeRepository VolumeRepo { get; } = new();
    public FakeChapterRepository ChapterRepo { get; } = new();

    public FakeUnitOfWork()
    {
        ChapterRepo.VolumeSource = VolumeRepo.Volumes; // 章节按项目过滤需联动卷数据
    }

    public int SaveChangesCount { get; private set; }

    public IProjectRepository Projects => null!;
    public IVolumeRepository Volumes => VolumeRepo;
    public IChapterRepository Chapters => ChapterRepo;
    public ICharacterRepository Characters => CharacterRepo;
    public IFactionRepository Factions => null!;
    public ICharacterRelationshipRepository CharacterRelationships => null!;
    public ICharacterEventRepository CharacterEvents => null!;
    public IFactionRelationshipRepository FactionRelationships => null!;
    public IWorldSettingRepository WorldSettings => null!;
    public ICultivationSystemRepository CultivationSystems => null!;
    public ICultivationLevelRepository CultivationLevels => null!;
    public IPoliticalSystemRepository PoliticalSystems => null!;
    public IPoliticalPositionRepository PoliticalPositions => null!;
    public IPlotRepository Plots => PlotRepo;
    public IResourceRepository Resources => null!;
    public IRaceRepository Races => null!;
    public IRaceRelationshipRepository RaceRelationships => null!;
    public ISecretRealmRepository SecretRealms => null!;
    public IRelationshipNetworkRepository RelationshipNetworks => null!;
    public ICurrencySystemRepository CurrencySystems => null!;
    public ITimelineEventRepository TimelineEvents => null!;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCount++;
        return Task.FromResult(SaveChangesCount);
    }

    public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task CommitTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public void Dispose() { }
}
