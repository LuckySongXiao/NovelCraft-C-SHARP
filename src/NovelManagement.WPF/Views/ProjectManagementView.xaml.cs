using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using NovelManagement.Application.Services;
using NovelManagement.Core.Entities;
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
            public Guid Id { get; set; } // Changed to Guid to match Entity
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string LastUpdated { get; set; } = string.Empty;
            public bool IsDeleted { get; set; } = false;
            public DateTime? DeletedAt { get; set; }
            public string? DeletedBy { get; set; }
            public string ProjectPath { get; set; } = string.Empty;
            public bool IsExisting => !string.IsNullOrEmpty(ProjectPath) && Directory.Exists(ProjectPath);
        }

        private List<ProjectViewModel> _allProjects = new();
        private List<ProjectViewModel> _filteredProjects = new();
        private List<ProjectViewModel> _deletedProjects = new();
        private bool _showRecycleBin = false;
        
        private readonly ProjectService? _projectService;
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
        public ProjectManagementView()
        {
            InitializeComponent();
            
            // 获取服务
            _projectService = App.ServiceProvider?.GetService<ProjectService>();
            _projectContextService = App.ServiceProvider?.GetService<ProjectContextService>();
            
            _ = LoadProjectsAsync();
        }

        #region 数据加载

        /// <summary>
        /// 加载项目数据
        /// </summary>
        private async Task LoadProjectsAsync()
        {
            if (_projectService == null) return;

            try
            {
                // 加载实际存在的项目数据
                var projects = await _projectService.GetAllProjectsAsync();
                
                _allProjects = projects.Select(p => new ProjectViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description ?? string.Empty,
                    Type = p.Type,
                    Status = p.Status,
                    LastUpdated = p.UpdatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                    ProjectPath = p.ProjectPath ?? string.Empty,
                    IsDeleted = false // 目前数据库不支持软删除
                }).ToList();

                // 暂时不支持回收站（除非我们使用本地存储）
                _deletedProjects = new List<ProjectViewModel>();

                // 根据当前视图模式过滤项目
                FilterProjects();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载项目失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        // LoadProjects, LoadExistingProjects, LoadDeletedProjects are replaced by LoadProjectsAsync
        
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
                _filteredProjects = new List<ProjectViewModel>(_allProjects);
            }
            else
            {
                _filteredProjects = _allProjects.Where(p => 
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
            if (_projectService == null)
            {
                MessageBox.Show("项目服务不可用", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var dialog = new NewProjectDialog();
            dialog.Owner = Window.GetWindow(this);

            if (dialog.ShowDialog() == true && dialog.IsConfirmed && dialog.ProjectData != null)
            {
                try
                {
                    // 创建新项目实体
                    var newProjectEntity = new Project
                    {
                        Id = Guid.NewGuid(),
                        Name = dialog.ProjectData.Name,
                        Description = dialog.ProjectData.Description,
                        Type = dialog.ProjectData.Type,
                        Status = "Planning",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    // 保存到数据库
                    await _projectService.CreateProjectAsync(newProjectEntity);

                    // 创建新的项目视图模型
                    var newProject = new ProjectViewModel
                    {
                        Id = newProjectEntity.Id,
                        Name = newProjectEntity.Name,
                        Description = newProjectEntity.Description ?? string.Empty,
                        Type = newProjectEntity.Type,
                        Status = newProjectEntity.Status,
                        LastUpdated = "刚刚"
                    };

                    // 添加到项目列表
                    _allProjects.Add(newProject);
                    
                    // 刷新列表
                    FilterProjects();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"创建项目失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// 导入项目按钮点击事件
        /// </summary>
        private void ImportProject_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "导入项目",
                    Filter = "项目文件|*.npj;*.json|JSON文件|*.json|所有文件|*.*",
                    DefaultExt = "npj"
                };

                if (dialog.ShowDialog() == true)
                {
                    // 模拟导入过程
                    var fileName = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);
                    var newProject = new ProjectViewModel
                    {
                        Id = Guid.NewGuid(),
                        Name = fileName,
                        Description = $"从文件 {dialog.FileName} 导入的项目",
                        Type = "导入项目",
                        Status = "进行中",
                        LastUpdated = "刚刚"
                    };

                    _allProjects.Add(newProject);
                    _filteredProjects.Add(newProject);
                    UpdateProjectList();

                    MessageBox.Show($"项目 '{fileName}' 导入成功！", "导入完成",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入项目失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 导出项目按钮点击事件
        /// </summary>
        private void ExportProject_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_filteredProjects.Count == 0)
                {
                    MessageBox.Show("没有可导出的项目", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "导出项目",
                    Filter = "项目文件|*.npj|JSON文件|*.json|所有文件|*.*",
                    DefaultExt = "npj",
                    FileName = $"项目导出_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (dialog.ShowDialog() == true)
                {
                    // 模拟导出过程
                    MessageBox.Show($"已导出 {_filteredProjects.Count} 个项目到：\n{dialog.FileName}",
                        "导出完成", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出项目失败：{ex.Message}", "错误",
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
                try
                {
                    // 设置全局项目上下文
                    if (_projectContextService != null)
                    {
                        _projectContextService.SetCurrentProject(project.Id, project.Name);
                    }

                    // 获取主窗口并切换到项目概览
                    var mainWindow = Window.GetWindow(this) as MainWindow;
                    if (mainWindow != null)
                    {
                        // 切换到项目概览界面
                        mainWindow.ShowProjectOverview();

                        MessageBox.Show($"已打开项目: {project.Name}\n当前显示项目概览界面",
                            "项目已打开", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        // 如果无法获取主窗口，创建新的项目概览窗口
                        var projectWindow = new Window
                        {
                            Title = $"项目: {project.Name}",
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
                    MessageBox.Show($"打开项目失败：{ex.Message}", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
                        if (_projectService != null)
                        {
                            // 获取现有项目实体
                            var projectEntity = await _projectService.GetProjectByIdAsync(project.Id);
                            if (projectEntity != null)
                            {
                                // 更新实体属性
                                projectEntity.Name = dialog.ProjectData.Name;
                                projectEntity.Description = dialog.ProjectData.Description;
                                projectEntity.Type = dialog.ProjectData.Type;
                                projectEntity.UpdatedAt = DateTime.UtcNow;

                                // 保存到数据库
                                await _projectService.UpdateProjectAsync(projectEntity);
                                
                                // 更新视图模型
                                project.Name = dialog.ProjectData.Name;
                                project.Description = dialog.ProjectData.Description;
                                project.Type = dialog.ProjectData.Type;
                                project.LastUpdated = "刚刚";
                                
                                // 如果是当前项目，更新上下文
                                if (_projectContextService != null && _projectContextService.CurrentProjectId == project.Id)
                                {
                                    _projectContextService.SetCurrentProject(project.Id, project.Name);
                                }

                                // 刷新列表显示
                                UpdateProjectList();

                                MessageBox.Show($"项目 '{project.Name}' 更新成功！", "编辑完成",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"编辑项目失败：{ex.Message}", "错误",
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
                // 直接执行删除操作
                DeleteProjectAsync(project);
            }
        }

        /// <summary>
        /// 异步删除项目
        /// </summary>
        private async void DeleteProjectAsync(ProjectViewModel project)
        {
            var result = MessageBox.Show(
                $"确定要删除项目 '{project.Name}' 吗？\n此操作将永久删除项目及其所有数据，且不可恢复！",
                "删除项目",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    if (_projectService != null)
                    {
                        // 从数据库删除
                        await _projectService.DeleteProjectAsync(project.Id);
                        
                        // 检查是否是当前打开的项目，如果是则清除上下文
                        if (_projectContextService != null && _projectContextService.CurrentProjectId == project.Id)
                        {
                            _projectContextService.ClearCurrentProject();
                        }
                        
                        // 从列表移除
                        _allProjects.Remove(project);
                        
                        // 刷新显示
                        FilterProjects();

                        MessageBox.Show($"项目 '{project.Name}' 已删除", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"删除项目失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        // MoveToRecycleBin and PermanentlyDeleteProject are removed/replaced by DeleteProjectAsync

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
        private void RestoreProject_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ProjectViewModel project)
            {
                var result = MessageBox.Show(
                    $"确定要恢复项目 '{project.Name}' 吗？",
                    "恢复项目",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // 恢复项目
                        project.IsDeleted = false;
                        project.DeletedAt = null;
                        project.DeletedBy = null;

                        // 从回收站移除，添加到活动项目列表
                        _deletedProjects.Remove(project);
                        _allProjects.Add(project);

                        // 刷新显示
                        FilterProjects();

                        MessageBox.Show($"项目 '{project.Name}' 已恢复", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"恢复项目失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        /// <summary>
        /// 清空回收站按钮点击事件
        /// </summary>
        private void EmptyRecycleBin_Click(object sender, RoutedEventArgs e)
        {
            if (_deletedProjects.Count == 0)
            {
                MessageBox.Show("回收站已经是空的", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"确定要清空回收站吗？\n这将永久删除 {_deletedProjects.Count} 个项目，此操作不可撤销！",
                "清空回收站",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // 永久删除所有回收站项目
                    var deletedCount = _deletedProjects.Count;
                    _deletedProjects.Clear();

                    // 刷新显示
                    FilterProjects();

                    MessageBox.Show($"已永久删除 {deletedCount} 个项目", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"清空回收站失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
            if (_projectService == null) return;

            try
            {
                // 创建新项目实体
                var newProjectEntity = new Project
                {
                    Id = Guid.NewGuid(),
                    Name = projectData.Name,
                    Description = projectData.Description,
                    Type = projectData.Type,
                    Status = "Planning",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // 保存到数据库
                await _projectService.CreateProjectAsync(newProjectEntity);

                // 创建新的项目视图模型
                var newProject = new ProjectViewModel
                {
                    Id = newProjectEntity.Id,
                    Name = newProjectEntity.Name,
                    Description = newProjectEntity.Description ?? string.Empty,
                    Type = newProjectEntity.Type,
                    Status = newProjectEntity.Status,
                    LastUpdated = "刚刚"
                };

                // 添加到项目列表
                _allProjects.Add(newProject);
                
                // 刷新列表
                FilterProjects();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加项目失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
