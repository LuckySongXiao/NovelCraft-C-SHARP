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
    /// 商业体系管理视图
    /// </summary>
    public partial class BusinessSystemView : UserControl, INavigationRefreshableView, INavigationAwareView
    {
        private readonly ILogger<BusinessSystemView>? _logger;
        private readonly IAIAssistantService? _aiAssistantService;
        private readonly BusinessDataService? _businessDataService;
        private readonly ProjectContextService? _projectContextService;
        private readonly CurrentProjectGuard? _currentProjectGuard;
        private readonly ProjectReadModelService? _projectReadModelService;
        private Guid _currentProjectId;

        public ObservableCollection<BusinessSystemViewModel> BusinessSystems { get; } = new();
        public ObservableCollection<BusinessProductViewModel> BusinessProducts { get; } = new();
        public ObservableCollection<BusinessServiceViewModel> BusinessServices { get; } = new();
        public BusinessSystemViewModel? SelectedBusiness { get; set; }
        public ICommand SelectBusinessCommand { get; }

        public int TotalCount => BusinessSystems.Count;
        public int TradeCount => BusinessSystems.Count(system => system.Category == "贸易商行");
        public int AuctionCount => BusinessSystems.Count(system => system.Category == "拍卖行");
        public int CraftCount => BusinessSystems.Count(system => system.Category == "炼器坊");

        /// <summary>
        /// 初始化商业体系管理视图。
        /// </summary>
        public BusinessSystemView()
        {
            InitializeComponent();

            try
            {
                var serviceProvider = App.ServiceProvider;
                _logger = serviceProvider?.GetService<ILogger<BusinessSystemView>>();
                _aiAssistantService = serviceProvider?.GetService<IAIAssistantService>();
                _businessDataService = serviceProvider?.GetService<BusinessDataService>();
                _projectContextService = serviceProvider?.GetService<ProjectContextService>();
                _currentProjectGuard = serviceProvider?.GetService<CurrentProjectGuard>();
                _projectReadModelService = serviceProvider?.GetService<ProjectReadModelService>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取服务失败: {ex.Message}");
            }

            SelectBusinessCommand = new RelayCommand<BusinessSystemViewModel>(SelectBusinessSystem);
            DataContext = this;
            ProductListControl.ItemsSource = BusinessProducts;
            ServiceListControl.ItemsSource = BusinessServices;
            _ = LoadBusinessSystemsAsync();
        }

        private async Task LoadBusinessSystemsAsync()
        {
            try
            {
                _currentProjectId = _projectContextService?.CurrentProjectId ?? Guid.Empty;
                if (_currentProjectId == Guid.Empty)
                {
                    _currentProjectGuard?.TryGetCurrentProjectId(Window.GetWindow(this), "商业体系管理", out _);
                    BusinessSystems.Clear();
                    BusinessProducts.Clear();
                    BusinessServices.Clear();
                    BusinessListControl.ItemsSource = BusinessSystems;
                    UpdateStatistics();
                    HideEditPanel();
                    return;
                }

                var businesses = _businessDataService == null
                    ? new List<BusinessSystemViewModel>()
                    : await _businessDataService.LoadBusinessSystemsAsync(_currentProjectId);

                BusinessSystems.Clear();
                foreach (var business in businesses.OrderBy(system => system.Name))
                {
                    business.Products ??= new List<BusinessProductViewModel>();
                    business.Services ??= new List<BusinessServiceViewModel>();
                    business.ProductCount = business.Products.Count;
                    business.ServiceCount = business.Services.Count;
                    BusinessSystems.Add(business);
                }

                BusinessListControl.ItemsSource = BusinessSystems;
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载商业体系数据失败");
                MessageBox.Show($"加载商业体系数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStatistics()
        {
            BusinessListControl.Items.Refresh();
            DataContext = null;
            DataContext = this;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterBusinessSystems();
        }

        private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterBusinessSystems();
        }

        private void FilterBusinessSystems()
        {
            var searchText = SearchTextBox?.Text?.ToLowerInvariant() ?? string.Empty;
            var selectedCategory = (CategoryFilterComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString();

            var filteredBusinesses = BusinessSystems.Where(system =>
                (string.IsNullOrWhiteSpace(searchText) ||
                 system.Name.ToLowerInvariant().Contains(searchText) ||
                 system.Description.ToLowerInvariant().Contains(searchText) ||
                 system.Location.ToLowerInvariant().Contains(searchText)) &&
                (selectedCategory == "全部类别" || selectedCategory == null || system.Category == selectedCategory))
                .ToList();

            BusinessListControl.ItemsSource = filteredBusinesses;
        }

        private void AddBusiness_Click(object sender, RoutedEventArgs e)
        {
            SelectedBusiness = new BusinessSystemViewModel
            {
                Category = "贸易商行",
                CreatedAt = DateTime.Now,
                Products = new List<BusinessProductViewModel>(),
                Services = new List<BusinessServiceViewModel>()
            };

            BusinessProducts.Clear();
            BusinessServices.Clear();
            ShowEditPanel();
            BusinessNameTextBox.Focus();
        }

        private async void ImportBusiness_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!EnsureCurrentProject("导入商业体系"))
                {
                    return;
                }

                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "导入商业体系数据",
                    Filter = "JSON文件|*.json|所有文件|*.*",
                    DefaultExt = "json"
                };

                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                if (_businessDataService == null)
                {
                    MessageBox.Show("商业数据服务未初始化。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var importedBusinesses = await _businessDataService.ImportBusinessSystemsAsync(dialog.FileName);
                BusinessSystems.Clear();
                foreach (var business in importedBusinesses)
                {
                    business.Products ??= new List<BusinessProductViewModel>();
                    business.Services ??= new List<BusinessServiceViewModel>();
                    business.ProductCount = business.Products.Count;
                    business.ServiceCount = business.Services.Count;
                    BusinessSystems.Add(business);
                }

                await PersistBusinessSystemsAsync();
                FilterBusinessSystems();
                UpdateStatistics();
                MessageBox.Show($"已成功导入 {BusinessSystems.Count} 个商业体系。", "导入成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "导入商业体系失败");
                MessageBox.Show($"导入失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExportBusiness_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!EnsureCurrentProject("导出商业体系"))
                {
                    return;
                }

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "导出商业体系数据",
                    Filter = "JSON文件|*.json",
                    DefaultExt = "json",
                    FileName = $"商业体系数据_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                if (_businessDataService == null)
                {
                    MessageBox.Show("商业数据服务未初始化。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                await _businessDataService.ExportBusinessSystemsAsync(_currentProjectId, BusinessSystems, dialog.FileName);
                MessageBox.Show($"商业体系数据已导出到：{dialog.FileName}", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "导出商业体系失败");
                MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectBusinessSystem(BusinessSystemViewModel? business)
        {
            if (business == null)
            {
                return;
            }

            SelectedBusiness = business;
            LoadBusinessSystemDetails(business);
            ShowEditPanel();
        }

        private void LoadBusinessSystemDetails(BusinessSystemViewModel business)
        {
            BusinessNameTextBox.Text = business.Name;
            BusinessDescriptionTextBox.Text = business.Description;
            BusinessLocationTextBox.Text = business.Location;
            BusinessOwnerTextBox.Text = business.Owner;

            foreach (ComboBoxItem item in BusinessCategoryComboBox.Items)
            {
                if (item.Content?.ToString() == business.Category)
                {
                    BusinessCategoryComboBox.SelectedItem = item;
                    break;
                }
            }

            LoadBusinessProducts(business);
            LoadBusinessServices(business);
        }

        private void LoadBusinessProducts(BusinessSystemViewModel business)
        {
            BusinessProducts.Clear();
            foreach (var product in business.Products ?? Enumerable.Empty<BusinessProductViewModel>())
            {
                BusinessProducts.Add(new BusinessProductViewModel
                {
                    Name = product.Name,
                    Price = product.Price,
                    Description = product.Description
                });
            }

            ProductListControl.ItemsSource = BusinessProducts;
        }

        private void LoadBusinessServices(BusinessSystemViewModel business)
        {
            BusinessServices.Clear();
            foreach (var service in business.Services ?? Enumerable.Empty<BusinessServiceViewModel>())
            {
                BusinessServices.Add(new BusinessServiceViewModel
                {
                    Name = service.Name,
                    Fee = service.Fee,
                    Description = service.Description
                });
            }

            ServiceListControl.ItemsSource = BusinessServices;
        }

        private void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            BusinessProducts.Add(new BusinessProductViewModel());
        }

        private void RemoveProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { CommandParameter: BusinessProductViewModel product })
            {
                BusinessProducts.Remove(product);
            }
        }

        private void AddService_Click(object sender, RoutedEventArgs e)
        {
            BusinessServices.Add(new BusinessServiceViewModel());
        }

        private void RemoveService_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { CommandParameter: BusinessServiceViewModel service })
            {
                BusinessServices.Remove(service);
            }
        }

        private async void SaveBusiness_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SelectedBusiness == null)
                {
                    return;
                }

                if (!EnsureCurrentProject("保存商业体系"))
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(BusinessNameTextBox.Text))
                {
                    MessageBox.Show("请输入商业模式名称", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                SelectedBusiness.Name = BusinessNameTextBox.Text.Trim();
                SelectedBusiness.Description = BusinessDescriptionTextBox.Text.Trim();
                SelectedBusiness.Location = BusinessLocationTextBox.Text.Trim();
                SelectedBusiness.Owner = BusinessOwnerTextBox.Text.Trim();
                SelectedBusiness.Category = (BusinessCategoryComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "贸易商行";
                SelectedBusiness.Products = BusinessProducts
                    .Where(product => !string.IsNullOrWhiteSpace(product.Name))
                    .Select(product => new BusinessProductViewModel
                    {
                        Name = product.Name.Trim(),
                        Price = product.Price.Trim(),
                        Description = product.Description.Trim()
                    })
                    .ToList();
                SelectedBusiness.Services = BusinessServices
                    .Where(service => !string.IsNullOrWhiteSpace(service.Name))
                    .Select(service => new BusinessServiceViewModel
                    {
                        Name = service.Name.Trim(),
                        Fee = service.Fee.Trim(),
                        Description = service.Description.Trim()
                    })
                    .ToList();
                SelectedBusiness.ProductCount = SelectedBusiness.Products.Count;
                SelectedBusiness.ServiceCount = SelectedBusiness.Services.Count;

                if (SelectedBusiness.Id == 0)
                {
                    SelectedBusiness.Id = BusinessSystems.Count > 0 ? BusinessSystems.Max(system => system.Id) + 1 : 1;
                    BusinessSystems.Add(SelectedBusiness);
                }

                await PersistBusinessSystemsAsync();
                MessageBox.Show("商业体系保存成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                FilterBusinessSystems();
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存商业体系失败");
                MessageBox.Show($"保存商业体系失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelEdit_Click(object sender, RoutedEventArgs e)
        {
            HideEditPanel();
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
            SelectedBusiness = null;
            BusinessProducts.Clear();
            BusinessServices.Clear();
        }

        private async void AIAssistant_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!EnsureCurrentProject("AI商业体系"))
                {
                    return;
                }

                if (_aiAssistantService == null)
                {
                    ShowAIAssistantDialog();
                    return;
                }

                if (SelectedBusiness != null)
                {
                    var choice = MessageBox.Show(
                        "是：AI优化当前商业体系并保存\n否：AI生成新的商业体系并保存\n取消：打开原始AI助手",
                        "AI商业体系",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);

                    if (choice == MessageBoxResult.Cancel)
                    {
                        ShowAIAssistantDialog();
                        return;
                    }

                    await GenerateBusinessWithAiAsync(choice == MessageBoxResult.Yes);
                    return;
                }

                var generateChoice = MessageBox.Show(
                    "是：AI生成新的商业体系并保存\n否：打开原始AI助手",
                    "AI商业体系",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (generateChoice == MessageBoxResult.Yes)
                {
                    await GenerateBusinessWithAiAsync(false);
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
            var dialog = new AIAssistantDialog("商业体系管理", contextString)
            {
                Owner = Window.GetWindow(this)
            };
            dialog.ShowDialog();
        }

        private Dictionary<string, object> GetCurrentContext()
        {
            var context = new Dictionary<string, object>
            {
                ["interfaceType"] = "商业体系管理",
                ["totalBusinessSystems"] = BusinessSystems.Count,
                ["selectedBusiness"] = SelectedBusiness,
                ["businessCategories"] = new[] { "贸易商行", "拍卖行", "炼器坊", "丹药铺", "客栈", "传送阵" }
            };

            if (SelectedBusiness != null)
            {
                context["currentBusinessName"] = SelectedBusiness.Name;
                context["currentBusinessCategory"] = SelectedBusiness.Category;
                context["currentBusinessLocation"] = SelectedBusiness.Location;
                context["currentBusinessOwner"] = SelectedBusiness.Owner;
                context["currentBusinessDescription"] = SelectedBusiness.Description;
                context["productCount"] = SelectedBusiness.ProductCount;
                context["serviceCount"] = SelectedBusiness.ServiceCount;
                context["businessProducts"] = BusinessProducts.ToList();
                context["businessServices"] = BusinessServices.ToList();
            }

            context["categoryStatistics"] = BusinessSystems.GroupBy(system => system.Category).ToDictionary(group => group.Key, group => group.Count());
            return context;
        }

        public async Task RefreshOnProjectChangedAsync(Guid? projectId, string? projectName)
        {
            _currentProjectId = projectId ?? Guid.Empty;
            await LoadBusinessSystemsAsync();
        }

        public void OnNavigatedTo(NavigationContext context)
        {
            _currentProjectId = context.ProjectId ?? Guid.Empty;
            _ = LoadBusinessSystemsAsync();
        }

        private async Task PersistBusinessSystemsAsync()
        {
            if (_currentProjectId == Guid.Empty || _businessDataService == null)
            {
                return;
            }

            foreach (var business in BusinessSystems)
            {
                business.Products ??= new List<BusinessProductViewModel>();
                business.Services ??= new List<BusinessServiceViewModel>();
                business.ProductCount = business.Products.Count;
                business.ServiceCount = business.Services.Count;
            }

            await _businessDataService.SaveBusinessSystemsAsync(_currentProjectId, BusinessSystems);
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

        private async Task<string> BuildProjectConstraintsAsync()
        {
            if (_projectReadModelService == null || _currentProjectId == Guid.Empty)
            {
                return string.Empty;
            }

            var constraints = await _projectReadModelService.BuildSubsystemPromptContextAsync(_currentProjectId, "商业体系");
            return string.IsNullOrWhiteSpace(constraints) ? string.Empty : constraints + Environment.NewLine;
        }

        private async Task GenerateBusinessWithAiAsync(bool optimizeCurrent)
        {
            if (_aiAssistantService == null)
            {
                MessageBox.Show("AI助手服务未初始化。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var projectConstraints = await BuildProjectConstraintsAsync();
            var parameters = new Dictionary<string, object>
            {
                ["title"] = optimizeCurrent && SelectedBusiness != null ? $"优化商业体系：{SelectedBusiness.Name}" : "生成商业体系",
                ["theme"] = "请生成一个适合书籍项目使用的商业体系，并输出名称、类别、地点、经营者、描述、商品列表和服务列表。",
                ["requirements"] = projectConstraints + (optimizeCurrent && SelectedBusiness != null
                    ? $"请基于当前商业体系进行优化并输出结构化文本。当前体系：{SelectedBusiness.Name}，类别：{SelectedBusiness.Category}，地点：{SelectedBusiness.Location}，经营者：{SelectedBusiness.Owner}，描述：{SelectedBusiness.Description}"
                    : "请输出一个完整商业体系，至少包含名称、类别、地点、经营者、描述、至少3个商品、至少2项服务。"),
                ["context"] = GetCurrentContext()
            };

            var result = await _aiAssistantService.GenerateOutlineAsync(parameters);
            if (!result.IsSuccess || result.Data == null)
            {
                MessageBox.Show(result.Message ?? "AI生成失败。", "AI商业体系", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var generatedBusiness = ParseBusinessFromAiResult(result.Data, optimizeCurrent ? SelectedBusiness : null);
            if (generatedBusiness == null)
            {
                MessageBox.Show("AI结果无法解析为商业体系。", "AI商业体系", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (optimizeCurrent && SelectedBusiness != null)
            {
                generatedBusiness.Id = SelectedBusiness.Id;
                generatedBusiness.CreatedAt = SelectedBusiness.CreatedAt;
                var index = BusinessSystems.IndexOf(SelectedBusiness);
                if (index >= 0)
                {
                    BusinessSystems[index] = generatedBusiness;
                }
                SelectedBusiness = generatedBusiness;
            }
            else
            {
                generatedBusiness.Id = BusinessSystems.Count > 0 ? BusinessSystems.Max(system => system.Id) + 1 : 1;
                generatedBusiness.CreatedAt = DateTime.Now;
                BusinessSystems.Add(generatedBusiness);
                SelectedBusiness = generatedBusiness;
            }

            await PersistBusinessSystemsAsync();
            FilterBusinessSystems();
            UpdateStatistics();
            LoadBusinessSystemDetails(generatedBusiness);
            ShowEditPanel();
            MessageBox.Show(
                optimizeCurrent ? $"已使用 AI 优化并保存商业体系：{generatedBusiness.Name}" : $"已使用 AI 生成并保存商业体系：{generatedBusiness.Name}",
                "AI商业体系",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private BusinessSystemViewModel? ParseBusinessFromAiResult(object data, BusinessSystemViewModel? baseBusiness)
        {
            var text = data?.ToString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var business = new BusinessSystemViewModel
            {
                Id = baseBusiness?.Id ?? 0,
                Name = ExtractField(text, "名称") ?? baseBusiness?.Name ?? ExtractFirstMeaningfulLine(text) ?? "AI生成商业体系",
                Category = ExtractField(text, "类别") ?? baseBusiness?.Category ?? "贸易商行",
                Location = ExtractField(text, "地点") ?? baseBusiness?.Location ?? "主城商圈",
                Owner = ExtractField(text, "经营者") ?? baseBusiness?.Owner ?? "AI经营者",
                Description = ExtractField(text, "描述") ?? baseBusiness?.Description ?? text.Trim(),
                CreatedAt = baseBusiness?.CreatedAt ?? DateTime.Now,
                Products = ParseProducts(text),
                Services = ParseServices(text)
            };

            if (business.Products.Count == 0)
            {
                business.Products = baseBusiness?.Products?.ToList() ?? new List<BusinessProductViewModel>
                {
                    new() { Name = "灵石补给包", Price = "500灵石", Description = "常用修炼资源组合" },
                    new() { Name = "制式法器", Price = "2000灵石", Description = "标准化常备法器" },
                    new() { Name = "丹药礼盒", Price = "3500灵石", Description = "适合新晋修士的丹药组合" }
                };
            }

            if (business.Services.Count == 0)
            {
                business.Services = baseBusiness?.Services?.ToList() ?? new List<BusinessServiceViewModel>
                {
                    new() { Name = "鉴宝服务", Fee = "200灵石", Description = "评估宝物品质与流通价值" },
                    new() { Name = "寄售服务", Fee = "成交额抽成", Description = "代售稀有商品与修炼资源" }
                };
            }

            business.ProductCount = business.Products.Count;
            business.ServiceCount = business.Services.Count;
            return business;
        }

        private static List<BusinessProductViewModel> ParseProducts(string text)
        {
            var products = new List<BusinessProductViewModel>();
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (!Regex.IsMatch(line, "丹|器|符|石|药|宝|材料|商品|货物"))
                {
                    continue;
                }

                products.Add(new BusinessProductViewModel
                {
                    Name = TrimListMarker(line),
                    Price = "待定",
                    Description = line
                });

                if (products.Count >= 8)
                {
                    break;
                }
            }

            return products;
        }

        private static List<BusinessServiceViewModel> ParseServices(string text)
        {
            var services = new List<BusinessServiceViewModel>();
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (!Regex.IsMatch(line, "服务|鉴定|寄售|拍卖|炼制|定制|配送|维修"))
                {
                    continue;
                }

                services.Add(new BusinessServiceViewModel
                {
                    Name = TrimListMarker(line),
                    Fee = "待定",
                    Description = line
                });

                if (services.Count >= 8)
                {
                    break;
                }
            }

            return services;
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
    /// 商业体系视图模型
    /// </summary>
    public class BusinessSystemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public string Location { get; set; } = "";
        public string Owner { get; set; } = "";
        public string Description { get; set; } = "";
        public int ProductCount { get; set; }
        public int ServiceCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<BusinessProductViewModel> Products { get; set; } = new();
        public List<BusinessServiceViewModel> Services { get; set; } = new();
    }

    /// <summary>
    /// 商品视图模型
    /// </summary>
    public class BusinessProductViewModel
    {
        public string Name { get; set; } = "";
        public string Price { get; set; } = "";
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// 服务视图模型
    /// </summary>
    public class BusinessServiceViewModel
    {
        public string Name { get; set; } = "";
        public string Fee { get; set; } = "";
        public string Description { get; set; } = "";
    }
}
