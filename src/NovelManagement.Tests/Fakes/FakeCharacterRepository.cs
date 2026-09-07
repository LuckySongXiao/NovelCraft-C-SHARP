using System.Linq.Expressions;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;

namespace NovelManagement.Tests.Fakes;

/// <summary>
/// 角色仓储内存 Fake：记录更新调用，供回填服务测试
/// </summary>
internal sealed class FakeCharacterRepository : ICharacterRepository
{
    public List<Character> Characters { get; } = new();
    public List<Character> Updated { get; } = new();

    public Task<Character?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Characters.FirstOrDefault(c => c.Id == id));

    public Task<IEnumerable<Character>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Characters.AsEnumerable());

    public Task<IEnumerable<Character>> FindAsync(Expression<Func<Character, bool>> predicate, CancellationToken cancellationToken = default)
        => Task.FromResult(Characters.AsEnumerable().Where(predicate.Compile()));

    public Task<Character?> FirstOrDefaultAsync(Expression<Func<Character, bool>> predicate, CancellationToken cancellationToken = default)
        => Task.FromResult(Characters.AsEnumerable().FirstOrDefault(predicate.Compile()));

    public Task<Character> AddAsync(Character entity, CancellationToken cancellationToken = default)
    {
        Characters.Add(entity);
        return Task.FromResult(entity);
    }

    public Task<IEnumerable<Character>> AddRangeAsync(IEnumerable<Character> entities, CancellationToken cancellationToken = default)
    {
        Characters.AddRange(entities);
        return Task.FromResult(entities);
    }

    public Task<Character> UpdateAsync(Character entity, CancellationToken cancellationToken = default)
    {
        Updated.Add(entity);
        return Task.FromResult(entity);
    }

    public Task DeleteAsync(Character entity, CancellationToken cancellationToken = default)
    {
        Characters.RemoveAll(c => c.Id == entity.Id);
        return Task.CompletedTask;
    }

    public Task DeleteByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Characters.RemoveAll(c => c.Id == id);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(Expression<Func<Character, bool>> predicate, CancellationToken cancellationToken = default)
        => Task.FromResult(Characters.AsEnumerable().Any(predicate.Compile()));

    public Task<int> CountAsync(Expression<Func<Character, bool>>? predicate = null, CancellationToken cancellationToken = default)
        => Task.FromResult(predicate == null ? Characters.Count : Characters.AsEnumerable().Count(predicate.Compile()));

    public Task<IEnumerable<Character>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
        => Task.FromResult(Characters.Where(c => c.ProjectId == projectId).AsEnumerable());

    public Task<IEnumerable<Character>> GetByTypeAsync(Guid projectId, string type, CancellationToken cancellationToken = default)
        => Task.FromResult(Characters.Where(c => c.ProjectId == projectId && c.Type == type).AsEnumerable());

    public Task<IEnumerable<Character>> GetByFactionIdAsync(Guid factionId, CancellationToken cancellationToken = default)
        => Task.FromResult(Enumerable.Empty<Character>());

    public Task<Character?> GetWithRelationshipsAsync(Guid id, CancellationToken cancellationToken = default)
        => GetByIdAsync(id, cancellationToken);

    public Task<IEnumerable<Character>> GetByImportanceAsync(Guid projectId, int importance, CancellationToken cancellationToken = default)
        => Task.FromResult(Characters.Where(c => c.ProjectId == projectId && c.Importance == importance).AsEnumerable());

    public Task<IEnumerable<Character>> SearchAsync(Guid projectId, string keyword, CancellationToken cancellationToken = default)
        => Task.FromResult(Characters.Where(c => c.ProjectId == projectId && c.Name.Contains(keyword)).AsEnumerable());

    public Task<IEnumerable<Character>> GetByFactionAsync(Guid projectId, Guid factionId, CancellationToken cancellationToken = default)
        => Task.FromResult(Enumerable.Empty<Character>());
}
