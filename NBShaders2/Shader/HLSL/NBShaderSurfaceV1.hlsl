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


// The original overlay 1/2 arithmetic, now shared by ShaderLab and Graph.
void NBFX_ApplyColorOverlayV1(
    inout half3 baseColor,
    inout half baseAlpha,
    half4 overlaySample,
    half4 overlayTint,
    half overlayColorIntensity,
    half overlayAlphaIntensity,
    bool multiplyMode,
    bool alphaMultiplyMode)
{
    half3 overlayColor = overlaySample.rgb;
    half overlayAlpha = overlaySample.a * overlayTint.a;
    half3 overlayTintWithIntensity = overlayTint.rgb * overlayColorIntensity;

    UNITY_BRANCH
    if (overlayAlphaIntensity != 1.0h)
    {
        overlayAlpha = lerp(1.0h, overlayAlpha, overlayAlphaIntensity);
    }

    UNITY_BRANCH
    if (multiplyMode)
    {
        overlayColor *= overlayTintWithIntensity;

        UNITY_BRANCH
        if (alphaMultiplyMode)
        {
            baseColor *= overlayColor;
        }
        else
        {
            baseColor = lerp(baseColor, baseColor * overlayColor, overlayAlpha);
        }
    }
    else
    {
        UNITY_BRANCH
        if (!alphaMultiplyMode)
        {
            overlayColor *= overlayAlpha;
        }

        overlayColor *= overlayTintWithIntensity;
        baseColor += overlayColor;
    }

    UNITY_BRANCH
    if (alphaMultiplyMode)
    {
        baseAlpha *= overlayAlpha;
    }
}

#endif
