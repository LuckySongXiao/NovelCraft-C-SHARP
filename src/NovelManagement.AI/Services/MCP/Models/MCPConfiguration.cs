using System.ComponentModel.DataAnnotations;
using NovelManagement.AI.Interfaces;

namespace NovelManagement.AI.Services.MCP.Models
{
    /// <summary>
    /// MCP配置
    /// </summary>
    public class MCPConfiguration : IModelConfiguration
    {
        /// <summary>
        /// 提供者名称
        /// </summary>
        public string ProviderName => "MCP";

        /// <summary>
        /// 服务器地址
        /// </summary>
        [Required]
        public string ServerUrl { get; set; } = string.Empty;

        /// <summary>
        /// 连接类型
        /// </summary>
        public MCPConnectionType ConnectionType { get; set; } = MCPConnectionType.WebSocket;

        /// <summary>
        /// 认证类型
        /// </summary>
        public MCPAuthenticationType AuthenticationType { get; set; } = MCPAuthenticationType.None;

        /// <summary>
        /// API密钥
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// 用户名
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// 密码
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 客户端证书路径
        /// </summary>
        public string ClientCertificatePath { get; set; } = string.Empty;

        /// <summary>
        /// 客户端证书密码
        /// </summary>
        public string ClientCertificatePassword { get; set; } = string.Empty;

        /// <summary>
        /// 连接超时时间（秒）
        /// </summary>
        [Range(1, 300)]
        public int ConnectionTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// 请求超时时间（秒）
        /// </summary>
        [Range(1, 600)]
        public int RequestTimeoutSeconds { get; set; } = 120;

        /// <summary>
        /// 心跳间隔（秒）
        /// </summary>
        [Range(10, 300)]
        public int HeartbeatIntervalSeconds { get; set; } = 60;

        /// <summary>
        /// 最大重连次数
        /// </summary>
        [Range(0, 10)]
        public int MaxReconnectAttempts { get; set; } = 3;

        /// <summary>
        /// 重连间隔（秒）
        /// </summary>
        [Range(1, 60)]
        public int ReconnectIntervalSeconds { get; set; } = 5;

        /// <summary>
        /// 是否启用自动重连
        /// </summary>
        public bool EnableAutoReconnect { get; set; } = true;

        /// <summary>
        /// 是否启用压缩
        /// </summary>
        public bool EnableCompression { get; set; } = true;

        /// <summary>
        /// 缓冲区大小（字节）
        /// </summary>
        [Range(1024, 1048576)]
        public int BufferSize { get; set; } = 8192;

        /// <summary>
        /// 最大消息大小（字节）
        /// </summary>
        [Range(1024, 104857600)]
        public int MaxMessageSize { get; set; } = 1048576;

        /// <summary>
        /// 是否启用详细日志
        /// </summary>
        public bool EnableVerboseLogging { get; set; } = false;

        /// <summary>
        /// 自定义头部
        /// </summary>
        public Dictionary<string, string> CustomHeaders { get; set; } = new();

        /// <summary>
        /// 自定义参数
        /// </summary>
        public Dictionary<string, object> CustomParameters { get; set; } = new();

        /// <summary>
        /// 支持的协议版本列表
        /// </summary>
        public List<string> SupportedProtocolVersions { get; set; } = new() { MCPVersion.Current };

        /// <summary>
        /// 客户端能力配置
        /// </summary>
        public MCPClientCapabilitiesConfig ClientCapabilities { get; set; } = new();

        /// <summary>
        /// 验证配置
        /// </summary>
        /// <returns>验证结果</returns>
        public bool IsValid()
        {
            var errors = GetValidationErrors();
            return errors.Count == 0;
        }

        /// <summary>
        /// 获取验证错误
        /// </summary>
        /// <returns>错误列表</returns>
        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();

            // 验证服务器地址
            if (string.IsNullOrWhiteSpace(ServerUrl))
            {
                errors.Add("ServerUrl不能为空");
            }
            else if (!Uri.TryCreate(ServerUrl, UriKind.Absolute, out var uri))
            {
                errors.Add("ServerUrl格式无效");
            }
            else
            {
                // 验证协议
                var validSchemes = ConnectionType switch
                {
                    MCPConnectionType.WebSocket => new[] { "ws", "wss" },
                    MCPConnectionType.HTTP => new[] { "http", "https" },
                    MCPConnectionType.TCP => new[] { "tcp" },
                    _ => Array.Empty<string>()
                };

                if (!validSchemes.Contains(uri.Scheme.ToLower()))
                {
                    errors.Add($"ServerUrl协议不匹配连接类型 {ConnectionType}");
                }
            }

            // 验证认证配置
            switch (AuthenticationType)
            {
                case MCPAuthenticationType.ApiKey:
                    if (string.IsNullOrWhiteSpace(ApiKey))
                        errors.Add("使用ApiKey认证时，ApiKey不能为空");
                    break;

                case MCPAuthenticationType.Basic:
                    if (string.IsNullOrWhiteSpace(Username))
                        errors.Add("使用Basic认证时，Username不能为空");
                    if (string.IsNullOrWhiteSpace(Password))
                        errors.Add("使用Basic认证时，Password不能为空");
                    break;

                case MCPAuthenticationType.Certificate:
                    if (string.IsNullOrWhiteSpace(ClientCertificatePath))
                        errors.Add("使用Certificate认证时，ClientCertificatePath不能为空");
                    else if (!File.Exists(ClientCertificatePath))
                        errors.Add("客户端证书文件不存在");
                    break;
            }

