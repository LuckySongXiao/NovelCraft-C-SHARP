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
