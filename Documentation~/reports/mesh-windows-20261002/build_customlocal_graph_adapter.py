from pathlib import Path
import json,hashlib,uuid
work=Path(__file__).resolve().parent;root=work.parents[1];package=root/'.utmp/NBFXMeshValidation-20261002/Packages/NB_FX'
out=work/'customlocal-adapter-preview';assert not out.exists()
path='NBShaders2/ShaderGraph/NBGraphCustomLocalSpace.hlsl'
rows=[a+str(i) for a in ['LocalToWorld','WorldToLocal'] for i in range(4)]
parameters=''.join('    float4 '+n+',\n' for n in rows)
body='''#ifndef NB_GRAPH_CUSTOM_LOCAL_SPACE_INCLUDED
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
'''+parameters+'''    out float3 OutPositionOS,out float3 OutNormalOS,
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
'''+parameters+'''    out float3 OutPositionOS,out float3 OutNormalOS,out float3 OutTangentOS)
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
'''
guid=uuid.uuid5(uuid.UUID('52b6057d-6a1c-4f59-a34a-78ae478f71ca'),path).hex
for name,data in [(path,body),(path+'.meta','fileFormatVersion: 2\nguid: '+guid+'\nShaderIncludeImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')]:
    dest=out/name;dest.parent.mkdir(parents=True,exist_ok=True);dest.write_text(data,encoding='utf-8',newline='\n')
(out/'manifest.json').write_text(json.dumps({'scope':'Preview only: explicit world-simulation pre stage and SG block post stage. No Graph edges/helper writes installed yet; UV/Fog/VO/TBN consumers must be connected as one slice before Unity verification.',
 'path':path,'guid':guid,'sha256':hashlib.sha256((out/path).read_bytes()).hexdigest(),'newKeywords':0,'newPackedBits':0,'installed':False},indent=2)+'\n',encoding='utf-8',newline='\n')
print('Prepared custom-space pre/post and exact transform adapters; not installed')
