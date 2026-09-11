using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using NovelManagement.AI.Services.RWKV.Models;

namespace NovelManagement.AI.Services.RWKV
{
    /// <summary>
    /// RWKV 推理服务实现，兼容旧版 libtorch 服务与新版纯 CUDA HTTP 服务。
    /// </summary>
    public class RwkvLightningService : IRwkvLightningService, IDisposable
    {
        private const int DefaultCudaResponseChunkSize = 8;

        /// <summary>
        /// 单次续写最大 token 上限（原 200 无法支撑章节级生成；RWKV7-G1i 上下文 16384，
        /// 服务端由 stop_tokens 兜底，放宽到 8192 以支持双 Agent 长文生成）。
        /// </summary>
        private const int RwkvMaxTokensCeiling = 8192;

        private enum RwkvApiFlavor
        {
            Unknown = 0,
            LegacyLibtorch = 1,
            CudaHttp = 2,
            LlamaCpp = 3
        }

        private readonly ILogger<RwkvLightningService> _logger;
        private readonly HttpClient _httpClient;
        private RwkvConfiguration _configuration = new();
        private bool _isAvailable;
        private bool _disposed;
        private Process? _serverProcess;
        private SemaphoreSlim _requestSemaphore = new(20, 20);
        private RwkvApiFlavor _apiFlavor = RwkvApiFlavor.Unknown;

        // ====== LlamaCpp flavor：进程内 state 会话仿真（llama.cpp 无 state 会话 API）======
        private const int LlamaSessionMaxChars = 14000;      // 会话累计字符上限（约 1.1 万 token，留足 16K ctx 余量）
        private const int LlamaSessionKeepHeadChars = 2000;  // 超限时保留的开头字符数
        private const int LlamaSessionKeepTailChars = 8000;  // 超限时保留的结尾字符数
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, LlamaSessionBuffer> _llamaSessions = new();

        private sealed class LlamaSessionBuffer
        {
            public StringBuilder Accumulated { get; } = new();
            public DateTimeOffset LastUsed { get; set; } = DateTimeOffset.UtcNow;
        }

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
                _apiFlavor = ResolveConfiguredApiFlavor(configuration);
                _httpClient.Timeout = TimeSpan.FromSeconds(configuration.TimeoutSeconds);
                _requestSemaphore.Dispose();
                _requestSemaphore = new SemaphoreSlim(configuration.MaxConcurrentRequests, configuration.MaxConcurrentRequests);

                // 如果配置了自动启动，尝试启动 Python 推理服务
                if (configuration.AutoStartServer && !string.IsNullOrWhiteSpace(configuration.ServerScriptPath))
                {
                    await StartServerAsync();
                }

                // 测试连接
                _isAvailable = await TestConnectionAsync();

                if (_isAvailable)
                {
                    // 不打印服务地址，避免远程隧道 URL 泄入日志文件
                    _logger.LogInformation("RWKV 推理服务初始化成功");
                }
                else
                {
                    _logger.LogWarning("RWKV 推理服务不可用（地址已从日志脱敏），请检查 AI 模型配置");
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
                _apiFlavor = await DetectApiFlavorAsync();
                _isAvailable = _apiFlavor != RwkvApiFlavor.Unknown;
                return _isAvailable;
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
                var flavor = await EnsureApiFlavorAsync();
                if (flavor == RwkvApiFlavor.LlamaCpp)
                {
                    using var llamaRequest = new HttpRequestMessage(HttpMethod.Get, $"{_configuration.BaseUrl.TrimEnd('/')}/health");
                    ApplyAuthHeader(llamaRequest);
                    using var llamaResponse = await _httpClient.SendAsync(llamaRequest);
                    var llamaContent = await llamaResponse.Content.ReadAsStringAsync();
                    return new RwkvStatusResponse
                    {
                        Ready = llamaResponse.IsSuccessStatusCode,
                        Model = _configuration.ModelName,
                        Strategy = _configuration.Strategy,
                        GpuInfo = llamaContent
                    };
                }

                if (flavor == RwkvApiFlavor.CudaHttp)
                {
                    try
                    {
                        using var request = new HttpRequestMessage(HttpMethod.Get, $"{_configuration.BaseUrl.TrimEnd('/')}/v1/server/status");
                        ApplyAuthHeader(request);
                        using var response = await _httpClient.SendAsync(request);
                        if (response.IsSuccessStatusCode)
                        {
                            var content = await response.Content.ReadAsStringAsync();
                            return ParseCudaStatusResponse(content);
                        }
                    }
                    catch
                    {
                        // 新版纯 CUDA 构建不暴露 /v1/server/status，回退 state 状态端点探活。
                    }
                }

                var requestBody = BuildOptionalPasswordBody();
                using var legacyResponse = await _httpClient.PostAsync(
                    $"{_configuration.BaseUrl.TrimEnd('/')}/state/status",
                    new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));
                legacyResponse.EnsureSuccessStatusCode();
                var legacyContent = await legacyResponse.Content.ReadAsStringAsync();
                return new RwkvStatusResponse
                {
                    Ready = true,
                    Model = _configuration.ModelName,
                    Strategy = _configuration.Strategy,
                    GpuInfo = legacyContent
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取 RWKV 服务状态失败");
                return new RwkvStatusResponse();
            }
        }

