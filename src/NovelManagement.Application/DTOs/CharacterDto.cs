namespace NovelManagement.Application.DTOs;

/// <summary>
/// 角色数据传输对象
/// </summary>
public class CharacterDto
{
    /// <summary>
    /// 角色ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 角色姓名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 角色类型
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 角色性别
    /// </summary>
    public string? Gender { get; set; }

    /// <summary>
    /// 角色年龄
    /// </summary>
    public int? Age { get; set; }

    /// <summary>
    /// 角色修为
    /// </summary>
    public string? CultivationLevel { get; set; }

    /// <summary>
    /// 所属势力ID
    /// </summary>
    public Guid? FactionId { get; set; }

    /// <summary>
    /// 所属势力名称
    /// </summary>
    public string? FactionName { get; set; }

    /// <summary>
    /// 角色重要性
    /// </summary>
    public int Importance { get; set; }

    /// <summary>
    /// 外貌描述
    /// </summary>
    public string? Appearance { get; set; }

    /// <summary>
    /// 性格特点
    /// </summary>
    public string? Personality { get; set; }

    /// <summary>
    /// 背景故事
    /// </summary>
    public string? Background { get; set; }

    /// <summary>
    /// 能力技能
    /// </summary>
    public string? Abilities { get; set; }

    /// <summary>
    /// 所属项目ID
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// 角色头像路径
    /// </summary>
    public string? AvatarPath { get; set; }

    /// <summary>
    /// 角色标签
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// 角色状态
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// 角色备注
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// 创建角色请求
/// </summary>
public class CreateCharacterRequest
{
    /// <summary>
    /// 角色姓名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 角色类型
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 角色性别
    /// </summary>
    public string? Gender { get; set; }

    /// <summary>
    /// 角色年龄
    /// </summary>
    public int? Age { get; set; }

    /// <summary>
    /// 角色修为
    /// </summary>
    public string? CultivationLevel { get; set; }

    /// <summary>
    /// 所属势力ID
    /// </summary>
    public Guid? FactionId { get; set; }

    /// <summary>
    /// 角色重要性
    /// </summary>
    public int Importance { get; set; } = 1;

    /// <summary>
    /// 外貌描述
    /// </summary>
    public string? Appearance { get; set; }

    /// <summary>
    /// 性格特点
    /// </summary>
    public string? Personality { get; set; }

    /// <summary>
    /// 背景故事
    /// </summary>
    public string? Background { get; set; }

    /// <summary>
    /// 能力技能
    /// </summary>
    public string? Abilities { get; set; }

    /// <summary>
    /// 所属项目ID
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// 角色标签
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// 角色备注
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// 更新角色请求
/// </summary>
public class UpdateCharacterRequest
{
    /// <summary>
    /// 角色ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 角色姓名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 角色类型
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 角色性别
    /// </summary>
    public string? Gender { get; set; }

    /// <summary>
    /// 角色年龄
    /// </summary>
    public int? Age { get; set; }

    /// <summary>
    /// 角色修为
    /// </summary>
    public string? CultivationLevel { get; set; }

    /// <summary>
    /// 所属势力ID
    /// </summary>
    public Guid? FactionId { get; set; }

    /// <summary>
    /// 角色重要性
    /// </summary>
    public int Importance { get; set; }

    /// <summary>
    /// 外貌描述
    /// </summary>
    public string? Appearance { get; set; }

    /// <summary>
    /// 性格特点
    /// </summary>
    public string? Personality { get; set; }

    /// <summary>
    /// 背景故事
    /// </summary>
    public string? Background { get; set; }

    /// <summary>
    /// 能力技能
    /// </summary>
    public string? Abilities { get; set; }

    /// <summary>
    /// 角色标签
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// 角色状态
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 角色备注
    /// </summary>
    public string? Notes { get; set; }
}
