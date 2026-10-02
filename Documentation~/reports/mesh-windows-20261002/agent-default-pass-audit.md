# PASS-R 默认 Pass 与高级状态只读审核

审核环境：Unity 6000.3.25f1，URP/SG/VFX 17.3.0，已有 Direct3D11 证据。此次只读源码、官方本地包和既有捕获；没有调用 Unity、修改产品/候选/Graph/测试或 Git。源码/报告可能由其他 Agent 继续更新，以下 SHA 仅是本审核输入。

## 已能作出的结论

- 旧 NBShader 没有 DepthNormals/DepthNormalsOnly/GBuffer/MotionVectors Pass。Graph 委托官方 Unlit 生成这类额外 Pass，默认材质仅同步 DepthOnly/ShadowCaster/MotionVectors，DepthNormalsOnly 未受旧意图约束。已有两个 VAT 阴影对照的显著 B/C 差来自这个额外 normal/depth 贡献参与 SSAO，不能算微差；关闭该 Graph material Pass 的直接控制已有零差结果，但完整默认链路没有完成。
- 此问题不要求修改官方包、UniversalTarget、SSAO RendererFeature 或 NBPostprocess。可以在现有 NB material/GUI 投影中让 NB Graph Mesh 的 DepthNormalsOnly 默认不参与，保留生成 Pass。但 GUI 单片不能保证绕过 Inspector 的创建、自动导入和 runtime new Material 默认状态，必须明示并补生命周期证据。
- Graph 当前缺少 BackFirst 真正第二次绘制，且 main 无 LightMode，实际由 SRPDefaultUnlit 选择。直接应用旧 backFirst→SRPDefaultUnlit 的 disable 映射会关闭 Graph 主颜色。实现 BackFirst 时必须先拆分 main/optionalBack 的 LightMode 身份，再复用旧 Resolver；不能仅添加 Toggle。
- ZOffset 的两个 ShaderLab 属性和 Offset 命令尚未接入当前 Graph；Portal 的业务 Toggle/转换尚未接入共享 Graph GUI。已有 ColorMask/Stencil30 只能证明显式原始状态绘制，不能代替这些业务事务。

## 旧实现：默认状态与顺序

来源：`NBShaders2/Shader/NBShader.shader`、`NBShaderMaterialIntentResolver.cs:214–285,453`、`NBShaderSyncService.cs:264–292,889–928`。

旧 Shader 原始属性默认为 TransparentMode=1、ForceZWrite=0、Cull=2（剔除背面）、ZTest=4、ZWrite=0、BackFirst=0、AffectsShadows=0、offsetFactor/Units=0、Stencil=0/Always/Keep/read-write255。

旧 GUI 的 `NBShaderRootItem.OnChildOnGUI` 顺序为 Toolbar→Mode→Base→MainTexture→Light→Features→TA，然后在初始化或编辑变化时 `SyncMaterialState`。同步顺序：MeshSource→CustomData→UV derived flags→TransparentMode/ZWrite→transparent shadow flags→blend→time→toggle flags→POM layers→同一 Resolver 有效 keyword/pass 投影。

旧 normalized Pass 状态：

| 意图 | Main UniversalForward | optional SRPDefaultUnlit | DepthOnly | ShadowCaster | 两条精确 NB Pass |
|---|---|---|---|---|---|
| 默认透明 | on | off | off | off | off |
| Opaque/CutOff | on | off | on（ForceOff 可禁止） | AffectsShadows 与有效 Tier 决定 | screen mode 决定 |
| Transparent+ForceOn | on | BackFirst toggle/Tier 决定 | on | AffectsShadows 决定 | screen mode 决定 |
| 有效 screen distortion+DisableMain | off | 独立 BackFirst 条件（不能猜随 main） | ZWrite/Tier 决定 | AffectsShadows 决定 | 模式1 Deferred；模式2 CameraOpaque |

所有六个 managed optional pass 最后还受相同 allowedPassFeatures 过滤；本报告不改默认 Tier。Universal2D 对原路径仍按 D22 用户现配置处理，Graph 不承担该宿主。

旧 Pass 实际布局为：BackFirst `Name/LightMode=SRPDefaultUnlit`、Main `UniversalForward`、DepthOnly、ShadowCaster、NBCameraOpaqueDistortPass、NBDeferredDistortPass、Universal2D。BackFirst 在 Main 前、Cull Front，跟 Main 共享 Blend/ZWrite/ZTest/ColorMask/Stencil/Offset；启用回调会把 main `_Cull` 设为2（只画正面）。不能把本 Pass 的 Tags Queue=Opaque 当作独立渲染队列，队列仍由 material/SubShader 过滤。

