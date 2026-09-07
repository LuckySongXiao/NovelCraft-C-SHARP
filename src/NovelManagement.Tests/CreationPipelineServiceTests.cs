using Microsoft.Extensions.Logging.Abstractions;
using NovelManagement.Application.Services;
using NovelManagement.Core.Entities;
using NovelManagement.Tests.Fakes;
using NovelManagement.WPF.Services.Copilot;
using Xunit;

namespace NovelManagement.Tests;

/// <summary>
/// 五级创作流水线单元测试：
/// 1. L1-L5 各级生成 → 解析 → 确认卡
/// 2. 状态持久化（系统 Plot JSON）与重启续跑
/// 3. RWKV 失败/解析失败绝不降级假数据（铁律）
/// </summary>
public class CreationPipelineServiceTests
{
    private const string BlueprintText =
        "书名：《星穹之下》\n" +
        "定位：都市修仙爽文，小人物逆天改命\n" +
        "核心冲突：凡人少年对抗操控灵脉的隐世组织\n" +
        "阶段脉络：\n" +
        "1. 第一卷：灵气复苏，主角觉醒\n" +
        "2. 第二卷：进入宗门，崭露头角\n";

    private const string PlotLinesText =
        "1. 灵气复苏｜类型：主线｜描述：天地灵气重现，主角觉醒灵脉。｜冲突：旧秩序与新生力量的碰撞\n" +
        "2. 宗门暗流｜类型：暗线｜描述：宗门高层操控灵脉的秘密。｜冲突：真相与谎言\n" +
        "3. 同门情谊｜类型：支线｜描述：与同门师姐的羁绊。｜冲突：个人与宗门\n";

    private const string VolumesText =
        "1. 觉醒之卷｜卷目标：主角觉醒并踏入修行界｜收束事件：击败第一波追杀\n" +
        "2. 崭露头角｜卷目标：进入宗门获得认可｜收束事件：外门大比夺魁\n";

    private const string ChapterDraftsText =
        "1. 灵脉初醒｜深夜矿井中主角手掌发光，灵气入体\n" +
        "2. 不速之客｜神秘组织上门搜查，主角险些暴露\n" +
        "3. 逃离小镇｜主角告别家人踏上修行路\n";

    private static string LongChapterContent() => new('甲', 2100);

    private sealed class Harness
    {
        public FakeRwkvLightningService Rwkv { get; } = new();
        public FakeUnitOfWork Uow { get; } = new();
        public PlotService Plots { get; }
        public VolumeService Volumes { get; }
        public ChapterService Chapters { get; }
        public CreationPipelineService Pipeline { get; }
        public List<PendingProposal> Proposals { get; } = new();
        public List<CopilotMessageItem> Messages { get; } = new();

        public Harness(params string[] rwkvResponses)
        {
            foreach (var response in rwkvResponses)
            {
                Rwkv.Responses.Enqueue(response);
            }

            Plots = new PlotService(Uow, NullLogger<PlotService>.Instance);
            Volumes = new VolumeService(Uow, NullLogger<VolumeService>.Instance);
            Chapters = new ChapterService(Uow, NullLogger<ChapterService>.Instance);
            Pipeline = new CreationPipelineService(
                NullLogger<CreationPipelineService>.Instance, Rwkv, Plots, Volumes, Chapters);
            Pipeline.ProposalReady += (_, p) => Proposals.Add(p);
            Pipeline.MessageReady += (_, m) => Messages.Add(m);
        }

        /// <summary>模拟会话服务采纳总纲：落库特殊 Plot（Type=总纲）。</summary>
        public async Task AcceptOutlineAsync(Guid projectId)
        {
            var item = Proposals.Single(p => p.Kind == ProposalKind.Outline).Items[0];
            await Plots.CreatePlotAsync(new Plot
            {
                ProjectId = projectId,
                Title = "全书总纲",
                Type = "总纲",
                Status = "规划中",
                Outline = item.EffectiveBody,
                Importance = 10
            });
        }
    }

