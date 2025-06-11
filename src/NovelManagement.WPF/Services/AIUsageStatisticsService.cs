using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NovelManagement.WPF.Services
{
    /// <summary>
    /// AI使用统计服务
    /// </summary>
    public class AIUsageStatisticsService
    {
        private readonly ILogger<AIUsageStatisticsService> _logger;
        private readonly List<AIUsageRecord> _usageRecords;
        private readonly object _lockObject = new object();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public AIUsageStatisticsService(ILogger<AIUsageStatisticsService> logger)
        {
            _logger = logger;
            _usageRecords = new List<AIUsageRecord>();
        }

        /// <summary>
        /// 记录AI使用情况
        /// </summary>
        /// <param name="functionName">功能名称</param>
        /// <param name="isSuccess">是否成功</param>
        /// <param name="executionTime">执行时间</param>
        /// <param name="tokensUsed">使用的令牌数</param>
        /// <param name="errorMessage">错误消息</param>
        public void RecordUsage(string functionName, bool isSuccess, TimeSpan executionTime, int tokensUsed = 0, string? errorMessage = null)
        {
            try
            {
                lock (_lockObject)
                {
                    var record = new AIUsageRecord
                    {
                        Id = Guid.NewGuid(),
                        FunctionName = functionName,
                        Timestamp = DateTime.Now,
                        IsSuccess = isSuccess,
                        ExecutionTime = executionTime,
                        TokensUsed = tokensUsed,
                        ErrorMessage = errorMessage
                    };

                    _usageRecords.Add(record);
                    
                    // 保持最近1000条记录
                    if (_usageRecords.Count > 1000)
                    {
                        _usageRecords.RemoveRange(0, _usageRecords.Count - 1000);
                    }
                }

                _logger.LogInformation($"记录AI使用: {functionName}, 成功: {isSuccess}, 耗时: {executionTime.TotalSeconds:F2}秒");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录AI使用统计失败");
            }
        }

        /// <summary>
        /// 获取使用统计信息
        /// </summary>
        /// <param name="timeRange">时间范围</param>
        /// <returns>统计信息</returns>
        public AIUsageStatistics GetUsageStatistics(TimeRange timeRange = TimeRange.Today)
        {
            try
            {
                lock (_lockObject)
                {
                    var startTime = GetStartTime(timeRange);
                    var filteredRecords = _usageRecords.Where(r => r.Timestamp >= startTime).ToList();

                    var statistics = new AIUsageStatistics
                    {
                        TimeRange = timeRange,
                        StartTime = startTime,
                        EndTime = DateTime.Now,
                        TotalRequests = filteredRecords.Count,
                        SuccessfulRequests = filteredRecords.Count(r => r.IsSuccess),
                        FailedRequests = filteredRecords.Count(r => !r.IsSuccess),
                        TotalTokensUsed = filteredRecords.Sum(r => r.TokensUsed),
                        AverageExecutionTime = filteredRecords.Any() ? 
                            TimeSpan.FromMilliseconds(filteredRecords.Average(r => r.ExecutionTime.TotalMilliseconds)) : 
                            TimeSpan.Zero,
                        FunctionUsageCount = filteredRecords.GroupBy(r => r.FunctionName)
                            .ToDictionary(g => g.Key, g => g.Count()),
                        HourlyUsage = GetHourlyUsage(filteredRecords),
                        RecentErrors = filteredRecords.Where(r => !r.IsSuccess)
                            .OrderByDescending(r => r.Timestamp)
                            .Take(10)
                            .Select(r => new AIErrorInfo
                            {
                                FunctionName = r.FunctionName,
                                Timestamp = r.Timestamp,
                                ErrorMessage = r.ErrorMessage ?? "未知错误"
                            })
                            .ToList()
                    };

                    statistics.SuccessRate = statistics.TotalRequests > 0 ? 
                        (double)statistics.SuccessfulRequests / statistics.TotalRequests * 100 : 0;

                    return statistics;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取AI使用统计失败");
                return new AIUsageStatistics();
            }
        }

        /// <summary>
        /// 获取功能使用排行
        /// </summary>
        /// <param name="timeRange">时间范围</param>
        /// <param name="topCount">返回数量</param>
        /// <returns>功能使用排行</returns>
        public List<FunctionUsageRank> GetFunctionUsageRanking(TimeRange timeRange = TimeRange.Today, int topCount = 10)
        {
            try
            {
                lock (_lockObject)
                {
                    var startTime = GetStartTime(timeRange);
                    var filteredRecords = _usageRecords.Where(r => r.Timestamp >= startTime).ToList();

                    return filteredRecords
                        .GroupBy(r => r.FunctionName)
                        .Select(g => new FunctionUsageRank
                        {
                            FunctionName = g.Key,
                            UsageCount = g.Count(),
                            SuccessCount = g.Count(r => r.IsSuccess),
                            FailureCount = g.Count(r => !r.IsSuccess),
                            AverageExecutionTime = TimeSpan.FromMilliseconds(g.Average(r => r.ExecutionTime.TotalMilliseconds)),
                            TotalTokensUsed = g.Sum(r => r.TokensUsed)
                        })
                        .OrderByDescending(f => f.UsageCount)
                        .Take(topCount)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取功能使用排行失败");
                return new List<FunctionUsageRank>();
            }
        }

        /// <summary>
        /// 清除统计数据
        /// </summary>
        /// <param name="olderThan">清除早于指定时间的数据</param>
        public void ClearStatistics(DateTime? olderThan = null)
        {
            try
            {
                lock (_lockObject)
                {
                    if (olderThan.HasValue)
                    {
                        _usageRecords.RemoveAll(r => r.Timestamp < olderThan.Value);
                        _logger.LogInformation($"清除了早于 {olderThan.Value} 的AI使用统计数据");
                    }
                    else
                    {
                        _usageRecords.Clear();
                        _logger.LogInformation("清除了所有AI使用统计数据");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清除AI使用统计失败");
            }
        }

        /// <summary>
        /// 导出统计数据
        /// </summary>
        /// <param name="timeRange">时间范围</param>
        /// <returns>CSV格式的统计数据</returns>
        public string ExportStatistics(TimeRange timeRange = TimeRange.Today)
        {
            try
            {
                lock (_lockObject)
                {
                    var startTime = GetStartTime(timeRange);
                    var filteredRecords = _usageRecords.Where(r => r.Timestamp >= startTime).ToList();

                    var csv = "时间,功能名称,是否成功,执行时间(秒),令牌使用,错误消息\n";
                    foreach (var record in filteredRecords.OrderBy(r => r.Timestamp))
                    {
                        csv += $"{record.Timestamp:yyyy-MM-dd HH:mm:ss}," +
                               $"{record.FunctionName}," +
                               $"{(record.IsSuccess ? "成功" : "失败")}," +
                               $"{record.ExecutionTime.TotalSeconds:F2}," +
                               $"{record.TokensUsed}," +
                               $"\"{record.ErrorMessage?.Replace("\"", "\"\"") ?? ""}\"\n";
                    }

                    return csv;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出AI使用统计失败");
                return string.Empty;
            }
        }

        /// <summary>
        /// 获取开始时间
        /// </summary>
        /// <param name="timeRange">时间范围</param>
        /// <returns>开始时间</returns>
        private DateTime GetStartTime(TimeRange timeRange)
        {
            var now = DateTime.Now;
            return timeRange switch
            {
                TimeRange.LastHour => now.AddHours(-1),
                TimeRange.Today => now.Date,
                TimeRange.Yesterday => now.Date.AddDays(-1),
                TimeRange.LastWeek => now.AddDays(-7),
                TimeRange.LastMonth => now.AddMonths(-1),
                TimeRange.LastYear => now.AddYears(-1),
                TimeRange.All => DateTime.MinValue,
                _ => now.Date
            };
        }

        /// <summary>
        /// 获取小时使用情况
        /// </summary>
        /// <param name="records">记录列表</param>
        /// <returns>小时使用情况</returns>
        private Dictionary<int, int> GetHourlyUsage(List<AIUsageRecord> records)
        {
            var hourlyUsage = new Dictionary<int, int>();
            for (int i = 0; i < 24; i++)
            {
                hourlyUsage[i] = 0;
            }

            foreach (var record in records)
            {
                hourlyUsage[record.Timestamp.Hour]++;
            }

            return hourlyUsage;
        }
    }

    /// <summary>
    /// AI使用记录
    /// </summary>
    public class AIUsageRecord
    {
        /// <summary>
        /// 记录ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 功能名称
        /// </summary>
        public string FunctionName { get; set; } = string.Empty;

        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 执行时间
        /// </summary>
        public TimeSpan ExecutionTime { get; set; }

        /// <summary>
        /// 使用的令牌数
        /// </summary>
        public int TokensUsed { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// AI使用统计信息
    /// </summary>
    public class AIUsageStatistics
    {
        /// <summary>
        /// 时间范围
        /// </summary>
        public TimeRange TimeRange { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// 总请求数
        /// </summary>
        public int TotalRequests { get; set; }

        /// <summary>
        /// 成功请求数
        /// </summary>
        public int SuccessfulRequests { get; set; }

        /// <summary>
        /// 失败请求数
        /// </summary>
        public int FailedRequests { get; set; }

        /// <summary>
        /// 成功率
        /// </summary>
        public double SuccessRate { get; set; }

        /// <summary>
        /// 总令牌使用数
        /// </summary>
        public int TotalTokensUsed { get; set; }

        /// <summary>
        /// 平均执行时间
        /// </summary>
        public TimeSpan AverageExecutionTime { get; set; }

        /// <summary>
        /// 功能使用次数统计
        /// </summary>
        public Dictionary<string, int> FunctionUsageCount { get; set; } = new();

        /// <summary>
        /// 小时使用情况
        /// </summary>
        public Dictionary<int, int> HourlyUsage { get; set; } = new();

        /// <summary>
        /// 最近错误
        /// </summary>
        public List<AIErrorInfo> RecentErrors { get; set; } = new();
    }

    /// <summary>
    /// 功能使用排行
    /// </summary>
    public class FunctionUsageRank
    {
        /// <summary>
        /// 功能名称
        /// </summary>
        public string FunctionName { get; set; } = string.Empty;

        /// <summary>
        /// 使用次数
        /// </summary>
        public int UsageCount { get; set; }

        /// <summary>
        /// 成功次数
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// 失败次数
        /// </summary>
        public int FailureCount { get; set; }

        /// <summary>
        /// 平均执行时间
        /// </summary>
        public TimeSpan AverageExecutionTime { get; set; }

        /// <summary>
        /// 总令牌使用数
        /// </summary>
        public int TotalTokensUsed { get; set; }

        /// <summary>
        /// 成功率
        /// </summary>
        public double SuccessRate => UsageCount > 0 ? (double)SuccessCount / UsageCount * 100 : 0;
    }

    /// <summary>
    /// AI错误信息
    /// </summary>
    public class AIErrorInfo
    {
        /// <summary>
        /// 功能名称
        /// </summary>
        public string FunctionName { get; set; } = string.Empty;

        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// 时间范围
    /// </summary>
    public enum TimeRange
    {
        /// <summary>
        /// 最近一小时
        /// </summary>
        LastHour,

        /// <summary>
        /// 今天
        /// </summary>
        Today,

        /// <summary>
        /// 昨天
        /// </summary>
        Yesterday,

        /// <summary>
        /// 最近一周
        /// </summary>
        LastWeek,

        /// <summary>
        /// 最近一个月
        /// </summary>
        LastMonth,

        /// <summary>
        /// 最近一年
        /// </summary>
        LastYear,

        /// <summary>
        /// 全部
        /// </summary>
        All
    }
}
