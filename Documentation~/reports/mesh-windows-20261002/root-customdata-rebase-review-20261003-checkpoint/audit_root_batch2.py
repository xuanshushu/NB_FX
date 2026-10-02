"""Audit only completed root CD batch2 and two corresponding historical identities."""
from pathlib import Path
import json, hashlib, gzip, array, math, sys
import xml.etree.ElementTree as ET

ROOT=Path('D:/UnityProject/NBUnityProject')
BASE=ROOT/'.utmp/nbfx-mesh-current-6000.3.25f1-d3d11-20261002'
CURRENT=BASE/'root-customdata152-isolated-1/batch-2'
OUT=Path(__file__).resolve().parent/'root-batch2-verdict-audit.json'
assert not OUT.exists(),'Never overwrite original audit'
PREFIX='NBFX.Baseline.Tests.G4GraphCustomDataTests.G4CustomData_GPU_'
CODES=[0,15,14,13,12,11,10,9,8]
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def raw(path):
    payload=gzip.decompress(path.read_bytes())
    assert len(payload)==96*96*4*4
    values=array.array('f'); values.frombytes(payload)
    if sys.byteorder!='little':values.byteswap()
    assert all(math.isfinite(v) for v in values),'Nonfinite raw frame'
    assert payload!=bytes.fromhex('cdcdcdcd')*(96*96*4),'Invalid all-CDCD raw'
    return values
def delta(a,b):return max(abs(x-y) for x,y in zip(a,b))
def visible(v):return sum(sum(abs(x) for x in v[i:i+4])>.01 for i in range(0,len(v),4))

xml=ET.parse(CURRENT/'results.xml')
cases=xml.findall('.//test-case')
failed=[c for c in cases if c.get('result')=='Failed']
assert len(cases)==28 and len(failed)==2
assert {c.get('fullname') for c in failed}=={PREFIX+x+'_perspective_varying' for x in ['mask-x','mask-y']}
historical={}
for run in ['customdata-gpu-first28-isolated-1','customdata-control-repair36-isolated-1']:
    xp=BASE/run/'batch-1/results.xml'
    historical[run]={c.get('fullname'):c for c in ET.parse(xp).findall('.//test-case')}
