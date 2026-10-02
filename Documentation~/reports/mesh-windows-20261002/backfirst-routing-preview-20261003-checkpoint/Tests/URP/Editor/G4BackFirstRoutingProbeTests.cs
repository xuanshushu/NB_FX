using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace NBFX.Baseline.Tests
{
    // Diagnostic, not BackFirst product implementation. Default URP draw only,
    // two independent tags/colors, no forced DrawRenderer/extra pass feature.
    public sealed class G4BackFirstRoutingProbeTests
    {
        readonly List<Object> owned=new List<Object>();
        T Keep<T>(T value)where T:Object{owned.Add(value);return value;}
        [TearDown]public void Cleanup(){foreach(var o in owned.AsEnumerable().Reverse())if(o)Object.DestroyImmediate(o);owned.Clear();}
        static IEnumerable<TestCaseData> Cases()
        {
            foreach(bool unlitFirst in new[]{true,false})foreach(bool transparent in new[]{true,false})foreach(bool ortho in new[]{true,false})
                yield return new TestCaseData(unlitFirst,transparent,ortho).SetName("G4BackRouting_"+(unlitFirst?"SRP-first":"Forward-first")+(transparent?"_transparent":"_opaque")+(ortho?"_ortho":"_perspective"));
        }
        Shader ProbeShader(bool unlitFirst,bool transparent)
        {
            string Pass(string name,string tag,string color)=>"Pass { Name \""+name+"\" Tags { \"LightMode\"=\""+tag+"\" } Cull Off ZWrite Off ZTest Always Blend One One HLSLPROGRAM\n#pragma vertex Vert\n#pragma fragment Frag\n#include \"Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl\"\nstruct A { float4 p:POSITION; }; struct V { float4 p:SV_POSITION; }; V Vert(A i) { V o; o.p=TransformObjectToHClip(i.p.xyz); return o; } half4 Frag(V i):SV_Target{return "+color+";}\nENDHLSL\n}";
            string r=Pass("RedDiagnostic","SRPDefaultUnlit","half4(.25,0,0,0)"),g=Pass("GreenDiagnostic","UniversalForward","half4(0,.5,0,0)");
            string text="Shader \"Hidden/NBFX/BackRoutingProbe\" { SubShader { Tags { \"RenderPipeline\"=\"UniversalPipeline\" \"Queue\"=\""+(transparent?"Transparent":"Geometry")+"\" } "+(unlitFirst?r+g:g+r)+"} }";
            var s=Keep(ShaderUtil.CreateShaderAsset(text,false));Assert.That(s&&s.isSupported,Is.True);s.hideFlags=HideFlags.HideAndDontSave;return s;
        }
        [Serializable]sealed class Result
        {
            public string api,unity,rendererType,observation,scope;
            public bool unlitFirst,transparent,orthographic,finite;
            public float repeatMax,redResponse,greenResponse;
            public int redVisible,greenVisible;
            public Color[] center;public string[] passReadback;
        }
        [TestCaseSource(nameof(Cases))]
        public void DefaultURP_TwoTags_IndependentVisibleAndRepeatEvidence(bool unlitFirst,bool transparent,bool ortho)
        {
            var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;Assert.That(pipeline,Is.Not.Null);var data=pipeline.rendererDataList[0];
            var rendererPath=Path.Combine(Path.GetDirectoryName(Application.dataPath),AssetDatabase.GetAssetPath(data));byte[] rendererBytes=File.ReadAllBytes(rendererPath);
            var nb=data.rendererFeatures.FirstOrDefault(f=>f&&f.GetType().FullName=="NBShader.NBPostProcess");bool oldNb=nb&&nb.isActive;
            var shader=ProbeShader(unlitFirst,transparent);var material=Keep(new Material(shader){hideFlags=HideFlags.HideAndDontSave});material.renderQueue=transparent?3000:2000;
            var scene=EditorSceneManager.NewPreviewScene();var actor=Keep(GameObject.CreatePrimitive(PrimitiveType.Quad));var go=Keep(new GameObject("DefaultURP dual-tag routing camera"));var camera=go.AddComponent<Camera>();go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=false;
            var rt=Keep(new RenderTexture(96,96,24,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear));var read=Keep(new Texture2D(96,96,TextureFormat.RGBAHalf,false,true));var oldRT=RenderTexture.active;bool oldAsync=ShaderUtil.allowAsyncCompilation;
            string folder=Path.Combine(Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR")??Path.Combine(Path.GetDirectoryName(Application.dataPath),"Temp/NBFXBackRouting"),(unlitFirst?"SRP-first":"Forward-first")+(transparent?"-transparent":"-opaque")+(ortho?"-ortho":"-perspective"));Directory.CreateDirectory(folder);
            try
            {
                ShaderUtil.allowAsyncCompilation=false;SceneManager.MoveGameObjectToScene(actor,scene);SceneManager.MoveGameObjectToScene(go,scene);actor.layer=4;actor.transform.localScale=new Vector3(2,2,1);var renderer=actor.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
                camera.scene=scene;camera.enabled=false;camera.orthographic=ortho;camera.orthographicSize=1.5f;camera.fieldOfView=42;camera.transform.position=new Vector3(0,0,4);camera.transform.rotation=Quaternion.LookRotation(Vector3.back);camera.nearClipPlane=.1f;camera.farClipPlane=20;camera.cullingMask=1<<4;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.125f,.125f,.125f,1);camera.allowHDR=true;camera.allowMSAA=false;camera.targetTexture=rt;Assert.That(rt.Create(),Is.True);if(nb)nb.SetActive(false);
                Color[] Capture(string label)
                {
                    for(int warm=0;warm<4;warm++)camera.Render();RenderTexture.active=rt;read.ReadPixels(new Rect(0,0,96,96),0,0,false);read.Apply(false,false);var pixels=read.GetPixels();using(var file=File.Create(Path.Combine(folder,label+".rgba-f32.gz")))using(var gzip=new GZipStream(file,System.IO.Compression.CompressionLevel.Optimal))using(var w=new BinaryWriter(gzip))foreach(var p in pixels){w.Write(p.r);w.Write(p.g);w.Write(p.b);w.Write(p.a);}return pixels;
                }
                float Delta(Color[] a,Color[] b)=>a.Zip(b,(x,y)=>Enumerable.Range(0,4).Max(i=>Mathf.Abs(x[i]-y[i]))).Max();
                int Visible(Color[] a,Color[] b)=>a.Zip(b,(x,y)=>Enumerable.Range(0,4).Any(i=>x[i]!=y[i])?1:0).Sum();
                string[] descriptions=new string[material.passCount];for(int i=0;i<material.passCount;i++)descriptions[i]=i+":"+material.GetPassName(i)+":"+shader.FindPassTagValue(i,new ShaderTagId("LightMode")).name;
                var frames=new Color[4][];var repeats=new Color[4][];var readbacks=new string[4];
                for(int state=0;state<4;state++)
                {
                    bool red=(state&1)!=0,green=(state&2)!=0;material.SetShaderPassEnabled("SRPDefaultUnlit",red);material.SetShaderPassEnabled("UniversalForward",green);
                    Assert.That(material.GetShaderPassEnabled("SRPDefaultUnlit"),Is.EqualTo(red));Assert.That(material.GetShaderPassEnabled("UniversalForward"),Is.EqualTo(green));
                    readbacks[state]=string.Join("|",descriptions)+" activeSRP="+material.GetShaderPassEnabled("SRPDefaultUnlit")+" activeForward="+material.GetShaderPassEnabled("UniversalForward");frames[state]=Capture("state"+state);repeats[state]=Capture("state"+state+"-repeat");
                }
                Color baseline=frames[0][48*96+48],both=frames[3][48*96+48]-baseline;string observation=both.r>.2f&&both.g>.4f?"both pass colors contributed":both.r>.2f?"only SRP color contributed":both.g>.4f?"only Forward color contributed":"unexpected/unsupported routing; retain raw";
                var result=new Result{api=SystemInfo.graphicsDeviceType.ToString(),unity=Application.unityVersion,rendererType=data.GetType().FullName,unlitFirst=unlitFirst,transparent=transparent,orthographic=ortho,scope="Default configured URP only; no extra renderer/pass/tag manager. Routes are observed, not presumed equivalent to NB BackFirst.",finite=frames.Concat(repeats).SelectMany(x=>x).All(p=>Enumerable.Range(0,4).All(i=>!float.IsNaN(p[i])&&!float.IsInfinity(p[i]))),repeatMax=Enumerable.Range(0,4).Max(i=>Delta(frames[i],repeats[i])),redResponse=Delta(frames[1],frames[0]),greenResponse=Delta(frames[2],frames[0]),redVisible=Visible(frames[1],frames[0]),greenVisible=Visible(frames[2],frames[0]),center=frames.Select(x=>x[48*96+48]).ToArray(),passReadback=readbacks,observation=observation};File.WriteAllText(Path.Combine(folder,"routing.json"),JsonUtility.ToJson(result,true));Debug.Log("NBFX_BACK_ROUTING "+JsonUtility.ToJson(result));
                Assert.That(result.finite,Is.True);Assert.That(result.repeatMax,Is.Zero);Assert.That(result.redResponse,Is.GreaterThan(.1f));Assert.That(result.greenResponse,Is.GreaterThan(.1f));Assert.That(result.redVisible,Is.GreaterThan(128));Assert.That(result.greenVisible,Is.GreaterThan(128));Assert.That(observation,Does.Not.StartWith("unexpected"));
            }
            finally{if(nb)nb.SetActive(oldNb);camera.targetTexture=null;RenderTexture.active=oldRT;rt.Release();ShaderUtil.allowAsyncCompilation=oldAsync;EditorSceneManager.ClosePreviewScene(scene);Assert.That(File.ReadAllBytes(rendererPath),Is.EqualTo(rendererBytes));}
        }
    }
}