BackFirst 定义 `PARTICLE_BACKFACE_PASS`。`NBShaderInput.hlsl:1142` 的 `ignoreFresnel()` 在该 Pass 返回 true；旧 fragment 留 `fresnelValue=0` 后仍调用同一 `NBFX_ApplyFresnelV1`（所以 alpha-mode 的后续效果仍发生）。Graph 如只是复制 Forward，会错误计算背面 Fresnel；如简单跳过全部 Fresnel apply，也与旧 alpha-mode 不同。旧 BackFirst 同样编译 POM/OverrideZ/VAT/灯光；应保留其实际功能。

## 当前 Graph/SubTarget 的实情

当前 root 仍是 GUI1B/C 范围；隔离 SubTarget 有 SM4.5 interpolator 修复、VAT screen exclusion、OverrideZ 等后续修改。两份不能混作同一版本。

根/隔离 Graph 目标默认：AllowMaterialOverride=true、SurfaceType=Transparent、AlphaClip=false、ZWriteControl=Auto、ZTest=LEqual、CastShadows=false、RenderFace=0（Graph Cull Off，区别于旧 Cull Back）。Graph validation material 序列化无需旧材质相同，但原始默认值的差异不得隐瞒；等价测试需明确对齐有效状态，不能擅自宣传 raw new Material 默认等同旧材质。

当前 SubTarget 委托 Builtin Unlit 后，只把 Forward 的 fragment 改为 NBGraphForwardPass，并添加两个 NB screen clone；ShadowCaster 替换为 NB fragment。DepthNormalsOnly、DepthOnly、GBuffer、MotionVectors、Selection/Picking 仍由原 descriptor 生成（部分加 NB stencil）。没有 BackFirst clone、Offset renderstate、Portal business projection。

历史实际暖绘制的十 Pass 是：Universal Forward（LightMode空）、NBCameraOpaqueDistortPass、NBDeferredDistortPass、DepthOnly、MotionVectors、DepthNormalsOnly、ShadowCaster、GBuffer、SceneSelectionPass、ScenePickingPass。来源 `mesh-windows-20261002/cli-current-graph-inspection.json`。它对应早期 Graph；当前候选需重新生成检查，不能拿历史生成 Shader 当最新源码证明。

当前 Graph 内有 `_AffectsShadows` 编辑字段，但 URP 真实 ShadowCaster 控制仍是 `_CastShadows`。Graph GUI 当前仅真正共享 MainTexture，`SyncMaterialState` 对 Graph 跳过所有 legacy pass/Surface/Tier 投影，`ApplyShaderPass`/`ApplyPortalState` 也在 HasGraphTargets 时返回。不能把存在 Float 或本地 Native fallback 当业务已经联动。

## DepthNormals/SSAO 的证据和源码链

- `UniversalUnlitSubTarget.cs:242–277` 无条件添加 Unlit DepthNormalsOnly，不取决于 CastShadows，材质 override 也不会取消它。
- `BaseShaderGUI.cs:765–803,1138` 按 ZWrite 决定 DepthOnly，按 `_CastShadows` 决定 ShadowCaster，没有 DepthNormalsOnly material 状态同步。
- `UniversalRenderer.cs:363–364` 深度法线预通道过滤 opaque；当前隔离高画质 Renderer 的 SSAO 活跃、Source=1（DepthNormals）、AfterOpaque=0、Intensity=.5，Forward renderer、DepthPriming=0。不能把透明队列下暂时没画 normal pass 扩写为 Opaque/Portal/Shadow 全状态不受影响。
- `DepthNormalOnlyPass.cs:199–204` 用 DepthNormals/DepthNormalsOnly tag RendererList，无自定义 depth RenderStateBlock 覆盖；官方 fragment会生成 real normal 和写 depth。Builtin DefaultSSAO checkbox控制 Forward 受 AO 的定义，不取消这个 Pass。给 Builtin 设置 defaultSSAO=false 不是本问题修复。
- 原两失败 B/C=660/1015像素，max≈.228515625/.208984375；关 normal bias、Shadow fragment clip 或 SRP Batcher 都不消除。仅禁用 Graph material DepthNormalsOnly 才消除。来源 `vat-shadow-fullframe-analysis.json`、`vat-shadow-depthnormals-diagnostic-1/batch-1/results.xml`；保持原失败及限定 Pass 的最终通过分开。

