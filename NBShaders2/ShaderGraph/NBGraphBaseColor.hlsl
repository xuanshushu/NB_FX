#ifndef NB_GRAPH_BASE_COLOR_INCLUDED
#define NB_GRAPH_BASE_COLOR_INCLUDED
#include "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBGraphCustomLocalSpace.hlsl"

#include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderSurfaceV1.hlsl"
#include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderEnvironmentV2.hlsl"
#include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderDistortionV1.hlsl"
#include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderMaskV3.hlsl"
#include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderDissolveV3.hlsl"
#include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderPackedGradientV1.hlsl"
#include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderUVV2.hlsl"
#include "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBGraphFlags.hlsl"
#include "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBGraphSampling.hlsl"
#include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderChromaticV1.hlsl"
#include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderParallaxV1.hlsl"
#define NB_GRAPH_SIX_WAY 1
#include "Packages/com.xuanxuan.nb.fx/XuanXuanRenderUtility/Shader/HLSL/SixWaySmokeLit.hlsl"
#undef NB_GRAPH_SIX_WAY
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// DepthOnly and ShadowCaster use the same NB surface up to the old
// NB_DEPTH_SHADOW_PASS boundary. Do not evaluate Forward-only alpha stages.
#if defined(SHADERPASS) && ((SHADERPASS == SHADERPASS_DEPTHONLY) || (SHADERPASS == SHADERPASS_SHADOWCASTER))
    #define NB_GRAPH_DEPTH_SHADOW_PASS 1
#else
    #define NB_GRAPH_DEPTH_SHADOW_PASS 0
#endif

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
    float2 offsetSpeed, float2 customAfterST = float2(0,0))
{
    // Keep the previous Graph sampling expression for the zero-control case;
    // in particular non-identity texture ST retains its original arithmetic.
    if (rotationDegrees == 0.0 && offsetSpeed.x == 0.0 && offsetSpeed.y == 0.0 && all(customAfterST == 0.0))
        return map.GetTransformedUV(uv);

    NBFX_FeatureUVTransformInputV2 input = (NBFX_FeatureUVTransformInputV2)0;
    input.originUV = uv;
    input.scaleOffset = map.scaleTranslate;
    input.offsetSpeed = offsetSpeed;
    input.rotationDegrees = rotationDegrees;
    input.rotationCenter = float2(0.5, 0.5);
    input.customOffsetAfterST=customAfterST;
    input.timeY = _Time.y;
    return NBFX_TransformFeatureUVV2(input);
}

// Shared N1 surface/N2 screen texture noise. Preserve ParticleUVCommonProcess's rotation
// -> ST and SampleNoise's later scrolling, with ShaderLab's half material
// boundaries. NoiseMask has its own source/ST and no rotation or scrolling.
// The caller weights only texture-consumer offsets by noiseMask; the screen
// passes receive signedRG and noiseMask separately. PN2 blends PNoise
// in the caller after texture scaling. Refraction replaces only the texture
// Noise source; CustomData and chromatic aberration remain pending.
void NBGraphTextureNoise(UnityTexture2D noiseMap, float2 noiseSourceUV,
    float noiseRotation, half4 noiseOffset, half intensity, half4 direction,
    UnityTexture2D maskMap, float2 maskSourceUV, bool hasMask,
    uint flags0, uint channels, uint wrapFlags, uint noMipFlags,
    bool useRefraction, half refractionIOR, float3 viewDirWS, float3 facedNormalWS,
    out half2 signedRG, out half noiseMask)
{
    NBFX_FeatureUVTransformInputV2 uvInput = (NBFX_FeatureUVTransformInputV2)0;
    signedRG = 0;
    noiseMask = 1;
    if (useRefraction)
    {
        half3 refracVec = NBFX_RefractDirectionV1(-viewDirWS, facedNormalWS,
            1 / refractionIOR);
        refracVec = TransformWorldToHClipDir(refracVec);
        signedRG = refracVec.xy;
    }
    else
    {
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
        NBFX_DecodeTextureNoiseV1(noiseSample,
            (flags0 & FLAG_BIT_PARTICLE_NOISEMAP_NORMALIZEED_ON) != 0u,
            signedRG, noiseMask);
    }
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
// ShaderLab. The host owns texture/channel selection; CustomData and
// unsupported special-position UV modes remain separate slices.
NBFX_DissolveResolvedV3 NBGraphResolveDissolve(half4 sampledDissolve,
    half4 sampledDissolveMask, bool hasDissolveMask, half4 dissolve,
    half dissolveMaskMode, float colorChannelLo16, bool hasPNoise,
    half programNoise, uint pNoiseBlendFlags, half pNoiseOpacity,
    out half debugPreparedValue)
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
    if (hasPNoise)
        value = BlendPNoise(pNoiseBlendFlags, FLAG_BIT_PNOISE_BLEND_POS_0_DISSOLVE,
            value, programNoise, pNoiseOpacity);
    NBFX_DissolvePrepareInputV3 prepareInput = (NBFX_DissolvePrepareInputV3)0;
    prepareInput.decodedAndNoiseBlendedValue = value;
    prepareInput.exponent = dissolve.y;
    prepareInput.hasMask = hasDissolveMask;
    prepareInput.decodedMaskValue = maskValue;
    prepareInput.maskStrength = dissolve.z;
    prepareInput.maskMode = dissolveMaskMode;
    NBFX_DissolvePreparedV3 prepared = NBFX_PrepareDissolveV3(prepareInput);
    debugPreparedValue = prepared.valueForDebugAndSoftStep;
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
#if defined(NB_DEBUG_FRESNEL) && defined(NB_GRAPH_MAIN_FORWARD)
    return half4(fresnelValue.xxx * unit.z, 1.0h);
#endif
    NBFX_ApplyFresnelV1(color.rgb, color.a, fresnelValue,
        unit.z, fresnelColor,
        (flags0 & FLAG_BIT_PARTICLE_FRESNEL_FADE_ON) != 0u,
        (flags0 & FLAG_BIT_PARTICLE_FRESNEL_COLOR_AFFETCT_BY_ALPHA) != 0u);
    return color;
}

// Genuine vertex-stage six-direction SH, not pixel SH or a zero-GI stand-in.
// The same EVALUATE_SH_VERTEX keyword used by the old material is selected
// by NBShaderGraphGUI when mode 4 is active. Geometry N/T/B is post-VertexOffset.
void NBGraphSixWayBake_float(float3 NormalWS, float3 TangentWS,
    float3 BitangentWS, float CustomLocalToggle,float CustomSign,
    out float3 Bake0, out float3 Bake1,
    out float3 Bake2, out float3 Back0, out float3 Back1,
    out float3 Back2, out float4 TangentSigned)
{
    // SG 17.3 constructs object-space bitangent with GetOddNegativeScale,
    // then transforms it as a direction. For a reflected object, the world
    // cross-product adds another determinant sign. Recover tangentOS.w,
    // then explicitly match NBShader's tangentOS.w*odd-negative-scale and
    // its half-precision world tangent before SH evaluation.
    half sign = (dot(cross(NormalWS,TangentWS),BitangentWS)<0 ? -1.0h : 1.0h)
        * GetOddNegativeScale();
    if(CustomLocalToggle>0.5)sign=(half)CustomSign;
    half3 tangentWS = (half3)TangentWS;
    float3 bitangentWS = sign * cross(NormalWS,tangentWS);
    half3 b0,b1,b2,r0,r1,r2;
    GetSixWayBakeDiffuseLight(NormalWS,tangentWS,bitangentWS,
        b0,b1,b2,r0,r1,r2);
    Bake0=b0; Bake1=b1; Bake2=b2;
    Back0=r0; Back1=r1; Back2=r2;
    TangentSigned=float4(tangentWS,sign);
}
void NBGraphSixWayBake_half(half3 NormalWS, half3 TangentWS,
    half3 BitangentWS, float CustomLocalToggle,float CustomSign,
    out half3 Bake0, out half3 Bake1,
    out half3 Bake2, out half3 Back0, out half3 Back1,
    out half3 Back2, out half4 TangentSigned)
{
    float3 b0,b1,b2,r0,r1,r2;float4 ts;
    NBGraphSixWayBake_float(NormalWS,TangentWS,BitangentWS,CustomLocalToggle,CustomSign,
        b0,b1,b2,r0,r1,r2,ts);
    Bake0=(half3)b0; Bake1=(half3)b1; Bake2=(half3)b2;
    Back0=(half3)r0; Back1=(half3)r1; Back2=(half3)r2;
    TangentSigned=(half4)ts;
}

