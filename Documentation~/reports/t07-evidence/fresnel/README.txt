T07 bounded Fresnel Graph Mesh and original ShaderLab regression, 2026-09-30

Product code 0ecb5b5. Current Unity 6000.3.18f1, URP/SG/VFX 17.3, Metal.
Original legacy Fresnel evaluation and composition moved to shared SurfaceV1;
ShaderLab keeps CustomData resolution, its _FRESNEL keyword and pass policy.
Graph adds world normal/view nodes and _fresnelEnabled/Unit/Color/Rotation;
existing Flags0 bit2 alpha mode, bit13 colorAffectedByAlpha, bit18 invert.
No new keywords/Pass. Graph Mesh tests use unperturbed normal and test-only
flat quad, not NormalMap, CustomData, 3D mesh tangent, or full VFX/GUI.

Mesh linear ARGBHalf center off (.033,.073,.133,.600), color
(.808,.014,.026,.802), affectAlpha (.808,.014,.026,.600), fade
(.033,.073,.133,.481), invert (.224,.059,.107,.600), rotation
(.362,.048,.088,.600), back (.808,.014,.026,.802). Pass10, exact
custom pass indices1/2, ShaderUtil messages5 (warnings, not clean Console).
First Mesh fixture had camera pointed away from the quad and failed; second
had an incorrect test expectation of .2 instead of .033 linear R and failed.
Both original logs retained. Only test fixture changed, product code did not.

Original ShaderLab current vs clone-only pre-extraction source from d847b59:
strict RGBA32 per-pixel diff0 across off/color/alpha/invert/affect/
rotation/back after four warm Camera.Render frames. Both Pass7 and 18
ShaderUtil messages. This is not T00 Frozen strict B/C.

Graph serial generator script archived; it was executed once to add four
properties, two geometry nodes and six CF inputs, then imported in clone.
The follow-up Graph GUI groups four implemented Fresnel properties under the
existing localized section label, keeping Unity MaterialEditor drawers and
unknown-property fallback. Clone CLI compiled exit0; visible GUI/Undo still
not tested, and split-flag controls are not synchronized.

No official package/Target/NBPostprocess/main Assets/ProjectSettings changes.
unityMCP unavailable. No push.

Default-off VFX compatibility follow-up: original five ColorAdjustment VFX
assets from the preceding report were NOT reserialized with Fresnel slots.
Rebuilt on product Graph 0ecb5b5: errors0/warnings36, Player exit0 PASS,
all five per-case numeric JSON fields equal previous baseline and six key
PNGs byte-identical. This proves the new default-off Graph inputs do not
change those existing VFX Outputs in the controlled case, not active Fresnel.
Clone test assets removed; cleanup CLI exit0, protected hashes unchanged.

Follow-up Mesh Graph Inspector packed flag controls (code bc33ebf): bit2
alpha mode, bit18 invert, bit13 color/alpha relation write existing Flags0
low/high 16-bit floats per selected Material, preserving every other bit;
Undo.RecordObjects and mixed state are implemented. Clone Editor test invoked
the private bit writer on two differently flagged materials, verified on/off,
independent writes and no-op; exit0. No visible GUI/Undo mouse test, VFX
Output inspector remains official and is not controlled by this Mesh adapter.
Cleanup compile exit0, protected hashes unchanged.
