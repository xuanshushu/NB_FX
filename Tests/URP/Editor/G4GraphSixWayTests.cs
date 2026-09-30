using System;
using System.IO;
using System.Collections.Generic;
using System.IO.Compression;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace NBFX.Baseline.Tests
{
    /// <summary>Ordinary Mesh L1 SixWay A=Frozen, B=current ShaderLab, C=Graph.
    /// Genuine vertex SH is required; no VFX/Player/VertexLights/Forward+ claim.</summary>
    public sealed class G4GraphSixWayTests
    {
        const string FrozenPath = "Packages/com.xuanxuan.nb.fx/Tests/Baseline/Frozen/NBShaders2/Shader/NBShader.shader";
        const string CurrentPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader";
        const string GraphPath = "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph";
        const int Size = 96, Min = 26, Max = 70, Layer = 2;
        [Serializable] sealed class Record
        {
            public string caseId, api, unityVersion, limitation;
            public int abDiff, bcDiff, aRepeatDiff, bRepeatDiff, cRepeatDiff, bControlDiff, cControlDiff;
            public float abMax, bcMax, aRepeatMax, bRepeatMax, cRepeatMax, bControlMax, cControlMax;
            public bool finite;
        }
        [OneTimeSetUp] public void WarmGraph()
            => AssetDatabase.ImportAsset(GraphPath, ImportAssetOptions.ForceUpdate |
                ImportAssetOptions.ForceSynchronousImport);
        [TestCase(true)] [TestCase(false)]
        public void SixWayOrdinaryMeshABC(bool ortho)
        {
            Assert.That(GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset, Is.Not.Null);
            Assert.That(SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf), Is.True);
            Shader fa = AssetDatabase.LoadAssetAtPath<Shader>(FrozenPath);
            Shader fb = AssetDatabase.LoadAssetAtPath<Shader>(CurrentPath);
            Shader fc = AssetDatabase.LoadAssetAtPath<Shader>(GraphPath);
            Assert.That(fa && fb && fc && fa.isSupported && fb.isSupported, Is.True);
            Assert.That(fa.name, Is.EqualTo("Effects/NBShader_T00_Frozen"));
            string output = Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR");
            if (string.IsNullOrEmpty(output)) output = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/NBFXG4SixWay");
            output = Path.Combine(output, ortho ? "ortho" : "perspective"); Directory.CreateDirectory(output);
            Scene scene = SceneManager.GetActiveScene();
            Assert.That(scene.IsValid() && scene.isLoaded, Is.True);
            var oldAmbientMode = RenderSettings.ambientMode;
            var oldProbe = RenderSettings.ambientProbe;
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var cameraGo = new GameObject("L1 SixWay camera");
            var lightGo = new GameObject("L1 SixWay light", typeof(Light));
            var a = new Material(fa); var b = new Material(fb); var c = new Material(fc);
            var baseMap = new Texture2D(2, 2, TextureFormat.RGBAHalf, true, true);
            for (int y=0;y<2;y++) for (int x=0;x<2;x++) baseMap.SetPixel(x,y,new Color(.8f,.55f,.32f,1));
            baseMap.Apply(true);
            var rigP = RigTexture(true, .63f);
            var rigPAlpha = RigTexture(true, .23f);
            var rigN = RigTexture(false, .42f);
            var ramp = new Texture2D(4, 4, TextureFormat.RGBAHalf, true, true);
            for (int y=0;y<4;y++) for (int x=0;x<4;x++) ramp.SetPixel(x,y,new Color(.2f+.18f*x,.9f-.14f*x,.35f+.1f*x,.8f));
            ramp.Apply(true);
            var rt = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            var read = new Texture2D(Size, Size, TextureFormat.RGBAHalf, false, true);
            RenderTexture oldRT = RenderTexture.active;
            try
            {
                foreach (var go in new[] { quad,cameraGo,lightGo }) SceneManager.MoveGameObjectToScene(go,scene);
                quad.layer = Layer; quad.transform.localScale = new Vector3(1.85f,1.33f,1);
                quad.transform.rotation = Quaternion.Euler(10,18,14);
                var mesh = quad.GetComponent<MeshRenderer>();
                mesh.shadowCastingMode = ShadowCastingMode.Off; mesh.receiveShadows = false;
                var camera = cameraGo.AddComponent<Camera>(); camera.scene=scene;
                camera.orthographic=ortho; camera.orthographicSize=1.5f; camera.fieldOfView=43;
                camera.nearClipPlane=.1f; camera.farClipPlane=20; camera.allowHDR=true; camera.allowMSAA=false;
                camera.transform.position=new Vector3(0,0,5); camera.transform.rotation=Quaternion.LookRotation(Vector3.back);
                camera.cullingMask=1<<Layer; camera.clearFlags=CameraClearFlags.SolidColor;
                camera.backgroundColor=new Color(.0625f,.125f,.1875f,1); camera.targetTexture=rt;
                cameraGo.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=false;
                var light=lightGo.GetComponent<Light>(); light.type=LightType.Directional; light.color=Color.white;
                light.intensity=1.4f; light.shadows=LightShadows.None;
                var sh=Probe(new Color(.18f,.07f,.02f));
                RenderSettings.ambientMode=AmbientMode.Custom; RenderSettings.ambientProbe=sh;
                rt.Create(); Assert.That(rt.IsCreated() && !rt.sRGB, Is.True);
                // A Graph can expose a placeholder Shader before its first GPU
                // compile after assembly reload. Render once after synchronous
                // import, then query properties; do not treat prewarm -1/missing
                // metadata as product absence.
                mesh.sharedMaterial=c;
                for(int i=0;i<3;i++)camera.Render();
                Assert.That(fc.isSupported,Is.True,"Graph unsupported after warm render");
                foreach (string p in new[] { "_FxLightMode", "_RigRTBk", "_RigLBtF", "_SixWayInfo",
                    "_SixWayEmissionRamp", "_SixWayEmissionColor", "_SixWayColorAbsorptionToggle" })
                    Assert.That(c.HasProperty(p), Is.True, "SixWay Graph property missing after warm render: " + p);
                foreach (var m in new[] {a,b,c}) Configure(m,m==c,baseMap,rigP,rigN,ramp);
                // Collect every strict parity failure without skipping later captures.
                // The final NUnit assertion retains the zero tolerance.
                var failures = new List<string>();
                Color[][] directionalB = new Color[6][];
                Vector3[] axes={Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back};
                for (int i=0;i<axes.Length;i++)
                {
                    // Directional light points opposite its transform.forward.
                    light.transform.rotation=Quaternion.LookRotation(-axes[i]);
                    var t=Triplet(camera,mesh,rt,read,a,b,c,output,failures,"direction-"+i);
                    directionalB[i]=t[1];
                }
                // Six distinct rig channels must be causally observable, not
                // just three paired directions that coincidentally match.
                for (int i=0;i<3;i++)
                {
                    int n;float d;Compare(directionalB[i*2],directionalB[i*2+1],out n,out d,.015f);
                    Assert.That(n,Is.GreaterThan(40),"opposite SixWay direction had no visible effect");
                }
                light.transform.rotation=Quaternion.LookRotation(-new Vector3(.6f,.3f,.75f).normalized);
                var baseline=Triplet(camera,mesh,rt,read,a,b,c,output,failures,"baseline");
                RenderSettings.ambientProbe=Probe(new Color(.02f,.11f,.3f));
                Control(camera,mesh,rt,read,a,b,c,output,failures,"SH-blue",baseline,64,.005f);
                RenderSettings.ambientProbe=sh;
                foreach (var m in new[] {a,b,c}) m.SetTexture("_RigRTBk",rigPAlpha);
                Control(camera,mesh,rt,read,a,b,c,output,failures,"positive-alpha",baseline,64,.02f);
                foreach (var m in new[] {a,b,c}) m.SetTexture("_RigRTBk",rigP);
                foreach (var m in new[] {a,b,c}) {m.SetFloat("_SixWayColorAbsorptionToggle",1);m.EnableKeyword("VFX_SIX_WAY_ABSORPTION");}
                Control(camera,mesh,rt,read,a,b,c,output,failures,"absorption-on",baseline,64,.005f);
                foreach (var m in new[] {a,b,c}) {m.SetFloat("_SixWayColorAbsorptionToggle",0);m.DisableKeyword("VFX_SIX_WAY_ABSORPTION");}
                foreach (var m in new[] {a,b,c}) SetRampBit(m,m==c,true);
                Control(camera,mesh,rt,read,a,b,c,output,failures,"emission-ramp",baseline,64,.005f);
                foreach (var m in new[] {a,b,c}) SetRampBit(m,m==c,false);
                foreach (var m in new[] {a,b,c}) {m.SetTextureScale("_BaseMap",new Vector2(1.7f,1.35f));m.SetTextureOffset("_BaseMap",new Vector2(.42f,-.23f));}
                var st = Control(camera,mesh,rt,read,a,b,c,output,failures,"MainTex-ST",baseline,64,.005f);
                foreach (var m in new[] {a,b,c}) SetWrap(m,m==c,1);
                var wrap = Control(camera,mesh,rt,read,a,b,c,output,failures,"clamp-wrap",st,32,.005f);
                foreach (var m in new[] {a,b,c }) SetNoMip(m,m==c,28);
                Control(camera,mesh,rt,read,a,b,c,output,failures,"rig-LOD0",wrap,16,.003f);
                quad.transform.localScale=new Vector3(-1.85f,1.33f,1);
                Triplet(camera,mesh,rt,read,a,b,c,output,failures,"negative-scale");
                Assert.That(failures, Is.Empty, string.Join("\n", failures));
            }
            finally
            {
                RenderSettings.ambientMode=oldAmbientMode;RenderSettings.ambientProbe=oldProbe;
                RenderTexture.active=oldRT;var cam=cameraGo.GetComponent<Camera>();if(cam)cam.targetTexture=null;
                foreach (var o in new UnityEngine.Object[] {a,b,c,baseMap,rigP,rigPAlpha,rigN,ramp,rt,read}) if(o) UnityEngine.Object.DestroyImmediate(o);
                foreach (var go in new[] {quad,cameraGo,lightGo}) if(go) UnityEngine.Object.DestroyImmediate(go);
            }
        }
        static Texture2D RigTexture(bool positive,float alpha)
        {
            var t=new Texture2D(128,128,TextureFormat.RGBAHalf,true,true);
            for(int y=0;y<128;y++)for(int x=0;x<128;x++)
            {
                float u=x/127f,v=y/127f,checker=((x^y)&1)==0 ? .07f : 0;
                t.SetPixel(x,y,positive?
                    new Color(.12f+.14f*u+checker,.44f+.08f*v,.76f-.11f*u,alpha):
                    new Color(.72f-.1f*v,.21f+.16f*u+checker,.52f+.09f*v,alpha));
            }
            t.Apply(true);return t;
        }
        static SphericalHarmonicsL2 Probe(Color color)
        {var sh=new SphericalHarmonicsL2();sh.AddDirectionalLight(new Vector3(.4f,.5f,1).normalized,color,1);return sh;}
        static void Configure(Material m,bool graph,Texture2D baseMap,Texture2D positive,Texture2D negative,Texture2D ramp)
        {
            m.shaderKeywords=graph?new[]{"_SURFACE_TYPE_TRANSPARENT","EVALUATE_SH_VERTEX"}:
                new[]{"_FX_LIGHT_MODE_SIX_WAY","EVALUATE_SH_VERTEX"};
            m.SetTexture("_BaseMap",baseMap);m.SetTexture("_RigRTBk",positive);m.SetTexture("_RigLBtF",negative);
            m.SetTexture("_SixWayEmissionRamp",ramp);m.SetTextureScale("_BaseMap",Vector2.one);m.SetTextureOffset("_BaseMap",Vector2.zero);
            m.SetColor(graph?"_Color":"_BaseColor",new Color(.7f,.43f,.24f,.8f));m.SetColor("_ColorA",Color.white);
            m.SetColor("_SixWayEmissionColor",new Color(.8f,.42f,.17f,.75f));m.SetVector("_SixWayInfo",new Vector4(.55f,1.4f,0,0));
            m.SetFloat("_SixWayColorAbsorptionToggle",0);m.SetFloat("_FxLightMode",4);
            m.SetFloat("_BaseColorIntensityForTimeline",1);m.SetFloat("_AlphaAll",1);
            m.SetFloat("_Cull",(float)CullMode.Off);m.SetFloat("_ZTest",(float)CompareFunction.LessEqual);
            m.SetFloat("_ZWrite",0);m.SetFloat("_SrcBlend",(float)BlendMode.One);m.SetFloat("_DstBlend",(float)BlendMode.Zero);
            if(graph)
            {
                m.SetFloat("_Surface",1);m.SetFloat("_SrcBlendAlpha",(float)BlendMode.One);m.SetFloat("_DstBlendAlpha",(float)BlendMode.Zero);
                m.SetFloat("_NB_ColorChannelLo16",3);m.SetFloat("_NB_Flags1Lo16",1+(1<<9));
                m.SetFloat("_NB_Flags1Hi16",0);m.SetFloat("_NB_WrapFlagsLo16",0);m.SetFloat("_NB_WrapFlagsHi16",0);
                m.SetFloat("_NB_ForceNoMipFlagsLo16",0);m.SetFloat("_NB_ForceNoMipFlagsHi16",0);
                m.SetFloat("_NB_DistortionMode",0);
            }
            else
            {
                m.SetFloat("_ColorMask",15);m.SetFloat("_fogintensity",0);
                m.SetInteger("_W9ParticleShaderFlags",0);m.SetInteger("_W9ParticleShaderFlags1",1+(1<<9));
                m.SetInteger("_W9ParticleShaderColorChannelFlag",3);m.SetInteger("_W9ParticleShaderWrapFlags",0);
                m.SetInteger("_NBShaderForceNoMipFlags",0);
                foreach(var p in new[]{"_W9ParticleCustomDataFlag0","_W9ParticleCustomDataFlag1","_W9ParticleCustomDataFlag2","_W9ParticleCustomDataFlag3"})m.SetInteger(p,0);
                m.SetShaderPassEnabled("SRPDefaultUnlit",false);m.SetShaderPassEnabled("SRPDEFAULTUNLIT",false);
                m.SetShaderPassEnabled("UniversalForward",true);
            }
            foreach(string p in new[]{"DepthOnly","ShadowCaster","NBCameraOpaqueDistortPass","NBDeferredDistortPass","Universal2D"})m.SetShaderPassEnabled(p,false);
            m.renderQueue=3000;
        }
        static void SetRampBit(Material m,bool graph,bool on)
        {
            int word=1+(1<<9)+(on?(1<<29):0);
            if(graph){m.SetFloat("_NB_Flags1Lo16",word&65535);m.SetFloat("_NB_Flags1Hi16",(uint)word>>16);}
            else m.SetInteger("_W9ParticleShaderFlags1",word);
        }
        static void SetWrap(Material m,bool graph,int wrap)
        {if(graph)m.SetFloat("_NB_WrapFlagsLo16",wrap);else m.SetInteger("_W9ParticleShaderWrapFlags",wrap);}
        static void SetNoMip(Material m,bool graph,int flags)
        {if(graph)m.SetFloat("_NB_ForceNoMipFlagsLo16",flags);else m.SetInteger("_NBShaderForceNoMipFlags",flags);}
        static Color[][] Triplet(Camera camera,MeshRenderer mesh,RenderTexture rt,Texture2D read,
            Material a,Material b,Material c,string dir,List<string> failures,string name)
        {
            var materials=new[]{a,b,c};var pixels=new Color[3][];var repeats=new Color[3][];
            for(int i=0;i<3;i++)
            {mesh.sharedMaterial=materials[i];pixels[i]=Capture(camera,rt,read,dir,name+"-"+"ABC"[i]);
             repeats[i]=Capture(camera,rt,read,dir,name+"-"+"ABC"[i]+"-repeat");}
            var m=new Record{caseId=name,api=SystemInfo.graphicsDeviceType.ToString(),unityVersion=Application.unityVersion,
                limitation="Mesh SixWay; no flipbook blend UV, VertexLights, Forward+, VFX or Player claim. Ramp LOD0 separately untested."};
            Compare(pixels[0],pixels[1],out m.abDiff,out m.abMax);
            Compare(pixels[1],pixels[2],out m.bcDiff,out m.bcMax);
            Compare(pixels[0],repeats[0],out m.aRepeatDiff,out m.aRepeatMax);
            Compare(pixels[1],repeats[1],out m.bRepeatDiff,out m.bRepeatMax);
            Compare(pixels[2],repeats[2],out m.cRepeatDiff,out m.cRepeatMax);
            m.finite=Finite(pixels[0])&&Finite(pixels[1])&&Finite(pixels[2])&&Finite(repeats[0])&&Finite(repeats[1])&&Finite(repeats[2]);
            File.WriteAllText(Path.Combine(dir,name+"-metrics.json"),JsonUtility.ToJson(m,true));
            Assert.That(m.finite,Is.True,"SixWay nonfinite");
            if (m.abDiff != 0) failures.Add(name+": Frozen/current "+m.abDiff+" pixels, max "+m.abMax);
            if (m.bcDiff != 0) failures.Add(name+": ShaderLab/Graph "+m.bcDiff+" pixels, max "+m.bcMax);
            Assert.That(m.aRepeatDiff+m.bRepeatDiff+m.cRepeatDiff,Is.Zero,"SixWay repeat differs");
            return pixels;
        }
        static Color[][] Control(Camera camera,MeshRenderer mesh,RenderTexture rt,Texture2D read,
            Material a,Material b,Material c,string dir,List<string> failures,string name,Color[][] baseline,int minPixels,float threshold)
        {
            var p=Triplet(camera,mesh,rt,read,a,b,c,dir,failures,name);int nb,nc;float db,dc;
            Compare(baseline[1],p[1],out nb,out db,threshold);Compare(baseline[2],p[2],out nc,out dc,threshold);
            var m=new Record{caseId=name+"-causality",bControlDiff=nb,cControlDiff=nc,bControlMax=db,cControlMax=dc,finite=Finite(p[1])&&Finite(p[2])};
            File.WriteAllText(Path.Combine(dir,name+"-causality.json"),JsonUtility.ToJson(m,true));
            Assert.That(nb,Is.GreaterThanOrEqualTo(minPixels),name+" old Shader effect not observable");
            Assert.That(nc,Is.GreaterThanOrEqualTo(minPixels),name+" Graph effect not observable");
            return p;
        }
        static Color[] Capture(Camera camera,RenderTexture rt,Texture2D read,string dir,string name)
        {
            for(int i=0;i<3;i++)camera.Render();var old=RenderTexture.active;
            try{RenderTexture.active=rt;read.ReadPixels(new Rect(0,0,Size,Size),0,0);read.Apply(false);var px=read.GetPixels();
                using(var f=File.Create(Path.Combine(dir,name+".rgba-f32.gz")))
                using(var z=new GZipStream(f,CompressionMode.Compress))
                using(var w=new BinaryWriter(z))foreach(var c in px){w.Write(c.r);w.Write(c.g);w.Write(c.b);w.Write(c.a);}return px;}
            finally{RenderTexture.active=old;}
        }
        static void Compare(Color[] a,Color[] b,out int n,out float maximum,float threshold=0)
        {n=0;maximum=0;for(int y=Min;y<Max;y++)for(int x=Min;x<Max;x++)
            {int i=y*Size+x;float d=0;for(int k=0;k<4;k++)d=Mathf.Max(d,Mathf.Abs(a[i][k]-b[i][k]));maximum=Mathf.Max(maximum,d);if(d>threshold)n++;}}
        static bool Finite(Color[] p)
        {foreach(var c in p)for(int i=0;i<4;i++)if(float.IsNaN(c[i])||float.IsInfinity(c[i]))return false;return true;}
    }
}
