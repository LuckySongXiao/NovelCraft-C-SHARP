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
    /// 功法体系管理视图
    /// </summary>
    public partial class TechniqueSystemView : UserControl, INavigationRefreshableView, INavigationAwareView
    {
        #region 属性

        /// <summary>
        /// 功法体系列表
        /// </summary>
        public ObservableCollection<TechniqueSystemViewModel> TechniqueSystems { get; set; }

        /// <summary>
        /// 功法等级列表
        /// </summary>
        public ObservableCollection<TechniqueLevelViewModel> TechniqueLevels { get; set; }

        /// <summary>
        /// 功法招式列表
        /// </summary>
        public ObservableCollection<TechniqueMoveViewModel> TechniqueMoves { get; set; }

        /// <summary>
        /// 当前选中的功法体系
        /// </summary>
        public TechniqueSystemViewModel SelectedTechnique { get; set; }

        /// <summary>
        /// 选择功法体系命令
        /// </summary>
        public ICommand SelectTechniqueCommand { get; private set; }

        /// <summary>
        /// 获取当前功法体系总数。
        /// </summary>
        public int TotalCount => TechniqueSystems.Count;

        /// <summary>
        /// 获取修炼功法数量。
        /// </summary>
        public int CultivationCount => TechniqueSystems.Count(t => t.Category == "修炼功法");

        /// <summary>
        /// 获取攻击功法数量。
        /// </summary>
        public int AttackCount => TechniqueSystems.Count(t => t.Category == "攻击功法");

        /// <summary>
        /// 获取防御功法数量。
        /// </summary>
        public int DefenseCount => TechniqueSystems.Count(t => t.Category == "防御功法");

        private readonly IAIAssistantService? _aiAssistantService;
        private readonly TechniqueDataService? _techniqueDataService;
        private readonly ProjectContextService? _projectContextService;
        private readonly CurrentProjectGuard? _currentProjectGuard;
        private Guid _currentProjectId;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化功法体系管理视图。
        /// </summary>
        public TechniqueSystemView()
        {
            InitializeComponent();
            _aiAssistantService = App.ServiceProvider?.GetService<IAIAssistantService>();
            _techniqueDataService = App.ServiceProvider?.GetService<TechniqueDataService>();
            _projectContextService = App.ServiceProvider?.GetService<ProjectContextService>();
            _currentProjectGuard = App.ServiceProvider?.GetService<CurrentProjectGuard>();
            InitializeData();
            InitializeCommands();
            _ = LoadTechniqueSystemsAsync();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化数据
        /// </summary>
        private void InitializeData()
        {
            TechniqueSystems = new ObservableCollection<TechniqueSystemViewModel>();
            TechniqueLevels = new ObservableCollection<TechniqueLevelViewModel>();
            TechniqueMoves = new ObservableCollection<TechniqueMoveViewModel>();

            // 设置数据上下文
            this.DataContext = this;
        }

        /// <summary>
        /// 初始化命令
        /// </summary>
        private void InitializeCommands()
        {
            SelectTechniqueCommand = new RelayCommand<TechniqueSystemViewModel>(SelectTechniqueSystem);
        }

        /// <summary>
        /// 加载功法体系数据
        /// </summary>
        private async Task LoadTechniqueSystemsAsync()
        {
            try
            {
                _currentProjectId = _projectContextService?.CurrentProjectId ?? Guid.Empty;
                if (_currentProjectId == Guid.Empty)
                {
                    _currentProjectGuard?.TryGetCurrentProjectId(Window.GetWindow(this), "功法体系管理", out _);
                    TechniqueSystems.Clear();
                    TechniqueListControl.ItemsSource = TechniqueSystems;
                    UpdateStatistics();
                    HideEditPanel();
                    return;
                }

                var techniques = _techniqueDataService == null
                    ? new List<TechniqueSystemViewModel>()
                    : await _techniqueDataService.LoadTechniqueSystemsAsync(_currentProjectId);

                TechniqueSystems.Clear();
                foreach (var technique in techniques)
                {
                    technique.LevelCount = technique.Levels.Count;
                    technique.MoveCount = technique.Moves.Count;
                    TechniqueSystems.Add(technique);
                }

                // 设置列表数据源
                TechniqueListControl.ItemsSource = TechniqueSystems;

                // 更新统计信息
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载功法体系数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 更新统计信息
        /// </summary>
        private void UpdateStatistics()
        {
            TechniqueListControl.Items.Refresh();
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
            FilterTechniqueSystems();
        }

        /// <summary>
        /// 类别筛选变化事件
        /// </summary>
        private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterTechniqueSystems();
        }

        /// <summary>
        /// 筛选功法体系
        /// </summary>
        private void FilterTechniqueSystems()
        {
            var searchText = SearchTextBox?.Text?.ToLower() ?? "";
            var selectedCategory = (CategoryFilterComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString();

            var filteredTechniques = TechniqueSystems.Where(t =>
                (string.IsNullOrEmpty(searchText) || 
                 t.Name.ToLower().Contains(searchText) || 
                 t.Description.ToLower().Contains(searchText)) &&
                (selectedCategory == "全部类别" || selectedCategory == null || t.Category == selectedCategory)
            ).ToList();

            TechniqueListControl.ItemsSource = filteredTechniques;
        }

        #endregion

        #region 功法体系管理

        /// <summary>
        /// 添加功法体系
        /// </summary>
        private void AddTechnique_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 创建新的功法体系
                SelectedTechnique = new TechniqueSystemViewModel
                {
                    Id = 0, // 新建时ID为0
                    Name = "",
                    Category = "修炼功法",
                    Grade = "凡级",
                    Origin = "",
                    Description = "",
                    CreatedAt = DateTime.Now
                };

                // 清空等级和招式列表
                TechniqueLevels.Clear();
                TechniqueMoves.Clear();

                // 显示编辑面板
                ShowEditPanel();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"创建功法体系失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 导入功法数据
        /// </summary>
        private async void ImportTechnique_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!EnsureCurrentProject("导入功法体系"))
                {
                    return;
                }

                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "导入功法体系数据",
                    Filter = "JSON文件|*.json|所有文件|*.*",
                    DefaultExt = "json"
                };

                if (dialog.ShowDialog() == true)
                {
                    if (_techniqueDataService == null)
                    {
                        MessageBox.Show("功法数据服务未初始化。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var importedTechniques = await _techniqueDataService.ImportTechniqueSystemsAsync(dialog.FileName);
                    TechniqueSystems.Clear();
                    foreach (var technique in importedTechniques)
                    {
                        technique.LevelCount = technique.Levels.Count;
                        technique.MoveCount = technique.Moves.Count;
                        TechniqueSystems.Add(technique);
                    }

                    await PersistTechniqueSystemsAsync();
                    FilterTechniqueSystems();
                    UpdateStatistics();
                    MessageBox.Show($"已成功导入 {TechniqueSystems.Count} 个功法体系。", "导入成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 导出功法数据
        /// </summary>
        private async void ExportTechnique_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!EnsureCurrentProject("导出功法体系"))
                {
                    return;
                }

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "导出功法体系数据",
                    Filter = "JSON文件|*.json",
                    DefaultExt = "json",
                    FileName = $"功法体系数据_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (dialog.ShowDialog() == true)
                {
                    if (_techniqueDataService == null)
                    {
                        MessageBox.Show("功法数据服务未初始化。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    await _techniqueDataService.ExportTechniqueSystemsAsync(_currentProjectId, TechniqueSystems, dialog.FileName);
                    MessageBox.Show($"功法体系数据已导出到：{dialog.FileName}",
                        "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
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
                if (!EnsureCurrentProject("AI功法体系"))
                {
                    return;
                }

                if (_aiAssistantService == null)
                {
                    ShowAIAssistantDialog();
                    return;
                }

                if (SelectedTechnique != null)
                {
                    var choice = MessageBox.Show(
                        "是：AI优化当前功法并保存\n否：AI生成新的功法并保存\n取消：打开原始AI助手",
                        "AI功法体系",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);

                    if (choice == MessageBoxResult.Cancel)
                    {
                        ShowAIAssistantDialog();
                        return;
                    }

                    await GenerateTechniqueWithAiAsync(optimizeCurrent: choice == MessageBoxResult.Yes);
                    return;
                }

                var generateChoice = MessageBox.Show(
                    "是：AI生成新的功法并保存\n否：打开原始AI助手",
                    "AI功法体系",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (generateChoice == MessageBoxResult.Yes)
                {
                    await GenerateTechniqueWithAiAsync(optimizeCurrent: false);
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
            var context = GetCurrentContext();
            var contextString = System.Text.Json.JsonSerializer.Serialize(context, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            var dialog = new AIAssistantDialog("功法体系管理", contextString);
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();
        }

        /// <summary>
        /// 获取当前上下文信息
        /// </summary>
        private string GetCurrentContext()
        {
            var context = new StringBuilder();
            context.AppendLine("当前功能：功法体系管理");
            context.AppendLine($"功法总数：{TechniqueSystems.Count}");

            if (SelectedTechnique != null)
            {
                context.AppendLine($"当前选中：{SelectedTechnique.Name}");
                context.AppendLine($"功法类型：{SelectedTechnique.Category}");
                context.AppendLine($"功法等级：{SelectedTechnique.Grade}");
            }

            context.AppendLine("\n可用操作：");
            context.AppendLine("- 创建新的功法体系");
            context.AppendLine("- 编辑现有功法属性");
            context.AppendLine("- 设计功法等级体系");
            context.AppendLine("- 添加功法招式");
            context.AppendLine("- 分析功法平衡性");

            return context.ToString();
        }

        private async Task GenerateTechniqueWithAiAsync(bool optimizeCurrent)
        {
            if (_aiAssistantService == null)
            {
                MessageBox.Show("AI助手服务未初始化。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var parameters = new Dictionary<string, object>
            {
                ["title"] = optimizeCurrent && SelectedTechnique != null ? $"优化功法体系：{SelectedTechnique.Name}" : "生成功法体系",
                ["theme"] = "请生成一个适合小说项目使用的功法体系，并输出名称、类别、品级、起源、描述、等级列表和招式列表。",
                ["requirements"] = optimizeCurrent && SelectedTechnique != null
                    ? $"请基于当前功法体系进行优化并输出结构化文本：名称、类别、品级、起源、描述、等级列表、招式列表。当前功法：{SelectedTechnique.Name}，类别：{SelectedTechnique.Category}，品级：{SelectedTechnique.Grade}，描述：{SelectedTechnique.Description}"
                    : "请输出一个完整功法体系，包含名称、类别、品级、起源、描述、至少3个等级、至少2个招式。",
                ["context"] = GetCurrentContext()
            };

            var result = await _aiAssistantService.GenerateOutlineAsync(parameters);
            if (!result.IsSuccess || result.Data == null)
            {
                MessageBox.Show(result.Message ?? "AI生成失败。", "AI功法体系", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var generatedTechnique = ParseTechniqueFromAiResult(result.Data, optimizeCurrent ? SelectedTechnique : null);
            if (generatedTechnique == null)
            {
                MessageBox.Show("AI结果无法解析为功法体系。", "AI功法体系", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (optimizeCurrent && SelectedTechnique != null)
            {
                generatedTechnique.Id = SelectedTechnique.Id;
                generatedTechnique.CreatedAt = SelectedTechnique.CreatedAt;
                var index = TechniqueSystems.IndexOf(SelectedTechnique);
                if (index >= 0)
                {
                    TechniqueSystems[index] = generatedTechnique;
                }
                SelectedTechnique = generatedTechnique;
            }
            else
            {
                generatedTechnique.Id = TechniqueSystems.Count > 0 ? TechniqueSystems.Max(t => t.Id) + 1 : 1;
                generatedTechnique.CreatedAt = DateTime.Now;
                TechniqueSystems.Add(generatedTechnique);
                SelectedTechnique = generatedTechnique;
            }

            await PersistTechniqueSystemsAsync();
            FilterTechniqueSystems();
            UpdateStatistics();
            LoadTechniqueSystemDetails(generatedTechnique);
            ShowEditPanel();
            MessageBox.Show(
                optimizeCurrent ? $"已使用 AI 优化并保存功法：{generatedTechnique.Name}" : $"已使用 AI 生成并保存功法：{generatedTechnique.Name}",
                "AI功法体系",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private TechniqueSystemViewModel? ParseTechniqueFromAiResult(object data, TechniqueSystemViewModel? baseTechnique)
        {
            var text = data?.ToString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var technique = new TechniqueSystemViewModel
            {
                Id = baseTechnique?.Id ?? 0,
                Name = ExtractField(text, "名称") ?? baseTechnique?.Name ?? ExtractFirstMeaningfulLine(text) ?? "AI生成功法体系",
                Category = ExtractField(text, "类别") ?? baseTechnique?.Category ?? "修炼功法",
                Grade = ExtractField(text, "品级") ?? baseTechnique?.Grade ?? "凡级",
                Origin = ExtractField(text, "起源") ?? baseTechnique?.Origin ?? "AI生成",
                Description = ExtractField(text, "描述") ?? baseTechnique?.Description ?? text.Trim(),
                CreatedAt = baseTechnique?.CreatedAt ?? DateTime.Now
            };

            technique.Levels = ParseTechniqueLevels(text);
            technique.Moves = ParseTechniqueMoves(text);

            if (technique.Levels.Count == 0)
            {
                technique.Levels = baseTechnique?.Levels?.ToList() ?? new List<TechniqueLevelViewModel>
                {
                    new() { Level = 1, Name = "初窥门径", Requirements = "基础修为" },
                    new() { Level = 2, Name = "登堂入室", Requirements = "进阶修为" },
                    new() { Level = 3, Name = "大成圆满", Requirements = "高阶修为" }
                };
            }

            if (technique.Moves.Count == 0)
            {
                technique.Moves = baseTechnique?.Moves?.ToList() ?? new List<TechniqueMoveViewModel>
                {
                    new() { Name = "基础式", Type = "攻击", Description = "AI生成的基础招式" },
                    new() { Name = "护体式", Type = "防御", Description = "AI生成的护体招式" }
                };
            }

            technique.LevelCount = technique.Levels.Count;
            technique.MoveCount = technique.Moves.Count;
            return technique;
        }

        private static List<TechniqueLevelViewModel> ParseTechniqueLevels(string text)
        {
            var levels = new List<TechniqueLevelViewModel>();
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var levelIndex = 1;
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (!Regex.IsMatch(line, "等级|层|境"))
                {
                    continue;
                }

                levels.Add(new TechniqueLevelViewModel
                {
                    Level = levelIndex++,
                    Name = TrimListMarker(line),
                    Requirements = "根据 AI 生成内容整理"
                });

                if (levels.Count >= 6)
                {
                    break;
                }
            }

            return levels;
        }

        private static List<TechniqueMoveViewModel> ParseTechniqueMoves(string text)
        {
            var moves = new List<TechniqueMoveViewModel>();
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (!Regex.IsMatch(line, "式|招|术|掌|剑|拳|步|法"))
                {
                    continue;
                }

                moves.Add(new TechniqueMoveViewModel
                {
                    Name = TrimListMarker(line),
                    Type = InferMoveType(line),
                    Description = line
                });

                if (moves.Count >= 8)
                {
                    break;
                }
            }

            return moves;
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

        private static string InferMoveType(string line)
        {
            if (Regex.IsMatch(line, "守|护|御|盾"))
            {
                return "防御";
            }
            if (Regex.IsMatch(line, "步|身法|遁"))
            {
                return "身法";
            }

            return "攻击";
        }

        private static string TrimListMarker(string line)
        {
            return line.Trim().TrimStart('•', '-', '*', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', '.', '、', ' ');
        }

        /// <summary>
        /// 选择功法体系
        /// </summary>
        private void SelectTechniqueSystem(TechniqueSystemViewModel technique)
        {
            if (technique == null) return;

            SelectedTechnique = technique;
            LoadTechniqueSystemDetails(technique);
            ShowEditPanel();
        }

        /// <summary>
        /// 加载功法体系详情
        /// </summary>
        private void LoadTechniqueSystemDetails(TechniqueSystemViewModel technique)
        {
            // 填充基本信息
            TechniqueNameTextBox.Text = technique.Name;
            TechniqueDescriptionTextBox.Text = technique.Description;
            TechniqueOriginTextBox.Text = technique.Origin;
            
            // 设置功法类别选择
            foreach (ComboBoxItem item in TechniqueCategoryComboBox.Items)
            {
                if (item.Content.ToString() == technique.Category)
                {
                    TechniqueCategoryComboBox.SelectedItem = item;
                    break;
                }
            }

            // 设置功法品级选择
            foreach (ComboBoxItem item in TechniqueGradeComboBox.Items)
            {
                if (item.Content.ToString() == technique.Grade)
                {
                    TechniqueGradeComboBox.SelectedItem = item;
                    break;
                }
            }

            // 加载等级列表
            LoadTechniqueLevels(technique);
            
            // 加载招式列表
            LoadTechniqueMoves(technique);
        }

        /// <summary>
        /// 加载功法等级
        /// </summary>
        private void LoadTechniqueLevels(TechniqueSystemViewModel technique)
        {
            TechniqueLevels.Clear();
            foreach (var level in technique.Levels.OrderBy(l => l.Level))
            {
                TechniqueLevels.Add(level);
            }

            LevelListControl.ItemsSource = TechniqueLevels;
        }

        /// <summary>
        /// 加载功法招式
        /// </summary>
        private void LoadTechniqueMoves(TechniqueSystemViewModel technique)
        {
            TechniqueMoves.Clear();
            foreach (var move in technique.Moves)
            {
                TechniqueMoves.Add(move);
            }

            MoveListControl.ItemsSource = TechniqueMoves;
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
                var newLevel = new TechniqueLevelViewModel
                {
                    Level = TechniqueLevels.Count + 1,
                    Name = "",
                    Requirements = ""
                };

                TechniqueLevels.Add(newLevel);
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
                if (sender is Button button && button.CommandParameter is TechniqueLevelViewModel level)
                {
                    TechniqueLevels.Remove(level);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除等级失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region 招式管理

        /// <summary>
        /// 添加招式
        /// </summary>
        private void AddMove_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var newMove = new TechniqueMoveViewModel
                {
                    Name = "",
                    Type = "",
                    Description = ""
                };

                TechniqueMoves.Add(newMove);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加招式失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 删除招式
        /// </summary>
        private void RemoveMove_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.CommandParameter is TechniqueMoveViewModel move)
                {
                    TechniqueMoves.Remove(move);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除招式失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region 保存和取消

        /// <summary>
        /// 保存功法体系
        /// </summary>
        private async void SaveTechnique_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 验证输入
                if (string.IsNullOrWhiteSpace(TechniqueNameTextBox.Text))
                {
                    MessageBox.Show("请输入功法名称", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 更新功法体系信息
                SelectedTechnique.Name = TechniqueNameTextBox.Text.Trim();
                SelectedTechnique.Description = TechniqueDescriptionTextBox.Text.Trim();
                SelectedTechnique.Origin = TechniqueOriginTextBox.Text.Trim();
                SelectedTechnique.Category = (TechniqueCategoryComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "修炼功法";
                SelectedTechnique.Grade = (TechniqueGradeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "凡级";
                SelectedTechnique.LevelCount = TechniqueLevels.Count;
                SelectedTechnique.MoveCount = TechniqueMoves.Count;
                SelectedTechnique.Levels = TechniqueLevels.OrderBy(l => l.Level).ToList();
                SelectedTechnique.Moves = TechniqueMoves.ToList();

                // 如果是新建功法体系，添加到列表
                if (SelectedTechnique.Id == 0)
                {
                    SelectedTechnique.Id = TechniqueSystems.Count > 0 ? TechniqueSystems.Max(t => t.Id) + 1 : 1;
                    TechniqueSystems.Add(SelectedTechnique);
                }

                await PersistTechniqueSystemsAsync();
                MessageBox.Show("功法体系保存成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);

                // 刷新列表
                FilterTechniqueSystems();
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存功法体系失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                TechniqueNameTextBox.Text = "";
                TechniqueDescriptionTextBox.Text = "";
                TechniqueOriginTextBox.Text = "";
                TechniqueCategoryComboBox.SelectedIndex = 0;
                TechniqueGradeComboBox.SelectedIndex = 0;

                // 清空列表
                TechniqueLevels.Clear();
                TechniqueMoves.Clear();

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

        /// <summary>
        /// 在项目切换后刷新功法体系数据。
        /// </summary>
        /// <param name="projectId">当前项目标识。</param>
        /// <param name="projectName">当前项目名称。</param>
        public async Task RefreshOnProjectChangedAsync(Guid? projectId, string? projectName)
        {
            _currentProjectId = projectId ?? Guid.Empty;
            await LoadTechniqueSystemsAsync();
        }

        /// <summary>
        /// 在导航到当前视图时刷新对应项目的功法体系数据。
        /// </summary>
        /// <param name="context">导航上下文。</param>
        public void OnNavigatedTo(NavigationContext context)
        {
            _currentProjectId = context.ProjectId ?? Guid.Empty;
            _ = LoadTechniqueSystemsAsync();
        }

        private async Task PersistTechniqueSystemsAsync()
        {
            if (_currentProjectId == Guid.Empty || _techniqueDataService == null)
            {
                return;
            }

            foreach (var technique in TechniqueSystems)
            {
                technique.LevelCount = technique.Levels.Count;
                technique.MoveCount = technique.Moves.Count;
            }

            await _techniqueDataService.SaveTechniqueSystemsAsync(_currentProjectId, TechniqueSystems);
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

    #region ViewModel类

    /// <summary>
    /// 功法体系视图模型
    /// </summary>
    public class TechniqueSystemViewModel
    {
        /// <summary>
        /// 功法体系标识。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 功法体系名称。
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// 功法类别。
        /// </summary>
        public string Category { get; set; } = "";

        /// <summary>
        /// 功法品级。
        /// </summary>
        public string Grade { get; set; } = "";

        /// <summary>
        /// 功法起源。
        /// </summary>
        public string Origin { get; set; } = "";

        /// <summary>
        /// 功法描述。
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// 等级数量。
        /// </summary>
        public int LevelCount { get; set; }

        /// <summary>
        /// 招式数量。
        /// </summary>
        public int MoveCount { get; set; }

        /// <summary>
        /// 创建时间。
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 功法等级列表。
        /// </summary>
        public List<TechniqueLevelViewModel> Levels { get; set; } = new();

        /// <summary>
        /// 功法招式列表。
        /// </summary>
        public List<TechniqueMoveViewModel> Moves { get; set; } = new();
    }

    /// <summary>
    /// 功法等级视图模型
    /// </summary>
    public class TechniqueLevelViewModel
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
    /// 功法招式视图模型
    /// </summary>
    public class TechniqueMoveViewModel
    {
        /// <summary>
        /// 招式名称。
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// 招式类型。
        /// </summary>
        public string Type { get; set; } = "";

        /// <summary>
        /// 招式描述。
        /// </summary>
        public string Description { get; set; } = "";
    }

    #endregion
}
