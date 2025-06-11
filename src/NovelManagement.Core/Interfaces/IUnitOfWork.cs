namespace NovelManagement.Core.Interfaces;

/// <summary>
/// 工作单元接口
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// 项目仓储
    /// </summary>
    IProjectRepository Projects { get; }

    /// <summary>
    /// 卷宗仓储
    /// </summary>
    IVolumeRepository Volumes { get; }

    /// <summary>
    /// 章节仓储
    /// </summary>
    IChapterRepository Chapters { get; }

    /// <summary>
    /// 角色仓储
    /// </summary>
    ICharacterRepository Characters { get; }

    /// <summary>
    /// 势力仓储
    /// </summary>
    IFactionRepository Factions { get; }

    /// <summary>
    /// 角色关系仓储
    /// </summary>
    ICharacterRelationshipRepository CharacterRelationships { get; }

    /// <summary>
    /// 势力关系仓储
    /// </summary>
    IFactionRelationshipRepository FactionRelationships { get; }

    /// <summary>
    /// 世界设定仓储
    /// </summary>
    IWorldSettingRepository WorldSettings { get; }

    /// <summary>
    /// 修炼体系仓储
    /// </summary>
    ICultivationSystemRepository CultivationSystems { get; }

    /// <summary>
    /// 修炼等级仓储
    /// </summary>
    ICultivationLevelRepository CultivationLevels { get; }

    /// <summary>
    /// 政治体系仓储
    /// </summary>
    IPoliticalSystemRepository PoliticalSystems { get; }

    /// <summary>
    /// 政治职位仓储
    /// </summary>
    IPoliticalPositionRepository PoliticalPositions { get; }

    /// <summary>
    /// 剧情仓储
    /// </summary>
    IPlotRepository Plots { get; }

    /// <summary>
    /// 资源仓储
    /// </summary>
    IResourceRepository Resources { get; }

    /// <summary>
    /// 种族仓储
    /// </summary>
    IRaceRepository Races { get; }

    /// <summary>
    /// 种族关系仓储
    /// </summary>
    IRaceRelationshipRepository RaceRelationships { get; }

    /// <summary>
    /// 秘境仓储
    /// </summary>
    ISecretRealmRepository SecretRealms { get; }

    /// <summary>
    /// 关系网络仓储
    /// </summary>
    IRelationshipNetworkRepository RelationshipNetworks { get; }

    /// <summary>
    /// 货币体系仓储
    /// </summary>
    ICurrencySystemRepository CurrencySystems { get; }

    /// <summary>
    /// 保存更改
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>受影响的行数</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 开始事务
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>事务对象</returns>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 提交事务
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 回滚事务
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
