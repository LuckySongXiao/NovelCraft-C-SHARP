using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NovelManagement.WPF.Localization;
using NovelManagement.WPF.Services;

namespace NovelManagement.WPF.Views;

/// <summary>
/// 首次启动引导窗口。
/// </summary>
public class FirstRunOnboardingWindow : Window
{
    private readonly ConfigurationService _configurationService;
    private readonly TextBlock _statusTextBlock;

    public FirstRunOnboardingWindow(ConfigurationService configurationService)
    {
        _configurationService = configurationService;

        Title = LocalizationManager.T("FO.WindowTitle", "首次启动向导");
        Width = 760;
        Height = 520;
        MinWidth = 680;
        MinHeight = 460;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        ResizeMode = ResizeMode.NoResize;

        _statusTextBlock = new TextBlock
        {
            Margin = new Thickness(0, 12, 0, 0),
            Foreground = (Brush)FindResource("AppForegroundTextBrush"),
            TextWrapping = TextWrapping.Wrap
        };

        var openConfigButton = CreateButton(LocalizationManager.T("FO.OpenConfigDir", "打开配置目录"), (_, _) => OpenConfigurationDirectory());
        var createTemplateButton = CreateButton(LocalizationManager.T("FO.CreateTemplate", "创建用户配置模板"), (_, _) => CreateUserConfigTemplate());
        var finishButton = CreateButton(LocalizationManager.T("FO.Finish", "完成并进入系统"), async (_, _) => await CompleteAsync());
        finishButton.FontWeight = FontWeights.SemiBold;

        var contentPanel = new StackPanel
        {
            Margin = new Thickness(24)
        };

        contentPanel.Children.Add(new TextBlock
        {
            Text = LocalizationManager.T("Common.Welcome", "欢迎使用书籍管理系统"),
            FontSize = 28,
            FontWeight = FontWeights.Bold
        });

        contentPanel.Children.Add(new TextBlock
        {
            Margin = new Thickness(0, 12, 0, 0),
            Text = LocalizationManager.T("FO.Intro", "这是首次启动引导。建议在正式使用前先确认以下内容："),
            FontSize = 15
        });

        contentPanel.Children.Add(CreateChecklistText());
        contentPanel.Children.Add(_statusTextBlock);

        var actionPanel = new WrapPanel
        {
            Margin = new Thickness(0, 24, 0, 0)
        };
        actionPanel.Children.Add(openConfigButton);
        actionPanel.Children.Add(createTemplateButton);
        actionPanel.Children.Add(finishButton);

        contentPanel.Children.Add(actionPanel);

        Content = contentPanel;
        UpdateStatus(LocalizationManager.T("FO.StatusHint", "请先创建或检查用户配置，然后点击“完成并进入系统”。"));
    }

    private TextBlock CreateChecklistText()
    {
        var targetConfigPath = Path.Combine(_configurationService.GetConfigurationDirectory(), "appsettings.user.json");

        return new TextBlock
        {
            Margin = new Thickness(0, 16, 0, 0),
            TextWrapping = TextWrapping.Wrap,
            Text = LocalizationManager.TF("FO.Checklist",
                "1. 检查数据库与日志目录是否可写。\n" +
                "2. 如需云端 AI，请在用户配置中填写 API Key。\n" +
                "3. 如需本地 AI，请确认 Ollama 或 RWKV 服务已经启动。\n" +
                "4. 用户覆盖配置建议放置在以下路径：\n" +
                "{0}\n\n" +
                "提示：生产环境建议优先使用环境变量覆盖密钥，而不是把密钥直接写入发布目录。",
                targetConfigPath)
        };
    }

    private static Button CreateButton(string text, RoutedEventHandler onClick)
    {
        var button = new Button
        {
            Content = text,
            Margin = new Thickness(0, 0, 12, 12),
            Padding = new Thickness(16, 8, 16, 8),
            MinWidth = 140
        };
        button.Click += onClick;
        return button;
    }

    private void OpenConfigurationDirectory()
    {
        try
        {
            var directory = _configurationService.GetConfigurationDirectory();
            Directory.CreateDirectory(directory);
            Process.Start(new ProcessStartInfo
            {
                FileName = directory,
                UseShellExecute = true
            });
            UpdateStatus(LocalizationManager.TF("FO.ConfigOpened", "已打开配置目录：{0}", directory));
        }
        catch (Exception ex)
        {
            UpdateStatus(LocalizationManager.TF("FO.ConfigOpenFailed", "打开配置目录失败：{0}", ex.Message), isError: true);
        }
    }

    private void CreateUserConfigTemplate()
    {
        try
        {
            var source = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.user.example.json");
            var target = Path.Combine(_configurationService.GetConfigurationDirectory(), "appsettings.user.json");

            if (!File.Exists(source))
            {
                UpdateStatus(LocalizationManager.TF("FO.TemplateNotFound", "未找到模板文件：{0}", source), isError: true);
                return;
            }

            if (!File.Exists(target))
            {
                File.Copy(source, target);
                UpdateStatus(LocalizationManager.TF("FO.TemplateCreated", "已创建用户配置模板：{0}", target));
            }
            else
            {
                UpdateStatus(LocalizationManager.TF("FO.ConfigExists", "用户配置已存在：{0}", target));
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = target,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            UpdateStatus(LocalizationManager.TF("FO.TemplateCreateFailed", "创建用户配置模板失败：{0}", ex.Message), isError: true);
        }
    }

    private async System.Threading.Tasks.Task CompleteAsync()
    {
        var state = await _configurationService.LoadAppStateAsync();
        state.FirstRunCompleted = true;
        state.FirstRunCompletedAt = DateTime.Now;

        if (!await _configurationService.SaveAppStateAsync(state))
        {
            UpdateStatus(LocalizationManager.T("FO.SaveStateFailed", "保存首次启动状态失败，请重试。"), isError: true);
            return;
        }

        DialogResult = true;
        Close();
    }

    private void UpdateStatus(string message, bool isError = false)
    {
        _statusTextBlock.Text = message;
        _statusTextBlock.Foreground = isError ? Brushes.IndianRed : (Brush)FindResource("AppForegroundTextBrush");
    }
}
