#ifndef NB_GRAPH_CUSTOM_LOCAL_SPACE_INCLUDED
#define NB_GRAPH_CUSTOM_LOCAL_SPACE_INCLUDED
#include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderCoordinateSpaceV1.hlsl"
// Keep regular SG material branches byte-compatible. Custom mode receives
// world-space simulation attributes, exactly like the existing NB host.
float3 NBGraphLocalToWorldPositionV1(float3 position,float enabled,
    float4 row0,float4 row1,float4 row2,float4 row3)
{
    if(enabled<0.5)return TransformObjectToWorld(position);
    return NBFX_MatrixLocalToWorldPositionV1(position,float4x4(row0,row1,row2,row3));
}
float3 NBGraphWorldToLocalPositionV1(float3 position,float enabled,
    float4 row0,float4 row1,float4 row2,float4 row3)
{
    if(enabled<0.5)return TransformWorldToObject(position);
    return NBFX_MatrixWorldToLocalPositionV1(position,float4x4(row0,row1,row2,row3));
}
float4 NBGraphLocalToHClipV1(float3 position,float enabled,
    float4 row0,float4 row1,float4 row2,float4 row3)
{
    if(enabled<0.5)return TransformObjectToHClip(position);
    return TransformWorldToHClip(NBFX_MatrixLocalToWorldPositionV1(position,float4x4(row0,row1,row2,row3)));
}
void NBGraphCustomLocalBefore_float(float3 PositionOS,float3 NormalOS,
    float3 TangentOS,float3 BitangentOS,float CustomLocalToggle,
    float4 LocalToWorld0,
    float4 LocalToWorld1,
    float4 LocalToWorld2,
    float4 LocalToWorld3,
    float4 WorldToLocal0,
    float4 WorldToLocal1,
    float4 WorldToLocal2,
    float4 WorldToLocal3,
    out float3 OutPositionOS,out float3 OutNormalOS,
    out float3 OutTangentOS,out float CustomSign)
{
    OutPositionOS=PositionOS;OutNormalOS=NormalOS;OutTangentOS=TangentOS;
    float rawTangentSign=(dot(cross(NormalOS,TangentOS),BitangentOS)<0.0?-1.0:1.0)*GetOddNegativeScale();
    CustomSign=rawTangentSign*GetOddNegativeScale();
    if(CustomLocalToggle<0.5)return;
    float4x4 l2w=float4x4(LocalToWorld0,LocalToWorld1,LocalToWorld2,LocalToWorld3);
    float4x4 w2l=float4x4(WorldToLocal0,WorldToLocal1,WorldToLocal2,WorldToLocal3);
    OutPositionOS=NBFX_MatrixWorldToLocalPositionV1(PositionOS,w2l);
    OutNormalOS=NBFX_MatrixWorldToLocalNormalV1(NormalOS,l2w);
    OutTangentOS=NBFX_MatrixWorldToLocalDirV1(TangentOS,w2l,true);
    CustomSign=rawTangentSign*NBFX_MatrixOddNegativeScaleV1(l2w);
}
void NBGraphCustomLocalAfter_float(float3 PositionOS,float3 NormalOS,
    float3 TangentOS,float CustomLocalToggle,
    float4 LocalToWorld0,
    float4 LocalToWorld1,
    float4 LocalToWorld2,
    float4 LocalToWorld3,
    float4 WorldToLocal0,
    float4 WorldToLocal1,
    float4 WorldToLocal2,
    float4 WorldToLocal3,
    out float3 OutPositionOS,out float3 OutNormalOS,out float3 OutTangentOS)
{
    OutPositionOS=PositionOS;OutNormalOS=NormalOS;OutTangentOS=TangentOS;
    if(CustomLocalToggle<0.5)return;
    float4x4 l2w=float4x4(LocalToWorld0,LocalToWorld1,LocalToWorld2,LocalToWorld3);
    float4x4 w2l=float4x4(WorldToLocal0,WorldToLocal1,WorldToLocal2,WorldToLocal3);
    OutPositionOS=TransformWorldToObject(NBFX_MatrixLocalToWorldPositionV1(PositionOS,l2w));
    OutNormalOS=TransformWorldToObjectNormal(NBFX_MatrixLocalToWorldNormalV1(NormalOS,w2l));
    OutTangentOS=TransformWorldToObjectDir(NBFX_MatrixLocalToWorldDirV1(TangentOS,l2w,true));
}
#endif
