using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NovelManagement.AI.Services.RWKV;
using NovelManagement.AI.Services.RWKV.Models;
using NovelManagement.AI.Utilities;
using NovelManagement.Application.DTOs;
using NovelManagement.Application.Interfaces;
using NovelManagement.Application.Services;
using NovelManagement.Core.Entities;
using NovelManagement.WPF.Models;

namespace NovelManagement.WPF.Services;

/// <summary>
/// 整本长篇批量生成结果。
/// </summary>
public class FullNovelBatchStartResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public bool Resumed { get; init; }
}

/// <summary>
/// 批量生成进度快照（供 UI 查询）。
/// </summary>
public class FullNovelBatchStatus
{
    public bool IsRunning { get; set; }
    public string Phase { get; set; } = "未开始";
    public string BookTitle { get; set; } = string.Empty;
    public Guid? ProjectId { get; set; }
    public int VolumeCount { get; set; }
    public int ChaptersPerVolume { get; set; }
    public int CurrentVolume { get; set; }
    public int CurrentChapter { get; set; }
    public int CompletedChapters { get; set; }
    public int FailedChapters { get; set; }
    public double? LastChapterScore { get; set; }
    public string? LastError { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string RecentMessage { get; set; } = string.Empty;
}

/// <summary>
/// 整本长篇批量生成服务（RWKV 本地推理）。
///
/// 规格：默认 3 卷 × 30 章 × 每章 ≥3000 字（卷数/章数/字数可经启动选项自定义，支持无限续写与快速切卷）；
/// 创作工艺：创意采样参数（temp 0.9 / top-p 0.85 / frequency 0.2 / presence 0.3，
/// 见 appsettings.json RWKV 节）+ DRY 抗复读采样（dry 0.8/1.75/2/2048，抑制长序列重复）+
/// Fake-think 提示（官方推荐 "Assistant: &lt;think&gt;&lt;/think"，跳过思考直出正文）；
/// 由于模型上下文仅 16K，章节正文采用“切片创作 + 拼接”：每章一个 RWKV state 会话，
/// 首片注入大纲/工艺/梗概，后续片仅发续写指令（state 增量续写，已实测验证），
/// 逐片拼接直到达到字数要求；每章生成后由 RWKV 按五维rubric评分并存入 Chapter.Notes；
/// 全部进度持久化在 Plots（Type=系统，Title=批量生成进度），应用重启后可断点续跑。
/// </summary>
public interface IFullNovelBatchGenerationService
{
    bool IsRunning { get; }

    FullNovelBatchStatus GetStatus();

    Task<FullNovelBatchStartResult> StartAsync(BatchGenerationOptions? options = null);

    void Cancel();
}

public class FullNovelBatchGenerationService : IFullNovelBatchGenerationService
{
    // ====== 批量规格（可被启动选项覆盖） ======
    private const int DefaultVolumeCount = 3;
    private const int DefaultChaptersPerVolume = 30;
    private const int DefaultChapterTargetWords = 3000;
    // ====== 上下文档位写作策略（32K 以下切片且小批量辅助信息；32K+ 整章直出并分档注入更多辅助信息） ======
    private ContextWritingPolicy _policy = ContextWritingPolicy.Resolve(16 * 1024);
    private int _sliceBudget = 1100;
    private const int OutlineMaxTokens = 2400;
    private const int ScoreMaxTokens = 320;

    private BatchGenerationOptions? _options;
    private int _chapterMinChars = DefaultChapterTargetWords;

    private const string StatePlotTitle = "批量生成进度";
    private const string StatePlotType = "系统";

    private readonly ILogger<FullNovelBatchGenerationService> _logger;
    private readonly IRwkvLightningService _rwkvService;
    private readonly InferenceRuntimeCoordinator _runtimeCoordinator;
    private readonly ProjectCatalogService _projectCatalogService;
    private readonly ProjectContextService _projectContextService;
    private readonly PlotService _plotService;
    private readonly VolumeService _volumeService;
    private readonly ChapterService _chapterService;
    private readonly ProjectArchiveService _archiveService;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;

    private readonly FullNovelBatchStatus _status = new();
    private CancellationTokenSource? _cts;
    private Task? _runningTask;
    private readonly object _lock = new();

    public FullNovelBatchGenerationService(
        ILogger<FullNovelBatchGenerationService> logger,
        IRwkvLightningService rwkvService,
        InferenceRuntimeCoordinator runtimeCoordinator,
        ProjectCatalogService projectCatalogService,
        ProjectContextService projectContextService,
        PlotService plotService,
        VolumeService volumeService,
        ChapterService chapterService,
        ProjectArchiveService archiveService,
        IConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _rwkvService = rwkvService;
        _runtimeCoordinator = runtimeCoordinator;
        _projectCatalogService = projectCatalogService;
        _projectContextService = projectContextService;
        _plotService = plotService;
        _volumeService = volumeService;
        _chapterService = chapterService;
        _archiveService = archiveService;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
    }

    public bool IsRunning
    {
        get { lock (_lock) { return _status.IsRunning; } }
    }

    public FullNovelBatchStatus GetStatus()
    {
        lock (_lock)
        {
            return new FullNovelBatchStatus
            {
                IsRunning = _status.IsRunning,
                Phase = _status.Phase,
                BookTitle = _status.BookTitle,
                ProjectId = _status.ProjectId,
                VolumeCount = _status.VolumeCount,
                ChaptersPerVolume = _status.ChaptersPerVolume,
                CurrentVolume = _status.CurrentVolume,
                CurrentChapter = _status.CurrentChapter,
                CompletedChapters = _status.CompletedChapters,
                FailedChapters = _status.FailedChapters,
                LastChapterScore = _status.LastChapterScore,
                LastError = _status.LastError,
                StartedAt = _status.StartedAt,
                FinishedAt = _status.FinishedAt,
                RecentMessage = _status.RecentMessage
            };
        }
    }

    public void Cancel()
    {
        _cts?.Cancel();
    }

    public Task<FullNovelBatchStartResult> StartAsync(BatchGenerationOptions? options = null)
    {
        lock (_lock)
        {
            if (_status.IsRunning)
            {
                return Task.FromResult(new FullNovelBatchStartResult
                {
                    Success = false,
                    Message = "批量生成任务已在运行中。"
                });
            }

            _options = options ?? new BatchGenerationOptions();
            _chapterMinChars = _options.ChapterTargetWords;
            _status.IsRunning = true;
            _status.LastError = null;
            _status.StartedAt = DateTime.Now;
            _status.FinishedAt = null;
            _cts = new CancellationTokenSource();
            _runningTask = Task.Run(() => RunAsync(_cts.Token));
            return Task.FromResult(new FullNovelBatchStartResult
            {
                Success = true,
                Message = "批量生成任务已启动（后台运行，可用「生成进度」按钮随时查看）。"
            });
        }
    }

    // ====================== 主流程 ======================

