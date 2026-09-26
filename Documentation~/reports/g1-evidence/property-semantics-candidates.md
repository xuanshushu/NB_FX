# T01 property semantic candidate inventory (draft, NOT G1 approval)

Rows: 450; CBUFFER-without-Properties: 22; sample materials: 40.

## Rules
- No-direct-reference overrides all name-based interpretation and remains compatibility-preserve/unresolved.
- Foldout/packed/render-state/control/texture labels are CANDIDATES based on exact name, type, source section, and lexical evidence.
- Only physical-line, comment-stripped HLSL and TRANSFORM_TEX macro references are listed; no runtime execution claim.
- Candidate URP Graph/VFX destination is a planning bucket, not a confirmed Graph port or feature cut.

## Candidate counts
- active_hlsl_reference_candidate: 175
- cbuffer_declared_no_operational_lexical_site: 11
- host_intent_or_mode_control_candidate: 10
- host_side_control_candidate: 97
- inspector_state_candidate: 71
- packed_protocol_candidate: 14
- serialized_property_no_direct_lexical_reference: 31
- shaderlab_render_state_candidate: 13
- texture_binding_candidate: 28

## 31 no-direct-reference Properties

All are serialized compatibility properties, not deletion candidates. Search all user assets before any migration.

- `_DissolveRampUVModeFoldOut` — `NBShader.shader:56`; sample occurrences 38; candidate section inspector-foldout.
- `_ParallaxUVModeFoldOut` — `NBShader.shader:72`; sample occurrences 38; candidate section inspector-foldout.
- `_UIEffect_Toggle` — `NBShader.shader:80`; sample occurrences 38; candidate section host-mode.
- `_UseUV1_Toggle` — `NBShader.shader:89`; sample occurrences 38; candidate section host-mode.
- `_BackFaceColor_Toggle` — `NBShader.shader:164`; sample occurrences 38; candidate section feature-switches.
- `_PolarCordinateOnlySpecialFunciton_Toggle` — `NBShader.shader:167`; sample occurrences 38; candidate section feature-switches.
- `_CustomData1X_MainTexOffsetX_Toggle` — `NBShader.shader:169`; sample occurrences 38; candidate section feature-switches.
- `_CustomData1Y_MainTexOffsetY_Toggle` — `NBShader.shader:170`; sample occurrences 38; candidate section feature-switches.
- `_CustomData1Z_Dissolve_Toggle` — `NBShader.shader:171`; sample occurrences 38; candidate section feature-switches.
- `_CustomData1W_HueShift_Toggle` — `NBShader.shader:172`; sample occurrences 38; candidate section feature-switches.
- `_CustomData2X_MaskMapOffsetX_Toggle` — `NBShader.shader:173`; sample occurrences 38; candidate section feature-switches.
- `_CustomData2Y_MaskMapOffsetY_Toggle` — `NBShader.shader:174`; sample occurrences 38; candidate section feature-switches.
- `_CustomData2Z_FresnelOffset_Toggle` — `NBShader.shader:175`; sample occurrences 38; candidate section feature-switches.
- `_CustomData2W_Toggle` — `NBShader.shader:176`; sample occurrences 38; candidate section feature-switches.
- `_Chachu` — `NBShader.shader:277`; sample occurrences 38; candidate section twirl-polar-legacy.
- `_XianXingCH_UVRota` — `NBShader.shader:279`; sample occurrences 38; candidate section twirl-polar-legacy.
- `_jingxiangCH_dire` — `NBShader.shader:280`; sample occurrences 38; candidate section twirl-polar-legacy.
- `_CustomData1X` — `NBShader.shader:401`; sample occurrences 38; candidate section shared-uv-custom-data.
- `_CustomData1Y` — `NBShader.shader:402`; sample occurrences 38; candidate section shared-uv-custom-data.
- `_CustomData2X` — `NBShader.shader:404`; sample occurrences 38; candidate section shared-uv-custom-data.
- `_CameraNearFadeDistance` — `NBShader.shader:413`; sample occurrences 38; candidate section particle-controls.
- `_CameraFarFadeDistance` — `NBShader.shader:414`; sample occurrences 38; candidate section particle-controls.
- `_AlphaClip` — `NBShader.shader:421`; sample occurrences 40; candidate section render-state.
- `_ColorMode` — `NBShader.shader:474`; sample occurrences 38; candidate section mixed-depth-legacy.
- `_CameraFadingEnabled` — `NBShader.shader:477`; sample occurrences 38; candidate section mixed-depth-legacy.
- `_IntersectEnabled` — `NBShader.shader:480`; sample occurrences 38; candidate section mixed-depth-legacy.
- `_FlipbookMode` — `NBShader.shader:489`; sample occurrences 38; candidate section mixed-depth-legacy.
- `_Mode` — `NBShader.shader:490`; sample occurrences 38; candidate section mixed-depth-legacy.
- `_InspectorData` — `NBShader.shader:509`; sample occurrences 38; candidate section mixed-depth-legacy.
- `_W9ParticleShaderWrapFlags2` — `NBShader.shader:525`; sample occurrences 38; candidate section protocol-and-inspector-state.
- `EmiDistortionIntensityRangeVec` — `NBShader.shader:542`; sample occurrences 38; candidate section inspector-range-metadata.

