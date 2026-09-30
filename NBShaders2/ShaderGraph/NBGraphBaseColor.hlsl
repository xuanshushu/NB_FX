#ifndef NB_GRAPH_BASE_COLOR_INCLUDED
#define NB_GRAPH_BASE_COLOR_INCLUDED

#include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderSurfaceV1.hlsl"
#include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderEnvironmentV2.hlsl"
#include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderDistortionV1.hlsl"
#include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderMaskV3.hlsl"
#include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderDissolveV3.hlsl"
#include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderPackedGradientV1.hlsl"
#include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderUVV2.hlsl"
#include "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBGraphFlags.hlsl"
#include "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBGraphSampling.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

// Match the ShaderLab host's depth conversion without making every Graph
// material request a camera depth prepass. When enabled, the URP camera must
// provide _CameraDepthTexture, just as the original ShaderLab feature does.
float NBGraphSceneEyeDepth(float2 screenUV)
{
#if SHADERGRAPH_PREVIEW
    return _ProjectionParams.z;
#else
    float rawDepth = SampleSceneDepth(screenUV);
    #if !UNITY_REVERSED_Z
        rawDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1, rawDepth);
    #endif
    return unity_OrthoParams.w == 0 ?
        LinearEyeDepth(rawDepth, _ZBufferParams) :
        LinearDepthToEyeDepth(rawDepth);
#endif
}

// UnityTexture2D already carries the Graph texture's _ST in scaleTranslate.
// Transform it exactly once, before sampling, rather than asking the sampler
// to apply GetTransformedUV to an already transformed coordinate.
float2 NBGraphFeatureUV(UnityTexture2D map, float2 uv, float rotationDegrees,
    float2 offsetSpeed)
{
    // Keep the previous Graph sampling expression for the zero-control case;
    // in particular non-identity texture ST retains its original arithmetic.
    if (rotationDegrees == 0.0 && offsetSpeed.x == 0.0 && offsetSpeed.y == 0.0)
        return map.GetTransformedUV(uv);

    NBFX_FeatureUVTransformInputV2 input = (NBFX_FeatureUVTransformInputV2)0;
    input.originUV = uv;
    input.scaleOffset = map.scaleTranslate;
    input.offsetSpeed = offsetSpeed;
    input.rotationDegrees = rotationDegrees;
    input.rotationCenter = float2(0.5, 0.5);
    input.timeY = _Time.y;
    return NBFX_TransformFeatureUVV2(input);
}

// Shared N1 surface/N2 screen texture noise. Preserve ParticleUVCommonProcess's rotation
// -> ST and SampleNoise's later scrolling, with ShaderLab's half material
// boundaries. NoiseMask has its own source/ST and no rotation or scrolling.
// The caller weights only texture-consumer offsets by noiseMask; the screen
// passes receive signedRG and noiseMask separately. CustomData, PNoise,
// Refraction and chromatic aberration remain pending.
void NBGraphTextureNoise(UnityTexture2D noiseMap, float2 noiseSourceUV,
    float noiseRotation, half4 noiseOffset, half intensity, half4 direction,
    UnityTexture2D maskMap, float2 maskSourceUV, bool hasMask,
    uint flags0, uint channels, uint wrapFlags, uint noMipFlags,
    out half2 signedRG, out half noiseMask)
{
    NBFX_FeatureUVTransformInputV2 uvInput = (NBFX_FeatureUVTransformInputV2)0;
    uvInput.originUV = noiseSourceUV;
    uvInput.scaleOffset = (half4)noiseMap.scaleTranslate;
    uvInput.rotationDegrees = noiseRotation;
    uvInput.rotationCenter = float2(0.5, 0.5);
    uvInput.timeY = _Time.y;
    float2 noiseUV = NBFX_TransformFeatureUVV2(uvInput);
    noiseUV = float2(noiseOffset.x * _Time.y + noiseUV.x,
        noiseOffset.y * _Time.y + noiseUV.y);
    half4 noiseSample = NBGraphSampleMap(noiseMap, noiseUV,
        NBGraphMaskWrapMode(wrapFlags, FLAG_BIT_WRAPMODE_NOISEMAP),
        (noMipFlags & FLAG_BIT_FORCE_NO_MIP_NOISEMAP) != 0u);
    signedRG = 0;
    noiseMask = 1;
    NBFX_DecodeTextureNoiseV1(noiseSample,
        (flags0 & FLAG_BIT_PARTICLE_NOISEMAP_NORMALIZEED_ON) != 0u,
        signedRG, noiseMask);
    if (hasMask)
    {
        uvInput.originUV = maskSourceUV;
        uvInput.scaleOffset = (half4)maskMap.scaleTranslate;
        uvInput.rotationDegrees = 0;
        float2 maskUV = NBFX_TransformFeatureUVV2(uvInput);
        half4 maskSample = NBGraphSampleMap(maskMap, maskUV,
            NBGraphMaskWrapMode(wrapFlags, FLAG_BIT_WRAPMODE_NOISE_MASKMAP),
            (noMipFlags & FLAG_BIT_FORCE_NO_MIP_NOISE_MASKMAP) != 0u);
        uint channel = (channels >> FLAG_BIT_COLOR_CHANNEL_POS_0_NOISE_MASK) & 3u;
        half maskWeight = channel == 0u ? maskSample.r :
            channel == 1u ? maskSample.g : channel == 2u ? maskSample.b : maskSample.a;
        noiseMask *= maskWeight;
    }
    // ShaderLab saves screenDistort_Noise.xy before multiplying the texture
    // consumer offset by noiseMask. The same value feeds both NB passes.
    signedRG = signedRG * direction.xy * intensity;
}

// Texture-only layer 2/3 adapter. Both layers multiply before the one shared
// NBFX_ResolveMaskCoverageV3 call, matching the ShaderLab sampling branch.
half NBGraphSampleMaskLayer(UnityTexture2D map, float2 originUV,
    float rotationDegrees, float2 offsetSpeed, uint packedChannels,
    uint channelPosition, uint wrapFlags, uint wrapBit,
    uint noMipFlags, uint noMipBit)
{
    float2 uv = NBGraphFeatureUV(map, originUV, rotationDegrees, offsetSpeed);
    half4 sampled = NBGraphSampleMap(map, uv,
        NBGraphMaskWrapMode(wrapFlags, wrapBit), (noMipFlags & noMipBit) != 0u);
    uint channel = (packedChannels >> channelPosition) & 3u;
    return channel == 0u ? sampled.r :
        channel == 1u ? sampled.g :
        channel == 2u ? sampled.b : sampled.a;
}

half NBGraphSampleMaskGradient(half4 pack0, half4 pack1, half4 pack2,
    float keyCount, float2 transformedUV, uint wrapMode, bool useY)
{
    float coordinate = useY ? transformedUV.y : transformedUV.x;
    // Mask 1/3 use U (repeat for modes 0/2); Mask 2 uses V (0/3).
    bool repeatAxis = useY ? (wrapMode == 0u || wrapMode == 3u) :
        (wrapMode == 0u || wrapMode == 2u);
    half gradientTime = (half)(repeatAxis ? frac(coordinate) : saturate(coordinate));
    return SamplePackedGradientAlpha(pack0, pack1, pack2, (int)keyCount,
        gradientTime);
}

// ShaderLab GetColorChannel selects the BaseMap alpha source from the first
// two bits of the shared packed channel word (default 3 = A). Keep this in
// the Graph host because it owns the sampled RGBA value.
half NBGraphSelectBaseAlpha(half4 albedo, float packedChannelsLo16)
{
    uint channel = NBGraphDecodeUInt32(packedChannelsLo16, 0.0) & 3u;
    return channel == 0u ? albedo.r :
        channel == 1u ? albedo.g :
        channel == 2u ? albedo.b : albedo.a;
}