    private async Task RunAsync(CancellationToken ct)
    {
        try
        {
            await EnsureRwkvOnlineAsync(ct);

            var state = await LoadOrCreateStateAsync(_options, ct);

            // 应用启动选项（每卷章节数/卷数模式以本次启动选项为准）
            _chapterMinChars = _options.ChapterTargetWords;

            // 上下文档位写作策略：显式档位优先，否则按 ContextSize 自动归档
            var rwkvCfg = _configuration.GetSection("AI:Providers:RWKV");
            var cfgContextSize = int.TryParse(rwkvCfg["ContextSize"], out var ctxVal) ? ctxVal : 16 * 1024;
            _policy = ContextWritingPolicy.Resolve(cfgContextSize, rwkvCfg["WritingContextTier"]);
            // 不切片档（32K+）的整章 token 预算随自定义每章字数放大（中文约 2 token/字），上限 32K 留出提示词空间
            _sliceBudget = _policy.UseSlicing
                ? _policy.SliceMaxTokens
                : Math.Clamp(_options.ChapterTargetWords * 2, _policy.SliceMaxTokens, 32768);
            _logger.LogInformation("写作档位 {Tier}（上下文 {Ctx}）：{Mode}，单片预算 {Budget} tokens，最大片数 {Slices}，辅助信息配额 角色{Cast}/设定{Setting}/势力{Faction}",
                _policy.TierName, cfgContextSize,
                _policy.UseSlicing ? "切片生成" : "整章直出",
                _sliceBudget, _policy.MaxSlicesPerChapter,
                _policy.MaxCastCount, _policy.MaxSettingCount, _policy.MaxFactionCount);
            UpdateStatusMessage($"写作档位 {_policy.TierName}：{(_policy.UseSlicing ? $"切片生成（每片约{_sliceBudget} tokens，最多{_policy.MaxSlicesPerChapter}片）" : "整章直出")}；辅助信息配额 角色{_policy.MaxCastCount}/设定{_policy.MaxSettingCount}/势力{_policy.MaxFactionCount}");

            if (_options.UnlimitedMode)
            {
                state.VolumeCount = 0; // 0 = 无限续写
            }
            else if (state.VolumeCount <= 0)
            {
                state.VolumeCount = DefaultVolumeCount; // 此前为无限模式，恢复固定卷数
            }
            state.ChaptersPerVolume = _options.ChaptersPerVolume;
            await SaveStateAsync(state, ct);

            _status.VolumeCount = state.VolumeCount;
            _status.ChaptersPerVolume = state.ChaptersPerVolume;
            _status.BookTitle = state.Title;
            _status.ProjectId = state.ProjectId;

            // 主线大纲缺失则补齐
            if (string.IsNullOrWhiteSpace(state.MasterOutline))
            {
                SetPhase($"生成全书主线大纲（《{state.Title}》）");
                state.MasterOutline = await GenerateTextAsync(BuildMasterOutlinePrompt(state), OutlineMaxTokens, ct);
                await SaveStateAsync(state, ct);
            }

            // 前置数据：依据主线大纲生成主要角色 / 世界设定 / 势力（章节联动同步依赖这些实体存在）
            await EnsurePrerequisitesAsync(state, ct);

            // 本次试写章节数上限（0 = 不限制，生成整本）
            var capRaw = _configuration.GetSection("AI:Providers:RWKV")["BatchMaxChapters"];
            var maxChapters = int.TryParse(capRaw, out var capParsed) ? capParsed : 0;

            var stopForCap = false;
            for (var v = 0; !stopForCap; v++)
            {
                // 固定卷数模式：到达指定卷数即结束
                if (state.VolumeCount > 0 && v >= state.VolumeCount)
                {
                    break;
                }

                // 无限续写模式：懒创建新卷状态
                if (v >= state.Volumes.Count)
                {
                    if (state.VolumeCount > 0)
                    {
                        break; // 防御：非无限模式不应越界
                    }

                    state.Volumes.Add(new BatchVolumeState { Index = v + 1 });
                    _logger.LogInformation("无限续写模式：开启第 {Index} 卷", v + 1);
                }

                var vol = state.Volumes[v];
                _status.CurrentVolume = vol.Index;
                ct.ThrowIfCancellationRequested();

                // 本卷实际写作上限（快速切卷：当前进度再写 3 章即切新卷）
                var volumeChapterCap = _options.NextThreeChaptersThenNewVolume
                    ? Math.Min(state.ChaptersPerVolume, vol.NextChapter + 2)
                    : state.ChaptersPerVolume;
                var briefCount = Math.Min(volumeChapterCap, state.ChaptersPerVolume);

                // 卷大纲 + N 章梗概
                if (vol.Briefs.Count < briefCount)
                {
                    SetPhase($"生成第 {vol.Index}/{(state.VolumeCount > 0 ? state.VolumeCount.ToString() : "∞")} 卷大纲与 {briefCount} 章梗概");
                    vol.Briefs = await GenerateVolumeBriefsAsync(state, vol, briefCount, ct);
                    await SaveStateAsync(state, ct);
                }

                // 卷实体
                if (vol.VolumeId == Guid.Empty)
                {
                    SetPhase($"创建第 {vol.Index} 卷");
                    var volume = await _volumeService.CreateVolumeAsync(new Volume
                    {
                        Title = $"第{vol.Index}卷",
                        Description = $"《{state.Title}》第{vol.Index}卷（{vol.VolumeTheme}）",
                        Order = vol.Index,
                        Status = "Writing",
                        ProjectId = state.ProjectId
                    }, ct);
                    vol.VolumeId = volume.Id;
                    await SaveStateAsync(state, ct);
                }

                // 逐章生成（快速切卷时以 volumeChapterCap 为上限）
                var serviceRetries = 0;
                for (var i = vol.NextChapter; i <= volumeChapterCap && !stopForCap; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    _status.CurrentChapter = i;
                    SetPhase($"第 {vol.Index} 卷 第 {i}/{volumeChapterCap} 章创作中");

                    try
                    {
                        var score = await GenerateOneChapterAsync(state, vol, i, ct);
                        _status.CompletedChapters++;
                        _status.LastChapterScore = score;
                        vol.NextChapter = i + 1;
                        serviceRetries = 0;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception chapterEx) when (IsInferenceUnavailable(chapterEx) && serviceRetries < 20)
                    {
                        // 推理服务暂不可用（llama-server 启动中/重启/过载）：等待恢复后重试本章，不跳过不空洞
                        serviceRetries++;
                        _logger.LogWarning(chapterEx, "推理服务暂不可用（第 {Volume} 卷第 {Chapter} 章），第 {Retry}/20 次等待重试",
                            vol.Index, i, serviceRetries);
                        UpdateStatusMessage($"推理服务暂不可用，30 秒后重试第 {vol.Index} 卷第 {i} 章（{serviceRetries}/20）");
                        await Task.Delay(TimeSpan.FromSeconds(30), ct);
                        i--; // 重试本章
                    }
                    catch (Exception chapterEx)
                    {
                        _status.FailedChapters++;
                        vol.NextChapter = i + 1; // 跳过失败章节，保证任务继续
                        _logger.LogError(chapterEx, "第 {Volume} 卷第 {Chapter} 章生成失败", vol.Index, i);
                        UpdateStatusMessage($"第 {vol.Index} 卷第 {i} 章失败：{chapterEx.Message}，已跳过");
                    }

                    await SaveStateAsync(state, ct);

                    if (maxChapters > 0 && _status.CompletedChapters + _status.FailedChapters >= maxChapters)
                    {
                        stopForCap = true;
                        _logger.LogInformation("已达到试写章节数上限 {Cap}，任务停止（BatchMaxChapters 置 0 可继续生成全书）", maxChapters);
                    }
                }

                if (stopForCap)
                {
                    break;
                }

                // 卷收尾：统计字数
                var volumeEntity = await _volumeService.GetVolumeByIdAsync(vol.VolumeId, ct);
                if (volumeEntity != null)
                {
                    var chapters = await _chapterService.GetChapterListAsync(vol.VolumeId, ct);
                    volumeEntity.WordCount = chapters.Sum(c => c.WordCount);
                    volumeEntity.Status = "Completed";
                    await _volumeService.UpdateVolumeAsync(volumeEntity, ct);
                }
            }

            if (stopForCap)
            {
                // 试写模式：达到上限即停止，不标记 Finished，保留状态便于后续继续
                SetPhase("试写完成");
                UpdateStatusMessage($"试写完成：已生成 {_status.CompletedChapters} 章（失败 {_status.FailedChapters}）。可检查各管理模块的联动更新。");
                _logger.LogInformation("批量试写在达到上限后停止：《{Title}》", state.Title);
                return;
            }

            // 全书评分报告
            SetPhase("生成全书评分报告");
            await WriteScoreReportAsync(state, ct);

            state.Finished = true;
            await SaveStateAsync(state, ct);

            SetPhase("已完成");
            UpdateStatusMessage($"全书《{state.Title}》生成完毕：{_status.CompletedChapters} 章成功，{_status.FailedChapters} 章失败。");
            _logger.LogInformation("整本长篇批量生成完成：《{Title}》", state.Title);
        }
        catch (OperationCanceledException)
        {
            SetPhase("已取消");
            UpdateStatusMessage("批量生成已取消（进度已保存，可再次点击继续）。");
            _logger.LogInformation("整本长篇批量生成已取消");
        }
        catch (Exception ex)
        {
            lock (_lock)
            {
                _status.LastError = ex.Message;
                _status.Phase = "出错暂停";
                _status.RecentMessage = $"出错：{ex.Message}（进度已保存，可再次点击继续）";
            }
            _logger.LogError(ex, "整本长篇批量生成出错");
        }
        finally
        {
            lock (_lock)
            {
                _status.IsRunning = false;
                _status.FinishedAt = DateTime.Now;
            }
        }
    }

    // ====================== 状态持久化 ======================

    private sealed class BatchVolumeState
    {
        public int Index { get; set; }
        public string VolumeTheme { get; set; } = string.Empty;
        public string VolumeOutline { get; set; } = string.Empty;
        public List<string> Briefs { get; set; } = new();
        public Guid VolumeId { get; set; }
        public int NextChapter { get; set; } = 1;
    }

    private sealed class BatchState
    {
        public Guid ProjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public string Premise { get; set; } = string.Empty;
        public string MasterOutline { get; set; } = string.Empty;
        public int VolumeCount { get; set; } = DefaultVolumeCount;
        public int ChaptersPerVolume { get; set; } = DefaultChaptersPerVolume;
        public List<BatchVolumeState> Volumes { get; set; } = new();
        public bool Finished { get; set; }
        public List<string> CastNames { get; set; } = new();
        public List<string> SettingNames { get; set; } = new();
    }

