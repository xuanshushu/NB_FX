"""Extract unchanged Houdini branches into one package-owned kernel, preview only."""
from pathlib import Path
import copy,hashlib,json,re,uuid
work=Path(__file__).resolve().parent;root=work.parents[1]
package=root/'.utmp/NBFXMeshValidation-20261002/Packages/NB_FX'
out=work/'houdini-modes-preview';assert not out.exists()
paths={k:v for k,v in [('legacy','XuanXuanRenderUtility/Shader/HLSL/HoudiniVAT.hlsl'),
 ('math','XuanXuanRenderUtility/Shader/HLSL/HoudiniVATMathV1.hlsl'),
 ('graph','NBShaders2/ShaderGraph/NBShaderGraph.shadergraph'),
 ('adapter','NBShaders2/ShaderGraph/NBGraphVATSoftBody.hlsl')]}
raw={k:(package/p).read_bytes() for k,p in paths.items()};texts={k:v.decode('utf-8') for k,v in raw.items()}
def function(s,name):
    start=s.rfind('\n',0,s.index(name+'('))+1;brace=s.index('{',start);level=1;end=brace+1
    while level:
        level+=(s[end]=='{')-(s[end]=='}');end+=1
    return start,end,s[start:end]
legacy=texts['legacy'];math=texts['math']
for name in ['HVAT_DecodeQuaternion','HVAT_DecodeLookupUV','HVAT_HashRandom2D']:
    a,b,fn=function(legacy,name);legacy=legacy[:a]+legacy[b:];math=math.replace('\n#endif','\n'+fn+'\n\n#endif')
texts['math']=math
_,_,old_fn=function(legacy,'ApplyHoudiniVAT');body=old_fn[old_fn.index('{')+1:old_fn.rfind('}')]
body=body.replace('    HVAT_ComputeFrameSelection(input, selectedFrame, frameAlpha);','')
body=body.replace('    float selectedFrame, frameAlpha;','')
for i,macro in enumerate(['SOFTBODY','RIGIDBODY','DYNAMIC_REMESH','PARTICLE_SPRITE']):
    directive=('#if' if i==0 else '#elif')+' defined(_HOUDINI_VAT_'+macro+')'
    assert body.count(directive)==1;body=body.replace(directive,('if' if i==0 else 'else if')+' (mode == '+str(i)+')')
assert body.count('#endif')==1;body=body.replace('#endif','')
body=body.replace('CheckLocalFlags1(FLAG_BIT_PARTICLE_1_IS_PARTICLE_SYSTEM)','particle')
body=body.replace('HVAT_GetVatUV1(input)','(particle ? uv0.zw : uv1.xy)')
for old,new in [('positionOS.xyz','positionOS'),('input.Custom2','uv2'),('input.vatTexcoord5','uv4'),('input.texcoords','uv0')]:body=body.replace(old,new)
properties=sorted(set(re.findall(r'\b_[A-Za-z][A-Za-z0-9_]*\b',body)))
texture_names=['_posTexture','_posTexture2','_rotTexture','_colTexture','_lookupTable']
scalars=[p for p in properties if not p.startswith('_HOUDINI') and p not in texture_names]
for p in scalars:body=re.sub(r'\b'+p+r'\b','config.'+p,body)
for p in texture_names:
    body=body.replace('SAMPLE_TEXTURE2D_LOD('+p+', sampler'+p+',','SAMPLE_TEXTURE2D_LOD('+p+', sampler'+p+',')
