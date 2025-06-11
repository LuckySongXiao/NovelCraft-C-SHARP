using System.ComponentModel.DataAnnotations;
using NovelManagement.Core.Entities;

namespace NovelManagement.Core.Entities;

/// <summary>
/// 秘境实体
/// </summary>
public class SecretRealm : BaseEntity
{
    /// <summary>
    /// 秘境名称
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 秘境类型
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty; // 地下城、豢兽乐园、祭天法坛、野兽森林、海妖诞生地、天庭碎片、佛陀道场、菩萨道场、小酆都

    /// <summary>
    /// 秘境位置
    /// </summary>
    [MaxLength(500)]
    public string? Location { get; set; }

    /// <summary>
    /// 发现者势力ID
    /// </summary>
    public Guid? DiscovererFactionId { get; set; }

    /// <summary>
    /// 危险等级（1-10分）
    /// </summary>
    public int DangerLevel { get; set; } = 5;

    /// <summary>
    /// 容量限制（人数）
    /// </summary>
    public int? CapacityLimit { get; set; }

    /// <summary>
    /// 时间限制（小时）
    /// </summary>
    public int? TimeLimit { get; set; }

    /// <summary>
    /// 推荐修为
    /// </summary>
    [MaxLength(100)]
    public string? RecommendedCultivation { get; set; }

    /// <summary>
    /// 秘境状态
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "隐藏"; // 开放、封印、毁坏、隐藏

    /// <summary>
    /// 探索条件
    /// </summary>
    public string? ExplorationConditions { get; set; }

    /// <summary>
    /// 探索奖励
    /// </summary>
    public string? ExplorationRewards { get; set; }

    /// <summary>
    /// 秘境描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 秘境攻略
    /// </summary>
    public string? Strategy { get; set; }

    /// <summary>
    /// 最后探索时间
    /// </summary>
    public DateTime? LastExplorationDate { get; set; }

    /// <summary>
    /// 所属项目ID
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// 重要性等级（1-10）
    /// </summary>
    public int Importance { get; set; } = 5;

    /// <summary>
    /// 秘境标签
    /// </summary>
    [MaxLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// 发现时间
    /// </summary>
    public DateTime? DiscoveryDate { get; set; }

    /// <summary>
    /// 开放时间
    /// </summary>
    public DateTime? OpenDate { get; set; }

    /// <summary>
    /// 关闭时间
    /// </summary>
    public DateTime? CloseDate { get; set; }

    /// <summary>
    /// 探索次数
    /// </summary>
    public int ExplorationCount { get; set; } = 0;

    /// <summary>
    /// 成功探索次数
    /// </summary>
    public int SuccessfulExplorationCount { get; set; } = 0;

    /// <summary>
    /// 维度信息
    /// </summary>
    [MaxLength(200)]
    public string? DimensionInfo { get; set; }

    /// <summary>
    /// 入口信息
    /// </summary>
    public string? EntranceInfo { get; set; }

    /// <summary>
    /// 内部结构
    /// </summary>
    public string? InternalStructure { get; set; }

    /// <summary>
    /// 特殊规则
    /// </summary>
    public string? SpecialRules { get; set; }

    // 导航属性
    /// <summary>
    /// 所属项目
    /// </summary>
    public virtual Project Project { get; set; } = null!;

    /// <summary>
    /// 发现者势力
    /// </summary>
    public virtual Faction? DiscovererFaction { get; set; }

    /// <summary>
    /// 相关资源
    /// </summary>
    public virtual ICollection<Resource> RelatedResources { get; set; } = new List<Resource>();
}
