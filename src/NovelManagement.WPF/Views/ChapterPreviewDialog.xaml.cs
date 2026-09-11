using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using System.IO;

using static NovelManagement.WPF.Localization.LocalizationManager;
namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// 章节预览对话框
    /// </summary>
    public partial class ChapterPreviewDialog : Window
    {
        #region 字段

        private ChapterEditData _chapterData;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="chapterData">章节数据</param>
        public ChapterPreviewDialog(ChapterEditData chapterData)
        {
            InitializeComponent();
            _chapterData = chapterData ?? throw new ArgumentNullException(nameof(chapterData));
            LoadChapterData();
        }

        #endregion

        #region 数据加载

        /// <summary>
        /// 加载章节数据
        /// </summary>
        private void LoadChapterData()
        {
            try
            {
                // 设置标题信息
                ChapterTitleText.Text = _chapterData.Title;
                Title = TF("CPV.TitleFmt", "章节预览 - {0}", _chapterData.Title);

                // 设置内容
                ContentPreviewText.Text = _chapterData.Content;

                // 设置摘要
                SummaryText.Text = string.IsNullOrEmpty(_chapterData.Summary) ? T("CPV.NoSummary", "暂无摘要") : _chapterData.Summary;

                // 设置相关角色
                CharactersText.Text = string.IsNullOrEmpty(_chapterData.Characters) ? T("CPV.NoChars", "暂无相关角色") : _chapterData.Characters;

                // 设置备注
                NotesText.Text = string.IsNullOrEmpty(_chapterData.Notes) ? T("PLM.NoNotes", "暂无备注") : _chapterData.Notes;

                // 设置状态信息
                StatusText.Text = TF("CPV.StatusFmt", "状态: {0}", _chapterData.Status);
                ImportanceText.Text = $"重要性: {GetImportanceStars(_chapterData.ImportanceLevel)}";

                // 加载标签
                LoadTags();

                // 更新统计信息
                UpdateStatistics();

                // 设置最后修改时间
                LastModifiedText.Text = TF("CPV.LastModifiedFmt", "最后修改: {0}", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(TF("CPV.LoadFailed", "加载章节数据失败：{0}", ex.Message), T("Msg.Error"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 加载标签
        /// </summary>
        private void LoadTags()
        {
            try
            {
                TagsPanel.Children.Clear();

                if (!string.IsNullOrEmpty(_chapterData.Tags))
                {
                    var tags = _chapterData.Tags.Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var tag in tags)
                    {
                        var tagBorder = new Border
                        {
                            Background = new SolidColorBrush(Color.FromRgb(63, 81, 181)),
                            CornerRadius = new CornerRadius(12),
                            Padding = new Thickness(8, 4, 8, 4),
                            Margin = new Thickness(0, 0, 4, 4)
                        };

                        var tagText = new TextBlock
                        {
                            Text = tag.Trim(),
                            Foreground = Brushes.White,
                            FontSize = 11
                        };

                        tagBorder.Child = tagText;
                        TagsPanel.Children.Add(tagBorder);
                    }
                }
                else
                {
                    var noTagsText = new TextBlock
                    {
                        Text = T("CPV.NoTags", "暂无标签"),
                        FontSize = 12,
                        Foreground = (Brush)FindResource("MaterialDesignBodyLight")
                    };
                    TagsPanel.Children.Add(noTagsText);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(TF("CPV.LoadTagsFailed", "加载标签失败：{0}", ex.Message), T("Msg.Error"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 更新统计信息
        /// </summary>
        private void UpdateStatistics()
        {
            try
            {
                var content = _chapterData.Content ?? "";
                var wordCount = content.Length;
                var paragraphs = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries).Length;
                var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).Length;
                var readTime = Math.Max(1, wordCount / 400); // 假设每分钟阅读400字
                var progress = _chapterData.TargetWordCount > 0 ? 
                    Math.Min(100, (wordCount * 100) / _chapterData.TargetWordCount) : 100;

                WordCountText.Text = TF("CPV.WordsFmt", "字数: {0:N0}", wordCount);
                StatsWordCount.Text = wordCount.ToString("N0");
                StatsParagraphs.Text = paragraphs.ToString();
                StatsLines.Text = lines.ToString();
                StatsReadTime.Text = TF("CPV.ReadTimeFmt", "{0}分钟", readTime);
                StatsProgress.Text = $"{progress}%";
            }
            catch (Exception ex)
            {
                MessageBox.Show(TF("CPV.UpdateStatsFailed", "更新统计信息失败：{0}", ex.Message), T("Msg.Error"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 获取重要性星级显示
        /// </summary>
        private string GetImportanceStars(int level)
        {
            return level switch
            {
                1 => "★☆☆☆☆",
                2 => "★★☆☆☆",
                3 => "★★★☆☆",
                4 => "★★★★☆",
                5 => "★★★★★",
                _ => "★★☆☆☆"
            };
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 字体大小变化事件
        /// </summary>
        private void FontSize_Changed(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (ContentPreviewText == null) return;

                var selectedIndex = FontSizeComboBox.SelectedIndex;
                ContentPreviewText.FontSize = selectedIndex switch
                {
                    0 => 14, // 小
                    1 => 16, // 中
                    2 => 18, // 大
                    3 => 20, // 特大
                    _ => 16
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show(TF("CPV.FontSizeFailed", "更改字体大小失败：{0}", ex.Message), T("Msg.Error"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 导出按钮点击事件
        /// </summary>
        private void Export_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Title = T("CPV.ExportTitle", "导出章节"),
                    Filter = T("CPV.ExportFilter", "文本文件 (*.txt)|*.txt|Word文档 (*.docx)|*.docx|所有文件 (*.*)|*.*"),
                    FileName = $"{_chapterData.Title}.txt"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var content = $"{_chapterData.Title}\r\n\r\n{_chapterData.Content}";
                    File.WriteAllText(saveDialog.FileName, content, System.Text.Encoding.UTF8);
                    
                    MessageBox.Show(TF("CPV.ExportSuccess", "章节已导出到：{0}", saveDialog.FileName), T("CPV.ExportDoneTitle", "导出成功"),
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(TF("CPV.ExportFailed", "导出章节失败：{0}", ex.Message), T("Msg.Error"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 打印按钮点击事件
        /// </summary>
        private void Print_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var printDialog = new System.Windows.Controls.PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    // 创建打印文档
                    var flowDoc = new System.Windows.Documents.FlowDocument();
                    
                    // 添加标题
                    var titleParagraph = new System.Windows.Documents.Paragraph();
                    titleParagraph.Inlines.Add(new System.Windows.Documents.Run(_chapterData.Title)
                    {
                        FontSize = 20,
                        FontWeight = FontWeights.Bold
                    });
                    flowDoc.Blocks.Add(titleParagraph);
                    
                    // 添加空行
                    flowDoc.Blocks.Add(new System.Windows.Documents.Paragraph());
                    
                    // 添加内容
                    var contentParagraph = new System.Windows.Documents.Paragraph();
                    contentParagraph.Inlines.Add(new System.Windows.Documents.Run(_chapterData.Content));
                    flowDoc.Blocks.Add(contentParagraph);
                    
                    // 设置页面大小
                    flowDoc.PageHeight = printDialog.PrintableAreaHeight;
                    flowDoc.PageWidth = printDialog.PrintableAreaWidth;
                    flowDoc.PagePadding = new Thickness(50);
                    flowDoc.ColumnGap = 0;
                    flowDoc.ColumnWidth = printDialog.PrintableAreaWidth;
                    
                    // 打印
                    printDialog.PrintDocument(((System.Windows.Documents.IDocumentPaginatorSource)flowDoc).DocumentPaginator, 
                        TF("CPV.PrintTitleFmt", "章节打印 - {0}", _chapterData.Title));
                    
                    MessageBox.Show(T("CPV.PrintSent", "章节已发送到打印机"), T("CPV.PrintDoneTitle", "打印成功"),
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(TF("CPV.PrintFailed", "打印章节失败：{0}", ex.Message), T("Msg.Error"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 关闭按钮点击事件
        /// </summary>
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion
    }
}