// Original NBShader SixWay stage: rig sampling -> original six-direction
// baked-GI/direct-light/emission helper -> original alpha override. The
// caller retains pre-early-adjust albedo, then continues MatCap/overlays.
half4 NBGraphApplySixWay(half4 preAdjustAlbedo, UnityTexture2D rigPositive,
    UnityTexture2D rigNegative, UnityTexture2D emissionRamp,
    float2 mainUV, uint wrapFlags, uint noMipFlags, uint flags1,
    float4 sixWayInfo, half4 sixWayEmissionColor, half baseColorAlpha,
    float3 positionWS, float3 unfacedNormalWS, float3 viewDirWS,
    float2 normalizedScreenUV, half isFrontFace, half3 bake0,
    half3 bake1, half3 bake2, half3 back0, half3 back1,
    half3 back2, half4 tangentSigned)
{
    // The maps have no independent ST in the old inspector; MainTex UV owns
    // the coordinate. Flipbook blend awaits the common UV-host slice.
    half4 positive = NBGraphSampleMap(rigPositive,mainUV,
        NBGraphMaskWrapMode(wrapFlags,FLAG_BIT_WRAPMODE_BASEMAP),
        (noMipFlags&FLAG_BIT_FORCE_NO_MIP_RIG_RTBK)!=0u);
    half4 negative = NBGraphSampleMap(rigNegative,mainUV,
        NBGraphMaskWrapMode(wrapFlags,FLAG_BIT_WRAPMODE_BASEMAP),
        (noMipFlags&FLAG_BIT_FORCE_NO_MIP_RIG_LBTF)!=0u);
    InputData inputData=(InputData)0;
    inputData.positionWS=positionWS;
    inputData.normalWS=isFrontFace>0.5h?unfacedNormalWS:-unfacedNormalWS;
    inputData.viewDirectionWS=(half3)viewDirWS;
    inputData.normalizedScreenSpaceUV=normalizedScreenUV;
    inputData.shadowMask=SAMPLE_SHADOWMASK(float2(0,0));
    BSDFData bsdfData=(BSDFData)0;
    bsdfData.absorptionRange=GetAbsorptionRange(sixWayInfo.x);
    bsdfData.diffuseColor=preAdjustAlbedo;
    bsdfData.normalWS=inputData.normalWS;
    bsdfData.tangentWS=tangentSigned;
    bsdfData.rigRTBk=positive.xyz*INV_PI;
    bsdfData.rigLBtF=negative.xyz*INV_PI;
    bsdfData.bakeDiffuseLighting0=bake0;
    bsdfData.bakeDiffuseLighting1=bake1;
    bsdfData.bakeDiffuseLighting2=bake2;
    bsdfData.backBakeDiffuseLighting0=back0;
    bsdfData.backBakeDiffuseLighting1=back1;
    bsdfData.backBakeDiffuseLighting2=back2;
    bsdfData.emissionInput=negative.a;
    GetSixWayEmissionExplicitPower(bsdfData,sixWayInfo.y,emissionRamp.tex,sixWayEmissionColor,
        (flags1&FLAG_BIT_PARTICLE_1_SIXWAY_RAMPMAP)!=0u,
        (noMipFlags&FLAG_BIT_FORCE_NO_MIP_SIX_WAY_EMISSION_RAMP)!=0u);
    bsdfData.alpha=positive.a*baseColorAlpha;
    ModifyBakedDiffuseLighting(bsdfData,inputData.bakedGI);
    return UniversalFragmentSixWay(inputData,bsdfData);
}

