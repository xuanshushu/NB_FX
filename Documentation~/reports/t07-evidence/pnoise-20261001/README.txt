PN1 ordinary Mesh procedural-noise archive, 2026-10-01. STRICT FAILURES RETAINED.

FIXED SOURCE AND RUN
Product commit 94e8d7cd3518c225766031d056cd346362199efa, parent
6caac3772752a914a90a23981c72199b69d9a248. The five changed
Graph/UV/CF/test/meta paths come from git show at that commit and are
losslessly gzipped under product-source/. Original NBShader, G0 Frozen,
NBShaderFlags, NBShaderInput and XuanXuan_Utility input hashes are included.
Do not take later PN2/other-slice workspace files as PN1 source.
Unity 6000.3.18f1; URP/ShaderGraph/VFX package 17.3, Metal RenderGraph.
Root ran an exclusive CLI clone because unityMCP was unavailable. Archive
agent did not start Unity, edit product/Test/index, mutate Git or push.
The three protected clone settings hashes match prior archives.

FINAL SINGLE-SLICE RESULT
Final standalone /tmp v3 is root results.xml and cases/ here: 140 total,
47 STRICT pass, 93 STRICT fail, 0 skip, Unity exit 2. The 116 Forward cases
have 23 pass/93 fail. The 12 directed DepthOnly and 12 actual main-light
ShadowCaster cases ALL pass exact A/B and B/C zero for main+control frames.
For Forward, the maximum B/C main/control single-channel delta is
0.00048828125. One A/B Frozen/current Forward case has a nonzero
0.00006103515625 at one pixel: surface-dissolve-both-2-uv0-perspective.
This is also retained as failure evidence, not hidden. No tolerance was
widened or failure treated as a pass. Root reports no final C#/Shader error.

All 140 cases have finite values in every captured full frame; ON-state
A/B/C repeats are exactly zero. Control-state repeats were NOT captured,
so do not claim repeat stability for controls. Forward counterfactuals are
strong: min Graph response 0.601806640625 and min 2,552 ROI pixels changed.
The 32 both-noise BaseBlend direct controls have min Graph response
0.1839599609375 and min 2,448 changed pixels. Four procedural states
OFF/Simplex/Voronoi/both, Mask/Dissolve consumers, orthographic/perspective
cameras, UV0/UV0.zw/UV1/UV2/Twirl/Shared UV route examples are represented.
128x128 linear RGBAHalf, strict x/y32..95 (4,096 pixels) RGBA comparison;
raw float32 RGBA gzip plus PNG saved for all 1,356 captures. Four warm-up
renders are not cold-first-frame proof.

ORIGINAL BLEND PROTOCOL, NOT SIX NEW ALGORITHMS
Packed 3-bit consumer fields were exercised with encoded values 0..5,
but the shared original BlendPNoise defines behaviors only for 0..3.
Values 4 and 5 intentionally fall back to unchanged source. The PN1
Graph preserves those semantics; do NOT advertise six blend algorithms.
Both-noise BaseBlend, Mask and Dissolve are applied at their original
stages. PN2 procedural distortion is not included.

HISTORICAL RUNS
v1: 0/140, a pre-render FindPass name/timing fixture check returned -1.
No metrics or raw were produced; this is not evidence that final Graph
lacks the pass. v2: 24/140, test fixture mistakenly disabled
SRPDefaultUnlit, so 116 Forward cases did not render; 24 Depth/Shadow
cases did pass. Historical XML/log and v2 raw/metrics are kept under
history/. Test fixture v1/v2 source snapshots and all three import logs
are retained. These were fixed fixture states, not user decision blockers.

REPLAY, GATE AND PENDING
Use an exclusive clone at the fixed product commit. Set
NBFX_MESH_EVIDENCE_DIR=<fresh-empty-output> and
UNITY_BURST_DISABLE_COMPILATION=1, then Unity -batchmode -projectPath
<clone> -runTests -testPlatform EditMode -testFilter
NBFX.Baseline.Tests.G4GraphPNoiseTests -testResults <xml> -logFile <log>.
Do not use -quit or -nographics for tests; force Graph import and check
Editor readiness separately. Exact original command/log preserved here.
PN1 does NOT prove procedural Distort PN2, wrap variants, CustomData
offsets, UV modes3..7/position/screen/cylinder inputs, true VFX, Player,
full Mesh combinations, GUI persistence, variants, SRP Batcher or
performance. Complete ordinary Mesh G3/G4 NOT released; no push.

FINAL FULL 923 REGRESSION AND LOSSLESS DE-DUPLICATION
After the PN1 slice, Root reran all 923 EditMode cases at the fixed PN1
product: 771 strict pass / 152 strict fail / 0 skip, exit 2. The 152
failure IDs are exactly the previous 59 (UV22 + NormalMap34 + L0 3)
plus the PN1 standalone 93. No added or disappeared failures. Final
full import and test logs have no C# or Shader compile errors.
This name comparison is supplemented by actual raw checks, not used alone.

The namespace-corrected run has 923 distinct capture directories:
g4-matcap and g4-normalmap prevent the two earlier back-face name
collisions and preserve each source independently. All 9,258 captures
from the earlier 783-case L0 run were compared to durable prior evidence
(the MatCap/NormalMap pairs to their independent M0/N0 archives). All
9,258 matched exact payload SHA256 and byte length. All 2,712 PN1
captures matched the 140-case standalone PN1 archive the same way.
Total 11,970 compared, 11,970 identical, zero changed/unmapped. PNG
bytes or gzip-DECOMPRESSED raw float32 RGBA bytes are compared, not visual
similarity or compressed gzip header bytes. full/capture-equivalence.json
lists every current payload SHA, candidate prior SHA and actual relative
prior archive path. No 139 MB duplicate raw/PNG tree is copied here;
all 1,203 full-run metrics, B/C material JSON, generated Preview sources
and shader-messages are retained under full/cases/. If a capture had
differed, it would be in full/changed-captures; there were none.
Full XML, test log and import log are under full/. This confirms the
known strict failures remain, not that PN1 or full Mesh has passed G3/G4.
