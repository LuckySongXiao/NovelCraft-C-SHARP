using System.Linq.Expressions;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;

namespace NovelManagement.Tests.Fakes;

/// <summary>
/// 卷宗仓储内存 Fake：供分卷落库与流水线推进测试
/// </summary>
internal sealed class FakeVolumeRepository : IVolumeRepository
{
    public List<Volume> Volumes { get; } = new();

    public Task<Volume?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Volumes.FirstOrDefault(v => v.Id == id));

    public Task<IEnumerable<Volume>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Volumes.AsEnumerable());

    public Task<IEnumerable<Volume>> FindAsync(Expression<Func<Volume, bool>> predicate, CancellationToken cancellationToken = default)
        => Task.FromResult(Volumes.AsEnumerable().Where(predicate.Compile()));

    public Task<Volume?> FirstOrDefaultAsync(Expression<Func<Volume, bool>> predicate, CancellationToken cancellationToken = default)
        => Task.FromResult(Volumes.AsEnumerable().FirstOrDefault(predicate.Compile()));

    public Task<Volume> AddAsync(Volume entity, CancellationToken cancellationToken = default)
    {
        Volumes.Add(entity);
        return Task.FromResult(entity);
    }

    public Task<IEnumerable<Volume>> AddRangeAsync(IEnumerable<Volume> entities, CancellationToken cancellationToken = default)
    {
        Volumes.AddRange(entities);
        return Task.FromResult(entities);
    }

    public Task<Volume> UpdateAsync(Volume entity, CancellationToken cancellationToken = default)
        => Task.FromResult(entity);

    public Task DeleteAsync(Volume entity, CancellationToken cancellationToken = default)
    {
        Volumes.RemoveAll(v => v.Id == entity.Id);
        return Task.CompletedTask;
    }

    public Task DeleteByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Volumes.RemoveAll(v => v.Id == id);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(Expression<Func<Volume, bool>> predicate, CancellationToken cancellationToken = default)
        => Task.FromResult(Volumes.AsEnumerable().Any(predicate.Compile()));

    public Task<int> CountAsync(Expression<Func<Volume, bool>>? predicate = null, CancellationToken cancellationToken = default)
        => Task.FromResult(predicate == null ? Volumes.Count : Volumes.AsEnumerable().Count(predicate.Compile()));

    public Task<IEnumerable<Volume>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
        => Task.FromResult(Volumes.Where(v => v.ProjectId == projectId && !v.IsDeleted).AsEnumerable());

    public Task<Volume?> GetWithChaptersAsync(Guid id, CancellationToken cancellationToken = default)
        => GetByIdAsync(id, cancellationToken);

    public Task<IEnumerable<Volume>> GetByStatusAsync(Guid projectId, string status, CancellationToken cancellationToken = default)
        => Task.FromResult(Volumes.Where(v => v.ProjectId == projectId && v.Status == status).AsEnumerable());

    public Task<int> GetNextOrderIndexAsync(Guid projectId, CancellationToken cancellationToken = default)
        => Task.FromResult(Volumes.Where(v => v.ProjectId == projectId).Any()
            ? Volumes.Where(v => v.ProjectId == projectId).Max(v => v.Order) + 1
            : 1);
}
