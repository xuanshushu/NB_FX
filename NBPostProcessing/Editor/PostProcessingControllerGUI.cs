using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using System.Reflection;
#if CINIMACHINE_3_0
using Unity.Cinemachine;
#endif
using NBShader;

using System;
// using Unity.Properties;
namespace NBShaderEditor
{
    
    [CustomEditor(typeof(PostProcessingController))]
    public class PostProcessingControllerGUI : Editor
    {
        private SerializedProperty _managerProperty;
        private SerializedProperty _indexProperty;

        private Action delayExcuteReflect = () => { };

        public override void OnInspectorGUI()
        {
            PostProcessingController ppController = (PostProcessingController)target;
            serializedObject.Update();

            _managerProperty = serializedObject.FindProperty("_manager");
            _indexProperty = serializedObject.FindProperty("_index");
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(_managerProperty);
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginChangeCheck();
            // ppController.customScreenCenterPos = EditorGUILayout.Vector2Field("自定义屏幕中心", ppController.customScreenCenterPos);
            SerializedProperty customScreenCenterPosProp = serializedObject.FindProperty("customScreenCenterPos");
            EditorGUILayout.PropertyField(customScreenCenterPosProp, new GUIContent("自定义屏幕中心"));

            if (EditorGUI.EndChangeCheck())
            {
                ReflectMethod("SetScreenCenterPos", ppController);
            }

            SerializedProperty caToggleProp = serializedObject.FindProperty("chromaticAberrationToggle");
            DrawToggleFoldOut(ppController.AnimBools[0], "色散", caToggleProp,
                drawEndChangeCheck: isChangeToggle => { ReflectMethod("InitAllSettings", ppController); }
                , drawBlock: isToggle =>
                {
                    EditorGUI.BeginChangeCheck();
                    // ppController.caFromDistort = EditorGUILayout.Toggle("色散UV跟随后处理扭曲", ppController.caFromDistort);
                    SerializedProperty caFromDistortProp = serializedObject.FindProperty("caFromDistort");
                    EditorGUILayout.PropertyField(caFromDistortProp, new GUIContent("色散UV跟随后处理扭曲"));
                    if (EditorGUI.EndChangeCheck())
                    {
                        ReflectMethod("SetUVFromDistort", ppController);
                    }

                    // ppController.chromaticAberrationIntensity = EditorGUILayout.FloatField("色散强度", ppController.chromaticAberrationIntensity);
                    SerializedProperty caIntensityProps = serializedObject.FindProperty("chromaticAberrationIntensity");
                    EditorGUILayout.PropertyField(caIntensityProps, new GUIContent("色散强度"));

                    if (!ppController.caFromDistort)
                    {
                        // ppController.chromaticAberrationPos = EditorGUILayout.FloatField("色散位置", ppController.chromaticAberrationPos);
                        SerializedProperty caPosProp = serializedObject.FindProperty("chromaticAberrationPos");
                        EditorGUILayout.PropertyField(caPosProp, new GUIContent("色散位置"));

                        // ppController.chromaticAberrationRange = EditorGUILayout.FloatField("色散过渡范围", ppController.chromaticAberrationRange);
                        SerializedProperty caRangeProp = serializedObject.FindProperty("chromaticAberrationRange");
                        EditorGUILayout.PropertyField(caRangeProp, new GUIContent("色散过渡范围"));
                    }
                });

            SerializedProperty distortSpeedToggleProp = serializedObject.FindProperty("distortSpeedToggle");
            DrawToggleFoldOut(ppController.AnimBools[1], "扭曲", distortSpeedToggleProp, drawEndChangeCheck:
                isChangeToggle => { ReflectMethod("InitAllSettings", ppController); },
                drawBlock: isToggle =>
                {
                    EditorGUI.BeginChangeCheck();
                    // ppController.distortScreenUVMode = EditorGUILayout.Toggle("后处理走常规屏幕坐标", ppController.distortScreenUVMode);
                    SerializedProperty distortScreenUVModeProp = serializedObject.FindProperty("distortScreenUVMode");
                    EditorGUILayout.PropertyField(distortScreenUVModeProp, new GUIContent("后处理走常规屏幕坐标"));
                    if (EditorGUI.EndChangeCheck())
                    {
                        ReflectMethod("SetUVFromDistort", ppController);
                    }

                    EditorGUI.BeginChangeCheck();
                    // ppController.distortSpeedTexture = (Texture2D)EditorGUILayout.ObjectField("后处理扭曲贴图", ppController.distortSpeedTexture, typeof(Texture2D));
                    SerializedProperty distortSpeedTextureProp = serializedObject.FindProperty("distortSpeedTexture");
                    EditorGUILayout.PropertyField(distortSpeedTextureProp, new GUIContent("后处理扭曲贴图"));
                    if (EditorGUI.EndChangeCheck())
                    {
                        ReflectMethod("InitAllSettings", ppController);
                    }

                    if (ppController.distortScreenUVMode)
                    {
                        EditorGUI.BeginChangeCheck();
                        // ppController.distortTextureMidValue = EditorGUILayout.FloatField("扭曲贴图中间值", ppController.distortTextureMidValue);
                        SerializedProperty distortTextureMidValueProp =
                            serializedObject.FindProperty("distortTextureMidValue");
                        EditorGUILayout.PropertyField(distortTextureMidValueProp, new GUIContent("扭曲贴图中间值"));
                        if (EditorGUI.EndChangeCheck())
                        {
                            ReflectMethod("SetTexture", ppController);
                        }
                    }


                    // ppController.distortSpeedTexSt = EditorGUILayout.Vector4Field("扭曲贴图ST", ppController.distortSpeedTexSt);
                    SerializedProperty distortSpeedTexStProp = serializedObject.FindProperty("distortSpeedTexSt");
                    EditorGUILayout.PropertyField(distortSpeedTexStProp, new GUIContent("扭曲贴图缩放平移"));
                    // ppController.distortSpeedIntensity = EditorGUILayout.FloatField("扭曲强度", ppController.distortSpeedIntensity);
                    SerializedProperty distortSpeedIntensityProp =
                        serializedObject.FindProperty("distortSpeedIntensity");
                    EditorGUILayout.PropertyField(distortSpeedIntensityProp, new GUIContent("扭曲强度"));

                    if (!ppController.distortScreenUVMode)
                    {
                        // ppController.distortSpeedPosition = EditorGUILayout.FloatField("扭曲位置", ppController.distortSpeedPosition);
                        SerializedProperty distortSpeedPositionProp =
                            serializedObject.FindProperty("distortSpeedPosition");
                        EditorGUILayout.PropertyField(distortSpeedPositionProp, new GUIContent("扭曲位置"));
                        // ppController.distortSpeedRange = EditorGUILayout.FloatField("扭曲过渡范围", ppController.distortSpeedRange);
                        SerializedProperty distortSpeedRangeProp = serializedObject.FindProperty("distortSpeedRange");
                        EditorGUILayout.PropertyField(distortSpeedRangeProp, new GUIContent("扭曲过渡范围"));
                    }

                    // ppController.distortSpeedMoveSpeedX = EditorGUILayout.FloatField("扭曲纹理流动X", ppController.distortSpeedMoveSpeedX);
                    SerializedProperty distortSpeedMoveSpeedXProp =
                        serializedObject.FindProperty("distortSpeedMoveSpeedX");
                    EditorGUILayout.PropertyField(distortSpeedMoveSpeedXProp, new GUIContent("扭曲纹理流动X"));
                    // ppController.distortSpeedMoveSpeed = EditorGUILayout.FloatField("扭曲纹理流动Y", ppController.distortSpeedMoveSpeed);
                    SerializedProperty distortSpeedMoveSpeed = serializedObject.FindProperty("distortSpeedMoveSpeed");
                    EditorGUILayout.PropertyField(distortSpeedMoveSpeed, new GUIContent("扭曲纹理流动Y"));
                });


            SerializedProperty radialBlurToggleProp = serializedObject.FindProperty("radialBlurToggle");
            DrawToggleFoldOut(ppController.AnimBools[2], "径向模糊", radialBlurToggleProp, drawEndChangeCheck:
                isChangeToggle => { ReflectMethod("InitAllSettings", ppController); },
                drawBlock: isToggle =>
                {
                    EditorGUI.BeginChangeCheck();
                    // ppController.radialBlurFromDistort = EditorGUILayout.Toggle("径向模糊跟随后处理扭曲", ppController.radialBlurFromDistort);
                    SerializedProperty radialBlurFromDistortProp =
                        serializedObject.FindProperty("radialBlurFromDistort");
                    EditorGUILayout.PropertyField(radialBlurFromDistortProp, new GUIContent("径向模糊跟随后处理扭曲"));
                    if (EditorGUI.EndChangeCheck())
                    {
                        ReflectMethod("SetUVFromDistort", ppController);
                    }

                    // ppController.radialBlurSampleCount = EditorGUILayout.IntSlider("采样次数", ppController.radialBlurSampleCount, 1, 12);
                    SerializedProperty radialBlurSampleCountProp =
                        serializedObject.FindProperty("radialBlurSampleCount");
                    EditorGUILayout.PropertyField(radialBlurSampleCountProp, new GUIContent("采样次数"));
                    // ppController.radialBlurIntensity = EditorGUILayout.FloatField("强度", ppController.radialBlurIntensity);
                    SerializedProperty radialBlurIntensityProp = serializedObject.FindProperty("radialBlurIntensity");
                    EditorGUILayout.PropertyField(radialBlurIntensityProp, new GUIContent("强度"));
                    if (!ppController.radialBlurFromDistort)
                    {
                        // ppController.radialBlurPos = EditorGUILayout.FloatField("位置", ppController.radialBlurPos);\
                        SerializedProperty radialBlurPosProp = serializedObject.FindProperty("radialBlurPos");
                        EditorGUILayout.PropertyField(radialBlurPosProp, new GUIContent("位置"));
                        // ppController.radialBlurRange = EditorGUILayout.FloatField("过渡范围", ppController.radialBlurRange);
                        SerializedProperty radialBlurRangeProp = serializedObject.FindProperty("radialBlurRange");
                        EditorGUILayout.PropertyField(radialBlurRangeProp, new GUIContent("过渡范围"));
                    }
                });

#if CINIMACHINE_3_0
            SerializedProperty cameraShakeToggleProp = serializedObject.FindProperty("cameraShakeToggle");
            DrawToggleFoldOut(ppController.AnimBools[3], "震屏", cameraShakeToggleProp, drawEndChangeCheck:
                isChangeToggle => { ReflectMethod("InitAllSettings", ppController); },
                drawBlock: isToggle =>
                {

                    EditorGUI.BeginChangeCheck();
                    // ppController.cinemachineCamera = (CinemachineCamera)EditorGUILayout.ObjectField("绑定Cinemachine相机", ppController.cinemachineCamera, typeof(CinemachineCamera));
                    SerializedProperty cinemachineCameraProp = serializedObject.FindProperty("cinemachineCamera");
                    EditorGUILayout.PropertyField(cinemachineCameraProp, new GUIContent("绑定Cinemachine相机"));
                    if (EditorGUI.EndChangeCheck())
                    {
                        // ReflectMethod("InitCinemachineCamera",ppController);
                        ppController.InitCinemachineCamera();
                    }

                    // ppController.cameraShakeIntensity = EditorGUILayout.FloatField("相机震动强度", ppController.cameraShakeIntensity);
                    SerializedProperty cameraShakeIntensityProp = serializedObject.FindProperty("cameraShakeIntensity");
                    EditorGUILayout.PropertyField(cameraShakeIntensityProp, new GUIContent("相机震动强度"));
                });
#endif

            SerializedProperty overlayTextureToggleProp = serializedObject.FindProperty("overlayTextureToggle");
            DrawToggleFoldOut(ppController.AnimBools[4], "肌理叠加图", overlayTextureToggleProp,
                drawEndChangeCheck: isChangeToggle => { ReflectMethod("InitAllSettings", ppController); },
                drawBlock: isToggle =>
                {
                    EditorGUI.BeginChangeCheck();
                    // ppController.overlayTexturePolarCoordMode = EditorGUILayout.Toggle("肌理图极坐标模式", ppController.overlayTexturePolarCoordMode);
                    SerializedProperty overlayTexturePolarCoordModeProp =
                        serializedObject.FindProperty("overlayTexturePolarCoordMode");
                    EditorGUILayout.PropertyField(overlayTexturePolarCoordModeProp, new GUIContent("肌理图极坐标模式"));
                    // ppController.overlayTexture = (Texture2D)EditorGUILayout.ObjectField("肌理图", ppController.overlayTexture, typeof(Texture2D));
                    SerializedProperty overlayTextureProp = serializedObject.FindProperty("overlayTexture");
                    EditorGUILayout.PropertyField(overlayTextureProp, new GUIContent("肌理图"));
                    if (EditorGUI.EndChangeCheck())
                    {
                        ReflectMethod("SetTexture", ppController);
                    }

                    // ppController.overlayTextureSt = EditorGUILayout.Vector4Field("肌理图缩放平移", ppController.overlayTextureSt);
                    SerializedProperty overlayTextureStProp = serializedObject.FindProperty("overlayTextureSt");
                    EditorGUILayout.PropertyField(overlayTextureStProp, new GUIContent("肌理图缩放平移"));
                    // ppController.overlayTextureAnim = EditorGUILayout.Vector2Field("肌理图偏移动画", ppController.overlayTextureAnim);
                    SerializedProperty overlayTextureAnimProp = serializedObject.FindProperty("overlayTextureAnim");
                    EditorGUILayout.PropertyField(overlayTextureAnimProp, new GUIContent("肌理图偏移动画"));
                    // ppController.overlayTextureIntensity = EditorGUILayout.FloatField("肌理图强度", ppController.overlayTextureIntensity);
                    SerializedProperty overlayTextureIntensityProp =
                        serializedObject.FindProperty("overlayTextureIntensity");
                    EditorGUILayout.PropertyField(overlayTextureIntensityProp, new GUIContent("肌理图强度"));

                    EditorGUI.BeginChangeCheck();
                    // ppController.overlayMaskTexture = (Texture2D)EditorGUILayout.ObjectField("肌理蒙版图", ppController.overlayMaskTexture, typeof(Texture2D));
                    SerializedProperty overlayMaskTextureProp = serializedObject.FindProperty("overlayMaskTexture");
                    EditorGUILayout.PropertyField(overlayMaskTextureProp, new GUIContent("肌理蒙版图"));
                    if (EditorGUI.EndChangeCheck())
                    {
                        ReflectMethod("SetTexture", ppController);
                    }

                    // ppController.overlayMaskTextureSt = EditorGUILayout.Vector4Field("肌理图蒙版缩放平移", ppController.overlayMaskTextureSt);
                    SerializedProperty overlayMaskTextureStProp = serializedObject.FindProperty("overlayMaskTextureSt");
                    EditorGUILayout.PropertyField(overlayMaskTextureStProp, new GUIContent("肌理图蒙版缩放平移"));

                });

            SerializedProperty flashToggleProp = serializedObject.FindProperty("flashToggle");
            DrawToggleFoldOut(ppController.AnimBools[5], "反闪", flashToggleProp, drawEndChangeCheck:
                isChangeToggle => { ReflectMethod("InitAllSettings", ppController); },
                drawBlock: isToggle =>
                {
                    // ppController.flashInvertIntensity = EditorGUILayout.FloatField("反转度", ppController.flashInvertIntensity);
                    SerializedProperty flashInvertIntensityProp = serializedObject.FindProperty("flashInvertIntensity");
                    EditorGUILayout.PropertyField(flashInvertIntensityProp, new GUIContent("反转度"));
                    // ppController.flashDeSaturateIntensity = EditorGUILayout.FloatField("饱和度", ppController.flashDeSaturateIntensity);
                    SerializedProperty flashDeSaturateIntensityProp =
                        serializedObject.FindProperty("flashDeSaturateIntensity");
                    EditorGUILayout.PropertyField(flashDeSaturateIntensityProp, new GUIContent("饱和度"));
                    // ppController.flashContrast = EditorGUILayout.FloatField("对比度", ppController.flashContrast);
                    SerializedProperty flashContrastProp = serializedObject.FindProperty("flashContrast");
                    EditorGUILayout.PropertyField(flashContrastProp, new GUIContent("对比度"));
                    // ppController.flashColor = EditorGUILayout.ColorField("闪颜色", ppController.flashColor);
                    SerializedProperty flashColorProp = serializedObject.FindProperty("flashColor");
                    EditorGUILayout.PropertyField(flashColorProp, new GUIContent("亮部闪颜色"));
                    SerializedProperty blackFlashColorProp = serializedObject.FindProperty("blackFlashColor");
                    EditorGUILayout.PropertyField(blackFlashColorProp, new GUIContent("暗部闪颜色"));
                });

            SerializedProperty vignetteToggleProp = serializedObject.FindProperty("vignetteToggle");
            DrawToggleFoldOut(ppController.AnimBools[6], "暗角", vignetteToggleProp, drawEndChangeCheck:
                isChangeToggle => { ReflectMethod("InitAllSettings", ppController); },
                drawBlock: isToggle =>
                {
                    // ppController.vignetteColor = EditorGUILayout.ColorField("暗角颜色", ppController.vignetteColor);
                    SerializedProperty vignetteColorProp = serializedObject.FindProperty("vignetteColor");
                    EditorGUILayout.PropertyField(vignetteColorProp, new GUIContent("暗角颜色"));
                    // ppController.vignetteIntensity = EditorGUILayout.FloatField("暗角强度", ppController.vignetteIntensity);
                    SerializedProperty vignetteIntensityProp = serializedObject.FindProperty("vignetteIntensity");
                    EditorGUILayout.PropertyField(vignetteIntensityProp, new GUIContent("暗角强度"));
                    // ppController.vignetteRoundness = EditorGUILayout.FloatField("暗角圆度", ppController.vignetteRoundness);
                    SerializedProperty vignetteRoundnessProp = serializedObject.FindProperty("vignetteRoundness");
                    EditorGUILayout.PropertyField(vignetteRoundnessProp, new GUIContent("暗角圆度"));
                    // ppController.vignetteSmothness = EditorGUILayout.FloatField("暗角平滑度", ppController.vignetteSmothness);
                    SerializedProperty vignetteSmothnessProp = serializedObject.FindProperty("vignetteSmothness");
                    EditorGUILayout.PropertyField(vignetteSmothnessProp, new GUIContent("暗角平滑度"));
                });

            if (GUILayout.Button("选择当前Manager"))
            {
                ReflectMethod("FindManager", ppController);
            }
#if CINIMACHINE_3_0
            if (GUILayout.Button("选择当前CinemachineCamera"))
            {
                ppController.FindVirtualCamera();
            }
#endif

            serializedObject.ApplyModifiedProperties();

        }

