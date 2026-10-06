#ifndef NB_SHADER_UV_V1
#define NB_SHADER_UV_V1

// Include order is intentional: Core supplies Unity shader macros, the shared
// utility supplies the existing rotation/coordinate/time helpers, and the v1
// contract supplies BaseUVs plus the single packed-flag decoder implementation.
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.xuanxuan.nb.fx/XuanXuanRenderUtility/Shader/HLSL/XuanXuan_Utility.hlsl"
#include "NBShaderSharedContractV1.hlsl"

// Pure equivalents of the two Base-UV helpers currently private to
// NBShaderInput.hlsl. The Graph host must not include that ShaderLab input file.
float2 NBFX_GetPosUVByPosUVModeV1(float3 position, half posUVMode)
{
    switch ((int)posUVMode)
    {
        case 0: return position.xy;
        case 1: return position.xz;
        case 2: return position.yz;
        default: return position.xz;
    }
}

float2 NBFX_UTwirlV1(float2 uv, float2 center, float strength)
{
    float2 delta = uv - center;
    float angle = strength * length(delta);
    float x = cos(angle) * delta.x - sin(angle) * delta.y;
    float y = sin(angle) * delta.x + cos(angle) * delta.y;
    return float2(x + center.x, y + center.y);
}

BaseUVs NBFX_BuildBaseUVsV1(NBFX_BaseUVInputV1 input, NBFX_BaseUVParamsV1 parameters)
{
    // The caller supplies the stage-specific pre/post displacement positions
    // and projected/raster screen UV. Keep both custom streams as float4 until
    // the existing GetCustomData(half4, half4) call boundary.
    float2 defaultUVChannel = input.meshTexcoord0.xy;
    float2 specialUVChannel = input.meshTexcoord0.zw;
#if _FLIPBOOKBLENDING_ON
    if (((parameters.flags1 & FLAG_BIT_PARTICLE_1_IS_PARTICLE_SYSTEM) != 0u) &
        ((parameters.flags1 & FLAG_BIT_PARTICLE_1_USE_TEXCOORD2) != 0u))
    {
        specialUVChannel = input.specialUVInTexcoord3;
    }
#else
    // Graph has no Flipbook variant. Keep the particle TEXCOORD3.yz
    // exception and preserve the old non-Flipbook path when zero.
    if (parameters.flipbookBlending != 0u &&
        (parameters.flags1 & FLAG_BIT_PARTICLE_1_IS_PARTICLE_SYSTEM) != 0u &&
        (parameters.flags1 & FLAG_BIT_PARTICLE_1_USE_TEXCOORD2) != 0u)
    {
        specialUVChannel = input.specialUVInTexcoord3;
    }
    else if (parameters.flipbookBlending == 0u &&
        (parameters.flags1 & FLAG_BIT_PARTICLE_1_UV_FROM_MESH) != 0u)
    {
        if ((parameters.flags1 & FLAG_BIT_PARTICLE_1_USE_TEXCOORD1) != 0u)
        {
            specialUVChannel = input.custom1.xy;
        }
        if ((parameters.flags1 & FLAG_BIT_PARTICLE_1_USE_TEXCOORD2) != 0u)
        {
            specialUVChannel = input.custom2.xy;
        }
    }
#endif

    if ((parameters.flags1 & FLAG_BIT_PARTICLE_1_UIEFFECT_SPRITE_MODE) != 0u)
    {
        defaultUVChannel = defaultUVChannel * parameters.mainTexReverseST.xy + parameters.mainTexReverseST.zw;
    }

    float3 positionOS = input.positionOS;
    float2 cylinderUV = input.meshTexcoord0.xy;
    if ((parameters.flags1 & FLAG_BIT_PARTICLE_1_CYLINDER_CORDINATE) != 0u)
    {
        positionOS = mul(parameters.cylinderUVMatrix, float4(positionOS, 1));
        cylinderUV = CylinderCoordinate(positionOS);
    }

    float2 uvAfterTwirlPolar = defaultUVChannel;
    if ((parameters.flags0 & FLAG_BIT_PARTICLE_UTWIRL_ON) != 0u)
    {
        uvAfterTwirlPolar = NBFX_UTwirlV1(defaultUVChannel, parameters.twirlParameter.xy, parameters.twirlStrength);
    }
    if ((parameters.flags0 & FLAG_BIT_PARTICLE_POLARCOORDINATES_ON) != 0u)
    {
        float2 uvAfterTwirl = uvAfterTwirlPolar;
        uvAfterTwirlPolar = PolarCoordinates(uvAfterTwirlPolar, parameters.polarCenter.xy);
        uvAfterTwirlPolar = lerp(uvAfterTwirl, uvAfterTwirlPolar, parameters.polarCenter.z);
    }

    BaseUVs baseUVs = (BaseUVs)0;
    baseUVs.defaultUVChannel = defaultUVChannel;
    baseUVs.specialUVChannel = specialUVChannel;
    baseUVs.uvAfterTwirlPolar = uvAfterTwirlPolar;
    baseUVs.cylinderUV = cylinderUV;
    baseUVs.screenUV = input.screenUV;
    baseUVs.worldPosUV = NBFX_GetPosUVByPosUVModeV1(input.positionWS, parameters.worldSpaceUVModeSelector);
    baseUVs.objectPosUV = NBFX_GetPosUVByPosUVModeV1(positionOS, parameters.objectSpaceUVModeSelector);

    // Preserve original ordering: shared UV is available when main UV mode is
    // decoded, while mainTexUV is still zero when shared UV mode is decoded.
    baseUVs.sharedUV = defaultUVChannel;
    float2 sharedUV = GetUVByUVMode(parameters.uvModeFlag0, parameters.uvModeFlagType0,
        FLAG_BIT_UVMODE_POS_0_SHAREDUV, baseUVs);
    half4 sharedUVVec = parameters.sharedUVVec;
    sharedUVVec.z += parameters.timeY * sharedUVVec.w;
    sharedUV = Rotate_Radians_float(sharedUV, half2(0.5, 0.5), sharedUVVec.z);
    sharedUV.x += GetCustomData(parameters.customDataFlag3, FLAGBIT_POS_3_CUSTOMDATA_SHARED_UV_OFFSET_X,
        0, input.custom1, input.custom2);
    sharedUV.y += GetCustomData(parameters.customDataFlag3, FLAGBIT_POS_3_CUSTOMDATA_SHARED_UV_OFFSET_Y,
        0, input.custom1, input.custom2);
    sharedUV = sharedUV * parameters.sharedUVST.xy + parameters.sharedUVST.zw;
    sharedUV = UVOffsetAnimaiton(sharedUV, sharedUVVec.xy, parameters.timeY);
    baseUVs.sharedUV = sharedUV;

    baseUVs.mainTexUV = defaultUVChannel;
    float2 baseMapUV = GetUVByUVMode(parameters.uvModeFlag0, parameters.uvModeFlagType0,
        FLAG_BIT_UVMODE_POS_0_MAINTEX, baseUVs);
    float2 mainTexUV = 0;
    half baseMapUVRotation = parameters.baseMapUVRotation;
    baseMapUVRotation += parameters.timeY * parameters.baseMapUVRotationSpeed;
    baseMapUV = Rotate_Radians_float(baseMapUV, half2(0.5, 0.5), baseMapUVRotation);

    UNITY_BRANCH
    if (((parameters.flags0 & FLAG_BIT_PARTICLE_UIEFFECT_ON) != 0u) &
        !((parameters.flags1 & FLAG_BIT_PARTICLE_1_UIEFFECT_BASEMAP_MODE) != 0u))
    {
        if ((parameters.flags1 & FLAG_BIT_PARTICLE_1_UIEFFECT_SPRITE_MODE) != 0u)
        {
            float2 originUV = input.meshTexcoord0.xy;
            mainTexUV = originUV * parameters.uiMainTexST.xy + parameters.uiMainTexST.zw;
        }
        else
        {
            mainTexUV = baseMapUV * parameters.uiMainTexST.xy + parameters.uiMainTexST.zw;
        }
    }
    else
    {
        baseMapUV.x += GetCustomData(parameters.customDataFlag0, FLAGBIT_POS_0_CUSTOMDATA_MAINTEX_OFFSET_X,
            0, input.custom1, input.custom2);
        baseMapUV.y += GetCustomData(parameters.customDataFlag0, FLAGBIT_POS_0_CUSTOMDATA_MAINTEX_OFFSET_Y,
            0, input.custom1, input.custom2);
        mainTexUV = baseMapUV * parameters.baseMapST.xy + parameters.baseMapST.zw;
    }
    mainTexUV = UVOffsetAnimaiton(mainTexUV, parameters.baseMapMaskMapOffset.xy, parameters.timeY);
    baseUVs.mainTexUV = mainTexUV;
    return baseUVs;
}

// Pure original animation-sheet secondary UV and blend-weight selection.
// These do not advance frames; UV0.zw/TEXCOORD3.x or AnimationSheetHelper
// already supply the next frame and fractional blend.
float2 NBFX_ResolveFlipbookUVV1(float4 meshTexcoord0,
    float4 animationSheetBlendST, bool helper)
{
    return helper ? meshTexcoord0.xy * animationSheetBlendST.xy +
        animationSheetBlendST.zw : meshTexcoord0.zw;
}
float NBFX_ResolveFlipbookWeightV1(float streamWeight,
    half helperWeight, bool helper)
{
    return helper ? (float)helperWeight : streamWeight;
}

#endif
