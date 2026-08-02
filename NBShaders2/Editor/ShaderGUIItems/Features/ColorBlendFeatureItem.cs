using NBShader;

namespace NBShaderEditor
{
    internal sealed class ColorBlendFeatureItem : ColorOverlayFeatureItem
    {
        public ColorBlendFeatureItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem)
            : base(
                rootItem,
                parentItem,
                label: "叠加贴图 2",
                foldOutPropertyName: "_ColorBlendBlockFoldOut",
                togglePropertyName: "_ColorBlendMap_Toggle",
                keyword: "_COLORMAPBLEND",
                blendModePropertyName: "_ColorBlendMode",
                blendModeFlag: NBShaderFlags.FLAG_BIT_PARTICLE_1_COLOR_OVERLAY_2_ADD,
                blendModeFlagIndex: 1,
                blendModeFlagEnabledMode: 0,
                texturePropertyName: "_ColorBlendMap",
                colorPropertyName: "_ColorBlendColor",
                wrapFlag: NBShaderFlags.FLAG_BIT_WRAPMODE_COLORBLENDMAP,
                forceNoMipFlag: NBShaderFlags.FLAG_BIT_FORCE_NO_MIP_COLORBLENDMAP,
                uvFoldOutPropertyName: "_ColorBlendUVModeFoldOut",
                uvModeFlag: NBShaderFlags.FLAG_BIT_UVMODE_POS_0_COLOR_BLEND_MAP,
                customDataOffsetXFlag: NBShaderFlags.FLAGBIT_POS_3_CUSTOMDATA_COLOR_BLEND_OFFSET_X,
                customDataOffsetYFlag: NBShaderFlags.FLAGBIT_POS_3_CUSTOMDATA_COLOR_BLEND_OFFSET_Y,
                rotation: NumericBinding.VectorSlider("_ColorBlendVec", 3, 0f, 360f),
                offsetPropertyName: "_ColorBlendMapOffset",
                distortion: NumericBinding.VectorSlider("_ColorBlendVec", 0, 0f, 1f),
                colorIntensityPropertyName: "_ColorBlendColorIntensity",
                alphaModePropertyName: "_ColorBlendAlphaMultiplyMode",
                alphaModeFlag: NBShaderFlags.FLAG_BIT_PARTICLE_COLOR_BLEND_ALPHA_MULTIPLY_MODE,
                alphaModeFlagIndex: 0,
                alphaIntensity: NumericBinding.VectorSlider("_ColorBlendVec", 2, 0f, 1f))
        {
        }
    }
}
