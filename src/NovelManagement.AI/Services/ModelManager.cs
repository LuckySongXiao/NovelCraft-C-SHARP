using Microsoft.Extensions.Logging;
using NovelManagement.AI.Interfaces;
using NovelManagement.AI.Services.DeepSeek;
using NovelManagement.AI.Services.Ollama;
using NovelManagement.AI.Services.MCP;

namespace NovelManagement.AI.Services
{
    /// <summary>
    /// AI模型管理器
    /// </summary>
    public class ModelManager : IDisposable
    {
        private readonly ILogger<ModelManager> _logger;
        private readonly Dictionary<string, IModelProvider> _providers;
        private readonly Dictionary<string, ProviderStatistics> _statistics;
        private string _defaultProvider = "DeepSeek";
        private bool _disposed = false;

        /// <summary>
        /// 提供者变更事件
        /// </summary>
        public event EventHandler<ProviderChangedEventArgs>? ProviderChanged;

        /// <summary>
        /// 模型响应事件
        /// </summary>
        public event EventHandler<ModelResponseEventArgs>? ModelResponse;

        /// <summary>
        /// 错误事件
        /// </summary>
        public event EventHandler<ModelErrorEventArgs>? ErrorOccurred;

        public ModelManager(ILogger<ModelManager> logger)
        {
            _logger = logger;
            _providers = new Dictionary<string, IModelProvider>();
            _statistics = new Dictionary<string, ProviderStatistics>();
        }

        /// <summary>
        /// 注册模型提供者
        /// </summary>
        /// <param name="provider">提供者实例</param>
        /// <returns>注册结果</returns>
        public bool RegisterProvider(IModelProvider provider)
        {
            try
            {
                if (_providers.ContainsKey(provider.ProviderName))
                {
                    _logger.LogWarning("提供者已存在: {ProviderName}", provider.ProviderName);
                    return false;
                }

                _providers[provider.ProviderName] = provider;
                _statistics[provider.ProviderName] = new ProviderStatistics();

                // 订阅事件
                provider.ConfigurationChanged += OnProviderConfigurationChanged;
                provider.ConnectionStatusChanged += OnProviderConnectionStatusChanged;

                _logger.LogInformation("注册模型提供者成功: {ProviderName}", provider.ProviderName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "注册模型提供者失败: {ProviderName}", provider.ProviderName);
                return false;
            }
        }

        /// <summary>
        /// 注销模型提供者
        /// </summary>
        /// <param name="providerName">提供者名称</param>
        /// <returns>注销结果</returns>
        public bool UnregisterProvider(string providerName)
        {
            try
            {
                if (!_providers.TryGetValue(providerName, out var provider))
                {
                    _logger.LogWarning("提供者不存在: {ProviderName}", providerName);
                    return false;
                }

                // 取消订阅事件
                provider.ConfigurationChanged -= OnProviderConfigurationChanged;
                provider.ConnectionStatusChanged -= OnProviderConnectionStatusChanged;

                // 释放资源
                provider.Dispose();

                _providers.Remove(providerName);
                _statistics.Remove(providerName);

                _logger.LogInformation("注销模型提供者成功: {ProviderName}", providerName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "注销模型提供者失败: {ProviderName}", providerName);
                return false;
            }
        }

        /// <summary>
        /// 获取提供者
        /// </summary>
        /// <param name="providerName">提供者名称</param>
        /// <returns>提供者实例</returns>
        public IModelProvider? GetProvider(string providerName)
        {
            return _providers.TryGetValue(providerName, out var provider) ? provider : null;
        }

        /// <summary>
        /// 获取所有提供者
        /// </summary>
        /// <returns>提供者列表</returns>
        public List<IModelProvider> GetAllProviders()
        {
            return _providers.Values.ToList();
        }

        /// <summary>
        /// 获取可用提供者
        /// </summary>
        /// <returns>可用提供者列表</returns>
        public List<IModelProvider> GetAvailableProviders()
        {
            return _providers.Values.Where(p => p.IsAvailable).ToList();
        }

        /// <summary>
        /// 设置默认提供者
        /// </summary>
        /// <param name="providerName">提供者名称</param>
        /// <returns>设置结果</returns>
        public bool SetDefaultProvider(string providerName)
        {
            if (!_providers.ContainsKey(providerName))
            {
                _logger.LogWarning("设置默认提供者失败，提供者不存在: {ProviderName}", providerName);
                return false;
            }

            var oldProvider = _defaultProvider;
            _defaultProvider = providerName;

            _logger.LogInformation("默认提供者已更改: {OldProvider} -> {NewProvider}", oldProvider, providerName);
            ProviderChanged?.Invoke(this, new ProviderChangedEventArgs(oldProvider, providerName));

            return true;
        }

