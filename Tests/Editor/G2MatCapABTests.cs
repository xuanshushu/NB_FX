using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace NBFX.Baseline.Tests
{
    public sealed class G2MatCapABTests
    {
        private const string OriginalPath = "Assets/NBShaderSamples/NBShaderSamples/UnLit.mat";
        private const string FrozenPath = "Assets/NBShaderSamples/NBShader_VFXGraphValidation/Materials/UnLit.mat";
        private const int Size = 96;
        private const int Layer = 2;

        [Test]
        public void MatCapViewUVAndBlendMatchFrozenWithVisibleControls()
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
                    colors[y * 32 + x] = new Color32((byte)(x * 8), (byte)(y * 8), (byte)((x * 3 + y * 5) & 255), 255);
            texture.SetPixels32(colors);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.Apply(false, false);
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var cameraObject = new GameObject("NBFX_G2_MatCapCamera");
            var camera = cameraObject.AddComponent<Camera>();
            var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
            var previousActive = RenderTexture.active;
            try
            {
                foreach (var material in new[] { currentMaterial, frozenMaterial })
                {
                    material.SetColor("_BaseColor", new Color(0.18f, 0.23f, 0.31f, 1));
                    material.SetFloat("_Cull", 0);
                    material.SetTexture("_MatCapTex", texture);
                    material.SetColor("_MatCapColor", new Color(0.9f, 0.7f, 0.5f, 1));
                    material.SetVector("_MatCapInfo", Vector4.zero);
                }
                quad.layer = Layer;
                quad.transform.position = new Vector3(0, 0, 2);
                quad.transform.rotation = Quaternion.Euler(0, 25, 0);
                var renderer = quad.GetComponent<MeshRenderer>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.125f, 0.25f, 0.375f, 1);
                camera.orthographicSize = 1;
                camera.fieldOfView = 65;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 10;
                camera.allowHDR = false;
                camera.allowMSAA = false;
                camera.transform.position = new Vector3(0, 0, 3);
                camera.transform.rotation = Quaternion.Euler(0, 180, 0);
                camera.cullingMask = 1 << Layer;
                camera.targetTexture = target;

                for (var projection = 0; projection < 2; projection++)
                {
                    camera.orthographic = projection == 0;
                    currentMaterial.DisableKeyword("_MATCAP");
                    frozenMaterial.DisableKeyword("_MATCAP");
                    var withoutMatCap = Capture(camera, renderer, currentMaterial, target);
                    var frozenWithout = Capture(camera, renderer, frozenMaterial, target);
                    Assert.That(CountDifferent(withoutMatCap, frozenWithout), Is.Zero);
                    currentMaterial.EnableKeyword("_MATCAP");
                    frozenMaterial.EnableKeyword("_MATCAP");
                    // Do not compare a cold local-keyword variant against an
                    // already compiled one on the first Editor render.
                    Capture(camera, renderer, currentMaterial, target);
                    Capture(camera, renderer, frozenMaterial, target);

                    Color32[] additive = null;
                    for (var blend = 0; blend <= 1; blend++)
                        foreach (var opacity in new[] { 0f, 0.5f, 1f })
                        {
                            foreach (var material in new[] { currentMaterial, frozenMaterial })
                            {
                                material.SetColor("_MatCapColor", new Color(0.9f, 0.7f, 0.5f, opacity));
                                material.SetVector("_MatCapInfo", new Vector4(blend, 0, 0, 0));
                            }
                            var current = Capture(camera, renderer, currentMaterial, target);
                            var reference = Capture(camera, renderer, frozenMaterial, target);
                            if (projection == 0 && blend == 0 && opacity == 0)
                            {
                                var path = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/NBFXG2");
                                Directory.CreateDirectory(path);
                                WritePng(Path.Combine(path, "matcap_current_alpha0.png"), current);
                                WritePng(Path.Combine(path, "matcap_frozen_alpha0.png"), reference);
                                WritePng(Path.Combine(path, "matcap_off.png"), withoutMatCap);
                            }
                            Assert.That(CountDifferent(current, reference), Is.Zero,
                                "MatCap projection=" + projection + ", blend=" + blend + ", opacity=" + opacity +
                                ", currentCenter=" + current[Size * Size / 2 + Size / 2] +
                                ", frozenCenter=" + reference[Size * Size / 2 + Size / 2]);
                            if (opacity == 0)
                                Assert.That(CountDifferent(withoutMatCap, current), Is.Zero,
                                    "Zero MatCap opacity must leave the image unchanged.");
                            if (blend == 0 && opacity == 1)
                            {
                                additive = current;
                                Assert.That(CountDifferent(withoutMatCap, current), Is.GreaterThan(0),
                                    "MatCap must visibly affect the image.");
                            }
                            if (blend == 1 && opacity == 1)
                                Assert.That(CountDifferent(additive, current), Is.GreaterThan(0),
                                    "Multiply and additive MatCap branches must differ.");
                        }
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
                if (left[i].r != right[i].r || left[i].g != right[i].g ||
                    left[i].b != right[i].b || left[i].a != right[i].a)
                    count++;
            return count;
        }

        private static void WritePng(string path, Color32[] pixels)
        {
            var image = new Texture2D(Size, Size, TextureFormat.RGBA32, false, false);
            try
            {
                image.SetPixels32(pixels);
                image.Apply(false);
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(image);
            }
        }
    }
}