// The optional process/late mask uses the same two-stage numeric contract as
// ShaderLab. The host owns texture/channel selection; custom data, noise,
// and non-UV0 modes remain separate slices.
NBFX_DissolveResolvedV3 NBGraphResolveDissolve(half4 sampledDissolve,
    half4 sampledDissolveMask, bool hasDissolveMask, half4 dissolve,
    half dissolveMaskMode, float colorChannelLo16)
{
    uint channels = NBGraphDecodeUInt32(colorChannelLo16, 0.0);
    uint channel = (channels >>
        FLAG_BIT_COLOR_CHANNEL_POS_0_DISSOLVE_MAP) & 3u;
    half value = channel == 0u ? sampledDissolve.r :
        channel == 1u ? sampledDissolve.g :
        channel == 2u ? sampledDissolve.b : sampledDissolve.a;
    half maskValue = 0.0h;
    if (hasDissolveMask)
    {
        uint maskChannel = (channels >>
            FLAG_BIT_COLOR_CHANNEL_POS_0_DISSOLVE_MASK_MAP) & 3u;
        maskValue = maskChannel == 0u ? sampledDissolveMask.r :
            maskChannel == 1u ? sampledDissolveMask.g :
            maskChannel == 2u ? sampledDissolveMask.b : sampledDissolveMask.a;
    }
    NBFX_DissolvePrepareInputV3 prepareInput = (NBFX_DissolvePrepareInputV3)0;
    prepareInput.decodedAndNoiseBlendedValue = value;
    prepareInput.exponent = dissolve.y;
    prepareInput.hasMask = hasDissolveMask;
    prepareInput.decodedMaskValue = maskValue;
    prepareInput.maskStrength = dissolve.z;
    prepareInput.maskMode = dissolveMaskMode;
    NBFX_DissolvePreparedV3 prepared = NBFX_PrepareDissolveV3(prepareInput);
    NBFX_DissolveResolveInputV3 resolveInput = (NBFX_DissolveResolveInputV3)0;
    resolveInput.prepared = prepared;
    resolveInput.threshold = dissolve.x;
    resolveInput.softWidth = dissolve.w;
    resolveInput.hasMask = hasDissolveMask;
    resolveInput.maskStrength = dissolve.z;
    resolveInput.maskMode = dissolveMaskMode;
    NBFX_DissolveResolvedV3 resolved = NBFX_ResolveDissolveV3(resolveInput);
    return resolved;
}

// ShaderLab's line tint uses the value *before* the soft-step and late-mask
// coverage, not the resulting alpha. Preserve that stage distinction.
half4 NBGraphApplyDissolveLine(half4 color, half valueBeforeSoftStep,
    half4 lineRange, half4 lineColor, uint flags1)
{
    if ((flags1 & FLAG_BIT_PARTICLE_1_DISSOLVE_LINE_MASK) != 0u)
    {
        half lineMask = 1.0h - saturate(NB_Remap01(valueBeforeSoftStep,
            lineRange.x - lineRange.y, lineRange.x + lineRange.y));
        color.rgb = lerp(color.rgb, lineColor.rgb, lineMask * lineColor.a);
    }
    return color;
}

// The original Dissolve Ramp consumes pre-soft-step value, including the
// source texture's U scale/offset, and changes RGB before the edge line.
half4 NBGraphApplyDissolveRamp(half4 color, half valueBeforeSoftStep,
    UnityTexture2D map, float sourceMode, half4 tint, float packedCount,
    half4 color0, half4 color1, half4 color2, half4 color3,
    half4 color4, half4 color5, half4 alpha0, half4 alpha1, half4 alpha2,
    float wrapLo16, float wrapHi16, float forceNoMipLo16,
    float forceNoMipHi16, float flags1Lo16, float flags1Hi16,
    float stOverrideEnabled, float4 stOverride)
{
    uint wrapFlags = NBGraphDecodeUInt32(wrapLo16, wrapHi16);
    uint wrapMode = NBGraphMaskWrapMode(wrapFlags,
        FLAG_BIT_WRAPMODE_DISSOLVE_RAMPMAP);
    // Keep ShaderLab's two half-precision assignments. In LOD0 on a rapidly
    // varying ramp, fusing the ST expression shifts which texel is sampled.
    // VFX Output material settings do not expose texture ST as a connected
    // value. Keep Mesh's texture ST by default; the explicit Graph port lets
    // VFX supply the same U scale/offset without changing its Target.
    float4 rampST = stOverrideEnabled > 0.5 ? stOverride : map.scaleTranslate;
    half rampRange = valueBeforeSoftStep;
    rampRange = rampRange * rampST.x + rampST.z;
    half4 rampSample;
    if (sourceMode > 0.5)
    {
        uint noMipFlags = NBGraphDecodeUInt32(forceNoMipLo16,
            forceNoMipHi16);
        rampSample = NBGraphSampleMap(map,
            float2(rampRange, 0.5), wrapMode,
            (noMipFlags & FLAG_BIT_FORCE_NO_MIP_DISSOLVE_RAMPMAP) != 0u);
    }
    else
    {
        half key = (wrapMode == 0u || wrapMode == 2u) ?
            frac(rampRange) : saturate(rampRange);
        uint countWord = (uint)packedCount;
        rampSample.rgb = SamplePackedGradientColor(color0, color1, color2,
            color3, color4, color5, (int)(countWord & 0xffffu), key);
        rampSample.a = SamplePackedGradientAlpha(alpha0, alpha1, alpha2,
            (int)(countWord >> 16), key);
    }
    uint flags1 = NBGraphDecodeUInt32(flags1Lo16, flags1Hi16);
    NBFX_ApplyDissolveRampV1(color.rgb, rampSample, tint,
        (flags1 & FLAG_BIT_PARTICLE_1_DISSOLVE_RAMP_MULITPLY) != 0u);
    return color;
}

// ShaderLab Ramp sampling/source selection, followed by the same packed-key
// evaluator and composition used by the original ForwardPass. This slice
// covers UV0; special UV/custom data and noise are separate host inputs.
half4 NBGraphApplyColorRamp(half4 color, UnityTexture2D map, float2 originUV,
    float4 offsetAndRotation, float sourceMode, float packedCount,
    half4 color0, half4 color1, half4 color2, half4 color3,
    half4 color4, half4 color5, half4 alpha0, half4 alpha1, half4 alpha2,
    half4 tint, float packedChannelsLo16, float wrapLo16, float wrapHi16,
    float flags0Lo16, float flags0Hi16,
    float forceNoMipLo16, float forceNoMipHi16)
{
    float2 rampUV = NBGraphFeatureUV(map, originUV,
        offsetAndRotation.w, offsetAndRotation.xy);
    half rampValue;
    if (sourceMode > 0.5)
    {
        uint wrapFlags = NBGraphDecodeUInt32(wrapLo16, wrapHi16);
        uint noMipFlags = NBGraphDecodeUInt32(forceNoMipLo16, forceNoMipHi16);
        half4 sampled = NBGraphSampleMap(map, rampUV,
            NBGraphMaskWrapMode(wrapFlags, FLAG_BIT_WRAPMODE_RAMP_COLOR_MAP),
            (noMipFlags & FLAG_BIT_FORCE_NO_MIP_RAMP_COLOR_MAP) != 0u);
        uint channels = NBGraphDecodeUInt32(packedChannelsLo16, 0.0);
        uint channel = (channels >> FLAG_BIT_COLOR_CHANNEL_POS_0_RAMP_COLOR_MAP) & 3u;
        rampValue = channel == 0u ? sampled.r :
            channel == 1u ? sampled.g :
            channel == 2u ? sampled.b : sampled.a;
    }
    else
    {
        uint wrapFlags = NBGraphDecodeUInt32(wrapLo16, wrapHi16);
        uint wrapMode = NBGraphMaskWrapMode(wrapFlags,
            FLAG_BIT_WRAPMODE_RAMP_COLOR_MAP);
        rampValue = (half)((wrapMode == 0u || wrapMode == 2u) ?
            frac(rampUV.x) : saturate(rampUV.x));
    }

    uint countWord = (uint)packedCount;
    int colorCount = (int)(countWord & 0xffffu);
    int alphaCount = (int)(countWord >> 16);
    half4 rampColor;
    rampColor.rgb = SamplePackedGradientColor(color0, color1, color2,
        color3, color4, color5, colorCount, rampValue);
    rampColor.a = SamplePackedGradientAlpha(alpha0, alpha1, alpha2,
        alphaCount, rampValue);
    uint flags0 = NBGraphDecodeUInt32(flags0Lo16, flags0Hi16);
    NBFX_ApplyColorRampV1(color.rgb, color.a, rampColor, tint,
        (flags0 & FLAG_BIT_PARTICLE_RAMP_COLOR_BLEND_ADD) != 0u);
    return color;
}

