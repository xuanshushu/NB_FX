using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace NBFX.Baseline.Tests
{
    /// <summary>Same-position, in-memory A/B for individual shared-HLSL slices.</summary>
    public sealed class G2SharedSliceTests
    {
        private const string OriginalPath = "Assets/NBShaderSamples/NBShaderSamples/UnLit.mat";
        private const string FrozenPath = "Assets/NBShaderSamples/NBShader_VFXGraphValidation/Materials/UnLit.mat";
        private const int Size = 64;
        private const int Layer = 2;

        [Test]
        public void BaseColorTimelineMatchesFrozenAndChangesPixels()
        {
            var source = AssetDatabase.LoadAssetAtPath<Material>(OriginalPath);
            var frozen = AssetDatabase.LoadAssetAtPath<Material>(FrozenPath);
            Assert.That(source, Is.Not.Null);
            Assert.That(frozen, Is.Not.Null);

            var currentMaterial = new Material(source);
            var frozenMaterial = new Material(frozen);
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var cameraObject = new GameObject("NBFX_G2_TemporaryCamera");
            var camera = cameraObject.AddComponent<Camera>();
            var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
            var previousActive = RenderTexture.active;
            try
            {
                foreach (var material in new[] { currentMaterial, frozenMaterial })
                {
                    material.SetColor("_BaseColor", new Color(0.8f, 0.4f, 0.2f, 0.7f));
                    material.SetFloat("_Cull", 0);
                    material.SetFloat("_BaseColorIntensityForTimeline", 1);
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

                var currentFull = Capture(camera, renderer, currentMaterial, target);
                var frozenFull = Capture(camera, renderer, frozenMaterial, target);
                Assert.That(CountDifferent(currentFull, frozenFull), Is.Zero, "Full-intensity A/B differs.");

                currentMaterial.SetFloat("_BaseColorIntensityForTimeline", 0.375f);
                frozenMaterial.SetFloat("_BaseColorIntensityForTimeline", 0.375f);
                var currentDim = Capture(camera, renderer, currentMaterial, target);
                var frozenDim = Capture(camera, renderer, frozenMaterial, target);
                Assert.That(CountDifferent(currentDim, frozenDim), Is.Zero, "Dimmed A/B differs.");
                Assert.That(CountDifferent(currentFull, currentDim), Is.GreaterThan(0),
                    "The timeline-intensity control must visibly exercise the new surface call.");
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previousActive;
                target.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(quad);
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
            finally
            {
                Object.DestroyImmediate(readback);
            }
        }

        private static int CountDifferent(Color32[] left, Color32[] right)
        {
            Assert.That(right.Length, Is.EqualTo(left.Length));
            var count = 0;
            for (var i = 0; i < left.Length; i++)
            {
                if (left[i].r != right[i].r || left[i].g != right[i].g ||
                    left[i].b != right[i].b || left[i].a != right[i].a)
                    count++;
            }
            return count;
        }
    }
}
