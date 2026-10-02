# STATE-OFFSET-1 首片预览

未生成/修改当前 SubTarget；未写中央 Graph、HLSL、GUI、官方包或 NBPostprocess；未运行 Unity。`build_zoffset_subtarget.py` 只由主 Agent 在串行窗口调用，在调用时读取所选 package 的最新 SubTarget，输出到本目录下的新子目录。

| 字段 | 旧 NBShader | Graph 首片 |
|---|---|---|
| `_offsetFactor` | Range(-2000,2000)，float 存储，默认0 | 同名普通 Float，默认0 |
| `_offsetUnits` | Range(-2000,2000)，float 存储，默认0 | 同名普通 Float，默认0 |
| `_ZOffset_Toggle` | GUI 开关；关闭时现有 TABigBlockItem 将两值归零 | 本片不新增/改 GUI 开关或写入管理 |

ShaderLab 的 Offset 指令始终直接读两个数值，不以 toggle 为条件。因此测试保持 toggle0、从脚本写非零数值，验证真实 render-state 合同。

旧普通四色 Pass（UniversalForward、两种 NB distortion、Universal2D）均带 Offset；另一个 BackFirst/SRPDefaultUnlit 在595行也带 Offset。DepthOnly/ShadowCaster 无 Offset。本片只覆盖已有 Graph Forward + 两 NB Pass；BackFirst、Universal2D 和完整组合是未验证范围，不能写成“旧路径原本排除Offset”。

最小生成层改动：现有 collector 增加两个同名 Float default0；现有 `WithNBRenderStates` 仅 `colorMask=true` 的 ZTest descriptor 通过 `WithNBZOffset`，原 type/fieldConditions 与其它 states 保留。现有 AddDistortionPass 只包装其原 ZTest descriptor。Helper 保持 ZTest 文本，追加独立行 `Offset [_offsetFactor], [_offsetUnits]`；使用已读取的 SG17.3 descriptor API，没有虚构 RenderState.Offset 或新 descriptor type。

`plan.json` 只有 source 生成的12个 exact identity：三 Pass × Factor/Units × 正交/透视。每项是真实共面板写 depth，再画 NB actor，通过正/零/负 bias 检查深度拒绝；Factor 使用20度斜面（±16），Units 使用平面（±512）。Actor关闭的 depth-board捕获、A/B/C每态及repeat全部保留浮点raw；finite、visible>150、signed响应>.01、全帧AB/BC/repeat严格0。没有源码包含判断代替 GPU 结果。

相机/对象使用 preview scene；只临时插已有定向 NB RT test feature，保留并恢复 NB feature active、RendererData列表、RT/async状态，核对Renderer资产原字节。不改生产Renderer/场景。测试不自行Import中央Graph；主 Agent 负责安装、检查loaded scenes/TAI和导入/编译。

上下文明确是当前 root35OVZ / source3eb 与隔离临时RootUVP1132，不是全部渲染组合。manifest 保存实际 SubTarget/Graph 字节SHA；生成脚本不复制任何旧整文件或改主源，锚点不匹配/重复Offset时立即拒绝。

检查仅 Python AST；脚本未执行，候选 Target 未生成，12项均未实跑。
