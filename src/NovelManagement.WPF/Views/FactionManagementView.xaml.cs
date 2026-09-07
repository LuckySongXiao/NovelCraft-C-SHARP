using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using NovelManagement.WPF.Commands;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using NovelManagement.Application.Services;
using NovelManagement.Application.DTOs;
using NovelManagement.Core.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NovelManagement.WPF.Services;

namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// FactionManagementView.xaml 的交互逻辑
    /// </summary>
    public partial class FactionManagementView : UserControl, INavigationRefreshableView, INavigationAwareView
    {
        /// <summary>
        /// 待定位势力ID（导航携带）。
        /// </summary>
        private Guid? _pendingHighlightFactionId;

        /// <summary>
        /// 势力数据模型
        /// </summary>
        public class FactionViewModel
        {
            /// <summary>
            /// 势力唯一标识。
            /// </summary>
            public Guid FactionId { get; set; }

            /// <summary>
            /// 势力显示编号。
            /// </summary>
            public int Id { get; set; }

            /// <summary>
            /// 势力名称。
            /// </summary>
            public string Name { get; set; } = string.Empty;

            /// <summary>
            /// 用于列表展示的名称。
            /// </summary>
            public string DisplayName { get; set; } = string.Empty;

            /// <summary>
            /// 势力类型。
            /// </summary>
            public string Type { get; set; } = string.Empty;

            /// <summary>
            /// 势力实力等级文本。
            /// </summary>
            public string PowerLevel { get; set; } = string.Empty;

            /// <summary>
            /// 势力描述。
            /// </summary>
            public string Description { get; set; } = string.Empty;

            /// <summary>
            /// 势力领地。
            /// </summary>
            public string Territory { get; set; } = string.Empty;

            /// <summary>
            /// 成员数量。
            /// </summary>
            public int MemberCount { get; set; }

            /// <summary>
            /// 势力领袖名称。
            /// </summary>
            public string Leader { get; set; } = string.Empty;

            /// <summary>
            /// 势力理念。
            /// </summary>
            public string Ideology { get; set; } = string.Empty;

            /// <summary>
            /// 势力资源概况。
            /// </summary>
            public string Resources { get; set; } = string.Empty;

            /// <summary>
            /// 势力图标类型。
            /// </summary>
            public PackIconKind IconKind { get; set; }

            /// <summary>
            /// 图标颜色。
            /// </summary>
            public Brush IconColor { get; set; } = Brushes.Gray;

            /// <summary>
            /// 势力等级对应颜色。
            /// </summary>
            public Brush PowerLevelColor { get; set; } = Brushes.Gray;

            /// <summary>
            /// 盟友列表。
            /// </summary>
            public List<string> Allies { get; set; } = new();

            /// <summary>
            /// 敌对势力列表。
            /// </summary>
            public List<string> Enemies { get; set; } = new();

            /// <summary>
            /// 其他关系列表。
            /// </summary>
            public List<string> Relationships { get; set; } = new();

            /// <summary>
            /// 总部位置。
            /// </summary>
            public string Headquarters { get; set; } = string.Empty;

            /// <summary>
            /// 势力历史。
            /// </summary>
            public string History { get; set; } = string.Empty;

            /// <summary>
            /// 特色能力。
            /// </summary>
            public string SpecialAbilities { get; set; } = string.Empty;

            /// <summary>
            /// 势力备注。
            /// </summary>
            public string Notes { get; set; } = string.Empty;

            /// <summary>
            /// 势力标签。
            /// </summary>
            public string Tags { get; set; } = string.Empty;

            /// <summary>
            /// 势力状态。
            /// </summary>
            public string Status { get; set; } = "Active";

            /// <summary>
            /// 数值化实力等级。
            /// </summary>
            public int NumericPowerLevel { get; set; }

            /// <summary>
            /// 父势力标识。
            /// </summary>
            public Guid? ParentFactionId { get; set; }
        }

        private readonly FactionService _factionService;
        private readonly ILogger<FactionManagementView> _logger;
        private readonly ProjectContextService? _projectContextService;
        private readonly CurrentProjectGuard? _currentProjectGuard;
        private readonly ChapterContentSyncNotificationService? _chapterContentSyncNotificationService;
        private readonly FactionAnalysisService _factionAnalysisService;
        private Guid _currentProjectId;
        private bool _isChapterSyncSubscribed;
        private string? _pendingHighlightedFactionName;

        private List<FactionViewModel> _allFactions = new();
        private List<FactionViewModel> _filteredFactions = new();
        private FactionViewModel? _selectedFaction;

        /// <summary>
        /// 选择势力命令
        /// </summary>
        public ICommand SelectFactionCommand { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <summary>
        /// 初始化势力管理视图。
        /// </summary>
        public FactionManagementView()
        {
            // 从依赖注入容器获取服务
            var serviceProvider = App.ServiceProvider;
            _factionService = serviceProvider.GetRequiredService<FactionService>();
            _logger = serviceProvider.GetRequiredService<ILogger<FactionManagementView>>();
            _projectContextService = serviceProvider.GetService<ProjectContextService>();
            _currentProjectGuard = serviceProvider.GetService<CurrentProjectGuard>();
            _chapterContentSyncNotificationService = serviceProvider.GetService<ChapterContentSyncNotificationService>();
            _factionAnalysisService = serviceProvider.GetService<FactionAnalysisService>()
                ?? new FactionAnalysisService(
                    serviceProvider.GetService<ILogger<FactionAnalysisService>>(),
                    rwkvService: serviceProvider.GetService<NovelManagement.AI.Services.RWKV.IRwkvLightningService>());

            _currentProjectId = GetCurrentProjectId();

            InitializeComponent();
            InitializeCommands();
            Loaded += FactionManagementView_Loaded;
            Unloaded += FactionManagementView_Unloaded;
            _ = LoadFactionsAsync(); // 异步加载势力数据
        }

        /// <summary>
        /// 获取当前项目ID
        /// </summary>
        private Guid GetCurrentProjectId()
        {
            return _projectContextService?.CurrentProjectId ?? Guid.Empty;
        }

        private bool EnsureCurrentProject(string featureName, out Guid projectId)
        {
            if (_currentProjectGuard != null)
            {
                return _currentProjectGuard.TryGetCurrentProjectId(Window.GetWindow(this), featureName, out projectId);
            }

            projectId = GetCurrentProjectId();
            return projectId != Guid.Empty;
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
                if (_currentProjectId == Guid.Empty)
                {
                    _allFactions = new List<FactionViewModel>();
                    _filteredFactions = new List<FactionViewModel>();
                    UpdateFactionList();
                    EnsureCurrentProject("势力管理", out _);
                    return;
                }

                _logger?.LogInformation("开始加载势力数据，项目ID: {ProjectId}", _currentProjectId);

                var factions = HierarchyViewToggle?.IsChecked == true
                    ? await _factionService.GetFactionHierarchyAsync(_currentProjectId)
                    : await _factionService.GetFactionsByProjectIdAsync(_currentProjectId);

                _allFactions = await BuildFactionViewModelsAsync(factions.ToList());

                _filteredFactions = new List<FactionViewModel>(_allFactions);
                UpdateFactionList();
                RenderStatisticsPanel();
                TryHighlightFactionFromSync();
                TryHighlightFactionFromNavigation();

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
                RenderStatisticsPanel();
            }
        }

        /// <summary>
        /// 在项目切换后刷新势力数据。
        /// </summary>
        /// <param name="projectId">当前项目标识。</param>
        /// <param name="projectName">当前项目名称。</param>
        public async Task RefreshOnProjectChangedAsync(Guid? projectId, string? projectName)
        {
            _currentProjectId = projectId ?? Guid.Empty;
            await LoadFactionsAsync();
        }

        private void FactionManagementView_Loaded(object sender, RoutedEventArgs e)
        {
            if (_isChapterSyncSubscribed || _chapterContentSyncNotificationService == null)
            {
                return;
            }

            _chapterContentSyncNotificationService.ChapterContentSynced += OnChapterContentSynced;
            _isChapterSyncSubscribed = true;
        }

        private void FactionManagementView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (!_isChapterSyncSubscribed || _chapterContentSyncNotificationService == null)
            {
                return;
            }

            _chapterContentSyncNotificationService.ChapterContentSynced -= OnChapterContentSynced;
            _isChapterSyncSubscribed = false;
        }

        private void OnChapterContentSynced(object? sender, ChapterContentSyncedEventArgs e)
        {
            if (_currentProjectId == Guid.Empty || e.ProjectId != _currentProjectId)
            {
                return;
            }

            _logger.LogInformation("收到章节同步通知，刷新势力列表，ChapterId={ChapterId}", e.ChapterId);
            _pendingHighlightedFactionName = e.Result.UpdatedFactionNames.FirstOrDefault();
            _ = Dispatcher.BeginInvoke(new Action(() => _ = LoadFactionsAsync()));
        }

        private void TryHighlightFactionFromSync()
        {
            if (string.IsNullOrWhiteSpace(_pendingHighlightedFactionName))
            {
                return;
            }

            var matchedFaction = _allFactions.FirstOrDefault(faction =>
                string.Equals(faction.Name, _pendingHighlightedFactionName, StringComparison.OrdinalIgnoreCase));
            _pendingHighlightedFactionName = null;

            if (matchedFaction != null)
            {
                SelectFaction(matchedFaction);
            }
        }

        /// <summary>
        /// 在导航到当前视图时接收上下文，记录待定位的势力。
        /// </summary>
        public void OnNavigatedTo(NavigationContext context)
        {
            if (context.Payload is EntityHighlightNavigationPayload payload && payload.TargetId.HasValue)
            {
                _pendingHighlightFactionId = payload.TargetId;
                _logger?.LogInformation("收到势力定位导航参数: {FactionId}", payload.TargetId);
                // 数据可能已（同步）加载完成，立即尝试定位；否则由加载完成回调兜底
                TryHighlightFactionFromNavigation();
            }
        }

        /// <summary>
        /// 数据加载完成后按导航参数选中并定位目标势力。
        /// </summary>
        private void TryHighlightFactionFromNavigation()
        {
            if (_pendingHighlightFactionId == null)
            {
                return;
            }

            try
            {
                var targetId = _pendingHighlightFactionId.Value;
                _pendingHighlightFactionId = null;

                var matchedFaction = _allFactions.FirstOrDefault(faction => faction.FactionId == targetId);
                if (matchedFaction == null)
                {
                    _logger?.LogWarning("导航定位势力失败，未找到 FactionId: {FactionId}", targetId);
                    return;
                }

                SelectFaction(matchedFaction);
                _logger?.LogInformation("已按导航参数定位势力: {Name} (FactionId: {FactionId})", matchedFaction.Name, targetId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "导航定位势力时发生异常");
            }
        }

        /// <summary>
        /// 将Faction实体转换为FactionViewModel
        /// </summary>
        private FactionViewModel ConvertToViewModel(Faction faction)
        {
            return new FactionViewModel
            {
                FactionId = faction.Id,
                Id = (int)faction.Id.GetHashCode(), // 临时转换，实际应该使用Guid
                Name = faction.Name,
                DisplayName = faction.Name,
                Type = faction.Type,
                PowerLevel = GetPowerLevelText(faction.PowerLevel),
                NumericPowerLevel = faction.PowerLevel,
                Description = faction.Description ?? string.Empty,
                Territory = faction.Territory ?? string.Empty,
                MemberCount = faction.MemberCount ?? 0,
                Leader = "未设置", // TODO: 根据LeaderId查找角色名称
                Ideology = string.IsNullOrWhiteSpace(faction.Tags) ? "未设置" : faction.Tags,
                Resources = faction.Resources ?? string.Empty,
                IconKind = GetIconKind(faction.Type),
                IconColor = GetIconColor(faction.Type),
                PowerLevelColor = GetPowerLevelColor(faction.PowerLevel),
                Allies = new List<string>(),
                Enemies = new List<string>(),
                Relationships = new List<string>(),
                Headquarters = faction.Headquarters ?? string.Empty,
                History = faction.History ?? string.Empty,
                SpecialAbilities = faction.SpecialAbilities ?? string.Empty,
                Notes = faction.Notes ?? string.Empty,
                Tags = faction.Tags ?? string.Empty,
                Status = faction.Status,
                ParentFactionId = faction.ParentFactionId
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
                _selectedFaction = faction;
                RenderFactionDetailPanel(faction);
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
        private async void ViewMode_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                await LoadFactionsAsync();
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
        private async void NewFaction_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!EnsureCurrentProject("新建势力", out var projectId))
                {
                    return;
                }

                var dialog = new FactionEditDialog(projectId, _allFactions) { Owner = Window.GetWindow(this) };
                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                var faction = dialog.BuildFaction();
                await _factionService.CreateFactionAsync(faction);
                await LoadFactionsAsync();

                var created = _allFactions.FirstOrDefault(f => f.Name == faction.Name);
                if (created != null)
                {
                    ShowFactionDetails(created);
                }
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
        private async void ImportFactions_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!EnsureCurrentProject("导入势力", out var projectId))
                {
                    return;
                }

                var dialog = new OpenFileDialog
                {
                    Title = "导入势力数据",
                    Filter = "JSON 文件|*.json",
                    CheckFileExists = true
                };

                if (dialog.ShowDialog(Window.GetWindow(this)) != true)
                {
                    return;
                }

                var items = JsonSerializer.Deserialize<List<FactionImportModel>>(File.ReadAllText(dialog.FileName))
                    ?? new List<FactionImportModel>();
                if (items.Count == 0)
                {
                    MessageBox.Show("未读取到可导入的势力数据。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var existingFactions = (await _factionService.GetFactionsByProjectIdAsync(projectId)).ToList();
                var importedCount = 0;
                foreach (var item in items)
                {
                    var existing = existingFactions.FirstOrDefault(f => string.Equals(f.Name, item.Name, StringComparison.OrdinalIgnoreCase));
                    if (existing == null)
                    {
                        await _factionService.CreateFactionAsync(item.ToEntity(projectId));
                    }
                    else
                    {
                        item.ApplyTo(existing);
                        await _factionService.UpdateFactionAsync(existing);
                    }

                    importedCount++;
                }

                await LoadFactionsAsync();
                MessageBox.Show($"已完成导入，共处理 {importedCount} 个势力。", "导入完成", MessageBoxButton.OK, MessageBoxImage.Information);
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
                if (_filteredFactions.Count == 0)
                {
                    MessageBox.Show("当前没有可导出的势力。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dialog = new SaveFileDialog
                {
                    Title = "导出势力数据",
                    Filter = "JSON 文件|*.json",
                    FileName = $"势力数据_{DateTime.Now:yyyyMMdd_HHmmss}.json",
                    AddExtension = true
                };

                if (dialog.ShowDialog(Window.GetWindow(this)) != true)
                {
                    return;
                }

                var exportItems = _filteredFactions.Select(FactionImportModel.FromViewModel).ToList();
                Directory.CreateDirectory(Path.GetDirectoryName(dialog.FileName)!);
                File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(exportItems, new JsonSerializerOptions
                {
                    WriteIndented = true
                }));

                MessageBox.Show($"已导出 {_filteredFactions.Count} 个势力到：{dialog.FileName}", "导出完成", MessageBoxButton.OK, MessageBoxImage.Information);
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
                if (Window.GetWindow(this) is MainWindow mainWindow)
                {
                    mainWindow.ShowRelationshipNetwork();
                    return;
                }

                MessageBox.Show("未找到主窗口，无法打开关系网络。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
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
        private async void AIAnalysisFaction_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_allFactions.Count == 0)
                {
                    MessageBox.Show("当前没有可分析的势力。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var allFactions = _allFactions.Select(ToFactionDto).ToList();
                if (_selectedFaction != null)
                {
                    var targetFaction = allFactions.First(f => f.Id == _selectedFaction.FactionId);
                    var analysis = await _factionAnalysisService.AnalyzeFactionAsync(targetFaction, allFactions);
                    ShowAnalysisResultDialog(BuildSingleFactionAnalysisText(analysis), $"AI分析 - {targetFaction.Name}");
                    return;
                }

                var networkAnalysis = await _factionAnalysisService.AnalyzeFactionNetworkAsync(allFactions);
                ShowAnalysisResultDialog(BuildNetworkAnalysisText(networkAnalysis), "AI势力网络分析");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"AI分析势力失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        private async Task<List<FactionViewModel>> BuildFactionViewModelsAsync(IReadOnlyList<Faction> factions)
        {
            var allFactions = factions.ToDictionary(f => f.Id);
            var result = new List<FactionViewModel>();
            foreach (var faction in factions)
            {
                var viewModel = ConvertToViewModel(faction);
                var depth = HierarchyViewToggle?.IsChecked == true ? GetFactionDepth(faction, allFactions) : 0;
                viewModel.DisplayName = depth > 0 ? $"{new string(' ', depth * 2)}- {viewModel.Name}" : viewModel.Name;

                var relationships = (await _factionService.GetFactionRelationshipsAsync(faction.Id)).ToList();
                PopulateRelationshipInfo(viewModel, relationships);
                result.Add(viewModel);
            }

            return result;
        }

        private static int GetFactionDepth(Faction faction, IReadOnlyDictionary<Guid, Faction> allFactions)
        {
            var depth = 0;
            var parentId = faction.ParentFactionId;
            while (parentId.HasValue && allFactions.TryGetValue(parentId.Value, out var parent))
            {
                depth++;
                parentId = parent.ParentFactionId;
            }

            return depth;
        }

        private void PopulateRelationshipInfo(FactionViewModel viewModel, IReadOnlyList<FactionRelationship> relationships)
        {
            foreach (var relationship in relationships)
            {
                var targetName = relationship.SourceFactionId == viewModel.FactionId
                    ? relationship.TargetFaction?.Name
                    : relationship.SourceFaction?.Name;

                if (string.IsNullOrWhiteSpace(targetName))
                {
                    continue;
                }

                var type = relationship.RelationshipType;
                if (type.Contains("盟") || type.Contains("合作") || type.Contains("友"))
                {
                    viewModel.Allies.Add(targetName);
                }
                else if (type.Contains("敌") || type.Contains("对抗") || type.Contains("威胁") || type.Contains("冲突"))
                {
                    viewModel.Enemies.Add(targetName);
                }
                else
                {
                    viewModel.Relationships.Add($"{targetName} ({type})");
                }
            }
        }

        private void RenderStatisticsPanel()
        {
            DetailArea.Children.Clear();

            var total = _allFactions.Count;
            var sectCount = _allFactions.Count(f => f.Type == "宗门");
            var familyCount = _allFactions.Count(f => f.Type == "家族");
            var relationshipCount = _allFactions.Sum(f => f.Allies.Count + f.Enemies.Count + f.Relationships.Count) / 2;

            var panel = new StackPanel();
            panel.Children.Add(new TextBlock
            {
                Text = "势力统计",
                FontSize = 24,
                FontWeight = FontWeights.Medium,
                Margin = new Thickness(0, 0, 0, 24)
            });

            var grid = new UniformGrid { Columns = 2, Margin = new Thickness(0, 0, 0, 24) };
            grid.Children.Add(CreateStatCard("总势力数", total.ToString()));
            grid.Children.Add(CreateStatCard("宗门势力", sectCount.ToString()));
            grid.Children.Add(CreateStatCard("家族势力", familyCount.ToString()));
            grid.Children.Add(CreateStatCard("关系数量", relationshipCount.ToString()));
            panel.Children.Add(grid);

            panel.Children.Add(new TextBlock
            {
                Text = "实力等级分布",
                FontSize = 18,
                FontWeight = FontWeights.Medium,
                Margin = new Thickness(0, 0, 0, 16)
            });

            foreach (var item in new[]
            {
                new { Label = "超级势力", Count = _allFactions.Count(f => f.PowerLevel == "超级"), Color = "#F44336" },
                new { Label = "一流势力", Count = _allFactions.Count(f => f.PowerLevel == "一流"), Color = "#FF9800" },
                new { Label = "二流势力", Count = _allFactions.Count(f => f.PowerLevel == "二流"), Color = "#4CAF50" },
                new { Label = "三流势力", Count = _allFactions.Count(f => f.PowerLevel == "三流"), Color = "#2196F3" },
                new { Label = "普通势力", Count = _allFactions.Count(f => f.PowerLevel == "普通"), Color = "#9E9E9E" }
            })
            {
                panel.Children.Add(CreateDistributionRow(item.Label, item.Count, Math.Max(total, 1), item.Color));
            }

            DetailArea.Children.Add(new MaterialDesignThemes.Wpf.Card
            {
                Padding = new Thickness(24),
                Content = panel
            });
        }

        private void RenderFactionDetailPanel(FactionViewModel faction)
        {
            DetailArea.Children.Clear();

            var panel = new StackPanel();
            panel.Children.Add(new TextBlock
            {
                Text = faction.Name,
                FontSize = 24,
                FontWeight = FontWeights.Bold
            });
            panel.Children.Add(new TextBlock
            {
                Text = $"{faction.Type} | {faction.PowerLevel}势力 | {faction.Status}",
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 8, 0, 20)
            });

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 20)
            };

            buttonPanel.Children.Add(CreateActionButton("编辑", async (_, _) => await EditSelectedFactionAsync(faction), true));
            buttonPanel.Children.Add(CreateActionButton("删除", async (_, _) => await DeleteSelectedFactionAsync(faction), false));
            buttonPanel.Children.Add(CreateActionButton("AI分析", async (_, _) => await AnalyzeSelectedFactionAsync(faction), false));
            buttonPanel.Children.Add(CreateActionButton("返回统计", (_, _) => RenderStatisticsPanel(), false));
            panel.Children.Add(buttonPanel);

            panel.Children.Add(CreateInfoBlock("描述", faction.Description));
            panel.Children.Add(CreateInfoBlock("领地", faction.Territory));
            panel.Children.Add(CreateInfoBlock("总部", faction.Headquarters));
            panel.Children.Add(CreateInfoBlock("成员数量", faction.MemberCount.ToString()));
            panel.Children.Add(CreateInfoBlock("资源", faction.Resources));
            panel.Children.Add(CreateInfoBlock("理念/标签", faction.Tags));
            panel.Children.Add(CreateInfoBlock("历史", faction.History));
            panel.Children.Add(CreateInfoBlock("特色能力", faction.SpecialAbilities));
            panel.Children.Add(CreateInfoBlock("盟友", faction.Allies.Count == 0 ? "无" : string.Join("、", faction.Allies)));
            panel.Children.Add(CreateInfoBlock("敌对势力", faction.Enemies.Count == 0 ? "无" : string.Join("、", faction.Enemies)));
            panel.Children.Add(CreateInfoBlock("其他关系", faction.Relationships.Count == 0 ? "无" : string.Join(Environment.NewLine, faction.Relationships)));
            panel.Children.Add(CreateInfoBlock("备注", faction.Notes));

            DetailArea.Children.Add(new MaterialDesignThemes.Wpf.Card
            {
                Padding = new Thickness(24),
                Content = new ScrollViewer
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Content = panel
                }
            });
        }

        private static UIElement CreateStatCard(string title, string value)
        {
            return new MaterialDesignThemes.Wpf.Card
            {
                Margin = new Thickness(6),
                Padding = new Thickness(16),
                Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = title, FontSize = 14, Foreground = Brushes.Gray },
                        new TextBlock { Text = value, FontSize = 32, FontWeight = FontWeights.Bold }
                    }
                }
            };
        }

        private static UIElement CreateDistributionRow(string label, int count, int total, string chipColor)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var chip = new Chip
            {
                Content = label,
                Margin = new Thickness(0, 0, 12, 0),
                Background = (Brush)new BrushConverter().ConvertFromString(chipColor)!
            };
            Grid.SetColumn(chip, 0);

            var progressBar = new ProgressBar
            {
                Value = count,
                Maximum = total,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(progressBar, 1);

            var textBlock = new TextBlock
            {
                Text = count.ToString(),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0)
            };
            Grid.SetColumn(textBlock, 2);

            grid.Children.Add(chip);
            grid.Children.Add(progressBar);
            grid.Children.Add(textBlock);
            return grid;
        }

        private static Button CreateActionButton(string text, RoutedEventHandler handler, bool primary)
        {
            var button = new Button
            {
                Content = text,
                Margin = new Thickness(0, 0, 12, 0),
                Padding = new Thickness(16, 8, 16, 8)
            };
            button.Click += handler;
            if (primary)
            {
                button.Background = new SolidColorBrush(Color.FromRgb(33, 150, 243));
                button.Foreground = Brushes.White;
            }

            return button;
        }

        private static UIElement CreateInfoBlock(string title, string? value)
        {
            return new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 12),
                Children =
                {
                    new TextBlock { Text = title, FontWeight = FontWeights.SemiBold },
                    new TextBlock
                    {
                        Text = string.IsNullOrWhiteSpace(value) ? "未设置" : value,
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = Brushes.Gray,
                        Margin = new Thickness(0, 4, 0, 0)
                    }
                }
            };
        }

        private async Task EditSelectedFactionAsync(FactionViewModel faction)
        {
            if (!EnsureCurrentProject("编辑势力", out var projectId))
            {
                return;
            }

            var dialog = new FactionEditDialog(projectId, _allFactions.Where(f => f.FactionId != faction.FactionId), faction)
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var entity = await _factionService.GetFactionByIdAsync(faction.FactionId);
            if (entity == null)
            {
                MessageBox.Show("未找到要编辑的势力。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            dialog.ApplyTo(entity);
            await _factionService.UpdateFactionAsync(entity);
            await LoadFactionsAsync();
            var updated = _allFactions.FirstOrDefault(f => f.FactionId == entity.Id);
            if (updated != null)
            {
                ShowFactionDetails(updated);
            }
        }

        private async Task DeleteSelectedFactionAsync(FactionViewModel faction)
        {
            var result = MessageBox.Show($"确定要删除势力“{faction.Name}”吗？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            await _factionService.DeleteFactionAsync(faction.FactionId);
            _selectedFaction = null;
            await LoadFactionsAsync();
        }

        private async Task AnalyzeSelectedFactionAsync(FactionViewModel faction)
        {
            _selectedFaction = faction;
            var allFactions = _allFactions.Select(ToFactionDto).ToList();
            var targetFaction = allFactions.First(f => f.Id == faction.FactionId);
            var analysis = await _factionAnalysisService.AnalyzeFactionAsync(targetFaction, allFactions);
            ShowAnalysisResultDialog(BuildSingleFactionAnalysisText(analysis), $"AI分析 - {faction.Name}");
        }

        private static FactionDto ToFactionDto(FactionViewModel faction)
        {
            return new FactionDto
            {
                Id = faction.FactionId,
                Name = faction.Name,
                Type = faction.Type,
                PowerLevel = faction.PowerLevel,
                Description = faction.Description,
                MemberCount = faction.MemberCount,
                Resources = faction.Resources,
                Territory = faction.Territory,
                Tags = faction.Tags,
                Headquarters = faction.Headquarters,
                Notes = faction.Notes,
                Status = faction.Status
            };
        }

        private static string BuildSingleFactionAnalysisText(FactionAnalysisService.FactionAnalysisResult analysis)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"势力：{analysis.FactionName}");
            builder.AppendLine($"综合评分：{analysis.OverallScore:F1}");
            builder.AppendLine($"实力评分：{analysis.PowerScore:F1}");
            builder.AppendLine($"影响力评分：{analysis.InfluenceScore:F1}");
            builder.AppendLine($"稳定性评分：{analysis.StabilityScore:F1}");
            builder.AppendLine($"威胁等级：{analysis.ThreatLevel:F1}");
            builder.AppendLine();
            builder.AppendLine("优势：");
            foreach (var item in analysis.Strengths.DefaultIfEmpty("无"))
            {
                builder.AppendLine($"- {item}");
            }
            builder.AppendLine("劣势：");
            foreach (var item in analysis.Weaknesses.DefaultIfEmpty("无"))
            {
                builder.AppendLine($"- {item}");
            }
            builder.AppendLine("机会：");
            foreach (var item in analysis.Opportunities.DefaultIfEmpty("无"))
            {
                builder.AppendLine($"- {item}");
            }
            builder.AppendLine("威胁：");
            foreach (var item in analysis.Threats.DefaultIfEmpty("无"))
            {
                builder.AppendLine($"- {item}");
            }
            builder.AppendLine("战略建议：");
            foreach (var item in analysis.StrategicRecommendations.DefaultIfEmpty("无"))
            {
                builder.AppendLine($"- {item}");
            }
            builder.AppendLine("关系分析：");
            foreach (var item in analysis.Relationships.DefaultIfEmpty())
            {
                if (item == null)
                {
                    builder.AppendLine("- 无");
                    break;
                }

                builder.AppendLine($"- {item.TargetFactionName}：{item.RelationshipType}（强度 {item.RelationshipStrength:F1}）");
            }

            return builder.ToString();
        }

        private static string BuildNetworkAnalysisText(FactionAnalysisService.NetworkAnalysisResult analysis)
        {
            var builder = new StringBuilder();
            builder.AppendLine("势力网络分析");
            builder.AppendLine($"势力总数：{analysis.TotalFactions}");
            builder.AppendLine($"网络稳定性：{analysis.NetworkStability:F1}");
            builder.AppendLine();
            builder.AppendLine("关键节点：");
            foreach (var item in analysis.KeyNodes.DefaultIfEmpty("无"))
            {
                builder.AppendLine($"- {item}");
            }
            builder.AppendLine("权力中心：");
            foreach (var item in analysis.PowerCenters.DefaultIfEmpty("无"))
            {
                builder.AppendLine($"- {item}");
            }
            builder.AppendLine("网络洞察：");
            foreach (var item in analysis.NetworkInsights.DefaultIfEmpty("无"))
            {
                builder.AppendLine($"- {item}");
            }

            return builder.ToString();
        }

        private static void ShowAnalysisResultDialog(string text, string title)
        {
            var window = new Window
            {
                Title = title,
                Width = 760,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new DockPanel
                {
                    Margin = new Thickness(20),
                    Children =
                    {
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            HorizontalAlignment = HorizontalAlignment.Right,
                            Margin = new Thickness(0, 12, 0, 0),
                            VerticalAlignment = VerticalAlignment.Bottom
                        }
                    }
                }
            };

            var root = (DockPanel)window.Content;
            var textBox = new TextBox
            {
                Text = text,
                IsReadOnly = true,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            DockPanel.SetDock(textBox, Dock.Top);
            root.Children.Add(textBox);

            var buttonPanel = (StackPanel)root.Children[0];
            var copyButton = new Button { Content = "复制结果", Width = 88, Margin = new Thickness(0, 0, 12, 0) };
            copyButton.Click += (_, _) =>
            {
                Clipboard.SetText(text);
                MessageBox.Show("结果已复制到剪贴板。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            };
            var closeButton = new Button { Content = "关闭", Width = 88, IsDefault = true };
            closeButton.Click += (_, _) => window.Close();
            buttonPanel.Children.Add(copyButton);
            buttonPanel.Children.Add(closeButton);

            window.ShowDialog();
        }
    }

    internal sealed class FactionImportModel
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "组织";
        public int PowerLevel { get; set; } = 50;
        public string Description { get; set; } = string.Empty;
        public string Territory { get; set; } = string.Empty;
        public int MemberCount { get; set; }
        public string Resources { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public string Headquarters { get; set; } = string.Empty;
        public string History { get; set; } = string.Empty;
        public string SpecialAbilities { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";

        public Faction ToEntity(Guid projectId)
        {
            return new Faction
            {
                ProjectId = projectId,
                Name = Name,
                Type = Type,
                PowerLevel = PowerLevel,
                Description = Description,
                Territory = Territory,
                MemberCount = MemberCount,
                Resources = Resources,
                Tags = Tags,
                Headquarters = Headquarters,
                History = History,
                SpecialAbilities = SpecialAbilities,
                Notes = Notes,
                Status = Status,
                Importance = Math.Clamp(PowerLevel / 10, 1, 10),
                Influence = Math.Clamp(PowerLevel, 1, 100)
            };
        }

        public void ApplyTo(Faction faction)
        {
            faction.Name = Name;
            faction.Type = Type;
            faction.PowerLevel = PowerLevel;
            faction.Description = Description;
            faction.Territory = Territory;
            faction.MemberCount = MemberCount;
            faction.Resources = Resources;
            faction.Tags = Tags;
            faction.Headquarters = Headquarters;
            faction.History = History;
            faction.SpecialAbilities = SpecialAbilities;
            faction.Notes = Notes;
            faction.Status = Status;
            faction.Importance = Math.Clamp(PowerLevel / 10, 1, 10);
            faction.Influence = Math.Clamp(PowerLevel, 1, 100);
        }

        public static FactionImportModel FromViewModel(FactionManagementView.FactionViewModel faction)
        {
            return new FactionImportModel
            {
                Name = faction.Name,
                Type = faction.Type,
                PowerLevel = faction.NumericPowerLevel,
                Description = faction.Description,
                Territory = faction.Territory,
                MemberCount = faction.MemberCount,
                Resources = faction.Resources,
                Tags = faction.Tags,
                Headquarters = faction.Headquarters,
                History = faction.History,
                SpecialAbilities = faction.SpecialAbilities,
                Notes = faction.Notes,
                Status = faction.Status
            };
        }
    }

    internal sealed class FactionEditDialog : Window
    {
        private readonly Guid _projectId;
        private readonly IAIAssistantService? _aiAssistantService;
        private readonly ProjectReadModelService? _projectReadModelService;
        private readonly IEnumerable<FactionManagementView.FactionViewModel> _existingFactions;
        private readonly FactionManagementView.FactionViewModel? _originalFaction;
        private readonly TextBox _nameTextBox = new();
        private readonly ComboBox _typeComboBox = new();
        private readonly Slider _powerLevelSlider = new();
        private readonly TextBox _memberCountTextBox = new();
        private readonly TextBox _territoryTextBox = new();
        private readonly TextBox _headquartersTextBox = new();
        private readonly TextBox _descriptionTextBox = new();
        private readonly TextBox _resourcesTextBox = new();
        private readonly TextBox _historyTextBox = new();
        private readonly TextBox _abilitiesTextBox = new();
        private readonly TextBox _tagsTextBox = new();
        private readonly TextBox _notesTextBox = new();
        private readonly ComboBox _statusComboBox = new();
        private readonly ComboBox _parentFactionComboBox = new();

        public FactionEditDialog(Guid projectId, IEnumerable<FactionManagementView.FactionViewModel> existingFactions, FactionManagementView.FactionViewModel? faction = null)
        {
            _projectId = projectId;
            _existingFactions = existingFactions.ToList();
            _originalFaction = faction;
            _aiAssistantService = App.ServiceProvider?.GetService<IAIAssistantService>();
            _projectReadModelService = App.ServiceProvider?.GetService<ProjectReadModelService>();

            Title = faction == null ? "新建势力" : $"编辑势力 - {faction.Name}";
            Width = 620;
            Height = 760;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            _typeComboBox.ItemsSource = new[] { "宗门", "家族", "组织", "国家", "商会" };
            _statusComboBox.ItemsSource = new[] { "Active", "Dormant", "Destroyed", "Secret" };
            _parentFactionComboBox.Items.Add("(无)");
            foreach (var item in _existingFactions)
            {
                _parentFactionComboBox.Items.Add(item.Name);
            }

            _powerLevelSlider.Minimum = 1;
            _powerLevelSlider.Maximum = 100;
            _powerLevelSlider.Value = faction?.NumericPowerLevel ?? 50;
            _powerLevelSlider.TickFrequency = 5;
            _powerLevelSlider.IsSnapToTickEnabled = false;
            _memberCountTextBox.Text = (faction?.MemberCount ?? 0).ToString();

            _nameTextBox.Text = faction?.Name ?? string.Empty;
            _territoryTextBox.Text = faction?.Territory ?? string.Empty;
            _headquartersTextBox.Text = faction?.Headquarters ?? string.Empty;
            _descriptionTextBox.Text = faction?.Description ?? string.Empty;
            _resourcesTextBox.Text = faction?.Resources ?? string.Empty;
            _historyTextBox.Text = faction?.History ?? string.Empty;
            _abilitiesTextBox.Text = faction?.SpecialAbilities ?? string.Empty;
            _tagsTextBox.Text = faction?.Tags ?? string.Empty;
            _notesTextBox.Text = faction?.Notes ?? string.Empty;
            _typeComboBox.SelectedItem = faction?.Type ?? "组织";
            _statusComboBox.SelectedItem = faction?.Status ?? "Active";
            _parentFactionComboBox.SelectedItem = faction?.ParentFactionId == null
                ? "(无)"
                : _existingFactions.FirstOrDefault(f => f.FactionId == faction.ParentFactionId)?.Name ?? "(无)";

            Content = BuildContent();
        }

        public Faction BuildFaction()
        {
            return new Faction
            {
                ProjectId = _projectId,
                Name = _nameTextBox.Text.Trim(),
                Type = _typeComboBox.SelectedItem?.ToString() ?? "组织",
                PowerLevel = (int)_powerLevelSlider.Value,
                MemberCount = ParseMemberCount(),
                Territory = _territoryTextBox.Text.Trim(),
                Headquarters = _headquartersTextBox.Text.Trim(),
                Description = _descriptionTextBox.Text.Trim(),
                Resources = _resourcesTextBox.Text.Trim(),
                History = _historyTextBox.Text.Trim(),
                SpecialAbilities = _abilitiesTextBox.Text.Trim(),
                Tags = _tagsTextBox.Text.Trim(),
                Notes = _notesTextBox.Text.Trim(),
                Status = _statusComboBox.SelectedItem?.ToString() ?? "Active",
                ParentFactionId = ResolveParentFactionId(),
                Importance = Math.Clamp((int)_powerLevelSlider.Value / 10, 1, 10),
                Influence = Math.Clamp((int)_powerLevelSlider.Value, 1, 100)
            };
        }

        public void ApplyTo(Faction faction)
        {
            var updated = BuildFaction();
            faction.Name = updated.Name;
            faction.Type = updated.Type;
            faction.PowerLevel = updated.PowerLevel;
            faction.MemberCount = updated.MemberCount;
            faction.Territory = updated.Territory;
            faction.Headquarters = updated.Headquarters;
            faction.Description = updated.Description;
            faction.Resources = updated.Resources;
            faction.History = updated.History;
            faction.SpecialAbilities = updated.SpecialAbilities;
            faction.Tags = updated.Tags;
            faction.Notes = updated.Notes;
            faction.Status = updated.Status;
            faction.ParentFactionId = updated.ParentFactionId;
            faction.Importance = updated.Importance;
            faction.Influence = updated.Influence;
        }

        private FrameworkElement BuildContent()
        {
            var panel = new StackPanel { Margin = new Thickness(20) };
            panel.Children.Add(CreateLabel("势力名称"));
            panel.Children.Add(_nameTextBox);
            panel.Children.Add(CreateLabel("势力类型"));
            panel.Children.Add(_typeComboBox);
            panel.Children.Add(CreateLabel("父势力"));
            panel.Children.Add(_parentFactionComboBox);
            panel.Children.Add(CreateLabel("实力等级"));
            panel.Children.Add(_powerLevelSlider);
            panel.Children.Add(CreateLabel("成员数量"));
            panel.Children.Add(_memberCountTextBox);
            panel.Children.Add(CreateLabel("领地"));
            panel.Children.Add(_territoryTextBox);
            panel.Children.Add(CreateLabel("总部"));
            panel.Children.Add(_headquartersTextBox);
            panel.Children.Add(CreateLabel("势力描述"));
            panel.Children.Add(CreateMultilineTextBox(_descriptionTextBox, 80));
            panel.Children.Add(CreateLabel("资源"));
            panel.Children.Add(CreateMultilineTextBox(_resourcesTextBox, 60));
            panel.Children.Add(CreateLabel("历史"));
            panel.Children.Add(CreateMultilineTextBox(_historyTextBox, 80));
            panel.Children.Add(CreateLabel("特色能力"));
            panel.Children.Add(CreateMultilineTextBox(_abilitiesTextBox, 60));
            panel.Children.Add(CreateLabel("标签"));
            panel.Children.Add(_tagsTextBox);
            panel.Children.Add(CreateLabel("状态"));
            panel.Children.Add(_statusComboBox);
            panel.Children.Add(CreateLabel("备注"));
            panel.Children.Add(CreateMultilineTextBox(_notesTextBox, 80));

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 16, 0, 0)
            };

            var aiButton = new Button { Content = "AI自动补全", Width = 100, Margin = new Thickness(0, 0, 12, 0) };
            aiButton.Click += async (_, _) => await AutoFillWithAiAsync();
            var resetButton = new Button { Content = "重置内容", Width = 100, Margin = new Thickness(0, 0, 12, 0) };
            resetButton.Click += (_, _) => ResetForm();
            var okButton = new Button { Content = "保存", Width = 84, Margin = new Thickness(0, 0, 12, 0), IsDefault = true };
            okButton.Click += (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(_nameTextBox.Text))
                {
                    MessageBox.Show("请输入势力名称。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                DialogResult = true;
            };
            var cancelButton = new Button { Content = "返回", Width = 84, IsCancel = true };

            buttonPanel.Children.Add(aiButton);
            buttonPanel.Children.Add(resetButton);
            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            panel.Children.Add(buttonPanel);

            return new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = panel
            };
        }

        private async Task AutoFillWithAiAsync()
        {
            if (_aiAssistantService == null)
            {
                MessageBox.Show("AI助手服务未初始化。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var prompt = await BuildAiPromptAsync();
            var result = await _aiAssistantService.GeneratePlotAsync(new Dictionary<string, object>
            {
                ["plotType"] = "势力设定",
                ["theme"] = string.IsNullOrWhiteSpace(_nameTextBox.Text)
                    ? (_typeComboBox.SelectedItem?.ToString() ?? "势力设定")
                    : _nameTextBox.Text.Trim(),
                ["requirements"] = prompt
            });

            if (!result.IsSuccess || result.Data == null)
            {
                MessageBox.Show(result.Message ?? "AI自动补全失败。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var text = AiAutoFillFormatter.Normalize(result.Data.ToString());
            AiAutoFillFormatter.FillIfEmpty(_descriptionTextBox, AiAutoFillFormatter.ExtractSummary(text, "势力描述", "描述", "简介"));
            AiAutoFillFormatter.FillSectionIfEmpty(_territoryTextBox, text, "领地");
            AiAutoFillFormatter.FillSectionIfEmpty(_headquartersTextBox, text, "总部");
            AiAutoFillFormatter.FillSectionIfEmpty(_resourcesTextBox, text, "资源");
            AiAutoFillFormatter.FillSectionIfEmpty(_historyTextBox, text, "历史");
            AiAutoFillFormatter.FillSectionIfEmpty(_abilitiesTextBox, text, "特色能力", "能力");
            AiAutoFillFormatter.FillSectionIfEmpty(_tagsTextBox, text, "标签");
            AiAutoFillFormatter.FillSectionIfEmpty(_notesTextBox, text, "备注");
            if (string.IsNullOrWhiteSpace(_nameTextBox.Text))
            {
                var generatedName = AiAutoFillFormatter.ExtractSection(text, "名称");
                if (string.IsNullOrWhiteSpace(generatedName))
                {
                    generatedName = AiAutoFillFormatter.ExtractFirstMeaningfulLine(text);
                }

                _nameTextBox.Text = string.IsNullOrWhiteSpace(generatedName) ? "AI势力" : generatedName;
            }

            MessageBox.Show("已完成势力信息自动补全。", "AI自动补全", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task<string> BuildAiPromptAsync()
        {
            var builder = new StringBuilder();
            builder.AppendLine("请补全一个书籍中的势力设定。");
            builder.AppendLine("请优先补全缺失信息，并与现有内容保持一致。");
            builder.AppendLine("内容必须属于世界设定层的势力组织，不要输出剧情大纲、正文片段、人物小传或无关模块。");
            builder.AppendLine("请仅按以下字段输出，不要思考过程、解释、Markdown、序号或特殊符号。");
            builder.AppendLine();

            if (_projectReadModelService != null && _projectId != Guid.Empty)
            {
                try
                {
                    var projectContext = await _projectReadModelService.BuildAiContextDataAsync(_projectId);
                    if (!string.IsNullOrWhiteSpace(projectContext.PromptSummary))
                    {
                        builder.AppendLine(projectContext.PromptSummary);
                        builder.AppendLine();
                    }
                }
                catch
                {
                }
            }

            var parentFactionName = _parentFactionComboBox.SelectedItem?.ToString();
            if (!string.IsNullOrWhiteSpace(parentFactionName) && parentFactionName != "(无)")
            {
                var parentFaction = _existingFactions.FirstOrDefault(item => item.Name == parentFactionName);
                if (parentFaction != null)
                {
                    builder.AppendLine("【父势力约束】");
                    builder.AppendLine($"名称：{parentFaction.Name}");
                    builder.AppendLine($"类型：{parentFaction.Type}");
                    builder.AppendLine($"描述：{parentFaction.Description}");
                    builder.AppendLine($"领地：{parentFaction.Territory}");
                    builder.AppendLine($"资源：{parentFaction.Resources}");
                    builder.AppendLine($"总部：{parentFaction.Headquarters}");
                    builder.AppendLine($"历史：{parentFaction.History}");
                    builder.AppendLine($"特色能力：{parentFaction.SpecialAbilities}");
                    builder.AppendLine($"标签：{parentFaction.Tags}");
                    builder.AppendLine();
                }
            }

            builder.AppendLine("当前信息：");
            builder.AppendLine($"名称：{_nameTextBox.Text}");
            builder.AppendLine($"类型：{_typeComboBox.SelectedItem}");
            builder.AppendLine($"父势力：{_parentFactionComboBox.SelectedItem}");
            builder.AppendLine($"实力等级：{(int)_powerLevelSlider.Value}");
            builder.AppendLine($"成员数量：{_memberCountTextBox.Text}");
            builder.AppendLine($"领地：{_territoryTextBox.Text}");
            builder.AppendLine($"总部：{_headquartersTextBox.Text}");
            builder.AppendLine($"势力描述：{_descriptionTextBox.Text}");
            builder.AppendLine($"资源：{_resourcesTextBox.Text}");
            builder.AppendLine($"历史：{_historyTextBox.Text}");
            builder.AppendLine($"特色能力：{_abilitiesTextBox.Text}");
            builder.AppendLine($"标签：{_tagsTextBox.Text}");
            builder.AppendLine($"状态：{_statusComboBox.SelectedItem}");
            builder.AppendLine($"备注：{_notesTextBox.Text}");
            builder.AppendLine();
            builder.AppendLine("生成要求：");
            builder.AppendLine("1. 必须优先遵循项目基础信息、已有世界设定和大纲约束。");
            builder.AppendLine("2. 只能补全势力组织相关字段，不得生成无关世界设定分支。");
            builder.AppendLine("3. 如果选择了父势力，新内容必须与父势力主题和结构保持一致。");
            builder.AppendLine("4. 若与现有输入冲突，优先保持现有输入语义一致。");
            builder.AppendLine();
            builder.AppendLine("请按以下字段输出：");
            builder.AppendLine("名称：");
            builder.AppendLine("势力描述：");
            builder.AppendLine("领地：");
            builder.AppendLine("总部：");
            builder.AppendLine("资源：");
            builder.AppendLine("历史：");
            builder.AppendLine("特色能力：");
            builder.AppendLine("标签：");
            builder.AppendLine("备注：");
            return builder.ToString().Trim();
        }

        private void ResetForm()
        {
            _nameTextBox.Text = _originalFaction?.Name ?? string.Empty;
            _territoryTextBox.Text = _originalFaction?.Territory ?? string.Empty;
            _headquartersTextBox.Text = _originalFaction?.Headquarters ?? string.Empty;
            _descriptionTextBox.Text = _originalFaction?.Description ?? string.Empty;
            _resourcesTextBox.Text = _originalFaction?.Resources ?? string.Empty;
            _historyTextBox.Text = _originalFaction?.History ?? string.Empty;
            _abilitiesTextBox.Text = _originalFaction?.SpecialAbilities ?? string.Empty;
            _tagsTextBox.Text = _originalFaction?.Tags ?? string.Empty;
            _notesTextBox.Text = _originalFaction?.Notes ?? string.Empty;
            _typeComboBox.SelectedItem = _originalFaction?.Type ?? "组织";
            _statusComboBox.SelectedItem = _originalFaction?.Status ?? "Active";
            _memberCountTextBox.Text = (_originalFaction?.MemberCount ?? 0).ToString();
            _powerLevelSlider.Value = _originalFaction?.NumericPowerLevel ?? 50;
            _parentFactionComboBox.SelectedItem = _originalFaction?.ParentFactionId == null
                ? "(无)"
                : _existingFactions.FirstOrDefault(item => item.FactionId == _originalFaction.ParentFactionId)?.Name ?? "(无)";
        }

        private int ParseMemberCount()
        {
            return int.TryParse(_memberCountTextBox.Text, out var value) ? value : 0;
        }

        private Guid? ResolveParentFactionId()
        {
            var selected = _parentFactionComboBox.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(selected) || selected == "(无)")
            {
                return null;
            }

            return _existingFactions.FirstOrDefault(f => f.Name == selected)?.FactionId;
        }

        private static TextBlock CreateLabel(string text)
        {
            return new TextBlock { Text = text, Margin = new Thickness(0, 10, 0, 6), FontWeight = FontWeights.Medium };
        }

        private static TextBox CreateMultilineTextBox(TextBox textBox, double height)
        {
            textBox.AcceptsReturn = true;
            textBox.Height = height;
            textBox.TextWrapping = TextWrapping.Wrap;
            textBox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            return textBox;
        }

    }
}
