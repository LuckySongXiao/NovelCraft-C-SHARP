using System;
using System.IO;

namespace NovelManagement.AI.Utilities
{
    /// <summary>
    /// Agent 流程提示词模板注册表。
    /// <para>
    /// 目录结构：<c>{应用目录}/PromptTemplates/{模板ID}/{语种}.txt</c>（语种：zh / en，二次开发者可追加 de、fr…）。
    /// 语种由 <see cref="AIPromptLanguage.UseEnglish"/> 决定（en / zh）；当前语种缺失时回退中文，中文也缺失时返回 null
    /// （调用方继续使用代码内置默认值）。
    /// </para>
    /// <para>
    /// 已登记模板 ID（节选）：
    /// WriterAgent/{taskType}.System、WriterAgent/GenerateChapterContent.User、
    /// RWKV/ChapterWriting.Instruction、Prerequisite/Cultivation.System、Prerequisite/Cultivation.User、
    /// Workflow/SubAgentRequirement.System、Workflow/MainAgent.System、Workflow/SubAgentRefine.System。
    /// </para>
    /// </summary>
    public static class PromptTemplate
    {
        private static string BaseDirectory =>
            Path.Combine(AppContext.BaseDirectory, "PromptTemplates");

        /// <summary>按模板 ID 与当前语言取模板；不存在返回 null（调用方用内置默认）。</summary>
        public static string? Get(string templateId)
        {
            if (string.IsNullOrWhiteSpace(templateId))
            {
                return null;
            }

            var lang = AIPromptLanguage.UseEnglish ? "en" : "zh";
            return Read(templateId, lang) ?? Read(templateId, "zh");
        }

        /// <summary>按模板 ID + 显式语种取模板（供工具/测试使用）。</summary>
        public static string? Get(string templateId, string language)
        {
            return Read(templateId, Normalize(language));
        }

        /// <summary>该模板是否已有当前语言的外置文件。</summary>
        public static bool Exists(string templateId)
        {
            var lang = AIPromptLanguage.UseEnglish ? "en" : "zh";
            return File.Exists(Resolve(templateId, lang));
        }

        private static string Resolve(string templateId, string lang)
        {
            // 模板 ID 允许 '/' 分层；防目录穿越
            var safeId = templateId.Replace("..", string.Empty).TrimStart('/', '\\');
            return Path.Combine(BaseDirectory, safeId.Replace('/', Path.DirectorySeparatorChar), lang + ".txt");
        }

        private static string? Read(string templateId, string lang)
        {
            try
            {
                var path = Resolve(templateId, lang);
                return File.Exists(path) ? File.ReadAllText(path) : null;
            }
            catch
            {
                return null;
            }
        }

        private static string Normalize(string? language)
        {
            if (string.IsNullOrWhiteSpace(language))
            {
                return "zh";
            }

            var code = language.Trim();
            var dash = code.IndexOf('-');
            return dash > 0 ? code[..dash] : code;
        }
    }
}
