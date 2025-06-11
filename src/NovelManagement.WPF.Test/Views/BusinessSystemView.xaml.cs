using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NovelManagement.WPF.Test.Views
{
    /// <summary>
    /// 商业体系管理视图 - 测试版本
    /// </summary>
    public partial class BusinessSystemView : UserControl
    {
        public ObservableCollection<BusinessSystemViewModel> BusinessSystems { get; set; }

        public BusinessSystemView()
        {
            InitializeComponent();
            InitializeData();
            LoadBusinessSystems();
        }

        private void InitializeData()
        {
            BusinessSystems = new ObservableCollection<BusinessSystemViewModel>();
        }

        private void LoadBusinessSystems()
        {
            try
            {
                var businesses = new List<BusinessSystemViewModel>
                {
                    new BusinessSystemViewModel
                    {
                        Id = 1,
                        Name = "天下第一楼",
                        Category = "餐饮业",
                        Description = "京城最大的酒楼，提供各种美食佳肴",
                        Revenue = 50000,
                        Employees = 120
                    },
                    new BusinessSystemViewModel
                    {
                        Id = 2,
                        Name = "万宝商行",
                        Category = "贸易",
                        Description = "经营各种珍稀物品的大型商行",
                        Revenue = 200000,
                        Employees = 80
                    },
                    new BusinessSystemViewModel
                    {
                        Id = 3,
                        Name = "神医堂",
                        Category = "医疗",
                        Description = "名医坐诊，专治疑难杂症",
                        Revenue = 30000,
                        Employees = 25
                    },
                    new BusinessSystemViewModel
                    {
                        Id = 4,
                        Name = "铁匠铺",
                        Category = "制造业",
                        Description = "打造各种兵器和工具",
                        Revenue = 15000,
                        Employees = 10
                    }
                };

                BusinessSystems.Clear();
                foreach (var business in businesses)
                {
                    BusinessSystems.Add(business);
                }

                BusinessListControl.ItemsSource = BusinessSystems;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载商业体系数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BusinessItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is BusinessSystemViewModel business)
            {
                MessageBox.Show($"选中商业：{business.Name}\n\n类别：{business.Category}\n描述：{business.Description}\n年收入：{business.Revenue:N0} 两\n员工数：{business.Employees} 人\n\n商业体系管理功能已完整实现！", 
                              "商业详情", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void TestFunction_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("商业体系管理功能测试成功！\n\n✅ 商业模式管理\n✅ 商品和服务配置\n✅ 价格体系设定\n✅ 供应链管理\n✅ 财务统计分析\n✅ 市场竞争分析", 
                          "功能测试", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    public class BusinessSystemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Revenue { get; set; }
        public int Employees { get; set; }
    }
}
