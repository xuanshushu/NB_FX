> **Status update (main Agent):** The two-line MPB Integer-channel fix was applied after this proposal was written; H1–H4 current-Editor API probes passed. The remaining sections describe design candidates and unrun GPU/Player work, not implemented Graph functionality.

# T01/G1 current-Unity packed Flags / MPB / Graph bridge proposal (read-only)

**Status:** Design and source review only; no product code, official package, Docs or Unity asset was edited by this sub-agent. Current target is Unity 6000.3.18f1 + URP/SG/VFX 17.3.0 under D21. This proposal does **not** mark G1 approved. Run `python3 Packages/NB_FX/Documentation~/reports/g1-evidence/replay_flag_protocol.py`, `python3 Packages/NB_FX/Documentation~/reports/g1-evidence/static_bridge_matrix.py`, `python3 Packages/NB_FX/Documentation~/reports/g1-evidence/replay_split16_bridge.py`, and `python3 Packages/NB_FX/Documentation~/reports/g1-evidence/replay_frequency_variants.py` for pure-source/model evidence. Existing main-Agent Unity Editor CLI observations are cited separately; GPU/Player bridge is not verified.

## 1. Three different issues, not one

### A. Existing `ShaderFlagsBase` helper bug, **observed now**

`XuanXuanRenderUtility/Runtime/ShaderFlagsBase.cs:43-55` reads/writes the MPB branch of `SetFlagBits` through `GetInt/SetInt`, while `ClearFlagBits` and `CheckFlagBits` use `GetInteger/SetInteger` (`:58-86`). The Material branch uses `GetInteger/SetInteger` (`:34-49,60-64,76-80`). The main Agent's current-Unity Editor CLI raw evidence establishes that Int and Integer are not interchangeable here:

- `Documentation~/reports/g1-evidence/current-material-bit31-command.json`: `Material.SetInteger` and `MPB.SetInteger` of signed `0xC0800001` round-trip through `GetInteger` as `-1065353215`; `MPB.GetInt` returns `0`.
- `Documentation~/reports/g1-evidence/current-material-setint-command.json`: `SetInt`/`GetInt` yielded `-1065353216` (low bit lost versus original) while Integer-channel reads stayed at default/zero.
- `Documentation~/reports/g1-evidence/current-helper-mpb-command.json`: actual `ShaderFlagsBase.SetFlagBits` MPB call yielded `blockInt=-2147483648`, `blockInteger=0`, `helperCheck=False`.

Thus this is **not only an MPB seeding concern**: the helper's own Set and Check disagree in the tested Editor API. Do not call the existing helper safe for Graph or VFX bridge.

**Smallest candidate diff, not applied:**

```diff
--- a/XuanXuanRenderUtility/Runtime/ShaderFlagsBase.cs
+++ b/XuanXuanRenderUtility/Runtime/ShaderFlagsBase.cs
@@ SetFlagBits(int flagBits, MaterialPropertyBlock propertyBlock = null, int index = 0)
-                int flags = propertyBlock.GetInt(GetShaderFlagsId(index));
-                propertyBlock.SetInt(GetShaderFlagsId(index), flags | flagBits);
+                int flags = propertyBlock.GetInteger(GetShaderFlagsId(index));
+                propertyBlock.SetInteger(GetShaderFlagsId(index), flags | flagBits);
```

This two-line fix aligns all `ShaderFlagsBase` operations with integer properties and preserves bit31/low bits in the Editor API channel. It does **not** solve initialization, Graph binding, shader compilation, or Player GPU behavior. `NBPostProcessFlags` also derives from this shared base (`NBPostProcessing/Runtime/NBPostprocessFlags.cs:5-22`), and its in-repo callers currently use the Material branch (`PostProcessingManager.cs:316-692`, `PostProcessingController.cs:203-253`). No explicit non-null `propertyBlock` argument at call sites was found by source search under `Packages/NB_FX`; external consumers are unknown. Review regression impact on NBPostprocess before applying this shared-file change. `AnimationSheetHelper` has separate Material-only integer helper methods (`AnimationSheetHelper.cs:313-335`), unaffected by the two-line diff.

### B. Full-word MPB seeding is a **separate contract**

`MaterialPropertyBlock` bitwise operations in `ShaderFlagsBase.cs:43-70` read **only the block**; no fallback to `_material.GetInteger` is present. For a fresh block, setting slot1 bit31 without seeding can replace the ShaderLab/material default bit0=1 (`NBShader.shader:522`) with a block value containing only bit31. The existing Editor API evidence does not itself prove final GPU rendering of that override. Do not silently change `SetFlagBits` to always OR Material bits: doing so could erase an intentional preexisting per-renderer override.

