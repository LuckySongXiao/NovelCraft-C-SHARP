using NovelManagement.Core.Entities;

namespace NovelManagement.Core.Interfaces;

/// <summary>
/// 卷宗仓储接口
/// </summary>
public interface IVolumeRepository : IRepository<Volume>
{
    /// <summary>
    /// 根据项目ID获取卷宗列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>卷宗列表</returns>
    Task<IEnumerable<Volume>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取卷宗及其章节
    /// </summary>
    /// <param name="id">卷宗ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>卷宗对象</returns>
    Task<Volume?> GetWithChaptersAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据状态获取卷宗列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="status">卷宗状态</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>卷宗列表</returns>
    Task<IEnumerable<Volume>> GetByStatusAsync(Guid projectId, string status, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取项目中的下一个序号
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>下一个序号</returns>
    Task<int> GetNextOrderIndexAsync(Guid projectId, CancellationToken cancellationToken = default);
}

/// <summary>
/// 章节仓储接口
/// </summary>
public interface IChapterRepository : IRepository<Chapter>
{
    /// <summary>
    /// 根据卷宗ID获取章节列表
    /// </summary>
    /// <param name="volumeId">卷宗ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>章节列表</returns>
    Task<IEnumerable<Chapter>> GetByVolumeIdAsync(Guid volumeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据项目ID获取所有章节
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>章节列表</returns>
    Task<IEnumerable<Chapter>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据状态获取章节列表
    /// </summary>
    /// <param name="volumeId">卷宗ID</param>
    /// <param name="status">章节状态</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>章节列表</returns>
    Task<IEnumerable<Chapter>> GetByStatusAsync(Guid volumeId, string status, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取卷宗中的下一个序号
    /// </summary>
    /// <param name="volumeId">卷宗ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>下一个序号</returns>
    Task<int> GetNextOrderIndexAsync(Guid volumeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 搜索章节
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="keyword">关键词</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>章节列表</returns>
    Task<IEnumerable<Chapter>> SearchAsync(Guid projectId, string keyword, CancellationToken cancellationToken = default);
}
