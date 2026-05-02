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
        private const string EnglishLanguage = "en-US";
        private const string TooltipColumnSuffix = "-tip";
        private const string TooltipColumnAliasSuffix = "-tooltip";
        private const string LanguagePreferenceKey = "NBShader2.Localization.Language";
        private const string CsvAssetPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/Editor/Localization/NBShaderInspectorLocalization.csv";
        private const string ChineseMenuPath = "Tools/NBShader2/Language/中文";
        private const string EnglishMenuPath = "Tools/NBShader2/Language/English";

        private static Dictionary<string, Dictionary<string, string>> _table;
        private static string[] _languages;

        public static GUIContent MakeContent(string labelKey, string labelFallback, string tooltipKey = null, string tooltipFallback = "")
        {
            return new GUIContent(Get(labelKey, labelFallback), GetTooltip(labelKey, tooltipKey, tooltipFallback));
        }

        public static GUIContent MakeInspectorContent(string key, string fallback, string tip = "")
        {
            return MakeContent("inspector." + key + ".label", fallback, "inspector." + key + ".tip", tip);
        }

        public static string GetInspectorText(string key, string fallback = "")
        {
            return Get("inspector." + key, fallback);
        }

        public static string[] GetInspectorOptions(string key, string[] fallback)
        {
            return GetOptions("inspector." + key + ".option", fallback);
        }

        public static string[] GetOptions(string keyPrefix, string[] fallback)
        {
            if (fallback == null)
            {
                return Array.Empty<string>();
            }

            string[] values = new string[fallback.Length];
            for (int i = 0; i < fallback.Length; i++)
            {
                values[i] = Get($"{keyPrefix}.{i}", fallback[i]);
            }

            return values;
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

        public static string GetTooltip(string labelKey, string tooltipKey = null, string fallback = "")
        {
            EnsureLoaded();
            if (!string.IsNullOrEmpty(labelKey) &&
                _table != null &&
                _table.TryGetValue(labelKey, out Dictionary<string, string> row))
            {
                string language = GetCurrentLanguage();
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
                return Get(tooltipKey, fallback);
            }

            return fallback;
        }

        public static void Reload()
        {
            _table = null;
            _languages = null;
        }

        [MenuItem(ChineseMenuPath)]
        private static void SelectChinese()
        {
            SetLanguage(DefaultLanguage);
        }

        [MenuItem(ChineseMenuPath, true)]
        private static bool ValidateChinese()
        {
            Menu.SetChecked(ChineseMenuPath, string.Equals(GetCurrentLanguage(), DefaultLanguage, StringComparison.OrdinalIgnoreCase));
            return true;
        }

        [MenuItem(EnglishMenuPath)]
        private static void SelectEnglish()
        {
            SetLanguage(EnglishLanguage);
        }

        [MenuItem(EnglishMenuPath, true)]
        private static bool ValidateEnglish()
        {
            Menu.SetChecked(EnglishMenuPath, string.Equals(GetCurrentLanguage(), EnglishLanguage, StringComparison.OrdinalIgnoreCase));
            return true;
        }

        [MenuItem("Tools/NBShader2/Language/Reload Localization")]
        private static void ReloadMenu()
        {
            AssetDatabase.ImportAsset(CsvAssetPath, ImportAssetOptions.ForceUpdate);
            Reload();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        private static void SetLanguage(string language)
        {
            EditorPrefs.SetString(LanguagePreferenceKey, language);
            Reload();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
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

            var languages = new List<string>();
            for (int i = 1; i < headers.Count; i++)
            {
                if (!IsTooltipColumn(headers[i]))
                {
                    languages.Add(headers[i]);
                }
            }

            _languages = languages.ToArray();
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
