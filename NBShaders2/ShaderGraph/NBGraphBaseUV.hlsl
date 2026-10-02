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
// Mesh TEXCOORD0/1/2 streams, original packed UV words and stage-specific
// position/screen/cylinder inputs enter ShaderLab's one shared contract.
// Feature-specific ST/rotation/scroll remain owned by their current consumers;
// full CustomData, CustomLocalTransform, VAT and VFX are separate host slices.
// Material/stream binding only: the existing NBFX_BuildBaseUVsV1
// remains the one UV arithmetic implementation shared with ShaderLab.
NBFX_BaseUVParamsV1 NBGraphUVParameters(
    float4 BaseMapST,
    float BaseMapUVRotation,
    float BaseMapUVRotationSpeed,
    float4 BaseMapMaskMapOffset,
    float NB_Flags0Lo16,
    float NB_Flags0Hi16,
    float NB_Flags1Lo16,
    float NB_Flags1Hi16,
    float UVModeFlag0Lo16,
    float UVModeFlag0Hi16,
    float UVModeFlagType0Lo16,
    float UVModeFlagType0Hi16,
    float4 SharedUVST,
    float4 SharedUVVec,
    float4 TWParameter,
    float TWStrength,
    float4 PCCenter,
    float WorldSelector,
    float ObjectSelector,
    float4 CylinderMatrix0,
    float4 CylinderMatrix1,
    float4 CylinderMatrix2,
    float4 CylinderMatrix3, float FlipbookToggle)
{
    NBFX_BaseUVParamsV1 parameters = (NBFX_BaseUVParamsV1)0;
    uint flags0 = NBGraphDecodeUInt32(NB_Flags0Lo16, NB_Flags0Hi16);
    uint flags1 = NBGraphDecodeUInt32(NB_Flags1Lo16, NB_Flags1Hi16);
    parameters.flags0 = flags0 &
        (FLAG_BIT_PARTICLE_UTWIRL_ON | FLAG_BIT_PARTICLE_POLARCOORDINATES_ON);
    parameters.flags1 = flags1 &
        (FLAG_BIT_PARTICLE_1_UV_FROM_MESH | FLAG_BIT_PARTICLE_1_USE_TEXCOORD1 |
         FLAG_BIT_PARTICLE_1_USE_TEXCOORD2 | FLAG_BIT_PARTICLE_1_CYLINDER_CORDINATE | FLAG_BIT_PARTICLE_1_IS_PARTICLE_SYSTEM);
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
    parameters.worldSpaceUVModeSelector = (half)WorldSelector;
    parameters.objectSpaceUVModeSelector = (half)ObjectSelector;
    parameters.cylinderUVMatrix = float4x4(CylinderMatrix0, CylinderMatrix1,
        CylinderMatrix2, CylinderMatrix3);
    parameters.flipbookBlending = FlipbookToggle > 0.5 ? 1u : 0u;
    parameters.timeY = _Time.y;
    return parameters;
}

// Keep the old stage decision independent of the material's DistortMode.
// Static pass defines are emitted before Custom Functions by our package
// SubTarget; POM is only a main-Forward path, not inherited by NB clones.
bool NBGraphUVInFragment(uint flags0, float depthDecalToggle,
    float parallaxToggle)
{
#if defined(SHADERGRAPH_PREVIEW)
    // Preview has no interpolated Mesh vertex blocks. Preserve the previous
    // Graph preview's fragment evaluation instead of reading zero CI values.
    return true;
#else
    if ((flags0 & (FLAG_BIT_PARTICLE_UTWIRL_ON |
        FLAG_BIT_PARTICLE_POLARCOORDINATES_ON)) != 0u)
        return true;
    #if defined(NB_GRAPH_CAMERA_OPAQUE_PASS) || defined(NB_GRAPH_DEFERRED_DISTORT_PASS)
        return true;
    #elif defined(NB_GRAPH_MAIN_FORWARD)
        return depthDecalToggle > 0.5 || parallaxToggle > 0.5;
    #else
        // DepthOnly/Shadow/ordinary 2D have no original POM/Decal variant.
        return false;
    #endif
#endif
}

