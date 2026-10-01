from pathlib import Path
import xml.etree.ElementTree as ET, json, gzip, hashlib, struct, collections
root=Path('/tmp/nbfx-g4-depth-parallax-full-20261001')
xml=Path(str(root)+'.xml'); log=Path(str(root)+'.log')
audit=Path('/tmp/nbfx-dd-pom-state-audit-20261001'); audit.mkdir(exist_ok=True)
tree=ET.parse(xml).getroot()
fixtures=[]
for s in tree.iter('test-suite'):
 if s.get('type')=='TestFixture':
  fixtures.append({k:s.get(k) for k in ['name','start-time','end-time','total','passed','failed']})
fixtures.sort(key=lambda x:x['start-time'])
raw=[]
standalone=Path('/tmp/nbfx-g4-parallax-v2-20261001/g4-parallax/on-ortho')
for p in sorted((root/'g4-parallax/on-ortho').glob('*.rgba-f32.gz')):
 b=gzip.decompress(p.read_bytes()); f=struct.unpack('<'+str(len(b)//4)+'f',b)
 frames=list(zip(*(iter(f),)*4)); counts=collections.Counter(frames); prev=standalone/p.name
 raw.append({'name':p.name,'payloadBytes':len(b),'payloadSHA256':hashlib.sha256(b).hexdigest(),'isAllZero':not any(b),'uniqueRGBA':len(counts),'dominantRGBA':counts.most_common(1),'matchesStandaloneV2': prev.exists() and b==gzip.decompress(prev.read_bytes()),'source':str(p)})
clone=Path('/tmp/NBFXG2DissolveMaskProbe-20260928/Library/PackageCache')
core=clone/'com.unity.render-pipelines.core@43eeb73e554f'
urp=clone/'com.unity.render-pipelines.universal@35356061dd01'
refs=[(core/'Runtime/RenderGraph/RenderGraphResources.cs','18-27,57-61,73-76,92-109'),(core/'Runtime/RenderGraph/RenderGraph.cs','573,1597-1609,1662-1680,1829-1834,754-769'),(core/'Runtime/RenderGraph/RenderGraphResourceRegistry.cs','116-122,309-322'),(core/'Runtime/RenderGraph/RenderGraphResourcePool.cs','44-46,66-102,230-260'),(core/'Runtime/RenderGraph/RenderGraphCompilationCache.cs','34-39,47-89'),(urp/'Runtime/UniversalRenderPipeline.cs','29-58,449-518,544-676,698-720,746-761'),(urp/'Runtime/UniversalRenderPipelineRenderGraph.cs','8-29'),(urp/'Runtime/RTHandleUtils.cs','21-26,49-52,66-77,101-136'),(urp/'Runtime/RenderingUtils.cs','1019-1023'),(urp/'Runtime/UniversalRendererRenderGraph.cs','461-487,1685-1723,1796-1817')]
sources=[{'path':str(p),'lines':lines,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p,lines in refs]
jsondata={'scope':'Read-only audit; no Unity/Git/product/test mutations. V2 Root replay currently running, not evaluated by this report.','v1XML':str(xml),'v1XMLSHA256':hashlib.sha256(xml.read_bytes()).hexdigest(),'v1LOGSHA256':hashlib.sha256(log.read_bytes()).hexdigest(),'fixturesInExecutionOrder':fixtures,'firstPOMFrames':raw,'officialCloneSourceReferences':sources}
(audit/'raw-and-source-summary.json').write_text(json.dumps(jsondata,ensure_ascii=False,indent=2)+'\n')
lines='\n'.join(f"{x['name']}: {x['total']} total / {x['passed']} pass / {x['failed']} fail; {x['start-time']} -> {x['end-time']}" for x in fixtures)
refsText='\n'.join(f"{x['path']}:{x['lines']} ; sha256={x['sha256']}" for x in sources)
report=f'''DD / POM 全矩阵空图：只读宿主审计（2026-10-01）

结论先说人话
- v1 的这次后半程不是“ShaderGraph 和旧材质颜色不同”，而是抓到整张空图，连相机本该清出的背景也为零。此时相同的空图 B/C=0 不构成正确渲染或功能通过。
- 首个 POM 用例里，Graph warm 和 Frozen A 的 raw 与单片 v2 完全相同；紧接 A-repeat 首次变成全零，之后 B、C、off/control、后续新相机均全零。问题发生在已有正确 Frozen 图像之后，不能据此先改 Shader 算法。
- DD 之后仍有数百次正常渲染。不能写成“DD 结束就把所有后续画面污染了”，也不能现在证明 DD 完全无因果。当前证据只能精确定位 v1 的首次可观察坏 capture。
- 无 root cause 已证实；Root 已启动不改材质/RT 的 v2 callback 观察回放。本审计未运行 Unity。

一、实际顺序（从 XML start/end，非 testFilter 排列猜测）
{lines}

二、首个 POM on-ortho 的硬证据
1) C-warm raw payload sha256=d06f6ffb0a156ebf63955835bf18fb196b04a2ba5817a7064b57874b59f96566，逐字节等于 standalone v2。
2) A-frozen sha256=f9f98f8f2893e00d6446160289065b65c678ab78e4c595f3e72377e8edcbc416，逐字节等于 standalone v2；5250 种 RGBA，其中背景 (0.0116424560546875,0.0245208740234375,0.043243408203125,0.35009765625) 占11132像素。
3) A-repeat、B-current、B-repeat、C-graph、C-repeat 和六张 off/control capture 都是128x128x4完整262144 bytes的 float32 零值，payload sha256=8a39d2abd3999ab73c34db2476849cddf303ce389b35826850f9a700589b4a90。
4) metrics.json finite=true, aVisible=4625, b/cVisible=0, aRepeat=1。finite 与 B/C=0 没有说明宿主有效；visible/repeat断言正确把该轮判坏，没有放宽标准。
5) Capture 每次4个 Camera.Render，没有逐帧 raw，只能定位“第一张 A capture 最后一帧正确，A-repeat 最后一帧失败”；不能指定4次 repeat 中哪一次首次坏。
6) log30024 强制重导入Graph，30074首POM metrics；其间无 C# / Shader / Metal GPU hang / allocation error / rendergraph exception 日志。没有错误日志不是 GPU正常的证明。

三、源代码状态污染检查（未发现直接全局漏恢复）
- DD：自己的 Material 设置Cull/ZTest/ZWrite/blend/flags/ColorMask；不写 renderer opaque/transparent masks，不写 pipeline defaultRenderer，不写 RenderSettings，不写全局shader状态。
- DD Tests/URP/Editor/G4GraphDepthDecalTests.cs:148-156 临时关闭NBfeature/加入directed feature；258-268 finally删除并Destroy directed、恢复NBactive、SetDirty、targetTexture=null、activeRT还原、释放/销毁自己的RT/材料/物体、ClosePreviewScene、验证renderer磁盘bytes。
- G4ScreenNoiseDirectedFeature:510-559 仅绑定同一camera和pass rendererList，activeColor/depth附件，不存RT/SetGlobal/改viewport或矩阵；DD有remove。后续有效渲染也反证“始终残留一个会清空所有camera的directed pass”这一简单解释。
- POM：没有改Feature或RD.SetDirty；自己的相机、previewScene、Mesh、RT、材料；184-191 finally还原 activeRT/target、释放RT与材料、ClosePreviewScene。Configure拥有各Pass/Surface/blend/_ColorMask等写入，Apply仅本材质POM/Wrap/NoMip协议。
- POM Capture:345-364 Camera.Render×4后读自己的RT，finally还原activeRT；但rt.IsCreated仅setup查询，未逐render验证pipeline callbacks/RT实例、尺寸/相机culling成功等，因此GPU/renderer/RT未执行也可以得到一张finite零图（由positive control捕获失败）。
- Lighting/DepthShadow的RenderSettings变更有finally还原；NormalMap/MatCap/MeshParity自己Material/RT作用域。GUI1A warm临时NBactive和ShaderUtil.allowAsyncCompilation有finally还原，且GUI之后的三个渲染fixture仍可见。未找到上述字段在DD/POM中未恢复的直接写。
- RD.SetDirty会令后续URP按需重建Renderer（UniversalRenderPipelineAsset.scriptableRenderer/GetRenderer），不是直接变更磁盘，但重建之后的系统状态还需要实测observer，不应把disk bytes相同当作所有native资源正常。

四、累计执行计数与资源边界：官方本地源码说明什么
A. RenderGraph不是“16位renderCount到65535就没了”
- RenderGraph.cs:573 m_ExecutionCount是int；1609每次BeginRecording自增，与Time.frameCount分离。
- ResourceHandle上16位是validity hash，低16位是当前graph资源index。NewFrame:92-109对executionIndex混洗，并显式跳过0和共享值；不是计数达到16384/32768停止分支。
- 每次ClearCompiledGraph:1829-1834清本轮资源/rendererLists/globals；ResourceRegistry:116-122将resourceArray size恢复 sharedResourcesCount+1，不把每次所有资源index永久累加到16位上限。
- 因此暂未从C#源证实所谓累计次数硬边界；native engine/GPU内部不在这些开放源码内，不能排除所有驱动/句柄耗尽。

B. 同Editor frame大量Camera.Render不等于所有RT不能复用
- URP RG参数使用Time.frameCount（UniversalRenderPipelineRenderGraph.cs:8-29），但Core pool记录frameIndex和executionCount；RenderGraphResourcePool:66-102 正常别名模式直接复用，禁别名时也允许前一次execution的资源复用。
- URP Render():517-518会调用RG.EndFrame和RTHandlePool.PurgeUnusedResources；purge需要真实frame前进（RG旧资源10帧，URP staleRT3帧）。此规则给诊断提供frame相关候选，但尚不能证明本轮同Editor frame积累泄漏。
- URP staleRT pool容量32，满时AddResourceToPool返回false；RenderingUtils:1019-1023释放旧handle，不是停止新的camera渲染。
- GraphCompilationCache仅20个compiledGraph槽，不足时47-89复用最旧slot并Clear；没有“第21种graph起直接跳过”的源分支。
- CameraMetadataCache为int cameraID字典，仅ProfilingSampler，未找到最大camera数量早退。没有足够证据归为GPUalloc阈值。

C. 一个直接可导致“连背景都没有”的无日志早退
- UniversalRenderPipeline.RenderSingleCamera:746-761：若TryGetCullingParameters=false直接return，发生在Clear/Record/Submit前，且该分支不发warning。
- 另一分支 invalidrenderer/invalid target size/Overlay renderrequest 有warning，本轮没有匹配日志。不能只凭无日志推断唯一是culling false，但可在后续observer独立记录TryGetCullingParameters/sceneLoaded/active/pixel dimensions/UAC renderType/renderer。
- begin/endCameraRendering callback成对出现也不保证实际draw/clear：scope在渲染调用周围，内部早退仍触发Dispose/end。这些callback只能证明宿主调用路径到达。

五、Root串行最小复跑建议（不改变原严格标准）
1) 等正在运行v2：记录累计begin/frame和首64 POM begin/end+RTCreated+instanceID，留v1完整日志/raw；不要用observer自身作产品通过证据。
2) 若v2正常，记“单次原始全矩阵出现宿主空图，重放未复现；根因未证实”，保留异常，不将v1替换/删除，也不称任何已知微差解决。
3) 若v2复现，先扩首POM capture诊断：4个render逐次存最小raw统计；RT.IsCreated/width/height/format、camera.targetTexture精确identity、camera rect/pixelWidth/height、camera.scene valid/loaded/active、UAC Base、renderer实例和feature active列表；Begin/End次数；TryGetCullingParameters的独立bool。输出发生在已有正确A之后，可严格定位首坏调用。
4) 同新process跑NormalMap44→POM26（或仅POM on-ortho）再反向POM→DD→POM，用隔离快照但不改产品/Threshold；如果仅大矩阵累积复现，再以相同fixture顺序分前后两个新process回放全部1105，确保无遗漏，说明并非单process状态完整证明。
5) 需要区分RT/readback与URP时，用独立的test-only RT读取哨兵：自己RT先Graphics/GL clear非零并readback；再只有相机background无Mesh render/readback。不得把哨兵加进原expected图像，不得清零后继续算B/C pass或自动偷偷修复renderer后不记异常。
6) 可以在坏capture首次出现就Abort该fixture/测试宿主（明确host-invalid），避免后续几百张空图污染“最新正常矩阵”；strict0、positive controls、finite、repeat照原不变。只有通过Root审核后实施；本代理没有写测试。
7) 若日志指向已发布官方/native Renderer问题，只提交证据和暂时分批验证边界，不修改官方包、Target或NBPostprocess，也不削减功能。现在没有需要用户决定这些改动的证据。

六、官方clone源码固定引用与hash
{refsText}

边界
- 本次是只读根因定位与运行宿主建议，不是修复或实测通过。没有Unity/Git操作；所有写入只到/tmp。
- 未证明完整NBPostprocessController链、完整普通Mesh G3/G4、真实VFX、Player、其它API/管线。
- CA静态Pass define/clamp预览另有独立交付，此空图不能拿来判断CA。
'''
(audit/'audit.txt').write_text(report)
print(json.dumps({'report':str(audit/'audit.txt'),'reportSha256':hashlib.sha256((audit/'audit.txt').read_bytes()).hexdigest(),'summary':str(audit/'raw-and-source-summary.json'),'summarySHA256':hashlib.sha256((audit/'raw-and-source-summary.json').read_bytes()).hexdigest()},indent=2))
