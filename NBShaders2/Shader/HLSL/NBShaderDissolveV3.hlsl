#ifndef NB_SHADER_DISSOLVE_V3
#define NB_SHADER_DISSOLVE_V3

#include "NBShaderSharedContractV3.hlsl"

// Stage 1 stops at the original NB_DEBUG_DISSOLVE early-return boundary.
NBFX_DissolvePreparedV3 NBFX_PrepareDissolveV3(NBFX_DissolvePrepareInputV3 input)
{
    NBFX_DissolvePreparedV3 prepared;
    half dissolveValue = pow(input.decodedAndNoiseBlendedValue, input.exponent);
    half dissolveMaskValue = input.decodedMaskValue;

    if (input.hasMask && input.maskMode < 0.5)
    {
        dissolveMaskValue = lerp(dissolveValue, dissolveMaskValue, input.maskStrength);
        dissolveValue = (dissolveValue + dissolveMaskValue) * 0.5;
    }

    prepared.valueForDebugAndSoftStep = dissolveValue;
    prepared.maskValueForLateMode = dissolveMaskValue;
    return prepared;
}

// Stage 2 starts only after the caller has handled NB_DEBUG_DISSOLVE.
NBFX_DissolveResolvedV3 NBFX_ResolveDissolveV3(NBFX_DissolveResolveInputV3 input)
{
    NBFX_DissolveResolvedV3 resolved;
    half dissolveValue = input.prepared.valueForDebugAndSoftStep;
    half dissolveMaskValue = input.prepared.maskValueForLateMode;
    half dissolveMaskStrength = input.maskStrength;
    half dissolveStrenth = input.threshold;

    half invSoftStep = 1 / input.softWidth;
    half dissolveValueBeforeSoftStep = dissolveValue - ((dissolveStrenth) * (invSoftStep + 1) - 1) * input.softWidth;
    dissolveValue = dissolveValue * invSoftStep - (1 + invSoftStep) * dissolveStrenth + 1;

    dissolveValue = saturate(dissolveValue);
    if (input.hasMask && input.maskMode > 0.5)
    {
        dissolveMaskStrength = dissolveMaskStrength - 1;
        dissolveMaskValue = saturate(dissolveMaskValue - dissolveMaskStrength);
        dissolveValue = lerp(1, dissolveValue, dissolveMaskValue);
    }

    resolved.valueBeforeSoftStep = dissolveValueBeforeSoftStep;
    resolved.coverage = dissolveValue;
    return resolved;
}

#endif
