Ordinary Mesh packed sampling protocol, 2026-09-30.
Parent input 6081fae; package before e3dae2e; candidate code d31bdfe.
Unity 6000.3.18f1 / URP+SG+VFX packages 17.3 / Metal / RenderGraph.
No true VFX Output tests. No official package, UniversalTarget, NBPostprocess,
old ShaderLab or main Assets/ProjectSettings modifications.

Replay product d31bdfe + existing Tests/URP/Editor/G4GraphSamplingTests.cs.
Set NBFX_MESH_EVIDENCE_DIR=<output>; UNITY_BURST_DISABLE_COMPILATION=1.
Unity -batchmode -projectPath <exclusive-isolated-clone> -runTests
 -testPlatform EditMode -testFilter NBFX.Baseline.Tests.G4GraphSamplingTests
 -testResults <results.xml> -logFile <unity.log>
Do not add -nographics or -quit. Compiler/import log also retained.
Sampling raw files: 128x128, little-endian float32 RGBA read from linear Half,
row-major bottom-up. Strict comparisons use interior x/y32..95 (4096pixels),
not edges or PNG. Four Render() calls precede capture: steady-state only.
Deterministic source maps/mip bytes are defined by archived test source.
Each case has B/C/control raw and PNGs, repeat and foreground/response metrics.
63 tests: 10 textures x4 wrap + auto/forcedLOD, source-gradient diagnostic,
period31 automatic/forced ramp. All strict ROI RGBA differences/repeats=0.
The source-gradient diagnostic has no required auto-vs-LOD response.

v1 fixture: 53 passed/10 failed solely because checker R average .5 equalled
manual-mip R .5; control delta .00439/.00293 did not meet .005 positive threshold.
Strict B/C differences were zero. Change manual mip to(.9,.1,.2) for a visible
signal, unchanged assertion; v2 63/63 passed. Failure XML/log/metrics/source kept.

Old high-frequency exact replay: archived original fixture, only Root path changed.
It requires Assets/NBShaderSamples/NBShaderSamples/UnLit.mat in the clone.
Copy .cs.txt to Assets/Editor/NBFXRampWrapMipMeshProbe.cs and run batchmode
-quit -executeMethod NBFXRampWrapMipMeshProbe.Run. For before use original Graph
and HLSL from e3dae2e; candidate use d31bdfe. before NUMERIC_MISMATCH/exit1:
period31 autoP95=.0731201/forceP95=.5962524. Candidate seven cases each maxRGB=0,
PASS/exit0, auto-vs-force3904changed pixels in both. A causal test keeps the OLD
Graph and replaces ONLY NBGraphSampleMap's texture-asset sampler with existing
inline linear_repeat (sampler declarations relocated for availability): all seven
maxRGB=0/PASS. Causal HLSL preserved. This confirms source-sampler mismatch for
this reproduced failure on current Metal, not every platform/resource contract.
Test-only causal/probe files removed; candidate restored before final regression.

Surface regression:92/92(88MeshB/C+4MasterPreview), all full-frame RGBA/repeats=0.
Raw data/PNG equivalence against previous surface archive checked; see JSON.
Identical captures are referenced rather than copied again. Changed captures,
if any, and new generated Preview source are stored here. Results/log/metrics
retained for ALL cases. No claim of visible Graph-window interaction/first frame.

Default sampling now follows original NB linear/repeat(autoLOD), not the old
Graph asset sampler.118Graphproperties and slots0..129 unchanged; new130/131
BaseMap/BaseUV appended; old upstream SampleTexture2D removed, one BaseMap sample.
No keyword increase; packed protocol positions unchanged. Generated source has
one base-sample call in CF body and no old upstream SampleTexture2D node.
Actual compiled GPU sampling/variant counts, batching/performance still unmeasured.
No G3/G4 clearance: advancedUV/Noise/lighting/geometry/render-state/fullPass/
NBPostprocess/GUI/Player and platform/version work remain. Protected hashes intact.
