using System;
using System.Windows;
using System.Windows.Controls;

namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// NewProjectDialog.xaml 的交互逻辑
    /// </summary>
    public partial class NewProjectDialog : Window
    {
        /// <summary>
        /// 新建项目的数据模型
        /// </summary>
        public class NewProjectModel
        {
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public int TargetWordCount { get; set; }
            public bool EnableAI { get; set; } = true;
            public bool AutoSave { get; set; } = true;
            public bool VersionControl { get; set; } = false;
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
        /// 构造函数
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
                MessageBox.Show("请输入项目名称", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                ProjectNameTextBox.Focus();
                return false;
            }

            // 验证项目类型
            if (ProjectTypeComboBox.SelectedItem == null)
            {
                MessageBox.Show("请选择项目类型", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                ProjectTypeComboBox.Focus();
                return false;
            }

            // 验证目标字数
            if (!string.IsNullOrWhiteSpace(TargetWordCountTextBox.Text))
            {
                if (!int.TryParse(TargetWordCountTextBox.Text, out int wordCount) || wordCount <= 0)
                {
                    MessageBox.Show("目标字数必须是正整数", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                Description = ProjectDescriptionTextBox.Text.Trim(),
                Type = ((ComboBoxItem)ProjectTypeComboBox.SelectedItem).Content.ToString() ?? string.Empty,
                EnableAI = EnableAICheckBox.IsChecked ?? false,
                AutoSave = AutoSaveCheckBox.IsChecked ?? false,
                VersionControl = VersionControlCheckBox.IsChecked ?? false,
                Template = ((ComboBoxItem)ProjectTemplateComboBox.SelectedItem).Content.ToString() ?? string.Empty
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

                // TODO: 这里应该调用实际的项目创建服务
                // await _projectService.CreateProjectAsync(ProjectData);

                MessageBox.Show($"项目 '{ProjectData.Name}' 创建成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"创建项目时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
