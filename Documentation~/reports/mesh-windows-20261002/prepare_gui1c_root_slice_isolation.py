"""Preserve the combined candidate, then isolate a reversible GUI-only root slice."""
from pathlib import Path
import ctypes
import hashlib
import json
import shutil

work=Path(__file__).resolve().parent
root=work.parents[1]
package=root/'Packages/NB_FX'
project=root/'.utmp/NBFXMeshValidation-20261002'
clone=project/'Packages/NB_FX'
assert project.resolve()==Path('D:/UnityProject/NBUnityProject/.utmp/NBFXMeshValidation-20261002')
lock=project/'Temp/UnityLockfile'
if lock.exists():
    kernel=ctypes.WinDLL('kernel32',use_last_error=True)
    kernel.CreateFileW.restype=ctypes.c_void_p
    kernel.CreateFileW.argtypes=[ctypes.c_wchar_p,ctypes.c_uint32,ctypes.c_uint32,ctypes.c_void_p,ctypes.c_uint32,ctypes.c_uint32,ctypes.c_void_p]
    kernel.CloseHandle.argtypes=[ctypes.c_void_p]
    handle=kernel.CreateFileW(str(lock),0xC0000000,0,None,3,0x80,None)
    assert handle!=ctypes.c_void_p(-1).value, 'Owned project still active, error='+str(ctypes.get_last_error())
    kernel.CloseHandle(handle)

assert hashlib.sha256((package/'NBShaders2/ShaderGraph/NBShaderGraph.shadergraph').read_bytes()).hexdigest()=='24bb84c9f37b34256f305a86dad0e3b4e4f74ee9db7895fec64f906723284da0'
saved=work/'gui1c-combined-source-before-rebase'
assert not saved.exists()
changes=[]
for path in sorted(clone.rglob('*')):
    if not path.is_file():continue
    rel=path.relative_to(clone)
    if rel.parts[0] not in ('NBShaders2','XuanXuanRenderUtility','Tests'):continue
    original=package/rel
    data=path.read_bytes()
    if original.exists() and original.read_bytes().replace(b'\r\n',b'\n')==data.replace(b'\r\n',b'\n'):continue
    target=saved/rel;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(data)
    changes.append({'path':rel.as_posix(),'sha256':hashlib.sha256(data).hexdigest(),'rootSHA256':hashlib.sha256(original.read_bytes()).hexdigest() if original.exists() else None})
(saved/'manifest.json').write_text(json.dumps({'scope':'Preserved UVP/GUI1C/F0/V0 combined isolation source; GUI110 passed; no root installation','changes':changes},indent=2)+'\n',encoding='utf-8',newline='\n')

sync='NBShaders2/Editor/ShaderGUIItems/NBShaderSyncService.cs'
guiroot='NBShaders2/Editor/NBShaderGraphRootItem.cs'
graph='NBShaders2/ShaderGraph/NBShaderGraph.shadergraph'
sync_data=(clone/sync).read_bytes();guiroot_data=(clone/guiroot).read_bytes()
original_sync=(package/sync).read_text(encoding='utf-8')
candidate_sync=sync_data.decode('utf-8')
# The seed/readback is confined to one existing section; all legacy sync after it is exact.
assert original_sync[original_sync.index('        public void SyncMaterialState()'):]==candidate_sync[candidate_sync.index('        public void SyncMaterialState()'):]
copied=0
for folder in ('NBShaders2','XuanXuanRenderUtility'):
    for path in sorted((package/folder).rglob('*')):
        if not path.is_file():continue
        target=clone/path.relative_to(package);target.parent.mkdir(parents=True,exist_ok=True)
        target.write_bytes(path.read_bytes());copied+=1

preview=work/'gui1c-root-rebased-preview'
graph_data=(preview/graph).read_bytes()
(clone/graph).write_bytes(graph_data);(clone/sync).write_bytes(sync_data);(clone/guiroot).write_bytes(guiroot_data)
quarantine=work/'historical-vfx-sample-quarantine'
for receipt in json.loads((quarantine/'source-sha256.json').read_text(encoding='utf-8-sig')):
    path=Path(receipt['Path'])
    assert path.is_relative_to(clone)
    assert hashlib.sha256(path.read_bytes()).hexdigest()==receipt['Hash'].lower()

result={'scope':'GUI-only candidate derived from unchanged current root; combined candidate preserved, not overwritten',
    'baselineProductFilesCopiedOnlyInsideIsolation':copied,'rootNotModified':True,
    'combinedSnapshotFiles':len(changes),'combinedSnapshot':str(saved),
    'restoredHistoricalVFXSampleByteIdentical':True,'formalVFXValidationStarted':False,
    'rootSliceFiles':[{'path':name,'sha256':hashlib.sha256((clone/name).read_bytes()).hexdigest()} for name in (sync,guiroot,graph)]}
(work/'gui1c-root-slice-isolation.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps(result,indent=2))
