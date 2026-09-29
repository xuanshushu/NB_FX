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
The archived age/lifetime VFX layer-1 fixture was also rebuilt against this
Graph. Its Player result JSON and all eight PNGs match the earlier result
byte-for-byte; see age-replay-compare.txt and age-result-current.json. This
controls default VFX Mesh color and active particle Color/Alpha, but not a
nonwhite source Mesh COLOR. Test assets were removed and a final isolated Unity
import exited 0; age-cleanup.log.gz records it. No visible Editor, official
package, NBPostprocess, UniversalTarget, main Assets/ProjectSettings or push.
