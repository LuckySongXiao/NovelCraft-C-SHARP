using NovelManagement.AI.Interfaces;
using NovelManagement.AI.Services.Ollama.Models;

namespace NovelManagement.AI.Services.Ollama
{
    /// <summary>
    /// Ollama API服务接口
    /// </summary>
    public interface IOllamaApiService : IModelProvider
    {
        /// <summary>
        /// 模型下载进度事件
        /// </summary>
        event EventHandler<ModelDownloadProgressEventArgs>? ModelDownloadProgress;

        /// <summary>
        /// 获取当前配置
        /// </summary>
        /// <returns>配置信息</returns>
        OllamaConfiguration GetConfiguration();

        /// <summary>
        /// 更新配置
        /// </summary>
        /// <param name="configuration">新配置</param>
        /// <returns>是否成功</returns>
        Task<bool> UpdateConfigurationAsync(OllamaConfiguration configuration);

        /// <summary>
        /// 获取Ollama版本信息
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>版本信息</returns>
        Task<string> GetVersionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查模型是否存在
        /// </summary>
        /// <param name="modelName">模型名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否存在</returns>
        Task<bool> ModelExistsAsync(string modelName, CancellationToken cancellationToken = default);

        /// <summary>
        /// 拉取模型
        /// </summary>
        /// <param name="modelName">模型名称</param>
        /// <param name="onProgress">进度回调</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否成功</returns>
        Task<bool> PullModelAsync(string modelName, Action<ModelDownloadProgress>? onProgress = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除模型
        /// </summary>
        /// <param name="modelName">模型名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否成功</returns>
        Task<bool> DeleteModelAsync(string modelName, CancellationToken cancellationToken = default);

        /// <summary>
        /// 生成文本（非聊天模式）
        /// </summary>
        /// <param name="request">生成请求</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>生成响应</returns>
        Task<OllamaGenerateResponse> GenerateAsync(OllamaGenerateRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// 流式生成文本
        /// </summary>
        /// <param name="request">生成请求</param>
        /// <param name="onChunkReceived">数据块接收回调</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>最终响应</returns>
        Task<OllamaGenerateResponse> GenerateStreamAsync(OllamaGenerateRequest request, Action<OllamaGenerateResponse> onChunkReceived, CancellationToken cancellationToken = default);

        /// <summary>
        /// 预加载模型
        /// </summary>
        /// <param name="modelName">模型名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否成功</returns>
        Task<bool> PreloadModelAsync(string modelName, CancellationToken cancellationToken = default);

        /// <summary>
        /// 卸载模型
        /// </summary>
        /// <param name="modelName">模型名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否成功</returns>
        Task<bool> UnloadModelAsync(string modelName, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取模型详细信息
        /// </summary>
        /// <param name="modelName">模型名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>模型信息</returns>
        Task<OllamaModelInfo?> GetModelInfoAsync(string modelName, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取服务器状态
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>服务器状态</returns>
        Task<OllamaServerStatus> GetServerStatusAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量处理聊天请求
        /// </summary>
        /// <param name="requests">请求列表</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>响应列表</returns>
        Task<List<ChatResponse>> BatchChatAsync(List<ChatRequest> requests, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 模型下载进度事件参数
    /// </summary>
    public class ModelDownloadProgressEventArgs : EventArgs
    {
        /// <summary>
        /// 模型名称
        /// </summary>
        public string ModelName { get; }

        /// <summary>
        /// 下载进度
        /// </summary>
        public ModelDownloadProgress Progress { get; }

        public ModelDownloadProgressEventArgs(string modelName, ModelDownloadProgress progress)
        {
            ModelName = modelName;
            Progress = progress;
        }
    }

    /// <summary>
    /// 模型下载进度
    /// </summary>
    public class ModelDownloadProgress
    {
        /// <summary>
        /// 状态
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 总大小（字节）
        /// </summary>
        public long TotalBytes { get; set; }

        /// <summary>
        /// 已下载大小（字节）
        /// </summary>
        public long DownloadedBytes { get; set; }

        /// <summary>
        /// 下载百分比
        /// </summary>
        public double ProgressPercentage => TotalBytes > 0 ? (double)DownloadedBytes / TotalBytes * 100 : 0;

        /// <summary>
        /// 是否完成
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 下载速度（字节/秒）
        /// </summary>
        public long DownloadSpeed { get; set; }

        /// <summary>
        /// 预计剩余时间
        /// </summary>
        public TimeSpan? EstimatedTimeRemaining { get; set; }
    }

    /// <summary>
    /// Ollama服务器状态
    /// </summary>
    public class OllamaServerStatus
    {
        /// <summary>
        /// 是否在线
        /// </summary>
        public bool IsOnline { get; set; }

        /// <summary>
        /// 版本信息
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// 已加载的模型列表
        /// </summary>
        public List<string> LoadedModels { get; set; } = new();

        /// <summary>
        /// 可用内存（字节）
        /// </summary>
        public long AvailableMemory { get; set; }

        /// <summary>
        /// 已使用内存（字节）
        /// </summary>
        public long UsedMemory { get; set; }

        /// <summary>
        /// GPU信息
        /// </summary>
        public List<GpuInfo> GpuInfo { get; set; } = new();

        /// <summary>
        /// 响应时间
        /// </summary>
        public TimeSpan ResponseTime { get; set; }

        /// <summary>
        /// 最后检查时间
        /// </summary>
        public DateTime LastCheckTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// GPU信息
    /// </summary>
    public class GpuInfo
    {
        /// <summary>
        /// GPU ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// GPU名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 总内存（字节）
        /// </summary>
        public long TotalMemory { get; set; }

        /// <summary>
        /// 可用内存（字节）
        /// </summary>
        public long AvailableMemory { get; set; }

        /// <summary>
        /// 使用率百分比
        /// </summary>
        public double UtilizationPercentage { get; set; }

        /// <summary>
        /// 温度（摄氏度）
        /// </summary>
        public double Temperature { get; set; }

        /// <summary>
        /// 是否支持
        /// </summary>
        public bool IsSupported { get; set; }
    }
}
