Shader "XuanXuan/Disturbance"
{
    Properties
    {
        [MhGroup(Main)]_mainTex("Main", float) = 0
        [MhTexture(Main)] _MaskMap("Mask Map", 2D) = "white" {}

        [MhTexture(Main,_Strength,on)]_NoiseMap("Noise Map", 2D) = "white"{}
        [HideInInspector]_Strength("Strength", Range(-0.2,0.2)) =0.1
        [MhToggleKeyword(Main,_PARTICLE_CUSTOMDATA_ON)]_ParticleCustomDataOn("Particle customData Strength", float) = 0
        [HideInInspector]_SurfaceType("surfaceType",float)=1
        [HideInInspector] _QueueOffset("Queue offset", Float) = 0.0
    }

    // The SubShader block containing the Shader code.
    SubShader
    {
        // SubShader Tags define when and under which conditions a SubShader block or
        // a pass is executed.
        Tags
        {
            "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "IgnoreProjector" = "True"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            //ZTest Always
            Cull Off
            HLSLPROGRAM
            //gpuInstancing on
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #pragma target 3.5 DOTS_INSTANCING_ON

            #pragma shader_feature_local_fragment _PARTICLE_CUSTOMDATA_ON
            #pragma enable_d3d11_debug_symbols
            

            // This line defines the name of the vertex shader.
            #pragma vertex vert
            // This line defines the name of the fragment shader.
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"


            // This example uses the Attributes structure as an input structure in
            // the vertex shader.
            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                // The positions in this struct must have the SV_POSITION semantic.
                float4 positionHCS : SV_POSITION;
                float4 uv :TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            Texture2D _NoiseMap;
            SAMPLER(sampler_NoiseMap);
            
            Texture2D _MaskMap;
            SAMPLER(sampler_MaskMap);
            float _Strength;

            CBUFFER_START(UnityPerMaterial)

            float4 _NoiseMap_ST;

            CBUFFER_END


            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                // Declaring the output object (OUT) with the Varyings struct.

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                half2 screenSpaceUV = IN.positionHCS.xy / _ScaledScreenParams.xy;
                const half2 noiseUV = screenSpaceUV * _NoiseMap_ST.xy + _NoiseMap_ST.zw;

                half noise = SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, noiseUV).r * 2 - 1;
                half mask = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, IN.uv.xy).r;
                noise = lerp(0, noise, mask);
                
                //noise = (noise * _Strength * 5) * 0.5 + 0.5;
                noise = (noise * _Strength) * 1.25 + 0.5;
                
                return half4(noise.xxx, 1.0);
            }
            ENDHLSL
        }
    }
    customEditor "ShaderEditor.MhBaseShaderGUI"
}