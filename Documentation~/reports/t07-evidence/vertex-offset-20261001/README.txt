Ordinary Mesh VertexOffset and main-color HDR correction checkpoint, 2026-10-01.
Product: 64308ec31b32b64983d618823b5cd7e042ed8cc2. Input: 58f2048.

FIXED INPUTS
The exact 8 changed paths and 2 original unchanged GeometryV1/UVV1 snapshots
are from git show 64308ec:<path>, never subsequent working-tree files.
GeometryV1, UVV1, Forward and Shader bytes match input 58f2048. Existing
four-mode geometry arithmetic is reused. No new Frozen ShaderLab feature was
introduced; the 92 Frozen/current Noise regressions are NOT VertexOffset A/B.
Vertex UV/ST/channel/position half-assignment boundaries are preserved.
The vertex and fragment hosts share NBGraphSampling: vertex raw LOD0,
fragment HDR decode retained. Normal and tangent pass through unchanged.

WHAT THE 14 NEW TESTS PROVE
7 configurations x orthographic/perspective; rotated Quad Euler (7,34,3),
nonuniform scale (1.2,1.1,1.7). Four direction modes: custom, normal, vertex RGB,
texture RGB. Selected world-to-object direction conversion, without extra
normalization. Map channel G / mask channel B; UV sources 0/1/2/8, UV0.zw,
UV1/UV2, Twirl, SharedUV, independent map/mask ST and selected Wrap states.
This is NOT an exhaustive channel/Wrap matrix or a curved-Mesh test.
128x128 linear RGBAHalf; full-frame 16,384 pixels, strict RGBA 0.
Raw gzip contains little-endian float32 RGBA, bottom-up. PNG is only a preview.
Four warm-up renders: steady state only, not cold first frame.
On-state B/C and on-state repeats are 0; zero-intensity B/C and mask-off B/C
are 0. Off/mask controls have no separate repeat captures. Independently checked
all actual raw values finite and all captured B/C/repeat pairs exactly equal.
Minimum displaced/off response: 313 changed pixels (original >50 threshold).
Minimum mask response: 288 changed pixels (original >30 threshold).

FINAL AND REGRESSION RESULTS
Final 649: 627 passed / 22 known UV strict failures, no skips, test exit 2.
Vertex 14: all passed. Existing 635: 613 passed / 22 unchanged failure IDs.
All 6542 existing PNG/raw payloads compared exactly to prior N2/Blend archives;
G2/G4 Noise namespaces mapped separately. 6542 identical, 0 unmapped/different.
PNG bytes or decompressed raw float32 bytes are used for equivalence, not PNG
appearance or gzip compression bytes. Only verified equal captures are replaced
by prior-evidence links. Current metrics/material/Preview snapshots retained.
regression/capture-equivalence.json lists 635 directories, per-file payload
SHA256 and actual prior paths. The 13 screen regressions are 11 true B/C plus
2 Graph-only uniform-fallback equivalence checks, NOT 13 true B/C.

INITIAL FAILURE AND PRODUCT FIX
Initial 649: 613 passed / 36 failed, including all 14 new Vertex cases plus
22 existing UV failures. This was NOT a microdifference. Identical authored
RGB (0.9,0.25,0.13) yielded ShaderLab (0.89990234,0.25,0.12988281) but Graph
(0.78710938,0.05087280,0.01531982): main Color lacked HDR metadata and applied
an additional gamma-to-linear conversion in this linear-render setup.
Product fixes Color m_ColorMode 0 -> 1, matching legacy [HDR] _BaseColor.
The 5 pre-HDR source files are preserved losslessly. Graph object comparison
found only that Color field change; vertex/sampler/CF/test bytes are unchanged.
No test-color replacement, fixture simplification, or tolerance relaxation.
6 mask cases failed strict mask-off B/C before saving metrics. Their raw and
XML are preserved; missing metrics are explicitly listed, never synthesized.
Initial and final XML/full Unity/import logs retained without suppressing
warnings. Both test runs exited 2; both import logs completed successfully.
All 4 logs contain no error CS / Shader error.

REPLAY AND BOUNDARIES
Root ran exclusive CLI clone /tmp/NBFXG2DissolveMaskProbe-20260928;
unityMCP was unavailable. Main project not opened; official packages,
UniversalTarget and original NBPostprocess not changed, per Root's record.
The 3 protected on-disk clone setting hashes equal the prior Blend archive.
Archive agent launched no Unity, edited no product/Test/index, and made no
Git mutation or push.

Replay only in an exclusive clone, with a unique empty output directory:
NBFX_MESH_EVIDENCE_DIR=<unique-empty-output>
UNITY_BURST_DISABLE_COMPILATION=1
Unity -batchmode -projectPath <exclusive-clone> -runTests
  -testPlatform EditMode
  -testFilter NBFX.Baseline.Tests.G4GraphVertexOffsetTests
  -testResults <xml> -logFile <log>
Test invocation does not use -nographics or -quit; import separately with
-batchmode -quit. Full 649-fixture arguments are preserved in unity.log.gz.

Only VertexOffset is proved: no VAT, CustomLocalTransform, CustomData,
curved Mesh, other vertex UV modes, true VFX or Player claim. Unsupported UV
sources return Supported=0 and unchanged position, not UV0 substitution;
remaining adapters and GUI diagnostics are still pending, not removed goals.
This slice does not prove other geometry/lighting/depth chains, GUI persistence,
all combinations, full NBPostprocess chain, Player, batcher, variants, performance
or manual HDR material migration. Full G3/G4 NOT complete and NOT released.
D24: ordinary Mesh first; earlier VFX results remain historical exploration.
