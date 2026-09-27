#ifndef NB_SHADER_ENVIRONMENT_V2
#define NB_SHADER_ENVIRONMENT_V2

#include "NBShaderSharedContractV2.hlsl"

float2 NBFX_MatCapUVV2(half3 positionVS, half3 normalVS)
{
    float3 r = reflect(positionVS, normalVS);
    r = normalize(r);
    float m = 2.828427f * sqrt(r.z + 1.0);
    return r.xy / m + 0.5;
}

half3 NBFX_CompositeMatCapV2(
    half3 sourceRGB, half3 sampleRGB, half4 matCapColor, half multiplyBlend)
{
    sampleRGB *= matCapColor.rgb;

    half3 matCapMutilResult = sourceRGB * sampleRGB;
    half3 matAddResult = sourceRGB + sampleRGB;
    half3 matCapResult = lerp(matAddResult, matCapMutilResult, multiplyBlend);

    return lerp(sourceRGB, matCapResult, matCapColor.a);
}

#endif
