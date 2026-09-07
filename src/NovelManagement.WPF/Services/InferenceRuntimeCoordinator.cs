using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NovelManagement.AI.Services.RWKV;
using NovelManagement.AI.Services.RWKV.Models;

namespace NovelManagement.WPF.Services
{
    /// <summary>
    /// 本地推理运行时主控服务：协调 RWKV 与 llama.cpp 的启停和状态检测。
    /// </summary>
    public sealed class InferenceRuntimeCoordinator : IDisposable
    {
        private readonly ILogger<InferenceRuntimeCoordinator> _logger;
        private readonly IRwkvLightningService? _rwkvService;
        private readonly HttpClient _httpClient;
        private Process? _managedRwkvProcess;
        private Process? _managedLlamaProcess;
        private readonly Queue<string> _rwkvRecentLogs = new();
        private readonly Queue<string> _llamaRecentLogs = new();
        private bool _disposed;
        private const int MaxRetainedLogLines = 20;

        /// <summary>
        /// rwkv_launcher.exe 的监管控制台端口（Go 实现，固定 8088）。
        /// 直接拉起 rwkv_lighting_cuda.exe 会因 lib 依赖缺失立即 0xC0000409 崩溃，
        /// 必须经 launcher 拉起（launcher 会注入必要环境）。
        /// </summary>
        private const int RwkvLauncherConsolePort = 8088;
        private const string RwkvLauncherFileName = "rwkv_launcher.exe";

        public InferenceRuntimeCoordinator(
            ILogger<InferenceRuntimeCoordinator> logger,
            IRwkvLightningService? rwkvService = null)
        {
            _logger = logger;
            _rwkvService = rwkvService;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(5)
            };
        }

        public async Task<IReadOnlyList<LlamaModelOption>> DiscoverLlamaModelsAsync(string modelDirectory)
        {
            if (string.IsNullOrWhiteSpace(modelDirectory) || !Directory.Exists(modelDirectory))
            {
                return Array.Empty<LlamaModelOption>();
            }

            return await Task.Run(() =>
            {
                var modelFiles = Directory.EnumerateFiles(modelDirectory, "*.gguf", SearchOption.TopDirectoryOnly)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .Select(path =>
                    {
                        var mmprojCandidate = Path.ChangeExtension(path, ".mmproj");
                        return new LlamaModelOption
                        {
                            DisplayName = Path.GetFileName(path),
                            ModelPath = path,
                            MmprojPath = File.Exists(mmprojCandidate) ? mmprojCandidate : string.Empty
                        };
                    })
                    .ToList();

                return (IReadOnlyList<LlamaModelOption>)modelFiles;
            });
        }

