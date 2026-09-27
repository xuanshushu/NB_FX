#ifndef NB_SHADER_MASK_V3
#define NB_SHADER_MASK_V3

#include "NBShaderSharedContractV3.hlsl"

// Samples, channels, gradient lookup, PNoise and the debug/alpha uses belong
// to the caller. This is only the original mask post-processing arithmetic.
half NBFX_ResolveMaskCoverageV3(NBFX_MaskCoverageInputV3 input)
{
    half maskValue = input.combinedMaskAfterNoise;
    if (input.refine)
    {
        maskValue = pow(maskValue, input.refinePowMulAdd.x);
        maskValue = maskValue * input.refinePowMulAdd.y;
        maskValue += input.refinePowMulAdd.z;
    }

    maskValue = lerp(1, maskValue, input.overallStrength);
    maskValue = saturate(maskValue);
    return maskValue;
}

#endif