// Runs from the same pre-VertexOffset input as Fog and geometry preparation.
// Future VAT/CustomLocal hosts must feed their post-VAT/pre-offset PositionOS
// upstream here AND to Fog/VertexOffset, not replace this with final positions.
void NBGraphUVVertex_float(float3 PositionOS,
    float4 UV,
    float4 BaseMapST,
    float BaseMapUVRotation,
    float BaseMapUVRotationSpeed,
    float4 BaseMapMaskMapOffset,
    float4 UV1,
    float4 UV2,
    float NB_Flags0Lo16,
    float NB_Flags0Hi16,
    float NB_Flags1Lo16,
    float NB_Flags1Hi16,
    float UVModeFlag0Lo16,
    float UVModeFlag0Hi16,
    float UVModeFlagType0Lo16,
    float UVModeFlagType0Hi16,
    float4 SharedUVST,
    float4 SharedUVVec,
    float4 TWParameter,
    float TWStrength,
    float4 PCCenter,
    float WorldSelector,
    float ObjectSelector,
    float4 CylinderMatrix0,
    float4 CylinderMatrix1,
    float4 CylinderMatrix2,
    float4 CylinderMatrix3,
    float4 UV3, float FlipbookToggle,
    out float4 PreCylinderScreen, out float4 PreWorldObject,
    out float4 PreSharedMain)
{
    NBFX_BaseUVInputV1 input = (NBFX_BaseUVInputV1)0;
    input.meshTexcoord0 = UV;
    input.custom1 = UV1;
    input.custom2 = UV2;
    input.specialUVInTexcoord3 = UV3.yz;
    input.positionOS = PositionOS;
    input.positionWS = TransformObjectToWorld(PositionOS);
    float4 clipPosition = TransformObjectToHClip(PositionOS);
    input.screenUV = clipPosition.xy / clipPosition.w;
    input.screenUV = input.screenUV * 0.5 + 0.5;
    NBFX_BaseUVParamsV1 parameters = NBGraphUVParameters(
        BaseMapST, BaseMapUVRotation, BaseMapUVRotationSpeed, BaseMapMaskMapOffset, NB_Flags0Lo16, NB_Flags0Hi16, NB_Flags1Lo16, NB_Flags1Hi16, UVModeFlag0Lo16, UVModeFlag0Hi16, UVModeFlagType0Lo16, UVModeFlagType0Hi16, SharedUVST, SharedUVVec, TWParameter, TWStrength, PCCenter, WorldSelector, ObjectSelector, CylinderMatrix0, CylinderMatrix1, CylinderMatrix2, CylinderMatrix3, FlipbookToggle);
    BaseUVs resolved = NBFX_BuildBaseUVsV1(input, parameters);
    PreCylinderScreen = float4(resolved.cylinderUV, resolved.screenUV);
    PreWorldObject = float4(resolved.worldPosUV, resolved.objectPosUV);
    PreSharedMain = float4(resolved.sharedUV, resolved.mainTexUV);
}

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
    float3 PostPositionOS,
    float3 PositionWS,
    float4 PreCylinderScreen,
    float4 PreWorldObject,
    float4 PreSharedMain,
    float WorldSelector,
    float ObjectSelector,
    float4 CylinderMatrix0,
    float4 CylinderMatrix1,
    float4 CylinderMatrix2,
    float4 CylinderMatrix3,
    float ParallaxToggle,
    float4 UV3, float FlipbookToggle,
    float4 AnimationSheetBlendST, float AnimationSheetBlendIntensity,
    out float2 Out, out float2 MaskUV, out float2 Mask2UV,
    out float2 Mask3UV, out float2 EmissionUV, out float2 DissolveUV,
    out float2 DissolveMaskUV, out float2 ColorBlendUV,
    out float2 RampColorUV, out float2 NoiseUV, out float2 NoiseMaskUV,
    out float2 BumpUV, out float2 ProgramNoiseUV,
    out float DecalAlpha, out float2 BlendUV, out float BlendWeight)
{
    NBFX_BaseUVInputV1 input = (NBFX_BaseUVInputV1)0;
    input.meshTexcoord0 = UV;
    input.positionOS = PostPositionOS;
    input.positionWS = PositionWS;
    input.screenUV = NBGraphDecalRasterUV(PixelPosition);
#if defined(SHADERGRAPH_PREVIEW)
    input.positionOS = TransformWorldToObject(PositionWS);
#endif
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
    input.specialUVInTexcoord3 = UV3.yz;

    NBFX_BaseUVParamsV1 parameters = NBGraphUVParameters(
        BaseMapST, BaseMapUVRotation, BaseMapUVRotationSpeed, BaseMapMaskMapOffset, NB_Flags0Lo16, NB_Flags0Hi16, NB_Flags1Lo16, NB_Flags1Hi16, UVModeFlag0Lo16, UVModeFlag0Hi16, UVModeFlagType0Lo16, UVModeFlagType0Hi16, SharedUVST, SharedUVVec, TWParameter, TWStrength, PCCenter, WorldSelector, ObjectSelector, CylinderMatrix0, CylinderMatrix1, CylinderMatrix2, CylinderMatrix3, FlipbookToggle);
    BaseUVs resolved = NBFX_BuildBaseUVsV1(input, parameters);
    uint animationFlags1 = NBGraphDecodeUInt32(NB_Flags1Lo16, NB_Flags1Hi16);
    bool animationHelper = (animationFlags1 & FLAG_BIT_PARTICLE_1_ANIMATION_SHEET_HELPER) != 0u;
    BlendUV = NBFX_ResolveFlipbookUVV1(input.meshTexcoord0, AnimationSheetBlendST, animationHelper);
    BlendWeight = NBFX_ResolveFlipbookWeightV1(UV3.x, (half)AnimationSheetBlendIntensity, animationHelper);
    if (!NBGraphUVInFragment(parameters.flags0, DepthDecalToggle, ParallaxToggle))
    {
        // The old vertex path calculates spatial sources and Shared/Main
        // before VertexOffset, then interpolates them. In particular NDC and
        // atan2 must not be recalculated per pixel, nor MainST re-applied.
        resolved.cylinderUV = PreCylinderScreen.xy;
        resolved.screenUV = PreCylinderScreen.zw;
        resolved.worldPosUV = PreWorldObject.xy;
        resolved.objectPosUV = PreWorldObject.zw;
        resolved.sharedUV = PreSharedMain.xy;
        resolved.mainTexUV = PreSharedMain.zw;
    }
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
    float3 PostPositionOS,
    float3 PositionWS,
    float4 PreCylinderScreen,
    float4 PreWorldObject,
    float4 PreSharedMain,
    float WorldSelector,
    float ObjectSelector,
    float4 CylinderMatrix0,
    float4 CylinderMatrix1,
    float4 CylinderMatrix2,
    float4 CylinderMatrix3,
    float ParallaxToggle,
    half4 UV3, half FlipbookToggle,
    half4 AnimationSheetBlendST, half AnimationSheetBlendIntensity,
    out half2 Out, out half2 MaskUV, out half2 Mask2UV,
    out half2 Mask3UV, out half2 EmissionUV, out half2 DissolveUV,
    out half2 DissolveMaskUV, out half2 ColorBlendUV,
    out half2 RampColorUV, out half2 NoiseUV, out half2 NoiseMaskUV,
    out half2 BumpUV, out half2 ProgramNoiseUV,
    out half DecalAlpha, out half2 BlendUV, out half BlendWeight)
{
    float2 resolved, maskResolved, mask2Resolved, mask3Resolved;
    float2 emissionResolved, dissolveResolved, dissolveMaskResolved;
    float2 colorBlendResolved, rampColorResolved, noiseResolved, noiseMaskResolved, bumpResolved, programNoiseResolved;
    float decalAlphaResolved, blendWeightResolved;
    float2 blendUVResolved;
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
        PostPositionOS, PositionWS, PreCylinderScreen, PreWorldObject, PreSharedMain, WorldSelector, ObjectSelector, CylinderMatrix0, CylinderMatrix1, CylinderMatrix2, CylinderMatrix3, ParallaxToggle,
        (float4)UV3, (float)FlipbookToggle, (float4)AnimationSheetBlendST, (float)AnimationSheetBlendIntensity,
        resolved, maskResolved, mask2Resolved, mask3Resolved,
        emissionResolved, dissolveResolved, dissolveMaskResolved,
        colorBlendResolved, rampColorResolved, noiseResolved, noiseMaskResolved, bumpResolved, programNoiseResolved, decalAlphaResolved, blendUVResolved, blendWeightResolved);
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
    BlendUV = (half2)blendUVResolved;
    BlendWeight = (half)blendWeightResolved;
}

#endif
