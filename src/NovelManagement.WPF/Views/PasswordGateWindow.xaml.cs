using System.Windows;
using System.Windows.Input;
using NovelManagement.WPF.Localization;
using NovelManagement.WPF.Services;

namespace NovelManagement.WPF.Views;

/// <summary>
/// 启动密码验证窗口：应用启动时的访问门禁
/// </summary>
public partial class PasswordGateWindow : Window
{
    /// <summary>
    /// 是否通过验证
    /// </summary>
    public bool Authenticated { get; private set; }

    public PasswordGateWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => PasswordInput.Focus();
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (PasswordGate.Verify(PasswordInput.Password))
        {
            Authenticated = true;
            DialogResult = true;
        }
        else
        {
            ErrorText.Text = LocalizationManager.T("PGate.WrongPassword");
            ErrorText.Visibility = Visibility.Visible;
            PasswordInput.Clear();
            PasswordInput.Focus();
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Authenticated = false;
        DialogResult = false;
    }

    private void PasswordInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Confirm_Click(sender, e);
        }
    }
}
