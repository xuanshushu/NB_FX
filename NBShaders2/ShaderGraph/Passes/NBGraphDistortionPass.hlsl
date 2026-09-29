// Product-candidate Graph pass. URP owns the vertex/SurfaceDescription setup;
// the numeric distortion payload is the same NBShader shared HLSL as ShaderLab.
#define frag NBGraphUnusedForwardFragment
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/UnlitPass.hlsl"
#undef frag

#include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderDistortionV1.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

void frag(
    PackedVaryings packedInput,
    out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);
    SurfaceDescription surface = BuildSurfaceDescription(unpacked);

    // The two original NBPostprocess RendererLists select these exact tags.
    // Mode 0 intentionally writes neither pass; mode 1/2 isolates each path.
#if defined(NB_GRAPH_DEFERRED_DISTORT_PASS)
    clip(_NB_DistortionMode > 0.5 && _NB_DistortionMode < 1.5 ? 1.0h : -1.0h);
#elif defined(NB_GRAPH_CAMERA_OPAQUE_PASS)
    clip(_NB_DistortionMode > 1.5 ? 1.0h : -1.0h);
#else
    #error NB Graph distortion pass kind is missing.
#endif

    NBFX_DistortionInputV1 input = (NBFX_DistortionInputV1)0;
    input.signedNoise = _NB_DistortionNoise.xy;
    input.noiseMask = 1.0h;
    input.alphaBeforePremultiply = surface.Alpha;
    input.refineAlpha = false;
    input.intensity = _NB_DistortionIntensity;
    NBFX_DistortionPayloadV1 payload = NBFX_BuildDistortionPayloadV1(input);

#if defined(NB_GRAPH_DEFERRED_DISTORT_PASS)
    outColor = half4(payload.signedRG, 1.0h, payload.coverage * payload.intensity);
#else
    float2 uv = GetNormalizedScreenSpaceUV(unpacked.positionCS);
    outColor = half4(SampleSceneColor(uv + payload.signedRG * payload.coverage * payload.intensity), 1.0h);
#endif

#ifdef _WRITE_RENDERING_LAYERS
    outRenderingLayers = EncodeMeshRenderingLayer();
#endif
}
