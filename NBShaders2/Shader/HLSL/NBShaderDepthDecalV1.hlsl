#ifndef NB_SHADER_DEPTH_DECAL_V1
#define NB_SHADER_DEPTH_DECAL_V1

#include "Packages/com.xuanxuan.nb.fx/XuanXuanRenderUtility/Shader/HLSL/XuanXuan_Utility.hlsl"

// Only the original object-space cube/UV/alpha math. Both ShaderLab and SG
// hosts own their depth sample, world reconstruction and World->Object path.
struct NBFX_DepthDecalProjectionV1
{
    float2 uv;
    half alpha;
};

NBFX_DepthDecalProjectionV1 NBFX_ResolveDepthDecalV1(float3 fragobjectPos)
{
    float3 absFragObjectPos = abs(fragobjectPos);
    half clipValue = step(absFragObjectPos.x,0.5);
    clipValue *= step(absFragObjectPos.y,0.5);
    clipValue *= step(absFragObjectPos.z,0.5);
    half decalAlpha = NB_Remap (abs(fragobjectPos.y),0.1,0.5,1,0);
    decalAlpha = decalAlpha*decalAlpha;
    decalAlpha *= clipValue;
    NBFX_DepthDecalProjectionV1 result;
    result.uv = fragobjectPos.xz + 0.5;
    result.alpha = decalAlpha;
    return result;
}

#endif
