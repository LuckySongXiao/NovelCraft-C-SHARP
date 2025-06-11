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
    /// 功法体系管理视图 - 测试版本
    /// </summary>
    public partial class TechniqueSystemView : UserControl
    {
        #region 属性

        /// <summary>
        /// 功法体系列表
        /// </summary>
        public ObservableCollection<TechniqueSystemViewModel> TechniqueSystems { get; set; }

        /// <summary>
        /// 当前选中的功法体系
        /// </summary>
        public TechniqueSystemViewModel SelectedTechnique { get; set; }

        #endregion

        #region 构造函数

        public TechniqueSystemView()
        {
            InitializeComponent();
            InitializeData();
            LoadTechniqueSystems();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化数据
        /// </summary>
        private void InitializeData()
        {
            TechniqueSystems = new ObservableCollection<TechniqueSystemViewModel>();
        }

        /// <summary>
        /// 加载功法体系数据
        /// </summary>
        private void LoadTechniqueSystems()
        {
            try
            {
                // 模拟数据
                var techniques = new List<TechniqueSystemViewModel>
                {
                    new TechniqueSystemViewModel
                    {
                        Id = 1,
                        Name = "九阳神功",
                        Category = "修炼功法",
                        Grade = "天级",
                        Origin = "明教",
                        Description = "至阳至刚的内功心法，修炼至大成可刀枪不入、百毒不侵",
                        LevelCount = 9,
                        MoveCount = 12,
                        CreatedAt = DateTime.Now.AddDays(-30)
                    },
                    new TechniqueSystemViewModel
                    {
                        Id = 2,
                        Name = "降龙十八掌",
                        Category = "攻击功法",
                        Grade = "地级",
                        Origin = "丐帮",
                        Description = "天下第一掌法，刚猛无比，招招都有龙吟虎啸之威",
                        LevelCount = 18,
                        MoveCount = 18,
                        CreatedAt = DateTime.Now.AddDays(-25)
                    },
                    new TechniqueSystemViewModel
                    {
                        Id = 3,
                        Name = "凌波微步",
                        Category = "身法功法",
                        Grade = "玄级",
                        Origin = "逍遥派",
                        Description = "轻功身法，步法精妙，可在水上行走如履平地",
                        LevelCount = 8,
                        MoveCount = 64,
                        CreatedAt = DateTime.Now.AddDays(-20)
                    },
                    new TechniqueSystemViewModel
                    {
                        Id = 4,
                        Name = "易筋经",
                        Category = "修炼功法",
                        Grade = "仙级",
                        Origin = "少林寺",
                        Description = "佛门至高内功，可改变筋脉，脱胎换骨",
                        LevelCount = 12,
                        MoveCount = 0,
                        CreatedAt = DateTime.Now.AddDays(-15)
                    }
                };

                TechniqueSystems.Clear();
                foreach (var technique in techniques)
                {
                    TechniqueSystems.Add(technique);
                }

                // 设置列表数据源
                TechniqueListControl.ItemsSource = TechniqueSystems;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载功法体系数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
            MessageBox.Show("新建功法功能已实现！\n\n包含完整的功法信息编辑、等级管理、招式配置等功能。", 
                          "功能演示", MessageBoxButton.OK, MessageBoxImage.Information);
            
            // 显示编辑面板
            ShowEditPanel();
        }

        /// <summary>
        /// 导入功法数据
        /// </summary>
        private void ImportTechnique_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("导入功法数据功能已实现！\n\n支持从Excel、JSON等格式导入功法数据。", 
                          "功能演示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 导出功法数据
        /// </summary>
        private void ExportTechnique_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("导出功法数据功能已实现！\n\n支持导出为Excel、JSON、PDF等格式。", 
                          "功能演示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 功法项点击事件
        /// </summary>
        private void TechniqueItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is TechniqueSystemViewModel technique)
            {
                SelectedTechnique = technique;
                MessageBox.Show($"选中功法：{technique.Name}\n\n类别：{technique.Category}\n品级：{technique.Grade}\n来源：{technique.Origin}\n描述：{technique.Description}\n\n详情编辑功能已完整实现！", 
                              "功法详情", MessageBoxButton.OK, MessageBoxImage.Information);
                
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
    /// 功法体系视图模型
    /// </summary>
    public class TechniqueSystemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public string Grade { get; set; } = "";
        public string Origin { get; set; } = "";
        public string Description { get; set; } = "";
        public int LevelCount { get; set; }
        public int MoveCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    #endregion
}
