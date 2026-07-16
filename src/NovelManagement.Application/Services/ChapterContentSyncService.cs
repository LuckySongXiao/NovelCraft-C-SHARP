using Microsoft.Extensions.Logging;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;

namespace NovelManagement.Application.Services;

/// <summary>
/// 章节内容同步服务。
/// 在章节保存后，将章节上下文同步到人物、势力、剧情与世界设定管理数据中。
/// </summary>
public class ChapterContentSyncService
{
    private static readonly char[] ListSeparators = [',', '，', ';', '；', '\n', '\r', '|', '、'];

    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ChapterContentSyncService> _logger;

    public ChapterContentSyncService(IUnitOfWork unitOfWork, ILogger<ChapterContentSyncService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// 根据章节内容同步相关管理数据。
    /// </summary>
    public async Task<ChapterContentSyncResult> SyncChapterAsync(
        Chapter chapter,
        string? relatedCharactersText = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chapter);

        var volume = await _unitOfWork.Volumes.GetByIdAsync(chapter.VolumeId, cancellationToken);
        if (volume == null)
        {
            throw new InvalidOperationException($"未找到章节所属卷宗，VolumeId={chapter.VolumeId}");
        }

        var projectId = volume.ProjectId;
        var chapters = (await _unitOfWork.Chapters.GetByProjectIdAsync(projectId, cancellationToken)).ToList();
        if (chapters.All(existing => existing.Id != chapter.Id))
        {
            chapter.Volume = volume;
            chapters.Add(chapter);
        }

        var chapterOrderLookup = chapters
            .OrderBy(existing => existing.Volume?.Order ?? int.MaxValue)
            .ThenBy(existing => existing.Order)
            .Select((existing, index) => new { existing.Id, Index = index })
            .ToDictionary(item => item.Id, item => item.Index);

        var context = BuildChapterContext(chapter, relatedCharactersText);
        var characterHints = ParseList(relatedCharactersText);
        var summaryText = BuildSummaryText(chapter);
        var chapterMarker = BuildChapterMarker(chapter);

        var matchedCharacters = await SyncCharactersAsync(
            projectId,
            chapter,
            chapterOrderLookup,
            context,
            characterHints,
            summaryText,
            chapterMarker,
            cancellationToken);

        var matchedFactions = await SyncFactionsAsync(
            projectId,
            context,
            matchedCharacters,
            summaryText,
            chapterMarker,
            cancellationToken);

        var matchedPlots = await SyncPlotsAsync(
            projectId,
            chapter,
            chapters,
            chapterOrderLookup,
            context,
            cancellationToken);

        var matchedSettings = await SyncWorldSettingsAsync(
            projectId,
            context,
            summaryText,
            chapterMarker,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "章节上下文同步完成 chapter={ChapterId} characters={CharacterCount} factions={FactionCount} plots={PlotCount} settings={SettingCount}",
            chapter.Id,
            matchedCharacters.Count,
            matchedFactions.Count,
            matchedPlots.Count,
            matchedSettings.Count);

        return new ChapterContentSyncResult
        {
            UpdatedCharacterCount = matchedCharacters.Count,
            UpdatedCharacterNames = matchedCharacters
                .Select(character => character.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            UpdatedFactionCount = matchedFactions.Count,
            UpdatedFactionNames = matchedFactions
                .Select(faction => faction.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            UpdatedPlotCount = matchedPlots.Count,
            UpdatedPlotTitles = matchedPlots
                .Select(plot => plot.Title)
                .Where(title => !string.IsNullOrWhiteSpace(title))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            UpdatedWorldSettingCount = matchedSettings.Count,
            UpdatedWorldSettingNames = matchedSettings
                .Select(setting => setting.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
    }

    private async Task<List<Character>> SyncCharactersAsync(
        Guid projectId,
        Chapter chapter,
        IReadOnlyDictionary<Guid, int> chapterOrderLookup,
        string context,
        HashSet<string> characterHints,
        string summaryText,
        string chapterMarker,
        CancellationToken cancellationToken)
    {
        var characters = (await _unitOfWork.Characters.GetByProjectIdAsync(projectId, cancellationToken)).ToList();
        var matchedCharacters = characters
            .Where(character => IsCharacterMatched(character, context, characterHints))
            .ToList();

        foreach (var character in matchedCharacters)
        {
            UpdateAppearanceRange(character, chapter, chapterOrderLookup);
            character.History = AppendUniqueEntry(character.History, $"{chapterMarker}：{summaryText}");
            character.KeyEvents = AppendUniqueEntry(character.KeyEvents, $"{chapterMarker}：{summaryText}");
            character.UpdatedAt = DateTime.UtcNow;

            var existingEvents = (await _unitOfWork.CharacterEvents.GetEventsByCharacterIdAsync(character.Id, cancellationToken)).ToList();
            var existingEvent = existingEvents.FirstOrDefault(evt => evt.ChapterId == chapter.Id);
            if (existingEvent == null)
            {
                var newEvent = new CharacterEvent
                {
                    Id = Guid.NewGuid(),
                    CharacterId = character.Id,
                    ChapterId = chapter.Id,
                    Title = chapterMarker,
                    Description = summaryText,
                    EventType = "章节更新",
                    StoryTime = chapterMarker,
                    Order = existingEvents.Count,
                    Tags = chapter.Tags,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.CharacterEvents.AddAsync(newEvent, cancellationToken);
            }
            else
            {
                existingEvent.Title = chapterMarker;
                existingEvent.Description = summaryText;
                existingEvent.StoryTime = chapterMarker;
                existingEvent.Tags = chapter.Tags;
                existingEvent.UpdatedAt = DateTime.UtcNow;
            }
        }

        return matchedCharacters;
    }

    private async Task<List<Faction>> SyncFactionsAsync(
        Guid projectId,
        string context,
        IReadOnlyCollection<Character> matchedCharacters,
        string summaryText,
        string chapterMarker,
        CancellationToken cancellationToken)
    {
        var factions = (await _unitOfWork.Factions.GetByProjectIdAsync(projectId, cancellationToken)).ToList();
        var factionIdsFromCharacters = matchedCharacters
            .Where(character => character.FactionId.HasValue)
            .Select(character => character.FactionId!.Value)
            .ToHashSet();

        var matchedFactions = factions
            .Where(faction => factionIdsFromCharacters.Contains(faction.Id) || ContainsValue(context, faction.Name))
            .ToList();

        foreach (var faction in matchedFactions)
        {
            var members = (await _unitOfWork.Characters.GetByFactionIdAsync(faction.Id, cancellationToken)).ToList();
            faction.MemberCount = members.Count;
            faction.History = AppendUniqueEntry(faction.History, $"{chapterMarker}：{summaryText}");
            faction.Notes = AppendUniqueEntry(faction.Notes, $"{chapterMarker}：同步更新");
            faction.UpdatedAt = DateTime.UtcNow;
        }

        return matchedFactions;
    }

    private async Task<List<Plot>> SyncPlotsAsync(
        Guid projectId,
        Chapter chapter,
        IReadOnlyCollection<Chapter> projectChapters,
        IReadOnlyDictionary<Guid, int> chapterOrderLookup,
        string context,
        CancellationToken cancellationToken)
    {
        var plots = (await _unitOfWork.Plots.GetByProjectIdAsync(projectId, cancellationToken)).ToList();
        var matchedPlots = plots
            .Where(plot => ContainsValue(context, plot.Title))
            .ToList();

        foreach (var matchedPlot in matchedPlots)
        {
            var plot = await _unitOfWork.Plots.GetWithChaptersAsync(matchedPlot.Id, cancellationToken);
            if (plot == null)
            {
                continue;
            }

            if (plot.InvolvedChapters.All(involved => involved.Id != chapter.Id))
            {
                plot.InvolvedChapters.Add(chapter);
            }

            var involvedChapterIds = plot.InvolvedChapters
                .Select(involved => involved.Id)
                .Append(chapter.Id)
                .Distinct()
                .ToList();

            var orderedChapterIds = involvedChapterIds
                .Where(chapterOrderLookup.ContainsKey)
                .OrderBy(id => chapterOrderLookup[id])
                .ToList();

            if (orderedChapterIds.Count > 0)
            {
                plot.StartChapterId = orderedChapterIds.First();
                plot.EndChapterId = orderedChapterIds.Last();
            }

            var involvedChapters = projectChapters
                .Where(item => orderedChapterIds.Contains(item.Id))
                .OrderBy(item => chapterOrderLookup[item.Id])
                .ToList();
            plot.ActualWordCount = involvedChapters.Sum(item => item.WordCount);
            plot.Progress = CalculatePlotProgress(plot, involvedChapters.Count);

            if (plot.Progress >= 100)
            {
                plot.Status = "已完成";
            }
            else if (plot.Status == "规划中")
            {
                plot.Status = "进行中";
            }

            plot.UpdatedAt = DateTime.UtcNow;
        }

        return matchedPlots;
    }

    private async Task<List<WorldSetting>> SyncWorldSettingsAsync(
        Guid projectId,
        string context,
        string summaryText,
        string chapterMarker,
        CancellationToken cancellationToken)
    {
        var settings = (await _unitOfWork.WorldSettings.GetByProjectIdAsync(projectId, cancellationToken)).ToList();
        var matchedSettings = settings
            .Where(setting => ContainsValue(context, setting.Name))
            .ToList();

        foreach (var setting in matchedSettings)
        {
            setting.Content = AppendUniqueEntry(setting.Content, $"{chapterMarker}：{summaryText}");
            setting.History = AppendUniqueEntry(setting.History, $"{chapterMarker}：章节推进同步");
            setting.UpdatedAt = DateTime.UtcNow;
        }

        return matchedSettings;
    }

    private static bool IsCharacterMatched(Character character, string context, IReadOnlySet<string> characterHints)
    {
        return characterHints.Contains(character.Name) || ContainsValue(context, character.Name);
    }

    private static void UpdateAppearanceRange(Character character, Chapter currentChapter, IReadOnlyDictionary<Guid, int> chapterOrderLookup)
    {
        var currentOrder = chapterOrderLookup.GetValueOrDefault(currentChapter.Id, int.MaxValue);

        if (!character.FirstAppearanceChapterId.HasValue ||
            currentOrder < chapterOrderLookup.GetValueOrDefault(character.FirstAppearanceChapterId.Value, int.MaxValue))
        {
            character.FirstAppearanceChapterId = currentChapter.Id;
        }

        if (!character.LastAppearanceChapterId.HasValue ||
            currentOrder >= chapterOrderLookup.GetValueOrDefault(character.LastAppearanceChapterId.Value, int.MinValue))
        {
            character.LastAppearanceChapterId = currentChapter.Id;
        }
    }

    private static decimal CalculatePlotProgress(Plot plot, int involvedChapterCount)
    {
        if (plot.EstimatedWordCount.HasValue && plot.EstimatedWordCount.Value > 0)
        {
            return Math.Min(100m, Math.Round(plot.ActualWordCount * 100m / plot.EstimatedWordCount.Value, 2));
        }

        var inferredProgress = involvedChapterCount * 20m;
        return Math.Max(plot.Progress, Math.Min(100m, inferredProgress));
    }

    private static string BuildChapterContext(Chapter chapter, string? relatedCharactersText)
    {
        return string.Join(
            Environment.NewLine,
            new[]
            {
                chapter.Title,
                chapter.Summary,
                chapter.Content,
                chapter.Tags,
                chapter.Notes,
                relatedCharactersText
            }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string BuildSummaryText(Chapter chapter)
    {
        if (!string.IsNullOrWhiteSpace(chapter.Summary))
        {
            return chapter.Summary.Trim();
        }

        if (!string.IsNullOrWhiteSpace(chapter.Content))
        {
            var content = chapter.Content.Trim();
            return content.Length > 120 ? content[..120] : content;
        }

        return "章节内容已更新";
    }

    private static string BuildChapterMarker(Chapter chapter)
    {
        return $"第{chapter.Order}章《{chapter.Title}》";
    }

    private static HashSet<string> ParseList(string? value)
    {
        return value?
            .Split(ListSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? [];
    }

    private static string AppendUniqueEntry(string? original, string entry)
    {
        if (string.IsNullOrWhiteSpace(entry))
        {
            return original ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(original))
        {
            return entry;
        }

        return ContainsValue(original, entry)
            ? original
            : $"{original.Trim()}{Environment.NewLine}{entry}";
    }

    private static bool ContainsValue(string? source, string? value)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}

public sealed class ChapterContentSyncResult
{
    public int UpdatedCharacterCount { get; init; }

    public IReadOnlyList<string> UpdatedCharacterNames { get; init; } = [];

    public int UpdatedFactionCount { get; init; }

    public IReadOnlyList<string> UpdatedFactionNames { get; init; } = [];

    public int UpdatedPlotCount { get; init; }

    public IReadOnlyList<string> UpdatedPlotTitles { get; init; } = [];

    public int UpdatedWorldSettingCount { get; init; }

    public IReadOnlyList<string> UpdatedWorldSettingNames { get; init; } = [];
}
