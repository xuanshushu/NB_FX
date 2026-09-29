TEST ONLY. Prepared without Unity. Do not copy these files into Packages/NB_FX or the main project Assets.

Variants: Alpha100/095/050 differ only in VFX Output Color slot fileID 8926484042661614855 default m_SerializableObject.a = 1.0/0.95/0.5. No dynamic Age link. Each has a distinct .meta GUID; all still reference the product Shader Graph.

Copy three .vfx and .vfx.meta to isolated clone Assets; NBFXT08AlphaStaircasePlayerProbe.cs to clone Assets; NBFXT08AlphaStaircasePlayerBuild.cs to clone Assets/Editor. Ensure package product files are already synchronized to clone.

Run in isolated clone:
/Applications/Unity/Hub/Editor/6000.3.18f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /tmp/NBFXG2DissolveMaskProbe-20260928 -executeMethod NBFXT08AlphaStaircasePlayerBuild.Run -logFile /tmp/nbfx-vfx-alpha-staircase-20260930/player-build.log
/tmp/nbfx-vfx-alpha-staircase-20260930/player/AlphaStaircase.app/Contents/MacOS/NBUnityProject -batchmode -logFile /tmp/nbfx-vfx-alpha-staircase-20260930/player-run.log --nbfx-alpha-staircase-output=/tmp/nbfx-vfx-alpha-staircase-20260930/player-output

Output: player-output/result.json and four PNGs; editor-material-inspection.txt plus generated-alpha{100,095,050}-{0..N}.shader. The Player exits 0 when alpha1 control and pass tags work, even if 0.95/0.5 fail; the purpose is diagnostic, not Gate passage. Material fields labelled `newMaterial*` are new Material(shader) defaults, NOT the hidden VFX renderer's runtime material. The Editor script also enumerates imported Material subassets and reflectively reads the VFX Output context's own hidden material if available; absence/reflection failure is logged, not treated as zero/default.

Interpretation: if alpha0.95 is invisible while alpha1 is visible, there is a near-binary Alpha path (clip/blend or actual VFX material state) rather than insufficient particle age. If alpha0.95 is visible but age-bound alpha is not, investigate dynamic alpha handoff/current generated shader and VFX material state. If both alpha0.95 and alpha0.5 are visible, test a biased age alpha expression (e.g. 0.5 + 0.5*t) in an isolated VFX variant, not product. Do not infer actual VFX material state from a freshly constructed Material(shader).

After test, archive evidence needed and remove temporary clone Assets/scripts/scene/metas; never push.
