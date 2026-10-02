#ifndef NB_GRAPH_VAT_SOFTBODY_INCLUDED
#define NB_GRAPH_VAT_SOFTBODY_INCLUDED
#include "Packages/com.xuanxuan.nb.fx/XuanXuanRenderUtility/Shader/HLSL/HoudiniVATKernelV1.hlsl"
#include "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBGraphFlags.hlsl"
#include "Packages/com.xuanxuan.nb.fx/XuanXuanRenderUtility/Shader/HLSL/TyflowVATKernelV1.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
SamplerState NBGraphVAT_point_clamp_sampler;

// Ordinary Mesh VAT adapter: existing Houdini and Tyflow modes.
// Both exact NB screen passes retain their static VAT exclusion.
void NBGraphVATSoftBody_float(float3 PositionOS, float3 NormalOS, float4 UV1,
    UnityTexture2D PosTexture, UnityTexture2D PosTexture2,
    UnityTexture2D RotTexture, float VATToggle, float VATMode,
    float HoudiniVATSubMode, float AutoPlayback,
    float GameTimeAtFirstFrame, float PlaybackSpeed, float HoudiniFPS,
    float DisplayFrame, float FrameCount,
    float BoundMinX, float BoundMinY, float BoundMinZ,
    float BoundMaxX, float BoundMaxY, float BoundMaxZ,
    float LoadPosTwoTex, float UnloadRotTex,
    float Flags1Lo16, float Flags1Hi16,
    float CustomDataFlag2Lo16,
    float CustomDataFlag2Hi16,
    float4 UV0, float4 Custom2,
    UnityTexture2D ColTexture, UnityTexture2D LookupTable, float4 UV4,
    float Interpolate,
    float AnimateFirstFrame,
    float GlobalPscaleMul,
    float PscaleAreInPosA,
    float WidthBaseScale,
    float HeightBaseScale,
    float HideOverlappingOrigin,
    float OriginRadius,
    float CanSpin,
    float SpinFromHeading,
    float SpinPhase,
    float ScaleByVelAmount,
    float LoadColTex,
    UnityTexture2D VATTexture, float4 UV3, float4 UV5, float4 UV6, float4 UV7,
    float FlipbookToggle,
    float TyFlowVATSubMode,
    float DeformingSkin,
    float SkinBoneCount,
    float RGBAEncoded,
    float RGBAHalf,
    float LinearToGamma,
    float VATIncludesNormals,
    float ImportScale,
    float AffectsShadows,
    float Frame,
    float Frames,
    float Autoplay,
    float AutoplaySpeed,
    float Loop,
    float InterpolateLoop,
    float FrameInterpolation,
    out float3 OutPositionOS, out float3 OutNormalOS, out float Supported)
{
    OutPositionOS=PositionOS; OutNormalOS=NormalOS; Supported=1.0;
#if defined(NB_GRAPH_NO_VAT)
    return;
#else
    if (VATToggle < 0.5) return;
    uint flags1=NBGraphDecodeUInt32(Flags1Lo16,Flags1Hi16);
    if (round(VATMode) == 1.0)
    {
        if (TyFlowVATSubMode < 0.0 || TyFlowVATSubMode > 5.0) { Supported=0.0; return; }
        TVAT_ConfigV1 config;
        config.mode = (int)round(TyFlowVATSubMode);
#if defined(SHADERPASS) && ((SHADERPASS == SHADERPASS_DEPTHONLY) || (SHADERPASS == SHADERPASS_SHADOWCASTER))
        config.shadows = true;
#else
        config.shadows = false;
#endif
        config.flipbook = FlipbookToggle;
        config.timeY = _Time.y;
        config._VATTex_TexelSize = VATTexture.texelSize;
        config._DeformingSkin = DeformingSkin;
        config._SkinBoneCount = SkinBoneCount;
        config._RGBAEncoded = RGBAEncoded;
        config._RGBAHalf = RGBAHalf;
        config._LinearToGamma = LinearToGamma;
        config._VATIncludesNormals = VATIncludesNormals;
        config._ImportScale = ImportScale;
        config._AffectsShadows = AffectsShadows;
        config._Frame = Frame;
        config._Frames = Frames;
        config._Autoplay = Autoplay;
        config._AutoplaySpeed = AutoplaySpeed;
        config._Loop = Loop;
        config._InterpolateLoop = InterpolateLoop;
        config._FrameInterpolation = FrameInterpolation;
        float frameCustomData=GetCustomData(NBGraphDecodeUInt32(CustomDataFlag2Lo16,CustomDataFlag2Hi16),FLAGBIT_POS_2_CUSTOMDATA_VAT_FRAME,-1.0,UV1,Custom2);
        TVAT_ApplyModesV1(config,TEXTURE2D_ARGS(VATTexture.tex,NBGraphVAT_point_clamp_sampler),
            UV0,UV1,Custom2,UV3,UV4,UV5,UV6,UV7,
            (flags1 & FLAG_BIT_PARTICLE_1_IS_PARTICLE_SYSTEM)!=0u,frameCustomData,OutPositionOS,OutNormalOS);
        return;
    }
    if (round(VATMode)!=0.0 || HoudiniVATSubMode<0.0 || HoudiniVATSubMode>3.0)
    { Supported=0.0; return; }
    float selectedFrame, frameAlpha;
    HVAT_ComputeMeshFrameSelectionV1(FrameCount, _Time.y,
        GameTimeAtFirstFrame,HoudiniFPS,PlaybackSpeed,AutoPlayback,
        DisplayFrame,selectedFrame,frameAlpha);
    bool particle=(flags1 & FLAG_BIT_PARTICLE_1_IS_PARTICLE_SYSTEM)!=0u;
    if (particle)
    {
        float customFrame=GetCustomData(NBGraphDecodeUInt32(CustomDataFlag2Lo16,CustomDataFlag2Hi16),FLAGBIT_POS_2_CUSTOMDATA_VAT_FRAME,-1.0,UV1,Custom2);
        if (customFrame>=0.0)
        {
            float chosen=saturate(customFrame)*max(FrameCount-1.0,0.0)+1.0;
            selectedFrame=floor(chosen);frameAlpha=frac(chosen);
        }
    }
    HVAT_ConfigV1 config;
    config._B_CAN_SPIN = CanSpin;
    config._B_LOAD_COL_TEX = LoadColTex;
    config._B_LOAD_POS_TWO_TEX = LoadPosTwoTex;
    config._B_UNLOAD_ROT_TEX = UnloadRotTex;
    config._B_hideOverlappingOrigin = HideOverlappingOrigin;
    config._B_interpolate = Interpolate;
    config._B_pscaleAreInPosA = PscaleAreInPosA;
    config._B_spinFromHeading = SpinFromHeading;
    config._animateFirstFrame = AnimateFirstFrame;
    config._boundMaxX = BoundMaxX;
    config._boundMaxY = BoundMaxY;
    config._boundMaxZ = BoundMaxZ;
    config._boundMinX = BoundMinX;
    config._boundMinY = BoundMinY;
    config._boundMinZ = BoundMinZ;
    config._frameCount = FrameCount;
    config._globalPscaleMul = GlobalPscaleMul;
    config._heightBaseScale = HeightBaseScale;
    config._originRadius = OriginRadius;
    config._scaleByVelAmount = ScaleByVelAmount;
    config._spinPhase = SpinPhase;
    config._widthBaseScale = WidthBaseScale;
    HVAT_ApplyModesV1((int)round(HoudiniVATSubMode),particle,UV0,UV1,Custom2,UV4,
        selectedFrame,frameAlpha,config,
        TEXTURE2D_ARGS(PosTexture.tex,PosTexture.samplerstate),
        TEXTURE2D_ARGS(PosTexture2.tex,PosTexture2.samplerstate),
        TEXTURE2D_ARGS(RotTexture.tex,RotTexture.samplerstate),
        TEXTURE2D_ARGS(ColTexture.tex,ColTexture.samplerstate),
        TEXTURE2D_ARGS(LookupTable.tex,LookupTable.samplerstate),
        OutPositionOS,OutNormalOS);
#endif
}

// VertexDescriptionInputs WorldSpace N/T/B are built from original Attributes
// before VertexDescription.Position/Normal blocks are assigned. Feed the
// actual post-VAT normal to SixWay vertex SH; VAT-off is exact passthrough.
void NBGraphVATWorldBasis_float(float3 VATNormalOS, float3 RawNormalWS,
    float3 RawTangentWS, float3 RawBitangentWS, float VATToggle,
    float Supported, out float3 NormalWS, out float3 TangentWS,
    out float3 BitangentWS)
{
    NormalWS=RawNormalWS; TangentWS=RawTangentWS;
    BitangentWS=RawBitangentWS;
#if !defined(NB_GRAPH_NO_VAT)
    if (VATToggle < 0.5 || Supported < 0.5) return;
    NormalWS=TransformObjectToWorldNormal(VATNormalOS);
    float originalSign=dot(cross(RawNormalWS,RawTangentWS),RawBitangentWS)<0 ? -1.0 : 1.0;
    BitangentWS=originalSign*cross(NormalWS,TangentWS);
#endif
}
#endif
