using NBShader;
using NBShaderEditor;
using UnityEditor;
using UnityEngine;

namespace NBShaders2.Editor.FeatureLevel
{
    [CustomEditor(typeof(NBShaderFeatureRuntimeSettings))]
    public sealed class NBShaderFeatureRuntimeSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawSyncButton();

            EditorGUILayout.Space();
            DrawDefaultInspector();
        }

        private void DrawSyncButton()
        {
            var settings = (NBShaderFeatureRuntimeSettings)target;
            using (new EditorGUI.DisabledScope(settings == null))
            {
                if (!GUILayout.Button(ButtonContent(
                        "featureRuntimeSettings.updateFromProjectSettings",
                        "根据当前ProjectSetting更新配置",
                        "Write the current NBShader Project Settings data into this runtime settings asset.")))
                {
                    return;
                }
            }

            Undo.RecordObject(
                settings,
                Text(
                    "featureRuntimeSettings.updateFromProjectSettings.undo",
                    "Update NBShader Runtime Settings From Project Settings"));

            if (NBShaderRuntimeSettingsSynchronizer.WriteProjectSettingsToRuntimeAsset(settings))
            {
                EditorUtility.DisplayDialog(
                    Text("featureRuntimeSettings.updateFromProjectSettings.successTitle", "Runtime Settings Updated"),
                    Text("featureRuntimeSettings.updateFromProjectSettings.successMessage", "The current NBShader Project Settings data was written to this runtime settings asset."),
                    Text("featureLevel.dialog.ok", "OK"));
            }
        }

        private static GUIContent ButtonContent(string key, string fallback, string tip)
        {
            return NBShaderInspectorLocalization.MakeInspectorContent(key + ".button", fallback, tip);
        }

        private static string Text(string key, string fallback)
        {
            return NBShaderInspectorLocalization.GetInspectorText(key, fallback);
        }
    }
}