            // 验证超时配置
            if (ConnectionTimeoutSeconds < 1 || ConnectionTimeoutSeconds > 300)
                errors.Add("ConnectionTimeoutSeconds必须在1-300秒之间");

            if (RequestTimeoutSeconds < 1 || RequestTimeoutSeconds > 600)
                errors.Add("RequestTimeoutSeconds必须在1-600秒之间");

            if (HeartbeatIntervalSeconds < 10 || HeartbeatIntervalSeconds > 300)
                errors.Add("HeartbeatIntervalSeconds必须在10-300秒之间");

            if (MaxReconnectAttempts < 0 || MaxReconnectAttempts > 10)
                errors.Add("MaxReconnectAttempts必须在0-10之间");

            if (ReconnectIntervalSeconds < 1 || ReconnectIntervalSeconds > 60)
                errors.Add("ReconnectIntervalSeconds必须在1-60秒之间");

            if (BufferSize < 1024 || BufferSize > 1048576)
                errors.Add("BufferSize必须在1024-1048576字节之间");

            if (MaxMessageSize < 1024 || MaxMessageSize > 104857600)
                errors.Add("MaxMessageSize必须在1024-104857600字节之间");

            // 验证协议版本
            if (SupportedProtocolVersions.Count == 0)
                errors.Add("SupportedProtocolVersions不能为空");

            return errors;
        }

        /// <summary>
        /// 克隆配置
        /// </summary>
        /// <returns>配置副本</returns>
        public MCPConfiguration Clone()
        {
            return new MCPConfiguration
            {
                ServerUrl = ServerUrl,
                ConnectionType = ConnectionType,
                AuthenticationType = AuthenticationType,
                ApiKey = ApiKey,
                Username = Username,
                Password = Password,
                ClientCertificatePath = ClientCertificatePath,
                ClientCertificatePassword = ClientCertificatePassword,
                ConnectionTimeoutSeconds = ConnectionTimeoutSeconds,
                RequestTimeoutSeconds = RequestTimeoutSeconds,
                HeartbeatIntervalSeconds = HeartbeatIntervalSeconds,
                MaxReconnectAttempts = MaxReconnectAttempts,
                ReconnectIntervalSeconds = ReconnectIntervalSeconds,
                EnableAutoReconnect = EnableAutoReconnect,
                EnableCompression = EnableCompression,
                BufferSize = BufferSize,
                MaxMessageSize = MaxMessageSize,
                EnableVerboseLogging = EnableVerboseLogging,
                CustomHeaders = new Dictionary<string, string>(CustomHeaders),
                CustomParameters = new Dictionary<string, object>(CustomParameters),
                SupportedProtocolVersions = new List<string>(SupportedProtocolVersions),
                ClientCapabilities = ClientCapabilities.Clone()
            };
        }

        /// <summary>
        /// 获取默认配置
        /// </summary>
        /// <returns>默认配置</returns>
        public static MCPConfiguration GetDefault()
        {
            return new MCPConfiguration();
        }
    }

    /// <summary>
    /// MCP连接类型
    /// </summary>
    public enum MCPConnectionType
    {
        /// <summary>
        /// WebSocket连接
        /// </summary>
        WebSocket,

        /// <summary>
        /// HTTP连接
        /// </summary>
        HTTP,

        /// <summary>
        /// TCP连接
        /// </summary>
        TCP
    }

    /// <summary>
    /// MCP认证类型
    /// </summary>
    public enum MCPAuthenticationType
    {
        /// <summary>
        /// 无认证
        /// </summary>
        None,

        /// <summary>
        /// API密钥认证
        /// </summary>
        ApiKey,

        /// <summary>
        /// 基本认证
        /// </summary>
        Basic,

        /// <summary>
        /// 证书认证
        /// </summary>
        Certificate,

        /// <summary>
        /// OAuth认证
        /// </summary>
        OAuth
    }

    /// <summary>
    /// MCP客户端能力配置
    /// </summary>
    public class MCPClientCapabilitiesConfig
    {
        /// <summary>
        /// 是否支持根列表变更
        /// </summary>
        public bool SupportsRootsListChanged { get; set; } = true;

        /// <summary>
        /// 是否支持采样
        /// </summary>
        public bool SupportsSampling { get; set; } = true;

        /// <summary>
        /// 支持的采样功能
        /// </summary>
        public List<string> SupportedSamplingFeatures { get; set; } = new() { "temperature", "maxTokens", "stopSequences" };

        /// <summary>
        /// 克隆配置
        /// </summary>
        /// <returns>配置副本</returns>
        public MCPClientCapabilitiesConfig Clone()
        {
            return new MCPClientCapabilitiesConfig
            {
                SupportsRootsListChanged = SupportsRootsListChanged,
                SupportsSampling = SupportsSampling,
                SupportedSamplingFeatures = new List<string>(SupportedSamplingFeatures)
            };
        }
    }
}
