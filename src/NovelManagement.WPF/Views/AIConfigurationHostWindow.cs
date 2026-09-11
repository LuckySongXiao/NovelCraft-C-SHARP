using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NovelManagement.WPF.Localization;

namespace NovelManagement.WPF.Views;

/// <summary>
/// AI 模型配置独立宿主窗口，便于单页调试和发布验证。
/// </summary>
public class AIConfigurationHostWindow : Window
{
    public AIConfigurationHostWindow()
    {
        Title = LocalizationManager.T("AHW.WindowTitle", "AI模型配置");
        Width = 1320;
        Height = 940;
        MinWidth = 1100;
        MinHeight = 760;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        #region debug-point A:host-window-loaded
        _ = ReportDebugEventAsync(
            "A",
            "AIConfigurationHostWindow.OnLoaded",
            "AI配置独立宿主窗口已加载");
        #endregion
        TryRenderConfigurationView();
    }

    private void TryRenderConfigurationView()
    {
        try
        {
            Content = new AIConfigurationView();
            #region debug-point A:host-render-success
            _ = ReportDebugEventAsync(
                "A",
                "AIConfigurationHostWindow.TryRenderConfigurationView",
                "AI配置页面实例化成功",
                new Dictionary<string, object?>
                {
                    ["contentType"] = Content?.GetType().FullName
                });
            #endregion
        }
        catch (Exception ex)
        {
            #region debug-point E:host-render-failed
            _ = ReportDebugEventAsync(
                "E",
                "AIConfigurationHostWindow.TryRenderConfigurationView",
                $"AI配置页面实例化失败: {ex.Message}",
                new Dictionary<string, object?>
                {
                    ["exceptionType"] = ex.GetType().FullName,
                    ["stack"] = ex.ToString()
                });
            #endregion
            Content = BuildErrorView(ex);
        }
    }

    #region debug-point A:report-helper
    private static async Task ReportDebugEventAsync(string hypothesisId, string location, string message, Dictionary<string, object?>? data = null)
    {
        try
        {
            var debugServerUrl = "http://127.0.0.1:7777/event";
            var debugSessionId = "single-file-ai-ui";
            var envPath = FindDebugEnvPath();
            if (!string.IsNullOrWhiteSpace(envPath) && File.Exists(envPath))
            {
                foreach (var line in File.ReadAllLines(envPath, Encoding.UTF8))
                {
                    if (line.StartsWith("DEBUG_SERVER_URL=", StringComparison.OrdinalIgnoreCase))
                    {
                        debugServerUrl = line["DEBUG_SERVER_URL=".Length..].Trim();
                    }
                    else if (line.StartsWith("DEBUG_SESSION_ID=", StringComparison.OrdinalIgnoreCase))
                    {
                        debugSessionId = line["DEBUG_SESSION_ID=".Length..].Trim();
                    }
                }
            }

            var payload = JsonSerializer.Serialize(new
            {
                sessionId = debugSessionId,
                runId = "pre-fix",
                hypothesisId,
                location,
                msg = $"[DEBUG] {message}",
                data,
                ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });

            using var client = new System.Net.Http.HttpClient();
            using var content = new System.Net.Http.StringContent(payload, Encoding.UTF8, "application/json");
            await client.PostAsync(debugServerUrl, content);
        }
        catch
        {
            // ignore debug instrumentation failures
        }
    }

    private static string? FindDebugEnvPath()
    {
        var candidates = new[]
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".dbg", "single-file-ai-ui.env"),
            Path.Combine(Directory.GetCurrentDirectory(), ".dbg", "single-file-ai-ui.env")
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, ".dbg", "single-file-ai-ui.env");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }
    #endregion

    private static UIElement BuildErrorView(Exception ex)
    {
        var title = new TextBlock
        {
            Text = LocalizationManager.T("AHW.ErrorTitle", "AI 模型配置独立测试失败"),
            FontSize = 22,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 12)
        };

        var description = new TextBlock
        {
            Text = LocalizationManager.T("AHW.ErrorDescription", "页面在独立窗口中初始化时发生异常。请将下面的完整错误详情发回继续排查。"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 12)
        };

        var detailBox = new TextBox
        {
            Text = ex.ToString(),
            IsReadOnly = true,
            AcceptsReturn = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            TextWrapping = TextWrapping.Wrap,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 13,
            MinHeight = 420
        };

        var copyButton = new Button
        {
            Content = LocalizationManager.T("AHW.CopyError", "复制错误详情"),
            Padding = new Thickness(14, 8, 14, 8),
            Margin = new Thickness(0, 0, 12, 0),
            MinWidth = 120
        };
        copyButton.Click += (_, _) =>
        {
            Clipboard.SetText(ex.ToString());
            MessageBox.Show(LocalizationManager.T("AHW.Copied", "错误详情已复制到剪贴板。"), LocalizationManager.T("Msg.Tip", "提示"), MessageBoxButton.OK, MessageBoxImage.Information);
        };

        var closeButton = new Button
        {
            Content = LocalizationManager.T("Dlg.Close", "关闭"),
            Padding = new Thickness(14, 8, 14, 8),
            MinWidth = 100
        };
        closeButton.Click += (_, _) => Window.GetWindow(closeButton)?.Close();

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 16, 0, 0)
        };
        buttonPanel.Children.Add(copyButton);
        buttonPanel.Children.Add(closeButton);

        var panel = new StackPanel
        {
            Margin = new Thickness(24)
        };
        panel.Children.Add(title);
        panel.Children.Add(description);
        panel.Children.Add(detailBox);
        panel.Children.Add(buttonPanel);

        return new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = panel
        };
    }
}
