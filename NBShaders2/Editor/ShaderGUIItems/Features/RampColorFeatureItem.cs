using System;
using NBShader;
using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    internal sealed class RampColorFeatureItem : FeatureToggleFoldOutItem
    {
        private static readonly string[] RampSourceNames = { "渐变", "贴图" };
        private static readonly string[] BlendModeNames = { "相乘", "相加" };

        public RampColorFeatureItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem, bool graphSharedMode=false)
            : base(rootItem, parentItem, "_RampColorBlockFoldOut", "_RampColorToggle", "颜色映射(Ramp)", keyword: graphSharedMode ? null : "_COLOR_RAMP",
                onValueChanged: graphSharedMode ? (Action<bool>)(_ => rootItem.SyncService.TryApplyGraphColorRampIntentEdit()) : null)
        {
            Func<bool> isRampMapVisible = TierVisible(rootItem, "_COLOR_RAMP_MAP", () => IsPropertyMode(rootItem, "_RampColorSourceMode", 1));
            new FeaturePopupItem(rootItem, this, "_RampColorSourceMode", () => Content("Ramp来源模式"), RampSourceNames,
                _ => { if(graphSharedMode)rootItem.SyncService.TryApplyGraphColorRampIntentEdit();else rootItem.SyncService.SyncMaterialState(); },
                keyword: "_COLOR_RAMP_MAP");
            AddTextureWithWrap(rootItem, this, "_RampColorMap", "颜色映射黑白图", NBShaderFlags.FLAG_BIT_WRAPMODE_RAMP_COLOR_MAP,
                NBShaderFlags.FLAG_BIT_FORCE_NO_MIP_RAMP_COLOR_MAP,
                isVisible: isRampMapVisible);
            new TextureScaleOffsetItem(rootItem, this, "_RampColorMap", false, () => IsPropertyMode(rootItem, "_RampColorSourceMode", 0), TillingContent, OffsetContent);
            new WrapModeItem(rootItem, this, NBShaderFlags.FLAG_BIT_WRAPMODE_RAMP_COLOR_MAP, () => Content("颜色映射UV Wrap"), 2,
                () => IsPropertyMode(rootItem, "_RampColorSourceMode", 0));
            new ColorChannelSelectItem(rootItem, this, NBShaderFlags.FLAG_BIT_COLOR_CHANNEL_POS_0_RAMP_COLOR_MAP, 0, () => Content("颜色映射黑白图通道选择"),
                isRampMapVisible);
            new UVModeSelectItem(rootItem, this, "_RampColorUVModeFoldOut", NBShaderFlags.FLAG_BIT_UVMODE_POS_0_RAMP_COLOR_MAP, 0, () => Content("颜色映射黑白图UV来源"), "_RampColorMap", true, graphFeatureProtocolEdit: graphSharedMode);
            new Vector2LineItem(rootItem, this, "_RampColorMapOffset", true, () => Content("颜色映射贴图偏移速度"));
            new VectorComponentItem(rootItem, this, "_RampColorMapOffset", 3, () => Content("颜色映射贴图旋转"), true, 0f, 360f);
            AddGradient(rootItem, this, "映射颜色", "_RampColorCount", "_RampColor", "_RampColorAlpha", hdr: true);
            new FeaturePopupItem(rootItem, this, "_RampColorBlendMode", () => Content("Ramp颜色混合模式"), BlendModeNames,
                property => { if(graphSharedMode)rootItem.SyncService.TryApplyGraphColorRampBlendEdit(property.floatValue > 0.5f);else rootItem.SyncService.ApplyToggleFlag(NBShaderFlags.FLAG_BIT_PARTICLE_RAMP_COLOR_BLEND_ADD,property.floatValue > 0.5f); });
            new ColorItem(rootItem, this, "_RampColorBlendColor", () => Content("颜色映射叠加颜色"));
            InitTriggerByChild();
        }
    }
}
