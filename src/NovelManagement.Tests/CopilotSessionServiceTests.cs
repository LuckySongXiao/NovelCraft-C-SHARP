using Microsoft.Extensions.Logging.Abstractions;
using NovelManagement.Application.Services;
using NovelManagement.Core.Entities;
using NovelManagement.Tests.Fakes;
using NovelManagement.WPF.Services;
using NovelManagement.WPF.Services.Copilot;
using Xunit;

namespace NovelManagement.Tests;

/// <summary>
/// 创作助手会话中枢单元测试：
/// 1. 意图分派（导航回调/本地查询/自由问答/愿景收集）
/// 2. 确认卡生命周期（单项采纳/修改模式/放弃重生成/旧卡过期）
/// 3. 采纳落库与流水线推进（L1→L5 端到端）
/// </summary>
public class CopilotSessionServiceTests
{
    private const string BlueprintText =
        "书名：《星穹之下》\n" +
        "定位：都市修仙爽文\n" +
        "核心冲突：凡人少年对抗操控灵脉的隐世组织\n" +
        "阶段脉络：\n" +
        "1. 第一卷：灵气复苏，主角觉醒\n";

    private const string PlotLinesText =
        "1. 灵气复苏｜类型：主线｜描述：天地灵气重现，主角觉醒灵脉。｜冲突：新旧秩序碰撞\n" +
        "2. 宗门暗流｜类型：暗线｜描述：高层操控灵脉的秘密。｜冲突：真相与谎言\n" +
        "3. 同门情谊｜类型：支线｜描述：与师姐的羁绊。｜冲突：个人与宗门\n";

    private const string VolumesText =
        "1. 觉醒之卷｜卷目标：主角觉醒并踏入修行界｜收束事件：击败追杀\n" +
        "2. 崭露头角｜卷目标：进入宗门获得认可｜收束事件：外门大比夺魁\n";

    private const string ChapterDraftsText =
        "1. 灵脉初醒｜深夜矿井中主角手掌发光\n" +
        "2. 不速之客｜神秘组织上门搜查\n" +
        "3. 逃离小镇｜主角踏上修行路\n";

    private static string LongChapterContent() => new('甲', 2100);

    private static readonly Guid ProjectId = new("00000000-0000-0000-0000-0000000000D0");

    private sealed class Harness
    {
        public FakeRwkvLightningService Rwkv { get; } = new();
        public FakeUnitOfWork Uow { get; } = new();
        public PlotService Plots { get; }
        public VolumeService Volumes { get; }
        public ChapterService Chapters { get; }
        public CreationPipelineService Pipeline { get; }
        public CopilotSessionService Session { get; }

        public List<(NavigationTarget Target, NavigationContext? Context)> Navigations { get; } = new();

        public Harness(params string[] rwkvResponses)
        {
            // 意图兜底调用（无 JSON → 按自由问答）走标记路由固定回复，不消耗流水线脚本队列
            Rwkv.IntentFallbackResponse = "。";
            foreach (var response in rwkvResponses)
            {
                Rwkv.Responses.Enqueue(response);
            }

            Plots = new PlotService(Uow, NullLogger<PlotService>.Instance);
            Volumes = new VolumeService(Uow, NullLogger<VolumeService>.Instance);
            Chapters = new ChapterService(Uow, NullLogger<ChapterService>.Instance);
            Pipeline = new CreationPipelineService(
                NullLogger<CreationPipelineService>.Instance, Rwkv, Plots, Volumes, Chapters);
            var intent = new CopilotIntentService(Rwkv, NullLogger<CopilotIntentService>.Instance);
            Session = new CopilotSessionService(
                NullLogger<CopilotSessionService>.Instance, intent, Pipeline, Rwkv, Plots, Volumes, Chapters);
            Session.NavigationRequested += (target, context) => Navigations.Add((target, context));
        }

        /// <summary>走完「开始规划 → 描述愿景」得到 L1 总纲确认卡。</summary>
        public async Task<PendingProposal> StartToOutlineAsync()
        {
            Session.OpenForProject(ProjectId, "星穹之下");
            await Session.SendUserMessageAsync("开始规划");
            await Session.SendUserMessageAsync("小人物逆天改命的修仙爽文");
            var proposal = Session.CurrentProposal;
            Assert.NotNull(proposal);
            Assert.Equal(ProposalKind.Outline, proposal!.Kind);
            return proposal;
        }

