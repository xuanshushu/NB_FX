"""Rebase all four unchanged custom-data words onto the latest Mesh candidate."""
from pathlib import Path
import copy, hashlib, json, re, uuid
work=Path(__file__).resolve().parent
package=work.parents[1]/'.utmp/NBFXMeshValidation-20261002/Packages/NB_FX'
out=work/'customdata-preview';assert not out.exists()
files={
 'graph':'NBShaders2/ShaderGraph/NBShaderGraph.shadergraph',
 'uv':'NBShaders2/ShaderGraph/NBGraphBaseUV.hlsl',
 'color':'NBShaders2/ShaderGraph/NBGraphBaseColor.hlsl',
 'vo':'NBShaders2/ShaderGraph/NBGraphVertexOffset.hlsl',
 'vat':'NBShaders2/ShaderGraph/NBGraphVATSoftBody.hlsl',
 'runtime':'NBShaders2/Runtime/NBShaderFlags.cs',
 'contract':'NBShaders2/Shader/HLSL/NBShaderSharedContractV2.hlsl',
 'math':'NBShaders2/Shader/HLSL/NBShaderUVV2.hlsl'}
raw={k:(package/p).read_bytes() for k,p in files.items()}
texts={k:v.decode('utf-8') for k,v in raw.items()}
def one(s,old,new,n=1):
    assert s.count(old)==n,(old,s.count(old),n)
    return s.replace(old,new)
def decode(s):
    decoder=json.JSONDecoder();objects=[];position=0
    while position<len(s):
        while position<len(s) and s[position].isspace():position+=1
        if position==len(s):break
        value,position=decoder.raw_decode(s,position);objects.append(value)
    return objects
objects=decode(texts['graph']);old=copy.deepcopy(objects);root=objects[0]
by={o['m_ObjectId']:o for o in objects};namespace=uuid.UUID('314d2120-0c50-46c9-8b8c-bb22c2a7d076')
def uid(name):return uuid.uuid5(namespace,name).hex
def add(o):
    assert o['m_ObjectId'] not in by
    objects.append(o);by[o['m_ObjectId']]=o
def slot(node,name):return next(by[r['m_Id']] for r in node['m_Slots'] if by[r['m_Id']]['m_DisplayName']==name)
def source(node,name):
    sid=slot(node,name)['m_Id'];edges=[e for e in root['m_Edges'] if e['m_InputSlot']['m_Node']['m_Id']==node['m_ObjectId'] and e['m_InputSlot']['m_SlotId']==sid]
    assert len(edges)==1,(node['m_FunctionName'],name,len(edges));return copy.deepcopy(edges[0]['m_OutputSlot'])
def edge(src,node,sid):root['m_Edges'].append({'m_OutputSlot':src,'m_InputSlot':{'m_Node':{'m_Id':node['m_ObjectId']},'m_SlotId':sid}})
color=next(o for o in objects if o.get('m_FunctionName')=='NBGraphBaseColor')
vo=next(o for o in objects if o.get('m_FunctionName')=='NBGraphVertexOffset')
word_sources={}
for word in range(4):
    for half in ('Lo16','Hi16'):
        label='CustomDataFlag'+str(word)+half;ref='_NB_'+label
        if word==0:
            word_sources[label]=source(color,label);continue
        template=next(o for o in objects if o.get('m_OverrideReferenceName')=='_NB_CustomDataFlag0'+half)
        template_node=next(o for o in objects if o.get('m_Property',{}).get('m_Id')==template['m_ObjectId'])
        prop=copy.deepcopy(template);prop['m_ObjectId']=uid(ref);prop['m_Guid']={'m_GuidSerialized':str(uuid.uuid5(namespace,ref+':guid'))}
        for key in ('m_Name','m_RefNameGeneratedByDisplayName'):prop[key]=label
        for key in ('m_DefaultReferenceName','m_OverrideReferenceName'):prop[key]=ref
        prop['m_Value']=0.0;prop['m_Hidden']=True;prop['m_FloatType']=0
        node=copy.deepcopy(template_node);node['m_ObjectId']=uid(ref+':node');node['m_Property']={'m_Id':prop['m_ObjectId']}
        output=copy.deepcopy(by[template_node['m_Slots'][0]['m_Id']]);output['m_ObjectId']=uid(ref+':output');output['m_DisplayName']=label;output['m_ShaderOutputName']=label
        output['m_Value']=output['m_DefaultValue']=0.0
        node['m_Slots']=[{'m_Id':output['m_ObjectId']}];add(prop);add(node);add(output)
        root['m_Properties'].append({'m_Id':prop['m_ObjectId']});root['m_Nodes'].append({'m_Id':node['m_ObjectId']})
        word_sources[label]={'m_Node':{'m_Id':node['m_ObjectId']},'m_SlotId':output['m_Id']}
