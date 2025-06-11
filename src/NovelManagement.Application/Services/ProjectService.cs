using Microsoft.Extensions.Logging;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;
using NovelManagement.Application.DTOs;

namespace NovelManagement.Application.Services;

/// <summary>
/// 项目管理服务
/// </summary>
public class ProjectService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(IUnitOfWork unitOfWork, ILogger<ProjectService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有项目
    /// </summary>
    public async Task<IEnumerable<Project>> GetAllProjectsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取所有项目");
            var projects = await _unitOfWork.Projects.GetAllAsync(cancellationToken);
            _logger.LogInformation("成功获取 {Count} 个项目", projects.Count());
            return projects;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取所有项目时发生错误");
            throw;
        }
    }

    /// <summary>
    /// 根据ID获取项目
    /// </summary>
    public async Task<Project?> GetProjectByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取项目，ID: {ProjectId}", id);
            var project = await _unitOfWork.Projects.GetByIdAsync(id, cancellationToken);
            if (project == null)
            {
                _logger.LogWarning("未找到项目，ID: {ProjectId}", id);
            }
            return project;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取项目时发生错误，ID: {ProjectId}", id);
            throw;
        }
    }

    /// <summary>
    /// 创建新项目
    /// </summary>
    public async Task<Project> CreateProjectAsync(Project project, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始创建项目: {ProjectName}", project.Name);
            
            // 设置创建时间
            project.CreatedAt = DateTime.UtcNow;
            project.UpdatedAt = DateTime.UtcNow;
            
            await _unitOfWork.Projects.AddAsync(project, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("成功创建项目: {ProjectName}, ID: {ProjectId}", project.Name, project.Id);
            return project;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建项目时发生错误: {ProjectName}", project.Name);
            throw;
        }
    }

    /// <summary>
    /// 更新项目
    /// </summary>
    public async Task<Project> UpdateProjectAsync(Project project, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始更新项目: {ProjectName}, ID: {ProjectId}", project.Name, project.Id);
            
            // 设置更新时间
            project.UpdatedAt = DateTime.UtcNow;
            
            await _unitOfWork.Projects.UpdateAsync(project, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("成功更新项目: {ProjectName}, ID: {ProjectId}", project.Name, project.Id);
            return project;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新项目时发生错误: {ProjectName}, ID: {ProjectId}", project.Name, project.Id);
            throw;
        }
    }

    /// <summary>
    /// 删除项目
    /// </summary>
    public async Task<bool> DeleteProjectAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始删除项目，ID: {ProjectId}", id);
            
            var project = await _unitOfWork.Projects.GetByIdAsync(id, cancellationToken);
            if (project == null)
            {
                _logger.LogWarning("要删除的项目不存在，ID: {ProjectId}", id);
                return false;
            }
            
            await _unitOfWork.Projects.DeleteAsync(project, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("成功删除项目，ID: {ProjectId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除项目时发生错误，ID: {ProjectId}", id);
            throw;
        }
    }

    /// <summary>
    /// 搜索项目
    /// </summary>
    public async Task<IEnumerable<Project>> SearchProjectsAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始搜索项目，关键词: {SearchTerm}", searchTerm);
            
            var projects = await _unitOfWork.Projects.GetAllAsync(cancellationToken);
            var filteredProjects = projects.Where(p => 
                p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                (p.Description != null && p.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                (p.Tags != null && p.Tags.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            
            _logger.LogInformation("搜索完成，找到 {Count} 个匹配的项目", filteredProjects.Count);
            return filteredProjects;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索项目时发生错误，关键词: {SearchTerm}", searchTerm);
            throw;
        }
    }

    /// <summary>
    /// 获取项目统计信息
    /// </summary>
    public async Task<Dictionary<string, object>> GetProjectStatisticsAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始获取项目统计信息，ID: {ProjectId}", projectId);
            
            var project = await _unitOfWork.Projects.GetByIdAsync(projectId, cancellationToken);
            if (project == null)
            {
                throw new ArgumentException($"项目不存在，ID: {projectId}");
            }

            var statistics = new Dictionary<string, object>
            {
                ["ProjectName"] = project.Name,
                ["CreatedAt"] = project.CreatedAt,
                ["UpdatedAt"] = project.UpdatedAt,
                ["Status"] = project.Status,
                ["Progress"] = project.Progress
            };

            // 可以在这里添加更多统计信息，比如角色数量、章节数量等
            // 这需要在后续实现其他服务后补充

            _logger.LogInformation("成功获取项目统计信息，ID: {ProjectId}", projectId);
            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取项目统计信息时发生错误，ID: {ProjectId}", projectId);
            throw;
        }
    }
}
