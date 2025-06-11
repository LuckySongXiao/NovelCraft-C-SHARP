using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// 新建章节对话框
    /// </summary>
    public partial class NewChapterDialog : Window
    {
        #region 属性

        /// <summary>
        /// 是否确认创建
        /// </summary>
        public bool IsConfirmed { get; private set; }

        /// <summary>
        /// 章节数据
        /// </summary>
        public ChapterData ChapterData { get; private set; } = new();

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        public NewChapterDialog()
        {
            InitializeComponent();
            InitializeDefaults();
        }

        /// <summary>
        /// 构造函数（指定卷宗）
        /// </summary>
        /// <param name="volumeName">卷宗名称</param>
        public NewChapterDialog(string volumeName) : this()
        {
            if (!string.IsNullOrEmpty(volumeName))
            {
                // 设置选中的卷宗
                for (int i = 0; i < VolumeComboBox.Items.Count; i++)
                {
                    var item = VolumeComboBox.Items[i] as System.Windows.Controls.ComboBoxItem;
                    if (item?.Content?.ToString()?.Contains(volumeName) == true)
                    {
                        VolumeComboBox.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化默认值
        /// </summary>
        private void InitializeDefaults()
        {
            try
            {
                // 设置默认值
                VolumeComboBox.SelectedIndex = 2; // 第三卷
                ChapterTypeComboBox.SelectedIndex = 0; // 正文章节
                ImportanceLevelComboBox.SelectedIndex = 1; // 重要
                DifficultyLevelComboBox.SelectedIndex = 1; // 普通
                ChapterStatusComboBox.SelectedIndex = 0; // 计划中
                
                // 设置默认章节编号（假设当前最大章节号是45）
                ChapterNumberTextBox.Text = "46";
                
                // 设置默认目标字数
                TargetWordCountTextBox.Text = "2800";
                
                // 聚焦到标题输入框
                ChapterTitleTextBox.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化对话框失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region 验证

        /// <summary>
        /// 验证输入
        /// </summary>
        private bool ValidateInput()
        {
            try
            {
                // 验证所属卷宗
                if (VolumeComboBox.SelectedItem == null)
                {
                    MessageBox.Show("请选择所属卷宗", "验证失败", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    VolumeComboBox.Focus();
                    return false;
                }

                // 验证章节标题
                if (string.IsNullOrWhiteSpace(ChapterTitleTextBox.Text))
                {
                    MessageBox.Show("请输入章节标题", "验证失败", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    ChapterTitleTextBox.Focus();
                    return false;
                }

                // 验证章节编号
                if (string.IsNullOrWhiteSpace(ChapterNumberTextBox.Text) || 
                    !int.TryParse(ChapterNumberTextBox.Text, out int chapterNumber) || chapterNumber <= 0)
                {
                    MessageBox.Show("请输入有效的章节编号（正整数）", "验证失败", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    ChapterNumberTextBox.Focus();
                    return false;
                }

                // 验证章节类型
                if (ChapterTypeComboBox.SelectedItem == null)
                {
                    MessageBox.Show("请选择章节类型", "验证失败", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    ChapterTypeComboBox.Focus();
                    return false;
                }

                // 验证目标字数
                if (!string.IsNullOrWhiteSpace(TargetWordCountTextBox.Text))
                {
                    if (!int.TryParse(TargetWordCountTextBox.Text, out int wordCount) || wordCount <= 0)
                    {
                        MessageBox.Show("请输入有效的目标字数（正整数）", "验证失败", 
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        TargetWordCountTextBox.Focus();
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"验证输入时发生错误：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// 收集表单数据
        /// </summary>
        private ChapterData CollectFormData()
        {
            var data = new ChapterData
            {
                Volume = (VolumeComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "",
                Title = ChapterTitleTextBox.Text.Trim(),
                Number = int.Parse(ChapterNumberTextBox.Text),
                Type = (ChapterTypeComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "",
                Summary = ChapterSummaryTextBox.Text.Trim(),
                RelatedCharacters = RelatedCharactersTextBox.Text.Trim(),
                KeyEvents = KeyEventsTextBox.Text.Trim(),
                Status = (ChapterStatusComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "",
                Tags = ChapterTagsTextBox.Text.Trim(),
                Notes = ChapterNotesTextBox.Text.Trim()
            };

            // 可选字段
            if (int.TryParse(TargetWordCountTextBox.Text, out int wordCount))
                data.TargetWordCount = wordCount;

            // 重要性等级
            if (ImportanceLevelComboBox.SelectedItem != null)
            {
                var importanceText = (ImportanceLevelComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString();
                if (!string.IsNullOrEmpty(importanceText) && importanceText.Length > 0)
                {
                    data.ImportanceLevel = int.Parse(importanceText.Substring(0, 1));
                }
            }

            // 难度等级
            if (DifficultyLevelComboBox.SelectedItem != null)
            {
                var difficultyText = (DifficultyLevelComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString();
                if (!string.IsNullOrEmpty(difficultyText) && difficultyText.Length > 0)
                {
                    data.DifficultyLevel = int.Parse(difficultyText.Substring(0, 1));
                }
            }

            return data;
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 数字输入限制
        /// </summary>
        private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        /// <summary>
        /// 创建按钮点击事件
        /// </summary>
        private void Create_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            try
            {
                ChapterData = CollectFormData();
                IsConfirmed = true;

                MessageBox.Show($"章节 '{ChapterData.Title}' 创建成功！", "成功", 
                    MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"创建章节时发生错误：{ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 取消按钮点击事件
        /// </summary>
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = false;
            DialogResult = false;
            Close();
        }

        #endregion
    }

    /// <summary>
    /// 章节数据模型
    /// </summary>
    public class ChapterData
    {
        public string Volume { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int Number { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public int? TargetWordCount { get; set; }
        public int ImportanceLevel { get; set; } = 2;
        public int DifficultyLevel { get; set; } = 2;
        public string RelatedCharacters { get; set; } = string.Empty;
        public string KeyEvents { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }
}