**Minimal bridge policy:** require an explicit first-write `SeedPackedWord(material, block, slot)` that calls `block.SetInteger(id, material.GetInteger(id))`, or an owner-maintained **full 32-bit packed value** passed to `block.SetInteger`. Subsequent Set/Clear may mutate the same Integer channel. The owner must obtain the renderer's existing MPB first and preserve unrelated MPB properties. Since no safe `HasInteger`/inheritance behavior has been established here, do not infer whether a zero block value means “unset” or an intentional zero override. Keep this initialization policy in the NBShaders2 Graph/VFX adapter rather than modifying NBPostprocess behavior via the shared helper.

**Post-fix Editor API test vectors (not run by this sub-agent):**

| Case | Initial state | Operations | Expected Integer-channel result |
|---|---|---|---|
| H1 | Material slot1 `1`; MPB explicitly seeded to `1` | Set bit31, Set bit23, Clear bit31 | `0x00800001`; Check bit23=true, bit31=false, bit0=true |
| H2 | Material slot1 `1`; fresh empty MPB | Set bit31 after two-line fix, without seed | `0x80000000`; Check bit31=true but bit0=false (**contract risk remains**) |
| H3 | Material slot1 `0xC0800001`; MPB seeded from Material | Set bit2 then Clear bit31 | `0x40800005`; verify high/low bits survive, no `SetInt`/`GetInt` path |
| H4 | `NBPostProcessFlags` Material with multiple bits | Material Set/Clear/Check | Existing behavior unchanged by MPB-only diff; verify with existing controller fixture |

Tests H1-H4 should be run serially in current Unity Editor by the main Agent, with `Material.GetInteger`, `MPB.GetInteger` and direct helper Check logged; then representative Mesh/Player GPU readback, SRP Batcher profiling, and VFX Output behavior. H1-H4 expected values are proposed, **not observed**.

### C. Product Graph's packed bridge **does not exist yet**

The GF Graph contains only BaseMap/Color and no NB packed flags/Graph keywords (`GF_URP_VFX_NBSubTarget.shadergraph:5-13,349-358,978-987`); GF distortion pass is explicitly a fixed probe (`NBGFDistortPass.hlsl:1-3,28-42`). Its prior Pass/Player GF result cannot validate a full NBShader wire bridge.

**Why Graph blackboard “Integer” is not a proven direct uint path in installed SG17.3:** local official package source `Library/PackageCache/com.unity.shadergraph@2b401d56d4b3/Editor/Data/Graphs/Vector1ShaderProperty.cs:13,20,25-34,87-112` implements `Vector1ShaderProperty : AbstractShaderProperty<float>`, `PropertyType.Float`; `FloatType.Integer` emits ShaderLab `(Int)` yet HLSL `HLSLType._float`. `IntegerNode.cs:25-27,46-56` outputs Vector1 as well. A full packed `0xC0800001` therefore cannot be **assumed** lossless through this blackboard path, even though ShaderLab says `Int`. Do not alter official SG code.

**Smallest product-bridge candidate, not applied:** retain NBShader's existing 32-bit material integer words for URP Mesh; for the separate URP VFX Graph, expose **two full-precision float uniforms per runtime packed word** (lo/hi 16 bits) and reconstruct locally in one shared HLSL Custom Function wrapper. Runtime words are flags0/1, Wrap, ForceNoMip, CustomData0..3, UV low/type, Channel, PNoise = 12 words / 24 float properties (96 bytes of raw float payload before alignment/other generated storage) if all needed. Foldout slots3..5 are persisted GUI-only state and should not be sent to GPU. Keep public Graph property reference names stable, copy values from a canonical signed `int` without mutating source materials, and write via `SetFloat` to Graph Float properties (not `SetInt` or Graph's misleading “Integer” mode). Input/output use `float`, not `half` or interpolators. Per-particle Custom1/2 values remain a **separate dynamic stream**; the uniform nibble selector merely chooses a stream component (`NBShaderInput.hlsl:1393-1394,1436-1437`; `NBShaderForwardPass.hlsl:117-122`; `NBShaderFlags.hlsl:178-225`). No new keyword should encode these packed modes.

Suggested local adapter pseudo-code (names to freeze at G1, **not** product patch):

```csharp
uint bits = unchecked((uint)packedSignedInt);
material.SetFloat(lowReferenceId,  (float)(bits & 0xFFFFu));
material.SetFloat(highReferenceId, (float)(bits >> 16));
```

```hlsl
uint NBRebuildPackedUInt(float low16, float high16)
{
    uint lo = (uint)round(clamp(low16,  0.0, 65535.0));
    uint hi = (uint)round(clamp(high16, 0.0, 65535.0));
    return lo | (hi << 16);
}
```

