using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using NovelManagement.Application.Services;
using NovelManagement.WPF.Services;

namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// ProjectOverviewView.xaml 的交互逻辑
    /// </summary>
    public partial class ProjectOverviewView : UserControl, INavigationRefreshableView, INavigationAwareView
    {
        private NavigationContext? _navigationContext;
        private readonly ProjectReadModelService? _projectReadModelService;

        private MainWindow? GetMainWindow() => Window.GetWindow(this) as MainWindow;

        private bool EnsureCurrentProject(string featureName)
        {
            return TryGetCurrentProjectId(featureName, out _);
        }

        private bool TryNavigateToMainWindow(
            string featureName,
            NavigationTarget target,
            bool requireProject = true,
            object? payload = null,
            string? source = null)
        {
            Guid? projectId = null;
            string? projectName = null;
            if (requireProject)
            {
                if (!TryGetCurrentProjectId(featureName, out var currentProjectId))
                {
                    return false;
                }

                projectId = currentProjectId;
                projectName = _navigationContext?.ProjectName;
            }

            var mainWindow = GetMainWindow();
            if (mainWindow == null)
            {
                MessageBox.Show("无法获取主窗口", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            mainWindow.NavigateTo(target, new NavigationContext
            {
                ProjectId = projectId,
                ProjectName = projectName,
                Source = source ?? "ProjectOverview",
                Payload = payload
            });
            return true;
        }

        private bool TryGetCurrentProjectId(string featureName, out Guid projectId)
        {
            var mainWindow = GetMainWindow();
            var guard = App.ServiceProvider?.GetService<CurrentProjectGuard>();
            if (guard == null)
            {
                MessageBox.Show("项目校验服务未初始化", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                projectId = Guid.Empty;
                return false;
            }

            return guard.TryGetCurrentProjectId(mainWindow, featureName, out projectId, () => mainWindow?.ShowProjectManagement());
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public ProjectOverviewView()
        {
            InitializeComponent();
            _projectReadModelService = App.ServiceProvider?.GetService<ProjectReadModelService>();
            Loaded += ProjectOverviewView_Loaded;
        }

        /// <summary>
        /// 在项目切换后刷新项目概览数据。
        /// </summary>
        /// <param name="projectId">当前项目标识。</param>
        /// <param name="projectName">当前项目名称。</param>
        public async Task RefreshOnProjectChangedAsync(Guid? projectId, string? projectName)
        {
            _navigationContext = new NavigationContext
            {
                ProjectId = projectId,
                ProjectName = projectName,
                Source = "ProjectChanged"
            };

            await RefreshWorkspaceAsync(projectId);
        }

        /// <summary>
        /// 在导航到当前视图时应用导航上下文并刷新工作区。
        /// </summary>
        /// <param name="context">导航上下文。</param>
        public void OnNavigatedTo(NavigationContext context)
        {
            _navigationContext = context;
            _ = RefreshWorkspaceAsync(context.ProjectId);
        }

        private async void ProjectOverviewView_Loaded(object sender, RoutedEventArgs e)
        {
            var projectId = _navigationContext?.ProjectId;
            if (projectId == null && TryGetCurrentProjectId("项目概览", out var currentProjectId))
            {
                projectId = currentProjectId;
            }

            await RefreshWorkspaceAsync(projectId);
        }

        private async Task RefreshWorkspaceAsync(Guid? projectId)
        {
            if (projectId == null || projectId == Guid.Empty || _projectReadModelService == null)
            {
                return;
            }

            var workspace = await _projectReadModelService.GetWorkspaceAsync(projectId.Value);
            if (workspace == null)
            {
                return;
            }

            ProjectTitleTextBlock.Text = workspace.ProjectName;
            ProjectTypeTextBlock.Text = workspace.ProjectType;
            ProjectStatusChip.Content = workspace.ProjectStatus;
            ProjectCreatedTextBlock.Text = workspace.ProjectCreatedAtText;
            ProjectUpdatedTextBlock.Text = workspace.ProjectUpdatedAtText;
            ProjectAuthorTextBlock.Text = workspace.ProjectAuthorText;
            ProjectTargetWordsTextBlock.Text = workspace.ProjectTargetWordsText;
            WordCountProgressTextBlock.Text = workspace.WordCountProgressText;
            WordCountProgressBar.Value = workspace.WordCountProgressPercent;
            WordCountPercentTextBlock.Text = workspace.WordCountPercentText;
            ChapterCountProgressTextBlock.Text = workspace.ChapterCountProgressText;
            ChapterCountProgressBar.Value = workspace.ChapterCountProgressPercent;
            ChapterCountPercentTextBlock.Text = workspace.ChapterCountPercentText;
            CharacterCountTextBlock.Text = workspace.CharacterCountText;
            SettingCompletionTextBlock.Text = workspace.SettingCompletionText;
            CharacterListTextBlock.Text = workspace.CharacterOverviewText;
            OutlineContentTextBlock.Text = workspace.OutlineOverviewText;
        }

        #region 事件处理

        /// <summary>
        /// 写作新章节按钮点击事件
        /// </summary>
        private void WriteChapter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TryNavigateToMainWindow("新建章节", NavigationTarget.VolumeManagement,
                    payload: new VolumeNavigationPayload { Action = "CreateChapter" },
                    source: "ProjectOverview.WriteChapter");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开卷章管理失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 管理角色按钮点击事件
        /// </summary>
        private void ManageCharacters_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TryNavigateToMainWindow("角色管理", NavigationTarget.CharacterManagement);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开角色管理失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 剧情大纲按钮点击事件
        /// </summary>
        private void PlotOutline_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TryNavigateToMainWindow("剧情管理", NavigationTarget.PlotManagement);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开剧情管理失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// AI助手按钮点击事件
        /// </summary>
        private void AIAssistant_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TryNavigateToMainWindow("AI协作", NavigationTarget.AICollaboration);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开AI助手失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 项目状态按钮点击事件
        /// </summary>
        private void ProjectStatus_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TryNavigateToMainWindow("项目主页", NavigationTarget.VolumeManagement,
                    payload: new VolumeNavigationPayload { Action = "ProjectHome" },
                    source: "ProjectOverview.ProjectStatus");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"进入项目主页失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 世界设定按钮点击事件
        /// </summary>
        private void WorldSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TryNavigateToMainWindow("世界设定", NavigationTarget.WorldSettingManagement,
                    payload: new WorldSettingNavigationPayload { Action = "WorldSettingsHome" },
                    source: "ProjectOverview.WorldSettings");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开世界设定管理失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 查看全部角色按钮点击事件
        /// </summary>
        private void ViewAllCharacters_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TryNavigateToMainWindow("角色管理", NavigationTarget.CharacterManagement);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开角色管理失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// AI生成大纲按钮点击事件
        /// </summary>
        private void GenerateOutline_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!TryGetCurrentProjectId("AI大纲生成", out var currentProjectId))
                {
                    return;
                }

                // 创建AI大纲生成对话框
                var dialog = new AIOutlineGeneratorDialog(currentProjectId);
                dialog.Owner = Window.GetWindow(this);
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                var result = dialog.ShowDialog();
                if (result == true)
                {
                    MessageBox.Show("大纲生成完成并已保存", "成功",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开AI大纲生成器失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 编辑大纲按钮点击事件
        /// </summary>
        private void EditOutline_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TryNavigateToMainWindow("剧情管理", NavigationTarget.PlotManagement);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开剧情管理失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 导入导出按钮点击事件
        /// </summary>
        private void ImportExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!TryNavigateToMainWindow("导入导出", NavigationTarget.ImportExport,
                    payload: new ImportExportNavigationPayload { Action = "EntireProjectExport" },
                    source: "ProjectOverview.ImportExport"))
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开导入导出界面失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 项目设置按钮点击事件
        /// </summary>
        private async void ProjectSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!TryGetCurrentProjectId("项目设置", out var currentProjectId))
                {
                    return;
                }

                var readModelService = _projectReadModelService;
                if (readModelService == null)
                {
                    MessageBox.Show("项目读模型服务未初始化", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var workspace = await readModelService.GetWorkspaceAsync(currentProjectId);
                if (workspace == null)
                {
                    MessageBox.Show("未找到当前项目，请重新选择项目。", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var currentProject = new ProjectManagementView.ProjectViewModel
                {
                    Id = 0,
                    Name = workspace.ProjectName,
                    Description = workspace.ProjectDescription,
                    Type = workspace.ProjectType,
                    Status = workspace.ProjectStatus,
                    LastUpdated = workspace.ProjectUpdatedAtText
                };

                // 创建项目设置对话框
                var settingsDialog = new EditProjectDialog(currentProject);
                settingsDialog.Owner = Window.GetWindow(this);
                settingsDialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                var result = settingsDialog.ShowDialog();
                if (result == true)
                {
                    MessageBox.Show("项目设置已更新", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开项目设置失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 统计分析按钮点击事件
        /// </summary>
        private async void Statistics_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!TryGetCurrentProjectId("统计分析", out var currentProjectId))
                {
                    return;
                }

                var statisticsService = App.ServiceProvider?.GetService<ProjectStatisticsService>();
                if (statisticsService == null)
                {
                    MessageBox.Show("项目统计服务未初始化", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var summary = await statisticsService.GetProjectStatisticsAsync(currentProjectId);
                if (summary == null)
                {
                    MessageBox.Show("未找到当前项目，请重新选择项目。", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var statisticsWindow = statisticsService.CreateStatisticsWindow(summary);
                statisticsWindow.Owner = Window.GetWindow(this);
                statisticsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开统计分析失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 备份项目按钮点击事件
        /// </summary>
        private void Backup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!TryGetCurrentProjectId("项目备份", out var currentProjectId))
                {
                    return;
                }

                var projectService = App.ServiceProvider?.GetService<ProjectService>();
                if (projectService == null)
                {
                    MessageBox.Show("项目服务未初始化", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var project = projectService.GetProjectByIdAsync(currentProjectId).GetAwaiter().GetResult();
                if (project == null)
                {
                    MessageBox.Show("未找到当前项目，请重新选择项目。", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    $"确定要备份当前项目“{project.Name}”吗？\n\n备份将包含：\n• 所有文本内容\n• 角色设定\n• 剧情大纲\n• 世界设定\n• 项目配置",
                    "备份确认",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    MessageBox.Show(
                        $"当前项目：{project.Name}\n\n真实备份链路尚未接入，本入口已完成项目上下文收口。\n后续将统一接入导出/备份服务，避免继续使用演示数据。",
                        "功能待完善",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"备份项目失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
