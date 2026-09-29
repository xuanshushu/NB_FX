#ifndef NB_SHADER_SURFACE_V1
#define NB_SHADER_SURFACE_V1

#include "NBShaderSharedContractV1.hlsl"
#include "Packages/com.xuanxuan.nb.fx/XuanXuanRenderUtility/Shader/HLSL/XuanXuan_Utility.hlsl"

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

// Original Ramp composition, shared by ShaderLab and Graph. Sampling and
// packed-gradient key selection stay with each host's resource binding.
void NBFX_ApplyColorRampV1(inout half3 baseColor, inout half baseAlpha,
    half4 rampSample, half4 rampTint, bool addMode)
{
    half4 rampColor = rampSample * rampTint;
    if (addMode)
    {
        baseColor += rampColor.rgb;
        baseAlpha += rampColor.a;
    }
    else
    {
        baseColor *= rampColor.rgb;
        baseAlpha *= rampColor.a;
    }
}

// Pure original color-adjustment arithmetic. ShaderLab resolves its optional
// CustomData before calling this; Graph/VFX supplies the current uniform or
// connected value, so neither host silently changes the other's protocol.
void NBFX_ApplyColorAdjustmentV1(inout half3 color, half alpha,
    bool hueOn, half hueShift,
    bool contrastOn, half contrast, half3 contrastMidColor,
    bool saturationOn, half saturability,
    bool refineOn, half4 baseMapColorRefine,
    bool premultiplyRGB)
{
    UNITY_BRANCH
    if (hueOn)
    {
        half3 hsv = RgbToHsv(color);
        hsv.r += hueShift;
        color = HsvToRgb(hsv);
    }

    UNITY_BRANCH
    if (contrastOn)
        color = lerp(contrastMidColor, color, contrast);

    UNITY_BRANCH
    if (saturationOn)
    {
        half3 whiteBlack = luminance(color);
        color = lerp(whiteBlack, color, saturability);
    }

    if (refineOn)
    {
        half3 colorA = color * baseMapColorRefine.x;
        half3 colorB = pow(color, baseMapColorRefine.y) * baseMapColorRefine.z;
        color = lerp(colorA, colorB, baseMapColorRefine.w);
    }

    if (premultiplyRGB)
        color *= alpha;
}

#endif
