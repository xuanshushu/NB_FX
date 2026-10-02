"""Rebuild the OverrideZ slice from the current root Graph without copying a whole candidate Graph."""
from pathlib import Path
import copy, hashlib, json, uuid
from graph_preview_helpers import GraphPreview

work=Path(__file__).resolve().parent; root=work.parents[1]
package=root/'Packages/NB_FX'; out=work/'overridez-root-rebased-preview'
assert not out.exists(), 'Do not overwrite reviewed candidates'
ns=uuid.UUID('7a2f938c-0c81-47c4-a7c4-7f0f9963ef92')
def sha(data): return hashlib.sha256(data).hexdigest()
def one(text,old,new):
    assert text.count(old)==1,(old,text.count(old))
    return text.replace(old,new)
paths={'legacy':'NBShaders2/Shader/HLSL/NBShaderForwardPass.hlsl',
 'forward':'NBShaders2/ShaderGraph/Passes/NBGraphForwardPass.hlsl',
 'blocks':'NBShaders2/ShaderGraph/Editor/NBGraphDistortionBlocks.cs',
 'target':'NBShaders2/ShaderGraph/Editor/NBGraphUnlitSubTarget.cs',
 'gui':'NBShaders2/Editor/NBShaderGraphGUI.cs',
 'graph':'NBShaders2/ShaderGraph/NBShaderGraph.shadergraph'}
raw={k:(package/p).read_bytes() for k,p in paths.items()}; texts={k:v.decode('utf-8') for k,v in raw.items()}
legacy=texts['legacy']; start=legacy.index('        float OverrideZToDeviceDepth()'); brace=legacy.index('{',start); level=1; end=brace+1
while level: level+=(legacy[end]=='{')-(legacy[end]=='}'); end+=1
body=legacy[brace+1:end-1]
for a,b in [('_ProjectionParams','projectionParams'),('_ZBufferParams','zBufferParams'),('_OverrideZValue','overrideValue'),('unity_OrthoParams.w','orthographic')]:body=body.replace(a,b)
kernelPath='NBShaders2/Shader/HLSL/NBShaderOverrideDepthV1.hlsl'
kernel=(work/'overridez-preview'/kernelPath).read_text()
assert ''.join(body.split()) in ''.join(kernel.split()), 'Original math must be exact'
texts['legacy']=one(legacy[:start]+'''        float OverrideZToDeviceDepth()
        {
            return NBFX_OverrideZDeviceDepthV1(_OverrideZValue,_ProjectionParams,_ZBufferParams,unity_OrthoParams.w);
        }'''+legacy[end:], '    #include "NBShaderSurfaceV1.hlsl"','    #include "NBShaderSurfaceV1.hlsl"\n    #include "NBShaderOverrideDepthV1.hlsl"')
texts['forward']=one(texts['forward'],'    out half4 outColor : SV_Target0','''    out half4 outColor : SV_Target0
#if defined(_OVERRIDE_Z)
    , out float outDepth : SV_Depth
#endif''')
texts['forward']=one(texts['forward'],'    outColor = min(outColor, 1000);','''    outColor = min(outColor, 1000);
#if defined(_OVERRIDE_Z)
    outDepth=surface.NBOverrideDeviceDepth;
#endif''')
texts['blocks']=one(texts['blocks'],'        public struct SurfaceDescription\n        {','''        public struct SurfaceDescription
        {
            public static BlockFieldDescriptor OverrideDeviceDepth = new BlockFieldDescriptor(
                "SurfaceDescription", "NBOverrideDeviceDepth", "NB Override Device Depth",
                "SURFACEDESCRIPTION_NB_OVERRIDE_DEVICE_DEPTH",
                new FloatControl(0.0f), ShaderStage.Fragment);''')
