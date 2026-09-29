#ifndef NB_SHADER_SHARED_CONTRACT_V1
#define NB_SHADER_SHARED_CONTRACT_V1

// G1 v1: cross-host numeric inputs only. ShaderLab and Shader Graph/VFX bind
// their own resources and material state; neither may redefine packed flags.
#include "NBShaderFlags.hlsl"

struct NBFX_BaseUVInputV1
{
    float4 meshTexcoord0;
    float2 specialUVInTexcoord3;
    float4 custom1;
    float4 custom2;
    float3 positionOS;
    float3 positionWS;
    float2 screenUV;
};

struct NBFX_BaseUVParamsV1
{
    uint flags0;
    uint flags1;
    uint customDataFlag0;
    uint customDataFlag3;
    uint uvModeFlag0;
    uint uvModeFlagType0;
    float4 mainTexReverseST;
    float4 uiMainTexST;
    float4 baseMapST;
    half4 sharedUVST;
    half4 sharedUVVec;
    half4 baseMapMaskMapOffset;
    float4 twirlParameter;
    float4 polarCenter;
    float4x4 cylinderUVMatrix;
    float twirlStrength;
    half baseMapUVRotation;
    half baseMapUVRotationSpeed;
    half worldSpaceUVModeSelector;
    half objectSpaceUVModeSelector;
    float timeY;
};

struct NBFX_BaseColorInputV1
{
    half4 sampledAlbedo;
    half selectedAlpha;
    half4 effectiveBaseColor;
    half timelineIntensity;
    bool applyTimelineIntensity;
};

struct NBFX_DistortionInputV1
{
    half2 signedNoise;
    half noiseMask;
    half alphaBeforePremultiply;
    bool refineAlpha;
    half alphaPow;
    half alphaMultiplier;
    half alphaAdd;
    half intensity;
};

struct NBFX_DistortionPayloadV1
{
    half2 signedRG;
    half coverage;
    half intensity;
};

struct NBFX_VertexOffsetPreparedV1
{
    half3 normalOS;
    half3 directionOS;
    half3 customDirectionOS;
    half sampledScalar;
    half maskWeight;
    half intensity;
    int directionMode;
};

BaseUVs NBFX_BuildBaseUVsV1(NBFX_BaseUVInputV1 input, NBFX_BaseUVParamsV1 parameters);
half4 NBFX_ComposeBaseColorV1(NBFX_BaseColorInputV1 input);
void NBFX_ApplyColorOverlayV1(inout half3 baseColor, inout half baseAlpha,
    half4 overlaySample, half4 overlayTint, half overlayColorIntensity,
    half overlayAlphaIntensity, bool multiplyMode, bool alphaMultiplyMode);
void NBFX_ApplyColorRampV1(inout half3 baseColor, inout half baseAlpha,
    half4 rampSample, half4 rampTint, bool addMode);
void NBFX_ApplyColorAdjustmentV1(inout half3 color, half alpha,
    bool hueOn, half hueShift,
    bool contrastOn, half contrast, half3 contrastMidColor,
    bool saturationOn, half saturability,
    bool refineOn, half4 baseMapColorRefine,
    bool premultiplyRGB);
NBFX_DistortionPayloadV1 NBFX_BuildDistortionPayloadV1(NBFX_DistortionInputV1 input);
half3 NBFX_ComputeVertexOffsetOSV1(NBFX_VertexOffsetPreparedV1 input);

#endif
