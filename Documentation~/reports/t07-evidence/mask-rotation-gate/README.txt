T07 Mask1 rotation speed bit7 gate, isolated Metal Unity 6000.3.18f1.
Source commit: 5538dbd. Run archived NBFXT07MaskRotationGateProbe.cs.txt
as Assets/Editor/NBFXT07MaskRotationGateProbe.cs via -batchmode -quit
-executeMethod NBFXT07MaskRotationGateProbe.Run (do not use -nographics).
It writes PNGs to /tmp/nbfx-t06-20260930 and asserts:
- bit7 off, speed 0 vs 37: 0 RGB>2 differences, PNG bytes identical.
- bit7 on, speed 37: >0 differences (observed 750).
It reruns all prior Graph Mask gradient assertions. Test script and .meta
were removed from clone before cleanup import.
Protected settings SHA256 unchanged:
83180be7acb3bcdd8836e0298590f62eb6e96b2ff3f55a8c2271580c36e9da3b ProjectSettings/ProjectSettings.asset
720fdbd129c1b423a4e3e82fb5f19ee157a2bbccdc0c5ac7c9410bdcede0931d Assets/UniversalRenderPipelineGlobalSettings.asset