half4 NBGraphApplyColorAdjustment(half4 color,
    half hueShift, half contrast, half3 contrastMidColor,
    half saturability, half4 baseMapColorRefine,
    float flags0Lo16, float flags0Hi16,
    float flags1Lo16, float flags1Hi16)
{
    uint flags0 = NBGraphDecodeUInt32(flags0Lo16, flags0Hi16);
    uint flags1 = NBGraphDecodeUInt32(flags1Lo16, flags1Hi16);
    NBFX_ApplyColorAdjustmentV1(color.rgb, color.a,
        (flags0 & FLAG_BIT_HUESHIFT_ON) != 0u, hueShift,
        (flags1 & FLAG_BIT_PARTICLE_1_MAINTEX_CONTRAST) != 0u,
        contrast, contrastMidColor,
        (flags0 & FLAG_BIT_SATURABILITY_ON) != 0u, saturability,
        (flags1 & FLAG_BIT_PARTICLE_1_MAINTEX_COLOR_REFINE) != 0u,
        baseMapColorRefine,
        (flags0 & FLAG_BIT_PARTICLE_COLOR_MULTI_ALPHA) != 0u);
    return color;
}

// The original Fresnel stage is after Mask and before VertexColor. World
// normal/view inputs come from SG geometry nodes; this first host path uses
// the unperturbed normal and mirrors ShaderLab's VFACE back-face reversal.
half4 NBGraphApplyFresnel(half4 color, float3 viewDirWS, half3 normalWS,
    half isFrontFace, half4 unit, half4 fresnelColor,
    half3 rotationOffset, uint flags0)
{
    if (isFrontFace < 0.5h)
        normalWS = -normalWS;
    half fresnelValue = NBFX_EvaluateFresnelV1(viewDirWS, normalWS,
        rotationOffset, unit,
        (flags0 & FLAG_BIT_PARTICLE_FRESNEL_INVERT_ON) != 0u);
    NBFX_ApplyFresnelV1(color.rgb, color.a, fresnelValue,
        unit.z, fresnelColor,
        (flags0 & FLAG_BIT_PARTICLE_FRESNEL_FADE_ON) != 0u,
        (flags0 & FLAG_BIT_PARTICLE_FRESNEL_COLOR_AFFETCT_BY_ALPHA) != 0u);
    return color;
}

// SHADERGRAPH_PREVIEW and runtime share the color arithmetic; optional scene
// depth uses the explicit preview substitute in NBGraphSceneEyeDepth.
// Stage: fragment BaseColor/Alpha. BaseMap is sampled here once, after BaseUV,
// using the same packed sampler protocol as every enabled feature texture.
// Pass-only inputs must be connected to this live fragment node: VFX Graph
// otherwise gives them stage None and omits them from GraphProperties. The NB
// distortion passes consume those inputs from GraphProperties, not from this
// color function. Flags0 is also used here for the original bit29 behavior.
// M0: original ShaderLab MatCap host, before Emission and after early
// color adjustment. Geometry normal only; NormalMap/lighting are later slices.
// Hosts retain float world normal until the original matrix-to-half boundary.
// This fixed sampler deliberately ignores texture ST and packed wrap flags.
half4 NBGraphApplyMatCap(half4 color, UnityTexture2D map, float3 normalWS,
    half3 positionVS, half isFrontFace, half4 matCapColor,
    half4 matCapInfo, uint noMipFlags)
{
    if (isFrontFace < 0.5h)
        normalWS = -normalWS;
    half3 normalVS = mul(normalWS, (float3x3)UNITY_MATRIX_I_V);
    float2 matCapUV = NBFX_MatCapUVV2(positionVS, normalVS);
    half3 matCapSample;
    UNITY_BRANCH
    if ((noMipFlags & FLAG_BIT_FORCE_NO_MIP_MATCAP) != 0u)
        matCapSample = SAMPLE_TEXTURE2D_LOD(map.tex, sampler_linear_clamp, matCapUV, 0).rgb;
    else
        matCapSample = SAMPLE_TEXTURE2D(map.tex, sampler_linear_clamp, matCapUV).rgb;
    color.rgb = NBFX_CompositeMatCapV2(color.rgb, matCapSample,
        matCapColor, matCapInfo.x);
    return color;
}

