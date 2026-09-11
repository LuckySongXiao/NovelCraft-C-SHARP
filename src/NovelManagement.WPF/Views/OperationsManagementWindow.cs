using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using Microsoft.Extensions.DependencyInjection;
using NovelManagement.WPF.Localization;
using NovelManagement.WPF.Services;

namespace NovelManagement.WPF.Views;

/// <summary>
/// 生产运维面板，提供健康检查、数据库备份和运行目录快捷入口。
/// </summary>
public class OperationsManagementWindow : Window
{
    private readonly ProductionHealthCheckService _healthCheckService;
    private readonly DatabaseMaintenanceService _databaseMaintenanceService;
    private readonly DiagnosticBundleService _diagnosticBundleService;
    private readonly ConfigurationService _configurationService;
    private readonly TextBlock _summaryTextBlock;
    private readonly TextBox _detailsTextBox;
    private readonly Button _refreshButton;
    private readonly Button _backupButton;
    private readonly Button _restoreButton;
    private readonly Button _exportDiagnosticButton;
    private ProductionHealthReport? _currentReport;

    public OperationsManagementWindow(
        ProductionHealthCheckService healthCheckService,
        DatabaseMaintenanceService databaseMaintenanceService,
        DiagnosticBundleService diagnosticBundleService,
        ConfigurationService configurationService)
    {
        _healthCheckService = healthCheckService;
        _databaseMaintenanceService = databaseMaintenanceService;
        _diagnosticBundleService = diagnosticBundleService;
        _configurationService = configurationService;

        Title = LocalizationManager.T("MW.OpsTitle", "发布与运维管理");
        Width = 980;
        Height = 700;
        MinWidth = 820;
        MinHeight = 560;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        _summaryTextBlock = new TextBlock
        {
            Margin = new Thickness(0, 0, 0, 12),
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
            Text = LocalizationManager.T("MW.LoadingHealth", "正在加载健康检查...")
        };

        _detailsTextBox = new TextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            TextWrapping = TextWrapping.Wrap,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 13,
            Margin = new Thickness(0, 12, 0, 0)
        };

        _refreshButton = CreateButton(LocalizationManager.T("MW.BtnRefreshHealth", "刷新健康检查"), async (_, _) => await RefreshHealthReportAsync());
        _backupButton = CreateButton(LocalizationManager.T("MW.BtnBackupNow", "立即备份数据库"), (_, _) => CreateBackup());
        _restoreButton = CreateButton(LocalizationManager.T("MW.BtnRestore", "从备份恢复数据库"), (_, _) => RestoreBackup());
        _exportDiagnosticButton = CreateButton(LocalizationManager.T("MW.BtnExportDiagnostics", "导出诊断包"), async (_, _) => await ExportDiagnosticBundleAsync());

        var openDataButton = CreateButton(LocalizationManager.T("MW.BtnOpenDataDir", "打开数据目录"), (_, _) => OpenDataDirectory());
        var openLogsButton = CreateButton(LocalizationManager.T("MW.BtnOpenLogsDir", "打开日志目录"), (_, _) => OpenDirectory(_currentReport?.LogsDirectory, LocalizationManager.T("MW.DirLogs", "日志目录")));
        var openBackupsButton = CreateButton(LocalizationManager.T("MW.BtnOpenBackupsDir", "打开备份目录"), (_, _) => OpenDirectory(_currentReport?.BackupsDirectory, LocalizationManager.T("MW.DirBackups", "备份目录")));
        var onboardingButton = CreateButton(LocalizationManager.T("MW.BtnReopenOnboarding", "重新打开首次向导"), (_, _) => ReopenOnboarding());
        var changePasswordButton = CreateButton(LocalizationManager.T("MW.BtnChangePassword", "修改启动密码"), (_, _) => ChangeLaunchPassword());

        var actionPanel = new WrapPanel
        {
            Margin = new Thickness(0, 0, 0, 12)
        };

        actionPanel.Children.Add(_refreshButton);
        actionPanel.Children.Add(_backupButton);
        actionPanel.Children.Add(_restoreButton);
        actionPanel.Children.Add(_exportDiagnosticButton);
        actionPanel.Children.Add(openDataButton);
        actionPanel.Children.Add(openLogsButton);
        actionPanel.Children.Add(openBackupsButton);
        actionPanel.Children.Add(onboardingButton);
        actionPanel.Children.Add(changePasswordButton);

        var rootPanel = new DockPanel
        {
            Margin = new Thickness(20)
        };

        DockPanel.SetDock(actionPanel, Dock.Top);
        DockPanel.SetDock(_summaryTextBlock, Dock.Top);

        rootPanel.Children.Add(actionPanel);
        rootPanel.Children.Add(_summaryTextBlock);
        rootPanel.Children.Add(_detailsTextBox);

        Content = rootPanel;

