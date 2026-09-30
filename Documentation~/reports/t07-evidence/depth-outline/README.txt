URP Graph Depth Outline bounded evidence, 2026-09-30

Product package code commit: 6f51533. Pre-extraction source: 620f82d (documentation-only successor of ee1856b).
This archive was assembled from tests already run by the main Agent. The
report writer did not run Unity. D24 makes ordinary Graph Mesh T06/G3 and
T07/G4 the current ordered work. The VFX Player evidence here is historical
early exploration only; it is NOT T08/G5 advancement or Gate clearance.

Environment: isolated Unity 6000.3.18f1, URP/Shader Graph/VFX Graph 17.3,
Metal. The ordinary Mesh and Player fixtures explicitly set camera
requiresDepthTexture=true. Current main-project URP assets default depth
request off. No test here proves behavior without depth texture.

Contents:
- mesh/: asserted Graph/old ShaderLab source, import and asserted logs,
  16 PNGs. Orthographic/perspective x empty/opaque board x Off/On: every
  full-frame Graph-vs-ShaderLab alphaDiff=0. Center with board + On is
  RGBA(255,25,25,242) for both. With no outline, Graph G=76 and old
  ShaderLab G=77, so RGB strict equality is NOT claimed. Graph 10 Pass,
  ShaderLab 7 Pass; 5/18 ShaderUtil messages. Capture uses four Render()
  calls before readback; it does not test first visible frame.
- legacy-regression/: second probe source and log, plus 24 Graph/current
  ShaderLab/pre-outline ShaderLab PNGs. Each of eight current-vs-pre-outline
  full RGBA PixelDiff==0 assertions passed; Graph-vs-current alphaDiff==0.
  Pre-outline shader is test-only and not part of the product package.
- vfx/inputs/: gzip-compressed original Off/On .vfx and .meta plus BaseMap
  PNG and .meta. Preserve the GUIDs; Player builder verifies them.
- vfx/fixture/: original Player and Editor build C# sources as text.
  Editor builder creates a serialized URP Unlit board material and scene.
- vfx/generated/: gzip-compressed generated VFX shader source for Off/On.
  See vfx/editor-inspection.txt for generated properties, host call and
  exact custom Pass checks.
- vfx/output/: original Player result.json and twenty screenshots. Player
  PASS is limited to one-particle Cube/CullOff Output, 0/2 outline near/far,
  explicit camera depth, one opaque board. Empty Off/On maxRGB=0. Board
  Off vs On maxRGB=.7475586; board-relative median signal On/Off=2.442847.
  This is final RGB displacement from the SAME board baseline, NOT direct
  GPU alpha. Runtime alive=1, passCount=5, custom indices 1/2, repeats 0.
- vfx/logs/: StandaloneOSX build Succeeded / 0 errors / 30 warnings and
  Metal Player log. Licencing and fallback-shader log lines are preserved;
  do not describe the full log as warning-free.
- sha256.txt: checksums for all archived files, including this README.

Ordinary Mesh replay: in an exclusive isolated clone with the product
package at 6f51533, copy mesh/NBFXDepthOutlineMeshProbe.cs.txt to
Assets/Editor/NBFXDepthOutlineMeshProbe.cs and run Unity batchmode method
NBFXDepthOutlineMeshProbe.Run. The fixture needs the sample material at
Assets/NBShaderSamples/NBShaderSamples/UnLit.mat, URP Unlit, the product
Graph and NBShader ShaderLab. It writes PNGs to
/tmp/nbfx-depth-outline-mesh-20260930.

Pre-outline regression replay: copy legacy-regression probe into
Assets/Editor/NBFXDepthOutlineLegacyRegression.cs. In the clone only,
make NBShaderPreOutline.shader from current NBShader.shader, change shader
name to NBFXTest/NBShaderPreOutline and ForwardPass includes to
HLSL/NBShaderForwardPassPreOutline.hlsl. Obtain the old ForwardPass HLSL
from `git show 620f82d:NBShaders2/Shader/HLSL/NBShaderForwardPass.hlsl`
and save it next to the current file as NBShaderForwardPassPreOutline.hlsl.
The old ForwardPass uses the current unchanged shared Input and Surface
includes but retains its inline Depth Outline block. Run batchmode method
NBFXDepthOutlineLegacyRegression.Run; remove test-only files afterward.

VFX early-exploration replay: gunzip vfx/inputs/*.gz into clone Assets/;
copy fixture C# into Assets/NBFXOutlinePlayerProbe.cs and
Assets/Editor/NBFXOutlinePlayerBuild.cs. Run Unity batchmode method
NBFXOutlinePlayerBuild.Run. The builder creates Assets/NBFXOutlineBoard.mat
and Assets/NBFXOutlinePlayer.unity and writes a StandaloneOSX app under
/tmp/nbfx-vfx-depth-outline-probe-20260930/run/player/. Run the app's
Contents/MacOS executable with -batchmode --nbfx-outline-output=<dir>.
Do not use -nographics; Metal and depth rendering are needed. Compare
Player JSON and PNGs, not just editor import. This replay is deferred by
D24 until ordinary Mesh stages are reviewed.

The main Agent completed exclusive clone cleanup/recompile; see the cleanup
checkpoint and archived log below.
No complete Mesh parity, VFX C/D, NBPostprocess, multi-particle, depth-
unavailable, performance, platform compatibility, or G3-G7 clearance is
claimed here.

Cleanup checkpoint: main Agent removed clone-only NBFXOutline* assets/scripts,
DepthOutline probes and pre-outline shader/HLSL. Unity CLI import/compile then
exited 0, with no error CS/Shader error markers (cleanup-compile.log.gz).
The new ordinary-Mesh G4 test file was compiled in the same run; it does not
change product shaders. Protected setting digests remained unchanged; see
protected-settings-sha256.txt. No new VFX test was run during this cleanup.
