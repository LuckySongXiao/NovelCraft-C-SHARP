using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using NovelManagement.WPF.Localization;
using NovelManagement.WPF.Services;

namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// ProjectManagementView.xaml 的交互逻辑
    /// </summary>
    public partial class ProjectManagementView : UserControl, INotifyPropertyChanged
    {
        /// <summary>
        /// 项目数据模型
        /// </summary>
        public class ProjectViewModel
        {
            /// <summary>
            /// 项目显示编号。
            /// </summary>
            public int Id { get; set; }

            /// <summary>
            /// 项目真实标识。
            /// </summary>
            public Guid? ProjectGuid { get; set; }

            /// <summary>
            /// 项目名称。
            /// </summary>
            public string Name { get; set; } = string.Empty;

            /// <summary>
            /// 项目描述。
            /// </summary>
            public string Description { get; set; } = string.Empty;

            /// <summary>
            /// 项目类型。
            /// </summary>
            public string Type { get; set; } = string.Empty;

            /// <summary>
            /// 当前项目状态。
            /// </summary>
            public string Status { get; set; } = string.Empty;

            /// <summary>
            /// 最后更新时间的显示文本。
            /// </summary>
            public string LastUpdated { get; set; } = string.Empty;

            /// <summary>
            /// 是否已移入回收站。
            /// </summary>
            public bool IsDeleted { get; set; } = false;

            /// <summary>
            /// 删除时间。
            /// </summary>
            public DateTime? DeletedAt { get; set; }

            /// <summary>
            /// 删除执行人。
            /// </summary>
            public string? DeletedBy { get; set; }

            /// <summary>
            /// 项目本地路径。
            /// </summary>
            public string ProjectPath { get; set; } = string.Empty;

            /// <summary>
            /// 指示项目路径是否实际存在。
            /// </summary>
            public bool IsExisting => !string.IsNullOrEmpty(ProjectPath) && Directory.Exists(ProjectPath);
        }

        private List<ProjectViewModel> _allProjects = new();
        private List<ProjectViewModel> _filteredProjects = new();
        private List<ProjectViewModel> _deletedProjects = new();
        private bool _showRecycleBin = false;
        private readonly ProjectCatalogService? _projectCatalogService;
        private readonly ProjectContextService? _projectContextService;

        /// <summary>
        /// 是否显示回收站（用于数据绑定）
        /// </summary>
        public bool ShowRecycleBin
        {
            get => _showRecycleBin;
            set
            {
                _showRecycleBin = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 属性变化事件
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 触发属性变化事件
        /// </summary>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <summary>
        /// 初始化项目管理视图。
        /// </summary>
        public ProjectManagementView()
        {
            InitializeComponent();
            DataContext = this;
            _projectCatalogService = App.ServiceProvider?.GetService<ProjectCatalogService>();
            _projectContextService = App.ServiceProvider?.GetService<ProjectContextService>();
            Loaded += async (_, _) => await ReloadProjectsAsync();
        }

        #region 数据加载

        /// <summary>
        /// 加载项目数据
        /// </summary>
        private async Task ReloadProjectsAsync()
        {
            try
            {
                _allProjects = await LoadExistingProjectsAsync();
                _deletedProjects = await LoadDeletedProjectsAsync();
                FilterProjects();
            }
            catch (Exception ex)
            {
                MessageBox.Show(LocalizationManager.TF("PM.LoadFailed", "加载项目失败：{0}", ex.Message), LocalizationManager.T("Msg.Error", "错误"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 加载实际存在的项目
        /// </summary>
        private async Task<List<ProjectViewModel>> LoadExistingProjectsAsync()
        {
            if (_projectCatalogService == null)
            {
                return new List<ProjectViewModel>();
            }

            var items = await _projectCatalogService.GetActiveProjectsAsync();
            return items.Select((item, index) => MapToViewModel(item, index + 1)).ToList();
        }

        /// <summary>
        /// 加载已删除的项目（回收站）
        /// </summary>
        private async Task<List<ProjectViewModel>> LoadDeletedProjectsAsync()
        {
            if (_projectCatalogService == null)
            {
                return new List<ProjectViewModel>();
            }

            var items = await _projectCatalogService.GetDeletedProjectsAsync();
            return items.Select((item, index) => MapToViewModel(item, index + 1)).ToList();
        }

        /// <summary>
        /// 更新项目列表显示
        /// </summary>
        private void UpdateProjectList()
        {
            ProjectListControl.ItemsSource = _filteredProjects;

            // 更新界面状态
            UpdateUIState();
        }

        /// <summary>
        /// 过滤项目
        /// </summary>
        private void FilterProjects()
        {
            if (_showRecycleBin)
            {
                _filteredProjects = new List<ProjectViewModel>(_deletedProjects);
            }
            else
            {
                _filteredProjects = new List<ProjectViewModel>(_allProjects.Where(p => !p.IsDeleted));
            }

            // 应用搜索过滤
            ApplySearchFilter();

            UpdateProjectList();
        }

        /// <summary>
        /// 应用搜索过滤
        /// </summary>
        private void ApplySearchFilter()
        {
            if (SearchTextBox != null && !string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                var searchText = SearchTextBox.Text.ToLower();
                _filteredProjects = _filteredProjects.Where(p =>
                    p.Name.ToLower().Contains(searchText) ||
                    p.Description.ToLower().Contains(searchText) ||
                    p.Type.ToLower().Contains(searchText)).ToList();
            }
        }

        /// <summary>
        /// 更新界面状态
        /// </summary>
        private void UpdateUIState()
        {
            try
            {
                // 更新按钮状态
                if (ShowProjectsButton != null)
                    ShowProjectsButton.IsEnabled = _showRecycleBin;

                if (ShowRecycleBinButton != null)
                    ShowRecycleBinButton.IsEnabled = !_showRecycleBin;

                if (EmptyRecycleBinButton != null)
                    EmptyRecycleBinButton.Visibility = _showRecycleBin ? Visibility.Visible : Visibility.Collapsed;

                // 更新项目列表中的按钮显示
                UpdateProjectItemButtons();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateUIState异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新项目项中的按钮显示
        /// </summary>
        private void UpdateProjectItemButtons()
        {
            // 通过重新绑定数据来刷新按钮状态
            if (ProjectListControl != null)
            {
                var currentSource = ProjectListControl.ItemsSource;
                ProjectListControl.ItemsSource = null;
                ProjectListControl.ItemsSource = currentSource;

                // 手动更新每个项目项中的按钮状态
                UpdateItemButtonsVisibility();
            }
        }

        /// <summary>
        /// 更新项目项按钮的可见性
        /// </summary>
        private void UpdateItemButtonsVisibility()
        {
            // 这个方法将在ItemTemplate中通过数据绑定来处理
            // 实际的按钮状态更新会在数据模板中根据当前模式自动调整
        }

        #endregion

        #region 搜索功能

        /// <summary>
        /// 搜索文本框内容变化事件
        /// </summary>
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = SearchTextBox.Text?.Trim().ToLower() ?? string.Empty;

            if (string.IsNullOrEmpty(searchText))
            {
                _filteredProjects = new List<ProjectViewModel>(_showRecycleBin ? _deletedProjects : _allProjects);
            }
            else
            {
                var source = _showRecycleBin ? _deletedProjects : _allProjects;
                _filteredProjects = source.Where(p =>
                    p.Name.ToLower().Contains(searchText) ||
                    p.Description.ToLower().Contains(searchText) ||
                    p.Type.ToLower().Contains(searchText)
                ).ToList();
            }

            UpdateProjectList();
        }

        #endregion

        #region 项目操作事件

        /// <summary>
        /// 新建项目按钮点击事件
        /// </summary>
        private async void NewProject_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new NewProjectDialog();
            dialog.Owner = Window.GetWindow(this);

            if (dialog.ShowDialog() == true && dialog.IsConfirmed && dialog.ProjectData != null)
            {
                try
                {
                    if (_projectCatalogService == null)
                    {
                        throw new InvalidOperationException(LocalizationManager.T("PM.ServiceNotInitialized", "项目目录服务未初始化"));
                    }

                    var createdProject = await _projectCatalogService.CreateProjectAsync(dialog.ProjectData);
                    await ReloadProjectsAsync();

                    var createdViewModel = _allProjects.FirstOrDefault(p => p.ProjectGuid == createdProject.ProjectId);
                    if (createdViewModel != null)
                    {
                        OpenProjectInternal(createdViewModel);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(LocalizationManager.TF("PM.CreateFailed", "创建项目失败：{0}", ex.Message), LocalizationManager.T("Msg.Error", "错误"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// 导入项目按钮点击事件
        /// </summary>
        private async void ImportProject_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = LocalizationManager.T("Common.ImportProject", "导入项目"),
                    Filter = $"{LocalizationManager.T("PM.FileType.Project", "项目文件")}|*.npj;*.json|{LocalizationManager.T("PM.FileType.Json", "JSON文件")}|*.json|{LocalizationManager.T("PM.FileType.All", "所有文件")}|*.*",
                    DefaultExt = "npj"
                };

                if (dialog.ShowDialog() == true)
                {
                    if (_projectCatalogService == null)
                    {
                        throw new InvalidOperationException(LocalizationManager.T("PM.ServiceNotInitialized", "项目目录服务未初始化"));
                    }

                    var imported = await _projectCatalogService.ImportProjectAsync(dialog.FileName);
                    await ReloadProjectsAsync();

                    MessageBox.Show(LocalizationManager.TF("PM.ImportSuccess", "项目 '{0}' 导入成功！", imported.Name), LocalizationManager.T("PM.ImportComplete", "导入完成"),
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(LocalizationManager.TF("PM.ImportFailed", "导入项目失败：{0}", ex.Message), LocalizationManager.T("Msg.Error", "错误"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 导出项目按钮点击事件
        /// </summary>
        private async void ExportProject_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_filteredProjects.Count == 0)
                {
                    MessageBox.Show(LocalizationManager.T("PM.NothingToExport", "没有可导出的项目"), LocalizationManager.T("Msg.Tip", "提示"),
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = LocalizationManager.T("Side.Btn.ExportProject", "导出项目"),
                    Filter = $"{LocalizationManager.T("PM.FileType.Project", "项目文件")}|*.npj|{LocalizationManager.T("PM.FileType.Json", "JSON文件")}|*.json|{LocalizationManager.T("PM.FileType.All", "所有文件")}|*.*",
                    DefaultExt = "npj",
                    FileName = LocalizationManager.TF("PM.ExportFileName", "项目导出_{0}", DateTime.Now.ToString("yyyyMMdd_HHmmss"))
                };

                if (dialog.ShowDialog() == true)
                {
                    if (_projectCatalogService == null)
                    {
                        throw new InvalidOperationException(LocalizationManager.T("PM.ServiceNotInitialized", "项目目录服务未初始化"));
                    }

                    await _projectCatalogService.ExportProjectsAsync(_filteredProjects.Select(p => p.ProjectGuid ?? Guid.Empty).Where(id => id != Guid.Empty), dialog.FileName);
                    MessageBox.Show(LocalizationManager.TF("PM.ExportSuccess", "已导出 {0} 个项目到：\n{1}", _filteredProjects.Count, dialog.FileName),
                        LocalizationManager.T("PM.ExportComplete", "导出完成"), MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(LocalizationManager.TF("PM.ExportFailed", "导出项目失败：{0}", ex.Message), LocalizationManager.T("Msg.Error", "错误"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 打开项目按钮点击事件
        /// </summary>
        private void OpenProject_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ProjectViewModel project)
            {
                OpenProjectInternal(project);
            }
        }

        /// <summary>
        /// 编辑项目按钮点击事件
        /// </summary>
        private async void EditProject_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ProjectViewModel project)
            {
                try
                {
                    // 创建编辑项目对话框
                    var dialog = new EditProjectDialog(project);
                    dialog.Owner = Window.GetWindow(this);

                    if (dialog.ShowDialog() == true && dialog.IsConfirmed && dialog.ProjectData != null)
                    {
                        project.Name = dialog.ProjectData.Name;
                        project.Description = dialog.ProjectData.Description;
                        project.Type = dialog.ProjectData.Type;
                        project.LastUpdated = LocalizationManager.T("PM.JustNow", "刚刚");
                        if (_projectCatalogService == null || project.ProjectGuid == null)
                        {
                            throw new InvalidOperationException(LocalizationManager.T("PM.ServiceNotInitialized", "项目目录服务未初始化"));
                        }

                        await _projectCatalogService.UpdateProjectAsync(MapToCatalogItem(project));

                        await ReloadProjectsAsync();

                        MessageBox.Show(LocalizationManager.TF("PM.UpdateSuccess", "项目 '{0}' 更新成功！", project.Name), LocalizationManager.T("PM.EditComplete", "编辑完成"),
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(LocalizationManager.TF("PM.EditFailed", "编辑项目失败：{0}", ex.Message), LocalizationManager.T("Msg.Error", "错误"),
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// 删除项目按钮点击事件
        /// </summary>
        private void DeleteProject_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ProjectViewModel project)
            {
                if (_showRecycleBin)
                {
                    // 在回收站中，执行永久删除
                    PermanentlyDeleteProject(project);
                }
                else
                {
                    // 在项目列表中，移到回收站
                    MoveToRecycleBin(project);
                }
            }
        }

        /// <summary>
        /// 移动项目到回收站
        /// </summary>
        private async void MoveToRecycleBin(ProjectViewModel project)
        {
            var result = MessageBox.Show(
                LocalizationManager.TF("PM.MoveToRecycleBinConfirm", "确定要将项目 '{0}' 移到回收站吗？\n您可以稍后从回收站恢复此项目。", project.Name),
                LocalizationManager.T("PM.MoveToRecycleBin", "移到回收站"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    if (_projectCatalogService == null || project.ProjectGuid == null)
                    {
                        throw new InvalidOperationException(LocalizationManager.T("PM.ServiceNotInitialized", "项目目录服务未初始化"));
                    }

                    await _projectCatalogService.SoftDeleteAsync(project.ProjectGuid.Value);
                    await ReloadProjectsAsync();

                    MessageBox.Show(LocalizationManager.TF("PM.MovedToRecycleBin", "项目 '{0}' 已移到回收站", project.Name), LocalizationManager.T("Msg.Tip", "提示"), MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(LocalizationManager.TF("PM.MoveToRecycleBinFailed", "移动项目到回收站失败：{0}", ex.Message), LocalizationManager.T("Msg.Error", "错误"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// 永久删除项目
        /// </summary>
        private async void PermanentlyDeleteProject(ProjectViewModel project)
        {
            var result = MessageBox.Show(
                LocalizationManager.TF("PM.PermanentDeleteConfirm", "确定要永久删除项目 '{0}' 吗？\n此操作不可撤销！", project.Name),
                LocalizationManager.T("PM.PermanentDelete", "永久删除"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    if (_projectCatalogService == null || project.ProjectGuid == null)
                    {
                        throw new InvalidOperationException(LocalizationManager.T("PM.ServiceNotInitialized", "项目目录服务未初始化"));
                    }

                    await _projectCatalogService.PermanentlyDeleteAsync(project.ProjectGuid.Value);
                    await ReloadProjectsAsync();

                    MessageBox.Show(LocalizationManager.TF("PM.PermanentlyDeleted", "项目 '{0}' 已永久删除", project.Name), LocalizationManager.T("Msg.Tip", "提示"), MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(LocalizationManager.TF("PM.PermanentDeleteFailed", "永久删除项目失败：{0}", ex.Message), LocalizationManager.T("Msg.Error", "错误"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// 显示回收站按钮点击事件
        /// </summary>
        private void ShowRecycleBin_Click(object sender, RoutedEventArgs e)
        {
            ShowRecycleBin = true;
            FilterProjects();
        }

        /// <summary>
        /// 显示项目列表按钮点击事件
        /// </summary>
        private void ShowProjects_Click(object sender, RoutedEventArgs e)
        {
            ShowRecycleBin = false;
            FilterProjects();
        }

        /// <summary>
        /// 恢复项目按钮点击事件
        /// </summary>
        private async void RestoreProject_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ProjectViewModel project)
            {
                var result = MessageBox.Show(
                    LocalizationManager.TF("PM.RestoreConfirm", "确定要恢复项目 '{0}' 吗？", project.Name),
                    LocalizationManager.T("PM.RestoreProject", "恢复项目"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        if (_projectCatalogService == null || project.ProjectGuid == null)
                        {
                            throw new InvalidOperationException(LocalizationManager.T("PM.ServiceNotInitialized", "项目目录服务未初始化"));
                        }

                        await _projectCatalogService.RestoreAsync(project.ProjectGuid.Value);
                        await ReloadProjectsAsync();

                        MessageBox.Show(LocalizationManager.TF("PM.Restored", "项目 '{0}' 已恢复", project.Name), LocalizationManager.T("Msg.Tip", "提示"), MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(LocalizationManager.TF("PM.RestoreFailed", "恢复项目失败：{0}", ex.Message), LocalizationManager.T("Msg.Error", "错误"), MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        /// <summary>
        /// 清空回收站按钮点击事件
        /// </summary>
        private async void EmptyRecycleBin_Click(object sender, RoutedEventArgs e)
        {
            if (_deletedProjects.Count == 0)
            {
                MessageBox.Show(LocalizationManager.T("PM.RecycleBinEmpty", "回收站已经是空的"), LocalizationManager.T("Msg.Tip", "提示"), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                LocalizationManager.TF("PM.EmptyRecycleBinConfirm", "确定要清空回收站吗？\n这将永久删除 {0} 个项目，此操作不可撤销！", _deletedProjects.Count),
                LocalizationManager.T("PM.EmptyRecycleBin", "清空回收站"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    if (_projectCatalogService == null)
                    {
                        throw new InvalidOperationException(LocalizationManager.T("PM.ServiceNotInitialized", "项目目录服务未初始化"));
                    }

                    var deletedCount = await _projectCatalogService.EmptyRecycleBinAsync();
                    await ReloadProjectsAsync();

                    MessageBox.Show(LocalizationManager.TF("PM.PermanentlyDeletedCount", "已永久删除 {0} 个项目", deletedCount), LocalizationManager.T("Msg.Tip", "提示"), MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(LocalizationManager.TF("PM.EmptyRecycleBinFailed", "清空回收站失败：{0}", ex.Message), LocalizationManager.T("Msg.Error", "错误"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 添加新项目
        /// </summary>
        /// <param name="projectData">项目数据</param>
        public async void AddNewProject(NewProjectDialog.NewProjectModel projectData)
        {
            try
            {
                if (_projectCatalogService == null)
                {
                    throw new InvalidOperationException(LocalizationManager.T("PM.ServiceNotInitialized", "项目目录服务未初始化"));
                }

                var createdProject = await _projectCatalogService.CreateProjectAsync(projectData);
                await ReloadProjectsAsync();

                var createdViewModel = _allProjects.FirstOrDefault(p => p.ProjectGuid == createdProject.ProjectId);
                if (createdViewModel != null)
                {
                    OpenProjectInternal(createdViewModel);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(LocalizationManager.TF("PM.AddFailed", "添加项目失败：{0}", ex.Message), LocalizationManager.T("Msg.Error", "错误"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        private void OpenProjectInternal(ProjectViewModel project)
        {
            try
            {
                var projectId = project.ProjectGuid ?? Guid.NewGuid();
                project.ProjectGuid = projectId;
                _projectContextService?.SetCurrentProject(projectId, project.Name);
                _ = _projectCatalogService?.TouchProjectAsync(projectId);

                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.NavigateTo(NavigationTarget.ProjectOverview, new NavigationContext
                    {
                        ProjectId = project.ProjectGuid,
                        ProjectName = project.Name,
                        Source = "ProjectManagement.OpenProject",
                        Payload = project
                    });
                }
                else
                {
                    var projectWindow = new Window
                    {
                        Title = LocalizationManager.TF("PM.OpenedProjectTitle", "项目: {0}", project.Name),
                        Width = 1200,
                        Height = 800,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        Content = new ProjectOverviewView()
                    };
                    projectWindow.Show();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(LocalizationManager.TF("PM.OpenFailed", "打开项目失败：{0}", ex.Message), LocalizationManager.T("Msg.Error", "错误"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static ProjectViewModel MapToViewModel(ProjectCatalogItem item, int displayId)
        {
            return new ProjectViewModel
            {
                Id = displayId,
                ProjectGuid = item.ProjectId,
                Name = item.Name,
                Description = item.Description,
                Type = item.Type,
                Status = item.Status,
                LastUpdated = item.LastUpdated,
                IsDeleted = item.IsDeleted,
                DeletedAt = item.DeletedAt,
                DeletedBy = item.DeletedBy,
                ProjectPath = item.ProjectPath
            };
        }

        private static ProjectCatalogItem MapToCatalogItem(ProjectViewModel item)
        {
            return new ProjectCatalogItem
            {
                ProjectId = item.ProjectGuid ?? Guid.Empty,
                Name = item.Name,
                Description = item.Description,
                Type = item.Type,
                Status = item.Status,
                LastUpdated = item.LastUpdated,
                IsDeleted = item.IsDeleted,
                DeletedAt = item.DeletedAt,
                DeletedBy = item.DeletedBy,
                ProjectPath = item.ProjectPath
            };
        }
    }
}