    private async Task<BatchState> LoadOrCreateStateAsync(BatchGenerationOptions options, CancellationToken ct)
    {
        // 状态 Plot 挂在批量项目自身下（Plot.ProjectId 有外键约束，不能用 Guid.Empty），
        // 因此续跑时需扫描所有活跃项目
        var activeProjects = await _projectCatalogService.GetActiveProjectsAsync();
        foreach (var project in activeProjects)
        {
            var plots = await _plotService.GetPlotsByTypeAsync(project.ProjectId, StatePlotType, ct);
            var statePlot = plots.FirstOrDefault(p => p.Title == StatePlotTitle && p.Status != "已完成");
            if (statePlot != null && !string.IsNullOrWhiteSpace(statePlot.Outline))
            {
                try
                {
                    var state = JsonSerializer.Deserialize<BatchState>(statePlot.Outline);
                    if (state != null && !state.Finished)
                    {
                        _logger.LogInformation("恢复批量生成状态：项目 {ProjectId}，进度 第{Volume}卷第{Chapter}章",
                            state.ProjectId, state.Volumes.FirstOrDefault()?.Index ?? 0, state.Volumes.FirstOrDefault()?.NextChapter ?? 0);
                        UpdateStatusMessage($"检测到未完成任务，继续生成《{state.Title}》");
                        return state;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "批量生成状态反序列化失败，将开启新书");
                }
            }
        }

        // 新书：概念 → 项目
        SetPhase("构思新书概念");
        var concept = await GenerateConceptAsync(ct);

        SetPhase($"创建项目《{concept.Title}》");
        var projectName = await EnsureUniqueProjectNameAsync(concept.Title);
        var catalogItem = await _projectCatalogService.CreateProjectAsync(new Views.NewProjectDialog.NewProjectModel
        {
            Name = projectName,
            Description = concept.Premise,
            Type = concept.Genre,
            TargetWordCount = options.ChaptersPerVolume * options.ChapterTargetWords * DefaultVolumeCount,
            EnableAI = true,
            AutoSave = true,
            VersionControl = false,
            Template = "AI长篇批量生成"
        });
        _projectContextService.SetCurrentProject(catalogItem.ProjectId, catalogItem.Name);

        var newState = new BatchState
        {
            ProjectId = catalogItem.ProjectId,
            Title = catalogItem.Name,
            Genre = concept.Genre,
            Premise = concept.Premise
        };
        // 无限续写模式不预建卷（RunAsync 懒创建）；固定模式默认 3 卷
        var initialVolumeCount = options.UnlimitedMode ? 0 : DefaultVolumeCount;
        newState.VolumeCount = initialVolumeCount;
        newState.ChaptersPerVolume = options.ChaptersPerVolume;
        for (var v = 1; v <= initialVolumeCount; v++)
        {
            newState.Volumes.Add(new BatchVolumeState { Index = v });
        }

        // 女频文检测：类型/简介命中女频关键词时自动切换红粉花漾少女风皮肤
        ThemeManager.ApplyNovelGenre($"{newState.Genre} {newState.Premise}");
        await SaveStateAsync(newState, ct);
        return newState;
    }

    private async Task SaveStateAsync(BatchState state, CancellationToken ct)
    {
        // 状态 Plot 挂在批量项目自身下（Plot.ProjectId 有外键约束，不能用 Guid.Empty）
        var plots = await _plotService.GetPlotsByTypeAsync(state.ProjectId, StatePlotType, ct);
        var statePlot = plots.FirstOrDefault(p => p.Title == StatePlotTitle && !p.IsDeleted);
        var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = false });

