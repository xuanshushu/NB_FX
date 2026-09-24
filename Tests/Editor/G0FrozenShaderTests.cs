using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace NBFX.Baseline.Tests
{
    /// <summary>Test-only, same-position ShaderLab baseline calibration. Never edits source assets.</summary>
    public sealed class G0FrozenShaderTests
    {
        private const string OriginalMaterialPath = "Assets/NBShaderSamples/NBShaderSamples/UnLit.mat";
        private const string FrozenMaterialPath = "Assets/NBShaderSamples/NBShader_VFXGraphValidation/Materials/UnLit.mat";
        private const int Size = 128;
        // Renderer mask 55 includes layer 2; the source sample scene uses only layers 0 and 5.
        private const int Layer = 2;

        [Test]
        public void FrozenShaderIsDistinctAndSupported()
        {
            var original = Shader.Find("Effects/NBShader");
            var frozen = Shader.Find("Effects/NBShader_T00_Frozen");
            Assert.That(original, Is.Not.Null);
            Assert.That(frozen, Is.Not.Null);
            Assert.That(frozen, Is.Not.SameAs(original));
            Assert.That(original.isSupported && frozen.isSupported, Is.True);
            Assert.That(ShaderUtil.GetShaderMessages(frozen), Has.None.Matches<ShaderMessage>(m => m.severity.ToString() == "Error"));
        }

        [Test]
        public void OriginalAndFrozenRenderEqualAndInjectedDifferenceFails()
        {
            var original = AssetDatabase.LoadAssetAtPath<Material>(OriginalMaterialPath);
            var frozen = AssetDatabase.LoadAssetAtPath<Material>(FrozenMaterialPath);
            Assert.That(original, Is.Not.Null);
            Assert.That(frozen, Is.Not.Null);
            Assert.That(original.renderQueue, Is.EqualTo(frozen.renderQueue));

            var originalCopy = new Material(original);
            var frozenCopy = new Material(frozen);
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var cameraObject = new GameObject("NBFX_G0_TemporaryCamera");
            var camera = cameraObject.AddComponent<Camera>();
            var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
            var previousActive = RenderTexture.active;
            try
            {
                // Controlled property overrides are identical for both candidates and never saved.
                originalCopy.SetColor("_BaseColor", Color.red);
                frozenCopy.SetColor("_BaseColor", Color.red);
                originalCopy.SetFloat("_Cull", 0);
                frozenCopy.SetFloat("_Cull", 0);
                quad.layer = Layer;
                quad.transform.position = new Vector3(0, 0, 2);
                var renderer = quad.GetComponent<MeshRenderer>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.125f, 0.25f, 0.375f, 1);
                camera.orthographic = true;
                camera.orthographicSize = 1;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 10;
                camera.allowHDR = false;
                camera.allowMSAA = false;
                camera.transform.position = new Vector3(0, 0, 3);
                camera.transform.rotation = Quaternion.Euler(0, 180, 0);
                camera.cullingMask = 1 << Layer;
                camera.targetTexture = target;

                var reference = Capture(camera, renderer, originalCopy, target);
                var candidate = Capture(camera, renderer, frozenCopy, target);
                var center = (Size / 2) * Size + Size / 2;
                Assert.That(reference[center].r, Is.GreaterThan(200), "The shader test object must actually draw.");
                Assert.That(reference[center].g, Is.LessThan(32));
                Assert.That(CountDifferent(reference, candidate, out var maxError), Is.Zero,
                    "Frozen shader differs in the controlled central object capture; maximum channel error " + maxError);

                var injected = (Color32[])candidate.Clone();
                injected[center].r = (byte)(injected[center].r - 1);
                Assert.That(CountDifferent(reference, injected, out maxError), Is.EqualTo(1),
                    "The one-pixel, one-level negative control must fail the zero-tolerance comparator.");
                Assert.That(maxError, Is.EqualTo(1));

                var output = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/NBFXG0");
                Directory.CreateDirectory(output);
                WritePng(output, "G0_reference.png", reference);
                WritePng(output, "G0_frozen.png", candidate);
                WritePng(output, "G0_diff.png", Difference(reference, candidate));
                WritePng(output, "G0_negative_control.png", injected);
                Debug.Log("NBFX-G0: 128x128 RGBA32, red test object confirmed at center; full image has 0 differing pixels at exact threshold; injected one-level red-channel difference detected at one pixel. Output: " + output);
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previousActive;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(quad);
                UnityEngine.Object.DestroyImmediate(originalCopy);
                UnityEngine.Object.DestroyImmediate(frozenCopy);
            }
        }

        private static Color32[] Capture(Camera camera, MeshRenderer renderer, Material material, RenderTexture target)
        {
            renderer.sharedMaterial = material;
            camera.Render();
            RenderTexture.active = target;
            var readback = new Texture2D(Size, Size, TextureFormat.RGBA32, false, false);
            try
            {
                readback.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
                readback.Apply(false);
                return readback.GetPixels32();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(readback);
            }
        }

        private static int CountDifferent(Color32[] reference, Color32[] candidate, out int maxError)
        {
            Assert.That(candidate.Length, Is.EqualTo(reference.Length));
            var count = 0;
            maxError = 0;
            for (var i = 0; i < reference.Length; i++)
            {
                var error = Math.Max(Math.Max(Math.Abs(reference[i].r - candidate[i].r), Math.Abs(reference[i].g - candidate[i].g)),
                    Math.Max(Math.Abs(reference[i].b - candidate[i].b), Math.Abs(reference[i].a - candidate[i].a)));
                if (error == 0) continue;
                count++;
                maxError = Math.Max(maxError, error);
            }
            return count;
        }

        private static void WritePng(string directory, string name, Color32[] pixels)
        {
            var image = new Texture2D(Size, Size, TextureFormat.RGBA32, false, false);
            try
            {
                image.SetPixels32(pixels);
                image.Apply(false);
                File.WriteAllBytes(Path.Combine(directory, name), image.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(image);
            }
        }

        private static Color32[] Difference(Color32[] reference, Color32[] candidate)
        {
            var difference = new Color32[reference.Length];
            for (var i = 0; i < difference.Length; i++)
            {
                difference[i] = new Color32(
                    (byte)Math.Abs(reference[i].r - candidate[i].r),
                    (byte)Math.Abs(reference[i].g - candidate[i].g),
                    (byte)Math.Abs(reference[i].b - candidate[i].b),
                    255);
            }
            return difference;
        }
    }
}
