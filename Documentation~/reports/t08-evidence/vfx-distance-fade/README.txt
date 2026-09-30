T07/T08 URP Graph Distance Fade bounded evidence, 2026-09-30

Environment: isolated Unity 6000.3.18f1 / URP, Shader Graph and VFX Graph
17.3 / Metal. This archive is evidence for a working-tree Graph slice, not a
released commit or a G4/G5 Gate decision. The report writer did not run Unity.

Evidence:
- mesh/editor-mesh.log.gz and five PNG: Graph Mesh Off 0.6 and On fade at
  eye depth 2/3/5 -> 0/0.3/0.6. ShaderUtil warnings 5; do not claim zero.
- bc/editor-bc-initial.log.gz plus bc/editor-bc-gamma-adjust.log.gz and all
  24 PNG: Graph/old ShaderLab × orthographic/perspective × depth 2/3/5 ×
  Off/On. Every case recorded alphaDiff=0, a strict per-pixel Alpha match.
  Initial input color Gamma mismatch caused large RGB differences. After
  fixture-only Gamma correction, G is still 76 versus 77 at sampled pixels,
  and perspective geometry coverage differs. No RGB/full-frame equality.
- mesh-replay/NBFXDistanceFadeReplay.cs.txt, replay.log.gz, and 24 PNG:
  later independent replay of all 12 Graph/ShaderLab B/C cases from a
  complete source probe. Every alphaDiff=0 again; its log ends with PASS,
  graphPass=10 and legacyPass=7. Original failed-RGB/adjusted logs remain.
- vfx/editor-inspection.txt, editor-build.log.gz, player.log.gz, result.json,
  all 20 PNG and generated Off/On shader dumps. Off/On VFX generated source
  has both exact custom Pass tags, GraphProperties bindings for
  _DistanceFade_Toggle and _Fade, and NBGraphBaseColor call; Player reports
  PASS, alive 1, Pass count 5, custom indices 1/2, repeat maxRGB 0.

VFX fixture replay, only in a fresh isolated clone with exclusive Editor
ownership: copy vfx/NBFXDistance*.vfx.gz and the BaseRGBA png.gz after gunzip
to clone Assets/, with archived matching .meta; copy
vfx/NBFXDistancePlayerProbe.cs.txt to Assets/NBFXDistancePlayerProbe.cs and
vfx/NBFXDistancePlayerBuild.cs.txt to Assets/Editor/NBFXDistancePlayerBuild.cs.
The deterministic generator vfx/prepare_assets.py.txt can regenerate inputs
from the basecolor-active archive if renamed .py and pointed at that archive.
Run Unity 6000.3.18f1 -batchmode -quit -projectPath <clone>
-executeMethod NBFXDistancePlayerBuild.Run, then run its built
/tmp/nbfx-vfx-distance-fade-probe-20260930/run/player/NBFXDistancePlayer.app
with -batchmode --nbfx-distance-output=<new-output-directory>. Use Metal,
not -nographics. The build script Root constant fixes its output path.
The archived VFX build script is a replayable version with an explicit
GraphProperties field/transfer assertion; generated-source bindings and
numeric Player outcome from the original run are separately preserved.

The original short-lived Mesh and B/C probe sources were removed from the
isolated clone before the first archive was assembled; their logs/PNGs remain
historical evidence. The subsequent **B/C replay is now source-complete**:
copy mesh-replay/NBFXDistanceFadeReplay.cs.txt as
Assets/Editor/NBFXDistanceFadeReplay.cs into an isolated clone with the same
Graph, legacy NBShader, and sample UnLit material referenced in its source;
run Unity 6000.3.18f1 -batchmode -quit -projectPath <clone>
-executeMethod NBFXDistanceFadeReplay.Run. The source Root constant writes
24 PNG to /tmp/nbfx-distance-fade-replay-20260930; replay.log.gz archives
the executed run. The separate initial Graph-only Mesh direct-response
probe still lacks its original C# source; do not claim that exact probe can
be regenerated from this archive alone. The main Agent subsequently removed
all NBFXDistance* fixture assets and Editor scripts, including the replay
script, from the isolated clone. cleanup-initial.log.gz and
cleanup-after-replay.log.gz archive its two post-cleanup Unity CLI compiles:
both exited batchmode successfully and contain no "error CS" or
"Shader error" markers. The main Agent also checked that three protected
settings hashes were unchanged; the digest values themselves are not part
of these two compile logs.

VFX black/white pairs infer source-over effective Alpha, not GPU target Alpha.
The output uses a double-sided Cube; two visible faces make effective Alpha
different from one fragment's Alpha. This evidence does not cover Soft
Particles, NBPostprocess RT/Uber, complete ShaderLab parity, multi-particle
state, non-Metal platforms, or a full G4/G5 release. See sha256.txt for exact
archived byte hashes; all PNG files follow repository Git LFS rules.
