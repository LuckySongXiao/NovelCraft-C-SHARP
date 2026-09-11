namespace NovelManagement.AI.Utilities
{
    /// <summary>
    /// 生成链路输出语言开关：由表现层按当前界面语言设置。
    /// <para>
    /// 经验：RWKV7-G1 等本地模型的输出语言由**提示词语言**决定（中文提示词+英文指令仍输出中文），
    /// 因此英文界面下必须整段使用英文提示词，才能得到英文正文。
    /// </para>
    /// </summary>
    public static class AIPromptLanguage
    {
        /// <summary>true = 英文界面，生成链路使用英文提示词并输出英文内容。</summary>
        public static bool UseEnglish { get; set; }
    }
}
