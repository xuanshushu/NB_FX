using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace NBFX.Baseline.Tests
{
    // CA1 ordinary Mesh: actual Forward and both exact NB screen passes.
    // A immutable Frozen, B current ShaderLab, C Graph; no VFX/Player claim.
    public sealed class G4GraphChromaticTests
    {
        const string GraphPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph";
        const string CurrentPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader";
        const string FrozenPath = "Packages/com.xuanxuan.nb.fx/Tests/Baseline/Frozen/NBShaders2/Shader/NBShader.shader";
        const string Deferred = "NBDeferredDistortPass", Opaque = "NBCameraOpaqueDistortPass";
        const int Size = 128, BackgroundLayer = 2, ForwardLayer = 4,
            DirectedLayer = G4GraphScreenNoiseTests.ForegroundLayer, Min = 40, Max = 88;
        static readonly string[] ForwardKinds = { "without-noise", "with-noise", "pnoise", "refraction",
            "pom", "custom1x", "custom2w", "wrap-clampU-repeatV", "lod0", "backface", "negative-scale" };
        static readonly string[] ScreenKinds = { "without-noise", "with-noise", "pnoise", "refraction", "pom-inert" };
        [Serializable] sealed class Metrics
        {
            public string route, kind, api, unityVersion, note;
            public bool ortho, finite;
            public int visibleA, visibleB, visibleC, abOnChanged, bcOnChanged,
                abOffChanged, bcOffChanged, responseAChanged, responseBChanged, responseCChanged;
            public float abOn, bcOn, abOff, bcOff, repeatA, repeatB, repeatC,
                responseA, responseB, responseC, withNoiseDeltaA, withNoiseDeltaB,
                withNoiseDeltaC, pomInertA, pomInertB, pomInertC,
                opaquePackedWrapA, opaquePackedWrapB, opaquePackedWrapC,
                opaqueClampShiftA, opaqueClampShiftB, opaqueClampShiftC,
                opaqueAltAB, opaqueAltBC, opaqueShiftAB, opaqueShiftBC;
        }
        [OneTimeSetUp]
        public void ForceGraphImportAfterReload() => AssetDatabase.ImportAsset(GraphPath,
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        static IEnumerable<TestCaseData> Cases()
        {
            foreach (string kind in ForwardKinds)
                foreach (bool ortho in new[] { true, false })
                    yield return new TestCaseData("Forward", kind, ortho).SetName(
                        "G4Chromatic_Forward_" + kind + (ortho ? "_ortho" : "_perspective"));
            foreach (string route in new[] { Deferred, Opaque })
                foreach (string kind in ScreenKinds)
                    foreach (bool ortho in new[] { true, false })
                        yield return new TestCaseData(route, kind, ortho).SetName(
                            "G4Chromatic_" + route + "_" + kind + (ortho ? "_ortho" : "_perspective"));
            // CameraOpaque's BaseMap sampler is statically clamped in the
            // ShaderLab pass, independent of all four packed material wraps.
            // Each case already captures CA on/off; two LOD states and two
            // cameras make 16 additional causal cross-boundary cases.
            foreach (int wrap in new[] { 0, 1, 2, 3 })
                foreach (bool lod0 in new[] { false, true })
                    foreach (bool ortho in new[] { true, false })
                    {
                        string kind = $"opaque-wrap-{wrap}-lod{(lod0 ? 0 : 1)}";
                        yield return new TestCaseData(Opaque, kind, ortho).SetName(
                            "G4Chromatic_" + Opaque + "_" + kind +
                            (ortho ? "_ortho" : "_perspective"));
                    }
        }
        [TestCaseSource(nameof(Cases))]
        public void ThreeChromaticSamplesMatchFrozenAndGraph(string route, string kind, bool ortho)
        {
            Assert.That(GraphicsSettings.currentRenderPipeline, Is.InstanceOf<UniversalRenderPipelineAsset>());
            Assert.That(GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>()
                .enableRenderCompatibilityMode, Is.False);
            Assert.That(SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf), Is.True);
            var rendererData = ((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline).rendererDataList[0];
            Assert.That(rendererData, Is.Not.Null);
            var urd = rendererData as UniversalRendererData;
            Assert.That(urd, Is.Not.Null);
            Assert.That(urd.transparentLayerMask.value & (1 << ForwardLayer), Is.Not.Zero);
            var nbFeature = FindNBPostProcess(rendererData);
            Assert.That(nbFeature, Is.Not.Null);
            bool nbWasActive = nbFeature.isActive;
            string rendererPath = Path.Combine(Path.GetDirectoryName(Application.dataPath),
                AssetDatabase.GetAssetPath(rendererData));
            byte[] rendererBefore = File.ReadAllBytes(rendererPath);
            string evidence = Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR");
            if (string.IsNullOrEmpty(evidence)) evidence = Path.Combine(
                Path.GetDirectoryName(Application.dataPath), "Temp/NBFXG4Chromatic");
            string folder = Path.Combine(evidence, "g4-chromatic", route + "-" + kind +
                (ortho ? "-ortho" : "-perspective"));
            Directory.CreateDirectory(folder);
            Shader fs = AssetDatabase.LoadAssetAtPath<Shader>(FrozenPath),
                bs = AssetDatabase.LoadAssetAtPath<Shader>(CurrentPath),
                gs = AssetDatabase.LoadAssetAtPath<Shader>(GraphPath);
            Assert.That(fs && bs && gs && fs.isSupported && bs.isSupported && gs.isSupported, Is.True);
            Assert.That(fs.name, Is.EqualTo("Effects/NBShader_T00_Frozen"));
            Scene scene = EditorSceneManager.NewPreviewScene();
            GameObject backdropObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            GameObject actor = new GameObject("CA1 ordinary Mesh", typeof(MeshFilter), typeof(MeshRenderer));
            GameObject cameraObject = new GameObject("CA1 camera");
            Mesh mesh = BuildMesh();
            Material frozen = new Material(fs), current = new Material(bs), graph = new Material(gs);
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            Assert.That(unlit, Is.Not.Null);
            Material backdrop = new Material(unlit);
            Texture2D baseMap = MakeBaseMap(IsOpaqueWrapCase(kind)), height = MakeHeight(), noise = MakeNoise(),
                backgroundMap = MakeBackdrop();
            var rt = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGBHalf,
                RenderTextureReadWrite.Linear);
            var readback = new Texture2D(Size, Size, TextureFormat.RGBAHalf, false, true);
            Camera camera = cameraObject.AddComponent<Camera>();
            var camData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            RenderTexture previous = RenderTexture.active;
            G4ScreenNoiseDirectedFeature directed = null;
            try
            {
                foreach (GameObject go in new[] { backdropObject, actor, cameraObject })
                    SceneManager.MoveGameObjectToScene(go, scene);
                backdropObject.layer = BackgroundLayer;
                backdropObject.transform.position = new Vector3(0, 0, 1);
                backdropObject.transform.localScale = new Vector3(6, 6, 1);
                backdrop.SetTexture("_BaseMap", backgroundMap);
                backdrop.SetColor("_BaseColor", Color.white);
                backdrop.SetFloat("_Cull", 0); backdrop.renderQueue = 2000;
                backdropObject.GetComponent<MeshRenderer>().sharedMaterial = backdrop;
                backdropObject.GetComponent<MeshRenderer>().enabled = route != Deferred;
                actor.layer = route == "Forward" ? ForwardLayer : DirectedLayer;
                actor.transform.position = new Vector3(0, 0, 2);
                actor.transform.rotation = kind == "backface" ? Quaternion.Euler(0, 205, 0) :
                    Quaternion.Euler(0, 27, 0);
                actor.transform.localScale = kind == "negative-scale" ?
                    new Vector3(-2.0f, 2.0f, 1.0f) : new Vector3(2.0f, 2.0f, 1.0f);
                actor.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = actor.GetComponent<MeshRenderer>();
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                camera.scene = scene; camera.orthographic = ortho;
                camera.orthographicSize = 1.5f; camera.fieldOfView = 53.13f;
                camera.nearClipPlane = .1f; camera.farClipPlane = 20;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.09f, .15f, .22f, .35f);
                camera.allowHDR = true; camera.allowMSAA = false;
                camera.cullingMask = (1 << BackgroundLayer) | (1 << actor.layer);
                camera.transform.position = new Vector3(0, 0, 5);
                camera.transform.rotation = Quaternion.Euler(0, 180, 0);
                camera.targetTexture = rt; camData.requiresColorTexture = true;
                camData.renderPostProcessing = false;
                rt.Create(); Assert.That(rt.IsCreated() && !rt.sRGB, Is.True);
                nbFeature.SetActive(false);
                if (route != "Forward")
                {
                    directed = ScriptableObject.CreateInstance<G4ScreenNoiseDirectedFeature>();
                    directed.hideFlags = HideFlags.HideAndDontSave;
                    directed.targetCamera = camera; directed.selectedPass = route;
                    directed.Create(); directed.SetActive(true);
                    rendererData.rendererFeatures.Add(directed); rendererData.SetDirty();
                }
                Configure(frozen, false, route, kind, baseMap, height, noise);
                Configure(current, false, route, kind, baseMap, height, noise);
                Configure(graph, true, route, kind, baseMap, height, noise);
                renderer.enabled = false;
                Capture(camera, rt, readback, Path.Combine(folder, "background"));
                renderer.enabled = true;
                renderer.sharedMaterial = graph;
                Capture(camera, rt, readback, Path.Combine(folder, "C-warm"));
                // A fresh import can expose a placeholder before this real draw.
                Configure(graph, true, route, kind, baseMap, height, noise);
                bool urpMetadata = false;
                foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(GraphPath))
                    if (asset && asset.GetType().FullName ==
                        "UnityEditor.Rendering.Universal.ShaderGraph.UniversalMetadata")
                        urpMetadata = true;
                Assert.That(urpMetadata, Is.True, "Real URP Graph import metadata missing after warm draw.");
                foreach (string property in new[] { "_Distortion_Choraticaberrat_Toggle",
                    "_NB_CustomDataFlag0Lo16", "_NB_CustomDataFlag0Hi16" })
                    Assert.That(graph.HasProperty(property), Is.True,
                        "CA1 Graph property absent after warm draw: " + property);
                Color[] Snap(Material m, bool enabled, bool withNoise, bool pom,
                    string label, int wrapOverride = -1)
                {
                    Apply(m, m == graph, kind, enabled, withNoise, pom, wrapOverride);
                    renderer.sharedMaterial = m;
                    return Capture(camera, rt, readback, Path.Combine(folder, label));
                }
                bool withNoise = kind == "with-noise" || kind == "pnoise" ||
                    kind == "refraction" || kind == "pom" || kind == "pom-inert";
                bool pom = kind == "pom" || kind == "pom-inert";
                Color[] a = Snap(frozen, true, withNoise, pom, "A-frozen-on");
                Color[] ar = Snap(frozen, true, withNoise, pom, "A-repeat");
                Color[] b = Snap(current, true, withNoise, pom, "B-current-on");
                Color[] br = Snap(current, true, withNoise, pom, "B-repeat");
                Color[] c = Snap(graph, true, withNoise, pom, "C-graph-on");
                Color[] cr = Snap(graph, true, withNoise, pom, "C-repeat");
                Color[] ao = Snap(frozen, false, withNoise, pom, "A-frozen-off");
                Color[] bo = Snap(current, false, withNoise, pom, "B-current-off");
                Color[] co = Snap(graph, false, withNoise, pom, "C-graph-off");
                Color[] aw = Snap(frozen, true, !withNoise, pom, "A-withnoise-flipped");
                Color[] bw = Snap(current, true, !withNoise, pom, "B-withnoise-flipped");
                Color[] cw = Snap(graph, true, !withNoise, pom, "C-withnoise-flipped");
                Color[] ap = null, bp = null, cp = null;
                if (kind == "pom-inert")
                {
                    ap = Snap(frozen, true, withNoise, false, "A-pom-off");
                    bp = Snap(current, true, withNoise, false, "B-pom-off");
                    cp = Snap(graph, true, withNoise, false, "C-pom-off");
                }
                Color[] altA = null, altB = null, altC = null;
                Color[] shiftedA = null, shiftedB = null, shiftedC = null;
                if (IsOpaqueWrapCase(kind))
                {
                    int selectedWrap = OpaqueWrap(kind);
                    int alternateWrap = selectedWrap == 1 ? 0 : 1;
                    altA = Snap(frozen, true, false, false, "A-other-packed-wrap", alternateWrap);
                    altB = Snap(current, true, false, false, "B-other-packed-wrap", alternateWrap);
                    altC = Snap(graph, true, false, false, "C-other-packed-wrap", alternateWrap);
                    // A +1 texture-coordinate shift changes clamped edge
                    // sampling but would be periodic for a repeating sampler.
                    // CA off isolates the single BaseMap path from CA delta.
                    foreach (Material mat in new[] { frozen, current, graph })
                        mat.SetTextureOffset("_BaseMap", new Vector2(.1f, -.75f));
                    shiftedA = Snap(frozen, false, false, false, "A-clamp-uv-plus-one");
                    shiftedB = Snap(current, false, false, false, "B-clamp-uv-plus-one");
                    shiftedC = Snap(graph, false, false, false, "C-clamp-uv-plus-one");
                    foreach (Material mat in new[] { frozen, current, graph })
                        mat.SetTextureOffset("_BaseMap", new Vector2(-.9f, -.75f));
                }
                var abOn = Compare(a, b); var bcOn = Compare(b, c);
                var abOff = Compare(ao, bo); var bcOff = Compare(bo, co);
                var ra = Compare(a, ao); var rb = Compare(b, bo); var rc = Compare(c, co);
                var mtr = new Metrics {
                    route = route, kind = kind, ortho = ortho,
                    api = SystemInfo.graphicsDeviceType.ToString(), unityVersion = Application.unityVersion,
                    note = "CA1 ordinary Mesh only. A Frozen/B current/C Graph, 128x128 linear RGBAHalf, ROI40..87. Per-channel alpha composition and pre-POM origin; screen exact tags via test-only directed feature. Opaque-wrap cases cross UV 0/1, require packed-wrap invariance and a nonzero +1U clamp control under both implicit/LOD0. No VFX/Player, UI or GUI-sync claim.",
                    visibleA = Visible(a), visibleB = Visible(b), visibleC = Visible(c),
                    abOnChanged = abOn.changed, bcOnChanged = bcOn.changed,
                    abOffChanged = abOff.changed, bcOffChanged = bcOff.changed,
                    abOn = abOn.max, bcOn = bcOn.max, abOff = abOff.max, bcOff = bcOff.max,
                    repeatA = Compare(a, ar).max, repeatB = Compare(b, br).max,
                    repeatC = Compare(c, cr).max,
                    responseA = ra.max, responseB = rb.max, responseC = rc.max,
                    responseAChanged = ra.changed, responseBChanged = rb.changed,
                    responseCChanged = rc.changed,
                    withNoiseDeltaA = Compare(a, aw).max,
                    withNoiseDeltaB = Compare(b, bw).max,
                    withNoiseDeltaC = Compare(c, cw).max,
                    pomInertA = ap == null ? -1 : Compare(a, ap).max,
                    pomInertB = bp == null ? -1 : Compare(b, bp).max,
                    pomInertC = cp == null ? -1 : Compare(c, cp).max,
                    opaquePackedWrapA = altA == null ? -1 : Compare(a, altA).max,
                    opaquePackedWrapB = altB == null ? -1 : Compare(b, altB).max,
                    opaquePackedWrapC = altC == null ? -1 : Compare(c, altC).max,
                    opaqueClampShiftA = shiftedA == null ? -1 : Compare(ao, shiftedA).max,
                    opaqueClampShiftB = shiftedB == null ? -1 : Compare(bo, shiftedB).max,
                    opaqueClampShiftC = shiftedC == null ? -1 : Compare(co, shiftedC).max,
                    opaqueAltAB = altA == null ? -1 : Compare(altA, altB).max,
                    opaqueAltBC = altB == null ? -1 : Compare(altB, altC).max,
                    opaqueShiftAB = shiftedA == null ? -1 : Compare(shiftedA, shiftedB).max,
                    opaqueShiftBC = shiftedB == null ? -1 : Compare(shiftedB, shiftedC).max,
                    finite = Finite(a, ar, b, br, c, cr, ao, bo, co, aw, bw, cw) &&
                        (ap == null || Finite(ap, bp, cp)) &&
                        (altA == null || Finite(altA, altB, altC, shiftedA, shiftedB, shiftedC))
                };
                File.WriteAllText(Path.Combine(folder, "metrics.json"), JsonUtility.ToJson(mtr, true));
                Debug.Log("NBFX_G4_CHROMATIC_ABC " + JsonUtility.ToJson(mtr));
                Assert.That(mtr.finite, Is.True);
                Assert.That(mtr.visibleA, Is.GreaterThan(100));
                Assert.That(mtr.visibleB, Is.GreaterThan(100));
                Assert.That(mtr.visibleC, Is.GreaterThan(100));
                Assert.That(mtr.repeatA + mtr.repeatB + mtr.repeatC, Is.Zero);
                Assert.That(mtr.responseA, Is.GreaterThan(.005f));
                Assert.That(mtr.responseB, Is.GreaterThan(.005f));
                Assert.That(mtr.responseC, Is.GreaterThan(.005f));
                Assert.That(mtr.responseAChanged, Is.GreaterThan(20));
                Assert.That(mtr.responseBChanged, Is.GreaterThan(20));
                Assert.That(mtr.responseCChanged, Is.GreaterThan(20));
                Assert.That(mtr.abOnChanged + mtr.abOffChanged, Is.Zero,
                    "Pure old ShaderLab CA extraction changed Frozen A/current B.");
                Assert.That(mtr.bcOnChanged + mtr.bcOffChanged, Is.Zero,
                    "Graph CA differs from current ShaderLab.");
                if (kind == "with-noise" || kind == "pnoise" || kind == "refraction" || kind == "pom")
                {
                    Assert.That(mtr.withNoiseDeltaA, Is.GreaterThan(.003f));
                    Assert.That(mtr.withNoiseDeltaB, Is.GreaterThan(.003f));
                    Assert.That(mtr.withNoiseDeltaC, Is.GreaterThan(.003f));
                }
                if (kind == "pom-inert")
                    Assert.That(mtr.pomInertA + mtr.pomInertB + mtr.pomInertC, Is.Zero,
                        "Old and Graph NB screen passes must ignore POM.");
                if (IsOpaqueWrapCase(kind))
                {
                    Assert.That(mtr.opaquePackedWrapA + mtr.opaquePackedWrapB +
                        mtr.opaquePackedWrapC, Is.Zero,
                        "CameraOpaque must ignore packed BaseMap wrap in Frozen/current/Graph.");
                    Assert.That(mtr.opaqueAltAB + mtr.opaqueAltBC +
                        mtr.opaqueShiftAB + mtr.opaqueShiftBC, Is.Zero,
                        "CameraOpaque alternate wrap and UV-shift controls must retain strict A/B/C parity.");
                    Assert.That(mtr.opaqueClampShiftA, Is.GreaterThan(.005f));
                    Assert.That(mtr.opaqueClampShiftB, Is.GreaterThan(.005f));
                    Assert.That(mtr.opaqueClampShiftC, Is.GreaterThan(.005f));
                }
            }
            finally
            {
                if (directed != null)
                { rendererData.rendererFeatures.Remove(directed); UnityEngine.Object.DestroyImmediate(directed); }
                nbFeature.SetActive(nbWasActive); rendererData.SetDirty();
                camera.targetTexture = null; RenderTexture.active = previous; rt.Release();
                foreach (UnityEngine.Object obj in new UnityEngine.Object[] { mesh, frozen, current, graph,
                    backdrop, baseMap, height, noise, backgroundMap, rt, readback })
                    UnityEngine.Object.DestroyImmediate(obj);
                EditorSceneManager.ClosePreviewScene(scene);
                Assert.That(File.ReadAllBytes(rendererPath), Is.EqualTo(rendererBefore),
                    "Renderer asset changed on disk by CA1 fixture.");
            }
        }
        static ScriptableRendererFeature FindNBPostProcess(ScriptableRendererData data)
        {
            foreach (ScriptableRendererFeature feature in data.rendererFeatures)
                if (feature != null && feature.GetType().FullName == "NBShader.NBPostProcess") return feature;
            return null;
        }
        static void Configure(Material m, bool graph, string route, string kind,
            Texture2D baseMap, Texture2D height, Texture2D noise)
        {
            m.shaderKeywords = graph ? new[] { "_SURFACE_TYPE_TRANSPARENT" } :
                new[] { "_FX_LIGHT_MODE_UNLIT" };
            m.SetTexture("_BaseMap", baseMap); m.SetTexture("_NoiseMap", noise);
            m.SetTexture("_ParallaxMapping_Map", height);
            m.SetTextureScale("_BaseMap", IsOpaqueWrapCase(kind) ?
                new Vector2(2.8f, 2.5f) : Vector2.one);
            m.SetTextureOffset("_BaseMap", IsOpaqueWrapCase(kind) ?
                new Vector2(-.9f, -.75f) : Vector2.zero);
            m.SetColor(graph ? "_Color" : "_BaseColor", Color.white);
            m.SetColor("_ColorA", Color.white); m.SetFloat("_AlphaAll", 1);
            m.SetFloat("_BaseColorIntensityForTimeline", 1);
            m.SetFloat("_Cull", kind == "backface" ? (float)CullMode.Front : (float)CullMode.Off);
            m.SetFloat("_ZTest", (float)CompareFunction.LessEqual);
            m.SetFloat("_ZWrite", 0);
            m.SetFloat("_SrcBlend", (float)BlendMode.One);
            m.SetFloat("_DstBlend", (float)BlendMode.Zero);
            m.SetFloat("_noisemapEnabled", 1);
            m.SetFloat("_noiseMaskMap_Toggle", 0);
            m.SetFloat("_TexDistortion_intensity", .6f);
            m.SetFloat("_NoiseIntensity", .7f);
            m.SetVector("_NoiseOffset", Vector4.zero);
            m.SetVector("_DistortionDirection", new Vector4(.8f, .45f, 1.2f, 0));
            m.SetFloat("_DistortMode", 0); m.SetFloat("_RefractionIOR", 1.45f);
            m.SetFloat("_DistortPNoiseBlendOpacity", 1);
            m.SetVector("_DissolveVoronoi_Vec", new Vector4(3.1f, 2.7f, 2.4f, 3.3f));
            m.SetVector("_DissolveVoronoi_Vec2", new Vector4(1, 1, 0, 0));
            m.SetVector("_DissolveVoronoi_Vec3", Vector4.zero);
            m.SetVector("_DissolveVoronoi_Vec4", new Vector4(.17f, -.23f, -.19f, .11f));
            m.SetFloat("_ProgramNoise_Rotate", 0);
            m.SetFloat("_ProgramNoiseBaseBlendOpacity", 1);
            m.SetFloat("_ParallaxMapping_Intensity", .17f);
            m.SetVector("_ParallaxMapping_Vec", new Vector4(5, 30, 0, 0));
            m.SetTextureScale("_ParallaxMapping_Map", new Vector2(1.7f, 1.4f));
            m.SetTextureOffset("_ParallaxMapping_Map", new Vector2(-.24f, .13f));
            if (graph)
            {
                m.SetFloat("_Surface", 1); m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                m.SetFloat("_DstBlendAlpha", (float)BlendMode.Zero);
                m.SetFloat("_NB_DistortionMode", route == Deferred ? 1 : route == Opaque ? 2 : 0);
                m.SetFloat("_NB_DistortionIntensity", route == Opaque ? 2 : .7f);
                m.SetFloat("_NB_ColorChannelLo16", 3);
                m.SetFloat("_NB_DistortionAlphaPow", 1);
                m.SetFloat("_NB_DistortionAlphaMultiplier", 1);
                m.SetFloat("_NB_DistortionAlphaAdd", 0);
                m.SetFloat("_ColorMask", 15);
            }
            else
            {
                m.SetFloat("_ScreenDistortIntensity", route == Opaque ? 2 : .7f);
                m.SetFloat("_ColorMask", 15); m.SetFloat("_fogintensity", 0);
                m.SetFloat("_FxLightMode", 0);
                m.SetInteger("_W9ParticleShaderColorChannelFlag", 3);
                m.SetInteger("_W9ParticleShaderFlags1", 1 << 9);
                m.SetInteger("_W9ParticleShaderPNoiseBlendFlag", 1 << 9);
            }
            m.renderQueue = 3000;
            foreach (string pass in new[] { "SRPDefaultUnlit", "SRPDEFAULTUNLIT", "UniversalForward",
                "DepthOnly", "ShadowCaster", "Universal2D", Deferred, Opaque })
                m.SetShaderPassEnabled(pass, false);
            if (route == "Forward")
            {
                m.SetShaderPassEnabled(graph || kind == "backface" ? "SRPDefaultUnlit" : "UniversalForward", true);
                if (graph) m.SetShaderPassEnabled("SRPDEFAULTUNLIT", true);
            }
            else m.SetShaderPassEnabled(route, true);
        }
        static void Apply(Material m, bool graph, string kind, bool enabled,
            bool withNoise, bool pom, int wrapOverride = -1)
        {
            bool pn = kind == "pnoise", rf = kind == "refraction";
            bool noise = kind == "with-noise" || pn || rf || kind == "pom" || kind == "pom-inert";
            m.SetFloat("_Distortion_Choraticaberrat_Toggle", enabled ? 1 : 0);
            if (!graph) m.SetFloat("_Distortion_Choraticaberrat_WithNoise_Toggle", withNoise ? 1 : 0);
            m.SetFloat("_noisemapEnabled", noise ? 1 : 0);
            m.SetFloat("_ProgramNoise_Toggle", pn ? 1 : 0);
            m.SetFloat("_ProgramNoise_Simple_Toggle", pn ? 1 : 0);
            m.SetFloat("_ProgramNoise_Voronoi_Toggle", 0);
            m.SetFloat("_DistortMode", rf ? 1 : 0);
            m.SetFloat("_ParallaxMapping_Toggle", pom ? 1 : 0);
            int customNibble = kind == "custom1x" ? 15 : kind == "custom2w" ? 8 : 0;
            uint custom = (uint)customNibble << 28;
            uint flags = withNoise ? 1u << 1 : 0u;
            uint wrapMode = kind == "wrap-clampU-repeatV" ? 3u :
                IsOpaqueWrapCase(kind) ? (uint)(wrapOverride >= 0 ? wrapOverride : OpaqueWrap(kind)) : 0u;
            uint wrap = (wrapMode & 1u) | ((wrapMode & 2u) != 0u ? 1u << 16 : 0u);
            uint noMip = kind == "lod0" || (IsOpaqueWrapCase(kind) && kind.EndsWith("lod0")) ? 1u : 0u;
            if (graph)
            {
                SetWord(m, "_NB_Flags0Lo16", "_NB_Flags0Hi16", flags);
                SetWord(m, "_NB_CustomDataFlag0Lo16", "_NB_CustomDataFlag0Hi16", custom);
                SetWord(m, "_NB_WrapFlagsLo16", "_NB_WrapFlagsHi16", wrap);
                SetWord(m, "_NB_ForceNoMipFlagsLo16", "_NB_ForceNoMipFlagsHi16", noMip);
                SetWord(m, "_NB_PNoiseBlendLo16", "_NB_PNoiseBlendHi16", 1u << 9);
            }
            else
            {
                m.SetInteger("_W9ParticleShaderFlags", unchecked((int)flags));
                m.SetInteger("_W9ParticleCustomDataFlag0", unchecked((int)custom));
                m.SetInteger("_W9ParticleShaderWrapFlags", unchecked((int)wrap));
                m.SetInteger("_NBShaderForceNoMipFlags", unchecked((int)noMip));
                Toggle(m, "_CHROMATIC_ABERRATION", enabled);
                Toggle(m, "_NOISEMAP", noise);
                Toggle(m, "_PROGRAM_NOISE", pn);
                Toggle(m, "_PROGRAM_NOISE_SIMPLE", pn);
                Toggle(m, "_PROGRAM_NOISE_VORONOI", false);
                Toggle(m, "_DISTORT_REFRACTION", rf);
                Toggle(m, "_PARALLAX_MAPPING", pom);
            }
        }
        static void Toggle(Material m, string keyword, bool enabled)
        { if (enabled) m.EnableKeyword(keyword); else m.DisableKeyword(keyword); }
        static void SetWord(Material m, string lo, string hi, uint value)
        { m.SetFloat(lo, value & 65535u); m.SetFloat(hi, value >> 16); }
        static Mesh BuildMesh()
        {
            var mesh = new Mesh { name = "CA1 UV0/Custom1/Custom2 tangent Mesh" };
            mesh.vertices = new[] { new Vector3(-1, -1, 0), new Vector3(1, -1, 0),
                new Vector3(-1, 1, 0), new Vector3(1, 1, 0) };
            mesh.triangles = new[] { 0, 1, 2, 1, 3, 2 };
            mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(0, 1), new Vector2(1, 1) };
            List<Vector4> c1 = new List<Vector4>(), c2 = new List<Vector4>();
            for (int i = 0; i < 4; i++)
            {
                c1.Add(new Vector4(.15f + .15f * i, .25f, .35f, .45f));
                c2.Add(new Vector4(.1f, .2f, .3f, .65f + .1f * i));
            }
            mesh.SetUVs(1, c1); mesh.SetUVs(2, c2);
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            return mesh;
        }
        static bool IsOpaqueWrapCase(string kind) => kind.StartsWith("opaque-wrap-", StringComparison.Ordinal);
        static int OpaqueWrap(string kind)
        {
            Assert.That(IsOpaqueWrapCase(kind), Is.True);
            return kind["opaque-wrap-".Length] - '0';
        }
        static Texture2D MakeBaseMap(bool edgeMap)
        {
            const int n = 128; var tex = new Texture2D(n, n, TextureFormat.RGBAHalf, true, true);
            var pixels = new Color[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float u = x / (n - 1f), v = y / (n - 1f);
                pixels[y * n + x] = edgeMap ? new Color(
                    .08f + .84f * u,
                    (x & 1) == 0 ? .12f : .86f,
                    .08f + .75f * v,
                    .2f + .7f * (.6f * u + .4f * v)) :
                    new Color(.12f + .8f * u, .08f + .75f * v,
                        .15f + .7f * (.5f + .5f * Mathf.Sin(11f * u + 3f * v)),
                        .18f + .72f * (.6f * u + .4f * v));
            }
            tex.SetPixels(pixels); tex.Apply(true); tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Repeat; return tex;
        }
        static Texture2D MakeHeight()
        {
            const int n = 64; var tex = new Texture2D(n, n, TextureFormat.RGBAHalf, false, true);
            var pixels = new Color[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            { float h = .2f + .7f * (.5f + .5f * Mathf.Sin(x * .17f + y * .11f));
              pixels[y * n + x] = new Color(h, h, h, 1); }
            tex.SetPixels(pixels); tex.Apply(false); tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Repeat; return tex;
        }
        static Texture2D MakeNoise()
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBAHalf, false, true);
            var pixels = new Color[16];
            for (int y = 0; y < 4; y++) for (int x = 0; x < 4; x++)
                pixels[y * 4 + x] = new Color(.2f + .16f * x, .25f + .13f * y, 0, .8f);
            tex.SetPixels(pixels); tex.Apply(false); tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Repeat; return tex;
        }
        static Texture2D MakeBackdrop()
        {
            const int n = 64; var tex = new Texture2D(n, n, TextureFormat.RGBAHalf, false, true);
            var pixels = new Color[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                pixels[y * n + x] = new Color(.2f + .7f * x / (n - 1f),
                    .13f + .67f * y / (n - 1f), .25f + .35f * (x + y) / (2f * n - 2f), 1);
            tex.SetPixels(pixels); tex.Apply(false); tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp; return tex;
        }
        static Color[] Capture(Camera camera, RenderTexture rt, Texture2D readback, string path)
        {
            for (int i = 0; i < 4; i++) camera.Render();
            RenderTexture prev = RenderTexture.active;
            try
            {
                RenderTexture.active = rt;
                readback.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
                readback.Apply(false); Color[] pixels = readback.GetPixels();
                using (var file = File.Create(path + ".rgba-f32.gz"))
                using (var zip = new GZipStream(file, CompressionMode.Compress))
                using (var writer = new BinaryWriter(zip))
                    foreach (Color c in pixels)
                    { writer.Write(c.r); writer.Write(c.g); writer.Write(c.b); writer.Write(c.a); }
                var png = new Texture2D(Size, Size, TextureFormat.RGBA32, false, true);
                try { png.SetPixels(pixels); png.Apply(false); File.WriteAllBytes(path + ".png", png.EncodeToPNG()); }
                finally { UnityEngine.Object.DestroyImmediate(png); }
                return pixels;
            }
            finally { RenderTexture.active = prev; }
        }
        struct Difference { public float max; public int changed; }
        static Difference Compare(Color[] a, Color[] b)
        {
            var d = new Difference();
            for (int y = Min; y < Max; y++) for (int x = Min; x < Max; x++)
            {
                int i = y * Size + x; float delta = 0;
                for (int c = 0; c < 4; c++) delta = Mathf.Max(delta, Mathf.Abs(a[i][c] - b[i][c]));
                d.max = Mathf.Max(d.max, delta); if (delta > 0) d.changed++;
            }
            return d;
        }
        static int Visible(Color[] pixels)
        {
            int count = 0;
            for (int y = Min; y < Max; y++) for (int x = Min; x < Max; x++)
            {
                Color c = pixels[y * Size + x];
                if (Mathf.Max(c.r, Mathf.Max(c.g, c.b)) > .4f) count++;
            }
            return count;
        }
        static bool Finite(params Color[][] frames)
        {
            foreach (Color[] frame in frames)
                foreach (Color c in frame)
                    for (int i = 0; i < 4; i++)
                        if (float.IsNaN(c[i]) || float.IsInfinity(c[i])) return false;
            return true;
        }
    }
}
