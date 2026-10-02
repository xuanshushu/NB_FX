# NBPostprocess Controller/Manager首片预览

只新增G4NBPostFullControllerTests.cs/meta；未安装、未Unity运行，未改产品/中央Graph/官方包/主Assets/RendererData文件。源审查SHA与未来实际消费package/Graph生成shaderSHA分开。当前暂rootUVP源不代表VAT全组合。

## 真正执行链

NBPostProcess.Create建原runtime Uber、ScreenColor/Mask/CameraOpaque各Pass，AddRenderPasses按原CameraOpaque→AfterTransparents Copy→Mask→Uber执行。Controller.OnEnable注册真实Manager index/toggles，Controller.EditorUpdate/Manager.EditorUpdate调用原Update/LateUpdate，Manager真实聚合参数写flags和Uber，Controller.OnDisable原逻辑释放index。测试只调用原组件路径，不设置伪造flags、不复制Manager逻辑。

Volume不是这条feature的前置，camera.renderPostProcessing=false仍使用原NBfeature；Game相机、requiresColorTexture=true、当前RenderGraph/当前downsampling是本片明确环境。不能拿Compatibility/不同API结果替代。

## 有限12身份与先2入口

2个精确NB模式×正交/透视×Overlay、Flash、multi-camera Overlay=12；建议先native opaque/deferred overlay ortho两个原身份验证fixture可信，再跑剩余计划身份，不重复相加14。

每case两真实Controller验证index、toggle bit union、max .4/.7（Overlay）或.3/.65（Flash）、第二组件disable/reenable恢复及最后全disable释放；双Mesh原tag绘制验证叠加、strength0和单对象强控制；透明底层on/off、SceneCopy部分alpha均保留。4个multi-camera case核对第二镜头强差别/重复以及回到primary无污染。

最终图、原生Mask、SceneCopy、OpaqueCopy分别raw保存与ABC严格0差/repeat0/finite。Mask特别有strength0/双mesh控制，CameraOpaque不得误画DeferredMask。仅overlay flash成功不能让空mask或错误native采样通过。

## Observer边界

纯读observer借用原G2MaskView.shader，在AfterTransparents+1读取已经由原pipeline生成的globalRT并在另外view-frame显现。它没有NB标签的RendererList，不重画特效，不替代Controller/Manager/原feature。最终帧observer关闭；View帧和final结果分别记录。

Observer在原renderer warm和static快照前加入memory feature-list，避免Renderer重建时替换Uber导致flag绑定漂移。只限独占隔离、无现存loaded Manager/Controller；如该环境条件不成立应失败并指出具体冲突组件，不去销毁用户对象。所有managedstate/runtimeMaterial恢复、owned资源清理，RendererData/pipeline文件SHA前后必须相同。

## 未验证范围

Roslyn以当前49源码真实reference/define全程序集静态编译exit0，无Editor启动/刷新。Shader/GPU/Manager runtime行为尚待main serial执行。兼容、降采样、SceneView/overlay-camera/MSAA/分辨率、其余后处理效果、Player/性能及冷首帧均未据此闭合；三生产tick用于消费已有isFirstUpdate skip，不替代冷首帧验收。