kernel='''#ifndef NB_FX_HOUDINI_VAT_KERNEL_V1_INCLUDED
#define NB_FX_HOUDINI_VAT_KERNEL_V1_INCLUDED
#include "Packages/com.xuanxuan.nb.fx/XuanXuanRenderUtility/Shader/HLSL/HoudiniVATMathV1.hlsl"
// Arithmetic and sampling order extracted from the existing four-mode path.
// Legacy supplies a compile-time mode; Graph supplies its existing mode input.
struct HVAT_ConfigV1
{
'''+''.join('    float '+p+';\n' for p in scalars)+'''};
void HVAT_ApplyModesV1(int mode, bool particle, float4 uv0, float4 uv1,
    float4 uv2, float4 uv4, float selectedFrame, float frameAlpha,
    HVAT_ConfigV1 config,
'''+''.join('    TEXTURE2D_PARAM('+p+', sampler'+p+'),\n' for p in texture_names)+'''    inout float3 positionOS, inout float3 normalOS)
{
'''+body+'}\n#endif\n'
assert 'AttributesParticle' not in kernel and 'CheckLocalFlags' not in kernel
kernel_path='XuanXuanRenderUtility/Shader/HLSL/HoudiniVATKernelV1.hlsl'
paths['kernel']=kernel_path;texts['kernel']=kernel
paths['kernel_meta']=kernel_path+'.meta';texts['kernel_meta']='fileFormatVersion: 2\nguid: 266d6d95604a56e68fe75e2c30ad1006\nShaderIncludeImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
a,b,_=function(legacy,'ApplyHoudiniVAT')
wrapper='''void ApplyHoudiniVAT(AttributesParticle input, inout float4 positionOS, inout float3 normalOS)
{
    int mode = -1;
#if defined(_HOUDINI_VAT_SOFTBODY)
    mode = 0;
#elif defined(_HOUDINI_VAT_RIGIDBODY)
    mode = 1;
#elif defined(_HOUDINI_VAT_DYNAMIC_REMESH)
    mode = 2;
#elif defined(_HOUDINI_VAT_PARTICLE_SPRITE)
    mode = 3;
#endif
    float selectedFrame, frameAlpha;
    HVAT_ComputeFrameSelection(input, selectedFrame, frameAlpha);
    HVAT_ConfigV1 config;
'''+''.join('    config.'+p+' = '+p+';\n' for p in scalars)+'''    float3 animatedPositionOS = positionOS.xyz;
    HVAT_ApplyModesV1(mode, CheckLocalFlags1(FLAG_BIT_PARTICLE_1_IS_PARTICLE_SYSTEM),
        input.texcoords, input.Custom1, input.Custom2, float4(input.vatTexcoord5,0,0),
        selectedFrame, frameAlpha, config,
'''+''.join('        TEXTURE2D_ARGS('+p+', sampler'+p+'),\n' for p in texture_names)+'''        animatedPositionOS, normalOS);
    positionOS.xyz = animatedPositionOS;
}'''
legacy=legacy[:a]+wrapper+legacy[b:];legacy=legacy.replace('#define HOUDINI_VAT_INCLUDED','#define HOUDINI_VAT_INCLUDED\n#include "Packages/com.xuanxuan.nb.fx/'+kernel_path+'"')
texts['legacy']=legacy
extra=[('_B_interpolate','Interpolate',1),('_animateFirstFrame','AnimateFirstFrame',0),
 ('_globalPscaleMul','GlobalPscaleMul',1),('_B_pscaleAreInPosA','PscaleAreInPosA',1),
 ('_widthBaseScale','WidthBaseScale',.2),('_heightBaseScale','HeightBaseScale',.2),
 ('_B_hideOverlappingOrigin','HideOverlappingOrigin',1),('_originRadius','OriginRadius',.02),
 ('_B_CAN_SPIN','CanSpin',0),('_B_spinFromHeading','SpinFromHeading',0),
 ('_spinPhase','SpinPhase',0),('_scaleByVelAmount','ScaleByVelAmount',1),('_B_LOAD_COL_TEX','LoadColTex',0)]
bindings={p:n for p,n,v in extra}
bindings.update({'_frameCount':'FrameCount','_B_LOAD_POS_TWO_TEX':'LoadPosTwoTex','_B_UNLOAD_ROT_TEX':'UnloadRotTex'})
for axis in 'XYZ':
    bindings['_boundMin'+axis]='BoundMin'+axis;bindings['_boundMax'+axis]='BoundMax'+axis
