using System;
using System.Reflection;
using NBShader;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace NBShaderEditor
{
    internal sealed class NoiseAndDistortFeatureItem : FeatureToggleFoldOutItem
    {
        private static readonly string[] DistortModeNames = { "FlowMap/RG贴图", "折射率" };
        private static readonly string[] ScreenDistortModeNames = { "No Screen Distort", "Deferred Distort", "Camera Opaque Distort" };
        private const int ScreenDistortModeDeferred = 1;
        private const int ScreenDistortModeCameraOpaque = 2;
        private const string ScreenDistortKeyword = "_SCREEN_DISTORT_MODE";
        private const string ScreenDistortModePropertyName = "_ScreenDistortModeToggle";
        private const string RendererDataListPropertyName = "m_RendererDataList";
        private const string LegacyRendererDataPropertyName = "m_RendererData";
        private const string RendererFeaturesPropertyName = "m_RendererFeatures";
        private const string OpaqueTexturePropertyName = "supportsCameraOpaqueTexture";
        private const string OpaqueTextureSerializedPropertyName = "m_RequireOpaqueTexture";
        private const string RendererFeatureActivePropertyName = "isActive";
        private const string RendererFeatureActiveSerializedPropertyName = "m_Active";
        private const string NBPostProcessTypeFullName = "NBShader.NBPostProcess";
        private const string NBPostProcessTypeName = "NBPostProcess";

        public NoiseAndDistortFeatureItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem)
            : base(rootItem, parentItem, "_NoiseBlockFoldOut", "_noisemapEnabled", "扭曲", keyword: "_NOISEMAP")
        {
            new NBShaderKeywordToggleItem(
                rootItem,
                this,
                "_NB_Debug_Distort",
                "NB_DEBUG_DISTORT",
                () => Content("扭曲强度值测试"),
                isVisible: null);
            ShaderGUISliderItem noiseIntensityItem = new ShaderGUISliderItem(rootItem, this)
            {
                PropertyName = "_NoiseIntensity",
                GuiContent = Content("整体扭曲强度"),
                RangePropertyName = "_NoiseIntensityRangeVec"
            };
            noiseIntensityItem.InitTriggerByChild();
            new CustomDataSelectItem(rootItem, this, NBShaderFlags.FLAGBIT_POS_1_CUSTOMDATA_NOISE_INTENSITY, 1, () => Content("扭曲强度自定义曲线"));
            new FeaturePopupItem(rootItem, this, "_ScreenDistortModeToggle", () => Content("屏幕扰动模式"), ScreenDistortModeNames,
                property => rootItem.SyncService.ApplyScreenDistortMode(Mathf.RoundToInt(property.floatValue)),
                () => rootItem.Context.UIEffectEnabled != MixedBool.True,
                ScreenDistortKeyword);
            Func<bool> isDeferredDistortVisible = TierVisible(
                rootItem,
                ScreenDistortKeyword,
                () => IsScreenDistortMode(rootItem, ScreenDistortModeDeferred));
            Func<bool> isCameraOpaqueDistortVisible = TierVisible(
                rootItem,
                ScreenDistortKeyword,
                () => IsScreenDistortMode(rootItem, ScreenDistortModeCameraOpaque));
            Func<bool> isScreenDistortModeNeedsNBPostProcessVisible = () => isDeferredDistortVisible() || isCameraOpaqueDistortVisible();
            Func<bool> isMissingNBPostProcessVisible = () => isScreenDistortModeNeedsNBPostProcessVisible() && !HasActiveNBPostProcessRendererFeature();
            Func<bool> isMissingOpaqueTextureVisible = () => isCameraOpaqueDistortVisible() && !HasCameraOpaqueTextureCopyEnabled();
            new PingableHelpBoxItem(
                rootItem,
                this,
                () => Text(
                    "feature.screenDistort.missingNbPostProcess.message",
                    "Screen Distort requires an active NB Post Process Feature on at least one RendererData in the current URP Pipeline Asset."),
                MessageType.Warning,
                () => ButtonContent("feature.screenDistort.pingRendererData", "Ping Renderer"),
                GetRendererDataForNBPostProcessPing,
                isMissingNBPostProcessVisible);
            new PingableHelpBoxItem(
                rootItem,
                this,
                () => Text(
                    "feature.screenDistort.missingOpaqueTextureCopy.message",
                    "Camera Opaque Distort requires Opaque Texture to be enabled on the current URP Pipeline Asset."),
                MessageType.Warning,
                () => ButtonContent("feature.screenDistort.pingPipelineAsset", "Ping URP Asset"),
                GetCurrentRenderPipelineAsset,
                isMissingOpaqueTextureVisible);
            ShaderGUISliderItem screenDistortIntensityItem = new ShaderGUISliderItem(
                rootItem,
                this,
                TierVisible(
                    rootItem,
                    ScreenDistortKeyword,
                    () => rootItem.Context.UIEffectEnabled != MixedBool.True && IsPropertyGreater(rootItem, "_ScreenDistortModeToggle", 0.5f)))
            {
                PropertyName = "_ScreenDistortIntensity",
                GuiContent = Content("屏幕扭曲强度"),
                RangePropertyName = "_ScreenDistortIntensityRangeVec"
            };
            screenDistortIntensityItem.InitTriggerByChild();
            new ToggleItem(
                rootItem,
                this,
                "_DisableMainPassToggle",
                () => Content("关闭主材质Pass"),
                enabled =>
                {
                    rootItem.SyncService.ApplyScreenDistortMode(GetIntProperty(rootItem, "_ScreenDistortModeToggle"));
                },
                TierVisible(
                    rootItem,
                    ScreenDistortKeyword,
                    () => rootItem.Context.UIEffectEnabled != MixedBool.True && IsPropertyGreater(rootItem, "_ScreenDistortModeToggle", 0.5f)));

            PropertyToggleBlockItem screenAlphaBlock = ToggleBlock(
                rootItem,
                "_ScreenDistortAlphaFoldOut",
                "_ScreenDistortAlphaRefineToggle",
                "屏幕扭曲Alpha整体调整",
                NBShaderFlags.FLAG_BIT_PARTICLE_1_SCREEN_DISTORT_ALPHA_REFINE,
                1,
                parent: this,
                isVisible: TierVisible(
                    rootItem,
                    ScreenDistortKeyword,
                    () => rootItem.Context.UIEffectEnabled != MixedBool.True && IsPropertyGreater(rootItem, "_ScreenDistortModeToggle", 0.5f)));
            ShaderGUIFloatItem screenDistortAlphaPowItem = new ShaderGUIFloatItem(rootItem, screenAlphaBlock)
            {
                PropertyName = "_ScreenDistortAlphaPow",
                GuiContent = Content("范围(Pow)")
            };
            screenDistortAlphaPowItem.InitTriggerByChild();
            ShaderGUIFloatItem screenDistortAlphaMultiItem = new ShaderGUIFloatItem(rootItem, screenAlphaBlock)
            {
                PropertyName = "_ScreenDistortAlphaMulti",
                GuiContent = Content("相乘")
            };
            screenDistortAlphaMultiItem.InitTriggerByChild();
            ShaderGUIFloatItem screenDistortAlphaAddItem = new ShaderGUIFloatItem(rootItem, screenAlphaBlock)
            {
                PropertyName = "_ScreenDistortAlphaAdd",
                GuiContent = Content("偏移(相加)")
            };
            screenDistortAlphaAddItem.InitTriggerByChild();

            new FeaturePopupItem(rootItem, this, "_DistortMode", () => Content("扭曲模式"), DistortModeNames,
                property => rootItem.SyncService.ApplyToggleKeyword("_DISTORT_REFRACTION", property.floatValue > 0.5f),
                keyword: "_DISTORT_REFRACTION");
            TextureRelatedFoldOutItem noiseMapRelatedFoldOut = AddTextureWithRelatedFoldOut(rootItem, this, "_NoiseMap", "扭曲贴图", "_NoiseMapFoldOut", NBShaderFlags.FLAG_BIT_WRAPMODE_NOISEMAP,
                isVisible: () => IsPropertyMode(rootItem, "_DistortMode", 0));
            new UVModeSelectItem(rootItem, noiseMapRelatedFoldOut, "_NoiseUVModeFoldOut", NBShaderFlags.FLAG_BIT_UVMODE_POS_0_NOISE_MAP, 0, () => Content("扭曲贴图UV来源"), "_NoiseMap",
                isVisible: () => IsPropertyMode(rootItem, "_DistortMode", 0));
            new Vector2LineItem(rootItem, noiseMapRelatedFoldOut, "_DistortionDirection", true, () => Content("扭曲方向强度"), () => IsPropertyMode(rootItem, "_DistortMode", 0));
            new CustomDataSelectItem(rootItem, noiseMapRelatedFoldOut, NBShaderFlags.FLAGBIT_POS_2_CUSTOMDATA_NOISE_DIRECTION_X, 2, () => Content("扭曲方向强度X自定义曲线"), () => IsPropertyMode(rootItem, "_DistortMode", 0));
            new CustomDataSelectItem(rootItem, noiseMapRelatedFoldOut, NBShaderFlags.FLAGBIT_POS_2_CUSTOMDATA_NOISE_DIRECTION_Y, 2, () => Content("扭曲方向强度Y自定义曲线"), () => IsPropertyMode(rootItem, "_DistortMode", 0));
            ShaderGUISliderItem noiseMapUVRotationItem = new ShaderGUISliderItem(rootItem, noiseMapRelatedFoldOut, () => IsPropertyMode(rootItem, "_DistortMode", 0))
            {
                PropertyName = "_NoiseMapUVRotation",
                GuiContent = Content("扭曲旋转"),
                Min = 0f,
                Max = 360f
            };
            noiseMapUVRotationItem.InitTriggerByChild();
            new Vector2LineItem(rootItem, noiseMapRelatedFoldOut, "_NoiseOffset", true, () => Content("扭曲偏移速度"), () => IsPropertyMode(rootItem, "_DistortMode", 0));
            new ToggleItem(
                rootItem,
                noiseMapRelatedFoldOut,
                "_DistortionBothDirection_Toggle",
                () => Content("0.5为中值，双向扭曲"),
                enabled => rootItem.SyncService.ApplyToggleFlag(NBShaderFlags.FLAG_BIT_PARTICLE_NOISEMAP_NORMALIZEED_ON, enabled),
                () => IsPropertyMode(rootItem, "_DistortMode", 0));
            ShaderGUISliderItem refractionIorItem = new ShaderGUISliderItem(
                rootItem,
                this,
                TierVisible(rootItem, "_DISTORT_REFRACTION", () => IsPropertyMode(rootItem, "_DistortMode", 1)))
            {
                PropertyName = "_RefractionIOR",
                GuiContent = Content("折射率"),
                Min = 0f,
                Max = 5f
            };
            refractionIorItem.InitTriggerByChild();
            new PNoiseBlendModeItem(rootItem, this, NBShaderFlags.FLAG_BIT_PNOISE_BLEND_POS_0_DISTORT, "_DistortPNoiseBlendOpacity", () => Content("扭曲程序噪波混合"),
                () => rootItem.Context.ProgramNoiseEnabled == MixedBool.True);

            PropertyToggleBlockItem noiseMaskBlock = ToggleBlock(
                rootItem,
                "_NoiseMaskBlockFoldOut",
                "_noiseMaskMap_Toggle",
                "扭曲遮罩",
                parent: this,
                keyword: "_NOISE_MASKMAP");
            AddTextureWithWrap(rootItem, noiseMaskBlock, "_NoiseMaskMap", "扭曲遮罩贴图", NBShaderFlags.FLAG_BIT_WRAPMODE_NOISE_MASKMAP);
            new ColorChannelSelectItem(rootItem, noiseMaskBlock, NBShaderFlags.FLAG_BIT_COLOR_CHANNEL_POS_0_NOISE_MASK, 0, () => Content("扭曲遮罩图通道选择"));
            new UVModeSelectItem(rootItem, noiseMaskBlock, "_NoiseMaskUVModeFoldOut", NBShaderFlags.FLAG_BIT_UVMODE_POS_0_NOISE_MASK_MAP, 0, () => Content("扭曲遮罩贴图UV来源"), "_NoiseMaskMap");

            InitTriggerByChild();
        }

        private static int GetIntProperty(NBShaderRootItem rootItem, string propertyName)
        {
            return rootItem.PropertyInfoDic.TryGetValue(propertyName, out ShaderPropertyInfo info) && !info.Property.hasMixedValue
                ? Mathf.RoundToInt(info.Property.floatValue)
                : 0;
        }

        private static bool IsPropertyGreater(NBShaderRootItem rootItem, string propertyName, float threshold)
        {
            return rootItem.PropertyInfoDic.TryGetValue(propertyName, out ShaderPropertyInfo info) &&
                   !info.Property.hasMixedValue &&
                   info.Property.floatValue > threshold;
        }

        private static bool IsScreenDistortMode(NBShaderRootItem rootItem, int mode)
        {
            return rootItem.Context.UIEffectEnabled != MixedBool.True &&
                   IsPropertyMode(rootItem, ScreenDistortModePropertyName, mode);
        }

        private static bool HasActiveNBPostProcessRendererFeature()
        {
            try
            {
                RenderPipelineAsset pipelineAsset = GetCurrentRenderPipelineAsset();
                if (pipelineAsset == null)
                {
                    return false;
                }

                SerializedObject pipelineObject = new SerializedObject(pipelineAsset);
                SerializedProperty rendererDataList = pipelineObject.FindProperty(RendererDataListPropertyName);
                if (rendererDataList != null && rendererDataList.isArray)
                {
                    for (int i = 0; i < rendererDataList.arraySize; i++)
                    {
                        UnityEngine.Object rendererData = rendererDataList.GetArrayElementAtIndex(i).objectReferenceValue;
                        if (RendererDataHasNBPostProcess(rendererData, requireActive: true))
                        {
                            return true;
                        }
                    }

                    return false;
                }

                SerializedProperty legacyRendererData = pipelineObject.FindProperty(LegacyRendererDataPropertyName);
                return legacyRendererData != null && RendererDataHasNBPostProcess(legacyRendererData.objectReferenceValue, requireActive: true);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool RendererDataHasNBPostProcess(UnityEngine.Object rendererData, bool requireActive)
        {
            if (rendererData == null)
            {
                return false;
            }

            SerializedObject rendererDataObject = new SerializedObject(rendererData);
            SerializedProperty rendererFeatures = rendererDataObject.FindProperty(RendererFeaturesPropertyName);
            if (rendererFeatures == null || !rendererFeatures.isArray)
            {
                return false;
            }

            for (int i = 0; i < rendererFeatures.arraySize; i++)
            {
                UnityEngine.Object rendererFeature = rendererFeatures.GetArrayElementAtIndex(i).objectReferenceValue;
                if (IsNBPostProcessFeature(rendererFeature) &&
                    (!requireActive || IsRendererFeatureActive(rendererFeature)))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsNBPostProcessFeature(UnityEngine.Object rendererFeature)
        {
            if (rendererFeature == null)
            {
                return false;
            }

            for (Type type = rendererFeature.GetType(); type != null; type = type.BaseType)
            {
                if (string.Equals(type.FullName, NBPostProcessTypeFullName, StringComparison.Ordinal) ||
                    string.Equals(type.Name, NBPostProcessTypeName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsRendererFeatureActive(UnityEngine.Object rendererFeature)
        {
            if (rendererFeature == null)
            {
                return false;
            }

            if (TryGetBoolProperty(rendererFeature, RendererFeatureActivePropertyName, out bool isActive))
            {
                return isActive;
            }

            try
            {
                SerializedObject rendererFeatureObject = new SerializedObject(rendererFeature);
                SerializedProperty activeProperty = rendererFeatureObject.FindProperty(RendererFeatureActiveSerializedPropertyName);
                return activeProperty != null &&
                       activeProperty.propertyType == SerializedPropertyType.Boolean &&
                       activeProperty.boolValue;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool HasCameraOpaqueTextureCopyEnabled()
        {
            try
            {
                RenderPipelineAsset pipelineAsset = GetCurrentRenderPipelineAsset();
                if (pipelineAsset == null)
                {
                    return false;
                }

                if (TryGetBoolProperty(pipelineAsset, OpaqueTexturePropertyName, out bool supportsCameraOpaqueTexture))
                {
                    return supportsCameraOpaqueTexture;
                }

                SerializedObject pipelineObject = new SerializedObject(pipelineAsset);
                SerializedProperty opaqueTextureProperty = pipelineObject.FindProperty(OpaqueTextureSerializedPropertyName);
                return opaqueTextureProperty != null &&
                       opaqueTextureProperty.propertyType == SerializedPropertyType.Boolean &&
                       opaqueTextureProperty.boolValue;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static UnityEngine.Object GetRendererDataForNBPostProcessPing()
        {
            try
            {
                RenderPipelineAsset pipelineAsset = GetCurrentRenderPipelineAsset();
                if (pipelineAsset == null)
                {
                    return null;
                }

                SerializedObject pipelineObject = new SerializedObject(pipelineAsset);
                SerializedProperty rendererDataList = pipelineObject.FindProperty(RendererDataListPropertyName);
                if (rendererDataList != null && rendererDataList.isArray)
                {
                    return GetRendererDataForNBPostProcessPing(rendererDataList);
                }

                SerializedProperty legacyRendererData = pipelineObject.FindProperty(LegacyRendererDataPropertyName);
                return legacyRendererData != null ? legacyRendererData.objectReferenceValue : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static UnityEngine.Object GetRendererDataForNBPostProcessPing(SerializedProperty rendererDataList)
        {
            UnityEngine.Object firstRendererData = null;
            UnityEngine.Object rendererDataWithInactiveFeature = null;
            for (int i = 0; i < rendererDataList.arraySize; i++)
            {
                UnityEngine.Object rendererData = rendererDataList.GetArrayElementAtIndex(i).objectReferenceValue;
                if (rendererData == null)
                {
                    continue;
                }

                if (firstRendererData == null)
                {
                    firstRendererData = rendererData;
                }

                if (RendererDataHasNBPostProcess(rendererData, requireActive: true))
                {
                    return rendererData;
                }

                if (rendererDataWithInactiveFeature == null &&
                    RendererDataHasNBPostProcess(rendererData, requireActive: false))
                {
                    rendererDataWithInactiveFeature = rendererData;
                }
            }

            return rendererDataWithInactiveFeature != null ? rendererDataWithInactiveFeature : firstRendererData;
        }

        private static RenderPipelineAsset GetCurrentRenderPipelineAsset()
        {
            return QualitySettings.renderPipeline != null
                ? QualitySettings.renderPipeline
                : GraphicsSettings.currentRenderPipeline;
        }

        private static bool TryGetBoolProperty(UnityEngine.Object target, string propertyName, out bool value)
        {
            value = false;
            if (target == null)
            {
                return false;
            }

            try
            {
                PropertyInfo propertyInfo = target.GetType().GetProperty(
                    propertyName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (propertyInfo == null || propertyInfo.PropertyType != typeof(bool))
                {
                    return false;
                }

                value = (bool)propertyInfo.GetValue(target, null);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static GUIContent ButtonContent(string key, string fallback)
        {
            return NBShaderInspectorLocalization.MakeContent("inspector." + key + ".button", fallback);
        }

        private sealed class PingableHelpBoxItem : ShaderGUIItem
        {
            private const float HelpBoxPadding = 6f;
            private const float HelpBoxButtonGap = 4f;
            private const float MaxButtonWidth = 160f;
            private static readonly GUIContent TempHelpContent = new GUIContent();

            private readonly Func<string> _messageProvider;
            private readonly MessageType _messageType;
            private readonly Func<GUIContent> _buttonContentProvider;
            private readonly Func<UnityEngine.Object> _targetProvider;
            private readonly Func<bool> _isVisible;

            public PingableHelpBoxItem(
                ShaderGUIRootItem rootItem,
                ShaderGUIItem parentItem,
                Func<string> messageProvider,
                MessageType messageType,
                Func<GUIContent> buttonContentProvider,
                Func<UnityEngine.Object> targetProvider,
                Func<bool> isVisible) : base(rootItem, parentItem)
            {
                _messageProvider = messageProvider ?? (() => string.Empty);
                _messageType = messageType;
                _buttonContentProvider = buttonContentProvider ?? (() => GUIContent.none);
                _targetProvider = targetProvider ?? (() => null);
                _isVisible = isVisible;
            }

            public override void OnGUI()
            {
                if (_isVisible != null && !_isVisible())
                {
                    return;
                }

                using (ParentControlDisabledScope())
                {
                    string message = _messageProvider();
                    GUIContent buttonContent = _buttonContentProvider();
                    TempHelpContent.text = message;
                    float width = Mathf.Max(1f, EditorGUIUtility.currentViewWidth + GlobalRectWidthExpansion + GlobalRectXOffset);
                    float messageHeight = Mathf.Max(
                        EditorGUIUtility.singleLineHeight * 2f,
                        EditorStyles.helpBox.CalcHeight(TempHelpContent, width));
                    float buttonHeight = EditorGUIUtility.singleLineHeight;
                    float height = messageHeight + HelpBoxButtonGap + buttonHeight + HelpBoxPadding;
                    Rect rect = ApplyGlobalRectCompensation(LayoutRect(height));
                    EditorGUI.HelpBox(rect, message, _messageType);

                    float buttonWidth = Mathf.Min(
                        MaxButtonWidth,
                        Mathf.Max(90f, EditorStyles.miniButton.CalcSize(buttonContent).x + 18f));
                    buttonWidth = Mathf.Min(buttonWidth, Mathf.Max(0f, rect.width - HelpBoxPadding * 2f));
                    Rect buttonRect = new Rect(
                        rect.xMax - HelpBoxPadding - buttonWidth,
                        rect.yMax - HelpBoxPadding - buttonHeight,
                        buttonWidth,
                        buttonHeight);

                    UnityEngine.Object target = _targetProvider();
                    using (new EditorGUI.DisabledScope(target == null))
                    {
                        if (GUI.Button(buttonRect, buttonContent, EditorStyles.miniButton))
                        {
                            Selection.activeObject = target;
                            EditorGUIUtility.PingObject(target);
                        }
                    }

                    TempHelpContent.text = string.Empty;
                }
            }
        }
    }
}
