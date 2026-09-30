using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace NBFX.Baseline.Tests
{
    /// <summary>
    /// PN1 ordinary Mesh only. Procedural Simple/Voronoi noise is consumed by
    /// Mask or Dissolve before alpha resolution. PN distortion and VFX are not
    /// tested. A=G0 Frozen, B=current ShaderLab, C=actual Graph. Strict zero
    /// assertions are intentionally unchanged; captures precede assertions.
    /// </summary>
    public sealed class G4GraphPNoiseTests
    {
        const string FrozenPath = "Packages/com.xuanxuan.nb.fx/Tests/Baseline/Frozen/NBShaders2/Shader/NBShader.shader";
        const string CurrentPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader";
        const string GraphPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph";
        const int Size = 128, RoiMin = 32, RoiMax = 96, Layer = 2;
        const int DepthLayer = G2DepthOnlyABTests.ForegroundLayer;

        [OneTimeSetUp] public void ReimportGraphAfterAssemblyReload() =>
            AssetDatabase.ImportAsset(GraphPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

        [Serializable] sealed class State
        {
            public string kind, consumer, route;
            public int consumerBlend, baseBlend;
            public bool enabled, simple, voronoi;
            public float baseOpacity = .75f, consumerOpacity = .9f;
        }
        [Serializable] sealed class Metrics
        {
            public string caseId, stage, unityVersion, api, note;
            public State input, control;
            public bool finite;
            public int roiPixels, abDifferent, bcDifferent, abControlDifferent, bcControlDifferent;
            public int visibleA, visibleB, visibleC, changedA, changedB, changedC;
            public float abMax, bcMax, abControlMax, bcControlMax;
            public float repeatA, repeatB, repeatC, responseA, responseB, responseC;
            public float baseBlendResponseA, baseBlendResponseB, baseBlendResponseC;
            public float baseBlendABMax, baseBlendBCMax;
            public int baseBlendChangedA, baseBlendChangedB, baseBlendChangedC;
            public int directedRecordedFrames, directedValidFrames;
        }
        static IEnumerable<TestCaseData> SurfaceCases()
        {
            foreach (string consumer in new[] { "mask", "dissolve" })
                foreach (string kind in new[] { "off", "simple", "voronoi", "both" })
                    foreach (int blend in new[] { 0, 1, 2, 3, 4, 5 })
                        foreach (bool ortho in new[] { true, false })
                            yield return new TestCaseData(consumer, kind, blend, "uv0", ortho)
                                .SetName("G4PNoiseABC_surface_" + consumer + "_" + kind + "_blend" + blend + (ortho ? "_ortho" : "_perspective"));
            foreach (string consumer in new[] { "mask", "dissolve" })
                foreach (string route in new[] { "uv0zw", "uv1", "uv2", "twirl", "shared-uv2" })
                    foreach (bool ortho in new[] { true, false })
                        yield return new TestCaseData(consumer, "both", 1, route, ortho)
                            .SetName("G4PNoiseABC_route_" + consumer + "_" + route + (ortho ? "_ortho" : "_perspective"));
        }
        [TestCaseSource(nameof(SurfaceCases))]
        public void SurfaceMaskDissolveMatchesFrozenAndGraph(string consumer, string kind,
            int blend, string route, bool ortho)
        {
            RequirePipeline();
            string id = "surface-" + consumer + "-" + kind + "-" + blend + "-" + route + (ortho ? "-ortho" : "-perspective");
            string output = Output(id);
            Material a = new Material(Load(FrozenPath)), b = new Material(Load(CurrentPath)), c = new Material(Load(GraphPath));
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var camObject = new GameObject("PN1 surface camera"); var camera = camObject.AddComponent<Camera>();
            camObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
            var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            var readback = new Texture2D(Size, Size, TextureFormat.RGBAHalf, false, true);
            var map = Map(); var mesh = UnityEngine.Object.Instantiate(go.GetComponent<MeshFilter>().sharedMesh);
            RenderTexture old = RenderTexture.active;
            try
            {
                SetDistinctStreams(mesh); go.GetComponent<MeshFilter>().sharedMesh = mesh;
                go.layer = Layer; go.transform.localScale = new Vector3(2, 2, 1);
                var renderer = go.GetComponent<MeshRenderer>();
                renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                foreach (var m in new[] { a, b, c }) Configure(m, m == c, consumer, map, false);
                camera.orthographic = ortho; camera.orthographicSize = 1.5f; camera.fieldOfView = 45;
                camera.nearClipPlane = .1f; camera.farClipPlane = 20;
                camera.transform.position = new Vector3(0, 0, 5);
                camera.transform.rotation = Quaternion.Euler(0, 180, 0);
                camera.cullingMask = 1 << Layer; camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.125f, .25f, .375f, .5f);
                camera.allowHDR = true; camera.allowMSAA = false; camera.targetTexture = target;
                target.Create(); Assert.That(target.IsCreated() && !target.sRGB, Is.True);
                var input = MakeState(consumer, kind, blend, route);
                var control = Control(input);
                Apply(a, b, c, input);
                Color[] aa = Draw(renderer, a, camera, target, readback, output, "A");
                Color[] ar = Draw(renderer, a, camera, target, readback, output, "A-repeat");
                Color[] ba = Draw(renderer, b, camera, target, readback, output, "B");
                Color[] br = Draw(renderer, b, camera, target, readback, output, "B-repeat");
                Color[] ca = Draw(renderer, c, camera, target, readback, output, "C");
                Color[] cr = Draw(renderer, c, camera, target, readback, output, "C-repeat");
                // FindPass takes ShaderLab Name, not the LightMode tag. Query
                // after actual warm render, not SG importer placeholder state.
                Assert.That(a.FindPass("UniversalForward"), Is.GreaterThanOrEqualTo(0));
                Assert.That(b.FindPass("UniversalForward"), Is.GreaterThanOrEqualTo(0));
                Assert.That(c.FindPass("Universal Forward"), Is.GreaterThanOrEqualTo(0));
                Apply(a, b, c, control);
                Color[] ac = Draw(renderer, a, camera, target, readback, output, "A-control");
                Color[] bc = Draw(renderer, b, camera, target, readback, output, "B-control");
                Color[] cc = Draw(renderer, c, camera, target, readback, output, "C-control");
                var mtr = Compare(id, "Forward", input, control, aa, ba, ca, ac, bc, cc, ar, br, cr);
                bool checkBaseBlend = kind == "both" && blend >= 1 && blend <= 3;
                if (checkBaseBlend)
                {
                    State alternate = MakeState(consumer, kind, blend, route);
                    alternate.baseBlend = 0;
                    Apply(a, b, c, alternate);
                    Color[] ax = Draw(renderer, a, camera, target, readback, output, "A-baseblend0");
                    Color[] bx = Draw(renderer, b, camera, target, readback, output, "B-baseblend0");
                    Color[] cx = Draw(renderer, c, camera, target, readback, output, "C-baseblend0");
                    mtr.finite &= Finite(ax, bx, cx);
                    mtr.baseBlendABMax = Delta(ax, bx);
                    mtr.baseBlendBCMax = Delta(bx, cx);
                    mtr.baseBlendResponseA = Delta(aa, ax);
                    mtr.baseBlendResponseB = Delta(ba, bx);
                    mtr.baseBlendResponseC = Delta(ca, cx);
                    mtr.baseBlendChangedA = Changed(aa, ax);
                    mtr.baseBlendChangedB = Changed(ba, bx);
                    mtr.baseBlendChangedC = Changed(ca, cx);
                }
                Save(output, mtr);
                AssertStrict(mtr, .01f, 64);
                if (checkBaseBlend)
                {
                    Assert.That(mtr.baseBlendABMax, Is.Zero);
                    Assert.That(mtr.baseBlendBCMax, Is.Zero);
                    Assert.That(mtr.baseBlendResponseA, Is.GreaterThan(.005f));
                    Assert.That(mtr.baseBlendResponseB, Is.GreaterThan(.005f));
                    Assert.That(mtr.baseBlendResponseC, Is.GreaterThan(.005f));
                    Assert.That(mtr.baseBlendChangedA, Is.GreaterThan(16));
                    Assert.That(mtr.baseBlendChangedB, Is.GreaterThan(16));
                    Assert.That(mtr.baseBlendChangedC, Is.GreaterThan(16));
                }
            }
            finally
            {
                camera.targetTexture = null; RenderTexture.active = old; target.Release();
                foreach (var obj in new UnityEngine.Object[] { a, b, c, go, camObject, target, readback, map, mesh })
                    UnityEngine.Object.DestroyImmediate(obj);
            }
        }

        static IEnumerable<TestCaseData> DepthCases()
        {
            foreach (string consumer in new[] { "mask", "dissolve" })
                foreach (string kind in new[] { "simple", "voronoi", "both" })
                    foreach (bool ortho in new[] { true, false })
                        yield return new TestCaseData(consumer, kind, ortho)
                            .SetName("G4PNoiseABC_depth_" + consumer + "_" + kind + (ortho ? "_ortho" : "_perspective"));
        }
        [TestCaseSource(nameof(DepthCases))]
        public void DirectedDepthOnlyMaskDissolveMatches(string consumer, string kind, bool ortho)
        {
            RequirePipeline();
            Assert.That(GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>().enableRenderCompatibilityMode, Is.False);
            var pipeline = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            var data = pipeline.rendererDataList[0]; Assert.That(data, Is.Not.Null);
            string assetFile = Path.Combine(Path.GetDirectoryName(Application.dataPath), AssetDatabase.GetAssetPath(data));
            byte[] assetBefore = File.ReadAllBytes(assetFile);
            string output = Output("depth-" + consumer + "-" + kind + (ortho ? "-ortho" : "-perspective"));
            Material a = new Material(Load(FrozenPath)), b = new Material(Load(CurrentPath)), c = new Material(Load(GraphPath));
            var probe = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            var writerObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var probeObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var camObject = new GameObject("PN1 directed depth camera"); var camera = camObject.AddComponent<Camera>();
            camObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
            var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            var readback = new Texture2D(Size, Size, TextureFormat.RGBAHalf, false, true);
            var map = Map(); var old = RenderTexture.active;
            G2DirectedDepthOnlyFeature directed = null; Material post = null; int oldPost = 0;
            try
            {
                Assert.That(probe.shader && probe.shader.isSupported, Is.True);
                foreach (var m in new[] { a, b, c }) Configure(m, m == c, consumer, map, true);
                writerObject.layer = DepthLayer; writerObject.transform.position = new Vector3(0, 0, 2);
                writerObject.transform.localScale = new Vector3(1.2f, 1.2f, 1);
                var writer = writerObject.GetComponent<MeshRenderer>(); writer.shadowCastingMode = ShadowCastingMode.Off;
                probeObject.layer = Layer; probeObject.transform.position = new Vector3(0, 0, 1);
                probeObject.transform.localScale = new Vector3(3, 3, 1);
                probe.SetColor("_BaseColor", Color.green); probe.SetFloat("_Surface", 1);
                probe.SetFloat("_SrcBlend", (float)BlendMode.One); probe.SetFloat("_DstBlend", (float)BlendMode.Zero);
                probe.SetFloat("_ZWrite", 0); probe.SetFloat("_Cull", 0); probe.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                probe.renderQueue = 3000;
                var probeRenderer = probeObject.GetComponent<MeshRenderer>();
                probeRenderer.sharedMaterial = probe; probeRenderer.shadowCastingMode = ShadowCastingMode.Off;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.125f, .25f, .375f, 1);
                camera.orthographic = ortho; camera.orthographicSize = 1; camera.fieldOfView = 45;
                camera.nearClipPlane = .1f; camera.farClipPlane = 10; camera.allowHDR = false; camera.allowMSAA = false;
                camera.cullingMask = (1 << DepthLayer) | (1 << Layer);
                camera.transform.position = new Vector3(0, 0, 3); camera.transform.rotation = Quaternion.Euler(0, 180, 0);
                camera.targetTexture = target; target.filterMode = FilterMode.Point; target.Create();
                directed = ScriptableObject.CreateInstance<G2DirectedDepthOnlyFeature>();
                directed.hideFlags = HideFlags.HideAndDontSave; directed.targetCamera = camera;
                directed.Create(); directed.SetActive(true); data.rendererFeatures.Add(directed); data.SetDirty();
                foreach (var f in data.rendererFeatures)
                    if (f != null && f.GetType().FullName == "NBShader.NBPostProcess" && f.isActive)
                    { post = f.GetType().GetField("NBPostProcessMaterial", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) as Material; break; }
                if (post != null && post.HasProperty("_NBPostProcessFlags"))
                { oldPost = post.GetInteger("_NBPostProcessFlags"); post.SetInteger("_NBPostProcessFlags", 0); }
                State input = MakeState(consumer, kind, 1, "uv0"), control = Control(input);
                Apply(a, b, c, input);
                var aa = DepthDraw(writer, a, camera, target, readback, output, "A");
                var ar = DepthDraw(writer, a, camera, target, readback, output, "A-repeat");
                var ba = DepthDraw(writer, b, camera, target, readback, output, "B");
                var br = DepthDraw(writer, b, camera, target, readback, output, "B-repeat");
                var ca = DepthDraw(writer, c, camera, target, readback, output, "C");
                var cr = DepthDraw(writer, c, camera, target, readback, output, "C-repeat");
                // FindPass takes ShaderLab Name, not the LightMode tag. Query
                // after actual warm render, not SG importer placeholder state.
                Assert.That(a.FindPass("DepthOnly"), Is.GreaterThanOrEqualTo(0));
                Assert.That(b.FindPass("DepthOnly"), Is.GreaterThanOrEqualTo(0));
                Assert.That(c.FindPass("DepthOnly"), Is.GreaterThanOrEqualTo(0));
                Apply(a, b, c, control);
                var ac = DepthDraw(writer, a, camera, target, readback, output, "A-control");
                var bc = DepthDraw(writer, b, camera, target, readback, output, "B-control");
                var cc = DepthDraw(writer, c, camera, target, readback, output, "C-control");
                var metrics = Compare("depth-" + consumer + "-" + kind + (ortho ? "-ortho" : "-perspective"),
                    "directed DepthOnly", input, control, aa, ba, ca, ac, bc, cc, ar, br, cr);
                metrics.directedRecordedFrames = directed.recordedFrames;
                metrics.directedValidFrames = directed.validDepthFrames;
                Save(output, metrics);
                Assert.That(metrics.directedRecordedFrames, Is.GreaterThan(0));
                Assert.That(metrics.directedValidFrames, Is.EqualTo(metrics.directedRecordedFrames));
                AssertStrict(metrics, .1f, 64);
            }
            finally
            {
                if (post != null && post.HasProperty("_NBPostProcessFlags")) post.SetInteger("_NBPostProcessFlags", oldPost);
                if (directed != null) { data.rendererFeatures.Remove(directed); data.SetDirty(); UnityEngine.Object.DestroyImmediate(directed); }
                camera.targetTexture = null; RenderTexture.active = old; target.Release();
                foreach (var obj in new UnityEngine.Object[] { a, b, c, probe, writerObject, probeObject, camObject, target, readback, map })
                    UnityEngine.Object.DestroyImmediate(obj);
                Assert.That(File.ReadAllBytes(assetFile), Is.EqualTo(assetBefore), "Renderer asset changed on disk.");
            }
        }

        static IEnumerable<TestCaseData> ShadowCases()
        {
            foreach (string consumer in new[] { "mask", "dissolve" })
                foreach (string kind in new[] { "simple", "voronoi", "both" })
                    foreach (bool ortho in new[] { true, false })
                        yield return new TestCaseData(consumer, kind, ortho)
                            .SetName("G4PNoiseABC_shadow_" + consumer + "_" + kind + (ortho ? "_ortho" : "_perspective"));
        }
        [TestCaseSource(nameof(ShadowCases))]
        public void RealMainLightShadowMaskDissolveMatches(string consumer, string kind, bool ortho)
        {
            RequirePipeline();
            var pipeline = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            Assert.That(pipeline.supportsMainLightShadows && pipeline.shadowDistance > 10, Is.True);
            Assert.That(QualitySettings.shadows, Is.Not.EqualTo(UnityEngine.ShadowQuality.Disable));
            string output = Output("shadow-" + consumer + "-" + kind + (ortho ? "-ortho" : "-perspective"));
            Material a = new Material(Load(FrozenPath)), b = new Material(Load(CurrentPath)), c = new Material(Load(GraphPath));
            var receiverMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            var caster = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var receiver = GameObject.CreatePrimitive(PrimitiveType.Plane);
            var lightObject = new GameObject("PN1 main light"); var light = lightObject.AddComponent<Light>();
            var camObject = new GameObject("PN1 shadow camera"); var camera = camObject.AddComponent<Camera>();
            camObject.AddComponent<UniversalAdditionalCameraData>().renderShadows = true;
            var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            var readback = new Texture2D(Size, Size, TextureFormat.RGBAHalf, false, true);
            var map = Map(); var old = RenderTexture.active; var oldSun = RenderSettings.sun;
            var oldAmbientMode = RenderSettings.ambientMode; var oldAmbient = RenderSettings.ambientLight;
            bool oldFog = RenderSettings.fog;
            try
            {
                Assert.That(receiverMaterial.shader && receiverMaterial.shader.isSupported, Is.True);
                foreach (var m in new[] { a, b, c }) Configure(m, m == c, consumer, map, true);
                receiverMaterial.SetColor("_BaseColor", Color.white);
                receiverMaterial.DisableKeyword("_RECEIVE_SHADOWS_OFF");
                receiver.layer = Layer; receiver.transform.position = Vector3.zero;
                receiver.transform.localScale = new Vector3(.55f, 1, .55f);
                var receiverRenderer = receiver.GetComponent<MeshRenderer>();
                receiverRenderer.sharedMaterial = receiverMaterial;
                receiverRenderer.shadowCastingMode = ShadowCastingMode.Off;
                caster.layer = Layer; caster.transform.position = new Vector3(0, 1, 0);
                caster.transform.rotation = Quaternion.Euler(-90, 0, 0);
                caster.transform.localScale = new Vector3(1.5f, 1.5f, 1);
                var casterRenderer = caster.GetComponent<MeshRenderer>(); casterRenderer.shadowCastingMode = ShadowCastingMode.On;
                light.type = LightType.Directional; light.shadows = LightShadows.Hard;
                light.shadowStrength = 1; light.intensity = 2; light.color = Color.white;
                light.cullingMask = 1 << Layer; light.transform.rotation = Quaternion.Euler(50, -30, 0);
                RenderSettings.sun = light; RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = Color.black; RenderSettings.fog = false;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.08f, .13f, .19f, 1);
                camera.orthographic = ortho; camera.orthographicSize = 3.5f; camera.fieldOfView = 45;
                camera.nearClipPlane = .1f; camera.farClipPlane = 25; camera.allowHDR = false; camera.allowMSAA = false;
                camera.cullingMask = 1 << Layer;
                camera.transform.position = new Vector3(0, 4.5f, -5.5f); camera.transform.LookAt(Vector3.zero);
                camera.targetTexture = target; target.filterMode = FilterMode.Point; target.Create();
                State input = MakeState(consumer, kind, 1, "uv0"), control = Control(input);
                Apply(a, b, c, input);
                var aa = ShadowDraw(casterRenderer, a, camera, target, readback, output, "A");
                var ar = ShadowDraw(casterRenderer, a, camera, target, readback, output, "A-repeat");
                var ba = ShadowDraw(casterRenderer, b, camera, target, readback, output, "B");
                var br = ShadowDraw(casterRenderer, b, camera, target, readback, output, "B-repeat");
                var ca = ShadowDraw(casterRenderer, c, camera, target, readback, output, "C");
                var cr = ShadowDraw(casterRenderer, c, camera, target, readback, output, "C-repeat");
                // FindPass takes ShaderLab Name, not the LightMode tag. Query
                // after actual warm render, not SG importer placeholder state.
                Assert.That(a.FindPass("ShadowCaster"), Is.GreaterThanOrEqualTo(0));
                Assert.That(b.FindPass("ShadowCaster"), Is.GreaterThanOrEqualTo(0));
                Assert.That(c.FindPass("ShadowCaster"), Is.GreaterThanOrEqualTo(0));
                Apply(a, b, c, control);
                var ac = ShadowDraw(casterRenderer, a, camera, target, readback, output, "A-control");
                var bc = ShadowDraw(casterRenderer, b, camera, target, readback, output, "B-control");
                var cc = ShadowDraw(casterRenderer, c, camera, target, readback, output, "C-control");
                var metrics = Compare("shadow-" + consumer + "-" + kind + (ortho ? "-ortho" : "-perspective"),
                    "real URP main-light ShadowCaster", input, control, aa, ba, ca, ac, bc, cc, ar, br, cr);
                Save(output, metrics); AssertStrict(metrics, .05f, 64);
            }
            finally
            {
                camera.targetTexture = null; RenderTexture.active = old; target.Release();
                RenderSettings.sun = oldSun; RenderSettings.ambientMode = oldAmbientMode;
                RenderSettings.ambientLight = oldAmbient; RenderSettings.fog = oldFog;
                foreach (var obj in new UnityEngine.Object[] { a, b, c, receiverMaterial, caster, receiver, lightObject, camObject, target, readback, map })
                    UnityEngine.Object.DestroyImmediate(obj);
            }
        }

        static void RequirePipeline()
        {
            Assert.That(GraphicsSettings.currentRenderPipeline, Is.InstanceOf<UniversalRenderPipelineAsset>());
            Assert.That(SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf), Is.True);
        }
        static Shader Load(string path)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            Assert.That(shader && shader.isSupported, Is.True, path);
            return shader;
        }
        static string Output(string id)
        {
            string root = Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR");
            if (string.IsNullOrEmpty(root)) root = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/NBFXG4PNoise");
            string path = Path.Combine(root, "g4-pnoise", id); Directory.CreateDirectory(path); return path;
        }
        static Texture2D Map()
        {
            const int n = 64;
            var t = new Texture2D(n, n, TextureFormat.RGBAHalf, false, true);
            var pixels = new Color[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float u = x / (n - 1f), v = y / (n - 1f);
                pixels[y * n + x] = new Color(.8f - .4f * v, .25f + .4f * u,
                    .1f + .65f * v, .45f + .45f * u);
            }
            t.SetPixels(pixels); t.Apply(false); t.wrapMode = TextureWrapMode.Repeat;
            t.filterMode = FilterMode.Bilinear; return t;
        }
        static void SetDistinctStreams(Mesh mesh)
        {
            mesh.SetUVs(0, new List<Vector4> {
                new Vector4(0, 0, .14f, .83f), new Vector4(1, 0, .84f, .19f),
                new Vector4(0, 1, .25f, .74f), new Vector4(1, 1, .73f, .26f) });
            mesh.SetUVs(1, new List<Vector4> {
                new Vector4(.12f, .82f, 0, 0), new Vector4(.76f, .71f, 0, 0),
                new Vector4(.19f, .18f, 0, 0), new Vector4(.88f, .22f, 0, 0) });
            mesh.SetUVs(2, new List<Vector4> {
                new Vector4(.81f, .11f, 0, 0), new Vector4(.24f, .17f, 0, 0),
                new Vector4(.75f, .85f, 0, 0), new Vector4(.16f, .78f, 0, 0) });
        }
        static State MakeState(string consumer, string kind, int blend, string route)
        {
            return new State { consumer = consumer, kind = kind, route = route,
                consumerBlend = blend, baseBlend = blend % 4,
                enabled = kind != "off", simple = kind == "simple" || kind == "both",
                voronoi = kind == "voronoi" || kind == "both" };
        }
        static State Control(State s)
        {
            var c = MakeState(s.consumer, s.kind, s.consumerBlend, s.route);
            if (s.enabled && s.consumerBlend >= 1 && s.consumerBlend <= 3)
                c.enabled = false;
            else { c.enabled = true; c.simple = true; c.voronoi = false; c.consumerBlend = 1; c.baseBlend = 0; }
            return c;
        }
        static void Configure(Material m, bool graph, string consumer, Texture2D map, bool depthShadow)
        {
            foreach (string property in new[] { "_ProgramNoise_Toggle", "_ProgramNoise_Simple_Toggle",
                "_ProgramNoise_Voronoi_Toggle", "_DissolveVoronoi_Vec", "_DissolveVoronoi_Vec2",
                "_DissolveVoronoi_Vec3", "_DissolveVoronoi_Vec4",
                "_ProgramNoiseBaseBlendOpacity", "_MaskPNoiseBlendOpacity",
                "_DissolvePNoiseBlendOpacity" })
                Assert.That(m.HasProperty(property), Is.True, "PN1 property missing: " + property);
            if (graph)
            {
                Assert.That(m.HasProperty("_NB_PNoiseBlendLo16"), Is.True);
                Assert.That(m.HasProperty("_NB_PNoiseBlendHi16"), Is.True);
            }
            else Assert.That(m.HasProperty("_W9ParticleShaderPNoiseBlendFlag"), Is.True);
            m.SetTexture("_BaseMap", Texture2D.whiteTexture);
            m.SetColor(graph ? "_Color" : "_BaseColor", Color.white);
            m.SetColor("_ColorA", Color.white); m.SetFloat("_AlphaAll", 1);
            m.SetFloat("_BaseColorIntensityForTimeline", 1);
            m.SetFloat("_Cull", 0); m.SetFloat("_ZTest", 4); m.SetFloat("_ZWrite", depthShadow ? 1 : 0);
            m.SetFloat("_Cutoff", .5f); m.SetFloat("_AlphaClip", depthShadow ? 1 : 0);
            if (depthShadow) m.EnableKeyword("_ALPHATEST_ON");
            m.SetFloat("_SrcBlend", (float)BlendMode.One); m.SetFloat("_DstBlend", (float)BlendMode.Zero);
            if (m.HasProperty("_SrcBlendAlpha")) m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            if (m.HasProperty("_DstBlendAlpha")) m.SetFloat("_DstBlendAlpha", (float)BlendMode.Zero);
            m.renderQueue = depthShadow ? 2100 : 3000;
            if (graph)
            {
                m.SetFloat("_Surface", depthShadow ? 0 : 1);
                if (!depthShadow) m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                SetWord(m, "_NB_Flags0Lo16", "_NB_Flags0Hi16", 0);
                SetWord(m, "_NB_Flags1Lo16", "_NB_Flags1Hi16", 0);
                SetWord(m, "_NB_ColorChannelLo16", "_NB_ColorChannelHi16", 3u | (3u << (consumer == "mask" ? 2 : 10)));
                m.SetFloat("_NB_DistortionMode", 0);
            }
            else
            {
                m.EnableKeyword("_FX_LIGHT_MODE_UNLIT");
                m.SetInteger("_W9ParticleShaderFlags", 0); m.SetInteger("_W9ParticleShaderFlags1", 0);
                m.SetInteger("_W9ParticleShaderColorChannelFlag", (int)(3u | (3u << (consumer == "mask" ? 2 : 10))));
                m.EnableKeyword(consumer == "mask" ? "_MASKMAP_ON" : "_DISSOLVE");
            }
            if (consumer == "mask")
            {
                m.SetFloat("_Mask_Toggle", 1); m.SetTexture("_MaskMap", map);
                m.SetVector("_MaskMapVec", new Vector4(1, 0, 0, 0));
            }
            else
            {
                m.SetFloat("_Dissolve_Toggle", 1); m.SetTexture("_DissolveMap", map);
                m.SetVector("_Dissolve", new Vector4(.48f, 1, 0, .45f));
            }
            if (m.HasProperty("_AffectsShadows")) m.SetFloat("_AffectsShadows", 1);
            if (m.HasProperty("_CastShadows")) m.SetFloat("_CastShadows", 1);
            foreach (string pass in new[] { "Universal2D",
                "NBCameraOpaqueDistortPass", "NBDeferredDistortPass" }) m.SetShaderPassEnabled(pass, false);
            // The delegated URP Unlit Forward has no explicit LightMode tag:
            // it renders through SRPDefaultUnlit. Only the old ShaderLab uses
            // UniversalForward. Do not disable Graph's actual Forward host.
            m.SetShaderPassEnabled("SRPDefaultUnlit", graph && !depthShadow);
            m.SetShaderPassEnabled("SRPDEFAULTUNLIT", graph && !depthShadow);
            m.SetShaderPassEnabled("UniversalForward", !graph && !depthShadow);
            m.SetShaderPassEnabled("DepthOnly", depthShadow);
            m.SetShaderPassEnabled("ShadowCaster", depthShadow);
        }
        static void Apply(Material a, Material b, Material c, State s)
        {
            uint packed = (uint)(s.baseBlend & 7) | ((uint)(s.consumerBlend & 7) << (s.consumer == "mask" ? 3 : 6));
            uint uvWords = 0, uvTypes = 0, flags0 = 0, flags1 = 0;
            int routeMode = 0;
            if (s.route == "uv0zw") routeMode = 1;
            else if (s.route == "uv1") { routeMode = 1; flags1 |= (1u << 21) | (1u << 18); }
            else if (s.route == "uv2") { routeMode = 1; flags1 |= (1u << 21) | (1u << 19); }
            else if (s.route == "twirl") { routeMode = 2; flags0 |= 1u << 9; }
            else if (s.route == "shared-uv2")
            { routeMode = 8; flags1 |= (1u << 21) | (1u << 19); uvWords |= 1u << 30; }
            uvWords |= (uint)(routeMode & 3) << 28;
            uvTypes |= (uint)(routeMode >> 2) << 28;
            foreach (Material m in new[] { a, b, c })
            {
                m.SetFloat("_ProgramNoise_Toggle", s.enabled ? 1 : 0);
                m.SetFloat("_ProgramNoise_Simple_Toggle", s.simple ? 1 : 0);
                m.SetFloat("_ProgramNoise_Voronoi_Toggle", s.voronoi ? 1 : 0);
                m.SetFloat("_ProgramNoise_Rotate", 21);
                m.SetVector("_DissolveVoronoi_Vec", new Vector4(3.1f, 2.7f, 2.4f, 3.3f));
                // Freeze the time axis so sequential A/B/C and repeat captures
                // test the shader, not the Editor clock.
                m.SetVector("_DissolveVoronoi_Vec2", new Vector4(1, 1, 0, 0));
                m.SetVector("_DissolveVoronoi_Vec3", Vector4.zero);
                m.SetVector("_DissolveVoronoi_Vec4", new Vector4(.17f, -.23f, -.19f, .11f));
                m.SetFloat("_ProgramNoiseBaseBlendOpacity", s.baseOpacity);
                m.SetFloat(s.consumer == "mask" ? "_MaskPNoiseBlendOpacity" : "_DissolvePNoiseBlendOpacity", s.consumerOpacity);
                m.SetVector("_SharedUV_ST", new Vector4(.91f, 1.17f, .08f, -.06f));
                m.SetVector("_SharedUV_Vec", new Vector4(0, 0, 27, 0));
                m.SetVector("_TWParameter", new Vector4(.43f, .58f, 0, 0));
                m.SetFloat("_TWStrength", 2.3f);
                if (m == c)
                {
                    SetWord(m, "_NB_PNoiseBlendLo16", "_NB_PNoiseBlendHi16", packed);
                    SetWord(m, "_NB_UVModeFlag0Lo16", "_NB_UVModeFlag0Hi16", uvWords);
                    SetWord(m, "_NB_UVModeFlagType0Lo16", "_NB_UVModeFlagType0Hi16", uvTypes);
                    SetWord(m, "_NB_Flags0Lo16", "_NB_Flags0Hi16", flags0);
                    SetWord(m, "_NB_Flags1Lo16", "_NB_Flags1Hi16", flags1);
                }
                else
                {
                    m.SetInteger("_W9ParticleShaderPNoiseBlendFlag", (int)packed);
                    m.SetInteger("_UVModeFlag0", (int)uvWords); m.SetInteger("_UVModeFlagType0", (int)uvTypes);
                    m.SetInteger("_W9ParticleShaderFlags", (int)flags0);
                    m.SetInteger("_W9ParticleShaderFlags1", (int)flags1);
                    Toggle(m, "_PROGRAM_NOISE", s.enabled);
                    Toggle(m, "_PROGRAM_NOISE_SIMPLE", s.enabled && s.simple);
                    Toggle(m, "_PROGRAM_NOISE_VORONOI", s.enabled && s.voronoi);
                    Toggle(m, "_SHARED_UV", s.route == "shared-uv2");
                }
            }
        }
        static void Toggle(Material m, string keyword, bool on)
        { if (on) m.EnableKeyword(keyword); else m.DisableKeyword(keyword); }
        static void SetWord(Material m, string lo, string hi, uint word)
        { m.SetFloat(lo, word & 65535u); if (m.HasProperty(hi)) m.SetFloat(hi, word >> 16); }
        static Color[] Draw(MeshRenderer r, Material m, Camera camera, RenderTexture rt,
            Texture2D readback, string output, string name)
        { r.sharedMaterial = m; return Capture(camera, rt, readback, Path.Combine(output, name)); }
        static Color[] DepthDraw(MeshRenderer r, Material m, Camera camera, RenderTexture rt,
            Texture2D readback, string output, string name)
        { r.sharedMaterial = m; m.SetShaderPassEnabled("DepthOnly", true); return Capture(camera, rt, readback, Path.Combine(output, name)); }
        static Color[] ShadowDraw(MeshRenderer r, Material m, Camera camera, RenderTexture rt,
            Texture2D readback, string output, string name)
        { r.sharedMaterial = m; m.SetShaderPassEnabled("ShadowCaster", true); return Capture(camera, rt, readback, Path.Combine(output, name)); }
        static Color[] Capture(Camera camera, RenderTexture rt, Texture2D readback, string path)
        {
            for (int i = 0; i < 4; i++) camera.Render();
            RenderTexture old = RenderTexture.active;
            try
            {
                RenderTexture.active = rt;
                readback.ReadPixels(new Rect(0, 0, Size, Size), 0, 0); readback.Apply(false);
                Color[] pixels = readback.GetPixels();
                using (var file = File.Create(path + ".rgba-f32.gz"))
                using (var zip = new GZipStream(file, CompressionMode.Compress))
                using (var writer = new BinaryWriter(zip))
                    foreach (Color p in pixels) { writer.Write(p.r); writer.Write(p.g); writer.Write(p.b); writer.Write(p.a); }
                var png = new Texture2D(Size, Size, TextureFormat.RGBA32, false, true);
                try { png.SetPixels(pixels); png.Apply(false); File.WriteAllBytes(path + ".png", png.EncodeToPNG()); }
                finally { UnityEngine.Object.DestroyImmediate(png); }
                return pixels;
            }
            finally { RenderTexture.active = old; }
        }
        static Metrics Compare(string id, string stage, State input, State control,
            Color[] a, Color[] b, Color[] c, Color[] ac, Color[] bc, Color[] cc,
            Color[] ar, Color[] br, Color[] cr)
        {
            var m = new Metrics { caseId = id, stage = stage, input = input, control = control,
                unityVersion = Application.unityVersion, api = SystemInfo.graphicsDeviceType.ToString(),
                note = "PN1 ordinary Mesh only; A Frozen/B ShaderLab/C Graph, 128x128 linear RGBAHalf, strict ROI 32..95; 4 warmups; no PN2 distortion/CustomData/procedural wrap modes/VFX/Player." };
            m.finite = Finite(a, b, c, ac, bc, cc, ar, br, cr);
            m.abMax = Delta(a, b, out m.abDifferent);
            m.bcMax = Delta(b, c, out m.bcDifferent);
            m.abControlMax = Delta(ac, bc, out m.abControlDifferent);
            m.bcControlMax = Delta(bc, cc, out m.bcControlDifferent);
            m.repeatA = Delta(a, ar); m.repeatB = Delta(b, br); m.repeatC = Delta(c, cr);
            m.responseA = Delta(a, ac); m.responseB = Delta(b, bc); m.responseC = Delta(c, cc);
            m.changedA = Changed(a, ac); m.changedB = Changed(b, bc); m.changedC = Changed(c, cc);
            for (int y = RoiMin; y < RoiMax; y++) for (int x = RoiMin; x < RoiMax; x++)
            {
                int i = y * Size + x; m.roiPixels++;
                if (a[i].maxColorComponent > .01f) m.visibleA++;
                if (b[i].maxColorComponent > .01f) m.visibleB++;
                if (c[i].maxColorComponent > .01f) m.visibleC++;
            }
            return m;
        }
        static void AssertStrict(Metrics m, float threshold, int pixels)
        {
            Assert.That(m.finite, Is.True, "Nonfinite raw capture.");
            Assert.That(m.roiPixels, Is.EqualTo(4096));
            Assert.That(m.visibleA, Is.GreaterThan(256));
            Assert.That(m.visibleB, Is.GreaterThan(256));
            Assert.That(m.visibleC, Is.GreaterThan(256));
            Assert.That(m.repeatA, Is.Zero); Assert.That(m.repeatB, Is.Zero); Assert.That(m.repeatC, Is.Zero);
            Assert.That(m.responseA, Is.GreaterThan(threshold));
            Assert.That(m.responseB, Is.GreaterThan(threshold));
            Assert.That(m.responseC, Is.GreaterThan(threshold));
            Assert.That(m.changedA, Is.GreaterThan(pixels));
            Assert.That(m.changedB, Is.GreaterThan(pixels));
            Assert.That(m.changedC, Is.GreaterThan(pixels));
            Assert.That(m.abMax, Is.Zero); Assert.That(m.abControlMax, Is.Zero);
            Assert.That(m.bcMax, Is.Zero); Assert.That(m.bcControlMax, Is.Zero);
        }
        static bool Finite(params Color[][] frames)
        {
            foreach (var f in frames) foreach (var p in f)
                for (int channel = 0; channel < 4; channel++)
                    if (float.IsNaN(p[channel]) || float.IsInfinity(p[channel])) return false;
            return true;
        }
        static float Delta(Color[] a, Color[] b) => Delta(a, b, out _);
        static float Delta(Color[] a, Color[] b, out int changed)
        {
            float max = 0; changed = 0;
            for (int y = RoiMin; y < RoiMax; y++) for (int x = RoiMin; x < RoiMax; x++)
            {
                int i = y * Size + x; float d = 0;
                for (int ch = 0; ch < 4; ch++) d = Mathf.Max(d, Mathf.Abs(a[i][ch] - b[i][ch]));
                if (d > 0) changed++; max = Mathf.Max(max, d);
            }
            return max;
        }
        static int Changed(Color[] a, Color[] b)
        {
            int count = 0;
            for (int y = RoiMin; y < RoiMax; y++) for (int x = RoiMin; x < RoiMax; x++)
            {
                int i = y * Size + x; float d = 0;
                for (int ch = 0; ch < 4; ch++) d = Mathf.Max(d, Mathf.Abs(a[i][ch] - b[i][ch]));
                if (d > .01f) count++;
            }
            return count;
        }
        static void Save(string path, Metrics metrics)
        {
            File.WriteAllText(Path.Combine(path, "metrics.json"), JsonUtility.ToJson(metrics, true));
            Debug.Log("NBFX_G4_PNOISE_ABC " + JsonUtility.ToJson(metrics));
        }
    }
}
