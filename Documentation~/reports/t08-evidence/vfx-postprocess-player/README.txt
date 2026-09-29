NBFX VFX Mesh -> original NBPostProcess Mask -> Uber Player probe
================================================================
TEST-ONLY. This is the original pre-run replay plan; the main Agent later ran
and archived both the initial fixture-threshold failure and successful retry.
See RESULTS.txt and result-final.json. Preparation was confined to /tmp.
The main Agent owns Unity CLI, the isolated clone, evidence archiving, and
cleanup. Do not put these files in the main project's Assets or the NB_FX
product package. Do not alter official URP/SG/VFX packages, UniversalTarget,
NBPostprocess, or their RenderPass implementations.

Source inputs
-------------
Product Graph: Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph
Original NBPostprocess: Packages/com.xuanxuan.nb.fx/NBPostProcessing/Runtime/NBPostProcess.cs
Original mask/Uber: Packages/com.xuanxuan.nb.fx/NBPostProcessing/Runtime/DisturbanceMaskRenderPass.cs
                    Packages/com.xuanxuan.nb.fx/NBPostProcessing/Shader/NBPostProcessUber.shader
Test-only mask viewer: Packages/com.xuanxuan.nb.fx/NBShaders2/Tests/PassFeasibility/FunctionalProbe/NBGFMaskView.shader
Stationary one-particle source archive: Packages/com.xuanxuan.nb.fx/Documentation~/reports/t08-evidence/vfx-dissolve-numeric-one-particle/NBFXNumericOff.vfx.gz

Fixture generation (already done once; rerun is deterministic)
---------------------------------------------------------------
python3 /tmp/nbfx-vfx-postprocess-probe-20260930/prepare_assets.py
The two VFX files are byte-identical except the Output Particle Shader Graph
Mesh slot _NB_DistortionMode: mode0=0, mode1=1. They retain the same real
capacity=1 VFX, Cube mesh, white base texture, Cull Off, source-over blend,
noise=(.25,0), intensity=.25, seed supplied by the Player, and stationary
initialization. The Python generator checks the normalized exact equality.
It creates assets only under this /tmp directory.

COPY TO ISOLATED CLONE ONLY, after ensuring exclusive Unity access
---------------------------------------------------------------
CLONE=/tmp/NBFXG2DissolveMaskProbe-20260928
mkdir -p "$CLONE/Assets/Editor"
cp /tmp/nbfx-vfx-postprocess-probe-20260930/Assets/NBFXPP* "$CLONE/Assets/"
cp /tmp/nbfx-vfx-postprocess-probe-20260930/Assets/Editor/NBFXPPPlayerBuild.cs "$CLONE/Assets/Editor/"
Do not copy README or prepare_assets.py into the clone. Check the original
RendererData, project setting hashes and Git worktrees before/after.

Build, then Player (not -nographics)
------------------------------------
mkdir -p /tmp/nbfx-vfx-postprocess-probe-20260930/run
UNITY_BURST_DISABLE_COMPILATION=1 \
/Applications/Unity/Hub/Editor/6000.3.18f1/Unity.app/Contents/MacOS/Unity \
 -batchmode -quit -projectPath "$CLONE" \
 -executeMethod NBFXPPPlayerBuild.Run \
 -logFile /tmp/nbfx-vfx-postprocess-probe-20260930/run/editor-build.log

Find the actual binary under run/player/NBFXPPPlayer.app/Contents/MacOS/;
its name may not match the .app directory. Run that binary with:
  -batchmode \
  -logFile /tmp/nbfx-vfx-postprocess-probe-20260930/run/player.log \
  --nbfx-pp-output=/tmp/nbfx-vfx-postprocess-probe-20260930/run/output
The build script creates only Assets/NBFXPPPlayer.unity in the clone and
run/player/NBFXPPPlayer.app under /tmp. It does not persist a renderer feature.
The Player appends the test-only mask-view feature to the RendererData in
memory before rendering, sets it dirty, and removes it in finally. It toggles
the original NBPostProcess feature only for a controlled negative capture,
then restores its original active state. The Mask viewer runs one event after
the original copy/mask/Uber sequence and is OFF during final Uber captures.

Read run/output/result.json and all PNGs. The JSON has PASS, CHAIN_MISMATCH,
FIXTURE_INVALID, or CLEANUP_FAILED; raw linear Half measurements (not PNG
8-bit values) drive the assertions. Report original failing observations,
not merely a Boolean. Never call Shader.GetGlobalTexture after Camera.Render
to inspect the mask: at that point it can be a 4x4 default texture.

Accept only if: each real VFX Output has alive=1, !culled, >100 visible pixels
and exact custom Pass indices 1/2; repeated frozen captures maxRGB<=.005;
no-particle and mode0 Mask maxRG<=.005, mode1 Mask maxRG>.01 and >5 nonzero
pixels; final Uber mode0->mode1 >5 changed pixels at .005 linear tolerance;
with NBPostProcess feature disabled, the same mode0/mode1 Forward images are
identical within .005 maxRGB; renderer feature list and active state restore.
This demonstrates this fixture's integrated Player chain only. It does not
prove all material combinations, full NBShader parity, or G5/G6.

Cleanup after archiving evidence
--------------------------------
rm -f "$CLONE"/Assets/NBFXPP* "$CLONE"/Assets/Editor/NBFXPPPlayerBuild.cs \
 "$CLONE"/Assets/Editor/NBFXPPPlayerBuild.cs.meta
The NBFXPP* glob removes the generated scene and its .meta as well as fixtures.
Run one final isolated Unity import/compilation check, verify protected hashes,
then inspect both Git worktrees. Do not push.
