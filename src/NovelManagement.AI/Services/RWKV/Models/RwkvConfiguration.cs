using NovelManagement.AI.Interfaces;

namespace NovelManagement.AI.Services.RWKV.Models
{
    /// <summary>
    /// RWKV 推理服务配置
    /// </summary>
    public class RwkvConfiguration : IModelConfiguration
    {
        /// <summary>
        /// 提供者名称
        /// </summary>
        public string ProviderName => "RWKV";

        /// <summary>
        /// RWKV 本地推理服务地址
        /// </summary>
        public string BaseUrl { get; set; } = "http://localhost:8000";

        /// <summary>
        /// 模型文件路径
        /// </summary>
        public string ModelPath { get; set; } = string.Empty;

        /// <summary>
        /// 模型名称（用于显示）
        /// </summary>
        public string ModelName { get; set; } = "rwkv7-g1i";

        /// <summary>
        /// 推理策略（web 或 cuda）
        /// </summary>
        public string Strategy { get; set; } = "cuda fp16";

        /// <summary>
        /// 分词器词表路径（自动启动本地可执行推理服务时使用）
        /// </summary>
        public string VocabPath { get; set; } = string.Empty;

        /// <summary>
        /// 可选访问密码
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 单次续写最大 token 数
        /// </summary>
        public int MaxTokensPerCompletion { get; set; } = 200;

        /// <summary>
        /// 温度参数（越低越确定，越高越随机）
        /// </summary>
        public double Temperature { get; set; } = 1.0;

        /// <summary>
        /// Top-P 采样参数
        /// </summary>
        public double TopP { get; set; } = 0.7;

        /// <summary>
        /// Top-K 采样参数
        /// </summary>
        public int TopK { get; set; } = 50;

        /// <summary>
        /// 惩罚频率
        /// </summary>
        public double FrequencyPenalty { get; set; } = 0.0;

        /// <summary>
        /// 惩罚出现
        /// </summary>
        public double PresencePenalty { get; set; } = 0.0;

        /// <summary>
        /// DRY 抗复读乘数（0=禁用；创作文本推荐 0.8，越大对重复序列惩罚越强）
        /// </summary>
        public double DryMultiplier { get; set; } = 0.8;

        /// <summary>
        /// DRY 惩罚基数（推荐 1.75；越大对更长重复序列的惩罚增长越快）
        /// </summary>
        public double DryBase { get; set; } = 1.75;

        /// <summary>
        /// DRY 允许的连续重复长度（2 表示允许连续出现 2 次才开始惩罚）
        /// </summary>
        public int DryAllowedLength { get; set; } = 2;

        /// <summary>
        /// DRY 回溯窗口（最近 N 个 token 内检测重复序列；0 表示仅上一次生成）
        /// </summary>
        public int DryPenaltyLastN { get; set; } = 2048;

        /// <summary>
        /// 请求超时（秒）
        /// </summary>
        public int TimeoutSeconds { get; set; } = 120;

        /// <summary>
        /// 最大重试次数
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// 上下文窗口大小
        /// </summary>
        public int ContextSize { get; set; } = 8192;

        /// <summary>
        /// 写作档位覆盖（Auto=按 ContextSize 自动归档；可显式指定 8K/16K/32K/64K/90K/128K/256K/1M）。
        /// 32K 以下切片生成且仅注入小批量辅助信息；32K 及以上不切片并按档注入更多辅助信息。
        /// </summary>
        public string WritingContextTier { get; set; } = "Auto";

        /// <summary>
        /// 最大并发请求数
        /// </summary>
        public int MaxConcurrentRequests { get; set; } = 20;

        /// <summary>
        /// 是否自动启动推理服务
        /// </summary>
        public bool AutoStartServer { get; set; } = false;

        /// <summary>
        /// Python 解释器路径（仅旧版 Python 服务自动启动时使用）
        /// </summary>
        public string PythonPath { get; set; } = "python";

        /// <summary>
        /// 本地 RWKV 服务脚本或可执行文件路径（自动启动时使用）
        /// </summary>
        public string ServerScriptPath { get; set; } = string.Empty;

        /// <summary>
        /// 运行时风格：auto / cuda / legacy
        /// </summary>
        public string RuntimeFlavor { get; set; } = "auto";

        /// <summary>
        /// 纯 CUDA 运行时的预填充分块大小
        /// </summary>
        public int PrefillChunkSize { get; set; } = 128;

        /// <summary>
        /// 纯 CUDA OpenAI 聊天接口的 think 类型
        /// </summary>
        public string ThinkType { get; set; } = "fast";

        /// <summary>
        /// GPU 设备 ID（-1 为自动选择）
        /// </summary>
        public int GpuDeviceId { get; set; } = -1;

        /// <inheritdoc/>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(BaseUrl) && GetValidationErrors().Count == 0;
        }

        /// <inheritdoc/>
        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(BaseUrl))
                errors.Add("RWKV 服务地址不能为空");
            if (MaxTokensPerCompletion <= 0 || MaxTokensPerCompletion > 200)
                errors.Add("单次续写最大 token 数必须在 1-200 之间");
            if (Temperature <= 0 || Temperature > 3)
                errors.Add("温度参数必须在 0-3 之间");
            if (ContextSize < 1024 || ContextSize > 1048576)
                errors.Add("RWKV 上下文大小必须在 1024-1048576 之间");
            if (MaxConcurrentRequests < 1 || MaxConcurrentRequests > 20)
                errors.Add("RWKV 最大并发请求数必须在 1-20 之间");
            if (PrefillChunkSize < 1 || PrefillChunkSize > 4096)
                errors.Add("RWKV 预填充分块大小必须在 1-4096 之间");
            return errors;
        }
    }
}
