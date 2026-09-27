#!/usr/bin/env python3
"""T01 design routing; does not infer runtime reachability or Graph parity."""
from __future__ import annotations

import hashlib
import json
from collections import Counter
from pathlib import Path

HERE = Path(__file__).resolve().parent
SOURCE = HERE / "feature-trace-coverage-audit.json"
CBUFFER_SOURCE = HERE / "property-semantics-candidates.json"
OUT = HERE / "g1-property-routing.json"
audit = json.loads(SOURCE.read_text())
semantic = json.loads(CBUFFER_SOURCE.read_text())

lead_by_feature = {
    "F-BASE_COLOR_UV": "T02+T03", "F-COLOR_BLEND": "T03", "F-COLOR_RAMP": "T03",
    "F-DISSOLVE": "T02+T03", "F-EMISSION": "T02+T03",
    "F-FEATURE_SWITCHES": "T05+T07", "F-FRESNEL_DEPTH": "T03+T05",
    "F-HOST_MODE": "T05+T07", "F-INSPECTOR_FOLDOUT": "T07",
    "F-INSPECTOR_RANGE_METADATA": "T07", "F-LIGHTING": "T04",
    "F-MASK": "T02+T03", "F-MIXED_DEPTH_LEGACY": "T03+T05",
    "F-NOISE_DISTORTION": "T02+T03", "F-PARALLAX": "T02+T04",
    "F-PARTICLE_CONTROLS": "T02+T05", "F-PROTOCOL_AND_INSPECTOR_STATE": "T01+T07",
    "F-RENDER_STATE": "T05", "F-SHARED_UV_CUSTOM_DATA": "T02",
    "F-STENCIL": "T05", "F-TWIRL_POLAR_LEGACY": "T02",
    "F-VAT": "T04", "F-VERTEX_OFFSET": "T02+T04", "F-Z_STATE": "T05",
}

def destination(row: dict) -> tuple[str, str]:
    kind = row["boundary_candidate"]
    if row["feature"] in ("F-RENDER_STATE", "F-STENCIL", "F-Z_STATE") and kind in (
        "host_or_inspector_control_candidate", "shaderlab_state_binding_candidate"
    ):
        return "host_render_state", "Preserve ShaderLab Pass/queue/stencil binding; Graph/VFX host must prove equivalent state."
    if kind in ("hlsl_operation_candidate", "hlsl_macro_indirection_candidate"):
        return "shared_calculation_or_uniform_input", "Trace actual stage and generated Pass before parity claim."
    if kind == "texture_or_implicit_st_sampler_candidate":
        return "shared_sampling_resource_or_st_input", "Preserve texture/sampler/ST binding and import settings."
    if kind == "packed_protocol_indirect_consumer_candidate":
        return "canonical_uint_protocol_then_graph_wire_bridge", "Do not send the full packed word through Graph Float/half."
    if kind == "shaderlab_state_binding_candidate":
        return "host_render_state", "Preserve original Pass and material-state binding."
    if kind in ("inspector_or_serialized_metadata_candidate", "host_or_inspector_control_candidate"):
        return "host_gui_intent_or_sync_state", "Check indirect GUI/keyword/Flag/Pass writers before Graph/VFX control design."
    if kind == "serialized_legacy_semantics_unresolved":
        return "legacy_serialized_preserve_and_indirect_use_review", "Not a deletion or proven-unused decision."
    if kind in ("cbuffer_declared_lexically_unread_unresolved", "cbuffer_macro_alias_without_local_use_unresolved"):
        return "legacy_cbuffer_preserve_and_variant_review", "Do not remove the field from UnityPerMaterial."
    raise AssertionError((row["property"], kind))

rows = []
for row in audit["rows"]:
    route, caveat = destination(row)
    feature = row["feature"]
    assert feature in lead_by_feature, feature
    rows.append({
        "property": row["property"],
        "shader_property_line": row["shader_property_line"],
        "feature_id": feature,
        "candidate_boundary": row["boundary_candidate"],
        "shaderlab_route": "retain_exact_name_type_default_serialization_and_existing_7_pass_behavior",
        "new_graph_vfx_route": route,
        "module_lead": lead_by_feature[feature],
        "host_binding_leads": ["T05 original ShaderLab", "T06/T07 Graph", "T08/T09 VFX"],
        "planned_case": row["planned_case"]["id"],
        "source_pass_evidence": row["pass_link_evidence"],
        "source_writer_read_evidence": {
            "cs_sites": row["cs_sites"], "hlsl_sites": row["hlsl_sites"],
            "macro_alias_uses": row["hlsl_macro_alias_uses"],
            "implicit_st_sites": row["implicit_texture_st_macro_sites"],
            "cbuffer_field": row["cbuffer_field"],
        },
        "validation_state": "contract_route_only_no_product_graph_vfx_test",
        "caveat": caveat,
    })

assert len(rows) == len({r["property"] for r in rows}) == 450
cbuffer_routes = {
    "texture_st_companion": ("T02", "texture_st_from_host_preserve"),
    "external_animation_sheet_helper": ("T02+T05", "animation_sheet_runtime_input_preserve"),
    "external_local_transform_helper": ("T04+T05", "custom_local_matrix_runtime_input_preserve"),
    "unresolved_possible_ui_external": ("T05+T07", "keep_cbuffer_external_ui_binding_review"),
    "unresolved_commented_property": ("T05", "keep_cbuffer_comment_and_variant_review"),
}
cbuffer_only_rows = []
for field in semantic["cbuffer_without_properties"]:
    lead, route = cbuffer_routes[field["kind_candidate"]]
    cbuffer_only_rows.append({
        "name": field["name"], "field": field["field"],
        "kind_candidate": field["kind_candidate"], "module_lead": lead,
        "route": route, "hlsl_sites": field["active_hlsl_lexical_sites"],
        "macro_indirect_sites": field["macro_indirect_sites"],
        "external_source_evidence": field["external_source_evidence"],
        "validation_state": "preserve_original_cbuffer_no_graph_runtime_claim",
    })
assert len(cbuffer_only_rows) == 22
output = {
    "kind": "G1 main-Agent routing policy applied to every property; no parity claim",
    "input_sha256": hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
    "cbuffer_input_sha256": hashlib.sha256(CBUFFER_SOURCE.read_bytes()).hexdigest(),
    "script_sha256": hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
    "summary": {
        "properties_routed": len(rows),
        "cbuffer_only_fields_routed": len(cbuffer_only_rows),
        "by_graph_vfx_route": dict(sorted(Counter(r["new_graph_vfx_route"] for r in rows).items())),
        "by_module_lead": dict(sorted(Counter(r["module_lead"] for r in rows).items())),
        "runtime_cases_executed_by_this_script": 0,
    },
    "policy": [
        "All 450 existing ShaderLab properties remain unchanged; no rename/removal/repacking is authorized.",
        "Graph/VFX route is a design destination, not a claim that a Graph port or generated Pass already exists.",
        "Lexical misses and host-only candidates must remain reviewable; no property is declared dead code here.",
        "T02/T03/T04 may develop assigned calculations only after cross-module signatures and G1 are approved; T05 is sole ShaderLab binder.",
        "Every planned case must be executed or explicitly dispositioned at its later functional Gate; GF is not a substitute.",
    ],
    "rows": rows,
    "cbuffer_only_rows": cbuffer_only_rows,
}
OUT.write_text(json.dumps(output, ensure_ascii=False, indent=2) + "\n")
print(json.dumps(output["summary"], ensure_ascii=False, indent=2))
