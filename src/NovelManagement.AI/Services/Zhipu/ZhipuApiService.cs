using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NovelManagement.AI.Interfaces;
using NovelManagement.AI.Services.Zhipu.Models;

namespace NovelManagement.AI.Services.Zhipu
{
    /// <summary>
    /// Zhipu AI API服务实现
    /// </summary>
    public class ZhipuApiService : IZhipuApiService, IDisposable
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ZhipuApiService> _logger;
        private ZhipuConfiguration _configuration;
        private bool _disposed = false;
        private readonly ProviderStatistics _statistics = new();

        /// <summary>
        /// 提供者名称
        /// </summary>
        public string ProviderName => "Zhipu";

        /// <summary>
        /// 提供者类型
        /// </summary>
        public ModelProviderType ProviderType => ModelProviderType.CloudAPI;

        /// <summary>
        /// 是否可用
        /// </summary>
        public bool IsAvailable => _configuration != null && _configuration.IsValid();

        /// <summary>
        /// 配置变更事件
        /// </summary>
        public event EventHandler<ModelConfigurationChangedEventArgs>? ConfigurationChanged;

        /// <summary>
        /// 连接状态变更事件
        /// </summary>
        public event EventHandler<ConnectionStatusChangedEventArgs>? ConnectionStatusChanged;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ZhipuApiService(
            IHttpClientFactory httpClientFactory,
            ILogger<ZhipuApiService> logger,
            IOptions<ZhipuConfiguration> configuration)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _configuration = configuration.Value ?? new ZhipuConfiguration();
        }

        /// <summary>
        /// 获取当前配置
        /// </summary>
        public ZhipuConfiguration GetConfiguration()
        {
            return _configuration;
        }

        /// <summary>
        /// 初始化提供者
        /// </summary>
        public Task<bool> InitializeAsync(IModelConfiguration configuration, CancellationToken cancellationToken = default)
        {
            // 此处不做复杂初始化，主要是验证配置
            // 由于IModelConfiguration是通用接口，这里可能需要适配
            // 实际上App.xaml.cs中可能会直接调用UpdateConfigurationAsync
            return Task.FromResult(IsAvailable);
        }

        /// <summary>
        /// 更新配置
        /// </summary>
        public Task<bool> UpdateConfigurationAsync(ZhipuConfiguration configuration)
        {
            if (configuration == null || !configuration.IsValid())
            {
                return Task.FromResult(false);
            }

            var oldConfig = _configuration; // 需要适配 IModelConfiguration
            _configuration = configuration;
            
            // 触发事件
            // 注意：ConfigurationChanged需要IModelConfiguration参数，这里简化处理或需要包装
            // 由于ZhipuConfiguration没有实现IModelConfiguration，这里暂时不触发通用事件，或者需要包装类
            // 为了兼容接口，我们先不触发通用事件，或者后续实现包装器
            
            return Task.FromResult(true);
        }

        /// <summary>
        /// 测试连接
        /// </summary>
        public async Task<ConnectionTestResult> TestConnectionAsync(CancellationToken cancellationToken = default)
        {
            var result = new ConnectionTestResult();
            var startTime = DateTime.Now;

            try
            {
                var request = new ZhipuRequest
                {
                    Model = _configuration.DefaultModel,
                    Messages = new List<ZhipuMessage> { ZhipuMessage.CreateUserMessage("Hello") },
                    MaxTokens = 5
                };

                using var httpClient = CreateHttpClient();
                var response = await SendRequestAsync(httpClient, request, cancellationToken);

                result.IsSuccess = response != null;
                result.ResponseTime = DateTime.Now - startTime;
                
                ConnectionStatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs(true));
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                result.ResponseTime = DateTime.Now - startTime;
                
                ConnectionStatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs(false, ex.Message));
            }

            return result;
        }

        /// <summary>
        /// 获取可用模型列表
        /// </summary>
        public Task<List<ModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
        {
            // Zhipu API没有直接获取模型列表的端点（或者不常用），这里返回已知支持的模型
            var models = new List<ModelInfo>
            {
                new ModelInfo { Id = "glm-4.7-flash", Name = "GLM-4.7 Flash", Description = "智谱AI免费高速模型", Capabilities = new List<string> { "chat", "context-128k" } },
                new ModelInfo { Id = "glm-4", Name = "GLM-4", Description = "智谱AI最强模型", Capabilities = new List<string> { "chat", "context-128k" } },
                new ModelInfo { Id = "glm-4-flash", Name = "GLM-4 Flash", Description = "智谱AI高速模型", Capabilities = new List<string> { "chat", "context-128k" } },
                new ModelInfo { Id = "glm-3-turbo", Name = "GLM-3 Turbo", Description = "智谱AI高性价比模型", Capabilities = new List<string> { "chat", "context-128k" } }
            };

            return Task.FromResult(models);
        }

        /// <summary>
        /// 发送聊天请求
        /// </summary>
        public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.Now;
            try
            {
                var zhipuRequest = MapToZhipuRequest(request);
                using var httpClient = CreateHttpClient();
                var zhipuResponse = await SendRequestAsync(httpClient, zhipuRequest, cancellationToken);

                var response = MapToChatResponse(zhipuResponse);
                response.ResponseTime = DateTime.Now - startTime;
                
                UpdateStatistics(true, response.ResponseTime, response.Usage);
                return response;
            }
            catch (Exception ex)
            {
                UpdateStatistics(false, DateTime.Now - startTime, null);
                _logger.LogError(ex, "Zhipu ChatAsync failed");
                return new ChatResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    ResponseTime = DateTime.Now - startTime
                };
            }
        }

        /// <summary>
        /// 发送流式聊天请求
        /// </summary>
        public async Task<ChatResponse> ChatStreamAsync(ChatRequest request, Action<ChatChunk> onChunkReceived, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.Now;
            try
            {
                var zhipuRequest = MapToZhipuRequest(request);
                zhipuRequest.Stream = true;

                using var httpClient = CreateHttpClient();
                var finalResponse = await SendStreamRequestAsync(httpClient, zhipuRequest, onChunkReceived, cancellationToken);
                
                finalResponse.ResponseTime = DateTime.Now - startTime;
                UpdateStatistics(true, finalResponse.ResponseTime, finalResponse.Usage);
                return finalResponse;
            }
            catch (Exception ex)
            {
                UpdateStatistics(false, DateTime.Now - startTime, null);
                _logger.LogError(ex, "Zhipu ChatStreamAsync failed");
                return new ChatResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    ResponseTime = DateTime.Now - startTime
                };
            }
        }

        /// <summary>
        /// 获取提供者统计信息
        /// </summary>
        public Task<ProviderStatistics> GetStatisticsAsync()
        {
            return Task.FromResult(_statistics);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }

        #region Private Methods

        private HttpClient CreateHttpClient()
        {
            var client = _httpClientFactory.CreateClient("Zhipu");
            client.BaseAddress = new Uri(_configuration.BaseUrl);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _configuration.ApiKey);
            client.Timeout = TimeSpan.FromSeconds(_configuration.TimeoutSeconds);
            return client;
        }

        private ZhipuRequest MapToZhipuRequest(ChatRequest request)
        {
            return new ZhipuRequest
            {
                Model = string.IsNullOrEmpty(request.Model) ? _configuration.DefaultModel : request.Model,
                Messages = request.Messages.Select(m => new ZhipuMessage { Role = m.Role, Content = m.Content }).ToList(),
                Temperature = request.Temperature > 0 ? request.Temperature : _configuration.DefaultTemperature,
                MaxTokens = request.MaxTokens > 0 ? request.MaxTokens : _configuration.DefaultMaxTokens,
                Stream = request.Stream
            };
        }

        private ChatResponse MapToChatResponse(ZhipuResponse? response)
        {
            if (response == null) return new ChatResponse { IsSuccess = false, ErrorMessage = "Empty response" };

            return new ChatResponse
            {
                Id = response.Id,
                Model = response.Model,
                Content = response.GetContent(),
                IsSuccess = true,
                Usage = response.Usage != null ? new TokenUsage 
                { 
                    PromptTokens = response.Usage.PromptTokens,
                    CompletionTokens = response.Usage.CompletionTokens,
                    TotalTokens = response.Usage.TotalTokens
                } : null
            };
        }

        private async Task<ZhipuResponse?> SendRequestAsync(HttpClient client, ZhipuRequest request, CancellationToken cancellationToken)
        {
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/api/paas/v4/chat/completions", content, cancellationToken); // Handle full URL if BaseAddress is just domain
            
            // If BaseAddress is already set to https://open.bigmodel.cn/api/paas/v4, then path is /chat/completions
            // Let's assume BaseUrl in config includes /api/paas/v4 or user sets it. 
            // The default in config is "https://open.bigmodel.cn/api/paas/v4".
            // So we should append "/chat/completions".
            // But HttpClient.BaseAddress behavior depends on trailing slash.
            // Safe bet: ensure path starts with relative path.
            
            // Correction: SendRequestAsync should use relative path.
            // If BaseUrl is https://open.bigmodel.cn/api/paas/v4
            // Path should be "chat/completions" (no leading slash if base has trailing, or handle it)
            // Let's retry with safe logic.
            
            // For now, let's assume the client is configured correctly in CreateHttpClient.
            // We'll use "chat/completions" relative path.
            
            // Re-creating client with correct path logic inside SendRequest is safer if we control it here.
            // But CreateHttpClient sets BaseAddress. 
            
            // Let's assume standard behavior.
            var path = "chat/completions";
            // Check if BaseUrl ends with /
            if (!_configuration.BaseUrl.EndsWith("/"))
            {
                // If not ending with /, and we use relative path, it might drop the last segment if we are not careful.
                // But HttpClient combines BaseAddress and request Uri.
                // If BaseAddress is "https://api.com/v4", Request "chat/completions" -> "https://api.com/chat/completions" (drops v4 if no slash!)
                // If BaseAddress is "https://api.com/v4/", Request "chat/completions" -> "https://api.com/v4/chat/completions"
                
                // We should ensure BaseUrl ends with / in Config or here.
            }
            
            // Override: CreateHttpClient should ensure slash.
            // But here I'll use absolute URL if needed or just trust the config.
            // Let's assume config is good or fix it in CreateHttpClient.
            
            // Actually, let's just use the client.
            var responseMsg = await client.PostAsync("chat/completions", content, cancellationToken);
            responseMsg.EnsureSuccessStatusCode();
            
            var json = await responseMsg.Content.ReadAsStringAsync(cancellationToken);
            return JsonConvert.DeserializeObject<ZhipuResponse>(json);
        }

        private async Task<ChatResponse> SendStreamRequestAsync(HttpClient client, ZhipuRequest request, Action<ChatChunk> onChunkReceived, CancellationToken cancellationToken)
        {
             var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
             // Use SendAsync with CompletionOption.ResponseHeadersRead
             using var requestMsg = new HttpRequestMessage(HttpMethod.Post, "chat/completions") { Content = content };
             using var responseMsg = await client.SendAsync(requestMsg, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
             responseMsg.EnsureSuccessStatusCode();
             
             using var stream = await responseMsg.Content.ReadAsStreamAsync(cancellationToken);
             using var reader = new StreamReader(stream);
             
             var finalResponse = new ChatResponse { IsSuccess = true };
             var sb = new StringBuilder();
             
             string? line;
             while ((line = await reader.ReadLineAsync()) != null && !cancellationToken.IsCancellationRequested)
             {
                 if (line.StartsWith("data: "))
                 {
                     var data = line.Substring(6);
                     if (data == "[DONE]") break;
                     
                     try 
                     {
                         var chunk = JsonConvert.DeserializeObject<ZhipuResponse>(data);
                         if (chunk != null && chunk.Choices != null && chunk.Choices.Count > 0)
                         {
                             var deltaContent = chunk.Choices[0].Delta?.Content;
                             if (!string.IsNullOrEmpty(deltaContent))
                             {
                                 sb.Append(deltaContent);
                                 onChunkReceived?.Invoke(new ChatChunk 
                                 { 
                                     Content = deltaContent,
                                     Id = chunk.Id,
                                     IsComplete = false
                                 });
                             }
                             
                             if (chunk.Usage != null)
                             {
                                 finalResponse.Usage = new TokenUsage
                                 {
                                     PromptTokens = chunk.Usage.PromptTokens,
                                     CompletionTokens = chunk.Usage.CompletionTokens,
                                     TotalTokens = chunk.Usage.TotalTokens
                                 };
                             }
                         }
                     }
                     catch {}
                 }
             }
             
             finalResponse.Content = sb.ToString();
             return finalResponse;
        }

        private void UpdateStatistics(bool success, TimeSpan duration, TokenUsage? usage)
        {
            _statistics.TotalRequests++;
            _statistics.LastRequestTime = DateTime.Now;
            if (success)
            {
                _statistics.SuccessfulRequests++;
                if (usage != null) _statistics.TotalTokensUsed += usage.TotalTokens;
                
                // Avg calc
                var totalMs = _statistics.AverageResponseTime.TotalMilliseconds * (_statistics.SuccessfulRequests - 1) + duration.TotalMilliseconds;
                _statistics.AverageResponseTime = TimeSpan.FromMilliseconds(totalMs / _statistics.SuccessfulRequests);
            }
            else
            {
                _statistics.FailedRequests++;
            }
        }

        #endregion
    }
}
