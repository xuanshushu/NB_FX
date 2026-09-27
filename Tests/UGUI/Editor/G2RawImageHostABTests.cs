using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NBFX.Baseline.Tests
{
    /// <summary>G2 A/B through an actual world-space Canvas, RawImage, and CanvasRenderer.</summary>
    public sealed class G2RawImageHostABTests
    {
        private const string OriginalPath = "Assets/NBShaderSamples/NBShaderSamples/UnLit.mat";
        private const string FrozenPath = "Assets/NBShaderSamples/NBShader_VFXGraphValidation/Materials/UnLit.mat";
        private const int Size = 96;
        private const int Layer = 2; // Included by the active HighFidelity renderer layer mask.

        [Test]
        public void WorldSpaceRawImageMatchesFrozenAndUsesGraphicMainTexture()
        {
            var source = AssetDatabase.LoadAssetAtPath<Material>(OriginalPath);
            var frozenSource = AssetDatabase.LoadAssetAtPath<Material>(FrozenPath);
            Assert.That(source, Is.Not.Null, OriginalPath);
            Assert.That(frozenSource, Is.Not.Null, FrozenPath);
            Assert.That(source.shader.name, Is.EqualTo("Effects/NBShader"));
            Assert.That(frozenSource.shader.name, Is.EqualTo("Effects/NBShader_T00_Frozen"));
            Assert.That(source.shader, Is.Not.SameAs(frozenSource.shader));

            var originalBytes = File.ReadAllBytes(AbsoluteAssetPath(OriginalPath));
            var frozenBytes = File.ReadAllBytes(AbsoluteAssetPath(FrozenPath));
            var evidence = new StringBuilder();
            var directory = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Library/NBFXG2");
            var previousActive = RenderTexture.active;
            Scene previewScene = default;
            Material current = null;
            Material frozen = null;
            Texture2D redTexture = null;
            Texture2D greenTexture = null;
            RenderTexture target = null;
            GameObject cameraObject = null;
            GameObject canvasObject = null;
            GameObject imageObject = null;
            Camera camera = null;
            Canvas canvas = null;
            RawImage image = null;
            try
            {
                current = new Material(source) { hideFlags = HideFlags.HideAndDontSave };
                frozen = new Material(frozenSource) { hideFlags = HideFlags.HideAndDontSave };
                Assert.That(AssetDatabase.Contains(current), Is.False);
                Assert.That(AssetDatabase.Contains(frozen), Is.False);
                ConfigureUIRawImage(current);
                ConfigureUIRawImage(frozen);
                Assert.That(current.GetShaderPassEnabled("UniversalForward"), Is.True);
                Assert.That(frozen.GetShaderPassEnabled("UniversalForward"), Is.True);
                Assert.That(current.renderQueue, Is.EqualTo(frozen.renderQueue));

                redTexture = MakeSolidTexture(new Color32(220, 40, 40, 255));
                greenTexture = MakeSolidTexture(new Color32(40, 220, 40, 255));
                target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32)
                {
                    filterMode = FilterMode.Point
                };
                target.Create();
                previewScene = EditorSceneManager.NewPreviewScene();

                cameraObject = EditorUtility.CreateGameObjectWithHideFlags(
                    "NBFX_G2_UGUI_Camera", HideFlags.HideAndDontSave, typeof(Camera));
                SceneManager.MoveGameObjectToScene(cameraObject, previewScene);
                camera = cameraObject.GetComponent<Camera>();
                camera.scene = previewScene;
                camera.cameraType = CameraType.Game;
                camera.enabled = false;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.125f, .25f, .375f, 1f);
                camera.orthographic = true;
                camera.orthographicSize = 1f;
                camera.nearClipPlane = .1f;
                camera.farClipPlane = 10f;
                camera.allowHDR = false;
                camera.allowMSAA = false;
                camera.cullingMask = 1 << Layer;
                camera.transform.position = new Vector3(0, 0, 3);
                camera.transform.rotation = Quaternion.Euler(0, 180, 0);
                camera.targetTexture = target;

                canvasObject = EditorUtility.CreateGameObjectWithHideFlags(
                    "NBFX_G2_WorldCanvas", HideFlags.HideAndDontSave,
                    typeof(RectTransform), typeof(Canvas));
                SceneManager.MoveGameObjectToScene(canvasObject, previewScene);
                canvasObject.layer = Layer;
                canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = camera;
                var canvasRect = canvasObject.GetComponent<RectTransform>();
                canvasRect.position = new Vector3(0, 0, 2);
                canvasRect.rotation = Quaternion.identity;
                canvasRect.localScale = Vector3.one * .01f;
                canvasRect.sizeDelta = new Vector2(200, 200);

                Directory.CreateDirectory(directory);
                evidence.AppendLine("host=WorldSpace Canvas + RawImage + CanvasRenderer; previewScene=true; layer=2");
                evidence.AppendLine("shaders=" + current.shader.name + " / " + frozen.shader.name +
                    "; queue=" + current.renderQueue + "; size=" + Size + "x" + Size + " RGBA32");
                var empty = Capture(camera, target, directory, "G2RawImage-empty.png");

                imageObject = EditorUtility.CreateGameObjectWithHideFlags(
                    "NBFX_G2_RawImage", HideFlags.HideAndDontSave,
                    typeof(RectTransform), typeof(RawImage));
                SceneManager.MoveGameObjectToScene(imageObject, previewScene);
                imageObject.transform.SetParent(canvasRect, false);
                imageObject.layer = Layer;
                var imageRect = imageObject.GetComponent<RectTransform>();
                imageRect.anchorMin = imageRect.anchorMax = new Vector2(.5f, .5f);
                imageRect.pivot = new Vector2(.5f, .5f);
                imageRect.sizeDelta = new Vector2(120, 120);
                imageRect.anchoredPosition = Vector2.zero;
                image = imageObject.GetComponent<RawImage>();
                Assert.That(image.canvasRenderer, Is.Not.Null, "The real UGUI CanvasRenderer is required.");
                image.color = Color.white;
                image.texture = redTexture;
                image.uvRect = new Rect(0, 0, 1, 1);

                // Built-in UI material proves this Canvas/camera/preview scene actually draws UGUI.
                image.material = null;
                var positive = Capture(camera, target, directory, "G2RawImage-ui-default.png");
                image.enabled = false;
                var disabled = Capture(camera, target, directory, "G2RawImage-disabled.png");
                image.enabled = true;

                var currentRed = CaptureMaterial(image, current, redTexture, camera, target,
                    directory, "G2RawImage-red-current.png");
                var frozenRed = CaptureMaterial(image, frozen, redTexture, camera, target,
                    directory, "G2RawImage-red-frozen.png");
                var currentGreen = CaptureMaterial(image, current, greenTexture, camera, target,
                    directory, "G2RawImage-green-current.png");
                var frozenGreen = CaptureMaterial(image, frozen, greenTexture, camera, target,
                    directory, "G2RawImage-green-frozen.png");

                var emptyDisabled = CountDifferent(empty, disabled);
                var positiveVisible = CountDifferent(empty, positive);
                var redAB = CountDifferent(currentRed, frozenRed);
                var greenAB = CountDifferent(currentGreen, frozenGreen);
                var currentVisible = CountDifferent(empty, currentRed);
                var frozenVisible = CountDifferent(empty, frozenRed);
                var currentTextureChange = CountDifferent(currentRed, currentGreen);
                var frozenTextureChange = CountDifferent(frozenRed, frozenGreen);
                Record(evidence, "emptyDisabled=" + emptyDisabled +
                    "; uiDefaultVisible=" + positiveVisible +
                    "; redAB=" + redAB + "; greenAB=" + greenAB +
                    "; currentVisible=" + currentVisible + "; frozenVisible=" + frozenVisible +
                    "; currentRedGreen=" + currentTextureChange +
                    "; frozenRedGreen=" + frozenTextureChange);

                Assert.That(emptyDisabled, Is.Zero, "A disabled RawImage must leave the Canvas empty.");
                Assert.That(positiveVisible, Is.GreaterThan(100), "Built-in UI/Default positive control did not draw.");
                Assert.That(currentVisible, Is.GreaterThan(100), "Current UIEffect RawImage is blank.");
                Assert.That(frozenVisible, Is.GreaterThan(100), "Frozen UIEffect RawImage is blank.");
                Assert.That(redAB, Is.Zero, "Current and G0 Frozen red RawImage pixels differ.");
                Assert.That(greenAB, Is.Zero, "Current and G0 Frozen green RawImage pixels differ.");
                Assert.That(currentTextureChange, Is.GreaterThan(100), "Current shader ignored RawImage.texture.");
                Assert.That(frozenTextureChange, Is.GreaterThan(100), "Frozen shader ignored RawImage.texture.");
            }
            finally
            {
                try
                {
                    Directory.CreateDirectory(directory);
                    File.WriteAllText(Path.Combine(directory, "G2RawImage-AB.txt"),
                        evidence.ToString(), new UTF8Encoding(false));
                }
                finally
                {
                    if (image != null) image.material = null;
                    if (canvas != null) canvas.worldCamera = null;
                    if (camera != null) camera.targetTexture = null;
                    RenderTexture.active = previousActive;
                    if (imageObject != null) UnityEngine.Object.DestroyImmediate(imageObject);
                    if (canvasObject != null) UnityEngine.Object.DestroyImmediate(canvasObject);
                    if (cameraObject != null) UnityEngine.Object.DestroyImmediate(cameraObject);
                    if (previewScene.IsValid()) EditorSceneManager.ClosePreviewScene(previewScene);
                    if (target != null)
                    {
                        target.Release();
                        UnityEngine.Object.DestroyImmediate(target);
                    }
                    if (redTexture != null) UnityEngine.Object.DestroyImmediate(redTexture);
                    if (greenTexture != null) UnityEngine.Object.DestroyImmediate(greenTexture);
                    if (current != null) UnityEngine.Object.DestroyImmediate(current);
                    if (frozen != null) UnityEngine.Object.DestroyImmediate(frozen);
                    Assert.That(File.ReadAllBytes(AbsoluteAssetPath(OriginalPath)), Is.EqualTo(originalBytes),
                        "The original material asset changed during this transient GPU test.");
                    Assert.That(File.ReadAllBytes(AbsoluteAssetPath(FrozenPath)), Is.EqualTo(frozenBytes),
                        "The G0 frozen material asset changed during this transient GPU test.");
                }
            }
        }

        private static void ConfigureUIRawImage(Material material)
        {
            // Clone-only normalization: bit 14 selects UIEffect, and Flags1 bit 0 selects
            // transparent mode. Sprite/BaseMap bits remain clear, so UGUI's _MainTex is sampled.
            material.shaderKeywords = new[] { "_FX_LIGHT_MODE_UNLIT" };
            material.SetFloat("_MeshSourceMode", 2f);
            material.SetFloat("_UIEffect_Toggle", 1f);
            material.SetFloat("_TransparentMode", 1f);
            material.SetInteger("_W9ParticleShaderFlags", 1 << 14);
            material.SetInteger("_W9ParticleShaderFlags1", 1);
            material.SetInteger("_W9ParticleShaderColorChannelFlag", 3);
            material.SetInteger("_UVModeFlag0", 0);
            material.SetInteger("_UVModeFlagType0", 0);
            material.SetVector("_UI_MainTex_ST", new Vector4(1, 1, 0, 0));
            material.SetTexture("_BaseMap", Texture2D.whiteTexture);
            material.SetVector("_BaseMap_ST", new Vector4(1, 1, 0, 0));
            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_Color", Color.white);
            material.SetColor("_ColorA", Color.white);
            material.SetFloat("_BaseColorIntensityForTimeline", 1f);
            material.SetFloat("_AlphaAll", 1f);
            material.SetFloat("_fogintensity", 0f);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.SetFloat("_ZTest", (float)CompareFunction.LessEqual);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ColorMask", 15f);
            material.renderQueue = 3000;
        }

        private static Texture2D MakeSolidTexture(Color32 color)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixels32(new[] { color, color, color, color });
            texture.Apply(false, false);
            return texture;
        }

        private static Color32[] CaptureMaterial(RawImage image, Material material, Texture2D texture,
            Camera camera, RenderTexture target, string directory, string fileName)
        {
            image.material = material;
            image.texture = texture;
            var pixels = Capture(camera, target, directory, fileName);
            var renderedMaterial = image.canvasRenderer.GetMaterial(0);
            Assert.That(renderedMaterial, Is.Not.Null, "RawImage did not submit a CanvasRenderer material.");
            Assert.That(renderedMaterial.shader, Is.SameAs(material.shader),
                "CanvasRenderer did not use the requested A/B shader.");
            return pixels;
        }

        private static Color32[] Capture(Camera camera, RenderTexture target, string directory, string fileName)
        {
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var previous = RenderTexture.active;
            var readback = new Texture2D(Size, Size, TextureFormat.RGBA32, false, false);
            try
            {
                RenderTexture.active = target;
                readback.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
                readback.Apply(false, false);
                File.WriteAllBytes(Path.Combine(directory, fileName), readback.EncodeToPNG());
                return readback.GetPixels32();
            }
            finally
            {
                RenderTexture.active = previous;
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

        private static string AbsoluteAssetPath(string path)
        {
            return Path.Combine(Path.GetDirectoryName(Application.dataPath),
                path.Replace('/', Path.DirectorySeparatorChar));
        }

        private static void Record(StringBuilder evidence, string line)
        {
            TestContext.WriteLine(line);
            evidence.AppendLine(line);
        }
    }
}
