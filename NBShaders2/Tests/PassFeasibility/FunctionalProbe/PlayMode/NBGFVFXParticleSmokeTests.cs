using System.Collections;
using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.VFX;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NBFX.GF.URP.Tests
{
    public sealed class NBGFVFXParticleSmokeTests
    {
#if UNITY_EDITOR
        const string VfxPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/Tests/PassFeasibility/SubTargetProbe/GF_VFXOutput_NBSubTarget.vfx";
        const string GraphPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/Tests/PassFeasibility/SubTargetProbe/GF_URP_VFX_NBSubTarget.shadergraph";

        static Color32[] Capture(Camera camera, RenderTexture target, Texture2D readback, string path)
        {
            camera.Render();
            var previous = RenderTexture.active;
            try
            {
                RenderTexture.active = target;
                readback.ReadPixels(new Rect(0, 0, 128, 128), 0, 0);
                readback.Apply(false);
                File.WriteAllBytes(path, readback.EncodeToPNG());
                return readback.GetPixels32();
            }
            finally { RenderTexture.active = previous; }
        }

        static int Changed(Color32[] a, Color32[] b)
        {
            int count = 0;
            for (int i = 0; i < a.Length; ++i)
                if (Math.Abs(a[i].r - b[i].r) > 2 || Math.Abs(a[i].g - b[i].g) > 2 || Math.Abs(a[i].b - b[i].b) > 2)
                    ++count;
            return count;
        }

        [UnityTest]
        public IEnumerator RealShaderGraphParticleOutputSpawns()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(VfxPath);
            Assert.That(asset, Is.Not.Null);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(GraphPath);
            Assert.That(shader, Is.Not.Null);
            var cameraObject = new GameObject("NBGF_TMP_VFX_Camera");
            var vfxObject = new GameObject("NBGF_TMP_VFX_Particles");
            var background = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var target = new RenderTexture(128, 128, 24);
            var readback = new Texture2D(128, 128, TextureFormat.RGBA32, false, true);
            var gradient = new Texture2D(128, 128, TextureFormat.RGBA32, false, true);
            var backgroundMaterial = new Material(shader);
            float previousMode = Shader.GetGlobalFloat("_NBGFMode");
            try
            {
                var pixels = new Color32[128 * 128];
                for (int y = 0; y < 128; ++y)
                    for (int x = 0; x < 128; ++x)
                        pixels[y * 128 + x] = new Color32((byte)(x * 255 / 127), (byte)(y * 255 / 127), 50, 255);
                gradient.SetPixels32(pixels);
                gradient.Apply(false);
                gradient.filterMode = FilterMode.Point;
                backgroundMaterial.SetTexture("_BaseMap", gradient);
                backgroundMaterial.SetColor("_Color", Color.white);
                backgroundMaterial.SetFloat("_Surface", 0);
                backgroundMaterial.SetFloat("_SrcBlend", (float)BlendMode.One);
                backgroundMaterial.SetFloat("_DstBlend", (float)BlendMode.Zero);
                backgroundMaterial.SetFloat("_ZWrite", 1);
                backgroundMaterial.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                backgroundMaterial.renderQueue = 2000;
                background.layer = 2;
                background.transform.position = new Vector3(0, 1, 1);
                background.transform.localScale = new Vector3(6, 6, 1);
                background.GetComponent<MeshRenderer>().sharedMaterial = backgroundMaterial;

                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 3;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 20;
                camera.cullingMask = 1 << 2;
                camera.transform.position = new Vector3(0, 1, 8);
                camera.transform.rotation = Quaternion.Euler(0, 180, 0);
                camera.targetTexture = target;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.allowHDR = false;
                camera.allowMSAA = false;
                target.Create();

                vfxObject.layer = 2;
                vfxObject.transform.position = new Vector3(0, 0, 2);
                var vfx = vfxObject.AddComponent<VisualEffect>();
                vfx.visualEffectAsset = asset;
                var outputDirectory = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/NBFXGF");
                Directory.CreateDirectory(outputDirectory);
                Shader.SetGlobalFloat("_NBGFMode", 0);
                vfxObject.SetActive(false);
                var noVfx = Capture(camera, target, readback, Path.Combine(outputDirectory, "gf_vfx_no_particle.png"));
                vfxObject.SetActive(true);
                vfx.Reinit();
                for (var i = 0; i < 30; ++i)
                    yield return null;

                var info = vfx.GetParticleSystemInfo("Simple Loop");
                Debug.Log("NBFX GF VFX smoke: alive=" + info.aliveCount +
                          ", culled=" + vfx.culled + ", frame=" + Time.frameCount);
                Assert.That(info.aliveCount, Is.GreaterThan(0),
                    "Actual particles, not only a generated VFX shader, are required.");
                var noDistort = Capture(camera, target, readback, Path.Combine(outputDirectory, "gf_vfx_mode0.png"));
                Shader.SetGlobalFloat("_NBGFMode", 1);
                var deferred = Capture(camera, target, readback, Path.Combine(outputDirectory, "gf_vfx_deferred.png"));
                Shader.SetGlobalFloat("_NBGFMode", 2);
                var opaque = Capture(camera, target, readback, Path.Combine(outputDirectory, "gf_vfx_camera.png"));
                int visiblePixels = Changed(noVfx, noDistort);
                int deferredChanged = Changed(noDistort, deferred);
                int cameraChanged = Changed(noDistort, opaque);
                Debug.Log("NBFX GF VFX pixels: visible=" + visiblePixels +
                          ", deferred=" + deferredChanged + ", camera=" + cameraChanged);
                Assert.That(visiblePixels, Is.GreaterThan(0), "Particles must visibly render.");
                Assert.That(deferredChanged, Is.GreaterThan(0), "Deferred-mode output must differ in the same frame.");
                Assert.That(cameraChanged, Is.GreaterThan(0), "Camera-opaque-mode output must differ in the same frame.");
            }
            finally
            {
                Shader.SetGlobalFloat("_NBGFMode", previousMode);
                var camera = cameraObject.GetComponent<Camera>();
                if (camera != null)
                    camera.targetTexture = null;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(vfxObject);
                UnityEngine.Object.DestroyImmediate(background);
                UnityEngine.Object.DestroyImmediate(backgroundMaterial);
                UnityEngine.Object.DestroyImmediate(gradient);
                UnityEngine.Object.DestroyImmediate(readback);
            }
        }
#endif
    }
}