void NBGraphBaseColor_float(float4 SampledAlbedo, float SelectedAlpha,
    float4 EffectiveBaseColor,
    float NB_Flags0Lo16, float NB_Flags0Hi16,
    float NB_Flags1Lo16, float NB_Flags1Hi16,
    float2 NB_DistortionNoise, float NB_DistortionIntensity, float NB_DistortionMode,
    float NB_DistortionAlphaPow, float NB_DistortionAlphaMultiplier, float NB_DistortionAlphaAdd,
    UnityTexture2D MaskMap, float MaskToggle, float4 MaskMapVec, float4 MaskRefineVec,
    float NB_ColorChannelLo16,
    UnityTexture2D DissolveMap, float DissolveToggle, float4 Dissolve,
    float2 MaskUV, float2 DissolveUV,
    float MaskMapUVRotation, float MaskMapRotationSpeed, float4 MaskMapOffsetAnition,
    float4 DissolveOffsetRotateDistort,
    UnityTexture2D DissolveMaskMap, float DissolveMaskToggle, float DissolveMaskMode,
    float2 DissolveMaskUV,
    UnityTexture2D MaskMap2, float Mask2Toggle,
    UnityTexture2D MaskMap3, float Mask3Toggle, float4 MaskMap3OffsetAnition,
    float2 Mask2UV, float2 Mask3UV,
    float NB_WrapFlagsLo16, float NB_WrapFlagsHi16,
    float MaskMapGradientCount, float4 MaskMapGradientFloat0,
    float4 MaskMapGradientFloat1, float4 MaskMapGradientFloat2,
    float MaskMap2GradientCount, float4 MaskMap2GradientFloat0,
    float4 MaskMap2GradientFloat1, float4 MaskMap2GradientFloat2,
    float MaskMap3GradientCount, float4 MaskMap3GradientFloat0,
    float4 MaskMap3GradientFloat1, float4 MaskMap3GradientFloat2,
    float AlphaAll, float4 ColorA, float4 VertexColor,
    float BaseColorIntensityForTimeline, float4 BaseBackColor, float IsFrontFace,
    UnityTexture2D EmissionMap, float EmissionEnabled, float2 EmissionUV,
    float4 EmissionMapUVOffset, float EmissionMapUVRotation,
    float4 EmissionMapColor, float EmissionMapColorIntensity,
    float EmissionAlphaIntensity,
    UnityTexture2D ColorBlendMap, float ColorBlendMapToggle,
    float2 ColorBlendUV, float4 ColorBlendMapOffset,
    float4 ColorBlendVec, float4 ColorBlendColor,
    float ColorBlendColorIntensity,
    UnityTexture2D RampColorMap, float RampColorToggle,
    float RampColorSourceMode, float2 RampColorUV,
    float4 RampColorMapOffset, float4 RampColor0, float4 RampColor1,
    float4 RampColor2, float4 RampColor3, float4 RampColor4,
    float4 RampColor5, float4 RampColorAlpha0, float4 RampColorAlpha1,
    float4 RampColorAlpha2, float RampColorCount,
    float4 RampColorBlendColor,
    float HueShift, float Contrast, float4 ContrastMidColor,
    float Saturability, float4 BaseMapColorRefine,
    float FresnelEnabled, float4 FresnelUnit, float4 FresnelColor,
    float4 FresnelRotation, float3 NormalWS, float3 ViewDirWS,
    float4 DissolveLineRange, float4 DissolveLineColor,
    float DissolveRampToggle, UnityTexture2D DissolveRampMap,
    float DissolveRampSourceMode, float4 DissolveRampColor,
    float DissolveRampCount, float4 DissolveRampColor0,
    float4 DissolveRampColor1, float4 DissolveRampColor2,
    float4 DissolveRampColor3, float4 DissolveRampColor4,
    float4 DissolveRampColor5, float4 DissolveRampAlpha0,
    float4 DissolveRampAlpha1, float4 DissolveRampAlpha2,
    float NB_ForceNoMipFlagsLo16, float NB_ForceNoMipFlagsHi16,
    float NB_DissolveRampSTOverrideEnabled, float4 NB_DissolveRampSTOverride,
    float DistanceFadeToggle, float4 Fade, float3 PositionVS,
    float SoftParticlesEnabled, float4 SoftParticleFadeParams, float4 ScreenPosition,
    float DepthOutlineToggle, float4 DepthOutlineColor, float4 DepthOutlineVec,
    UnityTexture2D BaseMap, float2 BaseMapUV,
    UnityTexture2D NoiseMap, float NoiseEnabled, float2 NoiseUV,
    float NoiseMapUVRotation, float4 NoiseOffset, float NoiseIntensity,
    float4 DistortionDirection, UnityTexture2D NoiseMaskMap,
    float NoiseMaskToggle, float2 NoiseMaskUV,
    float TexDistortionIntensity, float EmiDistortionIntensity,
    float MaskDistortionIntensity,
    float MatCapToggle, UnityTexture2D MatCapTex,
    float4 MatCapColor, float4 MatCapInfo,
    out float4 Out, out float2 NBDistortionSignedRG,
    out float NBDistortionNoiseMask)
{
    NBFX_BaseColorInputV1 input = (NBFX_BaseColorInputV1)0;
    uint wrapFlags = NBGraphDecodeUInt32(NB_WrapFlagsLo16, NB_WrapFlagsHi16);
    uint noMipFlags = NBGraphDecodeUInt32(NB_ForceNoMipFlagsLo16, NB_ForceNoMipFlagsHi16);
    // Preserve the existing Noise-off uniform prototype for screen passes;
    // it must not distort the surface's texture consumers when Noise is off.
    half2 signedRG = (half2)NB_DistortionNoise;
    half noiseMask = 1;
    half2 textureNoise = 0;
    if (NoiseEnabled > 0.5)
    {
        NBGraphTextureNoise(NoiseMap, NoiseUV,
            NoiseMapUVRotation, (half4)NoiseOffset, (half)NoiseIntensity,
            (half4)DistortionDirection, NoiseMaskMap, NoiseMaskUV,
            NoiseMaskToggle > 0.5,
            NBGraphDecodeUInt32(NB_Flags0Lo16, NB_Flags0Hi16),
            NBGraphDecodeUInt32(NB_ColorChannelLo16, 0.0), wrapFlags, noMipFlags,
            signedRG, noiseMask);
        textureNoise = signedRG * noiseMask;
    }
    NBDistortionSignedRG = (float2)signedRG;
    NBDistortionNoiseMask = (float)noiseMask;
    float2 mainTexNoise = textureNoise * (half)TexDistortionIntensity;
    float2 baseUV = BaseMapUV + mainTexNoise;
    half4 baseSample = NBGraphSampleMap(BaseMap, baseUV,
        NBGraphMaskWrapMode(wrapFlags, FLAG_BIT_WRAPMODE_BASEMAP),
        (noMipFlags & FLAG_BIT_FORCE_NO_MIP_BASEMAP) != 0u);
    input.sampledAlbedo = baseSample;
    // Historical sampled-color/A ports remain serialized but are disconnected.
    // The packed channel word selects alpha from this one protocol-owned sample.
    input.selectedAlpha = NBGraphSelectBaseAlpha(baseSample,
        NB_ColorChannelLo16);
    input.effectiveBaseColor = (half4)EffectiveBaseColor;
    if ((NBGraphDecodeUInt32(NB_Flags0Lo16, NB_Flags0Hi16) &
        FLAG_BIT_PARTICLE_BACKCOLOR) != 0u && IsFrontFace < 0.5)
        input.effectiveBaseColor = (half4)BaseBackColor;
    input.timelineIntensity = (half)BaseColorIntensityForTimeline;
    input.applyTimelineIntensity = true;
    Out = (float4)NBFX_ComposeBaseColorV1(input);
    uint adjustmentFlags0 = NBGraphDecodeUInt32(NB_Flags0Lo16, NB_Flags0Hi16);
    if ((adjustmentFlags0 & FLAG_BIT_PARTICLE_COLOR_ADJUSTMENT_ONLY_AFFECT_MAINTEX) != 0u)
        Out = (float4)NBGraphApplyColorAdjustment((half4)Out,
            (half)HueShift, (half)Contrast, (half3)ContrastMidColor.rgb,
            (half)Saturability, (half4)BaseMapColorRefine,
            NB_Flags0Lo16, NB_Flags0Hi16, NB_Flags1Lo16, NB_Flags1Hi16);
    if (MatCapToggle > 0.5)
        Out = (float4)NBGraphApplyMatCap((half4)Out, MatCapTex, NormalWS,
            (half3)PositionVS, (half)IsFrontFace, (half4)MatCapColor,
            (half4)MatCapInfo, noMipFlags);
    if (EmissionEnabled > 0.5)
    {
        float2 emissionUV = NBGraphFeatureUV(EmissionMap, EmissionUV,
            EmissionMapUVRotation, EmissionMapUVOffset.xy);
        if (NoiseEnabled > 0.5)
            emissionUV += textureNoise * (half)EmiDistortionIntensity;
        half4 emission = NBGraphSampleMap(EmissionMap, emissionUV,
            NBGraphMaskWrapMode(wrapFlags, FLAG_BIT_WRAPMODE_EMISSIONMAP),
            (noMipFlags & FLAG_BIT_FORCE_NO_MIP_EMISSIONMAP) != 0u);
        half3 result = (half3)Out.rgb;
        half alpha = (half)Out.a;
        uint flags0 = NBGraphDecodeUInt32(NB_Flags0Lo16, NB_Flags0Hi16);
        uint flags1 = NBGraphDecodeUInt32(NB_Flags1Lo16, NB_Flags1Hi16);
        NBFX_ApplyColorOverlayV1(result, alpha, emission,
            (half4)EmissionMapColor, (half)EmissionMapColorIntensity,
            (half)EmissionAlphaIntensity,
            (flags0 & FLAG_BIT_PARTICLE_COLOR_OVERLAY_1_MULTIPLY) != 0u,
            (flags1 & FLAG_BIT_PARTICLE_1_COLOR_OVERLAY_1_ALPHA_MULTIPLY) != 0u);
        Out = float4(result, alpha);
    }
    if (RampColorToggle > 0.5)
        Out = (float4)NBGraphApplyColorRamp((half4)Out, RampColorMap,
            RampColorUV, RampColorMapOffset, RampColorSourceMode,
            RampColorCount, (half4)RampColor0, (half4)RampColor1,
            (half4)RampColor2, (half4)RampColor3, (half4)RampColor4,
            (half4)RampColor5, (half4)RampColorAlpha0,
            (half4)RampColorAlpha1, (half4)RampColorAlpha2,
            (half4)RampColorBlendColor, NB_ColorChannelLo16,
            NB_WrapFlagsLo16, NB_WrapFlagsHi16,
            NB_Flags0Lo16, NB_Flags0Hi16,
            NB_ForceNoMipFlagsLo16, NB_ForceNoMipFlagsHi16);
    if (DissolveToggle > 0.5)
    {
        float2 dissolveUV = NBGraphFeatureUV(DissolveMap, DissolveUV,
            DissolveOffsetRotateDistort.z, DissolveOffsetRotateDistort.xy);
        if (NoiseEnabled > 0.5)
            dissolveUV += textureNoise * (half)DissolveOffsetRotateDistort.w;
        half4 sampledDissolve = NBGraphSampleMap(DissolveMap, dissolveUV,
            NBGraphMaskWrapMode(wrapFlags, FLAG_BIT_WRAPMODE_DISSOLVE_MAP),
            (noMipFlags & FLAG_BIT_FORCE_NO_MIP_DISSOLVE_MAP) != 0u);
        bool hasDissolveMask = DissolveMaskToggle > 0.5;
        half4 sampledDissolveMask = (half4)0;
        if (hasDissolveMask)
        {
            float2 dissolveMaskUV = NBGraphFeatureUV(DissolveMaskMap, DissolveMaskUV,
                DissolveOffsetRotateDistort.z, float2(0.0, 0.0));
            if (NoiseEnabled > 0.5)
                dissolveMaskUV += textureNoise * (half)DissolveOffsetRotateDistort.w;
            sampledDissolveMask = NBGraphSampleMap(DissolveMaskMap, dissolveMaskUV,
            NBGraphMaskWrapMode(wrapFlags, FLAG_BIT_WRAPMODE_DISSOLVE_MASKMAP),
            (noMipFlags & FLAG_BIT_FORCE_NO_MIP_DISSOLVE_MASKMAP) != 0u);
        }
        NBFX_DissolveResolvedV3 resolved = NBGraphResolveDissolve(sampledDissolve,
            sampledDissolveMask, hasDissolveMask, (half4)Dissolve,
            (half)DissolveMaskMode, NB_ColorChannelLo16);
        Out.a *= resolved.coverage;
        if (DissolveRampToggle > 0.5)
            Out = (float4)NBGraphApplyDissolveRamp((half4)Out,
                resolved.valueBeforeSoftStep, DissolveRampMap,
                DissolveRampSourceMode, (half4)DissolveRampColor,
                DissolveRampCount, (half4)DissolveRampColor0,
                (half4)DissolveRampColor1, (half4)DissolveRampColor2,
                (half4)DissolveRampColor3, (half4)DissolveRampColor4,
                (half4)DissolveRampColor5, (half4)DissolveRampAlpha0,
                (half4)DissolveRampAlpha1, (half4)DissolveRampAlpha2,
                NB_WrapFlagsLo16, NB_WrapFlagsHi16,
                NB_ForceNoMipFlagsLo16, NB_ForceNoMipFlagsHi16,
                NB_Flags1Lo16, NB_Flags1Hi16,
                NB_DissolveRampSTOverrideEnabled, NB_DissolveRampSTOverride);
        Out = (float4)NBGraphApplyDissolveLine((half4)Out,
            resolved.valueBeforeSoftStep, (half4)DissolveLineRange,
            (half4)DissolveLineColor,
            NBGraphDecodeUInt32(NB_Flags1Lo16, NB_Flags1Hi16));
    }
    // Overlay 2 keeps its ShaderLab position after Dissolve and before Mask.
    // N1 texture Noise offsets this layer; CustomData remains a later slice.
    if (ColorBlendMapToggle > 0.5)
    {
        float2 overlayUV = NBGraphFeatureUV(ColorBlendMap, ColorBlendUV,
            ColorBlendVec.w, ColorBlendMapOffset.xy);
        if (NoiseEnabled > 0.5)
            overlayUV += textureNoise * (half)ColorBlendVec.x;
        half4 overlay = NBGraphSampleMap(ColorBlendMap, overlayUV,
            NBGraphMaskWrapMode(wrapFlags, FLAG_BIT_WRAPMODE_COLORBLENDMAP),
            (noMipFlags & FLAG_BIT_FORCE_NO_MIP_COLORBLENDMAP) != 0u);
        half3 result = (half3)Out.rgb;
        half alpha = (half)Out.a;
        uint flags0 = NBGraphDecodeUInt32(NB_Flags0Lo16, NB_Flags0Hi16);
        uint flags1 = NBGraphDecodeUInt32(NB_Flags1Lo16, NB_Flags1Hi16);
        NBFX_ApplyColorOverlayV1(result, alpha, overlay,
            (half4)ColorBlendColor, (half)ColorBlendColorIntensity,
            (half)ColorBlendVec.z,
            (flags1 & FLAG_BIT_PARTICLE_1_COLOR_OVERLAY_2_ADD) == 0u,
            (flags0 & FLAG_BIT_PARTICLE_COLOR_BLEND_ALPHA_MULTIPLY_MODE) != 0u);
        Out = float4(result, alpha);
    }
    if (MaskToggle > 0.5)
    {
        uint maskFlags = NBGraphDecodeUInt32(NB_Flags1Lo16, NB_Flags1Hi16);
        float maskRotation = MaskMapUVRotation;
        if ((NBGraphDecodeUInt32(NB_Flags0Lo16, NB_Flags0Hi16) &
            FLAG_BIT_PARTILCE_MASKMAPROTATIONANIMATION_ON) != 0u)
            maskRotation += _Time.y * MaskMapRotationSpeed;
        float2 maskUV = NBGraphFeatureUV(MaskMap, MaskUV,
            maskRotation, MaskMapOffsetAnition.xy);
        if (NoiseEnabled > 0.5)
            maskUV += textureNoise * (half)MaskDistortionIntensity;
        uint packedChannels = NBGraphDecodeUInt32(NB_ColorChannelLo16, 0.0);
        half channelValue;
        if ((maskFlags & FLAG_BIT_PARTICLE_1_MASKMAP_GRADIENT) != 0u)
            channelValue = NBGraphSampleMaskGradient(
                (half4)MaskMapGradientFloat0, (half4)MaskMapGradientFloat1,
                (half4)MaskMapGradientFloat2, MaskMapGradientCount, maskUV,
                NBGraphMaskWrapMode(wrapFlags, FLAG_BIT_WRAPMODE_MASKMAP), false);
        else
        {
            half4 sampledMask = NBGraphSampleMap(MaskMap, maskUV,
            NBGraphMaskWrapMode(wrapFlags, FLAG_BIT_WRAPMODE_MASKMAP),
            (noMipFlags & FLAG_BIT_FORCE_NO_MIP_MASKMAP) != 0u);
            uint maskChannel = (packedChannels >> FLAG_BIT_COLOR_CHANNEL_POS_0_MASKMAP1) & 3u;
            channelValue = maskChannel == 0u ? sampledMask.r :
                maskChannel == 1u ? sampledMask.g :
                maskChannel == 2u ? sampledMask.b : sampledMask.a;
        }
        if (Mask2Toggle > 0.5)
        {
            if ((maskFlags & FLAG_BIT_PARTICLE_1_MASKMAP_2_GRADIENT) != 0u)
            {
                float2 mask2UV = NBGraphFeatureUV(MaskMap2, Mask2UV,
                    MaskMapVec.y, MaskMapOffsetAnition.zw);
                channelValue *= NBGraphSampleMaskGradient(
                    (half4)MaskMap2GradientFloat0, (half4)MaskMap2GradientFloat1,
                    (half4)MaskMap2GradientFloat2, MaskMap2GradientCount, mask2UV,
                    NBGraphMaskWrapMode(wrapFlags, FLAG_BIT_WRAPMODE_MASKMAP2), true);
            }
            else
                channelValue *= NBGraphSampleMaskLayer(MaskMap2, Mask2UV,
                    MaskMapVec.y, MaskMapOffsetAnition.zw, packedChannels,
                    FLAG_BIT_COLOR_CHANNEL_POS_0_MASKMAP2,
                    wrapFlags, FLAG_BIT_WRAPMODE_MASKMAP2,
                    noMipFlags, FLAG_BIT_FORCE_NO_MIP_MASKMAP2);
        }
        if (Mask3Toggle > 0.5)
        {
            if ((maskFlags & FLAG_BIT_PARTICLE_1_MASKMAP_3_GRADIENT) != 0u)
            {
                float2 mask3UV = NBGraphFeatureUV(MaskMap3, Mask3UV,
                    MaskMapVec.z, MaskMap3OffsetAnition.xy);
                channelValue *= NBGraphSampleMaskGradient(
                    (half4)MaskMap3GradientFloat0, (half4)MaskMap3GradientFloat1,
                    (half4)MaskMap3GradientFloat2, MaskMap3GradientCount, mask3UV,
                    NBGraphMaskWrapMode(wrapFlags, FLAG_BIT_WRAPMODE_MASKMAP3), false);
            }
            else
                channelValue *= NBGraphSampleMaskLayer(MaskMap3, Mask3UV,
                    MaskMapVec.z, MaskMap3OffsetAnition.xy, packedChannels,
                    FLAG_BIT_COLOR_CHANNEL_POS_0_MASKMAP3,
                    wrapFlags, FLAG_BIT_WRAPMODE_MASKMAP3,
                    noMipFlags, FLAG_BIT_FORCE_NO_MIP_MASKMAP3);
        }
        NBFX_MaskCoverageInputV3 maskInput = (NBFX_MaskCoverageInputV3)0;
        maskInput.combinedMaskAfterNoise = channelValue;
        maskInput.refine = (maskFlags & FLAG_BIT_PARTICLE_1_MASK_REFINE) != 0u;
        maskInput.refinePowMulAdd = (half3)MaskRefineVec.xyz;
        maskInput.overallStrength = (half)MaskMapVec.x;
        Out.a *= NBFX_ResolveMaskCoverageV3(maskInput);
    }
    if (FresnelEnabled > 0.5)
        Out = (float4)NBGraphApplyFresnel((half4)Out, ViewDirWS,
            (half3)NormalWS, (half)IsFrontFace, (half4)FresnelUnit,
            (half4)FresnelColor, (half3)FresnelRotation.xyz,
            NBGraphDecodeUInt32(NB_Flags0Lo16, NB_Flags0Hi16));
    // ShaderLab applies camera-distance alpha after Fresnel, before vertex color.
    float sceneEyeDepth = 0.0;
    if (DepthOutlineToggle > 0.5 || SoftParticlesEnabled > 0.5)
        sceneEyeDepth = NBGraphSceneEyeDepth(ScreenPosition.xy);
    if (DepthOutlineToggle > 0.5)
    {
        half3 outlineRGB = (half3)Out.rgb;
        half outlineAlpha = (half)Out.a;
        NBFX_ApplyDepthOutlineV1(outlineRGB, outlineAlpha,
            (half4)DepthOutlineColor, (half2)DepthOutlineVec.xy,
            sceneEyeDepth, -PositionVS.z);
        Out = float4(outlineRGB, outlineAlpha);
    }
    if (DistanceFadeToggle > 0.5)
        Out.a *= DepthFactor(-PositionVS.z, Fade.x, Fade.y);
    if (SoftParticlesEnabled > 0.5)
        Out.a *= NBFX_SoftParticlesV1(SoftParticleFadeParams.x,
            SoftParticleFadeParams.y, sceneEyeDepth,
            -PositionVS.z);
    if ((NBGraphDecodeUInt32(NB_Flags1Lo16, NB_Flags1Hi16) &
        FLAG_BIT_PARTICLE_1_IGNORE_VERTEX_COLOR) == 0u)
        Out *= VertexColor;
    Out.rgb *= ColorA.rgb;
    Out.a *= ColorA.a;
    if ((adjustmentFlags0 & FLAG_BIT_PARTICLE_COLOR_ADJUSTMENT_ONLY_AFFECT_MAINTEX) == 0u)
        Out = (float4)NBGraphApplyColorAdjustment((half4)Out,
            (half)HueShift, (half)Contrast, (half3)ContrastMidColor.rgb,
            (half)Saturability, (half4)BaseMapColorRefine,
            NB_Flags0Lo16, NB_Flags0Hi16, NB_Flags1Lo16, NB_Flags1Hi16);
    Out.a = saturate(Out.a * AlphaAll);
}

