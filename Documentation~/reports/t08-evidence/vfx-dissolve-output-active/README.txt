TEST ONLY. These assets/probes were prepared without running Unity. Never copy to product Packages/NB_FX or main-project Assets.

Three cloned, already VFX-importer-synchronized variants differ only in two VFX Output slot values:
- NBGraphVFXCDOff: _Dissolve_Toggle=0, _DissolveMask_Toggle=0.
- NBGraphVFXCDSingle: _Dissolve_Toggle=1, _DissolveMask_Toggle=0.
- NBGraphVFXCDProcessMask: _Dissolve_Toggle=1, _DissolveMask_Toggle=1.
Output slots are serialized float MasterData: fileID 8926484042661614890 and 8926484042661614921. Product Graph has default _DissolveMap=grey and _DissolveMaskMap=white, _Dissolve=(threshold .5, pow 1, mask strength 1, soft width .1), mode0 process. By NBShaderDissolveV3 arithmetic, expected coverage off=1, single~0.5, process-mask=1 if defaults are actually bound. This is a prediction, not a runtime result. Build inspection logs the material textures/generated source to check that assumption.

Source scaffold: /tmp/NBFXG2DissolveMaskProbe-20260928/Packages/NB_FX/NBShaders2/ShaderGraph/Samples/NBGraphVFXMeshMinimum.vfx after prior forced import (SHA-256 1b76c8b9c0cfd125bcb0e5484d639907a2136d2705654b4f87def8aa1053d7bc). It has all current Graph property ports; product source sample lacks those serialized slots until import. No official package or product source was edited.

Copy 3 .vfx and .meta to isolated clone Assets, NBFXT08VFXCDPlayerProbe.cs to clone Assets, and NBFXT08VFXCDPlayerBuild.cs to clone Assets/Editor. Assets have unique GUIDs. Then run:
/Applications/Unity/Hub/Editor/6000.3.18f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /tmp/NBFXG2DissolveMaskProbe-20260928 -executeMethod NBFXT08VFXCDPlayerBuild.Run -logFile /tmp/nbfx-vfx-cd-active-20260930/player-build.log
/tmp/nbfx-vfx-cd-active-20260930/player/VFXCDPlayer.app/Contents/MacOS/NBUnityProject -batchmode -logFile /tmp/nbfx-vfx-cd-active-20260930/player-run.log --nbfx-vfx-cd-output=/tmp/nbfx-vfx-cd-active-20260930/player-output

The probe uses three same-seed VFX Mesh systems on allowed transparent layers Off=2, Single=1, ProcessMask=5 (background=4). RendererData transparentLayerMask=55 excludes layer3, so no case uses layer3. It explicitly simulates 0.35s, captures background/off/single/process-mask PNG, and writes JSON with alive/culling/visible/pair differences/Pass metrics. Editor inspection writes generated shaders and VFX hidden material texture/keyword data when available. Player exits 0 only if Off control is valid; the diagnostic C/D observations are in JSON and must be reviewed separately. A pass here does not imply Gate passage or all UV/texture modes supported. Archive evidence and clean temporary clone Assets/scripts/scene afterwards. Never push.
