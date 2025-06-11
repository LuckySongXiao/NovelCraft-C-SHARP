using System.ComponentModel.DataAnnotations;

namespace NovelManagement.Core.Entities;

/// <summary>
/// 章节实体
/// </summary>
public class Chapter : BaseEntity
{
    /// <summary>
    /// 章节标题
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 章节内容
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// 章节摘要
    /// </summary>
    [MaxLength(1000)]
    public string? Summary { get; set; }

    /// <summary>
    /// 章节序号
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 章节状态
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Draft";

    /// <summary>
    /// 所属卷宗ID
    /// </summary>
    public Guid VolumeId { get; set; }

    /// <summary>
    /// 章节类型
    /// </summary>
    [MaxLength(50)]
    public string? Type { get; set; }

    /// <summary>
    /// 章节标签
    /// </summary>
    [MaxLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// 章节备注
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// 字数统计
    /// </summary>
    public int WordCount { get; set; } = 0;

    /// <summary>
    /// 阅读时长（分钟）
    /// </summary>
    public int? ReadingTime { get; set; }

    /// <summary>
    /// 章节难度等级
    /// </summary>
    public int? DifficultyLevel { get; set; }

    /// <summary>
    /// 章节重要性
    /// </summary>
    public int? Importance { get; set; }

    /// <summary>
    /// 发布时间
    /// </summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// 最后编辑时间
    /// </summary>
    public DateTime? LastEditedAt { get; set; }

    /// <summary>
    /// 版本号
    /// </summary>
    public int VersionNumber { get; set; } = 1;

    // 导航属性
    /// <summary>
    /// 所属卷宗
    /// </summary>
    public virtual Volume Volume { get; set; } = null!;

    /// <summary>
    /// 涉及的剧情
    /// </summary>
    public virtual ICollection<Plot> InvolvedPlots { get; set; } = new List<Plot>();

    /// <summary>
    /// 作为起始章节的剧情
    /// </summary>
    public virtual ICollection<Plot> PlotsAsStart { get; set; } = new List<Plot>();

    /// <summary>
    /// 作为结束章节的剧情
    /// </summary>
    public virtual ICollection<Plot> PlotsAsEnd { get; set; } = new List<Plot>();
}
