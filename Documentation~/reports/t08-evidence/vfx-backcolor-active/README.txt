T08 BackColor active VFX Mesh Player, 2026-09-30

Environment: Unity 6000.3.18f1, URP/SG/VFX 17.3, Metal, linear ARGBHalf.
Unity MCP unavailable; exclusive isolated CLI clone only. Source generator and
clone-only C# probes are archived here. Do not copy test assets into product.

Final fixture: real one-particle VFX Mesh Output on an explicit single-sided
Quad; both VFX objects have the SAME transform. The camera is placed on +Z or
-Z to view opposite mesh faces. BaseMap white, _Color white, _BaseBackColor red.
Only Flags0 bit28 differs between Off and On Output assets. Editor build checks
the generated shaders for exact slot values, face sign, and custom Pass tags.
Player reads linear ARGBHalf final color on a common 576-pixel interior ROI.

Final: output-camera-sides/result.json status PASS, player exit 0. Off +Z/-Z
both white and identical; On +Z white, On -Z red, all 576 ROI pixels changed;
four cases each alive1, Pass5, custom Pass indices1/2, repeat maxRGB0.

Retired orientation fixture: the first probe rotated the VFX GameObject by
180 degrees while holding the camera fixed. Both On views stayed white;
output/result.json and player.log.gz preserve this failed hypothesis. VFX
particle/output transform did not provide the intended face control. A
clone-only diagnostic temporarily forced BackColor whenever flag was on;
both On views then became red, proving binding but not front-face selection.
The final camera-side fixture replaced it. Diagnostic HLSL was restored in
the clone before the final build; never committed to product.

This is a bounded active Output color/Pass test, not legacy ShaderLab B/C,
postprocess RT/Uber, transparent sorting, other devices, or G5 approval.
Reproduce by using prepare_assets.py.txt and the archived C# fixture as
test-only files in a separate Unity 6000.3.18f1 clone; use the final camera
side C# code, build with NBFXBackPlayerBuild.Run, run the built app with
--nbfx-back-output=<outside-repo-dir>, and inspect result.json. No -nographics.
