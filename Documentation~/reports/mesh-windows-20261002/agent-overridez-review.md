# OVZ-R · OverrideZ 只读审核与独占 GUI 预览

输入产品 HEAD 为 `8a29a5e68ceb7965af66450599af9c24c7ece0b0`。审核 `overridez-preview/manifest.json` 中 10 个文件；其候选、安装字节及声明的 afterSHA256 全部一致。主产品仍未集成本片。未运行 Unity、未改原候选/安装源/中央 Graph、未操作 Git。

## 证据

- `overridez-depth16-isolated-1/batch-1/results.xml` 实读为 16/16 通过、0 失败/跳过/不确定；XML SHA256 `4b30b01387b3f6cbe43434ee3910884ee2ef991c6f0227ddaa4d28c1845963c0`。
- 原日志的实际 `G4OverrideZABC_` filter 与结果一致，未发现 Shader error、Unhandled/AssertionException 或 GPU crash。Licensing validation 及进程退出 mono abort 信息仍保留，未当作 Shader 故障。
- 16 项覆盖 normal Forward：开/关 × .05/3/7/50 深度 × 正交/透视；包含 finite、ABC 与 control 严格零差、同轮 repeat、可见与强响应断言。只涵盖本范围。
- 原捕获审计 `capture-audit-overridez-depth16-isolated-1-1-batches.json` 记录 144 份 raw、16 metrics、无 nonfinite；这是复用的既有审计，本 Agent 未重新读取全部 raw。

## 源码结论

1. `NBShaderOverrideDepthV1.hlsl:4–22` 与原 `NBShaderForwardPass.hlsl:35–54` 保持同一 clamp、rcp、正交/透视及 reversed Z 数学。共享函数 SHA256 `d9cf15436acc394cb1ad8eb8de5a6c795942c117a6feb5f4bc8e5b94dea4ee6b`。
2. `NBGraphForwardPass.hlsl:9–16,47–49` 仅在 `_OVERRIDE_Z` variant 声明/写出 SV_Depth；`NBGraphUnlitSubTarget.cs:124–130` 复用 local fragment shader_feature。未新增 keyword 名或 packed bit；没有本片新增 cbuffer/MPB。实际 SRP Batcher/variants/Player/perf 未验证。Forward wrapper SHA256 `60bf8db7900991468c8c3abf94eaa8b5ea5737ef4cae2327351deee9ac1aa35d`。
3. `NBGraphUnlitSubTarget.cs:53–58` 只有 main Forward 加 keyword；NB distortion clones 来自原 `pass`，Depth/Shadow 保持原 collection，符合原 ShaderLab 排除。结构证据不能替代生成代码及真实排除回归。
4. Graph 对 exact predecessor `customlocal-combined-preview` 作对象级比较：1565→1578，仅现有 GraphData 根对象变更；无旧对象删除、无现有非根对象改变；526→530 edges，无旧接线删除。Graph SHA256 `ab81db7259959ae3975efac30ab4a472340547133d99a5b911ec9d7524c7c611`。
5. 当前 `NBShaderGUI.cs:90–96` 只将主纹理字段标为共享，其余包括 OverrideZ 仍走原生 Graph fallback。新 Graph GUI 的 `OnGUI:18–19` 与 `ValidateMaterial:22–25,31–32` 保持 property > .5 到 keyword 同步；legacy `PropertyToggleBlockItem.cs:68,125–137` threshold 一致。不能把本片写成完整共享 TA 控件/Tier 已完成。

## 集成前问题与修复预览

原候选 `NBShaderGraphGUI.cs:48–49` 只委托 URP assignment；官方 URP `BaseShaderGUI.cs:1173–1182` 清空旧 keyword 后仅恢复 URP state，不调用外层 NB 同步。因此换 Shader 后保存的 `_OverrideZ_Toggle`/SixWay intent 与 keyword 可能失配，直至下次 OnGUI/Validate。这是生命周期真实缺口，需本轮验证后才集成本片。

主 Agent 已授权生成 `overridez-gui-lifecycle-preview`。其中只改 GUI assignment：URP 委托后调用现有 `SyncSixWayKeywords`，不新建 Resolver。GUI beforeSHA `125c8c37553072d45ce97a996285936d53c293139a8113ff17f2024c7d03a6fc`，afterSHA `946aae39b15747b68a1ff8e0ecd7e6f207d5cd843b5431035bd12ac23da28812`。

新 `G4GraphOverrideDepthLifecycleTests.cs` 为 17 个后台 case，filter `G4OverrideZGUI_`；真实 Material 和 ShaderGUI assignment 8 组合、Validate 4 组合双向转换、两材质独立 1、no-op serialized 2、与真实 URP GUI baseline 的 Surface/queue/pass/keyword 状态一致 2。全部 exact names 与 3 文件 SHA 在预览 manifest。未运行、未声称通过。测试不主动导入/刷新/保存/渲染。

