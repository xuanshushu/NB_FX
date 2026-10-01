#!/usr/bin/env python3
# Preview only, root owns the application after current GUI checkpoint.
from pathlib import Path
import copy,json,uuid,hashlib
B=Path('/Users/bytedance/UnityProject/NBUnityProject/Packages/NB_FX'); O=Path('/tmp/nbfx-refraction-preview');O.mkdir(exist_ok=True)
ns=uuid.UUID('c3eeb0f3-e1b2-4b9d-a048-a870135dffbe')
def u(s):return uuid.uuid5(ns,s).hex
def once(s,a,b,n=1):
 assert s.count(a)==n,(s.count(a),a[:80]);return s.replace(a,b)
def dec(s):
 d=json.JSONDecoder();r=[];i=0
 while i<len(s):
  while i<len(s) and s[i].isspace():i+=1
  if i<len(s):v,i=d.raw_decode(s,i);r.append(v)
 return r
paths=['NBShaders2/Shader/HLSL/NBShaderDistortionV1.hlsl','NBShaders2/Shader/HLSL/NBShaderInput.hlsl','NBShaders2/ShaderGraph/NBGraphBaseColor.hlsl','NBShaders2/ShaderGraph/NBShaderGraph.shadergraph']
source={p:(B/p).read_text()for p in paths};out={}
p=paths[0];s=source[p]
fun='''// Original CustomRefract math, shared without material/global dependencies.
// Keep full float arithmetic and the zero vector for total internal reflection;
// each host retains its original half assignment before clip-direction transform.
float3 NBFX_RefractDirectionV1(float3 incident, float3 normal, float eta)
{
    float N_dot_I = dot(normal, incident);
    float k = 1.0f - eta * eta * (1.0f - N_dot_I * N_dot_I);
    if (k < 0.0)
        return float3(0, 0, 0);
    else
        return eta * incident - (eta * N_dot_I + sqrt(k)) * normal;
}

'''
assert 'NBFX_RefractDirectionV1'not in s;out[p]=once(s,'// Pure texture-noise decode',fun+'// Pure texture-noise decode')
p=paths[1];s=source[p];s=once(s,'    #include "NBShaderGeometryV1.hlsl"','    #include "NBShaderGeometryV1.hlsl"\n    #include "NBShaderDistortionV1.hlsl"')
a=s.index('    float3 CustomRefract(');b=s.index('\n    Texture2D _ParallaxMapping_Map;',a)
s=s[:a]+'''    float3 CustomRefract(float3 incident, float3 normal, float eta)
    {
        return NBFX_RefractDirectionV1(incident, normal, eta);
    }
'''+s[b:];out[p]=s
p=paths[2];s=source[p]
s=once(s,'// in the caller after texture scaling. CustomData, Refraction and\n// chromatic aberration remain pending.','// in the caller after texture scaling. Refraction replaces only the texture\n// Noise source; CustomData and chromatic aberration remain pending.')
s=once(s,'    uint flags0, uint channels, uint wrapFlags, uint noMipFlags,\n    out half2 signedRG, out half noiseMask)', '    uint flags0, uint channels, uint wrapFlags, uint noMipFlags,\n    bool useRefraction, half refractionIOR, float3 viewDirWS, float3 facedNormalWS,\n    out half2 signedRG, out half noiseMask)')
a=s.index('    NBFX_FeatureUVTransformInputV2 uvInput =',s.index('void NBGraphTextureNoise('));b=s.index('    if (hasMask)',a)
old=s[a:b];start=old.index('    uvInput.originUV =');end=old.index('    if (hasMask)')if '    if (hasMask)'in old else len(old)
# Keep texture-noise calculation exactly intact, but no texture sample at all
# in the refractive branch. The external mask still starts at 1 rather than
# inheriting NoiseMap.alpha from an otherwise unneeded texture sample.
texture=old[:]
texture=texture.replace('    signedRG = 0;\n    noiseMask = 1;\n','')
texture=texture.replace('    NBFX_FeatureUVTransformInputV2 uvInput = (NBFX_FeatureUVTransformInputV2)0;\n','')
new='''    NBFX_FeatureUVTransformInputV2 uvInput = (NBFX_FeatureUVTransformInputV2)0;
    signedRG = 0;
    noiseMask = 1;
    if (useRefraction)
    {
        half3 refracVec = NBFX_RefractDirectionV1(-viewDirWS, facedNormalWS,
            1 / refractionIOR);
        refracVec = TransformWorldToHClipDir(refracVec);
        signedRG = refracVec.xy;
    }
    else
    {
'''+''.join('    '+line+'\n'for line in texture.splitlines())+'''    }
'''
s=s[:a]+new+s[b:]
# CF inputs append immediately before output arguments; serialized slots are
# traversed in order, irrespective of their numeric slot IDs.
s=once(s,'    out float4 Out, out float2 NBDistortionSignedRG,','    float DistortMode, float RefractionIOR,\n    out float4 Out, out float2 NBDistortionSignedRG,')
s=once(s,'    out half4 Out, out half2 NBDistortionSignedRG,','    float DistortMode, float RefractionIOR,\n    out half4 Out, out half2 NBDistortionSignedRG,')
s=once(s,'            NBGraphDecodeUInt32(NB_ColorChannelLo16, 0.0), wrapFlags, noMipFlags,\n            signedRG, noiseMask);','            NBGraphDecodeUInt32(NB_ColorChannelLo16, 0.0), wrapFlags, noMipFlags,\n            !NB_GRAPH_DEPTH_SHADOW_PASS && round(DistortMode) == 1.0, (half)RefractionIOR,\n            ViewDirWS, (IsFrontFace > 0.5 ? 1.0 : -1.0) * normalForFeatures,\n            signedRG, noiseMask);',2)
out[p]=s
p=paths[3];objs=dec(source[p]);old=copy.deepcopy(objs);root=objs[0];by={o['m_ObjectId']:o for o in objs};cf=next(o for o in objs if o.get('m_FunctionName')=='NBGraphBaseColor');cat=next(o for o in objs if o.get('m_Type')=='UnityEditor.ShaderGraph.CategoryData')
tp=next(o for o in objs if o.get('m_OverrideReferenceName')=='_NoiseIntensity');tn=next(o for o in objs if o.get('m_Property',{}).get('m_Id')==tp['m_ObjectId']);to=by[tn['m_Slots'][0]['m_Id']];ti=next(by[x['m_Id']]for x in cf['m_Slots']if by[x['m_Id']].get('m_DisplayName')=='NoiseIntensity')
append=[]
for ix,(ref,name,value)in enumerate([('_DistortMode','DistortMode',0.0),('_RefractionIOR','RefractionIOR',1.5)]):
 assert not any(o.get('m_OverrideReferenceName')==ref for o in objs),ref
 prop=copy.deepcopy(tp);prop['m_ObjectId']=u(ref+'prop');prop['m_Guid']={'m_GuidSerialized':str(uuid.uuid5(ns,ref+'guid'))};prop['m_Name']=prop['m_RefNameGeneratedByDisplayName']=name;prop['m_DefaultReferenceName']=prop['m_OverrideReferenceName']=ref;prop['m_Value']=value
 node=copy.deepcopy(tn);node['m_ObjectId']=u(ref+'node');node['m_Property']={'m_Id':prop['m_ObjectId']};node['m_Slots']=[{'m_Id':u(ref+'out')}];node['m_DrawState']['m_Position']['y']=28000.0+ix*140
 output=copy.deepcopy(to);output['m_ObjectId']=u(ref+'out');output['m_DisplayName']=name;output['m_Value']=value;output['m_DefaultValue']=value
 inp=copy.deepcopy(ti);inp['m_ObjectId']=u(ref+'in');inp['m_Id']=max(by[x['m_Id']]['m_Id']for x in cf['m_Slots'])+1;inp['m_DisplayName']=inp['m_ShaderOutputName']=name;inp['m_Value']=value;inp['m_DefaultValue']=value
 objs.extend([prop,node,output,inp]);by.update({o['m_ObjectId']:o for o in [prop,node,output,inp]});cf['m_Slots'].append({'m_Id':inp['m_ObjectId']});root['m_Properties'].append({'m_Id':prop['m_ObjectId']});root['m_Nodes'].append({'m_Id':node['m_ObjectId']});cat['m_ChildObjectList'].append({'m_Id':prop['m_ObjectId']});root['m_Edges'].append({'m_OutputSlot':{'m_Node':{'m_Id':node['m_ObjectId']},'m_SlotId':output['m_Id']},'m_InputSlot':{'m_Node':{'m_Id':cf['m_ObjectId']},'m_SlotId':inp['m_Id']}});append.append({'name':name,'slotId':inp['m_Id']})
changed=[a['m_ObjectId']for a,b in zip(old,objs)if a!=b];assert set(changed)=={root['m_ObjectId'],cf['m_ObjectId'],cat['m_ObjectId']}
out[p]='\n\n'.join(json.dumps(o,indent=4,ensure_ascii=False)for o in objs)+'\n'
for p,s in out.items():q=O/p;q.parent.mkdir(parents=True,exist_ok=True);q.write_text(s)
proof={'scope':'RF1 preview only no Unity/Git','objectCount':[len(old),len(objs)],'changedObjects':changed,'appendedSlots':append,'sourceSha256':{p:hashlib.sha256(s.encode()).hexdigest()for p,s in source.items()},'candidateSha256':{p:hashlib.sha256(s.encode()).hexdigest()for p,s in out.items()}}
(O/'audit.json').write_text(json.dumps(proof,indent=2));print(json.dumps(proof,indent=2))
