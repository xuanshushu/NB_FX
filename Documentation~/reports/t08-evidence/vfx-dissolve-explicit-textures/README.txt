TEST ONLY — prepared without running Unity. Copy only into an isolated clone, never Packages/NB_FX or the main project. No product/clone mutation or commit was made during preparation.

Purpose: distinguish an unbound/null VFX Output texture from the dissolve threshold/math. This supersedes the earlier default-texture-only C/D variant, not product logic.

Artifacts:
- NBDissolveGray133Linear.png, GUID 955ea0926dde4e128aec69a8ce164b47: 1x1 RGBA=(133,133,133,255), linear (sRGB off), no mips/compression, point/clamp. Sampled channel = 133/255 = 0.5215686.
- NBDissolveMaskWhiteLinear.png, GUID cef3ff1fb43149c781878d9235ea29fd: 1x1 RGBA=(255,255,255,255), same importer settings. Sampled channel = 1.
- NBGraphVFXCDExplicitOff.vfx: both toggles 0, texture slots still null. Baseline control.
- NBGraphVFXCDExplicitSingle.vfx: _Dissolve_Toggle=1 and _DissolveMap (slot fileID 8926484042661614889) references gray texture as VFX serialized object {fileID:2800000,guid:955ea0926dde4e128aec69a8ce164b47,type:3}; mask toggle 0.
- NBGraphVFXCDExplicitProcessMask.vfx: both toggles 1, same gray texture, and _DissolveMaskMap (slot fileID 8926484042661614920) references white texture as {fileID:2800000,guid:cef3ff1fb43149c781878d9235ea29fd,type:3}.
- Each .vfx and each .png has its own new .meta GUID, distinct from the earlier default-texture C/D set.
- NBFXT08VFXCDExplicitTexturesPlayerProbe.cs and NBFXT08VFXCDExplicitTexturesPlayerBuild.cs are cloned test harnesses, with unique class/scene/asset/output names.

Expected *numeric coverage*, conditional on VFX Graph actually binding these texture slots and current product defaults _Dissolve=(threshold .5, exponent 1, mask strength 1, soft width .1), process mask mode 0:
- Off = 1.
- Single = saturate(10*(133/255)-4.5) = 0.7156863 (approximately; half precision may vary).
- ProcessMask: prepared dissolve value = ((133/255)+1)/2 = 0.7607843, then coverage saturates to 1.
These are math predictions only; the Player images and imported hidden VFX Output material/source must establish actual rendering. The transparent RendererData mask 55 excludes layer 3, so probe uses Off=2, Single=1, ProcessMask=5 and background=4.

Copy all 3 .vfx plus .meta, both .png plus .meta, and Probe.cs to isolated clone Assets; copy Build.cs to clone Assets/Editor. Paths in Build.cs assume this layout and package alias Packages/com.xuanxuan.nb.fx. Build performs forced texture and VFX imports, validates texture GUID/import settings/1x1 pixel values, and records generated sources and hidden VFX output material texture names/keywords (if reflection can access it).

Invoke:
/Applications/Unity/Hub/Editor/6000.3.18f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /tmp/NBFXG2DissolveMaskProbe-20260928 -executeMethod NBFXT08VFXCDExplicitTexturesPlayerBuild.Run -logFile /tmp/nbfx-vfx-cd-explicit-textures-20260930/player-build.log
/tmp/nbfx-vfx-cd-explicit-textures-20260930/player/VFXCDExplicitTexturesPlayer.app/Contents/MacOS/NBUnityProject -batchmode -logFile /tmp/nbfx-vfx-cd-explicit-textures-20260930/player-run.log --nbfx-vfx-cd-explicit-output=/tmp/nbfx-vfx-cd-explicit-textures-20260930/player-output

Review editor-inspection.txt, generated-*.shader, player-output/result.json and PNGs. JSON validControl verifies only Off baseline. observedSingleDifference/observedProcessDifference are separate observations and do not automatically pass T08/Gate. Compare against the earlier default-texture-only variant to isolate null/default binding. If imported VFX Output material still reports null textures or generated source lacks expected toggle constants, stop and diagnose that harness/import state before inferring feature failure. Archive evidence and remove temporary clone Assets/scripts/scene afterwards; never push.
