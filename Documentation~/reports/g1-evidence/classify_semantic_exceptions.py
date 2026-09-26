#!/usr/bin/env python3
"""T01 manually reviewed classes for lexical inventory exceptions; no deletion claims."""
import json
import re
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = next(path for path in HERE.parents if (path / "Packages/NB_FX/package.json").is_file())
inventory = json.loads((HERE / "properties-static.json").read_text(encoding="utf-8"))
by_name = {item["name"]: item for item in inventory["properties"]}
sample_materials = sorted((ROOT / "Assets/NBShaderSamples").rglob("*.mat"))

serialized_only = []
for name in inventory["summary"]["property_no_cs_hlsl_shaderbody_ref"]:
    item = by_name[name]
    count = sum(bool(re.search(r"^\s*-\s+" + re.escape(name) + r":", path.read_text(errors="ignore"), re.M)) for path in sample_materials)
    serialized_only.append({"name": name, "shaderLine": item["line"], "sampleMaterialOccurrences": count, "status": "serialized-compatibility; semantic use unresolved; do not delete"})

external_helper = {"_BaseMap_AnimationSheetBlend_ST", "_AnimationSheetHelperBlendIntensity"}
dynamic_matrix = {"_CustomLocalTransformLocalToWorld", "_CustomLocalTransformWorldToLocal"}
unresolved = {"_ClipRect", "_FresnelUnit2"}
cbuffer_only = []
for name in inventory["summary"]["cbuffer_not_in_properties"]:
    if name in external_helper:
        category = "runtime-helper-uniform"
    elif name in dynamic_matrix:
        category = "runtime-written-matrix"
    elif name in unresolved:
        category = "active-use-unresolved"
    else:
        assert name.endswith("_ST") and name[:-3] in by_name
        category = "texture-transform-uniform"
    cbuffer_only.append({"name": name, "category": category})

result = {
    "serializedOnlyPropertyCount": len(serialized_only),
    "sampleMaterialFilesScanned": len(sample_materials),
    "serializedOnlyProperties": serialized_only,
    "cbufferOnlyCount": len(cbuffer_only),
    "cbufferOnlyFields": cbuffer_only,
    "note": "Categories reflect read-only source review; unresolved means not proven unused. Sample material occurrences do not cover user materials.",
}
assert len(serialized_only) == 31 and len(cbuffer_only) == 22
(HERE / "semantic-exceptions.json").write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"serializedOnlyPropertyCount": len(serialized_only), "cbufferOnlyCount": len(cbuffer_only)}, ensure_ascii=False))
