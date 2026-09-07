using System;
using System.Windows;
using NovelManagement.WPF.Models;

namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// 长篇批量生成选项对话框。
    /// </summary>
    public partial class BatchGenerationOptionsDialog : Window
    {
        public BatchGenerationOptionsDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 用户确认后的选项（仅在 ShowDialog 返回 true 后有效）。
        /// </summary>
        public BatchGenerationOptions Options { get; } = new();

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(TxtChaptersPerVolume.Text.Trim(), out var chaptersPerVolume))
            {
                MessageBox.Show("每卷章节数必须是数字。", "输入有误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(TxtChapterTargetWords.Text.Trim(), out var targetWords))
            {
                MessageBox.Show("每章目标字数必须是数字。", "输入有误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Options.UnlimitedMode = ChkUnlimited.IsChecked == true;
            Options.NextThreeChaptersThenNewVolume = ChkQuickSwitch.IsChecked == true;
            Options.ChaptersPerVolume = chaptersPerVolume;
            Options.ChapterTargetWords = targetWords;
            Options.Normalize();

            DialogResult = true;
        }
    }
}
