from pathlib import Path
import json,hashlib,ctypes
work=Path(__file__).resolve().parent;root=work.parents[1];project=root/'.utmp/NBFXMeshValidation-20261002';package=project/'Packages/NB_FX';preview=work/'tyflow-preview-pass-guard-fixed'
lock=project/'Temp/UnityLockfile'
if lock.exists():
    dll=ctypes.WinDLL('kernel32',use_last_error=True);dll.CreateFileW.restype=ctypes.c_void_p;dll.CreateFileW.argtypes=[ctypes.c_wchar_p,ctypes.c_uint32,ctypes.c_uint32,ctypes.c_void_p,ctypes.c_uint32,ctypes.c_uint32,ctypes.c_void_p];dll.CloseHandle.argtypes=[ctypes.c_void_p]
    h=dll.CreateFileW(str(lock),0xC0000000,0,None,3,0x80,None);assert h!=ctypes.c_void_p(-1).value;dll.CloseHandle(h)
records=[];backup=work/'tyflow-pass-guard-pre-repair';assert not backup.exists()
for r in json.loads((preview/'manifest.json').read_text(encoding='utf-8'))['records']:
    path=package/r['path'];before=path.read_bytes();after=(preview/r['path']).read_bytes()
    if before==after:continue
    assert path.name=='NBGraphVATSoftBody.hlsl';save=backup/r['path'];save.parent.mkdir(parents=True,exist_ok=True);save.write_bytes(before);path.write_bytes(after)
    records.append({'path':r['path'],'beforeSHA256':hashlib.sha256(before).hexdigest(),'afterSHA256':hashlib.sha256(after).hexdigest()})
assert len(records)==1
(work/'tyflow-pass-guard-repair.json').write_text(json.dumps({'scope':'Graph-only actual legacy guard correction. NB depth/shadow defines NB_DEPTH_ONLY_PASS/NB_SHADOW_CASTER_PASS, not SHADOWS_DEPTH. Frozen and current skin normals remain animated in these passes; old AffectsShadows guard is inactive here. Prior alias incorrectly zeroed Graph skin normal in ShadowCaster. No legacy fix or API reinterpretation.',
 'records':records,'mathChanged':False,'officialTargetChanged':False},indent=2)+'\n',encoding='utf-8',newline='\n')
base=json.loads((work/'all-vat-geometry40-plan.json').read_text(encoding='utf-8'));names=[n for b in base['batches'] for n in b['exactCaseNames'] if any('G4VATGeometry_t'+str(mode)+'_shadow' in n for mode in range(2,6))];assert len(names)==8
base['cases']=8;base['scope']='Tyflow actual legacy-pass guard fix: eight skin ShadowCaster cases. Keep original failed40 XML, no lowered tolerance.'
base['batches']=[{'batch':1,'filter':';'.join('G4VATGeometry_t'+str(mode)+'_shadow' for mode in range(2,6)),'expectedCasesFromActualDiscovery':8,'exactCaseNames':names}]
(work/'tyflow-shadow-guard8-plan.json').write_text(json.dumps(base,indent=2)+'\n',encoding='utf-8',newline='\n')
print('Corrected actual macro contract; unrelated current bytes equal preview; prepared shadow8')
