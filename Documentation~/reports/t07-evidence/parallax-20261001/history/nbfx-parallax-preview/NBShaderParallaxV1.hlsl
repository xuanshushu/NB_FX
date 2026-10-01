#ifndef NB_SHADER_PARALLAX_V1_INCLUDED
#define NB_SHADER_PARALLAX_V1_INCLUDED

// Both hosts declare these four named sampler states before this include.
// This is the original NB per-feature wrap/LOD sampler, not the texture
// asset's sampler or ShaderGraph's HDR-decoded sample.
half4 NBFX_SampleParallaxRawV1(Texture2D map, float2 uv,
    uint wrapMode, bool forceLod0)
{
#if defined(SHADER_TARGET_GLSL) || defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)
    switch (wrapMode)
    {
        case 0: uv = frac(uv); break;
        case 1: uv = saturate(uv); break;
        case 2: uv = float2(frac(uv.x), saturate(uv.y)); break;
        case 3: uv = float2(saturate(uv.x), frac(uv.y)); break;
    }
    half4 sampled;
    UNITY_BRANCH
    if (forceLod0) sampled = SAMPLE_TEXTURE2D_LOD(map, sampler_linear_clamp, uv, 0);
    else sampled = SAMPLE_TEXTURE2D(map, sampler_linear_clamp, uv);
    return sampled;
#else
    half4 sampled;
    switch (wrapMode)
    {
        case 0:
            UNITY_BRANCH
            if (forceLod0) sampled = SAMPLE_TEXTURE2D_LOD(map, sampler_linear_repeat, uv, 0);
            else sampled = SAMPLE_TEXTURE2D(map, sampler_linear_repeat, uv);
            break;
        case 1:
            UNITY_BRANCH
            if (forceLod0) sampled = SAMPLE_TEXTURE2D_LOD(map, sampler_linear_clamp, uv, 0);
            else sampled = SAMPLE_TEXTURE2D(map, sampler_linear_clamp, uv);
            break;
        case 2:
            UNITY_BRANCH
            if (forceLod0) sampled = SAMPLE_TEXTURE2D_LOD(map, sampler_linear_RepeatU_ClampV, uv, 0);
            else sampled = SAMPLE_TEXTURE2D(map, sampler_linear_RepeatU_ClampV, uv);
            break;
        case 3:
            UNITY_BRANCH
            if (forceLod0) sampled = SAMPLE_TEXTURE2D_LOD(map, sampler_linear_ClampU_RepeatV, uv, 0);
            else sampled = SAMPLE_TEXTURE2D(map, sampler_linear_ClampU_RepeatV, uv);
            break;
        default:
            UNITY_BRANCH
            if (forceLod0) sampled = SAMPLE_TEXTURE2D_LOD(map, sampler_linear_repeat, uv, 0);
            else sampled = SAMPLE_TEXTURE2D(map, sampler_linear_repeat, uv);
            break;
    }
    return sampled;
#endif
}

// Literal extraction of NBShaderInput.hlsl ParallaxOcclusionMapping's
// arithmetic, float/half parameters, [loop] and sample sequence.
// No clamp/safety branch is added to the legacy numerical contract.
float2 NBFX_ParallaxOcclusionMappingV1(Texture2D map,
    float2 texCoords, float3 viewDir, half4 mapST, half intensity,
    half4 layerVec, uint wrapMode, bool forceLod0)
{
    texCoords = texCoords * mapST + mapST.zw;
    const float minLayers = layerVec.x;
    const float maxLayers = layerVec.y;
    float numLayers = lerp(maxLayers, minLayers, abs(dot(half3(0.0, 0.0, 1.0), viewDir)));
    float layerDepth = 1.0 / numLayers;
    float currentLayerDepth = 0.0;
    float2 P = viewDir.xy / viewDir.z * intensity;
    float2 deltaTexCoords = P / numLayers;
    float2 currentTexCoords = texCoords;
    float currentDepthMapValue = NBFX_SampleParallaxRawV1(map, currentTexCoords, wrapMode, forceLod0).r;
    currentLayerDepth = clamp(currentLayerDepth, 0, 1);
    int i = 0;
    [loop]
    while (currentLayerDepth < currentDepthMapValue && i < numLayers)
    {
        currentTexCoords -= deltaTexCoords;
        currentDepthMapValue = NBFX_SampleParallaxRawV1(map, currentTexCoords, wrapMode, forceLod0).r;
        currentLayerDepth += layerDepth;
        i++;
    }
    float2 prevTexCoords = currentTexCoords + deltaTexCoords;
    float afterDepth = currentDepthMapValue - currentLayerDepth;
    float beforeDepth = NBFX_SampleParallaxRawV1(map, prevTexCoords, wrapMode, forceLod0).r - currentLayerDepth + layerDepth;
    float weight = afterDepth / (afterDepth - beforeDepth);
    float2 finalTexCoords = prevTexCoords * weight + currentTexCoords * (1.0 - weight);
    return finalTexCoords;
}

#endif
