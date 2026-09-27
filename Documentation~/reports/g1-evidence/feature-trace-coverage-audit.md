# T01 writer/read/pass coverage audit (draft; no G1 claim)

Replay: `python3 Packages/NB_FX/Documentation~/reports/g1-evidence/classify_property_semantics.py && python3 Packages/NB_FX/Documentation~/reports/g1-evidence/build_feature_trace_matrix.py && python3 Packages/NB_FX/Documentation~/reports/g1-evidence/audit_feature_trace_coverage.py`.

Rows: 450. No static Pass candidate: 146. No exact per-property Pass link: 398. Executed product property cases: 0.

## 146 no-Pass candidate breakdown

- cbuffer_declared_lexically_unread_unresolved: 8
- host_or_inspector_control_candidate: 28
- inspector_or_serialized_metadata_candidate: 83
- packed_protocol_indirect_consumer_candidate: 2
- serialized_legacy_semantics_unresolved: 25

## 398 no-exact-link breakdown

- cbuffer_declared_lexically_unread_unresolved: 11
- cbuffer_macro_alias_without_local_use_unresolved: 1
- hlsl_macro_indirection_candidate: 1
- hlsl_operation_candidate: 173
- host_or_inspector_control_candidate: 57
- inspector_or_serialized_metadata_candidate: 83
- packed_protocol_indirect_consumer_candidate: 14
- serialized_legacy_semantics_unresolved: 30
- texture_or_implicit_st_sampler_candidate: 28

## Minimum exception packets for main Agent

- E1 — all 31 serialized legacy or no-direct properties: 31 members. Freeze preservation/alias policy; check production user materials and external scripts before any removal or Graph exposure decision.
- E2 — CBUFFER declared but no operational lexical read plus two CBUFFER-only unknowns: 14 members. Inspect variant/preprocessor and external UI binding for `_ClipRect`; confirm `_FresnelUnit2` commented Property intent; preserve until contract decision.
- E3 — host/inspector boundary: 113 members. Assign each to editor metadata, material-intent/packed-flag encoder, runtime helper, or Graph/VFX control; do not equate no direct Pass to no feature.
- E4 — HLSL/texture-to-generated-Pass reachability: 216 members. Trace include/call/preprocessor/stage in shared HLSL, then verify generated URP Graph/VFX Pass and actual current-environment draw/readback. GF isolated proof is insufficient.

## Limits
- A no-pass candidate can be legitimate editor/host state, a packed indirection, or an unresolved binding; it is not a deletion list.
- C# Set/Get, GUI constructor, and HLSL assignment/read labels are local syntax candidates only; control flow, reflection, MPB, Unity binding, and CBUFFER mutation are not proven.
- The 398 figure is absence of exact per-property Pass linkage in the current static method, not 398 missing product features.
- All cases remain planned and unrun; D21 current 6000.3.18f1/17.3 target is the next validation environment, not evidence already collected.
