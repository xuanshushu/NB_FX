using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
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
    // F0 ordinary Mesh only. Real Forward and both exact NB screen Passes;
    // DepthOnly/ShadowCaster, helper-component lifecycle, GUI and VFX are separate gates.
    public sealed class G4GraphFlipbookTests
    {
        const string GraphPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph";
        const string CurrentPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader";
        const string FrozenPath = "Packages/com.xuanxuan.nb.fx/Tests/Baseline/Frozen/NBShaders2/Shader/NBShader.shader";
        const string Deferred = "NBDeferredDistortPass", Opaque = "NBCameraOpaqueDistortPass";
        const int Size = 128, Min = 40, Max = 88, BackgroundLayer = 2, ForwardLayer = 4;
        const int DirectedLayer = G4GraphScreenNoiseTests.ForegroundLayer;

        [Serializable] sealed class Metrics
        {
            public string route, feed, api, unityVersion, note;
            public bool ortho, finite;
            public float weight, abOn, bcOn, abOff, bcOff, aRepeat, bRepeat, cRepeat,
                aResponse, bResponse, cResponse, aAlternate, bAlternate, cAlternate;
            public int visibleA, visibleB, visibleC, responsePixelsA, responsePixelsB, responsePixelsC;
        }
        struct Difference { public float max; public int pixels; }

        [OneTimeSetUp] public void ForceGraphImport()
        {
            AssetDatabase.ImportAsset(GraphPath,
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            var assembly = AppDomain.CurrentDomain.GetAssemblies().Single(
                x => x.GetName().Name == "Unity.ShaderGraph.Editor");
            var importer = assembly.GetType("UnityEditor.ShaderGraph.ShaderGraphImporter", true);
            var method = importer.GetMethods(BindingFlags.Static | BindingFlags.NonPublic).Single(
                x => x.Name == "GetShaderText" && x.GetParameters().Length == 4 &&
                    x.GetParameters()[3].IsOut);
            object[] arguments = { GraphPath, null, null, null };
            string generated = (string)method.Invoke(null, arguments);
            Assert.That(arguments[3], Is.Not.Null);
            Assert.That(generated, Does.Contain("NBCameraOpaqueDistortPass"));
        }
        static IEnumerable<TestCaseData> Cases()
        {
            foreach (string route in new[] { "Forward", Deferred, Opaque })
                foreach (string feed in new[] { "stream25", "stream75", "helper25", "helper75", "particle25", "particle75" })
                    foreach (bool ortho in new[] { true, false })
                        yield return new TestCaseData(route, feed, ortho).SetName(
                            "G4Flipbook_" + route + "_" + feed + (ortho ? "_ortho" : "_perspective"));
        }
        [TestCaseSource(nameof(Cases))]
        public void OrdinaryMeshFrameBlendMatchesFrozenAndGraph(string route, string feed, bool ortho)
        {
            Assert.That(GraphicsSettings.currentRenderPipeline,
                Is.InstanceOf<UniversalRenderPipelineAsset>());
            Assert.That(GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>()
                .enableRenderCompatibilityMode, Is.False);
            Assert.That(SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf), Is.True);
            var rendererData = ((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline)
                .rendererDataList[0];
            Assert.That(rendererData, Is.InstanceOf<UniversalRendererData>());
            Assert.That(((UniversalRendererData)rendererData).transparentLayerMask.value &
                (1 << ForwardLayer), Is.Not.Zero);
            var nbFeature = rendererData.rendererFeatures.FirstOrDefault(f => f != null &&
                f.GetType().FullName == "NBShader.NBPostProcess");
            Assert.That(nbFeature, Is.Not.Null);
            bool nbWasActive = nbFeature.isActive;
            string rendererFile = Path.Combine(Path.GetDirectoryName(Application.dataPath),
                AssetDatabase.GetAssetPath(rendererData));
            byte[] rendererBefore = File.ReadAllBytes(rendererFile);
            string outputRoot = Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR");
            if (string.IsNullOrEmpty(outputRoot)) outputRoot = Path.Combine(
                Path.GetDirectoryName(Application.dataPath), "Temp/NBFXG4Flipbook");
            string folder = Path.Combine(outputRoot, "g4-flipbook", route + "-" + feed +
                (ortho ? "-ortho" : "-perspective"));
            Directory.CreateDirectory(folder);
            Shader fs = AssetDatabase.LoadAssetAtPath<Shader>(FrozenPath),
                bs = AssetDatabase.LoadAssetAtPath<Shader>(CurrentPath),
                gs = AssetDatabase.LoadAssetAtPath<Shader>(GraphPath);
            Assert.That(fs && bs && gs && fs.isSupported && bs.isSupported && gs.isSupported, Is.True);
            Assert.That(fs.name, Is.EqualTo("Effects/NBShader_T00_Frozen"));
            var scene = EditorSceneManager.NewPreviewScene();
            var background = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var actor = new GameObject("F0 UV0xy/zw and TEXCOORD3 Mesh", typeof(MeshFilter), typeof(MeshRenderer));
            var cameraObject = new GameObject("F0 ordinary Mesh camera");
            var mesh = BuildMesh(feed.EndsWith("75", StringComparison.Ordinal) ? .75f : .25f);
            var a = new Material(fs); var b = new Material(bs); var c = new Material(gs);
            Shader backdropShader = Shader.Find("Universal Render Pipeline/Unlit");
            Assert.That(backdropShader, Is.Not.Null);
            var backdrop = new Material(backdropShader);
            var baseMap = MakeAtlas(), noise = MakeNoise(), backgroundMap = MakeBackground();
            var rt = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGBHalf,
                RenderTextureReadWrite.Linear);
            var readback = new Texture2D(Size, Size, TextureFormat.RGBAHalf, false, true);
            var camera = cameraObject.AddComponent<Camera>();
            var data = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            var priorActive = RenderTexture.active;
            G4ScreenNoiseDirectedFeature directed = null;
            try
            {
                foreach (GameObject go in new[] { background, actor, cameraObject })
                    SceneManager.MoveGameObjectToScene(go, scene);
                background.layer = BackgroundLayer;
                background.transform.position = new Vector3(0, 0, 1);
                background.transform.localScale = new Vector3(6, 6, 1);
                backdrop.SetTexture("_BaseMap", backgroundMap);
                backdrop.SetColor("_BaseColor", Color.white); backdrop.SetFloat("_Cull", 0);
                backdrop.renderQueue = 2000;
                background.GetComponent<MeshRenderer>().sharedMaterial = backdrop;
                background.GetComponent<MeshRenderer>().enabled = route != Deferred;
                actor.layer = route == "Forward" ? ForwardLayer : DirectedLayer;
                actor.transform.position = new Vector3(0, 0, 2);
                actor.transform.rotation = Quaternion.Euler(0, 27, 0);
                actor.transform.localScale = new Vector3(2, 2, 1);
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
                camera.targetTexture = rt; data.requiresColorTexture = true;
                data.renderPostProcessing = false;
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
                float weight = feed.EndsWith("75", StringComparison.Ordinal) ? .75f : .25f;
                bool helper = feed.StartsWith("helper", StringComparison.Ordinal);
                bool particle = feed.StartsWith("particle", StringComparison.Ordinal);
                foreach (var m in new[] { a, b, c })
                    Configure(m, m == c, route, baseMap, noise, helper, particle, weight);
                renderer.enabled = false;
                Color[] backgroundFrame = Capture(camera, rt, readback,
                    Path.Combine(folder, "background"));
                renderer.enabled = true; renderer.sharedMaterial = c;
                Capture(camera, rt, readback, Path.Combine(folder, "C-warm"));
                Configure(c, true, route, baseMap, noise, helper, particle, weight);
                foreach (string property in new[] { "_FlipbookBlending",
                    "_BaseMap_AnimationSheetBlend_ST", "_AnimationSheetHelperBlendIntensity" })
                    Assert.That(c.HasProperty(property), Is.True,
                        "F0 Graph property absent after real warm draw: " + property);
                foreach (Material m in new[] { a, b, c })
                    Assert.That(m.FindPass(route == "Forward" ?
                        (m == c ? "Universal Forward" : "UniversalForward") : route),
                        Is.GreaterThanOrEqualTo(0));
                Color[] Snap(Material m, bool enabled, bool alternate, string label)
                {
                    SetStreamNextFrame(mesh, alternate && !helper);
                    SetState(m, m == c, helper, enabled, weight, alternate);
                    renderer.sharedMaterial = m;
                    return Capture(camera, rt, readback, Path.Combine(folder, label));
                }
                var af = Snap(a, true, false, "A-on"); var ar = Snap(a, true, false, "A-repeat");
                var bf = Snap(b, true, false, "B-on"); var br = Snap(b, true, false, "B-repeat");
                var cf = Snap(c, true, false, "C-on"); var cr = Snap(c, true, false, "C-repeat");
                var ao = Snap(a, false, false, "A-off");
                var bo = Snap(b, false, false, "B-off");
                var co = Snap(c, false, false, "C-off");
                var aa = Snap(a, true, true, "A-alt-next-frame");
                var ba = Snap(b, true, true, "B-alt-next-frame");
                var ca = Snap(c, true, true, "C-alt-next-frame");
                var ra = Compare(af, ao); var rb = Compare(bf, bo); var rc = Compare(cf, co);
                var mtr = new Metrics {
                    route = route, feed = feed, ortho = ortho, weight = weight,
                    api = SystemInfo.graphicsDeviceType.ToString(), unityVersion = Application.unityVersion,
                    note = "F0 ordinary Mesh. UV0.xy first/zw second, UV3.x stream or Flags1 bit15 helper, " +
                        "plus Flags1 bits23+19 particle special UV3.yz path. " +
                        "Forward and exact NB screen Passes only; no automatic postprocess, Depth/Shadow, " +
                        "component lifecycle, GUI, VFX or Player claim. A Frozen/B current/C Graph, " +
                        "linear RGBAHalf; strict ROI 40..87; full raw frames saved.",
                    finite = Finite(backgroundFrame, af, ar, bf, br, cf, cr,
                        ao, bo, co, aa, ba, ca),
                    visibleA = Compare(af, backgroundFrame).pixels,
                    visibleB = Compare(bf, backgroundFrame).pixels,
                    visibleC = Compare(cf, backgroundFrame).pixels,
                    abOn = Compare(af, bf).max, bcOn = Compare(bf, cf).max,
                    abOff = Compare(ao, bo).max, bcOff = Compare(bo, co).max,
                    aRepeat = Compare(af, ar).max, bRepeat = Compare(bf, br).max,
                    cRepeat = Compare(cf, cr).max,
                    aResponse = ra.max, bResponse = rb.max, cResponse = rc.max,
                    responsePixelsA = ra.pixels, responsePixelsB = rb.pixels,
                    responsePixelsC = rc.pixels,
                    aAlternate = Compare(af, aa).max,
                    bAlternate = Compare(bf, ba).max,
                    cAlternate = Compare(cf, ca).max
                };
                File.WriteAllText(Path.Combine(folder, "metrics.json"), JsonUtility.ToJson(mtr, true));
                Debug.Log("NBFX_G4_FLIPBOOK_ABC " + JsonUtility.ToJson(mtr));
                Assert.That(mtr.finite, Is.True);
                Assert.That(mtr.visibleA, Is.GreaterThan(100));
                Assert.That(mtr.visibleB, Is.GreaterThan(100));
                Assert.That(mtr.visibleC, Is.GreaterThan(100));
                Assert.That(mtr.aRepeat + mtr.bRepeat + mtr.cRepeat, Is.Zero);
                Assert.That(mtr.abOn + mtr.bcOn + mtr.abOff + mtr.bcOff, Is.Zero);
                Assert.That(mtr.aResponse, Is.GreaterThan(.005f));
                Assert.That(mtr.bResponse, Is.GreaterThan(.005f));
                Assert.That(mtr.cResponse, Is.GreaterThan(.005f));
                Assert.That(mtr.responsePixelsA, Is.GreaterThan(20));
                Assert.That(mtr.responsePixelsB, Is.GreaterThan(20));
                Assert.That(mtr.responsePixelsC, Is.GreaterThan(20));
                Assert.That(mtr.aAlternate, Is.GreaterThan(.005f));
                Assert.That(mtr.bAlternate, Is.GreaterThan(.005f));
                Assert.That(mtr.cAlternate, Is.GreaterThan(.005f));
                Assert.That(Compare(aa, ba).max + Compare(ba, ca).max, Is.Zero);
            }
            finally
            {
                if (directed != null)
                { rendererData.rendererFeatures.Remove(directed); UnityEngine.Object.DestroyImmediate(directed); }
                nbFeature.SetActive(nbWasActive); rendererData.SetDirty();
                camera.targetTexture = null; RenderTexture.active = priorActive; rt.Release();
                foreach (var obj in new UnityEngine.Object[] { background, actor, cameraObject,
                    mesh, a, b, c, backdrop, baseMap, noise, backgroundMap, rt, readback })
                    UnityEngine.Object.DestroyImmediate(obj);
                EditorSceneManager.ClosePreviewScene(scene);
                Assert.That(File.ReadAllBytes(rendererFile), Is.EqualTo(rendererBefore),
                    "F0 fixture changed renderer asset on disk.");
            }
        }
        static Mesh BuildMesh(float weight)
        {
            var mesh = new Mesh { name = "F0 two-frame UV0 + UV3 stream" };
            mesh.vertices = new[] { new Vector3(-1, -1, 0), new Vector3(1, -1, 0),
                new Vector3(-1, 1, 0), new Vector3(1, 1, 0) };
            mesh.triangles = new[] { 0, 1, 2, 1, 3, 2 };
            var uv0 = new List<Vector4>(); var uv1 = new List<Vector4>();
            var uv2 = new List<Vector4>(); var uv3 = new List<Vector4>();
            for (int i = 0; i < 4; i++)
            {
                float u = (i & 1) != 0 ? .20f : .05f;
                float v = i >= 2 ? .80f : .20f;
                uv0.Add(new Vector4(u, v, u + .5f, v));
                uv1.Add(new Vector4(.13f + .07f * i, .17f, .29f, .43f));
                uv2.Add(new Vector4(.11f, .19f + .05f * i, .37f, .59f));
                uv3.Add(new Vector4(weight, .31f, .69f, 0));
            }
            mesh.SetUVs(0, uv0); mesh.SetUVs(1, uv1);
            mesh.SetUVs(2, uv2); mesh.SetUVs(3, uv3);
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            return mesh;
        }
        static void SetStreamNextFrame(Mesh mesh, bool alternate)
        {
            var uv = new List<Vector4>();
            mesh.GetUVs(0, uv);
            for (int i = 0; i < uv.Count; i++)
            {
                Vector4 v = uv[i]; v.z = v.x + (alternate ? 0 : .5f); uv[i] = v;
            }
            mesh.SetUVs(0, uv);
        }
        static Texture2D MakeAtlas()
        {
            const int n = 64;
            var t = new Texture2D(n, n, TextureFormat.RGBAHalf, false, true);
            var pixels = new Color[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float v = y / (n - 1f);
                pixels[y * n + x] = x < n / 2 ?
                    new Color(.8f - .25f * v, .13f, .12f + .18f * v, .25f) :
                    new Color(.12f, .32f + .38f * v, .8f - .15f * v, .75f);
            }
            t.SetPixels(pixels); t.Apply(false); t.filterMode = FilterMode.Point;
            t.wrapMode = TextureWrapMode.Repeat; return t;
        }
        static Texture2D MakeNoise()
        {
            var t = new Texture2D(1, 1, TextureFormat.RGBAHalf, false, true);
            t.SetPixel(0, 0, new Color(.75f, .25f, 0, 1)); t.Apply(false);
            t.filterMode = FilterMode.Point; return t;
        }
        static Texture2D MakeBackground()
        {
            const int n = 64; var t = new Texture2D(n, n, TextureFormat.RGBAHalf, false, true);
            var p = new Color[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                p[y * n + x] = new Color(.2f + .7f * x / (n - 1f),
                    .15f + .7f * y / (n - 1f), .25f + .3f * (x + y) / (2f * n - 2f), 1);
            t.SetPixels(p); t.Apply(false); t.filterMode = FilterMode.Bilinear;
            t.wrapMode = TextureWrapMode.Clamp; return t;
        }
        static void Configure(Material m, bool graph, string route, Texture2D atlas,
            Texture2D noise, bool helper, bool particle, float weight)
        {
            m.shaderKeywords = graph ? new[] { "_SURFACE_TYPE_TRANSPARENT" } :
                new[] { "_FX_LIGHT_MODE_UNLIT", "_FLIPBOOKBLENDING_ON", "_NOISEMAP" };
            m.SetTexture("_BaseMap", atlas); m.SetTexture("_NoiseMap", noise);
            m.SetTextureScale("_BaseMap", Vector2.one); m.SetTextureOffset("_BaseMap", Vector2.zero);
            m.SetTextureScale("_NoiseMap", Vector2.one); m.SetTextureOffset("_NoiseMap", Vector2.zero);
            m.SetColor(graph ? "_Color" : "_BaseColor", Color.white);
            m.SetColor("_ColorA", Color.white); m.SetFloat("_AlphaAll", 1);
            m.SetFloat("_BaseColorIntensityForTimeline", 1);
            m.SetFloat("_Cull", (float)CullMode.Off);
            m.SetFloat("_ZTest", (float)CompareFunction.LessEqual); m.SetFloat("_ZWrite", 0);
            m.SetFloat("_SrcBlend", (float)BlendMode.One); m.SetFloat("_DstBlend", (float)BlendMode.Zero);
            m.SetFloat("_TexDistortion_intensity", 0);
            m.SetFloat("_NoiseIntensity", .5f); m.SetFloat("_noisemapEnabled", 1);
            m.SetVector("_NoiseOffset", Vector4.zero);
            m.SetVector("_DistortionDirection", new Vector4(.5f, .75f, 0, 0));
            m.SetFloat("_ScreenDistortAlphaPow", 1); m.SetFloat("_ScreenDistortAlphaMulti", 1);
            m.SetFloat("_ScreenDistortAlphaAdd", 0); m.SetFloat("_ScreenDistortIntensity", .65f);
            m.SetFloat("_FlipbookBlending", 1);
            m.SetVector("_BaseMap_AnimationSheetBlend_ST", new Vector4(1, 1, .5f, 0));
            m.SetFloat("_AnimationSheetHelperBlendIntensity", weight);
            if (graph)
            {
                m.SetFloat("_Surface", 1);
                m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                m.SetFloat("_DstBlendAlpha", (float)BlendMode.Zero);
                m.SetVector("_BaseMap_ST", new Vector4(1, 1, 0, 0));
                m.SetVector("_NB_DistortionNoise", Vector4.zero);
                m.SetFloat("_NB_DistortionMode", route == Deferred ? 1 : route == Opaque ? 2 : 0);
                m.SetFloat("_NB_DistortionAlphaPow", 1);
                m.SetFloat("_NB_DistortionAlphaMultiplier", 1);
                m.SetFloat("_NB_DistortionAlphaAdd", 0);
                m.SetFloat("_NB_DistortionIntensity", .65f);
                m.SetFloat("_NB_ColorChannelLo16", 3);
                m.SetFloat("_ColorMask", 15);
                SetWord(m, "_NB_Flags1Lo16", "_NB_Flags1Hi16",
                    (1u << 9) | (helper ? 1u << 15 : 0u) |
                    (particle ? (1u << 23) | (1u << 19) : 0u));
                SetWord(m, "_NB_UVModeFlag0Lo16", "_NB_UVModeFlag0Hi16",
                    particle ? 1u : 0u);
            }
            else
            {
                m.SetFloat("_fogintensity", 0); m.SetFloat("_ColorMask", 15);
                m.SetFloat("_FxLightMode", 0);
                m.SetInteger("_W9ParticleShaderColorChannelFlag", 3);
                m.SetInteger("_W9ParticleShaderFlags1", (1 << 9) |
                    (helper ? 1 << 15 : 0) |
                    (particle ? (1 << 23) | (1 << 19) : 0));
                m.SetInteger("_UVModeFlag0", particle ? 1 : 0);
            }
            m.renderQueue = 3000;
            foreach (string pass in new[] { "SRPDefaultUnlit", "SRPDEFAULTUNLIT", "UniversalForward",
                "DepthOnly", "ShadowCaster", "Universal2D", Deferred, Opaque })
                m.SetShaderPassEnabled(pass, false);
            if (route == "Forward")
            {
                m.SetShaderPassEnabled(graph ? "SRPDefaultUnlit" : "UniversalForward", true);
                if (graph) m.SetShaderPassEnabled("SRPDEFAULTUNLIT", true);
            }
            else m.SetShaderPassEnabled(route, true);
        }
        static void SetState(Material m, bool graph, bool helper, bool on,
            float weight, bool alternate)
        {
            m.SetFloat("_FlipbookBlending", on ? 1 : 0);
            if (!graph)
            { if (on) m.EnableKeyword("_FLIPBOOKBLENDING_ON"); else m.DisableKeyword("_FLIPBOOKBLENDING_ON"); }
            // Helper changes next-frame ST; stream changes UV0.zw on the Mesh.
            // Both controls should visibly select the first frame instead.
            if (helper)
                m.SetVector("_BaseMap_AnimationSheetBlend_ST",
                    alternate ? new Vector4(1, 1, 0, 0) : new Vector4(1, 1, .5f, 0));
            else
                m.SetVector("_BaseMap_AnimationSheetBlend_ST", new Vector4(1, 1, .5f, 0));
            m.SetFloat("_AnimationSheetHelperBlendIntensity", weight);
        }
        static void SetWord(Material m, string lo, string hi, uint value)
        { m.SetFloat(lo, value & 65535u); m.SetFloat(hi, value >> 16); }
        static Color[] Capture(Camera camera, RenderTexture rt, Texture2D readback, string path)
        {
            for (int i = 0; i < 4; i++) camera.Render();
            RenderTexture prior = RenderTexture.active;
            try
            {
                RenderTexture.active = rt;
                readback.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
                readback.Apply(false); var pixels = readback.GetPixels();
                using (var file = File.Create(path + ".rgba-f32.gz"))
                using (var zip = new GZipStream(file, CompressionMode.Compress))
                using (var writer = new BinaryWriter(zip))
                    foreach (Color color in pixels)
                    { writer.Write(color.r); writer.Write(color.g); writer.Write(color.b); writer.Write(color.a); }
                return pixels;
            }
            finally { RenderTexture.active = prior; }
        }
        static Difference Compare(Color[] a, Color[] b)
        {
            var result = new Difference();
            for (int y = Min; y < Max; y++) for (int x = Min; x < Max; x++)
            {
                int k = y * Size + x; float delta = 0;
                for (int i = 0; i < 4; i++)
                    delta = Mathf.Max(delta, Mathf.Abs(a[k][i] - b[k][i]));
                result.max = Mathf.Max(result.max, delta);
                if (delta > 0) result.pixels++;
            }
            return result;
        }
        static bool Finite(params Color[][] frames)
        {
            foreach (Color[] frame in frames) foreach (Color c in frame)
                for (int i = 0; i < 4; i++)
                    if (float.IsNaN(c[i]) || float.IsInfinity(c[i])) return false;
            return true;
        }
    }
}
