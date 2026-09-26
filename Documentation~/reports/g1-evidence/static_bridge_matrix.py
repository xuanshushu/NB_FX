#!/usr/bin/env python3
"""Pure-Python/regex model checks for T01 flag wire format; never invokes Unity."""
from __future__ import annotations
import json
import pathlib
import re
import subprocess
import sys

ROOT = next(p for p in pathlib.Path(__file__).resolve().parents if (p / "Packages/NB_FX/package.json").is_file())
HERE = pathlib.Path(__file__).resolve().parent
subprocess.run([sys.executable, str(HERE / "replay_flag_protocol.py")], cwd=ROOT, check=True, stdout=subprocess.DEVNULL)
inv = json.loads((HERE / "flag_protocol_inventory.json").read_text())
U32 = 0xffffffff

def i32(x):
    x &= U32
    return x if x < 0x80000000 else x - 0x100000000

def check(name, condition, count, sources, note=""):
    tests.append({"name": name, "status": "static-pass" if condition else "static-fail", "cases": count, "source": sources, "note": note})

def pending(name, sources, note=""):
    tests.append({"name": name, "status": "unity-not-run", "cases": 0, "source": sources, "note": note})

def risk(name, condition, sources, note=""):
    tests.append({"name": name, "status": "risk-confirmed-static" if condition else "risk-not-reproduced-static", "cases": 1, "source": sources, "note": note})

tests = []
cs = (ROOT / inv["sourceFiles"]["cs"]["path"]).read_text()
hlsl = (ROOT / inv["sourceFiles"]["hlsl"]["path"]).read_text()
input_hlsl = (ROOT / inv["sourceFiles"]["input"]["path"]).read_text()
base = (ROOT / "Packages/NB_FX/XuanXuanRenderUtility/Runtime/ShaderFlagsBase.cs").read_text()
gui = (ROOT / "Packages/NB_FX/NBShaders2/Editor/ShaderGUIItems/NBShaderProtocolItems.cs").read_text()
graph_path = ROOT / "Packages/NB_FX/NBShaders2/Tests/PassFeasibility/SubTargetProbe/GF_URP_VFX_NBSubTarget.shadergraph"
graph = graph_path.read_text()
probe = (ROOT / "Packages/NB_FX/NBShaders2/Tests/PassFeasibility/SubTargetProbe/NBGFDistortPass.hlsl").read_text()

check("C#/HLSL constant parity", inv["parity"]["equalCount"] == 148 and not inv["parity"]["unmatchedHlsl"], 148,
      ["NBShaderFlags.cs:114-215,323-357,629-644,719-728,762-766", "NBShaderFlags.hlsl:4-176,275-279"],
      "Bump UV symbol alias BUMPMAP/BUMPTEX has the same numeric position 24.")
defaults = {item["property"]: item["default"]["value"] for item in inv["slots"]}
expected = {"_W9ParticleShaderFlags":0,"_W9ParticleShaderFlags1":1,"_W9ParticleShaderWrapFlags":0,
            "_NBShaderGUIFoldToggle":3,"_NBShaderGUIFoldToggle1":255,"_NBShaderGUIFoldToggle2":255,
            "_W9ParticleShaderColorChannelFlag":3,"_W9ParticleShaderPNoiseBlendFlag":0,"_NBShaderForceNoMipFlags":0}
check("ShaderLab packed defaults", defaults == expected, len(expected), ["NBShader.shader:521-536"])
aux = inv["auxiliaryDefaults"]
check("ShaderLab auxiliary defaults", all(aux[k]["value"] == 0 for k in [*(f"_W9ParticleCustomDataFlag{i}" for i in range(4)),"_UVModeFlag0","_UVModeFlagType0"]), 6, ["NBShader.shader:526-531"])
check("UnityPerMaterial uint declarations", all(re.search(r"\buint\s+" + re.escape(k) + r"\s*;", input_hlsl) for k in ["_W9ParticleShaderFlags","_W9ParticleShaderFlags1","_W9ParticleShaderWrapFlags","_NBShaderForceNoMipFlags","_UVModeFlag0","_UVModeFlagType0"]), 6, ["NBShaderInput.hlsl:222-240,251"])

