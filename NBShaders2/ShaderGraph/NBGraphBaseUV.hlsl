#ifndef NB_GRAPH_BASE_UV_INCLUDED
#define NB_GRAPH_BASE_UV_INCLUDED

#include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderUVV1.hlsl"
#include "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBGraphFlags.hlsl"
#include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderDepthDecalV1.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// The official SG Pixel ScreenPosition output is not raw SV_POSITION:
// BuildSurfaceDescriptionInputs optionally flips its Y with _ScaledScreenParams.
// Invert that exact transform before applying the ShaderLab denominator.
float2 NBGraphDecalRasterUV(float4 pixelPosition)
{
    float2 pixel = pixelPosition.xy;
    #if UNITY_UV_STARTS_AT_TOP
        if (_ProjectionParams.x < 0) pixel.y = _ScaledScreenParams.y - pixel.y;
    #else
        if (_ProjectionParams.x > 0) pixel.y = _ScaledScreenParams.y - pixel.y;
    #endif
    return pixel / _ScaledScreenParams.xy;
}

// The BaseMap Graph sample owns no implicit ST. This host adapter supplies the
// Mesh TEXCOORD0/1/2 streams and the original packed UV mode words enter
// ShaderLab's shared contract. This first slice supplies the inputs needed
// for MainTex modes 0/1/2/8 and SharedUV sources 0/1/2/8. Other modes retain
// their original packed values but still need position/screen/cylinder host
// inputs before they may be claimed as supported.
void NBGraphBaseUV_float(float4 UV, float4 BaseMapST,
    float BaseMapUVRotation, float BaseMapUVRotationSpeed,
    float4 BaseMapMaskMapOffset,
    float4 UV1, float4 UV2,
    float NB_Flags0Lo16, float NB_Flags0Hi16,
    float NB_Flags1Lo16, float NB_Flags1Hi16,
    float UVModeFlag0Lo16, float UVModeFlag0Hi16,
    float UVModeFlagType0Lo16, float UVModeFlagType0Hi16,
    float4 SharedUVST, float4 SharedUVVec,
    float4 TWParameter, float TWStrength, float4 PCCenter,
    float4 PixelPosition, float DepthDecalToggle,
    out float2 Out, out float2 MaskUV, out float2 Mask2UV,
    out float2 Mask3UV, out float2 EmissionUV, out float2 DissolveUV,
    out float2 DissolveMaskUV, out float2 ColorBlendUV,
    out float2 RampColorUV, out float2 NoiseUV, out float2 NoiseMaskUV,
    out float2 BumpUV, out float2 ProgramNoiseUV,
    out float DecalAlpha)
{
    NBFX_BaseUVInputV1 input = (NBFX_BaseUVInputV1)0;
    input.meshTexcoord0 = UV;
    DecalAlpha = 1.0;
    // Only the delegated URP Unlit Forward and its two exact NB distortion
    // clones have the old _DEPTH_DECAL variant. Exclude DepthOnly,
    // ShadowCaster, 2D, Meta, MotionVectors and preview.
    #if !defined(SHADERGRAPH_PREVIEW) && defined(SHADERPASS) && (SHADERPASS == SHADERPASS_UNLIT)
    if (DepthDecalToggle > 0.5)
    {
        float2 screenUV = NBGraphDecalRasterUV(PixelPosition);
        float sceneZBufferDepth = SampleSceneDepth(screenUV);
        #if !UNITY_REVERSED_Z
            sceneZBufferDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1, sceneZBufferDepth);
        #endif
        float3 fragWorldPos = ComputeWorldSpacePosition(screenUV,
            sceneZBufferDepth, UNITY_MATRIX_I_VP);
        // ShaderLab's _CUSTOM_LOCAL_TRANSFORM alternative matrix is a
        // separate pending Graph-host protocol, not silently emulated.
        float3 fragobjectPos = TransformWorldToObject(fragWorldPos);
        NBFX_DepthDecalProjectionV1 depthDecal = NBFX_ResolveDepthDecalV1(fragobjectPos);
        input.meshTexcoord0.xy = depthDecal.uv;
        DecalAlpha = depthDecal.alpha;
    }
    #endif
    // Legacy AttributesParticle.Custom1/Custom2 are separate float4 values
    // from TEXCOORD1/2. Do not fold them into UV0.zw or into one stream.
    input.custom1 = UV1;
    input.custom2 = UV2;

    NBFX_BaseUVParamsV1 parameters = (NBFX_BaseUVParamsV1)0;
    uint flags0 = NBGraphDecodeUInt32(NB_Flags0Lo16, NB_Flags0Hi16);
    uint flags1 = NBGraphDecodeUInt32(NB_Flags1Lo16, NB_Flags1Hi16);
    parameters.flags0 = flags0 &
        (FLAG_BIT_PARTICLE_UTWIRL_ON | FLAG_BIT_PARTICLE_POLARCOORDINATES_ON);
    parameters.flags1 = flags1 &
        (FLAG_BIT_PARTICLE_1_UV_FROM_MESH | FLAG_BIT_PARTICLE_1_USE_TEXCOORD1 |
         FLAG_BIT_PARTICLE_1_USE_TEXCOORD2);
    parameters.uvModeFlag0 = NBGraphDecodeUInt32(UVModeFlag0Lo16,
        UVModeFlag0Hi16);
    parameters.uvModeFlagType0 = NBGraphDecodeUInt32(UVModeFlagType0Lo16,
        UVModeFlagType0Hi16);
    parameters.baseMapST = BaseMapST;
    parameters.sharedUVST = (half4)SharedUVST;
    parameters.sharedUVVec = (half4)SharedUVVec;
    parameters.baseMapMaskMapOffset = (half4)BaseMapMaskMapOffset;
    parameters.twirlParameter = TWParameter;
    parameters.twirlStrength = TWStrength;
    parameters.polarCenter = PCCenter;
    parameters.baseMapUVRotation = (half)BaseMapUVRotation;
    parameters.baseMapUVRotationSpeed = (half)BaseMapUVRotationSpeed;
    parameters.timeY = _Time.y;
    BaseUVs resolved = NBFX_BuildBaseUVsV1(input, parameters);
    Out = resolved.mainTexUV;
    // These are source coordinates only. NBGraphBaseColor owns each feature's
    // rotation, ST, animated offset and sampling, as ShaderLab does after
    // GetUVByUVMode. In particular, they must not inherit MainTex's ST.
    MaskUV = GetUVByUVMode(parameters.uvModeFlag0, parameters.uvModeFlagType0,
        FLAG_BIT_UVMODE_POS_0_MASKMAP, resolved);
    Mask2UV = GetUVByUVMode(parameters.uvModeFlag0, parameters.uvModeFlagType0,
        FLAG_BIT_UVMODE_POS_0_MASKMAP_2, resolved);
    Mask3UV = GetUVByUVMode(parameters.uvModeFlag0, parameters.uvModeFlagType0,
        FLAG_BIT_UVMODE_POS_0_MASKMAP_3, resolved);
    EmissionUV = GetUVByUVMode(parameters.uvModeFlag0, parameters.uvModeFlagType0,
        FLAG_BIT_UVMODE_POS_0_EMISSION_MAP, resolved);
    DissolveUV = GetUVByUVMode(parameters.uvModeFlag0, parameters.uvModeFlagType0,
        FLAG_BIT_UVMODE_POS_0_DISSOLVE_MAP, resolved);
    DissolveMaskUV = GetUVByUVMode(parameters.uvModeFlag0, parameters.uvModeFlagType0,
        FLAG_BIT_UVMODE_POS_0_DISSOLVE_MASK_MAP, resolved);
    ColorBlendUV = GetUVByUVMode(parameters.uvModeFlag0, parameters.uvModeFlagType0,
        FLAG_BIT_UVMODE_POS_0_COLOR_BLEND_MAP, resolved);
    RampColorUV = GetUVByUVMode(parameters.uvModeFlag0, parameters.uvModeFlagType0,
        FLAG_BIT_UVMODE_POS_0_RAMP_COLOR_MAP, resolved);
    NoiseUV = GetUVByUVMode(parameters.uvModeFlag0, parameters.uvModeFlagType0,
        FLAG_BIT_UVMODE_POS_0_NOISE_MAP, resolved);
    NoiseMaskUV = GetUVByUVMode(parameters.uvModeFlag0, parameters.uvModeFlagType0,
        FLAG_BIT_UVMODE_POS_0_NOISE_MASK_MAP, resolved);
    BumpUV = GetUVByUVMode(parameters.uvModeFlag0, parameters.uvModeFlagType0,
        FLAG_BIT_UVMODE_POS_0_BUMPTEX, resolved);
    ProgramNoiseUV = GetUVByUVMode(parameters.uvModeFlag0, parameters.uvModeFlagType0,
        FLAG_BIT_UVMODE_POS_0_PROGRAM_NOISE, resolved);
}

