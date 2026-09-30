N2 ordinary Mesh screen Noise checkpoint, 2026-10-01.
Product fixed to e2bdae5abe4ee5231cab670cc8ccdae9dc4f0e84, parent db1e304.
All ten product-source paths come from git show e2bdae5:<path>, NOT future
working-tree files. input-sha256 hashes decompressed committed source bytes.
Unity6000.3.18f1 / URP,SG,VFX packages17.3 / Metal / RenderGraph. Root used
exclusive CLI clone /tmp/NBFXG2DissolveMaskProbe-20260928; unityMCP was unavailable.
This archive task launched no Unity, edited no product/tests/index, and did no
Git write. D24: ordinary Mesh complete parity FIRST; prior VFX = exploration.

Human conclusion: Graph texture Noise now feeds both exact NB distortion Passes.
8 ShaderLab B / Graph C cases (2passes*4states) strict ROI RGBA0 on AND off.
2 Noise-off uniform fallback cases are Graph-only: old NB has no same uniform
path. Actual on-state B/C difference is retained, NOT counted as B/C0. Those
2 prove exact equality to analytically equivalent sampled Graph texture input.
Do NOT say all10are B/C0 or complete NBPostprocess has passed.

The shared SurfaceDescription evaluation exports unmasked signedRG and a
separate noiseMask. NB payload applies mask once to coverage, not twice to RG.
Deferred test camera RT over black recovers signedRG=R/B,G/B and source
coverage*intensity from B. CameraOpaque samples a nonuniform actual URP opaque
texture using the same payload. This is a temporary exact-tag RendererList;
NBPostprocess feature is disabled during each case and restored afterward.
This is NOT the full real deferred RT/Controller/Manager/final composite chain.
128x128 linearRGBAHalf; strict 48x48ROI(x/y40..87). Capture raw gzip holds
little-endianfloat32RGBA bottom-up. Four warm-up renders -> steady state only.
Repeat strictROI0; fullframefinite and fullframe mode0/wrong-mode no-write0.
Graph response minimum.00927734375>.005 unchanged threshold. Uniform fallback
sampled equivalence0. Noise uses constant1x1 maps, zero speeds, identity ST;
NO boundary Wrap/LOD proof from this screen fixture. No VFX, Player, perspective,
full pipeline chain, material migration/GUI/batcher/performance coverage.

584 final XML=562passed/22same known strict UV failures(no skips), exit2.
All10screen passed. Other fixture totals:surface+Preview92,sampling63,mainUV22,
featureUV96,FrozenNoise92,renderstate30,GraphNoise179. The two Noise classes
used duplicate case dirs in this first combined run:92 names overlap; their
metrics/raw files were partly overwritten. Original in-memory assertions/XML
remain valid, but mixed original raw is NOT independently complete evidence.
They are excluded from regression/cases and listed/hashes in original-collision-audit.

Output namespace ONLY changed for G2/G4 Noise. Corrected 271-run=92+179 ALLPASS,
exit0. cases are mapped under g2-texture-noise/g4-texture-noise. Each captured
PNG byte sequence / gzip-decompressed raw payload was compared with the right
prior archive, never matched between different fixture classes. Identical
payloads are referenced by capture-equivalence rather than duplicated; current
metrics and nonidentical captures are retained. See exact measured counts in
summary.json. Other303regressioncase payloads checked independently vs five
prior archives. No tolerance adjustment or re-diagnosis of22UVmicrodifferences.

Fixture history (available named snapshots preserved, not renamed to imply
an unrecorded version): initial import CS1061 Shader.FindPass invalid API;
corrected Material.FindPass. Initial10failed premature pass index check -1;
forced reimport after block assembly reload and actual pass/tag check after
steady warm-up. Next5failed camera-opaque background uniform due to back-face
culling; test-only background CullOff. Next1failed response .00463867188<=.005;
test-only backdrop R/G gradient span .6 ->1.2 gave stronger visible signal.
The .005 control and strict RGBA0 assertions were NOT relaxed. Imports and
original XML/logs, v2/v3/v4 source snapshots, failedcase captures all retained.
No synthetic missing v1 source snapshot is claimed.

Replay: set NBFX_MESH_EVIDENCE_DIR to an empty unique directory and
UNITY_BURST_DISABLE_COMPILATION=1; use Unity -batchmode -projectPath <clone>
-runTests -testPlatform EditMode -testFilter <fixture-list> -testResults <xml>
-logFile <log>. Test runs omit -nographics and -quit; import separately with
-batchmode -quit. Full fixture list is in unity.log.gz command line; focused
screen filter NBFX.Baseline.Tests.G4GraphScreenNoiseTests. Corrected Noise
filter G2TextureNoiseExtractionTests;G4GraphTextureNoiseTests under the same
NBFX.Baseline.Tests namespace. Logs retain shutdown warnings, not hidden.
Three protected clone-on-disk settings hashes match previous N1 exactly;
see protected-settings-audit.json. No full G3/G4 release, true VFX/Player or
complete postprocess-chain claim. Root owns gate review/local rollback commits;
no push is authorized.
