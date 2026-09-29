using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;

// Diagnostic only: copy into isolated clone Assets, never the product package.
public sealed class NBFXT08AlphaStaircasePlayerProbe : MonoBehaviour
{
    public VisualEffectAsset alpha100Asset, alpha095Asset, alpha050Asset;
    public Shader graphShader, alpha100Shader, alpha095Shader, alpha050Shader;
    const int Size = 128;
    const int BackgroundLayer = 4, Alpha100Layer = 2, Alpha095Layer = 3, Alpha050Layer = 5;

    [Serializable] sealed class CaseResult
    {
        public string name;
        public int alive, visiblePixelsThreshold2, changedPixelsExact, maxChannelDifference;
        public bool culled;
        public int passCount, opaquePassIndex, deferredPassIndex;
        public float newMaterialSurface, newMaterialAlphaClip, newMaterialSrcBlend, newMaterialDstBlend;
        public bool newMaterialTransparentKeyword, newMaterialAlphaTestKeyword;
        public string newMaterialKeywords;
    }
    [Serializable] sealed class Result
    {
        public string unityVersion, graphicsDevice, note;
        public CaseResult alpha100, alpha095, alpha050;
        public bool validControl;
    }

    static Color32[] Capture(Camera camera, RenderTexture target, Texture2D read, string path)
    {
        camera.Render();
        var previous = RenderTexture.active;
        RenderTexture.active = target;
        try
        {
            read.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
            read.Apply(false);
            File.WriteAllBytes(path, read.EncodeToPNG());
            return read.GetPixels32();
        }
        finally { RenderTexture.active = previous; }
    }
    static CaseResult Measure(string name, Color32[] background, Color32[] candidate,
        VisualEffect visualEffect, Shader shader)
    {
        var result = new CaseResult { name = name,
            alive = (int)visualEffect.GetParticleSystemInfo("Simple Loop").aliveCount,
            culled = visualEffect.culled };
        for (int i = 0; i < background.Length; i++)
        {
            int delta = Math.Max(Math.Abs(background[i].r - candidate[i].r),
                Math.Max(Math.Abs(background[i].g - candidate[i].g),
                    Math.Abs(background[i].b - candidate[i].b)));
            if (delta > 0) result.changedPixelsExact++;
            if (delta > 2) result.visiblePixelsThreshold2++;
            result.maxChannelDifference = Math.Max(result.maxChannelDifference, delta);
        }
        var material = new Material(shader);
        try
        {
            result.passCount = material.passCount;
            result.opaquePassIndex = material.FindPass("NBCameraOpaqueDistortPass");
            result.deferredPassIndex = material.FindPass("NBDeferredDistortPass");
            result.newMaterialSurface = material.HasProperty("_Surface") ? material.GetFloat("_Surface") : float.NaN;
            result.newMaterialAlphaClip = material.HasProperty("_AlphaClip") ? material.GetFloat("_AlphaClip") : float.NaN;
            result.newMaterialSrcBlend = material.HasProperty("_SrcBlend") ? material.GetFloat("_SrcBlend") : float.NaN;
            result.newMaterialDstBlend = material.HasProperty("_DstBlend") ? material.GetFloat("_DstBlend") : float.NaN;
            result.newMaterialTransparentKeyword = material.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT");
            result.newMaterialAlphaTestKeyword = material.IsKeywordEnabled("_ALPHATEST_ON");
            result.newMaterialKeywords = string.Join(",", material.shaderKeywords);
        }
        finally { Destroy(material); }
        return result;
    }
    static VisualEffect Create(string label, int layer, VisualEffectAsset asset)
    {
        var go = new GameObject(label);
        go.layer = layer;
        go.transform.position = new Vector3(0, 0, 2);
        var effect = go.AddComponent<VisualEffect>();
        effect.resetSeedOnPlay = false;
        effect.startSeed = 12345;
        effect.visualEffectAsset = asset;
        effect.Reinit();
        return effect;
    }

