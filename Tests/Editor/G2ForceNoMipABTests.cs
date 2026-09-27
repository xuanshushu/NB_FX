using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace NBFX.Baseline.Tests
{
    /// <summary>Same-position G0 A/B for BaseMap ForceNoMip bit 0, with an observable mip-level control.</summary>
    public sealed class G2ForceNoMipABTests
    {
        private const string OriginalPath = "Assets/NBShaderSamples/NBShaderSamples/UnLit.mat";
        private const string FrozenPath = "Assets/NBShaderSamples/NBShader_VFXGraphValidation/Materials/UnLit.mat";
        private const int Size = 128;
        private const int TextureSize = 64;
        private const int Layer = 2;

        [Test]
        public void BaseMapForceNoMipBit0MatchesFrozenAndChangesMinifiedPixels()
        {
            var source = AssetDatabase.LoadAssetAtPath<Material>(OriginalPath);
            var frozenSource = AssetDatabase.LoadAssetAtPath<Material>(FrozenPath);
            Assert.That(source, Is.Not.Null, OriginalPath);
            Assert.That(frozenSource, Is.Not.Null, FrozenPath);
            Assert.That(source.shader, Is.Not.EqualTo(frozenSource.shader), "G0 A/B must use distinct shaders.");

            var current = new Material(source);
            var frozen = new Material(frozenSource);
            var mipTexture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, true, false);
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var cameraObject = new GameObject("NBFX_G2_ForceNoMipCamera");
            var camera = cameraObject.AddComponent<Camera>();
            var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
            var previousActive = RenderTexture.active;
            var evidence = new StringBuilder();
            try
            {
                // A spatial checkerboard at mip 0 makes the texture high frequency.
                // Every smaller mip is solid green, so LOD 0 and implicit minified
                // sampling cannot accidentally produce the same center color.
                for (var mip = 0; mip < mipTexture.mipmapCount; mip++)
                {
                    var width = Mathf.Max(1, TextureSize >> mip);
                    var colors = new Color32[width * width];
                    for (var y = 0; y < width; y++)
                        for (var x = 0; x < width; x++)
                            colors[y * width + x] = mip == 0
                                ? (((x + y) & 1) == 0
                                    ? new Color32(255, 0, 255, 255)
                                    : new Color32(160, 0, 160, 255))
                                : new Color32(0, 255, 0, 255);
                    mipTexture.SetPixels32(colors, mip);
                }
                mipTexture.filterMode = FilterMode.Trilinear;
                mipTexture.wrapMode = TextureWrapMode.Repeat;
                mipTexture.Apply(false, false); // upload hand-authored levels; never regenerate mip 1+
                evidence.AppendLine("texture=64x64 RGBA32; mip0=magenta checkerboard; mip1+=solid green; Apply(false,false)");
                evidence.AppendLine("mipmapCount=" + mipTexture.mipmapCount + " captureSize=" + Size + " orthoSize=8 quadWorldSize=1");
                Assert.That(mipTexture.mipmapCount, Is.GreaterThan(1), "A full mip chain is required.");

                foreach (var material in new[] { current, frozen })
                {
                    Assert.That(material.GetShaderPassEnabled("UniversalForward"), Is.True);
                    material.shaderKeywords = new[] { "_FX_LIGHT_MODE_UNLIT" };
                    material.SetTexture("_BaseMap", mipTexture);
                    material.SetVector("_BaseMap_ST", new Vector4(1, 1, 0, 0));
                    material.SetVector("_BaseMapMaskMapOffset", Vector4.zero);
                    material.SetFloat("_BaseMapUVRotation", 0);
                    material.SetFloat("_BaseMapUVRotationSpeed", 0);
                    material.SetColor("_BaseColor", Color.white);
                    material.SetColor("_ColorA", Color.white);
                    material.SetFloat("_BaseColorIntensityForTimeline", 1);
                    material.SetFloat("_AlphaAll", 1);
                    material.SetFloat("_fogintensity", 0);
                    material.SetFloat("_Cull", 0);
                    material.SetInteger("_W9ParticleShaderFlags", 0);
                    material.SetInteger("_W9ParticleShaderFlags1", 1);
                    material.SetInteger("_W9ParticleShaderWrapFlags", 0);
                    material.SetInteger("_W9ParticleShaderColorChannelFlag", 3);
                    material.SetInteger("_W9ParticleCustomDataFlag0", 0);
                    material.SetInteger("_UVModeFlag0", 0);
                    material.SetInteger("_UVModeFlagType0", 0);
                    material.SetInteger("_NBShaderForceNoMipFlags", 0);
                }

                quad.layer = Layer;
                quad.transform.position = new Vector3(0, 0, 2);
                var renderer = quad.GetComponent<MeshRenderer>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.125f, .25f, .375f, 1);
                camera.orthographic = true;
                camera.orthographicSize = 8; // 1-world-unit Quad occupies roughly 8 of 128 pixels
                camera.nearClipPlane = .1f;
                camera.farClipPlane = 10;
                camera.allowHDR = false;
                camera.allowMSAA = false;
                camera.transform.position = new Vector3(0, 0, 3);
                camera.transform.rotation = Quaternion.Euler(0, 180, 0);
                camera.cullingMask = 1 << Layer;
                camera.targetTexture = target;

                renderer.enabled = false;
                var clear = Capture(camera, target);
                renderer.enabled = true;
                Color32[] implicitPixels = null;
                Color32[] lodZeroPixels = null;
                for (var forceNoMip = 0; forceNoMip <= 1; forceNoMip++)
                {
                    current.SetInteger("_NBShaderForceNoMipFlags", forceNoMip);
                    frozen.SetInteger("_NBShaderForceNoMipFlags", forceNoMip);
                    renderer.sharedMaterial = current;
                    var currentPixels = Capture(camera, target);
                    renderer.sharedMaterial = frozen;
                    var frozenPixels = Capture(camera, target);
                    var differentPixels = CountDifferent(currentPixels, frozenPixels);
                    var visiblePixels = CountDifferent(clear, currentPixels);
                    var center = currentPixels[(Size / 2) * Size + Size / 2];
                    evidence.AppendLine("forceNoMip=" + forceNoMip + " currentFrozenDifferentPixels=" + differentPixels +
                        " visiblePixels=" + visiblePixels + " currentCenterRGBA=" + center);
                    Assert.That(differentPixels, Is.Zero, "ForceNoMip bit0=" + forceNoMip + " differs from G0 frozen ShaderLab.");
                    Assert.That(visiblePixels, Is.GreaterThan(0), "ForceNoMip bit0=" + forceNoMip + " rendered blank.");
                    if (forceNoMip == 0)
                        implicitPixels = currentPixels;
                    else
                        lodZeroPixels = currentPixels;
                }

                var controlDifferentPixels = CountDifferent(implicitPixels, lodZeroPixels);
                var implicitCenter = implicitPixels[(Size / 2) * Size + Size / 2];
                var lodZeroCenter = lodZeroPixels[(Size / 2) * Size + Size / 2];
                evidence.AppendLine("D4_D5_controlDifferentPixels=" + controlDifferentPixels);
                evidence.AppendLine("implicitCenterRGBA=" + implicitCenter + " explicitLOD0CenterRGBA=" + lodZeroCenter);
                Assert.That(controlDifferentPixels, Is.GreaterThan(0),
                    "Implicit minified LOD and explicit LOD0 must visibly differ; otherwise ForceNoMip was not covered.");
                Assert.That(implicitCenter.g, Is.GreaterThan(implicitCenter.r),
                    "Implicit LOD did not reach the green higher mip; shrink the projection before treating this as coverage.");
                Assert.That(lodZeroCenter.r, Is.GreaterThan(lodZeroCenter.g),
                    "ForceNoMip did not sample the magenta LOD0 level.");
                Assert.That(lodZeroCenter.b, Is.GreaterThan(lodZeroCenter.g),
                    "ForceNoMip did not sample the magenta LOD0 level.");
            }
            finally
            {
                try
                {
                    var directory = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/NBFXG2");
                    Directory.CreateDirectory(directory);
                    File.WriteAllText(Path.Combine(directory, "G2ForceNoMip-BaseMapBit0.txt"),
                        evidence.ToString(), new UTF8Encoding(false));
                }
                finally
                {
                    camera.targetTexture = null;
                    RenderTexture.active = previousActive;
                    target.Release();
                    Object.DestroyImmediate(target);
                    Object.DestroyImmediate(cameraObject);
                    Object.DestroyImmediate(quad);
                    Object.DestroyImmediate(mipTexture);
                    Object.DestroyImmediate(current);
                    Object.DestroyImmediate(frozen);
                }
            }
        }

        private static Color32[] Capture(Camera camera, RenderTexture target)
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
