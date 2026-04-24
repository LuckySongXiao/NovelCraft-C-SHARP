using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NovelManagement.WPF.Views;

namespace NovelManagement.WPF.Services
{
    /// <summary>
    /// 职业体系数据服务。
    /// </summary>
    public class ProfessionDataService
    {
        private readonly ILogger<ProfessionDataService> _logger;
        private readonly string _professionDirectory;
        private readonly JsonSerializerOptions _serializerOptions = new()
        {
            WriteIndented = true
        };

        public ProfessionDataService(ILogger<ProfessionDataService> logger)
        {
            _logger = logger;
            _professionDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "NovelManagement",
                "professions");
            Directory.CreateDirectory(_professionDirectory);
        }

        public async Task<List<ProfessionSystemViewModel>> LoadProfessionSystemsAsync(Guid projectId)
        {
            try
            {
                var filePath = GetProjectProfessionPath(projectId);
                if (!File.Exists(filePath))
                {
                    return new List<ProfessionSystemViewModel>();
                }

                var json = await File.ReadAllTextAsync(filePath);
                var document = JsonSerializer.Deserialize<ProfessionProjectDocument>(json, _serializerOptions);
                return document?.ProfessionSystems ?? new List<ProfessionSystemViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载职业体系数据失败，ProjectId: {ProjectId}", projectId);
                throw;
            }
        }

        public async Task SaveProfessionSystemsAsync(Guid projectId, IEnumerable<ProfessionSystemViewModel> systems)
        {
            try
            {
                var document = new ProfessionProjectDocument
                {
                    ProjectId = projectId,
                    UpdatedAt = DateTime.UtcNow,
                    ProfessionSystems = systems.OrderBy(s => s.Name).ToList()
                };

                var json = JsonSerializer.Serialize(document, _serializerOptions);
                await File.WriteAllTextAsync(GetProjectProfessionPath(projectId), json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存职业体系数据失败，ProjectId: {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<List<ProfessionSystemViewModel>> ImportProfessionSystemsAsync(string filePath)
        {
            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var document = JsonSerializer.Deserialize<ProfessionProjectDocument>(json, _serializerOptions);
                return document?.ProfessionSystems ?? new List<ProfessionSystemViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入职业体系数据失败，FilePath: {FilePath}", filePath);
                throw;
            }
        }

        public async Task ExportProfessionSystemsAsync(Guid projectId, IEnumerable<ProfessionSystemViewModel> systems, string filePath)
        {
            try
            {
                var document = new ProfessionProjectDocument
                {
                    ProjectId = projectId,
                    UpdatedAt = DateTime.UtcNow,
                    ProfessionSystems = systems.OrderBy(s => s.Name).ToList()
                };

                var json = JsonSerializer.Serialize(document, _serializerOptions);
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出职业体系数据失败，ProjectId: {ProjectId}", projectId);
                throw;
            }
        }

        private string GetProjectProfessionPath(Guid projectId)
        {
            return Path.Combine(_professionDirectory, $"{projectId:N}.json");
        }
    }

    /// <summary>
    /// 项目职业体系文档。
    /// </summary>
    public sealed class ProfessionProjectDocument
    {
        public Guid ProjectId { get; init; }
        public DateTime UpdatedAt { get; init; }
        public List<ProfessionSystemViewModel> ProfessionSystems { get; init; } = new();
    }
}
