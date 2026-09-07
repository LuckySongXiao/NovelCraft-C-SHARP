using System.Linq.Expressions;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;

namespace NovelManagement.Tests.Fakes;

/// <summary>
/// 剧情仓储内存 Fake：供创作流水线状态持久化与剧情线落库测试
/// </summary>
internal sealed class FakePlotRepository : IPlotRepository
{
    public List<Plot> Plots { get; } = new();

    public Task<Plot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Plots.FirstOrDefault(p => p.Id == id));

    public Task<IEnumerable<Plot>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Plots.AsEnumerable());

    public Task<IEnumerable<Plot>> FindAsync(Expression<Func<Plot, bool>> predicate, CancellationToken cancellationToken = default)
        => Task.FromResult(Plots.AsEnumerable().Where(predicate.Compile()));

    public Task<Plot?> FirstOrDefaultAsync(Expression<Func<Plot, bool>> predicate, CancellationToken cancellationToken = default)
        => Task.FromResult(Plots.AsEnumerable().FirstOrDefault(predicate.Compile()));

    public Task<Plot> AddAsync(Plot entity, CancellationToken cancellationToken = default)
    {
        Plots.Add(entity);
        return Task.FromResult(entity);
    }

    public Task<IEnumerable<Plot>> AddRangeAsync(IEnumerable<Plot> entities, CancellationToken cancellationToken = default)
    {
        Plots.AddRange(entities);
        return Task.FromResult(entities);
    }

    public Task<Plot> UpdateAsync(Plot entity, CancellationToken cancellationToken = default)
        => Task.FromResult(entity);

    public Task DeleteAsync(Plot entity, CancellationToken cancellationToken = default)
    {
        Plots.RemoveAll(p => p.Id == entity.Id);
        return Task.CompletedTask;
    }

    public Task DeleteByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Plots.RemoveAll(p => p.Id == id);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(Expression<Func<Plot, bool>> predicate, CancellationToken cancellationToken = default)
        => Task.FromResult(Plots.AsEnumerable().Any(predicate.Compile()));

    public Task<int> CountAsync(Expression<Func<Plot, bool>>? predicate = null, CancellationToken cancellationToken = default)
        => Task.FromResult(predicate == null ? Plots.Count : Plots.AsEnumerable().Count(predicate.Compile()));

    public Task<IEnumerable<Plot>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
        => Task.FromResult(Plots.Where(p => p.ProjectId == projectId && !p.IsDeleted).AsEnumerable());

    public Task<IEnumerable<Plot>> GetByTypeAsync(Guid projectId, string type, CancellationToken cancellationToken = default)
        => Task.FromResult(Plots.Where(p => p.ProjectId == projectId && p.Type == type && !p.IsDeleted).AsEnumerable());

    public Task<IEnumerable<Plot>> GetByStatusAsync(Guid projectId, string status, CancellationToken cancellationToken = default)
        => Task.FromResult(Plots.Where(p => p.ProjectId == projectId && p.Status == status).AsEnumerable());

    public Task<IEnumerable<Plot>> GetByPriorityAsync(Guid projectId, string priority, CancellationToken cancellationToken = default)
        => Task.FromResult(Plots.Where(p => p.ProjectId == projectId && p.Priority == priority).AsEnumerable());

    public Task<Plot?> GetWithCharactersAsync(Guid id, CancellationToken cancellationToken = default)
        => GetByIdAsync(id, cancellationToken);

    public Task<Plot?> GetWithChaptersAsync(Guid id, CancellationToken cancellationToken = default)
        => GetByIdAsync(id, cancellationToken);

    public Task<IEnumerable<Plot>> GetByImportanceAsync(Guid projectId, int minImportance, CancellationToken cancellationToken = default)
        => Task.FromResult(Plots.Where(p => p.ProjectId == projectId && p.Importance >= minImportance).AsEnumerable());

    public Task<IEnumerable<Plot>> SearchAsync(Guid projectId, string searchTerm, CancellationToken cancellationToken = default)
        => Task.FromResult(Plots.Where(p => p.ProjectId == projectId && p.Title.Contains(searchTerm)).AsEnumerable());

    public Task<IEnumerable<Plot>> GetByCharacterAsync(Guid characterId, CancellationToken cancellationToken = default)
        => Task.FromResult(Enumerable.Empty<Plot>());

    public Task<IEnumerable<Plot>> GetByChapterAsync(Guid chapterId, CancellationToken cancellationToken = default)
        => Task.FromResult(Enumerable.Empty<Plot>());
}
