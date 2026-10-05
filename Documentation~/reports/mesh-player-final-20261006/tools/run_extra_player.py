"""Actual independent Player entries. No Build, Unity RPC, or project mutation."""
from pathlib import Path
import argparse,json,subprocess,hashlib,math,struct,statistics,time,datetime,re
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
def main():
    p=argparse.ArgumentParser();p.add_argument('mode',choices=['runtime','performance']);p.add_argument('--build',type=Path,required=True);p.add_argument('--evidence',type=Path,required=True);p.add_argument('--correctness-verdict',type=Path,required=True);p.add_argument('--timeout',type=int,default=600);a=p.parse_args()
    build=a.build.resolve();out=a.evidence.resolve();exe=build/'NBFXMeshValidation.exe';assert not out.exists();assert 30<=a.timeout<=900
    previous=json.loads(a.correctness_verdict.read_text());assert previous['status']=='PASSED' and previous['nativeProcessExitCode']==0 and previous['exeSHA256']==sha(exe),'Frozen Player8 must pass on this exact executable first'
    receipt=json.loads((build/'build-receipt.json').read_text());assert receipt['buildResult']=='Succeeded'and receipt['totalErrors']==0 and receipt['expectedRuntimeCases']==8 and receipt['expectedRawCaptures']==140
    audit=json.loads((build/'build-observation.json').read_text());assert audit['generatedAssetsUnchanged'],'Generated build inputs drifted'
    data=exe.read_bytes();offset=struct.unpack_from('<I',data,0x3c)[0];assert data[:2]==b'MZ'and data[offset:offset+4]==b'PE\0\0'and struct.unpack_from('<H',data,offset+4)[0]==0x8664
    out.mkdir(parents=True)
    hardware=subprocess.run(['powershell','-NoProfile','-Command','Get-CimInstance Win32_VideoController | Select-Object Name,DriverVersion | ConvertTo-Json -Compress'],capture_output=True,timeout=20)
    (out/'video-driver.json').write_bytes(hardware.stdout)
    switch='--nbfx-runtime-proof'if a.mode=='runtime'else'--nbfx-performance';dest='--nbfx-runtime-proof-evidence'if a.mode=='runtime'else'--nbfx-perf-evidence'
    command=[str(exe),'-batchmode','-force-d3d11','-screen-width','128','-screen-height','128','-logFile',str(out/'Player.log'),switch,dest,str(out)]
    start=time.monotonic();exitcode=None;timeout=False
    try:
        run=subprocess.run(command,cwd=build,capture_output=True,timeout=a.timeout,creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0));exitcode=run.returncode;(out/'process-output.log').write_bytes(run.stdout+run.stderr)
    except subprocess.TimeoutExpired as e:timeout=True;(out/'process-output.log').write_bytes(e.stdout or b'')
    issues=[]
    if timeout or exitcode!=0:issues.append('Native process timeout/nonzero exit: '+str(exitcode))
    log=out/'Player.log'
    gpu_errors=[line for line in log.read_text(encoding='utf-8',errors='replace').splitlines()if re.search(r'Shader error in|DXGI_ERROR|GPU crash|D3D11: Failed|RenderTexture\.Create failed|Out of memory|Unhandled Exception',line,re.I)]if log.exists()else[]
    if gpu_errors:issues.append('Actual shader/GPU error in Player.log')
    name='runtime-projection-result.json'if a.mode=='runtime'else'performance-result.json';f=out/name;result=json.loads(f.read_text())if f.exists()else None
    if result is None:issues.append('Missing actual Player receipt')
    elif result.get('applicationIsEditor')or result.get('platform')!='WindowsPlayer'or result.get('pointerBytes')!=8 or result.get('api')!='Direct3D11'or result.get('buildIdentity')!=receipt['identity']or result.get('sourceLockSHA256')!=receipt['sourceLockSHA256']:issues.append('Actual platform/build/source identity mismatch')
    analysis={}
    if a.mode=='runtime':
        stages=['mask-on','mask-denied','mask-restored','ovz-on','ovz-denied','ovz-restored'];expected={s+'-'+r+'.rgba32f'for s in stages for r in ['B','Br','C','Cr']};raws=list(out.glob('*.rgba32f'));bad=[]
        for path in raws:
            b=path.read_bytes()
            if len(b)!=128*128*16 or any(not math.isfinite(v[0])or v[0]in(-23.203125,-431602080.)for v in struct.iter_unpack('<f',b)):bad.append(path.name)
        if {f.name for f in raws}!=expected or bad:issues.append('Missing/invalid exact independent24 raw')
        if result:
            if result.get('identity')!='PlayerBC_RuntimeGraphMaskOVZ_ortho'or result.get('status')!='PASSED'or result.get('rawCount')!=24:issues.append('Independent Runtime projection did not pass')
            if [s['name']for s in result.get('stages',[])]!=stages:issues.append('Stage sequence incomplete')
            for s in result.get('stages',[]):
                if s['bc']!=0 or s['bRepeat']!=0 or s['cRepeat']!=0 or not s['rawPreserved']or not s['declaredKeyword']:issues.append('Strict stage/state failure: '+s['name'])
            for k in ['maskResponseB','maskResponseC','ovzResponseB','ovzResponseC']:
                if not math.isfinite(result.get(k,float('nan')))or result[k]<=.1:issues.append('Weak/missing actual response: '+k)
            for k in ['maskRestoreB','maskRestoreC','ovzRestoreB','ovzRestoreC']:
                if result.get(k)!=0:issues.append('Restore difference: '+k)
        compiled=[]
        for host,suffix in [('B','/NBShaders2/Shader/NBShader.shader'),('C','/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph')]:
            rows=[r for r in audit['shaderCallbacks']if r['assetPath'].endswith(suffix)and r['stage']=='Fragment']
            on=sum(r['ovzEnabled']for r in rows);off=sum(r['ovzDisabled']for r in rows)
            compiled.append({'host':host,'OVZonObserved':on,'OVZoffObserved':off,'available':on>0 and off>0,'scope':'Observed preprocess callback counts; not proven final-binary enumeration'})
        analysis={'rawCount':len(raws),'badRaw':bad,'compiledVariantObservation':compiled,'retentionStrategy':'Explicit three on/off scene-referenced material states per host; no SVC or AlwaysIncluded mutation','renderVerdict':'PASSED'if not issues else'FAILED_OR_UNVERIFIED','compiledVariantEvidenceComplete':all(r['available']for r in compiled)}
        status=analysis['renderVerdict']
    else:
        if result and (result.get('status')!='COLLECTED_NO_BUDGET_VERDICT'or not result.get('configurationRestored')):issues.append('Sampler/restore did not complete')
        expected=[(route,r,role)for route in ['Forward','NBCameraOpaqueDistortPass','NBDeferredDistortPass']for r,pair in enumerate(['BC','CB','CB','BC'])for role in pair]
        blocks=result.get('blocks',[])if result else[]
        if [(b['route'],b['repetition'],b['role'])for b in blocks]!=expected:issues.append('Fixed24 paired blocks incomplete/reordered')
        stats=[]
        def dist(values):
            if not values:return None
            v=sorted(values);return {'n':len(v),'median':statistics.median(v),'p95':v[max(0,math.ceil(.95*len(v))-1)],'min':v[0],'max':v[-1]}
        for block in blocks:
            samples=block['samples'];expectedCount=240
            if len(samples)!=expectedCount or block['warmFrames']!=120 or block['lastFrame']-block['firstFrame']<expectedCount:issues.append('Invalid real sample-frame window')
            values={}
            for key,flag in [('gpuMs','gpuAvailable'),('cpuMs','cpuAvailable'),('cpuMainMs','cpuAvailable'),('cpuRenderMs','cpuAvailable'),('cpuPresentWaitMs','cpuAvailable')]:
                v=[s[key]for s in samples if s[flag]and s['freshTiming']and math.isfinite(s[key])]
                if key=='gpuMs'and not result.get('frameTimingEnabled'):v=[]
                values[key]={'availability':'COMPLETE'if len(v)==expectedCount else'PARTIAL'if v else'UNAVAILABLE','distributionMs':dist(v)}
            for n,key in enumerate(['mainThread','renderThread','gpuCounter','drawCalls','setPassCalls','batches']):
                v=[s[key]for s in samples if s['recorderValidMask']&(1<<n)and(n>=3 or s[key]>0)];desc=block['counters'][n]
                # A missing metric is never silently substituted with zero.
                values[key]={'availability':'COMPLETE'if len(v)==expectedCount else'PARTIAL'if v else'UNAVAILABLE','unit':desc['unit'],'counterName':desc['name'],'rawDistribution':dist(v)}
            stats.append({'route':block['route'],'role':block['role'],'repetition':block['repetition'],'sampleSeconds':block['actualSampleSeconds'],'metrics':values})
        paired=[]
        for route in ['Forward','NBCameraOpaqueDistortPass','NBDeferredDistortPass']:
            for rep in range(4):
                selected={s['role']:s for s in stats if s['route']==route and s['repetition']==rep}
                if set(selected)!=set('BC'):continue
                bm=selected['B']['metrics']['gpuMs'];cm=selected['C']['metrics']['gpuMs']
                valid=bm['availability']=='COMPLETE'and cm['availability']=='COMPLETE'
                paired.append({'route':route,'repetition':rep,'GPUComparableComplete':valid,'graphMinusNativeMedianMs':cm['distributionMs']['median']-bm['distributionMs']['median']if valid else None})
        analysis={'fixedProtocol':{'warmFrames':120,'sampleFrames':240,'pairs':['BC','CB','CB','BC'],'resolution':[128,128],'workloads':3,'NBMeshCount':1},'blocks':stats,'pairedGPU':paired,'performanceBudget':None,'performancePassed':None,'boundaries':['whole-frame timing, no per-draw GPU attribution','CPU frame includes wait; render-thread metric is not pure submission','Draw/SetPass counts are not exact executed pass inventory','Sample frame count fixed; actual elapsed duration is recorded at uncapped runtime frame rate','No SVC/AssetBundle, pure import-only time or per-host build-size attribution']}
        status='FAILED_OR_UNVERIFIED'if issues else'COLLECTED_NO_PERFORMANCE_VERDICT'
    verdict={'mode':a.mode,'status':status,'startedUTC':datetime.datetime.now(datetime.timezone.utc).isoformat(),'elapsedSeconds':time.monotonic()-start,'nativeExit':exitcode,'timedOut':timeout,'command':command,'exeSHA256':sha(exe),'buildReceiptSHA256':sha(build/'build-receipt.json'),'issues':issues,'shaderGPUErrorLines':gpu_errors,'analysis':analysis}
    (out/'process-verdict.json').write_text(json.dumps(verdict,indent=2)+'\n');print(json.dumps({'status':status,'issues':issues}));raise SystemExit(1 if issues else 0)
if __name__=='__main__':main()
