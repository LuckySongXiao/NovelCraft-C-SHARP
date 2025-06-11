using Microsoft.Extensions.Logging;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;

namespace NovelManagement.Application.Services;

/// <summary>
/// 卷宗管理服务
/// </summary>
public class VolumeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<VolumeService> _logger;

    public VolumeService(IUnitOfWork unitOfWork, ILogger<VolumeService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// 创建卷宗
    /// </summary>
    public async Task<Volume> CreateVolumeAsync(Volume volume, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始创建卷宗: {VolumeTitle}, 项目ID: {ProjectId}", volume.Title, volume.ProjectId);
            
            // 设置创建时间
            volume.CreatedAt = DateTime.UtcNow;
            volume.UpdatedAt = DateTime.UtcNow;
            
            // 如果没有指定顺序，设置为最后一个
            if (volume.Order == 0)
            {
                var maxOrder = await GetMaxOrderInProjectAsync(volume.ProjectId, cancellationToken);
                volume.Order = maxOrder + 1;
            }
            
            await _unitOfWork.Volumes.AddAsync(volume, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("成功创建卷宗: {VolumeTitle}, ID: {VolumeId}", volume.Title, volume.Id);
            return volume;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建卷宗时发生错误: {VolumeTitle}", volume.Title);
            throw;
        }
    }

    /// <summary>
    /// 获取项目的卷宗列表
    /// </summary>
    public async Task<IEnumerable<Volume>> GetVolumeListAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取项目卷宗列表，项目ID: {ProjectId}", projectId);
            
            var volumes = await _unitOfWork.Volumes.GetByProjectIdAsync(projectId, cancellationToken);
            var orderedVolumes = volumes.OrderBy(v => v.Order).ToList();
            
            _logger.LogInformation("成功获取 {Count} 个卷宗", orderedVolumes.Count);
            return orderedVolumes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取卷宗列表时发生错误，项目ID: {ProjectId}", projectId);
            throw;
        }
    }

    /// <summary>
    /// 根据ID获取卷宗
    /// </summary>
    public async Task<Volume?> GetVolumeByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取卷宗，ID: {VolumeId}", id);
            var volume = await _unitOfWork.Volumes.GetByIdAsync(id, cancellationToken);
            if (volume == null)
            {
                _logger.LogWarning("未找到卷宗，ID: {VolumeId}", id);
            }
            return volume;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取卷宗时发生错误，ID: {VolumeId}", id);
            throw;
        }
    }

    /// <summary>
    /// 更新卷宗
    /// </summary>
    public async Task<Volume> UpdateVolumeAsync(Volume volume, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始更新卷宗: {VolumeTitle}, ID: {VolumeId}", volume.Title, volume.Id);
            
            // 设置更新时间
            volume.UpdatedAt = DateTime.UtcNow;
            
            await _unitOfWork.Volumes.UpdateAsync(volume, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("成功更新卷宗: {VolumeTitle}, ID: {VolumeId}", volume.Title, volume.Id);
            return volume;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新卷宗时发生错误: {VolumeTitle}, ID: {VolumeId}", volume.Title, volume.Id);
            throw;
        }
    }

    /// <summary>
    /// 删除卷宗
    /// </summary>
    public async Task<bool> DeleteVolumeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始删除卷宗，ID: {VolumeId}", id);
            
            var volume = await _unitOfWork.Volumes.GetByIdAsync(id, cancellationToken);
            if (volume == null)
            {
                _logger.LogWarning("要删除的卷宗不存在，ID: {VolumeId}", id);
                return false;
            }
            
            await _unitOfWork.Volumes.DeleteAsync(volume, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("成功删除卷宗，ID: {VolumeId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除卷宗时发生错误，ID: {VolumeId}", id);
            throw;
        }
    }

    /// <summary>
    /// 更新卷宗顺序
    /// </summary>
    public async Task<bool> UpdateVolumeOrderAsync(Guid projectId, List<Guid> volumeIds, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始更新卷宗顺序，项目ID: {ProjectId}", projectId);
            
            var volumes = await _unitOfWork.Volumes.GetByProjectIdAsync(projectId, cancellationToken);
            var volumeDict = volumes.ToDictionary(v => v.Id);
            
            for (int i = 0; i < volumeIds.Count; i++)
            {
                if (volumeDict.TryGetValue(volumeIds[i], out var volume))
                {
                    volume.Order = i + 1;
                    volume.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.Volumes.UpdateAsync(volume, cancellationToken);
                }
            }
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("成功更新卷宗顺序，项目ID: {ProjectId}", projectId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新卷宗顺序时发生错误，项目ID: {ProjectId}", projectId);
            throw;
        }
    }

    /// <summary>
    /// 获取卷宗统计信息
    /// </summary>
    public async Task<Dictionary<string, object>> GetVolumeStatisticsAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取卷宗统计信息，项目ID: {ProjectId}", projectId);
            
            var volumes = await _unitOfWork.Volumes.GetByProjectIdAsync(projectId, cancellationToken);
            var volumeList = volumes.ToList();
            
            var statistics = new Dictionary<string, object>
            {
                ["TotalVolumes"] = volumeList.Count,
                ["CompletedVolumes"] = volumeList.Count(v => v.Status == "已完成"),
                ["InProgressVolumes"] = volumeList.Count(v => v.Status == "进行中"),
                ["PlannedVolumes"] = volumeList.Count(v => v.Status == "计划中"),
                ["TotalWordCount"] = volumeList.Sum(v => v.WordCount),
                ["AverageWordCount"] = volumeList.Any() ? volumeList.Average(v => v.WordCount) : 0
            };
            
            _logger.LogInformation("成功获取卷宗统计信息，项目ID: {ProjectId}", projectId);
            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取卷宗统计信息时发生错误，项目ID: {ProjectId}", projectId);
            throw;
        }
    }

    /// <summary>
    /// 搜索卷宗
    /// </summary>
    public async Task<IEnumerable<Volume>> SearchVolumesAsync(Guid projectId, string searchTerm, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始搜索卷宗，项目ID: {ProjectId}, 关键词: {SearchTerm}", projectId, searchTerm);
            
            var volumes = await _unitOfWork.Volumes.GetByProjectIdAsync(projectId, cancellationToken);
            var filteredVolumes = volumes.Where(v => 
                v.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                (v.Description != null && v.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                (v.Tags != null && v.Tags.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(v => v.Order)
                .ToList();
            
            _logger.LogInformation("搜索完成，找到 {Count} 个匹配的卷宗", filteredVolumes.Count);
            return filteredVolumes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索卷宗时发生错误，项目ID: {ProjectId}, 关键词: {SearchTerm}", projectId, searchTerm);
            throw;
        }
    }

    /// <summary>
    /// 获取项目中卷宗的最大顺序号
    /// </summary>
    private async Task<int> GetMaxOrderInProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var volumes = await _unitOfWork.Volumes.GetByProjectIdAsync(projectId, cancellationToken);
        return volumes.Any() ? volumes.Max(v => v.Order) : 0;
    }
}
