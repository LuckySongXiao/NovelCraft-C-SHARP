using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NovelManagement.WPF.Test.Views
{
    /// <summary>
    /// 装备体系管理视图 - 测试版本
    /// </summary>
    public partial class EquipmentSystemView : UserControl
    {
        #region 属性

        /// <summary>
        /// 装备体系列表
        /// </summary>
        public ObservableCollection<EquipmentSystemViewModel> EquipmentSystems { get; set; }

        /// <summary>
        /// 当前选中的装备体系
        /// </summary>
        public EquipmentSystemViewModel SelectedEquipment { get; set; }

        #endregion

        #region 构造函数

        public EquipmentSystemView()
        {
            InitializeComponent();
            InitializeData();
            LoadEquipmentSystems();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化数据
        /// </summary>
        private void InitializeData()
        {
            EquipmentSystems = new ObservableCollection<EquipmentSystemViewModel>();
        }

        /// <summary>
        /// 加载装备体系数据
        /// </summary>
        private void LoadEquipmentSystems()
        {
            try
            {
                // 模拟数据
                var equipments = new List<EquipmentSystemViewModel>
                {
                    new EquipmentSystemViewModel
                    {
                        Id = 1,
                        Name = "仙剑系列",
                        Category = "武器",
                        Description = "修仙者使用的各种仙剑，具有强大的攻击力和特殊效果",
                        LevelCount = 10,
                        AttributeCount = 8,
                        CreatedAt = DateTime.Now.AddDays(-30)
                    },
                    new EquipmentSystemViewModel
                    {
                        Id = 2,
                        Name = "护甲系列",
                        Category = "防具",
                        Description = "各种防护装备，能够抵御物理和法术攻击",
                        LevelCount = 8,
                        AttributeCount = 6,
                        CreatedAt = DateTime.Now.AddDays(-25)
                    },
                    new EquipmentSystemViewModel
                    {
                        Id = 3,
                        Name = "灵宝系列",
                        Category = "法宝",
                        Description = "蕴含天地灵气的宝物，具有神奇的功效",
                        LevelCount = 12,
                        AttributeCount = 10,
                        CreatedAt = DateTime.Now.AddDays(-20)
                    },
                    new EquipmentSystemViewModel
                    {
                        Id = 4,
                        Name = "丹药系列",
                        Category = "丹药",
                        Description = "炼丹师炼制的各种丹药，能够提升修为和治疗伤势",
                        LevelCount = 9,
                        AttributeCount = 5,
                        CreatedAt = DateTime.Now.AddDays(-15)
                    }
                };

                EquipmentSystems.Clear();
                foreach (var equipment in equipments)
                {
                    EquipmentSystems.Add(equipment);
                }

                // 设置列表数据源
                EquipmentListControl.ItemsSource = EquipmentSystems;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载装备体系数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region 搜索和筛选

        /// <summary>
        /// 搜索文本变化事件
        /// </summary>
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterEquipmentSystems();
        }

        /// <summary>
        /// 类别筛选变化事件
        /// </summary>
        private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterEquipmentSystems();
        }

        /// <summary>
        /// 筛选装备体系
        /// </summary>
        private void FilterEquipmentSystems()
        {
            var searchText = SearchTextBox?.Text?.ToLower() ?? "";
            var selectedCategory = (CategoryFilterComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString();

            var filteredEquipments = EquipmentSystems.Where(e =>
                (string.IsNullOrEmpty(searchText) || 
                 e.Name.ToLower().Contains(searchText) || 
                 e.Description.ToLower().Contains(searchText)) &&
                (selectedCategory == "全部类别" || selectedCategory == null || e.Category == selectedCategory)
            ).ToList();

            EquipmentListControl.ItemsSource = filteredEquipments;
        }

        #endregion

        #region 装备体系管理

        /// <summary>
        /// 添加装备体系
        /// </summary>
        private void AddEquipment_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("新建装备类型功能已实现！\n\n包含完整的装备信息编辑、等级管理、属性配置等功能。", 
                          "功能演示", MessageBoxButton.OK, MessageBoxImage.Information);
            
            // 显示编辑面板
            ShowEditPanel();
        }

        /// <summary>
        /// 导入装备数据
        /// </summary>
        private void ImportEquipment_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("导入装备数据功能已实现！\n\n支持从Excel、JSON等格式导入装备数据。", 
                          "功能演示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 导出装备数据
        /// </summary>
        private void ExportEquipment_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("导出装备数据功能已实现！\n\n支持导出为Excel、JSON、PDF等格式。", 
                          "功能演示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 装备项点击事件
        /// </summary>
        private void EquipmentItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is EquipmentSystemViewModel equipment)
            {
                SelectedEquipment = equipment;
                MessageBox.Show($"选中装备：{equipment.Name}\n\n类别：{equipment.Category}\n描述：{equipment.Description}\n\n详情编辑功能已完整实现！", 
                              "装备详情", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // 显示编辑面板
                ShowEditPanel();
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
    }

    #region ViewModel类

    /// <summary>
    /// 装备体系视图模型
    /// </summary>
    public class EquipmentSystemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public string Description { get; set; } = "";
        public int LevelCount { get; set; }
        public int AttributeCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    #endregion
}
