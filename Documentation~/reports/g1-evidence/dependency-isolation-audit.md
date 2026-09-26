# T01 依赖 / 版本隔离只读审计（2026-09-27）

范围：仅核对当前工作树及本机已安装 Editor/包源码；未修改 Unity 工程、NB_FX 包或官方包，未运行 Editor。采样提交：主工程 `31ea499`，NB_FX `a73df4b`。以下「观察」不等于其他环境验证；「候选」尚未获得生产授权。

## 1. 当前观察

| 项目 | 证据 | 结论 |
|---|---|---|
| NB_FX 包清单 | `Packages/NB_FX/package.json:1-7` | `unity=2021.1`，无 `dependencies`；不能据此推断 URP/SG/VFX/Cinemachine 都可选。 |
| 当前项目 | `Packages/manifest.json`；`Packages/packages-lock.json:123-130,155-162,216-229`；`ProjectSettings/ProjectVersion.txt` | Unity 6000.3.18f1；项目显式安装 URP/VFX 17.3、Cinemachine 3.1；SG 17.3 为 URP/VFX 传递依赖，NB_FX lock 条目依赖为空。 |
| 生产 asmdef | `XuanXuanRenderUtility/Runtime/com.xuanxuan.render.utility.asmdef:4-15`；`NBPostProcessing/Runtime/com.xuanxuan.nb.postprocessing.asmdef:4-34`；相应 Editor asmdef；`NBShaders2/{Runtime,Editor}/*.asmdef` | Utility Runtime 无条件引用 URP Runtime；NBPostProcessing Runtime 无条件引用 Core、URP、Cinemachine、Utility；NBShaders2 通过 Utility 间接依赖。`NBPostProcess.cs:7-13` 实际继承 URP `ScriptableRendererFeature`。现有 NBShader ShaderLab `NBShaders2/Shader/NBShader.shader:713,837,902,...` 直接包含 URP HLSL。 |
| 可选 Cinemachine 宏 | `NBPostProcessing/Runtime/*.asmdef:17-32`、`Editor/*.asmdef:19-25`；`PostProcessingManager.cs:7-9,73-76` | Version Defines 让 Cinemachine 3 C# 分支有条件编译，**不**让 asmdef 中的 Cinemachine GUID reference 变成条件引用。无 Cinemachine 项目未验证。 |
| GF 测试 asmdef | `NBShaders2/Tests/PassFeasibility/FunctionalProbe/{PlayMode,PlayerProbe}/*.asmdef` | Version Defines + Define Constraints 要求 URP/VFX 17.3；只在两者都安装的当前工程编译/运行过，不能据此断言缺包时整个 NB_FX 安全导入。 |
| GF SubTarget | `NBShaders2/Tests/PassFeasibility/SubTargetProbe/Editor/NBGFSubTarget.asmref:1-3`；`NBGFUnlitSubTarget.cs:3-11,21-61` | `.asmref` 无条件并入 GUID `c579267770062bf448e75eb160330b7f` 所指 URP Editor 程序集。GF C# 依赖 URP Editor / Shader Graph 类型。VFX 特有段 `:33-38` 受目标 URP Editor asmdef 的 `HAS_VFX_GRAPH` Version Define 控制（`Library/PackageCache/com.unity.render-pipelines.universal@.../Editor/Unity.RenderPipelines.Universal.Editor.asmdef:49-53`）。 |
| 旧 Editor 的 `.asmref` 目标 | 本机 `/Applications/Unity/Hub/Editor/2022.3.62f3/.../com.unity.render-pipelines.universal/package.json:4-5` 与其 Editor asmdef `.meta:2` | URP 14.0.12 / Unity 2022.3 的 Editor asmdef **使用同一 GUID**。因此该 `.asmref` 在旧 URP 中不会按版本自动失效；URP14 中部分 SubTarget API 可见，但未实际编译或导入 GF Graph。未安装 2021.3，故 URP12/2021.3 GUID 与兼容性未知。 |
| GF Graph 序列化 | `NBShaders2/Tests/PassFeasibility/SubTargetProbe/GF_URP_VFX_NBSubTarget.shadergraph:197-204,579-598,1298-1320` | 同时保存 HDTarget 和 UniversalTarget 两个 active target。GF Graph 不能直接作为 URP-only 产品 Graph；HDRP-only 或旧 SG 导入效果未测。 |

## 2. 原理边界（官方手册）

