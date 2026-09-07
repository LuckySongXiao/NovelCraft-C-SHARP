using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NovelManagement.AI.Services.RWKV;
using NovelManagement.AI.Utilities;
using NovelManagement.Application.Services;
using NovelManagement.Core.Entities;

namespace NovelManagement.WPF.Services.Copilot;

/// <summary>
/// 五级创作流水线编排服务：全书总纲 → 剧情线 → 分卷 → 章节剧情草稿 → 章节正文。
/// 每级三段式：RWKV 生成 → 原始文本解析 → 确认卡（经 ProposalReady 事件交会话服务展示）；
/// 用户采纳落库后经 AdvanceAsync 推进下一级；整改意见经 RegenerateCurrentStageAsync 重生成当前级。
/// 状态持久化复用批量生成先例：Plot(Type="系统", Title="创作流水线进度", Outline=JSON)。
/// 铁律：RWKV 调用失败或解析失败时明确报错，绝不降级假数据。
/// </summary>
public class CreationPipelineService
{
    // ====== 常量（对齐 FullNovelBatchGenerationService 工艺参数） ======
    private const int OutlineMaxTokens = 2400;
    private const int SliceBudget = 1100;
    private const int ChapterMinChars = 2000;
    private const int MaxSlicesPerChapter = 4;
    private const string StatePlotTitle = "创作流水线进度";
    private const string StatePlotType = "系统";
    private const string BlueprintPlotType = "总纲";
    private const string BlueprintPlotTitle = "全书总纲";

    private readonly ILogger<CreationPipelineService> _logger;
    private readonly IRwkvLightningService _rwkvService;
    private readonly PlotService _plotService;
    private readonly VolumeService _volumeService;
    private readonly ChapterService _chapterService;

    private readonly object _lock = new();
    private PipelineState _state = new();
    private bool _isGenerating;

    /// <summary>确认卡就绪事件（会话服务订阅后入消息流）。</summary>
    public event EventHandler<PendingProposal>? ProposalReady;

    /// <summary>文本消息就绪事件（汇报/提示）。</summary>
    public event EventHandler<CopilotMessageItem>? MessageReady;

    /// <summary>流水线进度变化事件（UI 刷新阶段徽标）。</summary>
    public event EventHandler? ProgressChanged;

    /// <summary>
    /// 构造函数（依赖注入对齐 FullNovelBatchGenerationService 的 singleton 直注模式）。
    /// </summary>
    public CreationPipelineService(
        ILogger<CreationPipelineService> logger,
        IRwkvLightningService rwkvService,
        PlotService plotService,
        VolumeService volumeService,
        ChapterService chapterService)
    {
        _logger = logger;
        _rwkvService = rwkvService;
        _plotService = plotService;
        _volumeService = volumeService;
        _chapterService = chapterService;
    }

    #region 公共状态

    /// <summary>当前流水线阶段。</summary>
    public PipelineStage CurrentStage
    {
        get { lock (_lock) { return _state.Stage; } }
    }

    /// <summary>流水线所属项目 ID（未运行为 null）。</summary>
    public Guid? CurrentProjectId
    {
        get { lock (_lock) { return _state.ProjectId == Guid.Empty ? null : _state.ProjectId; } }
    }

    /// <summary>是否正在生成中。</summary>
    public bool IsGenerating
    {
        get { lock (_lock) { return _isGenerating; } }
    }

    /// <summary>当前状态快照（UI 只读展示）。</summary>
    public PipelineState GetStateSnapshot()
    {
        lock (_lock)
        {
            return _state;
        }
    }

    #endregion

    #region 状态机入口

    /// <summary>
    /// 开始规划：记录用户想法 → 生成 L1 全书总纲 → 发确认卡。
    /// </summary>
    public Task StartPlanningAsync(Guid projectId, string projectName, string userVision)
    {
        lock (_lock)
        {
            if (_isGenerating)
            {
                return Task.CompletedTask;
            }

            _state = new PipelineState
            {
                ProjectId = projectId,
                ProjectName = string.IsNullOrWhiteSpace(projectName) ? "未命名作品" : projectName.Trim(),
                UserVision = userVision ?? string.Empty,
                Stage = PipelineStage.BlueprintPending
            };
        }

        PostMessage($"收到！我将根据你的想法为《{(string.IsNullOrWhiteSpace(projectName) ? "未命名作品" : projectName)}》规划全书。先从总纲开始。");
        return RunStageAsync();
    }

    /// <summary>
    /// 重启续跑：从系统 Plot 恢复状态并重新生成当前级的确认卡（重启后 Pending 卡不保留）。
    /// </summary>
    /// <returns>是否成功恢复（无进行中状态返回 false）。</returns>
    public async Task<bool> TryResumeAsync(Guid projectId)
    {
        PipelineState? loaded;
        lock (_lock)
        {
            if (_state.ProjectId == projectId && _state.Stage != PipelineStage.NotStarted && !_state.Finished)
            {
                return true; // 当前会话已在进行中
            }
        }

        loaded = await LoadStateAsync(projectId);
        if (loaded == null || loaded.Finished || loaded.Stage == PipelineStage.NotStarted)
        {
            return false;
        }

        lock (_lock)
        {
            _state = loaded;
        }

        PostMessage($"已恢复《{loaded.ProjectName}》的创作流水线（当前阶段：{DescribeStage(loaded.Stage)}），正在重建确认卡…");
        await RunStageAsync();
        return true;
    }

    /// <summary>
    /// 确认卡采纳落库后推进：加载库中最新数据 → 生成下一级确认卡。
    /// </summary>
    public Task AdvanceAsync(ProposalKind acceptedKind, Guid projectId)
    {
        lock (_lock)
        {
            if (_isGenerating || _state.ProjectId != projectId)
            {
                return Task.CompletedTask;
            }
        }

        return acceptedKind switch
        {
            ProposalKind.Outline => AdvanceToStageAsync(PipelineStage.PlotLinesPending),
            ProposalKind.PlotLine => AdvanceToStageAsync(PipelineStage.VolumesPending),
            ProposalKind.Volume => AdvanceToVolumeDraftsAsync(projectId),
            ProposalKind.ChapterDraft => AdvanceToStageAsync(PipelineStage.ChapterContentInProgress),
            ProposalKind.ChapterContent => AdvanceChapterPointerAsync(projectId),
            _ => Task.CompletedTask
        };
    }

