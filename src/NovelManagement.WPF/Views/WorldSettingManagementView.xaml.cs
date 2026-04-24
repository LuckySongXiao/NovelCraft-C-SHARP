using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NovelManagement.Application.DTOs;
using NovelManagement.Application.Interfaces;
using NovelManagement.WPF.Services;
using Microsoft.Extensions.Logging;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;

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
            _aiAssistantService = App.ServiceProvider?.GetService<IAIAssistantService>();
            _logger = App.ServiceProvider?.GetService(typeof(ILogger<WorldSettingManagementView>)) as ILogger<WorldSettingManagementView>;
            var analysisLogger = App.ServiceProvider?.GetService(typeof(ILogger<WorldSettingAnalysisService>)) as ILogger<WorldSettingAnalysisService>;
            _analysisService = new WorldSettingAnalysisService(analysisLogger);
        }
        catch (Exception ex)
        {
            _worldSettingService = null!;
            _projectContextService = null;
            _aiAssistantService = null;
            _analysisService = new WorldSettingAnalysisService();
            MessageBox.Show($"AI分析服务初始化失败，使用默认服务: {ex.Message}", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            new WorldSettingDto
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
                    new WorldSettingDto
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
            new WorldSettingDto
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
                    new WorldSettingDto
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
            new WorldSettingDto
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
        
        // 加载筛选器数据
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
            MessageBox.Show($"加载世界设定失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 加载筛选器数据
    /// </summary>
    private void LoadFilterData()
    {
        var types = _worldSettings.Select(ws => ws.Type).Distinct().ToList();
        types.Insert(0, "全部类型");
        TypeFilterComboBox.ItemsSource = types;
        TypeFilterComboBox.SelectedIndex = 0;

        var categories = _worldSettings.Where(ws => !string.IsNullOrEmpty(ws.Category))
                                      .Select(ws => ws.Category!)
                                      .Distinct()
                                      .ToList();
        categories.Insert(0, "全部分类");
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
            MessageBox.Show("请先选择项目后再创建世界设定。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_worldSettingService == null)
        {
            MessageBox.Show("世界设定服务未初始化。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var selectedSetting = WorldSettingsTreeView.SelectedItem as WorldSettingDto;
        var dialog = new WorldSettingEditDialog(selectedSetting);
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
                MessageBox.Show("无法获取主窗口，无法打开导入导出页面。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
            MessageBox.Show($"打开设定导入页面失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show("请先选择一个世界设定进行分析", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 显示加载指示器
            var loadingDialog = new ProgressDialog("AI分析中", "正在分析世界设定的一致性和完整性...");
            loadingDialog.Show();

            try
            {
                // 执行AI分析
                var analysisResult = await _analysisService.AnalyzeWorldSettingAsync(selectedSetting, _worldSettings.ToList());

                loadingDialog.Close();

                // 显示分析结果
                ShowAnalysisResult(analysisResult);
            }
            catch (Exception ex)
            {
                loadingDialog.Close();
                MessageBox.Show($"AI分析失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "AI分析过程中发生错误");
            MessageBox.Show($"分析过程中发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 刷新按钮点击事件
    /// </summary>
    private async void RefreshWorldSettings_Click(object sender, RoutedEventArgs e)
    {
        await LoadWorldSettingsAsync();
        MessageBox.Show("数据已刷新", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
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
                MessageBox.Show("无法获取主窗口，无法打开导入导出页面。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
            MessageBox.Show($"打开设定导出页面失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show("请先选择项目后再使用AI助手。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
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
                    "是：AI优化当前设定并保存\n否：AI生成新的设定并保存\n取消：打开原始AI助手",
                    "AI世界设定",
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
                "是：AI生成新的设定并保存\n否：打开原始AI助手",
                "AI世界设定",
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
            MessageBox.Show($"启动AI助手失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 显示AI助手对话框
    /// </summary>
    private void ShowAIAssistantDialog()
    {
        var dialog = new AIAssistantDialog("世界设定管理", GetCurrentContext());
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
            context.AppendLine($"当前选中：{selectedSetting.Name}");
            context.AppendLine($"设定类型：{selectedSetting.Type}");
            context.AppendLine($"设定分类：{selectedSetting.Category}");
            context.AppendLine($"重要性：{selectedSetting.Importance}/10");
            context.AppendLine($"描述：{selectedSetting.Description}");
        }

        context.AppendLine("\n可用操作：");
        context.AppendLine("- 创建新的世界设定");
        context.AppendLine("- 分析设定合理性");
        context.AppendLine("- 检查设定一致性");
        context.AppendLine("- 优化设定内容");
        context.AppendLine("- 生成相关设定");

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
        var searchText = SearchTextBox?.Text?.ToLower() ?? "";
        var selectedType = TypeFilterComboBox?.SelectedItem?.ToString();
        var selectedCategory = CategoryFilterComboBox?.SelectedItem?.ToString();

        var filteredSettings = _worldSettings.Where(ws =>
        {
            // 搜索文本筛选
            var matchesSearch = string.IsNullOrEmpty(searchText) ||
                               ws.Name.ToLower().Contains(searchText) ||
                               (ws.Description?.ToLower().Contains(searchText) ?? false);

            // 类型筛选
            var matchesType = selectedType == "全部类型" || ws.Type == selectedType;

            // 分类筛选
            var matchesCategory = selectedCategory == "全部分类" || ws.Category == selectedCategory;

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

        // 标题
        var titleBlock = new TextBlock
        {
            Text = setting.Name,
            Style = (Style)FindResource("MaterialDesignHeadline5TextBlock"),
            Margin = new Thickness(0, 0, 0, 20)
        };
        DetailsPanel.Children.Add(titleBlock);

        // 基本信息
        AddDetailItem("类型", setting.Type);
        AddDetailItem("分类", setting.Category ?? "无");
        AddDetailItem("重要性", $"{setting.Importance}/10");
        AddDetailItem("创建时间", setting.CreatedAt.ToString("yyyy-MM-dd HH:mm"));
        AddDetailItem("更新时间", setting.UpdatedAt.ToString("yyyy-MM-dd HH:mm"));

        // 描述
        if (!string.IsNullOrEmpty(setting.Description))
        {
            AddDetailSection("描述", setting.Description);
        }

        // 内容
        if (!string.IsNullOrEmpty(setting.Content))
        {
            AddDetailSection("详细内容", setting.Content);
        }

        // 规则
        if (!string.IsNullOrEmpty(setting.Rules))
        {
            AddDetailSection("相关规则", setting.Rules);
        }

        // 历史
        if (!string.IsNullOrEmpty(setting.History))
        {
            AddDetailSection("历史背景", setting.History);
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
        if (payload?.SettingId != null)
        {
            if (FocusSetting(payload.SettingId.Value))
            {
                return;
            }
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
        var parameters = new Dictionary<string, object>
        {
            ["title"] = optimizeCurrent && baseSetting != null ? $"优化世界设定：{baseSetting.Name}" : "生成世界设定",
            ["theme"] = "请生成一个适合小说项目使用的世界设定，并输出名称、类型、分类、描述、详细内容、相关规则、历史背景、重要性。",
            ["requirements"] = optimizeCurrent && baseSetting != null
                ? $"请基于当前设定进行优化并输出结构化文本。当前设定：{baseSetting.Name}，类型：{baseSetting.Type}，分类：{baseSetting.Category}，描述：{baseSetting.Description}"
                : "请输出一个完整世界设定，至少包含名称、类型、分类、描述、详细内容、相关规则、历史背景、重要性。",
            ["context"] = GetCurrentContext()
        };

        var result = await _aiAssistantService!.GenerateOutlineAsync(parameters);
        if (!result.IsSuccess || result.Data == null)
        {
            MessageBox.Show(result.Message ?? "AI生成失败。", "AI世界设定", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (optimizeCurrent && baseSetting != null)
        {
            var updateDto = BuildUpdateDtoFromAiResult(result.Data, baseSetting);
            var updatedSetting = await _worldSettingService.UpdateAsync(updateDto);
            await LoadWorldSettingsAsync();
            FocusSetting(updatedSetting.Id);
            MessageBox.Show($"已使用 AI 优化并保存设定：{updatedSetting.Name}", "AI世界设定", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var createDto = BuildCreateDtoFromAiResult(result.Data, null);
        createDto.ProjectId = _currentProjectId;
        var created = await _worldSettingService.CreateAsync(createDto);
        await LoadWorldSettingsAsync();
        FocusSetting(created.Id);
        MessageBox.Show($"已使用 AI 生成并保存设定：{created.Name}", "AI世界设定", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private CreateWorldSettingDto BuildCreateDtoFromAiResult(object data, Guid? parentId)
    {
        var text = data?.ToString() ?? string.Empty;
        return new CreateWorldSettingDto
        {
            ProjectId = _currentProjectId,
            ParentId = parentId,
            Name = ExtractField(text, "名称") ?? ExtractFirstMeaningfulLine(text) ?? "AI生成世界设定",
            Type = ExtractField(text, "类型") ?? "世界观",
            Category = ExtractField(text, "分类") ?? "综合",
            Description = ExtractField(text, "描述") ?? text,
            Content = ExtractField(text, "详细内容") ?? text,
            Rules = ExtractField(text, "相关规则"),
            History = ExtractField(text, "历史背景"),
            Importance = ExtractImportance(text)
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
            Importance = createDto.Importance,
            RelatedSettings = baseSetting.RelatedSettings,
            ImagePath = baseSetting.ImagePath,
            Tags = baseSetting.Tags,
            Notes = baseSetting.Notes,
            Status = baseSetting.Status,
            OrderIndex = baseSetting.OrderIndex,
            IsPublic = baseSetting.IsPublic,
            Version = baseSetting.Version
        };
    }

    private static string? ExtractField(string text, string fieldName)
    {
        var match = Regex.Match(text, $"{fieldName}\\s*[:：]\\s*(.+)");
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    private static int ExtractImportance(string text)
    {
        var raw = ExtractField(text, "重要性");
        if (raw == null)
        {
            return 5;
        }

        var match = Regex.Match(raw, @"\d+");
        return match.Success && int.TryParse(match.Value, out var value)
            ? Math.Clamp(value, 1, 10)
            : 5;
    }

    private static string? ExtractFirstMeaningfulLine(string text)
    {
        return text
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim().TrimStart('•', '-', '*', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', '.', '、', ' '))
            .FirstOrDefault(line => !string.IsNullOrWhiteSpace(line));
    }
}

/// <summary>
/// 世界设定创建对话框。
/// </summary>
public class WorldSettingEditDialog : Window
{
    private readonly TextBox _nameTextBox = new();
    private readonly ComboBox _typeComboBox = new();
    private readonly TextBox _categoryTextBox = new();
    private readonly TextBox _descriptionTextBox = new();
    private readonly TextBox _contentTextBox = new();
    private readonly Slider _importanceSlider = new();

    /// <summary>
    /// 初始化世界设定创建对话框。
    /// </summary>
    /// <param name="parentSetting">父级设定；为空时创建根设定。</param>
    public WorldSettingEditDialog(WorldSettingDto? parentSetting)
    {
        Title = parentSetting == null ? "新建设定" : $"新建子设定 - {parentSetting.Name}";
        Width = 520;
        Height = 640;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        _typeComboBox.ItemsSource = new[] { "世界观", "体系", "规则", "地点", "组织", "文化", "种族" };
        _typeComboBox.SelectedIndex = 0;
        _importanceSlider.Minimum = 1;
        _importanceSlider.Maximum = 10;
        _importanceSlider.Value = 5;
        _importanceSlider.TickFrequency = 1;
        _importanceSlider.IsSnapToTickEnabled = true;

        _descriptionTextBox.AcceptsReturn = true;
        _descriptionTextBox.TextWrapping = TextWrapping.Wrap;
        _descriptionTextBox.Height = 80;
        _contentTextBox.AcceptsReturn = true;
        _contentTextBox.TextWrapping = TextWrapping.Wrap;
        _contentTextBox.Height = 180;

        Content = BuildContent();
    }

    /// <summary>
    /// 根据当前输入构建创建设定所需的数据传输对象。
    /// </summary>
    /// <param name="projectId">所属项目标识。</param>
    /// <param name="parentId">父级设定标识。</param>
    /// <returns>可用于创建世界设定的数据对象。</returns>
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
            Importance = (int)_importanceSlider.Value
        };
    }

    private FrameworkElement BuildContent()
    {
        var panel = new StackPanel { Margin = new Thickness(20) };
        panel.Children.Add(new TextBlock { Text = "设定名称" });
        panel.Children.Add(_nameTextBox);
        panel.Children.Add(new TextBlock { Text = "设定类型", Margin = new Thickness(0, 12, 0, 0) });
        panel.Children.Add(_typeComboBox);
        panel.Children.Add(new TextBlock { Text = "设定分类", Margin = new Thickness(0, 12, 0, 0) });
        panel.Children.Add(_categoryTextBox);
        panel.Children.Add(new TextBlock { Text = "重要性", Margin = new Thickness(0, 12, 0, 0) });
        panel.Children.Add(_importanceSlider);
        panel.Children.Add(new TextBlock { Text = "简要描述", Margin = new Thickness(0, 12, 0, 0) });
        panel.Children.Add(_descriptionTextBox);
        panel.Children.Add(new TextBlock { Text = "详细内容", Margin = new Thickness(0, 12, 0, 0) });
        panel.Children.Add(_contentTextBox);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 16, 0, 0)
        };

        var okButton = new Button { Content = "创建", Width = 84, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
        okButton.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_nameTextBox.Text))
            {
                MessageBox.Show("设定名称不能为空。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            DialogResult = true;
        };

        var cancelButton = new Button { Content = "取消", Width = 84, IsCancel = true };
        buttons.Children.Add(okButton);
        buttons.Children.Add(cancelButton);
        panel.Children.Add(buttons);
        return new ScrollViewer { Content = panel };
    }
}
