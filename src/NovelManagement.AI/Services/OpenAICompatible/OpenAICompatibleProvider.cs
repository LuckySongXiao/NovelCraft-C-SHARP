using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using NovelManagement.AI.Interfaces;
using NovelManagement.AI.Services.OpenAICompatible.Models;
using NovelManagement.AI.Utilities;

namespace NovelManagement.AI.Services.OpenAICompatible
{
    /// <summary>
    /// OpenAI 兼容接口提供者（支持智谱、Ollama、自定义 OpenAI 端点）
    /// </summary>
    public class OpenAICompatibleProvider : IModelProvider, IDisposable
    {
        private readonly ILogger<OpenAICompatibleProvider> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _registeredProviderName;
        private OpenAICompatibleConfiguration _configuration = new();
        private bool _isAvailable;
        private bool _disposed;
        private ProviderStatistics _statistics = new();
        private readonly HashSet<string> _appliedCustomHeaderNames = new(StringComparer.OrdinalIgnoreCase);

        /// <inheritdoc/>
        public string ProviderName =>
            string.IsNullOrWhiteSpace(_configuration.ProviderName) ||
            (string.Equals(_configuration.ProviderName, "OpenAICompatible", StringComparison.OrdinalIgnoreCase)
             && !string.Equals(_registeredProviderName, "OpenAICompatible", StringComparison.OrdinalIgnoreCase))
                ? _registeredProviderName
                : _configuration.ProviderName;

        /// <inheritdoc/>
        public ModelProviderType ProviderType => ModelProviderType.CloudAPI;

        /// <inheritdoc/>
        public bool IsAvailable => _isAvailable;

        /// <inheritdoc/>
        public event EventHandler<ModelConfigurationChangedEventArgs>? ConfigurationChanged;

        /// <inheritdoc/>
        public event EventHandler<ConnectionStatusChangedEventArgs>? ConnectionStatusChanged;

