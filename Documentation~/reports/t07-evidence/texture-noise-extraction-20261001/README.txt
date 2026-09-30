Ordinary Mesh N1 texture Noise numeric extraction,2026-10-01.
Parent input1181639; package inpute3dd725; product99e9207.
Unity6000.3.18f1/URP,SG,VFX17.3/Metal/RenderGraph;unityMCP unavailable.
No Graph Noise integration,screen RT,Refraction/PNoise/CustomData or VFX claim.
Only3original half assignments extracted:RG assignment,optional RG*2-1,A weight.
All other host operations/order untouched;Frozen baseline unchanged.
No official package,UniversalTarget,NBPostprocess or main project setting edits.

Replay92cases with product99e9207 and its unchanged G0 Frozen assets.
NBFX_MESH_EVIDENCE_DIR=<output> UNITY_BURST_DISABLE_COMPILATION=1 Unity
-batchmode -projectPath <exclusive-clone> -runTests -testPlatform EditMode
-testFilter NBFX.Baseline.Tests.G2TextureNoiseExtractionTests
-testResults <xml> -logFile <log>;no -nographics or -quit on tests.
4consumers(Base/Emission/Dissolve/Mask1)*23states,unsigned/signed RG/0/1,
A0/.375/1,directionXY/negative/zero,intensity0/fraction/1,NoiseMaskRGBA/0,
combinedAlphaMask,noiseOff. ConstantRGBAHalfNoise and distinctgradient consumer.
Ortho only,four renders beforecapture=steady-state not cold first frame.

AFrozen/Bcurrent,BOTH controlled configuration from test source;6captures percase
includingA/B repeats+parameter-controlA/B. LinearRGBAHalf128x128 read as little-
endian float32RGBA,bottom-up. StrictROI x/y32..95=4096pixels,allRGB/Alpha/control
max0,repeat0;full-frame finite. Positive-control delta minimum.03515625 and
4096changedROI pixels everycase. threshold.01 retained,never relaxed.
All92pass,test/importexit0,no errorCS or Shader error. Protectedhashesunchanged.
GraphUV product9e2455d remained in clone,not the concurrently authored GraphNoise
orStencil candidate. No Inspector synchronization/material/asset persistence proof.
CompleteG2/G3/G4stillNOTpassed;local rollback slice only,no push.