    private static readonly Guid ProjectId = new("00000000-0000-0000-0000-0000000000C0");

    #region L1 总纲

    [Fact]
    public async Task StartPlanning_GeneratesOutlineProposalAndPersistsState()
    {
        var h = new Harness(BlueprintText);
        await h.Pipeline.StartPlanningAsync(ProjectId, "星穹之下", "小人物逆天改命");

        var proposal = Assert.Single(h.Proposals);
        Assert.Equal(ProposalKind.Outline, proposal.Kind);
        var item = Assert.Single(proposal.Items);
        Assert.Equal("星穹之下", item.Title); // ExtractLineValue 解析「书名：」并剥书名号
        Assert.Equal("都市修仙爽文，小人物逆天改命", item.Fields["定位"]);
        Assert.Equal("凡人少年对抗操控灵脉的隐世组织", item.Fields["核心冲突"]);

        // 状态持久化：系统 Plot JSON，Stage=BlueprintPending
        var statePlot = h.Uow.PlotRepo.Plots.Single(p => p.Type == "系统" && p.Title == "创作流水线进度");
        var state = System.Text.Json.JsonSerializer.Deserialize<PipelineState>(statePlot.Outline!);
        Assert.NotNull(state);
        Assert.Equal(PipelineStage.BlueprintPending, state!.Stage);
        Assert.Equal(ProjectId, state.ProjectId);
        Assert.Equal("小人物逆天改命", state.UserVision);
    }

    [Fact]
    public async Task StartPlanning_RwkvFailure_NoProposalAndExplicitError()
    {
        var h = new Harness();
        h.Rwkv.FailAll = true;

        await h.Pipeline.StartPlanningAsync(ProjectId, "星穹之下", "想法");

        Assert.Empty(h.Proposals); // 绝不降级假数据
        Assert.Contains(h.Messages, m => m.Text.Contains("RWKV 推理服务多次调用均失败"));
    }

    #endregion

    #region L1 → L2 剧情线

    [Fact]
    public async Task AdvanceToPlotLines_ParsesNumberedLineSegments()
    {
        var h = new Harness(BlueprintText, PlotLinesText);
        await h.Pipeline.StartPlanningAsync(ProjectId, "星穹之下", "想法");
        await h.AcceptOutlineAsync(ProjectId);
        await h.Pipeline.AdvanceAsync(ProposalKind.Outline, ProjectId);

        var proposal = h.Proposals.Single(p => p.Kind == ProposalKind.PlotLine);
        Assert.Equal(3, proposal.Items.Count);

        var first = proposal.Items[0];
        Assert.Equal("灵气复苏", first.Title);
        Assert.Equal("主线", first.Fields["类型"]);
        Assert.Equal("天地灵气重现，主角觉醒灵脉。", first.Body);
        Assert.Equal("旧秩序与新生力量的碰撞", first.Fields["冲突"]);

        var second = proposal.Items[1];
        Assert.Equal("暗线", second.Fields["类型"]);
    }

    [Fact]
    public async Task AdvanceToPlotLines_ParseFailure_PostsErrorWithoutProposal()
    {
        var h = new Harness(BlueprintText, "这段输出没有编号行，模型跑题了。");
        await h.Pipeline.StartPlanningAsync(ProjectId, "星穹之下", "想法");
        await h.AcceptOutlineAsync(ProjectId);
        await h.Pipeline.AdvanceAsync(ProposalKind.Outline, ProjectId);

        Assert.DoesNotContain(h.Proposals, p => p.Kind == ProposalKind.PlotLine);
        Assert.Contains(h.Messages, m => m.Text.Contains("生成失败") && m.Text.Contains("剧情线输出解析失败"));
    }

    #endregion

    #region L3 分卷与追加接续

