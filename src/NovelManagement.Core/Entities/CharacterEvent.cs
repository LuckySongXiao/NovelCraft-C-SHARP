using System.ComponentModel.DataAnnotations;

namespace NovelManagement.Core.Entities;

/// <summary>
/// 角色事件/履历记录实体
/// </summary>
/// <remarks>
/// 记录角色的重要经历、关键事件、成长节点等，形成结构化的时间线履历
/// </remarks>
public class CharacterEvent : BaseEntity
{
    /// <summary>
    /// 所属角色ID
    /// </summary>
    public Guid CharacterId { get; set; }

    /// <summary>
    /// 事件标题
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 事件描述/详细内容
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 事件类型（如：出生、拜师、突破、参战、结盟、离队、死亡、其他）
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string EventType { get; set; } = "其他";

    /// <summary>
    /// 事件发生时间（小说世界内的时间描述，如"修炼第三年"）
    /// </summary>
    [MaxLength(200)]
    public string? StoryTime { get; set; }

    /// <summary>
    /// 事件排序序号（用于时间线排序）
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 关联章节ID（事件发生在哪个章节）
    /// </summary>
    public Guid? ChapterId { get; set; }

    /// <summary>
    /// 关联剧情ID（事件属于哪条剧情线）
    /// </summary>
    public Guid? PlotId { get; set; }

    /// <summary>
    /// 事件影响/后果
    /// </summary>
    public string? Impact { get; set; }

    /// <summary>
    /// 涉及的其他角色ID列表（逗号分隔）
    /// </summary>
    [MaxLength(1000)]
    public string? InvolvedCharacterIds { get; set; }

    /// <summary>
    /// 事件标签
    /// </summary>
    [MaxLength(500)]
    public string? Tags { get; set; }

    // 导航属性

    /// <summary>
    /// 所属角色
    /// </summary>
    public virtual Character Character { get; set; } = null!;

    /// <summary>
    /// 关联章节
    /// </summary>
    public virtual Chapter? Chapter { get; set; }

    /// <summary>
    /// 关联剧情
    /// </summary>
    public virtual Plot? Plot { get; set; }
}
