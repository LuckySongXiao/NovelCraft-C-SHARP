using System;
using Microsoft.Extensions.Logging;

namespace NovelManagement.WPF.Services
{
    /// <summary>
    /// 项目上下文服务，管理当前选中的项目
    /// </summary>
    public class ProjectContextService
    {
        private readonly ILogger<ProjectContextService> _logger;
        private Guid? _currentProjectId;
        private string? _currentProjectName;

        /// <summary>
        /// 当前项目ID变更事件
        /// </summary>
        public event EventHandler<ProjectChangedEventArgs>? ProjectChanged;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ProjectContextService(ILogger<ProjectContextService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 当前项目ID
        /// </summary>
        public Guid? CurrentProjectId => _currentProjectId;

        /// <summary>
        /// 当前项目名称
        /// </summary>
        public string? CurrentProjectName => _currentProjectName;

        /// <summary>
        /// 是否有当前项目
        /// </summary>
        public bool HasCurrentProject => _currentProjectId.HasValue;

        /// <summary>
        /// 设置当前项目
        /// </summary>
        /// <param name="projectId">项目ID</param>
        /// <param name="projectName">项目名称</param>
        public void SetCurrentProject(Guid projectId, string projectName)
        {
            var oldProjectId = _currentProjectId;
            var oldProjectName = _currentProjectName;

            _currentProjectId = projectId;
            _currentProjectName = projectName;

            _logger.LogInformation("项目上下文已切换: {OldProject} -> {NewProject}", 
                oldProjectName ?? "无", projectName);

            // 触发项目变更事件
            ProjectChanged?.Invoke(this, new ProjectChangedEventArgs(
                oldProjectId, oldProjectName, projectId, projectName));
        }

        /// <summary>
        /// 清除当前项目
        /// </summary>
        public void ClearCurrentProject()
        {
            var oldProjectId = _currentProjectId;
            var oldProjectName = _currentProjectName;

            _currentProjectId = null;
            _currentProjectName = null;

            _logger.LogInformation("项目上下文已清除: {OldProject}", oldProjectName ?? "无");

            // 触发项目变更事件
            ProjectChanged?.Invoke(this, new ProjectChangedEventArgs(
                oldProjectId, oldProjectName, null, null));
        }

        /// <summary>
        /// 获取当前项目ID，如果没有则抛出异常
        /// </summary>
        /// <returns>当前项目ID</returns>
        /// <exception cref="InvalidOperationException">当没有当前项目时抛出</exception>
        public Guid GetCurrentProjectIdOrThrow()
        {
            if (!_currentProjectId.HasValue)
            {
                throw new InvalidOperationException("当前没有选中的项目");
            }
            return _currentProjectId.Value;
        }

        /// <summary>
        /// 获取当前项目ID，如果没有则返回默认值
        /// </summary>
        /// <param name="defaultValue">默认值</param>
        /// <returns>当前项目ID或默认值</returns>
        public Guid GetCurrentProjectIdOrDefault(Guid defaultValue = default)
        {
            return _currentProjectId ?? defaultValue;
        }
    }

    /// <summary>
    /// 项目变更事件参数
    /// </summary>
    public class ProjectChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public ProjectChangedEventArgs(Guid? oldProjectId, string? oldProjectName, 
            Guid? newProjectId, string? newProjectName)
        {
            OldProjectId = oldProjectId;
            OldProjectName = oldProjectName;
            NewProjectId = newProjectId;
            NewProjectName = newProjectName;
        }

        /// <summary>
        /// 旧项目ID
        /// </summary>
        public Guid? OldProjectId { get; }

        /// <summary>
        /// 旧项目名称
        /// </summary>
        public string? OldProjectName { get; }

        /// <summary>
        /// 新项目ID
        /// </summary>
        public Guid? NewProjectId { get; }

        /// <summary>
        /// 新项目名称
        /// </summary>
        public string? NewProjectName { get; }
    }
}