        /// <summary>采纳当前确认卡全部 Pending 项并推进流水线。</summary>
        public async Task AcceptAllCurrentItemsAsync()
        {
            var proposal = Session.CurrentProposal!;
            foreach (var item in proposal.Items.Where(i => i.IsPending).ToList())
            {
                Session.AcceptItem(proposal.Id, item.Id);
            }

            await Session.AcceptProposalAsync(proposal.Id);
        }
    }

    #region 会话生命周期与意图分派

    [Fact]
    public void OpenForProject_BindsContextAndPostsWelcome()
    {
        var h = new Harness();
        h.Session.OpenForProject(ProjectId, "星穹之下");

        Assert.Equal(ProjectId, h.Session.CurrentProjectId);
        Assert.Equal("星穹之下", h.Session.CurrentProjectName);
        Assert.Contains(h.Session.Messages, m => m.Role == CopilotMessageRole.System && m.Text.Contains("已连接"));
    }

    [Fact]
    public void OpenForProject_SameProject_NoReset()
    {
        var h = new Harness();
        h.Session.OpenForProject(ProjectId, "星穹之下");
        var count = h.Session.Messages.Count;
        h.Session.OpenForProject(ProjectId, "星穹之下");
        Assert.Equal(count, h.Session.Messages.Count);
    }

    [Fact]
    public async Task SendUserMessageAsync_NavigateIntent_InvokesNavigationCallback()
    {
        var h = new Harness();
        h.Session.OpenForProject(ProjectId, "星穹之下");

        await h.Session.SendUserMessageAsync("打开角色管理");

        var navigation = Assert.Single(h.Navigations);
        Assert.Equal(NavigationTarget.CharacterManagement, navigation.Target);
        Assert.Equal("Copilot", navigation.Context?.Source);
        Assert.Contains(h.Session.Messages, m => m.Role == CopilotMessageRole.Assistant && m.Text.Contains("已切换到角色管理"));
    }

    [Fact]
    public async Task SendUserMessageAsync_QueryIntent_ReportsLocalStatistics()
    {
        var h = new Harness();
        await h.Volumes.CreateVolumeAsync(new Volume { ProjectId = ProjectId, Title = "第一卷", Order = 1 });
        await h.Plots.CreatePlotAsync(new Plot { ProjectId = ProjectId, Title = "主线", Type = "主线" });
        await h.Plots.CreatePlotAsync(new Plot { ProjectId = ProjectId, Title = "系统状态", Type = "系统" });
        await h.Chapters.CreateChapterAsync(new Chapter { VolumeId = h.Uow.VolumeRepo.Volumes[0].Id, Title = "第一章", Order = 1, Content = "正文", WordCount = 1500 });
        await h.Chapters.CreateChapterAsync(new Chapter { VolumeId = h.Uow.VolumeRepo.Volumes[0].Id, Title = "第二章", Order = 2 });
        h.Session.OpenForProject(ProjectId, "星穹之下");

        await h.Session.SendUserMessageAsync("已经写了多少字");

        var reply = h.Session.Messages.Last(m => m.Role == CopilotMessageRole.Assistant).Text;
        Assert.Contains("卷宗：1 卷", reply);
        Assert.Contains("剧情/剧情线：1 条", reply); // 系统 Plot 不计入
        Assert.Contains("章节：2 章（已完成正文 1 章，共 1500 字）", reply);
    }

    [Fact]
    public async Task SendUserMessageAsync_ChatOffline_ReportsOfflineWithoutFakeAnswer()
    {
        var h = new Harness();
        h.Rwkv.IsAvailable = false;
        h.Session.OpenForProject(ProjectId, "星穹之下");

        await h.Session.SendUserMessageAsync("帮我想一个主角名字");

        Assert.Contains(h.Session.Messages, m => m.Text.Contains("离线"));
        Assert.DoesNotContain(h.Session.Messages, m => m.Text.Contains("主角名字是")); // 绝不编造回答
    }

    [Fact]
    public async Task SendUserMessageAsync_ChatOnline_ReturnsCleanedAnswer()
    {
        var h = new Harness();
        h.Rwkv.Responses.Enqueue("建议主角名叫林晚，性格坚韧。");
        h.Session.OpenForProject(ProjectId, "星穹之下");

        await h.Session.SendUserMessageAsync("帮我想一个主角名字");

        Assert.Contains(h.Session.Messages, m => m.Text == "建议主角名叫林晚，性格坚韧。");
    }

