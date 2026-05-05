using System;
using NBShader;
using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    internal sealed class ColorBlendFeatureItem : FeatureToggleFoldOutItem
    {
        private static readonly string[] OnOffNames = { "关闭", "开启" };

        public ColorBlendFeatureItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem)
            : base(rootItem, parentItem, "_ColorBlendBlockFoldOut", "_ColorBlendMap_Toggle", "渐变(颜色相乘)", keyword: "_COLORMAPBLEND")
        {
            AddTextureWithWrap(rootItem, this, "_ColorBlendMap", "颜色渐变贴图", NBShaderFlags.FLAG_BIT_WRAPMODE_COLORBLENDMAP, "_ColorBlendColor");
            new UVModeSelectItem(rootItem, this, "_ColorBlendUVModeFoldOut", NBShaderFlags.FLAG_BIT_UVMODE_POS_0_COLOR_BLEND_MAP, 0, () => Content("颜色渐变贴图UV来源"), "_ColorBlendMap");
            new CustomDataSelectItem(rootItem, this, NBShaderFlags.FLAGBIT_POS_3_CUSTOMDATA_COLOR_BLEND_OFFSET_X, 3, () => Content("颜色渐变贴图X轴偏移自定义曲线"));
            new CustomDataSelectItem(rootItem, this, NBShaderFlags.FLAGBIT_POS_3_CUSTOMDATA_COLOR_BLEND_OFFSET_Y, 3, () => Content("颜色渐变贴图Y轴偏移自定义曲线"));
            new VectorComponentItem(rootItem, this, "_ColorBlendVec", 3, () => Content("颜色渐变贴图旋转"), true, 0f, 360f);
            new Vector2LineItem(rootItem, this, "_ColorBlendMapOffset", true, () => Content("颜色渐变贴图偏移速度"));
            ShaderGUIItem colorBlendNoiseAffect = new NoiseAffectItem(rootItem, this);
            new VectorComponentItem(rootItem, colorBlendNoiseAffect, "_ColorBlendVec", 0, () => Content("颜色渐变扭曲强度"), true);
            new FeaturePopupItem(rootItem, this, "_ColorBlendAlphaMultiplyMode", () => Content("颜色渐变图Alpha作用"), OnOffNames,
                property => rootItem.SyncService.ApplyToggleFlag(NBShaderFlags.FLAG_BIT_PARTICLE_COLOR_BLEND_ALPHA_MULTIPLY_MODE, property.floatValue > 0.5f));
            new VectorComponentItem(rootItem, this, "_ColorBlendVec", 2, () => Content("颜色渐变图Alpha强度"), true);
            InitTriggerByChild();
        }
    }
}
