using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace NBFX.Baseline.Tests
{
    /// <summary>
    /// G2/P3: real URP main-light shadow rendering, not a directed test RendererList.
    /// Only the NB foreground's ShadowCaster Pass can affect the Lit receiver.
    /// </summary>
    public sealed class G2ShadowCasterABTests
    {
        private const string CurrentPath = "Assets/NBShaderSamples/NBShaderSamples/UnLit.mat";
        private const string FrozenPath = "Assets/NBShaderSamples/NBShader_VFXGraphValidation/Materials/UnLit.mat";
        private const int Size = 128;
        private const int Layer = 2; // Included by the active HighFidelity renderer's opaque mask (55).

        [Test]
        public void MainLightShadowCasterMatchesFrozenAndPassOffRemovesVisibleShadow()
        {
            var currentSource = AssetDatabase.LoadAssetAtPath<Material>(CurrentPath);
            var frozenSource = AssetDatabase.LoadAssetAtPath<Material>(FrozenPath);
            Assert.That(currentSource, Is.Not.Null, CurrentPath);
            Assert.That(frozenSource, Is.Not.Null, FrozenPath);
            Assert.That(currentSource.shader.name, Is.EqualTo("Effects/NBShader"));
            Assert.That(frozenSource.shader.name, Is.EqualTo("Effects/NBShader_T00_Frozen"));
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            Assert.That(pipeline, Is.Not.Null, "P3 requires active URP.");
            Assert.That(GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>()
                .enableRenderCompatibilityMode, Is.False, "P3 targets the current RenderGraph environment.");
            Assert.That(pipeline.supportsMainLightShadows, Is.True, "The active URP asset must support main-light shadows.");
            Assert.That(pipeline.shadowDistance, Is.GreaterThan(10));
            Assert.That(QualitySettings.shadows, Is.Not.EqualTo(UnityEngine.ShadowQuality.Disable));

            var receiverShader = Shader.Find("Universal Render Pipeline/Lit");
            Assert.That(receiverShader, Is.Not.Null);
            Assert.That(receiverShader.isSupported, Is.True);
            var current = new Material(currentSource);
            var frozen = new Material(frozenSource);
            var receiverMaterial = new Material(receiverShader);
            var receiver = GameObject.CreatePrimitive(PrimitiveType.Plane);
            var caster = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var lightObject = new GameObject("NBFX_G2_P3_DirectionalLight");
            var light = lightObject.AddComponent<Light>();
            var cameraObject = new GameObject("NBFX_G2_P3_Camera");
            var camera = cameraObject.AddComponent<Camera>();
            var cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
            var previousActive = RenderTexture.active;
            var previousSun = RenderSettings.sun;
            var previousAmbientMode = RenderSettings.ambientMode;
            var previousAmbientLight = RenderSettings.ambientLight;
            var previousFog = RenderSettings.fog;
            var evidence = new StringBuilder();
            try
            {
                ConfigureCaster(current);
                ConfigureCaster(frozen);
                VerifyShadowPass(current);
                VerifyShadowPass(frozen);
                evidence.AppendLine("P3 real URP main-light ShadowCaster; RenderGraph; RGBA32 128x128");
                evidence.AppendLine("currentShader=" + current.shader.name + " frozenShader=" + frozen.shader.name +
                    " pipeline=" + pipeline.name + " shadowDistance=" + pipeline.shadowDistance);

                receiverMaterial.SetColor("_BaseColor", Color.white);
                receiverMaterial.SetFloat("_Surface", 0);
                receiverMaterial.SetFloat("_Cull", (float)CullMode.Off);
                receiverMaterial.DisableKeyword("_RECEIVE_SHADOWS_OFF");
                receiver.layer = Layer;
                receiver.transform.position = Vector3.zero;
                receiver.transform.localScale = new Vector3(.55f, 1, .55f);
                var receiverRenderer = receiver.GetComponent<MeshRenderer>();
                receiverRenderer.sharedMaterial = receiverMaterial;
                receiverRenderer.shadowCastingMode = ShadowCastingMode.Off;
                receiverRenderer.receiveShadows = true;

                caster.layer = Layer;
                caster.transform.position = new Vector3(0, 1, 0);
                caster.transform.rotation = Quaternion.Euler(-90, 0, 0); // horizontal, +Y normal
                caster.transform.localScale = new Vector3(1.5f, 1.5f, 1);
                var casterRenderer = caster.GetComponent<MeshRenderer>();
                casterRenderer.shadowCastingMode = ShadowCastingMode.On;
                casterRenderer.receiveShadows = false;

                light.type = LightType.Directional;
                light.shadows = LightShadows.Hard;
                light.shadowStrength = 1;
                light.intensity = 2;
                light.color = Color.white;
                light.cullingMask = 1 << Layer;
                light.transform.rotation = Quaternion.Euler(50, -30, 0); // light direction has negative Y
                RenderSettings.sun = light; // URP chooses this directional as the main light.
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = Color.black;
                RenderSettings.fog = false;

                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.08f, .13f, .19f, 1);
                camera.orthographic = true;
                camera.orthographicSize = 3.5f;
                camera.nearClipPlane = .1f;
                camera.farClipPlane = 25;
                camera.allowHDR = false;
                camera.allowMSAA = false;
                camera.cullingMask = 1 << Layer;
                camera.transform.position = new Vector3(0, 4.5f, -5.5f);
                camera.transform.LookAt(Vector3.zero);
                camera.targetTexture = target;
                cameraData.renderShadows = true;
                cameraData.renderPostProcessing = false;
                target.filterMode = FilterMode.Point;
                target.Create();

                var output = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/NBFXG2");
                Directory.CreateDirectory(output);
                casterRenderer.enabled = false;
                Capture(camera, target, Path.Combine(output, "p3_shadow_warmup.png"));
                var empty = Capture(camera, target, Path.Combine(output, "p3_shadow_empty.png"));
                casterRenderer.enabled = true;
                var currentOff = CaptureState(camera, target, casterRenderer, current, false,
                    Path.Combine(output, "p3_shadow_current_off.png"));
                var frozenOff = CaptureState(camera, target, casterRenderer, frozen, false,
                    Path.Combine(output, "p3_shadow_frozen_off.png"));
                var currentOn = CaptureState(camera, target, casterRenderer, current, true,
                    Path.Combine(output, "p3_shadow_current_on.png"));
                var frozenOn = CaptureState(camera, target, casterRenderer, frozen, true,
                    Path.Combine(output, "p3_shadow_frozen_on.png"));

                var emptyOffCurrent = CountDifferent(empty, currentOff);
                var emptyOffFrozen = CountDifferent(empty, frozenOff);
                var offAB = CountDifferent(currentOff, frozenOff);
                var onAB = CountDifferent(currentOn, frozenOn);
                var receiverVisiblePixels = CountDifferentFromPixel(empty, empty[0]);
                var currentShadowPixels = CountDarkened(currentOff, currentOn, 8);
                var frozenShadowPixels = CountDarkened(frozenOff, frozenOn, 8);
                evidence.AppendLine("emptyVsCurrentOff=" + emptyOffCurrent + " emptyVsFrozenOff=" + emptyOffFrozen +
                    " offAB=" + offAB + " onAB=" + onAB);
                evidence.AppendLine("receiverVisiblePixels=" + receiverVisiblePixels +
                    " currentDarkenedPixels(>8)=" + currentShadowPixels +
                    " frozenDarkenedPixels(>8)=" + frozenShadowPixels +
                    " emptyCenter=" + Center(empty) + " currentOffCenter=" + Center(currentOff) +
                    " currentOnCenter=" + Center(currentOn));
                File.WriteAllText(Path.Combine(output, "p3_shadow_metrics.txt"), evidence.ToString(), new UTF8Encoding(false));

                Assert.That(receiverVisiblePixels, Is.GreaterThan(1000),
                    "The Lit receiving plane is not visible; an empty shadow A/B would be vacuous.");
                Assert.That(emptyOffCurrent, Is.Zero, "Disabling the current ShadowCaster Pass must remove the shadow.");
                Assert.That(emptyOffFrozen, Is.Zero, "Disabling the frozen ShadowCaster Pass must remove the shadow.");
                Assert.That(offAB, Is.Zero);
                Assert.That(onAB, Is.Zero, "Current and G0 Frozen ShadowCaster outputs differ.");
                Assert.That(currentShadowPixels, Is.GreaterThan(32),
                    "Current Pass-on did not darken the Lit receiver; this is not a ShadowCaster execution proof.");
                Assert.That(frozenShadowPixels, Is.GreaterThan(32),
                    "Frozen Pass-on did not darken the Lit receiver; the positive control failed.");
            }
            finally
            {
                RenderSettings.sun = previousSun;
                RenderSettings.ambientMode = previousAmbientMode;
                RenderSettings.ambientLight = previousAmbientLight;
                RenderSettings.fog = previousFog;
                camera.targetTexture = null;
                RenderTexture.active = previousActive;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(caster);
                UnityEngine.Object.DestroyImmediate(receiver);
                UnityEngine.Object.DestroyImmediate(receiverMaterial);
                UnityEngine.Object.DestroyImmediate(current);
                UnityEngine.Object.DestroyImmediate(frozen);
            }
        }

        private static void ConfigureCaster(Material material)
        {
            material.DisableKeyword("_FRESNEL");
            material.DisableKeyword("_NORMALMAP");
            material.DisableKeyword("_PARCUSTOMDATA_ON");
            material.DisableKeyword("_SPECULAR_COLOR");
            material.EnableKeyword("_FX_LIGHT_MODE_UNLIT");
            material.SetTexture("_BaseMap", Texture2D.whiteTexture);
            material.SetTextureScale("_BaseMap", Vector2.one);
            material.SetTextureOffset("_BaseMap", Vector2.zero);
            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_ColorA", Color.white);
            material.SetFloat("_BaseColorIntensityForTimeline", 1);
            material.SetFloat("_AlphaAll", 1);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.SetInteger("_W9ParticleShaderFlags", 0);
            material.SetInteger("_W9ParticleShaderFlags1", 0); // no transparent-shadow clip or dither
            material.renderQueue = 2100; // opaque shadow caster range
            material.SetShaderPassEnabled("SRPDefaultUnlit", false);
            material.SetShaderPassEnabled("SRPDEFAULTUNLIT", false);
            material.SetShaderPassEnabled("UniversalForward", false);
            material.SetShaderPassEnabled("DepthOnly", false);
            material.SetShaderPassEnabled("NBCameraOpaqueDistortPass", false);
            material.SetShaderPassEnabled("NBDeferredDistortPass", false);
            material.SetShaderPassEnabled("Universal2D", false);
            material.SetShaderPassEnabled("ShadowCaster", true);
        }

        private static void VerifyShadowPass(Material material)
        {
            var passIndex = material.FindPass("ShadowCaster");
            Assert.That(passIndex, Is.GreaterThanOrEqualTo(0), material.shader.name + " lost ShadowCaster.");
            var lightMode = material.shader.FindPassTagValue(0, passIndex, new ShaderTagId("LightMode")).name;
            Assert.That(string.Equals(lightMode, "ShadowCaster", StringComparison.OrdinalIgnoreCase), Is.True,
                material.shader.name + " ShadowCaster LightMode changed: " + lightMode);
        }

        private static Color32[] CaptureState(Camera camera, RenderTexture target, MeshRenderer caster,
            Material material, bool passEnabled, string path)
        {
            caster.sharedMaterial = material;
            material.SetShaderPassEnabled("ShadowCaster", passEnabled);
            return Capture(camera, target, path);
        }

        private static Color32[] Capture(Camera camera, RenderTexture target, string path)
        {
            camera.Render();
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
            var c = pixels[(Size / 2) * Size + Size / 2];
            return "(" + c.r + "," + c.g + "," + c.b + "," + c.a + ")";
        }

        private static int CountDifferent(Color32[] a, Color32[] b)
        {
            Assert.That(b.Length, Is.EqualTo(a.Length));
            var count = 0;
            for (var i = 0; i < a.Length; i++)
                if (a[i].r != b[i].r || a[i].g != b[i].g || a[i].b != b[i].b || a[i].a != b[i].a)
                    count++;
            return count;
        }

        private static int CountDifferentFromPixel(Color32[] pixels, Color32 reference)
        {
            var count = 0;
            for (var i = 0; i < pixels.Length; i++)
                if (pixels[i].r != reference.r || pixels[i].g != reference.g ||
                    pixels[i].b != reference.b || pixels[i].a != reference.a)
                    count++;
            return count;
        }

        private static int CountDarkened(Color32 off, Color32 on, int threshold)
        {
            return Math.Max(0, off.r - on.r) + Math.Max(0, off.g - on.g) + Math.Max(0, off.b - on.b) > threshold ? 1 : 0;
        }

        private static int CountDarkened(Color32[] off, Color32[] on, int threshold)
        {
            Assert.That(on.Length, Is.EqualTo(off.Length));
            var count = 0;
            for (var i = 0; i < off.Length; i++) count += CountDarkened(off[i], on[i], threshold);
            return count;
        }
    }
}
