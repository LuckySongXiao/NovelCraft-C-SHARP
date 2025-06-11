using NovelManagement.Core.Entities;

namespace NovelManagement.Core.Interfaces;

/// <summary>
/// 剧情仓储接口
/// </summary>
public interface IPlotRepository : IRepository<Plot>
{
    /// <summary>
    /// 根据项目ID获取剧情列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>剧情列表</returns>
    Task<IEnumerable<Plot>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据类型获取剧情列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="type">剧情类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>剧情列表</returns>
    Task<IEnumerable<Plot>> GetByTypeAsync(Guid projectId, string type, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据状态获取剧情列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="status">剧情状态</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>剧情列表</returns>
    Task<IEnumerable<Plot>> GetByStatusAsync(Guid projectId, string status, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据优先级获取剧情列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="priority">优先级</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>剧情列表</returns>
    Task<IEnumerable<Plot>> GetByPriorityAsync(Guid projectId, string priority, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取剧情及其相关人物
    /// </summary>
    /// <param name="id">剧情ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>剧情对象</returns>
    Task<Plot?> GetWithCharactersAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取剧情及其涉及章节
    /// </summary>
    /// <param name="id">剧情ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>剧情对象</returns>
    Task<Plot?> GetWithChaptersAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据重要性获取剧情列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="minImportance">最小重要性</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>剧情列表</returns>
    Task<IEnumerable<Plot>> GetByImportanceAsync(Guid projectId, int minImportance, CancellationToken cancellationToken = default);

    /// <summary>
    /// 搜索剧情
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="searchTerm">搜索词</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>剧情列表</returns>
    Task<IEnumerable<Plot>> SearchAsync(Guid projectId, string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色相关的剧情
    /// </summary>
    /// <param name="characterId">角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>剧情列表</returns>
    Task<IEnumerable<Plot>> GetByCharacterAsync(Guid characterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取章节涉及的剧情
    /// </summary>
    /// <param name="chapterId">章节ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>剧情列表</returns>
    Task<IEnumerable<Plot>> GetByChapterAsync(Guid chapterId, CancellationToken cancellationToken = default);
}
