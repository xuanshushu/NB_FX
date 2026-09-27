# T01 当前环境依赖边界（D21 后的 G1 输入，2026-09-27）

只读静态审计；未调用 Unity Editor。主工程 `21f9695`、NB_FX `f33fcf4` 时采样。结论仅针对当前项目 Unity 6000.3.18f1 + URP / Shader Graph / VFX Graph 17.3.0；旧版本与 HDRP-only 按 D21/D20 移至后续，不阻挡此项 G1 审核。本文不表示整个 G1 已放行。

## 当前环境已核实

| Case | 观察与证据 | 判定范围 |
|---|---|---|
| DEP-01 包解析 | `ProjectSettings/ProjectVersion.txt` 为 6000.3.18f1；`Packages/manifest.json:3,8,11` 安装 Cinemachine 3.1、URP 17.3、VFX 17.3；`Packages/packages-lock.json:123-130,155-162,216-229` 确认 SG 17.3 由 URP/VFX 传递。包缓存 URP/VFX `package.json:4-10` 均要求 Unity 6000.3。`Packages/NB_FX/package.json:1-7` 自身无依赖声明、`unity=2021.1`。 | **当前项目组合存在并与包最低 Unity 匹配**；NB_FX 作为独立分发包的自动依赖安装未被证明。 |
| DEP-02 程序集解析 | `NBShaders2/Tests/PassFeasibility/SubTargetProbe/Editor/NBGFSubTarget.asmref:1-3` 指向 URP Editor GUID `c579267770062bf448e75eb160330b7f`，当前 `Library/PackageCache/com.unity.render-pipelines.universal@.../Editor/Unity.RenderPipelines.Universal.Editor.asmdef.meta:2` 可解析。`NBGFUnlitSubTarget.cs:3-11,21-61` 实际使用该程序集内 `UniversalSubTarget`。URP Editor asmdef `:49-53` 在 VFX 包存在时给本程序集 `HAS_VFX_GRAPH`，GF 源 `:33-38` 只在此宏下处理 VFX GUI。 | 包内源码 / 官方程序集边界明确；**不改官方文件**，但编译出的类型属于 URP Editor 程序集；其 internal API 稳定性只对当前已测组合成立。 |
| DEP-03 自有测试 asmdef | GF PlayMode asmdef `:4-18`、PlayerProbe asmdef `:4-11` 以 URP/VFX 17.3 Version Defines + Define Constraints 限定自身；引用各自需要的 Runtime 程序集。 | 当前 URP/VFX 安装时参与编译并经 GF 运行；不代表 `.asmref` 或整个包可在缺 URP/HDRP-only 项目条件化。 |
| DEP-04 生产 asmdef | `XuanXuanRenderUtility/Runtime/*.asmdef:4-15` 硬引用 URP Runtime；`NBPostProcessing/Runtime/*.asmdef:4-34` 硬引用 Core、URP、Cinemachine；NBShaders2 自身 asmdef 通过 Utility 间接依赖。`NBPostProcess.cs:7-13` 本身需要 URP `ScriptableRendererFeature`。 | **当前安装组合可解析**；按 D20 未来拆分时不能只搬 `.asmref`，共同程序集与 NBPostprocess 也须重新划界，但本阶段不提前改。 |
| DEP-05 GF Graph / VFX 导入 | `Documentation~/reports/gf-functional-evidence/final-pass-list-after-reimport.json` 记录强制重导入 Graph Shader supported、10 Pass、VFX Output Shader supported、8 Pass、两者精确 LightMode `NBCameraOpaqueDistortPass`/`NBDeferredDistortPass` 均索引 1/2、ShaderUtil messages 0。`GF_URP_VFX_NBSubTarget.shadergraph:197-204,579-598,1298-1320` 同时序列化 HDTarget 与 UniversalTarget（且 URP active SubTarget 为 GF 类）；`GF_VFXOutput_NBSubTarget.vfx` 为独立真实资产。 | 当前 GF 资产在本环境有效；其 HDRP target 是序列化残留/历史状态的**观察**，不能作为 HDRP 已支持的证据，也不能直接照搬为 URP-only 产品 Graph。 |
| DEP-06 运行与构建 | GF 报告 `Documentation~/reports/gf-t00a-functional-gate.html:14-20`：Editor PlayMode VFX UnityTest 1/1；Player StandaloneOSX build succeeded，0 error，Metal Player 实跑 `passed=true`，Mesh 与粒子两条模式均有像素变化。`gf-functional-evidence/sha256.txt` 64 项在当前工作树校验 **0 不匹配**。 | 仅是固定向量测试 HLSL 和 test-only Graph/RendererFeature/场景的可行性证据；不等于产品共享 HLSL、完整 NB 功能或 G3。 |
| DEP-07 Editor 历史状态 | GF 原始 `final-compilation.json` 为 completed/failed=false；`final-editor-status.json` 为 ready；`final-console-status.json` 当时 groundTruth consoleErrors=0。 | 这是 **GF 当时记录**（2026-09-25），不是本轮实时 Editor 查询。本轮未调用 Unity；主 Agent 在 G1 审核前应串行取新编译和 Console 状态。 |

