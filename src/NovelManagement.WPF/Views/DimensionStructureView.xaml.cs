using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NovelManagement.WPF.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using NovelManagement.WPF.Services;

namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// 维度结构管理视图
    /// </summary>
    public partial class DimensionStructureView : UserControl
    {
        #region 属性

        /// <summary>
        /// 维度列表
        /// </summary>
        public ObservableCollection<DimensionViewModel> Dimensions { get; set; }

        /// <summary>
        /// 当前选中的维度
        /// </summary>
        public DimensionViewModel SelectedDimension { get; set; }

        /// <summary>
        /// 传送门列表
        /// </summary>
        public ObservableCollection<PortalViewModel> Portals { get; set; }

        /// <summary>
        /// 选择维度命令
        /// </summary>
        public ICommand SelectDimensionCommand { get; set; }

        /// <summary>
        /// 日志记录器
        /// </summary>
        private readonly ILogger<DimensionStructureView>? _logger;

        /// <summary>
        /// AI助手服务
        /// </summary>
        private readonly IAIAssistantService? _aiAssistantService;

        #endregion

        #region 构造函数

        public DimensionStructureView()
        {
            InitializeComponent();

            // 获取服务
            try
            {
                var serviceProvider = App.ServiceProvider;
                _logger = serviceProvider?.GetService<ILogger<DimensionStructureView>>();
                _aiAssistantService = serviceProvider?.GetService<IAIAssistantService>();
            }
            catch (Exception ex)
            {
                // 如果获取服务失败，记录错误但不影响界面初始化
                System.Diagnostics.Debug.WriteLine($"获取服务失败: {ex.Message}");
            }

            InitializeData();
            InitializeCommands();
            LoadDimensions();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化数据
        /// </summary>
        private void InitializeData()
        {
            Dimensions = new ObservableCollection<DimensionViewModel>();
            Portals = new ObservableCollection<PortalViewModel>();
            
            // 设置数据上下文
            this.DataContext = this;
        }

        /// <summary>
        /// 初始化命令
        /// </summary>
        private void InitializeCommands()
        {
            SelectDimensionCommand = new RelayCommand<DimensionViewModel>(SelectDimension);
        }

        #endregion

        #region 数据加载

        /// <summary>
        /// 加载维度数据
        /// </summary>
        private void LoadDimensions()
        {
            try
            {
                // 模拟数据 - 实际应用中应该从服务层获取
                var dimensions = new List<DimensionViewModel>
                {
                    new DimensionViewModel
                    {
                        Id = 1,
                        Name = "修仙界",
                        Type = "物质维度",
                        Stability = "稳定",
                        AccessLevel = "限制",
                        Description = "修仙者居住的主要维度，灵气浓郁，适合修炼",
                        EnvironmentType = "灵气环境",
                        Climate = "四季分明，灵气充沛",
                        EnergyLevel = "高",
                        DangerLevel = "中等",
                        CreatedAt = DateTime.Now.AddDays(-365)
                    },
                    new DimensionViewModel
                    {
                        Id = 2,
                        Name = "凡人界",
                        Type = "物质维度",
                        Stability = "极稳定",
                        AccessLevel = "公开",
                        Description = "普通人类居住的维度，灵气稀薄，科技发达",
                        EnvironmentType = "自然环境",
                        Climate = "多样化气候",
                        EnergyLevel = "低",
                        DangerLevel = "低",
                        CreatedAt = DateTime.Now.AddDays(-300)
                    },
                    new DimensionViewModel
                    {
                        Id = 3,
                        Name = "仙界",
                        Type = "精神维度",
                        Stability = "极稳定",
                        AccessLevel = "机密",
                        Description = "仙人居住的高等维度，法则完善，仙气浓郁",
                        EnvironmentType = "仙气环境",
                        Climate = "永恒春天",
                        EnergyLevel = "极高",
                        DangerLevel = "低",
                        CreatedAt = DateTime.Now.AddDays(-250)
                    },
                    new DimensionViewModel
                    {
                        Id = 4,
                        Name = "魔界",
                        Type = "能量维度",
                        Stability = "不稳定",
                        AccessLevel = "禁止",
                        Description = "魔族居住的维度，魔气浓郁，环境恶劣",
                        EnvironmentType = "魔气环境",
                        Climate = "永恒黑暗",
                        EnergyLevel = "高",
                        DangerLevel = "极高",
                        CreatedAt = DateTime.Now.AddDays(-200)
                    },
                    new DimensionViewModel
                    {
                        Id = 5,
                        Name = "虚空裂隙",
                        Type = "空间维度",
                        Stability = "极不稳定",
                        AccessLevel = "未知",
                        Description = "连接各个维度的空间裂隙，充满未知危险",
                        EnvironmentType = "虚空环境",
                        Climate = "无规律变化",
                        EnergyLevel = "混乱",
                        DangerLevel = "极高",
                        CreatedAt = DateTime.Now.AddDays(-150)
                    }
                };

                Dimensions.Clear();
                foreach (var dimension in dimensions)
                {
                    Dimensions.Add(dimension);
                }

                // 设置列表数据源
                DimensionListControl.ItemsSource = Dimensions;

                // 更新统计信息
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载维度数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 更新统计信息
        /// </summary>
        private void UpdateStatistics()
        {
            // 这里应该绑定到ViewModel的属性，暂时使用硬编码值
            // 实际应用中应该计算真实的统计数据
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 搜索文本变化
        /// </summary>
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterDimensions();
        }

        /// <summary>
        /// 维度类型筛选变化
        /// </summary>
        private void DimensionTypeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterDimensions();
        }

        /// <summary>
        /// 稳定性筛选变化
        /// </summary>
        private void StabilityFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterDimensions();
        }

        /// <summary>
        /// 筛选维度
        /// </summary>
        private void FilterDimensions()
        {
            var searchText = SearchTextBox?.Text?.ToLower() ?? "";
            var selectedType = (DimensionTypeFilterComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString();
            var selectedStability = (StabilityFilterComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString();

            var filteredDimensions = Dimensions.Where(d =>
                (string.IsNullOrEmpty(searchText) || 
                 d.Name.ToLower().Contains(searchText) || 
                 d.Description.ToLower().Contains(searchText) ||
                 d.Type.ToLower().Contains(searchText)) &&
                (selectedType == "全部类型" || selectedType == null || d.Type == selectedType) &&
                (selectedStability == "全部稳定性" || selectedStability == null || d.Stability == selectedStability)
            ).ToList();

            DimensionListControl.ItemsSource = filteredDimensions;
        }

        /// <summary>
        /// 选择维度
        /// </summary>
        private void SelectDimension(DimensionViewModel dimension)
        {
            if (dimension == null) return;

            SelectedDimension = dimension;
            LoadDimensionDetails(dimension);
            ShowEditPanel();
        }

        /// <summary>
        /// 加载维度详情
        /// </summary>
        private void LoadDimensionDetails(DimensionViewModel dimension)
        {
            // 填充基本信息
            DimensionNameTextBox.Text = dimension.Name;
            DimensionDescriptionTextBox.Text = dimension.Description;
            EnvironmentTypeTextBox.Text = dimension.EnvironmentType;
            ClimateTextBox.Text = dimension.Climate;
            EnergyLevelTextBox.Text = dimension.EnergyLevel;
            DangerLevelTextBox.Text = dimension.DangerLevel;
            
            // 设置类型选择
            foreach (ComboBoxItem item in DimensionTypeComboBox.Items)
            {
                if (item.Content.ToString() == dimension.Type)
                {
                    DimensionTypeComboBox.SelectedItem = item;
                    break;
                }
            }

            // 设置稳定性选择
            foreach (ComboBoxItem item in StabilityComboBox.Items)
            {
                if (item.Content.ToString() == dimension.Stability)
                {
                    StabilityComboBox.SelectedItem = item;
                    break;
                }
            }

            // 设置访问等级选择
            foreach (ComboBoxItem item in AccessLevelComboBox.Items)
            {
                if (item.Content.ToString() == dimension.AccessLevel)
                {
                    AccessLevelComboBox.SelectedItem = item;
                    break;
                }
            }

            // 加载传送门列表
            LoadPortals(dimension.Id);
        }

        /// <summary>
        /// 加载传送门列表
        /// </summary>
        private void LoadPortals(int dimensionId)
        {
            // 模拟数据 - 实际应用中应该从服务层获取
            var portals = new List<PortalViewModel>();
            
            if (dimensionId == 1) // 修仙界
            {
                portals.AddRange(new[]
                {
                    new PortalViewModel { Name = "天门", TargetDimension = "仙界", Status = "正常" },
                    new PortalViewModel { Name = "凡界通道", TargetDimension = "凡人界", Status = "正常" },
                    new PortalViewModel { Name = "魔界裂缝", TargetDimension = "魔界", Status = "关闭" }
                });
            }
            else if (dimensionId == 2) // 凡人界
            {
                portals.AddRange(new[]
                {
                    new PortalViewModel { Name = "修仙门", TargetDimension = "修仙界", Status = "限制" },
                    new PortalViewModel { Name = "秘境入口", TargetDimension = "虚空裂隙", Status = "不稳定" }
                });
            }

            Portals.Clear();
            foreach (var portal in portals)
            {
                Portals.Add(portal);
            }

            PortalListControl.ItemsSource = Portals;
        }

        /// <summary>
        /// 显示编辑面板
        /// </summary>
        private void ShowEditPanel()
        {
            EmptyStatePanel.Visibility = Visibility.Collapsed;
            EditPanel.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 隐藏编辑面板
        /// </summary>
        private void HideEditPanel()
        {
            EmptyStatePanel.Visibility = Visibility.Visible;
            EditPanel.Visibility = Visibility.Collapsed;
            SelectedDimension = null;
        }

        #endregion

        #region 按钮事件

        /// <summary>
        /// 新建维度
        /// </summary>
        private void AddDimension_Click(object sender, RoutedEventArgs e)
        {
            var newDimension = new DimensionViewModel
            {
                Id = 0,
                Name = "",
                Type = "物质维度",
                Stability = "稳定",
                AccessLevel = "公开",
                Description = "",
                EnvironmentType = "",
                Climate = "",
                EnergyLevel = "",
                DangerLevel = "",
                CreatedAt = DateTime.Now
            };

            SelectedDimension = newDimension;
            LoadDimensionDetails(newDimension);
            ShowEditPanel();

            // 清空传送门列表
            Portals.Clear();
            PortalListControl.ItemsSource = Portals;

            // 聚焦到名称输入框
            DimensionNameTextBox.Focus();
        }

        /// <summary>
        /// 导入维度数据
        /// </summary>
        private void ImportDimension_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("导入功能开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 导出维度数据
        /// </summary>
        private void ExportDimension_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("导出功能开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 添加传送门
        /// </summary>
        private void AddPortal_Click(object sender, RoutedEventArgs e)
        {
            var newPortal = new PortalViewModel
            {
                Name = "",
                TargetDimension = "",
                Status = "正常"
            };

            Portals.Add(newPortal);
        }

        /// <summary>
        /// 删除传送门
        /// </summary>
        private void RemovePortal_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is PortalViewModel portal)
            {
                Portals.Remove(portal);
            }
        }

        /// <summary>
        /// 保存维度
        /// </summary>
        private void SaveDimension_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SelectedDimension == null) return;

                // 验证输入
                if (string.IsNullOrWhiteSpace(DimensionNameTextBox.Text))
                {
                    MessageBox.Show("请输入维度名称", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 更新维度信息
                SelectedDimension.Name = DimensionNameTextBox.Text.Trim();
                SelectedDimension.Description = DimensionDescriptionTextBox.Text.Trim();
                SelectedDimension.Type = (DimensionTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "物质维度";
                SelectedDimension.Stability = (StabilityComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "稳定";
                SelectedDimension.AccessLevel = (AccessLevelComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "公开";
                SelectedDimension.EnvironmentType = EnvironmentTypeTextBox.Text.Trim();
                SelectedDimension.Climate = ClimateTextBox.Text.Trim();
                SelectedDimension.EnergyLevel = EnergyLevelTextBox.Text.Trim();
                SelectedDimension.DangerLevel = DangerLevelTextBox.Text.Trim();

                // 如果是新建维度，添加到列表
                if (SelectedDimension.Id == 0)
                {
                    SelectedDimension.Id = Dimensions.Count > 0 ? Dimensions.Max(d => d.Id) + 1 : 1;
                    Dimensions.Add(SelectedDimension);
                }

                // 这里应该调用服务层保存数据
                MessageBox.Show("维度保存成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);

                // 刷新列表
                FilterDimensions();
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 取消编辑
        /// </summary>
        private void CancelEdit_Click(object sender, RoutedEventArgs e)
        {
            HideEditPanel();
        }

        #endregion

        #region AI助手功能

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
            var context = GetCurrentContext();
            var contextString = System.Text.Json.JsonSerializer.Serialize(context, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            var dialog = new AIAssistantDialog("维度结构管理", contextString);
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();
        }

        /// <summary>
        /// 获取当前上下文信息
        /// </summary>
        /// <returns>上下文信息</returns>
        private Dictionary<string, object> GetCurrentContext()
        {
            var context = new Dictionary<string, object>
            {
                ["interfaceType"] = "维度结构管理",
                ["totalDimensions"] = Dimensions.Count,
                ["selectedDimension"] = SelectedDimension,
                ["dimensionTypes"] = new[] { "物质维度", "精神维度", "能量维度", "时间维度", "空间维度", "混合维度" },
                ["stabilityLevels"] = new[] { "极稳定", "稳定", "不稳定", "极不稳定", "崩塌中" },
                ["accessLevels"] = new[] { "公开", "限制", "机密", "禁止", "未知" }
            };

            if (SelectedDimension != null)
            {
                context["currentDimensionName"] = SelectedDimension.Name;
                context["currentDimensionType"] = SelectedDimension.Type;
                context["currentDimensionStability"] = SelectedDimension.Stability;
                context["currentDimensionAccessLevel"] = SelectedDimension.AccessLevel;
                context["currentDimensionDescription"] = SelectedDimension.Description;
                context["currentDimensionEnvironment"] = new
                {
                    EnvironmentType = SelectedDimension.EnvironmentType,
                    Climate = SelectedDimension.Climate,
                    EnergyLevel = SelectedDimension.EnergyLevel,
                    DangerLevel = SelectedDimension.DangerLevel
                };
                context["currentDimensionPortals"] = Portals.ToList();
            }

            // 添加统计信息
            var typeStats = Dimensions.GroupBy(d => d.Type)
                .ToDictionary(g => g.Key, g => g.Count());
            context["typeStatistics"] = typeStats;

            var stabilityStats = Dimensions.GroupBy(d => d.Stability)
                .ToDictionary(g => g.Key, g => g.Count());
            context["stabilityStatistics"] = stabilityStats;

            return context;
        }

        #endregion
    }

    #region 视图模型

    /// <summary>
    /// 维度视图模型
    /// </summary>
    public class DimensionViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Stability { get; set; }
        public string AccessLevel { get; set; }
        public string Description { get; set; }
        public string EnvironmentType { get; set; }
        public string Climate { get; set; }
        public string EnergyLevel { get; set; }
        public string DangerLevel { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// 传送门视图模型
    /// </summary>
    public class PortalViewModel
    {
        public string Name { get; set; }
        public string TargetDimension { get; set; }
        public string Status { get; set; }
    }

    #endregion
}
