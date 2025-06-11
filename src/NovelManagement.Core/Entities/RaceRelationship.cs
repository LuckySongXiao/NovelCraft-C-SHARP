using System.ComponentModel.DataAnnotations;
using NovelManagement.Core.Entities;

namespace NovelManagement.Core.Entities;

/// <summary>
/// 种族关系实体
/// </summary>
public class RaceRelationship : BaseEntity
{
    /// <summary>
    /// 源种族ID
    /// </summary>
    public Guid SourceRaceId { get; set; }

    /// <summary>
    /// 目标种族ID
    /// </summary>
    public Guid TargetRaceId { get; set; }

    /// <summary>
    /// 关系类型
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string RelationshipType { get; set; } = string.Empty; // 盟友、敌对、中立、附庸、宗主

    /// <summary>
    /// 关系强度（1-10）
    /// </summary>
    public int Strength { get; set; } = 5;

    /// <summary>
    /// 关系状态
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "稳定"; // 稳定、紧张、恶化、改善、破裂

    /// <summary>
    /// 关系描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 关系历史
    /// </summary>
    public string? History { get; set; }

    /// <summary>
    /// 关键事件
    /// </summary>
    public string? KeyEvents { get; set; }

    /// <summary>
    /// 所属项目ID
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// 重要性等级（1-10）
    /// </summary>
    public int Importance { get; set; } = 5;

    /// <summary>
    /// 关系标签
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
    /// 是否双向
    /// </summary>
    public bool IsMutual { get; set; } = true;

    // 导航属性
    /// <summary>
    /// 所属项目
    /// </summary>
    public virtual Project Project { get; set; } = null!;

    /// <summary>
    /// 源种族
    /// </summary>
    public virtual Race SourceRace { get; set; } = null!;

    /// <summary>
    /// 目标种族
    /// </summary>
    public virtual Race TargetRace { get; set; } = null!;
}
