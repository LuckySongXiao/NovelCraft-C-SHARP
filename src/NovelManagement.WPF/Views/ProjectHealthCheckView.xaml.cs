using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using MaterialDesignThemes.Wpf;
using NovelManagement.WPF.Localization;
using NovelManagement.WPF.Services;

namespace NovelManagement.WPF.Views
{
    /// <summary>
    /// 项目体检报告页：一致性检查 + 质量检查的结果列表、严重性排序、跳转定位与复检。
    /// </summary>
    public partial class ProjectHealthCheckView : UserControl, INavigationAwareView, INavigationRefreshableView
    {
        private readonly ProjectHealthCheckService? _healthCheckService;
        private readonly ProjectContextService? _projectContextService;
        private readonly NavigationService? _navigationService;

        private Guid _currentProjectId;
        private string _currentProjectName = string.Empty;
        private bool _isChecking;
        private ProjectHealthCheckMode _lastMode = ProjectHealthCheckMode.All;
        private readonly ObservableCollection<ProjectHealthIssue> _issues = new();

        public ProjectHealthCheckView()
        {
            InitializeComponent();

            _healthCheckService = App.ServiceProvider?.GetService<ProjectHealthCheckService>();
            _projectContextService = App.ServiceProvider?.GetService<ProjectContextService>();
            _navigationService = App.ServiceProvider?.GetService<NavigationService>();

            IssuesList.ItemsSource = _issues;
        }

        /// <inheritdoc/>
        public void OnNavigatedTo(NavigationContext context)
        {
            if (context.ProjectId.HasValue)
            {
                _currentProjectId = context.ProjectId.Value;
            }
            else
            {
                _currentProjectId = _projectContextService?.CurrentProjectId ?? Guid.Empty;
            }

            _currentProjectName = context.ProjectName
                ?? _projectContextService?.CurrentProjectName
                ?? string.Empty;

            UpdateProjectInfo();

            if (context.Payload is ProjectHealthCheckMode mode)
            {
                _lastMode = mode;
                _ = RunChecksAsync(mode);
            }
        }

        /// <inheritdoc/>
        public async Task RefreshOnProjectChangedAsync(Guid? projectId, string? projectName)
        {
            _currentProjectId = projectId ?? Guid.Empty;
            _currentProjectName = projectName ?? string.Empty;
            UpdateProjectInfo();

            _issues.Clear();
            UpdateSummary();
            IssueDetailPanel.Children.Clear();
            AddDetailPlaceholder(LocalizationManager.T("HC.SelectIssueHint", "选择上方问题查看详情与修复建议"));
            JumpButton.IsEnabled = false;

            if (_currentProjectId != Guid.Empty && _issues.Count == 0)
            {
                // 项目切换后自动复检，保持报告与当前项目一致
                await RunChecksAsync(_lastMode);
            }
        }

        private void UpdateProjectInfo()
        {
            ProjectInfoText.Text = _currentProjectId == Guid.Empty
                ? LocalizationManager.T("HC.SelectProjectFirst", "请先选择项目")
                : LocalizationManager.TF("HC.CurrentProject", "当前项目：{0}（{1}）", _currentProjectName, $"{_currentProjectId:N}");
        }

        #region 检查入口

        private async void ConsistencyCheckButton_Click(object sender, RoutedEventArgs e)
        {
            await RunChecksAsync(ProjectHealthCheckMode.Consistency);
        }

        private async void QualityCheckButton_Click(object sender, RoutedEventArgs e)
        {
            await RunChecksAsync(ProjectHealthCheckMode.Quality);
        }

        private async void FullCheckButton_Click(object sender, RoutedEventArgs e)
        {
            await RunChecksAsync(ProjectHealthCheckMode.All);
        }

        private async void ReCheckButton_Click(object sender, RoutedEventArgs e)
        {
            await RunChecksAsync(_lastMode);
        }

