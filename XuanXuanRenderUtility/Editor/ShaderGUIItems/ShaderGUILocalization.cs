using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    public static class ShaderGUILocalization
    {
        public const string DefaultLanguage = "zh-CN";
        public const string EnglishLanguage = "en-US";

        private const string TooltipColumnSuffix = "-tip";
        private const string TooltipColumnAliasSuffix = "-tooltip";

        private static readonly Dictionary<string, LocalizationTable> Tables =
            new Dictionary<string, LocalizationTable>(StringComparer.OrdinalIgnoreCase);
        private static NBFXLanguageMode s_CachedLanguageMode;
        private static string s_CachedPreferredLanguage;
        private static bool s_CachedPreferredLanguageValid;

        public static void RegisterCsv(string tableName, string csvAssetPath)
        {
            tableName = NormalizeTableName(tableName);
            if (Tables.TryGetValue(tableName, out LocalizationTable table))
            {
                if (string.Equals(table.CsvAssetPath, csvAssetPath, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                table.CsvAssetPath = csvAssetPath;
                table.Reload();
                return;
            }

            Tables[tableName] = new LocalizationTable(csvAssetPath);
        }

        public static string GetCurrentLanguage(string tableName)
        {
            LocalizationTable table = GetTable(tableName);
            table.EnsureLoaded();
            return GetCurrentLanguage(table);
        }

        public static string GetEditorLanguageName()
        {
            return GetLanguageFromEditor();
        }

        private static string GetCurrentLanguage(LocalizationTable table)
        {
            string preferred = GetPreferredLanguageName();
            foreach (string language in table.Languages)
            {
                if (string.Equals(language, preferred, StringComparison.OrdinalIgnoreCase))
                {
                    return language;
                }
            }

            return DefaultLanguage;
        }

        public static GUIContent MakeContent(
            string tableName,
            string labelKey,
            string labelFallback,
            string tooltipKey = null,
            string tooltipFallback = "")
        {
            LocalizationTable table = GetTable(tableName);
            table.EnsureLoaded();
            string language = GetCurrentLanguage(table);
            var cacheKey = new ContentCacheKey(language, labelKey, labelFallback, tooltipKey, tooltipFallback);
            if (table.ContentCache.TryGetValue(cacheKey, out GUIContent cachedContent))
            {
                return cachedContent;
            }

            var content = new GUIContent(
                GetLocalizedValue(table, labelKey, labelFallback, language),
                GetTooltip(table, labelKey, tooltipKey, tooltipFallback, language));
            table.ContentCache[cacheKey] = content;
            return content;
        }

        public static GUIContent MakeInspectorContent(string tableName, string key, string fallback, string tip = "")
        {
            LocalizationTable table = GetTable(tableName);
            table.EnsureLoaded();
            string language = GetCurrentLanguage(table);
            var cacheKey = new InspectorContentCacheKey(language, key, fallback, tip);
            if (table.InspectorContentCache.TryGetValue(cacheKey, out GUIContent cachedContent))
            {
                return cachedContent;
            }

            string labelKey = "inspector." + key + ".label";
            string tooltipKey = "inspector." + key + ".tip";
            var content = new GUIContent(
                GetLocalizedValue(table, labelKey, fallback, language),
                GetTooltip(table, labelKey, tooltipKey, tip, language));
            table.InspectorContentCache[cacheKey] = content;
            return content;
        }

        public static string GetInspectorText(string tableName, string key, string fallback = "")
        {
            LocalizationTable table = GetTable(tableName);
            table.EnsureLoaded();
            string language = GetCurrentLanguage(table);
            return GetInspectorText(table, key, fallback, language);
        }

        public static string GetInspectorText(string tableName, string key, string fallback, string language)
        {
            LocalizationTable table = GetTable(tableName);
            table.EnsureLoaded();
            return GetInspectorText(table, key, fallback, language);
        }

        private static string GetInspectorText(LocalizationTable table, string key, string fallback, string language)
        {
            var cacheKey = new InspectorTextCacheKey(language, key, fallback);
            if (table.InspectorTextCache.TryGetValue(cacheKey, out string cachedText))
            {
                return cachedText;
            }

            cachedText = GetLocalizedValue(table, "inspector." + key, fallback, language);
            table.InspectorTextCache[cacheKey] = cachedText;
            return cachedText;
        }

        public static string[] GetInspectorOptions(string tableName, string key, string[] fallback)
        {
            LocalizationTable table = GetTable(tableName);
            table.EnsureLoaded();
            string language = GetCurrentLanguage(table);
            return GetInspectorOptions(table, key, fallback, language);
        }

        public static string[] GetInspectorOptions(string tableName, string key, string[] fallback, string language)
        {
            LocalizationTable table = GetTable(tableName);
            table.EnsureLoaded();
            return GetInspectorOptions(table, key, fallback, language);
        }

        private static string[] GetInspectorOptions(LocalizationTable table, string key, string[] fallback, string language)
        {
            if (fallback == null)
            {
                return Array.Empty<string>();
            }

            var cacheKey = new OptionsCacheKey(language, key, fallback);
            if (table.InspectorOptionsCache.TryGetValue(cacheKey, out string[] cachedValues))
            {
                return cachedValues;
            }

            string keyPrefix = "inspector." + key + ".option";
            string[] values = new string[fallback.Length];
            for (int i = 0; i < fallback.Length; i++)
            {
                values[i] = GetLocalizedValue(table, keyPrefix + "." + i, fallback[i], language);
            }

            table.InspectorOptionsCache[cacheKey] = values;
            return values;
        }

        public static string[] GetOptions(string tableName, string keyPrefix, string[] fallback)
        {
            if (fallback == null)
            {
                return Array.Empty<string>();
            }

            LocalizationTable table = GetTable(tableName);
            table.EnsureLoaded();
            string language = GetCurrentLanguage(table);
            var cacheKey = new OptionsCacheKey(language, keyPrefix, fallback);
            if (table.OptionsCache.TryGetValue(cacheKey, out string[] cachedValues))
            {
                return cachedValues;
            }

            string[] values = new string[fallback.Length];
            for (int i = 0; i < fallback.Length; i++)
            {
                values[i] = GetLocalizedValue(table, keyPrefix + "." + i, fallback[i], language);
            }

            table.OptionsCache[cacheKey] = values;
            return values;
        }

        public static string Get(string tableName, string key, string fallback = "")
        {
            LocalizationTable table = GetTable(tableName);
            table.EnsureLoaded();
            return GetLocalizedValue(table, key, fallback, GetCurrentLanguage(table));
        }

        private static string GetLocalizedValue(LocalizationTable table, string key, string fallback, string language)
        {
            if (string.IsNullOrEmpty(key))
            {
                return fallback;
            }

            if (table.Rows.TryGetValue(key, out Dictionary<string, string> row))
            {
                if (row.TryGetValue(language, out string localizedValue) && !string.IsNullOrEmpty(localizedValue))
                {
                    return localizedValue;
                }

                if (row.TryGetValue(DefaultLanguage, out string defaultValue) && !string.IsNullOrEmpty(defaultValue))
                {
                    return defaultValue;
                }
            }

            return fallback;
        }

        public static string GetTooltip(string tableName, string labelKey, string tooltipKey = null, string fallback = "")
        {
            LocalizationTable table = GetTable(tableName);
            table.EnsureLoaded();
            return GetTooltip(table, labelKey, tooltipKey, fallback, GetCurrentLanguage(table));
        }

        private static string GetTooltip(
            LocalizationTable table,
            string labelKey,
            string tooltipKey,
            string fallback,
            string language)
        {
            if (!string.IsNullOrEmpty(labelKey) &&
                table.Rows.TryGetValue(labelKey, out Dictionary<string, string> row))
            {
                if (TryGetTooltip(row, language, out string localizedTooltip))
                {
                    return localizedTooltip;
                }

                if (TryGetTooltip(row, DefaultLanguage, out string defaultTooltip))
                {
                    return defaultTooltip;
                }
            }

            if (!string.IsNullOrEmpty(tooltipKey))
            {
                return GetLocalizedValue(table, tooltipKey, fallback, language);
            }

            return fallback;
        }

        public static void Reload(string tableName = null)
        {
            if (string.IsNullOrEmpty(tableName))
            {
                foreach (LocalizationTable table in Tables.Values)
                {
                    table.Reload();
                }

                InvalidatePreferredLanguageCache();
                return;
            }

            GetTable(tableName).Reload();
            InvalidatePreferredLanguageCache();
        }

        private static void InvalidatePreferredLanguageCache()
        {
            s_CachedPreferredLanguageValid = false;
        }

        private static LocalizationTable GetTable(string tableName)
        {
            tableName = NormalizeTableName(tableName);
            if (!Tables.TryGetValue(tableName, out LocalizationTable table))
            {
                table = new LocalizationTable(string.Empty);
                Tables[tableName] = table;
            }

            return table;
        }

        private static string NormalizeTableName(string tableName)
        {
            return string.IsNullOrEmpty(tableName) ? "default" : tableName;
        }

        private static bool IsTooltipColumn(string header)
        {
            return !string.IsNullOrEmpty(header) &&
                   (header.EndsWith(TooltipColumnSuffix, StringComparison.OrdinalIgnoreCase) ||
                    header.EndsWith(TooltipColumnAliasSuffix, StringComparison.OrdinalIgnoreCase));
        }

        private static bool TryGetTooltip(Dictionary<string, string> row, string language, out string tooltip)
        {
            tooltip = string.Empty;
            if (string.IsNullOrEmpty(language))
            {
                return false;
            }

            if (row.TryGetValue(language + TooltipColumnSuffix, out tooltip) && !string.IsNullOrEmpty(tooltip))
            {
                return true;
            }

            if (row.TryGetValue(language + TooltipColumnAliasSuffix, out tooltip) && !string.IsNullOrEmpty(tooltip))
            {
                return true;
            }

            return false;
        }

        private static string GetPreferredLanguageName()
        {
            NBFXLanguageMode languageMode = NBFXProjectSettings.LanguageMode;
            if (s_CachedPreferredLanguageValid && s_CachedLanguageMode == languageMode)
            {
                return s_CachedPreferredLanguage;
            }

            string language;
            switch (languageMode)
            {
                case NBFXLanguageMode.Chinese:
                    language = DefaultLanguage;
                    break;
                case NBFXLanguageMode.English:
                    language = EnglishLanguage;
                    break;
                case NBFXLanguageMode.FollowEditor:
                default:
                    language = GetLanguageFromEditor();
                    break;
            }

            s_CachedLanguageMode = languageMode;
            s_CachedPreferredLanguage = language;
            s_CachedPreferredLanguageValid = true;
            return language;
        }

        private static string GetLanguageFromEditor()
        {
            if (TryGetEditorLanguageName(out string language))
            {
                return language;
            }

            return GetSystemLanguageName();
        }

        private static bool TryGetEditorLanguageName(out string language)
        {
            language = string.Empty;

            Type localizationDatabaseType = typeof(Editor).Assembly.GetType("UnityEditor.LocalizationDatabase");
            if (localizationDatabaseType == null)
            {
                return false;
            }

            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            string[] memberNames =
            {
                "currentEditorLanguage",
                "CurrentEditorLanguage"
            };

            foreach (string memberName in memberNames)
            {
                PropertyInfo propertyInfo = localizationDatabaseType.GetProperty(memberName, flags);
                if (propertyInfo != null && TryGetEditorLanguageValue(() => propertyInfo.GetValue(null, null), out language))
                {
                    return true;
                }

                FieldInfo fieldInfo = localizationDatabaseType.GetField(memberName, flags);
                if (fieldInfo != null && TryGetEditorLanguageValue(() => fieldInfo.GetValue(null), out language))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetEditorLanguageValue(Func<object> valueGetter, out string language)
        {
            language = string.Empty;
            try
            {
                return TryMapLanguageValue(valueGetter(), out language);
            }
            catch
            {
                return false;
            }
        }

        private static bool TryMapLanguageValue(object value, out string language)
        {
            language = string.Empty;
            if (value == null)
            {
                return false;
            }

            if (value is SystemLanguage systemLanguage)
            {
                language = MapSystemLanguage(systemLanguage);
                return true;
            }

            string languageName = value.ToString();
            if (string.IsNullOrEmpty(languageName))
            {
                return false;
            }

            string normalizedLanguageName = languageName.Replace("_", "-").ToLowerInvariant();
            if (normalizedLanguageName.StartsWith("en", StringComparison.Ordinal) ||
                normalizedLanguageName.Contains("english"))
            {
                language = EnglishLanguage;
                return true;
            }

            if (normalizedLanguageName.StartsWith("zh", StringComparison.Ordinal) ||
                normalizedLanguageName.Contains("chinese"))
            {
                language = DefaultLanguage;
                return true;
            }

            language = DefaultLanguage;
            return true;
        }

        private static string GetSystemLanguageName()
        {
            return MapSystemLanguage(Application.systemLanguage);
        }

        private static string MapSystemLanguage(SystemLanguage systemLanguage)
        {
            switch (systemLanguage)
            {
                case SystemLanguage.English:
                    return EnglishLanguage;
                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseSimplified:
                case SystemLanguage.ChineseTraditional:
                    return DefaultLanguage;
                default:
                    return DefaultLanguage;
            }
        }

        private readonly struct ContentCacheKey : IEquatable<ContentCacheKey>
        {
            private readonly string _language;
            private readonly string _labelKey;
            private readonly string _labelFallback;
            private readonly string _tooltipKey;
            private readonly string _tooltipFallback;

            public ContentCacheKey(
                string language,
                string labelKey,
                string labelFallback,
                string tooltipKey,
                string tooltipFallback)
            {
                _language = language;
                _labelKey = labelKey;
                _labelFallback = labelFallback;
                _tooltipKey = tooltipKey;
                _tooltipFallback = tooltipFallback;
            }

            public bool Equals(ContentCacheKey other)
            {
                return StringEquals(_language, other._language) &&
                       StringEquals(_labelKey, other._labelKey) &&
                       StringEquals(_labelFallback, other._labelFallback) &&
                       StringEquals(_tooltipKey, other._tooltipKey) &&
                       StringEquals(_tooltipFallback, other._tooltipFallback);
            }

            public override bool Equals(object obj)
            {
                return obj is ContentCacheKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = 17;
                    hash = hash * 31 + StringHash(_language);
                    hash = hash * 31 + StringHash(_labelKey);
                    hash = hash * 31 + StringHash(_labelFallback);
                    hash = hash * 31 + StringHash(_tooltipKey);
                    hash = hash * 31 + StringHash(_tooltipFallback);
                    return hash;
                }
            }
        }

        private readonly struct InspectorContentCacheKey : IEquatable<InspectorContentCacheKey>
        {
            private readonly string _language;
            private readonly string _key;
            private readonly string _fallback;
            private readonly string _tip;

            public InspectorContentCacheKey(string language, string key, string fallback, string tip)
            {
                _language = language;
                _key = key;
                _fallback = fallback;
                _tip = tip;
            }

            public bool Equals(InspectorContentCacheKey other)
            {
                return StringEquals(_language, other._language) &&
                       StringEquals(_key, other._key) &&
                       StringEquals(_fallback, other._fallback) &&
                       StringEquals(_tip, other._tip);
            }

            public override bool Equals(object obj)
            {
                return obj is InspectorContentCacheKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = 17;
                    hash = hash * 31 + StringHash(_language);
                    hash = hash * 31 + StringHash(_key);
                    hash = hash * 31 + StringHash(_fallback);
                    hash = hash * 31 + StringHash(_tip);
                    return hash;
                }
            }
        }

        private readonly struct OptionsCacheKey : IEquatable<OptionsCacheKey>
        {
            private readonly string _language;
            private readonly string _keyPrefix;
            private readonly string[] _fallback;
            private readonly int _fallbackLength;

            public OptionsCacheKey(string language, string keyPrefix, string[] fallback)
            {
                _language = language;
                _keyPrefix = keyPrefix;
                _fallback = fallback;
                _fallbackLength = fallback != null ? fallback.Length : 0;
            }

            public bool Equals(OptionsCacheKey other)
            {
                if (!StringEquals(_language, other._language) ||
                    !StringEquals(_keyPrefix, other._keyPrefix) ||
                    _fallbackLength != other._fallbackLength)
                {
                    return false;
                }

                for (int i = 0; i < _fallbackLength; i++)
                {
                    if (!StringEquals(_fallback[i], other._fallback[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            public override bool Equals(object obj)
            {
                return obj is OptionsCacheKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = 17;
                    hash = hash * 31 + StringHash(_language);
                    hash = hash * 31 + StringHash(_keyPrefix);
                    hash = hash * 31 + _fallbackLength;
                    for (int i = 0; i < _fallbackLength; i++)
                    {
                        hash = hash * 31 + StringHash(_fallback[i]);
                    }

                    return hash;
                }
            }
        }

        private readonly struct InspectorTextCacheKey : IEquatable<InspectorTextCacheKey>
        {
            private readonly string _language;
            private readonly string _key;
            private readonly string _fallback;

            public InspectorTextCacheKey(string language, string key, string fallback)
            {
                _language = language;
                _key = key;
                _fallback = fallback;
            }

            public bool Equals(InspectorTextCacheKey other)
            {
                return StringEquals(_language, other._language) &&
                       StringEquals(_key, other._key) &&
                       StringEquals(_fallback, other._fallback);
            }

            public override bool Equals(object obj)
            {
                return obj is InspectorTextCacheKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = 17;
                    hash = hash * 31 + StringHash(_language);
                    hash = hash * 31 + StringHash(_key);
                    hash = hash * 31 + StringHash(_fallback);
                    return hash;
                }
            }
        }

        private static bool StringEquals(string a, string b)
        {
            return string.Equals(a, b, StringComparison.Ordinal);
        }

        private static int StringHash(string value)
        {
            return value == null ? 0 : StringComparer.Ordinal.GetHashCode(value);
        }

        private sealed class LocalizationTable
        {
            private bool _loaded;
            private bool _warningLogged;

            public LocalizationTable(string csvAssetPath)
            {
                CsvAssetPath = csvAssetPath;
                Rows = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
                Languages = Array.Empty<string>();
                ContentCache = new Dictionary<ContentCacheKey, GUIContent>();
                InspectorContentCache = new Dictionary<InspectorContentCacheKey, GUIContent>();
                InspectorTextCache = new Dictionary<InspectorTextCacheKey, string>();
                OptionsCache = new Dictionary<OptionsCacheKey, string[]>();
                InspectorOptionsCache = new Dictionary<OptionsCacheKey, string[]>();
            }

            public string CsvAssetPath { get; set; }
            public Dictionary<string, Dictionary<string, string>> Rows { get; private set; }
            public string[] Languages { get; private set; }
            public Dictionary<ContentCacheKey, GUIContent> ContentCache { get; }
            public Dictionary<InspectorContentCacheKey, GUIContent> InspectorContentCache { get; }
            public Dictionary<InspectorTextCacheKey, string> InspectorTextCache { get; }
            public Dictionary<OptionsCacheKey, string[]> OptionsCache { get; }
            public Dictionary<OptionsCacheKey, string[]> InspectorOptionsCache { get; }

            public void Reload()
            {
                _loaded = false;
                _warningLogged = false;
                Rows = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
                Languages = Array.Empty<string>();
                ContentCache.Clear();
                InspectorContentCache.Clear();
                InspectorTextCache.Clear();
                OptionsCache.Clear();
                InspectorOptionsCache.Clear();
            }

            public void EnsureLoaded()
            {
                if (_loaded)
                {
                    return;
                }

                _loaded = true;
                Rows = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
                Languages = Array.Empty<string>();

                if (string.IsNullOrEmpty(CsvAssetPath))
                {
                    LogWarningOnce("ShaderGUI localization table is not registered.");
                    return;
                }

                TextAsset csvAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(CsvAssetPath);
                if (csvAsset == null)
                {
                    LogWarningOnce($"ShaderGUI localization csv not found at '{CsvAssetPath}'.");
                    return;
                }

                ParseCsv(csvAsset.text);
            }

            private void ParseCsv(string csvText)
            {
                List<string> rows = SplitRows(csvText);
                if (rows.Count == 0)
                {
                    return;
                }

                List<string> headers = ParseCsvRow(rows[0]);
                if (headers.Count < 2)
                {
                    return;
                }

                var languages = new List<string>();
                for (int i = 1; i < headers.Count; i++)
                {
                    if (!IsTooltipColumn(headers[i]))
                    {
                        languages.Add(headers[i]);
                    }
                }

                Languages = languages.ToArray();
                for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
                {
                    List<string> values = ParseCsvRow(rows[rowIndex]);
                    if (values.Count == 0 || string.IsNullOrWhiteSpace(values[0]))
                    {
                        continue;
                    }

                    var languageMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    for (int i = 1; i < headers.Count; i++)
                    {
                        languageMap[headers[i]] = i < values.Count ? values[i] : string.Empty;
                    }

                    Rows[values[0]] = languageMap;
                }
            }

            private static List<string> SplitRows(string csvText)
            {
                var rows = new List<string>();
                if (string.IsNullOrEmpty(csvText))
                {
                    return rows;
                }

                var builder = new StringBuilder();
                bool inQuotes = false;
                for (int i = 0; i < csvText.Length; i++)
                {
                    char current = csvText[i];
                    if (current == '"')
                    {
                        if (inQuotes && i + 1 < csvText.Length && csvText[i + 1] == '"')
                        {
                            builder.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = !inQuotes;
                        }

                        continue;
                    }

                    if (!inQuotes && (current == '\n' || current == '\r'))
                    {
                        if (builder.Length > 0)
                        {
                            rows.Add(builder.ToString());
                            builder.Length = 0;
                        }

                        if (current == '\r' && i + 1 < csvText.Length && csvText[i + 1] == '\n')
                        {
                            i++;
                        }

                        continue;
                    }

                    builder.Append(current);
                }

                if (builder.Length > 0)
                {
                    rows.Add(builder.ToString());
                }

                return rows;
            }

            private static List<string> ParseCsvRow(string rowText)
            {
                var values = new List<string>();
                var builder = new StringBuilder();
                bool inQuotes = false;
                for (int i = 0; i < rowText.Length; i++)
                {
                    char current = rowText[i];
                    if (current == '"')
                    {
                        if (inQuotes && i + 1 < rowText.Length && rowText[i + 1] == '"')
                        {
                            builder.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = !inQuotes;
                        }

                        continue;
                    }

                    if (!inQuotes && current == ',')
                    {
                        values.Add(builder.ToString());
                        builder.Length = 0;
                        continue;
                    }

                    builder.Append(current);
                }

                values.Add(builder.ToString());
                return values;
            }

            private void LogWarningOnce(string message)
            {
                if (_warningLogged)
                {
                    return;
                }

                _warningLogged = true;
                Debug.LogWarning(message);
            }
        }
    }
}
