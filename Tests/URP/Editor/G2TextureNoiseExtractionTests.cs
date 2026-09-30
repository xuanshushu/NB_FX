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
    /// N1 only: ordinary Mesh G0 Frozen (A) / current ShaderLab (B).
    /// Graph, screen RT, Refraction, PNoise and VFX are deliberately not claimed.
    /// No saved materials, scenes, pipeline settings or renderer features change.
    /// </summary>
    public sealed class G2TextureNoiseExtractionTests
    {
        const string CurrentPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader";
        const string FrozenPath = "Packages/com.xuanxuan.nb.fx/Tests/Baseline/Frozen/NBShaders2/Shader/NBShader.shader";
        const int Size = 128, Layer = 2, RoiMin = 32, RoiMax = 96;
        const float ControlThreshold = .01f;
        static readonly string[] Consumers = { "base", "emission", "dissolve", "mask" };
        static readonly string[] Inputs =
        {
            "unsigned", "signed", "unsigned-rg0", "unsigned-rg1", "signed-rg0", "signed-rg1",
            "alpha-zero", "alpha-weight", "alpha-one",
            "direction-zero", "direction-x", "direction-y", "direction-negative",
            "intensity-zero", "intensity-fraction", "intensity-one",
            "mask-r", "mask-g", "mask-b", "mask-a", "mask-zero", "alpha-and-mask", "noise-disabled"
        };

        [Serializable] sealed class State
        {
            public bool noiseEnabled = true, normalizeRG, maskEnabled;
            public Color noise = new Color(.75f, .25f, 0, 1);
            public Color mask = new Color(.125f, .375f, .625f, .875f);
            public Vector2 direction = new Vector2(1, .5f);
            public float intensity = .3f;
            public int maskChannel;
        }

        [Serializable] sealed class Metrics
        {
            public string caseId, unityVersion, api, target, note;
            public State input, control;
            public int roiPixels, differingRGB, differingAlpha, controlDifferingRGB, controlDifferingAlpha;
            public int frozenVisible, currentVisible, frozenControlPixels, currentControlPixels;
            public bool finite;
            public float maxRGB, maxAlpha, controlMaxRGB, controlMaxAlpha;
            public float frozenRepeat, currentRepeat, frozenControlDelta, currentControlDelta;
        }

        static IEnumerable<TestCaseData> Cases()
        {
            foreach (string consumer in Consumers)
                foreach (string input in Inputs)
                    yield return new TestCaseData(consumer, input).SetName("G2TextureNoiseAB_" + consumer + "_" + input);
        }

        [TestCaseSource(nameof(Cases))]
        public void TextureNoiseDecodeMatchesFrozen(string consumer, string inputId)
        {
            Assert.That(GraphicsSettings.currentRenderPipeline, Is.InstanceOf<UniversalRenderPipelineAsset>());
            Assert.That(SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf), Is.True);
            var currentShader = AssetDatabase.LoadAssetAtPath<Shader>(CurrentPath);
            var frozenShader = AssetDatabase.LoadAssetAtPath<Shader>(FrozenPath);
            Assert.That(currentShader, Is.Not.Null);
            Assert.That(frozenShader, Is.Not.Null);
            Assert.That(currentShader.name, Is.EqualTo("Effects/NBShader"));
            Assert.That(frozenShader.name, Is.EqualTo("Effects/NBShader_T00_Frozen"));
            Assert.That(currentShader.isSupported && frozenShader.isSupported, Is.True);
            string root = Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR");
            if (string.IsNullOrEmpty(root)) root = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/NBFXG2TextureNoise");
            string output = Path.Combine(root, "g2-texture-noise", consumer + "-" + inputId);
            Directory.CreateDirectory(output);
            var scene = EditorSceneManager.NewPreviewScene();
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var cameraObject = new GameObject("NBFX ordinary Mesh texture Noise A/B");
            SceneManager.MoveGameObjectToScene(quad, scene);
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<Camera>();
            var current = new Material(currentShader);
            var frozen = new Material(frozenShader);
            var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            var readback = new Texture2D(Size, Size, TextureFormat.RGBAHalf, false, true);
            var map = MakeConsumerMap();
            var noise = new Texture2D(1, 1, TextureFormat.RGBAHalf, false, true);
            var mask = new Texture2D(1, 1, TextureFormat.RGBAHalf, false, true);
            var previousActive = RenderTexture.active;
            try
            {
                State input = MakeState(inputId, false), control = MakeState(inputId, true);
                Configure(current, consumer, map, noise, mask);
                Configure(frozen, consumer, map, noise, mask);
                ApplyState(current, frozen, noise, mask, input);
                quad.layer = Layer;
                quad.transform.localScale = new Vector3(2, 2, 1);
                var renderer = quad.GetComponent<MeshRenderer>();
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                camera.scene = scene;
                camera.transform.position = new Vector3(0, 0, 5);
                camera.transform.rotation = Quaternion.Euler(0, 180, 0);
                camera.orthographic = true;
                camera.orthographicSize = 1.5f;
                camera.nearClipPlane = .1f;
                camera.farClipPlane = 20;
                camera.cullingMask = 1 << Layer;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
                camera.allowHDR = true;
                camera.allowMSAA = false;
                camera.targetTexture = target;
                target.Create();
                Assert.That(target.IsCreated() && !target.sRGB, Is.True);
                renderer.sharedMaterial = frozen;
                Color[] a = Capture(camera, target, readback, Path.Combine(output, "A-frozen"));
                Color[] ar = Capture(camera, target, readback, Path.Combine(output, "A-repeat"));
                renderer.sharedMaterial = current;
                Color[] b = Capture(camera, target, readback, Path.Combine(output, "B-current"));
                Color[] br = Capture(camera, target, readback, Path.Combine(output, "B-repeat"));
                ApplyState(current, frozen, noise, mask, control);
                renderer.sharedMaterial = frozen;
                Color[] ac = Capture(camera, target, readback, Path.Combine(output, "A-control"));
                renderer.sharedMaterial = current;
                Color[] bc = Capture(camera, target, readback, Path.Combine(output, "B-control"));
                Metrics metrics = Compare(a, b);
                Metrics controlParity = Compare(ac, bc);
                metrics.caseId = consumer + "-" + inputId;
                metrics.unityVersion = Application.unityVersion;
                metrics.api = SystemInfo.graphicsDeviceType.ToString();
                metrics.target = "128x128 linear RGBAHalf; lossless float32 RGBA; strict safe ROI x/y=32..95";
                metrics.note = "N1 Frozen/current ShaderLab only. Four warm-up renders per capture; no first-frame or Graph/VFX claims. One isolated consumer; constant half noise/mask and nonuniform consumer texture. Fixed zero parity, repeat zero and >.01 positive-control criteria; no tolerance relaxation.";
                metrics.input = input; metrics.control = control;
                metrics.controlDifferingRGB = controlParity.differingRGB;
                metrics.controlDifferingAlpha = controlParity.differingAlpha;
                metrics.controlMaxRGB = controlParity.maxRGB;
                metrics.controlMaxAlpha = controlParity.maxAlpha;
                metrics.frozenRepeat = MaxDelta(a, ar);
                metrics.currentRepeat = MaxDelta(b, br);
                metrics.frozenControlDelta = MaxDelta(a, ac);
                metrics.currentControlDelta = MaxDelta(b, bc);
                metrics.frozenControlPixels = CountControlPixels(a, ac);
                metrics.currentControlPixels = CountControlPixels(b, bc);
                metrics.finite = AllFinite(a, ar, b, br, ac, bc);
                File.WriteAllText(Path.Combine(output, "metrics.json"), JsonUtility.ToJson(metrics, true));
                Debug.Log("NBFX_G2_TEXTURE_NOISE_AB " + JsonUtility.ToJson(metrics));
                Assert.That(metrics.finite, Is.True, "Nonfinite raw output.");
                Assert.That(metrics.frozenVisible, Is.GreaterThan(100));
                Assert.That(metrics.currentVisible, Is.GreaterThan(100));
                Assert.That(metrics.frozenRepeat, Is.Zero);
                Assert.That(metrics.currentRepeat, Is.Zero);
                Assert.That(metrics.frozenControlDelta, Is.GreaterThan(ControlThreshold), "Frozen parameter positive control had no effect.");
                Assert.That(metrics.currentControlDelta, Is.GreaterThan(ControlThreshold), "Current parameter positive control had no effect.");
                Assert.That(metrics.frozenControlPixels, Is.GreaterThan(64));
                Assert.That(metrics.currentControlPixels, Is.GreaterThan(64));
                Assert.That(metrics.differingRGB, Is.Zero, "Strict RGB Frozen/current mismatch.");
                Assert.That(metrics.differingAlpha, Is.Zero, "Strict Alpha Frozen/current mismatch.");
                Assert.That(metrics.controlDifferingRGB, Is.Zero, "Strict RGB positive-control A/B mismatch.");
                Assert.That(metrics.controlDifferingAlpha, Is.Zero, "Strict Alpha positive-control A/B mismatch.");
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previousActive;
                target.Release();
                foreach (var obj in new UnityEngine.Object[] { current, frozen, target, readback, map, noise, mask })
                    UnityEngine.Object.DestroyImmediate(obj);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        static State MakeState(string id, bool control)
        {
            var s = new State();
            switch (id)
            {
                case "unsigned": s.normalizeRG = control; break;
                case "signed": s.normalizeRG = !control; break;
                case "unsigned-rg0": case "unsigned-rg1": case "signed-rg0": case "signed-rg1":
                    s.normalizeRG = id.StartsWith("signed", StringComparison.Ordinal);
                    float rg = id.EndsWith("rg1", StringComparison.Ordinal) != control ? 1 : 0;
                    s.noise = new Color(rg, rg, 0, 1); break;
                case "alpha-zero": s.noise.a = control ? 1 : 0; break;
                case "alpha-weight": s.noise.a = control ? 1 : .375f; break;
                case "alpha-one": s.noise.a = control ? 0 : 1; break;
                case "direction-zero": s.direction = control ? new Vector2(1, .5f) : Vector2.zero; break;
                case "direction-x": s.direction = control ? Vector2.up : Vector2.right; break;
                case "direction-y": s.direction = control ? Vector2.right : Vector2.up; break;
                case "direction-negative": s.direction = new Vector2(control ? 1 : -1, control ? .5f : -.5f); break;
                case "intensity-zero": s.intensity = control ? .3f : 0; break;
                case "intensity-fraction": s.intensity = control ? .75f : .125f; break;
                case "intensity-one": s.intensity = control ? 0 : 1; break;
                case "mask-r": case "mask-g": case "mask-b": case "mask-a":
                    s.maskEnabled = true;
                    s.maskChannel = "rgba".IndexOf(id[id.Length - 1]);
                    if (control) s.maskChannel = (s.maskChannel + 2) % 4;
                    break;
                case "mask-zero": s.maskEnabled = true; s.mask = control ? Color.white : Color.clear; break;
                case "alpha-and-mask": s.maskEnabled = true; s.maskChannel = 1; s.noise.a = control ? 1 : .375f; break;
                case "noise-disabled": s.noiseEnabled = control; break;
                default: throw new ArgumentException(id);
            }
            return s;
        }

        static Texture2D MakeConsumerMap()
        {
            const int side = 32;
            var texture = new Texture2D(side, side, TextureFormat.RGBAHalf, false, true);
            var pixels = new Color[side * side];
            for (int y = 0; y < side; y++) for (int x = 0; x < side; x++)
            {
                float u = x / (side - 1f), v = y / (side - 1f);
                pixels[y * side + x] = new Color(.1f + .6f * u + .2f * v, .15f + .1f * u + .6f * v, .2f + .3f * u + .25f * v, .2f + .25f * u + .5f * v);
            }
            texture.SetPixels(pixels); texture.Apply(false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }

        static void Configure(Material m, string consumer, Texture2D map, Texture2D noise, Texture2D mask)
        {
            m.shaderKeywords = new[] { "_FX_LIGHT_MODE_UNLIT" };
            m.SetTexture("_BaseMap", consumer == "base" ? map : Texture2D.whiteTexture);
            m.SetTexture("_NoiseMap", noise); m.SetTexture("_NoiseMaskMap", mask);
            foreach (string property in new[] { "_BaseMap", "_NoiseMap", "_NoiseMaskMap", "_EmissionMap", "_DissolveMap", "_MaskMap" })
            {
                m.SetTextureScale(property, Vector2.one); m.SetTextureOffset(property, Vector2.zero);
            }
            m.SetColor("_BaseColor", Color.white); m.SetColor("_ColorA", Color.white);
            m.SetFloat("_BaseColorIntensityForTimeline", 1); m.SetFloat("_AlphaAll", 1);
            m.SetFloat("_Cull", (float)CullMode.Off); m.SetFloat("_ZTest", (float)CompareFunction.LessEqual); m.SetFloat("_ZWrite", 0);
            m.SetFloat("_SrcBlend", (float)BlendMode.One); m.SetFloat("_DstBlend", (float)BlendMode.Zero);
            m.SetFloat("_ColorMask", 15); m.SetFloat("_fogintensity", 0); m.renderQueue = 3000;
            m.SetFloat("_BaseMapUVRotation", 0); m.SetFloat("_NoiseMapUVRotation", 0); m.SetVector("_NoiseOffset", Vector4.zero);
            m.SetVector("_BaseMapMaskMapOffset", Vector4.zero); m.SetVector("_EmissionMapUVOffset", Vector4.zero);
            m.SetVector("_MaskMapOffsetAnition", Vector4.zero); m.SetFloat("_EmissionMapUVRotation", 0); m.SetFloat("_MaskMapUVRotation", 0);
            m.SetFloat("_TexDistortion_intensity", consumer == "base" ? 1 : 0);
            m.SetFloat("_Emi_Distortion_intensity", consumer == "emission" ? 1 : 0);
            m.SetFloat("_MaskDistortion_intensity", consumer == "mask" ? 1 : 0);
            m.SetVector("_DissolveOffsetRotateDistort", new Vector4(0, 0, 0, consumer == "dissolve" ? 1 : 0));
            m.SetInteger("_W9ParticleShaderFlags1", 1 << 9); // Existing ignore-vertex-color protocol.
            m.SetInteger("_W9ParticleShaderWrapFlags", (1 << 0) | (1 << 1) | (1 << 4) | (1 << 5)); // Clamp the four consumers.
            m.SetInteger("_NBShaderForceNoMipFlags", (1 << 0) | (1 << 6) | (1 << 9) | (1 << 10) | (1 << 11) | (1 << 14));
            foreach (string property in new[] { "_W9ParticleCustomDataFlag0", "_W9ParticleCustomDataFlag1", "_W9ParticleCustomDataFlag2", "_W9ParticleCustomDataFlag3", "_UVModeFlag0", "_UVModeFlagType0", "_W9ParticleShaderPNoiseBlendFlag" })
                m.SetInteger(property, 0);
            m.SetShaderPassEnabled("SRPDefaultUnlit", false); m.SetShaderPassEnabled("SRPDEFAULTUNLIT", false);
            m.SetShaderPassEnabled("UniversalForward", true);
            foreach (string pass in new[] { "DepthOnly", "ShadowCaster", "NBCameraOpaqueDistortPass", "NBDeferredDistortPass", "Universal2D" })
                m.SetShaderPassEnabled(pass, false);
            Assert.That(m.FindPass("UniversalForward"), Is.GreaterThanOrEqualTo(0));
            switch (consumer)
            {
                case "base": break;
                case "emission":
                    m.EnableKeyword("_EMISSION"); m.SetFloat("_EmissionEnabled", 1); m.SetTexture("_EmissionMap", map);
                    m.SetColor("_EmissionMapColor", Color.white); m.SetFloat("_EmissionMapColorIntensity", 1); m.SetFloat("_EmissionAlphaIntensity", 1); break;
                case "dissolve":
                    m.EnableKeyword("_DISSOLVE"); m.SetFloat("_Dissolve_Toggle", 1); m.SetTexture("_DissolveMap", map);
                    m.SetVector("_Dissolve", new Vector4(.5f, 1, 0, 1)); break;
                case "mask":
                    m.EnableKeyword("_MASKMAP_ON"); m.SetFloat("_Mask_Toggle", 1); m.SetTexture("_MaskMap", map);
                    m.SetVector("_MaskMapVec", new Vector4(1, 0, 0, 0)); break;
                default: throw new ArgumentException(consumer);
            }
        }

        static void ApplyState(Material current, Material frozen, Texture2D noise, Texture2D mask, State s)
        {
            noise.SetPixel(0, 0, s.noise); noise.Apply(false);
            mask.SetPixel(0, 0, s.mask); mask.Apply(false);
            foreach (var m in new[] { current, frozen })
            {
                if (s.noiseEnabled) m.EnableKeyword("_NOISEMAP"); else m.DisableKeyword("_NOISEMAP");
                if (s.maskEnabled) m.EnableKeyword("_NOISE_MASKMAP"); else m.DisableKeyword("_NOISE_MASKMAP");
                m.SetFloat("_noisemapEnabled", s.noiseEnabled ? 1 : 0); m.SetFloat("_noiseMaskMap_Toggle", s.maskEnabled ? 1 : 0);
                m.SetFloat("_NoiseIntensity", s.intensity); m.SetVector("_DistortionDirection", new Vector4(s.direction.x, s.direction.y, 0, 0));
                m.SetInteger("_W9ParticleShaderFlags", s.normalizeRG ? 1 << 12 : 0);
                m.SetInteger("_W9ParticleShaderColorChannelFlag", 3 | (s.maskChannel << 8)); // Base A; Mask/Dissolve R; NoiseMask selected channel.
            }
        }

        static Color[] Capture(Camera camera, RenderTexture target, Texture2D readback, string path)
        {
            for (int i = 0; i < 4; i++) camera.Render();
            var previous = RenderTexture.active;
            try
            {
                RenderTexture.active = target; readback.ReadPixels(new Rect(0, 0, Size, Size), 0, 0); readback.Apply(false);
                Color[] pixels = readback.GetPixels();
                using (var file = File.Create(path + ".rgba-f32.gz")) using (var gzip = new GZipStream(file, CompressionMode.Compress)) using (var writer = new BinaryWriter(gzip))
                    foreach (Color c in pixels) { writer.Write(c.r); writer.Write(c.g); writer.Write(c.b); writer.Write(c.a); }
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
                int i = y * Size + x; float rgb = 0;
                for (int channel = 0; channel < 3; channel++) rgb = Mathf.Max(rgb, Mathf.Abs(a[i][channel] - b[i][channel]));
                float alpha = Mathf.Abs(a[i].a - b[i].a); m.roiPixels++;
                if (a[i].maxColorComponent > .01f) m.frozenVisible++;
                if (b[i].maxColorComponent > .01f) m.currentVisible++;
                if (rgb > 0) m.differingRGB++; if (alpha > 0) m.differingAlpha++;
                m.maxRGB = Mathf.Max(m.maxRGB, rgb); m.maxAlpha = Mathf.Max(m.maxAlpha, alpha);
            }
            return m;
        }
        static float MaxDelta(Color[] a, Color[] b)
        { var m = Compare(a, b); return Mathf.Max(m.maxRGB, m.maxAlpha); }
        static int CountControlPixels(Color[] a, Color[] b)
        {
            int count = 0;
            for (int y = RoiMin; y < RoiMax; y++) for (int x = RoiMin; x < RoiMax; x++)
            {
                int i = y * Size + x; float delta = 0;
                for (int channel = 0; channel < 4; channel++) delta = Mathf.Max(delta, Mathf.Abs(a[i][channel] - b[i][channel]));
                if (delta > ControlThreshold) count++;
            }
            return count;
        }
        static bool AllFinite(params Color[][] captures)
        {
            foreach (var capture in captures) foreach (Color pixel in capture) for (int channel = 0; channel < 4; channel++)
                if (float.IsNaN(pixel[channel]) || float.IsInfinity(pixel[channel])) return false;
            return true;
        }
    }
}