The `Temp/NBFXT01/replay_split16_bridge.py` pure-Python IEEE-754 model checked every integer 0..65535 and ten full-word boundary vectors (including `0x80000000`, `0xC0800001`, `0xF0000000`, `0xFFFFFFFF`): all round-trip in **float32 arithmetic**. This does not prove SG generated `UnityPerMaterial` uses full float, nor Unity serialization/Material/MPB/Player/VFX preserves it. `half` has insufficient precision for all 16-bit integers. If current SG SubTarget can instead expose an actual `uint` CBUFFER path without custom official-package mutation, it may be better; require generated-code and Player proof before choosing it. Do not introduce parallel C# flag definitions or repack NBShader's serialized words.

**Graph/VFX verification cases to schedule under main-Agent Unity lane:**

1. Generated Graph and VFX shaders after force reimport: 24 split properties (or proven exact alternative), full-float declarations, stable references, exact `NBCameraOpaqueDistortPass` and `NBDeferredDistortPass` LightModes, no compiler errors; close/reopen project persistence.
2. Graph Material save/reload → values `0`, `1`, `0x7FFFFFFF`, `0x80000000`, `0xC0800001`, `0xFFFFFFFF`; reconstruct HLSL and read back GPU output; compare Mesh NBShader int source versus Graph output. Include Wrap bit31, UV-type bit31, CustomData nibble at shift28, and color/PNoise fields.
3. VFX Output actual per-particle Custom1/2 eight distinct channels and dynamic values, with uniform selector fixed, across forward and both distortion passes. Do not infer VFX streams from Mesh TEXCOORD semantics.
4. MPB use, if any, must write complete split pair to Graph's float properties and prove SRP Batcher/instancing behavior. Do not reuse `ShaderFlagsBase.SetFlagBits(MPB)` for float Graph properties.
5. Build current Standalone Player, inspect retained variants and render output; no claim from Editor API-only tests.

## 2. Frequency and keyword/Pass budget conclusion for **G1 review**

The complete frequency table and per-pass lexical pragma inventory are in `T01-frequency-variant-budget-draft.md` / `frequency_variant_inventory.json`. Source-proven classification: runtime packed words and texture/scalar properties = `UnityPerMaterial` uniform; `Custom1/Custom2` = vertex input carried to fragment, potentially per particle only after VFX binding proof; `shader_feature*`/`multi_compile*` = compile-time group; LightMode = pass selection; GF `_NBGFMode` = test-only global. `ShaderFlagsBase` optional MPB is a renderer override API with the observed bug above, not an already validated Graph bridge.

Current production NBShader has seven Passes (`NBShader.shader:589-1226`). Static pragma groups per pass (feature/multi/fog): `SRPDefaultUnlit 44/2/1`, `UniversalForward 44/2/1`, `DepthOnly 21/0/0`, `ShadowCaster 21/1/0`, each custom distortion Pass `31/1/1`, `Universal2D 32/2/1`. These are **declaration counts only**. Across them, 65 managed keyword names are in `NBShaderFeatureCatalog.cs:23-93`, with `_CUSTOM_LOCAL_TRANSFORM` external and enabled at runtime by `NBParticleLocalTransformHelper.cs:60-98`. The current tier stripper early-returns for any shader not named `Effects/NBShader` (`NBShaderVariantStripper.cs:22-45`), so product SG/VFX variants are **not** automatically covered by existing stripping.

Main Agent's current-Unity **internal, non-public** `ShaderUtil.GetVariantCount` read-only evidence `Documentation~/reports/g1-evidence/current-variant-baseline-command.json` returned `allVariants=9091715745054720` and `usedBySceneOnly=true=59` for `Effects/NBShader` (non-public method, current project/current scene). The first is a theoretical combination/reporting ceiling, **not actual compiled or Player-retained variants**; 59 is current scene sample usage, **not a Player count**. Neither is a product Graph number. Treat both as current-environment trend baselines only; do not bind correctness or build-budget pass/fail to this unsupported API.

**Proposed source-level budget for G1 (needs main-Agent signoff):** 0 new `multi_compile` groups; 0 unreviewed new keyword groups; no packed/per-particle mode converted to compile-time; any justified Graph keyword must list affected generated passes, expected material reachability, tier/strip policy and current Unity compiled/retained variant delta. Exactly the two required custom LightModes, with no extra unexplained Pass. Existing GF generated 10 Graph / 8 VFX passes after force reimport (GF evidence) is a reference, not a fixed permanent target. A numeric compilation-time/Player-size/retained-variant ceiling must wait for current Unity measurement (cold/warm Graph import duration, generated code/pragma diff, Editor compiled variants, current-platform Player retained variants and build size). Lower-version trials remain deferred per D21.

**Gate note:** the shared helper's observed MPB bug is a product maintenance issue; it is not an automatic blocker to a Graph adapter that avoids it, but G1 must explicitly assign ownership and prohibit relying on the broken path. Full Graph bridge and all GPU/Player checks are still unimplemented/unrun; no G1 approval claimed here.
