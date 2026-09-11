using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NovelManagement.Application.Services;
using NovelManagement.Core.Entities;
using NovelManagement.Infrastructure.Data;
using NovelManagement.WPF.Views;

namespace NovelManagement.WPF.Services;

/// <summary>
/// 项目目录服务，负责项目列表、软删除与恢复、导入导出等操作。
/// </summary>
public class ProjectCatalogService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProjectCatalogService> _logger;

    public ProjectCatalogService(
        IServiceScopeFactory serviceScopeFactory,
        IConfiguration configuration,
        ILogger<ProjectCatalogService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ProjectCatalogItem>> GetActiveProjectsAsync()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var projectService = scope.ServiceProvider.GetRequiredService<ProjectService>();
        var projects = await projectService.GetAllProjectsAsync();
        return projects
            .OrderByDescending(p => p.LastAccessedAt ?? p.UpdatedAt)
            .ThenBy(p => p.Name)
            .Select(MapToCatalogItem)
            .ToList();
    }

    public async Task<IReadOnlyList<ProjectCatalogItem>> GetDeletedProjectsAsync()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NovelManagementDbContext>();
        var projects = await dbContext.Projects
            .IgnoreQueryFilters()
            .Where(p => p.IsDeleted)
            .OrderByDescending(p => p.DeletedAt ?? p.UpdatedAt)
            .ThenBy(p => p.Name)
            .ToListAsync();

        return projects.Select(MapToCatalogItem).ToList();
    }

    public async Task<ProjectCatalogItem> CreateProjectAsync(NewProjectDialog.NewProjectModel model)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var projectService = scope.ServiceProvider.GetRequiredService<ProjectService>();

        var projectDirectory = PrepareProjectDirectory(model.Name);
        var project = new Project
        {
            Name = model.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(model.Description) ? "新建项目" : model.Description.Trim(),
            Type = string.IsNullOrWhiteSpace(model.Type) ? "长篇书籍" : model.Type.Trim(),
            Status = "进行中",
            Progress = 0,
            ProjectPath = projectDirectory,
            Tags = string.Join(',', new[] { model.Template, model.EnableAI ? "AI" : null, model.AutoSave ? "AutoSave" : null, model.VersionControl ? "VersionControl" : null }.Where(v => !string.IsNullOrWhiteSpace(v))),
            Settings = JsonSerializer.Serialize(new
            {
                model.Author,
                model.TargetWordCount,
                model.EnableAI,
                model.AutoSave,
                model.VersionControl,
                model.Template
            }),
            Notes = model.Template
        };

        var created = await projectService.CreateProjectAsync(project);
        CreateProjectScaffold(created.ProjectPath, created.Name);
        return MapToCatalogItem(created);
    }

    public async Task<ProjectCatalogItem> ImportProjectAsync(string filePath)
    {
        var model = new NewProjectDialog.NewProjectModel
        {
            Name = Path.GetFileNameWithoutExtension(filePath),
            Description = $"从文件 {filePath} 导入的项目",
            Type = "导入项目",
            TargetWordCount = 100000,
            EnableAI = true,
            AutoSave = true,
            VersionControl = false,
            Template = "标准模板"
        };

        return await CreateProjectAsync(model);
    }

    public async Task<ProjectCatalogItem> UpdateProjectAsync(ProjectCatalogItem item)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var projectService = scope.ServiceProvider.GetRequiredService<ProjectService>();
        var project = await projectService.GetProjectByIdAsync(item.ProjectId)
            ?? throw new InvalidOperationException("未找到要更新的项目。");

        project.Name = item.Name.Trim();
        project.Description = item.Description.Trim();
        project.Type = item.Type.Trim();
        project.Status = item.Status.Trim();
        project.ProjectPath = item.ProjectPath;
        project.LastAccessedAt = item.LastAccessedAt;

        var updated = await projectService.UpdateProjectAsync(project);
        return MapToCatalogItem(updated);
    }

    public async Task<ProjectCatalogItem?> TouchProjectAsync(Guid projectId)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var projectService = scope.ServiceProvider.GetRequiredService<ProjectService>();
        var project = await projectService.GetProjectByIdAsync(projectId);
        if (project == null)
        {
            return null;
        }

        project.LastAccessedAt = DateTime.UtcNow;
        var updated = await projectService.UpdateProjectAsync(project);
        return MapToCatalogItem(updated);
    }

    public async Task SoftDeleteAsync(Guid projectId)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var projectService = scope.ServiceProvider.GetRequiredService<ProjectService>();
        await projectService.DeleteProjectAsync(projectId);
    }

    public async Task RestoreAsync(Guid projectId)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NovelManagementDbContext>();
        var project = await dbContext.Projects
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project == null)
        {
            throw new InvalidOperationException("未找到要恢复的项目。");
        }

        project.IsDeleted = false;
        project.DeletedAt = null;
        project.DeletedBy = null;
        project.UpdatedAt = DateTime.UtcNow;
        dbContext.Update(project);
        await dbContext.SaveChangesAsync();
    }

    public async Task PermanentlyDeleteAsync(Guid projectId)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NovelManagementDbContext>();
        var project = await dbContext.Projects
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project == null)
        {
            return;
        }

        var projectPath = project.ProjectPath;
        dbContext.Remove(project);
        await dbContext.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(projectPath) && Directory.Exists(projectPath))
        {
            try
            {
                Directory.Delete(projectPath, recursive: true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "删除项目目录失败: {ProjectPath}", projectPath);
            }
        }
    }

    public async Task<int> EmptyRecycleBinAsync()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NovelManagementDbContext>();
        var deletedProjects = await dbContext.Projects
            .IgnoreQueryFilters()
            .Where(p => p.IsDeleted)
            .ToListAsync();

        var count = deletedProjects.Count;
        if (count == 0)
        {
            return 0;
        }

        dbContext.RemoveRange(deletedProjects);
        await dbContext.SaveChangesAsync();
        return count;
    }

    public async Task ExportProjectsAsync(IEnumerable<Guid> projectIds, string filePath)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NovelManagementDbContext>();
        var projects = await dbContext.Projects
            .Where(p => projectIds.Contains(p.Id))
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.Type,
                p.Status,
                p.ProjectPath,
                p.Tags,
                p.Settings,
                p.CreatedAt,
                p.UpdatedAt
            })
            .ToListAsync();

        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(projects, new JsonSerializerOptions
        {
            WriteIndented = true
        }));
    }

    private string PrepareProjectDirectory(string projectName)
    {
        var appDataRoot = _configuration["Paths:AppDataRoot"]
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NovelManagement");
        var projectsRoot = Path.Combine(appDataRoot, "projects");
        Directory.CreateDirectory(projectsRoot);

        var safeName = string.Concat(projectName.Trim().Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch));
        if (string.IsNullOrWhiteSpace(safeName))
        {
            safeName = "DemoProject";
        }

        var directory = Path.Combine(projectsRoot, $"{DateTime.Now:yyyyMMdd_HHmmss}_{safeName}");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void CreateProjectScaffold(string? projectPath, string projectName)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
        {
            return;
        }

        Directory.CreateDirectory(projectPath);
        Directory.CreateDirectory(Path.Combine(projectPath, "chapters"));
        Directory.CreateDirectory(Path.Combine(projectPath, "exports"));
        Directory.CreateDirectory(Path.Combine(projectPath, "assets"));
        Directory.CreateDirectory(Path.Combine(projectPath, "archive_library"));
        Directory.CreateDirectory(Path.Combine(projectPath, "archive_library", "entries"));

        var readmePath = Path.Combine(projectPath, "README.txt");
        if (!File.Exists(readmePath))
        {
            File.WriteAllText(readmePath, $"项目名称: {projectName}{Environment.NewLine}此目录用于保存项目相关附件与导出文件。");
        }
    }

    private static ProjectCatalogItem MapToCatalogItem(Project project)
    {
        return new ProjectCatalogItem
        {
            ProjectId = project.Id,
            Name = project.Name,
            Description = project.Description ?? string.Empty,
            Type = project.Type,
            Status = string.IsNullOrWhiteSpace(project.Status) ? "进行中" : project.Status,
            LastUpdated = FormatRelativeTime(project.UpdatedAt.ToLocalTime()),
            LastUpdatedAt = project.UpdatedAt.ToLocalTime(),
            ProjectPath = project.ProjectPath ?? string.Empty,
            IsDeleted = project.IsDeleted,
            DeletedAt = project.DeletedAt?.ToLocalTime(),
            DeletedBy = project.DeletedBy,
            LastAccessedAt = project.LastAccessedAt?.ToLocalTime()
        };
    }

    private static string FormatRelativeTime(DateTime time)
    {
        var delta = DateTime.Now - time;
        if (delta.TotalMinutes < 1)
        {
            return Localization.LocalizationManager.T("PM.JustNow", "刚刚");
        }

        if (delta.TotalHours < 1)
        {
            return Localization.LocalizationManager.TF("VM.MinutesAgo", "{0}分钟前", Math.Max(1, (int)delta.TotalMinutes));
        }

        if (delta.TotalDays < 1)
        {
            return Localization.LocalizationManager.TF("VM.HoursAgo", "{0}小时前", Math.Max(1, (int)delta.TotalHours));
        }

        if (delta.TotalDays < 7)
        {
            return Localization.LocalizationManager.TF("VM.DaysAgo", "{0}天前", Math.Max(1, (int)delta.TotalDays));
        }

        return time.ToString("yyyy-MM-dd HH:mm");
    }
}

/// <summary>
/// 项目目录项。
/// </summary>
public class ProjectCatalogItem
{
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string LastUpdated { get; set; } = string.Empty;
    public DateTime LastUpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public string ProjectPath { get; set; } = string.Empty;
    public DateTime? LastAccessedAt { get; set; }
}