assert set(bindings)==set(scalars),(set(bindings)-set(scalars),set(scalars)-set(bindings))
adapter=texts['adapter'].replace('HoudiniVATMathV1.hlsl','HoudiniVATKernelV1.hlsl')
adapter=adapter.replace('// V0 only: ordinary Mesh Houdini SoftBody. NB screen passes are statically\n// excluded; other VAT submodes are not replaced by a fake SoftBody result.',
 '// Ordinary Mesh Houdini four-mode adapter. NB screen passes are statically\n// excluded. Tyflow modes remain explicitly unsupported by this adapter.')
signature='    out float3 OutPositionOS, out float3 OutNormalOS, out float Supported)'
assert adapter.count(signature)==1
adapter=adapter.replace(signature,'    UnityTexture2D ColTexture, UnityTexture2D LookupTable, float4 UV4,\n'+''.join('    float '+n+',\n' for _,n,_ in extra)+signature)
adapter=adapter.replace('if (round(VATMode)!=0.0 || round(HoudiniVATSubMode)!=0.0)','if (round(VATMode)!=0.0 || HoudiniVATSubMode<0.0 || HoudiniVATSubMode>3.0)')
start=adapter.index('    float comparisonBoundMaxb=');end=adapter.index('    bool particle=',start)
adapter=adapter[:start]+adapter[end:]
start=adapter.index('    float2 vatUV=');end=adapter.index('\n#endif',start)
adapter=adapter[:start]+'''    HVAT_ConfigV1 config;
'''+''.join('    config.'+p+' = '+bindings[p]+';\n' for p in scalars)+'''    HVAT_ApplyModesV1((int)round(HoudiniVATSubMode),particle,UV0,UV1,Custom2,UV4,
        selectedFrame,frameAlpha,config,
        TEXTURE2D_ARGS(PosTexture.tex,PosTexture.samplerstate),
        TEXTURE2D_ARGS(PosTexture2.tex,PosTexture2.samplerstate),
        TEXTURE2D_ARGS(RotTexture.tex,RotTexture.samplerstate),
        TEXTURE2D_ARGS(ColTexture.tex,ColTexture.samplerstate),
        TEXTURE2D_ARGS(LookupTable.tex,LookupTable.samplerstate),
        OutPositionOS,OutNormalOS);'''+adapter[end:]
texts['adapter']=adapter
def decode(s):
    d=json.JSONDecoder();objects=[];pos=0
    while pos<len(s):
        while pos<len(s) and s[pos].isspace():pos+=1
        if pos==len(s):break
        value,pos=d.raw_decode(s,pos);objects.append(value)
    return objects
objects=decode(texts['graph']);before=copy.deepcopy(objects);g=objects[0];by={o['m_ObjectId']:o for o in objects}
namespace=uuid.UUID('699ea584-8394-49ab-8e52-b74f29ce1ab5')
def uid(s):return uuid.uuid5(namespace,s).hex
def add(o):
    assert o['m_ObjectId'] not in by;objects.append(o);by[o['m_ObjectId']]=o