void NBGraphBaseUV_half(half4 UV, half4 BaseMapST,
    half BaseMapUVRotation, half BaseMapUVRotationSpeed,
    half4 BaseMapMaskMapOffset,
    half4 UV1, half4 UV2,
    float NB_Flags0Lo16, float NB_Flags0Hi16,
    float NB_Flags1Lo16, float NB_Flags1Hi16,
    float UVModeFlag0Lo16, float UVModeFlag0Hi16,
    float UVModeFlagType0Lo16, float UVModeFlagType0Hi16,
    half4 SharedUVST, half4 SharedUVVec,
    half4 TWParameter, half TWStrength, half4 PCCenter,
    half4 PixelPosition, half DepthDecalToggle,
    out half2 Out, out half2 MaskUV, out half2 Mask2UV,
    out half2 Mask3UV, out half2 EmissionUV, out half2 DissolveUV,
    out half2 DissolveMaskUV, out half2 ColorBlendUV,
    out half2 RampColorUV, out half2 NoiseUV, out half2 NoiseMaskUV,
    out half2 BumpUV, out half2 ProgramNoiseUV,
    out half DecalAlpha)
{
    float2 resolved, maskResolved, mask2Resolved, mask3Resolved;
    float2 emissionResolved, dissolveResolved, dissolveMaskResolved;
    float2 colorBlendResolved, rampColorResolved, noiseResolved, noiseMaskResolved, bumpResolved, programNoiseResolved;
    float decalAlphaResolved;
    NBGraphBaseUV_float((float4)UV, (float4)BaseMapST,
        (float)BaseMapUVRotation, (float)BaseMapUVRotationSpeed,
        (float4)BaseMapMaskMapOffset,
        (float4)UV1, (float4)UV2,
        (float)NB_Flags0Lo16, (float)NB_Flags0Hi16,
        (float)NB_Flags1Lo16, (float)NB_Flags1Hi16,
        (float)UVModeFlag0Lo16, (float)UVModeFlag0Hi16,
        (float)UVModeFlagType0Lo16, (float)UVModeFlagType0Hi16,
        (float4)SharedUVST, (float4)SharedUVVec,
        (float4)TWParameter, (float)TWStrength, (float4)PCCenter,
        (float4)PixelPosition, (float)DepthDecalToggle,
        resolved, maskResolved, mask2Resolved, mask3Resolved,
        emissionResolved, dissolveResolved, dissolveMaskResolved,
        colorBlendResolved, rampColorResolved, noiseResolved, noiseMaskResolved, bumpResolved, programNoiseResolved, decalAlphaResolved);
    Out = (half2)resolved;
    MaskUV = (half2)maskResolved;
    Mask2UV = (half2)mask2Resolved;
    Mask3UV = (half2)mask3Resolved;
    EmissionUV = (half2)emissionResolved;
    DissolveUV = (half2)dissolveResolved;
    DissolveMaskUV = (half2)dissolveMaskResolved;
    ColorBlendUV = (half2)colorBlendResolved;
    RampColorUV = (half2)rampColorResolved;
    NoiseUV = (half2)noiseResolved;
    NoiseMaskUV = (half2)noiseMaskResolved;
    BumpUV = (half2)bumpResolved;
    ProgramNoiseUV = (half2)programNoiseResolved;
    DecalAlpha = (half)decalAlphaResolved;
}

#endif
