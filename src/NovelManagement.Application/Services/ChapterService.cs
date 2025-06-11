using Microsoft.Extensions.Logging;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;

namespace NovelManagement.Application.Services;

/// <summary>
/// 章节管理服务
/// </summary>
public class ChapterService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ChapterService> _logger;

    public ChapterService(IUnitOfWork unitOfWork, ILogger<ChapterService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// 创建章节
    /// </summary>
    public async Task<Chapter> CreateChapterAsync(Chapter chapter, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始创建章节: {ChapterTitle}, 卷宗ID: {VolumeId}", chapter.Title, chapter.VolumeId);
            
            // 设置创建时间
            chapter.CreatedAt = DateTime.UtcNow;
            chapter.UpdatedAt = DateTime.UtcNow;
            
            // 如果没有指定顺序，设置为最后一个
            if (chapter.Order == 0)
            {
                var maxOrder = await GetMaxOrderInVolumeAsync(chapter.VolumeId, cancellationToken);
                chapter.Order = maxOrder + 1;
            }
            
            await _unitOfWork.Chapters.AddAsync(chapter, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("成功创建章节: {ChapterTitle}, ID: {ChapterId}", chapter.Title, chapter.Id);
            return chapter;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建章节时发生错误: {ChapterTitle}", chapter.Title);
            throw;
        }
    }

    /// <summary>
    /// 更新章节内容
    /// </summary>
    public async Task<Chapter> UpdateChapterContentAsync(Guid chapterId, string content, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始更新章节内容，ID: {ChapterId}", chapterId);
            
            var chapter = await _unitOfWork.Chapters.GetByIdAsync(chapterId, cancellationToken);
            if (chapter == null)
            {
                throw new ArgumentException($"章节不存在，ID: {chapterId}");
            }
            
            chapter.Content = content;
            chapter.WordCount = CountWords(content);
            chapter.UpdatedAt = DateTime.UtcNow;
            
            await _unitOfWork.Chapters.UpdateAsync(chapter, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("成功更新章节内容，ID: {ChapterId}, 字数: {WordCount}", chapterId, chapter.WordCount);
            return chapter;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新章节内容时发生错误，ID: {ChapterId}", chapterId);
            throw;
        }
    }

    /// <summary>
    /// 获取卷宗的章节列表
    /// </summary>
    public async Task<IEnumerable<Chapter>> GetChapterListAsync(Guid volumeId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取卷宗章节列表，卷宗ID: {VolumeId}", volumeId);
            
            var chapters = await _unitOfWork.Chapters.GetByVolumeIdAsync(volumeId, cancellationToken);
            var orderedChapters = chapters.OrderBy(c => c.Order).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个章节", orderedChapters.Count);
            return orderedChapters;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取章节列表时发生错误，卷宗ID: {VolumeId}", volumeId);
            throw;
        }
    }

    /// <summary>
    /// 根据项目ID获取所有章节
    /// </summary>
    public async Task<IEnumerable<Chapter>> GetChaptersByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取项目章节列表，项目ID: {ProjectId}", projectId);
            
            var chapters = await _unitOfWork.Chapters.GetByProjectIdAsync(projectId, cancellationToken);
            var orderedChapters = chapters.OrderBy(c => c.Volume?.Order).ThenBy(c => c.Order).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个章节", orderedChapters.Count);
            return orderedChapters;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取项目章节列表时发生错误，项目ID: {ProjectId}", projectId);
            throw;
        }
    }

    /// <summary>
    /// 根据ID获取章节
    /// </summary>
    public async Task<Chapter?> GetChapterByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取章节，ID: {ChapterId}", id);
            var chapter = await _unitOfWork.Chapters.GetByIdAsync(id, cancellationToken);
            if (chapter == null)
            {
                _logger.LogWarning("未找到章节，ID: {ChapterId}", id);
            }
            return chapter;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取章节时发生错误，ID: {ChapterId}", id);
            throw;
        }
    }

    /// <summary>
    /// 更新章节
    /// </summary>
    public async Task<Chapter> UpdateChapterAsync(Chapter chapter, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始更新章节: {ChapterTitle}, ID: {ChapterId}", chapter.Title, chapter.Id);
            
            // 设置更新时间
            chapter.UpdatedAt = DateTime.UtcNow;
            
            // 如果内容有变化，重新计算字数
            if (!string.IsNullOrEmpty(chapter.Content))
            {
                chapter.WordCount = CountWords(chapter.Content);
            }
            
            await _unitOfWork.Chapters.UpdateAsync(chapter, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("成功更新章节: {ChapterTitle}, ID: {ChapterId}", chapter.Title, chapter.Id);
            return chapter;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新章节时发生错误: {ChapterTitle}, ID: {ChapterId}", chapter.Title, chapter.Id);
            throw;
        }
    }

    /// <summary>
    /// 删除章节
    /// </summary>
    public async Task<bool> DeleteChapterAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始删除章节，ID: {ChapterId}", id);
            
            var chapter = await _unitOfWork.Chapters.GetByIdAsync(id, cancellationToken);
            if (chapter == null)
            {
                _logger.LogWarning("要删除的章节不存在，ID: {ChapterId}", id);
                return false;
            }
            
            await _unitOfWork.Chapters.DeleteAsync(chapter, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("成功删除章节，ID: {ChapterId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除章节时发生错误，ID: {ChapterId}", id);
            throw;
        }
    }

    /// <summary>
    /// 更新章节顺序
    /// </summary>
    public async Task<bool> UpdateChapterOrderAsync(Guid volumeId, List<Guid> chapterIds, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始更新章节顺序，卷宗ID: {VolumeId}", volumeId);
            
            var chapters = await _unitOfWork.Chapters.GetByVolumeIdAsync(volumeId, cancellationToken);
            var chapterDict = chapters.ToDictionary(c => c.Id);
            
            for (int i = 0; i < chapterIds.Count; i++)
            {
                if (chapterDict.TryGetValue(chapterIds[i], out var chapter))
                {
                    chapter.Order = i + 1;
                    chapter.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.Chapters.UpdateAsync(chapter, cancellationToken);
                }
            }
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("成功更新章节顺序，卷宗ID: {VolumeId}", volumeId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新章节顺序时发生错误，卷宗ID: {VolumeId}", volumeId);
            throw;
        }
    }

    /// <summary>
    /// 获取章节统计信息
    /// </summary>
    public async Task<Dictionary<string, object>> GetChapterStatisticsAsync(Guid volumeId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取章节统计信息，卷宗ID: {VolumeId}", volumeId);
            
            var chapters = await _unitOfWork.Chapters.GetByVolumeIdAsync(volumeId, cancellationToken);
            var chapterList = chapters.ToList();
            
            var statistics = new Dictionary<string, object>
            {
                ["TotalChapters"] = chapterList.Count,
                ["CompletedChapters"] = chapterList.Count(c => c.Status == "已完成"),
                ["InProgressChapters"] = chapterList.Count(c => c.Status == "进行中"),
                ["PlannedChapters"] = chapterList.Count(c => c.Status == "计划中"),
                ["TotalWordCount"] = chapterList.Sum(c => c.WordCount),
                ["AverageWordCount"] = chapterList.Any() ? chapterList.Average(c => c.WordCount) : 0,
                ["MaxWordCount"] = chapterList.Any() ? chapterList.Max(c => c.WordCount) : 0,
                ["MinWordCount"] = chapterList.Any() ? chapterList.Min(c => c.WordCount) : 0
            };
            
            _logger.LogInformation("成功获取章节统计信息，卷宗ID: {VolumeId}", volumeId);
            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取章节统计信息时发生错误，卷宗ID: {VolumeId}", volumeId);
            throw;
        }
    }

    /// <summary>
    /// 搜索章节
    /// </summary>
    public async Task<IEnumerable<Chapter>> SearchChaptersAsync(Guid projectId, string searchTerm, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始搜索章节，项目ID: {ProjectId}, 关键词: {SearchTerm}", projectId, searchTerm);
            
            var chapters = await _unitOfWork.Chapters.GetByProjectIdAsync(projectId, cancellationToken);
            var filteredChapters = chapters.Where(c => 
                c.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                (c.Summary != null && c.Summary.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                (c.Content != null && c.Content.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                (c.Tags != null && c.Tags.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(c => c.Volume?.Order).ThenBy(c => c.Order)
                .ToList();
            
            _logger.LogInformation("搜索完成，找到 {Count} 个匹配的章节", filteredChapters.Count);
            return filteredChapters;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索章节时发生错误，项目ID: {ProjectId}, 关键词: {SearchTerm}", projectId, searchTerm);
            throw;
        }
    }

    /// <summary>
    /// 计算文本字数
    /// </summary>
    private static int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;
        
        // 简单的字数统计，可以根据需要优化
        return text.Length;
    }

    /// <summary>
    /// 获取卷宗中章节的最大顺序号
    /// </summary>
    private async Task<int> GetMaxOrderInVolumeAsync(Guid volumeId, CancellationToken cancellationToken = default)
    {
        var chapters = await _unitOfWork.Chapters.GetByVolumeIdAsync(volumeId, cancellationToken);
        return chapters.Any() ? chapters.Max(c => c.Order) : 0;
    }
}
