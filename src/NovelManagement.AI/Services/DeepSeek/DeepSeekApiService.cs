using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http;
using Newtonsoft.Json;
using System.Text;
using System.Net.Http.Headers;
using NovelManagement.AI.Services.DeepSeek.Models;
using NovelManagement.AI.Services.ThinkingChain.Models;
using NovelManagement.AI.Services.ThinkingChain;
using ThinkingChainModel = NovelManagement.AI.Services.ThinkingChain.Models.ThinkingChain;

namespace NovelManagement.AI.Services.DeepSeek
{
    /// <summary>
    /// DeepSeek API服务实现
    /// </summary>
    public class DeepSeekApiService : IDeepSeekApiService, IDisposable
    {
        private readonly ILogger<DeepSeekApiService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IThinkingChainProcessor _thinkingChainProcessor;
        private readonly DeepSeekUsageStats _usageStats;
        private DeepSeekConfiguration _configuration;
        private bool _disposed = false;

        /// <summary>
        /// 配置变更事件
        /// </summary>
        public event EventHandler<DeepSeekConfiguration>? ConfigurationChanged;

        /// <summary>
        /// 思维链更新事件
        /// </summary>
        public event EventHandler<ThinkingChainModel>? ThinkingChainUpdated;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="httpClientFactory">HTTP客户端工厂</param>
        /// <param name="thinkingChainProcessor">思维链处理器</param>
        /// <param name="configuration">配置</param>
        public DeepSeekApiService(
            ILogger<DeepSeekApiService> logger,
            IHttpClientFactory httpClientFactory,
            IThinkingChainProcessor thinkingChainProcessor,
            IConfiguration? configuration = null)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _thinkingChainProcessor = thinkingChainProcessor;
            _usageStats = new DeepSeekUsageStats();

            // 初始化配置
            _configuration = new DeepSeekConfiguration();
            if (configuration != null)
            {
                LoadConfigurationFromSettings(configuration);
            }

            _logger.LogInformation("DeepSeek API服务已初始化");
        }

        /// <summary>
        /// 获取当前配置
        /// </summary>
        /// <returns>配置信息</returns>
        public DeepSeekConfiguration GetConfiguration()
        {
            return _configuration;
        }

        /// <summary>
        /// 更新配置
        /// </summary>
        /// <param name="configuration">新配置</param>
        /// <returns>是否成功</returns>
        public async Task<bool> UpdateConfigurationAsync(DeepSeekConfiguration configuration)
        {
            try
            {
                if (!configuration.IsValid())
                {
                    var errors = configuration.GetValidationErrors();
                    _logger.LogWarning("配置验证失败: {Errors}", string.Join(", ", errors));
                    return false;
                }

                _configuration = configuration;
                ConfigurationChanged?.Invoke(this, configuration);
                
                _logger.LogInformation("DeepSeek配置已更新");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新DeepSeek配置失败");
                return false;
            }
        }

        /// <summary>
        /// 测试API连接
        /// </summary>
        /// <returns>测试结果</returns>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var testRequest = new DeepSeekRequest
                {
                    Model = _configuration.DefaultModel,
                    Messages = new List<DeepSeekMessage>
                    {
                        DeepSeekMessage.CreateUserMessage("Hello, this is a connection test.")
                    },
                    MaxTokens = 10,
                    Temperature = 0.1
                };

                using var httpClient = CreateHttpClient();
                var response = await SendRequestAsync(httpClient, testRequest, CancellationToken.None);
                
