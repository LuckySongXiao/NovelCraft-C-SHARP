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
    /// 灵宝体系管理视图
    /// </summary>
    public partial class TreasureSystemView : UserControl, INavigationRefreshableView, INavigationAwareView
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

        private readonly TreasureDataService? _treasureDataService;
        private readonly ProjectContextService? _projectContextService;
        private readonly CurrentProjectGuard? _currentProjectGuard;
        private Guid _currentProjectId;

        /// <summary>
        /// 获取当前灵宝总数。
        /// </summary>
        public int TotalCount => Treasures.Count;

        /// <summary>
        /// 获取武器类灵宝数量。
        /// </summary>
        public int WeaponCount => Treasures.Count(t => t.Type == "武器类");

        /// <summary>
        /// 获取法器类灵宝数量。
        /// </summary>
        public int ArtifactCount => Treasures.Count(t => t.Type == "法器类");

        /// <summary>
        /// 获取丹药类灵宝数量。
        /// </summary>
        public int PillCount => Treasures.Count(t => t.Type == "丹药类");

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化灵宝体系管理视图。
        /// </summary>
        public TreasureSystemView()
        {
            InitializeComponent();

            // 获取服务
            try
            {
                var serviceProvider = App.ServiceProvider;
                _logger = serviceProvider?.GetService<ILogger<TreasureSystemView>>();
                _aiAssistantService = serviceProvider?.GetService<IAIAssistantService>();
                _treasureDataService = serviceProvider?.GetService<TreasureDataService>();
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
            _ = LoadTreasuresAsync();
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
        private async Task LoadTreasuresAsync()
        {
            try
            {
                _currentProjectId = _projectContextService?.CurrentProjectId ?? Guid.Empty;
                if (_currentProjectId == Guid.Empty)
                {
                    _currentProjectGuard?.TryGetCurrentProjectId(Window.GetWindow(this), "灵宝体系管理", out _);
                    Treasures.Clear();
                    TreasureListControl.ItemsSource = Treasures;
                    UpdateStatistics();
                    HideEditPanel();
                    return;
                }

                var treasures = _treasureDataService == null
                    ? new List<TreasureViewModel>()
                    : await _treasureDataService.LoadTreasuresAsync(_currentProjectId);

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
            TreasureListControl.Items.Refresh();
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
        private async void ImportTreasure_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!EnsureCurrentProject("导入灵宝体系"))
                {
                    return;
                }

                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "导入灵宝体系数据",
                    Filter = "JSON文件|*.json|所有文件|*.*",
                    DefaultExt = "json"
                };

                if (dialog.ShowDialog() == true)
                {
                    if (_treasureDataService == null)
                    {
                        MessageBox.Show("灵宝数据服务未初始化。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var importedTreasures = await _treasureDataService.ImportTreasuresAsync(dialog.FileName);
                    Treasures.Clear();
                    foreach (var treasure in importedTreasures)
                    {
                        Treasures.Add(treasure);
                    }

                    await PersistTreasuresAsync();
                    FilterTreasures();
                    UpdateStatistics();
                    MessageBox.Show($"已成功导入 {Treasures.Count} 个灵宝。", "导入成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 导出灵宝数据
        /// </summary>
        private async void ExportTreasure_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!EnsureCurrentProject("导出灵宝体系"))
                {
                    return;
                }

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "导出灵宝体系数据",
                    Filter = "JSON文件|*.json",
                    DefaultExt = "json",
                    FileName = $"灵宝体系数据_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (dialog.ShowDialog() == true)
                {
                    if (_treasureDataService == null)
                    {
                        MessageBox.Show("灵宝数据服务未初始化。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    await _treasureDataService.ExportTreasuresAsync(_currentProjectId, Treasures, dialog.FileName);
                    MessageBox.Show($"灵宝体系数据已导出到：{dialog.FileName}", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 保存灵宝
        /// </summary>
        private async void SaveTreasure_Click(object sender, RoutedEventArgs e)
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

                await PersistTreasuresAsync();
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
        private async void AIAssistant_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!EnsureCurrentProject("AI灵宝体系"))
                {
                    return;
                }

                if (_aiAssistantService == null)
                {
                    ShowAIAssistantDialog();
                    return;
                }

                if (SelectedTreasure != null)
                {
                    var choice = MessageBox.Show(
                        "是：AI优化当前灵宝并保存\n否：AI生成新的灵宝并保存\n取消：打开原始AI助手",
                        "AI灵宝体系",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);

                    if (choice == MessageBoxResult.Cancel)
                    {
                        ShowAIAssistantDialog();
                        return;
                    }

                    await GenerateTreasureWithAiAsync(optimizeCurrent: choice == MessageBoxResult.Yes);
                    return;
                }

                var generateChoice = MessageBox.Show(
                    "是：AI生成新的灵宝并保存\n否：打开原始AI助手",
                    "AI灵宝体系",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (generateChoice == MessageBoxResult.Yes)
                {
                    await GenerateTreasureWithAiAsync(optimizeCurrent: false);
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

        private async Task GenerateTreasureWithAiAsync(bool optimizeCurrent)
        {
            if (_aiAssistantService == null)
            {
                MessageBox.Show("AI助手服务未初始化。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var parameters = new Dictionary<string, object>
            {
                ["title"] = optimizeCurrent && SelectedTreasure != null ? $"优化灵宝：{SelectedTreasure.Name}" : "生成灵宝体系条目",
                ["theme"] = "请生成一个适合小说项目使用的灵宝设定，并输出名称、类型、品级、灵性、描述、灵力、属性、特殊能力、使用限制、炼制材料、炼制方法、炼制难度。",
                ["requirements"] = optimizeCurrent && SelectedTreasure != null
                    ? $"请基于当前灵宝进行优化并输出结构化文本。当前灵宝：{SelectedTreasure.Name}，类型：{SelectedTreasure.Type}，品级：{SelectedTreasure.Grade}，灵性：{SelectedTreasure.SpiritLevel}，描述：{SelectedTreasure.Description}"
                    : "请输出一个完整灵宝设定，至少包含名称、类型、品级、灵性、描述、灵力、属性、特殊能力、使用限制、炼制材料、炼制方法、炼制难度。",
                ["context"] = GetCurrentContext()
            };

            var result = await _aiAssistantService.GenerateOutlineAsync(parameters);
            if (!result.IsSuccess || result.Data == null)
            {
                MessageBox.Show(result.Message ?? "AI生成失败。", "AI灵宝体系", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var generatedTreasure = ParseTreasureFromAiResult(result.Data, optimizeCurrent ? SelectedTreasure : null);
            if (generatedTreasure == null)
            {
                MessageBox.Show("AI结果无法解析为灵宝。", "AI灵宝体系", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (optimizeCurrent && SelectedTreasure != null)
            {
                generatedTreasure.Id = SelectedTreasure.Id;
                generatedTreasure.CreatedAt = SelectedTreasure.CreatedAt;
                var index = Treasures.IndexOf(SelectedTreasure);
                if (index >= 0)
                {
                    Treasures[index] = generatedTreasure;
                }
                SelectedTreasure = generatedTreasure;
            }
            else
            {
                generatedTreasure.Id = Treasures.Count > 0 ? Treasures.Max(t => t.Id) + 1 : 1;
                generatedTreasure.CreatedAt = DateTime.Now;
                Treasures.Add(generatedTreasure);
                SelectedTreasure = generatedTreasure;
            }

            await PersistTreasuresAsync();
            FilterTreasures();
            UpdateStatistics();
            LoadTreasureDetails(generatedTreasure);
            ShowEditPanel();
            MessageBox.Show(
                optimizeCurrent ? $"已使用 AI 优化并保存灵宝：{generatedTreasure.Name}" : $"已使用 AI 生成并保存灵宝：{generatedTreasure.Name}",
                "AI灵宝体系",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private TreasureViewModel? ParseTreasureFromAiResult(object data, TreasureViewModel? baseTreasure)
        {
            var text = data?.ToString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            return new TreasureViewModel
            {
                Id = baseTreasure?.Id ?? 0,
                Name = ExtractField(text, "名称") ?? baseTreasure?.Name ?? ExtractFirstMeaningfulLine(text) ?? "AI生成灵宝",
                Type = ExtractField(text, "类型") ?? baseTreasure?.Type ?? "法器类",
                Grade = ExtractField(text, "品级") ?? baseTreasure?.Grade ?? "凡品",
                SpiritLevel = ExtractField(text, "灵性") ?? baseTreasure?.SpiritLevel ?? "初级灵性",
                Description = ExtractField(text, "描述") ?? baseTreasure?.Description ?? text.Trim(),
                SpiritPower = ExtractField(text, "灵力") ?? baseTreasure?.SpiritPower ?? "1000",
                ElementAffinity = ExtractField(text, "属性") ?? baseTreasure?.ElementAffinity ?? "无",
                SpecialAbility = ExtractField(text, "特殊能力") ?? baseTreasure?.SpecialAbility ?? "待补充",
                UsageLimit = ExtractField(text, "使用限制") ?? baseTreasure?.UsageLimit ?? "待补充",
                CraftingMaterials = ExtractField(text, "炼制材料") ?? baseTreasure?.CraftingMaterials ?? "待补充",
                CraftingMethod = ExtractField(text, "炼制方法") ?? baseTreasure?.CraftingMethod ?? "待补充",
                CraftingDifficulty = ExtractField(text, "炼制难度") ?? baseTreasure?.CraftingDifficulty ?? "普通",
                CreatedAt = baseTreasure?.CreatedAt ?? DateTime.Now
            };
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
                .Select(line => line.Trim().TrimStart('•', '-', '*', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', '.', '、', ' '))
                .FirstOrDefault(line => !string.IsNullOrWhiteSpace(line));
        }

        /// <summary>
        /// 在项目切换后刷新灵宝体系数据。
        /// </summary>
        /// <param name="projectId">当前项目标识。</param>
        /// <param name="projectName">当前项目名称。</param>
        public async Task RefreshOnProjectChangedAsync(Guid? projectId, string? projectName)
        {
            _currentProjectId = projectId ?? Guid.Empty;
            await LoadTreasuresAsync();
        }

        /// <summary>
        /// 在导航到当前视图时刷新对应项目的灵宝体系数据。
        /// </summary>
        /// <param name="context">导航上下文。</param>
        public void OnNavigatedTo(NavigationContext context)
        {
            _currentProjectId = context.ProjectId ?? Guid.Empty;
            _ = LoadTreasuresAsync();
        }

        private async Task PersistTreasuresAsync()
        {
            if (_currentProjectId == Guid.Empty || _treasureDataService == null)
            {
                return;
            }

            await _treasureDataService.SaveTreasuresAsync(_currentProjectId, Treasures);
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

        #endregion
    }

    #region 视图模型

    /// <summary>
    /// 灵宝视图模型
    /// </summary>
    public class TreasureViewModel
    {
        /// <summary>
        /// 灵宝标识。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 灵宝名称。
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// 灵宝类型。
        /// </summary>
        public string Type { get; set; } = "";

        /// <summary>
        /// 灵宝品级。
        /// </summary>
        public string Grade { get; set; } = "";

        /// <summary>
        /// 灵性等级。
        /// </summary>
        public string SpiritLevel { get; set; } = "";

        /// <summary>
        /// 灵宝描述。
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// 灵力强度。
        /// </summary>
        public string SpiritPower { get; set; } = "";

        /// <summary>
        /// 元素亲和属性。
        /// </summary>
        public string ElementAffinity { get; set; } = "";

        /// <summary>
        /// 特殊能力说明。
        /// </summary>
        public string SpecialAbility { get; set; } = "";

        /// <summary>
        /// 使用限制。
        /// </summary>
        public string UsageLimit { get; set; } = "";

        /// <summary>
        /// 炼制材料。
        /// </summary>
        public string CraftingMaterials { get; set; } = "";

        /// <summary>
        /// 炼制方法。
        /// </summary>
        public string CraftingMethod { get; set; } = "";

        /// <summary>
        /// 炼制难度。
        /// </summary>
        public string CraftingDifficulty { get; set; } = "";

        /// <summary>
        /// 创建时间。
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }

    #endregion
}
