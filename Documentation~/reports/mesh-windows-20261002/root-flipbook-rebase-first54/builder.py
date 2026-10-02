"""Root F0 preview generation only. Exact source hashes; docs-only HEAD allowed.
Default audit is read-only except its own JSON. --generate creates a new child
candidate; never writes Root, isolation, official packages or Git.
"""
import argparse,ast,copy,difflib,hashlib,json,re,subprocess,uuid
from pathlib import Path
HERE=Path(__file__).resolve().parent
WORK=HERE.parent
ROOT=HERE.parents[2]/'Packages/NB_FX'
REL=dict(graph='NBShaders2/ShaderGraph/NBShaderGraph.shadergraph',baseuv='NBShaders2/ShaderGraph/NBGraphBaseUV.hlsl',
 vertex='NBShaders2/ShaderGraph/NBGraphVertexOffset.hlsl',color='NBShaders2/ShaderGraph/NBGraphBaseColor.hlsl',
 shared='NBShaders2/Shader/HLSL/NBShaderUVV1.hlsl',contract='NBShaders2/Shader/HLSL/NBShaderSharedContractV1.hlsl',
 legacy='NBShaders2/Shader/HLSL/NBShaderInput.hlsl',forward='NBShaders2/Shader/HLSL/NBShaderForwardPass.hlsl',
 helper='XuanXuanRenderUtility/Runtime/AnimationSheetHelper.cs',shader='NBShaders2/Shader/NBShader.shader')
def sha(data):return hashlib.sha256(data).hexdigest()
def save(path,obj):path.write_text(json.dumps(obj,indent=2,ensure_ascii=False)+'\n',encoding='utf-8',newline='\n')
def load_functions(path,names,env):
    tree=ast.parse(path.read_text(encoding='utf-8'))
    selected=[n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name in names]
    assert {n.name for n in selected}==names
    exec(compile(ast.Module(body=selected,type_ignores=[]),str(path),'exec'),env)
    return env
def libraries():
    base=load_functions(WORK/'flipbook/rebound-preview.py',
        {'uid','one','decode','patch_shared','patch_contract','patch_legacy','patch_forward','patch_color','patch_helper','patch_graph'},
        dict(copy=copy,hashlib=hashlib,json=json,uuid=uuid,Path=Path,NS=uuid.UUID('fc8d41de-5b0e-439a-830d-a917b720b61b')))
    rebase=load_functions(WORK/'rebase_flipbook_uvp.py',{'patch_baseuv','patch_vertex','patch_graph'},
        dict(copy=copy,json=json,uuid=uuid,one=base['one'],g=base,original_graph_patch=base['patch_graph'],
             namespace=uuid.UUID('ca36b26e-3429-4b57-a414-08182afc5d11')))
    return base,rebase
def verify(expected):
    head=subprocess.check_output(['git','-C',str(ROOT),'rev-parse','HEAD'],text=True).strip()
    subprocess.run(['git','-C',str(ROOT),'merge-base','--is-ancestor',expected['sourceCommit'],head],check=True)
    for rel,digest in expected['rootHashes'].items():assert sha((ROOT/rel).read_bytes())==digest,('Source changed',rel)
    for item in expected['toolInputs']:assert sha(Path(item['path']).read_bytes())==item['sha256'],item['path']
    return head
def signature(text,name):
    a=text.index('void '+name+'(')+len('void '+name+'(');b=text.index(')',a);sig=[]
    for part in text[a:b].split(','):
        m=re.fullmatch(r'\s*(out )?(float[234]?|half[234]?|UnityTexture2D)\s+(\w+)\s*',part);assert m,(name,part)
        sig.append(dict(direction=1 if m[1] else 0,type=m[2],name=m[3]))
    return sig
def check_cf(objects,name,text):
    by={o['m_ObjectId']:o for o in objects};node=next(o for o in objects if o.get('m_FunctionName')==name)
    types={'Vector1MaterialSlot':'float','Vector2MaterialSlot':'float2','UVMaterialSlot':'float2','Vector3MaterialSlot':'float3','Vector4MaterialSlot':'float4','Texture2DInputMaterialSlot':'UnityTexture2D'}
    slots=[]
    for direction in (0,1):
        for ref in node['m_Slots']:
            s=by[ref['m_Id']]
            if s['m_SlotType']==direction:
                kind=s['m_Type'].split('.')[-1]
                if kind=='DynamicVectorMaterialSlot':
                    value=s['m_Value'];width=len(value) if isinstance(value,dict) else 1
                    inferred='float'+(str(width) if width>1 else '')
                else:inferred=types[kind]
                slots.append(dict(direction=direction,type=inferred,name=s['m_DisplayName']))
    assert signature(text,name+'_float')==slots,(name,signature(text,name+'_float'),slots)
    return dict(inputs=sum(s['direction']==0 for s in slots),outputs=sum(s['direction']==1 for s in slots),ABIExact=True)