## D21 下的 G1 依赖项结论

在当前项目的 URP/SG/VFX 17.3 安装组合里，GF `.asmref` 指向的目标存在、试验 SubTarget 可编译、Graph/VFX 两个精确 Pass 能重导入和运行；自有 GF asmdef 条件也已满足。**因此依赖边界不阻止 T01 其他接口合同的 G1 审核。** G1 需记录：本阶段依赖由项目 manifest 提供而非 NB_FX `package.json` 自动安装；产品 SubTarget 仍是 URP Editor internal API 扩展；HDRP-only/无 URP/旧版本不在此结论中。G1 的属性/Flags/阶段/所有权/变体预算是另行验收项，不能用本审计替代。

D21 `Documentation~/decisions.html:40,56` 已把 Unity 2021.3 与低版本兼容评估移到 T10/G7，`tasks/t01-contracts.html:40,48` 明确旧版本不是当前 G1 条件。因此先前 `Temp/NBFXT01/dependency-isolation-audit.md` 中“G1 不能因缺 2021.3 实测通过”的时序判断**已被 D21 取代**；其中静态依赖事实仍有效。不得把时序放宽误写成旧版兼容已验证。HDRP-only 安装边界则按 D20 `decisions.html:39` 在 HDRP 实现前设计；本阶段不提前拆包或改 package.json。

## T06 产品化前 / G3 必须重新验证

1. **真实产品候选而非 GF 原型**：Graph 单独明确 URP active target、`Support VFX Graph` 启用，实际引用 G1 冻结的共享 HLSL File Custom Function；固定向量 GF HLSL 不能作产品功能结论。保存、强制重导入、Inspector 与 VFX 输出材质引用后检查产生的 Shader/Pass 列表及 ShaderUtil 消息。
2. **两 Pass 生成契约**：采用产品 SubTarget/wrapper 时验证原 Forward 未损坏，准确复制 `NBCameraOpaqueDistortPass` 和 `NBDeferredDistortPass` LightMode、排序、RenderState、HLSL include、Graph 参数绑定。GF 源通过遍历 URP Unlit Forward 的 `referenceName==SHADERPASS_UNLIT` 并替换最后一个 fragment include（`NBGFUnlitSubTarget.cs:43-89`），这是当前 URP17.3 内部布局耦合，产品不能不经复测视作稳定。
3. **Graph/VFX 参数与变体**：核对整数/packed Flags、高位值、精度、uniform 与逐粒子动态输入、Custom Function stage、sampler/texture、Preview 分支、关键字和 stripping；GF 无真实 NB 数据覆盖。对当前 17.3 实际生成 HLSL、编译信息和 Player 材质做验证。
4. **NBPostprocess 真链路**：在 Mesh C 组与当前原 NBPostprocess 的 CameraOpaque/Deferred 筛选、RendererList、Mask RT、采样、Blend 与最终合成复测；Graph/VFX 的固定向量 GF 像素差异不证明产品 HLSL 等价。RenderGraph 和计划内 Compatibility 路径、控制器 flag 组合、原 NBShader B 组对照按 T06/G3 与后续 Gate 分层记录。
5. **构建与 Console**：主 Agent 串行进行当前 Editor 编译/Console、产品资产强制重导入、Player 构建与运行；验证材质/Graph 变体不会被裁剪。保留原始 evidence 和未运行项，不能继承 GF 的 `passed=true` 作为 G3 结果。
6. **越界停止**：若必须改官方包、URP Target、NBPostprocess、削减功能，或需将 D20 安装拆分提前，先提交决策材料；当前 G1 只冻结接口和责任，不授权这些更改。

**未验证、非 G1 阻塞：** Unity 2021.3/URP12、Unity 2022.3/URP14、Unity 6.5、HDRP-only/无 URP、VFX 缺席、DirectX/Vulkan、多 OS/GPU、产品版完整 NB 功能。后续宣称支持前按 T10/G7 与各功能 Gate 独立实测。