        public async Task<RuntimeOperationResult> StartRwkvAsync(RwkvRuntimeLaunchOptions options)
        {
            try
            {
                var validationMessage = ValidateRwkvOptions(options);
                if (!string.IsNullOrWhiteSpace(validationMessage))
                {
                    return RuntimeOperationResult.Fail(validationMessage);
                }

                // 后端已在线时跳过重复启动
                if (await ProbeRwkvAsync(options.BaseUrl, options.Password))
                {
                    _logger.LogInformation("RWKV 推理服务已在线，跳过启动");
                    return RuntimeOperationResult.Ok("RWKV 服务已在线。");
                }

                await StopRwkvAsync(options);

                var launcherPath = ResolveRwkvLauncherPath(options.ExecutablePath);
                Process? startedProcess;
                string startMode;

                if (!string.IsNullOrWhiteSpace(launcherPath))
                {
                    startMode = "launcher";
                    startedProcess = await StartRwkvViaLauncherAsync(options, launcherPath);
                }
                else
                {
                    // 兜底：launcher 不存在时退回直连方式（已知会崩溃，仅保留兼容路径）
                    _logger.LogWarning("未找到 rwkv_launcher.exe，回退为直接拉起 {ExecutablePath}（该方式在此 CUDA 构建上会崩溃）", options.ExecutablePath);
                    startMode = "direct";
                    var startInfo = BuildRwkvStartInfo(options);
                    startedProcess = Process.Start(startInfo);
                }

                _managedRwkvProcess = startedProcess;
                if (startedProcess == null && startMode == "direct")
                {
                    return RuntimeOperationResult.Fail("RWKV 推理服务进程启动失败。");
                }

                if (startedProcess != null)
                {
                    AttachProcessLogging(startedProcess, startMode == "launcher" ? "RWKV-launcher" : "RWKV", _rwkvRecentLogs);
                }

                var ready = await WaitUntilAsync(
                    () => ProbeRwkvAsync(options.BaseUrl, options.Password),
                    () => _managedRwkvProcess?.HasExited == true && startMode != "launcher"
                        ? BuildProcessExitMessage("RWKV", _managedRwkvProcess.ExitCode, _rwkvRecentLogs)
                        : null,
                    options.StartupTimeoutSeconds);

                if (!ready.Success)
                {
                    return ready;
                }

                if (_rwkvService != null)
                {
                    await _rwkvService.InitializeAsync(new RwkvConfiguration
                    {
                        BaseUrl = NormalizeBaseUrl(options.BaseUrl),
                        ModelPath = options.ModelPath,
                        ModelName = options.ModelName,
                        Strategy = options.Strategy,
                        VocabPath = options.VocabPath,
                        Password = options.Password,
                        TimeoutSeconds = 120,
                        MaxRetries = 3,
                        AutoStartServer = false,
                        ServerScriptPath = options.ExecutablePath,
                        RuntimeFlavor = options.RuntimeFlavor,
                        PrefillChunkSize = options.PrefillChunkSize
                    });
                }

                return RuntimeOperationResult.Ok(
                    $"RWKV 已启动：{Path.GetFileName(options.ModelPath)}（{startMode}）",
                    _managedRwkvProcess?.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启动 RWKV 推理服务失败");
                return RuntimeOperationResult.Fail($"启动 RWKV 失败：{ex.Message}");
            }
        }

        public async Task<RuntimeOperationResult> StopRwkvAsync(RwkvRuntimeLaunchOptions options)
        {
            try
            {
                var launcherPath = ResolveRwkvLauncherPath(options.ExecutablePath);
                var killed = false;

                // 优先通过 launcher 的 /api/stop 优雅停止后端
                if (!string.IsNullOrWhiteSpace(launcherPath) && await IsLauncherConsoleAliveAsync())
                {
                    killed |= await TryLauncherStopAsync();
                }

                killed |= KillTrackedProcess(ref _managedRwkvProcess);
                killed |= KillProcessesByPath(options.ExecutablePath);
                if (!string.IsNullOrWhiteSpace(launcherPath))
                {
                    killed |= KillProcessesByPath(launcherPath);
                }

                if (_rwkvService != null)
                {
                    await _rwkvService.UnloadModelAsync();
                }

                var stillOnline = await ProbeRwkvAsync(options.BaseUrl, options.Password);
                if (stillOnline)
                {
                    return RuntimeOperationResult.Fail("RWKV 服务仍在线，可能由其他未匹配到的外部进程占用。");
                }

                return RuntimeOperationResult.Ok(killed ? "RWKV 已停止。" : "RWKV 当前未运行。");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "停止 RWKV 推理服务失败");
                return RuntimeOperationResult.Fail($"停止 RWKV 失败：{ex.Message}");
            }
        }

        public async Task<RuntimeStatusSnapshot> GetRwkvStatusAsync(RwkvRuntimeLaunchOptions options)
        {
            var online = await ProbeRwkvAsync(options.BaseUrl, options.Password);
            var localProcessRunning = FindProcessesByPath(options.ExecutablePath).Any();

            return new RuntimeStatusSnapshot
            {
                IsProcessRunning = localProcessRunning,
                IsEndpointReachable = online,
                StatusText = online
                    ? $"在线 ({Path.GetFileName(options.ModelPath)})"
                    : localProcessRunning
                        ? "进程运行中，服务未就绪"
                        : "离线"
            };
        }

