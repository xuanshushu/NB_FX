#!/usr/bin/env python3
"""Reproducible *candidate* classification; never treats a lexical miss as dead code.

Run from the project root: python3 Packages/NB_FX/Documentation~/reports/g1-evidence/classify_property_semantics.py
Only reads source/assets and writes JSON/Markdown alongside this script.
"""
from __future__ import annotations

import collections
import hashlib
import json
from pathlib import Path
import re

ROOT = next(p for p in Path(__file__).resolve().parents if (p / "Packages/NB_FX/package.json").is_file())
TEMP = Path(__file__).resolve().parent
PACKAGE = ROOT / "Packages/NB_FX"
SHADER = PACKAGE / "NBShaders2/Shader/NBShader.shader"
INPUT = PACKAGE / "NBShaders2/Shader/HLSL/NBShaderInput.hlsl"
INV = TEMP / "properties-static.json"
MATERIAL_ROOT = ROOT / "Assets/NBShaderSamples"

inventory = json.loads(INV.read_text())
props = inventory["properties"]
assert len(props) == 450 and len({p["name"] for p in props}) == 450
# Independently verify physical CBUFFER lines against the static inventory.
cbfields = {}
in_cbuffer = False
for physical_line, source_line in enumerate(INPUT.read_text().splitlines(), 1):
    if "CBUFFER_START(UnityPerMaterial)" in source_line:
        in_cbuffer = True
        continue
    if in_cbuffer and "CBUFFER_END" in source_line:
        break
    if not in_cbuffer:
        continue
    m = re.match(r"^\s*((?:half|float|int|uint)(?:[1-4](?:x[1-4])?)?)\s+(_[A-Za-z0-9_]+)\s*;", source_line)
    if m:
        cbfields[m.group(2)] = {"name": m.group(2), "line": physical_line, "type": m.group(1)}
assert set(cbfields) == {p["name"] for p in inventory["cbuffer_fields"]}
assert all(p["line"] == cbfields[p["name"]]["line"] for p in inventory["cbuffer_fields"])
no_direct = set(inventory["summary"]["property_no_cs_hlsl_shaderbody_ref"])
assert len(no_direct) == 31

def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()

def rel(path: Path) -> str:
    return str(path.relative_to(ROOT))

def token(name: str) -> re.Pattern[str]:
    return re.compile(r"(?<![A-Za-z0-9_])" + re.escape(name) + r"(?![A-Za-z0-9_])")

def strip_comments(lines: list[str]) -> list[str]:
    """Keep physical line numbers; remove // and block comments, not strings."""
    text = "\n".join(lines)
    text = re.sub(r"/\*.*?\*/", lambda m: "\n" * m.group().count("\n"), text, flags=re.S)
    return [re.sub(r"//.*$", "", line) for line in text.splitlines()]

hlsl_files = sorted((PACKAGE / "NBShaders2/Shader/HLSL").rglob("*.hlsl"))
# NBShaderForwardPass.hlsl directly includes VAT.hlsl and SixWaySmokeLit.hlsl
# from the shared utility package. Include all utility HLSL candidates rather
# than falsely marking its VAT uniforms declaration-only.
hlsl_files += sorted((PACKAGE / "XuanXuanRenderUtility/Shader/HLSL").rglob("*.hlsl"))
hlsl_lines = {path: strip_comments(path.read_text(errors="replace").splitlines()) for path in hlsl_files}
mat_files = sorted(MATERIAL_ROOT.rglob("*.mat"))
mat_lines = {path: path.read_text(errors="replace").splitlines() for path in mat_files}

def active_hlsl(name: str) -> list[dict]:
    pat = token(name)
    out = []
    for file, lines in hlsl_lines.items():
        for line_no, line in enumerate(lines, 1):
            if not pat.search(line):
                continue
            # A CBUFFER declaration is not an operational read. A macro alias
            # does show a binding, but can still be unreachable, so retain it.
            if re.match(r"^\s*(?:half|float|int|uint)(?:[1-4](?:x[1-4])?)?\s+" + re.escape(name) + r"\s*;", line):
                continue
            out.append({"file": rel(file), "line": line_no, "text": line.strip()[:190]})
    return out

