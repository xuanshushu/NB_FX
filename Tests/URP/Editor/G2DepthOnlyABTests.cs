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
    /// G2/P2: exact DepthOnly RendererList writes the active RenderGraph depth
    /// attachment before a farther transparent probe. Occlusion, not the
    /// DepthOnly color output, is the positive depth-write control. This is a
    /// directed test Pass, not proof of URP's normal depth-prepass selection.
    /// </summary>
    public sealed class G2DepthOnlyABTests
    {
        private const string CurrentPath = "Assets/NBShaderSamples/NBShaderSamples/UnLit.mat";
        private const string FrozenPath = "Assets/NBShaderSamples/NBShader_VFXGraphValidation/Materials/UnLit.mat";
        private const string PassName = "DepthOnly";
        private const int Size = 128;
        internal const int ForegroundLayer = 3; // Excluded by the active HighFidelity renderer's ordinary draw masks.
        private const int ProbeLayer = 2; // Included by its transparent draw mask.

        [Test]
        public void DirectedDepthOnlyOccludesFarProbeAndMatchesFrozen()
        {
            var currentSource = AssetDatabase.LoadAssetAtPath<Material>(CurrentPath);
            var frozenSource = AssetDatabase.LoadAssetAtPath<Material>(FrozenPath);
            Assert.That(currentSource, Is.Not.Null);
            Assert.That(frozenSource, Is.Not.Null);
            Assert.That(currentSource.shader.name, Is.EqualTo("Effects/NBShader"));
            Assert.That(frozenSource.shader.name, Is.EqualTo("Effects/NBShader_T00_Frozen"));
            Assert.That(GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>()
                .enableRenderCompatibilityMode, Is.False, "P2's directed RendererList is RenderGraph-only.");
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            Assert.That(pipeline, Is.Not.Null);
            Assert.That(pipeline.rendererDataList.Length, Is.GreaterThan(0));
            var rendererData = pipeline.rendererDataList[0];
            Assert.That(rendererData, Is.Not.Null);
            var rendererAssetPath = AssetDatabase.GetAssetPath(rendererData);
            Assert.That(rendererAssetPath, Is.Not.Empty);
            var rendererAssetFile = Path.Combine(Path.GetDirectoryName(Application.dataPath), rendererAssetPath);
            var rendererAssetBytes = File.ReadAllBytes(rendererAssetFile);

            var current = new Material(currentSource);
            var frozen = new Material(frozenSource);
            var probeShader = Shader.Find("Universal Render Pipeline/Unlit");
            Assert.That(probeShader, Is.Not.Null);
            var probeMaterial = new Material(probeShader);
            var foreground = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var probe = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var cameraObject = new GameObject("NBFX_G2_P2_Camera");
            var camera = cameraObject.AddComponent<Camera>();
            var cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
            var previousActive = RenderTexture.active;
            G2DirectedDepthOnlyFeature feature = null;
            Material postMaterial = null;
            int previousPostFlags = 0;
            try
            {
                ConfigureForeground(current);
                ConfigureForeground(frozen);
                VerifyDepthPass(current);
                VerifyDepthPass(frozen);
                ConfigureProbe(probeMaterial);

                foreground.layer = ForegroundLayer;
                foreground.transform.position = new Vector3(0, 0, 2); // Nearer to camera.
                foreground.transform.localScale = new Vector3(1.2f, 1.2f, 1);
                var foregroundRenderer = foreground.GetComponent<MeshRenderer>();
                foregroundRenderer.shadowCastingMode = ShadowCastingMode.Off;
                foregroundRenderer.receiveShadows = false;
                probe.layer = ProbeLayer;
                probe.transform.position = new Vector3(0, 0, 1); // Behind the depth writer.
                probe.transform.localScale = new Vector3(3, 3, 1);
                var probeRenderer = probe.GetComponent<MeshRenderer>();
                probeRenderer.sharedMaterial = probeMaterial;
                probeRenderer.shadowCastingMode = ShadowCastingMode.Off;

                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.125f, 0.25f, 0.375f, 1);
                camera.orthographic = true;
                camera.orthographicSize = 1;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 10;
                camera.allowHDR = false;
                camera.allowMSAA = false;
                camera.cullingMask = (1 << ForegroundLayer) | (1 << ProbeLayer);
                camera.transform.position = new Vector3(0, 0, 3);
                camera.transform.rotation = Quaternion.Euler(0, 180, 0);
                camera.targetTexture = target;
                cameraData.renderPostProcessing = false; // Existing NBPostProcess remains a renderer feature.
                target.filterMode = FilterMode.Point;
                target.Create();

                feature = ScriptableObject.CreateInstance<G2DirectedDepthOnlyFeature>();
                feature.hideFlags = HideFlags.HideAndDontSave;
                feature.targetCamera = camera;
                feature.Create();
                feature.SetActive(true);
                rendererData.rendererFeatures.Add(feature);
                rendererData.SetDirty(); // URP renderer invalidation only; never SaveAssets.

                var output = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/NBFXG2");
                Directory.CreateDirectory(output);
                foregroundRenderer.enabled = false;
                camera.Render();
                var preFlags = ReadCurrentTarget(target, Path.Combine(output, "p2_depth_pre_flags.png"));
                var nbFeature = FindNBPostProcess(rendererData);
                Assert.That(nbFeature, Is.Not.Null);
                var field = nbFeature.GetType().GetField("NBPostProcessMaterial", BindingFlags.Public | BindingFlags.Static);
                Assert.That(field, Is.Not.Null);
                postMaterial = field.GetValue(null) as Material;
                Assert.That(postMaterial, Is.Not.Null);
                Assert.That(AssetDatabase.Contains(postMaterial), Is.False);
                Assert.That(postMaterial.HasProperty("_NBPostProcessFlags"), Is.True);
                previousPostFlags = postMaterial.GetInteger("_NBPostProcessFlags");
                postMaterial.SetInteger("_NBPostProcessFlags", 0); // In-memory runtime material; restored below.

                var empty = Capture(camera, target, foregroundRenderer, current, false, false,
                    Path.Combine(output, "p2_depth_empty.png"));
                var currentOff = Capture(camera, target, foregroundRenderer, current, true, false,
                    Path.Combine(output, "p2_depth_current_off.png"));
                var frozenOff = Capture(camera, target, foregroundRenderer, frozen, true, false,
                    Path.Combine(output, "p2_depth_frozen_off.png"));
                var currentOn = Capture(camera, target, foregroundRenderer, current, true, true,
                    Path.Combine(output, "p2_depth_current_on.png"));
                var frozenOn = Capture(camera, target, foregroundRenderer, frozen, true, true,
                    Path.Combine(output, "p2_depth_frozen_on.png"));

                int emptyCurrentOff = CountDifferent(empty, currentOff, 0, false);
                int emptyFrozenOff = CountDifferent(empty, frozenOff, 0, false);
                int offAB = CountDifferent(currentOff, frozenOff, 0, false);
                int onAB = CountDifferent(currentOn, frozenOn, 0, false);
                int currentOcclusion = CountDifferent(currentOff, currentOn, 4, true);
                int frozenOcclusion = CountDifferent(frozenOff, frozenOn, 4, true);
                int outside = CountOutsideCenter(currentOff, currentOn, 4);
                string metrics = "P2 exact DepthOnly RendererList -> active depth attachment; directed, not normal URP prepass\n" +
                    "preFlagsCenter=" + Center(preFlags) + ", emptyCenter=" + Center(empty) +
                    ", currentOffCenter=" + Center(currentOff) + ", currentOnCenter=" + Center(currentOn) + "\n" +
                    "recordedFrames=" + feature.recordedFrames + ", validDepthFrames=" + feature.validDepthFrames +
                    ", emptyCurrentOff=" + emptyCurrentOff + ", emptyFrozenOff=" + emptyFrozenOff +
                    ", offAB=" + offAB + ", onAB=" + onAB +
                    ", currentOcclusionROI(>4)=" + currentOcclusion +
                    ", frozenOcclusionROI(>4)=" + frozenOcclusion + ", outsideROI(>4)=" + outside + "\n";
                File.WriteAllText(Path.Combine(output, "p2_depth_metrics.txt"), metrics);
                Debug.Log("NBFX-G2-P2: " + metrics);

                Assert.That(feature.recordedFrames, Is.GreaterThanOrEqualTo(5),
                    "The test feature must record a pass for every measured Camera.Render.");
                Assert.That(feature.validDepthFrames, Is.EqualTo(feature.recordedFrames),
                    "The test requires a valid RenderGraph depth attachment.");
                Assert.That(empty[(Size / 2) * Size + Size / 2].g, Is.GreaterThan(200),
                    "The farther green transparent probe must visibly render without the depth writer.");
                Assert.That(emptyCurrentOff, Is.Zero, "Disabling DepthOnly must restore the probe.");
                Assert.That(emptyFrozenOff, Is.Zero);
                Assert.That(offAB, Is.Zero);
                Assert.That(onAB, Is.Zero, "Current DepthOnly occlusion differs from G0 Frozen.");
                Assert.That(currentOcclusion, Is.GreaterThan(100), "Current DepthOnly did not occlude the farther probe.");
                Assert.That(frozenOcclusion, Is.GreaterThan(100), "Frozen positive depth control did not occlude.");
                Assert.That(currentOn[(Size / 2) * Size + Size / 2].g, Is.LessThan(100),
                    "The near depth writer should reject the farther probe at the center.");
                Assert.That(outside, Is.Zero, "DepthOnly affected color outside its Quad neighborhood.");
            }
            finally
            {
                if (postMaterial != null)
                    postMaterial.SetInteger("_NBPostProcessFlags", previousPostFlags);
                if (feature != null)
                {
                    rendererData.rendererFeatures.Remove(feature);
                    rendererData.SetDirty();
                    UnityEngine.Object.DestroyImmediate(feature);
                }
                camera.targetTexture = null;
                RenderTexture.active = previousActive;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(foreground);
                UnityEngine.Object.DestroyImmediate(probe);
                UnityEngine.Object.DestroyImmediate(current);
                UnityEngine.Object.DestroyImmediate(frozen);
                UnityEngine.Object.DestroyImmediate(probeMaterial);
                Assert.That(File.ReadAllBytes(rendererAssetFile), Is.EqualTo(rendererAssetBytes),
                    "The temporary feature must not change the renderer asset on disk.");
            }
        }

        private static ScriptableRendererFeature FindNBPostProcess(ScriptableRendererData data)
        {
            foreach (var feature in data.rendererFeatures)
                if (feature != null && feature.GetType().FullName == "NBShader.NBPostProcess" && feature.isActive)
                    return feature;
            return null;
        }

        private static void ConfigureForeground(Material material)
        {
            material.DisableKeyword("_FRESNEL");
            material.DisableKeyword("_NORMALMAP");
            material.DisableKeyword("_PARCUSTOMDATA_ON");
            material.DisableKeyword("_SPECULAR_COLOR");
            material.DisableKeyword("_ALPHATEST_ON");
            material.SetTexture("_BaseMap", Texture2D.whiteTexture);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_AlphaAll", 1);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.SetFloat("_ZTest", (float)CompareFunction.LessEqual);
            material.renderQueue = 3000;
            material.SetShaderPassEnabled("SRPDefaultUnlit", false);
            material.SetShaderPassEnabled("SRPDEFAULTUNLIT", false);
            material.SetShaderPassEnabled("UniversalForward", false);
            material.SetShaderPassEnabled(PassName, false);
            material.SetShaderPassEnabled("ShadowCaster", false);
            material.SetShaderPassEnabled("NBCameraOpaqueDistortPass", false);
            material.SetShaderPassEnabled("NBDeferredDistortPass", false);
            material.SetShaderPassEnabled("Universal2D", false);
        }

        private static void ConfigureProbe(Material material)
        {
            material.SetTexture("_BaseMap", Texture2D.whiteTexture);
            material.SetColor("_BaseColor", Color.green);
            material.SetFloat("_Surface", 1);
            material.SetFloat("_SrcBlend", (float)BlendMode.One);
            material.SetFloat("_DstBlend", (float)BlendMode.Zero);
            material.SetFloat("_ZWrite", 0);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = 3000;
        }

        private static void VerifyDepthPass(Material material)
        {
            int index = material.FindPass(PassName);
            Assert.That(index, Is.GreaterThanOrEqualTo(0), material.shader.name + " lost DepthOnly.");
            Assert.That(material.shader.FindPassTagValue(0, index, new ShaderTagId("LightMode")).name,
                Is.EqualTo(PassName), material.shader.name + " changed DepthOnly LightMode.");
        }

        private static Color32[] Capture(Camera camera, RenderTexture target, MeshRenderer foreground,
            Material material, bool visible, bool depthPassEnabled, string path)
        {
            foreground.sharedMaterial = material;
            foreground.enabled = visible;
            material.SetShaderPassEnabled(PassName, depthPassEnabled);
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

        private static string Center(Color32[] pixels)
        {
            var p = pixels[(Size / 2) * Size + Size / 2];
            return "(" + p.r + "," + p.g + "," + p.b + "," + p.a + ")";
        }

        private static bool IsCenter(int x, int y) => x >= Size / 5 && x < Size * 4 / 5 &&
                                                      y >= Size / 5 && y < Size * 4 / 5;

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

    internal sealed class G2DirectedDepthOnlyFeature : ScriptableRendererFeature
    {
        internal Camera targetCamera;
        internal int recordedFrames;
        internal int validDepthFrames;
        private DepthPass _pass;

        public override void Create()
        {
            _pass = new DepthPass(this)
            {
                renderPassEvent = RenderPassEvent.AfterRenderingOpaques + 1
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.camera == targetCamera)
                renderer.EnqueuePass(_pass);
        }

        private sealed class DepthPass : ScriptableRenderPass
        {
            private readonly G2DirectedDepthOnlyFeature _owner;
            private static readonly List<ShaderTagId> Tags = new List<ShaderTagId> { new ShaderTagId("DepthOnly") };
            private sealed class PassData { internal RendererListHandle rendererList; }

            internal DepthPass(G2DirectedDepthOnlyFeature owner) { _owner = owner; }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                _owner.recordedFrames++;
                var resources = frameData.Get<UniversalResourceData>();
                if (!resources.activeDepthTexture.IsValid() || !resources.activeColorTexture.IsValid())
                    return;
                _owner.validDepthFrames++;
                var cameraData = frameData.Get<UniversalCameraData>();
                var renderingData = frameData.Get<UniversalRenderingData>();
                var lightData = frameData.Get<UniversalLightData>();
                var drawing = RenderingUtils.CreateDrawingSettings(Tags, renderingData, cameraData, lightData,
                    cameraData.defaultOpaqueSortFlags);
                var filtering = new FilteringSettings(RenderQueueRange.all, 1 << G2DepthOnlyABTests.ForegroundLayer);
                var list = renderGraph.CreateRendererList(new RendererListParams(renderingData.cullResults, drawing, filtering));
                using (var builder = renderGraph.AddRasterRenderPass<PassData>("NBFX G2 directed DepthOnly", out var data))
                {
                    data.rendererList = list;
                    builder.UseRendererList(list);
                    // DepthOnly currently has ColorMask R and an SV_Target output.
                    // Attach color for ShaderLab fidelity; the later full-screen
                    // green probe overwrites it only where depth testing succeeds.
                    builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.ReadWrite);
                    builder.SetRenderAttachmentDepth(resources.activeDepthTexture, AccessFlags.ReadWrite);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc(static (PassData passData, RasterGraphContext context) =>
                        context.cmd.DrawRendererList(passData.rendererList));
                }
            }
        }
    }
}
