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
    /// <summary>
    /// PN2 ordinary Mesh only. The same signed Noise RG is blended with PN1
    /// before the NoiseMask; the screen passes keep signed RG unmasked, while
    /// the ordinary Forward texture offset gets the one mask multiplication.
    /// This fixture never edits a renderer asset on disk or claims VFX/Player.
    /// </summary>
    public sealed class G4GraphPNoiseScreenTests
    {
        const string GraphPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph";
        const string LegacyPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader";
        const string Deferred = "NBDeferredDistortPass";
        const string Opaque = "NBCameraOpaqueDistortPass";
        const int Size = 128, BackgroundLayer = 2, ForegroundLayer = 3;
        const int RoiMin = 40, RoiMax = 88;

        [OneTimeSetUp]
        public void ForceGraphImportAfterAssemblyReload() => AssetDatabase.ImportAsset(GraphPath,
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

        static IEnumerable<TestCaseData> Cases()
        {
            foreach (string pass in new[] { Deferred, Opaque })
            {
                for (int mode = 0; mode <= 5; mode++)
                    yield return new TestCaseData(pass, mode, "both", false).SetName(
                        $"G4PN2_{pass}_both_blend{mode}");
                foreach (string kind in new[] { "simple", "voronoi" })
                    yield return new TestCaseData(pass, 1, kind, false).SetName(
                        $"G4PN2_{pass}_{kind}_blend1");
                yield return new TestCaseData(pass, 3, "both", true).SetName(
                    $"G4PN2_{pass}_both_blend3_opacity0");
            }
            foreach (int mode in new[] { 1, 3 })
                yield return new TestCaseData("Forward", mode, "both", false).SetName(
                    $"G4PN2_Forward_masked_blend{mode}");
        }

        [Serializable]
        sealed class Metrics
        {
            public string route, kind, unityVersion, graphicsApi, note;
            public int mode, roiPixels, abcDifferentRGBA, maskDifferentRGBA, legacyVisible, graphVisible;
            public bool finite, maskRatioApplicable;
            public float maxBC, maxMaskBC, maxAOffC, graphRepeat, legacyRepeat,
                legacyOnOff, graphOnOff, graphMaskResponse, legacyMaskResponse,
                modeZeroDelta, wrongModeDelta, graphSignedMaskDeltaR,
                graphSignedMaskDeltaG, legacySignedMaskDeltaR, legacySignedMaskDeltaG,
                graphNoTexturePNoiseDelta, legacyNoTexturePNoiseDelta;
        }

        [TestCaseSource(nameof(Cases))]
        public void ProceduralNoiseFeedsOriginalNoiseAndScreenPasses(
            string route, int mode, string kind, bool opacityZero)
        {
            Assert.That(GraphicsSettings.currentRenderPipeline, Is.InstanceOf<UniversalRenderPipelineAsset>());
            Assert.That(GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>()
                .enableRenderCompatibilityMode, Is.False);
            Assert.That(SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf), Is.True);
            Shader graphShader = AssetDatabase.LoadAssetAtPath<Shader>(GraphPath);
            Shader legacyShader = AssetDatabase.LoadAssetAtPath<Shader>(LegacyPath);
            Assert.That(graphShader && legacyShader && graphShader.isSupported && legacyShader.isSupported, Is.True);
            var rendererData = ((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline).rendererDataList[0];
            Assert.That(rendererData, Is.Not.Null);
            ScriptableRendererFeature nbFeature = FindNBPostProcess(rendererData);
            Assert.That(nbFeature, Is.Not.Null);
            bool nbWasActive = nbFeature.isActive;
            string rendererAsset = Path.Combine(Path.GetDirectoryName(Application.dataPath),
                AssetDatabase.GetAssetPath(rendererData));
            byte[] rendererBefore = File.ReadAllBytes(rendererAsset);
            string evidence = Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR");
            if (string.IsNullOrEmpty(evidence))
                evidence = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/NBFXG4PN2");
            string folder = Path.Combine(evidence, route + "-" + kind + "-blend" + mode +
                (opacityZero ? "-opacity0" : ""));
            Directory.CreateDirectory(folder);

            Scene scene = EditorSceneManager.NewPreviewScene();
            GameObject background = GameObject.CreatePrimitive(PrimitiveType.Quad);
            GameObject foreground = GameObject.CreatePrimitive(PrimitiveType.Quad);
            GameObject cameraObject = new GameObject("PN2 ordinary Mesh camera");
            Material graph = new Material(graphShader), legacy = new Material(legacyShader);
            Shader backgroundShader = Shader.Find("Universal Render Pipeline/Unlit");
            Assert.That(backgroundShader, Is.Not.Null);
            Material backdrop = new Material(backgroundShader);
            Texture2D backdropMap = MakeGradient(), noise = MakeConstant(new Color(.75f, .25f, 0, .5f));
            Texture2D mask = MakeConstant(new Color(.5f, 0, 0, 1));
            Texture2D whiteMask = MakeConstant(Color.white);
            Texture2D baseMap = MakeGradient();
            var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            var readback = new Texture2D(Size, Size, TextureFormat.RGBAHalf, false, true);
            Camera camera = cameraObject.AddComponent<Camera>();
            var cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            RenderTexture previousActive = RenderTexture.active;
            G4ScreenNoiseDirectedFeature directed = null;
            try
            {
                SceneManager.MoveGameObjectToScene(background, scene);
                SceneManager.MoveGameObjectToScene(foreground, scene);
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                backdrop.SetTexture("_BaseMap", backdropMap);
                backdrop.SetColor("_BaseColor", Color.white);
                backdrop.SetFloat("_Cull", (float)CullMode.Off);
                backdrop.renderQueue = 2000;
                background.layer = BackgroundLayer;
                background.transform.position = new Vector3(0, 0, 1);
                background.transform.localScale = new Vector3(6, 6, 1);
                background.GetComponent<MeshRenderer>().sharedMaterial = backdrop;
                background.GetComponent<MeshRenderer>().enabled = route != Deferred;
                foreground.layer = route == "Forward" ? 4 : ForegroundLayer;
                // The existing renderer excludes layer 3 from its regular
                // transparent list (mask 55). Directed NB lists still use 3.
                // Pick its allowed layer 4 for Forward, never edit the asset.
                if (route == "Forward")
                    Assert.That(((UniversalRendererData)rendererData).transparentLayerMask.value & (1 << foreground.layer), Is.Not.Zero);
                foreground.transform.position = new Vector3(0, 0, 2);
                foreground.transform.localScale = new Vector3(2, 2, 1);
                MeshRenderer renderer = foreground.GetComponent<MeshRenderer>();
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                Configure(graph, true, route, noise, mask, baseMap);
                Configure(legacy, false, route, noise, mask, baseMap);
                Apply(graph, true, mode, kind, opacityZero, true);
                Apply(legacy, false, mode, kind, opacityZero, true);

                camera.scene = scene;
                camera.orthographic = true;
                camera.orthographicSize = 1.5f;
                camera.nearClipPlane = .1f;
                camera.farClipPlane = 20;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
                camera.allowHDR = true;
                camera.allowMSAA = false;
                camera.cullingMask = (1 << BackgroundLayer) | (1 << foreground.layer);
                camera.transform.position = new Vector3(0, 0, 5);
                camera.transform.rotation = Quaternion.Euler(0, 180, 0);
                camera.targetTexture = target;
                cameraData.requiresColorTexture = true;
                cameraData.renderPostProcessing = false;
                target.Create();
                Assert.That(target.IsCreated() && !target.sRGB, Is.True);

                nbFeature.SetActive(false);
                if (route != "Forward")
                {
                    directed = ScriptableObject.CreateInstance<G4ScreenNoiseDirectedFeature>();
                    directed.hideFlags = HideFlags.HideAndDontSave;
                    directed.targetCamera = camera;
                    directed.selectedPass = route;
                    directed.Create();
                    directed.SetActive(true);
                    rendererData.rendererFeatures.Add(directed);
                    rendererData.SetDirty();
                }
                renderer.enabled = false;
                camera.Render(); // Renderer rebuild, Graph import and opaque-copy warm-up.
                Color[] baseline = Capture(camera, target, readback, Path.Combine(folder, "background"));
                renderer.enabled = true;
                renderer.sharedMaterial = graph;
                // This render must exercise the Graph's *real* selected
                // Forward/NB pass, not just the background renderer. A fresh
                // import can initially expose Unity's placeholder shader.
                Capture(camera, target, readback, Path.Combine(folder, "C-warm"));
                bool hasUniversalMetadata = false;
                foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(GraphPath))
                    if (asset && asset.GetType().FullName ==
                        "UnityEditor.Rendering.Universal.ShaderGraph.UniversalMetadata")
                        hasUniversalMetadata = true;
                Assert.That(hasUniversalMetadata, Is.True,
                    "Graph importer has not produced URP target metadata after warm render.");
                Assert.That(graph.HasProperty("_DistortPNoiseBlendOpacity"), Is.True,
                    "Graph material lacks the PN2 property after real Graph warm render.");
                graph.SetFloat("_DistortPNoiseBlendOpacity", opacityZero ? 0 : 1);

                Apply(legacy, false, mode, kind, opacityZero, false);
                renderer.sharedMaterial = legacy;
                Color[] a = Capture(camera, target, readback, Path.Combine(folder, "A-legacy-off"));
                Apply(legacy, false, mode, kind, opacityZero, true);
                Color[] b = Capture(camera, target, readback, Path.Combine(folder, "B-legacy-on"));
                Color[] br = Capture(camera, target, readback, Path.Combine(folder, "B-repeat"));
                renderer.sharedMaterial = graph;
                Color[] c = Capture(camera, target, readback, Path.Combine(folder, "C-graph-on"));
                Color[] cr = Capture(camera, target, readback, Path.Combine(folder, "C-repeat"));
                Apply(graph, true, mode, kind, opacityZero, false);
                Color[] cOff = Capture(camera, target, readback, Path.Combine(folder, "C-graph-off"));
                Apply(graph, true, mode, kind, opacityZero, true);

                graph.SetTexture("_NoiseMaskMap", whiteMask);
                legacy.SetTexture("_NoiseMaskMap", whiteMask);
                renderer.sharedMaterial = legacy;
                Color[] bMask = Capture(camera, target, readback, Path.Combine(folder, "B-mask1"));
                renderer.sharedMaterial = graph;
                Color[] cMask = Capture(camera, target, readback, Path.Combine(folder, "C-mask1"));
                graph.SetTexture("_NoiseMaskMap", mask);
                legacy.SetTexture("_NoiseMaskMap", mask);

                // In the old shader the PNoise distortion blend is nested
                // inside _NOISEMAP. Disabling texture Noise must not create
                // a procedural-only distortion path in either host.
                graph.SetFloat("_noisemapEnabled", 0);
                legacy.DisableKeyword("_NOISEMAP");
                renderer.sharedMaterial = legacy;
                Color[] bNoTextureOn = Capture(camera, target, readback,
                    Path.Combine(folder, "B-no-texture-pnoise-on"));
                renderer.sharedMaterial = graph;
                Color[] cNoTextureOn = Capture(camera, target, readback,
                    Path.Combine(folder, "C-no-texture-pnoise-on"));
                Apply(legacy, false, mode, kind, opacityZero, false);
                renderer.sharedMaterial = legacy;
                Color[] bNoTextureOff = Capture(camera, target, readback,
                    Path.Combine(folder, "B-no-texture-pnoise-off"));
                Apply(graph, true, mode, kind, opacityZero, false);
                renderer.sharedMaterial = graph;
                Color[] cNoTextureOff = Capture(camera, target, readback,
                    Path.Combine(folder, "C-no-texture-pnoise-off"));
                legacy.EnableKeyword("_NOISEMAP");
                graph.SetFloat("_noisemapEnabled", 1);
                Apply(legacy, false, mode, kind, opacityZero, true);
                Apply(graph, true, mode, kind, opacityZero, true);

                Color[] zero = null, wrong = null;
                if (route != "Forward")
                {
                    graph.SetFloat("_NB_DistortionMode", 0);
                    zero = Capture(camera, target, readback, Path.Combine(folder, "C-mode0"));
                    graph.SetFloat("_NB_DistortionMode", route == Deferred ? 2 : 1);
                    wrong = Capture(camera, target, readback, Path.Combine(folder, "C-wrong-mode"));
                    graph.SetFloat("_NB_DistortionMode", route == Deferred ? 1 : 2);
                }

                var metrics = new Metrics
                {
                    route = route, kind = kind, mode = mode,
                    unityVersion = Application.unityVersion,
                    graphicsApi = SystemInfo.graphicsDeviceType.ToString(),
                    note = "Ordinary Mesh PN2 only. Current ShaderLab PNoise-off A/PNoise-on B and Graph-on C; A is not Frozen; exact NB pass RendererList, linear RGBAHalf raw RT, four warm-up renders per capture. No VFX/Player claim.",
                    roiPixels = (RoiMax - RoiMin) * (RoiMax - RoiMin),
                    finite = AllFinite(a, b, br, c, cr, cOff, bMask, cMask, baseline,
                        bNoTextureOn, cNoTextureOn, bNoTextureOff, cNoTextureOff),
                    maxBC = MaxRoiDelta(b, c), maxMaskBC = MaxRoiDelta(bMask, cMask),
                    maxAOffC = MaxRoiDelta(a, cOff),
                    abcDifferentRGBA = CountRoiDifferences(b, c),
                    maskDifferentRGBA = CountRoiDifferences(bMask, cMask),
                    legacyVisible = CountRoiVisible(b, baseline),
                    graphVisible = CountRoiVisible(c, baseline),
                    graphRepeat = MaxFrameDelta(c, cr), legacyRepeat = MaxFrameDelta(b, br),
                    legacyOnOff = MaxRoiDelta(a, b), graphOnOff = MaxRoiDelta(cOff, c),
                    graphMaskResponse = MaxRoiDelta(c, cMask), legacyMaskResponse = MaxRoiDelta(b, bMask),
                    graphNoTexturePNoiseDelta = MaxFrameDelta(cNoTextureOn, cNoTextureOff),
                    legacyNoTexturePNoiseDelta = MaxFrameDelta(bNoTextureOn, bNoTextureOff),
                    modeZeroDelta = zero == null ? 0 : MaxFrameDelta(baseline, zero),
                    wrongModeDelta = wrong == null ? 0 : MaxFrameDelta(baseline, wrong),
                    maskRatioApplicable = route == Deferred,
                };
                if (route == Deferred)
                {
                    int center = (Size / 2) * Size + Size / 2;
                    Assert.That(b[center].b * bMask[center].b * c[center].b * cMask[center].b,
                        Is.GreaterThan(0), "Deferred ratio requires visible source blue/coverage.");
                    metrics.graphSignedMaskDeltaR = Mathf.Abs(c[center].r / c[center].b - cMask[center].r / cMask[center].b);
                    metrics.graphSignedMaskDeltaG = Mathf.Abs(c[center].g / c[center].b - cMask[center].g / cMask[center].b);
                    metrics.legacySignedMaskDeltaR = Mathf.Abs(b[center].r / b[center].b - bMask[center].r / bMask[center].b);
                    metrics.legacySignedMaskDeltaG = Mathf.Abs(b[center].g / b[center].b - bMask[center].g / bMask[center].b);
                }
                if (zero != null) metrics.finite &= AllFinite(zero, wrong);
                File.WriteAllText(Path.Combine(folder, "metrics.json"), JsonUtility.ToJson(metrics, true));
                Debug.Log("NBFX_G4_PN2 " + JsonUtility.ToJson(metrics));

                Assert.That(metrics.finite, Is.True);
                Assert.That(metrics.legacyVisible, Is.GreaterThan(100));
                Assert.That(metrics.graphVisible, Is.GreaterThan(100));
                Assert.That(metrics.graphRepeat, Is.Zero);
                Assert.That(metrics.legacyRepeat, Is.Zero);
                Assert.That(metrics.abcDifferentRGBA, Is.Zero, "PN2 B/C exact-RT mismatch.");
                Assert.That(metrics.maskDifferentRGBA, Is.Zero, "PN2 mask-control B/C mismatch.");
                Assert.That(metrics.maxAOffC, Is.Zero, "PNoise-off A/C mismatch.");
                Assert.That(metrics.modeZeroDelta, Is.Zero);
                Assert.That(metrics.wrongModeDelta, Is.Zero);
                Assert.That(metrics.graphMaskResponse, Is.GreaterThan(.005f));
                Assert.That(metrics.legacyMaskResponse, Is.GreaterThan(.005f));
                Assert.That(metrics.graphNoTexturePNoiseDelta, Is.Zero);
                Assert.That(metrics.legacyNoTexturePNoiseDelta, Is.Zero);
                bool activeMode = mode >= 1 && mode <= 3 && !opacityZero;
                if (activeMode)
                {
                    Assert.That(metrics.legacyOnOff, Is.GreaterThan(.005f));
                    Assert.That(metrics.graphOnOff, Is.GreaterThan(.005f));
                }
                else
                {
                    Assert.That(metrics.legacyOnOff, Is.Zero, "Original fallback/no-op mode unexpectedly changed.");
                    Assert.That(metrics.graphOnOff, Is.Zero, "Graph fallback/no-op mode unexpectedly changed.");
                }
                if (route == Deferred)
                {
                    // The encoded RT is RGBAHalf and R/B is a *derived*
                    // quotient, so allow two half-ULPs of division error;
                    // direct B/C frame comparisons above stay strict zero.
                    Assert.That(metrics.graphSignedMaskDeltaR, Is.LessThanOrEqualTo(.001f),
                        "Screen signed R was multiplied by NoiseMask.");
                    Assert.That(metrics.graphSignedMaskDeltaG, Is.LessThanOrEqualTo(.001f),
                        "Screen signed G was multiplied by NoiseMask.");
                    Assert.That(metrics.legacySignedMaskDeltaR, Is.LessThanOrEqualTo(.001f));
                    Assert.That(metrics.legacySignedMaskDeltaG, Is.LessThanOrEqualTo(.001f));
                }
            }
            finally
            {
                if (directed != null)
                {
                    rendererData.rendererFeatures.Remove(directed);
                    UnityEngine.Object.DestroyImmediate(directed);
                }
                nbFeature.SetActive(nbWasActive);
                rendererData.SetDirty();
                camera.targetTexture = null;
                RenderTexture.active = previousActive;
                target.Release();
                foreach (UnityEngine.Object obj in new UnityEngine.Object[] { graph, legacy, backdrop,
                    backdropMap, noise, mask, whiteMask, baseMap, target, readback })
                    UnityEngine.Object.DestroyImmediate(obj);
                EditorSceneManager.ClosePreviewScene(scene);
                Assert.That(File.ReadAllBytes(rendererAsset), Is.EqualTo(rendererBefore),
                    "Temporary test feature changed renderer asset on disk.");
            }
        }

        static ScriptableRendererFeature FindNBPostProcess(ScriptableRendererData data)
        {
            foreach (ScriptableRendererFeature feature in data.rendererFeatures)
                if (feature != null && feature.GetType().FullName == "NBShader.NBPostProcess") return feature;
            return null;
        }

        static void Configure(Material m, bool graph, string route, Texture2D noise,
            Texture2D mask, Texture2D baseMap)
        {
            m.SetTexture("_BaseMap", route == "Forward" ? baseMap : Texture2D.whiteTexture);
            m.SetTexture("_NoiseMap", noise);
            m.SetTexture("_NoiseMaskMap", mask);
            m.SetColor(graph ? "_Color" : "_BaseColor", Color.white);
            m.SetColor("_ColorA", Color.white);
            m.SetFloat("_AlphaAll", 1);
            m.SetFloat("_BaseColorIntensityForTimeline", 1);
            m.SetFloat("_Cull", 0);
            m.SetFloat("_ZTest", (float)CompareFunction.LessEqual);
            m.SetFloat("_ZWrite", 0);
            m.SetFloat("_SrcBlend", (float)BlendMode.One);
            m.SetFloat("_DstBlend", (float)BlendMode.Zero);
            m.SetFloat("_TexDistortion_intensity", route == "Forward" ? .4f : 0);
            m.SetFloat("_noisemapEnabled", 1);
            m.SetFloat("_noiseMaskMap_Toggle", 1);
            m.SetVector("_NoiseOffset", Vector4.zero);
            m.SetVector("_DistortionDirection", new Vector4(.5f, .75f, 0, 0));
            m.SetFloat("_NoiseIntensity", .5f);
            m.SetVector("_DissolveVoronoi_Vec", new Vector4(3.1f, 2.7f, 2.4f, 3.3f));
            m.SetVector("_DissolveVoronoi_Vec2", new Vector4(1, 1, 0, 0));
            m.SetVector("_DissolveVoronoi_Vec3", Vector4.zero);
            m.SetVector("_DissolveVoronoi_Vec4", new Vector4(.17f, -.23f, -.19f, .11f));
            m.SetFloat("_ProgramNoise_Rotate", 0);
            m.SetFloat("_ProgramNoiseBaseBlendOpacity", 1);
            if (!graph) m.SetFloat("_DistortPNoiseBlendOpacity", 1);
            if (graph)
            {
                m.SetFloat("_Surface", 1);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                m.SetFloat("_DstBlendAlpha", (float)BlendMode.Zero);
                m.SetVector("_BaseMap_ST", new Vector4(1, 1, 0, 0));
                m.SetFloat("_NB_DistortionAlphaPow", 2);
                m.SetFloat("_NB_DistortionAlphaMultiplier", .5f);
                m.SetFloat("_NB_DistortionAlphaAdd", .125f);
                m.SetFloat("_NB_DistortionIntensity", route == Opaque ? 2f : .5f);
                m.SetFloat("_NB_DistortionMode", route == Deferred ? 1 : route == Opaque ? 2 : 0);
                SetWord(m, "_NB_Flags0Lo16", "_NB_Flags0Hi16", 0);
                SetWord(m, "_NB_Flags1Lo16", "_NB_Flags1Hi16", 1u << 9);
                m.SetFloat("_NB_ColorChannelLo16", 3); // Base alpha A, NoiseMask R.
            }
            else
            {
                m.EnableKeyword("_FX_LIGHT_MODE_UNLIT");
                m.EnableKeyword("_NOISEMAP");
                m.EnableKeyword("_NOISE_MASKMAP");
                m.SetFloat("_ColorMask", 15);
                m.SetFloat("_fogintensity", 0);
                m.SetInteger("_W9ParticleShaderFlags", 0);
                m.SetInteger("_W9ParticleShaderFlags1", 1 << 9);
                m.SetInteger("_W9ParticleShaderColorChannelFlag", 3);
                m.SetFloat("_ScreenDistortAlphaPow", 2);
                m.SetFloat("_ScreenDistortAlphaMulti", .5f);
                m.SetFloat("_ScreenDistortAlphaAdd", .125f);
                m.SetFloat("_ScreenDistortIntensity", route == Opaque ? 2f : .5f);
            }
            m.renderQueue = 3000;
            foreach (string pass in new[] { "SRPDefaultUnlit", "SRPDEFAULTUNLIT", "UniversalForward",
                "DepthOnly", "ShadowCaster", "Universal2D", Deferred, Opaque })
                m.SetShaderPassEnabled(pass, false);
            if (route == "Forward")
            {
                // Delegated Graph Unlit Forward has no explicit LightMode and
                // uses SRPDefaultUnlit. The old ShaderLab uses UniversalForward.
                m.SetShaderPassEnabled(graph ? "SRPDefaultUnlit" : "UniversalForward", true);
                if (graph) m.SetShaderPassEnabled("SRPDEFAULTUNLIT", true);
            }
            else m.SetShaderPassEnabled(route, true);
        }

        static void Apply(Material m, bool graph, int mode, string kind, bool opacityZero, bool enabled)
        {
            m.SetFloat("_ProgramNoise_Toggle", enabled ? 1 : 0);
            m.SetFloat("_ProgramNoise_Simple_Toggle", kind != "voronoi" ? 1 : 0);
            m.SetFloat("_ProgramNoise_Voronoi_Toggle", kind != "simple" ? 1 : 0);
            // Graph PN2 property is assigned only after a true Graph draw:
            // the imported shader can be a placeholder immediately after
            // assembly reload. The original ShaderLab property is stable.
            if (!graph) m.SetFloat("_DistortPNoiseBlendOpacity", opacityZero ? 0 : 1);
            uint packed = 1u | ((uint)mode << 9); // base blend Multiply + original Distort 3-bit field.
            if (graph) SetWord(m, "_NB_PNoiseBlendLo16", "_NB_PNoiseBlendHi16", packed);
            else
            {
                m.SetInteger("_W9ParticleShaderPNoiseBlendFlag", (int)packed);
                Toggle(m, "_PROGRAM_NOISE", enabled);
                Toggle(m, "_PROGRAM_NOISE_SIMPLE", enabled && kind != "voronoi");
                Toggle(m, "_PROGRAM_NOISE_VORONOI", enabled && kind != "simple");
            }
        }
        static void Toggle(Material m, string name, bool on)
        { if (on) m.EnableKeyword(name); else m.DisableKeyword(name); }
        static void SetWord(Material m, string lo, string hi, uint value)
        { m.SetFloat(lo, value & 65535u); m.SetFloat(hi, value >> 16); }

        static Texture2D MakeConstant(Color value)
        {
            var map = new Texture2D(1, 1, TextureFormat.RGBAHalf, false, true);
            map.SetPixel(0, 0, value); map.Apply(false);
            map.filterMode = FilterMode.Point; map.wrapMode = TextureWrapMode.Repeat;
            return map;
        }
        static Texture2D MakeGradient()
        {
            const int side = 64;
            var map = new Texture2D(side, side, TextureFormat.RGBAHalf, false, true);
            var pixels = new Color[side * side];
            for (int y = 0; y < side; y++) for (int x = 0; x < side; x++)
                pixels[y * side + x] = new Color(.1f + 1.2f * x / (side - 1f),
                    .1f + 1.2f * y / (side - 1f), .25f + .25f * (x + y) / (2f * side - 2f), 1);
            map.SetPixels(pixels); map.Apply(false);
            map.filterMode = FilterMode.Bilinear; map.wrapMode = TextureWrapMode.Clamp;
            return map;
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
                Color[] result = readback.GetPixels();
                using (var file = File.Create(path + ".rgba-f32.gz"))
                using (var zip = new GZipStream(file, CompressionMode.Compress))
                using (var writer = new BinaryWriter(zip))
                    foreach (Color color in result)
                    { writer.Write(color.r); writer.Write(color.g); writer.Write(color.b); writer.Write(color.a); }
                var png = new Texture2D(Size, Size, TextureFormat.RGBA32, false, true);
                try { png.SetPixels(result); png.Apply(false); File.WriteAllBytes(path + ".png", png.EncodeToPNG()); }
                finally { UnityEngine.Object.DestroyImmediate(png); }
                return result;
            }
            finally { RenderTexture.active = previous; }
        }
        static bool AllFinite(params Color[][] frames)
        {
            foreach (Color[] frame in frames) foreach (Color color in frame)
                for (int channel = 0; channel < 4; channel++)
                    if (float.IsNaN(color[channel]) || float.IsInfinity(color[channel])) return false;
            return true;
        }
        static float MaxFrameDelta(Color[] a, Color[] b)
        {
            float result = 0;
            for (int i = 0; i < a.Length; i++) for (int c = 0; c < 4; c++)
                result = Mathf.Max(result, Mathf.Abs(a[i][c] - b[i][c]));
            return result;
        }
        static float MaxRoiDelta(Color[] a, Color[] b)
        {
            float result = 0;
            for (int y = RoiMin; y < RoiMax; y++) for (int x = RoiMin; x < RoiMax; x++)
                for (int c = 0; c < 4; c++)
                    result = Mathf.Max(result, Mathf.Abs(a[y * Size + x][c] - b[y * Size + x][c]));
            return result;
        }
        static int CountRoiDifferences(Color[] a, Color[] b)
        {
            int count = 0;
            for (int y = RoiMin; y < RoiMax; y++) for (int x = RoiMin; x < RoiMax; x++)
            {
                int i = y * Size + x;
                for (int c = 0; c < 4; c++) if (a[i][c] != b[i][c]) { count++; break; }
            }
            return count;
        }
        static int CountRoiVisible(Color[] image, Color[] background)
        {
            int count = 0;
            for (int y = RoiMin; y < RoiMax; y++) for (int x = RoiMin; x < RoiMax; x++)
            {
                int i = y * Size + x;
                if (Mathf.Abs(image[i].r - background[i].r) +
                    Mathf.Abs(image[i].g - background[i].g) +
                    Mathf.Abs(image[i].b - background[i].b) > .01f) count++;
            }
            return count;
        }
    }
}
