from pathlib import Path
import json
work=Path(__file__).resolve().parent
groups=[['base-x','base-y','dissolve-strength','hue','mask-x','mask-y','fresnel'],
 ['dissolve-x','dissolve-y','noise-intensity','saturation','vertex-x','vertex-y','vertex-intensity','dissolve-mask'],
 ['simple-x','simple-y','voronoi-x','voronoi-y','noise-direction-x','noise-direction-y','contrast','vat-frame'],
 ['vertex-mask-x','vertex-mask-y','shared-x','shared-y','emission-x','emission-y','overlay-x','overlay-y']]
plan={'scope':'Four-word functional consumers in ordinary Mesh; CA native crash branch excluded explicitly, all9 selector codes percase; not full Mesh',
 'unity':'6000.3.25f1','api':'Direct3D11','cases':124,'batches':[]}
for i,names in enumerate(groups,1):
    cases=['NBFX.Baseline.Tests.G4GraphCustomDataTests.G4CustomData_GPU_'+name+('_ortho' if o else '_perspective')+('_varying' if v else '_constant') for name in names for o in (True,False) for v in (False,True)]
    plan['batches'].append({'batch':i,'filter':';'.join('G4CustomData_GPU_'+name+'_' for name in names),'expectedCasesFromActualDiscovery':len(cases),'exactCaseNames':cases})
(work/'customdata-gpu124-plan.json').write_text(json.dumps(plan,indent=2)+'\n',encoding='utf-8',newline='\n')
first=plan.copy();first['batches']=[plan['batches'][0]];first['cases']=len(first['batches'][0]['exactCaseNames'])
(work/'customdata-gpu-first28-plan.json').write_text(json.dumps(first,indent=2)+'\n',encoding='utf-8',newline='\n')
print('Four independent batches 28+32+32+32; all9 selectors')
