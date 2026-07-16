using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NovelManagement.WPF.Views;

namespace NovelManagement.WPF.Services
{
    /// <summary>
    /// 商业体系数据服务。
    /// </summary>
    public class BusinessDataService
    {
        private readonly ILogger<BusinessDataService> _logger;
        private readonly string _businessDirectory;
        private readonly JsonSerializerOptions _serializerOptions = new()
        {
            WriteIndented = true
        };

        public BusinessDataService(ILogger<BusinessDataService> logger)
        {
            _logger = logger;
            _businessDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "NovelManagement",
                "businesses");
            Directory.CreateDirectory(_businessDirectory);
        }

        public async Task<List<BusinessSystemViewModel>> LoadBusinessSystemsAsync(Guid projectId)
        {
            try
            {
                var filePath = GetProjectBusinessPath(projectId);
                if (!File.Exists(filePath))
                {
                    return new List<BusinessSystemViewModel>();
                }

                var json = await File.ReadAllTextAsync(filePath);
                var document = JsonSerializer.Deserialize<BusinessProjectDocument>(json, _serializerOptions);
                return document?.BusinessSystems ?? new List<BusinessSystemViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载商业体系数据失败，ProjectId: {ProjectId}", projectId);
                throw;
            }
        }

        public async Task SaveBusinessSystemsAsync(Guid projectId, IEnumerable<BusinessSystemViewModel> businessSystems)
        {
            try
            {
                var document = new BusinessProjectDocument
                {
                    ProjectId = projectId,
                    UpdatedAt = DateTime.UtcNow,
                    BusinessSystems = businessSystems.OrderBy(system => system.Name).ToList()
                };

                var json = JsonSerializer.Serialize(document, _serializerOptions);
                await File.WriteAllTextAsync(GetProjectBusinessPath(projectId), json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存商业体系数据失败，ProjectId: {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<List<BusinessSystemViewModel>> ImportBusinessSystemsAsync(string filePath)
        {
            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var document = JsonSerializer.Deserialize<BusinessProjectDocument>(json, _serializerOptions);
                return document?.BusinessSystems ?? new List<BusinessSystemViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入商业体系数据失败，FilePath: {FilePath}", filePath);
                throw;
            }
        }

        public async Task ExportBusinessSystemsAsync(Guid projectId, IEnumerable<BusinessSystemViewModel> businessSystems, string filePath)
        {
            try
            {
                var document = new BusinessProjectDocument
                {
                    ProjectId = projectId,
                    UpdatedAt = DateTime.UtcNow,
                    BusinessSystems = businessSystems.OrderBy(system => system.Name).ToList()
                };

                var json = JsonSerializer.Serialize(document, _serializerOptions);
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出商业体系数据失败，ProjectId: {ProjectId}", projectId);
                throw;
            }
        }

        private string GetProjectBusinessPath(Guid projectId)
        {
            return Path.Combine(_businessDirectory, $"{projectId:N}.json");
        }
    }

    /// <summary>
    /// 项目商业体系文档。
    /// </summary>
    public sealed class BusinessProjectDocument
    {
        public Guid ProjectId { get; init; }
        public DateTime UpdatedAt { get; init; }
        public List<BusinessSystemViewModel> BusinessSystems { get; init; } = new();
    }
}
