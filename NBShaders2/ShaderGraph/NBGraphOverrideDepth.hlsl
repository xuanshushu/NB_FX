#ifndef NB_GRAPH_OVERRIDE_DEPTH_INCLUDED
#define NB_GRAPH_OVERRIDE_DEPTH_INCLUDED
#include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderOverrideDepthV1.hlsl"
void NBGraphOverrideDepth_float(float OverrideZToggle,float OverrideZValue,
    float4 PixelPosition,out float DeviceDepth)
{
    // The existing keyword owns depth output, as in the original ShaderLab.
    // Keep the serialized slot interface; the disabled variant has no SV_Depth.
    DeviceDepth=0;
#if defined(_OVERRIDE_Z)
    DeviceDepth=NBFX_OverrideZDeviceDepthV1(OverrideZValue,_ProjectionParams,_ZBufferParams,unity_OrthoParams.w);
#endif
}
#endif
