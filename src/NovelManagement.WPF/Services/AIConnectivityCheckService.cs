using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Http;
using NovelManagement.AI.Services.DeepSeek;
using NovelManagement.AI.Services.Ollama;
using NovelManagement.AI.Services.OpenAICompatible.Models;
using NovelManagement.AI.Services.RWKV;

namespace NovelManagement.WPF.Services;

/// <summary>
/// 统一执行各 AI 提供者连通性检测。
/// </summary>
public class AIConnectivityCheckService
{
    private readonly IConfiguration _configuration;
    private readonly IOllamaApiService _ollamaApiService;
    private readonly IDeepSeekApiService _deepSeekApiService;
    private readonly IRwkvLightningService _rwkvLightningService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AIConnectivityCheckService> _logger;

    public AIConnectivityCheckService(
        IConfiguration configuration,
        IOllamaApiService ollamaApiService,
        IDeepSeekApiService deepSeekApiService,
        IRwkvLightningService rwkvLightningService,
        IHttpClientFactory httpClientFactory,
        ILogger<AIConnectivityCheckService> logger)
    {
        _configuration = configuration;
        _ollamaApiService = ollamaApiService;
        _deepSeekApiService = deepSeekApiService;
        _rwkvLightningService = rwkvLightningService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// 执行 AI 提供者连通性检测。
    /// </summary>
    public async Task<IReadOnlyList<AIConnectivityItem>> CheckAllAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<AIConnectivityItem>();

        if (!_configuration.GetValue("Features:EnableAI", true))
        {
            items.Add(new AIConnectivityItem("AI 总开关", HealthStatus.Warning, "AI 功能已关闭，跳过连通性检测"));
            return items;
        }

        items.Add(await CheckOllamaAsync(cancellationToken));
        items.Add(await CheckRwkvAsync());
        items.Add(await CheckDeepSeekAsync());
        items.Add(await CheckOpenAiCompatibleAsync("智谱AI", "AI:Providers:ZhipuAI", cancellationToken));
        items.Add(await CheckOpenAiCompatibleAsync("OpenAI", "AI:Providers:OpenAI", cancellationToken));

        return items;
    }

    private async Task<AIConnectivityItem> CheckOllamaAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _ollamaApiService.TestConnectionAsync(cancellationToken);
            return result.IsSuccess
                ? new AIConnectivityItem("Ollama", HealthStatus.Healthy, $"连接成功，响应时间 {result.ResponseTime.TotalMilliseconds:F0} ms")
                : new AIConnectivityItem("Ollama", HealthStatus.Warning, result.ErrorMessage ?? "连接失败");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ollama 连通性检测失败");
            return new AIConnectivityItem("Ollama", HealthStatus.Warning, ex.Message);
        }
    }

    private async Task<AIConnectivityItem> CheckRwkvAsync()
    {
        try
        {
            var success = await _rwkvLightningService.TestConnectionAsync();
            return success
                ? new AIConnectivityItem("RWKV", HealthStatus.Healthy, "连接成功")
                : new AIConnectivityItem("RWKV", HealthStatus.Warning, "连接失败或服务未启动");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RWKV 连通性检测失败");
            return new AIConnectivityItem("RWKV", HealthStatus.Warning, ex.Message);
        }
    }

    private async Task<AIConnectivityItem> CheckDeepSeekAsync()
    {
        try
        {
            var config = _deepSeekApiService.GetConfiguration();
            if (!config.IsValid())
            {
                return new AIConnectivityItem("DeepSeek", HealthStatus.Warning, $"配置不完整: {string.Join("；", config.GetValidationErrors())}");
            }

            var success = await _deepSeekApiService.TestConnectionAsync();
            return success
                ? new AIConnectivityItem("DeepSeek", HealthStatus.Healthy, "连接成功")
                : new AIConnectivityItem("DeepSeek", HealthStatus.Warning, "连接失败");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DeepSeek 连通性检测失败");
            return new AIConnectivityItem("DeepSeek", HealthStatus.Warning, ex.Message);
        }
    }

    private async Task<AIConnectivityItem> CheckOpenAiCompatibleAsync(
        string displayName,
        string sectionPath,
        CancellationToken cancellationToken)
    {
        try
        {
            var config = new OpenAICompatibleConfiguration();
            _configuration.GetSection(sectionPath).Bind(config);

            if (!config.IsValid())
            {
                return new AIConnectivityItem(displayName, HealthStatus.Warning, $"配置不完整: {string.Join("；", config.GetValidationErrors())}");
            }

            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(5, config.TimeoutSeconds));
            if (!string.IsNullOrWhiteSpace(config.ApiKey))
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);
            }

            var startTime = DateTime.Now;
            var response = await httpClient.GetAsync($"{config.BaseUrl.TrimEnd('/')}/models", cancellationToken);
            var elapsed = DateTime.Now - startTime;

            return response.IsSuccessStatusCode
                ? new AIConnectivityItem(displayName, HealthStatus.Healthy, $"连接成功，响应时间 {elapsed.TotalMilliseconds:F0} ms")
                : new AIConnectivityItem(displayName, HealthStatus.Warning, $"HTTP {(int)response.StatusCode} {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{DisplayName} 连通性检测失败", displayName);
            return new AIConnectivityItem(displayName, HealthStatus.Warning, ex.Message);
        }
    }
}

/// <summary>
/// AI 连通性检测结果。
/// </summary>
public sealed record AIConnectivityItem(string Name, HealthStatus Status, string Message);
