using System.ComponentModel.DataAnnotations;

namespace NovelManagement.Core.Entities;

/// <summary>
/// 人物角色实体
/// </summary>
public class Character : BaseEntity
{
    /// <summary>
    /// 角色姓名
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 角色类型
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 角色性别
    /// </summary>
    [MaxLength(20)]
    public string? Gender { get; set; }

    /// <summary>
    /// 角色年龄
    /// </summary>
    public int? Age { get; set; }

    /// <summary>
    /// 角色修为
    /// </summary>
    [MaxLength(100)]
    public string? CultivationLevel { get; set; }

    /// <summary>
    /// 所属势力ID
    /// </summary>
    public Guid? FactionId { get; set; }

    /// <summary>
    /// 所属种族ID
    /// </summary>
    public Guid? RaceId { get; set; }

    /// <summary>
    /// 角色重要性
    /// </summary>
    public int Importance { get; set; } = 1;

    /// <summary>
    /// 外貌描述
    /// </summary>
    public string? Appearance { get; set; }

    /// <summary>
    /// 性格特点
    /// </summary>
    public string? Personality { get; set; }

    /// <summary>
    /// 背景故事
    /// </summary>
    public string? Background { get; set; }

    /// <summary>
    /// 能力技能
    /// </summary>
    public string? Abilities { get; set; }

    /// <summary>
    /// 所属项目ID
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// 角色头像路径
    /// </summary>
    [MaxLength(500)]
    public string? AvatarPath { get; set; }

    /// <summary>
    /// 角色标签
    /// </summary>
    [MaxLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// 角色备注
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// 角色状态
    /// </summary>
    [MaxLength(50)]
    public string Status { get; set; } = "Active";

    /// <summary>
    /// 首次出场章节
    /// </summary>
    public Guid? FirstAppearanceChapterId { get; set; }

    /// <summary>
    /// 最后出场章节
    /// </summary>
    public Guid? LastAppearanceChapterId { get; set; }

    // 导航属性
    /// <summary>
    /// 所属项目
    /// </summary>
    public virtual Project Project { get; set; } = null!;

    /// <summary>
    /// 所属势力
    /// </summary>
    public virtual Faction? Faction { get; set; }

    /// <summary>
    /// 所属种族
    /// </summary>
    public virtual Race? Race { get; set; }

    /// <summary>
    /// 人物关系（作为关系发起方）
    /// </summary>
    public virtual ICollection<CharacterRelationship> RelationshipsAsSource { get; set; } = new List<CharacterRelationship>();

    /// <summary>
    /// 人物关系（作为关系接收方）
    /// </summary>
    public virtual ICollection<CharacterRelationship> RelationshipsAsTarget { get; set; } = new List<CharacterRelationship>();

    /// <summary>
    /// 相关剧情
    /// </summary>
    public virtual ICollection<Plot> RelatedPlots { get; set; } = new List<Plot>();

    /// <summary>
    /// 参与的关系网络
    /// </summary>
    public virtual ICollection<RelationshipNetwork> ParticipatedNetworks { get; set; } = new List<RelationshipNetwork>();

    /// <summary>
    /// 作为中心的关系网络
    /// </summary>
    public virtual ICollection<RelationshipNetwork> CentralNetworks { get; set; } = new List<RelationshipNetwork>();
}
