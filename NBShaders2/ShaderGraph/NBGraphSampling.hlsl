#ifndef NB_GRAPH_SAMPLING_INCLUDED
#define NB_GRAPH_SAMPLING_INCLUDED

// The two bits for each legacy wrap slot live 16 positions apart.
uint NBGraphMaskWrapMode(uint packedWrapFlags, uint bit)
{
    return ((packedWrapFlags & bit) != 0u ? 1u : 0u) |
        ((packedWrapFlags & (bit << 16)) != 0u ? 2u : 0u);
}

// Match ShaderLab's explicit per-feature wrap, linear filtering and LOD0
// selection. The texture asset's sampler is not the NB material protocol.
// Callers supply already transformed UV; do not apply scaleTranslate twice.
SamplerState sampler_linear_repeat;
SamplerState sampler_linear_clamp;
SamplerState sampler_linear_RepeatU_ClampV;
SamplerState sampler_linear_ClampU_RepeatV;

half4 NBGraphSampleRawMap(UnityTexture2D map, float2 uv,
    uint wrapMode, bool forceLod0)
{
    half4 sampled;
#if defined(SHADER_TARGET_GLSL) || defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)
    if (wrapMode == 0u) uv = frac(uv);
    else if (wrapMode == 1u) uv = saturate(uv);
    else if (wrapMode == 2u) uv = float2(frac(uv.x), saturate(uv.y));
    else uv = float2(saturate(uv.x), frac(uv.y));
    UNITY_BRANCH
    if (forceLod0) sampled = SAMPLE_TEXTURE2D_LOD(map.tex, sampler_linear_clamp, uv, 0);
    else sampled = SAMPLE_TEXTURE2D(map.tex, sampler_linear_clamp, uv);
#else
    if (wrapMode == 0u)
    {
        UNITY_BRANCH
        if (forceLod0) sampled = SAMPLE_TEXTURE2D_LOD(map.tex, sampler_linear_repeat, uv, 0);
        else sampled = SAMPLE_TEXTURE2D(map.tex, sampler_linear_repeat, uv);
    }
    else if (wrapMode == 1u)
    {
        UNITY_BRANCH
        if (forceLod0) sampled = SAMPLE_TEXTURE2D_LOD(map.tex, sampler_linear_clamp, uv, 0);
        else sampled = SAMPLE_TEXTURE2D(map.tex, sampler_linear_clamp, uv);
    }
    else if (wrapMode == 2u)
    {
        UNITY_BRANCH
        if (forceLod0) sampled = SAMPLE_TEXTURE2D_LOD(map.tex, sampler_linear_RepeatU_ClampV, uv, 0);
        else sampled = SAMPLE_TEXTURE2D(map.tex, sampler_linear_RepeatU_ClampV, uv);
    }
    else
    {
        UNITY_BRANCH
        if (forceLod0) sampled = SAMPLE_TEXTURE2D_LOD(map.tex, sampler_linear_ClampU_RepeatV, uv, 0);
        else sampled = SAMPLE_TEXTURE2D(map.tex, sampler_linear_ClampU_RepeatV, uv);
    }
#endif
    return sampled;
}

// Fragment texture properties retain SG's explicit HDR decode. VertexOffset
// needs the original raw RGB displacement and calls the raw sampler directly.
half4 NBGraphSampleMap(UnityTexture2D map, float2 uv, uint wrapMode, bool forceLod0)
{
    half4 sampled = NBGraphSampleRawMap(map, uv, wrapMode, forceLod0);
    if (map.hdrDecode.x > 0.0)
        sampled = (half4)DecodeHDRSample(sampled, map.hdrDecode);
    return sampled;
}

#endif
