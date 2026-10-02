# DN0 默认链路有限扩展16预览

产品、DN0 SubTarget、Graph、当前隔离源均未修改。只安装manifest列出的4个Tests文件；编译通过后按dn0-default-coverage16-plan.json的4×4串行新进程运行。

| 配置（每项2相机） | 几何 | 额外实际风险/控制 |
|---|---|---|
| softbody-normal-bias0 | Houdini SoftBody | normalmap+BumpScale1/0+FresnelRGB；normal bias0 |
| softbody-clip-bias1 | Houdini SoftBody | Mask alpha cut .5/on-off强控制；normal bias1 |
| tyabsolute-vatnormal-bias0 | Tyflow absolute | 实际VAT法线block+includesNormals1/0+FresnelRGB；bias0 |
| tyrelative-clip-bias1 | Tyflow relative | Mask裁剪；bias1 |
| customlocal-hpositive-bias0 | CustomLocal+SoftBody，正非均匀scale | 模拟world-space mesh与实际矩阵；bias0 |
| customlocal-tnegative-clip-bias1 | CustomLocal+Tyflow relative，负非均匀scale | 实际矩阵/帧/裁剪；bias1 |
| stencil-softbody-fail-zfail | SoftBody | raw Fail/ZFail=Replace，Write255，probe200和native writer200/0强控制 |
| stencil-tyrelative-fail-zfail | Tyflow relative | 同上 |

所有case调用同一defaultFullChain，旧真实main/DepthOnly/ShadowCaster可用，C DepthNormalsOnly rawTrue，SSAO on，无定向RendererFeature。默认全链路帧在Stencil oracle开启前已经实拍，oracle只作额外普通UniversalForward对象读回控制，不替代默认绘制。原2身份/内容及旧57（本输入全部80）行assert逐字保留，无容差变化。新增控制亦finite/repeat0/全帧ABC0，弱响应或微差照常Failed。

ABI已修：唯一private三参CaptureVATDepthAndShadowGeometry→独立四参Core；旧NonPublic Invoke3两个调用源保持原样。原2调用Core；新增16通过public独立CaptureDefaultForwardDepthShadow入口。OneTimeSetUp实际检查private/三参/唯一名称，供主Agent运行验证。

Stencil rawCompAlways/PassKeep/Fail与ZFailReplace/write255是对DN0继承material写入副作用的风险控制，不宣称穷举所有比较分支。新增depth-stencil RT真实格式断言、无writer probe不响应、writerRef200强响应且repeat0、writerRef0恢复baseline；所有控制raw保留。normalmap/clip/VATnormal控制必须强响应，不只检查Float字段。

Standalone Roslyn按当前真实test assembly的references/defines编译全部47源（候选替换两个输入），exit0。没有启动Unity Editor或改当前隔离源；此静态编译不替代Unity Shader/GPU实跑。manifest记录源SHA、before a1b1c7be、静态回执和未验证边界。
