# T01 Feature → Property → Pass → case trace proposal (NOT G1)

450 properties; 24 feature-section candidates; 24 planned case groups; 7 original ShaderLab passes.

## Closure counts

- `rows_with_literal_shaderlab_body_pass_link`: 3
- `rows_with_literal_subshader_state_binding`: 12
- `rows_with_exact_intent_keyword_pragma_link`: 37
- `rows_with_feature_keyword_pass_hint`: 224
- `rows_with_common_hlsl_include_possibility`: 215
- `rows_without_any_pass_candidate`: 146
- `rows_without_exact_per_property_pass_link`: 398
- `rows_without_any_executed_property_case`: 450
- `rows_without_current_urp_graph_vfx_runtime_confirmation`: 450

## Semantics
- ShaderLab literal binding proves only textual binding in the named original pass or SubShader state block, not effective rendering or inherited-override resolution.
- Exact intent keyword and feature-group hint prove only pragma presence; dynamic selection and generated Graph/VFX pass behavior require Unity verification.
- Common HLSL include is possible in seven original passes; preprocessor branch, shader stage, and actual read are not determined by this matrix.
- All case IDs are proposed trace anchors, not implemented or executed tests. Existing G0/GF evidence proves baseline/feasibility only, not 450-property parity.
- D21 narrows the immediate validation environment; it does not authorize deleting legacy properties or claiming 2021.3 regression passed.

## Planned feature/case groups

- `F-BASE_COLOR_UV` → 29 properties → `T01-PLAN-BASE-UV-SAMPLING-AND-COLOR` (planned, not executed).
- `F-COLOR_BLEND` → 8 properties → `T01-PLAN-OVERLAY-TWO-COLOR-BLEND` (planned, not executed).
- `F-COLOR_RAMP` → 16 properties → `T01-PLAN-COLOR-RAMP` (planned, not executed).
- `F-DISSOLVE` → 27 properties → `T01-PLAN-DISSOLVE-AND-RAMP` (planned, not executed).
- `F-EMISSION` → 12 properties → `T01-PLAN-OVERLAY-ONE-EMISSION` (planned, not executed).
- `F-FEATURE_SWITCHES` → 24 properties → `T01-PLAN-KEYWORD-AND-PACKED-FLAG-SYNC` (planned, not executed).
- `F-FRESNEL_DEPTH` → 6 properties → `T01-PLAN-FRESNEL-DEPTH-AND-DEPTH-DECAL` (planned, not executed).
- `F-HOST_MODE` → 12 properties → `T01-PLAN-HOST-MODE-AND-PASS-STATE` (planned, not executed).
- `F-INSPECTOR_FOLDOUT` → 73 properties → `T01-PLAN-MATERIAL-GUI-SERIALIZATION` (planned, not executed).
- `F-INSPECTOR_RANGE_METADATA` → 17 properties → `T01-PLAN-INSPECTOR-RANGES-AND-DEBUG` (planned, not executed).
- `F-LIGHTING` → 19 properties → `T01-PLAN-LIGHTING-MODES-AND-SHADOW` (planned, not executed).
- `F-MASK` → 27 properties → `T01-PLAN-MASK-AND-GRADIENT` (planned, not executed).
- `F-MIXED_DEPTH_LEGACY` → 24 properties → `T01-PLAN-LEGACY-DEPTH-AND-MATERIAL-INTENT` (planned, not executed).
- `F-NOISE_DISTORTION` → 21 properties → `T01-PLAN-NOISE-CAMERA-AND-DEFERRED-DISTORT` (planned, not executed).
- `F-PARALLAX` → 4 properties → `T01-PLAN-POM-NORMAL-LIGHTING` (planned, not executed).
- `F-PARTICLE_CONTROLS` → 5 properties → `T01-PLAN-PARTICLE-FLIPBOOK-AND-SOFT-PARTICLE` (planned, not executed).
- `F-PROTOCOL_AND_INSPECTOR_STATE` → 19 properties → `T01-PLAN-FULL-PACKED-PROTOCOL-BOUNDARIES` (planned, not executed).
- `F-RENDER_STATE` → 19 properties → `T01-PLAN-BLEND-Z-STENCIL-COLOR-MASK` (planned, not executed).
- `F-SHARED_UV_CUSTOM_DATA` → 6 properties → `T01-PLAN-SHARED-UV-AND-CUSTOM-DATA` (planned, not executed).
- `F-STENCIL` → 1 properties → `T01-PLAN-STENCIL-AND-PORTAL` (planned, not executed).
- `F-TWIRL_POLAR_LEGACY` → 9 properties → `T01-PLAN-TWIRL-POLAR-SHARED-UV` (planned, not executed).
- `F-VAT` → 57 properties → `T01-PLAN-GEOMETRY-VAT-MODES` (planned, not executed).
- `F-VERTEX_OFFSET` → 10 properties → `T01-PLAN-VERTEX-OFFSET-AND-LOCAL-TRANSFORM` (planned, not executed).
- `F-Z_STATE` → 5 properties → `T01-PLAN-OVERRIDE-Z` (planned, not executed).

## Mandatory manual/Unity closure

1. Review all section and property→feature assignments, especially 31 serialized-only Properties and 22 CBUFFER-only fields in the companion semantic inventory.
2. Inspect exact generated URP Graph/VFX passes and keyword variants after product port; do not substitute GF isolated prototype.
3. Run feature-specific Mesh/VFX baseline image, intermediate RT, protocol boundary, build/Player and persistence cases in the current 6000.3/17.3 environment.
4. Record all failed, unrun, and externally bound cases; G1 may freeze *contract/test obligations*, not assert unrun parity.
