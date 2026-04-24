using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace NovelManagement.WPF.Services
{
    /// <summary>
    /// 最小可用的主内容区导航服务。
    /// </summary>
    public class NavigationService
    {
        private readonly Stack<NavigationHistoryEntry> _history = new();
        private Func<NavigationTarget, NavigationViewRequest>? _viewFactory;
        private Action<UserControl, string>? _renderAction;
        private UserControl? _currentView;

        /// <summary>
        /// 当前导航目标。
        /// </summary>
        public NavigationTarget? CurrentTarget { get; private set; }

        /// <summary>
        /// 当前导航上下文。
        /// </summary>
        public NavigationContext? CurrentContext { get; private set; }

        /// <summary>
        /// 当前是否可返回。
        /// </summary>
        public bool CanGoBack => _history.Count > 0;

        /// <summary>
        /// 导航状态变化事件。
        /// </summary>
        public event EventHandler? NavigationStateChanged;

        /// <summary>
        /// 配置导航宿主与视图工厂。
        /// </summary>
        public void Configure(
            Func<NavigationTarget, NavigationViewRequest> viewFactory,
            Action<UserControl, string> renderAction)
        {
            _viewFactory = viewFactory;
            _renderAction = renderAction;
        }

        /// <summary>
        /// 导航到指定目标。
        /// </summary>
        public void NavigateTo(NavigationTarget target, NavigationContext? context = null)
        {
            if (CurrentTarget.HasValue && !IsEquivalent(CurrentTarget.Value, CurrentContext, target, context))
            {
                _history.Push(new NavigationHistoryEntry(CurrentTarget.Value, CurrentContext));
            }

            NavigateCore(target, context);
        }

        /// <summary>
        /// 返回上一页。
        /// </summary>
        public bool GoBack()
        {
            if (!CanGoBack)
            {
                return false;
            }

            var previousEntry = _history.Pop();
            NavigateCore(previousEntry.Target, previousEntry.Context);
            return true;
        }

        private void NavigateCore(NavigationTarget target, NavigationContext? context)
        {
            if (_viewFactory == null || _renderAction == null)
            {
                throw new InvalidOperationException("导航服务尚未完成配置。");
            }

            var request = _viewFactory(target);
            _currentView = request.View;
            CurrentTarget = target;
            CurrentContext = context;
            _renderAction(request.View, request.Title);

            if (_currentView is INavigationAwareView awareView)
            {
                awareView.OnNavigatedTo(context ?? new NavigationContext());
            }

            NavigationStateChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 在项目变更后刷新当前视图。
        /// </summary>
        public async Task RefreshCurrentViewAsync(Guid? projectId, string? projectName)
        {
            if (_currentView is INavigationRefreshableView refreshableView)
            {
                await refreshableView.RefreshOnProjectChangedAsync(projectId, projectName);
            }
        }

        private static bool IsEquivalent(
            NavigationTarget currentTarget,
            NavigationContext? currentContext,
            NavigationTarget nextTarget,
            NavigationContext? nextContext)
        {
            if (currentTarget != nextTarget)
            {
                return false;
            }

            if ((currentContext == null) != (nextContext == null))
            {
                return false;
            }

            if (currentContext == null && nextContext == null)
            {
                return true;
            }

            return currentContext!.ProjectId == nextContext!.ProjectId
                && string.Equals(currentContext.ProjectName, nextContext.ProjectName, StringComparison.Ordinal)
                && string.Equals(currentContext.Source, nextContext.Source, StringComparison.Ordinal)
                && IsPayloadEquivalent(currentContext.Payload, nextContext.Payload);
        }

        private static bool IsPayloadEquivalent(object? left, object? right)
        {
            if (left == null || right == null)
            {
                return left == right;
            }

            if (left.GetType() != right.GetType())
            {
                return false;
            }

            return (left, right) switch
            {
                (VolumeNavigationPayload a, VolumeNavigationPayload b) =>
                    string.Equals(a.Action, b.Action, StringComparison.Ordinal)
                    && a.VolumeId == b.VolumeId
                    && a.ChapterId == b.ChapterId,
                (WorldSettingNavigationPayload a, WorldSettingNavigationPayload b) =>
                    string.Equals(a.Action, b.Action, StringComparison.Ordinal)
                    && a.SettingId == b.SettingId,
                (ImportExportNavigationPayload a, ImportExportNavigationPayload b) =>
                    string.Equals(a.Action, b.Action, StringComparison.Ordinal)
                    && a.VolumeId == b.VolumeId
                    && a.ChapterId == b.ChapterId
                    && a.SettingId == b.SettingId
                    && string.Equals(a.SettingName, b.SettingName, StringComparison.Ordinal),
                _ => Equals(left, right)
            };
        }
    }

    /// <summary>
    /// 导航目标。
    /// </summary>
    public enum NavigationTarget
    {
        ProjectManagement,
        ProjectOverview,
        VolumeManagement,
        CharacterManagement,
        RelationshipNetwork,
        FactionManagement,
        PlotManagement,
        AICollaboration,
        ImportExport,
        WorldSettingManagement,
        DialogGeneration
    }

    /// <summary>
    /// 导航视图请求。
    /// </summary>
    public sealed class NavigationViewRequest
    {
        public required UserControl View { get; init; }

        public required string Title { get; init; }
    }

    internal sealed record NavigationHistoryEntry(NavigationTarget Target, NavigationContext? Context);
}
