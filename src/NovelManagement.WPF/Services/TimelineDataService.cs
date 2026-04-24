using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NovelManagement.WPF.Views;

namespace NovelManagement.WPF.Services
{
    /// <summary>
    /// 时间线数据服务。
    /// </summary>
    public class TimelineDataService
    {
        private readonly ILogger<TimelineDataService> _logger;
        private readonly string _timelineDirectory;
        private readonly JsonSerializerOptions _serializerOptions = new()
        {
            WriteIndented = true
        };

        public TimelineDataService(ILogger<TimelineDataService> logger)
        {
            _logger = logger;
            _timelineDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "NovelManagement",
                "timelines");
            Directory.CreateDirectory(_timelineDirectory);
        }

        public async Task<List<TimelineEventViewModel>> LoadTimelineEventsAsync(Guid projectId)
        {
            try
            {
                var filePath = GetProjectTimelinePath(projectId);
                if (!File.Exists(filePath))
                {
                    return new List<TimelineEventViewModel>();
                }

                var json = await File.ReadAllTextAsync(filePath);
                var document = JsonSerializer.Deserialize<TimelineProjectDocument>(json, _serializerOptions);
                return document?.Events ?? new List<TimelineEventViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载时间线数据失败，ProjectId: {ProjectId}", projectId);
                throw;
            }
        }

        public async Task SaveTimelineEventsAsync(Guid projectId, IEnumerable<TimelineEventViewModel> events)
        {
            try
            {
                var document = new TimelineProjectDocument
                {
                    ProjectId = projectId,
                    UpdatedAt = DateTime.UtcNow,
                    Events = events.OrderBy(e => e.EventDate).ToList()
                };

                var json = JsonSerializer.Serialize(document, _serializerOptions);
                await File.WriteAllTextAsync(GetProjectTimelinePath(projectId), json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存时间线数据失败，ProjectId: {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<List<TimelineEventViewModel>> ImportTimelineEventsAsync(string filePath)
        {
            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var document = JsonSerializer.Deserialize<TimelineProjectDocument>(json, _serializerOptions);
                return document?.Events ?? new List<TimelineEventViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入时间线数据失败，FilePath: {FilePath}", filePath);
                throw;
            }
        }

        public async Task ExportTimelineEventsAsync(Guid projectId, IEnumerable<TimelineEventViewModel> events, string filePath)
        {
            try
            {
                var document = new TimelineProjectDocument
                {
                    ProjectId = projectId,
                    UpdatedAt = DateTime.UtcNow,
                    Events = events.OrderBy(e => e.EventDate).ToList()
                };

                var json = JsonSerializer.Serialize(document, _serializerOptions);
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出时间线数据失败，ProjectId: {ProjectId}", projectId);
                throw;
            }
        }

        private string GetProjectTimelinePath(Guid projectId)
        {
            return Path.Combine(_timelineDirectory, $"{projectId:N}.json");
        }
    }

    /// <summary>
    /// 项目时间线文档。
    /// </summary>
    public sealed class TimelineProjectDocument
    {
        public Guid ProjectId { get; init; }
        public DateTime UpdatedAt { get; init; }
        public List<TimelineEventViewModel> Events { get; init; } = new();
    }
}
