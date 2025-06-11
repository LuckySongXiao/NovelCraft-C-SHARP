using System.Windows;

namespace NovelManagement.WPF.Test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 测试装备体系界面
        /// </summary>
        private void TestEquipmentSystem_Click(object sender, RoutedEventArgs e)
        {
            // 清空主内容区域
            MainContentArea.Children.Clear();

            // 添加装备体系视图
            var equipmentSystemView = new Views.EquipmentSystemView();
            MainContentArea.Children.Add(equipmentSystemView);

            // 更新窗口标题
            this.Title = "小说管理系统 - 装备体系管理测试";
        }

        /// <summary>
        /// 测试功法体系界面
        /// </summary>
        private void TestTechniqueSystem_Click(object sender, RoutedEventArgs e)
        {
            // 清空主内容区域
            MainContentArea.Children.Clear();

            // 添加功法体系视图
            var techniqueSystemView = new Views.TechniqueSystemView();
            MainContentArea.Children.Add(techniqueSystemView);

            // 更新窗口标题
            this.Title = "小说管理系统 - 功法体系管理测试";
        }

        /// <summary>
        /// 测试商业体系界面
        /// </summary>
        private void TestBusinessSystem_Click(object sender, RoutedEventArgs e)
        {
            // 清空主内容区域
            MainContentArea.Children.Clear();

            // 添加商业体系视图
            var businessSystemView = new Views.BusinessSystemView();
            MainContentArea.Children.Add(businessSystemView);

            // 更新窗口标题
            this.Title = "小说管理系统 - 商业体系管理测试";
        }

        /// <summary>
        /// 测试时间线界面
        /// </summary>
        private void TestTimeline_Click(object sender, RoutedEventArgs e)
        {
            // 清空主内容区域
            MainContentArea.Children.Clear();

            // 添加时间线视图
            var timelineView = new Views.TimelineView();
            MainContentArea.Children.Add(timelineView);

            // 更新窗口标题
            this.Title = "小说管理系统 - 时间线管理测试";
        }
    }
}