// Ordinary Mesh lighting consumes the existing post-vertex geometry SH
// interpolator. SampleSHPixel preserves original pixel/mixed/vertex keywords;
// this does not add lightmap/APV/vertex-additional-light support.
half4 NBGraphApplyLighting(half4 color, float mode, float3 positionWS,
    float3 unfacedNormalWS, float3 viewDirWS, float2 normalizedScreenUV,
    half isFrontFace, half3 normalTS, half metallicWeight,
    half smoothnessWeight, half4 materialInfo, half4 specularColor, half3 vertexSH)
{
    if (mode < 0.5 || mode >= 3.5) return color;
    InputData inputData = (InputData)0;
    inputData.positionWS = positionWS;
    inputData.normalWS = isFrontFace > 0.5h ? unfacedNormalWS : -unfacedNormalWS;
    // SG ViewDirection(World) is already GetWorldSpaceNormalizeViewDir.
    // Match the old float -> half InputData boundary, without normalizing twice.
    inputData.viewDirectionWS = (half3)viewDirWS;
    #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
        inputData.shadowCoord = TransformWorldToShadowCoord(positionWS);
    #endif
    inputData.normalizedScreenSpaceUV = normalizedScreenUV;
    inputData.bakedGI = SampleSHPixel(vertexSH, inputData.normalWS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(float2(0, 0));
    half metallic = metallicWeight * materialInfo.x;
    half smoothness = smoothnessWeight * materialInfo.y;
    half4 lit;
    if (mode < 1.5)
        lit = UniversalFragmentBlinnPhong(inputData, color.rgb,
            specularColor, smoothness, 0, color.a, normalTS);
    else if (mode < 2.5)
        lit = UniversalFragmentHalfLambert(inputData, color.rgb,
            specularColor, smoothness, 0, color.a, normalTS);
    else
        lit = UniversalFragmentPBR(inputData, color.rgb, metallic,
            0, smoothness, 1, 0, color.a);
    return lit;
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

// N0: Graph receives the same geometric N/T/B basis as URP 17.3's
// SurfaceDescriptionInputs. SG's bitangent includes tangent.w * odd-scale;
// VFACE reverses N and B but not T, matching the ShaderLab fragment stage.
// Return an unfaced vector because the existing Fresnel/MatCap adapters each
// apply VFACE once. There is no normal-map sampling when the toggle is off.
half3 NBGraphNormalForFeatures(UnityTexture2D map, float2 sourceUV,
    half scale, float3 normalWS, float3 tangentWS, float3 bitangentWS,
    float isFrontFace, uint flags0, uint wrapFlags, uint noMipFlags,
    out half3 normalTS, out half metallicWeight, out half smoothnessWeight)
{
    half side = isFrontFace > 0.5 ? 1.0h : -1.0h;
    float2 uv = map.GetTransformedUV(sourceUV);
    half4 sampled = NBGraphSampleRawMap(map, uv,
        NBGraphMaskWrapMode(wrapFlags, FLAG_BIT_WRAPMODE_BUMPTEX),
        (noMipFlags & FLAG_BIT_FORCE_NO_MIP_BUMPTEX) != 0u);
    half3x3 tangentToWorld = half3x3((half3)tangentWS,
        (half3)(side * bitangentWS), (half3)(side * normalWS));
    half3 facedNormalWS;
    NBFX_DecodeNormalMapV2(sampled, scale,
        (flags0 & FLAG_BIT_PARTICLE_NORMALMAP_MASK_MODE) != 0u,
        tangentToWorld, normalTS, facedNormalWS, metallicWeight, smoothnessWeight);
    return facedNormalWS * side;
}

// PN1: the same existing procedural kernels and packed BlendPNoise as ShaderLab.
// UV0/1/2/Shared modes are supplied by NBGraphBaseUV; procedural CustomData
// offsets, procedural wrap variants and PN distortion are separate later
// slices, not silent fallbacks.
half NBGraphProgramNoise(float2 sourceUV, half rotation, float4 vec,
    half4 vec2, float4 vec3, float4 vec4, bool simpleOn, bool voronoiOn,
    uint blendFlags, half baseOpacity)
{
    float2 uv = Rotate_Radians_float(sourceUV, half2(0.5h, 0.5h), rotation);
    half simpleValue = 0.0h;
    half voronoiValue = 0.0h;
    if (simpleOn)
    {
        float2 simpleUV = uv * vec.xy + vec4.xy + _Time.y * vec3.xy;
        simpleValue = SimplexNoise(simpleUV, _Time.y * vec2.z);
    }
    if (voronoiOn)
    {
        float2 voronoiUV = uv * vec.zw + vec4.zw + _Time.y * vec3.zw;
        float voronoiFloat, cell;
        Unity_Voronoi_float(voronoiUV, _Time.y * vec2.w, 1.0, voronoiFloat, cell);
        voronoiValue = (half)voronoiFloat;
    }
    if (simpleOn && voronoiOn)
        return BlendPNoise(blendFlags, FLAG_BIT_PNOISE_BLEND_POS_0_BASE_BLEND,
            simpleValue, voronoiValue, baseOpacity);
    return simpleOn ? simpleValue : voronoiValue;
}

// POM uses the pre-normalmap fragment basis. Current URP SharedCode already
// forms fragment BitangentWS with tangentOS.w * GetOddNegativeScale().
// This differs from the vertex-stage SixWay bake adapter: do not apply odd twice.
float2 NBGraphApplyParallax(UnityTexture2D map, float2 baseUV,
    float intensity, float4 layerVec, float3 normalWS,
    float3 tangentWS, float3 bitangentWS, float3 viewDirWS,
    float isFrontFace, uint wrapFlags, uint noMipFlags)
{
    half3 rawN = (half3)normalWS;
    half3 tangent = (half3)tangentWS;
    half tangentSign = dot((half3)bitangentWS,
        cross(rawN, tangent)) < 0.0h ? -1.0h : 1.0h;
    half3 facedN = isFrontFace > 0.5 ? rawN : -rawN;
    half3 bitangent = tangentSign * cross(facedN, tangent);
    half3x3 tangentToWorld = half3x3(tangent, bitangent, facedN);
    float3 tangentViewDir = (float3)SafeNormalize(mul(tangentToWorld, (half3)viewDirWS));
    return NBFX_ParallaxOcclusionMappingV1(map.tex, baseUV,
        tangentViewDir, (half4)map.scaleTranslate, (half)intensity,
        (half4)layerVec,
        NBGraphMaskWrapMode(wrapFlags, FLAG_BIT_WRAPMODE_PARALLAXMAPPINGMAP),
        (noMipFlags & FLAG_BIT_FORCE_NO_MIP_PARALLAXMAPPINGMAP) != 0u);
}

// The original CameraOpaque pass forces BASEMAP wrap=Clamp even when
// the material's saved wrap says Repeat. This is pass identity, not mode.
uint NBGraphBaseMapWrapMode(uint wrapFlags)
{
#if defined(NB_GRAPH_CAMERA_OPAQUE_PASS)
    return 1u;
#else
    return NBGraphMaskWrapMode(wrapFlags, FLAG_BIT_WRAPMODE_BASEMAP);
#endif
}

// Match the legacy raw three-sample path, not Graph's HDR-decoded
// single BaseMap sample. The UV parameters are half2 before sampling.
half4 NBGraphChromaticSample(UnityTexture2D map,
    half2 originUV, half2 finalUV, half intensity, bool withNoise,
    uint wrapFlags, uint noMipFlags)
{
    half2 delta = NBFX_ChromaticDeltaV1(originUV, finalUV,
        intensity, withNoise);
    uint wrap = NBGraphBaseMapWrapMode(wrapFlags);
    bool lod0 = (noMipFlags & FLAG_BIT_FORCE_NO_MIP_BASEMAP) != 0u;
    half2 ra = NBGraphSampleRawMap(map, finalUV, wrap, lod0).xw;
    half2 ga = NBGraphSampleRawMap(map, finalUV - delta, wrap, lod0).yw;
    half2 ba = NBGraphSampleRawMap(map, finalUV - delta * 2, wrap, lod0).zw;
    return NBFX_ComposeChromaticV1(ra, ga, ba);
}

// Frozen NBShaderForwardPass computes this before _VERTEX_OFFSET, after
// ApplyVAT. SG PositionOS feeding NBGraphVertexOffset is the pre-offset source.
void NBGraphFogVertex_float(float3 PositionOS, float CustomLocalToggle,
    float4 LocalToWorld0,
    float4 LocalToWorld1,
    float4 LocalToWorld2,
    float4 LocalToWorld3,
    float4 WorldToLocal0,
    float4 WorldToLocal1,
    float4 WorldToLocal2,
    float4 WorldToLocal3,
    out float FogFactor)
{
    FogFactor = ComputeFogFactor(NBGraphLocalToHClipV1(PositionOS,CustomLocalToggle,LocalToWorld0,LocalToWorld1,LocalToWorld2,LocalToWorld3).z);
}
void NBGraphFogVertex_half(half3 PositionOS, float CustomLocalToggle,
    float4 LocalToWorld0,
    float4 LocalToWorld1,
    float4 LocalToWorld2,
    float4 LocalToWorld3,
    float4 WorldToLocal0,
    float4 WorldToLocal1,
    float4 WorldToLocal2,
    float4 WorldToLocal3,
    out half FogFactor)
{
    float computed;
    NBGraphFogVertex_float((float3)PositionOS,CustomLocalToggle, LocalToWorld0, LocalToWorld1, LocalToWorld2, LocalToWorld3, WorldToLocal0, WorldToLocal1, WorldToLocal2, WorldToLocal3, computed);
    FogFactor = (half)computed;
}

// NBShader's half3 fog path. The identity branch keeps all existing Graph
// fog-off materials unchanged, including non-8-bit intermediate values.
void NBGraphApplyFogV1(inout float3 rgb, float interpolatedFogFactor,
    float fogIntensity)
{
    bool active = false;
    #if defined(FOG_LINEAR_KEYWORD_DECLARED)
        if (FOG_LINEAR) active = true;
    #endif
    #if defined(FOG_EXP_KEYWORD_DECLARED)
        if (FOG_EXP) active = true;
    #endif
    #if defined(FOG_EXP2_KEYWORD_DECLARED)
        if (FOG_EXP2) active = true;
    #endif
    if (active && IsFogEnabled())
    {
        half3 beforeFog = (half3)rgb;
        half3 mixed = MixFog(beforeFog, (half)interpolatedFogFactor);
        rgb = (float3)lerp(beforeFog, mixed, (half)fogIntensity);
    }
}
void NBGraphApplyFogV1(inout half3 rgb, float interpolatedFogFactor,
    float fogIntensity)
{
    bool active = false;
    #if defined(FOG_LINEAR_KEYWORD_DECLARED)
        if (FOG_LINEAR) active = true;
    #endif
    #if defined(FOG_EXP_KEYWORD_DECLARED)
        if (FOG_EXP) active = true;
    #endif
    #if defined(FOG_EXP2_KEYWORD_DECLARED)
        if (FOG_EXP2) active = true;
    #endif
    if (active && IsFogEnabled())
    {
        half3 beforeFog = rgb;
        rgb = lerp(beforeFog, MixFog(rgb, (half)interpolatedFogFactor),
            (half)fogIntensity);
    }
}

// The two samples share the exact BaseMap wrap, HDR decode and force-LOD0
// path. Alpha selection/masks/lighting consume the already-blended half4.
half4 NBGraphSampleFlipbookBaseV1(UnityTexture2D map,
    float2 primaryUV, float2 blendUV, float blendWeight,
    float flipbookToggle, uint wrapFlags, uint noMipFlags)
{
    uint wrap = NBGraphBaseMapWrapMode(wrapFlags);
    bool lod0 = (noMipFlags & FLAG_BIT_FORCE_NO_MIP_BASEMAP) != 0u;
    half4 first = NBGraphSampleMap(map, primaryUV, wrap, lod0);
    if (flipbookToggle <= 0.5) return first;
    half4 second = NBGraphSampleMap(map, blendUV, wrap, lod0);
    return lerp(first, second, blendWeight);
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
    float BumpMapToggle, UnityTexture2D BumpTex, float BumpScale,
    float2 BumpUV, float3 TangentWS, float3 BitangentWS,
    float FxLightMode, float4 MaterialInfo, float4 SpecularColor,
    float3 PositionWS,
    float2 ProgramNoiseUV, float ProgramNoiseToggle, float ProgramSimpleToggle,
    float ProgramVoronoiToggle, float ProgramNoiseRotate,
    float4 PNoiseVec, float4 PNoiseVec2, float4 PNoiseVec3, float4 PNoiseVec4,
    float PNoiseBaseBlendOpacity, float PNoiseMaskBlendOpacity,
    float PNoiseDissolveBlendOpacity, float PNoiseBlendLo16, float PNoiseBlendHi16,
    UnityTexture2D RigRTBk, UnityTexture2D RigLBtF,
    UnityTexture2D SixWayEmissionRamp, float4 SixWayInfo,
    float4 SixWayEmissionColor, float SixWayColorAbsorptionToggle,
    float3 SixBake0, float3 SixBake1, float3 SixBake2,
    float3 SixBack0, float3 SixBack1, float3 SixBack2,
    float4 SixTangentSigned, float PNoiseDistortBlendOpacity,
    float DistortMode, float RefractionIOR,
    float DecalAlpha,
    UnityTexture2D ParallaxMappingMap, float ParallaxMappingToggle,
    float ParallaxMappingIntensity, float4 ParallaxMappingVec,
    float ChromaticToggle, float CustomDataFlag0Lo16,
    float CustomDataFlag0Hi16, float4 Custom1, float4 Custom2,
    float FogFactor, float FogIntensity,
    float FlipbookToggle, float2 BlendUV, float BlendWeight,
    float CustomDataFlag1Lo16,
    float CustomDataFlag1Hi16,
    float CustomDataFlag2Lo16,
    float CustomDataFlag2Hi16,
    float CustomDataFlag3Lo16,
    float CustomDataFlag3Hi16,
    float CustomLocalToggle,
    float4 LocalToWorld0,
    float4 LocalToWorld1,
    float4 LocalToWorld2,
    float4 LocalToWorld3,
    float4 WorldToLocal0,
    float4 WorldToLocal1,
    float4 WorldToLocal2,
    float4 WorldToLocal3,
    float NBGraphTierAllowMask, float NBGraphTierAllowMask2, float NBGraphTierAllowMask3,
    float NBGraphTierAllowNoise, float NBGraphTierAllowNoiseMask,
    float NBGraphDebugVertexOffsetToggle, float3 NBGraphDebugVertexOffsetRGB,
    float NBGraphTierAllowProgramNoise, float NBGraphTierAllowProgramSimple, float NBGraphTierAllowProgramVoronoi,
    float NBGraphTierAllowFresnel,
    float NBGraphTierAllowEmission, float NBGraphTierAllowColorBlend,
    float NBGraphTierAllowDissolve, float NBGraphTierAllowDissolveMask,
    float NBGraphTierAllowDissolveRamp, float NBGraphTierAllowDissolveRampMap,
    float NBGraphTierAllowParallax,
    float NBGraphTierAllowNormalMap,
    float NBGraphTierAllowColorRamp, float NBGraphTierAllowColorRampMap,
    float NBGraphTierAllowMatCap,
    out float4 Out, out float2 NBDistortionSignedRG,
    out float NBDistortionNoiseMask)
{
    MatCapToggle *= NBGraphTierAllowMatCap > 0.5 ? 1.0 : 0.0;

    RampColorToggle *= NBGraphTierAllowColorRamp > 0.5 ? 1.0 : 0.0;
    // Tier changes effective sampling only; the serialized source enum stays intent.
    RampColorSourceMode = RampColorSourceMode > 0.5 && NBGraphTierAllowColorRampMap > 0.5 ? 1.0 : 0.0;

    BumpMapToggle *= NBGraphTierAllowNormalMap > 0.5 ? 1.0 : 0.0;

    ParallaxMappingToggle *= NBGraphTierAllowParallax > 0.5 ? 1.0 : 0.0;

    DissolveToggle *= NBGraphTierAllowDissolve > 0.5 ? 1.0 : 0.0;
    DissolveMaskToggle *= NBGraphTierAllowDissolveMask > 0.5 ? 1.0 : 0.0;
    DissolveRampToggle *= NBGraphTierAllowDissolveRamp > 0.5 ? 1.0 : 0.0;
    // Raw source enum remains intent; Native stripped MAP samples gradient.
    DissolveRampSourceMode = DissolveRampSourceMode > 0.5 && NBGraphTierAllowDissolveRampMap > 0.5 ? 1.0 : 0.0;

    EmissionEnabled *= NBGraphTierAllowEmission > 0.5 ? 1.0 : 0.0;
    ColorBlendMapToggle *= NBGraphTierAllowColorBlend > 0.5 ? 1.0 : 0.0;

    FresnelEnabled *= NBGraphTierAllowFresnel > 0.5 ? 1.0 : 0.0;

    ProgramNoiseToggle *= NBGraphTierAllowProgramNoise > 0.5 ? 1.0 : 0.0;
    ProgramSimpleToggle *= NBGraphTierAllowProgramSimple > 0.5 ? 1.0 : 0.0;
    ProgramVoronoiToggle *= NBGraphTierAllowProgramVoronoi > 0.5 ? 1.0 : 0.0;

    // All outputs are defined before original Debug early returns.
    NBDistortionSignedRG = 0;
    NBDistortionNoiseMask = 1;
#if defined(NB_DEBUG_VERTEX_OFFSET) && defined(NB_GRAPH_MAIN_FORWARD)
    // Original raw-keyword behavior also retains vertex color when offset is off.
    Out = NBGraphDebugVertexOffsetToggle > 0.5 ? float4(NBGraphDebugVertexOffsetRGB, 1) : VertexColor;
    return;
#endif

    NoiseEnabled *= NBGraphTierAllowNoise > 0.5 ? 1.0 : 0.0;
    NoiseMaskToggle *= NBGraphTierAllowNoiseMask > 0.5 ? 1.0 : 0.0;

    // Derived Mask capability gates; serialized feature inputs remain intent.
    MaskToggle *= NBGraphTierAllowMask > 0.5 ? 1.0 : 0.0;
    Mask2Toggle *= NBGraphTierAllowMask2 > 0.5 ? 1.0 : 0.0;
    Mask3Toggle *= NBGraphTierAllowMask3 > 0.5 ? 1.0 : 0.0;

    // SG's fragment basis owns renderer odd scale; replace that factor with
    // the original custom matrix determinant for the active world-sim host.
    if(CustomLocalToggle>0.5)
        BitangentWS*=NBFX_MatrixOddNegativeScaleV1(float4x4(LocalToWorld0,LocalToWorld1,LocalToWorld2,LocalToWorld3))/GetOddNegativeScale();

    uint cd0=NBGraphDecodeUInt32(CustomDataFlag0Lo16,CustomDataFlag0Hi16);
    uint cd1=NBGraphDecodeUInt32(CustomDataFlag1Lo16,CustomDataFlag1Hi16);
    uint cd2=NBGraphDecodeUInt32(CustomDataFlag2Lo16,CustomDataFlag2Hi16);
    uint cd3=NBGraphDecodeUInt32(CustomDataFlag3Lo16,CustomDataFlag3Hi16);
    HueShift=(half)GetCustomData(cd0,FLAGBIT_POS_0_CUSTOMDATA_HUESHIFT,(half)HueShift,Custom1,Custom2);
    Contrast=(half)GetCustomData(cd2,FLAGBIT_POS_2_CUSTOMDATA_MAINTEX_CONTRAST,(half)Contrast,Custom1,Custom2);
    Saturability=(half)GetCustomData(cd1,FLAGBIT_POS_1_CUSTOMDATA_SATURATE,(half)Saturability,Custom1,Custom2);
    Dissolve.x=(half)((half)Dissolve.x+GetCustomData(cd0,FLAGBIT_POS_0_CUSTOMDATA_DISSOLVE_INTENSITY,0,Custom1,Custom2));
    Dissolve.z=(half)((half)Dissolve.z+GetCustomData(cd1,FLAGBIT_POS_1_CUSTOMDATA_DISSOLVE_MASK_INTENSITY,0,Custom1,Custom2));
    FresnelUnit.x=(half)((half)FresnelUnit.x+GetCustomData(cd0,FLAGBIT_POS_0_CUSTOMDATA_FRESNEL_OFFSET,0,Custom1,Custom2));
    PNoiseVec4.x+=GetCustomData(cd2,FLAGBIT_POS_2_CUSTOMDATA_DISSOLVE_NOISE1_OFFSET_X,0,Custom1,Custom2);
    PNoiseVec4.y+=GetCustomData(cd2,FLAGBIT_POS_2_CUSTOMDATA_DISSOLVE_NOISE1_OFFSET_Y,0,Custom1,Custom2);
    PNoiseVec4.z+=GetCustomData(cd2,FLAGBIT_POS_2_CUSTOMDATA_DISSOLVE_NOISE2_OFFSET_X,0,Custom1,Custom2);
    PNoiseVec4.w+=GetCustomData(cd2,FLAGBIT_POS_2_CUSTOMDATA_DISSOLVE_NOISE2_OFFSET_Y,0,Custom1,Custom2);
    EmissionUV+=float2(GetCustomData(cd3,FLAGBIT_POS_3_CUSTOMDATA_EMISSION_OFFSET_X,0,Custom1,Custom2),GetCustomData(cd3,FLAGBIT_POS_3_CUSTOMDATA_EMISSION_OFFSET_Y,0,Custom1,Custom2));
    ColorBlendUV+=float2(GetCustomData(cd3,FLAGBIT_POS_3_CUSTOMDATA_COLOR_BLEND_OFFSET_X,0,Custom1,Custom2),GetCustomData(cd3,FLAGBIT_POS_3_CUSTOMDATA_COLOR_BLEND_OFFSET_Y,0,Custom1,Custom2));
    float2 maskCustomOffset=float2(GetCustomData(cd0,FLAGBIT_POS_0_CUSTOMDATA_MASK_OFFSET_X,0,Custom1,Custom2),GetCustomData(cd0,FLAGBIT_POS_0_CUSTOMDATA_MASK_OFFSET_Y,0,Custom1,Custom2));
    if (((cd1>>FLAGBIT_POS_1_CUSTOMDATA_DISSOLVE_OFFSET_X)&8u)!=0u || ((cd1>>FLAGBIT_POS_1_CUSTOMDATA_DISSOLVE_OFFSET_Y)&8u)!=0u)
    {
        half4 dissolveST=(half4)DissolveMap.scaleTranslate;
        dissolveST.z+=GetCustomData(cd1,FLAGBIT_POS_1_CUSTOMDATA_DISSOLVE_OFFSET_X,0,Custom1,Custom2);
        dissolveST.w+=GetCustomData(cd1,FLAGBIT_POS_1_CUSTOMDATA_DISSOLVE_OFFSET_Y,0,Custom1,Custom2);
        DissolveMap.scaleTranslate=dissolveST;
    }

    NBFX_BaseColorInputV1 input = (NBFX_BaseColorInputV1)0;
    uint wrapFlags = NBGraphDecodeUInt32(NB_WrapFlagsLo16, NB_WrapFlagsHi16);
    uint noMipFlags = NBGraphDecodeUInt32(NB_ForceNoMipFlagsLo16, NB_ForceNoMipFlagsHi16);
    uint pNoiseBlendFlags = NBGraphDecodeUInt32(PNoiseBlendLo16, PNoiseBlendHi16);
    bool hasPNoise = ProgramNoiseToggle > 0.5 &&
        (ProgramSimpleToggle > 0.5 || ProgramVoronoiToggle > 0.5);
    half programNoise = 0.0h;
    if (hasPNoise)
        programNoise = NBGraphProgramNoise(ProgramNoiseUV,
            (half)ProgramNoiseRotate, PNoiseVec, (half4)PNoiseVec2,
            PNoiseVec3, PNoiseVec4, ProgramSimpleToggle > 0.5,
            ProgramVoronoiToggle > 0.5, pNoiseBlendFlags,
            (half)PNoiseBaseBlendOpacity);
#if defined(NB_DEBUG_PNOISE) && defined(NB_GRAPH_MAIN_FORWARD)
    if (hasPNoise) { Out = float4(programNoise.xxx, 1); return; }
#endif
    // The original ShaderLab _NORMALMAP keyword is represented by the
    // existing material toggle; no new SG keyword/variant is introduced.
    float3 normalForFeatures = (float3)NormalWS;
    half3 lightingNormalTS = half3(0, 0, 1);
    half lightingMetallicWeight = 1, lightingSmoothnessWeight = 1;
    if (!NB_GRAPH_DEPTH_SHADOW_PASS && (BumpMapToggle > 0.5))
        normalForFeatures = NBGraphNormalForFeatures(BumpTex, BumpUV,
            (half)BumpScale, (float3)NormalWS, TangentWS, BitangentWS,
            IsFrontFace, NBGraphDecodeUInt32(NB_Flags0Lo16, NB_Flags0Hi16),
            wrapFlags, noMipFlags, lightingNormalTS,
            lightingMetallicWeight, lightingSmoothnessWeight);
    // Preserve the existing Noise-off uniform prototype for screen passes;
    // it must not distort the surface's texture consumers when Noise is off.
    half2 signedRG = (half2)NB_DistortionNoise;
    half noiseMask = 1;
    half2 textureNoise = 0;

    if (NoiseEnabled>0.5)
    {
        if (NB_GRAPH_DEPTH_SHADOW_PASS || round(DistortMode)!=1.0)
        {
            DistortionDirection.x=(half)((half)DistortionDirection.x+GetCustomData(cd2,FLAGBIT_POS_2_CUSTOMDATA_NOISE_DIRECTION_X,0,Custom1,Custom2));
            DistortionDirection.y=(half)((half)DistortionDirection.y+GetCustomData(cd2,FLAGBIT_POS_2_CUSTOMDATA_NOISE_DIRECTION_Y,0,Custom1,Custom2));
        }
        NoiseIntensity=(half)GetCustomData(cd1,FLAGBIT_POS_1_CUSTOMDATA_NOISE_INTENSITY,(half)NoiseIntensity,Custom1,Custom2);
    }
    if (NoiseEnabled > 0.5)
    {
        NBGraphTextureNoise(NoiseMap, NoiseUV,
            NoiseMapUVRotation, (half4)NoiseOffset, (half)NoiseIntensity,
            (half4)DistortionDirection, NoiseMaskMap, NoiseMaskUV,
            NoiseMaskToggle > 0.5,
            NBGraphDecodeUInt32(NB_Flags0Lo16, NB_Flags0Hi16),
            NBGraphDecodeUInt32(NB_ColorChannelLo16, 0.0), wrapFlags, noMipFlags,
            !NB_GRAPH_DEPTH_SHADOW_PASS && round(DistortMode) == 1.0, (half)RefractionIOR,
            ViewDirWS, (IsFrontFace > 0.5 ? 1.0 : -1.0) * normalForFeatures,
            signedRG, noiseMask);
        // ShaderLab: _PROGRAM_NOISE_ACTIVE is nested under _NOISEMAP.
        // Blend the unmasked, already scaled signed RG; only texture
        // consumers multiply by noiseMask after this point.
        if (hasPNoise)
            signedRG = BlendPNoise(pNoiseBlendFlags,
                FLAG_BIT_PNOISE_BLEND_POS_0_DISTORT, signedRG,
                (half2)programNoise, (half)PNoiseDistortBlendOpacity);
        textureNoise = signedRG * noiseMask;
#if defined(NB_DEBUG_DISTORT) && defined(NB_GRAPH_MAIN_FORWARD)
        Out = float4(textureNoise, 0, 1); return;
#endif
    }
    NBDistortionSignedRG = (float2)signedRG;
    NBDistortionNoiseMask = (float)noiseMask;
    float2 mainTexNoise = textureNoise * (half)TexDistortionIntensity;
    // ShaderLab saves originUV before POM. Do not alter other feature UVs.
    float2 baseUVPreNoise = BaseMapUV;
#if defined(NB_GRAPH_MAIN_FORWARD) && !NB_GRAPH_DEPTH_SHADOW_PASS
    if (ParallaxMappingToggle > 0.5)
        baseUVPreNoise = NBGraphApplyParallax(ParallaxMappingMap,
            BaseMapUV, ParallaxMappingIntensity, ParallaxMappingVec,
            NormalWS, TangentWS, BitangentWS, ViewDirWS,
            IsFrontFace, wrapFlags, noMipFlags);
#endif
    float2 baseUV = baseUVPreNoise + mainTexNoise;
    half4 baseSample;
#if !NB_GRAPH_DEPTH_SHADOW_PASS
    if (ChromaticToggle > 0.5)
    {
        // Legacy half4 CBUFFER component, CustomData override and half
        // *=0.1 occur before passing half2 UVs to the three raw samples.
        half4 direction = (half4)DistortionDirection;
        direction.z = (half)GetCustomData(
            NBGraphDecodeUInt32(CustomDataFlag0Lo16, CustomDataFlag0Hi16),
            FLAGBIT_POS_0_CUSTOMDATA_CHORATICABERRAT_INTENSITY,
            direction.z, (half4)Custom1, (half4)Custom2);
        direction.z *= 0.1;
        baseSample = NBGraphChromaticSample(BaseMap,
            (half2)BaseMapUV, (half2)baseUV, direction.z,
            (NBGraphDecodeUInt32(NB_Flags0Lo16, NB_Flags0Hi16) &
                FLAG_BIT_PARTICLE_NOISE_CHORATICABERRAT_WITH_NOISE) != 0u,
            wrapFlags, noMipFlags);
    }
    else
#endif
        baseSample = NBGraphSampleFlipbookBaseV1(BaseMap, baseUV,
            BlendUV + mainTexNoise, BlendWeight, FlipbookToggle,
            wrapFlags, noMipFlags);
    input.sampledAlbedo = baseSample;
    // Historical sampled-color/A ports remain serialized but are disconnected.
    // The packed channel word selects alpha from this one protocol-owned sample.
    input.selectedAlpha = NBGraphSelectBaseAlpha(baseSample,
        NB_ColorChannelLo16);
    input.effectiveBaseColor = (half4)EffectiveBaseColor;
    if (!NB_GRAPH_DEPTH_SHADOW_PASS &&
        (NBGraphDecodeUInt32(NB_Flags0Lo16, NB_Flags0Hi16) &
        FLAG_BIT_PARTICLE_BACKCOLOR) != 0u && IsFrontFace < 0.5)
        input.effectiveBaseColor = (half4)BaseBackColor;
    input.timelineIntensity = (half)BaseColorIntensityForTimeline;
    input.applyTimelineIntensity = !NB_GRAPH_DEPTH_SHADOW_PASS;
    Out = (float4)NBFX_ComposeBaseColorV1(input);
    half4 sixWayPreAdjustAlbedo=(half4)Out;
    uint adjustmentFlags0 = NBGraphDecodeUInt32(NB_Flags0Lo16, NB_Flags0Hi16);
    if (!NB_GRAPH_DEPTH_SHADOW_PASS &&
        (adjustmentFlags0 & FLAG_BIT_PARTICLE_COLOR_ADJUSTMENT_ONLY_AFFECT_MAINTEX) != 0u)
        Out = (float4)NBGraphApplyColorAdjustment((half4)Out,
            (half)HueShift, (half)Contrast, (half3)ContrastMidColor.rgb,
            (half)Saturability, (half4)BaseMapColorRefine,
            NB_Flags0Lo16, NB_Flags0Hi16, NB_Flags1Lo16, NB_Flags1Hi16);
    if (!NB_GRAPH_DEPTH_SHADOW_PASS && FxLightMode > 3.5 && FxLightMode < 4.5)
        Out = NBGraphApplySixWay(sixWayPreAdjustAlbedo,RigRTBk,RigLBtF,
            SixWayEmissionRamp,baseUV,wrapFlags,noMipFlags,
            NBGraphDecodeUInt32(NB_Flags1Lo16,NB_Flags1Hi16),
            SixWayInfo,(half4)SixWayEmissionColor,
            (half)EffectiveBaseColor.a,PositionWS,normalForFeatures,
            (float3)ViewDirWS,ScreenPosition.xy,(half)IsFrontFace,
            (half3)SixBake0,(half3)SixBake1,(half3)SixBake2,
            (half3)SixBack0,(half3)SixBack1,(half3)SixBack2,
            (half4)SixTangentSigned);
    else if (!NB_GRAPH_DEPTH_SHADOW_PASS && FxLightMode > 0.5 && FxLightMode < 3.5)
        Out = NBGraphApplyLighting((half4)Out, FxLightMode, PositionWS,
            normalForFeatures, (float3)ViewDirWS, ScreenPosition.xy,
            (half)IsFrontFace, lightingNormalTS, lightingMetallicWeight,
            lightingSmoothnessWeight, (half4)MaterialInfo, (half4)SpecularColor,
            (half3)SixBack2);
    if (!NB_GRAPH_DEPTH_SHADOW_PASS && (MatCapToggle > 0.5))
        Out = (float4)NBGraphApplyMatCap((half4)Out, MatCapTex, (float3)normalForFeatures,
            (half3)PositionVS, (half)IsFrontFace, (half4)MatCapColor,
            (half4)MatCapInfo, noMipFlags);
    if (!NB_GRAPH_DEPTH_SHADOW_PASS && (EmissionEnabled > 0.5))
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
    if (!NB_GRAPH_DEPTH_SHADOW_PASS && (RampColorToggle > 0.5))
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
        half debugDissolvePreparedValue;
        NBFX_DissolveResolvedV3 resolved = NBGraphResolveDissolve(sampledDissolve,
            sampledDissolveMask, hasDissolveMask, (half4)Dissolve,
            (half)DissolveMaskMode, NB_ColorChannelLo16, hasPNoise,
            programNoise, pNoiseBlendFlags, (half)PNoiseDissolveBlendOpacity, debugDissolvePreparedValue);
#if defined(NB_DEBUG_DISSOLVE) && defined(NB_GRAPH_MAIN_FORWARD)
        Out = float4(debugDissolvePreparedValue.xxx, 1); return;
#endif
        Out.a *= resolved.coverage;
        if (!NB_GRAPH_DEPTH_SHADOW_PASS && (DissolveRampToggle > 0.5))
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
        if (!NB_GRAPH_DEPTH_SHADOW_PASS)
            Out = (float4)NBGraphApplyDissolveLine((half4)Out,
            resolved.valueBeforeSoftStep, (half4)DissolveLineRange,
            (half4)DissolveLineColor,
            NBGraphDecodeUInt32(NB_Flags1Lo16, NB_Flags1Hi16));
    }
    // Overlay 2 keeps its ShaderLab position after Dissolve and before Mask.
    // N1 texture Noise offsets this layer; CustomData remains a later slice.
    if (!NB_GRAPH_DEPTH_SHADOW_PASS && (ColorBlendMapToggle > 0.5))
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
            maskRotation, MaskMapOffsetAnition.xy, maskCustomOffset);
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
        if (hasPNoise)
            channelValue = BlendPNoise(pNoiseBlendFlags, FLAG_BIT_PNOISE_BLEND_POS_0_MASK,
                channelValue, programNoise, (half)PNoiseMaskBlendOpacity);
        NBFX_MaskCoverageInputV3 maskInput = (NBFX_MaskCoverageInputV3)0;
        maskInput.combinedMaskAfterNoise = channelValue;
        maskInput.refine = (maskFlags & FLAG_BIT_PARTICLE_1_MASK_REFINE) != 0u;
        maskInput.refinePowMulAdd = (half3)MaskRefineVec.xyz;
        maskInput.overallStrength = (half)MaskMapVec.x;
        half resolvedMaskCoverage = NBFX_ResolveMaskCoverageV3(maskInput);
#if defined(NB_DEBUG_MASK) && defined(NB_GRAPH_MAIN_FORWARD)
        Out = float4(resolvedMaskCoverage.xxx, 1); return;
#endif
        Out.a *= resolvedMaskCoverage;
    }
    if (!NB_GRAPH_DEPTH_SHADOW_PASS && (FresnelEnabled > 0.5))
    {
        Out = (float4)NBGraphApplyFresnel((half4)Out, ViewDirWS,
            normalForFeatures, (half)IsFrontFace, (half4)FresnelUnit,
            (half4)FresnelColor, (half3)FresnelRotation.xyz,
            NBGraphDecodeUInt32(NB_Flags0Lo16, NB_Flags0Hi16));
#if defined(NB_DEBUG_FRESNEL) && defined(NB_GRAPH_MAIN_FORWARD)
        return;
#endif
    }
    // ShaderLab applies camera-distance alpha after Fresnel, before vertex color.
    float sceneEyeDepth = 0.0;
    if (!NB_GRAPH_DEPTH_SHADOW_PASS && (DepthOutlineToggle > 0.5 || SoftParticlesEnabled > 0.5))
        sceneEyeDepth = NBGraphSceneEyeDepth(ScreenPosition.xy);
    if (!NB_GRAPH_DEPTH_SHADOW_PASS && (DepthOutlineToggle > 0.5))
    {
        half3 outlineRGB = (half3)Out.rgb;
        half outlineAlpha = (half)Out.a;
        NBFX_ApplyDepthOutlineV1(outlineRGB, outlineAlpha,
            (half4)DepthOutlineColor, (half2)DepthOutlineVec.xy,
            sceneEyeDepth, -PositionVS.z);
        Out = float4(outlineRGB, outlineAlpha);
    }
    if (!NB_GRAPH_DEPTH_SHADOW_PASS && (DistanceFadeToggle > 0.5))
        Out.a *= DepthFactor(-PositionVS.z, Fade.x, Fade.y);
    if (!NB_GRAPH_DEPTH_SHADOW_PASS && (SoftParticlesEnabled > 0.5))
        Out.a *= NBFX_SoftParticlesV1(SoftParticleFadeParams.x,
            SoftParticleFadeParams.y, sceneEyeDepth,
            -PositionVS.z);
    if ((NBGraphDecodeUInt32(NB_Flags1Lo16, NB_Flags1Hi16) &
        FLAG_BIT_PARTICLE_1_IGNORE_VERTEX_COLOR) == 0u)
        Out *= VertexColor;
    Out.rgb *= ColorA.rgb;
    Out.a *= ColorA.a;
    // Alpha=1 is the disabled/depth-shadow identity. Do not inject a
    // new half truncation into every existing Graph material.
    if (!NB_GRAPH_DEPTH_SHADOW_PASS && DecalAlpha != 1.0)
        Out.a = (half)((half)Out.a * (half)DecalAlpha);
    if (!NB_GRAPH_DEPTH_SHADOW_PASS)
        NBGraphApplyFogV1(Out.rgb, FogFactor, FogIntensity);
    if (!NB_GRAPH_DEPTH_SHADOW_PASS &&
        (adjustmentFlags0 & FLAG_BIT_PARTICLE_COLOR_ADJUSTMENT_ONLY_AFFECT_MAINTEX) == 0u)
        Out = (float4)NBGraphApplyColorAdjustment((half4)Out,
            (half)HueShift, (half)Contrast, (half3)ContrastMidColor.rgb,
            (half)Saturability, (half4)BaseMapColorRefine,
            NB_Flags0Lo16, NB_Flags0Hi16, NB_Flags1Lo16, NB_Flags1Hi16);
    if (!NB_GRAPH_DEPTH_SHADOW_PASS &&
        (NBGraphDecodeUInt32(NB_Flags0Lo16, NB_Flags0Hi16) &
            FLAG_BIT_PARTICLE_LINEARTOGAMMA_ON) != 0u)
        Out.rgb = (float3)(half3)LinearToGammaSpace((half3)Out.rgb);
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
    float BumpMapToggle, UnityTexture2D BumpTex, float BumpScale,
    float2 BumpUV, float3 TangentWS, float3 BitangentWS,
    float FxLightMode, float4 MaterialInfo, float4 SpecularColor,
    float3 PositionWS,
    float2 ProgramNoiseUV, float ProgramNoiseToggle, float ProgramSimpleToggle,
    float ProgramVoronoiToggle, float ProgramNoiseRotate,
    float4 PNoiseVec, float4 PNoiseVec2, float4 PNoiseVec3, float4 PNoiseVec4,
    float PNoiseBaseBlendOpacity, float PNoiseMaskBlendOpacity,
    float PNoiseDissolveBlendOpacity, float PNoiseBlendLo16, float PNoiseBlendHi16,
    UnityTexture2D RigRTBk, UnityTexture2D RigLBtF,
    UnityTexture2D SixWayEmissionRamp, float4 SixWayInfo,
    float4 SixWayEmissionColor, float SixWayColorAbsorptionToggle,
    float3 SixBake0, float3 SixBake1, float3 SixBake2,
    float3 SixBack0, float3 SixBack1, float3 SixBack2,
    float4 SixTangentSigned, float PNoiseDistortBlendOpacity,
    float DistortMode, float RefractionIOR,
    half DecalAlpha,
    UnityTexture2D ParallaxMappingMap, float ParallaxMappingToggle,
    float ParallaxMappingIntensity, float4 ParallaxMappingVec,
    float ChromaticToggle, float CustomDataFlag0Lo16,
    float CustomDataFlag0Hi16, float4 Custom1, float4 Custom2,
    float FogFactor, float FogIntensity,
    float FlipbookToggle, float2 BlendUV, float BlendWeight,
    float CustomDataFlag1Lo16,
    float CustomDataFlag1Hi16,
    float CustomDataFlag2Lo16,
    float CustomDataFlag2Hi16,
    float CustomDataFlag3Lo16,
    float CustomDataFlag3Hi16,
    float CustomLocalToggle,
    float4 LocalToWorld0,
    float4 LocalToWorld1,
    float4 LocalToWorld2,
    float4 LocalToWorld3,
    float4 WorldToLocal0,
    float4 WorldToLocal1,
    float4 WorldToLocal2,
    float4 WorldToLocal3,
    float NBGraphTierAllowMask, float NBGraphTierAllowMask2, float NBGraphTierAllowMask3,
    float NBGraphTierAllowNoise, float NBGraphTierAllowNoiseMask,
    float NBGraphDebugVertexOffsetToggle, float3 NBGraphDebugVertexOffsetRGB,
    float NBGraphTierAllowProgramNoise, float NBGraphTierAllowProgramSimple, float NBGraphTierAllowProgramVoronoi,
    float NBGraphTierAllowFresnel,
    float NBGraphTierAllowEmission, float NBGraphTierAllowColorBlend,
    float NBGraphTierAllowDissolve, float NBGraphTierAllowDissolveMask,
    float NBGraphTierAllowDissolveRamp, float NBGraphTierAllowDissolveRampMap,
    float NBGraphTierAllowParallax,
    float NBGraphTierAllowNormalMap,
    float NBGraphTierAllowColorRamp, float NBGraphTierAllowColorRampMap,
    float NBGraphTierAllowMatCap,
    out half4 Out, out half2 NBDistortionSignedRG,
    out half NBDistortionNoiseMask)
{
    MatCapToggle *= NBGraphTierAllowMatCap > 0.5 ? 1.0 : 0.0;

    RampColorToggle *= NBGraphTierAllowColorRamp > 0.5 ? 1.0 : 0.0;
    // Tier changes effective sampling only; the serialized source enum stays intent.
    RampColorSourceMode = RampColorSourceMode > 0.5 && NBGraphTierAllowColorRampMap > 0.5 ? 1.0 : 0.0;

    BumpMapToggle *= NBGraphTierAllowNormalMap > 0.5 ? 1.0 : 0.0;

    ParallaxMappingToggle *= NBGraphTierAllowParallax > 0.5 ? 1.0 : 0.0;

    DissolveToggle *= NBGraphTierAllowDissolve > 0.5 ? 1.0 : 0.0;
    DissolveMaskToggle *= NBGraphTierAllowDissolveMask > 0.5 ? 1.0 : 0.0;
    DissolveRampToggle *= NBGraphTierAllowDissolveRamp > 0.5 ? 1.0 : 0.0;
    // Raw source enum remains intent; Native stripped MAP samples gradient.
    DissolveRampSourceMode = DissolveRampSourceMode > 0.5 && NBGraphTierAllowDissolveRampMap > 0.5 ? 1.0 : 0.0;

    EmissionEnabled *= NBGraphTierAllowEmission > 0.5 ? 1.0 : 0.0;
    ColorBlendMapToggle *= NBGraphTierAllowColorBlend > 0.5 ? 1.0 : 0.0;

    FresnelEnabled *= NBGraphTierAllowFresnel > 0.5 ? 1.0 : 0.0;

    ProgramNoiseToggle *= NBGraphTierAllowProgramNoise > 0.5 ? 1.0 : 0.0;
    ProgramSimpleToggle *= NBGraphTierAllowProgramSimple > 0.5 ? 1.0 : 0.0;
    ProgramVoronoiToggle *= NBGraphTierAllowProgramVoronoi > 0.5 ? 1.0 : 0.0;

    // All outputs are defined before original Debug early returns.
    NBDistortionSignedRG = 0;
    NBDistortionNoiseMask = 1;
#if defined(NB_DEBUG_VERTEX_OFFSET) && defined(NB_GRAPH_MAIN_FORWARD)
    // Original raw-keyword behavior also retains vertex color when offset is off.
    Out = NBGraphDebugVertexOffsetToggle > 0.5 ? half4(NBGraphDebugVertexOffsetRGB, 1) : VertexColor;
    return;
#endif

    NoiseEnabled *= NBGraphTierAllowNoise > 0.5 ? 1.0 : 0.0;
    NoiseMaskToggle *= NBGraphTierAllowNoiseMask > 0.5 ? 1.0 : 0.0;

    // Derived Mask capability gates; serialized feature inputs remain intent.
    MaskToggle *= NBGraphTierAllowMask > 0.5 ? 1.0 : 0.0;
    Mask2Toggle *= NBGraphTierAllowMask2 > 0.5 ? 1.0 : 0.0;
    Mask3Toggle *= NBGraphTierAllowMask3 > 0.5 ? 1.0 : 0.0;

    // SG's fragment basis owns renderer odd scale; replace that factor with
    // the original custom matrix determinant for the active world-sim host.
    if(CustomLocalToggle>0.5)
        BitangentWS*=NBFX_MatrixOddNegativeScaleV1(float4x4(LocalToWorld0,LocalToWorld1,LocalToWorld2,LocalToWorld3))/GetOddNegativeScale();

    uint cd0=NBGraphDecodeUInt32(CustomDataFlag0Lo16,CustomDataFlag0Hi16);
    uint cd1=NBGraphDecodeUInt32(CustomDataFlag1Lo16,CustomDataFlag1Hi16);
    uint cd2=NBGraphDecodeUInt32(CustomDataFlag2Lo16,CustomDataFlag2Hi16);
    uint cd3=NBGraphDecodeUInt32(CustomDataFlag3Lo16,CustomDataFlag3Hi16);
    HueShift=(half)GetCustomData(cd0,FLAGBIT_POS_0_CUSTOMDATA_HUESHIFT,(half)HueShift,Custom1,Custom2);
    Contrast=(half)GetCustomData(cd2,FLAGBIT_POS_2_CUSTOMDATA_MAINTEX_CONTRAST,(half)Contrast,Custom1,Custom2);
    Saturability=(half)GetCustomData(cd1,FLAGBIT_POS_1_CUSTOMDATA_SATURATE,(half)Saturability,Custom1,Custom2);
    Dissolve.x=(half)((half)Dissolve.x+GetCustomData(cd0,FLAGBIT_POS_0_CUSTOMDATA_DISSOLVE_INTENSITY,0,Custom1,Custom2));
    Dissolve.z=(half)((half)Dissolve.z+GetCustomData(cd1,FLAGBIT_POS_1_CUSTOMDATA_DISSOLVE_MASK_INTENSITY,0,Custom1,Custom2));
    FresnelUnit.x=(half)((half)FresnelUnit.x+GetCustomData(cd0,FLAGBIT_POS_0_CUSTOMDATA_FRESNEL_OFFSET,0,Custom1,Custom2));
    PNoiseVec4.x+=GetCustomData(cd2,FLAGBIT_POS_2_CUSTOMDATA_DISSOLVE_NOISE1_OFFSET_X,0,Custom1,Custom2);
    PNoiseVec4.y+=GetCustomData(cd2,FLAGBIT_POS_2_CUSTOMDATA_DISSOLVE_NOISE1_OFFSET_Y,0,Custom1,Custom2);
    PNoiseVec4.z+=GetCustomData(cd2,FLAGBIT_POS_2_CUSTOMDATA_DISSOLVE_NOISE2_OFFSET_X,0,Custom1,Custom2);
    PNoiseVec4.w+=GetCustomData(cd2,FLAGBIT_POS_2_CUSTOMDATA_DISSOLVE_NOISE2_OFFSET_Y,0,Custom1,Custom2);
    EmissionUV+=float2(GetCustomData(cd3,FLAGBIT_POS_3_CUSTOMDATA_EMISSION_OFFSET_X,0,Custom1,Custom2),GetCustomData(cd3,FLAGBIT_POS_3_CUSTOMDATA_EMISSION_OFFSET_Y,0,Custom1,Custom2));
    ColorBlendUV+=float2(GetCustomData(cd3,FLAGBIT_POS_3_CUSTOMDATA_COLOR_BLEND_OFFSET_X,0,Custom1,Custom2),GetCustomData(cd3,FLAGBIT_POS_3_CUSTOMDATA_COLOR_BLEND_OFFSET_Y,0,Custom1,Custom2));
    float2 maskCustomOffset=float2(GetCustomData(cd0,FLAGBIT_POS_0_CUSTOMDATA_MASK_OFFSET_X,0,Custom1,Custom2),GetCustomData(cd0,FLAGBIT_POS_0_CUSTOMDATA_MASK_OFFSET_Y,0,Custom1,Custom2));
    if (((cd1>>FLAGBIT_POS_1_CUSTOMDATA_DISSOLVE_OFFSET_X)&8u)!=0u || ((cd1>>FLAGBIT_POS_1_CUSTOMDATA_DISSOLVE_OFFSET_Y)&8u)!=0u)
    {
        half4 dissolveST=(half4)DissolveMap.scaleTranslate;
        dissolveST.z+=GetCustomData(cd1,FLAGBIT_POS_1_CUSTOMDATA_DISSOLVE_OFFSET_X,0,Custom1,Custom2);
        dissolveST.w+=GetCustomData(cd1,FLAGBIT_POS_1_CUSTOMDATA_DISSOLVE_OFFSET_Y,0,Custom1,Custom2);
        DissolveMap.scaleTranslate=dissolveST;
    }

    NBFX_BaseColorInputV1 input = (NBFX_BaseColorInputV1)0;
    uint wrapFlags = NBGraphDecodeUInt32(NB_WrapFlagsLo16, NB_WrapFlagsHi16);
    uint noMipFlags = NBGraphDecodeUInt32(NB_ForceNoMipFlagsLo16, NB_ForceNoMipFlagsHi16);
    uint pNoiseBlendFlags = NBGraphDecodeUInt32(PNoiseBlendLo16, PNoiseBlendHi16);
    bool hasPNoise = ProgramNoiseToggle > 0.5 &&
        (ProgramSimpleToggle > 0.5 || ProgramVoronoiToggle > 0.5);
    half programNoise = 0.0h;
    if (hasPNoise)
        programNoise = NBGraphProgramNoise(ProgramNoiseUV,
            (half)ProgramNoiseRotate, PNoiseVec, (half4)PNoiseVec2,
            PNoiseVec3, PNoiseVec4, ProgramSimpleToggle > 0.5,
            ProgramVoronoiToggle > 0.5, pNoiseBlendFlags,
            (half)PNoiseBaseBlendOpacity);
#if defined(NB_DEBUG_PNOISE) && defined(NB_GRAPH_MAIN_FORWARD)
    if (hasPNoise) { Out = half4(programNoise.xxx, 1); return; }
#endif
    // The original ShaderLab _NORMALMAP keyword is represented by the
    // existing material toggle; no new SG keyword/variant is introduced.
    float3 normalForFeatures = (float3)NormalWS;
    half3 lightingNormalTS = half3(0, 0, 1);
    half lightingMetallicWeight = 1, lightingSmoothnessWeight = 1;
    if (!NB_GRAPH_DEPTH_SHADOW_PASS && (BumpMapToggle > 0.5))
        normalForFeatures = NBGraphNormalForFeatures(BumpTex, BumpUV,
            (half)BumpScale, (float3)NormalWS, TangentWS, BitangentWS,
            IsFrontFace, NBGraphDecodeUInt32(NB_Flags0Lo16, NB_Flags0Hi16),
            wrapFlags, noMipFlags, lightingNormalTS,
            lightingMetallicWeight, lightingSmoothnessWeight);
    half2 signedRG = (half2)NB_DistortionNoise;
    half noiseMask = 1;
    half2 textureNoise = 0;

    if (NoiseEnabled>0.5)
    {
        if (NB_GRAPH_DEPTH_SHADOW_PASS || round(DistortMode)!=1.0)
        {
            DistortionDirection.x=(half)((half)DistortionDirection.x+GetCustomData(cd2,FLAGBIT_POS_2_CUSTOMDATA_NOISE_DIRECTION_X,0,Custom1,Custom2));
            DistortionDirection.y=(half)((half)DistortionDirection.y+GetCustomData(cd2,FLAGBIT_POS_2_CUSTOMDATA_NOISE_DIRECTION_Y,0,Custom1,Custom2));
        }
        NoiseIntensity=(half)GetCustomData(cd1,FLAGBIT_POS_1_CUSTOMDATA_NOISE_INTENSITY,(half)NoiseIntensity,Custom1,Custom2);
    }
    if (NoiseEnabled > 0.5)
    {
        NBGraphTextureNoise(NoiseMap, NoiseUV,
            NoiseMapUVRotation, (half4)NoiseOffset, (half)NoiseIntensity,
            (half4)DistortionDirection, NoiseMaskMap, NoiseMaskUV,
            NoiseMaskToggle > 0.5,
            NBGraphDecodeUInt32(NB_Flags0Lo16, NB_Flags0Hi16),
            NBGraphDecodeUInt32(NB_ColorChannelLo16, 0.0), wrapFlags, noMipFlags,
            !NB_GRAPH_DEPTH_SHADOW_PASS && round(DistortMode) == 1.0, (half)RefractionIOR,
            ViewDirWS, (IsFrontFace > 0.5 ? 1.0 : -1.0) * normalForFeatures,
            signedRG, noiseMask);
        // ShaderLab: _PROGRAM_NOISE_ACTIVE is nested under _NOISEMAP.
        // Blend the unmasked, already scaled signed RG; only texture
        // consumers multiply by noiseMask after this point.
        if (hasPNoise)
            signedRG = BlendPNoise(pNoiseBlendFlags,
                FLAG_BIT_PNOISE_BLEND_POS_0_DISTORT, signedRG,
                (half2)programNoise, (half)PNoiseDistortBlendOpacity);
        textureNoise = signedRG * noiseMask;
#if defined(NB_DEBUG_DISTORT) && defined(NB_GRAPH_MAIN_FORWARD)
        Out = half4(textureNoise, 0, 1); return;
#endif
    }
    NBDistortionSignedRG = signedRG;
    NBDistortionNoiseMask = noiseMask;
    float2 mainTexNoise = textureNoise * (half)TexDistortionIntensity;
    // ShaderLab saves originUV before POM. Do not alter other feature UVs.
    float2 baseUVPreNoise = BaseMapUV;
#if defined(NB_GRAPH_MAIN_FORWARD) && !NB_GRAPH_DEPTH_SHADOW_PASS
    if (ParallaxMappingToggle > 0.5)
        baseUVPreNoise = NBGraphApplyParallax(ParallaxMappingMap,
            BaseMapUV, ParallaxMappingIntensity, ParallaxMappingVec,
            NormalWS, TangentWS, BitangentWS, ViewDirWS,
            IsFrontFace, wrapFlags, noMipFlags);
#endif
    float2 baseUV = baseUVPreNoise + mainTexNoise;
    half4 baseSample;
#if !NB_GRAPH_DEPTH_SHADOW_PASS
    if (ChromaticToggle > 0.5)
    {
        // Legacy half4 CBUFFER component, CustomData override and half
        // *=0.1 occur before passing half2 UVs to the three raw samples.
        half4 direction = (half4)DistortionDirection;
        direction.z = (half)GetCustomData(
            NBGraphDecodeUInt32(CustomDataFlag0Lo16, CustomDataFlag0Hi16),
            FLAGBIT_POS_0_CUSTOMDATA_CHORATICABERRAT_INTENSITY,
            direction.z, (half4)Custom1, (half4)Custom2);
        direction.z *= 0.1;
        baseSample = NBGraphChromaticSample(BaseMap,
            (half2)BaseMapUV, (half2)baseUV, direction.z,
            (NBGraphDecodeUInt32(NB_Flags0Lo16, NB_Flags0Hi16) &
                FLAG_BIT_PARTICLE_NOISE_CHORATICABERRAT_WITH_NOISE) != 0u,
            wrapFlags, noMipFlags);
    }
    else
#endif
        baseSample = NBGraphSampleFlipbookBaseV1(BaseMap, baseUV,
            BlendUV + mainTexNoise, BlendWeight, FlipbookToggle,
            wrapFlags, noMipFlags);
    input.sampledAlbedo = baseSample;
    input.selectedAlpha = NBGraphSelectBaseAlpha(baseSample,
        NB_ColorChannelLo16);
    input.effectiveBaseColor = EffectiveBaseColor;
    if (!NB_GRAPH_DEPTH_SHADOW_PASS &&
        (NBGraphDecodeUInt32(NB_Flags0Lo16, NB_Flags0Hi16) &
        FLAG_BIT_PARTICLE_BACKCOLOR) != 0u && IsFrontFace < 0.5)
        input.effectiveBaseColor = BaseBackColor;
    input.timelineIntensity = (half)BaseColorIntensityForTimeline;
    input.applyTimelineIntensity = !NB_GRAPH_DEPTH_SHADOW_PASS;
    Out = NBFX_ComposeBaseColorV1(input);
    half4 sixWayPreAdjustAlbedo=(half4)Out;
    uint adjustmentFlags0 = NBGraphDecodeUInt32(NB_Flags0Lo16, NB_Flags0Hi16);
    if (!NB_GRAPH_DEPTH_SHADOW_PASS &&
        (adjustmentFlags0 & FLAG_BIT_PARTICLE_COLOR_ADJUSTMENT_ONLY_AFFECT_MAINTEX) != 0u)
        Out = NBGraphApplyColorAdjustment(Out,
            (half)HueShift, (half)Contrast, ContrastMidColor.rgb,
            (half)Saturability, BaseMapColorRefine,
            NB_Flags0Lo16, NB_Flags0Hi16, NB_Flags1Lo16, NB_Flags1Hi16);
    if (!NB_GRAPH_DEPTH_SHADOW_PASS && FxLightMode > 3.5 && FxLightMode < 4.5)
        Out = NBGraphApplySixWay(sixWayPreAdjustAlbedo,RigRTBk,RigLBtF,
            SixWayEmissionRamp,baseUV,wrapFlags,noMipFlags,
            NBGraphDecodeUInt32(NB_Flags1Lo16,NB_Flags1Hi16),
            SixWayInfo,(half4)SixWayEmissionColor,
            (half)EffectiveBaseColor.a,PositionWS,normalForFeatures,
            (float3)ViewDirWS,ScreenPosition.xy,(half)IsFrontFace,
            (half3)SixBake0,(half3)SixBake1,(half3)SixBake2,
            (half3)SixBack0,(half3)SixBack1,(half3)SixBack2,
            (half4)SixTangentSigned);
    else if (!NB_GRAPH_DEPTH_SHADOW_PASS && FxLightMode > 0.5 && FxLightMode < 3.5)
        Out = NBGraphApplyLighting((half4)Out, FxLightMode, PositionWS,
            normalForFeatures, (float3)ViewDirWS, ScreenPosition.xy,
            (half)IsFrontFace, lightingNormalTS, lightingMetallicWeight,
            lightingSmoothnessWeight, (half4)MaterialInfo, (half4)SpecularColor,
            (half3)SixBack2);
    if (!NB_GRAPH_DEPTH_SHADOW_PASS && (MatCapToggle > 0.5))
        Out = NBGraphApplyMatCap(Out, MatCapTex, (float3)normalForFeatures,
            (half3)PositionVS, (half)IsFrontFace, MatCapColor,
            MatCapInfo, noMipFlags);
    if (!NB_GRAPH_DEPTH_SHADOW_PASS && (EmissionEnabled > 0.5))
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
    if (!NB_GRAPH_DEPTH_SHADOW_PASS && (RampColorToggle > 0.5))
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
        half debugDissolvePreparedValue;
        NBFX_DissolveResolvedV3 resolved = NBGraphResolveDissolve(sampledDissolve,
            sampledDissolveMask, hasDissolveMask, Dissolve,
            (half)DissolveMaskMode, NB_ColorChannelLo16, hasPNoise,
            programNoise, pNoiseBlendFlags, (half)PNoiseDissolveBlendOpacity, debugDissolvePreparedValue);
#if defined(NB_DEBUG_DISSOLVE) && defined(NB_GRAPH_MAIN_FORWARD)
        Out = half4(debugDissolvePreparedValue.xxx, 1); return;
#endif
        Out.a *= resolved.coverage;
        if (!NB_GRAPH_DEPTH_SHADOW_PASS && (DissolveRampToggle > 0.5))
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
        if (!NB_GRAPH_DEPTH_SHADOW_PASS)
            Out = NBGraphApplyDissolveLine(Out, resolved.valueBeforeSoftStep,
            DissolveLineRange, DissolveLineColor,
            NBGraphDecodeUInt32(NB_Flags1Lo16, NB_Flags1Hi16));
    }
    if (!NB_GRAPH_DEPTH_SHADOW_PASS && (ColorBlendMapToggle > 0.5))
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
            maskRotation, MaskMapOffsetAnition.xy, maskCustomOffset);
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
        if (hasPNoise)
            channelValue = BlendPNoise(pNoiseBlendFlags, FLAG_BIT_PNOISE_BLEND_POS_0_MASK,
                channelValue, programNoise, (half)PNoiseMaskBlendOpacity);
        NBFX_MaskCoverageInputV3 maskInput = (NBFX_MaskCoverageInputV3)0;
        maskInput.combinedMaskAfterNoise = channelValue;
        maskInput.refine = (maskFlags & FLAG_BIT_PARTICLE_1_MASK_REFINE) != 0u;
        maskInput.refinePowMulAdd = MaskRefineVec.xyz;
        maskInput.overallStrength = MaskMapVec.x;
        half resolvedMaskCoverage = NBFX_ResolveMaskCoverageV3(maskInput);
