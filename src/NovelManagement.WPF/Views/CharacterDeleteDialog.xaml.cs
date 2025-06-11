using System;
using System.Windows;
using System.Windows.Controls;
using NovelManagement.Core.Entities;

namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// CharacterDeleteDialog.xaml 的交互逻辑
    /// </summary>
    public partial class CharacterDeleteDialog : Window
    {
        /// <summary>
        /// 要删除的角色
        /// </summary>
        public Character Character { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="character">要删除的角色</param>
        public CharacterDeleteDialog(Character character)
        {
            InitializeComponent();
            Character = character ?? throw new ArgumentNullException(nameof(character));
            LoadCharacterInfo();
        }

        /// <summary>
        /// 加载角色信息
        /// </summary>
        private void LoadCharacterInfo()
        {
            try
            {
                CharacterNameText.Text = Character.Name;
                CharacterTypeChip.Content = Character.Type;
                CharacterFactionChip.Content = Character.Faction?.Name ?? "无";
                CharacterCultivationText.Text = Character.CultivationLevel ?? "未知";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载角色信息失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 确认姓名输入框文本变化事件
        /// </summary>
        private void ConfirmNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                // 只有当输入的姓名与角色姓名完全匹配时才启用删除按钮
                DeleteButton.IsEnabled = ConfirmNameTextBox.Text.Trim() == Character.Name;
            }
            catch (Exception ex)
            {
                // 记录错误但不显示给用户，避免干扰输入体验
                System.Diagnostics.Debug.WriteLine($"确认姓名验证失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 删除按钮点击事件
        /// </summary>
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 最后一次确认
                var result = MessageBox.Show(
                    $"您确定要删除角色 '{Character.Name}' 吗？\n\n此操作不可撤销！",
                    "最终确认",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    MessageBoxResult.No);

                if (result == MessageBoxResult.Yes)
                {
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除操作失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 取消按钮点击事件
        /// </summary>
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
