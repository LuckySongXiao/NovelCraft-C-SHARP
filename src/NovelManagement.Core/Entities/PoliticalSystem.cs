using System.ComponentModel.DataAnnotations;

namespace NovelManagement.Core.Entities;

/// <summary>
/// 政治体系实体
/// </summary>
public class PoliticalSystem : BaseEntity
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
    /// 政治等级制度
    /// </summary>
    public string? Hierarchy { get; set; }

    /// <summary>
    /// 政治稳定性（1-100）
    /// </summary>
    public int Stability { get; set; } = 50;

    /// <summary>
    /// 政治影响力（1-100）
    /// </summary>
    public int Influence { get; set; } = 50;

    /// <summary>
    /// 体系描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 政治结构
    /// </summary>
    public string? Structure { get; set; }

    /// <summary>
    /// 权力分配
    /// </summary>
    public string? PowerDistribution { get; set; }

    /// <summary>
    /// 法律制度
    /// </summary>
    public string? LegalSystem { get; set; }

    /// <summary>
    /// 选举制度
    /// </summary>
    public string? ElectionSystem { get; set; }

    /// <summary>
    /// 行政体系
    /// </summary>
    public string? AdministrativeSystem { get; set; }

    /// <summary>
    /// 军事体系
    /// </summary>
    public string? MilitarySystem { get; set; }

    /// <summary>
    /// 经济制度
    /// </summary>
    public string? EconomicSystem { get; set; }

    /// <summary>
    /// 社会阶层
    /// </summary>
    public string? SocialHierarchy { get; set; }

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
    /// 政治职位
    /// </summary>
    public virtual ICollection<PoliticalPosition> Positions { get; set; } = new List<PoliticalPosition>();
}

/// <summary>
/// 政治职位实体
/// </summary>
public class PoliticalPosition : BaseEntity
{
    /// <summary>
    /// 职位名称
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 职位等级
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// 职位描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 职位权力
    /// </summary>
    public string? Powers { get; set; }

    /// <summary>
    /// 职位职责
    /// </summary>
    public string? Responsibilities { get; set; }

    /// <summary>
    /// 任职条件
    /// </summary>
    public string? Requirements { get; set; }

    /// <summary>
    /// 任期
    /// </summary>
    public string? Term { get; set; }

    /// <summary>
    /// 所属政治体系ID
    /// </summary>
    public Guid PoliticalSystemId { get; set; }

    /// <summary>
    /// 职位标签
    /// </summary>
    [MaxLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// 职位备注
    /// </summary>
    public string? Notes { get; set; }

    // 导航属性
    /// <summary>
    /// 所属政治体系
    /// </summary>
    public virtual PoliticalSystem PoliticalSystem { get; set; } = null!;
}