### 最小安全路线与生命周期边界

第一片建议把唯一、只依赖 Material 的 Graph Mesh pass-contract 小规则放在现有 NBGraphUnlitGUIBridge 中，由 NBShaderGraphGUI 的 Validate/OnGUI编辑后/Assign、NBShaderSyncService 的精确Graph支路及 NBGraphUnlitSubTarget.ProcessPreviewMaterial 调用。现有 bridge和SubTarget通过asmref在URP Editor程序集，而NBEditor已依赖此public bridge；不能反过来让URP Editor引用NBShaderSyncService（会形成程序集反向依赖）。此规则只做既有Pass投影，不复制Resolver/GUI。在 URP Surface 验证之后，精确 NB Graph Mesh host 执行 `material.SetShaderPassEnabled("DepthNormalsOnly", false)`；先比较 Get 状态保证 no-op。主路径、Shadow、DepthOnly、surface/blend/queue 保留官方 `_Surface/_AlphaClip/_ZWriteControl/_CastShadows` 权威。不能把 Normals 绑到 DepthOnly，旧 DepthOnly-on 仍没有 normals pass。

这片能证明已 normalized 的 Mesh materials，不自动保证 raw creation：

- `MaterialPostprocessor.cs:212` 新材质初始化→`ShaderUtils.UpdateMaterial`→SG_Unlit分支→**官方静态** ShaderGraphUnlitGUI.UpdateMaterial，绕过 NB custom GUI。
- `ShaderGraphMaterialsUpdater.cs` Graph 保存后同样走静态 ShaderUtils；`ShaderGraphImporter.cs:187` 直接 new Material(shader)。
- Player `new Material(graphShader)` 不会运行 Editor GUI。仅补 NB Assign/Validate 而缺这些证据，就仍不能声称所有新建/Player默认无 normals。

如果要求这些完全未经规范化的 NB Graph 材质也必须原生没有 extra normal/depth/stencil贡献，最小包内 shader-level候选可在当前 NB SubTarget 的 **DepthNormalsOnly descriptor** 保留 Name/LightMode/生成模板/官方 include/接口，改为无输出 renderstates：`RenderState.ZTest("Never")`、ZWrite Off、ColorMask 0、Stencil WriteMask0（Fail/Pass/ZFail Keep），不删除 Pass、不关全局 SSAO、不改官方包、不增 bit/keyword/Tier。`RenderState.ZTest(string)` 和原始 descriptor 字符串 API 在本机真实存在；SG `ZTest` enum 没有 Never，不能编造 `ZTest.Never`。这是恢复旧 NB 无Normals贡献的候选路线，必须先真实生成/暖绘制测试，未实施/未验证。

此 shader-level fallback 仍可能被 URP 选中并消耗 vertex/draw；normalized material Pass off 可省掉它。因此 performance/draw证据也要记，不能用“无图像变化”冒充无成本。不能给官方 Forward/DepthOnly/Shadow 或无关 shader 应用此规则。若需要 NB Graph 新增真正 normals/SSAO 贡献并提供开关，这是新合同决策，另讨论；本轮没有这些新增要求。

## BackFirst 最小路线

在现有 NBGraphUnlitSubTarget 顺序建立 optionalBack→normalMain→两NBscreen→其它官方Pass。normalMain 显式 `lightMode="UniversalForward"`（显示 Name仍可保留 `Universal Forward`）；optionalBack保留旧 `SRPDefaultUnlit`、Cull Front、useInPreview=false，同一 generated vertex和 NB fragment/CF/lighting/OverrideZ。已有 main名字和实际 LightMode 要分别登记，测试 SetShaderPassEnabled 一律使用真实 tag。

optionalBack 加 static pass define `NB_GRAPH_BACKFACE_PASS`；同一 NBGraphApplyFresnel helper中令 fresnelValue=0，保留原 ApplyFresnel 后续，并让 POM 在普通 main/back两者都按旧实际实现可用。无须新材质 keyword或 packed bit；不能另复制整套表面 HLSL。

与现有同一 Resolver 联动时由 Graph host映射旧 coreMain→UniversalForward、backFirst→SRPDefaultUnlit，避免 legacy direct pass名规则误关原 Graph主图。必需 `_BackFirstPassToggle`真Float及共享回调/Undo/multiselect；opaque强制BackFirst无效和 Tier禁用→允许恢复都须覆盖。

