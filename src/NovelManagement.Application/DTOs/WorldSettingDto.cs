using System.ComponentModel.DataAnnotations;

namespace NovelManagement.Application.DTOs;

/// <summary>
/// 世界设定DTO
/// </summary>
public class WorldSettingDto
{
    /// <summary>
    /// 设定ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 设定名称
    /// </summary>
    [Required(ErrorMessage = "设定名称不能为空")]
    [StringLength(200, ErrorMessage = "设定名称长度不能超过200个字符")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 设定类型
    /// </summary>
    [Required(ErrorMessage = "设定类型不能为空")]
    [StringLength(50, ErrorMessage = "设定类型长度不能超过50个字符")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 设定分类
    /// </summary>
    [StringLength(50, ErrorMessage = "设定分类长度不能超过50个字符")]
    public string? Category { get; set; }

    /// <summary>
    /// 设定描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 详细内容
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// 设定规则
    /// </summary>
    public string? Rules { get; set; }

    /// <summary>
    /// 设定历史
    /// </summary>
    public string? History { get; set; }

    /// <summary>
    /// 相关设定
    /// </summary>
    public string? RelatedSettings { get; set; }

    /// <summary>
    /// 设定重要性（1-10）
    /// </summary>
    [Range(1, 10, ErrorMessage = "设定重要性必须在1-10之间")]
    public int Importance { get; set; } = 1;

    /// <summary>
    /// 所属项目ID
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// 父级设定ID
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// 设定图片路径
    /// </summary>
    [StringLength(500, ErrorMessage = "图片路径长度不能超过500个字符")]
    public string? ImagePath { get; set; }

    /// <summary>
    /// 设定标签
    /// </summary>
    [StringLength(500, ErrorMessage = "标签长度不能超过500个字符")]
    public string? Tags { get; set; }

    /// <summary>
    /// 设定备注
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// 设定状态
    /// </summary>
    [StringLength(50, ErrorMessage = "状态长度不能超过50个字符")]
    public string Status { get; set; } = "Active";

    /// <summary>
    /// 排序索引
    /// </summary>
    public int OrderIndex { get; set; } = 0;

    /// <summary>
    /// 是否为公开设定
    /// </summary>
    public bool IsPublic { get; set; } = true;

    /// <summary>
    /// 设定版本
    /// </summary>
    [StringLength(20, ErrorMessage = "版本长度不能超过20个字符")]
    public string? Version { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// 父级设定名称
    /// </summary>
    public string? ParentName { get; set; }

    /// <summary>
    /// 子设定列表
    /// </summary>
    public List<WorldSettingDto> Children { get; set; } = new();
}

/// <summary>
/// 创建世界设定DTO
/// </summary>
public class CreateWorldSettingDto
{
    /// <summary>
    /// 设定名称
    /// </summary>
    [Required(ErrorMessage = "设定名称不能为空")]
    [StringLength(200, ErrorMessage = "设定名称长度不能超过200个字符")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 设定类型
    /// </summary>
    [Required(ErrorMessage = "设定类型不能为空")]
    [StringLength(50, ErrorMessage = "设定类型长度不能超过50个字符")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 设定分类
    /// </summary>
    [StringLength(50, ErrorMessage = "设定分类长度不能超过50个字符")]
    public string? Category { get; set; }

    /// <summary>
    /// 设定描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 详细内容
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// 设定规则
    /// </summary>
    public string? Rules { get; set; }

    /// <summary>
    /// 设定历史
    /// </summary>
    public string? History { get; set; }

    /// <summary>
    /// 相关设定
    /// </summary>
    public string? RelatedSettings { get; set; }

    /// <summary>
    /// 设定重要性（1-10）
    /// </summary>
    [Range(1, 10, ErrorMessage = "设定重要性必须在1-10之间")]
    public int Importance { get; set; } = 1;

    /// <summary>
    /// 所属项目ID
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// 父级设定ID
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// 设定图片路径
    /// </summary>
    [StringLength(500, ErrorMessage = "图片路径长度不能超过500个字符")]
    public string? ImagePath { get; set; }

    /// <summary>
    /// 设定标签
    /// </summary>
    [StringLength(500, ErrorMessage = "标签长度不能超过500个字符")]
    public string? Tags { get; set; }

    /// <summary>
    /// 设定备注
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// 设定状态
    /// </summary>
    [StringLength(50, ErrorMessage = "状态长度不能超过50个字符")]
    public string Status { get; set; } = "Active";

    /// <summary>
    /// 排序索引
    /// </summary>
    public int OrderIndex { get; set; } = 0;

    /// <summary>
    /// 是否为公开设定
    /// </summary>
    public bool IsPublic { get; set; } = true;

    /// <summary>
    /// 设定版本
    /// </summary>
    [StringLength(20, ErrorMessage = "版本长度不能超过20个字符")]
    public string? Version { get; set; }
}

/// <summary>
/// 更新世界设定DTO
/// </summary>
public class UpdateWorldSettingDto : CreateWorldSettingDto
{
    /// <summary>
    /// 设定ID
    /// </summary>
    public Guid Id { get; set; }
}
