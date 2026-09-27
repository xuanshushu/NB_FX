#!/usr/bin/env python3
"""T01 feature→property→pass→case *candidate* trace (no Unity execution).

Run: python3 Packages/NB_FX/Documentation~/reports/g1-evidence/build_feature_trace_matrix.py
Inputs and outputs are alongside this script; source is read-only.
"""
from __future__ import annotations

from collections import Counter, defaultdict
import hashlib
import json
from pathlib import Path
import re

ROOT = next(p for p in Path(__file__).resolve().parents if (p / "Packages/NB_FX/package.json").is_file())
TEMP = Path(__file__).resolve().parent
PROPS = TEMP / "property-semantics-candidates.json"
KEYWORDS = TEMP / "keywords-static.json"
SHADER = ROOT / "Packages/NB_FX/NBShaders2/Shader/NBShader.shader"
OUT = TEMP / "feature-property-pass-case-candidates.json"
REPORT = TEMP / "feature-property-pass-case-candidates.md"

def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()

properties = json.loads(PROPS.read_text())
keywords = json.loads(KEYWORDS.read_text())
passes = sorted(keywords["passes"], key=lambda p: p["line"])
assert len(properties["property_rows"]) == 450 and len(passes) == 7
pass_by_name = {p["name"]: p for p in passes}
pass_names = [p["name"] for p in passes]
shader_lines = SHADER.read_text().splitlines()
subshader_line = next(i for i, line in enumerate(shader_lines, 1) if line.strip() == "SubShader")
keyword_bindings = defaultdict(list)
for binding in keywords["intent_bindings"]:
    keyword_bindings[binding["property"]].append({"keyword": binding["keyword"], "file": "Packages/NB_FX/NBShaders2/Runtime/NBShaderMaterialIntentResolver.cs", "line": binding["line"], "evidence_kind": "intent_binding_inventory_candidate"})

# Explicit section-to-keyword grouping is a REVIEW HINT only. It is never an
# exact per-property binding and never proves a pass executes the feature.
FEATURE_KEYWORDS = {
    "vat": ["_VAT", "_VAT_HOUDINI", "_VAT_TYFLOW", "_HOUDINI_VAT_SOFTBODY", "_HOUDINI_VAT_RIGIDBODY", "_TYFLOW_VAT_ABSOLUTE", "_TYFLOW_VAT_RELATIVE", "_FLIPBOOKBLENDING_ON"],
    "lighting": ["_FX_LIGHT_MODE_UNLIT", "_FX_LIGHT_MODE_BLINN_PHONG", "_FX_LIGHT_MODE_HALF_LAMBERT", "_FX_LIGHT_MODE_PBR", "_FX_LIGHT_MODE_SIX_WAY", "_NORMALMAP", "_MATCAP", "_SPECULAR_COLOR", "VFX_SIX_WAY_ABSORPTION"],
    "mask": ["_MASKMAP_ON", "_MASKMAP2_ON", "_MASKMAP3_ON"],
    "noise-distortion": ["_NOISEMAP", "_NOISE_MASKMAP", "_DISTORT_REFRACTION", "_CHROMATIC_ABERRATION"],
    "emission": ["_EMISSION"],
    "color-blend": ["_COLORMAPBLEND"],
    "color-ramp": ["_COLOR_RAMP", "_COLOR_RAMP_MAP"],
    "dissolve": ["_DISSOLVE", "_DISSOLVE_MASK", "_DISSOLVE_RAMP", "_DISSOLVE_RAMP_MAP", "_PROGRAM_NOISE", "_PROGRAM_NOISE_SIMPLE", "_PROGRAM_NOISE_VORONOI"],
    "shared-uv-custom-data": ["_SHARED_UV"],
    "particle-controls": ["_SOFTPARTICLES_ON", "_FLIPBOOKBLENDING_ON"],
    "fresnel-depth": ["_FRESNEL", "_DEPTH_OUTLINE", "_DEPTH_DECAL"],
    "vertex-offset": ["_VERTEX_OFFSET", "_VERTEX_OFFSET_MASKMAP", "_CUSTOM_LOCAL_TRANSFORM"],
    "parallax": ["_PARALLAX_MAPPING"],
    "z-state": ["_OVERRIDE_Z"],
    "stencil": ["_STENCIL_WITHOUT_PLAYER"],
}

CASE_GROUP = {
    "inspector-foldout": "material-gui-serialization",
    "host-mode": "host-mode-and-pass-state",
    "vat": "geometry-vat-modes",
    "feature-switches": "keyword-and-packed-flag-sync",
    "base-color-uv": "base-uv-sampling-and-color",
    "lighting": "lighting-modes-and-shadow",
    "stencil": "stencil-and-portal",
    "mask": "mask-and-gradient",
    "twirl-polar-legacy": "twirl-polar-shared-uv",
    "noise-distortion": "noise-camera-and-deferred-distort",
    "emission": "overlay-one-emission",
    "color-blend": "overlay-two-color-blend",
    "color-ramp": "color-ramp",
    "dissolve": "dissolve-and-ramp",
    "shared-uv-custom-data": "shared-uv-and-custom-data",
    "particle-controls": "particle-flipbook-and-soft-particle",
    "render-state": "blend-z-stencil-color-mask",
    "fresnel-depth": "fresnel-depth-and-depth-decal",
    "vertex-offset": "vertex-offset-and-local-transform",
    "parallax": "pom-normal-lighting",
    "mixed-depth-legacy": "legacy-depth-and-material-intent",
    "z-state": "override-z",
    "protocol-and-inspector-state": "full-packed-protocol-boundaries",
    "inspector-range-metadata": "inspector-ranges-and-debug",
}

