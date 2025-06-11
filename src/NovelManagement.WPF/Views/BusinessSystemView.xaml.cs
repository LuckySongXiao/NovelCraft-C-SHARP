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
    /// 商业体系管理视图
    /// </summary>
    public partial class BusinessSystemView : UserControl
    {
        #region 属性

        /// <summary>
        /// 商业体系列表
        /// </summary>
        public ObservableCollection<BusinessSystemViewModel> BusinessSystems { get; set; }

        /// <summary>
        /// 商品列表
        /// </summary>
        public ObservableCollection<BusinessProductViewModel> BusinessProducts { get; set; }

        /// <summary>
        /// 服务列表
        /// </summary>
        public ObservableCollection<BusinessServiceViewModel> BusinessServices { get; set; }

        /// <summary>
        /// 当前选中的商业体系
        /// </summary>
        public BusinessSystemViewModel SelectedBusiness { get; set; }

        /// <summary>
        /// 选择商业体系命令
        /// </summary>
        public ICommand SelectBusinessCommand { get; private set; }

        /// <summary>
        /// 日志记录器
        /// </summary>
        private readonly ILogger<BusinessSystemView>? _logger;

        /// <summary>
        /// AI助手服务
        /// </summary>
        private readonly IAIAssistantService? _aiAssistantService;

        #endregion

        #region 构造函数

        public BusinessSystemView()
        {
            InitializeComponent();

            // 获取服务
            try
            {
                var serviceProvider = App.ServiceProvider;
                _logger = serviceProvider?.GetService<ILogger<BusinessSystemView>>();
                _aiAssistantService = serviceProvider?.GetService<IAIAssistantService>();
            }
            catch (Exception ex)
            {
                // 如果获取服务失败，记录错误但不影响界面初始化
                System.Diagnostics.Debug.WriteLine($"获取服务失败: {ex.Message}");
            }

            InitializeData();
            InitializeCommands();
            LoadBusinessSystems();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化数据
        /// </summary>
        private void InitializeData()
        {
            BusinessSystems = new ObservableCollection<BusinessSystemViewModel>();
            BusinessProducts = new ObservableCollection<BusinessProductViewModel>();
            BusinessServices = new ObservableCollection<BusinessServiceViewModel>();

            // 设置数据上下文
            this.DataContext = this;
        }

        /// <summary>
        /// 初始化命令
        /// </summary>
        private void InitializeCommands()
        {
            SelectBusinessCommand = new RelayCommand<BusinessSystemViewModel>(SelectBusinessSystem);
        }

        /// <summary>
        /// 加载商业体系数据
        /// </summary>
        private void LoadBusinessSystems()
        {
            try
            {
                // 模拟数据 - 实际应用中应该从服务层获取
                var businesses = new List<BusinessSystemViewModel>
                {
                    new BusinessSystemViewModel
                    {
                        Id = 1,
                        Name = "万宝商行",
                        Category = "贸易商行",
                        Location = "天元城",
                        Owner = "李掌柜",
                        Description = "经营各种修炼资源和法器的大型商行，在各大城市都有分店",
                        ProductCount = 156,
                        ServiceCount = 8,
                        CreatedAt = DateTime.Now.AddDays(-30)
                    },
                    new BusinessSystemViewModel
                    {
                        Id = 2,
                        Name = "天机拍卖行",
                        Category = "拍卖行",
                        Location = "天机城",
                        Owner = "王拍卖师",
                        Description = "专门拍卖珍稀宝物和高级功法的拍卖行，每月举办一次大型拍卖会",
                        ProductCount = 89,
                        ServiceCount = 5,
                        CreatedAt = DateTime.Now.AddDays(-25)
                    },
                    new BusinessSystemViewModel
                    {
                        Id = 3,
                        Name = "神工炼器坊",
                        Category = "炼器坊",
                        Location = "炼器城",
                        Owner = "张炼器师",
                        Description = "专业炼制各种法器和灵器的炼器坊，技艺精湛，声名远播",
                        ProductCount = 67,
                        ServiceCount = 12,
                        CreatedAt = DateTime.Now.AddDays(-20)
                    },
                    new BusinessSystemViewModel
                    {
                        Id = 4,
                        Name = "仙丹阁",
                        Category = "丹药铺",
                        Location = "丹药谷",
                        Owner = "赵丹师",
                        Description = "炼制各种丹药的专业店铺，从基础丹药到高级仙丹应有尽有",
                        ProductCount = 234,
                        ServiceCount = 6,
                        CreatedAt = DateTime.Now.AddDays(-15)
                    }
                };

                BusinessSystems.Clear();
                foreach (var business in businesses)
                {
                    BusinessSystems.Add(business);
                }

                // 设置列表数据源
                BusinessListControl.ItemsSource = BusinessSystems;

                // 更新统计信息
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载商业体系数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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

        #region 搜索和筛选

        /// <summary>
        /// 搜索文本变化事件
        /// </summary>
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterBusinessSystems();
        }

        /// <summary>
        /// 类别筛选变化事件
        /// </summary>
        private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterBusinessSystems();
        }

        /// <summary>
        /// 筛选商业体系
        /// </summary>
        private void FilterBusinessSystems()
        {
            var searchText = SearchTextBox?.Text?.ToLower() ?? "";
            var selectedCategory = (CategoryFilterComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString();

            var filteredBusinesses = BusinessSystems.Where(b =>
                (string.IsNullOrEmpty(searchText) || 
                 b.Name.ToLower().Contains(searchText) || 
                 b.Description.ToLower().Contains(searchText)) &&
                (selectedCategory == "全部类别" || selectedCategory == null || b.Category == selectedCategory)
            ).ToList();

            BusinessListControl.ItemsSource = filteredBusinesses;
        }

        #endregion

        #region 商业体系管理

        /// <summary>
        /// 添加商业体系
        /// </summary>
        private void AddBusiness_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 创建新的商业体系
                SelectedBusiness = new BusinessSystemViewModel
                {
                    Id = 0, // 新建时ID为0
                    Name = "",
                    Category = "贸易商行",
                    Location = "",
                    Owner = "",
                    Description = "",
                    CreatedAt = DateTime.Now
                };

                // 清空商品和服务列表
                BusinessProducts.Clear();
                BusinessServices.Clear();

                // 显示编辑面板
                ShowEditPanel();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"创建商业体系失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 导入商业数据
        /// </summary>
        private void ImportBusiness_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "导入商业体系数据",
                    Filter = "JSON文件|*.json|CSV文件|*.csv|所有文件|*.*",
                    DefaultExt = "json"
                };

                if (dialog.ShowDialog() == true)
                {
                    // 这里应该实现实际的导入逻辑
                    MessageBox.Show($"已选择文件：{dialog.FileName}\n导入功能将在后续版本中完善。",
                        "导入", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 导出商业数据
        /// </summary>
        private void ExportBusiness_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "导出商业体系数据",
                    Filter = "JSON文件|*.json|CSV文件|*.csv|所有文件|*.*",
                    DefaultExt = "json",
                    FileName = $"商业体系数据_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (dialog.ShowDialog() == true)
                {
                    // 这里应该实现实际的导出逻辑
                    MessageBox.Show($"数据将导出到：{dialog.FileName}\n导出功能将在后续版本中完善。",
                        "导出", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 选择商业体系
        /// </summary>
        private void SelectBusinessSystem(BusinessSystemViewModel business)
        {
            if (business == null) return;

            SelectedBusiness = business;
            LoadBusinessSystemDetails(business);
            ShowEditPanel();
        }

        /// <summary>
        /// 加载商业体系详情
        /// </summary>
        private void LoadBusinessSystemDetails(BusinessSystemViewModel business)
        {
            // 填充基本信息
            BusinessNameTextBox.Text = business.Name;
            BusinessDescriptionTextBox.Text = business.Description;
            BusinessLocationTextBox.Text = business.Location;
            BusinessOwnerTextBox.Text = business.Owner;
            
            // 设置商业类别选择
            foreach (ComboBoxItem item in BusinessCategoryComboBox.Items)
            {
                if (item.Content.ToString() == business.Category)
                {
                    BusinessCategoryComboBox.SelectedItem = item;
                    break;
                }
            }

            // 加载商品列表
            LoadBusinessProducts(business.Id);
            
            // 加载服务列表
            LoadBusinessServices(business.Id);
        }

        /// <summary>
        /// 加载商品列表
        /// </summary>
        private void LoadBusinessProducts(int businessId)
        {
            // 模拟数据
            var products = new List<BusinessProductViewModel>
            {
                new BusinessProductViewModel { Name = "筑基丹", Price = "1000灵石", Description = "帮助修炼者突破筑基期的丹药" },
                new BusinessProductViewModel { Name = "飞剑", Price = "5000灵石", Description = "中品法器，锋利无比" }
            };

            BusinessProducts.Clear();
            foreach (var product in products)
            {
                BusinessProducts.Add(product);
            }

            ProductListControl.ItemsSource = BusinessProducts;
        }

        /// <summary>
        /// 加载服务列表
        /// </summary>
        private void LoadBusinessServices(int businessId)
        {
            // 模拟数据
            var services = new List<BusinessServiceViewModel>
            {
                new BusinessServiceViewModel { Name = "法器鉴定", Fee = "100灵石", Description = "鉴定法器品质和价值" },
                new BusinessServiceViewModel { Name = "代为炼制", Fee = "材料费+手工费", Description = "代为炼制各种法器" }
            };

            BusinessServices.Clear();
            foreach (var service in services)
            {
                BusinessServices.Add(service);
            }

            ServiceListControl.ItemsSource = BusinessServices;
        }

        #endregion

        #region 商品管理

        /// <summary>
        /// 添加商品
        /// </summary>
        private void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var newProduct = new BusinessProductViewModel
                {
                    Name = "",
                    Price = "",
                    Description = ""
                };

                BusinessProducts.Add(newProduct);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加商品失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 删除商品
        /// </summary>
        private void RemoveProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.CommandParameter is BusinessProductViewModel product)
                {
                    BusinessProducts.Remove(product);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除商品失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region 服务管理

        /// <summary>
        /// 添加服务
        /// </summary>
        private void AddService_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var newService = new BusinessServiceViewModel
                {
                    Name = "",
                    Fee = "",
                    Description = ""
                };

                BusinessServices.Add(newService);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加服务失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 删除服务
        /// </summary>
        private void RemoveService_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.CommandParameter is BusinessServiceViewModel service)
                {
                    BusinessServices.Remove(service);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除服务失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region 保存和取消

        /// <summary>
        /// 保存商业体系
        /// </summary>
        private void SaveBusiness_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 验证输入
                if (string.IsNullOrWhiteSpace(BusinessNameTextBox.Text))
                {
                    MessageBox.Show("请输入商业模式名称", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 更新商业体系信息
                SelectedBusiness.Name = BusinessNameTextBox.Text.Trim();
                SelectedBusiness.Description = BusinessDescriptionTextBox.Text.Trim();
                SelectedBusiness.Location = BusinessLocationTextBox.Text.Trim();
                SelectedBusiness.Owner = BusinessOwnerTextBox.Text.Trim();
                SelectedBusiness.Category = (BusinessCategoryComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "贸易商行";
                SelectedBusiness.ProductCount = BusinessProducts.Count;
                SelectedBusiness.ServiceCount = BusinessServices.Count;

                // 如果是新建商业体系，添加到列表
                if (SelectedBusiness.Id == 0)
                {
                    SelectedBusiness.Id = BusinessSystems.Count > 0 ? BusinessSystems.Max(b => b.Id) + 1 : 1;
                    BusinessSystems.Add(SelectedBusiness);
                }

                // 这里应该调用服务层保存数据
                MessageBox.Show("商业体系保存成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);

                // 刷新列表
                FilterBusinessSystems();
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存商业体系失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                BusinessNameTextBox.Text = "";
                BusinessDescriptionTextBox.Text = "";
                BusinessLocationTextBox.Text = "";
                BusinessOwnerTextBox.Text = "";
                BusinessCategoryComboBox.SelectedIndex = 0;

                // 清空列表
                BusinessProducts.Clear();
                BusinessServices.Clear();

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
            var dialog = new AIAssistantDialog("商业体系管理", contextString);
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

            // 添加统计信息
            var categoryStats = BusinessSystems.GroupBy(b => b.Category)
                .ToDictionary(g => g.Key, g => g.Count());
            context["categoryStatistics"] = categoryStats;

            return context;
        }

        #endregion
    }

    #region ViewModel类

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

    #endregion
}
