from pathlib import Path
import hashlib,json,re,shutil
from graph_preview_helpers import GraphPreview
work=Path(__file__).resolve().parent;root=work.parents[1];package=root/'.utmp/NBFXMeshValidation-20261002/Packages/NB_FX';out=work/'customlocal-combined-preview';assert not out.exists();files={}
for name in ['customlocal-space-core-preview','customlocal-adapter-preview','customlocal-consumers-preview','customlocal-helper-preview']:
    for p in (work/name).rglob('*'):
        if not p.is_file() or p.name=='manifest.json':continue
        rel=p.relative_to(work/name).as_posix();assert rel not in files,rel;files[rel]=p.read_bytes()
for rel,data in files.items():
    target=out/rel;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(data)
g=GraphPreview(out/'NBShaders2/ShaderGraph/NBShaderGraph.shadergraph','52b6057d-6a1c-4f59-a34a-78ae478f71ca')
changed_functions=['NBGraphCustomLocalBefore','NBGraphCustomLocalAfter','NBGraphUVVertex','NBGraphBaseUV','NBGraphVertexOffset','NBGraphBaseColor','NBGraphFogVertex','NBGraphSixWayBake','NBGraphVATWorldBasis']
all_hlsl='\n'.join(data.decode('utf-8') for rel,data in files.items() if rel.endswith('.hlsl'))
signatures=[]
for name in changed_functions:
    node=next(o for o in g.objects if o.get('m_FunctionName')==name)
    actual=[g.by[r['m_Id']]['m_DisplayName'] for r in node['m_Slots'] if g.by[r['m_Id']]['m_SlotType']==0]
    for precision in ['float','half']:
        m=re.search(r'void\s+'+re.escape(name)+'_'+precision+r'\((.*?)\)\s*\{',all_hlsl,re.S)
        if not m:continue
        params=[p.strip() for p in m[1].split(',')];expected=[p.split()[-1] for p in params if not p.startswith('out ')]
        assert actual==expected,(name,precision,actual,expected)
        outputs=[g.by[r['m_Id']]['m_DisplayName'] for r in node['m_Slots'] if g.by[r['m_Id']]['m_SlotType']==1];wanted=[p.split()[-1] for p in params if p.startswith('out ')]
        assert outputs==wanted,(name,precision,outputs,wanted)
        signatures.append({'name':name,'precision':precision,'inputs':len(actual),'outputs':len(outputs)})
assert len(signatures)==13,len(signatures)
for rel,data in files.items():
    if rel.endswith('.meta'):
        m=re.search(rb'(?m)^guid: ([0-9a-f]+)',data);assert m and len(m[1])==32,rel
records=[]
for rel,data in sorted(files.items()):
    original=package/rel;records.append({'path':rel,'beforeSHA256':hashlib.sha256(original.read_bytes()).hexdigest() if original.exists() else None,'afterSHA256':hashlib.sha256(data).hexdigest()})
(out/'manifest.json').write_text(json.dumps({'scope':'Combined preview only: unchanged custom-space matrix math, existing helper Graph rows, full vertex/UV/Fog/VO/SixWay/fragment TBN consumers. Ordered CF signature/slot andendpoint/metaformat checked; no Unity orfullDAG validation yet.',
 'records':records,'graphObjects':len(g.objects),'signatureChecks':signatures,'newKeywords':0,'newPackedBits':0,'installed':False},indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps({'files':len(records),'objects':len(g.objects),'signatureChecks':len(signatures),'installed':False}))
