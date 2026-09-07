using System;
using System.Collections.Generic;

namespace NovelManagement.WPF.Models
{
    /// <summary>
    /// 项目上下文数据
    /// </summary>
    public class ProjectContextData
    {
        /// <summary>
        /// 项目ID
        /// </summary>
        public Guid ProjectId { get; set; } = Guid.Empty;

        /// <summary>
        /// 项目名称
        /// </summary>
        public string ProjectName { get; set; } = string.Empty;

        /// <summary>
        /// 项目描述
        /// </summary>
        public string ProjectDescription { get; set; } = string.Empty;

        /// <summary>
        /// 项目类型
        /// </summary>
        public string ProjectType { get; set; } = string.Empty;

        /// <summary>
        /// 项目标签
        /// </summary>
        public string ProjectTags { get; set; } = string.Empty;

        /// <summary>
        /// 项目备注
        /// </summary>
        public string ProjectNotes { get; set; } = string.Empty;

        /// <summary>
        /// 剧情大纲列表
        /// </summary>
        public List<object> PlotOutlines { get; set; } = new();

        /// <summary>
        /// 主要角色列表
        /// </summary>
        public List<object> MainCharacters { get; set; } = new();

        /// <summary>
        /// 世界设定列表
        /// </summary>
        public List<object> WorldSettings { get; set; } = new();

        /// <summary>
        /// 可直接拼接到 Prompt 的项目上下文摘要。
        /// </summary>
        public string PromptSummary { get; set; } = string.Empty;
    }
}
