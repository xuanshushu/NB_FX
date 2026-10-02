"""Preview custom-space vertex chain; consumer signatures remain a next step."""
from pathlib import Path
import copy,json,hashlib
from graph_preview_helpers import GraphPreview
work=Path(__file__).resolve().parent;root=work.parents[1];package=root/'.utmp/NBFXMeshValidation-20261002/Packages/NB_FX'
path='NBShaders2/ShaderGraph/NBShaderGraph.shadergraph';g=GraphPreview(package/path,'52b6057d-6a1c-4f59-a34a-78ae478f71ca');out=work/'customlocal-wiring-preview';assert not out.exists()
vat=next(o for o in g.objects if o.get('m_FunctionName')=='NBGraphVATSoftBody');vo=next(o for o in g.objects if o.get('m_FunctionName')=='NBGraphVertexOffset')
def source(node,label):
    sid=g.slot(node,label)['m_Id'];matches=[e['m_OutputSlot'] for e in g.g['m_Edges'] if e['m_InputSlot']['m_Node']['m_Id']==node['m_ObjectId'] and e['m_InputSlot']['m_SlotId']==sid];assert len(matches)==1
    return copy.deepcopy(matches[0])
def output(node,label):return {'m_Node':{'m_Id':node['m_ObjectId']},'m_SlotId':g.slot(node,label)['m_Id']}
def replace_input(node,label,new_source):
    sid=g.slot(node,label)['m_Id'];matches=[e for e in g.g['m_Edges'] if e['m_InputSlot']['m_Node']['m_Id']==node['m_ObjectId'] and e['m_InputSlot']['m_SlotId']==sid];assert len(matches)==1
    matches[0]['m_OutputSlot']=new_source
raw={'PositionOS':source(vat,'PositionOS'),'NormalOS':source(vat,'NormalOS'),'TangentOS':source(vo,'TangentOS')}
bitangent=copy.deepcopy(next(o for o in g.objects if o.get('m_Type','').endswith('BitangentVectorNode')));bitangent['m_ObjectId']=g.uid('customlocal:raw-object-bitangent');bitangent['m_Space']=0
bslot=copy.deepcopy(g.by[bitangent['m_Slots'][0]['m_Id']]);bslot['m_ObjectId']=g.uid('customlocal:raw-object-bitangent:out');bitangent['m_Slots']=[{'m_Id':bslot['m_ObjectId']}]
g.add(bitangent);g.add(bslot);g.g['m_Nodes'].append({'m_Id':bitangent['m_ObjectId']});raw['BitangentOS']={'m_Node':{'m_Id':bitangent['m_ObjectId']},'m_SlotId':bslot['m_Id']}
properties={'CustomLocalToggle':g.property('_NB_CustomLocalTransform','Custom Local Transform',0)}
for stem in ['LocalToWorld','WorldToLocal']:
    for i in range(4):properties[stem+str(i)]=g.vector4_property('_NB_Custom'+stem+str(i),'Custom '+stem+' '+str(i),[int(i==j) for j in range(4)])
adapter=json.loads((work/'customlocal-adapter-preview/manifest.json').read_text(encoding='utf-8'))
def function(name,inputs,outputs):
    node=copy.deepcopy(vat);node['m_ObjectId']=g.uid(name);node['m_Name']=name+' (Custom Function)';node['m_FunctionName']=name;node['m_FunctionSource']=adapter['guid'];node['m_Slots']=[]
    for sid,(label,kind,src) in enumerate(inputs+[(n,k,None) for n,k in outputs]):
        template=g.slot(vat,'UV1' if kind=='vec4' else 'VATToggle' if kind=='scalar' else 'PositionOS')
        slot=copy.deepcopy(template);slot['m_ObjectId']=g.uid(name+':'+label);slot['m_Id']=sid;slot['m_DisplayName']=slot['m_ShaderOutputName']=label;slot['m_SlotType']=0 if sid<len(inputs) else 1
        slot['m_StageCapability']=1;g.add(slot);node['m_Slots'].append({'m_Id':slot['m_ObjectId']})
        if src is not None:g.g['m_Edges'].append({'m_OutputSlot':src,'m_InputSlot':{'m_Node':{'m_Id':node['m_ObjectId']},'m_SlotId':sid}})
    g.add(node);g.g['m_Nodes'].append({'m_Id':node['m_ObjectId']});return node
matrix_inputs=[('CustomLocalToggle','scalar',properties['CustomLocalToggle'])]+[(n,'vec4',s) for n,s in properties.items() if n!='CustomLocalToggle']
pre=function('NBGraphCustomLocalBefore',[(n,'vec3',s) for n,s in raw.items()]+matrix_inputs,[('OutPositionOS','vec3'),('OutNormalOS','vec3'),('OutTangentOS','vec3'),('CustomSign','scalar')])
replace_input(vat,'PositionOS',output(pre,'OutPositionOS'));replace_input(vat,'NormalOS',output(pre,'OutNormalOS'));replace_input(vo,'TangentOS',output(pre,'OutTangentOS'))
post=function('NBGraphCustomLocalAfter',[(n,'vec3',output(vo,'Out'+n)) for n in ['PositionOS','NormalOS','TangentOS']]+matrix_inputs,[('OutPositionOS','vec3'),('OutNormalOS','vec3'),('OutTangentOS','vec3')])
for name,label in [('VertexDescription.Position','OutPositionOS'),('VertexDescription.Normal','OutNormalOS'),('VertexDescription.Tangent','OutTangentOS')]:
    block=next(o for o in g.objects if o.get('m_Name')==name);slot=g.by[block['m_Slots'][0]['m_Id']];replace_input(block,slot['m_DisplayName'],output(post,label))
serialized=g.serialize([vat['m_ObjectId'],vo['m_ObjectId']]);target=out/path;target.parent.mkdir(parents=True,exist_ok=True);target.write_text(serialized,encoding='utf-8',newline='\n')
(out/'manifest.json').write_text(json.dumps({'scope':'Preview only: custom pre→VAT→VO→custom post→SG blocks, preserve pre-VO custom-space interpolators. Nine exposed render inputs, existing IDs/targets preserved. NOT ready to install: UV/Fog/VO-world directions, vertex/world SixWay, fragment TBN and helper writer need same-slice bindings.',
 'beforeSHA256':hashlib.sha256((package/path).read_bytes()).hexdigest(),'afterSHA256':hashlib.sha256(target.read_bytes()).hexdigest(),'objectsBefore':len(g.before),'objectsAfter':len(g.objects),
 'properties':list(properties),'customSignOutput':output(pre,'CustomSign'),'preTangentOutput':output(pre,'OutTangentOS'),'installed':False},indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps({'previewObjects':len(g.objects),'newProperties':len(properties),'installed':False}))
