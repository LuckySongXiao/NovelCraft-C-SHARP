using System;
using System.Windows;

namespace NovelManagement.WPF.Services
{
    /// <summary>
    /// 当前项目访问守卫，统一处理项目上下文校验与提示。
    /// </summary>
    public class CurrentProjectGuard
    {
        private readonly ProjectContextService _projectContextService;

        public CurrentProjectGuard(ProjectContextService projectContextService)
        {
            _projectContextService = projectContextService;
        }

        /// <summary>
        /// 尝试获取当前项目ID。
        /// </summary>
        public bool TryGetCurrentProjectId(out Guid projectId)
        {
            if (_projectContextService.CurrentProjectId.HasValue)
            {
                projectId = _projectContextService.CurrentProjectId.Value;
                return true;
            }

            projectId = Guid.Empty;
            return false;
        }

        /// <summary>
        /// 确保当前已选择项目；若未选择，则显示统一提示。
        /// </summary>
        public bool TryGetCurrentProjectId(
            Window? owner,
            string featureName,
            out Guid projectId,
            Action? onMissingProjectConfirmed = null)
        {
            if (TryGetCurrentProjectId(out projectId))
            {
                return true;
            }

            if (onMissingProjectConfirmed == null)
            {
                var infoMessage = $"{featureName}需要先选择项目。";
                if (owner != null)
                {
                    MessageBox.Show(owner, infoMessage, "未选择项目", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show(infoMessage, "未选择项目", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                var message = $"{featureName}需要先选择项目。\n\n是否前往项目管理页面？";
                var result = owner != null
                    ? MessageBox.Show(owner, message, "未选择项目", MessageBoxButton.YesNo, MessageBoxImage.Warning)
                    : MessageBox.Show(message, "未选择项目", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    onMissingProjectConfirmed.Invoke();
                }
            }

            return false;
        }
    }
}