    [Fact]
    public async Task AppendVolume_IncludesExistingVolumesInPrompt()
    {
        var h = new Harness(BlueprintText, PlotLinesText, VolumesText, ChapterDraftsText, VolumesText);
        await h.Pipeline.StartPlanningAsync(ProjectId, "星穹之下", "想法");
        await h.AcceptOutlineAsync(ProjectId);
        await h.Pipeline.AdvanceAsync(ProposalKind.Outline, ProjectId);

        // 采纳 L2 剧情线 → L3 分卷
        foreach (var item in h.Proposals.Single(p => p.Kind == ProposalKind.PlotLine).Items)
        {
            await h.Plots.CreatePlotAsync(new Plot
            {
                ProjectId = ProjectId,
                Title = item.Title,
                Type = item.Fields["类型"],
                Status = "规划中",
                Description = item.EffectiveBody
            });
        }

        await h.Pipeline.AdvanceAsync(ProposalKind.PlotLine, ProjectId);
        var volumeProposal = h.Proposals.Single(p => p.Kind == ProposalKind.Volume);
        Assert.Equal(2, volumeProposal.Items.Count);
        Assert.DoesNotContain("已有分卷", h.Rwkv.ReceivedPrompts[^1]);

        // 模拟采纳分卷（会推进到 L4），再请求追加：提示词必须注入已有卷
        var volume = h.Proposals.Single(p => p.Kind == ProposalKind.Volume).Items[0];
        await h.Volumes.CreateVolumeAsync(new Volume
        {
            ProjectId = ProjectId,
            Title = volume.Title,
            Description = volume.EffectiveBody,
            Order = 1
        });

        await h.Pipeline.AdvanceAsync(ProposalKind.Volume, ProjectId); // → L4 草稿
        await h.Pipeline.RequestAppendVolumeAsync(ProjectId);           // → 再次进入分卷规划

        Assert.Contains("已有分卷", h.Rwkv.ReceivedPrompts[^1]);
        Assert.Contains("第1卷觉醒之卷", h.Rwkv.ReceivedPrompts[^1]);
        Assert.Equal(2, h.Proposals.Count(p => p.Kind == ProposalKind.Volume));
    }

    #endregion

    #region L4 章节草稿与 L5 正文

    [Fact]
    public async Task AdvanceToChapterDrafts_BuildsDraftProposalForCurrentVolume()
    {
        var h = new Harness(BlueprintText, PlotLinesText, VolumesText, ChapterDraftsText);
        await h.Pipeline.StartPlanningAsync(ProjectId, "星穹之下", "想法");
        await h.AcceptOutlineAsync(ProjectId);
        await h.Pipeline.AdvanceAsync(ProposalKind.Outline, ProjectId);

        foreach (var item in h.Proposals.Single(p => p.Kind == ProposalKind.PlotLine).Items)
        {
            await h.Plots.CreatePlotAsync(new Plot
            {
                ProjectId = ProjectId,
                Title = item.Title,
                Type = item.Fields["类型"],
                Status = "规划中",
                Description = item.EffectiveBody
            });
        }

        await h.Pipeline.AdvanceAsync(ProposalKind.PlotLine, ProjectId);
        await h.Volumes.CreateVolumeAsync(new Volume
        {
            ProjectId = ProjectId, Title = "觉醒之卷", Description = "目标", Order = 1
        });
        await h.Pipeline.AdvanceAsync(ProposalKind.Volume, ProjectId);

        var draft = h.Proposals.Single(p => p.Kind == ProposalKind.ChapterDraft);
        Assert.StartsWith("第1卷章节剧情草稿", draft.Title);
        Assert.Equal(3, draft.Items.Count);
        Assert.All(draft.Items, i => Assert.Equal("1", i.Fields["卷序"]));
        Assert.Equal("灵脉初醒", draft.Items[0].Title);
        Assert.Equal("深夜矿井中主角手掌发光，灵气入体", draft.Items[0].Body);
    }

