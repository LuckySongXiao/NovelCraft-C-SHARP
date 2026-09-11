using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using NovelManagement.WPF.Localization;
using static NovelManagement.WPF.Localization.LocalizationManager;
using NovelManagement.WPF.Services.Copilot;

namespace NovelManagement.WPF.Views.Copilot
{
    /// <summary>
    /// AI 创作助手抽屉面板：消息流 + 确认卡裁决 + 用户输入。
    /// 仅绑定 <see cref="CopilotSessionService"/> 状态；导航与落库均由服务层完成。
    /// </summary>
    public partial class CopilotPanel : UserControl
    {
        private readonly CopilotSessionService? _session;
        private readonly CreationPipelineService? _pipeline;

        /// <summary>请求收起抽屉（由 MainWindow 订阅执行列宽切换）。</summary>
        public event EventHandler? CloseRequested;

        public CopilotPanel()
        {
            InitializeComponent();

            _session = App.ServiceProvider?.GetService<CopilotSessionService>();
            _pipeline = App.ServiceProvider?.GetService<CreationPipelineService>();

            if (_session != null)
            {
                DataContext = _session;
                MessageItemsControl.ItemsSource = _session.Messages;
                _session.Messages.CollectionChanged += OnMessagesChanged;
                _session.SessionChanged += OnSessionChanged;
                RefreshSessionUi();
            }
            else
            {
                InputTextBox.IsEnabled = false;
            }
        }

        /// <summary>
        /// 绑定项目上下文（MainWindow 项目切换时调用）。
        /// </summary>
        public void BindProject(Guid? projectId, string projectName)
        {
            if (_session == null)
            {
                return;
            }

            if (projectId.HasValue)
            {
                _session.OpenForProject(projectId.Value, projectName);
            }
            else
            {
                _session.Close();
            }
        }

        #region 消息流刷新

        private void OnMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(() => ScrollToBottom());
                return;
            }