        private async Task RunChecksAsync(ProjectHealthCheckMode mode)
        {
            if (_isChecking)
            {
                return;
            }

            if (_currentProjectId == Guid.Empty)
            {
                MessageBox.Show(LocalizationManager.T("HC.SelectProjectMsg", "请先在项目管理中选择一个项目。"), LocalizationManager.T("HC.MsgTitle", "项目体检"),
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_healthCheckService == null)
            {
                MessageBox.Show(LocalizationManager.T("HC.ServiceNotInit", "健康检查服务未初始化。"), LocalizationManager.T("HC.MsgTitle", "项目体检"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                _isChecking = true;
                _lastMode = mode;
                SetCheckButtonsEnabled(false);

                IssueDetailPanel.Children.Clear();
                AddDetailPlaceholder(LocalizationManager.T("HC.Checking", "正在检查，请稍候……"));

                var issues = await _healthCheckService.RunChecksAsync(_currentProjectId, mode);

                _issues.Clear();
                foreach (var issue in issues)
                {
                    _issues.Add(issue);
                }

                UpdateSummary();
                IssueDetailPanel.Children.Clear();
                AddDetailPlaceholder(issues.Count == 0
                    ? LocalizationManager.T("HC.NoIssues", "检查完成，未发现问题。")
                    : LocalizationManager.T("HC.SelectIssueHint", "选择上方问题查看详情与修复建议"));
                JumpButton.IsEnabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(LocalizationManager.TF("HC.CheckFailed", "执行检查失败：{0}", ex.Message), LocalizationManager.T("HC.MsgTitle", "项目体检"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isChecking = false;
                SetCheckButtonsEnabled(true);
            }
        }

        private void SetCheckButtonsEnabled(bool enabled)
        {
            ConsistencyCheckButton.IsEnabled = enabled;
            QualityCheckButton.IsEnabled = enabled;
            FullCheckButton.IsEnabled = enabled;
            ReCheckButton.IsEnabled = enabled;
        }

        #endregion

        #region 结果展示

        private void IssuesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IssuesList.SelectedItem is ProjectHealthIssue issue)
            {
                ShowIssueDetail(issue);
            }
        }

        private void IssuesList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (IssuesList.SelectedItem is ProjectHealthIssue issue)
            {
                JumpToIssue(issue);
            }
        }

        private void ShowIssueDetail(ProjectHealthIssue issue)
        {
            IssueDetailPanel.Children.Clear();

            var severityBrush = issue.Severity switch
            {
                "错误" => new SolidColorBrush(Color.FromRgb(0xD3, 0x2F, 0x2F)),
                "警告" => new SolidColorBrush(Color.FromRgb(0xF5, 0x7C, 0x00)),
                _ => new SolidColorBrush(Color.FromRgb(0x19, 0x76, 0xD2))
            };

            IssueDetailPanel.Children.Add(new TextBlock
            {
                Inlines = { new System.Windows.Documents.Run($"[{issue.Severity}] ") { Foreground = severityBrush, FontWeight = FontWeights.Bold },
                            new System.Windows.Documents.Run(issue.Category) { FontWeight = FontWeights.Bold } },
                FontSize = 15,
                Margin = new Thickness(0, 0, 0, 8)
            });

            IssueDetailPanel.Children.Add(new TextBlock
            {
                Text = issue.Description,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 8)
            });

            if (issue.TargetId.HasValue || !string.IsNullOrWhiteSpace(issue.TargetName))
            {
                IssueDetailPanel.Children.Add(new TextBlock
                {
                    Inlines =
                    {
                        new System.Windows.Documents.Run(LocalizationManager.T("HC.TargetLabel", "目标：")) { FontWeight = FontWeights.Medium },
                        new System.Windows.Documents.Run($"{issue.TargetType} · {issue.TargetName}")
                    },
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 8)
                });
            }

            IssueDetailPanel.Children.Add(new TextBlock
            {
                Inlines =
                {
                    new System.Windows.Documents.Run(LocalizationManager.T("HC.DetailLabel", "详情与建议：")) { FontWeight = FontWeights.Medium },
                    new System.Windows.Documents.Run(issue.Detail)
                },
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32)),
                Margin = new Thickness(0, 0, 0, 8)
            });

