#ifndef NB_GRAPH_BASE_COLOR_INCLUDED
#define NB_GRAPH_BASE_COLOR_INCLUDED

#include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderSurfaceV1.hlsl"
#include "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBGraphFlags.hlsl"

// SHADERGRAPH_PREVIEW and runtime execute the same numeric shared function;
// no preview-only camera/scene substitute is needed.
// Stage: fragment BaseColor/Alpha. Preserve the GF BaseMap and Color controls.
// Pass-only inputs must be connected to this live fragment node: VFX Graph
// otherwise gives them stage None and omits them from GraphProperties. The NB
// distortion passes consume those inputs from GraphProperties, not from this
// color function. Flags0 is also used here for the original bit29 behavior.
void NBGraphBaseColor_float(float4 SampledAlbedo, float SelectedAlpha,
    float4 EffectiveBaseColor,
    float NB_Flags0Lo16, float NB_Flags0Hi16,
    float NB_Flags1Lo16, float NB_Flags1Hi16,
    float2 NB_DistortionNoise, float NB_DistortionIntensity, float NB_DistortionMode,
    float NB_DistortionAlphaPow, float NB_DistortionAlphaMultiplier, float NB_DistortionAlphaAdd,
    out float4 Out)
{
    NBFX_BaseColorInputV1 input = (NBFX_BaseColorInputV1)0;
    input.sampledAlbedo = (half4)SampledAlbedo;
    input.selectedAlpha = (half)SelectedAlpha;
    input.effectiveBaseColor = (half4)EffectiveBaseColor;
    input.timelineIntensity = 1.0h;
    input.applyTimelineIntensity = false;
    Out = (float4)NBFX_ComposeBaseColorV1(input);
    // Original ColorAdjustment applies this flag after the base sample. In this
    // minimum Unlit Graph, no intervening lighting/effects alter that ordering.
    if ((NBGraphDecodeUInt32(NB_Flags0Lo16, NB_Flags0Hi16) & FLAG_BIT_PARTICLE_COLOR_MULTI_ALPHA) != 0u)
        Out.rgb *= Out.a;
}

void NBGraphBaseColor_half(half4 SampledAlbedo, half SelectedAlpha,
    half4 EffectiveBaseColor,
    float NB_Flags0Lo16, float NB_Flags0Hi16,
    float NB_Flags1Lo16, float NB_Flags1Hi16,
    float2 NB_DistortionNoise, float NB_DistortionIntensity, float NB_DistortionMode,
    float NB_DistortionAlphaPow, float NB_DistortionAlphaMultiplier, float NB_DistortionAlphaAdd,
    out half4 Out)
{
    NBFX_BaseColorInputV1 input = (NBFX_BaseColorInputV1)0;
    input.sampledAlbedo = SampledAlbedo;
    input.selectedAlpha = SelectedAlpha;
    input.effectiveBaseColor = EffectiveBaseColor;
    input.timelineIntensity = 1.0h;
    input.applyTimelineIntensity = false;
    Out = NBFX_ComposeBaseColorV1(input);
    if ((NBGraphDecodeUInt32(NB_Flags0Lo16, NB_Flags0Hi16) & FLAG_BIT_PARTICLE_COLOR_MULTI_ALPHA) != 0u)
        Out.rgb *= Out.a;
}

#endif
