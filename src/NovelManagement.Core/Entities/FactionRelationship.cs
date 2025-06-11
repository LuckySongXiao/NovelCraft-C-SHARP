using System.ComponentModel.DataAnnotations;

namespace NovelManagement.Core.Entities;

/// <summary>
/// 势力关系实体
/// </summary>
public class FactionRelationship : BaseEntity
{
    /// <summary>
    /// 关系发起方势力ID
    /// </summary>
    public Guid SourceFactionId { get; set; }

    /// <summary>
    /// 关系接收方势力ID
    /// </summary>
    public Guid TargetFactionId { get; set; }

    /// <summary>
    /// 关系类型
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string RelationshipType { get; set; } = string.Empty;

    /// <summary>
    /// 关系强度（1-10）
    /// </summary>
    public int Intensity { get; set; } = 1;

    /// <summary>
    /// 关系状态
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Active";

    /// <summary>
    /// 关系名称/标题
    /// </summary>
    [MaxLength(100)]
    public string? RelationshipName { get; set; }

    /// <summary>
    /// 关系描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 关系发展历程
    /// </summary>
    public string? DevelopmentHistory { get; set; }

    /// <summary>
    /// 关键事件
    /// </summary>
    public string? KeyEvents { get; set; }

    /// <summary>
    /// 关系影响
    /// </summary>
    public string? Impact { get; set; }

    /// <summary>
    /// 关系开始时间
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// 关系结束时间
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// 关系标签
    /// </summary>
    [MaxLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// 关系备注
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// 是否为双向关系
    /// </summary>
    public bool IsBidirectional { get; set; } = true;

    /// <summary>
    /// 关系重要性（1-10）
    /// </summary>
    public int Importance { get; set; } = 1;

    /// <summary>
    /// 军事实力对比
    /// </summary>
    public string? MilitaryComparison { get; set; }

    /// <summary>
    /// 经济关系
    /// </summary>
    public string? EconomicRelations { get; set; }

    // 导航属性
    /// <summary>
    /// 关系发起方势力
    /// </summary>
    public virtual Faction SourceFaction { get; set; } = null!;

    /// <summary>
    /// 关系接收方势力
    /// </summary>
    public virtual Faction TargetFaction { get; set; } = null!;
}
