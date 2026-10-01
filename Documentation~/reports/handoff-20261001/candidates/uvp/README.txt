UVP 普通 Mesh 位置 UV 候选 — 换机交接冻结，2026-10-01

状态先说清楚
这是“待审查/待应用/待 Unity 验证”的候选，不是通过报告。
四个产品候选和新测试都没有应用到产品；本归档不启动 Unity、不修改产品或 Test。
离线 C# 检查 exit0 与结构审计不等于 Shader/HLSL 编译，更不等于 Mesh 画面通过。
普通 Mesh、完整 GUI、G3/G4、VFX、Player、HDRP 均不能由本档放行。
Git 提交/push 和换机集成由主 Agent 负责；本 Agent 未执行 Git。

如何看附件
- dynamic-preview.py.gz：最新动态构建入口原始字节，只生成 /tmp preview，不含 apply。
- full-preview/：四个完整候选、三个文本 unified diff、一个 Graph 语义审计 diff、manifest 和原 README。
  Graph diff 不是可直接 patch 的 unified diff。完整 Graph 快照仅供审查，禁止覆盖实时产品。
- G4GraphPositionUVTests.cs.gz/.meta.gz：新测试候选，尚未复制进产品 Tests。
- consumer-batches.json.gz：376 个预期 case 的精确名称和九个独立进程 filter。
- audit-candidate.py.gz、static-structural-audit.json.gz/.log.gz：仅静态结构证据。
- compile-*.py.gz、static-*.rsp.gz/.log.gz/*-result.json.gz：原始离线检查记录。
- static-subtarget-renamed-identity.log.gz：首次把输出程序集改名导致 InternalsVisibleTo 不匹配的离线失败。
  随后只改 /tmp 输出文件名为 Unity.RenderPipelines.Universal.Editor.dll，实际程序集源码重编译 exit0。
  不把此日志当 Unity/产品编译失败，也不把后续 exit0 当 Unity 验证。
- final-handoff-sha256.json.gz：冻结前的最终候选 16 项原 payload hash，归档已逐项核验。
- source-to-archive.json：所有 38 个原附件的真实 source→dest、原字节数/原 SHA、gzip SHA 和无损核验。
- sha256.txt：归档附件与本说明/清单的最终 SHA。自身 SHA 由交付消息给出，避免自引用。
- history/NOTAPPLY/：11 项旧 phase0 历史。旧 helper 拆分/旧 schema scaffold 不需要应用，不能和最新四文件混用。
全部原附件使用无文件名、mtime0 的 gzip；保留空成功日志。解压后的每一字节都和原 /tmp 文件相同。
没有复制任何官方 DLL、PDB、Library/cache；rsp 只是文本，含旧机器引用路径。

固定输入与候选（SHA256）
构建时 FG1 Graph 输入：24bb84c9f37b34256f305a86dad0e3b4e4f74ee9db7895fec64f906723284da0
输入 BaseUV：305b618e9bb02276a64844db909ce58613529a88069161f9dc821d473ec3b33f
输入 VertexOffset：e9ef0af1b77e7179d8ec83a77867d58359348f2483901d7bdfd687450f9930a5
输入包内 SubTarget：63358e53b9067af300c57b3c77117928e3e8c27c96eb203efe7777e52feb565f
这些是当时输入，不是新机器实时源，也不是归档时后来工作区内容的替身。
输入旧 shared UV/Forward/Input/BaseColor 哈希保存在 full-preview/manifest.json.gz。
输入源本身未另取未来工作区重新快照；以已 push 源、实时 Git 和 manifest 重新校准。

仅四个产品候选路径：
NBShaders2/ShaderGraph/NBGraphBaseUV.hlsl
  6075eb17f4b6154f934e9db3273b5f0dc042f1c8da9b8a251efa9650bb20a29a
NBShaders2/ShaderGraph/NBGraphVertexOffset.hlsl
  18720533d8c42ef4cc25a8a494033d42eecce6d320bd315c41ae66bb9469e8a5
NBShaders2/ShaderGraph/Editor/NBGraphUnlitSubTarget.cs
  f38fb46a1c338a1b9738ce6c069acb4696eb16f9afe0f42ccb2b701f26df7654
NBShaders2/ShaderGraph/NBShaderGraph.shadergraph
  16bc0ba2cdb2d01168b41b9108c4b38b82a3cf653bfc80830e2b60fb68ad7d5d
新测试：3c2aba46b0fe2ee0ca49819d1b97f2e0cfaf26e8454600b2ca79ad707767100e
动态入口：026761e481b2edb080e976d4e544a93ad00826b77ada84f19f2c7878cca89c82

新机器的安全流程（不要直接运行归档脚本）
1. 主 Agent 先查实时 Git/Editor 状态、主工程与 NB_FX 分支/依赖/Unity 版本。
   文档中的旧机器路径、输入 hash、隔离 clone 名、Bee DAG 名都不是新机配置。
2. 只解压到新的专属临时工作目录。归档内脚本都压缩保存，没有自动执行或安装钩子。
   不把 full-preview 下的旧 wholeGraph 复制到 live package，不把 history 文件当补丁。
3. 在临时工作副本中人工审查、重新绑定 dynamic-preview.py 的 P/OUT。
   原 P=/Users/bytedance/UnityProject/NBUnityProject/Packages/NB_FX，原 OUT=/tmp/nbfx-position-uv-candidate/full-preview。
   builder 的已知 FG1 rawPositionNode、共享原有插值/Slot/Pass 接缝断言需对新源复审；不能删除断言硬套。
   从实时四源和 readonlyDependencies 重新 preview，审查新 manifest input/candidate hashes 和所有 diff。
   若实时输入不等于上列冻结输入，旧四文件不能直接 apply；应审查动态重新生成结果。
4. audit-candidate.py 的 D/O/P 同样需绑定新目录。它只验证结构/源码 hash，不编译 HLSL。
5. 离线 compile 脚本及 rsp 不可原样重放：
   原 clone=/tmp/NBFXG2DissolveMaskProbe-20260928；Bee=Library/Bee/artifacts/200b0aEDbg.dag；
   原 SDK=/Applications/Unity/Hub/Editor/6000.3.18f1/Unity.app/Contents/Resources/Scripting/。
   新机应从当前隔离 clone 的实际编译产物重新发现 rsp、Unity SDK、程序集源码/refs，重新生成工作 rsp。
   保持实际 Unity.RenderPipelines.Universal.Editor 程序集 identity，否则无法访问合法内部 API。
   新测试只用真实 URP 测试程序集已有引用；不要添加假的 NB runtime -r 或复制官方 DLL。
6. 主 Agent review 后按可回退切片应用，再独占隔离 Unity 导入/console/串行实测。
   有可用 unityMCP 时优先使用；否则由主 Agent明确原因并使用 CLI/Pipeline。
   手动可见 Inspector 检验最后集中进行，不要求用户持续值守。

候选实际范围
- 复用唯一 NBFX_BuildBaseUVsV1/GetUVByUVMode；没有复制位置 UV 算法或新增独立 packed 协议。
- preVO 原 ObjectPosition 源与 FG Fog、VertexOffset 共享，顶点计算 cylinder/screen/world/object、完整 Main 和 Shared。
  输出 3*float4；另一个 float3 自 VO.OutPositionOS 传 postOS。fragment world/pixel 复用现有实际节点。
- BaseUV 追加 12 inputs IDs37..48，VO 追加 10 inputs IDs37..46；所有旧端口/属性/halfword defaults 保留。
- Twirl/Polar 强制 fragment；主 Forward 的 Decal/POM 强制 fragment；两个 NB 扭曲 Pass 恒 fragment。
  包内现有 WithNBPassDefine 给 Deferred 加 pre-CF static define；不以材质 DistortionMode 猜当前 Pass。
  Depth/Shadow/普通2D不因为 POM/Decal uniform 切换阶段；POM 仍只主 Forward。
- 8 个旧名实际 selector/cylinder 属性；matrix 默认全零与旧 ShaderLab 一致，不伪造 identity 默认值。
- VertexMap/VertexMask 的 source3..7 候选接同 preVO Build，原 half2、LOD0、ownST/scroll/方向/强度顺序保留。
- 旧 ShaderLab/UVV1/Input/Forward/BaseColor、官方包/Target/NBPostprocess 不变；无新 keyword。
- Graph 997→1090；只旧 Root/category/BaseUV/VO 四个对象追加列表或 forceSingle；旧 edges/slots/objects保留。
- custom components 23→38；noncustom9 + URP padding16=25，源码预算合计63 components（15.75 float4）。
  这只是源码层 SG 阈值估计，不是实际 Metal varying packing/register/API限制/性能通过。
- 三 CF input/output 精确类型/顺序、所有 edge endpoint、唯一 fanin、含 CI 隐式阶段的 DAG 静态审计通过。

376 新测试到底证明什么（目前未运行）
预期数：9 consumer*5 modes3..7*direct/shared*vertex/forced-fragment*ortho/persp=360；
加 Main6/7 selector0/2 两阶段两相机16，共376。不是实际 NUnit discovery 数，也不是376通过。
九进程：Main56 + Mask1/2/3、Overlay1/2、ColorRamp、Dissolve、DissolveMask 各40；不删 case/降阈值。
实际 Frozen A/current ShaderLab B/product Graph C、真实业务 setter、非平面粗 Mesh/旋转/非均匀缩放。
真实 URP Camera warm 后读 schema；RGBA raw float32 gzip+PNG；全帧finite、ROI40..87严格0、重复严格0、
可见像素和反事实变化强断言。Main自读mode4等价route0后改ST作强control；Shared4保持zero-before-Main。
离线 fixture compile exit0/实际URP Editor源码候选 compile exit0 仅说明 C# 静态可编译。
分进程源于已见同进程宿主空图/非finite不确定故障，不声称固定 draw 数量上限或根因已确定。
空图、NaN、repeat/可见性失败不许当 B/C 0 差通过。不能放宽标准掩盖真实差异。

仍待完成，不删功能
1. 各 feature 自己的 ST/rotation/scroll 仍在 BaseColor consumer CF 的 fragment 阶段。
   本片只是 source stage +完整 Main/Shared stage，不是全部旧 ParticleProcessUV finaltransform 的 literal 迁移。
   仿射交换不能保证 float严格0；真实微差只能记录，下片依据实测审查共享 transform/stage 插值。
2. 新376只覆盖上述九表面 consumer。另六路 Noise、NoiseMask、Bump、PNoise、VertexMap、VertexMask
   有 source 接线候选，但没有新的位置UV图像/几何实测；需要在各现有 fixture 扩展强因果测试。
   旧14 VertexOffset回归不等于新3..7几何通过。
3. 完整 CustomData UV偏移words、CustomLocalTransform、VAT、UI/Particle/MPB未实现/未验证。
   未来VAT/CustomLocal插入必须在同一 postVAT/preVO host seam：PositionOS同步供FG Fog、UVVertex、VO，
   preVO NormalOS供VO/geometry；不能接最终VO位移后位置或用world->object替代普通Mesh原object varying。
4. Cylinder matrix 自动安装、派生bit、可见GUI、Undo/persistence、FeatureIntent/Tier有效投影待完成。
   2D screen-mode完整组合、完整所有 PassCombination、全部 ordinary Mesh GUI未通过。
5. 完整 NBPostprocessController/Manager→正式RT→合成链、真实VFX/Player、正式G3/G4和HDRP仍pending。
   保留SupportVFX资产设置并不证明VFX渲染通过。本档不能替代后续正式 Gate。

归档统计
最新27个payload +history11个payload=38；原2,249,524 bytes；gzip243,336 bytes。
38/38 解压逐字等价；final-handoff 16/16 entries payload hash/bytes吻合。
只写本目录；不写中央索引、其它报告、产品/Test、官方包或主工程设置。