        public async Task<RuntimeOperationResult> StartLlamaAsync(LlamaRuntimeLaunchOptions options)
        {
            try
            {
                var validationMessage = ValidateLlamaOptions(options);
                if (!string.IsNullOrWhiteSpace(validationMessage))
                {
                    return RuntimeOperationResult.Fail(validationMessage);
                }

                await StopLlamaAsync(options);

                var startInfo = BuildLlamaStartInfo(options);
                _managedLlamaProcess = Process.Start(startInfo);
                if (_managedLlamaProcess == null)
                {
                    return RuntimeOperationResult.Fail("llama.cpp 推理服务进程启动失败。");
                }

                AttachProcessLogging(_managedLlamaProcess, "llama.cpp", _llamaRecentLogs);

                var ready = await WaitUntilAsync(
                    () => ProbeLlamaAsync(options.BaseUrl, options.ApiKey),
                    () => _managedLlamaProcess?.HasExited == true
                        ? BuildProcessExitMessage("llama.cpp", _managedLlamaProcess.ExitCode, _llamaRecentLogs)
                        : null,
                    options.StartupTimeoutSeconds);

                return ready.Success
                    ? RuntimeOperationResult.Ok($"llama.cpp 已启动：{Path.GetFileName(options.ModelPath)}", _managedLlamaProcess.Id)
                    : ready;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启动 llama.cpp 推理服务失败");
                return RuntimeOperationResult.Fail($"启动 llama.cpp 失败：{ex.Message}");
            }
        }

        public Task<RuntimeOperationResult> StopLlamaAsync(LlamaRuntimeLaunchOptions options)
        {
            try
            {
                var killed = KillTrackedProcess(ref _managedLlamaProcess);
                killed |= KillProcessesByPath(options.ExecutablePath);

                return Task.FromResult(RuntimeOperationResult.Ok(killed ? "llama.cpp 已停止。" : "llama.cpp 当前未运行。"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "停止 llama.cpp 推理服务失败");
                return Task.FromResult(RuntimeOperationResult.Fail($"停止 llama.cpp 失败：{ex.Message}"));
            }
        }

        public async Task<RuntimeStatusSnapshot> GetLlamaStatusAsync(LlamaRuntimeLaunchOptions options)
        {
            var online = await ProbeLlamaAsync(options.BaseUrl, options.ApiKey);
            var localProcessRunning = FindProcessesByPath(options.ExecutablePath).Any();

            return new RuntimeStatusSnapshot
            {
                IsProcessRunning = localProcessRunning,
                IsEndpointReachable = online,
                StatusText = online
                    ? $"在线 ({Path.GetFileName(options.ModelPath)})"
                    : localProcessRunning
                        ? "进程运行中，服务未就绪"
                        : "离线"
            };
        }

        private string ValidateRwkvOptions(RwkvRuntimeLaunchOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.ExecutablePath) || !File.Exists(options.ExecutablePath))
            {
                return "RWKV 可执行文件路径无效。";
            }

            if (string.IsNullOrWhiteSpace(options.ModelPath) || !File.Exists(options.ModelPath))
            {
                return "RWKV 模型路径无效。";
            }

