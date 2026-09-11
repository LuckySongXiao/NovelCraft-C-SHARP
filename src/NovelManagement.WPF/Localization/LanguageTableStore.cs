using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace NovelManagement.WPF.Localization
{
    /// <summary>
    /// 语种切换表：以 CSV 文件承载「键 → 各语种文本」的映射。
    /// 第一列为键，第二列为基础语种（中文），第三列为英文，
    /// 后续列由二次开发者按需追加（表头即语种代码，如 de / fr / ja）。
    /// 加载顺序：应用目录 Localization/language-table.csv → 用户配置目录 language-table.csv（后者逐键覆盖前者）。
    /// 运行时取词优先级：语种表（当前语种列）→ 内置词条表 → 中文回退 → fallback。
    /// </summary>
    public static class LanguageTableStore
    {
        private static Dictionary<string, Dictionary<string, string>>? _table;
        private static List<string> _languages = new() { "zh", "en" };

        /// <summary>表中登记的全部语种代码（含表头追加列），按列顺序。</summary>
        public static IReadOnlyList<string> Languages => _languages;

        /// <summary>语种表是否已加载。</summary>
        public static bool IsLoaded => _table != null;

        /// <summary>
        /// 加载语种表。appDir：应用目录（随包分发的基础表）；userDir：用户配置目录（二次开发者/用户覆盖表）。
        /// </summary>
        public static void Load(string? appDir, string? userDir)
        {
            var merged = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
            var languages = new List<string> { "zh", "en" };

            foreach (var (dir, _) in new[] { (appDir, "app"), (userDir, "user") })
            {
                if (string.IsNullOrWhiteSpace(dir))
                {
                    continue;
                }

                var path = Path.Combine(dir, "Localization", "language-table.csv");
                if (!File.Exists(path))
                {
                    path = Path.Combine(dir, "language-table.csv");
                }

                if (!File.Exists(path))
                {
                    continue;
                }

                try
                {
                    foreach (var row in ReadCsvRows(path))
                    {
                        if (row.Count == 0)
                        {
                            continue;
                        }

                        // 表头：key,zh,en,...（以 key 列后的列名作为语种代码）
                        if (row[0].Trim().Equals("key", StringComparison.OrdinalIgnoreCase))
                        {
                            languages = new List<string>();
                            for (var c = 1; c < row.Count; c++)
                            {
                                var lang = row[c].Trim();
                                if (!string.IsNullOrEmpty(lang))
                                {
                                    languages.Add(lang);
                                }
                            }

                            continue;
                        }

                        var key = row[0].Trim();
                        if (string.IsNullOrEmpty(key))
                        {
                            continue;
                        }

                        var values = merged.TryGetValue(key, out var existing)
                            ? existing
                            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        for (var c = 1; c < row.Count && c - 1 < languages.Count; c++)
                        {
                            var lang = languages[c - 1];
                            var value = c < row.Count ? row[c] : string.Empty;
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                values[lang] = value;
                            }
                        }

                        merged[key] = values;
                    }
                }
                catch
                {
                    // 语种表解析失败不应影响启动（内置词条仍可用）
                }
            }

            _table = merged;
            _languages = languages;
        }

        /// <summary>按语种代码取值（lang 如 zh/en/de；"zh-CN" 归一化为 "zh"）。</summary>
        public static bool TryGet(string key, string languageCode, out string value)
        {
            value = string.Empty;
            if (_table == null || string.IsNullOrEmpty(key))
            {
                return false;
            }

            var lang = NormalizeLanguage(languageCode);
            if (_table.TryGetValue(key, out var row) &&
                row.TryGetValue(lang, out var v) &&
                !string.IsNullOrWhiteSpace(v))
            {
                value = v;
                return true;
            }

            return false;
        }

        /// <summary>"zh-CN" → "zh"。</summary>
        public static string NormalizeLanguage(string? languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
            {
                return "zh";
            }

            var code = languageCode.Trim();
            var dash = code.IndexOf('-');
            return dash > 0 ? code[..dash] : code;
        }

        /// <summary>极简 CSV 解析：支持引号包裹、双引号转义、跨行值。</summary>
        private static IEnumerable<List<string>> ReadCsvRows(string path)
        {
            using var reader = new StreamReader(path, Encoding.UTF8);
            var row = new List<string>();
            var cell = new StringBuilder();
            var inQuotes = false;

            int ch;
            while ((ch = reader.Read()) != -1)
            {
                if (inQuotes)
                {
                    if (ch == '"')
                    {
                        if (reader.Peek() == '"')
                        {
                            reader.Read();
                            cell.Append('"');
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        cell.Append((char)ch);
                    }

                    continue;
                }

                if (ch == '"')
                {
                    inQuotes = true;
                }
                else if (ch == ',')
                {
                    row.Add(cell.ToString());
                    cell.Clear();
                }
                else if (ch == '\n' || ch == '\r')
                {
                    if (ch == '\r' && reader.Peek() == '\n')
                    {
                        reader.Read();
                    }

                    row.Add(cell.ToString());
                    cell.Clear();
                    if (row.Count > 1 || !string.IsNullOrEmpty(row[0]))
                    {
                        yield return row;
                    }

                    row = new List<string>();
                }
                else
                {
                    cell.Append((char)ch);
                }
            }

            if (cell.Length > 0 || row.Count > 0)
            {
                row.Add(cell.ToString());
                if (row.Count > 1 || !string.IsNullOrEmpty(row[0]))
                {
                    yield return row;
                }
            }
        }
    }
}