def audit(old,new,outputs,base):
    by={o['m_ObjectId']:o for o in new};assert len(by)==len(new)
    owned={'NBGraphBaseUV','NBGraphBaseColor','NBGraphUVVertex','NBGraphVertexOffset'}
    allowed={old[0]['m_ObjectId']}|{o['m_ObjectId'] for o in old if o.get('m_FunctionName') in owned or o.get('m_Type')=='UnityEditor.ShaderGraph.CategoryData'}
    for obj in old:
        assert obj['m_ObjectId'] in by
        if obj['m_ObjectId'] not in allowed:assert by[obj['m_ObjectId']]==obj,obj['m_ObjectId']
        if obj.get('m_FunctionName') in owned:assert by[obj['m_ObjectId']]['m_Slots'][:len(obj['m_Slots'])]==obj['m_Slots']
    for key in ('m_Edges','m_Properties','m_Nodes'):
        assert new[0][key][:len(old[0][key])]==old[0][key]
    for key in ('m_VertexContext','m_FragmentContext','m_ActiveTargets','m_Keywords','m_GraphPrecision'):
        assert new[0][key]==old[0][key],key
    cf={n:check_cf(new,n,outputs[k]) for n,k in [('NBGraphBaseUV','baseuv'),('NBGraphUVVertex','baseuv'),('NBGraphVertexOffset','vertex'),('NBGraphBaseColor','color')]}
    slots={o['m_ObjectId']:{by[r['m_Id']]['m_Id'] for r in o.get('m_Slots',[])} for o in new if 'm_Slots' in o}
    adjacency={n:[] for n in slots};fanins=set()
    for e in new[0]['m_Edges']:
        for side in ('m_OutputSlot','m_InputSlot'):
            q=e[side];assert q['m_SlotId'] in slots[q['m_Node']['m_Id']]
        q=e['m_InputSlot'];key=(q['m_Node']['m_Id'],q['m_SlotId']);assert key not in fanins;fanins.add(key)
        adjacency[e['m_OutputSlot']['m_Node']['m_Id']].append(q['m_Node']['m_Id'])
    for ci in new:
        if ci.get('customBlockNodeName'):
            block=next(o for o in new if o.get('m_SerializedDescriptor','').split('#')[0]=='VertexDescription.'+ci['customBlockNodeName'])
            adjacency[block['m_ObjectId']].append(ci['m_ObjectId'])
    visited={}
    def visit(n):
        assert visited.get(n)!=1,('Stage loop',n)
        if visited.get(n)==2:return
        visited[n]=1
        for child in adjacency[n]:visit(child)
        visited[n]=2
    for n in adjacency:visit(n)
    return dict(CFs=cf,oldNonOwnedObjectsExact=True,oldEdgesAndSlotsExact=True,oldBlocksTargetsConditionsExact=True,
                validEdgesSingleFanIn=True,noCyclesIncludingCI=True,OVZAndUVPMatrixVisibilityExact=True)
