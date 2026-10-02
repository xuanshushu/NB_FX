"""Complete custom-space consumer signature preview; runtime writer still pending."""
from pathlib import Path
import json,hashlib,re
from graph_preview_helpers import GraphPreview
work=Path(__file__).resolve().parent;root=work.parents[1];package=root/'.utmp/NBFXMeshValidation-20261002/Packages/NB_FX'
out=work/'customlocal-consumers-preview';assert not out.exists()
paths={k:'NBShaders2/ShaderGraph/'+v for k,v in [('uv','NBGraphBaseUV.hlsl'),('vo','NBGraphVertexOffset.hlsl'),('color','NBGraphBaseColor.hlsl'),('vat','NBGraphVATSoftBody.hlsl')]}
raw={k:(package/p).read_bytes() for k,p in paths.items()};texts={k:v.decode('utf-8') for k,v in raw.items()}
labels=['CustomLocalToggle']+[stem+str(i) for stem in ['LocalToWorld','WorldToLocal'] for i in range(4)]
params='    float CustomLocalToggle,\n'+''.join('    float4 '+n+',\n' for n in labels[1:]);args=', '.join(labels)
l2w='LocalToWorld0,LocalToWorld1,LocalToWorld2,LocalToWorld3';w2l='WorldToLocal0,WorldToLocal1,WorldToLocal2,WorldToLocal3'
include='#include "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBGraphCustomLocalSpace.hlsl"'
def one(s,old,new,n=1):assert s.count(old)==n,(old,s.count(old),n);return s.replace(old,new)
def signature(s,name,extra):
    start=s.index('void '+name+'(');end=s.index('out ',start)
    return s[:end]+extra.lstrip()+'    '+s[end:]
for k,s in texts.items():
    pos=s.index('\n',s.index('#define '))+1;texts[k]=s[:pos]+include+'\n'+s[pos:]
uv=texts['uv']
for name in ['NBGraphUVVertex_float','NBGraphBaseUV_float','NBGraphBaseUV_half']:uv=signature(uv,name,params)
uv=one(uv,'input.positionWS = TransformObjectToWorld(PositionOS);','input.positionWS = NBGraphLocalToWorldPositionV1(PositionOS,CustomLocalToggle,'+l2w+');')
uv=one(uv,'float4 clipPosition = TransformObjectToHClip(PositionOS);','float4 clipPosition = NBGraphLocalToHClipV1(PositionOS,CustomLocalToggle,'+l2w+');')
uv=one(uv,'float3 fragobjectPos = TransformWorldToObject(fragWorldPos);','float3 fragobjectPos = NBGraphWorldToLocalPositionV1(fragWorldPos,CustomLocalToggle,'+w2l+');')
uv=one(uv,'input.positionOS = TransformWorldToObject(PositionWS);','input.positionOS = NBGraphWorldToLocalPositionV1(PositionWS,CustomLocalToggle,'+w2l+');')
uv=one(uv,'        resolved, maskResolved, mask2Resolved, mask3Resolved,','        '+args+',\n        resolved, maskResolved, mask2Resolved, mask3Resolved,')
texts['uv']=uv
vo=signature(texts['vo'],'NBGraphVertexOffset_float',params)
vo=one(vo,'uvInput.positionWS = TransformObjectToWorld(PositionOS);','uvInput.positionWS = NBGraphLocalToWorldPositionV1(PositionOS,CustomLocalToggle,'+l2w+');')
vo=one(vo,'float4 clipPosition = TransformObjectToHClip(PositionOS);','float4 clipPosition = NBGraphLocalToHClipV1(PositionOS,CustomLocalToggle,'+l2w+');')
vo=one(vo,'        direction = mul((float3x3)unity_WorldToObject, direction);','''    {
        if(CustomLocalToggle>0.5)
            direction=NBFX_MatrixWorldToLocalDirV1(direction,float4x4('''+w2l+'''),false);
        else direction=mul((float3x3)unity_WorldToObject,direction);
    }''')
