namespace NovelManagement.Core.Enums;

/// <summary>
/// 项目状态枚举
/// </summary>
public enum ProjectStatus
{
    /// <summary>
    /// 规划中
    /// </summary>
    Planning = 0,

    /// <summary>
    /// 进行中
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// 暂停
    /// </summary>
    Paused = 2,

    /// <summary>
    /// 已完成
    /// </summary>
    Completed = 3,

    /// <summary>
    /// 已取消
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// 已发布
    /// </summary>
    Published = 5,

    /// <summary>
    /// 归档
    /// </summary>
    Archived = 6
}
