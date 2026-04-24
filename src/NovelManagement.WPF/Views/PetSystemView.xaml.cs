using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
    public partial class PetSystemView : UserControl, INavigationRefreshableView, INavigationAwareView
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

        private readonly PetDataService? _petDataService;
        private readonly ProjectContextService? _projectContextService;
        private readonly CurrentProjectGuard? _currentProjectGuard;
        private Guid _currentProjectId;

        /// <summary>
        /// 获取当前宠物总数。
        /// </summary>
        public int TotalCount => Pets.Count;

        /// <summary>
        /// 获取灵兽数量。
        /// </summary>
        public int SpiritBeastCount => Pets.Count(p => p.Type == "灵兽");

        /// <summary>
        /// 获取神兽数量。
        /// </summary>
        public int DivineBeastCount => Pets.Count(p => p.Type == "神兽");

        /// <summary>
        /// 获取传说级及以上宠物数量。
        /// </summary>
        public int LegendaryCount => Pets.Count(p => p.Rarity == "传说" || p.Rarity == "神话" || p.Rarity == "至尊");

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化宠物体系管理视图。
        /// </summary>
        public PetSystemView()
        {
            InitializeComponent();

            // 获取服务
            try
            {
                var serviceProvider = App.ServiceProvider;
                _logger = serviceProvider?.GetService<ILogger<PetSystemView>>();
                _aiAssistantService = serviceProvider?.GetService<IAIAssistantService>();
                _petDataService = serviceProvider?.GetService<PetDataService>();
                _projectContextService = serviceProvider?.GetService<ProjectContextService>();
                _currentProjectGuard = serviceProvider?.GetService<CurrentProjectGuard>();
            }
            catch (Exception ex)
            {
                // 如果获取服务失败，记录错误但不影响界面初始化
                System.Diagnostics.Debug.WriteLine($"获取服务失败: {ex.Message}");
            }

            InitializeData();
            InitializeCommands();
            _ = LoadPetsAsync();
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
        private async Task LoadPetsAsync()
        {
            try
            {
                _currentProjectId = _projectContextService?.CurrentProjectId ?? Guid.Empty;
                if (_currentProjectId == Guid.Empty)
                {
                    _currentProjectGuard?.TryGetCurrentProjectId(Window.GetWindow(this), "宠物体系管理", out _);
                    Pets.Clear();
                    PetListControl.ItemsSource = Pets;
                    UpdateStatistics();
                    HideEditPanel();
                    return;
                }

                var pets = _petDataService == null
                    ? new List<PetViewModel>()
                    : await _petDataService.LoadPetsAsync(_currentProjectId);

                Pets.Clear();
                foreach (var pet in pets)
                {
                    pet.Skills ??= new List<SkillViewModel>();
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
            PetListControl.Items.Refresh();
            DataContext = null;
            DataContext = this;
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
            LoadSkills(pet);
        }

        /// <summary>
        /// 加载技能列表
        /// </summary>
        private void LoadSkills(PetViewModel pet)
        {
            var skills = pet.Skills ?? new List<SkillViewModel>();

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
        private async void ImportPet_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!EnsureCurrentProject("导入宠物体系"))
                {
                    return;
                }

                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "导入宠物体系数据",
                    Filter = "JSON文件|*.json|所有文件|*.*",
                    DefaultExt = "json"
                };

                if (dialog.ShowDialog() == true)
                {
                    if (_petDataService == null)
                    {
                        MessageBox.Show("宠物数据服务未初始化。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var importedPets = await _petDataService.ImportPetsAsync(dialog.FileName);
                    Pets.Clear();
                    foreach (var pet in importedPets)
                    {
                        pet.Skills ??= new List<SkillViewModel>();
                        Pets.Add(pet);
                    }

                    await PersistPetsAsync();
                    FilterPets();
                    UpdateStatistics();
                    MessageBox.Show($"已成功导入 {Pets.Count} 个宠物。", "导入成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 导出宠物数据
        /// </summary>
        private async void ExportPet_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!EnsureCurrentProject("导出宠物体系"))
                {
                    return;
                }

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "导出宠物体系数据",
                    Filter = "JSON文件|*.json",
                    DefaultExt = "json",
                    FileName = $"宠物体系数据_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (dialog.ShowDialog() == true)
                {
                    if (_petDataService == null)
                    {
                        MessageBox.Show("宠物数据服务未初始化。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    await _petDataService.ExportPetsAsync(_currentProjectId, Pets, dialog.FileName);
                    MessageBox.Show($"宠物体系数据已导出到：{dialog.FileName}", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
        private async void SavePet_Click(object sender, RoutedEventArgs e)
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
                SelectedPet.Skills = Skills.ToList();

                // 如果是新建宠物，添加到列表
                if (SelectedPet.Id == 0)
                {
                    SelectedPet.Id = Pets.Count > 0 ? Pets.Max(p => p.Id) + 1 : 1;
                    Pets.Add(SelectedPet);
                }

                await PersistPetsAsync();
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
        private async void AIAssistant_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!EnsureCurrentProject("AI宠物体系"))
                {
                    return;
                }

                if (_aiAssistantService == null)
                {
                    ShowAIAssistantDialog();
                    return;
                }

                if (SelectedPet != null)
                {
                    var choice = MessageBox.Show(
                        "是：AI优化当前宠物并保存\n否：AI生成新的宠物并保存\n取消：打开原始AI助手",
                        "AI宠物体系",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);

                    if (choice == MessageBoxResult.Cancel)
                    {
                        ShowAIAssistantDialog();
                        return;
                    }

                    await GeneratePetWithAiAsync(optimizeCurrent: choice == MessageBoxResult.Yes);
                    return;
                }

                var generateChoice = MessageBox.Show(
                    "是：AI生成新的宠物并保存\n否：打开原始AI助手",
                    "AI宠物体系",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (generateChoice == MessageBoxResult.Yes)
                {
                    await GeneratePetWithAiAsync(optimizeCurrent: false);
                }
                else
                {
                    ShowAIAssistantDialog();
                }
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

        /// <summary>
        /// 在项目切换后刷新宠物体系数据。
        /// </summary>
        /// <param name="projectId">当前项目标识。</param>
        /// <param name="projectName">当前项目名称。</param>
        public async Task RefreshOnProjectChangedAsync(Guid? projectId, string? projectName)
        {
            _currentProjectId = projectId ?? Guid.Empty;
            await LoadPetsAsync();
        }

        /// <summary>
        /// 在导航到当前视图时刷新对应项目的宠物体系数据。
        /// </summary>
        /// <param name="context">导航上下文。</param>
        public void OnNavigatedTo(NavigationContext context)
        {
            _currentProjectId = context.ProjectId ?? Guid.Empty;
            _ = LoadPetsAsync();
        }

        private async Task PersistPetsAsync()
        {
            if (_currentProjectId == Guid.Empty || _petDataService == null)
            {
                return;
            }

            foreach (var pet in Pets)
            {
                pet.Skills ??= new List<SkillViewModel>();
            }

            await _petDataService.SavePetsAsync(_currentProjectId, Pets);
        }

        private bool EnsureCurrentProject(string actionName)
        {
            _currentProjectId = _projectContextService?.CurrentProjectId ?? Guid.Empty;
            if (_currentProjectId != Guid.Empty)
            {
                return true;
            }

            _currentProjectGuard?.TryGetCurrentProjectId(Window.GetWindow(this), actionName, out _);
            return false;
        }

        private async Task GeneratePetWithAiAsync(bool optimizeCurrent)
        {
            if (_aiAssistantService == null)
            {
                MessageBox.Show("AI助手服务未初始化。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var parameters = new Dictionary<string, object>
            {
                ["title"] = optimizeCurrent && SelectedPet != null ? $"优化宠物：{SelectedPet.Name}" : "生成宠物体系条目",
                ["theme"] = "请生成一个适合小说项目使用的宠物设定，并输出名称、类型、稀有度、等级、元素、成长阶段、描述、六维属性、忠诚度、进化信息、技能列表。",
                ["requirements"] = optimizeCurrent && SelectedPet != null
                    ? $"请基于当前宠物进行优化并输出结构化文本。当前宠物：{SelectedPet.Name}，类型：{SelectedPet.Type}，稀有度：{SelectedPet.Rarity}，等级：{SelectedPet.Level}，描述：{SelectedPet.Description}"
                    : "请输出一个完整宠物设定，至少包含名称、类型、稀有度、等级、元素、成长阶段、描述、六维属性、忠诚度、进化信息、至少3个技能。",
                ["context"] = GetCurrentContext()
            };

            var result = await _aiAssistantService.GenerateOutlineAsync(parameters);
            if (!result.IsSuccess || result.Data == null)
            {
                MessageBox.Show(result.Message ?? "AI生成失败。", "AI宠物体系", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var generatedPet = ParsePetFromAiResult(result.Data, optimizeCurrent ? SelectedPet : null);
            if (generatedPet == null)
            {
                MessageBox.Show("AI结果无法解析为宠物。", "AI宠物体系", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (optimizeCurrent && SelectedPet != null)
            {
                generatedPet.Id = SelectedPet.Id;
                generatedPet.CreatedAt = SelectedPet.CreatedAt;
                var index = Pets.IndexOf(SelectedPet);
                if (index >= 0)
                {
                    Pets[index] = generatedPet;
                }
                SelectedPet = generatedPet;
            }
            else
            {
                generatedPet.Id = Pets.Count > 0 ? Pets.Max(p => p.Id) + 1 : 1;
                generatedPet.CreatedAt = DateTime.Now;
                Pets.Add(generatedPet);
                SelectedPet = generatedPet;
            }

            await PersistPetsAsync();
            FilterPets();
            UpdateStatistics();
            LoadPetDetails(generatedPet);
            ShowEditPanel();
            MessageBox.Show(
                optimizeCurrent ? $"已使用 AI 优化并保存宠物：{generatedPet.Name}" : $"已使用 AI 生成并保存宠物：{generatedPet.Name}",
                "AI宠物体系",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private PetViewModel? ParsePetFromAiResult(object data, PetViewModel? basePet)
        {
            var text = data?.ToString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var pet = new PetViewModel
            {
                Id = basePet?.Id ?? 0,
                Name = ExtractField(text, "名称") ?? basePet?.Name ?? ExtractFirstMeaningfulLine(text) ?? "AI生成宠物",
                Type = ExtractField(text, "类型") ?? basePet?.Type ?? "灵兽",
                Rarity = ExtractField(text, "稀有度") ?? basePet?.Rarity ?? "稀有",
                Level = ExtractField(text, "等级") ?? basePet?.Level ?? "1",
                Element = ExtractField(text, "元素") ?? basePet?.Element ?? "无",
                GrowthStage = ExtractField(text, "成长阶段") ?? basePet?.GrowthStage ?? "幼体",
                Description = ExtractField(text, "描述") ?? basePet?.Description ?? text.Trim(),
                Attack = ExtractField(text, "攻击") ?? basePet?.Attack ?? "100",
                Defense = ExtractField(text, "防御") ?? basePet?.Defense ?? "100",
                Health = ExtractField(text, "生命") ?? basePet?.Health ?? "100",
                Speed = ExtractField(text, "速度") ?? basePet?.Speed ?? "100",
                Mana = ExtractField(text, "法力") ?? basePet?.Mana ?? "100",
                Loyalty = ExtractField(text, "忠诚度") ?? basePet?.Loyalty ?? "80",
                EvolutionFrom = ExtractField(text, "进化前") ?? basePet?.EvolutionFrom ?? "",
                EvolutionTo = ExtractField(text, "进化后") ?? basePet?.EvolutionTo ?? "",
                EvolutionCondition = ExtractField(text, "进化条件") ?? basePet?.EvolutionCondition ?? "待补充",
                CreatedAt = basePet?.CreatedAt ?? DateTime.Now
            };

            pet.Skills = ParseSkills(text);
            if (pet.Skills.Count == 0)
            {
                pet.Skills = basePet?.Skills?.ToList() ?? new List<SkillViewModel>
                {
                    new() { Name = "基础攻击", Type = "攻击", Level = "1" },
                    new() { Name = "守护姿态", Type = "防御", Level = "1" },
                    new() { Name = "灵息感知", Type = "辅助", Level = "1" }
                };
            }

            return pet;
        }

        private static List<SkillViewModel> ParseSkills(string text)
        {
            var skills = new List<SkillViewModel>();
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (!Regex.IsMatch(line, "技能|术|击|爪|护|焰|雷|风|冰"))
                {
                    continue;
                }

                skills.Add(new SkillViewModel
                {
                    Name = TrimListMarker(line),
                    Type = InferSkillType(line),
                    Level = "1"
                });

                if (skills.Count >= 8)
                {
                    break;
                }
            }

            return skills;
        }

        private static string InferSkillType(string line)
        {
            if (Regex.IsMatch(line, "护|守|盾"))
            {
                return "防御";
            }
            if (Regex.IsMatch(line, "辅|治疗|感知|增益"))
            {
                return "辅助";
            }

            return "攻击";
        }

        private static string? ExtractField(string text, string fieldName)
        {
            var match = Regex.Match(text, $"{fieldName}\\s*[:：]\\s*(.+)");
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }

        private static string? ExtractFirstMeaningfulLine(string text)
        {
            return text
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(TrimListMarker)
                .FirstOrDefault(line => !string.IsNullOrWhiteSpace(line));
        }

        private static string TrimListMarker(string line)
        {
            return line.Trim().TrimStart('•', '-', '*', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', '.', '、', ' ');
        }

        #endregion
    }

    #region 视图模型

    /// <summary>
    /// 宠物视图模型
    /// </summary>
    public class PetViewModel
    {
        /// <summary>
        /// 宠物标识。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 宠物名称。
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// 宠物类型。
        /// </summary>
        public string Type { get; set; } = "";

        /// <summary>
        /// 稀有度。
        /// </summary>
        public string Rarity { get; set; } = "";

        /// <summary>
        /// 当前等级。
        /// </summary>
        public string Level { get; set; } = "";

        /// <summary>
        /// 元素属性。
        /// </summary>
        public string Element { get; set; } = "";

        /// <summary>
        /// 成长阶段。
        /// </summary>
        public string GrowthStage { get; set; } = "";

        /// <summary>
        /// 宠物描述。
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// 攻击属性值。
        /// </summary>
        public string Attack { get; set; } = "";

        /// <summary>
        /// 防御属性值。
        /// </summary>
        public string Defense { get; set; } = "";

        /// <summary>
        /// 生命属性值。
        /// </summary>
        public string Health { get; set; } = "";

        /// <summary>
        /// 速度属性值。
        /// </summary>
        public string Speed { get; set; } = "";

        /// <summary>
        /// 法力属性值。
        /// </summary>
        public string Mana { get; set; } = "";

        /// <summary>
        /// 忠诚度。
        /// </summary>
        public string Loyalty { get; set; } = "";

        /// <summary>
        /// 进化前形态。
        /// </summary>
        public string EvolutionFrom { get; set; } = "";

        /// <summary>
        /// 进化后形态。
        /// </summary>
        public string EvolutionTo { get; set; } = "";

        /// <summary>
        /// 进化条件。
        /// </summary>
        public string EvolutionCondition { get; set; } = "";

        /// <summary>
        /// 创建时间。
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 技能列表。
        /// </summary>
        public List<SkillViewModel> Skills { get; set; } = new();
    }

    /// <summary>
    /// 技能视图模型
    /// </summary>
    public class SkillViewModel
    {
        /// <summary>
        /// 技能名称。
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// 技能类型。
        /// </summary>
        public string Type { get; set; } = "";

        /// <summary>
        /// 技能等级。
        /// </summary>
        public string Level { get; set; } = "";
    }

    #endregion
}