Forward renderer本机 DrawObjectsPass 默认 tag顺序为 SRPDefaultUnlit/UniversalForward/UniversalForwardOnly（源码88–93）。C#只说明提交 tag集合，不能单凭 descriptor顺序推断最终 native绘制次数/先后；必须做下面的真实两面半透明操作对照。当前 Forward实现不要求 NBPostprocess 或官方 UniversalTarget变化。URP Deferred另一条 forward-only列表 **不含 UniversalForward**，当前额外GBuffer也会改变选择；本轮基于现有 Forward环境先实现，并把 Deferred renderer真验放到单独矩阵，不凭本片宣称所有 renderer等价。

## ZOffset 最小路线

旧 Offset 命令适用于 Back/Main/两NBscreen/Universal2D；**不适用于 DepthOnly和ShadowCaster**。`_ZOffset_Toggle`只控制GUI可见/关闭时把offsetFactor/Units清0，shader真实直接读取两值，不能额外用toggle改变渲染数学。

在既有 SubTarget 收集 `_offsetFactor/_offsetUnits`真实Float默认0，并只对对应color passes加 `Offset [_offsetFactor], [_offsetUnits]`；不改VO/OverrideZ/pipeline bias。

本机 SG RenderStateType/RenderState静态类没有 Offset API；不得编造 `RenderState.Offset`。现有 `RenderStateDescriptor.value` 是直接发射 ShaderLab原文，Generator.cs:763–787实际按type遍历字符串。可以对这些color passes的现有ZTest descriptor **保持type/fieldConditions并将 value追加一行Offset**，避免新增错类型或替换官方模板；两个NBscreen克隆的固定 ZTest也同样追加。需要生成源码核对每个对应Pass恰一条Offset，Depth/Shadow/extras为零，以及 VFX后处理阶段未截断多行。该技术候选仍待真实编译/绘制，不能计通过。

## Portal 实际业务

`PortalFeatureItem` 调用同一 SyncService.ApplyPortalState。旧策略：

| 状态 | Stencil preset | Surface/Depth | 其余 |
|---|---|---|---|
| portal off | ParticleBaseDefault | Transparent；ForceZWrite=Default | ZTest LEqual，CustomStencil=0 |
| portal mask | ParticalBasePortalMask：Ref200/Always/Replace | 若原是Transparent则切CutOff；ForceZWrite=ForceOff | ZTest LEqual，CustomStencil=1 |
| portal content | ParticalBasePortal：Ref200/Equal/Keep | 保留现有Surface/Depth | CustomStencil=1 |

真实 `StencilConfig.asset` 还给mask/content DefaultQueue2000/3100，但 `StencilTestHelper.SetMaterialStencil` 仅通过out返回，当前 ApplyStencilPresetToMaterial 使用 `out _`；旧可观察实际队列仍来自 Surface/QueueBias GUI，不可按配置DefaultQueue直接改成另一合同。保留旧行为：mask转CutOff通常2450+QueueBias，content由自身3000+QueueBias等决定。

Graph当前只有rawStencil功能，无Portal字段/回调。下一片应在原 ApplyPortalState 分Graph host小支路、复用同一StencilTestHelper/Config，直接设置官方 `_Surface/_AlphaClip/_ZWriteControl/_ZTest`，之后用现有URP bridge验证。映射CutOff为 `_Surface=0,_AlphaClip=1`，ForceOff为 `_ZWriteControl=2`，off回Default为Transparent/AlphaClip0/ZWriteControl0；不能引入 shadow `_TransparentMode` 每次回写官方值。多材料Undo/恢复/Surface权威需单独验。

## 有限默认全链路验证方案

先跑 **2个相机的旧VAT阴影差异复现**：使用当前组合Graph；A Frozen/B NBShader/C Graph匹配opaque有效Surface、CastShadows on、Cull、ZWrite、同一几何/灯和SSAO设置。此新夹具不再手动关闭 C DepthNormalsOnly，不手动禁用官方 extras来营造限定 Pass。B真正调用同一旧 SyncService；A取可追踪的旧基线有效状态快照；C只走真实新的 Graph Validate/normalized合同。保留真实floor lit material和背景、完整帧RGBA、finite、visible、repeat0、VAT on/off/帧号强响应。记录 `_CameraNormalsTexture`和实际CameraDepth/readback或等价drawing证据；SSAO保留现配置活跃。如果2项通过，仅算对应修复链路。

