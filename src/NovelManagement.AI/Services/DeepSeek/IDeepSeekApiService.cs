using NovelManagement.AI.Services.DeepSeek.Models;
using NovelManagement.AI.Services.ThinkingChain.Models;
using ThinkingChainModel = NovelManagement.AI.Services.ThinkingChain.Models.ThinkingChain;

namespace NovelManagement.AI.Services.DeepSeek
{
    /// <summary>
    /// DeepSeek API服务接口
    /// </summary>
    public interface IDeepSeekApiService
    {
        /// <summary>
        /// 配置变更事件
        /// </summary>
        event EventHandler<DeepSeekConfiguration>? ConfigurationChanged;

        /// <summary>
        /// 思维链更新事件
        /// </summary>
        event EventHandler<ThinkingChainModel>? ThinkingChainUpdated;

        /// <summary>
        /// 获取当前配置
        /// </summary>
        /// <returns>配置信息</returns>
        DeepSeekConfiguration GetConfiguration();

        /// <summary>
        /// 更新配置
        /// </summary>
        /// <param name="configuration">新配置</param>
        /// <returns>是否成功</returns>
        Task<bool> UpdateConfigurationAsync(DeepSeekConfiguration configuration);

        /// <summary>
        /// 测试API连接
        /// </summary>
        /// <returns>测试结果</returns>
        Task<bool> TestConnectionAsync();

        /// <summary>
        /// 发送聊天请求
        /// </summary>
        /// <param name="request">请求参数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>响应结果</returns>
        Task<DeepSeekResponse> ChatAsync(DeepSeekRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// 发送流式聊天请求
        /// </summary>
        /// <param name="request">请求参数</param>
        /// <param name="onChunkReceived">接收到数据块时的回调</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>完整响应结果</returns>
        Task<DeepSeekResponse> ChatStreamAsync(
            DeepSeekRequest request,
            Action<DeepSeekStreamResponse>? onChunkReceived = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 发送带思维链的请求
        /// </summary>
        /// <param name="prompt">提示词</param>
        /// <param name="systemMessage">系统消息</param>
        /// <param name="onThinkingUpdated">思维链更新回调</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>响应结果和思维链</returns>
        Task<(DeepSeekResponse Response, ThinkingChainModel ThinkingChain)> ChatWithThinkingAsync(
            string prompt,
            string? systemMessage = null,
            Action<ThinkingChainModel>? onThinkingUpdated = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量处理请求
        /// </summary>
        /// <param name="requests">请求列表</param>
        /// <param name="maxConcurrency">最大并发数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>响应结果列表</returns>
        Task<List<DeepSeekResponse>> BatchChatAsync(
            List<DeepSeekRequest> requests,
            int maxConcurrency = 3,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取模型列表
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>模型列表</returns>
        Task<List<string>> GetModelsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取API使用统计
        /// </summary>
        /// <returns>使用统计</returns>
        Task<DeepSeekUsageStats> GetUsageStatsAsync();

        /// <summary>
        /// 清理资源
        /// </summary>
        void Dispose();
    }

    /// <summary>
    /// DeepSeek使用统计
    /// </summary>
    public class DeepSeekUsageStats
    {
        /// <summary>
        /// 总请求次数
        /// </summary>
        public int TotalRequests { get; set; }

        /// <summary>
        /// 成功请求次数
        /// </summary>
        public int SuccessfulRequests { get; set; }

        /// <summary>
        /// 失败请求次数
        /// </summary>
        public int FailedRequests { get; set; }

        /// <summary>
        /// 总令牌使用量
        /// </summary>
        public int TotalTokensUsed { get; set; }

        /// <summary>
        /// 平均响应时间（毫秒）
        /// </summary>
        public double AverageResponseTime { get; set; }

        /// <summary>
        /// 最后请求时间
        /// </summary>
        public DateTime? LastRequestTime { get; set; }

        /// <summary>
        /// 今日请求次数
        /// </summary>
        public int TodayRequests { get; set; }

        /// <summary>
        /// 今日令牌使用量
        /// </summary>
        public int TodayTokensUsed { get; set; }

        /// <summary>
        /// 成功率
        /// </summary>
        public double SuccessRate => TotalRequests > 0 ? (double)SuccessfulRequests / TotalRequests * 100 : 0;

        /// <summary>
        /// 失败率
        /// </summary>
        public double FailureRate => TotalRequests > 0 ? (double)FailedRequests / TotalRequests * 100 : 0;
    }
}
