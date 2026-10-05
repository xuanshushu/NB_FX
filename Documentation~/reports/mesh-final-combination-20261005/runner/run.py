"""Root-only serial execution. This file is a template; preparing it never runs Unity.
After a semantic F, inspect native failure/raw/cleanup, then start a NEW output at
the next case with --after-review. Never amend a native result or retry a microdiff.
"""
from pathlib import Path
import argparse, copy, gzip, hashlib, json, math, re, struct, subprocess, time

RUNNER_DIR=Path(__file__).resolve().parent
cfg=json.loads((RUNNER_DIR/'config.json').read_text(encoding='utf-8'))
p=argparse.ArgumentParser();p.add_argument('--run',action='store_true');p.add_argument('--out',required=True,type=Path);p.add_argument('--editor-log',required=True,type=Path);p.add_argument('--first',type=int,default=1);p.add_argument('--last',type=int,default=14);p.add_argument('--after-review',type=Path);a=p.parse_args()
assert a.run,'Explicit Root --run required'
assert 1<=a.first<=a.last<=14
if a.first>1:
    assert a.after_review,'Continuation requires previous strict-F review or explicit independent-slice review'
    review=json.loads(a.after_review.read_text(encoding='utf-8'))
    assert review['nextCase']==a.first and review['classification'] in ('strict-semantic-failure','independent-unaffected-slice')
    assert review['originalVerdictRetained'] and review['healthVerified'] and review['cleanupVerified']
    previous=json.loads(Path(review['previousBoundaryResult']).read_text())
    assert previous['healthy'] and previous['cleanupVerified'] and not previous['stopEnvironment'], 'Review cannot waive environment/GPU/cleanup failure'
    assert review['evidencePaths'] and review['reason'], 'Point to actual strong/visible/repeat/control evidence; do not just assert a bool'
