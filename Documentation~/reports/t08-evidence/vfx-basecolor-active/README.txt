NBFX VFX Mesh BaseColor Timeline + packed BaseMap alpha selector
================================================================
TEST-ONLY, NOT UNITY-RUN. Everything this subagent created is under /tmp.
Do not copy to the main project's Assets or NB_FX product package. The main
Agent owns isolated Unity CLI, clone mutations, evidence, cleanup and Gate.
No official URP/ShaderGraph/VFX package, UniversalTarget, or NBPostprocess
source/asset needs modification for this probe.

Purpose and separable cases
---------------------------
Four real one-particle Output Particle Shader Graph Mesh VFX variants share a
linear, uncompressed 1x1 RGBA BaseMap with byte values (64,128,160,192),
_Color white, Cull Off, transparent SrcAlpha/OneMinusSrcAlpha. They are
byte-identical except these two Output scalar slot values:
  A0: _NB_ColorChannelLo16=3 (BaseMap A alpha),
      _BaseColorIntensityForTimeline=0
  A1: same alpha selector A, timeline=1
  A2: same alpha selector A, timeline=2
  R0: _NB_ColorChannelLo16=0 (BaseMap R alpha), timeline=0
A0/A1/A2 independently test RGB timeline scaling; A0/R0 independently test
packed alpha channel selection at zero source RGB. DistortionMode remains 0,
so NBPostprocess mask is empty and is not the subject of this test. Off-state
Dissolve and Mask slots stay disabled.

`prepare_assets.py` adds the new timeline float slot to the archived VFX Asset
because the original numeric fixture predates the Graph property. It attaches
fileID 8926484042661616001 to the one Output's `m_InputSlots`; this is a
SERIALIZATION HYPOTHESIS, not assumed valid. The clone Editor build inspects
imported generated VFX shader and fails before Player if the Output timeline
or packed channel values do not survive VFX import. If that happens, fix the
test-only fixture (or record the tooling limitation), not the product Target.
The generator verifies normalized VFX text equality across all four cases.

Preparation (already run once; deterministic)
-----------------------------------------------
python3 /tmp/nbfx-vfx-basecolor-probe-20260930/prepare_assets.py
Check `/tmp/nbfx-vfx-basecolor-probe-20260930/Assets/` and preserve all .meta.

COPY INTO ISOLATED CLONE ONLY, after obtaining exclusive Unity CLI access
-----------------------------------------------------------------------
CLONE=/tmp/NBFXG2DissolveMaskProbe-20260928
mkdir -p "$CLONE/Assets/Editor"
cp /tmp/nbfx-vfx-basecolor-probe-20260930/Assets/NBFXBC* "$CLONE/Assets/"
cp /tmp/nbfx-vfx-basecolor-probe-20260930/Assets/Editor/NBFXBCPlayerBuild.cs "$CLONE/Assets/Editor/"
Do NOT copy into main project. Read current clone Git/settings/renderer hashes
and available Unity MCP state first (no unityMCP tool exposed to this agent).

Build / Player
--------------
mkdir -p /tmp/nbfx-vfx-basecolor-probe-20260930/run
UNITY_BURST_DISABLE_COMPILATION=1 \
/Applications/Unity/Hub/Editor/6000.3.18f1/Unity.app/Contents/MacOS/Unity \
 -batchmode -quit -projectPath "$CLONE" \
 -executeMethod NBFXBCPlayerBuild.Run \
 -logFile /tmp/nbfx-vfx-basecolor-probe-20260930/run/editor-build.log

The build script imports/inspects the 1x1 texture, all four serialized and
generated VFX Outputs, records generated shader sources and runtime scene
references, creates clone-only `Assets/NBFXBCPlayer.unity`, then builds a
StandaloneOSX Player at `run/player/NBFXBCPlayer.app`. Editor hidden VFX
Material may report lazy passCount=1; Player asserts actual compiled Passes.
Find the actual executable under app/Contents/MacOS and run it WITH Metal,
not `-nographics`:
  <actual-executable> -batchmode \
   -logFile /tmp/nbfx-vfx-basecolor-probe-20260930/run/player.log \
   --nbfx-bc-output=/tmp/nbfx-vfx-basecolor-probe-20260930/run/output

Read `run/output/result.json`, four case PNGs + repeats, background PNG, and
logs. JSON statistics are from linear ARGBHalf readback, NOT encoded PNG bytes.
Actual test only after build + Player exit 0; this /tmp preparation is not a
Unity result. The script reports separate `timelinePass` and `selectorPass`.

Numeric acceptance/limitations
------------------------------
All four: alive=1, !culled, >100 visible pixels, runtime exact custom Pass
indices CameraOpaque=1 and Deferred=2, and repeated frozen captures max RGB
error <=0.005. Safe interior ROI >=100.
Timeline A0->A1 and A1->A2 each change >=100 pixels. The two RGB increments
have median >0.05, normalized second-difference median <0.06, P90 <0.10.
Alpha selector: A0 and R0 differ >=100 pixels at intensity0; infer coverage
from `1 - dot(frame, background)/dot(background, background)` on interior
pixels. A0 median within 0.08 of 1-(1-192/255)^2=0.938962; R0 median within
0.08 of 1-(1-64/255)^2=0.438970; A P10 > R P90+0.2.
The two-face prediction follows the archived numeric test's closed Cube/
Cull Off/source-over observation. It remains an inference, not a direct GPU
alpha readback. If imported Output material/render state differs, report that
rather than falsely diagnosing Graph alpha logic. No VFX custom attributes,
postprocess/Mask, full old ShaderLab parity, other platforms, or G5 sign-off.

Cleanup, after archiving evidence
---------------------------------
Remove only clone `Assets/NBFXBC*`, clone `Assets/Editor/NBFXBCPlayerBuild.cs`
plus its .meta, then run one isolated Unity import/compile check. Verify
ProjectSettings and URP GlobalSettings hashes, both Git worktrees, and no
main project Assets changes. Never push.

Actual main-agent run, 2026-09-30
---------------------------------
Editor build exit 0, 0 errors, 33 warnings; Player exit 0 on Metal, status PASS, fixtureValid/timelinePass/selectorPass true. See result.json. Four real single-particle Outputs retained exact Pass indices 1/2 at runtime, alive 1, repeatMaxRGB 0. Cleanup import/compile exit 0 and clone project/settings/renderer hashes unchanged. This is not a G5/G6 Gate pass.
The four raw Unity VFX YAML variants are archived losslessly as NBFXBC*.vfx.gz; decompress to .vfx with adjacent .vfx.meta.txt renamed to .vfx.meta for direct replay. Alternatively run prepare_assets.py against its archived numeric fixture dependency.
