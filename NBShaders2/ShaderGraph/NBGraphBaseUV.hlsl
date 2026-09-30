#ifndef NB_GRAPH_BASE_UV_INCLUDED
#define NB_GRAPH_BASE_UV_INCLUDED

#include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderUVV1.hlsl"
#include "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBGraphFlags.hlsl"

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
    out float2 Out)
{
    NBFX_BaseUVInputV1 input = (NBFX_BaseUVInputV1)0;
    input.meshTexcoord0 = UV;
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
    out half2 Out)
{
    float2 resolved;
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
        resolved);
    Out = (half2)resolved;
}

#endif
