T07 BaseColorIntensityForTimeline, product NB_FX 609fd2a, 2026-09-30.

Test-only Mesh probe is archived here. Copy it to isolated clone Assets/Editor
as NBFXT07TimelineProbe.cs after syncing the product Graph HLSL and
.shadergraph at 609fd2a. Unity 6000.3.18f1 -batchmode -quit with
-executeMethod NBFXT07TimelineProbe.Run and UNITY_BURST_DISABLE_COMPILATION=1
produces the archived PNGs and log. Expected:
  NBFX_T07_TIMELINE default=1 centers=0,63,89,124 blackAlphaInvariantDiff=0
The four centers use intensity 0/0.5/1/2; the transparent black case uses
Alpha=0.6 and shows intensity 0/2 produce identical RGB, a limited alpha
invariance test. No direct GPU alpha readback or old ShaderLab B/C parity.

The first Graph port serialization accidentally used Vector4 for a scalar
function parameter, causing additional ShaderUtil implicit-truncation warnings.
That first log is archived. Before product commit, the port was changed to the
same scalar slot type as AlphaAll, and messages returned from 9 to the prior 4.
The successful log is mesh-probe-fixed-port.log.gz. Current generated Mesh and
fresh-imported product VFX sources are archived as generated-current.shader.gz
and generated-2.shader.gz. The VFX Output default is 1, but this import test
does not actively vary the Output value or run a Player.

Only isolated clone Assets/Editor scripts were used. No visible Editor,
main Assets/ProjectSettings, official package, UniversalTarget, NBPostprocess
or push was involved.
