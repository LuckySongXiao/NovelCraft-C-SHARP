using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NovelManagement.AI.Services.RWKV;
using NovelManagement.AI.Utilities;
using NovelManagement.Application.Services;
using NovelManagement.Core.Entities;
using NovelManagement.WPF.Localization;
using static NovelManagement.WPF.Localization.LocalizationManager;

namespace NovelManagement.WPF.Services.Copilot;

/// <summary>
/// 创作助手会话服务：消息流、意图分派、确认卡管理与采纳落库的汇聚枢纽。
/// UI（CopilotPanel）只绑定本服务状态；流水线事件经本服务进入消息流；
/// 导航经 <see cref="NavigationRequested"/> 回调由 MainWindow 执行（避免服务反向依赖视图）。
/// </summary>
public class CopilotSessionService
{
    private readonly ILogger<CopilotSessionService> _logger;
    private readonly CopilotIntentService _intentService;
    private readonly CreationPipelineService _pipelineService;
    private readonly IRwkvLightningService _rwkvService;
    private readonly PlotService _plotService;
    private readonly VolumeService _volumeService;
    private readonly ChapterService _chapterService;

    /// <summary>当前项目 ID（面板绑定的项目上下文）。</summary>
    private Guid? _projectId;

    /// <summary>当前项目名。</summary>
    private string _projectName = string.Empty;

    /// <summary>确认卡「修改」模式目标（proposalId/itemId），null 表示非修改模式。</summary>
    private (Guid ProposalId, Guid ItemId)? _editTarget;

    /// <summary>当前关联的目标章节（输入区选择书/卷/章后进入章节处理模式，null 表示普通模式）。</summary>
    private ChapterReferral? _chapterRef;

    /// <summary>
    /// 导航请求回调：MainWindow 注入，参数（目标, 导航上下文）。
    /// </summary>
    public Action<NovelManagement.WPF.Services.NavigationTarget, NovelManagement.WPF.Services.NavigationContext?>? NavigationRequested { get; set; }

    /// <summary>
    /// 构造函数（订阅流水线事件，将其转发进消息流）。
    /// </summary>
    public CopilotSessionService(
        ILogger<CopilotSessionService> logger,
        CopilotIntentService intentService,
        CreationPipelineService pipelineService,
        IRwkvLightningService rwkvService,
        PlotService plotService,
        VolumeService volumeService,
        ChapterService chapterService)
    {
        _logger = logger;
        _intentService = intentService;
        _pipelineService = pipelineService;
        _rwkvService = rwkvService;
        _plotService = plotService;
        _volumeService = volumeService;
        _chapterService = chapterService;

        _pipelineService.ProposalReady += OnPipelineProposalReady;
        _pipelineService.MessageReady += OnPipelineMessageReady;
        _pipelineService.ProgressChanged += (_, _) => SessionChanged?.Invoke(this, EventArgs.Empty);
    }

    #region 会话状态

    /// <summary>消息流（UI 绑定）。</summary>
    public ObservableCollection<CopilotMessageItem> Messages { get; } = new();

    /// <summary>当前待裁决确认卡（同一时刻仅一张 Pending）。</summary>
    public PendingProposal? CurrentProposal { get; private set; }

    /// <summary>当前项目 ID。</summary>
    public Guid? CurrentProjectId => _projectId;

    /// <summary>当前项目名。</summary>
    public string CurrentProjectName => _projectName;

    /// <summary>是否处于确认卡「修改」模式。</summary>
    public bool IsEditing => _editTarget != null;

    /// <summary>当前关联的目标章节（null 表示未关联）。</summary>
    public ChapterReferral? ChapterRef => _chapterRef;

    /// <summary>会话状态变化事件（UI 刷新徽标/按钮）。</summary>
    public event EventHandler? SessionChanged;

    #endregion

    #region 会话生命周期

    /// <summary>
    /// 绑定项目并初始化会话（切换项目时清空消息流）。
    /// </summary>
    public void OpenForProject(Guid projectId, string projectName)
    {
        if (_projectId == projectId)
        {
            return;
        }

        _projectId = projectId;
        _projectName = projectName;
        Messages.Clear();
        CurrentProposal = null;
        _editTarget = null;
        _chapterRef = null;

        PostSystem(TF("CPS.Connected", $"已连接《{projectName}》的创作助手。你可以直接告诉我想法（例如规划这本书、看看第三章、打开剧情管理），我会帮你完成。", projectName));
    }

