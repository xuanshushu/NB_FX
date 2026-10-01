using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace NBFX.Baseline.Tests
{
    /// <summary>
    /// FG1 ordinary Mesh. A=immutable Frozen, B=current ShaderLab, C=Graph.
    /// Tests the old pre-VertexOffset vertex fog factor, pixel order and
    /// bit10 gamma; no Player, VFX or alternate project ColorSpace claim.
    /// </summary>
    public sealed class G4GraphFogGammaTests
    {
        const string GraphPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph";
        const string CurrentPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader";
        const string FrozenPath = "Packages/com.xuanxuan.nb.fx/Tests/Baseline/Frozen/NBShaders2/Shader/NBShader.shader";
        const string Deferred = "NBDeferredDistortPass", Opaque = "NBCameraOpaqueDistortPass";
        const int Size = 128, Min = 40, Max = 88, BackLayer = 2,
            ForwardLayer = 4, DirectedLayer = G4GraphScreenNoiseTests.ForegroundLayer;

        [Serializable] sealed class Metrics
        {
            public string route, kind, fogMode, api, unityVersion, note;
            public bool orthographic, finite;
            public int visibleA, visibleB, visibleC, abOnChanged, bcOnChanged,
                abOffChanged, bcOffChanged, intensityChangedA, intensityChangedB,
                intensityChangedC, fogResponseAChanged, fogResponseBChanged,
                fogResponseCChanged, actorAChanged, actorBChanged, actorCChanged;
            public float abOn, bcOn, abOff, bcOff, repeatA, repeatB, repeatC,
                intensityResponseA, intensityResponseB, intensityResponseC,
                fogResponseA, fogResponseB, fogResponseC,
                actorFogResponseA, actorFogResponseB, actorFogResponseC,
                offsetResponseA, offsetResponseB, offsetResponseC;
        }

        [OneTimeSetUp]
        public void ForceGraphImportAfterReload() => AssetDatabase.ImportAsset(GraphPath,
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

        static IEnumerable<TestCaseData> Cases()
        {
            foreach (FogMode mode in new[] { FogMode.Linear, FogMode.Exponential, FogMode.ExponentialSquared })
                foreach (bool ortho in new[] { true, false })
                    yield return new TestCaseData("Forward", "fog", mode, ortho).SetName(
                        "G4FogGamma_Forward_" + mode + (ortho ? "_ortho" : "_perspective"));
            foreach (string route in new[] { Deferred, Opaque })
                foreach (bool ortho in new[] { true, false })
                    yield return new TestCaseData(route, "fog", FogMode.Linear, ortho).SetName(
                        "G4FogGamma_" + route + (ortho ? "_ortho" : "_perspective"));
            foreach (bool ortho in new[] { true, false })
            {
                yield return new TestCaseData("Forward", "gamma", FogMode.Linear, ortho).SetName(
                    "G4FogGamma_Gamma_" + (ortho ? "ortho" : "perspective"));
                yield return new TestCaseData("Forward", "vertex-offset", FogMode.Linear, ortho).SetName(
                    "G4FogGamma_PreOffset_" + (ortho ? "ortho" : "perspective"));
                yield return new TestCaseData("Forward", "late-adjustment", FogMode.Linear, ortho).SetName(
                    "G4FogGamma_LateAdjustment_" + (ortho ? "ortho" : "perspective"));
            }
        }

        [TestCaseSource(nameof(Cases))]
        public void OrdinaryMeshFogGammaMatchesFrozen(string route, string kind,
            FogMode fogMode, bool ortho)
        {
            Assert.That(GraphicsSettings.currentRenderPipeline, Is.InstanceOf<UniversalRenderPipelineAsset>());
            Assert.That(GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>()
                .enableRenderCompatibilityMode, Is.False);
            Assert.That(SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf), Is.True);
            var data = ((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline).rendererDataList[0];
            var urd = data as UniversalRendererData;
            Assert.That(urd, Is.Not.Null);
            Assert.That(urd.transparentLayerMask.value & (1 << ForwardLayer), Is.Not.Zero);
            var nb = FindNBPostProcess(data);
            Assert.That(nb, Is.Not.Null);
            bool oldFeatureActive = nb.isActive;
            string rendererPath = Path.Combine(Path.GetDirectoryName(Application.dataPath),
                AssetDatabase.GetAssetPath(data));
            byte[] rendererBefore = File.ReadAllBytes(rendererPath);
            string root = Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR");
            if (string.IsNullOrEmpty(root)) root = Path.Combine(
                Path.GetDirectoryName(Application.dataPath), "Temp/NBFXG4FogGamma");
            string folder = Path.Combine(root, "g4-fog-gamma", route + "-" + kind + "-" + fogMode +
                (ortho ? "-ortho" : "-perspective"));
            Directory.CreateDirectory(folder);

            Shader frozenShader = Load(FrozenPath), currentShader = Load(CurrentPath), graphShader = Load(GraphPath);
            Assert.That(frozenShader.name, Is.EqualTo("Effects/NBShader_T00_Frozen"));
            var activeScene = SceneManager.GetActiveScene();
            Assert.That(activeScene.IsValid() && activeScene.isLoaded, Is.True);
            // The test-runner scene is in memory. Never save or change the active
            // scene; restore every RenderSettings field in finally.
            bool oldFog = RenderSettings.fog;
            FogMode oldMode = RenderSettings.fogMode;
            Color oldColor = RenderSettings.fogColor;
            float oldDensity = RenderSettings.fogDensity;
            float oldStart = RenderSettings.fogStartDistance;
            float oldEnd = RenderSettings.fogEndDistance;
            RenderTexture oldRT = RenderTexture.active;
            var backgroundObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var actorObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var cameraObject = new GameObject("FG1 camera");
            var a = new Material(frozenShader); var b = new Material(currentShader);
            var c = new Material(graphShader);
            var backdrop = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            var baseMap = Solid(new Color(.73f, .19f, .08f, .83f));
            var noise = Solid(new Color(.64f, .32f, 0, 1));
            var vertexMap = Solid(new Color(.75f, .75f, .75f, 1));
            var rt = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGBHalf,
                RenderTextureReadWrite.Linear);
            var read = new Texture2D(Size, Size, TextureFormat.RGBAHalf, false, true);
            Camera camera = cameraObject.AddComponent<Camera>();
            var cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            G4ScreenNoiseDirectedFeature directed = null;
            try
            {
                foreach (var go in new[] { backgroundObject, actorObject, cameraObject })
                    SceneManager.MoveGameObjectToScene(go, activeScene);
                backgroundObject.layer = BackLayer;
                backgroundObject.transform.position = new Vector3(0, 0, 1);
                backgroundObject.transform.localScale = new Vector3(6, 6, 1);
                backdrop.SetColor("_BaseColor", new Color(.15f, .28f, .37f, 1));
                backdrop.SetFloat("_Cull", 0); backdrop.renderQueue = 2000;
                backgroundObject.GetComponent<MeshRenderer>().sharedMaterial = backdrop;
                actorObject.layer = route == "Forward" ? ForwardLayer : DirectedLayer;
                actorObject.transform.position = new Vector3(0, 0, 2);
                actorObject.transform.localScale = new Vector3(2, 2, 1);
                actorObject.transform.rotation = Quaternion.Euler(0, 23, 0);
                var mesh = actorObject.GetComponent<MeshRenderer>();
                mesh.shadowCastingMode = ShadowCastingMode.Off;
                mesh.receiveShadows = false;
                camera.scene = activeScene; camera.orthographic = ortho;
                camera.orthographicSize = 1.5f; camera.fieldOfView = 53.13f;
                camera.nearClipPlane = .1f; camera.farClipPlane = 20;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.07f, .11f, .17f, 1);
                camera.allowHDR = true; camera.allowMSAA = false;
                camera.cullingMask = (1 << BackLayer) | (1 << actorObject.layer);
                camera.transform.position = new Vector3(0, 0, 5);
                camera.transform.rotation = Quaternion.Euler(0, 180, 0);
                camera.targetTexture = rt; cameraData.requiresColorTexture = true;
                cameraData.renderPostProcessing = false;
                rt.Create(); Assert.That(rt.IsCreated() && !rt.sRGB, Is.True);
                nb.SetActive(false);
                if (route != "Forward")
                {
                    directed = ScriptableObject.CreateInstance<G4ScreenNoiseDirectedFeature>();
                    directed.hideFlags = HideFlags.HideAndDontSave;
                    directed.targetCamera = camera; directed.selectedPass = route;
                    directed.Create(); directed.SetActive(true);
                    data.rendererFeatures.Add(directed); data.SetDirty();
                }
                Configure(a, false, route, baseMap, noise, vertexMap);
                Configure(b, false, route, baseMap, noise, vertexMap);
                Configure(c, true, route, baseMap, noise, vertexMap);
                RenderSettings.fog = true; RenderSettings.fogMode = fogMode;
                RenderSettings.fogColor = new Color(.02f, .05f, .85f, 1);
                RenderSettings.fogStartDistance = .4f; RenderSettings.fogEndDistance = 4.2f;
                RenderSettings.fogDensity = .42f;
                mesh.enabled = false;
                Color[] background = Capture(camera, rt, read, Path.Combine(folder, "background"));
                mesh.enabled = true; mesh.sharedMaterial = c;
                Capture(camera, rt, read, Path.Combine(folder, "C-warm"));
                // A forced import can initially expose a placeholder shader.
                Configure(c, true, route, baseMap, noise, vertexMap);
                Assert.That(c.HasProperty("_fogintensity"), Is.True,
                    "FG1 property absent after a real Graph draw/import.");
                bool metadata = false;
                foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(GraphPath))
                    if (asset && asset.GetType().FullName ==
                        "UnityEditor.Rendering.Universal.ShaderGraph.UniversalMetadata")
                        metadata = true;
                Assert.That(metadata, Is.True, "Real URP Graph import metadata missing after warm draw.");
                Color[] Snap(Material m, bool fogOn, float intensity, bool gamma,
                    bool vertexOffset, bool lateAdjust, string label)
                {
                    RenderSettings.fog = fogOn;
                    Apply(m, m == c, intensity, gamma, vertexOffset, lateAdjust);
                    mesh.sharedMaterial = m;
                    return Capture(camera, rt, read, Path.Combine(folder, label));
                }
                bool gammaOn = kind == "gamma";
                bool offsetOn = kind == "vertex-offset";
                bool lateOn = kind == "late-adjustment";
                Color[] ao = Snap(a, true, 1, gammaOn, offsetOn, lateOn, "A-on");
                Color[] ar = Snap(a, true, 1, gammaOn, offsetOn, lateOn, "A-repeat");
                Color[] bo = Snap(b, true, 1, gammaOn, offsetOn, lateOn, "B-on");
                Color[] br = Snap(b, true, 1, gammaOn, offsetOn, lateOn, "B-repeat");
                Color[] co = Snap(c, true, 1, gammaOn, offsetOn, lateOn, "C-on");
                Color[] cr = Snap(c, true, 1, gammaOn, offsetOn, lateOn, "C-repeat");
                mesh.enabled = false;
                RenderSettings.fog = false;
                Color[] backgroundOff = Capture(camera, rt, read,
                    Path.Combine(folder, "background-fog-off"));
                mesh.enabled = true;
                Color[] af = Snap(a, false, 1, gammaOn, offsetOn, lateOn, "A-fog-off");
                Color[] bf = Snap(b, false, 1, gammaOn, offsetOn, lateOn, "B-fog-off");
                Color[] cf = Snap(c, false, 1, gammaOn, offsetOn, lateOn, "C-fog-off");
                Color[] ai = Snap(a, true, .4f, gammaOn, offsetOn, lateOn, "A-intensity-04");
                Color[] bi = Snap(b, true, .4f, gammaOn, offsetOn, lateOn, "B-intensity-04");
                Color[] ci = Snap(c, true, .4f, gammaOn, offsetOn, lateOn, "C-intensity-04");
                Color[] az = Snap(a, true, 0, gammaOn, offsetOn, lateOn, "A-intensity-0");
                Color[] bz = Snap(b, true, 0, gammaOn, offsetOn, lateOn, "B-intensity-0");
                Color[] cz = Snap(c, true, 0, gammaOn, offsetOn, lateOn, "C-intensity-0");
                Color[] ax = null, bx = null, cx = null;
                if (gammaOn || offsetOn || lateOn)
                {
                    ax = Snap(a, true, 1, false, false, false, "A-switch-off");
                    bx = Snap(b, true, 1, false, false, false, "B-switch-off");
                    cx = Snap(c, true, 1, false, false, false, "C-switch-off");
                }
                var mtr = new Metrics {
                    route = route, kind = kind, fogMode = fogMode.ToString(), orthographic = ortho,
                    api = SystemInfo.graphicsDeviceType.ToString(), unityVersion = Application.unityVersion,
                    note = "FG1 ordinary Mesh only. Frozen A/current ShaderLab B/Graph C. Fog factor pre-VertexOffset; Gamma bit10 only current project color space. Exact NB screen tags use test-only directed feature. No Player/VFX claim.",
                    visibleA = Visible(ao), visibleB = Visible(bo), visibleC = Visible(co),
                    actorAChanged = Compare(background, ao).changed,
                    actorBChanged = Compare(background, bo).changed,
                    actorCChanged = Compare(background, co).changed,
                    abOn = Compare(ao, bo).max, bcOn = Compare(bo, co).max,
                    abOff = Compare(af, bf).max, bcOff = Compare(bf, cf).max,
                    abOnChanged = Compare(ao, bo).changed, bcOnChanged = Compare(bo, co).changed,
                    abOffChanged = Compare(af, bf).changed, bcOffChanged = Compare(bf, cf).changed,
                    repeatA = Compare(ao, ar).max, repeatB = Compare(bo, br).max,
                    repeatC = Compare(co, cr).max,
                    intensityResponseA = Compare(ao, ai).max,
                    intensityResponseB = Compare(bo, bi).max,
                    intensityResponseC = Compare(co, ci).max,
                    intensityChangedA = Compare(ao, ai).changed,
                    intensityChangedB = Compare(bo, bi).changed,
                    intensityChangedC = Compare(co, ci).changed,
                    fogResponseA = Compare(ao, af).max,
                    fogResponseB = Compare(bo, bf).max,
                    fogResponseC = Compare(co, cf).max,
                    actorFogResponseA = ActorFogResponse(ao, af, background, backgroundOff),
                    actorFogResponseB = ActorFogResponse(bo, bf, background, backgroundOff),
                    actorFogResponseC = ActorFogResponse(co, cf, background, backgroundOff),
                    fogResponseAChanged = Compare(ao, af).changed,
                    fogResponseBChanged = Compare(bo, bf).changed,
                    fogResponseCChanged = Compare(co, cf).changed,
                    offsetResponseA = ax == null ? -1 : Compare(ao, ax).max,
                    offsetResponseB = bx == null ? -1 : Compare(bo, bx).max,
                    offsetResponseC = cx == null ? -1 : Compare(co, cx).max,
                    finite = Finite(background, backgroundOff, ao, ar, bo, br, co, cr, af, bf, cf, ai, bi, ci,
                        az, bz, cz) && (ax == null || Finite(ax, bx, cx))
                };
                File.WriteAllText(Path.Combine(folder, "metrics.json"), JsonUtility.ToJson(mtr, true));
                Debug.Log("NBFX_G4_FOG_GAMMA_ABC " + JsonUtility.ToJson(mtr));
                Assert.That(mtr.finite, Is.True);
                Assert.That(mtr.visibleA, Is.GreaterThan(100));
                Assert.That(mtr.visibleB, Is.GreaterThan(100));
                Assert.That(mtr.visibleC, Is.GreaterThan(100));
                Assert.That(mtr.actorAChanged, Is.GreaterThan(20));
                Assert.That(mtr.actorBChanged, Is.GreaterThan(20));
                Assert.That(mtr.actorCChanged, Is.GreaterThan(20));
                Assert.That(mtr.repeatA + mtr.repeatB + mtr.repeatC, Is.Zero);
                Assert.That(mtr.fogResponseAChanged, Is.GreaterThan(20));
                Assert.That(mtr.fogResponseBChanged, Is.GreaterThan(20));
                Assert.That(mtr.fogResponseCChanged, Is.GreaterThan(20));
                Assert.That(mtr.fogResponseA, Is.GreaterThan(.005f));
                Assert.That(mtr.fogResponseB, Is.GreaterThan(.005f));
                Assert.That(mtr.fogResponseC, Is.GreaterThan(.005f));
                Assert.That(mtr.actorFogResponseA, Is.GreaterThan(.005f));
                Assert.That(mtr.actorFogResponseB, Is.GreaterThan(.005f));
                Assert.That(mtr.actorFogResponseC, Is.GreaterThan(.005f));
                Assert.That(mtr.intensityChangedA, Is.GreaterThan(20));
                Assert.That(mtr.intensityChangedB, Is.GreaterThan(20));
                Assert.That(mtr.intensityChangedC, Is.GreaterThan(20));
                Assert.That(mtr.abOnChanged + mtr.abOffChanged, Is.Zero,
                    "Frozen A/current ShaderLab B changed; do not blame Graph.");
                Assert.That(mtr.bcOnChanged + mtr.bcOffChanged, Is.Zero,
                    "Graph differs from current ShaderLab in fog/Gamma stage.");
                Assert.That(Compare(ai, bi).changed + Compare(bi, ci).changed, Is.Zero,
                    "0.4 intensity must match Frozen/current/Graph strictly.");
                Assert.That(Compare(az, bz).changed + Compare(bz, cz).changed, Is.Zero,
                    "Intensity zero must match Frozen/current/Graph strictly.");
                if (ax != null)
                {
                    Assert.That(mtr.offsetResponseA, Is.GreaterThan(.005f));
                    Assert.That(mtr.offsetResponseB, Is.GreaterThan(.005f));
                    Assert.That(mtr.offsetResponseC, Is.GreaterThan(.005f));
                    Assert.That(Compare(ax, bx).changed + Compare(bx, cx).changed, Is.Zero);
                }
            }
            finally
            {
                RenderSettings.fog = oldFog; RenderSettings.fogMode = oldMode;
                RenderSettings.fogColor = oldColor; RenderSettings.fogDensity = oldDensity;
                RenderSettings.fogStartDistance = oldStart; RenderSettings.fogEndDistance = oldEnd;
                if (directed != null)
                { data.rendererFeatures.Remove(directed); UnityEngine.Object.DestroyImmediate(directed); }
                nb.SetActive(oldFeatureActive); data.SetDirty();
                camera.targetTexture = null; RenderTexture.active = oldRT; rt.Release();
                foreach (UnityEngine.Object obj in new UnityEngine.Object[] { backgroundObject,
                    actorObject, cameraObject, a, b, c, backdrop, baseMap, noise,
                    vertexMap, rt, read })
                    UnityEngine.Object.DestroyImmediate(obj);
                Assert.That(File.ReadAllBytes(rendererPath), Is.EqualTo(rendererBefore),
                    "FG1 fixture wrote the renderer asset on disk.");
            }
        }

        static Shader Load(string path)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            Assert.That(shader && shader.isSupported, Is.True, path);
            return shader;
        }

        static ScriptableRendererFeature FindNBPostProcess(ScriptableRendererData data)
        {
            foreach (var f in data.rendererFeatures)
                if (f != null && f.GetType().FullName == "NBShader.NBPostProcess") return f;
            return null;
        }

        static Texture2D Solid(Color color)
        {
            var t = new Texture2D(1, 1, TextureFormat.RGBAHalf, false, true);
            t.SetPixel(0, 0, color); t.Apply(false);
            t.filterMode = FilterMode.Bilinear; t.wrapMode = TextureWrapMode.Repeat;
            return t;
        }

        static void Configure(Material m, bool graph, string route,
            Texture2D baseMap, Texture2D noise, Texture2D vertexMap)
        {
            m.shaderKeywords = graph ? new[] { "_SURFACE_TYPE_TRANSPARENT" } :
                new[] { "_FX_LIGHT_MODE_UNLIT" };
            m.SetTexture("_BaseMap", baseMap); m.SetTexture("_NoiseMap", noise);
            m.SetTexture("_VertexOffset_Map", vertexMap);
            m.SetColor(graph ? "_Color" : "_BaseColor", Color.white);
            m.SetColor("_ColorA", Color.white); m.SetFloat("_AlphaAll", 1);
            m.SetFloat("_BaseColorIntensityForTimeline", 1);
            m.SetFloat("_Cull", (float)CullMode.Off);
            m.SetFloat("_ZTest", (float)CompareFunction.LessEqual);
            m.SetFloat("_ZWrite", 0);
            m.SetFloat("_SrcBlend", (float)BlendMode.One);
            m.SetFloat("_DstBlend", (float)BlendMode.Zero);
            m.SetFloat("_ColorMask", 15);
            m.SetFloat("_noisemapEnabled", route == "Forward" ? 0 : 1);
            m.SetFloat("_NoiseIntensity", .75f);
            m.SetVector("_NoiseOffset", Vector4.zero);
            m.SetVector("_DistortionDirection", new Vector4(.8f, .55f, 1, 0));
            m.SetFloat("_TexDistortion_intensity", .5f);
            m.SetFloat("_VertexOffset_Toggle", 0);
            m.SetVector("_VertexOffset_Vec", new Vector4(0, 0, .55f, 0));
            m.SetVector("_VertexOffset_CustomDir", new Vector4(0, 0, 1, 0));
            m.SetFloat("_VertexOffset_NormalDir_Toggle", 1);
            m.SetFloat("_VertexOffset_DirectionSpace", 0);
            if (graph)
            {
                m.SetFloat("_Surface", 1);
                m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                m.SetFloat("_DstBlendAlpha", (float)BlendMode.Zero);
                m.SetFloat("_NB_DistortionMode", route == Deferred ? 1 : route == Opaque ? 2 : 0);
                m.SetFloat("_NB_DistortionIntensity", route == Opaque ? 2 : .75f);
                m.SetFloat("_NB_ColorChannelLo16", 3);
                m.SetFloat("_NB_DistortionAlphaPow", 1);
                m.SetFloat("_NB_DistortionAlphaMultiplier", 1);
                m.SetFloat("_NB_DistortionAlphaAdd", 0);
            }
            else
            {
                m.SetFloat("_FxLightMode", 0);
                m.SetFloat("_ScreenDistortIntensity", route == Opaque ? 2 : .75f);
                m.SetInteger("_W9ParticleShaderColorChannelFlag", 3);
                m.SetInteger("_W9ParticleShaderFlags1", 1 << 9);
            }
            m.renderQueue = 3000;
            foreach (string pass in new[] { "SRPDefaultUnlit", "SRPDEFAULTUNLIT",
                "UniversalForward", "DepthOnly", "ShadowCaster", "Universal2D",
                Deferred, Opaque }) m.SetShaderPassEnabled(pass, false);
            if (route == "Forward")
            {
                m.SetShaderPassEnabled(graph ? "SRPDefaultUnlit" : "UniversalForward", true);
                if (graph) m.SetShaderPassEnabled("SRPDEFAULTUNLIT", true);
            }
            else m.SetShaderPassEnabled(route, true);
        }

        static void Apply(Material m, bool graph, float intensity, bool gamma,
            bool vertexOffset, bool lateAdjust)
        {
            m.SetFloat("_fogintensity", intensity);
            m.SetFloat("_VertexOffset_Toggle", vertexOffset ? 1 : 0);
            uint f0 = (gamma ? 1u << 10 : 0u) |
                (lateAdjust ? (1u << 19) | 1u : 0u);
            uint f1 = (1u << 9) | (lateAdjust ? (1u << 24) | (1u << 27) : 0u);
            if (lateAdjust)
            {
                m.SetFloat("_HueShift", .25f); m.SetFloat("_Contrast", .5f);
                m.SetFloat("_Saturability", .5f);
                m.SetColor("_ContrastMidColor", Color.white);
                m.SetVector("_BaseMapColorRefine", new Vector4(1, 1, 1, 0));
            }
            if (graph)
            {
                SetWord(m, "_NB_Flags0Lo16", "_NB_Flags0Hi16", f0);
                SetWord(m, "_NB_Flags1Lo16", "_NB_Flags1Hi16", f1);
            }
            else
            {
                m.SetInteger("_W9ParticleShaderFlags", unchecked((int)f0));
                m.SetInteger("_W9ParticleShaderFlags1", unchecked((int)f1));
                Toggle(m, "_VERTEX_OFFSET", vertexOffset);
                Toggle(m, "_NOISEMAP", m.GetFloat("_noisemapEnabled") > .5f);
            }
        }

        static void Toggle(Material m, string keyword, bool enabled)
        { if (enabled) m.EnableKeyword(keyword); else m.DisableKeyword(keyword); }
        static void SetWord(Material m, string lo, string hi, uint v)
        { m.SetFloat(lo, v & 65535u); m.SetFloat(hi, v >> 16); }

        static Color[] Capture(Camera camera, RenderTexture rt, Texture2D read, string path)
        {
            for (int n = 0; n < 4; n++) camera.Render();
            RenderTexture old = RenderTexture.active;
            try
            {
                RenderTexture.active = rt; read.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
                read.Apply(false); Color[] pixels = read.GetPixels();
                using (var file = File.Create(path + ".rgba-f32.gz"))
                using (var zip = new GZipStream(file, CompressionMode.Compress))
                using (var writer = new BinaryWriter(zip))
                    foreach (Color c in pixels)
                    { writer.Write(c.r); writer.Write(c.g); writer.Write(c.b); writer.Write(c.a); }
                return pixels;
            }
            finally { RenderTexture.active = old; }
        }
        struct Diff { public float max; public int changed; }
        static Diff Compare(Color[] x, Color[] y)
        {
            var d = new Diff();
            for (int j = Min; j < Max; j++) for (int i = Min; i < Max; i++)
            {
                int pixel = j * Size + i; float delta = 0;
                for (int channel = 0; channel < 4; channel++)
                    delta = Mathf.Max(delta, Mathf.Abs(x[pixel][channel] - y[pixel][channel]));
                d.max = Mathf.Max(d.max, delta); if (delta > 0) d.changed++;
            }
            return d;
        }
        static int Visible(Color[] pixels)
        {
            int count = 0;
            for (int j = Min; j < Max; j++) for (int i = Min; i < Max; i++)
            {
                Color c = pixels[j * Size + i];
                if (Mathf.Max(c.r, Mathf.Max(c.g, c.b)) > .4f) count++;
            }
            return count;
        }
        static float ActorFogResponse(Color[] actorFogOn, Color[] actorFogOff,
            Color[] backgroundFogOn, Color[] backgroundFogOff)
        {
            float max = 0;
            for (int y = Min; y < Max; y++) for (int x = Min; x < Max; x++)
            {
                int pixel = y * Size + x;
                for (int channel = 0; channel < 4; channel++)
                {
                    float onEffect = actorFogOn[pixel][channel] - backgroundFogOn[pixel][channel];
                    float offEffect = actorFogOff[pixel][channel] - backgroundFogOff[pixel][channel];
                    max = Mathf.Max(max, Mathf.Abs(onEffect - offEffect));
                }
            }
            return max;
        }
        static bool Finite(params Color[][] frames)
        {
            foreach (var frame in frames) foreach (var c in frame)
                for (int i = 0; i < 4; i++)
                    if (float.IsNaN(c[i]) || float.IsInfinity(c[i])) return false;
            return true;
        }
    }
}