        public OpenAICompatibleProvider(ILogger<OpenAICompatibleProvider> logger, HttpClient httpClient, string? registeredProviderName = null)
        {
            _logger = logger;
            _httpClient = httpClient;
            _registeredProviderName = string.IsNullOrWhiteSpace(registeredProviderName)
                ? "OpenAICompatible"
                : registeredProviderName.Trim();
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

                _configuration = NormalizeConfiguration(config);

                foreach (var headerName in _appliedCustomHeaderNames.ToList())
                {
                    _httpClient.DefaultRequestHeaders.Remove(headerName);
                }
                _appliedCustomHeaderNames.Clear();

                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                _httpClient.DefaultRequestHeaders.Authorization = null;
                _httpClient.DefaultRequestHeaders.UserAgent.Clear();
                _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("NovelCraft/1.0");

                if (!string.IsNullOrWhiteSpace(_configuration.ApiKey))
                {
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", _configuration.ApiKey);
                }

                if (string.Equals(_configuration.ProviderKind, "XiaoMiMiMo", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(_configuration.ApiKey))
                {
                    _httpClient.DefaultRequestHeaders.Remove("api-key");
                    if (_httpClient.DefaultRequestHeaders.TryAddWithoutValidation("api-key", _configuration.ApiKey))
                    {
                        _appliedCustomHeaderNames.Add("api-key");
                    }
                }

                foreach (var header in _configuration.CustomHeaders)
                {
                    if (string.IsNullOrWhiteSpace(header.Key) || string.Equals(header.Key, "Authorization", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    _httpClient.DefaultRequestHeaders.Remove(header.Key);
                    if (_httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value))
                    {
                        _appliedCustomHeaderNames.Add(header.Key);
                    }
                }

                _isAvailable = (await TestConnectionAsync(cancellationToken)).IsSuccess;

                _logger.LogInformation("OpenAI 兼容提供者初始化: {ProviderName} ({Kind}), 可用: {IsAvailable}",
                    _configuration.ProviderName, _configuration.ProviderKind, _isAvailable);

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
                var modelsRequest = new HttpRequestMessage(HttpMethod.Get, BuildEndpoint("models"));
                using var response = await SendAsyncWithTimeout(modelsRequest, cancellationToken);
                var elapsed = DateTime.Now - startTime;

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    var modelsResponse = JsonSerializer.Deserialize<OpenAIModelsResponse>(content);
                    _isAvailable = true;
                    ConnectionStatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs(true));
                    return new ConnectionTestResult
                    {
                        IsSuccess = true,
                        ResponseTime = elapsed,
                        ServerInfo = new Dictionary<string, object>
                        {
                            ["Endpoint"] = BuildEndpoint("models"),
                            ["ModelCount"] = modelsResponse?.Data?.Count ?? 0
                        }
                    };
                }

                var probeResult = await ProbeChatEndpointAsync(startTime, cancellationToken);
                if (probeResult.IsSuccess)
                {
                    return probeResult;
                }

                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _isAvailable = false;
                ConnectionStatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs(false, $"HTTP {response.StatusCode}"));
                return new ConnectionTestResult
                {
                    IsSuccess = false,
                    ResponseTime = elapsed,
                    ErrorMessage = $"HTTP {response.StatusCode}: {errorBody}"
                };
            }
            catch (Exception ex)
            {
                var probeResult = await ProbeChatEndpointAsync(startTime, cancellationToken);
                if (probeResult.IsSuccess)
                {
                    return probeResult;
                }

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
                var response = await GetAsyncWithTimeout(BuildEndpoint("models"), cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var modelsResponse = JsonSerializer.Deserialize<OpenAIModelsResponse>(content);

                if (modelsResponse?.Data == null || modelsResponse.Data.Count == 0)
                {
                    return BuildFallbackModelList();
                }

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
                return BuildFallbackModelList();
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

                var response = await PostAsyncWithTimeout(BuildEndpoint("chat/completions"), content, cancellationToken);
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
                var sanitizedContent = AIOutputSanitizer.ExtractVisibleContent(
                    choice?.Message?.Content,
                    choice?.Message?.ReasoningContent);

                return new ChatResponse
                {
                    Id = openAIResponse.Id ?? string.Empty,
                    Model = openAIResponse.Model ?? request.Model,
                    Content = sanitizedContent,
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

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, BuildEndpoint("chat/completions"))
                {
                    Content = content
                };

                using var response = await SendAsyncWithTimeout(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
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
                        var deltaContent = AIOutputSanitizer.ExtractVisibleContent(delta?.Content, delta?.ReasoningContent);
                        if (!string.IsNullOrWhiteSpace(deltaContent))
                        {
                            fullContent.Append(deltaContent);
                            onChunkReceived(new ChatChunk
                            {
                                Id = chunk?.Id ?? string.Empty,
                                Content = deltaContent,
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
                Temperature = NormalizeTemperature(request.Temperature),
                MaxTokens = UsesMaxCompletionTokens()
                    ? null
                    : request.MaxTokens > 0 ? request.MaxTokens : _configuration.DefaultMaxTokens,
                MaxCompletionTokens = UsesMaxCompletionTokens()
                    ? request.MaxTokens > 0 ? request.MaxTokens : _configuration.DefaultMaxTokens
                    : TryGetIntParameter(request.Parameters, "max_completion_tokens"),
                Stream = request.Stream,
                TopP = TryGetDoubleParameter(request.Parameters, "top_p"),
                FrequencyPenalty = TryGetDoubleParameter(request.Parameters, "frequency_penalty"),
                PresencePenalty = TryGetDoubleParameter(request.Parameters, "presence_penalty"),
                Stop = TryGetStopParameter(request.Parameters),
                AdditionalProperties = BuildAdditionalRequestFields(request.Parameters)
            };
        }

        private async Task<ConnectionTestResult> ProbeChatEndpointAsync(DateTime startTime, CancellationToken cancellationToken)
        {
            try
            {
                var probeRequest = new OpenAIChatRequest
                {
                    Model = _configuration.DefaultModel,
                    Messages = new List<OpenAIMessage>
                    {
                        new() { Role = "user", Content = "ping" }
                    },
                    Temperature = NormalizeTemperature(-1),
                    MaxTokens = 1,
                    Stream = false
                };

                using var response = await PostAsyncWithTimeout(
                    BuildEndpoint("chat/completions"),
                    new StringContent(JsonSerializer.Serialize(probeRequest), Encoding.UTF8, "application/json"),
                    cancellationToken);

                var elapsed = DateTime.Now - startTime;
                if (response.IsSuccessStatusCode)
                {
                    _isAvailable = true;
                    ConnectionStatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs(true));
                    return new ConnectionTestResult
                    {
                        IsSuccess = true,
                        ResponseTime = elapsed,
                        ServerInfo = new Dictionary<string, object>
                        {
                            ["Endpoint"] = BuildEndpoint("chat/completions"),
                            ["ProbeMode"] = "chat"
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "OpenAI 兼容提供者聊天探活失败");
            }

            _isAvailable = false;
            return new ConnectionTestResult
            {
                IsSuccess = false,
                ResponseTime = DateTime.Now - startTime,
                ErrorMessage = "模型列表与聊天探活均失败"
            };
        }

        private string BuildEndpoint(string relativePath)
        {
            return $"{_configuration.BaseUrl.TrimEnd('/')}/{relativePath.TrimStart('/')}";
        }

        private async Task<HttpResponseMessage> GetAsyncWithTimeout(string requestUri, CancellationToken cancellationToken)
        {
            using var timeoutCts = CreateTimeoutCancellationTokenSource(cancellationToken);
            return await _httpClient.GetAsync(requestUri, timeoutCts.Token);
        }

        private async Task<HttpResponseMessage> PostAsyncWithTimeout(string requestUri, HttpContent content, CancellationToken cancellationToken)
        {
            using var timeoutCts = CreateTimeoutCancellationTokenSource(cancellationToken);
            return await _httpClient.PostAsync(requestUri, content, timeoutCts.Token);
        }

        private async Task<HttpResponseMessage> SendAsyncWithTimeout(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using var timeoutCts = CreateTimeoutCancellationTokenSource(cancellationToken);
            return await _httpClient.SendAsync(request, timeoutCts.Token);
        }

        private async Task<HttpResponseMessage> SendAsyncWithTimeout(HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken)
        {
            using var timeoutCts = CreateTimeoutCancellationTokenSource(cancellationToken);
            return await _httpClient.SendAsync(request, completionOption, timeoutCts.Token);
        }

        private CancellationTokenSource CreateTimeoutCancellationTokenSource(CancellationToken cancellationToken)
        {
            var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(_configuration.TimeoutSeconds));
            return timeoutCts;
        }

        private static OpenAICompatibleConfiguration NormalizeConfiguration(OpenAICompatibleConfiguration config)
        {
            return new OpenAICompatibleConfiguration
            {
                ProviderName = string.IsNullOrWhiteSpace(config.ProviderName) ? "OpenAICompatible" : config.ProviderName.Trim(),
                ProviderKind = string.IsNullOrWhiteSpace(config.ProviderKind) ? "Custom" : config.ProviderKind.Trim(),
                BaseUrl = NormalizeBaseUrl(config.BaseUrl),
                ApiKey = config.ApiKey?.Trim() ?? string.Empty,
                DefaultModel = string.IsNullOrWhiteSpace(config.DefaultModel) ? "gpt-3.5-turbo" : config.DefaultModel.Trim(),
                TimeoutSeconds = config.TimeoutSeconds > 0 ? config.TimeoutSeconds : 120,
                MaxRetries = Math.Max(0, config.MaxRetries),
                DefaultTemperature = config.DefaultTemperature,
                DefaultMaxTokens = config.DefaultMaxTokens > 0 ? config.DefaultMaxTokens : 4000,
                EnableStreaming = config.EnableStreaming,
                CustomHeaders = new Dictionary<string, string>(config.CustomHeaders, StringComparer.OrdinalIgnoreCase)
            };
        }

        private static string NormalizeBaseUrl(string? baseUrl)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return "https://api.openai.com/v1";
            }

            return baseUrl.Trim().TrimEnd('/');
        }

        private List<ModelInfo> BuildFallbackModelList()
        {
            var models = new List<string>();

            if (!string.IsNullOrWhiteSpace(_configuration.DefaultModel))
            {
                models.Add(_configuration.DefaultModel);
            }

            if (string.Equals(_configuration.ProviderKind, "ZhipuAI", StringComparison.OrdinalIgnoreCase))
            {
                models.AddRange(new[] { "glm-4.7-flash", "glm-4-flash", "glm-4", "glm-4-plus", "glm-3-turbo" });
            }
            else if (string.Equals(_configuration.ProviderKind, "DeepSeek", StringComparison.OrdinalIgnoreCase))
            {
                models.AddRange(new[] { "deepseek-v4-flash", "deepseek-v4-pro", "deepseek-chat", "deepseek-reasoner" });
            }
            else if (string.Equals(_configuration.ProviderKind, "XiaoMiMiMo", StringComparison.OrdinalIgnoreCase))
            {
                models.AddRange(new[] { "mimo-v2.5-pro", "mimo-v2.5", "mimo-v2-flash" });
            }

            return models
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(model => new ModelInfo
                {
                    Id = model,
                    Name = model,
                    Description = $"{_configuration.ProviderName} 兼容接口模型",
                    IsDownloaded = true
                })
                .ToList();
        }

        private static double? TryGetDoubleParameter(IReadOnlyDictionary<string, object> parameters, string key)
        {
            if (!parameters.TryGetValue(key, out var value) || value == null)
            {
                return null;
            }

            return double.TryParse(value.ToString(), out var parsed) ? parsed : null;
        }

        private static int? TryGetIntParameter(IReadOnlyDictionary<string, object> parameters, string key)
        {
            if (!parameters.TryGetValue(key, out var value) || value == null)
            {
                return null;
            }

            return int.TryParse(value.ToString(), out var parsed) ? parsed : null;
        }

        private Dictionary<string, JsonElement>? BuildAdditionalRequestFields(IReadOnlyDictionary<string, object> parameters)
        {
            Dictionary<string, JsonElement>? additionalFields = null;
            foreach (var key in new[] { "thinking", "response_format", "tools", "tool_choice", "stream_options", "user" })
            {
                if (!parameters.TryGetValue(key, out var value) || value == null)
                {
                    continue;
                }

                additionalFields ??= new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
                additionalFields[key] = JsonSerializer.SerializeToElement(value);
            }

            return additionalFields;
        }

        private double NormalizeTemperature(double requestedTemperature)
        {
            var temperature = requestedTemperature >= 0 ? requestedTemperature : _configuration.DefaultTemperature;
            if (string.Equals(_configuration.ProviderKind, "ZhipuAI", StringComparison.OrdinalIgnoreCase))
            {
                // 智谱官方 OpenAI 兼容教程要求 temperature 位于 (0, 1)。
                temperature = temperature <= 0 ? 0.7 : temperature;
                return Math.Clamp(temperature, 0.01, 1.0);
            }

            return temperature;
        }

        private bool UsesMaxCompletionTokens()
        {
            return string.Equals(_configuration.ProviderKind, "XiaoMiMiMo", StringComparison.OrdinalIgnoreCase);
        }

        private static object? TryGetStopParameter(IReadOnlyDictionary<string, object> parameters)
        {
            if (!parameters.TryGetValue("stop", out var value) || value == null)
            {
                return null;
            }

            if (value is string stopText)
            {
                return stopText;
            }

            if (value is IEnumerable<string> stopList)
            {
                return stopList.ToArray();
            }

            if (value is IEnumerable<object> stopObjects)
            {
                return stopObjects.Select(static item => item?.ToString())
                    .Where(static item => !string.IsNullOrWhiteSpace(item))
                    .ToArray();
            }

            return value.ToString();
        }

        #region OpenAI API 数据模型

        private class OpenAIChatRequest
        {
            [JsonPropertyName("model")] public string Model { get; set; } = string.Empty;
            [JsonPropertyName("messages")] public List<OpenAIMessage> Messages { get; set; } = new();
            [JsonPropertyName("temperature")] public double Temperature { get; set; } = 0.7;
            [JsonPropertyName("max_tokens")] public int? MaxTokens { get; set; }
            [JsonPropertyName("max_completion_tokens")] public int? MaxCompletionTokens { get; set; }
            [JsonPropertyName("stream")] public bool Stream { get; set; }
            [JsonPropertyName("top_p")] public double? TopP { get; set; }
            [JsonPropertyName("frequency_penalty")] public double? FrequencyPenalty { get; set; }
            [JsonPropertyName("presence_penalty")] public double? PresencePenalty { get; set; }
            [JsonPropertyName("stop")] public object? Stop { get; set; }
            [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
        }

        private class OpenAIMessage
        {
            [JsonPropertyName("role")] public string Role { get; set; } = string.Empty;
            [JsonPropertyName("content")] public string Content { get; set; } = string.Empty;
            [JsonPropertyName("reasoning_content")] public string? ReasoningContent { get; set; }
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
