using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NovelManagement.AI.Interfaces;

namespace NovelManagement.AI.Memory
{
    /// <summary>
    /// 记忆压缩引擎实现
    /// </summary>
    public class CompressionEngine : ICompressionEngine
    {
        private readonly ILogger<CompressionEngine> _logger;
        private readonly CompressionConfig _config;
        private readonly ImportanceFactors _importanceFactors;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="config">压缩配置</param>
        public CompressionEngine(ILogger<CompressionEngine> logger, CompressionConfig? config = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? new CompressionConfig();
            _importanceFactors = new ImportanceFactors();
        }

        /// <inheritdoc/>
        public async Task<int> EvaluateImportanceAsync(string content, MemoryContext context)
        {
            try
            {
                _logger.LogDebug($"评估内容重要性，内容长度: {content.Length}");

                var score = 0.0;

                // 1. 内容长度因子
                var lengthScore = Math.Min(content.Length / 1000.0, 1.0) * 10;
                score += lengthScore * _importanceFactors.ContentLengthWeight;

                // 2. 关键词密度因子
                var keywords = await ExtractKeywordsAsync(content, 20);
                var keywordDensity = keywords.Count / Math.Max(content.Split(' ').Length / 100.0, 1.0);
                var keywordScore = Math.Min(keywordDensity, 10.0);
                score += keywordScore * _importanceFactors.KeywordDensityWeight;

                // 3. 实体关联因子
                var entityScore = await EvaluateEntityRelationsAsync(content, context);
                score += entityScore * _importanceFactors.EntityRelationWeight;

                // 4. 时间新鲜度因子（假设新内容更重要）
                var timeScore = 10.0; // 新内容默认高分
                score += timeScore * _importanceFactors.TimeRecencyWeight;

                // 5. 特殊内容类型加权
                score += await EvaluateContentTypeImportanceAsync(content);

                // 确保评分在1-10范围内
                var finalScore = Math.Max(1, Math.Min(10, (int)Math.Round(score)));

                _logger.LogDebug($"重要性评估完成，评分: {finalScore}");
                return finalScore;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "评估内容重要性失败");
                return 5; // 默认中等重要性
            }
        }

