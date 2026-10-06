# Main-relative ShaderLab/HLSL source audit

Baseline package: `26917c03709830fc62b12fa466e38862840cfde2`. Candidate package: `17ac773c29fdc8a1bb2780ca0b2057ea0f5826bf`.

This audit uses `git show` snapshots and current source reads; no checkout/worktree, product mutation, Unity interaction, refresh, build, or remote mutation was performed. Private snapshots/diffs/evidence only.

## Conclusion

Seven Pass blocks, render states, Pass order/LightMode, all keyword pragmas, debug pragma include, fallback and CustomEditor text are exactly main. Every existing 450 Properties declaration line/default is exactly main. Two hidden animation helper Properties are added (452 current). The earlier regex counts 441/443 omitted nine declarations without leading underscores; Root's ../all450-shaderlab-declarations-proof.json supersedes that count. Historical snapshot-summary.json counts remain preserved, not evidence of the total property count.

Strict full behavior/precision invariance is not yet proved: two confirmed source narrowing boundaries and one conditional output narrowing were found. A minimal private preview patch removes them; it is ready for Root review and contains no installed product edit. Unity compilation/AB still required after approved application.

## Minimal precision preview

`precision-preview-1/precision.patch`: 6 files, 7 replaced source lines. `manifest.json` records original/preview hashes and exact replacements. `interface-checks.json` checks all definitions, forward declarations, and the 2 known ShaderLab/Graph callers for each shared function.

1. Particle Flipbook: main `blendUv.z = input.normalWSAndAnimBlend.w` has `float4` source and `float3` destination. Current helper inserted `(float)(half)streamWeight`. Preview keeps float stream directly; helper uniform remains half. Graph UV3.x/BlendWeight remain float.
2. Fresnel: main `half dotNV = dot(half3 fresnelDir, float3 varying.normal)` rounds after dot. Current helper takes half3 normal, rounding before dot. Preview takes float3 normal in implementation/declaration, retains half3 fresnelDir/half result and original `1.01` literal. Native caller keeps float normal; Graph's existing half normal is promoted without another narrowing. Remap, inversion and pow algorithm unchanged.
3. NormalMap: main `float varying.normal = normalize(TransformTangentToWorld(...))` receives the URP real3 result. Preview uses float3 out/local before the same float varying. The normalize call is untouched. Graph adapter local matches float3 out; its existing half3 return remains. This eliminates conditional loss where real=float and half is native16; mobile real=half already has the original half boundary inside the URP helper.

## Intentional source behavior/contract changes requiring separate scope treatment

- Additional vertex light/fog repair: `InitializeInputData` reads `positionWS.w`/`vertexLight` instead of nonexistent `fogFactorAndVertexLight`; native non-SixWay parsing is restricted to the existing Half-Lambert code with `NB_HALF_LAMBERT_ONLY`. These improve previously broken variants. Do not revert into noncompiling code; describe as baseline defect repair, not pure relocation.
- UnityPerMaterial: append 31 declarations = `_VATTex_TexelSize` plus 30 Houdini floats. Old declarations are exact prefix, preserving prior member ordering. Global/material uniform binding and SRP Batcher eligibility/layout differ from main and require current material/MPB/Batched validation. This is a batching/binding repair, not unchanged buffer layout.
- NBPostProcessing: camera-shake owner binding/release, previous Perlin stop, disable ResetEffect, owned-only SoloCamera clearing, and inactive editor update guards are real lifecycle behavior changes. NBPost shader/HLSL, flag protocol, resource declarations and distortion RT math have no source diff.
- Properties: the two hidden animation properties are material/API/default contract additions. Both HLSL variables already existed in UnityPerMaterial. Existing declaration defaults unchanged; import/API consequences require read-back evidence.

## Equivalent relocation/adaptation reviewed

- Base UV: default/special/particle stream selection, Sprite reverse-ST, cylinder matrix affecting object UV, Twirl/Polar order, UV flag decoder, shared/main order, CustomData nibble selection, ST/animation and time source are preserved. Host flipbookBlending parameter remains zero-initialized and compile-time `_FLIPBOOKBLENDING_ON` retains original native branch. The previous global rotation mutation becomes a local half; current native call graph executes ProcessBaseUVs at most once per stage, so later consumers do not observe lost side effects.
- Feature UV V2: rotation -> ST -> existing half offset animation order is preserved. Graph-only customOffsetAfterST stays zero for native host. No new texture samples.
- Geometry V1: modes 0/1/2/3, non-normalized world/object vector conversion, signed StartFromZero decode, original multiply order and half inputs are preserved; a Unity branch hint moved but no algorithm change.
- Distortion/Chromatic/Parallax: original texture wrap sampler and LOD, half decode/alpha boundaries, signedRG payload and alpha-before-premultiply, branch thresholds, float refraction/TIR zero, chromatic three-sample alpha 0.5 each and POM iteration/sample sequence preserved.
- Surface/Environment/Mask/Dissolve/Depth: overlay add/multiply and alpha semantics, base tint/timeline gates, ramp composition, color adjustment CustomData, dither Bayer math, depth decal object cube, override device depth, SoftParticles/outline and dissolve debug boundary/early vs late mask modes preserve original equations and order. The identified precision boundaries above remain the actionable exceptions.
- Packed Gradient: eight moved helper bodies token-identical, including half interpolation/SmoothStep and inner alpha square. JSON proof included.
- Houdini VAT: six utility math bodies token-identical. All four mode samples/frame selection/bounds decode/quaternion/rest pivot/pscale/origin mask and camera-to-world-to-object billboard transform reviewed. SoftBody rotates texture sampled before pure position math now, but texUV/LOD/resources independent and numeric sequence retained. Native mode remains compile-time constant passed to kernel. Particle time remains original `_Time.y`, not common `time`.
- Tyflow VAT: 23 utility helper bodies equal after config/texture argument normalization; named gamma replacement body is literally the original XuanXuan function. Six modes preserve metadata sizes 2/5/6/7, frame/custom selection, seven-bone unroll, PRSXYZ inverse start TM rule, index UV channels, interpolation, normals and shadow suppression. Graph-only `[loop]` guard does not affect native `[unroll]`.
- SixWay: explicit emission power helper retains `_SixWayInfo.y` native wrapper. Graph `NB_GRAPH_SIX_WAY` guards do not change native SixWay equations. Existing native SixWay + vertex additional light references `surfaceData.albedo`; this pre-existing issue is not a new regression and should not be hidden by parity claims.

## Evidence files

- `snapshot-summary.json`: revisions/file inventory/property count/SubShader equality.
- `static-contract-checks.json`: exact old Properties/SubShader/CBUFFER prefix plus 14 exact moved function bodies.
- `tyflow-moved-function-checks.json`: 22 direct normalized matches, and one gamma-renamed helper resolved by `additional-exact-checks.json`.
- `additional-exact-checks.json`: gamma equality and unchanged dependency byte hashes (Flags, debug pragma, Xuan utility, VAT dispatcher, NBPost flags).
- `findings.json`: classified findings, main/current line mappings, triggers and smallest fixes.
- `main/`, `candidate/`, `diff/`: source snapshots and bounded per-file diff evidence.