labels=list(word_sources)
changed_nodes=[]
for name in ('NBGraphUVVertex','NBGraphBaseUV','NBGraphVertexOffset','NBGraphBaseColor'):
    node=next(o for o in objects if o.get('m_FunctionName')==name)
    current_names={by[r['m_Id']]['m_DisplayName'] for r in node['m_Slots']}
    sid=max(by[r['m_Id']]['m_Id'] for r in node['m_Slots'])
    template=slot(node,'CustomDataFlag0Lo16') if name=='NBGraphBaseColor' else slot(node,'NB_Flags0Lo16' if name in ('NBGraphBaseUV','NBGraphUVVertex') else 'Flags0Lo16')
    for label in labels:
        if label in current_names:continue
        sid+=1;p=copy.deepcopy(template);p['m_ObjectId']=uid(name+':'+label);p['m_Id']=sid;p['m_DisplayName']=p['m_ShaderOutputName']=label
        p['m_Value']=p['m_DefaultValue']=0.0;add(p);node['m_Slots'].append({'m_Id':p['m_ObjectId']});edge(word_sources[label],node,sid)
    changed_nodes.append(node['m_ObjectId'])
vat=next(o for o in objects if o.get('m_FunctionName')=='NBGraphVATSoftBody')
sid=max(by[r['m_Id']]['m_Id'] for r in vat['m_Slots'])
for label,template,src in [('CustomDataFlag2Lo16',slot(vat,'Flags1Lo16'),word_sources['CustomDataFlag2Lo16']),
    ('CustomDataFlag2Hi16',slot(vat,'Flags1Hi16'),word_sources['CustomDataFlag2Hi16']),
    ('UV0',slot(vat,'UV1'),source(vo,'UV0')),('Custom2',slot(vat,'UV1'),source(vo,'UV2'))]:
    sid+=1;p=copy.deepcopy(template);p['m_ObjectId']=uid('VAT:'+label);p['m_Id']=sid;p['m_DisplayName']=p['m_ShaderOutputName']=label
    add(p);vat['m_Slots'].append({'m_Id':p['m_ObjectId']});edge(src,vat,sid)
changed_nodes.append(vat['m_ObjectId'])
allowed={root['m_ObjectId'],*changed_nodes}
for before in old:
    after=by[before['m_ObjectId']]
    if before['m_ObjectId'] not in allowed:assert before==after,before.get('m_Name')
texts['graph']='\n\n'.join(json.dumps(o,indent=4,ensure_ascii=False) for o in objects)+'\n'

def append_signature(s,function,extra):
    start=s.index('void '+function+'(');end=s.index('    out ',start)
    return s[:end]+''.join('    float '+label+',\n' for label in extra)+s[end:]
all_extra=labels
uv=texts['uv']
for name in ('NBGraphUVVertex_float','NBGraphBaseUV_float','NBGraphBaseUV_half'):uv=append_signature(uv,name,all_extra)
uv=one(uv,'float4 CylinderMatrix3, float FlipbookToggle)','float4 CylinderMatrix3, float FlipbookToggle,\n'+', '.join('float '+label for label in labels)+')')
uv=one(uv,'    parameters.timeY = _Time.y;',
    '    parameters.customDataFlag0 = NBGraphDecodeUInt32(CustomDataFlag0Lo16, CustomDataFlag0Hi16);\n    parameters.customDataFlag3 = NBGraphDecodeUInt32(CustomDataFlag3Lo16, CustomDataFlag3Hi16);\n    parameters.timeY = _Time.y;')
uv=one(uv,'CylinderMatrix3, FlipbookToggle);','CylinderMatrix3, FlipbookToggle, '+', '.join(labels)+');',2)
uv=one(uv,'        resolved, maskResolved, mask2Resolved, mask3Resolved,','        '+', '.join(labels)+',\n        resolved, maskResolved, mask2Resolved, mask3Resolved,')
texts['uv']=uv

