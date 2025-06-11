namespace NovelManagement.Application.DTOs;

/// <summary>
/// 势力数据传输对象
/// </summary>
public class FactionDto
{
    /// <summary>
    /// 势力ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 势力名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 势力类型
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 势力等级
    /// </summary>
    public string? Level { get; set; }

    /// <summary>
    /// 势力领袖ID
    /// </summary>
    public Guid? LeaderId { get; set; }

    /// <summary>
    /// 势力领袖名称
    /// </summary>
    public string? LeaderName { get; set; }

    /// <summary>
    /// 成员数量
    /// </summary>
    public int? MemberCount { get; set; }

    /// <summary>
    /// 势力实力评级
    /// </summary>
    public int? PowerRating { get; set; }

    /// <summary>
    /// 势力影响力评级
    /// </summary>
    public int? InfluenceRating { get; set; }

    /// <summary>
    /// 势力实力等级（字符串形式）
    /// </summary>
    public string? PowerLevel { get; set; }

    /// <summary>
    /// 势力描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 势力历史
    /// </summary>
    public string? History { get; set; }

    /// <summary>
    /// 特色能力
    /// </summary>
    public string? SpecialAbilities { get; set; }

    /// <summary>
    /// 势力资源
    /// </summary>
    public string? Resources { get; set; }

    /// <summary>
    /// 所属项目ID
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// 势力徽章路径
    /// </summary>
    public string? EmblemPath { get; set; }

    /// <summary>
    /// 势力标签
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// 势力状态
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 势力总部位置
    /// </summary>
    public string? Headquarters { get; set; }

    /// <summary>
    /// 势力控制区域
    /// </summary>
    public string? Territory { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// 势力备注
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// 成员列表
    /// </summary>
    public List<CharacterDto> Members { get; set; } = new();
}

/// <summary>
/// 创建势力请求
/// </summary>
public class CreateFactionRequest
{
    /// <summary>
    /// 势力名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 势力类型
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 势力等级
    /// </summary>
    public string? Level { get; set; }

    /// <summary>
    /// 势力领袖ID
    /// </summary>
    public Guid? LeaderId { get; set; }

    /// <summary>
    /// 成员数量
    /// </summary>
    public int? MemberCount { get; set; }

    /// <summary>
    /// 势力实力评级
    /// </summary>
    public int? PowerRating { get; set; }

    /// <summary>
    /// 势力影响力评级
    /// </summary>
    public int? InfluenceRating { get; set; }

    /// <summary>
    /// 势力描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 势力历史
    /// </summary>
    public string? History { get; set; }

    /// <summary>
    /// 特色能力
    /// </summary>
    public string? SpecialAbilities { get; set; }

    /// <summary>
    /// 势力资源
    /// </summary>
    public string? Resources { get; set; }

    /// <summary>
    /// 所属项目ID
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// 势力标签
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// 势力总部位置
    /// </summary>
    public string? Headquarters { get; set; }

    /// <summary>
    /// 势力控制区域
    /// </summary>
    public string? Territory { get; set; }

    /// <summary>
    /// 势力备注
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// 更新势力请求
/// </summary>
public class UpdateFactionRequest
{
    /// <summary>
    /// 势力ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 势力名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 势力类型
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 势力等级
    /// </summary>
    public string? Level { get; set; }

    /// <summary>
    /// 势力领袖ID
    /// </summary>
    public Guid? LeaderId { get; set; }

    /// <summary>
    /// 成员数量
    /// </summary>
    public int? MemberCount { get; set; }

    /// <summary>
    /// 势力实力评级
    /// </summary>
    public int? PowerRating { get; set; }

    /// <summary>
    /// 势力影响力评级
    /// </summary>
    public int? InfluenceRating { get; set; }

    /// <summary>
    /// 势力描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 势力历史
    /// </summary>
    public string? History { get; set; }

    /// <summary>
    /// 特色能力
    /// </summary>
    public string? SpecialAbilities { get; set; }

    /// <summary>
    /// 势力资源
    /// </summary>
    public string? Resources { get; set; }

    /// <summary>
    /// 势力标签
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// 势力状态
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 势力总部位置
    /// </summary>
    public string? Headquarters { get; set; }

    /// <summary>
    /// 势力控制区域
    /// </summary>
    public string? Territory { get; set; }

    /// <summary>
    /// 势力备注
    /// </summary>
    public string? Notes { get; set; }
}
