using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using NovelManagement.AI.Interfaces;
using NovelManagement.AI.Services.OpenAICompatible.Models;

namespace NovelManagement.AI.Services.OpenAICompatible
{
    /// <summary>
    /// OpenAI 兼容接口提供者（支持智谱、Ollama、自定义 OpenAI 端点）
    /// </summary>
    public class OpenAICompatibleProvider : IModelProvider, IDisposable
    {
        private readonly ILogger<OpenAICompatibleProvider> _logger;
        private readonly HttpClient _httpClient;
        private OpenAICompatibleConfiguration _configuration = new();
        private bool _isAvailable;
        private bool _disposed;
        private ProviderStatistics _statistics = new();

        /// <inheritdoc/>
        public string ProviderName => _configuration.ProviderName;

        /// <inheritdoc/>
        public ModelProviderType ProviderType => ModelProviderType.CloudAPI;

        /// <inheritdoc/>
        public bool IsAvailable => _isAvailable;

        /// <inheritdoc/>
        public event EventHandler<ModelConfigurationChangedEventArgs>? ConfigurationChanged;

        /// <inheritdoc/>
        public event EventHandler<ConnectionStatusChangedEventArgs>? ConnectionStatusChanged;

        public OpenAICompatibleProvider(ILogger<OpenAICompatibleProvider> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        /// <inheritdoc/>
        public async Task<bool> InitializeAsync(IModelConfiguration configuration, CancellationToken cancellationToken = default)
        {
            try
            {
                if (configuration is not OpenAICompatibleConfiguration config)
                {
                    _logger.LogError("配置类型不匹配，期望 OpenAICompatibleConfiguration");
                    return false;
                }

                _configuration = config;
                _httpClient.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);

                // 设置认证头
                if (!string.IsNullOrWhiteSpace(config.ApiKey))
                {
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.ApiKey);
                }

                // 添加自定义头
                foreach (var header in config.CustomHeaders)
                {
                    _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                }

                // 测试连接
                _isAvailable = await TestConnectionAsync(cancellationToken).ContinueWith(t => t.Result.IsSuccess, cancellationToken);

                _logger.LogInformation("OpenAI 兼容提供者初始化: {ProviderName} ({Kind}), 可用: {IsAvailable}",
                    config.ProviderName, config.ProviderKind, _isAvailable);

                return _isAvailable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenAI 兼容提供者初始化失败");
                _isAvailable = false;
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<ConnectionTestResult> TestConnectionAsync(CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.Now;
            try
            {
                // 尝试获取模型列表来验证连接
                var response = await _httpClient.GetAsync($"{_configuration.BaseUrl.TrimEnd('/')}/models", cancellationToken);
                var elapsed = DateTime.Now - startTime;

                if (response.IsSuccessStatusCode)
                {
                    _isAvailable = true;
                    ConnectionStatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs(true));
                    return new ConnectionTestResult { IsSuccess = true, ResponseTime = elapsed };
                }

                _isAvailable = false;
                ConnectionStatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs(false, $"HTTP {response.StatusCode}"));
                return new ConnectionTestResult { IsSuccess = false, ResponseTime = elapsed, ErrorMessage = $"HTTP {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                var elapsed = DateTime.Now - startTime;
                _isAvailable = false;
                ConnectionStatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs(false, ex.Message));
                return new ConnectionTestResult { IsSuccess = false, ResponseTime = elapsed, ErrorMessage = ex.Message };
            }
        }

