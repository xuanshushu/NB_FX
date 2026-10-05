# Mode / TA / Toolbar 原ordinary Mesh清单一次收口

Mode：MeshSource缺省固定D01 ordinary3D，UI/Sprite/UIParticle/Universal2D不造模式字段；Surface/Clip/Cutoff/Blend/Cull/ZWrite/ZTest仍官方URP桥。原AddToPremultiply slider复用同class，Graph只补Deferred Float写/重置，Blend明确变更Premultiply1→scalar1、Additive2→scalar0；不在paint/Validate初始化写，不接管官方Blend/keywords。该标量消费Forward现已实现，算法不改。

TA：Offset/OVZ/Queue/ColorMask/Stencil已接受；原EnabledKeywords同KeywordListItem复用单材质只读，Float fold状态，Graph读取实时keyword变化而不写状态。不存在新的shader keyword。

Toolbar：同原DrawToolbar/按钮/菜单。Ping/Settings/Help保只读行为，测试不实际打开外链/窗口打断；Copy/Paste仅同Shader completeknown Graph单材质，拒Native/future/unknown剪贴板与mixed host；不换shader或暗迁Pass。Collapse只准确Root原Float fold集合，不按名字扫未知Shader字段。CleanUnused仅实际owned且raw明确关闭的PropertyToggle叶纹理；active/Tier denied但raw on/未知字段保留。清理真实服务立即执行，非在行尾Request后被finally清掉。Native旧路径完整保持。

Graph写动作复用accepted完整Undo＋Serialized ObjectReference回滚的同Sync事务，独立GUI工具不FullApply所有gates/pass；原GlobalReset只保旧final投射路径。所有新动作整选pure schema预检、后置合法性，拒future/illegal；新UI两个原Floatfold metadata各图fresh增量，HLSL/Target/Runtime/Gate不改。

14已通过风险结果复用；有限实际callback/Undo/passive/拒绝证明。原菜单选择/颜色Picker自动化边界保人工，不声称实际点击。TimeMode旧无消费者仍knownbaseline，本片不造time模式功能。
