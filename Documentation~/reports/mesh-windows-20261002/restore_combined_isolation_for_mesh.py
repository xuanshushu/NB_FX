from pathlib import Path
import ctypes
import hashlib
import json
work=Path(__file__).resolve().parent
root=work.parents[1]
project=root/'.utmp/NBFXMeshValidation-20261002'
clone=project/'Packages/NB_FX'
source=work/'gui1c-combined-source-before-rebase'
lock=project/'Temp/UnityLockfile'
kernel=ctypes.WinDLL('kernel32',use_last_error=True)
kernel.CreateFileW.restype=ctypes.c_void_p
kernel.CreateFileW.argtypes=[ctypes.c_wchar_p,ctypes.c_uint32,ctypes.c_uint32,ctypes.c_void_p,ctypes.c_uint32,ctypes.c_uint32,ctypes.c_void_p]
kernel.CloseHandle.argtypes=[ctypes.c_void_p]
if lock.exists():
    handle=kernel.CreateFileW(str(lock),0xC0000000,0,None,3,0x80,None)
    assert handle!=ctypes.c_void_p(-1).value,'Owned isolated Editor still active'
    kernel.CloseHandle(handle)
assert clone.resolve().is_relative_to((root/'.utmp').resolve())
quarantine=work/'gui1c-historical-vfx-post-import-quarantine'
assert not quarantine.exists();quarantine.mkdir()
quarantined=[]
for name in ('NBGraphVFXMeshMinimum.vfx','NBGraphVFXMeshMinimum.vfx.meta'):
    path=clone/'NBShaders2/ShaderGraph/Samples'/name
    assert path.resolve().is_relative_to(clone.resolve()) and path.is_file()
    data=path.read_bytes();(quarantine/name).write_bytes(data)
    assert (quarantine/name).read_bytes()==data
    quarantined.append({'path':path.relative_to(clone).as_posix(),'sha256':hashlib.sha256(data).hexdigest()})
    path.unlink() # Two explicitly validated owned-clone files only; originals retained.
manifest=json.loads((source/'manifest.json').read_text(encoding='utf-8'))
for item in manifest['changes']:
    rel=Path(item['path']);assert not rel.is_absolute() and '..' not in rel.parts
    data=(source/rel).read_bytes();assert hashlib.sha256(data).hexdigest()==item['sha256']
    target=clone/rel;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(data)
result={'scope':'Restore preserved combined UVP/GUI1C/F0/V0 source only in owned Mesh validation clone',
 'rootMainHEAD':'8cea287082b5c38d6f0617c010361a44e5af07e2','rootPackageHEAD':'5e2acdad7856c3912cb21501e588619495df0ed7',
 'rootGraphSHA256':'c6ab58b7af69bcd4fe6d4383516d41589695c37fc4de3a1fc9402c7858ea3a5b',
 'restoredFiles':len(manifest['changes']),'historicalSampleQuarantined':quarantined,'formalVFXStarted':False,
 'combinedGraphSHA256':hashlib.sha256((clone/'NBShaders2/ShaderGraph/NBShaderGraph.shadergraph').read_bytes()).hexdigest(),
 'noMainProjectChange':True,'noPush':True,'next':'GUI2 Noise/NoiseMask pair using shared resolver/dependencies and real CF allow inputs; no full Tier claim'}
(work/'CURRENT_STATE.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps(result,indent=2))
