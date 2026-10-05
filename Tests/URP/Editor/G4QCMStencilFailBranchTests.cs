using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
namespace NBFX.Baseline.Tests
{
    // Original real URP Fixture, original B/C contract. No GUI/ABC/Queue claim.
    public sealed class G4QCMStencilFailBranchTests
    {
        const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
        Shader background,writer,probe;
        static Type Original=>typeof(G4GraphRenderStateTests);
        static Shader Make(string name,string stencil,string colorMask,string output)=>(Shader)Original.GetMethod("MakeAuxiliaryShader",All).Invoke(null,new object[]{name,stencil,colorMask,output});
        [OneTimeSetUp]public void ExistingAuxiliaryShadersOnly()
        {
            Assert.That((GraphicsFormat)Original.GetMethod("SupportedStencilFormat",All).Invoke(null,null),Is.Not.EqualTo(GraphicsFormat.None),"Unknown/unsupported stencil attachment cannot pass this branch test.");
            background=Make("QCMFailBackdrop","","RGBA","half4(0.125, 0.25, 0.5, 0.25)");
            writer=Make("QCMFailWriter","Stencil { Ref [_WriterRef] Comp Always Pass Replace ReadMask 255 WriteMask [_WriterMask] }","0","half4(0, 0, 0, 0)");
            probe=Make("QCMFailProbe","Stencil { Ref 5 ReadMask 15 Comp Equal Pass Keep }","RGBA","half4(0.8, 0.1, 0.9, 0.6)");
            Assert.That(background&&writer&&probe,Is.True); // No AssetDatabase import.
        }
        [OneTimeTearDown]public void Cleanup(){foreach(var s in new[]{background,writer,probe})if(s)Object.DestroyImmediate(s);}
        [Serializable]sealed class Metrics
        {
            public string branch,unity,api,scope;
            public bool orthographic,finite,stencilAttachment;
            public float bcMax,repeatB,repeatC,bControl,cControl,keepBC;
            public int visibleB,visibleC;
        }
        [TestCase("Fail",true,TestName="G4QCMStencil_FailReplaceKeep_ortho")]
        [TestCase("Fail",false,TestName="G4QCMStencil_FailReplaceKeep_perspective")]
        [TestCase("ZFail",true,TestName="G4QCMStencil_ZFailReplaceKeep_ortho")]
        [TestCase("ZFail",false,TestName="G4QCMStencil_ZFailReplaceKeep_perspective")]
        public void ActualStencilRejectBranchWritesOnlyChosenOperation(string branch,bool ortho)
        {
            var fixtureType=Original.GetNestedType("Fixture",All);Assert.That(fixtureType,Is.Not.Null);
            var f=Activator.CreateInstance(fixtureType,All,null,new object[]{background,writer,probe,ortho,true},null);
            var graph=(Material)fixtureType.GetField("Graph",All).GetValue(f);var legacy=(Material)fixtureType.GetField("Legacy",All).GetValue(f);
            var target=(RenderTexture)fixtureType.GetField("Target",All).GetValue(f);
            string project=Path.GetDirectoryName(Application.dataPath),directory=Path.Combine(Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR")??Path.Combine(project,"Temp/NBFXQCM"),branch+(ortho?"-ortho":"-perspective"));Directory.CreateDirectory(directory);
            Color[] Capture(Material m,string name)=>(Color[])fixtureType.GetMethod("Capture",All).Invoke(f,new object[]{m,Path.Combine(directory,name)});
            float Delta(Color[] a,Color[] b)=>a.Zip(b,(x,y)=>Enumerable.Range(0,4).Max(i=>Mathf.Abs(x[i]-y[i]))).Max();
            bool Finite(Color[] a)=>a.All(c=>Enumerable.Range(0,4).All(i=>!float.IsNaN(c[i])&&!float.IsInfinity(c[i])));
            int Visible(Color[] a,Color[] b)=>a.Zip(b,(x,y)=>Enumerable.Range(0,4).Any(i=>x[i]!=y[i])).Count(x=>x);
            try
            {
                fixtureType.GetProperty("WriterEnabled",All).SetValue(f,false);fixtureType.GetProperty("ProbeEnabled",All).SetValue(f,true);
                fixtureType.GetMethod("ConfigureFront",All).Invoke(f,new object[]{0,branch=="Fail"?CompareFunction.Never:CompareFunction.Always,5,255,15,StencilOp.Keep});
                foreach(var m in new[]{legacy,graph}){m.SetFloat("_ZTest",(float)(branch=="ZFail"?CompareFunction.Never:CompareFunction.Always));m.SetFloat("_Stencil"+branch,(float)StencilOp.Replace);}
                var empty=Capture(null,"empty");var b=Capture(legacy,"B-on");var br=Capture(legacy,"B-repeat");var c=Capture(graph,"C-on");var cr=Capture(graph,"C-repeat");
                foreach(var m in new[]{legacy,graph})m.SetFloat("_Stencil"+branch,(float)StencilOp.Keep);
                var bk=Capture(legacy,"B-keep");var ck=Capture(graph,"C-keep");
                var data=new Metrics{branch=branch,orthographic=ortho,unity=Application.unityVersion,api=SystemInfo.graphicsDeviceType.ToString(),scope="Real original B/C fixture; Comp rejection vs depth rejection independently writes stencil for late Equal5 probe; no new pass/layout or whole Gate claim.",finite=new[]{empty,b,br,c,cr,bk,ck}.All(Finite),stencilAttachment=target.descriptor.depthStencilFormat==(GraphicsFormat)Original.GetMethod("SupportedStencilFormat",All).Invoke(null,null),bcMax=Delta(b,c),repeatB=Delta(b,br),repeatC=Delta(c,cr),bControl=Delta(b,bk),cControl=Delta(c,ck),keepBC=Delta(bk,ck),visibleB=Visible(b,empty),visibleC=Visible(c,empty)};
                File.WriteAllText(Path.Combine(directory,"metrics.json"),JsonUtility.ToJson(data,true));Debug.Log("NBFX_QCM_FAIL_BRANCH "+JsonUtility.ToJson(data));
                Assert.That(data.finite&&data.stencilAttachment,Is.True);Assert.That(data.bcMax,Is.Zero);Assert.That(data.keepBC,Is.Zero);Assert.That(data.repeatB,Is.Zero);Assert.That(data.repeatC,Is.Zero);
                Assert.That(data.visibleB,Is.GreaterThan(128));Assert.That(data.visibleC,Is.GreaterThan(128));Assert.That(data.bControl,Is.GreaterThan(.04f));Assert.That(data.cControl,Is.GreaterThan(.04f));
                Assert.That(Delta(bk,empty),Is.Zero);Assert.That(Delta(ck,empty),Is.Zero,"Keep negative control cannot write the late probe.");
            }
            finally{((IDisposable)f).Dispose();}
        }
    }
}
