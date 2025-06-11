using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NovelManagement.WPF.Commands;

namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// 功法体系管理视图
    /// </summary>
    public partial class TechniqueSystemView : UserControl
    {
        #region 属性

        /// <summary>
        /// 功法体系列表
        /// </summary>
        public ObservableCollection<TechniqueSystemViewModel> TechniqueSystems { get; set; }

        /// <summary>
        /// 功法等级列表
        /// </summary>
        public ObservableCollection<TechniqueLevelViewModel> TechniqueLevels { get; set; }

        /// <summary>
        /// 功法招式列表
        /// </summary>
        public ObservableCollection<TechniqueMoveViewModel> TechniqueMoves { get; set; }

        /// <summary>
        /// 当前选中的功法体系
        /// </summary>
        public TechniqueSystemViewModel SelectedTechnique { get; set; }

        /// <summary>
        /// 选择功法体系命令
        /// </summary>
        public ICommand SelectTechniqueCommand { get; private set; }

        #endregion

        #region 构造函数

        public TechniqueSystemView()
        {
            InitializeComponent();
            InitializeData();
            InitializeCommands();
            LoadTechniqueSystems();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化数据
        /// </summary>
        private void InitializeData()
        {
            TechniqueSystems = new ObservableCollection<TechniqueSystemViewModel>();
            TechniqueLevels = new ObservableCollection<TechniqueLevelViewModel>();
            TechniqueMoves = new ObservableCollection<TechniqueMoveViewModel>();

            // 设置数据上下文
            this.DataContext = this;
        }

        /// <summary>
        /// 初始化命令
        /// </summary>
        private void InitializeCommands()
        {
            SelectTechniqueCommand = new RelayCommand<TechniqueSystemViewModel>(SelectTechniqueSystem);
        }

        /// <summary>
        /// 加载功法体系数据
        /// </summary>
        private void LoadTechniqueSystems()
        {
            try
            {
                // 模拟数据 - 实际应用中应该从服务层获取
                var techniques = new List<TechniqueSystemViewModel>
                {
                    new TechniqueSystemViewModel
                    {
                        Id = 1,
                        Name = "九阳神功",
                        Category = "修炼功法",
                        Grade = "天级",
                        Origin = "明教",
                        Description = "至阳至刚的内功心法，修炼至大成可刀枪不入、百毒不侵",
                        LevelCount = 9,
                        MoveCount = 12,
                        CreatedAt = DateTime.Now.AddDays(-30)
                    },
                    new TechniqueSystemViewModel
                    {
                        Id = 2,
                        Name = "降龙十八掌",
                        Category = "攻击功法",
                        Grade = "地级",
                        Origin = "丐帮",
                        Description = "天下第一掌法，刚猛无比，招招都有龙吟虎啸之威",
                        LevelCount = 18,
                        MoveCount = 18,
                        CreatedAt = DateTime.Now.AddDays(-25)
                    },
                    new TechniqueSystemViewModel
                    {
                        Id = 3,
                        Name = "凌波微步",
                        Category = "身法功法",
                        Grade = "玄级",
                        Origin = "逍遥派",
                        Description = "轻功身法，步法精妙，可在水上行走如履平地",
                        LevelCount = 8,
                        MoveCount = 64,
                        CreatedAt = DateTime.Now.AddDays(-20)
                    },
                    new TechniqueSystemViewModel
                    {
                        Id = 4,
                        Name = "易筋经",
                        Category = "修炼功法",
                        Grade = "仙级",
                        Origin = "少林寺",
                        Description = "佛门至高内功，可改变筋脉，脱胎换骨",
                        LevelCount = 12,
                        MoveCount = 0,
                        CreatedAt = DateTime.Now.AddDays(-15)
                    }
                };

                TechniqueSystems.Clear();
                foreach (var technique in techniques)
                {
                    TechniqueSystems.Add(technique);
                }

                // 设置列表数据源
                TechniqueListControl.ItemsSource = TechniqueSystems;

                // 更新统计信息
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载功法体系数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
            FilterTechniqueSystems();
        }

        /// <summary>
        /// 类别筛选变化事件
        /// </summary>
        private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterTechniqueSystems();
        }

        /// <summary>
        /// 筛选功法体系
        /// </summary>
        private void FilterTechniqueSystems()
        {
            var searchText = SearchTextBox?.Text?.ToLower() ?? "";
            var selectedCategory = (CategoryFilterComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString();

            var filteredTechniques = TechniqueSystems.Where(t =>
                (string.IsNullOrEmpty(searchText) || 
                 t.Name.ToLower().Contains(searchText) || 
                 t.Description.ToLower().Contains(searchText)) &&
                (selectedCategory == "全部类别" || selectedCategory == null || t.Category == selectedCategory)
            ).ToList();

            TechniqueListControl.ItemsSource = filteredTechniques;
        }

        #endregion

        #region 功法体系管理

        /// <summary>
        /// 添加功法体系
        /// </summary>
        private void AddTechnique_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 创建新的功法体系
                SelectedTechnique = new TechniqueSystemViewModel
                {
                    Id = 0, // 新建时ID为0
                    Name = "",
                    Category = "修炼功法",
                    Grade = "凡级",
                    Origin = "",
                    Description = "",
                    CreatedAt = DateTime.Now
                };

                // 清空等级和招式列表
                TechniqueLevels.Clear();
                TechniqueMoves.Clear();

                // 显示编辑面板
                ShowEditPanel();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"创建功法体系失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 导入功法数据
        /// </summary>
        private void ImportTechnique_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "导入功法体系数据",
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
        /// 导出功法数据
        /// </summary>
        private void ExportTechnique_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "导出功法体系数据",
                    Filter = "JSON文件|*.json|CSV文件|*.csv|所有文件|*.*",
                    DefaultExt = "json",
                    FileName = $"功法体系数据_{DateTime.Now:yyyyMMdd_HHmmss}"
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
            var dialog = new AIAssistantDialog("功法体系管理", contextString);
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();
        }

        /// <summary>
        /// 获取当前上下文信息
        /// </summary>
        private string GetCurrentContext()
        {
            var context = new StringBuilder();
            context.AppendLine("当前功能：功法体系管理");
            context.AppendLine($"功法总数：{TechniqueSystems.Count}");

            if (SelectedTechnique != null)
            {
                context.AppendLine($"当前选中：{SelectedTechnique.Name}");
                context.AppendLine($"功法类型：{SelectedTechnique.Category}");
                context.AppendLine($"功法等级：{SelectedTechnique.Grade}");
            }

            context.AppendLine("\n可用操作：");
            context.AppendLine("- 创建新的功法体系");
            context.AppendLine("- 编辑现有功法属性");
            context.AppendLine("- 设计功法等级体系");
            context.AppendLine("- 添加功法招式");
            context.AppendLine("- 分析功法平衡性");

            return context.ToString();
        }

        /// <summary>
        /// 选择功法体系
        /// </summary>
        private void SelectTechniqueSystem(TechniqueSystemViewModel technique)
        {
            if (technique == null) return;

            SelectedTechnique = technique;
            LoadTechniqueSystemDetails(technique);
            ShowEditPanel();
        }

        /// <summary>
        /// 加载功法体系详情
        /// </summary>
        private void LoadTechniqueSystemDetails(TechniqueSystemViewModel technique)
        {
            // 填充基本信息
            TechniqueNameTextBox.Text = technique.Name;
            TechniqueDescriptionTextBox.Text = technique.Description;
            TechniqueOriginTextBox.Text = technique.Origin;
            
            // 设置功法类别选择
            foreach (ComboBoxItem item in TechniqueCategoryComboBox.Items)
            {
                if (item.Content.ToString() == technique.Category)
                {
                    TechniqueCategoryComboBox.SelectedItem = item;
                    break;
                }
            }

            // 设置功法品级选择
            foreach (ComboBoxItem item in TechniqueGradeComboBox.Items)
            {
                if (item.Content.ToString() == technique.Grade)
                {
                    TechniqueGradeComboBox.SelectedItem = item;
                    break;
                }
            }

            // 加载等级列表
            LoadTechniqueLevels(technique.Id);
            
            // 加载招式列表
            LoadTechniqueMoves(technique.Id);
        }

        /// <summary>
        /// 加载功法等级
        /// </summary>
        private void LoadTechniqueLevels(int techniqueId)
        {
            // 模拟数据
            var levels = new List<TechniqueLevelViewModel>
            {
                new TechniqueLevelViewModel { Level = 1, Name = "初窥门径", Requirements = "基础修为" },
                new TechniqueLevelViewModel { Level = 2, Name = "略有小成", Requirements = "筑基期修为" },
                new TechniqueLevelViewModel { Level = 3, Name = "登堂入室", Requirements = "金丹期修为" }
            };

            TechniqueLevels.Clear();
            foreach (var level in levels)
            {
                TechniqueLevels.Add(level);
            }

            LevelListControl.ItemsSource = TechniqueLevels;
        }

        /// <summary>
        /// 加载功法招式
        /// </summary>
        private void LoadTechniqueMoves(int techniqueId)
        {
            // 模拟数据
            var moves = new List<TechniqueMoveViewModel>
            {
                new TechniqueMoveViewModel { Name = "第一式", Type = "攻击", Description = "基础攻击招式" },
                new TechniqueMoveViewModel { Name = "第二式", Type = "防御", Description = "基础防御招式" }
            };

            TechniqueMoves.Clear();
            foreach (var move in moves)
            {
                TechniqueMoves.Add(move);
            }

            MoveListControl.ItemsSource = TechniqueMoves;
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
                var newLevel = new TechniqueLevelViewModel
                {
                    Level = TechniqueLevels.Count + 1,
                    Name = "",
                    Requirements = ""
                };

                TechniqueLevels.Add(newLevel);
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
                if (sender is Button button && button.CommandParameter is TechniqueLevelViewModel level)
                {
                    TechniqueLevels.Remove(level);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除等级失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region 招式管理

        /// <summary>
        /// 添加招式
        /// </summary>
        private void AddMove_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var newMove = new TechniqueMoveViewModel
                {
                    Name = "",
                    Type = "",
                    Description = ""
                };

                TechniqueMoves.Add(newMove);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加招式失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 删除招式
        /// </summary>
        private void RemoveMove_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.CommandParameter is TechniqueMoveViewModel move)
                {
                    TechniqueMoves.Remove(move);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除招式失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region 保存和取消

        /// <summary>
        /// 保存功法体系
        /// </summary>
        private void SaveTechnique_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 验证输入
                if (string.IsNullOrWhiteSpace(TechniqueNameTextBox.Text))
                {
                    MessageBox.Show("请输入功法名称", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 更新功法体系信息
                SelectedTechnique.Name = TechniqueNameTextBox.Text.Trim();
                SelectedTechnique.Description = TechniqueDescriptionTextBox.Text.Trim();
                SelectedTechnique.Origin = TechniqueOriginTextBox.Text.Trim();
                SelectedTechnique.Category = (TechniqueCategoryComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "修炼功法";
                SelectedTechnique.Grade = (TechniqueGradeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "凡级";
                SelectedTechnique.LevelCount = TechniqueLevels.Count;
                SelectedTechnique.MoveCount = TechniqueMoves.Count;

                // 如果是新建功法体系，添加到列表
                if (SelectedTechnique.Id == 0)
                {
                    SelectedTechnique.Id = TechniqueSystems.Count > 0 ? TechniqueSystems.Max(t => t.Id) + 1 : 1;
                    TechniqueSystems.Add(SelectedTechnique);
                }

                // 这里应该调用服务层保存数据
                MessageBox.Show("功法体系保存成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);

                // 刷新列表
                FilterTechniqueSystems();
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存功法体系失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                TechniqueNameTextBox.Text = "";
                TechniqueDescriptionTextBox.Text = "";
                TechniqueOriginTextBox.Text = "";
                TechniqueCategoryComboBox.SelectedIndex = 0;
                TechniqueGradeComboBox.SelectedIndex = 0;

                // 清空列表
                TechniqueLevels.Clear();
                TechniqueMoves.Clear();

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
    }

    #region ViewModel类

    /// <summary>
    /// 功法体系视图模型
    /// </summary>
    public class TechniqueSystemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public string Grade { get; set; } = "";
        public string Origin { get; set; } = "";
        public string Description { get; set; } = "";
        public int LevelCount { get; set; }
        public int MoveCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// 功法等级视图模型
    /// </summary>
    public class TechniqueLevelViewModel
    {
        public int Level { get; set; }
        public string Name { get; set; } = "";
        public string Requirements { get; set; } = "";
    }

    /// <summary>
    /// 功法招式视图模型
    /// </summary>
    public class TechniqueMoveViewModel
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string Description { get; set; } = "";
    }

    #endregion
}
