using Microsoft.EntityFrameworkCore.Storage;
using NovelManagement.Core.Interfaces;
using NovelManagement.Infrastructure.Data.Repositories;

namespace NovelManagement.Infrastructure.Data;

/// <summary>
/// 工作单元实现
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly NovelManagementDbContext _context;
    private IDbContextTransaction? _transaction;

    private IProjectRepository? _projects;
    private IVolumeRepository? _volumes;
    private IChapterRepository? _chapters;
    private ICharacterRepository? _characters;
    private IFactionRepository? _factions;
    private ICharacterRelationshipRepository? _characterRelationships;
    private IFactionRelationshipRepository? _factionRelationships;
    private IWorldSettingRepository? _worldSettings;
    private ICultivationSystemRepository? _cultivationSystems;
    private ICultivationLevelRepository? _cultivationLevels;
    private IPoliticalSystemRepository? _politicalSystems;
    private IPoliticalPositionRepository? _politicalPositions;
    private IPlotRepository? _plots;
    private IResourceRepository? _resources;
    private IRaceRepository? _races;
    private IRaceRelationshipRepository? _raceRelationships;
    private ISecretRealmRepository? _secretRealms;
    private IRelationshipNetworkRepository? _relationshipNetworks;
    private ICurrencySystemRepository? _currencySystems;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="context">数据库上下文</param>
    public UnitOfWork(NovelManagementDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// 项目仓储
    /// </summary>
    public IProjectRepository Projects => _projects ??= new ProjectRepository(_context);

    /// <summary>
    /// 卷宗仓储
    /// </summary>
    public IVolumeRepository Volumes => _volumes ??= new VolumeRepository(_context);

    /// <summary>
    /// 章节仓储
    /// </summary>
    public IChapterRepository Chapters => _chapters ??= new ChapterRepository(_context);

    /// <summary>
    /// 角色仓储
    /// </summary>
    public ICharacterRepository Characters => _characters ??= new CharacterRepository(_context);

    /// <summary>
    /// 势力仓储
    /// </summary>
    public IFactionRepository Factions => _factions ??= new FactionRepository(_context);

    /// <summary>
    /// 角色关系仓储
    /// </summary>
    public ICharacterRelationshipRepository CharacterRelationships => 
        _characterRelationships ??= new CharacterRelationshipRepository(_context);

    /// <summary>
    /// 势力关系仓储
    /// </summary>
    public IFactionRelationshipRepository FactionRelationships =>
        _factionRelationships ??= new FactionRelationshipRepository(_context);

    /// <summary>
    /// 世界设定仓储
    /// </summary>
    public IWorldSettingRepository WorldSettings =>
        _worldSettings ??= new WorldSettingRepository(_context);

    /// <summary>
    /// 修炼体系仓储
    /// </summary>
    public ICultivationSystemRepository CultivationSystems =>
        _cultivationSystems ??= new CultivationSystemRepository(_context);

    /// <summary>
    /// 修炼等级仓储
    /// </summary>
    public ICultivationLevelRepository CultivationLevels =>
        _cultivationLevels ??= new CultivationLevelRepository(_context);

    /// <summary>
    /// 政治体系仓储
    /// </summary>
    public IPoliticalSystemRepository PoliticalSystems =>
        _politicalSystems ??= new PoliticalSystemRepository(_context);

    /// <summary>
    /// 政治职位仓储
    /// </summary>
    public IPoliticalPositionRepository PoliticalPositions =>
        _politicalPositions ??= new PoliticalPositionRepository(_context);

    /// <summary>
    /// 剧情仓储
    /// </summary>
    public IPlotRepository Plots => _plots ??= new PlotRepository(_context);

    /// <summary>
    /// 资源仓储
    /// </summary>
    public IResourceRepository Resources => _resources ??= new ResourceRepository(_context);

    /// <summary>
    /// 种族仓储
    /// </summary>
    public IRaceRepository Races => _races ??= new RaceRepository(_context);

    /// <summary>
    /// 种族关系仓储
    /// </summary>
    public IRaceRelationshipRepository RaceRelationships =>
        _raceRelationships ??= new RaceRelationshipRepository(_context);

    /// <summary>
    /// 秘境仓储
    /// </summary>
    public ISecretRealmRepository SecretRealms => _secretRealms ??= new SecretRealmRepository(_context);

    /// <summary>
    /// 关系网络仓储
    /// </summary>
    public IRelationshipNetworkRepository RelationshipNetworks =>
        _relationshipNetworks ??= new RelationshipNetworkRepository(_context);

    /// <summary>
    /// 货币体系仓储
    /// </summary>
    public ICurrencySystemRepository CurrencySystems =>
        _currencySystems ??= new CurrencySystemRepository(_context);

    /// <summary>
    /// 保存更改
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>受影响的行数</returns>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// 开始事务
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    /// <summary>
    /// 提交事务
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    /// <summary>
    /// 回滚事务
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
