using System;

namespace NovelManagement.AI.Services.RWKV.Models
{
    /// <summary>
    /// 模型上下文档位（8K/16K/32K/64K/90K/128K/256K/1M）。
    /// </summary>
    public enum ContextTier
    {
        Ctx8K,
        Ctx16K,
        Ctx32K,
        Ctx64K,
        Ctx90K,
        Ctx128K,
        Ctx256K,
        Ctx1M
    }

    /// <summary>
    /// 上下文档位对应的写作策略：
    /// - 32K 以下必须切片生成（上下文越短，单片越小、片数越多），且仅注入小批量参考辅助信息；
    /// - 32K 及以上无需切片（单片一次生成整章），按档位注入更多辅助信息（大纲/前情回顾更长，角色/设定/势力更多）。
    /// </summary>
    public sealed class ContextWritingPolicy
    {
        /// <summary>档位名称（如 "16K"）。</summary>
        public string TierName { get; private set; } = string.Empty;

        /// <summary>档位枚举。</summary>
        public ContextTier Tier { get; private set; }

        /// <summary>是否切片生成（32K 以下为 true）。</summary>
        public bool UseSlicing { get; private set; }

        /// <summary>单片生成 token 预算（不切片档 = 整章 token 预算）。</summary>
        public int SliceMaxTokens { get; private set; }

        /// <summary>每章最大切片数（不切片档 = 1）。</summary>
        public int MaxSlicesPerChapter { get; private set; }

        /// <summary>全书大纲注入字符上限（8K/16K 小批量参考）。</summary>
        public int OutlineTruncateChars { get; private set; }

        /// <summary>前情回顾注入字符上限（0 = 不注入）。</summary>
        public int RecapTruncateChars { get; private set; }

        /// <summary>前置生成与注入的主要角色数量上限。</summary>
        public int MaxCastCount { get; private set; }

        /// <summary>前置生成与注入的世界设定数量上限。</summary>
        public int MaxSettingCount { get; private set; }

        /// <summary>前置生成与注入的势力数量上限。</summary>
        public int MaxFactionCount { get; private set; }

        /// <summary>
        /// 解析写作策略：显式档位配置优先（"8K"/"16K"/.../"1M"），
        /// 否则按 ContextSize 自动归档。
        /// </summary>
        public static ContextWritingPolicy Resolve(int contextSize, string? tierOverride = null)
        {
            var tier = ParseTier(tierOverride) ?? FromContextSize(contextSize);
            return FromTier(tier);
        }

        private static ContextTier FromContextSize(int contextSize)
        {
            if (contextSize <= 8 * 1024) return ContextTier.Ctx8K;
            if (contextSize <= 16 * 1024) return ContextTier.Ctx16K;
            if (contextSize <= 32 * 1024) return ContextTier.Ctx32K;
            if (contextSize <= 64 * 1024) return ContextTier.Ctx64K;
            if (contextSize <= 90 * 1024) return ContextTier.Ctx90K;
            if (contextSize <= 128 * 1024) return ContextTier.Ctx128K;
            if (contextSize <= 256 * 1024) return ContextTier.Ctx256K;
            return ContextTier.Ctx1M;
        }

        private static ContextTier? ParseTier(string? text)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Trim().Equals("Auto", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return text.Trim() switch
            {
                "8K" or "8k" => ContextTier.Ctx8K,
                "16K" or "16k" => ContextTier.Ctx16K,
                "32K" or "32k" => ContextTier.Ctx32K,
                "64K" or "64k" => ContextTier.Ctx64K,
                "90K" or "90k" => ContextTier.Ctx90K,
                "128K" or "128k" => ContextTier.Ctx128K,
                "256K" or "256k" => ContextTier.Ctx256K,
                "1M" or "1m" => ContextTier.Ctx1M,
                _ => null
            };
        }

        private static ContextWritingPolicy FromTier(ContextTier tier)
        {
            // 32K 以下切片：上下文越短单片越小、片数越多；辅助信息小批量（8K/16K 仅写作+少量参考）。
            // 32K 及以上不切片：单片即整章，按档位放宽辅助信息注入与前置生成数量。
            return tier switch
            {
                ContextTier.Ctx8K => new ContextWritingPolicy
                {
                    Tier = tier, TierName = "8K", UseSlicing = true,
                    SliceMaxTokens = 600, MaxSlicesPerChapter = 16,
                    OutlineTruncateChars = 400, RecapTruncateChars = 0,
                    MaxCastCount = 3, MaxSettingCount = 2, MaxFactionCount = 1
                },
                ContextTier.Ctx16K => new ContextWritingPolicy
                {
                    Tier = tier, TierName = "16K", UseSlicing = true,
                    SliceMaxTokens = 1100, MaxSlicesPerChapter = 12,
                    OutlineTruncateChars = 1100, RecapTruncateChars = 900,
                    MaxCastCount = 4, MaxSettingCount = 3, MaxFactionCount = 2
                },
                ContextTier.Ctx32K => new ContextWritingPolicy
                {
                    Tier = tier, TierName = "32K", UseSlicing = false,
                    SliceMaxTokens = 6000, MaxSlicesPerChapter = 1,
                    OutlineTruncateChars = 2200, RecapTruncateChars = 1800,
                    MaxCastCount = 6, MaxSettingCount = 5, MaxFactionCount = 3
                },
                ContextTier.Ctx64K => new ContextWritingPolicy
                {
                    Tier = tier, TierName = "64K", UseSlicing = false,
                    SliceMaxTokens = 6000, MaxSlicesPerChapter = 1,
                    OutlineTruncateChars = 3200, RecapTruncateChars = 2600,
                    MaxCastCount = 8, MaxSettingCount = 6, MaxFactionCount = 4
                },
                ContextTier.Ctx90K => new ContextWritingPolicy
                {
                    Tier = tier, TierName = "90K", UseSlicing = false,
                    SliceMaxTokens = 6000, MaxSlicesPerChapter = 1,
                    OutlineTruncateChars = 4200, RecapTruncateChars = 3400,
                    MaxCastCount = 10, MaxSettingCount = 8, MaxFactionCount = 5
                },
                ContextTier.Ctx128K => new ContextWritingPolicy
                {
                    Tier = tier, TierName = "128K", UseSlicing = false,
                    SliceMaxTokens = 6000, MaxSlicesPerChapter = 1,
                    OutlineTruncateChars = 5200, RecapTruncateChars = 4200,
                    MaxCastCount = 12, MaxSettingCount = 10, MaxFactionCount = 6
                },
                ContextTier.Ctx256K => new ContextWritingPolicy
                {
                    Tier = tier, TierName = "256K", UseSlicing = false,
                    SliceMaxTokens = 6000, MaxSlicesPerChapter = 1,
                    OutlineTruncateChars = 6400, RecapTruncateChars = 5000,
                    MaxCastCount = 16, MaxSettingCount = 12, MaxFactionCount = 8
                },
                _ => new ContextWritingPolicy
                {
                    Tier = ContextTier.Ctx1M, TierName = "1M", UseSlicing = false,
                    SliceMaxTokens = 6000, MaxSlicesPerChapter = 1,
                    OutlineTruncateChars = 8000, RecapTruncateChars = 6000,
                    MaxCastCount = 24, MaxSettingCount = 16, MaxFactionCount = 10
                }
            };
        }
    }
}
