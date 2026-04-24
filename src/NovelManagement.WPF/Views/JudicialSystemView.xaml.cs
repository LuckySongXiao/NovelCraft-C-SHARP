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
    /// 司法体系管理视图
    /// </summary>
    public partial class JudicialSystemView : UserControl, INavigationRefreshableView, INavigationAwareView
    {
        #region 属性

        /// <summary>
        /// 司法体系列表
        /// </summary>
        public ObservableCollection<JudicialSystemViewModel> JudicialSystems { get; set; }

        /// <summary>
        /// 当前选中的司法体系
        /// </summary>
        public JudicialSystemViewModel SelectedJudicialSystem { get; set; }

        /// <summary>
        /// 法院列表
        /// </summary>
        public ObservableCollection<CourtViewModel> Courts { get; set; }

        /// <summary>
        /// 选择司法体系命令
        /// </summary>
        public ICommand SelectJudicialSystemCommand { get; set; }

        /// <summary>
        /// 日志记录器
        /// </summary>
        private readonly ILogger<JudicialSystemView>? _logger;

        /// <summary>
        /// AI助手服务
        /// </summary>
        private readonly IAIAssistantService? _aiAssistantService;

        private readonly JudicialDataService? _judicialDataService;
        private readonly ProjectContextService? _projectContextService;
        private readonly CurrentProjectGuard? _currentProjectGuard;
        private Guid _currentProjectId;

        /// <summary>
        /// 获取当前司法体系总数。
        /// </summary>
        public int TotalCount => JudicialSystems.Count;

        /// <summary>
        /// 获取成文法系数量。
        /// </summary>
        public int WrittenLawCount => JudicialSystems.Count(s => s.LegalSystemType == "成文法系");

        /// <summary>
        /// 获取判例法系数量。
        /// </summary>
        public int CaseLawCount => JudicialSystems.Count(s => s.LegalSystemType == "判例法系");

        /// <summary>
        /// 获取宗教法系数量。
        /// </summary>
        public int ReligiousLawCount => JudicialSystems.Count(s => s.LegalSystemType == "宗教法系");

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化司法体系管理视图。
        /// </summary>
        public JudicialSystemView()
        {
            InitializeComponent();

            // 获取服务
            try
            {
                var serviceProvider = App.ServiceProvider;
                _logger = serviceProvider?.GetService<ILogger<JudicialSystemView>>();
                _aiAssistantService = serviceProvider?.GetService<IAIAssistantService>();
                _judicialDataService = serviceProvider?.GetService<JudicialDataService>();
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
            _ = LoadJudicialSystemsAsync();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化数据
        /// </summary>
        private void InitializeData()
        {
            JudicialSystems = new ObservableCollection<JudicialSystemViewModel>();
            Courts = new ObservableCollection<CourtViewModel>();
            
            // 设置数据上下文
            this.DataContext = this;
        }

        /// <summary>
        /// 初始化命令
        /// </summary>
        private void InitializeCommands()
        {
            SelectJudicialSystemCommand = new RelayCommand<JudicialSystemViewModel>(SelectJudicialSystem);
        }

        #endregion

        #region 数据加载

        /// <summary>
        /// 加载司法体系数据
        /// </summary>
        private async Task LoadJudicialSystemsAsync()
        {
            try
            {
                _currentProjectId = _projectContextService?.CurrentProjectId ?? Guid.Empty;
                if (_currentProjectId == Guid.Empty)
                {
                    _currentProjectGuard?.TryGetCurrentProjectId(Window.GetWindow(this), "司法体系管理", out _);
                    JudicialSystems.Clear();
                    JudicialSystemListControl.ItemsSource = JudicialSystems;
                    UpdateStatistics();
                    HideEditPanel();
                    return;
                }

                var judicialSystems = _judicialDataService == null
                    ? new List<JudicialSystemViewModel>()
                    : await _judicialDataService.LoadJudicialSystemsAsync(_currentProjectId);

                JudicialSystems.Clear();
                foreach (var system in judicialSystems)
                {
                    system.Courts ??= new List<CourtViewModel>();
                    system.CourtCount = system.Courts.Count;
                    JudicialSystems.Add(system);
                }

                // 设置列表数据源
                JudicialSystemListControl.ItemsSource = JudicialSystems;

                // 更新统计信息
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载司法体系数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 更新统计信息
        /// </summary>
        private void UpdateStatistics()
        {
            JudicialSystemListControl.Items.Refresh();
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
            FilterJudicialSystems();
        }

        /// <summary>
        /// 法律体系类型筛选变化
        /// </summary>
        private void LegalSystemFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterJudicialSystems();
        }

        /// <summary>
        /// 筛选司法体系
        /// </summary>
        private void FilterJudicialSystems()
        {
            var searchText = SearchTextBox?.Text?.ToLower() ?? "";
            var selectedType = (LegalSystemFilterComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString();

            var filteredSystems = JudicialSystems.Where(s =>
                (string.IsNullOrEmpty(searchText) || 
                 s.Name.ToLower().Contains(searchText) || 
                 s.Description.ToLower().Contains(searchText) ||
                 s.Jurisdiction.ToLower().Contains(searchText)) &&
                (selectedType == "全部类型" || selectedType == null || s.LegalSystemType == selectedType)
            ).ToList();

            JudicialSystemListControl.ItemsSource = filteredSystems;
        }

        /// <summary>
        /// 选择司法体系
        /// </summary>
        private void SelectJudicialSystem(JudicialSystemViewModel system)
        {
            if (system == null) return;

            SelectedJudicialSystem = system;
            LoadJudicialSystemDetails(system);
            ShowEditPanel();
        }

        /// <summary>
        /// 加载司法体系详情
        /// </summary>
        private void LoadJudicialSystemDetails(JudicialSystemViewModel system)
        {
            // 填充基本信息
            JudicialNameTextBox.Text = system.Name;
            JudicialDescriptionTextBox.Text = system.Description;
            JurisdictionTextBox.Text = system.Jurisdiction;
            DimensionIdTextBox.Text = system.DimensionId;
            
            // 设置法律体系类型选择
            foreach (ComboBoxItem item in LegalSystemTypeComboBox.Items)
            {
                if (item.Content.ToString() == system.LegalSystemType)
                {
                    LegalSystemTypeComboBox.SelectedItem = item;
                    break;
                }
            }

            // 加载法院列表
            LoadCourts(system);
        }

        /// <summary>
        /// 加载法院列表
        /// </summary>
        private void LoadCourts(JudicialSystemViewModel system)
        {
            var courts = system.Courts ?? new List<CourtViewModel>();

            Courts.Clear();
            foreach (var court in courts)
            {
                Courts.Add(court);
            }

            CourtListControl.ItemsSource = Courts;
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
            SelectedJudicialSystem = null;
        }

        #endregion

        #region 按钮事件

        /// <summary>
        /// 新建司法体系
        /// </summary>
        private void AddJudicialSystem_Click(object sender, RoutedEventArgs e)
        {
            var newSystem = new JudicialSystemViewModel
            {
                Id = 0,
                Name = "",
                LegalSystemType = "成文法系",
                Jurisdiction = "",
                DimensionId = "",
                Description = "",
                CourtCount = 0,
                CreatedAt = DateTime.Now
            };

            SelectedJudicialSystem = newSystem;
            LoadJudicialSystemDetails(newSystem);
            ShowEditPanel();

            // 清空法院列表
            Courts.Clear();
            CourtListControl.ItemsSource = Courts;

            // 聚焦到名称输入框
            JudicialNameTextBox.Focus();
        }

        /// <summary>
        /// 导入司法数据
        /// </summary>
        private async void ImportJudicial_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!EnsureCurrentProject("导入司法体系"))
                {
                    return;
                }

                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "导入司法体系数据",
                    Filter = "JSON文件|*.json|所有文件|*.*",
                    DefaultExt = "json"
                };

                if (dialog.ShowDialog() == true)
                {
                    if (_judicialDataService == null)
                    {
                        MessageBox.Show("司法数据服务未初始化。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var importedSystems = await _judicialDataService.ImportJudicialSystemsAsync(dialog.FileName);
                    JudicialSystems.Clear();
                    foreach (var system in importedSystems)
                    {
                        system.Courts ??= new List<CourtViewModel>();
                        system.CourtCount = system.Courts.Count;
                        JudicialSystems.Add(system);
                    }

                    await PersistJudicialSystemsAsync();
                    FilterJudicialSystems();
                    UpdateStatistics();
                    MessageBox.Show($"已成功导入 {JudicialSystems.Count} 个司法体系。", "导入成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 导出司法数据
        /// </summary>
        private async void ExportJudicial_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!EnsureCurrentProject("导出司法体系"))
                {
                    return;
                }

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "导出司法体系数据",
                    Filter = "JSON文件|*.json",
                    DefaultExt = "json",
                    FileName = $"司法体系数据_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (dialog.ShowDialog() == true)
                {
                    if (_judicialDataService == null)
                    {
                        MessageBox.Show("司法数据服务未初始化。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    await _judicialDataService.ExportJudicialSystemsAsync(_currentProjectId, JudicialSystems, dialog.FileName);
                    MessageBox.Show($"司法体系数据已导出到：{dialog.FileName}", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 添加法院
        /// </summary>
        private void AddCourt_Click(object sender, RoutedEventArgs e)
        {
            var newCourt = new CourtViewModel
            {
                Name = "",
                Level = "基层法院",
                Jurisdiction = ""
            };

            Courts.Add(newCourt);
        }

        /// <summary>
        /// 删除法院
        /// </summary>
        private void RemoveCourt_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is CourtViewModel court)
            {
                Courts.Remove(court);
            }
        }

        /// <summary>
        /// 保存司法体系
        /// </summary>
        private async void SaveJudicialSystem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SelectedJudicialSystem == null) return;

                // 验证输入
                if (string.IsNullOrWhiteSpace(JudicialNameTextBox.Text))
                {
                    MessageBox.Show("请输入司法体系名称", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(JurisdictionTextBox.Text))
                {
                    MessageBox.Show("请输入司法管辖区", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 更新司法体系信息
                SelectedJudicialSystem.Name = JudicialNameTextBox.Text.Trim();
                SelectedJudicialSystem.Description = JudicialDescriptionTextBox.Text.Trim();
                SelectedJudicialSystem.Jurisdiction = JurisdictionTextBox.Text.Trim();
                SelectedJudicialSystem.DimensionId = DimensionIdTextBox.Text.Trim();
                SelectedJudicialSystem.LegalSystemType = (LegalSystemTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "成文法系";
                SelectedJudicialSystem.CourtCount = Courts.Count;
                SelectedJudicialSystem.Courts = Courts.ToList();

                // 如果是新建司法体系，添加到列表
                if (SelectedJudicialSystem.Id == 0)
                {
                    SelectedJudicialSystem.Id = JudicialSystems.Count > 0 ? JudicialSystems.Max(s => s.Id) + 1 : 1;
                    JudicialSystems.Add(SelectedJudicialSystem);
                }

                await PersistJudicialSystemsAsync();
                MessageBox.Show("司法体系保存成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);

                // 刷新列表
                FilterJudicialSystems();
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
                if (!EnsureCurrentProject("AI司法体系"))
                {
                    return;
                }

                if (_aiAssistantService == null)
                {
                    ShowAIAssistantDialog();
                    return;
                }

                if (SelectedJudicialSystem != null)
                {
                    var choice = MessageBox.Show(
                        "是：AI优化当前司法体系并保存\n否：AI生成新的司法体系并保存\n取消：打开原始AI助手",
                        "AI司法体系",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);

                    if (choice == MessageBoxResult.Cancel)
                    {
                        ShowAIAssistantDialog();
                        return;
                    }

                    await GenerateJudicialSystemWithAiAsync(optimizeCurrent: choice == MessageBoxResult.Yes);
                    return;
                }

                var generateChoice = MessageBox.Show(
                    "是：AI生成新的司法体系并保存\n否：打开原始AI助手",
                    "AI司法体系",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (generateChoice == MessageBoxResult.Yes)
                {
                    await GenerateJudicialSystemWithAiAsync(optimizeCurrent: false);
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
            var dialog = new AIAssistantDialog("司法体系管理", contextString);
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
                ["interfaceType"] = "司法体系管理",
                ["totalJudicialSystems"] = JudicialSystems.Count,
                ["selectedJudicialSystem"] = SelectedJudicialSystem,
                ["legalSystemTypes"] = new[] { "成文法系", "判例法系", "宗教法系", "习惯法系", "混合法系" },
                ["courtLevels"] = new[] { "最高法院", "高级法院", "中级法院", "基层法院", "专门法院" }
            };

            if (SelectedJudicialSystem != null)
            {
                context["currentSystemName"] = SelectedJudicialSystem.Name;
                context["currentSystemType"] = SelectedJudicialSystem.LegalSystemType;
                context["currentSystemJurisdiction"] = SelectedJudicialSystem.Jurisdiction;
                context["currentSystemDimension"] = SelectedJudicialSystem.DimensionId;
                context["currentSystemDescription"] = SelectedJudicialSystem.Description;
                context["currentSystemCourtCount"] = SelectedJudicialSystem.CourtCount;
                context["currentSystemCourts"] = Courts.ToList();
            }

            // 添加统计信息
            var typeStats = JudicialSystems.GroupBy(s => s.LegalSystemType)
                .ToDictionary(g => g.Key, g => g.Count());
            context["typeStatistics"] = typeStats;

            var jurisdictionStats = JudicialSystems.GroupBy(s => s.Jurisdiction)
                .ToDictionary(g => g.Key, g => g.Count());
            context["jurisdictionStatistics"] = jurisdictionStats;

            return context;
        }

        /// <summary>
        /// 在项目切换后刷新司法体系数据。
        /// </summary>
        /// <param name="projectId">当前项目标识。</param>
        /// <param name="projectName">当前项目名称。</param>
        public async Task RefreshOnProjectChangedAsync(Guid? projectId, string? projectName)
        {
            _currentProjectId = projectId ?? Guid.Empty;
            await LoadJudicialSystemsAsync();
        }

        /// <summary>
        /// 在导航到当前视图时刷新对应项目的司法体系数据。
        /// </summary>
        /// <param name="context">导航上下文。</param>
        public void OnNavigatedTo(NavigationContext context)
        {
            _currentProjectId = context.ProjectId ?? Guid.Empty;
            _ = LoadJudicialSystemsAsync();
        }

        private async Task PersistJudicialSystemsAsync()
        {
            if (_currentProjectId == Guid.Empty || _judicialDataService == null)
            {
                return;
            }

            foreach (var system in JudicialSystems)
            {
                system.Courts ??= new List<CourtViewModel>();
                system.CourtCount = system.Courts.Count;
            }

            await _judicialDataService.SaveJudicialSystemsAsync(_currentProjectId, JudicialSystems);
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

        private async Task GenerateJudicialSystemWithAiAsync(bool optimizeCurrent)
        {
            if (_aiAssistantService == null)
            {
                MessageBox.Show("AI助手服务未初始化。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var parameters = new Dictionary<string, object>
            {
                ["title"] = optimizeCurrent && SelectedJudicialSystem != null ? $"优化司法体系：{SelectedJudicialSystem.Name}" : "生成司法体系",
                ["theme"] = "请生成一个适合小说项目使用的司法体系，并输出名称、法律体系类型、管辖区、维度、描述、法院列表。",
                ["requirements"] = optimizeCurrent && SelectedJudicialSystem != null
                    ? $"请基于当前司法体系进行优化并输出结构化文本。当前体系：{SelectedJudicialSystem.Name}，类型：{SelectedJudicialSystem.LegalSystemType}，管辖区：{SelectedJudicialSystem.Jurisdiction}，描述：{SelectedJudicialSystem.Description}"
                    : "请输出一个完整司法体系，至少包含名称、法律体系类型、管辖区、维度、描述、至少3个法院。",
                ["context"] = GetCurrentContext()
            };

            var result = await _aiAssistantService.GenerateOutlineAsync(parameters);
            if (!result.IsSuccess || result.Data == null)
            {
                MessageBox.Show(result.Message ?? "AI生成失败。", "AI司法体系", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var generatedSystem = ParseJudicialSystemFromAiResult(result.Data, optimizeCurrent ? SelectedJudicialSystem : null);
            if (generatedSystem == null)
            {
                MessageBox.Show("AI结果无法解析为司法体系。", "AI司法体系", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (optimizeCurrent && SelectedJudicialSystem != null)
            {
                generatedSystem.Id = SelectedJudicialSystem.Id;
                generatedSystem.CreatedAt = SelectedJudicialSystem.CreatedAt;
                var index = JudicialSystems.IndexOf(SelectedJudicialSystem);
                if (index >= 0)
                {
                    JudicialSystems[index] = generatedSystem;
                }
                SelectedJudicialSystem = generatedSystem;
            }
            else
            {
                generatedSystem.Id = JudicialSystems.Count > 0 ? JudicialSystems.Max(s => s.Id) + 1 : 1;
                generatedSystem.CreatedAt = DateTime.Now;
                JudicialSystems.Add(generatedSystem);
                SelectedJudicialSystem = generatedSystem;
            }

            await PersistJudicialSystemsAsync();
            FilterJudicialSystems();
            UpdateStatistics();
            LoadJudicialSystemDetails(generatedSystem);
            ShowEditPanel();
            MessageBox.Show(
                optimizeCurrent ? $"已使用 AI 优化并保存司法体系：{generatedSystem.Name}" : $"已使用 AI 生成并保存司法体系：{generatedSystem.Name}",
                "AI司法体系",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private JudicialSystemViewModel? ParseJudicialSystemFromAiResult(object data, JudicialSystemViewModel? baseSystem)
        {
            var text = data?.ToString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var system = new JudicialSystemViewModel
            {
                Id = baseSystem?.Id ?? 0,
                Name = ExtractField(text, "名称") ?? baseSystem?.Name ?? ExtractFirstMeaningfulLine(text) ?? "AI生成司法体系",
                LegalSystemType = ExtractField(text, "法律体系类型") ?? baseSystem?.LegalSystemType ?? "成文法系",
                Jurisdiction = ExtractField(text, "管辖区") ?? baseSystem?.Jurisdiction ?? "全域",
                DimensionId = ExtractField(text, "维度") ?? baseSystem?.DimensionId ?? "DIM-AI",
                Description = ExtractField(text, "描述") ?? baseSystem?.Description ?? text.Trim(),
                CreatedAt = baseSystem?.CreatedAt ?? DateTime.Now
            };

            system.Courts = ParseCourts(text);
            if (system.Courts.Count == 0)
            {
                system.Courts = baseSystem?.Courts?.ToList() ?? new List<CourtViewModel>
                {
                    new() { Name = "最高法院", Level = "最高法院", Jurisdiction = "全域" },
                    new() { Name = "高级法院", Level = "高级法院", Jurisdiction = "主要区域" },
                    new() { Name = "基层法院", Level = "基层法院", Jurisdiction = "地方辖区" }
                };
            }

            system.CourtCount = system.Courts.Count;
            return system;
        }

        private static List<CourtViewModel> ParseCourts(string text)
        {
            var courts = new List<CourtViewModel>();
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (!Regex.IsMatch(line, "法院|审判庭|法庭"))
                {
                    continue;
                }

                courts.Add(new CourtViewModel
                {
                    Name = TrimListMarker(line),
                    Level = InferCourtLevel(line),
                    Jurisdiction = "待定"
                });

                if (courts.Count >= 8)
                {
                    break;
                }
            }

            return courts;
        }

        private static string InferCourtLevel(string line)
        {
            if (line.Contains("最高"))
            {
                return "最高法院";
            }
            if (line.Contains("高级"))
            {
                return "高级法院";
            }
            if (line.Contains("中级"))
            {
                return "中级法院";
            }
            if (line.Contains("专门"))
            {
                return "专门法院";
            }

            return "基层法院";
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
    /// 司法体系视图模型
    /// </summary>
    public class JudicialSystemViewModel
    {
        /// <summary>
        /// 司法体系标识。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 司法体系名称。
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// 法律体系类型。
        /// </summary>
        public string LegalSystemType { get; set; } = "";

        /// <summary>
        /// 管辖范围。
        /// </summary>
        public string Jurisdiction { get; set; } = "";

        /// <summary>
        /// 所属维度标识。
        /// </summary>
        public string DimensionId { get; set; } = "";

        /// <summary>
        /// 司法体系描述。
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// 法院数量。
        /// </summary>
        public int CourtCount { get; set; }

        /// <summary>
        /// 创建时间。
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 法院列表。
        /// </summary>
        public List<CourtViewModel> Courts { get; set; } = new();
    }

    /// <summary>
    /// 法院视图模型
    /// </summary>
    public class CourtViewModel
    {
        /// <summary>
        /// 法院名称。
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// 法院层级。
        /// </summary>
        public string Level { get; set; } = "";

        /// <summary>
        /// 法院管辖范围。
        /// </summary>
        public string Jurisdiction { get; set; } = "";
    }

    #endregion
}