results=[]
for c in failed:
    name=c.get('fullname'); consumer=name[len(PREFIX):].replace('_perspective_varying','')
    folder=CURRENT/'captures'/f'{consumer}-perspective-varying'
    mp=folder/'metrics.json'; metrics=json.loads(mp.read_text(encoding='utf-8'))
    frames={f'{s}-code{k}':raw(folder/f'{s}-code{k}.rgba-f32.gz') for s in 'ABC' for k in CODES}
    repeats={s:raw(folder/f'{s}-repeat.rgba-f32.gz') for s in 'ABC'}
    ab=[delta(frames[f'A-code{k}'],frames[f'B-code{k}']) for k in CODES]
    bc=[delta(frames[f'B-code{k}'],frames[f'C-code{k}']) for k in CODES]
    responses={s:max(delta(frames[f'{s}-code0'],frames[f'{s}-code{k}']) for k in CODES) for s in 'ABC'}
    repeat={s:delta(frames[f'{s}-code8'],repeats[s]) for s in 'ABC'}
    visibles={s:max(visible(frames[f'{s}-code{k}']) for k in CODES) for s in 'ABC'}
    assert ab==metrics['abMax'] and bc==metrics['bcMax']
    for s in 'ABC':
        assert responses[s]==metrics[s.lower()+'Response']
        assert repeat[s]==metrics[s.lower()+'Repeat']
        assert visibles[s]==metrics[s.lower()+'Visible']
    assert metrics['finite'] and all(v>128 for v in visibles.values())
    assert all(v==0 for v in repeat.values()) and all(v>.001 for v in responses.values())
    old=[]
    for run,oldcases in historical.items():
        prior=oldcases.get(name)
        if prior is None:continue
        oldmp=BASE/run/'batch-1/captures'/f'{consumer}-perspective-varying/metrics.json'
        om=json.loads(oldmp.read_text(encoding='utf-8'))
        old.append({'run':run,'exactIdentity':name,'result':prior.get('result'),
                    'failureMessage':prior.findtext('failure/message'),'xmlSha256':sha(BASE/run/'batch-1/results.xml'),
                    'metricsSha256':sha(oldmp),'metrics':om,'allParsedMetricFieldsEqualCurrent':om==metrics})
    latest=old[-1]
    assert latest['allParsedMetricFieldsEqualCurrent']
    results.append({
       'exactIdentity':name,'rootVerdict':'Failed','classification':'mild BCdiff; strict zero parity failure retained for manual review',
       'failureMessage':c.findtext('failure/message'),'stack':c.findtext('failure/stack-trace'),
       'metricsSha256':sha(mp),'finite':{'metric':True,'all30RawFramesFinite':True,'allCDCD':False},
       'visible':{'byShader':visibles,'requiredGreaterThan':128,'observedSatisfied':True},
       'repeat':{'byShader':repeat,'requiredSum':0,'observedSatisfied':True},
       'strongResponse':{'byShader':responses,'requiredGreaterThan':.001,'observedSatisfied':True},
       'AB':{'bySelectorCode':dict(zip(CODES,ab)),'allZero':True},
       'BC':{'bySelectorCode':dict(zip(CODES,bc)),'max':max(bc),'allZero':False},
       'rawAudit':{'frames':30,'size':[96,96],'format':'gzip little-endian RGBAfloat32',
                   'bytesPerFrame':147456,'computedMetricsExactlyMatchSaved':True},
       'assertionExecutionLimit':'finite assertion reached and passed; strict ABC failed at source275. Later repeat/visible/response assertions source276–277 not reached, but values independently recomputed from stored raw and satisfy original unchanged conditions.',
       'history':old,'comparisonToLatestHistoricalIdentity':'All parsed metric fields identical to corresponding prior finalized identity; no newly observed functional or invalid-output error in these two root cases.',
       'manualStrictFailureRetained':True})
source=ROOT/'.utmp/NBFXMeshValidation-20261002/Packages/NB_FX'
audit={
  'scope':'Only completed RootCustomData batch2 (28 cases), failures2 and historical exact matching identities; no future/root unfinished XML inspected',
  'unity':'6000.3.25f1','api':'Direct3D11','xml':str(CURRENT/'results.xml'),'xmlSha256':sha(CURRENT/'results.xml'),
  'sourceReadCurrentInstalled':{
     'GraphSha256':sha(source/'NBShaders2/ShaderGraph/NBShaderGraph.shadergraph'),
     'fixtureSha256':sha(source/'Tests/URP/Editor/G4GraphCustomDataTests.cs'),
     'sourceDiffersFromPriorCombined':True},
  'batch2Counts':{'actualUniqueIdentities':28,'Passed':26,'Failed':2,'Skipped':0,'Inconclusive':0},
  'failedCaseRawFramesAudited':60,'twoFailureCategories':{'mildBCdiff':2,'functionalMissingOrZeroResponse':0,'invisible':0,'nonfiniteOrInvalidReadback':0},
  'failureCases':results,
  'limits':['No whole152 verdict or Gate conclusion. Do not combine source versions/overlapping historical runs.',
            'No tolerance/identity/source/assertion modification or Unity rerun performed.',
            'Historical mask-y first run had zero response; finalized control-repair run regained .3153076171875 response and still failed tiny BCdiff. Root matches that finalized record, not the early zero-response failure.',
            'Existing historical12 manual failures are not substituted for current root results; this audit proves only the current two identities.'],
  'fullMeshGatePassed':False}
OUT.write_text(json.dumps(audit,ensure_ascii=False,indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps({'rootBatch2':'26Passed/2Failed','twoFailures':'strict tinyBCdiff','maxBC':max(r['BC']['max'] for r in results),
                  'failedRawFramesAudited':60,'latestSameIdentityMetricsEqual':True,'output':str(OUT)},ensure_ascii=False))
