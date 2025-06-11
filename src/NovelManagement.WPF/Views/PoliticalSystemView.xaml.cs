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
    /// 政治体系管理视图
    /// </summary>
    public partial class PoliticalSystemView : UserControl
    {
        #region 属性

        /// <summary>
        /// 政治体系列表
        /// </summary>
        public ObservableCollection<PoliticalSystemViewModel> PoliticalSystems { get; set; }

        /// <summary>
        /// 政治职位列表
        /// </summary>
        public ObservableCollection<PoliticalPositionViewModel> PoliticalPositions { get; set; }

        /// <summary>
        /// 当前选中的政治体系
        /// </summary>
        public PoliticalSystemViewModel SelectedPolitical { get; set; }

        /// <summary>
        /// 选择政治体系命令
        /// </summary>
        public ICommand SelectPoliticalCommand { get; private set; }

        #endregion

        #region 构造函数

        public PoliticalSystemView()
        {
            InitializeComponent();
            InitializeData();
            InitializeCommands();
            LoadPoliticalSystems();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化数据
        /// </summary>
        private void InitializeData()
        {
            PoliticalSystems = new ObservableCollection<PoliticalSystemViewModel>();
            PoliticalPositions = new ObservableCollection<PoliticalPositionViewModel>();
        }

        /// <summary>
        /// 初始化命令
        /// </summary>
        private void InitializeCommands()
        {
            SelectPoliticalCommand = new RelayCommand<PoliticalSystemViewModel>(SelectPolitical);
        }

        /// <summary>
        /// 加载政治体系数据
        /// </summary>
        private void LoadPoliticalSystems()
        {
            try
            {
                // 模拟数据 - 实际应用中应该从服务层获取
                var politicalSystems = new List<PoliticalSystemViewModel>
                {
                    new PoliticalSystemViewModel
                    {
                        Id = 1,
                        Name = "大秦帝国",
                        Type = "帝制",
                        Territory = "中原大陆",
                        Description = "以皇帝为最高统治者的中央集权制国家，设有三公九卿制度",
                        PositionCount = 15,
                        Population = 50000000,
                        Capital = "咸阳城",
                        CreatedAt = DateTime.Now.AddDays(-365)
                    },
                    new PoliticalSystemViewModel
                    {
                        Id = 2,
                        Name = "修仙联盟",
                        Type = "联邦制",
                        Territory = "东海诸岛",
                        Description = "由各大修仙宗门组成的联盟，设有盟主和长老会",
                        PositionCount = 12,
                        Population = 5000000,
                        Capital = "天机岛",
                        CreatedAt = DateTime.Now.AddDays(-200)
                    },
                    new PoliticalSystemViewModel
                    {
                        Id = 3,
                        Name = "圣光教廷",
                        Type = "宗教制",
                        Territory = "西方大陆",
                        Description = "以教皇为首的宗教政治体系，神权与世俗权力结合",
                        PositionCount = 18,
                        Population = 30000000,
                        Capital = "圣光城",
                        CreatedAt = DateTime.Now.AddDays(-300)
                    },
                    new PoliticalSystemViewModel
                    {
                        Id = 4,
                        Name = "蛮族部落联盟",
                        Type = "部落制",
                        Territory = "北方草原",
                        Description = "由多个部落组成的松散联盟，以大酋长为首",
                        PositionCount = 8,
                        Population = 8000000,
                        Capital = "金帐汗庭",
                        CreatedAt = DateTime.Now.AddDays(-150)
                    },
                    new PoliticalSystemViewModel
                    {
                        Id = 5,
                        Name = "自由城邦联合",
                        Type = "城邦制",
                        Territory = "南方群岛",
                        Description = "由独立城邦组成的商业联合体，重视贸易和自由",
                        PositionCount = 10,
                        Population = 12000000,
                        Capital = "黄金港",
                        CreatedAt = DateTime.Now.AddDays(-100)
                    }
                };

                PoliticalSystems.Clear();
                foreach (var political in politicalSystems)
                {
                    PoliticalSystems.Add(political);
                }

                // 设置列表数据源
                PoliticalListControl.ItemsSource = PoliticalSystems;

                // 更新统计信息
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载政治体系数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 更新统计信息
        /// </summary>
        private void UpdateStatistics()
        {
            TotalCountText.Text = PoliticalSystems.Count.ToString();
            SelectedCountText.Text = SelectedPolitical != null ? "1" : "0";
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 搜索文本变化事件
        /// </summary>
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterPoliticalSystems();
        }

        /// <summary>
        /// 类型筛选变化事件
        /// </summary>
        private void TypeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterPoliticalSystems();
        }

        /// <summary>
        /// 政治体系列表选择变化事件
        /// </summary>
        private void PoliticalList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PoliticalListControl.SelectedItem is PoliticalSystemViewModel selected)
            {
                SelectPolitical(selected);
            }
        }

        /// <summary>
        /// 添加政治体系按钮点击事件
        /// </summary>
        private void AddPolitical_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 创建新的政治体系
                SelectedPolitical = new PoliticalSystemViewModel
                {
                    Id = 0, // 新建时ID为0
                    Name = "",
                    Type = "帝制",
                    Territory = "",
                    Description = "",
                    CreatedAt = DateTime.Now
                };

                // 清空职位列表
                PoliticalPositions.Clear();

                // 显示编辑面板
                ShowEditPanel();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"创建政治体系失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
        /// 筛选政治体系
        /// </summary>
        private void FilterPoliticalSystems()
        {
            // 实现筛选逻辑
            // 这里应该根据搜索文本和类型筛选器来过滤数据
        }

        /// <summary>
        /// 选择政治体系
        /// </summary>
        private void SelectPolitical(PoliticalSystemViewModel political)
        {
            SelectedPolitical = political;
            ShowPoliticalDetails();
            UpdateStatistics();
        }

        /// <summary>
        /// 显示政治体系详情
        /// </summary>
        private void ShowPoliticalDetails()
        {
            if (SelectedPolitical == null)
            {
                DetailsPanel.Children.Clear();
                DetailsPanel.Children.Add(new TextBlock
                {
                    Text = "请选择一个政治体系查看详情",
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
                Text = SelectedPolitical.Id == 0 ? "新建政治体系" : "编辑政治体系",
                Style = (Style)FindResource("MaterialDesignHeadline6TextBlock"),
                Margin = new Thickness(0, 0, 0, 20)
            };
            panel.Children.Add(titleBlock);

            // 基本信息编辑区域
            var basicInfoCard = CreateBasicInfoCard();
            panel.Children.Add(basicInfoCard);

            // 职位设置区域
            var positionsCard = CreatePositionsCard();
            panel.Children.Add(positionsCard);

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
                Text = SelectedPolitical?.Name ?? "",
                Margin = new Thickness(0, 0, 0, 16)
            };
            HintAssist.SetHint(nameTextBox, "政治体系名称");
            nameTextBox.Style = (Style)FindResource("MaterialDesignOutlinedTextBox");
            stackPanel.Children.Add(nameTextBox);

            // 类型选择框
            var typeComboBox = new ComboBox
            {
                Margin = new Thickness(0, 0, 0, 16)
            };
            HintAssist.SetHint(typeComboBox, "体系类型");
            typeComboBox.Style = (Style)FindResource("MaterialDesignOutlinedComboBox");
            typeComboBox.Items.Add("帝制");
            typeComboBox.Items.Add("共和制");
            typeComboBox.Items.Add("联邦制");
            typeComboBox.Items.Add("宗教制");
            typeComboBox.Items.Add("部落制");
            typeComboBox.Items.Add("城邦制");
            typeComboBox.SelectedItem = SelectedPolitical?.Type ?? "帝制";
            stackPanel.Children.Add(typeComboBox);

            // 领土输入框
            var territoryTextBox = new TextBox
            {
                Text = SelectedPolitical?.Territory ?? "",
                Margin = new Thickness(0, 0, 0, 16)
            };
            HintAssist.SetHint(territoryTextBox, "统治领土");
            territoryTextBox.Style = (Style)FindResource("MaterialDesignOutlinedTextBox");
            stackPanel.Children.Add(territoryTextBox);

            // 首都输入框
            var capitalTextBox = new TextBox
            {
                Text = SelectedPolitical?.Capital ?? "",
                Margin = new Thickness(0, 0, 0, 16)
            };
            HintAssist.SetHint(capitalTextBox, "首都/中心");
            capitalTextBox.Style = (Style)FindResource("MaterialDesignOutlinedTextBox");
            stackPanel.Children.Add(capitalTextBox);

            // 人口输入框
            var populationTextBox = new TextBox
            {
                Text = SelectedPolitical?.Population.ToString() ?? "0",
                Margin = new Thickness(0, 0, 0, 16)
            };
            HintAssist.SetHint(populationTextBox, "总人口");
            populationTextBox.Style = (Style)FindResource("MaterialDesignOutlinedTextBox");
            stackPanel.Children.Add(populationTextBox);

            // 描述输入框
            var descriptionTextBox = new TextBox
            {
                Text = SelectedPolitical?.Description ?? "",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 80
            };
            HintAssist.SetHint(descriptionTextBox, "体系描述");
            descriptionTextBox.Style = (Style)FindResource("MaterialDesignOutlinedTextBox");
            stackPanel.Children.Add(descriptionTextBox);

            card.Content = stackPanel;
            return card;
        }

        /// <summary>
        /// 创建职位设置卡片
        /// </summary>
        private Card CreatePositionsCard()
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
                Text = "政治职位设置",
                Style = (Style)FindResource("MaterialDesignSubtitle1TextBlock"),
                Margin = new Thickness(0, 0, 0, 16)
            };
            stackPanel.Children.Add(titleBlock);

            // 职位列表
            var positionsListView = new ListView
            {
                Height = 200
            };
            positionsListView.ItemsSource = PoliticalPositions;
            stackPanel.Children.Add(positionsListView);

            // 添加职位按钮
            var addPositionButton = new Button
            {
                Content = "添加职位",
                Style = (Style)FindResource("MaterialDesignOutlinedButton"),
                Margin = new Thickness(0, 8, 0, 0)
            };
            stackPanel.Children.Add(addPositionButton);

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
    /// 政治体系视图模型
    /// </summary>
    public class PoliticalSystemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Territory { get; set; }
        public string Description { get; set; }
        public int PositionCount { get; set; }
        public long Population { get; set; }
        public string Capital { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// 政治职位视图模型
    /// </summary>
    public class PoliticalPositionViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Level { get; set; }
        public string Description { get; set; }
        public string Requirements { get; set; }
        public string Powers { get; set; }
        public string Responsibilities { get; set; }
    }

    #endregion
}
