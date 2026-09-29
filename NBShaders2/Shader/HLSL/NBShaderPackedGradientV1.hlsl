#ifndef NB_SHADER_PACKED_GRADIENT_V1
#define NB_SHADER_PACKED_GRADIENT_V1

// Pure alpha-key evaluator moved intact from NBShaderInput.hlsl so ShaderLab
// and Shader Graph can use one implementation. SmoothStep01 is supplied by the
// existing utility include, as it was on the ShaderLab path.
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.xuanxuan.nb.fx/XuanXuanRenderUtility/Shader/HLSL/XuanXuan_Utility.hlsl"

    half GetPackedGradientAlphaKey(half4 pack0, half4 pack1, half4 pack2, int index)
    {
        if (index <= 0) return pack0.x;
        if (index == 1) return pack0.z;
        if (index == 2) return pack1.x;
        if (index == 3) return pack1.z;
        if (index == 4) return pack2.x;
        return pack2.z;
    }

    half GetPackedGradientAlphaTime(half4 pack0, half4 pack1, half4 pack2, int index)
    {
        if (index <= 0) return pack0.y;
        if (index == 1) return pack0.w;
        if (index == 2) return pack1.y;
        if (index == 3) return pack1.w;
        if (index == 4) return pack2.y;
        return pack2.w;
    }

    int2 GetPackedGradientAlphaIndex(half4 pack0, half4 pack1, half4 pack2, int arrCount, half gradientTime)
    {
        int2 indexes = int2(-1, 0);
        bool found = false;
        [unroll]
        for (int i = 0; i < 6; i++)
        {
            if (!found && i < arrCount && GetPackedGradientAlphaTime(pack0, pack1, pack2, i) > gradientTime)
            {
                indexes = int2(i - 1, i);
                found = true;
            }
        }
        return indexes;
    }

    half SamplePackedGradientAlpha(half4 pack0, half4 pack1, half4 pack2, int arrCount, half gradientTime)
    {
        if (gradientTime <= GetPackedGradientAlphaTime(pack0, pack1, pack2, 0))
        {
            return GetPackedGradientAlphaKey(pack0, pack1, pack2, 0);
        }
        else if (gradientTime >= GetPackedGradientAlphaTime(pack0, pack1, pack2, arrCount - 1))
        {
            return GetPackedGradientAlphaKey(pack0, pack1, pack2, arrCount - 1);
        }
        else
        {
            int2 indexes = GetPackedGradientAlphaIndex(pack0, pack1, pack2, arrCount, gradientTime);
            half smallVal = GetPackedGradientAlphaTime(pack0, pack1, pack2, indexes.x);
            half bigVal = GetPackedGradientAlphaTime(pack0, pack1, pack2, indexes.y);
            half interval = SmoothStep01((gradientTime - smallVal) / (bigVal - smallVal));
            half alpha = lerp(
                GetPackedGradientAlphaKey(pack0, pack1, pack2, indexes.x),
                GetPackedGradientAlphaKey(pack0, pack1, pack2, indexes.y),
                interval);
            alpha *= alpha;
            return alpha;
        }
    }

// Pure packed color-key evaluator moved intact from NBShaderInput.hlsl.
    half3 GetPackedGradientColorKey(half4 key0, half4 key1, half4 key2, half4 key3, half4 key4, half4 key5, int index)
    {
        if (index <= 0) return key0.rgb;
        if (index == 1) return key1.rgb;
        if (index == 2) return key2.rgb;
        if (index == 3) return key3.rgb;
        if (index == 4) return key4.rgb;
        return key5.rgb;
    }

    half GetPackedGradientColorTime(half4 key0, half4 key1, half4 key2, half4 key3, half4 key4, half4 key5, int index)
    {
        if (index <= 0) return key0.a;
        if (index == 1) return key1.a;
        if (index == 2) return key2.a;
        if (index == 3) return key3.a;
        if (index == 4) return key4.a;
        return key5.a;
    }

    int2 GetPackedGradientColorIndex(half4 key0, half4 key1, half4 key2, half4 key3, half4 key4, half4 key5, int arrCount, half gradientTime)
    {
        int2 indexes = int2(-1, 0);
        bool found = false;
        [unroll]
        for (int i = 0; i < 6; i++)
        {
            if (!found && i < arrCount && GetPackedGradientColorTime(key0, key1, key2, key3, key4, key5, i) > gradientTime)
            {
                indexes = int2(i - 1, i);
                found = true;
            }
        }
        return indexes;
    }

    half3 SamplePackedGradientColor(half4 key0, half4 key1, half4 key2, half4 key3, half4 key4, half4 key5, int arrCount, half gradientTime)
    {
        if (gradientTime <= GetPackedGradientColorTime(key0, key1, key2, key3, key4, key5, 0))
        {
            return GetPackedGradientColorKey(key0, key1, key2, key3, key4, key5, 0);
        }
        else if (gradientTime >= GetPackedGradientColorTime(key0, key1, key2, key3, key4, key5, arrCount - 1))
        {
            return GetPackedGradientColorKey(key0, key1, key2, key3, key4, key5, arrCount - 1);
        }
        else
        {
            int2 indexes = GetPackedGradientColorIndex(key0, key1, key2, key3, key4, key5, arrCount, gradientTime);
            half smallVal = GetPackedGradientColorTime(key0, key1, key2, key3, key4, key5, indexes.x);
            half bigVal = GetPackedGradientColorTime(key0, key1, key2, key3, key4, key5, indexes.y);
            half interval = SmoothStep01((gradientTime - smallVal) / (bigVal - smallVal));
            return lerp(
                GetPackedGradientColorKey(key0, key1, key2, key3, key4, key5, indexes.x),
                GetPackedGradientColorKey(key0, key1, key2, key3, key4, key5, indexes.y),
                interval);
        }
    }

#endif
