#ifndef NB_SHADER_CHROMATIC_V1_INCLUDED
#define NB_SHADER_CHROMATIC_V1_INCLUDED

// Literal half-math from the original DistortionChoraticaberrat. Caller
// provides the packed WithNoise bit, pre-POM origin and final POM+Noise UV.
half2 NBFX_ChromaticDeltaV1(half2 originUV, half2 uvAfterNoise,
    half intensity, bool withNoise)
{
    half2 delta = half2(originUV.x * 2 - 1, 0);
    if (withNoise)
    {
        half2 noiseIntensity = uvAfterNoise - originUV;
        delta = noiseIntensity * intensity * 10;
    }
    else
    {
        delta *= intensity;
    }
    return delta;
}

// Old alpha is 0.5 of EACH of the three sample alphas, clamped; not /3.
half4 NBFX_ComposeChromaticV1(half2 ra, half2 ga, half2 ba)
{
    ra.r *= ra.y;
    ga.r *= ga.y;
    ba.r *= ba.y;
    return half4(ra.r, ga.r, ba.r,
        clamp(ra.y * 0.5 + ga.y * 0.5 + ba.y * 0.5, 0, 1));
}

#endif
