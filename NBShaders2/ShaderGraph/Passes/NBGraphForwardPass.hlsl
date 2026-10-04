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
#if defined(NB_GRAPH_BACKFIRST_PASS)
    // The derived effective Float starts0 and is owned by the same narrow
    // resolver/service. A new default-enabled pass cannot draw before sync.
    clip(_NB_BackFirstEffective - 0.5f);
#endif
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
    bool NBGraphDebugOutputActive = false;
#if defined(NB_DEBUG_VERTEX_OFFSET)
    NBGraphDebugOutputActive = true;
#elif defined(NB_DEBUG_PNOISE)
    NBGraphDebugOutputActive = graphProperties._ProgramNoise_Toggle > 0.5 && graphProperties._NB_TierAllowProgramNoise > 0.5 && ((graphProperties._ProgramNoise_Simple_Toggle > 0.5 && graphProperties._NB_TierAllowProgramSimple > 0.5) || (graphProperties._ProgramNoise_Voronoi_Toggle > 0.5 && graphProperties._NB_TierAllowProgramVoronoi > 0.5));
#elif defined(NB_DEBUG_DISTORT)
    NBGraphDebugOutputActive = graphProperties._noisemapEnabled > 0.5 && graphProperties._NB_TierAllowNoise > 0.5;
#elif defined(NB_DEBUG_DISSOLVE)
    NBGraphDebugOutputActive = graphProperties._Dissolve_Toggle > 0.5;
#elif defined(NB_DEBUG_MASK)
    NBGraphDebugOutputActive = graphProperties._Mask_Toggle > 0.5 && graphProperties._NB_TierAllowMask > 0.5;
#elif defined(NB_DEBUG_FRESNEL)
    NBGraphDebugOutputActive = graphProperties._fresnelEnabled > 0.5;
#endif

#else
    SurfaceDescription surface = SurfaceDescriptionFunction(graphInputs);
    additiveToPremultiply = (half)_AdditiveToPreMultiplyAlphaLerp;
    bool NBGraphDebugOutputActive = false;
#if defined(NB_DEBUG_VERTEX_OFFSET)
    NBGraphDebugOutputActive = true;
#elif defined(NB_DEBUG_PNOISE)
    NBGraphDebugOutputActive = _ProgramNoise_Toggle > 0.5 && _NB_TierAllowProgramNoise > 0.5 && ((_ProgramNoise_Simple_Toggle > 0.5 && _NB_TierAllowProgramSimple > 0.5) || (_ProgramNoise_Voronoi_Toggle > 0.5 && _NB_TierAllowProgramVoronoi > 0.5));
#elif defined(NB_DEBUG_DISTORT)
    NBGraphDebugOutputActive = _noisemapEnabled > 0.5 && _NB_TierAllowNoise > 0.5;
#elif defined(NB_DEBUG_DISSOLVE)
    NBGraphDebugOutputActive = _Dissolve_Toggle > 0.5;
#elif defined(NB_DEBUG_MASK)
    NBGraphDebugOutputActive = _Mask_Toggle > 0.5 && _NB_TierAllowMask > 0.5;
#elif defined(NB_DEBUG_FRESNEL)
    NBGraphDebugOutputActive = _fresnelEnabled > 0.5;
#endif

#endif
    half3 result = (half3)surface.BaseColor;
    half alpha = (half)surface.Alpha;
    // Native Debug returns bypass blend, clip, final clamp and still write OVZ.
    if (NBGraphDebugOutputActive)
    {
        outColor = half4(result, alpha);
#if defined(_OVERRIDE_Z)
        outDepth = surface.NBOverrideDeviceDepth;
#endif
#ifdef _WRITE_RENDERING_LAYERS
        outRenderingLayers = EncodeMeshRenderingLayer();
#endif
        return;
    }
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