assert not a.out.exists(),'Use a new short output; preserve all previous native results'
a.out.mkdir(parents=True);project=Path(cfg['project']);pkg=project/'Packages/NB_FX';status=project/'Temp/pipeline_test_status.json'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
def save(name,value):
    target=a.out/name;assert not target.exists(),target
    target.write_text(json.dumps(value,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');return target
def source_guard():
    for n,h in cfg['sourceInputSHA256'].items():assert sha(pkg/n)==h,n
def command(args,name):
    target=a.out/name;assert not target.exists()
    cp=subprocess.run([cfg['cli'],'command',*args,'--project-path',str(project),'--timeout','30','--json','--no-banner','--non-interactive'],capture_output=True,timeout=45)
    target.write_bytes(cp.stdout)
    if cp.stderr:target.with_suffix('.stderr.log').write_bytes(cp.stderr)
    d=json.loads(cp.stdout.decode('utf-8-sig'));assert cp.returncode==0 and d['success'], 'CLI failure saved; no automatic retry'
    assert d['data']['target']['projectPath'].replace('\\','/').rstrip('/')==cfg['project']
    return d['data']['result']
def evaluation(path,name):
    d=command(['eval_file','--file',str(path)],name);assert d['success'],d
    value=d['result'];value=json.loads(value) if isinstance(value,str) else value
    assert isinstance(value,dict),'Explicit structured JSON required; never parse a ToString fallback'
    return value
def script(name,text):
    f=a.out/name;assert not f.exists();f.write_text(text,encoding='utf-8');return f
def env_script(values):
    # C# string literal uses JSON escapes; never shell interpolation.
    return ''.join('System.Environment.SetEnvironmentVariable('+json.dumps(k)+','+json.dumps(v)+');' for k,v in values.items())+'return new{evidence=System.Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR"),isolation=System.Environment.GetEnvironmentVariable("NBFX_ISOLATED_PROJECT_DIR")};'
def comparison_boundary(value):
    # Only these captured ownership slots may replace an instance ID. All
    # ownership positions, shader references and full serialized JSON stay exact.
    v=copy.deepcopy(value)
    allowed=('UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion.m_Material','NBShader.NBPostProcess._disturbanceDownSampleMat','NBShader.NBPostProcess._screenColorDownSampleMat')
    for asset in v['assets']:
        if asset['identityKind']=='owned-feature-material':
            assert asset['type']=='UnityEngine.Material' and asset['path']=='' and asset['diskSHA256'] is None
            assert asset['owners'] and all(owner.rsplit('/',1)[-1] in allowed for owner in asset['owners'])
            assert asset['json'] and asset['shader']['name'] and asset['shader']['path'] and asset['shader']['guid']
            del asset['id']
    identity=v['render']['sunIdentity']
    if identity['kind']=='exact-runner-bootstrap-light':
        assert v['bootstrap']['recognized'] and v['bootstrap']['fingerprint'] and identity['key']=='Directional Light/UnityEngine.Light'
        v['render']['sun']=identity['key']
    # Original IDs remain untouched in the durable before/after receipts.
    return v
def read_raws(folder,case,native_passed):
    files=sorted(list(folder.rglob('*.rgba32f'))+list(folder.rglob('*.rgba-f32.gz')));bad=[]
    for f in files:
        try:
            b=gzip.decompress(f.read_bytes()) if f.suffix=='.gz' else f.read_bytes()
            if len(b)!=262144:bad.append({'file':str(f),'reason':'not 128x128 RGBA float32'});continue
            vals=struct.unpack('<65536f',b)
            if not all(math.isfinite(v) for v in vals) or any(u[0]==0xCDCDCDCD for u in struct.iter_unpack('<I',b)) or any(v==-23.203125 for v in vals):bad.append({'file':str(f),'reason':'nonfinite/CDCD poison'})
        except Exception as e:bad.append({'file':str(f),'reason':str(e)})
    expected=case['expectedSuccessfulRawCount'];expected_set=None
    if case['index']==10:
        near=list(folder.rglob('near.json'))
        if native_passed and len(near)!=1:bad.append({'reason':'missing/ambiguous near.json'})
        if len(near)==1:
            n=json.loads(near[0].read_text());rows=n.get('observations',[])
            expected=20+sum(1+int(r['depthCopied'])+int(r['normalsCopied'])+int(r['aoCopied']) for r in rows)
            if native_passed and (len(rows)!=11 or not n.get('measurementsComplete')):bad.append({'reason':'near observations incomplete'})
            for r in rows:
                if r['ssaoActive'] and not all(r[k] for k in ['depthCopied','normalsCopied','aoCopied']):bad.append({'reason':'active SSAO missing fresh buffers','label':r['label']})
                for suffix,key in [('f',None),('d','depthCopied'),('n','normalsCopied'),('a','aoCopied')]:
                    exists=(near[0].parent/(r['label']+'-'+suffix+'.rgba32f')).exists()
                    if exists != (True if key is None else r[key]):bad.append({'reason':'copy flag/file mismatch','label':r['label'],'suffix':suffix})
            for mask in ['near-roi.bin','ctl-roi.bin']:
                q=near[0].parent/mask
                if not q.exists() or len(q.read_bytes())!=16384 or set(q.read_bytes())-{0,1}:bad.append({'reason':'bad ROI mask','file':mask})
    if native_passed and len(files)!=expected:bad.append({'reason':'successful capture count mismatch','actual':len(files),'expected':expected})
    if case['body']=='GPU' and not files:bad.append({'reason':'GPU case produced no scientific raw'})
    return {'count':len(files),'bad':bad,'allExistingRawHealthy':not bad,'expectedOnSuccess':expected,'nativePassed':native_passed,'incompleteFailedCaptureIsNotPass':not native_passed,'files':[str(f) for f in files]}

source_guard();save('source-lock.json',cfg['sourceInputSHA256'])
initial=evaluation(RUNNER_DIR/'boundary.cs','initial-boundary.json')
assert not initial['shaderErrors'] and not initial['components'] and not initial['callbacks'] and not initial['tempAssets'],'Initial residue/errors: stop without mutation'
assert initial['api']=='Direct3D11' and initial['unity']=='6000.3.25f1'
oldenv={'NBFX_MESH_EVIDENCE_DIR':initial['evidenceEnvironment'],'NBFX_ISOLATED_PROJECT_DIR':initial['isolationEnvironment']};save('before-environment.json',oldenv)
discovery=command(['list_tests','--mode','editor'],'actual-discovery.json')
tests=discovery['Tests']
for c in cfg['cases']:
    assert [t['FullName'] for t in tests if c['fullName'].lower() in t['FullName'].lower()]==[c['fullName']],c['fullName']
active_run=False;env_touched=False;rows=[]
try:
    for c in cfg['cases'][a.first-1:a.last]:
        i=c['index'];tag=f'c{i:02d}';capture=a.out/tag;capture.mkdir()
        source_guard();before=evaluation(RUNNER_DIR/'boundary.cs',tag+'-before.json')
        assert not before['shaderErrors'] and not before['components'] and not before['callbacks'] and not before['tempAssets']
        env={'NBFX_MESH_EVIDENCE_DIR':capture.resolve().as_posix(),'NBFX_ISOLATED_PROJECT_DIR':cfg['project']}
        env_touched=True;evaluation(script(tag+'-env.cs',env_script(env)),tag+'-env.json')
        if status.exists():(a.out/(tag+'-prior-status.json')).write_bytes(status.read_bytes())
        assert a.editor_log.exists();log_size=a.editor_log.stat().st_size
        started=time.time();active_run=True
        response=command(['run_tests','--mode','editor','--filter',c['fullName'],'--filter_type','testName','--async_tests','true'],tag+'-start.json')
        assert response['result']=='running','Not completed/pass; unexpected response requires inspection'
        done=None
        while time.monotonic() and time.time()-started<300:
            if status.exists() and status.stat().st_mtime>=started:
                try:d=json.loads(status.read_text(encoding='utf-8-sig'))
                except (ValueError,OSError):d={}
                if d.get('status')=='completed':done=d;break
            time.sleep(.5)
        assert done is not None,'Deadline: preserve Editor, no next batch and no concurrent restore CLI'
        active_run=False;(a.out/(tag+'-completed.json')).write_bytes(status.read_bytes())
        assert [r['FullName'] for r in done['results']]==[c['fullName']],'Wrong/zero native identity'
        source_guard()
        with a.editor_log.open('rb') as f:
            assert a.editor_log.stat().st_size>=log_size,'Editor log reset during case';f.seek(log_size);newlog=f.read()
        (a.out/(tag+'-editor-span.log')).write_bytes(newlog)
        fatal=re.findall(r'(?im)^.*(?:device\s+(?:removed|lost)|d3d11[^\n]*(?:failed|error)|GPU crash|GfxDevice.*(?:fail|lost)|Shader error in|error CS\d+|Fatal Error|Crash!!!).*$',newlog.decode('utf-8',errors='replace'))
        after=evaluation(RUNNER_DIR/'boundary.cs',tag+'-after.json')
        compare=['project','unity','api','gpu','scenes','bootstrap','previewSceneCount','features','assets','components','callbacks','tempAssets','render']
        normalized_before=comparison_boundary(before);normalized_after=comparison_boundary(after)
        mismatched=[k for k in compare if normalized_before[k]!=normalized_after[k]]
        cleanup=not mismatched and not after['components'] and not after['callbacks'] and not after['tempAssets']
        passed=done['results'][0]['Status']=='Passed';health=read_raws(capture,c,passed);save(tag+'-raw-health.json',health)
        boundary={'identity':c['fullName'],'nativeSummary':done['summary'],'nativeStatus':done['results'][0]['Status'],'seconds':round(time.time()-started,2),'healthy':health['allExistingRawHealthy'] and not fatal and not after['shaderErrors'],'cleanupVerified':cleanup,'cleanupDifferences':mismatched,'newLogErrors':fatal,'actualShaderErrors':after['shaderErrors'],'stopEnvironment':bool(mismatched or fatal or after['shaderErrors'] or health['bad']),'originalFailure':done['results'][0].get('Message'),'nextCase':i+1}
        save(tag+'-boundary-result.json',boundary);rows.append(boundary);print(json.dumps({k:boundary[k] for k in ['identity','nativeSummary','nativeStatus','healthy','cleanupVerified','nextCase']}),flush=True)
        assert not boundary['stopEnvironment'],'GPU/compile/cleanup/health failed: stop all dispatch'
        if not passed:
            # Root must prove the first assertion is strict semantic parity AND
            # inspect all later strong/visible/repeat controls from metrics/raw.
            # No global "Failed means microdifference" or tolerance waiver.
            save(tag+'-review-needed.json',{'nextCase':i+1,'classification':'REVIEW_REQUIRED','previousBoundaryResult':str(a.out/(tag+'-boundary-result.json')),'reason':'Inspect original stack/assertion and actual controls. Preserve F. If independent continuation is justified, provide reviewed JSON to a new run --first.'})
            break
finally:
    if env_touched and not active_run:
        restored=evaluation(script('restore-env.cs',env_script(oldenv)),'restored-env.json')
        assert restored=={'evidence':oldenv['NBFX_MESH_EVIDENCE_DIR'],'isolation':oldenv['NBFX_ISOLATED_PROJECT_DIR']}
    save('run-summary.json',{'cases':rows,'environmentRestoreAttempted':env_touched and not active_run,'testStillRunningOrUnknown':active_run,'fullGateClaim':False})
