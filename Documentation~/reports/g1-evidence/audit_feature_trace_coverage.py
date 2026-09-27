#!/usr/bin/env python3
"""T01 review aid: writer/read/pass gaps, lexical only; writes its JSON/Markdown beside this script.

Run: python3 Packages/NB_FX/Documentation~/reports/g1-evidence/audit_feature_trace_coverage.py
Requires classify_property_semantics.py and build_feature_trace_matrix.py first.
"""
from __future__ import annotations

from collections import Counter
import hashlib
import json
from pathlib import Path
import re

ROOT = next(p for p in Path(__file__).resolve().parents if (p / "Packages/NB_FX/package.json").is_file())
TEMP = Path(__file__).resolve().parent
SEM = TEMP / "property-semantics-candidates.json"
TRACE = TEMP / "feature-property-pass-case-candidates.json"
OUT = TEMP / "feature-trace-coverage-audit.json"
MD = TEMP / "feature-trace-coverage-audit.md"

semantic = json.loads(SEM.read_text())
trace = json.loads(TRACE.read_text())
props = {p["name"]: p for p in semantic["property_rows"]}
assert len(props) == len(trace["trace_rows"]) == 450
source_cache: dict[Path, list[str]] = {}
hlsl_sources = [ROOT / path for path in semantic["summary"]["source_sha256"] if path.endswith(".hlsl")]

def source(ref: dict) -> str:
    path = ROOT / ref["file"]
    if path not in source_cache:
        source_cache[path] = path.read_text(errors="replace").splitlines()
    lines = source_cache[path]
    line = ref["line"]
    return lines[line - 1].strip() if 0 < line <= len(lines) else "<line-out-of-range>"

def cs_evidence(ref: dict) -> dict:
    line = source(ref)
    is_editor = "/Editor/" in ref["file"]
    if re.search(r"\b(?:Set(?:Float|Int|Integer|Vector|Color|Texture|Matrix|FloatIfExists|IntIfExists)|\.floatValue\s*=|\.vectorValue\s*=)\b", line):
        operation = "writer_candidate"
    elif re.search(r"\b(?:Get(?:Float|Int|Integer|Vector|Color|Texture|Matrix)|HasProperty)\s*\(", line):
        operation = "reader_or_guard_candidate"
    elif "Shader.PropertyToID" in line:
        operation = "property_id_declaration"
    elif re.search(r"\b(?:PropertyName|RangePropertyName)\s*=|new\s+\w+Item\s*\(", line):
        operation = "editor_binding_candidate" if is_editor else "binding_candidate"
    else:
        operation = "lexical_reference_direction_unknown"
    return {**ref, "source": line[:220], "operation_candidate": operation,
            "scope": "editor" if is_editor else "runtime_or_other"}

def hlsl_evidence(ref: dict, name: str) -> dict:
    line = ref["text"]
    if re.search(r"\b(?:TEXTURE2D|Texture2D|SAMPLER|sampler2D)\b[^;]*\b" + re.escape(name) + r"\b\s*;", line):
        operation = "resource_declaration_not_read"
    elif line.lstrip().startswith("#define"):
        operation = "macro_alias_not_direct_read"
    elif re.search(r"(?<![A-Za-z0-9_])" + re.escape(name) + r"(?:\.[xyzwrgba]+)?\s*(?:\+=|-=|\*=|/=|(?<![=!<>])=(?!=))", line):
        operation = "direct_assignment_candidate"
    else:
        operation = "read_or_argument_candidate"
    return {**ref, "operation_candidate": operation}

def macro_alias_uses(hlsl_sites: list[dict]) -> list[dict]:
    uses = []
    for ref in hlsl_sites:
        if ref["operation_candidate"] != "macro_alias_not_direct_read":
            continue
        m = re.match(r"\s*#define\s+([A-Za-z_]\w*)\b", ref["text"])
        if not m:
            continue
        alias = m.group(1)
        pat = re.compile(r"(?<![A-Za-z0-9_])" + re.escape(alias) + r"(?![A-Za-z0-9_])")
        for file in hlsl_sources:
            if file not in source_cache:
                source_cache[file] = file.read_text(errors="replace").splitlines()
            for line_no, line in enumerate(source_cache[file], 1):
                if file == ROOT / ref["file"] and line_no == ref["line"]:
                    continue
                if pat.search(line.split("//",1)[0]):
                    uses.append({"alias":alias,"file":str(file.relative_to(ROOT)),"line":line_no,"source":line.strip()[:220],
                                 "evidence_kind":"macro_alias_lexical_use_not_expansion_or_runtime"})
    return uses