    IEnumerator Start()
    {
        string output = null;
        foreach (var arg in Environment.GetCommandLineArgs())
            if (arg.StartsWith("--nbfx-alpha-staircase-output=", StringComparison.Ordinal))
                output = arg.Substring("--nbfx-alpha-staircase-output=".Length);
        if (string.IsNullOrEmpty(output)) yield break;
        Directory.CreateDirectory(output);
        var result = new Result { unityVersion = Application.unityVersion,
            graphicsDevice = SystemInfo.graphicsDeviceType.ToString(),
            note = "newMaterial* fields describe freshly constructed Material(shader), NOT hidden VFX renderer material" };
        RenderTexture target = null;
        Texture2D read = null, gradient = null;
        Material backgroundMaterial = null;
        GameObject background = null, cameraObject = null;
        VisualEffect alpha100 = null, alpha095 = null, alpha050 = null;
        try
        {
            if (!alpha100Asset || !alpha095Asset || !alpha050Asset ||
                !graphShader || !alpha100Shader || !alpha095Shader || !alpha050Shader)
                throw new Exception("Alpha staircase assets absent");
            gradient = new Texture2D(Size, Size, TextureFormat.RGBA32, false, true);
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++)
                pixels[y * Size + x] = new Color32((byte)(x * 255 / 127), (byte)(y * 255 / 127), 50, 255);
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
            background.layer = BackgroundLayer;
            background.transform.position = new Vector3(0, 1, 1);
            background.transform.localScale = new Vector3(6, 6, 1);
            background.GetComponent<MeshRenderer>().sharedMaterial = backgroundMaterial;
            cameraObject = new GameObject("Alpha Staircase Probe Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 3;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 20;
            camera.transform.position = new Vector3(0, 1, 8);
            camera.transform.rotation = Quaternion.Euler(0, 180, 0);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
            target.Create();
            camera.targetTexture = target;
            read = new Texture2D(Size, Size, TextureFormat.RGBA32, false, true);
            alpha100 = Create("Alpha100 VFX", Alpha100Layer, alpha100Asset);
            alpha095 = Create("Alpha095 VFX", Alpha095Layer, alpha095Asset);
            alpha050 = Create("Alpha050 VFX", Alpha050Layer, alpha050Asset);
            camera.cullingMask = (1 << BackgroundLayer) | (1 << Alpha100Layer) |
                (1 << Alpha095Layer) | (1 << Alpha050Layer);
            for (int i = 0; i < 3; i++) { camera.Render(); yield return null; }
            alpha100.Simulate(1f / 60f, 21u);
            alpha095.Simulate(1f / 60f, 21u);
            alpha050.Simulate(1f / 60f, 21u);
            for (int i = 0; i < 2; i++) { camera.Render(); yield return null; }
            camera.cullingMask = 1 << BackgroundLayer;
            var baseline = Capture(camera, target, read, Path.Combine(output, "background.png"));
            camera.cullingMask = (1 << BackgroundLayer) | (1 << Alpha100Layer);
            var image100 = Capture(camera, target, read, Path.Combine(output, "alpha100.png"));
            camera.cullingMask = (1 << BackgroundLayer) | (1 << Alpha095Layer);
            var image095 = Capture(camera, target, read, Path.Combine(output, "alpha095.png"));
            camera.cullingMask = (1 << BackgroundLayer) | (1 << Alpha050Layer);
            var image050 = Capture(camera, target, read, Path.Combine(output, "alpha050.png"));
            result.alpha100 = Measure("alpha1.00", baseline, image100, alpha100, alpha100Shader);
            result.alpha095 = Measure("alpha0.95", baseline, image095, alpha095, alpha095Shader);
            result.alpha050 = Measure("alpha0.50", baseline, image050, alpha050, alpha050Shader);
            result.validControl = result.graphicsDevice == "Metal" &&
                result.alpha100.alive > 0 && !result.alpha100.culled &&
                result.alpha100.visiblePixelsThreshold2 > 0 &&
                result.alpha100.passCount == 5 && result.alpha100.opaquePassIndex == 1 &&
                result.alpha100.deferredPassIndex == 2;
        }
        finally
        {
            File.WriteAllText(Path.Combine(output, "result.json"), JsonUtility.ToJson(result, true));
            Debug.Log("NBFX_T08_ALPHA_STAIRCASE_PLAYER " + JsonUtility.ToJson(result));
            if (target) { target.Release(); Destroy(target); }
            if (read) Destroy(read);
            if (gradient) Destroy(gradient);
            if (backgroundMaterial) Destroy(backgroundMaterial);
            if (background) Destroy(background);
            if (cameraObject) Destroy(cameraObject);
            if (alpha100) Destroy(alpha100.gameObject);
            if (alpha095) Destroy(alpha095.gameObject);
            if (alpha050) Destroy(alpha050.gameObject);
            Application.Quit(result.validControl ? 0 : 1);
        }
    }
}
