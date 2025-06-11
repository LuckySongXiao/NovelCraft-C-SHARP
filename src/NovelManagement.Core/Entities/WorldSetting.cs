using System.ComponentModel.DataAnnotations;

namespace NovelManagement.Core.Entities;

/// <summary>
/// 世界设定实体
/// </summary>
public class WorldSetting : BaseEntity
{
    /// <summary>
    /// 设定名称
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 设定类型
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 设定分类
    /// </summary>
    [MaxLength(50)]
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
    public int Importance { get; set; } = 1;

    /// <summary>
    /// 所属项目ID
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// 父级设定ID（用于层级结构）
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// 设定图片路径
    /// </summary>
    [MaxLength(500)]
    public string? ImagePath { get; set; }

    /// <summary>
    /// 设定标签
    /// </summary>
    [MaxLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// 设定备注
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// 设定状态
    /// </summary>
    [MaxLength(50)]
    public string Status { get; set; } = "Active";

    /// <summary>
    /// 排序索引
    /// </summary>
    public int Order { get; set; } = 0;

    /// <summary>
    /// 是否为公开设定
    /// </summary>
    public bool IsPublic { get; set; } = true;

    /// <summary>
    /// 设定版本
    /// </summary>
    [MaxLength(20)]
    public new string? Version { get; set; }

    // 导航属性
    /// <summary>
    /// 所属项目
    /// </summary>
    public virtual Project Project { get; set; } = null!;

    /// <summary>
    /// 父级设定
    /// </summary>
    public virtual WorldSetting? Parent { get; set; }

    /// <summary>
    /// 子级设定
    /// </summary>
    public virtual ICollection<WorldSetting> Children { get; set; } = new List<WorldSetting>();
}
