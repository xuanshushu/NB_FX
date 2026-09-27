// G2-only readback of NBPostProcess's transient RenderGraph mask.
Shader "Hidden/NBFX/G2MaskView"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "MaskView"
            ZTest Always ZWrite Off Cull Off
            Blend One Zero
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D_X(_DisturbanceMaskTex);

            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings Vert(uint vertexID : SV_VertexID)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(vertexID);
                output.uv = GetFullScreenTriangleTexCoord(vertexID);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                return half4(SAMPLE_TEXTURE2D_X(_DisturbanceMaskTex, sampler_LinearClamp, input.uv).xy, 0, 1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "CopyView"
            ZTest Always ZWrite Off Cull Off
            Blend One Zero
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D_X(_ScreenColorCopy1);

            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings Vert(uint vertexID : SV_VertexID)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(vertexID);
                output.uv = GetFullScreenTriangleTexCoord(vertexID);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                return SAMPLE_TEXTURE2D_X(_ScreenColorCopy1, sampler_LinearClamp, input.uv);
            }
            ENDHLSL
        }
    }
}
