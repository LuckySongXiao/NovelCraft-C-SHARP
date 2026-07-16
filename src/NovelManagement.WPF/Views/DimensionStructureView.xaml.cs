using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NovelManagement.WPF.Commands;
using NovelManagement.WPF.Services;

namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// 维度结构管理视图
    /// </summary>
    public partial class DimensionStructureView : UserControl, INavigationRefreshableView, INavigationAwareView
    {
        private readonly ILogger<DimensionStructureView>? _logger;
        private readonly IAIAssistantService? _aiAssistantService;
        private readonly DimensionDataService? _dimensionDataService;
        private readonly ProjectContextService? _projectContextService;
        private readonly CurrentProjectGuard? _currentProjectGuard;
        private Guid _currentProjectId;

        public ObservableCollection<DimensionViewModel> Dimensions { get; } = new();
        public ObservableCollection<PortalViewModel> Portals { get; } = new();
        public DimensionViewModel? SelectedDimension { get; set; }
        public ICommand SelectDimensionCommand { get; }

        public int TotalCount => Dimensions.Count;
        public int MaterialCount => Dimensions.Count(dimension => dimension.Type == "物质维度");
        public int SpiritualCount => Dimensions.Count(dimension => dimension.Type == "精神维度");
        public int StableCount => Dimensions.Count(dimension => dimension.Stability == "极稳定" || dimension.Stability == "稳定");

        /// <summary>
        /// 初始化维度结构管理视图。
        /// </summary>
        public DimensionStructureView()
        {
            InitializeComponent();

            try
            {
                var serviceProvider = App.ServiceProvider;
                _logger = serviceProvider?.GetService<ILogger<DimensionStructureView>>();
                _aiAssistantService = serviceProvider?.GetService<IAIAssistantService>();
                _dimensionDataService = serviceProvider?.GetService<DimensionDataService>();
                _projectContextService = serviceProvider?.GetService<ProjectContextService>();
                _currentProjectGuard = serviceProvider?.GetService<CurrentProjectGuard>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取服务失败: {ex.Message}");
            }

            SelectDimensionCommand = new RelayCommand<DimensionViewModel>(SelectDimension);
            DataContext = this;
            PortalListControl.ItemsSource = Portals;
            _ = LoadDimensionsAsync();
        }

        private async Task LoadDimensionsAsync()
        {
            try
            {
                _currentProjectId = _projectContextService?.CurrentProjectId ?? Guid.Empty;
                if (_currentProjectId == Guid.Empty)
                {
                    _currentProjectGuard?.TryGetCurrentProjectId(Window.GetWindow(this), "维度结构管理", out _);
                    Dimensions.Clear();
                    Portals.Clear();
                    DimensionListControl.ItemsSource = Dimensions;
                    UpdateStatistics();
                    HideEditPanel();
                    return;
                }

                var dimensions = _dimensionDataService == null
                    ? new List<DimensionViewModel>()
                    : await _dimensionDataService.LoadDimensionsAsync(_currentProjectId);

                Dimensions.Clear();
                foreach (var dimension in dimensions.OrderBy(dimension => dimension.Name))
                {
                    dimension.Portals ??= new List<PortalViewModel>();
                    Dimensions.Add(dimension);
                }

                DimensionListControl.ItemsSource = Dimensions;
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载维度数据失败");
                MessageBox.Show($"加载维度数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStatistics()
        {
            DimensionListControl.Items.Refresh();
            DataContext = null;
            DataContext = this;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterDimensions();
        }

        private void DimensionTypeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterDimensions();
        }

        private void StabilityFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterDimensions();
        }

        private void FilterDimensions()
        {
            var searchText = SearchTextBox?.Text?.ToLowerInvariant() ?? string.Empty;
            var selectedType = (DimensionTypeFilterComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString();
            var selectedStability = (StabilityFilterComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString();

            var filteredDimensions = Dimensions.Where(dimension =>
                (string.IsNullOrWhiteSpace(searchText) ||
                 dimension.Name.ToLowerInvariant().Contains(searchText) ||
                 dimension.Description.ToLowerInvariant().Contains(searchText) ||
                 dimension.Type.ToLowerInvariant().Contains(searchText)) &&
                (selectedType == "全部类型" || selectedType == null || dimension.Type == selectedType) &&
                (selectedStability == "全部稳定性" || selectedStability == null || dimension.Stability == selectedStability))
                .ToList();

            DimensionListControl.ItemsSource = filteredDimensions;
        }

        private void SelectDimension(DimensionViewModel? dimension)
        {
            if (dimension == null)
            {
                return;
            }

            SelectedDimension = dimension;
            LoadDimensionDetails(dimension);
            ShowEditPanel();
        }

        private void LoadDimensionDetails(DimensionViewModel dimension)
        {
            DimensionNameTextBox.Text = dimension.Name;
            DimensionDescriptionTextBox.Text = dimension.Description;
            EnvironmentTypeTextBox.Text = dimension.EnvironmentType;
            ClimateTextBox.Text = dimension.Climate;
            EnergyLevelTextBox.Text = dimension.EnergyLevel;
            DangerLevelTextBox.Text = dimension.DangerLevel;

            SelectComboBoxItem(DimensionTypeComboBox, dimension.Type);
            SelectComboBoxItem(StabilityComboBox, dimension.Stability);
            SelectComboBoxItem(AccessLevelComboBox, dimension.AccessLevel);

            LoadPortals(dimension);
        }

        private void LoadPortals(DimensionViewModel dimension)
        {
            Portals.Clear();
            foreach (var portal in dimension.Portals ?? Enumerable.Empty<PortalViewModel>())
            {
                Portals.Add(new PortalViewModel
                {
                    Name = portal.Name,
                    TargetDimension = portal.TargetDimension,
                    Status = portal.Status
                });
            }

            PortalListControl.ItemsSource = Portals;
        }

        private static void SelectComboBoxItem(ComboBox comboBox, string? value)
        {
            foreach (ComboBoxItem item in comboBox.Items)
            {
                if (item.Content?.ToString() == value)
                {
                    comboBox.SelectedItem = item;
                    return;
                }
            }

            if (comboBox.Items.Count > 0)
            {
                comboBox.SelectedIndex = 0;
            }
        }

        private void ShowEditPanel()
        {
            EmptyStatePanel.Visibility = Visibility.Collapsed;
            EditPanel.Visibility = Visibility.Visible;
        }

        private void HideEditPanel()
        {
            EmptyStatePanel.Visibility = Visibility.Visible;
            EditPanel.Visibility = Visibility.Collapsed;
            SelectedDimension = null;
            Portals.Clear();
        }

        private void AddDimension_Click(object sender, RoutedEventArgs e)
        {
            var newDimension = new DimensionViewModel
            {
                Type = "物质维度",
                Stability = "稳定",
                AccessLevel = "公开",
                CreatedAt = DateTime.Now,
                Portals = new List<PortalViewModel>()
            };

            SelectedDimension = newDimension;
            LoadDimensionDetails(newDimension);
            ShowEditPanel();
            DimensionNameTextBox.Focus();
        }

        private async void ImportDimension_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!EnsureCurrentProject("导入维度结构"))
                {
                    return;
                }

                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "导入维度结构数据",
                    Filter = "JSON文件|*.json|所有文件|*.*",
                    DefaultExt = "json"
                };

                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                if (_dimensionDataService == null)
                {
                    MessageBox.Show("维度数据服务未初始化。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var importedDimensions = await _dimensionDataService.ImportDimensionsAsync(dialog.FileName);
                Dimensions.Clear();
                foreach (var dimension in importedDimensions)
                {
                    dimension.Portals ??= new List<PortalViewModel>();
                    Dimensions.Add(dimension);
                }

                await PersistDimensionsAsync();
                FilterDimensions();
                UpdateStatistics();
                MessageBox.Show($"已成功导入 {Dimensions.Count} 个维度。", "导入成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "导入维度数据失败");
                MessageBox.Show($"导入失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExportDimension_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!EnsureCurrentProject("导出维度结构"))
                {
                    return;
                }

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "导出维度结构数据",
                    Filter = "JSON文件|*.json",
                    DefaultExt = "json",
                    FileName = $"维度结构数据_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                if (_dimensionDataService == null)
                {
                    MessageBox.Show("维度数据服务未初始化。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                await _dimensionDataService.ExportDimensionsAsync(_currentProjectId, Dimensions, dialog.FileName);
                MessageBox.Show($"维度结构数据已导出到：{dialog.FileName}", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "导出维度数据失败");
                MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddPortal_Click(object sender, RoutedEventArgs e)
        {
            Portals.Add(new PortalViewModel
            {
                Status = "正常"
            });
        }

        private void RemovePortal_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { CommandParameter: PortalViewModel portal })
            {
                Portals.Remove(portal);
            }
        }

        private async void SaveDimension_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SelectedDimension == null)
                {
                    return;
                }

                if (!EnsureCurrentProject("保存维度结构"))
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(DimensionNameTextBox.Text))
                {
                    MessageBox.Show("请输入维度名称", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                SelectedDimension.Name = DimensionNameTextBox.Text.Trim();
                SelectedDimension.Description = DimensionDescriptionTextBox.Text.Trim();
                SelectedDimension.Type = (DimensionTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "物质维度";
                SelectedDimension.Stability = (StabilityComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "稳定";
                SelectedDimension.AccessLevel = (AccessLevelComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "公开";
                SelectedDimension.EnvironmentType = EnvironmentTypeTextBox.Text.Trim();
                SelectedDimension.Climate = ClimateTextBox.Text.Trim();
                SelectedDimension.EnergyLevel = EnergyLevelTextBox.Text.Trim();
                SelectedDimension.DangerLevel = DangerLevelTextBox.Text.Trim();
                SelectedDimension.Portals = Portals
                    .Where(portal => !string.IsNullOrWhiteSpace(portal.Name) || !string.IsNullOrWhiteSpace(portal.TargetDimension))
                    .Select(portal => new PortalViewModel
                    {
                        Name = portal.Name.Trim(),
                        TargetDimension = portal.TargetDimension.Trim(),
                        Status = string.IsNullOrWhiteSpace(portal.Status) ? "正常" : portal.Status.Trim()
                    })
                    .ToList();

                if (SelectedDimension.Id == 0)
                {
                    SelectedDimension.Id = Dimensions.Count > 0 ? Dimensions.Max(dimension => dimension.Id) + 1 : 1;
                    Dimensions.Add(SelectedDimension);
                }

                await PersistDimensionsAsync();
                MessageBox.Show("维度保存成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                FilterDimensions();
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存维度失败");
                MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelEdit_Click(object sender, RoutedEventArgs e)
        {
            HideEditPanel();
        }

        private async void AIAssistant_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!EnsureCurrentProject("AI维度结构"))
                {
                    return;
                }

                if (_aiAssistantService == null)
                {
                    ShowAIAssistantDialog();
                    return;
                }

                if (SelectedDimension != null)
                {
                    var choice = MessageBox.Show(
                        "是：AI优化当前维度并保存\n否：AI生成新的维度并保存\n取消：打开原始AI助手",
                        "AI维度结构",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);

                    if (choice == MessageBoxResult.Cancel)
                    {
                        ShowAIAssistantDialog();
                        return;
                    }

                    await GenerateDimensionWithAiAsync(choice == MessageBoxResult.Yes);
                    return;
                }

                var generateChoice = MessageBox.Show(
                    "是：AI生成新的维度并保存\n否：打开原始AI助手",
                    "AI维度结构",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (generateChoice == MessageBoxResult.Yes)
                {
                    await GenerateDimensionWithAiAsync(false);
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

        private void ShowAIAssistantDialog()
        {
            var context = GetCurrentContext();
            var contextString = System.Text.Json.JsonSerializer.Serialize(
                context,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            var dialog = new AIAssistantDialog("维度结构管理", contextString)
            {
                Owner = Window.GetWindow(this)
            };
            dialog.ShowDialog();
        }

        private Dictionary<string, object> GetCurrentContext()
        {
            var context = new Dictionary<string, object>
            {
                ["interfaceType"] = "维度结构管理",
                ["totalDimensions"] = Dimensions.Count,
                ["selectedDimension"] = SelectedDimension,
                ["dimensionTypes"] = new[] { "物质维度", "精神维度", "能量维度", "时间维度", "空间维度", "混合维度" },
                ["stabilityLevels"] = new[] { "极稳定", "稳定", "不稳定", "极不稳定", "崩塌中" },
                ["accessLevels"] = new[] { "公开", "限制", "机密", "禁止", "未知" }
            };

            if (SelectedDimension != null)
            {
                context["currentDimensionName"] = SelectedDimension.Name;
                context["currentDimensionType"] = SelectedDimension.Type;
                context["currentDimensionStability"] = SelectedDimension.Stability;
                context["currentDimensionAccessLevel"] = SelectedDimension.AccessLevel;
                context["currentDimensionDescription"] = SelectedDimension.Description;
                context["currentDimensionEnvironment"] = new
                {
                    SelectedDimension.EnvironmentType,
                    SelectedDimension.Climate,
                    SelectedDimension.EnergyLevel,
                    SelectedDimension.DangerLevel
                };
                context["currentDimensionPortals"] = Portals.ToList();
            }

            context["typeStatistics"] = Dimensions.GroupBy(dimension => dimension.Type).ToDictionary(group => group.Key, group => group.Count());
            context["stabilityStatistics"] = Dimensions.GroupBy(dimension => dimension.Stability).ToDictionary(group => group.Key, group => group.Count());
            return context;
        }

        public async Task RefreshOnProjectChangedAsync(Guid? projectId, string? projectName)
        {
            _currentProjectId = projectId ?? Guid.Empty;
            await LoadDimensionsAsync();
        }

        public void OnNavigatedTo(NavigationContext context)
        {
            _currentProjectId = context.ProjectId ?? Guid.Empty;
            _ = LoadDimensionsAsync();
        }

        private async Task PersistDimensionsAsync()
        {
            if (_currentProjectId == Guid.Empty || _dimensionDataService == null)
            {
                return;
            }

            foreach (var dimension in Dimensions)
            {
                dimension.Portals ??= new List<PortalViewModel>();
            }

            await _dimensionDataService.SaveDimensionsAsync(_currentProjectId, Dimensions);
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

        private async Task GenerateDimensionWithAiAsync(bool optimizeCurrent)
        {
            if (_aiAssistantService == null)
            {
                MessageBox.Show("AI助手服务未初始化。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var parameters = new Dictionary<string, object>
            {
                ["title"] = optimizeCurrent && SelectedDimension != null ? $"优化维度：{SelectedDimension.Name}" : "生成维度结构",
                ["theme"] = "请生成一个适合小说项目使用的维度设定，并输出名称、类型、稳定性、访问等级、描述、环境类型、气候、能量等级、危险等级、传送门。",
                ["requirements"] = optimizeCurrent && SelectedDimension != null
                    ? $"请基于当前维度进行优化并输出结构化文本。当前维度：{SelectedDimension.Name}，类型：{SelectedDimension.Type}，稳定性：{SelectedDimension.Stability}，访问等级：{SelectedDimension.AccessLevel}，描述：{SelectedDimension.Description}"
                    : "请输出一个完整维度设定，至少包含名称、类型、稳定性、访问等级、描述、环境类型、气候、能量等级、危险等级，并列出至少2个传送门。",
                ["context"] = GetCurrentContext()
            };

            var result = await _aiAssistantService.GenerateOutlineAsync(parameters);
            if (!result.IsSuccess || result.Data == null)
            {
                MessageBox.Show(result.Message ?? "AI生成失败。", "AI维度结构", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var generatedDimension = ParseDimensionFromAiResult(result.Data, optimizeCurrent ? SelectedDimension : null);
            if (generatedDimension == null)
            {
                MessageBox.Show("AI结果无法解析为维度。", "AI维度结构", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (optimizeCurrent && SelectedDimension != null)
            {
                generatedDimension.Id = SelectedDimension.Id;
                generatedDimension.CreatedAt = SelectedDimension.CreatedAt;
                var index = Dimensions.IndexOf(SelectedDimension);
                if (index >= 0)
                {
                    Dimensions[index] = generatedDimension;
                }
                SelectedDimension = generatedDimension;
            }
            else
            {
                generatedDimension.Id = Dimensions.Count > 0 ? Dimensions.Max(dimension => dimension.Id) + 1 : 1;
                generatedDimension.CreatedAt = DateTime.Now;
                Dimensions.Add(generatedDimension);
                SelectedDimension = generatedDimension;
            }

            await PersistDimensionsAsync();
            FilterDimensions();
            UpdateStatistics();
            LoadDimensionDetails(generatedDimension);
            ShowEditPanel();
            MessageBox.Show(
                optimizeCurrent ? $"已使用 AI 优化并保存维度：{generatedDimension.Name}" : $"已使用 AI 生成并保存维度：{generatedDimension.Name}",
                "AI维度结构",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private DimensionViewModel? ParseDimensionFromAiResult(object data, DimensionViewModel? baseDimension)
        {
            var text = data?.ToString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var dimension = new DimensionViewModel
            {
                Id = baseDimension?.Id ?? 0,
                Name = ExtractField(text, "名称") ?? baseDimension?.Name ?? ExtractFirstMeaningfulLine(text) ?? "AI生成维度",
                Type = ExtractField(text, "类型") ?? baseDimension?.Type ?? "物质维度",
                Stability = ExtractField(text, "稳定性") ?? baseDimension?.Stability ?? "稳定",
                AccessLevel = ExtractField(text, "访问等级") ?? baseDimension?.AccessLevel ?? "限制",
                Description = ExtractField(text, "描述") ?? baseDimension?.Description ?? text.Trim(),
                EnvironmentType = ExtractField(text, "环境类型") ?? baseDimension?.EnvironmentType ?? "复合环境",
                Climate = ExtractField(text, "气候") ?? baseDimension?.Climate ?? "多变",
                EnergyLevel = ExtractField(text, "能量等级") ?? baseDimension?.EnergyLevel ?? "高",
                DangerLevel = ExtractField(text, "危险等级") ?? baseDimension?.DangerLevel ?? "中等",
                CreatedAt = baseDimension?.CreatedAt ?? DateTime.Now,
                Portals = ParsePortals(text)
            };

            if (dimension.Portals.Count == 0)
            {
                dimension.Portals = baseDimension?.Portals?.ToList() ?? new List<PortalViewModel>
                {
                    new() { Name = "主通道", TargetDimension = "相邻维度", Status = "正常" },
                    new() { Name = "裂隙入口", TargetDimension = "未知区域", Status = "不稳定" }
                };
            }

            return dimension;
        }

        private static List<PortalViewModel> ParsePortals(string text)
        {
            var portals = new List<PortalViewModel>();
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (!Regex.IsMatch(line, "门|通道|裂隙|入口|传送"))
                {
                    continue;
                }

                portals.Add(new PortalViewModel
                {
                    Name = TrimListMarker(line),
                    TargetDimension = InferTargetDimension(line),
                    Status = InferPortalStatus(line)
                });

                if (portals.Count >= 8)
                {
                    break;
                }
            }

            return portals;
        }

        private static string InferTargetDimension(string line)
        {
            var match = Regex.Match(line, @"通往(.+)$|连接(.+)$|前往(.+)$");
            if (match.Success)
            {
                return match.Groups.Cast<Group>().Skip(1).FirstOrDefault(group => !string.IsNullOrWhiteSpace(group.Value))?.Value.Trim() ?? "未知维度";
            }

            return "未知维度";
        }

        private static string InferPortalStatus(string line)
        {
            if (line.Contains("关闭") || line.Contains("封闭"))
            {
                return "关闭";
            }

            if (line.Contains("损坏") || line.Contains("破碎"))
            {
                return "损坏";
            }

            if (line.Contains("不稳定") || line.Contains("紊乱"))
            {
                return "不稳定";
            }

            return "正常";
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
    }

    /// <summary>
    /// 维度视图模型
    /// </summary>
    public class DimensionViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Stability { get; set; } = string.Empty;
        public string AccessLevel { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string EnvironmentType { get; set; } = string.Empty;
        public string Climate { get; set; } = string.Empty;
        public string EnergyLevel { get; set; } = string.Empty;
        public string DangerLevel { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<PortalViewModel> Portals { get; set; } = new();
    }

    /// <summary>
    /// 传送门视图模型
    /// </summary>
    public class PortalViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string TargetDimension { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
