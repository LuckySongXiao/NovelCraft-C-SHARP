using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using MaterialDesignThemes.Wpf;
using NovelManagement.Application.Services;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;
using NovelManagement.WPF.Services;

namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// 修炼体系管理视图
    /// </summary>
    public partial class CultivationSystemView : UserControl, INavigationRefreshableView
    {
        private readonly CultivationSystemService _cultivationSystemService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CultivationSystemView> _logger;
        private readonly ProjectContextService? _projectContextService;
        private readonly CurrentProjectGuard? _currentProjectGuard;
        private readonly IAIAssistantService? _aiAssistantService;
        private Guid _currentProjectId;
        private List<CultivationSystemViewModel> _allCultivationSystems = new();

        public ObservableCollection<CultivationSystemViewModel> CultivationSystems { get; } = new();
        public ObservableCollection<CultivationLevelViewModel> CultivationLevels { get; } = new();
        public CultivationSystemViewModel? SelectedCultivation { get; set; }

        /// <summary>
        /// 初始化修炼体系管理视图。
        /// </summary>
        public CultivationSystemView()
        {
            var serviceProvider = App.ServiceProvider;
            _cultivationSystemService = serviceProvider.GetRequiredService<CultivationSystemService>();
            _unitOfWork = serviceProvider.GetRequiredService<IUnitOfWork>();
            _logger = serviceProvider.GetRequiredService<ILogger<CultivationSystemView>>();
            _projectContextService = serviceProvider.GetService<ProjectContextService>();
            _currentProjectGuard = serviceProvider.GetService<CurrentProjectGuard>();
            _aiAssistantService = serviceProvider.GetService<IAIAssistantService>();
            _currentProjectId = _projectContextService?.CurrentProjectId ?? Guid.Empty;

            InitializeComponent();
            CultivationListControl.ItemsSource = CultivationSystems;
            Loaded += async (_, _) => await LoadCultivationSystemsAsync();
        }

        private bool EnsureCurrentProject(string featureName, out Guid projectId)
        {
            if (_currentProjectGuard != null)
            {
                return _currentProjectGuard.TryGetCurrentProjectId(Window.GetWindow(this), featureName, out projectId);
            }

            projectId = _projectContextService?.CurrentProjectId ?? Guid.Empty;
            return projectId != Guid.Empty;
        }

        private async Task LoadCultivationSystemsAsync()
        {
            try
            {
                if (!EnsureCurrentProject("修炼体系管理", out var projectId))
                {
                    _allCultivationSystems.Clear();
                    CultivationSystems.Clear();
                    CultivationLevels.Clear();
                    SelectedCultivation = null;
                    RenderEmptyState();
                    UpdateStatistics();
                    return;
                }

                _currentProjectId = projectId;
                var systems = (await _cultivationSystemService.GetCultivationSystemsByProjectIdAsync(projectId)).ToList();
                var viewModels = new List<CultivationSystemViewModel>();
                foreach (var system in systems)
                {
                    var withLevels = await _unitOfWork.CultivationSystems.GetWithLevelsAsync(system.Id);
                    viewModels.Add(ConvertToViewModel(withLevels ?? system));
                }

                _allCultivationSystems = viewModels;
                ApplyFilters();
                UpdateStatistics();

                if (SelectedCultivation != null)
                {
                    var refreshed = _allCultivationSystems.FirstOrDefault(c => c.CultivationSystemId == SelectedCultivation.CultivationSystemId);
                    if (refreshed != null)
                    {
                        SelectCultivation(refreshed);
                        return;
                    }
                }

                RenderEmptyState();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载修炼体系失败");
                MessageBox.Show($"加载修炼体系数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task RefreshOnProjectChangedAsync(Guid? projectId, string? projectName)
        {
            _currentProjectId = projectId ?? Guid.Empty;
            SelectedCultivation = null;
            await LoadCultivationSystemsAsync();
        }

        private static CultivationSystemViewModel ConvertToViewModel(CultivationSystem system)
        {
            var levels = system.Levels
                .OrderBy(level => level.Order)
                .Select(level => new CultivationLevelViewModel
                {
                    CultivationLevelId = level.Id,
                    Name = level.Name,
                    Level = level.Order,
                    Description = level.Description ?? string.Empty,
                    Requirements = level.BreakthroughCondition ?? string.Empty,
                    Benefits = level.Abilities ?? string.Empty,
                    CultivationTime = level.CultivationTime ?? string.Empty
                })
                .ToList();

            var metadata = CultivationMetadata.Parse(system.Tags);

            return new CultivationSystemViewModel
            {
                CultivationSystemId = system.Id,
                Name = system.Name,
                Category = system.Type,
                Grade = metadata.Grade,
                Origin = metadata.Origin,
                Description = system.Description ?? string.Empty,
                LevelCount = levels.Count,
                MaxLevel = levels.LastOrDefault()?.Name ?? $"第{system.MaxLevel}层",
                CreatedAt = system.CreatedAt.ToLocalTime(),
                Difficulty = system.Difficulty,
                MaxLevelValue = system.MaxLevel,
                CultivationMethod = system.CultivationMethod ?? string.Empty,
                RealmDivision = system.RealmDivision ?? string.Empty,
                BreakthroughConditions = system.BreakthroughConditions ?? string.Empty,
                CultivationResources = system.CultivationResources ?? string.Empty,
                Characteristics = system.Characteristics ?? string.Empty,
                Risks = system.Risks ?? string.Empty,
                Notes = system.Notes ?? string.Empty,
                Tags = system.Tags ?? string.Empty,
                Status = system.Status,
                Importance = system.Importance,
                Levels = levels
            };
        }

        private void UpdateStatistics()
        {
            TotalCountText.Text = CultivationSystems.Count.ToString();
            SelectedCountText.Text = SelectedCultivation == null ? "0" : "1";
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void CultivationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CultivationListControl.SelectedItem is CultivationSystemViewModel selected)
            {
                SelectCultivation(selected);
            }
        }

        private async void AddCultivation_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!EnsureCurrentProject("新建修炼体系", out var projectId))
                {
                    return;
                }

                var dialog = new CultivationSystemEditDialog(projectId, _aiAssistantService) { Owner = Window.GetWindow(this) };
                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                var entity = dialog.BuildCultivationSystem();
                await _cultivationSystemService.CreateCultivationSystemAsync(entity);
                await SaveLevelsAsync(entity.Id, dialog.GetLevelEntities(entity.Id));
                await LoadCultivationSystemsAsync();

                var created = _allCultivationSystems.FirstOrDefault(c => c.CultivationSystemId == entity.Id);
                if (created != null)
                {
                    SelectCultivation(created);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建修炼体系失败");
                MessageBox.Show($"创建修炼体系失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Import_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!EnsureCurrentProject("导入修炼体系", out var projectId))
                {
                    return;
                }

                var dialog = new OpenFileDialog
                {
                    Title = "导入修炼体系",
                    Filter = "JSON 文件|*.json",
                    CheckFileExists = true
                };

                if (dialog.ShowDialog(Window.GetWindow(this)) != true)
                {
                    return;
                }

                var items = JsonSerializer.Deserialize<List<CultivationSystemImportModel>>(File.ReadAllText(dialog.FileName))
                    ?? new List<CultivationSystemImportModel>();
                if (items.Count == 0)
                {
                    MessageBox.Show("未读取到可导入的修炼体系。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var existing = (await _cultivationSystemService.GetCultivationSystemsByProjectIdAsync(projectId)).ToList();
                foreach (var item in items)
                {
                    var current = existing.FirstOrDefault(c => string.Equals(c.Name, item.Name, StringComparison.OrdinalIgnoreCase));
                    if (current == null)
                    {
                        current = item.ToEntity(projectId);
                        await _cultivationSystemService.CreateCultivationSystemAsync(current);
                    }
                    else
                    {
                        item.ApplyTo(current);
                        await _cultivationSystemService.UpdateCultivationSystemAsync(current);
                    }

                    await SaveLevelsAsync(current.Id, item.ToLevels(current.Id));
                }

                await LoadCultivationSystemsAsync();
                MessageBox.Show($"已完成导入，共处理 {items.Count} 个修炼体系。", "导入完成", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入修炼体系失败");
                MessageBox.Show($"导入失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CultivationSystems.Count == 0)
                {
                    MessageBox.Show("当前没有可导出的修炼体系。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dialog = new SaveFileDialog
                {
                    Title = "导出修炼体系",
                    Filter = "JSON 文件|*.json",
                    FileName = $"修炼体系_{DateTime.Now:yyyyMMdd_HHmmss}.json",
                    AddExtension = true
                };

                if (dialog.ShowDialog(Window.GetWindow(this)) != true)
                {
                    return;
                }

                var data = CultivationSystems.Select(CultivationSystemImportModel.FromViewModel).ToList();
                Directory.CreateDirectory(Path.GetDirectoryName(dialog.FileName)!);
                File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
                MessageBox.Show($"已导出 {data.Count} 个修炼体系到：{dialog.FileName}", "导出完成", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出修炼体系失败");
                MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void AIAssistant_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_aiAssistantService == null)
                {
                    MessageBox.Show("AI助手服务未初始化。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (SelectedCultivation == null)
                {
                    MessageBox.Show("请先选择一个修炼体系，再进行 AI 分析。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var result = await _aiAssistantService.GeneratePlotAsync(new Dictionary<string, object>
                {
                    ["plotType"] = "修炼体系分析",
                    ["theme"] = SelectedCultivation.Name,
                    ["requirements"] = BuildAiAnalysisPrompt(SelectedCultivation)
                });

                if (!result.IsSuccess || result.Data == null)
                {
                    MessageBox.Show(result.Message ?? "AI分析失败。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ShowAnalysisResultDialog($"AI分析 - {SelectedCultivation.Name}", result.Data.ToString() ?? string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI分析修炼体系失败");
                MessageBox.Show($"AI分析失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            var searchText = SearchTextBox.Text?.Trim().ToLowerInvariant() ?? string.Empty;
            var selectedCategory = (CategoryFilterComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "全部类型";

            var result = _allCultivationSystems.Where(item =>
            {
                var matchesSearch = string.IsNullOrWhiteSpace(searchText) ||
                                    item.Name.ToLowerInvariant().Contains(searchText) ||
                                    item.Description.ToLowerInvariant().Contains(searchText) ||
                                    item.Category.ToLowerInvariant().Contains(searchText) ||
                                    item.Origin.ToLowerInvariant().Contains(searchText);
                var matchesType = selectedCategory == "全部类型" || string.IsNullOrWhiteSpace(selectedCategory) || item.Category == selectedCategory;
                return matchesSearch && matchesType;
            }).ToList();

            CultivationSystems.Clear();
            foreach (var item in result)
            {
                CultivationSystems.Add(item);
            }

            UpdateStatistics();
        }

        private void SelectCultivation(CultivationSystemViewModel cultivation)
        {
            SelectedCultivation = cultivation;
            CultivationLevels.Clear();
            foreach (var item in cultivation.Levels.OrderBy(level => level.Level))
            {
                CultivationLevels.Add(item);
            }

            RenderCultivationDetails(cultivation);
            UpdateStatistics();
        }

        private void RenderEmptyState()
        {
            DetailsPanel.Children.Clear();
            DetailsPanel.Children.Add(new TextBlock
            {
                Text = "请选择一个修炼体系查看详情",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 80, 0, 0)
            });
        }

        private void RenderCultivationDetails(CultivationSystemViewModel cultivation)
        {
            DetailsPanel.Children.Clear();

            var root = new StackPanel();
            root.Children.Add(new TextBlock
            {
                Text = cultivation.Name,
                FontSize = 26,
                FontWeight = FontWeights.Bold
            });
            root.Children.Add(new TextBlock
            {
                Text = $"{cultivation.Category} | {cultivation.Grade} | 难度 {cultivation.Difficulty}",
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 8, 0, 20)
            });

            var actions = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 20)
            };
            actions.Children.Add(CreateActionButton("编辑", async (_, _) => await EditSelectedAsync(), true));
            actions.Children.Add(CreateActionButton("删除", async (_, _) => await DeleteSelectedAsync(), false));
            actions.Children.Add(CreateActionButton("AI分析", async (_, _) => await AnalyzeSelectedAsync(), false));
            root.Children.Add(actions);

            root.Children.Add(CreateInfoCard("基础信息", new[]
            {
                ("描述", cultivation.Description),
                ("来源", cultivation.Origin),
                ("最高等级", cultivation.MaxLevel),
                ("修炼方法", cultivation.CultivationMethod),
                ("境界划分", cultivation.RealmDivision),
                ("突破条件", cultivation.BreakthroughConditions),
                ("修炼资源", cultivation.CultivationResources),
                ("体系特点", cultivation.Characteristics),
                ("修炼风险", cultivation.Risks),
                ("标签", cultivation.Tags),
                ("备注", cultivation.Notes)
            }));

            root.Children.Add(CreateLevelsCard(cultivation.Levels));
            DetailsPanel.Children.Add(root);
        }

        private static Card CreateInfoCard(string title, IEnumerable<(string Label, string Value)> items)
        {
            var card = new Card { Margin = new Thickness(0, 0, 0, 16), Padding = new Thickness(16) };
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 12)
            });

            foreach (var item in items)
            {
                panel.Children.Add(new TextBlock { Text = item.Label, FontWeight = FontWeights.Medium });
                panel.Children.Add(new TextBlock
                {
                    Text = string.IsNullOrWhiteSpace(item.Value) ? "未设置" : item.Value,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = Brushes.Gray,
                    Margin = new Thickness(0, 2, 0, 10)
                });
            }

            card.Content = panel;
            return card;
        }

        private static Card CreateLevelsCard(IEnumerable<CultivationLevelViewModel> levels)
        {
            var card = new Card { Margin = new Thickness(0, 0, 0, 16), Padding = new Thickness(16) };
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock
            {
                Text = "修炼等级",
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 12)
            });

            var list = levels.ToList();
            if (list.Count == 0)
            {
                panel.Children.Add(new TextBlock { Text = "未配置等级", Foreground = Brushes.Gray });
            }
            else
            {
                foreach (var level in list.OrderBy(item => item.Level))
                {
                    panel.Children.Add(new TextBlock
                    {
                        Text = $"{level.Level}. {level.Name}",
                        FontWeight = FontWeights.Medium
                    });
                    panel.Children.Add(new TextBlock
                    {
                        Text = string.IsNullOrWhiteSpace(level.Description) ? "未填写描述" : level.Description,
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = Brushes.Gray,
                        Margin = new Thickness(0, 2, 0, 8)
                    });
                }
            }

            card.Content = panel;
            return card;
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

        private async Task EditSelectedAsync()
        {
            if (SelectedCultivation == null || !EnsureCurrentProject("编辑修炼体系", out _))
            {
                return;
            }

            var dialog = new CultivationSystemEditDialog(_currentProjectId, _aiAssistantService, SelectedCultivation) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var entity = await _cultivationSystemService.GetCultivationSystemByIdAsync(SelectedCultivation.CultivationSystemId);
            if (entity == null)
            {
                MessageBox.Show("未找到要编辑的修炼体系。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            dialog.ApplyTo(entity);
            await _cultivationSystemService.UpdateCultivationSystemAsync(entity);
            await SaveLevelsAsync(entity.Id, dialog.GetLevelEntities(entity.Id));
            await LoadCultivationSystemsAsync();
            var updated = _allCultivationSystems.FirstOrDefault(c => c.CultivationSystemId == entity.Id);
            if (updated != null)
            {
                SelectCultivation(updated);
            }
        }

        private async Task DeleteSelectedAsync()
        {
            if (SelectedCultivation == null)
            {
                return;
            }

            var result = MessageBox.Show($"确定要删除修炼体系“{SelectedCultivation.Name}”吗？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            await _cultivationSystemService.DeleteCultivationSystemAsync(SelectedCultivation.CultivationSystemId);
            SelectedCultivation = null;
            CultivationLevels.Clear();
            await LoadCultivationSystemsAsync();
        }

        private async Task AnalyzeSelectedAsync()
        {
            if (SelectedCultivation == null)
            {
                return;
            }

            await Task.Run(() => AIAssistant_Click(this, new RoutedEventArgs()));
        }

        private async Task SaveLevelsAsync(Guid cultivationSystemId, IReadOnlyList<CultivationLevel> levels)
        {
            var existingLevels = (await _unitOfWork.CultivationLevels.GetBySystemIdAsync(cultivationSystemId)).ToList();
            foreach (var level in existingLevels)
            {
                await _unitOfWork.CultivationLevels.DeleteAsync(level);
            }

            foreach (var level in levels)
            {
                await _unitOfWork.CultivationLevels.AddAsync(level);
            }

            await _unitOfWork.SaveChangesAsync();
        }

        private static string BuildAiAnalysisPrompt(CultivationSystemViewModel cultivation)
        {
            var builder = new StringBuilder();
            builder.AppendLine("请从世界观设定与剧情可用性角度分析以下修炼体系，并提出优化建议。");
            builder.AppendLine($"名称：{cultivation.Name}");
            builder.AppendLine($"类别：{cultivation.Category}");
            builder.AppendLine($"品级：{cultivation.Grade}");
            builder.AppendLine($"来源：{cultivation.Origin}");
            builder.AppendLine($"描述：{cultivation.Description}");
            builder.AppendLine($"修炼方法：{cultivation.CultivationMethod}");
            builder.AppendLine($"境界划分：{cultivation.RealmDivision}");
            builder.AppendLine($"突破条件：{cultivation.BreakthroughConditions}");
            builder.AppendLine($"修炼资源：{cultivation.CultivationResources}");
            builder.AppendLine($"体系特点：{cultivation.Characteristics}");
            builder.AppendLine($"修炼风险：{cultivation.Risks}");
            builder.AppendLine("请输出优势、风险点、平衡性问题、剧情可用点和完善建议。");
            return builder.ToString();
        }

        private static void ShowAnalysisResultDialog(string title, string content)
        {
            var window = new Window
            {
                Title = title,
                Width = 760,
                Height = 620,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var panel = new DockPanel { Margin = new Thickness(20) };
            var textBox = new TextBox
            {
                Text = content,
                IsReadOnly = true,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            DockPanel.SetDock(textBox, Dock.Top);
            panel.Children.Add(textBox);

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 12, 0, 0)
            };
            var copyButton = new Button { Content = "复制结果", Width = 88, Margin = new Thickness(0, 0, 12, 0) };
            copyButton.Click += (_, _) =>
            {
                Clipboard.SetText(content);
                MessageBox.Show("结果已复制到剪贴板。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            };
            var closeButton = new Button { Content = "关闭", Width = 88, IsDefault = true };
            closeButton.Click += (_, _) => window.Close();
            buttons.Children.Add(copyButton);
            buttons.Children.Add(closeButton);
            DockPanel.SetDock(buttons, Dock.Bottom);
            panel.Children.Add(buttons);

            window.Content = panel;
            window.ShowDialog();
        }
    }

    public class CultivationSystemViewModel
    {
        public Guid CultivationSystemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Grade { get; set; } = "凡级";
        public string Origin { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int LevelCount { get; set; }
        public string MaxLevel { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int Difficulty { get; set; }
        public int MaxLevelValue { get; set; }
        public string CultivationMethod { get; set; } = string.Empty;
        public string RealmDivision { get; set; } = string.Empty;
        public string BreakthroughConditions { get; set; } = string.Empty;
        public string CultivationResources { get; set; } = string.Empty;
        public string Characteristics { get; set; } = string.Empty;
        public string Risks { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public int Importance { get; set; }
        public List<CultivationLevelViewModel> Levels { get; set; } = new();
    }

    public class CultivationLevelViewModel
    {
        public Guid CultivationLevelId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Requirements { get; set; } = string.Empty;
        public string Benefits { get; set; } = string.Empty;
        public string CultivationTime { get; set; } = string.Empty;
    }

    internal sealed class CultivationSystemImportModel
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = "内功心法";
        public string Grade { get; set; } = "凡级";
        public string Origin { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Difficulty { get; set; } = 50;
        public int MaxLevelValue { get; set; } = 10;
        public string CultivationMethod { get; set; } = string.Empty;
        public string RealmDivision { get; set; } = string.Empty;
        public string BreakthroughConditions { get; set; } = string.Empty;
        public string CultivationResources { get; set; } = string.Empty;
        public string Characteristics { get; set; } = string.Empty;
        public string Risks { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public List<CultivationLevelImportModel> Levels { get; set; } = new();

        public CultivationSystem ToEntity(Guid projectId)
        {
            return new CultivationSystem
            {
                ProjectId = projectId,
                Name = Name,
                Type = Category,
                Difficulty = Difficulty,
                MaxLevel = MaxLevelValue,
                Description = Description,
                CultivationMethod = CultivationMethod,
                RealmDivision = RealmDivision,
                BreakthroughConditions = BreakthroughConditions,
                CultivationResources = CultivationResources,
                Characteristics = Characteristics,
                Risks = Risks,
                Notes = Notes,
                Tags = CultivationMetadata.BuildTags(Grade, Origin, Tags),
                Importance = Math.Clamp(Difficulty / 10, 1, 10),
                Status = "Active"
            };
        }

        public void ApplyTo(CultivationSystem entity)
        {
            entity.Name = Name;
            entity.Type = Category;
            entity.Difficulty = Difficulty;
            entity.MaxLevel = MaxLevelValue;
            entity.Description = Description;
            entity.CultivationMethod = CultivationMethod;
            entity.RealmDivision = RealmDivision;
            entity.BreakthroughConditions = BreakthroughConditions;
            entity.CultivationResources = CultivationResources;
            entity.Characteristics = Characteristics;
            entity.Risks = Risks;
            entity.Notes = Notes;
            entity.Tags = CultivationMetadata.BuildTags(Grade, Origin, Tags);
            entity.Importance = Math.Clamp(Difficulty / 10, 1, 10);
        }

        public List<CultivationLevel> ToLevels(Guid systemId)
        {
            return Levels.Select(item => item.ToEntity(systemId)).ToList();
        }

        public static CultivationSystemImportModel FromViewModel(CultivationSystemViewModel viewModel)
        {
            return new CultivationSystemImportModel
            {
                Name = viewModel.Name,
                Category = viewModel.Category,
                Grade = viewModel.Grade,
                Origin = viewModel.Origin,
                Description = viewModel.Description,
                Difficulty = viewModel.Difficulty,
                MaxLevelValue = viewModel.MaxLevelValue,
                CultivationMethod = viewModel.CultivationMethod,
                RealmDivision = viewModel.RealmDivision,
                BreakthroughConditions = viewModel.BreakthroughConditions,
                CultivationResources = viewModel.CultivationResources,
                Characteristics = viewModel.Characteristics,
                Risks = viewModel.Risks,
                Notes = viewModel.Notes,
                Tags = viewModel.Tags,
                Levels = viewModel.Levels.Select(CultivationLevelImportModel.FromViewModel).ToList()
            };
        }
    }

    internal sealed class CultivationLevelImportModel
    {
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Requirements { get; set; } = string.Empty;
        public string Benefits { get; set; } = string.Empty;
        public string CultivationTime { get; set; } = string.Empty;

        public CultivationLevel ToEntity(Guid systemId)
        {
            return new CultivationLevel
            {
                CultivationSystemId = systemId,
                Name = Name,
                Order = Level,
                Description = Description,
                BreakthroughCondition = Requirements,
                Abilities = Benefits,
                CultivationTime = CultivationTime
            };
        }

        public static CultivationLevelImportModel FromViewModel(CultivationLevelViewModel viewModel)
        {
            return new CultivationLevelImportModel
            {
                Name = viewModel.Name,
                Level = viewModel.Level,
                Description = viewModel.Description,
                Requirements = viewModel.Requirements,
                Benefits = viewModel.Benefits,
                CultivationTime = viewModel.CultivationTime
            };
        }
    }

    internal sealed class CultivationSystemEditDialog : Window
    {
        private readonly Guid _projectId;
        private readonly IAIAssistantService? _aiAssistantService;
        private readonly ProjectReadModelService? _projectReadModelService;
        private readonly CultivationSystemViewModel? _originalModel;
        private readonly TextBox _nameTextBox = new();
        private readonly ComboBox _categoryComboBox = new();
        private readonly ComboBox _gradeComboBox = new();
        private readonly TextBox _originTextBox = new();
        private readonly Slider _difficultySlider = new();
        private readonly TextBox _descriptionTextBox = new();
        private readonly TextBox _cultivationMethodTextBox = new();
        private readonly TextBox _realmDivisionTextBox = new();
        private readonly TextBox _breakthroughConditionsTextBox = new();
        private readonly TextBox _cultivationResourcesTextBox = new();
        private readonly TextBox _characteristicsTextBox = new();
        private readonly TextBox _risksTextBox = new();
        private readonly TextBox _tagsTextBox = new();
        private readonly TextBox _notesTextBox = new();
        private readonly ObservableCollection<CultivationLevelViewModel> _levels = new();

        public CultivationSystemEditDialog(Guid projectId, IAIAssistantService? aiAssistantService, CultivationSystemViewModel? model = null)
        {
            _projectId = projectId;
            _aiAssistantService = aiAssistantService;
            _projectReadModelService = App.ServiceProvider?.GetService<ProjectReadModelService>();
            _originalModel = model;

            Title = model == null ? "新建修炼体系" : $"编辑修炼体系 - {model.Name}";
            Width = 720;
            Height = 900;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            _categoryComboBox.ItemsSource = new[] { "内功心法", "外功招式", "身法轻功", "炼体功法", "神识功法" };
            _gradeComboBox.ItemsSource = new[] { "凡级", "黄级", "玄级", "地级", "天级", "神级", "仙级" };
            _categoryComboBox.SelectedItem = model?.Category ?? "内功心法";
            _gradeComboBox.SelectedItem = model?.Grade ?? "凡级";
            _nameTextBox.Text = model?.Name ?? string.Empty;
            _originTextBox.Text = model?.Origin ?? string.Empty;
            _difficultySlider.Minimum = 1;
            _difficultySlider.Maximum = 100;
            _difficultySlider.Value = model?.Difficulty ?? 50;
            _descriptionTextBox.Text = model?.Description ?? string.Empty;
            _cultivationMethodTextBox.Text = model?.CultivationMethod ?? string.Empty;
            _realmDivisionTextBox.Text = model?.RealmDivision ?? string.Empty;
            _breakthroughConditionsTextBox.Text = model?.BreakthroughConditions ?? string.Empty;
            _cultivationResourcesTextBox.Text = model?.CultivationResources ?? string.Empty;
            _characteristicsTextBox.Text = model?.Characteristics ?? string.Empty;
            _risksTextBox.Text = model?.Risks ?? string.Empty;
            _tagsTextBox.Text = model?.Tags ?? string.Empty;
            _notesTextBox.Text = model?.Notes ?? string.Empty;

            foreach (var level in model?.Levels ?? Enumerable.Empty<CultivationLevelViewModel>())
            {
                _levels.Add(new CultivationLevelViewModel
                {
                    CultivationLevelId = level.CultivationLevelId,
                    Name = level.Name,
                    Level = level.Level,
                    Description = level.Description,
                    Requirements = level.Requirements,
                    Benefits = level.Benefits,
                    CultivationTime = level.CultivationTime
                });
            }

            Content = BuildContent();
        }

        public CultivationSystem BuildCultivationSystem()
        {
            return new CultivationSystem
            {
                ProjectId = _projectId,
                Name = _nameTextBox.Text.Trim(),
                Type = _categoryComboBox.SelectedItem?.ToString() ?? "内功心法",
                Difficulty = (int)_difficultySlider.Value,
                MaxLevel = _levels.Any() ? _levels.Max(level => level.Level) : 1,
                Description = _descriptionTextBox.Text.Trim(),
                CultivationMethod = _cultivationMethodTextBox.Text.Trim(),
                RealmDivision = _realmDivisionTextBox.Text.Trim(),
                BreakthroughConditions = _breakthroughConditionsTextBox.Text.Trim(),
                CultivationResources = _cultivationResourcesTextBox.Text.Trim(),
                Characteristics = _characteristicsTextBox.Text.Trim(),
                Risks = _risksTextBox.Text.Trim(),
                Tags = CultivationMetadata.BuildTags(
                    _gradeComboBox.SelectedItem?.ToString() ?? "凡级",
                    _originTextBox.Text.Trim(),
                    _tagsTextBox.Text.Trim()),
                Notes = _notesTextBox.Text.Trim(),
                Importance = Math.Clamp((int)_difficultySlider.Value / 10, 1, 10),
                Status = "Active"
            };
        }

        public void ApplyTo(CultivationSystem entity)
        {
            var value = BuildCultivationSystem();
            entity.Name = value.Name;
            entity.Type = value.Type;
            entity.Difficulty = value.Difficulty;
            entity.MaxLevel = value.MaxLevel;
            entity.Description = value.Description;
            entity.CultivationMethod = value.CultivationMethod;
            entity.RealmDivision = value.RealmDivision;
            entity.BreakthroughConditions = value.BreakthroughConditions;
            entity.CultivationResources = value.CultivationResources;
            entity.Characteristics = value.Characteristics;
            entity.Risks = value.Risks;
            entity.Tags = value.Tags;
            entity.Notes = value.Notes;
            entity.Importance = value.Importance;
        }

        public IReadOnlyList<CultivationLevel> GetLevelEntities(Guid cultivationSystemId)
        {
            return _levels.Select(level => new CultivationLevel
            {
                CultivationSystemId = cultivationSystemId,
                Name = level.Name,
                Order = level.Level,
                Description = level.Description,
                BreakthroughCondition = level.Requirements,
                Abilities = level.Benefits,
                CultivationTime = level.CultivationTime
            }).ToList();
        }

        private FrameworkElement BuildContent()
        {
            var panel = new StackPanel { Margin = new Thickness(20) };
            panel.Children.Add(DialogUiHelpers.CreateLabel("体系名称"));
            panel.Children.Add(_nameTextBox);
            panel.Children.Add(DialogUiHelpers.CreateLabel("体系类别"));
            panel.Children.Add(_categoryComboBox);
            panel.Children.Add(DialogUiHelpers.CreateLabel("功法品级"));
            panel.Children.Add(_gradeComboBox);
            panel.Children.Add(DialogUiHelpers.CreateLabel("来源"));
            panel.Children.Add(_originTextBox);
            panel.Children.Add(DialogUiHelpers.CreateLabel("修炼难度"));
            panel.Children.Add(_difficultySlider);
            panel.Children.Add(DialogUiHelpers.CreateLabel("体系描述"));
            panel.Children.Add(DialogUiHelpers.CreateMultiLine(_descriptionTextBox, 90));
            panel.Children.Add(DialogUiHelpers.CreateLabel("修炼方法"));
            panel.Children.Add(DialogUiHelpers.CreateMultiLine(_cultivationMethodTextBox, 70));
            panel.Children.Add(DialogUiHelpers.CreateLabel("境界划分"));
            panel.Children.Add(DialogUiHelpers.CreateMultiLine(_realmDivisionTextBox, 70));
            panel.Children.Add(DialogUiHelpers.CreateLabel("突破条件"));
            panel.Children.Add(DialogUiHelpers.CreateMultiLine(_breakthroughConditionsTextBox, 70));
            panel.Children.Add(DialogUiHelpers.CreateLabel("修炼资源"));
            panel.Children.Add(DialogUiHelpers.CreateMultiLine(_cultivationResourcesTextBox, 70));
            panel.Children.Add(DialogUiHelpers.CreateLabel("体系特点"));
            panel.Children.Add(DialogUiHelpers.CreateMultiLine(_characteristicsTextBox, 70));
            panel.Children.Add(DialogUiHelpers.CreateLabel("修炼风险"));
            panel.Children.Add(DialogUiHelpers.CreateMultiLine(_risksTextBox, 70));
            panel.Children.Add(DialogUiHelpers.CreateLabel("标签"));
            panel.Children.Add(_tagsTextBox);
            panel.Children.Add(DialogUiHelpers.CreateLabel("备注"));
            panel.Children.Add(DialogUiHelpers.CreateMultiLine(_notesTextBox, 70));

            panel.Children.Add(DialogUiHelpers.CreateLabel("修炼等级"));
            var levelsList = new ListBox
            {
                Height = 180,
                ItemsSource = _levels,
                DisplayMemberPath = "Name"
            };
            panel.Children.Add(levelsList);

            var levelButtons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 8, 0, 0)
            };
            var addLevelButton = new Button { Content = "添加等级", Margin = new Thickness(0, 0, 12, 0) };
            addLevelButton.Click += (_, _) =>
            {
                var dialog = new CultivationLevelEditDialog { Owner = this };
                if (dialog.ShowDialog() == true)
                {
                    _levels.Add(dialog.BuildViewModel());
                }
            };
            var editLevelButton = new Button { Content = "编辑等级", Margin = new Thickness(0, 0, 12, 0) };
            editLevelButton.Click += (_, _) =>
            {
                if (levelsList.SelectedItem is not CultivationLevelViewModel selected)
                {
                    MessageBox.Show("请先选择一个等级。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dialog = new CultivationLevelEditDialog(selected) { Owner = this };
                if (dialog.ShowDialog() == true)
                {
                    var index = _levels.IndexOf(selected);
                    _levels[index] = dialog.BuildViewModel();
                }
            };
            var removeLevelButton = new Button { Content = "删除等级" };
            removeLevelButton.Click += (_, _) =>
            {
                if (levelsList.SelectedItem is CultivationLevelViewModel selected)
                {
                    _levels.Remove(selected);
                }
            };
            levelButtons.Children.Add(addLevelButton);
            levelButtons.Children.Add(editLevelButton);
            levelButtons.Children.Add(removeLevelButton);
            panel.Children.Add(levelButtons);

            var buttons = new StackPanel
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
                    MessageBox.Show("请输入修炼体系名称。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                DialogResult = true;
            };
            var cancelButton = new Button { Content = "返回", Width = 84, IsCancel = true };
            buttons.Children.Add(aiButton);
            buttons.Children.Add(resetButton);
            buttons.Children.Add(okButton);
            buttons.Children.Add(cancelButton);
            panel.Children.Add(buttons);

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
                ["plotType"] = "修炼体系设定",
                ["theme"] = string.IsNullOrWhiteSpace(_nameTextBox.Text)
                    ? (_categoryComboBox.SelectedItem?.ToString() ?? "修炼体系")
                    : _nameTextBox.Text.Trim(),
                ["requirements"] = prompt
            });

            if (!result.IsSuccess || result.Data == null)
            {
                MessageBox.Show(result.Message ?? "AI自动补全失败。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var text = AiAutoFillFormatter.Normalize(result.Data.ToString());
            AiAutoFillFormatter.FillIfEmpty(_descriptionTextBox, AiAutoFillFormatter.ExtractSummary(text, "体系描述", "描述", "简介"));
            AiAutoFillFormatter.FillSectionIfEmpty(_cultivationMethodTextBox, text, "修炼方法");
            AiAutoFillFormatter.FillSectionIfEmpty(_realmDivisionTextBox, text, "境界划分");
            AiAutoFillFormatter.FillSectionIfEmpty(_breakthroughConditionsTextBox, text, "突破条件");
            AiAutoFillFormatter.FillSectionIfEmpty(_cultivationResourcesTextBox, text, "修炼资源");
            AiAutoFillFormatter.FillSectionIfEmpty(_characteristicsTextBox, text, "体系特点", "特点");
            AiAutoFillFormatter.FillSectionIfEmpty(_risksTextBox, text, "修炼风险", "风险");
            AiAutoFillFormatter.FillSectionIfEmpty(_tagsTextBox, text, "标签");
            AiAutoFillFormatter.FillSectionIfEmpty(_notesTextBox, text, "备注");
            if (string.IsNullOrWhiteSpace(_nameTextBox.Text))
            {
                var generatedName = AiAutoFillFormatter.ExtractSection(text, "名称");
                if (string.IsNullOrWhiteSpace(generatedName))
                {
                    generatedName = AiAutoFillFormatter.ExtractFirstMeaningfulLine(text);
                }

                _nameTextBox.Text = string.IsNullOrWhiteSpace(generatedName) ? "AI修炼体系" : generatedName;
            }

            MessageBox.Show("已完成修炼体系自动补全。", "AI自动补全", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task<string> BuildAiPromptAsync()
        {
            var builder = new StringBuilder();
            builder.AppendLine("请补全一个修炼体系设定。");
            builder.AppendLine("请优先补全缺失信息，并与现有内容保持一致。");
            builder.AppendLine("内容必须属于世界设定层的修炼体系，不要输出剧情大纲、正文片段、人物小传或无关模块。");
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

            builder.AppendLine("当前信息：");
            builder.AppendLine($"名称：{_nameTextBox.Text}");
            builder.AppendLine($"类别：{_categoryComboBox.SelectedItem}");
            builder.AppendLine($"品级：{_gradeComboBox.SelectedItem}");
            builder.AppendLine($"来源：{_originTextBox.Text}");
            builder.AppendLine($"描述：{_descriptionTextBox.Text}");
            builder.AppendLine($"修炼方法：{_cultivationMethodTextBox.Text}");
            builder.AppendLine($"境界划分：{_realmDivisionTextBox.Text}");
            builder.AppendLine($"突破条件：{_breakthroughConditionsTextBox.Text}");
            builder.AppendLine($"修炼资源：{_cultivationResourcesTextBox.Text}");
            builder.AppendLine($"体系特点：{_characteristicsTextBox.Text}");
            builder.AppendLine($"修炼风险：{_risksTextBox.Text}");
            builder.AppendLine($"标签：{_tagsTextBox.Text}");
            builder.AppendLine($"备注：{_notesTextBox.Text}");
            builder.AppendLine();
            builder.AppendLine("生成要求：");
            builder.AppendLine("1. 必须优先遵循项目基础信息、已有世界设定和大纲约束。");
            builder.AppendLine("2. 只能补全修炼体系相关字段，不得生成无关世界设定分支。");
            builder.AppendLine("3. 若与现有输入冲突，优先保持现有输入语义一致。");
            builder.AppendLine();
            builder.AppendLine("请按以下字段输出：");
            builder.AppendLine("名称：");
            builder.AppendLine("体系描述：");
            builder.AppendLine("修炼方法：");
            builder.AppendLine("境界划分：");
            builder.AppendLine("突破条件：");
            builder.AppendLine("修炼资源：");
            builder.AppendLine("体系特点：");
            builder.AppendLine("修炼风险：");
            builder.AppendLine("标签：");
            builder.AppendLine("备注：");
            return builder.ToString().Trim();
        }

        private void ResetForm()
        {
            _categoryComboBox.SelectedItem = _originalModel?.Category ?? "内功心法";
            _gradeComboBox.SelectedItem = _originalModel?.Grade ?? "凡级";
            _nameTextBox.Text = _originalModel?.Name ?? string.Empty;
            _originTextBox.Text = _originalModel?.Origin ?? string.Empty;
            _difficultySlider.Value = _originalModel?.Difficulty ?? 50;
            _descriptionTextBox.Text = _originalModel?.Description ?? string.Empty;
            _cultivationMethodTextBox.Text = _originalModel?.CultivationMethod ?? string.Empty;
            _realmDivisionTextBox.Text = _originalModel?.RealmDivision ?? string.Empty;
            _breakthroughConditionsTextBox.Text = _originalModel?.BreakthroughConditions ?? string.Empty;
            _cultivationResourcesTextBox.Text = _originalModel?.CultivationResources ?? string.Empty;
            _characteristicsTextBox.Text = _originalModel?.Characteristics ?? string.Empty;
            _risksTextBox.Text = _originalModel?.Risks ?? string.Empty;
            _tagsTextBox.Text = _originalModel?.Tags ?? string.Empty;
            _notesTextBox.Text = _originalModel?.Notes ?? string.Empty;
        }
    }

    internal sealed class CultivationLevelEditDialog : Window
    {
        private readonly TextBox _nameTextBox = new();
        private readonly TextBox _levelTextBox = new();
        private readonly TextBox _descriptionTextBox = new();
        private readonly TextBox _requirementsTextBox = new();
        private readonly TextBox _benefitsTextBox = new();
        private readonly TextBox _cultivationTimeTextBox = new();

        public CultivationLevelEditDialog(CultivationLevelViewModel? model = null)
        {
            Title = model == null ? "添加修炼等级" : $"编辑等级 - {model.Name}";
            Width = 520;
            Height = 500;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            _nameTextBox.Text = model?.Name ?? string.Empty;
            _levelTextBox.Text = (model?.Level ?? 1).ToString();
            _descriptionTextBox.Text = model?.Description ?? string.Empty;
            _requirementsTextBox.Text = model?.Requirements ?? string.Empty;
            _benefitsTextBox.Text = model?.Benefits ?? string.Empty;
            _cultivationTimeTextBox.Text = model?.CultivationTime ?? string.Empty;

            Content = BuildContent();
        }

        public CultivationLevelViewModel BuildViewModel()
        {
            return new CultivationLevelViewModel
            {
                Name = _nameTextBox.Text.Trim(),
                Level = int.TryParse(_levelTextBox.Text, out var level) ? level : 1,
                Description = _descriptionTextBox.Text.Trim(),
                Requirements = _requirementsTextBox.Text.Trim(),
                Benefits = _benefitsTextBox.Text.Trim(),
                CultivationTime = _cultivationTimeTextBox.Text.Trim()
            };
        }

        private FrameworkElement BuildContent()
        {
            var panel = new StackPanel { Margin = new Thickness(20) };
            panel.Children.Add(DialogUiHelpers.CreateLabel("等级名称"));
            panel.Children.Add(_nameTextBox);
            panel.Children.Add(DialogUiHelpers.CreateLabel("等级序号"));
            panel.Children.Add(_levelTextBox);
            panel.Children.Add(DialogUiHelpers.CreateLabel("等级描述"));
            panel.Children.Add(DialogUiHelpers.CreateMultiLine(_descriptionTextBox, 70));
            panel.Children.Add(DialogUiHelpers.CreateLabel("突破条件"));
            panel.Children.Add(DialogUiHelpers.CreateMultiLine(_requirementsTextBox, 70));
            panel.Children.Add(DialogUiHelpers.CreateLabel("能力收益"));
            panel.Children.Add(DialogUiHelpers.CreateMultiLine(_benefitsTextBox, 70));
            panel.Children.Add(DialogUiHelpers.CreateLabel("修炼时间"));
            panel.Children.Add(_cultivationTimeTextBox);

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 16, 0, 0)
            };
            var okButton = new Button { Content = "确定", Width = 84, Margin = new Thickness(0, 0, 12, 0), IsDefault = true };
            okButton.Click += (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(_nameTextBox.Text))
                {
                    MessageBox.Show("请输入等级名称。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                DialogResult = true;
            };
            var cancelButton = new Button { Content = "取消", Width = 84, IsCancel = true };
            buttons.Children.Add(okButton);
            buttons.Children.Add(cancelButton);
            panel.Children.Add(buttons);
            return panel;
        }
    }

    internal static class DialogUiHelpers
    {
        public static TextBlock CreateLabel(string text)
        {
            return new TextBlock { Text = text, Margin = new Thickness(0, 10, 0, 6), FontWeight = FontWeights.Medium };
        }

        public static TextBox CreateMultiLine(TextBox textBox, double height)
        {
            textBox.AcceptsReturn = true;
            textBox.Height = height;
            textBox.TextWrapping = TextWrapping.Wrap;
            textBox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            return textBox;
        }

        public static void FillIfEmpty(TextBox textBox, string value)
        {
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = value;
            }
        }

        public static string? ExtractFirstLine(string value)
        {
            return value.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim(' ', '-', '*', '•'))
                .FirstOrDefault(line => !string.IsNullOrWhiteSpace(line));
        }
    }

    internal sealed class CultivationMetadata
    {
        public string Grade { get; init; } = "凡级";
        public string Origin { get; init; } = string.Empty;

        public static CultivationMetadata Parse(string? tags)
        {
            var result = new CultivationMetadata();
            if (string.IsNullOrWhiteSpace(tags))
            {
                return result;
            }

            var values = tags.Split(new[] { ';', '；', ',', '，' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .ToList();

            var grade = values.FirstOrDefault(item => item.StartsWith("品级:", StringComparison.OrdinalIgnoreCase));
            var origin = values.FirstOrDefault(item => item.StartsWith("来源:", StringComparison.OrdinalIgnoreCase));

            return new CultivationMetadata
            {
                Grade = grade != null ? grade[(grade.IndexOf(':') + 1)..].Trim() : "凡级",
                Origin = origin != null ? origin[(origin.IndexOf(':') + 1)..].Trim() : string.Empty
            };
        }

        public static string BuildTags(string grade, string origin, string tags)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(grade))
            {
                parts.Add($"品级:{grade.Trim()}");
            }
            if (!string.IsNullOrWhiteSpace(origin))
            {
                parts.Add($"来源:{origin.Trim()}");
            }
            if (!string.IsNullOrWhiteSpace(tags))
            {
                parts.Add(tags.Trim());
            }
            return string.Join(";", parts);
        }
    }
}
