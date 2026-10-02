from pathlib import Path
import ctypes,hashlib,json,re
work=Path(__file__).resolve().parent;root=work.parents[1];project=root/'.utmp/NBFXMeshValidation-20261002';package=project/'Packages/NB_FX';backup=work/'tyflow-meta-error-backup';assert not backup.exists()
lock=project/'Temp/UnityLockfile'
if lock.exists():
    dll=ctypes.WinDLL('kernel32',use_last_error=True);dll.CreateFileW.restype=ctypes.c_void_p;dll.CreateFileW.argtypes=[ctypes.c_wchar_p,ctypes.c_uint32,ctypes.c_uint32,ctypes.c_void_p,ctypes.c_uint32,ctypes.c_uint32,ctypes.c_void_p];dll.CloseHandle.argtypes=[ctypes.c_void_p]
    h=dll.CreateFileW(str(lock),0xC0000000,0,None,3,0x80,None);assert h!=ctypes.c_void_p(-1).value;dll.CloseHandle(h)
records=[]
for name in ['Tests/URP/Editor/G4GraphTyflowVATTests.cs.meta','XuanXuanRenderUtility/Shader/HLSL/TyflowVATKernelV1.hlsl.meta']:
    path=package/name;before=path.read_bytes();s=before.decode('utf-8');m=re.search(r'guid: ([0-9a-f]+)',s);assert len(m[1])==33
    save=backup/name;save.parent.mkdir(parents=True,exist_ok=True);save.write_bytes(before);path.write_text(s[:m.start(1)]+m[1][:32]+s[m.end(1):],encoding='utf-8',newline='\n')
    records.append({'path':name,'beforeSHA256':hashlib.sha256(before).hexdigest(),'afterSHA256':hashlib.sha256(path.read_bytes()).hexdigest()})
for folder in ['NBShaders2','XuanXuanRenderUtility','Tests']:
    for path in (package/folder).rglob('*.meta'):
        m=re.search(r'(?m)^guid: ([^\s]+)',path.read_text(encoding='utf-8'));assert m and re.fullmatch('[0-9a-f]{32}',m[1]),path
(work/'tyflow-meta-repair.json').write_text(json.dumps({'scope':'Correct malformed new GUID length; prior two CLI attempts executed zero cases and are not pass evidence; all importable package meta GUID lengths now checked','records':records},indent=2)+'\n',encoding='utf-8',newline='\n')
print('Corrected two metadata GUIDs, validated all executable package GUID formats')
