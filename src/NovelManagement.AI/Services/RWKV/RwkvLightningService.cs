using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NovelManagement.AI.Services.RWKV.Models;

namespace NovelManagement.AI.Services.RWKV
{
    /// <summary>
    /// RWKV 推理服务实现（基于 rwkv_lightning_libtorch Python HTTP 服务）
    /// </summary>
    public class RwkvLightningService : IRwkvLightningService, IDisposable
    {
        private readonly ILogger<RwkvLightningService> _logger;
        private readonly HttpClient _httpClient;
        private RwkvConfiguration _configuration = new();
        private bool _isAvailable;
        private bool _disposed;
        private Process? _serverProcess;

        public RwkvLightningService(ILogger<RwkvLightningService> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        /// <inheritdoc/>
        public bool IsAvailable => _isAvailable;

        /// <inheritdoc/>
        public RwkvConfiguration Configuration => _configuration;

        /// <inheritdoc/>
        public async Task<bool> InitializeAsync(RwkvConfiguration configuration)
        {
            try
            {
                _configuration = configuration;
                _httpClient.Timeout = TimeSpan.FromSeconds(configuration.TimeoutSeconds);

                // 如果配置了自动启动，尝试启动 Python 推理服务
                if (configuration.AutoStartServer && !string.IsNullOrWhiteSpace(configuration.ServerScriptPath))
                {
                    await StartServerAsync();
                }

                // 测试连接
                _isAvailable = await TestConnectionAsync();

                if (_isAvailable)
                {
                    _logger.LogInformation("RWKV 推理服务初始化成功，地址: {BaseUrl}", configuration.BaseUrl);
                }
                else
                {
                    _logger.LogWarning("RWKV 推理服务不可用，地址: {BaseUrl}", configuration.BaseUrl);
                }

                return _isAvailable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RWKV 推理服务初始化失败");
                _isAvailable = false;
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_configuration.BaseUrl}/status");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var status = JsonSerializer.Deserialize<RwkvStatusResponse>(content);
                    _isAvailable = status?.Ready == true;
                    return _isAvailable;
                }
                _isAvailable = false;
                return false;
            }
            catch
            {
                _isAvailable = false;
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<RwkvStatusResponse> GetStatusAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_configuration.BaseUrl}/status");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<RwkvStatusResponse>(content) ?? new RwkvStatusResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取 RWKV 服务状态失败");
                return new RwkvStatusResponse();
            }
        }

        /// <inheritdoc/>
        public async Task<RwkvCompletionResponse> CompleteAsync(string prompt, int maxTokens = 200, string? direction = null, double? temperature = null)
        {
            var startTime = DateTime.Now;

            try
            {
                // 限制最大 token 数为 200
                maxTokens = Math.Clamp(maxTokens, 1, 200);

                // 构建续写提示词
                var fullPrompt = BuildCompletionPrompt(prompt, direction);

                var request = new RwkvCompletionRequest
                {
                    Prompt = fullPrompt,
                    MaxTokens = maxTokens,
                    Temperature = temperature ?? _configuration.Temperature,
                    TopP = _configuration.TopP,
                    TopK = _configuration.TopK,
                    FrequencyPenalty = _configuration.FrequencyPenalty,
                    PresencePenalty = _configuration.PresencePenalty,
                    Stop = new List<string> { "\n\n\n", "===" } // 防止生成过多空行或分隔符
                };

                var json = JsonSerializer.Serialize(request);
                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_configuration.BaseUrl}/complete", httpContent);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<RwkvCompletionResponse>(responseContent);

                if (result == null || !result.Success)
                {
                    throw new Exception(result?.Error ?? "RWKV 推理返回空结果");
                }

                // 后处理：清理续写内容
                result.Text = PostProcessCompletion(result.Text, prompt);

                var elapsed = DateTime.Now - startTime;
                _logger.LogInformation("RWKV 续写完成，生成 {TokenCount} tokens，耗时 {ElapsedMs}ms",
                    result.TokensGenerated, elapsed.TotalMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RWKV 续写失败");
                return new RwkvCompletionResponse
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        /// <inheritdoc/>
        public async Task<RwkvCompletionResponse> CompleteStreamAsync(string prompt, int maxTokens = 200, Action<string>? onTokenReceived = null, string? direction = null, double? temperature = null)
        {
            var startTime = DateTime.Now;
            var fullText = new StringBuilder();

            try
            {
                maxTokens = Math.Clamp(maxTokens, 1, 200);
                var fullPrompt = BuildCompletionPrompt(prompt, direction);

                var request = new RwkvCompletionRequest
                {
                    Prompt = fullPrompt,
                    MaxTokens = maxTokens,
                    Temperature = temperature ?? _configuration.Temperature,
                    TopP = _configuration.TopP,
                    TopK = _configuration.TopK,
                    FrequencyPenalty = _configuration.FrequencyPenalty,
                    PresencePenalty = _configuration.PresencePenalty,
                    Stop = new List<string> { "\n\n\n", "===" }
                };

                var json = JsonSerializer.Serialize(request);
                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_configuration.BaseUrl}/complete/stream")
                {
                    Content = httpContent
                };

                using var stream = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);
                using var reader = new StreamReader(await stream.Content.ReadAsStreamAsync());

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(line)) continue;

                    if (line.StartsWith("data: "))
                    {
                        var data = line[6..];
                        if (data == "[DONE]") break;

                        try
                        {
                            var chunk = JsonSerializer.Deserialize<RwkvCompletionResponse>(data);
                            if (chunk?.Success == true && !string.IsNullOrEmpty(chunk.Text))
                            {
                                fullText.Append(chunk.Text);
                                onTokenReceived?.Invoke(chunk.Text);
                            }
                        }
                        catch (JsonException) { /* 忽略解析错误 */ }
                    }
                }

                var result = new RwkvCompletionResponse
                {
                    Text = PostProcessCompletion(fullText.ToString(), prompt),
                    Success = true,
                    TokensGenerated = fullText.Length // 近似值
                };

                var elapsed = DateTime.Now - startTime;
                _logger.LogInformation("RWKV 流式续写完成，耗时 {ElapsedMs}ms", elapsed.TotalMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RWKV 流式续写失败");
                return new RwkvCompletionResponse
                {
                    Text = fullText.ToString(),
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        /// <inheritdoc/>
        public async Task<bool> LoadModelAsync(string modelPath, string strategy)
        {
            try
            {
                var request = new { model_path = modelPath, strategy };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_configuration.BaseUrl}/load_model", content);
                if (response.IsSuccessStatusCode)
                {
                    _isAvailable = true;
                    _logger.LogInformation("RWKV 模型加载成功: {ModelPath}", modelPath);
                    return true;
                }

                _logger.LogWarning("RWKV 模型加载失败: {ModelPath}", modelPath);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RWKV 模型加载异常");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task UnloadModelAsync()
        {
            try
            {
                await _httpClient.PostAsync($"{_configuration.BaseUrl}/unload_model", null);
                _isAvailable = false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RWKV 模型卸载异常");
            }
        }

        /// <summary>
        /// 构建续写提示词（参考 RWKV Completion Prompts 规范）
        /// </summary>
        private string BuildCompletionPrompt(string existingContent, string? direction)
        {
            var prompt = new StringBuilder();

            // RWKV 续写模式：直接拼接前文，模型会自然续写
            // 参考 https://www.rwkv.cn/docs/RWKV-Prompts/Completion-Prompts
            prompt.Append(existingContent);

            // 如果有续写方向/要求，在末尾添加引导
            if (!string.IsNullOrWhiteSpace(direction))
            {
                // 确保前文末尾有换行
                if (!existingContent.EndsWith("\n"))
                    prompt.AppendLine();

                prompt.AppendLine();
                prompt.Append($"<!-- 续写方向：{direction} -->");
                prompt.AppendLine();
            }

            return prompt.ToString();
        }

        /// <summary>
        /// 后处理续写内容
        /// </summary>
        private string PostProcessCompletion(string completion, string originalPrompt)
        {
            if (string.IsNullOrEmpty(completion)) return completion;

            // 移除可能的 HTML 注释标记（续写方向引导）
            var start = completion.IndexOf("-->");
            if (start >= 0)
            {
                completion = completion[(start + 3)..].TrimStart('\n', '\r', ' ');
            }

            // 移除开头和结尾的空白
            completion = completion.Trim();

            // 确保不会重复前文的最后一段
            if (!string.IsNullOrEmpty(originalPrompt))
            {
                var lastParagraph = originalPrompt.Split("\n\n").LastOrDefault()?.Trim();
                if (!string.IsNullOrEmpty(lastParagraph) && completion.StartsWith(lastParagraph))
                {
                    completion = completion[lastParagraph.Length..].TrimStart('\n', '\r', ' ');
                }
            }

            return completion;
        }

        /// <summary>
        /// 启动 Python 推理服务
        /// </summary>
        private async Task StartServerAsync()
        {
            try
            {
                var arguments = $"\"{_configuration.ServerScriptPath}\" --port {_configuration.BaseUrl.Split(':').Last().TrimEnd('/')}";

                if (!string.IsNullOrWhiteSpace(_configuration.ModelPath))
                    arguments += $" --model \"{_configuration.ModelPath}\"";

                if (!string.IsNullOrWhiteSpace(_configuration.Strategy))
                    arguments += $" --strategy \"{_configuration.Strategy}\"";

                if (_configuration.GpuDeviceId >= 0)
                    arguments += $" --gpu {_configuration.GpuDeviceId}";

                var startInfo = new ProcessStartInfo
                {
                    FileName = _configuration.PythonPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                _serverProcess = Process.Start(startInfo);
                if (_serverProcess != null)
                {
                    _logger.LogInformation("RWKV 推理服务进程已启动，PID: {Pid}", _serverProcess.Id);

                    // 等待服务就绪（最多等待30秒）
                    for (int i = 0; i < 30; i++)
                    {
                        await Task.Delay(1000);
                        if (await TestConnectionAsync())
                        {
                            _logger.LogInformation("RWKV 推理服务已就绪");
                            return;
                        }
                    }

                    _logger.LogWarning("RWKV 推理服务启动超时");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启动 RWKV 推理服务失败");
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                if (_serverProcess != null && !_serverProcess.HasExited)
                {
                    try
                    {
                        _serverProcess.Kill(entireProcessTree: true);
                        _logger.LogInformation("RWKV 推理服务进程已终止");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "终止 RWKV 推理服务进程失败");
                    }
                    _serverProcess.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
