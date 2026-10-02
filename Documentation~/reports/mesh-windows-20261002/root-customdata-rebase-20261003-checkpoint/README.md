# Root CustomData 重基准备

脚本与plan预备，未生成候选、未安装、未运行Unity/Git，未写中央源。

`build_root_customdata.py` 接收 `--current-root-package`、`--expected-source-sha256-json`、`--output`。七个实际输入源码必须逐文件SHA吻合；UV helper 必须已有真实F0 Flipbook输入，否则拒绝。当前Root输入示例仍是pre-F0，不能用于绕过该要求；主Agent在F0集成后保存fresh SHA再运行。

这是原 `build_customdata_preview.py` 增量脚本的薄适配：每次从当前图克隆原Float半字property/node/slot，补剩余word1/2/3及既有UVVertex/BaseUV/VertexOffset/BaseColor消费接口，保留旧Target/OVZ/UVP/F0节点、属性值、边和相对slot顺序，不复制旧wholeGraph。调用前的严格锚点不符就停，不猜新版接线。

沿用原4个nibble word及 `NBShaderFlags` Material ReadWord/WriteWord半字存储后端，旧integer/MPB路径仍保留。没有新packed bit、keyword、resolver或GUI体系。新已消费Float半字使用non-hidden exposed schema，与先前Mask/Noise导入问题修复一致；material Inspector继续由已有控件负责，不声称正式VFX。

Root尚无真实VAT节点或source时，不加VAT字段/output/假node，VAT frame消费另留待真实VAT片。word2依然是真实存储及其它消费点。Root无Noise Tier pair时，原噪波CustomData在实际Noise块之前注入，保持是否运行的原条件，不凭空加Tier gate。

writer前核对四个实际CF的 filtered inputs+outputs完整顺序与float/half形参，避免raw slot输出在中部产生误判。已有BaseUV half→float转发沿原增量脚本补8个实参。最终HLSL编译和实际RootGPU仍须主Agent执行，AST不代替ABI/GPU验证。

plan为32 storage +120非VAT consumer，共152个既有source identity；原4个VAT frame明确deferred，无skip/pass结论。CA原生故障分支仍排除并保留历史记录，12个旧严格微差不因新重基变为通过。当前产品不含G4GraphCustomDataTests，主Agent须审核安装现有隔离/归档fixture，不能把plan当已discover/已运行。

验证预案：runtime32存取、实际消费者各9selector/常量和插值及两相机，严格原finite/visible/repeat/response/0差；确认新RootOVZ+UVP+F0组合仍正常。必要回归随实际共享HLSL变化由主Agent选择，不重复没受影响的历史Gate。
