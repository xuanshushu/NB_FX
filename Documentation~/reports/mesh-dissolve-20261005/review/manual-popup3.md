三个原Popup身份保留人工待验。原自动GUI11挂在Native TrackPopupMenu，没有completion；禁止再运行自动Popup选择来补数量。新增直接UV服务身份即使通过，也只证明服务事务，不能替代此页的原生菜单验收。

仅在既有隔离验证工程使用临时普通Mesh Graph材质（本片中央b227），不改主工程既有材质/场景。使用现共享NB Inspector，Tier Full并允许Dissolve/Mask/Ramp；开启Dissolve、Mask、Ramp。记录操作前完整材质状态，选择菜单由人工完成，最后恢复。不要给渲染或菜单加容差。

1. **Ramp Source**（`G4Dissolve_ActualRampSourcePopup_RawEnumUndoRedo`）：展开Dissolve Ramp，点击“溶解Ramp模式”，人工从“渐变”选“贴图”。确认菜单正常关闭、raw `_DissolveRampSourceMode` 0→1、父Ramp开关不变；Ctrl+Z完整恢复，Ctrl+Y完整恢复选择后状态，最后恢复原材质。允许Tier下Map有效值应为1；原raw intent、未编辑packed words和其它功能状态均保持。
2. **Dissolve Map UV**（`G4Dissolve_ActualUVPopup_MapLoSliceCylinderUndoRestore`）：所有其它UV槽先无Cylinder，记录材质；点击“溶解贴图UV来源”人工选Cylinder。预期只改该UV槽position14（Lo16 mask49152）与它的fold，`Flags1Hi16` Cylinder位16开启，MainTex UV槽/其它UV槽和UV Type words不变。Ctrl+Z/Redo分别完整还原前/后态；人工改回原Default UV后，fold及聚合Cylinder恢复，其它状态原位不变。
3. **Dissolve Mask UV**（`G4Dissolve_ActualUVPopup_MaskHiSliceCylinderUndoRestore`）：同上，在“溶解遮罩图UV来源”人工选Cylinder。该槽position16，仅`UVModeFlag0Hi16` mask3变化；Lo16（包括MainTex和Dissolve Map槽）/邻位/UV Type words保持。Cylinder派生、Ctrl+Z/Redo及恢复要求同第2项。

验收记录最少保留：Unity/API/源SHA、三项各自完成或失败、实际菜单选择过程、前/后/Undo/Redo/恢复材质状态。若菜单无法关闭，单列GUI交互阻断；若raw槽、邻位、派生、Undo任一不符，记真实协议/GUI失败。没有菜单实际人工选择的结果仍为待验，不计Passed。
