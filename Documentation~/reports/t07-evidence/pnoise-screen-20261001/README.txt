T07 PN2 ordinary Mesh procedural-noise screen/Forward archive — 2026-10-01

FIXED CODE AND REPLAY
NB_FX product+test commit: 6a7682682fd2c46bead49ce89237dc6106677bf7
Parent: 1dc47170e700490d8218636f89e9fd96c42e5681
Exactly four commit paths (Graph, BaseColor CF, new PN2 test and .meta)
are captured losslessly from git show in product-source/. Existing
ShaderLab, official URP, UniversalTarget, NBPostprocess, renderer/project
settings and packed protocol were not modified in this slice.
Unity 6000.3.18f1 / Metal / URP RenderGraph. Root ran an exclusive CLI
clone since unityMCP was unavailable; this archive agent did not start
Unity, mutate Git/product/tests/index or push. Protected clone settings
were read-only hashed and match earlier evidence.

WHAT THE 20 ISOLATED TESTS MEAN
A is CURRENT ShaderLab with procedural noise OFF, NOT G0 Frozen.
B is the same current ShaderLab with procedural noise ON; C is the
actually generated Graph with procedural noise ON. This is B/C
functional parity and on/off causality, not a Frozen baseline audit.
The old shader nests the procedural distortion blend under texture
Noise; both hosts must leave it inactive when the texture Noise toggle
is off. The Graph routes the same generated noise into Forward masked
surface paths and into the two precisely tagged NB distortion passes.
No standalone new shader keyword, pass label or official package edit.

Final v2 has 20 NUnit cases, 20 strict passes, zero fail/skip:
2 Forward, 9 NBCameraOpaqueDistortPass and 9 NBDeferredDistortPass.
The 128x128 linear RGBAHalf targets compare 48x48 ROI (2,304 pixels)
in raw RGBA. Every B/C primary and NoiseMask-control ROI has STRICT
ZERO difference; A versus Graph procedural-OFF is zero too. Every
captured full frame is finite, B/C repeats are zero, and visible pixels
are at least 1,588. NoiseMask change has nonzero causal response in
both hosts (minimum 0.01220703125); active procedural noise has a
nonzero on/off response, while the original fallback/no-op modes and
wrong/mode0 pass controls remain inactive as specified. Texture Noise
OFF makes procedural distortion OFF in both hosts. 316 PNG and 316
lossless gzip float32 RGBA captures plus 20 metrics JSON are retained.
The tested blend values 4/5 remain the ORIGINAL fallback paths, not
newly implemented procedural algorithms.

Deferred's source RG is signed distortion, while blue carries coverage.
The test divides RG by blue at a visible center pixel before/after
changing NoiseMask to detect accidental double masking. Its 0.001
bound is for a DERIVED quotient after RGBAHalf quantization, NOT a
relaxation of B/C image comparison; actual measured derived deltas are
all zero. Direct B/C frame comparisons remain strict zero.

HISTORICAL V1 IS NOT THE FINAL PRODUCT RESULT
The first fixture produced 17 pass/3 fail. Two Forward cases used
foreground layer 3, which the isolated renderer transparentLayerMask
55 excludes; both original and Graph then rendered zero pixels. V2
reads the mask and chooses allowed layer 4, with no renderer asset edit.
The Opaque blend1 case used strength 0.5 and drew 61 ROI pixels, below
the fixture's fixed >100 visibility guard despite B/C zero; v2 uses
strength 2 and draws 1,588 pixels. V1 JSON note incorrectly calls A
Frozen; A always was current ShaderLab with PN OFF. Its original raw,
JSON/XML, test fixture source and import/test logs are preserved
UNMODIFIED under history/v1, with a correction in history/v1/summary.json.
These were test-fixture issues, not missing Graph passes or a request
for the user to change project settings.

1003-CASE FULL REGRESSION
At the same fixed commit the complete test suite reports 1003 total,
845 strict passes, 158 strict failures, zero skips. All 158 prior SH983
failure identities remain, none new or missing; PN2's 20 are all pass.
The 46 GUI storage tests have no raw frame; there are 957 image capture
directories (937 previous + 20 PN2), not 1003 image tests.

All 13,082 PNG or gzip-decoded float32 RGBA payloads were actually read
and SHA256-compared to the latest durable SH983 mapping (including its
12 changed raw overrides) or PN2 v2 standalone files. All 13,082 are
EXACT matches; no changed capture was hidden or discarded. The PN2
subset contributes 632 captures, not 500. The full XML and Unity log,
1,279 noncapture metadata files (1,183 JSON, 92 shader-message texts,
4 generated Preview shaders) are retained in full/. Warnings exist and
are preserved. After the completed NUnit run, the Unity log records a
background dotnet build-server/SDK-not-found message; it is not an
error CS or Shader error, and it must not be omitted or called proof
of a clean environment. The full NUnit XML completed 1003 results;
root found no C# or Shader compiler error in the test log.

To replay: use an exclusive clone at the fixed commit and force Graph
import. Set NBFX_MESH_EVIDENCE_DIR=<fresh-empty-dir> and
UNITY_BURST_DISABLE_COMPILATION=1; run Unity -batchmode -projectPath
<clone> -runTests -testPlatform EditMode -testFilter
NBFX.Baseline.Tests.G4GraphPNoiseScreenTests -testResults <xml>
-logFile <log>. Do not add -quit/-nographics for the test command.

This is ordinary Mesh only. It does NOT establish Frozen A/B parity in
this PN2 slice, true VFX, Player, all material combinations, all UV or
CustomData routes, full ordinary Mesh completion, variant stripping,
SRP Batcher, performance or Gate G3/G4 release. GF does not replace G3.
