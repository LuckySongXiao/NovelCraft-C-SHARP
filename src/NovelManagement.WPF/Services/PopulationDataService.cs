using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NovelManagement.WPF.Views;

namespace NovelManagement.WPF.Services
{
    /// <summary>
    /// 生民体系数据服务。
    /// </summary>
    public class PopulationDataService
    {
        private readonly ILogger<PopulationDataService> _logger;
        private readonly string _populationDirectory;
        private readonly JsonSerializerOptions _serializerOptions = new()
        {
            WriteIndented = true
        };

        public PopulationDataService(ILogger<PopulationDataService> logger)
        {
            _logger = logger;
            _populationDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "NovelManagement",
                "populations");
            Directory.CreateDirectory(_populationDirectory);
        }

        public async Task<List<PopulationSystemViewModel>> LoadPopulationSystemsAsync(Guid projectId)
        {
            try
            {
                var filePath = GetProjectPopulationPath(projectId);
                if (!File.Exists(filePath))
                {
                    return new List<PopulationSystemViewModel>();
                }

                var json = await File.ReadAllTextAsync(filePath);
                var document = JsonSerializer.Deserialize<PopulationProjectDocument>(json, _serializerOptions);
                return document?.PopulationSystems ?? new List<PopulationSystemViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载生民体系数据失败，ProjectId: {ProjectId}", projectId);
                throw;
            }
        }

        public async Task SavePopulationSystemsAsync(Guid projectId, IEnumerable<PopulationSystemViewModel> systems)
        {
            try
            {
                var document = new PopulationProjectDocument
                {
                    ProjectId = projectId,
                    UpdatedAt = DateTime.UtcNow,
                    PopulationSystems = systems.OrderBy(s => s.Name).ToList()
                };

                var json = JsonSerializer.Serialize(document, _serializerOptions);
                await File.WriteAllTextAsync(GetProjectPopulationPath(projectId), json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存生民体系数据失败，ProjectId: {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<List<PopulationSystemViewModel>> ImportPopulationSystemsAsync(string filePath)
        {
            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var document = JsonSerializer.Deserialize<PopulationProjectDocument>(json, _serializerOptions);
                return document?.PopulationSystems ?? new List<PopulationSystemViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入生民体系数据失败，FilePath: {FilePath}", filePath);
                throw;
            }
        }

        public async Task ExportPopulationSystemsAsync(Guid projectId, IEnumerable<PopulationSystemViewModel> systems, string filePath)
        {
            try
            {
                var document = new PopulationProjectDocument
                {
                    ProjectId = projectId,
                    UpdatedAt = DateTime.UtcNow,
                    PopulationSystems = systems.OrderBy(s => s.Name).ToList()
                };

                var json = JsonSerializer.Serialize(document, _serializerOptions);
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出生民体系数据失败，ProjectId: {ProjectId}", projectId);
                throw;
            }
        }

        private string GetProjectPopulationPath(Guid projectId)
        {
            return Path.Combine(_populationDirectory, $"{projectId:N}.json");
        }
    }

    /// <summary>
    /// 项目生民体系文档。
    /// </summary>
    public sealed class PopulationProjectDocument
    {
        public Guid ProjectId { get; init; }
        public DateTime UpdatedAt { get; init; }
        public List<PopulationSystemViewModel> PopulationSystems { get; init; } = new();
    }
}
