namespace NovelManagement.Application.DTOs;

/// <summary>
/// 卷宗数据传输对象
/// </summary>
public class VolumeDto
{
    /// <summary>
    /// 卷宗ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 卷宗标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 卷宗描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 卷宗序号
    /// </summary>
    public int OrderIndex { get; set; }

    /// <summary>
    /// 卷宗状态
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 所属项目ID
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// 卷宗类型
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// 卷宗标签
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// 预计字数
    /// </summary>
    public int? EstimatedWordCount { get; set; }

    /// <summary>
    /// 实际字数
    /// </summary>
    public int ActualWordCount { get; set; }

    /// <summary>
    /// 完成进度
    /// </summary>
    public decimal Progress { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// 卷宗备注
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// 章节列表
    /// </summary>
    public List<ChapterDto> Chapters { get; set; } = new();
}

/// <summary>
/// 创建卷宗请求
/// </summary>
public class CreateVolumeRequest
{
    /// <summary>
    /// 卷宗标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 卷宗描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 所属项目ID
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// 卷宗类型
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// 卷宗标签
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// 预计字数
    /// </summary>
    public int? EstimatedWordCount { get; set; }

    /// <summary>
    /// 卷宗备注
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// 更新卷宗请求
/// </summary>
public class UpdateVolumeRequest
{
    /// <summary>
    /// 卷宗ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 卷宗标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 卷宗描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 卷宗状态
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 卷宗类型
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// 卷宗标签
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// 预计字数
    /// </summary>
    public int? EstimatedWordCount { get; set; }

    /// <summary>
    /// 完成进度
    /// </summary>
    public decimal Progress { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// 卷宗备注
    /// </summary>
    public string? Notes { get; set; }
}
