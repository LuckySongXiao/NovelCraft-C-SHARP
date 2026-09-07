using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NovelManagement.AI.Interfaces;

namespace NovelManagement.AI.Services.RWKV
{
    /// <summary>
    /// RWKV 原生模型提供者：把 IRwkvLightningService（原生 CUDA 路由 /v1/chat/completions）
    /// 适配为 IModelProvider，供 ModelManager / 双 Agent（MainAgent/SubAgent）调用。
    /// 说明：此 CUDA 构建不存在 /openai/v1 兼容路由（404），因此不能用通用 OpenAICompatibleProvider。
    /// </summary>
    public class RwkvModelProvider : IModelProvider
    {
        private readonly ILogger<RwkvModelProvider> _logger;
        private readonly IRwkvLightningService _rwkvService;
        private readonly object _statsLock = new();
        private long _totalRequests;
        private long _successfulRequests;
        private long _failedRequests;
        private long _totalTokensUsed;
        private DateTime? _lastRequestTime;
        private TimeSpan _accumulatedResponseTime;

        public RwkvModelProvider(ILogger<RwkvModelProvider> logger, IRwkvLightningService rwkvService)
        {
            _logger = logger;
            _rwkvService = rwkvService;
        }

        /// <inheritdoc/>
        public string ProviderName => "RWKV";

        /// <inheritdoc/>
        public ModelProviderType ProviderType => ModelProviderType.Local;

        /// <inheritdoc/>
        public bool IsAvailable => _rwkvService.IsAvailable;

        /// <inheritdoc/>
        public event EventHandler<ModelConfigurationChangedEventArgs>? ConfigurationChanged;

        /// <inheritdoc/>
        public event EventHandler<ConnectionStatusChangedEventArgs>? ConnectionStatusChanged;

        /// <inheritdoc/>
        public Task<bool> InitializeAsync(IModelConfiguration configuration, CancellationToken cancellationToken = default)
        {
            // RWKV 服务的初始化由 DI 注册处负责（绑定 AI:Providers:RWKV 配置），这里只做连通性确认。
            return _rwkvService.TestConnectionAsync();
        }