            return string.Empty;
        }

        private string ValidateLlamaOptions(LlamaRuntimeLaunchOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.ExecutablePath) || !File.Exists(options.ExecutablePath))
            {
                return "llama-server.exe 路径无效。";
            }

            if (string.IsNullOrWhiteSpace(options.ModelPath) || !File.Exists(options.ModelPath))
            {
                return "GGUF 模型路径无效。";
            }

            if (!string.IsNullOrWhiteSpace(options.MmprojPath) && !File.Exists(options.MmprojPath))
            {
                return "mmproj 路径无效。";
            }

            return string.Empty;
        }

        private ProcessStartInfo BuildRwkvStartInfo(RwkvRuntimeLaunchOptions options)
        {
            var uri = new Uri(NormalizeBaseUrl(options.BaseUrl));
            var arguments = new StringBuilder();
            arguments.Append($"--model-path \"{options.ModelPath}\"");
            arguments.Append($" --port {uri.Port}");

            if (ResolveRwkvRuntimeFlavor(options) == "cuda")
            {
                arguments.Append($" --host {uri.Host}");
                arguments.Append($" --chunk-size {Math.Clamp(options.PrefillChunkSize, 1, 4096)}");
            }

            var vocabPath = !string.IsNullOrWhiteSpace(options.VocabPath) && File.Exists(options.VocabPath)
                ? options.VocabPath
                : ResolveRwkvVocabPath(options.ExecutablePath);
            if (!string.IsNullOrWhiteSpace(vocabPath))
            {
                arguments.Append($" --vocab-path \"{vocabPath}\"");
            }

            if (!string.IsNullOrWhiteSpace(options.Password))
            {
                arguments.Append($" --password \"{options.Password}\"");
            }

            return new ProcessStartInfo
            {
                FileName = options.ExecutablePath,
                Arguments = arguments.ToString(),
                WorkingDirectory = Path.GetDirectoryName(options.ExecutablePath) ?? AppDomain.CurrentDomain.BaseDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
        }

        /// <summary>
        /// 在后端可执行文件同目录下查找 rwkv_launcher.exe。
        /// </summary>
        private static string? ResolveRwkvLauncherPath(string executablePath)
        {
            var directory = Path.GetDirectoryName(executablePath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                return null;
            }

            var launcherPath = Path.Combine(directory, RwkvLauncherFileName);
            return File.Exists(launcherPath) ? launcherPath : null;
        }

        /// <summary>
        /// 经 rwkv_launcher.exe 拉起 RWKV 后端：
        /// 1. 复用或启动 launcher 监管进程；2. 等待其 8088 控制台就绪；
        /// 3. POST /api/start（port 为字符串、路径用正斜杠）请求加载模型。
        /// </summary>
        /// <returns>被跟踪的 launcher 进程（复用已运行的 launcher 时返回 null）。</returns>
        private async Task<Process?> StartRwkvViaLauncherAsync(RwkvRuntimeLaunchOptions options, string launcherPath)
        {
            Process? launcherProcess = null;

            if (!await IsLauncherConsoleAliveAsync())
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = launcherPath,
                    WorkingDirectory = Path.GetDirectoryName(launcherPath) ?? AppDomain.CurrentDomain.BaseDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                launcherProcess = Process.Start(startInfo);
                if (launcherProcess == null)
                {
                    throw new InvalidOperationException("rwkv_launcher.exe 进程启动失败。");
                }

                // 等待 launcher 控制台就绪（最多 15 秒）
                var consoleReady = false;
                for (int i = 0; i < 15; i++)
                {
                    if (launcherProcess.HasExited)
                    {
                        throw new InvalidOperationException($"rwkv_launcher.exe 提前退出，退出码 {launcherProcess.ExitCode}。");
                    }

                    if (await IsLauncherConsoleAliveAsync())
                    {
                        consoleReady = true;
                        break;
                    }

                    await Task.Delay(1000);
                }

                if (!consoleReady)
                {
                    throw new InvalidOperationException("rwkv_launcher.exe 控制台(8088)等待超时。");
                }

                _logger.LogInformation("RWKV launcher 监管进程已启动，PID: {Pid}", launcherProcess.Id);
            }
            else
            {
                _logger.LogInformation("复用已运行的 RWKV launcher 控制台(8088)");
            }

            var uri = new Uri(NormalizeBaseUrl(options.BaseUrl));
            var vocabPath = !string.IsNullOrWhiteSpace(options.VocabPath) && File.Exists(options.VocabPath)
                ? options.VocabPath
                : ResolveRwkvVocabPath(options.ExecutablePath);

            var payload = new Dictionary<string, string>
            {
                ["model_path"] = options.ModelPath.Replace('\\', '/'),
                ["vocab_path"] = (string.IsNullOrWhiteSpace(vocabPath) ? string.Empty : vocabPath).Replace('\\', '/'),
                ["port"] = uri.Port.ToString(),
                ["password"] = options.Password ?? string.Empty
            };

            var json = JsonSerializer.Serialize(payload);
            using var response = await _httpClient.PostAsync(
                $"http://127.0.0.1:{RwkvLauncherConsolePort}/api/start",
                new StringContent(json, Encoding.UTF8, "application/json"));
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"launcher /api/start 失败（{(int)response.StatusCode}）：{content}");
            }

            _logger.LogInformation("launcher /api/start 已受理: {Content}", content);
            return launcherProcess;
        }

        /// <summary>
        /// 探测 launcher 监管控制台（GET /api/status）是否在线。
        /// </summary>
        private async Task<bool> IsLauncherConsoleAliveAsync()
        {
            try
            {
                using var response = await _httpClient.GetAsync($"http://127.0.0.1:{RwkvLauncherConsolePort}/api/status");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 请求 launcher 停止后端（POST /api/stop）。
        /// </summary>
        private async Task<bool> TryLauncherStopAsync()
        {
            try
            {
                using var response = await _httpClient.PostAsync(
                    $"http://127.0.0.1:{RwkvLauncherConsolePort}/api/stop",
                    new StringContent("{}", Encoding.UTF8, "application/json"));
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "launcher /api/stop 请求失败");
                return false;
            }
        }

        private ProcessStartInfo BuildLlamaStartInfo(LlamaRuntimeLaunchOptions options)
        {
            var uri = new Uri(NormalizeBaseUrl(options.BaseUrl));
            var arguments = new StringBuilder();
            arguments.Append($"-m \"{options.ModelPath}\"");
            arguments.Append($" --host {uri.Host}");
            arguments.Append($" --port {uri.Port}");

            if (options.ContextSize > 0)
            {
                arguments.Append($" -c {options.ContextSize}");
            }

            if (options.ParallelSlots > 0)
            {
                arguments.Append($" -np {options.ParallelSlots}");
            }

            if (options.GpuLayers >= 0)
            {
                arguments.Append($" -ngl {options.GpuLayers}");
            }

            if (options.EnableFlashAttention)
            {
                arguments.Append(" -fa on");
            }

            if (!string.IsNullOrWhiteSpace(options.CacheTypeK))
            {
                arguments.Append($" -ctk {options.CacheTypeK}");
            }

            if (!string.IsNullOrWhiteSpace(options.CacheTypeV))
            {
                arguments.Append($" -ctv {options.CacheTypeV}");
            }

            if (options.EnableCpuMoE)
            {
                arguments.Append(" --cpu-moe");
            }
            else if (options.CpuMoELayerCount > 0)
            {
                arguments.Append($" --n-cpu-moe {options.CpuMoELayerCount}");
            }

            if (options.EnableAutoFit)
            {
                arguments.Append(" --fit on");
                if (options.FitContextSize > 0)
                {
                    arguments.Append($" -fitc {options.FitContextSize}");
                }
            }

            if (!string.IsNullOrWhiteSpace(options.MmprojPath))
            {
                arguments.Append($" --mmproj \"{options.MmprojPath}\"");
            }

            if (options.DisableWarmup)
            {
                arguments.Append(" --no-warmup");
            }

            if (!string.IsNullOrWhiteSpace(options.ApiKey))
            {
                arguments.Append($" --api-key \"{options.ApiKey}\"");
            }

            return new ProcessStartInfo
            {
                FileName = options.ExecutablePath,
                Arguments = arguments.ToString(),
                WorkingDirectory = Path.GetDirectoryName(options.ExecutablePath) ?? AppDomain.CurrentDomain.BaseDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
        }

        private void AttachProcessLogging(Process process, string runtimeName, Queue<string> logBuffer)
        {
            logBuffer.Clear();
            process.OutputDataReceived += (_, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                {
                    AppendLog(logBuffer, args.Data);
                    _logger.LogInformation("[{Runtime}] {Message}", runtimeName, args.Data);
                }
            };

            process.ErrorDataReceived += (_, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                {
                    AppendLog(logBuffer, args.Data);
                    _logger.LogWarning("[{Runtime}] {Message}", runtimeName, args.Data);
                }
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        private static void AppendLog(Queue<string> logBuffer, string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            if (logBuffer.Count >= MaxRetainedLogLines)
            {
                logBuffer.Dequeue();
            }

            logBuffer.Enqueue(line.Trim());
        }

        private static string BuildProcessExitMessage(string runtimeName, int exitCode, Queue<string> logBuffer)
        {
            var baseMessage = $"{runtimeName} 进程已退出，退出码 {exitCode}";
            if (logBuffer.Count == 0)
            {
                return baseMessage;
            }

            var lastInterestingLog = logBuffer
                .Reverse()
                .FirstOrDefault(line =>
                    line.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("failed", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("missing tensor", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("exception", StringComparison.OrdinalIgnoreCase))
                ?? logBuffer.Last();

            return $"{baseMessage}。最近日志：{lastInterestingLog}";
        }

        private async Task<RuntimeOperationResult> WaitUntilAsync(
            Func<Task<bool>> probeAsync,
            Func<string?> processExitedMessage,
            int timeoutSeconds)
        {
            var timeout = Math.Max(5, timeoutSeconds);
            for (int i = 0; i < timeout; i++)
            {
                if (await probeAsync())
                {
                    return RuntimeOperationResult.Ok("服务已就绪。");
                }

                var exitMessage = processExitedMessage();
                if (!string.IsNullOrWhiteSpace(exitMessage))
                {
                    return RuntimeOperationResult.Fail(exitMessage);
                }

                await Task.Delay(1000);
            }

            return RuntimeOperationResult.Fail("等待服务启动超时。");
        }

        private async Task<bool> ProbeRwkvAsync(string baseUrl, string password)
        {
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, $"{NormalizeBaseUrl(baseUrl).TrimEnd('/')}/v1/server/status"))
                {
                    if (!string.IsNullOrWhiteSpace(password))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", password);
                    }

                    using var response = await _httpClient.SendAsync(request);
                    if (response.IsSuccessStatusCode
                        || response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                        || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        return true;
                    }
                }

                using var legacyResponse = await _httpClient.PostAsync(
                    $"{NormalizeBaseUrl(baseUrl).TrimEnd('/')}/state/status",
                    new StringContent(JsonSerializer.Serialize(BuildOptionalPasswordBody(password)), Encoding.UTF8, "application/json"));
                return legacyResponse.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private static Dictionary<string, string> BuildOptionalPasswordBody(string password)
        {
            var body = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(password))
            {
                body["password"] = password;
            }

            return body;
        }

        private static string ResolveRwkvRuntimeFlavor(RwkvRuntimeLaunchOptions options)
        {
            if (!string.IsNullOrWhiteSpace(options.RuntimeFlavor) &&
                !string.Equals(options.RuntimeFlavor, "auto", StringComparison.OrdinalIgnoreCase))
            {
                return options.RuntimeFlavor.Trim().ToLowerInvariant();
            }

            var executablePath = options.ExecutablePath?.Replace('/', '\\').ToLowerInvariant() ?? string.Empty;
            return executablePath.Contains("rwkv_lighting_cuda") ? "cuda" : "legacy";
        }

        private async Task<bool> ProbeLlamaAsync(string baseUrl, string apiKey)
        {
            try
            {
                var endpoints = new[]
                {
                    $"{NormalizeBaseUrl(baseUrl).TrimEnd('/')}/health",
                    $"{NormalizeBaseUrl(baseUrl).TrimEnd('/')}/v1/models",
                    NormalizeBaseUrl(baseUrl).TrimEnd('/')
                };

                foreach (var endpoint in endpoints)
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
                    if (!string.IsNullOrWhiteSpace(apiKey))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                    }

                    using var response = await _httpClient.SendAsync(request);
                    if ((int)response.StatusCode < 500)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private static string NormalizeBaseUrl(string baseUrl)
        {
            var normalized = string.IsNullOrWhiteSpace(baseUrl)
                ? "http://localhost:8000"
                : baseUrl.Trim();
            return normalized.EndsWith("/", StringComparison.Ordinal) ? normalized : normalized + "/";
        }

        private static string ResolveRwkvVocabPath(string executablePath)
        {
            var directory = Path.GetDirectoryName(executablePath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                return string.Empty;
            }

            var candidate = Path.Combine(directory, "rwkv_vocab_v20230424.txt");
            return File.Exists(candidate) ? candidate : string.Empty;
        }

        private bool KillTrackedProcess(ref Process? process)
        {
            if (process == null)
            {
                return false;
            }

            var killed = TryKillProcess(process);
            process = null;
            return killed;
        }

        private bool KillProcessesByPath(string executablePath)
        {
            var killed = false;
            foreach (var process in FindProcessesByPath(executablePath).ToList())
            {
                killed |= TryKillProcess(process);
            }

            return killed;
        }

        private IEnumerable<Process> FindProcessesByPath(string executablePath)
        {
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                return Enumerable.Empty<Process>();
            }

            var processName = Path.GetFileNameWithoutExtension(executablePath);
            return Process.GetProcessesByName(processName)
                .Where(process =>
                {
                    try
                    {
                        return string.Equals(process.MainModule?.FileName, executablePath, StringComparison.OrdinalIgnoreCase);
                    }
                    catch
                    {
                        return false;
                    }
                });
        }

        private bool TryKillProcess(Process process)
        {
            try
            {
                if (process.HasExited)
                {
                    process.Dispose();
                    return false;
                }

                process.Kill(entireProcessTree: true);
                process.WaitForExit(5000);
                process.Dispose();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "终止进程失败: {ProcessName}", process.ProcessName);
                return false;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            KillTrackedProcess(ref _managedRwkvProcess);
            KillTrackedProcess(ref _managedLlamaProcess);
            _httpClient.Dispose();
            _disposed = true;
        }
    }

    public sealed class RwkvRuntimeLaunchOptions
    {
        public string ExecutablePath { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "http://localhost:8000";
        public string ModelPath { get; set; } = string.Empty;
        public string ModelName { get; set; } = "rwkv7-g1i";
        public string Strategy { get; set; } = "cuda fp16";
        public string VocabPath { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int ContextSize { get; set; } = 8192;
        public int MaxConcurrentRequests { get; set; } = 20;
        public string RuntimeFlavor { get; set; } = "auto";
        public int PrefillChunkSize { get; set; } = 128;
        public int StartupTimeoutSeconds { get; set; } = 45;
    }

    public sealed class LlamaRuntimeLaunchOptions
    {
        public string ExecutablePath { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "http://localhost:8081";
        public string ModelPath { get; set; } = string.Empty;
        public string MmprojPath { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public int ContextSize { get; set; } = 262144;
        public int GpuLayers { get; set; } = -1;
        public int ParallelSlots { get; set; } = 1;
        public bool EnableFlashAttention { get; set; } = true;
        public string CacheTypeK { get; set; } = "q8_0";
        public string CacheTypeV { get; set; } = "q8_0";
        public bool EnableCpuMoE { get; set; }
        public int CpuMoELayerCount { get; set; }
        public bool EnableAutoFit { get; set; } = true;
        public int FitContextSize { get; set; } = 262144;
        public bool DisableWarmup { get; set; } = true;
        public int StartupTimeoutSeconds { get; set; } = 45;
    }

    public sealed class RuntimeOperationResult
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public int? ProcessId { get; init; }

        public static RuntimeOperationResult Ok(string message, int? processId = null)
        {
            return new RuntimeOperationResult
            {
                Success = true,
                Message = message,
                ProcessId = processId
            };
        }

        public static RuntimeOperationResult Fail(string message)
        {
            return new RuntimeOperationResult
            {
                Success = false,
                Message = message
            };
        }
    }

    public sealed class RuntimeStatusSnapshot
    {
        public bool IsProcessRunning { get; set; }
        public bool IsEndpointReachable { get; set; }
        public string StatusText { get; set; } = "离线";
    }

    public sealed class LlamaModelOption
    {
        public string DisplayName { get; set; } = string.Empty;
        public string ModelPath { get; set; } = string.Empty;
        public string MmprojPath { get; set; } = string.Empty;
    }
}
