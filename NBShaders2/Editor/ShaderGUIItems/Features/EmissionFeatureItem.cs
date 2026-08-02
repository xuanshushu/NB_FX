using NBShader;

namespace NBShaderEditor
{
    internal sealed class EmissionFeatureItem : ColorOverlayFeatureItem
    {
        public EmissionFeatureItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem)
            : base(
                rootItem,
                parentItem,
                label: "叠加贴图 1",
                foldOutPropertyName: "_EmissionBlockFoldOut",
                togglePropertyName: "_EmissionEnabled",
                keyword: "_EMISSION",
                blendModePropertyName: "_EmissionBlendMode",
                blendModeFlag: NBShaderFlags.FLAG_BIT_PARTICLE_COLOR_OVERLAY_1_MULTIPLY,
                blendModeFlagIndex: 0,
                blendModeFlagEnabledMode: 1,
                texturePropertyName: "_EmissionMap",
                colorPropertyName: "_EmissionMapColor",
                wrapFlag: NBShaderFlags.FLAG_BIT_WRAPMODE_EMISSIONMAP,
                forceNoMipFlag: NBShaderFlags.FLAG_BIT_FORCE_NO_MIP_EMISSIONMAP,
                uvFoldOutPropertyName: "_EmissionUVModeFoldOut",
                uvModeFlag: NBShaderFlags.FLAG_BIT_UVMODE_POS_0_EMISSION_MAP,
                customDataOffsetXFlag: NBShaderFlags.FLAGBIT_POS_3_CUSTOMDATA_EMISSION_OFFSET_X,
                customDataOffsetYFlag: NBShaderFlags.FLAGBIT_POS_3_CUSTOMDATA_EMISSION_OFFSET_Y,
                rotation: NumericBinding.Slider("_EmissionMapUVRotation", 0f, 360f),
                offsetPropertyName: "_EmissionMapUVOffset",
                distortion: NumericBinding.Float("_Emi_Distortion_intensity"),
                colorIntensityPropertyName: "_EmissionMapColorIntensity",
                alphaModePropertyName: "_EmissionAlphaMultiplyMode",
                alphaModeFlag: NBShaderFlags.FLAG_BIT_PARTICLE_1_COLOR_OVERLAY_1_ALPHA_MULTIPLY,
                alphaModeFlagIndex: 1,
                alphaIntensity: NumericBinding.Slider("_EmissionAlphaIntensity", 0f, 1f))
        {
        }
    }
}
