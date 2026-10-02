from pathlib import Path
import ctypes,json,hashlib,sys,copy
from graph_preview_helpers import GraphPreview
work=Path(__file__).resolve().parent;root=work.parents[1];project=root/'.utmp/NBFXMeshValidation-20261002';package=project/'Packages/NB_FX';backup=work/'tyflow-import-error-backup';assert not backup.exists()
lock=project/'Temp/UnityLockfile'
if lock.exists():
    dll=ctypes.WinDLL('kernel32',use_last_error=True);dll.CreateFileW.restype=ctypes.c_void_p;dll.CreateFileW.argtypes=[ctypes.c_wchar_p,ctypes.c_uint32,ctypes.c_uint32,ctypes.c_void_p,ctypes.c_uint32,ctypes.c_uint32,ctypes.c_void_p];dll.CloseHandle.argtypes=[ctypes.c_void_p]
    h=dll.CreateFileW(str(lock),0xC0000000,0,None,3,0x80,None);assert h!=ctypes.c_void_p(-1).value;dll.CloseHandle(h)
records=[]
kernel=package/'XuanXuanRenderUtility/Shader/HLSL/TyflowVATKernelV1.hlsl';s=kernel.read_text(encoding='utf-8');old='TyflowVatGetMetaDataSize(config, TEXTURE2D_ARGS(vatTexture,samplerVAT), )';assert s.count(old)==4,s.count(old)
data=s.replace(old,'TyflowVatGetMetaDataSize(config, TEXTURE2D_ARGS(vatTexture,samplerVAT))')
graph=package/'NBShaders2/ShaderGraph/NBShaderGraph.shadergraph';g=GraphPreview(graph,'3f9fdcdd-c996-4c7b-b3ca-83775e72cb22');source=g.source_for_property('_FlipbookBlending');assert source is not None
bad=[e for e in g.g['m_Edges'] if e['m_OutputSlot'] is None];assert len(bad)==1
bad[0]['m_OutputSlot']=source;graph_text=g.serialize([])
for path,after in [(kernel,data),(graph,graph_text)]:
    rel=path.relative_to(package);before=path.read_bytes();save=backup/rel;save.parent.mkdir(parents=True,exist_ok=True);save.write_bytes(before)
    path.write_text(after,encoding='utf-8',newline='\n');records.append({'path':rel.as_posix(),'beforeSHA256':hashlib.sha256(before).hexdigest(),'afterSHA256':hashlib.sha256(path.read_bytes()).hexdigest()})
(work/'tyflow-import-error-repair.json').write_text(json.dumps({'scope':'Isolated correction before first valid GPU run; first run has zero test cases, not52 failed/passed; HLSL empty-arg punctuation and exact existing Flipbook input reference',
 'records':records,'noMathOrAssertionChange':True},indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps({'correctedFiles':len(records),'rootUnchanged':True}))
