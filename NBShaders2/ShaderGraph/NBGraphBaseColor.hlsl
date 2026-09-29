#ifndef NB_GRAPH_BASE_COLOR_INCLUDED
#define NB_GRAPH_BASE_COLOR_INCLUDED

#include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderSurfaceV1.hlsl"
#include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderMaskV3.hlsl"
#include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderDissolveV3.hlsl"
#include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderPackedGradientV1.hlsl"
#include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderUVV2.hlsl"
#include "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBGraphFlags.hlsl"

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

// Match Shader Graph's Sample Texture 2D node, including optional HDR decode.
// The caller invokes this only inside an enabled feature, with transformed UV.
float4 NBGraphSampleMap(UnityTexture2D map, float2 transformedUV)
{
    float4 sampled = SAMPLE_TEXTURE2D(map.tex, map.samplerstate, transformedUV);
    if (map.hdrDecode.x > 0.0)
        sampled = DecodeHDRSample(sampled, map.hdrDecode);
    return sampled;
}

// Texture-only layer 2/3 adapter. Both layers multiply before the one shared
// NBFX_ResolveMaskCoverageV3 call, matching the ShaderLab sampling branch.
half NBGraphSampleMaskLayer(UnityTexture2D map, float2 originUV,
    float rotationDegrees, float2 offsetSpeed, uint packedChannels,
    uint channelPosition)
{
    float2 uv = NBGraphFeatureUV(map, originUV, rotationDegrees, offsetSpeed);
    half4 sampled = (half4)NBGraphSampleMap(map, uv);
    uint channel = (packedChannels >> channelPosition) & 3u;
    return channel == 0u ? sampled.r :
        channel == 1u ? sampled.g :
        channel == 2u ? sampled.b : sampled.a;
}

