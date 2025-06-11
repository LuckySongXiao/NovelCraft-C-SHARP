using Microsoft.EntityFrameworkCore;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;

namespace NovelManagement.Infrastructure.Data.Repositories;

/// <summary>
/// 项目仓储实现
/// </summary>
public class ProjectRepository : BaseRepository<Project>, IProjectRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="context">数据库上下文</param>
    public ProjectRepository(NovelManagementDbContext context) : base(context)
    {
    }

    /// <summary>
    /// 根据名称获取项目
    /// </summary>
    /// <param name="name">项目名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>项目对象</returns>
    public async Task<Project?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(p => p.Name == name, cancellationToken);
    }

    /// <summary>
    /// 获取项目及其卷宗
    /// </summary>
    /// <param name="id">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>项目对象</returns>
    public async Task<Project?> GetWithVolumesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(p => p.Volumes.OrderBy(v => v.Order))
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    /// <summary>
    /// 获取项目及其所有相关数据
    /// </summary>
    /// <param name="id">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>项目对象</returns>
    public async Task<Project?> GetWithAllDataAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(p => p.Volumes.OrderBy(v => v.Order))
                .ThenInclude(v => v.Chapters.OrderBy(c => c.Order))
            .Include(p => p.Characters)
            .Include(p => p.Factions)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    /// <summary>
    /// 根据状态获取项目列表
    /// </summary>
    /// <param name="status">项目状态</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>项目列表</returns>
    public async Task<IEnumerable<Project>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(p => p.Status == status)
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据类型获取项目列表
    /// </summary>
    /// <param name="type">项目类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>项目列表</returns>
    public async Task<IEnumerable<Project>> GetByTypeAsync(string type, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(p => p.Type == type)
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取最近访问的项目列表
    /// </summary>
    /// <param name="count">数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>项目列表</returns>
    public async Task<IEnumerable<Project>> GetRecentlyAccessedAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(p => p.LastAccessedAt != null)
            .OrderByDescending(p => p.LastAccessedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 搜索项目
    /// </summary>
    /// <param name="keyword">关键词</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>项目列表</returns>
    public async Task<IEnumerable<Project>> SearchAsync(string keyword, CancellationToken cancellationToken = default)
    {
        var lowerKeyword = keyword.ToLower();
        return await DbSet
            .Where(p => p.Name.ToLower().Contains(lowerKeyword) ||
                       (p.Description != null && p.Description.ToLower().Contains(lowerKeyword)) ||
                       (p.Tags != null && p.Tags.ToLower().Contains(lowerKeyword)))
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync(cancellationToken);
    }
}
