TEST ONLY — prepared and executed in isolated Unity, 2026-09-30.
Do not add these assets/scripts to Packages/NB_FX or the main project's Assets/ProjectSettings.
All generated files are confined to this /tmp directory. No product or official-package edit is needed.

Purpose
-------
Take the already archived explicit-texture real Output Particle Shader Graph Mesh
case and ask a narrower numeric question: does Single coverage match shared
NBFX_ResolveDissolveV3 math when exactly one particle is visible? The current
Off/Single/Process images only prove a visible response, not a coverage value.
This is stage 1: final-color ROI. NBPostprocess _DisturbanceMaskTex readback and
Uber composite are NOT validated by this fixture; add those as separate stage 2.

Inputs and isolation
--------------------
prepare_assets.py deterministically expands the archived ProcessMask .vfx.gz
into three .vfx files. They are byte-identical after normalizing exactly two
Output toggle slots (fileIDs 8926484042661614890/4921): Off 0/0, Single 1/0,
ProcessMask 1/1. All three bind the same explicit 1x1 linear, point/clamp,
uncompressed/no-mip white _BaseMap, gray (133/255) _DissolveMap and white
_DissolveMaskMap. The source asset's Init capacity is reduced 68 -> 1; lifetime
becomes 10 seconds; initial speed, gravity, drag and spherical spawn radius
become 0. No original asset or source .meta is changed. GUIDs are fresh and
stable (uuid5), so this set does not collide with the older archived assets.
Static reverse-application of only these replacements reproduces the archived
ProcessMask VFX source byte for byte.
The VFX Output still uses the actual product ShaderGraph and Mesh Output.

The Player script uses the RendererData's currently allowed transparent layers
2/1/5 for Off/Single/Process and layer 4 for the background. Each VisualEffect
gets seed 12345, a fixed Simulate(1/60,21) after three rendered warm-up frames,
two more rendered bounds-update frames, then pause=true.
All captures use the same Game Camera in one frame, with the culling mask
switching to show one VFX layer at a time. The orthographic camera is centered at y=0 with size 0.5 to obtain a safe
576-pixel inner ROI. It hard-rejects any case that is not
alive==1, non-culled, visible >=100 pixels and carrying both exact NB custom
Pass names. The 256x256 target is linear ARGBHalf without MSAA; texture readback
is RGBAHalf. The script also captures a same-case repeat for stability.

Expected math (not a claim that the Player already passed)
------------------------------------------------------------
The Graph defaults are _Dissolve=(threshold 0.5, exponent 1, mask strength 1,
soft width 0.1), ProcessMask mode 0. Shared V3 then predicts:
  Off coverage = 1
  Single = saturate(10*(133/255)-4.5) = 0.7156862745
  ProcessMask = saturate(10*((133/255+1)/2)-4.5) = 1
The Output material must be transparent SrcAlpha/OneMinusSrcAlpha and AlphaClip
off. This sample is a closed Cube with Graph _Cull=0 (off), so front and back
faces source-over composite at each interior pixel. The final-color ratio
therefore predicts 1-(1-0.7156862745)^2 = 0.9191657 for Single, not 0.715686.
The Player reports both the single-fragment and composited expectations.
A test-only attempt to set VFX Output cullMode=Back still left hidden Material
_Cull=0; this needs separate UI/Target investigation and is NOT treated here
as an approved product change. The build script checks the VFX YAML, imported textures, generated shader
toggle constants, both custom Pass tags and the hidden Output material's
_Surface/_AlphaClip/_SrcBlend/_DstBlend before building. It does
NOT treat a new Material(shader).GetTexture() NULL as evidence of VFX binding;
the previous experiment showed that API cannot inspect the hidden VFX binding.

