"""Read completed root152 XML/120 consumers and every raw frame; no Unity operations."""
from pathlib import Path
import hashlib, json, gzip, array, sys, math, re
import xml.etree.ElementTree as ET
ROOT=Path('D:/UnityProject/NBUnityProject')
BASE=ROOT/'.utmp/nbfx-mesh-current-6000.3.25f1-d3d11-20261002'
RUN=BASE/'root-customdata152-isolated-1'
OUT=Path(__file__).resolve().parent/'root152-verdict-audit.json'
PREFIX='NBFX.Baseline.Tests.G4GraphCustomDataTests.G4CustomData_GPU_'
CODES=[0,15,14,13,12,11,10,9,8]
assert not OUT.exists(),'Never overwrite existing verdict audit'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def raw(path):
    payload=gzip.decompress(path.read_bytes())
    assert len(payload)==147456,f'Invalid96x96 RGBAf32 payload: {path}'
    values=array.array('f');values.frombytes(payload)
    if sys.byteorder!='little':values.byteswap()
    assert all(math.isfinite(v) for v in values),f'Nonfinite: {path}'
    assert payload!=b'\xcd'*(147456),f'AllCDCD: {path}'
    return values
def delta(a,b):return max(abs(x-y) for x,y in zip(a,b))
def visible(v):return sum(sum(abs(x) for x in v[i:i+4])>.01 for i in range(0,len(v),4))

current={};batches=[]
for batch in range(1,6):
    folder=RUN/f'batch-{batch}';xp=folder/'results.xml';cs=ET.parse(xp).findall('.//test-case')
    assert cs and len({c.get('fullname') for c in cs})==len(cs)
    for c in cs:
        assert c.get('fullname') not in current,'Duplicate current identity'
        current[c.get('fullname')]=(c,folder)
    batches.append({'batch':batch,'xmlSha256':sha(xp),'count':len(cs),
                    'Passed':sum(c.get('result')=='Passed' for c in cs),'Failed':sum(c.get('result')=='Failed' for c in cs)})
storage={k:v for k,v in current.items() if 'G4CustomData_Storage_' in k}
consumers={k:v for k,v in current.items() if k.startswith(PREFIX)}
assert len(current)==152 and len(storage)==32 and len(consumers)==120
assert all(c.get('result')=='Passed' for c,_ in storage.values())
assert not any('vat-frame' in k for k in consumers)

prior={};prior_xml=[]
for run,batches_old in [('customdata-gpu-first28-isolated-1',[1]),
                        ('customdata-gpu-rest96-isolated-1',[1,2,3]),
                        ('customdata-control-repair36-isolated-1',[1])]:
    for batch in batches_old:
        folder=BASE/run/f'batch-{batch}';xp=folder/'results.xml'
        cs=ET.parse(xp).findall('.//test-case')
        prior_xml.append({'run':run,'batch':batch,'xmlSha256':sha(xp),'count':len(cs),
                           'layer':'Replacement verdict for exact36 repaired control identities only' if 'repair36' in run else 'First consumer verdict'})
        for c in cs:
            name=c.get('fullname')
            assert name.startswith(PREFIX)
            suffix=name[len(PREFIX):].replace('_','-')
            mp=folder/'captures'/suffix/'metrics.json'
            prior[name]={'result':c.get('result'),'run':run,'batch':batch,
                         'metrics':json.loads(mp.read_text(encoding='utf-8')),'metricsSha256':sha(mp)}
assert len(prior)==124
prior_nonvat={k:v for k,v in prior.items() if 'vat-frame' not in k}
prior_vat=[k for k in prior if 'vat-frame' in k]
assert len(prior_nonvat)==120 and len(prior_vat)==4 and set(prior_nonvat)==set(consumers)
assert sum(v['result']=='Passed' for v in prior.values())==112
assert sum(v['result']=='Failed' for v in prior.values())==12

failures=[];changed_metrics=[];raw_count=0;metric_count=0
mins={'visible':float('inf'),'response':float('inf')}
maxs={'AB':0,'BC':0,'repeat':0}
counts={'finite':0,'visible':0,'repeat':0,'strongResponse':0,'allABZero':0,'allBCZero':0,
        'metricRawExactMatch':0,'latestHistoricalMetricFieldsEqual':0,'latestHistoricalVerdictEqual':0}
