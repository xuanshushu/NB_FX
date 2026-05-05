using System;
using NBShader;
using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    internal sealed class DissolveFeatureItem : FeatureToggleFoldOutItem
    {
        private static readonly string[] RampSourceNames = { "渐变", "贴图" };
        private static readonly string[] BlendModeNames = { "叠加", "相乘" };
        private static readonly string[] DissolveMaskModeNames = { "Process Dissolve", "Dissolve Mask" };

        public DissolveFeatureItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem)
            : base(rootItem, parentItem, "_DissolveBlockFoldOut", "_Dissolve_Toggle", "溶解", keyword: "_DISSOLVE")
        {
            new NBShaderKeywordToggleItem(
                rootItem,
                this,
                "_NB_Debug_Dissolve",
                "NB_DEBUG_DISSOLVE",
                () => Content("溶解度黑白值测试"),
                isVisible: null);
            TextureRelatedFoldOutItem dissolveMapRelatedFoldOut = AddTextureWithRelatedFoldOut(rootItem, this, "_DissolveMap", "溶解贴图", "_DissolveMapFoldOut", NBShaderFlags.FLAG_BIT_WRAPMODE_DISSOLVE_MAP);
            new ColorChannelSelectItem(rootItem, dissolveMapRelatedFoldOut, NBShaderFlags.FLAG_BIT_COLOR_CHANNEL_POS_0_DISSOLVE_MAP, 0, () => Content("溶解贴图通道选择"));
            new CustomDataSelectItem(rootItem, dissolveMapRelatedFoldOut, NBShaderFlags.FLAGBIT_POS_1_CUSTOMDATA_DISSOLVE_OFFSET_X, 1, () => Content("溶解贴图X轴偏移自定义曲线"));
            new CustomDataSelectItem(rootItem, dissolveMapRelatedFoldOut, NBShaderFlags.FLAGBIT_POS_1_CUSTOMDATA_DISSOLVE_OFFSET_Y, 1, () => Content("溶解贴图Y轴偏移自定义曲线"));
            new UVModeSelectItem(rootItem, dissolveMapRelatedFoldOut, "_DissolveUVModeFoldOut", NBShaderFlags.FLAG_BIT_UVMODE_POS_0_DISSOLVE_MAP, 0, () => Content("溶解贴图UV来源"), "_DissolveMap");
            new Vector2LineItem(rootItem, dissolveMapRelatedFoldOut, "_DissolveOffsetRotateDistort", true, () => Content("溶解贴图偏移速度"));
            new VectorComponentItem(rootItem, dissolveMapRelatedFoldOut, "_DissolveOffsetRotateDistort", 2, () => Content("溶解贴图旋转"), true, 0f, 360f);
            new PNoiseBlendModeItem(rootItem, this, NBShaderFlags.FLAG_BIT_PNOISE_BLEND_POS_0_DISSOLVE, "_DissolvePNoiseBlendOpacity", () => Content("溶解程序噪波混合"),
                () => rootItem.Context.ProgramNoiseEnabled == MixedBool.True);
            new VectorComponentItem(rootItem, this, "_Dissolve", 1, () => Content("溶解值Pow"), true, 0f, 10f);
            new VectorComponentRangeSliderItem(rootItem, this, "_Dissolve", 0, "DissolveXRangeVec", () => Content("溶解强度"));
            new CustomDataSelectItem(rootItem, this, NBShaderFlags.FLAGBIT_POS_0_CUSTOMDATA_DISSOLVE_INTENSITY, 0, () => Content("溶解强度自定义曲线"));
            new VectorComponentItem(rootItem, this, "_Dissolve", 3, () => Content("溶解硬软度"), true, 0.001f, 1f);
            ShaderGUIItem dissolveNoiseAffect = new NoiseAffectItem(rootItem, this);
            new VectorComponentItem(rootItem, dissolveNoiseAffect, "_DissolveOffsetRotateDistort", 3, () => Content("溶解贴图扭曲强度"), false);

            PropertyToggleBlockItem lineBlock = ToggleBlock(rootItem, "_DissolveLineFoldOut", "_DissolveLineMaskToggle", "溶解描边",
                NBShaderFlags.FLAG_BIT_PARTICLE_1_DISSOLVE_LINE_MASK, 1, parent: this);
            new ColorItem(rootItem, lineBlock, "_DissolveLineColor", () => Content("溶解描边颜色"));
            new VectorComponentRangeSliderItem(rootItem, lineBlock, "_Dissolve_Vec2", 0, "Dissolve2XRangeVec", () => Content("描边位置"));
            new VectorComponentRangeSliderItem(rootItem, lineBlock, "_Dissolve_Vec2", 1, "Dissolve2YRangeVec", () => Content("描边软硬"));

            PropertyToggleBlockItem rampBlock = ToggleBlock(rootItem, "_DissolveRampFoldOut", "_Dissolve_useRampMap_Toggle", "溶解Ramp图功能",
                NBShaderFlags.FLAG_BIT_PARTICLE_1_DISSOVLE_USE_RAMP, 1, parent: this, keyword: "_DISSOLVE_RAMP",
                onValueChanged: _ => rootItem.SyncService.SyncMaterialState());
            Func<bool> isDissolveRampMapVisible = TierVisible(rootItem, "_DISSOLVE_RAMP_MAP", () => IsPropertyMode(rootItem, "_DissolveRampSourceMode", 1));
            new FeaturePopupItem(rootItem, rampBlock, "_DissolveRampSourceMode", () => Content("溶解Ramp模式"), RampSourceNames,
                property => rootItem.SyncService.ApplyToggleFlagAndKeyword(NBShaderFlags.FLAG_BIT_PARTICLE_DISSOLVE_RAMP_MAP, 0, "_DISSOLVE_RAMP_MAP", property.floatValue > 0.5f),
                keyword: "_DISSOLVE_RAMP_MAP");
            AddTextureWithWrap(rootItem, rampBlock, "_DissolveRampMap", "溶解Ramp图", NBShaderFlags.FLAG_BIT_WRAPMODE_DISSOLVE_RAMPMAP, "_DissolveRampColor",
                isDissolveRampMapVisible);
            AddGradient(rootItem, rampBlock, "Ramp颜色", "_DissolveRampCount", "_DissolveRampColor", "_DissolveRampAlpha", hdr: true,
                isVisible: () => IsPropertyMode(rootItem, "_DissolveRampSourceMode", 0));
            new TextureScaleOffsetItem(rootItem, rampBlock, "_DissolveRampMap", false, () => IsPropertyMode(rootItem, "_DissolveRampSourceMode", 0), TillingContent, OffsetContent);
            new ColorItem(rootItem, rampBlock, "_DissolveRampColor", () => Content("Ramp颜色叠加"), () => IsPropertyMode(rootItem, "_DissolveRampSourceMode", 0));
            new WrapModeItem(rootItem, rampBlock, NBShaderFlags.FLAG_BIT_WRAPMODE_DISSOLVE_RAMPMAP, () => Content("溶解RampUV Wrap"), 2,
                () => IsPropertyMode(rootItem, "_DissolveRampSourceMode", 0));
            new FeaturePopupItem(rootItem, rampBlock, "_DissolveRampColorBlendMode", () => Content("溶解Ramp混合模式"), BlendModeNames,
                property => rootItem.SyncService.ApplyToggleFlag(NBShaderFlags.FLAG_BIT_PARTICLE_1_DISSOLVE_RAMP_MULITPLY, property.floatValue > 0.5f, 1));

            PropertyToggleBlockItem maskBlock = ToggleBlock(rootItem, "_DissolveMaskFoldOut", "_DissolveMask_Toggle", "溶解遮罩图(过程溶解)",
                NBShaderFlags.FLAG_BIT_PARTICLE_DISSOLVE_MASK, parent: this, keyword: "_DISSOLVE_MASK");
            new FeaturePopupItem(rootItem, maskBlock, "_DissolveMaskMode", () => Content("溶解遮罩模式"), DissolveMaskModeNames);
            AddTextureWithWrap(rootItem, maskBlock, "_DissolveMaskMap", "溶解遮罩图", NBShaderFlags.FLAG_BIT_WRAPMODE_DISSOLVE_MASKMAP);
            new UVModeSelectItem(rootItem, maskBlock, "_DissolveMaskUVModeFoldOut", NBShaderFlags.FLAG_BIT_UVMODE_POS_0_DISSOLVE_MASK_MAP, 0, () => Content("溶解遮罩图UV来源"), "_DissolveMaskMap");
            new ColorChannelSelectItem(rootItem, maskBlock, NBShaderFlags.FLAG_BIT_COLOR_CHANNEL_POS_0_DISSOLVE_MASK_MAP, 0, () => Content("溶解遮罩图通道选择"));
            new VectorComponentItem(rootItem, maskBlock, "_Dissolve", 2, () => Content("溶解遮罩强度"), false, isVisible: () => !IsDissolveMaskStrengthSlider(rootItem));
            new VectorComponentItem(rootItem, maskBlock, "_Dissolve", 2, () => Content("溶解遮罩强度"), true, 0f, 2f, () => IsDissolveMaskStrengthSlider(rootItem));
            new CustomDataSelectItem(rootItem, maskBlock, NBShaderFlags.FLAGBIT_POS_1_CUSTOMDATA_DISSOLVE_MASK_INTENSITY, 1, () => Content("溶解遮罩图强度自定义曲线"));
            InitTriggerByChild();
        }

        private static bool IsDissolveMaskStrengthSlider(NBShaderRootItem rootItem)
        {
            return rootItem.PropertyInfoDic.TryGetValue("_DissolveMaskMode", out ShaderPropertyInfo info) &&
                   !info.Property.hasMixedValue &&
                   info.Property.floatValue > 0.5f;
        }
    }
}
