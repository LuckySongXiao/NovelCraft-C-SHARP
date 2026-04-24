using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using NovelManagement.WPF.Commands;
using NovelManagement.WPF.Services;

namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// 时间线管理视图
    /// </summary>
    public partial class TimelineView : UserControl, INavigationRefreshableView, INavigationAwareView
    {
        #region 属性

        /// <summary>
        /// 时间线事件列表
        /// </summary>
        public ObservableCollection<TimelineEventViewModel> TimelineEvents { get; set; }

        /// <summary>
        /// 事件参与者列表
        /// </summary>
        public ObservableCollection<EventParticipantViewModel> EventParticipants { get; set; }

        /// <summary>
        /// 当前选中的事件
        /// </summary>
        public TimelineEventViewModel SelectedEvent { get; set; }

        /// <summary>
        /// 选择事件命令
        /// </summary>
        public ICommand SelectEventCommand { get; private set; }

        private readonly TimelineDataService? _timelineDataService;
        private readonly ProjectContextService? _projectContextService;
        private readonly CurrentProjectGuard? _currentProjectGuard;
        private Guid _currentProjectId;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化时间线管理视图。
        /// </summary>
        public TimelineView()
        {
            InitializeComponent();
            _timelineDataService = App.ServiceProvider?.GetService<TimelineDataService>();
            _projectContextService = App.ServiceProvider?.GetService<ProjectContextService>();
            _currentProjectGuard = App.ServiceProvider?.GetService<CurrentProjectGuard>();
            InitializeData();
            InitializeCommands();
            _ = LoadTimelineEventsAsync();

            // 添加调试信息检查控件是否正确加载
            this.Loaded += TimelineView_Loaded;
        }

        private void TimelineView_Loaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("=== TimelineView_Loaded 控件检查 ===");
            System.Diagnostics.Debug.WriteLine($"EmptyStatePanel: {EmptyStatePanel != null}");
            System.Diagnostics.Debug.WriteLine($"EditPanel: {EditPanel != null}");
            System.Diagnostics.Debug.WriteLine($"EventTitleTextBox: {EventTitleTextBox != null}");
            System.Diagnostics.Debug.WriteLine($"TimelineListControl: {TimelineListControl != null}");
            System.Diagnostics.Debug.WriteLine("=== 控件检查完成 ===");
            if (_currentProjectId == Guid.Empty)
            {
                _ = LoadTimelineEventsAsync();
            }
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化数据
        /// </summary>
        private void InitializeData()
        {
            TimelineEvents = new ObservableCollection<TimelineEventViewModel>();
            EventParticipants = new ObservableCollection<EventParticipantViewModel>();
        }

        /// <summary>
        /// 初始化命令
        /// </summary>
        private void InitializeCommands()
        {
            SelectEventCommand = new RelayCommand<TimelineEventViewModel>(SelectTimelineEvent);
        }

        /// <summary>
        /// 加载时间线事件数据
        /// </summary>
        private async Task LoadTimelineEventsAsync()
        {
            try
            {
                _currentProjectId = _projectContextService?.CurrentProjectId ?? Guid.Empty;
                if (_currentProjectId == Guid.Empty)
                {
                    _currentProjectGuard?.TryGetCurrentProjectId(Window.GetWindow(this), "时间线管理", out _);
                    TimelineEvents.Clear();
                    TimelineListControl.ItemsSource = TimelineEvents;
                    UpdateStatistics();
                    HideEditPanel();
                    return;
                }

                var events = _timelineDataService == null
                    ? new List<TimelineEventViewModel>()
                    : await _timelineDataService.LoadTimelineEventsAsync(_currentProjectId);

                TimelineEvents.Clear();
                foreach (var timelineEvent in events)
                {
                    timelineEvent.ParticipantCount = timelineEvent.Participants.Count;
                    TimelineEvents.Add(timelineEvent);
                }

                // 设置列表数据源
                TimelineListControl.ItemsSource = TimelineEvents;

                // 更新统计信息
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载时间线事件数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 更新统计信息
        /// </summary>
        private void UpdateStatistics()
        {
            try
            {
                if (TimelineEvents == null) return;

                var totalCount = TimelineEvents.Count;
                var historyCount = TimelineEvents.Count(e => e.Category == "历史事件");
                var plotCount = TimelineEvents.Count(e => e.Category == "剧情事件");
                var characterCount = TimelineEvents.Count(e => e.Category == "角色事件");
                var factionCount = TimelineEvents.Count(e => e.Category == "势力事件");
                var worldCount = TimelineEvents.Count(e => e.Category == "世界事件");
                var cultivationCount = TimelineEvents.Count(e => e.Category == "修炼事件");

                // 状态统计
                var completedCount = TimelineEvents.Count(e => e.Status == "已完成");
                var inProgressCount = TimelineEvents.Count(e => e.Status == "进行中");
                var plannedCount = TimelineEvents.Count(e => e.Status == "计划中");

                System.Diagnostics.Debug.WriteLine($"统计信息更新: 总数={totalCount}, 历史={historyCount}, 剧情={plotCount}");

                // 更新UI统计显示
                UpdateStatisticsUI(totalCount, historyCount, plotCount, characterCount,
                                 completedCount, inProgressCount, plannedCount);

                // 更新最近事件显示
                UpdateRecentEvents();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新统计信息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新统计信息UI
        /// </summary>
        private void UpdateStatisticsUI(int total, int history, int plot, int character,
                                       int completed, int inProgress, int planned)
        {
            try
            {
                // 更新数量显示
                if (TotalEventsText != null) TotalEventsText.Text = total.ToString();
                if (HistoryEventsText != null) HistoryEventsText.Text = history.ToString();
                if (PlotEventsText != null) PlotEventsText.Text = plot.ToString();
                if (CharacterEventsText != null) CharacterEventsText.Text = character.ToString();

                // 更新状态进度条
                if (total > 0)
                {
                    if (CompletedProgressBar != null) CompletedProgressBar.Value = (double)completed / total * 100;
                    if (InProgressProgressBar != null) InProgressProgressBar.Value = (double)inProgress / total * 100;
                    if (PlannedProgressBar != null) PlannedProgressBar.Value = (double)planned / total * 100;
                }

                if (CompletedCountText != null) CompletedCountText.Text = completed.ToString();
                if (InProgressCountText != null) InProgressCountText.Text = inProgress.ToString();
                if (PlannedCountText != null) PlannedCountText.Text = planned.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新统计UI失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新最近事件显示
        /// </summary>
        private void UpdateRecentEvents()
        {
            try
            {
                if (RecentEventsPanel == null || TimelineEvents == null) return;

                RecentEventsPanel.Children.Clear();

                var recentEvents = TimelineEvents
                    .OrderByDescending(e => e.CreatedAt)
                    .Take(3)
                    .ToList();

                if (recentEvents.Count == 0)
                {
                    var noDataText = new TextBlock
                    {
                        Text = "暂无事件数据",
                        FontSize = 14,
                        Foreground = (Brush)FindResource("MaterialDesignBodyLight"),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 16, 0, 0)
                    };
                    RecentEventsPanel.Children.Add(noDataText);
                    return;
                }

                foreach (var evt in recentEvents)
                {
                    var border = new Border
                    {
                        BorderBrush = (Brush)FindResource("MaterialDesignDivider"),
                        BorderThickness = new Thickness(0, 0, 0, 1),
                        Padding = new Thickness(0, 0, 0, 8),
                        Margin = new Thickness(0, 0, 0, 8)
                    };

                    var grid = new Grid();
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var stackPanel = new StackPanel();
                    Grid.SetColumn(stackPanel, 0);

                    var titleText = new TextBlock
                    {
                        Text = $"{evt.Category}：{evt.Title}",
                        FontWeight = FontWeights.Medium
                    };
                    stackPanel.Children.Add(titleText);

                    var descText = new TextBlock
                    {
                        Text = evt.Description,
                        Foreground = (Brush)FindResource("MaterialDesignBodyLight"),
                        FontSize = 12
                    };
                    stackPanel.Children.Add(descText);

                    var timeText = new TextBlock
                    {
                        Text = GetRelativeTime(evt.CreatedAt),
                        Foreground = (Brush)FindResource("MaterialDesignBodyLight"),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(timeText, 1);

                    grid.Children.Add(stackPanel);
                    grid.Children.Add(timeText);
                    border.Child = grid;
                    RecentEventsPanel.Children.Add(border);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新最近事件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取相对时间显示
        /// </summary>
        private string GetRelativeTime(DateTime dateTime)
        {
            var timeSpan = DateTime.Now - dateTime;

            if (timeSpan.TotalMinutes < 1)
                return "刚刚";
            else if (timeSpan.TotalHours < 1)
                return $"{(int)timeSpan.TotalMinutes}分钟前";
            else if (timeSpan.TotalDays < 1)
                return $"{(int)timeSpan.TotalHours}小时前";
            else if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays}天前";
            else
                return dateTime.ToString("MM月dd日");
        }

        #endregion

        #region 搜索和筛选

        /// <summary>
        /// 搜索文本变化事件
        /// </summary>
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterTimelineEvents();
        }

        /// <summary>
        /// 类别筛选变化事件
        /// </summary>
        private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterTimelineEvents();
        }

        /// <summary>
        /// 筛选时间线事件
        /// </summary>
        private void FilterTimelineEvents()
        {
            var searchText = SearchTextBox?.Text?.ToLower() ?? "";
            var selectedCategory = (CategoryFilterComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString();

            var filteredEvents = TimelineEvents.Where(e =>
                (string.IsNullOrEmpty(searchText) || 
                 e.Title.ToLower().Contains(searchText) || 
                 e.Description.ToLower().Contains(searchText)) &&
                (selectedCategory == "全部类别" || selectedCategory == null || e.Category == selectedCategory)
            ).ToList();

            TimelineListControl.ItemsSource = filteredEvents;
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 时间线事件点击事件
        /// </summary>
        private void TimelineEvent_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"TimelineEvent_Click 被触发 - sender: {sender?.GetType()}");

                if (sender is Button button)
                {
                    System.Diagnostics.Debug.WriteLine($"Button找到 - Tag类型: {button.Tag?.GetType()}");

                    if (button.Tag is TimelineEventViewModel timelineEvent)
                    {
                        System.Diagnostics.Debug.WriteLine($"选择事件: {timelineEvent.Title}");
                        SelectTimelineEvent(timelineEvent);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Tag不是TimelineEventViewModel - 实际类型: {button.Tag?.GetType()}");

                        // 尝试从DataContext获取
                        if (button.DataContext is TimelineEventViewModel dcEvent)
                        {
                            System.Diagnostics.Debug.WriteLine($"从DataContext获取事件: {dcEvent.Title}");
                            SelectTimelineEvent(dcEvent);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"DataContext也不是TimelineEventViewModel - 实际类型: {button.DataContext?.GetType()}");
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"sender不是Button - 实际类型: {sender?.GetType()}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TimelineEvent_Click异常: {ex.Message}");
                MessageBox.Show($"选择时间线事件失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 时间线事件预览鼠标按下事件
        /// </summary>
        private void TimelineEvent_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"TimelineEvent_PreviewMouseDown 被触发 - sender: {sender?.GetType()}");

                if (sender is Button button)
                {
                    TimelineEventViewModel timelineEvent = null;

                    // 首先尝试从Tag获取
                    if (button.Tag is TimelineEventViewModel tagEvent)
                    {
                        timelineEvent = tagEvent;
                        System.Diagnostics.Debug.WriteLine($"从Tag获取事件: {timelineEvent.Title}");
                    }
                    // 然后尝试从DataContext获取
                    else if (button.DataContext is TimelineEventViewModel dcEvent)
                    {
                        timelineEvent = dcEvent;
                        System.Diagnostics.Debug.WriteLine($"从DataContext获取事件: {timelineEvent.Title}");
                    }

                    if (timelineEvent != null)
                    {
                        SelectTimelineEvent(timelineEvent);
                        e.Handled = true; // 标记事件已处理，防止进一步传播
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"无法获取TimelineEventViewModel - Tag: {button.Tag?.GetType()}, DataContext: {button.DataContext?.GetType()}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TimelineEvent_PreviewMouseDown异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 测试选择事件
        /// </summary>
        private void TestSelect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== 测试选择事件开始 ===");

                // 检查是否有事件数据
                if (TimelineEvents == null || TimelineEvents.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("没有时间线事件数据");
                    MessageBox.Show("没有时间线事件数据", "测试", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 选择第一个事件进行测试
                var firstEvent = TimelineEvents.First();
                System.Diagnostics.Debug.WriteLine($"测试选择事件: {firstEvent.Title}");

                // 直接调用选择方法
                SelectTimelineEvent(firstEvent);

                System.Diagnostics.Debug.WriteLine("=== 测试选择事件完成 ===");
                MessageBox.Show($"已选择事件: {firstEvent.Title}", "测试", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"测试选择事件失败: {ex.Message}");
                MessageBox.Show($"测试失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 时间线事件鼠标释放事件
        /// </summary>
        private void TimelineEvent_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"TimelineEvent_MouseUp 被触发 - sender: {sender?.GetType()}");

                if (sender is Button button)
                {
                    TimelineEventViewModel timelineEvent = null;

                    // 首先尝试从Tag获取
                    if (button.Tag is TimelineEventViewModel tagEvent)
                    {
                        timelineEvent = tagEvent;
                        System.Diagnostics.Debug.WriteLine($"MouseUp从Tag获取事件: {timelineEvent.Title}");
                    }
                    // 然后尝试从DataContext获取
                    else if (button.DataContext is TimelineEventViewModel dcEvent)
                    {
                        timelineEvent = dcEvent;
                        System.Diagnostics.Debug.WriteLine($"MouseUp从DataContext获取事件: {timelineEvent.Title}");
                    }

                    if (timelineEvent != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"MouseUp选择事件: {timelineEvent.Title}");
                        SelectTimelineEvent(timelineEvent);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"MouseUp无法获取TimelineEventViewModel");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TimelineEvent_MouseUp异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 时间线列表选择变化事件
        /// </summary>
        private void TimelineList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("TimelineList_SelectionChanged 被触发");

                if (TimelineListControl.SelectedItem is TimelineEventViewModel selectedEvent)
                {
                    System.Diagnostics.Debug.WriteLine($"ListView选择事件: {selectedEvent.Title}");
                    SelectTimelineEvent(selectedEvent);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ListView没有选中项");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TimelineList_SelectionChanged异常: {ex.Message}");
                MessageBox.Show($"选择事件失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 事件卡片点击事件
        /// </summary>
        private void EventCard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("EventCard_MouseLeftButtonUp 被触发");

                if (sender is FrameworkElement element && element.DataContext is TimelineEventViewModel timelineEvent)
                {
                    System.Diagnostics.Debug.WriteLine($"卡片点击选择事件: {timelineEvent.Title}");
                    SelectTimelineEvent(timelineEvent);
                    e.Handled = true; // 防止事件继续传播
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"EventCard_MouseLeftButtonUp: 无法获取事件数据 - Sender: {sender?.GetType()}, DataContext: {(sender as FrameworkElement)?.DataContext?.GetType()}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EventCard_MouseLeftButtonUp异常: {ex.Message}");
                MessageBox.Show($"选择事件失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region 时间线事件管理

        /// <summary>
        /// 添加事件
        /// </summary>
        private void AddEvent_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 创建新的时间线事件
                SelectedEvent = new TimelineEventViewModel
                {
                    Id = 0, // 新建时ID为0
                    Title = "",
                    Category = "历史事件",
                    EventDate = DateTime.Now,
                    Location = "",
                    Importance = "中",
                    Status = "计划中",
                    Description = "",
                    Impact = "",
                    CreatedAt = DateTime.Now
                };

                // 清空参与者列表
                EventParticipants.Clear();

                // 显示编辑面板
                ShowEditPanel();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"创建时间线事件失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 导入时间线数据
        /// </summary>
        private void ImportTimeline_Click(object sender, RoutedEventArgs e)
        {
            _ = ImportTimelineAsync();
        }

        /// <summary>
        /// 导出时间线数据
        /// </summary>
        private void ExportTimeline_Click(object sender, RoutedEventArgs e)
        {
            _ = ExportTimelineAsync();
        }

        /// <summary>
        /// AI助手按钮点击事件
        /// </summary>
        private void AIAssistant_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 获取当前上下文信息
                var contextInfo = GetCurrentContextInfo();

                // 打开AI助手对话框
                var aiDialog = new AIAssistantDialog("时间线管理", contextInfo);
                aiDialog.Owner = Window.GetWindow(this);
                aiDialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开AI助手失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 获取当前上下文信息
        /// </summary>
        private string GetCurrentContextInfo()
        {
            var context = new System.Text.StringBuilder();
            context.AppendLine("=== 时间线管理上下文信息 ===");
            context.AppendLine($"总事件数量: {TimelineEvents?.Count ?? 0}");

            if (TimelineEvents?.Count > 0)
            {
                var categories = TimelineEvents.GroupBy(e => e.Category).Select(g => $"{g.Key}: {g.Count()}个");
                context.AppendLine($"事件分类: {string.Join(", ", categories)}");

                var recentEvents = TimelineEvents.OrderByDescending(e => e.CreatedAt).Take(3);
                context.AppendLine("\n最近的事件:");
                foreach (var evt in recentEvents)
                {
                    context.AppendLine($"- {evt.Title} ({evt.Category}, {evt.TimeDisplay})");
                }
            }

            if (SelectedEvent != null)
            {
                context.AppendLine($"\n当前选中事件: {SelectedEvent.Title}");
                context.AppendLine($"事件类别: {SelectedEvent.Category}");
                context.AppendLine($"事件时间: {SelectedEvent.TimeDisplay}");
                context.AppendLine($"事件描述: {SelectedEvent.Description}");
                context.AppendLine($"参与者数量: {SelectedEvent.ParticipantCount}");
            }

            return context.ToString();
        }

        /// <summary>
        /// 选择时间线事件
        /// </summary>
        private void SelectTimelineEvent(TimelineEventViewModel timelineEvent)
        {
            if (timelineEvent == null)
            {
                System.Diagnostics.Debug.WriteLine("SelectTimelineEvent: timelineEvent为null");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"SelectTimelineEvent: 开始选择事件 {timelineEvent.Title}");

            SelectedEvent = timelineEvent;
            LoadTimelineEventDetails(timelineEvent);
            ShowEditPanel();

            System.Diagnostics.Debug.WriteLine("SelectTimelineEvent: 事件选择完成");
        }

        /// <summary>
        /// 加载时间线事件详情
        /// </summary>
        private void LoadTimelineEventDetails(TimelineEventViewModel timelineEvent)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"LoadTimelineEventDetails: 开始加载事件详情 {timelineEvent.Title}");

                // 检查控件是否存在
                if (EventTitleTextBox == null)
                {
                    System.Diagnostics.Debug.WriteLine("LoadTimelineEventDetails: EventTitleTextBox为null");
                    return;
                }

                // 填充基本信息
                EventTitleTextBox.Text = timelineEvent.Title ?? "";
                EventDescriptionTextBox.Text = timelineEvent.Description ?? "";
                EventLocationTextBox.Text = timelineEvent.Location ?? "";
                EventImpactTextBox.Text = timelineEvent.Impact ?? "";
                EventDatePicker.SelectedDate = timelineEvent.EventDate;

                // 设置事件类别选择
                if (EventCategoryComboBox != null)
                {
                    foreach (ComboBoxItem item in EventCategoryComboBox.Items)
                    {
                        if (item.Content?.ToString() == timelineEvent.Category)
                        {
                            EventCategoryComboBox.SelectedItem = item;
                            break;
                        }
                    }
                }

                // 设置重要程度选择
                if (EventImportanceComboBox != null)
                {
                    foreach (ComboBoxItem item in EventImportanceComboBox.Items)
                    {
                        if (item.Content?.ToString() == timelineEvent.Importance)
                        {
                            EventImportanceComboBox.SelectedItem = item;
                            break;
                        }
                    }
                }

                // 设置事件状态选择
                if (EventStatusComboBox != null)
                {
                    foreach (ComboBoxItem item in EventStatusComboBox.Items)
                    {
                        if (item.Content?.ToString() == timelineEvent.Status)
                        {
                            EventStatusComboBox.SelectedItem = item;
                            break;
                        }
                    }
                }

                // 加载参与者列表
                LoadEventParticipants(timelineEvent.Id);

                System.Diagnostics.Debug.WriteLine("LoadTimelineEventDetails: 事件详情加载完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadTimelineEventDetails: 加载失败 - {ex.Message}");
                MessageBox.Show($"加载事件详情失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 加载事件参与者
        /// </summary>
        private void LoadEventParticipants(int eventId)
        {
            var participants = SelectedEvent?.Participants?.ToList() ?? new List<EventParticipantViewModel>();

            EventParticipants.Clear();
            foreach (var participant in participants)
            {
                EventParticipants.Add(participant);
            }

            ParticipantListControl.ItemsSource = EventParticipants;
        }

        #endregion

        #region 参与者管理

        /// <summary>
        /// 添加参与者
        /// </summary>
        private void AddParticipant_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var newParticipant = new EventParticipantViewModel
                {
                    Name = "",
                    Type = "角色",
                    Role = ""
                };

                EventParticipants.Add(newParticipant);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加参与者失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 删除参与者
        /// </summary>
        private void RemoveParticipant_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.CommandParameter is EventParticipantViewModel participant)
                {
                    EventParticipants.Remove(participant);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除参与者失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region 保存和取消

        /// <summary>
        /// 保存事件
        /// </summary>
        private async void SaveEvent_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 验证输入
                if (string.IsNullOrWhiteSpace(EventTitleTextBox.Text))
                {
                    MessageBox.Show("请输入事件标题", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 更新事件信息
                SelectedEvent.Title = EventTitleTextBox.Text.Trim();
                SelectedEvent.Description = EventDescriptionTextBox.Text.Trim();
                SelectedEvent.Location = EventLocationTextBox.Text.Trim();
                SelectedEvent.Impact = EventImpactTextBox.Text.Trim();
                SelectedEvent.EventDate = EventDatePicker.SelectedDate ?? DateTime.Now;
                SelectedEvent.Category = (EventCategoryComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "历史事件";
                SelectedEvent.Importance = (EventImportanceComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "中";
                SelectedEvent.Status = (EventStatusComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "计划中";
                SelectedEvent.ParticipantCount = EventParticipants.Count;
                SelectedEvent.Participants = EventParticipants.ToList();

                // 如果是新建事件，添加到列表
                if (SelectedEvent.Id == 0)
                {
                    SelectedEvent.Id = TimelineEvents.Count > 0 ? TimelineEvents.Max(e => e.Id) + 1 : 1;
                    SelectedEvent.CreatedAt = DateTime.Now;
                    TimelineEvents.Add(SelectedEvent);
                }

                await PersistTimelineEventsAsync();
                MessageBox.Show("时间线事件保存成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);

                // 刷新列表
                FilterTimelineEvents();
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存时间线事件失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 取消编辑
        /// </summary>
        private void CancelEdit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 清空表单
                EventTitleTextBox.Text = "";
                EventDescriptionTextBox.Text = "";
                EventLocationTextBox.Text = "";
                EventImpactTextBox.Text = "";
                EventDatePicker.SelectedDate = DateTime.Now;
                EventCategoryComboBox.SelectedIndex = 0;
                EventImportanceComboBox.SelectedIndex = 1; // 默认选择"中"
                EventStatusComboBox.SelectedIndex = 0;

                // 清空参与者列表
                EventParticipants.Clear();

                // 隐藏编辑面板
                HideEditPanel();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"取消编辑失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region 界面控制

        /// <summary>
        /// 显示编辑面板
        /// </summary>
        private void ShowEditPanel()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("ShowEditPanel: 开始显示编辑面板");

                if (EmptyStatePanel != null)
                {
                    EmptyStatePanel.Visibility = Visibility.Collapsed;
                    System.Diagnostics.Debug.WriteLine("ShowEditPanel: 隐藏空状态面板");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ShowEditPanel: EmptyStatePanel为null");
                }

                if (EditPanel != null)
                {
                    EditPanel.Visibility = Visibility.Visible;
                    System.Diagnostics.Debug.WriteLine("ShowEditPanel: 显示编辑面板");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ShowEditPanel: EditPanel为null");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ShowEditPanel: 显示面板失败 - {ex.Message}");
                MessageBox.Show($"显示编辑面板失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 隐藏编辑面板
        /// </summary>
        private void HideEditPanel()
        {
            EmptyStatePanel.Visibility = Visibility.Visible;
            EditPanel.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 在项目切换后刷新时间线数据。
        /// </summary>
        /// <param name="projectId">当前项目标识。</param>
        /// <param name="projectName">当前项目名称。</param>
        public async Task RefreshOnProjectChangedAsync(Guid? projectId, string? projectName)
        {
            _currentProjectId = projectId ?? Guid.Empty;
            await LoadTimelineEventsAsync();
        }

        /// <summary>
        /// 在导航到当前视图时刷新对应项目的时间线数据。
        /// </summary>
        /// <param name="context">导航上下文。</param>
        public void OnNavigatedTo(NavigationContext context)
        {
            _currentProjectId = context.ProjectId ?? Guid.Empty;
            _ = LoadTimelineEventsAsync();
        }

        private async Task PersistTimelineEventsAsync()
        {
            if (_currentProjectId == Guid.Empty || _timelineDataService == null)
            {
                return;
            }

            foreach (var timelineEvent in TimelineEvents)
            {
                timelineEvent.ParticipantCount = timelineEvent.Participants?.Count ?? 0;
            }

            await _timelineDataService.SaveTimelineEventsAsync(_currentProjectId, TimelineEvents);
        }

        private async Task ImportTimelineAsync()
        {
            try
            {
                if (!EnsureCurrentProject("导入时间线"))
                {
                    return;
                }

                if (_timelineDataService == null)
                {
                    MessageBox.Show("时间线数据服务未初始化。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var openDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "导入时间线数据",
                    Filter = "时间线文件 (*.json)|*.json|所有文件 (*.*)|*.*"
                };

                if (openDialog.ShowDialog() == true)
                {
                    var importedEvents = await _timelineDataService.ImportTimelineEventsAsync(openDialog.FileName);
                    TimelineEvents.Clear();
                    foreach (var importedEvent in importedEvents)
                    {
                        importedEvent.ParticipantCount = importedEvent.Participants.Count;
                        TimelineEvents.Add(importedEvent);
                    }

                    await PersistTimelineEventsAsync();
                    TimelineListControl.ItemsSource = TimelineEvents;
                    UpdateStatistics();
                    MessageBox.Show($"已成功导入 {TimelineEvents.Count} 条时间线事件。", "导入成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入时间线数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExportTimelineAsync()
        {
            try
            {
                if (!EnsureCurrentProject("导出时间线"))
                {
                    return;
                }

                if (_timelineDataService == null)
                {
                    MessageBox.Show("时间线数据服务未初始化。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "导出时间线数据",
                    Filter = "时间线文件 (*.json)|*.json",
                    FileName = $"时间线_{DateTime.Now:yyyyMMdd_HHmmss}.json"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    await _timelineDataService.ExportTimelineEventsAsync(_currentProjectId, TimelineEvents, saveDialog.FileName);
                    MessageBox.Show($"时间线数据已导出到：{saveDialog.FileName}", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出时间线数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool EnsureCurrentProject(string actionName)
        {
            _currentProjectId = _projectContextService?.CurrentProjectId ?? Guid.Empty;
            if (_currentProjectId != Guid.Empty)
            {
                return true;
            }

            _currentProjectGuard?.TryGetCurrentProjectId(Window.GetWindow(this), actionName, out _);
            return false;
        }

        #endregion
    }

    #region ViewModel类

    /// <summary>
    /// 时间线事件视图模型
    /// </summary>
    public class TimelineEventViewModel
    {
        /// <summary>
        /// 事件标识。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 事件标题。
        /// </summary>
        public string Title { get; set; } = "";

        /// <summary>
        /// 事件类别。
        /// </summary>
        public string Category { get; set; } = "";

        /// <summary>
        /// 事件发生时间。
        /// </summary>
        public DateTime EventDate { get; set; }

        /// <summary>
        /// 事件地点。
        /// </summary>
        public string Location { get; set; } = "";

        /// <summary>
        /// 重要程度。
        /// </summary>
        public string Importance { get; set; } = "";

        /// <summary>
        /// 当前状态。
        /// </summary>
        public string Status { get; set; } = "";

        /// <summary>
        /// 事件描述。
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// 事件影响。
        /// </summary>
        public string Impact { get; set; } = "";

        /// <summary>
        /// 参与者数量。
        /// </summary>
        public int ParticipantCount { get; set; }

        /// <summary>
        /// 创建时间。
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 参与者列表。
        /// </summary>
        public List<EventParticipantViewModel> Participants { get; set; } = new();

        /// <summary>
        /// 事件时间显示文本。
        /// </summary>
        [JsonIgnore]
        public string TimeDisplay => EventDate.ToString("yyyy年MM月dd日");

        /// <summary>
        /// 根据类别获取图标
        /// </summary>
        [JsonIgnore]
        public string CategoryIcon
        {
            get
            {
                return Category switch
                {
                    "历史事件" => "History",
                    "剧情事件" => "Drama",
                    "角色事件" => "Account",
                    "势力事件" => "Castle",
                    "世界事件" => "Earth",
                    "修炼事件" => "Meditation",
                    _ => "Timeline"
                };
            }
        }

        /// <summary>
        /// 根据重要程度获取颜色画刷
        /// </summary>
        [JsonIgnore]
        public Brush ImportanceColor
        {
            get
            {
                var colorString = Importance switch
                {
                    "极高" => "#F44336", // 红色
                    "高" => "#FF9800",   // 橙色
                    "中" => "#2196F3",   // 蓝色
                    "低" => "#4CAF50",   // 绿色
                    _ => "#9E9E9E"       // 灰色
                };
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorString));
            }
        }

        /// <summary>
        /// 根据状态获取颜色画刷
        /// </summary>
        [JsonIgnore]
        public Brush StatusColor
        {
            get
            {
                var colorString = Status switch
                {
                    "已完成" => "#4CAF50", // 绿色
                    "进行中" => "#2196F3", // 蓝色
                    "计划中" => "#FF9800", // 橙色
                    "已取消" => "#9E9E9E", // 灰色
                    _ => "#9E9E9E"
                };
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorString));
            }
        }
    }

    /// <summary>
    /// 事件参与者视图模型
    /// </summary>
    public class EventParticipantViewModel
    {
        /// <summary>
        /// 参与者名称。
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// 参与者类型。
        /// </summary>
        public string Type { get; set; } = "";

        /// <summary>
        /// 在事件中的角色。
        /// </summary>
        public string Role { get; set; } = "";
    }

    #endregion
}
