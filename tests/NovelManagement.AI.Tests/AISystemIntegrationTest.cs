using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NovelManagement.AI.Agents;
using NovelManagement.AI.Interfaces;
using NovelManagement.AI.Workflow;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NovelManagement.AI.Tests
{
    /// <summary>
    /// AI系统集成测试
    /// </summary>
    public class AISystemIntegrationTest
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AISystemIntegrationTest> _logger;

        public AISystemIntegrationTest()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
            _logger = _serviceProvider.GetRequiredService<ILogger<AISystemIntegrationTest>>();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // 配置日志
            services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Information);
            });

            // 注册记忆管理
            services.AddSingleton<ICompressionEngine, NovelManagement.AI.Memory.CompressionEngine>();
            services.AddSingleton<IMemoryManager, NovelManagement.AI.Memory.MemoryManager>();

            // 注册AI Agent
            services.AddScoped<DirectorAgent>();
            services.AddScoped<WriterAgent>();
            services.AddScoped<SummarizerAgent>();
            services.AddScoped<ReaderAgent>();
            services.AddScoped<SettingManagerAgent>();

            // 注册工作流引擎
            services.AddScoped<NovelWorkflowEngine>();
        }

        /// <summary>
        /// 执行完整的AI系统集成测试
        /// </summary>
        public async Task<bool> RunCompleteTestAsync()
        {
            _logger.LogInformation("🚀 开始AI系统集成测试");

            try
            {
                // 测试1: Agent状态监控测试
                var agentTest = await TestAgentStatusMonitoringAsync();
                _logger.LogInformation($"Agent状态监控测试: {(agentTest ? "✅ 通过" : "❌ 失败")}");

                // 测试2: 工作流执行测试
                var workflowTest = await TestWorkflowExecutionAsync();
                _logger.LogInformation($"工作流执行测试: {(workflowTest ? "✅ 通过" : "❌ 失败")}");

                // 测试3: 记忆管理测试
                var memoryTest = await TestMemoryManagementAsync();
                _logger.LogInformation($"记忆管理测试: {(memoryTest ? "✅ 通过" : "❌ 失败")}");

                var allTestsPassed = agentTest && workflowTest && memoryTest;
                
                _logger.LogInformation($"=== AI系统集成测试结果摘要 ===");
                _logger.LogInformation($"Agent状态监控测试: {(agentTest ? "✅ 通过" : "❌ 失败")}");
                _logger.LogInformation($"工作流执行测试: {(workflowTest ? "✅ 通过" : "❌ 失败")}");
                _logger.LogInformation($"记忆管理测试: {(memoryTest ? "✅ 通过" : "❌ 失败")}");
                _logger.LogInformation($"总计: {(allTestsPassed ? "3/3 通过 (100.0%)" : "部分失败")}");
                _logger.LogInformation($"{(allTestsPassed ? "🎉 所有AI系统测试都通过了！" : "⚠️ 部分测试失败，需要检查")}");

                return allTestsPassed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI系统集成测试执行异常");
                return false;
            }
        }

        /// <summary>
        /// 测试Agent状态监控
        /// </summary>
        private async Task<bool> TestAgentStatusMonitoringAsync()
        {
            _logger.LogInformation("📊 开始Agent状态监控测试");

            try
            {
                // 获取所有Agent实例
                var agents = new List<IAgent>
                {
                    _serviceProvider.GetRequiredService<DirectorAgent>(),
                    _serviceProvider.GetRequiredService<WriterAgent>(),
                    _serviceProvider.GetRequiredService<SummarizerAgent>(),
                    _serviceProvider.GetRequiredService<ReaderAgent>(),
                    _serviceProvider.GetRequiredService<SettingManagerAgent>()
                };

                _logger.LogInformation($"验证{agents.Count}个Agent的状态显示");

                foreach (var agent in agents)
                {
                    // 测试Agent基本信息
                    _logger.LogInformation($"Agent: {agent.Name} - {agent.Description}");
                    _logger.LogInformation($"状态: {agent.Status}, 版本: {agent.Version}");

                    // 测试获取状态信息
                    var statusInfo = await agent.GetStatusAsync();
                    _logger.LogInformation($"详细状态: {statusInfo.Status}, 最后活动: {statusInfo.LastActivity}");

                    // 测试获取能力信息
                    var capabilities = await agent.GetCapabilitiesAsync();
                    _logger.LogInformation($"能力数量: {capabilities.Count}");

                    // 测试Agent初始化
                    var initResult = await agent.InitializeAsync(new Dictionary<string, object>());
                    _logger.LogInformation($"初始化结果: {(initResult ? "成功" : "失败")}");

                    if (!initResult)
                    {
                        _logger.LogWarning($"Agent {agent.Name} 初始化失败");
                        return false;
                    }
                }

                _logger.LogInformation("✅ Agent状态监控测试完成");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Agent状态监控测试失败");
                return false;
            }
        }

        /// <summary>
        /// 测试工作流执行
        /// </summary>
        private async Task<bool> TestWorkflowExecutionAsync()
        {
            _logger.LogInformation("⚙️ 开始工作流执行测试");

            try
            {
                var workflowEngine = _serviceProvider.GetRequiredService<NovelWorkflowEngine>();

                // 创建测试工作流定义
                var workflowDefinition = new WorkflowDefinition
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "AI系统测试工作流",
                    Description = "用于测试AI系统集成的工作流",
                    Tasks = new List<WorkflowTask>
                    {
                        new WorkflowTask
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = "项目初始化任务",
                            TaskType = "InitializeProject",
                            Parameters = new Dictionary<string, object>
                            {
                                ["projectName"] = "测试项目",
                                ["projectType"] = "修仙小说"
                            }
                        },
                        new WorkflowTask
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = "章节创建任务",
                            TaskType = "CreateChapter",
                            Parameters = new Dictionary<string, object>
                            {
                                ["chapterTitle"] = "第一章：测试章节",
                                ["chapterOutline"] = "这是一个测试章节的大纲"
                            }
                        }
                    }
                };

                _logger.LogInformation($"执行工作流: {workflowDefinition.Name}");

                // 执行工作流
                var result = await workflowEngine.ExecuteWorkflowAsync(workflowDefinition);

                _logger.LogInformation($"工作流执行结果: {(result.IsSuccess ? "成功" : "失败")}");
                _logger.LogInformation($"执行时间: {result.ExecutionTime.TotalSeconds:F2}秒");
                _logger.LogInformation($"完成任务数: {result.CompletedTasks}/{result.CompletedTasks + result.FailedTasks}");

                if (result.IsSuccess)
                {
                    _logger.LogInformation("✅ 工作流执行测试完成");
                    return true;
                }
                else
                {
                    _logger.LogWarning($"工作流执行失败");
                    if (!string.IsNullOrEmpty(result.ErrorMessage))
                    {
                        _logger.LogError($"错误信息: {result.ErrorMessage}");
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "工作流执行测试失败");
                return false;
            }
        }

        /// <summary>
        /// 测试记忆管理系统
        /// </summary>
        private async Task<bool> TestMemoryManagementAsync()
        {
            _logger.LogInformation("🧠 开始记忆管理测试");

            try
            {
                var memoryManager = _serviceProvider.GetRequiredService<IMemoryManager>();

                // 测试记忆存储
                var testMemory = new MemoryItem
                {
                    Id = Guid.NewGuid().ToString(),
                    Content = "这是一个测试记忆项",
                    Type = MemoryType.WorldSetting,
                    Scope = MemoryScope.Global,
                    ImportanceScore = 8,
                    ProjectId = Guid.NewGuid(),
                    CreatedAt = DateTime.Now
                };

                _logger.LogInformation("测试记忆存储功能");
                await memoryManager.UpdateMemoryAsync(testMemory.Content, testMemory.ImportanceScore,
                    testMemory.Scope, testMemory.ProjectId, testMemory.VolumeId, testMemory.ChapterId);

                // 测试记忆搜索
                _logger.LogInformation("测试记忆搜索功能");
                var searchResults = await memoryManager.SearchMemoryAsync(
                    "测试", MemoryScope.Global, testMemory.ProjectId, 10);

                _logger.LogInformation($"搜索结果数量: {searchResults.Count}");

                // 测试记忆压缩
                _logger.LogInformation("测试记忆压缩功能");
                var compressionEngine = _serviceProvider.GetRequiredService<ICompressionEngine>();

                var compressedItems = await compressionEngine.CompressLowImportanceAsync(
                    new List<MemoryItem> { testMemory }, 5);

                _logger.LogInformation($"压缩结果: 原始1项，压缩后{compressedItems.Count}项");

                _logger.LogInformation("✅ 记忆管理测试完成");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记忆管理测试失败");
                return false;
            }
        }
    }

    /// <summary>
    /// 测试程序入口
    /// </summary>
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 启动AI系统集成测试");
            
            var test = new AISystemIntegrationTest();
            var result = await test.RunCompleteTestAsync();
            
            Console.WriteLine($"\n测试结果: {(result ? "✅ 全部通过" : "❌ 部分失败")}");
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }
    }
}
