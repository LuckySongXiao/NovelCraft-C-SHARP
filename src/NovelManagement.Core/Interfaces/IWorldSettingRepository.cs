using NovelManagement.Core.Entities;

namespace NovelManagement.Core.Interfaces;

/// <summary>
/// 世界设定仓储接口
/// </summary>
public interface IWorldSettingRepository : IRepository<WorldSetting>
{
    /// <summary>
    /// 根据项目ID获取世界设定列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>世界设定列表</returns>
    Task<IEnumerable<WorldSetting>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据类型获取世界设定列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="type">设定类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>世界设定列表</returns>
    Task<IEnumerable<WorldSetting>> GetByTypeAsync(Guid projectId, string type, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据分类获取世界设定列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="category">设定分类</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>世界设定列表</returns>
    Task<IEnumerable<WorldSetting>> GetByCategoryAsync(Guid projectId, string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取世界设定及其子设定
    /// </summary>
    /// <param name="id">设定ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>世界设定对象</returns>
    Task<WorldSetting?> GetWithChildrenAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据父级ID获取子设定列表
    /// </summary>
    /// <param name="parentId">父级设定ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>子设定列表</returns>
    Task<IEnumerable<WorldSetting>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取根级设定列表（没有父级的设定）
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>根级设定列表</returns>
    Task<IEnumerable<WorldSetting>> GetRootSettingsAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据重要性获取世界设定列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="minImportance">最小重要性</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>世界设定列表</returns>
    Task<IEnumerable<WorldSetting>> GetByImportanceAsync(Guid projectId, int minImportance, CancellationToken cancellationToken = default);

    /// <summary>
    /// 搜索世界设定
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="searchTerm">搜索关键词</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>世界设定列表</returns>
    Task<IEnumerable<WorldSetting>> SearchAsync(Guid projectId, string searchTerm, CancellationToken cancellationToken = default);
}
