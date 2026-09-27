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
    /// G2/P1 only. A temporary RendererFeature creates a one-tag RendererList for
    /// each main Pass. This proves directed Pass execution, NOT URP's normal
    /// main-Pass selection policy. No production feature or asset is saved.
    /// </summary>
    public sealed class G2MainPassABTests
    {
        private const string CurrentPath = "Assets/NBShaderSamples/NBShaderSamples/UnLit.mat";
        private const string FrozenPath = "Assets/NBShaderSamples/NBShader_VFXGraphValidation/Materials/UnLit.mat";
        private const int Size = 128;
        internal const int ForegroundLayer = 3; // Balanced Renderer excludes 3; only our exact-tag list draws it.
        private const int CenterMin = Size / 5;
        private const int CenterMax = Size * 4 / 5;

        [TestCase("SRPDefaultUnlit")]
        [TestCase("UniversalForward")]
        public void DirectedMainPassMatchesFrozenAndPassOffIsInvisible(string passName)
        {
            var currentSource = AssetDatabase.LoadAssetAtPath<Material>(CurrentPath);
            var frozenSource = AssetDatabase.LoadAssetAtPath<Material>(FrozenPath);
            Assert.That(currentSource, Is.Not.Null);
            Assert.That(frozenSource, Is.Not.Null);
            Assert.That(currentSource.shader.name, Is.EqualTo("Effects/NBShader"));
            Assert.That(frozenSource.shader.name, Is.EqualTo("Effects/NBShader_T00_Frozen"));
            Assert.That(currentSource.renderQueue, Is.EqualTo(frozenSource.renderQueue));
            Assert.That(GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>()
                .enableRenderCompatibilityMode, Is.False, "This test-only RendererList runs in RenderGraph.");
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
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var cameraObject = new GameObject("NBFX_G2_P1_Camera");
            var camera = cameraObject.AddComponent<Camera>();
            var cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
            var previousActive = RenderTexture.active;
            G2DirectedMainPassFeature feature = null;
            Material postMaterial = null;
            int previousPostFlags = 0;
            try
            {
                ConfigureMaterial(current);
                ConfigureMaterial(frozen);
                VerifyPass(current, passName);
                VerifyPass(frozen, passName);

                quad.layer = ForegroundLayer;
                quad.transform.position = new Vector3(0, 0, 2);
                quad.transform.localScale = new Vector3(1.2f, 1.2f, 1);
                var quadRenderer = quad.GetComponent<MeshRenderer>();
                quadRenderer.shadowCastingMode = ShadowCastingMode.Off;
                quadRenderer.receiveShadows = false;

                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.125f, 0.25f, 0.375f, 1);
                camera.orthographic = true;
                camera.orthographicSize = 1;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 10;
                camera.allowHDR = false;
                camera.allowMSAA = false;
                camera.cullingMask = 1 << ForegroundLayer;
                camera.transform.position = new Vector3(0, 0, 3);
                camera.transform.rotation = Quaternion.Euler(0, 180, 0);
                camera.targetTexture = target;
                cameraData.renderPostProcessing = false; // Existing NBPostProcess is a renderer feature.
                target.filterMode = FilterMode.Point;
                target.Create();

                // Mirrors GF's in-memory feature attachment. SetDirty only invalidates
                // the renderer instance in URP 17.3; it is not AssetDatabase.SaveAssets.
                feature = ScriptableObject.CreateInstance<G2DirectedMainPassFeature>();
                feature.hideFlags = HideFlags.HideAndDontSave;
                feature.targetCamera = camera;
                feature.selectedPass = passName;
                feature.Create();
                feature.SetActive(true);
                rendererData.rendererFeatures.Add(feature);
                rendererData.SetDirty();

                var output = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/NBFXG2");
                Directory.CreateDirectory(output);
                quadRenderer.enabled = false;
                camera.Render(); // Warm up the newly rebuilt renderer and runtime Uber material.
                var preFlags = ReadCurrentTarget(target,
                    Path.Combine(output, "p1_" + passName + "_pre_flags.png"));
                var nbFeature = FindNBPostProcess(rendererData);
                Assert.That(nbFeature, Is.Not.Null);
                var materialField = nbFeature.GetType().GetField("NBPostProcessMaterial", BindingFlags.Public | BindingFlags.Static);
                Assert.That(materialField, Is.Not.Null);
                postMaterial = materialField.GetValue(null) as Material;
                Assert.That(postMaterial, Is.Not.Null);
                Assert.That(AssetDatabase.Contains(postMaterial), Is.False);
                Assert.That(postMaterial.HasProperty("_NBPostProcessFlags"), Is.True);
                previousPostFlags = postMaterial.GetInteger("_NBPostProcessFlags");
                postMaterial.SetInteger("_NBPostProcessFlags", 0); // Runtime material only; restored in finally.

                var empty = Capture(camera, target, quadRenderer, current, false, passName, false,
                    Path.Combine(output, "p1_" + passName + "_empty.png"));
                var currentOff = Capture(camera, target, quadRenderer, current, true, passName, false,
                    Path.Combine(output, "p1_" + passName + "_current_off.png"));
                var frozenOff = Capture(camera, target, quadRenderer, frozen, true, passName, false,
                    Path.Combine(output, "p1_" + passName + "_frozen_off.png"));
                var currentOn = Capture(camera, target, quadRenderer, current, true, passName, true,
                    Path.Combine(output, "p1_" + passName + "_current_on.png"));
                var frozenOn = Capture(camera, target, quadRenderer, frozen, true, passName, true,
                    Path.Combine(output, "p1_" + passName + "_frozen_on.png"));

                int emptyOffCurrent = CountDifferent(empty, currentOff, 0, false);
                int emptyOffFrozen = CountDifferent(empty, frozenOff, 0, false);
                int offAB = CountDifferent(currentOff, frozenOff, 0, false);
                int onAB = CountDifferent(currentOn, frozenOn, 0, false);
                int currentVisible = CountDifferent(currentOff, currentOn, 4, true);
                int frozenVisible = CountDifferent(frozenOff, frozenOn, 4, true);
                int outside = CountOutsideCenter(currentOff, currentOn, 4);
                string metrics = "P1 test-only exact-tag RendererList: " + passName + " (not normal URP main-Pass selection)\n" +
                    "preFlagsCenter=" + Center(preFlags) + ", emptyCenter=" + Center(empty) + "\n" +
                    "emptyOffCurrent=" + emptyOffCurrent + ", emptyOffFrozen=" + emptyOffFrozen +
                    ", offAB=" + offAB + ", onAB=" + onAB +
                    ", currentVisibleROI(>4)=" + currentVisible + ", frozenVisibleROI(>4)=" + frozenVisible +
                    ", outsideROI(>4)=" + outside + "\n";
                File.WriteAllText(Path.Combine(output, "p1_" + passName + "_metrics.txt"), metrics);
                Debug.Log("NBFX-G2-P1: " + metrics);

                Assert.That(empty[(Size / 2) * Size + Size / 2].b, Is.GreaterThan(32),
                    "The empty Camera must retain its nonblack clear color.");
                Assert.That(emptyOffCurrent, Is.Zero, "Pass-off must hide the entire foreground.");
                Assert.That(emptyOffFrozen, Is.Zero);
                Assert.That(offAB, Is.Zero);
                Assert.That(onAB, Is.Zero, "Directed " + passName + " differs from G0 Frozen.");
                Assert.That(currentVisible, Is.GreaterThan(100), "Current " + passName + " did not draw visible color.");
                Assert.That(frozenVisible, Is.GreaterThan(100), "Frozen " + passName + " positive control did not draw.");
                Assert.That(outside, Is.Zero, "Directed Pass changed pixels outside its test Quad.");
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
                UnityEngine.Object.DestroyImmediate(quad);
                UnityEngine.Object.DestroyImmediate(current);
                UnityEngine.Object.DestroyImmediate(frozen);
                Assert.That(File.ReadAllBytes(rendererAssetFile), Is.EqualTo(rendererAssetBytes),
                    "The test must not change the renderer asset on disk.");
            }
        }

        private static ScriptableRendererFeature FindNBPostProcess(ScriptableRendererData data)
        {
            foreach (var feature in data.rendererFeatures)
                if (feature != null && feature.GetType().FullName == "NBShader.NBPostProcess" && feature.isActive)
                    return feature;
            return null;
        }

        private static void ConfigureMaterial(Material material)
        {
            material.DisableKeyword("_FRESNEL");
            material.DisableKeyword("_NORMALMAP");
            material.DisableKeyword("_PARCUSTOMDATA_ON");
            material.DisableKeyword("_SPECULAR_COLOR");
            material.EnableKeyword("_FX_LIGHT_MODE_UNLIT");
            material.SetTexture("_BaseMap", Texture2D.whiteTexture);
            material.SetTextureScale("_BaseMap", Vector2.one);
            material.SetTextureOffset("_BaseMap", Vector2.zero);
            material.SetColor("_BaseColor", Color.red);
            material.SetFloat("_BaseColorIntensityForTimeline", 1);
            material.SetFloat("_AlphaAll", 1);
            material.SetFloat("_Cull", (float)CullMode.Off); // The backface Pass has its own Cull Front.
            material.SetFloat("_ZWrite", 0);
            material.SetFloat("_ZTest", (float)CompareFunction.LessEqual);
            material.SetFloat("_SrcBlend", (float)BlendMode.One);
            material.SetFloat("_DstBlend", (float)BlendMode.Zero);
            material.renderQueue = 3000;
            material.SetShaderPassEnabled("SRPDefaultUnlit", false);
            material.SetShaderPassEnabled("SRPDEFAULTUNLIT", false); // Serialized material uses uppercase spelling.
            material.SetShaderPassEnabled("UniversalForward", false);
            material.SetShaderPassEnabled("DepthOnly", false);
            material.SetShaderPassEnabled("ShadowCaster", false);
            material.SetShaderPassEnabled("NBCameraOpaqueDistortPass", false);
            material.SetShaderPassEnabled("NBDeferredDistortPass", false);
            material.SetShaderPassEnabled("Universal2D", false);
        }

        private static void VerifyPass(Material material, string passName)
        {
            int passIndex = material.FindPass(passName);
            Assert.That(passIndex, Is.GreaterThanOrEqualTo(0), material.shader.name + " lost " + passName);
            string actualTag = material.shader.FindPassTagValue(0, passIndex, new ShaderTagId("LightMode")).name;
            Assert.That(string.Equals(actualTag, passName, StringComparison.OrdinalIgnoreCase), Is.True,
                material.shader.name + " changed the " + passName + " LightMode: " + actualTag);
        }

        private static Color32[] Capture(Camera camera, RenderTexture target, MeshRenderer quad,
            Material material, bool visible, string passName, bool passEnabled, string path)
        {
            quad.sharedMaterial = material;
            quad.enabled = visible;
            material.SetShaderPassEnabled(passName, passEnabled);
            if (passName == "SRPDefaultUnlit")
                material.SetShaderPassEnabled("SRPDEFAULTUNLIT", passEnabled);
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

        private static bool IsCenter(int x, int y) => x >= CenterMin && x < CenterMax &&
                                                      y >= CenterMin && y < CenterMax;

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

    // Test-only feature in the same URP-specific Editor assembly. Only the
    // fixture camera is affected; the exact one-tag list excludes the background.
    internal sealed class G2DirectedMainPassFeature : ScriptableRendererFeature
    {
        internal Camera targetCamera;
        internal string selectedPass;
        private G2DirectedMainPass _pass;

        public override void Create()
        {
            _pass = new G2DirectedMainPass(this)
            {
                renderPassEvent = RenderPassEvent.AfterRenderingOpaques + 1
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.camera == targetCamera && selectedPass != null)
                renderer.EnqueuePass(_pass);
        }

        private sealed class G2DirectedMainPass : ScriptableRenderPass
        {
            private readonly G2DirectedMainPassFeature _owner;
            private sealed class PassData { internal RendererListHandle rendererList; }

            internal G2DirectedMainPass(G2DirectedMainPassFeature owner) { _owner = owner; }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var resources = frameData.Get<UniversalResourceData>();
                if (!resources.activeColorTexture.IsValid()) return;
                var cameraData = frameData.Get<UniversalCameraData>();
                var renderingData = frameData.Get<UniversalRenderingData>();
                var lightData = frameData.Get<UniversalLightData>();
                var tags = new List<ShaderTagId> { new ShaderTagId(_owner.selectedPass) };
                var drawing = RenderingUtils.CreateDrawingSettings(tags, renderingData, cameraData, lightData,
                    cameraData.defaultOpaqueSortFlags);
                var filtering = new FilteringSettings(RenderQueueRange.all, 1 << G2MainPassABTests.ForegroundLayer);
                var list = renderGraph.CreateRendererList(new RendererListParams(renderingData.cullResults, drawing, filtering));
                using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                           "NBFX G2 directed " + _owner.selectedPass, out var data))
                {
                    data.rendererList = list;
                    builder.UseRendererList(list);
                    builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.ReadWrite);
                    if (resources.activeDepthTexture.IsValid())
                        builder.SetRenderAttachmentDepth(resources.activeDepthTexture, AccessFlags.ReadWrite);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc(static (PassData passData, RasterGraphContext context) =>
                        context.cmd.DrawRendererList(passData.rendererList));
                }
            }
        }
    }
}
