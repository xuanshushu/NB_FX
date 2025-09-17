Shader "XuanXuan/FullScreenDisturbance"
{
    Properties
    {
        _Strength("Strength", Range(-0.2,0.2)) =0
        _BaseMap("Base Map", 2D) = "white"
        //_Mask("Mask", 2D) = "white"

    }

    // The SubShader block containing the Shader code.
    SubShader
    {
        // SubShader Tags define when and under which conditions a SubShader block or
        // a pass is executed.
        Tags
        {
            "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {

            stencil
            {
                Ref 1
                Comp equal
                Pass keep
            }
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // This line defines the name of the vertex shader.
            #pragma vertex vert
            // This line defines the name of the fragment shader.
            #pragma fragment frag

            Texture2D _BlitTexture;
            SAMPLER(sampler_BlitTexture);
            Texture2D _BaseMap;
            SAMPLER(sampler_BaseMap);
            Texture2D _Mask;
            SAMPLER(simpler_Mask);
            float _Strength;

            struct Attributes
            {
                #if UNITY_ANY_INSTANCING_ENABLED
                   uint instanceID : INSTANCEID_SEMANTIC;
                #endif
                uint vertexID : VERTEXID_SEMANTIC;
            };

            struct Varyings
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                #if UNITY_ANY_INSTANCING_ENABLED
                uint instanceID : CUSTOM_INSTANCE_ID;
                #endif
            };


            Varyings vert(Attributes input)
            {
                Varyings output;
                output.position = GetFullScreenTriangleVertexPosition(input.vertexID, UNITY_NEAR_CLIP_VALUE);
                output.uv = output.position.xy * 0.5 + 0.5;
                return output;
            }

            float4 frag(Varyings packedInput):SV_TARGET
            {
                float2 baseUV = packedInput.position / _ScreenParams.xy;
                float3 noise = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, baseUV);
                noise=lerp(0,noise,noise.b);
                baseUV += noise.xy * _Strength;
                return SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, baseUV);
            }
            ENDHLSL
        }
    }

}