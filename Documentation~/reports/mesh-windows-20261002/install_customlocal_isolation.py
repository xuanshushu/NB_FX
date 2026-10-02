from pathlib import Path
import ctypes,json,hashlib
work=Path(__file__).resolve().parent;root=work.parents[1];project=root/'.utmp/NBFXMeshValidation-20261002';package=project/'Packages/NB_FX';preview=work/'customlocal-combined-preview';backup=work/'customlocal-pre-install-backup';assert not backup.exists()
lock=project/'Temp/UnityLockfile'
if lock.exists():
    dll=ctypes.WinDLL('kernel32',use_last_error=True);dll.CreateFileW.restype=ctypes.c_void_p;dll.CreateFileW.argtypes=[ctypes.c_wchar_p,ctypes.c_uint32,ctypes.c_uint32,ctypes.c_void_p,ctypes.c_uint32,ctypes.c_uint32,ctypes.c_void_p];dll.CloseHandle.argtypes=[ctypes.c_void_p]
    h=dll.CreateFileW(str(lock),0xC0000000,0,None,3,0x80,None);assert h!=ctypes.c_void_p(-1).value;dll.CloseHandle(h)
manifest=json.loads((preview/'manifest.json').read_text(encoding='utf-8'))
for r in manifest['records']:
    path=package/r['path'];after=(preview/r['path']).read_bytes();assert hashlib.sha256(after).hexdigest()==r['afterSHA256']
    if r['beforeSHA256'] is None:assert not path.exists()
    else:
        before=path.read_bytes();assert hashlib.sha256(before).hexdigest()==r['beforeSHA256'];save=backup/r['path'];save.parent.mkdir(parents=True,exist_ok=True);save.write_bytes(before)
    path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(after)
print(json.dumps({'installedFiles':len(manifest['records']),'isolationOnly':True,'rootUnchanged':True,'noTAISceneBatch':True}))