texts['vo']=vo
color=texts['color']
for name in ['NBGraphBaseColor_float','NBGraphBaseColor_half','NBGraphFogVertex_float','NBGraphFogVertex_half']:color=signature(color,name,params)
for name in ['NBGraphBaseColor_float','NBGraphBaseColor_half']:
    start=color.index('void '+name+'(');brace=color.index('{',start)+1
    color=color[:brace]+'''
    // SG's fragment basis owns renderer odd scale; replace that factor with
    // the original custom matrix determinant for the active world-sim host.
    if(CustomLocalToggle>0.5)
        BitangentWS*=NBFX_MatrixOddNegativeScaleV1(float4x4('''+l2w+'''))/GetOddNegativeScale();
'''+color[brace:]
color=one(color,'FogFactor = ComputeFogFactor(TransformObjectToHClip(PositionOS).z);','FogFactor = ComputeFogFactor(NBGraphLocalToHClipV1(PositionOS,CustomLocalToggle,'+l2w+').z);')
color=one(color,'NBGraphFogVertex_float((float3)PositionOS, computed);','NBGraphFogVertex_float((float3)PositionOS,'+args+', computed);')
for name in ['NBGraphSixWayBake_float','NBGraphSixWayBake_half']:color=signature(color,name,'    float CustomLocalToggle,float CustomSign,\n')
color=one(color,'    half3 tangentWS = (half3)TangentWS;','    if(CustomLocalToggle>0.5)sign=(half)CustomSign;\n    half3 tangentWS = (half3)TangentWS;')
color=one(color,'NBGraphSixWayBake_float(NormalWS,TangentWS,BitangentWS,','NBGraphSixWayBake_float(NormalWS,TangentWS,BitangentWS,CustomLocalToggle,CustomSign,')
texts['color']=color
vat=signature(texts['vat'],'NBGraphVATWorldBasis_float','    float3 CustomTangentOS,float CustomSign,\n'+params)
anchor='''    NormalWS=RawNormalWS; TangentWS=RawTangentWS;
    BitangentWS=RawBitangentWS;'''
vat=one(vat,anchor,anchor+'''
    if(CustomLocalToggle>0.5)
    {
        NormalWS=NBFX_MatrixLocalToWorldNormalV1(VATNormalOS,float4x4('''+w2l+'''));
        TangentWS=NBFX_MatrixLocalToWorldDirV1(CustomTangentOS,float4x4('''+l2w+'''),true);
        BitangentWS=CustomSign*cross(NormalWS,TangentWS);
        return;
    }''')
texts['vat']=vat
g=GraphPreview(work/'customlocal-wiring-preview/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph','52b6057d-6a1c-4f59-a34a-78ae478f71ca')
pre=next(o for o in g.objects if o.get('m_FunctionName')=='NBGraphCustomLocalBefore');sources={'CustomLocalToggle':g.source_for_property('_NB_CustomLocalTransform')}
for stem in ['LocalToWorld','WorldToLocal']:
    for i in range(4):sources[stem+str(i)]=g.source_for_property('_NB_Custom'+stem+str(i))
