using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NovelManagement.AI.Services.RWKV;
using NovelManagement.Application.Services;
using NovelManagement.Core.Entities;

namespace NovelManagement.WPF.Services
{
    /// <summary>
    /// 章节命名结果。
    /// </summary>
    public class ChapterNamingResult
    {
        /// <summary>章节实体。</summary>
        public Chapter Chapter { get; init; } = null!;

        /// <summary>原标题。</summary>
        public string OldTitle { get; init; } = string.Empty;

        /// <summary>新标题（不含「第X章」前缀）。</summary>
        public string NewTitle { get; init; } = string.Empty;

        /// <summary>是否成功。</summary>
        public bool Success { get; init; }

        /// <summary>失败原因（成功时为 null）。</summary>
        public string? Error { get; init; }
    }

    /// <summary>
    /// 章节命名 Agent：主动扫描卷宗管理中仍为默认名称（「第X章」/空白/未命名）的章节，
    /// 依据书名、类型、卷名与章节摘要/正文开头，调用 RWKV 推理生成符合本章主题的标题并回写数据库。
    /// 设计要点：
    /// 1. 主动触发：卷宗管理视图加载/导航时自动扫描（视图层调用 <see cref="ScanAndNameAsync"/>）；
    /// 2. 让路原则：批量生成任务运行中或推理服务未启动时静默跳过，不抢占 GPU、不自动拉起模型服务；
    /// 3. 串行低并发 + 单次扫描限量（50 章），避免长时间占用推理资源；
    /// 4. 生成失败时保持原标题不变，绝不覆盖用户已命名的章节。
    /// </summary>
    public class ChapterNamingAgent
    {
        /// <summary>单次扫描最多处理的章节数，防止长时间占用推理资源。</summary>
        public const int MaxChaptersPerScan = 50;

        private readonly ILogger<ChapterNamingAgent> _logger;
        private readonly IRwkvLightningService _rwkvService;
        private readonly ChapterService _chapterService;
        private readonly VolumeService _volumeService;
        private readonly ProjectService _projectService;
        private readonly IFullNovelBatchGenerationService _batchService;

        public ChapterNamingAgent(
            ILogger<ChapterNamingAgent> logger,
            IRwkvLightningService rwkvService,
            ChapterService chapterService,
            VolumeService volumeService,
            ProjectService projectService,
            IFullNovelBatchGenerationService batchService)
        {
            _logger = logger;
            _rwkvService = rwkvService;
            _chapterService = chapterService;
            _volumeService = volumeService;
            _projectService = projectService;
            _batchService = batchService;
        }

        /// <summary>
        /// 判断章节标题是否需要 AI 生成：标题为空、默认占位名或仅「第X章」无主题。
        /// </summary>
        public static bool NeedsNaming(Chapter? chapter)
        {
            var title = chapter?.Title?.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                return true;
            }

            return title is "未命名" or "未命名章节" or "新章节" or "新建章节"
                || Regex.IsMatch(title, @"^第\s*\d+\s*章$");
        }

        /// <summary>
        /// 扫描指定项目下所有需要命名的章节并逐个生成主题标题。
        /// </summary>
        /// <param name="projectId">项目标识。</param>
        /// <param name="onRenamed">单个章节命名成功后的回调（可能来自后台线程，用于 UI 即时更新）。</param>
        /// <param name="ct">取消令牌。</param>
        /// <returns>命名成功的章节结果列表。</returns>
        public async Task<List<ChapterNamingResult>> ScanAndNameAsync(
            Guid projectId,
            Action<Chapter, string, string>? onRenamed = null,
            CancellationToken ct = default)
        {
            var results = new List<ChapterNamingResult>();
            if (projectId == Guid.Empty)
            {
                return results;
            }

            if (_batchService.IsRunning)
            {
                _logger.LogInformation("批量生成任务运行中，章节命名 Agent 本次跳过（避免抢占推理资源）");
                return results;
            }

            if (!_rwkvService.IsAvailable)
            {
                _logger.LogInformation("推理服务未启动，章节命名 Agent 本次跳过（不自动拉起模型服务）");
                return results;
            }

            var project = await _projectService.GetProjectByIdAsync(projectId);
            var volumes = (await _volumeService.GetVolumeListAsync(projectId))
                .GroupBy(v => v.Id)
                .ToDictionary(g => g.Key, g => g.First());
            var allChapters = await _chapterService.GetChaptersByProjectIdAsync(projectId);

            // 已占用标题集合（用户已命名的章节 + 本次会话新生成的标题），用于避免跨章节重复
            var usedTitles = new HashSet<string>(
                allChapters
                    .Where(c => !NeedsNaming(c))
                    .Select(c => (c.Title ?? string.Empty).Trim())
                    .Where(t => t.Length > 0),
                StringComparer.Ordinal);

            var pendingChapters = allChapters
                .Where(NeedsNaming)
                .OrderBy(c => volumes.TryGetValue(c.VolumeId, out var v) ? v.Order : int.MaxValue)
                .ThenBy(c => c.Order)
                .Take(MaxChaptersPerScan)
                .ToList();

            if (pendingChapters.Count == 0)
            {
                return results;
            }

            _logger.LogInformation("章节命名 Agent 扫描到 {Count} 个待命名章节，开始生成", pendingChapters.Count);
            foreach (var chapter in pendingChapters)
            {
                ct.ThrowIfCancellationRequested();
                var result = await NameChapterAsync(chapter, volumes.GetValueOrDefault(chapter.VolumeId), project?.Name, project?.Type, usedTitles, ct);
                if (result.Success)
                {
                    results.Add(result);
                    try
                    {
                        onRenamed?.Invoke(chapter, result.OldTitle, result.NewTitle);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "章节命名回调执行失败（章节 {ChapterId}）", chapter.Id);
                    }
                }
            }

