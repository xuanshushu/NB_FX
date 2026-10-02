"""Append only two editor Float properties to the fresh current Graph.
Main Agent executes; no render Offset properties are invented by this script.
"""
from pathlib import Path
import argparse, copy, hashlib, json, uuid

def main():
    ap=argparse.ArgumentParser();ap.add_argument('--source-package',required=True);ap.add_argument('--output',required=True);args=ap.parse_args()
    package=Path(args.source_package).resolve();out=Path(args.output).resolve();owner=Path(__file__).resolve().parent
    assert owner in out.parents and package not in out.parents and not out.exists()
    target=(package/'NBShaders2/ShaderGraph/Editor/NBGraphUnlitSubTarget.cs').read_text()
    assert 'collector.AddFloatProperty("_offsetFactor", 0.0f);' in target and 'collector.AddFloatProperty("_offsetUnits", 0.0f);' in target, 'Integrate real Offset Target first; do not fabricate render fields'
    rel='NBShaders2/ShaderGraph/NBShaderGraph.shadergraph';raw=(package/rel).read_bytes();s=raw.decode('utf-8-sig');decoder=json.JSONDecoder();objs=[];i=0
    while i<len(s):
        while i<len(s) and s[i].isspace():i+=1
        if i==len(s):break
        obj,i=decoder.raw_decode(s,i);objs.append(obj)
    old=copy.deepcopy(objs);root=objs[0];by={x.get('m_ObjectId'):x for x in objs};props=[by[x['m_Id']] for x in root['m_Properties']]
    name=lambda p:p.get('m_OverrideReferenceName') or p.get('m_DefaultReferenceName')
    template=next(p for p in props if name(p)=='_BaseMapFoldOut')
    for ref in ['_ZOffsetBlockFoldOut','_ZOffset_Toggle']:
        assert not any(name(p)==ref for p in props),'Editor property collision, review latest schema'
        p=copy.deepcopy(template);p.update(m_ObjectId=uuid.uuid4().hex,m_Guid={'m_GuidSerialized':str(uuid.uuid4())},m_Name=ref,m_DefaultReferenceName=ref,m_OverrideReferenceName=ref,m_Hidden=True,m_Value=0.0,m_FloatType=0)
        objs.append(p);root['m_Properties'].append({'m_Id':p['m_ObjectId']})
    assert objs[1:len(old)]==old[1:]
    a=copy.deepcopy(root);b=copy.deepcopy(old[0]);a.pop('m_Properties');b.pop('m_Properties');assert a==b
    assert root['m_Properties'][:len(old[0]['m_Properties'])]==old[0]['m_Properties']
    assert (package/rel).read_bytes()==raw
    p=out/rel;p.parent.mkdir(parents=True);p.write_text('\n\n'.join(json.dumps(o,indent=4,ensure_ascii=False) for o in objs)+'\n',encoding='utf-8')
    records=[{'path':rel,'beforeSHA256':hashlib.sha256(raw).hexdigest(),'afterSHA256':hashlib.sha256(p.read_bytes()).hexdigest()}]
    (out/'manifest.json').write_text(json.dumps({'scope':'Editor-only Float foldout/toggle append; no nodes/edges/newbits or fake renderstate fields','records':records,'files':records,'oldObjectsPreserved':True,'UnityRun':False},indent=2)+'\n')

if __name__=='__main__':main()
