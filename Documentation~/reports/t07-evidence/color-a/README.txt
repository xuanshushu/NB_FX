T07 ColorA checkpoint, 2026-09-30. Product code: NB_FX a5d4249.

This folder contains the isolated, headless Unity 6000.3.18f1 / URP+SG+VFX
17.3 / Metal Mesh test. It does not contain product assets. Reproduce only in
the isolated clone /tmp/NBFXG2DissolveMaskProbe-20260928, not the main
project's Assets or ProjectSettings. Copy the archived C# probe to the clone
Assets/Editor as NBFXT07ColorAProbe.cs, then run Unity -batchmode -quit with
-executeMethod NBFXT07ColorAProbe.Run and UNITY_BURST_DISABLE_COMPILATION=1.
The probe writes temporary screenshots under /tmp/nbfx-t06-20260930; archived
images here are the four ColorA frames only. Remove the test C# file and run
Unity again to confirm clean compilation/readiness. The subsequent T08 numeric
VFX fixture cleanup did so; see its cleanup.log.gz.

Expected logged assertion:
  NBFX_T07_COLORA redDiff=1764 alphaWithoutBit29=0 alphaWithBit29=1764
These are RGB changed-pixel counts (>2 channel difference), not alpha float
readback or old ShaderLab B/C parity. The other T06/T07 assertions in the
probe's log also passed. No visible Unity Editor was opened. No product or
official package edit was made by the test. No push.