## 22 CBUFFER fields without Properties

- `_AnimationSheetHelperBlendIntensity` — `NBShaderInput.hlsl:29`; external_animation_sheet_helper; operational lexical sites 1, implicit macro sites 0.
- `_BaseMap_AnimationSheetBlend_ST` — `NBShaderInput.hlsl:28`; external_animation_sheet_helper; operational lexical sites 1, implicit macro sites 0.
- `_BaseMap_ST` — `NBShaderInput.hlsl:27`; texture_st_companion; operational lexical sites 0, implicit macro sites 1.
- `_BumpTex_ST` — `NBShaderInput.hlsl:57`; texture_st_companion; operational lexical sites 0, implicit macro sites 1.
- `_ClipRect` — `NBShaderInput.hlsl:116`; unresolved_possible_ui_external; operational lexical sites 0, implicit macro sites 0.
- `_ColorBlendMap_ST` — `NBShaderInput.hlsl:157`; texture_st_companion; operational lexical sites 1, implicit macro sites 0.
- `_CustomLocalTransformLocalToWorld` — `NBShaderInput.hlsl:245`; external_local_transform_helper; operational lexical sites 2, implicit macro sites 0.
- `_CustomLocalTransformWorldToLocal` — `NBShaderInput.hlsl:246`; external_local_transform_helper; operational lexical sites 2, implicit macro sites 0.
- `_DissolveMap_ST` — `NBShaderInput.hlsl:138`; texture_st_companion; operational lexical sites 3, implicit macro sites 0.
- `_DissolveMaskMap_ST` — `NBShaderInput.hlsl:140`; texture_st_companion; operational lexical sites 1, implicit macro sites 0.
- `_DissolveRampMap_ST` — `NBShaderInput.hlsl:149`; texture_st_companion; operational lexical sites 1, implicit macro sites 0.
- `_EmissionMap_ST` — `NBShaderInput.hlsl:34`; texture_st_companion; operational lexical sites 1, implicit macro sites 0.
- `_FresnelUnit2` — `NBShaderInput.hlsl:111`; unresolved_commented_property; operational lexical sites 0, implicit macro sites 0.
- `_MaskMap2_ST` — `NBShaderInput.hlsl:129`; texture_st_companion; operational lexical sites 1, implicit macro sites 0.
- `_MaskMap3_ST` — `NBShaderInput.hlsl:130`; texture_st_companion; operational lexical sites 1, implicit macro sites 0.
- `_MaskMap_ST` — `NBShaderInput.hlsl:30`; texture_st_companion; operational lexical sites 0, implicit macro sites 1.
- `_NoiseMap_ST` — `NBShaderInput.hlsl:35`; texture_st_companion; operational lexical sites 1, implicit macro sites 0.
- `_NoiseMaskMap_ST` — `NBShaderInput.hlsl:36`; texture_st_companion; operational lexical sites 1, implicit macro sites 0.
- `_ParallaxMapping_Map_ST` — `NBShaderInput.hlsl:217`; texture_st_companion; operational lexical sites 1, implicit macro sites 0.
- `_RampColorMap_ST` — `NBShaderInput.hlsl:174`; texture_st_companion; operational lexical sites 1, implicit macro sites 0.
- `_VertexOffset_Map_ST` — `NBShaderInput.hlsl:192`; texture_st_companion; operational lexical sites 2, implicit macro sites 1.
- `_VertexOffset_MaskMap_ST` — `NBShaderInput.hlsl:194`; texture_st_companion; operational lexical sites 2, implicit macro sites 1.

## Still undecidable
- 31 serialized-only direct lexical misses: exact semantic aliases and user-material values unknown.
- _ClipRect UI provenance/read and _FresnelUnit2 active intent unknown.
- HLSL reachability by keyword/pass/stage, C# writer vs reader direction, MPB values, Graph VFX attribute frequency, and precision cannot be decided by lexical scan.
- URP Graph/SubTarget port exposure, generated property names, exact full-uint bridge, and shader variant behavior require functional tests.
