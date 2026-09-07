using System.Linq.Expressions;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;

namespace NovelManagement.Tests.Fakes;

/// <summary>
/// 章节仓储内存 Fake：供章节草稿/正文落库与流水线推进测试
/// </summary>
internal sealed class FakeChapterRepository : IChapterRepository
{
    public List<Chapter> Chapters { get; } = new();

    /// <summary>卷宗数据源（由 FakeUnitOfWork 注入，用于按项目过滤章节）。</summary>
    public List<Volume> VolumeSource { get; set; } = new();

    public Task<Chapter?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Chapters.FirstOrDefault(c => c.Id == id));

    public Task<IEnumerable<Chapter>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Chapters.AsEnumerable());

    public Task<IEnumerable<Chapter>> FindAsync(Expression<Func<Chapter, bool>> predicate, CancellationToken cancellationToken = default)
        => Task.FromResult(Chapters.AsEnumerable().Where(predicate.Compile()));

    public Task<Chapter?> FirstOrDefaultAsync(Expression<Func<Chapter, bool>> predicate, CancellationToken cancellationToken = default)
        => Task.FromResult(Chapters.AsEnumerable().FirstOrDefault(predicate.Compile()));

    public Task<Chapter> AddAsync(Chapter entity, CancellationToken cancellationToken = default)
    {
        Chapters.Add(entity);
        return Task.FromResult(entity);
    }

    public Task<IEnumerable<Chapter>> AddRangeAsync(IEnumerable<Chapter> entities, CancellationToken cancellationToken = default)
    {
        Chapters.AddRange(entities);
        return Task.FromResult(entities);
    }

    public Task<Chapter> UpdateAsync(Chapter entity, CancellationToken cancellationToken = default)
        => Task.FromResult(entity);

    public Task DeleteAsync(Chapter entity, CancellationToken cancellationToken = default)
    {
        Chapters.RemoveAll(c => c.Id == entity.Id);
        return Task.CompletedTask;
    }

    public Task DeleteByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Chapters.RemoveAll(c => c.Id == id);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(Expression<Func<Chapter, bool>> predicate, CancellationToken cancellationToken = default)
        => Task.FromResult(Chapters.AsEnumerable().Any(predicate.Compile()));

    public Task<int> CountAsync(Expression<Func<Chapter, bool>>? predicate = null, CancellationToken cancellationToken = default)
        => Task.FromResult(predicate == null ? Chapters.Count : Chapters.AsEnumerable().Count(predicate.Compile()));

    public Task<IEnumerable<Chapter>> GetByVolumeIdAsync(Guid volumeId, CancellationToken cancellationToken = default)
        => Task.FromResult(Chapters.Where(c => c.VolumeId == volumeId && !c.IsDeleted).AsEnumerable());

    public Task<IEnumerable<Chapter>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
        => Task.FromResult(Chapters.Where(c => !c.IsDeleted && VolumeSource.Any(v => v.Id == c.VolumeId && v.ProjectId == projectId)).AsEnumerable());

    public Task<IEnumerable<Chapter>> GetByStatusAsync(Guid volumeId, string status, CancellationToken cancellationToken = default)
        => Task.FromResult(Chapters.Where(c => c.VolumeId == volumeId && c.Status == status).AsEnumerable());

    public Task<int> GetNextOrderIndexAsync(Guid volumeId, CancellationToken cancellationToken = default)
        => Task.FromResult(Chapters.Where(c => c.VolumeId == volumeId).Any()
            ? Chapters.Where(c => c.VolumeId == volumeId).Max(c => c.Order) + 1
            : 1);

    public Task<IEnumerable<Chapter>> SearchAsync(Guid projectId, string keyword, CancellationToken cancellationToken = default)
        => Task.FromResult(Chapters.Where(c => c.Title.Contains(keyword)).AsEnumerable());
}
