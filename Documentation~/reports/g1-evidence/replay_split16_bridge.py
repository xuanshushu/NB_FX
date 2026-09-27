#!/usr/bin/env python3
"""Static IEEE-754 float32 capacity check for a proposed split16 Graph bridge; no Unity."""
import hashlib
import json
import pathlib
import struct
ROOT=next(p for p in pathlib.Path(__file__).resolve().parents if (p / "Packages/NB_FX/package.json").is_file())
HERE=pathlib.Path(__file__).resolve().parent
sg=ROOT/'Library/PackageCache/com.unity.shadergraph@2b401d56d4b3/Editor/Data/Graphs/Vector1ShaderProperty.cs'
source=sg.read_text()
assert 'class Vector1ShaderProperty : AbstractShaderProperty<float>' in source
assert 'public override PropertyType propertyType => PropertyType.Float;' in source
assert 'HLSLType._float' in source
assert 'FloatType.Integer:' in source

def f32(x): return struct.unpack('<f',struct.pack('<f',float(x)))[0]
for halfword in range(1<<16):
    assert f32(halfword)==halfword
vectors=[0,1,0x7fffffff,0x80000000,0x80000001,0xc0800001,0xf0000000,0xffffffff,0x00010000,0xffff0001]
result=[]
for value in vectors:
    lo=value&0xffff; hi=value>>16
    got=(int(f32(hi))<<16)|int(f32(lo))
    assert got==value
    result.append({'uintHex':f'0x{value:08X}','low16':lo,'high16':hi,'roundTrip':f'0x{got:08X}'})
out={'kind':'pure Python float32 model; not Unity/SG HLSL/GPU proof',
     'source':{'path':str(sg.relative_to(ROOT)),'sha256':hashlib.sha256(sg.read_bytes()).hexdigest(),
               'facts':['Vector1ShaderProperty<float>','PropertyType.Float','FloatType.Integer emits ShaderLab Int but HLSLType._float']},
     'all65536HalfwordIntegersExactInFloat32':True,'vectors':result,
     'requiredNextEvidence':['Generated Graph HLSL must declare both halves as full float, not half.','Unity Material/MPB.SetFloat with serialized Graph Material and Player VFX output must preserve the halves.','Graph Custom Function must reconstruct with uint(round(clamp(...,0,65535))) and never interpolate the halves.']}
path=HERE/'split16_bridge_static.json';path.write_text(json.dumps(out,indent=2)+'\n')
print(json.dumps({'output':str(path.relative_to(ROOT)),'halfwordValuesChecked':65536,'fullWordVectors':len(vectors)}))
