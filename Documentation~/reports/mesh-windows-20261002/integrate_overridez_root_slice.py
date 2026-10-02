"""Install only the verified current-root-derived slice; protect all other root paths."""
from pathlib import Path
import hashlib,json,shutil,subprocess,xml.etree.ElementTree as ET
work=Path(__file__).resolve().parent;root=work.parents[1];package=root/'Packages/NB_FX'
ready=work/'overridez-root-ready-preview';before=work/'overridez-root-keyword-authority-preview'
assert not ready.exists();shutil.copytree(before,ready)
m=json.loads((before/'manifest.json').read_text());source='Tests/URP/Editor/G4GraphOverrideDepthSourceTests.cs'
(ready/source).write_bytes((work/'overridez-source-warm-repair-preview'/source).read_bytes())
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest() if p.exists() else None
for r in m['records']:
    if r['path']==source:r['afterSHA256']=sha(ready/source)
    assert sha(package/r['path'])==r['beforeSHA256'], 'Root changed; regenerate: '+r['path']
    assert sha(ready/r['path'])==r['afterSHA256']
evidence=root/'.utmp/nbfx-mesh-current-6000.3.25f1-d3d11-20261002'
plan=json.loads((work/'overridez-root-validation-plan.json').read_text())
run=evidence/'overridez-root-slice234-isolated-1';verdicts={}
for batch in plan['batches']:
    cases=ET.parse(run/('batch-'+str(batch['batch']))/'results.xml').findall('.//test-case')
    assert {c.get('fullname') for c in cases}==set(batch['exactCaseNames'])
    verdicts.update({c.get('fullname'):c.get('result') for c in cases})
repair=evidence/'overridez-root-source2-warmed-isolated-1/batch-1'
cases=ET.parse(repair/'results.xml').findall('.//test-case')
assert len(cases)==2 and all(c.get('result')=='Passed' for c in cases)
verdicts.update({c.get('fullname'):c.get('result') for c in cases})
assert len(verdicts)==234 and set(verdicts.values())=={'Passed'}
compat=json.loads((repair/'captures/overridez-source/srp-batcher-subshader0.json').read_text())
assert compat['warmPerformed'] and all(r['code']==0 for r in compat['shaders'] if r['role'] in ['current','graph'])
scene=json.loads((work/'main-scenes-before-overridez-integration.json').read_text(encoding='utf-8-sig'))['data']['result']['result']
assert scene['project']=='D:/UnityProject/NBUnityProject/Assets' and not scene['compiling'] and not scene['updating']
assert all(not s['dirty'] and 'tai' not in (s['name']+' '+s['path']).lower() for s in scene['scenes'] if s['loaded'])
protect=['Packages/manifest.json','Packages/packages-lock.json','ProjectSettings/ProjectVersion.txt','ProjectSettings/ShaderGraphSettings.asset','Assets/测试包.meta']
protected={name:sha(root/name) for name in protect}
backup=work/'overridez-root-pre-integration-backup';assert not backup.exists()
for r in m['records']:
    target=package/r['path']
    if target.exists():
        saved=backup/r['path'];saved.parent.mkdir(parents=True,exist_ok=True);saved.write_bytes(target.read_bytes())
    target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes((ready/r['path']).read_bytes())
    assert sha(target)==r['afterSHA256']
assert all(sha(root/name)==digest for name,digest in protected.items())
m['scope']='Verified current-root-derived OverrideZ normal Forward + assignment/runtime keyword contract; integrated product slice only. Not full advanced states/G3/G4.'
m['rootIntegration']=True;m['latestUniqueRootSliceVerdicts']=234;m['original234Result']='233 Passed/1 native uninitialized failure retained';m['sourceWarmRepair']='2/2 exact original identities passed; not 236 unique tests'
(ready/'manifest.json').write_text(json.dumps(m,indent=2)+'\n',encoding='utf-8',newline='\n')
receipt={**m,'rootPackageBeforeHEAD':subprocess.check_output(['git','rev-parse','HEAD'],cwd=package,text=True).strip(),'mainBeforeHEAD':subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip(),
 'protectedRootFiles':protected,'mainLoadedScenes':scene['scenes'],'noTAI':True,'isolatedCombined370':'370/370; separate candidate/source and overlapping identities not added',
 'guiReaderNotIntegrated':True,'renderCandidatesOtherThanOverrideZNotIntegrated':True,'noPush':True}
(work/'overridez-root-integration-installation.json').write_text(json.dumps(receipt,indent=2,ensure_ascii=False)+'\n',encoding='utf-8',newline='\n')
paths=['Packages/com.xuanxuan.nb.fx/'+r['path'] for r in m['records'] if not r['path'].endswith('.meta')]
script='''if(UnityEngine.Application.dataPath.Replace('\\\\','/')!="D:/UnityProject/NBUnityProject/Assets")throw new System.InvalidOperationException("Wrong project");
for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++){var s=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);if(s.isLoaded&&(s.name+" "+s.path).IndexOf("TAI",System.StringComparison.OrdinalIgnoreCase)>=0)throw new System.InvalidOperationException("TAI Domain Reload protection");}
string[] paths={'''+','.join(json.dumps(p) for p in paths)+'''};
foreach(var path in paths)UnityEditor.AssetDatabase.ImportAsset(path,UnityEditor.ImportAssetOptions.ForceUpdate);
return new {requestedPaths=paths,compiling=UnityEditor.EditorApplication.isCompiling,updating=UnityEditor.EditorApplication.isUpdating};
'''
(work/'import_main_overridez_slice.cs').write_text(script,encoding='utf-8',newline='\n')
print(json.dumps({'integratedFiles':len(m['records']),'rootGraphObjects':m['graphObjectsAfter'],'latestUniqueCases':234,'protectedRootFilesUnchanged':True,'otherCandidatesStillIsolated':True}))
