L0 ordinary Mesh lighting archive, 2026-10-01. STRICT FAILURES RETAINED.

FIXED PRODUCT AND RUN
Product commit dc9543b2873b6349697b903669904a5660b8d158, parent
34101d438c61d1882fe2053d8c2454b5189eff60. The six changed product/
test paths are snapshotted with git show at that commit, gzipped losslessly
under product-source/. Input hashes also include current and G0 Frozen
NBShader.shader. Do NOT replace these with current PN1 workspace files.
Unity 6000.3.18f1, URP/ShaderGraph/VFX package 17.3, Metal RenderGraph.
Main Agent ran an exclusive CLI clone because unityMCP was unavailable.
This archive agent did not launch Unity, edit product/Test/index, mutate Git,
or push. Three protected clone settings SHA values match prior archives.

FINAL SINGLE SLICE AND FULL REGRESSION
The FINAL committed test is represented by root results.xml and cases/ from
the 783-test combined run, not historical v6. L0 has 8 cases: Unlit,
BlinnPhong, HalfLambert, PBR x orthographic/perspective. 5 strict pass,
3 strict fail; no skips. All eight Frozen A/current ShaderLab B main frames
have zero RGBA difference in the 40x40 ROI (x/y 28..67). Every full raw frame
is finite, every within-run B/C repeat is zero, and all lit modes have
nonzero main-light, custom SH, additional point-light, and NormalMap-mask
controls. The Unlit controls are intentionally no-light invariance, not
false positive lighting. Existing Forward shadow reception is tested as
STRICT unchanged, not as successful shadow receiving. DS0 ShadowCaster
36/36 is a separate pass check.

The three strict B/C failures are HalfLambert perspective (2 control pixels,
max 0.000244140625), PBR orthographic (19 control pixels, max 0.015625), and
PBR perspective (54 main pixels, max 0.0078125; 170 control pixels, max
0.015625). No tolerance was widened, no failure changed into a pass, and
no root cause is claimed. User directed that low-amplitude color/HDR
differences not be pursued now; they remain for later combined manual review.
The full run is 783 total, 724 strict passed, 59 strict failed, no skips,
exit code 2. Failure IDs exactly equal the prior 22 UV + 34 NormalMap +
this 3 L0; no added/disappeared failure IDs. That ID comparison is not used
as a substitute for raw numerical comparisons.

RAW EQUIVALENCE, NO LOSS OF UNIQUE PAYLOAD
For the 775 pre-existing test invocations in the full run, 773 distinct
capture directories survive. Two fixtures both write back-face-ortho and
back-face-perspective, so their output names collide; XML still records
both tests. Prior independent archives preserve each fixture's original
captures. All 9,058 surviving prior-run PNG files or gzip-decompressed
float32 RGBA raw payloads were compared by SHA256 AND byte length against
actual durable prior archive files. All 9,058 are identical, none unmapped
or different. The exact relative prior path and both payload hashes for
every file are in regression/capture-equivalence.json. Thus duplicate raw/
PNG files are referenced, not copied; the 1,053 full-run metadata files
(metrics, B/C material JSON, shader messages and generated Preview shader)
are retained under regression/cases/. The eight NEW L0 case directories
and their 200 raw captures are in root cases/ without substitution.
The two collided full-run directories are a union/last-writer set, not
independent raw archives for both fixture executions; do not overclaim this.

The earlier v6 standalone 8-test run also had 5 pass/3 strict fail. Its raw,
XML and log remain under history/v6/. Of the 200 comparable final/v6 raw
captures, 196 were payload-identical; four PBR perspective shadow-on B/C
captures and repeats differed across runs by at most 0.0009765625 (B) or
0.000244140625 (C). Each run's own repeats were zero. Both versions remain
archived; no claim of cross-run byte identity is made.

HISTORY, NOT BLOCKERS
history/v1..v3 XML/logs and fixtures/v1..v4 show three EditMode scene
fixture failures before rendering. v4 reached GPU but a mistaken include
exposed undeclared _SixWayInfo; 8 failed (not evidence that final Graph is
broken). v5 had an incorrect receiving-shadow control axis plus RT cleanup
error; 8 failed. v6 fixed render fixture and gave the 5/3 result.
Snapshots of intermediate source candidates v1/v2 and import logs v1..v8
are preserved under history/. Historical failures were repaired in the
fixture/product candidate before final import; they are not user decision
blockers. Final import/test logs show no C# or Shader compile errors.

REPLAY AND SCOPE
In an exclusive clone at the fixed commit and current pinned dependencies,
set NBFX_MESH_EVIDENCE_DIR to a fresh empty directory and
UNITY_BURST_DISABLE_COMPILATION=1. Run Unity -batchmode -projectPath <clone>
-runTests -testPlatform EditMode -testFilter
NBFX.Baseline.Tests.G4GraphLightingTests -testResults <xml> -logFile <log>.
Do not use -quit or -nographics for tests. The full-run command is in the
archived full unity.log.gz. Forced Graph import/Editor readiness is separate.
L0 adds only the package-local Graph additional-light variant axis; old
Forward did not declare receiving-shadow variants. This is NOT SixWay,
vertex SH/Mixed, vertex additional lights, LM/APV, Forward+, every material
combination, true VFX, Player, GUI persistence, variants/stripping, SRP
Batcher, or performance. Complete ordinary Mesh G3/G4 are NOT released.
