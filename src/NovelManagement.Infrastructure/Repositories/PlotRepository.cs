using Microsoft.EntityFrameworkCore;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;
using NovelManagement.Infrastructure.Data;
using NovelManagement.Infrastructure.Data.Repositories;

namespace NovelManagement.Infrastructure.Repositories;

/// <summary>
/// 剧情仓储实现
/// </summary>
public class PlotRepository : BaseRepository<Plot>, IPlotRepository
{
    public PlotRepository(NovelManagementDbContext context) : base(context)
    {
    }

    /// <summary>
    /// 根据项目ID获取剧情列表
    /// </summary>
    public async Task<IEnumerable<Plot>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await Context.Plots
            .Where(p => p.ProjectId == projectId)
            .OrderBy(p => p.Title)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据类型获取剧情列表
    /// </summary>
    public async Task<IEnumerable<Plot>> GetByTypeAsync(Guid projectId, string type, CancellationToken cancellationToken = default)
    {
        return await Context.Plots
            .Where(p => p.ProjectId == projectId && p.Type == type)
            .OrderBy(p => p.Title)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据状态获取剧情列表
    /// </summary>
    public async Task<IEnumerable<Plot>> GetByStatusAsync(Guid projectId, string status, CancellationToken cancellationToken = default)
    {
        return await Context.Plots
            .Where(p => p.ProjectId == projectId && p.Status == status)
            .OrderBy(p => p.Title)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据优先级获取剧情列表
    /// </summary>
    public async Task<IEnumerable<Plot>> GetByPriorityAsync(Guid projectId, string priority, CancellationToken cancellationToken = default)
    {
        return await Context.Plots
            .Where(p => p.ProjectId == projectId && p.Priority == priority)
            .OrderBy(p => p.Title)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取剧情及其相关人物
    /// </summary>
    public async Task<Plot?> GetWithCharactersAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Context.Plots
            .Include(p => p.RelatedCharacters)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    /// <summary>
    /// 获取剧情及其涉及章节
    /// </summary>
    public async Task<Plot?> GetWithChaptersAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Context.Plots
            .Include(p => p.InvolvedChapters)
            .Include(p => p.StartChapter)
            .Include(p => p.EndChapter)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    /// <summary>
    /// 根据重要性获取剧情列表
    /// </summary>
    public async Task<IEnumerable<Plot>> GetByImportanceAsync(Guid projectId, int minImportance, CancellationToken cancellationToken = default)
    {
        return await Context.Plots
            .Where(p => p.ProjectId == projectId && p.Importance >= minImportance)
            .OrderByDescending(p => p.Importance)
            .ThenBy(p => p.Title)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 搜索剧情
    /// </summary>
    public async Task<IEnumerable<Plot>> SearchAsync(Guid projectId, string searchTerm, CancellationToken cancellationToken = default)
    {
        return await Context.Plots
            .Where(p => p.ProjectId == projectId &&
                       (p.Title.Contains(searchTerm) ||
                        p.Description!.Contains(searchTerm) ||
                        p.Outline!.Contains(searchTerm) ||
                        p.Tags!.Contains(searchTerm)))
            .OrderBy(p => p.Title)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取角色相关的剧情
    /// </summary>
    public async Task<IEnumerable<Plot>> GetByCharacterAsync(Guid characterId, CancellationToken cancellationToken = default)
    {
        return await Context.Plots
            .Where(p => p.RelatedCharacters.Any(c => c.Id == characterId))
            .OrderBy(p => p.Title)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取章节涉及的剧情
    /// </summary>
    public async Task<IEnumerable<Plot>> GetByChapterAsync(Guid chapterId, CancellationToken cancellationToken = default)
    {
        return await Context.Plots
            .Where(p => p.InvolvedChapters.Any(c => c.Id == chapterId) ||
                       p.StartChapterId == chapterId ||
                       p.EndChapterId == chapterId)
            .OrderBy(p => p.Title)
            .ToListAsync(cancellationToken);
    }
}
