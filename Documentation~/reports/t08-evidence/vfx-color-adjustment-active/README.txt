T08 Color Adjustment active VFX Mesh Player, 2026-09-30

Product code 06d0ef6. Unity 6000.3.18f1, URP/SG/VFX 17.3, Metal.
unityMCP unavailable; exclusive isolated CLI clone only. Test-only Python,
C#, five VFX assets, generated shaders, logs, JSON and PNGs archived here.

Editor build succeeded errors0/warnings36. Generated VFX shader values for
Hue/Contrast/MidColor/Saturation/Refine and flag slices were checked per
Output, as were both exact custom LightMode tags. Editor lazy hidden
passCount1 is not Player pass count.

Player exit0/result.json PASS. Five real one-particle Cube Mesh Outputs:
Off, Contrast, Saturation0, Hue, Refine. All alive1, Pass5, custom indices
1/2, visible676, common ROI576, repeat maxRGB0. All four active modes
change all 576 pixels relative to Off. Paired black/white linear ARGBHalf
captures infer effective alpha and source RGB; not direct GPU alpha.
Effective alpha across cases .938965-.939290 vs predicted .938962.
Maximum per-case source RGB median error .00105 (Refine); Hue error
.00036 vs conventional HSV-wrap reference. Exact values in result.json.

This Output test deliberately leaves Flags0 bit22 off, so it does not
prove early/late ordering with Overlay1 or bit29 premultiply; those have
bounded Graph Mesh evidence. CustomData, full ShaderLab T00 Frozen B/C,
Fog/Lighting/MatCap order, postprocess RT/Uber, Output GUI/persistence,
other platforms, and G5/G6 remain open. Player log retains fallback-
shader-not-found messages despite visible numeric PASS. No push.

Replay test-only fixture using prepare_assets.py.txt in separate clone,
NBFXAdjustPlayerBuild.Run, and executable --nbfx-adjust-output=<external>
without -nographics. Test-only clone assets were removed afterward;
cleanup compile exit0 and ProjectSettings/renderer/global hashes unchanged.
