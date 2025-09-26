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
            DrawToggle("controllerIndexFlags","_controllerIndexFlags");
            DrawToggle("色散开关",PostProcessingManager.chromaticAberrationToggles);
            DrawToggle("径向扭曲开关",PostProcessingManager.distortSpeedToggles);
            DrawToggle("径向模糊开关",PostProcessingManager.radialBlurToggles);
            #if CINIMACHINE_3_0
            DrawToggle("震屏开关",PostProcessingManager.cameraShakeToggles);
            #endif
            DrawToggle("肌理开关",PostProcessingManager.overlayTextureToggles);
            DrawToggle("黑白闪开关",PostProcessingManager.flashToggles);
            DrawToggle("暗角开关",PostProcessingManager.vignetteToggles);
            if (PostProcessingManager.material)
            {
                DrawToggle32("ShaderFlags", PostProcessingManager.material.GetInteger(NBPostProcessFlags.FlagsId));
            }

        }

        void DrawToggle(string label, string propertyName)
        {
            int intValue = ReflectIntValue(propertyName);
            DrawToggle(label,intValue);
        }

        void DrawToggle(string label, int intValue)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label);
            EditorGUILayout.LabelField(BinaryIntDrawer.DrawBinaryInt(intValue,8));
            EditorGUILayout.EndHorizontal();
        }
        void DrawToggle32(string label, int intValue)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label);
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
                Debug.LogError("PostProcessingManagerGUI获取变量错误");
                return -1;
            }
            
        }
       
    }
}