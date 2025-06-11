using NovelManagement.Application.DTOs;

namespace NovelManagement.Application.Interfaces;

/// <summary>
/// 修炼体系服务接口
/// </summary>
public interface ICultivationSystemService
{
    /// <summary>
    /// 获取修炼体系列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>修炼体系列表</returns>
    Task<IEnumerable<CultivationSystemDto>> GetAllAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据ID获取修炼体系
    /// </summary>
    /// <param name="id">体系ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>修炼体系对象</returns>
    Task<CultivationSystemDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据类型获取修炼体系列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="type">体系类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>修炼体系列表</returns>
    Task<IEnumerable<CultivationSystemDto>> GetByTypeAsync(Guid projectId, string type, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取修炼体系及其等级
    /// </summary>
    /// <param name="id">体系ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>修炼体系对象</returns>
    Task<CultivationSystemDto?> GetWithLevelsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据重要性获取修炼体系列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="minImportance">最小重要性</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>修炼体系列表</returns>
    Task<IEnumerable<CultivationSystemDto>> GetByImportanceAsync(Guid projectId, int minImportance, CancellationToken cancellationToken = default);

    /// <summary>
    /// 搜索修炼体系
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="searchTerm">搜索关键词</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>修炼体系列表</returns>
    Task<IEnumerable<CultivationSystemDto>> SearchAsync(Guid projectId, string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建修炼体系
    /// </summary>
    /// <param name="createDto">创建DTO</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建的修炼体系</returns>
    Task<CultivationSystemDto> CreateAsync(CreateCultivationSystemDto createDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新修炼体系
    /// </summary>
    /// <param name="updateDto">更新DTO</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新的修炼体系</returns>
    Task<CultivationSystemDto> UpdateAsync(UpdateCultivationSystemDto updateDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除修炼体系
    /// </summary>
    /// <param name="id">体系ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否删除成功</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 复制修炼体系
    /// </summary>
    /// <param name="id">源体系ID</param>
    /// <param name="newName">新体系名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>复制的修炼体系</returns>
    Task<CultivationSystemDto> CopyAsync(Guid id, string newName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取体系类型列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>体系类型列表</returns>
    Task<IEnumerable<string>> GetTypesAsync(Guid projectId, CancellationToken cancellationToken = default);

    // 修炼等级相关方法
    /// <summary>
    /// 获取修炼等级列表
    /// </summary>
    /// <param name="cultivationSystemId">修炼体系ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>修炼等级列表</returns>
    Task<IEnumerable<CultivationLevelDto>> GetLevelsAsync(Guid cultivationSystemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据ID获取修炼等级
    /// </summary>
    /// <param name="id">等级ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>修炼等级对象</returns>
    Task<CultivationLevelDto?> GetLevelByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建修炼等级
    /// </summary>
    /// <param name="createDto">创建DTO</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建的修炼等级</returns>
    Task<CultivationLevelDto> CreateLevelAsync(CreateCultivationLevelDto createDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新修炼等级
    /// </summary>
    /// <param name="updateDto">更新DTO</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新的修炼等级</returns>
    Task<CultivationLevelDto> UpdateLevelAsync(UpdateCultivationLevelDto updateDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除修炼等级
    /// </summary>
    /// <param name="id">等级ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否删除成功</returns>
    Task<bool> DeleteLevelAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取下一个等级
    /// </summary>
    /// <param name="currentLevelId">当前等级ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>下一个等级对象</returns>
    Task<CultivationLevelDto?> GetNextLevelAsync(Guid currentLevelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取上一个等级
    /// </summary>
    /// <param name="currentLevelId">当前等级ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>上一个等级对象</returns>
    Task<CultivationLevelDto?> GetPreviousLevelAsync(Guid currentLevelId, CancellationToken cancellationToken = default);
}
