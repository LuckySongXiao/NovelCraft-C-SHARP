namespace NovelManagement.Application.DTOs;

/// <summary>
/// 项目数据传输对象
/// </summary>
public class ProjectDto
{
    /// <summary>
    /// 项目ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 项目名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 项目描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 项目类型
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 项目状态
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 项目封面图片路径
    /// </summary>
    public string? CoverImagePath { get; set; }

    /// <summary>
    /// 项目标签
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// 项目优先级
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// 最后访问时间
    /// </summary>
    public DateTime? LastAccessedAt { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// 项目备注
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// 卷宗列表
    /// </summary>
    public List<VolumeDto> Volumes { get; set; } = new();

    /// <summary>
    /// 角色列表
    /// </summary>
    public List<CharacterDto> Characters { get; set; } = new();

    /// <summary>
    /// 势力列表
    /// </summary>
    public List<FactionDto> Factions { get; set; } = new();
}

/// <summary>
/// 创建项目请求
/// </summary>
public class CreateProjectRequest
{
    /// <summary>
    /// 项目名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 项目描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 项目类型
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 项目标签
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// 项目优先级
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// 项目备注
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// 更新项目请求
/// </summary>
public class UpdateProjectRequest
{
    /// <summary>
    /// 项目ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 项目名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 项目描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 项目类型
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 项目状态
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 项目标签
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// 项目优先级
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// 项目备注
    /// </summary>
    public string? Notes { get; set; }
}
