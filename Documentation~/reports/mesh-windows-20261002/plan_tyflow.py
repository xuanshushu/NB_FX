from pathlib import Path
import json
work=Path(__file__).resolve().parent
plan={'scope':'Six Tyflow modes actual Forward ABC; original raw/half/float layout and7-channel skin, no Gate claim','unity':'6000.3.25f1','api':'Direct3D11','cases':52,'batches':[]}
for mode in range(6):
    variants=['raw','half','float','custom-frame','normals'] if mode<2 else ['rigid','deform7','interpolated','deform7-interpolated']
    names=['NBFX.Baseline.Tests.G4GraphTyflowVATTests.G4TyflowABC_m'+str(mode)+'_'+v+('_ortho' if o else '_perspective') for v in variants for o in [True,False]]
    plan['batches'].append({'batch':mode+1,'filter':'G4TyflowABC_m'+str(mode)+'_','expectedCasesFromActualDiscovery':len(names),'exactCaseNames':names})
(work/'tyflow52-plan.json').write_text(json.dumps(plan,indent=2)+'\n',encoding='utf-8',newline='\n')
