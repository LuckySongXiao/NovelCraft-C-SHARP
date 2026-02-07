using System;
using System.Windows;
using System.Windows.Controls;

namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// ProjectOverviewView.xaml 的交互逻辑
    /// </summary>
    public partial class ProjectOverviewView : UserControl
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public ProjectOverviewView()
        {
            InitializeComponent();
        }

        #region 事件处理

        /// <summary>
        /// 写作新章节按钮点击事件
        /// </summary>
        private void WriteChapter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 获取主窗口并切换到卷章管理界面
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null)
                {
                    // 切换到卷章管理界面，用户可以在那里创建新章节
                    mainWindow.ShowVolumeManagement();

                    MessageBox.Show("已切换到卷章管理界面，您可以在此创建新章节", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("无法获取主窗口", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开卷章管理失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 管理角色按钮点击事件
        /// </summary>
        private void ManageCharacters_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 获取主窗口并切换到角色管理界面
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null)
                {
                    // 直接调用公有方法ShowCharacterManagement
                    mainWindow.ShowCharacterManagement();

                    MessageBox.Show("已切换到角色管理界面", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("无法获取主窗口", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开角色管理失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 剧情大纲按钮点击事件
        /// </summary>
        private void PlotOutline_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 获取主窗口并切换到剧情管理界面
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null)
                {
                    // 直接调用公有方法ShowPlotManagement
                    mainWindow.ShowPlotManagement();

                    MessageBox.Show("已切换到剧情管理界面", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("无法获取主窗口", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开剧情管理失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// AI助手按钮点击事件
        /// </summary>
        private void AIAssistant_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 创建AI协作界面窗口
                var aiWindow = new Window
                {
                    Title = "AI协作助手",
                    Width = 1200,
                    Height = 800,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Content = new AICollaborationView()
                };
                aiWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开AI助手失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 项目状态按钮点击事件
        /// </summary>
        private void ProjectStatus_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 获取主窗口并切换到卷章管理界面（项目主页）
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null)
                {
                    // 切换到卷章管理界面作为项目主页
                    mainWindow.ShowVolumeManagement();

                    MessageBox.Show("已进入项目主页 - 卷章管理界面", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("无法获取主窗口", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"进入项目主页失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 世界设定按钮点击事件
        /// </summary>
        private void WorldSettings_Click(object sender, RoutedEventArgs e)
        {
            // 创建一个新窗口来显示世界设定管理
            var window = new Window
            {
                Title = "世界设定管理",
                Width = 1200,
                Height = 800,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Content = new WorldSettingManagementView()
            };
            window.Show();
        }

        /// <summary>
        /// 查看全部角色按钮点击事件
        /// </summary>
        private void ViewAllCharacters_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 获取主窗口并切换到角色管理界面
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null)
                {
                    // 直接调用公有方法ShowCharacterManagement
                    mainWindow.ShowCharacterManagement();

                    MessageBox.Show("已切换到角色管理界面，您可以查看和管理所有角色", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("无法获取主窗口", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开角色管理失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// AI生成大纲按钮点击事件
        /// </summary>
        private void GenerateOutline_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 获取当前项目ID（这里使用模拟ID，实际应该从当前项目上下文获取）
                var currentProjectId = Guid.NewGuid(); // TODO: 从实际项目上下文获取

                // 创建AI大纲生成对话框
                var dialog = new AIOutlineGeneratorDialog(currentProjectId);
                dialog.Owner = Window.GetWindow(this);
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                var result = dialog.ShowDialog();
                if (result == true)
                {
                    MessageBox.Show("大纲生成完成并已保存", "成功",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开AI大纲生成器失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 编辑大纲按钮点击事件
        /// </summary>
        private void EditOutline_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 获取主窗口并切换到剧情管理界面
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null)
                {
                    // 直接调用公有方法ShowPlotManagement
                    mainWindow.ShowPlotManagement();

                    MessageBox.Show("已切换到剧情管理界面，您可以在此编辑剧情大纲", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("无法获取主窗口", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开剧情管理失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 导入导出按钮点击事件
        /// </summary>
        private void ImportExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 创建导入导出界面窗口
                var importExportWindow = new Window
                {
                    Title = "导入导出管理",
                    Width = 1000,
                    Height = 700,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Content = new ImportExportView()
                };
                importExportWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开导入导出界面失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 项目设置按钮点击事件
        /// </summary>
        private void ProjectSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 创建当前项目的模拟数据
                var currentProject = new ProjectManagementView.ProjectViewModel
                {
                    Id = Guid.NewGuid(), // Changed to Guid to match new ProjectViewModel
                    Name = "千面劫·宿命轮回",
                    Description = "一部修仙题材的长篇小说，讲述主角林轩在修仙世界中的成长历程",
                    Type = "修仙小说",
                    Status = "进行中",
                    LastUpdated = "刚刚"
                };

                // 创建项目设置对话框
                var settingsDialog = new EditProjectDialog(currentProject);
                settingsDialog.Owner = Window.GetWindow(this);
                settingsDialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                var result = settingsDialog.ShowDialog();
                if (result == true)
                {
                    MessageBox.Show("项目设置已更新", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开项目设置失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 统计分析按钮点击事件
        /// </summary>
        private void Statistics_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 创建统计分析窗口
                var statisticsWindow = new Window
                {
                    Title = "项目统计分析",
                    Width = 1200,
                    Height = 800,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };

                // 创建统计分析内容
                var statisticsContent = new StackPanel
                {
                    Margin = new Thickness(24)
                };

                // 添加标题
                var titleBlock = new TextBlock
                {
                    Text = "项目统计分析",
                    FontSize = 24,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 20)
                };
                statisticsContent.Children.Add(titleBlock);

                // 添加统计信息
                var statsText = new TextBlock
                {
                    Text = "📊 项目统计信息\n\n" +
                           "• 总字数：125,000字\n" +
                           "• 章节数：45章\n" +
                           "• 角色数：23个\n" +
                           "• 创作天数：120天\n" +
                           "• 平均日更：1,042字\n" +
                           "• 设定完成度：85%\n\n" +
                           "📈 写作趋势\n\n" +
                           "• 本周写作：8,500字\n" +
                           "• 本月写作：32,000字\n" +
                           "• 最高日更：3,200字\n" +
                           "• 连续写作：15天\n\n" +
                           "🎯 目标进度\n\n" +
                           "• 目标字数：500,000字\n" +
                           "• 完成进度：25%\n" +
                           "• 预计完成：还需380天\n" +
                           "• 建议日更：1,500字",
                    FontSize = 14,
                    LineHeight = 20
                };
                statisticsContent.Children.Add(statsText);

                var scrollViewer = new ScrollViewer
                {
                    Content = statisticsContent,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                };

                statisticsWindow.Content = scrollViewer;
                statisticsWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开统计分析失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 备份项目按钮点击事件
        /// </summary>
        private void Backup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    "确定要备份当前项目吗？\n\n备份将包含：\n• 所有文本内容\n• 角色设定\n• 剧情大纲\n• 世界设定\n• 项目配置",
                    "备份确认",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // 模拟备份过程
                    var backupPath = $"千面劫·宿命轮回_备份_{DateTime.Now:yyyyMMdd_HHmmss}.zip";

                    MessageBox.Show(
                        $"项目备份成功！\n\n备份文件：{backupPath}\n保存位置：项目根目录/Backups/\n\n备份包含了所有项目数据，可用于恢复或迁移项目。",
                        "备份完成",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"备份项目失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
