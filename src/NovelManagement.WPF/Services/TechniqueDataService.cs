using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NovelManagement.WPF.Views;

namespace NovelManagement.WPF.Services
{
    /// <summary>
    /// 功法体系数据服务。
    /// </summary>
    public class TechniqueDataService
    {
        private readonly ILogger<TechniqueDataService> _logger;
        private readonly string _techniqueDirectory;
        private readonly JsonSerializerOptions _serializerOptions = new()
        {
            WriteIndented = true
        };

        public TechniqueDataService(ILogger<TechniqueDataService> logger)
        {
            _logger = logger;
            _techniqueDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "NovelManagement",
                "techniques");
            Directory.CreateDirectory(_techniqueDirectory);
        }

        public async Task<List<TechniqueSystemViewModel>> LoadTechniqueSystemsAsync(Guid projectId)
        {
            try
            {
                var filePath = GetProjectTechniquePath(projectId);
                if (!File.Exists(filePath))
                {
                    return new List<TechniqueSystemViewModel>();
                }

                var json = await File.ReadAllTextAsync(filePath);
                var document = JsonSerializer.Deserialize<TechniqueProjectDocument>(json, _serializerOptions);
                return document?.Techniques ?? new List<TechniqueSystemViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载功法体系数据失败，ProjectId: {ProjectId}", projectId);
                throw;
            }
        }

        public async Task SaveTechniqueSystemsAsync(Guid projectId, IEnumerable<TechniqueSystemViewModel> techniques)
        {
            try
            {
                var document = new TechniqueProjectDocument
                {
                    ProjectId = projectId,
                    UpdatedAt = DateTime.UtcNow,
                    Techniques = techniques.OrderBy(t => t.Name).ToList()
                };

                var json = JsonSerializer.Serialize(document, _serializerOptions);
                await File.WriteAllTextAsync(GetProjectTechniquePath(projectId), json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存功法体系数据失败，ProjectId: {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<List<TechniqueSystemViewModel>> ImportTechniqueSystemsAsync(string filePath)
        {
            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var document = JsonSerializer.Deserialize<TechniqueProjectDocument>(json, _serializerOptions);
                return document?.Techniques ?? new List<TechniqueSystemViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入功法体系数据失败，FilePath: {FilePath}", filePath);
                throw;
            }
        }

        public async Task ExportTechniqueSystemsAsync(Guid projectId, IEnumerable<TechniqueSystemViewModel> techniques, string filePath)
        {
            try
            {
                var document = new TechniqueProjectDocument
                {
                    ProjectId = projectId,
                    UpdatedAt = DateTime.UtcNow,
                    Techniques = techniques.OrderBy(t => t.Name).ToList()
                };

                var json = JsonSerializer.Serialize(document, _serializerOptions);
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出功法体系数据失败，ProjectId: {ProjectId}", projectId);
                throw;
            }
        }

        private string GetProjectTechniquePath(Guid projectId)
        {
            return Path.Combine(_techniqueDirectory, $"{projectId:N}.json");
        }
    }

    /// <summary>
    /// 项目功法体系文档。
    /// </summary>
    public sealed class TechniqueProjectDocument
    {
        public Guid ProjectId { get; init; }
        public DateTime UpdatedAt { get; init; }
        public List<TechniqueSystemViewModel> Techniques { get; init; } = new();
    }
}
