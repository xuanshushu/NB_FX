from pathlib import Path
import json,hashlib
work=Path(__file__).resolve().parent;root=work.parents[1];package=root/'.utmp/NBFXMeshValidation-20261002/Packages/NB_FX'
preview=work/'input-visibility-preview';manifest=json.loads((preview/'manifest.json').read_text(encoding='utf-8'))
for rel,key in [('NBShaders2/ShaderGraph/NBShaderGraph.shadergraph','graph'),('NBShaders2/Editor/NBShaderGraphRootItem.cs','gui')]:
    path=package/rel;assert hashlib.sha256(path.read_bytes()).hexdigest()==manifest[key+'BeforeSHA256']
    data=(preview/rel).read_bytes();assert hashlib.sha256(data).hexdigest()==manifest[key+'AfterSHA256'];path.write_bytes(data)
sources=[]
for name in ('NBGraphVFXMeshMinimum.vfx','NBGraphVFXMeshMinimum.vfx.meta'):
    rel=Path('NBShaders2/ShaderGraph/Samples')/name;source=root/'Packages/NB_FX'/rel;target=package/rel
    assert not target.exists();target.write_bytes(source.read_bytes());sources.append({'path':rel.as_posix(),'originalRootSHA256':hashlib.sha256(source.read_bytes()).hexdigest()})
(work/'input-visibility-installation.json').write_text(json.dumps({'scope':'Owned isolation only; original sample restored to check automatic importer compatibility, not formal VFX verification','sources':sources,'rootSourceUnchanged':True,'formalVFXStarted':False},indent=2)+'\n',encoding='utf-8',newline='\n')
print('Visibility candidate installed and original sample restored only in isolation')