for idx,(name,(case,folder)) in enumerate(sorted(consumers.items()),1):
    suffix=name[len(PREFIX):].replace('_','-');capture=folder/'captures'/suffix;mp=capture/'metrics.json'
    m=json.loads(mp.read_text(encoding='utf-8'));metric_count+=1
    paths=list(capture.glob('*.rgba-f32.gz'))
    assert len(paths)==30,f'Unexpected raw frame count {len(paths)}: {name}'
    frames={f'{s}-code{k}':raw(capture/f'{s}-code{k}.rgba-f32.gz') for s in 'ABC' for k in CODES}
    repeats={s:raw(capture/f'{s}-repeat.rgba-f32.gz') for s in 'ABC'};raw_count+=30
    ab=[delta(frames[f'A-code{k}'],frames[f'B-code{k}']) for k in CODES]
    bc=[delta(frames[f'B-code{k}'],frames[f'C-code{k}']) for k in CODES]
    response={s:max(delta(frames[f'{s}-code0'],frames[f'{s}-code{k}']) for k in CODES) for s in 'ABC'}
    repeat={s:delta(frames[f'{s}-code8'],repeats[s]) for s in 'ABC'}
    visibles={s:max(visible(frames[f'{s}-code{k}']) for k in CODES) for s in 'ABC'}
    assert m['finite'] is True and ab==m['abMax'] and bc==m['bcMax']
    assert all(response[s]==m[s.lower()+'Response'] and repeat[s]==m[s.lower()+'Repeat'] and visibles[s]==m[s.lower()+'Visible'] for s in 'ABC')
    counts['metricRawExactMatch']+=1;counts['finite']+=1
    health={'visible':all(v>128 for v in visibles.values()),'repeat':sum(repeat.values())==0,
            'strongResponse':all(v>.001 for v in response.values()),'ABZero':all(v==0 for v in ab),'BCZero':all(v==0 for v in bc)}
    for k in ['visible','repeat','strongResponse']:counts[k]+=int(health[k])
    counts['allABZero']+=int(health['ABZero']);counts['allBCZero']+=int(health['BCZero'])
    mins['visible']=min(mins['visible'],*visibles.values());mins['response']=min(mins['response'],*response.values())
    maxs['AB']=max(maxs['AB'],*ab);maxs['BC']=max(maxs['BC'],*bc);maxs['repeat']=max(maxs['repeat'],*repeat.values())
    old=prior_nonvat[name]
    equal=m==old['metrics'];verdict_equal=case.get('result')==old['result']
    counts['latestHistoricalMetricFieldsEqual']+=int(equal);counts['latestHistoricalVerdictEqual']+=int(verdict_equal)
    if not equal:changed_metrics.append({'id':name,'oldRun':old['run'],'differentFields':[k for k in m if m[k]!=old['metrics'].get(k)]})
    if case.get('result')=='Failed':
        message=case.findtext('failure/message')
        only_bc=m['finite'] and health['visible'] and health['repeat'] and health['strongResponse'] and health['ABZero'] and not health['BCZero']
        failures.append({'exactIdentity':name,'batch':int(folder.name.split('-')[-1]),'result':'Failed',
           'classification':'same historical micro BCdiff, strict zero assertion remains failed' if only_bc and equal and verdict_equal else 'requires separate new regression investigation',
           'message':message,'metricsSha256':sha(mp),'finite':True,'visible':visibles,'repeat':repeat,'strongResponse':response,
           'ABMax':max(ab),'BCMax':max(bc),'BCNonzeroBySelector':{str(k):v for k,v in zip(CODES,bc) if v!=0},
           'onlyBCStrictParityFailure':only_bc,'oldFinalSameIdentity':{'run':old['run'],'batch':old['batch'],'result':old['result'],
               'metricsSha256':old['metricsSha256'],'allMetricFieldsIdentical':equal},
           'strictFailureRetained':True})
    if idx%30==0:print(json.dumps({'auditedConsumers':idx,'rawFrames':raw_count,'finite':counts['finite']},ensure_ascii=False),flush=True)

