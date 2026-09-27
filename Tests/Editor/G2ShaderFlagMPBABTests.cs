using System;
using System.IO;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace NBFX.Baseline.Tests
{
    /// <summary>
    /// GPU A/B for a whole-word MPB override of packed ShaderFlags1. The C# helper is
    /// resolved by reflection because the existing G0 Editor test asmdef has no runtime
    /// NBShader assembly reference; this test does not change that assembly boundary.
    /// </summary>
    public sealed class G2ShaderFlagMPBABTests
    {
        private const string OriginalPath = "Assets/NBShaderSamples/NBShaderSamples/UnLit.mat";
        private const string FrozenPath = "Assets/NBShaderSamples/NBShader_VFXGraphValidation/Materials/UnLit.mat";
        private const string Flags1Name = "_W9ParticleShaderFlags1";
        private const int Flags1Index = 1;
        private const int BaseWord = (1 << 9) | 1; // ignore vertex color + transparent mode
        private const int HighBit = unchecked((int)0x80000000u); // Overlay1 alpha-multiply
        private const int FullWord = unchecked((int)0x80000201u);
        private const int Size = 96;
        private const int Layer = 2; // included by the active HighFidelity renderer layer mask

        private enum State { Base, MaterialFullWord, SeededMPB, UnseededMPB, MaterialUnseededWord }

        [Test]
        public void ShaderFlags1HighBitMPBMatchesMaterialAndFrozenWithUnseededDiagnostic()
        {
            var source = AssetDatabase.LoadAssetAtPath<Material>(OriginalPath);
            var frozenSource = AssetDatabase.LoadAssetAtPath<Material>(FrozenPath);
            Assert.That(source, Is.Not.Null, OriginalPath);
            Assert.That(frozenSource, Is.Not.Null, FrozenPath);
            Assert.That(source.shader, Is.Not.EqualTo(frozenSource.shader), "G0 A/B requires distinct shaders.");

            var flagsType = Type.GetType("NBShader.NBShaderFlags, com.xuanxuan.nb.shaders2");
            Assert.That(flagsType, Is.Not.Null, "The production NBShaderFlags type must be loaded.");
            var setFlagBits = flagsType.GetMethod("SetFlagBits", BindingFlags.Public | BindingFlags.Instance,
                null, new[] { typeof(int), typeof(MaterialPropertyBlock), typeof(int) }, null);
            Assert.That(setFlagBits, Is.Not.Null, "The runtime packed-word helper signature changed.");

            var originalBytes = File.ReadAllBytes(AbsoluteAssetPath(OriginalPath));
            var frozenBytes = File.ReadAllBytes(AbsoluteAssetPath(FrozenPath));
            var current = new Material(source);
            var frozen = new Material(frozenSource);
            var emissionTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false, false);
            var mesh = new Mesh { name = "NBFX_G2_MPB_TransientQuad" };
            var quad = new GameObject("NBFX_G2_MPB_TransientQuad");
            var cameraObject = new GameObject("NBFX_G2_MPB_Camera");
            var camera = cameraObject.AddComponent<Camera>();
            var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
            var previousActive = RenderTexture.active;
            var evidence = new StringBuilder();
            MeshRenderer renderer = null;
            try
            {
                emissionTexture.SetPixel(0, 0, new Color32(80, 180, 220, 128));
                emissionTexture.filterMode = FilterMode.Point;
                emissionTexture.Apply(false, true);

                foreach (var material in new[] { current, frozen })
                {
                    Assert.That(material.GetShaderPassEnabled("UniversalForward"), Is.True);
                    material.shaderKeywords = new[] { "_FX_LIGHT_MODE_UNLIT", "_EMISSION" };
                    material.SetColor("_BaseColor", new Color(.8f, .6f, .4f, 1));
                    material.SetColor("_ColorA", Color.white);
                    material.SetFloat("_BaseColorIntensityForTimeline", 1);
                    material.SetFloat("_AlphaAll", 1);
                    material.SetFloat("_fogintensity", 0);
                    material.SetFloat("_Cull", 0);
                    material.SetFloat("_SrcBlend", 1);
                    material.SetFloat("_DstBlend", 0);
                    material.SetFloat("_ZWrite", 0);
                    material.SetFloat("_ColorMask", 15);
                    material.SetTexture("_BaseMap", Texture2D.whiteTexture);
                    material.SetVector("_BaseMap_ST", new Vector4(1, 1, 0, 0));
                    material.SetInteger("_W9ParticleShaderFlags", 1 << 5); // Overlay1 multiply mode
                    material.SetInteger(Flags1Name, BaseWord);
                    material.SetInteger("_W9ParticleShaderWrapFlags", 0);
                    material.SetInteger("_NBShaderForceNoMipFlags", 0);
                    material.SetInteger("_UVModeFlag0", 0);
                    material.SetInteger("_W9ParticleCustomDataFlag0", 0);
                    material.SetTexture("_EmissionMap", emissionTexture);
                    material.SetVector("_EmissionMap_ST", new Vector4(1, 1, 0, 0));
                    material.SetColor("_EmissionMapColor", Color.white);
                    material.SetFloat("_EmissionMapColorIntensity", 1);
                    material.SetFloat("_EmissionAlphaIntensity", 1);
                }

                mesh.vertices = new[]
                {
                    new Vector3(-.7f, -.7f, 0), new Vector3(.7f, -.7f, 0),
                    new Vector3(.7f, .7f, 0), new Vector3(-.7f, .7f, 0)
                };
                mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
                mesh.normals = new[] { Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward };
                mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
                var vertexColor = new Color(.5f, .25f, .75f, 1);
                mesh.colors = new[] { vertexColor, vertexColor, vertexColor, vertexColor };
                mesh.RecalculateBounds();

                quad.layer = Layer;
                quad.transform.position = new Vector3(0, 0, 2);
                quad.AddComponent<MeshFilter>().sharedMesh = mesh;
                renderer = quad.AddComponent<MeshRenderer>();
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;

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
                camera.targetTexture = target;

                renderer.enabled = false;
                var clear = Capture(camera, target);
                renderer.enabled = true;
                evidence.AppendLine("slot=1 property=" + Flags1Name + " base=0x00000201 highBit=0x80000000 full=0x80000201");
                evidence.AppendLine("texture=RGBA(80,180,220,128) vertexColor=(0.5,0.25,0.75,1) Overlay1=multiply");

                var baseline = CapturePair(State.Base, current, frozen, renderer, camera, target, clear,
                    flagsType, setFlagBits, evidence);
                var baselineCenter = baseline[(Size / 2) * Size + Size / 2];
                Assert.That(baselineCenter.r + baselineCenter.g + baselineCenter.b, Is.GreaterThan(0),
                    "The first baseline capture is black; do not accept a shader-compilation placeholder as an MPB control.");
                var materialFull = CapturePair(State.MaterialFullWord, current, frozen, renderer, camera, target, clear,
                    flagsType, setFlagBits, evidence);
                var seededMPB = CapturePair(State.SeededMPB, current, frozen, renderer, camera, target, clear,
                    flagsType, setFlagBits, evidence);
                var unseededMPB = CapturePair(State.UnseededMPB, current, frozen, renderer, camera, target, clear,
                    flagsType, setFlagBits, evidence);
                var materialUnseeded = CapturePair(State.MaterialUnseededWord, current, frozen, renderer, camera, target, clear,
                    flagsType, setFlagBits, evidence);

                var highBitControl = CountDifferent(baseline, materialFull);
                var seededGpuMatch = CountDifferent(materialFull, seededMPB);
                var lostBaseBitsControl = CountDifferent(seededMPB, unseededMPB);
                var unseededGpuMatch = CountDifferent(materialUnseeded, unseededMPB);
                Record(evidence, "baselineVsMaterialHighBit=" + highBitControl +
                    " materialVsSeededMPB=" + seededGpuMatch +
                    " seededVsUnseededMPB=" + lostBaseBitsControl +
                    " unseededMPBVsMaterial0x80000000=" + unseededGpuMatch);
                Assert.That(highBitControl, Is.GreaterThan(0), "Bit 31 must change GPU pixels, not only a C# word.");
                Assert.That(seededGpuMatch, Is.Zero, "Seeded MPB GPU pixels must match the material full-word reference.");
                Assert.That(lostBaseBitsControl, Is.GreaterThan(0),
                    "The unseeded diagnostic must visibly lose base bit 9 (ignore vertex color).");
                Assert.That(unseededGpuMatch, Is.Zero,
                    "Unseeded MPB must render like its actual 0x80000000 word, not silently inherit material bits.");
            }
            finally
            {
                try
                {
                    var directory = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Library/NBFXG2");
                    Directory.CreateDirectory(directory);
                    File.WriteAllText(Path.Combine(directory, "G2ShaderFlagMPB-Bit31.txt"),
                        evidence.ToString(), new UTF8Encoding(false));
                }
                finally
                {
                    if (renderer != null) renderer.SetPropertyBlock(null);
                    camera.targetTexture = null;
                    RenderTexture.active = previousActive;
                    target.Release();
                    UnityEngine.Object.DestroyImmediate(target);
                    UnityEngine.Object.DestroyImmediate(cameraObject);
                    UnityEngine.Object.DestroyImmediate(quad);
                    UnityEngine.Object.DestroyImmediate(mesh);
                    UnityEngine.Object.DestroyImmediate(emissionTexture);
                    UnityEngine.Object.DestroyImmediate(current);
                    UnityEngine.Object.DestroyImmediate(frozen);
                    Assert.That(File.ReadAllBytes(AbsoluteAssetPath(OriginalPath)), Is.EqualTo(originalBytes),
                        "The source material asset changed during this in-memory GPU test.");
                    Assert.That(File.ReadAllBytes(AbsoluteAssetPath(FrozenPath)), Is.EqualTo(frozenBytes),
                        "The G0 frozen material asset changed during this in-memory GPU test.");
                }
            }
        }

        private static Color32[] CapturePair(State state, Material current, Material frozen,
            MeshRenderer renderer, Camera camera, RenderTexture target, Color32[] clear,
            Type flagsType, MethodInfo setFlagBits, StringBuilder evidence)
        {
            var currentPixels = CaptureState(state, current, renderer, camera, target, flagsType, setFlagBits);
            var frozenPixels = CaptureState(state, frozen, renderer, camera, target, flagsType, setFlagBits);
            SavePng(state, "current", currentPixels);
            SavePng(state, "frozen", frozenPixels);
            var different = CountDifferent(currentPixels, frozenPixels);
            var visible = CountDifferent(clear, currentPixels);
            Record(evidence, state + " currentFrozenDifferentPixels=" + different + " visiblePixels=" + visible +
                " currentCenterRGBA=" + currentPixels[(Size / 2) * Size + Size / 2] +
                " frozenCenterRGBA=" + frozenPixels[(Size / 2) * Size + Size / 2]);
            Assert.That(different, Is.Zero, state + " differs from the frozen ShaderLab reference at the same position.");
            Assert.That(visible, Is.GreaterThan(0), state + " is blank; a blank A/B is not evidence.");
            return currentPixels;
        }

        private static Color32[] CaptureState(State state, Material material, MeshRenderer renderer,
            Camera camera, RenderTexture target, Type flagsType, MethodInfo setFlagBits)
        {
            renderer.sharedMaterial = material;
            renderer.SetPropertyBlock(null);
            var materialWord = state == State.MaterialFullWord ? FullWord :
                state == State.MaterialUnseededWord ? HighBit : BaseWord;
            material.SetInteger(Flags1Name, materialWord);
            Assert.That(material.GetInteger(Flags1Name), Is.EqualTo(materialWord));

            if (state == State.SeededMPB || state == State.UnseededMPB)
            {
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                var propertyId = Shader.PropertyToID(Flags1Name);
                if (state == State.SeededMPB)
                    block.SetInteger(propertyId, material.GetInteger(propertyId)); // seed the entire 32-bit word
                var helper = Activator.CreateInstance(flagsType, new object[] { material });
                setFlagBits.Invoke(helper, new object[] { HighBit, block, Flags1Index });
                var expected = state == State.SeededMPB ? FullWord : HighBit;
                Assert.That(block.GetInteger(propertyId), Is.EqualTo(expected), "MPB helper lost packed bits.");
                renderer.SetPropertyBlock(block);
                var applied = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(applied);
                Assert.That(applied.GetInteger(propertyId), Is.EqualTo(expected), "Renderer MPB round-trip lost packed bits.");
            }

            return Capture(camera, target);
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

        private static void SavePng(State state, string side, Color32[] pixels)
        {
            var image = new Texture2D(Size, Size, TextureFormat.RGBA32, false, false);
            try
            {
                image.SetPixels32(pixels);
                image.Apply(false, false);
                var directory = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Library/NBFXG2");
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(Path.Combine(directory, "G2ShaderFlagMPB-" + state + "-" + side + ".png"),
                    image.EncodeToPNG());
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