        /// <summary>
        /// 获取默认提供者
        /// </summary>
        /// <returns>默认提供者</returns>
        public IModelProvider? GetDefaultProvider()
        {
            return GetProvider(_defaultProvider);
        }

        /// <summary>
        /// 发送聊天请求（使用默认提供者）
        /// </summary>
        /// <param name="request">聊天请求</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>聊天响应</returns>
        public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
        {
            return await ChatAsync(_defaultProvider, request, cancellationToken);
        }

        /// <summary>
        /// 发送聊天请求（指定提供者）
        /// </summary>
        /// <param name="providerName">提供者名称</param>
        /// <param name="request">聊天请求</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>聊天响应</returns>
        public async Task<ChatResponse> ChatAsync(string providerName, ChatRequest request, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.Now;

            try
            {
                var provider = GetProvider(providerName);
                if (provider == null)
                {
                    var errorResponse = new ChatResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = $"提供者不存在: {providerName}",
                        ResponseTime = DateTime.Now - startTime
                    };

                    ErrorOccurred?.Invoke(this, new ModelErrorEventArgs(providerName, errorResponse.ErrorMessage));
                    return errorResponse;
                }

                if (!provider.IsAvailable)
                {
                    var errorResponse = new ChatResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = $"提供者不可用: {providerName}",
                        ResponseTime = DateTime.Now - startTime
                    };

                    ErrorOccurred?.Invoke(this, new ModelErrorEventArgs(providerName, errorResponse.ErrorMessage));
                    return errorResponse;
                }

                var response = await provider.ChatAsync(request, cancellationToken);
                
                // 更新统计信息
                UpdateStatistics(providerName, response);

                // 触发响应事件
                ModelResponse?.Invoke(this, new ModelResponseEventArgs(providerName, request, response));

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "聊天请求失败: {ProviderName}", providerName);
                
