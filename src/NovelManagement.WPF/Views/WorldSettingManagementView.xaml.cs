using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NovelManagement.Application.DTOs;
using NovelManagement.Application.Interfaces;
using NovelManagement.WPF.Services;
using Microsoft.Extensions.Logging;
using MaterialDesignThemes.Wpf;

namespace NovelManagement.WPF.Views;

/// <summary>
/// 世界设定管理视图
/// </summary>
public partial class WorldSettingManagementView : UserControl
{
    private readonly IWorldSettingService _worldSettingService;
    private readonly ILogger<WorldSettingManagementView>? _logger;
    private readonly WorldSettingAnalysisService _analysisService;
    private ObservableCollection<WorldSettingDto> _worldSettings;
    private Guid _currentProjectId;

    public WorldSettingManagementView()
    {
        InitializeComponent();
        _worldSettings = new ObservableCollection<WorldSettingDto>();

        // 注意：在实际应用中，这些服务应该通过依赖注入获取
        // 这里为了演示目的，暂时设为null
        _worldSettingService = null!;
        _currentProjectId = Guid.Empty;

        try
        {
            _logger = App.ServiceProvider?.GetService(typeof(ILogger<WorldSettingManagementView>)) as ILogger<WorldSettingManagementView>;
            var analysisLogger = App.ServiceProvider?.GetService(typeof(ILogger<WorldSettingAnalysisService>)) as ILogger<WorldSettingAnalysisService>;
            _analysisService = new WorldSettingAnalysisService(analysisLogger);
        }
        catch (Exception ex)
        {
            _analysisService = new WorldSettingAnalysisService();
            MessageBox.Show($"AI分析服务初始化失败，使用默认服务: {ex.Message}", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        LoadSampleData();
    }

    /// <summary>
    /// 加载示例数据（用于演示）
    /// </summary>
    private void LoadSampleData()
    {
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
    private void AddWorldSetting_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("新建世界设定功能待实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
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
    private void RefreshWorldSettings_Click(object sender, RoutedEventArgs e)
    {
        LoadSampleData();
        MessageBox.Show("数据已刷新", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <summary>
    /// AI助手按钮点击事件
    /// </summary>
    private void AIAssistant_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ShowAIAssistantDialog();
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
}
