using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using System.Collections.Generic;
using System.Reflection;
#if CINIMACHINE_3_0
using Unity.Cinemachine;
#endif
using NBShader;

using System;
// using Unity.Properties;
namespace NBShaderEditor
{
    [InitializeOnLoad]
    internal static class NBPostProcessingLocalization
    {
        private const string TableName = "NBPostProcessing";
        private const string CsvAssetPath =
            "Packages/com.xuanxuan.nb.fx/NBPostProcessing/Editor/Localization/NBPostProcessingLocalization.csv";

        static NBPostProcessingLocalization()
        {
            RegisterTable();
        }

        public static GUIContent MakeInspectorContent(string key, string fallback, string tip = "")
        {
            RegisterTable();
            return ShaderGUILocalization.MakeInspectorContent(TableName, key, fallback, tip);
        }

        public static string[] GetInspectorOptions(string key, string[] fallback)
        {
            RegisterTable();
            return ShaderGUILocalization.GetInspectorOptions(TableName, key, fallback);
        }

        private static void RegisterTable()
        {
            ShaderGUILocalization.RegisterCsv(TableName, CsvAssetPath);
        }
    }

    [CustomEditor(typeof(PostProcessingController))]
    public class PostProcessingControllerGUI : Editor
    {
        private static GUIContent ResetContent => NBPostProcessingLocalization.MakeInspectorContent(
            "common.reset", "R", "重置当前属性及子级属性（如有）");

        private static readonly string[] ChromaticAberrationPropertyNames =
        {
            "chromaticAberrationToggle",
            "caFromDistort",
            "chromaticAberrationIntensity",
            "chromaticAberrationPos",
            "chromaticAberrationRange"
        };

        private static readonly string[] DistortPropertyNames =
        {
            "distortSpeedToggle",
            "distortScreenUVMode",
            "distortSpeedTexture",
            "distortTextureMidValue",
            "distortSpeedTexSt",
            "distortSpeedIntensity",
            "distortSpeedPosition",
            "distortSpeedRange",
            "distortSpeedMoveSpeedX",
            "distortSpeedMoveSpeed"
        };

        private static readonly string[] RadialBlurPropertyNames =
        {
            "radialBlurToggle",
            "radialBlurSampleCount",
            "radialBlurFromDistort",
            "radialBlurIntensity",
            "radialBlurPos",
            "radialBlurRange"
        };

#if CINIMACHINE_3_0
        private static readonly string[] CameraShakePropertyNames =
        {
            "cameraShakeToggle",
            "cinemachineCamera",
            "cameraShakeIntensity"
        };
#endif

        private static readonly string[] OverlayTexturePropertyNames =
        {
            "overlayTextureToggle",
            "overlayTextureBlendMode",
            "overlayTexturePolarCoordMode",
            "overlayTexture",
            "overlayTextureSt",
            "overlayTextureAnim",
            "overlayTextureIntensity",
            "overlayMaskTexture",
            "overlayMaskTextureSt"
        };

        private static readonly string[] FlashPropertyNames =
        {
            "flashToggle",
            "flashIntensity",
            "flashGradientRange",
            "flashContrast",
            "flashColor",
            "blackFlashColor",
            "flashInvertIntensity",
            "flashTextureToggle",
            "flashTexture",
            "flashTexturePolarCoordMode",
            "flashTextureScaleOffset",
            "flashVec",
            "flashDeSaturateIntensity",
            "flashTextureIntensity",
            "flashVecZW",
            "flashTextureMaskIntensity"
        };

        private static readonly string[] FlashTexturePropertyNames =
        {
            "flashTexture",
            "flashTexturePolarCoordMode",
            "flashTextureScaleOffset",
            "flashVec",
            "flashDeSaturateIntensity",
            "flashTextureIntensity"
        };

        private static readonly string[] FlashTextureMaskPropertyNames =
        {
            "flashVecZW",
            "flashTextureMaskIntensity"
        };

        private static readonly string[] FlashTextureFeaturePropertyNames =
        {
            "flashTextureToggle",
            "flashTexture",
            "flashTexturePolarCoordMode",
            "flashTextureScaleOffset",
            "flashVec",
            "flashDeSaturateIntensity",
            "flashTextureIntensity",
            "flashVecZW",
            "flashTextureMaskIntensity"
        };