- Unity [asmdef / asmref 文件格式](https://docs.unity3d.com/cn/6000.0/Manual/assembly-definition-file-format.html)：`.asmdef` 有 `references`、`defineConstraints`、`versionDefines`；`.asmref` 只有目标 `reference`，不提供前述条件字段。Version Defines 是编译符号/程序集条件，不是 JSON `references` 数组的条件项。
- Unity [创建程序集资源](https://docs.unity3d.com/cn/6000.0/Manual/assembly-definitions-creating.html)：`.asmref` 把该文件夹脚本纳入既有程序集，不创建独立程序集。包内文件位置并不使编译后的类属于 NB_FX 自有 DLL。
- Unity [包示例目录](https://docs.unity3d.com/cn/6000.0/Manual/cus-samples.html)：`Samples~` 的 `~` 会让 Unity 忽略，用户点击 Import 后复制到 `Assets`；这适合隔离 GF 试验资产，却不是无需设计的产品安装方案（包内 GUID/资源路径、升级/卸载均受影响）。
- Unity [包清单](https://docs.unity3d.com/cn/6000.0/Manual/class-PackageManifestImporter.html)：`unity` 是一个最低 Editor 版本，`dependencies` 是固定依赖列表；单个清单不能依当前管线自动变出不同依赖列表。

## 3. 技术隔离候选与判断

| 候选 | 能解决 | 不能解决 / 约束 | 现阶段判断 |
|---|---|---|---|
| A. 保持通用 NB_FX 包，仅在自有独立 asmdef 上用 Version Defines + Define Constraints，GF SubTarget 源码用 Unity/URP 条件编译，并把 GF-only 资产移至不自动导入的测试/示例区 | 可减少普通 C# 在旧 Editor/缺 VFX 时编译；GF 资产可不污染生产导入。 | `.asmref` 仍须解析 URP Editor，无法让缺 URP 的 HDRP-only 导入安全；正式产品 Graph/VFX 资产若留在自动导入目录，也无法靠 C# `#if` 隔离；移动 GF 资产须维护 GUID/引用。 | 可作**临时试验隔离**，不是完整跨管线方案；必须另行在 Unity 2021.3 实测。 |
| B. URP6000 SG/VFX 独立安装单元（自身声明 Unity 6000.3、URP17.3、VFX17.3；拥有 `.asmref`、Graph/VFX 与 wrapper），旧 NB_FX 保留原 ShaderLab 路径 | 由包是否安装决定 `.asmref` 是否存在；固定版本依赖清楚；旧版不会解析 17.3 Graph。 | 当前通用 NB_FX 已有 URP/Cinemachine 硬依赖，HDRP-only 仍需未来整理；包间 GUID/引用、升级/卸载和双管线共存需设计/验证；D20 明确当前阶段不提前拆包。 | **技术上最完整的候选，当前需用户先改 D20 时序授权**，不擅自实施。 |
| C. 分离发布版本/分支：2021.3 旧版与 6000.3 新版同包名不同发布版本 | 旧使用者可继续装旧发行版，新发行版可提高 `unity` 最低值。 | 不满足同一当前 NB_FX 版本中旧 ShaderLab 2021.3 可打开/运行的通常理解；维护、更新和兼容承诺要重新界定。 | 需用户明确是否接受，不能当作既定决定。 |
| D. 仅把 `.asmref`/Graph 放在 `Samples~`，由用户导入 | 默认包导入时避开试验资产。 | Import 复制到 `Assets`，不是稳定的包内生产扩展；移动时 GUID/绝对 Package HLSL 路径、升级/卸载都要处理；不消除现有生产 URP/Cinemachine asmdef 引用。 | 适合样例/原型，不推荐直接作为产品解决方案。 |

对当前 URP-only 阶段，不应为了未来 HDRP 预先修改官方包、Target、NBPostprocess 或 `package.json`。但 G1 必须区分两件事：**URP6000 功能实现可以继续设计**，与 **当前同一包已证明不破坏 2021.3**（尚未证明）不是一回事。D08 要求旧 ShaderLab 兼容不自动放弃（`Documentation~/decisions.html:27`），D20 把按管线安装设计放到 HDRP 前（`:39`）；若 G1 的 2021.3 实开要求必须立即满足，而 A 在验证中失败，则 B/C 或调整时序需要用户决策。

## 4. 建议 G1 最小合同与验证矩阵

1. 明确**当前支持宣称**仅 Unity 6000.3.18f1 + URP/SG/VFX 17.3（GF 已测的功能有限）；2021.3 原 ShaderLab 路径仍为必须保护的目标，但本机未装 2021.3，不能写已通过；HDRP-only/无 URP 非当前支持宣称。
2. 新 Graph/VFX 对当前 URP 用产品专用 Graph，只保存 URP active target；保留 GF 原型为试验，不以其序列化或固定 HLSL 代替产品资产验证。
3. 给每个 C# assembly / ShaderLab / Graph / VFX / HLSL 文件标记 `Common`、`URP legacy`、`URP6000 SG/VFX`、`future HDRP`、`GF-only` 归属；注意现有 Utility 和 NBPostProcessing 仍含硬 URP 依赖，不能提前承诺 Manager/Controller 已可供 HDRP-only 复用。
4. Version Defines 只用于**自有 asmdef** 的可选源码或完整程序集；`.asmref` 需安装边界或目标程序集存在性保证，缺 URP 不能以 `#if` 自证通过。
5. 需要独立环境逐项验证：2021.3 + 对应 URP 的原 ShaderLab 导入/编译/样例 smoke；6000.3 + URP17.3+VFX17.3 的产品 Graph/VFX/G3；VFX 缺席的 URP-only（若声称可选）；最终 D20 前 HDRP-only 与双管线组合。当前仅后一矩阵中的 GF 有部分结果；其余写「未运行」。

结论（原审计时）：单靠 asmdef Version Defines 不能证明当前包的跨版本或 HDRP-only 安全导入。**无须立即拆包**可以继续 T01 接口设计；原文将 2021.3 实开/隔离视为 G1 条件。

**2026-09-27 用户 D21 更正：**低版本兼容不是当前必要条件，先在现有 Unity 6000.3.18f1 / URP、SG、VFX 17.3 跑通。上述技术风险仍属事实记录，但 2021.3、URP14、无 URP、HDRP-only 验证均移至最后的兼容/安装轮次，不再阻挡 G1/G2/G3；不得把未测版本写成支持。当前 G1 只需证明现有环境的依赖和程序集边界可供产品实现。
