T07 Overlay 2 / ColorBlend Graph bounded Mesh slice, 2026-09-30

Product code 6a0329e. Unity 6000.3.18f1, URP/SG/VFX 17.3, Metal.
unityMCP unavailable; exclusive headless CLI clone, not main Editor.
The product Graph uses existing ShaderLab property names and shared original
NBFX_ApplyColorOverlayV1 arithmetic. It executes after Graph Dissolve and
before Mask as in the old ForwardPass. _ColorBlendMap_Toggle gates sampling.
Flags1 bit16 selects Add (default bit clear = Multiply). Flags0 bit25
enables overlay alpha multiplication. ColorBlendVec.z is alpha strength,
ColorBlendVec.w rotation, ColorBlendMapOffset.xy scroll speed.

Mesh linear ARGBHalf center values match independently calculated original
overlay equations to ~0.001 for off/add/multiply/add-alpha/multiply-alpha.
Pass10, exact custom pass indices1/2, ShaderUtil messages4 as prior Graph.
Overlay 1 regression was rerun with Overlay 2 default off; all five archived
Overlay 1 PNGs byte-identical. Numeric test used a 1x1 texture, so this
bundle does NOT verify animated UV, Noise/custom data, wrap/ForceNoMip,
combined Overlay1+Overlay2 order, legacy ShaderLab T00 Frozen B/C, Output
GUI, or active VFX. Further VFX test is separate. No Gate release or push.

Probe source, original logs, PNGs, and hash manifest are archived here.
Do not place this test-only script in product or main project Assets.
