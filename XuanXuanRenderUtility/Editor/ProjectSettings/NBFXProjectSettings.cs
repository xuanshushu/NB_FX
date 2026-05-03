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
        private static readonly GUIContent DefaultLanguageContent = new GUIContent("默认语言");
        private static readonly string[] LanguageOptions =
        {
            "跟随编辑器",
            "中文",
            "英文"
        };

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

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            var provider = new SettingsProvider("Project/NB_FX", SettingsScope.Project)
            {
                label = "NB_FX",
                guiHandler = DrawSettingsGUI,
                keywords = new HashSet<string>
                {
                    "NB_FX",
                    "Language",
                    "语言",
                    "NBShader2"
                }
            };

            return provider;
        }

        private static void DrawSettingsGUI(string searchContext)
        {
            EditorGUI.BeginChangeCheck();
            var selectedMode = (NBFXLanguageMode)EditorGUILayout.Popup(
                DefaultLanguageContent,
                (int)LanguageMode,
                LanguageOptions);

            if (EditorGUI.EndChangeCheck())
            {
                SetLanguageMode(selectedMode);
            }
        }
    }
}
