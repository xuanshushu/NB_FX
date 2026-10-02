# Root GUI/Tier 严格重基准备

仅准备脚本与静态审计；未生成 Root 候选、未安装、未改中央源、未运行 Unity/Git。

`build_root_gui_tier.py` 始终读取调用时 Root package：先对实际字段做完整 v1 schema guard，不满足立即拒绝。没有 staged bypass，没有假 VAT/CustomData 属性，没有放松 reader。

当前 Root Graph SHA `6fc363bf...` 缺11项：Flipbook1项、VAT toggle/mode/Houdini/Tyflow共4项、CustomData1/2/3的6个半字。F0 单片补 Flipbook 仍不足；应等真实 CustomData、VAT消费片集成且 provenance 审核后，再运行此脚本。

代码变更是同一 Resolver 的方法增量、现有37 binding的支持元数据及共用枚举核心；原 pass/filter/dependency/VAT方法和一般 Apply 保持。九项不可用仍显式返回。只追加显式 Mask projector，无完整 Tier/Toolbar/Validate 解锁。Graph/HLSL 调用既有 Mask动态生成器，从当时最新图追加3个default1 Float/对应节点和边，保留旧IDs/Targets/slots/边；没有旧wholeGraph复制。历史VFX hidden+exposed坑按已确认方案修正为non-hidden，并由现有GraphRoot前缀过滤保留派生字段不可编辑。

| 合同 | Root 当前实际范围 | 剩余缺口 |
|---|---|---|
| GUI29 / packed intent | marker2单次seed、原生word编辑显式通知；不周期性猜raw/mirror权威 | 其余共享控件真实编辑事务、多选/Undo/重导入/动态材料状态；GUI29后台不等于完整Inspector |
| 共享布局 | 仅Main Texture进入原Root/Context/Item；其余GraphRoot原生分组fallback | Feature/Light/Mode/TA全部共享块，实际属性前置条件和回调逐片 |
| keyword intent | Root仅旧Shader name限定resolver；隔离v1 reader113通过，Root完整schema缺11字段 | 完整28真实toggle/原native枚举入口待消费者集成后追加；缺9保持不可用 |
| Tier gate | Root没有Noise/Mask派生gate；隔离Noise2/Mask3限定能力 | 全部feature消费者/组合、有效枚举、keyword与Pass同步；不能宣称完整Tier |
| Pass/native状态 | URP bridge拥有原Surface/Blend/Clip；OVZ已Root，额外DepthNormals默认链路另片；Offset在隔离验证 | BackFirst/DisableMain、更多Stencil/状态组合、Pass预算与Runtime/Player |
| time / SharedUV / specular | shared UV数学/SpecularColor输入存在；Graph时间仍_Time.y | 三者控制/gate合同真实缺口；不能从raw位或参数存在推断完整功能 |
| runtime/build | Runtime/SVC/stripper仍精确旧NBShader保护 | Graph有效投影、Player/variants/performance，后续正式VFX与兼容 |

九项不可用：StencilWithoutPlayer、SharedUV、BlinnPhongSpecular及6个Debug。不要为了满足schema或报表自动新增无消费者字段。

下一可独立GUI片建议：在 Offset真实render-state验证完成后，复用既有 `TABigBlockItem` 的ZOffset块创建代码与关闭时归零回调，薄适配到同一Graph Root。新增仅editor Float foldout/toggle，预检真实两offset Float，再构造同一控件；不另建状态管理，不触动37keyword/Tier，不依赖未集成VAT/CustomData。应从existing构造抽一个共享块factory，避免直接实例化依赖大量缺属性的整TA块。UVP控件也可逐个按现有UVModeSelectItem/原flags后端接入，但整个FeatureBigBlock不能直接硬接。

后续回归预案是已编写 reader113 + mask43 source身份（156），默认前置条件拒绝现在执行，详情见plan。完整旧GUI110与受影响Mesh45由主Agent按最终源码来源安排，避免把此预案当新证据。

ZOffset guard已修成单方法跨度；Root9ed17f09与隔离7888c90c的内存patch guard均通过，DN0 helper完整保留，colour-only合同不变。其真实GPU/fixture问题与此GUI重基无关，保留各次原始失败和后续补证。
