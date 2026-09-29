#ifndef NB_GRAPH_BASE_UV_INCLUDED
#define NB_GRAPH_BASE_UV_INCLUDED

#include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderUVV1.hlsl"

// The BaseMap Graph sample owns no implicit ST. This host adapter supplies the
// zero-flag/UV0 subset of ShaderLab's shared UV contract, retaining its exact
// rotation -> ST -> scrolling order. Advanced UV modes and custom streams are
// intentionally not inferred from Shader Graph's UV0 input.
void NBGraphBaseUV_float(float4 UV, float4 BaseMapST,
    float BaseMapUVRotation, float BaseMapUVRotationSpeed,
    float4 BaseMapMaskMapOffset, out float2 Out)
{
    NBFX_BaseUVInputV1 input = (NBFX_BaseUVInputV1)0;
    input.meshTexcoord0 = UV;

    NBFX_BaseUVParamsV1 parameters = (NBFX_BaseUVParamsV1)0;
    parameters.baseMapST = BaseMapST;
    parameters.sharedUVST = half4(1.0h, 1.0h, 0.0h, 0.0h);
    parameters.baseMapMaskMapOffset = (half4)BaseMapMaskMapOffset;
    parameters.baseMapUVRotation = (half)BaseMapUVRotation;
    parameters.baseMapUVRotationSpeed = (half)BaseMapUVRotationSpeed;
    parameters.timeY = _Time.y;
    BaseUVs resolved = NBFX_BuildBaseUVsV1(input, parameters);
    Out = resolved.mainTexUV;
}

void NBGraphBaseUV_half(half4 UV, half4 BaseMapST,
    half BaseMapUVRotation, half BaseMapUVRotationSpeed,
    half4 BaseMapMaskMapOffset, out half2 Out)
{
    float2 resolved;
    NBGraphBaseUV_float((float4)UV, (float4)BaseMapST,
        (float)BaseMapUVRotation, (float)BaseMapUVRotationSpeed,
        (float4)BaseMapMaskMapOffset, resolved);
    Out = (half2)resolved;
}

#endif
