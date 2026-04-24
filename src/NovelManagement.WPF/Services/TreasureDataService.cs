using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NovelManagement.WPF.Views;

namespace NovelManagement.WPF.Services
{
    /// <summary>
    /// 灵宝体系数据服务。
    /// </summary>
    public class TreasureDataService
    {
        private readonly ILogger<TreasureDataService> _logger;
        private readonly string _treasureDirectory;
        private readonly JsonSerializerOptions _serializerOptions = new()
        {
            WriteIndented = true
        };

        public TreasureDataService(ILogger<TreasureDataService> logger)
        {
            _logger = logger;
            _treasureDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "NovelManagement",
                "treasures");
            Directory.CreateDirectory(_treasureDirectory);
        }

        public async Task<List<TreasureViewModel>> LoadTreasuresAsync(Guid projectId)
        {
            try
            {
                var filePath = GetProjectTreasurePath(projectId);
                if (!File.Exists(filePath))
                {
                    return new List<TreasureViewModel>();
                }

                var json = await File.ReadAllTextAsync(filePath);
                var document = JsonSerializer.Deserialize<TreasureProjectDocument>(json, _serializerOptions);
                return document?.Treasures ?? new List<TreasureViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载灵宝体系数据失败，ProjectId: {ProjectId}", projectId);
                throw;
            }
        }

        public async Task SaveTreasuresAsync(Guid projectId, IEnumerable<TreasureViewModel> treasures)
        {
            try
            {
                var document = new TreasureProjectDocument
                {
                    ProjectId = projectId,
                    UpdatedAt = DateTime.UtcNow,
                    Treasures = treasures.OrderBy(t => t.Name).ToList()
                };

                var json = JsonSerializer.Serialize(document, _serializerOptions);
                await File.WriteAllTextAsync(GetProjectTreasurePath(projectId), json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存灵宝体系数据失败，ProjectId: {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<List<TreasureViewModel>> ImportTreasuresAsync(string filePath)
        {
            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var document = JsonSerializer.Deserialize<TreasureProjectDocument>(json, _serializerOptions);
                return document?.Treasures ?? new List<TreasureViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入灵宝体系数据失败，FilePath: {FilePath}", filePath);
                throw;
            }
        }

        public async Task ExportTreasuresAsync(Guid projectId, IEnumerable<TreasureViewModel> treasures, string filePath)
        {
            try
            {
                var document = new TreasureProjectDocument
                {
                    ProjectId = projectId,
                    UpdatedAt = DateTime.UtcNow,
                    Treasures = treasures.OrderBy(t => t.Name).ToList()
                };

                var json = JsonSerializer.Serialize(document, _serializerOptions);
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出灵宝体系数据失败，ProjectId: {ProjectId}", projectId);
                throw;
            }
        }

        private string GetProjectTreasurePath(Guid projectId)
        {
            return Path.Combine(_treasureDirectory, $"{projectId:N}.json");
        }
    }

    /// <summary>
    /// 项目灵宝体系文档。
    /// </summary>
    public sealed class TreasureProjectDocument
    {
        public Guid ProjectId { get; init; }
        public DateTime UpdatedAt { get; init; }
        public List<TreasureViewModel> Treasures { get; init; } = new();
    }
}
