T08 Mode2 VFX CameraOpaque isolation follow-up, 2026-09-30

Only test-only clone/player mutations. Product code unchanged at 0ecb5b5.
Unity 6000.3.18f1, URP/SG/VFX 17.3, Metal. unityMCP unavailable.
Mode1-named archived VFX is actually Output _NB_DistortionMode=2.

Fixture camera now requiresColorTexture=true. Player temporarily changes the
URP RendererData transparentLayerMask 55->49 (exclude VFX layers 1/2) while
retaining Camera.cullingMask and original NBPostprocess custom RendererList.
NBPostprocess OFF guard: Mode0 and Mode2 both exactly match background, proving
normal VFX transparent Forward is suppressed. State restores to mask55.

Clone-only post-Mode2-clip solid-magenta CameraOpaque sentinel: Editor build
errors0/warnings30; Player exit0 DIAGNOSTIC_RECORDED, fixture/isolation valid.
NBPostprocess ON Mode2 magenta676 pixels, Mode0 zero, repeat maxRGB0. Thus
original custom Pass selects/draws the VFX renderer at its event. Unisolated
Mode0/2 final difference remains zero, consistent with later Forward overdraw.

Restored original CameraOpaque sampling code, Editor build errors0/warnings30,
Player exit0 DIAGNOSTIC_RECORDED. With Forward suppressed, Mode2 changed130
pixels versus background at .005 linear threshold, maxRGB .00805664; Mode0
changed0, repeat maxRGB0. Unisolated final Mode0/2 remains equal because
opaque-ish double-sided cube Forward visually covers the subtle earlier copy.
The original Mode2 path works in this controlled isolation, but full ordinary
Mode2 composition/other alpha, intensity and mesh conditions remain untested.
Neither diagnostic status is a G5/G6 release. No product/official package/
Target/NBPostprocess changes. Clone test assets removed; CLI cleanup exit0;
settings/renderer/global hashes unchanged. No push.
