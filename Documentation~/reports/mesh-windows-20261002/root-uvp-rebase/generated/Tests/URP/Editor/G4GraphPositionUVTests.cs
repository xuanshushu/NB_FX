using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.ExceptionServices;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace NBFX.Baseline.Tests
{
    // REVIEW CANDIDATE ONLY (/tmp). Requires the position/stage adapter product.
    // Root must batch by consumer: the long single-Editor-frame host is invalid.
    // This first fixture covers Main + existing 8 surface feature setters.
    // Noise/NoiseMask/Bump/PNoise/VertexMap/VertexMask get dedicated follow-up fixtures.
    public sealed class G4GraphPositionUVTests
    {
        const string Base = "Packages/com.xuanxuan.nb.fx/";
        const string GraphPath = Base + "NBShaders2/ShaderGraph/NBShaderGraph.shadergraph";
        const string CurrentPath = Base + "NBShaders2/Shader/NBShader.shader";
        const string FrozenPath = Base + "Tests/Baseline/Frozen/NBShaders2/Shader/NBShader.shader";
        const int Size = 128, Layer = 4, Min = 40, Max = 88;
        static readonly string[] Consumers = { "main", "mask1", "mask2", "mask3", "overlay1", "overlay2", "color-ramp", "dissolve", "dissolve-mask" };
        static IEnumerable<TestCaseData> Cases()
        {
            foreach (string consumer in Consumers)
                for (int source = 3; source <= 7; ++source)
                    foreach (bool shared in new[] { false, true })
                        foreach (bool fragment in new[] { false, true })
                            foreach (bool ortho in new[] { true, false })
                                yield return Case(consumer, source, shared, fragment, ortho, 1);
            // Non-default projection axes independently exercise the real selectors.
            foreach (int source in new[] { 6, 7 })
                foreach (int selector in new[] { 0, 2 })
                    foreach (bool fragment in new[] { false, true })
                        foreach (bool ortho in new[] { true, false })
                            yield return Case("main", source, false, fragment, ortho, selector);
        }
        static TestCaseData Case(string c, int s, bool shared, bool frag, bool ortho, int selector)
            => new TestCaseData(c, s, shared, frag, ortho, selector).SetName("G4PositionUVABC_" + c + "_" +
                (shared ? "shared" : "direct") + s + "_axis" + selector + (frag ? "_fragment" : "_vertex") + (ortho ? "_ortho" : "_perspective"));

        [Serializable] sealed class Metrics
        {
            public string consumer, sourcePath, target, note; public int mode, selector;
            public bool shared, fragment, ortho, finite;
            public float ab, bc, abControl, bcControl, aRepeat, bRepeat, cRepeat,
                aControlRepeat, bControlRepeat, cControlRepeat, aResponse, bResponse, cResponse;
            public int aVisible, bVisible, cVisible, aResponsePixels, bResponsePixels, cResponsePixels;
            public bool mainSelfFourEquivalent;
        }
        [OneTimeSetUp]
        public void Import() => AssetDatabase.ImportAsset(GraphPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

        [TestCaseSource(nameof(Cases))]
        public void PositionalSourceMatchesOriginalMesh(string consumer, int source, bool shared, bool fragment, bool ortho, int selector)
        {
            Assert.That(SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf), Is.True);
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            Assert.That(pipeline, Is.Not.Null);
            var rd = pipeline.rendererDataList[0] as UniversalRendererData;
            Assert.That(rd, Is.Not.Null); Assert.That(rd.transparentLayerMask.value & (1 << Layer), Is.Not.Zero);
            var nb = rd.rendererFeatures.Find(f => f && f.GetType().FullName == "NBShader.NBPostProcess");
            Assert.That(nb, Is.Not.Null); bool wasActive = nb.isActive;
            string rdFile = Path.Combine(Path.GetDirectoryName(Application.dataPath), AssetDatabase.GetAssetPath(rd));
            byte[] rdBytes = File.ReadAllBytes(rdFile);
            string id = consumer + "-" + (shared ? "shared" : "direct") + source + "-axis" + selector +
                (fragment ? "-fragment" : "-vertex") + (ortho ? "-ortho" : "-perspective");
            string folder = Path.Combine(Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR") ??
                Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/NBFXG4PositionUV"), "g4-position-uv", id);
            Directory.CreateDirectory(folder);
            var scene = EditorSceneManager.NewPreviewScene();
            var actor = new GameObject("UVP coarse non-planar Mesh", typeof(MeshFilter), typeof(MeshRenderer));
            var cameraGO = new GameObject("UVP Camera"); var camera = cameraGO.AddComponent<Camera>();
            var mesh = CoarseMesh(); var map = Map();
            var a = MaterialAt(FrozenPath); var b = MaterialAt(CurrentPath); var c = MaterialAt(GraphPath);
            var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            var read = new Texture2D(Size, Size, TextureFormat.RGBAHalf, false, true);
            RenderTexture active = RenderTexture.active;
            var captured = new List<Color[]>();
            try
            {
                SceneManager.MoveGameObjectToScene(actor, scene); SceneManager.MoveGameObjectToScene(cameraGO, scene);
                actor.layer = Layer; actor.transform.position = new Vector3(.21f, -.13f, .37f);
                actor.transform.rotation = Quaternion.Euler(18, 31, 7); actor.transform.localScale = new Vector3(1.15f, .9f, 1.2f);
                actor.GetComponent<MeshFilter>().sharedMesh = mesh; var mr = actor.GetComponent<MeshRenderer>();
                mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false;
                camera.scene = scene; camera.orthographic = ortho; camera.orthographicSize = 1.7f; camera.fieldOfView = 48;
                camera.transform.position = new Vector3(0, 0, 5); camera.transform.rotation = Quaternion.Euler(0, 180, 0);
                camera.nearClipPlane = .1f; camera.farClipPlane = 20; camera.cullingMask = 1 << Layer;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.03f, .02f, .01f, .12f);
                camera.allowHDR = true; camera.allowMSAA = false; camera.targetTexture = target;
                var data = cameraGO.AddComponent<UniversalAdditionalCameraData>(); data.SetRenderer(0); data.renderPostProcessing = false;
                target.Create(); Assert.That(target.IsCreated() && !target.sRGB, Is.True);
                nb.SetActive(false); rd.SetDirty();
                // Diagnostic only: batch-7's original 520 payloads were all
                // half 0xCDCD after D3D11 device removal. Prove that the fresh
                // process can clear and read the target before testing math.
                mr.enabled = false;
                var background = Capture(camera, target, read, folder, "diagnostic-background");
                var backgroundRepeat = Capture(camera, target, read, folder, "diagnostic-background-repeat");
                Assert.That(Delta(background, backgroundRepeat), Is.Zero, "Background readback changed before the feature was drawn.");
                Assert.That(background[0].r, Is.GreaterThan(background[0].g), "Background R/G order was not rendered.");
                Assert.That(background[0].g, Is.GreaterThan(background[0].b), "Background G/B order was not rendered.");
                Assert.That(background[0].b, Is.GreaterThanOrEqualTo(0f));
                Assert.That(background[0].a, Is.GreaterThan(0f));
                mr.enabled = true;
                Basic(c, true); c.SetTexture("_BaseMap", Texture2D.whiteTexture); mr.sharedMaterial = c;
                var importWarm = Capture(camera, target, read, folder, "C-import-warm");
                Assert.That(Visible(importWarm), Is.GreaterThan(100), "Graph warm-up must be visible before Dissolve is configured; invalid host output is not feature parity.");
                // Schema after real URP camera draw, never the import placeholder.
                foreach (string property in new[] { "_WorldSpaceUVModeSelector", "_ObjectSpaceUVModeSelector", "_CylinderUVRotate", "_CylinderUVPosOffset", "_CylinderMatrix0", "_CylinderMatrix1", "_CylinderMatrix2", "_CylinderMatrix3" })
                {
                    Assert.That(c.HasProperty(property), Is.True, "UVP Graph interface not yet installed: " + property);
                    int index = c.shader.FindPropertyIndex(property); Assert.That(index, Is.GreaterThanOrEqualTo(0));
                    Assert.That(c.shader.GetPropertyType(index), Is.EqualTo(property.Contains("Selector") ? ShaderPropertyType.Float : ShaderPropertyType.Vector));
                }
                Assert.That(c.FindPass("Universal Forward"), Is.GreaterThanOrEqualTo(0), "Actual generated main Forward required after Camera warm.");
                foreach (var material in new[] { a, b, c })
                {
                    if (consumer == "main") { Basic(material, material == c); material.SetTexture("_BaseMap", map); }
                    else ExistingFeatureConfigure(material, material == c, consumer, map);
                    if (material.HasProperty("_fogintensity")) material.SetFloat("_fogintensity", 0);
                    SetupCoordinates(material, material == c, selector);
                    SetRoute(material, consumer, source, shared, fragment);
                }
                Color[] Snap(Material material, string label)
                {
                    mr.sharedMaterial = material; Color[] frame = Capture(camera, target, read, folder, label); captured.Add(frame); return frame;
                }
                var aa = Snap(a, "A-frozen"); var ar = Snap(a, "A-repeat");
                var bb = Snap(b, "B-current"); var br = Snap(b, "B-repeat");
                var cc = Snap(c, "C-graph"); var cr = Snap(c, "C-repeat");
                bool selfFour = consumer == "main" && source == 4 && !shared;
                bool sameFour = false;
                if (selfFour)
                {
                    // Main mode4 reads defaultUV before its own transform; route0 is intentionally equal.
                    foreach (var m in new[] { a, b, c }) SetRoute(m, consumer, 0, false, fragment);
                    var a0 = Snap(a, "A-mode0-equivalence"); var b0 = Snap(b, "B-mode0-equivalence"); var c0 = Snap(c, "C-mode0-equivalence");
                    sameFour = Delta(aa, a0) == 0 && Delta(bb, b0) == 0 && Delta(cc, c0) == 0;
                    foreach (var m in new[] { a, b, c })
                    {
                        SetRoute(m, consumer, source, false, fragment);
                        SetBaseST(m, m == c, new Vector4(2.31f, .47f, .24f, .1f));
                    }
                }
                else foreach (var m in new[] { a, b, c }) SetRoute(m, consumer, 0, false, fragment);
                var ac = Snap(a, "A-control"); var acr = Snap(a, "A-control-repeat");
                var bc = Snap(b, "B-control"); var bcr = Snap(b, "B-control-repeat");
                var cx = Snap(c, "C-control"); var cxr = Snap(c, "C-control-repeat");
                var metrics = new Metrics {
                    consumer = consumer, mode = source, shared = shared, fragment = fragment, ortho = ortho, selector = selector,
                    sourcePath = "A Frozen/B current/C actual product Graph; no shader probe substituted",
                    target = "128 RGBAHalf, raw f32 full frame; strict ROI40..87, finite full frame, repeat strict0",
                    note = "UVP review candidate. Cylinder/NDC/Shared/Main source evaluated at original stage; Twirl flag with strength0 forces legacy fragment path. Feature-owned ST/rotation/scroll stage strict parity remains follow-up. No VertexOffset/VAT/CustomLocal/UI/Particle/VFX/Player claim. Matrix rows installed explicitly with the original T*Quaternion.Euler formula, not GUI evidence.",
                    ab = Delta(aa, bb), bc = Delta(bb, cc), abControl = Delta(ac, bc), bcControl = Delta(bc, cx),
                    aRepeat = Delta(aa, ar), bRepeat = Delta(bb, br), cRepeat = Delta(cc, cr),
                    aControlRepeat = Delta(ac, acr), bControlRepeat = Delta(bc, bcr), cControlRepeat = Delta(cx, cxr),
                    aResponse = Delta(aa, ac), bResponse = Delta(bb, bc), cResponse = Delta(cc, cx),
                    aResponsePixels = Changed(aa, ac), bResponsePixels = Changed(bb, bc), cResponsePixels = Changed(cc, cx),
                    aVisible = Visible(aa), bVisible = Visible(bb), cVisible = Visible(cc), finite = Finite(captured),
                    mainSelfFourEquivalent = sameFour
                };
                File.WriteAllText(Path.Combine(folder, "metrics.json"), JsonUtility.ToJson(metrics, true));
                Debug.Log("NBFX_G4_POSITION_UV_ABC " + JsonUtility.ToJson(metrics));
                Assert.That(metrics.finite, Is.True, "NaN/Inf is never a microdiff exemption.");
                Assert.That(metrics.aVisible, Is.GreaterThan(100)); Assert.That(metrics.bVisible, Is.GreaterThan(100)); Assert.That(metrics.cVisible, Is.GreaterThan(100));
                Assert.That(metrics.aRepeat + metrics.bRepeat + metrics.cRepeat + metrics.aControlRepeat + metrics.bControlRepeat + metrics.cControlRepeat, Is.Zero);
                Assert.That(metrics.aResponse, Is.GreaterThan(.03f)); Assert.That(metrics.bResponse, Is.GreaterThan(.03f)); Assert.That(metrics.cResponse, Is.GreaterThan(.03f));
                Assert.That(metrics.aResponsePixels, Is.GreaterThan(48)); Assert.That(metrics.bResponsePixels, Is.GreaterThan(48)); Assert.That(metrics.cResponsePixels, Is.GreaterThan(48));
                if (selfFour) Assert.That(metrics.mainSelfFourEquivalent, Is.True, "Main self4 must equal route0 plus its own transform.");
                Assert.That(metrics.ab + metrics.abControl, Is.Zero, "Shared helper extraction changed original ShaderLab.");
                Assert.That(metrics.bc + metrics.bcControl, Is.Zero, "Position UV Graph mismatch; raw retained, no threshold relaxation.");
            }
            finally
            {
                nb.SetActive(wasActive); rd.SetDirty(); camera.targetTexture = null; RenderTexture.active = active; target.Release();
                foreach (var obj in new UnityEngine.Object[] { a, b, c, mesh, map, target, read }) UnityEngine.Object.DestroyImmediate(obj);
                EditorSceneManager.ClosePreviewScene(scene);
                Assert.That(File.ReadAllBytes(rdFile), Is.EqualTo(rdBytes), "UVP fixture saved renderer asset.");
            }
        }
        static Material MaterialAt(string path)
        { var shader = AssetDatabase.LoadAssetAtPath<Shader>(path); Assert.That(shader && shader.isSupported, Is.True, path); return new Material(shader); }
        static void ExistingFeatureConfigure(Material m, bool graph, string consumer, Texture2D map)
        {
            // Actual existing fixture business setter; not a CPU formula replacing a ShaderGraph draw.
            var method = typeof(G4GraphFeatureUVTests).GetMethod("Configure", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            try { method.Invoke(null, new object[] { m, graph, consumer, "default-uv0", map }); }
            catch (TargetInvocationException e) { ExceptionDispatchInfo.Capture(e.InnerException ?? e).Throw(); }
            m.SetShaderPassEnabled("UniversalForward", true);
            if (graph) m.SetShaderPassEnabled("SRPDefaultUnlit", true);
            foreach (string p in new[] { "DepthOnly", "ShadowCaster", "NBCameraOpaqueDistortPass", "NBDeferredDistortPass", "Universal2D" }) m.SetShaderPassEnabled(p, false);
        }
        static void Basic(Material m, bool graph)
        {
            m.shaderKeywords = graph ? new[] { "_SURFACE_TYPE_TRANSPARENT" } : new[] { "_FX_LIGHT_MODE_UNLIT" };
            m.SetColor(graph ? "_Color" : "_BaseColor", Color.white); m.SetColor("_ColorA", Color.white);
            m.SetFloat("_BaseColorIntensityForTimeline", 1); m.SetFloat("_AlphaAll", 1);
            m.SetFloat("_Cull", 0); m.SetFloat("_ZTest", 4); m.SetFloat("_ZWrite", 0);
            m.SetFloat("_SrcBlend", 1); m.SetFloat("_DstBlend", 0); m.SetFloat("_ColorMask", 15); m.renderQueue = 3000;
            if (graph) { m.SetFloat("_Surface", 1); m.SetFloat("_SrcBlendAlpha", 1); m.SetFloat("_DstBlendAlpha", 0); m.SetFloat("_NB_ColorChannelLo16", 3); m.SetFloat("_NB_DistortionMode", 0); }
            else { m.SetInteger("_W9ParticleShaderColorChannelFlag", 3); m.SetFloat("_fogintensity", 0); }
            m.SetShaderPassEnabled("UniversalForward", true); m.SetShaderPassEnabled("SRPDefaultUnlit", graph);
            foreach (string p in new[] { "DepthOnly", "ShadowCaster", "NBCameraOpaqueDistortPass", "NBDeferredDistortPass", "Universal2D" }) m.SetShaderPassEnabled(p, false);
        }
        static void SetupCoordinates(Material m, bool graph, int selector)
        {
            m.SetFloat("_WorldSpaceUVModeSelector", selector); m.SetFloat("_ObjectSpaceUVModeSelector", selector);
            var rotate = new Vector4(17, 29, 90, 0); var offset = new Vector4(.23f, -.19f, .31f, 0);
            m.SetVector("_CylinderUVRotate", rotate); m.SetVector("_CylinderUVPosOffset", offset);
            Matrix4x4 matrix = Matrix4x4.Translate(offset) * Matrix4x4.Rotate(Quaternion.Euler(rotate));
            for (int i = 0; i < 4; i++) m.SetVector("_CylinderMatrix" + i, matrix.GetRow(i));
            SetBaseST(m, graph, new Vector4(1.37f, .73f, -.11f, .17f));
            m.SetFloat("_BaseMapUVRotation", 0); m.SetFloat("_BaseMapUVRotationSpeed", 0); m.SetVector("_BaseMapMaskMapOffset", Vector4.zero);
            m.SetVector("_SharedUV_ST", new Vector4(.79f, 1.13f, .21f, -.09f)); m.SetVector("_SharedUV_Vec", Vector4.zero);
            m.SetVector("_TWParameter", new Vector4(.38f, .62f, 0, 0)); m.SetFloat("_TWStrength", 0);
            m.SetFloat("_ParallaxMapping_Toggle", 0); m.SetFloat("_DepthDecal_Toggle", 0);
        }
        static void SetBaseST(Material m, bool graph, Vector4 st)
        { m.SetTextureScale("_BaseMap", new Vector2(st.x, st.y)); m.SetTextureOffset("_BaseMap", new Vector2(st.z, st.w)); if (graph) m.SetVector("_BaseMap_ST", st); }
        static int Position(string consumer)
        {
            switch (consumer) { case "main": return 0; case "mask1": return 2; case "mask2": return 4; case "mask3": return 6;
                case "overlay1": return 12; case "dissolve": return 14; case "dissolve-mask": return 16; case "overlay2": return 18; case "color-ramp": return 26; default: throw new ArgumentException(consumer); }
        }
        static void SetRoute(Material m, string consumer, int mode, bool shared, bool fragment)
        {
            bool graph = m.HasProperty("_NB_UVModeFlag0Lo16");
            if (graph) foreach (string p in new[] { "_NB_UVModeFlag0Lo16", "_NB_UVModeFlag0Hi16", "_NB_UVModeFlagType0Lo16", "_NB_UVModeFlagType0Hi16", "_NB_Flags0Lo16", "_NB_Flags0Hi16", "_NB_Flags1Lo16", "_NB_Flags1Hi16" }) m.SetFloat(p, 0);
            else foreach (string p in new[] { "_UVModeFlag0", "_UVModeFlagType0", "_W9ParticleShaderFlags", "_W9ParticleShaderFlags1" }) m.SetInteger(p, 0);
            // The actual URP fixture asmdef intentionally has no NB runtime
            // reference. Invoke the one real storage/business API, not a second
            // test-only packed decoder/writer or an extra assembly dependency.
            Type type = FindFlagsType(); var flags = Activator.CreateInstance(type, new object[] { m });
            Type modes = type.GetNestedType("UVMode"); Assert.That(modes, Is.Not.Null);
            Invoke(flags, "SetUVMode", Enum.ToObject(modes, shared ? 8 : mode), Position(consumer), 0);
            Invoke(flags, "SetUVMode", Enum.ToObject(modes, shared ? mode : 0), 30, 0);
            Invoke(flags, "SetFlagBits", Constant(type, "FLAG_BIT_PARTICLE_1_IGNORE_VERTEX_COLOR"), null, 1);
            if (mode == 3) Invoke(flags, "SetFlagBits", Constant(type, "FLAG_BIT_PARTICLE_1_CYLINDER_CORDINATE"), null, 1);
            if (fragment) Invoke(flags, "SetFlagBits", Constant(type, "FLAG_BIT_PARTICLE_UTWIRL_ON"), null, 0);
        }
        static Type FindFlagsType()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType("NBShader.NBShaderFlags", false);
                if (type != null) return type;
            }
            Assert.Fail("The real NBShader.NBShaderFlags runtime type is not loaded."); return null;
        }
        static int Constant(Type type, string name)
        {
            var field = type.GetField(name, BindingFlags.Public | BindingFlags.Static);
            Assert.That(field, Is.Not.Null, name); return (int)field.GetValue(null);
        }
        static void Invoke(object target, string name, params object[] args)
        {
            var method = target.GetType().GetMethod(name, BindingFlags.Public | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null, name);
            try { method.Invoke(target, args); }
            catch (TargetInvocationException e) { ExceptionDispatchInfo.Capture(e.InnerException ?? e).Throw(); }
        }
        static Mesh CoarseMesh()
        {
            var mesh = new Mesh { name = "UVP coarse non-planar two triangles" };
            mesh.vertices = new[] { new Vector3(-1, -1, .21f), new Vector3(-1, 1, -.17f), new Vector3(1, 1, .33f), new Vector3(1, -1, -.25f) };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.SetUVs(0, new List<Vector4> { new Vector4(.09f, .13f, .84f, .18f), new Vector4(.09f, .83f, .84f, .77f), new Vector4(.88f, .83f, .33f, .77f), new Vector4(.88f, .13f, .33f, .18f) });
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
        static Texture2D Map()
        {
            const int n = 64; var tex = new Texture2D(n, n, TextureFormat.RGBA32, false, true); var pixels = new Color[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++) pixels[y * n + x] = new Color((x + 1f) / (n + 1f), (y + 1f) / (n + 1f), .15f + .7f * ((x * 3 + y * 5) % 31) / 30f, .25f + .7f * ((x * 7 + y * 11) % 47) / 46f);
            tex.SetPixels(pixels); tex.Apply(false); tex.filterMode = FilterMode.Bilinear; tex.wrapMode = TextureWrapMode.Repeat; return tex;
        }
        static Color[] Capture(Camera camera, RenderTexture target, Texture2D read, string folder, string label)
        {
            Assert.That(camera.targetTexture, Is.SameAs(target)); Assert.That(target.IsCreated(), Is.True);
            for (int i = 0; i < 4; i++) camera.Render(); var previous = RenderTexture.active;
            try {
                RenderTexture.active = target; read.ReadPixels(new Rect(0, 0, Size, Size), 0, 0); read.Apply(false); var pixels = read.GetPixels();
                using (var file = File.Create(Path.Combine(folder, label + ".rgba-f32.gz"))) using (var zip = new GZipStream(file, CompressionMode.Compress)) using (var writer = new BinaryWriter(zip))
                    foreach (var p in pixels) { writer.Write(p.r); writer.Write(p.g); writer.Write(p.b); writer.Write(p.a); }
                var png = new Texture2D(Size, Size, TextureFormat.RGBA32, false, true);
                try { png.SetPixels(pixels); png.Apply(false); File.WriteAllBytes(Path.Combine(folder, label + ".png"), png.EncodeToPNG()); } finally { UnityEngine.Object.DestroyImmediate(png); }
                ValidateReadback(pixels, folder, label);
                return pixels;
            } finally { RenderTexture.active = previous; }
        }
        [Serializable] sealed class ReadbackHealth
        {
            public string label;
            public bool finite, allCDCD;
            public float minR, maxR, minG, maxG, minB, maxB, minA, maxA;
        }
        static void ValidateReadback(Color[] pixels, string folder, string label)
        {
            var h = new ReadbackHealth { label = label, finite = true, allCDCD = true,
                minR = float.PositiveInfinity, minG = float.PositiveInfinity,
                minB = float.PositiveInfinity, minA = float.PositiveInfinity,
                maxR = float.NegativeInfinity, maxG = float.NegativeInfinity,
                maxB = float.NegativeInfinity, maxA = float.NegativeInfinity };
            foreach (Color p in pixels)
            {
                h.minR = Mathf.Min(h.minR, p.r); h.maxR = Mathf.Max(h.maxR, p.r);
                h.minG = Mathf.Min(h.minG, p.g); h.maxG = Mathf.Max(h.maxG, p.g);
                h.minB = Mathf.Min(h.minB, p.b); h.maxB = Mathf.Max(h.maxB, p.b);
                h.minA = Mathf.Min(h.minA, p.a); h.maxA = Mathf.Max(h.maxA, p.a);
                for (int i = 0; i < 4; ++i)
                {
                    h.finite &= !float.IsNaN(p[i]) && !float.IsInfinity(p[i]);
                    h.allCDCD &= p[i] == -23.203125f;
                }
            }
            // Always preserve the original capture and health before failing.
            File.WriteAllText(Path.Combine(folder, label + "-readback-health.json"), JsonUtility.ToJson(h, true));
            Assert.That(h.finite, Is.True, label + ": nonfinite raw readback.");
            Assert.That(h.allCDCD, Is.False, label + ": every channel equals half 0xCDCD; prior batch logged DXGI device removal. Invalid readback cannot prove parity.");
        }
        static float Delta(Color[] a, Color[] b) { float d = 0; for (int y = Min; y < Max; y++) for (int x = Min; x < Max; x++) for (int c = 0; c < 4; c++) d = Mathf.Max(d, Mathf.Abs(a[y * Size + x][c] - b[y * Size + x][c])); return d; }
        static int Changed(Color[] a, Color[] b) { int n = 0; for (int y = Min; y < Max; y++) for (int x = Min; x < Max; x++) { bool change = false; for (int c = 0; c < 4; c++) change |= a[y * Size + x][c] != b[y * Size + x][c]; if (change) n++; } return n; }
        static int Visible(Color[] a) { int n = 0; for (int y = Min; y < Max; y++) for (int x = Min; x < Max; x++) if (Mathf.Max(a[y * Size + x].r, Mathf.Max(a[y * Size + x].g, a[y * Size + x].b)) > .15f) n++; return n; }
        static bool Finite(IEnumerable<Color[]> frames) { foreach (var frame in frames) foreach (var p in frame) for (int c = 0; c < 4; c++) if (float.IsNaN(p[c]) || float.IsInfinity(p[c])) return false; return true; }
    }
}
