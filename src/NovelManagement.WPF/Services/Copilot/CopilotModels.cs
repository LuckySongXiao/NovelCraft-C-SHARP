using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace NovelManagement.WPF.Services.Copilot;

/// <summary>
/// 会话消息角色。
/// </summary>
public enum CopilotMessageRole
{
    User,
    Assistant,
    System
}

/// <summary>
/// 确认项状态。
/// </summary>
public enum ProposalStatus
{
    Pending,
    Accepted,
    Modified,
    Rejected,
    Expired
}

/// <summary>
/// 确认卡类型（对应五级流水线产物，Worldview 为第二期预留）。
/// </summary>
public enum ProposalKind
{
    /// <summary>全书总纲（单 item）。</summary>
    Outline,

    /// <summary>剧情线（多 item，映射 Plot）。</summary>
    PlotLine,

    /// <summary>分卷（多 item）。</summary>
    Volume,

    /// <summary>章节剧情草稿（多 item，映射 Chapter.Summary）。</summary>
    ChapterDraft,

    /// <summary>章节正文（单 item，映射 Chapter.Content）。</summary>
    ChapterContent,

    /// <summary>世界设定/角色/势力卡（第二期扩展预留）。</summary>
    Worldview
}

/// <summary>
/// 创作流水线阶段。
/// </summary>
public enum PipelineStage
{
    NotStarted,
    BlueprintPending,
    PlotLinesPending,
    VolumesPending,
    ChapterDraftsPending,
    ChapterContentInProgress,
    Completed
}

/// <summary>
/// 用户意图类别。
/// </summary>
public enum CopilotIntentKind
{
    /// <summary>切换页面（可带实体定位）。</summary>
    Navigate,

    /// <summary>本地查询（进度/统计）。</summary>
    Query,

    /// <summary>流水线动作（开始/继续/状态/重新生成）。</summary>
    PipelineAction,

    /// <summary>自由问答（RWKV 带项目上下文）。</summary>
    Chat
}

/// <summary>
/// 意图解析结果。
/// </summary>
public sealed class CopilotIntentResult
{
    /// <summary>意图类别。</summary>
    public CopilotIntentKind Kind { get; init; }

    /// <summary>导航目标（Navigate 类有效）。</summary>
    public NovelManagement.WPF.Services.NavigationTarget? Target { get; init; }

    /// <summary>实体名（如角色名/剧情线名，可为空）。</summary>
    public string? EntityName { get; init; }

    /// <summary>中文序号（「第三章」→ 3）。</summary>
    public int? OrdinalNumber { get; init; }

    /// <summary>流水线动作（start/resume/status/regen）。</summary>
    public string? PipelineAction { get; init; }

    /// <summary>命中方式（rule/rwkv/fallback），用于日志与调试。</summary>
    public string MatchedBy { get; init; } = "rule";

    /// <summary>流水线动作的用户原话（regen 整改意见透传）。</summary>
    public string? RawText { get; init; }
}

/// <summary>
/// 确认卡：一个流水线阶段的一批待采纳项。
/// </summary>
public sealed class PendingProposal : INotifyPropertyChanged
{
    private ProposalStatus _status = ProposalStatus.Pending;

    /// <summary>卡片唯一标识。</summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>卡片类型。</summary>
    public ProposalKind Kind { get; init; }

    /// <summary>卡片标题（如「全书总纲（第1版）」）。</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>阶段汇报文本。</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>待采纳项列表。</summary>
    public List<ProposalItem> Items { get; } = new();

    /// <summary>卡片状态。</summary>
    public ProposalStatus Status
    {
        get => _status;
        set
        {
            if (_status == value)
            {
                return;
            }

            _status = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
        }
    }

    /// <summary>创建时间。</summary>
    public DateTime CreatedAt { get; } = DateTime.Now;

    /// <summary>
    /// 章节关联处理的目标章节 ID（null 表示流水线常规卡片，非空表示直接更新该章节）。
    /// </summary>
    public Guid? TargetChapterId { get; init; }

    /// <summary>是否存在已采纳/已修改项。</summary>
    public bool HasAcceptedItems
    {
        get
        {
            foreach (var item in Items)
            {
                if (item.Status is ProposalStatus.Accepted or ProposalStatus.Modified)
                {
                    return true;
                }
            }
            return false;
        }
    }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;
}

/// <summary>
/// 确认卡内单条待采纳项。
/// </summary>
public sealed class ProposalItem : INotifyPropertyChanged
{
    private ProposalStatus _status = ProposalStatus.Pending;
    private string? _editedBody;