        Loaded += async (_, _) => await RefreshHealthReportAsync();
    }

    private static Button CreateButton(string text, RoutedEventHandler onClick)
    {
        var button = new Button
        {
            Content = text,
            Margin = new Thickness(0, 0, 12, 12),
            Padding = new Thickness(14, 8, 14, 8),
            MinWidth = 130
        };
        button.Click += onClick;
        return button;
    }

    private async System.Threading.Tasks.Task RefreshHealthReportAsync()
    {
        try
        {
            SetBusyState(true);
            _currentReport = await _healthCheckService.RunAsync();
            _summaryTextBlock.Text = LocalizationManager.TF("MW.HealthSummary", "系统状态: {0}    生成时间: {1:yyyy-MM-dd HH:mm:ss}", ToStatusText(_currentReport.Status), _currentReport.GeneratedAt);
            _summaryTextBlock.Foreground = ToBrush(_currentReport.Status);
            _detailsTextBox.Text = BuildReportText(_currentReport);
        }
        catch (Exception ex)
        {
            _summaryTextBlock.Text = LocalizationManager.T("MW.HealthFailedSummary", "系统状态: 健康检查失败");
            _summaryTextBlock.Foreground = Brushes.IndianRed;
            _detailsTextBox.Text = LocalizationManager.TF("MW.HealthFailedDetail", "健康检查失败:{0}{1}", Environment.NewLine, ex);
        }
        finally
        {
            SetBusyState(false);
        }
    }

    private void CreateBackup()
    {
        try
        {
            var result = _databaseMaintenanceService.CreateBackup("manual");
            if (!result.Success)
            {
                MessageBox.Show(result.ErrorMessage ?? LocalizationManager.T("MW.BackupFailed", "数据库备份失败"), LocalizationManager.T("MW.BackupFailedTitle", "备份失败"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show(
                LocalizationManager.TF("MW.BackupDone", "数据库备份已完成：{0}{1}", Environment.NewLine, result.BackupFilePath),
                LocalizationManager.T("MW.BackupDoneTitle", "备份成功"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            _ = RefreshHealthReportAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(LocalizationManager.TF("MW.BackupFailedWithReason", "数据库备份失败：{0}", ex.Message), LocalizationManager.T("Msg.Error", "错误"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RestoreBackup()
    {
        try
        {
            var dialog = new OpenFileDialog
            {
                Title = LocalizationManager.T("MW.RestoreDialogTitle", "选择要恢复的数据库备份"),
                Filter = LocalizationManager.T("MW.FilterSQLite", "SQLite 数据库|*.db|所有文件|*.*"),
                InitialDirectory = _databaseMaintenanceService.GetBackupDirectory(),
                CheckFileExists = true
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            var confirmResult = MessageBox.Show(
                LocalizationManager.T("MW.RestoreConfirm", "恢复数据库将覆盖当前数据库文件。系统会先自动为当前数据库创建一个恢复前备份。恢复完成后建议立即重启应用。\n\n是否继续？"),
                LocalizationManager.T("MW.RestoreConfirmTitle", "确认恢复"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmResult != MessageBoxResult.Yes)
            {
                return;
            }

            var preRestoreBackup = _databaseMaintenanceService.CreateBackup("pre_restore");
            if (!preRestoreBackup.Success)
            {
                MessageBox.Show(preRestoreBackup.ErrorMessage ?? LocalizationManager.T("MW.PreRestoreFailed", "恢复前备份失败"), LocalizationManager.T("MW.RestoreFailedTitle", "恢复失败"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var restoreResult = _databaseMaintenanceService.RestoreBackup(dialog.FileName, overwriteExisting: true);
            if (!restoreResult.Success)
            {
                MessageBox.Show(restoreResult.ErrorMessage ?? LocalizationManager.T("MW.RestoreFailed", "数据库恢复失败"), LocalizationManager.T("MW.RestoreFailedTitle", "恢复失败"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show(
                LocalizationManager.TF("MW.RestoreDone", "数据库恢复成功：{0}{1}{2}{2}恢复前备份：{3}{2}{2}请重启应用以重新加载数据库。", Environment.NewLine, dialog.FileName, Environment.NewLine, preRestoreBackup.BackupFilePath),
                LocalizationManager.T("MW.RestoreDoneTitle", "恢复成功"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            _ = RefreshHealthReportAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(LocalizationManager.TF("MW.RestoreFailedWithReason", "数据库恢复失败：{0}", ex.Message), LocalizationManager.T("Msg.Error", "错误"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenDataDirectory()
    {
        var dataDirectory = _currentReport != null
            ? Path.GetDirectoryName(_currentReport.DatabasePath)
            : Path.GetDirectoryName(_databaseMaintenanceService.GetDatabaseFilePath());

        OpenDirectory(dataDirectory, LocalizationManager.T("MW.DirData", "数据目录"));
    }

    private async System.Threading.Tasks.Task ExportDiagnosticBundleAsync()
    {
        try
        {
            var dialog = new SaveFileDialog
            {
                Title = LocalizationManager.T("MW.BtnExportDiagnostics", "导出诊断包"),
                Filter = LocalizationManager.T("MW.FilterZip", "ZIP 文件|*.zip"),
                FileName = $"NovelManagement_Diagnostic_{DateTime.Now:yyyyMMdd_HHmmss}.zip",
                AddExtension = true,
                DefaultExt = "zip"
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            SetBusyState(true);
            var result = await _diagnosticBundleService.ExportAsync(dialog.FileName);
            if (!result.Success)
            {
                MessageBox.Show(result.ErrorMessage ?? LocalizationManager.T("MW.ExportFailed", "诊断包导出失败"), LocalizationManager.T("MW.ExportFailedTitle", "导出失败"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show(
                LocalizationManager.TF("MW.ExportDone", "诊断包已导出：{0}{1}", Environment.NewLine, result.ZipFilePath),
                LocalizationManager.T("MW.ExportDoneTitle", "导出成功"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(LocalizationManager.TF("MW.ExportFailedWithReason", "导出诊断包失败：{0}", ex.Message), LocalizationManager.T("Msg.Error", "错误"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetBusyState(false);
        }
    }

    private void ReopenOnboarding()
    {
        try
        {
            var onboardingWindow = App.ServiceProvider?.GetService<FirstRunOnboardingWindow>();
            if (onboardingWindow == null)
            {
                MessageBox.Show(LocalizationManager.T("MW.OnboardingNotInit", "首次启动向导未初始化"), LocalizationManager.T("Msg.Error", "错误"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            onboardingWindow.Owner = this;
            onboardingWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show(LocalizationManager.TF("MW.OpenOnboardingFailed", "打开首次启动向导失败：{0}", ex.Message), LocalizationManager.T("Msg.Error", "错误"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ChangeLaunchPassword()
    {
        try
        {
            var dialog = new ChangePasswordDialog { Owner = this };
            dialog.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show(LocalizationManager.TF("MW.OpenChangePwdFailed", "打开修改启动密码对话框失败：{0}", ex.Message), LocalizationManager.T("Msg.Error", "错误"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenDirectory(string? path, string displayName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                MessageBox.Show(LocalizationManager.TF("MW.DirNotExist", "{0}不存在", displayName), LocalizationManager.T("Msg.Tip", "提示"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(LocalizationManager.TF("MW.OpenFailed", "打开{0}失败：{1}", displayName, ex.Message), LocalizationManager.T("Msg.Error", "错误"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SetBusyState(bool isBusy)
    {
        _refreshButton.IsEnabled = !isBusy;
        _backupButton.IsEnabled = !isBusy;
        _restoreButton.IsEnabled = !isBusy;
        _exportDiagnosticButton.IsEnabled = !isBusy;
    }

    private static string BuildReportText(ProductionHealthReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine(LocalizationManager.T("MW.ReportTitle", "发布与运维健康检查报告"));
        builder.AppendLine(new string('=', 72));
        builder.AppendLine(LocalizationManager.TF("MW.ReportOverall", "总体状态: {0}", ToStatusText(report.Status)));
        builder.AppendLine(LocalizationManager.TF("MW.ReportAppDataRoot", "应用数据根目录: {0}", report.AppDataRoot));
        builder.AppendLine(LocalizationManager.TF("MW.ReportDbFile", "数据库文件: {0}", report.DatabasePath));
        builder.AppendLine(LocalizationManager.TF("MW.ReportLogsDir", "日志目录: {0}", report.LogsDirectory));
        builder.AppendLine(LocalizationManager.TF("MW.ReportBackupsDir", "备份目录: {0}", report.BackupsDirectory));
        builder.AppendLine();
        builder.AppendLine(LocalizationManager.T("MW.ReportItemsHeader", "检查项明细:"));

        foreach (var item in report.Items)
        {
            builder.AppendLine($"[{ToStatusText(item.Status)}] {item.Name}");
            builder.AppendLine($"  {item.Message}");
        }

        return builder.ToString();
    }

    private static string ToStatusText(HealthStatus status)
    {
        return status switch
        {
            HealthStatus.Healthy => LocalizationManager.T("MW.StatusHealthy", "健康"),
            HealthStatus.Warning => LocalizationManager.T("Msg.Warning", "警告"),
            HealthStatus.Unhealthy => LocalizationManager.T("MW.StatusUnhealthy", "异常"),
            _ => status.ToString()
        };
    }

    private static Brush ToBrush(HealthStatus status)
    {
        return status switch
        {
            HealthStatus.Healthy => Brushes.ForestGreen,
            HealthStatus.Warning => Brushes.DarkOrange,
            HealthStatus.Unhealthy => Brushes.IndianRed,
            _ => Brushes.Gray
        };
    }
}
