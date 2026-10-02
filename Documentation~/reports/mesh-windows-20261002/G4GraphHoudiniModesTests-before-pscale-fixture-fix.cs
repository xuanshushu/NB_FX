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
    public sealed class G4GraphHoudiniModesTests
    {
        const string Package="Packages/com.xuanxuan.nb.fx/";
        const BindingFlags Private=BindingFlags.Static|BindingFlags.NonPublic;
        readonly List<Object> owned=new List<Object>();
        Shader aShader,bShader,cShader;
        [OneTimeSetUp] public void Warm()
        {
            new G4GraphGuiFeatureIntentTests().WarmImportedGraphInRealUrpCamera();
            aShader=AssetDatabase.LoadAssetAtPath<Shader>(Package+"Tests/Baseline/Frozen/NBShaders2/Shader/NBShader.shader");
            bShader=AssetDatabase.LoadAssetAtPath<Shader>(Package+"NBShaders2/Shader/NBShader.shader");
            cShader=AssetDatabase.LoadAssetAtPath<Shader>(Package+"NBShaders2/ShaderGraph/NBShaderGraph.shadergraph");
            Assert.That(aShader&&bShader&&cShader&&cShader.isSupported,Is.True);
        }
        T Keep<T>(T value) where T:Object {owned.Add(value);return value;}
        [TearDown] public void Clean() {for(int i=owned.Count-1;i>=0;i--)if(owned[i])Object.DestroyImmediate(owned[i]);owned.Clear();}
        static readonly string[] Variants={"rigid-basic","rigid-interpolated","rigid-dual","rigid-pscale","rigid-uv4","rigid-rest",
            "remesh-lookup","remesh-dual","remesh-compressed","sprite-basic","sprite-interpolated","sprite-spin",
            "sprite-heading","sprite-origin","sprite-pscale","sprite-dual"};
        static IEnumerable<TestCaseData> Cases()
        {foreach(string v in Variants)foreach(bool o in new[]{true,false})yield return new TestCaseData(v,o).SetName("G4HoudiniModesABC_"+v+(o?"_ortho":"_perspective"));}
        [Serializable] sealed class Metrics
        {
            public string variant,unityVersion,api,scope;
            public bool orthographic,finite;
            public float abOn,bcOn,abOff,bcOff,abControl,bcControl,aRepeat,bRepeat,cRepeat;
            public float aResponse,bResponse,cResponse,aControlResponse,bControlResponse,cControlResponse;
            public int aVisible,bVisible,cVisible;
        }
        Texture2D Texture(int size,Func<int,int,Color> pixel)
        {
            var t=Keep(new Texture2D(size,size,TextureFormat.RGBAHalf,false,true));
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)t.SetPixel(x,y,pixel(x,y));
            t.wrapMode=TextureWrapMode.Clamp;t.filterMode=FilterMode.Point;t.Apply(false);return t;
        }
        [TestCaseSource(nameof(Cases))]
        public void ThreeRemainingHoudiniModes_ActualMeshABC(string variant,bool ortho)
        {
            Assert.That(GraphicsSettings.currentRenderPipeline,Is.InstanceOf<UniversalRenderPipelineAsset>());
            string folder=Path.Combine(Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR")??Path.Combine(Path.GetDirectoryName(Application.dataPath),"Temp/NBFXHoudiniModes"),variant+(ortho?"-ortho":"-perspective"));
            Directory.CreateDirectory(folder);
            var actor=Keep(GameObject.CreatePrimitive(PrimitiveType.Quad));actor.layer=4;
            var cameraObject=Keep(new GameObject("Houdini modes comparison camera"));var camera=cameraObject.AddComponent<Camera>();
            var scene=SceneManager.GetActiveScene();Assert.That(scene.IsValid()&&scene.isLoaded,Is.True);
            SceneManager.MoveGameObjectToScene(actor,scene);SceneManager.MoveGameObjectToScene(cameraObject,scene);camera.scene=scene;
            var mesh=Keep(Object.Instantiate(actor.GetComponent<MeshFilter>().sharedMesh));actor.GetComponent<MeshFilter>().sharedMesh=mesh;
            mesh.SetUVs(0,new List<Vector4>{new Vector4(.12f,.25f,0,0),new Vector4(.88f,.25f,0,0),new Vector4(.12f,.9f,0,0),new Vector4(.88f,.9f,0,0)});
            bool sprite=variant.StartsWith("sprite",StringComparison.Ordinal);bool rigid=variant.StartsWith("rigid",StringComparison.Ordinal);
            mesh.SetUVs(1,sprite?Enumerable.Repeat(new Vector4(.4f,.7f,0,0),4).ToList():new List<Vector4>{new Vector4(.2f,.66f,0,0),new Vector4(.8f,.66f,0,0),new Vector4(.2f,.86f,0,0),new Vector4(.8f,.86f,0,0)});
            mesh.SetUVs(2,Enumerable.Repeat(new Vector4(variant=="rigid-uv4"?.12f:0,0,0,0),4).ToList());
            mesh.SetUVs(4,Enumerable.Repeat(new Vector4(variant=="rigid-uv4"?.14f:0,variant=="rigid-uv4"?.78f:1,0,0),4).ToList());
            actor.transform.rotation=Quaternion.Euler(4,17,3);actor.transform.localScale=new Vector3(1.65f,1.45f,1.2f);
            var renderer=actor.GetComponent<MeshRenderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            camera.orthographic=ortho;camera.orthographicSize=1.4f;camera.fieldOfView=42;camera.nearClipPlane=.1f;camera.farClipPlane=20;
            camera.transform.position=new Vector3(.08f,.04f,4);camera.transform.rotation=Quaternion.LookRotation(Vector3.back);
            camera.cullingMask=1<<4;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.0625f,.125f,.1875f,1);
            camera.allowHDR=true;camera.allowMSAA=false;cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=false;
            var map=Texture(8,(x,y)=>((x/2+y/2)&1)==0?new Color(.85f,.32f,.12f,1):new Color(.18f,.72f,.83f,1));
            var pos=Texture(8,(x,y)=>new Color(.3f+.055f*x,.27f+.06f*y,.46f+.015f*((x+2*y)%5),.1f));
            var second=Texture(2,(x,y)=>new Color(1,.25f,.75f,1));
            var rot=Texture(2,(x,y)=>rigid?new Color(.58f,.55f,.48f,1):new Color(.5f,.5f,.5f,1));
            var heading=Texture(2,(x,y)=>new Color(.4f,.7f,.1f,1));
            var lookup=Texture(8,(x,y)=>new Color(.08f+.12f*x,0,1-(.08f+.12f*y),0));
            var materials=new[]{Keep(new Material(aShader)),Keep(new Material(bShader)),Keep(new Material(cShader))};
            var configure=typeof(G4GraphVATTests).GetMethod("Configure",Private);Assert.That(configure,Is.Not.Null);
            int mode=rigid?1:sprite?3:2;
            void Setup(Material m)
            {
                configure.Invoke(null,new object[]{m,m==materials[2],"manual2",map,pos,second,rot,map,map,map});
                m.SetFloat("_HoudiniVATSubMode",mode);m.SetTexture("_colTexture",heading);m.SetTexture("_lookupTable",lookup);
                m.SetFloat("_displayFrame",variant.EndsWith("interpolated",StringComparison.Ordinal)?1.5f:2);
                m.SetFloat("_B_interpolate",variant.EndsWith("interpolated",StringComparison.Ordinal)?1:0);m.SetFloat("_animateFirstFrame",1);
                m.SetFloat("_globalPscaleMul",1);m.SetFloat("_B_pscaleAreInPosA",variant.EndsWith("pscale",StringComparison.Ordinal)?1:0);
                m.SetFloat("_widthBaseScale",1.1f);m.SetFloat("_heightBaseScale",.95f);m.SetFloat("_B_hideOverlappingOrigin",variant=="sprite-origin"?1:0);
                m.SetFloat("_originRadius",.02f);m.SetFloat("_B_CAN_SPIN",variant=="sprite-spin"||variant=="sprite-heading"?1:0);
                m.SetFloat("_B_spinFromHeading",variant=="sprite-heading"?1:0);m.SetFloat("_B_LOAD_COL_TEX",variant=="sprite-heading"?1:0);
                m.SetFloat("_spinPhase",.17f);m.SetFloat("_scaleByVelAmount",1.3f);
                m.SetFloat("_B_LOAD_POS_TWO_TEX",variant.EndsWith("dual",StringComparison.Ordinal)?1:0);
                m.SetFloat("_B_UNLOAD_ROT_TEX",variant=="remesh-compressed"?1:0);
                if(variant=="rigid-rest"){m.SetFloat("_displayFrame",1);m.SetFloat("_animateFirstFrame",0);}
            }
            void On(Material m,bool enabled)
            {
                m.SetFloat("_VAT_Toggle",enabled?1:0);if(m==materials[2])return;
                m.DisableKeyword("_HOUDINI_VAT_SOFTBODY");
                string[] modes={"_HOUDINI_VAT_RIGIDBODY","_HOUDINI_VAT_DYNAMIC_REMESH","_HOUDINI_VAT_PARTICLE_SPRITE"};
                foreach(string k in modes)m.DisableKeyword(k);
                if(enabled){m.EnableKeyword("_VAT");m.EnableKeyword("_VAT_HOUDINI");m.EnableKeyword(modes[mode-1]);}
                else {m.DisableKeyword("_VAT");m.DisableKeyword("_VAT_HOUDINI");}
            }
            var rt=Keep(new RenderTexture(128,128,24,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear));
            var read=Keep(new Texture2D(128,128,TextureFormat.RGBAHalf,false,true));var oldRT=RenderTexture.active;camera.targetTexture=rt;rt.Create();
            var draw=typeof(G4GraphVATTests).GetMethod("Draw",Private);
            Color[] Snap(Material m,string name)=>(Color[])draw.Invoke(null,new object[]{renderer,m,camera,rt,read,folder,name});
            try
            {
                foreach(var m in materials){Setup(m);On(m,true);}renderer.sharedMaterial=materials[2];for(int i=0;i<4;i++)camera.Render();
                foreach(var m in materials)Setup(m);
                renderer.enabled=false;var clear=Snap(materials[2],"clear");renderer.enabled=true;
                Color[][] off=new Color[3][],on=new Color[3][],repeat=new Color[3][],control=new Color[3][];
                for(int i=0;i<3;i++){On(materials[i],false);off[i]=Snap(materials[i],"ABC"[i]+"-off");On(materials[i],true);on[i]=Snap(materials[i],"ABC"[i]+"-on");repeat[i]=Snap(materials[i],"ABC"[i]+"-repeat");}
                foreach(var m in materials)
                {
                    if(variant.EndsWith("dual",StringComparison.Ordinal))m.SetFloat("_B_LOAD_POS_TWO_TEX",0);
                    else if(variant.EndsWith("interpolated",StringComparison.Ordinal))m.SetFloat("_B_interpolate",0);
                    else if(variant.EndsWith("pscale",StringComparison.Ordinal))m.SetFloat("_B_pscaleAreInPosA",0);
                    else if(variant=="sprite-spin"||variant=="sprite-heading")m.SetFloat("_B_CAN_SPIN",0);
                    else if(variant=="sprite-origin")m.SetFloat("_originRadius",100);
                    else if(variant=="rigid-rest")m.SetFloat("_animateFirstFrame",1);
                    else if(variant=="remesh-compressed")m.SetFloat("_displayFrame",1);
                    else m.SetFloat("_displayFrame",1);
                }
                for(int i=0;i<3;i++)control[i]=Snap(materials[i],"ABC"[i]+"-control");
                var mtr=new Metrics{variant=variant,orthographic=ortho,api=SystemInfo.graphicsDeviceType.ToString(),unityVersion=Application.unityVersion,
                    scope="Three additional Houdini modes actual Mesh Forward ABC and controls; no Tyflow, full normals, depth/shadow, VFX or Player claim",
                    finite=clear.Concat(off.SelectMany(x=>x)).Concat(on.SelectMany(x=>x)).Concat(repeat.SelectMany(x=>x)).Concat(control.SelectMany(x=>x)).All(p=>new[]{p.r,p.g,p.b,p.a}.All(v=>!float.IsNaN(v)&&!float.IsInfinity(v))),
                    abOff=Delta(off[0],off[1]),bcOff=Delta(off[1],off[2]),abOn=Delta(on[0],on[1]),bcOn=Delta(on[1],on[2]),
                    abControl=Delta(control[0],control[1]),bcControl=Delta(control[1],control[2]),aRepeat=Delta(on[0],repeat[0]),bRepeat=Delta(on[1],repeat[1]),cRepeat=Delta(on[2],repeat[2]),
                    aResponse=Delta(off[0],on[0]),bResponse=Delta(off[1],on[1]),cResponse=Delta(off[2],on[2]),
                    aControlResponse=Delta(on[0],control[0]),bControlResponse=Delta(on[1],control[1]),cControlResponse=Delta(on[2],control[2]),
                    aVisible=Visible(on[0],clear),bVisible=Visible(on[1],clear),cVisible=Visible(on[2],clear)};
                File.WriteAllText(Path.Combine(folder,"metrics.json"),JsonUtility.ToJson(mtr,true));Debug.Log("NBFX_G4_HOUDINI_MODES "+JsonUtility.ToJson(mtr));
                Assert.That(mtr.finite,Is.True);Assert.That(mtr.aVisible,Is.GreaterThan(150));Assert.That(mtr.bVisible,Is.GreaterThan(150));Assert.That(mtr.cVisible,Is.GreaterThan(150));
                Assert.That(mtr.abOff,Is.Zero);Assert.That(mtr.bcOff,Is.Zero);Assert.That(mtr.abOn,Is.Zero);Assert.That(mtr.bcOn,Is.Zero);Assert.That(mtr.abControl,Is.Zero);Assert.That(mtr.bcControl,Is.Zero);
                Assert.That(mtr.aRepeat+mtr.bRepeat+mtr.cRepeat,Is.Zero);
                if(variant=="rigid-rest")Assert.That(mtr.aResponse+mtr.bResponse+mtr.cResponse,Is.Zero,"Rest frame is exact original geometry");
                else {Assert.That(mtr.aResponse,Is.GreaterThan(.01f));Assert.That(mtr.bResponse,Is.GreaterThan(.01f));Assert.That(mtr.cResponse,Is.GreaterThan(.01f));}
                Assert.That(mtr.aControlResponse,Is.GreaterThan(.005f));Assert.That(mtr.bControlResponse,Is.GreaterThan(.005f));Assert.That(mtr.cControlResponse,Is.GreaterThan(.005f));
            }
            finally {camera.targetTexture=null;RenderTexture.active=oldRT;rt.Release();}
        }
        static float Delta(Color[] a,Color[] b)
        {float m=0;for(int i=0;i<a.Length;i++){var d=a[i]-b[i];m=Mathf.Max(m,Mathf.Abs(d.r),Mathf.Abs(d.g),Mathf.Abs(d.b),Mathf.Abs(d.a));}return m;}
        static int Visible(Color[] a,Color[] b)
        {int n=0;for(int i=0;i<a.Length;i++)if(Mathf.Max(Mathf.Abs(a[i].r-b[i].r),Mathf.Abs(a[i].g-b[i].g),Mathf.Abs(a[i].b-b[i].b))>.001f)n++;return n;}
    }
}
