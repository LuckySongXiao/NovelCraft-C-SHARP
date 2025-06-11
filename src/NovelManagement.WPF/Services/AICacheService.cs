using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NovelManagement.WPF.Services
{
    /// <summary>
    /// AI缓存服务
    /// </summary>
    public class AICacheService
    {
        #region 字段和属性

        private readonly ILogger<AICacheService>? _logger;
        private readonly ConcurrentDictionary<string, CacheItem> _cache;
        private readonly TimeSpan _defaultExpiration;

        /// <summary>
        /// 缓存项
        /// </summary>
        private class CacheItem
        {
            public object Data { get; set; } = null!;
            public DateTime ExpirationTime { get; set; }
            public int AccessCount { get; set; }
            public DateTime LastAccessTime { get; set; }
        }

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="defaultExpirationMinutes">默认过期时间（分钟）</param>
        public AICacheService(ILogger<AICacheService>? logger = null, int defaultExpirationMinutes = 30)
        {
            _logger = logger;
            _cache = new ConcurrentDictionary<string, CacheItem>();
            _defaultExpiration = TimeSpan.FromMinutes(defaultExpirationMinutes);
            
            // 启动清理任务
            _ = Task.Run(CleanupExpiredItemsAsync);
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 获取缓存项
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <returns>缓存的数据，如果不存在或已过期则返回null</returns>
        public T? Get<T>(string key) where T : class
        {
            try
            {
                if (_cache.TryGetValue(key, out var item))
                {
                    if (DateTime.Now <= item.ExpirationTime)
                    {
                        // 更新访问信息
                        item.AccessCount++;
                        item.LastAccessTime = DateTime.Now;
                        
                        _logger?.LogDebug("缓存命中: {Key}, 访问次数: {AccessCount}", key, item.AccessCount);
                        return item.Data as T;
                    }
                    else
                    {
                        // 缓存已过期，移除
                        _cache.TryRemove(key, out _);
                        _logger?.LogDebug("缓存过期并移除: {Key}", key);
                    }
                }
                
                _logger?.LogDebug("缓存未命中: {Key}", key);
                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取缓存失败: {Key}", key);
                return null;
            }
        }

        /// <summary>
        /// 设置缓存项
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <param name="data">要缓存的数据</param>
        /// <param name="expiration">过期时间，如果为null则使用默认过期时间</param>
        public void Set(string key, object data, TimeSpan? expiration = null)
        {
            try
            {
                var expirationTime = DateTime.Now.Add(expiration ?? _defaultExpiration);
                
                var cacheItem = new CacheItem
                {
                    Data = data,
                    ExpirationTime = expirationTime,
                    AccessCount = 0,
                    LastAccessTime = DateTime.Now
                };

                _cache.AddOrUpdate(key, cacheItem, (k, v) => cacheItem);
                
                _logger?.LogDebug("缓存设置: {Key}, 过期时间: {ExpirationTime}", key, expirationTime);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "设置缓存失败: {Key}", key);
            }
        }

        /// <summary>
        /// 移除缓存项
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <returns>是否成功移除</returns>
        public bool Remove(string key)
        {
            try
            {
                var removed = _cache.TryRemove(key, out _);
                if (removed)
                {
                    _logger?.LogDebug("缓存移除: {Key}", key);
                }
                return removed;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "移除缓存失败: {Key}", key);
                return false;
            }
        }

        /// <summary>
        /// 清空所有缓存
        /// </summary>
        public void Clear()
        {
            try
            {
                var count = _cache.Count;
                _cache.Clear();
                _logger?.LogInformation("清空所有缓存，共移除 {Count} 项", count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "清空缓存失败");
            }
        }

        /// <summary>
        /// 获取或设置缓存项
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="factory">数据工厂函数</param>
        /// <param name="expiration">过期时间</param>
        /// <returns>缓存的数据</returns>
        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null) where T : class
        {
            try
            {
                // 先尝试从缓存获取
                var cached = Get<T>(key);
                if (cached != null)
                {
                    return cached;
                }

                // 缓存未命中，执行工厂函数
                _logger?.LogDebug("缓存未命中，执行工厂函数: {Key}", key);
                var data = await factory();
                
                // 将结果存入缓存
                if (data != null)
                {
                    Set(key, data, expiration);
                }

                return data;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取或设置缓存失败: {Key}", key);
                throw;
            }
        }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        /// <returns>缓存统计</returns>
        public CacheStatistics GetStatistics()
        {
            try
            {
                var totalItems = _cache.Count;
                var totalAccess = 0;
                var expiredItems = 0;
                var now = DateTime.Now;

                foreach (var item in _cache.Values)
                {
                    totalAccess += item.AccessCount;
                    if (now > item.ExpirationTime)
                    {
                        expiredItems++;
                    }
                }

                return new CacheStatistics
                {
                    TotalItems = totalItems,
                    ExpiredItems = expiredItems,
                    TotalAccess = totalAccess,
                    HitRate = totalAccess > 0 ? (double)(totalAccess - expiredItems) / totalAccess : 0
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取缓存统计失败");
                return new CacheStatistics();
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 清理过期项的后台任务
        /// </summary>
        private async Task CleanupExpiredItemsAsync()
        {
            while (true)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(5)); // 每5分钟清理一次

                    var now = DateTime.Now;
                    var expiredKeys = new List<string>();

                    foreach (var kvp in _cache)
                    {
                        if (now > kvp.Value.ExpirationTime)
                        {
                            expiredKeys.Add(kvp.Key);
                        }
                    }

                    foreach (var key in expiredKeys)
                    {
                        _cache.TryRemove(key, out _);
                    }

                    if (expiredKeys.Count > 0)
                    {
                        _logger?.LogDebug("清理过期缓存项: {Count} 项", expiredKeys.Count);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "清理过期缓存项失败");
                }
            }
        }

        #endregion

        #region 嵌套类

        /// <summary>
        /// 缓存统计信息
        /// </summary>
        public class CacheStatistics
        {
            public int TotalItems { get; set; }
            public int ExpiredItems { get; set; }
            public int TotalAccess { get; set; }
            public double HitRate { get; set; }
        }

        #endregion
    }

    /// <summary>
    /// AI缓存键生成器
    /// </summary>
    public static class AICacheKeyGenerator
    {
        /// <summary>
        /// 生成对话缓存键
        /// </summary>
        public static string GenerateDialogueKey(string characters, string situation, string emotion, string style)
        {
            return $"dialogue:{characters}:{situation}:{emotion}:{style}".GetHashCode().ToString();
        }

        /// <summary>
        /// 生成世界设定分析缓存键
        /// </summary>
        public static string GenerateWorldSettingAnalysisKey(string settingId, string settingContent)
        {
            return $"worldsetting_analysis:{settingId}:{settingContent.GetHashCode()}";
        }

        /// <summary>
        /// 生成势力分析缓存键
        /// </summary>
        public static string GenerateFactionAnalysisKey(string factionId, string factionData)
        {
            return $"faction_analysis:{factionId}:{factionData.GetHashCode()}";
        }
    }
}