## 有限补验与 G4 缺口

- 本片：17 GUI lifecycle；生成 Shader 中 Forward keyword/SV_Depth 与 Depth/Shadow/two NB Pass 排除；共享 math 抽取后关键 Depth/Shadow及当前 GUI 回归；真实 SRP Batcher 检查。GPU source 未因 GUI lifecycle 修改，16 深度证据可复用。
- 高级状态仍缺：ShaderLab `NBShader.shader:589–597,680` 有先画背面 `SRPDefaultUnlit`＋OverrideZ，Graph `SubTarget.cs:44–60` 未生成背面额外 Pass；不能宣称 BackFirst/OverrideZ 组合等价。
- Graph 默认仍从官方 Unlit `UniversalUnlitSubTarget.cs:255` 生成额外 DepthNormalsOnly；`NBGraphUnlitSubTarget.cs:51–52,222–225` 仅加 Stencil，没有完成默认 SSAO 链路等价审查。属于独立 G4 阻断，不应让 Forward 16 项冒充默认全 Pass 通过。
- 原 ShaderLab `NBShader.shader:595,733,979,1103` 的 depth offset 在当前 Graph render states 尚无对应 Offset；需独立 ZOffset 片。Player、变体与性能、运行时 MPB/Tier 仍留后续，不因本审核放行 G3/G4。

P4 `where` 已确认本独占 `.utmp` 位于当前 client `C:/Users/Admin/Work/tangyuxuan.nb_T3_C` 外；新文件不在该 Perforce 工作区。

## OVZ-ROOT-R · 当前根产品动态切片复核

2026-10-02 定向审核主 Agent 的 `overridez-root-rebased-preview` 与 `overridez-selfcontained-fixture-preview`。根产品仍为 `8a29a5e6`，报告目录增量由主 Agent 归档；本 Agent 未修改产品/候选/fixture，也未运行 Unity。

- root manifest 10 个 afterSHA 全部实读相符，6 个现有文件 beforeSHA 全部匹配当前根产品，4 个新增文件当前根源确实不存在。
- Graph `c6ab58b7…`→`6c10a11c…`：1026→1039 对象；无旧对象删除，仅 GraphData 根对象改变；274→278 edges，旧 274 条全部保留；原 6 个 fragment blocks 顺序完整保留，再追加 NBOverrideDeviceDepth。既有非根对象逐对象 JSON 精确相同。
- CF `43dee989b2425072ab470918469b89f8` 的四个 fragment slots为 id0 Vector1 input OverrideZToggle、id1 Vector1 input OverrideZValue、id2 Vector4 input PixelPosition、id3 Vector1 output DeviceDepth。CF `m_Precision=1`，文件 GUID `820f0a6a6d1a5a44abf863f0d1939520` 对应当前 adapter meta。两条 property edges分别接 `_OverrideZ_Toggle` 默认0、`_OverrideZValue` 默认1000；output接新 block 的输入slot0。类型、HLSL签名和属性名一致。
- 非Graph diff仅：原 ShaderLab Forward将原深度数学抽为同式共享函数；Graph Forward追加 conditional SV_Depth/写surface depth；SubTarget追加原local fragment axis与一个block；GUI追加property同步及assignment后同步。没有复制组合源的VAT/CustomLocal/UVP/Tier变化。共有数学和wrapper与原候选 SHA相同；root GUI SHA `e83ea53751f23bbed359bc27775d33da5a50c2cb23bccc286e6093335226e69a`。
- self-contained fixture SHA `4a6ee11ef5ad498b55aa71ba9910b475a461e6ad04117465943aed804a0db145`；原5条含断言的源行／12个Assert调用完整逐字一致，case source、finite/ABC/control/repeat/强响应/可见的运算也保持原文。移除未集成的 G4GraphVATTests Configure/Draw 类型反射，改为本地固定基础材质配置及同样128×128 RGBAHalf→rgba32f draw。删去的仅VAT关闭时无效的VAT参数配置。已集成的 G4GraphGuiFeatureIntentTests warm依赖仍存在；不应称为零外部测试依赖。

### PixelPosition fallback 的真实接口风险

slot2接现有 ScreenPositionNode `0be8cf583795508cb1024e8c098b47a1`，其 m_ScreenSpaceType=4（Pixel）。当前官方 ShaderGraph `Editor/Data/Util/ScreenSpaceType.cs:24–25` 返回 `$precision4(IN.PixelPosition.xy,0,0)`，因此 adapter `NBGraphOverrideDepth.hlsl:7` 使用的 PixelPosition.z 恒为0，不是原像素device depth。GUI属性/keyword同步且关闭variant没有SV_Depth时，此fallback不会写出；既有16项证据仍成立。但 keyword `_OVERRIDE_Z` 已开而 toggle<=.5 的runtime/动画/原生keyword配置会让Graph写0，原Shader始终按keyword做Value override，存在语义失配。

