# GUI-TIER-MASKS-1：预览待审核

仅 Mask1/2/3 三个派生 gate。没有改当前源，没有运行 Unity，也没有执行 Graph builder。

接口 `ApplyGraphMaskGroup(Material, tier, allowedKeywords, out changed)` 使用已经通过 113 项实跑的 `TryResolveGraphSupportedKeywordIntent`，同一 dependency core 决定父 Mask 被过滤后两个子 Mask 的有效值。只写 `_NB_TierAllowMask / Mask2 / Mask3`，默认均 1；保存的开关、raw flags、GUI mirrors、枚举、keywords、Pass 与 native URP 状态保留。一般 Apply/Toolbar/Validate 保护保持。

真实 mapping：

| 原属性 / keyword | CF 参数 | 派生 Float |
|---|---|---|
| `_Mask_Toggle / _MASKMAP_ON` | `MaskToggle` | `_NB_TierAllowMask` |
| `_Mask2_Toggle / _MASKMAP2_ON` | `Mask2Toggle` | `_NB_TierAllowMask2` |
| `_Mask3_Toggle / _MASKMAP3_ON` | `Mask3Toggle` | `_NB_TierAllowMask3` |

三参数追加到 float/half CF 输入尾部，原 input 顺序/类型保留；函数体入口把这三个 local toggle 与 allow 相乘。没有新增 keyword/packed bit。

`build_graph_mask_gates.py` 必须由主 Agent 在组合源恢复后调用。它在调用时读取当前 Graph/HLSL；克隆实际 Float property/PropertyNode/slot，追加三属性、节点、输入 slot 与边；逐一断言已有对象、旧 slot refs、属性值、Target/interpolator 与边保持。输出只能是本目录下新的子目录，不覆盖现存候选，不写 source。

静态 HLSL 预览来自本片最初组合输入，用于审查。正式候选安装优先使用 builder 在最新组合源上生成的 HLSL/Graph，而非把此静态快照覆盖到后续源。

输入 SHA 在 `input-source.json` / `manifest.json`。当前 `SourceStable=temporary-root-scope`：主 Agent 暂时安装 root-only OverrideZ 做 234 回归，原组合源在 `overridez-combined-before-root-slice` 精确备份。Applier before SHA 保持 `8b625ed4...`；还与已存 GUI2 源归档字节匹配，不采临时 root applier 的新 SHA。

`plan.json` 为 source identity：CPU 37（32 个开关×policy、恢复/no-op/普通 Apply 保护、marker、缺/错类型派生 schema）；GPU 6（Forward/两 NB Pass × 正交/透视）。GPU 三态是意图→剥离→恢复，保留 A/B/C finite、visible > 128、全帧 AB/BC/repeat/restore 0 误差与强响应 > .001，复用已验证 GUI2 的真实 RT/定向 NB Pass capture、现有 Snapshot；环境配置和原 Renderer 文件字节恢复。

源码确认旧 Shader 与 Graph 两种 NB screen Pass 的 coverage 都消费完成 Mask 后的 `alphaBeforePremultiply * NoiseMask`。因此两 NB Pass 有真实 Mask 响应，不伪造排除测试或用空图算一致。

检查：Python AST 通过，builder 未执行；Applier 与 reader 的非 Unity stub C# 语法检查通过。Unity 编译、NUnit、GPU 均未运行；没有 GUI/Tier/G4 放行声明。

Revision 2 补充实际 HLSL ABI 审计，见 `abi-order-audit.json`。官方 SG 17.3 `CustomFunctionNode.GenerateNodeCode` 先分别取得 Input/Output slots，再传全部 input、全部 output。原 raw slots 中 Out 在位置3，另外两个 output 在中部；不能把新 gate 插在第一个 raw Out 前。继续 raw append，删除3新ref后与旧raw list精确相同，并在builder中断言 filtered call 的全部226个参数依次匹配 float/half 两签名。

末12个实参依次为 LocalToWorld2、LocalToWorld3、WorldToLocal0–3、三个 Mask allow、Out、NBDistortionSignedRG、NBDistortionNoiseMask。half 是独立完整实现，没有内部 float 调用，两个入口各只乘一次 gate。builder 遇到未来 half→float wrapper 会拒绝自动改ABI，需另行审核转发参数。以上是源码审查，不是 HLSL 编译证据。