def has_pass(row: dict) -> bool:
    p = row["pass_links"]
    return bool(p["literal_shaderlab_body"] or p["literal_subshader_state_binding"] or
                p["exact_intent_keyword_pragma"] or p["feature_group_keyword_hint"]["passes"] or
                p["common_hlsl_include_possible"])

audited = []
for row in trace["trace_rows"]:
    p = props[row["property"]]
    name = row["property"]
    cs = [cs_evidence(ref) for ref in p["cs_lexical_sites"]]
    hlsl = [hlsl_evidence(ref, name) for ref in p["active_hlsl_lexical_sites"]]
    aliases = macro_alias_uses(hlsl)
    st_companion = next((x for x in semantic["cbuffer_without_properties"] if x["name"] == name + "_ST"), None)
    indirect = [] if st_companion is None else st_companion["macro_indirect_sites"]
    provenance = row["property_provenance_candidate"]
    no_pass = not has_pass(row)
    no_exact = not (row["pass_links"]["literal_shaderlab_body"] or row["pass_links"]["literal_subshader_state_binding"] or row["pass_links"]["exact_intent_keyword_pragma"])
    if provenance == "inspector_state_candidate" or (name.endswith("RangeVec") and not hlsl):
        boundary = "inspector_or_serialized_metadata_candidate"
    elif provenance == "serialized_property_no_direct_lexical_reference":
        boundary = "serialized_legacy_semantics_unresolved"
    elif provenance == "cbuffer_declared_no_operational_lexical_site":
        boundary = "cbuffer_declared_lexically_unread_unresolved"
    elif provenance == "packed_protocol_candidate":
        boundary = "packed_protocol_indirect_consumer_candidate"
    elif provenance == "texture_binding_candidate" or indirect:
        boundary = "texture_or_implicit_st_sampler_candidate"
    elif any(x["operation_candidate"] in ("read_or_argument_candidate", "direct_assignment_candidate") for x in hlsl):
        boundary = "hlsl_operation_candidate"
    elif aliases:
        boundary = "hlsl_macro_indirection_candidate"
    elif p["cbuffer_field"] and hlsl and all(x["operation_candidate"] == "macro_alias_not_direct_read" for x in hlsl):
        boundary = "cbuffer_macro_alias_without_local_use_unresolved"
    elif cs and not hlsl:
        boundary = "host_or_inspector_control_candidate"
    elif row["pass_links"]["literal_shaderlab_body"] or row["pass_links"]["literal_subshader_state_binding"]:
        boundary = "shaderlab_state_binding_candidate"
    else:
        boundary = "manual_review_unclassified"
    audited.append({
        "property": name, "feature": row["feature_id_candidate"],
        "shader_property_line": row["shader_property_line"],
        "no_pass_candidate": no_pass, "no_exact_property_pass_link": no_exact,
        "boundary_candidate": boundary,
        "cs_sites": cs, "hlsl_sites": hlsl, "hlsl_macro_alias_uses": aliases,
        "implicit_texture_st_macro_sites": indirect,
        "cbuffer_field": p["cbuffer_field"],
        "pass_link_evidence": row["pass_links"],
        "planned_case": row["case"],
        "review_warning": "Lexical direction/binding is not a proven write/read, runtime pass selection, or Graph/VFX parity.",
    })

assert len(audited) == 450
no_pass = [p for p in audited if p["no_pass_candidate"]]
no_exact = [p for p in audited if p["no_exact_property_pass_link"]]
assert len(no_pass) == 146 and len(no_exact) == 398
summary = {
    "properties": 450, "no_pass_candidate": len(no_pass), "no_exact_property_pass_link": len(no_exact),
    "no_pass_by_boundary_candidate": dict(sorted(Counter(p["boundary_candidate"] for p in no_pass).items())),
    "no_exact_by_boundary_candidate": dict(sorted(Counter(p["boundary_candidate"] for p in no_exact).items())),
    "all_by_boundary_candidate": dict(sorted(Counter(p["boundary_candidate"] for p in audited).items())),
    "writer_candidate_rows": sum(any(r["operation_candidate"] == "writer_candidate" for r in p["cs_sites"]) for p in audited),
    "reader_or_guard_candidate_rows": sum(any(r["operation_candidate"] == "reader_or_guard_candidate" for r in p["cs_sites"]) for p in audited),
    "editor_binding_candidate_rows": sum(any(r["operation_candidate"] == "editor_binding_candidate" for r in p["cs_sites"]) for p in audited),
    "hlsl_read_or_argument_candidate_rows": sum(any(r["operation_candidate"] == "read_or_argument_candidate" for r in p["hlsl_sites"]) for p in audited),
    "hlsl_direct_assignment_candidate_rows": sum(any(r["operation_candidate"] == "direct_assignment_candidate" for r in p["hlsl_sites"]) for p in audited),
    "texture_property_rows_with_implicit_st_macro_sites": sum(bool(p["implicit_texture_st_macro_sites"]) for p in audited),
    "still_no_executed_feature_property_case": 450,
    "still_no_product_graph_vfx_pass_validation": 450,
    "source_sha256": {str(path.relative_to(ROOT)):hashlib.sha256(path.read_bytes()).hexdigest() for path in (SEM, TRACE, Path(__file__))},
}

