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
    /// M0: steady ordinary Mesh ShaderLab B / Graph C MatCap, without
    /// NormalMap/lighting. Original view/reflection/composite math, fixed linear
    /// clamp, no texture ST/wrap protocol, and ForceNoMip bit5. Strict raw RGBA
    /// differences are retained and fail; historical A/Frozen microdiff waivers
    /// are NOT a new B/C tolerance. No saved scene/pipeline/material mutation.
    /// </summary>
    public sealed class G4GraphMatCapTests
    {
        const string GraphPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph";
        const string LegacyPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader";
        const int Size = 128, Layer = 2, RoiMin = 40, RoiMax = 88;
        const float ControlThreshold = .01f;
        static readonly string[] Names =
        {
            "mix-add", "mix-mid", "mix-multiply", "alpha-zero", "alpha-half", "alpha-one",
            "hdr-tint", "nonuniform", "disabled", "mesh-yaw", "mesh-pitch", "back-face",
            "back-rotated", "camera-yaw", "camera-roll", "lod-auto", "lod-force",
            "st-ignored", "wrap-flags-ignored", "sampler-ignored",
            "early-adjustment-order", "late-adjustment-order", "emission-order"
        };

        [Serializable] sealed class State
        {
            public bool enabled = true, forceLod0 = true;
            public float multiplyBlend = 1;
            public Color color = Color.white;
            public Vector4 textureST = new Vector4(1, 1, 0, 0);
            public uint wrapFlags;
            public bool pointRepeatSampler = true;
        }
        [Serializable] sealed class Metrics
        {
            public string caseId, unityVersion, api, target, note;
            public bool orthographic, finite, excludedInput;
            public State input, control;
            public int roiPixels, differingRGB, differingAlpha, controlDifferingRGB, controlDifferingAlpha;
            public int legacyVisible, graphVisible, legacyControlPixels, graphControlPixels;
            public int legacyFeaturePixels, graphFeaturePixels;
            public float maxRGB, maxAlpha, controlMaxRGB, controlMaxAlpha;
            public float legacyRepeat, graphRepeat, legacyControlRepeat, graphControlRepeat;
            public float legacyControlDelta, graphControlDelta, legacyControlAlpha, graphControlAlpha;
            public float legacyFeatureDelta, graphFeatureDelta, legacyFeatureRepeat, graphFeatureRepeat;
        }

        static IEnumerable<TestCaseData> Cases()
        {
            foreach (string name in Names)
                foreach (bool ortho in new[] { true, false })
                    yield return new TestCaseData(name, ortho)
                        .SetName("G4MatCapBC_" + name + "_" + (ortho ? "ortho" : "perspective"));
        }

        [TestCaseSource(nameof(Cases))]
        public void MatCapMatchesShaderLab(string name, bool ortho)
        {
            Assert.That(GraphicsSettings.currentRenderPipeline, Is.InstanceOf<UniversalRenderPipelineAsset>());
            Assert.That(SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf), Is.True);
            var graphShader = AssetDatabase.LoadAssetAtPath<Shader>(GraphPath);
            var legacyShader = AssetDatabase.LoadAssetAtPath<Shader>(LegacyPath);
            Assert.That(graphShader && legacyShader && graphShader.isSupported && legacyShader.isSupported, Is.True);
            string root = Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR");
            if (string.IsNullOrEmpty(root)) root = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/NBFXG4MatCap");
            string id = name + "-" + (ortho ? "ortho" : "perspective");
            string output = Path.Combine(root, "g4-matcap", id);
            Directory.CreateDirectory(output);
            var scene = EditorSceneManager.NewPreviewScene();
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var cameraObject = new GameObject("NBFX M0 ordinary Mesh MatCap B/C");
            SceneManager.MoveGameObjectToScene(quad, scene);
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<Camera>();
            var graph = new Material(graphShader);
            var legacy = new Material(legacyShader);
            var map = MakeMatCapTexture(name.StartsWith("lod-", StringComparison.Ordinal));
            var baseMap = MakeConstantTexture(new Color(.125f, .25f, .5f, .75f));
            var emission = MakeConstantTexture(new Color(.25f, .75f, .125f, .75f));
            var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            var readback = new Texture2D(Size, Size, TextureFormat.RGBAHalf, false, true);
            var previous = RenderTexture.active;
            try
            {
                Configure(graph, true, name, baseMap, emission);
                Configure(legacy, false, name, baseMap, emission);
                State input = MakeState(name, false), control = MakeState(name, true);
                quad.layer = Layer;
                quad.transform.localScale = new Vector3(2, 2, 1);
                if (name == "mesh-yaw") quad.transform.rotation = Quaternion.Euler(0, 23, 0);
                if (name == "mesh-pitch") quad.transform.rotation = Quaternion.Euler(19, 0, 0);
                if (name == "back-face") quad.transform.rotation = Quaternion.Euler(0, 180, 0);
                if (name == "back-rotated") quad.transform.rotation = Quaternion.Euler(12, 199, 4);
                var renderer = quad.GetComponent<MeshRenderer>();
                renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                camera.scene = scene;
                camera.orthographic = ortho; camera.orthographicSize = 1.5f; camera.fieldOfView = 45;
                camera.nearClipPlane = .1f; camera.farClipPlane = 20;
                float distance = name.StartsWith("lod-", StringComparison.Ordinal) ? 2.8f : 5;
                camera.transform.position = name == "camera-yaw" ? new Vector3(1.25f, .375f, distance) : new Vector3(0, 0, distance);
                camera.transform.rotation = Quaternion.LookRotation(-camera.transform.position, Vector3.up);
                if (name == "camera-roll") camera.transform.rotation *= Quaternion.Euler(0, 0, 17);
                camera.cullingMask = 1 << Layer; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.clear;
                camera.allowHDR = true; camera.allowMSAA = false; camera.targetTexture = target;
                cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
                target.Create(); Assert.That(target.IsCreated() && !target.sRGB, Is.True);

                ApplyState(graph, legacy, map, input);
                renderer.sharedMaterial = legacy;
                Color[] b = Capture(camera, target, readback, Path.Combine(output, "B-shaderlab"));
                Color[] br = Capture(camera, target, readback, Path.Combine(output, "B-repeat"));
                renderer.sharedMaterial = graph;
                Color[] c = Capture(camera, target, readback, Path.Combine(output, "C-graph"));
                Color[] cr = Capture(camera, target, readback, Path.Combine(output, "C-repeat"));
                ApplyState(graph, legacy, map, control);
                renderer.sharedMaterial = legacy;
                Color[] bc = Capture(camera, target, readback, Path.Combine(output, "B-counterfactual"));
                Color[] bcr = Capture(camera, target, readback, Path.Combine(output, "B-counterfactual-repeat"));
                renderer.sharedMaterial = graph;
                Color[] cc = Capture(camera, target, readback, Path.Combine(output, "C-counterfactual"));
                Color[] ccr = Capture(camera, target, readback, Path.Combine(output, "C-counterfactual-repeat"));
                Metrics m = Compare(b, c), mc = Compare(bc, cc);
                m.caseId = id; m.unityVersion = Application.unityVersion; m.api = SystemInfo.graphicsDeviceType.ToString();
                m.orthographic = ortho; m.target = "128x128 linear RGBAHalf, raw float32 RGBA gzip; strict interior ROI x/y=40..87";
                m.note = "M0 ordinary Mesh only: geometry normal MatCap. Four warm-up renders per capture; no first-frame/NormalMap/lighting/screen RT/VFX claim. Fixed strict RGBA zero and repeat zero, >.01 and >=64-pixel positive controls. ST/wrap/texture-sampler cases require zero input effect PLUS separate strong MatCap toggle control. Historical MatCap A/Frozen microdiff exception is not a B/C waiver.";
                m.input = input; m.control = control; m.excludedInput = name.EndsWith("-ignored", StringComparison.Ordinal);
                m.controlDifferingRGB = mc.differingRGB; m.controlDifferingAlpha = mc.differingAlpha;
                m.controlMaxRGB = mc.maxRGB; m.controlMaxAlpha = mc.maxAlpha;
                m.legacyRepeat = MaxDelta(b, br); m.graphRepeat = MaxDelta(c, cr);
                m.legacyControlRepeat = MaxDelta(bc, bcr); m.graphControlRepeat = MaxDelta(cc, ccr);
                m.legacyControlDelta = MaxDelta(b, bc); m.graphControlDelta = MaxDelta(c, cc);
                m.legacyControlAlpha = Compare(b, bc).maxAlpha; m.graphControlAlpha = Compare(c, cc).maxAlpha;
                m.legacyControlPixels = CountControlPixels(b, bc); m.graphControlPixels = CountControlPixels(c, cc);
                m.finite = AllFinite(b, br, c, cr, bc, bcr, cc, ccr);
                if (m.excludedInput)
                {
                    control.enabled = false; ApplyState(graph, legacy, map, control);
                    renderer.sharedMaterial = legacy;
                    Color[] bf = Capture(camera, target, readback, Path.Combine(output, "B-feature-off"));
                    Color[] bfr = Capture(camera, target, readback, Path.Combine(output, "B-feature-off-repeat"));
                    renderer.sharedMaterial = graph;
                    Color[] cf = Capture(camera, target, readback, Path.Combine(output, "C-feature-off"));
                    Color[] cfr = Capture(camera, target, readback, Path.Combine(output, "C-feature-off-repeat"));
                    Metrics mf = Compare(bf, cf);
                    m.controlDifferingRGB += mf.differingRGB; m.controlDifferingAlpha += mf.differingAlpha;
                    m.controlMaxRGB = Mathf.Max(m.controlMaxRGB, mf.maxRGB); m.controlMaxAlpha = Mathf.Max(m.controlMaxAlpha, mf.maxAlpha);
                    m.legacyFeatureDelta = MaxDelta(bc, bf); m.graphFeatureDelta = MaxDelta(cc, cf);
                    m.legacyFeaturePixels = CountControlPixels(bc, bf); m.graphFeaturePixels = CountControlPixels(cc, cf);
                    m.legacyFeatureRepeat = MaxDelta(bf, bfr); m.graphFeatureRepeat = MaxDelta(cf, cfr);
                    m.finite &= AllFinite(bf, bfr, cf, cfr);
                    // Do not alter the recorded counterfactual state when making the extra feature-off capture.
                    control.enabled = true;
                }
                File.WriteAllText(Path.Combine(output, "metrics.json"), JsonUtility.ToJson(m, true));
                Debug.Log("NBFX_G4_MATCAP_BC " + JsonUtility.ToJson(m));
                Assert.That(m.finite, Is.True, "Nonfinite raw output.");
                Assert.That(m.legacyVisible, Is.GreaterThan(100)); Assert.That(m.graphVisible, Is.GreaterThan(100));
                Assert.That(m.legacyRepeat, Is.Zero); Assert.That(m.graphRepeat, Is.Zero);
                Assert.That(m.legacyControlRepeat, Is.Zero); Assert.That(m.graphControlRepeat, Is.Zero);
                Assert.That(m.legacyControlAlpha, Is.Zero, "MatCap must not change source Alpha.");
                Assert.That(m.graphControlAlpha, Is.Zero, "Graph MatCap changed Alpha.");
                if (m.excludedInput)
                {
                    Assert.That(m.legacyControlDelta, Is.Zero, "ShaderLab MatCap must ignore ST/wrap/texture sampler.");
                    Assert.That(m.graphControlDelta, Is.Zero, "Graph MatCap invented ignored-input behavior.");
                    Assert.That(m.legacyFeatureDelta, Is.GreaterThan(ControlThreshold)); Assert.That(m.graphFeatureDelta, Is.GreaterThan(ControlThreshold));
                    Assert.That(m.legacyFeaturePixels, Is.GreaterThanOrEqualTo(64)); Assert.That(m.graphFeaturePixels, Is.GreaterThanOrEqualTo(64));
                    Assert.That(m.legacyFeatureRepeat, Is.Zero); Assert.That(m.graphFeatureRepeat, Is.Zero);
                }
                else
                {
                    Assert.That(m.legacyControlDelta, Is.GreaterThan(ControlThreshold), "ShaderLab MatCap counterfactual had no measurable effect.");
                    Assert.That(m.graphControlDelta, Is.GreaterThan(ControlThreshold), "Graph MatCap counterfactual had no measurable effect.");
                    Assert.That(m.legacyControlPixels, Is.GreaterThanOrEqualTo(64)); Assert.That(m.graphControlPixels, Is.GreaterThanOrEqualTo(64));
                }
                Assert.That(m.controlDifferingRGB, Is.Zero, "Strict counterfactual RGB B/C mismatch; raw evidence retained.");
                Assert.That(m.controlDifferingAlpha, Is.Zero, "Strict counterfactual Alpha B/C mismatch.");
                Assert.That(m.differingAlpha, Is.Zero, "Strict primary Alpha B/C mismatch.");
                Assert.That(m.differingRGB, Is.Zero, "Strict primary RGB B/C mismatch; do not tune tolerance for microdiff.");
            }
            finally
            {
                camera.targetTexture = null; RenderTexture.active = previous; target.Release();
                foreach (var obj in new UnityEngine.Object[] { graph, legacy, map, baseMap, emission, target, readback })
                    UnityEngine.Object.DestroyImmediate(obj);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        static State MakeState(string name, bool control)
        {
            var s = new State();
            switch (name)
            {
                case "mix-add": s.multiplyBlend = control ? 1 : 0; break;
                case "mix-mid": s.multiplyBlend = control ? 0 : .5f; break;
                case "mix-multiply": s.multiplyBlend = control ? 0 : 1; break;
                case "alpha-zero": s.color.a = control ? 1 : 0; break;
                case "alpha-half": s.color.a = control ? 0 : .5f; break;
                case "alpha-one": s.color.a = control ? 0 : 1; break;
                case "hdr-tint": s.color = control ? Color.white : new Color(2, 0, .5f, 1); break;
                case "disabled": s.enabled = control; break;
                case "emission-order": s.multiplyBlend = 0; s.enabled = !control; break;
                case "lod-auto": s.forceLod0 = control; break;
                case "lod-force": s.forceLod0 = !control; break;
                case "st-ignored": s.textureST = control ? new Vector4(1, 1, 0, 0) : new Vector4(7, .25f, -.75f, .375f); break;
                case "wrap-flags-ignored": s.wrapFlags = control ? 0u : uint.MaxValue; break;
                case "sampler-ignored": s.pointRepeatSampler = !control; break;
                default: s.enabled = !control; break;
            }
            return s;
        }

        static void Configure(Material m, bool graph, string name, Texture2D baseMap, Texture2D emission)
        {
            foreach (string property in new[] { "_MatCapToggle", "_MatCapTex", "_MatCapColor", "_MatCapInfo" })
                Assert.That(m.HasProperty(property), Is.True, "M0 property missing: " + property);
            m.shaderKeywords = graph ? new[] { "_SURFACE_TYPE_TRANSPARENT" } : new[] { "_FX_LIGHT_MODE_UNLIT" };
            m.SetTexture("_BaseMap", baseMap); m.SetTextureScale("_BaseMap", Vector2.one); m.SetTextureOffset("_BaseMap", Vector2.zero);
            m.SetColor(graph ? "_Color" : "_BaseColor", Color.white); m.SetColor("_ColorA", Color.white);
            m.SetFloat("_BaseColorIntensityForTimeline", 1); m.SetFloat("_AlphaAll", 1);
            m.SetFloat("_Cull", (float)CullMode.Off); m.SetFloat("_ZTest", (float)CompareFunction.LessEqual); m.SetFloat("_ZWrite", 0);
            m.SetFloat("_SrcBlend", (float)BlendMode.One); m.SetFloat("_DstBlend", (float)BlendMode.Zero);
            m.SetFloat("_BaseMapUVRotation", 0); m.SetVector("_BaseMapMaskMapOffset", Vector4.zero);
            if (!graph) { m.SetFloat("_BumpMapToggle", 0); m.SetFloat("_FxLightMode", 0); }
            uint flags0 = 0, flags1 = 1u | (1u << 9);
            if (name.EndsWith("adjustment-order", StringComparison.Ordinal))
            {
                flags0 |= (1u << 19) | 1u; flags1 |= (1u << 24) | (1u << 27);
                if (name == "early-adjustment-order") flags0 |= 1u << 22;
                m.SetFloat("_HueShift", .25f); m.SetFloat("_Saturability", .5f); m.SetFloat("_Contrast", .5f);
                m.SetColor("_ContrastMidColor", Color.white); m.SetVector("_BaseMapColorRefine", new Vector4(1, 1, 1, 0));
            }
            if (name == "emission-order")
            {
                m.SetFloat("_EmissionEnabled", 1); if (!graph) m.EnableKeyword("_EMISSION");
                m.SetTexture("_EmissionMap", emission); m.SetTextureScale("_EmissionMap", Vector2.one); m.SetTextureOffset("_EmissionMap", Vector2.zero);
                m.SetVector("_EmissionMapUVOffset", Vector4.zero); m.SetFloat("_EmissionMapUVRotation", 0);
                m.SetColor("_EmissionMapColor", Color.white); m.SetFloat("_EmissionMapColorIntensity", 1); m.SetFloat("_EmissionAlphaIntensity", 0);
                flags0 |= 1u << 5; // Additive MatCap followed by multiply overlay does not commute; tests the exact stage.
            }
            if (graph)
            {
                m.SetFloat("_Surface", 1); m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One); m.SetFloat("_DstBlendAlpha", (float)BlendMode.Zero);
                m.SetFloat("_NB_DistortionMode", 0); m.SetFloat("_NB_ColorChannelLo16", 3);
                SetWord(m, "_NB_Flags0Lo16", "_NB_Flags0Hi16", flags0); SetWord(m, "_NB_Flags1Lo16", "_NB_Flags1Hi16", flags1);
            }
            else
            {
                m.SetFloat("_ColorMask", 15); m.SetFloat("_fogintensity", 0);
                m.SetInteger("_W9ParticleShaderFlags", unchecked((int)flags0)); m.SetInteger("_W9ParticleShaderFlags1", unchecked((int)flags1));
                m.SetInteger("_W9ParticleShaderColorChannelFlag", 3);
                foreach (string property in new[] { "_W9ParticleCustomDataFlag0", "_W9ParticleCustomDataFlag1", "_W9ParticleCustomDataFlag2", "_W9ParticleCustomDataFlag3" }) m.SetInteger(property, 0);
                m.SetShaderPassEnabled("SRPDefaultUnlit", false); m.SetShaderPassEnabled("SRPDEFAULTUNLIT", false); m.SetShaderPassEnabled("UniversalForward", true);
            }
            foreach (string pass in new[] { "DepthOnly", "ShadowCaster", "NBCameraOpaqueDistortPass", "NBDeferredDistortPass", "Universal2D" }) m.SetShaderPassEnabled(pass, false);
            m.renderQueue = 3000;
        }

        static void ApplyState(Material graph, Material legacy, Texture2D texture, State s)
        {
            texture.filterMode = s.pointRepeatSampler ? FilterMode.Point : FilterMode.Bilinear;
            texture.wrapMode = s.pointRepeatSampler ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            foreach (Material m in new[] { legacy, graph })
            {
                m.SetFloat("_MatCapToggle", s.enabled ? 1 : 0); m.SetTexture("_MatCapTex", texture);
                m.SetColor("_MatCapColor", s.color); m.SetVector("_MatCapInfo", new Vector4(s.multiplyBlend, 0, 0, 0));
                m.SetTextureScale("_MatCapTex", new Vector2(s.textureST.x, s.textureST.y)); m.SetTextureOffset("_MatCapTex", new Vector2(s.textureST.z, s.textureST.w));
            }
            if (s.enabled) legacy.EnableKeyword("_MATCAP"); else legacy.DisableKeyword("_MATCAP");
            uint noMip = (1u << 0) | (1u << 6); if (s.forceLod0) noMip |= 1u << 5;
            SetWord(graph, "_NB_ForceNoMipFlagsLo16", "_NB_ForceNoMipFlagsHi16", noMip); legacy.SetInteger("_NBShaderForceNoMipFlags", unchecked((int)noMip));
            SetWord(graph, "_NB_WrapFlagsLo16", "_NB_WrapFlagsHi16", s.wrapFlags); legacy.SetInteger("_W9ParticleShaderWrapFlags", unchecked((int)s.wrapFlags));
        }
        static void SetWord(Material m, string lo, string hi, uint word)
        { m.SetFloat(lo, word & 65535u); m.SetFloat(hi, word >> 16); }

        static Texture2D MakeConstantTexture(Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBAHalf, false, true);
            texture.SetPixel(0, 0, color); texture.Apply(false); texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Repeat;
            return texture;
        }
        static Texture2D MakeMatCapTexture(bool lod)
        {
            int n = lod ? 1024 : 32;
            var texture = new Texture2D(n, n, TextureFormat.RGBAHalf, lod, true);
            var pixels = new Color[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                pixels[y * n + x] = lod ? (((x / 8 + y / 8) & 1) == 0 ? new Color(.125f, .75f, .25f, 1) : new Color(.5f, .25f, .875f, 1))
                    : new Color(.125f + .75f * x / (n - 1f), .125f + .75f * y / (n - 1f), .25f + .5f * x / (n - 1f), 1);
            texture.SetPixels(pixels); texture.Apply(lod);
            if (lod)
            {
                for (int mip = 1; mip < texture.mipmapCount; mip++)
                {
                    int side = Mathf.Max(1, n >> mip); var mipPixels = new Color[side * side];
                    for (int i = 0; i < mipPixels.Length; i++) mipPixels[i] = new Color(.9375f, .0625f, .125f, 1);
                    texture.SetPixels(mipPixels, mip);
                }
                texture.Apply(false); // Retain deliberate manual mips rather than regenerate.
            }
            texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Repeat;
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
                if (a[i].maxColorComponent > .01f) m.legacyVisible++; if (b[i].maxColorComponent > .01f) m.graphVisible++;
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
