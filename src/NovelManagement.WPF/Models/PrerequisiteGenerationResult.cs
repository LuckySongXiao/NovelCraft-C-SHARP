using System;
using System.Collections.Generic;

namespace NovelManagement.WPF.Models
{
    /// <summary>
    /// 前置条件生成结果
    /// </summary>
    public class PrerequisiteGenerationResult
    {
        /// <summary>
        /// 项目ID
        /// </summary>
        public Guid ProjectId { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 结果消息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 生成的项目列表
        /// </summary>
        public List<string> GeneratedItems { get; set; } = new();

        #region 现有数据统计

        /// <summary>
        /// 现有剧情大纲数量
        /// </summary>
        public int ExistingPlotsCount { get; set; }

        /// <summary>
        /// 现有主要角色数量
        /// </summary>
        public int ExistingMainCharactersCount { get; set; }

        /// <summary>
        /// 现有世界设定数量
        /// </summary>
        public int ExistingWorldSettingsCount { get; set; }

        /// <summary>
        /// 现有势力数量
        /// </summary>
        public int ExistingFactionsCount { get; set; }

        #endregion

        #region 需要生成的标志

        /// <summary>
        /// 是否需要生成剧情大纲
        /// </summary>
        public bool NeedsPlotOutlines { get; set; }

        /// <summary>
        /// 是否需要生成主要角色
        /// </summary>
        public bool NeedsMainCharacters { get; set; }

        /// <summary>
        /// 是否需要生成世界设定
        /// </summary>
        public bool NeedsWorldSettings { get; set; }

        /// <summary>
        /// 是否需要生成势力
        /// </summary>
        public bool NeedsFactions { get; set; }

        #endregion

        #region 生成数量统计

        /// <summary>
        /// 生成的剧情大纲数量
        /// </summary>
        public int GeneratedPlotsCount { get; set; }

        /// <summary>
        /// 生成的主要角色数量
        /// </summary>
        public int GeneratedCharactersCount { get; set; }

        /// <summary>
        /// 生成的世界设定数量
        /// </summary>
        public int GeneratedWorldSettingsCount { get; set; }

        /// <summary>
        /// 生成的势力数量
        /// </summary>
        public int GeneratedFactionsCount { get; set; }

        #endregion

        #region 辅助属性

        /// <summary>
        /// 是否有任何数据需要生成
        /// </summary>
        public bool HasDataToGenerate => NeedsPlotOutlines || NeedsMainCharacters || NeedsWorldSettings || NeedsFactions;

        /// <summary>
        /// 总生成项目数量
        /// </summary>
        public int TotalGeneratedCount => GeneratedPlotsCount + GeneratedCharactersCount + GeneratedWorldSettingsCount + GeneratedFactionsCount;

        /// <summary>
        /// 获取生成摘要
        /// </summary>
        public string GetGenerationSummary()
        {
            if (!IsSuccess)
            {
                return $"生成失败: {Message}";
            }

            if (TotalGeneratedCount == 0)
            {
                return "项目已有足够的前置数据，无需生成";
            }

            var summary = $"成功生成 {TotalGeneratedCount} 项前置数据：";
            
            if (GeneratedPlotsCount > 0)
            {
                summary += $"\n• 剧情大纲: {GeneratedPlotsCount} 个";
            }
            
            if (GeneratedCharactersCount > 0)
            {
                summary += $"\n• 主要角色: {GeneratedCharactersCount} 个";
            }
            
            if (GeneratedWorldSettingsCount > 0)
            {
                summary += $"\n• 世界设定: {GeneratedWorldSettingsCount} 个";
            }
            
            if (GeneratedFactionsCount > 0)
            {
                summary += $"\n• 势力组织: {GeneratedFactionsCount} 个";
            }

            return summary;
        }

        /// <summary>
        /// 获取详细报告
        /// </summary>
        public string GetDetailedReport()
        {
            var report = $"项目 {ProjectId} 前置条件生成报告\n";
            report += $"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
            report += $"生成状态: {(IsSuccess ? "成功" : "失败")}\n\n";

            if (!IsSuccess)
            {
                report += $"失败原因: {Message}\n";
                return report;
            }

            // 现有数据统计
            report += "现有数据统计:\n";
            report += $"• 剧情大纲: {ExistingPlotsCount} 个\n";
            report += $"• 主要角色: {ExistingMainCharactersCount} 个\n";
            report += $"• 世界设定: {ExistingWorldSettingsCount} 个\n";
            report += $"• 势力组织: {ExistingFactionsCount} 个\n\n";

            // 生成数据统计
            if (TotalGeneratedCount > 0)
            {
                report += "新生成数据:\n";
                if (GeneratedPlotsCount > 0)
                {
                    report += $"• 剧情大纲: {GeneratedPlotsCount} 个\n";
                }
                if (GeneratedCharactersCount > 0)
                {
                    report += $"• 主要角色: {GeneratedCharactersCount} 个\n";
                }
                if (GeneratedWorldSettingsCount > 0)
                {
                    report += $"• 世界设定: {GeneratedWorldSettingsCount} 个\n";
                }
                if (GeneratedFactionsCount > 0)
                {
                    report += $"• 势力组织: {GeneratedFactionsCount} 个\n";
                }
                report += "\n";

                // 详细生成项目列表
                if (GeneratedItems.Count > 0)
                {
                    report += "详细生成项目:\n";
                    foreach (var item in GeneratedItems)
                    {
                        report += $"• {item}\n";
                    }
                }
            }
            else
            {
                report += "无需生成新数据，项目已有足够的前置条件。\n";
            }

            return report;
        }

        #endregion
    }
}
