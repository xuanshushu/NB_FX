T07 Color Adjustment bounded Graph Mesh slice, 2026-09-30

Product code 06d0ef6. Unity 6000.3.18f1, URP/SG/VFX 17.3, Metal.
unityMCP unavailable; exclusive isolated headless CLI clone.

Original adjustment arithmetic was extracted to the shared pure
NBFX_ApplyColorAdjustmentV1. ShaderLab retains its conditional CustomData
resolution in the wrapper; Graph receives uniform/connected values and
does not yet implement legacy CustomData routing. Graph places adjustment
early after BaseMap when Flags0 bit22 is set, otherwise late after
VertexColor/ColorA and before AlphaAll. Shared helper also applies Flags0
bit29 RGB premultiply in that selected position. Flags0 bit0 saturation,
bit19 hue; Flags1 bit24 contrast and bit27 color refine are connected.

Graph Mesh linear ARGBHalf center: off (.033,.073,.133,.600), contrast
(.066,.146,.266,.600), saturation zero (.069,.069,.069,.600), hue half
(.133,.093,.033,.600), refine (.066,.146,.266,.600). With Overlay1 red,
late contrast R1.069 vs early R.568; with bit29 late R.642 vs early R.542.
The first fixture expected bit29 gap >.15 and failed; measured intended
gap is .100, so only this test threshold was corrected to >.08. Original
failure log and final passing log are retained. 10 Graph Pass, custom 1/2.
ShaderUtil warnings5 vs prior4: new compiled refine pow emits one extra
negative-input warning even though disabled in other variants. Do not call
Console clean or silently rewrite pow semantics.

Original ShaderLab extraction regression: current vs clone-only pre-
extraction variant from nested commit d40c3ef, same sample material and
4 warm Camera.Render frames per side. Strict RGBA32 per-pixel diff0 for
off/saturation/contrast/hue/refine/late/early/late-premul/early-premul;
both 7 Pass and 18 ShaderUtil messages. This is NOT T00 Frozen B/C.

Overlay1, Overlay2, Ramp default-off regressions: 14 previously archived
Mesh PNGs byte-identical. Fresh VFX sample imported and retained custom
Pass tags but active VFX adjustment is a separate test. No Gate release.

Test-only sources, old variant source and logs/PNGs are archived. Do not
copy them to product or main project Assets. Test-only clone files were
removed; cleanup compile exit0, ProjectSettings and renderer/global hashes
unchanged. No push.