    [Fact]
    public async Task SendUserMessageAsync_StartThenVision_LaunchesPipeline()
    {
        var h = new Harness(BlueprintText);
        h.Session.OpenForProject(ProjectId, "星穹之下");

        await h.Session.SendUserMessageAsync("开始规划");
        Assert.Contains(h.Session.Messages, m => m.Text.Contains("告诉我你对这本书的想法"));

        await h.Session.SendUserMessageAsync("小人物逆天改命的修仙爽文");

        Assert.NotNull(h.Session.CurrentProposal);
        Assert.Equal(ProposalKind.Outline, h.Session.CurrentProposal!.Kind);
        Assert.Equal(PipelineStage.BlueprintPending, h.Pipeline.CurrentStage);
    }

    #endregion

    #region 确认卡生命周期

    [Fact]
    public async Task AcceptOutlineProposal_PersistsPlotAndAdvancesToPlotLines()
    {
        var h = new Harness(BlueprintText, PlotLinesText);
        var outline = await h.StartToOutlineAsync();

        h.Session.AcceptItem(outline.Id, outline.Items[0].Id);
        await h.Session.AcceptProposalAsync(outline.Id);

        // 总纲落库：特殊 Plot（Type=总纲，Importance=10）
        var blueprint = h.Uow.PlotRepo.Plots.Single(p => p.Type == "总纲");
        Assert.Equal("全书总纲", blueprint.Title);
        Assert.Equal(10, blueprint.Importance);
        Assert.Equal(outline.Items[0].EffectiveBody, blueprint.Outline);

        // 流水线推进到 L2 剧情线
        Assert.Equal(ProposalStatus.Accepted, outline.Status);
        Assert.NotNull(h.Session.CurrentProposal);
        Assert.Equal(ProposalKind.PlotLine, h.Session.CurrentProposal!.Kind);
        Assert.Contains(h.Session.Messages, m => m.Text.Contains("已落库 1 项"));
    }

    [Fact]
    public async Task AcceptProposal_WithoutAcceptedItems_PromptsInsteadOfPersisting()
    {
        var h = new Harness(BlueprintText);
        var outline = await h.StartToOutlineAsync();

        await h.Session.AcceptProposalAsync(outline.Id);

        Assert.DoesNotContain(h.Uow.PlotRepo.Plots, p => p.Type == "总纲"); // 未落库
        Assert.Contains(h.Session.Messages, m => m.Text.Contains("请先勾选"));
        Assert.Equal(ProposalStatus.Pending, outline.Status); // 卡片保持待裁决
    }

    [Fact]
    public async Task ItemEditFlow_AppliesEditedTextOnAccept()
    {
        var h = new Harness(BlueprintText, PlotLinesText);
        var outline = await h.StartToOutlineAsync();
        var item = outline.Items[0];

        var original = h.Session.BeginItemEdit(outline.Id, item.Id);
        Assert.True(h.Session.IsEditing);
        Assert.Equal(item.Body, original);

        await h.Session.SendUserMessageAsync("全新总纲内容");
        Assert.False(h.Session.IsEditing);
        Assert.Equal("全新总纲内容", item.EditedBody);
        Assert.Equal(ProposalStatus.Modified, item.Status);

        h.Session.AcceptItem(outline.Id, item.Id); // 有修改文本 → Modified 而非 Accepted
        Assert.Equal(ProposalStatus.Modified, item.Status);

        await h.Session.AcceptProposalAsync(outline.Id);
        Assert.Equal("全新总纲内容", h.Uow.PlotRepo.Plots.Single(p => p.Type == "总纲").Outline);
    }

    [Fact]
    public async Task CancelItemEdit_ExitsEditModeWithoutChanges()
    {
        var h = new Harness(BlueprintText);
        var outline = await h.StartToOutlineAsync();

        h.Session.BeginItemEdit(outline.Id, outline.Items[0].Id);
        h.Session.CancelItemEdit();

        Assert.False(h.Session.IsEditing);
        Assert.Null(outline.Items[0].EditedBody);
    }

    [Fact]
    public async Task RejectProposal_TriggersRegenerationWithNewCard()
    {
        var h = new Harness(BlueprintText, BlueprintText);
        var first = await h.StartToOutlineAsync();

        await h.Session.RejectProposalAsync(first.Id);

        Assert.Equal(ProposalStatus.Rejected, first.Status);
        Assert.NotEqual(first.Id, h.Session.CurrentProposal!.Id);
        Assert.Contains(h.Session.Messages, m => m.Text.Contains("已放弃本卡全部内容"));
        Assert.Equal(2, h.Rwkv.ReceivedPrompts.Count(p => p.Contains("设计全书总纲"))); // 首次生成 + 放弃后重生成
    }