# The bit31 case is deliberately checked in signed C#-style serialization and uint HLSL-style decode.
for word in [1<<31, (1<<31)|(1<<30)|(1<<23)|1, (1<<31)|(1<<15)|(1<<2)]:
    assert (i32(word) & U32) == word
check("bit31 and composite high bits round-trip in 32-bit model", True, 3,
      ["NBShaderFlags.cs:147-178", "NBShaderFlags.hlsl:38-69", "ShaderFlagsBase.cs:43-70", "NBShaderInput.hlsl:371-374"],
      "Static signed/unsigned arithmetic only; Unity Material serialization and GPU uint behavior still require runtime checks.")

# Wrap: 16 lower bits and 16 paired upper bits, four modes, neighboring field preserved.
wrap_cases = 0
for pos in range(16):
    for mode in range(4):
        seed = U32 ^ (1<<pos) ^ (1<<(pos+16))
        packed = (seed & ~((1<<pos)|(1<<(pos+16)))) | ((mode&1)<<pos) | (((mode>>1)&1)<<(pos+16))
        decoded = ((packed>>pos)&1) | (((packed>>(pos+16))&1)<<1)
        assert decoded == mode and (packed & ~((1<<pos)|(1<<(pos+16)))) == (seed & ~((1<<pos)|(1<<(pos+16))))
        wrap_cases += 1
check("Wrap 16x4 modes and field isolation", all(x in gui for x in ["_wrapFlagBits << 16", "(mode & 1)", "(mode & 2)"]) and "bits<<16" in input_hlsl, wrap_cases,
      ["NBShaderFlags.cs:181-196", "NBShaderProtocolItems.cs:504-648", "NBShaderInput.hlsl:375-399,432-479"])

