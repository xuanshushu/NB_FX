T07 Graph late _AlphaAll, 2026-09-30. Product NB_FX commit ea22533.
Unity 6000.3.18f1, URP/SG/VFX 17.3, Metal. All Unity runs used an
isolated clone /tmp/NBFXG2DissolveMaskProbe-20260928 without visible Editor;
no -nographics for pixel tests.

Mesh: copy NBFXT07AlphaAllProbe.cs.txt as Assets/Editor/NBFXT07AlphaAllProbe.cs;
run -batchmode -quit -executeMethod NBFXT07AlphaAllProbe.Run. It writes
screenshots to /tmp/nbfx-t06-20260930, asserts 1/.5/0 transparent response
and bit29 RGB-before-AlphaAll ordering. For the deferred Mask response use
NBFXT07AlphaAllMaskProbe.cs.txt instead, never simultaneously: both contain
the same test-only NBFXT06MaskCaptureFeature helper type. The first mask-probe
log intentionally records this duplicate-class test setup mistake; retry
removed the prior script and passed.

Player: copy NBFXT07AlphaAllPlayerProbe.cs.txt as Assets/NBFXT07AlphaAllPlayerProbe.cs
and NBFXT07AlphaAllPlayerBuild.cs.txt as Assets/Editor/NBFXT07AlphaAllPlayerBuild.cs.
Run -batchmode -quit -executeMethod NBFXT07AlphaAllPlayerBuild.Run, then run
/tmp/nbfx-alpha-all-20260930/player/AlphaAllPlayer.app/Contents/MacOS/NBUnityProject
-batchmode -logFile <log> --nbfx-t08-output=/tmp/nbfx-alpha-all-20260930/player-result.
All assets/scenes and .meta were removed before cleanup import.

Protected clone SHA256 remained:
83180be7acb3bcdd8836e0298590f62eb6e96b2ff3f55a8c2271580c36e9da3b ProjectSettings/ProjectSettings.asset
720fdbd129c1b423a4e3e82fb5f19ee157a2bbccdc0c5ac7c9410bdcede0931d Assets/UniversalRenderPipelineGlobalSettings.asset
