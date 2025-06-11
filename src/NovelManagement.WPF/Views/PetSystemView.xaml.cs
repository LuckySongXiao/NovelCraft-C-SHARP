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
    /// 宠物体系管理视图
    /// </summary>
    public partial class PetSystemView : UserControl
    {
        #region 属性

        /// <summary>
        /// 宠物列表
        /// </summary>
        public ObservableCollection<PetViewModel> Pets { get; set; }

        /// <summary>
        /// 当前选中的宠物
        /// </summary>
        public PetViewModel SelectedPet { get; set; }

        /// <summary>
        /// 技能列表
        /// </summary>
        public ObservableCollection<SkillViewModel> Skills { get; set; }

        /// <summary>
        /// 选择宠物命令
        /// </summary>
        public ICommand SelectPetCommand { get; set; }

        /// <summary>
        /// 日志记录器
        /// </summary>
        private readonly ILogger<PetSystemView>? _logger;

        /// <summary>
        /// AI助手服务
        /// </summary>
        private readonly IAIAssistantService? _aiAssistantService;

        #endregion

        #region 构造函数

        public PetSystemView()
        {
            InitializeComponent();

            // 获取服务
            try
            {
                var serviceProvider = App.ServiceProvider;
                _logger = serviceProvider?.GetService<ILogger<PetSystemView>>();
                _aiAssistantService = serviceProvider?.GetService<IAIAssistantService>();
            }
            catch (Exception ex)
            {
                // 如果获取服务失败，记录错误但不影响界面初始化
                System.Diagnostics.Debug.WriteLine($"获取服务失败: {ex.Message}");
            }

            InitializeData();
            InitializeCommands();
            LoadPets();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化数据
        /// </summary>
        private void InitializeData()
        {
            Pets = new ObservableCollection<PetViewModel>();
            Skills = new ObservableCollection<SkillViewModel>();
            
            // 设置数据上下文
            this.DataContext = this;
        }

        /// <summary>
        /// 初始化命令
        /// </summary>
        private void InitializeCommands()
        {
            SelectPetCommand = new RelayCommand<PetViewModel>(SelectPet);
        }

        #endregion

        #region 数据加载

        /// <summary>
        /// 加载宠物数据
        /// </summary>
        private void LoadPets()
        {
            try
            {
                // 模拟数据 - 实际应用中应该从服务层获取
                var pets = new List<PetViewModel>
                {
                    new PetViewModel
                    {
                        Id = 1,
                        Name = "九尾天狐",
                        Type = "神兽",
                        Rarity = "神话",
                        Level = "100",
                        Element = "火、幻术",
                        GrowthStage = "究极体",
                        Description = "传说中的九尾狐，拥有强大的幻术能力和火焰操控力",
                        Attack = "9500",
                        Defense = "7800",
                        Health = "12000",
                        Speed = "8900",
                        Mana = "15000",
                        Loyalty = "95",
                        EvolutionFrom = "七尾狐",
                        EvolutionTo = "无",
                        EvolutionCondition = "已达到最终进化形态",
                        CreatedAt = DateTime.Now.AddDays(-365)
                    },
                    new PetViewModel
                    {
                        Id = 2,
                        Name = "青龙",
                        Type = "神兽",
                        Rarity = "传说",
                        Level = "85",
                        Element = "木、风",
                        GrowthStage = "完全体",
                        Description = "东方青龙，掌控木属性和风属性的神兽",
                        Attack = "8800",
                        Defense = "9200",
                        Health = "11500",
                        Speed = "7600",
                        Mana = "13000",
                        Loyalty = "88",
                        EvolutionFrom = "青蛟",
                        EvolutionTo = "苍龙",
                        EvolutionCondition = "等级达到100级，获得龙珠",
                        CreatedAt = DateTime.Now.AddDays(-300)
                    },
                    new PetViewModel
                    {
                        Id = 3,
                        Name = "雷鸟",
                        Type = "灵兽",
                        Rarity = "史诗",
                        Level = "60",
                        Element = "雷",
                        GrowthStage = "成熟期",
                        Description = "掌控雷电之力的神鸟，速度极快",
                        Attack = "7200",
                        Defense = "5800",
                        Health = "8500",
                        Speed = "9800",
                        Mana = "9500",
                        Loyalty = "75",
                        EvolutionFrom = "雷鹰",
                        EvolutionTo = "雷神鸟",
                        EvolutionCondition = "等级达到80级，雷劫洗礼",
                        CreatedAt = DateTime.Now.AddDays(-250)
                    },
                    new PetViewModel
                    {
                        Id = 4,
                        Name = "火麒麟",
                        Type = "仙兽",
                        Rarity = "传说",
                        Level = "90",
                        Element = "火、土",
                        GrowthStage = "完全体",
                        Description = "传说中的火麒麟，拥有强大的火焰和大地之力",
                        Attack = "9000",
                        Defense = "8500",
                        Health = "10800",
                        Speed = "7200",
                        Mana = "11500",
                        Loyalty = "92",
                        EvolutionFrom = "火麟兽",
                        EvolutionTo = "圣火麒麟",
                        EvolutionCondition = "等级达到100级，圣火洗礼",
                        CreatedAt = DateTime.Now.AddDays(-200)
                    },
                    new PetViewModel
                    {
                        Id = 5,
                        Name = "冰晶狼",
                        Type = "妖兽",
                        Rarity = "稀有",
                        Level = "45",
                        Element = "冰",
                        GrowthStage = "成长期",
                        Description = "生活在冰原的狼族，拥有冰属性攻击能力",
                        Attack = "5500",
                        Defense = "4800",
                        Health = "6200",
                        Speed = "7800",
                        Mana = "5800",
                        Loyalty = "68",
                        EvolutionFrom = "雪狼",
                        EvolutionTo = "冰霜巨狼",
                        EvolutionCondition = "等级达到60级，冰晶核心",
                        CreatedAt = DateTime.Now.AddDays(-150)
                    }
                };

                Pets.Clear();
                foreach (var pet in pets)
                {
                    Pets.Add(pet);
                }

                // 设置列表数据源
                PetListControl.ItemsSource = Pets;

                // 更新统计信息
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载宠物数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
            FilterPets();
        }

        /// <summary>
        /// 宠物类型筛选变化
        /// </summary>
        private void PetTypeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterPets();
        }

        /// <summary>
        /// 稀有度筛选变化
        /// </summary>
        private void RarityFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterPets();
        }

        /// <summary>
        /// 筛选宠物
        /// </summary>
        private void FilterPets()
        {
            var searchText = SearchTextBox?.Text?.ToLower() ?? "";
            var selectedType = (PetTypeFilterComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString();
            var selectedRarity = (RarityFilterComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString();

            var filteredPets = Pets.Where(p =>
                (string.IsNullOrEmpty(searchText) || 
                 p.Name.ToLower().Contains(searchText) || 
                 p.Description.ToLower().Contains(searchText) ||
                 p.Type.ToLower().Contains(searchText)) &&
                (selectedType == "全部类型" || selectedType == null || p.Type == selectedType) &&
                (selectedRarity == "全部稀有度" || selectedRarity == null || p.Rarity == selectedRarity)
            ).ToList();

            PetListControl.ItemsSource = filteredPets;
        }

        /// <summary>
        /// 选择宠物
        /// </summary>
        private void SelectPet(PetViewModel pet)
        {
            if (pet == null) return;

            SelectedPet = pet;
            LoadPetDetails(pet);
            ShowEditPanel();
        }

        /// <summary>
        /// 加载宠物详情
        /// </summary>
        private void LoadPetDetails(PetViewModel pet)
        {
            // 填充基本信息
            PetNameTextBox.Text = pet.Name;
            PetDescriptionTextBox.Text = pet.Description;
            LevelTextBox.Text = pet.Level;
            ElementTextBox.Text = pet.Element;
            
            // 设置类型选择
            foreach (ComboBoxItem item in PetTypeComboBox.Items)
            {
                if (item.Content.ToString() == pet.Type)
                {
                    PetTypeComboBox.SelectedItem = item;
                    break;
                }
            }

            // 设置稀有度选择
            foreach (ComboBoxItem item in RarityComboBox.Items)
            {
                if (item.Content.ToString() == pet.Rarity)
                {
                    RarityComboBox.SelectedItem = item;
                    break;
                }
            }

            // 设置成长阶段选择
            foreach (ComboBoxItem item in GrowthStageComboBox.Items)
            {
                if (item.Content.ToString() == pet.GrowthStage)
                {
                    GrowthStageComboBox.SelectedItem = item;
                    break;
                }
            }

            // 填充属性信息
            AttackTextBox.Text = pet.Attack;
            DefenseTextBox.Text = pet.Defense;
            HealthTextBox.Text = pet.Health;
            SpeedTextBox.Text = pet.Speed;
            ManaTextBox.Text = pet.Mana;
            LoyaltyTextBox.Text = pet.Loyalty;

            // 填充进化信息
            EvolutionFromTextBox.Text = pet.EvolutionFrom;
            EvolutionToTextBox.Text = pet.EvolutionTo;
            EvolutionConditionTextBox.Text = pet.EvolutionCondition;

            // 加载技能列表
            LoadSkills(pet.Id);
        }

        /// <summary>
        /// 加载技能列表
        /// </summary>
        private void LoadSkills(int petId)
        {
            // 模拟数据 - 实际应用中应该从服务层获取
            var skills = new List<SkillViewModel>();
            
            if (petId == 1) // 九尾天狐
            {
                skills.AddRange(new[]
                {
                    new SkillViewModel { Name = "狐火术", Type = "攻击", Level = "10" },
                    new SkillViewModel { Name = "幻术迷惑", Type = "辅助", Level = "8" },
                    new SkillViewModel { Name = "九尾冲击", Type = "攻击", Level = "9" },
                    new SkillViewModel { Name = "火焰护盾", Type = "防御", Level = "7" }
                });
            }
            else if (petId == 2) // 青龙
            {
                skills.AddRange(new[]
                {
                    new SkillViewModel { Name = "龙息", Type = "攻击", Level = "9" },
                    new SkillViewModel { Name = "风刃", Type = "攻击", Level = "8" },
                    new SkillViewModel { Name = "木遁", Type = "辅助", Level = "7" },
                    new SkillViewModel { Name = "龙鳞护体", Type = "防御", Level = "8" }
                });
            }
            else if (petId == 3) // 雷鸟
            {
                skills.AddRange(new[]
                {
                    new SkillViewModel { Name = "雷击", Type = "攻击", Level = "7" },
                    new SkillViewModel { Name = "闪电链", Type = "攻击", Level = "6" },
                    new SkillViewModel { Name = "疾风", Type = "辅助", Level = "8" }
                });
            }

            Skills.Clear();
            foreach (var skill in skills)
            {
                Skills.Add(skill);
            }

            SkillListControl.ItemsSource = Skills;
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
            SelectedPet = null;
        }

        #endregion

        #region 按钮事件

        /// <summary>
        /// 新建宠物
        /// </summary>
        private void AddPet_Click(object sender, RoutedEventArgs e)
        {
            var newPet = new PetViewModel
            {
                Id = 0,
                Name = "",
                Type = "灵兽",
                Rarity = "普通",
                Level = "1",
                Element = "",
                GrowthStage = "幼体",
                Description = "",
                Attack = "0",
                Defense = "0",
                Health = "0",
                Speed = "0",
                Mana = "0",
                Loyalty = "50",
                EvolutionFrom = "",
                EvolutionTo = "",
                EvolutionCondition = "",
                CreatedAt = DateTime.Now
            };

            SelectedPet = newPet;
            LoadPetDetails(newPet);
            ShowEditPanel();

            // 清空技能列表
            Skills.Clear();
            SkillListControl.ItemsSource = Skills;

            // 聚焦到名称输入框
            PetNameTextBox.Focus();
        }

        /// <summary>
        /// 导入宠物数据
        /// </summary>
        private void ImportPet_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("导入功能开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 导出宠物数据
        /// </summary>
        private void ExportPet_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("导出功能开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 添加技能
        /// </summary>
        private void AddSkill_Click(object sender, RoutedEventArgs e)
        {
            var newSkill = new SkillViewModel
            {
                Name = "",
                Type = "攻击",
                Level = "1"
            };

            Skills.Add(newSkill);
        }

        /// <summary>
        /// 删除技能
        /// </summary>
        private void RemoveSkill_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is SkillViewModel skill)
            {
                Skills.Remove(skill);
            }
        }

        /// <summary>
        /// 保存宠物
        /// </summary>
        private void SavePet_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SelectedPet == null) return;

                // 验证输入
                if (string.IsNullOrWhiteSpace(PetNameTextBox.Text))
                {
                    MessageBox.Show("请输入宠物名称", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 更新宠物信息
                SelectedPet.Name = PetNameTextBox.Text.Trim();
                SelectedPet.Description = PetDescriptionTextBox.Text.Trim();
                SelectedPet.Type = (PetTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "灵兽";
                SelectedPet.Rarity = (RarityComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "普通";
                SelectedPet.Level = LevelTextBox.Text.Trim();
                SelectedPet.Element = ElementTextBox.Text.Trim();
                SelectedPet.GrowthStage = (GrowthStageComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "幼体";
                SelectedPet.Attack = AttackTextBox.Text.Trim();
                SelectedPet.Defense = DefenseTextBox.Text.Trim();
                SelectedPet.Health = HealthTextBox.Text.Trim();
                SelectedPet.Speed = SpeedTextBox.Text.Trim();
                SelectedPet.Mana = ManaTextBox.Text.Trim();
                SelectedPet.Loyalty = LoyaltyTextBox.Text.Trim();
                SelectedPet.EvolutionFrom = EvolutionFromTextBox.Text.Trim();
                SelectedPet.EvolutionTo = EvolutionToTextBox.Text.Trim();
                SelectedPet.EvolutionCondition = EvolutionConditionTextBox.Text.Trim();

                // 如果是新建宠物，添加到列表
                if (SelectedPet.Id == 0)
                {
                    SelectedPet.Id = Pets.Count > 0 ? Pets.Max(p => p.Id) + 1 : 1;
                    Pets.Add(SelectedPet);
                }

                // 这里应该调用服务层保存数据
                MessageBox.Show("宠物保存成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);

                // 刷新列表
                FilterPets();
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
            var dialog = new AIAssistantDialog("宠物体系管理", contextString);
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
                ["interfaceType"] = "宠物体系管理",
                ["totalPets"] = Pets.Count,
                ["selectedPet"] = SelectedPet,
                ["petTypes"] = new[] { "灵兽", "妖兽", "神兽", "魔兽", "仙兽", "凶兽", "异兽" },
                ["rarityLevels"] = new[] { "普通", "稀有", "史诗", "传说", "神话", "至尊" },
                ["growthStages"] = new[] { "幼体", "成长期", "成熟期", "完全体", "究极体" }
            };

            if (SelectedPet != null)
            {
                context["currentPetName"] = SelectedPet.Name;
                context["currentPetType"] = SelectedPet.Type;
                context["currentPetRarity"] = SelectedPet.Rarity;
                context["currentPetLevel"] = SelectedPet.Level;
                context["currentPetElement"] = SelectedPet.Element;
                context["currentPetGrowthStage"] = SelectedPet.GrowthStage;
                context["currentPetDescription"] = SelectedPet.Description;
                context["currentPetStats"] = new
                {
                    Attack = SelectedPet.Attack,
                    Defense = SelectedPet.Defense,
                    Health = SelectedPet.Health,
                    Speed = SelectedPet.Speed,
                    Mana = SelectedPet.Mana,
                    Loyalty = SelectedPet.Loyalty
                };
                context["currentPetEvolution"] = new
                {
                    From = SelectedPet.EvolutionFrom,
                    To = SelectedPet.EvolutionTo,
                    Condition = SelectedPet.EvolutionCondition
                };
                context["currentPetSkills"] = Skills.ToList();
            }

            // 添加统计信息
            var typeStats = Pets.GroupBy(p => p.Type)
                .ToDictionary(g => g.Key, g => g.Count());
            context["typeStatistics"] = typeStats;

            var rarityStats = Pets.GroupBy(p => p.Rarity)
                .ToDictionary(g => g.Key, g => g.Count());
            context["rarityStatistics"] = rarityStats;

            return context;
        }

        #endregion
    }

    #region 视图模型

    /// <summary>
    /// 宠物视图模型
    /// </summary>
    public class PetViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Rarity { get; set; }
        public string Level { get; set; }
        public string Element { get; set; }
        public string GrowthStage { get; set; }
        public string Description { get; set; }
        public string Attack { get; set; }
        public string Defense { get; set; }
        public string Health { get; set; }
        public string Speed { get; set; }
        public string Mana { get; set; }
        public string Loyalty { get; set; }
        public string EvolutionFrom { get; set; }
        public string EvolutionTo { get; set; }
        public string EvolutionCondition { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// 技能视图模型
    /// </summary>
    public class SkillViewModel
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Level { get; set; }
    }

    #endregion
}
