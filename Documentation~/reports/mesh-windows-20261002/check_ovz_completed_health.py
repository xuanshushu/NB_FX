from pathlib import Path
import json,struct,sys,re,xml.etree.ElementTree as ET
work=Path(__file__).resolve().parent
run=Path(sys.argv[1]).resolve();assert run.is_relative_to(work.parents[1]/'.utmp/nbfx-mesh-current-6000.3.25f1-d3d11-20261002')
expected=int(sys.argv[2]);issues=[];rawcount=0;cases=[];metriccount=0
for path in run.rglob('unity.log'):
    text=path.read_text(encoding='utf-8',errors='replace')
    for pattern in ('887a0005','D3D shader create error','Crash!!!','failed staging texture','Failed to create 2D texture in GfxDeviceD3D11'):
        if pattern.lower() in text.lower():issues.append({'path':str(path.relative_to(run)),'GPUEvent':pattern})
for path in run.rglob('results.xml'):cases+=ET.parse(path).findall('.//test-case')
if len(cases)!=expected:issues.append({'caseVerdicts':len(cases),'expected':expected})
for path in run.rglob('*.rgba32f'):
    data=path.read_bytes();rawcount+=1
    if len(data)!=128*128*4*4:issues.append({'path':str(path.relative_to(run)),'invalidBytes':len(data)});continue
    values=[v[0] for v in struct.iter_unpack('<f',data)]
    import math
    if not all(math.isfinite(v) for v in values):issues.append({'path':str(path.relative_to(run)),'nonfinite':True})
    if all(v==-23.203125 for v in values):issues.append({'path':str(path.relative_to(run)),'allCDCD':True})
for path in run.rglob('metrics.json'):
    m=json.loads(path.read_text(encoding='utf-8'));metriccount+=1
    if not m['finite']:issues.append({'path':str(path.relative_to(run)),'finite':False})
    for key in ('aRepeat','bRepeat','cRepeat'):
        if m[key]!=0:issues.append({'path':str(path.relative_to(run)),key:m[key]})
    if m['enabled']:
        for key in ('aResponse','bResponse','cResponse'):
            if m[key]<=.1:issues.append({'path':str(path.relative_to(run)),key:m[key]})
        if (m['actorVisible'] if m['depth']<6 else m['probeVisible'])<=150:issues.append({'path':str(path.relative_to(run)),'invalidVisibility':True})
    elif m['actorVisible']<=150:issues.append({'path':str(path.relative_to(run)),'invalidVisibility':True})
if rawcount!=expected*9 or metriccount!=expected:issues.append({'raw':rawcount,'metrics':metriccount,'expectedRaw':expected*9})
record={'scope':'Before launching another OVZ batch: actual log/XML/full raw/metrics health; strict pixel verdicts retained separately','run':str(run),'caseVerdicts':len(cases),'passed':sum(c.get('result')=='Passed' for c in cases),'rawRead':rawcount,'metricsRead':metriccount,'issues':issues,'validForNextBatch':not issues}
(run/'external-ovz-health-check.json').write_text(json.dumps(record,indent=2)+'\n',encoding='utf-8')
print(json.dumps({key:record[key] for key in ('caseVerdicts','passed','rawRead','metricsRead','validForNextBatch','issues')}))
assert not issues, 'Invalid readback/GPU state: keep original failures and do not run further batches'
