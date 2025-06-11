using NovelManagement.Core.Entities;

namespace NovelManagement.Core.Interfaces;

/// <summary>
/// 角色仓储接口
/// </summary>
public interface ICharacterRepository : IRepository<Character>
{
    /// <summary>
    /// 根据项目ID获取角色列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色列表</returns>
    Task<IEnumerable<Character>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据角色类型获取角色列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="type">角色类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色列表</returns>
    Task<IEnumerable<Character>> GetByTypeAsync(Guid projectId, string type, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据势力ID获取角色列表
    /// </summary>
    /// <param name="factionId">势力ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色列表</returns>
    Task<IEnumerable<Character>> GetByFactionIdAsync(Guid factionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色及其关系
    /// </summary>
    /// <param name="id">角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色对象</returns>
    Task<Character?> GetWithRelationshipsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据重要性获取角色列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="importance">重要性等级</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色列表</returns>
    Task<IEnumerable<Character>> GetByImportanceAsync(Guid projectId, int importance, CancellationToken cancellationToken = default);

    /// <summary>
    /// 搜索角色
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="keyword">关键词</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色列表</returns>
    Task<IEnumerable<Character>> SearchAsync(Guid projectId, string keyword, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据势力获取角色列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="factionId">势力ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色列表</returns>
    Task<IEnumerable<Character>> GetByFactionAsync(Guid projectId, Guid factionId, CancellationToken cancellationToken = default);
}

/// <summary>
/// 势力仓储接口
/// </summary>
public interface IFactionRepository : IRepository<Faction>
{
    /// <summary>
    /// 根据项目ID获取势力列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>势力列表</returns>
    Task<IEnumerable<Faction>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据势力类型获取势力列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="type">势力类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>势力列表</returns>
    Task<IEnumerable<Faction>> GetByTypeAsync(Guid projectId, string type, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取势力及其成员
    /// </summary>
    /// <param name="id">势力ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>势力对象</returns>
    Task<Faction?> GetWithMembersAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取势力及其关系
    /// </summary>
    /// <param name="id">势力ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>势力对象</returns>
    Task<Faction?> GetWithRelationshipsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 搜索势力
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="keyword">关键词</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>势力列表</returns>
    Task<IEnumerable<Faction>> SearchAsync(Guid projectId, string keyword, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据力量等级范围获取势力列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="minPowerLevel">最小力量等级</param>
    /// <param name="maxPowerLevel">最大力量等级</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>势力列表</returns>
    Task<IEnumerable<Faction>> GetByPowerLevelRangeAsync(Guid projectId, int minPowerLevel, int maxPowerLevel, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据名称获取势力
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="name">势力名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>势力对象</returns>
    Task<Faction?> GetByNameAsync(Guid projectId, string name, CancellationToken cancellationToken = default);
}
