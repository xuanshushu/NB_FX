using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;

namespace NBFX.GF.URP.PlayerProbe
{
    // Test-only Player scene component. It writes only to an explicit --gf-output path.
    public sealed class NBGFPlayerProbe : MonoBehaviour
    {
        const int Size = 128;
        public Shader graphShader;
        public Material meshProbeMaterial;

        [Serializable]
        sealed class Result
        {
            public string unityVersion;
            public string graphicsDevice;
            public int alive;
            public bool culled;
            public int meshDeferredChanged;
            public int meshCameraChanged;
            public int meshBaselineCenterRed;
            public int meshDeferredCenterRed;
            public int visiblePixels;
            public int deferredChanged;
            public int cameraChanged;
            public bool passed;
            public string error;
        }

        static Color32[] Capture(Camera camera, RenderTexture target, Texture2D readback, string path)
        {
            camera.Render();
            var old = RenderTexture.active;
            try
            {
                RenderTexture.active = target;
                readback.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
                readback.Apply(false);
                File.WriteAllBytes(path, readback.EncodeToPNG());
                return readback.GetPixels32();
            }
            finally { RenderTexture.active = old; }
        }

        static int Changed(Color32[] a, Color32[] b)
        {
            int count = 0;
            for (int i = 0; i < a.Length; ++i)
                if (Math.Abs(a[i].r - b[i].r) > 2 || Math.Abs(a[i].g - b[i].g) > 2 || Math.Abs(a[i].b - b[i].b) > 2)
                    ++count;
            return count;
        }

