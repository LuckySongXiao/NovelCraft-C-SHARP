using System.ComponentModel.DataAnnotations;

namespace NovelManagement.Core.Entities;

/// <summary>
/// 时间线事件实体
/// </summary>
/// <remarks>
/// 原时间线数据以项目级 JSON 文件存储（AppData/Roaming/NovelManagement/timelines/{projectId}.json），
/// 现迁移为数据库存储，便于一致性检查、人物履历关联与跨设备同步。
/// </remarks>
public class TimelineEvent : BaseEntity
{
    /// <summary>
    /// 所属项目ID
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// 事件标题
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 事件类别（历史事件 / 剧情事件 / 角色事件 / 势力事件 / 世界事件 / 修炼事件）
    /// </summary>
    [MaxLength(50)]
    public string Category { get; set; } = "历史事件";

    /// <summary>
    /// 事件发生日期
    /// </summary>
    public DateTime EventDate { get; set; }

    /// <summary>
    /// 事件地点
    /// </summary>
    [MaxLength(200)]
    public string? Location { get; set; }

    /// <summary>
    /// 重要程度（极高 / 高 / 中 / 低）
    /// </summary>
    [MaxLength(50)]
    public string? Importance { get; set; }

    /// <summary>
    /// 事件状态（已完成 / 进行中 / 计划中 / 已取消）
    /// </summary>
    [MaxLength(50)]
    public string? Status { get; set; }

    /// <summary>
    /// 事件描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 事件影响
    /// </summary>
    public string? Impact { get; set; }

    /// <summary>
    /// 显示排序序号（默认按事件日期排序，同日事件用该序号稳定排序）
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// 关联章节ID
    /// </summary>
    public Guid? ChapterId { get; set; }

    /// <summary>
    /// 关联剧情ID
    /// </summary>
    public Guid? PlotId { get; set; }

    /// <summary>
    /// 事件标签
    /// </summary>
    [MaxLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// 迁移来源标识（旧 JSON 存储中的整数 Id），仅用于一次性迁移去重，日常业务不使用
    /// </summary>
    [MaxLength(50)]
    public string? LegacyId { get; set; }

    // 导航属性

    /// <summary>
    /// 所属项目
    /// </summary>
    public virtual Project Project { get; set; } = null!;

    /// <summary>
    /// 事件参与者
    /// </summary>
    public virtual List<TimelineEventParticipant> Participants { get; set; } = new();

    /// <summary>
    /// 关联章节
    /// </summary>
    public virtual Chapter? Chapter { get; set; }

    /// <summary>
    /// 关联剧情
    /// </summary>
    public virtual Plot? Plot { get; set; }
}
