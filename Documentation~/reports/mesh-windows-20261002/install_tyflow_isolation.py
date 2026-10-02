from pathlib import Path
import ctypes,hashlib,json,shutil
work=Path(__file__).resolve().parent;root=work.parents[1];project=root/'.utmp/NBFXMeshValidation-20261002';package=project/'Packages/NB_FX'
preview=work/'tyflow-preview-v2';backup=work/'tyflow-pre-install-backup';assert not backup.exists()
lock=project/'Temp/UnityLockfile'
if lock.exists():
    kernel=ctypes.WinDLL('kernel32',use_last_error=True);kernel.CreateFileW.restype=ctypes.c_void_p
    kernel.CreateFileW.argtypes=[ctypes.c_wchar_p,ctypes.c_uint32,ctypes.c_uint32,ctypes.c_void_p,ctypes.c_uint32,ctypes.c_uint32,ctypes.c_void_p];kernel.CloseHandle.argtypes=[ctypes.c_void_p]
    handle=kernel.CreateFileW(str(lock),0xC0000000,0,None,3,0x80,None);assert handle!=ctypes.c_void_p(-1).value,'Isolated Editor still open';kernel.CloseHandle(handle)
manifest=json.loads((preview/'manifest.json').read_text(encoding='utf-8'))
for r in manifest['records']:
    p=package/r['path'];data=(preview/r['path']).read_bytes();assert hashlib.sha256(data).hexdigest()==r['afterSHA256']
    if r['beforeSHA256'] is None:assert not p.exists()
    else:
        assert hashlib.sha256(p.read_bytes()).hexdigest()==r['beforeSHA256'];save=backup/r['path'];save.parent.mkdir(parents=True,exist_ok=True);save.write_bytes(p.read_bytes())
    p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(data)
test=package/'Tests/URP/Editor/G4GraphTyflowVATTests.cs';assert not test.exists();shutil.copyfile(work/test.name,test)
test.with_suffix('.cs.meta').write_text('fileFormatVersion: 2\nguid: dc86d77fb81c5a1a885f1276cdfd5981\n',encoding='utf-8',newline='\n')
print(json.dumps({'files':len(manifest['records']),'newFixture':True,'isolationOnly':True}))
