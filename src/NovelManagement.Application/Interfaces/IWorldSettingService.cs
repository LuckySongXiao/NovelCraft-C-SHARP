using NovelManagement.Application.DTOs;

namespace NovelManagement.Application.Interfaces;

/// <summary>
/// 世界设定服务接口
/// </summary>
public interface IWorldSettingService
{
    /// <summary>
    /// 获取世界设定列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>世界设定列表</returns>
    Task<IEnumerable<WorldSettingDto>> GetAllAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据ID获取世界设定
    /// </summary>
    /// <param name="id">设定ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>世界设定对象</returns>
    Task<WorldSettingDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据类型获取世界设定列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="type">设定类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>世界设定列表</returns>
    Task<IEnumerable<WorldSettingDto>> GetByTypeAsync(Guid projectId, string type, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据分类获取世界设定列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="category">设定分类</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>世界设定列表</returns>
    Task<IEnumerable<WorldSettingDto>> GetByCategoryAsync(Guid projectId, string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取世界设定及其子设定
    /// </summary>
    /// <param name="id">设定ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>世界设定对象</returns>
    Task<WorldSettingDto?> GetWithChildrenAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取根级设定列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>根级设定列表</returns>
    Task<IEnumerable<WorldSettingDto>> GetRootSettingsAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据重要性获取世界设定列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="minImportance">最小重要性</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>世界设定列表</returns>
    Task<IEnumerable<WorldSettingDto>> GetByImportanceAsync(Guid projectId, int minImportance, CancellationToken cancellationToken = default);

    /// <summary>
    /// 搜索世界设定
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="searchTerm">搜索关键词</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>世界设定列表</returns>
    Task<IEnumerable<WorldSettingDto>> SearchAsync(Guid projectId, string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建世界设定
    /// </summary>
    /// <param name="createDto">创建DTO</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建的世界设定</returns>
    Task<WorldSettingDto> CreateAsync(CreateWorldSettingDto createDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新世界设定
    /// </summary>
    /// <param name="updateDto">更新DTO</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新的世界设定</returns>
    Task<WorldSettingDto> UpdateAsync(UpdateWorldSettingDto updateDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除世界设定
    /// </summary>
    /// <param name="id">设定ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否删除成功</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量删除世界设定
    /// </summary>
    /// <param name="ids">设定ID列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除的数量</returns>
    Task<int> DeleteBatchAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// 复制世界设定
    /// </summary>
    /// <param name="id">源设定ID</param>
    /// <param name="newName">新设定名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>复制的世界设定</returns>
    Task<WorldSettingDto> CopyAsync(Guid id, string newName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 移动世界设定到新的父级
    /// </summary>
    /// <param name="id">设定ID</param>
    /// <param name="newParentId">新父级ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新的世界设定</returns>
    Task<WorldSettingDto> MoveAsync(Guid id, Guid? newParentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新排序索引
    /// </summary>
    /// <param name="id">设定ID</param>
    /// <param name="newOrderIndex">新排序索引</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否更新成功</returns>
    Task<bool> UpdateOrderIndexAsync(Guid id, int newOrderIndex, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取设定类型列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>设定类型列表</returns>
    Task<IEnumerable<string>> GetTypesAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取设定分类列表
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>设定分类列表</returns>
    Task<IEnumerable<string>> GetCategoriesAsync(Guid projectId, CancellationToken cancellationToken = default);
}
