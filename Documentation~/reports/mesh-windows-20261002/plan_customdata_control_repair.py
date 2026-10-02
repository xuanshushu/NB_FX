from pathlib import Path
import json,xml.etree.ElementTree as ET
work=Path(__file__).resolve().parent;runs=work.parents[1]/'.utmp/nbfx-mesh-current-6000.3.25f1-d3d11-20261002'
names=[]
for source in ('customdata-gpu-first28-isolated-1','customdata-gpu-rest96-isolated-1'):
    for xml in (runs/source).glob('batch-*/results.xml'):
        for case in ET.parse(xml).findall('.//test-case'):
            if case.get('result')=='Failed' and 'greater than' in (case.findtext('failure/message') or ''):names.append(case.get('fullname'))
assert len(names)==36 and len(set(names))==36
plan={'scope':'Only36 first-run insensitive fixtures, corrected 2D channel/threshold/actualPNoise consumer; all strict/finite/visible/response checks retained',
 'unity':'6000.3.25f1','api':'Direct3D11','cases':36,'batches':[{'batch':1,'filter':';'.join(n.split('.')[-1] for n in names),
 'expectedCasesFromActualDiscovery':36,'exactCaseNames':names}]}
(work/'customdata-control-repair36-plan.json').write_text(json.dumps(plan,indent=2)+'\n',encoding='utf-8',newline='\n')
print('36 exact control failures only; no weak difference replay')
