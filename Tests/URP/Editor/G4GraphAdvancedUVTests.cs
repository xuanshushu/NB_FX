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
    /// Ordinary Mesh MainTex UV B/C, limited to packed modes 0/1/2/8.
    /// Full RGBA equality is asserted, not relaxed after a stage/precision
    /// failure. VFX, position modes and first-frame behavior are not tested.
    /// </summary>
    public sealed class G4GraphAdvancedUVTests
    {
        const string GraphPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph";
        const string LegacyPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader";
        const int Size = 128, Layer = 2;

        static readonly string[] Names = {
            "default-uv0", "special-uv0zw", "special-uv1", "special-uv2",
            "twirl", "polar", "twirl-polar", "shared-default",
            "shared-special-uv1", "shared-special-uv2", "shared-twirl"
        };

        static IEnumerable<TestCaseData> Cases()
        {
            foreach (string name in Names)
                foreach (bool ortho in new[] { true, false })
                    yield return new TestCaseData(name, ortho).SetName(
                        "G4AdvancedUVBC_" + name + (ortho ? "_ortho" : "_perspective"));
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

        struct UVCase
        {
            public uint flags0, flags1;
            public int mainMode, sharedMode;
            public Vector4 sharedST, sharedVec, twirlCenter, polarCenter;
            public float twirlStrength;
        }

        [TestCaseSource(nameof(Cases))]
        public void MainTexPackedUVMatchesShaderLab(string name, bool ortho)
        {
            Assert.That(GraphicsSettings.currentRenderPipeline, Is.InstanceOf<UniversalRenderPipelineAsset>());
            Assert.That(SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf), Is.True);
            Shader graphShader = AssetDatabase.LoadAssetAtPath<Shader>(GraphPath);
            Shader legacyShader = AssetDatabase.LoadAssetAtPath<Shader>(LegacyPath);
            Assert.That(graphShader && legacyShader && graphShader.isSupported && legacyShader.isSupported, Is.True);
            string root = Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR");
            if (string.IsNullOrEmpty(root))
                root = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/NBFXG4AdvancedUV");
            string output = Path.Combine(root, name + (ortho ? "-ortho" : "-perspective"));
            Directory.CreateDirectory(output);

            Scene scene = EditorSceneManager.NewPreviewScene();
            GameObject front = GameObject.CreatePrimitive(PrimitiveType.Quad);
            GameObject cameraObject = new GameObject("NBFX ordinary Mesh advanced UV B/C");
            Mesh ownedMesh = UnityEngine.Object.Instantiate(front.GetComponent<MeshFilter>().sharedMesh);
            front.GetComponent<MeshFilter>().sharedMesh = ownedMesh;
            Material graph = new Material(graphShader), legacy = new Material(legacyShader);
            Texture2D map = MakeMap();
            var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            var readback = new Texture2D(Size, Size, TextureFormat.RGBAHalf, false, true);
            var camera = cameraObject.AddComponent<Camera>();
            RenderTexture previousActive = RenderTexture.active;
            try
            {
                SetDistinctUVStreams(ownedMesh);
                SceneManager.MoveGameObjectToScene(front, scene);
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                Configure(graph, true, map);
                Configure(legacy, false, map);
                front.layer = Layer;
                front.transform.localScale = new Vector3(2, 2, 1);
                var renderer = front.GetComponent<MeshRenderer>();
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

                SetProtocol(graph, legacy, Define(name));
                renderer.sharedMaterial = legacy;
                Color[] b = Capture(camera, target, readback, Path.Combine(output, "B-on"));
                Color[] br = Capture(camera, target, readback, null);
                renderer.sharedMaterial = graph;
                Color[] c = Capture(camera, target, readback, Path.Combine(output, "C-on"));
                Color[] cr = Capture(camera, target, readback, null);

                // For the default case the negative control selects UV0.zw;
                // every advanced case turns its routing back to UV0.xy.
                string controlName = name == "default-uv0" ? "special-uv0zw" : "default-uv0";
                SetProtocol(graph, legacy, Define(controlName));
                renderer.sharedMaterial = legacy;
                Color[] bOff = Capture(camera, target, readback, Path.Combine(output, "B-off"));
                renderer.sharedMaterial = graph;
                Color[] cOff = Capture(camera, target, readback, Path.Combine(output, "C-off"));

                Metrics metrics = Compare(b, c);
                Metrics control = Compare(bOff, cOff);
                metrics.caseId = name + (ortho ? "-ortho" : "-perspective");
                metrics.unityVersion = Application.unityVersion;
                metrics.api = SystemInfo.graphicsDeviceType.ToString();
                metrics.target = "linear RGBAHalf, 128x128, strict inner 48x48 ROI";
                metrics.note = "Ordinary Mesh, distinct float4 TEXCOORD0/1/2, nonuniform linear BaseMap, four warm-up renders. Mode 0/1/2/8 only; no VFX, first-frame, CustomData, UI, position or cylinder claim.";
                metrics.orthographic = ortho;
                metrics.graphPasses = graph.passCount;
                metrics.legacyPasses = legacy.passCount;
                metrics.graphRepeat = MaxDelta(c, cr);
                metrics.legacyRepeat = MaxDelta(b, br);
                metrics.graphOnOff = MaxDelta(c, cOff);
                metrics.legacyOnOff = MaxDelta(b, bOff);
                metrics.controlMaxRGBA = control.maxRGBA;
                File.WriteAllText(Path.Combine(output, "metrics.json"), JsonUtility.ToJson(metrics, true));
                Debug.Log("NBFX_G4_ADVANCED_UV_BC " + JsonUtility.ToJson(metrics));
                Assert.That(metrics.finite && control.finite, Is.True, "Non-finite UV color.");
                Assert.That(metrics.graphVisible, Is.GreaterThan(100), "Graph foreground absent.");
                Assert.That(metrics.legacyVisible, Is.GreaterThan(100), "ShaderLab foreground absent.");
                Assert.That(metrics.graphRepeat, Is.Zero, "Graph repeated capture changed.");
                Assert.That(metrics.legacyRepeat, Is.Zero, "ShaderLab repeated capture changed.");
                Assert.That(metrics.graphOnOff, Is.GreaterThan(.03f), "Graph advanced/default control had no effect.");
                Assert.That(metrics.legacyOnOff, Is.GreaterThan(.03f), "ShaderLab advanced/default control had no effect.");
                Assert.That(metrics.differentRGBA, Is.Zero, "Strict on-state RGBA Mesh B/C mismatch.");
                Assert.That(control.differentRGBA, Is.Zero, "Strict off-state RGBA Mesh B/C mismatch.");
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
                    (y + 1f) / (n + 1f), (x + y + 2f) / (2f * n + 2f),
                    (2f * x + y + 3f) / (3f * n + 3f));
            map.SetPixels(pixels); map.Apply(false);
            map.filterMode = FilterMode.Bilinear;
            map.wrapMode = TextureWrapMode.Repeat;
            return map;
        }

        static void Configure(Material m, bool graph, Texture2D map)
        {
            m.SetTexture("_BaseMap", map);
            m.SetTextureScale("_BaseMap", Vector2.one);
            m.SetTextureOffset("_BaseMap", Vector2.zero);
            m.SetColor(graph ? "_Color" : "_BaseColor", Color.white);
            m.SetColor("_ColorA", Color.white);
            m.SetFloat("_BaseColorIntensityForTimeline", 1);
            m.SetFloat("_AlphaAll", 1);
            m.SetFloat("_Cull", 0); m.SetFloat("_ZTest", 4); m.SetFloat("_ZWrite", 0);
            m.SetFloat("_SrcBlend", (float)BlendMode.One);
            m.SetFloat("_DstBlend", (float)BlendMode.Zero);
            m.SetFloat("_BaseMapUVRotation", 0);
            m.SetFloat("_BaseMapUVRotationSpeed", 0);
            m.SetVector("_BaseMapMaskMapOffset", Vector4.zero);
            m.renderQueue = 3000;
            if (graph)
            {
                m.SetVector("_BaseMap_ST", new Vector4(1, 1, 0, 0));
                m.SetFloat("_Surface", 1); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                m.SetFloat("_DstBlendAlpha", (float)BlendMode.Zero);
                m.SetFloat("_NB_DistortionMode", 0);
                m.SetFloat("_NB_ColorChannelLo16", 3);
                m.SetFloat("_NB_WrapFlagsLo16", 0); m.SetFloat("_NB_WrapFlagsHi16", 0);
                m.SetFloat("_NB_ForceNoMipFlagsLo16", 0); m.SetFloat("_NB_ForceNoMipFlagsHi16", 0);
                foreach (string property in new[] { "_NB_UVModeFlag0Lo16", "_NB_UVModeFlag0Hi16",
                    "_NB_UVModeFlagType0Lo16", "_NB_UVModeFlagType0Hi16", "_SharedUV_ST",
                    "_SharedUV_Vec", "_TWParameter", "_TWStrength", "_PCCenter" })
                    Assert.That(m.HasProperty(property), Is.True, "Graph missing " + property);
            }
            else
            {
                m.EnableKeyword("_FX_LIGHT_MODE_UNLIT");
                m.SetFloat("_ColorMask", 15); m.SetFloat("_fogintensity", 0);
                m.SetInteger("_W9ParticleShaderColorChannelFlag", 3);
                m.SetShaderPassEnabled("SRPDefaultUnlit", false);
                m.SetShaderPassEnabled("SRPDEFAULTUNLIT", false);
            }
        }

        static UVCase Define(string name)
        {
            var c = new UVCase {
                flags1 = 1u << 9, mainMode = 0, sharedMode = 0,
                sharedST = new Vector4(1, 1, 0, 0), sharedVec = Vector4.zero,
                twirlCenter = new Vector4(.38f, .62f, 0, 0),
                polarCenter = new Vector4(.47f, .54f, 1, 0),
                twirlStrength = 2.25f
            };
            switch (name)
            {
                case "default-uv0": break;
                case "special-uv0zw": c.mainMode = 1; break;
                case "special-uv1": c.mainMode = 1; c.flags1 |= (1u << 21) | (1u << 18); break;
                case "special-uv2": c.mainMode = 1; c.flags1 |= (1u << 21) | (1u << 19); break;
                case "twirl": c.mainMode = 2; c.flags0 = 1u << 9; break;
                case "polar": c.mainMode = 2; c.flags0 = 1u << 8; break;
                case "twirl-polar": c.mainMode = 2; c.flags0 = (1u << 9) | (1u << 8); break;
                case "shared-default": c.mainMode = 8; SetSharedTransform(ref c); break;
                case "shared-special-uv1": c.mainMode = 8; c.sharedMode = 1;
                    c.flags1 |= (1u << 21) | (1u << 18); SetSharedTransform(ref c); break;
                case "shared-special-uv2": c.mainMode = 8; c.sharedMode = 1;
                    c.flags1 |= (1u << 21) | (1u << 19); SetSharedTransform(ref c); break;
                case "shared-twirl": c.mainMode = 8; c.sharedMode = 2;
                    c.flags0 = 1u << 9; SetSharedTransform(ref c); break;
                default: throw new ArgumentException(name);
            }
            return c;
        }

        static void SetSharedTransform(ref UVCase c)
        {
            c.sharedST = new Vector4(.77f, .85f, .11f, -.08f);
            c.sharedVec = new Vector4(0, 0, 29f, 0);
        }

        static void SetProtocol(Material graph, Material legacy, UVCase c)
        {
            uint modeBits = (uint)(c.mainMode & 3) | ((uint)(c.sharedMode & 3) << 30);
            uint typeBits = (uint)(c.mainMode >> 2) | ((uint)(c.sharedMode >> 2) << 30);
            SetWord(graph, "_NB_Flags0Lo16", "_NB_Flags0Hi16", c.flags0);
            SetWord(graph, "_NB_Flags1Lo16", "_NB_Flags1Hi16", c.flags1);
            SetWord(graph, "_NB_UVModeFlag0Lo16", "_NB_UVModeFlag0Hi16", modeBits);
            SetWord(graph, "_NB_UVModeFlagType0Lo16", "_NB_UVModeFlagType0Hi16", typeBits);
            graph.SetVector("_SharedUV_ST", c.sharedST);
            graph.SetVector("_SharedUV_Vec", c.sharedVec);
            graph.SetVector("_TWParameter", c.twirlCenter);
            graph.SetFloat("_TWStrength", c.twirlStrength);
            graph.SetVector("_PCCenter", c.polarCenter);
            legacy.SetInteger("_W9ParticleShaderFlags", unchecked((int)c.flags0));
            legacy.SetInteger("_W9ParticleShaderFlags1", unchecked((int)c.flags1));
            legacy.SetInteger("_UVModeFlag0", unchecked((int)modeBits));
            legacy.SetInteger("_UVModeFlagType0", unchecked((int)typeBits));
            legacy.SetVector("_SharedUV_ST", c.sharedST);
            legacy.SetVector("_SharedUV_Vec", c.sharedVec);
            legacy.SetVector("_TWParameter", c.twirlCenter);
            legacy.SetFloat("_TWStrength", c.twirlStrength);
            legacy.SetVector("_PCCenter", c.polarCenter);
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
