using System.ComponentModel.DataAnnotations;
using NovelManagement.Core.Entities;

namespace NovelManagement.Core.Entities;

/// <summary>
/// 剧情实体
/// </summary>
public class Plot : BaseEntity
{
    /// <summary>
    /// 剧情标题
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 剧情类型
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty; // 主线、支线、暗线、伏笔

    /// <summary>
    /// 剧情状态
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "规划中"; // 规划中、进行中、已完成、暂停

    /// <summary>
    /// 优先级
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Priority { get; set; } = "中"; // 高、中、低

    /// <summary>
    /// 进度（百分比）
    /// </summary>
    public decimal Progress { get; set; } = 0;

    /// <summary>
    /// 起始章节ID
    /// </summary>
    public Guid? StartChapterId { get; set; }

    /// <summary>
    /// 结束章节ID
    /// </summary>
    public Guid? EndChapterId { get; set; }

    /// <summary>
    /// 剧情描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 剧情大纲
    /// </summary>
    public string? Outline { get; set; }

    /// <summary>
    /// 冲突要素
    /// </summary>
    public string? ConflictElements { get; set; }

    /// <summary>
    /// 主题元素
    /// </summary>
    public string? ThemeElements { get; set; }

    /// <summary>
    /// 所属项目ID
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// 重要性等级（1-10）
    /// </summary>
    public int Importance { get; set; } = 5;

    /// <summary>
    /// 剧情标签
    /// </summary>
    [MaxLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// 备注
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

    // 导航属性
    /// <summary>
    /// 所属项目
    /// </summary>
    public virtual Project Project { get; set; } = null!;

    /// <summary>
    /// 起始章节
    /// </summary>
    public virtual Chapter? StartChapter { get; set; }

    /// <summary>
    /// 结束章节
    /// </summary>
    public virtual Chapter? EndChapter { get; set; }

    /// <summary>
    /// 相关人物
    /// </summary>
    public virtual ICollection<Character> RelatedCharacters { get; set; } = new List<Character>();

    /// <summary>
    /// 涉及章节
    /// </summary>
    public virtual ICollection<Chapter> InvolvedChapters { get; set; } = new List<Chapter>();
}
