Ordinary Mesh feature-source UV route slice; tests23:59 Sep30+regression00:02 Oct01.
Parent input55697fe; package inputd8e772b; product9e2455d.
Unity6000.3.18f1/URP,SG,VFX17.3/Metal/RenderGraph; unityMCP unavailable.
Exclusive clone CLI only; no main project opening or VFX tests.
Original ShaderLab, official packages, Target/NBPostprocess unchanged in this slice.

Set NBFX_MESH_EVIDENCE_DIR=<output>, UNITY_BURST_DISABLE_COMPILATION=1.
Unity -batchmode -projectPath <clone> -runTests -testPlatform EditMode
-testFilter NBFX.Baseline.Tests.G4GraphFeatureUVTests -testResults <xml> -logFile <log>.
No -nographics or -quit on test run. Compile/import logs retained separately.
96 cases=8 features*6 routes*ortho/perspective. Strict77pass/19fail, no skip.
13 cases have on-state tiny differences;6 more fail only strict off-state.
On-state maxRGBA0.0009765625;off max0.00048828125;at most10/2304 ROI pixels
nonzero in on-state. All finite/visible/repeat0 and functional controls hold.
No tolerance adjustment or tiny-difference diagnosis; retained for final manual test.

Eight source outputs:Mask1/2/3,Overlay1,Dissolve,DissolveMask,Overlay2,ColorRamp.
Use original packed positions2/4/6/12/14/16/18/26 from the SAME BaseUVs.
Each texture owns rotation/ST/scroll after source selection. Do not inherit
MainTex's ST or route DissolveRamp as generic UV. Mode0/1/2/8 bounded only.
Per-feature nonidentityST;17degree rotation only specialUV2;scroll speeds zero.
SharedUV2 uses its ownST and29degree rotation. Explicit unique float4UV0/1/2,
nonuniform linear map. CustomData/position/screen/cylinder/Noise/VFX untested.

Captures128x128 linearRGBAHalf read as little-endian float32RGBA, bottom-up.
Strict ROI x/y40..87=48*48=2304, not full image or PNG;raw/PNG on/off retained.
Four Render calls before capture=steady-state, not cold first frame.
All deterministic scene/material/texture inputs are in archived fixture source.
Sources snapshotted from product9e2455d, BEFORE subsequent Noise helper changes.

Regression filter:G4GraphMeshParityTests;G4GraphSamplingTests;G4GraphAdvancedUVTests
(use fully qualified NBFX.Baseline.Tests prefixes).
177cases:174pass/3known MainTex micro-failures, same22MainTexUV metrics as before.
Other155surface/Preview/sampling pass. capture-equivalence.json compares actual
PNG/raw payload against prior archives;identical ones reference earlier bytes.
All new metrics/material snapshots/generatedPreview source retained; large material
JSON+generated source losslesslygzip. Source generated whitespace preserved.
No errorCS/Shader errors in logs;testexit2 means recorded assertion failures.
Protectedsettingsunchanged. FullG3/G4NOTpassed;continue ordinary Mesh, no push.
