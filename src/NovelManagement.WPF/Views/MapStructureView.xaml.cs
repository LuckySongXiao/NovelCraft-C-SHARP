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
    /// 地图结构管理视图
    /// </summary>
    public partial class MapStructureView : UserControl
    {
        #region 属性

        /// <summary>
        /// 地图列表
        /// </summary>
        public ObservableCollection<MapViewModel> Maps { get; set; }

        /// <summary>
        /// 当前选中的地图
        /// </summary>
        public MapViewModel SelectedMap { get; set; }

        /// <summary>
        /// 资源列表
        /// </summary>
        public ObservableCollection<ResourceViewModel> Resources { get; set; }

        /// <summary>
        /// 选择地图命令
        /// </summary>
        public ICommand SelectMapCommand { get; set; }

        /// <summary>
        /// 日志记录器
        /// </summary>
        private readonly ILogger<MapStructureView>? _logger;

        /// <summary>
        /// AI助手服务
        /// </summary>
        private readonly IAIAssistantService? _aiAssistantService;

        #endregion

        #region 构造函数

        public MapStructureView()
        {
            InitializeComponent();

            // 获取服务
            try
            {
                var serviceProvider = App.ServiceProvider;
                _logger = serviceProvider?.GetService<ILogger<MapStructureView>>();
                _aiAssistantService = serviceProvider?.GetService<IAIAssistantService>();
            }
            catch (Exception ex)
            {
                // 如果获取服务失败，记录错误但不影响界面初始化
                System.Diagnostics.Debug.WriteLine($"获取服务失败: {ex.Message}");
            }

            InitializeData();
            InitializeCommands();
            LoadMaps();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化数据
        /// </summary>
        private void InitializeData()
        {
            Maps = new ObservableCollection<MapViewModel>();
            Resources = new ObservableCollection<ResourceViewModel>();
            
            // 设置数据上下文
            this.DataContext = this;
        }

        /// <summary>
        /// 初始化命令
        /// </summary>
        private void InitializeCommands()
        {
            SelectMapCommand = new RelayCommand<MapViewModel>(SelectMap);
        }

        #endregion

        #region 数据加载

        /// <summary>
        /// 加载地图数据
        /// </summary>
        private void LoadMaps()
        {
            try
            {
                // 模拟数据 - 实际应用中应该从服务层获取
                var maps = new List<MapViewModel>
                {
                    new MapViewModel
                    {
                        Id = 1,
                        Name = "九州大陆",
                        Level = "世界地图",
                        TerrainType = "混合地形",
                        ClimateType = "多样化气候",
                        Description = "修仙世界的主大陆，包含九个州域，地形复杂多样",
                        DimensionId = "修仙界",
                        ParentMap = "",
                        AreaSize = "百万平方公里",
                        Coordinates = "0,0",
                        Elevation = "0-8000米",
                        DangerLevel = "中危",
                        CreatedAt = DateTime.Now.AddDays(-365)
                    },
                    new MapViewModel
                    {
                        Id = 2,
                        Name = "东域",
                        Level = "区域地图",
                        TerrainType = "山脉",
                        ClimateType = "温带",
                        Description = "九州大陆东部区域，多山脉，灵气浓郁，宗门林立",
                        DimensionId = "修仙界",
                        ParentMap = "九州大陆",
                        AreaSize = "十万平方公里",
                        Coordinates = "1000,0",
                        Elevation = "500-5000米",
                        DangerLevel = "中危",
                        CreatedAt = DateTime.Now.AddDays(-300)
                    },
                    new MapViewModel
                    {
                        Id = 3,
                        Name = "青云山",
                        Level = "城市地图",
                        TerrainType = "山脉",
                        ClimateType = "高原",
                        Description = "青云宗所在的灵山，山势险峻，灵气充沛",
                        DimensionId = "修仙界",
                        ParentMap = "东域",
                        AreaSize = "一千平方公里",
                        Coordinates = "1200,200",
                        Elevation = "2000-4000米",
                        DangerLevel = "低危",
                        CreatedAt = DateTime.Now.AddDays(-250)
                    },
                    new MapViewModel
                    {
                        Id = 4,
                        Name = "西域沙漠",
                        Level = "区域地图",
                        TerrainType = "沙漠",
                        ClimateType = "干旱",
                        Description = "九州大陆西部的广袤沙漠，环境恶劣，隐藏古老秘密",
                        DimensionId = "修仙界",
                        ParentMap = "九州大陆",
                        AreaSize = "八万平方公里",
                        Coordinates = "-1000,0",
                        Elevation = "200-1000米",
                        DangerLevel = "高危",
                        CreatedAt = DateTime.Now.AddDays(-200)
                    },
                    new MapViewModel
                    {
                        Id = 5,
                        Name = "北海",
                        Level = "区域地图",
                        TerrainType = "海洋",
                        ClimateType = "海洋性",
                        Description = "九州大陆北部的无边海域，海中有无数岛屿和海族",
                        DimensionId = "修仙界",
                        ParentMap = "九州大陆",
                        AreaSize = "无边海域",
                        Coordinates = "0,1000",
                        Elevation = "0-3000米深",
                        DangerLevel = "极危",
                        CreatedAt = DateTime.Now.AddDays(-150)
                    }
                };

                Maps.Clear();
                foreach (var map in maps)
                {
                    Maps.Add(map);
                }

                // 设置列表数据源
                MapListControl.ItemsSource = Maps;

                // 更新统计信息
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载地图数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
            FilterMaps();
        }

        /// <summary>
        /// 地图层级筛选变化
        /// </summary>
        private void MapLevelFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterMaps();
        }

        /// <summary>
        /// 地形类型筛选变化
        /// </summary>
        private void TerrainFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterMaps();
        }

        /// <summary>
        /// 筛选地图
        /// </summary>
        private void FilterMaps()
        {
            var searchText = SearchTextBox?.Text?.ToLower() ?? "";
            var selectedLevel = (MapLevelFilterComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString();
            var selectedTerrain = (TerrainFilterComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString();

            var filteredMaps = Maps.Where(m =>
                (string.IsNullOrEmpty(searchText) || 
                 m.Name.ToLower().Contains(searchText) || 
                 m.Description.ToLower().Contains(searchText) ||
                 m.TerrainType.ToLower().Contains(searchText)) &&
                (selectedLevel == "全部层级" || selectedLevel == null || m.Level == selectedLevel) &&
                (selectedTerrain == "全部地形" || selectedTerrain == null || m.TerrainType == selectedTerrain)
            ).ToList();

            MapListControl.ItemsSource = filteredMaps;
        }

        /// <summary>
        /// 选择地图
        /// </summary>
        private void SelectMap(MapViewModel map)
        {
            if (map == null) return;

            SelectedMap = map;
            LoadMapDetails(map);
            ShowEditPanel();
        }

        /// <summary>
        /// 加载地图详情
        /// </summary>
        private void LoadMapDetails(MapViewModel map)
        {
            // 填充基本信息
            MapNameTextBox.Text = map.Name;
            MapDescriptionTextBox.Text = map.Description;
            DimensionIdTextBox.Text = map.DimensionId;
            ParentMapTextBox.Text = map.ParentMap;
            AreaSizeTextBox.Text = map.AreaSize;
            CoordinatesTextBox.Text = map.Coordinates;
            ElevationTextBox.Text = map.Elevation;
            
            // 设置层级选择
            foreach (ComboBoxItem item in MapLevelComboBox.Items)
            {
                if (item.Content.ToString() == map.Level)
                {
                    MapLevelComboBox.SelectedItem = item;
                    break;
                }
            }

            // 设置地形类型选择
            foreach (ComboBoxItem item in TerrainTypeComboBox.Items)
            {
                if (item.Content.ToString() == map.TerrainType)
                {
                    TerrainTypeComboBox.SelectedItem = item;
                    break;
                }
            }

            // 设置气候类型选择
            foreach (ComboBoxItem item in ClimateTypeComboBox.Items)
            {
                if (item.Content.ToString() == map.ClimateType)
                {
                    ClimateTypeComboBox.SelectedItem = item;
                    break;
                }
            }

            // 设置危险等级选择
            foreach (ComboBoxItem item in DangerLevelComboBox.Items)
            {
                if (item.Content.ToString() == map.DangerLevel)
                {
                    DangerLevelComboBox.SelectedItem = item;
                    break;
                }
            }

            // 加载资源列表
            LoadResources(map.Id);
        }

        /// <summary>
        /// 加载资源列表
        /// </summary>
        private void LoadResources(int mapId)
        {
            // 模拟数据 - 实际应用中应该从服务层获取
            var resources = new List<ResourceViewModel>();
            
            if (mapId == 1) // 九州大陆
            {
                resources.AddRange(new[]
                {
                    new ResourceViewModel { Name = "灵石矿", Type = "矿物", Abundance = "丰富" },
                    new ResourceViewModel { Name = "千年灵草", Type = "植物", Abundance = "普通" },
                    new ResourceViewModel { Name = "灵兽", Type = "动物", Abundance = "少量" },
                    new ResourceViewModel { Name = "灵泉", Type = "水源", Abundance = "普通" }
                });
            }
            else if (mapId == 2) // 东域
            {
                resources.AddRange(new[]
                {
                    new ResourceViewModel { Name = "青云石", Type = "矿物", Abundance = "丰富" },
                    new ResourceViewModel { Name = "雷击木", Type = "植物", Abundance = "少量" },
                    new ResourceViewModel { Name = "风灵珠", Type = "特殊", Abundance = "稀有" }
                });
            }
            else if (mapId == 3) // 青云山
            {
                resources.AddRange(new[]
                {
                    new ResourceViewModel { Name = "青云草", Type = "植物", Abundance = "丰富" },
                    new ResourceViewModel { Name = "山泉水", Type = "水源", Abundance = "丰富" },
                    new ResourceViewModel { Name = "灵气", Type = "能量", Abundance = "极丰" }
                });
            }

            Resources.Clear();
            foreach (var resource in resources)
            {
                Resources.Add(resource);
            }

            ResourceListControl.ItemsSource = Resources;
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
            SelectedMap = null;
        }

        #endregion

        #region 按钮事件

        /// <summary>
        /// 新建地图
        /// </summary>
        private void AddMap_Click(object sender, RoutedEventArgs e)
        {
            var newMap = new MapViewModel
            {
                Id = 0,
                Name = "",
                Level = "区域地图",
                TerrainType = "平原",
                ClimateType = "温带",
                Description = "",
                DimensionId = "",
                ParentMap = "",
                AreaSize = "",
                Coordinates = "",
                Elevation = "",
                DangerLevel = "安全",
                CreatedAt = DateTime.Now
            };

            SelectedMap = newMap;
            LoadMapDetails(newMap);
            ShowEditPanel();

            // 清空资源列表
            Resources.Clear();
            ResourceListControl.ItemsSource = Resources;

            // 聚焦到名称输入框
            MapNameTextBox.Focus();
        }

        /// <summary>
        /// 导入地图数据
        /// </summary>
        private void ImportMap_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("导入功能开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 导出地图数据
        /// </summary>
        private void ExportMap_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("导出功能开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 添加资源
        /// </summary>
        private void AddResource_Click(object sender, RoutedEventArgs e)
        {
            var newResource = new ResourceViewModel
            {
                Name = "",
                Type = "矿物",
                Abundance = "普通"
            };

            Resources.Add(newResource);
        }

        /// <summary>
        /// 删除资源
        /// </summary>
        private void RemoveResource_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is ResourceViewModel resource)
            {
                Resources.Remove(resource);
            }
        }

        /// <summary>
        /// 保存地图
        /// </summary>
        private void SaveMap_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SelectedMap == null) return;

                // 验证输入
                if (string.IsNullOrWhiteSpace(MapNameTextBox.Text))
                {
                    MessageBox.Show("请输入地图名称", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 更新地图信息
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

                // 如果是新建地图，添加到列表
                if (SelectedMap.Id == 0)
                {
                    SelectedMap.Id = Maps.Count > 0 ? Maps.Max(m => m.Id) + 1 : 1;
                    Maps.Add(SelectedMap);
                }

                // 这里应该调用服务层保存数据
                MessageBox.Show("地图保存成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);

                // 刷新列表
                FilterMaps();
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
            var dialog = new AIAssistantDialog("地图结构管理", contextString);
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
                    DimensionId = SelectedMap.DimensionId,
                    ParentMap = SelectedMap.ParentMap,
                    AreaSize = SelectedMap.AreaSize,
                    Coordinates = SelectedMap.Coordinates,
                    Elevation = SelectedMap.Elevation,
                    DangerLevel = SelectedMap.DangerLevel
                };
                context["currentMapResources"] = Resources.ToList();
            }

            // 添加统计信息
            var levelStats = Maps.GroupBy(m => m.Level)
                .ToDictionary(g => g.Key, g => g.Count());
            context["levelStatistics"] = levelStats;

            var terrainStats = Maps.GroupBy(m => m.TerrainType)
                .ToDictionary(g => g.Key, g => g.Count());
            context["terrainStatistics"] = terrainStats;

            return context;
        }

        #endregion
    }

    #region 视图模型

    /// <summary>
    /// 地图视图模型
    /// </summary>
    public class MapViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Level { get; set; }
        public string TerrainType { get; set; }
        public string ClimateType { get; set; }
        public string Description { get; set; }
        public string DimensionId { get; set; }
        public string ParentMap { get; set; }
        public string AreaSize { get; set; }
        public string Coordinates { get; set; }
        public string Elevation { get; set; }
        public string DangerLevel { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// 资源视图模型
    /// </summary>
    public class ResourceViewModel
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Abundance { get; set; }
    }

    #endregion
}
