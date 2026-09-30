#ifndef NB_SHADER_SURFACE_V1
#define NB_SHADER_SURFACE_V1

#include "NBShaderSharedContractV1.hlsl"
#include "Packages/com.xuanxuan.nb.fx/XuanXuanRenderUtility/Shader/HLSL/XuanXuan_Utility.hlsl"

// Shared ShaderLab/Graph soft-particle alpha. The caller supplies linear eye
// depths from its host; resource acquisition remains host-specific.
float NBFX_SoftParticlesV1(float near, float far, float sceneZ, float thisZ)
{
    float fade = 1;
    if (near > 0.0 || far > 0.0)
    {
        float dist = sceneZ - thisZ;
        fade = NB_Remap(dist, near, far, 0, 1);
    }
    return fade;
}

// Depth-outline composition is shared by the original ForwardPass and the
// Graph host. Both pass in the same scene/fragment eye depths.
void NBFX_ApplyDepthOutlineV1(inout half3 color, inout half alpha,
    half4 outlineColor, half2 nearFar, float sceneZ, float thisZ)
{
    half outline = 1.0h - NBFX_SoftParticlesV1(nearFar.x, nearFar.y,
        sceneZ, thisZ);
    outline *= outlineColor.a;
    half3 original = color;
    color = lerp(color, outlineColor.rgb, clamp(outline * 3.0h, 0.0h, 1.0h));
    color = lerp(color, original, clamp(alpha - outline, 0.0h, 1.0h));
    alpha = max(alpha, outline);
}

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

// Dissolve Ramp is RGB-only and is evaluated before the dissolve edge line.
// Unlike the general Color Ramp above, its blend amount does not alter alpha.
void NBFX_ApplyDissolveRampV1(inout half3 color, half4 rampSample,
    half4 tint, bool multiplyMode)
{
    half3 rampRGB = rampSample.rgb * tint.rgb;
    half amount = rampSample.a * tint.a;
    if (multiplyMode)
        color *= lerp(1.0h, rampRGB, amount);
    else
        color = lerp(color, rampRGB, amount);
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

// Original NBShader Fresnel math. Hosts own the enable gate, normal source,
// CustomData offset and back-face policy; the numerical operation is shared.
half NBFX_EvaluateFresnelV1(float3 viewDirWS, half3 normalWS,
    half3 directionOffset, half4 unit, bool invert)
{
    half3 fresnelDir = normalize(viewDirWS + directionOffset);
    half fresnelValue = dot(fresnelDir, normalWS);
    fresnelValue = NB_Remap(fresnelValue, unit.x,
        unit.x + 1.01h - unit.w, 0.0h, 1.0h);
    UNITY_BRANCH
    if (!invert)
        fresnelValue = 1.0h - fresnelValue;
    return pow(fresnelValue, unit.y);
}

void NBFX_ApplyFresnelV1(inout half3 color, inout half alpha,
    half fresnelValue, half strength, half4 fresnelColor,
    bool alphaMode, bool colorAffectedByAlpha)
{
    UNITY_BRANCH
    if (alphaMode)
    {
        fresnelValue *= alpha;
        alpha = lerp(alpha, fresnelValue, strength);
    }
    else
    {
        float fresnelColorIntensity = fresnelValue * fresnelColor.a * strength;
        color = lerp(color, fresnelColor.rgb, fresnelColorIntensity);
        UNITY_BRANCH
        if (!colorAffectedByAlpha)
            alpha = max(alpha, fresnelColorIntensity);
    }
}

#endif
