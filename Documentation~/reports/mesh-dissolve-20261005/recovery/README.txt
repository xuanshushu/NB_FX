2026-10-05 recovery result

The user explicitly authorized Unity operations, including background operation, without repeated questions.
Only task-owned isolation Editor PID64312 was restarted. PID/start time/exe/project were verified first.
The main project Editor PID85496 was untouched. No source/assets were discarded.
Last actual isolation scene was clean Untitled; the blocked GUI fixture does not write scenes or serialized assets.

Evidence:
- Mini dump and native stacks: D:/UnityProject/NBUnityProject/.utmp/NBFXMeshValidation-20261002/Temp/UnityEditorDiagnostics/Unity_64312_20261004_194123
- Native main thread: NtUserTrackPopupMenuEx -> Unity ShowDelayedContextMenu -> Internal_SendEvent.
- Computer Use application permission first timed out overnight. Later window input failed GetCursorPos E_ACCESSDENIED and capture monitor failure with disconnected Windows desktop.
- No Computer Use input succeeded. No login/security UI was operated.
- Native menu batch gui11 exceeded300seconds and had no completed result; it remains quarantined. GPU2 had not started.

Recovered by restarting ONLY the verified isolation Editor, hidden, same project and -force-d3d11.
New PID22320. Actual CLI confirmed clean Untitled scene, not compiling/updating, no test windows, evidence env null, Pipeline.GetTestStatus null.
Background CLI, programmatic non-popup GUI events and rendering now work without unlocking the desktop.
Original3 native Popup tests remain in source and manual-pending; do not dispatch them again automatically.
8 non-popup originalGUI +2GPU passed after recovery;2 added direct UV-service/Undo identities also passed.
These service tests do not claim native menu selection. Original13 source bytes/assertions remain intact inside the extended fixture.

For future recovery, inspect actual target and preserve evidence first. Do not target the main project or another Unity project.
No approval is needed again for already authorized Unity operations, but observe actual TAI loaded-scene protection and avoid Windows authentication/security UI.
