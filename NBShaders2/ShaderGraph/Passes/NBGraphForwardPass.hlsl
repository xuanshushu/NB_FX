// NB's final output order differs from stock URP Unlit: premultiply RGB by
// the pre-blend alpha, optionally scale only the final alpha, then clip it.
// Keep URP's generated vertex/Graph setup; do not copy its Target or package.
#define frag NBGraphUnusedUnlitForwardFragment
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/UnlitPass.hlsl"
#undef frag
#include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderSurfaceV1.hlsl"

void frag(PackedVaryings packedInput,
    out half4 outColor : SV_Target0
#if defined(_OVERRIDE_Z)
    , out float outDepth : SV_Depth
#endif
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);
    SurfaceDescriptionInputs graphInputs = BuildSurfaceDescriptionInputs(unpacked);
    half additiveToPremultiply;
#if defined(HAVE_VFX_MODIFICATION)
    GraphProperties graphProperties = (GraphProperties)0;
    GetElementPixelProperties(graphInputs, graphProperties);
    SurfaceDescription surface = SurfaceDescriptionFunction(graphInputs, graphProperties);
    additiveToPremultiply = (half)graphProperties._AdditiveToPreMultiplyAlphaLerp;
#else
    SurfaceDescription surface = SurfaceDescriptionFunction(graphInputs);
    additiveToPremultiply = (half)_AdditiveToPreMultiplyAlphaLerp;
#endif
    half3 result = (half3)surface.BaseColor;
    half alpha = (half)surface.Alpha;
#if defined(_ALPHAPREMULTIPLY_ON) || defined(_ALPHAMODULATE_ON)
    #ifdef _ALPHAPREMULTIPLY_ON
        NBFX_ApplyBlendOutputV1(result, alpha, true, additiveToPremultiply);
    #else
        NBFX_ApplyBlendOutputV1(result, alpha, false, 1.0h);
    #endif
#endif
    outColor = half4(result, alpha);
#ifdef _ALPHATEST_ON
    clip(outColor.a - surface.AlphaClipThreshold);
#endif
    outColor = min(outColor, 1000);
#if defined(_OVERRIDE_Z)
    outDepth=surface.NBOverrideDeviceDepth;
#endif
#ifdef _WRITE_RENDERING_LAYERS
    outRenderingLayers = EncodeMeshRenderingLayer();
#endif
}
