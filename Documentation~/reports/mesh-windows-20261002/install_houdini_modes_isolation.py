from pathlib import Path
import ctypes,hashlib,json
work=Path(__file__).resolve().parent;root=work.parents[1];project=root/'.utmp/NBFXMeshValidation-20261002';package=project/'Packages/NB_FX'
preview=work/'houdini-modes-preview';backup=work/'houdini-modes-pre-install-backup';assert not backup.exists()
lock=project/'Temp/UnityLockfile'
if lock.exists():
    kernel=ctypes.WinDLL('kernel32',use_last_error=True);kernel.CreateFileW.restype=ctypes.c_void_p
    kernel.CreateFileW.argtypes=[ctypes.c_wchar_p,ctypes.c_uint32,ctypes.c_uint32,ctypes.c_void_p,ctypes.c_uint32,ctypes.c_uint32,ctypes.c_void_p];kernel.CloseHandle.argtypes=[ctypes.c_void_p]
    handle=kernel.CreateFileW(str(lock),0xC0000000,0,None,3,0x80,None)
    assert handle!=ctypes.c_void_p(-1).value,'Isolated Editor still open';kernel.CloseHandle(handle)
manifest=json.loads((preview/'manifest.json').read_text(encoding='utf-8'))
for record in manifest['records']:
    path=package/record['path']
    if record['beforeSHA256'] is None:assert not path.exists()
    else:
        assert hashlib.sha256(path.read_bytes()).hexdigest()==record['beforeSHA256']
        save=backup/record['path'];save.parent.mkdir(parents=True,exist_ok=True);save.write_bytes(path.read_bytes())
    data=(preview/record['path']).read_bytes();assert hashlib.sha256(data).hexdigest()==record['afterSHA256']
    path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(data)
print(json.dumps({'scope':'Isolation only','files':len(manifest['records']),'rootUnchanged':True}))
