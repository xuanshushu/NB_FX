T08 Overlay 1 active VFX Mesh Player, 2026-09-30

Product code commit 5c94de7. Unity 6000.3.18f1, URP/SG/VFX 17.3, Metal.
unityMCP unavailable; exclusive isolated CLI clone used. Replay fixture was
prepared under /tmp and no test file was placed into product or main Assets.
The archived prepare_assets.py.txt/C# probes and five gzipped test VFX assets
are test-only, not deliverable sample materials.

Editor built StandaloneOSX app with 0 errors, 35 warnings. All five imported
VFX Outputs had explicit intended _EmissionMap, enabled, tint, intensity and
Flags values in generated source, plus both exact custom Pass tags. Editor
lazy hidden material passCount=1 was NOT used as the compiled Player verdict.

Player exited 0, result.json status PASS. Five one-particle Cube Mesh Outputs:
Off, Add, Multiply, AddAlpha, MulAlpha. Each alive1, not culled, Pass5,
custom pass indices1/2, 676 visible pixels, 576 safe ROI pixels, frozen
repeat maxRGB0. All enabled modes differ from Off in all 576 ROI pixels.
The black/white background paired linear ARGBHalf captures infer effective
source-over alpha and source RGB; alpha is NOT a direct GPU channel readback.
Off/Add/Multiply coverage ~0.938965 vs expected 0.938962; AddAlpha/MulAlpha
~0.6132/0.61338 vs expected 0.61305. Maximum per-case source RGB median
error vs original overlay formula <0.0008. See result.json for exact numbers.

No NBPostprocess RT/Uber, old ShaderLab strict B/C, advanced UV/Noise/
CustomData, Output GUI, all renderers, or other platform verification. G5
remains open. The Player log's fallback-shader-not-found messages did not
prevent the visible numeric captures; they are preserved verbatim and must
not be hidden as though the build had a clean Console.

Replay: inflate five .vfx.gz assets into a separate clone Assets, copy the
archived Python-generated 1x1 textures or rerun prepare_assets.py.txt in
the original fixture layout, compile NBFXOverlayPlayerBuild.Run, run the
built app with --nbfx-overlay-output=<outside-repo-dir>, no -nographics.
Compare result.json and the paired PNGs. After evidence archive, all test-only
Assets were removed from the clone; cleanup compile exit 0. Clone ProjectSettings
and two renderer/global settings hashes stayed unchanged from pre-test values.
Do not push without user request.
