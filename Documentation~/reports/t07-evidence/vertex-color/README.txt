T07 Vertex Color checkpoint, product NB_FX 366a2e0, 2026-09-30.

The probe is test-only. Copy NBFXT07VertexColorProbe.cs.txt to the isolated
clone's Assets/Editor/NBFXT07VertexColorProbe.cs, not the main project.
First copy the product NBGraphBaseColor.hlsl and NBShaderGraph.shadergraph at
commit 366a2e0 to the clone's corresponding Packages/NB_FX paths. Without
this sync the initial run falsely failed; both logs are archived.

Run Unity 6000.3.18f1 -batchmode -quit with
  -projectPath /tmp/NBFXG2DissolveMaskProbe-20260928
  -executeMethod NBFXT07VertexColorProbe.Run
  -logFile <isolated log>
and UNITY_BURST_DISABLE_COMPILATION=1. The probe creates only transient Mesh,
Material, Camera and textures; the generated-current.shader and temporary PNGs
are written under /tmp/nbfx-t06-20260930. Expected log:
  NBFX_T07_VERTEX_COLOR redDiff=1764 ignoredDiff=0
                         alphaWithoutBit29=0 alphaWithBit29=1764
It also re-runs the earlier T06/T07 assertions.

These counts are RGB pixels with >2 byte-channel difference, not a GPU alpha
readback or old ShaderLab B/C numeric parity. The no-COLOR primitive default
PNG SHA-256 equals the archived T07 ColorA baseline. Gamma color-space
conversion and active VFX source-mesh/particle color require separate tests.
Remove the C# test asset; the later T08 Player build and final cleanup import
did so. No visible Editor, official package, NBPostprocess, UniversalTarget,
main Assets/ProjectSettings or push was involved.
