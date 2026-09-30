// Keep URP's generated vertex path and its shadow bias. Replace only the
// fragment's transparent coverage rule, which stock SG does not provide.
#define frag NBGraphUnusedShadowFragment
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShadowCasterPass.hlsl"
#undef frag
#include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderSurfaceV1.hlsl"

half4 frag(PackedVaryings packedInput) : SV_TARGET
{
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);
    SurfaceDescription surfaceDescription = BuildSurfaceDescription(unpacked);

#if defined(_ALPHATEST_ON)
    clip(surfaceDescription.Alpha - surfaceDescription.AlphaClipThreshold);
#endif

#if defined(HAVE_VFX_MODIFICATION)
    SurfaceDescriptionInputs graphInputs = BuildSurfaceDescriptionInputs(unpacked);
    GraphProperties graphProperties = (GraphProperties)0;
    GetElementPixelProperties(graphInputs, graphProperties);
    uint flags1 = (uint)round(graphProperties._NB_Flags1Lo16);
#else
    uint flags1 = (uint)round(_NB_Flags1Lo16);
#endif
    if ((flags1 & 1u) != 0u)
    {
        if ((flags1 & 2u) != 0u)
            clip(NBFX_ShadowDitherMaskClipV1(unpacked.positionCS, (half)surfaceDescription.Alpha));
        else
            clip((half)surfaceDescription.Alpha - 0.5h);
    }

#if defined(LOD_FADE_CROSSFADE) && USE_UNITY_CROSSFADE
    LODFadeCrossFade(unpacked.positionCS);
#endif
    return 0;
}
