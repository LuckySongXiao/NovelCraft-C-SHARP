using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NovelManagement.AI.Interfaces;
using NovelManagement.AI.Services.Ollama.Models;

namespace NovelManagement.AI.Services.Ollama
{
    /// <summary>
    /// Ollama API服务实现
    /// </summary>
    public class OllamaApiService : IOllamaApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<OllamaApiService> _logger;
        private OllamaConfiguration _configuration;
        private ProviderStatistics _statistics;
        private SemaphoreSlim _requestSemaphore;
        private bool _disposed = false;

        /// <summary>
        /// 提供者名称
        /// </summary>
        public string ProviderName => "Ollama";

        /// <summary>
        /// 提供者类型
        /// </summary>
        public ModelProviderType ProviderType => ModelProviderType.Local;

        /// <summary>
        /// 是否可用
        /// </summary>
        public bool IsAvailable { get; private set; }

        /// <summary>
        /// 配置变更事件
        /// </summary>
        public event EventHandler<ModelConfigurationChangedEventArgs>? ConfigurationChanged;

        /// <summary>
        /// 连接状态变更事件
        /// </summary>
        public event EventHandler<ConnectionStatusChangedEventArgs>? ConnectionStatusChanged;

        /// <summary>
        /// 模型下载进度事件
        /// </summary>
        public event EventHandler<ModelDownloadProgressEventArgs>? ModelDownloadProgress;

        public OllamaApiService(IHttpClientFactory httpClientFactory, ILogger<OllamaApiService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _configuration = OllamaConfiguration.GetDefault();
            _statistics = new ProviderStatistics();
            _requestSemaphore = new SemaphoreSlim(_configuration.MaxConcurrentRequests, _configuration.MaxConcurrentRequests);
        }

        /// <summary>
        /// 初始化提供者
        /// </summary>
        /// <param name="configuration">配置信息</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>初始化结果</returns>
        public async Task<bool> InitializeAsync(IModelConfiguration configuration, CancellationToken cancellationToken = default)
        {
            try
            {
                if (configuration is not OllamaConfiguration ollamaConfig)
                {
                    _logger.LogError("配置类型不匹配，期望OllamaConfiguration");
                    return false;
                }

                var oldConfig = _configuration;
                _configuration = ollamaConfig;

                // 更新信号量 - 如果并发请求数发生变化，需要重新创建信号量
                if (oldConfig.MaxConcurrentRequests != _configuration.MaxConcurrentRequests)
                {
                    var oldSemaphore = _requestSemaphore;
                    _requestSemaphore = new SemaphoreSlim(_configuration.MaxConcurrentRequests, _configuration.MaxConcurrentRequests);
                    oldSemaphore?.Dispose();
                    _logger.LogDebug("信号量已更新，新的并发请求数: {MaxConcurrentRequests}", _configuration.MaxConcurrentRequests);
                }

                // 测试连接
                var testResult = await TestConnectionAsync(cancellationToken);
                IsAvailable = testResult.IsSuccess;

                if (IsAvailable)
                {
                    _logger.LogInformation("Ollama服务初始化成功，服务器地址: {BaseUrl}", _configuration.BaseUrl);
                    ConfigurationChanged?.Invoke(this, new ModelConfigurationChangedEventArgs(_configuration, oldConfig));
                    ConnectionStatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs(true));
                }
                else
                {
                    _logger.LogWarning("Ollama服务初始化失败: {Error}", testResult.ErrorMessage);
                    ConnectionStatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs(false, testResult.ErrorMessage));
                }