    /// <summary>
    /// 用户整改：记录意见并重生成当前级确认卡。
    /// </summary>
    public Task RegenerateCurrentStageAsync(string feedback, Guid projectId)
    {
        lock (_lock)
        {
            if (_isGenerating || _state.ProjectId != projectId)
            {
                return Task.CompletedTask;
            }

            _state.LastFeedback = string.IsNullOrWhiteSpace(feedback) ? null : feedback.Trim();
        }

        PostMessage("收到整改意见，正在按你的要求重新生成当前阶段…");
        return RunStageAsync();
    }

    /// <summary>
    /// 追加后续分卷（已完成全部已确认卷后，用户回复「继续创作」触发）：
    /// 重新进入分卷规划阶段，新生成的卷将接续现有卷序。
    /// </summary>
    public Task RequestAppendVolumeAsync(Guid projectId)
    {
        lock (_lock)
        {
            if (_isGenerating || _state.ProjectId != projectId)
            {
                return Task.CompletedTask;
            }

            _state.Finished = false;
            _state.Stage = PipelineStage.VolumesPending;
        }

        PostMessage("好的，我们继续规划后续分卷。基于总纲与已有剧情，我会给出接下来几卷的安排。");
        return RunStageAsync();
    }

    #endregion

    #region 阶段推进

    private async Task AdvanceToStageAsync(PipelineStage stage)
    {
        lock (_lock)
        {
            _state.Stage = stage;
        }

        await SaveStateAsync();
        RaiseProgressChanged();
        await RunStageAsync();
    }

    private async Task AdvanceToVolumeDraftsAsync(Guid projectId)
    {
        // 采纳分卷后：定位最新一卷，进入其章节草稿阶段
        var volumes = await _volumeService.GetVolumeListAsync(projectId);
        var maxOrder = volumes.Any() ? volumes.Max(v => v.Order) : 1;
        lock (_lock)
        {
            _state.Stage = PipelineStage.ChapterDraftsPending;
            _state.CurrentVolumeOrder = maxOrder;
            _state.CurrentChapterOrder = 1;
            _state.ConfirmedVolumeCount = Math.Max(_state.ConfirmedVolumeCount, maxOrder);
        }

        await SaveStateAsync();
        RaiseProgressChanged();
        await RunStageAsync();
    }

    private async Task AdvanceChapterPointerAsync(Guid projectId)
    {
        // 一章正文采纳后：推进章指针；卷内写完则进入下一卷草稿；全部写完则汇报收尾
        var chapters = await GetCurrentVolumeChaptersAsync(projectId);
        var nextChapter = chapters.FirstOrDefault(c => c.Order >= _state.CurrentChapterOrder && string.IsNullOrWhiteSpace(c.Content));

        if (nextChapter != null)
        {
            lock (_lock)
            {
                _state.CurrentChapterOrder = nextChapter.Order;
            }

            await SaveStateAsync();
            await RunStageAsync();
            return;
        }

        // 当前卷无待写章节 → 下一卷
        var volumes = await _volumeService.GetVolumeListAsync(projectId);
        var nextVolume = volumes.FirstOrDefault(v => v.Order > _state.CurrentVolumeOrder);
        if (nextVolume != null)
        {
            lock (_lock)
            {
                _state.CurrentVolumeOrder = nextVolume.Order;
                _state.CurrentChapterOrder = 1;
                _state.Stage = PipelineStage.ChapterDraftsPending;
            }

            await SaveStateAsync();
            RaiseProgressChanged();
            await RunStageAsync();
            return;
        }

        // 全部已确认卷完成 → 汇报并询问是否追加
        lock (_lock)
        {
            _state.Finished = true;
            _state.Stage = PipelineStage.Completed;
        }

        await SaveStateAsync();
        RaiseProgressChanged();
        PostStageReport("全部已规划卷章创作完成", new[]
        {
            $"已完成 {_state.ConfirmedVolumeCount} 卷创作",
            "如需追加后续大纲，请回复「继续创作」，我会规划下一卷；也可以随时告诉我想调整的地方。"
        });
    }

    #endregion

    #region 各级生成（三段式：生成 → 解析 → 确认卡）

    /// <summary>
    /// 执行当前阶段的生成。
    /// </summary>
    private async Task RunStageAsync()
    {
        lock (_lock)
        {
            if (_isGenerating)
            {
                return;
            }

            _isGenerating = true;
        }

        RaiseProgressChanged();
        try
        {
            var stage = _state.Stage;
            switch (stage)
            {
                case PipelineStage.BlueprintPending:
                    await GenerateBlueprintAsync();
                    break;
                case PipelineStage.PlotLinesPending:
                    await GeneratePlotLinesAsync();
                    break;
                case PipelineStage.VolumesPending:
                    await GenerateVolumesAsync();
                    break;
                case PipelineStage.ChapterDraftsPending:
                    await GenerateChapterDraftsAsync();
                    break;
                case PipelineStage.ChapterContentInProgress:
                    await GenerateChapterContentAsync();
                    break;
                default:
                    return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "流水线阶段生成失败：{Stage}", _state.Stage);
            PostMessage($"生成失败：{ex.Message}。请回复「重新生成」重试，或告诉我新的想法。");
        }
        finally
        {
            lock (_lock)
            {
                _isGenerating = false;
                // 整改意见只作用于当前级的一次生成，完成后清空
                _state.LastFeedback = null;
            }

            RaiseProgressChanged();
        }
    }

    /// <summary>
    /// L1 全书总纲。
    /// </summary>
    private async Task GenerateBlueprintAsync()
    {
        var feedback = FormatFeedback();
        var prompt =
            "User: 你是顶级中文网文架构师。请为书籍《" + _state.ProjectName + "》设计全书总纲。\n" +
            "作者想法：" + Truncate(_state.UserVision, 500) + "\n" +
            (feedback ?? string.Empty) +
            "输出格式（逐行输出，不要解释）：\n" +
            "书名：《xxx》\n" +
            "定位：类型与卖点一句话\n" +
            "核心冲突：主线冲突一句话\n" +
            "阶段脉络：\n" +
            "1. 第一卷：一句话（3-5行，每行一卷）\n\n" +
            "Assistant: <think></think\n";

        var raw = await GenerateTextAsync(prompt, OutlineMaxTokens);
        var title = ExtractLineValue(raw, "书名") ?? _state.ProjectName;
        var position = ExtractLineValue(raw, "定位") ?? string.Empty;
        var conflict = ExtractLineValue(raw, "核心冲突") ?? string.Empty;

        var proposal = new PendingProposal
        {
            Kind = ProposalKind.Outline,
            Title = $"全书总纲（{title}）",
            Summary = "以下是为你规划的全书总纲，可采纳、修改或放弃后让我重新生成。"
        };
        proposal.Items.Add(new ProposalItem
        {
            Title = title,
            Body = raw.Trim(),
            Fields =
            {
                ["定位"] = position,
                ["核心冲突"] = conflict
            }
        });

        PostStageReport("总纲规划完成", new[] { position, conflict });
        PostProposal(proposal);
        await SaveStateAsync();
    }

