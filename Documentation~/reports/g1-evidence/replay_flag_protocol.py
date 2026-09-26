#!/usr/bin/env python3
"""Read-only NBShaders2 packed-flag protocol inventory. Run from project root."""
from __future__ import annotations
import hashlib
import json
import pathlib
import re
from collections import defaultdict

ROOT = next(p for p in pathlib.Path(__file__).resolve().parents if (p / "Packages/NB_FX/package.json").is_file())
FILES = {
    "cs": ROOT / "Packages/NB_FX/NBShaders2/Runtime/NBShaderFlags.cs",
    "hlsl": ROOT / "Packages/NB_FX/NBShaders2/Shader/HLSL/NBShaderFlags.hlsl",
    "shader": ROOT / "Packages/NB_FX/NBShaders2/Shader/NBShader.shader",
    "input": ROOT / "Packages/NB_FX/NBShaders2/Shader/HLSL/NBShaderInput.hlsl",
}


def value_of(expression: str) -> int:
    expression = expression.replace("(", "").replace(")", "").replace(" ", "")
    if not re.fullmatch(r"[0-9*+<|&-]+", expression):
        raise ValueError(expression)
    return int(eval(expression, {"__builtins__": {}}, {}))


def extract_cs(source: str, prefix: str) -> dict[str, dict]:
    result = {}
    for n, line in enumerate(source.splitlines(), 1):
        match = re.match(r"\s*public const int (\w+)\s*=\s*([^;]+);", line)
        if match and match.group(1).startswith(prefix):
            name, expr = match.group(1), match.group(2).strip()
            result[name] = {"name": name, "expression": expr, "value": value_of(expr), "line": n}
    return result


def extract_hlsl(source: str, prefix: str) -> dict[str, dict]:
    result = {}
    for n, line in enumerate(source.splitlines(), 1):
        match = re.match(r"[ \t]*#define[ \t]+(\w+)[ \t]+([^\r\n]+)", line)
        if match and match.group(1).startswith(prefix):
            name, expr = match.group(1), match.group(2).split("//", 1)[0].strip()
            result[name] = {"name": name, "expression": expr, "value": value_of(expr), "line": n}
    return result


def main() -> None:
    sources = {name: path.read_text(encoding="utf-8-sig") for name, path in FILES.items()}
    cs = extract_cs(sources["cs"], ("FLAG_", "FLAGBIT_"))
    hlsl = extract_hlsl(sources["hlsl"], ("FLAG_", "FLAGBIT_"))
    alias = {"FLAG_BIT_UVMODE_POS_0_BUMPMAP": "FLAG_BIT_UVMODE_POS_0_BUMPTEX"}
    pairs = []
    for cs_name, c in sorted(cs.items()):
        hlsl_name = alias.get(cs_name, cs_name)
        h = hlsl.get(hlsl_name)
        pairs.append({
            "csName": cs_name, "hlslName": hlsl_name if h else None,
            "csValue": c["value"], "hlslValue": h["value"] if h else None,
            "match": h is not None and c["value"] == h["value"],
            "csLine": c["line"], "hlslLine": h["line"] if h else None,
        })
    mapped_hlsl = {p["hlslName"] for p in pairs if p["hlslName"]}
    hlsl_unmatched = sorted(set(hlsl) - mapped_hlsl)
    shader_defaults = {}
    for n, line in enumerate(sources["shader"].splitlines(), 1):
        match = re.search(r"\[HideInInspector\]\s+(\w+)\([^)]*,\s*Integer\)\s*=\s*(-?\d+)", line)
        if match:
            shader_defaults[match.group(1)] = {"value": int(match.group(2)), "line": n}
    names = [
        "_W9ParticleShaderFlags", "_W9ParticleShaderFlags1", "_W9ParticleShaderWrapFlags",
        "_NBShaderGUIFoldToggle", "_NBShaderGUIFoldToggle1", "_NBShaderGUIFoldToggle2",
        "_W9ParticleShaderColorChannelFlag", "_W9ParticleShaderPNoiseBlendFlag", "_NBShaderForceNoMipFlags",
    ]
    slots = [{"index": i, "property": p, "default": shader_defaults.get(p)} for i, p in enumerate(names)]
    prefixes = {"customData": "FLAGBIT_POS_", "uv": "FLAG_BIT_UVMODE_POS_", "channel": "FLAG_BIT_COLOR_CHANNEL_POS_", "pnoise": "FLAG_BIT_PNOISE_BLEND_POS_", "wrap": "FLAG_BIT_WRAPMODE_", "forceNoMip": "FLAG_BIT_FORCE_NO_MIP_"}
    fields = {kind: [p for p in pairs if p["csName"].startswith(prefix)] for kind, prefix in prefixes.items()}
    foldouts = extract_cs(sources["cs"], "foldOut")
    mats = defaultdict(list)
    for path in sorted((ROOT / "Assets/NBShaderSamples").rglob("*.mat")):
        source = path.read_text(errors="ignore")
        if "_W9ParticleShaderFlags" not in source:
            continue
        for property_name in [*names[:3], names[6], names[7], names[8], *(f"_W9ParticleCustomDataFlag{i}" for i in range(4)), "_UVModeFlag0", "_UVModeFlagType0"]:
            match = re.search(r"^\s*- " + re.escape(property_name) + r":\s*(-?\d+)", source, re.M)
            if match:
                mats[property_name].append(int(match.group(1)))
    sample_counts = {name: {"present": len(values), "nonzero": sum(v != 0 for v in values), "distinct": sorted(set(values))} for name, values in sorted(mats.items())}
    result = {
        "sourceFiles": {name: {"path": str(path.relative_to(ROOT)), "sha256": hashlib.sha256(path.read_bytes()).hexdigest()} for name, path in FILES.items()},
        "parity": {"csCount": len(cs), "hlslCount": len(hlsl), "pairedCount": len(pairs), "equalCount": sum(p["match"] for p in pairs), "unmatchedHlsl": hlsl_unmatched, "nameAliases": alias},
        "slots": slots,
        "auxiliaryDefaults": {name: value for name, value in shader_defaults.items() if name in {*(f"_W9ParticleCustomDataFlag{i}" for i in range(4)), "_UVModeFlag0", "_UVModeFlagType0", "_W9ParticleShaderWrapFlags2"}},
        "fields": fields,
        "foldoutConstants": foldouts,
        "constantPairs": pairs,
        "sampleMaterials": {"scannedRoot": "Assets/NBShaderSamples", "materialCountWithFlags": len(mats.get("_W9ParticleShaderFlags", [])), "propertyCounts": sample_counts},
    }
    out = pathlib.Path(__file__).with_name("flag_protocol_inventory.json")
    out.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n")
    print(json.dumps({"output": str(out.relative_to(ROOT)), "parity": result["parity"], "materialCount": result["sampleMaterials"]["materialCountWithFlags"]}, ensure_ascii=False))


if __name__ == "__main__":
    main()
