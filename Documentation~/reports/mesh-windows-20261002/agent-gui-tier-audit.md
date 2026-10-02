# GUI/Tier 续接审查与首片预览

任务 GUI-TIER。主 Agent 分配的源只读；实现仅落在 `gui-normalized-reader-preview`。没有改根产品、隔离包、中央 Graph、Context、Sync、Applier、Unity 资产、Git 或 worktree。没有使用 Unity。

## 当前能力

- 根产品保留 GUI1B/GUI1C；隔离包有 GUI2 Noise/NoiseMask 首对与 OverrideZ 等渲染候选。
- 当前 Resolver 的完整 legacy Resolve 仍只识别 `Effects/NBShader`；Graph 只有 `TryResolveGraphNoisePair`。一般 Apply、Runtime、Toolbar、Sync 仍受旧路径保护。
- Graph Context 的 `IsKeywordAllowed` 恒为 true，VAT/Flipbook 状态固定 false；没有完整有效 Tier。共享现有控件仅 MainTexBigBlockItem，其余是现有 GraphRoot 的 native fallback。
- 37 toggle binding 中，当前 Graph 有 28 个真实 Float；缺 9 项：StencilWithoutPlayer、SharedUV、BlinnPhongSpecular、6 个 Debug。缺字段不能作为关闭状态或 Gate 完成。
- 目前只有 `_NB_TierAllowNoise`、`_NB_TierAllowNoiseMask` 两个真实 gate；其它 gate 未实施。29 个隐藏 GUI mirrors 保留 marker 2 单次 seed / 原生 flags 编辑显式通知协议。

## 本次最小预览

接口：`TryResolveGraphSupportedKeywordIntent(Material, NBShaderFeatureTier, IEnumerable<string>, out NBShaderMaterialIntentResult, out string[] unavailableFeatureKeywords)`。

能力标识：`NBGraph.Mesh.KeywordIntent.v1`。在原 37 binding 中增加 Graph 支持元数据，不建立第二份映射。只读 28 项实际 Float 和现有 FxLight、Distort、Ramp、VAT 枚举，复用原 allowed filter、依赖核心和 common mode 解析。`passes=[]`，不猜 Pass、screen/time、UI/Particle、MPB 或 VFX 支持。

入口要求 marker 恰为 2，12 个实际 packed word 的 24 个半字均为有限 Float，28 个支持 toggle 存在且有限，现有模式是已知整数枚举。缺字段、Integer 伪装、未知 marker、未知 tier、非有限值和未知/小数枚举均拒绝。允许有限非规范 raw 半字值，且不会规范化它们。9 个不可用 keyword 单独按原 catalog 顺序输出。原一般 Apply 保护和 Noise 首对入口均保留。

原 flags raw 是当前 HLSL 的消费存储；29 个 GUI mirrors 的同步只有现有显式协议负责。读取此片不会 seed、不会以 mirror 反写 raw，也不会根据冲突猜权威。

文件：

- `gui-normalized-reader-preview/NBShaders2/Runtime/NBShaderMaterialIntentResolver.cs` 与原 meta。
- `gui-normalized-reader-preview/Tests/URP/Editor/G4GraphNormalizedIntentReaderTests.cs` 与新 meta。
- `gui-normalized-reader-preview/manifest.json`：输入/候选 SHA、范围与静态 113 个 case 身份。

C# managed syntax 使用最小非 Unity stub 检查通过。首轮 syntax harness 漏了 `using UnityEngine`，补 harness 后通过；不是产品故障。没有 Unity 编译、NUnit 执行、GPU 或 Gate 通过声明。

113 个已编写 case 覆盖实际 imported Graph Material、4 Tier、28 toggle 阈值、原始有限半字与 GUI mirror 冲突的 no-op、16 官方表面组合、5 光照模式、10 VAT 模式、3 依赖枚举、缺/错类型 schema、未知/非有限状态和旧 Apply/Noise 保护。由主 Agent 安装后串行运行。

## 需要独立关闭的边界

1. 官方 URP 17.3 `BaseShaderGUI.UpdateMaterialSurfaceOptions` 中，`_Blend=Premultiply/Additive` 不等于旧 NB `_ALPHAPREMULTIPLY_ON`；该 keyword 只来自可选 PreserveSpecular，Multiply 对应 `_ALPHAMODULATE_ON`。首片按官方规则读取。原 BlendClip 夹具手动设置 blend factors/keyword，不能证明原生 Inspector 等价。
2. Graph 主 Pass 名为 `Universal Forward`，旧主 Pass 名为 `UniversalForward`；Graph 的额外 DepthNormalsOnly 还没有默认完整链路审核。现有 BackFirst 也没有直接有效 Graph 等价实现。不能复用 legacy ResolvePassIntents。
3. Graph screen 实际模式是 `_NB_DistortionMode`；旧 `_ScreenDistortModeToggle`、`_DisableMainPassToggle` 不存在。需要独立明确的状态能力。
4. Graph UV adapter 当前 `parameters.timeY = _Time.y`，无 `_TimeMode`；unscaled/scriptable time 不能由 raw flag 存在推断已消费。
5. SharedUV 算法/SpecularColor 输入存在，但其旧控制字段缺失；两项 gate/GUI 必须各自判断实际合同。
6. SixWay 与 OverrideZ keyword 同步当前跟随直接意图；以后接 Tier 时必须与有效模式/gate 协调。

## 后续共享 GUI 路线

沿用 NBShaderRootItem/Context/SyncService/FeatureBigBlockItem 和既有 FeatureItem。逐块核对构造要求后增补真实 Float UI foldout 与属性薄映射，再开放相应 block。现在只有 MainTex 两个 foldout，直接实例化整个 FeatureBigBlockItem 会触碰缺属性；Noise 控件还要求旧 screen 字段、调试字段等，不能批量硬接。GUI mirror→raw 仅在真实用户编辑事务内写；原生 raw→mirror 继续现有显式通知。

Tier 仍由同一 ProjectSettings/Preset 的 allowed keyword/pass sets 定义：Low/Medium/High/Ultra = 0/1/2/3；当前默认 keyword 数为 14/21/40/65。数字本身不代表固定预算，允许项目覆写；Runtime 不带 settings 时每 Tier 都允许全部 catalog。不得为了新 Graph 修改旧行为。

下一片可由主 Agent审核 reader 后，按真实消费点分组补 gate；每次基于当时中央 Graph 动态追加，禁止用旧整图覆盖。完成全部 renderer/enum/Pass/Tier 派生状态后才解除一般 Apply/Toolbar/Validate 的 Graph 保护。

## 串行执行入口补齐

`gui-normalized-reader-preview/plan.json` 保存 113 个 exact fully qualified identity；84 个静态 TestCase 加显式 TestName、3 个单 Test、16 Surface 和10 VAT 源循环。来源是本次源码，不是 live discovery；旧 runner 的 expectedCasesFromActualDiscovery 字段仅保留兼容并显式标注 source。CPU 全部建议一个串行进程，filter 为完整 fixture 类名。

`installation-manifest.json` 的 records 是 canonical files 的只读一致别名。仅测试显式名称变化，Resolver SHA不变；测试SHA及字节数已更新。默认字段源审查见 default-field-range-audit.json：marker原默认0，须显式seed为2；FxLight/Distort/Ramp/VAT全部默认0在已知整数范围，24半字字段存在。URP Target默认Surface1/Blend0/Clip0符合范围；native Shader property类型仍待Unity实际读回。