        public void DrawToggleFoldOut(AnimBool foldOutAnimBool, string label, SerializedProperty boolProperty,
            bool isIndentBlock = true,
            FontStyle fontStyle = FontStyle.Bold,
            Action<bool> drawBlock = null, Action<bool> drawEndChangeCheck = null)
        {
            if (fontStyle == FontStyle.Bold)
            {
                EditorGUILayout.Space();
            }

            EditorGUILayout.BeginHorizontal();
            var rect = EditorGUILayout.GetControlRect();

            var foldoutRect = new Rect(rect.x, rect.y, rect.width, rect.height);
            // foldoutRect.width = toggleRect.x - foldoutRect.x;
            var labelRect = new Rect(rect.x + 18f, rect.y, rect.width - 18f, rect.height);

            // bool isToggle = false;
            // 必须先画Toggle，不然按钮会被FoldOut和Label覆盖。
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(rect, boolProperty, new GUIContent(""));
            // isToggle = EditorGUI.Toggle(rect, isToggle, EditorStyles.toggle);
            if (EditorGUI.EndChangeCheck())
            {
                drawEndChangeCheck?.Invoke(boolProperty.boolValue);
            }

            // EditorGUI.DrawRect(foldoutRect,Color.red);
            foldOutAnimBool.target = EditorGUI.Foldout(foldoutRect, foldOutAnimBool.target, string.Empty, true);
            var origFontStyle = EditorStyles.label.fontStyle;
            EditorStyles.label.fontStyle = fontStyle;
            // EditorGUI.DrawRect(labelRect,Color.blue);
            EditorGUI.LabelField(labelRect, label);
            EditorStyles.label.fontStyle = origFontStyle;
            EditorGUILayout.EndHorizontal();
            if (isIndentBlock) EditorGUI.indentLevel++;
            float faded = foldOutAnimBool.faded;
            if (faded == 0) faded = 0.00001f; //用于欺骗FadeGroup，不要让他真的关闭了。这样会藏不住相关的GUI。我们的目的是，GUI藏住，但是逻辑还是在跑。drawBlock要执行。
            EditorGUILayout.BeginFadeGroup(faded);
            {
                EditorGUI.BeginDisabledGroup(!boolProperty.boolValue);
                drawBlock?.Invoke(boolProperty.boolValue);
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndFadeGroup();
            if (isIndentBlock) EditorGUI.indentLevel--;
        }

        void ReflectMethod(string methodName, PostProcessingController controller)
        {
            serializedObject.ApplyModifiedProperties();
            MethodInfo privateMethod =
                typeof(PostProcessingController).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (privateMethod != null)
            {
                privateMethod.Invoke(controller, null);
            }
            else
            {
                Debug.LogError("Private method " + methodName + " not found!");
            }
        }
    }
}