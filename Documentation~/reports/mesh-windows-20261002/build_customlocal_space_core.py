"""Preview shared custom-matrix arithmetic without installing during archival."""
from pathlib import Path
import hashlib,json,uuid
work=Path(__file__).resolve().parent;root=work.parents[1];package=root/'.utmp/NBFXMeshValidation-20261002/Packages/NB_FX'
out=work/'customlocal-space-core-preview';assert not out.exists()
input_path='NBShaders2/Shader/HLSL/NBShaderInput.hlsl';before=(package/input_path).read_bytes();s=before.decode('utf-8')
kernel_path='NBShaders2/Shader/HLSL/NBShaderCoordinateSpaceV1.hlsl'
kernel='''#ifndef NB_SHADER_COORDINATE_SPACE_V1_INCLUDED
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
'''
replacements=[
 ('return mul(_CustomLocalTransformWorldToLocal, float4(positionWS, 1.0)).xyz;','return NBFX_MatrixWorldToLocalPositionV1(positionWS,_CustomLocalTransformWorldToLocal);'),
 ('return mul(_CustomLocalTransformLocalToWorld, float4(positionOS, 1.0)).xyz;','return NBFX_MatrixLocalToWorldPositionV1(positionOS,_CustomLocalTransformLocalToWorld);'),
 ('return SafeNormalize(mul(normalWS, GetCustomLocalToWorld3x3()));','return NBFX_MatrixWorldToLocalNormalV1(normalWS,_CustomLocalTransformLocalToWorld);'),
 ('return SafeNormalize(mul(normalOS, GetCustomWorldToLocal3x3()));','return NBFX_MatrixLocalToWorldNormalV1(normalOS,_CustomLocalTransformWorldToLocal);'),
 ('float3 dirOS = mul(GetCustomWorldToLocal3x3(), dirWS);\n            return doNormalize ? SafeNormalize(dirOS) : dirOS;','return NBFX_MatrixWorldToLocalDirV1(dirWS,_CustomLocalTransformWorldToLocal,doNormalize);'),
 ('float3 dirWS = mul(GetCustomLocalToWorld3x3(), dirOS);\n            return doNormalize ? SafeNormalize(dirWS) : dirWS;','return NBFX_MatrixLocalToWorldDirV1(dirOS,_CustomLocalTransformLocalToWorld,doNormalize);'),
 ('return determinant(GetCustomLocalToWorld3x3()) < 0.0 ? -1.0 : 1.0;','return NBFX_MatrixOddNegativeScaleV1(_CustomLocalTransformLocalToWorld);')]
for old,new in replacements:assert s.count(old)==1,old;s=s.replace(old,new)
anchor='    float3x3 GetCustomLocalToWorld3x3()';assert s.count(anchor)==1;s=s.replace(anchor,'    #include "Packages/com.xuanxuan.nb.fx/'+kernel_path+'"\n\n'+anchor)
meta='fileFormatVersion: 2\nguid: '+uuid.uuid5(uuid.UUID('52b6057d-6a1c-4f59-a34a-78ae478f71ca'),kernel_path).hex+'\nShaderIncludeImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
files={input_path:s,kernel_path:kernel,kernel_path+'.meta':meta};records=[]
for path,text in files.items():
    target=out/path;target.parent.mkdir(parents=True,exist_ok=True);target.write_text(text,encoding='utf-8',newline='\n')
    original=package/path;records.append({'path':path,'beforeSHA256':hashlib.sha256(original.read_bytes()).hexdigest() if original.exists() else None,'afterSHA256':hashlib.sha256(target.read_bytes()).hexdigest()})
(out/'manifest.json').write_text(json.dumps({'scope':'Preview only: seven existing custom-matrix operations extracted; noncustom branches unchanged. Graph pre/post/UV/Fog/VO/tangent and existing helper row writer still need integration. No keyword/bit/pass changes, no Unity validation yet.',
 'records':records,'inputAttributesAreWorldSpaceWhenCustomEnabled':True,'newFeaturesClaimed':False},indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps({'previewFiles':len(records),'sharedExistingOperations':7,'installed':False}))