def macro_st_uses(name: str) -> list[dict]:
    if not name.endswith("_ST"):
        return []
    texture = name[:-3]
    pat = re.compile(r"\bTRANSFORM_TEX\s*\([^\n]*,\s*" + re.escape(texture) + r"\s*\)")
    out = []
    for file, lines in hlsl_lines.items():
        for line_no, line in enumerate(lines, 1):
            if pat.search(line):
                out.append({"file": rel(file), "line": line_no, "text": line.strip()[:190], "mechanism": "TRANSFORM_TEX implicit _ST"})
    return out

def material_occurrences(name: str) -> list[dict]:
    pat = re.compile(r"^\s*- " + re.escape(name) + r":\s*(.*)$")
    out = []
    for file, lines in mat_lines.items():
        for line_no, line in enumerate(lines, 1):
            m = pat.match(line)
            if m:
                out.append({"file": rel(file), "line": line_no, "value": m.group(1)})
    return out

def section(line: int, name: str) -> str:
    if line < 79: return "inspector-foldout"
    if line < 91: return "host-mode"
    if line < 149: return "vat"
    if line < 178: return "feature-switches"
    if line < 215: return "base-color-uv"
    if line < 241: return "lighting"
    if line < 244: return "stencil"
    if line < 273: return "mask"
    if line < 294: return "twirl-polar-legacy"
    if line < 319: return "noise-distortion"
    if line < 335: return "emission"
    if line < 345: return "color-blend"
    if line < 364: return "color-ramp"
    if line < 396: return "dissolve"
    if line < 406: return "shared-uv-custom-data"
    if line < 418: return "particle-controls"
    if line < 442: return "render-state"
    if line < 453: return "fresnel-depth"
    if line < 467: return "vertex-offset"
    if line < 473: return "parallax"
    if line < 512: return "mixed-depth-legacy"
    if line < 518: return "z-state"
    if line < 540: return "protocol-and-inspector-state"
    return "inspector-range-metadata"

render_state = {"_SrcBlend", "_DstBlend", "_SrcBlendAlpha", "_DstBlendAlpha", "_ZWrite", "_ZTest", "_Cull", "_ColorMask", "_Stencil", "_StencilComp", "_StencilOp", "_StencilFail", "_StencilZFail", "_offsetFactor", "_offsetUnits"}
packed_name = re.compile(r"^(_W9Particle(?:Shader|CustomData)|_UVModeFlag|_NBShaderForceNoMipFlags|_NBShaderGUIFoldToggle)")
controls = {"_MeshSourceMode", "_TransparentMode", "_DistortMode", "_ScreenDistortModeToggle", "_DisableMainPassToggle", "_FxLightMode", "_VATMode", "_TimeMode", "_StencilKeyIndex", "_QueueBias"}