        private static readonly string[] VignettePropertyNames =
        {
            "vignetteToggle",
            "vignetteColor",
            "vignetteIntensity",
            "vignetteRoundness",
            "vignetteSmothness",
            "vignetteFill"
        };

        private static readonly string[] ControllerPropertyNames = BuildControllerPropertyNames();

        private SerializedProperty _managerProperty;
        private SerializedProperty _indexProperty;
        private GameObject _defaultValuesObject;
        private SerializedObject _defaultValuesSerializedObject;

        private Action delayExcuteReflect = () => { };

        private void OnEnable()
        {
            EnsureDefaultValuesObject();
        }

        private void OnDisable()
        {
            _defaultValuesSerializedObject = null;
            if (_defaultValuesObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_defaultValuesObject);
                _defaultValuesObject = null;
            }
        }

        public override void OnInspectorGUI()
        {
            PostProcessingController ppController = (PostProcessingController)target;
            EnsureDefaultValuesObject();
            serializedObject.Update();

            _managerProperty = serializedObject.FindProperty("_manager");
            _indexProperty = serializedObject.FindProperty("_index");
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(_managerProperty,
                NBPostProcessingLocalization.MakeInspectorContent("property.manager", "Manager"));
            EditorGUI.EndDisabledGroup();

            DrawResetHeader(
                NBPostProcessingLocalization.MakeInspectorContent("header.parameters", "后处理参数"),
                ControllerPropertyNames,
                () => ReflectMethod("InitAllSettings", ppController));

            DrawPropertyWithReset("customScreenCenterTransform", "屏幕中心跟随Transform",
                () => ReflectMethod("SetScreenCenterPos", ppController),
                "绑定后使用主相机计算 Transform 在 GameView 中的屏幕位置。");
            using (new EditorGUI.DisabledScope(ppController.customScreenCenterTransform != null))
            {
                DrawPropertyWithReset("customScreenCenterPos", "自定义屏幕中心",
                    () => ReflectMethod("SetScreenCenterPos", ppController),
                    "以 GameView 为准的 Viewport 坐标：左下角为 (0, 0)，右上角为 (1, 1)；绑定 Transform 时只读。");
            }

            SerializedProperty caToggleProp = serializedObject.FindProperty("chromaticAberrationToggle");
            DrawToggleFoldOut(ppController.AnimBools[0], "色散", caToggleProp, ChromaticAberrationPropertyNames,
                drawEndChangeCheck: isChangeToggle => { ReflectMethod("InitAllSettings", ppController); }
                , drawBlock: isToggle =>
                {
                    DrawPropertyWithReset("caFromDistort", "材质扰动色散强度",
                        () => ReflectMethod("SetUVFromDistort", ppController));
                    DrawPropertyWithReset("chromaticAberrationIntensity", "后处理色散强度");
                    DrawPropertyWithReset("chromaticAberrationPos", "后处理色散过渡位置");
                    DrawPropertyWithReset("chromaticAberrationRange", "后处理色散过渡范围");
                });

            SerializedProperty distortSpeedToggleProp = serializedObject.FindProperty("distortSpeedToggle");
            DrawToggleFoldOut(ppController.AnimBools[1], "扭曲", distortSpeedToggleProp, DistortPropertyNames,
                drawEndChangeCheck:
                isChangeToggle => { ReflectMethod("InitAllSettings", ppController); },
                drawBlock: isToggle =>
                {
                    DrawPropertyWithReset("distortScreenUVMode", "后处理走常规屏幕坐标",
                        () => ReflectMethod("SetUVFromDistort", ppController));
                    DrawPropertyWithReset("distortSpeedTexture", "后处理扭曲贴图",
                        () => ReflectMethod("InitAllSettings", ppController));

                    if (ppController.distortScreenUVMode)
                    {
                        DrawPropertyWithReset("distortTextureMidValue", "扭曲贴图中间值",
                            () => ReflectMethod("SetTexture", ppController));
                    }

                    DrawPropertyWithReset("distortSpeedTexSt", "扭曲贴图缩放平移");
                    DrawPropertyWithReset("distortSpeedIntensity", "扭曲强度");

                    if (!ppController.distortScreenUVMode)
                    {
                        DrawPropertyWithReset("distortSpeedPosition", "扭曲过渡位置");
                        DrawPropertyWithReset("distortSpeedRange", "扭曲过渡范围");
                    }

                    DrawPropertyWithReset("distortSpeedMoveSpeedX", "扭曲纹理流动X");
                    DrawPropertyWithReset("distortSpeedMoveSpeed", "扭曲纹理流动Y");
                });


            SerializedProperty radialBlurToggleProp = serializedObject.FindProperty("radialBlurToggle");
            DrawToggleFoldOut(ppController.AnimBools[2], "径向模糊", radialBlurToggleProp,
                RadialBlurPropertyNames, drawEndChangeCheck:
                isChangeToggle => { ReflectMethod("InitAllSettings", ppController); },
                drawBlock: isToggle =>
                {
                    DrawPropertyWithReset("radialBlurSampleCount", "采样次数");
                    DrawPropertyWithReset("radialBlurFromDistort", "材质扰动径向模糊强度",
                        () => ReflectMethod("SetUVFromDistort", ppController));
                    DrawPropertyWithReset("radialBlurIntensity", "后处理径向模糊强度");
                    DrawPropertyWithReset("radialBlurPos", "后处理径向模糊过渡位置");
                    DrawPropertyWithReset("radialBlurRange", "后处理径向模糊过渡范围");
                });

            SerializedProperty overlayTextureToggleProp = serializedObject.FindProperty("overlayTextureToggle");
            DrawToggleFoldOut(ppController.AnimBools[4], "肌理叠加图", overlayTextureToggleProp,
                OverlayTexturePropertyNames,
                drawEndChangeCheck: isChangeToggle => { ReflectMethod("InitAllSettings", ppController); },
                drawBlock: isToggle =>
                {
                    DrawPropertyWithReset("overlayTextureBlendMode", "肌理图混合模式");
                    DrawPropertyWithReset("overlayTexturePolarCoordMode", "肌理图极坐标模式",
                        () => ReflectMethod("SetTexture", ppController));
                    DrawPropertyWithReset("overlayTexture", "肌理图",
                        () => ReflectMethod("SetTexture", ppController));
                    DrawPropertyWithReset("overlayTextureSt", "肌理图缩放平移");
                    DrawPropertyWithReset("overlayTextureAnim", "肌理图偏移动画");
                    DrawPropertyWithReset("overlayTextureIntensity", "肌理图强度");
                    DrawPropertyWithReset("overlayMaskTexture", "肌理蒙版图",
                        () => ReflectMethod("SetTexture", ppController));
                    DrawPropertyWithReset("overlayMaskTextureSt", "肌理图蒙版缩放平移");
                });

            SerializedProperty flashToggleProp = serializedObject.FindProperty("flashToggle");
            DrawToggleFoldOut(ppController.AnimBools[5], "反闪", flashToggleProp, FlashPropertyNames,
                drawEndChangeCheck:
                isChangeToggle => { ReflectMethod("InitAllSettings", ppController); },
                drawBlock: isToggle =>
                {
                    DrawPropertyWithReset("flashIntensity", "反转效果强度");
                    DrawPropertyWithReset("flashGradientRange", "过渡起始亮度");
                    DrawPropertyWithReset("flashContrast", "过渡范围");
                    DrawPropertyWithReset("flashColor", "亮部闪颜色");
                    DrawPropertyWithReset("blackFlashColor", "暗部闪颜色");
                    DrawPropertyWithReset("flashInvertIntensity", "反转度");

                    SerializedProperty flashTextureToggleProp =
                        serializedObject.FindProperty("flashTextureToggle");
                    DrawToggleFoldOut(ppController.AnimBools[7], "反闪纹理", flashTextureToggleProp,
                        FlashTextureFeaturePropertyNames,
                        drawEndChangeCheck:
                        isChangeToggle => { ReflectMethod("InitAllSettings", ppController); },
                        drawBlock: isTextureToggle =>
                        {
                            DrawFoldOut(ppController.AnimBools[8], "flashTexture", "纹理",
                                FlashTexturePropertyNames,
                                () => ReflectMethod("SetTexture", ppController), () =>
                                {
                                    DrawPropertyWithReset("flashTexture", "反闪纹理图",
                                        () => ReflectMethod("SetTexture", ppController));
                                    DrawPropertyWithReset("flashTexturePolarCoordMode", "极坐标模式",
                                        () => ReflectMethod("SetTexture", ppController));
                                    DrawPropertyWithReset("flashTextureScaleOffset", "缩放平移");
                                    DrawPropertyWithReset("flashVec", "偏移速度");
                                    DrawPropertyWithReset("flashDeSaturateIntensity", "纹理图Pow");
                                    DrawPropertyWithReset("flashTextureIntensity", "纹理图混合程度");
                                });

                            DrawFoldOut(ppController.AnimBools[9], "flashTextureMask", "遮罩",
                                FlashTextureMaskPropertyNames,
                                null, () =>
                                {
                                    DrawPropertyWithReset("flashTextureMaskIntensity", "遮罩强度");
                                    DrawVector2ComponentWithReset("flashVecZW", 0, "遮罩过渡位置", 0f);
                                    DrawVector2ComponentWithReset("flashVecZW", 1, "遮罩过渡范围", 0f);
                                });
                        });
                });

            SerializedProperty vignetteToggleProp = serializedObject.FindProperty("vignetteToggle");
            DrawToggleFoldOut(ppController.AnimBools[6], "暗角", vignetteToggleProp, VignettePropertyNames,
                drawEndChangeCheck:
                isChangeToggle => { ReflectMethod("InitAllSettings", ppController); },
                drawBlock: isToggle =>
                {
                    DrawPropertyWithReset("vignetteColor", "暗角颜色");
                    DrawPropertyWithReset("vignetteIntensity", "暗角强度");
                    DrawPropertyWithReset("vignetteRoundness", "暗角圆度");
                    DrawPropertyWithReset("vignetteSmothness", "暗角平滑度");
                    DrawPropertyWithReset("vignetteFill", "暗角填充度");
                });

#if CINIMACHINE_3_0
            SerializedProperty cameraShakeToggleProp = serializedObject.FindProperty("cameraShakeToggle");
            DrawToggleFoldOut(ppController.AnimBools[3], "震屏", cameraShakeToggleProp,
                CameraShakePropertyNames, drawEndChangeCheck:
                isChangeToggle => { ReflectMethod("InitAllSettings", ppController); },
                drawBlock: isToggle =>
                {
                    DrawPropertyWithReset("cinemachineCamera", "绑定Cinemachine相机", () =>
                    {
                        serializedObject.ApplyModifiedProperties();
                        ppController.InitCinemachineCamera();
                    });
                    DrawPropertyWithReset("cameraShakeIntensity", "相机震动强度");
                });
#endif

            if (GUILayout.Button(NBPostProcessingLocalization.MakeInspectorContent(
                    "button.selectManager", "选择当前Manager")))
            {
                ReflectMethod("FindManager", ppController);
            }
#if CINIMACHINE_3_0
            if (GUILayout.Button(NBPostProcessingLocalization.MakeInspectorContent(
                    "button.selectCinemachineCamera", "选择当前CinemachineCamera")))
            {
                ppController.FindVirtualCamera();
            }
#endif

            serializedObject.ApplyModifiedProperties();

        }