assert raw_count==3600 and metric_count==120
assert len(failures)==12
failed_new={r['exactIdentity'] for r in failures}
failed_old={k for k,v in prior_nonvat.items() if v['result']=='Failed'}
assert counts['finite']==counts['visible']==counts['repeat']==counts['strongResponse']==counts['allABZero']==120
same_failures=failed_new==failed_old
only_same_micro=all(r['onlyBCStrictParityFailure'] and r['oldFinalSameIdentity']['allMetricFieldsIdentical'] for r in failures) and same_failures
installed=ROOT/'.utmp/NBFXMeshValidation-20261002/Packages/NB_FX'
audit={'scope':'All5 completed RootCD XML,32storage+120nonVATconsumer identities; every current raw read; no Unity/GPU rerun or source/strict assertion mutation',
 'unity':'6000.3.25f1','api':'Direct3D11','run':str(RUN),'batches':batches,
 'sourceSha256ReadDuringAudit':{'Graph':sha(installed/'NBShaders2/ShaderGraph/NBShaderGraph.shadergraph'),
                               'fixture':sha(installed/'Tests/URP/Editor/G4GraphCustomDataTests.cs')},
 'counts':{'uniqueActualCases':152,'Passed':140,'Failed':12,'Skipped':0,'Inconclusive':0,
           'storage':{'cases':32,'Passed':32},'consumers':{'cases':120,'Passed':108,'Failed':12}},
 'currentEvidenceAudit':{'consumerMetrics':metric_count,'rawFrames':raw_count,'rawBytesUncompressed':raw_count*147456,
                        'rawFormat':'gzip little-endian96x96RGBAfloat32','allRawFinite':True,'allCDCDFrames':0,
                        'recomputedMetricsMatch':counts['metricRawExactMatch'],'independentConditionsCount':counts,
                        'minimumVisiblePixelsAcrossABC':mins['visible'],'minimumStrongResponseAcrossABC':mins['response'],
                        'maximumAB':maxs['AB'],'maximumBC':maxs['BC'],'maximumRepeat':maxs['repeat']},
 'failureClasses':{'sameHistoricalStrictMicroBCdiff':len(failures) if only_same_micro else None,
                   'functionalMissingOrNoStrongResponse':120-counts['strongResponse'],
                   'invisible':120-counts['visible'],'nonfiniteOrAllCDCD':0,'ABBaselineDeviation':120-counts['allABZero']},
 'failures':failures,
 'historicalComparison':{'method':'Disjoint original28+96 forms124; overlay exactly the36 control-repair identities, never add overlapping counts. Compare current120 only against actual same120nonVAT identities.',
    'xmlInputs':prior_xml,'oldFinal124':{'Passed':112,'Failed':12},
    'oldFinal120NonVAT':{'Passed':108,'Failed':12},'same120IdentitySet':True,'same12FailureSet':same_failures,
    'all120VerdictsIdentical':counts['latestHistoricalVerdictEqual']==120,
    'metricFieldsIdenticalCases':counts['latestHistoricalMetricFieldsEqual'],'changedMetricCases':changed_metrics,
    'sourceVersionsRemainDistinct':True},
 'VATScope':{'currentVATCasesRun':0,'originalVATIdentitiesDeferred':prior_vat,'deferredCount':4,
             'claim':'Original4 VAT frame cases are absent here, not skipped/passed nor supported by this Root candidate; require genuine VAT node/consumer integration and execution later.'},
 'assertionLimit':'12failedcases reachedfinite then failABC atsource275; laterrepeat/visible/response assertions not executed, but audit independently recomputes every frame and confirms original conditions. All originalfailXML retained.',
 'recommendation':{'supportsNextRootSliceIntegrationReview':only_same_micro,
    'reason':'Every current consumer finite/visible/repeat/strong/AB condition independently satisfied; exactly prior12tinyBC failures, no new missing/invalid output seen in covered120. Retain12strictmanual failures and VAT4 gap.',
    'fullMeshGatePassed':False,'scopeComplete':False},
 'limits':['Source and historical results are separate provenance; no inherited pass or tolerance relaxation.',
           'Chromatic native crash branch remains explicitly outside this152, not a pass.',
           'Pass/Player/performance and full combinations are not established by storage/consumer slice.',
           'Parent owns full artifact/raw/png provenance audit; this report provides numeric verdict classification without capture listings.']}
OUT.write_text(json.dumps(audit,ensure_ascii=False,indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps({'finished':'140Passed/12strictFailed','raw':raw_count,'sameHistorical12':same_failures,
                   'all120MetricFieldsSame':counts['latestHistoricalMetricFieldsEqual']==120,'minVisible':mins['visible'],
                   'minResponse':mins['response'],'maxBC':maxs['BC'],'supportsNextRootReview':only_same_micro,'output':str(OUT)},ensure_ascii=False),flush=True)
