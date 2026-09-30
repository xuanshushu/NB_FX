Ordinary Mesh bounded B/C and generated Master Preview, 2026-09-30.
Parent input 9d0f998; package input bbe0a72; product code 6f51533.
Unity 6000.3.18f1 / URP+SG+VFX packages 17.3 / Metal / RenderGraph.
NO true VFX Output is used. Support VFX Graph remains enabled on the product Graph.

Replay in an exclusive isolated clone. Product package must match input-sha256.txt.
Use Tests/URP/Editor/G4GraphMeshParityTests.cs and existing NBFX.G2.URP.Editor.Tests
assembly. Restore the archived .cs.txt if replaying after test changes.
Set NBFX_MESH_EVIDENCE_DIR=<output> and UNITY_BURST_DISABLE_COMPILATION=1.
Unity -batchmode -projectPath <clone> -runTests -testPlatform EditMode
 -testFilter NBFX.Baseline.Tests.G4GraphMeshParityTests
 -testResults <results.xml> -logFile <unity.log>
Do NOT add -nographics (Metal rendering required) or -quit (runner exits).
macOS sandbox may prevent GPU/application startup; run in an authorized host environment.

88 ordinary Mesh ShaderLab(B)/Graph(C) tests (44 presets x ortho/perspective)
and 4 runtime Graph(B)/generated Master Preview(C) tests. All strict full-frame
RGBA differences and B/C repeat differences are zero on linear RGBAHalf.
88 active presets have >0.01 RGBA difference from the disabled control;
4 base presets are deliberately unchanged. Feature-combination controls prove
some feature activity, not each individual feature's influence in that combination.

Each case has B/C/C-disabled previews, gzip float32 little-endian RGBA readback
(96*96*4 floats, row-major y from bottom), metrics and B/C material values.
B/C-material.json.gz stores property values, keywords and texture ST; texture
labels are not durable asset IDs. The fixture source and base-texture.png define
in-memory texture bytes; whiteTexture is Unity's built-in white texture.
PNG is a clipped 8-bit preview only; all equality metrics use the raw floats.
Four camera Render calls precede EVERY capture: this is steady-state evidence,
not a cold first-frame test and not a waiver of the recorded Dissolve issue.

Master Preview reflects SG17.3's same Generator/GenerationMode.Preview entry
used by PreviewManager, then creates and actually renders the generated shader.
This is not visible Graph Editor window/thumbnail interaction coverage.
Only non-scene-depth Preview presets were tested; Preview substitutes far depth
and does not prove runtime depth sampling parity. Generated Preview code archived.

Fixture correction: before-correction.xml/log retain 90 passed/2 failed.
Both failures were positive-control setup for vertex-color: the 'disabled'
material also consumed the nonwhite Mesh COLOR, so the On/Off delta was zero.
B/C raw RGBA differences were still zero. Corrected only that disabled control's
Flags1 bit9 (ignore vertex color); 92/92 then passed. No product shader changed.

No full G3/G4 clearance. No advanced UV/Noise/Normal/POM/MatCap/lighting/Fog,
VAT/VertexOffset, advanced depth/shadow/stencil/pass states, full NBPostprocess,
GUI persistence, Player, performance, or version/platform support is inferred.
Protected settings hashes stayed unchanged; preview scenes/materials/textures
were destroyed without saving source assets/settings. Unity log contains existing
ShaderUtil warnings and licensing/service warnings; not a warning-free assertion.
