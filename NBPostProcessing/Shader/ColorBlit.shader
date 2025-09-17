Shader "XuanXuan/ColorBlit"
{
    HLSLINCLUDE
    #pragma target 2.0
    #pragma editor_sync_compilation
    #pragma multi_compile _ DISABLE_TEXTURE2D_X_ARRAY
    #pragma multi_compile _ BLIT_SINGLE_SLICE
    // Core.hlsl for XR dependencies
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
    ENDHLSL

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"
        }
               ZWrite Off Cull Off
        Pass
        {
            Name "ColorBlitPass0"

            HLSLPROGRAM
            #pragma vertex vert
            // #pragma vertex vert
            // #pragma fragment FragNearest
            #pragma fragment frag
            #pragma enable_d3d11_debug_symbols

            Texture2D _CameraTexture;
            // Texture2D _CameraTexture; 
            //
            // struct MyAttributes
            // {
            //     float4 positionOS : POSITION;
            //     float4 texCoord :   TEXCOORD0;
            // };

            //
            // Varyings vert(MyAttributes IN)
            // {
            //     Varyings OUT = (Varyings)0;
            //     OUT.positionCS = float4(IN.positionOS.xyz,1);
            //     half4 clipVertex = OUT.positionCS/OUT.positionCS.w;
            //     OUT.texcoord = ComputeScreenPos(clipVertex);
            //     return OUT;
            // }

            Varyings vert(Attributes input)//避开官方_BlitScaleBias为0的错误。
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

            #if SHADER_API_GLES
                float4 pos = input.positionOS;
                float2 uv  = input.uv;
            #else
                float4 pos = GetFullScreenTriangleVertexPosition(input.vertexID);
                float2 uv  = GetFullScreenTriangleTexCoord(input.vertexID);
            #endif

                output.positionCS = pos;
                // output.texcoord   = uv * _BlitScaleBias.xy + _BlitScaleBias.zw;
                output.texcoord   = uv;
                return output;
            }
            
            half4 frag (Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
 
                float4 color = SAMPLE_TEXTURE2D_X(_CameraTexture, sampler_LinearRepeat, input.texcoord);
                
                return color;
            }
            ENDHLSL
        }
        Pass
        {
            Name "BoxDownsample"

            ZWrite Off
            Cull Off

            HLSLPROGRAM
            // #pragma vertex Vert
            #pragma vertex vert
            #pragma fragment FragBoxDownsample
            #pragma enable_d3d11_debug_symbols


            Texture2D _CameraTexture;
            SAMPLER(sampler_CameraTexture);
            
            #if UNITY_VERSION < 202320
            float4 _BlitTexture_TexelSize;
            #endif

            Varyings vert(Attributes input)//避开官方_BlitScaleBias为0的错误。
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

            #if SHADER_API_GLES
                float4 pos = input.positionOS;
                float2 uv  = input.uv;
            #else
                float4 pos = GetFullScreenTriangleVertexPosition(input.vertexID);
                float2 uv  = GetFullScreenTriangleTexCoord(input.vertexID);
            #endif

                output.positionCS = pos;
                // output.texcoord   = uv * _BlitScaleBias.xy + _BlitScaleBias.zw;
                output.texcoord   = uv;
                return output;
            }

            float _SampleOffset;

            half4 FragBoxDownsample(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);
                float4 d = _BlitTexture_TexelSize.xyxy * float4(-_SampleOffset, -_SampleOffset, _SampleOffset,
                                                                _SampleOffset);

                half4 s;
                
                s = SAMPLE_TEXTURE2D_X(_CameraTexture, sampler_CameraTexture, uv + d.xy);
                s += SAMPLE_TEXTURE2D_X(_CameraTexture, sampler_CameraTexture, uv + d.zy);
                s += SAMPLE_TEXTURE2D_X(_CameraTexture, sampler_CameraTexture, uv + d.xw);
                s += SAMPLE_TEXTURE2D_X(_CameraTexture, sampler_CameraTexture, uv + d.zw);

                return s * 0.25h;
            }
            ENDHLSL
        }
        Pass
        {
            Name "ColorBlitPass_URP_13_1_2_OR_OLDER"

            HLSLPROGRAM
            #pragma vertex vert
            // #pragma vertex vert
            // #pragma fragment FragNearest
            #pragma fragment frag
            #pragma enable_d3d11_debug_symbols

            Texture2D _CameraTexture;
            // Texture2D _CameraTexture; 

            struct MyAttributes
            {
                float4 positionOS : POSITION;
                float4 texCoord :   TEXCOORD0;
            };

           
            Varyings vert(MyAttributes IN)
            {
                Varyings OUT = (Varyings)0;
                OUT.positionCS = float4(IN.positionOS.xyz,1);
                half4 clipVertex = OUT.positionCS/OUT.positionCS.w;
                OUT.texcoord = ComputeScreenPos(clipVertex);
                return OUT;
            }
            half4 frag (Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
 
                float4 color = SAMPLE_TEXTURE2D_X(_CameraTexture, sampler_LinearRepeat, input.texcoord);
                
                return color;
            }
            ENDHLSL
        }
        Pass
        {
            Name "BoxDownsample_URP_13_1_2_OR_OLDER"

            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment FragBoxDownsample
            #pragma enable_d3d11_debug_symbols


            Texture2D _CameraTexture;
            SAMPLER(sampler_CameraTexture);
            
            #if UNITY_VERSION < 202320
            float4 _BlitTexture_TexelSize;
            #endif
            

            float _SampleOffset;
            struct MyAttributes
            {
                float4 positionOS : POSITION;
                float4 texCoord :   TEXCOORD0;
            };

            Varyings vert(MyAttributes IN)
            {
                Varyings OUT = (Varyings)0;
                OUT.positionCS = float4(IN.positionOS.xyz,1);
                half4 clipVertex = OUT.positionCS/OUT.positionCS.w;
                OUT.texcoord = ComputeScreenPos(clipVertex);
                return OUT;
            }

            half4 FragBoxDownsample(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);
                float4 d = _BlitTexture_TexelSize.xyxy * float4(-_SampleOffset, -_SampleOffset, _SampleOffset,
                                                                _SampleOffset);

                half4 s;
                
                s = SAMPLE_TEXTURE2D_X(_CameraTexture, sampler_CameraTexture, uv + d.xy);
                s += SAMPLE_TEXTURE2D_X(_CameraTexture, sampler_CameraTexture, uv + d.zy);
                s += SAMPLE_TEXTURE2D_X(_CameraTexture, sampler_CameraTexture, uv + d.xw);
                s += SAMPLE_TEXTURE2D_X(_CameraTexture, sampler_CameraTexture, uv + d.zw);

                return s * 0.25h;
            }
            ENDHLSL
        }
    }
}