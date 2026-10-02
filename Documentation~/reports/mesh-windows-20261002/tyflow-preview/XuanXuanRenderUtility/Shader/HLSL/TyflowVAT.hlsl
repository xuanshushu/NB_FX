#ifndef TYFLOW_VAT_INCLUDED
#define TYFLOW_VAT_INCLUDED
#include "Packages/com.xuanxuan.nb.fx/XuanXuanRenderUtility/Shader/HLSL/TyflowVATKernelV1.hlsl"
void ApplyTyflowVAT(AttributesParticle input, inout float4 positionOS, inout float3 normalOS)
{
    TVAT_ConfigV1 config;
    config.mode = -1;
#if defined(_TYFLOW_VAT_ABSOLUTE)
    config.mode = 0;
#elif defined(_TYFLOW_VAT_RELATIVE)
    config.mode = 1;
#elif defined(_TYFLOW_VAT_SKIN_R)
    config.mode = 2;
#elif defined(_TYFLOW_VAT_SKIN_PR)
    config.mode = 3;
#elif defined(_TYFLOW_VAT_SKIN_PRSAVE)
    config.mode = 4;
#elif defined(_TYFLOW_VAT_SKIN_PRSXYZ)
    config.mode = 5;
#endif
    if (config.mode < 0) return;
#if defined(SHADOWS_DEPTH)
    config.shadows = true;
#else
    config.shadows = false;
#endif
    float4 uv3 = 0;
#if defined(_FLIPBOOKBLENDING_ON)
    config.flipbook = 1;
#else
    config.flipbook = 0;
    uv3 = float4(input.vatTexcoord4,0,0);
#endif
    config.timeY = time;
    config._AffectsShadows = _AffectsShadows;
    config._Autoplay = _Autoplay;
    config._AutoplaySpeed = _AutoplaySpeed;
    config._DeformingSkin = _DeformingSkin;
    config._Frame = _Frame;
    config._FrameInterpolation = _FrameInterpolation;
    config._Frames = _Frames;
    config._ImportScale = _ImportScale;
    config._InterpolateLoop = _InterpolateLoop;
    config._LinearToGamma = _LinearToGamma;
    config._Loop = _Loop;
    config._RGBAEncoded = _RGBAEncoded;
    config._RGBAHalf = _RGBAHalf;
    config._SkinBoneCount = _SkinBoneCount;
    config._VATIncludesNormals = _VATIncludesNormals;
    config._VATTex_TexelSize = _VATTex_TexelSize;
    float frameCustomData = GetCustomData(NB_CUSTOM_DATA_FLAG_2, FLAGBIT_POS_2_CUSTOMDATA_VAT_FRAME,-1.0f,input.Custom1,input.Custom2);
    float3 animatedPositionOS = positionOS.xyz;
    TVAT_ApplyModesV1(config,TEXTURE2D_ARGS(_VATTex,sampler_point_clamp),
        input.texcoords,input.Custom1,input.Custom2,uv3,float4(input.vatTexcoord5,0,0),
        float4(input.vatTexcoord6,0,0),float4(input.vatTexcoord7,0,0),float4(input.vatTexcoord8,0,0),
        CheckLocalFlags1(FLAG_BIT_PARTICLE_1_IS_PARTICLE_SYSTEM),frameCustomData,animatedPositionOS,normalOS);
    positionOS.xyz = animatedPositionOS;
}
#endif
