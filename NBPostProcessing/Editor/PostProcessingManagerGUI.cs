using UnityEngine;
using UnityEditor;
using System.Reflection;
using NBShader;

namespace NBShaderEditor
{
    [CustomEditor(typeof(PostProcessingManager))]
    public class PostProcessingManagerGUI : Editor
    {
        private PostProcessingManager _ppManager;
        public override void OnInspectorGUI()
        {

            _ppManager = (PostProcessingManager)target;
            serializedObject.Update();
            DrawToggle(Content("manager.controllerFlags", "Controller索引标记"), "_controllerIndexFlags");
            DrawToggle(Content("manager.chromaticAberration", "色散开关"),
                PostProcessingManager.chromaticAberrationToggles);
            DrawToggle(Content("manager.distortion", "径向扭曲开关"), PostProcessingManager.distortSpeedToggles);
            DrawToggle(Content("manager.radialBlur", "径向模糊开关"), PostProcessingManager.radialBlurToggles);
            #if CINIMACHINE_3_0
            DrawToggle(Content("manager.cameraShake", "震屏开关"), PostProcessingManager.cameraShakeToggles);
            #endif
            DrawToggle(Content("manager.textureOverlay", "肌理开关"), PostProcessingManager.overlayTextureToggles);
            DrawToggle(Content("manager.flash", "反闪开关"), PostProcessingManager.flashToggles);
            DrawToggle(Content("manager.vignette", "暗角开关"), PostProcessingManager.vignetteToggles);
            if (PostProcessingManager.material)
            {
                DrawToggle32(Content("manager.shaderFlags", "ShaderFlags"),
                    PostProcessingManager.material.GetInteger(NBPostProcessFlags.FlagsId));
            }

        }

        private static GUIContent Content(string key, string fallback)
        {
            return NBPostProcessingLocalization.MakeInspectorContent(key, fallback);
        }

        void DrawToggle(GUIContent content, string propertyName)
        {
            int intValue = ReflectIntValue(propertyName);
            DrawToggle(content,intValue);
        }

        void DrawToggle(GUIContent content, int intValue)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(content);
            EditorGUILayout.LabelField(BinaryIntDrawer.DrawBinaryInt(intValue,8));
            EditorGUILayout.EndHorizontal();
        }
        void DrawToggle32(GUIContent content, int intValue)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(content);
            EditorGUILayout.LabelField(BinaryIntDrawer.DrawBinaryInt(intValue,32));
            EditorGUILayout.EndHorizontal();
        }

        int ReflectIntValue(string propertyName)
        {
            
            FieldInfo privateField = typeof(PostProcessingManager).GetField(propertyName, BindingFlags.NonPublic | BindingFlags.Instance);

            if (privateField != null)
            {
                // 获取私有字段的值
                int value = (int)privateField.GetValue(_ppManager);
                return value;
            }
            else
            {
                Debug.LogError(Content("manager.reflectionError", "PostProcessingManagerGUI获取变量错误").text);
                return -1;
            }
            
        }
       
    }
}