vo_text=append_signature(texts['vo'],'NBGraphVertexOffset_float',all_extra)
vo_text=one(vo_text,'    uvParams.timeY = _Time.y;',
    '    uvParams.customDataFlag0 = NBGraphDecodeUInt32(CustomDataFlag0Lo16, CustomDataFlag0Hi16);\n    uvParams.customDataFlag3 = NBGraphDecodeUInt32(CustomDataFlag3Lo16, CustomDataFlag3Hi16);\n    uvParams.timeY = _Time.y;')
vo_text=one(vo_text,'    half4 maskST = (half4)VertexOffsetMaskMap.scaleTranslate;','''    half4 maskST = (half4)VertexOffsetMaskMap.scaleTranslate;
    uint cd1=NBGraphDecodeUInt32(CustomDataFlag1Lo16,CustomDataFlag1Hi16);
    uint cd3=NBGraphDecodeUInt32(CustomDataFlag3Lo16,CustomDataFlag3Hi16);
    mapST.z += GetCustomData(cd1,FLAGBIT_POS_1_CUSTOMDATA_VERTEX_OFFSET_X,0,UV1,UV2);
    mapST.w += GetCustomData(cd1,FLAGBIT_POS_1_CUSTOMDATA_VERTEX_OFFSET_Y,0,UV1,UV2);
    maskST.z += GetCustomData(cd3,FLAGBIT_POS_3_CUSTOMDATA_VERTEX_OFFSET_MASK_X,0,UV1,UV2);
    maskST.w += GetCustomData(cd3,FLAGBIT_POS_3_CUSTOMDATA_VERTEX_OFFSET_MASK_Y,0,UV1,UV2);
    VertexOffsetVec.z=(half)GetCustomData(cd1,FLAGBIT_POS_1_CUSTOMDATA_VERTEXOFFSET_INTENSITY,(half)VertexOffsetVec.z,UV1,UV2);''')
texts['vo']=vo_text

