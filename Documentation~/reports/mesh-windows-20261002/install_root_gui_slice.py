"""Install only the verified, dynamically rebased GUI slice, with byte backups."""
from pathlib import Path
import hashlib
import json
import subprocess

work=Path(__file__).resolve().parent
root=work.parents[1]
package=root/'Packages/NB_FX'
clone=root/'.utmp/NBFXMeshValidation-20261002/Packages/NB_FX'
runs=root/'.utmp/nbfx-mesh-current-6000.3.25f1-d3d11-20261002'
for name,total in (('gui1c-root-slice110-isolated-final',110),('gui1c-root-mesh45-isolated-final',45)):
    summary=json.loads((runs/name/'batch-summary.json').read_text(encoding='utf-8'))
    assert summary['uniqueActualCases']==total
    assert sum(r['counts']['Passed'] for r in summary['results'])==total
    assert all(r['counts']['Failed']==r['counts']['Skipped']==r['counts']['Inconclusive']==0 for r in summary['results'])
scene=json.loads((work/'main-scenes-before-gui1c-root-slice.json').read_text(encoding='utf-8-sig'))
assert scene['success'] and scene['data']['result']['success']
live=scene['data']['result']['result']
assert live['project'].replace('\\','/')==str(root/'Assets').replace('\\','/')
assert live['unity']=='6000.3.25f1' and not live['compiling'] and not live['updating']
assert all('tai' not in (s['name']+' '+s['path']).lower() for s in live['scenes'] if s['loaded'])
info=json.loads((work/'main-package-before-gui1c-root-slice.json').read_text(encoding='utf-8-sig'))
assert info['success'] and Path(info['data']['result']['result']['package']['resolvedPath']).resolve()==package.resolve()

product=['NBShaders2/Editor/ShaderGUIItems/NBShaderSyncService.cs','NBShaders2/Editor/NBShaderGraphRootItem.cs','NBShaders2/ShaderGraph/NBShaderGraph.shadergraph']
tests=['Tests/URP/Editor/G4GraphGuiStorageTests.cs','Tests/URP/Editor/G4GraphGuiMainTextureTests.cs','Tests/URP/Editor/G4GraphGuiFeatureIntentTests.cs','Tests/URP/Editor/G4GraphGuiFeatureIntentTests.cs.meta']
assert subprocess.run(['git','diff','--quiet','--']+product,cwd=package).returncode==0,'Do not overwrite pre-existing product changes'
graph=product[2]
assert hashlib.sha256((clone/graph).read_bytes()).hexdigest()=='c6ab58b7af69bcd4fe6d4383516d41589695c37fc4de3a1fc9402c7858ea3a5b'
for name in product[:2]:
    assert (clone/name).read_bytes()==(work/'gui1c-combined-source-before-rebase'/name).read_bytes()
protected=['Packages/manifest.json','Packages/packages-lock.json','ProjectSettings/ProjectVersion.txt','Packages/NB_FX/NBShaders2/Shader/NBShader.shader']
before={name:hashlib.sha256((root/name).read_bytes()).hexdigest() for name in protected}
backup=work/'gui1c-root-pre-install-backup'
assert not backup.exists()
records=[]
for name in product+tests:
    destination=package/name
    previous=destination.read_bytes() if destination.exists() else None
    if previous is not None:
        saved=backup/name;saved.parent.mkdir(parents=True,exist_ok=True);saved.write_bytes(previous)
    candidate=(clone/name).read_bytes()
    destination.parent.mkdir(parents=True,exist_ok=True);destination.write_bytes(candidate)
    assert destination.read_bytes()==candidate
    records.append({'path':name,'beforeSHA256':hashlib.sha256(previous).hexdigest() if previous is not None else None,'afterSHA256':hashlib.sha256(candidate).hexdigest()})
assert before=={name:hashlib.sha256((root/name).read_bytes()).hexdigest() for name in protected}
receipt={'scope':'Only GUI seed/readback slice installed to actual embedded root; rendering candidates remain isolated',
 'realGUIVerdicts':110,'realMeshSideEffectVerdicts':45,'noTAISceneAtInstallation':True,
 'noShaderHLSLOrSubTargetChange':True,'noOfficialPackageEdit':True,'formalVFXStarted':False,'fullMeshGatePassed':False,
 'rootBackup':str(backup),'protectedSHA256':before,'files':records,'noPush':True}
(work/'gui1c-root-installation.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps(receipt,indent=2))
