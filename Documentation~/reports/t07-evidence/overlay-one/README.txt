T07 Overlay 1 / Emission bounded Graph slice, 2026-09-30

Product commit: 5c94de7. Unity 6000.3.18f1, URP/SG/VFX 17.3, Metal.
unityMCP was not exposed; tests used exclusive isolated headless CLI clone.
Do not put archived test scripts/assets into product or main project Assets.

Shared arithmetic was extracted verbatim from the existing ShaderLab local
ApplyColorOverlay function. Both ShaderLab calls now invoke the shared
NBFX_ApplyColorOverlayV1. Graph Overlay 1 is explicitly gated by
_EmissionEnabled and supports Add/Multiply plus Flags1 bit31 alpha multiply,
with UV0/ST/rotation/scroll. It is a subset: no Noise distortion, custom
data, non-UV0, wrap override, ForceNoMip, Overlay 2, Ramp, MatCap order,
complete combination parity, or VFX active Output assertion in this bundle.

Mesh: off/add/multiply/add-alpha/multiply-alpha center RGBA from linear
ARGBHalf matched independently calculated expected values within ~0.001.
Graph after render had 10 Pass and custom Pass indices 1/2. Unity's initial
lazy Shader import Pass count 1 is not a failure.

Original ShaderLab pre-extraction regression: clone-only shader copy with
the OLD ForwardPass from commit 0415b7b, otherwise same current includes and
same material. After warm-up, all 5 cases had strict per-pixel diff 0,
7 Pass each, 18 ShaderUtil messages each. Variable name `frozen` in the
archived test script/log refers to this pre-extraction shader, NOT the T00
Frozen package. The first current-vs-T00-Frozen test was invalid because
materials/serialized state differed. Cold first render of clone-only shader
also showed uncompiled/white-frame behavior; it was not reported as product
regression. Original setup failures are archived in selected logs.

Timeline/alpha regression: the earlier full T07 Mesh probe initially failed
its scroll assertion because _Time did not advance in this batch invocation;
other UV rotation/ST assertions remained valid. Clone-only fixture removed
only that nondeterministic scroll assertion. The probe then passed, and all
6 previously archived Timeline/alpha PNGs matched byte-for-byte. This is
not a new assertion that animated scroll was revalidated.

Fresh product VFX sample imported with _EmissionEnabled=0; its generated
source retained both custom Pass tags. The import script's hasOverlay=False
is a SOURCE-TEXT search for a helper in an externally included HLSL file,
not an execution failure. Active VFX numeric test is separate.

Replay scripts and generated sources are included. Main Editor/assets/
ProjectSettings were not mutated. No push.
