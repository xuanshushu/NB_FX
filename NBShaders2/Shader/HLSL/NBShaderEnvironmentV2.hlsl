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

// N0: only the numeric sample decode and TBN application are shared. The
// ShaderLab and Graph hosts still own UV stage, packed flags, texture sampling,
// tangent basis construction and consumers. Keep every original half boundary.
void NBFX_DecodeNormalMapV2(half4 normalMapSample, half bumpScale,
    bool maskMode, half3x3 tangentToWorld, out half3 normalTS,
    out float3 normalWS, out half metallicWeight, out half smoothnessWeight)
{
    metallicWeight = 1;
    smoothnessWeight = 1;
    if (maskMode)
    {
        normalTS = UnpackNormalRGB(half4(normalMapSample.xy, 1, 1), bumpScale);
        metallicWeight = normalMapSample.z;
        smoothnessWeight = normalMapSample.w;
    }
    else
    {
        normalTS = UnpackNormalScale(half4(normalMapSample), bumpScale);
    }
    normalWS = normalize(TransformTangentToWorld(normalTS, tangentToWorld));
}

#endif