                var errorResponse = new ChatResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    ResponseTime = DateTime.Now - startTime
                };

                ErrorOccurred?.Invoke(this, new ModelErrorEventArgs(providerName, ex.Message, ex));
                return errorResponse;
            }
        }

        /// <summary>
        /// 发送流式聊天请求
        /// </summary>
        /// <param name="providerName">提供者名称</param>
        /// <param name="request">聊天请求</param>
        /// <param name="onChunkReceived">数据块接收回调</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>聊天响应</returns>
        public async Task<ChatResponse> ChatStreamAsync(string providerName, ChatRequest request, Action<ChatChunk> onChunkReceived, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.Now;

            try
            {
                var provider = GetProvider(providerName);
                if (provider == null)
                {
                    var errorResponse = new ChatResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = $"提供者不存在: {providerName}",
                        ResponseTime = DateTime.Now - startTime
                    };

                    ErrorOccurred?.Invoke(this, new ModelErrorEventArgs(providerName, errorResponse.ErrorMessage));
                    return errorResponse;
                }

                if (!provider.IsAvailable)
                {
                    var errorResponse = new ChatResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = $"提供者不可用: {providerName}",
                        ResponseTime = DateTime.Now - startTime
                    };

                    ErrorOccurred?.Invoke(this, new ModelErrorEventArgs(providerName, errorResponse.ErrorMessage));
                    return errorResponse;
                }

                var response = await provider.ChatStreamAsync(request, onChunkReceived, cancellationToken);
                
                // 更新统计信息
                UpdateStatistics(providerName, response);

                // 触发响应事件
                ModelResponse?.Invoke(this, new ModelResponseEventArgs(providerName, request, response));

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "流式聊天请求失败: {ProviderName}", providerName);
                
                var errorResponse = new ChatResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    ResponseTime = DateTime.Now - startTime
                };

                ErrorOccurred?.Invoke(this, new ModelErrorEventArgs(providerName, ex.Message, ex));
                return errorResponse;
            }
        }

        /// <summary>
        /// 获取所有可用模型
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>模型列表</returns>
        public async Task<Dictionary<string, List<ModelInfo>>> GetAllAvailableModelsAsync(CancellationToken cancellationToken = default)
        {
            var allModels = new Dictionary<string, List<ModelInfo>>();

            foreach (var provider in _providers.Values)
            {
                try
                {
                    if (provider.IsAvailable)
                    {
                        var models = await provider.GetAvailableModelsAsync(cancellationToken);
                        allModels[provider.ProviderName] = models;
                    }
                    else
                    {
                        allModels[provider.ProviderName] = new List<ModelInfo>();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "获取模型列表失败: {ProviderName}", provider.ProviderName);
                    allModels[provider.ProviderName] = new List<ModelInfo>();
                }
            }

            return allModels;
        }

        /// <summary>
        /// 获取提供者统计信息
        /// </summary>
        /// <param name="providerName">提供者名称</param>
        /// <returns>统计信息</returns>
        public ProviderStatistics? GetProviderStatistics(string providerName)
        {
            return _statistics.TryGetValue(providerName, out var stats) ? stats : null;
        }

        /// <summary>
        /// 获取所有提供者统计信息
        /// </summary>
        /// <returns>统计信息字典</returns>
        public Dictionary<string, ProviderStatistics> GetAllStatistics()
        {
            return new Dictionary<string, ProviderStatistics>(_statistics);
        }

        #region 私有方法

        /// <summary>
        /// 更新统计信息
        /// </summary>
        /// <param name="providerName">提供者名称</param>
        /// <param name="response">响应</param>
        private void UpdateStatistics(string providerName, ChatResponse response)
        {
            if (!_statistics.TryGetValue(providerName, out var stats))
            {
                stats = new ProviderStatistics();
                _statistics[providerName] = stats;
            }

            stats.TotalRequests++;
            stats.LastRequestTime = DateTime.Now;

            if (response.IsSuccess)
            {
                stats.SuccessfulRequests++;

                // 更新平均响应时间
                if (stats.TotalRequests == 1)
                {
                    stats.AverageResponseTime = response.ResponseTime;
                }
                else
                {
                    var totalMs = stats.AverageResponseTime.TotalMilliseconds * (stats.TotalRequests - 1) + response.ResponseTime.TotalMilliseconds;
                    stats.AverageResponseTime = TimeSpan.FromMilliseconds(totalMs / stats.TotalRequests);
                }

                // 更新令牌使用量
                if (response.Usage != null)
                {
                    stats.TotalTokensUsed += response.Usage.TotalTokens;
                }
            }
            else
            {
                stats.FailedRequests++;
            }
        }

        /// <summary>
        /// 提供者配置变更事件处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">事件参数</param>
        private void OnProviderConfigurationChanged(object? sender, ModelConfigurationChangedEventArgs e)
        {
            if (sender is IModelProvider provider)
            {
                _logger.LogInformation("提供者配置已变更: {ProviderName}", provider.ProviderName);
            }
        }

        /// <summary>
        /// 提供者连接状态变更事件处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">事件参数</param>
        private void OnProviderConnectionStatusChanged(object? sender, ConnectionStatusChangedEventArgs e)
        {
            if (sender is IModelProvider provider)
            {
                _logger.LogInformation("提供者连接状态已变更: {ProviderName}, 连接状态: {IsConnected}",
                    provider.ProviderName, e.IsConnected);

                if (!e.IsConnected && !string.IsNullOrEmpty(e.ErrorMessage))
                {
                    ErrorOccurred?.Invoke(this, new ModelErrorEventArgs(provider.ProviderName, e.ErrorMessage));
                }
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
                foreach (var provider in _providers.Values)
                {
                    try
                    {
                        provider.ConfigurationChanged -= OnProviderConfigurationChanged;
                        provider.ConnectionStatusChanged -= OnProviderConnectionStatusChanged;
                        provider.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "释放提供者资源失败: {ProviderName}", provider.ProviderName);
                    }
                }

                _providers.Clear();
                _statistics.Clear();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// 提供者变更事件参数
    /// </summary>
    public class ProviderChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 旧提供者名称
        /// </summary>
        public string OldProviderName { get; }

        /// <summary>
        /// 新提供者名称
        /// </summary>
        public string NewProviderName { get; }

        public ProviderChangedEventArgs(string oldProviderName, string newProviderName)
        {
            OldProviderName = oldProviderName;
            NewProviderName = newProviderName;
        }
    }

    /// <summary>
    /// 模型响应事件参数
    /// </summary>
    public class ModelResponseEventArgs : EventArgs
    {
        /// <summary>
        /// 提供者名称
        /// </summary>
        public string ProviderName { get; }

        /// <summary>
        /// 请求
        /// </summary>
        public ChatRequest Request { get; }

        /// <summary>
        /// 响应
        /// </summary>
        public ChatResponse Response { get; }

        public ModelResponseEventArgs(string providerName, ChatRequest request, ChatResponse response)
        {
            ProviderName = providerName;
            Request = request;
            Response = response;
        }
    }

    /// <summary>
    /// 模型错误事件参数
    /// </summary>
    public class ModelErrorEventArgs : EventArgs
    {
        /// <summary>
        /// 提供者名称
        /// </summary>
        public string ProviderName { get; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// 异常
        /// </summary>
        public Exception? Exception { get; }

        public ModelErrorEventArgs(string providerName, string errorMessage, Exception? exception = null)
        {
            ProviderName = providerName;
            ErrorMessage = errorMessage;
            Exception = exception;
        }
    }
}
