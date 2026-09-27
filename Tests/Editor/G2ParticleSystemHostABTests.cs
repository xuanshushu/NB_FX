using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace NBFX.Baseline.Tests
{
    /// <summary>Same-position G0 A/B through a real Billboard ParticleSystemRenderer, not a mesh stand-in.</summary>
    public sealed class G2ParticleSystemHostABTests
    {
        private const string OriginalPath = "Assets/NBShaderSamples/NBShaderSamples/UnLit.mat";
        private const string FrozenPath = "Assets/NBShaderSamples/NBShader_VFXGraphValidation/Materials/UnLit.mat";
        private const int Size = 96;
        private const int Layer = 2; // Included by the active HighFidelity renderer layer mask.

        [Test]
        public void BillboardHostMatchesFrozenAndParticleStartColorChangesPixels()
        {
            var source = AssetDatabase.LoadAssetAtPath<Material>(OriginalPath);
            var frozenSource = AssetDatabase.LoadAssetAtPath<Material>(FrozenPath);
            Assert.That(source, Is.Not.Null, OriginalPath);
            Assert.That(frozenSource, Is.Not.Null, FrozenPath);
            Assert.That(source.shader.name, Is.EqualTo("Effects/NBShader"));
            Assert.That(frozenSource.shader.name, Is.EqualTo("Effects/NBShader_T00_Frozen"));
            Assert.That(source.shader, Is.Not.EqualTo(frozenSource.shader), "G0 A/B requires distinct shaders.");

            var originalBytes = File.ReadAllBytes(AbsoluteAssetPath(OriginalPath));
            var frozenBytes = File.ReadAllBytes(AbsoluteAssetPath(FrozenPath));
            var current = new Material(source);
            var frozen = new Material(frozenSource);
            var particleObject = new GameObject("NBFX_G2_TransientBillboardParticle");
            var system = particleObject.AddComponent<ParticleSystem>();
            var renderer = particleObject.GetComponent<ParticleSystemRenderer>();
            var cameraObject = new GameObject("NBFX_G2_ParticleHostCamera");
            var camera = cameraObject.AddComponent<Camera>();
            var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
            var previousActive = RenderTexture.active;
            var evidence = new StringBuilder();
            var directory = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Library/NBFXG2");
            try
            {
                Assert.That(renderer, Is.Not.Null, "A ParticleSystemRenderer is required; a MeshRenderer is not a host test.");
                Assert.That(current.GetShaderPassEnabled("UniversalForward"), Is.True);
                Assert.That(frozen.GetShaderPassEnabled("UniversalForward"), Is.True);
                Assert.That(current.renderQueue, Is.EqualTo(frozen.renderQueue));
                ConfigureMaterial(current);
                ConfigureMaterial(frozen);

                particleObject.layer = Layer;
                particleObject.transform.position = new Vector3(0, 0, 2);
                var main = system.main;
                main.playOnAwake = false;
                main.loop = false;
                main.startLifetime = 100f;
                main.startSpeed = 0f;
                main.startSize = 1.25f;
                main.startColor = Color.white;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                main.maxParticles = 1;
                var emission = system.emission;
                emission.enabled = false;
                var shape = system.shape;
                shape.enabled = false;
                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.alignment = ParticleSystemRenderSpace.View;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.SetActiveVertexStreams(new List<ParticleSystemVertexStream>
                {
                    ParticleSystemVertexStream.Position,
                    ParticleSystemVertexStream.Color,
                    ParticleSystemVertexStream.UV
                });
                var streams = new List<ParticleSystemVertexStream>();
                renderer.GetActiveVertexStreams(streams);
                Assert.That(renderer.renderMode, Is.EqualTo(ParticleSystemRenderMode.Billboard));
                Assert.That(streams, Does.Contain(ParticleSystemVertexStream.Color));
                renderer.sharedMaterial = current;

                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.125f, .25f, .375f, 1f);
                camera.orthographic = true;
                camera.orthographicSize = 1f;
                camera.nearClipPlane = .1f;
                camera.farClipPlane = 10f;
                camera.allowHDR = false;
                camera.allowMSAA = false;
                camera.transform.position = new Vector3(0, 0, 3);
                camera.transform.rotation = Quaternion.Euler(0, 180, 0);
                camera.cullingMask = 1 << Layer;
                camera.targetTexture = target;
                Assert.That((camera.cullingMask & (1 << particleObject.layer)) != 0, Is.True);

                Directory.CreateDirectory(directory);
                evidence.AppendLine("host=ParticleSystemRenderer renderMode=Billboard alignment=View layer=2 streams=Position,Color,UV");
                evidence.AppendLine("shaders=" + current.shader.name + " / " + frozen.shader.name +
                    " materialQueue=" + current.renderQueue + " size=" + Size + "x" + Size + " RGBA32");

                renderer.enabled = false;
                var clear = Capture(camera, target);
                SavePng(directory, "G2ParticleHost-disabled-empty.png", clear);
                Assert.That(system.particleCount, Is.Zero);
                renderer.enabled = true;
                var empty = Capture(camera, target);
                SavePng(directory, "G2ParticleHost-enabled-empty.png", empty);
                var emptyPixels = CountDifferent(clear, empty);
                Record(evidence, "emptyVsDisabled=" + emptyPixels);
                Assert.That(emptyPixels, Is.Zero, "An enabled but empty particle system must draw no pixels.");

                EmitFixedParticle(system, new Color32(255, 32, 32, 255));
                renderer.enabled = false;
                var disabledWithParticle = Capture(camera, target);
                SavePng(directory, "G2ParticleHost-disabled-live.png", disabledWithParticle);
                var disabledPixels = CountDifferent(clear, disabledWithParticle);
                Record(evidence, "disabledLiveVsDisabledEmpty=" + disabledPixels +
                    " liveParticleCount=" + system.particleCount);
                Assert.That(disabledPixels, Is.Zero, "A disabled particle renderer must draw no pixels even with a live particle.");
                renderer.enabled = true;

                var red = CapturePair("red", renderer, camera, target, current, frozen, clear, directory, evidence);
                EmitFixedParticle(system, new Color32(32, 255, 32, 255));
                var green = CapturePair("green", renderer, camera, target, current, frozen, clear, directory, evidence);
                var startColorPixels = CountDifferent(red, green);
                Record(evidence, "redVsGreenStartColorDifferentPixels=" + startColorPixels);
                Assert.That(startColorPixels, Is.GreaterThan(0),
                    "Changing only ParticleSystem.EmitParams.startColor must visibly change billboard pixels.");
            }
            finally
            {
                try
                {
                    Directory.CreateDirectory(directory);
                    File.WriteAllText(Path.Combine(directory, "G2ParticleHost-AB.txt"),
                        evidence.ToString(), new UTF8Encoding(false));
                }
                finally
                {
                    if (renderer != null) renderer.sharedMaterial = null;
                    camera.targetTexture = null;
                    RenderTexture.active = previousActive;
                    target.Release();
                    UnityEngine.Object.DestroyImmediate(target);
                    UnityEngine.Object.DestroyImmediate(cameraObject);
                    UnityEngine.Object.DestroyImmediate(particleObject);
                    UnityEngine.Object.DestroyImmediate(current);
                    UnityEngine.Object.DestroyImmediate(frozen);
                    Assert.That(File.ReadAllBytes(AbsoluteAssetPath(OriginalPath)), Is.EqualTo(originalBytes),
                        "The original source material asset changed during this in-memory GPU test.");
                    Assert.That(File.ReadAllBytes(AbsoluteAssetPath(FrozenPath)), Is.EqualTo(frozenBytes),
                        "The G0 frozen material asset changed during this in-memory GPU test.");
                }
            }
        }

        private static void ConfigureMaterial(Material material)
        {
            // Only in-memory copies are normalized. Keep all shader-time and feature variants out of the A/B.
            material.shaderKeywords = new[] { "_FX_LIGHT_MODE_UNLIT" };
            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_ColorA", Color.white);
            material.SetFloat("_BaseColorIntensityForTimeline", 1f);
            material.SetFloat("_AlphaAll", 1f);
            material.SetFloat("_fogintensity", 0f);
            material.SetFloat("_Cull", 0f);
            material.SetFloat("_SrcBlend", 1f);
            material.SetFloat("_DstBlend", 0f);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_ColorMask", 15f);
            material.SetTexture("_BaseMap", Texture2D.whiteTexture);
            material.SetVector("_BaseMap_ST", new Vector4(1, 1, 0, 0));
            material.SetInteger("_W9ParticleShaderFlags", 0);
            material.SetInteger("_W9ParticleShaderFlags1", (1 << 23) | 1);
            // Particle source mode: preserve the slot-1 host bit while leaving vertex color active.
            material.SetInteger("_W9ParticleShaderColorChannelFlag", 3); // Base-map alpha channel.
        }

        private static void EmitFixedParticle(ParticleSystem system, Color32 color)
        {
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            system.Play(true);
            system.Emit(new ParticleSystem.EmitParams
            {
                position = Vector3.zero,
                velocity = Vector3.zero,
                rotation = 0f,
                startSize = 1.25f,
                startLifetime = 100f,
                startColor = color,
                randomSeed = 1u
            }, 1);
            system.Pause(true); // No simulation advances between sequential same-position A/B renders.
            Assert.That(system.particleCount, Is.EqualTo(1), "Manual emission must leave exactly one frozen billboard.");
        }

        private static Color32[] CapturePair(string label, ParticleSystemRenderer renderer, Camera camera,
            RenderTexture target, Material current, Material frozen, Color32[] clear,
            string directory, StringBuilder evidence)
        {
            renderer.sharedMaterial = current;
            var currentPixels = Capture(camera, target);
            renderer.sharedMaterial = frozen;
            var frozenPixels = Capture(camera, target);
            SavePng(directory, "G2ParticleHost-" + label + "-current.png", currentPixels);
            SavePng(directory, "G2ParticleHost-" + label + "-frozen.png", frozenPixels);
            var different = CountDifferent(currentPixels, frozenPixels);
            var currentVisible = CountDifferent(clear, currentPixels);
            var frozenVisible = CountDifferent(clear, frozenPixels);
            Record(evidence, label + " currentFrozenDifferentPixels=" + different +
                " currentVisiblePixels=" + currentVisible + " frozenVisiblePixels=" + frozenVisible);
            Assert.That(currentVisible, Is.GreaterThan(16), label + " current billboard is blank.");
            Assert.That(frozenVisible, Is.GreaterThan(16), label + " frozen billboard is blank.");
            Assert.That(different, Is.Zero, label + " current and G0 Frozen billboard pixels differ.");
            return currentPixels;
        }

        private static Color32[] Capture(Camera camera, RenderTexture target)
        {
            var previousActive = RenderTexture.active;
            var readback = new Texture2D(Size, Size, TextureFormat.RGBA32, false, false);
            try
            {
                camera.Render();
                RenderTexture.active = target;
                readback.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
                readback.Apply(false, false);
                return readback.GetPixels32();
            }
            finally
            {
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(readback);
            }
        }

        private static void SavePng(string directory, string name, Color32[] pixels)
        {
            var image = new Texture2D(Size, Size, TextureFormat.RGBA32, false, false);
            try
            {
                image.SetPixels32(pixels);
                image.Apply(false, false);
                File.WriteAllBytes(Path.Combine(directory, name), image.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(image); }
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

        private static string AbsoluteAssetPath(string assetPath)
        {
            return Path.Combine(Path.GetDirectoryName(Application.dataPath),
                assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static void Record(StringBuilder evidence, string line)
        {
            TestContext.WriteLine(line);
            evidence.AppendLine(line);
        }
    }
}