#if defined(NB_DEBUG_MASK) && defined(NB_GRAPH_MAIN_FORWARD)
        Out = half4(resolvedMaskCoverage.xxx, 1); return;
#endif
        Out.a *= resolvedMaskCoverage;
    }
    if (!NB_GRAPH_DEPTH_SHADOW_PASS && (FresnelEnabled > 0.5))
    {
        Out = NBGraphApplyFresnel(Out, ViewDirWS, normalForFeatures,
            (half)IsFrontFace, FresnelUnit, FresnelColor,
            FresnelRotation.xyz,
            NBGraphDecodeUInt32(NB_Flags0Lo16, NB_Flags0Hi16));
#if defined(NB_DEBUG_FRESNEL) && defined(NB_GRAPH_MAIN_FORWARD)
        return;
#endif
    }
    float sceneEyeDepth = 0.0;
    if (!NB_GRAPH_DEPTH_SHADOW_PASS && (DepthOutlineToggle > 0.5 || SoftParticlesEnabled > 0.5))
        sceneEyeDepth = NBGraphSceneEyeDepth(ScreenPosition.xy);
    if (!NB_GRAPH_DEPTH_SHADOW_PASS && (DepthOutlineToggle > 0.5))
        NBFX_ApplyDepthOutlineV1(Out.rgb, Out.a, DepthOutlineColor,
            DepthOutlineVec.xy, sceneEyeDepth, -PositionVS.z);
    if (!NB_GRAPH_DEPTH_SHADOW_PASS && (DistanceFadeToggle > 0.5))
        Out.a *= DepthFactor(-PositionVS.z, Fade.x, Fade.y);
    if (!NB_GRAPH_DEPTH_SHADOW_PASS && (SoftParticlesEnabled > 0.5))
        Out.a *= NBFX_SoftParticlesV1(SoftParticleFadeParams.x,
            SoftParticleFadeParams.y, sceneEyeDepth,
            -PositionVS.z);
    if ((NBGraphDecodeUInt32(NB_Flags1Lo16, NB_Flags1Hi16) &
        FLAG_BIT_PARTICLE_1_IGNORE_VERTEX_COLOR) == 0u)
        Out *= VertexColor;
    Out.rgb *= ColorA.rgb;
    Out.a *= ColorA.a;
    // Alpha=1 is the disabled/depth-shadow identity. Do not inject a
    // new half truncation into every existing Graph material.
    if (!NB_GRAPH_DEPTH_SHADOW_PASS && DecalAlpha != 1.0)
        Out.a = (half)((half)Out.a * (half)DecalAlpha);
    if (!NB_GRAPH_DEPTH_SHADOW_PASS)
        NBGraphApplyFogV1(Out.rgb, FogFactor, FogIntensity);
    if (!NB_GRAPH_DEPTH_SHADOW_PASS &&
        (adjustmentFlags0 & FLAG_BIT_PARTICLE_COLOR_ADJUSTMENT_ONLY_AFFECT_MAINTEX) == 0u)
        Out = NBGraphApplyColorAdjustment(Out,
            (half)HueShift, (half)Contrast, ContrastMidColor.rgb,
            (half)Saturability, BaseMapColorRefine,
            NB_Flags0Lo16, NB_Flags0Hi16, NB_Flags1Lo16, NB_Flags1Hi16);
    if (!NB_GRAPH_DEPTH_SHADOW_PASS &&
        (NBGraphDecodeUInt32(NB_Flags0Lo16, NB_Flags0Hi16) &
            FLAG_BIT_PARTICLE_LINEARTOGAMMA_ON) != 0u)
        Out.rgb = LinearToGammaSpace(Out.rgb);
    Out.a = saturate(Out.a * (half)AlphaAll);
}

#endif
