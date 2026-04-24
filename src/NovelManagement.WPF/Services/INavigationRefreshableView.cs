using System;
using System.Threading.Tasks;

namespace NovelManagement.WPF.Services
{
    /// <summary>
    /// 支持在项目上下文变更后执行刷新逻辑的视图接口。
    /// </summary>
    public interface INavigationRefreshableView
    {
        /// <summary>
        /// 当前项目切换后刷新视图。
        /// </summary>
        Task RefreshOnProjectChangedAsync(Guid? projectId, string? projectName);
    }
}
