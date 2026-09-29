TEST ONLY. Prepared without running Unity. Never copy these variants or probe into Packages/NB_FX or the main project Assets.

Copy to isolated clone:
- NBGraphVFXMeshConstant1.vfx[.meta], NBGraphVFXMeshAgeBound.vfx[.meta], NBGraphVFXMeshAgeRed.vfx[.meta] -> clone/Assets/
- NBFXT08AgeFollowupPlayerProbe.cs -> clone/Assets/
- NBFXT08AgeFollowupPlayerBuild.cs -> clone/Assets/Editor/

AgeRed differs from AgeBound only by VFX slot link: AgeOverLifetime output fileID 8926484042661615001 now links _Color.r fileID 8926484042661614856 instead of _Color.a fileID 8926484042661614859. AgeRed alpha remains default 1. Each .vfx has a distinct .meta GUID; they all still reference the product Shader Graph GUID.

Run ONLY in isolated clone after current package files are synchronized, from shell:
/Applications/Unity/Hub/Editor/6000.3.18f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /tmp/NBFXG2DissolveMaskProbe-20260928 -executeMethod NBFXT08AgeFollowupPlayerBuild.Run -logFile /tmp/nbfx-vfx-age-followup-20260930/player-build.log
/tmp/nbfx-vfx-age-followup-20260930/player/AgeFollowup.app/Contents/MacOS/NBUnityProject -batchmode -logFile /tmp/nbfx-vfx-age-followup-20260930/player-run.log --nbfx-age-followup-output=/tmp/nbfx-vfx-age-followup-20260930/player-output

Probe uses three seed-matched real VFX Mesh systems, camera renders to avoid culling, then explicitly queues Simulate(1f/60f,9u) and later Simulate(1f/60f,12u) to examine ~0.15s and ~0.35s stages. It waits two player frames after each Simulate because VFX commands are processed on a subsequent Update. Result JSON and 8 PNGs are written to player-output.

Interpretation:
- AgeRed visible while AgeAlpha invisible: output attribute transport likely works; alpha near zero or alpha blending/temporal issue remains. Inspect early/late mean red and alive counts.
- Both visible late and generated shader reads age/lifetime: age→alpha is runtime viable in this controlled Player test; NOT full G5, dynamic range, per-particle feature matrix, nor Inspector validation.
- AgeRed invisible but Constant visible: examine generated shader binding/attribute-buffer path or variant import, not just alpha.
- All invisible or alive=0: test harness/culling/simulation invalid; do not infer product failure.
- `passed` requires late-stage visibility, pass count/index and Metal, but does not itself prove age changes with time; compare staged PNG and meanRed values separately.

No product file, official package, clone file, scene, or project setting was changed by preparation.
