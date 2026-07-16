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
    /// 地图结构管理视图
    /// </summary>
    public partial class MapStructureView : UserControl, INavigationRefreshableView, INavigationAwareView
    {
        private readonly ILogger<MapStructureView>? _logger;
        private readonly IAIAssistantService? _aiAssistantService;
        private readonly MapDataService? _mapDataService;
        private readonly ProjectContextService? _projectContextService;
        private readonly CurrentProjectGuard? _currentProjectGuard;
        private Guid _currentProjectId;

        /// <summary>
        /// 地图列表。
        /// </summary>
        public ObservableCollection<MapViewModel> Maps { get; } = new();

        /// <summary>
        /// 当前选中的地图。
        /// </summary>
        public MapViewModel? SelectedMap { get; set; }

        /// <summary>
        /// 当前地图的资源列表。
        /// </summary>
        public ObservableCollection<ResourceViewModel> Resources { get; } = new();

        /// <summary>
        /// 选择地图命令。
        /// </summary>
        public ICommand SelectMapCommand { get; }

        /// <summary>
        /// 获取当前地图总数。
        /// </summary>
        public int TotalCount => Maps.Count;

        /// <summary>
        /// 获取世界地图数量。
        /// </summary>
        public int WorldMapCount => Maps.Count(map => map.Level == "世界地图");

        /// <summary>
        /// 获取区域地图数量。
        /// </summary>
        public int RegionMapCount => Maps.Count(map => map.Level == "区域地图");

        /// <summary>
        /// 获取城市地图数量。
        /// </summary>
        public int CityMapCount => Maps.Count(map => map.Level == "城市地图");

        /// <summary>
        /// 初始化地图结构管理视图。
        /// </summary>
        public MapStructureView()
        {
            InitializeComponent();

            try
            {
                var serviceProvider = App.ServiceProvider;
                _logger = serviceProvider?.GetService<ILogger<MapStructureView>>();
                _aiAssistantService = serviceProvider?.GetService<IAIAssistantService>();
                _mapDataService = serviceProvider?.GetService<MapDataService>();
                _projectContextService = serviceProvider?.GetService<ProjectContextService>();
                _currentProjectGuard = serviceProvider?.GetService<CurrentProjectGuard>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取服务失败: {ex.Message}");
            }

            SelectMapCommand = new RelayCommand<MapViewModel>(SelectMap);
            DataContext = this;
            ResourceListControl.ItemsSource = Resources;
            _ = LoadMapsAsync();
        }

        private async Task LoadMapsAsync()
        {
            try
            {
                _currentProjectId = _projectContextService?.CurrentProjectId ?? Guid.Empty;
                if (_currentProjectId == Guid.Empty)
                {
                    _currentProjectGuard?.TryGetCurrentProjectId(Window.GetWindow(this), "地图结构管理", out _);
                    Maps.Clear();
                    Resources.Clear();
                    MapListControl.ItemsSource = Maps;
                    UpdateStatistics();
                    HideEditPanel();
                    return;
                }

                var maps = _mapDataService == null
                    ? new List<MapViewModel>()
                    : await _mapDataService.LoadMapsAsync(_currentProjectId);

                Maps.Clear();
                foreach (var map in maps.OrderBy(map => map.Name))
                {
                    map.Resources ??= new List<ResourceViewModel>();
                    Maps.Add(map);
                }

                MapListControl.ItemsSource = Maps;
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载地图数据失败");
                MessageBox.Show($"加载地图数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStatistics()
        {
            MapListControl.Items.Refresh();
            DataContext = null;
            DataContext = this;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterMaps();
        }

        private void MapLevelFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterMaps();
        }

        private void TerrainFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterMaps();
        }

        private void FilterMaps()
        {
            var searchText = SearchTextBox?.Text?.ToLowerInvariant() ?? string.Empty;
            var selectedLevel = (MapLevelFilterComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString();
            var selectedTerrain = (TerrainFilterComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString();

            var filteredMaps = Maps.Where(map =>
                (string.IsNullOrWhiteSpace(searchText) ||
                 map.Name.ToLowerInvariant().Contains(searchText) ||
                 map.Description.ToLowerInvariant().Contains(searchText) ||
                 map.TerrainType.ToLowerInvariant().Contains(searchText)) &&
                (selectedLevel == "全部层级" || selectedLevel == null || map.Level == selectedLevel) &&
                (selectedTerrain == "全部地形" || selectedTerrain == null || map.TerrainType == selectedTerrain))
                .ToList();

            MapListControl.ItemsSource = filteredMaps;
        }

        private void SelectMap(MapViewModel? map)
        {
            if (map == null)
            {
                return;
            }

            SelectedMap = map;
            LoadMapDetails(map);
            ShowEditPanel();
        }

        private void LoadMapDetails(MapViewModel map)
        {
            MapNameTextBox.Text = map.Name;
            MapDescriptionTextBox.Text = map.Description;
            DimensionIdTextBox.Text = map.DimensionId;
            ParentMapTextBox.Text = map.ParentMap;
            AreaSizeTextBox.Text = map.AreaSize;
            CoordinatesTextBox.Text = map.Coordinates;
            ElevationTextBox.Text = map.Elevation;

            SelectComboBoxItem(MapLevelComboBox, map.Level);
            SelectComboBoxItem(TerrainTypeComboBox, map.TerrainType);
            SelectComboBoxItem(ClimateTypeComboBox, map.ClimateType);
            SelectComboBoxItem(DangerLevelComboBox, map.DangerLevel);

            LoadResources(map);
        }

        private void LoadResources(MapViewModel map)
        {
            Resources.Clear();
            foreach (var resource in map.Resources ?? Enumerable.Empty<ResourceViewModel>())
            {
                Resources.Add(new ResourceViewModel
                {
                    Name = resource.Name,
                    Type = resource.Type,
                    Abundance = resource.Abundance
                });
            }

            ResourceListControl.ItemsSource = Resources;
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
            SelectedMap = null;
            Resources.Clear();
        }

        private void AddMap_Click(object sender, RoutedEventArgs e)
        {
            var newMap = new MapViewModel
            {
                Level = "区域地图",
                TerrainType = "平原",
                ClimateType = "温带",
                DangerLevel = "安全",
                CreatedAt = DateTime.Now,
                Resources = new List<ResourceViewModel>()
            };

            SelectedMap = newMap;
            LoadMapDetails(newMap);
            ShowEditPanel();
            MapNameTextBox.Focus();
        }

        private async void ImportMap_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!EnsureCurrentProject("导入地图结构"))
                {
                    return;
                }

                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "导入地图结构数据",
                    Filter = "JSON文件|*.json|所有文件|*.*",
                    DefaultExt = "json"
                };

                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                if (_mapDataService == null)
                {
                    MessageBox.Show("地图数据服务未初始化。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var importedMaps = await _mapDataService.ImportMapsAsync(dialog.FileName);
                Maps.Clear();
                foreach (var map in importedMaps)
                {
                    map.Resources ??= new List<ResourceViewModel>();
                    Maps.Add(map);
                }

                await PersistMapsAsync();
                FilterMaps();
                UpdateStatistics();
                MessageBox.Show($"已成功导入 {Maps.Count} 个地图。", "导入成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "导入地图数据失败");
                MessageBox.Show($"导入失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExportMap_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!EnsureCurrentProject("导出地图结构"))
                {
                    return;
                }

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "导出地图结构数据",
                    Filter = "JSON文件|*.json",
                    DefaultExt = "json",
                    FileName = $"地图结构数据_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                if (_mapDataService == null)
                {
                    MessageBox.Show("地图数据服务未初始化。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                await _mapDataService.ExportMapsAsync(_currentProjectId, Maps, dialog.FileName);
                MessageBox.Show($"地图结构数据已导出到：{dialog.FileName}", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "导出地图数据失败");
                MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddResource_Click(object sender, RoutedEventArgs e)
        {
            Resources.Add(new ResourceViewModel
            {
                Type = "矿物",
                Abundance = "普通"
            });
        }

        private void RemoveResource_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { CommandParameter: ResourceViewModel resource })
            {
                Resources.Remove(resource);
            }
        }

        private async void SaveMap_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SelectedMap == null)
                {
                    return;
                }

                if (!EnsureCurrentProject("保存地图结构"))
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(MapNameTextBox.Text))
                {
                    MessageBox.Show("请输入地图名称", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                SelectedMap.Name = MapNameTextBox.Text.Trim();
                SelectedMap.Description = MapDescriptionTextBox.Text.Trim();
                SelectedMap.Level = (MapLevelComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "区域地图";
                SelectedMap.TerrainType = (TerrainTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "平原";
                SelectedMap.ClimateType = (ClimateTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "温带";
                SelectedMap.DimensionId = DimensionIdTextBox.Text.Trim();
                SelectedMap.ParentMap = ParentMapTextBox.Text.Trim();
                SelectedMap.AreaSize = AreaSizeTextBox.Text.Trim();
                SelectedMap.Coordinates = CoordinatesTextBox.Text.Trim();
                SelectedMap.Elevation = ElevationTextBox.Text.Trim();
                SelectedMap.DangerLevel = (DangerLevelComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "安全";
                SelectedMap.Resources = Resources
                    .Where(resource => !string.IsNullOrWhiteSpace(resource.Name))
                    .Select(resource => new ResourceViewModel
                    {
                        Name = resource.Name.Trim(),
                        Type = string.IsNullOrWhiteSpace(resource.Type) ? "矿物" : resource.Type.Trim(),
                        Abundance = string.IsNullOrWhiteSpace(resource.Abundance) ? "普通" : resource.Abundance.Trim()
                    })
                    .ToList();

                if (SelectedMap.Id == 0)
                {
                    SelectedMap.Id = Maps.Count > 0 ? Maps.Max(map => map.Id) + 1 : 1;
                    Maps.Add(SelectedMap);
                }

                await PersistMapsAsync();
                MessageBox.Show("地图保存成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                FilterMaps();
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存地图失败");
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
                if (!EnsureCurrentProject("AI地图结构"))
                {
                    return;
                }

                if (_aiAssistantService == null)
                {
                    ShowAIAssistantDialog();
                    return;
                }

                if (SelectedMap != null)
                {
                    var choice = MessageBox.Show(
                        "是：AI优化当前地图并保存\n否：AI生成新的地图并保存\n取消：打开原始AI助手",
                        "AI地图结构",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);

                    if (choice == MessageBoxResult.Cancel)
                    {
                        ShowAIAssistantDialog();
                        return;
                    }

                    await GenerateMapWithAiAsync(choice == MessageBoxResult.Yes);
                    return;
                }

                var generateChoice = MessageBox.Show(
                    "是：AI生成新的地图并保存\n否：打开原始AI助手",
                    "AI地图结构",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (generateChoice == MessageBoxResult.Yes)
                {
                    await GenerateMapWithAiAsync(false);
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
            var dialog = new AIAssistantDialog("地图结构管理", contextString)
            {
                Owner = Window.GetWindow(this)
            };
            dialog.ShowDialog();
        }

        private Dictionary<string, object> GetCurrentContext()
        {
            var context = new Dictionary<string, object>
            {
                ["interfaceType"] = "地图结构管理",
                ["totalMaps"] = Maps.Count,
                ["selectedMap"] = SelectedMap,
                ["mapLevels"] = new[] { "世界地图", "大陆地图", "区域地图", "城市地图", "建筑地图" },
                ["terrainTypes"] = new[] { "平原", "山脉", "森林", "沙漠", "海洋", "湖泊", "冰原", "火山", "混合地形" },
                ["climateTypes"] = new[] { "温带", "热带", "寒带", "亚热带", "干旱", "湿润", "高原", "海洋性", "大陆性" },
                ["dangerLevels"] = new[] { "安全", "低危", "中危", "高危", "极危", "禁区" }
            };

            if (SelectedMap != null)
            {
                context["currentMapName"] = SelectedMap.Name;
                context["currentMapLevel"] = SelectedMap.Level;
                context["currentMapTerrain"] = SelectedMap.TerrainType;
                context["currentMapClimate"] = SelectedMap.ClimateType;
                context["currentMapDescription"] = SelectedMap.Description;
                context["currentMapGeography"] = new
                {
                    SelectedMap.DimensionId,
                    SelectedMap.ParentMap,
                    SelectedMap.AreaSize,
                    SelectedMap.Coordinates,
                    SelectedMap.Elevation,
                    SelectedMap.DangerLevel
                };
                context["currentMapResources"] = Resources.ToList();
            }

            context["levelStatistics"] = Maps.GroupBy(map => map.Level).ToDictionary(group => group.Key, group => group.Count());
            context["terrainStatistics"] = Maps.GroupBy(map => map.TerrainType).ToDictionary(group => group.Key, group => group.Count());
            return context;
        }

        public async Task RefreshOnProjectChangedAsync(Guid? projectId, string? projectName)
        {
            _currentProjectId = projectId ?? Guid.Empty;
            await LoadMapsAsync();
        }

        public void OnNavigatedTo(NavigationContext context)
        {
            _currentProjectId = context.ProjectId ?? Guid.Empty;
            _ = LoadMapsAsync();
        }

        private async Task PersistMapsAsync()
        {
            if (_currentProjectId == Guid.Empty || _mapDataService == null)
            {
                return;
            }

            foreach (var map in Maps)
            {
                map.Resources ??= new List<ResourceViewModel>();
            }

            await _mapDataService.SaveMapsAsync(_currentProjectId, Maps);
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

        private async Task GenerateMapWithAiAsync(bool optimizeCurrent)
        {
            if (_aiAssistantService == null)
            {
                MessageBox.Show("AI助手服务未初始化。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var parameters = new Dictionary<string, object>
            {
                ["title"] = optimizeCurrent && SelectedMap != null ? $"优化地图：{SelectedMap.Name}" : "生成地图结构",
                ["theme"] = "请生成一个适合小说项目使用的地图设定，并输出名称、层级、地形、气候、维度、父级地图、描述、面积、坐标、海拔、危险等级、资源分布。",
                ["requirements"] = optimizeCurrent && SelectedMap != null
                    ? $"请基于当前地图进行优化并输出结构化文本。当前地图：{SelectedMap.Name}，层级：{SelectedMap.Level}，地形：{SelectedMap.TerrainType}，气候：{SelectedMap.ClimateType}，描述：{SelectedMap.Description}"
                    : "请输出一个完整地图设定，至少包含名称、层级、地形、气候、维度、描述、面积、坐标、海拔、危险等级，并列出至少3种资源。",
                ["context"] = GetCurrentContext()
            };

            var result = await _aiAssistantService.GenerateOutlineAsync(parameters);
            if (!result.IsSuccess || result.Data == null)
            {
                MessageBox.Show(result.Message ?? "AI生成失败。", "AI地图结构", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var generatedMap = ParseMapFromAiResult(result.Data, optimizeCurrent ? SelectedMap : null);
            if (generatedMap == null)
            {
                MessageBox.Show("AI结果无法解析为地图。", "AI地图结构", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (optimizeCurrent && SelectedMap != null)
            {
                generatedMap.Id = SelectedMap.Id;
                generatedMap.CreatedAt = SelectedMap.CreatedAt;
                var index = Maps.IndexOf(SelectedMap);
                if (index >= 0)
                {
                    Maps[index] = generatedMap;
                }
                SelectedMap = generatedMap;
            }
            else
            {
                generatedMap.Id = Maps.Count > 0 ? Maps.Max(map => map.Id) + 1 : 1;
                generatedMap.CreatedAt = DateTime.Now;
                Maps.Add(generatedMap);
                SelectedMap = generatedMap;
            }

            await PersistMapsAsync();
            FilterMaps();
            UpdateStatistics();
            LoadMapDetails(generatedMap);
            ShowEditPanel();
            MessageBox.Show(
                optimizeCurrent ? $"已使用 AI 优化并保存地图：{generatedMap.Name}" : $"已使用 AI 生成并保存地图：{generatedMap.Name}",
                "AI地图结构",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private MapViewModel? ParseMapFromAiResult(object data, MapViewModel? baseMap)
        {
            var text = data?.ToString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var map = new MapViewModel
            {
                Id = baseMap?.Id ?? 0,
                Name = ExtractField(text, "名称") ?? baseMap?.Name ?? ExtractFirstMeaningfulLine(text) ?? "AI生成地图",
                Level = ExtractField(text, "层级") ?? baseMap?.Level ?? "区域地图",
                TerrainType = ExtractField(text, "地形") ?? baseMap?.TerrainType ?? "平原",
                ClimateType = ExtractField(text, "气候") ?? baseMap?.ClimateType ?? "温带",
                Description = ExtractField(text, "描述") ?? baseMap?.Description ?? text.Trim(),
                DimensionId = ExtractField(text, "维度") ?? baseMap?.DimensionId ?? "地图维度",
                ParentMap = ExtractField(text, "父级地图") ?? baseMap?.ParentMap ?? string.Empty,
                AreaSize = ExtractField(text, "面积") ?? baseMap?.AreaSize ?? "待定",
                Coordinates = ExtractField(text, "坐标") ?? baseMap?.Coordinates ?? "待定",
                Elevation = ExtractField(text, "海拔") ?? baseMap?.Elevation ?? "待定",
                DangerLevel = ExtractField(text, "危险等级") ?? baseMap?.DangerLevel ?? "中危",
                CreatedAt = baseMap?.CreatedAt ?? DateTime.Now,
                Resources = ParseResources(text)
            };

            if (map.Resources.Count == 0)
            {
                map.Resources = baseMap?.Resources?.ToList() ?? new List<ResourceViewModel>
                {
                    new() { Name = "灵石矿脉", Type = "矿物", Abundance = "丰富" },
                    new() { Name = "灵植群落", Type = "植物", Abundance = "普通" },
                    new() { Name = "灵泉", Type = "水源", Abundance = "少量" }
                };
            }

            return map;
        }

        private static List<ResourceViewModel> ParseResources(string text)
        {
            var resources = new List<ResourceViewModel>();
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (!Regex.IsMatch(line, "矿|草|木|泉|石|兽|资源|材料|晶|药"))
                {
                    continue;
                }

                resources.Add(new ResourceViewModel
                {
                    Name = TrimListMarker(line),
                    Type = InferResourceType(line),
                    Abundance = InferAbundance(line)
                });

                if (resources.Count >= 8)
                {
                    break;
                }
            }

            return resources;
        }

        private static string InferResourceType(string line)
        {
            if (Regex.IsMatch(line, "草|木|花|果|药"))
            {
                return "植物";
            }

            if (Regex.IsMatch(line, "兽|鱼|鸟|虫"))
            {
                return "动物";
            }

            if (Regex.IsMatch(line, "泉|河|湖|水"))
            {
                return "水源";
            }

            if (Regex.IsMatch(line, "气|脉|能|火种"))
            {
                return "能量";
            }

            if (Regex.IsMatch(line, "矿|石|晶|金属"))
            {
                return "矿物";
            }

            return "特殊";
        }

        private static string InferAbundance(string line)
        {
            if (line.Contains("极丰") || line.Contains("充沛"))
            {
                return "极丰";
            }

            if (line.Contains("丰富"))
            {
                return "丰富";
            }

            if (line.Contains("稀有"))
            {
                return "稀有";
            }

            if (line.Contains("少量"))
            {
                return "少量";
            }

            return "普通";
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
    /// 地图视图模型。
    /// </summary>
    public class MapViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public string TerrainType { get; set; } = string.Empty;
        public string ClimateType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DimensionId { get; set; } = string.Empty;
        public string ParentMap { get; set; } = string.Empty;
        public string AreaSize { get; set; } = string.Empty;
        public string Coordinates { get; set; } = string.Empty;
        public string Elevation { get; set; } = string.Empty;
        public string DangerLevel { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<ResourceViewModel> Resources { get; set; } = new();
    }

    /// <summary>
    /// 资源视图模型。
    /// </summary>
    public class ResourceViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Abundance { get; set; } = string.Empty;
    }
}
