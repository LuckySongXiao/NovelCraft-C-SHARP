using NovelManagement.AI.Interfaces;
using NovelManagement.AI.Services.Zhipu.Models;

namespace NovelManagement.AI.Services.Zhipu
{
    /// <summary>
    /// Zhipu API服务接口
    /// </summary>
    public interface IZhipuApiService : IModelProvider
    {
        /// <summary>
        /// 获取当前配置
        /// </summary>
        /// <returns>配置信息</returns>
        ZhipuConfiguration GetConfiguration();

        /// <summary>
        /// 更新配置
        /// </summary>
        /// <param name="configuration">新配置</param>
        /// <returns>是否成功</returns>
        Task<bool> UpdateConfigurationAsync(ZhipuConfiguration configuration);
    }
}