    /// <summary>
    /// L2 剧情线（3 条，映射 Plot）。
    /// </summary>
    private async Task GeneratePlotLinesAsync()
    {
        var blueprint = await LoadBlueprintAsync(_state.ProjectId);
        if (string.IsNullOrWhiteSpace(blueprint))
        {
            PostMessage("未找到已采纳的总纲，无法规划剧情线。请先采纳总纲确认卡。");
            return;
        }

        var feedback = FormatFeedback();
        var prompt =
            "User: 基于总纲为《" + _state.ProjectName + "》设计3条剧情线。\n" +
            "【总纲】\n" + Truncate(blueprint, 800) + "\n" +
            (feedback ?? string.Empty) +
            "输出3行，每行格式严格为：\n" +
            "1. 标题｜类型：主线或支线或暗线｜描述：100字内｜冲突：50字内\n" +
            "不要解释。\n\n" +
            "Assistant: <think></think\n";

        var raw = await GenerateTextAsync(prompt, OutlineMaxTokens);
        var lines = ParseNumberedLines(raw, 1, 6);
        if (lines.Count == 0)
        {
            throw new InvalidOperationException("剧情线输出解析失败（未找到编号行），请回复「重新生成」重试。");
        }

        var proposal = new PendingProposal
        {
            Kind = ProposalKind.PlotLine,
            Title = "剧情线规划",
            Summary = $"共 {lines.Count} 条剧情线，采纳后将写入剧情管理。"
        };
        foreach (var (content, _) in lines)
        {
            var parts = SplitSegments(content);
            var title = parts.Count > 0 ? parts[0] : "未命名剧情线";
            var type = GetSegmentValue(parts, "类型") ?? GuessPlotType(title);
            var desc = GetSegmentValue(parts, "描述") ?? (parts.Count > 1 ? parts[1] : content);
            var conflict = GetSegmentValue(parts, "冲突") ?? string.Empty;

            proposal.Items.Add(new ProposalItem
            {
                Title = title,
                Body = desc,
                Fields =
                {
                    ["类型"] = type,
                    ["冲突"] = conflict
                }
            });
        }

        PostStageReport("剧情线规划完成", lines.Select(l => l.Content).Take(3).Select(c => SplitSegments(c).FirstOrDefault() ?? c));
        PostProposal(proposal);
    }

    /// <summary>
    /// L3 分卷（3-5 卷）。
    /// </summary>
    private async Task GenerateVolumesAsync()
    {
        var blueprint = await LoadBlueprintAsync(_state.ProjectId);
        if (string.IsNullOrWhiteSpace(blueprint))
        {
            PostMessage("未找到已采纳的总纲，无法规划分卷。请先采纳总纲确认卡。");
            return;
        }

        var plotLines = await _plotService.GetPlotsByProjectIdAsync(_state.ProjectId);
        var lineTitles = string.Join("；", plotLines
            .Where(p => p.Type != StatePlotType && p.Type != BlueprintPlotType)
            .Select(p => p.Title)
            .Take(6));

        // 已有卷标题：追加规划时让模型接续而非重复
        var volumes = await _volumeService.GetVolumeListAsync(_state.ProjectId);
        var volumeTitles = string.Join("；", volumes.OrderBy(v => v.Order).Select(v => $"第{v.Order}卷{v.Title}").Take(8));

        var feedback = FormatFeedback();
        var prompt =
            "User: 基于总纲为《" + _state.ProjectName + "》规划分卷。\n" +
            "【总纲】\n" + Truncate(blueprint, 800) + "\n" +
            (string.IsNullOrWhiteSpace(lineTitles) ? string.Empty : "【剧情线】" + lineTitles + "\n") +
            (string.IsNullOrWhiteSpace(volumeTitles) ? string.Empty : "【已有分卷（新卷必须接续其后，不要重复）】" + volumeTitles + "\n") +
            (feedback ?? string.Empty) +
            "输出3-5行，每行格式严格为：\n" +
            "1. 卷名｜卷目标：一句话｜收束事件：50字内\n" +
            "不要解释。\n\n" +
            "Assistant: <think></think\n";

        var raw = await GenerateTextAsync(prompt, OutlineMaxTokens);
        var lines = ParseNumberedLines(raw, 1, 8);
        if (lines.Count == 0)
        {
            throw new InvalidOperationException("分卷输出解析失败（未找到编号行），请回复「重新生成」重试。");
        }

        var proposal = new PendingProposal
        {
            Kind = ProposalKind.Volume,
            Title = "分卷规划",
            Summary = $"共 {lines.Count} 卷，采纳后将创建卷宗。"
        };
        foreach (var (content, _) in lines)
        {
            var parts = SplitSegments(content);
            var title = parts.Count > 0 ? parts[0] : "未命名卷";
            var goal = GetSegmentValue(parts, "卷目标") ?? (parts.Count > 1 ? parts[1] : content);
            var ending = GetSegmentValue(parts, "收束事件") ?? string.Empty;

            proposal.Items.Add(new ProposalItem
            {
                Title = title,
                Body = string.IsNullOrWhiteSpace(ending) ? goal : goal + "\n收束事件：" + ending,
                Fields =
                {
                    ["卷目标"] = goal,
                    ["收束事件"] = ending
                }
            });
        }

        PostStageReport("分卷规划完成", lines.Select(l => l.Content).Select(c => SplitSegments(c).FirstOrDefault() ?? c));
        PostProposal(proposal);
    }