    [Fact]
    public async Task AdvanceToChapterContent_GeneratesLongContentProposal()
    {
        var h = new Harness(BlueprintText, PlotLinesText, VolumesText, ChapterDraftsText, LongChapterContent());
        await h.Pipeline.StartPlanningAsync(ProjectId, "星穹之下", "想法");
        await h.AcceptOutlineAsync(ProjectId);
        await h.Pipeline.AdvanceAsync(ProposalKind.Outline, ProjectId);

        foreach (var item in h.Proposals.Single(p => p.Kind == ProposalKind.PlotLine).Items)
        {
            await h.Plots.CreatePlotAsync(new Plot
            {
                ProjectId = ProjectId, Title = item.Title, Type = item.Fields["类型"],
                Status = "规划中", Description = item.EffectiveBody
            });
        }

        await h.Pipeline.AdvanceAsync(ProposalKind.PlotLine, ProjectId);
        var v1 = await h.Volumes.CreateVolumeAsync(new Volume { ProjectId = ProjectId, Title = "觉醒之卷", Order = 1 });
        var v2 = await h.Volumes.CreateVolumeAsync(new Volume { ProjectId = ProjectId, Title = "崭露头角", Order = 2 });
        Assert.Equal(1, v1.Order);
        await h.Pipeline.AdvanceAsync(ProposalKind.Volume, ProjectId); // 指针定位到最新卷 v2

        // 采纳第 2 卷章节草稿 → 创建空白章节
        var draftItems = h.Proposals.Single(p => p.Kind == ProposalKind.ChapterDraft).Items;
        for (var i = 0; i < draftItems.Count; i++)
        {
            await h.Chapters.CreateChapterAsync(new Chapter
            {
                VolumeId = v2.Id, Title = draftItems[i].Title,
                Summary = draftItems[i].EffectiveBody, Order = i + 1, Status = "Draft"
            });
        }

        await h.Pipeline.AdvanceAsync(ProposalKind.ChapterDraft, ProjectId); // → L5 正文

        var content = h.Proposals.Single(p => p.Kind == ProposalKind.ChapterContent);
        var body = Assert.Single(content.Items).Body;
        Assert.True(body.Length >= 2000, $"正文应达到 {2000} 字下限，实际 {body.Length}");

        // 指针已定位到第 2 卷第 1 章
        var snapshot = h.Pipeline.GetStateSnapshot();
        Assert.Equal(2, snapshot.CurrentVolumeOrder);
        Assert.Equal(PipelineStage.ChapterContentInProgress, snapshot.Stage);
    }

    #endregion

    #region 章节关联处理

    private async Task<(ChapterReferral Referral, Chapter Chapter)> SeedChapterAsync(
        Harness h, string? content, string? summary = "主角在矿井中觉醒灵脉")
    {
        var volume = await h.Volumes.CreateVolumeAsync(new Volume { ProjectId = ProjectId, Title = "觉醒之卷", Order = 1 });
        var chapter = await h.Chapters.CreateChapterAsync(new Chapter
        {
            VolumeId = volume.Id, Title = "灵脉初醒", Summary = summary, Order = 1, Status = "Draft"
        });

        if (!string.IsNullOrEmpty(content))
        {
            await h.Chapters.UpdateChapterContentAsync(chapter.Id, content);
        }

        var referral = new ChapterReferral
        {
            ProjectId = ProjectId,
            ProjectName = "星穹之下",
            VolumeId = volume.Id,
            VolumeTitle = volume.Title,
            VolumeOrder = volume.Order,
            ChapterId = chapter.Id,
            ChapterTitle = chapter.Title,
            ChapterOrder = chapter.Order,
            Summary = summary
        };
        return (referral, chapter);
    }

