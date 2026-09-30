#ifndef NB_SHADER_DISTORTION_V1
#define NB_SHADER_DISTORTION_V1

#include "NBShaderSharedContractV1.hlsl"

// Pure texture-noise decode shared by ShaderLab and future Graph hosts.
// Keep the three original half assignment boundaries; alpha is a separate
// weight, not part of signedRG. The host applies external NoiseMask,
// CustomData/Direction/Intensity and PNoise in its existing order, saves the
// screen payload, then weights only the texture-consumer offset by noiseMask.
void NBFX_DecodeTextureNoiseV1(
    half4 noiseSample,
    bool normalizeRG,
    inout half2 signedRG,
    inout half noiseMask)
{
    signedRG = noiseSample.xy;
    UNITY_FLATTEN
    if (normalizeRG)
    {
        signedRG = signedRG * 2 - 1;
    }
    noiseMask *= noiseSample.a;
}

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
