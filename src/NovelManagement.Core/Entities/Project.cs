using System.ComponentModel.DataAnnotations;

namespace NovelManagement.Core.Entities;

/// <summary>
/// 小说项目实体
/// </summary>
public class Project : BaseEntity
{
    /// <summary>
    /// 项目名称
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 项目描述
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// 项目类型
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 项目状态
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Planning";

    /// <summary>
    /// 项目封面图片路径
    /// </summary>
    [MaxLength(500)]
    public string? CoverImagePath { get; set; }

    /// <summary>
    /// 项目设置（JSON格式）
    /// </summary>
    public string? Settings { get; set; }

    /// <summary>
    /// 项目统计信息（JSON格式）
    /// </summary>
    public string? Statistics { get; set; }

    /// <summary>
    /// 项目标签
    /// </summary>
    [MaxLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// 项目优先级
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// 项目进度（0-100）
    /// </summary>
    public int Progress { get; set; } = 0;

    /// <summary>
    /// 最后访问时间
    /// </summary>
    public DateTime? LastAccessedAt { get; set; }

    /// <summary>
    /// 项目路径
    /// </summary>
    [MaxLength(1000)]
    public string? ProjectPath { get; set; }

    /// <summary>
    /// 项目备注
    /// </summary>
    public string? Notes { get; set; }

    // 导航属性
    /// <summary>
    /// 项目包含的卷宗
    /// </summary>
    public virtual ICollection<Volume> Volumes { get; set; } = new List<Volume>();

    /// <summary>
    /// 项目包含的人物
    /// </summary>
    public virtual ICollection<Character> Characters { get; set; } = new List<Character>();

    /// <summary>
    /// 项目包含的势力
    /// </summary>
    public virtual ICollection<Faction> Factions { get; set; } = new List<Faction>();

    /// <summary>
    /// 项目包含的世界设定
    /// </summary>
    public virtual ICollection<WorldSetting> WorldSettings { get; set; } = new List<WorldSetting>();

    /// <summary>
    /// 项目包含的修炼体系
    /// </summary>
    public virtual ICollection<CultivationSystem> CultivationSystems { get; set; } = new List<CultivationSystem>();

    /// <summary>
    /// 项目包含的政治体系
    /// </summary>
    public virtual ICollection<PoliticalSystem> PoliticalSystems { get; set; } = new List<PoliticalSystem>();

    /// <summary>
    /// 项目包含的剧情
    /// </summary>
    public virtual ICollection<Plot> Plots { get; set; } = new List<Plot>();

    /// <summary>
    /// 项目包含的资源
    /// </summary>
    public virtual ICollection<Resource> Resources { get; set; } = new List<Resource>();

    /// <summary>
    /// 项目包含的种族
    /// </summary>
    public virtual ICollection<Race> Races { get; set; } = new List<Race>();

    /// <summary>
    /// 项目包含的秘境
    /// </summary>
    public virtual ICollection<SecretRealm> SecretRealms { get; set; } = new List<SecretRealm>();

    /// <summary>
    /// 项目包含的货币体系
    /// </summary>
    public virtual ICollection<CurrencySystem> CurrencySystems { get; set; } = new List<CurrencySystem>();

    /// <summary>
    /// 项目包含的关系网络
    /// </summary>
    public virtual ICollection<RelationshipNetwork> RelationshipNetworks { get; set; } = new List<RelationshipNetwork>();

    /// <summary>
    /// 项目包含的种族关系
    /// </summary>
    public virtual ICollection<RaceRelationship> RaceRelationships { get; set; } = new List<RaceRelationship>();
}
