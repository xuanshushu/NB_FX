using System;
using NBShader;
using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    internal sealed class RampColorFeatureItem : FeatureToggleFoldOutItem
    {
        private static readonly string[] RampSourceNames = { "渐变", "贴图" };
        private static readonly string[] BlendModeNames = { "叠加", "相乘" };

        public RampColorFeatureItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem)
            : base(rootItem, parentItem, "_RampColorBlockFoldOut", "_RampColorToggle", "颜色映射(Ramp)", keyword: "_COLOR_RAMP")
        {
            Func<bool> isRampMapVisible = TierVisible(rootItem, "_COLOR_RAMP_MAP", () => IsPropertyMode(rootItem, "_RampColorSourceMode", 1));
            new FeaturePopupItem(rootItem, this, "_RampColorSourceMode", () => Content("Ramp来源模式"), RampSourceNames,
                property => rootItem.SyncService.ApplyToggleFlagAndKeyword(NBShaderFlags.FLAG_BIT_PARTICLE_RAMP_COLOR_MAP_MODE_ON, 0, "_COLOR_RAMP_MAP", property.floatValue > 0.5f),
                keyword: "_COLOR_RAMP_MAP");
            AddTextureWithWrap(rootItem, this, "_RampColorMap", "颜色映射黑白图", NBShaderFlags.FLAG_BIT_WRAPMODE_RAMP_COLOR_MAP,
                isVisible: isRampMapVisible);
            new TextureScaleOffsetItem(rootItem, this, "_RampColorMap", false, () => IsPropertyMode(rootItem, "_RampColorSourceMode", 0), TillingContent, OffsetContent);
            new WrapModeItem(rootItem, this, NBShaderFlags.FLAG_BIT_WRAPMODE_RAMP_COLOR_MAP, () => Content("颜色映射UV Wrap"), 2,
                () => IsPropertyMode(rootItem, "_RampColorSourceMode", 0));
            new ColorChannelSelectItem(rootItem, this, NBShaderFlags.FLAG_BIT_COLOR_CHANNEL_POS_0_RAMP_COLOR_MAP, 0, () => Content("颜色映射黑白图通道选择"),
                isRampMapVisible);
            new UVModeSelectItem(rootItem, this, "_RampColorUVModeFoldOut", NBShaderFlags.FLAG_BIT_UVMODE_POS_0_RAMP_COLOR_MAP, 0, () => Content("颜色映射黑白图UV来源"), "_RampColorMap", true, isRampMapVisible);
            new Vector2LineItem(rootItem, this, "_RampColorMapOffset", true, () => Content("颜色映射贴图偏移速度"), isRampMapVisible);
            new VectorComponentItem(rootItem, this, "_RampColorMapOffset", 3, () => Content("颜色映射贴图旋转"), true, 0f, 360f, isRampMapVisible);
            AddGradient(rootItem, this, "映射颜色", "_RampColorCount", "_RampColor", "_RampColorAlpha", hdr: true);
            new FeaturePopupItem(rootItem, this, "_RampColorBlendMode", () => Content("Ramp颜色混合模式"), BlendModeNames,
                property => rootItem.SyncService.ApplyToggleFlag(NBShaderFlags.FLAG_BIT_PARTICLE_RAMP_COLOR_BLEND_ADD, property.floatValue > 0.5f));
            new ColorItem(rootItem, this, "_RampColorBlendColor", () => Content("颜色映射叠加颜色"));
            InitTriggerByChild();
        }
    }
}
