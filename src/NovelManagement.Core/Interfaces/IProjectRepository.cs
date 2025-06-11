using NovelManagement.Core.Entities;

namespace NovelManagement.Core.Interfaces;

/// <summary>
/// 项目仓储接口
/// </summary>
public interface IProjectRepository : IRepository<Project>
{
    /// <summary>
    /// 根据名称获取项目
    /// </summary>
    /// <param name="name">项目名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>项目对象</returns>
    Task<Project?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取项目及其卷宗
    /// </summary>
    /// <param name="id">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>项目对象</returns>
    Task<Project?> GetWithVolumesAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取项目及其所有相关数据
    /// </summary>
    /// <param name="id">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>项目对象</returns>
    Task<Project?> GetWithAllDataAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据状态获取项目列表
    /// </summary>
    /// <param name="status">项目状态</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>项目列表</returns>
    Task<IEnumerable<Project>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据类型获取项目列表
    /// </summary>
    /// <param name="type">项目类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>项目列表</returns>
    Task<IEnumerable<Project>> GetByTypeAsync(string type, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取最近访问的项目列表
    /// </summary>
    /// <param name="count">数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>项目列表</returns>
    Task<IEnumerable<Project>> GetRecentlyAccessedAsync(int count = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// 搜索项目
    /// </summary>
    /// <param name="keyword">关键词</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>项目列表</returns>
    Task<IEnumerable<Project>> SearchAsync(string keyword, CancellationToken cancellationToken = default);
}
