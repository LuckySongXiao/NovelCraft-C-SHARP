using System;

namespace NovelManagement.WPF.Services
{
    /// <summary>
    /// 支持在导航进入时接收上下文参数的视图接口。
    /// </summary>
    public interface INavigationAwareView
    {
        /// <summary>
        /// 视图在导航进入时接收上下文。
        /// </summary>
        void OnNavigatedTo(NavigationContext context);
    }

    /// <summary>
    /// 轻量导航上下文。
    /// </summary>
    public sealed class NavigationContext
    {
        public Guid? ProjectId { get; init; }

        public string? ProjectName { get; init; }

        public string? Source { get; init; }

        public object? Payload { get; init; }
    }

    /// <summary>
    /// 卷章页面导航参数。
    /// </summary>
    public sealed class VolumeNavigationPayload
    {
        public string? Action { get; init; }

        public Guid? VolumeId { get; init; }

        public Guid? ChapterId { get; init; }
    }

    /// <summary>
    /// 世界设定页面导航参数。
    /// </summary>
    public sealed class WorldSettingNavigationPayload
    {
        public string? Action { get; init; }

        public Guid? SettingId { get; init; }
    }

    /// <summary>
    /// 通用实体定位导航参数：目标视图加载完数据后按 <see cref="TargetId"/> 选中并高亮实体。
    /// </summary>
    public sealed class EntityHighlightNavigationPayload
    {
        /// <summary>
        /// 目标实体ID（时间线事件为事件ID，其余为实体主键）。
        /// </summary>
        public Guid? TargetId { get; init; }

        /// <summary>
        /// 目标实体名称（仅用于展示与兜底按名称匹配）。
        /// </summary>
        public string? TargetName { get; init; }

        /// <summary>
        /// 目标实体类型描述（如"角色"、"时间线事件"）。
        /// </summary>
        public string? TargetType { get; init; }
    }

    /// <summary>
    /// 导入导出页面导航参数。
    /// </summary>
    public sealed class ImportExportNavigationPayload
    {
        public string? Action { get; init; }

        public Guid? CharacterId { get; init; }

        public Guid? VolumeId { get; init; }

        public Guid? ChapterId { get; init; }

        public Guid? SettingId { get; init; }

        public string? SettingName { get; init; }
    }
}
