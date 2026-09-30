# Dissolve line-color Graph Mesh + real VFX Mesh probe

**Preparation only** (2026-09-30). No Unity Editor/Player run has been performed by this fixture author. Files are test-only under `/tmp/nbfx-vfx-dissolve-line-probe-20260930`; no product, official package, main project, or clone files were edited. The main Agent owns isolated-clone integration, exclusive Unity use, evidence, and Gate interpretation.

## Contract

- Both real VFX assets clone archived Player-validated one-particle Cube/CullOff Output `Packages/NB_FX/Documentation~/reports/t08-evidence/vfx-basecolor-active/NBFXBCA1.vfx.gz`. The Off/On VFX streams differ **only** in `_NB_Flags1Lo16=0/32` (bit 5).
- Both explicitly bind 1×1 linear uncompressed point/clamp `_DissolveMap=(133,133,133,255)/255` and `_BaseMap=(64,128,160,192)/255`; `_Color=(1,1,1,2/3)`, `_Dissolve_Toggle=1`, `_Dissolve=(.5,1,1,.1)`, `_Dissolve_Vec2=(.2,.1,0,0)`, `_DissolveLineColor=(1,0,0,1)`.
- `NBFXLinePlayerBuild` validates exact serialized slots and generated VFX assignments, Graph host include/call, custom Pass tags, then builds a standalone macOS Player. `NBFXLinePlayerProbe` requires exactly one live particle per case, custom Pass indices 1/2, repeat stability, and computes composite effective alpha/source RGB from black/white linear captures. Expected: On is redder, greener/bluer channels decrease; effective alpha stays within .035 of Off. Two cube faces mean effective alpha is **not** single-face shader alpha.
- `NBFXLineMeshProbe` creates controlled single-quad Graph Off/On and ShaderLab Off/On materials, captures black/white pairs, asserts line changes RGB without changing alpha and compares Graph vs ShaderLab with .08 alpha/.1 RGB tolerances. Its legacy material is based on the clone sample `Assets/NBShaderSamples/NBShaderSamples/UnLit.mat`; missing sample or divergent sample state is a **fixture issue** first.
- Neither script checks NBPostprocess RT/composition, GUI sync, production material serialization, arbitrary textures, HDRP, other platforms, or G4 as a whole. Do not call a failed B/C threshold a product regression without examining captures and shader/material state.

## Replay in isolated clone, exclusively

```bash
FIX=/tmp/nbfx-vfx-dissolve-line-probe-20260930
CLONE=/tmp/NBFXG2DissolveMaskProbe-20260928
mkdir -p "$CLONE/Assets/Editor" "$FIX/run/mesh" "$FIX/run/output"
cp "$FIX"/Assets/NBFXLine*.png* "$CLONE/Assets/"
cp "$FIX"/Assets/NBFXLine*.vfx* "$CLONE/Assets/"
cp "$FIX"/Assets/NBFXLinePlayerProbe.cs "$CLONE/Assets/"
cp "$FIX"/Assets/Editor/NBFXLinePlayerBuild.cs "$CLONE/Assets/Editor/"
cp "$FIX"/Assets/Editor/NBFXLineMeshProbe.cs "$CLONE/Assets/Editor/"
UNITY=/Applications/Unity/Hub/Editor/6000.3.18f1/Unity.app/Contents/MacOS/Unity
UNITY_BURST_DISABLE_COMPILATION=1 "$UNITY" -batchmode -quit \
  -projectPath "$CLONE" -executeMethod NBFXLineMeshProbe.Run \
  -logFile "$FIX/run/mesh-editor.log"
UNITY_BURST_DISABLE_COMPILATION=1 "$UNITY" -batchmode -quit \
  -projectPath "$CLONE" -executeMethod NBFXLinePlayerBuild.Run \
  -logFile "$FIX/run/editor-build.log"
APP="$FIX/run/player/NBFXLinePlayer.app"
"$APP/Contents/MacOS/NBUnityProject" -batchmode \
  -logFile "$FIX/run/player.log" \
  --nbfx-line-output="$FIX/run/output"
```

The Mesh probe saves `run/mesh/result.json` and ten frames (two baselines plus eight cases); the Player saves `run/output/result.json` and frames. The Editor build also saves `run/editor-inspection.txt` and generated VFX shader source dumps. **Only successful Editor import/build and Player/Mesh outputs count as evidence.** If the product Graph's new slots are not live or importer rewrites VFX slots, the Editor build fails by design. The force-import source reflection is known from the earlier Fresnel Player fixture in the same Unity version; no claim is made for other versions.

After evidence collection, remove only test assets/scripts/scene from the isolated clone, then check compilation/readiness and console. Never copy these fixtures to the product package or main project. No push.
