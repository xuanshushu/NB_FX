# 根 VAT 动态重基入口

本目录只有 builder、已验证的 VAT 叶源、两个 VAT 节点的局部模板、只读审计和待运行计划。尚未生成/安装根候选，未运行 Unity。当前根还没有真实 F0 `UV3` 输入，不能用当前观察 JSON 充当下一片的输入锁。

建议第一片采用共同真实接口，输出 12 文件。Houdini 四模式、Tyflow 六模式已经共享一个验证过的完整 adapter ABI；先裁剪 Tyflow 或 Houdini 会额外制造未经验证的临时接口。该片之后可以按实际新根单独回退或继续 CustomLocal，不复制旧 wholeGraph。

| 输出 | 来源与范围 |
| --- | --- |
| `NBShaders2/ShaderGraph/NBShaderGraph.shadergraph` | 每次读取指定的当前根 Graph；仅新增 VAT/VATWorldBasis 与必要属性、原生 UV 节点闭包 |
| `NBShaders2/ShaderGraph/Editor/NBGraphUnlitSubTarget.cs` | 在既有两种 NB 屏幕 Pass 的公共 builder 加一条固定 `NB_GRAPH_NO_VAT` define；保留原 Pass 布局和状态 |
| `NBShaders2/ShaderGraph/NBGraphVATSoftBody.hlsl` 及 meta | 十模式验证归档的完整真实接口；保留实际 `SHADOWS_DEPTH` guard，不猜测 ShadowCaster 别名 |
| `XuanXuanRenderUtility/Shader/HLSL/HoudiniVAT.hlsl` | 验证过的既有 wrapper 委托；根旧源必须仍为 `5984e8ae…`，漂移则拒绝并重新抽取审核 |
| 同目录 `HoudiniVATMathV1.hlsl`、`HoudiniVATKernelV1.hlsl` 及各 meta | 已验证共享数学/四模式 kernel 原字节 |
| 同目录 `TyflowVAT.hlsl` | 验证过的既有 wrapper 委托；根旧源必须仍为 `5091f80d…` |
| 同目录 `TyflowVATKernelV1.hlsl` 及 meta | 精确 Gamma 标量、Graph host 七骨 `[loop]`、原生 `[unroll]` 修复后的原字节 |

`resource-provenance.json` 给出十个固定叶源、局部模板和原归档 SHA。资源文件没有 GraphData、根边列表、BlockNode、CategoryData 或其它功能节点。原归档仅在离线 self-test 内存中读取。

根 F0/CD 提交完毕后，由主 Agent 执行两个分开的调用。第一步只读捕获并审核输入锁，第二步才生成一个新的候选目录：

```powershell
& 'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' 'D:/UnityProject/NBUnityProject/.utmp/nbfx-resume-20261002/root-vat-rebase/build_root_vat.py' --input-package 'D:/UnityProject/NBUnityProject/Packages/NB_FX' --capture-input-lock 'D:/UnityProject/NBUnityProject/.utmp/nbfx-resume-20261002/root-vat-rebase/fresh-root-after-CD-input-lock.json'
```

```powershell
& 'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' 'D:/UnityProject/NBUnityProject/.utmp/nbfx-resume-20261002/root-vat-rebase/build_root_vat.py' --input-package 'D:/UnityProject/NBUnityProject/Packages/NB_FX' --expected-input-lock 'D:/UnityProject/NBUnityProject/.utmp/nbfx-resume-20261002/root-vat-rebase/fresh-root-after-CD-input-lock.json' --output 'D:/UnityProject/NBUnityProject/.utmp/nbfx-resume-20261002/root-vat-rebase/generated-after-CD-1'
```

输入锁冻结 NBShaders2/XuanXuanRenderUtility 的实际源与 meta SHA、所有 CF 顺序/类型/ID/源码 GUID；既有 HLSL 的 float/half 参数顺序逐一核对。捕获/生成要求真实 OVZ、UVP、F0、四 word CD、实际 TEXCOORD3、实际 Float word2，且根尚未含 VAT/CustomLocal。没有自动接受漂移、旧源覆盖、占位 word2、伪 UV3 或已有输出覆盖路径。目录锁字段不声称 Unity 导入/编译成功。

Graph 的七条既有输入边只允许如下修改，其它原对象/CF ABI/边逐一保留：

1. VO PositionOS/NormalOS 接 VAT 输出，保留原 VO 和最终 Vertex blocks。
2. UVP vertex PositionOS 与 Fog PositionOS 接同一 postVAT/preVO 输出。
3. SixWay 的 N/T/B 接 VATWorldBasis；该节点读新 VAT normal 与原有世界基。

VAT 的 UV0/1/2/3、Flipbook toggle、Flags1、CD word2 从新根的实际已有节点读取，UV4/5/6/7 只在缺失时加入原生 TEXCOORD 节点。根 OVZ、UVP、F0、CD 接线和其它属性不会被 archive 属性覆盖。新增 VAT Float 的默认值还须与当前原生 NBShader.shader 的真实默认值完全一致。

离线自检已在内存通过：1199→1444 对象、七边修改、原六个 CF ABI/其它旧对象边保留、确定性输出、十叶源 SHA，以及六类错误拒绝。该归档早于 OVZ，因此 self-test 只覆盖其真实既有六个接口；公开捕获/生成仍强制实际根 OVZ。没有把自检结果当根候选或 GPU 验证。

主 Agent 的串行窗口应先核对工程占用/已加载场景，再审核安装清单和实际 SHA，导入编译、核对 discovery/XML/生成 Shader 日志。建议运行顺序：

1. `root-vat-compile-storage1-plan.json`：只用既有 storage1 作为 import/warm 预检；功能证据另计。
2. `root-vat-first16-plan.json`：两相机 SoftBody/法线 SixWay、其它 Houdini 三族、正 Gamma/七骨插值与 CD word2 frame 控制。
3. `root-vat-rest146-plan.json`：补全部剩余原身份，包括十模式实际 DepthOnly/ShadowCaster、NB 精确屏幕 Pass 和额外 Gamma；与 first16 是 162 个去重身份，旧轮通过数不累加。
4. `root-vat-impact177-plan.json`：真正受本片改动影响的已有 VO/Depth/Shadow/SixWay/Fog、F0 屏幕、positionUV main、OVZ depth 与 Graph schema 原身份。保持原误差/finite/visible/repeat/强响应。
5. `root-vat-nbpost-controller12-plan.json`：原真实 Manager/Controller/RT 链在更新后的根重新验证。该夹具不包含 animated VAT screen 组合，不能据此声明它完整覆盖。
6. 之后动态重基 DN0，再运行 `root-vat-after-dn0-default-ssao2-plan.json` 的真实默认 Forward+Depth+Shadow+Normals rawTrue/SSAO 链。限定 Pass 的40/4不替代默认链。完整 coverage16 中 CustomLocal 四个身份须等实际根 CustomLocal；normal2 零响应保留 UNVERIFIED 严格失败，等待 draw-time 实证，原透视微差继续人工验收。

`test-fixture-readiness.json` 固定当前隔离夹具观察 SHA/修复入口，供安装前核对。不要用旧 core 覆盖恢复后的唯一 private 三参数 Reflection ABI。Forward 启用证据必须查询真实 LightMode；displayName 只用于 FindPass 索引。

上述原身份回归仍未覆盖 animated VAT 与全部 UVP/F0 组合、全部 GUI/Tier、正式 Player/变体/性能。普通 Mesh Gate 仍待最终组合与默认链审核。
