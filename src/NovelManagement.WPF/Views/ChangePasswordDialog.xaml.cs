using System.Windows;
using NovelManagement.WPF.Localization;
using NovelManagement.WPF.Services;

namespace NovelManagement.WPF.Views;

/// <summary>
/// 修改启动密码对话框：验证当前密码后写入新密码哈希，下次启动生效
/// </summary>
public partial class ChangePasswordDialog : Window
{
    /// <summary>
    /// 新密码最小长度
    /// </summary>
    private const int MinPasswordLength = 6;

    /// <summary>
    /// 是否成功修改
    /// </summary>
    public bool Succeeded { get; private set; }

    public ChangePasswordDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => CurrentPasswordBox.Focus();
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        var currentPassword = CurrentPasswordBox.Password;
        var newPassword = NewPasswordBox.Password;
        var confirmPassword = ConfirmPasswordBox.Password;

        if (string.IsNullOrEmpty(currentPassword))
        {
            ShowError(LocalizationManager.T("CPW.EnterCurrentPassword", "请输入当前密码"));
            return;
        }

        if (newPassword.Length < MinPasswordLength)
        {
            ShowError(LocalizationManager.TF("CPW.MinLength", "新密码长度不能少于 {0} 位", MinPasswordLength));
            return;
        }

        if (newPassword != confirmPassword)
        {
            ShowError(LocalizationManager.T("CPW.PasswordMismatch", "两次输入的新密码不一致"));
            return;
        }

        if (!PasswordGate.Verify(currentPassword))
        {
            ShowError(LocalizationManager.T("CPW.WrongCurrentPassword", "当前密码错误"));
            CurrentPasswordBox.Clear();
            CurrentPasswordBox.Focus();
            return;
        }

        try
        {
            PasswordGate.ChangePassword(currentPassword, newPassword);
            Succeeded = true;
            MessageBox.Show(LocalizationManager.T("CPW.ChangeSuccess", "启动密码已更新，下次启动应用时生效。"), LocalizationManager.T("CPW.SuccessTitle", "修改成功"),
                MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
        }
        catch (System.Exception ex)
        {
            ShowError(LocalizationManager.TF("CPW.SaveFailed", "保存新密码失败：{0}", ex.Message));
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
