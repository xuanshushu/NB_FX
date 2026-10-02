"""Verify the installed checkpoint, real NUnit verdicts and protected root bytes."""
from pathlib import Path
import hashlib, json, xml.etree.ElementTree as ET

work = Path(__file__).resolve().parent
root = work.parents[1]
clone = root / '.utmp/NBFXMeshValidation-20261002/Packages/NB_FX'
product = root / 'Packages/NB_FX'
evidence = root / '.utmp/nbfx-mesh-current-6000.3.25f1-d3d11-20261002'
manifest = json.loads((work/'overridez-preview/manifest.json').read_text())
installation = json.loads((work/'overridez-isolation-installation.json').read_text())
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest() if p.exists() else None
assert manifest['records'] == installation['records']
records=[]
for r in manifest['records']:
    actual = sha(clone/r['path'])
    assert actual == r['afterSHA256'], r['path']
    assert sha(work/'overridez-preview'/r['path']) == actual
    records.append({**r, 'installedSHA256':actual, 'rootSHA256':sha(product/r['path'])})
plan = json.loads((work/'overridez-depth16-plan.json').read_text())
run = evidence/'overridez-depth16-isolated-1'
cases = ET.parse(run/'batch-1/results.xml').findall('.//test-case')
assert {c.get('fullname') for c in cases} == set(plan['batches'][0]['exactCaseNames'])
assert len(cases)==16 and all(c.get('result')=='Passed' for c in cases)
summary=json.loads((run/'batch-summary.json').read_text())
assert summary['uniqueActualCases']==16 and summary['results'][0]['cliExitCode']==0
metrics=[json.loads(p.read_text()) for p in (run/'batch-1/captures').rglob('metrics.json')]
assert len(metrics)==16
assert all(m['finite'] and m['api']=='Direct3D11' and m['unityVersion']=='6000.3.25f1' for m in metrics)
assert all(m['ab']+m['bc']+m['abControl']+m['bcControl']==0 for m in metrics)
assert all(m['aRepeat']+m['bRepeat']+m['cRepeat']==0 for m in metrics)
assert all(m['aResponse']>.1 and m['bResponse']>.1 and m['cResponse']>.1 for m in metrics if m['enabled'])
assert all(m['aResponse']+m['bResponse']+m['cResponse']==0 for m in metrics if not m['enabled'])
receipt={'scope':'Live installed OverrideZ bytes and actual depth16 checkpoint; not full advanced states or Gate approval',
 'records':records,'actualCases':len(cases),'passed':16,'metricsRead':len(metrics),
 'xmlSHA256':sha(run/'batch-1/results.xml'),'logSHA256':sha(run/'batch-1/unity.log'),
 'invalidEarlierPreflightExcluded':True,'rootProductIntegration':False,
 'rootGraphSHA256':sha(product/'NBShaders2/ShaderGraph/NBShaderGraph.shadergraph'),
 'installedGraphSHA256':sha(clone/'NBShaders2/ShaderGraph/NBShaderGraph.shadergraph')}
(work/'overridez-resume-byte-evidence-check.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps({k:receipt[k] for k in ('actualCases','passed','metricsRead','rootGraphSHA256','installedGraphSHA256','invalidEarlierPreflightExcluded')}))
