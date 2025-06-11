using System.ComponentModel.DataAnnotations;

namespace NovelManagement.Core.Entities;

/// <summary>
/// 卷宗实体
/// </summary>
public class Volume : BaseEntity
{
    /// <summary>
    /// 卷宗标题
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 卷宗描述
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// 卷宗序号
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 卷宗状态
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Planning";

    /// <summary>
    /// 所属项目ID
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// 卷宗类型
    /// </summary>
    [MaxLength(50)]
    public string? Type { get; set; }

    /// <summary>
    /// 卷宗标签
    /// </summary>
    [MaxLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// 卷宗备注
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// 预计字数
    /// </summary>
    public int? EstimatedWordCount { get; set; }

    /// <summary>
    /// 实际字数
    /// </summary>
    public int ActualWordCount { get; set; } = 0;

    /// <summary>
    /// 字数（与ActualWordCount同义，用于兼容性）
    /// </summary>
    public int WordCount
    {
        get => ActualWordCount;
        set => ActualWordCount = value;
    }

    /// <summary>
    /// 完成进度（百分比）
    /// </summary>
    public decimal Progress { get; set; } = 0;

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime? EndDate { get; set; }

    // 导航属性
    /// <summary>
    /// 所属项目
    /// </summary>
    public virtual Project Project { get; set; } = null!;

    /// <summary>
    /// 卷宗包含的章节
    /// </summary>
    public virtual ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();
}
