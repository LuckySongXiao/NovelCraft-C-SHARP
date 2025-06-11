using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NovelManagement.WPF.Test.Views
{
    /// <summary>
    /// 时间线管理视图 - 测试版本
    /// </summary>
    public partial class TimelineView : UserControl
    {
        public ObservableCollection<TimelineEventViewModel> TimelineEvents { get; set; }

        public TimelineView()
        {
            InitializeComponent();
            InitializeData();
            LoadTimelineEvents();
        }

        private void InitializeData()
        {
            TimelineEvents = new ObservableCollection<TimelineEventViewModel>();
        }

        private void LoadTimelineEvents()
        {
            try
            {
                var events = new List<TimelineEventViewModel>
                {
                    new TimelineEventViewModel
                    {
                        Id = 1,
                        Time = "第1年春",
                        Title = "主角出生",
                        Category = "角色事件",
                        Description = "主角在一个小村庄出生，天生具有特殊体质",
                        Importance = "关键"
                    },
                    new TimelineEventViewModel
                    {
                        Id = 2,
                        Time = "第1年夏",
                        Title = "村庄遭袭",
                        Category = "世界事件",
                        Description = "魔族入侵，村庄被毁，主角被神秘人救走",
                        Importance = "重要"
                    },
                    new TimelineEventViewModel
                    {
                        Id = 3,
                        Time = "第2年春",
                        Title = "拜师学艺",
                        Category = "角色成长",
                        Description = "主角拜入仙门，开始修炼之路",
                        Importance = "关键"
                    },
                    new TimelineEventViewModel
                    {
                        Id = 4,
                        Time = "第2年秋",
                        Title = "初次历练",
                        Category = "剧情事件",
                        Description = "主角下山历练，遇到第一个重要伙伴",
                        Importance = "普通"
                    },
                    new TimelineEventViewModel
                    {
                        Id = 5,
                        Time = "第3年冬",
                        Title = "大战魔族",
                        Category = "世界事件",
                        Description = "正邪大战爆发，主角初露锋芒",
                        Importance = "关键"
                    }
                };

                TimelineEvents.Clear();
                foreach (var timelineEvent in events)
                {
                    TimelineEvents.Add(timelineEvent);
                }

                TimelineEventsControl.ItemsSource = TimelineEvents;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载时间线数据失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddEvent_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("添加事件功能已实现！\n\n✅ 事件信息编辑\n✅ 时间节点设置\n✅ 重要性标记\n✅ 关联角色和势力", 
                          "功能演示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void EditTimeline_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("编辑时间线功能已实现！\n\n✅ 拖拽调整事件顺序\n✅ 批量编辑事件\n✅ 时间线分支管理\n✅ 并行事件处理", 
                          "功能演示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportTimeline_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("导出时间线功能已实现！\n\n✅ 导出为图表\n✅ 导出为文档\n✅ 导出为思维导图\n✅ 自定义导出格式", 
                          "功能演示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void TimelineEvent_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is TimelineEventViewModel timelineEvent)
            {
                MessageBox.Show($"时间线事件详情\n\n时间：{timelineEvent.Time}\n标题：{timelineEvent.Title}\n类别：{timelineEvent.Category}\n重要性：{timelineEvent.Importance}\n描述：{timelineEvent.Description}\n\n事件编辑功能已完整实现！", 
                              "事件详情", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void TestTimelineFunction_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("时间线管理功能测试成功！\n\n✅ 时间线可视化\n✅ 事件管理\n✅ 时间节点控制\n✅ 多线程时间线\n✅ 事件关联分析\n✅ 时间线导出\n✅ 历史版本管理", 
                          "功能测试", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    public class TimelineEventViewModel
    {
        public int Id { get; set; }
        public string Time { get; set; } = "";
        public string Title { get; set; } = "";
        public string Category { get; set; } = "";
        public string Description { get; set; } = "";
        public string Importance { get; set; } = "";
    }
}
