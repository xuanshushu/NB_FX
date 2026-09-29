T08 Overlay 2 active VFX Mesh Player, 2026-09-30

Product code 6a0329e. Unity 6000.3.18f1, URP/SG/VFX 17.3, Metal.
unityMCP unavailable; exclusive isolated CLI clone. Test-only scripts/assets
and raw evidence are archived here; not product or main project assets.

Five real one-particle Cube Mesh Outputs: Off, Multiply, Add, MulAlpha,
AddAlpha. Editor build succeeded with 0 errors/35 warnings; generated VFX
shader inspected for exact ColorBlend texture/toggle/vector/flag slots and
both LightMode tags. Editor lazy hidden passCount1 is not Player pass count.

Player exit0 and result.json PASS. All cases alive1, Pass5, custom indices
1/2, visible676, safe ROI576, repeat maxRGB0. Each enabled case differs
from Off on all 576 ROI pixels. Paired black/white background ARGBHalf
captures infer final effective alpha and pre-blend source RGB, not direct
GPU alpha-channel readback. Off/Add/Multiply effective alpha ~0.938965 vs
predicted 0.938962; AddAlpha/MulAlpha ~0.6132/0.61338 vs 0.61305. Maximum
per-case source RGB median error <0.0008. Exact data in result.json.

Not proof of advanced UV, Noise/CustomData, wrap/ForceNoMip, two-layer
combinations, old ShaderLab B/C, postprocess RT/Uber, Output GUI/asset
reopen, other renderers/platforms, or G5/G6 release. Player log retains
fallback-shader-not-found messages despite visible numeric PASS. No push.

Replay test-only fixture by inflating five .vfx.gz assets in an isolated
clone, regenerating its 1x1 PNG textures with prepare_assets.py.txt,
running NBFXOverlay2PlayerBuild.Run, and executing the app with
--nbfx-overlay2-output=<outside-repo-dir> and no -nographics. Inspect both
logs and result.json. The test-only clone Assets were removed after capture and a cleanup compile
exited 0; ProjectSettings and renderer/global asset SHA-256 stayed fixed.
