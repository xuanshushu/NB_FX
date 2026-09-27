using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace NBFX.Baseline.Tests
{
    /// <summary>Same-position, RGBA-exact Mesh A/B for UV and packed flag paths.</summary>
    public sealed class G2SpecialUVPackedFlagsABTests
    {
        private const string OriginalPath = "Assets/NBShaderSamples/NBShaderSamples/UnLit.mat";
        private const string FrozenPath = "Assets/NBShaderSamples/NBShader_VFXGraphValidation/Materials/UnLit.mat";
        private const int Size = 96;
        private const int Layer = 2;

        [Test]
        public void SpecialUVSelectsTexcoord0Custom1AndCustom2WithVisibleControls()
        {
            using (var fixture = new Fixture(nameof(SpecialUVSelectsTexcoord0Custom1AndCustom2WithVisibleControls)))
            {
                fixture.ForBoth(material =>
                {
                    material.SetInteger("_UVModeFlag0", 1); // main UV slot 0 -> specialUVChannel
                    material.SetInteger("_W9ParticleCustomDataFlag0", 0);
                });

                var flags = new[]
                {
                    1,                                   // TEXCOORD0.zw
                    1 | (1 << 21) | (1 << 18),           // Custom1.xy
                    1 | (1 << 21) | (1 << 19),           // Custom2.xy
                    1 | (1 << 21) | (1 << 18) | (1 << 19) // Custom2 overrides Custom1
                };
                var captures = new Color32[flags.Length][];
                for (var i = 0; i < flags.Length; i++)
                {
                    var state = i;
                    fixture.ForBoth(material => material.SetInteger("_W9ParticleShaderFlags1", flags[state]));
                    captures[i] = fixture.CapturePair("Special UV state " + i);
                }

                fixture.LogControl("Special UV A0/A1", captures[0], captures[1]);
                fixture.LogControl("Special UV A0/A2", captures[0], captures[2]);
                fixture.LogControl("Special UV A1/A2", captures[1], captures[2]);
                fixture.LogControl("Special UV A2/A3", captures[2], captures[3]);
                fixture.LogControl("Special UV A1/A3", captures[1], captures[3]);
                Assert.That(CountDifferent(captures[0], captures[1]), Is.GreaterThan(0), "Custom1 must differ from TEXCOORD0.zw.");
                Assert.That(CountDifferent(captures[0], captures[2]), Is.GreaterThan(0), "Custom2 must differ from TEXCOORD0.zw.");
                Assert.That(CountDifferent(captures[1], captures[2]), Is.GreaterThan(0), "Custom1 and Custom2 must select different pixels.");
                Assert.That(CountDifferent(captures[2], captures[3]), Is.Zero, "When both flags are set, Custom2 must win.");
                Assert.That(CountDifferent(captures[1], captures[3]), Is.GreaterThan(0), "The overwrite control must be visible.");
            }
        }

        [Test]
        public void PackedCustomDataNibblesSelectCustom1AndCustom2MainOffsets()
        {
            using (var fixture = new Fixture(nameof(PackedCustomDataNibblesSelectCustom1AndCustom2MainOffsets)))
            {
                fixture.SetCustomStreams(new Vector4(.125f, .25f, 0, 0), new Vector4(-.375f, .5f, 0, 0));
                fixture.ForBoth(material =>
                {
                    material.SetInteger("_UVModeFlag0", 0);
                    material.SetInteger("_W9ParticleShaderFlags1", 1);
                });

                var packed = new[] { 0, 0xEF, 0xAB }; // XY offsets: none, Custom1.xy, Custom2.xy
                var captures = new Color32[packed.Length][];
                for (var i = 0; i < packed.Length; i++)
                {
                    var state = i;
                    fixture.ForBoth(material => material.SetInteger("_W9ParticleCustomDataFlag0", packed[state]));
                    captures[i] = fixture.CapturePair("Packed CustomData state " + i);
                }

                fixture.LogControl("Packed CustomData B0/B1", captures[0], captures[1]);
                fixture.LogControl("Packed CustomData B0/B2", captures[0], captures[2]);
                fixture.LogControl("Packed CustomData B1/B2", captures[1], captures[2]);
                Assert.That(CountDifferent(captures[0], captures[1]), Is.GreaterThan(0), "0xEF must visibly use Custom1.xy.");
                Assert.That(CountDifferent(captures[0], captures[2]), Is.GreaterThan(0), "0xAB must visibly use Custom2.xy.");
                Assert.That(CountDifferent(captures[1], captures[2]), Is.GreaterThan(0), "The two packed stream selectors must differ.");
            }
        }

        [Test]
        public void BaseMapPackedWrapModesMatchFrozenAndHavePairwiseVisibleControls()
        {
            using (var fixture = new Fixture(nameof(BaseMapPackedWrapModesMatchFrozenAndHavePairwiseVisibleControls)))
            {
                fixture.ForBoth(material =>
                {
                    material.SetInteger("_UVModeFlag0", 0);
                    material.SetInteger("_W9ParticleShaderFlags1", 1);
                    material.SetInteger("_W9ParticleCustomDataFlag0", 0);
                    material.SetInteger("_NBShaderForceNoMipFlags", 0);
                    material.SetVector("_BaseMap_ST", new Vector4(1.8f, 1.7f, -.4f, -.35f));
                });

                var wrapFlags = new[] { 0, 1, 1 << 16, 1 | (1 << 16) };
                var captures = new Color32[wrapFlags.Length][];
                for (var i = 0; i < wrapFlags.Length; i++)
                {
                    var state = i;
                    fixture.ForBoth(material => material.SetInteger("_W9ParticleShaderWrapFlags", wrapFlags[state]));
                    captures[i] = fixture.CapturePair("BaseMap Wrap state " + i);
                }

                for (var left = 0; left < captures.Length; left++)
                    for (var right = left + 1; right < captures.Length; right++)
                    {
                        fixture.LogControl("BaseMap Wrap " + left + "/" + right, captures[left], captures[right]);
                        Assert.That(CountDifferent(captures[left], captures[right]), Is.GreaterThan(0),
                            "Wrap states " + left + " and " + right + " must be distinguishable.");
                    }
            }
        }

        private sealed class Fixture : IDisposable
        {
            private readonly Material current;
            private readonly Material frozen;
            private readonly Texture2D texture;
            private readonly Mesh mesh;
            private readonly GameObject quad;
            private readonly MeshRenderer renderer;
            private readonly GameObject cameraObject;
            private readonly Camera camera;
            private readonly RenderTexture target;
            private readonly RenderTexture previousActive;
            private readonly Color32[] clear;
            private readonly string evidenceName;
            private readonly StringBuilder evidence = new StringBuilder();

            public Fixture(string evidenceName)
            {
                this.evidenceName = evidenceName;
                var originalAsset = AssetDatabase.LoadAssetAtPath<Material>(OriginalPath);
                var frozenAsset = AssetDatabase.LoadAssetAtPath<Material>(FrozenPath);
                Assert.That(originalAsset, Is.Not.Null, OriginalPath);
                Assert.That(frozenAsset, Is.Not.Null, FrozenPath);
                Assert.That(originalAsset.shader, Is.Not.EqualTo(frozenAsset.shader), "A/B must use distinct current/frozen shaders.");
                current = new Material(originalAsset);
                frozen = new Material(frozenAsset);
                foreach (var material in new[] { current, frozen })
                {
                    Assert.That(material.GetShaderPassEnabled("UniversalForward"), Is.True);
                    material.shaderKeywords = new[] { "_FX_LIGHT_MODE_UNLIT" }; // no Flipbook/VAT/Noise/other feature keyword
                    material.SetColor("_BaseColor", Color.white);
                    material.SetColor("_ColorA", Color.white);
                    material.SetFloat("_BaseColorIntensityForTimeline", 1);
                    material.SetFloat("_AlphaAll", 1);
                    material.SetFloat("_Cull", 0);
                    material.SetTexture("_BaseMap", null); // assigned after the in-memory texture is built
                    material.SetVector("_BaseMap_ST", new Vector4(1, 1, 0, 0));
                    material.SetVector("_BaseMapMaskMapOffset", Vector4.zero);
                    material.SetFloat("_BaseMapUVRotation", 0);
                    material.SetFloat("_BaseMapUVRotationSpeed", 0);
                    material.SetInteger("_W9ParticleShaderFlags", 0);
                    material.SetInteger("_W9ParticleShaderFlags1", 1);
                    material.SetInteger("_W9ParticleShaderColorChannelFlag", 3);
                    material.SetInteger("_W9ParticleShaderWrapFlags", 0);
                    material.SetInteger("_NBShaderForceNoMipFlags", 0);
                    material.SetInteger("_W9ParticleCustomDataFlag0", 0);
                    material.SetInteger("_UVModeFlag0", 0);
                    material.SetInteger("_UVModeFlagType0", 0);
                }

                texture = new Texture2D(16, 16, TextureFormat.RGBA32, false, false);
                var pixels = new Color32[16 * 16];
                for (var y = 0; y < 16; y++)
                    for (var x = 0; x < 16; x++)
                        pixels[y * 16 + x] = new Color32((byte)(x * 17), (byte)(y * 17),
                            (byte)((x * 29 + y * 13) & 255), 255);
                texture.SetPixels32(pixels);
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Repeat;
                texture.Apply(false, false);
                ForBoth(material => material.SetTexture("_BaseMap", texture));

                mesh = new Mesh { name = "NBFX_G2_SpecialUV_TransientQuad" };
                mesh.vertices = new[]
                {
                    new Vector3(-.7f, -.7f, 0), new Vector3(.7f, -.7f, 0),
                    new Vector3(.7f, .7f, 0), new Vector3(-.7f, .7f, 0)
                };
                mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
                mesh.normals = new[] { Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward };
                mesh.colors = new[] { Color.white, Color.white, Color.white, Color.white };
                mesh.SetUVs(0, new List<Vector4>
                {
                    new Vector4(0, 0, .125f, .875f), new Vector4(1, 0, .125f, .875f),
                    new Vector4(1, 1, .125f, .875f), new Vector4(0, 1, .125f, .875f)
                });
                SetCustomStreams(new Vector4(.125f, .375f, 0, 0), new Vector4(.75f, .625f, 0, 0));
                mesh.RecalculateBounds();

                quad = new GameObject("NBFX_G2_SpecialUV_TransientQuad");
                quad.layer = Layer;
                quad.transform.position = new Vector3(0, 0, 2);
                quad.AddComponent<MeshFilter>().sharedMesh = mesh;
                renderer = quad.AddComponent<MeshRenderer>();
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;

                cameraObject = new GameObject("NBFX_G2_SpecialUV_Camera");
                camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.125f, .25f, .375f, 1);
                camera.orthographic = true;
                camera.orthographicSize = 1;
                camera.nearClipPlane = .1f;
                camera.farClipPlane = 10;
                camera.allowHDR = false;
                camera.allowMSAA = false;
                camera.transform.position = new Vector3(0, 0, 3);
                camera.transform.rotation = Quaternion.Euler(0, 180, 0);
                camera.cullingMask = 1 << Layer;
                target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = target;
                previousActive = RenderTexture.active;

                renderer.enabled = false;
                clear = Capture();
                renderer.enabled = true;
            }

            public void SetCustomStreams(Vector4 custom1, Vector4 custom2)
            {
                mesh.SetUVs(1, new List<Vector4> { custom1, custom1, custom1, custom1 });
                mesh.SetUVs(2, new List<Vector4> { custom2, custom2, custom2, custom2 });
            }

            public void ForBoth(Action<Material> action)
            {
                action(current);
                action(frozen);
            }

            public Color32[] CapturePair(string label)
            {
                renderer.sharedMaterial = current;
                var currentPixels = Capture();
                renderer.sharedMaterial = frozen;
                var frozenPixels = Capture();
                var differentPixels = CountDifferent(currentPixels, frozenPixels);
                var visiblePixels = CountDifferent(clear, currentPixels);
                Record(label + " currentFrozenDifferentPixels=" + differentPixels +
                    " visiblePixels=" + visiblePixels);
                Assert.That(differentPixels, Is.Zero,
                    label + " current/frozen RGBA mismatch at the same Mesh/camera position.");
                Assert.That(visiblePixels, Is.GreaterThan(0),
                    label + " rendered no visible pixels; a blank A/B is not evidence.");
                return currentPixels;
            }

            private Color32[] Capture()
            {
                camera.Render();
                RenderTexture.active = target;
                var readback = new Texture2D(Size, Size, TextureFormat.RGBA32, false, false);
                try
                {
                    readback.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
                    readback.Apply(false, false);
                    return readback.GetPixels32();
                }
                finally { UnityEngine.Object.DestroyImmediate(readback); }
            }

            public void Dispose()
            {
                try
                {
                    var directory = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/NBFXG2");
                    Directory.CreateDirectory(directory);
                    File.WriteAllText(Path.Combine(directory, "G2SpecialUV-" + evidenceName + ".txt"),
                        evidence.ToString(), new UTF8Encoding(false));
                }
                finally
                {
                    camera.targetTexture = null;
                    RenderTexture.active = previousActive;
                    target.Release();
                    UnityEngine.Object.DestroyImmediate(target);
                    UnityEngine.Object.DestroyImmediate(cameraObject);
                    UnityEngine.Object.DestroyImmediate(quad);
                    UnityEngine.Object.DestroyImmediate(mesh);
                    UnityEngine.Object.DestroyImmediate(texture);
                    UnityEngine.Object.DestroyImmediate(current);
                    UnityEngine.Object.DestroyImmediate(frozen);
                }
            }

            public void LogControl(string label, Color32[] left, Color32[] right)
            {
                Record(label + " controlDifferentPixels=" + CountDifferent(left, right));
            }

            private void Record(string line)
            {
                TestContext.WriteLine(line);
                evidence.AppendLine(line);
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

    }
}
