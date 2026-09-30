L1 ordinary Mesh SixWay archive, 2026-10-01. STRICT FAILURES RETAINED.

FIXED SOURCE AND EDITOR RUN
Product commit becceb907aab53e77081151de785755310ac398f, parent
67bb8849763d178035f9f4e232c94eb86a9e146a. The seven changed
GUI/SubTarget/CF/Graph/helper/test/meta files are losslessly gzipped
under product-source/ using git show at this exact commit. Current and G0
Frozen NBShader plus old Forward/Input/Flags hashes are also recorded.
Do NOT replace these bytes with later workspace SixWay or PN2 candidates.
Unity 6000.3.18f1; URP/ShaderGraph/VFX package 17.3, Metal RenderGraph.
Root ran Unity CLI in an exclusive clone because unityMCP was unavailable.
This archive agent did not start Unity, edit product/Test/index, mutate Git,
or push. Three protected clone setting hashes match prior archives.

FINAL STANDALONE MATRIX
The test class has TWO NUnit test cases: orthographic and perspective.
Each contains 15 rendered rows. BOTH NUnit cases still FAIL strict zero
because three rows have one differing pixel each. Do not label the
27 exact-zero rows as 27 passing NUnit tests. Among 30 rendered rows:
27 exact B/C zero, 3 strict B/C fail, no skips. Failures are:
- orthographic MainTex-ST: 1 pixel, maximum 0.00048828125;
- orthographic rig-LOD0: 1 pixel, maximum 0.00048828125;
- perspective rig-LOD0: 1 pixel, maximum 0.00048828125.
No tolerance was widened or strict failure reclassified. All 30 Frozen
A/current ShaderLab B frames are exact zero; every captured full frame is
finite; all A/B/C within-row repeat captures exact zero. Fourteen direct
causality controls change at least 939 ROI pixels for BOTH B and C, so
ordinary SixWay responds strongly and is not a blank-image parity test.
96x96 linear RGBAHalf with strict x/y26..69 (1,936 pixels) RGBA region;
raw float32 RGBA gzip is preserved for each A/B/C and repeat frame.
The six directional lights are individually exercised (+/- X/Y/Z), plus
baseline, SH-blue, positive rig Alpha, absorption, emission ramp, MainTex
ST, Clamp, rig LOD0 and negative scale. Two distinct rig textures are
used. Separate Ramp LOD0 and flipbook blend UV are NOT covered.

REFLECTED SCALE BUG FIXED IN THE PACKAGE
Historical v3 had two REAL negative-scale mismatches: 1,780 pixels with
max0.02734375 orthographic; 1,340 pixels with max0.033203125 perspective.
This was not a harmless one-pixel color delta. Shader Graph constructs
its vertex bitangent with reflected-scale sign, while the legacy NBShader
uses a different tangent-sign path. The package-local Graph vertex adapter
now recovers the old tangent handedness using GetOddNegativeScale, keeps
the old half-precision tangent boundary, and rebuilds the same six baked
SH directions. In final v4, both negative-scale rows are STRICT zero.
No official URP package, UniversalTarget or NBPostprocess was changed.

HISTORY IS NOT FINAL STATUS
v1 rendered only 26 rows before early assertion stop; 24 exact, 2 tiny
strict failures. The v2 fixture accidentally used NUnit Assert.Multiple,
which this project version lacks: import logged CS0117 and produced NO
valid rendered test result. This is a fixture API error, not evidence of
a product blocker. v3 collected all 30 rows: 25 exact, 5 strict failed;
three were one-pixel deltas and two were the real negative-scale issue.
All v1/v3 raw, metrics, XML and logs, fixture source v1/v2, pre-fix CF
source and imports v1..v4 are retained under history/.

REPLAY AND LIMITS
Use an exclusive clone at becceb9 and pinned dependencies. Set
NBFX_MESH_EVIDENCE_DIR=<fresh-empty-dir> and
UNITY_BURST_DISABLE_COMPILATION=1, then Unity -batchmode -projectPath
<clone> -runTests -testPlatform EditMode -testFilter
NBFX.Baseline.Tests.G4GraphSixWayTests -testResults <xml> -logFile <log>.
Do not use -quit or -nographics for tests; force Graph import separately.
The final v4 import/test logs have no CS or Shader compilation errors.
This slice is only ordinary Mesh SixWay vertex SH/direct/emission/alpha.
No complete flipbook blending, vertex additional lights, Forward+,
separate emission Ramp LOD0, VFX, Player, all material combinations,
GUI persistence beyond tested material state, variant/stripping,
SRP Batcher or performance proof. Ordinary Mesh G3/G4 NOT released.

FULL 925-TEST REGRESSION AT THE SAME FIXED COMMIT
The separate full-suite NUnit XML reports 925 total, 771 passed, 154
failed, zero skipped. This is exactly the previous PN1 suite's 923 tests
(771 passed/152 failed) plus the two new SixWay NUnit failures; all 152
previous failing test identities remain, with no additional or missing
old failure. This identity audit is recorded in full/failure-id-audit.json.
The full log has no C# or Shader compilation error; its NUnit exception
stacks are assertion failures, not a product compile failure.

This archive reads and checks every actual full-suite capture payload,
not merely the test names or metrics: 12,150 PNG or gzip-decoded float32
RGBA payloads across 925 capture directories. 11,970 old captures are
compared to the durable PN1 full mapping (including separate MatCap and
NormalMap namespaces), and all 180 new SixWay captures to the standalone
v4 files. 12,146 payloads are exactly identical. Four old PN1 Voronoi
raw files, in two cases, differ across runs: the C image and C-repeat
image for each. In the first case one pixel at (61,22) differs by at
most 0.00000762939453125; in the second one at (95,30) by at most
0.0001220703125. Both lie OUTSIDE the 32..95 strict ROI in the 128x128
frame (first y22; second y30). Within this full run, C and C-repeat
are exact; case metrics match the old PN1 run. All four differing raw
files are separately retained in full/changed-captures/, and none is
mislabelled byte-identical. See full/capture-equivalence.json and
full/changed-capture-audit.json. Noncapture metrics/material/Preview
source files, XML, and logs are retained under full/.

Four generated Preview shaders in full/cases/ contain emitted vertex
NBGraphSixWayBake calls, VertexDescription SixBake assignments and the
generated interpolator pass-through/packing. Exact line locations and
source hashes are in full/generated-preview-locations.json. This static
source is corroborating wiring evidence, not VFX behavior or Player
proof. Neither 925-suite completion nor the 27 zero SixWay rows removes
the 154 strict NUnit failures or releases G3/G4.