            ScrollToBottom();
        }

        private void ScrollToBottom()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, () => MessageScrollViewer.ScrollToEnd());
        }

        private void OnSessionChanged(object? sender, EventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(() => RefreshSessionUi());
                return;
            }

            RefreshSessionUi();
        }

        /// <summary>
        /// 刷新阶段徽标与修改模式提示条。
        /// </summary>
        private void RefreshSessionUi()
        {
            if (_session == null)
            {
                return;
            }

            if (_pipeline != null && _pipeline.CurrentProjectId == _session.CurrentProjectId)
            {
                var stage = _pipeline.CurrentStage;
                var generating = _pipeline.IsGenerating;
                StageBadgeText.Text = stage == PipelineStage.NotStarted
                    ? T("CP.StageNotStartedHint", "流水线：未开始（回复「开始规划」启动）")
                    : generating
                        ? TF("CP.StageGenerating", $"流水线：{DescribeStage(stage)} · 生成中…", DescribeStage(stage))
                        : TF("CP.Stage", $"流水线：{DescribeStage(stage)}", DescribeStage(stage));
            }
            else
            {
                StageBadgeText.Text = T("CP.StageNotStarted", "流水线：未开始");
            }

            EditBanner.Visibility = _session.IsEditing ? Visibility.Visible : Visibility.Collapsed;

            // 章节关联提示条：已关联时显示目标章节信息
            var referral = _session.ChapterRef;
            if (referral != null)
            {
                ChapterRefText.Text = TF("CP.ChapterRef", $"已关联《{referral.ProjectName}》 第{referral.VolumeOrder}卷「{referral.VolumeTitle}」 第{referral.ChapterOrder}章《{referral.ChapterTitle}》 — 输入要求直接处理本章", referral.ProjectName, referral.VolumeOrder, referral.VolumeTitle, referral.ChapterOrder, referral.ChapterTitle);
                ChapterRefBanner.Visibility = Visibility.Visible;
            }
            else
            {
                ChapterRefBanner.Visibility = Visibility.Collapsed;
            }
        }

        private static string DescribeStage(PipelineStage stage) => stage switch
        {
            PipelineStage.BlueprintPending => T("CP.StageBlueprint", "总纲规划"),
            PipelineStage.PlotLinesPending => T("CP.StagePlotLines", "剧情线规划"),
            PipelineStage.VolumesPending => T("CP.StageVolumes", "分卷规划"),
            PipelineStage.ChapterDraftsPending => T("CP.StageChapterDrafts", "章节剧情草稿"),
            PipelineStage.ChapterContentInProgress => T("CP.StageChapterContent", "章节正文创作"),
            PipelineStage.Completed => T("CP.StageCompleted", "已完成"),
            _ => T("CP.StageNone", "未开始")
        };

        #endregion

        #region 输入

        private async void Send_Click(object sender, RoutedEventArgs e) => await SendInputAsync();

        private async void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers != ModifierKeys.Shift)
            {
                e.Handled = true;
                await SendInputAsync();
            }
        }

        private async System.Threading.Tasks.Task SendInputAsync()
        {
            if (_session == null)
            {
                return;
            }

            var text = InputTextBox.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            InputTextBox.Clear();
            SendButton.IsEnabled = false;
            try
            {
                await _session.SendUserMessageAsync(text);
            }
            finally
            {
                SendButton.IsEnabled = true;
                InputTextBox.Focus();
            }
        }

        #endregion

        #region 章节关联

        private void AttachChapter_Click(object sender, RoutedEventArgs e)
        {
            if (_session == null)
            {
                return;
            }

            var owner = Window.GetWindow(this);
            var dialog = new ChapterRefPickerDialog { Owner = owner };
            if (dialog.ShowDialog() == true && dialog.Result != null)
            {
                _session.AttachChapterRef(dialog.Result);
            }
        }

        private void ClearChapterRef_Click(object sender, RoutedEventArgs e)
        {
            _session?.ClearChapterRef();
        }

        #endregion

        #region 确认卡操作

        private ProposalItem? GetItem(object sender) =>
            (sender as FrameworkElement)?.DataContext as ProposalItem;

        private void AcceptItem_Click(object sender, RoutedEventArgs e)
        {
            var item = GetItem(sender);
            if (item != null && _session?.CurrentProposal != null)
            {
                _session.AcceptItem(_session.CurrentProposal.Id, item.Id);
            }
        }

        private void RejectItem_Click(object sender, RoutedEventArgs e)
        {
            var item = GetItem(sender);
            if (item != null && _session?.CurrentProposal != null)
            {
                _session.RejectItem(_session.CurrentProposal.Id, item.Id);
            }
        }

        private void EditItem_Click(object sender, RoutedEventArgs e)
        {
            var item = GetItem(sender);
            if (item == null || _session?.CurrentProposal == null)
            {
                return;
            }

            var original = _session.BeginItemEdit(_session.CurrentProposal.Id, item.Id);
            if (original != null)
            {
                InputTextBox.Text = original;
                InputTextBox.SelectAll();
                InputTextBox.Focus();
            }
        }

        private void CancelEdit_Click(object sender, RoutedEventArgs e)
        {
            _session?.CancelItemEdit();
            InputTextBox.Clear();
        }

        private async void AcceptAll_Click(object sender, RoutedEventArgs e)
        {
            if (_session?.CurrentProposal == null)
            {
                return;
            }

            var proposal = _session.CurrentProposal;
            foreach (var item in proposal.Items.Where(i => i.IsPending))
            {
                item.Status = string.IsNullOrWhiteSpace(item.EditedBody) ? ProposalStatus.Accepted : ProposalStatus.Modified;
            }

            await _session.AcceptProposalAsync(proposal.Id);
        }

        private async void RejectCard_Click(object sender, RoutedEventArgs e)
        {
            if (_session?.CurrentProposal == null)
            {
                return;
            }

            await _session.RejectProposalAsync(_session.CurrentProposal.Id);
        }

        #endregion

        private void ClosePanel_Click(object sender, RoutedEventArgs e) =>
            CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    #region 模板选择器与值转换器

    /// <summary>
    /// 消息模板选择：带确认卡的消息用确认卡模板，其余用文本模板。
    /// </summary>
    public class CopilotMessageTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? TextTemplate { get; set; }
        public DataTemplate? ProposalTemplate { get; set; }

        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            return item is CopilotMessageItem { Proposal: not null }
                ? ProposalTemplate
                : TextTemplate;
        }
    }

    /// <summary>
    /// 消息角色 → 可见性（Param 指定仅哪个角色可见），XAML 标记扩展内联使用。
    /// </summary>
    [MarkupExtensionReturnType(typeof(Visibility))]
    public class RoleToVisibilityConverter : MarkupExtension, IValueConverter
    {
        public string Param { get; set; } = "User";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var visibleRole = Param switch
            {
                "Assistant" => CopilotMessageRole.Assistant,
                "System" => CopilotMessageRole.System,
                _ => CopilotMessageRole.User
            };
            return value is CopilotMessageRole role && role == visibleRole
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();

        public override object ProvideValue(IServiceProvider serviceProvider) => this;
    }

    /// <summary>
    /// 确认卡/项状态 → 中文徽标文本。
    /// CardMode=true：整卡状态（Pending 之外都显示）；
    /// CardMode=false：单项状态。
    /// </summary>
    [MarkupExtensionReturnType(typeof(string))]
    public class ProposalStatusTextConverter : MarkupExtension, IValueConverter
    {
        public bool CardMode { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not ProposalStatus status)
            {
                return string.Empty;
            }

            return status switch
            {
                ProposalStatus.Pending => CardMode ? string.Empty : T("CP.StatusItemPending", "待裁决"),
                ProposalStatus.Accepted => T("CP.StatusAccepted", "已采纳"),
                ProposalStatus.Modified => T("CP.StatusModified", "已修改"),
                ProposalStatus.Rejected => CardMode ? T("CP.StatusCardRejected", "已放弃整卡") : T("CP.StatusRejected", "已放弃"),
                ProposalStatus.Expired => T("CP.StatusExpired", "已过期（已被新卡片取代）"),
                _ => status.ToString()
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();

        public override object ProvideValue(IServiceProvider serviceProvider) => this;
    }

    /// <summary>
    /// 整卡状态 → 可见性（仅 Pending 时显示操作按钮）。
    /// </summary>
    [MarkupExtensionReturnType(typeof(Visibility))]
    public class PendingToVisibilityConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is ProposalStatus status && status == ProposalStatus.Pending
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();

        public override object ProvideValue(IServiceProvider serviceProvider) => this;
    }

    #endregion
}
