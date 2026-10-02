from pathlib import Path
import json
work=Path(__file__).resolve().parent
plan=json.loads((work/'customdata-gpu124-plan.json').read_text(encoding='utf-8'))
plan['batches']=plan['batches'][1:]
plan['cases']=sum(b['expectedCasesFromActualDiscovery'] for b in plan['batches'])
for i,b in enumerate(plan['batches'],1):b['batch']=i
(work/'customdata-gpu-rest96-plan.json').write_text(json.dumps(plan,indent=2)+'\n',encoding='utf-8',newline='\n')
print('Remaining96 cases, without rerunning first28')
