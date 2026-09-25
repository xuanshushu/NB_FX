// GF-only controlled data source. This is not NBShader's production distortion.
// Reuse the installed URP version's vertex and graph plumbing, but substitute
// a measurable fragment output for the two NBPostprocess-selected passes.
#define frag NBGFUnusedForwardFragment
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/UnlitPass.hlsl"
#undef frag

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

float _NBGFMode; // 1 = deferred, 2 = camera-opaque; 0 keeps both probes disabled.

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
    SurfaceDescription surfaceDescription = BuildSurfaceDescription(unpacked);
    // The probe's foreground has blue=1; the gradient reference behind it
    // has blue<0.9 and must not write either distortion pass.
    clip(surfaceDescription.BaseColor.b > 0.9h ? 1.0h : -1.0h);

    // This fixed, non-cancelling vector yields an eight-pixel source offset
    // on a 128-pixel-wide test image after the 0.25 intensity is applied.
    const half2 noise = half2(0.25h, 0.0h);
    const half intensity = 0.25h;
    // Deliberately independent of Forward alpha: the controlled material can
    // hide its normal color while we isolate the two post-processing passes.
    half strength = intensity;

#if defined(NB_GF_CAMERA_OPAQUE_PASS)
    clip(_NBGFMode > 1.5 ? 1.0h : -1.0h);
    float2 uv = GetNormalizedScreenSpaceUV(unpacked.positionCS);
    outColor = half4(SampleSceneColor(uv + noise * strength), 1.0h);
#elif defined(NB_GF_DEFERRED_DISTORT_PASS)
    clip(_NBGFMode > 0.5 && _NBGFMode < 1.5 ? 1.0h : -1.0h);
    outColor = half4(noise, 1.0h, strength);
#else
    #error GF distortion pass kind is not specified.
#endif

#ifdef _WRITE_RENDERING_LAYERS
    outRenderingLayers = EncodeMeshRenderingLayer();
#endif
}
