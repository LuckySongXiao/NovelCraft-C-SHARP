using Microsoft.EntityFrameworkCore;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;

namespace NovelManagement.Infrastructure.Data.Repositories;

/// <summary>
/// 角色仓储实现
/// </summary>
public class CharacterRepository : BaseRepository<Character>, ICharacterRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="context">数据库上下文</param>
    public CharacterRepository(NovelManagementDbContext context) : base(context)
    {
    }

    /// <summary>
    /// 根据项目ID获取角色列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色列表</returns>
    public async Task<IEnumerable<Character>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.ProjectId == projectId)
            .OrderByDescending(c => c.Importance)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据角色类型获取角色列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="type">角色类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色列表</returns>
    public async Task<IEnumerable<Character>> GetByTypeAsync(Guid projectId, string type, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.ProjectId == projectId && c.Type == type)
            .OrderByDescending(c => c.Importance)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据势力ID获取角色列表
    /// </summary>
    /// <param name="factionId">势力ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色列表</returns>
    public async Task<IEnumerable<Character>> GetByFactionIdAsync(Guid factionId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.FactionId == factionId)
            .OrderByDescending(c => c.Importance)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取角色及其关系
    /// </summary>
    /// <param name="id">角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色对象</returns>
    public async Task<Character?> GetWithRelationshipsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(c => c.RelationshipsAsSource)
                .ThenInclude(r => r.TargetCharacter)
            .Include(c => c.RelationshipsAsTarget)
                .ThenInclude(r => r.SourceCharacter)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    /// <summary>
    /// 根据重要性获取角色列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="importance">重要性等级</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色列表</returns>
    public async Task<IEnumerable<Character>> GetByImportanceAsync(Guid projectId, int importance, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.ProjectId == projectId && c.Importance >= importance)
            .OrderByDescending(c => c.Importance)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 搜索角色
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="keyword">关键词</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色列表</returns>
    public async Task<IEnumerable<Character>> SearchAsync(Guid projectId, string keyword, CancellationToken cancellationToken = default)
    {
        var lowerKeyword = keyword.ToLower();
        return await DbSet
            .Where(c => c.ProjectId == projectId &&
                       (c.Name.ToLower().Contains(lowerKeyword) ||
                        (c.Appearance != null && c.Appearance.ToLower().Contains(lowerKeyword)) ||
                        (c.Personality != null && c.Personality.ToLower().Contains(lowerKeyword)) ||
                        (c.Background != null && c.Background.ToLower().Contains(lowerKeyword))))
            .OrderByDescending(c => c.Importance)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据势力获取角色列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="factionId">势力ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色列表</returns>
    public async Task<IEnumerable<Character>> GetByFactionAsync(Guid projectId, Guid factionId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.ProjectId == projectId && c.FactionId == factionId)
            .OrderByDescending(c => c.Importance)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }
}

/// <summary>
/// 势力仓储实现
/// </summary>
public class FactionRepository : BaseRepository<Faction>, IFactionRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="context">数据库上下文</param>
    public FactionRepository(NovelManagementDbContext context) : base(context)
    {
    }

    /// <summary>
    /// 根据项目ID获取势力列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>势力列表</returns>
    public async Task<IEnumerable<Faction>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(f => f.ProjectId == projectId)
            .OrderByDescending(f => f.PowerRating)
            .ThenBy(f => f.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据势力类型获取势力列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="type">势力类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>势力列表</returns>
    public async Task<IEnumerable<Faction>> GetByTypeAsync(Guid projectId, string type, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(f => f.ProjectId == projectId && f.Type == type)
            .OrderByDescending(f => f.PowerRating)
            .ThenBy(f => f.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取势力及其成员
    /// </summary>
    /// <param name="id">势力ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>势力对象</returns>
    public async Task<Faction?> GetWithMembersAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(f => f.Members.OrderByDescending(m => m.Importance))
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    /// <summary>
    /// 获取势力及其关系
    /// </summary>
    /// <param name="id">势力ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>势力对象</returns>
    public async Task<Faction?> GetWithRelationshipsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(f => f.AllianceRelationshipsAsSource)
                .ThenInclude(r => r.TargetFaction)
            .Include(f => f.AllianceRelationshipsAsTarget)
                .ThenInclude(r => r.SourceFaction)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    /// <summary>
    /// 搜索势力
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="keyword">关键词</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>势力列表</returns>
    public async Task<IEnumerable<Faction>> SearchAsync(Guid projectId, string keyword, CancellationToken cancellationToken = default)
    {
        var lowerKeyword = keyword.ToLower();
        return await DbSet
            .Where(f => f.ProjectId == projectId &&
                       (f.Name.ToLower().Contains(lowerKeyword) ||
                        (f.Description != null && f.Description.ToLower().Contains(lowerKeyword)) ||
                        (f.History != null && f.History.ToLower().Contains(lowerKeyword))))
            .OrderByDescending(f => f.PowerRating)
            .ThenBy(f => f.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据力量等级范围获取势力列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="minPowerLevel">最小力量等级</param>
    /// <param name="maxPowerLevel">最大力量等级</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>势力列表</returns>
    public async Task<IEnumerable<Faction>> GetByPowerLevelRangeAsync(Guid projectId, int minPowerLevel, int maxPowerLevel, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(f => f.ProjectId == projectId && f.PowerRating >= minPowerLevel && f.PowerRating <= maxPowerLevel)
            .OrderByDescending(f => f.PowerRating)
            .ThenBy(f => f.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据名称获取势力
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="name">势力名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>势力对象</returns>
    public async Task<Faction?> GetByNameAsync(Guid projectId, string name, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(f => f.ProjectId == projectId && f.Name == name, cancellationToken);
    }
}
