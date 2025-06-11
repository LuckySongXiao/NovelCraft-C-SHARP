using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NovelManagement.WPF.Commands;
using MaterialDesignThemes.Wpf;
using NovelManagement.Application.Services;
using NovelManagement.Core.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// FactionManagementView.xaml 的交互逻辑
    /// </summary>
    public partial class FactionManagementView : UserControl
    {
        /// <summary>
        /// 势力数据模型
        /// </summary>
        public class FactionViewModel
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public string PowerLevel { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Territory { get; set; } = string.Empty;
            public int MemberCount { get; set; }
            public string Leader { get; set; } = string.Empty;
            public string Ideology { get; set; } = string.Empty;
            public string Resources { get; set; } = string.Empty;
            public PackIconKind IconKind { get; set; }
            public Brush IconColor { get; set; } = Brushes.Gray;
            public Brush PowerLevelColor { get; set; } = Brushes.Gray;
            public List<string> Allies { get; set; } = new();
            public List<string> Enemies { get; set; } = new();
        }

        private readonly FactionService _factionService;
        private readonly ILogger<FactionManagementView> _logger;
        private Guid _currentProjectId;

        private List<FactionViewModel> _allFactions = new();
        private List<FactionViewModel> _filteredFactions = new();

        /// <summary>
        /// 选择势力命令
        /// </summary>
        public ICommand SelectFactionCommand { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public FactionManagementView()
        {
            // 从依赖注入容器获取服务
            var serviceProvider = App.ServiceProvider;
            _factionService = serviceProvider.GetRequiredService<FactionService>();
            _logger = serviceProvider.GetRequiredService<ILogger<FactionManagementView>>();

            // 获取当前项目ID（暂时使用固定值，后续可以从上下文获取）
            _currentProjectId = GetCurrentProjectId();

            InitializeComponent();
            InitializeCommands();
            _ = LoadFactionsAsync(); // 异步加载势力数据
        }

        /// <summary>
        /// 获取当前项目ID
        /// </summary>
        private Guid GetCurrentProjectId()
        {
            // TODO: 从应用程序上下文或用户选择中获取当前项目ID
            // 暂时返回一个固定的项目ID
            return Guid.Parse("12345678-1234-1234-1234-123456789012");
        }

        #region 初始化

        /// <summary>
        /// 初始化命令
        /// </summary>
        private void InitializeCommands()
        {
            SelectFactionCommand = new RelayCommand<FactionViewModel>(SelectFaction);
        }

        /// <summary>
        /// 异步加载势力数据
        /// </summary>
        private async Task LoadFactionsAsync()
        {
            try
            {
                _logger?.LogInformation("开始加载势力数据，项目ID: {ProjectId}", _currentProjectId);

                // 从数据库加载势力数据
                var factions = await _factionService.GetFactionsByProjectIdAsync(_currentProjectId);

                // 转换为ViewModel
                _allFactions = factions.Select(ConvertToViewModel).ToList();

                // 如果数据库中没有势力，创建一些示例数据
                if (!_allFactions.Any())
                {
                    await CreateSampleFactionsAsync();
                    // 重新加载数据
                    factions = await _factionService.GetFactionsByProjectIdAsync(_currentProjectId);
                    _allFactions = factions.Select(ConvertToViewModel).ToList();
                }

                _filteredFactions = new List<FactionViewModel>(_allFactions);
                UpdateFactionList();

                _logger?.LogInformation("成功加载 {Count} 个势力", _allFactions.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载势力数据失败");
                MessageBox.Show($"加载势力数据失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                // 加载失败时使用空列表
                _allFactions = new List<FactionViewModel>();
                _filteredFactions = new List<FactionViewModel>();
                UpdateFactionList();
            }
        }

        /// <summary>
        /// 将Faction实体转换为FactionViewModel
        /// </summary>
        private FactionViewModel ConvertToViewModel(Faction faction)
        {
            return new FactionViewModel
            {
                Id = (int)faction.Id.GetHashCode(), // 临时转换，实际应该使用Guid
                Name = faction.Name,
                Type = faction.Type,
                PowerLevel = GetPowerLevelText(faction.PowerLevel),
                Description = faction.Description ?? string.Empty,
                Territory = faction.Territory ?? string.Empty,
                MemberCount = faction.MemberCount ?? 0,
                Leader = "未设置", // TODO: 根据LeaderId查找角色名称
                Ideology = "未设置", // TODO: 从Tags或其他字段解析
                Resources = faction.Resources ?? string.Empty,
                IconKind = GetIconKind(faction.Type),
                IconColor = GetIconColor(faction.Type),
                PowerLevelColor = GetPowerLevelColor(faction.PowerLevel),
                Allies = new List<string>(), // TODO: 从关系数据中获取
                Enemies = new List<string>() // TODO: 从关系数据中获取
            };
        }

        /// <summary>
        /// 获取实力等级文本
        /// </summary>
        private string GetPowerLevelText(int powerLevel)
        {
            return powerLevel switch
            {
                >= 90 => "超级",
                >= 70 => "一流",
                >= 50 => "二流",
                >= 30 => "三流",
                _ => "普通"
            };
        }

        /// <summary>
        /// 根据势力类型获取图标
        /// </summary>
        private PackIconKind GetIconKind(string type)
        {
            return type switch
            {
                "宗门" => PackIconKind.Castle,
                "家族" => PackIconKind.Home,
                "组织" => PackIconKind.Eye,
                "国家" => PackIconKind.Crown,
                "商会" => PackIconKind.Store,
                _ => PackIconKind.Group
            };
        }

        /// <summary>
        /// 根据势力类型获取图标颜色
        /// </summary>
        private Brush GetIconColor(string type)
        {
            return type switch
            {
                "宗门" => new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                "家族" => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                "组织" => new SolidColorBrush(Color.FromRgb(103, 58, 183)),
                "国家" => new SolidColorBrush(Color.FromRgb(255, 193, 7)),
                "商会" => new SolidColorBrush(Color.FromRgb(0, 150, 136)),
                _ => new SolidColorBrush(Color.FromRgb(158, 158, 158))
            };
        }

        /// <summary>
        /// 根据实力等级获取颜色
        /// </summary>
        private Brush GetPowerLevelColor(int powerLevel)
        {
            return powerLevel switch
            {
                >= 90 => new SolidColorBrush(Color.FromRgb(244, 67, 54)), // 红色 - 超级
                >= 70 => new SolidColorBrush(Color.FromRgb(255, 152, 0)), // 橙色 - 一流
                >= 50 => new SolidColorBrush(Color.FromRgb(76, 175, 80)), // 绿色 - 二流
                >= 30 => new SolidColorBrush(Color.FromRgb(33, 150, 243)), // 蓝色 - 三流
                _ => new SolidColorBrush(Color.FromRgb(158, 158, 158)) // 灰色 - 普通
            };
        }

        /// <summary>
        /// 创建示例势力数据
        /// </summary>
        private async Task CreateSampleFactionsAsync()
        {
            try
            {
                _logger?.LogInformation("开始创建示例势力数据");

                var sampleFactions = new List<Faction>
                {
                    new Faction
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = _currentProjectId,
                        Name = "玄天宗",
                        Type = "宗门",
                        PowerLevel = 95,
                        Description = "修仙界第一大宗门，拥有悠久历史和强大实力",
                        Territory = "玄天山脉",
                        MemberCount = 50000,
                        Status = "Active",
                        PowerRating = 95,
                        Influence = 90,
                        Importance = 95,
                        Tags = "正道,修仙,济世救民"
                    },
                    new Faction
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = _currentProjectId,
                        Name = "血魔宗",
                        Type = "宗门",
                        PowerLevel = 75,
                        Description = "邪恶的血魔宗，以血祭修炼为主",
                        Territory = "血魔谷",
                        MemberCount = 15000,
                        Status = "Active",
                        PowerRating = 75,
                        Influence = 60,
                        Importance = 70,
                        Tags = "邪道,血祭,弱肉强食"
                    },
                    new Faction
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = _currentProjectId,
                        Name = "苏家",
                        Type = "家族",
                        PowerLevel = 55,
                        Description = "修仙世家，以炼丹闻名",
                        Territory = "苏家庄园",
                        MemberCount = 800,
                        Status = "Active",
                        PowerRating = 55,
                        Influence = 50,
                        Importance = 60,
                        Tags = "家族,炼丹,济世"
                    },
                    new Faction
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = _currentProjectId,
                        Name = "天机阁",
                        Type = "组织",
                        PowerLevel = 80,
                        Description = "神秘的情报组织，掌握天下秘密",
                        Territory = "各大城市",
                        MemberCount = 5000,
                        Status = "Active",
                        PowerRating = 80,
                        Influence = 85,
                        Importance = 75,
                        Tags = "情报,中立,神秘"
                    },
                    new Faction
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = _currentProjectId,
                        Name = "大燕王朝",
                        Type = "国家",
                        PowerLevel = 100,
                        Description = "修仙界最强大的王朝",
                        Territory = "大燕疆域",
                        MemberCount = 100000000,
                        Status = "Active",
                        PowerRating = 100,
                        Influence = 100,
                        Importance = 100,
                        Tags = "王朝,统治,强大"
                    },
                    new Faction
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = _currentProjectId,
                        Name = "万宝商会",
                        Type = "商会",
                        PowerLevel = 70,
                        Description = "修仙界最大的商业组织",
                        Territory = "各大商城",
                        MemberCount = 20000,
                        Status = "Active",
                        PowerRating = 70,
                        Influence = 80,
                        Importance = 65,
                        Tags = "商业,财富,贸易"
                    }
                };

                foreach (var faction in sampleFactions)
                {
                    await _factionService.CreateFactionAsync(faction);
                }

                _logger?.LogInformation("成功创建 {Count} 个示例势力", sampleFactions.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "创建示例势力数据失败");
                throw;
            }
        }

        /// <summary>
        /// 更新势力列表显示
        /// </summary>
        private void UpdateFactionList()
        {
            FactionListControl.ItemsSource = _filteredFactions;
        }

        #endregion

        #region 筛选和搜索

        /// <summary>
        /// 势力类型筛选变化事件
        /// </summary>
        private void FactionTypeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"筛选势力失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 实力等级筛选变化事件
        /// </summary>
        private void PowerLevelFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        /// <summary>
        /// 搜索文本变化事件
        /// </summary>
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        /// <summary>
        /// 应用筛选条件
        /// </summary>
        private void ApplyFilters()
        {
            var searchText = SearchTextBox.Text?.Trim().ToLower() ?? string.Empty;
            var selectedType = ((ComboBoxItem)FactionTypeFilter.SelectedItem)?.Content?.ToString();
            var selectedPowerLevel = ((ComboBoxItem)PowerLevelFilter.SelectedItem)?.Content?.ToString();

            _filteredFactions = _allFactions.Where(f =>
            {
                // 搜索筛选
                var matchesSearch = string.IsNullOrEmpty(searchText) ||
                                  f.Name.ToLower().Contains(searchText) ||
                                  f.Description.ToLower().Contains(searchText) ||
                                  f.Territory.ToLower().Contains(searchText);

                // 类型筛选
                var matchesType = string.IsNullOrEmpty(selectedType) ||
                                selectedType == "全部" ||
                                f.Type == selectedType;

                // 实力等级筛选
                var matchesPowerLevel = string.IsNullOrEmpty(selectedPowerLevel) ||
                                      selectedPowerLevel == "全部" ||
                                      f.PowerLevel == selectedPowerLevel;

                return matchesSearch && matchesType && matchesPowerLevel;
            }).ToList();

            UpdateFactionList();
        }

        #endregion

        #region 势力操作

        /// <summary>
        /// 选择势力
        /// </summary>
        private void SelectFaction(FactionViewModel faction)
        {
            try
            {
                if (faction != null)
                {
                    ShowFactionDetails(faction);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"选择势力失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 显示势力详细信息
        /// </summary>
        private void ShowFactionDetails(FactionViewModel faction)
        {
            try
            {
                // TODO: 创建势力详细信息界面
                MessageBox.Show($"显示势力详细信息: {faction.Name}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"显示势力详情失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 视图模式切换事件
        /// </summary>
        private void ViewMode_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                if (HierarchyViewToggle?.IsChecked == true)
                {
                    // TODO: 切换到层级视图
                    MessageBox.Show("层级视图功能正在开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"切换视图模式失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 新建势力按钮点击事件
        /// </summary>
        private void NewFaction_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBox.Show("新建势力功能正在开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"新建势力操作失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 导入势力按钮点击事件
        /// </summary>
        private void ImportFactions_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBox.Show("导入势力功能正在开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入势力操作失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 导出势力按钮点击事件
        /// </summary>
        private void ExportFactions_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBox.Show("导出势力功能正在开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出势力操作失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 查看关系网络按钮点击事件
        /// </summary>
        private void ViewRelationshipNetwork_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBox.Show("关系网络功能正在开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"查看关系网络失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// AI分析势力按钮点击事件
        /// </summary>
        private void AIAnalysisFaction_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBox.Show("AI势力分析功能正在开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"AI分析势力失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
