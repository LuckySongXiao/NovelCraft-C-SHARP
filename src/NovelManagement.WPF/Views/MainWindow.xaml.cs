using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
using NovelManagement.Application.Services;
using NovelManagement.WPF.Events;
using NovelManagement.WPF.Services;
using NovelManagement.WPF.Services.Copilot;
using NovelManagement.WPF.Views.Copilot;

namespace NovelManagement.WPF.Views;

/// <summary>
/// MainWindow.xaml 的交互逻辑
/// </summary>
public partial class MainWindow : Window
{
    #region 字段

    // 保持对各个界面的引用，以便进行数据同步
    private CharacterManagementView? _characterManagementView;
    private RelationshipNetworkView? _relationshipNetworkView;
    private readonly NavigationService? _navigationService;
    private readonly ProjectContextService? _projectContextService;
    private readonly ProjectCatalogService? _projectCatalogService;
    private readonly ObservableCollection<ProjectNavMenuItem> _projectMenuItems = new();
    private readonly ObservableCollection<RecentActivityItem> _recentActivities = new();
    private readonly System.Windows.Threading.DispatcherTimer? _recentActivityTimer;
    private bool _sidebarStateLoaded;
    private bool _copilotOpen;

    /// <summary>侧栏展开状态持久化文件路径（exe 旁，与 theme_settings.json 同目录）。</summary>
    private static string SidebarStatePath =>
        System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sidebar_state.json");

    #endregion

    /// <summary>
    /// 构造函数
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        _navigationService = App.ServiceProvider?.GetService<NavigationService>();
        _projectContextService = App.ServiceProvider?.GetService<ProjectContextService>();
        _projectCatalogService = App.ServiceProvider?.GetService<ProjectCatalogService>();
        _navigationService?.Configure(CreateNavigationRequest, RenderNavigationView);
        if (_navigationService != null)
        {
            _navigationService.NavigationStateChanged += OnNavigationStateChanged;
        }
        if (_projectContextService != null)
        {
            _projectContextService.ProjectChanged += OnProjectChanged;
        }

        // 动态项目导航列表
        ProjectMenuItemsControl.ItemsSource = _projectMenuItems;
        _ = LoadProjectMenuAsync();

        // 最近活动：真实项目更新动态（启动加载 + 每分钟自动刷新）
        RecentActivitiesControl.ItemsSource = _recentActivities;
        _ = LoadRecentActivitiesAsync();
        _recentActivityTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        _recentActivityTimer.Tick += (_, _) =>
        {
            _ = LoadRecentActivitiesAsync();
            StatusBarTimeText.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        };
        _recentActivityTimer.Start();
        StatusBarTimeText.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // 订阅窗口关闭事件
        this.Closing += MainWindow_Closing;

        // AI 创作助手：接线导航回调与项目上下文（会话服务为 Singleton，消息流跨页面存活）
        var copilotSession = App.ServiceProvider?.GetService<CopilotSessionService>();
        if (copilotSession != null)
        {
            copilotSession.NavigationRequested = (target, context) => _navigationService?.NavigateTo(target, context);
            if (_projectContextService?.HasCurrentProject == true)
            {
                copilotSession.OpenForProject(
                    _projectContextService.CurrentProjectId!.Value,
                    _projectContextService.CurrentProjectName ?? string.Empty);
            }
        }
        CopilotDrawer.CloseRequested += (_, _) => SetCopilotPanelOpen(false);

        // 窗口加载后恢复侧栏固定组展开状态（项目组经菜单项属性由绑定恢复）
        Loaded += (_, _) => RestoreFixedGroupState();

