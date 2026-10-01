using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace NBFX.Baseline.Tests
{
    // POM1: ordinary Mesh main Forward/backface only. No VFX/Player claim.
    // Raw A/B/C and controls are saved before strict assertions.
    public sealed class G4GraphParallaxTests
    {
        const string FrozenPath = "Packages/com.xuanxuan.nb.fx/Tests/Baseline/Frozen/NBShaders2/Shader/NBShader.shader";
        const string CurrentPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader";
        const string GraphPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph";
        const int Size = 128, Layer = 4, Min = 28, Max = 100;
        static readonly string[] Names = { "on", "off", "intensity-zero", "low-layers", "high-layers",
            "wrap-repeat", "wrap-clamp", "wrap-repeatU-clampV", "wrap-clampU-repeatV",
            "lod-auto", "lod-zero", "negative-scale", "backface" };

        [Serializable] sealed class State
        {
            public bool enabled = true, lod0 = true;
            public float intensity = .16f, minLayers = 5, maxLayers = 30;
            public int wrap;
            public Vector4 st = new Vector4(1.8f, 1.65f, -.38f, -.24f);
        }
        [Serializable] sealed class Metrics
        {
            public string name, api, unityVersion, note;
            public bool ortho, finite;
            public State primary, control;
            public int abPixels, bcPixels, abControlPixels, bcControlPixels,
                aVisible, bVisible, cVisible, aResponsePixels, bResponsePixels, cResponsePixels;
            public float abMax, bcMax, abControlMax, bcControlMax,
                aRepeat, bRepeat, cRepeat, aControlRepeat, bControlRepeat, cControlRepeat,
                aResponse, bResponse, cResponse;
        }
        [OneTimeSetUp]
        public void ImportGraphAfterReload() => AssetDatabase.ImportAsset(GraphPath,
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        static IEnumerable<TestCaseData> Cases()
        {
            foreach (string name in Names)
                foreach (bool ortho in new[] { true, false })
                    yield return new TestCaseData(name, ortho).SetName(
                        "G4ParallaxABC_" + name + (ortho ? "_ortho" : "_perspective"));
        }
        [TestCaseSource(nameof(Cases))]
        public void ForwardParallaxMatchesFrozenAndGraph(string name, bool ortho)
        {
            Assert.That(GraphicsSettings.currentRenderPipeline, Is.InstanceOf<UniversalRenderPipelineAsset>());
            Assert.That(SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf), Is.True);
            var rendererData = ((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline).rendererDataList[0]
                as UniversalRendererData;
            Assert.That(rendererData, Is.Not.Null);
            Assert.That(rendererData.transparentLayerMask.value & (1 << Layer), Is.Not.Zero);
            string rendererPath = Path.Combine(Path.GetDirectoryName(Application.dataPath),
                AssetDatabase.GetAssetPath(rendererData));
            byte[] rendererBefore = File.ReadAllBytes(rendererPath);
            Shader fs = AssetDatabase.LoadAssetAtPath<Shader>(FrozenPath),
                bs = AssetDatabase.LoadAssetAtPath<Shader>(CurrentPath),
                gs = AssetDatabase.LoadAssetAtPath<Shader>(GraphPath);
            Assert.That(fs && bs && gs && fs.isSupported && bs.isSupported && gs.isSupported, Is.True);
            Assert.That(fs.name, Is.EqualTo("Effects/NBShader_T00_Frozen"));
            string root = Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR");
            if (string.IsNullOrEmpty(root)) root = Path.Combine(
                Path.GetDirectoryName(Application.dataPath), "Temp/NBFXG4Parallax");
            string folder = Path.Combine(root, "g4-parallax", name + (ortho ? "-ortho" : "-perspective"));
            Directory.CreateDirectory(folder);
            Scene scene = EditorSceneManager.NewPreviewScene();
            var actor = new GameObject("POM1 curved Mesh", typeof(MeshFilter), typeof(MeshRenderer));
            var cameraObject = new GameObject("POM1 camera");
            Mesh mesh = BuildMesh();
            var frozen = new Material(fs); var current = new Material(bs); var graph = new Material(gs);
            Texture2D baseMap = MakeBaseMap(), height = MakeHeightMap();
            var rt = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGBHalf,
                RenderTextureReadWrite.Linear);
            var readback = new Texture2D(Size, Size, TextureFormat.RGBAHalf, false, true);
            Camera camera = cameraObject.AddComponent<Camera>();
            RenderTexture previous = RenderTexture.active;
            try
            {
                SceneManager.MoveGameObjectToScene(actor, scene);
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                actor.layer = Layer;
                actor.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = actor.GetComponent<MeshRenderer>();
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                actor.transform.rotation = name == "backface" ? Quaternion.Euler(0, 205, 0) :
                    Quaternion.Euler(11, 39, 4);
                actor.transform.localScale = name == "negative-scale" ?
                    new Vector3(-1.3f, 1.45f, .75f) : new Vector3(1.3f, .9f, 1.2f);
                camera.scene = scene; camera.orthographic = ortho;
                camera.orthographicSize = 1.7f; camera.fieldOfView = 48;
                camera.nearClipPlane = .1f; camera.farClipPlane = 20;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.11f, .17f, .23f, .35f);
                camera.allowHDR = true; camera.allowMSAA = false;
                camera.cullingMask = 1 << Layer; camera.targetTexture = rt;
                camera.transform.position = new Vector3(0, 0, 5);
                camera.transform.rotation = Quaternion.Euler(0, 180, 0);
                cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
                rt.Create(); Assert.That(rt.IsCreated() && !rt.sRGB, Is.True);
                Configure(frozen, false, name == "backface", baseMap, height);
                Configure(current, false, name == "backface", baseMap, height);
                Configure(graph, true, name == "backface", baseMap, height);
                renderer.sharedMaterial = graph;
                Capture(camera, rt, readback, Path.Combine(folder, "C-warm"));
                // A just-imported Graph can expose a placeholder until its first real draw.
                Configure(graph, true, name == "backface", baseMap, height);
                foreach (string property in new[] { "_ParallaxMapping_Toggle", "_ParallaxMapping_Map",
                    "_ParallaxMapping_Intensity", "_ParallaxMapping_Vec" })
                    Assert.That(graph.HasProperty(property), Is.True,
                        "Graph property missing after real warm draw: " + property);
                State primary = MakeState(name), control = MakeControl(name, primary);
                Color[] Snap(Material material, State state, string label)
                {
                    Apply(material, material == graph, state);
                    renderer.sharedMaterial = material;
                    return Capture(camera, rt, readback, Path.Combine(folder, label));
                }
                Color[] a = Snap(frozen, primary, "A-frozen");
                Color[] ar = Snap(frozen, primary, "A-repeat");
                Color[] b = Snap(current, primary, "B-current");
                Color[] br = Snap(current, primary, "B-repeat");
                Color[] c = Snap(graph, primary, "C-graph");
                Color[] cr = Snap(graph, primary, "C-repeat");
                Color[] ac = Snap(frozen, control, "A-control");
                Color[] acr = Snap(frozen, control, "A-control-repeat");
                Color[] bc = Snap(current, control, "B-control");
                Color[] bcr = Snap(current, control, "B-control-repeat");
                Color[] cc = Snap(graph, control, "C-control");
                Color[] ccr = Snap(graph, control, "C-control-repeat");
                var ab = Compare(a, b); var graphParity = Compare(b, c);
                var abControl = Compare(ac, bc); var graphControl = Compare(bc, cc);
                var aDelta = Compare(a, ac); var bDelta = Compare(b, bc);
                var cDelta = Compare(c, cc);
                var m = new Metrics {
                    name = name, ortho = ortho, api = SystemInfo.graphicsDeviceType.ToString(),
                    unityVersion = Application.unityVersion, primary = primary, control = control,
                    note = "POM1 ordinary Mesh Forward/backface only. A Frozen/B current/C Graph, 128x128 linear RGBAHalf, ROI 28..99. Raw/full-frame PNG and gzip f32; all compares strict0. No VFX, Player, CustomLocal, CA or cross-feature claim.",
                    abPixels = ab.changed, bcPixels = graphParity.changed,
                    abControlPixels = abControl.changed, bcControlPixels = graphControl.changed,
                    abMax = ab.max, bcMax = graphParity.max,
                    abControlMax = abControl.max, bcControlMax = graphControl.max,
                    aVisible = Visible(a), bVisible = Visible(b), cVisible = Visible(c),
                    aRepeat = Compare(a, ar).max, bRepeat = Compare(b, br).max,
                    cRepeat = Compare(c, cr).max,
                    aControlRepeat = Compare(ac, acr).max,
                    bControlRepeat = Compare(bc, bcr).max,
                    cControlRepeat = Compare(cc, ccr).max,
                    aResponse = aDelta.max, bResponse = bDelta.max, cResponse = cDelta.max,
                    aResponsePixels = aDelta.changed, bResponsePixels = bDelta.changed,
                    cResponsePixels = cDelta.changed,
                    finite = Finite(a, ar, b, br, c, cr, ac, acr, bc, bcr, cc, ccr)
                };
                File.WriteAllText(Path.Combine(folder, "metrics.json"), JsonUtility.ToJson(m, true));
                Debug.Log("NBFX_G4_PARALLAX_ABC " + JsonUtility.ToJson(m));
                Assert.That(m.finite, Is.True);
                Assert.That(m.aVisible, Is.GreaterThan(300));
                Assert.That(m.bVisible, Is.GreaterThan(300));
                Assert.That(m.cVisible, Is.GreaterThan(300));
                Assert.That(m.aRepeat + m.bRepeat + m.cRepeat +
                    m.aControlRepeat + m.bControlRepeat + m.cControlRepeat, Is.Zero);
                Assert.That(m.aResponse, Is.GreaterThan(.01f));
                Assert.That(m.bResponse, Is.GreaterThan(.01f));
                Assert.That(m.cResponse, Is.GreaterThan(.01f));
                Assert.That(m.aResponsePixels, Is.GreaterThan(48));
                Assert.That(m.bResponsePixels, Is.GreaterThan(48));
                Assert.That(m.cResponsePixels, Is.GreaterThan(48));
                Assert.That(m.abPixels + m.abControlPixels, Is.Zero, "Frozen/current changed by extraction.");
                Assert.That(m.bcPixels + m.bcControlPixels, Is.Zero, "Current/Graph POM mismatch.");
            }
            finally
            {
                camera.targetTexture = null; RenderTexture.active = previous;
                rt.Release();
                foreach (var obj in new UnityEngine.Object[] { mesh, frozen, current, graph,
                    baseMap, height, rt, readback }) UnityEngine.Object.DestroyImmediate(obj);
                EditorSceneManager.ClosePreviewScene(scene);
                Assert.That(File.ReadAllBytes(rendererPath), Is.EqualTo(rendererBefore),
                    "Renderer asset mutated by POM fixture.");
            }
        }
        static State MakeState(string name)
        {
            var s = new State();
            if (name == "off") s.enabled = false;
            if (name == "intensity-zero") s.intensity = 0;
            if (name == "low-layers") { s.minLayers = 2; s.maxLayers = 3; }
            if (name == "high-layers") { s.minLayers = 16; s.maxLayers = 40; }
            if (name == "wrap-clamp") s.wrap = 1;
            if (name == "wrap-repeatU-clampV") s.wrap = 2;
            if (name == "wrap-clampU-repeatV") s.wrap = 3;
            if (name == "lod-auto") s.lod0 = false;
            return s;
        }
        static State MakeControl(string name, State s)
        {
            var c = new State { enabled = s.enabled, lod0 = s.lod0,
                intensity = s.intensity, minLayers = s.minLayers,
                maxLayers = s.maxLayers, wrap = s.wrap, st = s.st };
            if (name == "off" || name == "intensity-zero")
            { c.enabled = true; c.intensity = .16f; }
            else if (name.Contains("layers"))
            { c.minLayers = s.minLayers == 2 ? 16 : 2; c.maxLayers = s.maxLayers == 3 ? 40 : 3; }
            else if (name.StartsWith("wrap-", StringComparison.Ordinal)) c.wrap = s.wrap == 0 ? 1 : 0;
            else if (name.StartsWith("lod-", StringComparison.Ordinal)) c.lod0 = !s.lod0;
            else c.enabled = false;
            return c;
        }
        static void Configure(Material m, bool graph, bool backface, Texture2D baseMap, Texture2D height)
        {
            m.shaderKeywords = graph ? new[] { "_SURFACE_TYPE_TRANSPARENT" } :
                new[] { "_FX_LIGHT_MODE_UNLIT" };
            m.SetTexture("_BaseMap", baseMap);
            m.SetTextureScale("_BaseMap", Vector2.one);
            m.SetTextureOffset("_BaseMap", Vector2.zero);
            m.SetTexture("_ParallaxMapping_Map", height);
            m.SetColor(graph ? "_Color" : "_BaseColor", Color.white);
            m.SetColor("_ColorA", Color.white);
            m.SetFloat("_BaseColorIntensityForTimeline", 1);
            m.SetFloat("_AlphaAll", 1);
            m.SetFloat("_Cull", backface ? (float)CullMode.Front : (float)CullMode.Off);
            m.SetFloat("_ZTest", (float)CompareFunction.LessEqual);
            m.SetFloat("_ZWrite", 0);
            m.SetFloat("_SrcBlend", (float)BlendMode.One);
            m.SetFloat("_DstBlend", (float)BlendMode.Zero);
            m.SetFloat("_BaseMapUVRotation", 0);
            m.SetVector("_BaseMapMaskMapOffset", Vector4.zero);
            if (graph)
            {
                m.SetFloat("_Surface", 1);
                m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                m.SetFloat("_DstBlendAlpha", (float)BlendMode.Zero);
                m.SetFloat("_NB_DistortionMode", 0);
                m.SetFloat("_NB_ColorChannelLo16", 3);
                m.SetShaderPassEnabled("SRPDefaultUnlit", true);
                m.SetShaderPassEnabled("UniversalForward", true);
            }
            else
            {
                m.SetFloat("_ColorMask", 15); m.SetFloat("_fogintensity", 0);
                m.SetFloat("_FxLightMode", 0);
                m.SetInteger("_W9ParticleShaderColorChannelFlag", 3);
                m.SetShaderPassEnabled("SRPDefaultUnlit", backface);
                m.SetShaderPassEnabled("UniversalForward", !backface);
            }
            foreach (string pass in new[] { "DepthOnly", "ShadowCaster",
                "NBCameraOpaqueDistortPass", "NBDeferredDistortPass", "Universal2D" })
                m.SetShaderPassEnabled(pass, false);
            m.renderQueue = 3000;
        }
        static void Apply(Material m, bool graph, State s)
        {
            m.SetFloat("_ParallaxMapping_Toggle", s.enabled ? 1 : 0);
            m.SetFloat("_ParallaxMapping_Intensity", s.intensity);
            m.SetVector("_ParallaxMapping_Vec", new Vector4(s.minLayers, s.maxLayers, 0, 0));
            m.SetTextureScale("_ParallaxMapping_Map", new Vector2(s.st.x, s.st.y));
            m.SetTextureOffset("_ParallaxMapping_Map", new Vector2(s.st.z, s.st.w));
            uint wrap = (uint)(s.wrap & 1) << 10 | (uint)(s.wrap >> 1) << 26;
            uint noMip = s.lod0 ? 1u << 17 : 0u;
            if (graph)
            {
                SetWord(m, "_NB_WrapFlagsLo16", "_NB_WrapFlagsHi16", wrap);
                SetWord(m, "_NB_ForceNoMipFlagsLo16", "_NB_ForceNoMipFlagsHi16", noMip);
            }
            else
            {
                m.SetInteger("_W9ParticleShaderWrapFlags", unchecked((int)wrap));
                m.SetInteger("_NBShaderForceNoMipFlags", unchecked((int)noMip));
                if (s.enabled) m.EnableKeyword("_PARALLAX_MAPPING");
                else m.DisableKeyword("_PARALLAX_MAPPING");
            }
        }
        static void SetWord(Material m, string lo, string hi, uint word)
        { m.SetFloat(lo, word & 65535u); m.SetFloat(hi, word >> 16); }
        static Mesh BuildMesh()
        {
            const int n = 17;
            var vertices = new Vector3[n * n]; var uv = new List<Vector4>(n * n);
            var tris = new int[(n - 1) * (n - 1) * 6];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float u = x / (n - 1f), v = y / (n - 1f);
                int i = y * n + x;
                vertices[i] = new Vector3(2 * u - 1, 2 * v - 1,
                    .19f * Mathf.Sin(u * Mathf.PI) * Mathf.Cos(v * Mathf.PI));
                uv.Add(new Vector4(u, v, .13f + .7f * u, .2f + .6f * v));
            }
            int t = 0;
            for (int y = 0; y < n - 1; y++) for (int x = 0; x < n - 1; x++)
            {
                int i = y * n + x;
                tris[t++] = i; tris[t++] = i + 1; tris[t++] = i + n;
                tris[t++] = i + 1; tris[t++] = i + n + 1; tris[t++] = i + n;
            }
            var mesh = new Mesh { name = "POM1 curved UV0+tangent" };
            mesh.vertices = vertices; mesh.triangles = tris; mesh.SetUVs(0, uv);
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            return mesh;
        }
        static Texture2D MakeBaseMap()
        {
            const int n = 256; var tex = new Texture2D(n, n, TextureFormat.RGBAHalf, true, true);
            var px = new Color[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float u = x / (n - 1f), v = y / (n - 1f);
                px[y * n + x] = new Color(.14f + .7f * u, .12f + .68f * v,
                    .17f + .65f * (.7f * u + .3f * v), 1);
            }
            tex.SetPixels(px); tex.Apply(true); tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Repeat; return tex;
        }
        static Texture2D MakeHeightMap()
        {
            const int n = 512; var tex = new Texture2D(n, n, TextureFormat.RGBAHalf, true, true);
            var px = new Color[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float u = x / (n - 1f), v = y / (n - 1f);
                float h = .18f + .62f * (.5f + .5f * Mathf.Sin(10.5f * u + 4.4f * v)) *
                    (.4f + .6f * Mathf.Cos(6.5f * v - 1.7f * u) * Mathf.Cos(6.5f * v - 1.7f * u));
                px[y * n + x] = new Color(h, h, h, 1);
            }
            tex.SetPixels(px); tex.Apply(true);
            for (int mip = 1; mip < tex.mipmapCount; mip++)
            {
                int side = Mathf.Max(1, n >> mip); var values = new Color[side * side];
                for (int i = 0; i < values.Length; i++) values[i] = new Color(.87f, .87f, .87f, 1);
                tex.SetPixels(values, mip);
            }
            tex.Apply(false); tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Repeat; return tex;
        }
        static Color[] Capture(Camera camera, RenderTexture rt, Texture2D readback, string path)
        {
            for (int i = 0; i < 4; i++) camera.Render();
            RenderTexture prev = RenderTexture.active;
            try
            {
                RenderTexture.active = rt;
                readback.ReadPixels(new Rect(0, 0, Size, Size), 0, 0); readback.Apply(false);
                Color[] pixels = readback.GetPixels();
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
            var result = new Difference();
            for (int y = Min; y < Max; y++) for (int x = Min; x < Max; x++)
            {
                int i = y * Size + x; float d = 0;
                for (int c = 0; c < 4; c++) d = Mathf.Max(d, Mathf.Abs(a[i][c] - b[i][c]));
                result.max = Mathf.Max(result.max, d);
                if (d > 0) result.changed++;
            }
            return result;
        }
        static int Visible(Color[] frame)
        {
            int count = 0;
            for (int y = Min; y < Max; y++) for (int x = Min; x < Max; x++)
            {
                Color c = frame[y * Size + x];
                if (Mathf.Max(c.r, Mathf.Max(c.g, c.b)) > .3f) count++;
            }
            return count;
        }
        static bool Finite(params Color[][] frames)
        {
            foreach (Color[] frame in frames) foreach (Color color in frame)
                for (int channel = 0; channel < 4; channel++)
                    if (float.IsNaN(color[channel]) || float.IsInfinity(color[channel])) return false;
            return true;
        }
    }
}
