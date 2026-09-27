#if UNITY_EDITOR
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
    /// P5 PlayMode slice. The original NBPostProcess draws the exact
    /// NBDeferredDistortPass into its transient mask and composites it.
    /// A test-only in-memory Feature views the mask, then is removed.
    /// </summary>
    public sealed class G2DeferredDistortABTests
    {
        private const string CurrentPath = "Assets/NBShaderSamples/NBShaderSamples/UnLit.mat";
        private const string FrozenPath = "Assets/NBShaderSamples/NBShader_VFXGraphValidation/Materials/UnLit.mat";
        private const string PassName = "NBDeferredDistortPass";
        private const string MaskViewPath = "Packages/com.xuanxuan.nb.fx/Tests/URP/Editor/G2MaskView.shader";
        private const int Size = 128;
        private const int Layer = 2;

        [Test]
        public void ExistingDeferredPassCompositesLikeFrozenWithVisibleControls()
        {
            var currentSource = AssetDatabase.LoadAssetAtPath<Material>(CurrentPath);
            var frozenSource = AssetDatabase.LoadAssetAtPath<Material>(FrozenPath);
            Assert.That(currentSource, Is.Not.Null);
            Assert.That(frozenSource, Is.Not.Null);
            Assert.That(currentSource.shader.name, Is.EqualTo("Effects/NBShader"));
            Assert.That(frozenSource.shader.name, Is.EqualTo("Effects/NBShader_T00_Frozen"));

            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            Assert.That(pipeline, Is.Not.Null, "P5 requires the active URP asset.");
            Assert.That(GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>()
                .enableRenderCompatibilityMode, Is.False, "P5 measures RenderGraph, not Compatibility.");
            Assert.That(pipeline.rendererDataList.Length, Is.GreaterThan(0));
            var rendererData = pipeline.rendererDataList[0]; // Current active URP asset uses renderer 0.
            Assert.That(rendererData, Is.Not.Null);
            ScriptableRendererFeature nbFeature = null;
            foreach (var feature in rendererData.rendererFeatures)
                if (feature != null && feature.GetType().FullName == "NBShader.NBPostProcess")
                {
                    nbFeature = feature;
                    break;
                }
            Assert.That(nbFeature, Is.Not.Null, "Must use the original NBPostProcess RendererFeature.");
            Assert.That(nbFeature.isActive, Is.True);
            Assert.That(Shader.Find("XuanXuan/ColorBlit"), Is.Not.Null);
            Assert.That(Shader.Find("XuanXuan/Postprocess/NBPostProcessUber"), Is.Not.Null);
            var downsamplingField = nbFeature.GetType().GetField("downSampling", BindingFlags.Public | BindingFlags.Instance);
            Assert.That(downsamplingField, Is.Not.Null);
            string downsampling = downsamplingField.GetValue(nbFeature).ToString();

            var current = new Material(currentSource);
            var frozen = new Material(frozenSource);
            var backgroundShader = Shader.Find("Universal Render Pipeline/Unlit");
            Assert.That(backgroundShader, Is.Not.Null);
            var backgroundMaterial = new Material(backgroundShader);
            var backgroundTexture = new Texture2D(Size, Size, TextureFormat.RGBA32, false, true);
            var noiseTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            var background = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var foreground = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var cameraObject = new GameObject("NBFX_G2_P5_Camera");
            var camera = cameraObject.AddComponent<Camera>();
            var cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
            var previousActive = RenderTexture.active;
            Material postMaterial = null;
            Material maskViewMaterial = null;
            G2MaskViewFeature maskViewFeature = null;
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
                cameraData.renderPostProcessing = false; // NBPostProcess is a renderer feature, not URP post-processing.
                target.filterMode = FilterMode.Point;
                target.Create();

                var output = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/NBFXG2");
                Directory.CreateDirectory(output);
                foregroundRenderer.enabled = false;
                camera.Render(); // Also initializes the runtime Uber material if needed.
                var preFlags = ReadCurrentTarget(target, Path.Combine(output, "p5_background_pre_flags.png"));
                var field = nbFeature.GetType().GetField("NBPostProcessMaterial", BindingFlags.Public | BindingFlags.Static);
                Assert.That(field, Is.Not.Null);
                postMaterial = field.GetValue(null) as Material;
                Assert.That(postMaterial, Is.Not.Null);
                Assert.That(AssetDatabase.Contains(postMaterial), Is.False,
                    "Do not alter a persistent NBPostProcess material.");
                Assert.That(postMaterial.HasProperty("_NBPostProcessFlags"), Is.True);
                previousPostFlags = postMaterial.GetInteger("_NBPostProcessFlags");
                postMaterial.SetInteger("_NBPostProcessFlags", 0); // Only the in-memory runtime material; restored below.

                var noFront = Capture(camera, target, foregroundRenderer, current, false, true, 0.14f,
                    Path.Combine(output, "p5_no_front.png"));
                var currentOff = Capture(camera, target, foregroundRenderer, current, true, false, 0.14f,
                    Path.Combine(output, "p5_current_pass_off.png"));
                var frozenOff = Capture(camera, target, foregroundRenderer, frozen, true, false, 0.14f,
                    Path.Combine(output, "p5_frozen_pass_off.png"));
                var currentZero = Capture(camera, target, foregroundRenderer, current, true, true, 0,
                    Path.Combine(output, "p5_current_zero.png"));
                var frozenZero = Capture(camera, target, foregroundRenderer, frozen, true, true, 0,
                    Path.Combine(output, "p5_frozen_zero.png"));
                var currentOn = Capture(camera, target, foregroundRenderer, current, true, true, 0.14f,
                    Path.Combine(output, "p5_current_on.png"));
                var frozenOn = Capture(camera, target, foregroundRenderer, frozen, true, true, 0.14f,
                    Path.Combine(output, "p5_frozen_on.png"));
                int flagsAfterFinalCaptures = postMaterial.GetInteger("_NBPostProcessFlags");
                postMaterial.SetInteger("_NBPostProcessFlags", 8); // Temporary FLASH control proves Uber execution.
                var uberFlash = Capture(camera, target, foregroundRenderer, current, false, true, 0.14f,
                    Path.Combine(output, "p5_uber_flash_control.png"));
                int flagsAfterFlashCapture = postMaterial.GetInteger("_NBPostProcessFlags");
                postMaterial.SetInteger("_NBPostProcessFlags", 0);

                // A final-color A/B can be vacuous when both sides produce a
                // zero mask. Read the transient global during RenderGraph with
                // an in-memory test-only Feature, then remove it in finally.
                var maskViewShader = AssetDatabase.LoadAssetAtPath<Shader>(MaskViewPath);
                Assert.That(maskViewShader, Is.Not.Null);
                Assert.That(maskViewShader.isSupported, Is.True);
                maskViewMaterial = new Material(maskViewShader);
                maskViewFeature = ScriptableObject.CreateInstance<G2MaskViewFeature>();
                maskViewFeature.material = maskViewMaterial;
                maskViewFeature.Create();
                maskViewFeature.SetActive(true);
                rendererData.rendererFeatures.Add(maskViewFeature);
                rendererData.SetDirty();
                var maskEmpty = Capture(camera, target, foregroundRenderer, current, false, true, 0.14f,
                    Path.Combine(output, "p5_mask_empty.png"));
                var maskOff = Capture(camera, target, foregroundRenderer, current, true, false, 0.14f,
                    Path.Combine(output, "p5_mask_off.png"));
                var maskZero = Capture(camera, target, foregroundRenderer, current, true, true, 0,
                    Path.Combine(output, "p5_mask_zero.png"));
                var maskCurrent = Capture(camera, target, foregroundRenderer, current, true, true, 0.14f,
                    Path.Combine(output, "p5_mask_current.png"));
                var maskFrozen = Capture(camera, target, foregroundRenderer, frozen, true, true, 0.14f,
                    Path.Combine(output, "p5_mask_frozen.png"));
                maskViewFeature.SetViewCopy();
                var copyEmpty = Capture(camera, target, foregroundRenderer, current, false, true, 0.14f,
                    Path.Combine(output, "p5_copy_empty.png"));
                var copyCurrent = Capture(camera, target, foregroundRenderer, current, true, true, 0.14f,
                    Path.Combine(output, "p5_copy_current.png"));
                var copyFrozen = Capture(camera, target, foregroundRenderer, frozen, true, true, 0.14f,
                    Path.Combine(output, "p5_copy_frozen.png"));

                int noFrontOff = CountDifferent(noFront, currentOff, 0, false);
                int frozenNoFrontOff = CountDifferent(noFront, frozenOff, 0, false);
                int offAB = CountDifferent(currentOff, frozenOff, 0, false);
                int zeroAB = CountDifferent(currentZero, frozenZero, 0, false);
                int onAB = CountDifferent(currentOn, frozenOn, 0, false);
                int zeroCurrentROI = CountDifferent(currentOff, currentZero, 4, true);
                int zeroFrozenROI = CountDifferent(frozenOff, frozenZero, 4, true);
                int onCurrentROI = CountDifferent(currentOff, currentOn, 4, true);
                int onFrozenROI = CountDifferent(frozenOff, frozenOn, 4, true);
                int outsideROI = CountOutsideCenter(currentOff, currentOn, 4);
                int onCurrentExactROI = CountDifferent(currentOff, currentOn, 0, true);
                int uberFlashVisible = CountDifferent(noFront, uberFlash, 4, false);
                int maskOffEmpty = CountDifferent(maskEmpty, maskOff, 0, false);
                int maskZeroEmpty = CountDifferent(maskEmpty, maskZero, 0, false);
                int maskCurrentVisible = CountDifferent(maskEmpty, maskCurrent, 4, true);
                int maskFrozenVisible = CountDifferent(maskEmpty, maskFrozen, 4, true);
                int maskAB = CountDifferent(maskCurrent, maskFrozen, 0, false);
                int copyCurrentEmpty = CountDifferent(copyEmpty, copyCurrent, 0, false);
                int copyAB = CountDifferent(copyCurrent, copyFrozen, 0, false);
                string metrics = "P5 RenderGraph 128x128 RGBA32; original NBPostProcess; feature downSampling=" + downsampling + "\n" +
                    "preFlags=" + DescribeBackground(preFlags) + ", noFront=" + DescribeBackground(noFront) + "\n" +
                    "noFrontOff=" + noFrontOff + ", frozenNoFrontOff=" + frozenNoFrontOff +
                    ", offAB=" + offAB + ", zeroAB=" + zeroAB + ", onAB=" + onAB + "\n" +
                    "zeroCurrentROI(>4)=" + zeroCurrentROI + ", zeroFrozenROI(>4)=" + zeroFrozenROI +
                    ", onCurrentROI(>4)=" + onCurrentROI + ", onFrozenROI(>4)=" + onFrozenROI +
                    ", outsideROI(>4)=" + outsideROI + "\n" +
                    "finalCurrentExactROI=" + onCurrentExactROI +
                    ", flagsAfterFinalCaptures=" + flagsAfterFinalCaptures +
                    ", flagsAfterFlashCapture=" + flagsAfterFlashCapture +
                    ", uberFlashVisible(>4)=" + uberFlashVisible + "\n" +
                    "maskOffEmpty=" + maskOffEmpty + ", maskZeroEmpty=" + maskZeroEmpty +
                    ", maskCurrentROI(>4)=" + maskCurrentVisible +
                    ", maskFrozenROI(>4)=" + maskFrozenVisible + ", maskAB=" + maskAB + "\n" +
                    "copyEmpty=" + DescribeBackground(copyEmpty) +
                    ", copyCurrentEmpty=" + copyCurrentEmpty + ", copyAB=" + copyAB + "\n";
                File.WriteAllText(Path.Combine(output, "p5_deferred_metrics.txt"), metrics);
                Debug.Log("NBFX-G2-P5: " + metrics);

                Assert.That(CountNonBlack(noFront), Is.GreaterThan(1000), "Background must render before P5 A/B is meaningful.");
                Assert.That(CountDistinctRGB(noFront), Is.GreaterThan(32));
                Assert.That(noFrontOff, Is.Zero, "No foreground Pass should draw in the pass-off control.");
                Assert.That(frozenNoFrontOff, Is.Zero);
                Assert.That(offAB, Is.Zero);
                Assert.That(zeroAB, Is.Zero);
                Assert.That(onAB, Is.Zero, "Current deferred final output differs from G0 Frozen.");
                Assert.That(maskOffEmpty, Is.Zero);
                Assert.That(maskZeroEmpty, Is.Zero);
                Assert.That(maskAB, Is.Zero, "Current deferred mask differs from G0 Frozen.");
                Assert.That(maskCurrentVisible, Is.GreaterThan(32), "Exact Deferred Pass must draw a nonzero transient mask.");
                Assert.That(maskFrozenVisible, Is.GreaterThan(32));
                Assert.That(CountNonBlack(copyEmpty), Is.GreaterThan(1000), "ScreenColorCopy1 must contain a visible background.");
                Assert.That(CountDistinctRGB(copyEmpty), Is.GreaterThan(32), "ScreenColorCopy1 must contain a gradient, not a default texture.");
                Assert.That(copyCurrentEmpty, Is.Zero, "Deferred Mask draw must not alter the earlier screen-color copy.");
                Assert.That(copyAB, Is.Zero, "Current/frozen screen-color copies must match.");
                Assert.That(uberFlashVisible, Is.GreaterThan(32), "The original NBPostProcess Uber must execute on this camera.");
                Assert.That(zeroCurrentROI, Is.Zero, "Zero distortion strength must not visibly move the background.");
                Assert.That(zeroFrozenROI, Is.Zero);
                Assert.That(onCurrentROI, Is.GreaterThan(32), "Original deferred Pass + Mask + Uber must visibly shift the gradient.");
                Assert.That(onFrozenROI, Is.GreaterThan(32), "Frozen positive control must also render.");
                Assert.That(outsideROI, Is.Zero, "Deferred distortion escaped its foreground neighborhood.");
            }
            finally
            {
                if (maskViewFeature != null)
                {
                    rendererData.rendererFeatures.Remove(maskViewFeature);
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
                UnityEngine.Object.DestroyImmediate(maskViewFeature);
                UnityEngine.Object.DestroyImmediate(maskViewMaterial);
            }
        }

        private sealed class G2MaskViewFeature : ScriptableRendererFeature
        {
            public Material material;
            private G2MaskViewPass pass;

            public void SetViewCopy() { pass.SetViewCopy(); }

            public override void Create()
            {
                pass = new G2MaskViewPass(material);
                pass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents + 1;
            }

            public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
            {
                if (renderingData.cameraData.cameraType == CameraType.Game)
                    renderer.EnqueuePass(pass);
            }

            private sealed class G2MaskViewPass : ScriptableRenderPass
            {
                private static readonly int MaskId = Shader.PropertyToID("_DisturbanceMaskTex");
                private static readonly int CopyId = Shader.PropertyToID("_ScreenColorCopy1");
                private readonly Material material;
                private bool viewCopy;
                private sealed class PassData { public Material material; public int materialPass; }

                public G2MaskViewPass(Material material) { this.material = material; }
                public void SetViewCopy() { viewCopy = true; }

                public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
                {
                    var resources = frameData.Get<UniversalResourceData>();
                    if (!resources.activeColorTexture.IsValid()) return;
                    using (var builder = renderGraph.AddRasterRenderPass<PassData>("G2 Mask View", out var data))
                    {
                        data.material = material;
                        data.materialPass = viewCopy ? 1 : 0;
                        builder.UseGlobalTexture(viewCopy ? CopyId : MaskId, AccessFlags.Read);
                        builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.ReadWrite);
                        builder.AllowPassCulling(false);
                        builder.SetRenderFunc(static (PassData d, RasterGraphContext context) =>
                            context.cmd.DrawProcedural(Matrix4x4.identity, d.material, d.materialPass, MeshTopology.Triangles, 3, 1));
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
            material.SetFloat("_Cull", (float)CullMode.Off); // Primitive Quad winding must not hide the background.
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
            material.SetFloat("_ScreenDistortModeToggle", 1);
            material.SetFloat("_DisableMainPassToggle", 1);
            material.SetFloat("_ScreenDistortAlphaRefineToggle", 0);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_AlphaAll", 1);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.SetFloat("_ZWrite", 0);
            material.SetFloat("_ZTest", (float)CompareFunction.LessEqual);
            material.renderQueue = 3000;
            material.SetShaderPassEnabled("SRPDefaultUnlit", false);
            material.SetShaderPassEnabled("SRPDEFAULTUNLIT", false);
            material.SetShaderPassEnabled("UniversalForward", false);
            material.SetShaderPassEnabled("DepthOnly", false);
            material.SetShaderPassEnabled("ShadowCaster", false);
            material.SetShaderPassEnabled("NBCameraOpaqueDistortPass", false);
            material.SetShaderPassEnabled("Universal2D", false);
            material.SetShaderPassEnabled(PassName, true);
            Assert.That(material.GetShaderPassEnabled("UniversalForward"), Is.False);
            Assert.That(material.GetShaderPassEnabled(PassName), Is.True);
        }

        private static void VerifyExactPass(Material material)
        {
            int index = material.FindPass(PassName);
            Assert.That(index, Is.GreaterThanOrEqualTo(0), material.shader.name + " lost " + PassName);
            Assert.That(material.shader.FindPassTagValue(0, index, new ShaderTagId("LightMode")).name,
                Is.EqualTo(PassName), material.shader.name + " changed its LightMode.");
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

        // Wider than the Quad's 60% footprint to include 2x-bilinear mask edge pixels.
        private static bool IsCenter(int x, int y) => x >= Size / 8 && x < Size * 7 / 8 &&
                                                      y >= Size / 8 && y < Size * 7 / 8;

        private static int CountDifferent(Color32[] left, Color32[] right, int threshold, bool centerOnly)
        {
            Assert.That(right.Length, Is.EqualTo(left.Length));
            int changed = 0;
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    if (centerOnly && !IsCenter(x, y)) continue;
                    int i = y * Size + x;
                    if (MaximumChannelError(left[i], right[i]) > threshold) changed++;
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

        private static int MaximumChannelError(Color32 a, Color32 b) =>
            Math.Max(Math.Max(Math.Abs(a.r - b.r), Math.Abs(a.g - b.g)),
                Math.Max(Math.Abs(a.b - b.b), Math.Abs(a.a - b.a)));
    }
}
#endif
