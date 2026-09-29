#ifndef NB_GRAPH_BASE_COLOR_INCLUDED
#define NB_GRAPH_BASE_COLOR_INCLUDED

#include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderSurfaceV1.hlsl"

// This is pure numeric HLSL, so SHADERGRAPH_PREVIEW and runtime intentionally
// execute the same shared function. No preview-only camera/scene substitute is needed.
// Stage: fragment BaseColor/Alpha. Preserve the GF BaseMap and Color controls.
void NBGraphBaseColor_float(float4 SampledAlbedo, float SelectedAlpha,
    float4 EffectiveBaseColor, out float4 Out)
{
    NBFX_BaseColorInputV1 input = (NBFX_BaseColorInputV1)0;
    input.sampledAlbedo = (half4)SampledAlbedo;
    input.selectedAlpha = (half)SelectedAlpha;
    input.effectiveBaseColor = (half4)EffectiveBaseColor;
    input.timelineIntensity = 1.0h;
    input.applyTimelineIntensity = false;
    Out = (float4)NBFX_ComposeBaseColorV1(input);
}

void NBGraphBaseColor_half(half4 SampledAlbedo, half SelectedAlpha,
    half4 EffectiveBaseColor, out half4 Out)
{
    NBFX_BaseColorInputV1 input = (NBFX_BaseColorInputV1)0;
    input.sampledAlbedo = SampledAlbedo;
    input.selectedAlpha = SelectedAlpha;
    input.effectiveBaseColor = EffectiveBaseColor;
    input.timelineIntensity = 1.0h;
    input.applyTimelineIntensity = false;
    Out = NBFX_ComposeBaseColorV1(input);
}

#endif