建议使用现有 `_OVERRIDE_Z` 编译态控制原数学并去掉toggle运行时分叉（可保留现有slot接口避免换接线），补有限的keyword/property失配真实深度对照。若先维持原候选，应明确只通过同步GUI态并留下此功能缺口，不把本片升级为完整runtime等价。修复权和Unity独占仍归主Agent。

### 已授权的 keyword authority 修复预览

主Agent已要求修复该真实合同问题，并指定独占目录 `overridez-keyword-authority-preview`。本Agent只生成该目录的两个替换文件及manifest/plan；未改原候选、主包、隔离源或Graph，未运行Unity。

- adapter保留原3 input＋1 output的名称/类型/顺序与既有GUID。`DeviceDepth=0`仅占位；只有`#if defined(_OVERRIDE_Z)`计算同一NBFX共享数学，不再按OverrideZToggle运行时分叉，也不使用PixelPosition.z。off variant的Forward wrapper没有SV_Depth，故0不写真实深度。
- adapter SHA `1d87f44a6451495a7083284221cb3627e30e1bea7bcae5be3804eb05f826e76f`→`e32d6685ee8a19243821ce00c46a041eea2bc3a0103ebe0651e2ca574c8a3fb1`。
- 最新self-contained fixture SHA `4a6ee11ef5ad498b55aa71ba9910b475a461e6ad04117465943aed804a0db145`→`2c392661a9a9b3027206da95101684af3df4623592c7765500db18143c6e7d36`。原16 TestCaseSource源行精确保留，12个原Assert表达式逐字一致。提取共同渲染体，原16仍执行原GUI Validate与一致property/keyword模式。
- 新增8身份：keyword on/toggle off，以及keyword off/toggle on，分别depth3/7×正交/透视。Graph先Validate再显式设置与Frozen/current相同keyword，按keyword的有效开关执行原严格finite/ABC/control/repeat/强响应/visible断言。新增metrics记录toggleEnabled和keywordAuthorityMismatch，新增capture目录前缀区分两种失配，避免覆盖原16。
- `plan.json`给出24个全限定exact IDs和8个delta IDs，filter `NBFX.Baseline.Tests.G4GraphOverrideDepthTests`。这是扩展24个唯一身份，不是原16加新24得到40；所有新用例仍未运行，不能宣称合同已闭合。应由主Agent将同一adapter应用到最新组合/root预览并串行验证。

### OVZ-SOURCE · 2个生成源码与native兼容性预览用例

新增独占 `overridez-generated-source-preview`，仅新建 `Tests/URP/Editor/G4GraphOverrideDepthSourceTests.cs`、meta、manifest、plan。测试源SHA `8a1db7cbb7601cb820b9d8e792148eab59fb58ba3d1acad52f0e714ed9d6c0ca`，meta SHA `6d39787f0b75b0b465896e29344228ec7d1273f1ed599f52606076297782c96b`；未安装/运行，未改现有源或Graph。

- `G4OverrideZSource_generated_pass_scope`沿用G4GraphChromaticTests实际用过的GetShaderText四参数/out GraphData反射，读取真实安装Graph，写完整generatedShader、逐Pass pragma摘要与输入SHA。只允许normalForward拥有一个`shader_feature_local_fragment _OVERRIDE_Z`轴，其余全部Pass禁止该轴；要求DepthOnly/ShadowCaster/two NB Pass存在。再检查安装Forward wrapper的conditionalSV_Depth声明/写入、adapter的keyword authority、同一共享数学include。无需主动ImportAsset/Refresh/渲染。
- `G4OverrideZSource_srp_batcher_codes`读取真实导入Frozen/current/Graph的subShader0 native code及非0reason，先存JSON再断言。缺API/signature、缺shader、未知码或未知reason严格失败；Graph要求0，current只允许改善到0或保持Frozen原code/reason，原baseline非0留实录，不因此把不兼容Graph当通过。
- 只读PE元数据确认安装 `UnityEditor.CoreModule.dll`的internal static `int GetSRPBatcherCompatibilityCode(Shader,int)`、`string GetSRPBatcherCompatibilityIssueReason(Shader,int,int)`，Blob分别`0002081285CD08`、`00030E1285CD0808`。安装DLL SHA `e539c23b5ea545f41677a2c925747a5e39bbdaee5c4e84e5c4e698ff2cb9641d`。
- 同DLL的`ShaderInspector.OnInspectorGUI`在call offset341之后以`brfalse.s`选择`compatible`文本，非0分支选择`not compatible`；0码语义来自本版本内置Inspector，未猜测/放宽码值。该证明只确认API/码义，绝不代替3个真实Shader的运行查询。
- plan有2个全限定exact IDs，filter `G4OverrideZSource_`。这是generated source/native compatibility证据入口，不代表图像/runtime/Player/stripping/performance通过。
