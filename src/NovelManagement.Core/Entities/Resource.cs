using System.ComponentModel.DataAnnotations;
using NovelManagement.Core.Entities;

namespace NovelManagement.Core.Entities;

/// <summary>
/// 资源实体
/// </summary>
public class Resource : BaseEntity
{
    /// <summary>
    /// 资源名称
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 资源类型
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty; // 矿脉、灵脉、龙脉、草药、灵兽、水源、人口、装备、灵宝

    /// <summary>
    /// 资源位置
    /// </summary>
    [MaxLength(500)]
    public string? Location { get; set; }

    /// <summary>
    /// 控制势力ID
    /// </summary>
    public Guid? ControllingFactionId { get; set; }

    /// <summary>
    /// 资源稀有度
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Rarity { get; set; } = "普通"; // 普通、不常见、稀有、史诗、传说

    /// <summary>
    /// 开采难度（1-10分）
    /// </summary>
    public int ExtractionDifficulty { get; set; } = 5;

    /// <summary>
    /// 经济价值（1-10分）
    /// </summary>
    public int EconomicValue { get; set; } = 5;

    /// <summary>
    /// 再生速度
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string RegenerationSpeed { get; set; } = "中等"; // 极慢、缓慢、中等、快速、极快

    /// <summary>
    /// 资源状态
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "活跃"; // 活跃、濒临枯竭、枯竭、废弃、受保护、争夺中

    /// <summary>
    /// 资源描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 开采方法及条件
    /// </summary>
    public string? ExtractionMethod { get; set; }

    /// <summary>
    /// 所属项目ID
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// 重要性等级（1-10）
    /// </summary>
    public int Importance { get; set; } = 5;

    /// <summary>
    /// 资源标签
    /// </summary>
    [MaxLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// 当前储量
    /// </summary>
    public long? CurrentReserves { get; set; }

    /// <summary>
    /// 最大储量
    /// </summary>
    public long? MaxReserves { get; set; }

    /// <summary>
    /// 年产量
    /// </summary>
    public long? AnnualOutput { get; set; }

    /// <summary>
    /// 发现时间
    /// </summary>
    public DateTime? DiscoveryDate { get; set; }

    /// <summary>
    /// 最后开采时间
    /// </summary>
    public DateTime? LastExtractionDate { get; set; }

    // 导航属性
    /// <summary>
    /// 所属项目
    /// </summary>
    public virtual Project Project { get; set; } = null!;

    /// <summary>
    /// 控制势力
    /// </summary>
    public virtual Faction? ControllingFaction { get; set; }

    /// <summary>
    /// 关联的秘境
    /// </summary>
    public virtual ICollection<SecretRealm> SecretRealms { get; set; } = new List<SecretRealm>();
}