    [Fact]
    public async Task NewProposal_ExpiresOldPendingCard()
    {
        var h = new Harness(BlueprintText, BlueprintText);
        var first = await h.StartToOutlineAsync();

        // 直接触发重生成（绕过 Reject，验证旧 Pending 卡自动过期）
        await h.Pipeline.RegenerateCurrentStageAsync("再想想", ProjectId);

        var second = h.Session.CurrentProposal!;
        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(ProposalStatus.Expired, first.Status);
        Assert.Equal(ProposalStatus.Pending, second.Status);
    }

    [Fact]
    public async Task RejectItem_MarksItemRejected()
    {
        var h = new Harness(BlueprintText, PlotLinesText);
        var outline = await h.StartToOutlineAsync();

        h.Session.RejectItem(outline.Id, outline.Items[0].Id);

        Assert.Equal(ProposalStatus.Rejected, outline.Items[0].Status);
        Assert.False(outline.Items[0].IsPending);
    }

    #endregion

    #region 章节关联处理

    private async Task<ChapterReferral> SeedReferralAsync(Harness h, string? content)
    {
        var volume = await h.Volumes.CreateVolumeAsync(new Volume { ProjectId = ProjectId, Title = "觉醒之卷", Order = 1 });
        var chapter = await h.Chapters.CreateChapterAsync(new Chapter
        {
            VolumeId = volume.Id, Title = "灵脉初醒", Summary = "主角在矿井中觉醒灵脉", Order = 1, Status = "Draft"
        });

        if (!string.IsNullOrEmpty(content))
        {
            await h.Chapters.UpdateChapterContentAsync(chapter.Id, content);
        }

        return new ChapterReferral
        {
            ProjectId = ProjectId,
            ProjectName = "星穹之下",
            VolumeId = volume.Id,
            VolumeTitle = volume.Title,
            VolumeOrder = volume.Order,
            ChapterId = chapter.Id,
            ChapterTitle = chapter.Title,
            ChapterOrder = chapter.Order,
            Summary = chapter.Summary
        };
    }

    [Fact]
    public async Task AttachChapterRef_SetsContextAndPostsInfo()
    {
        var h = new Harness();
        h.Session.OpenForProject(ProjectId, "星穹之下");
        var referral = await SeedReferralAsync(h, content: new string('原', 500));

        h.Session.AttachChapterRef(referral);

        Assert.Equal(referral, h.Session.ChapterRef);
        Assert.Contains(h.Session.Messages, m => m.Text.Contains("已关联《星穹之下》第1卷第1章《灵脉初醒》"));
        Assert.Contains(h.Session.Messages, m => m.Text.Contains("取消关联"));
    }

    [Fact]
    public async Task ChapterOp_WithAttachedChapter_ProducesCardAndPersistsTargetChapter()
    {
        var h = new Harness("润色后的正文内容");
        h.Session.OpenForProject(ProjectId, "星穹之下");
        var referral = await SeedReferralAsync(h, content: new string('原', 500));
        h.Session.AttachChapterRef(referral);

        await h.Session.SendUserMessageAsync("润色全文，强化战斗节奏");

        // 不走意图解析，直接生成处理卡
        var proposal = h.Session.CurrentProposal;
        Assert.NotNull(proposal);
        Assert.Equal(ProposalKind.ChapterContent, proposal!.Kind);
        Assert.Equal(referral.ChapterId, proposal.TargetChapterId);
        Assert.Single(h.Rwkv.ReceivedPrompts); // 关联模式零意图调用

        h.Session.AcceptItem(proposal.Id, proposal.Items[0].Id);
        await h.Session.AcceptProposalAsync(proposal.Id);

        // 直接更新目标章节正文（而非「首个空白章」），且不推进流水线
        var chapter = h.Uow.ChapterRepo.Chapters.Single(c => c.Id == referral.ChapterId);
        Assert.Equal("润色后的正文内容", chapter.Content);
        Assert.Equal("润色后的正文内容".Length, chapter.WordCount);
        Assert.Contains(h.Session.Messages, m => m.Text.Contains("正文已更新"));
        Assert.Equal(PipelineStage.NotStarted, h.Pipeline.CurrentStage);
    }

