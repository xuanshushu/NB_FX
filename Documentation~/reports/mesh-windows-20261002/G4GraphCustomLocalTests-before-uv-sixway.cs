using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;
namespace NBFX.Baseline.Tests
{
    public sealed class G4GraphCustomLocalTests
    {
        const string Package="Packages/com.xuanxuan.nb.fx/";
        const BindingFlags Private=BindingFlags.Static|BindingFlags.NonPublic;
        Shader aShader,bShader,cShader;readonly List<Object> owned=new List<Object>();
        [OneTimeSetUp] public void Warm()
        {
            new G4GraphGuiFeatureIntentTests().WarmImportedGraphInRealUrpCamera();
            aShader=AssetDatabase.LoadAssetAtPath<Shader>(Package+"Tests/Baseline/Frozen/NBShaders2/Shader/NBShader.shader");bShader=AssetDatabase.LoadAssetAtPath<Shader>(Package+"NBShaders2/Shader/NBShader.shader");cShader=AssetDatabase.LoadAssetAtPath<Shader>(Package+"NBShaders2/ShaderGraph/NBShaderGraph.shadergraph");
        }
        T Keep<T>(T x) where T:Object {owned.Add(x);return x;}
        [TearDown] public void Cleanup(){for(int i=owned.Count-1;i>=0;i--)if(owned[i])Object.DestroyImmediate(owned[i]);owned.Clear();}
        static IEnumerable<TestCaseData> Cases()
        {foreach(string variant in new[]{"passthrough","vo-normal","vo-world","houdini-soft","tyflow-relative"})foreach(bool neg in new[]{false,true})foreach(bool o in new[]{true,false})yield return new TestCaseData(variant,neg,o).SetName("G4CustomLocalABC_"+variant+(neg?"_negative":"_positive")+(o?"_ortho":"_perspective"));}
        [Serializable] sealed class Metrics
        {
            public string variant,api,unityVersion,scope;public bool finite,orthographic,negative;
            public float abOn,bcOn,abOff,bcOff,aRepeat,bRepeat,cRepeat,aResponse,bResponse,cResponse;public int aVisible,bVisible,cVisible;
        }
        Texture2D Map(Func<int,int,Color> pixel)
        {var t=Keep(new Texture2D(8,8,TextureFormat.RGBAHalf,false,true));for(int y=0;y<8;y++)for(int x=0;x<8;x++)t.SetPixel(x,y,pixel(x,y));t.wrapMode=TextureWrapMode.Clamp;t.filterMode=FilterMode.Point;t.Apply(false);return t;}
        [TestCaseSource(nameof(Cases))]
        public void WorldSimulationCustomSpace_ActualMeshABC(string variant,bool negative,bool ortho)
        {
            string folder=Path.Combine(Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR")??Path.Combine(Path.GetDirectoryName(Application.dataPath),"Temp/NBFXCustomLocal"),variant+(negative?"-negative":"-positive")+(ortho?"-ortho":"-perspective"));Directory.CreateDirectory(folder);
            var actor=Keep(GameObject.CreatePrimitive(PrimitiveType.Quad));actor.layer=4;var renderer=actor.GetComponent<MeshRenderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            // Raw attributes are world-space simulation data. A deliberately
            // translated renderer proves that this is not ordinary local mesh.
            var mesh=Keep(Object.Instantiate(actor.GetComponent<MeshFilter>().sharedMesh));actor.GetComponent<MeshFilter>().sharedMesh=mesh;
            var simulation=Matrix4x4.TRS(new Vector3(.125f,-.0625f,0),Quaternion.Euler(0,12,3),new Vector3(1.5f,1.25f,1));
            var positions=mesh.vertices;var normals=mesh.normals;var tangents=mesh.tangents;
            for(int i=0;i<positions.Length;i++){positions[i]=simulation.MultiplyPoint3x4(positions[i]);normals[i]=simulation.inverse.transpose.MultiplyVector(normals[i]).normalized;var t=simulation.MultiplyVector(new Vector3(tangents[i].x,tangents[i].y,tangents[i].z)).normalized;tangents[i]=new Vector4(t.x,t.y,t.z,tangents[i].w);}
            mesh.vertices=positions;mesh.normals=normals;mesh.tangents=tangents;mesh.RecalculateBounds();actor.transform.position=new Vector3(.5f,.25f,0);
            mesh.SetUVs(1,new List<Vector4>{new Vector4(.2f,.66f,0,0),new Vector4(.8f,.66f,0,0),new Vector4(.2f,.86f,0,0),new Vector4(.8f,.86f,0,0)});
            var custom=Matrix4x4.TRS(new Vector3(.25f,-.125f,.125f),Quaternion.Euler(0,17,11),new Vector3(negative?-1.25f:1.25f,.75f,1.5f));
            var cameraObject=Keep(new GameObject("CustomLocal world-simulation Mesh camera"));var camera=cameraObject.AddComponent<Camera>();var scene=SceneManager.GetActiveScene();Assert.That(scene.IsValid()&&scene.isLoaded,Is.True);SceneManager.MoveGameObjectToScene(actor,scene);SceneManager.MoveGameObjectToScene(cameraObject,scene);camera.scene=scene;
            camera.orthographic=ortho;camera.orthographicSize=1.4f;camera.fieldOfView=42;camera.nearClipPlane=.1f;camera.farClipPlane=20;camera.transform.position=new Vector3(0,0,4);camera.transform.rotation=Quaternion.LookRotation(Vector3.back);camera.cullingMask=1<<4;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.0625f,.125f,.1875f,1);camera.allowHDR=true;camera.allowMSAA=false;cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=false;
            var map=Map((x,y)=>((x/2+y/2)&1)==0?new Color(.85f,.32f,.12f,1):new Color(.18f,.72f,.83f,1));var position=Map((x,y)=>new Color(.5f+.018f*x,.5f+.025f*y,.5f,.5f));var rotation=Map((x,y)=>new Color(.5f,.5f,.5f,1));var voMap=Map((x,y)=>new Color(.7f,.6f,.8f,1));var tyMap=Map((x,y)=>new Color(.1875f,.125f,.0625f,1));
            var materials=new[]{Keep(new Material(aShader)),Keep(new Material(bShader)),Keep(new Material(cShader))};var configure=typeof(G4GraphVATTests).GetMethod("Configure",Private);
            foreach(var m in materials)
            {
                bool graph=m==materials[2];configure.Invoke(null,new object[]{m,graph,"manual2",map,position,position,rotation,map,map,map});
                if(variant.StartsWith("vo-",StringComparison.Ordinal))
                {
                    m.SetFloat("_VertexOffset_Toggle",1);m.SetTexture("_VertexOffset_Map",voMap);m.SetFloat("_VertexOffset_NormalDir_Toggle",variant=="vo-normal"?1:3);m.SetFloat("_VertexOffset_DirectionSpace",variant=="vo-world"?1:0);m.SetVector("_VertexOffset_Vec",new Vector4(0,0,.25f,0));if(!graph)m.EnableKeyword("_VERTEX_OFFSET");
                }
                if(variant=="houdini-soft"){m.SetFloat("_VAT_Toggle",1);if(!graph){m.EnableKeyword("_VAT");m.EnableKeyword("_VAT_HOUDINI");m.EnableKeyword("_HOUDINI_VAT_SOFTBODY");}}
                if(variant=="tyflow-relative")
                {
                    m.SetFloat("_VAT_Toggle",1);m.SetFloat("_VATMode",1);m.SetFloat("_TyFlowVATSubMode",1);m.SetFloat("_Frame",1);m.SetFloat("_Frames",2);m.SetFloat("_ImportScale",1);m.SetFloat("_Autoplay",0);m.SetFloat("_Loop",0);m.SetFloat("_FrameInterpolation",0);m.SetFloat("_RGBAEncoded",0);m.SetFloat("_LinearToGamma",0);m.SetTexture("_VATTex",tyMap);
                    mesh.SetUVs(1,Enumerable.Range(0,4).Select(i=>new Vector4(i,4,0,0)).ToList());if(!graph){m.EnableKeyword("_VAT");m.EnableKeyword("_VAT_TYFLOW");m.EnableKeyword("_TYFLOW_VAT_RELATIVE");}
                }
                if(graph)for(int row=0;row<4;row++){m.SetVector("_NB_CustomLocalToWorld"+row,custom.GetRow(row));m.SetVector("_NB_CustomWorldToLocal"+row,custom.inverse.GetRow(row));}
                else {m.SetMatrix("_CustomLocalTransformLocalToWorld",custom);m.SetMatrix("_CustomLocalTransformWorldToLocal",custom.inverse);}
            }
            void Mode(Material m,bool enabled){if(m==materials[2])m.SetFloat("_NB_CustomLocalTransform",enabled?1:0);else if(enabled)m.EnableKeyword("_CUSTOM_LOCAL_TRANSFORM");else m.DisableKeyword("_CUSTOM_LOCAL_TRANSFORM");}
            var rt=Keep(new RenderTexture(128,128,24,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear));var read=Keep(new Texture2D(128,128,TextureFormat.RGBAHalf,false,true));var oldRT=RenderTexture.active;camera.targetTexture=rt;rt.Create();var draw=typeof(G4GraphVATTests).GetMethod("Draw",Private);
            Color[] Snap(Material m,string name)=>(Color[])draw.Invoke(null,new object[]{renderer,m,camera,rt,read,folder,name});
            try
            {
                renderer.sharedMaterial=materials[2];for(int i=0;i<4;i++)camera.Render();renderer.enabled=false;var clear=Snap(materials[2],"clear");renderer.enabled=true;Color[][] on=new Color[3][],off=new Color[3][],repeat=new Color[3][];
                for(int i=0;i<3;i++){Mode(materials[i],false);off[i]=Snap(materials[i],"ABC"[i]+"-off");Mode(materials[i],true);on[i]=Snap(materials[i],"ABC"[i]+"-on");repeat[i]=Snap(materials[i],"ABC"[i]+"-repeat");}
                var mtr=new Metrics{variant=variant,negative=negative,orthographic=ortho,api=SystemInfo.graphicsDeviceType.ToString(),unityVersion=Application.unityVersion,scope="CustomLocal world-simulation Mesh Forward ABC, nonuniform/negative custom matrix, normal/world VO andVAT examples. FullTBN/UV/helper/depth-shadow/Player/VFX pending",
                    finite=clear.Concat(on.SelectMany(x=>x)).Concat(off.SelectMany(x=>x)).Concat(repeat.SelectMany(x=>x)).All(p=>new[]{p.r,p.g,p.b,p.a}.All(v=>!float.IsNaN(v)&&!float.IsInfinity(v))),abOff=Delta(off[0],off[1]),bcOff=Delta(off[1],off[2]),abOn=Delta(on[0],on[1]),bcOn=Delta(on[1],on[2]),aRepeat=Delta(on[0],repeat[0]),bRepeat=Delta(on[1],repeat[1]),cRepeat=Delta(on[2],repeat[2]),aResponse=Delta(off[0],on[0]),bResponse=Delta(off[1],on[1]),cResponse=Delta(off[2],on[2]),aVisible=Visible(on[0],clear),bVisible=Visible(on[1],clear),cVisible=Visible(on[2],clear)};
                File.WriteAllText(Path.Combine(folder,"metrics.json"),JsonUtility.ToJson(mtr,true));Debug.Log("NBFX_G4_CUSTOM_LOCAL "+JsonUtility.ToJson(mtr));Assert.That(mtr.finite,Is.True);Assert.That(mtr.aVisible,Is.GreaterThan(150));Assert.That(mtr.bVisible,Is.GreaterThan(150));Assert.That(mtr.cVisible,Is.GreaterThan(150));Assert.That(mtr.abOff,Is.Zero);Assert.That(mtr.bcOff,Is.Zero);Assert.That(mtr.abOn,Is.Zero);Assert.That(mtr.bcOn,Is.Zero);Assert.That(mtr.aRepeat+mtr.bRepeat+mtr.cRepeat,Is.Zero);Assert.That(mtr.aResponse,Is.GreaterThan(.01f));Assert.That(mtr.bResponse,Is.GreaterThan(.01f));Assert.That(mtr.cResponse,Is.GreaterThan(.01f));
            }
            finally{camera.targetTexture=null;RenderTexture.active=oldRT;rt.Release();}
        }
        static float Delta(Color[] a,Color[] b){float m=0;for(int i=0;i<a.Length;i++){var d=a[i]-b[i];m=Mathf.Max(m,Mathf.Abs(d.r),Mathf.Abs(d.g),Mathf.Abs(d.b),Mathf.Abs(d.a));}return m;}
        static int Visible(Color[] a,Color[] b){int n=0;for(int i=0;i<a.Length;i++)if(Mathf.Max(Mathf.Abs(a[i].r-b[i].r),Mathf.Abs(a[i].g-b[i].g),Mathf.Abs(a[i].b-b[i].b))>.001f)n++;return n;}
    }
}