t=texts['target']; t=one(t,'existing.Length + 2','existing.Length + 3')
t=one(t,'            blocks[existing.Length + 1] = NBGraphDistortionBlocks.SurfaceDescription.NoiseMask;','            blocks[existing.Length + 1] = NBGraphDistortionBlocks.SurfaceDescription.NoiseMask;\n            blocks[existing.Length + 2] = NBGraphDistortionBlocks.SurfaceDescription.OverrideDeviceDepth;')
t=one(t,'            context.AddBlock(NBGraphDistortionBlocks.SurfaceDescription.NoiseMask);','            context.AddBlock(NBGraphDistortionBlocks.SurfaceDescription.NoiseMask);\n            context.AddBlock(NBGraphDistortionBlocks.SurfaceDescription.OverrideDeviceDepth);')
t=one(t,'            pass.keywords = keywords;','''            // Original NB fragment axis, only on normal Forward.
            keywords.Add(new KeywordDescriptor
            {
                displayName = "NB Override Z", referenceName = "_OVERRIDE_Z",
                type = KeywordType.Boolean, definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Local, stages = KeywordShaderStage.Fragment,
            });
            pass.keywords = keywords;'''); texts['target']=t
texts['gui']=one(texts['gui'],'            if (material == null) return;','''            if (material == null) return;
            if (material.HasProperty("_OverrideZ_Toggle"))
                SetExistingKeyword(material, "_OVERRIDE_Z", material.GetFloat("_OverrideZ_Toggle") > 0.5f);''')
texts['gui']=one(texts['gui'],'            => _urpGUI.AssignNewShaderToMaterial(material, oldShader, newShader);','''        {
            _urpGUI.AssignNewShaderToMaterial(material, oldShader, newShader);
            SyncSixWayKeywords(material);
        }''')
for p in [kernelPath,kernelPath+'.meta','NBShaders2/ShaderGraph/NBGraphOverrideDepth.hlsl','NBShaders2/ShaderGraph/NBGraphOverrideDepth.hlsl.meta']:
    paths[p]=p; texts[p]=(work/'overridez-preview'/p).read_text()
g=GraphPreview(package/paths['graph'],str(ns)); uv=next(o for o in g.objects if o.get('m_FunctionName')=='NBGraphBaseUV')
floatTemplate=next(o for o in g.objects if o.get('m_OverrideReferenceName')=='_AlphaAll')
def scalarProperty(ref,label,value):
    existing=g.source_for_property(ref)
    if existing:return existing
    prop=copy.deepcopy(floatTemplate); prop['m_ObjectId']=g.uid(ref); prop['m_Guid']={'m_GuidSerialized':str(uuid.uuid5(ns,ref+':guid'))}
    for key in ['m_Name','m_RefNameGeneratedByDisplayName']:prop[key]=label
    for key in ['m_DefaultReferenceName','m_OverrideReferenceName']:prop[key]=ref
    prop['m_Hidden']=False; prop['m_Value']=float(value); prop['m_FloatType']=0
    oldNode=next(o for o in g.objects if o.get('m_Property',{}).get('m_Id')==floatTemplate['m_ObjectId'])
    node=copy.deepcopy(oldNode); node['m_ObjectId']=g.uid(ref+':node'); node['m_Property']={'m_Id':prop['m_ObjectId']}
    slot=copy.deepcopy(g.by[oldNode['m_Slots'][0]['m_Id']]); slot['m_ObjectId']=g.uid(ref+':out'); slot['m_DisplayName']=slot['m_ShaderOutputName']=label; slot['m_Value']=slot['m_DefaultValue']=float(value)
    node['m_Slots']=[{'m_Id':slot['m_ObjectId']}]
    for obj in [prop,node,slot]:g.add(obj)
    g.g['m_Properties'].append({'m_Id':prop['m_ObjectId']}); g.g['m_Nodes'].append({'m_Id':node['m_ObjectId']})
    return {'m_Node':{'m_Id':node['m_ObjectId']},'m_SlotId':slot['m_Id']}
