T07/T08 URP Graph Soft Particles bounded evidence, 2026-09-30

Product package commit: ee1856b. Original pre-shared-fade package revision:
a9c5cc1. This is a single-feature checkpoint, not G4/G5/G6 clearance.
The report writer archived tests run by the main Agent; it did not run Unity.

Environment and required depth resource:
- Isolated Unity 6000.3.18f1 / URP + Shader Graph + VFX Graph 17.3 / Metal.
- Both Mesh and Player probes explicitly set the URP camera's
  GetUniversalAdditionalCameraData().requiresDepthTexture=true.
- The Graph directly samples _CameraDepthTexture through
  NBGraphBaseColor.hlsl/DeclareDepthTexture.hlsl; it does not automatically
  request REQUIRE_DEPTH_TEXTURE. The current main-project URP-HighFidelity,
  URP-Balanced and URP-Performant assets all have m_RequireDepthTexture: 0.
  Runtime users must ensure depth provision and budget prepass/copy cost.

Contents:
- mesh/: asserted Mesh probe source, log and 16 Graph/ShaderLab PNGs.
  Orthographic/perspective x empty/opaque board x Off/On: all 8 cases have
  full-frame alphaDiff=0. Center alpha with board Off=153, On=77; without
  board both=153. Center RGB G=76/77, so do not claim RGB strict parity.
- legacy-regression/: later probe source, log and 24 Graph/current-legacy/
  pre-soft-legacy PNGs. All 8 current-vs-pre-soft legacy alpha differences
  were 0, as were Graph-vs-current legacy Alpha differences.
- vfx/inputs/: original Off/On .vfx plus .meta and linear BaseMap PNG plus
  .meta, gzip-compressed without changing raw bytes. The two VFX assets differ
  only in serialized _SoftParticlesEnabled 0/1; each explicitly binds
  _SoftParticleFadeParams=(0,1,0,0).
- vfx/fixture/: FINAL executed Player C#, Editor build C#, and deterministic
  input generator. The Editor builds a serialized URP Unlit board material;
  Player constructs a board Mesh rather than relying on CreatePrimitive.
- vfx/generated/: the two generated shader sources (gzip). See
  vfx/editor-inspection.txt for exact GraphProperties fields/transfers,
  host function and custom Pass checks.
- vfx/final/: final PASS JSON and 20 Player PNGs. Empty Off/On maxRGB=0;
  with opaque board, median RGB contribution relative to the same board
  baseline is Off=.6237704, On=.3646944; normalized On/Off=.5846612.
  These are final color contributions, NOT direct GPU or single-face Alpha.
  All cases alive=1, runtime passCount=5, custom Pass indices 1/2,
  repeatMaxRGB=0. StandaloneOSX build: 0 errors, 30 warnings.
- vfx/failed-attempts/ plus vfx/logs/: preserve all three historical
  FIXTURE_INVALID Player results and four Player/build log pairs. First
  runtime Shader.Find URP Unlit stripped; next two versions failed to draw
  the opaque board. The final board Mesh + serialized material run is PASS.
- distance-default-off/: original Distance Fade VFX assets rebuilt after
  Soft Graph addition. The post-soft Player result.json bytes and all 20 PNG
  bytes equal the prior Distance Fade archive's corresponding files (result
  SHA-256 da893ee3210335f77ebb117b9afd9b4a7ee51becf628b82538ec82d7fad2018c).
  This is a default-off nonregression for that fixture only.
- cleanup-compile.log.gz: after removing NBFXSoft* clone assets/scripts,
  Unity CLI batchmode exited successfully, no error CS/Shader error markers.
  Main Agent reported three protected-settings hashes stable; digest values
  are not contained in that compile log.

Mesh replay in an exclusive isolated clone:
  Copy mesh/NBFXSoftParticlesMeshProbe.cs.txt to
  Assets/Editor/NBFXSoftParticlesMeshProbe.cs, then run Unity batchmode
  -executeMethod NBFXSoftParticlesMeshProbe.Run. The test requires the
  package Graph, legacy ShaderLab, URP Unlit, and sample UnLit material at
  Assets/NBShaderSamples/NBShaderSamples/UnLit.mat. It writes its PNGs to
  /tmp/nbfx-soft-particles-mesh-20260930.

Pre-soft ShaderLab regression reconstruction, TEST-ONLY in a clone:
  1. Copy current NBShaders2/Shader/NBShader.shader to adjacent
     NBShaderPreSoft.shader. Change only the first Shader name to
     Shader "NBFXTest/NBShaderPreSoft" and each
     HLSL/NBShaderForwardPass.hlsl include to
     HLSL/NBShaderForwardPassPreSoft.hlsl.
  2. From package Git revision a9c5cc1, use `git show
     a9c5cc1:NBShaders2/Shader/HLSL/NBShaderInput.hlsl` and the same path
     ending NBShaderForwardPass.hlsl, writing their contents as
     NBShaderInputPreSoft.hlsl and NBShaderForwardPassPreSoft.hlsl in the
     clone's NBShaders2/Shader/HLSL directory. In the pre-soft ForwardPass,
     change its include "NBShaderInput.hlsl" to
     "NBShaderInputPreSoft.hlsl". Surface.hlsl remains current; the old
     path does not call the newly extracted function.
  3. Copy legacy-regression/NBFXSoftParticlesLegacyRegression.cs.txt to
     Assets/Editor/NBFXSoftParticlesLegacyRegression.cs and run batchmode
     -executeMethod NBFXSoftParticlesLegacyRegression.Run. Remove all these
     test-only shader/HLSL/scripts after recording output. Never commit them.

VFX replay in a fresh exclusive isolated clone:
  Gunzip vfx/inputs/*.gz into clone Assets/ preserving the .vfx/.meta and
  PNG/.meta file names. Copy the two files under vfx/fixture/ named
  NBFXSoftPlayerProbe.cs.txt and NBFXSoftPlayerBuild.cs.txt into
  Assets/NBFXSoftPlayerProbe.cs and Assets/Editor/NBFXSoftPlayerBuild.cs.
  Run Unity 6000.3.18f1 -batchmode -quit -projectPath <clone>
  -executeMethod NBFXSoftPlayerBuild.Run; the script creates
  Assets/NBFXSoftBoard.mat, Assets/NBFXSoftPlayer.unity and the Player at
  /tmp/nbfx-vfx-soft-particles-probe-20260930/run/player/NBFXSoftPlayer.app.
  Run the app's Contents/MacOS/NBFXSoftPlayer binary with -batchmode
  --nbfx-soft-output=<new-directory>; use Metal, not -nographics.
  Review editor-inspection.txt, generated shader, build log, Player JSON and
  PNGs. Do not infer Alpha from black/white pairs over the opaque board:
  the board blocks both clear colors; compare against same-color board-only
  baseline. Clean the clone test assets and recompile afterward.

This evidence does not cover multi-particle behavior, depth unavailable,
other platforms, performance, SRP Batcher/variant cost, complete ShaderLab
B/C, NBPostprocess, Depth Decal, or any full G4/G5/G6 release Gate.
