using System.Collections.Generic;

namespace NovelManagement.WPF.Models
{
    /// <summary>
    /// 前置条件生成选项
    /// </summary>
    public class PrerequisiteGenerationOptions
    {
        /// <summary>
        /// 是否生成剧情大纲
        /// </summary>
        public bool GeneratePlotOutlines { get; set; } = true;

        /// <summary>
        /// 是否生成主要角色
        /// </summary>
        public bool GenerateMainCharacters { get; set; } = true;

        /// <summary>
        /// 是否生成世界设定
        /// </summary>
        public bool GenerateWorldSettings { get; set; } = true;

        /// <summary>
        /// 是否生成势力组织
        /// </summary>
        public bool GenerateFactions { get; set; } = true;

        /// <summary>
        /// 是否使用AI智能生成
        /// </summary>
        public bool UseAIGeneration { get; set; } = false;

        /// <summary>
        /// AI生成提示词
        /// </summary>
        public string AIPrompt { get; set; } = string.Empty;

        /// <summary>
        /// 小说类型/风格
        /// </summary>
        public string NovelGenre { get; set; } = "修仙";

        /// <summary>
        /// 世界观风格
        /// </summary>
        public string WorldStyle { get; set; } = "东方玄幻";

        /// <summary>
        /// 自定义剧情大纲模板
        /// </summary>
        public List<PlotTemplate> CustomPlotTemplates { get; set; } = new();

        /// <summary>
        /// 自定义角色模板
        /// </summary>
        public List<CharacterTemplate> CustomCharacterTemplates { get; set; } = new();

        /// <summary>
        /// 自定义世界设定模板
        /// </summary>
        public List<WorldSettingTemplate> CustomWorldSettingTemplates { get; set; } = new();

        /// <summary>
        /// 自定义势力模板
        /// </summary>
        public List<FactionTemplate> CustomFactionTemplates { get; set; } = new();

        /// <summary>
        /// 生成数量配置
        /// </summary>
        public GenerationQuantityConfig QuantityConfig { get; set; } = new();

        /// <summary>
        /// 是否允许用户后续编辑
        /// </summary>
        public bool AllowUserEditing { get; set; } = true;

        /// <summary>
        /// 是否保存为模板供后续使用
        /// </summary>
        public bool SaveAsTemplate { get; set; } = false;
    }

    /// <summary>
    /// 剧情大纲模板
    /// </summary>
    public class PlotTemplate
    {
        /// <summary>
        /// 剧情标题
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 剧情类型
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 摘要模板
        /// </summary>
        public string SummaryTemplate { get; set; } = string.Empty;

        /// <summary>
        /// 重要性等级
        /// </summary>
        public int ImportanceLevel { get; set; } = 5;

        /// <summary>
        /// 剧情节点模板列表
        /// </summary>
        public List<string> PlotPointTemplates { get; set; } = new();
    }

    /// <summary>
    /// 角色模板
    /// </summary>
    public class CharacterTemplate
    {
        /// <summary>
        /// 名称模板
        /// </summary>
        public string NameTemplate { get; set; } = string.Empty;

        /// <summary>
        /// 角色类型
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 性别
        /// </summary>
        public string Gender { get; set; } = string.Empty;

        /// <summary>
        /// 外貌模板
        /// </summary>
        public string AppearanceTemplate { get; set; } = string.Empty;

        /// <summary>
        /// 性格模板
        /// </summary>
        public string PersonalityTemplate { get; set; } = string.Empty;

        /// <summary>
        /// 背景模板
        /// </summary>
        public string BackgroundTemplate { get; set; } = string.Empty;

        /// <summary>
        /// 重要性等级
        /// </summary>
        public int ImportanceLevel { get; set; } = 5;
    }

    /// <summary>
    /// 世界设定模板
    /// </summary>
    public class WorldSettingTemplate
    {
        /// <summary>
        /// 名称模板
        /// </summary>
        public string NameTemplate { get; set; } = string.Empty;

        /// <summary>
        /// 设定类型
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 内容模板
        /// </summary>
        public string ContentTemplate { get; set; } = string.Empty;

        /// <summary>
        /// 重要性
        /// </summary>
        public int Importance { get; set; } = 5;
    }

    /// <summary>
    /// 势力模板
    /// </summary>
    public class FactionTemplate
    {
        /// <summary>
        /// 名称模板
        /// </summary>
        public string NameTemplate { get; set; } = string.Empty;

        /// <summary>
        /// 势力类型
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 描述模板
        /// </summary>
        public string DescriptionTemplate { get; set; } = string.Empty;

        /// <summary>
        /// 力量等级
        /// </summary>
        public int PowerLevel { get; set; } = 50;

        /// <summary>
        /// 重要性
        /// </summary>
        public int Importance { get; set; } = 50;

        /// <summary>
        /// 标签模板列表
        /// </summary>
        public List<string> TagTemplates { get; set; } = new();
    }

    /// <summary>
    /// 生成数量配置
    /// </summary>
    public class GenerationQuantityConfig
    {
        /// <summary>
        /// 剧情大纲生成数量
        /// </summary>
        public int PlotOutlinesCount { get; set; } = 3;

        /// <summary>
        /// 主要角色生成数量
        /// </summary>
        public int MainCharactersCount { get; set; } = 3;

        /// <summary>
        /// 世界设定生成数量
        /// </summary>
        public int WorldSettingsCount { get; set; } = 5;

        /// <summary>
        /// 势力组织生成数量
        /// </summary>
        public int FactionsCount { get; set; } = 3;
    }
}
