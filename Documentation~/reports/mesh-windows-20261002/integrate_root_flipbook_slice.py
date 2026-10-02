from pathlib import Path
import hashlib,json,shutil,subprocess,time

work=Path(__file__).resolve().parent;root=work.parents[1]
product=root/'Packages/NB_FX';clone=root/'.utmp/NBFXMeshValidation-20261002/Packages/NB_FX'
runs=root/'.utmp/nbfx-mesh-current-6000.3.25f1-d3d11-20261002'
source=work/'root-flipbook-rebase/generated';ready=work/'root-flipbook-ready-preview'
manifest=json.loads((source/'manifest.json').read_text(encoding='utf-8'))
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest() if path.exists() else None
for record in manifest['records']:
    assert sha(product/record['path'])==record['beforeSHA256'],record['path']
    assert sha(source/record['path'])==record['afterSHA256'],record['path']
    assert sha(clone/record['path'])==record['afterSHA256'],record['path']
for label,cases in [('root-flipbook54-isolated-1',54),('root-flipbook-downstream68-isolated-1',68)]:
    result=json.loads((runs/label/'batch-summary.json').read_text(encoding='utf-8'))
    assert result['uniqueActualCases']==cases and result['completedBatches']==result['expectedBatches'],label
    assert all(batch['counts']['Passed']==batch['cases'] for batch in result['results']),label
latest136=json.loads((work/'root-f0-regression136-latest-verdicts.json').read_text(encoding='utf-8'))
assert latest136['uniquePassed']==136 and latest136['originalGPUIncidentPreserved']
uvp=json.loads((runs/'root-flipbook-uvp376-isolated-1/batch-summary.json').read_text(encoding='utf-8'))
assert uvp['uniqueActualCases']==376 and uvp['completedBatches']==uvp['expectedBatches']
comparison=json.loads((work/'root-f0-uvp-completed-comparison.json').read_text(encoding='utf-8'))
assert comparison['completed']==comparison['planned']==12 and not comparison['nonBCHealthFailures']
assert all(row['sameVerdictIdentities'] for row in comparison['rows']), 'Changed strict identities require separate review; do not waive them.'
assert sum(row['failed'] for row in comparison['rows'])==105
audit=json.loads((work/'capture-audit-root-flipbook-uvp376-isolated-1-12-batches.json').read_text(encoding='utf-8'))
assert audit['rawCapturesRead']==5652 and not audit['nonfinitePayloads'] and not audit['invalidPNGFiles']
scenePath=work/'main-scenes-before-root-flipbook-integration.json'
scene=json.loads(scenePath.read_text(encoding='utf-8-sig'))['data']['result']['result']
assert time.time()-scenePath.stat().st_mtime<120, 'Fresh loaded-scene inspection required before code write'
assert scene['project']=='D:/UnityProject/NBUnityProject/Assets' and not scene['compiling'] and not scene['updating']
assert all(not row['dirty'] and 'tai' not in (row['name']+' '+row['path']).lower() for row in scene['scenes'] if row['loaded'])
protectedPaths=['NBShaders2/ShaderGraph/Editor/NBGraphUnlitSubTarget.cs','NBShaders2/Editor/NBShaderGraphGUI.cs','NBShaders2/Shader/HLSL/NBShaderFlags.hlsl','XuanXuanRenderUtility/Runtime/ShaderFlagsBase.cs']
protected={relative:sha(product/relative) for relative in protectedPaths}
assert not ready.exists();shutil.copytree(source,ready)
normalized=[]
for record in manifest['records']:
    path=ready/record['path'];before=path.read_bytes()
    # New metadata carries only GUID/storage declarations. Preserve tested code.
    after=before.replace(b'\r\n',b'\n') if path.suffix=='.meta' else before
    if before!=after:
        path.write_bytes(after);normalized.append({'path':record['path'],'verifiedSHA256':record['afterSHA256'],'installedSHA256':sha(path),'onlyMetaCRLFToLF':True})
        record['afterSHA256']=sha(path)
backup=work/'root-flipbook-product-pre-install-backup';assert not backup.exists()
for record in manifest['records']:
    path=product/record['path']
    if path.exists():dest=backup/record['path'];dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(path.read_bytes())
    path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes((ready/record['path']).read_bytes())
    assert sha(path)==record['afterSHA256']
assert protected=={relative:sha(product/relative) for relative in protectedPaths}
manifest.update({'scope':'Reviewed dynamic root OVZ+UVP+F0/Helper/SRP slice only;54/68/136 pass, exact prior105 UVP strict failures retained; no full GUI/Tier/VAT/CustomLocal/Offset/Controller/Player/perf/Gate claim','rootWritten':True,'noG3G4Approval':True,'metaTransferNormalization':normalized,'protectedRootSHA256':protected,'inputPackageHEAD':subprocess.check_output(['git','rev-parse','HEAD'],cwd=product,text=True).strip(),'inputMainHEAD':subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip()})
(ready/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf-8')
(work/'root-flipbook-product-installation.json').write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'installedRootFiles':len(manifest['records']),'graphObjects':1156,'strictFailuresRetained':105,'metadataLFTransfers':len(normalized),'protectedRootUnchanged':True}))
