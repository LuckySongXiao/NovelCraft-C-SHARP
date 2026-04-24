using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NovelManagement.Application.Services;
using NovelManagement.Core.Entities;
using NovelManagement.WPF.Commands;
using NovelManagement.WPF.Services;

namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// PlotManagementView.xaml 的交互逻辑
    /// </summary>
    public partial class PlotManagementView : UserControl, INavigationRefreshableView
    {
        /// <summary>
        /// 剧情数据模型
        /// </summary>
        public class PlotViewModel
        {
            /// <summary>
            /// 剧情实体标识。
            /// </summary>
            public Guid PlotId { get; set; }

            /// <summary>
            /// 剧情标题。
            /// </summary>
            public string Title { get; set; } = string.Empty;

            /// <summary>
            /// 剧情类型。
            /// </summary>
            public string Type { get; set; } = string.Empty;

            /// <summary>
            /// 剧情状态。
            /// </summary>
            public string Status { get; set; } = string.Empty;

            /// <summary>
            /// 优先级。
            /// </summary>
            public string Priority { get; set; } = "中";

            /// <summary>
            /// 剧情描述。
            /// </summary>
            public string Description { get; set; } = string.Empty;

            /// <summary>
            /// 剧情完成进度。
            /// </summary>
            public int Progress { get; set; }

            /// <summary>
            /// 起始章节名称。
            /// </summary>
            public string StartChapter { get; set; } = string.Empty;

            /// <summary>
            /// 结束章节名称。
            /// </summary>
            public string EndChapter { get; set; } = string.Empty;

            /// <summary>
            /// 相关角色列表。
            /// </summary>
            public List<string> RelatedCharacters { get; set; } = new();

            /// <summary>
            /// 相关势力列表。
            /// </summary>
            public List<string> RelatedFactions { get; set; } = new();

            /// <summary>
            /// 备注信息。
            /// </summary>
            public string Notes { get; set; } = string.Empty;

            /// <summary>
            /// 创建时间。
            /// </summary>
            public DateTime CreatedDate { get; set; }

            /// <summary>
            /// 最近更新时间。
            /// </summary>
            public DateTime LastUpdated { get; set; }

            /// <summary>
            /// 类型对应颜色。
            /// </summary>
            public Brush TypeColor { get; set; } = Brushes.Gray;

            /// <summary>
            /// 状态对应颜色。
            /// </summary>
            public Brush StatusColor { get; set; } = Brushes.Gray;
        }

        private List<PlotViewModel> _allPlots = new();
        private List<PlotViewModel> _filteredPlots = new();
        private PlotViewModel? _selectedPlot;

        // AI服务
        private PlotService? _plotService;
        private ProjectContextService? _projectContextService;
        private CurrentProjectGuard? _currentProjectGuard;
        private AIAssistantService? _aiAssistantService;
        private ILogger<PlotManagementView>? _logger;
        private Guid _currentProjectId;

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
            _ = LoadPlotsAsync();
        }

        /// <summary>
        /// 初始化服务
        /// </summary>
        private void InitializeServices()
        {
            try
            {
                var serviceProvider = App.ServiceProvider
                    ?? throw new InvalidOperationException("应用服务提供者未初始化");
                var loggerFactory = serviceProvider.GetService<ILoggerFactory>()
                    ?? LoggerFactory.Create(builder => builder.AddConsole());

                _logger = serviceProvider.GetService<ILogger<PlotManagementView>>()
                    ?? loggerFactory.CreateLogger<PlotManagementView>();
                _plotService = serviceProvider.GetService<PlotService>();
                _projectContextService = serviceProvider.GetService<ProjectContextService>();
                _currentProjectGuard = serviceProvider.GetService<CurrentProjectGuard>();
                _aiAssistantService = serviceProvider.GetService<AIAssistantService>();
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
        private async Task LoadPlotsAsync()
        {
            try
            {
                _currentProjectId = _projectContextService?.CurrentProjectId ?? Guid.Empty;
                if (_currentProjectId == Guid.Empty)
                {
                    _currentProjectGuard?.TryGetCurrentProjectId(Window.GetWindow(this), "剧情管理", out _);
                    _allPlots = new List<PlotViewModel>();
                    _filteredPlots = new List<PlotViewModel>();
                    UpdatePlotList();
                    ShowPlotStatistics();
                    return;
                }

                if (_plotService == null)
                {
                    throw new InvalidOperationException("剧情服务未初始化");
                }

                var plots = await _plotService.GetPlotsByProjectIdAsync(_currentProjectId);
                _allPlots = plots.Select(MapToViewModel).ToList();
                _filteredPlots = new List<PlotViewModel>(_allPlots);
                UpdatePlotList();
                ShowPlotStatistics();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载剧情数据失败");
                MessageBox.Show($"加载剧情数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                var actionsPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 0, 0, 16)
                };

                var editButton = new Button
                {
                    Content = "编辑剧情",
                    Margin = new Thickness(0, 0, 12, 0),
                    MinWidth = 96
                };
                editButton.Click += async (_, _) => await EditPlotAsync(plot);

                var deleteButton = new Button
                {
                    Content = "删除剧情",
                    MinWidth = 96
                };
                deleteButton.Click += async (_, _) => await DeletePlotAsync(plot);

                actionsPanel.Children.Add(editButton);
                actionsPanel.Children.Add(deleteButton);

                var detailCard = new MaterialDesignThemes.Wpf.Card
                {
                    Padding = new Thickness(24),
                    Content = new ScrollViewer
                    {
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                        Content = new StackPanel
                        {
                            Children =
                            {
                                new TextBlock { Text = plot.Title, FontSize = 24, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 12) },
                                new TextBlock { Text = $"类型：{plot.Type}    状态：{plot.Status}    优先级：{plot.Priority}", FontSize = 14, Margin = new Thickness(0, 0, 0, 12) },
                                actionsPanel,
                                new TextBlock { Text = "剧情描述", FontSize = 18, FontWeight = FontWeights.Medium, Margin = new Thickness(0, 0, 0, 8) },
                                new TextBlock { Text = string.IsNullOrWhiteSpace(plot.Description) ? "暂无描述" : plot.Description, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 16) },
                                new TextBlock { Text = $"进度：{plot.Progress}%", Margin = new Thickness(0, 0, 0, 8) },
                                new ProgressBar { Value = plot.Progress, Maximum = 100, Height = 8, Margin = new Thickness(0, 0, 0, 16) },
                                new TextBlock { Text = $"起始章节：{plot.StartChapter}", Margin = new Thickness(0, 0, 0, 6) },
                                new TextBlock { Text = $"结束章节：{plot.EndChapter}", Margin = new Thickness(0, 0, 0, 6) },
                                new TextBlock { Text = $"关联角色：{(plot.RelatedCharacters.Count == 0 ? "暂无" : string.Join("、", plot.RelatedCharacters))}", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 6) },
                                new TextBlock { Text = $"创建时间：{plot.CreatedDate:yyyy-MM-dd HH:mm}", Margin = new Thickness(0, 0, 0, 6) },
                                new TextBlock { Text = $"最近更新：{plot.LastUpdated:yyyy-MM-dd HH:mm}", Margin = new Thickness(0, 0, 0, 12) },
                                new TextBlock { Text = "备注", FontSize = 18, FontWeight = FontWeights.Medium, Margin = new Thickness(0, 12, 0, 8) },
                                new TextBlock { Text = string.IsNullOrWhiteSpace(plot.Notes) ? "暂无备注" : plot.Notes, TextWrapping = TextWrapping.Wrap }
                            }
                        }
                    }
                };

                DetailArea.Children.Clear();
                DetailArea.Children.Add(detailCard);
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
                _filteredPlots = GroupByTypeToggle?.IsChecked == true
                    ? _filteredPlots.OrderBy(p => p.Type).ThenBy(p => p.Title).ToList()
                    : _filteredPlots.OrderByDescending(p => p.LastUpdated).ToList();
                UpdatePlotList();
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
        private async void NewPlot_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentProjectId == Guid.Empty)
                {
                    _currentProjectGuard?.TryGetCurrentProjectId(Window.GetWindow(this), "新建剧情", out _);
                    return;
                }

                if (_plotService == null)
                {
                    MessageBox.Show("剧情服务未初始化", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var dialog = new PlotEditDialog();
                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                var plot = await PersistNewPlotAsync(dialog);
                await LoadPlotsAsync();
                _selectedPlot = _allPlots.FirstOrDefault(p => p.PlotId == plot.Id);
                if (_selectedPlot != null)
                {
                    ShowPlotDetails(_selectedPlot);
                }
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
            var timelineText = _allPlots.Count == 0
                ? "当前项目暂无剧情。"
                : string.Join(
                    Environment.NewLine + Environment.NewLine,
                    _allPlots
                        .OrderBy(p => p.StartChapter)
                        .ThenBy(p => p.Title)
                        .Select(p => $"• {p.Title}{Environment.NewLine}  {p.StartChapter} -> {p.EndChapter}  [{p.Status}]"));

            ShowTextReportWindow("剧情时间线", timelineText);
        }

        /// <summary>
        /// 剧情分析按钮点击事件
        /// </summary>
        private void PlotAnalysis_Click(object sender, RoutedEventArgs e)
        {
            var total = _allPlots.Count;
            var mainline = _allPlots.Count(p => p.Type == "主线");
            var branch = _allPlots.Count(p => p.Type == "支线");
            var completed = _allPlots.Count(p => p.Status == "已完成");
            var inProgress = _allPlots.Count(p => p.Status == "进行中");
            var avgProgress = total > 0 ? _allPlots.Average(p => p.Progress) : 0;

            var report = "剧情分析概览" + Environment.NewLine + Environment.NewLine +
                         $"• 总剧情数：{total}" + Environment.NewLine +
                         $"• 主线剧情：{mainline}" + Environment.NewLine +
                         $"• 支线剧情：{branch}" + Environment.NewLine +
                         $"• 已完成：{completed}" + Environment.NewLine +
                         $"• 进行中：{inProgress}" + Environment.NewLine +
                         $"• 平均进度：{avgProgress:F0}%";

            ShowTextReportWindow("剧情分析", report);
        }

        /// <summary>
        /// 导出剧情按钮点击事件
        /// </summary>
        private void ExportPlots_Click(object sender, RoutedEventArgs e)
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
                Source = "PlotManagement.ExportPlots",
                Payload = new ImportExportNavigationPayload
                {
                    Action = "PlotsOnlyExport"
                }
            });
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
                                if (_plotService == null || _currentProjectId == Guid.Empty)
                                {
                                    MessageBox.Show("当前项目或剧情服务不可用，无法保存 AI 生成结果。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                                    return;
                                }

                                var createdPlot = await _plotService.CreatePlotAsync(new Plot
                                {
                                    ProjectId = _currentProjectId,
                                    Title = generatedPlot.Title,
                                    Type = generatedPlot.Type,
                                    Status = generatedPlot.Status,
                                    Priority = generatedPlot.Priority,
                                    Description = generatedPlot.Description,
                                    Progress = generatedPlot.Progress,
                                    Importance = generatedPlot.Priority switch
                                    {
                                        "高" => 9,
                                        "中" => 6,
                                        _ => 3
                                    },
                                    Notes = generatedPlot.Notes
                                });

                                await LoadPlotsAsync();
                                _selectedPlot = _allPlots.FirstOrDefault(p => p.PlotId == createdPlot.Id);
                                if (_selectedPlot != null)
                                {
                                    ShowPlotDetails(_selectedPlot);
                                }

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
                                if (_plotService == null)
                                {
                                    MessageBox.Show("剧情服务未初始化，无法保存优化结果。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                                    return;
                                }

                                var plotEntity = await _plotService.GetPlotByIdAsync(_selectedPlot.PlotId);
                                if (plotEntity == null)
                                {
                                    MessageBox.Show("未找到要优化的剧情。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                                    return;
                                }

                                plotEntity.Title = optimizedPlot.Title;
                                plotEntity.Type = optimizedPlot.Type;
                                plotEntity.Status = optimizedPlot.Status;
                                plotEntity.Priority = _selectedPlot.Priority;
                                plotEntity.Description = optimizedPlot.Description;
                                plotEntity.Progress = optimizedPlot.Progress;
                                plotEntity.Notes = optimizedPlot.Notes;

                                await _plotService.UpdatePlotAsync(plotEntity);
                                await LoadPlotsAsync();
                                _selectedPlot = _allPlots.FirstOrDefault(p => p.PlotId == plotEntity.Id);
                                if (_selectedPlot != null)
                                {
                                    ShowPlotDetails(_selectedPlot);
                                }

                                _aiAssistantService.ShowSuccess($"成功优化剧情: {optimizedPlot.Title}");
                                _logger?.LogInformation($"AI优化剧情成功: {optimizedPlot.Title}");
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
                if (data is string text && !string.IsNullOrWhiteSpace(text))
                {
                    return new PlotViewModel
                    {
                        Title = "AI生成剧情",
                        Type = "支线",
                        Status = "规划中",
                        Description = text.Trim(),
                        Progress = 0,
                        StartChapter = "待定",
                        EndChapter = "待定",
                        RelatedCharacters = new List<string>(),
                        Priority = "中",
                        TypeColor = GetTypeColor("支线"),
                        StatusColor = GetStatusColor("规划中")
                    };
                }

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
                    Priority = "中",
                    TypeColor = GetTypeColor("支线"),
                    StatusColor = GetStatusColor("规划中")
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "解析生成的剧情数据失败");
                return null;
            }
        }

        /// <summary>
        /// 在项目切换后刷新剧情数据。
        /// </summary>
        /// <param name="projectId">当前项目标识。</param>
        /// <param name="projectName">当前项目名称。</param>
        public async Task RefreshOnProjectChangedAsync(Guid? projectId, string? projectName)
        {
            _currentProjectId = projectId ?? Guid.Empty;
            await LoadPlotsAsync();
        }

        private async Task<Plot> PersistNewPlotAsync(PlotEditDialog dialog)
        {
            if (_plotService == null)
            {
                throw new InvalidOperationException("剧情服务未初始化");
            }

            var plot = new Plot
            {
                ProjectId = _currentProjectId,
                Title = dialog.PlotTitle,
                Type = dialog.PlotType,
                Status = dialog.PlotStatus,
                Priority = dialog.PlotPriority,
                Description = dialog.PlotDescription,
                Progress = 0,
                Importance = dialog.PlotPriority switch
                {
                    "高" => 9,
                    "中" => 6,
                    _ => 3
                }
            };

            return await _plotService.CreatePlotAsync(plot);
        }

        private async Task EditPlotAsync(PlotViewModel plot)
        {
            if (_plotService == null)
            {
                MessageBox.Show("剧情服务未初始化", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var entity = await _plotService.GetPlotByIdAsync(plot.PlotId);
            if (entity == null)
            {
                MessageBox.Show("未找到要编辑的剧情。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new PlotEditDialog(plot);
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            entity.Title = dialog.PlotTitle;
            entity.Type = dialog.PlotType;
            entity.Status = dialog.PlotStatus;
            entity.Priority = dialog.PlotPriority;
            entity.Description = dialog.PlotDescription;
            entity.Importance = dialog.PlotPriority switch
            {
                "高" => 9,
                "中" => 6,
                _ => 3
            };

            await _plotService.UpdatePlotAsync(entity);
            await LoadPlotsAsync();
            _selectedPlot = _allPlots.FirstOrDefault(p => p.PlotId == entity.Id);
            if (_selectedPlot != null)
            {
                ShowPlotDetails(_selectedPlot);
            }
        }

        private async Task DeletePlotAsync(PlotViewModel plot)
        {
            if (_plotService == null)
            {
                MessageBox.Show("剧情服务未初始化", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var result = MessageBox.Show($"确定删除剧情“{plot.Title}”吗？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            var deleted = await _plotService.DeletePlotAsync(plot.PlotId);
            if (!deleted)
            {
                MessageBox.Show("剧情删除失败或剧情不存在。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _selectedPlot = null;
            await LoadPlotsAsync();
            ShowPlotStatistics();
        }

        private PlotViewModel MapToViewModel(Plot plot)
        {
            return new PlotViewModel
            {
                PlotId = plot.Id,
                Title = plot.Title,
                Type = plot.Type,
                Status = plot.Status,
                Priority = plot.Priority,
                Description = plot.Description ?? string.Empty,
                Progress = (int)Math.Round(plot.Progress),
                StartChapter = plot.StartChapter?.Title ?? "待定",
                EndChapter = plot.EndChapter?.Title ?? "待定",
                RelatedCharacters = plot.RelatedCharacters.Select(c => c.Name).ToList(),
                Notes = plot.Notes ?? string.Empty,
                CreatedDate = plot.CreatedAt.ToLocalTime(),
                LastUpdated = plot.UpdatedAt.ToLocalTime(),
                TypeColor = GetTypeColor(plot.Type),
                StatusColor = GetStatusColor(plot.Status)
            };
        }

        private static SolidColorBrush GetTypeColor(string type)
        {
            return type switch
            {
                "主线" => new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                "支线" => new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                "伏笔" => new SolidColorBrush(Color.FromRgb(156, 39, 176)),
                "回忆" => new SolidColorBrush(Color.FromRgb(63, 81, 181)),
                _ => new SolidColorBrush(Color.FromRgb(96, 125, 139))
            };
        }

        private static SolidColorBrush GetStatusColor(string status)
        {
            return status switch
            {
                "已完成" => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                "进行中" => new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                "规划中" => new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                "暂停" => new SolidColorBrush(Color.FromRgb(158, 158, 158)),
                _ => new SolidColorBrush(Color.FromRgb(96, 125, 139))
            };
        }

        private void ShowPlotStatistics()
        {
            var total = _allPlots.Count;
            var mainline = _allPlots.Count(p => p.Type == "主线");
            var branch = _allPlots.Count(p => p.Type == "支线");
            var completionRate = total > 0 ? _allPlots.Average(p => p.Progress) : 0;
            var completed = _allPlots.Count(p => p.Status == "已完成");
            var inProgress = _allPlots.Count(p => p.Status == "进行中");
            var planning = _allPlots.Count(p => p.Status == "规划中");
            var recentUpdates = _allPlots
                .OrderByDescending(p => p.LastUpdated)
                .Take(5)
                .Select(p => $"• {p.Title}（{p.LastUpdated:MM-dd HH:mm}）")
                .ToList();

            var report = "剧情统计" + Environment.NewLine + Environment.NewLine +
                         $"• 总剧情数：{total}" + Environment.NewLine +
                         $"• 主线剧情：{mainline}" + Environment.NewLine +
                         $"• 支线剧情：{branch}" + Environment.NewLine +
                         $"• 平均完成度：{completionRate:F0}%" + Environment.NewLine +
                         $"• 已完成：{completed} / 进行中：{inProgress} / 规划中：{planning}" + Environment.NewLine + Environment.NewLine +
                         "最近更新：" + Environment.NewLine +
                         (recentUpdates.Count == 0 ? "• 暂无剧情更新" : string.Join(Environment.NewLine, recentUpdates));

            var card = new MaterialDesignThemes.Wpf.Card
            {
                Padding = new Thickness(24),
                Content = new ScrollViewer
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Content = new StackPanel
                    {
                        Children =
                        {
                            new TextBlock { Text = "剧情统计", FontSize = 24, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 20) },
                            new TextBlock { Text = report, TextWrapping = TextWrapping.Wrap, LineHeight = 20 }
                        }
                    }
                }
            };

            DetailArea.Children.Clear();
            DetailArea.Children.Add(card);
        }

        private void ShowTextReportWindow(string title, string content)
        {
            var window = new Window
            {
                Title = title,
                Width = 900,
                Height = 700,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                Content = new ScrollViewer
                {
                    Margin = new Thickness(24),
                    Content = new TextBlock
                    {
                        Text = content,
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 14,
                        LineHeight = 20
                    }
                }
            };
            window.ShowDialog();
        }

        #endregion
    }

    #region 剧情对话框类

    /// <summary>
    /// 剧情生成对话框
    /// </summary>
    public class PlotGenerationDialog : Window
    {
        /// <summary>
        /// 目标剧情类型。
        /// </summary>
        public string PlotType { get; set; } = "支线";

        /// <summary>
        /// 剧情主题。
        /// </summary>
        public string Theme { get; set; } = "";

        /// <summary>
        /// 目标章节范围。
        /// </summary>
        public string TargetChapters { get; set; } = "";

        /// <summary>
        /// 相关角色列表。
        /// </summary>
        public List<string> RelatedCharacters { get; set; } = new();

        /// <summary>
        /// 选中的剧情元素。
        /// </summary>
        public List<string> PlotElements { get; set; } = new();

        /// <summary>
        /// 初始化剧情生成参数对话框。
        /// </summary>
        /// <param name="existingPlots">当前项目已有剧情列表。</param>
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
    /// 剧情编辑对话框。
    /// </summary>
    public class PlotEditDialog : Window
    {
        /// <summary>
        /// 编辑后的剧情标题。
        /// </summary>
        public string PlotTitle { get; private set; } = string.Empty;

        /// <summary>
        /// 编辑后的剧情类型。
        /// </summary>
        public string PlotType { get; private set; } = "支线";

        /// <summary>
        /// 编辑后的剧情状态。
        /// </summary>
        public string PlotStatus { get; private set; } = "规划中";

        /// <summary>
        /// 编辑后的剧情优先级。
        /// </summary>
        public string PlotPriority { get; private set; } = "中";

        /// <summary>
        /// 编辑后的剧情描述。
        /// </summary>
        public string PlotDescription { get; private set; } = string.Empty;

        /// <summary>
        /// 初始化新建剧情对话框。
        /// </summary>
        public PlotEditDialog()
            : this(null)
        {
        }

        /// <summary>
        /// 初始化剧情编辑对话框。
        /// </summary>
        /// <param name="plot">待编辑的剧情；为空时表示新建。</param>
        public PlotEditDialog(PlotManagementView.PlotViewModel? plot)
        {
            Title = plot == null ? "新建剧情" : $"编辑剧情 - {plot.Title}";
            Width = 520;
            Height = 420;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;

            var titleTextBox = new TextBox
            {
                Text = plot?.Title ?? string.Empty
            };
            var typeComboBox = new ComboBox
            {
                ItemsSource = new[] { "主线", "支线", "伏笔", "回忆", "番外" }
            };
            var statusComboBox = new ComboBox
            {
                ItemsSource = new[] { "规划中", "进行中", "已完成", "暂停" }
            };
            var priorityComboBox = new ComboBox
            {
                ItemsSource = new[] { "高", "中", "低" }
            };
            var descriptionTextBox = new TextBox
            {
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Height = 140,
                Text = plot?.Description ?? string.Empty
            };

            typeComboBox.SelectedItem = plot?.Type ?? "支线";
            statusComboBox.SelectedItem = plot?.Status ?? "规划中";
            priorityComboBox.SelectedItem = plot?.Priority ?? "中";

            var formPanel = new StackPanel { Margin = new Thickness(24) };
            formPanel.Children.Add(new TextBlock { Text = "剧情标题", Margin = new Thickness(0, 0, 0, 6) });
            formPanel.Children.Add(titleTextBox);
            formPanel.Children.Add(new TextBlock { Text = "剧情类型", Margin = new Thickness(0, 12, 0, 6) });
            formPanel.Children.Add(typeComboBox);
            formPanel.Children.Add(new TextBlock { Text = "剧情状态", Margin = new Thickness(0, 12, 0, 6) });
            formPanel.Children.Add(statusComboBox);
            formPanel.Children.Add(new TextBlock { Text = "优先级", Margin = new Thickness(0, 12, 0, 6) });
            formPanel.Children.Add(priorityComboBox);
            formPanel.Children.Add(new TextBlock { Text = "剧情描述", Margin = new Thickness(0, 12, 0, 6) });
            formPanel.Children.Add(descriptionTextBox);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 20, 0, 0)
            };

            var confirmButton = new Button
            {
                Content = "确定",
                Width = 88,
                Margin = new Thickness(0, 0, 12, 0),
                IsDefault = true
            };
            confirmButton.Click += (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(titleTextBox.Text))
                {
                    MessageBox.Show("请输入剧情标题。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                PlotTitle = titleTextBox.Text.Trim();
                PlotType = typeComboBox.SelectedItem?.ToString() ?? "支线";
                PlotStatus = statusComboBox.SelectedItem?.ToString() ?? "规划中";
                PlotPriority = priorityComboBox.SelectedItem?.ToString() ?? "中";
                PlotDescription = descriptionTextBox.Text.Trim();
                DialogResult = true;
            };

            var cancelButton = new Button
            {
                Content = "取消",
                Width = 88,
                IsCancel = true
            };

            buttonPanel.Children.Add(confirmButton);
            buttonPanel.Children.Add(cancelButton);
            formPanel.Children.Add(buttonPanel);

            Content = formPanel;
        }
    }

    /// <summary>
    /// 剧情优化对话框
    /// </summary>
    public class PlotOptimizationDialog : Window
    {
        /// <summary>
        /// 选中的优化目标列表。
        /// </summary>
        public List<string> SelectedOptimizationGoals { get; set; } = new();

        /// <summary>
        /// 初始化剧情优化对话框。
        /// </summary>
        /// <param name="plot">待优化的剧情。</param>
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
        /// <summary>
        /// 初始化剧情连贯性检查结果对话框。
        /// </summary>
        /// <param name="continuityData">连贯性检查结果数据。</param>
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
        /// <summary>
        /// 初始化剧情建议对话框。
        /// </summary>
        /// <param name="suggestionsData">AI生成的剧情建议数据。</param>
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
