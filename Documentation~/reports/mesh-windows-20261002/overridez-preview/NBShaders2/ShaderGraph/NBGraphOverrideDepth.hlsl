#ifndef NB_GRAPH_OVERRIDE_DEPTH_INCLUDED
#define NB_GRAPH_OVERRIDE_DEPTH_INCLUDED
#include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderOverrideDepthV1.hlsl"
void NBGraphOverrideDepth_float(float OverrideZToggle,float OverrideZValue,
    float4 PixelPosition,out float DeviceDepth)
{
    DeviceDepth=PixelPosition.z;
    if(OverrideZToggle>0.5)
        DeviceDepth=NBFX_OverrideZDeviceDepthV1(OverrideZValue,_ProjectionParams,_ZBufferParams,unity_OrthoParams.w);
}
#endif