            JumpButton.IsEnabled = true;
        }

        private void AddDetailPlaceholder(string text)
        {
            IssueDetailPanel.Children.Add(new TextBlock
            {
                Text = text,
                FontStyle = FontStyles.Italic,
                Foreground = new SolidColorBrush(Color.FromRgb(0x90, 0xA4, 0xAE)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 8, 0, 0)
            });
        }

        private void UpdateSummary()
        {
            var errorCount = _issues.Count(issue => issue.Severity == "错误");
            var warningCount = _issues.Count(issue => issue.Severity == "警告");
            var infoCount = _issues.Count(issue => issue.Severity == "提示");

            ErrorCountText.Text = LocalizationManager.TF("HC.ErrorCount", "错误 {0}", errorCount);
            WarningCountText.Text = LocalizationManager.TF("HC.WarningCount", "警告 {0}", warningCount);
            InfoCountText.Text = LocalizationManager.TF("HC.InfoCount", "提示 {0}", infoCount);
            IssueCountText.Text = LocalizationManager.TF("HC.IssueCountFmt", "{0} 个问题", _issues.Count);

            LastCheckTimeText.Text = _issues.Count == 0 && errorCount == 0 && warningCount == 0 && infoCount == 0
                ? LocalizationManager.TF("HC.NotCheckedPassed", "尚未检查或检查通过（{0}）", DateTime.Now.ToString("HH:mm:ss"))
                : LocalizationManager.TF("HC.LastCheck", "最近检查：{0}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        #endregion

        #region 跳转定位

        private void JumpButton_Click(object sender, RoutedEventArgs e)
        {
            if (IssuesList.SelectedItem is ProjectHealthIssue issue)
            {
                JumpToIssue(issue);
            }
        }

        private void JumpToIssue(ProjectHealthIssue issue)
        {
            var target = ResolveNavigationTarget(issue.Category);
            if (target == null)
            {
                return;
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(issue.TargetName))
                {
                    Clipboard.SetText(issue.TargetName);
                }

                _navigationService?.NavigateTo(target.Value, new NavigationContext
                {
                    ProjectId = _currentProjectId == Guid.Empty ? null : _currentProjectId,
                    ProjectName = string.IsNullOrWhiteSpace(_currentProjectName) ? null : _currentProjectName,
                    Source = "ProjectHealthCheck",
                    Payload = BuildJumpPayload(issue)
                });

                MessageBox.Show(
                    LocalizationManager.TF("HC.JumpSuccess", "已跳转到“{0}”。\n目标：{1} · {2}（名称已复制到剪贴板，便于查找）", ResolveTargetDisplayName(target.Value), issue.TargetType, issue.TargetName),
                    LocalizationManager.T("HC.JumpTitle", "跳转定位"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(LocalizationManager.TF("HC.JumpFailed", "跳转失败：{0}", ex.Message), LocalizationManager.T("HC.JumpTitle", "跳转定位"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 按问题类别构造目标视图可识别的定位参数，支持实体级选中高亮。
        /// </summary>
        private static object? BuildJumpPayload(ProjectHealthIssue issue)
        {
            if (issue.TargetId == null)
            {
                return null;
            }

            return issue.Category switch
            {
                // 世界设定页与卷章页已有各自成熟的定位参数，直接复用
                "世界设定" => new WorldSettingNavigationPayload { SettingId = issue.TargetId },
                "章节正文" => new VolumeNavigationPayload { ChapterId = issue.TargetId },
                _ => new EntityHighlightNavigationPayload
                {
                    TargetId = issue.TargetId,
                    TargetName = issue.TargetName,
                    TargetType = issue.TargetType
                }
            };
        }

        private static NavigationTarget? ResolveNavigationTarget(string category) => category switch
        {
            "角色档案" => NavigationTarget.CharacterManagement,
            "人物关系" => NavigationTarget.RelationshipNetwork,
            "时间线" => NavigationTarget.Timeline,
            "势力组织" => NavigationTarget.FactionManagement,
            "剧情大纲" => NavigationTarget.PlotManagement,
            "世界设定" => NavigationTarget.WorldSettingManagement,
            "章节正文" => NavigationTarget.VolumeManagement,
            _ => null
        };

        private static string ResolveTargetDisplayName(NavigationTarget target) => target switch
        {
            NavigationTarget.CharacterManagement => LocalizationManager.T("HC.TargetCharacter", "角色管理"),
            NavigationTarget.RelationshipNetwork => LocalizationManager.T("HC.TargetRelationship", "关系网络"),
            NavigationTarget.Timeline => LocalizationManager.T("HC.TargetTimeline", "时间线管理"),
            NavigationTarget.FactionManagement => LocalizationManager.T("HC.TargetFaction", "势力管理"),
            NavigationTarget.PlotManagement => LocalizationManager.T("HC.TargetPlot", "剧情管理"),
            NavigationTarget.WorldSettingManagement => LocalizationManager.T("HC.TargetWorldSetting", "世界设定管理"),
            NavigationTarget.VolumeManagement => LocalizationManager.T("HC.TargetVolume", "卷章管理"),
            _ => target.ToString()
        };

        #endregion
    }
}
