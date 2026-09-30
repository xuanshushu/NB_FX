# Real VFX Mesh Dissolve Ramp ST override probe (test-only)

**Preparation only.** This author did not run Unity or modify product, clone, official package or main project. The main Agent owns exclusive Editor/Player execution and evidence.

## Why explicit Graph inputs

In the current VFX Graph package, `VFXMaterialSerializedSettings` stores only material `m_Floats` (local package cache `com.unity.visualeffectgraph@f89f6436500a/Editor/Models/Contexts/IVFXSubRenderer.cs:23-100`). The previously generated VFX shader declared `_DissolveRampMap_ST` but did not assign an `output._DissolveRampMap_ST` value. This fixture therefore does **not** pretend that editing a VFX Output texture's tiling is already supported. It tests package-local, live Shader Graph parameters instead.

The product Graph must expose `_NB_DissolveRampSTOverrideEnabled` (float; default 0) and `_NB_DissolveRampSTOverride` (Vector4; normal default `(1,1,0,0)`) as property nodes wired to new `NBGraphBaseColor` fragment Custom Function ports. The Graph HLSL must use `map.scaleTranslate` when enable=0 and the explicit vector when enable=1. Since these nodes feed a live fragment function, VFX Graph should include them in generated `GraphProperties`. This is a **hypothesis until force import checks it**. No official package change is required by the fixture.

Each VFX asset adds two master InputSlots to the existing one-particle Cube/CullOff Mesh Output: a `VFXSlotFloat` for enabled and a `VFXSlotFloat4` (plus x/y/z/w children) for ST, all owned by Output fileID `8926484042661614853`. It also adds scalar Output InputSlots `_NB_WrapFlagsLo16/Hi16` to make wrap explicit. After force import, the generated source **must** contain:

- `output._NB_DissolveRampSTOverrideEnabled = (float)0/1;`
- `output._NB_DissolveRampSTOverride = float4(1, 1, 1.25, 0);`
- `output._NB_WrapFlagsLo16 = (float)0/128;` and `output._NB_WrapFlagsHi16 = (float)0;`
- `NBGraphBaseColor_float(` with the package HLSL include, the RampMap `UnityBuildTexture2DStructInternal` binding, and exact custom `LightMode` Pass tags.

The test PNG `NBFXSTNonuniformRamp8x1.png` is explicit linear RGBA with red/orange/green/blue/yellow/cyan/violet/magenta texels, point/clamp import and no mips. The HLSL's explicit sampler must override the importer wrap. With constant gray133 DissolveMap, pre-soft-step is near `.0716`: native identity ST samples near red; override `(1,1,1.25,0)` places U near `1.3216`, so repeat samples green/blue and clamp samples magenta. All three cases retain Ramp texture source, lerp mode, same one-particle material, and only vary override enable/wrap:

| Case | Override enable | Explicit ST | Wrap low/high |
|---|---:|---|---|
| NativeRepeat | 0 | `(1,1,1.25,0)` ignored | 0 / 0 |
| OverrideRepeat | 1 | `(1,1,1.25,0)` used | 0 / 0 |
| OverrideClamp | 1 | `(1,1,1.25,0)` used | 128 / 0 |

The three serialized VFX streams differ only in override enable and wrap-low scalar values. The Player black/white linear captures infer effective alpha and RGB for one live particle per case; it requires repeat stability, custom Pass indices 1/2, unchanged alpha, and three color signatures separated by nonidentity ST/wrap. It does not check a user-facing VFX Output Inspector, nonidentity material `_ST`, per-particle overrides, mip behavior, HDRP, NBPostprocess, or G4 completion.

## Replay only in isolated clone under exclusive Editor ownership

```bash
FIX=/tmp/nbfx-vfx-dissolve-ramp-st-probe-20260930
CLONE=/tmp/NBFXG2DissolveMaskProbe-20260928
mkdir -p "$CLONE/Assets/Editor" "$FIX/run/output"
cp "$FIX"/Assets/NBFXST*.png* "$CLONE/Assets/"
cp "$FIX"/Assets/NBFXST*.vfx* "$CLONE/Assets/"
cp "$FIX"/Assets/NBFXSTPlayerProbe.cs "$CLONE/Assets/"
cp "$FIX"/Assets/Editor/NBFXSTPlayerBuild.cs "$CLONE/Assets/Editor/"
UNITY=/Applications/Unity/Hub/Editor/6000.3.18f1/Unity.app/Contents/MacOS/Unity
UNITY_BURST_DISABLE_COMPILATION=1 "$UNITY" -batchmode -quit \
  -projectPath "$CLONE" -executeMethod NBFXSTPlayerBuild.Run \
  -logFile "$FIX/run/editor-build.log"
APP="$FIX/run/player/NBFXSTPlayer.app"
"$APP/Contents/MacOS/NBUnityProject" -batchmode \
  -logFile "$FIX/run/player.log" \
  --nbfx-st-output="$FIX/run/output"
```

Inspect `run/editor-inspection.txt`, shader source dumps, `run/output/result.json`, and frames. Unity import/build must succeed before the Player result is meaningful. All results stay outside the product package; no push.