    /// <summary>项唯一标识。</summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>项标题（如剧情线名/卷名/章节名）。</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>项正文内容。</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>结构化字段（类型/优先级/卷次等）。</summary>
    public Dictionary<string, string> Fields { get; init; } = new(StringComparer.Ordinal);

    /// <summary>项状态。</summary>
    public ProposalStatus Status
    {
        get => _status;
        set
        {
            if (_status == value)
            {
                return;
            }

            _status = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPending)));
        }
    }

    /// <summary>用户「修改」后的文本（未修改为 null）。</summary>
    public string? EditedBody
    {
        get => _editedBody;
        set
        {
            if (_editedBody == value)
            {
                return;
            }

            _editedBody = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EditedBody)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayBody)));
        }
    }

    /// <summary>界面展示文本（优先展示用户修改后的内容）。</summary>
    public string DisplayBody => string.IsNullOrWhiteSpace(EditedBody) ? Body : EditedBody!;

    /// <summary>是否仍待裁决（控制按钮可用性）。</summary>
    public bool IsPending => Status == ProposalStatus.Pending;

    /// <summary>落库用最终文本（采纳时取 EditedBody ?? Body）。</summary>
    public string EffectiveBody => string.IsNullOrWhiteSpace(EditedBody) ? Body : EditedBody!;

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;
}

/// <summary>
/// 会话消息（含可选确认卡，UI 按 Proposal 是否为 null 选择模板）。
/// </summary>
public sealed class CopilotMessageItem
{
    /// <summary>消息角色。</summary>
    public CopilotMessageRole Role { get; init; }

    /// <summary>消息文本。</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>时间戳。</summary>
    public DateTime Timestamp { get; } = DateTime.Now;

    /// <summary>附带的确认卡（纯文本消息为 null）。</summary>
    public PendingProposal? Proposal { get; init; }
}

/// <summary>
/// 创作流水线持久化状态（序列化为 JSON 存入系统 Plot 的 Outline 字段）。
/// </summary>
public sealed class PipelineState
{
    /// <summary>所属项目 ID。</summary>
    public Guid ProjectId { get; set; }

    /// <summary>项目名（提示词中的书名）。</summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>当前阶段。</summary>
    public PipelineStage Stage { get; set; } = PipelineStage.NotStarted;

    /// <summary>用户原始创作想法。</summary>
    public string UserVision { get; set; } = string.Empty;

    /// <summary>已采纳的全书总纲全文。</summary>
    public string Blueprint { get; set; } = string.Empty;

    /// <summary>已采纳剧情线 ID 列表。</summary>
    public List<Guid> ConfirmedPlotLineIds { get; set; } = new();

    /// <summary>已确认分卷数。</summary>
    public int ConfirmedVolumeCount { get; set; }

    /// <summary>当前写作卷序（1 起）。</summary>
    public int CurrentVolumeOrder { get; set; } = 1;

    /// <summary>当前写作章序（卷内，1 起）。</summary>
    public int CurrentChapterOrder { get; set; } = 1;

    /// <summary>最近一次整改意见。</summary>
    public string? LastFeedback { get; set; }

    /// <summary>状态更新时间。</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    /// <summary>流水线是否全部完成。</summary>
    public bool Finished { get; set; }
}

/// <summary>
/// 章节关联引用：用户在输入区选择的「书 → 卷 → 章」，会话进入章节处理模式。
/// </summary>
public sealed class ChapterReferral
{
    /// <summary>所属书籍项目 ID（支持跨项目关联）。</summary>
    public Guid ProjectId { get; init; }

    /// <summary>书名。</summary>
    public string ProjectName { get; init; } = string.Empty;

    /// <summary>分卷 ID。</summary>
    public Guid VolumeId { get; init; }

    /// <summary>卷名。</summary>
    public string VolumeTitle { get; init; } = string.Empty;

    /// <summary>卷序（1 起）。</summary>
    public int VolumeOrder { get; init; }

    /// <summary>章节 ID。</summary>
    public Guid ChapterId { get; init; }

    /// <summary>章节标题。</summary>
    public string ChapterTitle { get; init; } = string.Empty;

    /// <summary>章序（卷内，1 起）。</summary>
    public int ChapterOrder { get; init; }

    /// <summary>章节梗概（关联时快照，供无正文成文使用）。</summary>
    public string? Summary { get; init; }
}
