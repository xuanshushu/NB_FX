T07 SH-M0 ordinary Mesh SH evaluation evidence — 2026-10-01

FIXED INPUT
Package commit: 51d64f4577aaa82e72be52992f345b6008a9a0d2
Parent: 37037808920cac81dcac09abec521000a12df820
Only two package files changed: NBGraphBaseColor.hlsl and the existing
G4GraphLightingTests.cs. The two sources are losslessly stored from
`git show` under product-source/ and audited by product-source/commit.json.
Unity 6000.3.18f1; Metal; URP RenderGraph. Root used an exclusive Unity
CLI clone because unityMCP was unavailable. This archive agent did not
run Unity, edit product/Test/Git, change protected settings or push.

HUMAN-READABLE RESULT
SH is ambient/environment lighting from many directions. The original
NBShader offers two explicit evaluation keywords, EVALUATE_SH_VERTEX and
EVALUATE_SH_MIXED. The Graph now uses the existing SixBack2 vertex data
for the same vertex SampleSHVertex result and supplies it to URP's
SampleSHPixel in the fragment stage. No new Graph port, keyword, Graph
selection GUI, official URP source or Target is needed for this slice.
The test activates the existing keywords on materials directly; UI
selection and persistence remain pending.

The final 20-case isolated matrix has 13 strict-zero NUnit passes and
7 strict failures, no skips. The original eight L0 cases still have
5 passes/3 failures with unchanged inputs. The 12 new SH variants have
8 passes/4 failures; all four new failures are PBR. All 20 Frozen A /
current ShaderLab B primary pairs are exact zero. Full frames are
finite. Every B/C main/control repeated capture in the same run is
exact zero. The new cases positively respond to main light, SH-probe,
normal-mask and pixel additional-light changes in both B and C.
This is real feature connection, NOT 20/20 parity or Gate release.

New PBR strict failures: orthographic vertex/mixed have only controlled
frame differences (27/28 ROI pixels, max 0.015625); perspective vertex
has 53 main pixels (max 0.015625) and 163 control pixels (max 0.03125);
perspective mixed has 50 main and 154 control pixels (max 0.03125).
These HDR differences remain strict failures; no tolerance was widened,
and no assertion that they are visually invisible is made. The three
old L0 failures also remain. See results.xml and summary.json.

IMPORTANT SHADOW-CONTROL LIMIT
The old Forward shader declares no receiving-shadow keyword axis, but
strict shadow-on invariance is FALSE in this isolated 20-case run for
the original PBR perspective case and both new PBR perspective cases.
Their metrics and raw frames
are retained. Therefore do not say that all negative shadow controls
passed, nor infer that Graph receiving shadows were verified. This
observation is not being hidden behind the earlier B/C assertion.

WHY V1 IS HISTORY
The first fixture yielded 20=8 passed/12 failed: old L0 5/3 and new
SH 3/9. Some vertex rows failed the fixture's fixed response threshold
although old ShaderLab B and Graph C showed the same nonzero response
(~0.009/~0.004). For v2, only new SH cases received a brighter test
sun (1.4 to 4.2) and extra ambient probe terms. Original eight inputs
and every strict pixel parity/positive-control threshold were kept.
The earlier XML, all 520 case files, fixture source and import/test
logs remain in history/v1. Do not treat the v1 fixture sensitivity as
a user decision or a missing Shader Graph feature.

REPLAY AND LIMITS
At the fixed commit, force Graph import in an exclusive clone. Set
NBFX_MESH_EVIDENCE_DIR=<fresh-empty-dir> and
UNITY_BURST_DISABLE_COMPILATION=1; run Unity -batchmode -projectPath
<clone> -runTests -testPlatform EditMode -testFilter
NBFX.Baseline.Tests.G4GraphLightingTests -testResults <xml> -logFile <log>.
Do not use -quit or -nographics for tests. The final import/test logs
have no C# or Shader compilation errors. Three protected clone setting
hashes in protected-settings-audit.json match prior evidence.

This is only ordinary Mesh lighting SH variant comparison: no GUI
keyword-selection/persistence, lightmap/APV, additional VERTEX lights,
curved Mesh, Forward+, VFX, Player, full material combinatorics,
stripping, SRP Batcher, performance or cold first-frame claim. The
G3/G4 are NOT released.

COMBINED REGRESSION ADDED AFTER THE ISOLATED RUN
The same fixed SH product commit, including the earlier 033b29f GUI
storage backend, was subsequently tested in a 983-case full suite:
825 strict passes, 158 strict failures, zero skips. Exactly the earlier
925 case failures (154) remain plus the four new SH PBR failures; no
old failure identity appeared or disappeared. This is 925 prior image
NUnit cases + 12 new SH image NUnit cases + 46 GUI storage NUnit cases.
The 46 GUI tests passed and have NO raw image capture, so do not claim
that 983 cases have raw frames. There are 937 image capture directories.
The test log has no C# or Shader compilation error, but shader warning
messages are present and preserved; do not call this warning-free.

All 12,450 full-run PNG or gzip-decoded float32 RGBA payloads were
actually read and SHA256-compared to the latest durable SixWay full
archive (including its four changed PN1 files) or SH v2 single archive.
12,438 are exactly identical; 12 raw files differ and are individually
retained in full/changed-captures/. These are the B/C shadow-on frames
and corresponding repeat frames in three PBR perspective cases. Per
unique frame, at most three full-frame pixels differ across runs; some
are INSIDE the strict ROI and the largest channel delta is 0.00390625.
The current-run shadow-on frame and its repeat are exact; however, the
three legacyForwardShadowInvariant metrics switch from FALSE in the
isolated run to TRUE in the 983-case run, and two controlBCDiff counts
shift. This is not a stable shadow-invariance proof, nor a reason to
hide the isolated result. Full raw differences, metric changes, source
references and hashes are in full/changed-capture-audit.json and
full/capture-equivalence.json. Every noncapture metrics/material JSON,
shader-messages.txt, generated Preview shader, XML and log is retained
under full/. Full metadata counts are in full/metadata-audit.json.

The 983-case full run is a regression check, NOT G3/G4 clearance,
VFX/Player behavior, a GUI keyword-selection UI, or evidence of stable
shadow receiving/invariance. Earlier isolated 20-case facts remain
historical observations, not overwritten by full-run differences.
