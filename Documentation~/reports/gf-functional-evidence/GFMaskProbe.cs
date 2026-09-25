using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

sealed class GFMaskCaptureFeature : ScriptableRendererFeature
{
    public Material material;
    GFMaskCapturePass pass;
    public override void Create()
    {
        pass = new GFMaskCapturePass(material);
        pass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents + 1;
    }
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Game) renderer.EnqueuePass(pass);
    }
    sealed class GFMaskCapturePass : ScriptableRenderPass
    {
        readonly Material material;
        static readonly int MaskID = Shader.PropertyToID("_DisturbanceMaskTex");
        sealed class PassData { public Material material; }
        public GFMaskCapturePass(Material material) { this.material = material; }
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resources = frameData.Get<UniversalResourceData>();
            if (!resources.activeColorTexture.IsValid()) return;
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("GF Mask View", out var data))
            {
                data.material = material;
                builder.UseGlobalTexture(MaskID, AccessFlags.Read);
                builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.ReadWrite);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc((PassData d, RasterGraphContext context) =>
                {
                    context.cmd.DrawProcedural(Matrix4x4.identity, d.material, 0, MeshTopology.Triangles, 3, 1);
                });
            }
        }
    }
}

public static class GFMaskProbe
{
    const int Size = 128;
    const string GraphPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/Tests/PassFeasibility/SubTargetProbe/GF_URP_VFX_NBSubTarget.shadergraph";
    const string MaskShaderPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/Tests/PassFeasibility/FunctionalProbe/NBGFMaskView.shader";
    static Color32[] Capture(Camera camera, RenderTexture rt, Texture2D readback, string path)
    {
        camera.Render();
        var previous = RenderTexture.active;
        try
        {
            RenderTexture.active = rt;
            readback.ReadPixels(new Rect(0,0,Size,Size),0,0);
            readback.Apply(false);
            File.WriteAllBytes(path,readback.EncodeToPNG());
            return readback.GetPixels32();
        }
        finally { RenderTexture.active = previous; }
    }
    static string Pixel(Color32[] a,int x,int y) { var c=a[y*Size+x]; return c.r+","+c.g+","+c.b+","+c.a; }
    static int Changed(Color32[] a,Color32[] b)
    {
        int count=0;for(int i=0;i<a.Length;i++) if(Math.Abs(a[i].r-b[i].r)>2||Math.Abs(a[i].g-b[i].g)>2||Math.Abs(a[i].b-b[i].b)>2)++count;return count;
    }
    public static string Run()
    {
        var root=Path.Combine(Path.GetDirectoryName(Application.dataPath),"Temp/NBFXGF"); Directory.CreateDirectory(root);
        var asset=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if(asset==null)throw new Exception("No URP");
        var data=asset.rendererDataList[0];
        var originalFeature=data.rendererFeatures.FirstOrDefault(x=>x.GetType().FullName=="NBShader.NBPostProcess");
        if(originalFeature==null||!originalFeature.isActive)throw new Exception("NBPostprocess inactive");
        var graph=AssetDatabase.LoadAssetAtPath<Shader>(GraphPath);
        var maskShader=AssetDatabase.LoadAssetAtPath<Shader>(MaskShaderPath);
        if(graph==null||maskShader==null||!maskShader.isSupported)throw new Exception("Probe Shader unavailable");
        var foregroundMaterial=new Material(graph);
        var backgroundMaterial=new Material(graph);
        var maskMaterial=new Material(maskShader);
        var capture=ScriptableObject.CreateInstance<GFMaskCaptureFeature>();
        capture.material=maskMaterial;capture.Create();capture.SetActive(true);
        var gradient=new Texture2D(Size,Size,TextureFormat.RGBA32,false,true);
        var readback=new Texture2D(Size,Size,TextureFormat.RGBA32,false,true);
        var background=GameObject.CreatePrimitive(PrimitiveType.Quad);
        var foreground=GameObject.CreatePrimitive(PrimitiveType.Quad);
        var cameraObject=new GameObject("GF_MASK_CAMERA");
        var rt=new RenderTexture(Size,Size,24,RenderTextureFormat.ARGB32);
        var oldActive=RenderTexture.active;
        float oldMode=Shader.GetGlobalFloat("_NBGFMode");
        int oldFeatureCount=data.rendererFeatures.Count;
        try
        {
            data.rendererFeatures.Add(capture); data.SetDirty();
            var pixels=new Color32[Size*Size];
            for(int y=0;y<Size;y++)for(int x=0;x<Size;x++)pixels[y*Size+x]=new Color32((byte)(x*255/127),(byte)(y*255/127),50,255);
            gradient.SetPixels32(pixels); gradient.Apply(false);gradient.filterMode=FilterMode.Point;
            backgroundMaterial.SetTexture("_BaseMap",gradient);backgroundMaterial.SetColor("_Color",Color.white);
            backgroundMaterial.SetFloat("_Surface",0);backgroundMaterial.SetFloat("_SrcBlend",(float)BlendMode.One);backgroundMaterial.SetFloat("_DstBlend",(float)BlendMode.Zero);backgroundMaterial.SetFloat("_ZWrite",1);backgroundMaterial.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");backgroundMaterial.renderQueue=2000;
            foregroundMaterial.SetTexture("_BaseMap",Texture2D.whiteTexture);foregroundMaterial.SetColor("_Color",new Color(1,1,1,0));foregroundMaterial.SetFloat("_Surface",1);foregroundMaterial.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);foregroundMaterial.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);foregroundMaterial.SetFloat("_ZWrite",0);foregroundMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            background.layer=foreground.layer=2; background.transform.position=new Vector3(0,0,1);background.transform.localScale=new Vector3(2,2,1);foreground.transform.position=new Vector3(0,0,2);
            background.GetComponent<MeshRenderer>().sharedMaterial=backgroundMaterial;foreground.GetComponent<MeshRenderer>().sharedMaterial=foregroundMaterial;
            var camera=cameraObject.AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.orthographic=true;camera.orthographicSize=1;camera.nearClipPlane=.1f;camera.farClipPlane=10;camera.allowHDR=false;camera.allowMSAA=false;camera.cullingMask=1<<2;camera.transform.position=new Vector3(0,0,3);camera.transform.rotation=Quaternion.Euler(0,180,0);camera.targetTexture=rt;rt.Create();
            Shader.SetGlobalFloat("_NBGFMode",0);var baseline=Capture(camera,rt,readback,Path.Combine(root,"gf_mask_mesh_baseline.png"));
            Shader.SetGlobalFloat("_NBGFMode",1);var deferred=Capture(camera,rt,readback,Path.Combine(root,"gf_mask_mesh_deferred.png"));
            Shader.SetGlobalFloat("_NBGFMode",2);var opaque=Capture(camera,rt,readback,Path.Combine(root,"gf_mask_mesh_camera.png"));
            return "baseline="+Pixel(baseline,64,64)+";deferred="+Pixel(deferred,64,64)+";camera="+Pixel(opaque,64,64)+";deferredChanged="+Changed(baseline,deferred)+";cameraChanged="+Changed(baseline,opaque)+";featureCount="+oldFeatureCount+","+data.rendererFeatures.Count;
        }
        finally
        {
            data.rendererFeatures.Remove(capture);data.SetDirty();
            Shader.SetGlobalFloat("_NBGFMode",oldMode);RenderTexture.active=oldActive;
            var cam=cameraObject.GetComponent<Camera>();if(cam!=null)cam.targetTexture=null;
            rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(background);UnityEngine.Object.DestroyImmediate(foreground);UnityEngine.Object.DestroyImmediate(foregroundMaterial);UnityEngine.Object.DestroyImmediate(backgroundMaterial);UnityEngine.Object.DestroyImmediate(maskMaterial);UnityEngine.Object.DestroyImmediate(gradient);UnityEngine.Object.DestroyImmediate(readback);UnityEngine.Object.DestroyImmediate(capture);
        }
    }
}
