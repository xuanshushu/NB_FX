#ifndef NB_SHADER_DISTORTION_V1
#define NB_SHADER_DISTORTION_V1

#include "NBShaderSharedContractV1.hlsl"

// The caller supplies alpha before premultiplication. Pass-specific RT output
// and camera-opaque sampling remain outside this numeric payload builder.
NBFX_DistortionPayloadV1 NBFX_BuildDistortionPayloadV1(NBFX_DistortionInputV1 input)
{
    NBFX_DistortionPayloadV1 payload;
    half coverage = input.alphaBeforePremultiply * input.noiseMask;
    if (input.refineAlpha)
    {
        coverage = pow(coverage, input.alphaPow);
        coverage *= input.alphaMultiplier;
        coverage += input.alphaAdd;
    }
    payload.signedRG = input.signedNoise;
    payload.coverage = coverage;
    payload.intensity = input.intensity;
    return payload;
}

#endif
