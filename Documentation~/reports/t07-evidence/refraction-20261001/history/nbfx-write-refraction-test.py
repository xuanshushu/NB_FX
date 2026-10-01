from pathlib import Path
import uuid
B=Path('/Users/bytedance/UnityProject/NBUnityProject/Packages/NB_FX')
prior=(B/'Tests/URP/Editor/G4GraphPNoiseScreenTests.cs').read_text()
imports=prior[:prior.index('namespace NBFX')]
configure=prior[prior.index('        static ScriptableRendererFeature FindNBPostProcess'):prior.index('        static void Apply(')]
configure=configure.replace('            if (!graph) m.SetFloat("_DistortPNoiseBlendOpacity", 1);','            m.SetFloat("_DistortPNoiseBlendOpacity", 1);')
tail=prior[prior.index('        static void Toggle('):]
body='''namespace NBFX.Baseline.Tests
{
    // Ordinary Mesh RF1. A=immutable Frozen, B=current ShaderLab, C=actual Graph.
    // Graph uses the original _DistortMode 0 texture / 1 refraction intent.
    // No official assets/Target/NBPostprocess or main project mutation.
    public sealed class G4GraphRefractionTests
    {
        const string GraphPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph";
        const string LegacyPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader";
        const string FrozenPath = "Packages/com.xuanxuan.nb.fx/Tests/Baseline/Frozen/NBShaders2/Shader/NBShader.shader";
        const string Deferred = "NBDeferredDistortPass", Opaque = "NBCameraOpaqueDistortPass";
        const int Size = 128, BackgroundLayer = 2, ForegroundLayer = 3, RoiMin = 40, RoiMax = 88;
        [OneTimeSetUp]
        public void ImportGraph() => AssetDatabase.ImportAsset(GraphPath,
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        static IEnumerable<TestCaseData> Cases()
        {
            foreach (string route in new[] { "Forward", Deferred, Opaque })
            {
                foreach (bool ortho in new[] { true, false })
                    foreach (float ior in new[] { .65f, 1f, 1.5f })
                        yield return new TestCaseData(route, ior, ortho, "flat")
                            .SetName($"G4RF_{route}_{(ortho ? "ortho" : "perspective")}_ior{ior}");
                foreach (string kind in new[] { "normal", "backface", "pnoise", "tir" })
                    yield return new TestCaseData(route, kind == "tir" ? .65f : 1.5f, true, kind)
                        .SetName($"G4RF_{route}_{kind}");
            }
        }
        [Serializable] sealed class Metrics
        {
            public string route, kind, unityVersion, api, note;
            public float ior, maxAB, maxBC, maxOffAB, maxOffBC, maxMaskAB, maxMaskBC,
                repeatB, repeatC, onOffB, onOffC, iorDeltaB, iorDeltaC,
                ignoredNoiseB, ignoredNoiseC, maskDeltaB, maskDeltaC,
                noNoiseModeDeltaB, noNoiseModeDeltaC;
            public int abDiff, bcDiff, offBCDiff, maskBCDiff, visibleB, visibleC;
            public bool ortho, finite;
        }
        [TestCaseSource(nameof(Cases))]
        public void RefractionReplacesTextureNoiseBeforeMaskAndProceduralBlend(
            string route, float ior, bool ortho, string kind)
        {
            Assert.That(GraphicsSettings.currentRenderPipeline, Is.InstanceOf<UniversalRenderPipelineAsset>());
            Assert.That(GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>().enableRenderCompatibilityMode, Is.False);
            var rendererData = ((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline).rendererDataList[0];
            var nbFeature = FindNBPostProcess(rendererData);
            Assert.That(nbFeature, Is.Not.Null);
            bool wasActive = nbFeature.isActive;
            string rendererAsset = Path.Combine(Path.GetDirectoryName(Application.dataPath), AssetDatabase.GetAssetPath(rendererData));
            byte[] rendererBefore = File.ReadAllBytes(rendererAsset);
            string root = Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR");
            if (string.IsNullOrEmpty(root)) root = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/NBFXG4RF");
            string folder = Path.Combine(root, "g4-refraction", route + "-" + (ortho ? "ortho" : "perspective") + "-" + kind + "-ior" + ior);
            Directory.CreateDirectory(folder);
            Shader gs = AssetDatabase.LoadAssetAtPath<Shader>(GraphPath), bs = AssetDatabase.LoadAssetAtPath<Shader>(LegacyPath), fs = AssetDatabase.LoadAssetAtPath<Shader>(FrozenPath);
            Assert.That(gs && bs && fs && gs.isSupported && bs.isSupported && fs.isSupported, Is.True);
            Scene scene = EditorSceneManager.NewPreviewScene();
            GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Quad), fg = GameObject.CreatePrimitive(PrimitiveType.Quad), co = new GameObject("RF ordinary Mesh camera");
            Material graph = new Material(gs), legacy = new Material(bs), frozen = new Material(fs), backdrop = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            Texture2D gradient = MakeGradient(), noise = MakeConstant(new Color(.75f, .25f, 0, .5f)), otherNoise = MakeConstant(new Color(.05f, .9f, 0, .125f)), mask = MakeConstant(new Color(.5f, 0, 0, 1)), white = MakeConstant(Color.white), normal = MakeConstant(new Color(.5f, .72f, 1, .58f));
            RenderTexture target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            Texture2D readback = new Texture2D(Size, Size, TextureFormat.RGBAHalf, false, true);
            Camera camera = co.AddComponent<Camera>(); var data = co.AddComponent<UniversalAdditionalCameraData>();
            RenderTexture oldActive = RenderTexture.active; G4ScreenNoiseDirectedFeature directed = null;
            try
            {
                foreach (GameObject o in new[] { bg, fg, co }) SceneManager.MoveGameObjectToScene(o, scene);
                bg.layer = BackgroundLayer; bg.transform.position = new Vector3(0, 0, 1); bg.transform.localScale = new Vector3(6, 6, 1);
                backdrop.SetTexture("_BaseMap", gradient); backdrop.SetColor("_BaseColor", Color.white); backdrop.SetFloat("_Cull", 0); backdrop.renderQueue = 2000;
                bg.GetComponent<MeshRenderer>().sharedMaterial = backdrop; bg.GetComponent<MeshRenderer>().enabled = route != Deferred;
                fg.layer = route == "Forward" ? 4 : ForegroundLayer;
                if (route == "Forward") Assert.That(((UniversalRendererData)rendererData).transparentLayerMask.value & (1 << fg.layer), Is.Not.Zero);
                fg.transform.position = new Vector3(0, 0, 2); fg.transform.localScale = new Vector3(2, 2, 1);
                fg.transform.rotation = Quaternion.Euler(0, kind == "tir" ? 65 : kind == "backface" ? 205 : 25, 0);
                MeshRenderer renderer = fg.GetComponent<MeshRenderer>(); renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                foreach (Material m in new[] { frozen, legacy }) { Configure(m, false, route, noise, mask, gradient); ConfigureRefraction(m, false, ior, kind, normal, true); }
                Configure(graph, true, route, noise, mask, gradient); ConfigureRefraction(graph, true, ior, kind, normal, true);
                camera.scene = scene; camera.orthographic = ortho; camera.orthographicSize = 1.5f; camera.fieldOfView = 53.13f;
                camera.nearClipPlane = .1f; camera.farClipPlane = 20; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.clear;
                camera.allowHDR = true; camera.allowMSAA = false; camera.cullingMask = (1 << BackgroundLayer) | (1 << fg.layer);
                camera.transform.position = new Vector3(0, 0, 5); camera.transform.rotation = Quaternion.Euler(0, 180, 0); camera.targetTexture = target;
                data.requiresColorTexture = true; data.renderPostProcessing = false; target.Create(); Assert.That(target.IsCreated() && !target.sRGB, Is.True);
                nbFeature.SetActive(false);
                if (route != "Forward")
                {
                    directed = ScriptableObject.CreateInstance<G4ScreenNoiseDirectedFeature>(); directed.hideFlags = HideFlags.HideAndDontSave; directed.targetCamera = camera; directed.selectedPass = route; directed.Create(); directed.SetActive(true);
                    rendererData.rendererFeatures.Add(directed); rendererData.SetDirty();
                }
                renderer.enabled = false; Color[] background = Capture(camera, target, readback, Path.Combine(folder, "background")); renderer.enabled = true;
                renderer.sharedMaterial = graph; Capture(camera, target, readback, Path.Combine(folder, "C-warm"));
                Assert.That(graph.HasProperty("_DistortMode") && graph.HasProperty("_RefractionIOR"), Is.True);
                ConfigureRefraction(graph, true, ior, kind, normal, true);
                Color[] A = Snap(frozen, "A-frozen"), B = Snap(legacy, "B-current"), BR = Snap(legacy, "B-repeat"), C = Snap(graph, "C-graph"), CR = Snap(graph, "C-repeat");
                foreach (Material m in new[] { frozen, legacy, graph }) ConfigureRefraction(m, m == graph, ior, kind, normal, false);
                Color[] AO = Snap(frozen, "A-texture-mode"), BO = Snap(legacy, "B-texture-mode"), CO = Snap(graph, "C-texture-mode");
                foreach (Material m in new[] { frozen, legacy, graph }) { ConfigureRefraction(m, m == graph, ior, kind, normal, true); m.SetTexture("_NoiseMaskMap", white); }
                Color[] AM = Snap(frozen, "A-mask1"), BM = Snap(legacy, "B-mask1"), CM = Snap(graph, "C-mask1");
                foreach (Material m in new[] { legacy, graph }) { m.SetTexture("_NoiseMaskMap", mask); m.SetTexture("_NoiseMap", otherNoise); }
                Color[] BN = Snap(legacy, "B-other-noisemap"), CN = Snap(graph, "C-other-noisemap");
                foreach (Material m in new[] { legacy, graph }) { m.SetTexture("_NoiseMap", noise); m.SetFloat("_RefractionIOR", ior == 1.5f ? 1 : 1.5f); }
                Color[] BI = Snap(legacy, "B-other-ior"), CI = Snap(graph, "C-other-ior");
                foreach (Material m in new[] { legacy, graph })
                {
                    m.SetFloat("_RefractionIOR", ior); m.SetFloat("_noisemapEnabled", 0); if (m != graph) m.DisableKeyword("_NOISEMAP");
                }
                Color[] BNO = Snap(legacy, "B-no-noise-refraction"), CNO = Snap(graph, "C-no-noise-refraction");
                foreach (Material m in new[] { legacy, graph }) ConfigureRefraction(m, m == graph, ior, kind, normal, false);
                Color[] BNF = Snap(legacy, "B-no-noise-texture"), CNF = Snap(graph, "C-no-noise-texture");
                var metrics = new Metrics
                {
                    route=route, kind=kind, ior=ior, ortho=ortho, unityVersion=Application.unityVersion, api=SystemInfo.graphicsDeviceType.ToString(),
                    note="A=immutable Frozen; B=current ShaderLab; C=real Graph. ROI rawRGBA strict0; repeats/ignored NoiseMap/no-noise gates fullframe. Static quad/time0; no Player/VFX/Tier/allUV/curved geometry claim.",
                    maxAB=MaxRoiDelta(A,B), maxBC=MaxRoiDelta(B,C), maxOffAB=MaxRoiDelta(AO,BO), maxOffBC=MaxRoiDelta(BO,CO), maxMaskAB=MaxRoiDelta(AM,BM), maxMaskBC=MaxRoiDelta(BM,CM),
                    abDiff=CountRoiDifferences(A,B),bcDiff=CountRoiDifferences(B,C),offBCDiff=CountRoiDifferences(BO,CO),maskBCDiff=CountRoiDifferences(BM,CM),
                    repeatB=MaxFrameDelta(B,BR),repeatC=MaxFrameDelta(C,CR),ignoredNoiseB=MaxFrameDelta(B,BN),ignoredNoiseC=MaxFrameDelta(C,CN),
                    onOffB=MaxRoiDelta(B,BO),onOffC=MaxRoiDelta(C,CO),iorDeltaB=MaxRoiDelta(B,BI),iorDeltaC=MaxRoiDelta(C,CI),
                    maskDeltaB=MaxRoiDelta(B,BM),maskDeltaC=MaxRoiDelta(C,CM),noNoiseModeDeltaB=MaxFrameDelta(BNO,BNF),noNoiseModeDeltaC=MaxFrameDelta(CNO,CNF),
                    visibleB=CountRoiVisible(B,background),visibleC=CountRoiVisible(C,background),
                    finite=AllFinite(A,B,BR,C,CR,AO,BO,CO,AM,BM,CM,BN,CN,BI,CI,BNO,CNO,BNF,CNF)
                };
                File.WriteAllText(Path.Combine(folder,"metrics.json"),JsonUtility.ToJson(metrics,true));Debug.Log("NBFX_G4_RF1 "+JsonUtility.ToJson(metrics));
                var failures = new List<string>();
                Check(metrics.finite,"nonfinite"); Check(metrics.visibleB>100 && metrics.visibleC>100,"empty visible source");
                Zero(metrics.maxAB,"Frozen/current main"); Zero(metrics.maxOffAB,"Frozen/current texture branch"); Zero(metrics.maxMaskAB,"Frozen/current mask1");
                Zero(metrics.maxBC,"B/C main");Zero(metrics.maxOffBC,"B/C texture branch");Zero(metrics.maxMaskBC,"B/C mask1");Zero(MaxRoiDelta(BI,CI),"B/C IOR control");
                Zero(metrics.repeatB,"B repeat");Zero(metrics.repeatC,"C repeat");Zero(metrics.ignoredNoiseB,"B used NoiseMap in refraction branch");Zero(metrics.ignoredNoiseC,"C used NoiseMap in refraction branch");
                Zero(metrics.noNoiseModeDeltaB,"B bypassed Noise gate");Zero(metrics.noNoiseModeDeltaC,"C bypassed Noise gate");
                Check(metrics.onOffB>.005f && metrics.onOffC>.005f,"texture/refraction causality weak");
                Check(metrics.iorDeltaB>.005f && metrics.iorDeltaC>.005f,"IOR causality weak");
                // IOR=1 on an orthographic flat quad yields zero XY offset,
                // so its ordinary Forward or opaque mask controls are no-ops.
                if (route==Deferred || ior!=1 || !ortho || kind!="flat") Check(metrics.maskDeltaB>.005f && metrics.maskDeltaC>.005f,"mask causality weak");
                Assert.That(failures,Is.Empty,string.Join("; ",failures));
                void Check(bool ok,string what) { if(!ok) failures.Add(what); }
                void Zero(float value,string what) { Check(value==0,what+"="+value); }
                Color[] Snap(Material mat,string name) { renderer.sharedMaterial=mat;return Capture(camera,target,readback,Path.Combine(folder,name)); }
            }
            finally
            {
                if (directed!=null) { rendererData.rendererFeatures.Remove(directed); UnityEngine.Object.DestroyImmediate(directed); }
                nbFeature.SetActive(wasActive);rendererData.SetDirty();camera.targetTexture=null;RenderTexture.active=oldActive;target.Release();
                foreach(UnityEngine.Object o in new UnityEngine.Object[] { graph,legacy,frozen,backdrop,gradient,noise,otherNoise,mask,white,normal,target,readback }) UnityEngine.Object.DestroyImmediate(o);
                EditorSceneManager.ClosePreviewScene(scene);
                Assert.That(File.ReadAllBytes(rendererAsset),Is.EqualTo(rendererBefore),"Renderer asset changed on disk");
            }
        }
        static void ConfigureRefraction(Material m,bool graph,float ior,string kind,Texture2D normal,bool refraction)
        {
            m.SetFloat("_DistortMode",refraction?1:0);m.SetFloat("_RefractionIOR",ior);
            m.SetFloat("_BumpMapToggle",kind=="normal"?1:0);m.SetTexture("_BumpTex",normal);m.SetFloat("_BumpScale",.6f);
            bool pn=kind=="pnoise";m.SetFloat("_ProgramNoise_Toggle",pn?1:0);m.SetFloat("_ProgramNoise_Simple_Toggle",pn?1:0);m.SetFloat("_ProgramNoise_Voronoi_Toggle",0);
            m.SetFloat("_DistortPNoiseBlendOpacity",1);
            if (graph) SetWord(m,"_NB_PNoiseBlendLo16","_NB_PNoiseBlendHi16",1u<<9);
            else
            {
                m.SetInteger("_W9ParticleShaderPNoiseBlendFlag",1<<9);Toggle(m,"_DISTORT_REFRACTION",refraction);Toggle(m,"_NORMALMAP",kind=="normal");
                Toggle(m,"_PROGRAM_NOISE",pn);Toggle(m,"_PROGRAM_NOISE_SIMPLE",pn);Toggle(m,"_PROGRAM_NOISE_VORONOI",false);
            }
        }
'''
p=B/'Tests/URP/Editor/G4GraphRefractionTests.cs';p.write_text(imports+body+configure+tail);p.with_suffix('.cs.meta').write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n')
print(p,len(p.read_text().splitlines()))
