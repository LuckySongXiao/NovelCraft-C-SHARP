using System.ComponentModel.DataAnnotations;

namespace NovelManagement.Core.Entities;

/// <summary>
/// 势力实体
/// </summary>
public class Faction : BaseEntity
{
    /// <summary>
    /// 势力名称
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 势力类型
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 势力等级
    /// </summary>
    [MaxLength(50)]
    public string? Level { get; set; }

    /// <summary>
    /// 势力领袖ID
    /// </summary>
    public Guid? LeaderId { get; set; }

    /// <summary>
    /// 父势力ID
    /// </summary>
    public Guid? ParentFactionId { get; set; }

    /// <summary>
    /// 势力实力等级（1-100）
    /// </summary>
    public int PowerLevel { get; set; } = 50;

    /// <summary>
    /// 势力影响力（1-100）
    /// </summary>
    public int Influence { get; set; } = 50;

    /// <summary>
    /// 势力重要性（1-10）
    /// </summary>
    public int Importance { get; set; } = 5;

    /// <summary>
    /// 成员数量
    /// </summary>
    public int? MemberCount { get; set; }

    /// <summary>
    /// 势力实力评级
    /// </summary>
    public int? PowerRating { get; set; }

    /// <summary>
    /// 势力影响力评级
    /// </summary>
    public int? InfluenceRating { get; set; }

    /// <summary>
    /// 势力描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 势力历史
    /// </summary>
    public string? History { get; set; }

    /// <summary>
    /// 特色能力
    /// </summary>
    public string? SpecialAbilities { get; set; }

    /// <summary>
    /// 势力资源
    /// </summary>
    public string? Resources { get; set; }

    /// <summary>
    /// 所属项目ID
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// 势力徽章路径
    /// </summary>
    [MaxLength(500)]
    public string? EmblemPath { get; set; }

    /// <summary>
    /// 势力标签
    /// </summary>
    [MaxLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// 势力备注
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// 势力状态
    /// </summary>
    [MaxLength(50)]
    public string Status { get; set; } = "Active";

    /// <summary>
    /// 势力总部位置
    /// </summary>
    [MaxLength(200)]
    public string? Headquarters { get; set; }

    /// <summary>
    /// 势力控制区域
    /// </summary>
    public string? Territory { get; set; }

    // 导航属性
    /// <summary>
    /// 所属项目
    /// </summary>
    public virtual Project Project { get; set; } = null!;

    /// <summary>
    /// 势力成员
    /// </summary>
    public virtual ICollection<Character> Members { get; set; } = new List<Character>();

    /// <summary>
    /// 盟友势力关系（作为关系发起方）
    /// </summary>
    public virtual ICollection<FactionRelationship> AllianceRelationshipsAsSource { get; set; } = new List<FactionRelationship>();

    /// <summary>
    /// 盟友势力关系（作为关系接收方）
    /// </summary>
    public virtual ICollection<FactionRelationship> AllianceRelationshipsAsTarget { get; set; } = new List<FactionRelationship>();

    /// <summary>
    /// 控制的资源
    /// </summary>
    public virtual ICollection<Resource> ControlledResources { get; set; } = new List<Resource>();

    /// <summary>
    /// 发现的秘境
    /// </summary>
    public virtual ICollection<SecretRealm> DiscoveredSecretRealms { get; set; } = new List<SecretRealm>();

    /// <summary>
    /// 使用的货币体系
    /// </summary>
    public virtual ICollection<CurrencySystem> UsedCurrencySystems { get; set; } = new List<CurrencySystem>();
}
