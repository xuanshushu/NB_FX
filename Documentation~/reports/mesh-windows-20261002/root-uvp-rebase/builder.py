#!/usr/bin/env python3
"""Root UVP rebase. Default/--audit-inputs never generates or writes a Graph.
The main Agent alone executes --generate, which writes only a NEW child output
directory here. Current root bytes/HEAD must match the prepared input manifest.
"""
import argparse, ast, copy, difflib, hashlib, json, re, subprocess, uuid
from pathlib import Path

HERE = Path(__file__).resolve().parent
PACKAGE = HERE.parents[2] / 'Packages/NB_FX'
EXPECTED = HERE / 'inputs.json'
BASE = HERE.parent / 'uvp/rebound-preview.py'
SM_PROOF = HERE.parent / 'overridez-preview/NBShaders2/ShaderGraph/Editor/NBGraphUnlitSubTarget.cs'
REL = {
 'uv': 'NBShaders2/ShaderGraph/NBGraphBaseUV.hlsl',
 'vertex': 'NBShaders2/ShaderGraph/NBGraphVertexOffset.hlsl',
 'host': 'NBShaders2/ShaderGraph/Editor/NBGraphUnlitSubTarget.cs',
 'graph': 'NBShaders2/ShaderGraph/NBShaderGraph.shadergraph',
}
def sha(data): return hashlib.sha256(data).hexdigest()
def save(path, obj): path.write_text(json.dumps(obj, indent=2, ensure_ascii=False)+'\n', encoding='utf-8', newline='\n')

def helpers():
    # Load only reviewed pure function definitions and the three static parameter
    # lists. Do NOT import/run the historical module's writer or wholeGraph code.
    tree = ast.parse(BASE.read_text(encoding='utf-8'))
    funcs = {'uid','hash','once','decode','input_signature','decls','args',
             'patch_uv','patch_vertex','patch_host','patch_graph'}
    constants = {'POSITION_PARAMS','SELECTOR_PARAMS','VERTEX_MAIN_PARAMS'}
    selected = [n for n in tree.body if isinstance(n, ast.FunctionDef) and n.name in funcs or
                isinstance(n, ast.Assign) and len(n.targets)==1 and isinstance(n.targets[0],ast.Name) and n.targets[0].id in constants]
    assert {n.name for n in selected if isinstance(n,ast.FunctionDef)} == funcs
    env = dict(Path=Path,copy=copy,difflib=difflib,hashlib=hashlib,json=json,re=re,uuid=uuid,
               NS=uuid.UUID('9f8f4842-eb4e-4b7b-9d38-cdf8f87773eb'))
    exec(compile(ast.Module(body=selected,type_ignores=[]),str(BASE),'exec'),env)
    return env

def signature(text, name):
    start = text.index('void '+name+'(') + len('void '+name+'(')
    end = text.index(')',start)
    result = []
    for part in text[start:end].split(','):
        match = re.fullmatch(r'\s*(out )?(float[234]?|half[234]?|UnityTexture2D)\s+(\w+)\s*',part)
        assert match,(name,part)
        result.append(dict(direction=1 if match[1] else 0,type=match[2],name=match[3]))
    return result

def check_cf(objects, node_name, text):
    by = {o['m_ObjectId']:o for o in objects}
    node = next(o for o in objects if o.get('m_FunctionName')==node_name)
    slots = []
    types = {'Vector1MaterialSlot':'float','Vector2MaterialSlot':'float2','UVMaterialSlot':'float2',
             'Vector3MaterialSlot':'float3','Vector4MaterialSlot':'float4','Texture2DInputMaterialSlot':'UnityTexture2D'}
    for direction in (0,1):
        for ref in node['m_Slots']:
            slot = by[ref['m_Id']]
            if slot['m_SlotType']!=direction: continue
            slots.append(dict(direction=direction,type=types[slot['m_Type'].split('.')[-1]],name=slot['m_DisplayName']))
    actual = signature(text,node_name+'_float')
    assert actual==slots,(node_name,actual,slots)
    return dict(node=node['m_ObjectId'],inputs=sum(s['direction']==0 for s in slots),outputs=sum(s['direction']==1 for s in slots),exactSignature=True)

