# 共享 ZOffset GUI 首片：未安装预览

源码仅3个existing文件：TABigBlockItem、NBShaderProtocolItems、NBShaderGUI；加9项backend测试与dynamic editor-state Graph append脚本。未安装、未生成中央候选、未运行Unity/Git。

TA原ZOffset block的构造抽成 `CreateZOffsetBlock`，旧TA和Graph使用同一 PropertyToggleBlockItem、两原 ShaderGUIFloatItem 与关闭归零回调。不实例化缺很多属性的整TA块，不另建状态管理、flag协议或keyword/Tier系统。

Graph Root预检两真实 ShaderPropertyType.Float `_offsetFactor/_offsetUnits` 与两个真实editor Float `_ZOffsetBlockFoldOut/_ZOffset_Toggle`，同Graph shader/mixed-host禁止。缺一即不构造、不显示、不写数据。只有ready时四个property被现有Graph fallback排除，避免重复控件。Graph GUI仍不解除general Apply/Tier保护。

新增两个editor state只有Float/default0，不占packed bit，没有HLSL节点/边。`append_editor_state.py` 必须先检测当前Target真实Offset collector声明，再从调用时当前Graph只追加这2 property、保留所有旧对象/接线；不会造两个render字段满足GUI。

事务审查发现原 PropertyToggleBlockItem 用户toggle前无明确完整Material Undo，原关闭callback直接 SetFloat 两值。现有Item仅加默认null的before-value hook；此Graph共享factory在用户编辑前调用已有 MaterialEditor.RegisterPropertyChangeUndo，toggle+关闭两offset进入同一原Undo事务。单独reset也先记录；Legacy与其它blocks仍default行为。未触动Flags、枚举、keywords、passes、Native surface同步。

9项是真实MaterialEditor/MaterialProperty/shared Item的backend事务回放：缺4字段/Integer拒绝、只构造ZOffset所需items、multi-select关闭归零与Undo/Redo一起恢复、enable保值/direct reset、混Graph/Legacy只读。测试没有伪称可视OnGUI点击或GPU Offset。Root真实Offset Target尚未正式集成，所以schema与安装依赖不能省略。

静态检查：真实Unity6000.3.25f1 Bee程序集响应参数的Editor源码与tests均离线Roslyn0errors；tests保留8项已有deprecated警告。首次Editor输出随意命名导致friend internal访问失败，改成实际 `com.xuanxuan.nb.shaders2.Editor.dll` 后通过，源码未变。编译不是Unity交互/验收；9项未执行。