        public void DrawToggleFoldOut(AnimBool foldOutAnimBool, string label, SerializedProperty boolProperty,
            string[] resetPropertyNames,
            bool isIndentBlock = true,
            FontStyle fontStyle = FontStyle.Bold,
            Action<bool> drawBlock = null, Action<bool> drawEndChangeCheck = null)
        {
            GUIContent content = NBPostProcessingLocalization.MakeInspectorContent(
                "foldout." + boolProperty.name, label);
            if (fontStyle == FontStyle.Bold)
            {
                EditorGUILayout.Space();
            }

            EditorGUILayout.BeginHorizontal();
            var rect = EditorGUILayout.GetControlRect();

            ShaderGUIItem.SplitControlAndResetRect(rect, out Rect propertyRect, out Rect resetRect, false);
            var foldoutRect = new Rect(propertyRect.x, propertyRect.y, propertyRect.width, propertyRect.height);
            var labelRect = new Rect(propertyRect.x + 18f, propertyRect.y,
                Mathf.Max(0f, propertyRect.width - 18f), propertyRect.height);

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(propertyRect, boolProperty, GUIContent.none);
            if (EditorGUI.EndChangeCheck())
            {
                drawEndChangeCheck?.Invoke(boolProperty.boolValue);
            }

            foldOutAnimBool.target = EditorGUI.Foldout(foldoutRect, foldOutAnimBool.target, string.Empty, true);
            var origFontStyle = EditorStyles.label.fontStyle;
            EditorStyles.label.fontStyle = fontStyle;
            EditorGUI.LabelField(labelRect, content);
            EditorStyles.label.fontStyle = origFontStyle;

            if (DrawResetButton(resetRect, IsAnyPropertyModified(resetPropertyNames),
                    () => ResetProperties(resetPropertyNames)))
            {
                drawEndChangeCheck?.Invoke(boolProperty.boolValue);
            }

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

        private void DrawFoldOut(AnimBool foldOutAnimBool, string localizationKey, string label,
            string[] resetPropertyNames, Action onReset, Action drawBlock)
        {
            GUIContent content = NBPostProcessingLocalization.MakeInspectorContent(
                "foldout." + localizationKey, label);
            EditorGUILayout.BeginHorizontal();
            Rect rect = EditorGUILayout.GetControlRect();
            ShaderGUIItem.SplitControlAndResetRect(rect, out Rect propertyRect, out Rect resetRect, false);

            foldOutAnimBool.target = EditorGUI.Foldout(propertyRect, foldOutAnimBool.target, content, true);
            if (DrawResetButton(resetRect, IsAnyPropertyModified(resetPropertyNames),
                    () => ResetProperties(resetPropertyNames)))
            {
                onReset?.Invoke();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel++;
            float faded = foldOutAnimBool.faded;
            if (faded == 0) faded = 0.00001f;
            EditorGUILayout.BeginFadeGroup(faded);
            drawBlock?.Invoke();
            EditorGUILayout.EndFadeGroup();
            EditorGUI.indentLevel--;
        }

        private static string[] BuildControllerPropertyNames()
        {
            var propertyNames = new List<string> { "customScreenCenterTransform", "customScreenCenterPos" };
            propertyNames.AddRange(ChromaticAberrationPropertyNames);
            propertyNames.AddRange(DistortPropertyNames);
            propertyNames.AddRange(RadialBlurPropertyNames);
            propertyNames.AddRange(OverlayTexturePropertyNames);
            propertyNames.AddRange(FlashPropertyNames);
            propertyNames.AddRange(VignettePropertyNames);
#if CINIMACHINE_3_0
            propertyNames.AddRange(CameraShakePropertyNames);
#endif
            return propertyNames.ToArray();
        }

        private void EnsureDefaultValuesObject()
        {
            if (_defaultValuesObject != null && _defaultValuesSerializedObject != null)
            {
                return;
            }

            _defaultValuesObject = new GameObject("PostProcessingController Defaults")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            _defaultValuesObject.SetActive(false);

            // The inactive, disabled component provides the real field-initializer defaults without running its lifecycle.
            PostProcessingController defaultController =
                _defaultValuesObject.AddComponent<PostProcessingController>();
            defaultController.enabled = false;
            _defaultValuesSerializedObject = new SerializedObject(defaultController);
            _defaultValuesSerializedObject.Update();
        }

        private void DrawResetHeader(GUIContent content, string[] propertyNames, Action onReset)
        {
            EditorGUILayout.Space();
            Rect rect = EditorGUILayout.GetControlRect();
            ShaderGUIItem.SplitControlAndResetRect(rect, out Rect labelRect, out Rect resetRect, false);
            EditorGUI.LabelField(labelRect, content, EditorStyles.boldLabel);

            if (DrawResetButton(resetRect, IsAnyPropertyModified(propertyNames),
                    () => ResetProperties(propertyNames)))
            {
                onReset?.Invoke();
            }
        }

        private void DrawPropertyWithReset(string propertyName, string label, Action onChanged = null,
            string tooltip = null)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            GUIContent content = NBPostProcessingLocalization.MakeInspectorContent(
                "property." + propertyName, label, tooltip ?? string.Empty);
            float propertyHeight = EditorGUI.GetPropertyHeight(property, content, true);
            Rect rect = EditorGUILayout.GetControlRect(true, propertyHeight);
            ShaderGUIItem.SplitControlAndResetRect(rect, out Rect propertyRect, out Rect resetRect, false);
            resetRect.height = EditorGUIUtility.singleLineHeight;

            EditorGUI.BeginChangeCheck();
            if (property.propertyType == SerializedPropertyType.Enum)
            {
                string[] options = NBPostProcessingLocalization.GetInspectorOptions(
                    "property." + propertyName, property.enumDisplayNames);
                Rect popupRect = EditorGUI.PrefixLabel(propertyRect, content);
                property.enumValueIndex = EditorGUI.Popup(popupRect, property.enumValueIndex, options);
            }
            else
            {
                EditorGUI.PropertyField(propertyRect, property, content, true);
            }
            bool changed = EditorGUI.EndChangeCheck();

            if (DrawResetButton(resetRect, IsPropertyModified(property), () => ResetProperty(property)))
            {
                changed = true;
            }

            if (changed)
            {
                onChanged?.Invoke();
            }
        }

        private void DrawVector2ComponentWithReset(string propertyName, int componentIndex, string label,
            float minValue)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            SerializedProperty defaultProperty =
                _defaultValuesSerializedObject.FindProperty(property.propertyPath);
            GUIContent content = NBPostProcessingLocalization.MakeInspectorContent(
                "property." + propertyName + "." + componentIndex, label);
            Vector2 value = property.vector2Value;
            Vector2 defaultValue = defaultProperty.vector2Value;
            float componentValue = componentIndex == 0 ? value.x : value.y;
            float defaultComponentValue = componentIndex == 0 ? defaultValue.x : defaultValue.y;

            Rect rect = EditorGUILayout.GetControlRect();
            ShaderGUIItem.SplitControlAndResetRect(rect, out Rect propertyRect, out Rect resetRect, false);
            EditorGUI.BeginChangeCheck();
            float editedComponentValue = EditorGUI.FloatField(propertyRect, content, componentValue);
            bool changed = EditorGUI.EndChangeCheck() || editedComponentValue < minValue;
            componentValue = Mathf.Max(minValue, editedComponentValue);

            if (DrawResetButton(resetRect, !Mathf.Approximately(componentValue, defaultComponentValue),
                    () => componentValue = defaultComponentValue))
            {
                changed = true;
            }

            if (changed)
            {
                if (componentIndex == 0)
                {
                    value.x = componentValue;
                }
                else
                {
                    value.y = componentValue;
                }

                property.vector2Value = value;
            }
        }

