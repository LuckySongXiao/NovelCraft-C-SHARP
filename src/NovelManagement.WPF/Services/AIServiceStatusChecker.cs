using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NovelManagement.AI.Services.Ollama;
using NovelManagement.AI.Services.DeepSeek;
using NovelManagement.AI.Agents;
using NovelManagement.AI.Interfaces;
using NovelManagement.AI.Extensions;
using NovelManagement.WPF.Services;

namespace NovelManagement.WPF.Services
{
    /// <summary>
    /// AI服务状态检查器
    /// </summary>
    public class AIServiceStatusChecker
    {
        private readonly ILogger<AIServiceStatusChecker> _logger;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="serviceProvider">服务提供者</param>
        public AIServiceStatusChecker(ILogger<AIServiceStatusChecker> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// 检查AI服务状态
        /// </summary>
        /// <returns>服务状态结果</returns>
        public async Task<AIServiceStatus> CheckServiceStatusAsync()
        {
            var status = new AIServiceStatus();

            try
            {
                // 检查AIAssistantService
                status.AIAssistantServiceAvailable = CheckAIAssistantService();

                // 检查Ollama服务
                status.OllamaServiceStatus = await CheckOllamaServiceAsync();

                // 检查DeepSeek服务
                status.DeepSeekServiceStatus = await CheckDeepSeekServiceAsync();

                // 检查Agent工厂
                status.AgentFactoryAvailable = CheckAgentFactory();

                // 计算总体状态
                status.OverallStatus = CalculateOverallStatus(status);

                _logger.LogInformation($"AI服务状态检查完成: {status.OverallStatus}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查AI服务状态失败");
                status.OverallStatus = ServiceStatus.Error;
                status.ErrorMessage = ex.Message;
            }

            return status;
        }

        /// <summary>
        /// 检查AI助手服务
        /// </summary>
        private bool CheckAIAssistantService()
        {
            try
            {
                // 检查IAIAssistantService接口
                var aiService = _serviceProvider.GetService<IAIAssistantService>();
                if (aiService != null)
                {
                    _logger.LogInformation("AI助手服务接口可用");
                    return true;
                }

                // 如果接口不可用，尝试检查具体实现
                var concreteService = _serviceProvider.GetService<AIAssistantService>();
                if (concreteService != null)
                {
                    _logger.LogInformation("AI助手服务具体实现可用");
                    return true;
                }

                _logger.LogWarning("AI助手服务未注册或不可用");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "检查AI助手服务失败");
                return false;
            }
        }

        /// <summary>
        /// 检查Ollama服务
        /// </summary>
        private async Task<ServiceStatus> CheckOllamaServiceAsync()
        {
            try
            {
                var ollamaService = _serviceProvider.GetService<IOllamaApiService>();
                if (ollamaService == null)
                {
                    _logger.LogWarning("Ollama服务未注册");
                    return ServiceStatus.NotAvailable;
                }

                // 检查服务是否可用
                if (!ollamaService.IsAvailable)
                {
                    _logger.LogWarning("Ollama服务标记为不可用");
                    return ServiceStatus.NotAvailable;
                }

                var testResult = await ollamaService.TestConnectionAsync();
                if (testResult.IsSuccess)
                {
                    _logger.LogInformation("Ollama服务连接测试成功");
                    return ServiceStatus.Available;
                }
                else
                {
                    _logger.LogWarning($"Ollama服务连接测试失败: {testResult.ErrorMessage}");
                    return ServiceStatus.Error;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "检查Ollama服务失败");
                return ServiceStatus.Error;
            }
        }

        /// <summary>
        /// 检查DeepSeek服务
        /// </summary>
        private async Task<ServiceStatus> CheckDeepSeekServiceAsync()
        {
            try
            {
                var deepSeekService = _serviceProvider.GetService<IDeepSeekApiService>();
                if (deepSeekService == null)
                {
                    _logger.LogWarning("DeepSeek服务未注册");
                    return ServiceStatus.NotAvailable;
                }

                var testResult = await deepSeekService.TestConnectionAsync();
                if (testResult)
                {
                    _logger.LogInformation("DeepSeek服务连接测试成功");
                    return ServiceStatus.Available;
                }
                else
                {
                    _logger.LogWarning("DeepSeek服务连接测试失败");
                    return ServiceStatus.Error;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "检查DeepSeek服务失败");
                return ServiceStatus.Error;
            }
        }

        /// <summary>
        /// 检查Agent工厂
        /// </summary>
        private bool CheckAgentFactory()
        {
            try
            {
                var agentFactory = _serviceProvider.GetService<IAgentFactory>();
                if (agentFactory == null)
                {
                    return false;
                }

                // 尝试创建一个测试Agent
                var testAgent = agentFactory.CreateAgent<DirectorAgent>();
                return testAgent != null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "检查Agent工厂失败");
                return false;
            }
        }

        /// <summary>
        /// 计算总体状态
        /// </summary>
        private ServiceStatus CalculateOverallStatus(AIServiceStatus status)
        {
            if (!status.AIAssistantServiceAvailable || !status.AgentFactoryAvailable)
            {
                return ServiceStatus.Error;
            }

            if (status.OllamaServiceStatus == ServiceStatus.Available || 
                status.DeepSeekServiceStatus == ServiceStatus.Available)
            {
                return ServiceStatus.Available;
            }

            return ServiceStatus.NotAvailable;
        }
    }

    /// <summary>
    /// AI服务状态
    /// </summary>
    public class AIServiceStatus
    {
        /// <summary>
        /// AI助手服务是否可用
        /// </summary>
        public bool AIAssistantServiceAvailable { get; set; }

        /// <summary>
        /// Ollama服务状态
        /// </summary>
        public ServiceStatus OllamaServiceStatus { get; set; }

        /// <summary>
        /// DeepSeek服务状态
        /// </summary>
        public ServiceStatus DeepSeekServiceStatus { get; set; }

        /// <summary>
        /// Agent工厂是否可用
        /// </summary>
        public bool AgentFactoryAvailable { get; set; }

        /// <summary>
        /// 总体状态
        /// </summary>
        public ServiceStatus OverallStatus { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 获取状态描述
        /// </summary>
        public string GetStatusDescription()
        {
            return OverallStatus switch
            {
                ServiceStatus.Available => "AI服务正常运行",
                ServiceStatus.NotAvailable => "AI服务不可用，请检查配置",
                ServiceStatus.Error => $"AI服务错误: {ErrorMessage}",
                _ => "未知状态"
            };
        }
    }

    /// <summary>
    /// 服务状态枚举
    /// </summary>
    public enum ServiceStatus
    {
        /// <summary>
        /// 可用
        /// </summary>
        Available,

        /// <summary>
        /// 不可用
        /// </summary>
        NotAvailable,

        /// <summary>
        /// 错误
        /// </summary>
        Error
    }
}