                return IsAvailable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化Ollama服务失败");
                IsAvailable = false;
                ConnectionStatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs(false, ex.Message));
                return false;
            }
        }

        /// <summary>
        /// 测试连接
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>连接测试结果</returns>
        public async Task<ConnectionTestResult> TestConnectionAsync(CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.Now;
            
            try
            {
                using var httpClient = CreateHttpClient();
                var response = await httpClient.GetAsync("/api/version", cancellationToken);
                
                var responseTime = DateTime.Now - startTime;
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    var versionInfo = JsonSerializer.Deserialize<OllamaVersionResponse>(content);
                    
                    return new ConnectionTestResult
                    {
                        IsSuccess = true,
                        ResponseTime = responseTime,
                        ServerInfo = new Dictionary<string, object>
                        {
                            ["Version"] = versionInfo?.Version ?? "Unknown",
                            ["BaseUrl"] = _configuration.BaseUrl
                        }
                    };
                }
                else
                {
                    return new ConnectionTestResult
                    {
                        IsSuccess = false,
                        ResponseTime = responseTime,
                        ErrorMessage = $"HTTP {response.StatusCode}: {response.ReasonPhrase}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ConnectionTestResult
                {
                    IsSuccess = false,
                    ResponseTime = DateTime.Now - startTime,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// 获取可用模型列表
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>模型列表</returns>
        public async Task<List<ModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var httpClient = CreateHttpClient();
                var response = await httpClient.GetAsync("/api/tags", cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var modelsResponse = JsonSerializer.Deserialize<OllamaModelsResponse>(content);

                var models = new List<ModelInfo>();
                if (modelsResponse?.Models != null)
                {
                    foreach (var model in modelsResponse.Models)
                    {
                        models.Add(new ModelInfo
                        {
                            Id = model.Name,
                            Name = model.Name,
                            Description = $"Ollama模型 - {model.Details?.Family ?? "Unknown"}",
                            Size = model.Size,
                            IsDownloaded = true,
                            Capabilities = GetModelCapabilities(model),
                            Parameters = new Dictionary<string, object>
                            {
                                ["Format"] = model.Details?.Format ?? "Unknown",
                                ["Family"] = model.Details?.Family ?? "Unknown",
                                ["ParameterSize"] = model.Details?.ParameterSize ?? "Unknown",
                                ["QuantizationLevel"] = model.Details?.QuantizationLevel ?? "Unknown",
                                ["ModifiedAt"] = model.ModifiedAt,
                                ["Digest"] = model.Digest
                            }
                        });
                    }
                }

                _logger.LogDebug("获取到{Count}个Ollama模型", models.Count);
                return models;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取Ollama模型列表失败");
                return new List<ModelInfo>();
            }
        }

        /// <summary>
        /// 发送聊天请求
        /// </summary>
        /// <param name="request">聊天请求</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>聊天响应</returns>
        public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.Now;
            
            try
            {
                await _requestSemaphore.WaitAsync(cancellationToken);
                
                try
                {
                    _statistics.TotalRequests++;
                    
                    var ollamaRequest = ConvertToOllamaRequest(request);
                    
                    using var httpClient = CreateHttpClient();
                    var json = JsonSerializer.Serialize(ollamaRequest);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    
                    var response = await httpClient.PostAsync("/api/chat", content, cancellationToken);
                    response.EnsureSuccessStatusCode();
                    
                    var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    var ollamaResponse = JsonSerializer.Deserialize<OllamaChatResponse>(responseContent);
                    
                    var chatResponse = ConvertToChatResponse(ollamaResponse, startTime);
                    
                    _statistics.SuccessfulRequests++;
                    _statistics.LastRequestTime = DateTime.Now;
                    UpdateAverageResponseTime(chatResponse.ResponseTime);
                    
                    if (ollamaResponse?.PromptEvalCount.HasValue == true && ollamaResponse?.EvalCount.HasValue == true)
                    {
                        _statistics.TotalTokensUsed += ollamaResponse.PromptEvalCount.Value + ollamaResponse.EvalCount.Value;
                    }
                    
                    return chatResponse;
                }
                finally
                {
                    _requestSemaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _statistics.FailedRequests++;
                _logger.LogError(ex, "Ollama聊天请求失败");
                
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
        /// <param name="request">聊天请求</param>
        /// <param name="onChunkReceived">数据块接收回调</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>聊天响应</returns>
        public async Task<ChatResponse> ChatStreamAsync(ChatRequest request, Action<ChatChunk> onChunkReceived, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.Now;
            var fullContent = new StringBuilder();
            
            try
            {
                await _requestSemaphore.WaitAsync(cancellationToken);
                
                try
                {
                    _statistics.TotalRequests++;
                    
                    var ollamaRequest = ConvertToOllamaRequest(request);
                    ollamaRequest.Stream = true;
                    
                    using var httpClient = CreateHttpClient();
                    var json = JsonSerializer.Serialize(ollamaRequest);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    
                    var response = await httpClient.PostAsync("/api/chat", content, cancellationToken);
                    response.EnsureSuccessStatusCode();
                    
                    using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    using var reader = new StreamReader(stream);
                    
                    OllamaChatResponse? lastResponse = null;
                    
                    while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
                    {
                        var line = await reader.ReadLineAsync();
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        
                        try
                        {
                            var chunkResponse = JsonSerializer.Deserialize<OllamaChatResponse>(line);
                            if (chunkResponse != null)
                            {
                                lastResponse = chunkResponse;
                                
                                var chunk = new ChatChunk
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    Content = chunkResponse.Message?.Content ?? "",
                                    IsComplete = chunkResponse.Done,
                                    FinishReason = chunkResponse.DoneReason
                                };
                                
                                fullContent.Append(chunk.Content);
                                onChunkReceived(chunk);
                                
                                if (chunkResponse.Done) break;
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogWarning("解析流式响应块失败: {Error}", ex.Message);
                        }
                    }
                    
                    var finalResponse = ConvertToChatResponse(lastResponse, startTime);
                    finalResponse.Content = fullContent.ToString();
                    
                    _statistics.SuccessfulRequests++;
                    _statistics.LastRequestTime = DateTime.Now;
                    UpdateAverageResponseTime(finalResponse.ResponseTime);
                    
                    if (lastResponse?.PromptEvalCount.HasValue == true && lastResponse?.EvalCount.HasValue == true)
                    {
                        _statistics.TotalTokensUsed += lastResponse.PromptEvalCount.Value + lastResponse.EvalCount.Value;
                    }
                    
                    return finalResponse;
                }
                finally
                {
                    _requestSemaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _statistics.FailedRequests++;
                _logger.LogError(ex, "Ollama流式聊天请求失败");
                
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
        /// <returns>统计信息</returns>
        public async Task<ProviderStatistics> GetStatisticsAsync()
        {
            return await Task.FromResult(_statistics);
        }

        /// <summary>
        /// 获取当前配置
        /// </summary>
        /// <returns>配置信息</returns>
        public OllamaConfiguration GetConfiguration()
        {
            return _configuration.Clone();
        }

        /// <summary>
        /// 更新配置
        /// </summary>
        /// <param name="configuration">新配置</param>
        /// <returns>是否成功</returns>
        public async Task<bool> UpdateConfigurationAsync(OllamaConfiguration configuration)
        {
            try
            {
                if (!configuration.IsValid())
                {
                    var errors = configuration.GetValidationErrors();
                    _logger.LogWarning("Ollama配置验证失败: {Errors}", string.Join(", ", errors));
                    return false;
                }

                var oldConfig = _configuration;
                _configuration = configuration;

                // 重新初始化
                var initResult = await InitializeAsync(configuration);
                
                if (initResult)
                {
                    _logger.LogInformation("Ollama配置已更新");
                    ConfigurationChanged?.Invoke(this, new ModelConfigurationChangedEventArgs(_configuration, oldConfig));
                }
                else
                {
                    // 回滚配置
                    _configuration = oldConfig;
                    _logger.LogWarning("Ollama配置更新失败，已回滚到原配置");
                }

                return initResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新Ollama配置失败");
                return false;
            }
        }

        /// <summary>
        /// 获取Ollama版本信息
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>版本信息</returns>
        public async Task<string> GetVersionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var httpClient = CreateHttpClient();
                var response = await httpClient.GetAsync("/api/version", cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var versionInfo = JsonSerializer.Deserialize<OllamaVersionResponse>(content);

                return versionInfo?.Version ?? "Unknown";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取Ollama版本失败");
                return "Unknown";
            }
        }

        /// <summary>
        /// 检查模型是否存在
        /// </summary>
        /// <param name="modelName">模型名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否存在</returns>
        public async Task<bool> ModelExistsAsync(string modelName, CancellationToken cancellationToken = default)
        {
            try
            {
                var models = await GetAvailableModelsAsync(cancellationToken);
                return models.Any(m => m.Id.Equals(modelName, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查模型存在性失败: {ModelName}", modelName);
                return false;
            }
        }

        /// <summary>
        /// 拉取模型
        /// </summary>
        /// <param name="modelName">模型名称</param>
        /// <param name="onProgress">进度回调</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否成功</returns>
        public async Task<bool> PullModelAsync(string modelName, Action<ModelDownloadProgress>? onProgress = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new OllamaPullRequest
                {
                    Name = modelName,
                    Stream = true
                };

                using var httpClient = CreateHttpClient();
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("/api/pull", content, cancellationToken);
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var reader = new StreamReader(stream);

                var progress = new ModelDownloadProgress { Status = "开始下载" };
                onProgress?.Invoke(progress);

                while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    try
                    {
                        var pullResponse = JsonSerializer.Deserialize<OllamaPullResponse>(line);
                        if (pullResponse != null)
                        {
                            progress.Status = pullResponse.Status;
                            progress.TotalBytes = pullResponse.Total ?? 0;
                            progress.DownloadedBytes = pullResponse.Completed ?? 0;

                            if (pullResponse.Status.Contains("success") || pullResponse.Status.Contains("完成"))
                            {
                                progress.IsCompleted = true;
                            }

                            onProgress?.Invoke(progress);
                            ModelDownloadProgress?.Invoke(this, new ModelDownloadProgressEventArgs(modelName, progress));

                            if (progress.IsCompleted) break;
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning("解析下载进度失败: {Error}", ex.Message);
                    }
                }

                _logger.LogInformation("模型下载完成: {ModelName}", modelName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "拉取模型失败: {ModelName}", modelName);
                onProgress?.Invoke(new ModelDownloadProgress
                {
                    Status = "下载失败",
                    ErrorMessage = ex.Message,
                    IsCompleted = true
                });
                return false;
            }
        }

        /// <summary>
        /// 删除模型
        /// </summary>
        /// <param name="modelName">模型名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否成功</returns>
        public async Task<bool> DeleteModelAsync(string modelName, CancellationToken cancellationToken = default)
        {
            try
            {
                using var httpClient = CreateHttpClient();
                var request = new { name = modelName };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/delete")
                {
                    Content = content
                }, cancellationToken);

                response.EnsureSuccessStatusCode();
                _logger.LogInformation("模型删除成功: {ModelName}", modelName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除模型失败: {ModelName}", modelName);
                return false;
            }
        }

        /// <summary>
        /// 生成文本（非聊天模式）
        /// </summary>
        /// <param name="request">生成请求</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>生成响应</returns>
        public async Task<OllamaGenerateResponse> GenerateAsync(OllamaGenerateRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                using var httpClient = CreateHttpClient();
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("/api/generate", content, cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var generateResponse = JsonSerializer.Deserialize<OllamaGenerateResponse>(responseContent);

                return generateResponse ?? new OllamaGenerateResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成文本失败");
                return new OllamaGenerateResponse { Response = $"生成失败: {ex.Message}" };
            }
        }

        /// <summary>
        /// 流式生成文本
        /// </summary>
        /// <param name="request">生成请求</param>
        /// <param name="onChunkReceived">数据块接收回调</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>最终响应</returns>
        public async Task<OllamaGenerateResponse> GenerateStreamAsync(OllamaGenerateRequest request, Action<OllamaGenerateResponse> onChunkReceived, CancellationToken cancellationToken = default)
        {
            try
            {
                request.Stream = true;

                using var httpClient = CreateHttpClient();
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("/api/generate", content, cancellationToken);
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var reader = new StreamReader(stream);

                var fullResponse = new StringBuilder();
                OllamaGenerateResponse? lastResponse = null;

                while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    try
                    {
                        var chunkResponse = JsonSerializer.Deserialize<OllamaGenerateResponse>(line);
                        if (chunkResponse != null)
                        {
                            lastResponse = chunkResponse;
                            fullResponse.Append(chunkResponse.Response);
                            onChunkReceived(chunkResponse);

                            if (chunkResponse.Done) break;
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning("解析生成响应块失败: {Error}", ex.Message);
                    }
                }

                if (lastResponse != null)
                {
                    lastResponse.Response = fullResponse.ToString();
                    return lastResponse;
                }

                return new OllamaGenerateResponse { Response = fullResponse.ToString(), Done = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "流式生成文本失败");
                return new OllamaGenerateResponse { Response = $"生成失败: {ex.Message}", Done = true };
            }
        }

        /// <summary>
        /// 预加载模型
        /// </summary>
        /// <param name="modelName">模型名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否成功</returns>
        public async Task<bool> PreloadModelAsync(string modelName, CancellationToken cancellationToken = default)
        {
            try
            {
                // 发送一个简单的生成请求来预加载模型
                var request = new OllamaGenerateRequest
                {
                    Model = modelName,
                    Prompt = "Hello",
                    Options = new OllamaOptions { NumPredict = 1 }
                };

                await GenerateAsync(request, cancellationToken);
                _logger.LogInformation("模型预加载成功: {ModelName}", modelName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "预加载模型失败: {ModelName}", modelName);
                return false;
            }
        }

        /// <summary>
        /// 卸载模型
        /// </summary>
        /// <param name="modelName">模型名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否成功</returns>
        public async Task<bool> UnloadModelAsync(string modelName, CancellationToken cancellationToken = default)
        {
            try
            {
                // Ollama会自动管理模型的加载和卸载
                // 这里可以设置keep_alive为0来立即卸载
                var request = new OllamaGenerateRequest
                {
                    Model = modelName,
                    Prompt = "",
                    KeepAlive = "0"
                };

                using var httpClient = CreateHttpClient();
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("/api/generate", content, cancellationToken);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("模型卸载成功: {ModelName}", modelName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "卸载模型失败: {ModelName}", modelName);
                return false;
            }
        }

        /// <summary>
        /// 获取模型详细信息
        /// </summary>
        /// <param name="modelName">模型名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>模型信息</returns>
        public async Task<OllamaModelInfo?> GetModelInfoAsync(string modelName, CancellationToken cancellationToken = default)
        {
            try
            {
                var models = await GetAvailableModelsAsync(cancellationToken);
                var modelInfo = models.FirstOrDefault(m => m.Id.Equals(modelName, StringComparison.OrdinalIgnoreCase));

                if (modelInfo != null)
                {
                    // 从ModelInfo转换为OllamaModelInfo
                    return new OllamaModelInfo
                    {
                        Name = modelInfo.Id,
                        Size = modelInfo.Size,
                        ModifiedAt = modelInfo.Parameters.TryGetValue("ModifiedAt", out var modifiedAt) && modifiedAt is DateTime dt ? dt : DateTime.MinValue,
                        Digest = modelInfo.Parameters.TryGetValue("Digest", out var digest) ? digest.ToString() ?? "" : "",
                        Details = new OllamaModelDetails
                        {
                            Format = modelInfo.Parameters.TryGetValue("Format", out var format) ? format.ToString() ?? "" : "",
                            Family = modelInfo.Parameters.TryGetValue("Family", out var family) ? family.ToString() ?? "" : "",
                            ParameterSize = modelInfo.Parameters.TryGetValue("ParameterSize", out var paramSize) ? paramSize.ToString() ?? "" : "",
                            QuantizationLevel = modelInfo.Parameters.TryGetValue("QuantizationLevel", out var quantLevel) ? quantLevel.ToString() ?? "" : ""
                        }
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取模型信息失败: {ModelName}", modelName);
                return null;
            }
        }

        /// <summary>
        /// 获取服务器状态
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>服务器状态</returns>
        public async Task<OllamaServerStatus> GetServerStatusAsync(CancellationToken cancellationToken = default)
        {
            var status = new OllamaServerStatus();
            var startTime = DateTime.Now;

            try
            {
                // 测试连接
                var testResult = await TestConnectionAsync(cancellationToken);
                status.IsOnline = testResult.IsSuccess;
                status.ResponseTime = testResult.ResponseTime;
                status.LastCheckTime = DateTime.Now;

                if (status.IsOnline)
                {
                    // 获取版本信息
                    status.Version = await GetVersionAsync(cancellationToken);

                    // 获取已加载的模型（通过尝试获取模型列表）
                    var models = await GetAvailableModelsAsync(cancellationToken);
                    status.LoadedModels = models.Select(m => m.Id).ToList();

                    // 模拟内存和GPU信息（Ollama API可能不直接提供这些信息）
                    status.AvailableMemory = 0; // 需要通过其他方式获取
                    status.UsedMemory = 0;
                    status.GpuInfo = new List<GpuInfo>(); // 需要通过其他方式获取
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取服务器状态失败");
                status.IsOnline = false;
                status.ResponseTime = DateTime.Now - startTime;
            }

            return status;
        }

        /// <summary>
        /// 批量处理聊天请求
        /// </summary>
        /// <param name="requests">请求列表</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>响应列表</returns>
        public async Task<List<ChatResponse>> BatchChatAsync(List<ChatRequest> requests, CancellationToken cancellationToken = default)
        {
            var responses = new List<ChatResponse>();
            var tasks = new List<Task<ChatResponse>>();

            // 限制并发数量
            var semaphore = new SemaphoreSlim(_configuration.MaxConcurrentRequests);

            foreach (var request in requests)
            {
                tasks.Add(ProcessBatchRequestAsync(request, semaphore, cancellationToken));
            }

            try
            {
                var results = await Task.WhenAll(tasks);
                responses.AddRange(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量处理聊天请求失败");
            }
            finally
            {
                semaphore.Dispose();
            }

            return responses;
        }

        #region 私有辅助方法

        /// <summary>
        /// 创建HTTP客户端
        /// </summary>
        /// <returns>HTTP客户端</returns>
        private HttpClient CreateHttpClient()
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(_configuration.BaseUrl);
            httpClient.Timeout = TimeSpan.FromSeconds(_configuration.TimeoutSeconds);

            // 添加默认头部
            httpClient.DefaultRequestHeaders.Add("User-Agent", "NovelManagement-Ollama-Client/1.0");

            return httpClient;
        }

        /// <summary>
        /// 转换为Ollama请求
        /// </summary>
        /// <param name="request">通用聊天请求</param>
        /// <returns>Ollama聊天请求</returns>
        private OllamaChatRequest ConvertToOllamaRequest(ChatRequest request)
        {
            var ollamaRequest = new OllamaChatRequest
            {
                Model = string.IsNullOrEmpty(request.Model) ? _configuration.DefaultModel : request.Model,
                Stream = request.Stream,
                KeepAlive = _configuration.ModelKeepAliveMinutes > 0 ? $"{_configuration.ModelKeepAliveMinutes}m" : null
            };

            // 转换消息
            foreach (var message in request.Messages)
            {
                ollamaRequest.Messages.Add(new OllamaChatMessage
                {
                    Role = message.Role,
                    Content = message.Content
                });
            }

            // 添加系统提示作为第一条消息
            if (!string.IsNullOrEmpty(request.SystemPrompt))
            {
                ollamaRequest.Messages.Insert(0, new OllamaChatMessage
                {
                    Role = "system",
                    Content = request.SystemPrompt
                });
            }

            // 设置选项
            ollamaRequest.Options = new OllamaOptions
            {
                Temperature = request.Temperature,
                NumPredict = request.MaxTokens > 0 ? request.MaxTokens : null
            };

            // 添加自定义参数
            foreach (var param in request.Parameters)
            {
                switch (param.Key.ToLower())
                {
                    case "top_p":
                        if (double.TryParse(param.Value.ToString(), out var topP))
                            ollamaRequest.Options.TopP = topP;
                        break;
                    case "top_k":
                        if (int.TryParse(param.Value.ToString(), out var topK))
                            ollamaRequest.Options.TopK = topK;
                        break;
                    case "repeat_penalty":
                        if (double.TryParse(param.Value.ToString(), out var repeatPenalty))
                            ollamaRequest.Options.RepeatPenalty = repeatPenalty;
                        break;
                    case "seed":
                        if (int.TryParse(param.Value.ToString(), out var seed))
                            ollamaRequest.Options.Seed = seed;
                        break;
                }
            }

            return ollamaRequest;
        }

        /// <summary>
        /// 转换为通用聊天响应
        /// </summary>
        /// <param name="ollamaResponse">Ollama响应</param>
        /// <param name="startTime">开始时间</param>
        /// <returns>通用聊天响应</returns>
        private ChatResponse ConvertToChatResponse(OllamaChatResponse? ollamaResponse, DateTime startTime)
        {
            if (ollamaResponse == null)
            {
                return new ChatResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "响应为空",
                    ResponseTime = DateTime.Now - startTime
                };
            }

            var response = new ChatResponse
            {
                Id = Guid.NewGuid().ToString(),
                Model = ollamaResponse.Model,
                Content = ollamaResponse.Message?.Content ?? "",
                FinishReason = ollamaResponse.DoneReason ?? (ollamaResponse.Done ? "stop" : "length"),
                ResponseTime = DateTime.Now - startTime,
                IsSuccess = true
            };

            // 设置使用统计
            if (ollamaResponse.PromptEvalCount.HasValue || ollamaResponse.EvalCount.HasValue)
            {
                response.Usage = new TokenUsage
                {
                    PromptTokens = ollamaResponse.PromptEvalCount ?? 0,
                    CompletionTokens = ollamaResponse.EvalCount ?? 0,
                    TotalTokens = (ollamaResponse.PromptEvalCount ?? 0) + (ollamaResponse.EvalCount ?? 0)
                };
            }

            return response;
        }

        /// <summary>
        /// 获取模型能力列表
        /// </summary>
        /// <param name="model">模型信息</param>
        /// <returns>能力列表</returns>
        private List<string> GetModelCapabilities(OllamaModelInfo model)
        {
            var capabilities = new List<string> { "text-generation", "chat" };

            // 根据模型系列添加特定能力
            var family = model.Details?.Family?.ToLower() ?? "";

            if (family.Contains("llama"))
            {
                capabilities.AddRange(new[] { "instruction-following", "reasoning" });
            }
            else if (family.Contains("codellama") || family.Contains("code"))
            {
                capabilities.AddRange(new[] { "code-generation", "code-completion" });
            }
            else if (family.Contains("mistral"))
            {
                capabilities.AddRange(new[] { "multilingual", "reasoning" });
            }

            // 根据模型大小添加能力
            var paramSize = model.Details?.ParameterSize?.ToLower() ?? "";
            if (paramSize.Contains("7b") || paramSize.Contains("13b") || paramSize.Contains("70b"))
            {
                capabilities.Add("high-quality");
            }

            return capabilities;
        }

        /// <summary>
        /// 更新平均响应时间
        /// </summary>
        /// <param name="responseTime">响应时间</param>
        private void UpdateAverageResponseTime(TimeSpan responseTime)
        {
            if (_statistics.TotalRequests == 1)
            {
                _statistics.AverageResponseTime = responseTime;
            }
            else
            {
                var totalMs = _statistics.AverageResponseTime.TotalMilliseconds * (_statistics.TotalRequests - 1) + responseTime.TotalMilliseconds;
                _statistics.AverageResponseTime = TimeSpan.FromMilliseconds(totalMs / _statistics.TotalRequests);
            }
        }

        /// <summary>
        /// 处理批量请求中的单个请求
        /// </summary>
        /// <param name="request">请求</param>
        /// <param name="semaphore">信号量</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>响应</returns>
        private async Task<ChatResponse> ProcessBatchRequestAsync(ChatRequest request, SemaphoreSlim semaphore, CancellationToken cancellationToken)
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
        }

        #endregion

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _requestSemaphore?.Dispose();
                _disposed = true;
            }
        }
    }
}
