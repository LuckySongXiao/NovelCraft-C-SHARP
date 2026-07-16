using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NovelManagement.Application.Services;

namespace NovelManagement.WPF.Services;

/// <summary>
/// 将 AI 产出的纯净正式内容写入项目档案库。
/// </summary>
public class ProjectArchiveService
{
    private const string ArchiveRootDirectoryName = "archive_library";
    private const string ArchiveEntriesDirectoryName = "entries";
    private const string ArchiveCatalogFileName = "catalog.json";

    private readonly ProjectService _projectService;
    private readonly ProjectContextService _projectContextService;
    private readonly ILogger<ProjectArchiveService> _logger;

    public ProjectArchiveService(
        ProjectService projectService,
        ProjectContextService projectContextService,
        ILogger<ProjectArchiveService> logger)
    {
        _projectService = projectService;
        _projectContextService = projectContextService;
        _logger = logger;
    }

    public async Task<ProjectArchiveWriteResult> WriteCleanContentAsync(
        Guid? projectId,
        string taskType,
        string content,
        string? titleHint,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return ProjectArchiveWriteResult.Skipped("归档内容为空，已跳过写入。");
        }

        var resolvedProjectId = projectId ?? _projectContextService.CurrentProjectId;
        if (!resolvedProjectId.HasValue || resolvedProjectId.Value == Guid.Empty)
        {
            return ProjectArchiveWriteResult.Skipped("当前没有可归档的项目上下文。");
        }

        var project = await _projectService.GetProjectByIdAsync(resolvedProjectId.Value, cancellationToken);
        if (project == null)
        {
            return ProjectArchiveWriteResult.Skipped("未找到归档目标项目。");
        }

        if (string.IsNullOrWhiteSpace(project.ProjectPath))
        {
            return ProjectArchiveWriteResult.Skipped("当前项目未配置项目目录，无法写入档案库。");
        }

        try
        {
            var archiveRoot = Path.Combine(project.ProjectPath, ArchiveRootDirectoryName);
            var entryDirectory = Path.Combine(archiveRoot, ArchiveEntriesDirectoryName);
            Directory.CreateDirectory(archiveRoot);
            Directory.CreateDirectory(entryDirectory);

            var safeTaskType = SanitizeFileName(taskType);
            var safeTitle = SanitizeFileName(titleHint);
            var fileName = string.IsNullOrWhiteSpace(safeTitle)
                ? $"{DateTime.Now:yyyyMMdd_HHmmss}_{safeTaskType}.md"
                : $"{DateTime.Now:yyyyMMdd_HHmmss}_{safeTaskType}_{safeTitle}.md";
            var entryPath = Path.Combine(entryDirectory, fileName);

            await File.WriteAllTextAsync(entryPath, content.Trim(), cancellationToken);

            var catalogPath = Path.Combine(archiveRoot, ArchiveCatalogFileName);
            var catalog = await LoadCatalogAsync(catalogPath, cancellationToken);
            catalog.Add(new ProjectArchiveCatalogEntry
            {
                ProjectId = project.Id,
                ProjectName = project.Name,
                TaskType = taskType,
                Title = string.IsNullOrWhiteSpace(titleHint) ? taskType : titleHint.Trim(),
                FileName = fileName,
                RelativePath = Path.Combine(ArchiveEntriesDirectoryName, fileName),
                CreatedAt = DateTimeOffset.Now,
                CharacterCount = content.Length,
                Metadata = metadata?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? new Dictionary<string, string>()
            });

            var json = JsonSerializer.Serialize(catalog.OrderByDescending(item => item.CreatedAt), new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(catalogPath, json, cancellationToken);

            return ProjectArchiveWriteResult.Success(entryPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "写入项目档案库失败，ProjectId: {ProjectId}, TaskType: {TaskType}", resolvedProjectId, taskType);
            return ProjectArchiveWriteResult.Failed($"写入项目档案库失败: {ex.Message}");
        }
    }

    private static async Task<List<ProjectArchiveCatalogEntry>> LoadCatalogAsync(string catalogPath, CancellationToken cancellationToken)
    {
        if (!File.Exists(catalogPath))
        {
            return new List<ProjectArchiveCatalogEntry>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(catalogPath, cancellationToken);
            return JsonSerializer.Deserialize<List<ProjectArchiveCatalogEntry>>(json) ?? new List<ProjectArchiveCatalogEntry>();
        }
        catch
        {
            return new List<ProjectArchiveCatalogEntry>();
        }
    }

    private static string SanitizeFileName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Trim().Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? string.Empty : sanitized;
    }
}

public sealed class ProjectArchiveWriteResult
{
    public bool IsSuccess { get; init; }
    public bool IsSkipped { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? ArchivePath { get; init; }

    public static ProjectArchiveWriteResult Success(string archivePath)
    {
        return new ProjectArchiveWriteResult
        {
            IsSuccess = true,
            ArchivePath = archivePath,
            Message = "已写入项目档案库。"
        };
    }

    public static ProjectArchiveWriteResult Skipped(string message)
    {
        return new ProjectArchiveWriteResult
        {
            IsSkipped = true,
            Message = message
        };
    }

    public static ProjectArchiveWriteResult Failed(string message)
    {
        return new ProjectArchiveWriteResult
        {
            Message = message
        };
    }
}

public sealed class ProjectArchiveCatalogEntry
{
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string TaskType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public int CharacterCount { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}
