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
    /// 政治体系管理视图
    /// </summary>
    public partial class PoliticalSystemView : UserControl, INavigationRefreshableView
    {
        private readonly PoliticalSystemService _politicalSystemService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PoliticalSystemView> _logger;
        private readonly ProjectContextService? _projectContextService;
        private readonly CurrentProjectGuard? _currentProjectGuard;
        private readonly IAIAssistantService? _aiAssistantService;
        private Guid _currentProjectId;
        private List<PoliticalSystemViewModel> _allPoliticalSystems = new();

        public ObservableCollection<PoliticalSystemViewModel> PoliticalSystems { get; } = new();
        public ObservableCollection<PoliticalPositionViewModel> PoliticalPositions { get; } = new();
        public PoliticalSystemViewModel? SelectedPolitical { get; set; }

        /// <summary>
        /// 初始化政治体系管理视图。
        /// </summary>
        public PoliticalSystemView()
        {
            var serviceProvider = App.ServiceProvider;
            _politicalSystemService = serviceProvider.GetRequiredService<PoliticalSystemService>();
            _unitOfWork = serviceProvider.GetRequiredService<IUnitOfWork>();
            _logger = serviceProvider.GetRequiredService<ILogger<PoliticalSystemView>>();
            _projectContextService = serviceProvider.GetService<ProjectContextService>();
            _currentProjectGuard = serviceProvider.GetService<CurrentProjectGuard>();
            _aiAssistantService = serviceProvider.GetService<IAIAssistantService>();
            _currentProjectId = _projectContextService?.CurrentProjectId ?? Guid.Empty;

            InitializeComponent();
            PoliticalListControl.ItemsSource = PoliticalSystems;
            Loaded += async (_, _) => await LoadPoliticalSystemsAsync();
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

        private async Task LoadPoliticalSystemsAsync()
        {
            try
            {
                if (!EnsureCurrentProject("政治体系管理", out var projectId))
                {
                    _allPoliticalSystems.Clear();
                    PoliticalSystems.Clear();
                    PoliticalPositions.Clear();
                    SelectedPolitical = null;
                    RenderEmptyState();
                    UpdateStatistics();
                    return;
                }

                _currentProjectId = projectId;
                var systems = (await _politicalSystemService.GetPoliticalSystemsByProjectIdAsync(projectId)).ToList();
                var viewModels = new List<PoliticalSystemViewModel>();
                foreach (var system in systems)
                {
                    var withPositions = await _unitOfWork.PoliticalSystems.GetWithPositionsAsync(system.Id);
                    viewModels.Add(ConvertToViewModel(withPositions ?? system));
                }

                _allPoliticalSystems = viewModels;
                ApplyFilters();
                UpdateStatistics();

                if (SelectedPolitical != null)
                {
                    var refreshed = _allPoliticalSystems.FirstOrDefault(p => p.PoliticalSystemId == SelectedPolitical.PoliticalSystemId);
                    if (refreshed != null)
                    {
                        SelectPolitical(refreshed);
                        return;
                    }
                }

                RenderEmptyState();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载政治体系数据失败");
                MessageBox.Show($"加载政治体系数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task RefreshOnProjectChangedAsync(Guid? projectId, string? projectName)
        {
            _currentProjectId = projectId ?? Guid.Empty;
            SelectedPolitical = null;
            await LoadPoliticalSystemsAsync();
        }

        private static PoliticalSystemViewModel ConvertToViewModel(PoliticalSystem system)
        {
            var positions = system.Positions
                .OrderBy(p => p.Level)
                .Select(p => new PoliticalPositionViewModel
                {
                    PositionId = p.Id,
                    Name = p.Name,
                    Level = p.Level,
                    Description = p.Description ?? string.Empty,
                    Requirements = p.Requirements ?? string.Empty,
                    Powers = p.Powers ?? string.Empty,
                    Responsibilities = p.Responsibilities ?? string.Empty,
                    Term = p.Term ?? string.Empty
                })
                .ToList();

            return new PoliticalSystemViewModel
            {
                PoliticalSystemId = system.Id,
                Name = system.Name,
                Type = system.Type,
                Territory = system.Structure ?? string.Empty,
                Description = system.Description ?? string.Empty,
                PositionCount = positions.Count,
                Population = ParsePopulation(system.Tags),
                Capital = system.PowerDistribution ?? string.Empty,
                CreatedAt = system.CreatedAt.ToLocalTime(),
                Hierarchy = system.Hierarchy ?? string.Empty,
                Stability = system.Stability,
                Influence = system.Influence,
                Structure = system.Structure ?? string.Empty,
                PowerDistribution = system.PowerDistribution ?? string.Empty,
                LegalSystem = system.LegalSystem ?? string.Empty,
                ElectionSystem = system.ElectionSystem ?? string.Empty,
                AdministrativeSystem = system.AdministrativeSystem ?? string.Empty,
                MilitarySystem = system.MilitarySystem ?? string.Empty,
                EconomicSystem = system.EconomicSystem ?? string.Empty,
                SocialHierarchy = system.SocialHierarchy ?? string.Empty,
                Notes = system.Notes ?? string.Empty,
                Tags = system.Tags ?? string.Empty,
                Status = system.Status,
                Importance = system.Importance,
                Positions = positions
            };
        }

        private static long ParsePopulation(string? tags)
        {
            if (string.IsNullOrWhiteSpace(tags))
            {
                return 0;
            }

            var marker = "人口:";
            var segment = tags.Split(';', '；').FirstOrDefault(item => item.TrimStart().StartsWith(marker, StringComparison.OrdinalIgnoreCase));
            if (segment == null)
            {
                return 0;
            }

            var value = segment[(segment.IndexOf(':') + 1)..].Trim();
            return long.TryParse(value, out var result) ? result : 0;
        }

        private void UpdateStatistics()
        {
            TotalCountText.Text = PoliticalSystems.Count.ToString();
            SelectedCountText.Text = SelectedPolitical == null ? "0" : "1";
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void TypeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void PoliticalList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PoliticalListControl.SelectedItem is PoliticalSystemViewModel selected)
            {
                SelectPolitical(selected);
            }
        }

        private async void AddPolitical_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!EnsureCurrentProject("新建政治体系", out var projectId))
                {
                    return;
                }

                var dialog = new PoliticalSystemEditDialog(projectId, _aiAssistantService) { Owner = Window.GetWindow(this) };
                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                var system = dialog.BuildPoliticalSystem();
                await _politicalSystemService.CreatePoliticalSystemAsync(system);
                await SavePositionsAsync(system.Id, dialog.GetPositionEntities(system.Id));
                await LoadPoliticalSystemsAsync();

                var created = _allPoliticalSystems.FirstOrDefault(p => p.PoliticalSystemId == system.Id);
                if (created != null)
                {
                    SelectPolitical(created);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建政治体系失败");
                MessageBox.Show($"创建政治体系失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Import_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!EnsureCurrentProject("导入政治体系", out var projectId))
                {
                    return;
                }

                var dialog = new OpenFileDialog
                {
                    Title = "导入政治体系",
                    Filter = "JSON 文件|*.json",
                    CheckFileExists = true
                };

                if (dialog.ShowDialog(Window.GetWindow(this)) != true)
                {
                    return;
                }

                var items = JsonSerializer.Deserialize<List<PoliticalSystemImportModel>>(File.ReadAllText(dialog.FileName))
                    ?? new List<PoliticalSystemImportModel>();
                if (items.Count == 0)
                {
                    MessageBox.Show("未读取到可导入的政治体系。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var existing = (await _politicalSystemService.GetPoliticalSystemsByProjectIdAsync(projectId)).ToList();
                foreach (var item in items)
                {
                    var current = existing.FirstOrDefault(p => string.Equals(p.Name, item.Name, StringComparison.OrdinalIgnoreCase));
                    if (current == null)
                    {
                        current = item.ToEntity(projectId);
                        await _politicalSystemService.CreatePoliticalSystemAsync(current);
                    }
                    else
                    {
                        item.ApplyTo(current);
                        await _politicalSystemService.UpdatePoliticalSystemAsync(current);
                    }

                    await SavePositionsAsync(current.Id, item.ToPositions(current.Id));
                }

                await LoadPoliticalSystemsAsync();
                MessageBox.Show($"已完成导入，共处理 {items.Count} 个政治体系。", "导入完成", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入政治体系失败");
                MessageBox.Show($"导入失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (PoliticalSystems.Count == 0)
                {
                    MessageBox.Show("当前没有可导出的政治体系。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dialog = new SaveFileDialog
                {
                    Title = "导出政治体系",
                    Filter = "JSON 文件|*.json",
                    FileName = $"政治体系_{DateTime.Now:yyyyMMdd_HHmmss}.json",
                    AddExtension = true
                };

                if (dialog.ShowDialog(Window.GetWindow(this)) != true)
                {
                    return;
                }

                var data = PoliticalSystems.Select(PoliticalSystemImportModel.FromViewModel).ToList();
                Directory.CreateDirectory(Path.GetDirectoryName(dialog.FileName)!);
                File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
                MessageBox.Show($"已导出 {data.Count} 个政治体系到：{dialog.FileName}", "导出完成", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出政治体系失败");
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

                if (SelectedPolitical == null)
                {
                    MessageBox.Show("请先选择一个政治体系，再进行 AI 分析。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var result = await _aiAssistantService.GeneratePlotAsync(new Dictionary<string, object>
                {
                    ["plotType"] = "政治体系分析",
                    ["theme"] = SelectedPolitical.Name,
                    ["requirements"] = BuildAiAnalysisPrompt(SelectedPolitical)
                });

                if (!result.IsSuccess || result.Data == null)
                {
                    MessageBox.Show(result.Message ?? "AI分析失败。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ShowAnalysisResultDialog($"AI分析 - {SelectedPolitical.Name}", result.Data.ToString() ?? string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI分析政治体系失败");
                MessageBox.Show($"AI分析失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            var searchText = SearchTextBox.Text?.Trim().ToLowerInvariant() ?? string.Empty;
            var selectedType = (TypeFilterComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "全部类型";

            var result = _allPoliticalSystems.Where(item =>
            {
                var matchesSearch = string.IsNullOrWhiteSpace(searchText) ||
                                    item.Name.ToLowerInvariant().Contains(searchText) ||
                                    item.Description.ToLowerInvariant().Contains(searchText) ||
                                    item.Type.ToLowerInvariant().Contains(searchText) ||
                                    item.Territory.ToLowerInvariant().Contains(searchText);
                var matchesType = selectedType == "全部类型" || string.IsNullOrWhiteSpace(selectedType) || item.Type == selectedType;
                return matchesSearch && matchesType;
            }).ToList();

            PoliticalSystems.Clear();
            foreach (var item in result)
            {
                PoliticalSystems.Add(item);
            }

            UpdateStatistics();
        }

        private void SelectPolitical(PoliticalSystemViewModel political)
        {
            SelectedPolitical = political;
            PoliticalPositions.Clear();
            foreach (var item in political.Positions.OrderBy(p => p.Level))
            {
                PoliticalPositions.Add(item);
            }

            RenderPoliticalDetails(political);
            UpdateStatistics();
        }

        private void RenderEmptyState()
        {
            DetailsPanel.Children.Clear();
            DetailsPanel.Children.Add(new TextBlock
            {
                Text = "请选择一个政治体系查看详情",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 80, 0, 0)
            });
        }

        private void RenderPoliticalDetails(PoliticalSystemViewModel political)
        {
            DetailsPanel.Children.Clear();

            var root = new StackPanel();
            root.Children.Add(new TextBlock
            {
                Text = political.Name,
                FontSize = 26,
                FontWeight = FontWeights.Bold
            });
            root.Children.Add(new TextBlock
            {
                Text = $"{political.Type} | 稳定性 {political.Stability} | 影响力 {political.Influence}",
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
                ("描述", political.Description),
                ("统治领土", political.Territory),
                ("首都/中心", political.Capital),
                ("政治层级", political.Hierarchy),
                ("结构概述", political.Structure),
                ("权力分配", political.PowerDistribution),
                ("法律制度", political.LegalSystem),
                ("选举制度", political.ElectionSystem),
                ("行政体系", political.AdministrativeSystem),
                ("军事体系", political.MilitarySystem),
                ("经济制度", political.EconomicSystem),
                ("社会阶层", political.SocialHierarchy),
                ("人口", political.Population.ToString()),
                ("标签", political.Tags),
                ("备注", political.Notes)
            }));

            root.Children.Add(CreatePositionsCard(political.Positions));
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

        private static Card CreatePositionsCard(IEnumerable<PoliticalPositionViewModel> positions)
        {
            var card = new Card { Margin = new Thickness(0, 0, 0, 16), Padding = new Thickness(16) };
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock
            {
                Text = "政治职位",
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 12)
            });

            var list = positions.ToList();
            if (list.Count == 0)
            {
                panel.Children.Add(new TextBlock { Text = "未配置职位", Foreground = Brushes.Gray });
            }
            else
            {
                foreach (var position in list.OrderBy(p => p.Level))
                {
                    panel.Children.Add(new TextBlock
                    {
                        Text = $"Lv.{position.Level} {position.Name}",
                        FontWeight = FontWeights.Medium
                    });
                    panel.Children.Add(new TextBlock
                    {
                        Text = string.IsNullOrWhiteSpace(position.Description) ? "未填写描述" : position.Description,
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
            if (SelectedPolitical == null || !EnsureCurrentProject("编辑政治体系", out _))
            {
                return;
            }

            var dialog = new PoliticalSystemEditDialog(_currentProjectId, _aiAssistantService, SelectedPolitical) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var entity = await _politicalSystemService.GetPoliticalSystemByIdAsync(SelectedPolitical.PoliticalSystemId);
            if (entity == null)
            {
                MessageBox.Show("未找到要编辑的政治体系。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            dialog.ApplyTo(entity);
            await _politicalSystemService.UpdatePoliticalSystemAsync(entity);
            await SavePositionsAsync(entity.Id, dialog.GetPositionEntities(entity.Id));
            await LoadPoliticalSystemsAsync();
            var updated = _allPoliticalSystems.FirstOrDefault(p => p.PoliticalSystemId == entity.Id);
            if (updated != null)
            {
                SelectPolitical(updated);
            }
        }

        private async Task DeleteSelectedAsync()
        {
            if (SelectedPolitical == null)
            {
                return;
            }

            var result = MessageBox.Show($"确定要删除政治体系“{SelectedPolitical.Name}”吗？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            await _politicalSystemService.DeletePoliticalSystemAsync(SelectedPolitical.PoliticalSystemId);
            SelectedPolitical = null;
            PoliticalPositions.Clear();
            await LoadPoliticalSystemsAsync();
        }

        private async Task AnalyzeSelectedAsync()
        {
            if (SelectedPolitical == null)
            {
                return;
            }

            await Task.Run(() => AIAssistant_Click(this, new RoutedEventArgs()));
        }

        private async Task SavePositionsAsync(Guid politicalSystemId, IReadOnlyList<PoliticalPosition> positions)
        {
            var existingPositions = (await _unitOfWork.PoliticalPositions.GetBySystemIdAsync(politicalSystemId)).ToList();
            foreach (var position in existingPositions)
            {
                await _unitOfWork.PoliticalPositions.DeleteAsync(position);
            }

            foreach (var position in positions)
            {
                await _unitOfWork.PoliticalPositions.AddAsync(position);
            }

            await _unitOfWork.SaveChangesAsync();
        }

        private static string BuildAiAnalysisPrompt(PoliticalSystemViewModel political)
        {
            var builder = new StringBuilder();
            builder.AppendLine("请从世界观设定角度分析以下政治体系，并给出改进建议。");
            builder.AppendLine($"名称：{political.Name}");
            builder.AppendLine($"类型：{political.Type}");
            builder.AppendLine($"领土：{political.Territory}");
            builder.AppendLine($"描述：{political.Description}");
            builder.AppendLine($"政治层级：{political.Hierarchy}");
            builder.AppendLine($"法律制度：{political.LegalSystem}");
            builder.AppendLine($"行政体系：{political.AdministrativeSystem}");
            builder.AppendLine($"军事体系：{political.MilitarySystem}");
            builder.AppendLine($"经济制度：{political.EconomicSystem}");
            builder.AppendLine("请输出优点、隐患、权力冲突点、剧情可用点和完善建议。");
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

    public class PoliticalSystemViewModel
    {
        public Guid PoliticalSystemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Territory { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int PositionCount { get; set; }
        public long Population { get; set; }
        public string Capital { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Hierarchy { get; set; } = string.Empty;
        public int Stability { get; set; }
        public int Influence { get; set; }
        public string Structure { get; set; } = string.Empty;
        public string PowerDistribution { get; set; } = string.Empty;
        public string LegalSystem { get; set; } = string.Empty;
        public string ElectionSystem { get; set; } = string.Empty;
        public string AdministrativeSystem { get; set; } = string.Empty;
        public string MilitarySystem { get; set; } = string.Empty;
        public string EconomicSystem { get; set; } = string.Empty;
        public string SocialHierarchy { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public int Importance { get; set; }
        public List<PoliticalPositionViewModel> Positions { get; set; } = new();
    }

    public class PoliticalPositionViewModel
    {
        public Guid PositionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Requirements { get; set; } = string.Empty;
        public string Powers { get; set; } = string.Empty;
        public string Responsibilities { get; set; } = string.Empty;
        public string Term { get; set; } = string.Empty;
    }

    internal sealed class PoliticalSystemImportModel
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "帝制";
        public string Territory { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public long Population { get; set; }
        public string Capital { get; set; } = string.Empty;
        public string Hierarchy { get; set; } = string.Empty;
        public int Stability { get; set; } = 60;
        public int Influence { get; set; } = 60;
        public string LegalSystem { get; set; } = string.Empty;
        public string ElectionSystem { get; set; } = string.Empty;
        public string AdministrativeSystem { get; set; } = string.Empty;
        public string MilitarySystem { get; set; } = string.Empty;
        public string EconomicSystem { get; set; } = string.Empty;
        public string SocialHierarchy { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public List<PoliticalPositionImportModel> Positions { get; set; } = new();

        public PoliticalSystem ToEntity(Guid projectId)
        {
            return new PoliticalSystem
            {
                ProjectId = projectId,
                Name = Name,
                Type = Type,
                Description = Description,
                Structure = Territory,
                PowerDistribution = Capital,
                Hierarchy = Hierarchy,
                Stability = Stability,
                Influence = Influence,
                LegalSystem = LegalSystem,
                ElectionSystem = ElectionSystem,
                AdministrativeSystem = AdministrativeSystem,
                MilitarySystem = MilitarySystem,
                EconomicSystem = EconomicSystem,
                SocialHierarchy = SocialHierarchy,
                Notes = Notes,
                Tags = BuildTags(Tags, Population),
                Importance = Math.Clamp(Influence / 10, 1, 10),
                Status = "Active"
            };
        }

        public void ApplyTo(PoliticalSystem entity)
        {
            entity.Name = Name;
            entity.Type = Type;
            entity.Description = Description;
            entity.Structure = Territory;
            entity.PowerDistribution = Capital;
            entity.Hierarchy = Hierarchy;
            entity.Stability = Stability;
            entity.Influence = Influence;
            entity.LegalSystem = LegalSystem;
            entity.ElectionSystem = ElectionSystem;
            entity.AdministrativeSystem = AdministrativeSystem;
            entity.MilitarySystem = MilitarySystem;
            entity.EconomicSystem = EconomicSystem;
            entity.SocialHierarchy = SocialHierarchy;
            entity.Notes = Notes;
            entity.Tags = BuildTags(Tags, Population);
            entity.Importance = Math.Clamp(Influence / 10, 1, 10);
        }

        public List<PoliticalPosition> ToPositions(Guid systemId)
        {
            return Positions.Select(item => item.ToEntity(systemId)).ToList();
        }

        public static PoliticalSystemImportModel FromViewModel(PoliticalSystemViewModel viewModel)
        {
            return new PoliticalSystemImportModel
            {
                Name = viewModel.Name,
                Type = viewModel.Type,
                Territory = viewModel.Territory,
                Description = viewModel.Description,
                Population = viewModel.Population,
                Capital = viewModel.Capital,
                Hierarchy = viewModel.Hierarchy,
                Stability = viewModel.Stability,
                Influence = viewModel.Influence,
                LegalSystem = viewModel.LegalSystem,
                ElectionSystem = viewModel.ElectionSystem,
                AdministrativeSystem = viewModel.AdministrativeSystem,
                MilitarySystem = viewModel.MilitarySystem,
                EconomicSystem = viewModel.EconomicSystem,
                SocialHierarchy = viewModel.SocialHierarchy,
                Notes = viewModel.Notes,
                Tags = viewModel.Tags,
                Positions = viewModel.Positions.Select(PoliticalPositionImportModel.FromViewModel).ToList()
            };
        }

        private static string BuildTags(string tags, long population)
        {
            var baseTags = string.IsNullOrWhiteSpace(tags) ? string.Empty : tags.Trim();
            return string.IsNullOrWhiteSpace(baseTags) ? $"人口:{population}" : $"{baseTags};人口:{population}";
        }
    }

    internal sealed class PoliticalPositionImportModel
    {
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Requirements { get; set; } = string.Empty;
        public string Powers { get; set; } = string.Empty;
        public string Responsibilities { get; set; } = string.Empty;
        public string Term { get; set; } = string.Empty;

        public PoliticalPosition ToEntity(Guid systemId)
        {
            return new PoliticalPosition
            {
                PoliticalSystemId = systemId,
                Name = Name,
                Level = Level,
                Description = Description,
                Requirements = Requirements,
                Powers = Powers,
                Responsibilities = Responsibilities,
                Term = Term
            };
        }

        public static PoliticalPositionImportModel FromViewModel(PoliticalPositionViewModel viewModel)
        {
            return new PoliticalPositionImportModel
            {
                Name = viewModel.Name,
                Level = viewModel.Level,
                Description = viewModel.Description,
                Requirements = viewModel.Requirements,
                Powers = viewModel.Powers,
                Responsibilities = viewModel.Responsibilities,
                Term = viewModel.Term
            };
        }
    }

    internal sealed class PoliticalSystemEditDialog : Window
    {
        private readonly Guid _projectId;
        private readonly IAIAssistantService? _aiAssistantService;
        private readonly PoliticalSystemViewModel? _originalModel;
        private readonly TextBox _nameTextBox = new();
        private readonly ComboBox _typeComboBox = new();
        private readonly TextBox _territoryTextBox = new();
        private readonly TextBox _capitalTextBox = new();
        private readonly TextBox _populationTextBox = new();
        private readonly TextBox _descriptionTextBox = new();
        private readonly TextBox _hierarchyTextBox = new();
        private readonly Slider _stabilitySlider = new();
        private readonly Slider _influenceSlider = new();
        private readonly TextBox _legalSystemTextBox = new();
        private readonly TextBox _electionSystemTextBox = new();
        private readonly TextBox _administrativeSystemTextBox = new();
        private readonly TextBox _militarySystemTextBox = new();
        private readonly TextBox _economicSystemTextBox = new();
        private readonly TextBox _socialHierarchyTextBox = new();
        private readonly TextBox _tagsTextBox = new();
        private readonly TextBox _notesTextBox = new();
        private readonly ObservableCollection<PoliticalPositionViewModel> _positions = new();

        public PoliticalSystemEditDialog(Guid projectId, IAIAssistantService? aiAssistantService, PoliticalSystemViewModel? model = null)
        {
            _projectId = projectId;
            _aiAssistantService = aiAssistantService;
            _originalModel = model;

            Title = model == null ? "新建政治体系" : $"编辑政治体系 - {model.Name}";
            Width = 720;
            Height = 860;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            _typeComboBox.ItemsSource = new[] { "帝制", "共和制", "联邦制", "宗教制", "部落制", "城邦制" };
            _typeComboBox.SelectedItem = model?.Type ?? "帝制";
            _nameTextBox.Text = model?.Name ?? string.Empty;
            _territoryTextBox.Text = model?.Territory ?? string.Empty;
            _capitalTextBox.Text = model?.Capital ?? string.Empty;
            _populationTextBox.Text = (model?.Population ?? 0).ToString();
            _descriptionTextBox.Text = model?.Description ?? string.Empty;
            _hierarchyTextBox.Text = model?.Hierarchy ?? string.Empty;
            _stabilitySlider.Minimum = 1;
            _stabilitySlider.Maximum = 100;
            _stabilitySlider.Value = model?.Stability ?? 60;
            _influenceSlider.Minimum = 1;
            _influenceSlider.Maximum = 100;
            _influenceSlider.Value = model?.Influence ?? 60;
            _legalSystemTextBox.Text = model?.LegalSystem ?? string.Empty;
            _electionSystemTextBox.Text = model?.ElectionSystem ?? string.Empty;
            _administrativeSystemTextBox.Text = model?.AdministrativeSystem ?? string.Empty;
            _militarySystemTextBox.Text = model?.MilitarySystem ?? string.Empty;
            _economicSystemTextBox.Text = model?.EconomicSystem ?? string.Empty;
            _socialHierarchyTextBox.Text = model?.SocialHierarchy ?? string.Empty;
            _tagsTextBox.Text = model?.Tags ?? string.Empty;
            _notesTextBox.Text = model?.Notes ?? string.Empty;

            foreach (var position in model?.Positions ?? Enumerable.Empty<PoliticalPositionViewModel>())
            {
                _positions.Add(new PoliticalPositionViewModel
                {
                    PositionId = position.PositionId,
                    Name = position.Name,
                    Level = position.Level,
                    Description = position.Description,
                    Requirements = position.Requirements,
                    Powers = position.Powers,
                    Responsibilities = position.Responsibilities,
                    Term = position.Term
                });
            }

            Content = BuildContent();
        }

        public PoliticalSystem BuildPoliticalSystem()
        {
            return new PoliticalSystem
            {
                ProjectId = _projectId,
                Name = _nameTextBox.Text.Trim(),
                Type = _typeComboBox.SelectedItem?.ToString() ?? "帝制",
                Description = _descriptionTextBox.Text.Trim(),
                Structure = _territoryTextBox.Text.Trim(),
                PowerDistribution = _capitalTextBox.Text.Trim(),
                Hierarchy = _hierarchyTextBox.Text.Trim(),
                Stability = (int)_stabilitySlider.Value,
                Influence = (int)_influenceSlider.Value,
                LegalSystem = _legalSystemTextBox.Text.Trim(),
                ElectionSystem = _electionSystemTextBox.Text.Trim(),
                AdministrativeSystem = _administrativeSystemTextBox.Text.Trim(),
                MilitarySystem = _militarySystemTextBox.Text.Trim(),
                EconomicSystem = _economicSystemTextBox.Text.Trim(),
                SocialHierarchy = _socialHierarchyTextBox.Text.Trim(),
                Tags = BuildTags(),
                Notes = _notesTextBox.Text.Trim(),
                Importance = Math.Clamp((int)_influenceSlider.Value / 10, 1, 10),
                Status = "Active"
            };
        }

        public void ApplyTo(PoliticalSystem system)
        {
            var value = BuildPoliticalSystem();
            system.Name = value.Name;
            system.Type = value.Type;
            system.Description = value.Description;
            system.Structure = value.Structure;
            system.PowerDistribution = value.PowerDistribution;
            system.Hierarchy = value.Hierarchy;
            system.Stability = value.Stability;
            system.Influence = value.Influence;
            system.LegalSystem = value.LegalSystem;
            system.ElectionSystem = value.ElectionSystem;
            system.AdministrativeSystem = value.AdministrativeSystem;
            system.MilitarySystem = value.MilitarySystem;
            system.EconomicSystem = value.EconomicSystem;
            system.SocialHierarchy = value.SocialHierarchy;
            system.Tags = value.Tags;
            system.Notes = value.Notes;
            system.Importance = value.Importance;
        }

        public IReadOnlyList<PoliticalPosition> GetPositionEntities(Guid politicalSystemId)
        {
            return _positions.Select(position => new PoliticalPosition
            {
                PoliticalSystemId = politicalSystemId,
                Name = position.Name,
                Level = position.Level,
                Description = position.Description,
                Requirements = position.Requirements,
                Powers = position.Powers,
                Responsibilities = position.Responsibilities,
                Term = position.Term
            }).ToList();
        }

        private FrameworkElement BuildContent()
        {
            var panel = new StackPanel { Margin = new Thickness(20) };
            panel.Children.Add(CreateLabel("体系名称"));
            panel.Children.Add(_nameTextBox);
            panel.Children.Add(CreateLabel("体系类型"));
            panel.Children.Add(_typeComboBox);
            panel.Children.Add(CreateLabel("统治领土"));
            panel.Children.Add(_territoryTextBox);
            panel.Children.Add(CreateLabel("首都/中心"));
            panel.Children.Add(_capitalTextBox);
            panel.Children.Add(CreateLabel("人口"));
            panel.Children.Add(_populationTextBox);
            panel.Children.Add(CreateLabel("体系描述"));
            panel.Children.Add(CreateMultiLine(_descriptionTextBox, 90));
            panel.Children.Add(CreateLabel("政治层级（逗号分隔）"));
            panel.Children.Add(_hierarchyTextBox);
            panel.Children.Add(CreateLabel("稳定性"));
            panel.Children.Add(_stabilitySlider);
            panel.Children.Add(CreateLabel("影响力"));
            panel.Children.Add(_influenceSlider);
            panel.Children.Add(CreateLabel("法律制度"));
            panel.Children.Add(CreateMultiLine(_legalSystemTextBox, 70));
            panel.Children.Add(CreateLabel("选举制度"));
            panel.Children.Add(CreateMultiLine(_electionSystemTextBox, 70));
            panel.Children.Add(CreateLabel("行政体系"));
            panel.Children.Add(CreateMultiLine(_administrativeSystemTextBox, 70));
            panel.Children.Add(CreateLabel("军事体系"));
            panel.Children.Add(CreateMultiLine(_militarySystemTextBox, 70));
            panel.Children.Add(CreateLabel("经济制度"));
            panel.Children.Add(CreateMultiLine(_economicSystemTextBox, 70));
            panel.Children.Add(CreateLabel("社会阶层"));
            panel.Children.Add(CreateMultiLine(_socialHierarchyTextBox, 70));
            panel.Children.Add(CreateLabel("标签"));
            panel.Children.Add(_tagsTextBox);
            panel.Children.Add(CreateLabel("备注"));
            panel.Children.Add(CreateMultiLine(_notesTextBox, 70));

            panel.Children.Add(CreateLabel("政治职位"));
            var positionsList = new ListBox
            {
                Height = 180,
                ItemsSource = _positions,
                DisplayMemberPath = "Name"
            };
            panel.Children.Add(positionsList);

            var positionButtons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 8, 0, 0)
            };
            var addPositionButton = new Button { Content = "添加职位", Margin = new Thickness(0, 0, 12, 0) };
            addPositionButton.Click += (_, _) =>
            {
                var dialog = new PoliticalPositionEditDialog { Owner = this };
                if (dialog.ShowDialog() == true)
                {
                    _positions.Add(dialog.BuildViewModel());
                }
            };
            var editPositionButton = new Button { Content = "编辑职位", Margin = new Thickness(0, 0, 12, 0) };
            editPositionButton.Click += (_, _) =>
            {
                if (positionsList.SelectedItem is not PoliticalPositionViewModel selected)
                {
                    MessageBox.Show("请先选择一个职位。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dialog = new PoliticalPositionEditDialog(selected) { Owner = this };
                if (dialog.ShowDialog() == true)
                {
                    var index = _positions.IndexOf(selected);
                    _positions[index] = dialog.BuildViewModel();
                }
            };
            var removePositionButton = new Button { Content = "删除职位" };
            removePositionButton.Click += (_, _) =>
            {
                if (positionsList.SelectedItem is PoliticalPositionViewModel selected)
                {
                    _positions.Remove(selected);
                }
            };
            positionButtons.Children.Add(addPositionButton);
            positionButtons.Children.Add(editPositionButton);
            positionButtons.Children.Add(removePositionButton);
            panel.Children.Add(positionButtons);

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
                    MessageBox.Show("请输入政治体系名称。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                DialogResult = true;
            };
            var cancelButton = new Button { Content = "取消", Width = 84, IsCancel = true };
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

            var result = await _aiAssistantService.GeneratePlotAsync(new Dictionary<string, object>
            {
                ["plotType"] = "政治体系设定",
                ["theme"] = _nameTextBox.Text,
                ["requirements"] = $@"请补全一个政治体系设定。
名称：{_nameTextBox.Text}
类型：{_typeComboBox.SelectedItem}
领土：{_territoryTextBox.Text}
描述：{_descriptionTextBox.Text}
请仅按以下字段输出，不要思考过程、解释、Markdown、序号或特殊符号：
名称：
体系描述：
政治层级：
法律制度：
选举制度：
行政体系：
军事体系：
经济制度：
社会阶层：
标签：
备注："
            });

            if (!result.IsSuccess || result.Data == null)
            {
                MessageBox.Show(result.Message ?? "AI自动补全失败。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var text = AiAutoFillFormatter.Normalize(result.Data.ToString());
            AiAutoFillFormatter.FillIfEmpty(_descriptionTextBox, AiAutoFillFormatter.ExtractSummary(text, "体系描述", "描述", "简介"));
            AiAutoFillFormatter.FillSectionIfEmpty(_hierarchyTextBox, text, "政治层级", "层级", "权力结构");
            AiAutoFillFormatter.FillSectionIfEmpty(_legalSystemTextBox, text, "法律制度");
            AiAutoFillFormatter.FillSectionIfEmpty(_electionSystemTextBox, text, "选举制度");
            AiAutoFillFormatter.FillSectionIfEmpty(_administrativeSystemTextBox, text, "行政体系");
            AiAutoFillFormatter.FillSectionIfEmpty(_militarySystemTextBox, text, "军事体系");
            AiAutoFillFormatter.FillSectionIfEmpty(_economicSystemTextBox, text, "经济制度");
            AiAutoFillFormatter.FillSectionIfEmpty(_socialHierarchyTextBox, text, "社会阶层");
            AiAutoFillFormatter.FillSectionIfEmpty(_tagsTextBox, text, "标签");
            AiAutoFillFormatter.FillSectionIfEmpty(_notesTextBox, text, "备注");
            if (string.IsNullOrWhiteSpace(_nameTextBox.Text))
            {
                var generatedName = AiAutoFillFormatter.ExtractSection(text, "名称");
                if (string.IsNullOrWhiteSpace(generatedName))
                {
                    generatedName = AiAutoFillFormatter.ExtractFirstMeaningfulLine(text);
                }

                _nameTextBox.Text = string.IsNullOrWhiteSpace(generatedName) ? "AI政治体系" : generatedName;
            }
            if (string.IsNullOrWhiteSpace(_hierarchyTextBox.Text))
            {
                _hierarchyTextBox.Text = "统治者,核心议会,地方官员";
            }

            MessageBox.Show("已完成政治体系自动补全。", "AI自动补全", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private string BuildTags()
        {
            var tags = _tagsTextBox.Text.Trim();
            var population = _populationTextBox.Text.Trim();
            return string.IsNullOrWhiteSpace(tags) ? $"人口:{population}" : $"{tags};人口:{population}";
        }

        private void ResetForm()
        {
            _typeComboBox.SelectedItem = _originalModel?.Type ?? "帝制";
            _nameTextBox.Text = _originalModel?.Name ?? string.Empty;
            _territoryTextBox.Text = _originalModel?.Territory ?? string.Empty;
            _capitalTextBox.Text = _originalModel?.Capital ?? string.Empty;
            _populationTextBox.Text = (_originalModel?.Population ?? 0).ToString();
            _descriptionTextBox.Text = _originalModel?.Description ?? string.Empty;
            _hierarchyTextBox.Text = _originalModel?.Hierarchy ?? string.Empty;
            _stabilitySlider.Value = _originalModel?.Stability ?? 60;
            _influenceSlider.Value = _originalModel?.Influence ?? 60;
            _legalSystemTextBox.Text = _originalModel?.LegalSystem ?? string.Empty;
            _electionSystemTextBox.Text = _originalModel?.ElectionSystem ?? string.Empty;
            _administrativeSystemTextBox.Text = _originalModel?.AdministrativeSystem ?? string.Empty;
            _militarySystemTextBox.Text = _originalModel?.MilitarySystem ?? string.Empty;
            _economicSystemTextBox.Text = _originalModel?.EconomicSystem ?? string.Empty;
            _socialHierarchyTextBox.Text = _originalModel?.SocialHierarchy ?? string.Empty;
            _tagsTextBox.Text = _originalModel?.Tags ?? string.Empty;
            _notesTextBox.Text = _originalModel?.Notes ?? string.Empty;
        }

        private static TextBlock CreateLabel(string text)
        {
            return new TextBlock { Text = text, Margin = new Thickness(0, 10, 0, 6), FontWeight = FontWeights.Medium };
        }

        private static TextBox CreateMultiLine(TextBox textBox, double height)
        {
            textBox.AcceptsReturn = true;
            textBox.Height = height;
            textBox.TextWrapping = TextWrapping.Wrap;
            textBox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            return textBox;
        }
    }

    internal sealed class PoliticalPositionEditDialog : Window
    {
        private readonly TextBox _nameTextBox = new();
        private readonly TextBox _levelTextBox = new();
        private readonly TextBox _descriptionTextBox = new();
        private readonly TextBox _requirementsTextBox = new();
        private readonly TextBox _powersTextBox = new();
        private readonly TextBox _responsibilitiesTextBox = new();
        private readonly TextBox _termTextBox = new();

        public PoliticalPositionEditDialog(PoliticalPositionViewModel? model = null)
        {
            Title = model == null ? "添加政治职位" : $"编辑职位 - {model.Name}";
            Width = 520;
            Height = 520;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            _nameTextBox.Text = model?.Name ?? string.Empty;
            _levelTextBox.Text = (model?.Level ?? 1).ToString();
            _descriptionTextBox.Text = model?.Description ?? string.Empty;
            _requirementsTextBox.Text = model?.Requirements ?? string.Empty;
            _powersTextBox.Text = model?.Powers ?? string.Empty;
            _responsibilitiesTextBox.Text = model?.Responsibilities ?? string.Empty;
            _termTextBox.Text = model?.Term ?? string.Empty;

            Content = BuildContent();
        }

        public PoliticalPositionViewModel BuildViewModel()
        {
            return new PoliticalPositionViewModel
            {
                Name = _nameTextBox.Text.Trim(),
                Level = int.TryParse(_levelTextBox.Text, out var level) ? level : 1,
                Description = _descriptionTextBox.Text.Trim(),
                Requirements = _requirementsTextBox.Text.Trim(),
                Powers = _powersTextBox.Text.Trim(),
                Responsibilities = _responsibilitiesTextBox.Text.Trim(),
                Term = _termTextBox.Text.Trim()
            };
        }

        private FrameworkElement BuildContent()
        {
            var panel = new StackPanel { Margin = new Thickness(20) };
            panel.Children.Add(CreateLabel("职位名称"));
            panel.Children.Add(_nameTextBox);
            panel.Children.Add(CreateLabel("职位等级"));
            panel.Children.Add(_levelTextBox);
            panel.Children.Add(CreateLabel("职位描述"));
            panel.Children.Add(CreateMultiLine(_descriptionTextBox, 70));
            panel.Children.Add(CreateLabel("任职条件"));
            panel.Children.Add(CreateMultiLine(_requirementsTextBox, 70));
            panel.Children.Add(CreateLabel("职位权力"));
            panel.Children.Add(CreateMultiLine(_powersTextBox, 70));
            panel.Children.Add(CreateLabel("职责"));
            panel.Children.Add(CreateMultiLine(_responsibilitiesTextBox, 70));
            panel.Children.Add(CreateLabel("任期"));
            panel.Children.Add(_termTextBox);

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
                    MessageBox.Show("请输入职位名称。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
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

        private static TextBlock CreateLabel(string text)
        {
            return new TextBlock { Text = text, Margin = new Thickness(0, 10, 0, 6), FontWeight = FontWeights.Medium };
        }

        private static TextBox CreateMultiLine(TextBox textBox, double height)
        {
            textBox.AcceptsReturn = true;
            textBox.Height = height;
            textBox.TextWrapping = TextWrapping.Wrap;
            textBox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            return textBox;
        }
    }
}
