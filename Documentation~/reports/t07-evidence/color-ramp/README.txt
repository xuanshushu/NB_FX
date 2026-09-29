T07 Color Ramp bounded Graph Mesh slice, 2026-09-30

Product code commit 44d0307. Unity 6000.3.18f1, URP/SG/VFX 17.3, Metal.
unityMCP not exposed; exclusive isolated CLI clone, no main Editor window.

The original packed color-key evaluator was moved verbatim from
NBShaderInput.hlsl into the existing shared packed-gradient header, where
Graph can call it. The original Ramp composition was extracted to
NBFX_ApplyColorRampV1 and both ShaderLab/Graph call it. Graph Ramp runs
after Overlay 1 and before Dissolve, as in old ShaderLab. The current slice
supports UV0 source or explicit RampColorMap channel, packed RGB/alpha keys,
count, tint, wrap on UV source and Flags0 bit24 add vs default multiply.
Noise, special UV/custom data, wrap override/ForceNoMip for texture, GUI
gradient widget and cross-effect combinations remain incomplete.

Graph Mesh linear ARGBHalf center: off (.033,.073,.133,.600), multiply
(.016,.037,0,.120), add (.521,.584,.133,.800), texture-source
(.875,.231,.133,.800). Across the gradient, left/right samples change
from red-biased (.868,.238) to green-biased (.181,.925). Pass10, exact
custom indices1/2, ShaderUtil messages4. 1x1 texture source changes the
center toward red as expected. This is controlled Mesh rendering, not old
ShaderLab T00 Frozen parity.

Original ShaderLab extraction regression: current shader vs clone-only
pre-Ramp-extraction variant built from nested commit b27a0a5, same sample
material/settings, 4 warm Camera.Render frames per side. Strict per-pixel
RGBA32 diff0 for off/multiply/add/texture-source. Both have 7 Pass;
ShaderUtil messages current18 vs pre20, because the extracted function
uses explicit .rgb where old half3+=half4 emitted truncation warnings.
This pre-extraction fixture is NOT T00 Frozen. Archived test-only old shader,
ForwardPass, Input, and packed-gradient header are gzipped here.

Overlay1 and Overlay2 default-off regression: all ten previously archived
Mesh PNGs reproduced byte-for-byte. Fresh product VFX sample import kept
the two exact custom Pass tags, but active Ramp VFX output is separate.

The test scripts and original logs/PNG/hash manifest are archived here;
do not copy test-only files into product or main Assets. No Gate release,
no push. The user's waived first-frame anomaly was not converted into a
general tolerance or an unrecorded warmup rule; warmup applies only to
the explicitly identified clone-only pre-extraction shader comparison.