        IEnumerator Start()
        {
            string output = null;
            foreach (string arg in Environment.GetCommandLineArgs())
                if (arg.StartsWith("--gf-output=", StringComparison.Ordinal))
                    output = arg.Substring("--gf-output=".Length);
            if (string.IsNullOrEmpty(output)) yield break;
            Directory.CreateDirectory(output);

            var result = new Result
            {
                unityVersion = Application.unityVersion,
                graphicsDevice = SystemInfo.graphicsDeviceType.ToString()
            };
            var camera = Camera.main;
            var vfx = FindFirstObjectByType<VisualEffect>();
            var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
            var readback = new Texture2D(Size, Size, TextureFormat.RGBA32, false, true);
            Texture2D gradient = null;
            Material backgroundMaterial = null;
            GameObject background = null;
            Material meshMaterial = null;
            GameObject meshProbe = null;
            float oldMode = Shader.GetGlobalFloat("_NBGFMode");
            try
            {
                if (camera == null || vfx == null || graphShader == null || meshProbeMaterial == null)
                    throw new Exception("Camera, VFX, Graph Shader or transparent Mesh material missing from GF Player scene.");
                gradient = new Texture2D(Size, Size, TextureFormat.RGBA32, false, true);
                var pixels = new Color32[Size * Size];
                for (int y = 0; y < Size; ++y)
                    for (int x = 0; x < Size; ++x)
                        pixels[y * Size + x] = new Color32((byte)(x * 255 / 127),
                            (byte)(y * 255 / 127), 50, 255);
                gradient.SetPixels32(pixels);
                gradient.Apply(false);
                gradient.filterMode = FilterMode.Point;
                backgroundMaterial = new Material(graphShader);
                backgroundMaterial.SetTexture("_BaseMap", gradient);
                backgroundMaterial.SetColor("_Color", Color.white);
                backgroundMaterial.SetFloat("_Surface", 0);
                backgroundMaterial.SetFloat("_SrcBlend", (float)BlendMode.One);
                backgroundMaterial.SetFloat("_DstBlend", (float)BlendMode.Zero);
                backgroundMaterial.SetFloat("_ZWrite", 1);
                backgroundMaterial.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                backgroundMaterial.renderQueue = 2000;
                background = GameObject.CreatePrimitive(PrimitiveType.Quad);
                background.name = "GF_Player_Gradient_Background";
                background.transform.position = new Vector3(0, 1, -1);
                background.transform.localScale = new Vector3(6, 6, 1);
                background.GetComponent<MeshRenderer>().sharedMaterial = backgroundMaterial;
                camera.targetTexture = target;
                camera.allowHDR = false;
                camera.allowMSAA = false;
                target.Create();
                Shader.SetGlobalFloat("_NBGFMode", 0);
                vfx.gameObject.SetActive(false);
                var baseline = Capture(camera, target, readback, Path.Combine(output, "player_no_particle.png"));
                meshMaterial = new Material(meshProbeMaterial);
                meshProbe = GameObject.CreatePrimitive(PrimitiveType.Quad);
                meshProbe.name = "GF_Player_Mesh_Probe";
                meshProbe.transform.position = new Vector3(0, 1, 2);
                meshProbe.transform.localScale = new Vector3(2, 2, 1);
                meshProbe.GetComponent<MeshRenderer>().sharedMaterial = meshMaterial;
                var meshBaseline = Capture(camera, target, readback, Path.Combine(output, "player_mesh_baseline.png"));
                Shader.SetGlobalFloat("_NBGFMode", 1);
                var meshDeferred = Capture(camera, target, readback, Path.Combine(output, "player_mesh_deferred.png"));
                Shader.SetGlobalFloat("_NBGFMode", 2);
                var meshCamera = Capture(camera, target, readback, Path.Combine(output, "player_mesh_camera.png"));
                result.meshDeferredChanged = Changed(meshBaseline, meshDeferred);
                result.meshCameraChanged = Changed(meshBaseline, meshCamera);
                result.meshBaselineCenterRed = meshBaseline[64 * Size + 64].r;
                result.meshDeferredCenterRed = meshDeferred[64 * Size + 64].r;
                meshProbe.SetActive(false);
                Shader.SetGlobalFloat("_NBGFMode", 0);
                vfx.gameObject.SetActive(true);
                vfx.Reinit();
                // Batchmode has no presented Game view. Render this camera every
                // frame so VFX culling and simulation see a live viewpoint.
                for (int i = 0; i < 60; ++i)
                {
                    camera.Render();
                    yield return null;
                }
                var info = vfx.GetParticleSystemInfo("Simple Loop");
                result.alive = (int)info.aliveCount;
                result.culled = vfx.culled;
                var mode0 = Capture(camera, target, readback, Path.Combine(output, "player_mode0.png"));
                Shader.SetGlobalFloat("_NBGFMode", 1);
                var deferred = Capture(camera, target, readback, Path.Combine(output, "player_deferred.png"));
                Shader.SetGlobalFloat("_NBGFMode", 2);
                var opaque = Capture(camera, target, readback, Path.Combine(output, "player_camera.png"));
                result.visiblePixels = Changed(baseline, mode0);
                result.deferredChanged = Changed(mode0, deferred);
                result.cameraChanged = Changed(mode0, opaque);
                result.passed = result.meshDeferredChanged > 0 && result.meshCameraChanged > 0 &&
                    result.alive > 0 && result.visiblePixels > 0 &&
                    result.deferredChanged > 0 && result.cameraChanged > 0;
            }
            finally
            {
                Shader.SetGlobalFloat("_NBGFMode", oldMode);
                if (camera != null) camera.targetTexture = null;
                target.Release();
                Destroy(target);
                Destroy(readback);
                if (background != null) Destroy(background);
                if (backgroundMaterial != null) Destroy(backgroundMaterial);
                if (gradient != null) Destroy(gradient);
                if (meshProbe != null) Destroy(meshProbe);
                if (meshMaterial != null) Destroy(meshMaterial);
                File.WriteAllText(Path.Combine(output, "result.json"), JsonUtility.ToJson(result, true));
                Debug.Log("NBFX GF Player: " + JsonUtility.ToJson(result));
                Application.Quit(result.passed ? 0 : 1);
            }
        }
    }
}