        if (statePlot == null)
        {
            await _plotService.CreatePlotAsync(new Plot
            {
                ProjectId = state.ProjectId,
                Title = StatePlotTitle,
                Type = StatePlotType,
                Status = state.Finished ? "已完成" : "进行中",
                Outline = json,
                Description = "整本长篇批量生成任务状态（系统自动维护）"
            }, ct);
        }
        else
        {
            statePlot.Outline = json;
            statePlot.Status = state.Finished ? "已完成" : "进行中";
            await _plotService.UpdatePlotAsync(statePlot, ct);
        }
    }

    private async Task<string> EnsureUniqueProjectNameAsync(string baseName)
    {
        var projects = await _projectCatalogService.GetActiveProjectsAsync();
        var name = baseName;
        var n = 2;
        while (projects.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            name = $"{baseName} {n}";
            n++;
        }

        return name;
    }

    // ====================== RWKV 调用 ======================

    private async Task EnsureRwkvOnlineAsync(CancellationToken ct)
    {
        SetPhase("检查推理服务");
        if (await _rwkvService.TestConnectionAsync())
        {
            return;
        }

        var isLlama = string.Equals(
            _configuration.GetSection("AI:Providers:RWKV")["RuntimeFlavor"], "llamacpp", StringComparison.OrdinalIgnoreCase);
        UpdateStatusMessage(isLlama
            ? "llama.cpp 未在线，自动拉起 llama-server……"
            : "RWKV 未在线，尝试经 rwkv_launcher 自动拉起……");

        var result = isLlama
            ? await _runtimeCoordinator.StartLlamaAsync(BuildLlamaLaunchOptions())
            : await _runtimeCoordinator.StartRwkvAsync(BuildLaunchOptions());
        if (!result.Success && !await _rwkvService.TestConnectionAsync())
        {
            throw new InvalidOperationException($"推理服务不可用：{result.Message}");
        }
    }

    private LlamaRuntimeLaunchOptions BuildLlamaLaunchOptions()
    {
        var cfg = _configuration.GetSection("AI:Providers:RWKV");
        return new LlamaRuntimeLaunchOptions
        {
            ExecutablePath = cfg["ServerScriptPath"] ?? string.Empty,
            BaseUrl = cfg["BaseUrl"] ?? "http://localhost:8000",
            ModelPath = cfg["ModelPath"] ?? string.Empty,
            ApiKey = cfg["Password"] ?? string.Empty,
            ContextSize = int.TryParse(cfg["ContextSize"], out var ctx) ? ctx : 16384,
            GpuLayers = 99,
            ParallelSlots = 1,
            EnableFlashAttention = false,   // RWKV 无 KV 缓存，禁用避免干扰
            CacheTypeK = string.Empty,
            CacheTypeV = string.Empty,
            EnableCpuMoE = false,
            EnableAutoFit = false,
            DisableWarmup = true,
            StartupTimeoutSeconds = 240
        };
    }

    private RwkvRuntimeLaunchOptions BuildLaunchOptions()
    {
        var cfg = _configuration.GetSection("AI:Providers:RWKV");
        return new RwkvRuntimeLaunchOptions
        {
            ExecutablePath = cfg["ServerScriptPath"] ?? string.Empty,
            BaseUrl = cfg["BaseUrl"] ?? "http://localhost:8000",
            ModelPath = cfg["ModelPath"] ?? string.Empty,
            ModelName = cfg["ModelName"] ?? "rwkv7-g1i",
            Strategy = cfg["Strategy"] ?? "cuda fp16",
            VocabPath = cfg["VocabPath"] ?? string.Empty,
            Password = cfg["Password"] ?? string.Empty,
            RuntimeFlavor = cfg["RuntimeFlavor"] ?? "cuda",
            PrefillChunkSize = 128,
            StartupTimeoutSeconds = 240
        };
    }

    private async Task<string> GenerateTextAsync(string prompt, int maxTokens, CancellationToken ct, int retries = 2)
    {
        for (var attempt = 1; attempt <= retries + 1; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            var response = await _rwkvService.CompleteAsync(prompt, maxTokens);
            if (response.Success && !string.IsNullOrWhiteSpace(response.Text))
            {
                var cleaned = CleanGenerated(response.Text);
                if (!string.IsNullOrWhiteSpace(cleaned))
                {
                    return cleaned;
                }
            }

            _logger.LogWarning("RWKV 调用失败（第 {Attempt} 次）：{Error}", attempt, response.Error);
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
        }

        throw new InvalidOperationException("RWKV 多次调用均失败");
    }

    private async Task<string> GenerateSliceAsync(string sessionId, string prompt, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var response = await _rwkvService.CompleteWithStateAsync(sessionId, prompt, _sliceBudget);
        if (!response.Success || string.IsNullOrWhiteSpace(response.Text))
        {
            throw new InvalidOperationException(response.Error ?? "RWKV state 切片返回空");
        }

        return CleanSlice(response.Text);
    }

    // ====================== 概念 / 大纲 ======================

    private sealed record BookConcept(string Title, string Genre, string Premise);

    private async Task<BookConcept> GenerateConceptAsync(CancellationToken ct)
    {
        var prompt =
            "User: 请为一批量生成的爆款中文网文构思一本新书，输出严格JSON（不要其他内容）：\n" +
            "{\"title\":\"四字以内书名\",\"genre\":\"类型（如：东方玄幻/都市异能/科幻末日）\",\"premise\":\"50字以内高概念简介，含金手指与核心冲突\"}\n" +
            "要求：题材新颖、冲突强烈、有爽点潜力。\n\nAssistant: <think></think\n";
        var raw = await GenerateTextAsync(prompt, 220, ct);
        var json = ExtractJson(raw);
        if (json != null)
        {
            var title = GetJsonProperty(json.Value, "title");
            var genre = GetJsonProperty(json.Value, "genre");
            var premise = GetJsonProperty(json.Value, "premise");
            if (!string.IsNullOrWhiteSpace(title))
            {
                return new BookConcept(title.Trim(), string.IsNullOrWhiteSpace(genre) ? "东方玄幻" : genre.Trim(), premise?.Trim() ?? string.Empty);
            }
        }

        // 兜底：从文本中提取《》
        var match = Regex.Match(raw, "《(.+?)》");
        var fallbackTitle = match.Success ? match.Groups[1].Value : $"批量新书 {DateTime.Now:MMdd-HHmm}";
        return new BookConcept(fallbackTitle, "东方玄幻", raw.Length > 120 ? raw[..120] : raw);
    }

    private string BuildMasterOutlinePrompt(BatchState state)
    {
        return
            "User: 你是顶级中文网文架构师。请为书籍《" + state.Title + "》（" + state.Genre + "）设计全书大纲，" +
            "共 " + state.VolumeCount + " 卷（每卷 " + state.ChaptersPerVolume + " 章）。\n" +
            "本书简介：" + state.Premise + "\n\n" +
            "要求：\n" +
            "1. 每卷一个阶段主题、一个阶段大反派、一个卷末大高潮；\n" +
            "2. 主角实力/地位逐卷跃升，冲突逐卷升级；\n" +
            "3. 输出纯文本，逐卷分节（第1卷/第2卷/第3卷），每卷 6-10 行，不要解释。\n\n" +
            "Assistant: <think></think\n";
    }

    private async Task<List<string>> GenerateVolumeBriefsAsync(BatchState state, BatchVolumeState vol, int briefCount, CancellationToken ct)
    {
        // 卷主题与卷大纲
        vol.VolumeOutline = await GenerateTextAsync(BuildVolumeOutlinePrompt(state, vol, briefCount), OutlineMaxTokens, ct);
        await SaveStateAsync(state, ct);

        // 章节梗概（可多轮补齐到 briefCount 条）
        var briefs = ParseBriefs(vol.VolumeOutline, briefCount);
        var extraRounds = 0;
        while (briefs.Count < briefCount && extraRounds < 3)
        {
            extraRounds++;
            var prompt =
                "User: 书籍《" + state.Title + "》第" + vol.Index + "卷大纲如下：\n" + vol.VolumeOutline + "\n\n" +
                "已有第1-" + briefs.Count + "章梗概。请从第" + (briefs.Count + 1) + "章开始，继续输出到第" + briefCount + "章的章节梗概，" +
                "每行格式严格为「第X章：15-30字梗概」，只输出这些行。\n\nAssistant: <think></think\n";
            var more = await GenerateTextAsync(prompt, OutlineMaxTokens, ct);
            briefs.AddRange(ParseBriefs(more, briefCount));
        }

        // 仍不足则用占位梗概补齐，保证推进
        while (briefs.Count < briefCount)
        {
            briefs.Add("主角遭遇新的挑战与冲突，实力与局面进一步推进，章末留下悬念。");
        }

        var result = briefs.Take(briefCount).ToList();
        vol.Briefs = result;
        return result;
    }

    private string BuildVolumeOutlinePrompt(BatchState state, BatchVolumeState vol, int briefCount)
    {
        var recap = BuildPreviousVolumesRecap(state, vol.Index, 1200);
        return
            "User: 你是顶级中文网文架构师。以下是《" + state.Title + "》（" + state.Genre + "）全书大纲：\n" +
            state.MasterOutline + "\n\n" +
            (string.IsNullOrEmpty(recap) ? string.Empty : "【前情提要（前" + (vol.Index - 1) + "卷已写章节梗概，跨卷剧情必须承接）】\n" + recap + "\n\n") +
            "请为第" + vol.Index + "卷设计详细大纲，输出两部分：\n" +
            "第一部分：本卷主题（1行）与卷末大高潮（1-2行）；\n" +
            "第二部分：逐章梗概，从「第1章」到「第" + briefCount + "章」，每行格式严格为「第X章：15-30字梗概」，" +
            "按爆款节奏排布：第1-3章密集冲突与金手指展开，每章一个爽点，每10章一个小高潮，卷末大高潮收束。\n" +
            "只输出以上内容，不要解释。\n\nAssistant: <think></think\n";
    }

    /// <summary>
    /// 构建前几卷的章节梗概回顾（跨卷记忆注入源）。
    /// </summary>
    private static string BuildPreviousVolumesRecap(BatchState state, int currentVolumeIndex, int maxChars)
    {
        if (currentVolumeIndex <= 1)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        foreach (var prev in state.Volumes.Where(v => v.Index < currentVolumeIndex).OrderBy(v => v.Index))
        {
            if (prev.Briefs.Count == 0)
            {
                continue;
            }

            sb.Append("第").Append(prev.Index).Append("卷（").Append(prev.VolumeTheme).Append("）：");
            for (var i = 0; i < prev.Briefs.Count; i++)
            {
                var brief = prev.Briefs[i];
                if (string.IsNullOrWhiteSpace(brief))
                {
                    continue;
                }

                sb.Append("第").Append(i + 1).Append("章：").Append(Truncate(brief, 30)).Append("；");
            }

            sb.Append('\n');
        }

        return Truncate(sb.ToString(), maxChars);
    }

    private static List<string> ParseBriefs(string text, int maxBriefs)
    {
        var briefs = new List<string>();
        if (string.IsNullOrWhiteSpace(text))
        {
            return briefs;
        }

        foreach (var line in text.Split('\n'))
        {
            var m = Regex.Match(line.Trim(), "^第?(\\d+)[章、.：:\\s]\\s*(.+)$");
            if (m.Success)
            {
                var idx = int.Parse(m.Groups[1].Value);
                var brief = m.Groups[2].Value.Trim();
                // 按索引对齐放置
                while (briefs.Count < idx - 1 && briefs.Count < maxBriefs)
                {
                    briefs.Add(string.Empty);
                }

                if (idx - 1 < briefs.Count)
                {
                    if (string.IsNullOrWhiteSpace(briefs[idx - 1]))
                    {
                        briefs[idx - 1] = brief;
                    }
                }
                else
                {
                    briefs.Add(brief);
                }
            }
        }

        return briefs.Where(b => !string.IsNullOrWhiteSpace(b)).ToList();
    }

    // ====================== 单章：切片创作 + 拼接 + 评分 ======================

    /// <summary>
    /// 判断异常是否为推理服务暂不可用（连接拒绝/503/超时类），此类失败应等待重试而非跳章。
    /// </summary>
    private static bool IsInferenceUnavailable(Exception ex)
    {
        if (ex is System.Net.Http.HttpRequestException)
        {
            return true;
        }

        var msg = ex.Message;
        return msg.Contains("503")
            || msg.Contains("Service Unavailable")
            || msg.Contains("Connection refused")
            || msg.Contains("socket")
            || msg.Contains("无法连接");
    }

    /// <summary>
    /// 依据章节梗概（辅以正文开头）生成符合本章主题的章节名，格式「第N章 XXX」。
    /// 短请求低成本；解析失败时回退「第N章」。
    /// </summary>
    private async Task<string> GenerateChapterTitleAsync(BatchState state, BatchVolumeState vol, int chapterIndex, string brief, string content, CancellationToken ct)
    {
        try
        {
            var contentHead = Truncate(content, 160);
            var prompt =
                "User: 书籍《" + state.Title + "》第" + vol.Index + "卷第" + chapterIndex + "章，章节梗概：" + Truncate(brief, 80) + "\n" +
                "正文开头：" + contentHead + "\n" +
                "请为本章起一个符合章节主题的标题（2-8个汉字，能概括本章核心事件，不要书名号、不要「第X章」字样、不要解释，只输出标题本身）。\n\nAssistant: <think></think\n";
            var raw = await GenerateTextAsync(prompt, 60, ct, retries: 1);
            var title = CleanGeneratedTitle(raw);
            if (string.IsNullOrWhiteSpace(title))
            {
                return $"第{chapterIndex}章";
            }

            return $"第{chapterIndex}章 {title}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "第 {Volume} 卷第 {Chapter} 章标题生成失败，使用默认标题", vol.Index, chapterIndex);
            return $"第{chapterIndex}章";
        }
    }

    /// <summary>
    /// 清洗模型输出的章节名：去书名号/引号/序号前缀/多余引导语，取首行并限长。
    /// </summary>
    private static string CleanGeneratedTitle(string raw)
    {
        var line = (raw ?? string.Empty)
            .Split('\n')
            .Select(l => l.Trim())
            .FirstOrDefault(l => l.Length > 0) ?? string.Empty;

        // 去掉「标题：」「第X章：」等前缀与书名号/引号
        line = Regex.Replace(line, "^(标题|章节名|章名|章节标题)\\s*[:：]\\s*", "");
        line = Regex.Replace(line, "^第\\s*\\d+\\s*章\\s*[:：]?\\s*", "");
        line = line.Trim('《', '》', '"', '\u201c', '\u201d', '\u300a', '\u300b', '“', '”', '「', '」', '-', '*', ' ', '，', '。');
        if (line.Length > 16)
        {
            line = line[..16].Trim();
        }

        // 只保留单行有效内容，剔除模型解释性长句
        return line.Length is >= 2 ? line : string.Empty;
    }

    private async Task<double> GenerateOneChapterAsync(BatchState state, BatchVolumeState vol, int chapterIndex, CancellationToken ct)
    {
        var brief = vol.Briefs[chapterIndex - 1];
        var isGolden = vol.Index == 1 && chapterIndex <= 3;

        // 上一章结尾（跨卷衔接也纳入）
        var prevTail = await GetPreviousChapterTailAsync(state, vol, chapterIndex, ct);

        var firstPrompt = BuildChapterFirstSlicePrompt(state, vol, chapterIndex, brief, prevTail, isGolden);

        var continuationPrompt =
            "\n\nUser: （继续输出本章正文后续内容：直接从上文停笔处续写，不要重复已有文字，不要总结，不要小标题，保持叙事连贯，约1000字。）\n\nAssistant: <think></think\n";

        var antiRepeatPrompt =
            "\n\nUser: （注意：上一次输出与已写内容重复了。请从上文停笔处继续，推进全新的情节：新的冲突、对话或场景转换，严禁重复任何已写文字，不要总结收尾，约1000字。）\n\nAssistant: <think></think\n";

        // 每次尝试使用一次性 state 会话（服务端会话跨运行持久，确定性 ID 会带入旧内容导致污染），
        // 字数不达标时整章重试，取最长结果
        List<string> parts = new();
        var fullContent = string.Empty;

        for (var attempt = 1; attempt <= 2; attempt++)
        {
            var session = $"batch-{state.ProjectId:N}-v{vol.Index}-c{chapterIndex}-{Guid.NewGuid():N}";
            var content = await GenerateSliceAsync(session, firstPrompt, ct);
            parts = new List<string> { content };

            var slices = 1;
            var duplicateRetries = 0;
            var sessionRollovers = 0;
            while (TotalChars(parts) < _chapterMinChars && slices < _policy.MaxSlicesPerChapter && sessionRollovers < 4)
            {
                ct.ThrowIfCancellationRequested();
                var slice = await GenerateSliceAsync(session, duplicateRetries > 0 ? antiRepeatPrompt : continuationPrompt, ct);
                // 防复读：与上一片重复时先换提示词重试一次；仍复读则滚动新会话携带已写尾部继续
                if (IsDuplicateSlice(parts[^1], slice))
                {
                    if (duplicateRetries == 0)
                    {
                        duplicateRetries++;
                        _logger.LogWarning("第 {Volume} 卷第 {Chapter} 章检测到切片复读，换反复读提示词重试", vol.Index, chapterIndex);
                        continue;
                    }

                    duplicateRetries = 0;
                    if (sessionRollovers >= 4)
                    {
                        _logger.LogWarning("第 {Volume} 卷第 {Chapter} 章滚动 {Count} 次仍复读，提前结束拼接", vol.Index, chapterIndex, sessionRollovers);
                        break;
                    }

                    sessionRollovers++;
                    var rolledSession = $"{session}-r{sessionRollovers}";
                    var rolled = await GenerateSliceAsync(rolledSession, BuildRollOverPrompt(state, vol, chapterIndex, brief, parts, isGolden), ct);
                    session = rolledSession;
                    if (IsDuplicateSlice(parts[^1], rolled))
                    {
                        _logger.LogWarning("第 {Volume} 卷第 {Chapter} 章滚动会话后仍复读，提前结束拼接", vol.Index, chapterIndex);
                        break;
                    }

                    parts.Add(rolled);
                    slices++;
                    UpdateStatusMessage($"第 {vol.Index} 卷第 {chapterIndex} 章：已滚动会话 {sessionRollovers} 次，拼接 {slices} 片 {TotalChars(parts)} 字");
                    continue;
                }

                duplicateRetries = 0;
                parts.Add(slice);
                slices++;
                UpdateStatusMessage($"第 {vol.Index} 卷第 {chapterIndex} 章：已拼接 {slices} 片，{TotalChars(parts)} 字");
            }

            fullContent = Stitch(parts);
            if (fullContent.Length >= _chapterMinChars)
            {
                break;
            }

            if (attempt < 2)
            {
                _logger.LogWarning("第 {Volume} 卷第 {Chapter} 章仅 {Length} 字（第 {Attempt} 次尝试），换新会话整章重试",
                    vol.Index, chapterIndex, fullContent.Length, attempt);
                UpdateStatusMessage($"第 {vol.Index} 卷第 {chapterIndex} 章仅 {fullContent.Length} 字，整章重试");
            }
        }

        var ok = fullContent.Length >= _chapterMinChars;

        // 评分
        var (score, comment) = await ScoreChapterAsync(fullContent, ct);

        // 依据章节梗概/正文生成符合本章主题的章节名（失败回退「第N章」）
        var chapterTitle = await GenerateChapterTitleAsync(state, vol, chapterIndex, brief, fullContent, ct);

        var chapter = await _chapterService.CreateChapterAsync(new Chapter
        {
            Title = chapterTitle,
            Content = fullContent,
            Summary = brief,
            Order = chapterIndex,
            Status = ok ? "Completed" : "Draft",
            Type = "正文",
            VolumeId = vol.VolumeId,
            WordCount = fullContent.Length,
            Notes = JsonSerializer.Serialize(new ChapterScore
            {
                Plot = score.Plot,
                Character = score.Character,
                Prose = score.Prose,
                Hook = score.Hook,
                Pacing = score.Pacing,
                Total = score.Total,
                Comment = comment
            })
        }, ct);

        await _archiveService.WriteCleanContentAsync(
            state.ProjectId,
            "GenerateChapterContent",
            fullContent,
            $"第{vol.Index}卷-第{chapterIndex}章",
            null,
            ct);

        // 章节联动更新：角色出场/历史、势力、剧情进度、世界设定、人物/势力关系、时间线
        await SyncChapterModulesAsync(chapter, ct);

        UpdateStatusMessage($"第 {vol.Index} 卷第 {chapterIndex} 章完成：{fullContent.Length} 字，评分 {score.Total:F1}（{comment}）");
        return score.Total;
    }

    /// <summary>
    /// 章节落库后的模块联动同步（失败不阻断批量任务）。
    /// </summary>
    private async Task SyncChapterModulesAsync(Chapter chapter, CancellationToken ct)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var workflow = scope.ServiceProvider.GetService<ChapterUpdateWorkflowService>();
            if (workflow == null)
            {
                _logger.LogWarning("ChapterUpdateWorkflowService 不可用，跳过章节联动");
                return;
            }

            var result = await workflow.RunAsync(chapter, null, ct);
            var s = result.SyncResult;
            _logger.LogInformation(
                "章节联动完成：角色 {C} 势力 {F} 剧情 {P} 设定 {W} 人物关系 {CR} 势力关系 {FR} 时间线 {T}",
                s?.UpdatedCharacterCount ?? 0,
                s?.UpdatedFactionCount ?? 0,
                s?.UpdatedPlotCount ?? 0,
                s?.UpdatedWorldSettingCount ?? 0,
                s?.UpdatedCharacterRelationshipCount ?? 0,
                s?.UpdatedFactionRelationshipCount ?? 0,
                result.UpdatedTimelineEventCount);
            UpdateStatusMessage(
                $"联动更新：角色 {s?.UpdatedCharacterCount ?? 0}、势力 {s?.UpdatedFactionCount ?? 0}、剧情 {s?.UpdatedPlotCount ?? 0}、设定 {s?.UpdatedWorldSettingCount ?? 0}、人物关系 {s?.UpdatedCharacterRelationshipCount ?? 0}、时间线 {result.UpdatedTimelineEventCount}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "章节联动更新失败（不阻断批量任务）");
        }
    }

    // ====================== 前置数据：角色 / 世界设定 / 势力 ======================

    /// <summary>
    /// 确保项目存在主要角色 / 世界设定 / 势力实体（依据主线大纲生成）。
    /// 章节联动同步按实体名做文本匹配，实体缺失时同步为空操作，因此必须先生成。
    /// </summary>
    private async Task EnsurePrerequisitesAsync(BatchState state, CancellationToken ct)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var characterService = scope.ServiceProvider.GetService<CharacterService>();
            var worldSettingService = scope.ServiceProvider.GetService<IWorldSettingService>();
            var factionService = scope.ServiceProvider.GetService<FactionService>();

            // 修炼体系：无体系时由 AI 自上而下设计自定义等级体系（角色修为等级与正文上下文均引用）
            try
            {
                var prerequisiteService = scope.ServiceProvider.GetService<PrerequisiteGenerationService>();
                if (prerequisiteService != null)
                {
                    await prerequisiteService.EnsureCultivationSystemAsync(
                        state.ProjectId, new PrerequisiteGenerationResult { ProjectId = state.ProjectId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "批量任务修炼体系生成失败（不阻断章节生成）");
            }

            // 角色
            var characters = characterService != null
                ? (await characterService.GetCharactersByProjectIdAsync(state.ProjectId, ct)).ToList()
                : new List<Character>();
            if (characters.Count == 0 && characterService != null)
            {
                SetPhase("依据主线大纲生成主要角色");
                var generated = await GenerateCastAsync(state, ct);
                foreach (var character in generated)
                {
                    try
                    {
                        await characterService.CreateCharacterAsync(character, ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "角色 {Name} 落库失败", character.Name);
                    }
                }

                characters = (await characterService.GetCharactersByProjectIdAsync(state.ProjectId, ct)).ToList();
                UpdateStatusMessage($"已生成主要角色 {characters.Count} 名：{string.Join("、", characters.Select(c => c.Name))}");
            }

            // 外貌主动补全：存量角色缺失外貌特征时批量补全（不阻断）
            if (characterService != null)
            {
                await BackfillCharacterAppearanceAsync(characterService, characters, state, ct);
            }

            state.CastNames = characters
                .Select(c => c.Name)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct()
                .ToList();

            // 世界设定
            if (worldSettingService != null)
            {
                var settings = (await worldSettingService.GetAllAsync(state.ProjectId, ct)).ToList();
                if (settings.Count == 0)
                {
                    SetPhase("依据主线大纲生成世界设定");
                    var generatedSettings = await GenerateWorldSettingsAsync(state, ct);
                    foreach (var dto in generatedSettings)
                    {
                        try
                        {
                            await worldSettingService.CreateAsync(dto, ct);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "世界设定 {Name} 落库失败", dto.Name);
                        }
                    }

                    UpdateStatusMessage($"已生成世界设定 {generatedSettings.Count} 条");
                }

                settings = (await worldSettingService.GetAllAsync(state.ProjectId, ct)).ToList();
                state.SettingNames = settings
                    .Select(s => s.Name)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct()
                    .ToList();
            }

            // 势力
            if (factionService != null)
            {
                var factions = (await factionService.GetFactionsByProjectIdAsync(state.ProjectId, ct)).ToList();
                if (factions.Count == 0)
                {
                    SetPhase("依据主线大纲生成势力组织");
                    foreach (var faction in await GenerateFactionsAsync(state, ct))
                    {
                        try
                        {
                            await factionService.CreateFactionAsync(faction, ct);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "势力 {Name} 落库失败", faction.Name);
                        }
                    }
                }
            }

            await SaveStateAsync(state, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "批量前置数据生成失败（不阻断章节生成）");
        }
    }

    /// <summary>
    /// 用 RWKV 依据主线大纲生成主要角色。
    /// 2.9B 小模型经常无视行内格式，改为「行内管道格式 + 字段标签多行格式」双解析、多次重试累积。
    /// </summary>
    private async Task<List<Character>> GenerateCastAsync(BatchState state, CancellationToken ct)
    {
        // 注意：不粘贴大纲正文——小模型看到长大纲会倾向续写大纲而非执行指令；
        // 只给书名/类型/高概念短输入（概念提示词验证过可被遵循）；数量按上下文档位配额
        // 外貌特征随角色一并向模型索取（6 行格式），缺失时由 BackfillCharacterAppearanceAsync 兜底补全
        var castCount = _policy.MaxCastCount;
        var promptA =
            "User: 为《" + state.Title + "》（" + state.Genre + "）设计" + castCount + "名主要角色。\n" +
            "高概念：" + state.Premise + "\n" +
            "输出格式（每名角色6行，共" + castCount * 6 + "行）：\n" +
            "名字：中文姓名\n类型：主角/女主角/师父/对手/挚友 之一\n性别：男或女\n人设：一句话性格\n外貌：一句话外貌特征（身形/发色/气质等）\n背景：一句话出身\n" +
            "只输出这" + castCount * 6 + "行，不要大纲，不要解释。\n\nAssistant: <think></think\n";
        var promptB =
            "User: 任务是角色设计，禁止续写大纲。\n" +
            "为《" + state.Title + "》（" + state.Genre + "）设计" + castCount + "名主要角色，高概念：" + state.Premise + "\n" +
            "严格按以下格式输出" + castCount * 6 + "行后立即停止：\n" +
            "名字：X\n类型：X\n性别：X\n人设：X\n外貌：X\n背景：X\n" +
            "（以上6行×" + castCount + "名角色）\n\nAssistant: <think></think\n";

        var result = new List<Character>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lastRaw = string.Empty;
        for (var attempt = 1; attempt <= 3 && result.Count < castCount; attempt++)
        {
            var text = await GenerateTextAsync(attempt == 1 ? promptA : promptB, Math.Max(1100, castCount * 260), ct);
            lastRaw = text;
            foreach (var fields in ParseLabeledRecords(text, new[] { "名字", "类型", "性别", "人设", "外貌", "背景" }))
            {
                // fields: 名字/类型/性别/人设/外貌/背景
                if (!seen.Add(fields[0]))
                {
                    continue;
                }

                result.Add(new Character
                {
                    Id = Guid.NewGuid(),
                    ProjectId = state.ProjectId,
                    Name = fields[0],
                    Type = fields[1],
                    Gender = fields[2].Contains("女") ? "女" : "男",
                    Personality = fields[3],
                    Appearance = NormalizeGeneratedField(fields[4]),
                    Background = fields[5],
                    Importance = result.Count == 0 ? 10 : 5,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        _logger.LogInformation("主线大纲角色解析出 {Count} 名角色", result.Count);
        if (result.Count == 0)
        {
            _logger.LogInformation("角色解析为0，模型原始输出：{Raw}", Truncate(lastRaw, 400));
        }

        // 修为等级：从项目自定义修炼体系按角色次序取低阶值（主角最低，逐个抬升）
        if (result.Count > 0)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var prerequisiteService = scope.ServiceProvider.GetService<PrerequisiteGenerationService>();
                var levelNames = prerequisiteService != null
                    ? await prerequisiteService.GetProjectCultivationLevelNamesAsync(state.ProjectId)
                    : new List<string>();
                if (levelNames.Count > 0)
                {
                    for (var i = 0; i < result.Count; i++)
                    {
                        result[i].CultivationLevel = levelNames[Math.Min(i, levelNames.Count - 1)];
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "角色修为等级赋值失败（不阻断批量生成）");
            }
        }

        return result;
    }

    /// <summary>
    /// 归一模型输出字段：占位符（未提供）视为空，交由补全逻辑处理。
    /// </summary>
    private static string NormalizeGeneratedField(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed == "（未提供）" || trimmed == "(未提供)")
        {
            return string.Empty;
        }

        return trimmed;
    }

    /// <summary>
    /// 主动补全角色外貌特征：为外貌为空的存量角色发起一次 AI 批量补全并落库。
    /// 失败不阻断批量任务。
    /// </summary>
    private async Task BackfillCharacterAppearanceAsync(CharacterService characterService, List<Character> characters, BatchState state, CancellationToken ct)
    {
        var pending = characters
            .Where(c => string.IsNullOrWhiteSpace(c.Appearance))
            .Take(12)
            .ToList();
        if (pending.Count == 0)
        {
            return;
        }

        try
        {
            var prompt =
                "User: 为书籍《" + state.Title + "》（" + state.Genre + "）的下列角色各写一句外貌特征（20字内，含身形/发色/气质）：\n" +
                string.Join("\n", pending.Select(c => c.Name + "：" + (string.IsNullOrWhiteSpace(c.Personality) ? (c.Background ?? "") : c.Personality))) + "\n" +
                "输出格式（每人1行，共" + pending.Count + "行）：\n名字：外貌描述\n只输出这些行。\n\nAssistant: <think></think\n";

            var text = await GenerateTextAsync(prompt, Math.Max(300, pending.Count * 60), ct, retries: 1);
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in text.Split('\n'))
            {
                var idx = line.IndexOf('：');
                if (idx <= 0)
                {
                    idx = line.IndexOf(':');
                }

                if (idx <= 0 || idx >= line.Length - 1)
                {
                    continue;
                }

                var name = line[..idx].Trim().TrimStart('-', '*', ' ');
                var appearance = NormalizeGeneratedField(line[(idx + 1)..]);
                if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(appearance))
                {
                    map.TryAdd(name, appearance);
                }
            }

            var updated = 0;
            foreach (var character in pending)
            {
                if (!map.TryGetValue(character.Name, out var appearance))
                {
                    continue;
                }

                character.Appearance = appearance;
                character.UpdatedAt = DateTime.UtcNow;
                try
                {
                    await characterService.UpdateCharacterAsync(character, ct);
                    updated++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "角色 {Name} 外貌补全落库失败", character.Name);
                }
            }

            if (updated > 0)
            {
                _logger.LogInformation("已为 {Count} 名角色补全外貌特征", updated);
                UpdateStatusMessage($"已为 {updated} 名角色补全外貌特征");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "角色外貌批量补全失败（不阻断批量任务）");
        }
    }

    /// <summary>
    /// 用 RWKV 依据主线大纲生成世界设定（双格式解析 + 重试）。
    /// </summary>
    private async Task<List<CreateWorldSettingDto>> GenerateWorldSettingsAsync(BatchState state, CancellationToken ct)
    {
        // 短输入：只给书名/类型/高概念，避免模型续写大纲；数量按上下文档位配额
        var settingCount = _policy.MaxSettingCount;
        var promptA =
            "User: 为《" + state.Title + "》（" + state.Genre + "）设计" + settingCount + "条核心世界设定（力量体系/地理/特殊规则）。\n" +
            "高概念：" + state.Premise + "\n" +
            "输出格式（每条设定3行，共" + settingCount * 3 + "行）：\n" +
            "设定名：X\n分类：力量体系/地理/规则 之一\n内容：一句话描述\n" +
            "只输出这" + settingCount * 3 + "行，不要大纲，不要解释。\n\nAssistant: <think></think\n";
        var promptB =
            "User: 任务是世界设定设计，禁止续写大纲。\n" +
            "为《" + state.Title + "》（" + state.Genre + "）设计" + settingCount + "条核心世界设定，高概念：" + state.Premise + "\n" +
            "严格按以下格式输出" + settingCount * 3 + "行后立即停止：\n" +
            "设定名：X\n分类：X\n内容：X\n" +
            "（以上3行×" + settingCount + "条设定）\n\nAssistant: <think></think\n";

        var result = new List<CreateWorldSettingDto>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lastRaw = string.Empty;
        for (var attempt = 1; attempt <= 3 && result.Count < settingCount; attempt++)
        {
            var text = await GenerateTextAsync(attempt == 1 ? promptA : promptB, Math.Max(700, settingCount * 250), ct);
            lastRaw = text;
            foreach (var fields in ParseLabeledRecords(text, new[] { "设定名", "分类", "内容" }))
            {
                // fields: 设定名/分类/内容
                if (!seen.Add(fields[0]))
                {
                    continue;
                }

                result.Add(new CreateWorldSettingDto
                {
                    Name = fields[0],
                    Type = "核心设定",
                    Category = fields[1],
                    Content = fields[2],
                    Description = fields[2],
                    Importance = 8,
                    ProjectId = state.ProjectId
                });
            }
        }

        _logger.LogInformation("主线大纲解析出 {Count} 条世界设定", result.Count);
        if (result.Count == 0)
        {
            _logger.LogInformation("世界设定解析为0，模型原始输出：{Raw}", Truncate(lastRaw, 400));
        }
        return result;
    }

    /// <summary>
    /// 用 RWKV 依据主线大纲生成势力（双格式解析 + 重试）。
    /// </summary>
    private async Task<List<Faction>> GenerateFactionsAsync(BatchState state, CancellationToken ct)
    {
        // 短输入：只给书名/类型/高概念，避免模型续写大纲；数量按上下文档位配额
        var factionCount = _policy.MaxFactionCount;
        var promptA =
            "User: 为《" + state.Title + "》（" + state.Genre + "）设计" + factionCount + "个重要势力/组织。\n" +
            "高概念：" + state.Premise + "\n" +
            "输出格式（每个势力2行，共" + factionCount * 2 + "行）：\n" +
            "势力名：X\n描述：一句话简介\n" +
            "只输出这" + factionCount * 2 + "行，不要大纲，不要解释。\n\nAssistant: <think></think\n";
        var promptB =
            "User: 任务是势力设计，禁止续写大纲。\n" +
            "为《" + state.Title + "》（" + state.Genre + "）设计" + factionCount + "个重要势力，高概念：" + state.Premise + "\n" +
            "严格按以下格式输出" + factionCount * 2 + "行后立即停止：\n" +
            "势力名：X\n描述：X\n" +
            "（以上2行×" + factionCount + "个势力）\n\nAssistant: <think></think\n";

        var result = new List<Faction>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lastRaw = string.Empty;
        for (var attempt = 1; attempt <= 3 && result.Count < factionCount; attempt++)
        {
            var text = await GenerateTextAsync(attempt == 1 ? promptA : promptB, Math.Max(500, factionCount * 160), ct);
            lastRaw = text;
            foreach (var fields in ParseLabeledRecords(text, new[] { "势力名", "描述" }))
            {
                // fields: 势力名/描述
                if (!seen.Add(fields[0]))
                {
                    continue;
                }

                result.Add(new Faction
                {
                    Id = Guid.NewGuid(),
                    ProjectId = state.ProjectId,
                    Name = fields[0],
                    Type = "组织",
                    Description = fields[1],
                    Importance = 6,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        _logger.LogInformation("主线大纲解析出 {Count} 个势力", result.Count);
        if (result.Count == 0)
        {
            _logger.LogInformation("势力解析为0，模型原始输出：{Raw}", Truncate(lastRaw, 400));
        }
        return result;
    }

    /// <summary>
    /// 通用实体解析：兼容小模型常见输出（管道行内格式 / 字段标签多行格式 / Markdown 标题混入）。
    /// 按「已知标签名」映射取值，未知标签行（如 #### 2. 主角：xxx）直接忽略，避免污染字段。
    /// </summary>
    private static List<List<string>> ParseLabeledRecords(string text, string[] labels)
    {
        var records = new List<List<string>>();
        if (string.IsNullOrWhiteSpace(text))
        {
            return records;
        }

        // 标签别名归一（仅本次调用传入的标签参与映射）
        var aliasMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var aliasPool = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["名字"] = new[] { "角色名", "姓名", "名称" },
            ["类型"] = new[] { "角色类型", "定位" },
            ["人设"] = new[] { "一句话人设", "性格", "一句话性格", "人设性格" },
            ["外貌"] = new[] { "外貌特征", "外观", "形象", "外形" },
            ["背景"] = new[] { "一句话背景", "背景介绍" },
            ["设定名"] = new[] { "名称", "设定名称" },
            ["分类"] = new[] { "类别" },
            ["内容"] = new[] { "描述", "一句话内容", "一句话描述", "说明" },
            ["势力名"] = new[] { "名称", "组织名", "势力名称" },
            ["描述"] = new[] { "简介", "一句话简介", "势力描述", "介绍" }
        };
        foreach (var label in labels)
        {
            aliasMap[label] = label;
            if (aliasPool.TryGetValue(label, out var als))
            {
                foreach (var alias in als)
                {
                    aliasMap.TryAdd(alias, label);
                }
            }
        }

        string? Canonical(string raw)
        {
            var l = raw.Trim().TrimEnd('：', ':', '　').Trim();
            return aliasMap.TryGetValue(l, out var v) ? v : null;
        }

        Dictionary<string, string>? current = null;
        void Flush()
        {
            if (current is { Count: > 0 })
            {
                records.Add(labels.Select(l => current.TryGetValue(l, out var v) ? v : "（未提供）").ToList());
            }
            current = null;
        }

        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.Trim().TrimStart('#').Trim();                       // 去 Markdown 标题
            line = Regex.Replace(line, "^\\d+\\s*[.、)）]\\s*", "");              // 去行号
            line = line.TrimStart('-', '*', '，', ',', '　', ' ').Trim();         // 去列表符号
            line = Regex.Replace(line, "\\*+", "");                               // 去 Markdown 加粗
            if (line.Length < 2)
            {
                continue;
            }

            // 行内管道格式：整行即一条记录
            if (line.Contains('|'))
            {
                Flush();
                var fields = line.Split('|').Select(f => f.Trim()).Where(f => f.Length > 0).ToList();
                records.Add(labels.Select((_, i) => i < fields.Count ? fields[i] : "（未提供）").ToList());
                continue;
            }

            // 字段标签格式「标签：值」
            var m = Regex.Match(line, "^([^：:=]{1,8})[：:=]\\s*(.+)$");
            if (m.Success && Canonical(m.Groups[1].Value) is { } canonical)
            {
                var value = m.Groups[2].Value.Trim();
                if (canonical == labels[0])
                {
                    Flush();
                    current = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { [canonical] = value };
                }
                else if (current != null && !current.ContainsKey(canonical))
                {
                    current[canonical] = value;
                }
                continue;
            }

            // 未知标签行与纯文本行一律忽略（小模型会输出标题/说明等杂行）
        }

        Flush();
        return records.Where(r => r[0].Length >= 2 && r[0].Length <= 12 && !Regex.IsMatch(r[0], "[a-zA-Z0-9（）()《》【】「」]")).ToList();
    }

    private async Task<string> GetPreviousChapterTailAsync(BatchState state, BatchVolumeState vol, int chapterIndex, CancellationToken ct)
    {
        try
        {
            if (chapterIndex > 1)
            {
                var chapters = await _chapterService.GetChapterListAsync(vol.VolumeId, ct);
                var prev = chapters.OrderByDescending(c => c.Order).FirstOrDefault();
                return Tail(prev?.Content, 260);
            }

            if (vol.Index > 1)
            {
                var prevVol = state.Volumes.First(x => x.Index == vol.Index - 1);
                if (prevVol.VolumeId != Guid.Empty)
                {
                    var chapters = await _chapterService.GetChapterListAsync(prevVol.VolumeId, ct);
                    var prev = chapters.OrderByDescending(c => c.Order).FirstOrDefault();
                    return Tail(prev?.Content, 260);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取上一章结尾失败（忽略）");
        }

        return "（本章为起始章或上一章缺失，直接开场）";
    }

    private string BuildChapterFirstSlicePrompt(
        BatchState state, BatchVolumeState vol, int chapterIndex, string brief, string prevTail, bool isGolden)
    {
        var sb = new StringBuilder();
        sb.Append("User: 你是顶级爆款中文网文写手，正在创作《").Append(state.Title).Append("》（").Append(state.Genre).Append("）。\n");
        sb.Append("本书简介：").Append(state.Premise).Append('\n');
        if (state.CastNames.Count > 0)
        {
            var cast = string.Join("；", state.CastNames.Take(_policy.MaxCastCount));
            sb.Append("\n【主要角色】").Append(cast).Append("（正文中必须自然使用这些角色名字）\n");
        }
        if (state.SettingNames.Count > 0)
        {
            var settingNames = string.Join("；", state.SettingNames.Take(_policy.MaxSettingCount));
            sb.Append("\n【核心设定】").Append(settingNames).Append('\n');
        }
        sb.Append("\n【全书大纲】\n").Append(Truncate(state.MasterOutline, _policy.OutlineTruncateChars)).Append('\n');
        // 跨卷记忆：第 2 卷起注入前几卷章节梗概回顾（8K 档 RecapTruncateChars=0 不注入）
        var recap = BuildPreviousVolumesRecap(state, vol.Index, _policy.RecapTruncateChars);
        if (!string.IsNullOrEmpty(recap))
        {
            sb.Append("\n【前情提要（前").Append(vol.Index - 1).Append("卷剧情，正文需自然承接）】\n").Append(recap).Append('\n');
        }
        sb.Append("\n【本卷大纲】\n").Append(Truncate(vol.VolumeOutline, 1100)).Append('\n');
        sb.Append("\n【本章梗概】第").Append(chapterIndex).Append("章：").Append(brief).Append('\n');
        sb.Append("\n【上一章结尾】\n").Append(prevTail).Append('\n');
        sb.Append("\n【爆款工艺】\n").Append(BuildCraftRules(isGolden, vol.Index, chapterIndex));
        sb.Append("\n【本章要求】先输出本章正文第一部分（约1000字）：直接从场景与冲突切入，");
        sb.Append(isGolden ? "这是全书黄金三章之一，务必快速立住金手指与主角目标；" : "开头直接承接上一章结尾；");
        sb.Append("严禁复述或改写任何大纲、梗概、工艺内容，严禁输出【】包裹的标题结构，只输出书籍正文本身，不要章节标题，不要任何解释或备注。\n\nAssistant: <think></think\n");
        return sb.ToString();
    }

    /// <summary>
    /// 构建滚动会话提示词：新会话无法复用服务端 state，改为携带已写正文尾部续写。
    /// </summary>
    private string BuildRollOverPrompt(
        BatchState state, BatchVolumeState vol, int chapterIndex, string brief, List<string> parts, bool isGolden)
    {
        var writtenTail = Tail(Stitch(parts), 2000);
        var sb = new StringBuilder();
        sb.Append("User: 你是顶级爆款中文网文写手，正在创作《").Append(state.Title).Append("》（").Append(state.Genre).Append("）第").Append(chapterIndex).Append("章。\n");
        sb.Append("【本章梗概】").Append(brief).Append('\n');
        sb.Append("\n【已写正文结尾】\n").Append(writtenTail).Append('\n');
        sb.Append("\n【要求】紧接着上面的内容继续写本章正文后续（约1000字）：直接从停笔处推进新冲突、新对话或场景转换，");
        sb.Append("禁止重复已写文字，禁止总结，禁止小标题，保持叙事连贯与爽点节奏。\n\nAssistant: <think></think\n");
        return sb.ToString();
    }

    private static string BuildCraftRules(bool isGolden, int volumeIndex, int chapterIndex)
    {
        var rules = new StringBuilder();
        rules.Append("1. 开篇即冲突：直接进入场景与冲突，禁止景物堆砌式开场；\n");
        rules.Append("2. 爽点节奏：每章至少一个爽点（打脸/升级/反转/获宝/扬名）；\n");
        rules.Append("3. 章末强钩子：结尾必须落在悬念、危机或重大反转上；\n");
        rules.Append("4. 对话推动：多用短对话与动作推进，单段不超过3行，禁止大段说明文；\n");
        rules.Append("5. 人物立体：主角目标明确，反派有智商，配角有记忆点。");
        if (isGolden)
        {
            rules.Append($"\n6. 黄金三章（当前第{chapterIndex}章）：第1章金手指觉醒+首个小冲突；第2章实力小提升+新地图；第3章首次打脸小高潮。");
        }

        if (chapterIndex % 10 == 0)
        {
            rules.Append($"\n6. 本章为本卷第{chapterIndex}章小高潮：冲突集中爆发，情绪拉满。");
        }

        return rules.ToString();
    }

    private sealed record ScoreResult(double Plot, double Character, double Prose, double Hook, double Pacing, double Total);

    private sealed class ChapterScore
    {
        public double Plot { get; set; }
        public double Character { get; set; }
        public double Prose { get; set; }
        public double Hook { get; set; }
        public double Pacing { get; set; }
        public double Total { get; set; }
        public string Comment { get; set; } = string.Empty;
    }

    private async Task<(ScoreResult Score, string Comment)> ScoreChapterAsync(string content, CancellationToken ct)
    {
        try
        {
            var prompt =
                "User: 请对以下网文章节从五个维度打分（1-10整数）：plot(情节推进)、character(人物塑造)、prose(文笔流畅)、hook(章末钩子)、pacing(节奏爽点)。\n" +
                "输出严格JSON：{\"plot\":x,\"character\":x,\"prose\":x,\"hook\":x,\"pacing\":x,\"total\":x,\"comment\":\"一句话点评\"}，只输出JSON。\n" +
                "\n【章节正文】\n" + Truncate(content, 3800) + "\n\nAssistant: <think></think\n";

            var raw = await GenerateTextAsync(prompt, ScoreMaxTokens, ct, retries: 1);
            var json = ExtractJson(raw);
            if (json != null)
            {
                double Get(string key)
                {
                    var value = GetJsonProperty(json.Value, key);
                    return double.TryParse(value, out var v) ? Math.Clamp(v, 1, 10) : 6;
                }

                var plot = Get("plot");
                var character = Get("character");
                var prose = Get("prose");
                var hook = Get("hook");
                var pacing = Get("pacing");
                // 模型可能把 total 输出为五维之和（如 35 被钳成 10），统一用五维均值重算
                var total = (plot + character + prose + hook + pacing) / 5;

                var comment = GetJsonProperty(json.Value, "comment") ?? string.Empty;
                if (string.IsNullOrWhiteSpace(comment))
                {
                    comment = $"情节{plot:F0} 人物{character:F0} 文笔{prose:F0} 钩子{hook:F0} 节奏{pacing:F0}";
                }

                return (new ScoreResult(plot, character, prose, hook, pacing, total), comment);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "章节评分失败（不影响正文）");
        }

        return (new ScoreResult(6, 6, 6, 6, 6, 6), "评分失败，默认分");
    }

    private async Task WriteScoreReportAsync(BatchState state, CancellationToken ct)
    {
        var report = new StringBuilder();
        report.AppendLine($"# 《{state.Title}》整本生成评分报告");
        report.AppendLine();
        report.AppendLine($"- 类型：{state.Genre}");
        report.AppendLine($"- 简介：{state.Premise}");
        report.AppendLine($"- 规格：{(state.VolumeCount > 0 ? state.VolumeCount.ToString() + " 卷" : "无限续写")} × {state.ChaptersPerVolume} 章 × 每章 ≥{_chapterMinChars} 字");
        report.AppendLine($"- 生成时间：{DateTime.Now:yyyy-MM-dd HH:mm}");
        report.AppendLine();

        double bookTotal = 0;
        int bookCount = 0;
        foreach (var vol in state.Volumes)
        {
            report.AppendLine($"## 第{vol.Index}卷");
            report.AppendLine();
            var chapters = vol.VolumeId == Guid.Empty
                ? Enumerable.Empty<Chapter>()
                : await _chapterService.GetChapterListAsync(vol.VolumeId, ct);

            double volTotal = 0;
            var volCount = 0;
            foreach (var chapter in chapters.OrderBy(c => c.Order))
            {
                var score = ParseScore(chapter.Notes);
                report.AppendLine($"- 第{chapter.Order}章（{chapter.WordCount} 字）：总分 {score.Total:F1} ｜ 情节 {score.Plot:F0} 人物 {score.Character:F0} 文笔 {score.Prose:F0} 钩子 {score.Hook:F0} 节奏 {score.Pacing:F0} ｜ {score.Comment}");
                volTotal += score.Total;
                volCount++;
            }

            if (volCount > 0)
            {
                report.AppendLine();
                report.AppendLine($"**第{vol.Index}卷均分：{volTotal / volCount:F2}（{volCount} 章）**");
                bookTotal += volTotal;
                bookCount += volCount;
            }

            report.AppendLine();
        }

        if (bookCount > 0)
        {
            report.AppendLine($"## 全书均分：{bookTotal / bookCount:F2}");
        }

        await _archiveService.WriteCleanContentAsync(state.ProjectId, "BookScoreReport", report.ToString(), $"《{state.Title}》评分报告", null, ct);
    }

    private static (double Plot, double Character, double Prose, double Hook, double Pacing, double Total, string Comment) ParseScore(string? notes)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(notes))
            {
                var score = JsonSerializer.Deserialize<ChapterScore>(notes);
                if (score != null)
                {
                    return (score.Plot, score.Character, score.Prose, score.Hook, score.Pacing, score.Total, score.Comment);
                }
            }
        }
        catch
        {
            // 忽略解析失败
        }

        return (6, 6, 6, 6, 6, 6, "无评分");
    }

    // ====================== 文本工具 ======================

    private static string CleanGenerated(string text)
    {
        var cleaned = AIOutputSanitizer.ExtractCleanOutput(text) ?? string.Empty;
        return cleaned.Trim();
    }

    private static string CleanSlice(string text)
    {
        var cleaned = AIOutputSanitizer.ExtractCleanOutput(text) ?? string.Empty;
        cleaned = cleaned.Trim();

        // 去除模型偶发的 markdown 标题/分隔符
        cleaned = Regex.Replace(cleaned, "^#+\\s.*$", string.Empty, RegexOptions.Multiline);
        cleaned = cleaned.Replace("---", string.Empty);
        cleaned = Regex.Replace(cleaned, "（?未完待续）?", string.Empty);

        // 去除客套前缀
        var firstLineEnd = cleaned.IndexOf('\n');
        if (firstLineEnd > 0)
        {
            var firstLine = cleaned[..firstLineEnd].Trim();
            if (firstLine.Length <= 60 &&
                Regex.IsMatch(firstLine, "^(好的|当然|没问题|明白了|收到|以下是|遵照)"))
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

        // 特定的提示词结构标题（不包含正文里常见的【系统提示】【叮！】等）
        if (Regex.IsMatch(line, "^【(全书大纲|本卷大纲|本章梗概|爆款工艺|本章要求|上一章结尾|已写正文结尾)】"))
        {
            return true;
        }

        // 大纲条目的章节标题行
        if (Regex.IsMatch(line, "^第[0-9一二三四五六七八九十百]{1,4}章[:：·]"))
        {
            return true;
        }

        // markdown 列表/加粗/标题
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

        // 取前 60 字符对比，若上一片尾部与下一片头部相同则视为复读
        var head = b.Length >= 60 ? b[..60] : b;
        return a.EndsWith(head, StringComparison.Ordinal);
    }

    private static string Truncate(string? text, int maxChars) =>
        string.IsNullOrEmpty(text) ? string.Empty : text.Length <= maxChars ? text : text[..maxChars] + "……";

    private static string Tail(string? text, int maxChars)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "（缺失）";
        }

        return text.Length <= maxChars ? text : "……" + text[^maxChars..];
    }

    private static JsonElement? ExtractJson(string text)
    {
        try
        {
            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');
            if (start >= 0 && end > start)
            {
                using var doc = JsonDocument.Parse(text[start..(end + 1)]);
                return doc.RootElement.Clone();
            }
        }
        catch
        {
            // 忽略
        }

        return null;
    }

    /// <summary>
    /// 读取 JSON 对象指定属性的字符串形式（属性名忽略大小写；不存在返回 null）。
    /// </summary>
    private static string? GetJsonProperty(JsonElement element, string name)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString()
                    : property.Value.GetRawText();
            }
        }

        return null;
    }

    private void SetPhase(string phase)
    {
        lock (_lock)
        {
            _status.Phase = phase;
        }

        _logger.LogInformation("批量生成阶段：{Phase}", phase);
    }

    private void UpdateStatusMessage(string message)
    {
        lock (_lock)
        {
            _status.RecentMessage = message;
        }

        _logger.LogInformation("批量生成进度：{Message}", message);
    }
}
