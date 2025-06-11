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
    /// 装备体系管理视图
    /// </summary>
    public partial class EquipmentSystemView : UserControl
    {
        #region 属性

        /// <summary>
        /// 装备体系列表
        /// </summary>
        public ObservableCollection<EquipmentSystemViewModel> EquipmentSystems { get; set; }

        /// <summary>
        /// 装备等级列表
        /// </summary>
        public ObservableCollection<EquipmentLevelViewModel> EquipmentLevels { get; set; }

        /// <summary>
        /// 装备属性列表
        /// </summary>
        public ObservableCollection<EquipmentAttributeViewModel> EquipmentAttributes { get; set; }

        /// <summary>
        /// 当前选中的装备体系
        /// </summary>
        public EquipmentSystemViewModel SelectedEquipment { get; set; }

        /// <summary>
        /// 选择装备体系命令
        /// </summary>
        public ICommand SelectEquipmentCommand { get; private set; }

        /// <summary>
        /// 日志记录器
        /// </summary>
        private readonly ILogger<EquipmentSystemView>? _logger;

        /// <summary>
        /// AI助手服务
        /// </summary>
        private readonly IAIAssistantService? _aiAssistantService;

        #endregion

        #region 构造函数

        public EquipmentSystemView()
        {
            InitializeComponent();

            // 获取服务
            try
            {
                var serviceProvider = App.ServiceProvider;
                _logger = serviceProvider?.GetService<ILogger<EquipmentSystemView>>();
                _aiAssistantService = serviceProvider?.GetService<IAIAssistantService>();
            }
            catch (Exception ex)
            {
                // 如果获取服务失败，记录错误但不影响界面初始化
                System.Diagnostics.Debug.WriteLine($"获取服务失败: {ex.Message}");
            }

            InitializeData();
            InitializeCommands();
            LoadEquipmentSystems();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化数据
        /// </summary>
        private void InitializeData()
        {
            EquipmentSystems = new ObservableCollection<EquipmentSystemViewModel>();
            EquipmentLevels = new ObservableCollection<EquipmentLevelViewModel>();
            EquipmentAttributes = new ObservableCollection<EquipmentAttributeViewModel>();

            // 设置数据上下文
            this.DataContext = this;
        }

        /// <summary>
        /// 初始化命令
        /// </summary>
        private void InitializeCommands()
        {
            SelectEquipmentCommand = new RelayCommand<EquipmentSystemViewModel>(SelectEquipmentSystem);
        }

        /// <summary>
        /// 加载装备体系数据
        /// </summary>
        private void LoadEquipmentSystems()
        {
            try
            {
                // 模拟数据 - 实际应用中应该从服务层获取
                var equipments = new List<EquipmentSystemViewModel>
                {
                    new EquipmentSystemViewModel
                    {
                        Id = 1,
                        Name = "仙剑系列",
                        Category = "武器",
                        Description = "修仙者使用的各种仙剑，具有强大的攻击力和特殊效果",
                        LevelCount = 10,
                        AttributeCount = 8,
                        CreatedAt = DateTime.Now.AddDays(-30)
                    },
                    new EquipmentSystemViewModel
                    {
                        Id = 2,
                        Name = "护甲系列",
                        Category = "防具",
                        Description = "各种防护装备，能够抵御物理和法术攻击",
                        LevelCount = 8,
                        AttributeCount = 6,
                        CreatedAt = DateTime.Now.AddDays(-25)
                    },
                    new EquipmentSystemViewModel
                    {
                        Id = 3,
                        Name = "灵宝系列",
                        Category = "法宝",
                        Description = "蕴含天地灵气的宝物，具有神奇的功效",
                        LevelCount = 12,
                        AttributeCount = 10,
                        CreatedAt = DateTime.Now.AddDays(-20)
                    },
                    new EquipmentSystemViewModel
                    {
                        Id = 4,
                        Name = "丹药系列",
                        Category = "丹药",
                        Description = "炼丹师炼制的各种丹药，能够提升修为和治疗伤势",
                        LevelCount = 9,
                        AttributeCount = 5,
                        CreatedAt = DateTime.Now.AddDays(-15)
                    }
                };

                EquipmentSystems.Clear();
                foreach (var equipment in equipments)
                {
                    EquipmentSystems.Add(equipment);
                }

                // 设置列表数据源
                EquipmentListControl.ItemsSource = EquipmentSystems;

                // 更新统计信息
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载装备体系数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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

        #region 搜索和筛选

        /// <summary>
        /// 搜索文本变化事件
        /// </summary>
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterEquipmentSystems();
        }

        /// <summary>
        /// 类别筛选变化事件
        /// </summary>
        private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterEquipmentSystems();
        }

        /// <summary>
        /// 筛选装备体系
        /// </summary>
        private void FilterEquipmentSystems()
        {
            var searchText = SearchTextBox?.Text?.ToLower() ?? "";
            var selectedCategory = (CategoryFilterComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString();

            var filteredEquipments = EquipmentSystems.Where(e =>
                (string.IsNullOrEmpty(searchText) || 
                 e.Name.ToLower().Contains(searchText) || 
                 e.Description.ToLower().Contains(searchText)) &&
                (selectedCategory == "全部类别" || selectedCategory == null || e.Category == selectedCategory)
            ).ToList();

            EquipmentListControl.ItemsSource = filteredEquipments;
        }

        #endregion

        #region 装备体系管理

        /// <summary>
        /// 添加装备体系
        /// </summary>
        private void AddEquipment_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 创建新的装备体系
                SelectedEquipment = new EquipmentSystemViewModel
                {
                    Id = 0, // 新建时ID为0
                    Name = "",
                    Category = "武器",
                    Description = "",
                    CreatedAt = DateTime.Now
                };

                // 清空等级和属性列表
                EquipmentLevels.Clear();
                EquipmentAttributes.Clear();

                // 显示编辑面板
                ShowEditPanel();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"创建装备体系失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 导入装备数据
        /// </summary>
        private void ImportEquipment_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "导入装备体系数据",
                    Filter = "JSON文件|*.json|CSV文件|*.csv|所有文件|*.*",
                    DefaultExt = "json"
                };

                if (dialog.ShowDialog() == true)
                {
                    // 这里应该实现实际的导入逻辑
                    MessageBox.Show($"已选择文件：{dialog.FileName}\n导入功能将在后续版本中完善。",
                        "导入", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 导出装备数据
        /// </summary>
        private void ExportEquipment_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "导出装备体系数据",
                    Filter = "JSON文件|*.json|CSV文件|*.csv|所有文件|*.*",
                    DefaultExt = "json",
                    FileName = $"装备体系数据_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (dialog.ShowDialog() == true)
                {
                    // 这里应该实现实际的导出逻辑
                    MessageBox.Show($"数据将导出到：{dialog.FileName}\n导出功能将在后续版本中完善。",
                        "导出", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 选择装备体系
        /// </summary>
        private void SelectEquipmentSystem(EquipmentSystemViewModel equipment)
        {
            if (equipment == null) return;

            SelectedEquipment = equipment;
            LoadEquipmentSystemDetails(equipment);
            ShowEditPanel();
        }

        /// <summary>
        /// 加载装备体系详情
        /// </summary>
        private void LoadEquipmentSystemDetails(EquipmentSystemViewModel equipment)
        {
            // 填充基本信息
            EquipmentNameTextBox.Text = equipment.Name;
            EquipmentDescriptionTextBox.Text = equipment.Description;
            
            // 设置装备类别选择
            foreach (ComboBoxItem item in EquipmentCategoryComboBox.Items)
            {
                if (item.Content.ToString() == equipment.Category)
                {
                    EquipmentCategoryComboBox.SelectedItem = item;
                    break;
                }
            }

            // 加载等级列表
            LoadEquipmentLevels(equipment.Id);
            
            // 加载属性列表
            LoadEquipmentAttributes(equipment.Id);
        }

        /// <summary>
        /// 加载装备等级
        /// </summary>
        private void LoadEquipmentLevels(int equipmentId)
        {
            // 模拟数据
            var levels = new List<EquipmentLevelViewModel>
            {
                new EquipmentLevelViewModel { Level = 1, Name = "凡品", Requirements = "普通材料" },
                new EquipmentLevelViewModel { Level = 2, Name = "精品", Requirements = "精良材料" },
                new EquipmentLevelViewModel { Level = 3, Name = "极品", Requirements = "稀有材料" }
            };

            EquipmentLevels.Clear();
            foreach (var level in levels)
            {
                EquipmentLevels.Add(level);
            }

            LevelListControl.ItemsSource = EquipmentLevels;
        }

        /// <summary>
        /// 加载装备属性
        /// </summary>
        private void LoadEquipmentAttributes(int equipmentId)
        {
            // 模拟数据
            var attributes = new List<EquipmentAttributeViewModel>
            {
                new EquipmentAttributeViewModel { Name = "攻击力", Value = "100-200", Description = "物理攻击伤害" },
                new EquipmentAttributeViewModel { Name = "耐久度", Value = "1000", Description = "装备使用次数" }
            };

            EquipmentAttributes.Clear();
            foreach (var attribute in attributes)
            {
                EquipmentAttributes.Add(attribute);
            }

            AttributeListControl.ItemsSource = EquipmentAttributes;
        }

        #endregion

        #region 等级管理

        /// <summary>
        /// 添加等级
        /// </summary>
        private void AddLevel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var newLevel = new EquipmentLevelViewModel
                {
                    Level = EquipmentLevels.Count + 1,
                    Name = "",
                    Requirements = ""
                };

                EquipmentLevels.Add(newLevel);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加等级失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 删除等级
        /// </summary>
        private void RemoveLevel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.CommandParameter is EquipmentLevelViewModel level)
                {
                    EquipmentLevels.Remove(level);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除等级失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region 属性管理

        /// <summary>
        /// 添加属性
        /// </summary>
        private void AddAttribute_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var newAttribute = new EquipmentAttributeViewModel
                {
                    Name = "",
                    Value = "",
                    Description = ""
                };

                EquipmentAttributes.Add(newAttribute);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加属性失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 删除属性
        /// </summary>
        private void RemoveAttribute_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.CommandParameter is EquipmentAttributeViewModel attribute)
                {
                    EquipmentAttributes.Remove(attribute);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除属性失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region 保存和取消

        /// <summary>
        /// 保存装备体系
        /// </summary>
        private void SaveEquipment_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 验证输入
                if (string.IsNullOrWhiteSpace(EquipmentNameTextBox.Text))
                {
                    MessageBox.Show("请输入装备体系名称", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 更新装备体系信息
                SelectedEquipment.Name = EquipmentNameTextBox.Text.Trim();
                SelectedEquipment.Description = EquipmentDescriptionTextBox.Text.Trim();
                SelectedEquipment.Category = (EquipmentCategoryComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "武器";
                SelectedEquipment.LevelCount = EquipmentLevels.Count;
                SelectedEquipment.AttributeCount = EquipmentAttributes.Count;

                // 如果是新建装备体系，添加到列表
                if (SelectedEquipment.Id == 0)
                {
                    SelectedEquipment.Id = EquipmentSystems.Count > 0 ? EquipmentSystems.Max(e => e.Id) + 1 : 1;
                    EquipmentSystems.Add(SelectedEquipment);
                }

                // 这里应该调用服务层保存数据
                MessageBox.Show("装备体系保存成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);

                // 刷新列表
                FilterEquipmentSystems();
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存装备体系失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                EquipmentNameTextBox.Text = "";
                EquipmentDescriptionTextBox.Text = "";
                EquipmentCategoryComboBox.SelectedIndex = 0;

                // 清空列表
                EquipmentLevels.Clear();
                EquipmentAttributes.Clear();

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
            var dialog = new AIAssistantDialog("装备体系管理", contextString);
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
                ["interfaceType"] = "装备体系管理",
                ["totalEquipmentSystems"] = EquipmentSystems.Count,
                ["selectedEquipment"] = SelectedEquipment,
                ["equipmentCategories"] = new[] { "武器", "防具", "饰品", "法宝", "丹药", "符箓" }
            };

            if (SelectedEquipment != null)
            {
                context["currentEquipmentName"] = SelectedEquipment.Name;
                context["currentEquipmentCategory"] = SelectedEquipment.Category;
                context["currentEquipmentDescription"] = SelectedEquipment.Description;
                context["levelCount"] = SelectedEquipment.LevelCount;
                context["attributeCount"] = SelectedEquipment.AttributeCount;
                context["equipmentLevels"] = EquipmentLevels.ToList();
                context["equipmentAttributes"] = EquipmentAttributes.ToList();
            }

            // 添加统计信息
            var categoryStats = EquipmentSystems.GroupBy(e => e.Category)
                .ToDictionary(g => g.Key, g => g.Count());
            context["categoryStatistics"] = categoryStats;

            return context;
        }

        #endregion
    }

    #region ViewModel类

    /// <summary>
    /// 装备体系视图模型
    /// </summary>
    public class EquipmentSystemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public string Description { get; set; } = "";
        public int LevelCount { get; set; }
        public int AttributeCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// 装备等级视图模型
    /// </summary>
    public class EquipmentLevelViewModel
    {
        public int Level { get; set; }
        public string Name { get; set; } = "";
        public string Requirements { get; set; } = "";
    }

    /// <summary>
    /// 装备属性视图模型
    /// </summary>
    public class EquipmentAttributeViewModel
    {
        public string Name { get; set; } = "";
        public string Value { get; set; } = "";
        public string Description { get; set; } = "";
    }

    #endregion
}
