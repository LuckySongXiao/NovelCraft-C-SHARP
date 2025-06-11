using Microsoft.Extensions.Logging;

namespace NovelManagement.Tests.EndToEnd;

/// <summary>
/// 端到端测试运行器
/// </summary>
public class TestRunner
{
    private readonly ILogger<TestRunner> _logger;

    public TestRunner()
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<TestRunner>();
    }

    /// <summary>
    /// 运行所有端到端测试
    /// </summary>
    public async Task<bool> RunAllTestsAsync()
    {
        _logger.LogInformation("开始执行端到端测试套件");

        var testResults = new List<(string TestName, bool Passed)>();

        // 测试场景1：项目创建流程
        var projectCreationTest = new ProjectCreationTest();
        try
        {
            var result = await projectCreationTest.TestProjectCreationFlowAsync();
            testResults.Add(("项目创建流程测试", result));
            _logger.LogInformation("项目创建流程测试 {Result}", result ? "通过" : "失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "项目创建流程测试发生异常");
            testResults.Add(("项目创建流程测试", false));
        }
        finally
        {
            projectCreationTest.Dispose();
        }

        // 测试场景2：内容管理流程
        var contentManagementTest = new ContentManagementTest();
        try
        {
            var result = await contentManagementTest.TestContentManagementFlowAsync();
            testResults.Add(("内容管理流程测试", result));
            _logger.LogInformation("内容管理流程测试 {Result}", result ? "通过" : "失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "内容管理流程测试发生异常");
            testResults.Add(("内容管理流程测试", false));
        }
        finally
        {
            contentManagementTest.Dispose();
        }

        // 测试场景4：数据导入导出
        var dataImportExportTest = new DataImportExportTest();
        try
        {
            var result = await dataImportExportTest.TestDataImportExportFlowAsync();
            testResults.Add(("数据导入导出测试", result));
            _logger.LogInformation("数据导入导出测试 {Result}", result ? "通过" : "失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据导入导出测试发生异常");
            testResults.Add(("数据导入导出测试", false));
        }
        finally
        {
            dataImportExportTest.Dispose();
        }

        // 输出测试结果摘要
        PrintTestSummary(testResults);

        // 返回是否所有测试都通过
        return testResults.All(r => r.Passed);
    }

    /// <summary>
    /// 打印测试结果摘要
    /// </summary>
    private void PrintTestSummary(List<(string TestName, bool Passed)> testResults)
    {
        _logger.LogInformation("=== 端到端测试结果摘要 ===");
        
        foreach (var (testName, passed) in testResults)
        {
            var status = passed ? "✅ 通过" : "❌ 失败";
            _logger.LogInformation("{TestName}: {Status}", testName, status);
        }

        var passedCount = testResults.Count(r => r.Passed);
        var totalCount = testResults.Count;
        var passRate = totalCount > 0 ? (double)passedCount / totalCount * 100 : 0;

        _logger.LogInformation("总计: {PassedCount}/{TotalCount} 通过 ({PassRate:F1}%)", 
            passedCount, totalCount, passRate);

        if (passedCount == totalCount)
        {
            _logger.LogInformation("🎉 所有测试都通过了！");
        }
        else
        {
            _logger.LogWarning("⚠️ 有 {FailedCount} 个测试失败", totalCount - passedCount);
        }
    }

    /// <summary>
    /// 主程序入口
    /// </summary>
    public static async Task Main(string[] args)
    {
        var runner = new TestRunner();
        var success = await runner.RunAllTestsAsync();
        
        Environment.Exit(success ? 0 : 1);
    }
}