decision_packets = [
    {"id": "E1", "scope": "all 31 serialized legacy or no-direct properties", "members": [p["property"] for p in audited if props[p["property"]]["provenance_candidate"] == "serialized_property_no_direct_lexical_reference"],
     "request": "Freeze preservation/alias policy; check production user materials and external scripts before any removal or Graph exposure decision.",
     "state": "manual_decision_required; no deletion inference"},
    {"id": "E2", "scope": "CBUFFER declared but no operational lexical read plus two CBUFFER-only unknowns", "members": [p["property"] for p in audited if p["boundary_candidate"] in ("cbuffer_declared_lexically_unread_unresolved", "cbuffer_macro_alias_without_local_use_unresolved")] + ["_ClipRect", "_FresnelUnit2"],
     "request": "Inspect variant/preprocessor and external UI binding for `_ClipRect`; confirm `_FresnelUnit2` commented Property intent; preserve until contract decision.",
     "state": "manual source/Unity review required"},
    {"id": "E3", "scope": "host/inspector boundary", "members": [p["property"] for p in no_pass if p["boundary_candidate"] in ("inspector_or_serialized_metadata_candidate", "host_or_inspector_control_candidate", "packed_protocol_indirect_consumer_candidate")],
     "request": "Assign each to editor metadata, material-intent/packed-flag encoder, runtime helper, or Graph/VFX control; do not equate no direct Pass to no feature.",
     "state": "main-Agent contract ownership decision required"},
    {"id": "E4", "scope": "HLSL/texture-to-generated-Pass reachability", "members": [p["property"] for p in no_exact if p["boundary_candidate"] in ("hlsl_operation_candidate", "hlsl_macro_indirection_candidate", "texture_or_implicit_st_sampler_candidate", "packed_protocol_indirect_consumer_candidate")],
     "request": "Trace include/call/preprocessor/stage in shared HLSL, then verify generated URP Graph/VFX Pass and actual current-environment draw/readback. GF isolated proof is insufficient.",
     "state": "test/implementation obligation, not evidence of failure"},
]

output = {"summary":summary, "rows":audited, "main_agent_exception_packets":decision_packets,
          "interpretation":[
              "A no-pass candidate can be legitimate editor/host state, a packed indirection, or an unresolved binding; it is not a deletion list.",
              "C# Set/Get, GUI constructor, and HLSL assignment/read labels are local syntax candidates only; control flow, reflection, MPB, Unity binding, and CBUFFER mutation are not proven.",
              "The 398 figure is absence of exact per-property Pass linkage in the current static method, not 398 missing product features.",
              "All cases remain planned and unrun; D21 current 6000.3.18f1/17.3 target is the next validation environment, not evidence already collected.",
          ]}
OUT.write_text(json.dumps(output,ensure_ascii=False,indent=2)+"\n")
lines=["# T01 writer/read/pass coverage audit (draft; no G1 claim)","",
       "Replay: `python3 Packages/NB_FX/Documentation~/reports/g1-evidence/classify_property_semantics.py && python3 Packages/NB_FX/Documentation~/reports/g1-evidence/build_feature_trace_matrix.py && python3 Packages/NB_FX/Documentation~/reports/g1-evidence/audit_feature_trace_coverage.py`.","",
       f"Rows: 450. No static Pass candidate: {len(no_pass)}. No exact per-property Pass link: {len(no_exact)}. Executed product property cases: 0.","",
       "## 146 no-Pass candidate breakdown",""]
lines += [f"- {k}: {v}" for k,v in summary["no_pass_by_boundary_candidate"].items()]
lines += ["","## 398 no-exact-link breakdown",""]
lines += [f"- {k}: {v}" for k,v in summary["no_exact_by_boundary_candidate"].items()]
lines += ["","## Minimum exception packets for main Agent",""]
lines += [f"- {e['id']} — {e['scope']}: {len(e['members'])} members. {e['request']}" for e in decision_packets]
lines += ["","## Limits", *["- "+v for v in output["interpretation"]],""]
MD.write_text("\n".join(lines))
print(json.dumps({"no_pass":summary["no_pass_by_boundary_candidate"],"no_exact":summary["no_exact_by_boundary_candidate"],"exceptions":{e["id"]:len(e["members"]) for e in decision_packets}},indent=2))
