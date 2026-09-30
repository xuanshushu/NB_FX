# Dissolve Ramp controlled fixture (test-only)

All files here are under `/tmp`. This author has **not run Unity** or edited product, clone, official package or main project. The main Agent owns serial Editor/Player verification and evidence.

## Real one-particle VFX Mesh Output, five cases

The assets derive from the previously Player-validated one-particle Cube/CullOff fixture. BaseMap is explicit linear RGBA `(64,128,160,192)/255`; DissolveMap is explicit linear gray `133/255`; RampMap is explicit linear yellow `(255,255,0,192)/255`, all 1×1 uncompressed point/clamp. `_Color=(1,1,1,2/3)`, `_Dissolve=(.5,1,1,.1)`, Dissolve enabled. Packed-gradient keys are blue at 0 and 1, alpha 1 at both ends, count `131074` (2 color/2 alpha). Ramp tint is white. These values separate the source and blend branches visibly without varying dissolve alpha.

| Asset | Ramp toggle | Source | Flags1 bit 6 |
|---|---:|---:|---:|
| Off | 0 | gradient | 0 |
| GradientLerp | 1 | gradient | 0 |
| GradientMultiply | 1 | gradient | 64 |
| TextureLerp | 1 | texture | 0 |
| TextureMultiply | 1 | texture | 64 |

The five serialized VFX files are identical except for those three scalar values. Build inspector validates explicit texture GUIDs, serialized slots, generated assignments, Graph Custom Function host, and custom Pass tags. Player measures RGB and effective alpha using black/white linear captures; it requires one live particle, exact custom Pass indices 1/2, repeat stability, source-mode/blend-mode numeric separation, and stable alpha.

### Replay in an isolated clone with exclusive Editor ownership

```bash
FIX=/tmp/nbfx-vfx-dissolve-ramp-probe-20260930
CLONE=/tmp/NBFXG2DissolveMaskProbe-20260928
mkdir -p "$CLONE/Assets/Editor" "$FIX/run/output"
cp "$FIX"/Assets/NBFXRamp*.png* "$CLONE/Assets/"
cp "$FIX"/Assets/NBFXRamp*.vfx* "$CLONE/Assets/"
cp "$FIX"/Assets/NBFXRampPlayerProbe.cs "$CLONE/Assets/"
cp "$FIX"/Assets/Editor/NBFXRampPlayerBuild.cs "$CLONE/Assets/Editor/"
cp "$FIX"/Assets/Editor/NBFXRampMeshProbe.cs "$CLONE/Assets/Editor/"
cp "$FIX"/Assets/Editor/NBFXRampWrapMipMeshProbe.cs "$CLONE/Assets/Editor/"
UNITY=/Applications/Unity/Hub/Editor/6000.3.18f1/Unity.app/Contents/MacOS/Unity
UNITY_BURST_DISABLE_COMPILATION=1 "$UNITY" -batchmode -quit \
  -projectPath "$CLONE" -executeMethod NBFXRampMeshProbe.Run \
  -logFile "$FIX/run/mesh-editor.log"
UNITY_BURST_DISABLE_COMPILATION=1 "$UNITY" -batchmode -quit \
  -projectPath "$CLONE" -executeMethod NBFXRampWrapMipMeshProbe.Run \
  -logFile "$FIX/run/wrap-mip-mesh-editor.log"
UNITY_BURST_DISABLE_COMPILATION=1 "$UNITY" -batchmode -quit \
  -projectPath "$CLONE" -executeMethod NBFXRampPlayerBuild.Run \
  -logFile "$FIX/run/editor-build.log"
APP="$FIX/run/player/NBFXRampPlayer.app"
"$APP/Contents/MacOS/NBUnityProject" -batchmode \
  -logFile "$FIX/run/player.log" \
  --nbfx-ramp-output="$FIX/run/output"
```

Read `run/editor-inspection.txt`, generated shader dumps, `run/output/result.json`, `run/mesh/result.json`, `run/wrap-mip-mesh/result.json`, and PNGs. The basic Mesh probe creates controlled Graph and ShaderLab single-quad materials for all five cases, infers RGB and effective alpha from black/white linear captures, and separately reports Graph function, ShaderLab function and B/C agreement with .08 alpha/.1 RGB tolerances. It starts from the clone's `Assets/NBShaderSamples/NBShaderSamples/UnLit.mat`; missing sample or unexpected saved state is a fixture issue first. A fixture import/build failure is **not** a product rendering verdict.

The **advanced Mesh-only** `NBFXRampWrapMipMeshProbe` uses a nonuniform 8×1 colored RampMap and `U=preSoftStep+1.25` to separate U-repeat modes 0/2 from U-clamp modes 1/3; it also compares against identity ST. Its LOD pair uses a 256×1 linear DissolveMap gradient and a mipmapped 1024×1 period-7 red/blue RampMap. With `_Dissolve.x=0` and Ramp ST offset `−0.1`, the Ramp coordinate is approximately the DissolveMap red channel and varies across the quad; automatic mip sampling should blur while `_NB_ForceNoMipFlagsHi16=1` / legacy bit16 should preserve high-frequency texels. The script requires visible Graph **and** ShaderLab auto-vs-force separation, wrap equivalences/separation, and bounded pixelwise B/C; all measurements/PNGs are retained. A failed LOD-separation threshold may mean the fixture did not produce sufficient derivatives—inspect generated textures and frames before labeling a product defect. It does not test VFX nonidentity ST, VFX wrap/LOD, other platforms, per-particle overrides, NBPostprocess, HDRP or G4 completion.

The VFX Player fixture uses identity Ramp ST and no ForceNoMip bit. VFX Graph's current material-settings implementation serializes floats only (official package cache `IVFXSubRenderer.cs:23-100`), so non-identity texture ST needs separate investigation rather than assumed support.

No push.
