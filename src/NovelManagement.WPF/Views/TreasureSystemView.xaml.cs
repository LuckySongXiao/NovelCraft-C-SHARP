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
    /// 灵宝体系管理视图
    /// </summary>
    public partial class TreasureSystemView : UserControl
    {
        #region 属性

        /// <summary>
        /// 灵宝列表
        /// </summary>
        public ObservableCollection<TreasureViewModel> Treasures { get; set; }

        /// <summary>
        /// 当前选中的灵宝
        /// </summary>
        public TreasureViewModel SelectedTreasure { get; set; }

        /// <summary>
        /// 选择灵宝命令
        /// </summary>
        public ICommand SelectTreasureCommand { get; set; }

        /// <summary>
        /// 日志记录器
        /// </summary>
        private readonly ILogger<TreasureSystemView>? _logger;

        /// <summary>
        /// AI助手服务
        /// </summary>
        private readonly IAIAssistantService? _aiAssistantService;

        #endregion

        #region 构造函数

        public TreasureSystemView()
        {
            InitializeComponent();

            // 获取服务
            try
            {
                var serviceProvider = App.ServiceProvider;
                _logger = serviceProvider?.GetService<ILogger<TreasureSystemView>>();
                _aiAssistantService = serviceProvider?.GetService<IAIAssistantService>();
            }
            catch (Exception ex)
            {
                // 如果获取服务失败，记录错误但不影响界面初始化
                System.Diagnostics.Debug.WriteLine($"获取服务失败: {ex.Message}");
            }

            InitializeData();
            InitializeCommands();
            LoadTreasures();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化数据
        /// </summary>
        private void InitializeData()
        {
            Treasures = new ObservableCollection<TreasureViewModel>();
            
            // 设置数据上下文
            this.DataContext = this;
        }

        /// <summary>
        /// 初始化命令
        /// </summary>
        private void InitializeCommands()
        {
            SelectTreasureCommand = new RelayCommand<TreasureViewModel>(SelectTreasure);
        }

        #endregion

        #region 数据加载

        /// <summary>
        /// 加载灵宝数据
        /// </summary>
        private void LoadTreasures()
        {
            try
            {
                // 模拟数据 - 实际应用中应该从服务层获取
                var treasures = new List<TreasureViewModel>
                {
                    new TreasureViewModel
                    {
                        Id = 1,
                        Name = "千面面具",
                        Type = "法器类",
                        Grade = "神品",
                        SpiritLevel = "超越灵性",
                        Description = "千面郎君的本命法器，可幻化千种面容，觉醒前世记忆",
                        SpiritPower = "10000",
                        ElementAffinity = "幻术、因果",
                        SpecialAbility = "千面幻化、记忆觉醒、因果轮回",
                        UsageLimit = "需要血脉认主，使用会消耗寿命",
                        CraftingMaterials = "千年梧桐木、九天玄铁、轮回石、因果丝",
                        CraftingMethod = "需要仙君级别的炼器师，配合天劫淬炼",
                        CraftingDifficulty = "传说",
                        CreatedAt = DateTime.Now.AddDays(-365)
                    },
                    new TreasureViewModel
                    {
                        Id = 2,
                        Name = "青云剑",
                        Type = "武器类",
                        Grade = "宝品",
                        SpiritLevel = "中级灵性",
                        Description = "青云宗传承仙剑，剑身如青云缭绕，锋利无比",
                        SpiritPower = "3000",
                        ElementAffinity = "风、雷",
                        SpecialAbility = "青云剑气、风雷斩",
                        UsageLimit = "需要剑修血脉",
                        CraftingMaterials = "青云石、雷击木、风灵珠",
                        CraftingMethod = "传统剑器炼制法，配合风雷淬炼",
                        CraftingDifficulty = "困难",
                        CreatedAt = DateTime.Now.AddDays(-200)
                    },
                    new TreasureViewModel
                    {
                        Id = 3,
                        Name = "九转金丹",
                        Type = "丹药类",
                        Grade = "王品",
                        SpiritLevel = "高级灵性",
                        Description = "传说中的九转金丹，可助修士突破大境界",
                        SpiritPower = "5000",
                        ElementAffinity = "金、火",
                        SpecialAbility = "境界突破、寿命延长、毒素清除",
                        UsageLimit = "一生只能服用一颗",
                        CraftingMaterials = "千年人参、九阳花、金精石、天火精华",
                        CraftingMethod = "九转炼丹法，需要九九八十一天",
                        CraftingDifficulty = "极难",
                        CreatedAt = DateTime.Now.AddDays(-150)
                    },
                    new TreasureViewModel
                    {
                        Id = 4,
                        Name = "护心镜",
                        Type = "防具类",
                        Grade = "灵品",
                        SpiritLevel = "初级灵性",
                        Description = "古老的护心镜，可抵御心魔侵扰和精神攻击",
                        SpiritPower = "1500",
                        ElementAffinity = "光、神",
                        SpecialAbility = "心魔抵御、精神防护",
                        UsageLimit = "需要正义之心",
                        CraftingMaterials = "白玉、圣光石、净心草",
                        CraftingMethod = "传统防具炼制法",
                        CraftingDifficulty = "普通",
                        CreatedAt = DateTime.Now.AddDays(-100)
                    },
                    new TreasureViewModel
                    {
                        Id = 5,
                        Name = "五行阵盘",
                        Type = "阵法类",
                        Grade = "皇品",
                        SpiritLevel = "完整灵性",
                        Description = "蕴含五行之力的阵法盘，可布置各种五行阵法",
                        SpiritPower = "8000",
                        ElementAffinity = "金木水火土",
                        SpecialAbility = "五行阵法、元素操控",
                        UsageLimit = "需要阵法师修为",
                        CraftingMaterials = "五行精石、阵法符文、天地灵气",
                        CraftingMethod = "五行炼制法，需要五行平衡",
                        CraftingDifficulty = "极难",
                        CreatedAt = DateTime.Now.AddDays(-80)
                    }
                };

                Treasures.Clear();
                foreach (var treasure in treasures)
                {
                    Treasures.Add(treasure);
                }

                // 设置列表数据源
                TreasureListControl.ItemsSource = Treasures;

                // 更新统计信息
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载灵宝数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
            FilterTreasures();
        }

        /// <summary>
        /// 灵宝类型筛选变化
        /// </summary>
        private void TreasureTypeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterTreasures();
        }

        /// <summary>
        /// 品级筛选变化
        /// </summary>
        private void GradeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterTreasures();
        }

        /// <summary>
        /// 筛选灵宝
        /// </summary>
        private void FilterTreasures()
        {
            var searchText = SearchTextBox?.Text?.ToLower() ?? "";
            var selectedType = (TreasureTypeFilterComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString();
            var selectedGrade = (GradeFilterComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString();

            var filteredTreasures = Treasures.Where(t =>
                (string.IsNullOrEmpty(searchText) || 
                 t.Name.ToLower().Contains(searchText) || 
                 t.Description.ToLower().Contains(searchText) ||
                 t.Type.ToLower().Contains(searchText)) &&
                (selectedType == "全部类型" || selectedType == null || t.Type == selectedType) &&
                (selectedGrade == "全部品级" || selectedGrade == null || t.Grade == selectedGrade)
            ).ToList();

            TreasureListControl.ItemsSource = filteredTreasures;
        }

        /// <summary>
        /// 选择灵宝
        /// </summary>
        private void SelectTreasure(TreasureViewModel treasure)
        {
            if (treasure == null) return;

            SelectedTreasure = treasure;
            LoadTreasureDetails(treasure);
            ShowEditPanel();
        }

        /// <summary>
        /// 加载灵宝详情
        /// </summary>
        private void LoadTreasureDetails(TreasureViewModel treasure)
        {
            // 填充基本信息
            TreasureNameTextBox.Text = treasure.Name;
            TreasureDescriptionTextBox.Text = treasure.Description;
            
            // 设置类型选择
            foreach (ComboBoxItem item in TreasureTypeComboBox.Items)
            {
                if (item.Content.ToString() == treasure.Type)
                {
                    TreasureTypeComboBox.SelectedItem = item;
                    break;
                }
            }

            // 设置品级选择
            foreach (ComboBoxItem item in GradeComboBox.Items)
            {
                if (item.Content.ToString() == treasure.Grade)
                {
                    GradeComboBox.SelectedItem = item;
                    break;
                }
            }

            // 设置灵性等级选择
            foreach (ComboBoxItem item in SpiritLevelComboBox.Items)
            {
                if (item.Content.ToString() == treasure.SpiritLevel)
                {
                    SpiritLevelComboBox.SelectedItem = item;
                    break;
                }
            }

            // 填充属性信息
            SpiritPowerTextBox.Text = treasure.SpiritPower;
            ElementAffinityTextBox.Text = treasure.ElementAffinity;
            SpecialAbilityTextBox.Text = treasure.SpecialAbility;
            UsageLimitTextBox.Text = treasure.UsageLimit;

            // 填充炼制信息
            CraftingMaterialsTextBox.Text = treasure.CraftingMaterials;
            CraftingMethodTextBox.Text = treasure.CraftingMethod;

            // 设置炼制难度选择
            foreach (ComboBoxItem item in CraftingDifficultyComboBox.Items)
            {
                if (item.Content.ToString() == treasure.CraftingDifficulty)
                {
                    CraftingDifficultyComboBox.SelectedItem = item;
                    break;
                }
            }
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
            SelectedTreasure = null;
        }

        #endregion

        #region 按钮事件

        /// <summary>
        /// 新建灵宝
        /// </summary>
        private void AddTreasure_Click(object sender, RoutedEventArgs e)
        {
            var newTreasure = new TreasureViewModel
            {
                Id = 0,
                Name = "",
                Type = "法器类",
                Grade = "凡品",
                SpiritLevel = "无灵性",
                Description = "",
                SpiritPower = "",
                ElementAffinity = "",
                SpecialAbility = "",
                UsageLimit = "",
                CraftingMaterials = "",
                CraftingMethod = "",
                CraftingDifficulty = "简单",
                CreatedAt = DateTime.Now
            };

            SelectedTreasure = newTreasure;
            LoadTreasureDetails(newTreasure);
            ShowEditPanel();

            // 聚焦到名称输入框
            TreasureNameTextBox.Focus();
        }

        /// <summary>
        /// 导入灵宝数据
        /// </summary>
        private void ImportTreasure_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("导入功能开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 导出灵宝数据
        /// </summary>
        private void ExportTreasure_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("导出功能开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 保存灵宝
        /// </summary>
        private void SaveTreasure_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SelectedTreasure == null) return;

                // 验证输入
                if (string.IsNullOrWhiteSpace(TreasureNameTextBox.Text))
                {
                    MessageBox.Show("请输入灵宝名称", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 更新灵宝信息
                SelectedTreasure.Name = TreasureNameTextBox.Text.Trim();
                SelectedTreasure.Description = TreasureDescriptionTextBox.Text.Trim();
                SelectedTreasure.Type = (TreasureTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "法器类";
                SelectedTreasure.Grade = (GradeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "凡品";
                SelectedTreasure.SpiritLevel = (SpiritLevelComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "无灵性";
                SelectedTreasure.SpiritPower = SpiritPowerTextBox.Text.Trim();
                SelectedTreasure.ElementAffinity = ElementAffinityTextBox.Text.Trim();
                SelectedTreasure.SpecialAbility = SpecialAbilityTextBox.Text.Trim();
                SelectedTreasure.UsageLimit = UsageLimitTextBox.Text.Trim();
                SelectedTreasure.CraftingMaterials = CraftingMaterialsTextBox.Text.Trim();
                SelectedTreasure.CraftingMethod = CraftingMethodTextBox.Text.Trim();
                SelectedTreasure.CraftingDifficulty = (CraftingDifficultyComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "简单";

                // 如果是新建灵宝，添加到列表
                if (SelectedTreasure.Id == 0)
                {
                    SelectedTreasure.Id = Treasures.Count > 0 ? Treasures.Max(t => t.Id) + 1 : 1;
                    Treasures.Add(SelectedTreasure);
                }

                // 这里应该调用服务层保存数据
                MessageBox.Show("灵宝保存成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);

                // 刷新列表
                FilterTreasures();
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
            var dialog = new AIAssistantDialog("灵宝体系管理", contextString);
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
                ["interfaceType"] = "灵宝体系管理",
                ["totalTreasures"] = Treasures.Count,
                ["selectedTreasure"] = SelectedTreasure,
                ["treasureTypes"] = new[] { "武器类", "防具类", "饰品类", "法器类", "丹药类", "符箓类", "阵法类", "特殊类" },
                ["gradeTypes"] = new[] { "凡品", "灵品", "宝品", "王品", "皇品", "帝品", "圣品", "神品" },
                ["spiritLevels"] = new[] { "无灵性", "微弱灵性", "初级灵性", "中级灵性", "高级灵性", "完整灵性", "超越灵性" },
                ["craftingDifficulties"] = new[] { "简单", "普通", "困难", "极难", "传说" }
            };

            if (SelectedTreasure != null)
            {
                context["currentTreasureName"] = SelectedTreasure.Name;
                context["currentTreasureType"] = SelectedTreasure.Type;
                context["currentTreasureGrade"] = SelectedTreasure.Grade;
                context["currentTreasureSpiritLevel"] = SelectedTreasure.SpiritLevel;
                context["currentTreasureDescription"] = SelectedTreasure.Description;
                context["currentTreasureProperties"] = new
                {
                    SpiritPower = SelectedTreasure.SpiritPower,
                    ElementAffinity = SelectedTreasure.ElementAffinity,
                    SpecialAbility = SelectedTreasure.SpecialAbility,
                    UsageLimit = SelectedTreasure.UsageLimit
                };
                context["currentTreasureCrafting"] = new
                {
                    Materials = SelectedTreasure.CraftingMaterials,
                    Method = SelectedTreasure.CraftingMethod,
                    Difficulty = SelectedTreasure.CraftingDifficulty
                };
            }

            // 添加统计信息
            var typeStats = Treasures.GroupBy(t => t.Type)
                .ToDictionary(g => g.Key, g => g.Count());
            context["typeStatistics"] = typeStats;

            var gradeStats = Treasures.GroupBy(t => t.Grade)
                .ToDictionary(g => g.Key, g => g.Count());
            context["gradeStatistics"] = gradeStats;

            return context;
        }

        #endregion
    }

    #region 视图模型

    /// <summary>
    /// 灵宝视图模型
    /// </summary>
    public class TreasureViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Grade { get; set; }
        public string SpiritLevel { get; set; }
        public string Description { get; set; }
        public string SpiritPower { get; set; }
        public string ElementAffinity { get; set; }
        public string SpecialAbility { get; set; }
        public string UsageLimit { get; set; }
        public string CraftingMaterials { get; set; }
        public string CraftingMethod { get; set; }
        public string CraftingDifficulty { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    #endregion
}
