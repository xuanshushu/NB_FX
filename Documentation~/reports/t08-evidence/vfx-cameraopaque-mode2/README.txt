T08 Mode2 CameraOpaque true VFX Mesh controlled Player, 2026-09-30

Product Graph code unchanged. Unity 6000.3.18f1, URP/SG/VFX 17.3, Metal.
The file named Mode1 is test-only legacy fixture naming; its Output _NB_DistortionMode=2.
Original two assets differ only this Output scalar 0 versus 2.
Build: errors0/warnings30. Player exit1 CHAIN_MISMATCH, fixtureValid=true.
Both real single particles alive1, Pass5 with exact custom indices1/2 and 676 visible
pixels. Deferred Mask all zero (expected for Mode2), but Mode0 versus Mode2 final
linear ARGBHalf images are byte-identical, finalChangedPixels=0. With original
NBPostProcess disabled they are also identical. This does NOT validate Mode2 RT
or final screen-color distortion.

A clone-only diagnostic replaced the CameraOpaque custom-pass output with
solid magenta, then rebuilt (errors0/warnings30). Player remained exit1 and
finalChangedPixels=0; do not infer whether the custom pass did not draw or was
overwritten by the later VFX Forward pass. The diagnostic never entered the
product repo and was restored before clone cleanup. Original and diagnostic
logs/results/images retained. The first fixture generator run failed only
during its file-hash print loop because an Editor directory existed; asset
generation had succeeded and print loop was corrected in /tmp before build.

Test-only files removed from isolated clone; cleanup CLI exit0, protected
ProjectSettings, renderer and global hashes unchanged. unityMCP unavailable.
Do not use as G5/G6 pass, do not push.
