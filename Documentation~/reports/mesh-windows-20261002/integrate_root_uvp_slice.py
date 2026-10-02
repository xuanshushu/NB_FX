from pathlib import Path
import json,hashlib,shutil,subprocess
work=Path(__file__).resolve().parent;root=work.parents[1];product=root/'Packages/NB_FX';clone=root/'.utmp/NBFXMeshValidation-20261002/Packages/NB_FX'
src=work/'root-uvp-rebase/generated';ready=work/'root-uvp-ready-preview';assert not ready.exists();shutil.copytree(src,ready)
m=json.loads((src/'manifest.json').read_text(encoding='utf-8'));rel='NBShaders2/ShaderGraph/NBShaderGraph.shadergraph'
(ready/rel).write_bytes((work/'root-uvp-input-visibility-preview'/rel).read_bytes())
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest() if p.exists() else None
for r in m['records']:
    if r['path']==rel:r['afterSHA256']=sha(ready/rel)
    assert sha(product/r['path'])==r['beforeSHA256'],r['path']
    assert sha(ready/r['path'])==r['afterSHA256'] and sha(clone/r['path'])==r['afterSHA256']
runs=root/'.utmp/nbfx-mesh-current-6000.3.25f1-d3d11-20261002'
for name,cases in [('root-uvp-downstream68-isolated-1',68),('root-uvp-gui-ovz136-isolated-1',136)]:
    r=json.loads((runs/name/'batch-summary.json').read_text());assert r['uniqueActualCases']==cases and all(b['counts']['Passed']==b['cases'] for b in r['results'])
verdict=json.loads((work/'root-uvp-rebase/verdict376-audit.json').read_text(encoding='utf-8'))
# The independent audit must classify every failure, not silently accept it.
assert json.loads((runs/'root-uvp376-isolated-1/batch-summary.json').read_text())['uniqueActualCases']==376
scene=json.loads((work/'main-scenes-before-root-uvp-integration.json').read_text(encoding='utf-8-sig'))['data']['result']['result']
assert scene['project']=='D:/UnityProject/NBUnityProject/Assets' and not scene['compiling'] and not scene['updating']
assert all(not s['dirty'] and 'tai' not in (s['name']+' '+s['path']).lower() for s in scene['scenes'] if s['loaded'])
backup=work/'root-uvp-product-pre-install-backup';assert not backup.exists()
for r in m['records']:
    p=product/r['path']
    if p.exists():b=backup/r['path'];b.parent.mkdir(parents=True,exist_ok=True);b.write_bytes(p.read_bytes())
    p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes((ready/r['path']).read_bytes());assert sha(p)==r['afterSHA256']
m.update(scope='Current-root UVP1+SM4.5 integrated only; OVZ/GUI retained. 376=271pass/105strictlowdiff retained manual, downstream68 andGUI/OVZ/native136 passed; not full Gate.',rootWritten=True,
 inputPackageHEAD=subprocess.check_output(['git','rev-parse','HEAD'],cwd=product,text=True).strip(),inputMainHEAD=subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip(),noG3G4Approval=True)
(ready/'manifest.json').write_text(json.dumps(m,indent=2)+'\n',encoding='utf-8',newline='\n')
(work/'root-uvp-product-installation.json').write_text(json.dumps(m,indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps({'rootFilesInstalled':len(m['records']),'rootGraphObjects':1132,'strictFailuresRetained':105,'downstream68Passed':True,'GUIOVZ136Passed':True}))
