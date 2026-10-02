# BackFirst / DisableMain：只读合同与默认URP routing诊断

未修改产品、官方包、中央Target或NBPostprocess；未安装/运行Unity/Git。8项fixture与source plan已准备，真实Unity/Bee references的离线Roslyn0error，保留8旧deprecated警告。

## 旧合同

BackFirst `SRPDefaultUnlit` 是原shader第一颜色Pass：Cull Front，Offset读两原参数，其余Blend/ZWrite/ZTest/ColorMask与完整Stencil继承SubShader。SubShader默认Transparent，实际material queue由原模式同步确定；不能因为该Pass额外Queue=Opaque字样就假定它独立走opaque路由。

有效条件：非UI、TransparentMode=Transparent、BackFirst toggle开启、该Tier允许 `pass.backFirst`。GUI仅3D透明显示，开启时主 `_Cull=RenderFace.Front(2)`，关闭不自动重设Cull，已有dynamic batching警告。Sync沿同一resolver/pass catalog写真实LightMode启用。

DisableMain只在实际有效screen模式开启且toggle为真时关主 `UniversalForward`；screen keyword依赖Noise、受Tier过滤，并排除UI。screen mode切0时原GUI同步将DisableMain置0。BackFirst intent与main独立，关闭main不等于关闭BackFirst；NB screen pass预算另独立过滤。

SVC/stripper仍准确保护旧shader名，BackFirst按原pass catalog/name或ScriptableRenderPipelineDefaultUnlit类型识别。Graph没有完整读/写/strip/runtime合同；新tag不能仅改Inspector就宣称功能完成。

## 当前 Graph 缺口与具体proposal

委托Unlit Forward实际没有显式LightMode，默认 `SRPDefaultUnlit`；名字 `Universal Forward` 是displayName，不是启用tag。若保持该main，再加同tag的BackFirst，Material tag启用API无法独立控制这两个职责。

先测默认URP行为，再考虑包内SubTarget的具体方案：给原main明确 `UniversalForward`，从现有完整color核心克隆一个 `SRPDefaultUnlit` backface Pass（Cull Front），保留blend/depth/stencil/offset/alpha全部合同。不改官方UniversalTarget或后处理，不增产品keyword；这属于未来pass/序列化/路由变更审查，当前只是proposal，未实现。

若默认pipeline只能选择一个pass，不能把probe读数写成BackFirst可用；保留实际结果，继续调查包内路由方案并提交具体决策。之后仍须真实前后面层叠、透明排序、所有状态/深度阴影/GUI/Tier/DisableMain+screen组合、Player/SVC/perf。

## 8项真实routing probe

in-memory两pass shader，各自实际tag是 SRPDefaultUnlit 和 UniversalForward，独立红/绿 additive output；改变physical pass先后2 × opaque/transparent queue2 × 两相机2。每项跑都禁用、SRP-only、Forward-only、两者都启用，记录真实GetPassName/FindPassTagValue/GetShaderPassEnabled读回与浮点raw/repeat。

使用实际Renderer/Camera/default configured URP，仅临时禁用已有NB post feature避免无关输出，不加额外DrawRenderer或定向feature强迫pipeline多pass。finite、repeat0、两单tag strong>.1、visible>128严格断言；两者同时开启时数值结果记录为一个或两个pass贡献，不预设答案。默认DrawObjectsPass源码91行tag列表只是待验证输入，不能当GPU结论。

JSON保存所有当前源hash/行号，plan身份来自源码而非实际discovery。这8项不验证BackFirst的Cull/alpha/stencil/offset和所有GPU平台，不放行Mesh/VFX Gate。
