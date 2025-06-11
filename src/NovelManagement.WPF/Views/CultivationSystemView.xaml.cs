using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Extensions.Logging;
using MaterialDesignThemes.Wpf;
using NovelManagement.WPF.Commands;

namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// 修炼体系管理视图
    /// </summary>
    public partial class CultivationSystemView : UserControl
    {
        #region 属性

        /// <summary>
        /// 修炼体系列表
        /// </summary>
        public ObservableCollection<CultivationSystemViewModel> CultivationSystems { get; set; }

        /// <summary>
        /// 修炼等级列表
        /// </summary>
        public ObservableCollection<CultivationLevelViewModel> CultivationLevels { get; set; }

        /// <summary>
        /// 当前选中的修炼体系
        /// </summary>
        public CultivationSystemViewModel SelectedCultivation { get; set; }

        /// <summary>
        /// 选择修炼体系命令
        /// </summary>
        public ICommand SelectCultivationCommand { get; private set; }

        #endregion

        #region 构造函数

        public CultivationSystemView()
        {
            InitializeComponent();
            InitializeData();
            InitializeCommands();
            LoadCultivationSystems();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化数据
        /// </summary>
        private void InitializeData()
        {
            CultivationSystems = new ObservableCollection<CultivationSystemViewModel>();
            CultivationLevels = new ObservableCollection<CultivationLevelViewModel>();
        }

        /// <summary>
        /// 初始化命令
        /// </summary>
        private void InitializeCommands()
        {
            SelectCultivationCommand = new RelayCommand<CultivationSystemViewModel>(SelectCultivation);
        }

        /// <summary>
        /// 加载修炼体系数据
        /// </summary>
        private void LoadCultivationSystems()
        {
            try
            {
                // 模拟数据 - 实际应用中应该从服务层获取
                var cultivationSystems = new List<CultivationSystemViewModel>
                {
                    new CultivationSystemViewModel
                    {
                        Id = 1,
                        Name = "九阳神功",
                        Category = "内功心法",
                        Grade = "神级",
                        Origin = "明教",
                        Description = "至阳至刚的内功心法，修炼至大成可百毒不侵，内力深厚无比",
                        LevelCount = 9,
                        MaxLevel = "第九层",
                        CreatedAt = DateTime.Now.AddDays(-30)
                    },
                    new CultivationSystemViewModel
                    {
                        Id = 2,
                        Name = "太极拳",
                        Category = "外功招式",
                        Grade = "玄级",
                        Origin = "武当派",
                        Description = "以柔克刚的拳法，讲究阴阳调和，四两拨千斤",
                        LevelCount = 7,
                        MaxLevel = "第七层",
                        CreatedAt = DateTime.Now.AddDays(-25)
                    },
                    new CultivationSystemViewModel
                    {
                        Id = 3,
                        Name = "凌波微步",
                        Category = "身法轻功",
                        Grade = "玄级",
                        Origin = "逍遥派",
                        Description = "轻功身法，步法精妙，可在水上行走如履平地",
                        LevelCount = 8,
                        MaxLevel = "第八层",
                        CreatedAt = DateTime.Now.AddDays(-20)
                    },
                    new CultivationSystemViewModel
                    {
                        Id = 4,
                        Name = "易筋经",
                        Category = "炼体功法",
                        Grade = "仙级",
                        Origin = "少林寺",
                        Description = "佛门至高内功，可改变筋脉，脱胎换骨",
                        LevelCount = 12,
                        MaxLevel = "第十二层",
                        CreatedAt = DateTime.Now.AddDays(-15)
                    },
                    new CultivationSystemViewModel
                    {
                        Id = 5,
                        Name = "紫霞神功",
                        Category = "神识功法",
                        Grade = "地级",
                        Origin = "华山派",
                        Description = "华山派镇派内功，修炼时面现紫气，内力精纯",
                        LevelCount = 6,
                        MaxLevel = "第六层",
                        CreatedAt = DateTime.Now.AddDays(-10)
                    }
                };

                CultivationSystems.Clear();
                foreach (var cultivation in cultivationSystems)
                {
                    CultivationSystems.Add(cultivation);
                }

                // 设置列表数据源
                CultivationListControl.ItemsSource = CultivationSystems;

                // 更新统计信息
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载修炼体系数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 更新统计信息
        /// </summary>
        private void UpdateStatistics()
        {
            TotalCountText.Text = CultivationSystems.Count.ToString();
            SelectedCountText.Text = SelectedCultivation != null ? "1" : "0";
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 搜索文本变化事件
        /// </summary>
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterCultivationSystems();
        }

        /// <summary>
        /// 类别筛选变化事件
        /// </summary>
        private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterCultivationSystems();
        }

        /// <summary>
        /// 修炼体系列表选择变化事件
        /// </summary>
        private void CultivationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CultivationListControl.SelectedItem is CultivationSystemViewModel selected)
            {
                SelectCultivation(selected);
            }
        }

        /// <summary>
        /// 添加修炼体系按钮点击事件
        /// </summary>
        private void AddCultivation_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 创建新的修炼体系
                SelectedCultivation = new CultivationSystemViewModel
                {
                    Id = 0, // 新建时ID为0
                    Name = "",
                    Category = "内功心法",
                    Grade = "凡级",
                    Origin = "",
                    Description = "",
                    CreatedAt = DateTime.Now
                };

                // 清空等级列表
                CultivationLevels.Clear();

                // 显示编辑面板
                ShowEditPanel();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"创建修炼体系失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 导入按钮点击事件
        /// </summary>
        private void Import_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("导入功能正在开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 导出按钮点击事件
        /// </summary>
        private void Export_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("导出功能正在开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// AI助手按钮点击事件
        /// </summary>
        private void AIAssistant_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("AI助手功能正在开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 筛选修炼体系
        /// </summary>
        private void FilterCultivationSystems()
        {
            // 实现筛选逻辑
            // 这里应该根据搜索文本和类别筛选器来过滤数据
        }

        /// <summary>
        /// 选择修炼体系
        /// </summary>
        private void SelectCultivation(CultivationSystemViewModel cultivation)
        {
            SelectedCultivation = cultivation;
            ShowCultivationDetails();
            UpdateStatistics();
        }

        /// <summary>
        /// 显示修炼体系详情
        /// </summary>
        private void ShowCultivationDetails()
        {
            if (SelectedCultivation == null)
            {
                DetailsPanel.Children.Clear();
                DetailsPanel.Children.Add(new TextBlock
                {
                    Text = "请选择一个修炼体系查看详情",
                    Style = (Style)FindResource("MaterialDesignBody1TextBlock"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = (Brush)FindResource("MaterialDesignBodyLight")
                });
                return;
            }

            ShowEditPanel();
        }

        /// <summary>
        /// 显示编辑面板
        /// </summary>
        private void ShowEditPanel()
        {
            DetailsPanel.Children.Clear();

            // 创建编辑界面
            var editPanel = CreateEditPanel();
            DetailsPanel.Children.Add(editPanel);
        }

        /// <summary>
        /// 创建编辑面板
        /// </summary>
        private StackPanel CreateEditPanel()
        {
            var panel = new StackPanel();

            // 标题
            var titleBlock = new TextBlock
            {
                Text = SelectedCultivation.Id == 0 ? "新建修炼体系" : "编辑修炼体系",
                Style = (Style)FindResource("MaterialDesignHeadline6TextBlock"),
                Margin = new Thickness(0, 0, 0, 20)
            };
            panel.Children.Add(titleBlock);

            // 基本信息编辑区域
            var basicInfoCard = CreateBasicInfoCard();
            panel.Children.Add(basicInfoCard);

            // 等级设置区域
            var levelsCard = CreateLevelsCard();
            panel.Children.Add(levelsCard);

            // 操作按钮
            var buttonPanel = CreateButtonPanel();
            panel.Children.Add(buttonPanel);

            return panel;
        }

        /// <summary>
        /// 创建基本信息卡片
        /// </summary>
        private Card CreateBasicInfoCard()
        {
            var card = new Card
            {
                Margin = new Thickness(0, 0, 0, 16)
            };

            var stackPanel = new StackPanel
            {
                Margin = new Thickness(16)
            };

            // 名称输入框
            var nameTextBox = new TextBox
            {
                Text = SelectedCultivation?.Name ?? "",
                Margin = new Thickness(0, 0, 0, 16)
            };
            HintAssist.SetHint(nameTextBox, "修炼体系名称");
            nameTextBox.Style = (Style)FindResource("MaterialDesignOutlinedTextBox");
            stackPanel.Children.Add(nameTextBox);

            // 类别选择框
            var categoryComboBox = new ComboBox
            {
                Margin = new Thickness(0, 0, 0, 16)
            };
            HintAssist.SetHint(categoryComboBox, "体系类别");
            categoryComboBox.Style = (Style)FindResource("MaterialDesignOutlinedComboBox");
            categoryComboBox.Items.Add("内功心法");
            categoryComboBox.Items.Add("外功招式");
            categoryComboBox.Items.Add("身法轻功");
            categoryComboBox.Items.Add("炼体功法");
            categoryComboBox.Items.Add("神识功法");
            categoryComboBox.SelectedItem = SelectedCultivation?.Category ?? "内功心法";
            stackPanel.Children.Add(categoryComboBox);

            // 等级选择框
            var gradeComboBox = new ComboBox
            {
                Margin = new Thickness(0, 0, 0, 16)
            };
            HintAssist.SetHint(gradeComboBox, "功法等级");
            gradeComboBox.Style = (Style)FindResource("MaterialDesignOutlinedComboBox");
            gradeComboBox.Items.Add("凡级");
            gradeComboBox.Items.Add("黄级");
            gradeComboBox.Items.Add("玄级");
            gradeComboBox.Items.Add("地级");
            gradeComboBox.Items.Add("天级");
            gradeComboBox.Items.Add("神级");
            gradeComboBox.Items.Add("仙级");
            gradeComboBox.SelectedItem = SelectedCultivation?.Grade ?? "凡级";
            stackPanel.Children.Add(gradeComboBox);

            // 来源输入框
            var originTextBox = new TextBox
            {
                Text = SelectedCultivation?.Origin ?? "",
                Margin = new Thickness(0, 0, 0, 16)
            };
            HintAssist.SetHint(originTextBox, "功法来源");
            originTextBox.Style = (Style)FindResource("MaterialDesignOutlinedTextBox");
            stackPanel.Children.Add(originTextBox);

            // 描述输入框
            var descriptionTextBox = new TextBox
            {
                Text = SelectedCultivation?.Description ?? "",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 80
            };
            HintAssist.SetHint(descriptionTextBox, "功法描述");
            descriptionTextBox.Style = (Style)FindResource("MaterialDesignOutlinedTextBox");
            stackPanel.Children.Add(descriptionTextBox);

            card.Content = stackPanel;
            return card;
        }

        /// <summary>
        /// 创建等级设置卡片
        /// </summary>
        private Card CreateLevelsCard()
        {
            var card = new Card
            {
                Margin = new Thickness(0, 0, 0, 16)
            };

            var stackPanel = new StackPanel
            {
                Margin = new Thickness(16)
            };

            // 标题
            var titleBlock = new TextBlock
            {
                Text = "修炼等级设置",
                Style = (Style)FindResource("MaterialDesignSubtitle1TextBlock"),
                Margin = new Thickness(0, 0, 0, 16)
            };
            stackPanel.Children.Add(titleBlock);

            // 等级列表
            var levelsListView = new ListView
            {
                Height = 200
            };
            levelsListView.ItemsSource = CultivationLevels;
            stackPanel.Children.Add(levelsListView);

            // 添加等级按钮
            var addLevelButton = new Button
            {
                Content = "添加等级",
                Style = (Style)FindResource("MaterialDesignOutlinedButton"),
                Margin = new Thickness(0, 8, 0, 0)
            };
            stackPanel.Children.Add(addLevelButton);

            card.Content = stackPanel;
            return card;
        }

        /// <summary>
        /// 创建按钮面板
        /// </summary>
        private StackPanel CreateButtonPanel()
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 20, 0, 0)
            };

            // 保存按钮
            var saveButton = new Button
            {
                Content = "保存",
                Style = (Style)FindResource("MaterialDesignRaisedButton"),
                Margin = new Thickness(0, 0, 8, 0)
            };
            panel.Children.Add(saveButton);

            // 取消按钮
            var cancelButton = new Button
            {
                Content = "取消",
                Style = (Style)FindResource("MaterialDesignOutlinedButton")
            };
            panel.Children.Add(cancelButton);

            return panel;
        }

        #endregion
    }

    #region 视图模型

    /// <summary>
    /// 修炼体系视图模型
    /// </summary>
    public class CultivationSystemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Grade { get; set; }
        public string Origin { get; set; }
        public string Description { get; set; }
        public int LevelCount { get; set; }
        public string MaxLevel { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// 修炼等级视图模型
    /// </summary>
    public class CultivationLevelViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Level { get; set; }
        public string Description { get; set; }
        public string Requirements { get; set; }
        public string Benefits { get; set; }
    }



    #endregion
}
