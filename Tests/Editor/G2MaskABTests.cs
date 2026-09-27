using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace NBFX.Baseline.Tests
{
    public sealed class G2MaskABTests
    {
        private const string OriginalPath = "Assets/NBShaderSamples/NBShaderSamples/UnLit.mat";
        private const string FrozenPath = "Assets/NBShaderSamples/NBShader_VFXGraphValidation/Materials/UnLit.mat";
        private const int Size = 96;
        private const int Layer = 2;

        [Test]
        public void MaskCoverageAndRefineMatchFrozenWithVisibleControls()
        {
            var source = AssetDatabase.LoadAssetAtPath<Material>(OriginalPath);
            var frozen = AssetDatabase.LoadAssetAtPath<Material>(FrozenPath);
            Assert.That(source, Is.Not.Null);
            Assert.That(frozen, Is.Not.Null);
            var currentMaterial = new Material(source);
            var frozenMaterial = new Material(frozen);
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false, false);
            var colors = new Color32[32 * 32];
            for (var y = 0; y < 32; y++)
                for (var x = 0; x < 32; x++)
                    colors[y * 32 + x] = new Color32(255, 255, 255, (byte)(x * 8));
            texture.SetPixels32(colors);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.Apply(false, false);
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var cameraObject = new GameObject("NBFX_G2_MaskCamera");
            var camera = cameraObject.AddComponent<Camera>();
            var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
            var previousActive = RenderTexture.active;
            try
            {
                foreach (var material in new[] { currentMaterial, frozenMaterial })
                {
                    material.SetColor("_BaseColor", Color.red);
                    material.SetFloat("_Cull", 0);
                    material.SetTexture("_MaskMap", texture);
                    material.SetVector("_MaskMap_ST", new Vector4(1, 1, 0, 0));
                    material.SetInteger("_W9ParticleShaderColorChannelFlag", 3 << 2);
                    material.SetInteger("_W9ParticleShaderFlags1", 1);
                    material.SetVector("_MaskRefineVec", new Vector4(1, 1, 0, 0));
                    material.SetVector("_MaskMapVec", new Vector4(1, 0, 0, 0));
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

                currentMaterial.DisableKeyword("_MASKMAP_ON");
                frozenMaterial.DisableKeyword("_MASKMAP_ON");
                var withoutMask = Capture(camera, renderer, currentMaterial, target);
                Assert.That(CountDifferent(withoutMask, Capture(camera, renderer, frozenMaterial, target)), Is.Zero);
                currentMaterial.EnableKeyword("_MASKMAP_ON");
                frozenMaterial.EnableKeyword("_MASKMAP_ON");
                Capture(camera, renderer, currentMaterial, target);
                Capture(camera, renderer, frozenMaterial, target);

                Color32[] unrefined = null;
                for (var mode = 0; mode < 4; mode++)
                {
                    foreach (var material in new[] { currentMaterial, frozenMaterial })
                    {
                        material.SetInteger("_W9ParticleShaderFlags1", 1 | (mode >= 2 ? 1 << 7 : 0));
                        material.SetVector("_MaskRefineVec", mode >= 2
                            ? new Vector4(2, 0.6f, 0.1f, 0) : new Vector4(1, 1, 0, 0));
                        material.SetVector("_MaskMapVec", new Vector4(mode == 0 ? 0 : mode == 3 ? 0.5f : 1, 0, 0, 0));
                    }
                    var current = Capture(camera, renderer, currentMaterial, target);
                    var reference = Capture(camera, renderer, frozenMaterial, target);
                    Assert.That(CountDifferent(current, reference), Is.Zero,
                        "Mask mode " + mode + " differs from frozen ShaderLab.");
                    if (mode == 0)
                        Assert.That(CountDifferent(withoutMask, current), Is.Zero,
                            "Zero overall strength must be a neutral mask.");
                    if (mode == 1)
                    {
                        unrefined = current;
                        Assert.That(CountDifferent(withoutMask, current), Is.GreaterThan(0),
                            "Mask sampling must visibly affect the image.");
                    }
                    if (mode == 2)
                        Assert.That(CountDifferent(unrefined, current), Is.GreaterThan(0),
                            "Refine pow/mul/add must visibly affect the image.");
                }
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previousActive;
                target.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(quad);
                Object.DestroyImmediate(texture);
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
    }
}
