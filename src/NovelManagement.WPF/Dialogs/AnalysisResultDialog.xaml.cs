using System.Linq;
using System.Windows;

namespace NovelManagement.WPF.Services
{
    /// <summary>
    /// AnalysisResultDialog.xaml 的交互逻辑
    /// </summary>
    public partial class AnalysisResultDialog : Window
    {
        public AnalysisResultDialog(WorldSettingAnalysisService.AnalysisResult result)
        {
            InitializeComponent();
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
            MessageBox.Show("导出功能正在开发中", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
