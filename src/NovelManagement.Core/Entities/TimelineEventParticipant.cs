using System.ComponentModel.DataAnnotations;

namespace NovelManagement.Core.Entities;

/// <summary>
/// 时间线事件参与者实体
/// </summary>
/// <remarks>
/// 参与者优先通过 <see cref="CharacterId"/> 与项目内角色建立强关联；
/// 无法匹配到角色时仍保留名称文本，避免历史数据丢失。
/// </remarks>
public class TimelineEventParticipant : BaseEntity
{
    /// <summary>
    /// 所属时间线事件ID
    /// </summary>
    public Guid TimelineEventId { get; set; }

    /// <summary>
    /// 参与者名称
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 参与者类型（角色 / 势力 / 其他）
    /// </summary>
    [MaxLength(50)]
    public string? Type { get; set; }

    /// <summary>
    /// 在事件中的角色定位
    /// </summary>
    [MaxLength(200)]
    public string? Role { get; set; }

    /// <summary>
    /// 关联角色ID（能在项目内按名称匹配到角色时写入，用于一致性检查与履历关联）
    /// </summary>
    public Guid? CharacterId { get; set; }

    /// <summary>
    /// 参与者显示顺序
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 所属时间线事件
    /// </summary>
    public virtual TimelineEvent TimelineEvent { get; set; } = null!;
}
