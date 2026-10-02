#ifndef NB_SHADER_OVERRIDE_DEPTH_V1_INCLUDED
#define NB_SHADER_OVERRIDE_DEPTH_V1_INCLUDED
// Original NB device-depth calculation and clamp order.
float NBFX_OverrideZDeviceDepthV1(float overrideValue,float4 projectionParams,
    float4 zBufferParams,float orthographic)
{
            float nearClip = projectionParams.y;
            float farClip = projectionParams.z;
            float eyeDepth = clamp(overrideValue, nearClip, farClip);

            if (orthographic == 0)
            {
                float reciprocalEyeDepth = rcp(max(eyeDepth, 1e-6));
                return saturate((reciprocalEyeDepth - zBufferParams.w) / zBufferParams.z);
            }

            float linearDepth = saturate((eyeDepth - nearClip) / max(farClip - nearClip, 1e-6));
            #if UNITY_REVERSED_Z
                return 1.0 - linearDepth;
            #else
                return linearDepth;
            #endif

}
#endif