For each common interior pixel, the Player reads background B, Off O, Single S,
and Process P. It rejects 1-pixel borders and pixels whose 3x3 Off-vs-background
neighborhood has insufficient contrast. The alpha-coverage estimate is the
least-squares RGB ratio dot(S-B,O-B)/dot(O-B,O-B), similarly for P. It records
median/P10/P90 and requires at least 100 ROI pixels. Initial acceptance bands
are median Single within +/-0.03 of 0.9191657 and median Process within +/-0.03
of 1, plus both P10/P90 within +/-0.05 of their predictions. These are
intentionally not strict per-pixel zero-difference assertions:
GPU half precision, blend quantization, post pipeline, filtering and edges can
change individual pixels. A result is a product numeric failure only after
confirming the fixture's single particle, identical geometry, actual linear
transfer and generated Output state. Result code 2 means fixture invalid;
code 1 means numeric mismatch under the stated initial bands; code 0 means
this limited numeric slice passed.

Replay after the main Agent grants exclusive isolated Unity CLI access
---------------------------------------------------------------------
1. Copy the *contents* of this Assets directory into an isolated clone's
   Assets directory, preserving each .meta. Do not copy to the main project.
   Ensure the clone has Packages/com.xuanxuan.nb.fx pointing at the current
   product Graph, and has Unity 6000.3.18f1 / URP+SG+VFX 17.3 / Metal.
2. Run the editor build (no unityMCP currently exposed in this session):
   UNITY_BURST_DISABLE_COMPILATION=1 \
   /Applications/Unity/Hub/Editor/6000.3.18f1/Unity.app/Contents/MacOS/Unity \
     -batchmode -quit -projectPath /tmp/NBFXG2DissolveMaskProbe-20260928 \
     -executeMethod NBFXNumericVFXPlayerBuild.Run \
     -logFile /tmp/nbfx-vfx-numeric-probe-20260930/run/editor-build.log
3. Inspect run/editor-inspection.txt and build log; resolve any fixture/import
   failure before treating the Player as a product test. The build creates
   only the clone scene Assets/NBFXNumericVFXPlayer.unity and the /tmp Player.
4. Run the built Player with Metal graphics, not -nographics. Its executable
   should be inside run/player/NBFXNumericVFXPlayer.app/Contents/MacOS/;
   inspect that directory rather than guessing its executable name. Example:
   <actual-player-executable> -batchmode \
     -logFile /tmp/nbfx-vfx-numeric-probe-20260930/run/player.log \
     --nbfx-numeric-output=/tmp/nbfx-vfx-numeric-probe-20260930/run/output
5. Read run/output/result.json and the background/off/single/process PNGs,
   also compare repeat PNGs. Do not infer direct GPU alpha from the preview
   PNGs: the JSON ratios come from linear Half readback. If results disagree,
   first verify target color transfer and identical particle silhouette.
6. Archive logs/JSON/images as appropriate, then remove only these temporary
   Assets and the generated scene from the isolated clone. Never push.

Known limitations
-----------------
- The final isolated Unity build and Metal Player passed. Earlier fixture-only
  retries had lazy Editor passCount=1, premature VFX pause with alive=0,
  insufficient image ROI and a wrong single-fragment expectation; all are
  preserved as evidence, not product failures.
- Capacity=1 and pause are intended to isolate one particle, but Player must
  prove alive==1; this is not inferred from YAML alone.
- The final-color ratio assumes the same source color/geometry and linear
  source-over blend for all three Outputs. If URP/NBPostprocess applies a
  nonlinear transfer, the initial numeric band is not an acceptance verdict;
  calibrate or read the transient mask in stage 2.
- This does not test late-mask mode, arbitrary UV/CustomData, actual NBPostprocess
  mask/composite, old ShaderLab B/C parity, or all VFX/Player functions.

Final result, 2026-09-30
------------------------
Unity 6000.3.18f1 / URP+SG+VFX 17.3 / Metal Player: build 0 errors,
31 warnings, exit 0. One alive/non-culled particle per case; 676 visible pixels,
576 safe ROI pixels; each repeated frozen capture RGB max difference 0.
Single final-color median ratio 0.91875565 (P10 0.91867542, P90 0.91889423)
versus two-face prediction 0.91916573; Process median/P10/P90 1.0;
Off and Process whole readback are identical. Both exact custom Passes retained
at runtime indices 1/2. This is a limited final-color numeric test, not direct
GPU alpha readback, NBPostprocess Mask/RT, Uber composite, or old ShaderLab B/C.
