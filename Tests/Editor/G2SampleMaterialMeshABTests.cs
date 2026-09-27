using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace NBFX.Baseline.Tests
{
    /// <summary>Mesh-host A/B for the 19 G0-frozen sample materials; source assets remain untouched.</summary>
    public sealed class G2SampleMaterialMeshABTests
    {
        private const string SourceRoot = "Assets/NBShaderSamples/NBShaderSamples/";
        private const string FrozenRoot = "Assets/NBShaderSamples/NBShader_VFXGraphValidation/Materials/";
        private const int Size = 128;
        private const int Layer = 2;

        [Test]
        public void AllFrozenSampleMaterialsMatchCurrentOnIsolatedMesh()
        {
            var frozenGuids = AssetDatabase.FindAssets("t:Material", new[] { FrozenRoot });
            Array.Sort(frozenGuids, StringComparer.Ordinal);
            Assert.That(frozenGuids.Length, Is.EqualTo(19), "G0 froze exactly 19 sample materials.");

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var cameraObject = new GameObject("NBFX_G2_SampleMeshCamera");
            var camera = cameraObject.AddComponent<Camera>();
            var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
            var previousActive = RenderTexture.active;
            try
            {
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

                renderer.enabled = false;
                var clear = Capture(camera, target);
                renderer.enabled = true;
                var output = new StringBuilder("{\"cases\":[");
                var failed = new StringBuilder();
                for (var i = 0; i < frozenGuids.Length; i++)
                {
                    var frozenPath = AssetDatabase.GUIDToAssetPath(frozenGuids[i]);
                    Assert.That(frozenPath.StartsWith(FrozenRoot, StringComparison.Ordinal), Is.True);
                    var sourcePath = SourceRoot + frozenPath.Substring(FrozenRoot.Length);
                    var source = AssetDatabase.LoadAssetAtPath<Material>(sourcePath);
                    var frozen = AssetDatabase.LoadAssetAtPath<Material>(frozenPath);
                    Assert.That(source, Is.Not.Null, sourcePath);
                    Assert.That(frozen, Is.Not.Null, frozenPath);
                    var currentCopy = new Material(source);
                    var frozenCopy = new Material(frozen);
                    try
                    {
                        Assert.That(currentCopy.renderQueue, Is.EqualTo(frozenCopy.renderQueue), sourcePath);
                        var fixedFrame = -1;
                        // Houdini VAT autoplay reads _Time.y on every Camera.Render. Two
                        // sequential captures can select different frames even without a
                        // shader change; pin only in-memory A/B copies to their serialized
                        // display frame, without changing either source material.
                        if (currentCopy.IsKeywordEnabled("_VAT_HOUDINI") && currentCopy.HasProperty("_B_autoPlayback"))
                        {
                            currentCopy.SetFloat("_B_autoPlayback", 0);
                            frozenCopy.SetFloat("_B_autoPlayback", 0);
                            currentCopy.SetFloat("_displayFrame", 1);
                            frozenCopy.SetFloat("_displayFrame", 1);
                            fixedFrame = 1;
                        }
                        if (sourcePath.Contains("/HoudiniVATSamples/Particle/"))
                        {
                            // This sample can be outside a single Quad camera at its
                            // serialized display frame. Find one visible, fixed frame
                            // before claiming an A/B result for its VAT sprite path.
                            renderer.sharedMaterial = currentCopy;
                            var found = false;
                            for (var frame = 1; frame <= 300; frame += 5)
                            {
                                currentCopy.SetFloat("_displayFrame", frame);
                                if (CountDifferent(Capture(camera, target), clear) == 0) continue;
                                fixedFrame = frame;
                                frozenCopy.SetFloat("_displayFrame", frame);
                                found = true;
                                break;
                            }
                            Assert.That(found, Is.True, "No visible fixed frame for the VAT ParticleSprite mesh test.");
                        }
                        renderer.sharedMaterial = currentCopy;
                        var currentPixels = Capture(camera, target);
                        renderer.sharedMaterial = frozenCopy;
                        var frozenPixels = Capture(camera, target);
                        var differing = CountDifferent(currentPixels, frozenPixels);
                        var visible = CountDifferent(currentPixels, clear);
                        if (i > 0) output.Append(',');
                        output.Append("{\"material\":\"").Append(frozenPath.Substring(FrozenRoot.Length))
                            .Append("\",\"differentPixels\":").Append(differing)
                            .Append(",\"visiblePixels\":").Append(visible)
                            .Append(",\"fixedFrame\":").Append(fixedFrame).Append('}');
                        if (differing != 0)
                            failed.Append(frozenPath).Append(": ").Append(differing).Append(" differing pixels; ");
                    }
                    finally
                    {
                        renderer.sharedMaterial = null;
                        UnityEngine.Object.DestroyImmediate(currentCopy);
                        UnityEngine.Object.DestroyImmediate(frozenCopy);
                    }
                }

                output.Append("],\"size\":").Append(Size).Append(",\"colorFormat\":\"RGBA32\"}");
                var directory = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/NBFXG2");
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "sample-material-mesh-ab.json"), output.ToString());
                Assert.That(failed.Length, Is.Zero, failed.ToString());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previousActive;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(quad);
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
                readback.Apply(false);
                return readback.GetPixels32();
            }
            finally
            {
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
    }
}
