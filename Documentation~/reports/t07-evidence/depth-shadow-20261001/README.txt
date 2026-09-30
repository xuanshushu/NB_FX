T07 ordinary Mesh DS0: directed DepthOnly plus real URP main-light ShadowCaster.
Fixed package product/test commit: 999aea335d4e8cdf33a0f4cb99b5d6d7a413f198.
All eight changed DS0 commit paths (six code files plus two .meta files) were extracted read-only with git show COMMIT:PATH,
not copied from the later L0 working tree. product-source/commit.json and
product-source-sha256.txt list those exact bytes. input-source/ and
input-sha256.txt fix the Graph JSON, current ShaderLab and G0 Frozen ShaderLab.

Environment: exclusive isolated clone at /private/tmp/NBFXG2DissolveMaskProbe-20260928;
Unity 6000.3.18f1, URP/Shader Graph/VFX packages 17.3, Metal, RenderGraph.
unityMCP unavailable to the root agent; root used isolated Unity CLI. This
archival step did not run Unity or edit product/Test/official packages.

Replay against commit and matching project dependencies in an isolated clone:
NBFX_MESH_EVIDENCE_DIR=<out> UNITY_BURST_DISABLE_COMPILATION=1 \
Unity -batchmode -projectPath <clone> -runTests -testPlatform EditMode \
-testFilter NBFX.Baseline.Tests.G4GraphDepthShadowTests \
-testResults <results.xml> -logFile <unity.log>
No -quit/-nographics for tests. Force-synchronous Graph import is in the
fixture's OneTimeSetUp. It prevents querying a temporary post-assembly-reload
Graph placeholder before the asset rebuild completes. It does not invent a
DepthOnly/ShadowCaster Pass or change the product.

Final v3: 36/36 strict pass, 0 skipped. 24 directed DepthOnly real RendererList
cases and 12 actual main-light shadow/receiver cases. A=G0 Frozen ShaderLab,
B=current ShaderLab, C=generated Graph. 128x128 full-frame linear RGBAHalf,
lossless raw float32 RGBA gzip plus PNG. Alpha texture halves .25/.75.
Cutoffs .25/.5/.8, base/mask/dissolve/Forward-only, transparent shadow fixed
0.5 and 4x4 dither; ortho/perspective. Pass-off, repeat, cutoff control,
Forward-only invariance and finite-value controls are recorded in every
relevant case. All 36 A/B and B/C full-frame comparisons are exactly 0.
For DepthOnly, all 24 cases recorded 27/27 valid depth frames; test checks
renderer asset bytes on disk unchanged after its temporary in-memory feature.

Historical runs are NOT discarded:
- history/v1/results.xml and unity.log.gz: 0/36 because test called FindPass
  before Graph asset completed import, observing a placeholder. This was a
  fixture timing/ordering failure, not proof the product lacks either Pass.
- history/v2/results.xml and unity.log.gz: 34/36; cutoff=.8 Forward-only
  orthographic/perspective controls failed because fixture did not restore
  cutoff=.8 after the temporary .5 control before the next invariant capture.
  history/v2/cases keeps that run's captured raw/PNG/metrics.
- history/fixtures/v1.cs and v2.cs keep both intermediate fixture versions;
  final fixture is product-source/Tests/URP/Editor/G4GraphDepthShadowTests.cs.
- history/import-v1..v4.log.gz keep all four Graph import logs.
These failures were fixed by fixture ordering/restore and re-run; no tolerance
was widened. Final result only is results.xml and cases/.

Protection: protected-settings-audit.json hashes three protected files in the
exclusive clone and compares them with the prior normal-map archive; all 3
match. This does not claim the present main workspace has those same hashes.
The fixture temporarily alters RendererData features and one runtime
NBPostprocess material flag, restores them in finally, and byte-checks the
renderer asset on disk. No protected asset-file edit is inferred.

Boundaries: directed DepthOnly validates an explicit depth writer and its
occlusion, not URP automatically selecting a depth prepass or depth texture
contents/DepthColor output. Main-light ShadowCaster validates visible receiver
shadow for the tested matrix/camera/light, not all cascades, bias, soft/point/
spot shadow matrices or every scene setup. No VFX, Player, NBPostprocess
end-to-end RT/composition, full Mesh combination, GUI persistence, stripping,
batching or performance claim. G3/G4 remain open; no push.

sha256.txt lists every attachment except itself.
