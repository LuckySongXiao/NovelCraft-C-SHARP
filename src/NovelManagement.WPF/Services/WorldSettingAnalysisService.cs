using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NovelManagement.Application.DTOs;

namespace NovelManagement.WPF.Services
{
    /// <summary>
    /// 世界设定分析服务
    /// </summary>
    public class WorldSettingAnalysisService
    {
        #region 字段和属性

        private readonly ILogger<WorldSettingAnalysisService>? _logger;

        /// <summary>
        /// 分析结果
        /// </summary>
        public class AnalysisResult
        {
            public string SettingName { get; set; } = string.Empty;
            public double ConsistencyScore { get; set; }
            public double CompletenessScore { get; set; }
            public double LogicalScore { get; set; }
            public double OverallScore => (ConsistencyScore + CompletenessScore + LogicalScore) / 3;
            public List<string> Strengths { get; set; } = new();
            public List<string> Weaknesses { get; set; } = new();
            public List<string> Suggestions { get; set; } = new();
            public List<string> Conflicts { get; set; } = new();
            public List<string> MissingElements { get; set; } = new();
            public DateTime AnalyzedAt { get; set; }
        }

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public WorldSettingAnalysisService(ILogger<WorldSettingAnalysisService>? logger = null)
        {
            _logger = logger;
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 分析世界设定
        /// </summary>
        /// <param name="setting">要分析的设定</param>
        /// <param name="allSettings">所有设定（用于一致性检查）</param>
        /// <returns>分析结果</returns>
        public async Task<AnalysisResult> AnalyzeWorldSettingAsync(WorldSettingDto setting, List<WorldSettingDto> allSettings)
        {
            try
            {
                _logger?.LogInformation("开始分析世界设定: {SettingName}", setting.Name);

                // 模拟AI分析过程
                await Task.Delay(2000);

                var result = new AnalysisResult
                {
                    SettingName = setting.Name,
                    AnalyzedAt = DateTime.Now
                };

                // 分析一致性
                result.ConsistencyScore = await AnalyzeConsistency(setting, allSettings);

                // 分析完整性
                result.CompletenessScore = await AnalyzeCompleteness(setting);

                // 分析逻辑性
                result.LogicalScore = await AnalyzeLogic(setting);

                // 生成优势、劣势和建议
                GenerateInsights(setting, allSettings, result);

                _logger?.LogInformation("世界设定分析完成: {SettingName}, 总体评分: {Score:F1}", 
                    setting.Name, result.OverallScore);

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "分析世界设定失败: {SettingName}", setting.Name);
                throw;
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 分析一致性
        /// </summary>
        private async Task<double> AnalyzeConsistency(WorldSettingDto setting, List<WorldSettingDto> allSettings)
        {
            await Task.Delay(100);

            var score = 7.0; // 基础分数

            // 检查与其他设定的冲突
            var relatedSettings = allSettings.Where(s => 
                s.Id != setting.Id && 
                (s.Category == setting.Category || s.Type == setting.Type)).ToList();

            foreach (var related in relatedSettings)
            {
                if (HasPotentialConflict(setting, related))
                {
                    score -= 1.0;
                }
                else if (HasGoodSynergy(setting, related))
                {
                    score += 0.5;
                }
            }

            return Math.Max(1.0, Math.Min(10.0, score));
        }

        /// <summary>
        /// 分析完整性
        /// </summary>
        private async Task<double> AnalyzeCompleteness(WorldSettingDto setting)
        {
            await Task.Delay(100);

            var score = 5.0; // 基础分数

            // 检查必要字段
            if (!string.IsNullOrEmpty(setting.Description)) score += 1.0;
            if (!string.IsNullOrEmpty(setting.Content)) score += 1.5;
            if (!string.IsNullOrEmpty(setting.Rules)) score += 1.0;
            if (!string.IsNullOrEmpty(setting.History)) score += 0.5;

            // 检查内容长度
            var totalLength = (setting.Description?.Length ?? 0) + (setting.Content?.Length ?? 0);
            if (totalLength > 100) score += 0.5;
            if (totalLength > 500) score += 0.5;

            // 检查重要性设置
            if (setting.Importance > 5) score += 0.5;

            return Math.Max(1.0, Math.Min(10.0, score));
        }

        /// <summary>
        /// 分析逻辑性
        /// </summary>
        private async Task<double> AnalyzeLogic(WorldSettingDto setting)
        {
            await Task.Delay(100);

            var score = 6.0; // 基础分数

            // 检查逻辑关键词
            var content = $"{setting.Description} {setting.Content} {setting.Rules}".ToLower();
            
            var logicalKeywords = new[] { "因为", "所以", "因此", "由于", "基于", "导致", "影响", "关系" };
            var keywordCount = logicalKeywords.Count(kw => content.Contains(kw));
            score += keywordCount * 0.3;

            // 检查矛盾词汇
            var contradictoryPairs = new[]
            {
                new[] { "强大", "弱小" },
                new[] { "古老", "新兴" },
                new[] { "和平", "战争" },
                new[] { "繁荣", "衰落" }
            };

            foreach (var pair in contradictoryPairs)
            {
                if (content.Contains(pair[0]) && content.Contains(pair[1]))
                {
                    score -= 0.5; // 可能存在逻辑矛盾
                }
            }

            return Math.Max(1.0, Math.Min(10.0, score));
        }

        /// <summary>
        /// 检查潜在冲突
        /// </summary>
        private bool HasPotentialConflict(WorldSettingDto setting1, WorldSettingDto setting2)
        {
            // 简单的冲突检测逻辑
            var content1 = $"{setting1.Description} {setting1.Content}".ToLower();
            var content2 = $"{setting2.Description} {setting2.Content}".ToLower();

            var conflictKeywords = new[]
            {
                new[] { "禁止", "允许" },
                new[] { "不存在", "存在" },
                new[] { "无法", "可以" }
            };

            foreach (var keywords in conflictKeywords)
            {
                if ((content1.Contains(keywords[0]) && content2.Contains(keywords[1])) ||
                    (content1.Contains(keywords[1]) && content2.Contains(keywords[0])))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 检查良好协同
        /// </summary>
        private bool HasGoodSynergy(WorldSettingDto setting1, WorldSettingDto setting2)
        {
            // 简单的协同检测逻辑
            var content1 = $"{setting1.Description} {setting1.Content}".ToLower();
            var content2 = $"{setting2.Description} {setting2.Content}".ToLower();

            var synergyKeywords = new[] { "相关", "配合", "支持", "补充", "协调" };

            return synergyKeywords.Any(kw => content1.Contains(kw) || content2.Contains(kw));
        }

        /// <summary>
        /// 生成洞察和建议
        /// </summary>
        private void GenerateInsights(WorldSettingDto setting, List<WorldSettingDto> allSettings, AnalysisResult result)
        {
            // 生成优势
            if (result.ConsistencyScore >= 8.0)
                result.Strengths.Add("与其他设定保持良好的一致性");
            if (result.CompletenessScore >= 8.0)
                result.Strengths.Add("设定内容详细完整");
            if (result.LogicalScore >= 8.0)
                result.Strengths.Add("逻辑结构清晰合理");
            if (setting.Importance >= 8)
                result.Strengths.Add("在世界观中具有重要地位");

            // 生成劣势
            if (result.ConsistencyScore < 6.0)
            {
                result.Weaknesses.Add("与其他设定存在潜在冲突");
                result.Conflicts.Add("检测到与相关设定的不一致之处");
            }
            if (result.CompletenessScore < 6.0)
            {
                result.Weaknesses.Add("设定内容不够完整");
                result.MissingElements.Add("缺少详细的规则说明");
                result.MissingElements.Add("缺少历史背景信息");
            }
            if (result.LogicalScore < 6.0)
            {
                result.Weaknesses.Add("逻辑结构需要改进");
            }

            // 生成建议
            if (result.ConsistencyScore < 7.0)
                result.Suggestions.Add("建议检查与其他设定的关联性，确保世界观的一致性");
            if (result.CompletenessScore < 7.0)
                result.Suggestions.Add("建议补充更多细节，包括规则、历史背景等");
            if (result.LogicalScore < 7.0)
                result.Suggestions.Add("建议重新梳理逻辑关系，确保设定的合理性");
            if (string.IsNullOrEmpty(setting.Rules))
                result.Suggestions.Add("建议添加相关规则说明，明确设定的运作机制");
            if (string.IsNullOrEmpty(setting.History))
                result.Suggestions.Add("建议添加历史背景，增加设定的深度和可信度");

            // 如果没有发现问题，给出积极建议
            if (result.OverallScore >= 8.0)
            {
                result.Suggestions.Add("设定质量很高，可以考虑扩展相关的子设定");
                result.Suggestions.Add("建议将此设定作为其他设定的参考标准");
            }
        }

        #endregion
    }
}
