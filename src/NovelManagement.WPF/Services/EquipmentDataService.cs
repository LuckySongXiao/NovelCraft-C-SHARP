using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NovelManagement.WPF.Views;

namespace NovelManagement.WPF.Services
{
    /// <summary>
    /// 装备体系数据服务。
    /// </summary>
    public class EquipmentDataService
    {
        private readonly ILogger<EquipmentDataService> _logger;
        private readonly string _equipmentDirectory;
        private readonly JsonSerializerOptions _serializerOptions = new()
        {
            WriteIndented = true
        };

        public EquipmentDataService(ILogger<EquipmentDataService> logger)
        {
            _logger = logger;
            _equipmentDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "NovelManagement",
                "equipments");
            Directory.CreateDirectory(_equipmentDirectory);
        }

        public async Task<List<EquipmentSystemViewModel>> LoadEquipmentSystemsAsync(Guid projectId)
        {
            try
            {
                var filePath = GetProjectEquipmentPath(projectId);
                if (!File.Exists(filePath))
                {
                    return new List<EquipmentSystemViewModel>();
                }

                var json = await File.ReadAllTextAsync(filePath);
                var document = JsonSerializer.Deserialize<EquipmentProjectDocument>(json, _serializerOptions);
                return document?.EquipmentSystems ?? new List<EquipmentSystemViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载装备体系数据失败，ProjectId: {ProjectId}", projectId);
                throw;
            }
        }

        public async Task SaveEquipmentSystemsAsync(Guid projectId, IEnumerable<EquipmentSystemViewModel> systems)
        {
            try
            {
                var document = new EquipmentProjectDocument
                {
                    ProjectId = projectId,
                    UpdatedAt = DateTime.UtcNow,
                    EquipmentSystems = systems.OrderBy(s => s.Name).ToList()
                };

                var json = JsonSerializer.Serialize(document, _serializerOptions);
                await File.WriteAllTextAsync(GetProjectEquipmentPath(projectId), json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存装备体系数据失败，ProjectId: {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<List<EquipmentSystemViewModel>> ImportEquipmentSystemsAsync(string filePath)
        {
            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var document = JsonSerializer.Deserialize<EquipmentProjectDocument>(json, _serializerOptions);
                return document?.EquipmentSystems ?? new List<EquipmentSystemViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入装备体系数据失败，FilePath: {FilePath}", filePath);
                throw;
            }
        }

        public async Task ExportEquipmentSystemsAsync(Guid projectId, IEnumerable<EquipmentSystemViewModel> systems, string filePath)
        {
            try
            {
                var document = new EquipmentProjectDocument
                {
                    ProjectId = projectId,
                    UpdatedAt = DateTime.UtcNow,
                    EquipmentSystems = systems.OrderBy(s => s.Name).ToList()
                };

                var json = JsonSerializer.Serialize(document, _serializerOptions);
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出装备体系数据失败，ProjectId: {ProjectId}", projectId);
                throw;
            }
        }

        private string GetProjectEquipmentPath(Guid projectId)
        {
            return Path.Combine(_equipmentDirectory, $"{projectId:N}.json");
        }
    }

    /// <summary>
    /// 项目装备体系文档。
    /// </summary>
    public sealed class EquipmentProjectDocument
    {
        public Guid ProjectId { get; init; }
        public DateTime UpdatedAt { get; init; }
        public List<EquipmentSystemViewModel> EquipmentSystems { get; init; } = new();
    }
}
