using Microsoft.EntityFrameworkCore;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;

namespace NovelManagement.Infrastructure.Data.Repositories;

/// <summary>
/// 卷宗仓储实现
/// </summary>
public class VolumeRepository : BaseRepository<Volume>, IVolumeRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="context">数据库上下文</param>
    public VolumeRepository(NovelManagementDbContext context) : base(context)
    {
    }

    /// <summary>
    /// 根据项目ID获取卷宗列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>卷宗列表</returns>
    public async Task<IEnumerable<Volume>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(v => v.ProjectId == projectId)
            .OrderBy(v => v.Order)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取卷宗及其章节
    /// </summary>
    /// <param name="id">卷宗ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>卷宗对象</returns>
    public async Task<Volume?> GetWithChaptersAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(v => v.Chapters.OrderBy(c => c.Order))
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    /// <summary>
    /// 根据状态获取卷宗列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="status">卷宗状态</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>卷宗列表</returns>
    public async Task<IEnumerable<Volume>> GetByStatusAsync(Guid projectId, string status, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(v => v.ProjectId == projectId && v.Status == status)
            .OrderBy(v => v.Order)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取项目中的下一个序号
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>下一个序号</returns>
    public async Task<int> GetNextOrderIndexAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var maxOrderIndex = await DbSet
            .Where(v => v.ProjectId == projectId)
            .MaxAsync(v => (int?)v.Order, cancellationToken);

        return (maxOrderIndex ?? 0) + 1;
    }
}

/// <summary>
/// 章节仓储实现
/// </summary>
public class ChapterRepository : BaseRepository<Chapter>, IChapterRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="context">数据库上下文</param>
    public ChapterRepository(NovelManagementDbContext context) : base(context)
    {
    }

    /// <summary>
    /// 根据卷宗ID获取章节列表
    /// </summary>
    /// <param name="volumeId">卷宗ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>章节列表</returns>
    public async Task<IEnumerable<Chapter>> GetByVolumeIdAsync(Guid volumeId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.VolumeId == volumeId)
            .OrderBy(c => c.Order)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据项目ID获取所有章节
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>章节列表</returns>
    public async Task<IEnumerable<Chapter>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(c => c.Volume)
            .Where(c => c.Volume.ProjectId == projectId)
            .OrderBy(c => c.Volume.Order)
            .ThenBy(c => c.Order)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据状态获取章节列表
    /// </summary>
    /// <param name="volumeId">卷宗ID</param>
    /// <param name="status">章节状态</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>章节列表</returns>
    public async Task<IEnumerable<Chapter>> GetByStatusAsync(Guid volumeId, string status, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.VolumeId == volumeId && c.Status == status)
            .OrderBy(c => c.Order)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取卷宗中的下一个序号
    /// </summary>
    /// <param name="volumeId">卷宗ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>下一个序号</returns>
    public async Task<int> GetNextOrderIndexAsync(Guid volumeId, CancellationToken cancellationToken = default)
    {
        var maxOrderIndex = await DbSet
            .Where(c => c.VolumeId == volumeId)
            .MaxAsync(c => (int?)c.Order, cancellationToken);

        return (maxOrderIndex ?? 0) + 1;
    }

    /// <summary>
    /// 搜索章节
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="keyword">关键词</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>章节列表</returns>
    public async Task<IEnumerable<Chapter>> SearchAsync(Guid projectId, string keyword, CancellationToken cancellationToken = default)
    {
        var lowerKeyword = keyword.ToLower();
        return await DbSet
            .Include(c => c.Volume)
            .Where(c => c.Volume.ProjectId == projectId &&
                       (c.Title.ToLower().Contains(lowerKeyword) ||
                        (c.Content != null && c.Content.ToLower().Contains(lowerKeyword)) ||
                        (c.Summary != null && c.Summary.ToLower().Contains(lowerKeyword))))
            .OrderBy(c => c.Volume.Order)
            .ThenBy(c => c.Order)
            .ToListAsync(cancellationToken);
    }
}
