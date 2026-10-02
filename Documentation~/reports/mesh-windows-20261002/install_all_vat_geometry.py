from pathlib import Path
import ctypes,json,hashlib,shutil,uuid
work=Path(__file__).resolve().parent;root=work.parents[1];project=root/'.utmp/NBFXMeshValidation-20261002';package=project/'Packages/NB_FX';preview=work/'all-vat-geometry-preview'
lock=project/'Temp/UnityLockfile'
if lock.exists():
    dll=ctypes.WinDLL('kernel32',use_last_error=True);dll.CreateFileW.restype=ctypes.c_void_p;dll.CreateFileW.argtypes=[ctypes.c_wchar_p,ctypes.c_uint32,ctypes.c_uint32,ctypes.c_void_p,ctypes.c_uint32,ctypes.c_uint32,ctypes.c_void_p];dll.CloseHandle.argtypes=[ctypes.c_void_p]
    h=dll.CreateFileW(str(lock),0xC0000000,0,None,3,0x80,None);assert h!=ctypes.c_void_p(-1).value;dll.CloseHandle(h)
manifest=json.loads((preview/'manifest.json').read_text(encoding='utf-8'));p=package/manifest['path'];assert hashlib.sha256(p.read_bytes()).hexdigest()==manifest['beforeSHA256']
backup=work/'G4GraphVATTests-before-all-mode-geometry.cs';assert not backup.exists();shutil.copyfile(p,backup)
data=(preview/manifest['path']).read_bytes();assert hashlib.sha256(data).hexdigest()==manifest['afterSHA256'];p.write_bytes(data)
fixture=package/'Tests/URP/Editor/G4GraphVATGeometryModesTests.cs';assert not fixture.exists();shutil.copyfile(work/fixture.name,fixture)
guid=uuid.uuid5(uuid.UUID('3f9fdcdd-c996-4c7b-b3ca-83775e72cb22'),fixture.name).hex
fixture.with_suffix('.cs.meta').write_text('fileFormatVersion: 2\nguid: '+guid+'\n',encoding='utf-8',newline='\n')
plan={'scope':'TenVAT modes selected actual DepthOnly/ShadowCaster ABC; Graph extraDepthNormals excluded andSSAOreceiver kept. Not default fullpipeline/VFX/Player claim','unity':'6000.3.25f1','api':'Direct3D11','cases':40,'batches':[]}
for batch,ty in enumerate([False,True],1):
    names=['NBFX.Baseline.Tests.G4GraphVATGeometryModesTests.G4VATGeometry_'+('t' if ty else 'h')+str(mode)+('_shadow' if shadow else '_depth')+('_ortho' if o else '_perspective') for mode in range(6 if ty else 4) for shadow in [False,True] for o in [True,False]]
    plan['batches'].append({'batch':batch,'filter':'G4VATGeometry_'+('t' if ty else 'h'),'expectedCasesFromActualDiscovery':len(names),'exactCaseNames':names})
(work/'all-vat-geometry40-plan.json').write_text(json.dumps(plan,indent=2)+'\n',encoding='utf-8',newline='\n')
print('Test-only directed callbacks installed, metadata generated as32hex; geometry40 plan prepared')
