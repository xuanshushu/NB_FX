T08 VFX CameraOpaque Mode2 stronger-intensity diagnostic, 2026-09-30

Environment: isolated Unity 6000.3.18f1 / URP, Shader Graph and VFX Graph
17.3 / Metal StandaloneOSX Player. Product NB_FX commit observed when this
report was prepared: 29fd262. This archive does not contain product mutations.

Input delta from the earlier vfx-cameraopaque-isolation sampling fixture:
- Serialized NBFXPPMode1.vfx (historical filename; _NB_DistortionMode is 2):
  _NB_DistortionIntensity 0.25 -> 4.0, the only VFX asset diff. See
  mode2-strength-only.diff.txt. NBFXPPMode0.vfx and NBFXPPPlayerProbe.cs are
  byte-identical to the previous archived fixture. NBFXPPPlayerBuild.cs only
  changes the /tmp run root. See build-path-only.diff.txt.
- Mode0/Mode2 have capacity 1, CullOff, source-over, and their generated
  shaders retain both exact custom Passes. Asset .meta GUIDs are archived.

Replay in an isolated clone, never the main project:
1. Copy archived .vfx/.png.gz assets (gunzip first) and their .meta.txt
   files (rename to .meta) into clone Assets/ at the archived names. Copy
   NBFXPPPlayerProbe.cs.txt to Assets/NBFXPPPlayerProbe.cs and
   NBFXPPPlayerBuild.cs.txt to Assets/Editor/NBFXPPPlayerBuild.cs. The clone
   must contain the current NB_FX Graph, mask-view test Shader, URP RendererData
   and active original NBPostprocess. The .png input assets are archived even
   though the visible gradient is created at runtime by PlayerProbe.
2. Run Unity 6000.3.18f1 against the isolated clone with -batchmode -quit
   -executeMethod NBFXPPPlayerBuild.Run. The build script writes the scene,
   editor-inspection.txt and StandaloneOSX app under its Root constant.
3. Run Root/player/NBFXPPPlayer.app/Contents/MacOS/NBFXPPPlayer with -batchmode
   --nbfx-pp-output=Root/output and a -logFile path. PlayerProbe writes
   result.json and raw PNGs. Run in a Metal-capable session, not -nographics.
4. Compare result.json: isolationValid=true, offMode0/offMode2/onMode0=0
   changed pixels, onMode2=676, onMode2VsBackgroundRGB=.043212890625,
   isolatedMode2RepeatMaxRGB=0. Unisolated finalChangedPixels=0;
   maskMode1Pixels=0. Status DIAGNOSTIC_RECORDED, chainPass=false.

Source/result mapping: result.json and selected PNG are verbatim copies from
/tmp/nbfx-vfx-cameraopaque-strong-20260930/run/output. editor-build.log.gz
and player.log.gz preserve the supplied raw CLI logs. Generated Mode2 Shader
and editor-inspection.txt are from that same isolated run. SHA-256 manifest
hashes archived bytes. .png files use the repository's Git LFS rule.

Important boundaries: Player's serializable "mode1" name means Mode2 here;
no magenta sentinel was installed for this strong run (magenta counts 0 are
not a pass-failure signal). The fixture suppresses built-in transparent
Forward only during its isolated captures and restores RendererData state.
The ordinary final frames still have zero Mode0/Mode2 difference. Original
Mask RG is zero as expected for CameraOpaque. This does not independently
read back the intermediate opaque RT, count RendererList draw events, verify
Controller/ScreenColorCopy combinations, or release G5/G6.
