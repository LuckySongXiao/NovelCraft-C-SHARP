using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NovelManagement.WPF.Views;

namespace NovelManagement.WPF.Services
{
    /// <summary>
    /// 司法体系数据服务。
    /// </summary>
    public class JudicialDataService
    {
        private readonly ILogger<JudicialDataService> _logger;
        private readonly string _judicialDirectory;
        private readonly JsonSerializerOptions _serializerOptions = new()
        {
            WriteIndented = true
        };

        public JudicialDataService(ILogger<JudicialDataService> logger)
        {
            _logger = logger;
            _judicialDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "NovelManagement",
                "judicials");
            Directory.CreateDirectory(_judicialDirectory);
        }

        public async Task<List<JudicialSystemViewModel>> LoadJudicialSystemsAsync(Guid projectId)
        {
            try
            {
                var filePath = GetProjectJudicialPath(projectId);
                if (!File.Exists(filePath))
                {
                    return new List<JudicialSystemViewModel>();
                }

                var json = await File.ReadAllTextAsync(filePath);
                var document = JsonSerializer.Deserialize<JudicialProjectDocument>(json, _serializerOptions);
                return document?.JudicialSystems ?? new List<JudicialSystemViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载司法体系数据失败，ProjectId: {ProjectId}", projectId);
                throw;
            }
        }

        public async Task SaveJudicialSystemsAsync(Guid projectId, IEnumerable<JudicialSystemViewModel> systems)
        {
            try
            {
                var document = new JudicialProjectDocument
                {
                    ProjectId = projectId,
                    UpdatedAt = DateTime.UtcNow,
                    JudicialSystems = systems.OrderBy(s => s.Name).ToList()
                };

                var json = JsonSerializer.Serialize(document, _serializerOptions);
                await File.WriteAllTextAsync(GetProjectJudicialPath(projectId), json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存司法体系数据失败，ProjectId: {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<List<JudicialSystemViewModel>> ImportJudicialSystemsAsync(string filePath)
        {
            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var document = JsonSerializer.Deserialize<JudicialProjectDocument>(json, _serializerOptions);
                return document?.JudicialSystems ?? new List<JudicialSystemViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入司法体系数据失败，FilePath: {FilePath}", filePath);
                throw;
            }
        }

        public async Task ExportJudicialSystemsAsync(Guid projectId, IEnumerable<JudicialSystemViewModel> systems, string filePath)
        {
            try
            {
                var document = new JudicialProjectDocument
                {
                    ProjectId = projectId,
                    UpdatedAt = DateTime.UtcNow,
                    JudicialSystems = systems.OrderBy(s => s.Name).ToList()
                };

                var json = JsonSerializer.Serialize(document, _serializerOptions);
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出司法体系数据失败，ProjectId: {ProjectId}", projectId);
                throw;
            }
        }

        private string GetProjectJudicialPath(Guid projectId)
        {
            return Path.Combine(_judicialDirectory, $"{projectId:N}.json");
        }
    }

    /// <summary>
    /// 项目司法体系文档。
    /// </summary>
    public sealed class JudicialProjectDocument
    {
        public Guid ProjectId { get; init; }
        public DateTime UpdatedAt { get; init; }
        public List<JudicialSystemViewModel> JudicialSystems { get; init; } = new();
    }
}
