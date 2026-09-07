using System;

namespace NovelManagement.WPF.Models
{
    /// <summary>
    /// 长篇批量生成任务选项。
    /// </summary>
    public sealed class BatchGenerationOptions
    {
        /// <summary>
        /// 无限续写模式：不设卷数上限，持续写新卷直到手动取消（默认关闭 = 3 卷）。
        /// </summary>
        public bool UnlimitedMode { get; set; }

        /// <summary>
        /// 快速切卷：当前卷再写 3 章即开始下一分卷（用于频繁验证跨卷衔接）。
        /// </summary>
        public bool NextThreeChaptersThenNewVolume { get; set; }

        /// <summary>
        /// 每卷章节数（默认 30）。
        /// </summary>
        public int ChaptersPerVolume { get; set; } = 30;

        /// <summary>
        /// 每章目标字数（默认 3000）。
        /// </summary>
        public int ChapterTargetWords { get; set; } = 3000;

        /// <summary>
        /// 校验并将越界值收敛到安全范围。
        /// </summary>
        public void Normalize()
        {
            ChaptersPerVolume = Math.Clamp(ChaptersPerVolume <= 0 ? 30 : ChaptersPerVolume, 3, 100);
            ChapterTargetWords = Math.Clamp(ChapterTargetWords <= 0 ? 3000 : ChapterTargetWords, 800, 10000);
        }
    }
}