    /// <summary>
    /// L4 章节剧情草稿（按当前卷，映射 Chapter.Summary）。
    /// </summary>
    private async Task GenerateChapterDraftsAsync()
    {
        var blueprint = await LoadBlueprintAsync(_state.ProjectId);
        var volume = await ResolveCurrentVolumeAsync(_state.ProjectId);
        if (volume == null)
        {
            PostMessage("未找到当前卷宗，请先采纳分卷确认卡。");
            return;
        }

        var feedback = FormatFeedback();
        var prompt =
            "User: 为《" + _state.ProjectName + "》第" + volume.Order + "卷「" + volume.Title + "」规划章节梗概。\n" +
            "【卷目标】" + Truncate(volume.Description, 300) + "\n" +
            "【总纲摘要】" + Truncate(blueprint, 400) + "\n" +
            (feedback ?? string.Empty) +
            "输出12-15行，每行格式严格为：\n" +
            "1. 章节标题｜梗概(80字内)\n" +
            "不要解释。\n\n" +
            "Assistant: <think></think\n";

        var raw = await GenerateTextAsync(prompt, OutlineMaxTokens);
        var lines = ParseNumberedLines(raw, 1, 20);
        if (lines.Count == 0)
        {
            throw new InvalidOperationException("章节梗概输出解析失败（未找到编号行），请回复「重新生成」重试。");
        }

        var proposal = new PendingProposal
        {
            Kind = ProposalKind.ChapterDraft,
            Title = $"第{volume.Order}卷章节剧情草稿",
            Summary = $"共 {lines.Count} 章梗概，采纳后将创建章节（状态：草稿）。"
        };
        foreach (var (content, index) in lines)
        {
            var parts = SplitSegments(content);
            var title = parts.Count > 0 ? parts[0] : $"第{index}章";
            var brief = parts.Count > 1 ? string.Join("｜", parts.Skip(1)) : content;

            proposal.Items.Add(new ProposalItem
            {
                Title = title,
                Body = brief,
                Fields =
                {
                    ["卷序"] = volume.Order.ToString(CultureInfo.InvariantCulture)
                }
            });
        }

        PostStageReport($"第{volume.Order}卷章节剧情草稿完成", new[] { $"覆盖 {lines.Count} 章梗概" });
        PostProposal(proposal);
    }

    /// <summary>
    /// L5 章节正文（切片创作 + 拼接，复用批量生成工艺：state 会话 + 首片注入上下文 + 防复读 + 滚动会话）。
    /// </summary>
    private async Task GenerateChapterContentAsync()
    {
        var chapter = await ResolveCurrentChapterAsync(_state.ProjectId);
        if (chapter == null)
        {
            PostMessage("未找到待写章节，请先采纳章节草稿确认卡。");
            return;
        }

        var volume = await ResolveCurrentVolumeAsync(_state.ProjectId);
        if (volume == null)
        {
            PostMessage("未找到当前卷宗，请先采纳分卷确认卡。");
            return;
        }

        var blueprint = await LoadBlueprintAsync(_state.ProjectId);
        var prevTail = await GetPreviousChapterTailAsync(volume.Id, chapter.Order);

        PostMessage($"正在为第{volume.Order}卷第{chapter.Order}章《{chapter.Title}》创作正文（切片拼接，目标 {ChapterMinChars} 字以上）…");

        var content = await GenerateChapterContentCoreAsync(
            volume.Order, chapter.Order, chapter.Title,
            chapter.Summary ?? string.Empty,
            blueprint ?? string.Empty, prevTail);

        var proposal = new PendingProposal
        {
            Kind = ProposalKind.ChapterContent,
            Title = $"第{chapter.Order}章正文（{content.Length}字）",
            Summary = $"《{chapter.Title}》正文已完成，共 {content.Length} 字。可采纳落库、逐项修改后采纳，或放弃整卡重写。"
        };
        proposal.Items.Add(new ProposalItem
        {
            Title = chapter.Title,
            Body = content,
            Fields =
            {
                ["卷序"] = volume.Order.ToString(CultureInfo.InvariantCulture),
                ["章序"] = chapter.Order.ToString(CultureInfo.InvariantCulture)
            }
        });

        PostStageReport($"第{chapter.Order}章正文完成", new[] { $"《{chapter.Title}》共 {content.Length} 字" });
        PostProposal(proposal);
    }

    /// <summary>
    /// 单章正文切片生成核心（对齐 FullNovelBatchGenerationService.GenerateOneChapterAsync 工艺）。
    /// </summary>
    private async Task<string> GenerateChapterContentCoreAsync(
        int volumeOrder, int chapterOrder, string chapterTitle, string brief,
        string blueprint, string prevTail)
    {
        var firstPrompt = BuildChapterFirstSlicePrompt(volumeOrder, chapterOrder, chapterTitle, brief, blueprint, prevTail);
        const string continuationPrompt =
            "\n\nUser: （继续输出本章正文后续内容：直接从上文停笔处续写，不要重复已有文字，不要总结，不要小标题，保持叙事连贯，约1000字。）\n\nAssistant: <think></think\n";
        const string antiRepeatPrompt =
            "\n\nUser: （注意：上一次输出与已写内容重复了。请从上文停笔处继续，推进全新的情节：新的冲突、对话或场景转换，严禁重复任何已写文字，不要总结收尾，约1000字。）\n\nAssistant: <think></think\n";

        List<string> parts = new();
        var fullContent = string.Empty;

        // 字数不达标时整章重试（最多2次），取达标结果
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            // 每次尝试使用一次性 state 会话（服务端会话跨运行持久，确定性 ID 会带入旧内容导致污染）
            var session = $"copilot-{_state.ProjectId:N}-v{volumeOrder}-c{chapterOrder}-{Guid.NewGuid():N}";
            var content = await GenerateSliceAsync(session, firstPrompt);
            parts = new List<string> { content };

            var slices = 1;
            var duplicateRetries = 0;
            var sessionRollovers = 0;
            while (TotalChars(parts) < ChapterMinChars && slices < MaxSlicesPerChapter && sessionRollovers < 4)
            {
                var slice = await GenerateSliceAsync(session, duplicateRetries > 0 ? antiRepeatPrompt : continuationPrompt);

                // 防复读：与上一片重复时先换提示词重试一次；仍复读则滚动新会话携带已写尾部继续
                if (IsDuplicateSlice(parts[^1], slice))
                {
                    if (duplicateRetries == 0)
                    {
                        duplicateRetries++;
                        _logger.LogWarning("创作助手第 {Volume} 卷第 {Chapter} 章检测到切片复读，换反复读提示词重试", volumeOrder, chapterOrder);
                        continue;
                    }

                    duplicateRetries = 0;
                    sessionRollovers++;
                    var rolledSession = $"{session}-r{sessionRollovers}";
                    var rolled = await GenerateSliceAsync(rolledSession, BuildRollOverPrompt(chapterOrder, chapterTitle, brief, parts));
                    session = rolledSession;
                    if (IsDuplicateSlice(parts[^1], rolled))
                    {
                        _logger.LogWarning("创作助手第 {Volume} 卷第 {Chapter} 章滚动会话后仍复读，提前结束拼接", volumeOrder, chapterOrder);
                        break;
                    }

                    parts.Add(rolled);
                    slices++;
                    continue;
                }

                duplicateRetries = 0;
                parts.Add(slice);
                slices++;
            }

            fullContent = Stitch(parts);
            if (fullContent.Length >= ChapterMinChars)
            {
                break;
            }

            if (attempt < 2)
            {
                _logger.LogWarning("创作助手第 {Volume} 卷第 {Chapter} 章仅 {Length} 字（第 {Attempt} 次尝试），换新会话整章重试",
                    volumeOrder, chapterOrder, fullContent.Length, attempt);
            }
        }