    [Fact]
    public async Task ProcessChapterOperation_RewriteExistingContent_SegmentsStitched()
    {
        var h = new Harness("改写第一段文本", "改写第二段文本");
        var (referral, chapter) = await SeedChapterAsync(h, content: new string('原', 2000)); // 两段

        var proposal = await h.Pipeline.ProcessChapterOperationAsync(referral, "润色全文，强化战斗节奏");

        Assert.Equal(ProposalKind.ChapterContent, proposal.Kind);
        Assert.Equal(chapter.Id, proposal.TargetChapterId);
        Assert.Contains("原文 2000 字", proposal.Title);
        var item = Assert.Single(proposal.Items);
        Assert.Contains("改写第一段文本", item.Body);
        Assert.Contains("改写第二段文本", item.Body);
        Assert.Equal(2, h.Rwkv.ReceivedPrompts.Count); // 每段一次调用，无重复

        // 改写提示词必须携带原文片段与处理要求
        Assert.Contains("处理要求", h.Rwkv.ReceivedPrompts[0]);
        Assert.Contains("原文片段 1/2", h.Rwkv.ReceivedPrompts[0]);
        Assert.Contains("已处理正文结尾", h.Rwkv.ReceivedPrompts[1]); // 滚动衔接
    }

    [Fact]
    public async Task ProcessChapterOperation_DraftFromSummary_GeneratesLongContent()
    {
        var h = new Harness(new string('文', 2100));
        var (referral, chapter) = await SeedChapterAsync(h, content: null);

        var proposal = await h.Pipeline.ProcessChapterOperationAsync(referral, "把觉醒场面写得有压迫感");

        Assert.Equal(chapter.Id, proposal.TargetChapterId);
        Assert.Contains("草稿成文", proposal.Title);
        var item = Assert.Single(proposal.Items);
        Assert.True(item.Body.Length >= 2000, $"成文应达到 {2000} 字下限，实际 {item.Body.Length}");
        Assert.Contains("本章梗概", h.Rwkv.ReceivedPrompts[0]); // 无正文 → 按梗概成文
    }

    [Fact]
    public async Task ProcessChapterOperation_RwkvFailure_ThrowsWithoutFakeData()
    {
        var h = new Harness();
        h.Rwkv.FailAll = true;
        var (referral, _) = await SeedChapterAsync(h, content: null); // 成文路径单次调用，快速验证铁律

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => h.Pipeline.ProcessChapterOperationAsync(referral, "润色"));

        Assert.DoesNotContain(h.Proposals, p => p.TargetChapterId != null); // 绝不降级假卡
    }

    #endregion

    #region 状态恢复

    [Fact]
    public async Task TryResumeAsync_RestoresStageAndRebuildsProposal()
    {
        var h1 = new Harness(BlueprintText);
        await h1.Pipeline.StartPlanningAsync(ProjectId, "星穹之下", "小人物逆天改命");

        // 模拟重启：新 RWKV 实例 + 同一存储（h1 的实体服务挂在 h1.Uow 上）
        var rwkv2 = new FakeRwkvLightningService();
        rwkv2.Responses.Enqueue(BlueprintText);
        var resumedPipeline = new CreationPipelineService(
            NullLogger<CreationPipelineService>.Instance, rwkv2, h1.Plots, h1.Volumes, h1.Chapters);
        var resumedProposals = new List<PendingProposal>();
        resumedPipeline.ProposalReady += (_, p) => resumedProposals.Add(p);

        var resumed = await resumedPipeline.TryResumeAsync(ProjectId);

        Assert.True(resumed);
        var proposal = Assert.Single(resumedProposals);
        Assert.Equal(ProposalKind.Outline, proposal.Kind);
        Assert.Equal(PipelineStage.BlueprintPending, resumedPipeline.CurrentStage);
    }

    [Fact]
    public async Task TryResumeAsync_NoPersistedState_ReturnsFalse()
    {
        var h = new Harness();
        var resumed = await h.Pipeline.TryResumeAsync(ProjectId);
        Assert.False(resumed);
        Assert.Equal(PipelineStage.NotStarted, h.Pipeline.CurrentStage);
    }

    #endregion
}
