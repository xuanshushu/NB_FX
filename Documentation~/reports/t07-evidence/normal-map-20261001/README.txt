N0 ordinary curved Mesh NormalMap evidence; STRICT FAILURES PRESERVED.
Package input a26a729e4760bf4653215158b85d5bd9abcef366; product f11f752967797cba416d72bc1576d1426c0f687d.
A=G0 Frozen ShaderLab, B=current ShaderLab with pure shared decode extraction,
C=actual generated Graph. Product-source uses git show f11f752967797cba416d72bc1576d1426c0f687d:PATH,
not the later DS0 working tree. Seven changed commit paths plus Sampling.hlsl,
current NBShader.shader and G0 Frozen NBShader.shader are snapshotted.
Unity 6000.3.18f1; URP/SG/VFX17.3; Metal; RenderGraph. unityMCP unavailable.
Exclusive isolated clone CLI; no main Editor or official package/Target/
NBPostprocess/protected-setting edits. Three clone protected hashes equal prior
archives. Source parent is a docs-only commit; logical product input a26a729.

Replay in exclusive clone using product commit and dependencies:
NBFX_MESH_EVIDENCE_DIR=<out> UNITY_BURST_DISABLE_COMPILATION=1
Unity -batchmode -projectPath <clone> -runTests -testPlatform EditMode
-testFilter NBFX.Baseline.Tests.G4GraphNormalMapTests -testResults <xml> -logFile <log>.
No -quit/-nographics for tests. Initial import exit0, no CS/Shader error.
Test EXIT 2: 44 total, 10 pass, 34 STRICT FAIL, no skips.
DO NOT reinterpret 34 failures as tolerance passes; threshold unchanged.
User directs stop chasing low-magnitude color differences now and collect
unresolved cases for one later manual review, not automatic G3/G4 release.

22 scenarios times ortho/perspective. Curved ordinary Mesh with nonuniform and
negative scale, backface, NormalScale, mask mode, UV0/UV0zw/UV1/UV2/shared,
Twirl/Polar, four Wrap modes, auto/forced LOD, Fresnel/MatCap/both.
Original packed flags and raw normal sample, shared pure decode math; no new
keyword. 128x128 linear RGBAHalf, strict 64x64 interior ROI x/y32..95=4096
pixels. Lossless raw float32 RGBA gzip bottom-up + PNG; 4 warm-up renders.
All 44 A/B main+control strict RGBA zero; full frames finite; repeats zero;
strong positive controls min max-channel change 0.063720703125 and min
1831 changed ROI pixels (>0.01). B/C main max delta 0.0029296875,
control max delta 0.0029296875. Per-pixel magnitude is small, but failure
coverage is NOT always small: up to 2999/4096 ROI pixels in a control frame.
These exact nonzero values still fail the strict zero assertion. No cause
is established by this artifact; no unverified normalization fix claimed.

NormalMap output is wired only into Fresnel/MatCap in this slice. Lighting,
Refraction, CustomLocal, Depth/Shadow, screen RT, VFX, Player, GUI persistence,
full feature combinations, variant/stripping/batching/performance and cold
first frame are outside scope. G3/G4 remain incomplete. No push.
