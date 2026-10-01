#ifndef NB_GRAPH_VAT_SOFTBODY_INCLUDED
#define NB_GRAPH_VAT_SOFTBODY_INCLUDED
#include "Packages/com.xuanxuan.nb.fx/XuanXuanRenderUtility/Shader/HLSL/HoudiniVATMathV1.hlsl"
#include "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBGraphFlags.hlsl"

// V0 only: ordinary Mesh Houdini SoftBody. NB screen passes are statically
// excluded; other VAT submodes are not replaced by a fake SoftBody result.
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
    out float3 OutPositionOS, out float3 OutNormalOS, out float Supported)
{
    OutPositionOS=PositionOS; OutNormalOS=NormalOS; Supported=1.0;
#if defined(NB_GRAPH_NO_VAT)
    return;
#else
    if (VATToggle < 0.5) return;
    uint flags1=NBGraphDecodeUInt32(Flags1Lo16,Flags1Hi16);
    if (round(VATMode)!=0.0 || round(HoudiniVATSubMode)!=0.0 ||
        (flags1 & FLAG_BIT_PARTICLE_1_IS_PARTICLE_SYSTEM)!=0u)
    { Supported=0.0; return; }
    float selectedFrame, frameAlpha;
    HVAT_ComputeMeshFrameSelectionV1(FrameCount, _Time.y,
        GameTimeAtFirstFrame,HoudiniFPS,PlaybackSpeed,AutoPlayback,
        DisplayFrame,selectedFrame,frameAlpha);
    float comparisonBoundMaxb=(frac(BoundMaxZ*10.0)>=0.5)?1.0:0.0;
    float oneMinusBoundMaxR=1.0-frac(BoundMaxX*(-10.0));
    float boundMinMul10z=BoundMinZ*10.0;
    float oneMinusBoundMinB=1.0-(ceil(boundMinMul10z)-boundMinMul10z);
    float pscaleDenom=max(1.0-frac(BoundMaxY*10.0),1e-5);
    float uv1r=UV1.x,uv1g=UV1.y;
    float multiplyBoundMinB=uv1r*oneMinusBoundMinB;
    float2 texUV=HVAT_VatUV(selectedFrame,uv1r,uv1g,
        oneMinusBoundMaxR,multiplyBoundMinB,FrameCount);
    float4 posSample=SAMPLE_TEXTURE2D_LOD(PosTexture.tex,PosTexture.samplerstate,texUV,0);
    float4 pos2=0;
    if (LoadPosTwoTex>0.5)
        pos2=SAMPLE_TEXTURE2D_LOD(PosTexture2.tex,PosTexture2.samplerstate,texUV,0);
    float4 rotSample=0;
    if (UnloadRotTex<=0.5)
        rotSample=SAMPLE_TEXTURE2D_LOD(RotTexture.tex,RotTexture.samplerstate,texUV,0);
    HVAT_ApplySoftBodySamplesV1(OutPositionOS,OutNormalOS,
        posSample,pos2,rotSample,LoadPosTwoTex,UnloadRotTex,
        comparisonBoundMaxb,float3(BoundMaxX,BoundMaxY,BoundMaxZ),
        float3(BoundMinX,BoundMinY,BoundMinZ));
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
