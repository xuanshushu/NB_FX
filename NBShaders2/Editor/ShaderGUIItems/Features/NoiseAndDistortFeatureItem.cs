using System;
using NBShader;
using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    internal sealed class NoiseAndDistortFeatureItem : FeatureToggleFoldOutItem
    {
        private static readonly string[] DistortModeNames = { "FlowMap/RG贴图", "折射率" };
        private static readonly string[] ScreenDistortModeNames = { "No Screen Distort", "Deferred Distort", "Camera Opaque Distort" };

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
                "_SCREEN_DISTORT_MODE");
            ShaderGUISliderItem screenDistortIntensityItem = new ShaderGUISliderItem(
                rootItem,
                this,
                TierVisible(
                    rootItem,
                    "_SCREEN_DISTORT_MODE",
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
                    "_SCREEN_DISTORT_MODE",
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
                    "_SCREEN_DISTORT_MODE",
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
                NBShaderFlags.FLAG_BIT_PARTICLE_1_NOISE_MASKMAP,
                1,
                parent: this,
                keyword: "_NOISE_MASKMAP");
            AddTextureWithWrap(rootItem, noiseMaskBlock, "_NoiseMaskMap", "扭曲遮罩贴图", NBShaderFlags.FLAG_BIT_WRAPMODE_NOISE_MASKMAP);
            new ColorChannelSelectItem(rootItem, noiseMaskBlock, NBShaderFlags.FLAG_BIT_COLOR_CHANNEL_POS_0_NOISE_MASK, 0, () => Content("扭曲遮罩图通道选择"));
            new UVModeSelectItem(rootItem, noiseMaskBlock, "_NoiseMaskUVModeFoldOut", NBShaderFlags.FLAG_BIT_UVMODE_POS_0_NOISE_MASK_MAP, 0, () => Content("扭曲遮罩贴图UV来源"), "_NoiseMaskMap");

            PropertyToggleBlockItem chromaticBlock = ToggleBlock(
                rootItem,
                "_ChromaticAberrationFoldOut",
                "_Distortion_Choraticaberrat_Toggle",
                "扭曲色散",
                NBShaderFlags.FLAG_BIT_PARTICLE_CHORATICABERRAT,
                parent: this,
                keyword: "_CHROMATIC_ABERRATION",
                bold: true);
            ShaderGUIItem chromaticNoiseAffect = new NoiseAffectItem(rootItem, chromaticBlock);
            new ToggleItem(
                rootItem,
                chromaticNoiseAffect,
                "_Distortion_Choraticaberrat_WithNoise_Toggle",
                () => Content("色散强度受扭曲强度影响"),
                enabled => rootItem.SyncService.ApplyToggleFlag(NBShaderFlags.FLAG_BIT_PARTICLE_NOISE_CHORATICABERRAT_WITH_NOISE, enabled));
            new VectorComponentItem(rootItem, chromaticBlock, "_DistortionDirection", 2, () => Content("色散强度"), false);
            new CustomDataSelectItem(rootItem, chromaticBlock, NBShaderFlags.FLAGBIT_POS_0_CUSTOMDATA_CHORATICABERRAT_INTENSITY, 0, () => Content("色散强度自定义曲线"));
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
    }
}