def findslot(n,label):return next(by[r['m_Id']] for r in n['m_Slots'] if by[r['m_Id']]['m_DisplayName']==label)
vat=next(o for o in objects if o.get('m_FunctionName')=='NBGraphVATSoftBody');sources={}
for ref,label,value in extra+[('_colTexture','ColTexture',None),('_lookupTable','LookupTable',None)]:
    template=next(o for o in objects if o.get('m_OverrideReferenceName')==('_posTexture' if value is None else '_B_autoPlayback'))
    prop=copy.deepcopy(template);prop['m_ObjectId']=uid(ref);prop['m_Guid']={'m_GuidSerialized':str(uuid.uuid5(namespace,ref+':guid'))}
    for key in ('m_Name','m_RefNameGeneratedByDisplayName'):prop[key]=label
    for key in ('m_DefaultReferenceName','m_OverrideReferenceName'):prop[key]=ref
    prop['m_Hidden']=False
    if value is not None:prop['m_Value']=float(value);prop['m_FloatType']=0
    node_template=next(o for o in objects if o.get('m_Property',{}).get('m_Id')==template['m_ObjectId'])
    node=copy.deepcopy(node_template);node['m_ObjectId']=uid(ref+':node');node['m_Property']={'m_Id':prop['m_ObjectId']}
    slot=copy.deepcopy(by[node_template['m_Slots'][0]['m_Id']]);slot['m_ObjectId']=uid(ref+':out');slot['m_DisplayName']=slot['m_ShaderOutputName']=label
    if value is not None:slot['m_Value']=slot['m_DefaultValue']=float(value)
    node['m_Slots']=[{'m_Id':slot['m_ObjectId']}]
    for o in [prop,node,slot]:add(o)
    g['m_Properties'].append({'m_Id':prop['m_ObjectId']});g['m_Nodes'].append({'m_Id':node['m_ObjectId']})
    sources[label]={'m_Node':{'m_Id':node['m_ObjectId']},'m_SlotId':slot['m_Id']}
uv=copy.deepcopy(next(o for o in objects if o.get('m_OutputChannel')==1));uv['m_ObjectId']=uid('UV4');uv['m_OutputChannel']=4
uv_slot=copy.deepcopy(by[uv['m_Slots'][0]['m_Id']]);uv_slot['m_ObjectId']=uid('UV4:out');uv['m_Slots']=[{'m_Id':uv_slot['m_ObjectId']}]
add(uv);add(uv_slot);g['m_Nodes'].append({'m_Id':uv['m_ObjectId']});sources['UV4']={'m_Node':{'m_Id':uv['m_ObjectId']},'m_SlotId':uv_slot['m_Id']}
sid=max(by[r['m_Id']]['m_Id'] for r in vat['m_Slots'])
for label in ['ColTexture','LookupTable','UV4']+[n for _,n,_ in extra]:
    template=findslot(vat,'PosTexture' if label in ['ColTexture','LookupTable'] else 'UV1' if label=='UV4' else 'FrameCount')
    sid+=1;slot=copy.deepcopy(template);slot['m_ObjectId']=uid('VAT:slot:'+label);slot['m_Id']=sid;slot['m_DisplayName']=slot['m_ShaderOutputName']=label
    add(slot);vat['m_Slots'].append({'m_Id':slot['m_ObjectId']});g['m_Edges'].append({'m_OutputSlot':sources[label],'m_InputSlot':{'m_Node':{'m_Id':vat['m_ObjectId']},'m_SlotId':sid}})
for o in before:
    if o['m_ObjectId'] not in [g['m_ObjectId'],vat['m_ObjectId']]:assert o==by[o['m_ObjectId']]
texts['graph']='\n\n'.join(json.dumps(o,indent=4,ensure_ascii=False) for o in objects)+'\n'
records=[]
for k,p in paths.items():
    dest=out/p;dest.parent.mkdir(parents=True,exist_ok=True);dest.write_text(texts[k],encoding='utf-8',newline='\n')
    records.append({'path':p,'beforeSHA256':hashlib.sha256(raw[k]).hexdigest() if k in raw else None,'afterSHA256':hashlib.sha256(dest.read_bytes()).hexdigest()})
(out/'manifest.json').write_text(json.dumps({'scope':'Isolated preview: unchanged four-mode Houdini geometry kernel shared by current ShaderLab and Graph; Frozen untouched; no new keywords/packed bits/passes',
 'objectsBefore':len(before),'objectsAfter':len(objects),'records':records,'rawMathExtraction':True,'additionalUVChannels':[4]},indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps({'files':len(records),'objects':len(objects),'previewOnly':True}))
