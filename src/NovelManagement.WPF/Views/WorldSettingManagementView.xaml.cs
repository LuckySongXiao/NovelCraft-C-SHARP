using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NovelManagement.Application.DTOs;
using NovelManagement.Application.Interfaces;
using NovelManagement.WPF.Localization;
using NovelManagement.WPF.Services;
using static NovelManagement.WPF.Localization.LocalizationManager;

namespace NovelManagement.WPF.Views;

/// <summary>
/// 世界设定管理视图
/// </summary>
public partial class WorldSettingManagementView : UserControl, INavigationRefreshableView, INavigationAwareView
{
    private readonly IWorldSettingService _worldSettingService;
    private readonly ILogger<WorldSettingManagementView>? _logger;
    private readonly WorldSettingAnalysisService _analysisService;
    private readonly ProjectContextService? _projectContextService;
    private readonly ProjectReadModelService? _projectReadModelService;
    private readonly IAIAssistantService? _aiAssistantService;
    private ObservableCollection<WorldSettingDto> _worldSettings;
    private Guid _currentProjectId;
    private NavigationContext? _navigationContext;

    /// <summary>
    /// 初始化世界设定管理视图。
    /// </summary>
    public WorldSettingManagementView()
    {
        InitializeComponent();
        _worldSettings = new ObservableCollection<WorldSettingDto>();
        _currentProjectId = Guid.Empty;

        try
        {
            _worldSettingService = (App.ServiceProvider?.GetService(typeof(IWorldSettingService)) as IWorldSettingService)!;
            _projectContextService = App.ServiceProvider?.GetService(typeof(ProjectContextService)) as ProjectContextService;
            _projectReadModelService = App.ServiceProvider?.GetService<ProjectReadModelService>();
            _aiAssistantService = App.ServiceProvider?.GetService<IAIAssistantService>();
            _logger = App.ServiceProvider?.GetService(typeof(ILogger<WorldSettingManagementView>)) as ILogger<WorldSettingManagementView>;
            var analysisLogger = App.ServiceProvider?.GetService(typeof(ILogger<WorldSettingAnalysisService>)) as ILogger<WorldSettingAnalysisService>;
            var rwkvService = App.ServiceProvider?.GetService(typeof(NovelManagement.AI.Services.RWKV.IRwkvLightningService)) as NovelManagement.AI.Services.RWKV.IRwkvLightningService;
            _analysisService = new WorldSettingAnalysisService(analysisLogger, rwkvService);
        }
        catch (Exception ex)
        {
            _worldSettingService = null!;
            _projectContextService = null;
            _projectReadModelService = null;
            _aiAssistantService = null;
            _analysisService = new WorldSettingAnalysisService();
            MessageBox.Show(LocalizationManager.TF("WS.AIInitFailed", "AI分析服务初始化失败，使用默认服务: {0}", ex.Message), LocalizationManager.T("Msg.Warning", "警告"), MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        _ = RefreshOnProjectChangedAsync(_projectContextService?.CurrentProjectId, null);
    }

    /// <summary>
    /// 加载示例数据（用于演示）
    /// </summary>
    private void LoadSampleData()
    {
        _worldSettings.Clear();
        var sampleSettings = new List<WorldSettingDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "修炼体系",
                Type = "体系",
                Category = "修炼",
                Description = "整个世界的修炼体系设定",
                Content = "包含各种修炼境界、功法、丹药等设定",
                Importance = 10,
                CreatedAt = DateTime.Now.AddDays(-30),
                UpdatedAt = DateTime.Now.AddDays(-1),
                Children = new List<WorldSettingDto>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Name = "境界划分",
                        Type = "规则",
                        Category = "修炼",
                        Description = "修炼境界的详细划分",
                        Content = "练气、筑基、金丹、元婴、化神、炼虚、合体、大乘、渡劫",
                        Importance = 9,
                        CreatedAt = DateTime.Now.AddDays(-25),
                        UpdatedAt = DateTime.Now.AddDays(-2)
                    }
                }
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "地理设定",
                Type = "世界观",
                Category = "地理",
                Description = "世界的地理环境设定",
                Content = "包含各大陆、海洋、山脉、城市等地理信息",
                Importance = 8,
                CreatedAt = DateTime.Now.AddDays(-20),
                UpdatedAt = DateTime.Now.AddDays(-3),
                Children = new List<WorldSettingDto>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Name = "东玄大陆",
                        Type = "地点",
                        Category = "地理",
                        Description = "主要故事发生的大陆",
                        Content = "东玄大陆是修炼者聚集的主要大陆，分为东、南、西、北四域",
                        Importance = 7,
                        CreatedAt = DateTime.Now.AddDays(-18),
                        UpdatedAt = DateTime.Now.AddDays(-4)
                    }
                }
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "势力体系",
                Type = "组织",
                Category = "政治",
                Description = "世界中的各大势力组织",
                Content = "包含宗门、家族、商会、帝国等各种势力组织",
                Importance = 8,
                CreatedAt = DateTime.Now.AddDays(-15),
                UpdatedAt = DateTime.Now.AddDays(-5)
            }
        };

        foreach (var setting in sampleSettings)
        {
            _worldSettings.Add(setting);
        }

        WorldSettingsTreeView.ItemsSource = _worldSettings;
        LoadFilterData();
    }

    private async Task LoadWorldSettingsAsync()
    {
        try
        {
            _worldSettings.Clear();

            if (_currentProjectId == Guid.Empty)
            {
                WorldSettingsTreeView.ItemsSource = _worldSettings;
                LoadFilterData();
                DetailsPanel.Children.Clear();
                return;
            }

            if (_worldSettingService == null)
            {
                WorldSettingsTreeView.ItemsSource = _worldSettings;
                LoadFilterData();
                DetailsPanel.Children.Clear();
                return;
            }

            var settings = (await _worldSettingService.GetAllAsync(_currentProjectId)).ToList();
            foreach (var setting in settings)
            {
                _worldSettings.Add(setting);
            }

            WorldSettingsTreeView.ItemsSource = _worldSettings;
            LoadFilterData();
            ApplyNavigationContext();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "加载世界设定失败");
            WorldSettingsTreeView.ItemsSource = _worldSettings;
            LoadFilterData();
            DetailsPanel.Children.Clear();
            MessageBox.Show(LocalizationManager.TF("WS.LoadFailed", "加载世界设定失败：{0}", ex.Message), LocalizationManager.T("Msg.Error", "错误"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 加载筛选器数据
    /// </summary>
    private void LoadFilterData()
    {
        var types = _worldSettings.Select(ws => ws.Type).Distinct().ToList();
        types.Insert(0, LocalizationManager.T("WS.AllTypes", "全部类型"));
        TypeFilterComboBox.ItemsSource = types;
        TypeFilterComboBox.SelectedIndex = 0;

        var categories = _worldSettings.Where(ws => !string.IsNullOrEmpty(ws.Category))
            .Select(ws => ws.Category!)
            .Distinct()
            .ToList();
        categories.Insert(0, LocalizationManager.T("WS.AllCategories", "全部分类"));
        CategoryFilterComboBox.ItemsSource = categories;
        CategoryFilterComboBox.SelectedIndex = 0;
    }

    /// <summary>
    /// 新建设定按钮点击事件
    /// </summary>
    private async void AddWorldSetting_Click(object sender, RoutedEventArgs e)
    {
        if (_currentProjectId == Guid.Empty)
        {
            MessageBox.Show(LocalizationManager.T("WS.SelectProjectFirst", "请先选择项目后再创建世界设定。"), LocalizationManager.T("Msg.Tip", "提示"), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_worldSettingService == null)
        {
            MessageBox.Show(LocalizationManager.T("WS.ServiceNotInit", "世界设定服务未初始化。"), LocalizationManager.T("Msg.Error", "错误"), MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var selectedSetting = WorldSettingsTreeView.SelectedItem as WorldSettingDto;
        var dialog = new WorldSettingEditDialog(_currentProjectId, selectedSetting);
        dialog.Owner = Window.GetWindow(this);
        if (dialog.ShowDialog() == true)
        {
            var createDto = dialog.BuildCreateDto(_currentProjectId, selectedSetting?.Id);
            var createdSetting = await _worldSettingService.CreateAsync(createDto);
            await LoadWorldSettingsAsync();
            FocusSetting(createdSetting.Id);
        }
    }

    private void ImportWorldSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow == null)
            {
                MessageBox.Show(LocalizationManager.T("WS.MainWindowUnavailable", "无法获取主窗口，无法打开导入导出页面。"), LocalizationManager.T("Msg.Error", "错误"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            mainWindow.NavigateTo(NavigationTarget.ImportExport, new NavigationContext
            {
                ProjectId = _currentProjectId == Guid.Empty ? null : _currentProjectId,
                Source = "WorldSettingManagement.Import",
                Payload = new ImportExportNavigationPayload
                {
                    Action = "SettingsImport"
                }
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "打开设定导入页面失败");
            MessageBox.Show(LocalizationManager.TF("WS.OpenImportFailed", "打开设定导入页面失败：{0}", ex.Message), LocalizationManager.T("Msg.Error", "错误"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// AI分析按钮点击事件
    /// </summary>
    private async void AIAnalysis_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var selectedSetting = WorldSettingsTreeView.SelectedItem as WorldSettingDto;
            if (selectedSetting == null)
            {
                MessageBox.Show(LocalizationManager.T("WS.SelectForAnalysis", "请先选择一个世界设定进行分析"), LocalizationManager.T("Msg.Tip", "提示"), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var loadingDialog = new ProgressDialog(LocalizationManager.T("WS.AIAnalyzing", "AI分析中"), LocalizationManager.T("WS.AnalyzingProgress", "正在分析世界设定的一致性和完整性..."));
            loadingDialog.Show();

            try
            {
                var analysisResult = await _analysisService.AnalyzeWorldSettingAsync(selectedSetting, _worldSettings.ToList());
                loadingDialog.Close();
                ShowAnalysisResult(analysisResult);
            }
            catch (Exception ex)
            {
                loadingDialog.Close();
                MessageBox.Show(LocalizationManager.TF("WS.AIAnalysisFailed", "AI分析失败: {0}", ex.Message), LocalizationManager.T("Msg.Error", "错误"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "AI分析过程中发生错误");
            MessageBox.Show(LocalizationManager.TF("WS.AnalysisError", "分析过程中发生错误: {0}", ex.Message), LocalizationManager.T("Msg.Error", "错误"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 刷新按钮点击事件
    /// </summary>
    private async void RefreshWorldSettings_Click(object sender, RoutedEventArgs e)
    {
        await LoadWorldSettingsAsync();
        MessageBox.Show(LocalizationManager.T("WS.DataRefreshed", "数据已刷新"), LocalizationManager.T("Msg.Tip", "提示"), MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <summary>
    /// 导出设定按钮点击事件
    /// </summary>
    private void ExportWorldSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow == null)
            {
                MessageBox.Show(LocalizationManager.T("WS.MainWindowUnavailable", "无法获取主窗口，无法打开导入导出页面。"), LocalizationManager.T("Msg.Error", "错误"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var selectedSetting = WorldSettingsTreeView.SelectedItem as WorldSettingDto;
            mainWindow.NavigateTo(NavigationTarget.ImportExport, new NavigationContext
            {
                ProjectId = _currentProjectId == Guid.Empty ? null : _currentProjectId,
                Source = "WorldSettingManagement.ExportCurrent",
                Payload = new ImportExportNavigationPayload
                {
                    Action = "SettingsOnlyExport",
                    SettingId = selectedSetting?.Id,
                    SettingName = selectedSetting?.Name
                }
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "打开设定导出页面失败");
            MessageBox.Show(LocalizationManager.TF("WS.OpenExportFailed", "打开设定导出页面失败：{0}", ex.Message), LocalizationManager.T("Msg.Error", "错误"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// AI助手按钮点击事件
    /// </summary>
    private async void AIAssistant_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_currentProjectId == Guid.Empty)
            {
                MessageBox.Show(LocalizationManager.T("WS.SelectProjectForAI", "请先选择项目后再使用AI助手。"), LocalizationManager.T("Msg.Tip", "提示"), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_aiAssistantService == null || _worldSettingService == null)
            {
                ShowAIAssistantDialog();
                return;
            }

            var selectedSetting = WorldSettingsTreeView.SelectedItem as WorldSettingDto;
            if (selectedSetting != null)
            {
                var choice = MessageBox.Show(
                    LocalizationManager.T("WS.AIChoiceWithSetting", "是：AI优化当前设定并保存\n否：基于当前设定生成新的子设定并保存\n取消：打开原始AI助手"),
                    LocalizationManager.T("WS.AIDialogTitle", "AI世界设定"),
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (choice == MessageBoxResult.Cancel)
                {
                    ShowAIAssistantDialog();
                    return;
                }

                await GenerateWorldSettingWithAiAsync(choice == MessageBoxResult.Yes, selectedSetting);
                return;
            }

            var generateChoice = MessageBox.Show(
                LocalizationManager.T("WS.AIChoiceNoSetting", "是：AI生成新的设定并保存\n否：打开原始AI助手"),
                LocalizationManager.T("WS.AIDialogTitle", "AI世界设定"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (generateChoice == MessageBoxResult.Yes)
            {
                await GenerateWorldSettingWithAiAsync(false, null);
            }
            else
            {
                ShowAIAssistantDialog();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "启动AI助手失败");
            MessageBox.Show(LocalizationManager.TF("WS.StartAIFailed", "启动AI助手失败：{0}", ex.Message), LocalizationManager.T("Msg.Error", "错误"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 显示AI助手对话框
    /// </summary>
    private void ShowAIAssistantDialog()
    {
        var dialog = new AIAssistantDialog(LocalizationManager.T("World.Title", "世界设定管理"), GetCurrentContext());
        dialog.Owner = Window.GetWindow(this);
        dialog.ShowDialog();
    }

    /// <summary>
    /// 获取当前上下文信息
    /// </summary>
    private string GetCurrentContext()
    {
        var context = new StringBuilder();
        context.AppendLine("当前功能：世界设定管理");
        context.AppendLine($"设定总数：{_worldSettings.Count}");

        var selectedSetting = WorldSettingsTreeView.SelectedItem as WorldSettingDto;
        if (selectedSetting != null)
        {
            var chain = GetAncestorChain(selectedSetting);
            if (chain.Count > 0)
            {
                context.AppendLine($"上级路径：{string.Join(" -> ", chain.Select(item => item.Name))}");
            }

            context.AppendLine($"当前选中：{selectedSetting.Name}");
            context.AppendLine($"设定类型：{selectedSetting.Type}");
            context.AppendLine($"设定分类：{selectedSetting.Category}");
            context.AppendLine($"重要性：{selectedSetting.Importance}/10");
            context.AppendLine($"描述：{selectedSetting.Description}");
        }

        context.AppendLine();
        context.AppendLine("可用操作：");
        context.AppendLine("- 创建新的世界设定");
        context.AppendLine("- 生成与当前主题一致的子设定");
        context.AppendLine("- 分析设定合理性");
        context.AppendLine("- 检查设定一致性");
        context.AppendLine("- 优化设定内容");

        return context.ToString();
    }

    /// <summary>
    /// 搜索文本框文本变化事件
    /// </summary>
    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        FilterWorldSettings();
    }

    /// <summary>
    /// 类型筛选器选择变化事件
    /// </summary>
    private void TypeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        FilterWorldSettings();
    }

    /// <summary>
    /// 分类筛选器选择变化事件
    /// </summary>
    private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        FilterWorldSettings();
    }

    /// <summary>
    /// 筛选世界设定
    /// </summary>
    private void FilterWorldSettings()
    {
        var searchText = SearchTextBox?.Text?.ToLower() ?? string.Empty;
        var selectedType = TypeFilterComboBox?.SelectedItem?.ToString();
        var selectedCategory = CategoryFilterComboBox?.SelectedItem?.ToString();

        var filteredSettings = _worldSettings.Where(ws =>
        {
            var matchesSearch = string.IsNullOrEmpty(searchText) ||
                ws.Name.ToLower().Contains(searchText) ||
                (ws.Description?.ToLower().Contains(searchText) ?? false);

            var matchesType = selectedType == LocalizationManager.T("WS.AllTypes", "全部类型") || ws.Type == selectedType;
            var matchesCategory = selectedCategory == LocalizationManager.T("WS.AllCategories", "全部分类") || ws.Category == selectedCategory;

            return matchesSearch && matchesType && matchesCategory;
        }).ToList();

        WorldSettingsTreeView.ItemsSource = filteredSettings;
    }

    /// <summary>
    /// 树视图选择项变化事件
    /// </summary>
    private void WorldSettingsTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is WorldSettingDto selectedSetting)
        {
            ShowSettingDetails(selectedSetting);
        }
    }

    /// <summary>
    /// 显示设定详情
    /// </summary>
    private void ShowSettingDetails(WorldSettingDto setting)
    {
        DetailsPanel.Children.Clear();

        var titleBlock = new TextBlock
        {
            Text = setting.Name,
            Style = (Style)FindResource("MaterialDesignHeadline5TextBlock"),
            Margin = new Thickness(0, 0, 0, 20)
        };
        DetailsPanel.Children.Add(titleBlock);

        AddDetailItem(LocalizationManager.T("Dlg.Type", "类型"), setting.Type);
        AddDetailItem(LocalizationManager.T("Dlg.Category", "分类"), setting.Category ?? LocalizationManager.T("WS.None", "无"));
        AddDetailItem(LocalizationManager.T("WS.Importance", "重要性"), $"{setting.Importance}/10");
        AddDetailItem(LocalizationManager.T("WS.CreatedAt", "创建时间"), setting.CreatedAt.ToString("yyyy-MM-dd HH:mm"));
        AddDetailItem(LocalizationManager.T("WS.UpdatedAt", "更新时间"), setting.UpdatedAt.ToString("yyyy-MM-dd HH:mm"));

        if (!string.IsNullOrEmpty(setting.Description))
        {
            AddDetailSection(LocalizationManager.T("Dlg.Description", "描述"), setting.Description);
        }

        if (!string.IsNullOrEmpty(setting.Content))
        {
            AddDetailSection(LocalizationManager.T("WS.Content", "详细内容"), setting.Content);
        }

        if (!string.IsNullOrEmpty(setting.Rules))
        {
            AddDetailSection(LocalizationManager.T("WS.Rules", "相关规则"), setting.Rules);
        }

        if (!string.IsNullOrEmpty(setting.History))
        {
            AddDetailSection(LocalizationManager.T("WS.History", "历史背景"), setting.History);
        }
    }

    /// <summary>
    /// 添加详情项
    /// </summary>
    private void AddDetailItem(string label, string value)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 5, 0, 5)
        };

        var labelBlock = new TextBlock
        {
            Text = $"{label}：",
            FontWeight = FontWeights.Bold,
            Width = 80,
            VerticalAlignment = VerticalAlignment.Top
        };

        var valueBlock = new TextBlock
        {
            Text = value,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Top
        };

        panel.Children.Add(labelBlock);
        panel.Children.Add(valueBlock);
        DetailsPanel.Children.Add(panel);
    }

    /// <summary>
    /// 添加详情段落
    /// </summary>
    private void AddDetailSection(string title, string content)
    {
        var titleBlock = new TextBlock
        {
            Text = title,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 15, 0, 5),
            Style = (Style)FindResource("MaterialDesignSubtitle1TextBlock")
        };

        var contentBlock = new TextBlock
        {
            Text = content,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 10),
            LineHeight = 20
        };

        DetailsPanel.Children.Add(titleBlock);
        DetailsPanel.Children.Add(contentBlock);
    }

    /// <summary>
    /// 显示AI分析结果
    /// </summary>
    private void ShowAnalysisResult(WorldSettingAnalysisService.AnalysisResult result)
    {
        var dialog = new AnalysisResultDialog(result);
        dialog.ShowDialog();
    }

    /// <summary>
    /// 在项目切换后刷新世界设定数据。
    /// </summary>
    /// <param name="projectId">当前项目标识。</param>
    /// <param name="projectName">当前项目名称。</param>
    public async Task RefreshOnProjectChangedAsync(Guid? projectId, string? projectName)
    {
        _currentProjectId = projectId ?? Guid.Empty;
        await LoadWorldSettingsAsync();
    }

    /// <summary>
    /// 在导航到当前视图时应用导航上下文。
    /// </summary>
    /// <param name="context">导航上下文。</param>
    public void OnNavigatedTo(NavigationContext context)
    {
        _navigationContext = context;
        ApplyNavigationContext();
    }

    private void ApplyNavigationContext()
    {
        if (_navigationContext == null || _worldSettings.Count == 0)
        {
            return;
        }

        var payload = _navigationContext.Payload as WorldSettingNavigationPayload;
        if (payload?.SettingId != null && FocusSetting(payload.SettingId.Value))
        {
            return;
        }

        if (payload?.Action == "WorldSettingsHome" || _navigationContext.Source == "ProjectOverview.WorldSettings")
        {
            ShowSettingDetails(_worldSettings[0]);
        }
    }

    private WorldSettingDto? FindSettingById(IEnumerable<WorldSettingDto> settings, Guid settingId)
    {
        foreach (var setting in settings)
        {
            if (setting.Id == settingId)
            {
                return setting;
            }

            var childMatch = FindSettingById(setting.Children, settingId);
            if (childMatch != null)
            {
                return childMatch;
            }
        }

        return null;
    }

    private bool FocusSetting(Guid settingId)
    {
        var matchedSetting = FindSettingById(_worldSettings.ToList(), settingId);
        if (matchedSetting == null)
        {
            return false;
        }

        WorldSettingsTreeView.SelectedItemChanged -= WorldSettingsTreeView_SelectedItemChanged;
        WorldSettingsTreeView.SelectedItemChanged += WorldSettingsTreeView_SelectedItemChanged;
        ShowSettingDetails(matchedSetting);
        return true;
    }

    private async Task GenerateWorldSettingWithAiAsync(bool optimizeCurrent, WorldSettingDto? baseSetting)
    {
        var promptContext = await BuildWorldSettingPromptContextAsync(baseSetting, optimizeCurrent);
        var targetParentId = !optimizeCurrent && baseSetting != null ? baseSetting.Id : (Guid?)null;
        var parameters = new Dictionary<string, object>
        {
            ["title"] = optimizeCurrent && baseSetting != null
                ? $"优化世界设定：{baseSetting.Name}"
                : baseSetting != null
                    ? $"生成子设定：{baseSetting.Name}"
                    : "生成世界设定",
            ["theme"] = optimizeCurrent && baseSetting != null
                ? $"请优化世界设定“{baseSetting.Name}”，保持主题一致并补足缺失字段。"
                : baseSetting != null
                    ? $"请围绕父级设定“{baseSetting.Name}”生成一个主题一致的子设定。"
                    : "请生成一个适合当前书籍项目使用的世界设定。",
            ["requirements"] = BuildWorldSettingRequirements(baseSetting, optimizeCurrent, promptContext),
            ["context"] = promptContext
        };

        var result = await _aiAssistantService!.GenerateOutlineAsync(parameters);
        if (!result.IsSuccess || result.Data == null)
        {
            MessageBox.Show(result.Message ?? LocalizationManager.T("WS.AIGenerateFailed", "AI生成失败。"), LocalizationManager.T("WS.AIDialogTitle", "AI世界设定"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (optimizeCurrent && baseSetting != null)
        {
            var updateDto = BuildUpdateDtoFromAiResult(result.Data, baseSetting);
            var updatedSetting = await _worldSettingService.UpdateAsync(updateDto);
            await LoadWorldSettingsAsync();
            FocusSetting(updatedSetting.Id);
            MessageBox.Show(LocalizationManager.TF("WS.AIOptimizedSaved", "已使用 AI 优化并保存设定：{0}", updatedSetting.Name), LocalizationManager.T("WS.AIDialogTitle", "AI世界设定"), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var createDto = BuildCreateDtoFromAiResult(result.Data, targetParentId);
        createDto.ProjectId = _currentProjectId;
        var created = await _worldSettingService.CreateAsync(createDto);
        await LoadWorldSettingsAsync();
        FocusSetting(created.Id);
        var successText = targetParentId == null
            ? LocalizationManager.TF("WS.AIGeneratedSaved", "已使用 AI 生成并保存设定：{0}", created.Name)
            : LocalizationManager.TF("WS.AIGeneratedChildSaved", "已使用 AI 生成并保存子设定：{0}", created.Name);
        MessageBox.Show(successText, LocalizationManager.T("WS.AIDialogTitle", "AI世界设定"), MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async Task<string> BuildWorldSettingPromptContextAsync(WorldSettingDto? baseSetting, bool optimizeCurrent)
    {
        var builder = new StringBuilder();

        if (_projectReadModelService != null && _currentProjectId != Guid.Empty)
        {
            try
            {
                var projectContext = await _projectReadModelService.BuildAiContextDataAsync(_currentProjectId);
                if (!string.IsNullOrWhiteSpace(projectContext.PromptSummary))
                {
                    builder.AppendLine(projectContext.PromptSummary);
                    builder.AppendLine();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "构建世界设定 AI 项目上下文失败");
            }
        }

        builder.AppendLine("【世界设定生成约束】");
        builder.AppendLine("- 只能生成世界设定相关内容，不得输出正文、完整大纲、角色小传或无关模块。");
        builder.AppendLine("- 必须优先遵循项目基本信息，再遵循已有世界设定。");
        builder.AppendLine("- 若存在父级设定或当前选中设定，生成内容必须与其主题、类型、分类和约束保持一致。");
        builder.AppendLine("- 生成新的子设定时，应作为父级设定的细分补充，不能偏离父级主题。");
        builder.AppendLine("- 层级顺序必须保持为：项目基础信息 -> 世界观 -> 大纲 -> 配套设定 -> 正文写作。");

        if (baseSetting != null)
        {
            builder.AppendLine();
            var chain = GetAncestorChain(baseSetting);
            if (chain.Count > 0)
            {
                builder.AppendLine($"【上级设定链】{string.Join(" -> ", chain.Select(item => item.Name))}");
                foreach (var ancestor in chain)
                {
                    builder.AppendLine(BuildSettingContextBlock("上级设定", ancestor));
                }
            }

            builder.AppendLine(BuildSettingContextBlock(
                optimizeCurrent ? "当前待优化设定" : "父级设定",
                baseSetting));
        }

        return builder.ToString().Trim();
    }

    private string BuildWorldSettingRequirements(WorldSettingDto? baseSetting, bool optimizeCurrent, string promptContext)
    {
        if (optimizeCurrent && baseSetting != null)
        {
            return $@"请基于当前设定进行优化，并严格按以下字段输出，每个字段只输出一次。
不要输出思考过程、解释、Markdown、序号或额外标题。

{promptContext}

【当前设定信息】
名称：{baseSetting.Name}
类型：{baseSetting.Type}
分类：{baseSetting.Category}
简要描述：{baseSetting.Description}
详细内容：{baseSetting.Content}
相关规则：{baseSetting.Rules}
历史背景：{baseSetting.History}
相关设定：{baseSetting.RelatedSettings}
标签：{baseSetting.Tags}
备注：{baseSetting.Notes}
重要性：{baseSetting.Importance}
状态：{baseSetting.Status}

【生成要求】
1. 必须保持当前设定主题不变。
2. 只能补充、优化或澄清与该设定直接相关的世界观内容。
3. 如与项目基本信息或上级设定冲突，优先遵循上层约束。

请按以下字段输出：
名称：
类型：
分类：
简要描述：
详细内容：
相关规则：
历史背景：
相关设定：
标签：
备注：
重要性：
状态：";
        }

        if (baseSetting != null)
        {
            return $@"请基于父级设定生成一个新的世界设定子项，并严格按以下字段输出，每个字段只输出一次。
不要输出思考过程、解释、Markdown、序号或额外标题。

{promptContext}

【生成要求】
1. 新设定必须是父级设定“{baseSetting.Name}”的下级细分内容。
2. 新设定的类型、分类、规则与历史必须与父级主题一致，不能跳到大纲、角色或正文创作。
3. 如父级设定已有规则、历史或关联内容，新设定只能延展和补充，不能推翻。

请按以下字段输出：
名称：
类型：
分类：
简要描述：
详细内容：
相关规则：
历史背景：
相关设定：
标签：
备注：
重要性：
状态：";
        }

        return $@"请生成一个完整的书籍世界设定，并严格按以下字段输出，每个字段只输出一次。
不要输出思考过程、解释、Markdown、序号或额外标题。

{promptContext}

【生成要求】
1. 新生成内容必须优先遵循项目基本信息。
2. 内容应属于世界设定层，不要越级生成完整大纲或正文情节。
3. 如果项目已有世界设定，新内容应与其保持一致并形成补充。

请按以下字段输出：
名称：
类型：
分类：
简要描述：
详细内容：
相关规则：
历史背景：
相关设定：
标签：
备注：
重要性：
状态：";
    }

    private IReadOnlyList<WorldSettingDto> GetAncestorChain(WorldSettingDto setting)
    {
        var chain = new List<WorldSettingDto>();
        var roots = _worldSettings.ToList();
        var current = setting;

        while (current.ParentId.HasValue)
        {
            var parent = FindSettingById(roots, current.ParentId.Value);
            if (parent == null)
            {
                break;
            }

            chain.Insert(0, parent);
            current = parent;
        }

        return chain;
    }

    private static string BuildSettingContextBlock(string title, WorldSettingDto setting)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"【{title}】");
        builder.AppendLine($"名称：{setting.Name}");
        builder.AppendLine($"类型：{setting.Type}");
        builder.AppendLine($"分类：{setting.Category}");
        builder.AppendLine($"描述：{setting.Description}");
        builder.AppendLine($"内容：{setting.Content}");
        builder.AppendLine($"规则：{setting.Rules}");
        builder.AppendLine($"历史：{setting.History}");
        builder.AppendLine($"关联：{setting.RelatedSettings}");
        builder.AppendLine($"标签：{setting.Tags}");
        builder.AppendLine($"备注：{setting.Notes}");
        builder.AppendLine($"重要性：{setting.Importance}");
        builder.AppendLine($"状态：{setting.Status}");
        return builder.ToString().TrimEnd();
    }

    private CreateWorldSettingDto BuildCreateDtoFromAiResult(object data, Guid? parentId)
    {
        var text = AiAutoFillFormatter.Normalize(data?.ToString());
        return new CreateWorldSettingDto
        {
            ProjectId = _currentProjectId,
            ParentId = parentId,
            Name = ExtractWorldSettingSingleLine(text, "名称") switch
            {
                { Length: > 0 } value => value,
                _ => AiAutoFillFormatter.ExtractFirstMeaningfulLine(text) ?? "AI生成世界设定"
            },
            Type = ExtractWorldSettingSingleLine(text, "类型") switch
            {
                { Length: > 0 } value => value,
                _ => "世界观"
            },
            Category = ExtractWorldSettingOptionalSingleLine(text, "分类"),
            Description = ExtractWorldSettingDescription(text),
            Content = ExtractWorldSettingOptionalSection(text, "详细内容", "内容", "设定内容") ?? text,
            Rules = ExtractWorldSettingOptionalSection(text, "相关规则", "规则"),
            History = ExtractWorldSettingOptionalSection(text, "历史背景", "历史"),
            RelatedSettings = ExtractWorldSettingOptionalSection(text, "相关设定"),
            Tags = ExtractWorldSettingOptionalSingleLine(text, "标签"),
            Notes = ExtractWorldSettingOptionalSection(text, "备注", "补充说明"),
            Status = NormalizeWorldSettingStatus(ExtractWorldSettingOptionalSingleLine(text, "状态")) ?? "Active",
            Importance = ExtractWorldSettingImportance(text)
        };
    }

    private UpdateWorldSettingDto BuildUpdateDtoFromAiResult(object data, WorldSettingDto baseSetting)
    {
        var createDto = BuildCreateDtoFromAiResult(data, baseSetting.ParentId);
        return new UpdateWorldSettingDto
        {
            Id = baseSetting.Id,
            ProjectId = baseSetting.ProjectId,
            ParentId = baseSetting.ParentId,
            Name = createDto.Name,
            Type = createDto.Type,
            Category = createDto.Category,
            Description = createDto.Description,
            Content = createDto.Content,
            Rules = createDto.Rules,
            History = createDto.History,
            RelatedSettings = createDto.RelatedSettings ?? baseSetting.RelatedSettings,
            Importance = createDto.Importance,
            ImagePath = baseSetting.ImagePath,
            Tags = createDto.Tags ?? baseSetting.Tags,
            Notes = createDto.Notes ?? baseSetting.Notes,
            Status = createDto.Status ?? baseSetting.Status,
            OrderIndex = baseSetting.OrderIndex,
            IsPublic = baseSetting.IsPublic,
            Version = baseSetting.Version
        };
    }

    private static string ExtractWorldSettingSingleLine(string text, params string[] headings)
    {
        var section = AiAutoFillFormatter.ExtractSection(text, headings);
        if (string.IsNullOrWhiteSpace(section))
        {
            return string.Empty;
        }

        return AiAutoFillFormatter.ExtractSingleLineValue(section, headings);
    }

    private static string? ExtractWorldSettingOptionalSingleLine(string text, params string[] headings)
    {
        var value = ExtractWorldSettingSingleLine(text, headings);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string? ExtractWorldSettingOptionalSection(string text, params string[] headings)
    {
        var section = AiAutoFillFormatter.ExtractSection(text, headings);
        return string.IsNullOrWhiteSpace(section) ? null : section;
    }

    private static string? ExtractWorldSettingDescription(string text)
    {
        var description = AiAutoFillFormatter.ExtractSummary(text, "简要描述", "描述", "简介");
        if (string.IsNullOrWhiteSpace(description))
        {
            description = AiAutoFillFormatter.ExtractSummary(text, "详细内容", "内容", "设定内容");
        }

        return string.IsNullOrWhiteSpace(description) ? null : description;
    }

    private static int ExtractWorldSettingImportance(string text)
    {
        var raw = ExtractWorldSettingSingleLine(text, "重要性");
        if (string.IsNullOrWhiteSpace(raw))
        {
            return 5;
        }

        var match = Regex.Match(raw, @"\d+");
        return match.Success && int.TryParse(match.Value, out var value)
            ? Math.Clamp(value, 1, 10)
            : 5;
    }

    private static string? NormalizeWorldSettingStatus(string? rawStatus)
    {
        if (string.IsNullOrWhiteSpace(rawStatus))
        {
            return null;
        }

        var normalized = rawStatus.Trim();
        return normalized switch
        {
            "启用" or "激活" or "已启用" or "生效" => "Active",
            "草稿" or "待完善" => "Draft",
            "归档" or "已归档" or "停用" => "Archived",
            _ => normalized
        };
    }
}

/// <summary>
/// 世界设定创建对话框。
/// </summary>
public class WorldSettingEditDialog : Window
{
    private readonly Guid _projectId;
    private readonly WorldSettingDto? _parentSetting;
    private readonly IAIAssistantService? _aiAssistantService;
    private readonly ProjectReadModelService? _projectReadModelService;
    private readonly TextBox _nameTextBox = new();
    private readonly ComboBox _typeComboBox = new();
    private readonly TextBox _categoryTextBox = new();
    private readonly TextBox _descriptionTextBox = new();
    private readonly TextBox _contentTextBox = new();
    private readonly TextBox _rulesTextBox = new();
    private readonly TextBox _historyTextBox = new();
    private readonly TextBox _relatedSettingsTextBox = new();
    private readonly TextBox _tagsTextBox = new();
    private readonly TextBox _notesTextBox = new();
    private readonly Slider _importanceSlider = new();
    private readonly ComboBox _statusComboBox = new();
    private readonly WorldSettingFormSnapshot _initialSnapshot;

    /// <summary>
    /// 初始化世界设定创建对话框。
    /// </summary>
    /// <param name="projectId">所属项目标识。</param>
    /// <param name="parentSetting">父级设定；为空时创建根设定。</param>
    public WorldSettingEditDialog(Guid projectId, WorldSettingDto? parentSetting)
    {
        _projectId = projectId;
        _parentSetting = parentSetting;
        _aiAssistantService = App.ServiceProvider?.GetService<IAIAssistantService>();
        _projectReadModelService = App.ServiceProvider?.GetService<ProjectReadModelService>();

        Title = parentSetting == null ? LocalizationManager.T("WS.NewSettingTitle", "新建世界设定") : LocalizationManager.TF("WS.NewChildTitle", "新建子设定 - {0}", parentSetting.Name);
        Width = 760;
        Height = 860;
        MinWidth = 700;
        MinHeight = 620;
        ResizeMode = ResizeMode.CanResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        _typeComboBox.ItemsSource = new[] { "世界观", "体系", "规则", "地点", "组织", "文化", "种族" };
        _typeComboBox.SelectedItem = parentSetting?.Type ?? "世界观";
        _categoryTextBox.Text = parentSetting?.Category ?? string.Empty;
        _importanceSlider.Minimum = 1;
        _importanceSlider.Maximum = 10;
        _importanceSlider.Value = 5;
        _importanceSlider.TickFrequency = 1;
        _importanceSlider.IsSnapToTickEnabled = true;
        _statusComboBox.ItemsSource = new[] { "Active", "Draft", "Archived" };
        _statusComboBox.SelectedItem = "Active";

        ConfigureMultilineTextBox(_descriptionTextBox, 84);
        ConfigureMultilineTextBox(_contentTextBox, 220);
        ConfigureMultilineTextBox(_rulesTextBox, 120);
        ConfigureMultilineTextBox(_historyTextBox, 120);
        ConfigureMultilineTextBox(_relatedSettingsTextBox, 84);
        ConfigureMultilineTextBox(_notesTextBox, 84);

        Content = BuildContent();
        _initialSnapshot = CaptureSnapshot();
    }

    /// <summary>
    /// 根据当前输入构建设定所需的数据传输对象。
    /// </summary>
    public CreateWorldSettingDto BuildCreateDto(Guid projectId, Guid? parentId)
    {
        return new CreateWorldSettingDto
        {
            ProjectId = projectId,
            ParentId = parentId,
            Name = _nameTextBox.Text.Trim(),
            Type = _typeComboBox.SelectedItem?.ToString() ?? "世界观",
            Category = string.IsNullOrWhiteSpace(_categoryTextBox.Text) ? null : _categoryTextBox.Text.Trim(),
            Description = string.IsNullOrWhiteSpace(_descriptionTextBox.Text) ? null : _descriptionTextBox.Text.Trim(),
            Content = string.IsNullOrWhiteSpace(_contentTextBox.Text) ? null : _contentTextBox.Text.Trim(),
            Rules = string.IsNullOrWhiteSpace(_rulesTextBox.Text) ? null : _rulesTextBox.Text.Trim(),
            History = string.IsNullOrWhiteSpace(_historyTextBox.Text) ? null : _historyTextBox.Text.Trim(),
            RelatedSettings = string.IsNullOrWhiteSpace(_relatedSettingsTextBox.Text) ? null : _relatedSettingsTextBox.Text.Trim(),
            Tags = string.IsNullOrWhiteSpace(_tagsTextBox.Text) ? null : _tagsTextBox.Text.Trim(),
            Notes = string.IsNullOrWhiteSpace(_notesTextBox.Text) ? null : _notesTextBox.Text.Trim(),
            Importance = (int)_importanceSlider.Value,
            Status = _statusComboBox.SelectedItem?.ToString() ?? "Active"
        };
    }

    private FrameworkElement BuildContent()
    {
        var panel = new StackPanel { Margin = new Thickness(20) };
        panel.Children.Add(CreateLabel(LocalizationManager.T("WS.FieldName", "设定名称")));
        panel.Children.Add(_nameTextBox);
        panel.Children.Add(CreateLabel(LocalizationManager.T("WS.FieldType", "设定类型")));
        panel.Children.Add(_typeComboBox);
        panel.Children.Add(CreateLabel(LocalizationManager.T("WS.FieldCategory", "设定分类")));
        panel.Children.Add(_categoryTextBox);
        panel.Children.Add(CreateLabel(LocalizationManager.T("WS.Importance", "重要性")));
        panel.Children.Add(_importanceSlider);
        panel.Children.Add(CreateLabel(LocalizationManager.T("Dlg.Status", "状态")));
        panel.Children.Add(_statusComboBox);
        panel.Children.Add(CreateLabel(LocalizationManager.T("WS.BriefDesc", "简要描述")));
        panel.Children.Add(CreateMultilineTextBox(_descriptionTextBox, 84));
        panel.Children.Add(CreateLabel(LocalizationManager.T("WS.Content", "详细内容")));
        panel.Children.Add(CreateMultilineTextBox(_contentTextBox, 220));
        panel.Children.Add(CreateLabel(LocalizationManager.T("WS.Rules", "相关规则")));
        panel.Children.Add(CreateMultilineTextBox(_rulesTextBox, 120));
        panel.Children.Add(CreateLabel(LocalizationManager.T("WS.History", "历史背景")));
        panel.Children.Add(CreateMultilineTextBox(_historyTextBox, 120));
        panel.Children.Add(CreateLabel(LocalizationManager.T("WS.RelatedSettings", "相关设定")));
        panel.Children.Add(CreateMultilineTextBox(_relatedSettingsTextBox, 84));
        panel.Children.Add(CreateLabel(LocalizationManager.T("WS.Tags", "标签")));
        panel.Children.Add(_tagsTextBox);
        panel.Children.Add(CreateLabel(LocalizationManager.T("Dlg.Remarks", "备注")));
        panel.Children.Add(CreateMultilineTextBox(_notesTextBox, 84));

        var buttons = new WrapPanel
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 16, 0, 0)
        };

        var aiButton = new Button { Content = LocalizationManager.T("WS.AIAutoFill", "AI自动补全"), MinWidth = 100, Margin = new Thickness(0, 0, 12, 8) };
        aiButton.Click += async (_, _) => await AutoFillWithAiAsync();

        var resetButton = new Button { Content = LocalizationManager.T("WS.ResetContent", "重置内容"), MinWidth = 100, Margin = new Thickness(0, 0, 12, 8) };
        resetButton.Click += (_, _) => ResetForm();

        var okButton = new Button { Content = LocalizationManager.T("Dlg.Create", "创建"), MinWidth = 84, Margin = new Thickness(0, 0, 12, 8), IsDefault = true };
        okButton.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_nameTextBox.Text))
            {
                MessageBox.Show(LocalizationManager.T("WS.NameRequired", "设定名称不能为空。"), LocalizationManager.T("Msg.Tip", "提示"), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            DialogResult = true;
        };

        var backButton = new Button { Content = LocalizationManager.T("WS.Back", "返回"), MinWidth = 84, Margin = new Thickness(0, 0, 0, 8), IsCancel = true };
        buttons.Children.Add(aiButton);
        buttons.Children.Add(resetButton);
        buttons.Children.Add(okButton);
        buttons.Children.Add(backButton);
        panel.Children.Add(buttons);

        return new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = panel
        };
    }

    private async Task AutoFillWithAiAsync()
    {
        if (_aiAssistantService == null)
        {
            MessageBox.Show(LocalizationManager.T("WS.AIServiceNotInit", "AI助手服务未初始化。"), LocalizationManager.T("Msg.Error", "错误"), MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var prompt = await BuildAiPromptAsync();
        var result = await _aiAssistantService.GeneratePlotAsync(new Dictionary<string, object>
        {
            ["plotType"] = "世界设定补全",
            ["theme"] = string.IsNullOrWhiteSpace(_nameTextBox.Text) ? (_typeComboBox.SelectedItem?.ToString() ?? "世界设定") : _nameTextBox.Text.Trim(),
            ["requirements"] = prompt
        });

        if (!result.IsSuccess || result.Data == null)
        {
            MessageBox.Show(result.Message ?? LocalizationManager.T("WS.AIAutoFillFailed", "AI自动补全失败。"), LocalizationManager.T("Msg.Tip", "提示"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ApplyAiResult(result.Data.ToString());
        MessageBox.Show(LocalizationManager.T("WS.AIAutoFillDone", "已按字段完成世界设定自动补全。"), LocalizationManager.T("WS.AIAutoFill", "AI自动补全"), MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async Task<string> BuildAiPromptAsync()
    {
        var builder = new StringBuilder();
        builder.AppendLine("请补全一个书籍世界设定表单。");
        builder.AppendLine("请优先补全缺失信息，并与现有内容保持一致。");
        builder.AppendLine("请严格按照以下字段输出，每个字段只输出一次。");
        builder.AppendLine("不要输出思考过程、解释、Markdown、序号或额外标题。");
        builder.AppendLine();

        if (_projectReadModelService != null && _projectId != Guid.Empty)
        {
            try
            {
                var projectContext = await _projectReadModelService.BuildAiContextDataAsync(_projectId);
                if (!string.IsNullOrWhiteSpace(projectContext.PromptSummary))
                {
                    builder.AppendLine(projectContext.PromptSummary);
                    builder.AppendLine();
                }
            }
            catch
            {
                // 项目上下文读取失败时保留已有表单补全能力
            }
        }

        builder.AppendLine("【表单约束】");
        builder.AppendLine("- 只能补全世界设定相关字段。");
        builder.AppendLine("- 必须优先遵循项目基础信息，再遵循已有世界设定。");
        if (_parentSetting != null)
        {
            builder.AppendLine("- 当前正在创建父级设定下的子设定，内容必须与父级主题保持一致。");
            builder.AppendLine(BuildSettingContextBlock("父级设定", _parentSetting));
        }
        else
        {
            builder.AppendLine("- 当前正在创建根级世界设定，内容必须与已有世界观保持一致。");
        }

        builder.AppendLine();
        builder.AppendLine("当前信息：");
        builder.AppendLine($"名称：{_nameTextBox.Text}");
        builder.AppendLine($"类型：{_typeComboBox.SelectedItem}");
        builder.AppendLine($"分类：{_categoryTextBox.Text}");
        builder.AppendLine($"简要描述：{_descriptionTextBox.Text}");
        builder.AppendLine($"详细内容：{_contentTextBox.Text}");
        builder.AppendLine($"相关规则：{_rulesTextBox.Text}");
        builder.AppendLine($"历史背景：{_historyTextBox.Text}");
        builder.AppendLine($"相关设定：{_relatedSettingsTextBox.Text}");
        builder.AppendLine($"标签：{_tagsTextBox.Text}");
        builder.AppendLine($"备注：{_notesTextBox.Text}");
        builder.AppendLine($"重要性：{(int)_importanceSlider.Value}");
        builder.AppendLine($"状态：{_statusComboBox.SelectedItem}");
        builder.AppendLine();
        builder.AppendLine("请按以下字段输出：");
        builder.AppendLine("名称：");
        builder.AppendLine("类型：");
        builder.AppendLine("分类：");
        builder.AppendLine("简要描述：");
        builder.AppendLine("详细内容：");
        builder.AppendLine("相关规则：");
        builder.AppendLine("历史背景：");
        builder.AppendLine("相关设定：");
        builder.AppendLine("标签：");
        builder.AppendLine("备注：");
        builder.AppendLine("重要性：");
        builder.AppendLine("状态：");
        return builder.ToString().Trim();
    }

    private void ApplyAiResult(string? rawResult)
    {
        var text = AiAutoFillFormatter.Normalize(rawResult);
        ApplySingleLineIfEmpty(_nameTextBox, text, "名称");
        ApplySingleLineIfEmpty(_categoryTextBox, text, "分类");
        ApplySingleLineIfEmpty(_tagsTextBox, text, "标签");

        if (_typeComboBox.SelectedItem == null || string.Equals(_typeComboBox.SelectedItem.ToString(), "世界观", StringComparison.Ordinal))
        {
            var type = ExtractWorldSettingSingleLine(text, "类型");
            if (!string.IsNullOrWhiteSpace(type))
            {
                _typeComboBox.SelectedItem = type;
            }
        }

        ApplyMultiLineIfEmpty(_descriptionTextBox, AiAutoFillFormatter.ExtractSummary(text, "简要描述", "描述", "简介"));
        ApplyMultiLineIfEmpty(_contentTextBox, AiAutoFillFormatter.ExtractSection(text, "详细内容", "内容", "设定内容"));
        ApplyMultiLineIfEmpty(_rulesTextBox, AiAutoFillFormatter.ExtractSection(text, "相关规则", "规则"));
        ApplyMultiLineIfEmpty(_historyTextBox, AiAutoFillFormatter.ExtractSection(text, "历史背景", "历史"));
        ApplyMultiLineIfEmpty(_relatedSettingsTextBox, AiAutoFillFormatter.ExtractSection(text, "相关设定"));
        ApplyMultiLineIfEmpty(_notesTextBox, AiAutoFillFormatter.ExtractSection(text, "备注", "补充说明"));

        if (string.IsNullOrWhiteSpace(_nameTextBox.Text))
        {
            var fallbackName = AiAutoFillFormatter.ExtractFirstMeaningfulLine(text);
            if (!string.IsNullOrWhiteSpace(fallbackName))
            {
                _nameTextBox.Text = fallbackName;
            }
        }

        if ((int)_importanceSlider.Value == 5)
        {
            var importance = ExtractWorldSettingImportance(text);
            if (importance != 5 || ExtractWorldSettingSingleLine(text, "重要性").Length > 0)
            {
                _importanceSlider.Value = importance;
            }
        }

        if (string.Equals(_statusComboBox.SelectedItem?.ToString(), "Active", StringComparison.Ordinal))
        {
            var status = NormalizeWorldSettingStatus(ExtractWorldSettingOptionalSingleLine(text, "状态"));
            if (!string.IsNullOrWhiteSpace(status))
            {
                _statusComboBox.SelectedItem = status;
            }
        }
    }

    private void ResetForm()
    {
        ApplySnapshot(_initialSnapshot);
    }

    private WorldSettingFormSnapshot CaptureSnapshot()
    {
        return new WorldSettingFormSnapshot(
            _nameTextBox.Text,
            _typeComboBox.SelectedItem?.ToString() ?? "世界观",
            _categoryTextBox.Text,
            _descriptionTextBox.Text,
            _contentTextBox.Text,
            _rulesTextBox.Text,
            _historyTextBox.Text,
            _relatedSettingsTextBox.Text,
            _tagsTextBox.Text,
            _notesTextBox.Text,
            (int)_importanceSlider.Value,
            _statusComboBox.SelectedItem?.ToString() ?? "Active");
    }

    private void ApplySnapshot(WorldSettingFormSnapshot snapshot)
    {
        _nameTextBox.Text = snapshot.Name;
        _typeComboBox.SelectedItem = snapshot.Type;
        _categoryTextBox.Text = snapshot.Category;
        _descriptionTextBox.Text = snapshot.Description;
        _contentTextBox.Text = snapshot.Content;
        _rulesTextBox.Text = snapshot.Rules;
        _historyTextBox.Text = snapshot.History;
        _relatedSettingsTextBox.Text = snapshot.RelatedSettings;
        _tagsTextBox.Text = snapshot.Tags;
        _notesTextBox.Text = snapshot.Notes;
        _importanceSlider.Value = snapshot.Importance;
        _statusComboBox.SelectedItem = snapshot.Status;
    }

    private static void ConfigureMultilineTextBox(TextBox textBox, double height)
    {
        textBox.AcceptsReturn = true;
        textBox.Height = height;
        textBox.TextWrapping = TextWrapping.Wrap;
        textBox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
    }

    private static TextBlock CreateLabel(string text)
    {
        return new TextBlock
        {
            Text = text,
            Margin = new Thickness(0, 10, 0, 6),
            FontWeight = FontWeights.Medium
        };
    }

    private static TextBox CreateMultilineTextBox(TextBox textBox, double height)
    {
        ConfigureMultilineTextBox(textBox, height);
        return textBox;
    }

    private static void ApplySingleLineIfEmpty(TextBox textBox, string rawContent, params string[] headings)
    {
        if (!string.IsNullOrWhiteSpace(textBox.Text))
        {
            return;
        }

        var value = ExtractWorldSettingSingleLine(rawContent, headings);
        if (!string.IsNullOrWhiteSpace(value))
        {
            textBox.Text = value;
        }
    }

    private static void ApplyMultiLineIfEmpty(TextBox textBox, string? value)
    {
        if (!string.IsNullOrWhiteSpace(textBox.Text))
        {
            return;
        }

        var normalized = AiAutoFillFormatter.Normalize(value);
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            textBox.Text = normalized;
        }
    }

    private static string ExtractWorldSettingSingleLine(string text, params string[] headings)
    {
        var section = AiAutoFillFormatter.ExtractSection(text, headings);
        if (string.IsNullOrWhiteSpace(section))
        {
            return string.Empty;
        }

        return AiAutoFillFormatter.ExtractSingleLineValue(section, headings);
    }

    private static string? ExtractWorldSettingOptionalSingleLine(string text, params string[] headings)
    {
        var value = ExtractWorldSettingSingleLine(text, headings);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static int ExtractWorldSettingImportance(string text)
    {
        var raw = ExtractWorldSettingSingleLine(text, "重要性");
        if (string.IsNullOrWhiteSpace(raw))
        {
            return 5;
        }

        var match = Regex.Match(raw, @"\d+");
        return match.Success && int.TryParse(match.Value, out var value)
            ? Math.Clamp(value, 1, 10)
            : 5;
    }

    private static string? NormalizeWorldSettingStatus(string? rawStatus)
    {
        if (string.IsNullOrWhiteSpace(rawStatus))
        {
            return null;
        }

        var normalized = rawStatus.Trim();
        return normalized switch
        {
            "启用" or "激活" or "已启用" or "生效" => "Active",
            "草稿" or "待完善" => "Draft",
            "归档" or "已归档" or "停用" => "Archived",
            _ => normalized
        };
    }

    private static string BuildSettingContextBlock(string title, WorldSettingDto setting)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"【{title}】");
        builder.AppendLine($"名称：{setting.Name}");
        builder.AppendLine($"类型：{setting.Type}");
        builder.AppendLine($"分类：{setting.Category}");
        builder.AppendLine($"描述：{setting.Description}");
        builder.AppendLine($"内容：{setting.Content}");
        builder.AppendLine($"规则：{setting.Rules}");
        builder.AppendLine($"历史：{setting.History}");
        builder.AppendLine($"关联：{setting.RelatedSettings}");
        builder.AppendLine($"标签：{setting.Tags}");
        builder.AppendLine($"备注：{setting.Notes}");
        builder.AppendLine($"重要性：{setting.Importance}");
        builder.AppendLine($"状态：{setting.Status}");
        return builder.ToString().TrimEnd();
    }
}

internal sealed record WorldSettingFormSnapshot(
    string Name,
    string Type,
    string Category,
    string Description,
    string Content,
    string Rules,
    string History,
    string RelatedSettings,
    string Tags,
    string Notes,
    int Importance,
    string Status);
