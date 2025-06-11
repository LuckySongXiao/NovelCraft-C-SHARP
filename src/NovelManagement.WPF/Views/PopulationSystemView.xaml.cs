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
    /// 生民体系管理视图
    /// </summary>
    public partial class PopulationSystemView : UserControl
    {
        #region 属性

        /// <summary>
        /// 生民体系列表
        /// </summary>
        public ObservableCollection<PopulationSystemViewModel> PopulationSystems { get; set; }

        /// <summary>
        /// 当前选中的生民体系
        /// </summary>
        public PopulationSystemViewModel SelectedPopulationSystem { get; set; }

        /// <summary>
        /// 社会阶层列表
        /// </summary>
        public ObservableCollection<SocialClassViewModel> SocialClasses { get; set; }

        /// <summary>
        /// 选择生民体系命令
        /// </summary>
        public ICommand SelectPopulationSystemCommand { get; set; }

        /// <summary>
        /// 日志记录器
        /// </summary>
        private readonly ILogger<PopulationSystemView>? _logger;

        /// <summary>
        /// AI助手服务
        /// </summary>
        private readonly IAIAssistantService? _aiAssistantService;

        #endregion

        #region 构造函数

        public PopulationSystemView()
        {
            InitializeComponent();

            // 获取服务
            try
            {
                var serviceProvider = App.ServiceProvider;
                _logger = serviceProvider?.GetService<ILogger<PopulationSystemView>>();
                _aiAssistantService = serviceProvider?.GetService<IAIAssistantService>();
            }
            catch (Exception ex)
            {
                // 如果获取服务失败，记录错误但不影响界面初始化
                System.Diagnostics.Debug.WriteLine($"获取服务失败: {ex.Message}");
            }

            InitializeData();
            InitializeCommands();
            LoadPopulationSystems();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化数据
        /// </summary>
        private void InitializeData()
        {
            PopulationSystems = new ObservableCollection<PopulationSystemViewModel>();
            SocialClasses = new ObservableCollection<SocialClassViewModel>();
            
            // 设置数据上下文
            this.DataContext = this;
        }

        /// <summary>
        /// 初始化命令
        /// </summary>
        private void InitializeCommands()
        {
            SelectPopulationSystemCommand = new RelayCommand<PopulationSystemViewModel>(SelectPopulationSystem);
        }

        #endregion

        #region 数据加载

        /// <summary>
        /// 加载生民体系数据
        /// </summary>
        private void LoadPopulationSystems()
        {
            try
            {
                // 模拟数据 - 实际应用中应该从服务层获取
                var populationSystems = new List<PopulationSystemViewModel>
                {
                    new PopulationSystemViewModel
                    {
                        Id = 1,
                        Name = "修仙界生民体系",
                        RegionName = "修仙界",
                        DimensionId = "DIM-001",
                        Description = "修仙界的生民体系，包含修仙者和凡人两大群体，社会结构以修为等级为主导",
                        TotalPopulation = "10亿",
                        PopulationDensity = "中等",
                        GrowthRate = 2.5,
                        DevelopmentLevel = "修仙文明",
                        CreatedAt = DateTime.Now.AddDays(-90)
                    },
                    new PopulationSystemViewModel
                    {
                        Id = 2,
                        Name = "凡人界生民体系",
                        RegionName = "凡人界",
                        DimensionId = "DIM-002",
                        Description = "凡人界的生民体系，以传统封建社会为主，分为皇室、贵族、平民等阶层",
                        TotalPopulation = "50亿",
                        PopulationDensity = "高",
                        GrowthRate = 1.8,
                        DevelopmentLevel = "封建社会",
                        CreatedAt = DateTime.Now.AddDays(-75)
                    },
                    new PopulationSystemViewModel
                    {
                        Id = 3,
                        Name = "魔界生民体系",
                        RegionName = "魔界",
                        DimensionId = "DIM-003",
                        Description = "魔界的生民体系，以魔族为主体，社会结构严格按照血脉和实力划分",
                        TotalPopulation = "5亿",
                        PopulationDensity = "低",
                        GrowthRate = 1.2,
                        DevelopmentLevel = "修仙文明",
                        CreatedAt = DateTime.Now.AddDays(-60)
                    },
                    new PopulationSystemViewModel
                    {
                        Id = 4,
                        Name = "妖界生民体系",
                        RegionName = "妖界",
                        DimensionId = "DIM-004",
                        Description = "妖界的生民体系，以各种妖族为主，崇尚自然法则和血脉传承",
                        TotalPopulation = "8亿",
                        PopulationDensity = "中等",
                        GrowthRate = 2.0,
                        DevelopmentLevel = "修仙文明",
                        CreatedAt = DateTime.Now.AddDays(-45)
                    }
                };

                PopulationSystems.Clear();
                foreach (var system in populationSystems)
                {
                    PopulationSystems.Add(system);
                }

                // 设置列表数据源
                PopulationSystemListControl.ItemsSource = PopulationSystems;

                // 更新统计信息
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载生民体系数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
            FilterPopulationSystems();
        }

        /// <summary>
        /// 区域筛选变化
        /// </summary>
        private void RegionFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterPopulationSystems();
        }

        /// <summary>
        /// 筛选生民体系
        /// </summary>
        private void FilterPopulationSystems()
        {
            var searchText = SearchTextBox?.Text?.ToLower() ?? "";
            var selectedRegion = (RegionFilterComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString();

            var filteredSystems = PopulationSystems.Where(s =>
                (string.IsNullOrEmpty(searchText) || 
                 s.Name.ToLower().Contains(searchText) || 
                 s.Description.ToLower().Contains(searchText) ||
                 s.RegionName.ToLower().Contains(searchText)) &&
                (selectedRegion == "全部区域" || selectedRegion == null || s.RegionName == selectedRegion)
            ).ToList();

            PopulationSystemListControl.ItemsSource = filteredSystems;
        }

        /// <summary>
        /// 选择生民体系
        /// </summary>
        private void SelectPopulationSystem(PopulationSystemViewModel system)
        {
            if (system == null) return;

            SelectedPopulationSystem = system;
            LoadPopulationSystemDetails(system);
            ShowEditPanel();
        }

        /// <summary>
        /// 加载生民体系详情
        /// </summary>
        private void LoadPopulationSystemDetails(PopulationSystemViewModel system)
        {
            // 填充基本信息
            PopulationNameTextBox.Text = system.Name;
            PopulationDescriptionTextBox.Text = system.Description;
            RegionNameTextBox.Text = system.RegionName;
            DimensionIdTextBox.Text = system.DimensionId;
            TotalPopulationTextBox.Text = system.TotalPopulation;
            PopulationDensityTextBox.Text = system.PopulationDensity;
            GrowthRateTextBox.Text = system.GrowthRate.ToString();
            
            // 设置发展水平选择
            foreach (ComboBoxItem item in DevelopmentLevelComboBox.Items)
            {
                if (item.Content.ToString() == system.DevelopmentLevel)
                {
                    DevelopmentLevelComboBox.SelectedItem = item;
                    break;
                }
            }

            // 加载社会阶层列表
            LoadSocialClasses(system.Id);
        }

        /// <summary>
        /// 加载社会阶层列表
        /// </summary>
        private void LoadSocialClasses(int systemId)
        {
            // 模拟数据 - 实际应用中应该从服务层获取
            var socialClasses = new List<SocialClassViewModel>();
            
            if (systemId == 1) // 修仙界生民体系
            {
                socialClasses.AddRange(new[]
                {
                    new SocialClassViewModel { Name = "仙人", Population = "1万", Description = "已成仙的修炼者，地位最高" },
                    new SocialClassViewModel { Name = "修仙者", Population = "100万", Description = "正在修炼的人群，社会中坚力量" },
                    new SocialClassViewModel { Name = "修仙家族", Population = "1000万", Description = "有修仙传承的家族成员" },
                    new SocialClassViewModel { Name = "普通凡人", Population = "9亿", Description = "无法修炼的普通人群" }
                });
            }
            else if (systemId == 2) // 凡人界生民体系
            {
                socialClasses.AddRange(new[]
                {
                    new SocialClassViewModel { Name = "皇室", Population = "1000", Description = "皇帝及其家族成员" },
                    new SocialClassViewModel { Name = "贵族", Population = "10万", Description = "各级贵族和官员" },
                    new SocialClassViewModel { Name = "商人", Population = "1000万", Description = "从事商业活动的人群" },
                    new SocialClassViewModel { Name = "农民", Population = "40亿", Description = "从事农业生产的人群" },
                    new SocialClassViewModel { Name = "手工业者", Population = "9亿", Description = "从事手工业的人群" }
                });
            }

            SocialClasses.Clear();
            foreach (var socialClass in socialClasses)
            {
                SocialClasses.Add(socialClass);
            }

            SocialClassListControl.ItemsSource = SocialClasses;
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
            SelectedPopulationSystem = null;
        }

        #endregion

        #region 按钮事件

        /// <summary>
        /// 新建生民体系
        /// </summary>
        private void AddPopulationSystem_Click(object sender, RoutedEventArgs e)
        {
            var newSystem = new PopulationSystemViewModel
            {
                Id = 0,
                Name = "",
                RegionName = "",
                DimensionId = "",
                Description = "",
                TotalPopulation = "",
                PopulationDensity = "",
                GrowthRate = 0,
                DevelopmentLevel = "封建社会",
                CreatedAt = DateTime.Now
            };

            SelectedPopulationSystem = newSystem;
            LoadPopulationSystemDetails(newSystem);
            ShowEditPanel();

            // 清空社会阶层列表
            SocialClasses.Clear();
            SocialClassListControl.ItemsSource = SocialClasses;

            // 聚焦到名称输入框
            PopulationNameTextBox.Focus();
        }

        /// <summary>
        /// 导入生民数据
        /// </summary>
        private void ImportPopulation_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("导入功能开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 导出生民数据
        /// </summary>
        private void ExportPopulation_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("导出功能开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 添加社会阶层
        /// </summary>
        private void AddSocialClass_Click(object sender, RoutedEventArgs e)
        {
            var newClass = new SocialClassViewModel
            {
                Name = "",
                Population = "",
                Description = ""
            };

            SocialClasses.Add(newClass);
        }

        /// <summary>
        /// 删除社会阶层
        /// </summary>
        private void RemoveSocialClass_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is SocialClassViewModel socialClass)
            {
                SocialClasses.Remove(socialClass);
            }
        }

        /// <summary>
        /// 保存生民体系
        /// </summary>
        private void SavePopulationSystem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SelectedPopulationSystem == null) return;

                // 验证输入
                if (string.IsNullOrWhiteSpace(PopulationNameTextBox.Text))
                {
                    MessageBox.Show("请输入生民体系名称", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(RegionNameTextBox.Text))
                {
                    MessageBox.Show("请输入区域名称", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 更新生民体系信息
                SelectedPopulationSystem.Name = PopulationNameTextBox.Text.Trim();
                SelectedPopulationSystem.Description = PopulationDescriptionTextBox.Text.Trim();
                SelectedPopulationSystem.RegionName = RegionNameTextBox.Text.Trim();
                SelectedPopulationSystem.DimensionId = DimensionIdTextBox.Text.Trim();
                SelectedPopulationSystem.TotalPopulation = TotalPopulationTextBox.Text.Trim();
                SelectedPopulationSystem.PopulationDensity = PopulationDensityTextBox.Text.Trim();
                SelectedPopulationSystem.DevelopmentLevel = (DevelopmentLevelComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "封建社会";
                
                if (double.TryParse(GrowthRateTextBox.Text, out double growthRate))
                {
                    SelectedPopulationSystem.GrowthRate = growthRate;
                }

                // 如果是新建生民体系，添加到列表
                if (SelectedPopulationSystem.Id == 0)
                {
                    SelectedPopulationSystem.Id = PopulationSystems.Count > 0 ? PopulationSystems.Max(s => s.Id) + 1 : 1;
                    PopulationSystems.Add(SelectedPopulationSystem);
                }

                // 这里应该调用服务层保存数据
                MessageBox.Show("生民体系保存成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);

                // 刷新列表
                FilterPopulationSystems();
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
            var dialog = new AIAssistantDialog("生民体系管理", contextString);
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
                ["interfaceType"] = "生民体系管理",
                ["totalPopulationSystems"] = PopulationSystems.Count,
                ["selectedPopulationSystem"] = SelectedPopulationSystem,
                ["regions"] = new[] { "修仙界", "凡人界", "魔界", "妖界", "仙界" },
                ["developmentLevels"] = new[] { "原始社会", "奴隶社会", "封建社会", "资本主义社会", "修仙文明", "高等文明" }
            };

            if (SelectedPopulationSystem != null)
            {
                context["currentSystemName"] = SelectedPopulationSystem.Name;
                context["currentSystemRegion"] = SelectedPopulationSystem.RegionName;
                context["currentSystemDimension"] = SelectedPopulationSystem.DimensionId;
                context["currentSystemDescription"] = SelectedPopulationSystem.Description;
                context["currentSystemPopulation"] = new
                {
                    Total = SelectedPopulationSystem.TotalPopulation,
                    Density = SelectedPopulationSystem.PopulationDensity,
                    GrowthRate = SelectedPopulationSystem.GrowthRate,
                    DevelopmentLevel = SelectedPopulationSystem.DevelopmentLevel
                };
                context["currentSystemSocialClasses"] = SocialClasses.ToList();
            }

            // 添加统计信息
            var regionStats = PopulationSystems.GroupBy(s => s.RegionName)
                .ToDictionary(g => g.Key, g => g.Count());
            context["regionStatistics"] = regionStats;

            var developmentStats = PopulationSystems.GroupBy(s => s.DevelopmentLevel)
                .ToDictionary(g => g.Key, g => g.Count());
            context["developmentStatistics"] = developmentStats;

            // 添加人口分析数据
            var populationAnalysis = new
            {
                TotalSystems = PopulationSystems.Count,
                AverageGrowthRate = PopulationSystems.Average(s => s.GrowthRate),
                HighestGrowthRate = PopulationSystems.Max(s => s.GrowthRate),
                LowestGrowthRate = PopulationSystems.Min(s => s.GrowthRate)
            };
            context["populationAnalysis"] = populationAnalysis;

            return context;
        }

        #endregion
    }

    #region 视图模型

    /// <summary>
    /// 生民体系视图模型
    /// </summary>
    public class PopulationSystemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string RegionName { get; set; }
        public string DimensionId { get; set; }
        public string Description { get; set; }
        public string TotalPopulation { get; set; }
        public string PopulationDensity { get; set; }
        public double GrowthRate { get; set; }
        public string DevelopmentLevel { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// 社会阶层视图模型
    /// </summary>
    public class SocialClassViewModel
    {
        public string Name { get; set; }
        public string Population { get; set; }
        public string Description { get; set; }
    }

    #endregion
}
