T07 BaseMap alpha channel selector, product NB_FX dd4e80d, 2026-09-30.

Test-only C# is archived here. Copy it to the isolated clone's Assets/Editor
as NBFXT07BaseAlphaProbe.cs, after copying the product Graph HLSL and
.shadergraph at dd4e80d to the clone Packages/NB_FX paths. Run Unity
6000.3.18f1 -batchmode -quit -executeMethod NBFXT07BaseAlphaProbe.Run with
UNITY_BURST_DISABLE_COMPILATION=1 and a log file. It force-reimports Graph,
creates transient Mesh/Camera/textures and writes captures under
/tmp/nbfx-t06-20260930. Expected log:
  NBFX_T07_BASE_ALPHA default=3 centers=71,99,120,129 noBit29RGBDiff=0
The centers are 8-bit rendered red, not sampled-alpha float readback. R/G/B/A
are controlled by only the lowest two bits of _NB_ColorChannelLo16. The probe
also replays earlier Graph Mesh assertions. New Graph property default3 matches
original NBShader's packed default, while an existing VFX Output with a
serialized slot0 keeps 0; no implicit material/VFX migration was built.

To inspect a fresh product VFX sample, overwrite only the isolated clone's
copy of NBGraphVFXMeshMinimum.vfx with the package source, force-reimport via
the archived NBFXT08ImportProbe from the T08 product-mesh evidence, and inspect
generated VFX source. A later current Graph import in timeline-intensity/
generated-2.shader.gz records the default3 line. No main Assets/ProjectSettings,
official package, UniversalTarget, NBPostprocess or push was involved.