color_text=texts['color'];new_extra=labels[2:]
for precision in ('float','half'):
    color_text=append_signature(color_text,'NBGraphBaseColor_'+precision,new_extra)
    start=color_text.index('void NBGraphBaseColor_'+precision+'(');brace=color_text.index('{',start)
    inject='''
    uint cd0=NBGraphDecodeUInt32(CustomDataFlag0Lo16,CustomDataFlag0Hi16);
    uint cd1=NBGraphDecodeUInt32(CustomDataFlag1Lo16,CustomDataFlag1Hi16);
    uint cd2=NBGraphDecodeUInt32(CustomDataFlag2Lo16,CustomDataFlag2Hi16);
    uint cd3=NBGraphDecodeUInt32(CustomDataFlag3Lo16,CustomDataFlag3Hi16);
    HueShift=(half)GetCustomData(cd0,FLAGBIT_POS_0_CUSTOMDATA_HUESHIFT,(half)HueShift,Custom1,Custom2);
    Contrast=(half)GetCustomData(cd2,FLAGBIT_POS_2_CUSTOMDATA_MAINTEX_CONTRAST,(half)Contrast,Custom1,Custom2);
    Saturability=(half)GetCustomData(cd1,FLAGBIT_POS_1_CUSTOMDATA_SATURATE,(half)Saturability,Custom1,Custom2);
    Dissolve.x=(half)((half)Dissolve.x+GetCustomData(cd0,FLAGBIT_POS_0_CUSTOMDATA_DISSOLVE_INTENSITY,0,Custom1,Custom2));
    Dissolve.z=(half)((half)Dissolve.z+GetCustomData(cd1,FLAGBIT_POS_1_CUSTOMDATA_DISSOLVE_MASK_INTENSITY,0,Custom1,Custom2));
    FresnelUnit.x=(half)((half)FresnelUnit.x+GetCustomData(cd0,FLAGBIT_POS_0_CUSTOMDATA_FRESNEL_OFFSET,0,Custom1,Custom2));
    PNoiseVec4.x+=GetCustomData(cd2,FLAGBIT_POS_2_CUSTOMDATA_DISSOLVE_NOISE1_OFFSET_X,0,Custom1,Custom2);
    PNoiseVec4.y+=GetCustomData(cd2,FLAGBIT_POS_2_CUSTOMDATA_DISSOLVE_NOISE1_OFFSET_Y,0,Custom1,Custom2);
    PNoiseVec4.z+=GetCustomData(cd2,FLAGBIT_POS_2_CUSTOMDATA_DISSOLVE_NOISE2_OFFSET_X,0,Custom1,Custom2);
    PNoiseVec4.w+=GetCustomData(cd2,FLAGBIT_POS_2_CUSTOMDATA_DISSOLVE_NOISE2_OFFSET_Y,0,Custom1,Custom2);
    EmissionUV+=float2(GetCustomData(cd3,FLAGBIT_POS_3_CUSTOMDATA_EMISSION_OFFSET_X,0,Custom1,Custom2),GetCustomData(cd3,FLAGBIT_POS_3_CUSTOMDATA_EMISSION_OFFSET_Y,0,Custom1,Custom2));
    ColorBlendUV+=float2(GetCustomData(cd3,FLAGBIT_POS_3_CUSTOMDATA_COLOR_BLEND_OFFSET_X,0,Custom1,Custom2),GetCustomData(cd3,FLAGBIT_POS_3_CUSTOMDATA_COLOR_BLEND_OFFSET_Y,0,Custom1,Custom2));
    float2 maskCustomOffset=float2(GetCustomData(cd0,FLAGBIT_POS_0_CUSTOMDATA_MASK_OFFSET_X,0,Custom1,Custom2),GetCustomData(cd0,FLAGBIT_POS_0_CUSTOMDATA_MASK_OFFSET_Y,0,Custom1,Custom2));
    if (((cd1>>FLAGBIT_POS_1_CUSTOMDATA_DISSOLVE_OFFSET_X)&8u)!=0u || ((cd1>>FLAGBIT_POS_1_CUSTOMDATA_DISSOLVE_OFFSET_Y)&8u)!=0u)
    {
        half4 dissolveST=(half4)DissolveMap.scaleTranslate;
        dissolveST.z+=GetCustomData(cd1,FLAGBIT_POS_1_CUSTOMDATA_DISSOLVE_OFFSET_X,0,Custom1,Custom2);
        dissolveST.w+=GetCustomData(cd1,FLAGBIT_POS_1_CUSTOMDATA_DISSOLVE_OFFSET_Y,0,Custom1,Custom2);
        DissolveMap.scaleTranslate=dissolveST;
    }
'''
    color_text=color_text[:brace+1]+inject+color_text[brace+1:]
    # Noise gates are consumed before determining whether the original Noise block runs.
    marker='    NoiseMaskToggle *= NBGraphTierAllowNoiseMask > 0.5 ? 1.0 : 0.0;'
    pos=color_text.index(marker,start)+len(marker)
    color_text=color_text[:pos]+'''
    if (NoiseEnabled>0.5)
    {
        if (NB_GRAPH_DEPTH_SHADOW_PASS || round(DistortMode)!=1.0)
        {
            DistortionDirection.x=(half)((half)DistortionDirection.x+GetCustomData(cd2,FLAGBIT_POS_2_CUSTOMDATA_NOISE_DIRECTION_X,0,Custom1,Custom2));
            DistortionDirection.y=(half)((half)DistortionDirection.y+GetCustomData(cd2,FLAGBIT_POS_2_CUSTOMDATA_NOISE_DIRECTION_Y,0,Custom1,Custom2));
        }
        NoiseIntensity=(half)GetCustomData(cd1,FLAGBIT_POS_1_CUSTOMDATA_NOISE_INTENSITY,(half)NoiseIntensity,Custom1,Custom2);
    }
'''+color_text[pos:]
color_text=one(color_text,'    float2 offsetSpeed)\n{','    float2 offsetSpeed, float2 customAfterST = float2(0,0))\n{')
color_text=one(color_text,'offsetSpeed.x == 0.0 && offsetSpeed.y == 0.0)','offsetSpeed.x == 0.0 && offsetSpeed.y == 0.0 && all(customAfterST == 0.0))')
color_text=one(color_text,'    input.timeY = _Time.y;\n    return NBFX_TransformFeatureUVV2(input);','    input.customOffsetAfterST=customAfterST;\n    input.timeY = _Time.y;\n    return NBFX_TransformFeatureUVV2(input);')
color_text=one(color_text,'maskRotation, MaskMapOffsetAnition.xy);','maskRotation, MaskMapOffsetAnition.xy, maskCustomOffset);',2)
texts['color']=color_text
texts['contract']=one(texts['contract'],'    float timeY;','    float timeY;\n    float2 customOffsetAfterST; // Additive host input, zero preserves every existing caller.')
texts['math']=one(texts['math'],'    uv = uv * input.scaleOffset.xy + input.scaleOffset.zw;',
    '    uv = uv * input.scaleOffset.xy + input.scaleOffset.zw;\n    if (any(input.customOffsetAfterST != 0.0)) uv += input.customOffsetAfterST;')

