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
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace NBFX.Baseline.Tests
{
    /// <summary>
    /// Steady-state ordinary-Mesh B/C replay. This is not VFX or a full G4 gate:
    /// it covers named, already implemented surface/depth subpaths only.
    /// White/primary-color tints avoid comparing different SG color UI encodings.
    /// Raw float values read from linear RGBAHalf are retained, not just PNGs.
    /// </summary>
    public sealed class G4GraphMeshParityTests
    {
        const string GraphPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph";
        const string LegacyPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader";
        const int Size = 96;
        const int Layer = 2;
        static readonly string[] Names = {
            "base", "base-alpha-r", "alpha-all", "color-a",
            "overlay1-add", "overlay1-multiply-alpha", "overlay2-add", "overlay2-multiply",
            "ramp-uv-multiply", "ramp-map-add", "mask-one", "mask-three-refine",
            "dissolve-single", "dissolve-process-mask", "dissolve-late-mask",
            "dissolve-line", "dissolve-ramp-map", "adjustment-early", "adjustment-late",
            "fresnel-alpha", "fresnel-color", "distance", "soft", "depth-outline",
            "depth-combination", "surface-combination",
            "base-alpha-g", "base-alpha-b", "base-timeline", "color-multi-alpha", "back-face",
            "vertex-color", "vertex-color-ignore", "overlay1-multiply", "overlay1-add-alpha",
            "overlay2-add-alpha", "overlay2-multiply-alpha", "mask-one-gradient", "mask-three-gradient",
            "dissolve-ramp-gradient", "dissolve-ramp-gradient-multiply", "dissolve-ramp-map-multiply",
            "fresnel-color-alpha", "fresnel-invert"
        };

        static IEnumerable<TestCaseData> Cases()
        {
            foreach (string name in Names)
                foreach (bool ortho in new[] { true, false })
                    yield return new TestCaseData(name, ortho)
                        .SetName("G4MeshBC_" + name + "_" + (ortho ? "ortho" : "perspective"));
        }

        [Serializable] sealed class Metrics
        {
            public string caseId, unityVersion, api, target, stage, note;
            public bool orthographic, depthTextureRequested, finite;
            public int size, differentRGB, differentAlpha, legacyVisible, graphVisible;
            public int legacyPassCount, graphPassCount, legacyWarnings, graphWarnings;
            public float maxRGB, maxAlpha, legacyRepeatMax, graphRepeatMax, featureVsDisabledMax;
        }

        [TestCaseSource(nameof(Cases))]
        public void AlreadyImplementedSubpathMatchesShaderLab(string name, bool ortho)
        { Replay(name, ortho, false); }

        [TestCase("base", true)]
        [TestCase("base", false)]
        [TestCase("surface-combination", true)]
        [TestCase("surface-combination", false)]
        public void MasterPreviewMatchesRuntimeGraphOnOrdinaryMesh(string name, bool ortho)
        { Replay(name, ortho, true); }

        void Replay(string name, bool ortho, bool preview)
        {
            Assert.That(SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf), Is.True);
            var runtimeGraphShader = AssetDatabase.LoadAssetAtPath<Shader>(GraphPath);
            var graphShader = runtimeGraphShader;
            var legacyShader = AssetDatabase.LoadAssetAtPath<Shader>(LegacyPath);
            Assert.That(graphShader && legacyShader && graphShader.isSupported && legacyShader.isSupported, Is.True);
            Assert.That(GraphicsSettings.currentRenderPipeline, Is.InstanceOf<UniversalRenderPipelineAsset>());

            string root = Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR");
            if (string.IsNullOrEmpty(root))
                root = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/NBFXG4MeshBC");
            string id = (preview ? "preview-" : "") + name + "-" + (ortho ? "ortho" : "perspective");
            string output = Path.Combine(root, id);
            Directory.CreateDirectory(output);
            if (preview)
            {
                graphShader = GenerateMasterPreview(Path.Combine(output, "generated-preview.shader"));
                legacyShader = runtimeGraphShader;
            }
            var scene = EditorSceneManager.NewPreviewScene();
            var graph = new Material(graphShader);
            var legacy = new Material(legacyShader);
            var disabled = new Material(graphShader);
            var texture = MakeTexture();
            var front = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var ownedMesh = UnityEngine.Object.Instantiate(front.GetComponent<MeshFilter>().sharedMesh);
            front.GetComponent<MeshFilter>().sharedMesh = ownedMesh;
            if (name.StartsWith("vertex-color", StringComparison.Ordinal))
                ownedMesh.colors = Enumerable.Repeat(new Color(1, 0, 1, .5f), ownedMesh.vertexCount).ToArray();
            var board = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var cameraObject = new GameObject("NBFX ordinary Mesh B/C camera");
            var camera = cameraObject.AddComponent<Camera>();
            var boardShader = Shader.Find("Universal Render Pipeline/Unlit");
            Assert.That(boardShader, Is.Not.Null);
            var opaque = new Material(boardShader);
            var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            var readback = new Texture2D(Size, Size, TextureFormat.RGBAHalf, false, true);
            var previous = RenderTexture.active;
            try
            {
                foreach (var go in new[] { front, board, cameraObject })
                    SceneManager.MoveGameObjectToScene(go, scene);
                Configure(graph, true, texture);
                Configure(disabled, true, texture);
                disabled.SetFloat("_NB_Flags1Lo16", name == "vertex-color" ? 1 | (1 << 9) : 1);
                Configure(legacy, preview, texture);
                uint flags0 = 0, flags1 = 1;
                ConfigureCase(name, graph, legacy, ref flags0, ref flags1, texture);
                SetFlags(graph, legacy, flags0, flags1);
                if (preview) SetGraphFlags(legacy, flags0, flags1);

                front.layer = board.layer = Layer;
                front.transform.position = new Vector3(0, 0, .5f);
                front.transform.localScale = new Vector3(2, 2, 1);
                board.transform.position = new Vector3(0, 0, .45f);
                board.transform.localScale = new Vector3(2, 2, 1);
                opaque.SetColor("_BaseColor", Color.white);
                opaque.SetFloat("_Cull", 0);
                board.GetComponent<MeshRenderer>().sharedMaterial = opaque;
                board.SetActive(name == "soft" || name.StartsWith("depth-", StringComparison.Ordinal));
                var renderer = front.GetComponent<MeshRenderer>();
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                camera.scene = scene;
                camera.orthographic = ortho;
                camera.orthographicSize = 1.5f;
                camera.fieldOfView = 45;
                camera.nearClipPlane = .1f;
                camera.farClipPlane = 20;
                camera.transform.position = new Vector3(0, 0, 5);
                camera.transform.rotation = Quaternion.Euler(0, 180, 0);
                camera.cullingMask = 1 << Layer;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.allowHDR = true;
                camera.allowMSAA = false;
                camera.GetUniversalAdditionalCameraData().requiresDepthTexture = true;
                target.Create();
                Assert.That(target.IsCreated() && !target.sRGB, Is.True);
                camera.targetTexture = target;
                renderer.enabled = false;
                var empty = Capture(camera, target, readback, null);
                renderer.enabled = true;
                renderer.sharedMaterial = legacy;
                var b = Capture(camera, target, readback, Path.Combine(output, "B"));
                var bRepeat = Capture(camera, target, readback, null);
                renderer.sharedMaterial = graph;
                var c = Capture(camera, target, readback, Path.Combine(output, "C"));
                var cRepeat = Capture(camera, target, readback, null);
                renderer.sharedMaterial = disabled;
                var off = Capture(camera, target, readback, Path.Combine(output, "C-disabled"));
                WriteMaterial(Path.Combine(output, "B-material.json"), legacy);
                WriteMaterial(Path.Combine(output, "C-material.json"), graph);
                File.WriteAllBytes(Path.Combine(output, "base-texture.png"), texture.EncodeToPNG());
                var metrics = Compare(b, c);
                metrics.featureVsDisabledMax = MaxDelta(c, off);
                metrics.caseId = id;
                metrics.unityVersion = Application.unityVersion;
                metrics.api = SystemInfo.graphicsDeviceType.ToString();
                metrics.target = "linear RGBAHalf read as float32 RGBA";
                metrics.stage = preview ? "ordinary Mesh runtime Graph / generated master Preview; not visible Graph window or VFX" : "ordinary Mesh steady-state B/C; not first-frame or VFX";
                metrics.note = "Static UV0 subpaths, zero animation speeds, no lighting/fog. Four renders precede each capture. No source material, Renderer or project setting is saved.";
                metrics.size = Size;
                metrics.orthographic = ortho;
                metrics.depthTextureRequested = true;
                metrics.legacyVisible = Visible(empty, b);
                metrics.graphVisible = Visible(empty, c);
                metrics.legacyRepeatMax = MaxDelta(b, bRepeat);
                metrics.graphRepeatMax = MaxDelta(c, cRepeat);
                metrics.legacyPassCount = legacy.passCount;
                metrics.graphPassCount = graph.passCount;
                metrics.legacyWarnings = ShaderUtil.GetShaderMessages(legacyShader).Length;
                metrics.graphWarnings = ShaderUtil.GetShaderMessages(graphShader).Length;
                File.WriteAllText(Path.Combine(output, "shader-messages.txt"), string.Join("\n", ShaderUtil.GetShaderMessages(graphShader).Select(m => m.severity + ": " + m.message)));
                Assert.That(ShaderUtil.GetShaderMessages(graphShader).Any(m => m.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error), Is.False, "Shader compiler error.");
                File.WriteAllText(Path.Combine(output, "metrics.json"), JsonUtility.ToJson(metrics, true));
                Debug.Log("NBFX_G4_MESH_BC " + JsonUtility.ToJson(metrics));
                Assert.That(metrics.finite, Is.True, "NaN/Inf is never a color tolerance.");
                Assert.That(metrics.legacyVisible, Is.GreaterThan(100), "Legacy positive control failed.");
                Assert.That(metrics.graphVisible, Is.GreaterThan(100), "Graph positive control failed.");
                Assert.That(metrics.legacyRepeatMax, Is.Zero, "Legacy repeat noise: parity cannot be judged.");
                Assert.That(metrics.graphRepeatMax, Is.Zero, "Graph repeat noise: parity cannot be judged.");
                if (name != "base")
                    Assert.That(metrics.featureVsDisabledMax, Is.GreaterThan(.01f), "Configured feature had no measurable effect; an inactive fixture is not evidence.");
                Assert.That(metrics.differentAlpha, Is.Zero, "Strict Alpha B/C mismatch; see raw captures.");
                Assert.That(metrics.differentRGB, Is.Zero, "Strict RGB B/C mismatch; see raw captures.");
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                target.Release();
                foreach (var obj in new UnityEngine.Object[] { graph, legacy, disabled, texture, opaque, target, readback, ownedMesh })
                    UnityEngine.Object.DestroyImmediate(obj);
                EditorSceneManager.ClosePreviewScene(scene);
                if (preview) UnityEngine.Object.DestroyImmediate(graphShader);
            }
        }

        // Current SG 17.3 test-only reflection mirrors PreviewManager. No product
        // reflection, official-file edit or Preview-window attendance is required.
        static Shader GenerateMasterPreview(string output)
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Unity.ShaderGraph.Editor");
            var importer = assembly.GetType("UnityEditor.ShaderGraph.ShaderGraphImporter", true);
            var parse = importer.GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                .Single(m => m.Name == "GetShaderText" && m.GetParameters().Length == 4 && m.GetParameters()[3].IsOut);
            object[] parseArgs = { GraphPath, null, null, null };
            parse.Invoke(null, parseArgs);
            var graphData = parseArgs[3];
            Assert.That(graphData, Is.Not.Null);
            var generatorType = assembly.GetType("UnityEditor.ShaderGraph.Generator", true);
            var modeType = assembly.GetType("UnityEditor.ShaderGraph.GenerationMode", true);
            var node = graphData.GetType().GetProperty("outputNode", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(graphData);
            var constructor = generatorType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Single(c => c.GetParameters().Length == 8);
            var generator = constructor.Invoke(new[] { graphData, node, Enum.Parse(modeType, "Preview"), "NBFXMeshMasterPreview", null, null, (object)true, false });
            string source = (string)generatorType.GetProperty("generatedShader").GetValue(generator);
            Assert.That(source, Does.Contain("SHADERGRAPH_PREVIEW"));
            File.WriteAllText(output, source);
            var shader = ShaderUtil.CreateShaderAsset(source, false);
            Assert.That(shader, Is.Not.Null);
            shader.hideFlags = HideFlags.HideAndDontSave;
            return shader;
        }

        static Texture2D MakeTexture()
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
            texture.SetPixels32(new[] { new Color32(64, 128, 160, 192) });
            texture.Apply(false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }

        static void Configure(Material m, bool graph, Texture2D texture)
        {
            m.SetTexture("_BaseMap", texture);
            m.SetColor(graph ? "_Color" : "_BaseColor", Color.white);
            m.SetColor("_ColorA", Color.white);
            m.SetFloat("_BaseColorIntensityForTimeline", 1);
            m.SetFloat("_AlphaAll", 1);
            m.SetFloat("_Cull", 0);
            m.SetFloat("_ZTest", 4);
            m.SetFloat("_ZWrite", 0);
            m.SetFloat("_SrcBlend", (float)BlendMode.One);
            m.SetFloat("_DstBlend", (float)BlendMode.Zero);
            if (graph)
            {
                m.SetFloat("_Surface", 1);
                m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                m.SetFloat("_DstBlendAlpha", (float)BlendMode.Zero);
                m.SetFloat("_NB_DistortionMode", 0);
                m.SetFloat("_NB_ColorChannelLo16", 3);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            else
            {
                m.EnableKeyword("_FX_LIGHT_MODE_UNLIT");
                m.SetFloat("_ColorMask", 15);
                m.SetFloat("_fogintensity", 0);
                m.SetInteger("_W9ParticleShaderColorChannelFlag", 3);
                m.SetShaderPassEnabled("SRPDefaultUnlit", false);
                m.SetShaderPassEnabled("SRPDEFAULTUNLIT", false);
            }
            m.renderQueue = 3000;
        }

        static void Enable(Material graph, Material legacy, string property, string keyword)
        {
            Assert.That(graph.HasProperty(property), Is.True, "Missing Graph property " + property);
            graph.SetFloat(property, 1);
            if (legacy.HasProperty(property)) legacy.SetFloat(property, 1);
            legacy.EnableKeyword(keyword);
        }
        static void Vector(Material a, Material b, string property, Vector4 value)
        { a.SetVector(property, value); b.SetVector(property, value); }
        static void ColorValue(Material a, Material b, string property, Color value)
        { a.SetColor(property, value); b.SetColor(property, value); }
        static void Texture(Material a, Material b, string property, UnityEngine.Texture value)
        { a.SetTexture(property, value); b.SetTexture(property, value); }
        static void SetFlags(Material graph, Material legacy, uint flags0, uint flags1)
        {
            SetGraphFlags(graph, flags0, flags1);
            legacy.SetInteger("_W9ParticleShaderFlags", unchecked((int)flags0));
            legacy.SetInteger("_W9ParticleShaderFlags1", unchecked((int)flags1));
        }

        static void SetGraphFlags(Material graph, uint flags0, uint flags1)
        {
            graph.SetFloat("_NB_Flags0Lo16", flags0 & 65535);
            graph.SetFloat("_NB_Flags0Hi16", flags0 >> 16);
            graph.SetFloat("_NB_Flags1Lo16", flags1 & 65535);
            graph.SetFloat("_NB_Flags1Hi16", flags1 >> 16);
        }

        static void ConfigureCase(string name, Material g, Material b, ref uint f0, ref uint f1, Texture2D tex)
        {
            bool combo = name == "surface-combination";
            if (name.StartsWith("base-alpha-", StringComparison.Ordinal))
            { uint channel = name.EndsWith("r", StringComparison.Ordinal) ? 0u : name.EndsWith("g", StringComparison.Ordinal) ? 1u : 2u;
              g.SetFloat("_NB_ColorChannelLo16", channel); b.SetInteger("_W9ParticleShaderColorChannelFlag", (int)channel); }
            if (name == "base-timeline") { g.SetFloat("_BaseColorIntensityForTimeline", .5f); b.SetFloat("_BaseColorIntensityForTimeline", .5f); }
            if (name == "color-multi-alpha") { f0 |= 1u << 29; ColorValue(g, b, "_ColorA", new Color(1, 1, 1, .5f)); }
            if (name == "back-face") { f0 |= 1u << 28; ColorValue(g, b, "_BaseBackColor", Color.red); }
            if (name == "vertex-color-ignore") f1 |= 1u << 9;
            if (name == "alpha-all") { g.SetFloat("_AlphaAll", .5f); b.SetFloat("_AlphaAll", .5f); }
            if (name == "color-a") ColorValue(g, b, "_ColorA", new Color(1, 0, 1, .5f));
            if (name.StartsWith("overlay1", StringComparison.Ordinal) || combo)
            {
                Enable(g, b, "_EmissionEnabled", "_EMISSION");
                Texture(g, b, "_EmissionMap", tex);
                ColorValue(g, b, "_EmissionMapColor", Color.white);
                if (name.Contains("multiply")) f0 |= 1u << 5;
                if (name.Contains("alpha")) f1 |= 1u << 31;
            }
            if (name.StartsWith("overlay2", StringComparison.Ordinal) || combo)
            {
                Enable(g, b, "_ColorBlendMap_Toggle", "_COLORMAPBLEND");
                Texture(g, b, "_ColorBlendMap", tex);
                ColorValue(g, b, "_ColorBlendColor", Color.white);
                if (name.Contains("add") || combo) f1 |= 1u << 16;
                if (name.Contains("alpha")) f0 |= 1u << 25;
            }
            if (name.StartsWith("ramp-", StringComparison.Ordinal))
            {
                Enable(g, b, "_RampColorToggle", "_COLOR_RAMP");
                g.SetFloat("_RampColorCount", 131074); b.SetInteger("_RampColorCount", 131074);
                ColorValue(g, b, "_RampColor0", new Color(1, 0, 0, 0));
                ColorValue(g, b, "_RampColor1", new Color(0, 1, 0, 1));
                Vector(g, b, "_RampColorAlpha0", new Vector4(1, 0, .5f, 1));
                ColorValue(g, b, "_RampColorBlendColor", Color.white);
                if (name == "ramp-map-add")
                {
                    f0 |= 1u << 24; g.SetFloat("_RampColorSourceMode", 1);
                    b.EnableKeyword("_COLOR_RAMP_MAP"); Texture(g, b, "_RampColorMap", tex);
                }
            }
            if (name.StartsWith("mask-", StringComparison.Ordinal) || combo)
            {
                Enable(g, b, "_Mask_Toggle", "_MASKMAP_ON");
                Texture(g, b, "_MaskMap", tex);
                if (name.StartsWith("mask-three", StringComparison.Ordinal))
                {
                    Enable(g, b, "_Mask2_Toggle", "_MASKMAP2_ON");
                    Enable(g, b, "_Mask3_Toggle", "_MASKMAP3_ON");
                    Texture(g, b, "_MaskMap2", tex); Texture(g, b, "_MaskMap3", tex);
                    if (name == "mask-three-refine") { f1 |= 1u << 7; Vector(g, b, "_MaskRefineVec", new Vector4(1, 2, .25f, 0)); }
                }
            }
            if (name.Contains("gradient") && name.StartsWith("mask-", StringComparison.Ordinal))
            {
                f1 |= 1u << 2;
                Vector(g, b, "_MaskMapGradientFloat0", new Vector4(0, 0, 1, 1));
                g.SetFloat("_MaskMapGradientCount", 2); b.SetInteger("_MaskMapGradientCount", 2);
                if (name == "mask-three-gradient")
                { f1 |= (1u << 3) | (1u << 4);
                  Vector(g, b, "_MaskMap2GradientFloat0", new Vector4(0, 0, 1, 1));
                  Vector(g, b, "_MaskMap3GradientFloat0", new Vector4(0, 0, 1, 1));
                  g.SetFloat("_MaskMap2GradientCount", 2); b.SetInteger("_MaskMap2GradientCount", 2);
                  g.SetFloat("_MaskMap3GradientCount", 2); b.SetInteger("_MaskMap3GradientCount", 2); }
            }
            if (name.StartsWith("dissolve-", StringComparison.Ordinal) || combo)
            {
                Enable(g, b, "_Dissolve_Toggle", "_DISSOLVE");
                Texture(g, b, "_DissolveMap", tex);
                Vector(g, b, "_Dissolve", new Vector4(.4f, 1, .5f, .5f));
                if (name.Contains("mask"))
                {
                    Enable(g, b, "_DissolveMask_Toggle", "_DISSOLVE_MASK");
                    Texture(g, b, "_DissolveMaskMap", name == "dissolve-process-mask" ? Texture2D.whiteTexture : tex);
                    float mode = name == "dissolve-late-mask" ? 1 : 0;
                    g.SetFloat("_DissolveMaskMode", mode); b.SetFloat("_DissolveMaskMode", mode);
                }
                if (name == "dissolve-line")
                { f1 |= 1u << 5; Vector(g, b, "_Dissolve_Vec2", new Vector4(.5f, .3f, 0, 0)); ColorValue(g, b, "_DissolveLineColor", Color.red); }
                if (name.StartsWith("dissolve-ramp-", StringComparison.Ordinal))
                {
                    Enable(g, b, "_Dissolve_useRampMap_Toggle", "_DISSOLVE_RAMP");
                    bool map = name.Contains("map");
                    g.SetFloat("_DissolveRampSourceMode", map ? 1 : 0);
                    if (map) b.EnableKeyword("_DISSOLVE_RAMP_MAP");
                    if (name.Contains("multiply")) f1 |= 1u << 6;
                    Texture(g, b, "_DissolveRampMap", Texture2D.whiteTexture); ColorValue(g, b, "_DissolveRampColor", Color.red);
                    g.SetFloat("_DissolveRampCount", 131074); b.SetInteger("_DissolveRampCount", 131074);
                    ColorValue(g, b, "_DissolveRampColor0", new Color(1, 0, 0, 0));
                    ColorValue(g, b, "_DissolveRampColor1", new Color(0, 1, 0, 1));
                    Vector(g, b, "_DissolveRampAlpha0", new Vector4(1, 0, 1, 1));
                }
            }
            if (name.StartsWith("adjustment-", StringComparison.Ordinal))
            {
                ColorValue(g, b, "_ColorA", new Color(1, 0, 1, 1));
                f0 |= (1u << 19) | 1u;
                f1 |= (1u << 24) | (1u << 27);
                if (name == "adjustment-early") f0 |= 1u << 22;
                g.SetFloat("_HueShift", .25f); b.SetFloat("_HueShift", .25f);
                g.SetFloat("_Saturability", .5f); b.SetFloat("_Saturability", .5f);
                g.SetFloat("_Contrast", .5f); b.SetFloat("_Contrast", .5f);
                ColorValue(g, b, "_ContrastMidColor", Color.white);
                Vector(g, b, "_BaseMapColorRefine", new Vector4(1, 1, 1, 0));
            }
            if (name.StartsWith("fresnel-", StringComparison.Ordinal) || name == "depth-combination")
            {
                Enable(g, b, "_fresnelEnabled", "_FRESNEL");
                Vector(g, b, "_FresnelUnit", new Vector4(.5f, 1, .75f, 0));
                ColorValue(g, b, "_FresnelColor", Color.red);
                if (!name.StartsWith("fresnel-color", StringComparison.Ordinal)) f0 |= 1u << 2;
                if (name == "fresnel-color-alpha") f0 |= 1u << 13;
                if (name == "fresnel-invert") f0 |= 1u << 18;
            }
            if (name == "distance" || name == "depth-combination")
            { Enable(g, b, "_DistanceFade_Toggle", "_DISTANCE_FADE"); Vector(g, b, "_Fade", new Vector4(4, 6, 0, 0)); }
            if (name == "soft" || name == "depth-combination")
            { Enable(g, b, "_SoftParticlesEnabled", "_SOFTPARTICLES_ON"); Vector(g, b, "_SoftParticleFadeParams", new Vector4(0, 1, 0, 0)); }
            if (name == "depth-outline" || name == "depth-combination")
            { Enable(g, b, "_DepthOutline_Toggle", "_DEPTH_OUTLINE"); Vector(g, b, "_DepthOutline_Vec", new Vector4(0, 1, 0, 0)); ColorValue(g, b, "_DepthOutline_Color", Color.red); }
        }

        [Serializable] sealed class PropertyInput
        { public string name, type, texture; public float scalar; public Vector4 vector; }
        [Serializable] sealed class MaterialInput
        { public string shader; public int queue; public string[] keywords; public List<PropertyInput> properties = new List<PropertyInput>(); }
        static void WriteMaterial(string path, Material material)
        {
            var state = new MaterialInput { shader = material.shader.name, queue = material.renderQueue, keywords = material.shaderKeywords };
            for (int i = 0; i < ShaderUtil.GetPropertyCount(material.shader); i++)
            {
                string name = ShaderUtil.GetPropertyName(material.shader, i);
                var type = ShaderUtil.GetPropertyType(material.shader, i);
                var p = new PropertyInput { name = name, type = type.ToString() };
                switch (type)
                {
                    case ShaderUtil.ShaderPropertyType.Color: p.vector = material.GetColor(name); break;
                    case ShaderUtil.ShaderPropertyType.Vector: p.vector = material.GetVector(name); break;
                    case ShaderUtil.ShaderPropertyType.TexEnv:
                        var texture = material.GetTexture(name); p.texture = texture ? texture.name + "|" + AssetDatabase.GetAssetPath(texture) : "null";
                        var scale = material.GetTextureScale(name); var offset = material.GetTextureOffset(name);
                        p.vector = new Vector4(scale.x, scale.y, offset.x, offset.y); break;
                    case ShaderUtil.ShaderPropertyType.Int: p.scalar = material.GetInteger(name); break;
                    default: p.scalar = material.GetFloat(name); break;
                }
                state.properties.Add(p);
            }
            File.WriteAllText(path, JsonUtility.ToJson(state, true));
        }

        static Color[] Capture(Camera camera, RenderTexture target, Texture2D readback, string path)
        {
            // Deliberate steady-state fixture; not evidence about the waived cold first frame.
            for (int i = 0; i < 4; i++) camera.Render();
            var previous = RenderTexture.active;
            try
            {
                RenderTexture.active = target;
                readback.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
                readback.Apply(false);
                var pixels = readback.GetPixels();
                if (path != null)
                {
                    using (var file = File.Create(path + ".rgba-f32.gz"))
                    using (var gzip = new GZipStream(file, CompressionMode.Compress))
                    using (var writer = new BinaryWriter(gzip))
                        foreach (var p in pixels) { writer.Write(p.r); writer.Write(p.g); writer.Write(p.b); writer.Write(p.a); }
                    var preview = new Texture2D(Size, Size, TextureFormat.RGBA32, false, true);
                    try { preview.SetPixels(pixels); preview.Apply(false); File.WriteAllBytes(path + ".png", preview.EncodeToPNG()); }
                    finally { UnityEngine.Object.DestroyImmediate(preview); }
                }
                return pixels;
            }
            finally { RenderTexture.active = previous; }
        }
        static Metrics Compare(Color[] b, Color[] c)
        {
            var result = new Metrics { finite = true };
            for (int i = 0; i < b.Length; i++)
            {
                float rgb = Mathf.Max(Mathf.Abs(b[i].r - c[i].r), Mathf.Abs(b[i].g - c[i].g), Mathf.Abs(b[i].b - c[i].b));
                float alpha = Mathf.Abs(b[i].a - c[i].a);
                if (rgb != 0) result.differentRGB++;
                if (alpha != 0) result.differentAlpha++;
                result.maxRGB = Mathf.Max(result.maxRGB, rgb); result.maxAlpha = Mathf.Max(result.maxAlpha, alpha);
                for (int k = 0; k < 4; k++)
                    if (float.IsNaN(b[i][k]) || float.IsInfinity(b[i][k]) || float.IsNaN(c[i][k]) || float.IsInfinity(c[i][k])) result.finite = false;
            }
            return result;
        }
        static float MaxDelta(Color[] a, Color[] b)
        { var d = Compare(a, b); return Mathf.Max(d.maxRGB, d.maxAlpha); }
        static int Visible(Color[] empty, Color[] value)
        { int n = 0; for (int i = 0; i < value.Length; i++) if (Mathf.Max(Mathf.Abs(value[i].r - empty[i].r), Mathf.Abs(value[i].g - empty[i].g), Mathf.Abs(value[i].b - empty[i].b)) > .02f) n++; return n; }
    }
}
