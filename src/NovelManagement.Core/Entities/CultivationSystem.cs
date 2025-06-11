using System.ComponentModel.DataAnnotations;

namespace NovelManagement.Core.Entities;

/// <summary>
/// 修炼体系实体
/// </summary>
public class CultivationSystem : BaseEntity
{
    /// <summary>
    /// 体系名称
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 体系类型
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 修炼难度（1-100）
    /// </summary>
    public int Difficulty { get; set; } = 50;

    /// <summary>
    /// 最高等级
    /// </summary>
    public int MaxLevel { get; set; } = 10;

    /// <summary>
    /// 体系描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 修炼方法
    /// </summary>
    public string? CultivationMethod { get; set; }

    /// <summary>
    /// 境界划分
    /// </summary>
    public string? RealmDivision { get; set; }

    /// <summary>
    /// 突破条件
    /// </summary>
    public string? BreakthroughConditions { get; set; }

    /// <summary>
    /// 修炼资源
    /// </summary>
    public string? CultivationResources { get; set; }

    /// <summary>
    /// 体系特点
    /// </summary>
    public string? Characteristics { get; set; }

    /// <summary>
    /// 修炼风险
    /// </summary>
    public string? Risks { get; set; }

    /// <summary>
    /// 所属项目ID
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// 体系重要性（1-10）
    /// </summary>
    public int Importance { get; set; } = 1;

    /// <summary>
    /// 体系图片路径
    /// </summary>
    [MaxLength(500)]
    public string? ImagePath { get; set; }

    /// <summary>
    /// 体系标签
    /// </summary>
    [MaxLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// 体系备注
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// 体系状态
    /// </summary>
    [MaxLength(50)]
    public string Status { get; set; } = "Active";

    /// <summary>
    /// 排序索引
    /// </summary>
    public int Order { get; set; } = 0;

    // 导航属性
    /// <summary>
    /// 所属项目
    /// </summary>
    public virtual Project Project { get; set; } = null!;

    /// <summary>
    /// 修炼等级
    /// </summary>
    public virtual ICollection<CultivationLevel> Levels { get; set; } = new List<CultivationLevel>();
}

/// <summary>
/// 修炼等级实体
/// </summary>
public class CultivationLevel : BaseEntity
{
    /// <summary>
    /// 等级名称
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 等级序号
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 等级描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 突破条件
    /// </summary>
    public string? BreakthroughCondition { get; set; }

    /// <summary>
    /// 能力特点
    /// </summary>
    public string? Abilities { get; set; }

    /// <summary>
    /// 修炼时间
    /// </summary>
    public string? CultivationTime { get; set; }

    /// <summary>
    /// 所属修炼体系ID
    /// </summary>
    public Guid CultivationSystemId { get; set; }

    /// <summary>
    /// 等级标签
    /// </summary>
    [MaxLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// 等级备注
    /// </summary>
    public string? Notes { get; set; }

    // 导航属性
    /// <summary>
    /// 所属修炼体系
    /// </summary>
    public virtual CultivationSystem CultivationSystem { get; set; } = null!;
}
