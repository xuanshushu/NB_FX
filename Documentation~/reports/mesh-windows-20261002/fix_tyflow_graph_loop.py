from pathlib import Path
import ctypes,hashlib,json
work=Path(__file__).resolve().parent;root=work.parents[1];project=root/'.utmp/NBFXMeshValidation-20261002';package=project/'Packages/NB_FX';preview=work/'tyflow-preview-loop-fixed';backup=work/'tyflow-loop-pre-repair';assert not backup.exists()
lock=project/'Temp/UnityLockfile'
if lock.exists():
    dll=ctypes.WinDLL('kernel32',use_last_error=True);dll.CreateFileW.restype=ctypes.c_void_p;dll.CreateFileW.argtypes=[ctypes.c_wchar_p,ctypes.c_uint32,ctypes.c_uint32,ctypes.c_void_p,ctypes.c_uint32,ctypes.c_uint32,ctypes.c_void_p];dll.CloseHandle.argtypes=[ctypes.c_void_p]
    h=dll.CreateFileW(str(lock),0xC0000000,0,None,3,0x80,None);assert h!=ctypes.c_void_p(-1).value;dll.CloseHandle(h)
records=[]
for r in json.loads((preview/'manifest.json').read_text(encoding='utf-8'))['records']:
    path=package/r['path'];before=path.read_bytes();after=(preview/r['path']).read_bytes()
    if before==after:continue
    assert path.name in ['TyflowVATKernelV1.hlsl','NBGraphVATSoftBody.hlsl'],r['path']
    save=backup/r['path'];save.parent.mkdir(parents=True,exist_ok=True);save.write_bytes(before);path.write_bytes(after)
    records.append({'path':r['path'],'beforeSHA256':hashlib.sha256(before).hexdigest(),'afterSHA256':hashlib.sha256(after).hexdigest()})
assert len(records)==2
(work/'tyflow-loop-repair.json').write_text(json.dumps({'scope':'Graph uniform mode makes full forced unroll too large for compiler. Graph-only bounded7-bone [loop]; current ShaderLab keeps original [unroll]. No arithmetic/order/keyword/pass changes; performance pending.',
 'records':records,'legacyCompileDirectiveUnchanged':True},indent=2)+'\n',encoding='utf-8',newline='\n')
plan=json.loads((work/'customdata-storage32-plan.json').read_text(encoding='utf-8'));name=plan['batches'][0]['exactCaseNames'][0];plan['cases']=1;plan['batches']=[{'batch':1,'filter':name.split('.')[-1],'expectedCasesFromActualDiscovery':1,'exactCaseNames':[name]}]
plan['scope']='Import/warm Graph compile preflight after Tyflow Graph-only loop adaptation; storage1 is not Tyflow functional evidence'
(work/'tyflow-compile-preflight1-plan.json').write_text(json.dumps(plan,indent=2)+'\n',encoding='utf-8',newline='\n')
print('Graph-only loop directive corrected; compile preflight1 prepared')
