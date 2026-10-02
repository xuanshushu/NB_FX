from pathlib import Path
import json,xml.etree.ElementTree as ET
work=Path(__file__).resolve().parent;runs=work.parents[1]/'.utmp/nbfx-mesh-current-6000.3.25f1-d3d11-20261002'
latest={};history=[]
for label in ('customdata-gpu-first28-isolated-1','customdata-gpu-rest96-isolated-1','customdata-control-repair36-isolated-1'):
    for xml in sorted((runs/label).glob('batch-*/results.xml')):
        for case in ET.parse(xml).findall('.//test-case'):
            name=case.get('fullname');row={'name':name,'result':case.get('result'),'message':case.findtext('failure/message'),'source':label,'xml':xml.relative_to(runs).as_posix()}
            history.append(row);latest[name]=row
assert len(latest)==124
failures=[]
for name,row in latest.items():
    if row['result']!='Failed':continue
    consumer=name.split('G4CustomData_GPU_')[1].replace('_ortho','-ortho').replace('_perspective','-perspective').replace('_varying','-varying').replace('_constant','-constant')
    source=runs/row['source'];metrics=next(source.glob('batch-*/captures/'+consumer+'/metrics.json'))
    data=json.loads(metrics.read_text(encoding='utf-8'))
    failures.append({**row,'finite':data['finite'],'abMax':max(data['abMax']),'bcMax':max(data['bcMax']),
     'response':[data['aResponse'],data['bResponse'],data['cResponse']],'repeat':[data['aRepeat'],data['bRepeat'],data['cRepeat']]})
result={'scope':'Latest case-by-case results after fixture-only control correction; original XMLs retained, not synthetic merged NUnit XML',
 'uniqueCases':124,'passed':sum(r['result']=='Passed' for r in latest.values()),'failed':len(failures),
 'CAConsumerNotReplayedDueToPriorNativeCrash':True,'histories':history,'failures':failures,'fullMeshPassed':False}
(work/'customdata-verdict-ledger.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps({k:v for k,v in result.items() if k!='histories'},indent=2))
