from pathlib import Path
import json,copy,hashlib,re
work=Path(__file__).resolve().parent;root=work.parents[1]
package=root/'.utmp/NBFXMeshValidation-20261002/Packages/NB_FX'
def decode(s):
    out=[];d=json.JSONDecoder();pos=0
    while pos<len(s):
        while pos<len(s) and s[pos].isspace():pos+=1
        if pos==len(s):break
        value,pos=d.raw_decode(s,pos);out.append(value)
    return out
graph=package/'NBShaders2/ShaderGraph/NBShaderGraph.shadergraph';raw=graph.read_bytes();objects=decode(raw.decode('utf-8'));original=copy.deepcopy(objects)
source_root=root/'Packages/NB_FX/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph'
root_ids={o.get('m_ObjectId') for o in decode(source_root.read_text(encoding='utf-8'))}
changes=[]
for o in objects:
    # All new real render inputs must own a VFX expression slot. The existing
    # unused hidden GUI state remains untouched; same GeneratePropertyBlock.
    if o['m_ObjectId'] not in root_ids and o.get('m_GeneratePropertyBlock') and o.get('m_Hidden'):
        changes.append({'name':o.get('m_OverrideReferenceName'),'id':o['m_ObjectId'],'beforeHidden':True,'afterHidden':False})
        o['m_Hidden']=False
assert changes
for before,after in zip(original,objects):
    a=copy.deepcopy(before);b=copy.deepcopy(after);a.pop('m_Hidden',None);b.pop('m_Hidden',None);assert a==b
output=work/'input-visibility-preview';assert not output.exists()
target=output/'NBShaders2/ShaderGraph/NBShaderGraph.shadergraph';target.parent.mkdir(parents=True,exist_ok=True)
target.write_text('\n\n'.join(json.dumps(o,indent=4,ensure_ascii=False) for o in objects)+'\n',encoding='utf-8',newline='\n')
gui=package/'NBShaders2/Editor/NBShaderGraphRootItem.cs';gui_raw=gui.read_bytes();text=gui_raw.decode('utf-8')
old='            => property != null && (property.propertyFlags &'
new='''            // Effective inputs belong to the projector, not native user editing.
            // They remain real exposed SG inputs for the VFX expression ABI.
            => property != null && !property.name.StartsWith("_NB_TierAllow", StringComparison.Ordinal) && (property.propertyFlags &'''
assert text.count(old)==1;text=text.replace(old,new)
target_gui=output/'NBShaders2/Editor/NBShaderGraphRootItem.cs';target_gui.parent.mkdir(parents=True,exist_ok=True);target_gui.write_text(text,encoding='utf-8',newline='\n')
(output/'manifest.json').write_text(json.dumps({'scope':'Preview only: fix hidden+exposed VFX expression ABI for new rendering inputs; material value/type, bindings, nodes, slots, HLSL math and original GUI hidden state unchanged',
 'graphBeforeSHA256':hashlib.sha256(raw).hexdigest(),'graphAfterSHA256':hashlib.sha256(target.read_bytes()).hexdigest(),
 'guiBeforeSHA256':hashlib.sha256(gui_raw).hexdigest(),'guiAfterSHA256':hashlib.sha256(target_gui.read_bytes()).hexdigest(),
 'properties':changes,'newKeywords':0,'newPackedBits':0,'formalVFXStarted':False},indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps({'newRenderInputsMadeSlotVisible':len(changes),'names':[c['name'] for c in changes],'previewOnly':True},indent=2))