def output(node,label):return {'m_Node':{'m_Id':node['m_ObjectId']},'m_SlotId':g.slot(node,label)['m_Id']}
changed=[]
for name in ['NBGraphUVVertex','NBGraphBaseUV','NBGraphVertexOffset','NBGraphFogVertex','NBGraphBaseColor','NBGraphVATWorldBasis','NBGraphSixWayBake']:
    node=next(o for o in g.objects if o.get('m_FunctionName')==name)
    if name=='NBGraphVATWorldBasis':
        # Its vec3 input template belongs to the same stage.
        g.input(node,'CustomTangentOS',output(pre,'OutTangentOS'),'VATNormalOS')
        g.input(node,'CustomSign',output(pre,'CustomSign'),'VATToggle')
    sequence=['CustomLocalToggle'] if name=='NBGraphSixWayBake' else labels
    scalar_template='VATToggle' if name=='NBGraphVATWorldBasis' else 'Supported' if name=='NBGraphVertexOffset' else 'NB_Flags0Lo16' if name in ['NBGraphBaseUV','NBGraphUVVertex'] else None
    # Fog/bake have no scalar input; clone their existing scalar output then
    # explicitly mark the appended slot as an input after creation.
    if scalar_template is None:scalar_template='FogFactor' if name=='NBGraphFogVertex' else 'CustomDataFlag0Lo16' if name=='NBGraphBaseColor' else None
    for label in sequence:
        if name in ['NBGraphFogVertex','NBGraphSixWayBake']:
            template=g.slot(pre,'CustomSign' if label=='CustomLocalToggle' else 'LocalToWorld0')
            sid=max(g.by[r['m_Id']]['m_Id'] for r in node['m_Slots'])+1
            import copy
            slot=copy.deepcopy(template);slot['m_ObjectId']=g.uid(name+':'+label);slot['m_Id']=sid;slot['m_DisplayName']=slot['m_ShaderOutputName']=label;slot['m_SlotType']=0
            g.add(slot);node['m_Slots'].append({'m_Id':slot['m_ObjectId']});g.g['m_Edges'].append({'m_OutputSlot':sources[label],'m_InputSlot':{'m_Node':{'m_Id':node['m_ObjectId']},'m_SlotId':sid}})
        else:
            # Pick a vec4 slot with the right type, retaining stage capability.
            vector_template='CylinderMatrix0' if name in ['NBGraphBaseUV','NBGraphUVVertex','NBGraphVertexOffset'] else 'Custom1' if name=='NBGraphBaseColor' else None
            if label=='CustomLocalToggle':g.input(node,label,sources[label],scalar_template)
            elif vector_template is not None:g.input(node,label,sources[label],vector_template)
            else:
                template=g.slot(pre,'LocalToWorld0');sid=max(g.by[r['m_Id']]['m_Id'] for r in node['m_Slots'])+1
                import copy
                slot=copy.deepcopy(template);slot['m_ObjectId']=g.uid(name+':'+label);slot['m_Id']=sid;slot['m_DisplayName']=slot['m_ShaderOutputName']=label;slot['m_SlotType']=0
                g.add(slot);node['m_Slots'].append({'m_Id':slot['m_ObjectId']});g.g['m_Edges'].append({'m_OutputSlot':sources[label],'m_InputSlot':{'m_Node':{'m_Id':node['m_ObjectId']},'m_SlotId':sid}})
    if name=='NBGraphSixWayBake':
        template=g.slot(pre,'CustomSign');sid=max(g.by[r['m_Id']]['m_Id'] for r in node['m_Slots'])+1
        import copy
        slot=copy.deepcopy(template);slot['m_ObjectId']=g.uid(name+':CustomSign');slot['m_Id']=sid;slot['m_DisplayName']=slot['m_ShaderOutputName']='CustomSign';slot['m_SlotType']=0;g.add(slot);node['m_Slots'].append({'m_Id':slot['m_ObjectId']})
        g.g['m_Edges'].append({'m_OutputSlot':output(pre,'CustomSign'),'m_InputSlot':{'m_Node':{'m_Id':node['m_ObjectId']},'m_SlotId':sid}})
    changed.append(node['m_ObjectId'])
paths['graph']='NBShaders2/ShaderGraph/NBShaderGraph.shadergraph';texts['graph']=g.serialize(changed);records=[]
for key,path in paths.items():
    target=out/path;target.parent.mkdir(parents=True,exist_ok=True);target.write_text(texts[key],encoding='utf-8',newline='\n')
    original=package/path;records.append({'path':path,'beforeSHA256':hashlib.sha256(original.read_bytes()).hexdigest(),'afterSHA256':hashlib.sha256(target.read_bytes()).hexdigest()})
(out/'manifest.json').write_text(json.dumps({'scope':'Custom-space consumer preview only, no installation. UV/Fog/VO/decal/SixWay/fragment TBN bind existing matrix math. Runtime helper writer and structural/Unity checks still pending.',
 'objectsAfter':len(g.objects),'records':records,'installed':False},indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps({'previewFiles':len(records),'objects':len(g.objects),'installed':False}))