def input_audit(expected, h):
    head = subprocess.check_output(['git','-C',str(PACKAGE),'rev-parse','HEAD'],text=True).strip()
    assert head==expected['packageHEAD'],('Root HEAD changed; re-audit instead of overwriting',head)
    for rel, digest in expected['rootHashes'].items(): assert sha((PACKAGE/rel).read_bytes())==digest,rel
    for item in expected['toolInputs']: assert sha(Path(item['path']).read_bytes())==item['sha256'],item['path']
    objects = h['decode']((PACKAGE/REL['graph']).read_text(encoding='utf-8'))
    assert len(objects)==expected['graphObjects']
    checks = {name:check_cf(objects,name,(PACKAGE/rel).read_text(encoding='utf-8')) for name,rel in
              [('NBGraphBaseUV',REL['uv']),('NBGraphVertexOffset',REL['vertex']),('NBGraphOverrideDepth','NBShaders2/ShaderGraph/NBGraphOverrideDepth.hlsl')]}
    existing = {o.get('m_OverrideReferenceName') or o.get('m_DefaultReferenceName') for o in objects if o.get('m_Type','').endswith('ShaderProperty')}
    by={o['m_ObjectId']:o for o in objects}
    def upstream(function, slot_name):
        node=next(o for o in objects if o.get('m_FunctionName')==function)
        slot=next(by[r['m_Id']] for r in node['m_Slots'] if by[r['m_Id']]['m_DisplayName']==slot_name)
        edges=[e['m_OutputSlot'] for e in objects[0]['m_Edges'] if e['m_InputSlot']['m_Node']['m_Id']==node['m_ObjectId'] and e['m_InputSlot']['m_SlotId']==slot['m_Id']]
        assert len(edges)==1,(function,slot_name,edges)
        return edges[0]
    pre=upstream('NBGraphFogVertex','PositionOS');assert pre==upstream('NBGraphVertexOffset','PositionOS')
    pre_node=by[pre['m_Node']['m_Id']];assert pre_node['m_Type']=='UnityEditor.ShaderGraph.PositionNode' and pre_node['m_Space']==0
    world=upstream('NBGraphBaseColor','PositionWS');world_node=by[world['m_Node']['m_Id']]
    assert world_node['m_Type']=='UnityEditor.ShaderGraph.PositionNode' and world_node['m_Space']==2
    spatial = ['_WorldSpaceUVModeSelector','_ObjectSpaceUVModeSelector','_CylinderUVRotate','_CylinderUVPosOffset']+['_CylinderMatrix'+str(i) for i in range(4)]
    assert not (existing & set(spatial)),('Existing spatial property conflict',existing & set(spatial))
    host = (PACKAGE/REL['host']).read_text(encoding='utf-8')
    assert 'NB_GRAPH_DEFERRED_DISTORT_PASS' not in host
    assert 'WithNBInterpolatorShaderModel' not in host
    assert 'referenceName = "_OVERRIDE_Z"' in host
    return dict(scope='Read-only current root compatibility; no generated Graph or Unity run',packageHEAD=head,
                graphObjects=len(objects),graphEdges=len(objects[0]['m_Edges']),CFs=checks,
                newSpatialPropertiesAbsent=True,OVZRetained=True,preOffsetSource=pre,worldSource=world,
                signatureConflicts=[],rootHashes=expected['rootHashes'])

def sm45_patch(host, h):
    # Extract just the already validated modern-interpolator method, never copy
    # the combined SubTarget with VAT, CustomLocal or extra pass decisions.
    proof = SM_PROOF.read_text(encoding='utf-8')
    a = proof.index('        static PassDescriptor WithNBInterpolatorShaderModel(')
    b = proof.index('\n        // Copy URP',a)
    method = proof[a:b]
    host = h['once'](host,'                var pass = item.descriptor;',
                    '                var pass = item.descriptor;\n                pass = WithNBInterpolatorShaderModel(pass);')
    return h['once'](host,'        // Copy URP\'s collection;',
                   '        // UVP requires the previously validated SM4.5 interpolator budget.\n'+method+'\n\n        // Copy URP\'s collection;')

