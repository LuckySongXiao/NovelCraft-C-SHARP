using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NovelManagement.WPF.Views;

namespace NovelManagement.WPF.Services
{
    /// <summary>
    /// 地图结构数据服务。
    /// </summary>
    public class MapDataService
    {
        private readonly ILogger<MapDataService> _logger;
        private readonly string _mapDirectory;
        private readonly JsonSerializerOptions _serializerOptions = new()
        {
            WriteIndented = true
        };

        public MapDataService(ILogger<MapDataService> logger)
        {
            _logger = logger;
            _mapDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "NovelManagement",
                "maps");
            Directory.CreateDirectory(_mapDirectory);
        }

        public async Task<List<MapViewModel>> LoadMapsAsync(Guid projectId)
        {
            try
            {
                var filePath = GetProjectMapPath(projectId);
                if (!File.Exists(filePath))
                {
                    return new List<MapViewModel>();
                }

                var json = await File.ReadAllTextAsync(filePath);
                var document = JsonSerializer.Deserialize<MapProjectDocument>(json, _serializerOptions);
                return document?.Maps ?? new List<MapViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载地图结构数据失败，ProjectId: {ProjectId}", projectId);
                throw;
            }
        }

        public async Task SaveMapsAsync(Guid projectId, IEnumerable<MapViewModel> maps)
        {
            try
            {
                var document = new MapProjectDocument
                {
                    ProjectId = projectId,
                    UpdatedAt = DateTime.UtcNow,
                    Maps = maps.OrderBy(map => map.Name).ToList()
                };

                var json = JsonSerializer.Serialize(document, _serializerOptions);
                await File.WriteAllTextAsync(GetProjectMapPath(projectId), json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存地图结构数据失败，ProjectId: {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<List<MapViewModel>> ImportMapsAsync(string filePath)
        {
            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var document = JsonSerializer.Deserialize<MapProjectDocument>(json, _serializerOptions);
                return document?.Maps ?? new List<MapViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入地图结构数据失败，FilePath: {FilePath}", filePath);
                throw;
            }
        }

        public async Task ExportMapsAsync(Guid projectId, IEnumerable<MapViewModel> maps, string filePath)
        {
            try
            {
                var document = new MapProjectDocument
                {
                    ProjectId = projectId,
                    UpdatedAt = DateTime.UtcNow,
                    Maps = maps.OrderBy(map => map.Name).ToList()
                };

                var json = JsonSerializer.Serialize(document, _serializerOptions);
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出地图结构数据失败，ProjectId: {ProjectId}", projectId);
                throw;
            }
        }

        private string GetProjectMapPath(Guid projectId)
        {
            return Path.Combine(_mapDirectory, $"{projectId:N}.json");
        }
    }

    /// <summary>
    /// 项目地图结构文档。
    /// </summary>
    public sealed class MapProjectDocument
    {
        public Guid ProjectId { get; init; }
        public DateTime UpdatedAt { get; init; }
        public List<MapViewModel> Maps { get; init; } = new();
    }
}
