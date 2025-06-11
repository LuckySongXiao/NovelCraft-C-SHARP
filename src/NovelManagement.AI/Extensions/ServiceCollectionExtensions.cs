using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NovelManagement.AI.Services;
using NovelManagement.AI.Services.DeepSeek;
using NovelManagement.AI.Services.DeepSeek.Models;
using NovelManagement.AI.Services.Ollama;
using NovelManagement.AI.Services.Ollama.Models;
using NovelManagement.AI.Services.MCP;
using NovelManagement.AI.Services.MCP.Models;
using NovelManagement.AI.Services.ThinkingChain;
using NovelManagement.AI.Agents;
using NovelManagement.AI.Interfaces;

namespace NovelManagement.AI.Extensions
{
    /// <summary>
    /// 服务集合扩展方法
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加AI服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configuration">配置</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddAIServices(this IServiceCollection services, IConfiguration configuration)
        {
            // 注册配置
            services.Configure<DeepSeekConfiguration>(configuration.GetSection("AI:Providers:DeepSeek"));
            services.Configure<OllamaConfiguration>(configuration.GetSection("AI:Providers:Ollama"));
            services.Configure<MCPConfiguration>(configuration.GetSection("AI:Providers:MCP"));

            // 注册HTTP客户端
            services.AddHttpClient();

            // 注册思维链处理器
            services.AddSingleton<IThinkingChainProcessor, ThinkingChainProcessor>();

            // 注册模型提供者
            services.AddModelProviders();

            // 注册模型管理器
            services.AddSingleton<ModelManager>();

            // 注册Agent服务
            services.AddAgentServices();

            return services;
        }

        /// <summary>
        /// 添加模型提供者
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddModelProviders(this IServiceCollection services)
        {
            // 注册DeepSeek API服务
            services.AddSingleton<IDeepSeekApiService, DeepSeekApiService>();

            // 注册Ollama API服务
            services.AddSingleton<IOllamaApiService, OllamaApiService>();

            // 注册MCP服务
            // services.AddSingleton<IMCPService, MCPService>(); // 待实现

            return services;
        }

        /// <summary>
        /// 添加Agent服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddAgentServices(this IServiceCollection services)
        {
            // 注册所有Agent
            services.AddTransient<DirectorAgent>();
            services.AddTransient<WriterAgent>();
            services.AddTransient<EditorAgent>();
            services.AddTransient<CriticAgent>();
            services.AddTransient<ResearcherAgent>();

            // 注册Agent工厂（如果需要）
            services.AddSingleton<IAgentFactory, AgentFactory>();

            return services;
        }

        /// <summary>
        /// 添加WPF相关服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddWPFServices(this IServiceCollection services)
        {
            // WPF相关服务将在WPF项目中单独注册

            return services;
        }
    }

    /// <summary>
    /// Agent工厂接口
    /// </summary>
    public interface IAgentFactory
    {
        /// <summary>
        /// 创建Agent
        /// </summary>
        /// <typeparam name="T">Agent类型</typeparam>
        /// <returns>Agent实例</returns>
        T CreateAgent<T>() where T : class, IAgent;

        /// <summary>
        /// 创建Agent
        /// </summary>
        /// <param name="agentType">Agent类型名称</param>
        /// <returns>Agent实例</returns>
        IAgent CreateAgent(string agentType);

        /// <summary>
        /// 获取所有可用的Agent类型
        /// </summary>
        /// <returns>Agent类型列表</returns>
        List<string> GetAvailableAgentTypes();
    }

    /// <summary>
    /// Agent工厂实现
    /// </summary>
    public class AgentFactory : IAgentFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AgentFactory> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="serviceProvider">服务提供者</param>
        /// <param name="logger">日志记录器</param>
        public AgentFactory(IServiceProvider serviceProvider, ILogger<AgentFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// 创建Agent
        /// </summary>
        /// <typeparam name="T">Agent类型</typeparam>
        /// <returns>Agent实例</returns>
        public T CreateAgent<T>() where T : class, IAgent
        {
            try
            {
                var agent = _serviceProvider.GetRequiredService<T>();
                _logger.LogDebug("成功创建Agent: {AgentType}", typeof(T).Name);
                return agent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建Agent失败: {AgentType}", typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// 创建Agent
        /// </summary>
        /// <param name="agentType">Agent类型名称</param>
        /// <returns>Agent实例</returns>
        public IAgent CreateAgent(string agentType)
        {
            try
            {
                IAgent agent = agentType.ToLower() switch
                {
                    "director" => _serviceProvider.GetRequiredService<DirectorAgent>(),
                    "writer" => _serviceProvider.GetRequiredService<WriterAgent>(),
                    "editor" => _serviceProvider.GetRequiredService<EditorAgent>(),
                    "critic" => _serviceProvider.GetRequiredService<CriticAgent>(),
                    "researcher" => _serviceProvider.GetRequiredService<ResearcherAgent>(),
                    "summarizer" => _serviceProvider.GetRequiredService<SummarizerAgent>(),
                    "reader" => _serviceProvider.GetRequiredService<ReaderAgent>(),
                    "settingmanager" => _serviceProvider.GetRequiredService<SettingManagerAgent>(),
                    _ => throw new ArgumentException($"不支持的Agent类型: {agentType}")
                };

                _logger.LogDebug("成功创建Agent: {AgentType}", agentType);
                return agent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建Agent失败: {AgentType}", agentType);
                throw;
            }
        }

        /// <summary>
        /// 获取所有可用的Agent类型
        /// </summary>
        /// <returns>Agent类型列表</returns>
        public List<string> GetAvailableAgentTypes()
        {
            return new List<string>
            {
                "director",
                "writer",
                "editor",
                "critic",
                "researcher",
                "summarizer",
                "reader",
                "settingmanager"
            };
        }
    }
}
