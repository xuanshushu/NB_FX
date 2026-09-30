GUI-0 ordinary Mesh shared-storage checkpoint, 2026-10-01.
Fixed product commit: 033b29f94994c0fd8b8ca25a0999c96a5427d1b6; parent: becceb907aab53e77081151de785755310ac398f.

46/46 actual executed tests passed, no failed/skipped/inconclusive, test log exit0.
Unity 6000.3.18f1 / current URP/SG17.3, Metal Apple M3 Pro.
Root exclusively ran CLI clone /tmp/NBFXG2DissolveMaskProbe-20260928.
unityMCP was unavailable to Root; archive agent did not launch Unity.

This commit changes exactly four existing C# files plus Test/meta. Product-source
and seven unchanged inputs are fixed git-show bytes, gzip losslessly verified.
Original ShaderLab/CBUFFER/HLSL/Graph/asmdef/AssemblyInfo not changed by GUI0.
No later Root dirty Graph/SH source is used as fixed evidence.

Eight original logical words: flags0, flags1, wrap, channel, PNoise slot7,
NoMip, UVModeFlag0, UVModeFlagType0. Graph Material uses its already-existing
lo/hi16 float pairs; bit31 and unrelated bits preserved. Default old Material
and all MPB writes stay integer IDs. MPB is deliberately legacy even with a
Graph Material owner; this is NOT GraphMPB support. Missing/unknown word and
nullMaterial semantics remain old-path; no shadow integer fields or keywords.
UV9 source values/PNoise0..5 are STORAGE coverage, not new rendering/UI support.
PNoise enum named0..3; 4/5 tests only original field storage/default fallback.

Finite halfwords clamp then round. Original fractional/raw fields are not
rewritten by reads; explicit writes encode canonical halfwords. Actual shared
HLSL Mesh unit probe checks 32 finite pairs incl half-way ties and bit31.
1x1 linear ARGBFloat/RGBAFloat, CommandBuffer.DrawMesh quad, actual GPU not skip.
First readbacks are logged 32 times in XML and log. JSON is derived from those
same records, independently compared to CPU word channels. Identical input is
rendered twice and strict repeat finite equality asserted IN MEMORY. No PNG or
raw-readback file was produced; do not claim 64 archived readbacks, full Graph
surface rendering, Frozen/current A/B or ShaderLab/Graph B/C parity.
NaN conversion undefined in old helper and +/-infinity not tested.

Actual shared Gradient Count uses original Int path or Float for Graph; 2..6
keys, packed counts/reset/boundaries tested. Optional vector ST reuses same
TextureScaleOffsetItem; original default nativeST and no MatCapST remain.
Caller-owned multi-material storage Undo/Redo is tested. Whole shared GUI
callback/seed Undo, persistence, visible GUI, Tier, effective Graph projection,
pass/keyword integration and GraphRoot routing are NOT proved in GUI0.
TextureItem optional constructor argument is source-compatible but changes CLR
signature; precompiled callers must rebuild, compatibility later per D21.

Historical preparation README/rsp/logs retained compressed, clearly pre-Unity.
Offline exit0 is not Unity evidence. DLL/PDB are not products here.
Root integration+actual XML is current evidence; prior candidate wording is
not rewritten. All warnings/network/licensing/shutdown messages kept verbatim.
No error CS or Shader error in either actual import/test log; import success
and test code0 explicitly logged. These are not general 'no warnings' claims.

Replay only after checking out fixed commit in an EXCLUSIVE clone containing
current project inputs/packages, not user's main project. No product editing:
Unity -batchmode -quit -projectPath <clone> -logFile <import-log>
Unity -batchmode -projectPath <clone> -runTests -testPlatform EditMode
 -testFilter NBFX.Baseline.Tests.G4GraphGuiStorageTests
 -testResults <unique-xml> -logFile <unique-log>
Do not use -nographics (real GPU probe), test invocation does not use -quit.
Exact original CLI is retained in each log. Root's serial execution ownership
is mandatory. Three clone protected hashes match prior Vertex archive.

D24 ordinary Mesh first. No new actual VFX/Player. Full G3/G4 NOT released.
No complete NBPostprocess Controller/Manager->formal RT->composite proof.
Archive agent changed only owned evidence/report, no index/product/Test/Git/push.