        /// <inheritdoc/>
        public async Task<RwkvCompletionResponse> CompleteAsync(
            string prompt,
            int maxTokens = 200,
            string? direction = null,
            double? temperature = null,
            double? topP = null,
            double? presencePenalty = null,
            double? frequencyPenalty = null,
            int? topK = null)
        {
            var startTime = DateTime.Now;

            try
            {
                await _requestSemaphore.WaitAsync();

                // 限制最大 token 数为 200
                maxTokens = Math.Clamp(maxTokens, 1, RwkvMaxTokensCeiling);

                // 构建续写提示词
                var fullPrompt = BuildCompletionPrompt(prompt, direction);

                var request = new RwkvCompletionRequest
                {
                    Prompt = fullPrompt,
                    MaxTokens = maxTokens,
                    Temperature = temperature ?? _configuration.Temperature,
                    TopP = topP ?? _configuration.TopP,
                    TopK = topK ?? _configuration.TopK,
                    FrequencyPenalty = frequencyPenalty ?? _configuration.FrequencyPenalty,
                    PresencePenalty = presencePenalty ?? _configuration.PresencePenalty,
                    Stop = new List<string> { "\n\n\n", "===" } // 防止生成过多空行或分隔符
                };

                var flavor = await EnsureApiFlavorAsync();
                var json = JsonSerializer.Serialize(BuildChatRequest(request, flavor));
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, BuildChatCompletionsEndpoint(flavor))
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                ApplyAuthHeader(httpRequest);

                var response = await _httpClient.SendAsync(httpRequest);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = ParseCompletionResponse(responseContent);

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
            finally
            {
                _requestSemaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<RwkvCompletionResponse> CompleteStreamAsync(
            string prompt,
            int maxTokens = 200,
            Action<string>? onTokenReceived = null,
            string? direction = null,
            double? temperature = null,
            double? topP = null,
            double? presencePenalty = null,
            double? frequencyPenalty = null,
            int? topK = null)
        {
            var startTime = DateTime.Now;
            var fullText = new StringBuilder();

            try
            {
                // 注意：本方法可能从 UI 线程调用。所有 await 必须 ConfigureAwait(false)，
                // 否则 SSE 每行续体都会被调度回 UI 线程；而 while (!reader.EndOfStream) 的
                // 同步 EndOfStream 属性会在无数据时同步阻塞 socket 读——两者叠加就是
                // 「生成期间窗口未响应」的直接原因（线程栈实锤：CompleteStreamAsync 状态机
                // 跑在 UI 线程的 DispatcherOperation 里，卡在 SslStream.Read）。
                await _requestSemaphore.WaitAsync().ConfigureAwait(false);

                maxTokens = Math.Clamp(maxTokens, 1, RwkvMaxTokensCeiling);
                var fullPrompt = BuildCompletionPrompt(prompt, direction);

                var request = new RwkvCompletionRequest
                {
                    Prompt = fullPrompt,
                    MaxTokens = maxTokens,
                    Temperature = temperature ?? _configuration.Temperature,
                    TopP = topP ?? _configuration.TopP,
                    TopK = topK ?? _configuration.TopK,
                    FrequencyPenalty = frequencyPenalty ?? _configuration.FrequencyPenalty,
                    PresencePenalty = presencePenalty ?? _configuration.PresencePenalty,
                    Stop = new List<string> { "\n\n\n", "===" }
                };

                var flavor = await EnsureApiFlavorAsync().ConfigureAwait(false);
                var json = JsonSerializer.Serialize(BuildChatRequest(request, flavor, stream: true));
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, BuildChatCompletionsEndpoint(flavor))
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                ApplyAuthHeader(httpRequest);

                using var stream = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                using var reader = new StreamReader(await stream.Content.ReadAsStreamAsync().ConfigureAwait(false));

                // 用异步 ReadLine 循环（绝不在 UI 线程做同步 EndOfStream 轮询）
                string? line;
                while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
                {
                    if (string.IsNullOrEmpty(line)) continue;

                    if (line.StartsWith("data: "))
                    {
                        var data = line[6..];
                        if (data == "[DONE]") break;

                        try
                        {
                            using var document = JsonDocument.Parse(data);
                            var text = ExtractOpenAiDeltaText(document.RootElement);
                            if (!string.IsNullOrEmpty(text))
                            {
                                fullText.Append(text);
                                onTokenReceived?.Invoke(text);
                            }
                        }
                        catch (JsonException) { /* 忽略解析错误 */ }
                    }
                }

                var rawText = fullText.ToString();

                // 空格健全性检查：部分 RWKV 服务端的流式分词会丢失词间空格
                // （非流式输出正常，流式全粘连）。检测到则用非流式重试一次。
                if (LooksLikeMissingSpaces(rawText))
                {
                    _logger.LogWarning("RWKV 流式输出疑似丢失空格（长度 {Len}），回退非流式重试一次", rawText.Length);
                    var retry = await CompleteAsync(
                        prompt,
                        maxTokens,
                        direction,
                        temperature,
                        topP,
                        presencePenalty,
                        frequencyPenalty,
                        topK).ConfigureAwait(false);
                    if (retry.Success && !LooksLikeMissingSpaces(retry.Text))
                    {
                        var retryElapsed = DateTime.Now - startTime;
                        _logger.LogInformation("RWKV 非流式回退成功，耗时 {ElapsedMs}ms", retryElapsed.TotalMilliseconds);
                        return retry;
                    }
                    // 回退仍异常（或 524 失败）：保留流式结果，交由上层处理
                }

                var result = new RwkvCompletionResponse
                {
                    Text = PostProcessCompletion(rawText, prompt),
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
            finally
            {
                _requestSemaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<RwkvCompletionResponse> CompleteWithStateAsync(
            string sessionId,
            string prompt,
            int maxTokens = 200,
            string? direction = null,
            double? temperature = null,
            double? topP = null,
            double? presencePenalty = null,
            double? frequencyPenalty = null,
            int? topK = null)
        {
            var startTime = DateTime.Now;

            try
            {
                await EnsureApiFlavorAsync();
                maxTokens = Math.Clamp(maxTokens, 1, RwkvMaxTokensCeiling);
                var fullPrompt = BuildCompletionPrompt(prompt, direction);

                if (_apiFlavor == RwkvApiFlavor.LlamaCpp)
                {
                    return await CompleteWithLlamaSessionAsync(
                        sessionId,
                        fullPrompt,
                        maxTokens,
                        temperature ?? _configuration.Temperature,
                        topP ?? _configuration.TopP,
                        presencePenalty ?? _configuration.PresencePenalty,
                        frequencyPenalty ?? _configuration.FrequencyPenalty,
                        topK ?? _configuration.TopK,
                        startTime);
                }

                var requestBody = BuildStateChatRequest(
                    sessionId,
                    fullPrompt,
                    maxTokens,
                    temperature ?? _configuration.Temperature,
                    topP ?? _configuration.TopP,
                    presencePenalty ?? _configuration.PresencePenalty,
                    frequencyPenalty ?? _configuration.FrequencyPenalty,
                    topK ?? _configuration.TopK);
                var response = await _httpClient.PostAsync(
                    $"{_configuration.BaseUrl.TrimEnd('/')}/state/chat/completions",
                    new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = ParseCompletionResponse(responseContent);
                result.Text = PostProcessCompletion(result.Text, prompt);

                var elapsed = DateTime.Now - startTime;
                _logger.LogInformation("RWKV state 续写完成，会话 {SessionId}，生成 {TokenCount} tokens，耗时 {ElapsedMs}ms",
                    sessionId, result.TokensGenerated, elapsed.TotalMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RWKV state 续写失败，会话 {SessionId}", sessionId);
                return new RwkvCompletionResponse
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        /// <summary>
        /// llama.cpp flavor 的 state 会话仿真：
        /// 进程内缓存会话累计提示词（前文 + 已生成内容），每次请求携带全量累计文本，
        /// llama-server 的 cache_prompt 机制会自动复用公共前缀做增量预填充。
        /// </summary>
        private async Task<RwkvCompletionResponse> CompleteWithLlamaSessionAsync(
            string sessionId,
            string fullPrompt,
            int maxTokens,
            double temperature,
            double topP,
            double presencePenalty,
            double frequencyPenalty,
            int topK,
            DateTime startTime)
        {
            try
            {
                var buffer = _llamaSessions.GetOrAdd(sessionId, _ => new LlamaSessionBuffer());
                buffer.LastUsed = DateTimeOffset.UtcNow;

                string full;
                lock (buffer)
                {
                    TrimLlamaSessionBuffer(buffer);
                    full = buffer.Accumulated.Length == 0
                        ? fullPrompt
                        : buffer.Accumulated + fullPrompt;
                }

                var request = new RwkvCompletionRequest
                {
                    Prompt = full,
                    MaxTokens = maxTokens,
                    Temperature = temperature,
                    TopP = topP,
                    TopK = topK,
                    FrequencyPenalty = frequencyPenalty,
                    PresencePenalty = presencePenalty,
                    Stop = new List<string> { "\n\n\n", "===" }
                };

                var json = JsonSerializer.Serialize(BuildChatRequest(request, RwkvApiFlavor.LlamaCpp));
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, BuildChatCompletionsEndpoint(RwkvApiFlavor.LlamaCpp))
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                ApplyAuthHeader(httpRequest);
                using var response = await _httpClient.SendAsync(httpRequest);
                response.EnsureSuccessStatusCode();

                var result = ParseCompletionResponse(await response.Content.ReadAsStringAsync());
                result.Text = PostProcessCompletion(result.Text, full);

                lock (buffer)
                {
                    buffer.Accumulated.Append(fullPrompt);
                    buffer.Accumulated.Append(result.Text);
                }

                _logger.LogInformation(
                    "llama.cpp state 续写完成，会话 {SessionId}，累计 {Chars} 字符，生成 {TokenCount} tokens，耗时 {ElapsedMs}ms",
                    sessionId, buffer.Accumulated.Length, result.TokensGenerated, (DateTime.Now - startTime).TotalMilliseconds);
                return result;
            }
            catch (Exception ex)
            {
                // 会话可能已损坏（如超上下文），清空以便下次重建
                _llamaSessions.TryRemove(sessionId, out _);
                _logger.LogError(ex, "llama.cpp state 续写失败，会话 {SessionId}", sessionId);
                return new RwkvCompletionResponse
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        /// <summary>
        /// 会话累计文本超限时截取“开头 + 中段省略 + 结尾”，防止请求超出模型上下文。
        /// </summary>
        private static void TrimLlamaSessionBuffer(LlamaSessionBuffer buffer)
        {
            var s = buffer.Accumulated;
            if (s.Length <= LlamaSessionMaxChars)
            {
                return;
            }

            var head = s.ToString(0, LlamaSessionKeepHeadChars);
            var tail = s.ToString(s.Length - LlamaSessionKeepTailChars, LlamaSessionKeepTailChars);
            s.Clear();
            s.Append(head).Append("\n\n（……中段情节略……）\n\n").Append(tail);
        }

        /// <inheritdoc/>
        public async Task<RwkvBatchCompletionResponse> CompleteBatchAsync(
            IReadOnlyList<string> prompts,
            int maxTokens = 200,
            double? temperature = null,
            double? topP = null,
            double? presencePenalty = null,
            double? frequencyPenalty = null,
            int? topK = null,
            int chunkSize = 8)
        {
            try
            {
                if (prompts == null || prompts.Count == 0)
                {
                    return new RwkvBatchCompletionResponse
                    {
                        Success = false,
                        Error = "批量提示词不能为空"
                    };
                }

                maxTokens = Math.Clamp(maxTokens, 1, RwkvMaxTokensCeiling);
                chunkSize = Math.Clamp(chunkSize, 1, 32);
                var flavor = await EnsureApiFlavorAsync();

                if (flavor == RwkvApiFlavor.LlamaCpp)
                {
                    // llama.cpp 无批量端点，退化为逐条调用
                    var items = new List<RwkvBatchCompletionItem>();
                    for (var i = 0; i < prompts.Count; i++)
                    {
                        var resp = await CompleteAsync(
                            prompts[i], maxTokens, null, temperature, topP, presencePenalty, frequencyPenalty, topK);
                        items.Add(new RwkvBatchCompletionItem
                        {
                            Index = i,
                            Text = resp.Success ? resp.Text : string.Empty,
                            TokensGenerated = resp.TokensGenerated
                        });
                    }

                    return new RwkvBatchCompletionResponse
                    {
                        Success = items.Any(x => !string.IsNullOrWhiteSpace(x.Text)),
                        Error = items.All(x => string.IsNullOrWhiteSpace(x.Text)) ? "llama.cpp 批量续写全部为空" : null,
                        Items = items
                    };
                }

                var requestBody = BuildBigBatchRequest(
                    prompts,
                    maxTokens,
                    temperature ?? _configuration.Temperature,
                    topP ?? _configuration.TopP,
                    presencePenalty ?? _configuration.PresencePenalty,
                    frequencyPenalty ?? _configuration.FrequencyPenalty,
                    topK ?? _configuration.TopK,
                    chunkSize);

                var response = await _httpClient.PostAsync(
                    BuildBatchCompletionsEndpoint(flavor),
                    new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = ParseBatchCompletionResponse(responseContent, prompts);
                foreach (var item in result.Items)
                {
                    if (item.Index >= 0 && item.Index < prompts.Count)
                    {
                        item.Text = PostProcessCompletion(item.Text, prompts[item.Index]);
                    }
                }

                result.Success = result.Items.Count > 0;
                if (!result.Success && string.IsNullOrWhiteSpace(result.Error))
                {
                    result.Error = "RWKV big_batch 未返回可用结果";
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RWKV 批量续写失败");
                return new RwkvBatchCompletionResponse
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteStateAsync(string sessionId)
        {
            try
            {
                if (_apiFlavor == RwkvApiFlavor.LlamaCpp)
                {
                    return _llamaSessions.TryRemove(sessionId, out _);
                }

                var body = BuildOptionalPasswordBody();
                body["session_id"] = sessionId;
                var response = await _httpClient.PostAsync(
                    $"{_configuration.BaseUrl.TrimEnd('/')}/state/delete",
                    new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "删除 RWKV state 失败，会话 {SessionId}", sessionId);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> LoadModelAsync(string modelPath, string strategy)
        {
            try
            {
                _configuration.ModelPath = modelPath;
                _configuration.Strategy = strategy;

                if (_configuration.AutoStartServer && !string.IsNullOrWhiteSpace(_configuration.ServerScriptPath))
                {
                    if (_serverProcess != null && !_serverProcess.HasExited)
                    {
                        _serverProcess.Kill(entireProcessTree: true);
                        _serverProcess.Dispose();
                        _serverProcess = null;
                    }

                    await StartServerAsync();
                    _isAvailable = await TestConnectionAsync();
                    return _isAvailable;
                }

                _logger.LogWarning("当前 RWKV 服务未启用自动启动，无法通过 HTTP 热切换模型: {ModelPath}", modelPath);
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
                if (_serverProcess != null && !_serverProcess.HasExited)
                {
                    _serverProcess.Kill(entireProcessTree: true);
                    _serverProcess.Dispose();
                    _serverProcess = null;
                }
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

            return prompt.ToString().TrimEnd(' ', '\t');
        }

        /// <summary>
        /// 后处理续写内容
        /// </summary>
        private string PostProcessCompletion(string completion, string originalPrompt)
        {
            if (string.IsNullOrEmpty(completion)) return completion;

            // 剥离 world 模型正文前的固定思维链规划前缀（"Let me craft..." 等）与 </think> 思考块
            completion = RwkvThinkingStripper.Strip(completion);
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

            // 折叠完全重复的句子（RWKV7 长文复读倾向：惩罚参数只能降低概率，无法根除；
            // 保留首次出现，删除后续完全相同的句子。仅处理长度 > 20 字符的句子，避免破坏短句节奏）
            completion = CollapseDuplicateSentences(completion);

            return completion;
        }

        /// <summary>
        /// 折叠正文中完全重复的句子（保留首次出现）。
        /// </summary>
        private static string CollapseDuplicateSentences(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;

            var parts = Regex.Split(text, @"(?<=[.!?][""”']?)\s+");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var kept = new List<string>();

            foreach (var raw in parts)
            {
                var sentence = raw.Trim();
                if (sentence.Length > 20)
                {
                    if (!seen.Add(sentence))
                    {
                        continue; // 完全重复的长句：丢弃后续出现
                    }
                }
                kept.Add(raw);
            }

            var rebuilt = string.Join(" ", kept.Select(k => k.TrimEnd()));
            rebuilt = Regex.Replace(rebuilt, @"(\r?\n){3,}", "\n\n"); // 收敛多余空行
            return rebuilt.Trim();
        }

        /// <summary>
        /// 检测文本是否疑似丢失词间空格（仅对拉丁字母文本判定；中文天然无空格）。
        /// </summary>
        private static bool LooksLikeMissingSpaces(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length < 200)
            {
                return false;
            }

            var letters = 0;
            var spaces = 0;
            var cjk = 0;
            foreach (var ch in text)
            {
                if (char.IsWhiteSpace(ch)) { spaces++; }
                else if (char.IsLetter(ch))
                {
                    letters++;
                    if (ch >= 0x4E00 && ch <= 0x9FFF) { cjk++; }
                }
            }

            if (letters == 0 || cjk > letters / 4)
            {
                return false; // 以中文为主的文本不判定
            }

            var spaceRatio = (double)spaces / (letters + spaces);
            return spaceRatio < 0.06; // 正常英文约 15-18%
        }

        /// <summary>
        /// 启动 RWKV 推理服务
        /// </summary>
        private async Task StartServerAsync()
        {
            try
            {
                if (await TestConnectionAsync())
                {
                    _logger.LogInformation("RWKV 推理服务已在线，跳过重复启动");
                    return;
                }

                var isExecutable = _configuration.ServerScriptPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
                var port = new Uri(_configuration.BaseUrl).Port;
                var startInfo = isExecutable
                    ? BuildExecutableStartInfo(port)
                    : BuildPythonStartInfo(port);

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

        private ProcessStartInfo BuildExecutableStartInfo(int port)
        {
            var flavor = ResolveConfiguredApiFlavor(_configuration);
            var uri = new Uri(_configuration.BaseUrl);
            var arguments = new StringBuilder();
            arguments.Append($"--port {port}");

            if (!string.IsNullOrWhiteSpace(_configuration.ModelPath))
            {
                // llama-server（标准 llama.cpp 构建）只认 --model，--model-path 会启动失败 ExitCode 1；
                // RWKV CUDA 后端（rwkv_lighting_cuda.exe）使用 --model-path
                var modelArgument = flavor == RwkvApiFlavor.LlamaCpp ? "--model" : "--model-path";
                arguments.Append($" {modelArgument} \"{_configuration.ModelPath}\"");
            }

            if (flavor == RwkvApiFlavor.CudaHttp)
            {
                arguments.Append($" --host {uri.Host}");
                arguments.Append($" --chunk-size {Math.Clamp(_configuration.PrefillChunkSize, 1, 4096)}");
            }

            if (flavor == RwkvApiFlavor.CudaHttp)
            {
                var vocabPath = ResolveVocabPath();
                if (!string.IsNullOrWhiteSpace(vocabPath))
                {
                    arguments.Append($" --vocab-path \"{vocabPath}\"");
                }
            }

            if (!string.IsNullOrWhiteSpace(_configuration.Password))
            {
                // llama-server 的鉴权参数是 --api-key（客户端以 Bearer 发送）；RWKV 后端为 --password
                if (flavor == RwkvApiFlavor.LlamaCpp)
                {
                    arguments.Append($" --api-key \"{_configuration.Password}\"");
                }
                else
                {
                    arguments.Append($" --password \"{_configuration.Password}\"");
                }
            }

            return new ProcessStartInfo
            {
                FileName = _configuration.ServerScriptPath,
                Arguments = arguments.ToString(),
                WorkingDirectory = Path.GetDirectoryName(_configuration.ServerScriptPath) ?? AppDomain.CurrentDomain.BaseDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
        }

        private ProcessStartInfo BuildPythonStartInfo(int port)
        {
            var arguments = new StringBuilder();
            arguments.Append($"\"{_configuration.ServerScriptPath}\" --port {port}");

            if (!string.IsNullOrWhiteSpace(_configuration.ModelPath))
                arguments.Append($" --model \"{_configuration.ModelPath}\"");

            if (!string.IsNullOrWhiteSpace(_configuration.Strategy))
                arguments.Append($" --strategy \"{_configuration.Strategy}\"");

            if (_configuration.GpuDeviceId >= 0)
                arguments.Append($" --gpu {_configuration.GpuDeviceId}");

            return new ProcessStartInfo
            {
                FileName = _configuration.PythonPath,
                Arguments = arguments.ToString(),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
        }

        private string ResolveVocabPath()
        {
            if (!string.IsNullOrWhiteSpace(_configuration.VocabPath))
            {
                return _configuration.VocabPath;
            }

            var executableDirectory = Path.GetDirectoryName(_configuration.ServerScriptPath);
            if (string.IsNullOrWhiteSpace(executableDirectory))
            {
                return string.Empty;
            }

            var candidate = Path.Combine(executableDirectory, "rwkv_vocab_v20230424.txt");
            return File.Exists(candidate) ? candidate : string.Empty;
        }

        private object BuildChatRequest(RwkvCompletionRequest request, RwkvApiFlavor flavor, bool stream = false)
        {
            if (flavor == RwkvApiFlavor.LlamaCpp)
            {
                // llama.cpp 原生文本补全端点：保持 RWKV completion 提示格式，
                // top_k=0 表示禁用；cache_prompt 复用公共前缀做增量预填充；
                // DRY 抗复读采样抑制长序列重复（参数来自 RwkvConfiguration，可经 appsettings 调整）
                return new
                {
                    prompt = request.Prompt,
                    max_tokens = request.MaxTokens,
                    temperature = request.Temperature,
                    top_p = request.TopP,
                    top_k = request.TopK,
                    presence_penalty = request.PresencePenalty,
                    frequency_penalty = request.FrequencyPenalty,
                    dry_multiplier = _configuration.DryMultiplier,
                    dry_base = _configuration.DryBase,
                    dry_allowed_length = _configuration.DryAllowedLength,
                    dry_penalty_last_n = _configuration.DryPenaltyLastN,
                    cache_prompt = true,
                    stop = request.Stop.Count == 0 ? null : request.Stop,
                    stream
                };
            }

            if (flavor == RwkvApiFlavor.CudaHttp)
            {
                return new
                {
                    contents = new[] { request.Prompt },
                    max_tokens = request.MaxTokens,
                    temperature = request.Temperature,
                    top_k = request.TopK,
                    top_p = request.TopP,
                    alpha_presence = request.PresencePenalty,
                    alpha_frequency = request.FrequencyPenalty,
                    alpha_decay = 0.99,
                    stop_tokens = new[] { 0, 261, 24281 },
                    chunk_size = DefaultCudaResponseChunkSize,
                    password = string.IsNullOrWhiteSpace(_configuration.Password) ? null : _configuration.Password,
                    stream
                };
            }

            return new
            {
                model = string.IsNullOrWhiteSpace(_configuration.ModelName) ? "rwkv7-g1i" : _configuration.ModelName,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = request.Prompt
                    }
                },
                max_tokens = request.MaxTokens,
                temperature = request.Temperature,
                top_p = request.TopP,
                presence_penalty = request.PresencePenalty,
                frequency_penalty = request.FrequencyPenalty,
                stop = request.Stop.Count == 0 ? null : request.Stop,
                stream
            };
        }

        private Dictionary<string, object> BuildStateChatRequest(
            string sessionId,
            string prompt,
            int maxTokens,
            double temperature,
            double topP,
            double presencePenalty,
            double frequencyPenalty,
            int topK)
        {
            var body = new Dictionary<string, object>
            {
                ["session_id"] = sessionId,
                ["contents"] = new[] { prompt },
                ["max_tokens"] = maxTokens,
                ["stop_tokens"] = new[] { 0, 261, 24281 },
                ["temperature"] = temperature,
                ["top_k"] = topK,
                ["top_p"] = topP,
                ["alpha_presence"] = presencePenalty,
                ["alpha_frequency"] = frequencyPenalty,
                ["alpha_decay"] = 0.99,
                ["chunk_size"] = DefaultCudaResponseChunkSize,
                ["stream"] = false
            };

            if (!string.IsNullOrWhiteSpace(_configuration.Password))
            {
                body["password"] = _configuration.Password;
            }

            return body;
        }

        private Dictionary<string, object> BuildBigBatchRequest(
            IReadOnlyList<string> prompts,
            int maxTokens,
            double temperature,
            double topP,
            double presencePenalty,
            double frequencyPenalty,
            int topK,
            int chunkSize)
        {
            var body = new Dictionary<string, object>
            {
                ["contents"] = prompts.ToArray(),
                ["max_tokens"] = maxTokens,
                ["stop_tokens"] = new[] { 0, 261, 24281 },
                ["temperature"] = temperature,
                ["top_k"] = topK,
                ["top_p"] = topP,
                ["alpha_presence"] = presencePenalty,
                ["alpha_frequency"] = frequencyPenalty,
                ["alpha_decay"] = 0.99,
                ["chunk_size"] = chunkSize,
                ["stream"] = false
            };

            if (!string.IsNullOrWhiteSpace(_configuration.Password))
            {
                body["password"] = _configuration.Password;
            }

            return body;
        }

        private void ApplyAuthHeader(HttpRequestMessage request)
        {
            if (!string.IsNullOrWhiteSpace(_configuration.Password))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _configuration.Password);
            }
        }

        private Dictionary<string, string> BuildOptionalPasswordBody()
        {
            var body = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(_configuration.Password))
            {
                body["password"] = _configuration.Password;
            }

            return body;
        }

        private async Task<RwkvApiFlavor> EnsureApiFlavorAsync()
        {
            if (_apiFlavor != RwkvApiFlavor.Unknown)
            {
                return _apiFlavor;
            }

            _apiFlavor = await DetectApiFlavorAsync();
            return _apiFlavor;
        }

        private async Task<RwkvApiFlavor> DetectApiFlavorAsync()
        {
            var configuredFlavor = ResolveConfiguredApiFlavor(_configuration);
            if (configuredFlavor != RwkvApiFlavor.Unknown)
            {
                if (await ProbeFlavorAsync(configuredFlavor))
                {
                    return configuredFlavor;
                }

                var fallbackFlavor = configuredFlavor switch
                {
                    RwkvApiFlavor.LlamaCpp => RwkvApiFlavor.CudaHttp,
                    RwkvApiFlavor.CudaHttp => RwkvApiFlavor.LlamaCpp,
                    _ => configuredFlavor == RwkvApiFlavor.LegacyLibtorch
                        ? RwkvApiFlavor.CudaHttp
                        : RwkvApiFlavor.LegacyLibtorch
                };
                if (await ProbeFlavorAsync(fallbackFlavor))
                {
                    return fallbackFlavor;
                }

                return RwkvApiFlavor.Unknown;
            }

            if (await ProbeFlavorAsync(RwkvApiFlavor.CudaHttp))
            {
                return RwkvApiFlavor.CudaHttp;
            }

            if (await ProbeFlavorAsync(RwkvApiFlavor.LegacyLibtorch))
            {
                return RwkvApiFlavor.LegacyLibtorch;
            }

            return RwkvApiFlavor.Unknown;
        }

        private async Task<bool> ProbeFlavorAsync(RwkvApiFlavor flavor)
        {
            try
            {
                if (flavor == RwkvApiFlavor.LlamaCpp)
                {
                    using var llamaRequest = new HttpRequestMessage(HttpMethod.Get, $"{_configuration.BaseUrl.TrimEnd('/')}/health");
                    ApplyAuthHeader(llamaRequest);
                    using var llamaResponse = await _httpClient.SendAsync(llamaRequest);
                    // 加载模型期间 llama-server /health 返回 503，但服务确已在线
                    return llamaResponse.IsSuccessStatusCode
                        || llamaResponse.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable;
                }

                if (flavor == RwkvApiFlavor.CudaHttp)
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, $"{_configuration.BaseUrl.TrimEnd('/')}/v1/server/status");
                    ApplyAuthHeader(request);
                    using var response = await _httpClient.SendAsync(request);
                    // 实测 2026-05 纯 CUDA 包已不再暴露 /v1/server/status（返回 404）。
                    // 404 表示端口上确有 HTTP 服务应答，不能据此否定 CUDA 后端。
                    return response.IsSuccessStatusCode
                        || response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                        || response.StatusCode == System.Net.HttpStatusCode.Forbidden
                        || response.StatusCode == System.Net.HttpStatusCode.NotFound;
                }

                if (flavor == RwkvApiFlavor.LegacyLibtorch)
                {
                    var requestBody = BuildOptionalPasswordBody();
                    using var response = await _httpClient.PostAsync(
                        $"{_configuration.BaseUrl.TrimEnd('/')}/state/status",
                        new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));
                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                // Ignore and treat as unavailable.
            }

            return false;
        }

        private RwkvApiFlavor ResolveConfiguredApiFlavor(RwkvConfiguration configuration)
        {
            var runtimeFlavor = configuration.RuntimeFlavor?.Trim().ToLowerInvariant();
            return runtimeFlavor switch
            {
                "cuda" => RwkvApiFlavor.CudaHttp,
                "legacy" => RwkvApiFlavor.LegacyLibtorch,
                "llamacpp" or "llama" => RwkvApiFlavor.LlamaCpp,
                _ => InferApiFlavorFromExecutablePath(configuration.ServerScriptPath)
            };
        }

        private static RwkvApiFlavor InferApiFlavorFromExecutablePath(string? serverScriptPath)
        {
            if (string.IsNullOrWhiteSpace(serverScriptPath))
            {
                return RwkvApiFlavor.Unknown;
            }

            var normalized = serverScriptPath.Replace('/', '\\').ToLowerInvariant();
            if (normalized.Contains("rwkv_lighting_cuda"))
            {
                return RwkvApiFlavor.CudaHttp;
            }

            if (normalized.Contains("llama-server"))
            {
                return RwkvApiFlavor.LlamaCpp;
            }

            if (normalized.Contains("rwkv_lightning_libtorch") || normalized.EndsWith("rwkv_lightning.exe", StringComparison.Ordinal))
            {
                return RwkvApiFlavor.LegacyLibtorch;
            }

            return RwkvApiFlavor.Unknown;
        }

        private string BuildChatCompletionsEndpoint(RwkvApiFlavor flavor)
        {
            return flavor switch
            {
                RwkvApiFlavor.CudaHttp => $"{_configuration.BaseUrl.TrimEnd('/')}/v1/chat/completions",
                RwkvApiFlavor.LlamaCpp => $"{_configuration.BaseUrl.TrimEnd('/')}/v1/completions",
                _ => $"{_configuration.BaseUrl.TrimEnd('/')}/openai/v1/chat/completions"
            };
        }

        private string BuildBatchCompletionsEndpoint(RwkvApiFlavor flavor)
        {
            return flavor == RwkvApiFlavor.CudaHttp
                ? $"{_configuration.BaseUrl.TrimEnd('/')}/v1/batch/completions"
                : $"{_configuration.BaseUrl.TrimEnd('/')}/big_batch/completions";
        }

        private RwkvStatusResponse ParseCudaStatusResponse(string json)
        {
            try
            {
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                var model = _configuration.ModelName;
                if (root.TryGetProperty("model", out var modelElement) && modelElement.ValueKind == JsonValueKind.String)
                {
                    model = modelElement.GetString() ?? model;
                }
                else if (root.TryGetProperty("loaded_model", out var loadedModelElement) && loadedModelElement.ValueKind == JsonValueKind.String)
                {
                    model = loadedModelElement.GetString() ?? model;
                }

                return new RwkvStatusResponse
                {
                    Ready = true,
                    Model = model,
                    Strategy = _configuration.Strategy,
                    GpuInfo = json
                };
            }
            catch
            {
                return new RwkvStatusResponse
                {
                    Ready = true,
                    Model = _configuration.ModelName,
                    Strategy = _configuration.Strategy,
                    GpuInfo = json
                };
            }
        }

        private static RwkvCompletionResponse ParseCompletionResponse(string json)
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var text = ExtractCompletionText(root);
            var completionTokens = 0;

            if (root.TryGetProperty("usage", out var usage) &&
                usage.TryGetProperty("completion_tokens", out var completionTokensElement) &&
                completionTokensElement.TryGetInt32(out var tokenCount))
            {
                completionTokens = tokenCount;
            }

            return new RwkvCompletionResponse
            {
                Success = true,
                Text = text,
                TokensGenerated = completionTokens
            };
        }

        private static string ExtractOpenAiMessageText(JsonElement root)
        {
            if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
            {
                return string.Empty;
            }

            var firstChoice = choices[0];
            if (firstChoice.TryGetProperty("message", out var message) &&
                message.TryGetProperty("content", out var content))
            {
                return content.GetString() ?? string.Empty;
            }

            return string.Empty;
        }

        private static string ExtractCompletionText(JsonElement root)
        {
            if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
            {
                return string.Empty;
            }

            return ExtractBatchChoiceText(choices[0]);
        }

        private static string ExtractOpenAiDeltaText(JsonElement root)
        {
            if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
            {
                return string.Empty;
            }

            var firstChoice = choices[0];
            if (firstChoice.TryGetProperty("delta", out var delta) &&
                delta.TryGetProperty("content", out var content))
            {
                return content.GetString() ?? string.Empty;
            }

            return ExtractBatchChoiceText(firstChoice);
        }

        private static RwkvBatchCompletionResponse ParseBatchCompletionResponse(string rawContent, IReadOnlyList<string> prompts)
        {
            var builders = Enumerable.Range(0, prompts.Count)
                .ToDictionary(index => index, _ => new StringBuilder());
            var hasSsePayload = false;

            foreach (var rawLine in rawContent.Replace("\r\n", "\n").Split('\n'))
            {
                var line = rawLine.Trim();
                if (!line.StartsWith("data: ", StringComparison.Ordinal))
                {
                    continue;
                }

                hasSsePayload = true;
                var data = line[6..].Trim();
                if (string.Equals(data, "[DONE]", StringComparison.Ordinal))
                {
                    break;
                }

                try
                {
                    using var document = JsonDocument.Parse(data);
                    AppendBatchChoices(document.RootElement, builders);
                }
                catch (JsonException)
                {
                    // 忽略单行解析失败，继续读取后续 chunk。
                }
            }

            if (!hasSsePayload)
            {
                try
                {
                    using var document = JsonDocument.Parse(rawContent);
                    AppendBatchChoices(document.RootElement, builders);
                }
                catch (JsonException)
                {
                    return new RwkvBatchCompletionResponse
                    {
                        Success = false,
                        Error = "无法解析 RWKV big_batch 返回内容"
                    };
                }
            }

            var items = builders
                .Where(pair => pair.Value.Length > 0)
                .Select(pair => new RwkvBatchCompletionItem
                {
                    Index = pair.Key,
                    Text = pair.Value.ToString(),
                    TokensGenerated = pair.Value.Length
                })
                .OrderBy(item => item.Index)
                .ToList();

            return new RwkvBatchCompletionResponse
            {
                Success = items.Count > 0,
                Items = items
            };
        }

        private static void AppendBatchChoices(JsonElement root, IDictionary<int, StringBuilder> builders)
        {
            if (!root.TryGetProperty("choices", out var choices) || choices.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            foreach (var choice in choices.EnumerateArray())
            {
                if (!choice.TryGetProperty("index", out var indexElement) ||
                    !indexElement.TryGetInt32(out var index) ||
                    !builders.TryGetValue(index, out var builder))
                {
                    continue;
                }

                var text = ExtractBatchChoiceText(choice);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    builder.Append(text);
                }
            }
        }

        private static string ExtractBatchChoiceText(JsonElement choice)
        {
            if (choice.TryGetProperty("delta", out var delta) &&
                delta.TryGetProperty("content", out var deltaContent))
            {
                return deltaContent.GetString() ?? string.Empty;
            }

            if (choice.TryGetProperty("message", out var message) &&
                message.TryGetProperty("content", out var messageContent))
            {
                return StripReasoningBlocks(messageContent.GetString());
            }

            if (choice.TryGetProperty("text", out var textContent))
            {
                return StripReasoningBlocks(textContent.GetString());
            }

            return string.Empty;
        }

        /// <summary>
        /// RWKV7-G1 系列是推理模型，输出可能携带 think 推理块（形如 &lt;think&gt;...&lt;/think&gt;）；
        /// 未闭合时说明思考贯穿到结尾，从 think 开标记起整段截断。
        /// </summary>
        private static string StripReasoningBlocks(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return text ?? string.Empty;
            }

            var stripped = Regex.Replace(
                text,
                "<think[\\s\\S]*?</think\\s*>",
                string.Empty,
                RegexOptions.IgnoreCase);

            var openIndex = stripped.IndexOf("<think", StringComparison.OrdinalIgnoreCase);
            if (openIndex >= 0)
            {
                stripped = stripped[..openIndex];
            }

            return stripped.TrimStart();
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
                _requestSemaphore.Dispose();
                _disposed = true;
            }
        }
    }
}
