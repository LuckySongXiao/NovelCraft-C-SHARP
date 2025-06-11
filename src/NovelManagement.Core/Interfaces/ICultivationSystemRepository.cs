using NovelManagement.Core.Entities;

namespace NovelManagement.Core.Interfaces;

/// <summary>
/// 修炼体系仓储接口
/// </summary>
public interface ICultivationSystemRepository : IRepository<CultivationSystem>
{
    /// <summary>
    /// 根据项目ID获取修炼体系列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>修炼体系列表</returns>
    Task<IEnumerable<CultivationSystem>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据类型获取修炼体系列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="type">体系类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>修炼体系列表</returns>
    Task<IEnumerable<CultivationSystem>> GetByTypeAsync(Guid projectId, string type, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取修炼体系及其等级
    /// </summary>
    /// <param name="id">体系ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>修炼体系对象</returns>
    Task<CultivationSystem?> GetWithLevelsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据重要性获取修炼体系列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="minImportance">最小重要性</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>修炼体系列表</returns>
    Task<IEnumerable<CultivationSystem>> GetByImportanceAsync(Guid projectId, int minImportance, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据角色获取修炼体系
    /// </summary>
    /// <param name="characterId">角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>修炼体系对象</returns>
    Task<CultivationSystem?> GetByCharacterAsync(Guid characterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据难度范围获取修炼体系列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="minDifficulty">最小难度</param>
    /// <param name="maxDifficulty">最大难度</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>修炼体系列表</returns>
    Task<IEnumerable<CultivationSystem>> GetByDifficultyRangeAsync(Guid projectId, int minDifficulty, int maxDifficulty, CancellationToken cancellationToken = default);

    /// <summary>
    /// 搜索修炼体系
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="searchTerm">搜索关键词</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>修炼体系列表</returns>
    Task<IEnumerable<CultivationSystem>> SearchAsync(Guid projectId, string searchTerm, CancellationToken cancellationToken = default);
}

/// <summary>
/// 修炼等级仓储接口
/// </summary>
public interface ICultivationLevelRepository : IRepository<CultivationLevel>
{
    /// <summary>
    /// 根据修炼体系ID获取等级列表
    /// </summary>
    /// <param name="cultivationSystemId">修炼体系ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>修炼等级列表</returns>
    Task<IEnumerable<CultivationLevel>> GetBySystemIdAsync(Guid cultivationSystemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据等级序号获取修炼等级
    /// </summary>
    /// <param name="cultivationSystemId">修炼体系ID</param>
    /// <param name="orderIndex">等级序号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>修炼等级对象</returns>
    Task<CultivationLevel?> GetByOrderIndexAsync(Guid cultivationSystemId, int orderIndex, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取下一个等级
    /// </summary>
    /// <param name="currentLevelId">当前等级ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>下一个等级对象</returns>
    Task<CultivationLevel?> GetNextLevelAsync(Guid currentLevelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取上一个等级
    /// </summary>
    /// <param name="currentLevelId">当前等级ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>上一个等级对象</returns>
    Task<CultivationLevel?> GetPreviousLevelAsync(Guid currentLevelId, CancellationToken cancellationToken = default);
}
