using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class GFFunctionalProbe
{
    const string GraphPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/Tests/PassFeasibility/SubTargetProbe/GF_URP_VFX_NBSubTarget.shadergraph";
    const int Size = 128;

    static Color32[] Capture(Camera camera, RenderTexture target, Texture2D readback, string path)
    {
        camera.Render();
        RenderTexture old = RenderTexture.active;
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

    static string Pixel(Color32[] pixels, int x, int y)
    {
        var c = pixels[y * Size + x];
        return c.r + "," + c.g + "," + c.b + "," + c.a;
    }

    static int Changed(Color32[] a, Color32[] b)
    {
        int count = 0;
        for (int i = 0; i < a.Length; ++i)
            if (Math.Abs(a[i].r - b[i].r) > 2 || Math.Abs(a[i].g - b[i].g) > 2 || Math.Abs(a[i].b - b[i].b) > 2)
                count++;
        return count;
    }

    public static string Run()
    {
        var root = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/NBFXGF");
        Directory.CreateDirectory(root);
        var asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (asset == null) throw new Exception("Active pipeline is not URP");
        var feature = asset.rendererDataList[0].rendererFeatures.FirstOrDefault(x => x.GetType().FullName == "NBShader.NBPostProcess");
        if (feature == null || !feature.isActive) throw new Exception("NBPostProcess feature is absent or inactive");
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(GraphPath);
        if (shader == null || !shader.isSupported) throw new Exception("GF Graph unavailable");
        var bgShader = shader;
        var probeMat = new Material(shader);
        var bgMat = new Material(bgShader);
        var gradient = new Texture2D(Size, Size, TextureFormat.RGBA32, false, true);
        var readback = new Texture2D(Size, Size, TextureFormat.RGBA32, false, true);
        var cameraObj = new GameObject("GF_TEMP_CAMERA");
        var background = GameObject.CreatePrimitive(PrimitiveType.Quad);
        var foreground = GameObject.CreatePrimitive(PrimitiveType.Quad);
        var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
        var maskCopy = new RenderTexture(Size, Size, 0, RenderTextureFormat.ARGBHalf);
        string callbackInfo = "not called";
        Action<ScriptableRenderContext,Camera> callback = (ctx,camera) => { if (camera != null && camera.name == "GF_TEMP_CAMERA") { var tex=Shader.GetGlobalTexture("_DisturbanceMaskTex"); callbackInfo = tex==null ? "null" : tex.GetType().Name+":"+tex.width+"x"+tex.height; if(tex!=null) Graphics.Blit(tex,maskCopy); } };
        RenderPipelineManager.endCameraRendering += callback;
        float oldMode = Shader.GetGlobalFloat("_NBGFMode");
        bool oldFeatureActive = feature.isActive;
        var oldActive = RenderTexture.active;
        try
        {
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; ++y)
                for (int x = 0; x < Size; ++x)
                    pixels[y * Size + x] = new Color32((byte)(x * 255 / (Size - 1)), (byte)(y * 255 / (Size - 1)), 50, 255);
            gradient.SetPixels32(pixels);
            gradient.Apply(false);
            gradient.filterMode = FilterMode.Point;
            probeMat.SetColor("_Color", new Color(1f, 1f, 1f, 0f));
            probeMat.SetFloat("_Surface",1); probeMat.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha); probeMat.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha); probeMat.SetFloat("_ZWrite",0); probeMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            bgMat.SetFloat("_Surface",0); bgMat.SetFloat("_SrcBlend",(float)BlendMode.One); bgMat.SetFloat("_DstBlend",(float)BlendMode.Zero); bgMat.SetFloat("_ZWrite",1); bgMat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT"); bgMat.renderQueue=2000;
            probeMat.SetTexture("_BaseMap", Texture2D.whiteTexture);
            bgMat.SetTexture("_BaseMap", gradient);
            bgMat.SetColor("_Color", Color.white);
            background.layer = foreground.layer = 2;
            background.transform.position = new Vector3(0, 0, 1);
            background.transform.localScale = new Vector3(2, 2, 1);
            foreground.transform.position = new Vector3(0, 0, 2);
            background.GetComponent<MeshRenderer>().sharedMaterial = bgMat;
            foreground.GetComponent<MeshRenderer>().sharedMaterial = probeMat;
            var cam = cameraObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.orthographic = true;
            cam.orthographicSize = 1;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 10;
            cam.allowHDR = false;
            cam.allowMSAA = false;
            cam.cullingMask = 1 << 2;
            cam.transform.position = new Vector3(0, 0, 3);
            cam.transform.rotation = Quaternion.Euler(0, 180, 0);
            cam.targetTexture = target;
            target.Create();

            Shader.SetGlobalFloat("_NBGFMode", 0);
            feature.SetActive(false);
            foreground.SetActive(false);
            var noFront = Capture(cam, target, readback, Path.Combine(root, "gf_functional_no_front.png"));
            foreground.SetActive(true);
            var noPost = Capture(cam, target, readback, Path.Combine(root, "gf_functional_no_post.png"));
            feature.SetActive(true);
            var baseline = Capture(cam, target, readback, Path.Combine(root, "gf_functional_baseline.png"));
            Shader.SetGlobalFloat("_NBGFMode", 1);
            var deferred = Capture(cam, target, readback, Path.Combine(root, "gf_functional_deferred.png"));
            var mask = Shader.GetGlobalTexture("_DisturbanceMaskTex");
            string maskInfo = mask == null ? "null" : mask.GetType().Name + ":" + mask.width + "x" + mask.height;
            Shader.SetGlobalFloat("_NBGFMode", 2);
            var opaque = Capture(cam, target, readback, Path.Combine(root, "gf_functional_camera.png"));
            return "graphPasses=" + shader.passCount + ";cameraPass=" + probeMat.FindPass("NBCameraOpaqueDistortPass") +
                ";maskPass=" + probeMat.FindPass("NBDeferredDistortPass") + ";noFrontCenter=" + Pixel(noFront,64,64) + ";noFrontLeft=" + Pixel(noFront,16,64) + ";noFrontRight=" + Pixel(noFront,112,64) + ";noPostCenter=" + Pixel(noPost, 64, 64) + ";baselineCenter=" + Pixel(baseline, 64, 64) +
                ";deferredCenter=" + Pixel(deferred, 64, 64) + ";cameraCenter=" + Pixel(opaque, 64, 64) +
                ";deferredChanged=" + Changed(baseline, deferred) + ";cameraChanged=" + Changed(baseline, opaque) +
                ";probeColor=" + probeMat.GetColor("_Color") + ";probeBlend=" + probeMat.GetFloat("_SrcBlend") + "," + probeMat.GetFloat("_DstBlend") + ";probeTransparent=" + probeMat.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT") + ";maskGlobal=" + maskInfo + ";callback="+callbackInfo+";pngRoot=" + root;
        }
        finally
        {
            RenderPipelineManager.endCameraRendering -= callback;
            maskCopy.Release(); UnityEngine.Object.DestroyImmediate(maskCopy);
            Shader.SetGlobalFloat("_NBGFMode", oldMode);
            feature.SetActive(oldFeatureActive);
            RenderTexture.active = oldActive;
            if (cameraObj != null && cameraObj.GetComponent<Camera>() != null) cameraObj.GetComponent<Camera>().targetTexture = null;
            if (target != null) target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(cameraObj);
            UnityEngine.Object.DestroyImmediate(background);
            UnityEngine.Object.DestroyImmediate(foreground);
            UnityEngine.Object.DestroyImmediate(probeMat);
            UnityEngine.Object.DestroyImmediate(bgMat);
            UnityEngine.Object.DestroyImmediate(gradient);
            UnityEngine.Object.DestroyImmediate(readback);
        }
    }
}
