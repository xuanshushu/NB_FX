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
    /// N2 ordinary Mesh only: draw each exact NB distortion Pass to an actual
    /// camera RT. The temporary directed feature and NBPostProcess active-state
    /// override are restored; no renderer asset is saved. VFX is not exercised.
    /// </summary>
    public sealed class G4GraphScreenNoiseTests
    {
        const string GraphPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph";
        const string LegacyPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader";
        const int Size = 128, BackgroundLayer = 2;
        internal const int ForegroundLayer = 3;
        const int RoiMin = 40, RoiMax = 88;
        const string Deferred = "NBDeferredDistortPass";
        const string Opaque = "NBCameraOpaqueDistortPass";

        [OneTimeSetUp]
        public void ReimportAfterBlockAssemblyReload() => AssetDatabase.ImportAsset(GraphPath,
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

        static IEnumerable<TestCaseData> Cases()
        {
            foreach (string pass in new[] { Deferred, Opaque })
                foreach (string state in new[] { "noise-a-half", "mask-half", "normalize-signed",
                    "alpha-refine", "uniform-fallback" })
                    yield return new TestCaseData(pass, state).SetName(
                        "G4ScreenNoise_" + pass + "_" + state);
        }

        [Serializable]
        sealed class Metrics
        {
            public string caseId, unityVersion, api, note;
            public int roiPixels, onDifferentRGBA, offDifferentRGBA;
            public int graphVisible, legacyVisible;
            public bool finite, parityApplicable;
            public float maxOnRGBA, maxOffRGBA, graphRepeat, legacyRepeat;
            public float graphControl, legacyControl, modeZeroDelta, wrongModeDelta;
            public float centerR, centerG, centerB, expectedSignedR, expectedSignedG;
            public float expectedNoiseMask, expectedBlue, recoveredSignedR, recoveredSignedG;
            public float fallbackTextureControl;
        }

        [TestCaseSource(nameof(Cases))]
        public void ScreenNoiseUsesOneUnmaskedPayload(string pass, string state)
        {
            Assert.That(GraphicsSettings.currentRenderPipeline, Is.InstanceOf<UniversalRenderPipelineAsset>());
            Assert.That(GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>()
                .enableRenderCompatibilityMode, Is.False);
            Assert.That(SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf), Is.True);
            Shader graphShader = AssetDatabase.LoadAssetAtPath<Shader>(GraphPath);
            Shader legacyShader = AssetDatabase.LoadAssetAtPath<Shader>(LegacyPath);
            Assert.That(graphShader && legacyShader && graphShader.isSupported && legacyShader.isSupported, Is.True);
            // Graph may expose its importer placeholder until a steady-state draw.
            // Verify actual pass names/tags after warm-up, never waive the check.
            var pipeline = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            Assert.That(pipeline.rendererDataList.Length, Is.GreaterThan(0));
            ScriptableRendererData rendererData = pipeline.rendererDataList[0];
            Assert.That(rendererData, Is.Not.Null);
            ScriptableRendererFeature nbFeature = FindNBPostProcess(rendererData);
            Assert.That(nbFeature, Is.Not.Null, "The original NBPostProcess is required for a restore audit.");
            bool nbWasActive = nbFeature.isActive;
            string assetPath = AssetDatabase.GetAssetPath(rendererData);
            string assetFile = Path.Combine(Path.GetDirectoryName(Application.dataPath), assetPath);
            byte[] assetBytes = File.ReadAllBytes(assetFile);
            string root = Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR");
            if (string.IsNullOrEmpty(root))
                root = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/NBFXG4ScreenNoise");
            string output = Path.Combine(root, pass + "-" + state);
            Directory.CreateDirectory(output);

            Scene scene = EditorSceneManager.NewPreviewScene();
            GameObject background = GameObject.CreatePrimitive(PrimitiveType.Quad);
            GameObject foreground = GameObject.CreatePrimitive(PrimitiveType.Quad);
            GameObject cameraObject = new GameObject("NBFX N2 screen Noise ordinary Mesh");
            Material graph = new Material(graphShader), legacy = new Material(legacyShader);
            Shader backdropShader = Shader.Find("Universal Render Pipeline/Unlit");
            Assert.That(backdropShader, Is.Not.Null);
            Material backdrop = new Material(backdropShader);
            Texture2D backdropMap = MakeBackdrop(), noise = MakeConstant(new Color(.75f, .25f, 0, .5f));
            Texture2D mask = MakeConstant(new Color(.5f, .125f, .75f, 1));
            var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            var readback = new Texture2D(Size, Size, TextureFormat.RGBAHalf, false, true);
            var camera = cameraObject.AddComponent<Camera>();
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
                backdrop.SetFloat("_Surface", 0);
                backdrop.SetFloat("_Cull", (float)CullMode.Off); // The camera views the Quad from its back.
                backdrop.renderQueue = 2000;
                background.layer = BackgroundLayer;
                background.transform.position = new Vector3(0, 0, 1);
                background.transform.localScale = new Vector3(6, 6, 1);
                background.GetComponent<MeshRenderer>().sharedMaterial = backdrop;
                // Deferred SrcAlpha over a black RT allows exact recovery of
                // the unmasked signed RG and the once-masked source alpha.
                // Camera-opaque instead needs a nonuniform source texture.
                background.GetComponent<MeshRenderer>().enabled = pass == Opaque;
                foreground.layer = ForegroundLayer;
                foreground.transform.position = new Vector3(0, 0, 2);
                foreground.transform.localScale = new Vector3(2, 2, 1);
                var foregroundRenderer = foreground.GetComponent<MeshRenderer>();
                foregroundRenderer.shadowCastingMode = ShadowCastingMode.Off;
                foregroundRenderer.receiveShadows = false;
                Configure(graph, true, noise, mask, pass, state);
                Configure(legacy, false, noise, mask, pass, state);

                camera.scene = scene;
                camera.orthographic = true;
                camera.orthographicSize = 1.5f;
                camera.nearClipPlane = .1f;
                camera.farClipPlane = 20f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
                camera.allowHDR = true;
                camera.allowMSAA = false;
                camera.cullingMask = (1 << BackgroundLayer) | (1 << ForegroundLayer);
                camera.transform.position = new Vector3(0, 0, 5);
                camera.transform.rotation = Quaternion.Euler(0, 180, 0);
                camera.targetTexture = target;
                cameraData.requiresColorTexture = true;
                cameraData.renderPostProcessing = false;
                target.Create();
                Assert.That(target.IsCreated() && !target.sRGB, Is.True);

                // Isolate the exact tag without changing NBPostprocess code or
                // saving the renderer asset. Always restore the prior state.
                nbFeature.SetActive(false);
                directed = ScriptableObject.CreateInstance<G4ScreenNoiseDirectedFeature>();
                directed.hideFlags = HideFlags.HideAndDontSave;
                directed.targetCamera = camera;
                directed.selectedPass = pass;
                directed.Create();
                directed.SetActive(true);
                rendererData.rendererFeatures.Add(directed);
                rendererData.SetDirty();
                foregroundRenderer.enabled = false;
                camera.Render(); // Renderer rebuild/opaque-copy warm-up.
                Color[] backgroundOnly = Capture(camera, target, readback, Path.Combine(output, "background"));
                if (pass == Opaque)
                    Assert.That(CountDistinctRGB(backgroundOnly), Is.GreaterThan(32),
                        "Camera-opaque source must be nonuniform.");
                else
                    Assert.That(CountDistinctRGB(backgroundOnly), Is.EqualTo(1),
                        "Deferred recovery requires a black background.");

                foregroundRenderer.enabled = true;
                int mode = pass == Deferred ? 1 : 2;
                SetMode(graph, legacy, pass, mode, true);
                foregroundRenderer.sharedMaterial = legacy;
                Color[] b = Capture(camera, target, readback, Path.Combine(output, "B-on"));
                Color[] br = Capture(camera, target, readback, Path.Combine(output, "B-repeat"));
                foregroundRenderer.sharedMaterial = graph;
                Color[] c = Capture(camera, target, readback, Path.Combine(output, "C-on"));
                Color[] cr = Capture(camera, target, readback, Path.Combine(output, "C-repeat"));

                SetStrength(graph, legacy, 0);
                foregroundRenderer.sharedMaterial = legacy;
                Color[] bOff = Capture(camera, target, readback, Path.Combine(output, "B-off"));
                foregroundRenderer.sharedMaterial = graph;
                Color[] cOff = Capture(camera, target, readback, Path.Combine(output, "C-off"));
                SetStrength(graph, legacy, .5f);

                // Both exact-tag lists are still invoked, but Graph's mode 0
                // and opposite mode must clip before writing any pixel.
                graph.SetFloat("_NB_DistortionMode", 0);
                Color[] modeZero = Capture(camera, target, readback, Path.Combine(output, "C-mode0"));
                graph.SetFloat("_NB_DistortionMode", mode == 1 ? 2 : 1);
                Color[] wrongMode = Capture(camera, target, readback, Path.Combine(output, "C-wrong-mode"));
                graph.SetFloat("_NB_DistortionMode", mode);

                Metrics parity = Compare(b, c);
                Metrics offParity = Compare(bOff, cOff);
                var metrics = parity;
                metrics.caseId = pass + "-" + state;
                metrics.parityApplicable = state != "uniform-fallback";
                metrics.unityVersion = Application.unityVersion;
                metrics.api = SystemInfo.graphicsDeviceType.ToString();
                metrics.note = "Ordinary Mesh; exact-tag directed RendererList into linear RGBAHalf camera RT. Raw B/C and controls saved. Deferred RT over black recovers source signedRG as R/B,G/B and source alpha from B; alpha channel itself is blended. Uniform fallback is Graph-only and B/C parity is not asserted for that case. VFX/Player not tested.";
                metrics.finite = AllFinite(backgroundOnly, b, br, c, cr, bOff, cOff,
                    modeZero, wrongMode);
                metrics.onDifferentRGBA = parity.onDifferentRGBA;
                metrics.offDifferentRGBA = offParity.onDifferentRGBA;
                metrics.maxOnRGBA = parity.maxOnRGBA;
                metrics.maxOffRGBA = offParity.maxOnRGBA;
                metrics.graphRepeat = MaxDelta(c, cr);
                metrics.legacyRepeat = MaxDelta(b, br);
                metrics.graphControl = MaxDelta(c, cOff);
                metrics.legacyControl = MaxDelta(b, bOff);
                metrics.modeZeroDelta = MaxDeltaAll(backgroundOnly, modeZero);
                metrics.wrongModeDelta = MaxDeltaAll(backgroundOnly, wrongMode);
                Color center = c[(Size / 2) * Size + Size / 2];
                metrics.centerR = center.r; metrics.centerG = center.g; metrics.centerB = center.b;
                Expected(state, out Vector2 signed, out float noiseMask, out float sourceAlpha);
                metrics.expectedSignedR = signed.x; metrics.expectedSignedG = signed.y;
                metrics.expectedNoiseMask = noiseMask; metrics.expectedBlue = sourceAlpha;
                if (pass == Deferred && center.b > 0)
                {
                    metrics.recoveredSignedR = center.r / center.b;
                    metrics.recoveredSignedG = center.g / center.b;
                }
                if (state == "uniform-fallback")
                {
                    // Equivalent sampled RG/A must produce the same payload;
                    // the old explicit uniform remains a Noise-off path only.
                    graph.SetFloat("_noisemapEnabled", 1);
                    noise.SetPixel(0, 0, new Color(.25f, -.125f, 0, 1)); noise.Apply(false);
                    graph.SetVector("_DistortionDirection", new Vector4(1, 1, 0, 0));
                    graph.SetFloat("_NoiseIntensity", 1);
                    foregroundRenderer.sharedMaterial = graph;
                    Color[] sampledEquivalent = Capture(camera, target, readback,
                        Path.Combine(output, "C-fallback-sampled-equivalent"));
                    metrics.finite &= AllFinite(sampledEquivalent);
                    metrics.fallbackTextureControl = MaxDelta(c, sampledEquivalent);
                }
                File.WriteAllText(Path.Combine(output, "metrics.json"), JsonUtility.ToJson(metrics, true));
                Debug.Log("NBFX_G4_SCREEN_NOISE " + JsonUtility.ToJson(metrics));
                Assert.That(metrics.finite && offParity.finite, Is.True);
                Assert.That(metrics.graphRepeat, Is.Zero);
                Assert.That(metrics.legacyRepeat, Is.Zero);
                Assert.That(metrics.modeZeroDelta, Is.Zero, "Graph mode 0 wrote the selected NB pass.");
                Assert.That(metrics.wrongModeDelta, Is.Zero, "Graph's other distortion mode wrote this pass.");
                Assert.That(metrics.graphControl, Is.GreaterThan(.005f), "Graph screen-noise on/off response absent.");
                if (metrics.parityApplicable)
                {
                    Assert.That(metrics.legacyControl, Is.GreaterThan(.005f));
                    Assert.That(metrics.onDifferentRGBA, Is.Zero, "Strict NB Pass B/C on-state mismatch.");
                    Assert.That(metrics.offDifferentRGBA, Is.Zero, "Strict NB Pass B/C off-state mismatch.");
                }
                else Assert.That(metrics.fallbackTextureControl, Is.Zero,
                    "Noise-off uniform prototype differs from equivalent sampled signedRG.");
                if (pass == Deferred)
                {
                    Assert.That(metrics.centerB, Is.EqualTo(metrics.expectedBlue),
                        "Deferred blue must contain coverage × intensity with only one noise mask multiplication.");
                    Assert.That(metrics.recoveredSignedR, Is.EqualTo(metrics.expectedSignedR),
                        "Deferred signed R was masked or changed.");
                    Assert.That(metrics.recoveredSignedG, Is.EqualTo(metrics.expectedSignedG),
                        "Deferred signed G was masked or changed.");
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
                foreach (var obj in new UnityEngine.Object[] { graph, legacy, backdrop, backdropMap,
                    noise, mask, target, readback }) UnityEngine.Object.DestroyImmediate(obj);
                EditorSceneManager.ClosePreviewScene(scene);
                Assert.That(File.ReadAllBytes(assetFile), Is.EqualTo(assetBytes),
                    "The temporary RendererFeature must not change the renderer asset on disk.");
            }
        }

        static ScriptableRendererFeature FindNBPostProcess(ScriptableRendererData data)
        {
            foreach (var feature in data.rendererFeatures)
                if (feature != null && feature.GetType().FullName == "NBShader.NBPostProcess") return feature;
            return null;
        }

        static void AssertPass(Shader shader, string pass)
        {
            var material = new Material(shader);
            try
            {
                int index = material.FindPass(pass);
                Assert.That(index, Is.GreaterThanOrEqualTo(0));
                Assert.That(shader.FindPassTagValue(0, index, new ShaderTagId("LightMode")).name, Is.EqualTo(pass));
            }
            finally { UnityEngine.Object.DestroyImmediate(material); }
        }

        static void Configure(Material m, bool graph, Texture2D noise, Texture2D mask, string pass, string state)
        {
            if (graph) m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            else { m.EnableKeyword("_FX_LIGHT_MODE_UNLIT"); m.EnableKeyword("_NOISEMAP"); }
            m.SetTexture("_BaseMap", Texture2D.whiteTexture);
            m.SetTexture("_NoiseMap", noise); m.SetTexture("_NoiseMaskMap", mask);
            m.SetTextureScale("_NoiseMap", Vector2.one); m.SetTextureOffset("_NoiseMap", Vector2.zero);
            m.SetTextureScale("_NoiseMaskMap", Vector2.one); m.SetTextureOffset("_NoiseMaskMap", Vector2.zero);
            m.SetColor(graph ? "_Color" : "_BaseColor", Color.white);
            m.SetColor("_ColorA", Color.white);
            m.SetFloat("_BaseColorIntensityForTimeline", 1); m.SetFloat("_AlphaAll", 1);
            m.SetFloat("_Cull", 0); m.SetFloat("_ZTest", (float)CompareFunction.LessEqual);
            m.SetFloat("_ZWrite", 0); m.SetFloat("_SrcBlend", (float)BlendMode.One);
            m.SetFloat("_DstBlend", (float)BlendMode.Zero);
            m.SetFloat("_TexDistortion_intensity", 0);
            m.SetVector("_NoiseOffset", Vector4.zero); m.SetFloat("_NoiseMapUVRotation", 0);
            m.SetVector("_DistortionDirection", new Vector4(.5f, .75f, 0, 0));
            m.SetFloat("_NoiseIntensity", .5f);
            m.SetFloat("_noisemapEnabled", state == "uniform-fallback" ? 0 : 1);
            m.SetFloat("_noiseMaskMap_Toggle", state == "mask-half" || state == "alpha-refine" ? 1 : 0);
            uint flags0 = state == "normalize-signed" ? 1u << 12 : 0;
            uint flags1 = (1u << 9) | (state == "alpha-refine" ? 1u << 8 : 0);
            uint channels = 3; // Noise mask channel R (bits 8..9 = 0).
            if (graph)
            {
                m.SetFloat("_Surface", 1);
                m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                m.SetFloat("_DstBlendAlpha", (float)BlendMode.Zero);
                m.SetVector("_BaseMap_ST", new Vector4(1, 1, 0, 0));
                m.SetVector("_NB_DistortionNoise", new Vector4(.25f, -.125f, 0, 0));
                SetWord(m, "_NB_Flags0Lo16", "_NB_Flags0Hi16", flags0);
                SetWord(m, "_NB_Flags1Lo16", "_NB_Flags1Hi16", flags1);
                m.SetFloat("_NB_ColorChannelLo16", channels);
                m.SetFloat("_NB_DistortionAlphaPow", 2);
                m.SetFloat("_NB_DistortionAlphaMultiplier", .5f);
                m.SetFloat("_NB_DistortionAlphaAdd", .125f);
                m.SetFloat("_NB_DistortionIntensity", .5f);
                m.SetFloat("_NB_DistortionMode", pass == Deferred ? 1 : 2);
            }
            else
            {
                m.SetFloat("_ColorMask", 15); m.SetFloat("_fogintensity", 0);
                m.SetInteger("_W9ParticleShaderFlags", unchecked((int)flags0));
                m.SetInteger("_W9ParticleShaderFlags1", unchecked((int)flags1));
                m.SetInteger("_W9ParticleShaderColorChannelFlag", unchecked((int)channels));
                m.SetFloat("_ScreenDistortAlphaPow", 2);
                m.SetFloat("_ScreenDistortAlphaMulti", .5f);
                m.SetFloat("_ScreenDistortAlphaAdd", .125f);
                m.SetFloat("_ScreenDistortIntensity", .5f);
                if (state == "mask-half" || state == "alpha-refine") m.EnableKeyword("_NOISE_MASKMAP");
                if (state == "uniform-fallback") m.DisableKeyword("_NOISEMAP");
            }
            m.renderQueue = 3000;
            foreach (string name in new[] { "SRPDefaultUnlit", "SRPDEFAULTUNLIT", "UniversalForward",
                "DepthOnly", "ShadowCaster", "Universal2D", Deferred, Opaque })
                m.SetShaderPassEnabled(name, false);
            m.SetShaderPassEnabled(pass, true);
        }

        static void SetMode(Material graph, Material legacy, string pass, int mode, bool legacyPassEnabled)
        {
            graph.SetFloat("_NB_DistortionMode", mode);
            legacy.SetShaderPassEnabled(pass, legacyPassEnabled);
        }
        static void SetStrength(Material graph, Material legacy, float value)
        { graph.SetFloat("_NB_DistortionIntensity", value); legacy.SetFloat("_ScreenDistortIntensity", value); }
        static void SetWord(Material material, string lo, string hi, uint value)
        { material.SetFloat(lo, value & 65535u); material.SetFloat(hi, value >> 16); }

        static void Expected(string state, out Vector2 signed, out float mask, out float sourceAlpha)
        {
            signed = state == "uniform-fallback" ? new Vector2(.25f, -.125f) :
                state == "normalize-signed" ? new Vector2(.125f, -.1875f) :
                new Vector2(.1875f, .09375f);
            mask = state == "uniform-fallback" ? 1 :
                state == "mask-half" || state == "alpha-refine" ? .25f : .5f;
            float coverage = state == "alpha-refine" ? Mathf.Pow(mask, 2) * .5f + .125f : mask;
            sourceAlpha = coverage * .5f;
        }

        static Texture2D MakeConstant(Color value)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBAHalf, false, true);
            texture.SetPixel(0, 0, value); texture.Apply(false);
            texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Repeat;
            return texture;
        }
        static Texture2D MakeBackdrop()
        {
            const int n = 64;
            var texture = new Texture2D(n, n, TextureFormat.RGBAHalf, false, true);
            var pixels = new Color[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                pixels[y * n + x] = new Color(.1f + 1.2f * x / (n - 1f),
                    .1f + 1.2f * y / (n - 1f), .25f + .25f * (x + y) / (2f * n - 2f), 1);
            texture.SetPixels(pixels); texture.Apply(false);
            texture.filterMode = FilterMode.Bilinear; texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }

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
                    using (var zip = new GZipStream(file, CompressionMode.Compress))
                    using (var writer = new BinaryWriter(zip))
                        foreach (Color c in pixels)
                        { writer.Write(c.r); writer.Write(c.g); writer.Write(c.b); writer.Write(c.a); }
                    var png = new Texture2D(Size, Size, TextureFormat.RGBA32, false, true);
                    try { png.SetPixels(pixels); png.Apply(false); File.WriteAllBytes(path + ".png", png.EncodeToPNG()); }
                    finally { UnityEngine.Object.DestroyImmediate(png); }
                }
                return pixels;
            }
            finally { RenderTexture.active = previous; }
        }
        static int CountDistinctRGB(Color[] colors)
        {
            var values = new HashSet<int>();
            foreach (Color c in colors)
                values.Add((Mathf.RoundToInt(c.r * 512) << 20) ^
                    (Mathf.RoundToInt(c.g * 512) << 10) ^ Mathf.RoundToInt(c.b * 512));
            return values.Count;
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
        static float MaxDeltaAll(Color[] a, Color[] b)
        {
            Assert.That(a.Length, Is.EqualTo(b.Length));
            float max = 0;
            for (int pixel = 0; pixel < a.Length; pixel++)
                for (int channel = 0; channel < 4; channel++)
                    max = Mathf.Max(max, Mathf.Abs(a[pixel][channel] - b[pixel][channel]));
            return max;
        }
        static Metrics Compare(Color[] a, Color[] b)
        {
            var result = new Metrics { finite = true };
            for (int y = RoiMin; y < RoiMax; y++) for (int x = RoiMin; x < RoiMax; x++)
            {
                int i = y * Size + x;
                result.roiPixels++;
                if (a[i].maxColorComponent > .01f) result.legacyVisible++;
                if (b[i].maxColorComponent > .01f) result.graphVisible++;
                float delta = 0;
                for (int k = 0; k < 4; k++)
                {
                    float av = a[i][k], bv = b[i][k];
                    if (float.IsNaN(av) || float.IsInfinity(av) || float.IsNaN(bv) || float.IsInfinity(bv))
                        result.finite = false;
                    delta = Mathf.Max(delta, Mathf.Abs(av - bv));
                }
                if (delta > 0) result.onDifferentRGBA++;
                result.maxOnRGBA = Mathf.Max(result.maxOnRGBA, delta);
            }
            return result;
        }
        static float MaxDelta(Color[] a, Color[] b) => Compare(a, b).maxOnRGBA;
        static float MaxFrameDelta(Color[] a, Color[] b)
        {
            float max = 0;
            for (int i = 0; i < a.Length; i++) for (int c = 0; c < 4; c++)
                max = Mathf.Max(max, Mathf.Abs(a[i][c] - b[i][c]));
            return max;
        }
    }

    // Test-only exact-tag RenderGraph list; no persistent renderer feature.
    internal sealed class G4ScreenNoiseDirectedFeature : ScriptableRendererFeature
    {
        internal Camera targetCamera;
        internal string selectedPass;
        DirectedPass _pass;
        public override void Create() => _pass = new DirectedPass(this)
            { renderPassEvent = RenderPassEvent.BeforeRenderingTransparents + 1 };
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.camera == targetCamera && selectedPass != null)
                renderer.EnqueuePass(_pass);
        }
        sealed class DirectedPass : ScriptableRenderPass
        {
            readonly G4ScreenNoiseDirectedFeature _owner;
            sealed class Data { internal RendererListHandle list; }
            internal DirectedPass(G4ScreenNoiseDirectedFeature owner) { _owner = owner; }
            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
            {
                var resources = frameData.Get<UniversalResourceData>();
                if (!resources.activeColorTexture.IsValid()) return;
                var cameraData = frameData.Get<UniversalCameraData>();
                var renderingData = frameData.Get<UniversalRenderingData>();
                var lightData = frameData.Get<UniversalLightData>();
                var tags = new List<ShaderTagId> { new ShaderTagId(_owner.selectedPass) };
                var drawing = RenderingUtils.CreateDrawingSettings(tags, renderingData,
                    cameraData, lightData, cameraData.defaultOpaqueSortFlags);
                var filtering = new FilteringSettings(RenderQueueRange.all,
                    1 << G4GraphScreenNoiseTests.ForegroundLayer);
                var list = graph.CreateRendererList(new RendererListParams(renderingData.cullResults,
                    drawing, filtering));
                using (var builder = graph.AddRasterRenderPass<Data>("NBFX N2 directed " + _owner.selectedPass,
                    out var data))
                {
                    data.list = list;
                    builder.UseRendererList(list);
                    builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.ReadWrite);
                    if (resources.activeDepthTexture.IsValid())
                        builder.SetRenderAttachmentDepth(resources.activeDepthTexture, AccessFlags.ReadWrite);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc(static (Data passData, RasterGraphContext context) =>
                        context.cmd.DrawRendererList(passData.list));
                }
            }
        }
    }
}
