from pathlib import Path
import json
import xml.etree.ElementTree as ET
work=Path(__file__).resolve().parent
root=work.parents[1]
source=root/'.utmp/nbfx-mesh-current-6000.3.25f1-d3d11-20261002/gui1c-native11-isolated-1'
plan=json.loads((work/'gui1c-native11-plan.json').read_text(encoding='utf-8'))
cases=ET.parse(source/'batch-1/results.xml').findall('.//test-case')
names=[c.attrib['fullname'] for c in cases]
assert len(names)==11 and len(set(names))==11
assert set(names)==set(plan['batches'][0]['exactCaseNames'])
cli=json.loads((source/'batch-1/cli.stdout.json').read_text(encoding='utf-8-sig'))
assert cli['success'] and not cli['errors']
counts={s:sum(c.get('result')==s for c in cases) for s in ('Passed','Failed','Skipped','Inconclusive')}
summary={'scope':plan['scope'],'unity':plan['unity'],'api':plan['api'],'uniqueActualCases':11,'completedBatches':1,'expectedBatches':1,'fullMeshGatePassed':False,
  'orchestratorReceiptCorrection':'The manually composed expected fullname incorrectly appended () to the no-argument Undo test. The native runner already completed successfully; its XML and CLI stdout are unchanged. Corrected exact name list now matches every actual test; no tests were rerun or verdicts synthesized.',
  'results':[{'batch':1,'cases':11,'counts':counts,'cliSuccessFromRawResponse':True,'xml':str(source/'batch-1/results.xml'),'failedNames':[c.get('fullname') for c in cases if c.get('result')=='Failed']}]}
(source/'batch-summary.json').write_text(json.dumps(summary,indent=2)+'\n',encoding='utf-8',newline='\n')
old=json.loads((work/'gui1b-99-isolated-plan.json').read_text(encoding='utf-8'))
new=old['batches'][1].copy();new['batch']=2;new['filter']='NBFX.Baseline.Tests.G4GraphGuiFeatureIntentTests'
new['expectedCasesFromActualDiscovery']=26;new['exactCaseNames']=new['exactCaseNames']+plan['batches'][0]['exactCaseNames']
final={'scope':'Current GUI1C explicit native edits plus GUI1B seed and old shared backend; not full visible Inspector or Tier',
    'unity':old['unity'],'api':old['api'],'cases':110,'batches':[old['batches'][0],new]}
assert len({n for b in final['batches'] for n in b['exactCaseNames']})==110
(work/'gui1c-110-final-plan.json').write_text(json.dumps(final,indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps({'native11Actual':counts,'noVerdictChanged':True,'freshRegressionCases':110}))
