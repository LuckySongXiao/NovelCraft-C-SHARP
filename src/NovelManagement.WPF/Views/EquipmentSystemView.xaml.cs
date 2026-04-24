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
    /// 装备体系管理视图
    /// </summary>
    public partial class EquipmentSystemView : UserControl, INavigationRefreshableView, INavigationAwareView
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

        private readonly EquipmentDataService? _equipmentDataService;
        private readonly ProjectContextService? _projectContextService;
        private readonly CurrentProjectGuard? _currentProjectGuard;
        private Guid _currentProjectId;

        /// <summary>
        /// 获取当前装备体系总数。
        /// </summary>
        public int TotalCount => EquipmentSystems.Count;

        /// <summary>
        /// 获取武器类装备数量。
        /// </summary>
        public int WeaponCount => EquipmentSystems.Count(e => e.Category == "武器");

        /// <summary>
        /// 获取防具类装备数量。
        /// </summary>
        public int ArmorCount => EquipmentSystems.Count(e => e.Category == "防具");

        /// <summary>
        /// 获取法宝类装备数量。
        /// </summary>
        public int TreasureCount => EquipmentSystems.Count(e => e.Category == "法宝");

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化装备体系管理视图。
        /// </summary>
        public EquipmentSystemView()
        {
            InitializeComponent();

            // 获取服务
            try
            {
                var serviceProvider = App.ServiceProvider;
                _logger = serviceProvider?.GetService<ILogger<EquipmentSystemView>>();
                _aiAssistantService = serviceProvider?.GetService<IAIAssistantService>();
                _equipmentDataService = serviceProvider?.GetService<EquipmentDataService>();
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
            _ = LoadEquipmentSystemsAsync();
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
        private async Task LoadEquipmentSystemsAsync()
        {
            try
            {
                _currentProjectId = _projectContextService?.CurrentProjectId ?? Guid.Empty;
                if (_currentProjectId == Guid.Empty)
                {
                    _currentProjectGuard?.TryGetCurrentProjectId(Window.GetWindow(this), "装备体系管理", out _);
                    EquipmentSystems.Clear();
                    EquipmentListControl.ItemsSource = EquipmentSystems;
                    UpdateStatistics();
                    HideEditPanel();
                    return;
                }

                var equipments = _equipmentDataService == null
                    ? new List<EquipmentSystemViewModel>()
                    : await _equipmentDataService.LoadEquipmentSystemsAsync(_currentProjectId);

                EquipmentSystems.Clear();
                foreach (var equipment in equipments)
                {
                    equipment.Levels ??= new List<EquipmentLevelViewModel>();
                    equipment.Attributes ??= new List<EquipmentAttributeViewModel>();
                    equipment.LevelCount = equipment.Levels.Count;
                    equipment.AttributeCount = equipment.Attributes.Count;
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
            EquipmentListControl.Items.Refresh();
            DataContext = null;
            DataContext = this;
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
        private async void ImportEquipment_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "导入装备体系数据",
                    Filter = "JSON文件|*.json|所有文件|*.*",
                    DefaultExt = "json"
                };

                if (dialog.ShowDialog() == true)
                {
                    if (!EnsureCurrentProject("导入装备体系"))
                    {
                        return;
                    }

                    if (_equipmentDataService == null)
                    {
                        MessageBox.Show("装备数据服务未初始化。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var importedSystems = await _equipmentDataService.ImportEquipmentSystemsAsync(dialog.FileName);
                    EquipmentSystems.Clear();
                    foreach (var equipment in importedSystems)
                    {
                        equipment.Levels ??= new List<EquipmentLevelViewModel>();
                        equipment.Attributes ??= new List<EquipmentAttributeViewModel>();
                        equipment.LevelCount = equipment.Levels.Count;
                        equipment.AttributeCount = equipment.Attributes.Count;
                        EquipmentSystems.Add(equipment);
                    }

                    await PersistEquipmentSystemsAsync();
                    FilterEquipmentSystems();
                    UpdateStatistics();
                    MessageBox.Show($"已成功导入 {EquipmentSystems.Count} 个装备体系。", "导入成功", MessageBoxButton.OK, MessageBoxImage.Information);
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
        private async void ExportEquipment_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "导出装备体系数据",
                    Filter = "JSON文件|*.json",
                    DefaultExt = "json",
                    FileName = $"装备体系数据_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (dialog.ShowDialog() == true)
                {
                    if (!EnsureCurrentProject("导出装备体系"))
                    {
                        return;
                    }

                    if (_equipmentDataService == null)
                    {
                        MessageBox.Show("装备数据服务未初始化。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    await _equipmentDataService.ExportEquipmentSystemsAsync(_currentProjectId, EquipmentSystems, dialog.FileName);
                    MessageBox.Show($"装备体系数据已导出到：{dialog.FileName}", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
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
            LoadEquipmentLevels(equipment);
            
            // 加载属性列表
            LoadEquipmentAttributes(equipment);
        }

        /// <summary>
        /// 加载装备等级
        /// </summary>
        private void LoadEquipmentLevels(EquipmentSystemViewModel equipment)
        {
            EquipmentLevels.Clear();
            foreach (var level in equipment.Levels.OrderBy(l => l.Level))
            {
                EquipmentLevels.Add(level);
            }

            LevelListControl.ItemsSource = EquipmentLevels;
        }

        /// <summary>
        /// 加载装备属性
        /// </summary>
        private void LoadEquipmentAttributes(EquipmentSystemViewModel equipment)
        {
            EquipmentAttributes.Clear();
            foreach (var attribute in equipment.Attributes)
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
        private async void SaveEquipment_Click(object sender, RoutedEventArgs e)
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
                SelectedEquipment.Levels = EquipmentLevels.OrderBy(l => l.Level).ToList();
                SelectedEquipment.Attributes = EquipmentAttributes.ToList();

                // 如果是新建装备体系，添加到列表
                if (SelectedEquipment.Id == 0)
                {
                    SelectedEquipment.Id = EquipmentSystems.Count > 0 ? EquipmentSystems.Max(e => e.Id) + 1 : 1;
                    EquipmentSystems.Add(SelectedEquipment);
                }

                await PersistEquipmentSystemsAsync();
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
        private async void AIAssistant_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!EnsureCurrentProject("AI装备体系"))
                {
                    return;
                }

                if (_aiAssistantService == null)
                {
                    ShowAIAssistantDialog();
                    return;
                }

                if (SelectedEquipment != null)
                {
                    var choice = MessageBox.Show(
                        "是：AI优化当前装备并保存\n否：AI生成新的装备并保存\n取消：打开原始AI助手",
                        "AI装备体系",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);

                    if (choice == MessageBoxResult.Cancel)
                    {
                        ShowAIAssistantDialog();
                        return;
                    }

                    await GenerateEquipmentWithAiAsync(optimizeCurrent: choice == MessageBoxResult.Yes);
                    return;
                }

                var generateChoice = MessageBox.Show(
                    "是：AI生成新的装备并保存\n否：打开原始AI助手",
                    "AI装备体系",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (generateChoice == MessageBoxResult.Yes)
                {
                    await GenerateEquipmentWithAiAsync(optimizeCurrent: false);
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

        /// <summary>
        /// 在项目切换后刷新装备体系数据。
        /// </summary>
        /// <param name="projectId">当前项目标识。</param>
        /// <param name="projectName">当前项目名称。</param>
        public async Task RefreshOnProjectChangedAsync(Guid? projectId, string? projectName)
        {
            _currentProjectId = projectId ?? Guid.Empty;
            await LoadEquipmentSystemsAsync();
        }

        /// <summary>
        /// 在导航到当前视图时刷新对应项目的装备体系数据。
        /// </summary>
        /// <param name="context">导航上下文。</param>
        public void OnNavigatedTo(NavigationContext context)
        {
            _currentProjectId = context.ProjectId ?? Guid.Empty;
            _ = LoadEquipmentSystemsAsync();
        }

        private async Task PersistEquipmentSystemsAsync()
        {
            if (_currentProjectId == Guid.Empty || _equipmentDataService == null)
            {
                return;
            }

            foreach (var equipment in EquipmentSystems)
            {
                equipment.Levels ??= new List<EquipmentLevelViewModel>();
                equipment.Attributes ??= new List<EquipmentAttributeViewModel>();
                equipment.LevelCount = equipment.Levels.Count;
                equipment.AttributeCount = equipment.Attributes.Count;
            }

            await _equipmentDataService.SaveEquipmentSystemsAsync(_currentProjectId, EquipmentSystems);
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

        private async Task GenerateEquipmentWithAiAsync(bool optimizeCurrent)
        {
            if (_aiAssistantService == null)
            {
                MessageBox.Show("AI助手服务未初始化。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var parameters = new Dictionary<string, object>
            {
                ["title"] = optimizeCurrent && SelectedEquipment != null ? $"优化装备体系：{SelectedEquipment.Name}" : "生成装备体系",
                ["theme"] = "请生成一个适合小说项目使用的装备体系，并输出名称、类别、描述、等级列表、属性列表。",
                ["requirements"] = optimizeCurrent && SelectedEquipment != null
                    ? $"请基于当前装备进行优化并输出结构化文本。当前装备：{SelectedEquipment.Name}，类别：{SelectedEquipment.Category}，描述：{SelectedEquipment.Description}"
                    : "请输出一个完整装备体系，至少包含名称、类别、描述、至少3个等级、至少2个属性。",
                ["context"] = GetCurrentContext()
            };

            var result = await _aiAssistantService.GenerateOutlineAsync(parameters);
            if (!result.IsSuccess || result.Data == null)
            {
                MessageBox.Show(result.Message ?? "AI生成失败。", "AI装备体系", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var generatedEquipment = ParseEquipmentFromAiResult(result.Data, optimizeCurrent ? SelectedEquipment : null);
            if (generatedEquipment == null)
            {
                MessageBox.Show("AI结果无法解析为装备体系。", "AI装备体系", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (optimizeCurrent && SelectedEquipment != null)
            {
                generatedEquipment.Id = SelectedEquipment.Id;
                generatedEquipment.CreatedAt = SelectedEquipment.CreatedAt;
                var index = EquipmentSystems.IndexOf(SelectedEquipment);
                if (index >= 0)
                {
                    EquipmentSystems[index] = generatedEquipment;
                }
                SelectedEquipment = generatedEquipment;
            }
            else
            {
                generatedEquipment.Id = EquipmentSystems.Count > 0 ? EquipmentSystems.Max(e => e.Id) + 1 : 1;
                generatedEquipment.CreatedAt = DateTime.Now;
                EquipmentSystems.Add(generatedEquipment);
                SelectedEquipment = generatedEquipment;
            }

            await PersistEquipmentSystemsAsync();
            FilterEquipmentSystems();
            UpdateStatistics();
            LoadEquipmentSystemDetails(generatedEquipment);
            ShowEditPanel();
            MessageBox.Show(
                optimizeCurrent ? $"已使用 AI 优化并保存装备：{generatedEquipment.Name}" : $"已使用 AI 生成并保存装备：{generatedEquipment.Name}",
                "AI装备体系",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private EquipmentSystemViewModel? ParseEquipmentFromAiResult(object data, EquipmentSystemViewModel? baseEquipment)
        {
            var text = data?.ToString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var equipment = new EquipmentSystemViewModel
            {
                Id = baseEquipment?.Id ?? 0,
                Name = ExtractField(text, "名称") ?? baseEquipment?.Name ?? ExtractFirstMeaningfulLine(text) ?? "AI生成装备体系",
                Category = ExtractField(text, "类别") ?? baseEquipment?.Category ?? "武器",
                Description = ExtractField(text, "描述") ?? baseEquipment?.Description ?? text.Trim(),
                CreatedAt = baseEquipment?.CreatedAt ?? DateTime.Now
            };

            equipment.Levels = ParseEquipmentLevels(text);
            equipment.Attributes = ParseEquipmentAttributes(text);

            if (equipment.Levels.Count == 0)
            {
                equipment.Levels = baseEquipment?.Levels?.ToList() ?? new List<EquipmentLevelViewModel>
                {
                    new() { Level = 1, Name = "凡品", Requirements = "普通材料" },
                    new() { Level = 2, Name = "精品", Requirements = "精良材料" },
                    new() { Level = 3, Name = "极品", Requirements = "稀有材料" }
                };
            }

            if (equipment.Attributes.Count == 0)
            {
                equipment.Attributes = baseEquipment?.Attributes?.ToList() ?? new List<EquipmentAttributeViewModel>
                {
                    new() { Name = "攻击力", Value = "100", Description = "AI生成的基础属性" },
                    new() { Name = "耐久度", Value = "1000", Description = "AI生成的基础属性" }
                };
            }

            equipment.LevelCount = equipment.Levels.Count;
            equipment.AttributeCount = equipment.Attributes.Count;
            return equipment;
        }

        private static List<EquipmentLevelViewModel> ParseEquipmentLevels(string text)
        {
            var levels = new List<EquipmentLevelViewModel>();
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var index = 1;
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (!Regex.IsMatch(line, "品|级|阶"))
                {
                    continue;
                }

                levels.Add(new EquipmentLevelViewModel
                {
                    Level = index++,
                    Name = TrimListMarker(line),
                    Requirements = "根据 AI 生成内容整理"
                });

                if (levels.Count >= 8)
                {
                    break;
                }
            }

            return levels;
        }

        private static List<EquipmentAttributeViewModel> ParseEquipmentAttributes(string text)
        {
            var attributes = new List<EquipmentAttributeViewModel>();
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (!Regex.IsMatch(line, "属性|攻击|防御|耐久|暴击|速度|灵力|效果"))
                {
                    continue;
                }

                attributes.Add(new EquipmentAttributeViewModel
                {
                    Name = TrimListMarker(line),
                    Value = "待定",
                    Description = line
                });

                if (attributes.Count >= 8)
                {
                    break;
                }
            }

            return attributes;
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

    #region ViewModel类

    /// <summary>
    /// 装备体系视图模型
    /// </summary>
    public class EquipmentSystemViewModel
    {
        /// <summary>
        /// 装备体系标识。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 装备体系名称。
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// 装备类别。
        /// </summary>
        public string Category { get; set; } = "";

        /// <summary>
        /// 装备体系描述。
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// 等级数量。
        /// </summary>
        public int LevelCount { get; set; }

        /// <summary>
        /// 属性数量。
        /// </summary>
        public int AttributeCount { get; set; }

        /// <summary>
        /// 创建时间。
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 装备等级列表。
        /// </summary>
        public List<EquipmentLevelViewModel> Levels { get; set; } = new();

        /// <summary>
        /// 装备属性列表。
        /// </summary>
        public List<EquipmentAttributeViewModel> Attributes { get; set; } = new();
    }

    /// <summary>
    /// 装备等级视图模型
    /// </summary>
    public class EquipmentLevelViewModel
    {
        /// <summary>
        /// 等级序号。
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// 等级名称。
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// 升级要求。
        /// </summary>
        public string Requirements { get; set; } = "";
    }

    /// <summary>
    /// 装备属性视图模型
    /// </summary>
    public class EquipmentAttributeViewModel
    {
        /// <summary>
        /// 属性名称。
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// 属性值。
        /// </summary>
        public string Value { get; set; } = "";

        /// <summary>
        /// 属性描述。
        /// </summary>
        public string Description { get; set; } = "";
    }

    #endregion
}
