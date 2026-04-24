using NovelManagement.AI.Services.RWKV.Models;

namespace NovelManagement.AI.Services.RWKV
{
    /// <summary>
    /// RWKV 推理服务接口（基于 rwkv_lightning_libtorch）
    /// </summary>
    public interface IRwkvLightningService
    {
        /// <summary>
        /// 是否可用
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// 当前配置
        /// </summary>
        RwkvConfiguration Configuration { get; }

        /// <summary>
        /// 初始化服务
        /// </summary>
        Task<bool> InitializeAsync(RwkvConfiguration configuration);

        /// <summary>
        /// 测试连接
        /// </summary>
        Task<bool> TestConnectionAsync();

        /// <summary>
        /// 获取服务状态
        /// </summary>
        Task<RwkvStatusResponse> GetStatusAsync();

        /// <summary>
        /// 续写文本（核心方法，单次最多200字）
        /// </summary>
        /// <param name="prompt">前文内容</param>
        /// <param name="maxTokens">最大生成 token 数（不超过200）</param>
        /// <param name="direction">续写方向/要求</param>
        /// <param name="temperature">温度参数（可选，使用配置默认值）</param>
        /// <returns>续写结果</returns>
        Task<RwkvCompletionResponse> CompleteAsync(string prompt, int maxTokens = 200, string? direction = null, double? temperature = null);

        /// <summary>
        /// 流式续写文本
        /// </summary>
        /// <param name="prompt">前文内容</param>
        /// <param name="maxTokens">最大生成 token 数</param>
        /// <param name="onTokenReceived">token 接收回调</param>
        /// <param name="direction">续写方向/要求</param>
        /// <param name="temperature">温度参数</param>
        /// <returns>完整续写结果</returns>
        Task<RwkvCompletionResponse> CompleteStreamAsync(string prompt, int maxTokens = 200, Action<string>? onTokenReceived = null, string? direction = null, double? temperature = null);

        /// <summary>
        /// 加载模型
        /// </summary>
        Task<bool> LoadModelAsync(string modelPath, string strategy);

        /// <summary>
        /// 卸载载模型
        /// </summary>
        Task UnloadModelAsync();
    }
}
