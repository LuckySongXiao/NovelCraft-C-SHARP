using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using NovelManagement.WPF.Services;
using Microsoft.Extensions.DependencyInjection;

namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// 章节一致性检查对话框
    /// </summary>
    public partial class ConsistencyCheckDialog : Window
    {
        #region 字段

        private readonly ChapterEditData _chapterData;
        private readonly string _chapterContent;
        private AIAssistantService? _aiAssistantService;
        private bool _isChecking;
        private ObservableCollection<ConsistencyIssue> _issues;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="chapterData">章节数据</param>
        /// <param name="chapterContent">章节内容</param>
        public ConsistencyCheckDialog(ChapterEditData chapterData, string chapterContent)
        {
            InitializeComponent();
            _chapterData = chapterData;
            _chapterContent = chapterContent;
            _issues = new ObservableCollection<ConsistencyIssue>();
            InitializeAIService();
            SetupUI();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化AI服务
        /// </summary>
        private void InitializeAIService()
        {
            try
            {
                _aiAssistantService = App.ServiceProvider?.GetService<AIAssistantService>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化AI服务失败：{ex.Message}", "警告",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// 设置UI
        /// </summary>
        private void SetupUI()
        {
            IssuesListView.ItemsSource = _issues;
            UpdateIssueCount();
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 开始检查按钮点击事件
        /// </summary>
        private async void StartCheck_Click(object sender, RoutedEventArgs e)
        {
            await PerformConsistencyCheck();
        }

        /// <summary>
        /// 重新检查按钮点击事件
        /// </summary>
        private async void ReCheck_Click(object sender, RoutedEventArgs e)
        {
            await PerformConsistencyCheck();
        }

        /// <summary>
        /// 问题列表选择变化事件
        /// </summary>
        private void IssuesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IssuesListView.SelectedItem is ConsistencyIssue issue)
            {
                ShowIssueDetail(issue);
            }
        }

        /// <summary>
        /// 导出报告按钮点击事件
        /// </summary>
        private void ExportReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var report = GenerateReport();
                Clipboard.SetText(report);
                MessageBox.Show("检查报告已复制到剪贴板", "提示", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出报告失败：{ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 自动修复按钮点击事件
        /// </summary>
        private void AutoFix_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("自动修复功能正在开发中，敬请期待！", "提示", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 关闭按钮点击事件
        /// </summary>
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 执行一致性检查
        /// </summary>
        private async Task PerformConsistencyCheck()
        {
            if (_isChecking)
                return;

            try
            {
                _isChecking = true;
                StartCheckButton.IsEnabled = false;
                StartCheckButton.Content = "检查中...";

                _issues.Clear();
                UpdateIssueCount();

                if (_aiAssistantService == null)
                {
                    MessageBox.Show("AI服务未初始化", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 模拟AI检查（实际应该调用AI服务）
                await Task.Delay(2000); // 模拟检查时间

                var mockIssues = GenerateMockIssues();
                foreach (var issue in mockIssues)
                {
                    _issues.Add(issue);
                }

                UpdateIssueCount();

                MessageBox.Show($"一致性检查完成！发现 {_issues.Count} 个问题", "检查完成", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"一致性检查失败：{ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isChecking = false;
                StartCheckButton.IsEnabled = true;
                StartCheckButton.Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Children =
                    {
                        new PackIcon { Kind = PackIconKind.CheckCircle, Margin = new Thickness(0,0,8,0) },
                        new TextBlock { Text = "开始检查" }
                    }
                };
            }
        }

        /// <summary>
        /// 生成模拟问题
        /// </summary>
        private List<ConsistencyIssue> GenerateMockIssues()
        {
            var issues = new List<ConsistencyIssue>();

            // 根据检查设置生成不同类型的问题
            if (CheckPlotConsistencyCheckBox.IsChecked == true)
            {
                issues.Add(new ConsistencyIssue
                {
                    Category = "剧情一致性",
                    Severity = "警告",
                    SeverityColor = new SolidColorBrush(Colors.Orange),
                    IconKind = PackIconKind.AlertCircle,
                    Description = "章节中提到的天劫等级与前文设定不符",
                    Location = "第3段",
                    DetailedDescription = "在第3段中，主角面临的是'九重天劫'，但在前面的章节中明确设定为'七重天劫'。",
                    Suggestion = "建议将'九重天劫'修改为'七重天劫'，或者在前文中调整设定。",
                    RelatedContent = "第一道雷劫从天而降..."
                });
            }

            if (CheckCharacterConsistencyCheckBox.IsChecked == true)
            {
                issues.Add(new ConsistencyIssue
                {
                    Category = "人物一致性",
                    Severity = "错误",
                    SeverityColor = new SolidColorBrush(Colors.Red),
                    IconKind = PackIconKind.AlertCircleOutline,
                    Description = "角色性格表现与人物设定不符",
                    Location = "第5段",
                    DetailedDescription = "林轩在此处表现得过于冲动，与其'沉稳谨慎'的人物设定不符。",
                    Suggestion = "建议调整林轩的行为描写，体现其谨慎的性格特点。",
                    RelatedContent = "林轩大喝一声，迎向了那道毁灭的雷光..."
                });
            }

            if (CheckWorldSettingCheckBox.IsChecked == true)
            {
                issues.Add(new ConsistencyIssue
                {
                    Category = "世界设定",
                    Severity = "提示",
                    SeverityColor = new SolidColorBrush(Colors.Blue),
                    IconKind = PackIconKind.Information,
                    Description = "修炼体系描述可以更详细",
                    Location = "第2段",
                    DetailedDescription = "关于真气运转的描述较为简单，可以结合世界设定中的修炼体系进行更详细的描写。",
                    Suggestion = "建议参考世界设定中的'九转玄功'体系，详细描述真气运转的路径和方法。",
                    RelatedContent = "立即运转体内真气..."
                });
            }

            return issues;
        }

        /// <summary>
        /// 显示问题详情
        /// </summary>
        private void ShowIssueDetail(ConsistencyIssue issue)
        {
            IssueDetailPanel.Children.Clear();

            // 问题标题
            var titleBlock = new TextBlock
            {
                Text = $"{issue.Category} - {issue.Severity}",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 12)
            };
            IssueDetailPanel.Children.Add(titleBlock);

            // 问题描述
            var descBlock = new TextBlock
            {
                Text = issue.DetailedDescription,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 12)
            };
            IssueDetailPanel.Children.Add(descBlock);

            // 相关内容
            if (!string.IsNullOrEmpty(issue.RelatedContent))
            {
                var contentLabel = new TextBlock
                {
                    Text = "相关内容：",
                    FontWeight = FontWeights.Medium,
                    Margin = new Thickness(0, 0, 0, 4)
                };
                IssueDetailPanel.Children.Add(contentLabel);

                var contentBlock = new TextBox
                {
                    Text = issue.RelatedContent,
                    IsReadOnly = true,
                    TextWrapping = TextWrapping.Wrap,
                    Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                    Margin = new Thickness(0, 0, 0, 12)
                };
                IssueDetailPanel.Children.Add(contentBlock);
            }

            // 修改建议
            var suggestionLabel = new TextBlock
            {
                Text = "修改建议：",
                FontWeight = FontWeights.Medium,
                Margin = new Thickness(0, 0, 0, 4)
            };
            IssueDetailPanel.Children.Add(suggestionLabel);

            var suggestionBlock = new TextBlock
            {
                Text = issue.Suggestion,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Colors.DarkGreen),
                Margin = new Thickness(0, 0, 0, 12)
            };
            IssueDetailPanel.Children.Add(suggestionBlock);
        }

        /// <summary>
        /// 更新问题数量
        /// </summary>
        private void UpdateIssueCount()
        {
            IssueCountLabel.Text = $"({_issues.Count}个问题)";
        }

        /// <summary>
        /// 生成检查报告
        /// </summary>
        private string GenerateReport()
        {
            var report = $"章节一致性检查报告\n";
            report += $"章节：{_chapterData.Title}\n";
            report += $"检查时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
            report += $"发现问题：{_issues.Count}个\n\n";

            foreach (var issue in _issues)
            {
                report += $"【{issue.Severity}】{issue.Category}\n";
                report += $"位置：{issue.Location}\n";
                report += $"描述：{issue.Description}\n";
                report += $"建议：{issue.Suggestion}\n\n";
            }

            return report;
        }

        #endregion
    }

    /// <summary>
    /// 一致性问题模型
    /// </summary>
    public class ConsistencyIssue
    {
        public string Category { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public SolidColorBrush SeverityColor { get; set; } = new SolidColorBrush(Colors.Gray);
        public PackIconKind IconKind { get; set; } = PackIconKind.Information;
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string DetailedDescription { get; set; } = string.Empty;
        public string Suggestion { get; set; } = string.Empty;
        public string RelatedContent { get; set; } = string.Empty;
    }
}
