using NovelManagement.AI.Services.RWKV;
using NovelManagement.AI.Services.RWKV.Models;

namespace NovelManagement.Tests.Fakes;

/// <summary>
/// RWKV 推理服务 Fake：按预设脚本返回输出，供 EnsureCultivationSystemAsync 链路测试
/// </summary>
internal sealed class FakeRwkvLightningService : IRwkvLightningService
{
    /// <summary>是否可用（控制回退路径）</summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>全部调用返回失败（验证「失败绝不降级假数据」铁律）</summary>
    public bool FailAll { get; set; }

    /// <summary>
    /// 意图兜底调用的固定回复（prompt 含「判断用户指令的意图」时优先返回，不消耗脚本队列；
    /// null 时按普通调用走脚本）。
    /// </summary>
    public string? IntentFallbackResponse { get; set; }

    /// <summary>CompleteAsync 返回的文本脚本（按调用顺序弹出，留空则一直返回最后一个）</summary>
    public Queue<string> Responses { get; } = new();

    /// <summary>CompleteAsync 收到的 prompt 列表</summary>
    public List<string> ReceivedPrompts { get; } = new();

    public RwkvConfiguration Configuration { get; } = new();

    public Task<bool> InitializeAsync(RwkvConfiguration configuration) => Task.FromResult(true);

    public Task<bool> TestConnectionAsync() => Task.FromResult(IsAvailable);

    public Task<RwkvStatusResponse> GetStatusAsync() => Task.FromResult(new RwkvStatusResponse());

    public Task<RwkvCompletionResponse> CompleteAsync(string prompt, int maxTokens = 200, string? direction = null,
        double? temperature = null, double? topP = null, double? presencePenalty = null,
        double? frequencyPenalty = null, int? topK = null)
    {
        if (FailAll)
        {
            return Task.FromResult(new RwkvCompletionResponse { Success = false, Error = "模拟推理失败" });
        }

        ReceivedPrompts.Add(prompt);

        // 意图兜底调用按标记路由，与流水线脚本队列解耦
        if (IntentFallbackResponse != null && prompt.Contains("判断用户指令的意图", StringComparison.Ordinal))
        {
            return Task.FromResult(new RwkvCompletionResponse { Success = true, Text = IntentFallbackResponse, TokensGenerated = 10 });
        }

        var text = Responses.Count > 1 ? Responses.Dequeue() : Responses.Peek();
        return Task.FromResult(new RwkvCompletionResponse { Success = true, Text = text, TokensGenerated = 100 });
    }

    public Task<RwkvCompletionResponse> CompleteStreamAsync(string prompt, int maxTokens = 200, Action<string>? onTokenReceived = null,
        string? direction = null, double? temperature = null, double? topP = null,
        double? presencePenalty = null, double? frequencyPenalty = null, int? topK = null)
        => CompleteAsync(prompt, maxTokens, direction, temperature, topP, presencePenalty, frequencyPenalty, topK);

    public Task<RwkvCompletionResponse> CompleteWithStateAsync(string sessionId, string prompt, int maxTokens = 200,
        string? direction = null, double? temperature = null, double? topP = null,
        double? presencePenalty = null, double? frequencyPenalty = null, int? topK = null)
        => CompleteAsync(prompt, maxTokens, direction, temperature, topP, presencePenalty, frequencyPenalty, topK);

    public Task<RwkvBatchCompletionResponse> CompleteBatchAsync(IReadOnlyList<string> prompts, int maxTokens = 200,
        double? temperature = null, double? topP = null, double? presencePenalty = null,
        double? frequencyPenalty = null, int? topK = null, int chunkSize = 8)
        => throw new NotSupportedException();

    public Task<bool> DeleteStateAsync(string sessionId) => Task.FromResult(true);

    public Task<bool> LoadModelAsync(string modelPath, string strategy) => Task.FromResult(true);

    public Task UnloadModelAsync() => Task.CompletedTask;
}
