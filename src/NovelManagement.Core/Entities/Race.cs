using System.ComponentModel.DataAnnotations;
using NovelManagement.Core.Entities;

namespace NovelManagement.Core.Entities;

/// <summary>
/// 种族实体
/// </summary>
public class Race : BaseEntity
{
    /// <summary>
    /// 种族名称
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 种族类型
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty; // 人类、精灵、魔法、兽族、元素、不死、修罗、灵异、神族

    /// <summary>
    /// 种族人口
    /// </summary>
    public long Population { get; set; } = 0;

    /// <summary>
    /// 主要领土
    /// </summary>
    [MaxLength(500)]
    public string? MainTerritory { get; set; }

    /// <summary>
    /// 统治区域
    /// </summary>
    public string? RulingArea { get; set; }

    /// <summary>
    /// 实力等级（1-10分）
    /// </summary>
    public int PowerLevel { get; set; } = 5;

    /// <summary>
    /// 影响力（1-10分）
    /// </summary>
    public int Influence { get; set; } = 5;

    /// <summary>
    /// 种族状态
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "稳定"; // 繁荣、稳定、衰落、濒危、灭绝

    /// <summary>
    /// 种族特征
    /// </summary>
    public string? Characteristics { get; set; }

    /// <summary>
    /// 文化背景
    /// </summary>
    public string? CulturalBackground { get; set; }

    /// <summary>
    /// 所属项目ID
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// 重要性等级（1-10）
    /// </summary>
    public int Importance { get; set; } = 5;

    /// <summary>
    /// 种族标签
    /// </summary>
    [MaxLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// 平均寿命
    /// </summary>
    public int? AverageLifespan { get; set; }

    /// <summary>
    /// 生育率
    /// </summary>
    public decimal? BirthRate { get; set; }

    /// <summary>
    /// 主要语言
    /// </summary>
    [MaxLength(100)]
    public string? PrimaryLanguage { get; set; }

    /// <summary>
    /// 主要宗教
    /// </summary>
    [MaxLength(100)]
    public string? PrimaryReligion { get; set; }

    /// <summary>
    /// 种族能力
    /// </summary>
    public string? RacialAbilities { get; set; }

    /// <summary>
    /// 种族弱点
    /// </summary>
    public string? RacialWeaknesses { get; set; }

    // 导航属性
    /// <summary>
    /// 所属项目
    /// </summary>
    public virtual Project Project { get; set; } = null!;

    /// <summary>
    /// 种族成员
    /// </summary>
    public virtual ICollection<Character> Members { get; set; } = new List<Character>();

    /// <summary>
    /// 盟友种族关系（作为关系发起方）
    /// </summary>
    public virtual ICollection<RaceRelationship> AllyRelationshipsAsSource { get; set; } = new List<RaceRelationship>();

    /// <summary>
    /// 盟友种族关系（作为关系接收方）
    /// </summary>
    public virtual ICollection<RaceRelationship> AllyRelationshipsAsTarget { get; set; } = new List<RaceRelationship>();

    /// <summary>
    /// 敌对种族关系（作为关系发起方）
    /// </summary>
    public virtual ICollection<RaceRelationship> EnemyRelationshipsAsSource { get; set; } = new List<RaceRelationship>();

    /// <summary>
    /// 敌对种族关系（作为关系接收方）
    /// </summary>
    public virtual ICollection<RaceRelationship> EnemyRelationshipsAsTarget { get; set; } = new List<RaceRelationship>();
}
