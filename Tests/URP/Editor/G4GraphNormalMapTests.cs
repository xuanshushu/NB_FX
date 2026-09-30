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
    /// <summary>
    /// N0 ordinary curved Mesh only. A=G0 Frozen ShaderLab, B=current
    /// ShaderLab after pure normal-math extraction, C=Graph. Strict RGBA
    /// parity and positive counterfactuals; no lighting, refraction or VFX.
    /// </summary>
    public sealed class G4GraphNormalMapTests
    {
        const string FrozenPath = "Packages/com.xuanxuan.nb.fx/Tests/Baseline/Frozen/NBShaders2/Shader/NBShader.shader";
        const string CurrentPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader";
        const string GraphPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph";
        const int Size = 128, Layer = 2, RoiMin = 32, RoiMax = 96;

        static readonly string[] Names = {
            "fresnel-uv0", "fresnel-uv0zw", "fresnel-uv1", "fresnel-uv2",
            "fresnel-shared", "fresnel-twirl", "fresnel-mask-mode", "fresnel-scale-half",
            "fresnel-backface", "fresnel-negative-scale", "fresnel-nonuniform",
            "matcap-uv0", "matcap-mask-mode", "matcap-backface",
            "both-uv2", "both-polar", "both-wrap-repeat", "both-wrap-clamp",
            "both-wrap-repeatU-clampV", "both-wrap-clampU-repeatV",
            "both-lod-auto", "both-lod0"
        };

        [Serializable] sealed class State
        {
            public int uvMode, sharedMode, specialStream, wrapMode;
            public bool maskMode, twirl, polar, lod0 = true, enabled = true;
            public float strength = 1;
            public Vector4 st = new Vector4(1.7f, 1.3f, -.31f, .17f);
        }

        [Serializable] sealed class Metrics
        {
            public string caseId, unityVersion, api, note;
            public State input, control;
            public int roiPixels, abDifferences, bcDifferences, abControlDifferences,
                bcControlDifferences, frozenVisible, currentVisible, graphVisible;
            public int frozenPositivePixels, currentPositivePixels, graphPositivePixels;
            public float abMax, bcMax, abControlMax, bcControlMax;
            public float frozenRepeat, currentRepeat, graphRepeat;
            public float frozenControl, currentControl, graphControl;
            public bool finite;
        }

        static IEnumerable<TestCaseData> Cases()
        {
            foreach (string name in Names)
                foreach (bool ortho in new[] { true, false })
                    yield return new TestCaseData(name, ortho).SetName(
                        "G4NormalMapABC_" + name + (ortho ? "_ortho" : "_perspective"));
        }

        [TestCaseSource(nameof(Cases))]
        public void CurvedMeshNormalMapMatchesFrozenAndGraph(string name, bool ortho)
        {
            Assert.That(GraphicsSettings.currentRenderPipeline,
                Is.InstanceOf<UniversalRenderPipelineAsset>());
            Assert.That(SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf), Is.True);
            Shader frozenShader = AssetDatabase.LoadAssetAtPath<Shader>(FrozenPath);
            Shader currentShader = AssetDatabase.LoadAssetAtPath<Shader>(CurrentPath);
            Shader graphShader = AssetDatabase.LoadAssetAtPath<Shader>(GraphPath);
            Assert.That(frozenShader && currentShader && graphShader, Is.True);
            Assert.That(frozenShader.name, Is.EqualTo("Effects/NBShader_T00_Frozen"));
            Assert.That(frozenShader.isSupported && currentShader.isSupported && graphShader.isSupported, Is.True);
            string root = Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR");
            if (string.IsNullOrEmpty(root)) root = Path.Combine(
                Path.GetDirectoryName(Application.dataPath), "Temp/NBFXG4NormalMap");
            string output = Path.Combine(root, name + (ortho ? "-ortho" : "-perspective"));
            Directory.CreateDirectory(output);

            Scene scene = EditorSceneManager.NewPreviewScene();
            var mesh = BuildCurvedMesh();
            var actor = new GameObject("N0 curved ordinary Mesh", typeof(MeshFilter), typeof(MeshRenderer));
            var cameraObject = new GameObject("N0 A/B/C camera");
            var camera = cameraObject.AddComponent<Camera>();
            var frozen = new Material(frozenShader);
            var current = new Material(currentShader);
            var graph = new Material(graphShader);
            var normal = MakeNormalTexture(name.StartsWith("both-lod", StringComparison.Ordinal));
            var matcap = MakeMatCapTexture();
            var rt = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGBHalf,
                RenderTextureReadWrite.Linear);
            var readback = new Texture2D(Size, Size, TextureFormat.RGBAHalf, false, true);
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
                actor.transform.rotation = name.Contains("backface") ? Quaternion.Euler(0, 180, 0)
                    : Quaternion.Euler(11, 29, 4);
                actor.transform.localScale = name.Contains("negative-scale")
                    ? new Vector3(-1.3f, 1.45f, .75f)
                    : name.Contains("nonuniform") ? new Vector3(1.4f, .6f, 1.9f)
                    : new Vector3(1.15f, .9f, 1.2f);
                camera.scene = scene;
                camera.orthographic = ortho; camera.orthographicSize = 1.5f;
                camera.fieldOfView = 45; camera.nearClipPlane = .1f; camera.farClipPlane = 20;
                camera.transform.position = new Vector3(0, 0, 5);
                camera.transform.rotation = Quaternion.Euler(0, 180, 0);
                camera.cullingMask = 1 << Layer; camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear; camera.allowHDR = true;
                camera.allowMSAA = false; camera.targetTexture = rt;
                cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
                rt.Create(); Assert.That(rt.IsCreated() && !rt.sRGB, Is.True);

                bool fresnel = name.StartsWith("fresnel", StringComparison.Ordinal) ||
                    name.StartsWith("both", StringComparison.Ordinal);
                bool matCap = name.StartsWith("matcap", StringComparison.Ordinal) ||
                    name.StartsWith("both", StringComparison.Ordinal);
                Configure(frozen, false, fresnel, matCap, normal, matcap);
                Configure(current, false, fresnel, matCap, normal, matcap);
                Configure(graph, true, fresnel, matCap, normal, matcap);
                State primary = MakeState(name), control = MakeControl(name, primary);

                Apply(frozen, current, graph, primary);
                renderer.sharedMaterial = frozen;
                Color[] a = Capture(camera, rt, readback, Path.Combine(output, "A-frozen"));
                Color[] ar = Capture(camera, rt, readback, Path.Combine(output, "A-repeat"));
                renderer.sharedMaterial = current;
                Color[] b = Capture(camera, rt, readback, Path.Combine(output, "B-current"));
                Color[] br = Capture(camera, rt, readback, Path.Combine(output, "B-repeat"));
                renderer.sharedMaterial = graph;
                Color[] c = Capture(camera, rt, readback, Path.Combine(output, "C-graph"));
                Color[] cr = Capture(camera, rt, readback, Path.Combine(output, "C-repeat"));

                Apply(frozen, current, graph, control);
                renderer.sharedMaterial = frozen;
                Color[] ac = Capture(camera, rt, readback, Path.Combine(output, "A-control"));
                renderer.sharedMaterial = current;
                Color[] bc = Capture(camera, rt, readback, Path.Combine(output, "B-control"));
                renderer.sharedMaterial = graph;
                Color[] cc = Capture(camera, rt, readback, Path.Combine(output, "C-control"));

                Metrics metrics = Compare(a, b);
                Metrics graphParity = Compare(b, c), abControl = Compare(ac, bc),
                    bcControl = Compare(bc, cc);
                metrics.caseId = name + (ortho ? "-ortho" : "-perspective");
                metrics.input = primary; metrics.control = control;
                metrics.unityVersion = Application.unityVersion;
                metrics.api = SystemInfo.graphicsDeviceType.ToString();
                metrics.note = "N0 ordinary curved Mesh, 128x128 linear RGBAHalf; strict ROI x/y=32..95. A Frozen/B extracted ShaderLab/C Graph. NormalMap influences only Fresnel/MatCap; lighting/refraction/CustomLocal/VFX/Player not tested. Four warm-up renders. Raw RGBA float gzip and PNG retained; no material or scene saved.";
                metrics.bcDifferences = graphParity.abDifferences;
                metrics.abControlDifferences = abControl.abDifferences;
                metrics.bcControlDifferences = bcControl.abDifferences;
                metrics.bcMax = graphParity.abMax;
                metrics.abControlMax = abControl.abMax;
                metrics.bcControlMax = bcControl.abMax;
                metrics.currentVisible = graphParity.frozenVisible;
                metrics.graphVisible = graphParity.currentVisible;
                metrics.frozenRepeat = MaxDelta(a, ar);
                metrics.currentRepeat = MaxDelta(b, br);
                metrics.graphRepeat = MaxDelta(c, cr);
                metrics.frozenControl = MaxDelta(a, ac);
                metrics.currentControl = MaxDelta(b, bc);
                metrics.graphControl = MaxDelta(c, cc);
                metrics.frozenPositivePixels = ChangedPixels(a, ac);
                metrics.currentPositivePixels = ChangedPixels(b, bc);
                metrics.graphPositivePixels = ChangedPixels(c, cc);
                metrics.finite = AllFinite(a, ar, b, br, c, cr, ac, bc, cc);
                File.WriteAllText(Path.Combine(output, "metrics.json"),
                    JsonUtility.ToJson(metrics, true));
                Debug.Log("NBFX_G4_NORMAL_MAP_ABC " + JsonUtility.ToJson(metrics));
                Assert.That(metrics.finite, Is.True);
                Assert.That(metrics.frozenVisible, Is.GreaterThan(256));
                Assert.That(metrics.currentVisible, Is.GreaterThan(256));
                Assert.That(metrics.graphVisible, Is.GreaterThan(256));
                Assert.That(metrics.frozenRepeat, Is.Zero);
                Assert.That(metrics.currentRepeat, Is.Zero);
                Assert.That(metrics.graphRepeat, Is.Zero);
                Assert.That(metrics.frozenControl, Is.GreaterThan(.01f));
                Assert.That(metrics.currentControl, Is.GreaterThan(.01f));
                Assert.That(metrics.graphControl, Is.GreaterThan(.01f));
                Assert.That(metrics.frozenPositivePixels, Is.GreaterThan(64));
                Assert.That(metrics.currentPositivePixels, Is.GreaterThan(64));
                Assert.That(metrics.graphPositivePixels, Is.GreaterThan(64));
                Assert.That(metrics.abDifferences, Is.Zero, "Pure ShaderLab extraction A/B mismatch.");
                Assert.That(metrics.abControlDifferences, Is.Zero);
                Assert.That(metrics.bcDifferences, Is.Zero, "Graph normal-map B/C mismatch.");
                Assert.That(metrics.bcControlDifferences, Is.Zero);
            }
            finally
            {
                camera.targetTexture = null; RenderTexture.active = previous;
                rt.Release();
                foreach (var obj in new UnityEngine.Object[] {
                    mesh, frozen, current, graph, normal, matcap, rt, readback })
                    UnityEngine.Object.DestroyImmediate(obj);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        static State MakeState(string name)
        {
            var s = new State();
            if (name.Contains("uv0zw")) s.uvMode = 1;
            else if (name.Contains("uv1")) { s.uvMode = 1; s.specialStream = 1; }
            else if (name.Contains("uv2")) { s.uvMode = 1; s.specialStream = 2; }
            else if (name.Contains("shared"))
            { s.uvMode = 8; s.sharedMode = 1; s.specialStream = 2; }
            if (name.Contains("twirl")) { s.uvMode = 2; s.twirl = true; }
            if (name.Contains("polar")) { s.uvMode = 2; s.polar = true; }
            if (name.Contains("mask-mode")) s.maskMode = true;
            if (name.Contains("scale-half")) s.strength = .5f;
            if (name.Contains("wrap-"))
            {
                s.st = new Vector4(2.75f, 2.25f, -.65f, -.5f);
                s.wrapMode = name.Contains("repeatU-clampV") ? 2 :
                    name.Contains("clampU-repeatV") ? 3 :
                    name.Contains("clamp") ? 1 : 0;
            }
            if (name == "both-lod-auto") s.lod0 = false;
            return s;
        }
        static State MakeControl(string name, State primary)
        {
            var c = new State { uvMode = primary.uvMode, sharedMode = primary.sharedMode,
                specialStream = primary.specialStream,
                wrapMode = primary.wrapMode, maskMode = primary.maskMode,
                twirl = primary.twirl, polar = primary.polar,
                lod0 = primary.lod0, strength = primary.strength, st = primary.st };
            if (name.Contains("uv0zw") || name.Contains("uv1") ||
                name.Contains("uv2") || name.Contains("shared") ||
                name.Contains("twirl") || name.Contains("polar")) c.uvMode = 0;
            else if (name.Contains("mask-mode")) c.maskMode = false;
            else if (name.Contains("scale-half")) c.strength = 1;
            else if (name.Contains("wrap-")) c.wrapMode = primary.wrapMode == 0 ? 1 : 0;
            else if (name.StartsWith("both-lod", StringComparison.Ordinal)) c.lod0 = !primary.lod0;
            else c.enabled = false;
            return c;
        }

        static void Configure(Material m, bool graph, bool fresnel, bool matCap,
            Texture2D normal, Texture2D matcapMap)
        {
            foreach (string property in new[] { "_BumpMapToggle", "_BumpTex", "_BumpScale" })
                Assert.That(m.HasProperty(property), Is.True, "N0 property missing: " + property);
            m.shaderKeywords = graph ? new[] { "_SURFACE_TYPE_TRANSPARENT" } :
                new[] { "_FX_LIGHT_MODE_UNLIT" };
            m.SetTexture("_BaseMap", Texture2D.whiteTexture);
            m.SetTextureScale("_BaseMap", Vector2.one);
            m.SetTextureOffset("_BaseMap", Vector2.zero);
            m.SetTexture("_BumpTex", normal);
            m.SetColor(graph ? "_Color" : "_BaseColor", new Color(.35f, .55f, .8f, 1));
            m.SetColor("_ColorA", Color.white);
            m.SetFloat("_BaseColorIntensityForTimeline", 1);
            m.SetFloat("_AlphaAll", 1);
            m.SetFloat("_Cull", (float)CullMode.Off);
            m.SetFloat("_ZTest", (float)CompareFunction.LessEqual);
            m.SetFloat("_ZWrite", 0);
            m.SetFloat("_SrcBlend", (float)BlendMode.One);
            m.SetFloat("_DstBlend", (float)BlendMode.Zero);
            m.SetFloat("_BaseMapUVRotation", 0);
            m.SetVector("_BaseMapMaskMapOffset", Vector4.zero);
            m.SetFloat("_fresnelEnabled", fresnel ? 1 : 0);
            m.SetVector("_FresnelUnit", new Vector4(.5f, 1, .75f, 0));
            m.SetColor("_FresnelColor", new Color(1, .125f, .25f, 1));
            m.SetVector("_FresnelRotation", Vector4.zero);
            m.SetFloat("_MatCapToggle", matCap ? 1 : 0);
            m.SetTexture("_MatCapTex", matcapMap);
            m.SetColor("_MatCapColor", new Color(.75f, .375f, 1.25f, .875f));
            m.SetVector("_MatCapInfo", new Vector4(.5f, 0, 0, 0));
            if (graph)
            {
                m.SetFloat("_Surface", 1);
                m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                m.SetFloat("_DstBlendAlpha", (float)BlendMode.Zero);
                m.SetFloat("_NB_DistortionMode", 0);
                m.SetFloat("_NB_ColorChannelLo16", 3);
            }
            else
            {
                m.SetFloat("_ColorMask", 15);
                m.SetFloat("_fogintensity", 0);
                m.SetFloat("_FxLightMode", 0);
                m.SetInteger("_W9ParticleShaderColorChannelFlag", 3);
                m.EnableKeyword("_NORMALMAP");
                if (fresnel) m.EnableKeyword("_FRESNEL");
                if (matCap) m.EnableKeyword("_MATCAP");
                m.SetShaderPassEnabled("SRPDefaultUnlit", false);
                m.SetShaderPassEnabled("SRPDEFAULTUNLIT", false);
                m.SetShaderPassEnabled("UniversalForward", true);
            }
            foreach (string pass in new[] { "DepthOnly", "ShadowCaster",
                "NBCameraOpaqueDistortPass", "NBDeferredDistortPass", "Universal2D" })
                m.SetShaderPassEnabled(pass, false);
            m.renderQueue = 3000;
        }

        static void Apply(Material frozen, Material current, Material graph, State s)
        {
            uint flags0 = (s.maskMode ? 1u << 21 : 0) |
                (s.twirl ? 1u << 9 : 0) | (s.polar ? 1u << 8 : 0);
            uint flags1 = 1u | (1u << 9);
            // Distinct UV1.xy and UV2.xy selection is a separate flags1 route.
            if (s.specialStream != 0)
                flags1 |= (1u << 21) | (s.specialStream == 1 ? 1u << 18 : 1u << 19);
            uint uvWords = (uint)(s.uvMode & 3) << 24 |
                (uint)(s.sharedMode & 3) << 30;
            uint uvTypeWords = (uint)(s.uvMode >> 2) << 24 |
                (uint)(s.sharedMode >> 2) << 30;
            uint wrap = (uint)(s.wrapMode & 1) << 14 |
                (uint)(s.wrapMode >> 1) << 30;
            uint noMip = s.lod0 ? 1u << 1 : 0;
            foreach (Material m in new[] { frozen, current, graph })
            {
                m.SetFloat("_BumpMapToggle", s.enabled ? 1 : 0);
                m.SetFloat("_BumpScale", s.strength);
                m.SetTextureScale("_BumpTex", new Vector2(s.st.x, s.st.y));
                m.SetTextureOffset("_BumpTex", new Vector2(s.st.z, s.st.w));
                m.SetVector("_SharedUV_ST", new Vector4(.89f, 1.13f, .07f, -.09f));
                m.SetVector("_SharedUV_Vec", new Vector4(0, 0, 23, 0));
                m.SetVector("_TWParameter", new Vector4(.42f, .57f, 0, 0));
                m.SetFloat("_TWStrength", 2.1f);
                m.SetVector("_PCCenter", new Vector4(.46f, .53f, 1, 0));
                if (m == graph)
                {
                    SetWord(m, "_NB_Flags0Lo16", "_NB_Flags0Hi16", flags0);
                    SetWord(m, "_NB_Flags1Lo16", "_NB_Flags1Hi16", flags1);
                    SetWord(m, "_NB_WrapFlagsLo16", "_NB_WrapFlagsHi16", wrap);
                    SetWord(m, "_NB_ForceNoMipFlagsLo16", "_NB_ForceNoMipFlagsHi16", noMip);
                    SetWord(m, "_NB_UVModeFlag0Lo16", "_NB_UVModeFlag0Hi16", uvWords);
                    SetWord(m, "_NB_UVModeFlagType0Lo16", "_NB_UVModeFlagType0Hi16", uvTypeWords);
                }
                else
                {
                    m.SetInteger("_W9ParticleShaderFlags", unchecked((int)flags0));
                    m.SetInteger("_W9ParticleShaderFlags1", unchecked((int)flags1));
                    m.SetInteger("_W9ParticleShaderWrapFlags", unchecked((int)wrap));
                    m.SetInteger("_NBShaderForceNoMipFlags", unchecked((int)noMip));
                    m.SetInteger("_UVModeFlag0", unchecked((int)uvWords));
                    m.SetInteger("_UVModeFlagType0", unchecked((int)uvTypeWords));
                    if (s.uvMode == 8) m.EnableKeyword("_SHARED_UV");
                    else m.DisableKeyword("_SHARED_UV");
                    if (s.enabled) m.EnableKeyword("_NORMALMAP");
                    else m.DisableKeyword("_NORMALMAP");
                }
            }
        }
        static void SetWord(Material m, string lo, string hi, uint word)
        { m.SetFloat(lo, word & 65535u); m.SetFloat(hi, word >> 16); }

        static Mesh BuildCurvedMesh()
        {
            const int n = 17;
            var vertices = new Vector3[n * n];
            var uv0 = new List<Vector4>(n * n);
            var uv1 = new List<Vector4>(n * n);
            var uv2 = new List<Vector4>(n * n);
            var triangles = new int[(n - 1) * (n - 1) * 6];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float u = x / (n - 1f), v = y / (n - 1f);
                int i = y * n + x;
                vertices[i] = new Vector3(2f * u - 1f, 2f * v - 1f,
                    .22f * Mathf.Sin(u * Mathf.PI) * Mathf.Cos(v * Mathf.PI));
                uv0.Add(new Vector4(u, v, .83f * u + .07f, .61f * v + .19f));
                uv1.Add(new Vector4(.71f * v + .11f, .88f * u + .05f, 0, 0));
                uv2.Add(new Vector4(.67f * u + .23f, .72f * (1f - v) + .09f, 0, 0));
            }
            int t = 0;
            for (int y = 0; y < n - 1; y++) for (int x = 0; x < n - 1; x++)
            {
                int i = y * n + x;
                triangles[t++] = i; triangles[t++] = i + 1; triangles[t++] = i + n;
                triangles[t++] = i + 1; triangles[t++] = i + n + 1; triangles[t++] = i + n;
            }
            var mesh = new Mesh { name = "N0 curved float4 UV0/1/2" };
            mesh.vertices = vertices; mesh.triangles = triangles;
            mesh.SetUVs(0, uv0); mesh.SetUVs(1, uv1); mesh.SetUVs(2, uv2);
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            return mesh;
        }

        static Texture2D MakeNormalTexture(bool lod)
        {
            int n = lod ? 1024 : 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBAHalf, lod, true);
            var colors = new Color[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float u = x / (n - 1f), v = y / (n - 1f);
                colors[y * n + x] = lod ?
                    (((x + y) & 1) == 0 ? new Color(.125f, .875f, .25f, .75f) :
                        new Color(.875f, .125f, .75f, .25f)) :
                    new Color(.25f + .5f * u, .25f + .5f * v,
                        .375f + .25f * u, .625f + .25f * v);
            }
            tex.SetPixels(colors); tex.Apply(lod);
            if (lod)
            {
                for (int mip = 1; mip < tex.mipmapCount; mip++)
                {
                    int side = Mathf.Max(1, n >> mip);
                    var values = new Color[side * side];
                    for (int i = 0; i < values.Length; i++)
                        values[i] = new Color(.875f, .125f, .25f, .75f);
                    tex.SetPixels(values, mip);
                }
                tex.Apply(false);
            }
            tex.filterMode = FilterMode.Bilinear; tex.wrapMode = TextureWrapMode.Repeat;
            return tex;
        }
        static Texture2D MakeMatCapTexture()
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBAHalf, false, true);
            var colors = new Color[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                colors[y * n + x] = new Color(.1f + .7f * x / (n - 1f),
                    .15f + .65f * y / (n - 1f), .25f + .5f * (x + y) / (2f * n - 2f), 1);
            tex.SetPixels(colors); tex.Apply(false);
            tex.filterMode = FilterMode.Bilinear; tex.wrapMode = TextureWrapMode.Clamp;
            return tex;
        }
        static Color[] Capture(Camera camera, RenderTexture rt, Texture2D readback, string path)
        {
            for (int i = 0; i < 4; i++) camera.Render();
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = rt;
                readback.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
                readback.Apply(false);
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
            finally { RenderTexture.active = previous; }
        }
        static Metrics Compare(Color[] a, Color[] b)
        {
            var m = new Metrics();
            for (int y = RoiMin; y < RoiMax; y++) for (int x = RoiMin; x < RoiMax; x++)
            {
                int i = y * Size + x; float max = 0;
                m.roiPixels++;
                if (a[i].maxColorComponent > .01f) m.frozenVisible++;
                if (b[i].maxColorComponent > .01f) m.currentVisible++;
                for (int channel = 0; channel < 4; channel++)
                    max = Mathf.Max(max, Mathf.Abs(a[i][channel] - b[i][channel]));
                if (max > 0) m.abDifferences++;
                m.abMax = Mathf.Max(m.abMax, max);
            }
            return m;
        }
        static float MaxDelta(Color[] a, Color[] b) => Compare(a, b).abMax;
        static int ChangedPixels(Color[] a, Color[] b)
        {
            int count = 0;
            for (int y = RoiMin; y < RoiMax; y++) for (int x = RoiMin; x < RoiMax; x++)
            {
                int i = y * Size + x; float max = 0;
                for (int channel = 0; channel < 4; channel++)
                    max = Mathf.Max(max, Mathf.Abs(a[i][channel] - b[i][channel]));
                if (max > .01f) count++;
            }
            return count;
        }
        static bool AllFinite(params Color[][] frames)
        {
            foreach (Color[] frame in frames)
                foreach (Color color in frame)
                    for (int channel = 0; channel < 4; channel++)
                        if (float.IsNaN(color[channel]) || float.IsInfinity(color[channel]))
                            return false;
            return true;
        }
    }
}
