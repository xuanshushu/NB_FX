using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace NBFX.Baseline.Tests
{
    public sealed class G2DissolveABTests
    {
        private const string OriginalPath = "Assets/NBShaderSamples/NBShaderSamples/UnLit.mat";
        private const string FrozenPath = "Assets/NBShaderSamples/NBShader_VFXGraphValidation/Materials/UnLit.mat";
        private const int Size = 96;
        private const int Layer = 2;

        [Test]
        public void DissolveMaskModesAndDebugBoundaryMatchFrozen()
        {
            var source = AssetDatabase.LoadAssetAtPath<Material>(OriginalPath);
            var frozen = AssetDatabase.LoadAssetAtPath<Material>(FrozenPath);
            Assert.That(source, Is.Not.Null);
            Assert.That(frozen, Is.Not.Null);
            var currentMaterial = new Material(source);
            var frozenMaterial = new Material(frozen);
            var dissolveTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false, false);
            var maskTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false, false);
            var dissolvePixels = new Color32[32 * 32];
            var maskPixels = new Color32[32 * 32];
            for (var y = 0; y < 32; y++)
                for (var x = 0; x < 32; x++)
                {
                    dissolvePixels[y * 32 + x] = new Color32(255, 255, 255, (byte)(x * 8));
                    maskPixels[y * 32 + x] = new Color32(255, 255, 255, (byte)(y * 8));
                }
            dissolveTexture.SetPixels32(dissolvePixels);
            maskTexture.SetPixels32(maskPixels);
            dissolveTexture.filterMode = FilterMode.Point;
            maskTexture.filterMode = FilterMode.Point;
            dissolveTexture.wrapMode = TextureWrapMode.Clamp;
            maskTexture.wrapMode = TextureWrapMode.Clamp;
            dissolveTexture.Apply(false, false);
            maskTexture.Apply(false, false);
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var cameraObject = new GameObject("NBFX_G2_DissolveCamera");
            var camera = cameraObject.AddComponent<Camera>();
            var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
            var previousActive = RenderTexture.active;
            try
            {
                foreach (var material in new[] { currentMaterial, frozenMaterial })
                {
                    material.SetColor("_BaseColor", Color.red);
                    material.SetFloat("_Cull", 0);
                    material.SetTexture("_DissolveMap", dissolveTexture);
                    material.SetTexture("_DissolveMaskMap", maskTexture);
                    material.SetInteger("_W9ParticleShaderColorChannelFlag", (3 << 10) | (3 << 12));
                    material.SetVector("_Dissolve", new Vector4(0.5f, 1.5f, 0.6f, 0.2f));
                    material.SetVector("_DissolveOffsetRotateDistort", Vector4.zero);
                }
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

                currentMaterial.DisableKeyword("_DISSOLVE");
                frozenMaterial.DisableKeyword("_DISSOLVE");
                var without = Capture(camera, renderer, currentMaterial, target);
                Assert.That(CountDifferent(without, Capture(camera, renderer, frozenMaterial, target)), Is.Zero);
                currentMaterial.EnableKeyword("_DISSOLVE");
                frozenMaterial.EnableKeyword("_DISSOLVE");
                Capture(camera, renderer, currentMaterial, target);
                Capture(camera, renderer, frozenMaterial, target);

                Color32[] noMask = null;
                Color32[] noMaskFrozen = null;
                foreach (var mode in new[] { -1f, 0f, 0.5f, 1f })
                {
                    foreach (var material in new[] { currentMaterial, frozenMaterial })
                    {
                        if (mode < 0) material.DisableKeyword("_DISSOLVE_MASK");
                        else material.EnableKeyword("_DISSOLVE_MASK");
                        material.SetFloat("_DissolveMaskMode", mode);
                    }
                    var current = Capture(camera, renderer, currentMaterial, target);
                    var reference = Capture(camera, renderer, frozenMaterial, target);
                    var different = CountDifferent(current, reference);
                    if (different != 0)
                    {
                        var output = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/NBFXG2");
                        Directory.CreateDirectory(output);
                        var stem = "dissolve_diff_mode" + mode;
                        var currentRepeat = Capture(camera, renderer, currentMaterial, target);
                        var frozenRepeat = Capture(camera, renderer, frozenMaterial, target);
                        var frozenFirst = Capture(camera, renderer, frozenMaterial, target);
                        var currentSecond = Capture(camera, renderer, currentMaterial, target);
                        WritePng(Path.Combine(output, stem + "_current.png"), current);
                        WritePng(Path.Combine(output, stem + "_frozen.png"), reference);
                        WritePng(Path.Combine(output, stem + "_current_repeat.png"), currentRepeat);
                        WritePng(Path.Combine(output, stem + "_frozen_repeat.png"), frozenRepeat);
                        WritePng(Path.Combine(output, stem + "_no_mask_current.png"), noMask);
                        WritePng(Path.Combine(output, stem + "_no_mask_frozen.png"), noMaskFrozen);
                        var metrics = "mode=" + mode + ", firstAB=" + different +
                            ", firstDifference=" + FirstDifference(current, reference) +
                            ", currentVsNoMask=" + (noMask == null ? -1 : CountDifferent(current, noMask)) +
                            ", frozenVsNoMask=" + (noMaskFrozen == null ? -1 : CountDifferent(reference, noMaskFrozen)) +
                            ", repeatAB=" + CountDifferent(currentRepeat, frozenRepeat) +
                            ", currentSelf=" + CountDifferent(current, currentRepeat) +
                            ", frozenSelf=" + CountDifferent(reference, frozenRepeat) +
                            ", reverseAB=" + CountDifferent(currentSecond, frozenFirst) +
                            ", currentMaskKeyword=" + currentMaterial.IsKeywordEnabled("_DISSOLVE_MASK") +
                            ", frozenMaskKeyword=" + frozenMaterial.IsKeywordEnabled("_DISSOLVE_MASK");
                        File.WriteAllText(Path.Combine(output, stem + "_metrics.txt"), metrics + "\n");
                        Assert.Fail("Dissolve mask differs from frozen ShaderLab: " + metrics);
                    }
                    if (mode < 0)
                    {
                        noMask = current;
                        noMaskFrozen = reference;
                        Assert.That(CountDifferent(without, current), Is.GreaterThan(0),
                            "Dissolve must visibly affect the image.");
                    }
                    if (mode == 0.5f)
                        Assert.That(CountDifferent(noMask, current), Is.Zero,
                            "Exactly 0.5 must enter neither mask branch.");
                    if (mode == 0f || mode == 1f)
                        Assert.That(CountDifferent(noMask, current), Is.GreaterThan(0),
                            "Early/late mask mode must visibly affect the image.");
                }

                foreach (var material in new[] { currentMaterial, frozenMaterial })
                {
                    material.EnableKeyword("NB_DEBUG_DISSOLVE");
                    material.SetVector("_Dissolve", new Vector4(0.5f, 1.5f, 0.6f, 0));
                    material.SetFloat("_DissolveMaskMode", 0);
                }
                var debug = Capture(camera, renderer, currentMaterial, target);
                var frozenDebug = Capture(camera, renderer, frozenMaterial, target);
                Assert.That(CountDifferent(debug, frozenDebug), Is.Zero,
                    "Debug must return before softWidth division, even when width is zero.");
                Assert.That(CountDifferent(without, debug), Is.GreaterThan(0));
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previousActive;
                target.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(quad);
                Object.DestroyImmediate(dissolveTexture);
                Object.DestroyImmediate(maskTexture);
                Object.DestroyImmediate(currentMaterial);
                Object.DestroyImmediate(frozenMaterial);
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
            finally { Object.DestroyImmediate(readback); }
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

        private static string FirstDifference(Color32[] left, Color32[] right)
        {
            for (var i = 0; i < left.Length; i++)
                if (left[i].r != right[i].r || left[i].g != right[i].g ||
                    left[i].b != right[i].b || left[i].a != right[i].a)
                    return "(" + i % Size + "," + i / Size + ") current=" + left[i] + " frozen=" + right[i];
            return "none";
        }

        private static void WritePng(string path, Color32[] pixels)
        {
            if (pixels == null) return;
            var image = new Texture2D(Size, Size, TextureFormat.RGBA32, false, false);
            try
            {
                image.SetPixels32(pixels);
                image.Apply(false);
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally { Object.DestroyImmediate(image); }
        }
    }
}
