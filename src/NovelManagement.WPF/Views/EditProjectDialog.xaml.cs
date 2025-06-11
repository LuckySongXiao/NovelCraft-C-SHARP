using System;
using System.Windows;
using System.Windows.Controls;

namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// 编辑项目对话框
    /// </summary>
    public partial class EditProjectDialog : Window
    {
        #region 属性

        /// <summary>
        /// 是否确认保存
        /// </summary>
        public bool IsConfirmed { get; private set; }

        /// <summary>
        /// 项目数据
        /// </summary>
        public ProjectManagementView.ProjectViewModel ProjectData { get; private set; }

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="project">要编辑的项目</param>
        public EditProjectDialog(ProjectManagementView.ProjectViewModel project)
        {
            InitializeComponent();
            
            if (project == null)
                throw new ArgumentNullException(nameof(project));

            ProjectData = new ProjectManagementView.ProjectViewModel
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                Type = project.Type,
                Status = project.Status,
                LastUpdated = project.LastUpdated
            };

            LoadProjectData();
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 加载项目数据到界面
        /// </summary>
        private void LoadProjectData()
        {
            ProjectNameTextBox.Text = ProjectData.Name;
            ProjectDescriptionTextBox.Text = ProjectData.Description;

            // 设置项目类型
            foreach (ComboBoxItem item in ProjectTypeComboBox.Items)
            {
                if (item.Content.ToString() == ProjectData.Type)
                {
                    ProjectTypeComboBox.SelectedItem = item;
                    break;
                }
            }

            // 设置项目状态
            foreach (ComboBoxItem item in ProjectStatusComboBox.Items)
            {
                if (item.Content.ToString() == ProjectData.Status)
                {
                    ProjectStatusComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        /// <summary>
        /// 验证输入数据
        /// </summary>
        /// <returns>验证是否通过</returns>
        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(ProjectNameTextBox.Text))
            {
                MessageBox.Show("请输入项目名称", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                ProjectNameTextBox.Focus();
                return false;
            }

            if (ProjectTypeComboBox.SelectedItem == null)
            {
                MessageBox.Show("请选择项目类型", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                ProjectTypeComboBox.Focus();
                return false;
            }

            return true;
        }

        /// <summary>
        /// 保存项目数据
        /// </summary>
        private void SaveProjectData()
        {
            ProjectData.Name = ProjectNameTextBox.Text.Trim();
            ProjectData.Description = ProjectDescriptionTextBox.Text.Trim();
            ProjectData.Type = (ProjectTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "";
            ProjectData.Status = (ProjectStatusComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "进行中";
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 保存项目按钮点击事件
        /// </summary>
        private void SaveProject_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateInput())
                    return;

                SaveProjectData();
                IsConfirmed = true;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存项目失败：{ex.Message}", "错误", 
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
}
