Shader "Hidden/NBFX/DN0StencilProbe20b479c591644d47b150c1b2b264e4e7" { Properties { _WriterRef("Writer Ref", Float)=200 } SubShader { Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" "RenderType"="Opaque" } Pass { Tags { "LightMode"="UniversalForward" } Cull Off ZWrite Off ZTest Always Blend One Zero ColorMask RGBA Stencil { Ref 200 Comp Equal Pass Keep Fail Keep ZFail Keep ReadMask 255 WriteMask 0 } HLSLPROGRAM
#pragma target 4.5
#pragma vertex Vert
#pragma fragment Frag
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
struct Attributes { float4 positionOS:POSITION; }; struct Varyings {float4 positionCS:SV_POSITION;};Varyings Vert(Attributes a){Varyings o;o.positionCS=TransformObjectToHClip(a.positionOS.xyz);return o;}half4 Frag(Varyings v):SV_Target{return half4(1,0,1,1);}
ENDHLSL
} } }