using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Extensions.Logging;
using NovelManagement.WPF.Commands;
using NovelManagement.WPF.Services;

namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// PlotManagementView.xaml 的交互逻辑
    /// </summary>
    public partial class PlotManagementView : UserControl
    {
        /// <summary>
        /// 剧情数据模型
        /// </summary>
        public class PlotViewModel
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public int Progress { get; set; }
            public string StartChapter { get; set; } = string.Empty;
            public string EndChapter { get; set; } = string.Empty;
            public List<string> RelatedCharacters { get; set; } = new();
            public List<string> RelatedFactions { get; set; } = new();
            public string Notes { get; set; } = string.Empty;
            public DateTime CreatedDate { get; set; }
            public DateTime LastUpdated { get; set; }
            public Brush TypeColor { get; set; } = Brushes.Gray;
            public Brush StatusColor { get; set; } = Brushes.Gray;
        }

        private List<PlotViewModel> _allPlots = new();
        private List<PlotViewModel> _filteredPlots = new();
        private PlotViewModel? _selectedPlot;

        // AI服务
        private AIAssistantService? _aiAssistantService;
        private ILogger<PlotManagementView>? _logger;

        /// <summary>
        /// 选择剧情命令
        /// </summary>
        public ICommand SelectPlotCommand { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public PlotManagementView()
        {
            InitializeComponent();
            InitializeServices();
            InitializeCommands();
            LoadPlots();
        }

        /// <summary>
        /// 初始化服务
        /// </summary>
        private void InitializeServices()
        {
            try
            {
                var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
                _logger = loggerFactory.CreateLogger<PlotManagementView>();

                _logger.LogInformation("剧情管理界面服务初始化完成");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"服务初始化失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region 初始化

        /// <summary>
        /// 初始化命令
        /// </summary>
        private void InitializeCommands()
        {
            SelectPlotCommand = new RelayCommand<PlotViewModel>(SelectPlot);
        }

        /// <summary>
        /// 加载剧情数据
        /// </summary>
        private void LoadPlots()
        {
            // 模拟剧情数据
            _allPlots = new List<PlotViewModel>
            {
                new PlotViewModel
                {
                    Id = 1,
                    Title = "主线：面具觉醒",
                    Type = "主线",
                    Status = "已完成",
                    Description = "林轩获得神秘面具，开始修仙之路的起始剧情",
                    Progress = 100,
                    StartChapter = "第1章",
                    EndChapter = "第15章",
                    RelatedCharacters = new List<string> { "林轩", "玄天老祖" },
                    CreatedDate = DateTime.Now.AddDays(-30),
                    LastUpdated = DateTime.Now.AddDays(-20),
                    TypeColor = new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                    StatusColor = new SolidColorBrush(Color.FromRgb(76, 175, 80))
                },
                new PlotViewModel
                {
                    Id = 2,
                    Title = "主线：天劫降临",
                    Type = "主线",
                    Status = "进行中",
                    Description = "林轩面临天劫考验，修为突破的关键剧情",
                    Progress = 75,
                    StartChapter = "第40章",
                    EndChapter = "第50章",
                    RelatedCharacters = new List<string> { "林轩", "血魔宗主" },
                    CreatedDate = DateTime.Now.AddDays(-10),
                    LastUpdated = DateTime.Now.AddHours(-2),
                    TypeColor = new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                    StatusColor = new SolidColorBrush(Color.FromRgb(33, 150, 243))
                },
                new PlotViewModel
                {
                    Id = 3,
                    Title = "支线：苏雨薇的秘密",
                    Type = "支线",
                    Status = "进行中",
                    Description = "揭示苏雨薇身世之谜，与主线剧情相互呼应",
                    Progress = 60,
                    StartChapter = "第25章",
                    EndChapter = "第35章",
                    RelatedCharacters = new List<string> { "苏雨薇", "苏家家主" },
                    CreatedDate = DateTime.Now.AddDays(-15),
                    LastUpdated = DateTime.Now.AddDays(-1),
                    TypeColor = new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                    StatusColor = new SolidColorBrush(Color.FromRgb(33, 150, 243))
                },
                new PlotViewModel
                {
                    Id = 4,
                    Title = "支线：血魔宗的阴谋",
                    Type = "支线",
                    Status = "已完成",
                    Description = "血魔宗暗中策划的阴谋，为后续主线做铺垫",
                    Progress = 100,
                    StartChapter = "第20章",
                    EndChapter = "第30章",
                    RelatedCharacters = new List<string> { "血魔宗主", "血魔长老" },
                    CreatedDate = DateTime.Now.AddDays(-25),
                    LastUpdated = DateTime.Now.AddDays(-10),
                    TypeColor = new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                    StatusColor = new SolidColorBrush(Color.FromRgb(76, 175, 80))
                },
                new PlotViewModel
                {
                    Id = 5,
                    Title = "伏笔：古老预言",
                    Type = "伏笔",
                    Status = "规划中",
                    Description = "关于千面劫体质的古老预言，为后续剧情埋下伏笔",
                    Progress = 20,
                    StartChapter = "第5章",
                    EndChapter = "待定",
                    RelatedCharacters = new List<string> { "玄天老祖", "天机阁主" },
                    CreatedDate = DateTime.Now.AddDays(-5),
                    LastUpdated = DateTime.Now.AddDays(-2),
                    TypeColor = new SolidColorBrush(Color.FromRgb(156, 39, 176)),
                    StatusColor = new SolidColorBrush(Color.FromRgb(255, 152, 0))
                },
                new PlotViewModel
                {
                    Id = 6,
                    Title = "支线：万宝商会的委托",
                    Type = "支线",
                    Status = "规划中",
                    Description = "万宝商会委托林轩寻找珍贵材料的任务",
                    Progress = 10,
                    StartChapter = "第55章",
                    EndChapter = "待定",
                    RelatedCharacters = new List<string> { "林轩", "万宝会长" },
                    CreatedDate = DateTime.Now.AddDays(-3),
                    LastUpdated = DateTime.Now.AddDays(-1),
                    TypeColor = new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                    StatusColor = new SolidColorBrush(Color.FromRgb(255, 152, 0))
                }
            };

            _filteredPlots = new List<PlotViewModel>(_allPlots);
            UpdatePlotList();
        }

        /// <summary>
        /// 更新剧情列表显示
        /// </summary>
        private void UpdatePlotList()
        {
            PlotListControl.ItemsSource = _filteredPlots;
        }

        #endregion

        #region 筛选和搜索

        /// <summary>
        /// 剧情类型筛选变化事件
        /// </summary>
        private void PlotTypeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        /// <summary>
        /// 剧情状态筛选变化事件
        /// </summary>
        private void PlotStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        /// <summary>
        /// 搜索文本变化事件
        /// </summary>
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        /// <summary>
        /// 应用筛选条件
        /// </summary>
        private void ApplyFilters()
        {
            var searchText = SearchTextBox.Text?.Trim().ToLower() ?? string.Empty;
            var selectedType = ((ComboBoxItem)PlotTypeFilter.SelectedItem)?.Content?.ToString();
            var selectedStatus = ((ComboBoxItem)PlotStatusFilter.SelectedItem)?.Content?.ToString();

            _filteredPlots = _allPlots.Where(p =>
            {
                // 搜索筛选
                var matchesSearch = string.IsNullOrEmpty(searchText) ||
                                  p.Title.ToLower().Contains(searchText) ||
                                  p.Description.ToLower().Contains(searchText);

                // 类型筛选
                var matchesType = string.IsNullOrEmpty(selectedType) ||
                                selectedType == "全部" ||
                                p.Type == selectedType;

                // 状态筛选
                var matchesStatus = string.IsNullOrEmpty(selectedStatus) ||
                                  selectedStatus == "全部" ||
                                  p.Status == selectedStatus;

                return matchesSearch && matchesType && matchesStatus;
            }).ToList();

            UpdatePlotList();
        }

        #endregion

        #region 剧情操作

        /// <summary>
        /// 选择剧情
        /// </summary>
        private void SelectPlot(PlotViewModel plot)
        {
            if (plot != null)
            {
                _selectedPlot = plot;
                ShowPlotDetails(plot);
            }
        }

        /// <summary>
        /// 显示剧情详细信息
        /// </summary>
        private void ShowPlotDetails(PlotViewModel plot)
        {
            try
            {
                // TODO: 创建剧情详细信息界面
                MessageBox.Show($"显示剧情详细信息: {plot.Title}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"显示剧情详情失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 分组模式切换事件
        /// </summary>
        private void GroupBy_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                if (GroupByTypeToggle?.IsChecked == true)
                {
                    // TODO: 按类型分组显示
                    MessageBox.Show("按类型分组功能正在开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"切换分组模式失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 新建剧情按钮点击事件
        /// </summary>
        private void NewPlot_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBox.Show("新建剧情功能正在开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"新建剧情操作失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 查看时间线按钮点击事件
        /// </summary>
        private void ViewTimeline_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("时间线视图功能正在开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 剧情分析按钮点击事件
        /// </summary>
        private void PlotAnalysis_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("剧情分析功能正在开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 导出剧情按钮点击事件
        /// </summary>
        private void ExportPlots_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("导出剧情功能正在开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region AI辅助功能

        /// <summary>
        /// AI生成剧情按钮点击事件
        /// </summary>
        private async void AIGeneratePlot_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger?.LogInformation("开始AI生成剧情");

                // 显示生成参数对话框
                var dialog = new PlotGenerationDialog(_allPlots);
                if (dialog.ShowDialog() == true)
                {
                    var parameters = new Dictionary<string, object>
                    {
                        ["plotType"] = dialog.PlotType,
                        ["theme"] = dialog.Theme,
                        ["targetChapters"] = dialog.TargetChapters,
                        ["relatedCharacters"] = dialog.RelatedCharacters,
                        ["plotElements"] = dialog.PlotElements,
                        ["existingPlots"] = _allPlots.Select(p => new { p.Title, p.Type, p.Description }).ToList()
                    };

                    if (_aiAssistantService != null)
                    {
                        var result = await _aiAssistantService.GeneratePlotAsync(parameters);

                        if (result.IsSuccess && result.Data != null)
                        {
                            var generatedPlot = ParseGeneratedPlot(result.Data);
                            if (generatedPlot != null)
                            {
                                generatedPlot.Id = _allPlots.Count + 1;
                                generatedPlot.CreatedDate = DateTime.Now;
                                generatedPlot.LastUpdated = DateTime.Now;
                                _allPlots.Add(generatedPlot);
                                ApplyFilters();

                                _aiAssistantService.ShowSuccess($"成功生成剧情: {generatedPlot.Title}");
                                _logger?.LogInformation($"AI生成剧情成功: {generatedPlot.Title}");
                            }
                            else
                            {
                                _aiAssistantService.ShowError("生成的剧情数据格式不正确");
                            }
                        }
                        else
                        {
                            _aiAssistantService.ShowError(result.Message ?? "AI生成剧情失败");
                        }
                    }
                    else
                    {
                        MessageBox.Show("AI助手服务未初始化", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "AI生成剧情异常");
                MessageBox.Show($"AI生成剧情失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// AI优化剧情按钮点击事件
        /// </summary>
        private async void AIOptimizePlot_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedPlot == null)
                {
                    MessageBox.Show("请先选择要优化的剧情", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                _logger?.LogInformation($"开始AI优化剧情: {_selectedPlot.Title}");

                var dialog = new PlotOptimizationDialog(_selectedPlot);
                if (dialog.ShowDialog() == true)
                {
                    var optimizationGoals = dialog.SelectedOptimizationGoals;

                    if (_aiAssistantService != null)
                    {
                        var parameters = new Dictionary<string, object>
                        {
                            ["plotData"] = _selectedPlot,
                            ["optimizationGoals"] = optimizationGoals
                        };
                        var result = await _aiAssistantService.OptimizePlotAsync(parameters);

                        if (result.IsSuccess && result.Data != null)
                        {
                            var optimizedPlot = ParseGeneratedPlot(result.Data);
                            if (optimizedPlot != null)
                            {
                                var index = _allPlots.FindIndex(p => p.Id == _selectedPlot.Id);
                                if (index >= 0)
                                {
                                    optimizedPlot.Id = _selectedPlot.Id;
                                    optimizedPlot.CreatedDate = _selectedPlot.CreatedDate;
                                    optimizedPlot.LastUpdated = DateTime.Now;
                                    _allPlots[index] = optimizedPlot;
                                    ApplyFilters();

                                    _aiAssistantService.ShowSuccess($"成功优化剧情: {optimizedPlot.Title}");
                                    _logger?.LogInformation($"AI优化剧情成功: {optimizedPlot.Title}");
                                }
                            }
                            else
                            {
                                _aiAssistantService.ShowError("优化后的剧情数据格式不正确");
                            }
                        }
                        else
                        {
                            _aiAssistantService.ShowError(result.Message ?? "AI优化剧情失败");
                        }
                    }
                    else
                    {
                        MessageBox.Show("AI助手服务未初始化", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "AI优化剧情异常");
                MessageBox.Show($"AI优化剧情失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// AI剧情连贯性检查按钮点击事件
        /// </summary>
        private async void AIPlotContinuity_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_allPlots.Count < 2)
                {
                    MessageBox.Show("至少需要2个剧情才能进行连贯性检查", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                _logger?.LogInformation("开始AI检查剧情连贯性");

                if (_aiAssistantService != null)
                {
                    var parameters = new Dictionary<string, object>
                    {
                        ["plots"] = _allPlots.Cast<object>().ToList()
                    };
                    var result = await _aiAssistantService.CheckPlotContinuityAsync(parameters);

                    if (result.IsSuccess && result.Data != null)
                    {
                        var continuityDialog = new PlotContinuityDialog(result.Data);
                        continuityDialog.ShowDialog();

                        _aiAssistantService.ShowSuccess("剧情连贯性检查完成");
                        _logger?.LogInformation("AI检查剧情连贯性成功");
                    }
                    else
                    {
                        _aiAssistantService.ShowError(result.Message ?? "AI检查剧情连贯性失败");
                    }
                }
                else
                {
                    MessageBox.Show("AI助手服务未初始化", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "AI检查剧情连贯性异常");
                MessageBox.Show($"AI检查剧情连贯性失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// AI剧情建议按钮点击事件
        /// </summary>
        private async void AIPlotSuggestions_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger?.LogInformation("开始AI获取剧情建议");

                var parameters = new Dictionary<string, object>
                {
                    ["currentPlot"] = _selectedPlot,
                    ["context"] = new Dictionary<string, object>
                    {
                        ["allPlots"] = _allPlots,
                        ["selectedPlot"] = _selectedPlot,
                        ["currentProgress"] = _allPlots.Average(p => p.Progress)
                    }
                };

                if (_aiAssistantService != null)
                {
                    var result = await _aiAssistantService.GetPlotSuggestionsAsync(parameters);

                    if (result.IsSuccess && result.Data != null)
                    {
                        var suggestionsDialog = new PlotSuggestionsDialog(result.Data);
                        suggestionsDialog.ShowDialog();

                        _aiAssistantService.ShowSuccess("剧情建议获取完成");
                        _logger?.LogInformation("AI获取剧情建议成功");
                    }
                    else
                    {
                        _aiAssistantService.ShowError(result.Message ?? "AI获取剧情建议失败");
                    }
                }
                else
                {
                    MessageBox.Show("AI助手服务未初始化", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "AI获取剧情建议异常");
                MessageBox.Show($"AI获取剧情建议失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 解析生成的剧情数据
        /// </summary>
        /// <param name="data">生成的数据</param>
        /// <returns>剧情视图模型</returns>
        private PlotViewModel? ParseGeneratedPlot(object data)
        {
            try
            {
                // 这里应该根据实际的AI返回数据格式进行解析
                // 暂时返回模拟数据
                return new PlotViewModel
                {
                    Title = "AI生成剧情",
                    Type = "支线",
                    Status = "规划中",
                    Description = "由AI生成的剧情，具有独特的故事线和发展脉络",
                    Progress = 0,
                    StartChapter = "待定",
                    EndChapter = "待定",
                    RelatedCharacters = new List<string> { "林轩" },
                    TypeColor = new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                    StatusColor = new SolidColorBrush(Color.FromRgb(255, 152, 0))
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "解析生成的剧情数据失败");
                return null;
            }
        }

        #endregion
    }

    #region 剧情对话框类

    /// <summary>
    /// 剧情生成对话框
    /// </summary>
    public class PlotGenerationDialog : Window
    {
        public string PlotType { get; set; } = "支线";
        public string Theme { get; set; } = "";
        public string TargetChapters { get; set; } = "";
        public List<string> RelatedCharacters { get; set; } = new();
        public List<string> PlotElements { get; set; } = new();

        public PlotGenerationDialog(List<PlotManagementView.PlotViewModel> existingPlots)
        {
            Title = "AI剧情生成参数";
            Width = 500;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var result = MessageBox.Show("是否使用默认参数生成剧情？", "剧情生成", MessageBoxButton.YesNo, MessageBoxImage.Question);
            DialogResult = result == MessageBoxResult.Yes;
        }
    }

    /// <summary>
    /// 剧情优化对话框
    /// </summary>
    public class PlotOptimizationDialog : Window
    {
        public List<string> SelectedOptimizationGoals { get; set; } = new();

        public PlotOptimizationDialog(PlotManagementView.PlotViewModel plot)
        {
            Title = $"优化剧情: {plot.Title}";
            Width = 400;
            Height = 350;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            SelectedOptimizationGoals = new List<string> { "完善剧情描述", "优化角色关系", "增强戏剧冲突", "改进节奏控制" };
            var result = MessageBox.Show("是否使用默认优化目标？", "剧情优化", MessageBoxButton.YesNo, MessageBoxImage.Question);
            DialogResult = result == MessageBoxResult.Yes;
        }
    }

    /// <summary>
    /// 剧情连贯性检查对话框
    /// </summary>
    public class PlotContinuityDialog : Window
    {
        public PlotContinuityDialog(object continuityData)
        {
            Title = "剧情连贯性检查结果";
            Width = 700;
            Height = 500;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            MessageBox.Show("剧情连贯性检查结果已生成", "检查完成", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
        }
    }

    /// <summary>
    /// 剧情建议对话框
    /// </summary>
    public class PlotSuggestionsDialog : Window
    {
        public PlotSuggestionsDialog(object suggestionsData)
        {
            Title = "AI剧情建议";
            Width = 600;
            Height = 450;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            MessageBox.Show("AI剧情建议已生成", "建议完成", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
        }
    }

    #endregion
}