        private static bool DrawResetButton(Rect rect, bool hasModified, Action resetAction)
        {
            GUIContent content = hasModified ? ResetContent : GUIContent.none;
            GUIStyle style = hasModified ? GUI.skin.button : GUI.skin.label;
            using (new EditorGUI.DisabledScope(!hasModified))
            {
                if (GUI.Button(rect, content, style))
                {
                    resetAction?.Invoke();
                    GUI.changed = true;
                    return true;
                }
            }

            return false;
        }

        private bool IsAnyPropertyModified(string[] propertyNames)
        {
            for (int i = 0; i < propertyNames.Length; i++)
            {
                SerializedProperty property = serializedObject.FindProperty(propertyNames[i]);
                if (property != null && IsPropertyModified(property))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsPropertyModified(SerializedProperty property)
        {
            SerializedProperty defaultProperty =
                _defaultValuesSerializedObject.FindProperty(property.propertyPath);
            return defaultProperty != null && !SerializedProperty.DataEquals(property, defaultProperty);
        }

        private void ResetProperties(string[] propertyNames)
        {
            for (int i = 0; i < propertyNames.Length; i++)
            {
                SerializedProperty property = serializedObject.FindProperty(propertyNames[i]);
                if (property != null)
                {
                    ResetProperty(property);
                }
            }
        }

        private void ResetProperty(SerializedProperty property)
        {
            SerializedProperty defaultProperty =
                _defaultValuesSerializedObject.FindProperty(property.propertyPath);
            if (defaultProperty == null)
            {
                return;
            }

            serializedObject.CopyFromSerializedPropertyIfDifferent(defaultProperty);
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
