"""Four NEW UVP matrix visibility fields only; own preview, no live writes."""
import copy,hashlib,json
from pathlib import Path
HERE=Path(__file__).resolve().parent
WORKSPACE=HERE.parents[2]
REL='NBShaders2/ShaderGraph/NBShaderGraph.shadergraph'
SOURCE=WORKSPACE/'.utmp/NBFXMeshValidation-20261002/Packages/NB_FX'/REL
EXPECTED='eac049e1c37d7ef9b7fe5cc68d1c1d14aaccf8932e6b93c61e3cc080f8e3e423'
def sha(data):return hashlib.sha256(data).hexdigest()
raw=SOURCE.read_bytes();assert sha(raw)==EXPECTED,'Temporary UVP input changed'
text=raw.decode('utf-8');dec=json.JSONDecoder();index=0;objects=[];spans=[]
while index<len(text):
    while index<len(text) and text[index].isspace():index+=1
    if index==len(text):break
    start=index;obj,index=dec.raw_decode(text,index);objects.append(obj);spans.append((start,index))
assert len(objects)==1132 and len(objects[0]['m_Edges'])==331
targets={f'_CylinderMatrix{i}' for i in range(4)}
records=[];chunks=[]
for obj,(start,end) in zip(objects,spans):
    name=obj.get('m_OverrideReferenceName')
    if name not in targets:continue
    assert obj['m_GeneratePropertyBlock'] is True and obj['m_Hidden'] is True
    fragment=text[start:end];assert fragment.count('"m_Hidden": true')==1
    chunks.append((start,end,fragment.replace('"m_Hidden": true','"m_Hidden": false')))
    records.append(dict(name=name,id=obj['m_ObjectId'],beforeHidden=True,afterHidden=False))
assert len(records)==4
for start,end,fragment in reversed(chunks):text=text[:start]+fragment+text[end:]
after=[];index=0
while index<len(text):
    while index<len(text) and text[index].isspace():index+=1
    if index==len(text):break
    obj,index=dec.raw_decode(text,index);after.append(obj)
for old,new in zip(objects,after):
    expected=copy.deepcopy(old)
    if old.get('m_OverrideReferenceName') in targets:expected['m_Hidden']=False
    assert expected==new,old['m_ObjectId']
root_text=(WORKSPACE/'Packages/NB_FX'/REL).read_text(encoding='utf-8');root=[];index=0
while index<len(root_text):
    while index<len(root_text) and root_text[index].isspace():index+=1
    if index==len(root_text):break
    obj,index=dec.raw_decode(root_text,index);root.append(obj)
by={obj['m_ObjectId']:obj for obj in after}
hidden_root=[o for o in root if o.get('m_Type','').endswith('ShaderProperty') and o.get('m_Hidden')]
assert len(hidden_root)==32
gui_hidden=[o for o in hidden_root if o.get('m_OverrideReferenceName') not in
            ('_MainTexBigBlockItemFoldOut','_BaseMapFoldOut','_NB_GraphGUIStateVersion')]
assert len(gui_hidden)==29
assert all(by[o['m_ObjectId']]==o for o in hidden_root)
dest=HERE/REL;dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(text.encode('utf-8'))
manifest=dict(scope='New UVP matrix hidden/exposed ABI repair only. Root GUI29 hidden objects exact; current live/isolation/Root untouched.',
 records=[dict(path=REL,beforeSHA256=EXPECTED,afterSHA256=sha(dest.read_bytes()))],properties=records,
 originalRootHiddenProperties=32,originalGUIHiddenMirrorProperties=29,rootHiddenObjectsExact=True,oldObjectsEdgesSlotsDefaultsTypesGUIDsExactExceptFourHidden=True,
 objectsBefore=1132,objectsAfter=len(after),edgesBefore=331,edgesAfter=len(after[0]['m_Edges']),
 allEightSpatialInputsExposedAndVisible=True,GUIChanged=False,HLSLChanged=False,newKeywords=0,newPackedBits=0,ranUnity=False,installed=False,
 sourceRule='Official VFXShaderGraphHelpers.cs:116 excludes hidden slots;318-323 exposed stage-live properties still require slot expression. Four IDs match prior input-visibility32 repair.',
 nativeGUIBoundary='GraphRoot has no matrix-specific suppression/row adapter; prior verified32 repair also used existing native input editing for matrices. Keep this interim UI; complete matrix derivation/Tier GUI remains separate.',formalVFXStarted=False)
(HERE/'manifest.json').write_text(json.dumps(manifest,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
(HERE/'precise-patch.json').write_text(json.dumps(dict(inputSHA=EXPECTED,operations=[dict(objectId=r['id'],field='m_Hidden',before=True,after=False) for r in records]),indent=2)+'\n',encoding='utf-8')
print(json.dumps(dict(afterSHA=manifest['records'][0]['afterSHA256'],properties=records,rootGUI29Exact=True)))