// The two bits for each legacy wrap slot live 16 positions apart. Gradient
// mode uses the already transformed UV but does not sample the texture.
uint NBGraphMaskWrapMode(uint packedWrapFlags, uint bit)
{
    return ((packedWrapFlags & bit) != 0u ? 1u : 0u) |
        ((packedWrapFlags & (bit << 16)) != 0u ? 2u : 0u);
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

// The optional process/late mask uses the same two-stage numeric contract as
// ShaderLab. The host owns texture/channel selection; custom data, noise,
// wrap overrides and non-UV0 modes remain separate slices.
half NBGraphResolveDissolveCoverage(half4 sampledDissolve,
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
    return resolved.coverage;
}

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
    out float4 Out)
{
    NBFX_BaseColorInputV1 input = (NBFX_BaseColorInputV1)0;
    input.sampledAlbedo = (half4)SampledAlbedo;
    input.selectedAlpha = (half)SelectedAlpha;
    input.effectiveBaseColor = (half4)EffectiveBaseColor;
    input.timelineIntensity = 1.0h;
    input.applyTimelineIntensity = false;
    Out = (float4)NBFX_ComposeBaseColorV1(input);
    if (DissolveToggle > 0.5)
    {
        float2 dissolveUV = NBGraphFeatureUV(DissolveMap, DissolveUV,
            DissolveOffsetRotateDistort.z, DissolveOffsetRotateDistort.xy);
        half4 sampledDissolve = (half4)NBGraphSampleMap(DissolveMap, dissolveUV);
        bool hasDissolveMask = DissolveMaskToggle > 0.5;
        half4 sampledDissolveMask = (half4)0;
        if (hasDissolveMask)
        {
            float2 dissolveMaskUV = NBGraphFeatureUV(DissolveMaskMap, DissolveMaskUV,
                DissolveOffsetRotateDistort.z, float2(0.0, 0.0));
            sampledDissolveMask = (half4)NBGraphSampleMap(DissolveMaskMap, dissolveMaskUV);
        }
        Out.a *= NBGraphResolveDissolveCoverage(sampledDissolve,
            sampledDissolveMask, hasDissolveMask, (half4)Dissolve,
            (half)DissolveMaskMode, NB_ColorChannelLo16);
    }
    if (MaskToggle > 0.5)
    {
        uint maskFlags = NBGraphDecodeUInt32(NB_Flags1Lo16, NB_Flags1Hi16);
        uint wrapFlags = NBGraphDecodeUInt32(NB_WrapFlagsLo16, NB_WrapFlagsHi16);
        float maskRotation = MaskMapUVRotation;
        if ((NBGraphDecodeUInt32(NB_Flags0Lo16, NB_Flags0Hi16) &
            FLAG_BIT_PARTILCE_MASKMAPROTATIONANIMATION_ON) != 0u)
            maskRotation += _Time.y * MaskMapRotationSpeed;
        float2 maskUV = NBGraphFeatureUV(MaskMap, MaskUV,
            maskRotation, MaskMapOffsetAnition.xy);
        uint packedChannels = NBGraphDecodeUInt32(NB_ColorChannelLo16, 0.0);
        half channelValue;
        if ((maskFlags & FLAG_BIT_PARTICLE_1_MASKMAP_GRADIENT) != 0u)
            channelValue = NBGraphSampleMaskGradient(
                (half4)MaskMapGradientFloat0, (half4)MaskMapGradientFloat1,
                (half4)MaskMapGradientFloat2, MaskMapGradientCount, maskUV,
                NBGraphMaskWrapMode(wrapFlags, FLAG_BIT_WRAPMODE_MASKMAP), false);
        else
        {
            half4 sampledMask = (half4)NBGraphSampleMap(MaskMap, maskUV);
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
                    FLAG_BIT_COLOR_CHANNEL_POS_0_MASKMAP2);
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
                    FLAG_BIT_COLOR_CHANNEL_POS_0_MASKMAP3);
        }
        NBFX_MaskCoverageInputV3 maskInput = (NBFX_MaskCoverageInputV3)0;
        maskInput.combinedMaskAfterNoise = channelValue;
        maskInput.refine = (maskFlags & FLAG_BIT_PARTICLE_1_MASK_REFINE) != 0u;
        maskInput.refinePowMulAdd = (half3)MaskRefineVec.xyz;
        maskInput.overallStrength = (half)MaskMapVec.x;
        Out.a *= NBFX_ResolveMaskCoverageV3(maskInput);
    }
    if ((NBGraphDecodeUInt32(NB_Flags1Lo16, NB_Flags1Hi16) &
        FLAG_BIT_PARTICLE_1_IGNORE_VERTEX_COLOR) == 0u)
        Out *= VertexColor;
    Out.rgb *= ColorA.rgb;
    Out.a *= ColorA.a;
    // Original ColorAdjustment applies this flag after the base sample. In this
    // minimum Unlit Graph, no intervening lighting/effects alter that ordering.
    // It follows the Mask alpha multiplication in the ShaderLab path as well.
    if ((NBGraphDecodeUInt32(NB_Flags0Lo16, NB_Flags0Hi16) & FLAG_BIT_PARTICLE_COLOR_MULTI_ALPHA) != 0u)
        Out.rgb *= Out.a;
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
    out half4 Out)
{
    NBFX_BaseColorInputV1 input = (NBFX_BaseColorInputV1)0;
    input.sampledAlbedo = SampledAlbedo;
    input.selectedAlpha = SelectedAlpha;
    input.effectiveBaseColor = EffectiveBaseColor;
    input.timelineIntensity = 1.0h;
    input.applyTimelineIntensity = false;
    Out = NBFX_ComposeBaseColorV1(input);
    if (DissolveToggle > 0.5)
    {
        float2 dissolveUV = NBGraphFeatureUV(DissolveMap, DissolveUV,
            DissolveOffsetRotateDistort.z, DissolveOffsetRotateDistort.xy);
        half4 sampledDissolve = (half4)NBGraphSampleMap(DissolveMap, dissolveUV);
        bool hasDissolveMask = DissolveMaskToggle > 0.5;
        half4 sampledDissolveMask = (half4)0;
        if (hasDissolveMask)
        {
            float2 dissolveMaskUV = NBGraphFeatureUV(DissolveMaskMap, DissolveMaskUV,
                DissolveOffsetRotateDistort.z, float2(0.0, 0.0));
            sampledDissolveMask = (half4)NBGraphSampleMap(DissolveMaskMap, dissolveMaskUV);
        }
        Out.a *= NBGraphResolveDissolveCoverage(sampledDissolve,
            sampledDissolveMask, hasDissolveMask, Dissolve,
            (half)DissolveMaskMode, NB_ColorChannelLo16);
    }
    if (MaskToggle > 0.5)
    {
        uint maskFlags = NBGraphDecodeUInt32(NB_Flags1Lo16, NB_Flags1Hi16);
        uint wrapFlags = NBGraphDecodeUInt32(NB_WrapFlagsLo16, NB_WrapFlagsHi16);
        float maskRotation = MaskMapUVRotation;
        if ((NBGraphDecodeUInt32(NB_Flags0Lo16, NB_Flags0Hi16) &
            FLAG_BIT_PARTILCE_MASKMAPROTATIONANIMATION_ON) != 0u)
            maskRotation += _Time.y * MaskMapRotationSpeed;
        float2 maskUV = NBGraphFeatureUV(MaskMap, MaskUV,
            maskRotation, MaskMapOffsetAnition.xy);
        uint packedChannels = NBGraphDecodeUInt32(NB_ColorChannelLo16, 0.0);
        half channelValue;
        if ((maskFlags & FLAG_BIT_PARTICLE_1_MASKMAP_GRADIENT) != 0u)
            channelValue = NBGraphSampleMaskGradient(
                MaskMapGradientFloat0, MaskMapGradientFloat1,
                MaskMapGradientFloat2, MaskMapGradientCount, maskUV,
                NBGraphMaskWrapMode(wrapFlags, FLAG_BIT_WRAPMODE_MASKMAP), false);
        else
        {
            half4 sampledMask = (half4)NBGraphSampleMap(MaskMap, maskUV);
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
                    FLAG_BIT_COLOR_CHANNEL_POS_0_MASKMAP2);
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
                    FLAG_BIT_COLOR_CHANNEL_POS_0_MASKMAP3);
        }
        NBFX_MaskCoverageInputV3 maskInput = (NBFX_MaskCoverageInputV3)0;
        maskInput.combinedMaskAfterNoise = channelValue;
        maskInput.refine = (maskFlags & FLAG_BIT_PARTICLE_1_MASK_REFINE) != 0u;
        maskInput.refinePowMulAdd = MaskRefineVec.xyz;
        maskInput.overallStrength = MaskMapVec.x;
        Out.a *= NBFX_ResolveMaskCoverageV3(maskInput);
    }
    if ((NBGraphDecodeUInt32(NB_Flags1Lo16, NB_Flags1Hi16) &
        FLAG_BIT_PARTICLE_1_IGNORE_VERTEX_COLOR) == 0u)
        Out *= VertexColor;
    Out.rgb *= ColorA.rgb;
    Out.a *= ColorA.a;
    if ((NBGraphDecodeUInt32(NB_Flags0Lo16, NB_Flags0Hi16) & FLAG_BIT_PARTICLE_COLOR_MULTI_ALPHA) != 0u)
        Out.rgb *= Out.a;
    Out.a = saturate(Out.a * (half)AlphaAll);
}

#endif
