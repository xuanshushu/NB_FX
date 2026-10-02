from pathlib import Path
import ctypes,json,hashlib
work=Path(__file__).resolve().parent;root=work.parents[1];project=root/'.utmp/NBFXMeshValidation-20261002';package=project/'Packages/NB_FX';preview=work/'tyflow-preview-final';backup=work/'tyflow-gamma-pre-repair';assert not backup.exists()
lock=project/'Temp/UnityLockfile'
if lock.exists():
    dll=ctypes.WinDLL('kernel32',use_last_error=True);dll.CreateFileW.restype=ctypes.c_void_p;dll.CreateFileW.argtypes=[ctypes.c_wchar_p,ctypes.c_uint32,ctypes.c_uint32,ctypes.c_void_p,ctypes.c_uint32,ctypes.c_uint32,ctypes.c_void_p];dll.CloseHandle.argtypes=[ctypes.c_void_p]
    h=dll.CreateFileW(str(lock),0xC0000000,0,None,3,0x80,None);assert h!=ctypes.c_void_p(-1).value;dll.CloseHandle(h)
manifest=json.loads((preview/'manifest.json').read_text(encoding='utf-8'))
records=[]
for r in manifest['records']:
    path=package/r['path'];before=path.read_bytes();after=(preview/r['path']).read_bytes()
    if before==after:continue
    assert path.name=='TyflowVATKernelV1.hlsl','All prior punctuation/binding/GUID repairs must reproduce byte-identically: '+r['path']
    save=backup/r['path'];save.parent.mkdir(parents=True,exist_ok=True);save.write_bytes(before);path.write_bytes(after)
    records.append({'path':r['path'],'beforeSHA256':hashlib.sha256(before).hexdigest(),'afterSHA256':hashlib.sha256(after).hexdigest()})
assert len(records)==1
(work/'tyflow-gamma-repair.json').write_text(json.dumps({'scope':'Original Tyflow uses UnityCG scalar exact gamma unavailable in SG host; extracted installed6000.3.25f1 exact function into shared kernel with unique name; original operation order/constants preserved. Official files unchanged.',
 'records':records,'allOtherPreviewFilesByteIdenticalToCorrectedIsolation':True},indent=2)+'\n',encoding='utf-8',newline='\n')
print('Shared exact gamma entry corrected; all other final-preview files reproduce current bytes')
