Ordinary Mesh Forward blend and final Pass alpha-cutoff slice, 2026-10-01.
Package input e2bdae5; product 58f2048; parent input fbe4012.
Current Unity6000.3.18f1 / URP,SG,VFX17.3 / Metal / RenderGraph.
unityMCP unavailable; exclusive isolated clone CLI, main Editor not opened.
No official package/UniversalTarget/NBPostprocess/protected-settings edits.
SupportVFX remains enabled, but no real VFX test or Player in this slice.

Replay in exclusive clone with current environment dependencies:
NBFX_MESH_EVIDENCE_DIR=<out> UNITY_BURST_DISABLE_COMPILATION=1
Unity -batchmode -projectPath <clone> -runTests -testPlatform EditMode
-testFilter 'NBFX.Baseline.Tests.G4GraphBlendClipTests;NBFX.Baseline.Tests.G4GraphScreenNoiseTests'
-testResults <xml> -logFile <log>. No -quit/-nographics on tests.
Final results 61/61, no skips, import/test exit0, no CS/Shader errors.
48 Forward =6 blend modes(opaque,alpha,premultiply,additive,additive-mix,multiply)
*4 clip states(off,.25,.5,.8)*2 cameras(orthographic,perspective).
A=frozen, B=current NBShader ShaderLab, C=actual generated Graph.
All A/B, B/C main/control strict ROI RGBA0, repeated B/C0, full-frame finite.
Nonblack backdrop RGBA(.125,.25,.5,.25), sampled source RGB(.5,.25,.75),
alpha .25/.75. Strong alternate cutoff response >.04, not empty-only proof.
Linear128x128RGBAHalf, raw float32RGBA gzip lossless bottom-up.
Forward strict ROI x/y32..95 =4096 pixels. Four warmups =steady-state,
not proof of cold first frame, full-frame parity or performance.

13 screen tests =previous10 (8trueB/C+2Graph-only uniform fallback analytical)
+3 true B/C final-output clipping cases. Deferred coverage*intensity alpha
.125: cutoff.2 discards, cutoff.1 retains. CameraOpaque source alpha.25 with
cutoff.5 must retain because actual final alpha1. Strong alternative controls,
repeat0/finite. Screen strict ROI48*48. Do not describe 13 as13 B/C0 tests.
N2 complete input scope/fallback caveats in screen-noise-20261001 archive.

V1 13pass/48fail. Frozen/current already0, B/C differed only in alpha,
including background pixels: fixture left the legacy additional distortion
passes enabled without _SCREEN_DISTORT_MODE, so the project's NB feature
redrew legacy material. Disable non-Forward Depth/Shadow/2D/twoNB passes
on all temporary materials; product/strict threshold unchanged, V2 all61pass.
Initial XML/log/captures/source retained in fixture-correction/v1.
This fixture intentionally measures Forward, not complete NBPostprocess.

Implementation: shared existing SurfaceV1 two-assignment blend output helper;
old ShaderLab calls same helper under original keywords/order. Graph-owned
Forward fragment reuses stock URP vertex/setup, keeps source alpha even opaque,
premultiplies RGB first, scales only final alpha when requested, clips final
alpha afterward. Graph adds old-name Cutoff and AdditiveToPreMultiply props;
Cutoff connected to actual AlphaClipThreshold. NB screen fragments clip their
encoded final output alpha, not the unencoded Surface alpha. No new keyword.
Explicit equal blend factors set in tests (including Graph separate alpha
blend factors). NOT proof that current URP Graph GUI writes NB-equivalent
blend defaults/keyword meaning. GUI/state adapter remains pending.

Depth/Shadow/dither, advanced Z/Stencil/Portal/OverrideZ, refraction/chroma,
all feature combinations, real Controller/Uber full chain, GUI persistence,
Player, generated variant/stripping/batching/performance not proved here.
FullG3/G4 still not passed. Local rollback slices only; no push.
