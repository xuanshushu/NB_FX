from pathlib import Path
import json
work=Path(__file__).resolve().parent
names=['NBFX.Baseline.Tests.G4GraphCustomDataTests.G4CustomData_Storage_w'+str(w)+'_n'+str(n) for w in range(4) for n in range(8)]
plan={'scope':'Existing runtime Material nibble setter/reader on four real Graph halfword pairs, versus original legacy integer words; no full GPU/GUI claim',
 'unity':'6000.3.25f1','api':'Direct3D11','cases':32,'batches':[{'batch':1,'filter':'G4CustomData_Storage_','expectedCasesFromActualDiscovery':32,'exactCaseNames':names}]}
(work/'customdata-storage32-plan.json').write_text(json.dumps(plan,indent=2)+'\n',encoding='utf-8',newline='\n')
print('32 word/position cases with nine selectors each')
