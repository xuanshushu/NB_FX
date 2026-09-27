#ifndef NB_SHADER_SHARED_CONTRACT_V2
#define NB_SHADER_SHARED_CONTRACT_V2

// Additive contract: G1's four v1 entries and flag layout stay unchanged.
#include "NBShaderSharedContractV1.hlsl"

// G2/T02: host prepares the feature-specific UV mode, CustomData and ST.
// The original helper converts offsetSpeed to half2 only at the final UV
// animation call; stage and texture sampling remain the caller's choice.
struct NBFX_FeatureUVTransformInputV2
{
    float2 originUV;
    float4 scaleOffset;
    float2 offsetSpeed;
    float rotationDegrees;
    float2 rotationCenter;
    float timeY;
};

float2 NBFX_TransformFeatureUVV2(NBFX_FeatureUVTransformInputV2 input);

// G2/T04: host supplies original view-space vectors and sampled texture RGB.
// URP matrices, sampler/LOD, keyword and alpha stay in the ShaderLab adapter.
float2 NBFX_MatCapUVV2(half3 positionVS, half3 normalVS);
half3 NBFX_CompositeMatCapV2(
    half3 sourceRGB, half3 sampleRGB, half4 matCapColor, half multiplyBlend);

#endif
