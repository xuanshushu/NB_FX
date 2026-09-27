using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
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
    /// G2/P6: the actual URP 2D Renderer draws the existing Universal2D Pass
    /// on a SpriteRenderer. All pipeline/renderer, scene, material and texture
    /// objects are transient; no official package or project asset is saved.
    /// </summary>
    public sealed class G2Universal2DABTests
    {
        private const string CurrentPath = "Assets/NBShaderSamples/NBShaderSamples/UnLit.mat";
        private const string FrozenPath = "Assets/NBShaderSamples/NBShader_VFXGraphValidation/Materials/UnLit.mat";
        private const string PassName = "Universal2D";
        private const int Size = 128;
        private const int Layer = 2;

        private static readonly string[] ProtectedPaths =
        {
            "ProjectSettings/QualitySettings.asset",
            "ProjectSettings/GraphicsSettings.asset",
            "Assets/UniversalRenderPipelineGlobalSettings.asset",
            "Assets/Settings/URP-HighFidelity.asset",
            "Assets/Settings/URP-HighFidelity-Renderer.asset",
            "Assets/Settings/URP-Balanced.asset",
            "Assets/Settings/URP-Balanced-Renderer.asset",
            "Assets/Settings/URP-Performant.asset",
            "Assets/Settings/URP-Performant-Renderer.asset"
        };

        [Test]
        public void RealRenderer2DUniversal2DPassMatchesFrozenWithVisibleControls()
        {
            // Unity may serialize QualitySettings after Test Runner returns,
            // despite restoring the override and passing immediate file hashes.
            // Run this pipeline-switch test only in a disposable project copy.
            if (Environment.GetEnvironmentVariable("NBFX_G2_P6_ISOLATED_PROJECT") != "1")
                Assert.Ignore("P6 switches QualitySettings; run only in a disposable project copy with NBFX_G2_P6_ISOLATED_PROJECT=1.");

            var currentSource = AssetDatabase.LoadAssetAtPath<Material>(CurrentPath);
            var frozenSource = AssetDatabase.LoadAssetAtPath<Material>(FrozenPath);
            Assert.That(currentSource, Is.Not.Null);
            Assert.That(frozenSource, Is.Not.Null);
            Assert.That(currentSource.shader.name, Is.EqualTo("Effects/NBShader"));
            Assert.That(frozenSource.shader.name, Is.EqualTo("Effects/NBShader_T00_Frozen"));
            Assert.That(GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>()
                .enableRenderCompatibilityMode, Is.False, "P6 exercises the URP 17.3 RenderGraph 2D Renderer.");

            var priorGraphics = GraphicsSettings.defaultRenderPipeline;
            var priorQuality = QualitySettings.renderPipeline;
            var priorActive = GraphicsSettings.currentRenderPipeline;
            var priorURP = priorActive as UniversalRenderPipelineAsset;
            Assert.That(priorURP, Is.Not.Null, "Record the current URP asset before switching the test pipeline.");
            Assert.That(priorURP.rendererDataList.Length, Is.GreaterThan(0));
            var priorRenderer = priorURP.rendererDataList[0];
            Assert.That(priorRenderer, Is.Not.Null);
            var priorURPPath = AssetDatabase.GetAssetPath(priorURP);
            var priorRendererPath = AssetDatabase.GetAssetPath(priorRenderer);
            Assert.That(priorURPPath, Is.Not.Empty, "The active URP asset must be a protected on-disk asset.");
            Assert.That(priorRendererPath, Is.Not.Empty, "The active RendererData must be a protected on-disk asset.");
            var priorURPDirty = EditorUtility.IsDirty(priorURP);
            var priorRendererDirty = EditorUtility.IsDirty(priorRenderer);
            var priorScene = SceneManager.GetActiveScene();
            var priorScenePath = priorScene.path;
            var priorSceneDirty = priorScene.isDirty;
            var priorRenderTexture = RenderTexture.active;
            var fileHashes = CaptureProtectedHashes(priorScenePath,
                priorURPPath, priorRendererPath);
            Debug.Log("NBFX-G2-P6 protected before: activeURP=" + priorURPPath +
                ", renderer=" + priorRendererPath +
                ", scene=" + priorScenePath + ", sceneDirty=" + priorSceneDirty +
                ", hashes=" + DescribeHashes(fileHashes));

            Renderer2DData renderer2DData = null;
            UniversalRenderPipelineAsset pipeline2D = null;
            Material current = null;
            Material frozen = null;
            Texture2D texture = null;
            Sprite sprite = null;
            GameObject spriteObject = null;
            GameObject cameraObject = null;
            RenderTexture target = null;
            Scene previewScene = default;
            try
            {
                renderer2DData = ScriptableObject.CreateInstance<Renderer2DData>();
                renderer2DData.hideFlags = HideFlags.HideAndDontSave;
                pipeline2D = UniversalRenderPipelineAsset.Create(renderer2DData);
                pipeline2D.hideFlags = HideFlags.HideAndDontSave;
                Assert.That(pipeline2D.rendererDataList.Length, Is.EqualTo(1));
                Assert.That(pipeline2D.rendererDataList[0], Is.SameAs(renderer2DData));
                QualitySettings.renderPipeline = pipeline2D;
                Assert.That(GraphicsSettings.currentRenderPipeline, Is.SameAs(pipeline2D),
                    "The test must use the real temporary 2D Renderer, not the project's UniversalRenderer.");

                current = new Material(currentSource);
                frozen = new Material(frozenSource);
                ConfigureMaterial(current);
                ConfigureMaterial(frozen);
                VerifyExactPass(current);
                VerifyExactPass(frozen);

                texture = new Texture2D(16, 16, TextureFormat.RGBA32, false, false);
                var white = new Color32[16 * 16];
                for (var i = 0; i < white.Length; i++) white[i] = new Color32(255, 255, 255, 255);
                texture.SetPixels32(white);
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.Apply(false, false);
                sprite = Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
                current.SetTexture("_BaseMap", texture);
                frozen.SetTexture("_BaseMap", texture);

                // PreviewScene keeps the user's active scene and its dirty state untouched.
                previewScene = EditorSceneManager.NewPreviewScene();
                spriteObject = EditorUtility.CreateGameObjectWithHideFlags(
                    "NBFX_G2_P6_Sprite", HideFlags.HideAndDontSave, typeof(SpriteRenderer));
                SceneManager.MoveGameObjectToScene(spriteObject, previewScene);
                spriteObject.layer = Layer;
                spriteObject.transform.position = new Vector3(0, 0, 2);
                var spriteRenderer = spriteObject.GetComponent<SpriteRenderer>();
                spriteRenderer.sprite = sprite;
                spriteRenderer.color = Color.white;
                spriteRenderer.sharedMaterial = current;
                spriteRenderer.shadowCastingMode = ShadowCastingMode.Off;

                cameraObject = EditorUtility.CreateGameObjectWithHideFlags(
                    "NBFX_G2_P6_Camera", HideFlags.HideAndDontSave, typeof(Camera));
                SceneManager.MoveGameObjectToScene(cameraObject, previewScene);
                var camera = cameraObject.GetComponent<Camera>();
                camera.scene = previewScene;
                camera.cameraType = CameraType.Game;
                camera.enabled = false;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.125f, 0.25f, 0.375f, 1);
                camera.orthographic = true;
                camera.orthographicSize = 1;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 10;
                camera.allowHDR = false;
                camera.allowMSAA = false;
                camera.cullingMask = 1 << Layer;
                camera.transform.position = new Vector3(0, 0, 3);
                camera.transform.rotation = Quaternion.Euler(0, 180, 0);
                target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
                target.filterMode = FilterMode.Point;
                target.Create();
                camera.targetTexture = target;

                var output = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/NBFXG2");
                Directory.CreateDirectory(output);
                spriteRenderer.enabled = false;
                camera.Render(); // Initialize the temporary 2D Renderer before measuring.
                var empty = Capture(camera, target, spriteRenderer, current, false, false,
                    Path.Combine(output, "p6_2d_empty.png"));
                var currentOff = Capture(camera, target, spriteRenderer, current, true, false,
                    Path.Combine(output, "p6_2d_current_off.png"));
                var frozenOff = Capture(camera, target, spriteRenderer, frozen, true, false,
                    Path.Combine(output, "p6_2d_frozen_off.png"));
                var currentOn = Capture(camera, target, spriteRenderer, current, true, true,
                    Path.Combine(output, "p6_2d_current_on.png"));
                var frozenOn = Capture(camera, target, spriteRenderer, frozen, true, true,
                    Path.Combine(output, "p6_2d_frozen_on.png"));

                int emptyCurrentOff = CountDifferent(empty, currentOff);
                int emptyFrozenOff = CountDifferent(empty, frozenOff);
                int offAB = CountDifferent(currentOff, frozenOff);
                int onAB = CountDifferent(currentOn, frozenOn);
                int currentVisible = CountDifferent(empty, currentOn);
                int frozenVisible = CountDifferent(empty, frozenOn);
                string metricsPath = Path.Combine(output, "p6_2d_metrics.txt");
                File.WriteAllText(metricsPath,
                    "P6 real Renderer2D/SpriteRenderer Universal2D 128x128 RGBA32\n" +
                    "emptyCurrentOff=" + emptyCurrentOff + ", emptyFrozenOff=" + emptyFrozenOff +
                    ", offAB=" + offAB + ", onAB=" + onAB +
                    ", currentVisible=" + currentVisible + ", frozenVisible=" + frozenVisible +
                    ", emptyCenter=" + Center(empty) + ", currentOnCenter=" + Center(currentOn) + "\n");

                Assert.That(emptyCurrentOff, Is.Zero,
                    "Pass-off must not fall back to SRPDefaultUnlit or any other NBShader Pass.");
                Assert.That(emptyFrozenOff, Is.Zero);
                Assert.That(offAB, Is.Zero);
                Assert.That(onAB, Is.Zero,
                    "Real 2D Renderer Universal2D current/frozen A/B differs.");
                Assert.That(currentVisible, Is.GreaterThan(100),
                    "Current Universal2D must visibly draw the SpriteRenderer.");
                Assert.That(frozenVisible, Is.GreaterThan(100),
                    "Frozen Universal2D must visibly draw the SpriteRenderer.");
                Assert.That(currentOn[Size * Size / 2 + Size / 2].r, Is.GreaterThan(180),
                    "The center must show the controlled red Sprite, not just a clear-color difference.");

                current.SetColor("_BaseColor", Color.green);
                frozen.SetColor("_BaseColor", Color.green);
                var currentGreen = Capture(camera, target, spriteRenderer, current, true, true,
                    Path.Combine(output, "p6_2d_current_green.png"));
                var frozenGreen = Capture(camera, target, spriteRenderer, frozen, true, true,
                    Path.Combine(output, "p6_2d_frozen_green.png"));
                int greenAB = CountDifferent(currentGreen, frozenGreen);
                int colorControl = CountDifferent(currentOn, currentGreen);
                File.AppendAllText(metricsPath, "greenAB=" + greenAB + ", redGreenControl=" + colorControl +
                    ", currentGreenCenter=" + Center(currentGreen) + "\n");
                Assert.That(greenAB, Is.Zero,
                    "The green positive-control state must also match Frozen exactly.");
                Assert.That(colorControl, Is.GreaterThan(100),
                    "The BaseColor control must change visible Sprite pixels.");
                Debug.Log("NBFX-G2-P6: real Renderer2D/SpriteRenderer exact Universal2D Pass; " +
                    "empty/off current/frozen = 0 difference, on/green A/B = 0 difference, " +
                    "on visible and red/green control positive. UI, SpriteMask, 2D Light and Player not covered.");
            }
            finally
            {
                QualitySettings.renderPipeline = priorQuality;
                RenderTexture.active = priorRenderTexture;
                if (cameraObject != null)
                {
                    var camera = cameraObject.GetComponent<Camera>();
                    if (camera != null) camera.targetTexture = null;
                    UnityEngine.Object.DestroyImmediate(cameraObject);
                }
                if (spriteObject != null) UnityEngine.Object.DestroyImmediate(spriteObject);
                if (previewScene.IsValid()) EditorSceneManager.ClosePreviewScene(previewScene);
                if (target != null)
                {
                    target.Release();
                    UnityEngine.Object.DestroyImmediate(target);
                }
                if (sprite != null) UnityEngine.Object.DestroyImmediate(sprite);
                if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
                if (current != null) UnityEngine.Object.DestroyImmediate(current);
                if (frozen != null) UnityEngine.Object.DestroyImmediate(frozen);
                if (pipeline2D != null) UnityEngine.Object.DestroyImmediate(pipeline2D);
                if (renderer2DData != null) UnityEngine.Object.DestroyImmediate(renderer2DData);

                Assert.That(QualitySettings.renderPipeline, Is.SameAs(priorQuality),
                    "Restore the active QualitySettings pipeline override.");
                Assert.That(GraphicsSettings.defaultRenderPipeline, Is.SameAs(priorGraphics));
                Assert.That(GraphicsSettings.currentRenderPipeline, Is.SameAs(priorActive));
                Assert.That(SceneManager.GetActiveScene().handle, Is.EqualTo(priorScene.handle));
                Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(priorScenePath));
                Assert.That(SceneManager.GetActiveScene().isDirty, Is.EqualTo(priorSceneDirty));
                Assert.That(priorURP.rendererDataList[0], Is.SameAs(priorRenderer));
                Assert.That(EditorUtility.IsDirty(priorURP), Is.EqualTo(priorURPDirty));
                Assert.That(EditorUtility.IsDirty(priorRenderer), Is.EqualTo(priorRendererDirty));
                AssertProtectedHashes(fileHashes);
                Debug.Log("NBFX-G2-P6 protected after: " + DescribeHashes(fileHashes));
            }
        }

        private static void ConfigureMaterial(Material material)
        {
            material.DisableKeyword("_FRESNEL");
            material.DisableKeyword("_NORMALMAP");
            material.DisableKeyword("_PARCUSTOMDATA_ON");
            material.DisableKeyword("_SPECULAR_COLOR");
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_FX_LIGHT_MODE_UNLIT");
            material.SetColor("_BaseColor", Color.red);
            material.SetFloat("_BaseColorIntensityForTimeline", 1);
            material.SetFloat("_AlphaAll", 1);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.SetFloat("_ZWrite", 0);
            material.SetFloat("_ZTest", (float)CompareFunction.LessEqual);
            material.SetFloat("_SrcBlend", (float)BlendMode.One);
            material.SetFloat("_DstBlend", (float)BlendMode.Zero);
            material.renderQueue = 3000;
            foreach (var pass in new[]
                     {
                         "SRPDefaultUnlit", "SRPDEFAULTUNLIT", "UniversalForward", "DepthOnly",
                         "ShadowCaster", "NBCameraOpaqueDistortPass", "NBDeferredDistortPass", PassName
                     })
                material.SetShaderPassEnabled(pass, false);
            Assert.That(material.GetShaderPassEnabled("SRPDefaultUnlit"), Is.False);
            Assert.That(material.GetShaderPassEnabled("SRPDEFAULTUNLIT"), Is.False);
        }

        private static void VerifyExactPass(Material material)
        {
            var index = material.FindPass(PassName);
            Assert.That(index, Is.GreaterThanOrEqualTo(0), material.shader.name + " lost Universal2D.");
            var tag = material.shader.FindPassTagValue(0, index, new ShaderTagId("LightMode"));
            Assert.That(tag.name, Is.EqualTo(PassName), material.shader.name + " changed Universal2D LightMode.");
        }

        private static Color32[] Capture(Camera camera, RenderTexture target, SpriteRenderer renderer,
            Material material, bool visible, bool passEnabled, string path)
        {
            renderer.sharedMaterial = material;
            renderer.enabled = visible;
            material.SetShaderPassEnabled(PassName, passEnabled);
            Assert.That(material.GetShaderPassEnabled(PassName), Is.EqualTo(passEnabled));
            camera.Render();
            var previousActive = RenderTexture.active;
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
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(readback);
            }
        }

        private static int CountDifferent(Color32[] left, Color32[] right)
        {
            Assert.That(right.Length, Is.EqualTo(left.Length));
            var count = 0;
            for (var i = 0; i < left.Length; i++)
                if (left[i].r != right[i].r || left[i].g != right[i].g ||
                    left[i].b != right[i].b || left[i].a != right[i].a)
                    count++;
            return count;
        }

        private static string Center(Color32[] pixels)
        {
            var p = pixels[(Size / 2) * Size + Size / 2];
            return "(" + p.r + "," + p.g + "," + p.b + "," + p.a + ")";
        }

        private static Dictionary<string, string> CaptureProtectedHashes(params string[] extraPaths)
        {
            var paths = new HashSet<string>(ProtectedPaths);
            foreach (var path in extraPaths)
                if (!string.IsNullOrEmpty(path)) paths.Add(path);
            var hashes = new Dictionary<string, string>();
            var root = Path.GetDirectoryName(Application.dataPath);
            foreach (var path in paths)
            {
                var fullPath = Path.Combine(root, path);
                Assert.That(File.Exists(fullPath), Is.True, "Protected file missing: " + path);
                hashes.Add(path, HashFile(fullPath));
            }
            return hashes;
        }

        private static string HashFile(string path)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }

        private static void AssertProtectedHashes(Dictionary<string, string> hashes)
        {
            var root = Path.GetDirectoryName(Application.dataPath);
            foreach (var entry in hashes)
                Assert.That(HashFile(Path.Combine(root, entry.Key)), Is.EqualTo(entry.Value),
                    "P6 changed protected file: " + entry.Key);
        }

        private static string DescribeHashes(Dictionary<string, string> hashes)
        {
            var text = new StringBuilder();
            foreach (var entry in hashes)
                text.Append(entry.Key).Append('=').Append(entry.Value).Append(';');
            return text.ToString();
        }
    }
}
