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
    /// 职业体系管理视图
    /// </summary>
    public partial class ProfessionSystemView : UserControl
    {
        #region 属性

        /// <summary>
        /// 职业体系列表
        /// </summary>
        public ObservableCollection<ProfessionSystemViewModel> ProfessionSystems { get; set; }

        /// <summary>
        /// 当前选中的职业体系
        /// </summary>
        public ProfessionSystemViewModel SelectedProfession { get; set; }

        /// <summary>
        /// 职业等级列表
        /// </summary>
        public ObservableCollection<ProfessionLevelViewModel> ProfessionLevels { get; set; }

        /// <summary>
        /// 选择职业命令
        /// </summary>
        public ICommand SelectProfessionCommand { get; set; }

        #endregion

        #region 构造函数

        public ProfessionSystemView()
        {
            InitializeComponent();
            InitializeData();
            InitializeCommands();
            LoadProfessionSystems();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化数据
        /// </summary>
        private void InitializeData()
        {
            ProfessionSystems = new ObservableCollection<ProfessionSystemViewModel>();
            ProfessionLevels = new ObservableCollection<ProfessionLevelViewModel>();
            
            // 设置数据上下文
            this.DataContext = this;
        }

        /// <summary>
        /// 初始化命令
        /// </summary>
        private void InitializeCommands()
        {
            SelectProfessionCommand = new RelayCommand<ProfessionSystemViewModel>(SelectProfession);
        }

        #endregion

        #region 数据加载

        /// <summary>
        /// 加载职业体系数据
        /// </summary>
        private void LoadProfessionSystems()
        {
            try
            {
                // 模拟数据 - 实际应用中应该从服务层获取
                var professions = new List<ProfessionSystemViewModel>
                {
                    new ProfessionSystemViewModel
                    {
                        Id = 1,
                        Name = "修仙者职业",
                        Category = "修炼职业",
                        Description = "以修炼为主的职业体系，包含炼气、筑基、金丹等境界",
                        LevelCount = 12,
                        SkillCount = 25,
                        CreatedAt = DateTime.Now.AddDays(-30)
                    },
                    new ProfessionSystemViewModel
                    {
                        Id = 2,
                        Name = "炼器师",
                        Category = "生活职业",
                        Description = "专门炼制法器、灵器的职业，需要深厚的炼器知识",
                        LevelCount = 8,
                        SkillCount = 15,
                        CreatedAt = DateTime.Now.AddDays(-25)
                    },
                    new ProfessionSystemViewModel
                    {
                        Id = 3,
                        Name = "剑修",
                        Category = "战斗职业",
                        Description = "专精剑道的修炼者，以剑为道，剑心通明",
                        LevelCount = 10,
                        SkillCount = 20,
                        CreatedAt = DateTime.Now.AddDays(-20)
                    },
                    new ProfessionSystemViewModel
                    {
                        Id = 4,
                        Name = "阵法师",
                        Category = "辅助职业",
                        Description = "精通阵法布置和破解的职业，能够布置各种功能阵法",
                        LevelCount = 9,
                        SkillCount = 18,
                        CreatedAt = DateTime.Now.AddDays(-15)
                    }
                };

                ProfessionSystems.Clear();
                foreach (var profession in professions)
                {
                    ProfessionSystems.Add(profession);
                }

                // 设置列表数据源
                ProfessionListControl.ItemsSource = ProfessionSystems;

                // 更新统计信息
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载职业体系数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
            FilterProfessionSystems();
        }

        /// <summary>
        /// 类别筛选变化
        /// </summary>
        private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterProfessionSystems();
        }

        /// <summary>
        /// 筛选职业体系
        /// </summary>
        private void FilterProfessionSystems()
        {
            var searchText = SearchTextBox?.Text?.ToLower() ?? "";
            var selectedCategory = (CategoryFilterComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString();

            var filteredProfessions = ProfessionSystems.Where(p =>
                (string.IsNullOrEmpty(searchText) || 
                 p.Name.ToLower().Contains(searchText) || 
                 p.Description.ToLower().Contains(searchText)) &&
                (selectedCategory == "全部类别" || selectedCategory == null || p.Category == selectedCategory)
            ).ToList();

            ProfessionListControl.ItemsSource = filteredProfessions;
        }

        /// <summary>
        /// 选择职业体系
        /// </summary>
        private void SelectProfession(ProfessionSystemViewModel profession)
        {
            if (profession == null) return;

            SelectedProfession = profession;
            LoadProfessionDetails(profession);
            ShowEditPanel();
        }

        /// <summary>
        /// 加载职业详情
        /// </summary>
        private void LoadProfessionDetails(ProfessionSystemViewModel profession)
        {
            // 填充基本信息
            ProfessionNameTextBox.Text = profession.Name;
            ProfessionDescriptionTextBox.Text = profession.Description;
            
            // 设置类别选择
            foreach (ComboBoxItem item in ProfessionCategoryComboBox.Items)
            {
                if (item.Content.ToString() == profession.Category)
                {
                    ProfessionCategoryComboBox.SelectedItem = item;
                    break;
                }
            }

            // 加载职业等级
            LoadProfessionLevels(profession.Id);
        }

        /// <summary>
        /// 加载职业等级
        /// </summary>
        private void LoadProfessionLevels(int professionId)
        {
            // 模拟数据 - 实际应用中应该从服务层获取
            var levels = new List<ProfessionLevelViewModel>();
            
            if (professionId == 1) // 修仙者职业
            {
                levels.AddRange(new[]
                {
                    new ProfessionLevelViewModel { Level = 1, Name = "炼气初期", Requirements = "开始修炼" },
                    new ProfessionLevelViewModel { Level = 2, Name = "炼气中期", Requirements = "炼气初期圆满" },
                    new ProfessionLevelViewModel { Level = 3, Name = "炼气后期", Requirements = "炼气中期圆满" },
                    new ProfessionLevelViewModel { Level = 4, Name = "筑基初期", Requirements = "炼气后期圆满，筑基丹" },
                    new ProfessionLevelViewModel { Level = 5, Name = "筑基中期", Requirements = "筑基初期圆满" },
                    new ProfessionLevelViewModel { Level = 6, Name = "筑基后期", Requirements = "筑基中期圆满" }
                });
            }
            else if (professionId == 2) // 炼器师
            {
                levels.AddRange(new[]
                {
                    new ProfessionLevelViewModel { Level = 1, Name = "学徒", Requirements = "基础炼器知识" },
                    new ProfessionLevelViewModel { Level = 2, Name = "初级炼器师", Requirements = "炼制10件法器" },
                    new ProfessionLevelViewModel { Level = 3, Name = "中级炼器师", Requirements = "炼制灵器" },
                    new ProfessionLevelViewModel { Level = 4, Name = "高级炼器师", Requirements = "炼制法宝" }
                });
            }

            ProfessionLevels.Clear();
            foreach (var level in levels)
            {
                ProfessionLevels.Add(level);
            }

            LevelListControl.ItemsSource = ProfessionLevels;
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
            SelectedProfession = null;
        }

        #endregion

        #region 按钮事件

        /// <summary>
        /// 新建职业体系
        /// </summary>
        private void AddProfession_Click(object sender, RoutedEventArgs e)
        {
            var newProfession = new ProfessionSystemViewModel
            {
                Id = 0,
                Name = "",
                Category = "修炼职业",
                Description = "",
                LevelCount = 0,
                SkillCount = 0,
                CreatedAt = DateTime.Now
            };

            SelectedProfession = newProfession;
            LoadProfessionDetails(newProfession);
            ShowEditPanel();

            // 清空等级列表
            ProfessionLevels.Clear();
            LevelListControl.ItemsSource = ProfessionLevels;

            // 聚焦到名称输入框
            ProfessionNameTextBox.Focus();
        }

        /// <summary>
        /// 导入职业数据
        /// </summary>
        private void ImportProfession_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("导入功能开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 导出职业数据
        /// </summary>
        private void ExportProfession_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("导出功能开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
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
            var dialog = new AIAssistantDialog("职业体系管理", GetCurrentContext());
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();
        }

        /// <summary>
        /// 获取当前上下文信息
        /// </summary>
        private string GetCurrentContext()
        {
            var context = new StringBuilder();
            context.AppendLine("当前功能：职业体系管理");
            context.AppendLine($"职业总数：{ProfessionSystems.Count}");

            if (SelectedProfession != null)
            {
                context.AppendLine($"当前选中：{SelectedProfession.Name}");
                context.AppendLine($"职业类型：{SelectedProfession.Category}");
                context.AppendLine($"等级数量：{SelectedProfession.LevelCount}");
            }

            context.AppendLine("\n可用操作：");
            context.AppendLine("- 创建新的职业体系");
            context.AppendLine("- 编辑现有职业属性");
            context.AppendLine("- 设计职业等级体系");
            context.AppendLine("- 添加职业技能");
            context.AppendLine("- 分析职业平衡性");

            return context.ToString();
        }

        /// <summary>
        /// 添加等级
        /// </summary>
        private void AddLevel_Click(object sender, RoutedEventArgs e)
        {
            var newLevel = new ProfessionLevelViewModel
            {
                Level = ProfessionLevels.Count + 1,
                Name = "",
                Requirements = ""
            };

            ProfessionLevels.Add(newLevel);
        }

        /// <summary>
        /// 删除等级
        /// </summary>
        private void RemoveLevel_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is ProfessionLevelViewModel level)
            {
                ProfessionLevels.Remove(level);
                
                // 重新排序等级
                for (int i = 0; i < ProfessionLevels.Count; i++)
                {
                    ProfessionLevels[i].Level = i + 1;
                }
            }
        }

        /// <summary>
        /// 保存职业体系
        /// </summary>
        private void SaveProfession_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SelectedProfession == null) return;

                // 验证输入
                if (string.IsNullOrWhiteSpace(ProfessionNameTextBox.Text))
                {
                    MessageBox.Show("请输入职业体系名称", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 更新职业信息
                SelectedProfession.Name = ProfessionNameTextBox.Text.Trim();
                SelectedProfession.Description = ProfessionDescriptionTextBox.Text.Trim();
                SelectedProfession.Category = (ProfessionCategoryComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "修炼职业";
                SelectedProfession.LevelCount = ProfessionLevels.Count;

                // 如果是新建职业，添加到列表
                if (SelectedProfession.Id == 0)
                {
                    SelectedProfession.Id = ProfessionSystems.Count > 0 ? ProfessionSystems.Max(p => p.Id) + 1 : 1;
                    ProfessionSystems.Add(SelectedProfession);
                }

                // 这里应该调用服务层保存数据
                MessageBox.Show("职业体系保存成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);

                // 刷新列表
                FilterProfessionSystems();
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
    }

    #region 视图模型

    /// <summary>
    /// 职业体系视图模型
    /// </summary>
    public class ProfessionSystemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public int LevelCount { get; set; }
        public int SkillCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// 职业等级视图模型
    /// </summary>
    public class ProfessionLevelViewModel
    {
        public int Level { get; set; }
        public string Name { get; set; }
        public string Requirements { get; set; }
    }

    #endregion
}
