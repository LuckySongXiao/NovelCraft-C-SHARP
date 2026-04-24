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
    /// 生民体系管理视图
    /// </summary>
    public partial class PopulationSystemView : UserControl, INavigationRefreshableView, INavigationAwareView
    {
        #region 属性

        /// <summary>
        /// 生民体系列表
        /// </summary>
        public ObservableCollection<PopulationSystemViewModel> PopulationSystems { get; set; }

        /// <summary>
        /// 当前选中的生民体系
        /// </summary>
        public PopulationSystemViewModel SelectedPopulationSystem { get; set; }

        /// <summary>
        /// 社会阶层列表
        /// </summary>
        public ObservableCollection<SocialClassViewModel> SocialClasses { get; set; }

        /// <summary>
        /// 选择生民体系命令
        /// </summary>
        public ICommand SelectPopulationSystemCommand { get; set; }

        /// <summary>
        /// 日志记录器
        /// </summary>
        private readonly ILogger<PopulationSystemView>? _logger;

        /// <summary>
        /// AI助手服务
        /// </summary>
        private readonly IAIAssistantService? _aiAssistantService;

        private readonly PopulationDataService? _populationDataService;
        private readonly ProjectContextService? _projectContextService;
        private readonly CurrentProjectGuard? _currentProjectGuard;
        private Guid _currentProjectId;

        /// <summary>
        /// 获取当前生民体系总数。
        /// </summary>
        public int TotalCount => PopulationSystems.Count;

        /// <summary>
        /// 获取修仙界生民体系数量。
        /// </summary>
        public int CultivationWorldCount => PopulationSystems.Count(s => s.RegionName == "修仙界");

        /// <summary>
        /// 获取凡人界生民体系数量。
        /// </summary>
        public int MortalWorldCount => PopulationSystems.Count(s => s.RegionName == "凡人界");

        /// <summary>
        /// 获取其他区域生民体系数量。
        /// </summary>
        public int OtherWorldCount => PopulationSystems.Count - CultivationWorldCount - MortalWorldCount;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化生民体系管理视图。
        /// </summary>
        public PopulationSystemView()
        {
            InitializeComponent();

            // 获取服务
            try
            {
                var serviceProvider = App.ServiceProvider;
                _logger = serviceProvider?.GetService<ILogger<PopulationSystemView>>();
                _aiAssistantService = serviceProvider?.GetService<IAIAssistantService>();
                _populationDataService = serviceProvider?.GetService<PopulationDataService>();
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
            _ = LoadPopulationSystemsAsync();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化数据
        /// </summary>
        private void InitializeData()
        {
            PopulationSystems = new ObservableCollection<PopulationSystemViewModel>();
            SocialClasses = new ObservableCollection<SocialClassViewModel>();
            
            // 设置数据上下文
            this.DataContext = this;
        }

        /// <summary>
        /// 初始化命令
        /// </summary>
        private void InitializeCommands()
        {
            SelectPopulationSystemCommand = new RelayCommand<PopulationSystemViewModel>(SelectPopulationSystem);
        }

        #endregion

        #region 数据加载

        /// <summary>
        /// 加载生民体系数据
        /// </summary>
        private async Task LoadPopulationSystemsAsync()
        {
            try
            {
                _currentProjectId = _projectContextService?.CurrentProjectId ?? Guid.Empty;
                if (_currentProjectId == Guid.Empty)
                {
                    _currentProjectGuard?.TryGetCurrentProjectId(Window.GetWindow(this), "生民体系管理", out _);
                    PopulationSystems.Clear();
                    PopulationSystemListControl.ItemsSource = PopulationSystems;
                    UpdateStatistics();
                    HideEditPanel();
                    return;
                }

                var populationSystems = _populationDataService == null
                    ? new List<PopulationSystemViewModel>()
                    : await _populationDataService.LoadPopulationSystemsAsync(_currentProjectId);

                PopulationSystems.Clear();
                foreach (var system in populationSystems)
                {
                    system.SocialClasses ??= new List<SocialClassViewModel>();
                    PopulationSystems.Add(system);
                }

                // 设置列表数据源
                PopulationSystemListControl.ItemsSource = PopulationSystems;

                // 更新统计信息
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载生民体系数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 更新统计信息
        /// </summary>
        private void UpdateStatistics()
        {
            PopulationSystemListControl.Items.Refresh();
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
            FilterPopulationSystems();
        }

        /// <summary>
        /// 区域筛选变化
        /// </summary>
        private void RegionFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterPopulationSystems();
        }

        /// <summary>
        /// 筛选生民体系
        /// </summary>
        private void FilterPopulationSystems()
        {
            var searchText = SearchTextBox?.Text?.ToLower() ?? "";
            var selectedRegion = (RegionFilterComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString();

            var filteredSystems = PopulationSystems.Where(s =>
                (string.IsNullOrEmpty(searchText) || 
                 s.Name.ToLower().Contains(searchText) || 
                 s.Description.ToLower().Contains(searchText) ||
                 s.RegionName.ToLower().Contains(searchText)) &&
                (selectedRegion == "全部区域" || selectedRegion == null || s.RegionName == selectedRegion)
            ).ToList();

            PopulationSystemListControl.ItemsSource = filteredSystems;
        }

        /// <summary>
        /// 选择生民体系
        /// </summary>
        private void SelectPopulationSystem(PopulationSystemViewModel system)
        {
            if (system == null) return;

            SelectedPopulationSystem = system;
            LoadPopulationSystemDetails(system);
            ShowEditPanel();
        }

        /// <summary>
        /// 加载生民体系详情
        /// </summary>
        private void LoadPopulationSystemDetails(PopulationSystemViewModel system)
        {
            // 填充基本信息
            PopulationNameTextBox.Text = system.Name;
            PopulationDescriptionTextBox.Text = system.Description;
            RegionNameTextBox.Text = system.RegionName;
            DimensionIdTextBox.Text = system.DimensionId;
            TotalPopulationTextBox.Text = system.TotalPopulation;
            PopulationDensityTextBox.Text = system.PopulationDensity;
            GrowthRateTextBox.Text = system.GrowthRate.ToString();
            
            // 设置发展水平选择
            foreach (ComboBoxItem item in DevelopmentLevelComboBox.Items)
            {
                if (item.Content.ToString() == system.DevelopmentLevel)
                {
                    DevelopmentLevelComboBox.SelectedItem = item;
                    break;
                }
            }

            // 加载社会阶层列表
            LoadSocialClasses(system);
        }

        /// <summary>
        /// 加载社会阶层列表
        /// </summary>
        private void LoadSocialClasses(PopulationSystemViewModel system)
        {
            var socialClasses = system.SocialClasses ?? new List<SocialClassViewModel>();

            SocialClasses.Clear();
            foreach (var socialClass in socialClasses)
            {
                SocialClasses.Add(socialClass);
            }

            SocialClassListControl.ItemsSource = SocialClasses;
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
            SelectedPopulationSystem = null;
        }

        #endregion

        #region 按钮事件

        /// <summary>
        /// 新建生民体系
        /// </summary>
        private void AddPopulationSystem_Click(object sender, RoutedEventArgs e)
        {
            var newSystem = new PopulationSystemViewModel
            {
                Id = 0,
                Name = "",
                RegionName = "",
                DimensionId = "",
                Description = "",
                TotalPopulation = "",
                PopulationDensity = "",
                GrowthRate = 0,
                DevelopmentLevel = "封建社会",
                CreatedAt = DateTime.Now
            };

            SelectedPopulationSystem = newSystem;
            LoadPopulationSystemDetails(newSystem);
            ShowEditPanel();

            // 清空社会阶层列表
            SocialClasses.Clear();
            SocialClassListControl.ItemsSource = SocialClasses;

            // 聚焦到名称输入框
            PopulationNameTextBox.Focus();
        }

        /// <summary>
        /// 导入生民数据
        /// </summary>
        private async void ImportPopulation_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!EnsureCurrentProject("导入生民体系"))
                {
                    return;
                }

                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "导入生民体系数据",
                    Filter = "JSON文件|*.json|所有文件|*.*",
                    DefaultExt = "json"
                };

                if (dialog.ShowDialog() == true)
                {
                    if (_populationDataService == null)
                    {
                        MessageBox.Show("生民数据服务未初始化。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var importedSystems = await _populationDataService.ImportPopulationSystemsAsync(dialog.FileName);
                    PopulationSystems.Clear();
                    foreach (var system in importedSystems)
                    {
                        system.SocialClasses ??= new List<SocialClassViewModel>();
                        PopulationSystems.Add(system);
                    }

                    await PersistPopulationSystemsAsync();
                    FilterPopulationSystems();
                    UpdateStatistics();
                    MessageBox.Show($"已成功导入 {PopulationSystems.Count} 个生民体系。", "导入成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 导出生民数据
        /// </summary>
        private async void ExportPopulation_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!EnsureCurrentProject("导出生民体系"))
                {
                    return;
                }

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "导出生民体系数据",
                    Filter = "JSON文件|*.json",
                    DefaultExt = "json",
                    FileName = $"生民体系数据_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (dialog.ShowDialog() == true)
                {
                    if (_populationDataService == null)
                    {
                        MessageBox.Show("生民数据服务未初始化。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    await _populationDataService.ExportPopulationSystemsAsync(_currentProjectId, PopulationSystems, dialog.FileName);
                    MessageBox.Show($"生民体系数据已导出到：{dialog.FileName}", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 添加社会阶层
        /// </summary>
        private void AddSocialClass_Click(object sender, RoutedEventArgs e)
        {
            var newClass = new SocialClassViewModel
            {
                Name = "",
                Population = "",
                Description = ""
            };

            SocialClasses.Add(newClass);
        }

        /// <summary>
        /// 删除社会阶层
        /// </summary>
        private void RemoveSocialClass_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is SocialClassViewModel socialClass)
            {
                SocialClasses.Remove(socialClass);
            }
        }

        /// <summary>
        /// 保存生民体系
        /// </summary>
        private async void SavePopulationSystem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SelectedPopulationSystem == null) return;

                // 验证输入
                if (string.IsNullOrWhiteSpace(PopulationNameTextBox.Text))
                {
                    MessageBox.Show("请输入生民体系名称", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(RegionNameTextBox.Text))
                {
                    MessageBox.Show("请输入区域名称", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 更新生民体系信息
                SelectedPopulationSystem.Name = PopulationNameTextBox.Text.Trim();
                SelectedPopulationSystem.Description = PopulationDescriptionTextBox.Text.Trim();
                SelectedPopulationSystem.RegionName = RegionNameTextBox.Text.Trim();
                SelectedPopulationSystem.DimensionId = DimensionIdTextBox.Text.Trim();
                SelectedPopulationSystem.TotalPopulation = TotalPopulationTextBox.Text.Trim();
                SelectedPopulationSystem.PopulationDensity = PopulationDensityTextBox.Text.Trim();
                SelectedPopulationSystem.DevelopmentLevel = (DevelopmentLevelComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "封建社会";
                
                if (double.TryParse(GrowthRateTextBox.Text, out double growthRate))
                {
                    SelectedPopulationSystem.GrowthRate = growthRate;
                }
                SelectedPopulationSystem.SocialClasses = SocialClasses.ToList();

                // 如果是新建生民体系，添加到列表
                if (SelectedPopulationSystem.Id == 0)
                {
                    SelectedPopulationSystem.Id = PopulationSystems.Count > 0 ? PopulationSystems.Max(s => s.Id) + 1 : 1;
                    PopulationSystems.Add(SelectedPopulationSystem);
                }

                await PersistPopulationSystemsAsync();
                MessageBox.Show("生民体系保存成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);

                // 刷新列表
                FilterPopulationSystems();
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
                if (!EnsureCurrentProject("AI生民体系"))
                {
                    return;
                }

                if (_aiAssistantService == null)
                {
                    ShowAIAssistantDialog();
                    return;
                }

                if (SelectedPopulationSystem != null)
                {
                    var choice = MessageBox.Show(
                        "是：AI优化当前生民体系并保存\n否：AI生成新的生民体系并保存\n取消：打开原始AI助手",
                        "AI生民体系",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);

                    if (choice == MessageBoxResult.Cancel)
                    {
                        ShowAIAssistantDialog();
                        return;
                    }

                    await GeneratePopulationSystemWithAiAsync(optimizeCurrent: choice == MessageBoxResult.Yes);
                    return;
                }

                var generateChoice = MessageBox.Show(
                    "是：AI生成新的生民体系并保存\n否：打开原始AI助手",
                    "AI生民体系",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (generateChoice == MessageBoxResult.Yes)
                {
                    await GeneratePopulationSystemWithAiAsync(optimizeCurrent: false);
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
            var dialog = new AIAssistantDialog("生民体系管理", contextString);
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
                ["interfaceType"] = "生民体系管理",
                ["totalPopulationSystems"] = PopulationSystems.Count,
                ["selectedPopulationSystem"] = SelectedPopulationSystem,
                ["regions"] = new[] { "修仙界", "凡人界", "魔界", "妖界", "仙界" },
                ["developmentLevels"] = new[] { "原始社会", "奴隶社会", "封建社会", "资本主义社会", "修仙文明", "高等文明" }
            };

            if (SelectedPopulationSystem != null)
            {
                context["currentSystemName"] = SelectedPopulationSystem.Name;
                context["currentSystemRegion"] = SelectedPopulationSystem.RegionName;
                context["currentSystemDimension"] = SelectedPopulationSystem.DimensionId;
                context["currentSystemDescription"] = SelectedPopulationSystem.Description;
                context["currentSystemPopulation"] = new
                {
                    Total = SelectedPopulationSystem.TotalPopulation,
                    Density = SelectedPopulationSystem.PopulationDensity,
                    GrowthRate = SelectedPopulationSystem.GrowthRate,
                    DevelopmentLevel = SelectedPopulationSystem.DevelopmentLevel
                };
                context["currentSystemSocialClasses"] = SocialClasses.ToList();
            }

            // 添加统计信息
            var regionStats = PopulationSystems.GroupBy(s => s.RegionName)
                .ToDictionary(g => g.Key, g => g.Count());
            context["regionStatistics"] = regionStats;

            var developmentStats = PopulationSystems.GroupBy(s => s.DevelopmentLevel)
                .ToDictionary(g => g.Key, g => g.Count());
            context["developmentStatistics"] = developmentStats;

            // 添加人口分析数据
            var populationAnalysis = new
            {
                TotalSystems = PopulationSystems.Count,
                AverageGrowthRate = PopulationSystems.Average(s => s.GrowthRate),
                HighestGrowthRate = PopulationSystems.Max(s => s.GrowthRate),
                LowestGrowthRate = PopulationSystems.Min(s => s.GrowthRate)
            };
            context["populationAnalysis"] = populationAnalysis;

            return context;
        }

        /// <summary>
        /// 在项目切换后刷新生民体系数据。
        /// </summary>
        /// <param name="projectId">当前项目标识。</param>
        /// <param name="projectName">当前项目名称。</param>
        public async Task RefreshOnProjectChangedAsync(Guid? projectId, string? projectName)
        {
            _currentProjectId = projectId ?? Guid.Empty;
            await LoadPopulationSystemsAsync();
        }

        /// <summary>
        /// 在导航到当前视图时刷新对应项目的生民体系数据。
        /// </summary>
        /// <param name="context">导航上下文。</param>
        public void OnNavigatedTo(NavigationContext context)
        {
            _currentProjectId = context.ProjectId ?? Guid.Empty;
            _ = LoadPopulationSystemsAsync();
        }

        private async Task PersistPopulationSystemsAsync()
        {
            if (_currentProjectId == Guid.Empty || _populationDataService == null)
            {
                return;
            }

            foreach (var system in PopulationSystems)
            {
                system.SocialClasses ??= new List<SocialClassViewModel>();
            }

            await _populationDataService.SavePopulationSystemsAsync(_currentProjectId, PopulationSystems);
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

        private async Task GeneratePopulationSystemWithAiAsync(bool optimizeCurrent)
        {
            if (_aiAssistantService == null)
            {
                MessageBox.Show("AI助手服务未初始化。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var parameters = new Dictionary<string, object>
            {
                ["title"] = optimizeCurrent && SelectedPopulationSystem != null ? $"优化生民体系：{SelectedPopulationSystem.Name}" : "生成生民体系",
                ["theme"] = "请生成一个适合小说项目使用的生民体系设定，并输出名称、区域、维度、描述、总人口、人口密度、增长率、发展水平、社会阶层。",
                ["requirements"] = optimizeCurrent && SelectedPopulationSystem != null
                    ? $"请基于当前生民体系进行优化并输出结构化文本。当前体系：{SelectedPopulationSystem.Name}，区域：{SelectedPopulationSystem.RegionName}，总人口：{SelectedPopulationSystem.TotalPopulation}，发展水平：{SelectedPopulationSystem.DevelopmentLevel}"
                    : "请输出一个完整生民体系，至少包含名称、区域、维度、描述、总人口、人口密度、增长率、发展水平、至少3个社会阶层。",
                ["context"] = GetCurrentContext()
            };

            var result = await _aiAssistantService.GenerateOutlineAsync(parameters);
            if (!result.IsSuccess || result.Data == null)
            {
                MessageBox.Show(result.Message ?? "AI生成失败。", "AI生民体系", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var generatedSystem = ParsePopulationSystemFromAiResult(result.Data, optimizeCurrent ? SelectedPopulationSystem : null);
            if (generatedSystem == null)
            {
                MessageBox.Show("AI结果无法解析为生民体系。", "AI生民体系", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (optimizeCurrent && SelectedPopulationSystem != null)
            {
                generatedSystem.Id = SelectedPopulationSystem.Id;
                generatedSystem.CreatedAt = SelectedPopulationSystem.CreatedAt;
                var index = PopulationSystems.IndexOf(SelectedPopulationSystem);
                if (index >= 0)
                {
                    PopulationSystems[index] = generatedSystem;
                }
                SelectedPopulationSystem = generatedSystem;
            }
            else
            {
                generatedSystem.Id = PopulationSystems.Count > 0 ? PopulationSystems.Max(s => s.Id) + 1 : 1;
                generatedSystem.CreatedAt = DateTime.Now;
                PopulationSystems.Add(generatedSystem);
                SelectedPopulationSystem = generatedSystem;
            }

            await PersistPopulationSystemsAsync();
            FilterPopulationSystems();
            UpdateStatistics();
            LoadPopulationSystemDetails(generatedSystem);
            ShowEditPanel();
            MessageBox.Show(
                optimizeCurrent ? $"已使用 AI 优化并保存生民体系：{generatedSystem.Name}" : $"已使用 AI 生成并保存生民体系：{generatedSystem.Name}",
                "AI生民体系",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private PopulationSystemViewModel? ParsePopulationSystemFromAiResult(object data, PopulationSystemViewModel? baseSystem)
        {
            var text = data?.ToString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var system = new PopulationSystemViewModel
            {
                Id = baseSystem?.Id ?? 0,
                Name = ExtractField(text, "名称") ?? baseSystem?.Name ?? ExtractFirstMeaningfulLine(text) ?? "AI生成生民体系",
                RegionName = ExtractField(text, "区域") ?? baseSystem?.RegionName ?? "修仙界",
                DimensionId = ExtractField(text, "维度") ?? baseSystem?.DimensionId ?? "DIM-AI",
                Description = ExtractField(text, "描述") ?? baseSystem?.Description ?? text.Trim(),
                TotalPopulation = ExtractField(text, "总人口") ?? baseSystem?.TotalPopulation ?? "1000万",
                PopulationDensity = ExtractField(text, "人口密度") ?? baseSystem?.PopulationDensity ?? "中等",
                GrowthRate = ExtractDoubleField(text, "增长率") ?? baseSystem?.GrowthRate ?? 1.5,
                DevelopmentLevel = ExtractField(text, "发展水平") ?? baseSystem?.DevelopmentLevel ?? "封建社会",
                CreatedAt = baseSystem?.CreatedAt ?? DateTime.Now,
                SocialClasses = ParseSocialClasses(text)
            };

            if (system.SocialClasses.Count == 0)
            {
                system.SocialClasses = baseSystem?.SocialClasses?.ToList() ?? new List<SocialClassViewModel>
                {
                    new() { Name = "统治阶层", Population = "10万", Description = "AI生成的统治阶层" },
                    new() { Name = "中坚阶层", Population = "500万", Description = "AI生成的中坚阶层" },
                    new() { Name = "普通居民", Population = "900万", Description = "AI生成的普通居民" }
                };
            }

            return system;
        }

        private static List<SocialClassViewModel> ParseSocialClasses(string text)
        {
            var classes = new List<SocialClassViewModel>();
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (!Regex.IsMatch(line, "阶层|族群|居民|贵族|平民|修士|百姓|商人|农民"))
                {
                    continue;
                }

                classes.Add(new SocialClassViewModel
                {
                    Name = TrimListMarker(line),
                    Population = "待定",
                    Description = line
                });

                if (classes.Count >= 8)
                {
                    break;
                }
            }

            return classes;
        }

        private static string? ExtractField(string text, string fieldName)
        {
            var match = Regex.Match(text, $"{fieldName}\\s*[:：]\\s*(.+)");
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }

        private static double? ExtractDoubleField(string text, string fieldName)
        {
            var value = ExtractField(text, fieldName);
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var numberMatch = Regex.Match(value, @"-?\d+(\.\d+)?");
            return numberMatch.Success && double.TryParse(numberMatch.Value, out var result) ? result : null;
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
    /// 生民体系视图模型
    /// </summary>
    public class PopulationSystemViewModel
    {
        /// <summary>
        /// 生民体系标识。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 生民体系名称。
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// 所属区域名称。
        /// </summary>
        public string RegionName { get; set; } = "";

        /// <summary>
        /// 所属维度标识。
        /// </summary>
        public string DimensionId { get; set; } = "";

        /// <summary>
        /// 生民体系描述。
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// 总人口规模。
        /// </summary>
        public string TotalPopulation { get; set; } = "";

        /// <summary>
        /// 人口密度描述。
        /// </summary>
        public string PopulationDensity { get; set; } = "";

        /// <summary>
        /// 人口增长率。
        /// </summary>
        public double GrowthRate { get; set; }

        /// <summary>
        /// 发展水平。
        /// </summary>
        public string DevelopmentLevel { get; set; } = "";

        /// <summary>
        /// 创建时间。
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 社会阶层列表。
        /// </summary>
        public List<SocialClassViewModel> SocialClasses { get; set; } = new();
    }

    /// <summary>
    /// 社会阶层视图模型
    /// </summary>
    public class SocialClassViewModel
    {
        /// <summary>
        /// 阶层名称。
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// 阶层人口规模。
        /// </summary>
        public string Population { get; set; } = "";

        /// <summary>
        /// 阶层描述。
        /// </summary>
        public string Description { get; set; } = "";
    }

    #endregion
}
