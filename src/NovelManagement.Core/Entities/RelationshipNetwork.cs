using System.ComponentModel.DataAnnotations;
using NovelManagement.Core.Entities;

namespace NovelManagement.Core.Entities;

/// <summary>
/// 关系网络实体 - 用于管理复杂的人物关系网络
/// </summary>
public class RelationshipNetwork : BaseEntity
{
    /// <summary>
    /// 网络名称
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 网络类型
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty; // 家族网络、势力网络、朋友圈、敌对网络、师承网络

    /// <summary>
    /// 网络描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 网络中心人物ID
    /// </summary>
    public Guid? CentralCharacterId { get; set; }

    /// <summary>
    /// 网络复杂度（1-10）
    /// </summary>
    public int Complexity { get; set; } = 5;

    /// <summary>
    /// 网络稳定性（1-10）
    /// </summary>
    public int Stability { get; set; } = 5;

    /// <summary>
    /// 网络影响力（1-10）
    /// </summary>
    public int Influence { get; set; } = 5;

    /// <summary>
    /// 网络状态
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "活跃"; // 活跃、衰落、重组、解散

    /// <summary>
    /// 关键事件
    /// </summary>
    public string? KeyEvents { get; set; }

    /// <summary>
    /// 发展历史
    /// </summary>
    public string? DevelopmentHistory { get; set; }

    /// <summary>
    /// 网络规则
    /// </summary>
    public string? NetworkRules { get; set; }

    /// <summary>
    /// 所属项目ID
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// 重要性等级（1-10）
    /// </summary>
    public int Importance { get; set; } = 5;

    /// <summary>
    /// 网络标签
    /// </summary>
    [MaxLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// 建立时间
    /// </summary>
    public DateTime? EstablishedDate { get; set; }

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime? LastUpdatedDate { get; set; }

    /// <summary>
    /// 是否公开
    /// </summary>
    public bool IsPublic { get; set; } = true;

    /// <summary>
    /// 网络层级
    /// </summary>
    public int? HierarchyLevel { get; set; }

    /// <summary>
    /// 成员数量
    /// </summary>
    public int MemberCount { get; set; } = 0;

    /// <summary>
    /// 关系数量
    /// </summary>
    public int RelationshipCount { get; set; } = 0;

    /// <summary>
    /// 网络密度
    /// </summary>
    public decimal? NetworkDensity { get; set; }

    /// <summary>
    /// 网络图数据（JSON格式）
    /// </summary>
    public string? NetworkGraphData { get; set; }

    // 导航属性
    /// <summary>
    /// 所属项目
    /// </summary>
    public virtual Project Project { get; set; } = null!;

    /// <summary>
    /// 网络中心人物
    /// </summary>
    public virtual Character? CentralCharacter { get; set; }

    /// <summary>
    /// 网络成员
    /// </summary>
    public virtual ICollection<Character> Members { get; set; } = new List<Character>();

    /// <summary>
    /// 网络中的关系
    /// </summary>
    public virtual ICollection<CharacterRelationship> Relationships { get; set; } = new List<CharacterRelationship>();
}
