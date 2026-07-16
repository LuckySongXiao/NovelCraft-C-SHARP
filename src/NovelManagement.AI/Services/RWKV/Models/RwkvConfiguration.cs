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
        /// rwkv_lightning 推理服务地址
        /// </summary>
        public string BaseUrl { get; set; } = "http://localhost:8000";

        /// <summary>
        /// 模型文件路径
        /// </summary>
        public string ModelPath { get; set; } = string.Empty;

        /// <summary>
        /// 模型名称（用于显示）
        /// </summary>
        public string ModelName { get; set; } = "rwkv7";

        /// <summary>
        /// 推理策略（web 或 cuda）
        /// </summary>
        public string Strategy { get; set; } = "cuda fp16";

        /// <summary>
        /// 分词器词表路径（自动启动 rwkv_lightning.exe 时使用）
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
        /// 最大并发请求数
        /// </summary>
        public int MaxConcurrentRequests { get; set; } = 20;

        /// <summary>
        /// 是否自动启动推理服务
        /// </summary>
        public bool AutoStartServer { get; set; } = false;

        /// <summary>
        /// Python 解释器路径（自动启动时使用）
        /// </summary>
        public string PythonPath { get; set; } = "python";

        /// <summary>
        /// rwkv_lightning_libtorch 脚本路径（自动启动时使用）
        /// </summary>
        public string ServerScriptPath { get; set; } = string.Empty;

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
            if (ContextSize < 1024 || ContextSize > 32768)
                errors.Add("RWKV 上下文大小必须在 1024-32768 之间");
            if (MaxConcurrentRequests < 1 || MaxConcurrentRequests > 20)
                errors.Add("RWKV 最大并发请求数必须在 1-20 之间");
            return errors;
        }
    }
}