随后 **默认表面16项**：Opaque、CutOff、Transparent Auto、Transparent ForceOn 四态×SSAO on/off诊断×ortho/persp。on是实际验收；off仅局部诊断且finally恢复，不改变用户全局配置。每态做 material创建→Normalize→保存重导入→同一Graph重生成的状态快照和图像；另列raw new Material未调用GUI/Player状态，证明 shader-level fallback 或如实未验证。独立 SSAO正常对象控制需有强响应，防止“SSAO没有运行”造成零差。

**BackFirst12项**：toggle off/on×plain/FresnelColor/FresnelAlpha×两相机。封闭非共面 Mesh、可分辨内外两侧色、半透明Alpha；比较正常draw、反向排序控制与Cull控制，必须有强响应，full-frame B/C0、repeat0，并核对实际draw次数。补opaque toggle无效与主Pass禁用/两screen排除状态；不能只看一个flatQuad。

**ZOffset12项**：两相机×(0,0)/(0,1)/(0,-1)/(1,0)/(-1,0)/(1,1)，固定近共面有坡度几何及第二物体探针，验证factor与units各独立响应。随后DepthOnly/Shadow的on/off offset值应不变（旧无Offset），Forward与两NBscreen有效；与OverrideZ交叉只挑代表4项，不新增无边界穷举。

**Portal12项**：portal off/mask/content×两相机×正常/反序绘制，真实模板遮罩及后续探针，mask外/内control必须强响应；记录相同Stencil/Surface/ZWrite/queue有效状态，DefaultNormals不得写Stencil副作用。再加2项mixed material Undo/Redo和保存重导入。

所有新组独立XML/日志、输入SHA和原始捕获，不与既有233/376/1105相加。原660/1015像素失败、限定Pass通过、CA GPU故障、Dissolve原40 CDCD故障和微差都分别保留。组合源完成后再必要回归/Player/性能，G3/G4仍须主 Agent审核。

## 并行请求附带结果：Dissolve health2与40计划

已只读核查 `uvp-dissolve-health2-isolated-1`：30份health+30份raw均完整128×128×RGBAfloat、finite且无CDCD。两case可见2304/2242像素，repeat0，强响应≈.927/.942；orthographic仅ROI1像素Alpha差0.00006103515625，RGB0，保留Failed；perspective0差Passed。`uvp-dissolve-health40-plan.json`已按原discovery40身份生成4×10fresh串行批次，夹具SHA92ba6858；后轮覆盖原身份latest evidence，不算42独立项。

## 输入 SHA

- rootGraph: `Packages/NB_FX/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph` SHA256 `c6ab58b7af69bcd4fe6d4383516d41589695c37fc4de3a1fc9402c7858ea3a5b`
- isolatedGraph: `.utmp/NBFXMeshValidation-20261002/Packages/NB_FX/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph` SHA256 `ab81db7259959ae3975efac30ab4a472340547133d99a5b911ec9d7524c7c611`
- rootSubTarget: `Packages/NB_FX/NBShaders2/ShaderGraph/Editor/NBGraphUnlitSubTarget.cs` SHA256 `63358e53b9067af300c57b3c77117928e3e8c27c96eb203efe7777e52feb565f`
- isolatedSubTarget: `.utmp/NBFXMeshValidation-20261002/Packages/NB_FX/NBShaders2/ShaderGraph/Editor/NBGraphUnlitSubTarget.cs` SHA256 `81c1b9ad0549b9aa6f23c51bc88c7655b73128a76a8787478efd288224cdf5ca`
- isolatedGUI: `.utmp/NBFXMeshValidation-20261002/Packages/NB_FX/NBShaders2/Editor/NBShaderGraphGUI.cs` SHA256 `946aae39b15747b68a1ff8e0ecd7e6f207d5cd843b5431035bd12ac23da28812`
- officialUnlit: `Library/PackageCache/com.unity.render-pipelines.universal@a0dd9d2bd983/Editor/ShaderGraph/Targets/UniversalUnlitSubTarget.cs` SHA256 `56087d7ac4bbb7758a0c8c6a575fa25fbb74cf68fc388f0ec7c6041bc0d98181`
- officialGenerator: `Library/PackageCache/com.unity.shadergraph@2fa139ee8b62/Editor/Generation/Processors/Generator.cs` SHA256 `9e2d4b5bdd4e577156eeaf8917ae12ac621d4ebf157378bdb6d4c21d9d3816c0`