        /// <inheritdoc/>
        public async Task<List<MemoryItem>> CompressLowImportanceAsync(List<MemoryItem> memoryItems, int compressionThreshold = 5)
        {
            try
            {
                _logger.LogInformation($"开始压缩低重要性记忆项，阈值: {compressionThreshold}");

                var compressedItems = new List<MemoryItem>();

                foreach (var item in memoryItems)
                {
                    if (item.ImportanceScore < compressionThreshold && !item.IsCompressed)
                    {
                        // 压缩内容
                        var compressedContent = await GenerateSummaryAsync(item.Content, _config.SummaryMaxLength);
                        
                        var compressedItem = new MemoryItem
                        {
                            Id = item.Id,
                            Content = compressedContent,
                            ImportanceScore = item.ImportanceScore,
                            Type = item.Type,
                            Scope = item.Scope,
                            ProjectId = item.ProjectId,
                            VolumeId = item.VolumeId,
                            ChapterId = item.ChapterId,
                            CreatedAt = item.CreatedAt,
                            LastAccessedAt = item.LastAccessedAt,
                            AccessCount = item.AccessCount,
                            IsCompressed = true,
                            OriginalLength = item.OriginalLength > 0 ? item.OriginalLength : item.Content.Length,
                            Tags = item.Tags,
                            RelatedEntityIds = item.RelatedEntityIds
                        };

                        compressedItems.Add(compressedItem);
                        _logger.LogDebug($"压缩记忆项 {item.Id}，原长度: {item.Content.Length}, 压缩后: {compressedContent.Length}");
                    }
                    else
                    {
                        compressedItems.Add(item);
                    }
                }

                _logger.LogInformation($"压缩完成，处理了 {memoryItems.Count} 个记忆项");
                return compressedItems;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "压缩低重要性记忆项失败");
                return memoryItems;
            }
        }

        /// <inheritdoc/>
        public async Task<List<MemoryItem>> OptimizeRetrievalAsync(string query, List<MemoryItem> memoryItems, int maxResults = 10)
        {
            try
            {
                _logger.LogDebug($"优化记忆检索，查询: {query}, 候选项: {memoryItems.Count}");

                var scoredItems = new List<(MemoryItem item, double score)>();

                foreach (var item in memoryItems)
                {
                    var relevanceScore = await CalculateRelevanceScoreAsync(query, item);
                    scoredItems.Add((item, relevanceScore));
                }

                // 按相关性和重要性排序
                var results = scoredItems
                    .OrderByDescending(x => x.score * x.item.ImportanceScore)
                    .Take(maxResults)
                    .Select(x => x.item)
                    .ToList();

                _logger.LogDebug($"检索优化完成，返回 {results.Count} 个结果");
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "优化记忆检索失败");
                return memoryItems.Take(maxResults).ToList();
            }
        }

        /// <inheritdoc/>
        public async Task<string> GenerateSummaryAsync(string content, int maxLength = 200)
        {
            try
            {
                if (content.Length <= maxLength)
                {
                    return content;
                }

                // 简单的摘要生成算法
                var sentences = content.Split(new[] { '.', '!', '?', '。', '！', '？' }, StringSplitOptions.RemoveEmptyEntries);
                
                if (sentences.Length <= 1)
                {
                    return content.Length > maxLength ? content.Substring(0, maxLength) + "..." : content;
                }

                // 选择最重要的句子
                var importantSentences = new List<string>();
                var currentLength = 0;

                // 优先选择包含关键词的句子
                var keywords = await ExtractKeywordsAsync(content, 10);
                var keywordPattern = string.Join("|", keywords.Select(Regex.Escape));

                foreach (var sentence in sentences.OrderByDescending(s => CountKeywordMatches(s, keywordPattern)))
                {
                    var sentenceLength = sentence.Length + 1; // +1 for punctuation
                    if (currentLength + sentenceLength <= maxLength)
                    {
                        importantSentences.Add(sentence.Trim());
                        currentLength += sentenceLength;
                    }
                    else
                    {
                        break;
                    }
                }

                var summary = string.Join("。", importantSentences);
                if (summary.Length < content.Length)
                {
                    summary += "...";
                }

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成摘要失败");
                return content.Length > maxLength ? content.Substring(0, maxLength) + "..." : content;
            }
        }

        /// <inheritdoc/>
        public async Task<List<MemoryItem>> MergeSimilarMemoriesAsync(List<MemoryItem> memoryItems, double similarityThreshold = 0.8)
        {
            try
            {
                _logger.LogDebug($"开始合并相似记忆项，阈值: {similarityThreshold}");

                var mergedItems = new List<MemoryItem>();
                var processed = new HashSet<string>();

                foreach (var item in memoryItems)
                {
                    if (processed.Contains(item.Id))
                        continue;

                    var similarItems = new List<MemoryItem> { item };
                    processed.Add(item.Id);

                    // 查找相似项
                    foreach (var otherItem in memoryItems)
                    {
                        if (processed.Contains(otherItem.Id))
                            continue;

                        var similarity = await CalculateSimilarityAsync(item.Content, otherItem.Content);
                        if (similarity >= similarityThreshold)
                        {
                            similarItems.Add(otherItem);
                            processed.Add(otherItem.Id);
                        }
                    }

                    if (similarItems.Count > 1)
                    {
                        // 合并相似项
                        var mergedItem = await MergeMemoryItemsAsync(similarItems);
                        mergedItems.Add(mergedItem);
                        _logger.LogDebug($"合并了 {similarItems.Count} 个相似记忆项");
                    }
                    else
                    {
                        mergedItems.Add(item);
                    }
                }

                _logger.LogDebug($"合并完成，原始 {memoryItems.Count} 项，合并后 {mergedItems.Count} 项");
                return mergedItems;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "合并相似记忆项失败");
                return memoryItems;
            }
        }

        /// <inheritdoc/>
        public Task<double> CalculateSimilarityAsync(string content1, string content2)
        {
            try
            {
                if (string.IsNullOrEmpty(content1) || string.IsNullOrEmpty(content2))
                    return Task.FromResult(0.0);

                // 简单的相似度计算算法（基于词汇重叠）
                var words1 = ExtractWords(content1);
                var words2 = ExtractWords(content2);

                var intersection = words1.Intersect(words2).Count();
                var union = words1.Union(words2).Count();

                return Task.FromResult(union > 0 ? (double)intersection / union : 0.0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "计算相似度失败");
                return Task.FromResult(0.0);
            }
        }

        /// <inheritdoc/>
        public Task<List<string>> ExtractKeywordsAsync(string content, int maxKeywords = 10)
        {
            try
            {
                if (string.IsNullOrEmpty(content))
                    return Task.FromResult(new List<string>());

                // 简单的关键词提取算法
                var words = ExtractWords(content);
                
                // 过滤停用词
                var stopWords = GetStopWords();
                var filteredWords = words.Where(w => !stopWords.Contains(w.ToLower())).ToList();

                // 计算词频
                var wordFreq = filteredWords
                    .GroupBy(w => w.ToLower())
                    .ToDictionary(g => g.Key, g => g.Count());

                // 按频率排序并返回前N个
                var keywords = wordFreq
                    .OrderByDescending(kvp => kvp.Value)
                    .Take(maxKeywords)
                    .Select(kvp => kvp.Key)
                    .ToList();

                return Task.FromResult(keywords);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "提取关键词失败");
                return Task.FromResult(new List<string>());
            }
        }

        /// <inheritdoc/>
        public Task<MemoryType> AnalyzeContentTypeAsync(string content)
        {
            try
            {
                if (string.IsNullOrEmpty(content))
                    return Task.FromResult(MemoryType.Other);

                var lowerContent = content.ToLower();

                // 基于关键词的简单内容类型分析
                if (ContainsKeywords(lowerContent, new[] { "角色", "人物", "性格", "外貌", "能力" }))
                    return Task.FromResult(MemoryType.Character);

                if (ContainsKeywords(lowerContent, new[] { "剧情", "情节", "故事", "发展", "转折" }))
                    return Task.FromResult(MemoryType.Plot);

                if (ContainsKeywords(lowerContent, new[] { "对话", "说道", "回答", "询问", "交谈" }))
                    return Task.FromResult(MemoryType.Dialogue);

                if (ContainsKeywords(lowerContent, new[] { "场景", "环境", "地点", "描述", "景色" }))
                    return Task.FromResult(MemoryType.Scene);

                if (ContainsKeywords(lowerContent, new[] { "世界", "设定", "规则", "体系", "背景" }))
                    return Task.FromResult(MemoryType.WorldSetting);

                if (ContainsKeywords(lowerContent, new[] { "关系", "联系", "关联", "影响", "互动" }))
                    return Task.FromResult(MemoryType.Relationship);

                if (ContainsKeywords(lowerContent, new[] { "事件", "发生", "经历", "过程", "结果" }))
                    return Task.FromResult(MemoryType.Event);

                return Task.FromResult(MemoryType.Other);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分析内容类型失败");
                return Task.FromResult(MemoryType.Other);
            }
        }

        #region 私有方法

        /// <summary>
        /// 评估实体关联重要性
        /// </summary>
        private async Task<double> EvaluateEntityRelationsAsync(string content, MemoryContext context)
        {
            // 简化实现：基于上下文中相关记忆项的数量
            var relatedCount = context.RelevantMemories.Count;
            return Math.Min(relatedCount / 10.0 * 10, 10.0);
        }

        /// <summary>
        /// 评估内容类型重要性
        /// </summary>
        private async Task<double> EvaluateContentTypeImportanceAsync(string content)
        {
            var contentType = await AnalyzeContentTypeAsync(content);
            
            return contentType switch
            {
                MemoryType.WorldSetting => 3.0,
                MemoryType.Character => 2.5,
                MemoryType.Plot => 2.0,
                MemoryType.Relationship => 1.5,
                MemoryType.Event => 1.0,
                MemoryType.Scene => 0.5,
                MemoryType.Dialogue => 0.3,
                _ => 0.0
            };
        }

        /// <summary>
        /// 计算相关性评分
        /// </summary>
        private async Task<double> CalculateRelevanceScoreAsync(string query, MemoryItem item)
        {
            var similarity = await CalculateSimilarityAsync(query, item.Content);
            var tagMatch = item.Tags.Any(tag => query.ToLower().Contains(tag.ToLower())) ? 0.5 : 0.0;
            var typeMatch = query.ToLower().Contains(item.Type.ToString().ToLower()) ? 0.3 : 0.0;
            
            return similarity + tagMatch + typeMatch;
        }

        /// <summary>
        /// 合并记忆项
        /// </summary>
        private async Task<MemoryItem> MergeMemoryItemsAsync(List<MemoryItem> items)
        {
            var primaryItem = items.OrderByDescending(i => i.ImportanceScore).First();
            var combinedContent = string.Join("\n---\n", items.Select(i => i.Content));
            var mergedContent = await GenerateSummaryAsync(combinedContent, _config.SummaryMaxLength * 2);

            return new MemoryItem
            {
                Id = primaryItem.Id,
                Content = mergedContent,
                ImportanceScore = (int)items.Average(i => i.ImportanceScore),
                Type = primaryItem.Type,
                Scope = primaryItem.Scope,
                ProjectId = primaryItem.ProjectId,
                VolumeId = primaryItem.VolumeId,
                ChapterId = primaryItem.ChapterId,
                CreatedAt = items.Min(i => i.CreatedAt),
                LastAccessedAt = items.Max(i => i.LastAccessedAt),
                AccessCount = items.Sum(i => i.AccessCount),
                IsCompressed = true,
                OriginalLength = items.Sum(i => i.OriginalLength),
                Tags = items.SelectMany(i => i.Tags).Distinct().ToList(),
                RelatedEntityIds = items.SelectMany(i => i.RelatedEntityIds).Distinct().ToList()
            };
        }

        /// <summary>
        /// 提取单词
        /// </summary>
        private List<string> ExtractWords(string content)
        {
            return Regex.Matches(content, @"\b\w+\b")
                .Cast<Match>()
                .Select(m => m.Value)
                .Where(w => w.Length > 1)
                .ToList();
        }

        /// <summary>
        /// 获取停用词列表
        /// </summary>
        private HashSet<string> GetStopWords()
        {
            return new HashSet<string>
            {
                "的", "了", "在", "是", "我", "有", "和", "就", "不", "人", "都", "一", "一个", "上", "也", "很", "到", "说", "要", "去", "你", "会", "着", "没有", "看", "好", "自己", "这"
            };
        }

        /// <summary>
        /// 检查是否包含关键词
        /// </summary>
        private bool ContainsKeywords(string content, string[] keywords)
        {
            return keywords.Any(keyword => content.Contains(keyword));
        }

        /// <summary>
        /// 计算关键词匹配数量
        /// </summary>
        private int CountKeywordMatches(string sentence, string keywordPattern)
        {
            if (string.IsNullOrEmpty(keywordPattern))
                return 0;

            return Regex.Matches(sentence, keywordPattern, RegexOptions.IgnoreCase).Count;
        }

        #endregion
    }
}