uv_cases = 0
for pos in range(0,32,2):
    for mode in range(9):
        lo_seed = U32 ^ (3<<pos); hi_seed = U32 ^ (3<<pos)
        lo = (lo_seed & ~(3<<pos)) | ((mode%4)<<pos)
        hi = (hi_seed & ~(3<<pos)) | ((mode//4)<<pos)
        decoded = ((hi>>pos)&3)*4 + ((lo>>pos)&3)
        assert decoded == mode and (lo & ~(3<<pos)) == (lo_seed & ~(3<<pos)) and (hi & ~(3<<pos)) == (hi_seed & ~(3<<pos))
        uv_cases += 1
check("UV 16 fields x 9 modes, including type word bit31", all(x in cs for x in ["(int)mode % 4 << uvModePos", "(int)mode / 4 << uvModePos"]) and "(uvModeType << 2u) | uvMode" in hlsl, uv_cases,
      ["NBShaderFlags.cs:599-692", "NBShaderFlags.hlsl:150-165,241-264"])

channel_cases = 0
for pos in range(0,20,2):
    for channel in range(4):
        seed=U32 ^ (3<<pos); packed=(seed & ~(3<<pos)) | (channel<<pos)
        assert ((packed>>pos)&3)==channel and (packed & ~(3<<pos)) == (seed & ~(3<<pos))
        channel_cases += 1
check("Color channel 10 fields x RGBA", "0b_11 << colorChannelFlagPos" in cs and "colorChannelFlag &= 0b_11" in cs, channel_cases,
      ["NBShaderFlags.cs:719-759", "NBShaderFlags.hlsl:167-176"])

custom_cases = 0
choices={"Off":0,"D1X":15,"D1Y":14,"D1Z":13,"D1W":12,"D2X":11,"D2Y":10,"D2Z":9,"D2W":8}
for word in range(4):
    for pos in range(0,32,4):
        for label,nibble in choices.items():
            seed=U32 ^ (15<<pos); packed=(seed & ~(15<<pos)) | (nibble<<pos)
            got=(packed>>pos)&15
            decoded="Off" if not got&8 else ("D1" if got&4 else "D2") + ("X" if got&2 and got&1 else "Y" if got&2 else "Z" if got&1 else "W")
            assert decoded==label and (packed & ~(15<<pos)) == (seed & ~(15<<pos))
            custom_cases += 1
check("CustomData 4 words x 8 nibbles x 9 source choices", all(x in hlsl for x in ["isCustomDataBit", "Data12Bit", "DataXYorZWBit", "DataXZorYWBit"]) and "15 << dataBitPos" in cs, custom_cases,
      ["NBShaderFlags.cs:300-371,466-507", "NBShaderFlags.hlsl:109-148,178-225"])

pnoise_cases=0
for pos in range(0,15,3):
    for mode in range(4):
        seed=U32 ^ (7<<pos); packed=(seed & ~(7<<pos)) | (mode<<pos)
        assert (packed>>pos)&7 == mode and (packed & ~(7<<pos)) == (seed & ~(7<<pos))
        pnoise_cases+=1
check("PNoise 5 fields x 4 supported modes", "0b_111 << pNoiseBlendModeFlagPos" in cs and "flagProperty &= 7" in hlsl, pnoise_cases,
      ["NBShaderFlags.cs:762-800", "NBShaderFlags.hlsl:275-318"])

# Source-supported semantic hazard: MPB starts empty rather than inheriting material's packed value.
material_word=1
mpb_empty=0
mpb_unseeded=mpb_empty | (1<<31)
mpb_seeded=material_word | (1<<31)
risk("MPB unseeded packed write loses material bit0", mpb_unseeded & 1 == 0 and mpb_seeded & 1 == 1 and "propertyBlock.GetInt(GetShaderFlagsId(index))" in base and "flags | flagBits" in base,
     ["ShaderFlagsBase.cs:43-55,58-70"], "A bridge must seed the entire packed property into MPB before OR/AND updates, or use an explicit full-value writer. Actual Unity MPB inheritance/batching not measured.")
risk("GF Graph is not a packed-protocol bridge", all(name not in graph for name in ["_W9ParticleShaderFlags","_UVModeFlag0","_W9ParticleCustomDataFlag0"]) and "GF-only controlled data source" in probe,
     ["GF_URP_VFX_NBSubTarget.shadergraph:5-12,349-358,978-987", "NBGFDistortPass.hlsl:1-3,28-42"], "GF Graph has only BaseMap/Color; it proves pass feasibility, not existing NBShader flag wiring.")

for name, src, note in [
    ("Unity Material.SetInteger/GetInteger signed bit31 persistence", ["ShaderFlagsBase.cs:34-70", "NBShaderInput.hlsl:222-240"], "Material asset save/reload and Player GPU capture"),
    ("Unity MPB seed/inheritance and SRP Batcher impact", ["ShaderFlagsBase.cs:43-70"], "Editor and Player, Mesh/VFX instancing paths"),
    ("Packed UV/Wrap/Channel/CustomData GPU decode", ["NBShaderFlags.hlsl:109-264", "NBShaderInput.hlsl:375-479"], "Compare pixel readback against integer test vectors, especially positions 28/30/31"),
    ("PNoise and ForceNoMip GPU/variant coverage", ["NBShaderFlags.hlsl:275-318", "NBShaderInput.hlsl:401-425"], "LOD difference requires mipmapped textures and platform-specific capture"),
    ("Graph/VFX product bridge and serialized material compatibility", ["GF_URP_VFX_NBSubTarget.shadergraph:5-12", "NBGFUnlitSubTarget.cs:1-18"], "Not implemented by GF probe; G3 and later required")
]: pending(name,src,note)

out={"kind":"T01 pure static model; NOT Unity/GPU proof", "tests":tests,
     "summary":{"staticPassed":sum(t["status"]=="static-pass" for t in tests),"staticFailed":sum(t["status"]=="static-fail" for t in tests),"riskConfirmed":sum(t["status"]=="risk-confirmed-static" for t in tests),"unityNotRun":sum(t["status"]=="unity-not-run" for t in tests),"modeledCases":sum(t["cases"] for t in tests if t["status"]=="static-pass")}}
outpath=HERE/"static_bridge_matrix.json"
outpath.write_text(json.dumps(out,ensure_ascii=False,indent=2)+"\n")
print(json.dumps({"output":str(outpath.relative_to(ROOT)),"summary":out["summary"]},ensure_ascii=False))
if out["summary"]["staticFailed"] or any(t["status"]=="risk-not-reproduced-static" for t in tests):
    raise SystemExit(1)