pixelId=g.slot(uv,'PixelPosition')['m_Id']; pixel=next(e['m_OutputSlot'] for e in g.g['m_Edges'] if e['m_InputSlot']['m_Node']['m_Id']==uv['m_ObjectId'] and e['m_InputSlot']['m_SlotId']==pixelId)
node=copy.deepcopy(uv); node['m_ObjectId']=g.uid('NBGraphOverrideDepth'); node['m_Name']='NBGraphOverrideDepth (Custom Function)'; node['m_FunctionName']='NBGraphOverrideDepth'; node['m_FunctionSource']=uuid.uuid5(ns,'NBShaders2/ShaderGraph/NBGraphOverrideDepth.hlsl').hex; node['m_Slots']=[]; node['m_Precision']=1
scalar=next(o for o in g.objects if o.get('m_Type','').endswith('.Vector1MaterialSlot'))
inputs=[('OverrideZToggle',scalarProperty('_OverrideZ_Toggle','Override Z',0),scalar),('OverrideZValue',scalarProperty('_OverrideZValue','Override Z Eye Depth',1000),scalar),('PixelPosition',pixel,g.slot(uv,'PixelPosition'))]
for i,(label,source,template) in enumerate(inputs):
    s=copy.deepcopy(template); s['m_ObjectId']=g.uid('NBGraphOverrideDepth:'+label); s['m_Id']=i; s['m_DisplayName']=s['m_ShaderOutputName']=label; s['m_SlotType']=0; s['m_StageCapability']=2; g.add(s); node['m_Slots'].append({'m_Id':s['m_ObjectId']}); g.g['m_Edges'].append({'m_OutputSlot':source,'m_InputSlot':{'m_Node':{'m_Id':node['m_ObjectId']},'m_SlotId':i}})
s=copy.deepcopy(scalar); s['m_ObjectId']=g.uid('NBGraphOverrideDepth:DeviceDepth'); s['m_Id']=3; s['m_DisplayName']=s['m_ShaderOutputName']='DeviceDepth'; s['m_SlotType']=1; s['m_StageCapability']=2; g.add(s); node['m_Slots'].append({'m_Id':s['m_ObjectId']}); g.add(node); g.g['m_Nodes'].append({'m_Id':node['m_ObjectId']})
template=next(o for o in g.objects if o.get('m_SerializedDescriptor')=='SurfaceDescription.AlphaClipThreshold')
block=copy.deepcopy(template); block['m_ObjectId']=g.uid('NBOverrideDeviceDepth:block'); block['m_Name']=block['m_SerializedDescriptor']='SurfaceDescription.NBOverrideDeviceDepth'
s=copy.deepcopy(g.by[template['m_Slots'][0]['m_Id']]); s['m_ObjectId']=g.uid('NBOverrideDeviceDepth:block:slot'); s['m_DisplayName']=s['m_ShaderOutputName']='NBOverrideDeviceDepth'; s['m_Value']=s['m_DefaultValue']=0.; block['m_Slots']=[{'m_Id':s['m_ObjectId']}]
g.add(block); g.add(s); g.g['m_Nodes'].append({'m_Id':block['m_ObjectId']}); g.g['m_FragmentContext']['m_Blocks'].append({'m_Id':block['m_ObjectId']}); g.g['m_Edges'].append({'m_OutputSlot':{'m_Node':{'m_Id':node['m_ObjectId']},'m_SlotId':3},'m_InputSlot':{'m_Node':{'m_Id':block['m_ObjectId']},'m_SlotId':s['m_Id']}})
texts['graph']=g.serialize([]); records=[]
for key,path in paths.items():
    p=out/path; p.parent.mkdir(parents=True,exist_ok=True); p.write_text(texts[key],encoding='utf-8',newline='\n')
    records.append({'path':path,'beforeSHA256':sha(raw[key]) if key in raw else None,'afterSHA256':sha(p.read_bytes())})
assert all(e in g.g['m_Edges'] for e in g.before[0]['m_Edges'])
receipt={'scope':'Current root-derived OverrideZ normal Forward slice with assignment lifecycle sync; not installed',
 'records':records,'graphObjectsBefore':len(g.before),'graphObjectsAfter':len(g.objects),
 'allOldNonRootObjectsUnchanged':True,'allOldEdgesRetained':True,'newPackedBits':0,'newKeywordNames':0}
(out/'manifest.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps({k:receipt[k] for k in ('scope','graphObjectsBefore','graphObjectsAfter','allOldEdgesRetained')}))
