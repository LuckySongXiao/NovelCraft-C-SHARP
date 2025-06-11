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
    /// 司法体系管理视图
    /// </summary>
    public partial class JudicialSystemView : UserControl
    {
        #region 属性

        /// <summary>
        /// 司法体系列表
        /// </summary>
        public ObservableCollection<JudicialSystemViewModel> JudicialSystems { get; set; }

        /// <summary>
        /// 当前选中的司法体系
        /// </summary>
        public JudicialSystemViewModel SelectedJudicialSystem { get; set; }

        /// <summary>
        /// 法院列表
        /// </summary>
        public ObservableCollection<CourtViewModel> Courts { get; set; }

        /// <summary>
        /// 选择司法体系命令
        /// </summary>
        public ICommand SelectJudicialSystemCommand { get; set; }

        /// <summary>
        /// 日志记录器
        /// </summary>
        private readonly ILogger<JudicialSystemView>? _logger;

        /// <summary>
        /// AI助手服务
        /// </summary>
        private readonly IAIAssistantService? _aiAssistantService;

        #endregion

        #region 构造函数

        public JudicialSystemView()
        {
            InitializeComponent();

            // 获取服务
            try
            {
                var serviceProvider = App.ServiceProvider;
                _logger = serviceProvider?.GetService<ILogger<JudicialSystemView>>();
                _aiAssistantService = serviceProvider?.GetService<IAIAssistantService>();
            }
            catch (Exception ex)
            {
                // 如果获取服务失败，记录错误但不影响界面初始化
                System.Diagnostics.Debug.WriteLine($"获取服务失败: {ex.Message}");
            }

            InitializeData();
            InitializeCommands();
            LoadJudicialSystems();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化数据
        /// </summary>
        private void InitializeData()
        {
            JudicialSystems = new ObservableCollection<JudicialSystemViewModel>();
            Courts = new ObservableCollection<CourtViewModel>();
            
            // 设置数据上下文
            this.DataContext = this;
        }

        /// <summary>
        /// 初始化命令
        /// </summary>
        private void InitializeCommands()
        {
            SelectJudicialSystemCommand = new RelayCommand<JudicialSystemViewModel>(SelectJudicialSystem);
        }

        #endregion

        #region 数据加载

        /// <summary>
        /// 加载司法体系数据
        /// </summary>
        private void LoadJudicialSystems()
        {
            try
            {
                // 模拟数据 - 实际应用中应该从服务层获取
                var judicialSystems = new List<JudicialSystemViewModel>
                {
                    new JudicialSystemViewModel
                    {
                        Id = 1,
                        Name = "修仙界司法体系",
                        LegalSystemType = "成文法系",
                        Jurisdiction = "修仙界全域",
                        DimensionId = "DIM-001",
                        Description = "修仙界统一的司法体系，以修仙法典为基础，管辖所有修仙者的法律事务",
                        CourtCount = 12,
                        CreatedAt = DateTime.Now.AddDays(-60)
                    },
                    new JudicialSystemViewModel
                    {
                        Id = 2,
                        Name = "凡人界司法体系",
                        LegalSystemType = "混合法系",
                        Jurisdiction = "凡人界各国",
                        DimensionId = "DIM-002",
                        Description = "凡人界各国的司法体系，结合成文法和习惯法，处理凡人间的纠纷",
                        CourtCount = 8,
                        CreatedAt = DateTime.Now.AddDays(-45)
                    },
                    new JudicialSystemViewModel
                    {
                        Id = 3,
                        Name = "宗门内部司法",
                        LegalSystemType = "宗教法系",
                        Jurisdiction = "各大宗门",
                        DimensionId = "DIM-001",
                        Description = "各大宗门内部的司法体系，以宗门戒律为准，处理门内弟子的违规行为",
                        CourtCount = 15,
                        CreatedAt = DateTime.Now.AddDays(-30)
                    },
                    new JudicialSystemViewModel
                    {
                        Id = 4,
                        Name = "魔界司法体系",
                        LegalSystemType = "判例法系",
                        Jurisdiction = "魔界领域",
                        DimensionId = "DIM-003",
                        Description = "魔界的司法体系，以强者为尊，通过判例积累形成法律体系",
                        CourtCount = 6,
                        CreatedAt = DateTime.Now.AddDays(-20)
                    }
                };

                JudicialSystems.Clear();
                foreach (var system in judicialSystems)
                {
                    JudicialSystems.Add(system);
                }

                // 设置列表数据源
                JudicialSystemListControl.ItemsSource = JudicialSystems;

                // 更新统计信息
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载司法体系数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
            FilterJudicialSystems();
        }

        /// <summary>
        /// 法律体系类型筛选变化
        /// </summary>
        private void LegalSystemFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterJudicialSystems();
        }

        /// <summary>
        /// 筛选司法体系
        /// </summary>
        private void FilterJudicialSystems()
        {
            var searchText = SearchTextBox?.Text?.ToLower() ?? "";
            var selectedType = (LegalSystemFilterComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString();

            var filteredSystems = JudicialSystems.Where(s =>
                (string.IsNullOrEmpty(searchText) || 
                 s.Name.ToLower().Contains(searchText) || 
                 s.Description.ToLower().Contains(searchText) ||
                 s.Jurisdiction.ToLower().Contains(searchText)) &&
                (selectedType == "全部类型" || selectedType == null || s.LegalSystemType == selectedType)
            ).ToList();

            JudicialSystemListControl.ItemsSource = filteredSystems;
        }

        /// <summary>
        /// 选择司法体系
        /// </summary>
        private void SelectJudicialSystem(JudicialSystemViewModel system)
        {
            if (system == null) return;

            SelectedJudicialSystem = system;
            LoadJudicialSystemDetails(system);
            ShowEditPanel();
        }

        /// <summary>
        /// 加载司法体系详情
        /// </summary>
        private void LoadJudicialSystemDetails(JudicialSystemViewModel system)
        {
            // 填充基本信息
            JudicialNameTextBox.Text = system.Name;
            JudicialDescriptionTextBox.Text = system.Description;
            JurisdictionTextBox.Text = system.Jurisdiction;
            DimensionIdTextBox.Text = system.DimensionId;
            
            // 设置法律体系类型选择
            foreach (ComboBoxItem item in LegalSystemTypeComboBox.Items)
            {
                if (item.Content.ToString() == system.LegalSystemType)
                {
                    LegalSystemTypeComboBox.SelectedItem = item;
                    break;
                }
            }

            // 加载法院列表
            LoadCourts(system.Id);
        }

        /// <summary>
        /// 加载法院列表
        /// </summary>
        private void LoadCourts(int systemId)
        {
            // 模拟数据 - 实际应用中应该从服务层获取
            var courts = new List<CourtViewModel>();
            
            if (systemId == 1) // 修仙界司法体系
            {
                courts.AddRange(new[]
                {
                    new CourtViewModel { Name = "修仙界最高法院", Level = "最高法院", Jurisdiction = "修仙界全域" },
                    new CourtViewModel { Name = "东域高级法院", Level = "高级法院", Jurisdiction = "东域各宗门" },
                    new CourtViewModel { Name = "西域高级法院", Level = "高级法院", Jurisdiction = "西域各宗门" },
                    new CourtViewModel { Name = "青云宗法院", Level = "专门法院", Jurisdiction = "青云宗内部" },
                    new CourtViewModel { Name = "天剑门法院", Level = "专门法院", Jurisdiction = "天剑门内部" }
                });
            }
            else if (systemId == 2) // 凡人界司法体系
            {
                courts.AddRange(new[]
                {
                    new CourtViewModel { Name = "大燕国最高法院", Level = "最高法院", Jurisdiction = "大燕国全境" },
                    new CourtViewModel { Name = "京城高级法院", Level = "高级法院", Jurisdiction = "京城及周边" },
                    new CourtViewModel { Name = "州府中级法院", Level = "中级法院", Jurisdiction = "各州府" },
                    new CourtViewModel { Name = "县级基层法院", Level = "基层法院", Jurisdiction = "各县" }
                });
            }

            Courts.Clear();
            foreach (var court in courts)
            {
                Courts.Add(court);
            }

            CourtListControl.ItemsSource = Courts;
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
            SelectedJudicialSystem = null;
        }

        #endregion

        #region 按钮事件

        /// <summary>
        /// 新建司法体系
        /// </summary>
        private void AddJudicialSystem_Click(object sender, RoutedEventArgs e)
        {
            var newSystem = new JudicialSystemViewModel
            {
                Id = 0,
                Name = "",
                LegalSystemType = "成文法系",
                Jurisdiction = "",
                DimensionId = "",
                Description = "",
                CourtCount = 0,
                CreatedAt = DateTime.Now
            };

            SelectedJudicialSystem = newSystem;
            LoadJudicialSystemDetails(newSystem);
            ShowEditPanel();

            // 清空法院列表
            Courts.Clear();
            CourtListControl.ItemsSource = Courts;

            // 聚焦到名称输入框
            JudicialNameTextBox.Focus();
        }

        /// <summary>
        /// 导入司法数据
        /// </summary>
        private void ImportJudicial_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("导入功能开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 导出司法数据
        /// </summary>
        private void ExportJudicial_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("导出功能开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 添加法院
        /// </summary>
        private void AddCourt_Click(object sender, RoutedEventArgs e)
        {
            var newCourt = new CourtViewModel
            {
                Name = "",
                Level = "基层法院",
                Jurisdiction = ""
            };

            Courts.Add(newCourt);
        }

        /// <summary>
        /// 删除法院
        /// </summary>
        private void RemoveCourt_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is CourtViewModel court)
            {
                Courts.Remove(court);
            }
        }

        /// <summary>
        /// 保存司法体系
        /// </summary>
        private void SaveJudicialSystem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SelectedJudicialSystem == null) return;

                // 验证输入
                if (string.IsNullOrWhiteSpace(JudicialNameTextBox.Text))
                {
                    MessageBox.Show("请输入司法体系名称", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(JurisdictionTextBox.Text))
                {
                    MessageBox.Show("请输入司法管辖区", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 更新司法体系信息
                SelectedJudicialSystem.Name = JudicialNameTextBox.Text.Trim();
                SelectedJudicialSystem.Description = JudicialDescriptionTextBox.Text.Trim();
                SelectedJudicialSystem.Jurisdiction = JurisdictionTextBox.Text.Trim();
                SelectedJudicialSystem.DimensionId = DimensionIdTextBox.Text.Trim();
                SelectedJudicialSystem.LegalSystemType = (LegalSystemTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "成文法系";
                SelectedJudicialSystem.CourtCount = Courts.Count;

                // 如果是新建司法体系，添加到列表
                if (SelectedJudicialSystem.Id == 0)
                {
                    SelectedJudicialSystem.Id = JudicialSystems.Count > 0 ? JudicialSystems.Max(s => s.Id) + 1 : 1;
                    JudicialSystems.Add(SelectedJudicialSystem);
                }

                // 这里应该调用服务层保存数据
                MessageBox.Show("司法体系保存成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);

                // 刷新列表
                FilterJudicialSystems();
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
            var dialog = new AIAssistantDialog("司法体系管理", contextString);
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
                ["interfaceType"] = "司法体系管理",
                ["totalJudicialSystems"] = JudicialSystems.Count,
                ["selectedJudicialSystem"] = SelectedJudicialSystem,
                ["legalSystemTypes"] = new[] { "成文法系", "判例法系", "宗教法系", "习惯法系", "混合法系" },
                ["courtLevels"] = new[] { "最高法院", "高级法院", "中级法院", "基层法院", "专门法院" }
            };

            if (SelectedJudicialSystem != null)
            {
                context["currentSystemName"] = SelectedJudicialSystem.Name;
                context["currentSystemType"] = SelectedJudicialSystem.LegalSystemType;
                context["currentSystemJurisdiction"] = SelectedJudicialSystem.Jurisdiction;
                context["currentSystemDimension"] = SelectedJudicialSystem.DimensionId;
                context["currentSystemDescription"] = SelectedJudicialSystem.Description;
                context["currentSystemCourtCount"] = SelectedJudicialSystem.CourtCount;
                context["currentSystemCourts"] = Courts.ToList();
            }

            // 添加统计信息
            var typeStats = JudicialSystems.GroupBy(s => s.LegalSystemType)
                .ToDictionary(g => g.Key, g => g.Count());
            context["typeStatistics"] = typeStats;

            var jurisdictionStats = JudicialSystems.GroupBy(s => s.Jurisdiction)
                .ToDictionary(g => g.Key, g => g.Count());
            context["jurisdictionStatistics"] = jurisdictionStats;

            return context;
        }

        #endregion
    }

    #region 视图模型

    /// <summary>
    /// 司法体系视图模型
    /// </summary>
    public class JudicialSystemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string LegalSystemType { get; set; }
        public string Jurisdiction { get; set; }
        public string DimensionId { get; set; }
        public string Description { get; set; }
        public int CourtCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// 法院视图模型
    /// </summary>
    public class CourtViewModel
    {
        public string Name { get; set; }
        public string Level { get; set; }
        public string Jurisdiction { get; set; }
    }

    #endregion
}
