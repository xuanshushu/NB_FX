Ordinary Mesh advanced MainTex UV, first slice; 2026-09-30.
Parent input4602308; package input15b5678; candidate product40cd0d4.
Unity6000.3.18f1 / URP,SG,VFX packages17.3 / Metal / RenderGraph.
unityMCP unavailable; exclusive clone CLI, no VFX tests or main project opening.
No official package, UniversalTarget, original ShaderLab or NBPostprocess edits.

Replay G4GraphAdvancedUVTests from candidate40cd0d4.
NBFX_MESH_EVIDENCE_DIR=<output> UNITY_BURST_DISABLE_COMPILATION=1 Unity
-batchmode -projectPath <exclusive-clone> -runTests -testPlatform EditMode
-testFilter NBFX.Baseline.Tests.G4GraphAdvancedUVTests
-testResults <xml> -logFile <log>. Do not use -nographics or -quit.
22 cases=11 MainTex configurations times ortho/perspective; 19 strict pass,
3 strict failures retained, each ONE ROI pixel maxRGBA0.000244140625.
Functional controls, finite/visible/repeat and off-state equality all hold.
No tolerances/assertions changed; no cause claimed for the tiny differences.
Float raw data is linear RGBAHalf read as little-endian float32 RGBA,128x128,
row-major bottom-up; strict ROI x/y40..87,2304pixels, not full-frame or PNG.
B-on/C-on and B-off/C-off raw+PNG retained. Four renders before capture:
steady-state only, not cold first frame. Texture and all deterministic material,
Mesh TEXCOORD0/1/2 inputs are defined in the archived test source.

Only MainTex modes0(default)/1(special)/2(TwirlPolar)/8(shared) connected.
SharedUV sources0/1/2 have bounded tests. Special follows UV0.zw by default,
Mesh bits18/19 select independent TEXCOORD1.xy/TEXCOORD2.xy under bit21.
Full packed UVModeFlag0/Type0 lo/hi remain float, including half wrapper.
Other texture UV routes, CustomData, UI, flipbook, geometry/world/screen/cylinder
host modes, time-animation, half-precision and VFX support are unverified.
Other mode words are not silently rewritten; required host inputs still pending.

Regression filter:
NBFX.Baseline.Tests.G4GraphMeshParityTests;NBFX.Baseline.Tests.G4GraphSamplingTests
155/155 pass (88 surface Mesh +4 generated MasterPreview +63 sampling).
All prior capture comparisons in regression/capture-equivalence.json; identical
PNG/raw refer to prior archived bytes instead of duplicating. New Preview source,
metrics, material inputs, XML and logs are stored. Import exits0, no errorCS or
Shader error; assertion-only UV test exit2. Protected settings hashes unchanged.
Full G3/G4 NOT passed. Tiny failures are recorded for final one-time manual test,
not put back into a diagnosis loop. Continue ordinary Mesh implementation.

Material-input JSON and generated Preview source are losslessly gzip-compressed;
original Unity-generated whitespace is preserved inside .gz, not edited.
