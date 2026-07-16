using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NovelManagement.WPF.Views;

namespace NovelManagement.WPF.Services
{
    /// <summary>
    /// 维度结构数据服务。
    /// </summary>
    public class DimensionDataService
    {
        private readonly ILogger<DimensionDataService> _logger;
        private readonly string _dimensionDirectory;
        private readonly JsonSerializerOptions _serializerOptions = new()
        {
            WriteIndented = true
        };

        public DimensionDataService(ILogger<DimensionDataService> logger)
        {
            _logger = logger;
            _dimensionDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "NovelManagement",
                "dimensions");
            Directory.CreateDirectory(_dimensionDirectory);
        }

        public async Task<List<DimensionViewModel>> LoadDimensionsAsync(Guid projectId)
        {
            try
            {
                var filePath = GetProjectDimensionPath(projectId);
                if (!File.Exists(filePath))
                {
                    return new List<DimensionViewModel>();
                }

                var json = await File.ReadAllTextAsync(filePath);
                var document = JsonSerializer.Deserialize<DimensionProjectDocument>(json, _serializerOptions);
                return document?.Dimensions ?? new List<DimensionViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载维度结构数据失败，ProjectId: {ProjectId}", projectId);
                throw;
            }
        }

        public async Task SaveDimensionsAsync(Guid projectId, IEnumerable<DimensionViewModel> dimensions)
        {
            try
            {
                var document = new DimensionProjectDocument
                {
                    ProjectId = projectId,
                    UpdatedAt = DateTime.UtcNow,
                    Dimensions = dimensions.OrderBy(dimension => dimension.Name).ToList()
                };

                var json = JsonSerializer.Serialize(document, _serializerOptions);
                await File.WriteAllTextAsync(GetProjectDimensionPath(projectId), json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存维度结构数据失败，ProjectId: {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<List<DimensionViewModel>> ImportDimensionsAsync(string filePath)
        {
            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var document = JsonSerializer.Deserialize<DimensionProjectDocument>(json, _serializerOptions);
                return document?.Dimensions ?? new List<DimensionViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入维度结构数据失败，FilePath: {FilePath}", filePath);
                throw;
            }
        }

        public async Task ExportDimensionsAsync(Guid projectId, IEnumerable<DimensionViewModel> dimensions, string filePath)
        {
            try
            {
                var document = new DimensionProjectDocument
                {
                    ProjectId = projectId,
                    UpdatedAt = DateTime.UtcNow,
                    Dimensions = dimensions.OrderBy(dimension => dimension.Name).ToList()
                };

                var json = JsonSerializer.Serialize(document, _serializerOptions);
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出维度结构数据失败，ProjectId: {ProjectId}", projectId);
                throw;
            }
        }

        private string GetProjectDimensionPath(Guid projectId)
        {
            return Path.Combine(_dimensionDirectory, $"{projectId:N}.json");
        }
    }

    /// <summary>
    /// 项目维度结构文档。
    /// </summary>
    public sealed class DimensionProjectDocument
    {
        public Guid ProjectId { get; init; }
        public DateTime UpdatedAt { get; init; }
        public List<DimensionViewModel> Dimensions { get; init; } = new();
    }
}
