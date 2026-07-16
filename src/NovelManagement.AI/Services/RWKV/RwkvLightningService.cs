using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
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
        private SemaphoreSlim _requestSemaphore = new(20, 20);

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
                var requestBody = BuildOptionalPasswordBody();
                var response = await _httpClient.PostAsync(
                    $"{_configuration.BaseUrl.TrimEnd('/')}/state/status",
                    new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));

                _isAvailable = response.IsSuccessStatusCode;
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
                var requestBody = BuildOptionalPasswordBody();
                var response = await _httpClient.PostAsync(
                    $"{_configuration.BaseUrl.TrimEnd('/')}/state/status",
                    new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return new RwkvStatusResponse
                {
                    Ready = true,
                    Model = _configuration.ModelName,
                    Strategy = _configuration.Strategy,
                    GpuInfo = content
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
                maxTokens = Math.Clamp(maxTokens, 1, 200);

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

                var json = JsonSerializer.Serialize(BuildOpenAiChatRequest(request));
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_configuration.BaseUrl.TrimEnd('/')}/openai/v1/chat/completions")
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
                await _requestSemaphore.WaitAsync();

                maxTokens = Math.Clamp(maxTokens, 1, 200);
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

                var json = JsonSerializer.Serialize(BuildOpenAiChatRequest(request, stream: true));
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_configuration.BaseUrl.TrimEnd('/')}/openai/v1/chat/completions")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                ApplyAuthHeader(httpRequest);

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
                maxTokens = Math.Clamp(maxTokens, 1, 200);
                var fullPrompt = BuildCompletionPrompt(prompt, direction);
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

                maxTokens = Math.Clamp(maxTokens, 1, 200);
                chunkSize = Math.Clamp(chunkSize, 1, 32);
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
                    $"{_configuration.BaseUrl.TrimEnd('/')}/big_batch/completions",
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
            var arguments = new StringBuilder();
            arguments.Append($"--port {port}");

            if (!string.IsNullOrWhiteSpace(_configuration.ModelPath))
            {
                arguments.Append($" --model-path \"{_configuration.ModelPath}\"");
            }

            var vocabPath = ResolveVocabPath();
            if (!string.IsNullOrWhiteSpace(vocabPath))
            {
                arguments.Append($" --vocab-path \"{vocabPath}\"");
            }

            if (!string.IsNullOrWhiteSpace(_configuration.Password))
            {
                arguments.Append($" --password \"{_configuration.Password}\"");
            }

            return new ProcessStartInfo
            {
                FileName = _configuration.ServerScriptPath,
                Arguments = arguments.ToString(),
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

        private object BuildOpenAiChatRequest(RwkvCompletionRequest request, bool stream = false)
        {
            return new
            {
                model = string.IsNullOrWhiteSpace(_configuration.ModelName) ? "rwkv7" : _configuration.ModelName,
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
                ["temperature"] = temperature,
                ["top_k"] = topK,
                ["top_p"] = topP,
                ["alpha_presence"] = presencePenalty,
                ["alpha_frequency"] = frequencyPenalty,
                ["alpha_decay"] = 0.99,
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

        private static RwkvCompletionResponse ParseCompletionResponse(string json)
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var text = ExtractOpenAiMessageText(root);
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

            return string.Empty;
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
                return messageContent.GetString() ?? string.Empty;
            }

            if (choice.TryGetProperty("text", out var textContent))
            {
                return textContent.GetString() ?? string.Empty;
            }

            return string.Empty;
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