vat_text=append_signature(texts['vat'],'NBGraphVATSoftBody_float',['CustomDataFlag2Lo16','CustomDataFlag2Hi16'])
vat_text=one(vat_text,'    out float3 OutPositionOS','    float4 UV0, float4 Custom2,\n    out float3 OutPositionOS')
vat_text=one(vat_text,'    if (round(VATMode)!=0.0 || round(HoudiniVATSubMode)!=0.0 ||\n        (flags1 & FLAG_BIT_PARTICLE_1_IS_PARTICLE_SYSTEM)!=0u)',
    '    if (round(VATMode)!=0.0 || round(HoudiniVATSubMode)!=0.0)')
vat_text=one(vat_text,'    float uv1r=UV1.x,uv1g=UV1.y;','''    bool particle=(flags1 & FLAG_BIT_PARTICLE_1_IS_PARTICLE_SYSTEM)!=0u;
    if (particle)
    {
        float customFrame=GetCustomData(NBGraphDecodeUInt32(CustomDataFlag2Lo16,CustomDataFlag2Hi16),FLAGBIT_POS_2_CUSTOMDATA_VAT_FRAME,-1.0,UV1,Custom2);
        if (customFrame>=0.0)
        {
            float chosen=saturate(customFrame)*max(FrameCount-1.0,0.0)+1.0;
            selectedFrame=floor(chosen);frameAlpha=frac(chosen);
        }
    }
    float2 vatUV=particle?UV0.zw:UV1.xy;
    float uv1r=vatUV.x,uv1g=vatUV.y;''')
texts['vat']=vat_text

runtime=texts['runtime']
constants=''.join('        private static readonly int GraphCustom'+str(word)+half+' = Shader.PropertyToID("_NB_CustomDataFlag'+str(word)+half+'16");\n' for word in range(4) for half in ('Lo','Hi'))
runtime=one(runtime,'        private static readonly int GraphDistortionMode',constants+'        private static readonly int GraphDistortionMode')
bindings=''.join('            else if (propertyId == CustomDataFlag'+str(word)+'Id) { loId = GraphCustom'+str(word)+'Lo; hiId = GraphCustom'+str(word)+'Hi; }\n' for word in range(4))
runtime=one(runtime,'            else { return false; }',bindings+'            else { return false; }')
runtime=runtime.replace('material.GetInteger(GetCustomDataFlagID(dataIndex))','ReadWord(GetCustomDataFlagID(dataIndex))')
runtime=runtime.replace('material.SetInteger(GetCustomDataFlagID(dataIndex), materialBit)','WriteWord(GetCustomDataFlagID(dataIndex), materialBit)')
for word in range(4):runtime=runtime.replace('material.GetInteger(CustomDataFlag'+str(word)+'Id)','ReadWord(CustomDataFlag'+str(word)+'Id)')
runtime=one(runtime,'            int flag = material.GetInteger(flagID);','            int flag = ReadWord(flagID);')
texts['runtime']=runtime

records=[]
for key,rel in files.items():
    assert (package/rel).read_bytes()==raw[key],rel
    target=out/rel;target.parent.mkdir(parents=True,exist_ok=True);target.write_text(texts[key],encoding='utf-8',newline='\n')
    records.append({'path':rel,'beforeSHA256':hashlib.sha256(raw[key]).hexdigest(),'afterSHA256':hashlib.sha256(target.read_bytes()).hexdigest()})
(out/'manifest.json').write_text(json.dumps({'scope':'Preview only; four unchanged nibble words, Mesh UV1/UV2, runtime hooks and32 host consumers; consumer/stage GPU proof pending',
    'objectsBefore':len(old),'objectsAfter':len(objects),'newKeywords':0,'newPackedBits':0,'oldObjectsExceptRootAndAppendedCFPortsUnchanged':True,
    'records':records},indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps({'files':len(records),'objectsBefore':len(old),'objectsAfter':len(objects),'previewOnly':True}))
