from pathlib import Path
import json
work=Path(__file__).resolve().parent;root=work.parents[1];test=root/'.utmp/NBFXMeshValidation-20261002/Packages/NB_FX/Tests/URP/Editor/G4GraphHoudiniModesTests.cs'
before=test.read_bytes();(work/'G4GraphHoudiniModesTests-before-pscale-fixture-fix.cs').write_bytes(before)
text=before.decode('utf-8');old='.46f+.015f*((x+2*y)%5),.1f)';assert text.count(old)==1
test.write_text(text.replace(old,'.46f+.015f*((x+2*y)%5),variant=="sprite-pscale"?.4f:.1f)'),encoding='utf-8',newline='\n')
base=json.loads((work/'houdini-modes32-plan.json').read_text(encoding='utf-8'))
names=[n for b in base['batches'] for n in b['exactCaseNames'] if 'sprite-pscale_' in n]
repair={k:base[k] for k in ['scope','unity','api']};repair.update({'cases':2,'batches':[{'batch':1,'filter':'G4HoudiniModesABC_sprite-pscale_','expectedCasesFromActualDiscovery':2,'exactCaseNames':names}]})
(work/'houdini-pscale-repair2-plan.json').write_text(json.dumps(repair,indent=2)+'\n',encoding='utf-8',newline='\n')
gui=json.loads((work/'gui2-noise-regression360-plan.json').read_text(encoding='utf-8'))['batches'][:2]
mesh=json.loads((work/'gui1c-root-mesh45-plan.json').read_text(encoding='utf-8'))['batches']
flip=json.loads((work/'flipbook36-isolated-plan.json').read_text(encoding='utf-8'))['batches']
vat=json.loads((work/'vat-softbody10-isolated-plan.json').read_text(encoding='utf-8'))['batches']
vat_names=[n for b in vat for n in b['exactCaseNames'] if 'normal-sixway' not in n]
vat=[{'filter':';'.join(n.split('.')[-1] for n in vat_names),'expectedCasesFromActualDiscovery':len(vat_names),'exactCaseNames':vat_names}]
for name in ['vat-geometry4-isolated-plan.json','vat-screen4-isolated-plan.json']:vat+=json.loads((work/name).read_text(encoding='utf-8'))['batches']
batches=gui+mesh+flip+vat
for i,b in enumerate(batches,1):b['batch']=i
all_names=[n for b in batches for n in b['exactCaseNames']];assert len(all_names)==len(set(all_names))==233,len(all_names)
plan={'scope':'Houdini shared-kernel side effects: GUI136/Mesh45/Flipbook36/Softbody8+selected geometry4+screen4. Known weak normal-sixway held for human, not replayed. Not whole Mesh',
 'unity':'6000.3.25f1','api':'Direct3D11','cases':233,'batches':batches}
(work/'houdini-regression233-plan.json').write_text(json.dumps(plan,indent=2)+'\n',encoding='utf-8',newline='\n')
print('Fixture-only pscale input repair2 and independent regression233')