        /// <inheritdoc/>
        public async Task<ConnectionTestResult> TestConnectionAsync(CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            try
            {
                var available = await _rwkvService.TestConnectionAsync();
                return new ConnectionTestResult
                {
                    IsSuccess = available,
                    ResponseTime = DateTime.UtcNow - startTime,
                    ErrorMessage = available ? null : "RWKV 推理服务不可达"
                };
            }
            catch (Exception ex)
            {
                return new ConnectionTestResult
                {
                    IsSuccess = false,
                    ResponseTime = DateTime.UtcNow - startTime,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <inheritdoc/>
        public Task<List<ModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
        {
            var modelName = string.IsNullOrWhiteSpace(_rwkvService.Configuration.ModelName)
                ? "rwkv7-g1i"
                : _rwkvService.Configuration.ModelName;

            return Task.FromResult(new List<ModelInfo>
            {
                new()
                {
                    Id = modelName,
                    Name = modelName,
                    Description = $"本地 RWKV 模型：{_rwkvService.Configuration.ModelPath}",
                    IsDownloaded = true,
                    Capabilities = new List<string> { "chat", "completion", "state-session" }
                }
            });
        }

        /// <inheritdoc/>
        public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            RecordRequestStart();

            try
            {
                var prompt = BuildPrompt(request);
                var maxTokens = request.MaxTokens > 0 ? request.MaxTokens : 1000;
                var temperature = request.Temperature > 0 ? request.Temperature : _rwkvService.Configuration.Temperature;

                var completion = await _rwkvService.CompleteAsync(
                    prompt,
                    maxTokens: maxTokens,
                    temperature: temperature,
                    topP: _rwkvService.Configuration.TopP,
                    presencePenalty: _rwkvService.Configuration.PresencePenalty,
                    frequencyPenalty: _rwkvService.Configuration.FrequencyPenalty,
                    topK: _rwkvService.Configuration.TopK);

                var responseTime = DateTime.UtcNow - startTime;
                var response = new ChatResponse
                {
                    Model = ResolveModelName(),
                    Content = completion.Text ?? string.Empty,
                    FinishReason = completion.Success ? "stop" : "error",
                    IsSuccess = completion.Success,
                    ErrorMessage = completion.Error,
                    ResponseTime = responseTime,
                    Usage = new TokenUsage
                    {
                        CompletionTokens = completion.TokensGenerated,
                        PromptTokens = prompt.Length / 2, // 近似值（中文约 2 字符/词元）
                        TotalTokens = completion.TokensGenerated + prompt.Length / 2
                    }
                };

                RecordRequestEnd(completion.Success, responseTime, completion.TokensGenerated);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RWKV ChatAsync 调用失败");
                RecordRequestEnd(false, DateTime.UtcNow - startTime, 0);
                return new ChatResponse
                {
                    Model = ResolveModelName(),
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    ResponseTime = DateTime.UtcNow - startTime
                };
            }
        }

        /// <inheritdoc/>
        public async Task<ChatResponse> ChatStreamAsync(ChatRequest request, Action<ChatChunk> onChunkReceived, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            RecordRequestStart();

            try
            {
                var prompt = BuildPrompt(request);
                var maxTokens = request.MaxTokens > 0 ? request.MaxTokens : 1000;

                var completion = await _rwkvService.CompleteStreamAsync(
                    prompt,
                    maxTokens: maxTokens,
                    onTokenReceived: text =>
                    {
                        onChunkReceived?.Invoke(new ChatChunk
                        {
                            Id = Guid.NewGuid().ToString("N"),
                            Content = text,
                            IsComplete = false
                        });
                    },
                    temperature: request.Temperature > 0 ? request.Temperature : _rwkvService.Configuration.Temperature,
                    topP: _rwkvService.Configuration.TopP,
                    presencePenalty: _rwkvService.Configuration.PresencePenalty,
                    frequencyPenalty: _rwkvService.Configuration.FrequencyPenalty,
                    topK: _rwkvService.Configuration.TopK);

                onChunkReceived?.Invoke(new ChatChunk
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Content = string.Empty,
                    IsComplete = true,
                    FinishReason = completion.Success ? "stop" : "error"
                });

                var responseTime = DateTime.UtcNow - startTime;
                RecordRequestEnd(completion.Success, responseTime, completion.TokensGenerated);
                return new ChatResponse
                {
                    Model = ResolveModelName(),
                    Content = completion.Text ?? string.Empty,
                    FinishReason = completion.Success ? "stop" : "error",
                    IsSuccess = completion.Success,
                    ErrorMessage = completion.Error,
                    ResponseTime = responseTime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RWKV ChatStreamAsync 调用失败");
                RecordRequestEnd(false, DateTime.UtcNow - startTime, 0);
                return new ChatResponse
                {
                    Model = ResolveModelName(),
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    ResponseTime = DateTime.UtcNow - startTime
                };
            }
        }

        /// <inheritdoc/>
        public Task<ProviderStatistics> GetStatisticsAsync()
        {
            lock (_statsLock)
            {
                return Task.FromResult(new ProviderStatistics
                {
                    TotalRequests = _totalRequests,
                    SuccessfulRequests = _successfulRequests,
                    FailedRequests = _failedRequests,
                    AverageResponseTime = _totalRequests > 0
                        ? TimeSpan.FromTicks(_accumulatedResponseTime.Ticks / _totalRequests)
                        : TimeSpan.Zero,
                    TotalTokensUsed = _totalTokensUsed,
                    LastRequestTime = _lastRequestTime
                });
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            // IRwkvLightningService 为 DI 单例，生命周期由容器管理，这里不重复释放。
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 把 ChatRequest 的消息列表拼成 RWKV 续写提示词。
        /// RWKV 原生路由没有 messages 语义，采用“系统提示 + 各消息内容顺序拼接”的形式。
        /// 注意：\n\n 是 RWKV 轮次分隔符，各段之间用单个空行分隔即可。
        /// </summary>
        private static string BuildPrompt(ChatRequest request)
        {
            var builder = new StringBuilder();
            var systemPrompt = request.SystemPrompt;
            if (string.IsNullOrWhiteSpace(systemPrompt))
            {
                systemPrompt = request.Messages
                    .FirstOrDefault(message => string.Equals(message.Role, "system", StringComparison.OrdinalIgnoreCase))
                    ?.Content;
            }

            if (!string.IsNullOrWhiteSpace(systemPrompt))
            {
                builder.AppendLine(systemPrompt.TrimEnd());
                builder.AppendLine();
            }

            foreach (var message in request.Messages)
            {
                if (string.IsNullOrWhiteSpace(message.Content))
                {
                    continue;
                }

                builder.AppendLine(message.Content.TrimEnd().Replace("\n\n", "\n"));
                builder.AppendLine();
            }

            return builder.ToString().TrimEnd(' ', '\t');
        }

        private string ResolveModelName()
        {
            return string.IsNullOrWhiteSpace(_rwkvService.Configuration.ModelName)
                ? "rwkv7-g1i"
                : _rwkvService.Configuration.ModelName;
        }

        private void RecordRequestStart()
        {
            lock (_statsLock)
            {
                _totalRequests++;
                _lastRequestTime = DateTime.UtcNow;
            }
        }

        private void RecordRequestEnd(bool success, TimeSpan responseTime, int tokens)
        {
            lock (_statsLock)
            {
                if (success)
                {
                    _successfulRequests++;
                }
                else
                {
                    _failedRequests++;
                }

                _accumulatedResponseTime += responseTime;
                _totalTokensUsed += tokens;
            }
        }
    }
}