    /// <summary>
    /// 断开项目（项目切换时由面板调用）。
    /// </summary>
    public void Close()
    {
        _projectId = null;
        _projectName = string.Empty;
    }

    #endregion

    #region 消息入口与意图分派

    /// <summary>
    /// 用户消息入口：意图解析 → 分派（修改模式/流水线/导航/查询/问答）。
    /// </summary>
    public async Task SendUserMessageAsync(string text)
    {
        var input = (text ?? string.Empty).Trim();
        if (input.Length == 0 || _projectId == null)
        {
            return;
        }

        Messages.Add(new CopilotMessageItem { Role = CopilotMessageRole.User, Text = input });

        // 确认卡「修改」模式：本次输入作为该项的修改文本
        if (_editTarget != null)
        {
            ApplyItemEdit(input);
            return;
        }

        // 章节关联模式：所有输入直接作用于目标章节（提示条已说明，输入「取消关联」可解除）
        if (_chapterRef != null)
        {
            if (input.Contains("取消关联", StringComparison.Ordinal) || input.Contains("解除关联", StringComparison.Ordinal))
            {
                ClearChapterRef();
                return;
            }

            await HandleChapterRefInputAsync(input);
            return;
        }

        try
        {
            var intent = await _intentService.ParseAsync(input);
            switch (intent.Kind)
            {
                case CopilotIntentKind.PipelineAction:
                    await HandlePipelineActionAsync(intent, input);
                    break;
                case CopilotIntentKind.Navigate:
                    await HandleNavigateAsync(intent);
                    break;
                case CopilotIntentKind.Query:
                    await HandleQueryAsync(input);
                    break;
                default:
                    await HandleChatAsync(input);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创作助手处理消息失败");
            PostAssistant(TF("CPS.ProcessFailed", $"处理失败：{ex.Message}", ex.Message));
        }
        finally
        {
            SessionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// 流水线动作分派。
    /// </summary>
    private async Task HandlePipelineActionAsync(CopilotIntentResult intent, string rawText)
    {
        var projectId = _projectId!.Value;
        switch (intent.PipelineAction)
        {
            case "start":
                if (_pipelineService.CurrentStage != PipelineStage.NotStarted && _pipelineService.CurrentProjectId == projectId)
                {
                    PostAssistant(TF("CPS.PipelineInProgress", "流水线已在进行中（当前阶段：" + DescribeStage(_pipelineService.CurrentStage) + "）。回复「继续创作」推进，或「重新生成」调整当前阶段。", DescribeStage(_pipelineService.CurrentStage)));
                    return;
                }

                // 愿景来源：本条消息本身就是想法时直接使用；否则请用户描述
                if (rawText.Contains("开始规划", StringComparison.Ordinal) || rawText.Contains("开始创作", StringComparison.Ordinal))
                {
                    PostAssistant(T("CPS.AskIdea", "请先用一句话告诉我你对这本书的想法（题材、主角、核心冲突或想写的故事），我会据此规划全书总纲。"));
                    _awaitingVision = true;
                    return;
                }

                await _pipelineService.StartPlanningAsync(projectId, _projectName, rawText);
                return;
            case "resume":
                if (_pipelineService.CurrentProjectId == projectId && _pipelineService.CurrentStage == PipelineStage.Completed)
                {
                    await _pipelineService.RequestAppendVolumeAsync(projectId);
                    return;
                }

                await _pipelineService.TryResumeAsync(projectId);
                return;
            case "status":
                PostAssistant(BuildPipelineStatusText());
                return;
            case "regen":
                if (_pipelineService.CurrentProjectId != projectId || _pipelineService.CurrentStage == PipelineStage.NotStarted)
                {
                    PostAssistant(T("CPS.NoStageToRegenerate", "当前没有进行中的流水线阶段可重新生成。回复「开始规划」可启动。"));
                    return;
                }

                await _pipelineService.RegenerateCurrentStageAsync(rawText, projectId);
                return;
            default:
                PostAssistant(BuildPipelineStatusText());
                return;
        }
    }

    /// <summary>是否等待用户输入创作愿景（下一条非流水线消息作为愿景）。</summary>
    private bool _awaitingVision;

    /// <summary>
    /// 导航分派：回调 MainWindow 执行页面切换，可带实体定位参数。
    /// </summary>
    private Task HandleNavigateAsync(CopilotIntentResult intent)
    {
        if (intent.Target == null)
        {
            return Task.CompletedTask;
        }

        object? payload = null;
        if (!string.IsNullOrWhiteSpace(intent.EntityName))
        {
            payload = new NovelManagement.WPF.Services.EntityHighlightNavigationPayload
            {
                TargetName = intent.EntityName,
                TargetType = DescribeTarget(intent.Target.Value)
            };
        }

        NavigationRequested?.Invoke(intent.Target.Value, new NovelManagement.WPF.Services.NavigationContext
        {
            ProjectId = _projectId,
            ProjectName = _projectName,
            Source = "Copilot",
            Payload = payload
        });

        var destination = DescribeTarget(intent.Target.Value);
        var suffix = intent.OrdinalNumber.HasValue
            ? (intent.Target == NovelManagement.WPF.Services.NavigationTarget.VolumeManagement
                ? TF("CPS.LocateOrdinalChapter", $"（定位第{intent.OrdinalNumber.Value}章）", intent.OrdinalNumber.Value)
                : TF("CPS.LocateOrdinalItem", $"（定位第{intent.OrdinalNumber.Value}项）", intent.OrdinalNumber.Value))
            : (string.IsNullOrWhiteSpace(intent.EntityName) ? string.Empty : TF("CPS.LocateEntity", $"（定位：{intent.EntityName}）", intent.EntityName));
        PostAssistant(TF("CPS.SwitchedTo", $"已切换到{destination}{suffix}。", destination, suffix));
        return Task.CompletedTask;
    }

    /// <summary>
    /// 本地查询分派（进度/统计，模型零参与）。
    /// </summary>
    private async Task HandleQueryAsync(string rawText)
    {
        var projectId = _projectId!.Value;
        try
        {
            var volumes = await _volumeService.GetVolumeListAsync(projectId);
            var plots = await _plotService.GetPlotsByProjectIdAsync(projectId);
            var chapters = await _chapterService.GetChaptersByProjectIdAsync(projectId);
            var chapterList = chapters.ToList();
            var totalWords = chapterList.Sum(c => c.WordCount);
            var finishedChapters = chapterList.Count(c => !string.IsNullOrWhiteSpace(c.Content));

            var lines = new List<string>
            {
                TF("CPS.StatusHeader", $"《{_projectName}》当前进度：", _projectName),
                TF("CPS.StatusVolumes", $"- 卷宗：{volumes.Count()} 卷", volumes.Count()),
                TF("CPS.StatusPlots", $"- 剧情/剧情线：{plots.Count(p => p.Type != "系统" && p.Type != "总纲")} 条", plots.Count(p => p.Type != "系统" && p.Type != "总纲")),
                TF("CPS.StatusChapters", $"- 章节：{chapterList.Count} 章（已完成正文 {finishedChapters} 章，共 {totalWords} 字）", chapterList.Count, finishedChapters, totalWords)
            };

            var stageText = BuildPipelineStatusText();
            lines.Add(TF("CPS.StatusPipeline", $"- 流水线：{_pipelineService.CurrentStage switch { PipelineStage.NotStarted => "未开始", _ => DescribeStage(_pipelineService.CurrentStage) }}", _pipelineService.CurrentStage == PipelineStage.NotStarted ? T("CP.StageNone", "未开始") : DescribeStage(_pipelineService.CurrentStage)));
            if (_pipelineService.CurrentStage != PipelineStage.NotStarted)
            {
                lines.Add(stageText);
            }

            PostAssistant(string.Join('\n', lines));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "查询统计失败");
            PostAssistant(TF("CPS.QueryFailed", $"查询失败：{ex.Message}", ex.Message));
        }
    }

    /// <summary>
    /// 自由问答：RWKV 带项目上下文（精简提示词），失败明确报错。
    /// </summary>
    private async Task HandleChatAsync(string question)
    {
        // 「开始规划」后等待愿景：本条消息即为创作想法
        if (_awaitingVision)
        {
            _awaitingVision = false;
            await _pipelineService.StartPlanningAsync(_projectId!.Value, _projectName, question);
            return;
        }

        if (!_rwkvService.IsAvailable)
        {
            PostAssistant(T("CPS.OfflineFreeQa", "AI 推理服务当前离线，无法回答自由问题。导航、查询与流水线操作不受影响；可在「AI模型配置」页检查服务状态。"));
            return;
        }

        var prompt =
            "User: 你是书籍创作助手，负责帮助作者构思和分析作品，回答保持简洁。\n" +
            "当前作品：《" + _projectName + "》\n" +
            "作者问题：" + TruncateForPrompt(question, 800) + "\n" +
            "请直接给出干净、可执行的中文答复。\n\n" +
            "Assistant: <think></think\n";

        var response = await _rwkvService.CompleteAsync(prompt, 800);
        if (response.Success && !string.IsNullOrWhiteSpace(response.Text))
        {
            var cleaned = AIOutputSanitizer.ExtractCleanOutput(response.Text)?.Trim();
            PostAssistant(string.IsNullOrWhiteSpace(cleaned) ? T("CPS.EmptyAIResponse", "AI 返回了空内容，请换个问法。") : cleaned);
        }
        else
        {
            PostAssistant(TF("CPS.AIInferFailed", $"AI 推理失败：{response.Error ?? "未知错误"}", response.Error ?? T("CPS.UnknownError", "未知错误")));
        }
    }

    #endregion

    #region 章节关联处理（输入区关联书/卷/章）

    /// <summary>章节处理类操作关键词（命中即按处理要求执行，否则疑似针对章节的提问）。</summary>
    private static readonly string[] ChapterOpKeywords =
    {
        "润色", "重写", "改写", "扩写", "续写", "缩写", "精简", "优化",
        "补全", "删减", "修改", "处理", "翻译", "调整", "换一版", "改得", "写得更"
    };

    /// <summary>
    /// 关联目标章节（书/卷/章选择完成后由面板调用）：会话进入章节处理模式。
    /// </summary>
    public void AttachChapterRef(ChapterReferral referral)
    {
        _chapterRef = referral;
        var brief = string.IsNullOrWhiteSpace(referral.Summary)
            ? string.Empty
            : TF("CPS.RefBrief", "｜梗概：" + TruncateForPrompt(referral.Summary, 60), TruncateForPrompt(referral.Summary, 60));
        PostSystem(TF("CPS.AttachedRef", $"已关联《{referral.ProjectName}》第{referral.VolumeOrder}卷第{referral.ChapterOrder}章《{referral.ChapterTitle}》{brief}。\n" +
            "直接输入要求即可处理本章（如：润色全文 / 重写开头 / 扩写战斗场面 / 续写结尾），处理结果将生成确认卡，采纳后直接更新本章正文；" +
            "也可以针对本章提问（如：这章的节奏有什么问题）。输入「取消关联」可解除。", referral.ProjectName, referral.VolumeOrder, referral.ChapterOrder, referral.ChapterTitle, brief));
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 解除章节关联，恢复普通助手模式。
    /// </summary>
    public void ClearChapterRef()
    {
        if (_chapterRef == null)
        {
            return;
        }

        _chapterRef = null;
        PostSystem(T("CPS.ChapterRefCleared", "已取消章节关联，恢复普通助手模式。"));
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 章节关联模式下的输入分派：操作关键词或自由要求 → 处理章节；疑问句 → 解答章节问题。
    /// </summary>
    private async Task HandleChapterRefInputAsync(string input)
    {
        var referral = _chapterRef!;
        if (LooksLikeChapterQuestion(input))
        {
            await HandleChapterQuestionAsync(referral, input);
            return;
        }

        try
        {
            await _pipelineService.ProcessChapterOperationAsync(referral, input);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "关联章节《{Title}》处理失败", referral.ChapterTitle);
            PostAssistant(TF("CPS.ChapterProcessFailed", $"章节处理失败：{ex.Message}", ex.Message));
        }
    }

    /// <summary>
    /// 疑问句判定：不含处理类关键词且带问号/「吗」结尾，视为针对章节的提问（走问答而非改写）。
    /// </summary>
    private static bool LooksLikeChapterQuestion(string input)
    {
        if (ChapterOpKeywords.Any(k => input.Contains(k, StringComparison.Ordinal)))
        {
            return false;
        }

        return input.Contains('？') || input.Contains('?') || input.EndsWith("吗", StringComparison.Ordinal);
    }

    /// <summary>
    /// 章节问答：带章节全文（或梗概）上下文的 RWKV 答复，不产生确认卡。
    /// </summary>
    private async Task HandleChapterQuestionAsync(ChapterReferral referral, string question)
    {
        if (!_rwkvService.IsAvailable)
        {
            PostAssistant(T("CPS.OfflineChapterQa", "AI 推理服务当前离线，无法分析章节内容。处理章节与取消关联不受影响。"));
            return;
        }

        try
        {
            var chapter = await _chapterService.GetChapterByIdAsync(referral.ChapterId);
            var content = chapter?.Content;
            var prompt =
                "User: 你是书籍创作助手。以下是《" + referral.ProjectName + "》第" + referral.VolumeOrder + "卷第" +
                referral.ChapterOrder + "章《" + referral.ChapterTitle + "》，请据此简洁回答作者问题。\n" +
                (string.IsNullOrWhiteSpace(content)
                    ? "（本章暂无正文，梗概：" + TruncateForPrompt(referral.Summary, 400) + "）\n"
                    : "【章节内容】\n" + TruncateForPrompt(content, 1600) + "\n") +
                "作者问题：" + TruncateForPrompt(question, 400) + "\n" +
                "请直接给出干净、可执行的中文答复。\n\n" +
                "Assistant: <think></think\n";

            var response = await _rwkvService.CompleteAsync(prompt, 800);
            if (response.Success && !string.IsNullOrWhiteSpace(response.Text))
            {
                var cleaned = AIOutputSanitizer.ExtractCleanOutput(response.Text)?.Trim();
                PostAssistant(string.IsNullOrWhiteSpace(cleaned) ? T("CPS.EmptyAIResponse", "AI 返回了空内容，请换个问法。") : cleaned);
            }
            else
            {
                PostAssistant(TF("CPS.AIInferFailed", $"AI 推理失败：{response.Error ?? "未知错误"}", response.Error ?? T("CPS.UnknownError", "未知错误")));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "章节问答失败");
            PostAssistant(TF("CPS.ChapterQaFailed", $"章节问答失败：{ex.Message}", ex.Message));
        }
    }

    #endregion

    #region 确认卡管理

    /// <summary>
    /// 流水线确认卡就绪：旧 Pending 卡置 Expired，新卡入消息流。
    /// </summary>
    private void OnPipelineProposalReady(object? sender, PendingProposal proposal)
    {
        if (CurrentProposal != null && CurrentProposal.Status == ProposalStatus.Pending)
        {
            CurrentProposal.Status = ProposalStatus.Expired;
        }

        CurrentProposal = proposal;
        Messages.Add(new CopilotMessageItem { Role = CopilotMessageRole.Assistant, Text = string.Empty, Proposal = proposal });
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 流水线文本消息入流。
    /// </summary>
    private void OnPipelineMessageReady(object? sender, CopilotMessageItem message) =>
        Messages.Add(message);

    /// <summary>
    /// 采纳确认卡：仅落库 Accepted/Modified 项，随后推进流水线。
    /// </summary>
    public async Task AcceptProposalAsync(Guid proposalId)
    {
        var proposal = CurrentProposal;
        if (proposal == null || proposal.Id != proposalId || proposal.Status != ProposalStatus.Pending || _projectId == null)
        {
            return;
        }

        var acceptedItems = proposal.Items.Where(i => i.Status is ProposalStatus.Accepted or ProposalStatus.Modified).ToList();
        if (acceptedItems.Count == 0)
        {
            PostAssistant(T("CPS.NoItemsSelected", "请先勾选要采纳的项（点击各项「采纳」），或使用「全部采纳」。"));
            return;
        }

        try
        {
            var persistedCount = await PersistProposalAsync(proposal, acceptedItems);
            proposal.Status = ProposalStatus.Accepted;
            PostSystem(TF("CPS.Persisted", $"已落库 {persistedCount} 项到数据库。", persistedCount));

            if (proposal.TargetChapterId.HasValue && proposal.TargetChapterId.Value != Guid.Empty)
            {
                // 章节关联处理：已直接更新目标章节，不推进流水线
                var target = await _chapterService.GetChapterByIdAsync(proposal.TargetChapterId.Value);
                PostAssistant(TF("CPS.ChapterUpdated", $"《{target?.Title ?? "目标章节"}》正文已更新。可继续输入新的处理要求，或输入「取消关联」解除关联。", target?.Title ?? T("CPS.DefaultChapterTitle", "目标章节")));
            }
            else
            {
                await _pipelineService.AdvanceAsync(proposal.Kind, _projectId.Value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "采纳确认卡落库失败");
            PostAssistant(TF("CPS.PersistFailed", $"落库失败：{ex.Message}", ex.Message));
        }
        finally
        {
            SessionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// 放弃整卡：流水线按「整卡放弃」整改意见重生成当前级。
    /// </summary>
    public async Task RejectProposalAsync(Guid proposalId)
    {
        var proposal = CurrentProposal;
        if (proposal == null || proposal.Id != proposalId || proposal.Status != ProposalStatus.Pending || _projectId == null)
        {
            return;
        }

        proposal.Status = ProposalStatus.Rejected;
        PostSystem(T("CPS.CardRejectedRegenerating", "已放弃本卡全部内容，正在重新生成…"));
        await _pipelineService.RegenerateCurrentStageAsync("整卡放弃，请换思路重新生成", _projectId.Value);
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 采纳单项。
    /// </summary>
    public void AcceptItem(Guid proposalId, Guid itemId)
    {
        var item = FindItem(proposalId, itemId);
        if (item != null && item.IsPending)
        {
            item.Status = string.IsNullOrWhiteSpace(item.EditedBody) ? ProposalStatus.Accepted : ProposalStatus.Modified;
            SessionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// 放弃单项。
    /// </summary>
    public void RejectItem(Guid proposalId, Guid itemId)
    {
        var item = FindItem(proposalId, itemId);
        if (item != null && item.IsPending)
        {
            item.Status = ProposalStatus.Rejected;
            SessionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// 进入修改模式：返回该项原文（供输入框回填），后续一条用户消息即作为修改文本。
    /// </summary>
    public string? BeginItemEdit(Guid proposalId, Guid itemId)
    {
        var item = FindItem(proposalId, itemId);
        if (item == null || !item.IsPending)
        {
            return null;
        }

        _editTarget = (proposalId, itemId);
        SessionChanged?.Invoke(this, EventArgs.Empty);
        return item.DisplayBody;
    }

    /// <summary>
    /// 应用修改文本到编辑目标项。
    /// </summary>
    private void ApplyItemEdit(string text)
    {
        if (_editTarget == null)
        {
            return;
        }

        var item = FindItem(_editTarget.Value.ProposalId, _editTarget.Value.ItemId);
        _editTarget = null;
        if (item != null)
        {
            item.EditedBody = text;
            item.Status = ProposalStatus.Modified;
            PostSystem(TF("CPS.EditSaved", $"已更新「{item.Title}」的修改内容，点击该项「采纳」生效。", item.Title));
        }

        SessionChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 取消修改模式。
    /// </summary>
    public void CancelItemEdit()
    {
        if (_editTarget != null)
        {
            _editTarget = null;
            SessionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private ProposalItem? FindItem(Guid proposalId, Guid itemId)
    {
        if (CurrentProposal?.Id != proposalId)
        {
            return null;
        }

        return CurrentProposal.Items.FirstOrDefault(i => i.Id == itemId);
    }

    #endregion

    #region 采纳落库（复用现有服务）

    /// <summary>
    /// 按卡片类型分派落库，返回落库数量。
    /// </summary>
    private async Task<int> PersistProposalAsync(PendingProposal proposal, List<ProposalItem> items)
    {
        var projectId = _projectId!.Value;
        return proposal.Kind switch
        {
            ProposalKind.Outline => await PersistOutlineAsync(projectId, items[0]),
            ProposalKind.PlotLine => await PersistPlotLinesAsync(projectId, items),
            ProposalKind.Volume => await PersistVolumesAsync(projectId, items),
            ProposalKind.ChapterDraft => await PersistChapterDraftsAsync(projectId, items),
            ProposalKind.ChapterContent => await PersistChapterContentAsync(projectId, items[0], proposal.TargetChapterId),
            _ => 0
        };
    }

    /// <summary>
    /// 总纲落库：特殊 Plot 记录（Type="总纲"，Importance=10 自动进 AI 上下文）。
    /// </summary>
    private async Task<int> PersistOutlineAsync(Guid projectId, ProposalItem item)
    {
        var plots = await _plotService.GetPlotsByTypeAsync(projectId, "总纲");
        var existing = plots.FirstOrDefault(p => p.Title == "全书总纲" && !p.IsDeleted);
        if (existing != null)
        {
            existing.Outline = item.EffectiveBody;
            await _plotService.UpdatePlotAsync(existing);
            return 1;
        }

        await _plotService.CreatePlotAsync(new Plot
        {
            ProjectId = projectId,
            Title = "全书总纲",
            Type = "总纲",
            Status = "规划中",
            Priority = "高",
            Outline = item.EffectiveBody,
            Description = item.Fields.GetValueOrDefault("定位") ?? string.Empty,
            ConflictElements = item.Fields.GetValueOrDefault("核心冲突") ?? string.Empty,
            Importance = 10
        });
        return 1;
    }

    /// <summary>
    /// 剧情线落库（Type=主线/支线/暗线，Importance 按类型分档）。
    /// </summary>
    private async Task<int> PersistPlotLinesAsync(Guid projectId, List<ProposalItem> items)
    {
        var count = 0;
        foreach (var item in items)
        {
            var type = item.Fields.GetValueOrDefault("类型") ?? "主线";
            var importance = type switch
            {
                "主线" => 9,
                "支线" => 6,
                "暗线" => 5,
                _ => 5
            };

            await _plotService.CreatePlotAsync(new Plot
            {
                ProjectId = projectId,
                Title = item.Title,
                Type = type,
                Status = "规划中",
                Priority = type == "主线" ? "高" : "中",
                Description = item.EffectiveBody,
                ConflictElements = item.Fields.GetValueOrDefault("冲突") ?? string.Empty,
                Importance = importance
            });
            count++;
        }

        return count;
    }

    /// <summary>
    /// 分卷落库（Order 接续现有最大卷序）。
    /// </summary>
    private async Task<int> PersistVolumesAsync(Guid projectId, List<ProposalItem> items)
    {
        var volumes = await _volumeService.GetVolumeListAsync(projectId);
        var nextOrder = volumes.Any() ? volumes.Max(v => v.Order) + 1 : 1;

        var count = 0;
        foreach (var item in items)
        {
            await _volumeService.CreateVolumeAsync(new Volume
            {
                ProjectId = projectId,
                Title = item.Title,
                Description = item.EffectiveBody,
                Order = nextOrder,
                Status = "Planning"
            });
            nextOrder++;
            count++;
        }

        return count;
    }

    /// <summary>
    /// 章节草稿落库（Summary=采纳文本且 ≤1000 字硬校验，挂当前最新卷）。
    /// </summary>
    private async Task<int> PersistChapterDraftsAsync(Guid projectId, List<ProposalItem> items)
    {
        var volumes = await _volumeService.GetVolumeListAsync(projectId);
        var volume = volumes.OrderByDescending(v => v.Order).FirstOrDefault();
        if (volume == null)
        {
            throw new InvalidOperationException(T("CPS.ErrNoVolumesForVolumes", "项目还没有卷宗，请先采纳分卷确认卡。"));
        }

        var chapters = await _chapterService.GetChapterListAsync(volume.Id);
        var nextOrder = chapters.Any() ? chapters.Max(c => c.Order) + 1 : 1;

        var count = 0;
        foreach (var item in items)
        {
            var summary = item.EffectiveBody;
            if (summary.Length > 1000)
            {
                summary = summary[..1000];
                PostSystem(TF("CPS.SummaryTruncated", $"「{item.Title}」梗概超过 1000 字，已截断保存。", item.Title));
            }

            await _chapterService.CreateChapterAsync(new Chapter
            {
                VolumeId = volume.Id,
                Title = item.Title,
                Summary = summary,
                Order = nextOrder,
                Status = "Draft",
                Type = "正文"
            });
            nextOrder++;
            count++;
        }

        return count;
    }

    /// <summary>
    /// 章节正文落库：章节关联处理卡直接更新目标章节；流水线常规卡挂当前卷中首个无正文章节。
    /// </summary>
    private async Task<int> PersistChapterContentAsync(Guid projectId, ProposalItem item, Guid? targetChapterId)
    {
        // 章节关联处理：按卡片携带的目标章节 ID 直接更新（支持跨项目，不走「首个空白章」查找）
        if (targetChapterId.HasValue && targetChapterId.Value != Guid.Empty)
        {
            var target = await _chapterService.GetChapterByIdAsync(targetChapterId.Value)
                ?? throw new InvalidOperationException(T("CPS.ErrChapterMissing", "目标章节不存在或已被删除，请重新关联。"));
            await _chapterService.UpdateChapterContentAsync(target.Id, item.EffectiveBody);
            return 1;
        }

        var volumes = await _volumeService.GetVolumeListAsync(projectId);
        var volume = volumes.OrderByDescending(v => v.Order).FirstOrDefault();
        if (volume == null)
        {
            throw new InvalidOperationException(T("CPS.ErrNoVolumes", "项目还没有卷宗。"));
        }

        var chapters = await _chapterService.GetChapterListAsync(volume.Id);
        var chapter = chapters.OrderBy(c => c.Order).FirstOrDefault(c => string.IsNullOrWhiteSpace(c.Content));
        if (chapter == null)
        {
            throw new InvalidOperationException(T("CPS.ErrNoPendingChapters", "当前卷没有待写章节。"));
        }

        await _chapterService.UpdateChapterContentAsync(chapter.Id, item.EffectiveBody);
        chapter.Status = "Completed";
        await _chapterService.UpdateChapterAsync(chapter);
        return 1;
    }

    #endregion

    #region 展示辅助

    private void PostSystem(string text) =>
        Messages.Add(new CopilotMessageItem { Role = CopilotMessageRole.System, Text = text });

    private void PostAssistant(string text) =>
        Messages.Add(new CopilotMessageItem { Role = CopilotMessageRole.Assistant, Text = text });

    private static string? TruncateForPrompt(string? text, int maxChars) =>
        string.IsNullOrEmpty(text) ? null : text.Length <= maxChars ? text : text[..maxChars] + "……";

    /// <summary>
    /// 构建流水线状态描述文本。
    /// </summary>
    private string BuildPipelineStatusText()
    {
        var stage = _pipelineService.CurrentStage;
        if (stage == PipelineStage.NotStarted)
        {
            return T("CPS.StatusNotStarted", "创作流水线尚未开始。回复「开始规划」并描述你的想法即可启动。");
        }

        var snapshot = _pipelineService.GetStateSnapshot();
        var stageName = DescribeStage(stage);
        return snapshot.Finished
            ? TF("CPS.StatusCompleted", $"流水线已完成 {_pipelineService.GetStateSnapshot().ConfirmedVolumeCount} 卷创作。回复「继续创作」可追加下一卷。", _pipelineService.GetStateSnapshot().ConfirmedVolumeCount)
            : TF("CPS.StatusInProgress", $"流水线进行中：{stageName}（第{snapshot.CurrentVolumeOrder}卷 第{snapshot.CurrentChapterOrder}章）。等待你对当前确认卡做出裁决。", stageName, snapshot.CurrentVolumeOrder, snapshot.CurrentChapterOrder);
    }

    /// <summary>
    /// 阶段名描述。
    /// </summary>
    internal static string DescribeStage(PipelineStage stage) => stage switch
    {
        PipelineStage.BlueprintPending => T("CP.StageBlueprint", "总纲规划"),
        PipelineStage.PlotLinesPending => T("CP.StagePlotLines", "剧情线规划"),
        PipelineStage.VolumesPending => T("CP.StageVolumes", "分卷规划"),
        PipelineStage.ChapterDraftsPending => T("CP.StageChapterDrafts", "章节剧情草稿"),
        PipelineStage.ChapterContentInProgress => T("CP.StageChapterContent", "章节正文创作"),
        PipelineStage.Completed => T("CP.StageCompleted", "已完成"),
        _ => T("CP.StageNone", "未开始")
    };

    /// <summary>
    /// 导航目标中文名。
    /// </summary>
    internal static string DescribeTarget(NovelManagement.WPF.Services.NavigationTarget target) => target switch
    {
        NovelManagement.WPF.Services.NavigationTarget.ProjectManagement => T("Nav.ProjectManagement", "项目管理"),
        NovelManagement.WPF.Services.NavigationTarget.ProjectOverview => T("Nav.ProjectOverview", "项目概览"),
        NovelManagement.WPF.Services.NavigationTarget.VolumeManagement => T("CPS.DestVolumeManagement", "卷章管理"),
        NovelManagement.WPF.Services.NavigationTarget.CharacterManagement => T("Nav.CharacterManagement", "角色管理"),
        NovelManagement.WPF.Services.NavigationTarget.Timeline => T("CPS.DestTimeline", "时间线"),
        NovelManagement.WPF.Services.NavigationTarget.RelationshipNetwork => T("Nav.RelationshipNetwork", "关系网络"),
        NovelManagement.WPF.Services.NavigationTarget.FactionManagement => T("Nav.FactionManagement", "势力管理"),
        NovelManagement.WPF.Services.NavigationTarget.PlotManagement => T("Nav.PlotManagement", "剧情管理"),
        NovelManagement.WPF.Services.NavigationTarget.AICollaboration => T("Nav.AICollaboration", "AI协作创作"),
        NovelManagement.WPF.Services.NavigationTarget.AIConfiguration => T("Nav.AIConfiguration", "AI模型配置"),
        NovelManagement.WPF.Services.NavigationTarget.ImportExport => T("Nav.ImportExport", "导入导出"),
        NovelManagement.WPF.Services.NavigationTarget.WorldSettingManagement => T("Nav.WorldSettingManagement", "世界设定"),
        NovelManagement.WPF.Services.NavigationTarget.DialogGeneration => T("CPS.DestDialogGeneration", "对话生成器"),
        NovelManagement.WPF.Services.NavigationTarget.ProjectHealthCheck => T("Nav.ProjectHealthCheck", "项目体检报告"),
        _ => target.ToString()
    };

    #endregion
}
