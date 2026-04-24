using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// 新建卷宗对话框
    /// </summary>
    public partial class NewVolumeDialog : Window
    {
        #region 属性

        /// <summary>
        /// 是否确认创建
        /// </summary>
        public bool IsConfirmed { get; private set; }

        /// <summary>
        /// 卷宗数据
        /// </summary>
        public VolumeData VolumeData { get; private set; } = new();

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化新建卷宗对话框。
        /// </summary>
        public NewVolumeDialog()
        {
            InitializeComponent();
            InitializeDefaults();
        }

        /// <summary>
        /// 使用已有卷宗数据初始化对话框。
        /// </summary>
        /// <param name="volumeData">待加载的卷宗数据。</param>
        public NewVolumeDialog(VolumeData volumeData) : this()
        {
            if (volumeData == null)
            {
                return;
            }

            VolumeNameTextBox.Text = volumeData.Name;
            VolumeDescriptionTextBox.Text = volumeData.Description;
            VolumeOrderTextBox.Text = volumeData.Order.ToString();
            TargetChapterCountTextBox.Text = volumeData.TargetChapterCount?.ToString() ?? string.Empty;
            EstimatedWordCountTextBox.Text = volumeData.EstimatedWordCount?.ToString() ?? string.Empty;
            VolumeThemeTextBox.Text = volumeData.Theme;
            KeyCharactersTextBox.Text = volumeData.KeyCharacters;
            ImportantEventsTextBox.Text = volumeData.ImportantEvents;
            VolumeTagsTextBox.Text = volumeData.Tags;

            SelectComboBoxItemByContent(VolumeTypeComboBox, volumeData.Type);
            SelectComboBoxItemByContent(VolumeStatusComboBox, volumeData.Status);
            Title = $"编辑卷宗 - {volumeData.Name}";
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
                VolumeTypeComboBox.SelectedIndex = 0; // 主线剧情
                VolumeStatusComboBox.SelectedIndex = 0; // 计划中
                
                // 设置默认顺序（假设当前有3卷）
                VolumeOrderTextBox.Text = "4";
                
                // 设置默认目标章节数
                TargetChapterCountTextBox.Text = "15";
                
                // 设置默认预计字数
                EstimatedWordCountTextBox.Text = "42000";
                
                // 聚焦到名称输入框
                VolumeNameTextBox.Focus();
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
                // 验证卷宗名称
                if (string.IsNullOrWhiteSpace(VolumeNameTextBox.Text))
                {
                    MessageBox.Show("请输入卷宗名称", "验证失败", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    VolumeNameTextBox.Focus();
                    return false;
                }

                // 验证卷宗描述
                if (string.IsNullOrWhiteSpace(VolumeDescriptionTextBox.Text))
                {
                    MessageBox.Show("请输入卷宗描述", "验证失败", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    VolumeDescriptionTextBox.Focus();
                    return false;
                }

                // 验证卷宗类型
                if (VolumeTypeComboBox.SelectedItem == null)
                {
                    MessageBox.Show("请选择卷宗类型", "验证失败", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    VolumeTypeComboBox.Focus();
                    return false;
                }

                // 验证卷宗顺序
                if (string.IsNullOrWhiteSpace(VolumeOrderTextBox.Text) || 
                    !int.TryParse(VolumeOrderTextBox.Text, out int order) || order <= 0)
                {
                    MessageBox.Show("请输入有效的卷宗顺序（正整数）", "验证失败", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    VolumeOrderTextBox.Focus();
                    return false;
                }

                // 验证目标章节数
                if (!string.IsNullOrWhiteSpace(TargetChapterCountTextBox.Text))
                {
                    if (!int.TryParse(TargetChapterCountTextBox.Text, out int chapterCount) || chapterCount <= 0)
                    {
                        MessageBox.Show("请输入有效的目标章节数（正整数）", "验证失败", 
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        TargetChapterCountTextBox.Focus();
                        return false;
                    }
                }

                // 验证预计字数
                if (!string.IsNullOrWhiteSpace(EstimatedWordCountTextBox.Text))
                {
                    if (!int.TryParse(EstimatedWordCountTextBox.Text, out int wordCount) || wordCount <= 0)
                    {
                        MessageBox.Show("请输入有效的预计字数（正整数）", "验证失败", 
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        EstimatedWordCountTextBox.Focus();
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
        private VolumeData CollectFormData()
        {
            var data = new VolumeData
            {
                Name = VolumeNameTextBox.Text.Trim(),
                Description = VolumeDescriptionTextBox.Text.Trim(),
                Type = (VolumeTypeComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "",
                Order = int.Parse(VolumeOrderTextBox.Text),
                Theme = VolumeThemeTextBox.Text.Trim(),
                KeyCharacters = KeyCharactersTextBox.Text.Trim(),
                ImportantEvents = ImportantEventsTextBox.Text.Trim(),
                Status = (VolumeStatusComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "",
                Tags = VolumeTagsTextBox.Text.Trim()
            };

            // 可选字段
            if (int.TryParse(TargetChapterCountTextBox.Text, out int chapterCount))
                data.TargetChapterCount = chapterCount;

            if (int.TryParse(EstimatedWordCountTextBox.Text, out int wordCount))
                data.EstimatedWordCount = wordCount;

            return data;
        }

        private static void SelectComboBoxItemByContent(System.Windows.Controls.ComboBox comboBox, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            foreach (var item in comboBox.Items)
            {
                if (item is System.Windows.Controls.ComboBoxItem comboBoxItem
                    && string.Equals(comboBoxItem.Content?.ToString(), value, StringComparison.OrdinalIgnoreCase))
                {
                    comboBox.SelectedItem = comboBoxItem;
                    break;
                }
            }
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
                VolumeData = CollectFormData();
                IsConfirmed = true;

                MessageBox.Show($"卷宗 '{VolumeData.Name}' 创建成功！", "成功", 
                    MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"创建卷宗时发生错误：{ex.Message}", "错误", 
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
    /// 卷宗数据模型
    /// </summary>
    public class VolumeData
    {
        /// <summary>
        /// 卷宗名称。
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 卷宗描述。
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 卷宗类型。
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 排序序号。
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// 目标章节数。
        /// </summary>
        public int? TargetChapterCount { get; set; }

        /// <summary>
        /// 预计字数。
        /// </summary>
        public int? EstimatedWordCount { get; set; }

        /// <summary>
        /// 卷宗主题。
        /// </summary>
        public string Theme { get; set; } = string.Empty;

        /// <summary>
        /// 关键角色。
        /// </summary>
        public string KeyCharacters { get; set; } = string.Empty;

        /// <summary>
        /// 重要事件。
        /// </summary>
        public string ImportantEvents { get; set; } = string.Empty;

        /// <summary>
        /// 当前状态。
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 标签集合文本。
        /// </summary>
        public string Tags { get; set; } = string.Empty;
    }
}
