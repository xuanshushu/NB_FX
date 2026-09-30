M0 ordinary Mesh MatCap evidence, 2026-10-01.
Package input 64308ec31b32b64983d618823b5cd7e042ed8cc2; product a26a729e4760bf4653215158b85d5bd9abcef366.
The six product-source snapshots are read via git show a26a729e4760bf4653215158b85d5bd9abcef366:PATH; NOT the
later N0 working tree. Four paths changed in M0 commit (Graph, CF, test/meta),
plus shared EnvironmentV2 and Sampling headers as read-only dependencies.
Unity 6000.3.18f1; URP/SG/VFX 17.3; Metal; RenderGraph. unityMCP unavailable.
Exclusive isolated clone CLI; no main Editor, official package, UniversalTarget,
NBPostprocess, or protected setting edits. Three protected clone hashes match
prior archives exactly; they are not taken from the main working tree.

Replay in exclusive clone with source fixed at product commit and dependencies:
NBFX_MESH_EVIDENCE_DIR=<out> UNITY_BURST_DISABLE_COMPILATION=1
Unity -batchmode -projectPath <clone> -runTests -testPlatform EditMode
-testFilter NBFX.Baseline.Tests.G4GraphMatCapTests -testResults <xml> -logFile <log>.
No -quit or -nographics on tests. Initial import -batchmode -quit exit0;
no error CS/Shader error in import/test logs. Test exit0, 46/46 no skips.

23 cases times orthographic/perspective. B=current NBShader ShaderLab;
C=actual generated ShaderGraph. All 46 B/C main and control strict ROI RGB+Alpha
zero difference; repeat zero; all captured frames finite (including outside
ROI). Fixed 128x128 linear RGBAHalf, strict interior x/y=40..87 (2304 pixels),
lossless float32 RGBA raw gzip bottom-up plus PNG. Four warm-up renders are
steady-state evidence only, not cold-first-frame or full-frame parity.
40 active feature cases have per-side toggle/control response >=0.09375;
6 excluded-input (ST/wrap/sampler ignored) cases have zero effect from the
excluded input AND separate MatCap toggle response >=0.257080078125. Strong
control is not a blank output or weak nearly-zero sample.

Graph uses original shared EnvironmentV2 MatCap UV/composite math, fixed
linear clamp sampling and original NoMip bit5, no ST/wrap response. Order:
early color adjustment, then MatCap, then Emission. Geometry normals on
plain/rotated planes, front/back and camera changes are in this slice.
No NormalMap, Lighting, curved geometry/raw normal, Depth/Shadow, screen RT,
VFX, Player, GUI persistence, all-feature combination, variants, batching or
performance claim. G3/G4 ordinary Mesh full parity remains incomplete.
This M0 archive contains ONLY 46 M0 tests, no new 649-case regression claim.
Earlier Vertex regression belongs to its own separately archived evidence.
No push; package slice already committed by main Agent at a26a729.
