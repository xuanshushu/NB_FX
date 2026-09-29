T08 Color Ramp active VFX Mesh Player, 2026-09-30

Product code commit 44d0307. Unity 6000.3.18f1, URP/SG/VFX 17.3, Metal.
unityMCP unavailable; exclusive isolated CLI clone only. Test-only Python,
C#, four VFX assets, generated shaders, logs, JSON and PNGs are archived.

Editor build succeeded errors0/warnings33. Generated VFX shaders contain
all intended Ramp key/count/tint/source/Flags values, texture binding, the
Ramp function and both exact LightMode Pass tags. Editor lazy hidden
passCount1 is not the compiled Player verdict.

Player exit0, result.json PASS. Four real one-particle Cube Mesh Outputs:
Off, texture Multiply, texture Add, UV0 Multiply. Each alive1, Pass5,
custom indices1/2, visible676, common ROI576, repeat maxRGB0. Every
enabled case differs from Off on all ROI pixels; UV0 source differs from
texture source as expected. Black/white background paired linear ARGBHalf
captures infer effective alpha and pre-blend source RGB, not direct GPU
alpha-channel readback. Effective alpha Off .938965 vs predicted .938962;
Multiply .278992 vs .278500; Add .997640 vs .997785; UV0 Multiply
.278945 vs .278500. Texture cases source RGB median error <.0005;
UV0 case only checks key bounds and difference, not invented exact UV.

No proof of arbitrary key counts, animated offset/rotation, Noise,
CustomData/special UV, wrap/ForceNoMip, old ShaderLab T00 Frozen B/C,
postprocess RT/Uber, real Output GUI/persistence, other platforms, or
G5/G6 approval. Player log retains fallback-shader-not-found messages
despite visible numeric PASS. No push.

Replay outside both repos using prepare_assets.py.txt to generate test
textures/VFX in an isolated clone, run NBFXRampPlayerBuild.Run, and execute
the built app with --nbfx-ramp-output=<outside-repo-dir>, no -nographics.
Read original Editor/Player logs and result.json. Test-only clone Assets were removed; cleanup compile exited 0 and
ProjectSettings/renderer/global SHA-256 stayed fixed.
