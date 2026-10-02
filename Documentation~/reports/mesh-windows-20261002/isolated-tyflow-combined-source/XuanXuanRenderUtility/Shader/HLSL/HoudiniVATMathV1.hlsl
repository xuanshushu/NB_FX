#ifndef NB_FX_HOUDINI_VAT_MATH_V1_INCLUDED
#define NB_FX_HOUDINI_VAT_MATH_V1_INCLUDED

float3 HVAT_RotateByQuat(float3 v, float4 q)
{
    float3 t = cross(q.xyz, v);
    return v + cross(q.xyz, t + v * q.w) * 2.0;
}

float3 HVAT_DecodeCompressedNormal(float posA)
{
    float scaledA = posA * 1024.0;
    float xIdx    = floor(scaledA / 32.0);
    float yRaw    = scaledA - xIdx * 32.0;
    float xNorm   = xIdx / 31.5;
    float yNorm   = yRaw / 31.5;
    float2 xy     = float2(xNorm, yNorm) * 4.0 - 2.0;
    float d       = dot(xy, xy);
    float sqrtF   = sqrt(saturate(1.0 - d * 0.25));
    return float3(-sqrtF * xy.x, 1.0 - d * 0.5, sqrtF * xy.y);
}

float2 HVAT_VatUV(float selectedFrame, float uv_r, float uv_g,
                  float oneMinusBoundMaxR, float multiplyBoundMinB, float totalFrames)
{
    float wrapped = fmod(selectedFrame - 1.0, totalFrames);
    float vBase   = (1.0 - uv_g) * oneMinusBoundMaxR
                    + (wrapped / totalFrames) * oneMinusBoundMaxR;
    return float2(multiplyBoundMinB, 1.0 - vBase);
}

void HVAT_ComputeMeshFrameSelectionV1(float totalFrames, float timeY,
    float gameTimeAtFirstFrame, float fps, float playbackSpeed,
    float autoPlayback, float displayFrame,
    out float selectedFrame, out float frameAlpha)
{
    float animTime    = (timeY - gameTimeAtFirstFrame)
                        * (fps / (totalFrames - 0.01))
                        * playbackSpeed;
    float frameFloat  = frac(animTime) * totalFrames;
    selectedFrame = autoPlayback ? floor(frameFloat) + 1.0 : floor(displayFrame);
    frameAlpha = frac(autoPlayback ? frameFloat : displayFrame);
}

void HVAT_ApplySoftBodySamplesV1(inout float3 positionOS,
    inout float3 normalOS, float4 posSample, float4 pos2,
    float4 rotSample, float loadPosTwoTex, float unloadRotTex,
    float comparisonBoundMaxb, float3 boundsMax, float3 boundsMin)
{
    float3 posRGB = posSample.rgb;
    float posA = posSample.a;
    if (loadPosTwoTex > 0.5)
        posRGB += pos2.rgb * 0.01;
    float3 posDecoded = posRGB * (boundsMax - boundsMin) + boundsMin;
    float3 displacement = comparisonBoundMaxb ? posRGB : posDecoded;
    positionOS += displacement;
    if (unloadRotTex > 0.5)
        normalOS = normalize(HVAT_DecodeCompressedNormal(posA));
    else
    {
        float4 rotFinal = comparisonBoundMaxb ? rotSample : (rotSample - 0.5) * 2.0;
        normalOS = normalize(HVAT_RotateByQuat(float3(0.0, 1.0, 0.0), rotFinal));
    }
}

float4 HVAT_DecodeQuaternion(float3 xyz, float maxComp)
{
    float w = sqrt(saturate(1.0 - dot(xyz, xyz)));
    float4 q = float4(xyz.x, xyz.y, xyz.z, w);
    int mc = (int)maxComp;
    if      (mc == 1) q = float4(    w,  xyz.y,  xyz.z,  xyz.x);
    else if (mc == 2) q = float4(xyz.x,     -w,  xyz.z, -xyz.y);
    else if (mc == 3) q = float4(xyz.x,  xyz.y,     -w, -xyz.z);
    return q;
}

float2 HVAT_DecodeLookupUV(float4 lookupSample, float boundMinX)
{
    float lookupHDR = (frac(-boundMinX * 10.0) >= 0.5) ? 1.0 : 0.0;
    float divisor   = lookupHDR ? 2048.0 : 255.0;
    float lookupX   = lookupSample.r + lookupSample.g / divisor;
    float lookupY   = 1.0 - (lookupSample.b + lookupSample.a / divisor);
    return float2(lookupX, lookupY);
}

float HVAT_HashRandom2D(float2 seed)
{
    return frac(sin(dot(seed, float2(12.9898, 78.233))) * 43758.5453);
}

#endif