rows = []
for p in props:
    n = p["name"]
    hlsl = active_hlsl(n)
    mats = material_occurrences(n)
    typ = p["type"].lower()
    if n in no_direct:
        provenance = "serialized_property_no_direct_lexical_reference"
        graph = "compatibility_preserve_semantic_unresolved"
        confidence = "low"
    elif "FoldOut" in n or n.endswith("FoldToggle"):
        provenance = "inspector_state_candidate"
        graph = "editor_state_not_graph_port_candidate"
        confidence = "medium"
    elif packed_name.match(n):
        provenance = "packed_protocol_candidate"
        graph = "exact_integer_bridge_or_host_decoder_candidate"
        confidence = "medium"
    elif n in render_state:
        provenance = "shaderlab_render_state_candidate"
        graph = "target_pass_render_state_binding_candidate"
        confidence = "medium"
    elif n in controls:
        provenance = "host_intent_or_mode_control_candidate"
        graph = "graph_target_or_vfx_adapter_control_candidate"
        confidence = "medium"
    elif typ.startswith(("2d", "3d", "cube")):
        provenance = "texture_binding_candidate"
        graph = "graph_texture_or_shared_sampler_candidate"
        confidence = "medium" if hlsl else "low"
    elif hlsl:
        provenance = "active_hlsl_reference_candidate"
        graph = "shared_hlsl_uniform_or_graph_input_candidate"
        confidence = "medium"
    elif p["cbuffer"] and not hlsl:
        provenance = "cbuffer_declared_no_operational_lexical_site"
        graph = "preserve_pending_variant_and_usage_review"
        confidence = "low"
    elif p["cs_refs"] or p["shader_body_refs"]:
        provenance = "host_side_control_candidate"
        graph = "host_adapter_or_target_binding_candidate"
        confidence = "low"
    else:
        provenance = "unresolved"
        graph = "preserve_pending_review"
        confidence = "low"
    rows.append({
        "name": n, "shader_property": {k:p[k] for k in ("line", "label", "type", "default", "attributes")},
        "source_section_candidate": section(p["line"], n),
        "provenance_candidate": provenance,
        "urp_graph_vfx_destination_candidate": graph,
        "confidence": confidence,
        "cbuffer_field": [cbfields[n]] if n in cbfields else [],
        "active_hlsl_lexical_sites": hlsl,
        "macro_indirect_sites": macro_st_uses(n),
        "cs_lexical_sites": p["cs_refs"],
        "shader_body_lexical_sites": [{"file": rel(SHADER), "line": line} for line in p["shader_body_refs"]],
        "sample_serialization": {"count": len(mats), "distinct_values": sorted({m["value"] for m in mats}), "examples": mats[:3]},
        "automatic_limitations": [
            "Lexical occurrence does not prove runtime reachability, read/write direction, or pass coverage.",
            "Sample materials are not all user materials and defaults do not prove safe deletion.",
        ],
    })

# CBUFFER fields without exact ShaderLab Properties are an independent inventory.
cb_extra = []
for name in inventory["summary"]["cbuffer_not_in_properties"]:
    field = cbfields[name]
    hlsl = active_hlsl(name)
    macro = macro_st_uses(name)
    if name in {"_BaseMap_AnimationSheetBlend_ST", "_AnimationSheetHelperBlendIntensity"}:
        kind = "external_animation_sheet_helper"
        external = ["Packages/NB_FX/XuanXuanRenderUtility/Runtime/AnimationSheetHelper.cs:20-21", "Packages/NB_FX/XuanXuanRenderUtility/Runtime/AnimationSheetHelper.cs:273-281"]
    elif name in {"_CustomLocalTransformLocalToWorld", "_CustomLocalTransformWorldToLocal"}:
        kind = "external_local_transform_helper"
        external = ["Packages/NB_FX/XuanXuanRenderUtility/Runtime/NBParticleLocalTransformHelper.cs:14-18", "Packages/NB_FX/XuanXuanRenderUtility/Runtime/NBParticleLocalTransformHelper.cs:76-78"]
    elif name.endswith("_ST") and name[:-3] in {p["name"] for p in props}:
        kind = "texture_st_companion"
        external = []
    elif name == "_ClipRect":
        kind = "unresolved_possible_ui_external"
        external = ["Packages/NB_FX/NBShaders2/Shader/NBShader.shader:646", "Packages/NB_FX/NBShaders2/Shader/NBShader.shader:782", "Packages/NB_FX/NBShaders2/Shader/NBShader.shader:1272"]
    elif name == "_FresnelUnit2":
        kind = "unresolved_commented_property"
        external = ["Packages/NB_FX/NBShaders2/Shader/NBShader.shader:449"]
    else:
        kind = "unresolved"
        external = []
    cb_extra.append({"name": name, "field": {"file": rel(INPUT), **field}, "kind_candidate": kind,
                     "active_hlsl_lexical_sites": hlsl, "macro_indirect_sites": macro, "external_source_evidence": external,
                     "note": "No Property does not imply missing binding; confirm supplied value and active variant in Unity before freezing contract."})

