using System.ComponentModel.DataAnnotations;

namespace NovelManagement.Application.DTOs;

/// <summary>
/// 修炼体系DTO
/// </summary>
public class CultivationSystemDto
{
    /// <summary>
    /// 体系ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 体系名称
    /// </summary>
    [Required(ErrorMessage = "体系名称不能为空")]
    [StringLength(100, ErrorMessage = "体系名称长度不能超过100个字符")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 体系类型
    /// </summary>
    [Required(ErrorMessage = "体系类型不能为空")]
    [StringLength(50, ErrorMessage = "体系类型长度不能超过50个字符")]
    public string Type { get; set; } = string.Empty;

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
    [Range(1, 10, ErrorMessage = "体系重要性必须在1-10之间")]
    public int Importance { get; set; } = 1;

    /// <summary>
    /// 体系图片路径
    /// </summary>
    [StringLength(500, ErrorMessage = "图片路径长度不能超过500个字符")]
    public string? ImagePath { get; set; }

    /// <summary>
    /// 体系标签
    /// </summary>
    [StringLength(500, ErrorMessage = "标签长度不能超过500个字符")]
    public string? Tags { get; set; }

    /// <summary>
    /// 体系备注
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// 体系状态
    /// </summary>
    [StringLength(50, ErrorMessage = "状态长度不能超过50个字符")]
    public string Status { get; set; } = "Active";

    /// <summary>
    /// 排序索引
    /// </summary>
    public int OrderIndex { get; set; } = 0;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// 修炼等级列表
    /// </summary>
    public List<CultivationLevelDto> Levels { get; set; } = new();
}

/// <summary>
/// 修炼等级DTO
/// </summary>
public class CultivationLevelDto
{
    /// <summary>
    /// 等级ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 等级名称
    /// </summary>
    [Required(ErrorMessage = "等级名称不能为空")]
    [StringLength(100, ErrorMessage = "等级名称长度不能超过100个字符")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 等级序号
    /// </summary>
    public int OrderIndex { get; set; }

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
    [StringLength(500, ErrorMessage = "标签长度不能超过500个字符")]
    public string? Tags { get; set; }

    /// <summary>
    /// 等级备注
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 创建修炼体系DTO
/// </summary>
public class CreateCultivationSystemDto
{
    /// <summary>
    /// 体系名称
    /// </summary>
    [Required(ErrorMessage = "体系名称不能为空")]
    [StringLength(100, ErrorMessage = "体系名称长度不能超过100个字符")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 体系类型
    /// </summary>
    [Required(ErrorMessage = "体系类型不能为空")]
    [StringLength(50, ErrorMessage = "体系类型长度不能超过50个字符")]
    public string Type { get; set; } = string.Empty;

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
    [Range(1, 10, ErrorMessage = "体系重要性必须在1-10之间")]
    public int Importance { get; set; } = 1;

    /// <summary>
    /// 体系图片路径
    /// </summary>
    [StringLength(500, ErrorMessage = "图片路径长度不能超过500个字符")]
    public string? ImagePath { get; set; }

    /// <summary>
    /// 体系标签
    /// </summary>
    [StringLength(500, ErrorMessage = "标签长度不能超过500个字符")]
    public string? Tags { get; set; }

    /// <summary>
    /// 体系备注
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// 体系状态
    /// </summary>
    [StringLength(50, ErrorMessage = "状态长度不能超过50个字符")]
    public string Status { get; set; } = "Active";

    /// <summary>
    /// 排序索引
    /// </summary>
    public int OrderIndex { get; set; } = 0;
}

/// <summary>
/// 更新修炼体系DTO
/// </summary>
public class UpdateCultivationSystemDto : CreateCultivationSystemDto
{
    /// <summary>
    /// 体系ID
    /// </summary>
    public Guid Id { get; set; }
}

/// <summary>
/// 创建修炼等级DTO
/// </summary>
public class CreateCultivationLevelDto
{
    /// <summary>
    /// 等级名称
    /// </summary>
    [Required(ErrorMessage = "等级名称不能为空")]
    [StringLength(100, ErrorMessage = "等级名称长度不能超过100个字符")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 等级序号
    /// </summary>
    public int OrderIndex { get; set; }

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
    [StringLength(500, ErrorMessage = "标签长度不能超过500个字符")]
    public string? Tags { get; set; }

    /// <summary>
    /// 等级备注
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// 更新修炼等级DTO
/// </summary>
public class UpdateCultivationLevelDto : CreateCultivationLevelDto
{
    /// <summary>
    /// 等级ID
    /// </summary>
    public Guid Id { get; set; }
}