def pass_for_line(line: int) -> str | None:
    for index, p in enumerate(passes):
        upper = passes[index+1]["line"] if index+1 < len(passes) else len(shader_lines)+1
        if p["line"] <= line < upper:
            return p["name"]
    return None

rows = []
for p in properties["property_rows"]:
    name = p["name"]
    feature = p["source_section_candidate"]
    exact_body = []
    inherited_body = []
    for ref in p["shader_body_lexical_sites"]:
        pass_name = pass_for_line(ref["line"])
        if pass_name:
            exact_body.append({"pass": pass_name, "line": ref["line"], "evidence_kind": "literal_shaderlab_body_reference_not_execution"})
        elif subshader_line < ref["line"] < passes[0]["line"]:
            inherited_body.append({"line": ref["line"], "possible_inherited_passes": pass_names,
                                   "evidence_kind": "literal_subshader_state_binding_possible_inheritance_not_execution"})
    exact_keywords = sorted({ref["keyword"] for ref in keyword_bindings[name]})
    keyword_passes = []
    for kw in exact_keywords:
        keyword_passes.extend({"pass": q["name"], "keyword": kw,
                               "pragma_kind": q["keywords"][kw],
                               "evidence_kind": "exact_keyword_pragma_presence_not_runtime_selection"}
                              for q in passes if kw in q["keywords"])
    hint_keywords = FEATURE_KEYWORDS.get(feature, [])
    hint_passes = sorted({q["name"] for q in passes if any(kw in q["keywords"] for kw in hint_keywords)})
    # Every original ShaderLab pass includes the shared ForwardPass HLSL; that
    # establishes include possibility, not operation or future Graph coverage.
    common_hlsl_possible = pass_names if p["active_hlsl_lexical_sites"] and p["provenance_candidate"] != "serialized_property_no_direct_lexical_reference" else []
    case_slug = CASE_GROUP.get(feature, feature)
    case_id = "T01-PLAN-" + re.sub(r"[^A-Z0-9]+", "-", case_slug.upper()).strip("-")
    needs = ["manual_feature_semantics_and_alias_review", "unity_6000_3_urp17_3_graph_vfx_execution", "pass_capture_or_generated_shader_inspection", "mesh_vfx_baseline_comparison"]
    if p["provenance_candidate"] == "serialized_property_no_direct_lexical_reference":
        needs.append("user_material_serialization_and_external_writer_search")
    if p["provenance_candidate"] == "packed_protocol_candidate":
        needs.append("exact_uint_high_bit_and_sign_boundary_gpu_roundtrip")
    if feature in ("noise-distortion", "render-state", "z-state", "stencil"):
        needs.append("both_exact_distortion_lightmodes_and_nbpostprocess_readback_where_applicable")
    rows.append({
        "feature_id_candidate": "F-" + feature.upper().replace("-", "_"),
        "feature_source_section": feature,
        "property": name,
        "shader_property_line": p["shader_property"]["line"],
        "property_provenance_candidate": p["provenance_candidate"],
        "target_destination_candidate": p["urp_graph_vfx_destination_candidate"],
        "pass_links": {
            "literal_shaderlab_body": exact_body,
            "literal_subshader_state_binding": inherited_body,
            "exact_intent_keyword_pragma": keyword_passes,
            "feature_group_keyword_hint": {"keywords": hint_keywords, "passes": hint_passes,
                                           "evidence_kind": "manual_grouping_plus_pragma_lexical_only"},
            "common_hlsl_include_possible": common_hlsl_possible,
            "runtime_pass_reachability": "not_verified",
            "graph_generated_pass_coverage": "not_verified",
        },
        "case": {"id": case_id, "group": case_slug, "status": "planned_not_executed",
                 "required_evidence": needs,
                 "relationship": "proposed_feature_group_case_not_a_property_specific_test"},
        "source_refs": {"property": f"Packages/NB_FX/NBShaders2/Shader/NBShader.shader:{p['shader_property']['line']}",
                        "active_hlsl_lexical_sites": p["active_hlsl_lexical_sites"][:8],
                        "cs_lexical_sites": p["cs_lexical_sites"][:8],
                        "sample_serialization_examples": p["sample_serialization"]["examples"][:2]},
    })

