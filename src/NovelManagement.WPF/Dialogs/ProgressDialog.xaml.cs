using System.Windows;

namespace NovelManagement.WPF.Services
{
    /// <summary>
    /// ProgressDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ProgressDialog : Window
    {
        public ProgressDialog(string title, string message)
        {
            InitializeComponent();
            TitleTextBlock.Text = title;
            MessageTextBlock.Text = message;
            Title = title;
        }
    }
}