            _logger.LogInformation("章节命名 Agent 完成：成功 {Success} / 扫描 {Total}", results.Count, pendingChapters.Count);
            return results;
        }

        /// <summary>
        /// 为单个章节生成符合主题的标题并回写数据库（失败时保持原标题）。
        /// </summary>
        /// <param name="chapter">章节实体。</param>
        /// <param name="volume">所属卷宗（可为 null）。</param>
        /// <param name="bookName">书名。</param>
        /// <param name="genre">题材类型。</param>
        /// <param name="usedTitles">已占用标题集合；命中重复时带回避清单重试一次，成功后追加新标题。</param>
        /// <param name="ct">取消令牌。</param>
        public async Task<ChapterNamingResult> NameChapterAsync(
            Chapter chapter,
            Volume? volume,
            string? bookName,
            string? genre,
            HashSet<string>? usedTitles = null,
            CancellationToken ct = default)
        {
            var oldTitle = chapter.Title ?? string.Empty;
            try
            {
                var contentHead = Truncate(chapter.Content, 160);
                var brief = Truncate(string.IsNullOrWhiteSpace(chapter.Summary) ? contentHead : chapter.Summary, 80);
                var volumeText = string.IsNullOrWhiteSpace(volume?.Title) ? string.Empty : volume!.Title.Trim();

                var prompt =
                    "User: 书籍《" + (bookName ?? "未知") + "》（" + (string.IsNullOrWhiteSpace(genre) ? "类型未定" : genre) + "）" +
                    (volumeText.Length == 0 ? "" : volumeText) + "第" + chapter.Order + "章，章节梗概：" +
                    (brief.Length == 0 ? "（暂无）" : brief) + "\n" +
                    "正文开头：" + (contentHead.Length == 0 ? "（暂无）" : contentHead) + "\n" +
                    "请为本章起一个符合章节主题的标题（2-8个汉字，能概括本章核心事件，不要书名号、不要「第X章」字样、不要解释，只输出标题本身）。\n\nAssistant: <think></think\n";

                var response = await _rwkvService.CompleteAsync(prompt, 60);
                if (!response.Success)
                {
                    return new ChapterNamingResult { Chapter = chapter, OldTitle = oldTitle, Success = false, Error = response.Error ?? "推理调用失败" };
                }

                var title = CleanTitle(response.Text);
                if (usedTitles != null && !string.IsNullOrWhiteSpace(title) && usedTitles.Contains(title))
                {
                    // 撞已用标题：定向追加回避说明后重试一次（避免整表注入干扰模型输出）
                    var retryPrompt = prompt.Replace(
                        "只输出标题本身）。",
                        $"只输出标题本身）。注意：「{title}」已被其他章节使用，请换一个不同的标题。");
                    var retryResponse = await _rwkvService.CompleteAsync(retryPrompt, 60);
                    var retryTitle = retryResponse.Success ? CleanTitle(retryResponse.Text) : string.Empty;
                    if (!string.IsNullOrWhiteSpace(retryTitle) && !usedTitles.Contains(retryTitle))
                    {
                        title = retryTitle;
                    }
                }

                if (string.IsNullOrWhiteSpace(title))
                {
                    return new ChapterNamingResult { Chapter = chapter, OldTitle = oldTitle, Success = false, Error = "模型未返回有效标题" };
                }

                usedTitles?.Add(title);
                chapter.Title = title;
                await _chapterService.UpdateChapterAsync(chapter);
                _logger.LogInformation("章节命名：第{Order}章 「{Old}」→「{New}」", chapter.Order, oldTitle, title);
                return new ChapterNamingResult { Chapter = chapter, OldTitle = oldTitle, NewTitle = title, Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "第 {Order} 章标题生成失败，保持原标题", chapter.Order);
                return new ChapterNamingResult { Chapter = chapter, OldTitle = oldTitle, Success = false, Error = ex.Message };
            }
        }

        /// <summary>
        /// 清洗模型输出的标题：取首个非空行，去引导前缀/书名号/引号/各类括号装饰（含【】段）/
        /// 「第X章」字样，限长 16 字。
        /// </summary>
        private static string CleanTitle(string? raw)
        {
            var line = (raw ?? string.Empty)
                .Split('\n')
                .Select(l => l.Trim())
                .FirstOrDefault(l => l.Length > 0) ?? string.Empty;

            line = Regex.Replace(line, "^(标题|章节名|章名|章节标题)\\s*[:：]\\s*", "");
            line = Regex.Replace(line, "^第\\s*\\d+\\s*章\\s*[:：]?\\s*", "");
            // 剥离模型装饰性括号段（如【数据墓穴】【代码轮回】），避免残留结构标记
            line = Regex.Replace(line, "【[^】]*】", "");
            line = line.Trim('《', '》', '"', '\u201c', '\u201d', '\u300a', '\u300b', '“', '”', '「', '」', '【', '】', '-', '*', ' ', '，', '。', '·');
            if (line.Length > 16)
            {
                line = line[..16].Trim();
            }

            return line.Length >= 2 ? line : string.Empty;
        }

        /// <summary>截断文本（含 null 保护）。</summary>
        private static string Truncate(string? text, int maxChars)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var trimmed = text.Trim();
            return trimmed.Length <= maxChars ? trimmed : trimmed[..maxChars];
        }
    }
}