def main():
    parser=argparse.ArgumentParser();parser.add_argument('--generate',action='store_true');parser.add_argument('--out',default='generated');a=parser.parse_args()
    expected=json.loads((HERE/'inputs.json').read_text(encoding='utf-8'));head=verify(expected);base,rebase=libraries()
    raw={k:(ROOT/r).read_bytes() for k,r in REL.items()};text={k:raw[k].decode('utf-8').replace('\r\n','\n') for k in raw}
    old=base['decode'](text['graph']);assert len(old)==1132
    beforeCF={n:check_cf(old,n,text[k]) for n,k in [('NBGraphBaseUV','baseuv'),('NBGraphUVVertex','baseuv'),('NBGraphVertexOffset','vertex'),('NBGraphBaseColor','color')]}
    assert not any(o.get('m_OverrideReferenceName')=='_FlipbookBlending' for o in old)
    save(HERE/'input-audit.json',dict(sourceCommit=expected['sourceCommit'],currentHEAD=head,ancestorVerified=True,exactRootHashes=True,objects=1132,CFs=beforeCF,graphWritten=False))
    if not a.generate:print(json.dumps(dict(inputAudit=str(HERE/'input-audit.json'),graphWritten=False)));return
    out=(HERE/a.out).resolve();assert out.is_relative_to(HERE) and out!=HERE and not out.exists()
    graph,graphAudit=rebase['patch_graph'](text['graph']);new=base['decode'](graph)
    # Existing exposed rendering inputs must have VFX slots. Keep every old
    # hidden GUI property exact; only TWO new F0 rendered fields become visible.
    visible=set()
    for obj in new:
        if obj.get('m_OverrideReferenceName') in ('_BaseMap_AnimationSheetBlend_ST','_AnimationSheetHelperBlendIntensity'):
            obj['m_Hidden']=False;visible.add(obj['m_OverrideReferenceName'])
    assert len(visible)==2
    graph='\n\n'.join(json.dumps(o,indent=4,ensure_ascii=False) for o in new)+'\n'
    outputs=dict(graph=graph,baseuv=rebase['patch_baseuv'](text['baseuv']),vertex=rebase['patch_vertex'](text['vertex']),
                 **{k:base['patch_'+k](text[k]) for k in ('color','shared','contract','legacy','forward')})
    graphHelper=base['patch_helper'](text['helper'])
    beforeLife=(WORK/'overridez-combined-before-root-slice/XuanXuanRenderUtility/Runtime/AnimationSheetHelper.cs').read_text(encoding='utf-8')
    assert graphHelper==beforeLife,'Root-derived Graph helper differs from reviewed lifecycle input; stop, do not copy over new changes.'
    outputs['helper']=(WORK/'flipbook-lifecycle-preview/XuanXuanRenderUtility/Runtime/AnimationSheetHelper.cs').read_text(encoding='utf-8')
    anchor='        [ToggleOff] _FlipbookBlending ("__flipbookblending_Toggle", Float) = 0.0'
    assert '_BaseMap_AnimationSheetBlend_ST (' not in text['shader'] and '_AnimationSheetHelperBlendIntensity (' not in text['shader']
    outputs['shader']=base['one'](text['shader'],anchor,
        '        [HideInInspector] _BaseMap_AnimationSheetBlend_ST ("AnimationSheetHelper next frame ST", Vector) = (0, 0, 0, 0)\n'
        '        [HideInInspector] _AnimationSheetHelperBlendIntensity ("AnimationSheetHelper blend weight", Float) = 0\n'+anchor)
    structural=audit(old,new,outputs,base);verify(expected)
    out.mkdir(parents=True);records=[]
    for key,value in outputs.items():
        dest=out/REL[key];dest.parent.mkdir(parents=True,exist_ok=True);dest.write_text(value,encoding='utf-8',newline='\n')
        records.append(dict(path=REL[key],beforeSHA256=sha(raw[key]),afterSHA256=sha(dest.read_bytes())))
        if key!='graph':(out/(Path(REL[key]).name+'.diff')).write_text(''.join(difflib.unified_diff(text[key].splitlines(True),value.splitlines(True))),encoding='utf-8',newline='\n')
    for item in expected['validationInputs']:
        dest=out/item['targetPath'];dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(Path(item['path']).read_bytes())
        records.append(dict(path=item['targetPath'],beforeSHA256=None,afterSHA256=sha(dest.read_bytes()),kind='strict validation'))
    save(out/'manifest.json',dict(scope='Root-derived F0+reviewed Helper lifecycle+SRP properties only; no VAT/CustomLocal/GUI2/Target merge',sourceCommit=expected['sourceCommit'],currentHEAD=head,
        records=records,graphAudit=graphAudit,structuralAudit=structural,renderInputVisibilityFix=list(sorted(visible)),
        graphObjectsBefore=len(old),graphObjectsAfter=len(new),newKeywords=0,newPackedBits=0,ranUnity=False,installed=False,productWritten=False,fullMeshGatePassed=False))
    print(json.dumps(dict(manifest=str(out/'manifest.json'),objectsBefore=len(old),objectsAfter=len(new),structuralAudit=structural)))
if __name__=='__main__':main()
