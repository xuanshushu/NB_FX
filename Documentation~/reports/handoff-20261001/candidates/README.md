# NB_FX 未应用候选交接（2026-10-01）

这里**只是源码/测试候选归档，不是已集成实现或 Gate 证据**。未修改中央 Graph、Shader/HLSL、GUI 产品文件；这些候选没有经过 Unity 导入、GPU 渲染或 NUnit 实测。Parent/root 是唯一集成与 Unity 串行验证负责人。UVP 候选由另一 Agent 单独归档于 `uvp/`；本页与 56 件清单不包含该专属子目录。

## 固定输入及内容

四组新预览均以 FG1 后、997 objects 的中央 Graph SHA-256 `24bb84c9f37b34256f305a86dad0e3b4e4f74ee9db7895fec64f906723284da0` 为输入。任何后续源码或 Graph 变化都会使此处生成的完整 `.shadergraph` **过期**，不能直接复制覆盖新机上的 Graph。先在新机修正脚本内的旧绝对工程路径，再对实时产品源码重新运行动态脚本，检查 manifest 的输入 SHA、旧 ObjectId/端口及 diff；由 root 审核后按可回退切片应用。

| 目录 | 实际候选 | 当前已知边界 |
|---|---|---|
| `vat/` | V0 Houdini SoftBody 候选；动态脚本、7 个预览源码/`.meta`、manifest、原计划、consumer audit、verification slices、候选 Test/meta。最新预览保留 FG Fog 顶点源接线，VAT 关闭时两个 CF 静态原值直返。 | **VAT-off 仅静态 identity 检查，非 GPU 精确通过**。VAT 位置/法线下游消费者、真实贴图/阴影/光照、全部模式及 VFX 未验证；不将 SoftBody 当所有 VAT 模式。 |
| `flipbook/` | F0 普通 Mesh Flipbook 候选；动态脚本、8 个预览源码、manifest、原计划、36 行候选 Test/meta。保留旧 UV0.xy/zw、TEXCOORD3.x/y/z、Flags1 bit15/19/23，基于现有采样器双帧采样；没有新关键词。 | 36 行仅编写，**未运行**。DepthOnly/ShadowCaster、AnimationSheetHelper 组件生命周期、wrap/LOD、CA 优先、mask/normal 独立、GUI/VFX/Player 均待分片验证。 |
| `gui1b/` | GUI1B seed-only 候选；动态脚本/设计、root 和 latest 两份相同输入的完整预览及各自 manifest/diff、delivery、15 行候选 Test/meta。按原 22 toggle + 7 mode 绑定一次性把原 Flags 意图种入 29 个真实隐藏 Float，marker1→2。 | **不是完整 GUI 或 Tier**；不周期性把镜像写回 flags，不改旧原生编辑权威。离线 Roslyn 日志仅证明离线编译，不是 Unity import/Shader/NUnit 通过。历史 GUI1A marker1 断言需 root 校准而不能静默屏蔽。 |
| `gui2-two-input-design/` | 在同一 Graph997 上重新生成的 GUI2 设计样本，仅 `_NB_TierAllowNoise` 与 `_NB_TierAllowNoiseMask` 两个默认 1 的普通 Float 消费输入；附 manifest/diff。脚本在 `gui1b/design/build-tier-input-preview.py`。 | **完全不是 Tier 实现**：CPU normalized host reader、共用 resolver/projector、toolbar、有效 pass、其它功能 allow inputs 均未接。不能据此声称 Tier 生效。 |

上述候选互相都生成了**完整 Graph 预览**，不是可以顺次直接复制的补丁。尤其先应用某一片后，必须重新以最新中央 Graph 运行下一片的生产脚本，不能用本目录其他 `.shadergraph` 快照覆盖已应用的工作。旧 plan 中的对象数、slot ID、旧绝对 `/tmp` 路径只是编写时快照；以重跑 manifest 和实时源码为准。

## 迁移/重放顺序约束

1. 在新机读实时两个仓库状态、当前源码及本目录 `inventory.json` / `source-destination-sha256.tsv`。清单逐件记录原路径→归档目标、字节数和 SHA-256；归档时逐件校验源/目标字节一致。
2. 修正 VAT、F0 脚本内的 `/Users/bytedance/UnityProject/NBUnityProject/Packages/NB_FX` 常量，以及 GUI1B/GUI2 脚本的 `--source-package` 默认值/调用参数。`manifest.json` 和 diff 中旧绝对路径是证据，不是新机目标路径。
3. 先跑脚本的**预览**，核对当前输入 SHA、生成文件、旧 Graph 对象/属性/边的保留情况以及任何冲突；不要对未知版本强行应用。GUI2 应在 GUI1B 和其它实际功能片之后动态重跑。
4. 仅 root 能将审过的候选按切片写回产品、启动 Unity、读取编译/Console 并做 Frozen A/current B/Graph C GPU 证据。候选 Test 副本此刻不是已执行测试；待实现接入后再放回可执行测试程序集并跑。不要把离线编译、静态检查或空白日志写成 Gate 通过。
5. 不修改官方 URP/HDRP 包、UniversalTarget、NBPostprocess，不 push 独立候选。最终 Git 集成/推送由主 Agent 按用户这次授权统一处理。

本目录不含 DLL、Library/Bee、PackageCache 或官方二进制。`inventory.json` 和 `source-destination-sha256.tsv` 是附件完整性清单，不是功能验证报告。
