using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using NovelManagement.WPF.Commands;
using NovelManagement.WPF.Services;

namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// 职业体系管理视图
    /// </summary>
    public partial class ProfessionSystemView : UserControl, INavigationRefreshableView, INavigationAwareView
    {
        #region 属性

        /// <summary>
        /// 职业体系列表
        /// </summary>
        public ObservableCollection<ProfessionSystemViewModel> ProfessionSystems { get; set; }

        /// <summary>
        /// 当前选中的职业体系
        /// </summary>
        public ProfessionSystemViewModel SelectedProfession { get; set; }

        /// <summary>
        /// 职业等级列表
        /// </summary>
        public ObservableCollection<ProfessionLevelViewModel> ProfessionLevels { get; set; }

        /// <summary>
        /// 选择职业命令
        /// </summary>
        public ICommand SelectProfessionCommand { get; set; }

        private readonly IAIAssistantService? _aiAssistantService;
        private readonly ProfessionDataService? _professionDataService;
        private readonly ProjectContextService? _projectContextService;
        private readonly CurrentProjectGuard? _currentProjectGuard;
        private Guid _currentProjectId;

        /// <summary>
        /// 获取当前职业体系总数。
        /// </summary>
        public int TotalCount => ProfessionSystems.Count;

        /// <summary>
        /// 获取修炼职业数量。
        /// </summary>
        public int CultivationCount => ProfessionSystems.Count(p => p.Category == "修炼职业");

        /// <summary>
        /// 获取生活职业数量。
        /// </summary>
        public int LifeCount => ProfessionSystems.Count(p => p.Category == "生活职业");

        /// <summary>
        /// 获取战斗职业数量。
        /// </summary>
        public int CombatCount => ProfessionSystems.Count(p => p.Category == "战斗职业");

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化职业体系管理视图。
        /// </summary>
        public ProfessionSystemView()
        {
            InitializeComponent();
            _aiAssistantService = App.ServiceProvider?.GetService<IAIAssistantService>();
            _professionDataService = App.ServiceProvider?.GetService<ProfessionDataService>();
            _projectContextService = App.ServiceProvider?.GetService<ProjectContextService>();
            _currentProjectGuard = App.ServiceProvider?.GetService<CurrentProjectGuard>();
            InitializeData();
            InitializeCommands();
            _ = LoadProfessionSystemsAsync();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化数据
        /// </summary>
        private void InitializeData()
        {
            ProfessionSystems = new ObservableCollection<ProfessionSystemViewModel>();
            ProfessionLevels = new ObservableCollection<ProfessionLevelViewModel>();
            
            // 设置数据上下文
            this.DataContext = this;
        }

        /// <summary>
        /// 初始化命令
        /// </summary>
        private void InitializeCommands()
        {
            SelectProfessionCommand = new RelayCommand<ProfessionSystemViewModel>(SelectProfession);
        }

        #endregion

        #region 数据加载

        /// <summary>
        /// 加载职业体系数据
        /// </summary>
        private async Task LoadProfessionSystemsAsync()
        {
            try
            {
                _currentProjectId = _projectContextService?.CurrentProjectId ?? Guid.Empty;
                if (_currentProjectId == Guid.Empty)
                {
                    _currentProjectGuard?.TryGetCurrentProjectId(Window.GetWindow(this), "职业体系管理", out _);
                    ProfessionSystems.Clear();
                    ProfessionListControl.ItemsSource = ProfessionSystems;
                    UpdateStatistics();
                    HideEditPanel();
                    return;
                }

                var professions = _professionDataService == null
                    ? new List<ProfessionSystemViewModel>()
                    : await _professionDataService.LoadProfessionSystemsAsync(_currentProjectId);

                ProfessionSystems.Clear();
                foreach (var profession in professions)
                {
                    profession.Levels ??= new List<ProfessionLevelViewModel>();
                    profession.LevelCount = profession.Levels.Count;
                    ProfessionSystems.Add(profession);
                }

                // 设置列表数据源
                ProfessionListControl.ItemsSource = ProfessionSystems;

                // 更新统计信息
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载职业体系数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 更新统计信息
        /// </summary>
        private void UpdateStatistics()
        {
            ProfessionListControl.Items.Refresh();
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
            FilterProfessionSystems();
        }

        /// <summary>
        /// 类别筛选变化
        /// </summary>
        private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterProfessionSystems();
        }

        /// <summary>
        /// 筛选职业体系
        /// </summary>
        private void FilterProfessionSystems()
        {
            var searchText = SearchTextBox?.Text?.ToLower() ?? "";
            var selectedCategory = (CategoryFilterComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString();

            var filteredProfessions = ProfessionSystems.Where(p =>
                (string.IsNullOrEmpty(searchText) || 
                 p.Name.ToLower().Contains(searchText) || 
                 p.Description.ToLower().Contains(searchText)) &&
                (selectedCategory == "全部类别" || selectedCategory == null || p.Category == selectedCategory)
            ).ToList();

            ProfessionListControl.ItemsSource = filteredProfessions;
        }

        /// <summary>
        /// 选择职业体系
        /// </summary>
        private void SelectProfession(ProfessionSystemViewModel profession)
        {
            if (profession == null) return;

            SelectedProfession = profession;
            LoadProfessionDetails(profession);
            ShowEditPanel();
        }

        /// <summary>
        /// 加载职业详情
        /// </summary>
        private void LoadProfessionDetails(ProfessionSystemViewModel profession)
        {
            // 填充基本信息
            ProfessionNameTextBox.Text = profession.Name;
            ProfessionDescriptionTextBox.Text = profession.Description;
            
            // 设置类别选择
            foreach (ComboBoxItem item in ProfessionCategoryComboBox.Items)
            {
                if (item.Content.ToString() == profession.Category)
                {
                    ProfessionCategoryComboBox.SelectedItem = item;
                    break;
                }
            }

            // 加载职业等级
            LoadProfessionLevels(profession);
        }

        /// <summary>
        /// 加载职业等级
        /// </summary>
        private void LoadProfessionLevels(ProfessionSystemViewModel profession)
        {
            ProfessionLevels.Clear();
            foreach (var level in profession.Levels.OrderBy(l => l.Level))
            {
                ProfessionLevels.Add(level);
            }

            LevelListControl.ItemsSource = ProfessionLevels;
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
            SelectedProfession = null;
        }

        #endregion

        #region 按钮事件

        /// <summary>
        /// 新建职业体系
        /// </summary>
        private void AddProfession_Click(object sender, RoutedEventArgs e)
        {
            var newProfession = new ProfessionSystemViewModel
            {
                Id = 0,
                Name = "",
                Category = "修炼职业",
                Description = "",
                LevelCount = 0,
                SkillCount = 0,
                CreatedAt = DateTime.Now
            };

            SelectedProfession = newProfession;
            LoadProfessionDetails(newProfession);
            ShowEditPanel();

            // 清空等级列表
            ProfessionLevels.Clear();
            LevelListControl.ItemsSource = ProfessionLevels;

            // 聚焦到名称输入框
            ProfessionNameTextBox.Focus();
        }

        /// <summary>
        /// 导入职业数据
        /// </summary>
        private async void ImportProfession_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!EnsureCurrentProject("导入职业体系"))
                {
                    return;
                }

                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "导入职业体系数据",
                    Filter = "JSON文件|*.json|所有文件|*.*",
                    DefaultExt = "json"
                };

                if (dialog.ShowDialog() == true)
                {
                    if (_professionDataService == null)
                    {
                        MessageBox.Show("职业数据服务未初始化。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var importedProfessions = await _professionDataService.ImportProfessionSystemsAsync(dialog.FileName);
                    ProfessionSystems.Clear();
                    foreach (var profession in importedProfessions)
                    {
                        profession.Levels ??= new List<ProfessionLevelViewModel>();
                        profession.LevelCount = profession.Levels.Count;
                        ProfessionSystems.Add(profession);
                    }

                    await PersistProfessionSystemsAsync();
                    FilterProfessionSystems();
                    UpdateStatistics();
                    MessageBox.Show($"已成功导入 {ProfessionSystems.Count} 个职业体系。", "导入成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 导出职业数据
        /// </summary>
        private async void ExportProfession_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!EnsureCurrentProject("导出职业体系"))
                {
                    return;
                }

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "导出职业体系数据",
                    Filter = "JSON文件|*.json",
                    DefaultExt = "json",
                    FileName = $"职业体系数据_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (dialog.ShowDialog() == true)
                {
                    if (_professionDataService == null)
                    {
                        MessageBox.Show("职业数据服务未初始化。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    await _professionDataService.ExportProfessionSystemsAsync(_currentProjectId, ProfessionSystems, dialog.FileName);
                    MessageBox.Show($"职业体系数据已导出到：{dialog.FileName}", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// AI助手按钮点击事件
        /// </summary>
        private async void AIAssistant_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!EnsureCurrentProject("AI职业体系"))
                {
                    return;
                }

                if (_aiAssistantService == null)
                {
                    ShowAIAssistantDialog();
                    return;
                }

                if (SelectedProfession != null)
                {
                    var choice = MessageBox.Show(
                        "是：AI优化当前职业并保存\n否：AI生成新的职业并保存\n取消：打开原始AI助手",
                        "AI职业体系",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);

                    if (choice == MessageBoxResult.Cancel)
                    {
                        ShowAIAssistantDialog();
                        return;
                    }

                    await GenerateProfessionWithAiAsync(optimizeCurrent: choice == MessageBoxResult.Yes);
                    return;
                }

                var generateChoice = MessageBox.Show(
                    "是：AI生成新的职业并保存\n否：打开原始AI助手",
                    "AI职业体系",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (generateChoice == MessageBoxResult.Yes)
                {
                    await GenerateProfessionWithAiAsync(optimizeCurrent: false);
                }
                else
                {
                    ShowAIAssistantDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动AI助手失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 显示AI助手对话框
        /// </summary>
        private void ShowAIAssistantDialog()
        {
            var dialog = new AIAssistantDialog("职业体系管理", GetCurrentContext());
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();
        }

        /// <summary>
        /// 获取当前上下文信息
        /// </summary>
        private string GetCurrentContext()
        {
            var context = new StringBuilder();
            context.AppendLine("当前功能：职业体系管理");
            context.AppendLine($"职业总数：{ProfessionSystems.Count}");

            if (SelectedProfession != null)
            {
                context.AppendLine($"当前选中：{SelectedProfession.Name}");
                context.AppendLine($"职业类型：{SelectedProfession.Category}");
                context.AppendLine($"等级数量：{SelectedProfession.LevelCount}");
            }

            context.AppendLine("\n可用操作：");
            context.AppendLine("- 创建新的职业体系");
            context.AppendLine("- 编辑现有职业属性");
            context.AppendLine("- 设计职业等级体系");
            context.AppendLine("- 添加职业技能");
            context.AppendLine("- 分析职业平衡性");

            return context.ToString();
        }

        /// <summary>
        /// 添加等级
        /// </summary>
        private void AddLevel_Click(object sender, RoutedEventArgs e)
        {
            var newLevel = new ProfessionLevelViewModel
            {
                Level = ProfessionLevels.Count + 1,
                Name = "",
                Requirements = ""
            };

            ProfessionLevels.Add(newLevel);
        }

        /// <summary>
        /// 删除等级
        /// </summary>
        private void RemoveLevel_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is ProfessionLevelViewModel level)
            {
                ProfessionLevels.Remove(level);
                
                // 重新排序等级
                for (int i = 0; i < ProfessionLevels.Count; i++)
                {
                    ProfessionLevels[i].Level = i + 1;
                }
            }
        }

        /// <summary>
        /// 保存职业体系
        /// </summary>
        private async void SaveProfession_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SelectedProfession == null) return;

                // 验证输入
                if (string.IsNullOrWhiteSpace(ProfessionNameTextBox.Text))
                {
                    MessageBox.Show("请输入职业体系名称", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 更新职业信息
                SelectedProfession.Name = ProfessionNameTextBox.Text.Trim();
                SelectedProfession.Description = ProfessionDescriptionTextBox.Text.Trim();
                SelectedProfession.Category = (ProfessionCategoryComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "修炼职业";
                SelectedProfession.LevelCount = ProfessionLevels.Count;
                SelectedProfession.Levels = ProfessionLevels.OrderBy(l => l.Level).ToList();
                SelectedProfession.SkillCount = Math.Max(SelectedProfession.SkillCount, SelectedProfession.Levels.Count * 2);

                // 如果是新建职业，添加到列表
                if (SelectedProfession.Id == 0)
                {
                    SelectedProfession.Id = ProfessionSystems.Count > 0 ? ProfessionSystems.Max(p => p.Id) + 1 : 1;
                    ProfessionSystems.Add(SelectedProfession);
                }

                await PersistProfessionSystemsAsync();
                MessageBox.Show("职业体系保存成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);

                // 刷新列表
                FilterProfessionSystems();
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

        /// <summary>
        /// 在项目切换后刷新职业体系数据。
        /// </summary>
        /// <param name="projectId">当前项目标识。</param>
        /// <param name="projectName">当前项目名称。</param>
        public async Task RefreshOnProjectChangedAsync(Guid? projectId, string? projectName)
        {
            _currentProjectId = projectId ?? Guid.Empty;
            await LoadProfessionSystemsAsync();
        }

        /// <summary>
        /// 在导航到当前视图时刷新对应项目的职业体系数据。
        /// </summary>
        /// <param name="context">导航上下文。</param>
        public void OnNavigatedTo(NavigationContext context)
        {
            _currentProjectId = context.ProjectId ?? Guid.Empty;
            _ = LoadProfessionSystemsAsync();
        }

        private async Task PersistProfessionSystemsAsync()
        {
            if (_currentProjectId == Guid.Empty || _professionDataService == null)
            {
                return;
            }

            foreach (var profession in ProfessionSystems)
            {
                profession.Levels ??= new List<ProfessionLevelViewModel>();
                profession.LevelCount = profession.Levels.Count;
            }

            await _professionDataService.SaveProfessionSystemsAsync(_currentProjectId, ProfessionSystems);
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

        private async Task GenerateProfessionWithAiAsync(bool optimizeCurrent)
        {
            if (_aiAssistantService == null)
            {
                MessageBox.Show("AI助手服务未初始化。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var parameters = new Dictionary<string, object>
            {
                ["title"] = optimizeCurrent && SelectedProfession != null ? $"优化职业体系：{SelectedProfession.Name}" : "生成职业体系",
                ["theme"] = "请生成一个适合小说项目使用的职业体系，并输出名称、类别、描述、等级列表和核心技能方向。",
                ["requirements"] = optimizeCurrent && SelectedProfession != null
                    ? $"请基于当前职业体系进行优化并输出结构化文本。当前职业：{SelectedProfession.Name}，类别：{SelectedProfession.Category}，描述：{SelectedProfession.Description}"
                    : "请输出一个完整职业体系，至少包含名称、类别、描述、至少3个等级。",
                ["context"] = GetCurrentContext()
            };

            var result = await _aiAssistantService.GenerateOutlineAsync(parameters);
            if (!result.IsSuccess || result.Data == null)
            {
                MessageBox.Show(result.Message ?? "AI生成失败。", "AI职业体系", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var generatedProfession = ParseProfessionFromAiResult(result.Data, optimizeCurrent ? SelectedProfession : null);
            if (generatedProfession == null)
            {
                MessageBox.Show("AI结果无法解析为职业体系。", "AI职业体系", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (optimizeCurrent && SelectedProfession != null)
            {
                generatedProfession.Id = SelectedProfession.Id;
                generatedProfession.CreatedAt = SelectedProfession.CreatedAt;
                var index = ProfessionSystems.IndexOf(SelectedProfession);
                if (index >= 0)
                {
                    ProfessionSystems[index] = generatedProfession;
                }
                SelectedProfession = generatedProfession;
            }
            else
            {
                generatedProfession.Id = ProfessionSystems.Count > 0 ? ProfessionSystems.Max(p => p.Id) + 1 : 1;
                generatedProfession.CreatedAt = DateTime.Now;
                ProfessionSystems.Add(generatedProfession);
                SelectedProfession = generatedProfession;
            }

            await PersistProfessionSystemsAsync();
            FilterProfessionSystems();
            UpdateStatistics();
            LoadProfessionDetails(generatedProfession);
            ShowEditPanel();
            MessageBox.Show(
                optimizeCurrent ? $"已使用 AI 优化并保存职业：{generatedProfession.Name}" : $"已使用 AI 生成并保存职业：{generatedProfession.Name}",
                "AI职业体系",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private ProfessionSystemViewModel? ParseProfessionFromAiResult(object data, ProfessionSystemViewModel? baseProfession)
        {
            var text = data?.ToString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var profession = new ProfessionSystemViewModel
            {
                Id = baseProfession?.Id ?? 0,
                Name = ExtractField(text, "名称") ?? baseProfession?.Name ?? ExtractFirstMeaningfulLine(text) ?? "AI生成职业体系",
                Category = ExtractField(text, "类别") ?? baseProfession?.Category ?? "修炼职业",
                Description = ExtractField(text, "描述") ?? baseProfession?.Description ?? text.Trim(),
                CreatedAt = baseProfession?.CreatedAt ?? DateTime.Now
            };

            profession.Levels = ParseProfessionLevels(text);
            if (profession.Levels.Count == 0)
            {
                profession.Levels = baseProfession?.Levels?.ToList() ?? new List<ProfessionLevelViewModel>
                {
                    new() { Level = 1, Name = "入门", Requirements = "基础条件" },
                    new() { Level = 2, Name = "进阶", Requirements = "完成初阶训练" },
                    new() { Level = 3, Name = "精通", Requirements = "积累足够经验" }
                };
            }

            profession.LevelCount = profession.Levels.Count;
            profession.SkillCount = Math.Max(baseProfession?.SkillCount ?? 0, profession.LevelCount * 2);
            return profession;
        }

        private static List<ProfessionLevelViewModel> ParseProfessionLevels(string text)
        {
            var levels = new List<ProfessionLevelViewModel>();
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var index = 1;
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (!Regex.IsMatch(line, "等级|级|阶|境|学徒|入门|精通|宗师"))
                {
                    continue;
                }

                levels.Add(new ProfessionLevelViewModel
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
    /// 职业体系视图模型
    /// </summary>
    public class ProfessionSystemViewModel
    {
        /// <summary>
        /// 职业体系标识。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 职业体系名称。
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// 职业类别。
        /// </summary>
        public string Category { get; set; } = "";

        /// <summary>
        /// 职业描述。
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// 等级数量。
        /// </summary>
        public int LevelCount { get; set; }

        /// <summary>
        /// 技能数量。
        /// </summary>
        public int SkillCount { get; set; }

        /// <summary>
        /// 创建时间。
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 职业等级列表。
        /// </summary>
        public List<ProfessionLevelViewModel> Levels { get; set; } = new();
    }

    /// <summary>
    /// 职业等级视图模型
    /// </summary>
    public class ProfessionLevelViewModel
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
        /// 等级要求。
        /// </summary>
        public string Requirements { get; set; } = "";
    }

    #endregion
}
