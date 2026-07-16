using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using Microsoft.Win32;

namespace NovelManagement.WPF.Services
{
    /// <summary>
    /// AnalysisResultDialog.xaml 的交互逻辑
    /// </summary>
    public partial class AnalysisResultDialog : Window
    {
        private readonly WorldSettingAnalysisService.AnalysisResult _result;

        public AnalysisResultDialog(WorldSettingAnalysisService.AnalysisResult result)
        {
            InitializeComponent();
            _result = result ?? throw new ArgumentNullException(nameof(result));
            DisplayResult(result);
        }

        private void DisplayResult(WorldSettingAnalysisService.AnalysisResult result)
        {
            // 设置基本信息
            SettingNameTextBlock.Text = result.SettingName;
            AnalysisTimeText.Text = $"分析时间: {result.AnalyzedAt:yyyy-MM-dd HH:mm:ss}";

            // 设置评分
            OverallScoreText.Text = result.OverallScore.ToString("F1");
            ConsistencyScoreText.Text = result.ConsistencyScore.ToString("F1");
            CompletenessScoreText.Text = result.CompletenessScore.ToString("F1");
            LogicalScoreText.Text = result.LogicalScore.ToString("F1");

            // 设置列表
            StrengthsList.ItemsSource = result.Strengths;
            WeaknessesList.ItemsSource = result.Weaknesses;
            SuggestionsList.ItemsSource = result.Suggestions;

            // 设置可选列表
            if (result.Conflicts.Any())
            {
                ConflictsHeader.Visibility = Visibility.Visible;
                ConflictsList.Visibility = Visibility.Visible;
                ConflictsList.ItemsSource = result.Conflicts;
            }

            if (result.MissingElements.Any())
            {
                MissingHeader.Visibility = Visibility.Visible;
                MissingElementsList.Visibility = Visibility.Visible;
                MissingElementsList.ItemsSource = result.MissingElements;
            }
        }

        private void ExportReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Title = "导出分析报告",
                    FileName = BuildSafeFileName(_result.SettingName),
                    DefaultExt = ".md",
                    Filter = "Markdown 文件 (*.md)|*.md|文本文件 (*.txt)|*.txt",
                    AddExtension = true,
                    OverwritePrompt = true
                };

                if (saveFileDialog.ShowDialog(this) != true)
                {
                    return;
                }

                var extension = Path.GetExtension(saveFileDialog.FileName);
                var content = string.Equals(extension, ".txt", StringComparison.OrdinalIgnoreCase)
                    ? BuildPlainTextReport(_result)
                    : BuildMarkdownReport(_result);

                File.WriteAllText(saveFileDialog.FileName, content, Encoding.UTF8);
                MessageBox.Show($"分析报告已导出到：{saveFileDialog.FileName}", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出分析报告失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static string BuildSafeFileName(string settingName)
        {
            var fileName = string.IsNullOrWhiteSpace(settingName)
                ? "AI分析报告"
                : $"AI分析报告-{settingName.Trim()}";

            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(invalidChar, '_');
            }

            return fileName;
        }

        private static string BuildMarkdownReport(WorldSettingAnalysisService.AnalysisResult result)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"# {result.SettingName} AI分析报告");
            builder.AppendLine();
            builder.AppendLine($"- 分析时间：{result.AnalyzedAt:yyyy-MM-dd HH:mm:ss}");
            builder.AppendLine($"- 总体评分：{result.OverallScore:F1}");
            builder.AppendLine($"- 一致性评分：{result.ConsistencyScore:F1}");
            builder.AppendLine($"- 完整性评分：{result.CompletenessScore:F1}");
            builder.AppendLine($"- 逻辑性评分：{result.LogicalScore:F1}");
            builder.AppendLine();

            AppendMarkdownSection(builder, "优势", result.Strengths);
            AppendMarkdownSection(builder, "需要改进", result.Weaknesses);
            AppendMarkdownSection(builder, "改进建议", result.Suggestions);

            if (result.Conflicts.Any())
            {
                AppendMarkdownSection(builder, "发现的冲突", result.Conflicts);
            }

            if (result.MissingElements.Any())
            {
                AppendMarkdownSection(builder, "缺失的元素", result.MissingElements);
            }

            return builder.ToString();
        }

        private static string BuildPlainTextReport(WorldSettingAnalysisService.AnalysisResult result)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"{result.SettingName} AI分析报告");
            builder.AppendLine(new string('=', Math.Max(12, result.SettingName.Length + 9)));
            builder.AppendLine($"分析时间: {result.AnalyzedAt:yyyy-MM-dd HH:mm:ss}");
            builder.AppendLine($"总体评分: {result.OverallScore:F1}");
            builder.AppendLine($"一致性评分: {result.ConsistencyScore:F1}");
            builder.AppendLine($"完整性评分: {result.CompletenessScore:F1}");
            builder.AppendLine($"逻辑性评分: {result.LogicalScore:F1}");
            builder.AppendLine();

            AppendPlainTextSection(builder, "优势", result.Strengths);
            AppendPlainTextSection(builder, "需要改进", result.Weaknesses);
            AppendPlainTextSection(builder, "改进建议", result.Suggestions);

            if (result.Conflicts.Any())
            {
                AppendPlainTextSection(builder, "发现的冲突", result.Conflicts);
            }

            if (result.MissingElements.Any())
            {
                AppendPlainTextSection(builder, "缺失的元素", result.MissingElements);
            }

            return builder.ToString();
        }

        private static void AppendMarkdownSection(StringBuilder builder, string title, IEnumerable<string> items)
        {
            builder.AppendLine($"## {title}");
            foreach (var item in items.Where(static item => !string.IsNullOrWhiteSpace(item)))
            {
                builder.AppendLine($"- {item.Trim()}");
            }

            builder.AppendLine();
        }

        private static void AppendPlainTextSection(StringBuilder builder, string title, IEnumerable<string> items)
        {
            builder.AppendLine($"{title}:");
            foreach (var item in items.Where(static item => !string.IsNullOrWhiteSpace(item)))
            {
                builder.AppendLine($"- {item.Trim()}");
            }

            builder.AppendLine();
        }
    }
}
