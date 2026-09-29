#ifndef NB_GRAPH_BASE_COLOR_INCLUDED
#define NB_GRAPH_BASE_COLOR_INCLUDED

#include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderSurfaceV1.hlsl"
#include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderMaskV3.hlsl"
#include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderDissolveV3.hlsl"
#include "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBGraphFlags.hlsl"

// Match Shader Graph's Sample Texture 2D node, including the texture's _ST and
// optional HDR decode. The caller invokes this only inside an enabled feature.
float4 NBGraphSampleMap(UnityTexture2D map, float2 uv)
{
    float4 sampled = SAMPLE_TEXTURE2D(map.tex, map.samplerstate, map.GetTransformedUV(uv));
    if (map.hdrDecode.x > 0.0)
        sampled = DecodeHDRSample(sampled, map.hdrDecode);
    return sampled;
}

// Basic single-map dissolve keeps the original two-stage numeric contract.
// Mask, procedural noise, ramp, custom data and animated UVs are later slices.
half NBGraphResolveDissolveCoverage(half4 sampledDissolve, half4 dissolve,
    float colorChannelLo16)
{
    uint channel = (NBGraphDecodeUInt32(colorChannelLo16, 0.0) >>
        FLAG_BIT_COLOR_CHANNEL_POS_0_DISSOLVE_MAP) & 3u;
    half value = channel == 0u ? sampledDissolve.r :
        channel == 1u ? sampledDissolve.g :
        channel == 2u ? sampledDissolve.b : sampledDissolve.a;
    NBFX_DissolvePrepareInputV3 prepareInput = (NBFX_DissolvePrepareInputV3)0;
    prepareInput.decodedAndNoiseBlendedValue = value;
    prepareInput.exponent = dissolve.y;
    NBFX_DissolvePreparedV3 prepared = NBFX_PrepareDissolveV3(prepareInput);
    NBFX_DissolveResolveInputV3 resolveInput = (NBFX_DissolveResolveInputV3)0;
    resolveInput.prepared = prepared;
    resolveInput.threshold = dissolve.x;
    resolveInput.softWidth = dissolve.w;
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
        half4 sampledDissolve = (half4)NBGraphSampleMap(DissolveMap, DissolveUV);
        Out.a *= NBGraphResolveDissolveCoverage(sampledDissolve,
            (half4)Dissolve, NB_ColorChannelLo16);
    }
    if (MaskToggle > 0.5)
    {
        half4 sampledMask = (half4)NBGraphSampleMap(MaskMap, MaskUV);
        uint maskChannel = (NBGraphDecodeUInt32(NB_ColorChannelLo16, 0.0) >> FLAG_BIT_COLOR_CHANNEL_POS_0_MASKMAP1) & 3u;
        half channelValue = maskChannel == 0u ? sampledMask.r :
            maskChannel == 1u ? sampledMask.g :
            maskChannel == 2u ? sampledMask.b : sampledMask.a;
        NBFX_MaskCoverageInputV3 maskInput = (NBFX_MaskCoverageInputV3)0;
        maskInput.combinedMaskAfterNoise = channelValue;
        maskInput.refine = (NBGraphDecodeUInt32(NB_Flags1Lo16, NB_Flags1Hi16) & FLAG_BIT_PARTICLE_1_MASK_REFINE) != 0u;
        maskInput.refinePowMulAdd = (half3)MaskRefineVec.xyz;
        maskInput.overallStrength = (half)MaskMapVec.x;
        Out.a *= NBFX_ResolveMaskCoverageV3(maskInput);
    }
    // Original ColorAdjustment applies this flag after the base sample. In this
    // minimum Unlit Graph, no intervening lighting/effects alter that ordering.
    // It follows the Mask alpha multiplication in the ShaderLab path as well.
    if ((NBGraphDecodeUInt32(NB_Flags0Lo16, NB_Flags0Hi16) & FLAG_BIT_PARTICLE_COLOR_MULTI_ALPHA) != 0u)
        Out.rgb *= Out.a;
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
        half4 sampledDissolve = (half4)NBGraphSampleMap(DissolveMap, DissolveUV);
        Out.a *= NBGraphResolveDissolveCoverage(sampledDissolve, Dissolve,
            NB_ColorChannelLo16);
    }
    if (MaskToggle > 0.5)
    {
        half4 sampledMask = (half4)NBGraphSampleMap(MaskMap, MaskUV);
        uint maskChannel = (NBGraphDecodeUInt32(NB_ColorChannelLo16, 0.0) >> FLAG_BIT_COLOR_CHANNEL_POS_0_MASKMAP1) & 3u;
        half channelValue = maskChannel == 0u ? sampledMask.r :
            maskChannel == 1u ? sampledMask.g :
            maskChannel == 2u ? sampledMask.b : sampledMask.a;
        NBFX_MaskCoverageInputV3 maskInput = (NBFX_MaskCoverageInputV3)0;
        maskInput.combinedMaskAfterNoise = channelValue;
        maskInput.refine = (NBGraphDecodeUInt32(NB_Flags1Lo16, NB_Flags1Hi16) & FLAG_BIT_PARTICLE_1_MASK_REFINE) != 0u;
        maskInput.refinePowMulAdd = MaskRefineVec.xyz;
        maskInput.overallStrength = MaskMapVec.x;
        Out.a *= NBFX_ResolveMaskCoverageV3(maskInput);
    }
    if ((NBGraphDecodeUInt32(NB_Flags0Lo16, NB_Flags0Hi16) & FLAG_BIT_PARTICLE_COLOR_MULTI_ALPHA) != 0u)
        Out.rgb *= Out.a;
}

#endif
