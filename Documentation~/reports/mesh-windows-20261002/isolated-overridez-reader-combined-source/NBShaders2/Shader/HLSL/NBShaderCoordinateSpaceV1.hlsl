#ifndef NB_SHADER_COORDINATE_SPACE_V1_INCLUDED
#define NB_SHADER_COORDINATE_SPACE_V1_INCLUDED
// Exact arithmetic of the existing _CUSTOM_LOCAL_TRANSFORM branch.
// Inputs in this branch are already world-space simulation attributes.
float3 NBFX_MatrixWorldToLocalPositionV1(float3 value, float4x4 worldToLocal)
{ return mul(worldToLocal,float4(value,1.0)).xyz; }
float3 NBFX_MatrixLocalToWorldPositionV1(float3 value, float4x4 localToWorld)
{ return mul(localToWorld,float4(value,1.0)).xyz; }
float3 NBFX_MatrixWorldToLocalNormalV1(float3 value, float4x4 localToWorld)
{ return SafeNormalize(mul(value,(float3x3)localToWorld)); }
float3 NBFX_MatrixLocalToWorldNormalV1(float3 value, float4x4 worldToLocal)
{ return SafeNormalize(mul(value,(float3x3)worldToLocal)); }
float3 NBFX_MatrixWorldToLocalDirV1(float3 value, float4x4 worldToLocal, bool doNormalize)
{ float3 dir=mul((float3x3)worldToLocal,value);return doNormalize?SafeNormalize(dir):dir; }
float3 NBFX_MatrixLocalToWorldDirV1(float3 value, float4x4 localToWorld, bool doNormalize)
{ float3 dir=mul((float3x3)localToWorld,value);return doNormalize?SafeNormalize(dir):dir; }
float NBFX_MatrixOddNegativeScaleV1(float4x4 localToWorld)
{ return determinant((float3x3)localToWorld)<0.0?-1.0:1.0; }
#endif