void NBGraphBaseColor_half(half4 SampledAlbedo, half SelectedAlpha,
    half4 EffectiveBaseColor,
    float NB_Flags0Lo16, float NB_Flags0Hi16,
    float NB_Flags1Lo16, float NB_Flags1Hi16,
    float2 NB_DistortionNoise, float NB_DistortionIntensity, float NB_DistortionMode,
    float NB_DistortionAlphaPow, float NB_DistortionAlphaMultiplier, float NB_DistortionAlphaAdd,
    UnityTexture2D MaskMap, float MaskToggle, half4 MaskMapVec, half4 MaskRefineVec,
    float NB_ColorChannelLo16,
    UnityTexture2D DissolveMap, float DissolveToggle, half4 Dissolve,
    float2 MaskUV, float2 DissolveUV,
    float MaskMapUVRotation, float MaskMapRotationSpeed, half4 MaskMapOffsetAnition,
    half4 DissolveOffsetRotateDistort,
    UnityTexture2D DissolveMaskMap, float DissolveMaskToggle, float DissolveMaskMode,
    float2 DissolveMaskUV,
    UnityTexture2D MaskMap2, float Mask2Toggle,
    UnityTexture2D MaskMap3, float Mask3Toggle, half4 MaskMap3OffsetAnition,
    float2 Mask2UV, float2 Mask3UV,
    float NB_WrapFlagsLo16, float NB_WrapFlagsHi16,
    float MaskMapGradientCount, half4 MaskMapGradientFloat0,
    half4 MaskMapGradientFloat1, half4 MaskMapGradientFloat2,
    float MaskMap2GradientCount, half4 MaskMap2GradientFloat0,
    half4 MaskMap2GradientFloat1, half4 MaskMap2GradientFloat2,
    float MaskMap3GradientCount, half4 MaskMap3GradientFloat0,
    half4 MaskMap3GradientFloat1, half4 MaskMap3GradientFloat2,
    float AlphaAll, half4 ColorA, half4 VertexColor,
    float BaseColorIntensityForTimeline, half4 BaseBackColor, float IsFrontFace,
    UnityTexture2D EmissionMap, float EmissionEnabled, float2 EmissionUV,
    half4 EmissionMapUVOffset, float EmissionMapUVRotation,
    half4 EmissionMapColor, float EmissionMapColorIntensity,
    float EmissionAlphaIntensity,
    UnityTexture2D ColorBlendMap, float ColorBlendMapToggle,
    float2 ColorBlendUV, half4 ColorBlendMapOffset,
    half4 ColorBlendVec, half4 ColorBlendColor,
    float ColorBlendColorIntensity,
    UnityTexture2D RampColorMap, float RampColorToggle,
    float RampColorSourceMode, float2 RampColorUV,
    half4 RampColorMapOffset, half4 RampColor0, half4 RampColor1,
    half4 RampColor2, half4 RampColor3, half4 RampColor4,
    half4 RampColor5, half4 RampColorAlpha0, half4 RampColorAlpha1,
    half4 RampColorAlpha2, float RampColorCount,
    half4 RampColorBlendColor,
    float HueShift, float Contrast, half4 ContrastMidColor,
    float Saturability, half4 BaseMapColorRefine,
    float FresnelEnabled, half4 FresnelUnit, half4 FresnelColor,
    half4 FresnelRotation, half3 NormalWS, half3 ViewDirWS,
    half4 DissolveLineRange, half4 DissolveLineColor,
    float DissolveRampToggle, UnityTexture2D DissolveRampMap,
    float DissolveRampSourceMode, half4 DissolveRampColor,
    float DissolveRampCount, half4 DissolveRampColor0,
    half4 DissolveRampColor1, half4 DissolveRampColor2,
    half4 DissolveRampColor3, half4 DissolveRampColor4,
    half4 DissolveRampColor5, half4 DissolveRampAlpha0,
    half4 DissolveRampAlpha1, half4 DissolveRampAlpha2,
    float NB_ForceNoMipFlagsLo16, float NB_ForceNoMipFlagsHi16,
    float NB_DissolveRampSTOverrideEnabled, float4 NB_DissolveRampSTOverride,
    float DistanceFadeToggle, half4 Fade, float3 PositionVS,
    float SoftParticlesEnabled, float4 SoftParticleFadeParams, float4 ScreenPosition,
    float DepthOutlineToggle, half4 DepthOutlineColor, half4 DepthOutlineVec,
    UnityTexture2D BaseMap, float2 BaseMapUV,
    UnityTexture2D NoiseMap, float NoiseEnabled, float2 NoiseUV,
    float NoiseMapUVRotation, half4 NoiseOffset, float NoiseIntensity,
    half4 DistortionDirection, UnityTexture2D NoiseMaskMap,
    float NoiseMaskToggle, float2 NoiseMaskUV,
    float TexDistortionIntensity, float EmiDistortionIntensity,
    float MaskDistortionIntensity,
    float MatCapToggle, UnityTexture2D MatCapTex,
    half4 MatCapColor, half4 MatCapInfo,
    out half4 Out, out half2 NBDistortionSignedRG,
    out half NBDistortionNoiseMask)
{
    NBFX_BaseColorInputV1 input = (NBFX_BaseColorInputV1)0;
    uint wrapFlags = NBGraphDecodeUInt32(NB_WrapFlagsLo16, NB_WrapFlagsHi16);
    uint noMipFlags = NBGraphDecodeUInt32(NB_ForceNoMipFlagsLo16, NB_ForceNoMipFlagsHi16);
    half2 signedRG = (half2)NB_DistortionNoise;
    half noiseMask = 1;
    half2 textureNoise = 0;
    if (NoiseEnabled > 0.5)
    {
        NBGraphTextureNoise(NoiseMap, NoiseUV,
            NoiseMapUVRotation, (half4)NoiseOffset, (half)NoiseIntensity,
            (half4)DistortionDirection, NoiseMaskMap, NoiseMaskUV,
            NoiseMaskToggle > 0.5,
            NBGraphDecodeUInt32(NB_Flags0Lo16, NB_Flags0Hi16),
            NBGraphDecodeUInt32(NB_ColorChannelLo16, 0.0), wrapFlags, noMipFlags,
            signedRG, noiseMask);
        textureNoise = signedRG * noiseMask;
    }
    NBDistortionSignedRG = signedRG;
    NBDistortionNoiseMask = noiseMask;
    float2 mainTexNoise = textureNoise * (half)TexDistortionIntensity;
    float2 baseUV = BaseMapUV + mainTexNoise;
    half4 baseSample = NBGraphSampleMap(BaseMap, baseUV,
        NBGraphMaskWrapMode(wrapFlags, FLAG_BIT_WRAPMODE_BASEMAP),
        (noMipFlags & FLAG_BIT_FORCE_NO_MIP_BASEMAP) != 0u);
    input.sampledAlbedo = baseSample;
    input.selectedAlpha = NBGraphSelectBaseAlpha(baseSample,
        NB_ColorChannelLo16);
    input.effectiveBaseColor = EffectiveBaseColor;
    if ((NBGraphDecodeUInt32(NB_Flags0Lo16, NB_Flags0Hi16) &
        FLAG_BIT_PARTICLE_BACKCOLOR) != 0u && IsFrontFace < 0.5)
        input.effectiveBaseColor = BaseBackColor;
    input.timelineIntensity = (half)BaseColorIntensityForTimeline;
    input.applyTimelineIntensity = true;
    Out = NBFX_ComposeBaseColorV1(input);
    uint adjustmentFlags0 = NBGraphDecodeUInt32(NB_Flags0Lo16, NB_Flags0Hi16);
    if ((adjustmentFlags0 & FLAG_BIT_PARTICLE_COLOR_ADJUSTMENT_ONLY_AFFECT_MAINTEX) != 0u)
        Out = NBGraphApplyColorAdjustment(Out,
            (half)HueShift, (half)Contrast, ContrastMidColor.rgb,
            (half)Saturability, BaseMapColorRefine,
            NB_Flags0Lo16, NB_Flags0Hi16, NB_Flags1Lo16, NB_Flags1Hi16);
    if (MatCapToggle > 0.5)
        Out = NBGraphApplyMatCap(Out, MatCapTex, (float3)NormalWS,
            (half3)PositionVS, (half)IsFrontFace, MatCapColor,
            MatCapInfo, noMipFlags);
    if (EmissionEnabled > 0.5)
    {
        float2 emissionUV = NBGraphFeatureUV(EmissionMap, EmissionUV,
            EmissionMapUVRotation, EmissionMapUVOffset.xy);
        if (NoiseEnabled > 0.5)
            emissionUV += textureNoise * (half)EmiDistortionIntensity;
        half4 emission = NBGraphSampleMap(EmissionMap, emissionUV,
            NBGraphMaskWrapMode(wrapFlags, FLAG_BIT_WRAPMODE_EMISSIONMAP),
            (noMipFlags & FLAG_BIT_FORCE_NO_MIP_EMISSIONMAP) != 0u);
        uint flags0 = NBGraphDecodeUInt32(NB_Flags0Lo16, NB_Flags0Hi16);
        uint flags1 = NBGraphDecodeUInt32(NB_Flags1Lo16, NB_Flags1Hi16);
        NBFX_ApplyColorOverlayV1(Out.rgb, Out.a, emission,
            EmissionMapColor, (half)EmissionMapColorIntensity,
            (half)EmissionAlphaIntensity,
            (flags0 & FLAG_BIT_PARTICLE_COLOR_OVERLAY_1_MULTIPLY) != 0u,
            (flags1 & FLAG_BIT_PARTICLE_1_COLOR_OVERLAY_1_ALPHA_MULTIPLY) != 0u);
    }
    if (RampColorToggle > 0.5)
        Out = NBGraphApplyColorRamp(Out, RampColorMap,
            RampColorUV, RampColorMapOffset, RampColorSourceMode,
            RampColorCount, RampColor0, RampColor1,
            RampColor2, RampColor3, RampColor4, RampColor5,
            RampColorAlpha0, RampColorAlpha1, RampColorAlpha2,
            RampColorBlendColor, NB_ColorChannelLo16,
            NB_WrapFlagsLo16, NB_WrapFlagsHi16,
            NB_Flags0Lo16, NB_Flags0Hi16,
            NB_ForceNoMipFlagsLo16, NB_ForceNoMipFlagsHi16);
    if (DissolveToggle > 0.5)
    {
        float2 dissolveUV = NBGraphFeatureUV(DissolveMap, DissolveUV,
            DissolveOffsetRotateDistort.z, DissolveOffsetRotateDistort.xy);
        if (NoiseEnabled > 0.5)
            dissolveUV += textureNoise * (half)DissolveOffsetRotateDistort.w;
        half4 sampledDissolve = NBGraphSampleMap(DissolveMap, dissolveUV,
            NBGraphMaskWrapMode(wrapFlags, FLAG_BIT_WRAPMODE_DISSOLVE_MAP),
            (noMipFlags & FLAG_BIT_FORCE_NO_MIP_DISSOLVE_MAP) != 0u);
        bool hasDissolveMask = DissolveMaskToggle > 0.5;
        half4 sampledDissolveMask = (half4)0;
        if (hasDissolveMask)
        {
            float2 dissolveMaskUV = NBGraphFeatureUV(DissolveMaskMap, DissolveMaskUV,
                DissolveOffsetRotateDistort.z, float2(0.0, 0.0));
            if (NoiseEnabled > 0.5)
                dissolveMaskUV += textureNoise * (half)DissolveOffsetRotateDistort.w;
            sampledDissolveMask = NBGraphSampleMap(DissolveMaskMap, dissolveMaskUV,
            NBGraphMaskWrapMode(wrapFlags, FLAG_BIT_WRAPMODE_DISSOLVE_MASKMAP),
            (noMipFlags & FLAG_BIT_FORCE_NO_MIP_DISSOLVE_MASKMAP) != 0u);
        }
        NBFX_DissolveResolvedV3 resolved = NBGraphResolveDissolve(sampledDissolve,
            sampledDissolveMask, hasDissolveMask, Dissolve,
            (half)DissolveMaskMode, NB_ColorChannelLo16);
        Out.a *= resolved.coverage;
        if (DissolveRampToggle > 0.5)
            Out = NBGraphApplyDissolveRamp(Out,
                resolved.valueBeforeSoftStep, DissolveRampMap,
                DissolveRampSourceMode, DissolveRampColor,
                DissolveRampCount, DissolveRampColor0,
                DissolveRampColor1, DissolveRampColor2,
                DissolveRampColor3, DissolveRampColor4,
                DissolveRampColor5, DissolveRampAlpha0,
                DissolveRampAlpha1, DissolveRampAlpha2,
                NB_WrapFlagsLo16, NB_WrapFlagsHi16,
                NB_ForceNoMipFlagsLo16, NB_ForceNoMipFlagsHi16,
                NB_Flags1Lo16, NB_Flags1Hi16,
                NB_DissolveRampSTOverrideEnabled, NB_DissolveRampSTOverride);
        Out = NBGraphApplyDissolveLine(Out, resolved.valueBeforeSoftStep,
            DissolveLineRange, DissolveLineColor,
            NBGraphDecodeUInt32(NB_Flags1Lo16, NB_Flags1Hi16));
    }
    if (ColorBlendMapToggle > 0.5)
    {
        float2 overlayUV = NBGraphFeatureUV(ColorBlendMap, ColorBlendUV,
            ColorBlendVec.w, ColorBlendMapOffset.xy);
        if (NoiseEnabled > 0.5)
            overlayUV += textureNoise * (half)ColorBlendVec.x;
        half4 overlay = NBGraphSampleMap(ColorBlendMap, overlayUV,
            NBGraphMaskWrapMode(wrapFlags, FLAG_BIT_WRAPMODE_COLORBLENDMAP),
            (noMipFlags & FLAG_BIT_FORCE_NO_MIP_COLORBLENDMAP) != 0u);
        uint flags0 = NBGraphDecodeUInt32(NB_Flags0Lo16, NB_Flags0Hi16);
        uint flags1 = NBGraphDecodeUInt32(NB_Flags1Lo16, NB_Flags1Hi16);
        NBFX_ApplyColorOverlayV1(Out.rgb, Out.a, overlay,
            ColorBlendColor, (half)ColorBlendColorIntensity,
            (half)ColorBlendVec.z,
            (flags1 & FLAG_BIT_PARTICLE_1_COLOR_OVERLAY_2_ADD) == 0u,
            (flags0 & FLAG_BIT_PARTICLE_COLOR_BLEND_ALPHA_MULTIPLY_MODE) != 0u);
    }
    if (MaskToggle > 0.5)
    {
        uint maskFlags = NBGraphDecodeUInt32(NB_Flags1Lo16, NB_Flags1Hi16);
        float maskRotation = MaskMapUVRotation;
        if ((NBGraphDecodeUInt32(NB_Flags0Lo16, NB_Flags0Hi16) &
            FLAG_BIT_PARTILCE_MASKMAPROTATIONANIMATION_ON) != 0u)
            maskRotation += _Time.y * MaskMapRotationSpeed;
        float2 maskUV = NBGraphFeatureUV(MaskMap, MaskUV,
            maskRotation, MaskMapOffsetAnition.xy);
        if (NoiseEnabled > 0.5)
            maskUV += textureNoise * (half)MaskDistortionIntensity;
        uint packedChannels = NBGraphDecodeUInt32(NB_ColorChannelLo16, 0.0);
        half channelValue;
        if ((maskFlags & FLAG_BIT_PARTICLE_1_MASKMAP_GRADIENT) != 0u)
            channelValue = NBGraphSampleMaskGradient(
                MaskMapGradientFloat0, MaskMapGradientFloat1,
                MaskMapGradientFloat2, MaskMapGradientCount, maskUV,
                NBGraphMaskWrapMode(wrapFlags, FLAG_BIT_WRAPMODE_MASKMAP), false);
        else
        {
            half4 sampledMask = NBGraphSampleMap(MaskMap, maskUV,
            NBGraphMaskWrapMode(wrapFlags, FLAG_BIT_WRAPMODE_MASKMAP),
            (noMipFlags & FLAG_BIT_FORCE_NO_MIP_MASKMAP) != 0u);
            uint maskChannel = (packedChannels >> FLAG_BIT_COLOR_CHANNEL_POS_0_MASKMAP1) & 3u;
            channelValue = maskChannel == 0u ? sampledMask.r :
                maskChannel == 1u ? sampledMask.g :
                maskChannel == 2u ? sampledMask.b : sampledMask.a;
        }
        if (Mask2Toggle > 0.5)
        {
            if ((maskFlags & FLAG_BIT_PARTICLE_1_MASKMAP_2_GRADIENT) != 0u)
            {
                float2 mask2UV = NBGraphFeatureUV(MaskMap2, Mask2UV,
                    MaskMapVec.y, MaskMapOffsetAnition.zw);
                channelValue *= NBGraphSampleMaskGradient(
                    MaskMap2GradientFloat0, MaskMap2GradientFloat1,
                    MaskMap2GradientFloat2, MaskMap2GradientCount, mask2UV,
                    NBGraphMaskWrapMode(wrapFlags, FLAG_BIT_WRAPMODE_MASKMAP2), true);
            }
            else
                channelValue *= NBGraphSampleMaskLayer(MaskMap2, Mask2UV,
                    MaskMapVec.y, MaskMapOffsetAnition.zw, packedChannels,
                    FLAG_BIT_COLOR_CHANNEL_POS_0_MASKMAP2,
                    wrapFlags, FLAG_BIT_WRAPMODE_MASKMAP2,
                    noMipFlags, FLAG_BIT_FORCE_NO_MIP_MASKMAP2);
        }
        if (Mask3Toggle > 0.5)
        {
            if ((maskFlags & FLAG_BIT_PARTICLE_1_MASKMAP_3_GRADIENT) != 0u)
            {
                float2 mask3UV = NBGraphFeatureUV(MaskMap3, Mask3UV,
                    MaskMapVec.z, MaskMap3OffsetAnition.xy);
                channelValue *= NBGraphSampleMaskGradient(
                    MaskMap3GradientFloat0, MaskMap3GradientFloat1,
                    MaskMap3GradientFloat2, MaskMap3GradientCount, mask3UV,
                    NBGraphMaskWrapMode(wrapFlags, FLAG_BIT_WRAPMODE_MASKMAP3), false);
            }
            else
                channelValue *= NBGraphSampleMaskLayer(MaskMap3, Mask3UV,
                    MaskMapVec.z, MaskMap3OffsetAnition.xy, packedChannels,
                    FLAG_BIT_COLOR_CHANNEL_POS_0_MASKMAP3,
                    wrapFlags, FLAG_BIT_WRAPMODE_MASKMAP3,
                    noMipFlags, FLAG_BIT_FORCE_NO_MIP_MASKMAP3);
        }
        NBFX_MaskCoverageInputV3 maskInput = (NBFX_MaskCoverageInputV3)0;
        maskInput.combinedMaskAfterNoise = channelValue;
        maskInput.refine = (maskFlags & FLAG_BIT_PARTICLE_1_MASK_REFINE) != 0u;
        maskInput.refinePowMulAdd = MaskRefineVec.xyz;
        maskInput.overallStrength = MaskMapVec.x;
        Out.a *= NBFX_ResolveMaskCoverageV3(maskInput);
    }
    if (FresnelEnabled > 0.5)
        Out = NBGraphApplyFresnel(Out, ViewDirWS, NormalWS,
            (half)IsFrontFace, FresnelUnit, FresnelColor,
            FresnelRotation.xyz,
            NBGraphDecodeUInt32(NB_Flags0Lo16, NB_Flags0Hi16));
    float sceneEyeDepth = 0.0;
    if (DepthOutlineToggle > 0.5 || SoftParticlesEnabled > 0.5)
        sceneEyeDepth = NBGraphSceneEyeDepth(ScreenPosition.xy);
    if (DepthOutlineToggle > 0.5)
        NBFX_ApplyDepthOutlineV1(Out.rgb, Out.a, DepthOutlineColor,
            DepthOutlineVec.xy, sceneEyeDepth, -PositionVS.z);
    if (DistanceFadeToggle > 0.5)
        Out.a *= DepthFactor(-PositionVS.z, Fade.x, Fade.y);
    if (SoftParticlesEnabled > 0.5)
        Out.a *= NBFX_SoftParticlesV1(SoftParticleFadeParams.x,
            SoftParticleFadeParams.y, sceneEyeDepth,
            -PositionVS.z);
    if ((NBGraphDecodeUInt32(NB_Flags1Lo16, NB_Flags1Hi16) &
        FLAG_BIT_PARTICLE_1_IGNORE_VERTEX_COLOR) == 0u)
        Out *= VertexColor;
    Out.rgb *= ColorA.rgb;
    Out.a *= ColorA.a;
    if ((adjustmentFlags0 & FLAG_BIT_PARTICLE_COLOR_ADJUSTMENT_ONLY_AFFECT_MAINTEX) == 0u)
        Out = NBGraphApplyColorAdjustment(Out,
            (half)HueShift, (half)Contrast, ContrastMidColor.rgb,
            (half)Saturability, BaseMapColorRefine,
            NB_Flags0Lo16, NB_Flags0Hi16, NB_Flags1Lo16, NB_Flags1Hi16);
    Out.a = saturate(Out.a * (half)AlphaAll);
}

#endif