    [Fact]
    public async Task ChapterOp_DraftChapter_GeneratesAndPersistsContent()
    {
        var h = new Harness(new string('文', 2100));
        h.Session.OpenForProject(ProjectId, "星穹之下");
        var referral = await SeedReferralAsync(h, content: null); // 草稿章无正文
        h.Session.AttachChapterRef(referral);

        await h.Session.SendUserMessageAsync("把觉醒场面写得有压迫感");

        var proposal = h.Session.CurrentProposal;
        Assert.NotNull(proposal);
        Assert.Equal(referral.ChapterId, proposal!.TargetChapterId);

        h.Session.AcceptItem(proposal.Id, proposal.Items[0].Id);
        await h.Session.AcceptProposalAsync(proposal.Id);

        var chapter = h.Uow.ChapterRepo.Chapters.Single(c => c.Id == referral.ChapterId);
        Assert.True(chapter.Content!.Length >= 2000);
    }

    [Fact]
    public async Task ChapterQuestion_WithAttachedChapter_AnswersWithoutProposal()
    {
        var h = new Harness("节奏整体偏慢，建议开头直接进入冲突。");
        h.Session.OpenForProject(ProjectId, "星穹之下");
        var referral = await SeedReferralAsync(h, content: new string('原', 500));
        h.Session.AttachChapterRef(referral);

        await h.Session.SendUserMessageAsync("这章的节奏有问题吗");

        Assert.Null(h.Session.CurrentProposal); // 提问不产生确认卡
        Assert.Contains(h.Session.Messages, m => m.Text.Contains("节奏整体偏慢"));
        Assert.Contains("章节内容", h.Rwkv.ReceivedPrompts[^1]); // 提示词携带章节全文
    }

    [Fact]
    public async Task CancelRefCommand_ClearsContextAndRestoresNormalMode()
    {
        var h = new Harness();
        h.Session.OpenForProject(ProjectId, "星穹之下");
        var referral = await SeedReferralAsync(h, content: new string('原', 100));
        h.Session.AttachChapterRef(referral);

        await h.Session.SendUserMessageAsync("取消关联");

        Assert.Null(h.Session.ChapterRef);
        Assert.Contains(h.Session.Messages, m => m.Text.Contains("已取消章节关联"));

        // 解除后恢复正常助手模式（导航可用）
        await h.Session.SendUserMessageAsync("打开角色管理");
        Assert.Contains(h.Navigations, n => n.Target == NavigationTarget.CharacterManagement);
    }

    #endregion

    #region L1 → L5 端到端

    [Fact]
    public async Task FullPipeline_AcceptThroughAllStages_PersistsEveryLevel()
    {
        var h = new Harness(BlueprintText, PlotLinesText, VolumesText, ChapterDraftsText, LongChapterContent());
        await h.StartToOutlineAsync();

        await h.AcceptAllCurrentItemsAsync(); // L1 总纲 → L2
        await h.AcceptAllCurrentItemsAsync(); // L2 剧情线 → L3
        await h.AcceptAllCurrentItemsAsync(); // L3 分卷 → L4（指针定位最新卷）
        await h.AcceptAllCurrentItemsAsync(); // L4 章节草稿 → L5 正文
        await h.AcceptAllCurrentItemsAsync(); // L5 第1章正文 → 落库并推进到第2章

        // 各级落库验证
        Assert.Single(h.Uow.PlotRepo.Plots, p => p.Type == "总纲");
        Assert.Equal(3, h.Uow.PlotRepo.Plots.Count(p => p.Type is "主线" or "支线" or "暗线"));
        Assert.Equal(2, h.Uow.VolumeRepo.Volumes.Count);
        Assert.Equal(3, h.Uow.ChapterRepo.Chapters.Count);

        // L5 正文写入首个空白章节，状态置已完成
        var volume2 = h.Uow.VolumeRepo.Volumes.OrderByDescending(v => v.Order).First();
        var chapters = h.Uow.ChapterRepo.Chapters.Where(c => c.VolumeId == volume2.Id).OrderBy(c => c.Order).ToList();
        Assert.True(chapters[0].Content!.Length >= 2000);
        Assert.Equal("Completed", chapters[0].Status);
        Assert.Equal(chapters[0].Content!.Length, chapters[0].WordCount);
        Assert.Null(chapters[1].Content); // 第2章待写

        // 流水线推进到第 2 章并生成新正文确认卡
        Assert.Equal(2, h.Pipeline.GetStateSnapshot().CurrentChapterOrder);
        Assert.Equal(ProposalKind.ChapterContent, h.Session.CurrentProposal!.Kind);
    }

    #endregion
}