        return fullContent;
    }

    /// <summary>
    /// 构建本章首片提示词（注入总纲/卷目标/本章梗概/上一章结尾/爆款工艺）。
    /// </summary>
    private string BuildChapterFirstSlicePrompt(
        int volumeOrder, int chapterOrder, string chapterTitle, string brief, string blueprint, string prevTail)
    {
        var sb = new StringBuilder();
        sb.Append("User: 你是顶级爆款中文网文写手，正在创作《").Append(_state.ProjectName).Append("》第").Append(volumeOrder).Append("卷第").Append(chapterOrder).Append("章。\n");
        if (!string.IsNullOrWhiteSpace(_state.UserVision))
        {
            sb.Append("本书立意：").Append(Truncate(_state.UserVision, 200)).Append('\n');
        }
        sb.Append("\n【全书总纲】\n").Append(Truncate(blueprint, 1200)).Append('\n');
        sb.Append("\n【本章梗概】第").Append(chapterOrder).Append("章《").Append(chapterTitle).Append("》：").Append(Truncate(brief, 300)).Append('\n');
        sb.Append("\n【上一章结尾】\n").Append(prevTail).Append('\n');
        sb.Append("\n【爆款工艺】\n");
        sb.Append("1. 开篇即冲突：直接进入场景与冲突，禁止景物堆砌式开场；\n");
        sb.Append("2. 爽点节奏：本章至少一个爽点（打脸/升级/反转/获宝/扬名）；\n");
        sb.Append("3. 对话推动：多用短对话与动作推进，单段不超过3行，禁止大段说明文；\n");
        sb.Append("4. 人物立体：主角目标明确，反派有智商。");
        sb.Append("\n【本章要求】先输出本章正文第一部分（约1000字）：直接从场景与冲突切入，开头直接承接上一章结尾；");
        sb.Append("严禁复述或改写任何总纲、梗概、工艺内容，严禁输出【】包裹的标题结构，只输出书籍正文本身，不要章节标题，不要任何解释或备注。\n\nAssistant: <think></think\n");
        return sb.ToString();
    }

    /// <summary>
    /// 构建滚动会话提示词：新会话无法复用服务端 state，改为携带已写正文尾部续写。
    /// </summary>
    private string BuildRollOverPrompt(int chapterOrder, string chapterTitle, string brief, List<string> parts)
    {
        var writtenTail = TailOf(Stitch(parts), 2000);
        var sb = new StringBuilder();
        sb.Append("User: 你是顶级爆款中文网文写手，正在创作《").Append(_state.ProjectName).Append("》第").Append(chapterOrder).Append("章。\n");
        sb.Append("【本章梗概】").Append(Truncate(brief, 200)).Append('\n');
        sb.Append("\n【已写正文结尾】\n").Append(writtenTail).Append('\n');
        sb.Append("\n【要求】紧接着上面的内容继续写本章正文后续（约1000字）：直接从停笔处推进新冲突、新对话或场景转换，");
        sb.Append("禁止重复已写文字，禁止总结，禁止小标题，保持叙事连贯与爽点节奏。\n\nAssistant: <think></think\n");
        return sb.ToString();
    }

    /// <summary>
    /// 上一章结尾（当前卷内前一章；卷首章取上一卷最后一章）。
    /// </summary>
    private async Task<string> GetPreviousChapterTailAsync(Guid volumeId, int chapterOrder)
    {
        try
        {
            var chapters = (await _chapterService.GetChapterListAsync(volumeId)).OrderBy(c => c.Order).ToList();

            var prev = chapters.LastOrDefault(c => c.Order < chapterOrder);
            if (prev != null)
            {
                return TailOf(prev.Content, 260);
            }

            // 卷首章：取上一卷最后一章结尾（跨卷衔接）
            var volumes = (await _volumeService.GetVolumeListAsync(_state.ProjectId)).OrderBy(v => v.Order).ToList();
            var currentVolume = volumes.FirstOrDefault(v => v.Id == volumeId);
            var prevVolume = volumes.LastOrDefault(v => currentVolume == null || v.Order < currentVolume.Order);
            if (prevVolume != null)
            {
                var prevChapters = await _chapterService.GetChapterListAsync(prevVolume.Id);
                var last = prevChapters.OrderByDescending(c => c.Order).FirstOrDefault();
                if (last != null)
                {
                    return TailOf(last.Content, 260);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取上一章结尾失败（忽略）");
        }

        return "（本章为起始章或上一章缺失，直接开场）";
    }

    #endregion

    #region 章节关联处理（输入区关联书/卷/章后的直接处理）

    /// <summary>关联章节改写的单段原文长度（长文按此切分逐段处理，保证长度随原文伸缩）。</summary>
    private const int RewriteSegmentChars = 1800;

    /// <summary>关联章节改写的单段输出 token 预算。</summary>
    private const int RewriteSliceTokens = 1800;

    /// <summary>
    /// 处理关联章节：按用户要求改写已有正文（分段滚动处理），
    /// 或对无正文的草稿章按梗概+要求成文（切片拼接）。返回可直接采纳落库的确认卡。
    /// </summary>
    public async Task<PendingProposal> ProcessChapterOperationAsync(ChapterReferral referral, string instruction)
    {
        var chapter = await _chapterService.GetChapterByIdAsync(referral.ChapterId);
        if (chapter == null)
        {
            throw new InvalidOperationException("目标章节不存在或已被删除，请重新关联。");
        }

        var original = chapter.Content ?? string.Empty;
        var hasContent = !string.IsNullOrWhiteSpace(original);

        PostMessage(hasContent
            ? $"正在按你的要求处理《{referral.ProjectName}》第{referral.VolumeOrder}卷第{referral.ChapterOrder}章《{chapter.Title}》（原文 {original.Length} 字）…"
            : $"《{chapter.Title}》还没有正文，我将按梗概与你的要求创作本章正文…");

        var processed = hasContent
            ? await RewriteChapterCoreAsync(referral, chapter.Title, instruction, original)
            : await DraftChapterFromSummaryAsync(referral, chapter.Title, instruction, chapter.Summary);

        var proposal = new PendingProposal
        {
            Kind = ProposalKind.ChapterContent,
            TargetChapterId = referral.ChapterId,
            Title = $"章节处理：{chapter.Title}（{(hasContent ? $"原文 {original.Length} 字" : "草稿成文")} → {processed.Length} 字）",
            Summary = "已按你的要求处理该章，采纳后直接更新本章正文；也可逐项修改后采纳，或放弃整卡重新提要求。"
        };
        proposal.Items.Add(new ProposalItem
        {
            Title = chapter.Title,
            Body = processed,
            Fields =
            {
                ["书名"] = referral.ProjectName,
                ["卷序"] = referral.VolumeOrder.ToString(CultureInfo.InvariantCulture),
                ["章序"] = referral.ChapterOrder.ToString(CultureInfo.InvariantCulture)
            }
        });

        PostStageReport($"《{chapter.Title}》处理完成", new[]
        {
            hasContent ? $"原文 {original.Length} 字 → 处理后 {processed.Length} 字" : $"根据梗概生成 {processed.Length} 字",
            "采纳后将直接更新该章正文（不推进流水线）"
        });
        PostProposal(proposal);
        return proposal;
    }

    /// <summary>
    /// 已有正文的改写：长文按段切分，逐段携带「处理要求 + 已处理结尾」滚动改写，保证与原文长度伸缩一致。
    /// </summary>
    private async Task<string> RewriteChapterCoreAsync(ChapterReferral referral, string chapterTitle, string instruction, string original)
    {
        var segments = new List<string>();
        for (var i = 0; i < original.Length; i += RewriteSegmentChars)
        {
            segments.Add(original[i..Math.Min(i + RewriteSegmentChars, original.Length)]);
        }

        var parts = new List<string>();
        for (var i = 0; i < segments.Count; i++)
        {
            var prompt =
                "User: 你是顶级中文网文编辑，正在按作者要求处理《" + referral.ProjectName + "》第" + referral.VolumeOrder +
                "卷第" + referral.ChapterOrder + "章《" + chapterTitle + "》。\n" +
                "【处理要求】" + Truncate(instruction, 300) + "\n" +
                "【已处理正文结尾】\n" + (parts.Count > 0 ? TailOf(parts[^1], 500) : "（本段为开头）") + "\n" +
                "【原文片段 " + (i + 1) + "/" + segments.Count + "】\n" + segments[i] + "\n" +
                "【要求】输出上述片段处理后的正文（长度与片段相近，可略有增减）：直接输出正文，" +
                (i > 0 ? "与上文已处理内容衔接连贯；" : string.Empty) +
                "禁止解释、禁止小标题、禁止输出提示词内容。\n\n" +
                "Assistant: <think></think\n";

            var text = await GenerateTextAsync(prompt, RewriteSliceTokens);

            // 防复读：输出与原文片段几乎相同 → 换更强提示词重试一次
            if (IsDuplicateSlice(segments[i], text))
            {
                _logger.LogWarning("关联章节《{Title}》片段 {Index} 改写输出与原文重复，换提示词重试", chapterTitle, i + 1);
                var retryPrompt =
                    "User: 注意：上一次输出与原文几乎相同。请切实执行【处理要求】对下面的片段做实质性改写，" +
                    "输出必须与原文明显不同（句式、细节、节奏都要调整），直接输出改写后的正文。\n" +
                    "【处理要求】" + Truncate(instruction, 300) + "\n" +
                    "【原文片段 " + (i + 1) + "/" + segments.Count + "】\n" + segments[i] + "\n\n" +
                    "Assistant: <think></think\n";
                text = await GenerateTextAsync(retryPrompt, RewriteSliceTokens);
            }

            parts.Add(text);
        }

        return Stitch(parts);
    }

    /// <summary>
    /// 无正文草稿章：按梗概 + 要求切片成文（一次性 state 会话 + 防复读），工艺对齐 L5。
    /// </summary>
    private async Task<string> DraftChapterFromSummaryAsync(ChapterReferral referral, string chapterTitle, string instruction, string? summary)
    {
        var firstPrompt =
            "User: 你是顶级爆款中文网文写手，正在创作《" + referral.ProjectName + "》第" + referral.VolumeOrder +
            "卷第" + referral.ChapterOrder + "章《" + chapterTitle + "》。\n" +
            "【本章梗概】" + Truncate(summary ?? string.Empty, 300) + "\n" +
            "【写作要求】" + Truncate(instruction, 300) + "\n" +
            "【本章要求】先输出本章正文第一部分（约1000字）：直接从场景与冲突切入；" +
            "严禁输出【】标题结构，只输出书籍正文本身，不要解释。\n\n" +
            "Assistant: <think></think\n";

        var session = $"copilot-ref-{referral.ChapterId:N}-{Guid.NewGuid():N}";
        var parts = new List<string> { await GenerateSliceAsync(session, firstPrompt) };
        var slices = 1;
        while (TotalChars(parts) < ChapterMinChars && slices < MaxSlicesPerChapter)
        {
            var slice = await GenerateSliceAsync(session,
                "\n\nUser: （继续输出本章正文后续内容：直接从上文停笔处续写，不要重复已有文字，不要总结，不要小标题，保持叙事连贯，约1000字。）\n\nAssistant: <think></think\n");
            if (IsDuplicateSlice(parts[^1], slice))
            {
                _logger.LogWarning("关联章节《{Title}》成文切片复读，提前结束拼接", chapterTitle);
                break;
            }

            parts.Add(slice);
            slices++;
        }

        return Stitch(parts);
    }

    #endregion

    #region RWKV 调用与文本工具（对齐批量生成工艺）

    /// <summary>
    /// RWKV 生成（带重试，失败抛异常绝不降级）。
    /// </summary>
    private async Task<string> GenerateTextAsync(string prompt, int maxTokens, int retries = 2)
    {
        for (var attempt = 1; attempt <= retries + 1; attempt++)
        {
            var response = await _rwkvService.CompleteAsync(prompt, maxTokens);
            if (response.Success && !string.IsNullOrWhiteSpace(response.Text))
            {
                var cleaned = AIOutputSanitizer.ExtractCleanOutput(response.Text)?.Trim();
                if (!string.IsNullOrWhiteSpace(cleaned))
                {
                    return cleaned;
                }
            }

            _logger.LogWarning("RWKV 调用失败（第 {Attempt} 次）：{Error}", attempt, response.Error);
            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        throw new InvalidOperationException("RWKV 推理服务多次调用均失败，请检查 AI 模型配置页的服务状态。");
    }

    /// <summary>
    /// state 会话切片生成（服务端 state 缓存跨调用保持上下文），失败抛异常绝不降级。
    /// </summary>
    private async Task<string> GenerateSliceAsync(string sessionId, string prompt)
    {
        var response = await _rwkvService.CompleteWithStateAsync(sessionId, prompt, SliceBudget);
        if (!response.Success || string.IsNullOrWhiteSpace(response.Text))
        {
            throw new InvalidOperationException(response.Error ?? "RWKV state 切片返回空");
        }

        return CleanSlice(response.Text);
    }

    /// <summary>
    /// 切片文本清洗（对齐批量生成 CleanSlice：去 markdown/客套前缀/结构行回显）。
    /// </summary>
    private static string CleanSlice(string text)
    {
        var cleaned = AIOutputSanitizer.ExtractCleanOutput(text) ?? string.Empty;
        cleaned = cleaned.Trim();

        cleaned = Regex.Replace(cleaned, "^#+\\s.*$", string.Empty, RegexOptions.Multiline);
        cleaned = cleaned.Replace("---", string.Empty);
        cleaned = Regex.Replace(cleaned, "（?未完待续）?", string.Empty);

        // 去除客套前缀
        var firstLineEnd = cleaned.IndexOf('\n');
        if (firstLineEnd > 0)
        {
            var firstLine = cleaned[..firstLineEnd].Trim();
            if (firstLine.Length <= 60 && Regex.IsMatch(firstLine, "^(好的|当然|没问题|明白了|收到|以下是|遵照)"))
            {
                cleaned = cleaned[(firstLineEnd + 1)..].Trim();
            }
        }

        // 去除模型回显的提示词/大纲结构行（仅处理开头连续的结构行）
        while (true)
        {
            var idx = cleaned.IndexOf('\n');
            if (idx <= 0)
            {
                break;
            }

            var line = cleaned[..idx].Trim();
            if (!IsStructureLine(line))
            {
                break;
            }

            cleaned = cleaned[(idx + 1)..].Trim();
        }

        return cleaned.Trim();
    }

    /// <summary>
    /// 判断一行是否为提示词/大纲结构行（用于清理模型回显）。
    /// </summary>
    private static bool IsStructureLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return true;
        }

        if (Regex.IsMatch(line, "^【(全书总纲|全书大纲|本卷大纲|本章梗概|爆款工艺|本章要求|上一章结尾|已写正文结尾)】"))
        {
            return true;
        }

        if (Regex.IsMatch(line, "^第[0-9一二三四五六七八九十百]{1,4}章[:：·]"))
        {
            return true;
        }

        if (Regex.IsMatch(line, "^(\\*+\\s*|-{1,2}\\s+|\\d+\\.\\s+)") || line.StartsWith("**"))
        {
            return true;
        }

        return false;
    }

    private static string Stitch(List<string> parts)
    {
        var sb = new StringBuilder();
        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part))
            {
                continue;
            }

            if (sb.Length > 0)
            {
                sb.Append('\n');
            }

            sb.Append(part.Trim());
        }

        return sb.ToString();
    }

    private static int TotalChars(List<string> parts) => parts.Sum(p => p.Length);

    /// <summary>
    /// 防复读检测：下一片头部 60 字符与上一片尾部相同时视为复读。
    /// </summary>
    private static bool IsDuplicateSlice(string previous, string next)
    {
        if (string.IsNullOrWhiteSpace(previous) || string.IsNullOrWhiteSpace(next))
        {
            return false;
        }

        var a = previous.Trim();
        var b = next.Trim();
        if (a == b)
        {
            return true;
        }

        var head = b.Length >= 60 ? b[..60] : b;
        return a.EndsWith(head, StringComparison.Ordinal);
    }

    /// <summary>
    /// 取文本尾部（滚动会话/跨章衔接用）。
    /// </summary>
    private static string TailOf(string? text, int maxChars)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "（缺失）";
        }

        return text.Length <= maxChars ? text : "……" + text[^maxChars..];
    }

    private static string? Truncate(string? text, int maxChars) =>
        string.IsNullOrEmpty(text) ? null : text.Length <= maxChars ? text : text[..maxChars] + "……";

    /// <summary>
    /// 在原始文本上按行前缀提取字段值（跳过标题行，遵守「先原始后清洗」解析教训）。
    /// </summary>
    private static string? ExtractLineValue(string text, string fieldKey)
    {
        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.StartsWith(fieldKey, StringComparison.Ordinal))
            {
                var value = line[fieldKey.Length..].TrimStart('：', ':', ' ', '《', '「');
                return value.TrimEnd('》', '」');
            }
        }
        return null;
    }

    /// <summary>
    /// 按编号行切分原始文本（「1. xxx」/「1、xxx」），返回（内容, 序号）列表。
    /// </summary>
    private static List<(string Content, int Index)> ParseNumberedLines(string text, int minIndex, int maxCount)
    {
        var result = new List<(string, int)>();
        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            var match = Regex.Match(line, "^(?<num>[0-9]{1,2})[.、．]\\s*(?<body>.+)$");
            if (!match.Success)
            {
                continue;
            }

            if (!int.TryParse(match.Groups["num"].Value, out var index) || index < minIndex)
            {
                continue;
            }

            result.Add((match.Groups["body"].Value.Trim(), index));
            if (result.Count >= maxCount)
            {
                break;
            }
        }
        return result;
    }

    /// <summary>
    /// 按全角/半角竖线切分段。
    /// </summary>
    private static List<string> SplitSegments(string line) =>
        line.Split(new[] { '｜', '|' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToList();

    /// <summary>
    /// 从分段中取「键：值」形式段的值。
    /// </summary>
    private static string? GetSegmentValue(List<string> parts, string key)
    {
        foreach (var part in parts)
        {
            if (part.StartsWith(key, StringComparison.Ordinal))
            {
                var value = part[key.Length..].TrimStart('：', ':', ' ');
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }
        }
        return null;
    }

    /// <summary>
    /// 未标注类型时按剧情线名猜测（含「暗」「秘」→ 暗线，含「副」「支」→ 支线，否则主线）。
    /// </summary>
    private static string GuessPlotType(string title)
    {
        if (title.Contains('暗', StringComparison.Ordinal) || title.Contains('秘', StringComparison.Ordinal))
        {
            return "暗线";
        }

        if (title.Contains('支', StringComparison.Ordinal) || title.Contains('副', StringComparison.Ordinal))
        {
            return "支线";
        }

        return "主线";
    }

    /// <summary>
    /// 整改意见注入段（无意见返回 null；RunStageAsync 完成后清空避免影响后续阶段）。
    /// </summary>
    private string? FormatFeedback()
    {
        var feedback = _state.LastFeedback;
        return string.IsNullOrWhiteSpace(feedback)
            ? null
            : "作者整改意见：" + Truncate(feedback, 200) + "\n";
    }

    #endregion

    #region 数据读取与状态持久化

    /// <summary>
    /// 加载已采纳总纲（特殊 Plot 记录的 Outline 字段）。
    /// </summary>
    private async Task<string?> LoadBlueprintAsync(Guid projectId)
    {
        try
        {
            var plots = await _plotService.GetPlotsByTypeAsync(projectId, BlueprintPlotType);
            var blueprint = plots.FirstOrDefault(p => p.Title == BlueprintPlotTitle && !p.IsDeleted);
            return blueprint?.Outline;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "加载总纲失败");
            return null;
        }
    }

    /// <summary>
    /// 解析当前卷（按 CurrentVolumeOrder 匹配）。
    /// </summary>
    private async Task<Volume?> ResolveCurrentVolumeAsync(Guid projectId)
    {
        var volumes = await _volumeService.GetVolumeListAsync(projectId);
        return volumes.FirstOrDefault(v => v.Order == _state.CurrentVolumeOrder)
            ?? volumes.OrderByDescending(v => v.Order).FirstOrDefault();
    }

    /// <summary>
    /// 解析当前待写章节（当前卷内 Order>=CurrentChapterOrder 且正文为空的第一章）。
    /// </summary>
    private async Task<Chapter?> ResolveCurrentChapterAsync(Guid projectId)
    {
        var volume = await ResolveCurrentVolumeAsync(_state.ProjectId);
        if (volume == null)
        {
            return null;
        }

        var chapters = await _chapterService.GetChapterListAsync(volume.Id);
        return chapters.FirstOrDefault(c => c.Order >= _state.CurrentChapterOrder && string.IsNullOrWhiteSpace(c.Content))
            ?? chapters.OrderBy(c => c.Order).FirstOrDefault(c => string.IsNullOrWhiteSpace(c.Content));
    }

    /// <summary>
    /// 当前卷全部章节（按序）。
    /// </summary>
    private async Task<List<Chapter>> GetCurrentVolumeChaptersAsync(Guid projectId)
    {
        var volume = await ResolveCurrentVolumeAsync(projectId);
        if (volume == null)
        {
            return new List<Chapter>();
        }

        var chapters = await _chapterService.GetChapterListAsync(volume.Id);
        return chapters.OrderBy(c => c.Order).ToList();
    }

    /// <summary>
    /// 持久化流水线状态（复用批量生成先例：系统 Plot 的 Outline 字段存 JSON）。
    /// </summary>
    private async Task SaveStateAsync()
    {
        try
        {
            PipelineState snapshot;
            lock (_lock)
            {
                _state.UpdatedAt = DateTime.Now;
                snapshot = _state;
            }

            var plots = await _plotService.GetPlotsByTypeAsync(snapshot.ProjectId, StatePlotType);
            var statePlot = plots.FirstOrDefault(p => p.Title == StatePlotTitle && !p.IsDeleted);
            var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = false });

            if (statePlot == null)
            {
                await _plotService.CreatePlotAsync(new Plot
                {
                    ProjectId = snapshot.ProjectId,
                    Title = StatePlotTitle,
                    Type = StatePlotType,
                    Status = snapshot.Finished ? "已完成" : "进行中",
                    Outline = json,
                    Description = "AI创作流水线状态（系统自动维护）"
                });
            }
            else
            {
                statePlot.Outline = json;
                statePlot.Status = snapshot.Finished ? "已完成" : "进行中";
                await _plotService.UpdatePlotAsync(statePlot);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "保存流水线状态失败（不阻断流程）");
        }
    }

    /// <summary>
    /// 从系统 Plot 加载流水线状态。
    /// </summary>
    private async Task<PipelineState?> LoadStateAsync(Guid projectId)
    {
        try
        {
            var plots = await _plotService.GetPlotsByTypeAsync(projectId, StatePlotType);
            var statePlot = plots.FirstOrDefault(p => p.Title == StatePlotTitle && !p.IsDeleted);
            if (statePlot == null || string.IsNullOrWhiteSpace(statePlot.Outline))
            {
                return null;
            }

            return JsonSerializer.Deserialize<PipelineState>(statePlot.Outline);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "加载流水线状态失败");
            return null;
        }
    }

    #endregion

    #region 事件与展示辅助

    /// <summary>
    /// 发送文本消息（汇报/提示）。
    /// </summary>
    private void PostMessage(string text) =>
        MessageReady?.Invoke(this, new CopilotMessageItem { Role = CopilotMessageRole.Assistant, Text = text });

    /// <summary>
    /// 发送确认卡。
    /// </summary>
    private void PostProposal(PendingProposal proposal) =>
        ProposalReady?.Invoke(this, proposal);

    /// <summary>
    /// 发送阶段汇报（统一步骤格式 + 询问下一步）。
    /// </summary>
    private void PostStageReport(string stageTitle, IEnumerable<string> points)
    {
        var sb = new StringBuilder();
        sb.Append("【").Append(stageTitle).Append("】\n");
        foreach (var point in points.Where(p => !string.IsNullOrWhiteSpace(p)).Take(4))
        {
            sb.Append("- ").Append(point.Trim()).Append('\n');
        }

        sb.Append("（生成内容见下方确认卡，可逐项采纳/修改/放弃）");
        PostMessage(sb.ToString().TrimEnd());
    }

    /// <summary>
    /// 阶段名描述（恢复提示用）。
    /// </summary>
    private static string DescribeStage(PipelineStage stage) => stage switch
    {
        PipelineStage.BlueprintPending => "总纲规划",
        PipelineStage.PlotLinesPending => "剧情线规划",
        PipelineStage.VolumesPending => "分卷规划",
        PipelineStage.ChapterDraftsPending => "章节剧情草稿",
        PipelineStage.ChapterContentInProgress => "章节正文创作",
        PipelineStage.Completed => "已完成",
        _ => "未开始"
    };

    private void RaiseProgressChanged() => ProgressChanged?.Invoke(this, EventArgs.Empty);

    #endregion
}
