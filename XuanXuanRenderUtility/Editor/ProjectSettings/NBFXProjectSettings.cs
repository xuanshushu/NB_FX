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
        private const string FoldoutSessionPrefix = "NBFXProjectSettings.Foldout.";

        private sealed class SettingsSection
        {
            public string id;
            public Func<GUIContent> titleProvider;
            public Action<string> guiHandler;
            public List<string> keywords;
            public int order;
            public GUIContent titleContent;
            public string titleLanguage;
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
        private static readonly Dictionary<string, GUIContent> s_ProjectSettingsContentCache =
            new Dictionary<string, GUIContent>(StringComparer.Ordinal);
        private static GUIStyle s_FoldoutHeaderStyle;
        private static string s_ProjectSettingsEditorLanguage;
        private static string[] s_ProjectSettingsLanguageOptions;

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
            EnsureProjectSettingsContentCacheForEditorLanguage();

            if (DrawSectionFoldout(
                    "General",
                    ProjectSettingsContent("projectSettings.generalSection", "基础设置", "General Settings")))
            {
                EditorGUI.BeginChangeCheck();
                var selectedMode = (NBFXLanguageMode)EditorGUILayout.Popup(
                    ProjectSettingsContent("projectSettings.defaultLanguage", "默认语言", "Default Language"),
                    (int)LanguageMode,
                    ProjectSettingsLanguageOptions());

                if (EditorGUI.EndChangeCheck())
                {
                    SetLanguageMode(selectedMode);
                }
            }

            DrawRegisteredSections(searchContext);
        }

        private static void DrawRegisteredSections(string searchContext)
        {
            for (int i = 0; i < s_Sections.Count; i++)
            {
                SettingsSection section = s_Sections[i];
                EditorGUILayout.Space(10f);
                if (DrawSectionFoldout(section.id, GetSectionTitleContent(section)))
                {
                    section.guiHandler(searchContext);
                }
            }
        }

        private static bool DrawSectionFoldout(string id, GUIContent content)
        {
            string sessionKey = FoldoutSessionPrefix + id;
            bool expanded = SessionState.GetBool(sessionKey, true);
            EditorGUI.BeginChangeCheck();
            bool nextExpanded = EditorGUILayout.Foldout(expanded, content, true, FoldoutHeaderStyle);
            if (EditorGUI.EndChangeCheck())
            {
                SessionState.SetBool(sessionKey, nextExpanded);
            }

            return nextExpanded;
        }

        private static GUIStyle FoldoutHeaderStyle
        {
            get
            {
                if (s_FoldoutHeaderStyle == null)
                {
                    s_FoldoutHeaderStyle = new GUIStyle(EditorStyles.foldout)
                    {
                        fontStyle = FontStyle.Bold
                    };
                }

                return s_FoldoutHeaderStyle;
            }
        }

        private static GUIContent ProjectSettingsContent(string key, string chineseFallback, string englishFallback)
        {
            EnsureProjectSettingsContentCacheForEditorLanguage();

            GUIContent content;
            if (s_ProjectSettingsContentCache.TryGetValue(key, out content))
                return content;

            var text = ProjectSettingsText(key, chineseFallback, englishFallback, s_ProjectSettingsEditorLanguage);
            content = new GUIContent(text, text);
            s_ProjectSettingsContentCache.Add(key, content);
            return content;
        }

        private static string ProjectSettingsText(
            string key,
            string chineseFallback,
            string englishFallback,
            string editorLanguage)
        {
            string fallback = IsEnglishLanguage(editorLanguage) ? englishFallback : chineseFallback;
            return ShaderGUILocalization.GetInspectorText(LocalizationTableName, key, fallback, editorLanguage);
        }

        private static string[] ProjectSettingsLanguageOptions()
        {
            EnsureProjectSettingsContentCacheForEditorLanguage();
            if (s_ProjectSettingsLanguageOptions != null)
                return s_ProjectSettingsLanguageOptions;

            string[] fallback = IsEnglishLanguage(s_ProjectSettingsEditorLanguage)
                ? LanguageOptionsEnglishFallback
                : LanguageOptionsChineseFallback;
            s_ProjectSettingsLanguageOptions = ShaderGUILocalization.GetInspectorOptions(
                LocalizationTableName,
                "projectSettings.languageMode",
                fallback,
                s_ProjectSettingsEditorLanguage);
            return s_ProjectSettingsLanguageOptions;
        }

        private static GUIContent GetSectionTitleContent(SettingsSection section)
        {
            if (section.titleProvider != null)
            {
                string language = ShaderGUILocalization.GetCurrentLanguage(LocalizationTableName);
                if (section.titleContent != null &&
                    string.Equals(section.titleLanguage, language, StringComparison.Ordinal))
                {
                    return section.titleContent;
                }

                section.titleLanguage = language;
                section.titleContent = section.titleProvider();
                return section.titleContent;
            }

            GUIContent content;
            if (s_ProjectSettingsContentCache.TryGetValue(section.id, out content))
                return content;

            content = new GUIContent(section.id, section.id);
            s_ProjectSettingsContentCache.Add(section.id, content);
            return content;
        }

        private static void EnsureProjectSettingsContentCacheForEditorLanguage()
        {
            RegisterLocalization();
            string editorLanguage = ShaderGUILocalization.GetEditorLanguageName();
            if (string.Equals(s_ProjectSettingsEditorLanguage, editorLanguage, StringComparison.Ordinal))
                return;

            s_ProjectSettingsEditorLanguage = editorLanguage;
            s_ProjectSettingsContentCache.Clear();
            s_ProjectSettingsLanguageOptions = null;
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
