namespace NovelManagement.Application.DTOs;

/// <summary>
/// 角色引用信息
/// </summary>
public class CharacterReferenceInfo
{
    /// <summary>
    /// 角色ID
    /// </summary>
    public Guid CharacterId { get; set; }

    /// <summary>
    /// 是否被引用
    /// </summary>
    public bool IsReferenced { get; set; }

    /// <summary>
    /// 引用详情列表
    /// </summary>
    public List<string> References { get; set; } = new List<string>();

    /// <summary>
    /// 引用总数
    /// </summary>
    public int ReferenceCount => References.Count;
}

/// <summary>
/// 角色删除结果
/// </summary>
public class CharacterDeleteResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 结果消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 引用信息（删除失败时）
    /// </summary>
    public CharacterReferenceInfo? ReferenceInfo { get; set; }
}
