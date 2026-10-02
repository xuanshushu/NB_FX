from pathlib import Path
import json
work=Path(__file__).resolve().parent;batches=[]
def add(filter,names):
    batches.append({'batch':len(batches)+1,'filter':filter,'expectedCasesFromActualDiscovery':len(names),'exactCaseNames':names})
for filename in ['gui1c-110-final-plan.json','gui1c-root-mesh45-plan.json','overridez-depth16-plan.json','overridez-lifecycle17-plan.json']:
    m=json.loads((work/filename).read_text())
    for b in m['batches']:add(b['filter'],b['exactCaseNames'])
discovery=json.loads((work/'cli-editmode-test-discovery.json').read_text())['data']['result']['Tests']
names=[t['FullName'] for t in discovery if t['FullName'].startswith('NBFX.Baseline.Tests.G4GraphDepthShadowTests.G4DepthShadow_')]
assert len(names)==36;add('NBFX.Baseline.Tests.G4GraphDepthShadowTests',names)
authority=work/'overridez-keyword-authority8-plan.json'
if authority.exists():
    for b in json.loads(authority.read_text())['batches']:add(b['filter'],b['exactCaseNames'])
source=work/'overridez-source2-plan.json'
if source.exists():
    for b in json.loads(source.read_text())['batches']:add(b['filter'],b['exactCaseNames'])
count=sum(len(b['exactCaseNames']) for b in batches)
assert count==len({n for b in batches for n in b['exactCaseNames']})
plan={'scope':'Current-root-derived OverrideZ slice: original GUI110, Mesh45, depth16, lifecycle17, shared-kernel Depth/Shadow36 and explicit keyword authority; not full Mesh/advanced-state coverage','unity':'6000.3.25f1','api':'Direct3D11','cases':count,'batches':batches}
(work/'overridez-root-validation-plan.json').write_text(json.dumps(plan,indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps({'rootSliceCases':count,'serialFreshEditors':len(batches)}))
