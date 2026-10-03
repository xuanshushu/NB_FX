using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace NBFX.Baseline.Tests
{
    public sealed class G4GraphTyflowVATTests
    {
        const string Package="Packages/com.xuanxuan.nb.fx/";
        const BindingFlags Private=BindingFlags.Static|BindingFlags.NonPublic;
        readonly List<Object> owned=new List<Object>();Shader aShader,bShader,cShader;
        [OneTimeSetUp] public void Warm()
        {
            new G4GraphGuiFeatureIntentTests().WarmImportedGraphInRealUrpCamera();
            aShader=AssetDatabase.LoadAssetAtPath<Shader>(Package+"Tests/Baseline/Frozen/NBShaders2/Shader/NBShader.shader");
            bShader=AssetDatabase.LoadAssetAtPath<Shader>(Package+"NBShaders2/Shader/NBShader.shader");
            cShader=AssetDatabase.LoadAssetAtPath<Shader>(Package+"NBShaders2/ShaderGraph/NBShaderGraph.shadergraph");
            Assert.That(aShader&&bShader&&cShader&&cShader.isSupported,Is.True);
        }
        T Keep<T>(T t) where T:Object {owned.Add(t);return t;}
        [TearDown] public void Cleanup(){for(int i=owned.Count-1;i>=0;i--)if(owned[i])Object.DestroyImmediate(owned[i]);owned.Clear();}
        public static IEnumerable<TestCaseData> Cases()
        {
            for(int mode=0;mode<6;mode++)foreach(string variant in mode<2?new[]{"raw","half","float","custom-frame","normals","half-gamma","float-gamma"}:new[]{"rigid","deform7","interpolated","deform7-interpolated","gamma"})
                foreach(bool ortho in new[]{true,false})yield return new TestCaseData(mode,variant,ortho).SetName("G4TyflowABC_m"+mode+"_"+variant+(ortho?"_ortho":"_perspective"));
        }
        [Serializable] sealed class Metrics
        {
            public int mode,aVisible,bVisible,cVisible;public string variant,api,unityVersion,scope;public bool finite,orthographic;
            public float abOff,bcOff,abOn,bcOn,abControl,bcControl,aRepeat,bRepeat,cRepeat,aResponse,bResponse,cResponse,aControlResponse,bControlResponse,cControlResponse;
        }
        static Color Bytes(byte[] b)=>new Color(b[0]/255f,b[1]/255f,b[2]/255f,b[3]/255f);
        static Color EncodedFloat(float f)=>Bytes(BitConverter.GetBytes(f));
        static Color EncodedInt(int i)=>Bytes(BitConverter.GetBytes(i));
        static ushort Half(float value)
        {
            uint x=BitConverter.ToUInt32(BitConverter.GetBytes(value),0);uint sign=(x>>16)&0x8000;int exponent=(int)((x>>23)&255)-127+15;
            Assert.That(exponent,Is.InRange(1,30));return (ushort)(sign|((uint)exponent<<10)|((x>>13)&1023));
        }
        static Color EncodedHalfs(float a,float b)
        {
            ushort ha=a==0?((ushort)0):Half(a),hb=b==0?((ushort)0):Half(b);return Bytes(new[]{(byte)(ha&255),(byte)(ha>>8),(byte)(hb&255),(byte)(hb>>8)});
        }
        Texture2D Texture(int width,int height,Func<int,int,Color> pixel,TextureFormat format,bool linear=true)
        {
            var t=Keep(new Texture2D(width,height,format,false,linear));for(int y=0;y<height;y++)for(int x=0;x<width;x++)t.SetPixel(x,y,pixel(x,y));
            // Deliberately non-point/non-clamp: Tyflow must use its own sampler.
            t.filterMode=FilterMode.Bilinear;t.wrapMode=TextureWrapMode.Repeat;t.Apply(false);return t;
        }
        Texture2D BuildVAT(int mode,string variant,Mesh mesh)
        {
            if(mode<2)
            {
                var positions=mesh.vertices;var floats=new List<float>();var raw=new List<Color>();bool normals=variant=="normals";
                for(int block=0;block<(normals?2:1);block++)for(int frame=0;frame<2;frame++)for(int v=0;v<4;v++)
                {
                    Vector3 value;
                    if(block==1)value=new Vector3(.25f,frame==0?.25f:.75f,1).normalized;
                    else if(mode==0)value=new Vector3(-positions[v].x+(frame==0?.125f:.375f),positions[v].y+(frame==0?-.125f:.125f),positions[v].z+.125f);
                    else value=new Vector3(frame==0?.125f:.375f,frame==0?-.125f:.125f,.125f);
                    floats.Add(value.x);floats.Add(value.y);floats.Add(value.z);raw.Add(new Color(value.x,value.y,value.z,1));
                }
                if(variant.StartsWith("float",StringComparison.Ordinal))return Texture(8,8,(x,y)=>{int i=(7-y)*8+x;return i<floats.Count?EncodedFloat(floats[i]):Color.clear;},TextureFormat.RGBA32,!variant.EndsWith("gamma",StringComparison.Ordinal));
                if(variant.StartsWith("half",StringComparison.Ordinal))return Texture(8,8,(x,y)=>{int i=((7-y)*8+x)*2;return i<floats.Count?EncodedHalfs(floats[i],i+1<floats.Count?floats[i+1]:0):Color.clear;},TextureFormat.RGBA32,!variant.EndsWith("gamma",StringComparison.Ordinal));
                return Texture(8,8,(x,y)=>{int i=(7-y)*8+x;return i<raw.Count?raw[i]:Color.clear;},TextureFormat.RGBAFloat);
            }
            bool deform=variant.StartsWith("deform7",StringComparison.Ordinal);int bones=deform?7:1;int metadata=deform||mode==5?12:3;
            int pixels=mode==2?2:mode==3?5:mode==4?6:7;var table=Enumerable.Repeat(Color.clear,256).ToArray();
            table[0]=EncodedInt(2);table[1]=EncodedInt(bones);
            for(int b=0;b<bones;b++)
            {
                int start=2+metadata*b;
                if(metadata==12)for(int row=0;row<4;row++)for(int col=0;col<3;col++)table[start+row*3+col]=EncodedFloat(row==col?1:0);
                else for(int c=0;c<3;c++)table[start+c]=EncodedFloat(0);
            }
            for(int frame=0;frame<2;frame++)for(int b=0;b<bones;b++)
            {
                int i=2+bones*metadata+frame*bones*pixels+b*pixels;
                if(mode!=2)
                {table[i++]=EncodedFloat((frame==0?.125f:.375f)+b*.0625f);table[i++]=EncodedFloat(frame==0?-.125f:.125f);table[i++]=EncodedFloat(.125f);}
                float s=frame==0?.25f:.5f;float c=frame==0?.96875f:.875f;
                table[i++]=EncodedHalfs(0,0);table[i++]=EncodedHalfs(-s,c);
                if(mode==4)table[i]=EncodedHalfs(frame==0?.75f:1.25f,0);
                if(mode==5){table[i++]=EncodedHalfs(frame==0?.75f:1.25f,frame==0?1.25f:.75f);table[i]=EncodedHalfs(1,0);}
            }
            return Texture(16,16,(x,y)=>table[(15-y)*16+x],TextureFormat.RGBA32,!variant.EndsWith("gamma",StringComparison.Ordinal));
        }
        [TestCaseSource(nameof(Cases))]
        public void AllSixTyflowModes_ActualMeshABC(int mode,string variant,bool ortho)
        {
            Assert.That(GraphicsSettings.currentRenderPipeline,Is.InstanceOf<UniversalRenderPipelineAsset>());
            string folder=Path.Combine(Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR")??Path.Combine(Path.GetDirectoryName(Application.dataPath),"Temp/NBFXTyflow"),"m"+mode+"-"+variant+(ortho?"-ortho":"-perspective"));Directory.CreateDirectory(folder);
            var actor=Keep(GameObject.CreatePrimitive(PrimitiveType.Quad));actor.layer=4;var mesh=Keep(Object.Instantiate(actor.GetComponent<MeshFilter>().sharedMesh));actor.GetComponent<MeshFilter>().sharedMesh=mesh;
            bool deform=variant.StartsWith("deform7",StringComparison.Ordinal);
            for(int channel=1;channel<8;channel++)mesh.SetUVs(channel,mode<2?(channel==1?Enumerable.Range(0,4).Select(v=>new Vector4(v,4,0,0)).ToList():Enumerable.Repeat(Vector4.zero,4).ToList()):Enumerable.Repeat(new Vector4(deform?channel-1:0,deform?1f/7f:0,0,0),4).ToList());
            var cameraObject=Keep(new GameObject("Tyflow actual Mesh camera"));var camera=cameraObject.AddComponent<Camera>();var scene=SceneManager.GetActiveScene();Assert.That(scene.IsValid()&&scene.isLoaded,Is.True);
            SceneManager.MoveGameObjectToScene(actor,scene);SceneManager.MoveGameObjectToScene(cameraObject,scene);camera.scene=scene;
            actor.transform.rotation=Quaternion.Euler(4,17,3);actor.transform.localScale=new Vector3(1.65f,1.45f,1.2f);
            var renderer=actor.GetComponent<MeshRenderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            camera.orthographic=ortho;camera.orthographicSize=1.4f;camera.fieldOfView=42;camera.nearClipPlane=.1f;camera.farClipPlane=20;camera.transform.position=new Vector3(.08f,.04f,4);camera.transform.rotation=Quaternion.LookRotation(Vector3.back);
            camera.cullingMask=1<<4;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.0625f,.125f,.1875f,1);camera.allowHDR=true;camera.allowMSAA=false;cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=false;
            var map=Texture(8,8,(x,y)=>((x/2+y/2)&1)==0?new Color(.85f,.32f,.12f,1):new Color(.18f,.72f,.83f,1),TextureFormat.RGBAHalf);
            var oldAmbientMode=RenderSettings.ambientMode;var oldProbe=RenderSettings.ambientProbe;
            if(variant=="normals")
            {
                var lightObject=Keep(new GameObject("Tyflow normal response light",typeof(Light)));SceneManager.MoveGameObjectToScene(lightObject,scene);
                var light=lightObject.GetComponent<Light>();light.type=LightType.Directional;light.intensity=1.35f;light.shadows=LightShadows.None;light.transform.rotation=Quaternion.Euler(42,31,0);
                var probe=new SphericalHarmonicsL2();probe.AddDirectionalLight(new Vector3(.4f,.5f,1).normalized,new Color(.35f,.24f,.14f),1);RenderSettings.ambientMode=AmbientMode.Custom;RenderSettings.ambientProbe=probe;
            }
            var vat=BuildVAT(mode,variant,mesh);var materials=new[]{Keep(new Material(aShader)),Keep(new Material(bShader)),Keep(new Material(cShader))};
            var configure=typeof(G4GraphVATTests).GetMethod("Configure",Private);string[] keywords={"_TYFLOW_VAT_ABSOLUTE","_TYFLOW_VAT_RELATIVE","_TYFLOW_VAT_SKIN_R","_TYFLOW_VAT_SKIN_PR","_TYFLOW_VAT_SKIN_PRSAVE","_TYFLOW_VAT_SKIN_PRSXYZ"};
            void Setup(Material m)
            {
                configure.Invoke(null,new object[]{m,m==materials[2],variant=="normals"?"normal-sixway":"manual2",map,map,map,map,map,map,map});
                m.SetFloat("_VATMode",1);m.SetFloat("_TyFlowVATSubMode",mode);m.SetTexture("_VATTex",vat);m.SetFloat("_ImportScale",1);m.SetFloat("_Frames",2);
                m.SetFloat("_Frame",variant.Contains("interpolated")?.5f:1);m.SetFloat("_Autoplay",0);m.SetFloat("_AutoplaySpeed",1);m.SetFloat("_Loop",0);m.SetFloat("_InterpolateLoop",0);m.SetFloat("_FrameInterpolation",variant.Contains("interpolated")?1:0);
                m.SetFloat("_LinearToGamma",variant.EndsWith("gamma",StringComparison.Ordinal)?1:0);m.SetFloat("_RGBAEncoded",variant.StartsWith("float",StringComparison.Ordinal)||variant.StartsWith("half",StringComparison.Ordinal)?1:0);m.SetFloat("_RGBAHalf",variant.StartsWith("half",StringComparison.Ordinal)?1:0);
                m.SetFloat("_DeformingSkin",deform?1:0);m.SetFloat("_SkinBoneCount",deform?7:1);m.SetFloat("_VATIncludesNormals",variant=="normals"?1:0);m.SetFloat("_AffectsShadows",1);
                if(variant=="custom-frame")
                {
                    if(m==materials[2]){m.SetFloat("_NB_CustomDataFlag2Lo16",0);m.SetFloat("_NB_CustomDataFlag2Hi16",11u<<12);}
                    else m.SetInteger("_W9ParticleCustomDataFlag2",unchecked((int)(11u<<28)));
                }
            }
            void On(Material m,bool enabled)
            {
                m.SetFloat("_VAT_Toggle",enabled?1:0);if(m==materials[2])return;
                m.DisableKeyword("_VAT_HOUDINI");m.DisableKeyword("_HOUDINI_VAT_SOFTBODY");foreach(string k in keywords)m.DisableKeyword(k);
                if(enabled){m.EnableKeyword("_VAT");m.EnableKeyword("_VAT_TYFLOW");m.EnableKeyword(keywords[mode]);}else{m.DisableKeyword("_VAT");m.DisableKeyword("_VAT_TYFLOW");}
            }
            if(variant=="custom-frame")mesh.SetUVs(2,Enumerable.Repeat(new Vector4(1,0,0,0),4).ToList());
            var rt=Keep(new RenderTexture(128,128,24,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear));var read=Keep(new Texture2D(128,128,TextureFormat.RGBAHalf,false,true));var oldRT=RenderTexture.active;camera.targetTexture=rt;rt.Create();var draw=typeof(G4GraphVATTests).GetMethod("Draw",Private);
            Color[] Snap(Material m,string n)=>(Color[])draw.Invoke(null,new object[]{renderer,m,camera,rt,read,folder,n});
            try
            {
                foreach(var m in materials){Setup(m);On(m,true);}renderer.sharedMaterial=materials[2];for(int i=0;i<4;i++)camera.Render();foreach(var m in materials)Setup(m);
                renderer.enabled=false;var clear=Snap(materials[2],"clear");renderer.enabled=true;
                Color[][] off=new Color[3][],on=new Color[3][],repeat=new Color[3][],control=new Color[3][];
                for(int i=0;i<3;i++){On(materials[i],false);off[i]=Snap(materials[i],"ABC"[i]+"-off");On(materials[i],true);on[i]=Snap(materials[i],"ABC"[i]+"-on");repeat[i]=Snap(materials[i],"ABC"[i]+"-repeat");}
                if(variant=="custom-frame")mesh.SetUVs(2,Enumerable.Repeat(Vector4.zero,4).ToList());
                else if(deform)mesh.SetUVs(7,Enumerable.Repeat(new Vector4(6,0,0,0),4).ToList());
                else if(variant=="normals")foreach(var m in materials)m.SetFloat("_VATIncludesNormals",0);
                else foreach(var m in materials)m.SetFloat("_Frame",0);
                for(int i=0;i<3;i++)control[i]=Snap(materials[i],"ABC"[i]+"-control");
                var mtr=new Metrics{mode=mode,variant=variant,orthographic=ortho,api=SystemInfo.graphicsDeviceType.ToString(),unityVersion=Application.unityVersion,scope="Tyflow6 modes actual Forward Mesh ABC; raw/half/float and real7 UV skin channels; no formal VFX/Player/depth-shadow claim",
                    finite=clear.Concat(off.SelectMany(x=>x)).Concat(on.SelectMany(x=>x)).Concat(repeat.SelectMany(x=>x)).Concat(control.SelectMany(x=>x)).All(p=>new[]{p.r,p.g,p.b,p.a}.All(v=>!float.IsNaN(v)&&!float.IsInfinity(v))),
                    abOff=Delta(off[0],off[1]),bcOff=Delta(off[1],off[2]),abOn=Delta(on[0],on[1]),bcOn=Delta(on[1],on[2]),abControl=Delta(control[0],control[1]),bcControl=Delta(control[1],control[2]),aRepeat=Delta(on[0],repeat[0]),bRepeat=Delta(on[1],repeat[1]),cRepeat=Delta(on[2],repeat[2]),
                    aResponse=Delta(off[0],on[0]),bResponse=Delta(off[1],on[1]),cResponse=Delta(off[2],on[2]),aControlResponse=Delta(on[0],control[0]),bControlResponse=Delta(on[1],control[1]),cControlResponse=Delta(on[2],control[2]),aVisible=Visible(on[0],clear),bVisible=Visible(on[1],clear),cVisible=Visible(on[2],clear)};
                File.WriteAllText(Path.Combine(folder,"metrics.json"),JsonUtility.ToJson(mtr,true));Debug.Log("NBFX_G4_TYFLOW "+JsonUtility.ToJson(mtr));
                Assert.That(mtr.finite,Is.True);Assert.That(mtr.aVisible,Is.GreaterThan(150));Assert.That(mtr.bVisible,Is.GreaterThan(150));Assert.That(mtr.cVisible,Is.GreaterThan(150));Assert.That(mtr.abOff,Is.Zero);Assert.That(mtr.bcOff,Is.Zero);Assert.That(mtr.abOn,Is.Zero);Assert.That(mtr.bcOn,Is.Zero);Assert.That(mtr.abControl,Is.Zero);Assert.That(mtr.bcControl,Is.Zero);Assert.That(mtr.aRepeat+mtr.bRepeat+mtr.cRepeat,Is.Zero);
                Assert.That(mtr.aResponse,Is.GreaterThan(.01f));Assert.That(mtr.bResponse,Is.GreaterThan(.01f));Assert.That(mtr.cResponse,Is.GreaterThan(.01f));Assert.That(mtr.aControlResponse,Is.GreaterThan(.005f));Assert.That(mtr.bControlResponse,Is.GreaterThan(.005f));Assert.That(mtr.cControlResponse,Is.GreaterThan(.005f));
            }
            finally{camera.targetTexture=null;RenderTexture.active=oldRT;rt.Release();RenderSettings.ambientMode=oldAmbientMode;RenderSettings.ambientProbe=oldProbe;}
        }
        static float Delta(Color[] a,Color[] b){float m=0;for(int i=0;i<a.Length;i++){var d=a[i]-b[i];m=Mathf.Max(m,Mathf.Abs(d.r),Mathf.Abs(d.g),Mathf.Abs(d.b),Mathf.Abs(d.a));}return m;}
        static int Visible(Color[] a,Color[] b){int n=0;for(int i=0;i<a.Length;i++)if(Mathf.Max(Mathf.Abs(a[i].r-b[i].r),Mathf.Abs(a[i].g-b[i].g),Mathf.Abs(a[i].b-b[i].b))>.001f)n++;return n;}
    }
}
