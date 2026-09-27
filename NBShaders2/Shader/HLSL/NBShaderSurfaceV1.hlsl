#ifndef NB_SHADER_SURFACE_V1
#define NB_SHADER_SURFACE_V1

#include "NBShaderSharedContractV1.hlsl"

// Matches the base-color operations after the caller selects the sampled alpha
// and facing-dependent color. Texture selection and later color adjustment stay
// with the host shader.
half4 NBFX_ComposeBaseColorV1(NBFX_BaseColorInputV1 input)
{
    half4 albedo = input.sampledAlbedo;
    albedo.a = input.selectedAlpha;
    albedo *= input.effectiveBaseColor;
    if (input.applyTimelineIntensity)
    {
        albedo.rgb *= input.timelineIntensity;
    }
    return albedo;
}

#endif
