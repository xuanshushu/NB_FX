from pathlib import Path
import json
work=Path(__file__).resolve().parent
variants=['rigid-basic','rigid-interpolated','rigid-dual','rigid-pscale','rigid-uv4','rigid-rest','remesh-lookup','remesh-dual','remesh-compressed','sprite-basic','sprite-interpolated','sprite-spin','sprite-heading','sprite-origin','sprite-pscale','sprite-dual']
plan={'scope':'Three additional Houdini modes actual Mesh Forward ABC and controls; not full Mesh','unity':'6000.3.25f1','api':'Direct3D11','cases':32,'batches':[]}
for batch,group in enumerate([variants[:6],variants[6:9],variants[9:]],1):
    names=['NBFX.Baseline.Tests.G4GraphHoudiniModesTests.G4HoudiniModesABC_'+v+('_ortho' if o else '_perspective') for v in group for o in (True,False)]
    plan['batches'].append({'batch':batch,'filter':';'.join('G4HoudiniModesABC_'+v+'_' for v in group),'expectedCasesFromActualDiscovery':len(names),'exactCaseNames':names})
(work/'houdini-modes32-plan.json').write_text(json.dumps(plan,indent=2)+'\n',encoding='utf-8',newline='\n')