        /// <inheritdoc/>
        public async Task<List<ModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_configuration.BaseUrl.TrimEnd('/')}/models", cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var modelsResponse = JsonSerializer.Deserialize<OpenAIModelsResponse>(content);

                if (modelsResponse?.Data == null) return new List<ModelInfo>();

                return modelsResponse.Data.Select(m => new ModelInfo
                {
                    Id = m.Id,
                    Name = m.Id,
                    Description = m.OwnedBy ?? string.Empty,
                    IsDownloaded = true
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取模型列表失败");
                return new List<ModelInfo>();
            }
        }

        /// <inheritdoc/>
        public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.Now;
            try
            {
                var openAIRequest = ConvertToOpenAIRequest(request);
                var json = JsonSerializer.Serialize(openAIRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_configuration.BaseUrl.TrimEnd('/')}/chat/completions", content, cancellationToken);
                var elapsed = DateTime.Now - startTime;

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    return new ChatResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = $"HTTP {response.StatusCode}: {errorContent}",
                        ResponseTime = elapsed
                    };
                }

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var openAIResponse = JsonSerializer.Deserialize<OpenAIChatResponse>(responseContent);

                if (openAIResponse == null)
                {
                    return new ChatResponse { IsSuccess = false, ErrorMessage = "响应解析失败", ResponseTime = elapsed };
                }

                var choice = openAIResponse.Choices?.FirstOrDefault();
                return new ChatResponse
                {
                    Id = openAIResponse.Id ?? string.Empty,
                    Model = openAIResponse.Model ?? request.Model,
                    Content = choice?.Message?.Content ?? string.Empty,
                    FinishReason = choice?.FinishReason ?? string.Empty,
                    Usage = openAIResponse.Usage != null ? new TokenUsage
                    {
                        PromptTokens = openAIResponse.Usage.PromptTokens,
                        CompletionTokens = openAIResponse.Usage.CompletionTokens,
                        TotalTokens = openAIResponse.Usage.TotalTokens
                    } : null,
                    IsSuccess = true,
                    ResponseTime = elapsed
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chat 请求失败");
                return new ChatResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    ResponseTime = DateTime.Now - startTime
                };
            }
        }

        /// <inheritdoc/>
        public async Task<ChatResponse> ChatStreamAsync(ChatRequest request, Action<ChatChunk> onChunkReceived, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.Now;
            var fullContent = new StringBuilder();

            try
            {
                var openAIRequest = ConvertToOpenAIRequest(request);
                openAIRequest.Stream = true;
                var json = JsonSerializer.Serialize(openAIRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_configuration.BaseUrl.TrimEnd('/')}/chat/completions")
                {
                    Content = content
                };

                using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var reader = new StreamReader(stream);

                string? finishReason = null;
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync(cancellationToken);
                    if (string.IsNullOrEmpty(line)) continue;
                    if (!line.StartsWith("data: ")) continue;

                    var data = line[6..];
                    if (data == "[DONE]") break;

                    try
                    {
                        var chunk = JsonSerializer.Deserialize<OpenAIStreamChunk>(data);
                        var delta = chunk?.Choices?.FirstOrDefault()?.Delta;
                        if (delta?.Content != null)
                        {
                            fullContent.Append(delta.Content);
                            onChunkReceived(new ChatChunk
                            {
                                Id = chunk?.Id ?? string.Empty,
                                Content = delta.Content,
                                IsComplete = false
                            });
                        }

                        finishReason ??= chunk?.Choices?.FirstOrDefault()?.FinishReason;
                    }
                    catch (JsonException) { /* 忽略解析错误 */ }
                }

                onChunkReceived(new ChatChunk { IsComplete = true, FinishReason = finishReason });

                return new ChatResponse
                {
                    Content = fullContent.ToString(),
                    Model = request.Model,
                    FinishReason = finishReason ?? "stop",
                    IsSuccess = true,
                    ResponseTime = DateTime.Now - startTime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "流式 Chat 请求失败");
                return new ChatResponse
                {
                    Content = fullContent.ToString(),
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    ResponseTime = DateTime.Now - startTime
                };
            }
        }

        /// <inheritdoc/>
        public Task<ProviderStatistics> GetStatisticsAsync() => Task.FromResult(_statistics);

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }

        /// <summary>
        /// 转换为 OpenAI 格式请求
        /// </summary>
        private OpenAIChatRequest ConvertToOpenAIRequest(ChatRequest request)
        {
            var messages = new List<OpenAIMessage>();

            if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
            {
                messages.Add(new OpenAIMessage { Role = "system", Content = request.SystemPrompt });
            }

            foreach (var msg in request.Messages)
            {
                messages.Add(new OpenAIMessage { Role = msg.Role, Content = msg.Content });
            }

            return new OpenAIChatRequest
            {
                Model = string.IsNullOrWhiteSpace(request.Model) ? _configuration.DefaultModel : request.Model,
                Messages = messages,
                Temperature = request.Temperature > 0 ? request.Temperature : _configuration.DefaultTemperature,
                MaxTokens = request.MaxTokens > 0 ? request.MaxTokens : _configuration.DefaultMaxTokens,
                Stream = request.Stream
            };
        }

        #region OpenAI API 数据模型

        private class OpenAIChatRequest
        {
            [JsonPropertyName("model")] public string Model { get; set; } = string.Empty;
            [JsonPropertyName("messages")] public List<OpenAIMessage> Messages { get; set; } = new();
            [JsonPropertyName("temperature")] public double Temperature { get; set; } = 0.7;
            [JsonPropertyName("max_tokens")] public int MaxTokens { get; set; } = 4000;
            [JsonPropertyName("stream")] public bool Stream { get; set; }
        }

        private class OpenAIMessage
        {
            [JsonPropertyName("role")] public string Role { get; set; } = string.Empty;
            [JsonPropertyName("content")] public string Content { get; set; } = string.Empty;
        }

        private class OpenAIChatResponse
        {
            [JsonPropertyName("id")] public string? Id { get; set; }
            [JsonPropertyName("model")] public string? Model { get; set; }
            [JsonPropertyName("choices")] public List<OpenAIChoice>? Choices { get; set; }
            [JsonPropertyName("usage")] public OpenAIUsage? Usage { get; set; }
        }

        private class OpenAIChoice
        {
            [JsonPropertyName("message")] public OpenAIMessage? Message { get; set; }
            [JsonPropertyName("finish_reason")] public string? FinishReason { get; set; }
            [JsonPropertyName("delta")] public OpenAIMessage? Delta { get; set; }
        }

        private class OpenAIUsage
        {
            [JsonPropertyName("prompt_tokens")] public int PromptTokens { get; set; }
            [JsonPropertyName("completion_tokens")] public int CompletionTokens { get; set; }
            [JsonPropertyName("total_tokens")] public int TotalTokens { get; set; }
        }

        private class OpenAIStreamChunk
        {
            [JsonPropertyName("id")] public string? Id { get; set; }
            [JsonPropertyName("choices")] public List<OpenAIChoice>? Choices { get; set; }
        }

        private class OpenAIModelsResponse
        {
            [JsonPropertyName("data")] public List<OpenAIModelItem>? Data { get; set; }
        }

        private class OpenAIModelItem
        {
            [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
            [JsonPropertyName("owned_by")] public string? OwnedBy { get; set; }
        }

        #endregion
    }
}
