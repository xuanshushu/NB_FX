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
    /// Ordinary-Mesh feature-source UV routing, not VFX or full UV parity.
    /// Every feature retains its own transform after the packed mode selects
    /// UV0, special UV, Twirl or Shared. Strict RGBA failures are evidence.
    /// </summary>
    public sealed class G4GraphFeatureUVTests
    {
        const string GraphPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph";
        const string LegacyPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader";
        const int Size = 128, Layer = 2;
        static readonly string[] Features = {
            "mask1", "mask2", "mask3", "overlay1", "overlay2",
            "color-ramp", "dissolve", "dissolve-mask"
        };
        static readonly string[] Routes = {
            "default-uv0", "special-uv0zw", "special-uv1", "special-uv2",
            "twirl", "shared-special-uv2"
        };

        static IEnumerable<TestCaseData> Cases()
        {
            foreach (string feature in Features)
                foreach (string route in Routes)
                    foreach (bool ortho in new[] { true, false })
                        yield return new TestCaseData(feature, route, ortho).SetName(
                            "G4FeatureUVBC_" + feature + "_" + route +
                            (ortho ? "_ortho" : "_perspective"));
        }

        [Serializable]
        sealed class Metrics
        {
            public string caseId, unityVersion, api, target, note;
            public bool orthographic, finite;
            public int roiPixels, differentRGBA, graphVisible, legacyVisible;
            public int graphPasses, legacyPasses;
            public float maxRGBA, controlMaxRGBA, graphRepeat, legacyRepeat;
            public float graphOnOff, legacyOnOff;
        }

        [TestCaseSource(nameof(Cases))]
        public void FeaturePackedUVMatchesShaderLab(string feature, string route, bool ortho)
        {
            Assert.That(GraphicsSettings.currentRenderPipeline, Is.InstanceOf<UniversalRenderPipelineAsset>());
            Assert.That(SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf), Is.True);
            Shader graphShader = AssetDatabase.LoadAssetAtPath<Shader>(GraphPath);
            Shader legacyShader = AssetDatabase.LoadAssetAtPath<Shader>(LegacyPath);
            Assert.That(graphShader && legacyShader && graphShader.isSupported && legacyShader.isSupported, Is.True);
            string root = Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR");
            if (string.IsNullOrEmpty(root))
                root = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/NBFXG4FeatureUV");
            string output = Path.Combine(root, feature + "-" + route +
                (ortho ? "-ortho" : "-perspective"));
            Directory.CreateDirectory(output);

            Scene scene = EditorSceneManager.NewPreviewScene();
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            GameObject cameraObject = new GameObject("NBFX ordinary Mesh feature UV B/C");
            Mesh ownedMesh = UnityEngine.Object.Instantiate(quad.GetComponent<MeshFilter>().sharedMesh);
            quad.GetComponent<MeshFilter>().sharedMesh = ownedMesh;
            Material graph = new Material(graphShader), legacy = new Material(legacyShader);
            Texture2D map = MakeMap();
            var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            var readback = new Texture2D(Size, Size, TextureFormat.RGBAHalf, false, true);
            var camera = cameraObject.AddComponent<Camera>();
            RenderTexture previousActive = RenderTexture.active;
            try
            {
                SetDistinctUVStreams(ownedMesh);
                SceneManager.MoveGameObjectToScene(quad, scene);
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                Configure(graph, true, feature, route, map);
                Configure(legacy, false, feature, route, map);
                quad.layer = Layer;
                quad.transform.localScale = new Vector3(2, 2, 1);
                var renderer = quad.GetComponent<MeshRenderer>();
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                camera.scene = scene;
                camera.transform.position = new Vector3(0, 0, 5);
                camera.transform.rotation = Quaternion.Euler(0, 180, 0);
                camera.orthographic = ortho;
                camera.orthographicSize = 1.5f;
                camera.fieldOfView = 45f;
                camera.nearClipPlane = .1f;
                camera.farClipPlane = 20f;
                camera.cullingMask = 1 << Layer;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
                camera.allowHDR = true;
                camera.allowMSAA = false;
                camera.targetTexture = target;
                target.Create();
                Assert.That(target.IsCreated() && !target.sRGB, Is.True);

                SetProtocol(graph, legacy, feature, route);
                renderer.sharedMaterial = legacy;
                Color[] b = Capture(camera, target, readback, Path.Combine(output, "B-on"));
                Color[] br = Capture(camera, target, readback, null);
                renderer.sharedMaterial = graph;
                Color[] c = Capture(camera, target, readback, Path.Combine(output, "C-on"));
                Color[] cr = Capture(camera, target, readback, null);

                string control = route == "default-uv0" ? "special-uv0zw" : "default-uv0";
                SetProtocol(graph, legacy, feature, control);
                renderer.sharedMaterial = legacy;
                Color[] bOff = Capture(camera, target, readback, Path.Combine(output, "B-off"));
                renderer.sharedMaterial = graph;
                Color[] cOff = Capture(camera, target, readback, Path.Combine(output, "C-off"));

                Metrics metrics = Compare(b, c);
                Metrics off = Compare(bOff, cOff);
                metrics.caseId = feature + "-" + route + (ortho ? "-ortho" : "-perspective");
                metrics.unityVersion = Application.unityVersion;
                metrics.api = SystemInfo.graphicsDeviceType.ToString();
                metrics.target = "linear RGBAHalf, 128x128, strict inner 48x48 ROI";
                metrics.note = "Ordinary Mesh; independent float4 TEXCOORD0/1/2, nonuniform linear texture, per-feature nonidentity ST, zero scroll speed. Static 17-degree rotation only for special-uv2. Modes 0/1/2/8; no VFX, first-frame, CustomData, position, cylinder, Noise or DissolveRamp claim.";
                metrics.orthographic = ortho;
                metrics.graphPasses = graph.passCount;
                metrics.legacyPasses = legacy.passCount;
                metrics.graphRepeat = MaxDelta(c, cr);
                metrics.legacyRepeat = MaxDelta(b, br);
                metrics.graphOnOff = MaxDelta(c, cOff);
                metrics.legacyOnOff = MaxDelta(b, bOff);
                metrics.controlMaxRGBA = off.maxRGBA;
                File.WriteAllText(Path.Combine(output, "metrics.json"), JsonUtility.ToJson(metrics, true));
                Debug.Log("NBFX_G4_FEATURE_UV_BC " + JsonUtility.ToJson(metrics));
                Assert.That(metrics.finite && off.finite, Is.True, "Non-finite UV color.");
                Assert.That(metrics.graphVisible, Is.GreaterThan(100), "Graph foreground absent.");
                Assert.That(metrics.legacyVisible, Is.GreaterThan(100), "ShaderLab foreground absent.");
                Assert.That(metrics.graphRepeat, Is.Zero, "Graph repeated capture changed.");
                Assert.That(metrics.legacyRepeat, Is.Zero, "ShaderLab repeated capture changed.");
                Assert.That(metrics.graphOnOff, Is.GreaterThan(.03f), "Graph route control had no effect.");
                Assert.That(metrics.legacyOnOff, Is.GreaterThan(.03f), "ShaderLab route control had no effect.");
                Assert.That(metrics.differentRGBA, Is.Zero, "Strict on-state RGBA Mesh B/C mismatch.");
                Assert.That(off.differentRGBA, Is.Zero, "Strict off-state RGBA Mesh B/C mismatch.");
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previousActive;
                target.Release();
                foreach (var obj in new UnityEngine.Object[] { ownedMesh, graph, legacy, map, target, readback })
                    UnityEngine.Object.DestroyImmediate(obj);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        static void SetDistinctUVStreams(Mesh mesh)
        {
            Vector2[] original = mesh.uv;
            var uv0 = new List<Vector4>(mesh.vertexCount);
            var custom1 = new List<Vector4>(mesh.vertexCount);
            var custom2 = new List<Vector4>(mesh.vertexCount);
            foreach (Vector2 uv in original)
            {
                float x = uv.x, y = uv.y;
                uv0.Add(new Vector4(.09f + .79f * x, .13f + .70f * y,
                    .84f - .51f * x, .18f + .59f * y));
                custom1.Add(new Vector4(.17f + .43f * x, .84f - .47f * y,
                    .22f + .37f * x, .31f + .29f * y));
                custom2.Add(new Vector4(.79f - .55f * x, .09f + .52f * y,
                    .91f - .60f * x, .77f - .42f * y));
            }
            mesh.SetUVs(0, uv0);
            mesh.SetUVs(1, custom1);
            mesh.SetUVs(2, custom2);
        }

        static Texture2D MakeMap()
        {
            const int n = 32;
            var map = new Texture2D(n, n, TextureFormat.RGBA32, false, true);
            var pixels = new Color[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                pixels[y * n + x] = new Color((x + 1f) / (n + 1f),
                    (y + 1f) / (n + 1f), (x + 2f * y + 2f) / (3f * n + 3f),
                    (2f * x + y + 3f) / (3f * n + 3f));
            map.SetPixels(pixels); map.Apply(false);
            map.filterMode = FilterMode.Bilinear;
            map.wrapMode = TextureWrapMode.Repeat;
            return map;
        }

        static void Configure(Material m, bool graph, string feature, string route, Texture2D map)
        {
            m.SetTexture("_BaseMap", Texture2D.whiteTexture);
            m.SetTexture("_MaskMap", Texture2D.whiteTexture);
            m.SetTexture("_MaskMap2", Texture2D.whiteTexture);
            m.SetTexture("_MaskMap3", Texture2D.whiteTexture);
            m.SetVector("_MaskMapVec", new Vector4(1, 0, 0, 0));
            m.SetColor(graph ? "_Color" : "_BaseColor", Color.white);
            m.SetColor("_ColorA", Color.white);
            m.SetFloat("_BaseColorIntensityForTimeline", 1);
            m.SetFloat("_AlphaAll", 1);
            m.SetFloat("_Cull", 0); m.SetFloat("_ZTest", 4); m.SetFloat("_ZWrite", 0);
            m.SetFloat("_SrcBlend", (float)BlendMode.One);
            m.SetFloat("_DstBlend", (float)BlendMode.Zero);
            m.renderQueue = 3000;
            if (graph)
            {
                m.SetFloat("_Surface", 1); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                m.SetFloat("_DstBlendAlpha", (float)BlendMode.Zero);
                m.SetFloat("_NB_DistortionMode", 0); m.SetFloat("_NB_ColorChannelLo16", 3);
                m.SetVector("_BaseMap_ST", new Vector4(1, 1, 0, 0));
            }
            else
            {
                m.EnableKeyword("_FX_LIGHT_MODE_UNLIT");
                m.SetFloat("_ColorMask", 15); m.SetFloat("_fogintensity", 0);
                m.SetInteger("_W9ParticleShaderColorChannelFlag", 3);
                m.SetShaderPassEnabled("SRPDefaultUnlit", false);
                m.SetShaderPassEnabled("SRPDEFAULTUNLIT", false);
            }
            string property;
            switch (feature)
            {
                case "mask1": property = "_MaskMap";
                    Enable(m, "_Mask_Toggle", "_MASKMAP_ON"); break;
                case "mask2": property = "_MaskMap2";
                    Enable(m, "_Mask_Toggle", "_MASKMAP_ON");
                    Enable(m, "_Mask2_Toggle", "_MASKMAP2_ON"); break;
                case "mask3": property = "_MaskMap3";
                    Enable(m, "_Mask_Toggle", "_MASKMAP_ON");
                    Enable(m, "_Mask3_Toggle", "_MASKMAP3_ON"); break;
                case "overlay1": property = "_EmissionMap";
                    Enable(m, "_EmissionEnabled", "_EMISSION");
                    m.SetColor("_EmissionMapColor", Color.white); break;
                case "overlay2": property = "_ColorBlendMap";
                    Enable(m, "_ColorBlendMap_Toggle", "_COLORMAPBLEND");
                    m.SetColor("_ColorBlendColor", Color.white); break;
                case "color-ramp": property = "_RampColorMap";
                    Enable(m, "_RampColorToggle", "_COLOR_RAMP");
                    if (!graph) m.EnableKeyword("_COLOR_RAMP_MAP");
                    m.SetFloat("_RampColorSourceMode", 1);
                    m.SetColor("_RampColor0", new Color(1, 0, 0, 0));
                    m.SetColor("_RampColor1", new Color(0, 0, 1, 1));
                    m.SetVector("_RampColorAlpha0", new Vector4(1, 0, 1, 1));
                    m.SetColor("_RampColorBlendColor", Color.white);
                    if (graph) m.SetFloat("_RampColorCount", 131074);
                    else m.SetInteger("_RampColorCount", 131074); break;
                case "dissolve": property = "_DissolveMap";
                    Enable(m, "_Dissolve_Toggle", "_DISSOLVE");
                    m.SetVector("_Dissolve", new Vector4(.5f, 1, 1, 1)); break;
                case "dissolve-mask": property = "_DissolveMaskMap";
                    Enable(m, "_Dissolve_Toggle", "_DISSOLVE");
                    Enable(m, "_DissolveMask_Toggle", "_DISSOLVE_MASK");
                    m.SetTexture("_DissolveMap", Texture2D.grayTexture);
                    m.SetVector("_Dissolve", new Vector4(.5f, 1, 1, 1));
                    m.SetFloat("_DissolveMaskMode", 1); break;
                default: throw new ArgumentException(feature);
            }
            m.SetTexture(property, map);
            // Each feature's own ST applies once after its selected source UV.
            // Keep animation speeds at zero so B/C and repeated captures share time.
            Vector4 st = new Vector4(1.19f, .83f, -.12f, .09f);
            m.SetTextureScale(property, new Vector2(st.x, st.y));
            m.SetTextureOffset(property, new Vector2(st.z, st.w));
            float rotation = route == "special-uv2" ? 17f : 0f;
            switch (feature)
            {
                case "mask1": m.SetFloat("_MaskMapUVRotation", rotation); break;
                case "mask2": m.SetVector("_MaskMapVec", new Vector4(1, rotation, 0, 0)); break;
                case "mask3": m.SetVector("_MaskMapVec", new Vector4(1, 0, rotation, 0)); break;
                case "overlay1": m.SetFloat("_EmissionMapUVRotation", rotation); break;
                case "overlay2": m.SetVector("_ColorBlendVec", new Vector4(0, 0, 1, rotation)); break;
                case "color-ramp": m.SetVector("_RampColorMapOffset", new Vector4(0, 0, 0, rotation)); break;
                case "dissolve":
                case "dissolve-mask": m.SetVector("_DissolveOffsetRotateDistort", new Vector4(0, 0, rotation, 0)); break;
            }
        }

        static void Enable(Material m, string property, string keyword)
        {
            if (m.HasProperty(property)) m.SetFloat(property, 1);
            if (!m.HasProperty("_NB_WrapFlagsLo16")) m.EnableKeyword(keyword);
        }

        static int FeaturePosition(string feature)
        {
            switch (feature)
            {
                case "mask1": return 2;
                case "mask2": return 4;
                case "mask3": return 6;
                case "overlay1": return 12;
                case "dissolve": return 14;
                case "dissolve-mask": return 16;
                case "overlay2": return 18;
                case "color-ramp": return 26;
                default: throw new ArgumentException(feature);
            }
        }

        static void SetProtocol(Material graph, Material legacy, string feature, string route)
        {
            int featureMode = 0, sharedMode = 0;
            uint flags0 = 0, flags1 = 1u << 9; // Ignore vertex color.
            switch (route)
            {
                case "default-uv0": break;
                case "special-uv0zw": featureMode = 1; break;
                case "special-uv1": featureMode = 1;
                    flags1 |= (1u << 21) | (1u << 18); break;
                case "special-uv2": featureMode = 1;
                    flags1 |= (1u << 21) | (1u << 19); break;
                case "twirl": featureMode = 2; flags0 = 1u << 9; break;
                case "shared-special-uv2": featureMode = 8; sharedMode = 1;
                    flags1 |= (1u << 21) | (1u << 19); break;
                default: throw new ArgumentException(route);
            }
            int pos = FeaturePosition(feature);
            uint modeBits = ((uint)(featureMode & 3) << pos) |
                ((uint)(sharedMode & 3) << 30);
            uint typeBits = ((uint)(featureMode >> 2) << pos) |
                ((uint)(sharedMode >> 2) << 30);
            SetWord(graph, "_NB_Flags0Lo16", "_NB_Flags0Hi16", flags0);
            SetWord(graph, "_NB_Flags1Lo16", "_NB_Flags1Hi16", flags1);
            SetWord(graph, "_NB_UVModeFlag0Lo16", "_NB_UVModeFlag0Hi16", modeBits);
            SetWord(graph, "_NB_UVModeFlagType0Lo16", "_NB_UVModeFlagType0Hi16", typeBits);
            legacy.SetInteger("_W9ParticleShaderFlags", unchecked((int)flags0));
            legacy.SetInteger("_W9ParticleShaderFlags1", unchecked((int)flags1));
            legacy.SetInteger("_UVModeFlag0", unchecked((int)modeBits));
            legacy.SetInteger("_UVModeFlagType0", unchecked((int)typeBits));
            Vector4 sharedST = route == "shared-special-uv2" ?
                new Vector4(.77f, .85f, .11f, -.08f) : new Vector4(1, 1, 0, 0);
            Vector4 sharedVec = route == "shared-special-uv2" ?
                new Vector4(0, 0, 29f, 0) : Vector4.zero;
            graph.SetVector("_SharedUV_ST", sharedST);
            graph.SetVector("_SharedUV_Vec", sharedVec);
            legacy.SetVector("_SharedUV_ST", sharedST);
            legacy.SetVector("_SharedUV_Vec", sharedVec);
            Vector4 twirl = new Vector4(.38f, .62f, 0, 0);
            graph.SetVector("_TWParameter", twirl); legacy.SetVector("_TWParameter", twirl);
            graph.SetFloat("_TWStrength", 2.25f); legacy.SetFloat("_TWStrength", 2.25f);
        }

        static void SetWord(Material m, string lo, string hi, uint value)
        { m.SetFloat(lo, value & 65535u); m.SetFloat(hi, value >> 16); }

        static Color[] Capture(Camera camera, RenderTexture target, Texture2D readback, string path)
        {
            for (int i = 0; i < 4; i++) camera.Render();
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = target;
                readback.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
                readback.Apply(false);
                Color[] pixels = readback.GetPixels();
                if (path != null)
                {
                    using (var file = File.Create(path + ".rgba-f32.gz"))
                    using (var gzip = new GZipStream(file, CompressionMode.Compress))
                    using (var writer = new BinaryWriter(gzip))
                        foreach (Color color in pixels)
                        { writer.Write(color.r); writer.Write(color.g); writer.Write(color.b); writer.Write(color.a); }
                    var png = new Texture2D(Size, Size, TextureFormat.RGBA32, false, true);
                    try { png.SetPixels(pixels); png.Apply(false); File.WriteAllBytes(path + ".png", png.EncodeToPNG()); }
                    finally { UnityEngine.Object.DestroyImmediate(png); }
                }
                return pixels;
            }
            finally { RenderTexture.active = previous; }
        }

        static Metrics Compare(Color[] b, Color[] c)
        {
            var metrics = new Metrics { finite = true };
            for (int y = 40; y < 88; y++) for (int x = 40; x < 88; x++)
            {
                int index = y * Size + x;
                metrics.roiPixels++;
                if (b[index].maxColorComponent > .01f) metrics.legacyVisible++;
                if (c[index].maxColorComponent > .01f) metrics.graphVisible++;
                float difference = 0;
                for (int channel = 0; channel < 4; channel++)
                {
                    float bv = b[index][channel], cv = c[index][channel];
                    if (float.IsNaN(bv) || float.IsInfinity(bv) || float.IsNaN(cv) || float.IsInfinity(cv))
                        metrics.finite = false;
                    difference = Mathf.Max(difference, Mathf.Abs(bv - cv));
                }
                if (difference > 0) metrics.differentRGBA++;
                metrics.maxRGBA = Mathf.Max(metrics.maxRGBA, difference);
            }
            return metrics;
        }

        static float MaxDelta(Color[] a, Color[] b)
        { return Compare(a, b).maxRGBA; }
    }
}
