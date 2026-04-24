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
        /// 初始化章节一致性检查对话框。
        /// </summary>
        /// <param name="chapterData">待检查的章节数据。</param>
        /// <param name="chapterContent">待检查的章节正文。</param>
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
                    InitializeAIService();
                    if (_aiAssistantService == null)
                    {
                        MessageBox.Show("AI服务未初始化，无法执行一致性检查。\n请确保AI服务已正确配置。",
                            "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                // 构建检查参数
                var parameters = new Dictionary<string, object>
                {
                    ["chapterTitle"] = _chapterData.Title,
                    ["chapterContent"] = _chapterContent,
                    ["chapterSummary"] = _chapterData.Summary,
                    ["characters"] = _chapterData.Characters,
                    ["tags"] = _chapterData.Tags,
                    ["checkPlotConsistency"] = CheckPlotConsistencyCheckBox.IsChecked == true,
                    ["checkCharacterConsistency"] = CheckCharacterConsistencyCheckBox.IsChecked == true,
                    ["checkWorldSetting"] = CheckWorldSettingCheckBox.IsChecked == true
                };

                // 调用AI服务执行一致性检查
                var result = await _aiAssistantService.CheckPlotContinuityAsync(parameters);

                if (result.IsSuccess && result.Data != null)
                {
                    // 解析AI返回的一致性问题
                    var parsedIssues = ParseConsistencyIssues(result.Data);
                    foreach (var issue in parsedIssues)
                    {
                        _issues.Add(issue);
                    }
                }
                else
                {
                    MessageBox.Show($"一致性检查未返回有效结果：{result.Message ?? "未知错误"}",
                        "检查结果", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                UpdateIssueCount();

                if (_issues.Count > 0)
                {
                    MessageBox.Show($"一致性检查完成！发现 {_issues.Count} 个问题", "检查完成",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("一致性检查完成！未发现问题", "检查完成",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
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
        /// 解析AI返回的一致性问题数据
        /// </summary>
        /// <param name="data">AI返回的原始数据</param>
        /// <returns>解析后的一致性问题列表</returns>
        private List<ConsistencyIssue> ParseConsistencyIssues(object data)
        {
            var issues = new List<ConsistencyIssue>();

            try
            {
                if (data is string text && !string.IsNullOrWhiteSpace(text))
                {
                    // 将AI返回的文本按段落解析为一致性问题
                    var sections = text.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var section in sections)
                    {
                        var lines = section.Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                        if (lines.Length == 0) continue;

                        var issue = new ConsistencyIssue
                        {
                            Category = DetermineCategory(section),
                            Severity = DetermineSeverity(section),
                            Description = lines[0].TrimStart('•', '-', '*', ' '),
                            DetailedDescription = section,
                            Suggestion = "请根据AI分析结果进行修改",
                            Location = "全文",
                            RelatedContent = lines.Length > 1 ? lines[^1] : string.Empty
                        };

                        issue.SeverityColor = issue.Severity switch
                        {
                            "错误" => new SolidColorBrush(Colors.Red),
                            "警告" => new SolidColorBrush(Colors.Orange),
                            _ => new SolidColorBrush(Colors.Blue)
                        };

                        issue.IconKind = issue.Severity switch
                        {
                            "错误" => PackIconKind.AlertCircleOutline,
                            "警告" => PackIconKind.AlertCircle,
                            _ => PackIconKind.Information
                        };

                        issues.Add(issue);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"解析一致性问题失败: {ex.Message}");
            }

            return issues;
        }

        /// <summary>
        /// 根据内容判断问题分类
        /// </summary>
        private string DetermineCategory(string text)
        {
            if (text.Contains("剧情") || text.Contains("情节") || text.Contains("连贯"))
                return "剧情一致性";
            if (text.Contains("人物") || text.Contains("角色") || text.Contains("性格"))
                return "人物一致性";
            if (text.Contains("设定") || text.Contains("世界") || text.Contains("体系"))
                return "世界设定";
            return "其他";
        }

        /// <summary>
        /// 根据内容判断问题严重级别
        /// </summary>
        private string DetermineSeverity(string text)
        {
            if (text.Contains("错误") || text.Contains("矛盾") || text.Contains("冲突"))
                return "错误";
            if (text.Contains("警告") || text.Contains("不符") || text.Contains("不一致"))
                return "警告";
            return "提示";
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
        /// <summary>
        /// 问题分类。
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// 问题严重级别。
        /// </summary>
        public string Severity { get; set; } = string.Empty;

        /// <summary>
        /// 严重级别对应颜色。
        /// </summary>
        public SolidColorBrush SeverityColor { get; set; } = new SolidColorBrush(Colors.Gray);

        /// <summary>
        /// 问题图标。
        /// </summary>
        public PackIconKind IconKind { get; set; } = PackIconKind.Information;

        /// <summary>
        /// 问题摘要描述。
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 问题位置。
        /// </summary>
        public string Location { get; set; } = string.Empty;

        /// <summary>
        /// 问题详细说明。
        /// </summary>
        public string DetailedDescription { get; set; } = string.Empty;

        /// <summary>
        /// 修复建议。
        /// </summary>
        public string Suggestion { get; set; } = string.Empty;

        /// <summary>
        /// 相关原文内容。
        /// </summary>
        public string RelatedContent { get; set; } = string.Empty;
    }
}
