using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    public enum NBFXLanguageMode
    {
        FollowEditor,
        Chinese,
        English
    }

    [FilePath("ProjectSettings/NB_FXSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class NBFXProjectSettings : ScriptableSingleton<NBFXProjectSettings>
    {
        public const string SettingsPath = "Project/NB_FX";

        private const string LocalizationTableName = "NBShader";
        private const string LocalizationCsvAssetPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/Editor/Localization/NBShaderInspectorLocalization.csv";

        private sealed class SettingsSection
        {
            public string id;
            public Func<GUIContent> titleProvider;
            public Action<string> guiHandler;
            public List<string> keywords;
            public int order;
        }

        private static readonly string[] LanguageOptionsChineseFallback =
        {
            "跟随编辑器",
            "中文",
            "英文"
        };
        private static readonly string[] LanguageOptionsEnglishFallback =
        {
            "Follow Editor",
            "Chinese",
            "English"
        };
        private static readonly List<SettingsSection> s_Sections = new List<SettingsSection>();

        [SerializeField]
        private NBFXLanguageMode languageMode = NBFXLanguageMode.FollowEditor;

        public static NBFXLanguageMode LanguageMode => instance.languageMode;

        public static void SetLanguageMode(NBFXLanguageMode mode)
        {
            if (instance.languageMode == mode)
            {
                return;
            }

            instance.languageMode = mode;
            instance.Save(true);
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        public static void RegisterSettingsSection(
            string id,
            Func<GUIContent> titleProvider,
            Action<string> guiHandler,
            IEnumerable<string> keywords = null,
            int order = 0)
        {
            if (string.IsNullOrEmpty(id) || guiHandler == null)
            {
                return;
            }

            var section = new SettingsSection
            {
                id = id,
                titleProvider = titleProvider,
                guiHandler = guiHandler,
                keywords = keywords == null ? new List<string>() : new List<string>(keywords),
                order = order
            };

            for (int i = 0; i < s_Sections.Count; i++)
            {
                if (!string.Equals(s_Sections[i].id, id, StringComparison.Ordinal))
                {
                    continue;
                }

                s_Sections[i] = section;
                SortSections();
                return;
            }

            s_Sections.Add(section);
            SortSections();
        }

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            var provider = new SettingsProvider(SettingsPath, SettingsScope.Project)
            {
                label = "NB_FX",
                guiHandler = DrawSettingsGUI,
                keywords = BuildKeywords()
            };

            return provider;
        }

        private static void DrawSettingsGUI(string searchContext)
        {
            DrawSectionHeader(ProjectSettingsContent("projectSettings.generalSection", "基础设置", "General Settings"));

            EditorGUI.BeginChangeCheck();
            var selectedMode = (NBFXLanguageMode)EditorGUILayout.Popup(
                ProjectSettingsContent("projectSettings.defaultLanguage", "默认语言", "Default Language"),
                (int)LanguageMode,
                ProjectSettingsLanguageOptions());

            if (EditorGUI.EndChangeCheck())
            {
                SetLanguageMode(selectedMode);
            }

            DrawRegisteredSections(searchContext);
        }

        private static void DrawRegisteredSections(string searchContext)
        {
            for (int i = 0; i < s_Sections.Count; i++)
            {
                SettingsSection section = s_Sections[i];
                EditorGUILayout.Space(10f);
                DrawSectionHeader(section.titleProvider?.Invoke() ?? new GUIContent(section.id));
                section.guiHandler(searchContext);
            }
        }

        private static void DrawSectionHeader(GUIContent content)
        {
            EditorGUILayout.LabelField(content, EditorStyles.boldLabel);
        }

        private static GUIContent ProjectSettingsContent(string key, string chineseFallback, string englishFallback)
        {
            return new GUIContent(ProjectSettingsText(key, chineseFallback, englishFallback));
        }

        private static string ProjectSettingsText(string key, string chineseFallback, string englishFallback)
        {
            RegisterLocalization();
            string editorLanguage = ShaderGUILocalization.GetEditorLanguageName();
            string fallback = IsEnglishLanguage(editorLanguage) ? englishFallback : chineseFallback;
            return ShaderGUILocalization.GetInspectorText(LocalizationTableName, key, fallback, editorLanguage);
        }

        private static string[] ProjectSettingsLanguageOptions()
        {
            RegisterLocalization();
            string editorLanguage = ShaderGUILocalization.GetEditorLanguageName();
            string[] fallback = IsEnglishLanguage(editorLanguage)
                ? LanguageOptionsEnglishFallback
                : LanguageOptionsChineseFallback;
            return ShaderGUILocalization.GetInspectorOptions(
                LocalizationTableName,
                "projectSettings.languageMode",
                fallback,
                editorLanguage);
        }

        private static bool IsEnglishLanguage(string language)
        {
            return string.Equals(language, ShaderGUILocalization.EnglishLanguage, StringComparison.OrdinalIgnoreCase);
        }

        private static void RegisterLocalization()
        {
            ShaderGUILocalization.RegisterCsv(LocalizationTableName, LocalizationCsvAssetPath);
        }

        private static HashSet<string> BuildKeywords()
        {
            var keywords = new HashSet<string>
            {
                "NB_FX",
                "Language",
                "语言",
                "默认语言",
                "Default Language",
                "General Settings",
                "基础设置",
                "NBShader"
            };

            for (int i = 0; i < s_Sections.Count; i++)
            {
                List<string> sectionKeywords = s_Sections[i].keywords;
                if (sectionKeywords == null)
                {
                    continue;
                }

                for (int keywordIndex = 0; keywordIndex < sectionKeywords.Count; keywordIndex++)
                {
                    keywords.Add(sectionKeywords[keywordIndex]);
                }
            }

            return keywords;
        }

        private static void SortSections()
        {
            s_Sections.Sort((a, b) =>
            {
                int orderCompare = a.order.CompareTo(b.order);
                if (orderCompare != 0)
                {
                    return orderCompare;
                }

                return string.CompareOrdinal(a.id, b.id);
            });
        }
    }
}
