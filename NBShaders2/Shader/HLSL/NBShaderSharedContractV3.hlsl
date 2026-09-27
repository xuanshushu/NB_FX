#ifndef NB_SHADER_SHARED_CONTRACT_V3
#define NB_SHADER_SHARED_CONTRACT_V3

// Additive T03 numeric contract. Host owns texture/flag decoding, debug early
// returns, alpha multiplication and Pass-specific output; V1/V2 stay unchanged.
#include "NBShaderSharedContractV2.hlsl"

struct NBFX_DissolvePrepareInputV3
{
    half decodedAndNoiseBlendedValue;
    half exponent;
    bool hasMask;
    half decodedMaskValue;
    half maskStrength;
    half maskMode;
};

struct NBFX_DissolvePreparedV3
{
    half valueForDebugAndSoftStep;
    half maskValueForLateMode;
};

struct NBFX_DissolveResolveInputV3
{
    NBFX_DissolvePreparedV3 prepared;
    half threshold;
    half softWidth;
    bool hasMask;
    half maskStrength;
    half maskMode;
};

struct NBFX_DissolveResolvedV3
{
    half valueBeforeSoftStep;
    half coverage;
};

struct NBFX_MaskCoverageInputV3
{
    half combinedMaskAfterNoise;
    bool refine;
    half3 refinePowMulAdd;
    half overallStrength;
};

NBFX_DissolvePreparedV3 NBFX_PrepareDissolveV3(NBFX_DissolvePrepareInputV3 input);
NBFX_DissolveResolvedV3 NBFX_ResolveDissolveV3(NBFX_DissolveResolveInputV3 input);
half NBFX_ResolveMaskCoverageV3(NBFX_MaskCoverageInputV3 input);

#endif