assert len(rows) == 450 and len({p["property"] for p in rows}) == 450
feature_groups = sorted({p["feature_source_section"] for p in rows})
case_ids = sorted({p["case"]["id"] for p in rows})
summary = {
    "property_rows": len(rows),
    "feature_section_candidates": len(feature_groups),
    "planned_case_groups": len(case_ids),
    "original_shaderlab_passes": [{"name": q["name"], "lightMode": q["lightMode"], "line": q["line"]} for q in passes],
    "rows_with_literal_shaderlab_body_pass_link": sum(bool(p["pass_links"]["literal_shaderlab_body"]) for p in rows),
    "rows_with_literal_subshader_state_binding": sum(bool(p["pass_links"]["literal_subshader_state_binding"]) for p in rows),
    "rows_with_exact_intent_keyword_pragma_link": sum(bool(p["pass_links"]["exact_intent_keyword_pragma"]) for p in rows),
    "rows_with_feature_keyword_pass_hint": sum(bool(p["pass_links"]["feature_group_keyword_hint"]["passes"]) for p in rows),
    "rows_with_common_hlsl_include_possibility": sum(bool(p["pass_links"]["common_hlsl_include_possible"]) for p in rows),
    "rows_without_any_pass_candidate": sum(not (p["pass_links"]["literal_shaderlab_body"] or p["pass_links"]["literal_subshader_state_binding"] or p["pass_links"]["exact_intent_keyword_pragma"] or p["pass_links"]["feature_group_keyword_hint"]["passes"] or p["pass_links"]["common_hlsl_include_possible"]) for p in rows),
    "rows_without_exact_per_property_pass_link": sum(not (p["pass_links"]["literal_shaderlab_body"] or p["pass_links"]["literal_subshader_state_binding"] or p["pass_links"]["exact_intent_keyword_pragma"]) for p in rows),
    "rows_without_any_executed_property_case": len(rows),
    "rows_without_current_urp_graph_vfx_runtime_confirmation": len(rows),
    "case_status_counts": dict(Counter(p["case"]["status"] for p in rows)),
    "input_sha256": {str(path.relative_to(ROOT)): sha(path) for path in (PROPS, KEYWORDS, SHADER, Path(__file__))},
    "environment_target": "Unity 6000.3.18f1 + URP/ShaderGraph/VFX 17.3; D21: 2021.3/older-version compatibility not G1 blocker",
}
out = {"summary": summary, "trace_rows": rows, "interpretation": [
    "ShaderLab literal binding proves only textual binding in the named original pass or SubShader state block, not effective rendering or inherited-override resolution.",
    "Exact intent keyword and feature-group hint prove only pragma presence; dynamic selection and generated Graph/VFX pass behavior require Unity verification.",
    "Common HLSL include is possible in seven original passes; preprocessor branch, shader stage, and actual read are not determined by this matrix.",
    "All case IDs are proposed trace anchors, not implemented or executed tests. Existing G0/GF evidence proves baseline/feasibility only, not 450-property parity.",
    "D21 narrows the immediate validation environment; it does not authorize deleting legacy properties or claiming 2021.3 regression passed.",
]}
OUT.write_text(json.dumps(out, ensure_ascii=False, indent=2) + "\n")
lines = ["# T01 Feature → Property → Pass → case trace proposal (NOT G1)", "",
         f"450 properties; {len(feature_groups)} feature-section candidates; {len(case_ids)} planned case groups; 7 original ShaderLab passes.",
         "", "## Closure counts", ""]
for key in ("rows_with_literal_shaderlab_body_pass_link", "rows_with_literal_subshader_state_binding", "rows_with_exact_intent_keyword_pragma_link", "rows_with_feature_keyword_pass_hint", "rows_with_common_hlsl_include_possibility", "rows_without_any_pass_candidate", "rows_without_exact_per_property_pass_link", "rows_without_any_executed_property_case", "rows_without_current_urp_graph_vfx_runtime_confirmation"):
    lines.append(f"- `{key}`: {summary[key]}")
lines += ["", "## Semantics", *["- " + i for i in out["interpretation"]], "", "## Planned feature/case groups", ""]
for feature in feature_groups:
    group_rows = [p for p in rows if p["feature_source_section"] == feature]
    lines.append(f"- `F-{feature.upper().replace('-', '_')}` → {len(group_rows)} properties → `{group_rows[0]['case']['id']}` (planned, not executed).")
lines += ["", "## Mandatory manual/Unity closure", "",
          "1. Review all section and property→feature assignments, especially 31 serialized-only Properties and 22 CBUFFER-only fields in the companion semantic inventory.",
          "2. Inspect exact generated URP Graph/VFX passes and keyword variants after product port; do not substitute GF isolated prototype.",
          "3. Run feature-specific Mesh/VFX baseline image, intermediate RT, protocol boundary, build/Player and persistence cases in the current 6000.3/17.3 environment.",
          "4. Record all failed, unrun, and externally bound cases; G1 may freeze *contract/test obligations*, not assert unrun parity.", ""]
REPORT.write_text("\n".join(lines))
print(json.dumps({k:summary[k] for k in summary if k.startswith("rows_")}, indent=2))
