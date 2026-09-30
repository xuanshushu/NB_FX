N1 ordinary Mesh texture Noise consumer slice, 2026-10-01.
Parent input b2c3972 (gitlink3298834); package input90d7bc6; product0c95b39.
Unity6000.3.18f1/URP,SG,VFX17.3/Metal/RenderGraph;unityMCP unavailable.
Exclusive CLI clone; no main project opened, official/Target/NBPostprocess/settings changes.
Only ordinary Mesh tests; SupportVFXtrue import is not a VFX behavior test.

Set NBFX_MESH_EVIDENCE_DIR=<output>, UNITY_BURST_DISABLE_COMPILATION=1.
Unity -batchmode -projectPath <exclusiveclone> -runTests -testPlatform EditMode
-testFilter NBFX.Baseline.Tests.G4GraphTextureNoiseTests -testResults <xml> -logFile <log>.
No -nographics/-quit on test run; import separately with -batchmode -quit.
179/179 strict pass, no skips. 144=6texture consumers*24 numeric states,
5combos,5nonuniform/ST/rotation,12independent Noise/Mask Wrap/LOD states,
10independentNoise/Mask UV modes1/2/8,3nonconsumers(Mask2/3/ColorRamp).
Fixed strict ROI x/y32..95=4096pixels at128x128 linearRGBAHalf, raw little-endian
float32RGBA(bottom-up), not fullframe/PNG. All finite fullframe and repeats0.
Consumer B/C and counterfactual B/C RGB/Alpha0; min responding control delta
0.0223388671875 (>0.01 threshold), >64 responding pixels. Zero-intensity/mask
cases use nonzero counterfactual controls, not a blank falsely positive test.
Nonconsumers remain Noise on/off0 and have featureST positive controls.
Four warm-up Render calls before capture=steady-state, not cold first frame.
Noise source rotation->halfST->scroll; mask source owns halfST with no rotation.
SharedDecode keeps half boundaries, original normalize flag, RG*Direction*Intensity
then mask(A*selected mask channel). NoiseMap bits3/19,Mask12/28;NoMip9/10.
Base/Emission/Dissolve/DissolveMask/Overlay2/Mask1 consume respective original
intensity. Mask2/3/Ramp do not. 11properties use original names, no new keyword.
Screen RT Noise remains old uniform prototype, NOT transported by N1.
CustomData/PNoise/Refraction/chroma/time animation/perspective/halfGraph/VFX pending.

395 regression:surface+Preview92,sampling63,mainUV22,featureUV96,FrozenNoise92,
renderstate30. 373pass/22same known strict micro-failures(mainUV3+featureUV19).
No tolerance adjustment or micro-diagnosis. Raw/PNG payload equivalence vs all
six prior evidence dirs is recorded, identical captures referenced not duplicated.
Allnewmetrics/material/generatedPreview snapshots retained losslesslygzip.
N1testexit0;regressionexit2 corresponds to retained22 assertions;importexit0;
no errorCS/Shader errors. Protectedsettings unchanged. FullG3/G4NOTpassed.
Only local rollback commits, no push. Continue ordinary Mesh N2 and remaining gaps.
