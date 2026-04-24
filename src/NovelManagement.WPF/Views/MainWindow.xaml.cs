using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using NovelManagement.WPF.Events;
using NovelManagement.WPF.Services;

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

    #endregion

    /// <summary>
    /// 构造函数
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        _navigationService = App.ServiceProvider?.GetService<NavigationService>();
        _projectContextService = App.ServiceProvider?.GetService<ProjectContextService>();
        _navigationService?.Configure(CreateNavigationRequest, RenderNavigationView);
        if (_navigationService != null)
        {
            _navigationService.NavigationStateChanged += OnNavigationStateChanged;
        }
        if (_projectContextService != null)
        {
            _projectContextService.ProjectChanged += OnProjectChanged;
        }

        // 订阅窗口关闭事件
        this.Closing += MainWindow_Closing;
        UpdateNavigationDisplay();
    }

    /// <summary>
    /// 窗口关闭事件处理
    /// </summary>
    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        try
        {
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

                MessageBox.Show($"项目 '{dialog.ProjectData.Name}' 创建成功！", "创建完成",
                    MessageBoxButton.OK, MessageBoxImage.Information);
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
                // 模拟导入过程
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
            // 创建一个新窗口来显示世界设定管理
            var window = new Window
            {
                Title = "设定管理",
                Width = 1200,
                Height = 800,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Content = new WorldSettingManagementView()
            };
            window.Show();
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
        try
        {
            var result = MessageBox.Show("开始进行项目质量检查？\n这将检查角色一致性、剧情逻辑等问题。",
                "质量检查", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // 模拟质量检查过程
                MessageBox.Show("质量检查完成！\n发现问题：\n- 角色张三在第3章和第5章的描述不一致\n- 时间线存在逻辑错误\n\n详细报告已生成。",
                    "检查结果", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"质量检查失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 一致性检查按钮点击事件
    /// </summary>
    private void ConsistencyCheck_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var result = MessageBox.Show("开始进行一致性检查？\n这将检查世界观、角色设定、剧情连贯性等。",
                "一致性检查", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // 模拟一致性检查过程
                MessageBox.Show("一致性检查完成！\n检查结果：\n✓ 世界观设定一致\n✓ 角色性格连贯\n⚠ 发现2处时间线冲突\n\n建议修复时间线问题。",
                    "检查结果", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"一致性检查失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
            this.Title = "小说管理系统 - 装备体系管理";
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
            this.Title = "小说管理系统 - 功法体系管理";
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
            this.Title = "小说管理系统 - 商业体系管理";
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
            // 清空主内容区域
            MainContentArea.Children.Clear();

            // 添加时间线视图
            var timelineView = new TimelineView();
            MainContentArea.Children.Add(timelineView);

            // 更新窗口标题
            this.Title = "小说管理系统 - 时间线管理";
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
        MessageBox.Show("发布管理功能正在开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
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
                Title = "小说管理系统 - 项目管理"
            },
            NavigationTarget.ProjectOverview => new NavigationViewRequest
            {
                View = new ProjectOverviewView(),
                Title = "小说管理系统 - 项目概览"
            },
            NavigationTarget.VolumeManagement => new NavigationViewRequest
            {
                View = new VolumeManagementView(),
                Title = "小说管理系统 - 卷宗管理"
            },
            NavigationTarget.CharacterManagement => new NavigationViewRequest
            {
                View = GetOrCreateCharacterManagementView(),
                Title = "小说管理系统 - 角色管理"
            },
            NavigationTarget.RelationshipNetwork => new NavigationViewRequest
            {
                View = GetOrCreateRelationshipNetworkView(),
                Title = "小说管理系统 - 关系网络"
            },
            NavigationTarget.FactionManagement => new NavigationViewRequest
            {
                View = new FactionManagementView(),
                Title = "小说管理系统 - 势力管理"
            },
            NavigationTarget.PlotManagement => new NavigationViewRequest
            {
                View = new PlotManagementView(),
                Title = "小说管理系统 - 剧情管理"
            },
            NavigationTarget.AICollaboration => new NavigationViewRequest
            {
                View = new AICollaborationView(),
                Title = "小说管理系统 - AI协作创作"
            },
            NavigationTarget.ImportExport => new NavigationViewRequest
            {
                View = new ImportExportView(),
                Title = "小说管理系统 - 导入导出管理"
            },
            NavigationTarget.WorldSettingManagement => new NavigationViewRequest
            {
                View = new WorldSettingManagementView(),
                Title = "小说管理系统 - 世界设定管理"
            },
            NavigationTarget.DialogGeneration => new NavigationViewRequest
            {
                View = new DialogGenerationView(),
                Title = "小说管理系统 - AI对话生成器"
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
        if (_navigationService != null)
        {
            await _navigationService.RefreshCurrentViewAsync(e.NewProjectId, e.NewProjectName);
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
            NavigationTarget.RelationshipNetwork => "关系网络",
            NavigationTarget.FactionManagement => "势力管理",
            NavigationTarget.PlotManagement => "剧情管理",
            NavigationTarget.AICollaboration => "AI协作",
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
        this.Title = "小说管理系统 - 职业体系管理";
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
        this.Title = "小说管理系统 - 司法体系管理";
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
        this.Title = "小说管理系统 - 生民体系管理";
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
        this.Title = "小说管理系统 - 修炼体系管理";
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
        this.Title = "小说管理系统 - 政治体系管理";
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
        this.Title = "小说管理系统 - 灵宝体系管理";
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
        this.Title = "小说管理系统 - 维度结构管理";
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
        this.Title = "小说管理系统 - 地图结构管理";
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
        this.Title = "小说管理系统 - 宠物体系管理";
    }

    #endregion
}
