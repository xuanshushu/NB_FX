using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    internal static class NBShaderInspectorLocalization
    {
        private const string DefaultLanguage = "zh-CN";
        private const string LanguagePreferenceKey = "NBShader2.Localization.Language";
        private const string CsvAssetPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/Editor/Localization/NBShaderInspectorLocalization.csv";

        private static Dictionary<string, Dictionary<string, string>> _table;
        private static string[] _languages;

        public static GUIContent MakeContent(string labelKey, string labelFallback, string tooltipKey = null, string tooltipFallback = "")
        {
            return new GUIContent(Get(labelKey, labelFallback), Get(tooltipKey, tooltipFallback));
        }

        public static string Get(string key, string fallback = "")
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(key))
            {
                return fallback;
            }

            if (_table != null && _table.TryGetValue(key, out Dictionary<string, string> row))
            {
                string language = GetCurrentLanguage();
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

        private static void EnsureLoaded()
        {
            if (_table != null)
            {
                return;
            }

            _table = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            _languages = Array.Empty<string>();

            TextAsset csvAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(CsvAssetPath);
            if (csvAsset == null)
            {
                Debug.LogWarning($"NBShader2 localization csv not found at '{CsvAssetPath}'.");
                return;
            }

            ParseCsv(csvAsset.text);
        }

        private static void ParseCsv(string csvText)
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

            _languages = headers.GetRange(1, headers.Count - 1).ToArray();
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

                _table[values[0]] = languageMap;
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

        private static string GetCurrentLanguage()
        {
            EnsureLoaded();

            string preferred = EditorPrefs.GetString(LanguagePreferenceKey, GetSystemLanguageName());
            foreach (string language in _languages)
            {
                if (string.Equals(language, preferred, StringComparison.OrdinalIgnoreCase))
                {
                    return language;
                }
            }

            return DefaultLanguage;
        }

        private static string GetSystemLanguageName()
        {
            switch (Application.systemLanguage)
            {
                case SystemLanguage.English:
                    return "en-US";
                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseSimplified:
                case SystemLanguage.ChineseTraditional:
                    return "zh-CN";
                default:
                    return DefaultLanguage;
            }
        }
    }
}
