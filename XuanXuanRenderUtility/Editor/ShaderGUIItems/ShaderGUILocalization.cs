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
            return new GUIContent(
                Get(tableName, labelKey, labelFallback),
                GetTooltip(tableName, labelKey, tooltipKey, tooltipFallback));
        }

        public static GUIContent MakeInspectorContent(string tableName, string key, string fallback, string tip = "")
        {
            return MakeContent(tableName, "inspector." + key + ".label", fallback, "inspector." + key + ".tip", tip);
        }

        public static string GetInspectorText(string tableName, string key, string fallback = "")
        {
            return Get(tableName, "inspector." + key, fallback);
        }

        public static string[] GetInspectorOptions(string tableName, string key, string[] fallback)
        {
            return GetOptions(tableName, "inspector." + key + ".option", fallback);
        }

        public static string[] GetOptions(string tableName, string keyPrefix, string[] fallback)
        {
            if (fallback == null)
            {
                return Array.Empty<string>();
            }

            string[] values = new string[fallback.Length];
            for (int i = 0; i < fallback.Length; i++)
            {
                values[i] = Get(tableName, $"{keyPrefix}.{i}", fallback[i]);
            }

            return values;
        }

        public static string Get(string tableName, string key, string fallback = "")
        {
            LocalizationTable table = GetTable(tableName);
            table.EnsureLoaded();
            if (string.IsNullOrEmpty(key))
            {
                return fallback;
            }

            if (table.Rows.TryGetValue(key, out Dictionary<string, string> row))
            {
                string language = GetCurrentLanguage(tableName);
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
            if (!string.IsNullOrEmpty(labelKey) &&
                table.Rows.TryGetValue(labelKey, out Dictionary<string, string> row))
            {
                string language = GetCurrentLanguage(tableName);
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
                return Get(tableName, tooltipKey, fallback);
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

                return;
            }

            GetTable(tableName).Reload();
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
            switch (NBFXProjectSettings.LanguageMode)
            {
                case NBFXLanguageMode.Chinese:
                    return DefaultLanguage;
                case NBFXLanguageMode.English:
                    return EnglishLanguage;
                case NBFXLanguageMode.FollowEditor:
                default:
                    return GetLanguageFromEditor();
            }
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

        private sealed class LocalizationTable
        {
            private bool _loaded;
            private bool _warningLogged;

            public LocalizationTable(string csvAssetPath)
            {
                CsvAssetPath = csvAssetPath;
                Rows = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
                Languages = Array.Empty<string>();
            }

            public string CsvAssetPath { get; set; }
            public Dictionary<string, Dictionary<string, string>> Rows { get; private set; }
            public string[] Languages { get; private set; }

            public void Reload()
            {
                _loaded = false;
                _warningLogged = false;
                Rows = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
                Languages = Array.Empty<string>();
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
