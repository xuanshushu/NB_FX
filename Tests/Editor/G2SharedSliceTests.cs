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

        [Test]
        public void VertexOffsetModesMatchFrozenAndMovePixels()
        {
            var source = AssetDatabase.LoadAssetAtPath<Material>(OriginalPath);
            var frozen = AssetDatabase.LoadAssetAtPath<Material>(FrozenPath);
            Assert.That(source, Is.Not.Null);
            Assert.That(frozen, Is.Not.Null);

            var currentMaterial = new Material(source);
            var frozenMaterial = new Material(frozen);
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var cameraObject = new GameObject("NBFX_G2_VertexOffsetCamera");
            var camera = cameraObject.AddComponent<Camera>();
            var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
            var previousActive = RenderTexture.active;
            try
            {
                foreach (var material in new[] { currentMaterial, frozenMaterial })
                {
                    material.SetColor("_BaseColor", Color.red);
                    material.SetFloat("_Cull", 0);
                    material.EnableKeyword("_VERTEX_OFFSET");
                    material.SetTexture("_VertexOffset_Map", Texture2D.whiteTexture);
                    material.SetVector("_VertexOffset_Map_ST", new Vector4(1, 1, 0, 0));
                    material.SetVector("_VertexOffset_CustomDir", new Vector4(1, 0, 0, 0));
                }

                quad.layer = Layer;
                quad.transform.position = new Vector3(0, 0, 2);
                quad.transform.rotation = Quaternion.Euler(0, 35, 0);
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

                for (var mode = 0; mode <= 3; mode++)
                {
                    foreach (var material in new[] { currentMaterial, frozenMaterial })
                    {
                        material.SetFloat("_VertexOffset_NormalDir_Toggle", mode);
                        material.SetFloat("_VertexOffset_DirectionSpace", mode == 3 ? 1 : 0);
                        material.SetVector("_VertexOffset_Vec", new Vector4(0, 0, 0, 0));
                    }
                    var zero = Capture(camera, renderer, currentMaterial, target);
                    foreach (var material in new[] { currentMaterial, frozenMaterial })
                        material.SetVector("_VertexOffset_Vec", new Vector4(0, 0, 0.35f, 0));
                    var currentOffset = Capture(camera, renderer, currentMaterial, target);
                    var frozenOffset = Capture(camera, renderer, frozenMaterial, target);
                    Assert.That(CountDifferent(currentOffset, frozenOffset), Is.Zero,
                        "Vertex offset mode " + mode + " differs from the frozen ShaderLab reference.");
                    Assert.That(CountDifferent(zero, currentOffset), Is.GreaterThan(0),
                        "Vertex offset mode " + mode + " did not move visible pixels.");
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
                Object.DestroyImmediate(currentMaterial);
                Object.DestroyImmediate(frozenMaterial);
            }
        }

        [Test]
        public void BaseUVModesMatchFrozenAndChangeSampledPixels()
        {
            var source = AssetDatabase.LoadAssetAtPath<Material>(OriginalPath);
            var frozen = AssetDatabase.LoadAssetAtPath<Material>(FrozenPath);
            Assert.That(source, Is.Not.Null);
            Assert.That(frozen, Is.Not.Null);

            var currentMaterial = new Material(source);
            var frozenMaterial = new Material(frozen);
            var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false, false);
            var colors = new Color32[16 * 16];
            for (var y = 0; y < 16; y++)
                for (var x = 0; x < 16; x++)
                    colors[y * 16 + x] = new Color32((byte)(x * 17), (byte)(y * 17), (byte)((x * 29 + y * 13) & 255), 255);
            texture.SetPixels32(colors);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.Apply(false, false);
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var cameraObject = new GameObject("NBFX_G2_BaseUVCamera");
            var camera = cameraObject.AddComponent<Camera>();
            var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
            var previousActive = RenderTexture.active;
            try
            {
                foreach (var material in new[] { currentMaterial, frozenMaterial })
                {
                    material.SetColor("_BaseColor", Color.white);
                    material.SetFloat("_Cull", 0);
                    material.SetTexture("_BaseMap", texture);
                    material.SetVector("_BaseMap_ST", new Vector4(1, 1, 0, 0));
                    material.SetVector("_CylinderMatrix0", new Vector4(1, 0, 0, 0));
                    material.SetVector("_CylinderMatrix1", new Vector4(0, 1, 0, 0));
                    material.SetVector("_CylinderMatrix2", new Vector4(0, 0, 1, 0));
                    material.SetVector("_CylinderMatrix3", new Vector4(0, 0, 0, 1));
                }

                quad.layer = Layer;
                quad.transform.position = new Vector3(0, 0, 2);
                quad.transform.rotation = Quaternion.Euler(0, 35, 0);
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

                Color32[] defaultPixels = null;
                for (var mode = 0; mode <= 4; mode++)
                {
                    foreach (var material in new[] { currentMaterial, frozenMaterial })
                    {
                        material.SetInteger("_W9ParticleShaderFlags", mode == 1 ? 1 << 9 : mode == 2 ? 1 << 8 : 0);
                        material.SetInteger("_W9ParticleShaderFlags1", 1 | (mode == 4 ? 1 << 20 : 0));
                        material.SetInteger("_UVModeFlag0", mode == 1 || mode == 2 ? 2 : mode == 4 ? 3 : 0);
                        material.SetInteger("_UVModeFlagType0", mode == 3 ? 2 : 0);
                        material.SetVector("_TWParameter", new Vector4(0.5f, 0.5f, 0, 0));
                        material.SetFloat("_TWStrength", 4);
                        material.SetVector("_PCCenter", new Vector4(0.5f, 0.5f, 1, 0));
                        material.SetVector("_SharedUV_ST", new Vector4(0.7f, 1.3f, 0.12f, -0.2f));
                        material.SetVector("_SharedUV_Vec", new Vector4(0, 0, 0.4f, 0));
                    }

                    var current = Capture(camera, renderer, currentMaterial, target);
                    var reference = Capture(camera, renderer, frozenMaterial, target);
                    Assert.That(CountDifferent(current, reference), Is.Zero,
                        "BaseUV mode " + mode + " differs from the frozen ShaderLab reference.");
                    if (mode == 0)
                        defaultPixels = current;
                    else
                        Assert.That(CountDifferent(defaultPixels, current), Is.GreaterThan(0),
                            "BaseUV mode " + mode + " did not change sampled pixels.");
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
