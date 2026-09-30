// Product-candidate Graph pass. URP owns the vertex/SurfaceDescription setup;
// the numeric distortion payload is the same NBShader shared HLSL as ShaderLab.
#define frag NBGraphUnusedForwardFragment
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/UnlitPass.hlsl"
#undef frag

#include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderDistortionV1.hlsl"
#include "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBGraphFlags.hlsl"
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
    // Evaluate the Graph once. VFX stores exposed properties in its generated
    // GraphProperties struct rather than the Mesh material CBUFFER.
    SurfaceDescriptionInputs graphInputs = BuildSurfaceDescriptionInputs(unpacked);
    float distortionMode, distortionIntensity, flags1Lo16, flags1Hi16;
    float alphaPow, alphaMultiplier, alphaAdd;
#if defined(HAVE_VFX_MODIFICATION)
    GraphProperties graphProperties = (GraphProperties)0;
    GetElementPixelProperties(graphInputs, graphProperties);
    SurfaceDescription surface = SurfaceDescriptionFunction(graphInputs, graphProperties);
    distortionMode = graphProperties._NB_DistortionMode;
    distortionIntensity = graphProperties._NB_DistortionIntensity;
    flags1Lo16 = graphProperties._NB_Flags1Lo16;
    flags1Hi16 = graphProperties._NB_Flags1Hi16;
    alphaPow = graphProperties._NB_DistortionAlphaPow;
    alphaMultiplier = graphProperties._NB_DistortionAlphaMultiplier;
    alphaAdd = graphProperties._NB_DistortionAlphaAdd;
#else
    SurfaceDescription surface = SurfaceDescriptionFunction(graphInputs);
    distortionMode = _NB_DistortionMode;
    distortionIntensity = _NB_DistortionIntensity;
    flags1Lo16 = _NB_Flags1Lo16;
    flags1Hi16 = _NB_Flags1Hi16;
    alphaPow = _NB_DistortionAlphaPow;
    alphaMultiplier = _NB_DistortionAlphaMultiplier;
    alphaAdd = _NB_DistortionAlphaAdd;
#endif

    // The two original NBPostprocess RendererLists select these exact tags.
    // Mode 0 intentionally writes neither pass; mode 1/2 isolates each path.
#if defined(NB_GRAPH_DEFERRED_DISTORT_PASS)
    clip(distortionMode > 0.5 && distortionMode < 1.5 ? 1.0h : -1.0h);
#elif defined(NB_GRAPH_CAMERA_OPAQUE_PASS)
    clip(distortionMode > 1.5 ? 1.0h : -1.0h);
#else
    #error NB Graph distortion pass kind is missing.
#endif

    NBFX_DistortionInputV1 input = (NBFX_DistortionInputV1)0;
    // Both values came from the same SurfaceDescriptionFunction evaluation.
    // signedRG is deliberately unmasked: the shared payload multiplies the
    // independent mask once into coverage, not again into RG.
    input.signedNoise = surface.NBDistortionSignedRG;
    input.noiseMask = surface.NBDistortionNoiseMask;
    input.alphaBeforePremultiply = surface.Alpha;
    input.refineAlpha = (NBGraphDecodeUInt32(flags1Lo16, flags1Hi16) & FLAG_BIT_PARTICLE_1_SCREEN_DISTORT_ALPHA_REFINE) != 0u;
    input.alphaPow = alphaPow;
    input.alphaMultiplier = alphaMultiplier;
    input.alphaAdd = alphaAdd;
    input.intensity = distortionIntensity;
    NBFX_DistortionPayloadV1 payload = NBFX_BuildDistortionPayloadV1(input);

#if defined(NB_GRAPH_DEFERRED_DISTORT_PASS)
    outColor = half4(payload.signedRG, 1.0h, payload.coverage * payload.intensity);
#else
    float2 uv = GetNormalizedScreenSpaceUV(unpacked.positionCS);
    outColor = half4(SampleSceneColor(uv + payload.signedRG * payload.coverage * payload.intensity), 1.0h);
#endif

// Match NBShader: Deferred clips its encoded coverage/intensity alpha;
// CameraOpaque clips its final alpha=1, not SurfaceDescription.Alpha.
#ifdef _ALPHATEST_ON
    clip(outColor.a - surface.AlphaClipThreshold);
#endif
    outColor = min(outColor, 1000);

#ifdef _WRITE_RENDERING_LAYERS
    outRenderingLayers = EncodeMeshRenderingLayer();
#endif
}
