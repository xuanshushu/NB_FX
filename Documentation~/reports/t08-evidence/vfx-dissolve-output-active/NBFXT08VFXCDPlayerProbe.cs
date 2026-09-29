using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;

// Isolated diagnostic only: do not include in Packages/NB_FX.
public sealed class NBFXT08VFXCDPlayerProbe : MonoBehaviour
{
    public VisualEffectAsset offAsset, singleAsset, processMaskAsset;
    public Shader graphShader, offShader, singleShader, processMaskShader;
    const int Size = 128;
    // RendererData transparentLayerMask=55 permits 1,2,4,5; layer 3 is excluded.
    const int BackgroundLayer = 4, OffLayer = 2, SingleLayer = 1, ProcessMaskLayer = 5;

    [Serializable] sealed class CaseResult
    {
        public string name, newMaterialKeywords, newMaterialDissolveTexture, newMaterialDissolveMaskTexture;
        public int alive, visiblePixels, exactChangedPixels, maxChannelDifference;
        public int passCount, opaqueIndex, deferredIndex;
        public bool culled, transparentKeyword, alphaTestKeyword;
        public float surface, alphaClip, srcBlend, dstBlend;
    }
    [Serializable] sealed class Result
    {
        public string unityVersion, graphicsDevice, note;
        public CaseResult off, single, processMask;
        public int changedSingleVsOff, changedProcessVsSingle, changedProcessVsOff;
        public bool validControl, observedSingleDifference, observedProcessDifference;
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
    static int MaxDifference(Color32 a, Color32 b)
    {
        return Math.Max(Math.Abs(a.r - b.r), Math.Max(Math.Abs(a.g - b.g), Math.Abs(a.b - b.b)));
    }
    static int Changed(Color32[] a, Color32[] b)
    {
        int count = 0;
        for (int i = 0; i < a.Length; i++) if (MaxDifference(a[i], b[i]) > 2) count++;
        return count;
    }
    static CaseResult Measure(string label, Color32[] background, Color32[] candidate,
        VisualEffect effect, Shader shader)
    {
        var result = new CaseResult { name = label,
            alive = (int)effect.GetParticleSystemInfo("Simple Loop").aliveCount,
            culled = effect.culled };
        for (int i = 0; i < background.Length; i++)
        {
            int difference = MaxDifference(background[i], candidate[i]);
            if (difference > 0) result.exactChangedPixels++;
            if (difference > 2) result.visiblePixels++;
            result.maxChannelDifference = Math.Max(result.maxChannelDifference, difference);
        }
        var material = new Material(shader);
        try
        {
            result.passCount = material.passCount;
            result.opaqueIndex = material.FindPass("NBCameraOpaqueDistortPass");
            result.deferredIndex = material.FindPass("NBDeferredDistortPass");
            result.surface = material.GetFloat("_Surface");
            result.alphaClip = material.GetFloat("_AlphaClip");
            result.srcBlend = material.GetFloat("_SrcBlend");
            result.dstBlend = material.GetFloat("_DstBlend");
            result.transparentKeyword = material.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT");
            result.alphaTestKeyword = material.IsKeywordEnabled("_ALPHATEST_ON");
            result.newMaterialKeywords = string.Join(",", material.shaderKeywords);
            var dissolve = material.GetTexture("_DissolveMap");
            var dissolveMask = material.GetTexture("_DissolveMaskMap");
            result.newMaterialDissolveTexture = dissolve ? dissolve.name : "NULL";
            result.newMaterialDissolveMaskTexture = dissolveMask ? dissolveMask.name : "NULL";
        }
        finally { Destroy(material); }
        return result;
    }
    static VisualEffect Create(string name, int layer, VisualEffectAsset asset)
    {
        var go = new GameObject(name);
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
            if (arg.StartsWith("--nbfx-vfx-cd-output=", StringComparison.Ordinal))
                output = arg.Substring("--nbfx-vfx-cd-output=".Length);
        if (string.IsNullOrEmpty(output)) yield break;
        Directory.CreateDirectory(output);
        var result = new Result { unityVersion = Application.unityVersion,
            graphicsDevice = SystemInfo.graphicsDeviceType.ToString(),
            note = "new Material(shader) fields are defaults, not the hidden VFX Output material" };
        RenderTexture target = null;
        Texture2D read = null, gradient = null;
        Material backgroundMaterial = null;
        GameObject background = null, cameraObject = null;
        VisualEffect off = null, single = null, processMask = null;
        try
        {
            if (!offAsset || !singleAsset || !processMaskAsset || !graphShader ||
                !offShader || !singleShader || !processMaskShader)
                throw new Exception("VFX C/D probe assets absent");
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
            cameraObject = new GameObject("VFX C/D Probe Camera");
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
            off = Create("Dissolve Off", OffLayer, offAsset);
            single = Create("Dissolve Single", SingleLayer, singleAsset);
            processMask = Create("Dissolve Process Mask", ProcessMaskLayer, processMaskAsset);
            camera.cullingMask = (1 << BackgroundLayer) | (1 << OffLayer) |
                (1 << SingleLayer) | (1 << ProcessMaskLayer);
            for (int i = 0; i < 3; i++) { camera.Render(); yield return null; }
            off.Simulate(1f / 60f, 21u);
            single.Simulate(1f / 60f, 21u);
            processMask.Simulate(1f / 60f, 21u);
            for (int i = 0; i < 2; i++) { camera.Render(); yield return null; }
            camera.cullingMask = 1 << BackgroundLayer;
            var baseline = Capture(camera, target, read, Path.Combine(output, "background.png"));
            camera.cullingMask = (1 << BackgroundLayer) | (1 << OffLayer);
            var offImage = Capture(camera, target, read, Path.Combine(output, "off.png"));
            camera.cullingMask = (1 << BackgroundLayer) | (1 << SingleLayer);
            var singleImage = Capture(camera, target, read, Path.Combine(output, "single.png"));
            camera.cullingMask = (1 << BackgroundLayer) | (1 << ProcessMaskLayer);
            var processImage = Capture(camera, target, read, Path.Combine(output, "process-mask.png"));
            result.off = Measure("off", baseline, offImage, off, offShader);
            result.single = Measure("single", baseline, singleImage, single, singleShader);
            result.processMask = Measure("process-mask", baseline, processImage, processMask, processMaskShader);
            result.changedSingleVsOff = Changed(offImage, singleImage);
            result.changedProcessVsSingle = Changed(processImage, singleImage);
            result.changedProcessVsOff = Changed(processImage, offImage);
            result.validControl = result.graphicsDevice == "Metal" && result.off.alive > 0 &&
                !result.off.culled && result.off.visiblePixels > 0 && result.off.passCount == 5 &&
                result.off.opaqueIndex == 1 && result.off.deferredIndex == 2;
            result.observedSingleDifference = result.single.alive > 0 && !result.single.culled &&
                result.single.visiblePixels > 0 && result.changedSingleVsOff > 0;
            result.observedProcessDifference = result.processMask.alive > 0 && !result.processMask.culled &&
                result.processMask.visiblePixels > 0 && result.changedProcessVsSingle > 0;
        }
        finally
        {
            File.WriteAllText(Path.Combine(output, "result.json"), JsonUtility.ToJson(result, true));
            Debug.Log("NBFX_T08_VFX_CD_PLAYER " + JsonUtility.ToJson(result));
            if (target) { target.Release(); Destroy(target); }
            if (read) Destroy(read);
            if (gradient) Destroy(gradient);
            if (backgroundMaterial) Destroy(backgroundMaterial);
            if (background) Destroy(background);
            if (cameraObject) Destroy(cameraObject);
            if (off) Destroy(off.gameObject);
            if (single) Destroy(single.gameObject);
            if (processMask) Destroy(processMask.gameObject);
            Application.Quit(result.validControl ? 0 : 1);
        }
    }
}
