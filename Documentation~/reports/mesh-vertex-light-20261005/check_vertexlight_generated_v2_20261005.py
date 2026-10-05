"""Inspect actual generated source saved by read-generated.cs; never runs Unity."""
from pathlib import Path
import argparse,hashlib,json,re

def args(text):
    out=[];start=0;depth=0
    for i,c in enumerate(text):
        if c in '([':depth+=1
        elif c in ')]':depth-=1
        elif c==',' and depth==0:out.append(text[start:i].strip());start=i+1
    out.append(text[start:].strip());return out

def bodies(text,pattern):
    out=[]
    for m in re.finditer(pattern,text):
        start=text.index('{',m.start());i=start+1;depth=1
        while depth:
            assert i<len(text),'Unterminated generated body'
            if text[i]=='{':depth+=1
            elif text[i]=='}':depth-=1
            i+=1
        out.append((m.start(),text[start+1:i-1]))
    return out

def inspect(path):
    data=path.read_bytes();text=data.decode('utf-8-sig');packed=[];production=[]
    for offset,body in bodies(text,r'\bstruct\s+PackedVaryings\s*\{'):
        coords=[int(x) for x in re.findall(r':\s*(?:TEXCOORD|INTERP)(\d+)\b',body)]
        assert coords,'No TEXCOORD/INTERP fields in actual generated PackedVaryings'
        packed.append(dict(line=text[:offset].count('\n')+1,interpolatorIndices=sorted(set(coords)),registerHighWater=max(coords)+1,withinD3D11SM45RegisterLimit=max(coords)<32,body=body.strip()))
    for offset,body in bodies(text,r'\bVertexDescription\s+VertexDescriptionFunction\s*\([^)]*\)\s*\{'):
        calls=list(re.finditer(r'\bNBGraphVertexLighting_(float|half)\(([^;]+)\);',body))
        for call in calls:
            aa=args(call.group(2));assert len(aa)==9
            vo=re.search(r'\bNBGraphVertexOffset_(?:float|half)\(([^;]+)\);',body);assert vo,'Missing VO call in vertex graph'
            va=args(vo.group(1));assert aa[0]==va[0],'VertexLighting does not use exact pre-VO PositionOS input'
            assert 'NBGraphVATSoftBody' in aa[0] and 'OutPositionOS' in aa[0],aa[0]
            assert 'NBGraphVATWorldBasis' in aa[1] and 'NormalWS' in aa[1],aa[1]
            assert re.search(r'description\.NBVertexLighting\s*=\s*'+re.escape(aa[-1])+r'\s*;',body),'VertexLight result not assigned to custom block'
            production.append(dict(line=text[:offset].count('\n')+1,precision=call.group(1),positionArgument=aa[0],normalArgument=aa[1],outputArgument=aa[-1],samePreVOArgument=True))
    assert packed and production,'Missing packed varyings or real vertex producer'
    assert all(r['withinD3D11SM45RegisterLimit'] for r in packed),'Generated interpolator register limit exceeded; stop before tests'
    assert 'NBVertexLighting' in text and re.search(r'IN\.NBVertexLighting',text),'Missing fragment interpolator read'
    return dict(path=str(path.resolve()),sha256=hashlib.sha256(data).hexdigest(),packedVaryings=packed,vertexProducers=production,
        sourceRegisterCheckOnly=True,actualGPUVariantCompileMustAlsoPass=True)

p=argparse.ArgumentParser();p.add_argument('--directory',required=True);a=p.parse_args();d=Path(a.directory).resolve()
expected=['NBShaderGraph.shader','NBBackFirstLegacy.shader','NBBackFirstModern.shader'];assert all((d/x).is_file() for x in expected)
result=dict(scope='Actual generated HLSL; conditional fields retained, not GPU disassembly',graphs=[inspect(d/x) for x in expected])
out=d/'varyings-and-pre-vo.json';assert not out.exists();out.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps(dict(receipt=str(out),graphs=len(result['graphs']),maxGeneratedRegisterHighWater=max(r['registerHighWater'] for g in result['graphs'] for r in g['packedVaryings']))))
