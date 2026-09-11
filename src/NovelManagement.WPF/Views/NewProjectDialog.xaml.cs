using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Configuration;
using NovelManagement.WPF.Localization;

namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// NewProjectDialog.xaml 的交互逻辑
    /// </summary>
    public partial class NewProjectDialog : Window
    {
        private const string DefaultTemplateName = "标准模板";
        /// <summary>默认作者：中文界面「精神抖擞」，英文界面「LuckySongXiao」。</summary>
        private static string DefaultAuthor =>
            Localization.LocalizationManager.IsEnglish ? "LuckySongXiao" : "精神抖擞";

        /// <summary>
        /// 新建项目的数据模型
        /// </summary>
        public class NewProjectModel
        {
            /// <summary>
            /// 项目名称。
            /// </summary>
            public string Name { get; set; } = string.Empty;

            /// <summary>
            /// 作者（默认取配置 Application:Author，缺省「精神抖擞」）。
            /// </summary>
            public string Author { get; set; } = DefaultAuthor;

            /// <summary>
            /// 项目描述。
            /// </summary>
            public string Description { get; set; } = string.Empty;

            /// <summary>
            /// 项目类型。
            /// </summary>
            public string Type { get; set; } = string.Empty;

            /// <summary>
            /// 目标字数。
            /// </summary>
            public int TargetWordCount { get; set; }

            /// <summary>
            /// 是否启用 AI 功能。
            /// </summary>
            public bool EnableAI { get; set; } = true;

            /// <summary>
            /// 是否启用自动保存。
            /// </summary>
            public bool AutoSave { get; set; } = true;

            /// <summary>
            /// 是否启用版本控制。
            /// </summary>
            public bool VersionControl { get; set; } = false;

            /// <summary>
            /// 选中的项目模板。
            /// </summary>
            public string Template { get; set; } = string.Empty;
        }

        /// <summary>
        /// 创建的项目数据
        /// </summary>
        public NewProjectModel? ProjectData { get; private set; }

        /// <summary>
        /// 对话框结果
        /// </summary>
        public bool IsConfirmed { get; private set; } = false;

        /// <summary>
        /// 初始化新建项目对话框。
        /// </summary>
        public NewProjectDialog()
        {
            InitializeComponent();
            InitializeDefaults();
        }

        /// <summary>
        /// 初始化默认值
        /// </summary>
        private void InitializeDefaults()
        {
            // 设置默认的项目类型
            ProjectTypeComboBox.SelectedIndex = 0;

            // 设置默认的项目模板
            ProjectTemplateComboBox.SelectedIndex = 0;

            // 设置默认目标字数
            TargetWordCountTextBox.Text = "500000";

            // 设置默认作者（配置 Application:Author，缺省「精神抖擞」）
            var configuredAuthor = App.ServiceProvider?.GetService(typeof(IConfiguration)) as IConfiguration;
            AuthorTextBox.Text = string.IsNullOrWhiteSpace(configuredAuthor?["Application:Author"])
                ? DefaultAuthor
                : configuredAuthor["Application:Author"]!.Trim();
        }

        /// <summary>
        /// 验证输入数据
        /// </summary>
        /// <returns>验证是否通过</returns>
        private bool ValidateInput()
        {
            // 验证项目名称
            if (string.IsNullOrWhiteSpace(ProjectNameTextBox.Text))
            {
                MessageBox.Show(LocalizationManager.T("NPD.NameRequired", "请输入项目名称"), LocalizationManager.T("NPD.ValidationFailed", "验证失败"), MessageBoxButton.OK, MessageBoxImage.Warning);
                ProjectNameTextBox.Focus();
                return false;
            }

            // 验证项目类型
            if (ProjectTypeComboBox.SelectedItem == null)
            {
                MessageBox.Show(LocalizationManager.T("NPD.TypeRequired", "请选择项目类型"), LocalizationManager.T("NPD.ValidationFailed", "验证失败"), MessageBoxButton.OK, MessageBoxImage.Warning);
                ProjectTypeComboBox.Focus();
                return false;
            }

            // 验证目标字数
            if (!string.IsNullOrWhiteSpace(TargetWordCountTextBox.Text))
            {
                if (!int.TryParse(TargetWordCountTextBox.Text, out int wordCount) || wordCount <= 0)
                {
                    MessageBox.Show(LocalizationManager.T("NPD.TargetWordsInvalid", "目标字数必须是正整数"), LocalizationManager.T("NPD.ValidationFailed", "验证失败"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    TargetWordCountTextBox.Focus();
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 收集表单数据
        /// </summary>
        /// <returns>项目数据</returns>
        private NewProjectModel CollectFormData()
        {
            var projectData = new NewProjectModel
            {
                Name = ProjectNameTextBox.Text.Trim(),
                Author = string.IsNullOrWhiteSpace(AuthorTextBox.Text) ? DefaultAuthor : AuthorTextBox.Text.Trim(),
                Description = ProjectDescriptionTextBox.Text.Trim(),
                Type = ((ComboBoxItem)ProjectTypeComboBox.SelectedItem)?.Tag?.ToString()
                     ?? ((ComboBoxItem)ProjectTypeComboBox.SelectedItem)?.Content?.ToString() ?? string.Empty,
                EnableAI = EnableAICheckBox.IsChecked ?? false,
                AutoSave = AutoSaveCheckBox.IsChecked ?? false,
                VersionControl = VersionControlCheckBox.IsChecked ?? false,
                Template = ((ComboBoxItem?)ProjectTemplateComboBox.SelectedItem)?.Tag?.ToString()
                          ?? ((ComboBoxItem?)ProjectTemplateComboBox.SelectedItem)?.Content?.ToString() ?? DefaultTemplateName
            };

            // 解析目标字数
            if (!string.IsNullOrWhiteSpace(TargetWordCountTextBox.Text))
            {
                if (int.TryParse(TargetWordCountTextBox.Text, out int wordCount))
                {
                    projectData.TargetWordCount = wordCount;
                }
            }

            return projectData;
        }

        #region 事件处理

        /// <summary>
        /// 创建项目按钮点击事件
        /// </summary>
        private void CreateProject_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
            {
                return;
            }

            try
            {
                // 收集表单数据
                ProjectData = CollectFormData();
                IsConfirmed = true;

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(LocalizationManager.TF("NPD.CreateFailed", "创建项目时发生错误：{0}", ex.Message), LocalizationManager.T("Msg.Error", "错误"), MessageBoxButton.OK, MessageBoxImage.Error);
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
}
