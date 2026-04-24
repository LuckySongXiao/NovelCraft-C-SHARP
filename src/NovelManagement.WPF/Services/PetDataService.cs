using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NovelManagement.WPF.Views;

namespace NovelManagement.WPF.Services
{
    /// <summary>
    /// 宠物体系数据服务。
    /// </summary>
    public class PetDataService
    {
        private readonly ILogger<PetDataService> _logger;
        private readonly string _petDirectory;
        private readonly JsonSerializerOptions _serializerOptions = new()
        {
            WriteIndented = true
        };

        public PetDataService(ILogger<PetDataService> logger)
        {
            _logger = logger;
            _petDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "NovelManagement",
                "pets");
            Directory.CreateDirectory(_petDirectory);
        }

        public async Task<List<PetViewModel>> LoadPetsAsync(Guid projectId)
        {
            try
            {
                var filePath = GetProjectPetPath(projectId);
                if (!File.Exists(filePath))
                {
                    return new List<PetViewModel>();
                }

                var json = await File.ReadAllTextAsync(filePath);
                var document = JsonSerializer.Deserialize<PetProjectDocument>(json, _serializerOptions);
                return document?.Pets ?? new List<PetViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载宠物体系数据失败，ProjectId: {ProjectId}", projectId);
                throw;
            }
        }

        public async Task SavePetsAsync(Guid projectId, IEnumerable<PetViewModel> pets)
        {
            try
            {
                var document = new PetProjectDocument
                {
                    ProjectId = projectId,
                    UpdatedAt = DateTime.UtcNow,
                    Pets = pets.OrderBy(p => p.Name).ToList()
                };

                var json = JsonSerializer.Serialize(document, _serializerOptions);
                await File.WriteAllTextAsync(GetProjectPetPath(projectId), json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存宠物体系数据失败，ProjectId: {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<List<PetViewModel>> ImportPetsAsync(string filePath)
        {
            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var document = JsonSerializer.Deserialize<PetProjectDocument>(json, _serializerOptions);
                return document?.Pets ?? new List<PetViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入宠物体系数据失败，FilePath: {FilePath}", filePath);
                throw;
            }
        }

        public async Task ExportPetsAsync(Guid projectId, IEnumerable<PetViewModel> pets, string filePath)
        {
            try
            {
                var document = new PetProjectDocument
                {
                    ProjectId = projectId,
                    UpdatedAt = DateTime.UtcNow,
                    Pets = pets.OrderBy(p => p.Name).ToList()
                };

                var json = JsonSerializer.Serialize(document, _serializerOptions);
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出宠物体系数据失败，ProjectId: {ProjectId}", projectId);
                throw;
            }
        }

        private string GetProjectPetPath(Guid projectId)
        {
            return Path.Combine(_petDirectory, $"{projectId:N}.json");
        }
    }

    /// <summary>
    /// 项目宠物体系文档。
    /// </summary>
    public sealed class PetProjectDocument
    {
        public Guid ProjectId { get; init; }
        public DateTime UpdatedAt { get; init; }
        public List<PetViewModel> Pets { get; init; } = new();
    }
}
