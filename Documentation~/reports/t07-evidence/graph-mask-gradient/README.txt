T07 Graph Mask1/2/3 packed-alpha Gradient, 2026-09-30.
Unity 6000.3.18f1, URP/ShaderGraph/VFX 17.3.0, Metal, isolated
/tmp/NBFXG2DissolveMaskProbe-20260928 (not the visible main Editor).
Product: NB_FX commit be30026, shared helper commit 2b0f811, GUI grouping commit 175b8d6.
No official package, UniversalTarget, NBPostprocess or main Assets/ProjectSettings edit.

Replay after copying package state into an isolated project:
1. Copy NBFXT07MaskGradientProbe.cs.txt as Assets/Editor/NBFXT07MaskGradientProbe.cs.
   Create /tmp/nbfx-t06-20260930. Unity -batchmode -quit -executeMethod
   NBFXT07MaskGradientProbe.Run (do not use -nographics). It writes all
   t07_gradient_*.png there and logs per-layer changed pixel counts.
2. Copy NBFXT07GradientPlayerProbe.cs.txt as Assets/NBFXT07GradientPlayerProbe.cs
   (not Editor), and NBFXT07GradientPlayerBuild.cs.txt as
   Assets/Editor/NBFXT07GradientPlayerBuild.cs. Run Unity -batchmode -quit
   -executeMethod NBFXT07GradientPlayerBuild.Run. This builds StandaloneOSX at
   /tmp/nbfx-graph-gradient-20260930/player/GradientPlayer.app.
3. Run that app's Contents/MacOS/NBUnityProject -batchmode -logFile <log>
   --nbfx-t08-output=/tmp/nbfx-graph-gradient-20260930/player-result.
   Expect result.json passed=true, Metal, alive>0, visible>0, custom Pass 1/2.
4. Remove all five test Assets and their .meta, reopen clone via CLI, inspect
   cleanup.log.gz and verify ProjectSettings/GlobalSettings SHA below.

The initial Player build log intentionally records a test-script placement error:
runtime MonoBehaviour was first copied into Assets/Editor; retry moved it to
Assets root and passed. Scroll image byte mismatch is not a deterministic
regression assertion because the capture uses live _Time. All other 10/12
archived Mask2/3 PNGs are byte-equal; see png-regression.txt.

Protected clone SHA256:
ProjectSettings/ProjectSettings.asset
83180be7acb3bcdd8836e0298590f62eb6e96b2ff3f55a8c2271580c36e9da3b
Assets/UniversalRenderPipelineGlobalSettings.asset
720fdbd129c1b423a4e3e82fb5f19ee157a2bbccdc0c5ac7c9410bdcede0931d
