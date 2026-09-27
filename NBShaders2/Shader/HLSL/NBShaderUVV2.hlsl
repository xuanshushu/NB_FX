#ifndef NB_SHADER_UV_V2
#define NB_SHADER_UV_V2

// Core precedes Utility for its Unity shader types/macros. This module uses
// only the supplied input; stage selection and sampling remain with the host.
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.xuanxuan.nb.fx/XuanXuanRenderUtility/Shader/HLSL/XuanXuan_Utility.hlsl"
#include "NBShaderSharedContractV2.hlsl"

float2 NBFX_TransformFeatureUVV2(NBFX_FeatureUVTransformInputV2 input)
{
    float2 uv = input.originUV;
    uv = Rotate_Radians_float(uv, input.rotationCenter, input.rotationDegrees);
    uv = uv * input.scaleOffset.xy + input.scaleOffset.zw;
    uv = UVOffsetAnimaiton(uv, (half2)input.offsetSpeed, input.timeY);
    return uv;
}

#endif