summary = {
    "row_count": len(rows), "cbuffer_extra_count": len(cb_extra),
    "provenance_candidate_counts": dict(sorted(collections.Counter(row["provenance_candidate"] for row in rows).items())),
    "section_candidate_counts": dict(sorted(collections.Counter(row["source_section_candidate"] for row in rows).items())),
    "undecidable_without_manual_or_unity": [
        "31 serialized-only direct lexical misses: exact semantic aliases and user-material values unknown.",
        "_ClipRect UI provenance/read and _FresnelUnit2 active intent unknown.",
        "HLSL reachability by keyword/pass/stage, C# writer vs reader direction, MPB values, Graph VFX attribute frequency, and precision cannot be decided by lexical scan.",
        "URP Graph/SubTarget port exposure, generated property names, exact full-uint bridge, and shader variant behavior require functional tests.",
    ],
    "source_sha256": {rel(path):sha(path) for path in sorted({INV, SHADER, INPUT, Path(__file__), *hlsl_files})},
    "inventory_cbuffer_line_offset_detected": sorted({p["line"] - cbfields[p["name"]]["line"] for p in inventory["cbuffer_fields"]}),
    "material_corpus": {"root": rel(MATERIAL_ROOT), "file_count": len(mat_files),
                        "path_hash_digest_sha256": hashlib.sha256("\n".join(f"{rel(path)} {sha(path)}" for path in mat_files).encode()).hexdigest(),
                        "scope_warning": "Sample corpus only; not all production/user materials."},
}
out = {"summary": summary, "property_rows": rows, "cbuffer_without_properties": cb_extra,
       "classification_rules": [
           "No-direct-reference overrides all name-based interpretation and remains compatibility-preserve/unresolved.",
           "Foldout/packed/render-state/control/texture labels are CANDIDATES based on exact name, type, source section, and lexical evidence.",
           "Only physical-line, comment-stripped HLSL and TRANSFORM_TEX macro references are listed; no runtime execution claim.",
           "Candidate URP Graph/VFX destination is a planning bucket, not a confirmed Graph port or feature cut.",
       ]}
(TEMP / "property-semantics-candidates.json").write_text(json.dumps(out, ensure_ascii=False, indent=2) + "\n")
report = ["# T01 property semantic candidate inventory (draft, NOT G1 approval)", "",
          f"Rows: {len(rows)}; CBUFFER-without-Properties: {len(cb_extra)}; sample materials: {len(mat_files)}.",
          "", "## Rules", *["- " + s for s in out["classification_rules"]], "", "## Candidate counts"]
report += [f"- {k}: {v}" for k, v in summary["provenance_candidate_counts"].items()]
report += ["", "## 31 no-direct-reference Properties", "", "All are serialized compatibility properties, not deletion candidates. Search all user assets before any migration.", ""]
report += [f"- `{r['name']}` — `NBShader.shader:{r['shader_property']['line']}`; sample occurrences {r['sample_serialization']['count']}; candidate section {r['source_section_candidate']}." for r in rows if r["name"] in no_direct]
report += ["", "## 22 CBUFFER fields without Properties", ""]
report += [f"- `{r['name']}` — `NBShaderInput.hlsl:{r['field']['line']}`; {r['kind_candidate']}; operational lexical sites {len(r['active_hlsl_lexical_sites'])}, implicit macro sites {len(r['macro_indirect_sites'])}." for r in cb_extra]
report += ["", "## Still undecidable", *["- " + s for s in summary["undecidable_without_manual_or_unity"]], ""]
(TEMP / "property-semantics-candidates.md").write_text("\n".join(report))
print(json.dumps(summary["provenance_candidate_counts"], indent=2))
