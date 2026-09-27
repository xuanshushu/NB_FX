using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace NBFX.Baseline.Tests
{
    /// <summary>
    /// P4 only: current NBShader versus G0 Frozen through the existing NBPostProcess
    /// CameraOpaque renderer list. No renderer feature or project asset is edited.
    /// </summary>
    public sealed class G2CameraOpaqueABTests
    {
        private const string CurrentPath = "Assets/NBShaderSamples/NBShaderSamples/UnLit.mat";
        private const string FrozenPath = "Assets/NBShaderSamples/NBShader_VFXGraphValidation/Materials/UnLit.mat";
        private const string PassName = "NBCameraOpaqueDistortPass";
        private const string OpaqueViewPath = "Packages/com.xuanxuan.nb.fx/Tests/URP/Editor/G2MaskView.shader";
        private const int Size = 128;
        private const int Layer = 2;

        [Test]
        public void ExistingCameraOpaquePassMatchesFrozenAndHasVisibleControls()
        {
            var currentSource = AssetDatabase.LoadAssetAtPath<Material>(CurrentPath);
            var frozenSource = AssetDatabase.LoadAssetAtPath<Material>(FrozenPath);
            Assert.That(currentSource, Is.Not.Null);
            Assert.That(frozenSource, Is.Not.Null);
            Assert.That(currentSource.shader.name, Is.EqualTo("Effects/NBShader"));
            Assert.That(frozenSource.shader.name, Is.EqualTo("Effects/NBShader_T00_Frozen"));

            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            Assert.That(pipeline, Is.Not.Null, "P4 requires the active URP asset.");
            Assert.That(GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>()
                .enableRenderCompatibilityMode, Is.False, "This fixture measures RenderGraph only.");
            // The current active URP asset uses renderer index 0; record its identity separately.
            Assert.That(pipeline.rendererDataList.Length, Is.GreaterThan(0));
            var rendererData = pipeline.rendererDataList[0];
            Assert.That(rendererData, Is.Not.Null);
            ScriptableRendererFeature nbFeature = null;
            foreach (var feature in rendererData.rendererFeatures)
            {
                if (feature != null && feature.GetType().FullName == "NBShader.NBPostProcess")
                {
                    nbFeature = feature;
                    break;
                }
            }
            Assert.That(nbFeature, Is.Not.Null, "Use the project's original NBPostProcess feature, not a test replacement.");
            Assert.That(nbFeature.isActive, Is.True);
            Assert.That(Shader.Find("XuanXuan/ColorBlit"), Is.Not.Null);
            Assert.That(Shader.Find("XuanXuan/Postprocess/NBPostProcessUber"), Is.Not.Null);

            var current = new Material(currentSource);
            var frozen = new Material(frozenSource);
            var backgroundShader = Shader.Find("Universal Render Pipeline/Unlit");
            Assert.That(backgroundShader, Is.Not.Null);
            var backgroundMaterial = new Material(backgroundShader);
            var backgroundTexture = new Texture2D(Size, Size, TextureFormat.RGBA32, false, true);
            var noiseTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            var background = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var foreground = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var cameraObject = new GameObject("NBFX_G2_P4_Camera");
            var camera = cameraObject.AddComponent<Camera>();
            var cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
            var previousActive = RenderTexture.active;
            Material postMaterial = null;
            Material opaqueViewMaterial = null;
            G2OpaqueViewFeature opaqueViewFeature = null;
            int previousPostFlags = 0;
            try
            {
                ConfigureTextures(backgroundTexture, noiseTexture);
                ConfigureBackground(backgroundMaterial, backgroundTexture);
                ConfigureForeground(current, noiseTexture);
                ConfigureForeground(frozen, noiseTexture);
                VerifyExactPass(current);
                VerifyExactPass(frozen);

                background.layer = Layer;
                background.transform.position = new Vector3(0, 0, 1);
                background.transform.localScale = new Vector3(6, 6, 1);
                background.GetComponent<MeshRenderer>().sharedMaterial = backgroundMaterial;

                foreground.layer = Layer;
                foreground.transform.position = new Vector3(0, 0, 2);
                foreground.transform.localScale = new Vector3(2.4f, 2.4f, 1);
                var foregroundRenderer = foreground.GetComponent<MeshRenderer>();

                camera.orthographic = true;
                camera.orthographicSize = 2;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 20;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.allowHDR = false;
                camera.allowMSAA = false;
                camera.cullingMask = 1 << Layer;
                camera.transform.position = new Vector3(0, 0, 8);
                camera.transform.rotation = Quaternion.Euler(0, 180, 0);
                camera.targetTexture = target;
                cameraData.requiresColorTexture = true; // The pass samples _CameraOpaqueTexture.
                cameraData.renderPostProcessing = false; // NBPostProcess is a renderer feature, not URP post-processing.
                target.filterMode = FilterMode.Point;
                target.Create();

                // The renderer may initialize its runtime Uber material on its first frame.
                foregroundRenderer.enabled = false;
                camera.Render();
                var output = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/NBFXG2");
                Directory.CreateDirectory(output);
                // Before touching any NBPostProcess flag, preserve the first background-only
                // camera result. This separates background/camera failure from flag override.
                var preFlagsWarmup = ReadCurrentTarget(target,
                    Path.Combine(output, "p4_background_pre_flags_warmup.png"));
                var field = nbFeature.GetType().GetField("NBPostProcessMaterial", BindingFlags.Public | BindingFlags.Static);
                Assert.That(field, Is.Not.Null, "The current NBPostProcess static material contract changed.");
                postMaterial = field.GetValue(null) as Material;
                Assert.That(postMaterial, Is.Not.Null, "NBPostProcess.Create did not create its runtime Uber material.");
                Assert.That(AssetDatabase.Contains(postMaterial), Is.False,
                    "Do not change a persistent NBPostProcess material during a test.");
                Assert.That(postMaterial.HasProperty("_NBPostProcessFlags"), Is.True);
                previousPostFlags = postMaterial.GetInteger("_NBPostProcessFlags");
                postMaterial.SetInteger("_NBPostProcessFlags", 0); // Integer ShaderLab property; restored below.

                var noFront = Capture(camera, target, foregroundRenderer, current, false, true, 0.14f,
                    Path.Combine(output, "p4_no_front.png"));
                var currentOff = Capture(camera, target, foregroundRenderer, current, true, false, 0.14f,
                    Path.Combine(output, "p4_current_pass_off.png"));
                var frozenOff = Capture(camera, target, foregroundRenderer, frozen, true, false, 0.14f,
                    Path.Combine(output, "p4_frozen_pass_off.png"));
                var currentZero = Capture(camera, target, foregroundRenderer, current, true, true, 0,
                    Path.Combine(output, "p4_current_zero.png"));
                var frozenZero = Capture(camera, target, foregroundRenderer, frozen, true, true, 0,
                    Path.Combine(output, "p4_frozen_zero.png"));
                var currentOn = Capture(camera, target, foregroundRenderer, current, true, true, 0.14f,
                    Path.Combine(output, "p4_current_on.png"));
                var frozenOn = Capture(camera, target, foregroundRenderer, frozen, true, true, 0.14f,
                    Path.Combine(output, "p4_frozen_on.png"));

                // Read the actual URP opaque copy in the same camera, not only
                // the final NBPostProcess result. The temporary feature is
                // removed before this test returns and never saved to the asset.
                var opaqueViewShader = AssetDatabase.LoadAssetAtPath<Shader>(OpaqueViewPath);
                Assert.That(opaqueViewShader, Is.Not.Null);
                Assert.That(opaqueViewShader.isSupported, Is.True);
                opaqueViewMaterial = new Material(opaqueViewShader);
                opaqueViewFeature = ScriptableObject.CreateInstance<G2OpaqueViewFeature>();
                opaqueViewFeature.material = opaqueViewMaterial;
                opaqueViewFeature.Create();
                opaqueViewFeature.SetActive(true);
                rendererData.rendererFeatures.Add(opaqueViewFeature);
                rendererData.SetDirty();
                var opaqueEmpty = Capture(camera, target, foregroundRenderer, current, false, true, 0.14f,
                    Path.Combine(output, "p4_opaque_copy_empty.png"));
                var opaqueOff = Capture(camera, target, foregroundRenderer, current, true, false, 0.14f,
                    Path.Combine(output, "p4_opaque_copy_off.png"));
                var opaqueCurrent = Capture(camera, target, foregroundRenderer, current, true, true, 0.14f,
                    Path.Combine(output, "p4_opaque_copy_current.png"));
                var opaqueFrozen = Capture(camera, target, foregroundRenderer, frozen, true, true, 0.14f,
                    Path.Combine(output, "p4_opaque_copy_frozen.png"));

                int offVersusEmpty = CountDifferent(noFront, currentOff, 0, false);
                int frozenOffVersusEmpty = CountDifferent(noFront, frozenOff, 0, false);
                int offAB = CountDifferent(currentOff, frozenOff, 0, false);
                int zeroAB = CountDifferent(currentZero, frozenZero, 0, false);
                int onAB = CountDifferent(currentOn, frozenOn, 0, false);
                int currentVisible = CountDifferent(currentOff, currentOn, 4, true);
                int frozenVisible = CountDifferent(frozenOff, frozenOn, 4, true);
                int currentZeroVisible = CountDifferent(currentOff, currentZero, 4, true);
                int frozenZeroVisible = CountDifferent(frozenOff, frozenZero, 4, true);
                int outsideChanged = CountOutsideCenter(currentOff, currentOn, 4);
                int opaqueOffEmpty = CountDifferent(opaqueEmpty, opaqueOff, 0, false);
                int opaqueCurrentEmpty = CountDifferent(opaqueEmpty, opaqueCurrent, 0, false);
                int opaqueAB = CountDifferent(opaqueCurrent, opaqueFrozen, 0, false);
                string backgroundStats = "preFlagsWarmup=" + DescribeBackground(preFlagsWarmup) +
                    ", noFrontAfterFlags=" + DescribeBackground(noFront);
                string metrics = "P4 RenderGraph 128x128 RGBA32, camera opaque copy required, original NBPostProcess active\n" +
                    backgroundStats + "\n" +
                    "noFront-vs-currentOff=" + offVersusEmpty + ", noFront-vs-frozenOff=" + frozenOffVersusEmpty +
                    ", offAB=" + offAB + ", zeroAB=" + zeroAB + ", onAB=" + onAB + "\n" +
                    "currentVisibleROI(>4)=" + currentVisible + ", frozenVisibleROI(>4)=" + frozenVisible +
                    ", currentZeroROI(>4)=" + currentZeroVisible + ", frozenZeroROI(>4)=" + frozenZeroVisible +
                    ", outsideROI(>4)=" + outsideChanged + "\n";
                metrics += "opaqueEmpty=" + DescribeBackground(opaqueEmpty) +
                    ", opaqueOffEmpty=" + opaqueOffEmpty +
                    ", opaqueCurrentEmpty=" + opaqueCurrentEmpty +
                    ", opaqueAB=" + opaqueAB + "\n";
                File.WriteAllText(Path.Combine(output, "p4_camera_opaque_metrics.txt"), metrics);
                Debug.Log("NBFX-G2-P4: " + metrics);

                Assert.That(CountNonBlack(noFront), Is.GreaterThan(1000),
                    "Background-only camera capture is black; inspect pre-flags warmup versus post-flags PNG/stats before interpreting Pass A/B.");
                Assert.That(CountDistinctRGB(noFront), Is.GreaterThan(32),
                    "Background-only gradient did not render; a zero-difference A/B would be vacuous.");
                Assert.That(offVersusEmpty, Is.Zero, "With every foreground Pass disabled, its object must be invisible.");
                Assert.That(frozenOffVersusEmpty, Is.Zero);
                Assert.That(offAB, Is.Zero);
                Assert.That(zeroAB, Is.Zero);
                Assert.That(onAB, Is.Zero, "Current CameraOpaque output differs from G0 Frozen.");
                Assert.That(currentZeroVisible, Is.Zero, "Zero strength must not visibly shift the screen.");
                Assert.That(frozenZeroVisible, Is.Zero);
                Assert.That(currentVisible, Is.GreaterThan(32), "The exact CameraOpaque Pass must visibly shift the controlled gradient.");
                Assert.That(frozenVisible, Is.GreaterThan(32), "Frozen positive control must also draw.");
                Assert.That(outsideChanged, Is.Zero, "Distortion escaped the foreground's central region.");
                Assert.That(CountNonBlack(opaqueEmpty), Is.GreaterThan(1000),
                    "_CameraOpaqueTexture must contain the actual background, not a black/default texture.");
                Assert.That(CountDistinctRGB(opaqueEmpty), Is.GreaterThan(32));
                Assert.That(opaqueOffEmpty, Is.Zero);
                Assert.That(opaqueCurrentEmpty, Is.Zero,
                    "The CameraOpaque distortion draw must not alter its earlier source copy.");
                Assert.That(opaqueAB, Is.Zero, "Current and Frozen opaque copies must match.");
            }
            finally
            {
                if (opaqueViewFeature != null)
                {
                    rendererData.rendererFeatures.Remove(opaqueViewFeature);
                    rendererData.SetDirty();
                }
                if (postMaterial != null)
                    postMaterial.SetInteger("_NBPostProcessFlags", previousPostFlags);
                camera.targetTexture = null;
                RenderTexture.active = previousActive;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(background);
                UnityEngine.Object.DestroyImmediate(foreground);
                UnityEngine.Object.DestroyImmediate(backgroundMaterial);
                UnityEngine.Object.DestroyImmediate(backgroundTexture);
                UnityEngine.Object.DestroyImmediate(noiseTexture);
                UnityEngine.Object.DestroyImmediate(current);
                UnityEngine.Object.DestroyImmediate(frozen);
                UnityEngine.Object.DestroyImmediate(opaqueViewFeature);
                UnityEngine.Object.DestroyImmediate(opaqueViewMaterial);
            }
        }

        private sealed class G2OpaqueViewFeature : ScriptableRendererFeature
        {
            public Material material;
            private G2OpaqueViewPass pass;

            public override void Create()
            {
                pass = new G2OpaqueViewPass(material)
                {
                    renderPassEvent = RenderPassEvent.AfterRenderingTransparents + 1
                };
            }

            public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
            {
                if (renderingData.cameraData.cameraType == CameraType.Game)
                    renderer.EnqueuePass(pass);
            }

            private sealed class G2OpaqueViewPass : ScriptableRenderPass
            {
                private static readonly int OpaqueId = Shader.PropertyToID("_CameraOpaqueTexture");
                private readonly Material material;
                private sealed class PassData { public Material material; }

                public G2OpaqueViewPass(Material material) { this.material = material; }

                public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
                {
                    var resources = frameData.Get<UniversalResourceData>();
                    if (!resources.activeColorTexture.IsValid()) return;
                    using (var builder = renderGraph.AddRasterRenderPass<PassData>("G2 Camera Opaque Copy View", out var data))
                    {
                        data.material = material;
                        builder.UseGlobalTexture(OpaqueId, AccessFlags.Read);
                        builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.ReadWrite);
                        builder.AllowPassCulling(false);
                        builder.SetRenderFunc(static (PassData d, RasterGraphContext context) =>
                            context.cmd.DrawProcedural(Matrix4x4.identity, d.material, 2, MeshTopology.Triangles, 3, 1));
                    }
                }
            }
        }

        private static void ConfigureTextures(Texture2D background, Texture2D noise)
        {
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                    pixels[y * Size + x] = new Color32((byte)(20 + x * 210 / (Size - 1)),
                        (byte)(20 + y * 210 / (Size - 1)), 64, 255);
            background.SetPixels32(pixels);
            background.Apply(false);
            background.filterMode = FilterMode.Point;
            background.wrapMode = TextureWrapMode.Clamp;
            noise.SetPixels(new[]
            {
                new Color(0.75f, 0.5f, 0, 1), new Color(0.75f, 0.5f, 0, 1),
                new Color(0.75f, 0.5f, 0, 1), new Color(0.75f, 0.5f, 0, 1)
            });
            noise.Apply(false);
            noise.filterMode = FilterMode.Point;
            noise.wrapMode = TextureWrapMode.Clamp;
        }

        private static void ConfigureBackground(Material material, Texture2D texture)
        {
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 0);
            material.SetFloat("_SrcBlend", (float)BlendMode.One);
            material.SetFloat("_DstBlend", (float)BlendMode.Zero);
            material.SetFloat("_ZWrite", 1);
            // Match G0's primitive-Quad control: do not let face winding hide the gradient.
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = 2000;
        }

        private static void ConfigureForeground(Material material, Texture2D noise)
        {
            material.DisableKeyword("_FRESNEL");
            material.DisableKeyword("_NORMALMAP");
            material.DisableKeyword("_PARCUSTOMDATA_ON");
            material.DisableKeyword("_SPECULAR_COLOR");
            material.EnableKeyword("_FX_LIGHT_MODE_UNLIT");
            material.EnableKeyword("_NOISEMAP");
            material.SetFloat("_noisemapEnabled", 1);
            material.SetTexture("_NoiseMap", noise);
            material.SetTextureScale("_NoiseMap", Vector2.one);
            material.SetTextureOffset("_NoiseMap", Vector2.zero);
            material.SetVector("_NoiseOffset", Vector4.zero);
            material.SetFloat("_NoiseIntensity", 1);
            material.SetVector("_DistortionDirection", new Vector4(1, 0, 0, 0));
            material.SetFloat("_ScreenDistortModeToggle", 2);
            material.SetFloat("_DisableMainPassToggle", 1);
            material.SetFloat("_ScreenDistortAlphaRefineToggle", 0);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_AlphaAll", 1);
            material.SetFloat("_Cull", 0);
            material.SetFloat("_ZWrite", 0);
            material.SetFloat("_ZTest", (float)CompareFunction.LessEqual);
            material.renderQueue = 3000;
            material.SetShaderPassEnabled("SRPDefaultUnlit", false);
            material.SetShaderPassEnabled("SRPDEFAULTUNLIT", false); // Serialized sample uses this casing.
            material.SetShaderPassEnabled("UniversalForward", false);
            material.SetShaderPassEnabled("DepthOnly", false);
            material.SetShaderPassEnabled("ShadowCaster", false);
            material.SetShaderPassEnabled("NBDeferredDistortPass", false);
            material.SetShaderPassEnabled("Universal2D", false);
            material.SetShaderPassEnabled(PassName, true);
            Assert.That(material.GetShaderPassEnabled("UniversalForward"), Is.False);
            Assert.That(material.GetShaderPassEnabled("SRPDefaultUnlit"), Is.False);
            Assert.That(material.GetShaderPassEnabled(PassName), Is.True);
        }

        private static void VerifyExactPass(Material material)
        {
            int index = material.FindPass(PassName);
            Assert.That(index, Is.GreaterThanOrEqualTo(0), material.shader.name + " lost " + PassName);
            var lightMode = material.shader.FindPassTagValue(0, index, new ShaderTagId("LightMode"));
            Assert.That(lightMode.name, Is.EqualTo(PassName), material.shader.name + " changed its LightMode.");
        }

        private static Color32[] Capture(Camera camera, RenderTexture target, MeshRenderer foreground,
            Material material, bool visible, bool passEnabled, float strength, string path)
        {
            foreground.sharedMaterial = material;
            foreground.enabled = visible;
            material.SetShaderPassEnabled(PassName, passEnabled);
            material.SetFloat("_ScreenDistortIntensity", strength);
            camera.Render();
            return ReadCurrentTarget(target, path);
        }

        private static Color32[] ReadCurrentTarget(RenderTexture target, string path)
        {
            var previous = RenderTexture.active;
            var readback = new Texture2D(Size, Size, TextureFormat.RGBA32, false, false);
            try
            {
                RenderTexture.active = target;
                readback.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
                readback.Apply(false);
                File.WriteAllBytes(path, readback.EncodeToPNG());
                return readback.GetPixels32();
            }
            finally
            {
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(readback);
            }
        }

        private static string DescribeBackground(Color32[] pixels)
        {
            var center = pixels[(Size / 2) * Size + Size / 2];
            return "centerRGBA=(" + center.r + "," + center.g + "," + center.b + "," + center.a +
                   "),nonBlackRGB=" + CountNonBlack(pixels) + ",distinctRGB=" + CountDistinctRGB(pixels);
        }

        private static int CountNonBlack(Color32[] pixels)
        {
            int count = 0;
            foreach (var p in pixels)
                if (p.r != 0 || p.g != 0 || p.b != 0) count++;
            return count;
        }

        private static int CountDistinctRGB(Color32[] pixels)
        {
            var colors = new HashSet<int>();
            foreach (var p in pixels)
                colors.Add((p.r << 16) | (p.g << 8) | p.b);
            return colors.Count;
        }

        private static int CountDifferent(Color32[] left, Color32[] right, int threshold, bool centerOnly)
        {
            Assert.That(right.Length, Is.EqualTo(left.Length));
            int changed = 0;
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    if (centerOnly && !IsCenter(x, y)) continue;
                    int i = y * Size + x;
                    if (MaximumChannelError(left[i], right[i]) > threshold) changed++;
                }
            }
            return changed;
        }

        private static int CountOutsideCenter(Color32[] left, Color32[] right, int threshold)
        {
            int changed = 0;
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    if (IsCenter(x, y)) continue;
                    int i = y * Size + x;
                    if (MaximumChannelError(left[i], right[i]) > threshold) changed++;
                }
            return changed;
        }

        // The 2.4-unit foreground spans the central 60% of a 4-unit orthographic view.
        private static bool IsCenter(int x, int y) => x >= Size / 5 && x < Size * 4 / 5 &&
                                                      y >= Size / 5 && y < Size * 4 / 5;

        private static int MaximumChannelError(Color32 a, Color32 b) =>
            Math.Max(Math.Max(Math.Abs(a.r - b.r), Math.Abs(a.g - b.g)),
                Math.Max(Math.Abs(a.b - b.b), Math.Abs(a.a - b.a)));
    }
}