                _logger.LogInformation("DeepSeek API连接测试成功");
                return response != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeepSeek API连接测试失败");
                return false;
            }
        }

        /// <summary>
        /// 发送聊天请求
        /// </summary>
        /// <param name="request">请求参数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>响应结果</returns>
        public async Task<DeepSeekResponse> ChatAsync(DeepSeekRequest request, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.Now;
            
            try
            {
                _logger.LogDebug("发送DeepSeek聊天请求: {Model}", request.Model);
                
                using var httpClient = CreateHttpClient();
                var response = await SendRequestAsync(httpClient, request, cancellationToken);
                
                // 更新统计信息
                UpdateUsageStats(true, DateTime.Now - startTime, response?.Usage);
                
                _logger.LogDebug("DeepSeek聊天请求完成，耗时: {Duration}ms", (DateTime.Now - startTime).TotalMilliseconds);
                
                return response ?? new DeepSeekResponse();
            }
            catch (Exception ex)
            {
                UpdateUsageStats(false, DateTime.Now - startTime, null);
                _logger.LogError(ex, "DeepSeek聊天请求失败");
                throw;
            }
        }

        /// <summary>
        /// 发送流式聊天请求
        /// </summary>
        /// <param name="request">请求参数</param>
        /// <param name="onChunkReceived">接收到数据块时的回调</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>完整响应结果</returns>
        public async Task<DeepSeekResponse> ChatStreamAsync(
            DeepSeekRequest request,
            Action<DeepSeekStreamResponse>? onChunkReceived = null,
            CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.Now;
            
            try
            {
                _logger.LogDebug("发送DeepSeek流式聊天请求: {Model}", request.Model);
                
                request.Stream = true;
                
                using var httpClient = CreateHttpClient();
                var response = await SendStreamRequestAsync(httpClient, request, onChunkReceived, cancellationToken);
                
                // 更新统计信息
                UpdateUsageStats(true, DateTime.Now - startTime, response?.Usage);
                
                _logger.LogDebug("DeepSeek流式聊天请求完成，耗时: {Duration}ms", (DateTime.Now - startTime).TotalMilliseconds);
                
                return response ?? new DeepSeekResponse();
            }
            catch (Exception ex)
            {
                UpdateUsageStats(false, DateTime.Now - startTime, null);
                _logger.LogError(ex, "DeepSeek流式聊天请求失败");
                throw;
            }
        }

        /// <summary>
        /// 发送带思维链的请求
        /// </summary>
        /// <param name="prompt">提示词</param>
        /// <param name="systemMessage">系统消息</param>
        /// <param name="onThinkingUpdated">思维链更新回调</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>响应结果和思维链</returns>
        public async Task<(DeepSeekResponse Response, ThinkingChainModel ThinkingChain)> ChatWithThinkingAsync(
            string prompt,
            string? systemMessage = null,
            Action<ThinkingChainModel>? onThinkingUpdated = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("发送带思维链的DeepSeek请求");
                
                var request = new DeepSeekRequest
                {
                    Model = _configuration.DefaultModel,
                    Messages = new List<DeepSeekMessage>(),
                    Temperature = _configuration.DefaultTemperature,
                    MaxTokens = _configuration.DefaultMaxTokens,
                    Stream = _configuration.EnableStreaming
                };

                // 添加系统消息
                if (!string.IsNullOrEmpty(systemMessage))
                {
                    request.Messages.Add(DeepSeekMessage.CreateSystemMessage(systemMessage));
                }

                // 添加用户消息
                request.Messages.Add(DeepSeekMessage.CreateUserMessage(prompt));

                // 创建思维链
                var thinkingChain = new ThinkingChainModel
                {
                    Title = "AI思维过程",
                    Description = $"处理提示: {prompt.Substring(0, Math.Min(50, prompt.Length))}...",
                    OriginalInput = prompt,
                    TaskId = Guid.NewGuid().ToString(),
                    AgentId = "DeepSeek"
                };

                thinkingChain.Start();

                // 发送请求
                DeepSeekResponse response;
                if (_configuration.EnableStreaming)
                {
                    response = await ChatStreamAsync(request, chunk =>
                    {
                        // 处理流式思维链更新
                        if (chunk.Choices.Count > 0 && chunk.Choices[0].Delta?.Reasoning != null)
                        {
                            ProcessThinkingChainUpdate(thinkingChain, chunk.Choices[0].Delta.Reasoning, onThinkingUpdated);
                        }
                    }, cancellationToken);
                }
                else
                {
                    response = await ChatAsync(request, cancellationToken);
                }

                // 处理思维链
                if (response.HasThinking())
                {
                    var thinkingText = response.GetThinkingContent();
                    await _thinkingChainProcessor.ProcessThinkingChainStreamAsync(
                        thinkingChain, 
                        thinkingText, 
                        step => onThinkingUpdated?.Invoke(thinkingChain),
                        cancellationToken);
                }

                thinkingChain.FinalOutput = response.GetContent();
                thinkingChain.Complete();

                ThinkingChainUpdated?.Invoke(this, thinkingChain);
                
                _logger.LogDebug("带思维链的DeepSeek请求完成");
                
                return (response, thinkingChain);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "带思维链的DeepSeek请求失败");
                throw;
            }
        }

        /// <summary>
        /// 批量处理请求
        /// </summary>
        /// <param name="requests">请求列表</param>
        /// <param name="maxConcurrency">最大并发数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>响应结果列表</returns>
        public async Task<List<DeepSeekResponse>> BatchChatAsync(
            List<DeepSeekRequest> requests,
            int maxConcurrency = 3,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("开始批量处理DeepSeek请求，数量: {Count}, 最大并发: {MaxConcurrency}", requests.Count, maxConcurrency);
                
                var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
                var tasks = requests.Select(async request =>
                {
                    await semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        return await ChatAsync(request, cancellationToken);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                var responses = await Task.WhenAll(tasks);
                
                _logger.LogDebug("批量处理DeepSeek请求完成");
                
                return responses.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量处理DeepSeek请求失败");
                throw;
            }
        }

        /// <summary>
        /// 获取模型列表
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>模型列表</returns>
        public async Task<List<string>> GetModelsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // DeepSeek支持的模型列表
                var models = new List<string>
                {
                    "deepseek-chat",
                    "deepseek-coder",
                    "deepseek-reasoner"
                };

                return await Task.FromResult(models);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取DeepSeek模型列表失败");
                return new List<string>();
            }
        }

        /// <summary>
        /// 获取API使用统计
        /// </summary>
        /// <returns>使用统计</returns>
        public async Task<DeepSeekUsageStats> GetUsageStatsAsync()
        {
            return await Task.FromResult(_usageStats);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _logger.LogInformation("DeepSeek API服务已释放");
            }
        }

        #region 私有方法

        /// <summary>
        /// 从配置文件加载配置
        /// </summary>
        /// <param name="configuration">配置对象</param>
        private void LoadConfigurationFromSettings(IConfiguration configuration)
        {
            var section = configuration.GetSection("DeepSeek");
            if (section.Exists())
            {
                _configuration.ApiKey = section["ApiKey"] ?? _configuration.ApiKey;
                _configuration.BaseUrl = section["BaseUrl"] ?? _configuration.BaseUrl;
                _configuration.DefaultModel = section["DefaultModel"] ?? _configuration.DefaultModel;
                
                if (int.TryParse(section["TimeoutSeconds"], out var timeout))
                    _configuration.TimeoutSeconds = timeout;
                
                if (int.TryParse(section["MaxRetries"], out var retries))
                    _configuration.MaxRetries = retries;
                
                if (bool.TryParse(section["EnableThinkingChain"], out var enableThinking))
                    _configuration.EnableThinkingChain = enableThinking;
                
                if (double.TryParse(section["DefaultTemperature"], out var temperature))
                    _configuration.DefaultTemperature = temperature;
                
                if (int.TryParse(section["DefaultMaxTokens"], out var maxTokens))
                    _configuration.DefaultMaxTokens = maxTokens;
            }
        }

        /// <summary>
        /// 创建HTTP客户端
        /// </summary>
        /// <returns>HTTP客户端</returns>
        private HttpClient CreateHttpClient()
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(_configuration.BaseUrl);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _configuration.ApiKey);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.Timeout = TimeSpan.FromSeconds(_configuration.TimeoutSeconds);
            
            return httpClient;
        }

        /// <summary>
        /// 发送请求
        /// </summary>
        /// <param name="httpClient">HTTP客户端</param>
        /// <param name="request">请求对象</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>响应对象</returns>
        private async Task<DeepSeekResponse?> SendRequestAsync(HttpClient httpClient, DeepSeekRequest request, CancellationToken cancellationToken)
        {
            var json = JsonConvert.SerializeObject(request, Formatting.None);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await httpClient.PostAsync("/v1/chat/completions", content, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonConvert.DeserializeObject<DeepSeekResponse>(responseJson);
        }

        /// <summary>
        /// 发送流式请求
        /// </summary>
        /// <param name="httpClient">HTTP客户端</param>
        /// <param name="request">请求对象</param>
        /// <param name="onChunkReceived">数据块接收回调</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>完整响应对象</returns>
        private async Task<DeepSeekResponse?> SendStreamRequestAsync(
            HttpClient httpClient, 
            DeepSeekRequest request, 
            Action<DeepSeekStreamResponse>? onChunkReceived, 
            CancellationToken cancellationToken)
        {
            var json = JsonConvert.SerializeObject(request, Formatting.None);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await httpClient.PostAsync("/v1/chat/completions", content, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var fullResponse = new DeepSeekResponse
            {
                Choices = new List<DeepSeekChoice>
                {
                    new DeepSeekChoice
                    {
                        Message = new DeepSeekResponseMessage
                        {
                            Role = "assistant",
                            Content = "",
                            Reasoning = ""
                        }
                    }
                }
            };
            
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);
            
            string? line;
            while ((line = await reader.ReadLineAsync()) != null && !cancellationToken.IsCancellationRequested)
            {
                if (line.StartsWith("data: "))
                {
                    var data = line.Substring(6);
                    if (data == "[DONE]") break;
                    
                    try
                    {
                        var chunk = JsonConvert.DeserializeObject<DeepSeekStreamResponse>(data);
                        if (chunk != null)
                        {
                            onChunkReceived?.Invoke(chunk);
                            
                            // 合并到完整响应
                            if (chunk.Choices.Count > 0 && chunk.Choices[0].Delta != null)
                            {
                                var delta = chunk.Choices[0].Delta;
                                if (!string.IsNullOrEmpty(delta.Content))
                                {
                                    fullResponse.Choices[0].Message!.Content += delta.Content;
                                }
                                if (!string.IsNullOrEmpty(delta.Reasoning))
                                {
                                    fullResponse.Choices[0].Message!.Reasoning += delta.Reasoning;
                                }
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "解析流式响应数据失败: {Data}", data);
                    }
                }
            }
            
            return fullResponse;
        }

        /// <summary>
        /// 处理思维链更新
        /// </summary>
        /// <param name="thinkingChain">思维链</param>
        /// <param name="reasoning">推理内容</param>
        /// <param name="onThinkingUpdated">更新回调</param>
        private void ProcessThinkingChainUpdate(ThinkingChainModel thinkingChain, string reasoning, Action<ThinkingChainModel>? onThinkingUpdated)
        {
            try
            {
                // 简单的思维链处理逻辑
                var step = new ThinkingStep
                {
                    Title = "AI推理",
                    Content = reasoning,
                    Type = ThinkingStepType.Reasoning,
                    Status = ThinkingStepStatus.Processing
                };
                
                thinkingChain.AddStep(step);
                step.Complete();
                thinkingChain.UpdateProgress();
                
                onThinkingUpdated?.Invoke(thinkingChain);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "处理思维链更新失败");
            }
        }

        /// <summary>
        /// 更新使用统计
        /// </summary>
        /// <param name="success">是否成功</param>
        /// <param name="duration">持续时间</param>
        /// <param name="usage">使用情况</param>
        private void UpdateUsageStats(bool success, TimeSpan duration, DeepSeekUsage? usage)
        {
            _usageStats.TotalRequests++;
            _usageStats.LastRequestTime = DateTime.Now;
            
            if (success)
            {
                _usageStats.SuccessfulRequests++;
                if (usage != null)
                {
                    _usageStats.TotalTokensUsed += usage.TotalTokens;
                }
            }
            else
            {
                _usageStats.FailedRequests++;
            }
            
            // 更新平均响应时间
            _usageStats.AverageResponseTime = (_usageStats.AverageResponseTime * (_usageStats.TotalRequests - 1) + duration.TotalMilliseconds) / _usageStats.TotalRequests;
            
            // 更新今日统计
            if (_usageStats.LastRequestTime?.Date == DateTime.Today)
            {
                _usageStats.TodayRequests++;
                if (success && usage != null)
                {
                    _usageStats.TodayTokensUsed += usage.TotalTokens;
                }
            }
        }

        #endregion
    }
}
