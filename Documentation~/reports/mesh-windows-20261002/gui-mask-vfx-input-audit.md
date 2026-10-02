# Mask gate / 历史 VFX 样例导入审查

本任务只读源码和原证据，仅写本审查与 JSON。未改安装源、样例、Graph、官方包或 Unity 状态。主 Agent 在诊断期间独立应用了最小 metadata 修复。

## 根因

SG/VFX 17.3 对 exposed+hidden property 的两条路径不一致：

- 官方 `VFXShaderGraphHelpers.GetProperties` 在116行使用 `hidden || !isExposed` 排除 VFX context slot。
- 同文件 `GetShaderGraphParameters` 在317–323行只看 `isExposed`，为该 stage property 返回 `exposed=true`，忽略 hidden。
- `VFXSGInputs` 84–88行按 exposed name 查询 GPU expression；找不到 `_NB_TierAllowMask` 就抛异常。

最初失败 Graph `7abf802d...` 中三个 Mask allow 都是 `m_Hidden=true`、`m_GeneratePropertyBlock=true`、Float/default1/默认 UnityPerMaterial。已处理过的 Noise pair 同样 Float/default1/默认声明，但 `m_Hidden=false`，所以能创建 VFX context slots。三个 Mask 的 range metadata 继承65535，但 FloatType=0，不是本次缺 expression 的原因。

## 最小包内 schema 修复

仅将三个 Mask allow property 的 `m_Hidden` 从 true 改 false；UUID、属性 Float/default1、generatePropertyBlock、CBUF 声明、CF slots/接线、Target/DN0、flags/关键词/状态协议全部保留。

现有 `NBShaderGraphRootItem.IsVisible` 在322行已按 `_NB_TierAllow` 前缀排除这些派生字段。因此 metadata non-hidden 仍不会在 NB 材质控件中开放编辑，与 Noise pair 的既有处理一致。无需新 VFX Output、第二套 GUI、官方包、UniversalTarget 或 NBPostprocess 修改。

主 Agent 当前 Graph `ec19d5f2...` 与失败输入逐对象比对，恰好只有这三个 `m_Hidden` 字段变更；HLSL仍 `87b89d9d...`。此审查没有读取修复后的 XML/导入结果，不声称修复已验证。仍须实际无错误重导入旧样例，再执行 Helper bodies / 相关 Mesh 状态验证；样例导入兼容不是正式 T08/T09 VFX 验收。

不建议仅把 generatePropertyBlock 改 false：官方默认 HLSL 声明会从 UnityPerMaterial 变为 Global，可能破坏材质 gate 和 SRP Batcher 合同。不得忽略 LogAssert error 或将缺执行算通过。

## 初轮结果分类

`flipbook-helper-lifecycle16-plan-fixed-isolated-1/batch-1/results.xml` 实际16项全部 Failed，`site=Parent`，message 以 OneTimeSetUp / unhandled VFX importer Error 开头。Helper 功能测试正文未执行；保留16失败和原 XML/log，分类为历史 VFX 样例 schema 导入阻塞，不能据此得出 Helper 功能失败或通过。

## 如仍阻塞，Mesh 可独立暂存两个文件

仅隔离工程 `Packages/NB_FX/NBShaders2/ShaderGraph/Samples/NBGraphVFXMeshMinimum.vfx` 及其 `.meta`；错误日志明确指向此资产，其2558行引用当前中央 Graph GUID `63c742d3d39d403693767ca92e94d789`。另一 GF_VFXOutput_NBSubTarget 不引用该 Graph，无需暂存。

提出的目标是主工程 `.utmp/nbfx-resume-20261002/nbfx-historical-vfx-quarantine/`，处于隔离 Unity project 外。主 Agent 执行前核对源/目标、loaded scene 和即时 SHA；两文件一起精确暂存、恢复，保留 GUID/原字节。JSON 保存完整路径和当前 SHA。未在 isolated Assets/package 的 `.unity/.prefab/.asset` 源文件找到该样例 GUID 引用；这不替代主 Agent 的 loaded-scene 检查。

这是可选 Mesh 继续路径，真实 VFX 兼容问题仍须保留待办。样例 source 在本次读取期间从首读 `fe1c4d92...` 变为当前 `efcd8fdb...`，对应主 Agent 导入后的源状态；不能用旧 SHA 做移动/恢复断言。meta仍 `b82fdf07...`。诊断子 Agent 未写样例。

精确 property patch、官方文件 SHA/行号、XML/log SHA、前后 Graph 和两文件暂存建议都在 `gui-mask-vfx-input-audit.json`。
