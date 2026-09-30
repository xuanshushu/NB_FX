Ordinary Mesh package SubTarget ColorMask/Stencil slice,2026-10-01.
Parent inputb2c3972;package input3298834;product90d7bc6.
Unity6000.3.18f1/URP,SG,VFX17.3/Metal/RenderGraph;unityMCPunavailable.
ShaderGraph product in these tests still featureUV9e2455d;Graph Noise candidate
not copied until after these tests. ShaderLab includes sharedNoise99e9207.
No official/UniversalTarget/NBPostprocess/main Assets/ProjectSettings changes.

Set NBFX_MESH_EVIDENCE_DIR=<output>,UNITY_BURST_DISABLE_COMPILATION=1.
Unity -batchmode -projectPath <exclusiveclone> -runTests -testPlatform EditMode
-testFilter NBFX.Baseline.Tests.G4GraphRenderStateTests -testResults <xml> -logFile <log>.
Do not use -nographics/-quit on tests. Fixture force-reimports actual Graph at setup.
30cases:ColorMask0/1/2/4/8/3/5/10/15 times ortho/perspective=18;
StencilAlways/Never/Equal/NotEqual/ReadHigh/WriteLow times both cameras=12.
All30strictpass,no skips. AllROI RGBA/repeat0,finite. Physical targetFormat
R16G16B16A16_SFloat/D32_SFloat_S8_UInt retained in every stencil metric.
Tests choose supported D24S8 orD32S8;no supported8bitStencil means Ignore,
not pass. Actual Metal used D32S8,so no need for manual stencil verification.
Raw128x128 linearHalf readas little-endian float32RGBA,bottom-up;strictROI
x/y32..95=4096pixels,not full-frameorPNG. Empty/B/C captures stored;4warm-up
renders=steady-state. Fixture creates in-memory background/writer/probe shaders,
materials,PreviewScene;no asset saves. Consumerwhite has RGBA distinct fromboard.
Numeric ShaderLabColorMaskR8/G4/B2/A1 verified,not RGBA1/2/4/8.

V1:24pass/6fail,ALL strictB/C0;Equal/NotEqual/ReadHigh image-left checks failed:
cameraYaw180 puts worldX-.5 stencil writer on the IMAGE RIGHT. Correct only
fixturewriterXto+.5,unchangedexpectedimageleft/control thresholds;V2all30pass.
V1 originalsource/XML/log/raw retained. Pre-run source checks also corrected
numericColorMask channel order and supportedDepthStencil selection;no product fix.

Product adds8hidden state-only Float properties with old names/defaults using
currentURP AddFloatProperty defaultDoNotDeclare. Clones built-in RenderState
collections before adding;original builtin globalstates not mutated.
Forward+NBtwoDistort pass use materialColorMask+Stencil;relevantDepth/Shadow/
DepthNormals/GBuffer/Motion passes gainStencil,retain their own ColorMask.
Only normalForward physically rendered here;Depth/Shadow/Distort RT/advanced
StencilOps/Zfail/Fail/Portal,GUI,Player,performance,defaultregression separate.
NoVFXclaim. Import/testexit0,no errorCS/Shadererror;protectedhashesunchanged.
FullG3/G4NOTpassed;local rollback slice/no push.
