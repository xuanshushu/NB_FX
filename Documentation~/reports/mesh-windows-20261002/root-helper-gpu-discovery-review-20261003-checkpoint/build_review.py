"""Read-only metadata discovery audit, write only this owned review folder."""
from pathlib import Path
import json, hashlib, re, xml.etree.ElementTree as ET
ROOT=Path('D:/UnityProject/NBUnityProject')
OUT=Path(__file__).resolve().parent
PREVIEW=ROOT/'.utmp/nbfx-resume-20261002/root-flipbook-rebase/helper-nbpass-preview'
INSTALLED=ROOT/'.utmp/NBFXMeshValidation-20261002/Packages/NB_FX'
EVIDENCE=ROOT/'.utmp/nbfx-mesh-current-6000.3.25f1-d3d11-20261002/root-flipbook-helper-nbpass28-isolated-1/batch-1'
REL='Tests/URP/Editor/G4FlipbookHelperNBPassTests.cs'
META=REL+'.meta'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def save(name,value):
    p=OUT/name
    assert not p.exists(),f'Do not overwrite previous evidence: {p}'
    p.write_text(json.dumps(value,ensure_ascii=False,indent=2)+'\n',encoding='utf-8',newline='\n')
assert sha(INSTALLED/REL)==sha(PREVIEW/REL)=='442e0792bdb0bcca948e5df95b5c4727cece0f52dea1b669004159a1bbc0db18'
assert sha(INSTALLED/META)==sha(PREVIEW/META)=='8c3271ad6590141757e80d6c623dca45ace202c875e51cd1dcb454a7d03c3bd3'
oldguid=re.search(r'^guid:\s*(\S+)\s*$',(INSTALLED/META).read_text(),re.M).group(1)
newguid=re.search(r'^guid:\s*(\S+)\s*$',(OUT/META).read_text(),re.M).group(1)
assert len(oldguid)==33 and re.fullmatch('[0-9a-f]{32}',newguid)
conflicts=[]
for base in [ROOT/'Packages/NB_FX',INSTALLED]:
    for path in base.rglob('*.meta'):
        if re.search(r'^guid:\s*'+newguid+r'\s*$',path.read_text(encoding='utf-8',errors='replace'),re.M):conflicts.append(str(path))
assert not conflicts,'Corrected GUID already exists: '+str(conflicts)
source=(INSTALLED/REL).read_text(encoding='utf-8')
assert 'const string Package="Packages/com.xuanxuan.nb.fx/", Deferred="NBDeferredDistortPass",Opaque="NBCameraOpaqueDistortPass";' in source
assert 'foreach(string route in new[]{Deferred,Opaque})foreach(int mode in new[]{0,3,4,5,6,7})foreach(bool ortho in new[]{true,false})' in source
assert '[TestCaseSource(nameof(Cases))]' in source
ids=[]
for route in ['NBDeferredDistortPass','NBCameraOpaqueDistortPass']:
    for mode in [0,3,4,5,6,7]:
        for cam in ['ortho','perspective']:
            ids.append(f'NBFX.Baseline.Tests.G4FlipbookHelperNBPassTests.G4FlipbookHelperGPU_{route}_direct{mode}_{cam}')
for route in ['NBDeferredDistortPass','NBCameraOpaqueDistortPass']:
    for cam in ['ortho','perspective']:
        ids.append(f'NBFX.Baseline.Tests.G4FlipbookHelperNBPassTests.G4FlipbookHelperGPU_{route}_shared3_{cam}')
plan=json.loads((PREVIEW/'plan.json').read_text(encoding='utf-8'))
expected=[n for b in plan['batches'] for n in b['exactCaseNames']]
assert len(ids)==len(set(ids))==len(set(expected))==28 and set(ids)==set(expected)
assert all(b['filter']==';'.join(b['exactCaseNames']) and b['expectedCasesFromSource']==7 for b in plan['batches'])
xml=ET.parse(EVIDENCE/'results.xml')
assert not xml.findall('.//test-case')
log=(EVIDENCE/'unity.log').read_text(encoding='utf-8',errors='replace')
assert 'does not have a valid GUID and its corresponding Asset file will be ignored' in log
assert 'No tests were executed.' in log
gpu_matches=[m.group() for m in re.finditer(r'887a000[567]|Crash!!!|failed to create 2D texture|Failed to create RenderTexture',log,re.I)]
save('manifest.json',{
  'scope':'One new-test metadata import repair only; current fixture source/28 identities/strict assertions unchanged',
  'records':[{'relativePath':META,'source':str(OUT/META),'beforeSha256':sha(INSTALLED/META),'afterSha256':sha(OUT/META),
              'change':'Invalid33hex GUID corrected32hex; canonical existing MonoImporter metadata. New ignored test asset has no valid serialized GUID contract.'}],
  'unchangedFixtureSha256':{REL:sha(INSTALLED/REL)},'originalPlanSha256':sha(PREVIEW/'plan.json'),
  'unityExecuted':False,'installed':False,'fullMeshGatePassed':False})
save('discovery-audit.json',{
  'classification':'New test asset ignored at import because metadata GUID malformed; no selected cases executed',
  'sourceAndOriginalPreviewMatch':True,'originalGuid':oldguid,'originalGuidLength':len(oldguid),
  'correctedGuid':newguid,'correctedGuidLength':len(newguid),'correctedGuidCollisions':conflicts,
  'logLines':{'invalidMetaYaml':238,'ignoredAsset':239,'filter':403,'noTestsExecuted':558},
  'xmlSha256':sha(EVIDENCE/'results.xml'),'logSha256':sha(EVIDENCE/'unity.log'),
  'emptyXml':{'xmlResultString':xml.getroot().attrib.get('result'),'total':0,'asserts':0,'actualTestCases':0,
              'validPassedCases':0,'unrunOriginalIdentities':28,'originalEmptyXmlRetained':True},
  'fixtureStaticChecks':{'class':'NBFX.Baseline.Tests.G4FlipbookHelperNBPassTests',
     'CasesLines':[32,37],'staticEnumerableSource':True,'distinctGeneratedCases':28,
     'exactPlanIdentitySetMatches':True,'originalFilterStringsPreserved':True,
     'OneTimeSetUpLines':[27,30],'OneTimeSetUpNotEnteredInEmptyRun':True,
     'OneTimeSetUpRuntimeStillUnverified':'Must import/discover/run after metadata repair; log-only static audit cannot prove warm/type resolution succeeds.'},
  'gpuFailureTokensFound':gpu_matches,
  'noGpuFailureConclusion':'No GPU/crash failure token in this run; empty selected test result is a discovery/import failure, not rendering parity/health evidence.',
  'next':'Parent applies only metadata, serial reimport in safe isolation, reruns original 4x7plan to new evidence directory; actual XML identities/counts mandatory. Never use top-level Passed on empty XML.'})
save('plan.json',plan)
(OUT/'original-invalid-meta.txt').write_bytes((INSTALLED/META).read_bytes())
print(json.dumps({'repairFiles':1,'sourceUnchanged':sha(INSTALLED/REL),'planIdentities':len(ids),'guidLength':len(newguid),'gpuTokens':len(gpu_matches)},ensure_ascii=False))
