using System;
using NBShader;
using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    internal sealed class MaskFeatureItem : FeatureToggleFoldOutItem
    {
        private static readonly string[] TextureGradientNames = { "贴图", "渐变" };

        public MaskFeatureItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem)
            : base(rootItem, parentItem, "_MaskBlockFoldOut", "_Mask_Toggle", "遮罩", keyword: "_MASKMAP_ON")
        {
            new ToggleItem(rootItem, this, "_NB_Debug_Mask", () => Content("测试遮罩颜色"), enabled => rootItem.SyncService.ApplyToggleKeyword("NB_DEBUG_MASK", enabled));
            new VectorComponentItem(rootItem, this, "_MaskMapVec", 0, () => Content("遮罩强度"), true);

            PropertyToggleBlockItem refineBlock = ToggleBlock(
                rootItem,
                "_MaskRefineFoldOut",
                "_MaskRefineToggle",
                "遮罩整体调整",
                W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_MASK_REFINE,
                1,
                parent: this);
            new VectorComponentItem(rootItem, refineBlock, "_MaskRefineVec", 0, () => Content("范围(Pow)"), false);
            new VectorComponentItem(rootItem, refineBlock, "_MaskRefineVec", 1, () => Content("相乘"), false);
            new VectorComponentItem(rootItem, refineBlock, "_MaskRefineVec", 2, () => Content("偏移(相加)"), false);

            new PNoiseBlendModeItem(rootItem, this, W9ParticleShaderFlags.FLAG_BIT_PNOISE_BLEND_POS_0_MASK, "_MaskPNoiseBlendOpacity", () => Content("遮罩程序噪波混合"),
                () => rootItem.Context.ProgramNoiseEnabled == MixedBool.True);
            AddMaskMap(rootItem, this, "_MaskMap", "_MaskMapGradientToggle", "_MaskUVModeFoldOut", "遮罩",
                W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_MASKMAP,
                W9ParticleShaderFlags.FLAG_BIT_COLOR_CHANNEL_POS_0_MASKMAP1,
                W9ParticleShaderFlags.FLAG_BIT_UVMODE_POS_0_MASKMAP,
                W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_MASKMAP_GRADIENT,
                1,
                "_MaskMapFoldOut");
            new CustomDataSelectItem(rootItem, this, W9ParticleShaderFlags.FLAGBIT_POS_0_CUSTOMDATA_MASK_OFFSET_X, 0, () => Content("Mask图X轴偏移自定义曲线"));
            new CustomDataSelectItem(rootItem, this, W9ParticleShaderFlags.FLAGBIT_POS_0_CUSTOMDATA_MASK_OFFSET_Y, 0, () => Content("Mask图Y轴偏移自定义曲线"));
            new Vector2LineItem(rootItem, this, "_MaskMapOffsetAnition", true, () => Content("遮罩偏移速度"));
            ShaderGUISliderItem maskMapUVRotationItem = new ShaderGUISliderItem(rootItem, this)
            {
                PropertyName = "_MaskMapUVRotation",
                GuiContent = Content("遮罩旋转"),
                Min = 0f,
                Max = 360f
            };
            maskMapUVRotationItem.InitTriggerByChild();
            PropertyToggleBlockItem rotateBlock = ToggleBlock(
                rootItem,
                "_MaskRotationFoldOut",
                "_Mask_RotationToggle",
                "遮罩旋转速度",
                W9ParticleShaderFlags.FLAG_BIT_PARTILCE_MASKMAPROTATIONANIMATION_ON,
                parent: this);
            ShaderGUIFloatItem maskMapRotationSpeedItem = new ShaderGUIFloatItem(rootItem, rotateBlock)
            {
                PropertyName = "_MaskMapRotationSpeed",
                GuiContent = Content("旋转速度")
            };
            maskMapRotationSpeedItem.InitTriggerByChild();
            ShaderGUIItem maskNoiseAffect = new NoiseAffectItem(rootItem, this);
            ShaderGUISliderItem maskDistortionIntensityItem = new ShaderGUISliderItem(rootItem, maskNoiseAffect)
            {
                PropertyName = "_MaskDistortion_intensity",
                GuiContent = Content("遮罩扭曲强度"),
                RangePropertyName = "MaskDistortionIntensityRangeVec"
            };
            maskDistortionIntensityItem.InitTriggerByChild();

            PropertyToggleBlockItem mask2Block = ToggleBlock(
                rootItem,
                "_Mask2BlockFoldOut",
                "_Mask2_Toggle",
                "遮罩2",
                W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_MASK_MAP2,
                1,
                parent: this);
            AddMaskMap(rootItem, mask2Block, "_MaskMap2", "_MaskMap2GradientToggle", "_Mask2UVModeFoldOut", "遮罩2",
                W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_MASKMAP2,
                W9ParticleShaderFlags.FLAG_BIT_COLOR_CHANNEL_POS_0_MASKMAP2,
                W9ParticleShaderFlags.FLAG_BIT_UVMODE_POS_0_MASKMAP_2,
                W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_MASKMAP_2_GRADIENT,
                1);
            new VectorComponentItem(rootItem, mask2Block, "_MaskMapVec", 1, () => Content("遮罩2旋转"), false);
            new Vector2LineItem(rootItem, mask2Block, "_MaskMapOffsetAnition", false, () => Content("遮罩2偏移速度"));

            PropertyToggleBlockItem mask3Block = ToggleBlock(
                rootItem,
                "_Mask3BlockFoldOut",
                "_Mask3_Toggle",
                "遮罩3",
                W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_MASK_MAP3,
                1,
                parent: this);
            AddMaskMap(rootItem, mask3Block, "_MaskMap3", "_MaskMap3GradientToggle", "_Mask3UVModeFoldOut", "遮罩3",
                W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_MASKMAP3,
                W9ParticleShaderFlags.FLAG_BIT_COLOR_CHANNEL_POS_0_MASKMAP3,
                W9ParticleShaderFlags.FLAG_BIT_UVMODE_POS_0_MASKMAP_3,
                W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_MASKMAP_3_GRADIENT,
                1);
            new VectorComponentItem(rootItem, mask3Block, "_MaskMapVec", 2, () => Content("遮罩3旋转"), false);
            new Vector2LineItem(rootItem, mask3Block, "_MaskMap3OffsetAnition", true, () => Content("遮罩3偏移速度"));
            InitTriggerByChild();
        }

        private void AddMaskMap(
            NBShaderRootItem rootItem,
            ShaderGUIItem parent,
            string texturePropertyName,
            string modePropertyName,
            string uvFoldOutPropertyName,
            string label,
            int wrapFlag,
            int colorChannelFlagPos,
            int uvModeFlagPos,
            int gradientFlag,
            int gradientFlagIndex,
            string textureFoldOutPropertyName = null)
        {
            new FeaturePopupItem(rootItem, parent, modePropertyName, () => Content(label + "模式"), TextureGradientNames,
                property => rootItem.SyncService.ApplyToggleFlag(gradientFlag, property.floatValue > 0.5f, gradientFlagIndex));
            ShaderGUIItem textureParent = parent;
            if (string.IsNullOrEmpty(textureFoldOutPropertyName))
            {
                AddTextureWithWrap(rootItem, parent, texturePropertyName, label + "贴图", wrapFlag,
                    isVisible: () => IsPropertyMode(rootItem, modePropertyName, 0));
            }
            else
            {
                textureParent = AddTextureWithRelatedFoldOut(rootItem, parent, texturePropertyName, label + "贴图", textureFoldOutPropertyName, wrapFlag,
                    isVisible: () => IsPropertyMode(rootItem, modePropertyName, 0));
            }

            new ColorChannelSelectItem(rootItem, textureParent, colorChannelFlagPos, 0, () => Content(label + "通道选择"),
                () => IsPropertyMode(rootItem, modePropertyName, 0));
            AddAlphaGradient(rootItem, parent, label + "渐变", texturePropertyName + "GradientCount", texturePropertyName + "GradientFloat",
                () => IsPropertyMode(rootItem, modePropertyName, 1));
            new TextureScaleOffsetItem(rootItem, parent, texturePropertyName, false, () => IsPropertyMode(rootItem, modePropertyName, 1), TillingContent, OffsetContent);
            new WrapModeItem(rootItem, parent, wrapFlag, () => Content(label + "UV Wrap"), 2,
                () => IsPropertyMode(rootItem, modePropertyName, 1));
            new UVModeSelectItem(rootItem, parent, uvFoldOutPropertyName, uvModeFlagPos, 0, () => Content(label + "UV来源"), texturePropertyName);
        }

        private static void AddAlphaGradient(
            NBShaderRootItem rootItem,
            ShaderGUIItem parent,
            string label,
            string countPropertyName,
            string alphaPrefix,
            Func<bool> isVisible = null)
        {
            new GradientItem(
                rootItem,
                parent,
                countPropertyName,
                6,
                Array.Empty<string>(),
                BuildPropertyNames(alphaPrefix, 3),
                () => Content(label),
                false,
                ColorSpace.Gamma,
                isVisible);
        }
    }
}