        UpdateNavigationDisplay();
    }

    /// <summary>
    /// 窗口关闭事件处理
    /// </summary>
    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        try
        {
            _recentActivityTimer?.Stop();

            if (_projectContextService != null)
            {
                _projectContextService.ProjectChanged -= OnProjectChanged;
            }

            if (_navigationService != null)
            {
                _navigationService.NavigationStateChanged -= OnNavigationStateChanged;
            }

            // 清理当前视图的资源
            CleanupCurrentView();
        }
        catch (Exception ex)
        {
            // 记录错误但不阻止窗口关闭
            System.Diagnostics.Debug.WriteLine($"窗口关闭时清理资源出错: {ex.Message}");
        }
    }

    #region 项目管理事件

    /// <summary>
    /// 项目管理按钮点击事件
    /// </summary>
    private void ProjectManagement_Click(object sender, RoutedEventArgs e)
    {
        ShowProjectManagement();
    }

    /// <summary>
    /// 新建项目按钮点击事件
    /// </summary>
    private void NewProject_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // 创建新项目对话框
            var dialog = new NewProjectDialog();
            dialog.Owner = this;

            if (dialog.ShowDialog() == true && dialog.IsConfirmed && dialog.ProjectData != null)
            {
                // 切换到项目管理界面
                ShowProjectManagement();

                // 通知项目管理界面添加新项目
                if (MainContentArea.Children.Count > 0 && MainContentArea.Children[0] is ProjectManagementView projectManagementView)
                {
                    projectManagementView.AddNewProject(dialog.ProjectData);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"创建项目失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
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
                var fileName = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);
                var projectData = new NewProjectDialog.NewProjectModel
                {
                    Name = fileName,
                    Description = $"从文件 {dialog.FileName} 导入的项目",
                    Type = "导入项目",
                    TargetWordCount = 100000,
                    EnableAI = true,
                    AutoSave = true,
                    VersionControl = false,
                    Template = "标准模板"
                };

                // 切换到项目管理界面
                ShowProjectManagement();

                // 通知项目管理界面添加导入的项目
                if (MainContentArea.Children.Count > 0 && MainContentArea.Children[0] is ProjectManagementView projectManagementView)
                {
                    projectManagementView.AddNewProject(projectData);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导入项目失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 项目概览按钮点击事件
    /// </summary>
    private void ProjectOverview_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // 切换到项目概览视图
            ShowProjectOverview();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开项目概览失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 卷宗管理按钮点击事件
    /// </summary>
    private void VolumeManagement_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ShowVolumeManagement();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开卷宗管理失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 人物管理按钮点击事件
    /// </summary>
    private void ContentManagement_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ShowCharacterManagement();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开人物管理失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 关系网络按钮点击事件
    /// </summary>
    private void RelationshipNetwork_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ShowRelationshipNetwork();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载关系网络界面失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 势力管理按钮点击事件
    /// </summary>
    private void FactionManagement_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ShowFactionManagement();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开势力管理失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 剧情管理按钮点击事件
    /// </summary>
    private void PlotManagement_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ShowPlotManagement();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开剧情管理失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 设定管理按钮点击事件
    /// </summary>
    private void SettingManagement_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ShowWorldSettingManagement();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开设定管理失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    #region AI助手事件

    /// <summary>
    /// 前置条件生成按钮点击事件
    /// </summary>
    private void PrerequisiteGeneration_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ShowPrerequisiteGeneration();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开前置条件生成失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// AI协作创作按钮点击事件
    /// </summary>
    private void AICollaboration_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ShowAICollaboration();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开AI协作失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// AI模型配置按钮点击事件
    /// </summary>
    private void AIConfiguration_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ShowAIConfiguration();
        }
        catch (Exception ex)
        {
            var detail = ex.InnerException?.Message;
            var message = string.IsNullOrWhiteSpace(detail) ? ex.Message : $"{ex.Message}\n详细信息：{detail}";
            MessageBox.Show($"打开AI模型配置失败：{message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 帮助按钮点击事件
    /// </summary>
    private void Help_Click(object sender, RoutedEventArgs e)
    {
        var helpMessage =
            "常用入口说明：\n" +
            "1. 左侧导航用于项目、人物、设定、AI 协作等功能切换。\n" +
            "2. 右上角齿轮用于打开 AI 模型配置中心。\n" +
            "3. AI 模型配置会保存到当前用户的本地配置目录，不会覆盖发布目录。\n" +
            "4. 修改模型配置后，部分选项需要重启应用才会完全生效。\n\n" +
            "如果某个页面打不开，请把完整弹窗内容发给我继续修复。";

        MessageBox.Show(helpMessage, "使用帮助", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <summary>
    /// 对话生成器按钮点击事件
    /// </summary>
    private void DialogGeneration_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ShowDialogGeneration();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开对话生成器失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 内容生成按钮点击事件
    /// </summary>
    private void ContentGeneration_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // 创建内容生成界面
            var contentWindow = new Window
            {
                Title = "AI内容生成",
                Width = 1000,
                Height = 700,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Content = new DialogGenerationView()
            };
            contentWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开内容生成失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 质量检查按钮点击事件
    /// </summary>
    private void QualityCheck_Click(object sender, RoutedEventArgs e)
    {
        ShowQualityCheck();
    }

    /// <summary>
    /// 一致性检查按钮点击事件
    /// </summary>
    private void ConsistencyCheck_Click(object sender, RoutedEventArgs e)
    {
        ShowConsistencyCheck();
    }

    public void ShowQualityCheck()
    {
        try
        {
            NavigateTo(NavigationTarget.ProjectHealthCheck, new NavigationContext
            {
                Source = "QualityCheck",
                Payload = ProjectHealthCheckMode.Quality
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开质量检查失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void ShowConsistencyCheck()
    {
        try
        {
            NavigateTo(NavigationTarget.ProjectHealthCheck, new NavigationContext
            {
                Source = "ConsistencyCheck",
                Payload = ProjectHealthCheckMode.Consistency
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开一致性检查失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    #region 设定管理事件

    /// <summary>
    /// 世界设定管理按钮点击事件
    /// </summary>
    private void WorldSettingManagement_Click(object sender, RoutedEventArgs e)
    {
        ShowWorldSettingManagement();
    }

    /// <summary>
    /// 修炼体系按钮点击事件
    /// </summary>
    private void CultivationSystem_Click(object sender, RoutedEventArgs e)
    {
        ShowCultivationSystem();
    }

    /// <summary>
    /// 政治体系按钮点击事件
    /// </summary>
    private void PoliticalSystem_Click(object sender, RoutedEventArgs e)
    {
        ShowPoliticalSystem();
    }

    /// <summary>
    /// 职业体系按钮点击事件
    /// </summary>
    private void ProfessionSystem_Click(object sender, RoutedEventArgs e)
    {
        ShowProfessionSystem();
    }

    /// <summary>
    /// 司法体系按钮点击事件
    /// </summary>
    private void JudicialSystem_Click(object sender, RoutedEventArgs e)
    {
        ShowJudicialSystem();
    }

    /// <summary>
    /// 生民体系按钮点击事件
    /// </summary>
    private void PopulationSystem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ShowPopulationSystem();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开生民体系失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 灵宝体系按钮点击事件
    /// </summary>
    private void TreasureSystem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ShowTreasureSystem();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开灵宝体系失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 维度结构按钮点击事件
    /// </summary>
    private void DimensionStructure_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ShowDimensionStructure();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开维度结构失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 地图结构按钮点击事件
    /// </summary>
    private void MapStructure_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ShowMapStructure();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开地图结构失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 宠物体系按钮点击事件
    /// </summary>
    private void PetSystem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ShowPetSystem();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开宠物体系失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 装备体系按钮点击事件
    /// </summary>
    private void EquipmentSystem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // 清空主内容区域
            MainContentArea.Children.Clear();

            // 添加装备体系视图
            var equipmentSystemView = new EquipmentSystemView();
            MainContentArea.Children.Add(equipmentSystemView);

            // 更新窗口标题
            this.Title = "书籍管理系统 - 装备体系管理";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开装备体系失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 功法体系按钮点击事件
    /// </summary>
    private void TechniqueSystem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // 清空主内容区域
            MainContentArea.Children.Clear();

            // 添加功法体系视图
            var techniqueSystemView = new TechniqueSystemView();
            MainContentArea.Children.Add(techniqueSystemView);

            // 更新窗口标题
            this.Title = "书籍管理系统 - 功法体系管理";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开功法体系失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 商业体系按钮点击事件
    /// </summary>
    private void BusinessSystem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // 清空主内容区域
            MainContentArea.Children.Clear();

            // 添加商业体系视图
            var businessSystemView = new BusinessSystemView();
            MainContentArea.Children.Add(businessSystemView);

            // 更新窗口标题
            this.Title = "书籍管理系统 - 商业体系管理";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开商业体系失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 时间线按钮点击事件
    /// </summary>
    private void Timeline_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ShowTimeline();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开时间线失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    #region 导入导出事件

    /// <summary>
    /// 导出项目按钮点击事件
    /// </summary>
    private void ExportProject_Click(object sender, RoutedEventArgs e)
    {
        ShowImportExport();
    }

    /// <summary>
    /// 发布管理按钮点击事件
    /// </summary>
    private void PublishManagement_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var window = App.ServiceProvider?.GetService<OperationsManagementWindow>();
            if (window == null)
            {
                MessageBox.Show("运维管理窗口未初始化", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            window.Owner = this;
            window.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开发布与运维管理失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 统计报告按钮点击事件
    /// </summary>
    private async void StatisticsReport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!TryGetCurrentProjectId("统计报告", out var currentProjectId))
            {
                return;
            }

            var statisticsService = App.ServiceProvider?.GetService<ProjectStatisticsService>();
            if (statisticsService == null)
            {
                MessageBox.Show("项目统计服务未初始化", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var summary = await statisticsService.GetProjectStatisticsAsync(currentProjectId);
            if (summary == null)
            {
                MessageBox.Show("未找到当前项目，请重新选择项目。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var statisticsWindow = statisticsService.CreateStatisticsWindow(summary);
            statisticsWindow.Owner = this;
            statisticsWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开统计报告失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    #region 私有方法

    private NavigationViewRequest CreateNavigationRequest(NavigationTarget target)
    {
        return target switch
        {
            NavigationTarget.ProjectManagement => new NavigationViewRequest
            {
                View = new ProjectManagementView(),
                Title = "书籍管理系统 - 项目管理"
            },
            NavigationTarget.ProjectOverview => new NavigationViewRequest
            {
                View = new ProjectOverviewView(),
                Title = "书籍管理系统 - 项目概览"
            },
            NavigationTarget.VolumeManagement => new NavigationViewRequest
            {
                View = new VolumeManagementView(),
                Title = "书籍管理系统 - 卷宗管理"
            },
            NavigationTarget.CharacterManagement => new NavigationViewRequest
            {
                View = GetOrCreateCharacterManagementView(),
                Title = "书籍管理系统 - 角色管理"
            },
            NavigationTarget.Timeline => new NavigationViewRequest
            {
                View = new TimelineView(),
                Title = "书籍管理系统 - 时间线管理"
            },
            NavigationTarget.RelationshipNetwork => new NavigationViewRequest
            {
                View = GetOrCreateRelationshipNetworkView(),
                Title = "书籍管理系统 - 关系网络"
            },
            NavigationTarget.FactionManagement => new NavigationViewRequest
            {
                View = new FactionManagementView(),
                Title = "书籍管理系统 - 势力管理"
            },
            NavigationTarget.PlotManagement => new NavigationViewRequest
            {
                View = new PlotManagementView(),
                Title = "书籍管理系统 - 剧情管理"
            },
            NavigationTarget.AICollaboration => new NavigationViewRequest
            {
                View = new AIAssistantWorkspaceView(),
                Title = "书籍管理系统 - AI协作创作"
            },
            NavigationTarget.AIConfiguration => new NavigationViewRequest
            {
                View = new AIConfigurationView(),
                Title = "书籍管理系统 - AI模型配置"
            },
            NavigationTarget.ImportExport => new NavigationViewRequest
            {
                View = new ImportExportView(),
                Title = "书籍管理系统 - 导入导出管理"
            },
            NavigationTarget.WorldSettingManagement => new NavigationViewRequest
            {
                View = new WorldSettingManagementView(),
                Title = "书籍管理系统 - 世界设定管理"
            },
            NavigationTarget.DialogGeneration => new NavigationViewRequest
            {
                View = new DialogGenerationView(),
                Title = "书籍管理系统 - AI对话生成器"
            },
            NavigationTarget.ProjectHealthCheck => new NavigationViewRequest
            {
                View = new ProjectHealthCheckView(),
                Title = "书籍管理系统 - 项目体检报告"
            },
            _ => throw new ArgumentOutOfRangeException(nameof(target), target, null)
        };
    }

    private void RenderNavigationView(UserControl view, string title)
    {
        CleanupCurrentView();
        MainContentArea.Children.Clear();
        MainContentArea.Children.Add(view);
        Title = title;
    }

    private CharacterManagementView GetOrCreateCharacterManagementView()
    {
        if (_characterManagementView == null)
        {
            _characterManagementView = new CharacterManagementView();
            _characterManagementView.CharacterUpdated += OnCharacterUpdated;
        }

        return _characterManagementView;
    }

    private RelationshipNetworkView GetOrCreateRelationshipNetworkView()
    {
        _relationshipNetworkView ??= new RelationshipNetworkView();
        return _relationshipNetworkView;
    }

    private async void OnProjectChanged(object? sender, ProjectChangedEventArgs e)
    {
        // 事件可能从后台线程触发（批量生成服务），统一调度到 UI 线程执行
        if (!Dispatcher.CheckAccess())
        {
            await Dispatcher.InvokeAsync(() => OnProjectChanged(sender, e));
            return;
        }

        UpdateCurrentProjectDisplay(e.NewProjectName);

        // AI 创作助手跟随项目切换（切换项目时清空消息流重建会话）
        CopilotDrawer.BindProject(e.NewProjectId, e.NewProjectName ?? string.Empty);

        // 项目切换后刷新最近活动动态
        _ = LoadRecentActivitiesAsync();

        // 自动展开当前项目组（项目管理页「打开项目」/一键生成等路径统一生效）
        if (e.NewProjectId.HasValue)
        {
            var menuItem = _projectMenuItems.FirstOrDefault(m => m.ProjectId == e.NewProjectId.Value);
            if (menuItem != null && !menuItem.IsExpanded)
            {
                menuItem.IsExpanded = true;
                SaveSidebarState();
            }
        }

        if (_navigationService != null)
        {
            await _navigationService.RefreshCurrentViewAsync(e.NewProjectId, e.NewProjectName);
        }
    }

    #region AI 创作助手抽屉

    /// <summary>
    /// 标题栏 AI 创作助手按钮：展开/收起抽屉。
    /// </summary>
    private void CopilotToggle_Click(object sender, RoutedEventArgs e) =>
        SetCopilotPanelOpen(!_copilotOpen);

    /// <summary>
    /// 切换 AI 创作助手抽屉列宽与可见性（收起时列宽归零释放空间）。
    /// </summary>
    private void SetCopilotPanelOpen(bool open)
    {
        _copilotOpen = open;
        CopilotColumn.Width = open ? new GridLength(380) : new GridLength(0);
        CopilotSplitterColumn.Width = open ? new GridLength(5) : new GridLength(0);
        CopilotDrawer.Visibility = open ? Visibility.Visible : Visibility.Collapsed;
        CopilotSplitter.Visibility = open ? Visibility.Visible : Visibility.Collapsed;
    }

    #endregion

    #region 侧栏展开状态持久化

    /// <summary>侧栏展开状态持久化模型。</summary>
    private sealed class SidebarState
    {
        /// <summary>顶层固定组展开状态（键 = 组名）。</summary>
        public Dictionary<string, bool> FixedGroups { get; set; } = new();

        /// <summary>项目组展开的项目 ID 列表。</summary>
        public List<string> ExpandedProjects { get; set; } = new();

        /// <summary>项目嵌套组展开状态（键 = 项目 ID，值 = 展开的组名列表）。</summary>
        public Dictionary<string, List<string>> ProjectSubGroups { get; set; } = new();
    }

    /// <summary>
    /// 读取侧栏展开状态文件（文件缺失或损坏时返回空状态）。
    /// </summary>
    private SidebarState LoadSidebarState()
    {
        try
        {
            if (System.IO.File.Exists(SidebarStatePath))
            {
                var json = System.IO.File.ReadAllText(SidebarStatePath);
                var state = System.Text.Json.JsonSerializer.Deserialize<SidebarState>(json);
                if (state != null)
                {
                    return state;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"读取侧栏状态失败: {ex.Message}");
        }
        return new SidebarState();
    }

    /// <summary>
    /// 保存侧栏展开状态（项目组 + 嵌套组从菜单项集合取值，固定组从视觉树收集）。
    /// </summary>
    private void SaveSidebarState()
    {
        try
        {
            var state = new SidebarState();
            foreach (var item in _projectMenuItems)
            {
                if (item.IsExpanded)
                {
                    state.ExpandedProjects.Add(item.ProjectId.ToString());
                }

                var subs = new List<string>();
                if (item.SettingsGroupExpanded) subs.Add("设定管理");
                if (item.PeopleGroupExpanded) subs.Add("人物管理");
                if (subs.Count > 0)
                {
                    state.ProjectSubGroups[item.ProjectId.ToString()] = subs;
                }
            }

            foreach (var fe in CollectFixedGroupExpanders())
            {
                var name = fe.Tag as string;
                if (!string.IsNullOrEmpty(name))
                {
                    state.FixedGroups[name] = fe.IsExpanded;
                }
            }

            var options = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            System.IO.File.WriteAllText(SidebarStatePath, System.Text.Json.JsonSerializer.Serialize(state, options));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"保存侧栏状态失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 收集主窗口侧栏中带「固定组:」Tag 的顶层 Expander。
    /// </summary>
    private IEnumerable<Expander> CollectFixedGroupExpanders()
    {
        var result = new List<Expander>();
        CollectFixedGroupExpanders(this, result);
        return result;
    }

    private static void CollectFixedGroupExpanders(System.Windows.DependencyObject node, List<Expander> result)
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(node); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(node, i);
            if (child is Expander expander && expander.Tag is string tag && tag.StartsWith("固定组:", StringComparison.Ordinal))
            {
                result.Add(expander);
            }
            CollectFixedGroupExpanders(child, result);
        }
    }

    /// <summary>
    /// 侧栏 Expander 展开状态变化统一处理：更新所属菜单项属性并持久化。
    /// 支持三种 Expander：顶层固定组（Tag=「固定组:组名」）、项目组（DataContext=菜单项）、项目嵌套组（Tag=组名）。
    /// </summary>
    private void SidebarExpander_StateChanged(object sender, RoutedEventArgs e)
    {
        if (!_sidebarStateLoaded || sender is not Expander expander)
        {
            return;
        }

        // 项目组：直接取菜单项
        if (expander.DataContext is ProjectNavMenuItem item)
        {
            SaveSidebarState();
            return;
        }

        // 项目嵌套组：向上找所属项目组 Expander
        var tag = expander.Tag as string;
        if (tag == "设定管理" || tag == "人物管理")
        {
            var owner = FindOwnerProjectItem(expander);
            if (owner != null)
            {
                if (tag == "设定管理") owner.SettingsGroupExpanded = expander.IsExpanded;
                if (tag == "人物管理") owner.PeopleGroupExpanded = expander.IsExpanded;
            }
        }

        SaveSidebarState();
    }

    /// <summary>
    /// 沿可视树向上查找所属项目组 Expander 的菜单项。
    /// </summary>
    private ProjectNavMenuItem? FindOwnerProjectItem(System.Windows.DependencyObject node)
    {
        var current = System.Windows.Media.VisualTreeHelper.GetParent(node);
        while (current != null)
        {
            if (current is Expander expander && expander.DataContext is ProjectNavMenuItem item)
            {
                return item;
            }
            current = System.Windows.Media.VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    /// <summary>
    /// 恢复顶层固定组展开状态（Loaded 时调用；项目组/嵌套组经菜单项属性由绑定恢复）。
    /// </summary>
    private void RestoreFixedGroupState()
    {
        try
        {
            var state = LoadSidebarState();
            foreach (var fe in CollectFixedGroupExpanders())
            {
                var name = fe.Tag as string;
                if (!string.IsNullOrEmpty(name) && state.FixedGroups.TryGetValue(name, out var expanded))
                {
                    fe.IsExpanded = expanded;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"恢复固定组状态失败: {ex.Message}");
        }
        finally
        {
            _sidebarStateLoaded = true;
        }
    }

    #endregion

    /// <summary>
    /// 从数据库加载最近活动：项目更新 + 当前项目最新章节/角色变更，按时间倒序展示。
    /// </summary>
    private async Task LoadRecentActivitiesAsync()
    {
        try
        {
            var activities = new List<RecentActivityItem>();

            // 项目级更新动态
            if (_projectCatalogService != null)
            {
                var projects = await _projectCatalogService.GetActiveProjectsAsync();
                foreach (var project in projects.OrderByDescending(p => p.LastUpdatedAt).Take(5))
                {
                    activities.Add(new RecentActivityItem
                    {
                        Icon = PackIconKind.Book,
                        Message = $"项目《{project.Name}》有内容更新",
                        Timestamp = project.LastUpdatedAt
                    });
                }
            }

            // 当前项目的章节 / 角色更新动态
            var projectId = _projectContextService?.CurrentProjectId;
            if (projectId.HasValue && App.ServiceProvider != null)
            {
                using var scope = App.ServiceProvider.CreateScope();
                var chapterService = scope.ServiceProvider.GetService<ChapterService>();
                if (chapterService != null)
                {
                    var chapters = (await chapterService.GetChaptersByProjectIdAsync(projectId.Value))
                        .OrderByDescending(c => c.UpdatedAt)
                        .Take(5);
                    foreach (var chapter in chapters)
                    {
                        activities.Add(new RecentActivityItem
                        {
                            Icon = PackIconKind.FileDocument,
                            Message = $"更新了章节：{chapter.Title}",
                            Timestamp = ToLocalTime(chapter.UpdatedAt)
                        });
                    }
                }

                var characterService = scope.ServiceProvider.GetService<CharacterService>();
                if (characterService != null)
                {
                    var characters = (await characterService.GetCharactersByProjectIdAsync(projectId.Value))
                        .OrderByDescending(c => c.UpdatedAt)
                        .Take(5);
                    foreach (var character in characters)
                    {
                        activities.Add(new RecentActivityItem
                        {
                            Icon = PackIconKind.Account,
                            Message = $"角色资料更新：{character.Name}",
                            Timestamp = ToLocalTime(character.UpdatedAt)
                        });
                    }
                }
            }

            var top = activities
                .OrderByDescending(a => a.Timestamp)
                .Take(10)
                .ToList();

            await Dispatcher.InvokeAsync(() =>
            {
                _recentActivities.Clear();
                foreach (var activity in top)
                {
                    _recentActivities.Add(activity);
                }

                if (RecentActivityEmptyText != null)
                {
                    RecentActivityEmptyText.Visibility = top.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载最近活动失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 供外部（项目/章节/角色变更后）刷新最近活动动态。
    /// </summary>
    public Task RefreshRecentActivitiesAsync() => LoadRecentActivitiesAsync();

    private static DateTime ToLocalTime(DateTime value)
        => value.Kind == DateTimeKind.Utc ? value.ToLocalTime() : value;

    /// <summary>
    /// 从数据库加载项目列表到左侧导航（新建书籍会自动出现）。
    /// </summary>
    private async Task LoadProjectMenuAsync()
    {
        try
        {
            if (_projectCatalogService == null)
            {
                return;
            }

            var projects = await _projectCatalogService.GetActiveProjectsAsync();
            await Dispatcher.InvokeAsync(() =>
            {
                var state = LoadSidebarState();
                _projectMenuItems.Clear();
                foreach (var project in projects)
                {
                    var idStr = project.ProjectId.ToString();
                    var menuItem = new ProjectNavMenuItem
                    {
                        ProjectId = project.ProjectId,
                        Name = project.Name,
                        Type = project.Type,
                        Description = $"{project.Type} | {project.Status} | {project.LastUpdated}",
                        IsExpanded = state.ExpandedProjects.Contains(idStr)
                    };
                    if (state.ProjectSubGroups.TryGetValue(idStr, out var subs))
                    {
                        menuItem.SettingsGroupExpanded = subs.Contains("设定管理");
                        menuItem.PeopleGroupExpanded = subs.Contains("人物管理");
                    }
                    _projectMenuItems.Add(menuItem);
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载项目导航列表失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 供外部（项目管理页、一键生成）在项目增删后刷新左侧导航与最近活动。
    /// </summary>
    public async Task RefreshProjectMenuAsync()
    {
        await LoadProjectMenuAsync();
        await LoadRecentActivitiesAsync();
    }

    /// <summary>
    /// 左侧项目导航菜单统一入口：先切换当前项目，再分派到原有导航逻辑。
    /// </summary>
    private void ProjectMenuItemNav_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is not FrameworkElement element)
            {
                return;
            }

            var menuItem = FindProjectNavMenuItem(element);
            if (menuItem != null)
            {
                SwitchToProject(menuItem);
            }

            switch (element.Tag?.ToString())
            {
                case "ProjectOverview": ProjectOverview_Click(sender, e); break;
                case "WorldSettingManagement": WorldSettingManagement_Click(sender, e); break;
                case "CultivationSystem": CultivationSystem_Click(sender, e); break;
                case "PoliticalSystem": PoliticalSystem_Click(sender, e); break;
                case "ProfessionSystem": ProfessionSystem_Click(sender, e); break;
                case "JudicialSystem": JudicialSystem_Click(sender, e); break;
                case "PopulationSystem": PopulationSystem_Click(sender, e); break;
                case "TreasureSystem": TreasureSystem_Click(sender, e); break;
                case "DimensionStructure": DimensionStructure_Click(sender, e); break;
                case "MapStructure": MapStructure_Click(sender, e); break;
                case "PetSystem": PetSystem_Click(sender, e); break;
                case "EquipmentSystem": EquipmentSystem_Click(sender, e); break;
                case "TechniqueSystem": TechniqueSystem_Click(sender, e); break;
                case "BusinessSystem": BusinessSystem_Click(sender, e); break;
                case "Timeline": Timeline_Click(sender, e); break;
                case "PlotManagement": PlotManagement_Click(sender, e); break;
                case "ContentManagement": ContentManagement_Click(sender, e); break;
                case "RelationshipNetwork": RelationshipNetwork_Click(sender, e); break;
                case "FactionManagement": FactionManagement_Click(sender, e); break;
                case "VolumeManagement": VolumeManagement_Click(sender, e); break;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开项目页面失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static ProjectNavMenuItem? FindProjectNavMenuItem(System.Windows.DependencyObject? start)
    {
        var current = start;
        while (current != null)
        {
            if (current is System.Windows.FrameworkElement element && element.DataContext is ProjectNavMenuItem menuItem)
            {
                return menuItem;
            }

            current = System.Windows.Media.VisualTreeHelper.GetParent(current)
                      ?? System.Windows.LogicalTreeHelper.GetParent(current);
        }

        return null;
    }

    private void SwitchToProject(ProjectNavMenuItem menuItem)
    {
        var currentId = _projectContextService?.CurrentProjectId;
        if (currentId == menuItem.ProjectId)
        {
            return;
        }

        _projectContextService?.SetCurrentProject(menuItem.ProjectId, menuItem.Name);
        UpdateCurrentProjectDisplay(menuItem.Name);

        // 女频文检测：类型/书名命中女频关键词时自动切换红粉花漾少女风皮肤，否则恢复用户所选皮肤
        ThemeManager.ApplyNovelGenre($"{menuItem.Type} {menuItem.Name}");

        // 刷新最近访问时间（不等待，避免阻塞导航）
        _ = _projectCatalogService?.TouchProjectAsync(menuItem.ProjectId);
    }

    private void UpdateCurrentProjectDisplay(string? projectName)
    {
        var display = string.IsNullOrWhiteSpace(projectName) ? "未选择" : projectName;
        if (CurrentProjectStatusText != null)
        {
            CurrentProjectStatusText.Text = $"当前项目: {display}";
        }

        if (DashboardProjectNameText != null)
        {
            DashboardProjectNameText.Text = display;
        }

        if (DashboardProjectSubtitleText != null && _projectContextService != null)
        {
            DashboardProjectSubtitleText.Text = _projectContextService.CurrentProjectId.HasValue ? "当前打开的项目" : "";
        }
    }

    /// <summary>
    /// 主题配置按钮：打开主题配置与皮肤设定窗口。
    /// </summary>
    private void ThemeSettings_Click(object sender, RoutedEventArgs e)
    {
        var window = new ThemeSettingsWindow { Owner = this };
        window.ShowDialog();
    }

    /// <summary>
    /// 一键生成书籍按钮：RWKV 自命名新书 + 双 Agent 生成大纲与第一章。
    /// </summary>
    private async void OneClickGenerate_Click(object sender, RoutedEventArgs e)
    {
        var generationService = App.ServiceProvider?.GetService<IOneClickNovelGenerationService>();
        if (generationService == null)
        {
            MessageBox.Show("一键生成服务未注册", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var progressWindow = new SimpleProgressDialog("一键生成书籍") { Owner = this };
        var progress = new Progress<string>(message => progressWindow.UpdateMessage(message));
        progressWindow.Show();

        try
        {
            var result = await generationService.GenerateAsync(progress);
            await RefreshProjectMenuAsync();
            progressWindow.Close();

            MessageBox.Show(
                result.IsSuccess ? result.Message : $"一键生成失败：{result.Message}",
                result.IsSuccess ? "一键生成完成" : "错误",
                MessageBoxButton.OK,
                result.IsSuccess ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            progressWindow.Close();
            MessageBox.Show($"一键生成失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 长篇批量生成按钮：启动整本书籍后台批量生成（3卷×30章×每章≥3000字，支持断点续跑）。
    /// </summary>
    private async void FullNovelBatchStart_Click(object sender, RoutedEventArgs e)
    {
        var batchService = App.ServiceProvider?.GetService<IFullNovelBatchGenerationService>();
        if (batchService == null)
        {
            MessageBox.Show("长篇批量生成服务未注册", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (batchService.IsRunning)
        {
            MessageBox.Show("批量生成任务已在运行中，可用「生成进度」按钮查看。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new BatchGenerationOptionsDialog { Owner = this };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var options = dialog.Options;
        var modeText = options.UnlimitedMode
            ? "无限续写模式"
            : $"{(options.NextThreeChaptersThenNewVolume ? "快速切卷（每卷 3 章后切新卷）" : "标准模式")}，每卷 {options.ChaptersPerVolume} 章";
        var confirm = MessageBox.Show(
            $"将启动长篇批量生成：{modeText} × 每章 ≥{options.ChapterTargetWords} 字。\n" +
            "采样采用 RWKV 官方创意参数 + DRY 抗复读采样，\n" +
            "章节正文使用“切片创作 + 拼接”工艺（16K 上下文限制）。\n\n" +
            "任务在后台运行，期间可正常使用软件其他功能。\n" +
            "若存在未完成的批量任务将自动从断点继续。\n\n确定开始？",
            "长篇批量生成", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var result = await batchService.StartAsync(options);
            if (result.Success)
            {
                MessageBox.Show(result.Message, "已启动", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(result.Message, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"启动批量生成失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 生成进度按钮：展示整本批量生成任务当前状态。
    /// </summary>
    private void FullNovelBatchStatus_Click(object sender, RoutedEventArgs e)
    {
        var batchService = App.ServiceProvider?.GetService<IFullNovelBatchGenerationService>();
        if (batchService == null)
        {
            MessageBox.Show("长篇批量生成服务未注册", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var s = batchService.GetStatus();
        var builder = new System.Text.StringBuilder();
        builder.AppendLine($"状态：{(s.IsRunning ? "运行中" : "未运行")}");
        builder.AppendLine($"阶段：{s.Phase}");
        if (!string.IsNullOrWhiteSpace(s.BookTitle))
        {
            builder.AppendLine($"书名：{s.BookTitle}");
        }

        builder.AppendLine($"规格：{s.VolumeCount} 卷 × {s.ChaptersPerVolume} 章");
        if (s.CompletedChapters > 0 || s.FailedChapters > 0 || s.CurrentChapter > 0)
        {
            builder.AppendLine($"位置：第 {Math.Max(s.CurrentVolume, 1)} 卷 第 {Math.Max(s.CurrentChapter, 1)} 章");
            builder.AppendLine($"已完成：{s.CompletedChapters} 章，失败：{s.FailedChapters} 章");
        }

        if (s.LastChapterScore.HasValue)
        {
            builder.AppendLine($"最近章节评分：{s.LastChapterScore.Value:F1}/10");
        }

        if (!string.IsNullOrWhiteSpace(s.RecentMessage))
        {
            builder.AppendLine();
            builder.AppendLine($"最近消息：{s.RecentMessage}");
        }

        if (!string.IsNullOrWhiteSpace(s.LastError))
        {
            builder.AppendLine($"最近错误：{s.LastError}");
        }

        if (s.StartedAt.HasValue)
        {
            builder.AppendLine();
            builder.AppendLine($"开始时间：{s.StartedAt:HH:mm:ss}，已运行 {(DateTime.Now - s.StartedAt.Value).TotalMinutes:F0} 分钟");
        }

        MessageBox.Show(builder.ToString(), "长篇批量生成进度", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <summary>
    /// 轻量进度对话框（代码构建，无 XAML）。
    /// </summary>
    private sealed class SimpleProgressDialog : Window
    {
        private readonly System.Windows.Controls.TextBlock _messageText;

        public SimpleProgressDialog(string title)
        {
            Title = title;
            Width = 460;
            SizeToContent = SizeToContent.Height;
            WindowStyle = WindowStyle.ToolWindow;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;

            _messageText = new System.Windows.Controls.TextBlock
            {
                Text = "准备中...",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(24, 18, 24, 12)
            };

            var progressBar = new System.Windows.Controls.ProgressBar
            {
                IsIndeterminate = true,
                Height = 14,
                Margin = new Thickness(24, 0, 24, 18)
            };

            Content = new StackPanel { Children = { _messageText, progressBar } };
        }

        public void UpdateMessage(string message)
        {
            _messageText.Text = message;
        }
    }

    private void OnNavigationStateChanged(object? sender, EventArgs e)
    {
        UpdateNavigationDisplay();
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        _navigationService?.GoBack();
    }

    private void UpdateNavigationDisplay()
    {
        if (_navigationService == null)
        {
            return;
        }

        if (BackButton != null)
        {
            BackButton.IsEnabled = _navigationService.CanGoBack;
        }

        if (CurrentLocationTextBlock != null)
        {
            var currentLabel = GetNavigationLabel(_navigationService.CurrentTarget) ?? "仪表盘";
            var source = _navigationService.CurrentContext?.Source;
            CurrentLocationTextBlock.Text = string.IsNullOrWhiteSpace(source)
                ? $"当前位置：{currentLabel}"
                : $"当前位置：{currentLabel} · 来源：{source}";
        }
    }

    private static string? GetNavigationLabel(NavigationTarget? target)
    {
        return target switch
        {
            NavigationTarget.ProjectManagement => "项目管理",
            NavigationTarget.ProjectOverview => "项目概览",
            NavigationTarget.VolumeManagement => "卷宗管理",
            NavigationTarget.CharacterManagement => "角色管理",
            NavigationTarget.Timeline => "时间线管理",
            NavigationTarget.RelationshipNetwork => "关系网络",
            NavigationTarget.FactionManagement => "势力管理",
            NavigationTarget.PlotManagement => "剧情管理",
            NavigationTarget.AICollaboration => "AI协作",
            NavigationTarget.AIConfiguration => "AI模型配置",
            NavigationTarget.ImportExport => "导入导出",
            NavigationTarget.WorldSettingManagement => "世界设定",
            NavigationTarget.DialogGeneration => "对话生成",
            _ => null
        };
    }

    /// <summary>
    /// 导航到指定目标页面。
    /// </summary>
    /// <param name="target">目标页面标识。</param>
    /// <param name="context">可选的导航上下文。</param>
    public void NavigateTo(NavigationTarget target, NavigationContext? context = null)
    {
        _navigationService?.NavigateTo(target, context);
    }

    private bool TryGetCurrentProjectId(string featureName, out Guid projectId)
    {
        var guard = App.ServiceProvider?.GetService<CurrentProjectGuard>();
        if (guard == null)
        {
            MessageBox.Show("项目校验服务未初始化", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
            projectId = Guid.Empty;
            return false;
        }

        return guard.TryGetCurrentProjectId(this, featureName, out projectId, ShowProjectManagement);
    }

    /// <summary>
    /// 显示项目概览
    /// </summary>
    public void ShowProjectOverview()
    {
        NavigateTo(NavigationTarget.ProjectOverview);
    }

    /// <summary>
    /// 显示项目管理
    /// </summary>
    public void ShowProjectManagement()
    {
        NavigateTo(NavigationTarget.ProjectManagement);
    }

    /// <summary>
    /// 显示卷宗管理
    /// </summary>
    public void ShowVolumeManagement()
    {
        NavigateTo(NavigationTarget.VolumeManagement);
    }

    /// <summary>
    /// 显示人物管理
    /// </summary>
    public void ShowCharacterManagement()
    {
        NavigateTo(NavigationTarget.CharacterManagement);
    }

    /// <summary>
    /// 显示关系网络
    /// </summary>
    public void ShowRelationshipNetwork()
    {
        NavigateTo(NavigationTarget.RelationshipNetwork);
    }

    /// <summary>
    /// 显示时间线管理
    /// </summary>
    public void ShowTimeline()
    {
        NavigateTo(NavigationTarget.Timeline);
    }

    /// <summary>
    /// 角色更新事件处理
    /// </summary>
    private async void OnCharacterUpdated(object? sender, CharacterUpdatedEventArgs e)
    {
        try
        {
            // 如果关系网络界面已创建，则更新其中的角色信息
            if (_relationshipNetworkView != null)
            {
                _relationshipNetworkView.UpdateCharacterInfo(e.CharacterId, e.UpdatedCharacter);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"更新关系网络中的角色信息失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 显示势力管理
    /// </summary>
    private void ShowFactionManagement()
    {
        try
        {
            NavigateTo(NavigationTarget.FactionManagement);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载势力管理界面失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 显示剧情管理
    /// </summary>
    public void ShowPlotManagement()
    {
        try
        {
            NavigateTo(NavigationTarget.PlotManagement);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载剧情管理界面失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 显示前置条件生成
    /// </summary>
    private void ShowPrerequisiteGeneration()
    {
        try
        {
            if (!TryGetCurrentProjectId("前置条件生成", out var projectId))
            {
                return;
            }

            var dialog = new PrerequisiteGenerationDialog(projectId);
            dialog.Owner = this;
            dialog.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"显示前置条件生成对话框失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 显示AI协作
    /// </summary>
    public void ShowAICollaboration()
    {
        try
        {
            NavigateTo(NavigationTarget.AICollaboration);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载AI协作界面失败：{ex.Message}\n\n详细信息：{ex.StackTrace}",
                "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 显示AI模型配置
    /// </summary>
    public void ShowAIConfiguration()
    {
        var window = new AIConfigurationHostWindow
        {
            Owner = this
        };
        window.Show();
    }

    /// <summary>
    /// 清理当前视图的资源
    /// </summary>
    private void CleanupCurrentView()
    {
        try
        {
            // 查找并清理AI协作视图
            foreach (UIElement child in MainContentArea.Children)
            {
                if (child is AICollaborationView aiView)
                {
                    aiView.Cleanup();
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            // 记录错误但不影响正常流程
            System.Diagnostics.Debug.WriteLine($"清理视图资源时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 显示对话生成器
    /// </summary>
    private void ShowDialogGeneration()
    {
        try
        {
            NavigateTo(NavigationTarget.DialogGeneration);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载对话生成器界面失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 显示导入导出
    /// </summary>
    public void ShowImportExport()
    {
        if (!TryGetCurrentProjectId("导入导出", out _))
        {
            return;
        }

        NavigateTo(NavigationTarget.ImportExport);
    }

    /// <summary>
    /// 显示世界设定管理
    /// </summary>
    public void ShowWorldSettingManagement()
    {
        NavigateTo(NavigationTarget.WorldSettingManagement);
    }

    /// <summary>
    /// 显示职业体系
    /// </summary>
    private void ShowProfessionSystem()
    {
        // 清空主内容区域
        MainContentArea.Children.Clear();

        // 添加职业体系视图
        var professionSystemView = new ProfessionSystemView();
        MainContentArea.Children.Add(professionSystemView);

        // 更新窗口标题
        this.Title = "书籍管理系统 - 职业体系管理";
    }

    /// <summary>
    /// 显示司法体系
    /// </summary>
    private void ShowJudicialSystem()
    {
        // 清空主内容区域
        MainContentArea.Children.Clear();

        // 添加司法体系视图
        var judicialSystemView = new JudicialSystemView();
        MainContentArea.Children.Add(judicialSystemView);

        // 更新窗口标题
        this.Title = "书籍管理系统 - 司法体系管理";
    }

    /// <summary>
    /// 显示生民体系
    /// </summary>
    private void ShowPopulationSystem()
    {
        // 清空主内容区域
        MainContentArea.Children.Clear();

        // 添加生民体系视图
        var populationSystemView = new PopulationSystemView();
        MainContentArea.Children.Add(populationSystemView);

        // 更新窗口标题
        this.Title = "书籍管理系统 - 生民体系管理";
    }

    /// <summary>
    /// 显示修炼体系
    /// </summary>
    private void ShowCultivationSystem()
    {
        // 清空主内容区域
        MainContentArea.Children.Clear();

        // 添加修炼体系视图
        var cultivationSystemView = new CultivationSystemView();
        MainContentArea.Children.Add(cultivationSystemView);

        // 更新窗口标题
        this.Title = "书籍管理系统 - 修炼体系管理";
    }

    /// <summary>
    /// 显示政治体系
    /// </summary>
    private void ShowPoliticalSystem()
    {
        // 清空主内容区域
        MainContentArea.Children.Clear();

        // 添加政治体系视图
        var politicalSystemView = new PoliticalSystemView();
        MainContentArea.Children.Add(politicalSystemView);

        // 更新窗口标题
        this.Title = "书籍管理系统 - 政治体系管理";
    }

    /// <summary>
    /// 显示灵宝体系
    /// </summary>
    private void ShowTreasureSystem()
    {
        // 清空主内容区域
        MainContentArea.Children.Clear();

        // 添加灵宝体系视图
        var treasureSystemView = new TreasureSystemView();
        MainContentArea.Children.Add(treasureSystemView);

        // 更新窗口标题
        this.Title = "书籍管理系统 - 灵宝体系管理";
    }

    /// <summary>
    /// 显示维度结构
    /// </summary>
    private void ShowDimensionStructure()
    {
        // 清空主内容区域
        MainContentArea.Children.Clear();

        // 添加维度结构视图
        var dimensionStructureView = new DimensionStructureView();
        MainContentArea.Children.Add(dimensionStructureView);

        // 更新窗口标题
        this.Title = "书籍管理系统 - 维度结构管理";
    }

    /// <summary>
    /// 显示地图结构
    /// </summary>
    private void ShowMapStructure()
    {
        // 清空主内容区域
        MainContentArea.Children.Clear();

        // 添加地图结构视图
        var mapStructureView = new MapStructureView();
        MainContentArea.Children.Add(mapStructureView);

        // 更新窗口标题
        this.Title = "书籍管理系统 - 地图结构管理";
    }

    /// <summary>
    /// 显示宠物体系
    /// </summary>
    private void ShowPetSystem()
    {
        // 清空主内容区域
        MainContentArea.Children.Clear();

        // 添加宠物体系视图
        var petSystemView = new PetSystemView();
        MainContentArea.Children.Add(petSystemView);

        // 更新窗口标题
        this.Title = "书籍管理系统 - 宠物体系管理";
    }

    #endregion
}

/// <summary>
/// 左侧导航项目菜单项。
/// </summary>
public class ProjectNavMenuItem : System.ComponentModel.INotifyPropertyChanged
{
    private bool _isExpanded;

    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>项目类型（用于女频文皮肤自动检测）。</summary>
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>项目组展开状态（持久化到 sidebar_state.json，重启恢复）。</summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value) return;
            _isExpanded = value;
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsExpanded)));
        }
    }

    /// <summary>「设定管理」嵌套组展开状态。</summary>
    public bool SettingsGroupExpanded { get => _settingsGroupExpanded; set { if (_settingsGroupExpanded == value) return; _settingsGroupExpanded = value; PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(SettingsGroupExpanded))); } }
    private bool _settingsGroupExpanded;

    /// <summary>「人物管理」嵌套组展开状态。</summary>
    public bool PeopleGroupExpanded { get => _peopleGroupExpanded; set { if (_peopleGroupExpanded == value) return; _peopleGroupExpanded = value; PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(PeopleGroupExpanded))); } }
    private bool _peopleGroupExpanded;

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
}

/// <summary>
/// 仪表盘最近活动条目（真实项目更新动态）。
/// </summary>
public class RecentActivityItem
{
    public PackIconKind Icon { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string RelativeTime => FormatRelativeTime(Timestamp);

    private static string FormatRelativeTime(DateTime time)
    {
        var delta = DateTime.Now - time;
        if (delta.TotalMinutes < 1)
        {
            return "刚刚";
        }

        if (delta.TotalMinutes < 60)
        {
            return $"{(int)delta.TotalMinutes}分钟前";
        }

        if (delta.TotalHours < 24)
        {
            return $"{(int)delta.TotalHours}小时前";
        }

        if (delta.TotalDays < 30)
        {
            return $"{(int)delta.TotalDays}天前";
        }

        return time.ToString("MM-dd HH:mm");
    }
}