def generated_audit(old, new, outputs, h):
    by = {o['m_ObjectId']:o for o in new}
    assert len(by)==len(new),'Duplicate IDs'
    assert new[0]['m_Edges'][:len(old[0]['m_Edges'])]==old[0]['m_Edges']
    for stage in ('m_VertexContext','m_FragmentContext'):
        assert new[0][stage]['m_Blocks'][:len(old[0][stage]['m_Blocks'])]==old[0][stage]['m_Blocks']
    checks = {n:check_cf(new,n,outputs[key]) for n,key in [('NBGraphBaseUV','uv'),('NBGraphUVVertex','uv'),('NBGraphVertexOffset','vertex')]}
    # OVZ function, all old slot objects and GUI mirror properties must be exact.
    allowed = {old[0]['m_ObjectId']} | {o['m_ObjectId'] for o in old if o.get('m_Type')=='UnityEditor.ShaderGraph.CategoryData' or o.get('m_FunctionName') in ('NBGraphBaseUV','NBGraphVertexOffset')}
    for obj in old:
        assert obj['m_ObjectId'] in by
        if obj['m_ObjectId'] not in allowed: assert by[obj['m_ObjectId']]==obj,obj['m_ObjectId']
    node_slots = {o['m_ObjectId']:{by[r['m_Id']]['m_Id'] for r in o.get('m_Slots',[])} for o in new if 'm_Slots' in o}
    inputs=set();adj={k:[] for k in node_slots}
    for edge in new[0]['m_Edges']:
        for side in ('m_InputSlot','m_OutputSlot'):
            q=edge[side];assert q['m_SlotId'] in node_slots[q['m_Node']['m_Id']]
        q=edge['m_InputSlot'];key=(q['m_Node']['m_Id'],q['m_SlotId']);assert key not in inputs;inputs.add(key)
        adj[edge['m_OutputSlot']['m_Node']['m_Id']].append(q['m_Node']['m_Id'])
    for ci in new:
        if not ci.get('customBlockNodeName'):continue
        block=next(o for o in new if o.get('m_SerializedDescriptor','').split('#')[0]=='VertexDescription.'+ci['customBlockNodeName'])
        adj[block['m_ObjectId']].append(ci['m_ObjectId'])
    seen={}
    def visit(node):
        assert seen.get(node)!=1,('Stage dependency loop',node)
        if seen.get(node)==2:return
        seen[node]=1
        for child in adj[node]:visit(child)
        seen[node]=2
    for node in adj:visit(node)
    return dict(CFs=checks,oldEdgesExact=True,oldBlocksExact=True,oldOVZAndGUIObjectsExact=True,
                allEdgeEndpointsValid=True,singleFanIn=True,noCyclesIncludingCustomInterpolatorStages=True)

def main():
    args=argparse.ArgumentParser();args.add_argument('--generate',action='store_true');args.add_argument('--audit-inputs',action='store_true');args.add_argument('--out',default='generated');a=args.parse_args()
    expected=json.loads(EXPECTED.read_text(encoding='utf-8'));h=helpers();audit=input_audit(expected,h)
    save(HERE/'input-audit.json',audit)
    if not a.generate:
        print(json.dumps(dict(inputAudit=str(HERE/'input-audit.json'),graphWritten=False,conflicts=audit['signatureConflicts'])));return
    out=(HERE/a.out).resolve();assert out.is_relative_to(HERE) and out!=HERE
    assert not out.exists(),'Never overwrite an existing generated candidate; choose a new --out child.'
    raw={k:(PACKAGE/r).read_bytes() for k,r in REL.items()}
    uv,prefix,_=h['patch_uv'](raw['uv'].decode('utf-8'))
    graph,stage=h['patch_graph'](raw['graph'].decode('utf-8'),prefix)
    outputs=dict(uv=uv,vertex=h['patch_vertex'](raw['vertex'].decode('utf-8')),
                 host=sm45_patch(h['patch_host'](raw['host'].decode('utf-8')),h),graph=graph)
    old=h['decode'](raw['graph'].decode('utf-8'));new=h['decode'](graph)
    structural=generated_audit(old,new,outputs,h)
    input_audit(expected,h) # root/tool hashes rechecked before any output write
    out.mkdir(parents=True)
    manifest=dict(scope='Current-root dynamically appended UVP1+SM4.5 only; no F0/VAT/CustomLocal/shared math/flags/keyword changes',
                  packageHEAD=expected['packageHEAD'],graphAudit=stage,structuralAudit=structural,records=[],ranUnity=False,productWritten=False)
    for key,text in outputs.items():
        dest=out/REL[key];dest.parent.mkdir(parents=True,exist_ok=True);dest.write_text(text,encoding='utf-8',newline='\n')
        manifest['records'].append(dict(path=REL[key],beforeSHA256=sha(raw[key]),afterSHA256=sha(dest.read_bytes())))
        if key!='graph':(out/(Path(REL[key]).name+'.diff')).write_text(''.join(difflib.unified_diff(raw[key].decode().splitlines(True),text.splitlines(True))),encoding='utf-8',newline='\n')
    for item in expected['validationInputs']:
        dest=out/item['targetPath'];dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(Path(item['path']).read_bytes())
        manifest['records'].append(dict(path=item['targetPath'],beforeSHA256=None,afterSHA256=sha(dest.read_bytes()),kind='strict-validation-only'))
    manifest['readonlyRootHashes']={r:d for r,d in expected['rootHashes'].items() if r not in REL.values()}
    save(out/'manifest.json',manifest)
    print(json.dumps(dict(manifest=str(out/'manifest.json'),graphObjectsBefore=len(old),graphObjectsAfter=len(new),structuralAudit=structural)))
if __name__=='__main__':main()
