namespace NovelManagement.Application.DTOs;

/// <summary>
/// 章节数据传输对象
/// </summary>
public class ChapterDto
{
    /// <summary>
    /// 章节ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 章节标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 章节内容
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// 章节摘要
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// 章节序号
    /// </summary>
    public int OrderIndex { get; set; }

    /// <summary>
    /// 章节状态
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 所属卷宗ID
    /// </summary>
    public Guid VolumeId { get; set; }

    /// <summary>
    /// 章节类型
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// 章节标签
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// 字数统计
    /// </summary>
    public int WordCount { get; set; }

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
    public int VersionNumber { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// 章节备注
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// 创建章节请求
/// </summary>
public class CreateChapterRequest
{
    /// <summary>
    /// 章节标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 章节内容
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// 章节摘要
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// 所属卷宗ID
    /// </summary>
    public Guid VolumeId { get; set; }

    /// <summary>
    /// 章节类型
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// 章节标签
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// 章节难度等级
    /// </summary>
    public int? DifficultyLevel { get; set; }

    /// <summary>
    /// 章节重要性
    /// </summary>
    public int? Importance { get; set; }

    /// <summary>
    /// 章节备注
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// 更新章节请求
/// </summary>
public class UpdateChapterRequest
{
    /// <summary>
    /// 章节ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 章节标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 章节内容
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// 章节摘要
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// 章节状态
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 章节类型
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// 章节标签
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// 章节难度等级
    /// </summary>
    public int? DifficultyLevel { get; set; }

    /// <summary>
    /// 章节重要性
    /// </summary>
    public int? Importance { get; set; }

    /// <summary>
    /// 章节备注
    /// </summary>
    public string? Notes { get; set; }
}
