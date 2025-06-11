using Microsoft.Extensions.Logging;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;

namespace NovelManagement.Application.Services;

/// <summary>
/// 剧情管理服务
/// </summary>
public class PlotService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PlotService> _logger;

    public PlotService(IUnitOfWork unitOfWork, ILogger<PlotService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// 创建剧情
    /// </summary>
    public async Task<Plot> CreatePlotAsync(Plot plot, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始创建剧情: {PlotTitle}, 项目ID: {ProjectId}", plot.Title, plot.ProjectId);
            
            // 设置创建时间
            plot.CreatedAt = DateTime.UtcNow;
            plot.UpdatedAt = DateTime.UtcNow;
            
            await _unitOfWork.Plots.AddAsync(plot, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("成功创建剧情: {PlotTitle}, ID: {PlotId}", plot.Title, plot.Id);
            return plot;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建剧情时发生错误: {PlotTitle}", plot.Title);
            throw;
        }
    }

    /// <summary>
    /// 更新剧情信息
    /// </summary>
    public async Task<Plot> UpdatePlotAsync(Plot plot, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始更新剧情: {PlotTitle}, ID: {PlotId}", plot.Title, plot.Id);
            
            // 设置更新时间
            plot.UpdatedAt = DateTime.UtcNow;
            
            await _unitOfWork.Plots.UpdateAsync(plot, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("成功更新剧情: {PlotTitle}, ID: {PlotId}", plot.Title, plot.Id);
            return plot;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新剧情时发生错误: {PlotTitle}, ID: {PlotId}", plot.Title, plot.Id);
            throw;
        }
    }

    /// <summary>
    /// 根据ID获取剧情
    /// </summary>
    public async Task<Plot?> GetPlotByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取剧情，ID: {PlotId}", id);
            var plot = await _unitOfWork.Plots.GetByIdAsync(id, cancellationToken);
            if (plot == null)
            {
                _logger.LogWarning("未找到剧情，ID: {PlotId}", id);
            }
            return plot;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取剧情时发生错误，ID: {PlotId}", id);
            throw;
        }
    }

    /// <summary>
    /// 根据项目ID获取所有剧情
    /// </summary>
    public async Task<IEnumerable<Plot>> GetPlotsByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取项目剧情列表，项目ID: {ProjectId}", projectId);
            
            var plots = await _unitOfWork.Plots.GetByProjectIdAsync(projectId, cancellationToken);
            var orderedPlots = plots.OrderBy(p => p.Title).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个剧情", orderedPlots.Count);
            return orderedPlots;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取项目剧情列表时发生错误，项目ID: {ProjectId}", projectId);
            throw;
        }
    }

    /// <summary>
    /// 按类型获取剧情
    /// </summary>
    public async Task<IEnumerable<Plot>> GetPlotsByTypeAsync(Guid projectId, string type, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始按类型获取剧情，项目ID: {ProjectId}, 类型: {Type}", projectId, type);
            
            var plots = await _unitOfWork.Plots.GetByTypeAsync(projectId, type, cancellationToken);
            var orderedPlots = plots.OrderBy(p => p.Title).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个 {Type} 类型的剧情", orderedPlots.Count, type);
            return orderedPlots;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按类型获取剧情时发生错误，项目ID: {ProjectId}, 类型: {Type}", projectId, type);
            throw;
        }
    }

    /// <summary>
    /// 获取剧情时间线
    /// </summary>
    public async Task<IEnumerable<Plot>> GetPlotTimelineAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取剧情时间线，项目ID: {ProjectId}", projectId);
            
            var plots = await _unitOfWork.Plots.GetByProjectIdAsync(projectId, cancellationToken);
            
            // 按开始章节顺序排序，构建时间线
            var timelinePlots = plots
                .Where(p => p.StartChapter != null)
                .OrderBy(p => p.StartChapter!.Volume?.Order)
                .ThenBy(p => p.StartChapter!.Order)
                .ThenBy(p => p.Title)
                .ToList();
            
            _logger.LogInformation("成功获取剧情时间线，共 {Count} 个剧情", timelinePlots.Count);
            return timelinePlots;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取剧情时间线时发生错误，项目ID: {ProjectId}", projectId);
            throw;
        }
    }

    /// <summary>
    /// 获取剧情进度
    /// </summary>
    public async Task<Dictionary<string, object>> GetPlotProgressAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取剧情进度，项目ID: {ProjectId}", projectId);
            
            var plots = await _unitOfWork.Plots.GetByProjectIdAsync(projectId, cancellationToken);
            var plotList = plots.ToList();
            
            var statusStats = plotList
                .GroupBy(p => p.Status)
                .ToDictionary(g => g.Key, g => g.Count());
            
            var priorityStats = plotList
                .GroupBy(p => p.Priority)
                .ToDictionary(g => g.Key, g => g.Count());
            
            var progress = new Dictionary<string, object>
            {
                ["TotalPlots"] = plotList.Count,
                ["StatusStatistics"] = statusStats,
                ["PriorityStatistics"] = priorityStats,
                ["CompletedPlots"] = plotList.Count(p => p.Status == "已完成"),
                ["InProgressPlots"] = plotList.Count(p => p.Status == "进行中"),
                ["PlannedPlots"] = plotList.Count(p => p.Status == "计划中"),
                ["HighPriorityPlots"] = plotList.Count(p => p.Priority == "高"),
                ["CompletionRate"] = plotList.Any() ? (double)plotList.Count(p => p.Status == "已完成") / plotList.Count * 100 : 0
            };
            
            _logger.LogInformation("成功获取剧情进度，项目ID: {ProjectId}", projectId);
            return progress;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取剧情进度时发生错误，项目ID: {ProjectId}", projectId);
            throw;
        }
    }

    /// <summary>
    /// 删除剧情
    /// </summary>
    public async Task<bool> DeletePlotAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始删除剧情，ID: {PlotId}", id);
            
            var plot = await _unitOfWork.Plots.GetByIdAsync(id, cancellationToken);
            if (plot == null)
            {
                _logger.LogWarning("要删除的剧情不存在，ID: {PlotId}", id);
                return false;
            }
            
            await _unitOfWork.Plots.DeleteAsync(plot, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("成功删除剧情，ID: {PlotId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除剧情时发生错误，ID: {PlotId}", id);
            throw;
        }
    }

    /// <summary>
    /// 搜索剧情
    /// </summary>
    public async Task<IEnumerable<Plot>> SearchPlotsAsync(Guid projectId, string searchTerm, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始搜索剧情，项目ID: {ProjectId}, 关键词: {SearchTerm}", projectId, searchTerm);
            
            var plots = await _unitOfWork.Plots.SearchAsync(projectId, searchTerm, cancellationToken);
            var orderedPlots = plots.OrderBy(p => p.Title).ToList();
            
            _logger.LogInformation("搜索完成，找到 {Count} 个匹配的剧情", orderedPlots.Count);
            return orderedPlots;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索剧情时发生错误，项目ID: {ProjectId}, 关键词: {SearchTerm}", projectId, searchTerm);
            throw;
        }
    }

    /// <summary>
    /// 根据角色获取相关剧情
    /// </summary>
    public async Task<IEnumerable<Plot>> GetPlotsByCharacterAsync(Guid characterId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取角色相关剧情，角色ID: {CharacterId}", characterId);
            
            var plots = await _unitOfWork.Plots.GetByCharacterAsync(characterId, cancellationToken);
            var orderedPlots = plots.OrderBy(p => p.Title).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个角色相关剧情", orderedPlots.Count);
            return orderedPlots;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取角色相关剧情时发生错误，角色ID: {CharacterId}", characterId);
            throw;
        }
    }

    /// <summary>
    /// 根据章节获取涉及的剧情
    /// </summary>
    public async Task<IEnumerable<Plot>> GetPlotsByChapterAsync(Guid chapterId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取章节涉及剧情，章节ID: {ChapterId}", chapterId);
            
            var plots = await _unitOfWork.Plots.GetByChapterAsync(chapterId, cancellationToken);
            var orderedPlots = plots.OrderBy(p => p.Title).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个章节涉及剧情", orderedPlots.Count);
            return orderedPlots;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取章节涉及剧情时发生错误，章节ID: {ChapterId}", chapterId);
            throw;
        }
    }

    /// <summary>
    /// 根据重要性获取剧情
    /// </summary>
    public async Task<IEnumerable<Plot>> GetPlotsByImportanceAsync(Guid projectId, int minImportance, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始按重要性获取剧情，项目ID: {ProjectId}, 最小重要性: {MinImportance}", projectId, minImportance);
            
            var plots = await _unitOfWork.Plots.GetByImportanceAsync(projectId, minImportance, cancellationToken);
            var orderedPlots = plots.OrderByDescending(p => p.Importance).ThenBy(p => p.Title).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个重要性 >= {MinImportance} 的剧情", orderedPlots.Count, minImportance);
            return orderedPlots;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按重要性获取剧情时发生错误，项目ID: {ProjectId}, 最小重要性: {MinImportance}", projectId, minImportance);
            throw;
        }
    }
}
