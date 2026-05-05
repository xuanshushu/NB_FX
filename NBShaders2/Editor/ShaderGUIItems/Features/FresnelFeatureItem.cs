using System;
using NBShader;
using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    internal sealed class FresnelFeatureItem : FeatureToggleFoldOutItem
    {
        private static readonly string[] FresnelModeNames = { "颜色", "透明" };

        public FresnelFeatureItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem)
            : base(rootItem, parentItem, "_FresnelBlockFoldOut", "_fresnelEnabled", "菲涅尔", NBShaderFlags.FLAG_BIT_PARTICLE_FRESNEL_ON, keyword: "_FRESNEL")
        {
            new NBShaderKeywordToggleItem(
                rootItem,
                this,
                "_NB_Debug_Fresnel",
                "NB_DEBUG_FRESNEL",
                () => Content("菲涅尔测试颜色"),
                isVisible: null);
            new FeaturePopupItem(rootItem, this, "_FresnelMode", () => Content("菲涅尔模式"), FresnelModeNames,
                property => rootItem.SyncService.ApplyToggleFlag(NBShaderFlags.FLAG_BIT_PARTICLE_FRESNEL_FADE_ON, property.floatValue > 0.5f));
            Func<bool> isFresnelColorMode = () => IsPropertyMode(rootItem, "_FresnelMode", 0);
            new ColorItem(rootItem, this, "_FresnelColor", () => Content("菲涅尔颜色"), isFresnelColorMode);
            new VectorComponentItem(rootItem, this, "_FresnelUnit", 2, () => Content("菲涅尔强度"), true);
            new VectorComponentItem(rootItem, this, "_FresnelUnit", 0, () => Content("菲涅尔位置"), true, -1f, 1f);
            new CustomDataSelectItem(rootItem, this, NBShaderFlags.FLAGBIT_POS_0_CUSTOMDATA_FRESNEL_OFFSET, 0, () => Content("菲涅尔位置自定义曲线"));
            new VectorComponentItem(rootItem, this, "_FresnelUnit", 1, () => Content("菲涅尔范围Pow"), true, 0f, 10f);
            new VectorComponentItem(rootItem, this, "_FresnelUnit", 3, () => Content("菲涅尔硬度"), true);
            new ToggleItem(
                rootItem,
                this,
                "_InvertFresnel_Toggle",
                () => Content("翻转菲涅尔"),
                enabled => rootItem.SyncService.ApplyToggleFlag(NBShaderFlags.FLAG_BIT_PARTICLE_FRESNEL_INVERT_ON, enabled));
            new ToggleItem(
                rootItem,
                this,
                "_FresnelColorAffectByAlpha",
                () => Content("菲涅尔颜色受Alpha影响"),
                enabled => rootItem.SyncService.ApplyToggleFlag(NBShaderFlags.FLAG_BIT_PARTICLE_FRESNEL_COLOR_AFFETCT_BY_ALPHA, enabled),
                isFresnelColorMode);
            new VectorComponentItem(rootItem, this, "_FresnelRotation", 0, () => Content("菲涅尔方向偏移X"), false);
            new VectorComponentItem(rootItem, this, "_FresnelRotation", 1, () => Content("菲涅尔方向偏移Y"), false);
            new VectorComponentItem(rootItem, this, "_FresnelRotation", 2, () => Content("菲涅尔方向偏移Z"), false);
            InitTriggerByChild();
        }
    }
}
