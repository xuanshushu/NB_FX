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
    /// N1 only: ordinary Mesh current ShaderLab (B) / Shader Graph (C).
    /// Screen RT, CustomData, Refraction, PNoise, chromatic aberration and VFX
    /// are deliberately pending. Strict numerical differences stay failures.
    /// No saved materials, scenes, pipeline settings or renderer features change.
    /// </summary>
    public sealed class G4GraphTextureNoiseTests
    {
        const string GraphPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph";
        const string LegacyPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader";
        const int Size = 128, Layer = 2, RoiMin = 32, RoiMax = 96;
        const float ControlThreshold = .01f;
        static readonly string[] Consumers = { "base", "emission", "dissolve", "mask", "overlay2", "dissolve-mask" };
        static readonly string[] Inputs =
        {
            "unsigned", "signed", "unsigned-rg0", "unsigned-rg1", "signed-rg0", "signed-rg1",
            "alpha-zero", "alpha-weight", "alpha-one",
            "direction-zero", "direction-x", "direction-y", "direction-negative",
            "intensity-zero", "intensity-fraction", "intensity-one", "intensity-negative",
            "mask-r", "mask-g", "mask-b", "mask-a", "mask-zero", "alpha-and-mask", "noise-disabled"
        };

        [Serializable] sealed class State
        {
            public bool noiseEnabled = true, normalizeRG, maskEnabled;
            public Color noise = new Color(.75f, .25f, 0, 1);
            public Color mask = new Color(.125f, .375f, .625f, .875f);
            public Vector2 direction = new Vector2(1, .5f);
            public float intensity = .3f;
            public int maskChannel, noiseWrap, maskWrap;
            public bool forceNoiseLod0 = true, forceMaskLod0 = true;
            public Vector4 noiseST = new Vector4(1, 1, 0, 0), maskST = new Vector4(1, 1, 0, 0);
            public float noiseRotation;
            public string noiseRoute = "default-uv0", maskRoute = "default-uv0";
        }

        [Serializable] sealed class Metrics
        {
            public string caseId, unityVersion, api, target, note;
            public State input, control;
            public int roiPixels, differingRGB, differingAlpha, controlDifferingRGB, controlDifferingAlpha;
            public int legacyVisible, graphVisible, legacyControlPixels, graphControlPixels;
            public bool finite;
            public float maxRGB, maxAlpha, controlMaxRGB, controlMaxAlpha;
            public float legacyRepeat, graphRepeat, legacyControlDelta, graphControlDelta;
            public float legacyExcludedFeatureControl, graphExcludedFeatureControl;
        }

        static IEnumerable<TestCaseData> Cases()
        {
            foreach (string consumer in Consumers)
                foreach (string input in Inputs)
                    yield return new TestCaseData(consumer, input).SetName("G4TextureNoiseBC_" + consumer + "_" + input);
            foreach (string input in new[] { "unsigned", "signed", "alpha-and-mask", "direction-negative", "noise-disabled" })
                yield return new TestCaseData("combined", input).SetName("G4TextureNoiseBC_combined_" + input);
            foreach (string input in new[] { "noise-nonuniform", "noise-mask-nonuniform", "noise-rotation", "noise-st", "noise-mask-st" })
                yield return new TestCaseData("base", input).SetName("G4TextureNoiseBC_base_" + input);
            foreach (string prefix in new[] { "noise", "noise-mask" })
            {
                foreach (string mode in new[] { "wrap0", "wrap1", "wrap2", "wrap3", "lod-auto", "lod-force" })
                    yield return new TestCaseData("base", prefix + "-" + mode).SetName("G4TextureNoiseBC_base_" + prefix + "-" + mode);
                foreach (string route in new[] { "special-uv0zw", "special-uv1", "special-uv2", "twirl", "shared-special-uv2" })
                    yield return new TestCaseData("base", prefix + "-route-" + route).SetName("G4TextureNoiseBC_base_" + prefix + "-route-" + route);
            }
            foreach (string consumer in new[] { "mask2-only", "mask3-only", "color-ramp-only" })
                yield return new TestCaseData(consumer, "noise-nonuniform").SetName("G4TextureNoiseBC_excluded_" + consumer);
        }

        [TestCaseSource(nameof(Cases))]
        public void TextureNoiseMatchesShaderLab(string consumer, string inputId)
        {
            Assert.That(GraphicsSettings.currentRenderPipeline, Is.InstanceOf<UniversalRenderPipelineAsset>());
            Assert.That(SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf), Is.True);
            var graphShader = AssetDatabase.LoadAssetAtPath<Shader>(GraphPath);
            var legacyShader = AssetDatabase.LoadAssetAtPath<Shader>(LegacyPath);
            Assert.That(graphShader, Is.Not.Null);
            Assert.That(legacyShader, Is.Not.Null);
            Assert.That(legacyShader.name, Is.EqualTo("Effects/NBShader"));
            Assert.That(graphShader.isSupported && legacyShader.isSupported, Is.True);
            string root = Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR");
            if (string.IsNullOrEmpty(root)) root = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/NBFXG4TextureNoise");
            string output = Path.Combine(root, "g4-texture-noise", consumer + "-" + inputId);
            Directory.CreateDirectory(output);
            var scene = EditorSceneManager.NewPreviewScene();
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var cameraObject = new GameObject("NBFX ordinary Mesh texture Noise B/C");
            SceneManager.MoveGameObjectToScene(quad, scene);
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<Camera>();
            var graph = new Material(graphShader);
            var legacy = new Material(legacyShader);
            var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            var readback = new Texture2D(Size, Size, TextureFormat.RGBAHalf, false, true);
            var map = MakeConsumerMap();
            var noise = MakeNoiseTexture(inputId, false);
            var mask = MakeNoiseTexture(inputId, true);
            var ownedMesh = UnityEngine.Object.Instantiate(quad.GetComponent<MeshFilter>().sharedMesh);
            quad.GetComponent<MeshFilter>().sharedMesh = ownedMesh;
            var previousActive = RenderTexture.active;
            try
            {
                State input = MakeState(inputId, false), control = MakeState(inputId, true);
                SetDistinctUVStreams(ownedMesh);
                Configure(graph, true, consumer, map, noise, mask);
                Configure(legacy, false, consumer, map, noise, mask);
                ApplyState(graph, legacy, noise, mask, input);
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
                renderer.sharedMaterial = legacy;
                Color[] a = Capture(camera, target, readback, Path.Combine(output, "B-shaderlab"));
                Color[] ar = Capture(camera, target, readback, Path.Combine(output, "B-repeat"));
                renderer.sharedMaterial = graph;
                Color[] b = Capture(camera, target, readback, Path.Combine(output, "C-graph"));
                Color[] br = Capture(camera, target, readback, Path.Combine(output, "C-repeat"));
                ApplyState(graph, legacy, noise, mask, control);
                renderer.sharedMaterial = legacy;
                Color[] ac = Capture(camera, target, readback, Path.Combine(output, "B-control"));
                renderer.sharedMaterial = graph;
                Color[] bc = Capture(camera, target, readback, Path.Combine(output, "C-control"));
                Metrics metrics = Compare(a, b);
                Metrics controlParity = Compare(ac, bc);
                metrics.caseId = consumer + "-" + inputId;
                metrics.unityVersion = Application.unityVersion;
                metrics.api = SystemInfo.graphicsDeviceType.ToString();
                metrics.target = "128x128 linear RGBAHalf; lossless float32 RGBA; strict safe ROI x/y=32..95";
                metrics.note = "N1 ordinary Mesh ShaderLab B / Graph C; four warm-up renders, no first-frame/screen RT/CustomData/PNoise/Refraction/chroma/VFX claim. Default UV0 or explicitly selected independent Noise/NoiseMask modes 0/1/2/8; zero speeds. Fixed strict RGBA zero, repeat zero and >.01 positive controls; no tolerance relaxation. Mask2/3/ColorRamp are intentional nonconsumers.";
                metrics.input = input; metrics.control = control;
                metrics.controlDifferingRGB = controlParity.differingRGB;
                metrics.controlDifferingAlpha = controlParity.differingAlpha;
                metrics.controlMaxRGB = controlParity.maxRGB;
                metrics.controlMaxAlpha = controlParity.maxAlpha;
                metrics.legacyRepeat = MaxDelta(a, ar);
                metrics.graphRepeat = MaxDelta(b, br);
                metrics.legacyControlDelta = MaxDelta(a, ac);
                metrics.graphControlDelta = MaxDelta(b, bc);
                metrics.legacyControlPixels = CountControlPixels(a, ac);
                metrics.graphControlPixels = CountControlPixels(b, bc);
                metrics.finite = AllFinite(a, ar, b, br, ac, bc);
                bool excluded = consumer.EndsWith("-only", StringComparison.Ordinal);
                if (excluded)
                {
                    string property = consumer == "mask2-only" ? "_MaskMap2" : consumer == "mask3-only" ? "_MaskMap3" : "_RampColorMap";
                    foreach (Material material in new[] { legacy, graph })
                    {
                        material.SetTextureScale(property, new Vector2(.6f, .7f));
                        material.SetTextureOffset(property, new Vector2(.25f, .2f));
                    }
                    renderer.sharedMaterial = legacy;
                    Color[] af = Capture(camera, target, readback, Path.Combine(output, "B-feature-control"));
                    renderer.sharedMaterial = graph;
                    Color[] bf = Capture(camera, target, readback, Path.Combine(output, "C-feature-control"));
                    metrics.legacyExcludedFeatureControl = MaxDelta(ac, af);
                    metrics.graphExcludedFeatureControl = MaxDelta(bc, bf);
                    metrics.finite &= AllFinite(af, bf);
                    Metrics featureParity = Compare(af, bf);
                    metrics.controlDifferingRGB += featureParity.differingRGB;
                    metrics.controlDifferingAlpha += featureParity.differingAlpha;
                }
                File.WriteAllText(Path.Combine(output, "metrics.json"), JsonUtility.ToJson(metrics, true));
                Debug.Log("NBFX_G4_TEXTURE_NOISE_BC " + JsonUtility.ToJson(metrics));
                Assert.That(metrics.finite, Is.True, "Nonfinite raw output.");
                Assert.That(metrics.legacyVisible, Is.GreaterThan(100));
                Assert.That(metrics.graphVisible, Is.GreaterThan(100));
                Assert.That(metrics.legacyRepeat, Is.Zero);
                Assert.That(metrics.graphRepeat, Is.Zero);
                if (excluded)
                {
                    Assert.That(metrics.legacyControlDelta, Is.Zero, "Mask2/3/ColorRamp must not consume texture Noise.");
                    Assert.That(metrics.graphControlDelta, Is.Zero, "Graph invented Noise behavior for a nonconsumer.");
                    Assert.That(metrics.legacyExcludedFeatureControl, Is.GreaterThan(ControlThreshold));
                    Assert.That(metrics.graphExcludedFeatureControl, Is.GreaterThan(ControlThreshold));
                }
                else
                {
                    Assert.That(metrics.legacyControlDelta, Is.GreaterThan(ControlThreshold), "ShaderLab parameter positive control had no effect.");
                    Assert.That(metrics.graphControlDelta, Is.GreaterThan(ControlThreshold), "Graph parameter positive control had no effect.");
                    Assert.That(metrics.legacyControlPixels, Is.GreaterThan(64));
                    Assert.That(metrics.graphControlPixels, Is.GreaterThan(64));
                }
                Assert.That(metrics.differingRGB, Is.Zero, "Strict RGB ShaderLab/Graph mismatch.");
                Assert.That(metrics.differingAlpha, Is.Zero, "Strict Alpha ShaderLab/Graph mismatch.");
                Assert.That(metrics.controlDifferingRGB, Is.Zero, "Strict RGB positive-control B/C mismatch.");
                Assert.That(metrics.controlDifferingAlpha, Is.Zero, "Strict Alpha positive-control B/C mismatch.");
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previousActive;
                target.Release();
                foreach (var obj in new UnityEngine.Object[] { graph, legacy, target, readback, map, noise, mask, ownedMesh })
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
                case "intensity-negative": s.intensity = control ? .3f : -.3f; break;
                case "mask-r": case "mask-g": case "mask-b": case "mask-a":
                    s.maskEnabled = true;
                    s.maskChannel = "rgba".IndexOf(id[id.Length - 1]);
                    if (control) s.maskChannel = (s.maskChannel + 2) % 4;
                    break;
                case "mask-zero": s.maskEnabled = true; s.mask = control ? Color.white : Color.clear; break;
                case "alpha-and-mask": s.maskEnabled = true; s.maskChannel = 1; s.noise.a = control ? 1 : .375f; break;
                case "noise-disabled": s.noiseEnabled = control; break;
                default:
                    if (!id.StartsWith("noise-", StringComparison.Ordinal)) throw new ArgumentException(id);
                    bool maskInput = id.StartsWith("noise-mask-", StringComparison.Ordinal);
                    s.maskEnabled = maskInput;
                    if (id.Contains("-wrap"))
                    {
                        int wrap = id[id.Length - 1] - '0';
                        // Asymmetric modes need an axis-specific response:
                        // mode 2 differs from repeat by V, mode 3 by U.
                        if (maskInput) s.maskChannel = wrap == 2 ? 1 : 0;
                        if (control) wrap = wrap == 0 ? 1 : 0;
                        if (maskInput) { s.maskWrap = wrap; s.maskST = new Vector4(2, 3, -.25f, 1.25f); }
                        else { s.noiseWrap = wrap; s.noiseST = new Vector4(2, 3, -.25f, 1.25f); }
                    }
                    else if (id.Contains("-lod-"))
                    {
                        bool force = id.EndsWith("force", StringComparison.Ordinal) != control;
                        if (maskInput) { s.forceMaskLod0 = force; s.maskST = new Vector4(16, 16, -.25f, -.25f); }
                        else { s.forceNoiseLod0 = force; s.noiseST = new Vector4(16, 16, -.25f, -.25f); }
                    }
                    else if (id.Contains("-route-"))
                    {
                        string route = control ? "default-uv0" : id.Substring(id.IndexOf("-route-", StringComparison.Ordinal) + 7);
                        if (maskInput) s.maskRoute = route; else s.noiseRoute = route;
                    }
                    else if (id == "noise-rotation") s.noiseRotation = control ? 0 : 17;
                    else if (id.EndsWith("-st", StringComparison.Ordinal))
                    {
                        var st = control ? new Vector4(1, 1, 0, 0) : new Vector4(1.5f, .75f, -.25f, .15f);
                        if (maskInput) s.maskST = st; else s.noiseST = st;
                    }
                    else s.noiseEnabled = !control; // Nonuniform-map on/off control.
                    break;
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

        static void Configure(Material m, bool graph, string consumer, Texture2D map, Texture2D noise, Texture2D mask)
        {
            if (graph) m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            else m.shaderKeywords = new[] { "_FX_LIGHT_MODE_UNLIT" };
            m.SetTexture("_BaseMap", consumer == "base" || consumer == "combined" ? map : Texture2D.whiteTexture);
            m.SetTexture("_NoiseMap", noise); m.SetTexture("_NoiseMaskMap", mask);
            foreach (string property in new[] { "_BaseMap", "_NoiseMap", "_NoiseMaskMap", "_EmissionMap", "_DissolveMap", "_DissolveMaskMap", "_MaskMap", "_MaskMap2", "_MaskMap3", "_ColorBlendMap", "_RampColorMap" })
            {
                m.SetTextureScale(property, Vector2.one); m.SetTextureOffset(property, Vector2.zero);
            }
            m.SetColor(graph ? "_Color" : "_BaseColor", Color.white); m.SetColor("_ColorA", Color.white);
            m.SetFloat("_BaseColorIntensityForTimeline", 1); m.SetFloat("_AlphaAll", 1);
            m.SetFloat("_Cull", (float)CullMode.Off); m.SetFloat("_ZTest", (float)CompareFunction.LessEqual); m.SetFloat("_ZWrite", 0);
            m.SetFloat("_SrcBlend", (float)BlendMode.One); m.SetFloat("_DstBlend", (float)BlendMode.Zero);
            if (graph)
            {
                m.SetFloat("_Surface", 1);
                m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One); m.SetFloat("_DstBlendAlpha", (float)BlendMode.Zero);
                m.SetFloat("_NB_DistortionMode", 0); m.SetVector("_BaseMap_ST", new Vector4(1, 1, 0, 0));
            }
            else { m.SetFloat("_ColorMask", 15); m.SetFloat("_fogintensity", 0); }
            m.renderQueue = 3000;
            m.SetFloat("_BaseMapUVRotation", 0); m.SetFloat("_NoiseMapUVRotation", 0); m.SetVector("_NoiseOffset", Vector4.zero);
            m.SetVector("_BaseMapMaskMapOffset", Vector4.zero); m.SetVector("_EmissionMapUVOffset", Vector4.zero);
            m.SetVector("_MaskMapOffsetAnition", Vector4.zero); m.SetFloat("_EmissionMapUVRotation", 0); m.SetFloat("_MaskMapUVRotation", 0);
            m.SetFloat("_TexDistortion_intensity", consumer == "base" || consumer == "combined" ? 1 : 0);
            m.SetFloat("_Emi_Distortion_intensity", consumer == "emission" || consumer == "combined" ? 1 : 0);
            m.SetFloat("_MaskDistortion_intensity", consumer == "mask" || consumer == "combined" ? 1 : 0);
            m.SetVector("_DissolveOffsetRotateDistort", new Vector4(0, 0, 0, consumer == "dissolve" || consumer == "dissolve-mask" || consumer == "combined" ? 1 : 0));
            m.SetVector("_ColorBlendVec", new Vector4(consumer == "overlay2" || consumer == "combined" ? 1 : 0, 0, 1, 0));
            m.SetTexture("_MaskMap", Texture2D.whiteTexture); m.SetVector("_MaskMapVec", new Vector4(1, 0, 0, 0));
            if (!graph)
            {
                foreach (string property in new[] { "_W9ParticleCustomDataFlag0", "_W9ParticleCustomDataFlag1", "_W9ParticleCustomDataFlag2", "_W9ParticleCustomDataFlag3", "_W9ParticleShaderPNoiseBlendFlag" })
                    m.SetInteger(property, 0);
                m.SetShaderPassEnabled("SRPDefaultUnlit", false); m.SetShaderPassEnabled("SRPDEFAULTUNLIT", false);
                m.SetShaderPassEnabled("UniversalForward", true);
                foreach (string pass in new[] { "DepthOnly", "ShadowCaster", "NBCameraOpaqueDistortPass", "NBDeferredDistortPass", "Universal2D" })
                    m.SetShaderPassEnabled(pass, false);
                Assert.That(m.FindPass("UniversalForward"), Is.GreaterThanOrEqualTo(0));
            }
            switch (consumer)
            {
                case "base": break;
                case "emission":
                    Enable(m, graph, "_EmissionEnabled", "_EMISSION"); m.SetTexture("_EmissionMap", map);
                    m.SetColor("_EmissionMapColor", Color.white); m.SetFloat("_EmissionMapColorIntensity", 1); m.SetFloat("_EmissionAlphaIntensity", 1); break;
                case "dissolve":
                    Enable(m, graph, "_Dissolve_Toggle", "_DISSOLVE"); m.SetTexture("_DissolveMap", map);
                    m.SetVector("_Dissolve", new Vector4(.5f, 1, 0, 1)); break;
                case "mask":
                    Enable(m, graph, "_Mask_Toggle", "_MASKMAP_ON"); m.SetTexture("_MaskMap", map);
                    m.SetVector("_MaskMapVec", new Vector4(1, 0, 0, 0)); break;
                case "overlay2":
                    Enable(m, graph, "_ColorBlendMap_Toggle", "_COLORMAPBLEND"); m.SetTexture("_ColorBlendMap", map);
                    m.SetColor("_ColorBlendColor", Color.white); m.SetFloat("_ColorBlendColorIntensity", 1); break;
                case "dissolve-mask":
                    Enable(m, graph, "_Dissolve_Toggle", "_DISSOLVE"); Enable(m, graph, "_DissolveMask_Toggle", "_DISSOLVE_MASK");
                    m.SetTexture("_DissolveMap", Texture2D.grayTexture); m.SetTexture("_DissolveMaskMap", map);
                    m.SetVector("_Dissolve", new Vector4(.5f, 1, 1, 1)); m.SetFloat("_DissolveMaskMode", 1); break;
                case "combined":
                    Enable(m, graph, "_EmissionEnabled", "_EMISSION"); m.SetTexture("_EmissionMap", map);
                    m.SetColor("_EmissionMapColor", Color.white); m.SetFloat("_EmissionMapColorIntensity", .5f); m.SetFloat("_EmissionAlphaIntensity", 1);
                    Enable(m, graph, "_ColorBlendMap_Toggle", "_COLORMAPBLEND"); m.SetTexture("_ColorBlendMap", map);
                    m.SetColor("_ColorBlendColor", Color.white); m.SetFloat("_ColorBlendColorIntensity", 1);
                    Enable(m, graph, "_Dissolve_Toggle", "_DISSOLVE"); Enable(m, graph, "_DissolveMask_Toggle", "_DISSOLVE_MASK");
                    m.SetTexture("_DissolveMap", map); m.SetTexture("_DissolveMaskMap", map);
                    m.SetVector("_Dissolve", new Vector4(.5f, 1, 1, 1)); m.SetFloat("_DissolveMaskMode", 1);
                    Enable(m, graph, "_Mask_Toggle", "_MASKMAP_ON"); m.SetTexture("_MaskMap", map); break;
                case "mask2-only": case "mask3-only":
                    Enable(m, graph, "_Mask_Toggle", "_MASKMAP_ON");
                    bool layer2 = consumer == "mask2-only";
                    Enable(m, graph, layer2 ? "_Mask2_Toggle" : "_Mask3_Toggle", layer2 ? "_MASKMAP2_ON" : "_MASKMAP3_ON");
                    m.SetTexture(layer2 ? "_MaskMap2" : "_MaskMap3", map); break;
                case "color-ramp-only":
                    Enable(m, graph, "_RampColorToggle", "_COLOR_RAMP"); if (!graph) m.EnableKeyword("_COLOR_RAMP_MAP");
                    m.SetFloat("_RampColorSourceMode", 1); m.SetTexture("_RampColorMap", map);
                    m.SetColor("_RampColor0", new Color(1, 0, 0, 0)); m.SetColor("_RampColor1", new Color(0, 0, 1, 1));
                    m.SetVector("_RampColorAlpha0", new Vector4(1, 0, 1, 1)); m.SetColor("_RampColorBlendColor", Color.white);
                    if (graph) m.SetFloat("_RampColorCount", 131074); else m.SetInteger("_RampColorCount", 131074); break;
                default: throw new ArgumentException(consumer);
            }
        }

        static void Enable(Material m, bool graph, string property, string keyword)
        { m.SetFloat(property, 1); if (!graph) m.EnableKeyword(keyword); }

        static void ApplyState(Material graph, Material legacy, Texture2D noise, Texture2D mask, State s)
        {
            if (noise.width == 1) { noise.SetPixel(0, 0, s.noise); noise.Apply(false); }
            if (mask.width == 1) { mask.SetPixel(0, 0, s.mask); mask.Apply(false); }
            graph.SetFloat("_noisemapEnabled", s.noiseEnabled ? 1 : 0);
            graph.SetFloat("_noiseMaskMap_Toggle", s.maskEnabled ? 1 : 0);
            if (s.noiseEnabled) legacy.EnableKeyword("_NOISEMAP"); else legacy.DisableKeyword("_NOISEMAP");
            if (s.maskEnabled) legacy.EnableKeyword("_NOISE_MASKMAP"); else legacy.DisableKeyword("_NOISE_MASKMAP");
            legacy.SetFloat("_noisemapEnabled", s.noiseEnabled ? 1 : 0); legacy.SetFloat("_noiseMaskMap_Toggle", s.maskEnabled ? 1 : 0);
            foreach (Material m in new[] { legacy, graph })
            {
                m.SetFloat("_NoiseIntensity", s.intensity); m.SetVector("_DistortionDirection", new Vector4(s.direction.x, s.direction.y, 0, 0));
                m.SetTextureScale("_NoiseMap", new Vector2(s.noiseST.x, s.noiseST.y)); m.SetTextureOffset("_NoiseMap", new Vector2(s.noiseST.z, s.noiseST.w));
                m.SetTextureScale("_NoiseMaskMap", new Vector2(s.maskST.x, s.maskST.y)); m.SetTextureOffset("_NoiseMaskMap", new Vector2(s.maskST.z, s.maskST.w));
                m.SetFloat("_NoiseMapUVRotation", s.noiseRotation);
                m.SetVector("_SharedUV_ST", new Vector4(.75f, .875f, .125f, -.125f)); m.SetVector("_SharedUV_Vec", Vector4.zero);
                m.SetVector("_TWParameter", new Vector4(.375f, .625f, 0, 0)); m.SetFloat("_TWStrength", 2.25f);
            }
            uint flags0 = s.normalizeRG ? 1u << 12 : 0, flags1 = 1u << 9, modes = 0, types = 0;
            SetRoute(s.noiseRoute, 8, ref flags0, ref flags1, ref modes, ref types);
            SetRoute(s.maskRoute, 10, ref flags0, ref flags1, ref modes, ref types);
            uint channels = 3u | (uint)(s.maskChannel << 8);
            uint wraps = (1u << 0) | (1u << 1) | (1u << 2) | (1u << 4) | (1u << 5) | (1u << 6) | (1u << 8) | (1u << 11);
            wraps |= ((s.noiseWrap & 1) != 0 ? 1u << 3 : 0) | ((s.noiseWrap & 2) != 0 ? 1u << 19 : 0);
            wraps |= ((s.maskWrap & 1) != 0 ? 1u << 12 : 0) | ((s.maskWrap & 2) != 0 ? 1u << 28 : 0);
            uint mips = (1u << 0) | (1u << 6) | (1u << 7) | (1u << 8) | (1u << 11) | (1u << 12) | (1u << 13) | (1u << 14) | (1u << 15);
            if (s.forceNoiseLod0) mips |= 1u << 9; if (s.forceMaskLod0) mips |= 1u << 10;
            SetWord(graph, "_NB_Flags0Lo16", "_NB_Flags0Hi16", flags0); legacy.SetInteger("_W9ParticleShaderFlags", unchecked((int)flags0));
            SetWord(graph, "_NB_Flags1Lo16", "_NB_Flags1Hi16", flags1); legacy.SetInteger("_W9ParticleShaderFlags1", unchecked((int)flags1));
            SetWord(graph, "_NB_UVModeFlag0Lo16", "_NB_UVModeFlag0Hi16", modes); legacy.SetInteger("_UVModeFlag0", unchecked((int)modes));
            SetWord(graph, "_NB_UVModeFlagType0Lo16", "_NB_UVModeFlagType0Hi16", types); legacy.SetInteger("_UVModeFlagType0", unchecked((int)types));
            SetWord(graph, "_NB_WrapFlagsLo16", "_NB_WrapFlagsHi16", wraps); legacy.SetInteger("_W9ParticleShaderWrapFlags", unchecked((int)wraps));
            SetWord(graph, "_NB_ForceNoMipFlagsLo16", "_NB_ForceNoMipFlagsHi16", mips); legacy.SetInteger("_NBShaderForceNoMipFlags", unchecked((int)mips));
            graph.SetFloat("_NB_ColorChannelLo16", channels); legacy.SetInteger("_W9ParticleShaderColorChannelFlag", unchecked((int)channels));
        }

        static void SetRoute(string route, int pos, ref uint flags0, ref uint flags1, ref uint modes, ref uint types)
        {
            int mode = 0;
            switch (route)
            {
                case "default-uv0": break;
                case "special-uv0zw": mode = 1; break;
                case "special-uv1": mode = 1; flags1 |= (1u << 21) | (1u << 18); break;
                case "special-uv2": mode = 1; flags1 |= (1u << 21) | (1u << 19); break;
                case "twirl": mode = 2; flags0 |= 1u << 9; break;
                case "shared-special-uv2":
                    mode = 8; flags1 |= (1u << 21) | (1u << 19); modes |= 1u << 30; break;
                default: throw new ArgumentException(route);
            }
            modes |= (uint)(mode & 3) << pos; types |= (uint)(mode >> 2) << pos;
        }
        static void SetWord(Material m, string lo, string hi, uint value)
        { m.SetFloat(lo, value & 65535u); m.SetFloat(hi, value >> 16); }

        static void SetDistinctUVStreams(Mesh mesh)
        {
            var uv0 = new List<Vector4>(); var uv1 = new List<Vector4>(); var uv2 = new List<Vector4>();
            foreach (Vector2 uv in mesh.uv)
            {
                float x = uv.x, y = uv.y;
                uv0.Add(new Vector4(x, y, .875f - .625f * x, .125f + .625f * y));
                uv1.Add(new Vector4(.125f + .5f * x, .875f - .5f * y, 0, 0));
                uv2.Add(new Vector4(.75f - .5f * x, .0625f + .5f * y, 0, 0));
            }
            mesh.SetUVs(0, uv0); mesh.SetUVs(1, uv1); mesh.SetUVs(2, uv2);
        }

        static Texture2D MakeNoiseTexture(string inputId, bool mask)
        {
            bool variable = inputId.StartsWith("noise-", StringComparison.Ordinal) && inputId != "noise-disabled" &&
                mask == inputId.StartsWith("noise-mask-", StringComparison.Ordinal);
            bool lod = variable && inputId.Contains("-lod-");
            int n = variable ? lod ? 512 : 8 : 1;
            var texture = new Texture2D(n, n, TextureFormat.RGBAHalf, lod, true);
            if (variable)
            {
                var pixels = new Color[n * n];
                for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                {
                    if (lod) pixels[y * n + x] = ((x / 8 + y / 8) & 1) == 0 ? new Color(.75f, .125f, .25f, 1) : new Color(.125f, .75f, .625f, 1);
                    else pixels[y * n + x] = new Color(.125f + .75f * x / (n - 1f), .125f + .75f * y / (n - 1f), .25f, 1);
                }
                texture.SetPixels(pixels); texture.Apply(lod);
                if (lod)
                {
                    for (int mip = 1; mip < texture.mipmapCount; mip++)
                    {
                        var pixelsMip = new Color[Mathf.Max(1, n >> mip) * Mathf.Max(1, n >> mip)];
                        for (int i = 0; i < pixelsMip.Length; i++) pixelsMip[i] = new Color(.95f, .95f, .125f, 1);
                        texture.SetPixels(pixelsMip, mip);
                    }
                    texture.Apply(false);
                }
            }
            texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
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
                if (a[i].maxColorComponent > .01f) m.legacyVisible++;
                if (b[i].maxColorComponent > .01f) m.graphVisible++;
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
