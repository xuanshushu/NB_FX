using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace NBFX.Baseline.Tests
{
    // Ordinary Mesh actual URP Camera Forward B/C, native all-channel blending.
    // Three source-alpha values and repeated frames; no tolerance or shader variant substitution.
    public sealed class G4GraphForwardNativeAlphaBlendTests
    {
        const string GraphPath=G4SpecDebugFixture.GraphPath;
        [Serializable] sealed class Metrics
        {
            public string scope="Actual current Native/Graph Forward, legal Alpha/Additive, steady-state camera render; not first-frame or VFX",caseId,api,unityVersion,graphPassTag,nativePassTag;
            public int blendMode; public bool orthographic,finite,materialUnchanged;
            public float requestedBackgroundAlpha,actualBackgroundAlphaMin,actualBackgroundAlphaMax;
            public float[] sourceAlpha,bcRGBA,bcRGB,bcAlpha,nativeRepeat,graphRepeat,nativeAlphaResponse,graphAlphaResponse;
            public int[] nativeVisible,graphVisible;
            public string[] nativeBlendProperties,graphBlendProperties;
        }
        [OneTimeSetUp] public void ImportOwnTargetAndProveGeneratedForwardBlend()
        {
            G4SpecDebugFixture.PreflightImport();
            Assert.That(SystemInfo.graphicsDeviceType,Is.EqualTo(GraphicsDeviceType.Direct3D11));
            var type=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Unity.ShaderGraph.Editor").GetType("UnityEditor.ShaderGraph.ShaderGraphImporter",true);
            var methods=type.GetMethods(BindingFlags.Static|BindingFlags.NonPublic).Where(m=>m.Name=="GetShaderText"&&m.GetParameters().Length==4&&m.GetParameters()[0].ParameterType==typeof(string)&&m.GetParameters()[1].IsOut&&m.GetParameters()[3].IsOut).ToArray();
            Assert.That(methods.Length,Is.EqualTo(1));object[] args={GraphPath,null,null,null};string text=(string)methods[0].Invoke(null,args);
            int first=text.IndexOf("Name \"Universal Forward\"",StringComparison.Ordinal);Assert.That(first,Is.GreaterThanOrEqualTo(0));int end=text.IndexOf("HLSLPROGRAM",first,StringComparison.Ordinal);Assert.That(end,Is.GreaterThan(first));string states=text.Substring(first,end-first);
            var blendLines=states.Split('\n').Select(v=>v.Trim()).Where(v=>v.StartsWith("Blend ",StringComparison.Ordinal)).ToArray();
            string root=Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR")??Path.Combine(Path.GetDirectoryName(Application.dataPath),"Temp/NBFXForwardNativeAlpha");Directory.CreateDirectory(root);
            using(var hash=SHA256.Create())File.WriteAllText(Path.Combine(root,"actual-generated-forward-proof.txt"),"SHA256="+BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-","").ToLowerInvariant()+"\n"+states);
            Assert.That(blendLines,Is.EqualTo(new[]{"Blend [_SrcBlend] [_DstBlend]"}),"Actual generated original asset Forward must use native all-channel factors.");
        }
        static float ChannelDelta(Color[] a,Color[] b,int from,int until)
        {float max=0;for(int i=0;i<a.Length;++i)for(int k=from;k<until;++k)max=Mathf.Max(max,Mathf.Abs(a[i][k]-b[i][k]));return max;}
        static string State(Material m)
        {
            var properties=G4SpecDebugFixture.Properties(m).OrderBy(k=>k.Key).Select(k=>k.Key+"="+k.Value);
            var passes=Enumerable.Range(0,m.passCount).Select(i=>m.GetPassName(i)+":"+m.GetShaderPassEnabled(Tag(m,i)));
            return string.Join("\n",properties)+"\n"+string.Join(",",m.shaderKeywords.OrderBy(v=>v))+"\n"+string.Join(",",passes)+"\nqueue="+m.renderQueue;
        }
        static string Tag(Material m,int index)
        {string tag=m.shader.FindPassTagValue(index,new ShaderTagId("LightMode")).name;return string.IsNullOrEmpty(tag)?"SRPDefaultUnlit":tag;}
        static string[] BlendState(Material m)
        {return new[]{"_Surface","_Blend","_SrcBlend","_DstBlend","_SrcBlendAlpha","_DstBlendAlpha","_AlphaAll"}.Where(m.HasProperty).Select(v=>v+"="+m.GetFloat(v).ToString("R",System.Globalization.CultureInfo.InvariantCulture)).ToArray();}
        static void Configure(G4SpecDebugFixture.Harness h,int blendMode)
        {
            var b=h.materials[1];var c=h.materials[2];var baseMap=h.Constant(new Color(.5f,.5f,.5f,1));
            foreach(var m in new[]{b,c})
            {
                m.SetTexture("_BaseMap",baseMap);m.SetColor(m==c?"_Color":"_BaseColor",Color.white);m.SetColor("_ColorA",Color.white);
                m.SetFloat("_AlphaAll",1);m.SetFloat("_BaseColorIntensityForTimeline",1);m.SetFloat("_FxLightMode",0);
                m.SetFloat("_AdditiveToPreMultiplyAlphaLerp",0);m.SetFloat("_fogintensity",0);m.SetFloat("_Surface",1);m.SetFloat("_Blend",blendMode);m.SetFloat("_AlphaClip",0);
                m.DisableKeyword("_ALPHAPREMULTIPLY_ON");m.DisableKeyword("_ALPHAMODULATE_ON");
            }
            c.SetFloat("_NBShaderFeatureTier",3);G4SpecDebugFixture.Validate(c);
            // Restore the real Forward LightMode after stock validation, then select
            // the legal mode's RGB factors. Graph stock separate-alpha fields remain
            // One/dst so a fixture cannot hide the product's former alpha mismatch.
            G4SpecDebugFixture.Harness.RestoreForward(b,false);G4SpecDebugFixture.Harness.RestoreForward(c,true);
            float destination=blendMode==0?(float)BlendMode.OneMinusSrcAlpha:(float)BlendMode.One;
            foreach(var m in new[]{b,c}){m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",destination);m.SetFloat("_ZWrite",0);m.SetFloat("_ColorMask",15);m.renderQueue=3000;}
            c.SetFloat("_SrcBlendAlpha",(float)BlendMode.One);c.SetFloat("_DstBlendAlpha",destination);
            Assert.That(c.GetFloat("_Surface"),Is.EqualTo(1));Assert.That(c.GetFloat("_Blend"),Is.EqualTo(blendMode));
            Assert.That(c.GetFloat("_SrcBlendAlpha"),Is.EqualTo(1));Assert.That(b.GetFloat("_SrcBlend"),Is.EqualTo(c.GetFloat("_SrcBlend")));Assert.That(b.GetFloat("_DstBlend"),Is.EqualTo(c.GetFloat("_DstBlend")));
        }
        [TestCase(0,true,TestName="G4ForwardNativeAlpha_Alpha_ortho")]
        [TestCase(0,false,TestName="G4ForwardNativeAlpha_Alpha_perspective")]
        [TestCase(2,true,TestName="G4ForwardNativeAlpha_Additive_ortho")]
        [TestCase(2,false,TestName="G4ForwardNativeAlpha_Additive_perspective")]
        public void NativeAllChannelForwardBlending(int mode,bool ortho)
        {
            string id="forward-native-alpha-"+(mode==0?"alpha":"additive")+"-"+(ortho?"ortho":"perspective");
            using(var h=new G4SpecDebugFixture.Harness(id,ortho))
            {
                Configure(h,mode);h.camera.backgroundColor=new Color(.125f,.1875f,.25f,.625f);
                var empty=h.Snap("empty-background");var stages=new[]{.35f,.5f,1f};var bs=new List<Color[]>();var cs=new List<Color[]>();
                var m=new Metrics{caseId=id,blendMode=mode,orthographic=ortho,requestedBackgroundAlpha=.625f,actualBackgroundAlphaMin=empty.Min(v=>v.a),actualBackgroundAlphaMax=empty.Max(v=>v.a),sourceAlpha=stages,bcRGBA=new float[3],bcRGB=new float[3],bcAlpha=new float[3],nativeRepeat=new float[3],graphRepeat=new float[3],nativeVisible=new int[3],graphVisible=new int[3],finite=G4SpecDebugFixture.Finite(empty),materialUnchanged=true,api=SystemInfo.graphicsDeviceType.ToString(),unityVersion=Application.unityVersion,nativePassTag=h.tags[1],graphPassTag=h.tags[2]};
                for(int i=0;i<stages.Length;++i)
                {
                    h.materials[1].SetFloat("_AlphaAll",stages[i]);h.materials[2].SetFloat("_AlphaAll",stages[i]);
                    string beforeB=State(h.materials[1]),beforeC=State(h.materials[2]);string label=i==2?"alpha1-control":"low-alpha-"+i;
                    var b=h.Snap(label+"-B",h.materials[1]);var br=h.Snap(label+"-B-repeat",h.materials[1]);var c=h.Snap(label+"-C",h.materials[2]);var cr=h.Snap(label+"-C-repeat",h.materials[2]);bs.Add(b);cs.Add(c);
                    m.bcRGBA[i]=G4SpecDebugFixture.Delta(b,c);m.bcRGB[i]=ChannelDelta(b,c,0,3);m.bcAlpha[i]=ChannelDelta(b,c,3,4);m.nativeRepeat[i]=G4SpecDebugFixture.Delta(b,br);m.graphRepeat[i]=G4SpecDebugFixture.Delta(c,cr);m.nativeVisible[i]=G4SpecDebugFixture.Visible(b,empty);m.graphVisible[i]=G4SpecDebugFixture.Visible(c,empty);
                    m.finite&=new[]{b,br,c,cr}.All(G4SpecDebugFixture.Finite);m.materialUnchanged&=beforeB==State(h.materials[1])&&beforeC==State(h.materials[2]);
                }
                m.nativeAlphaResponse=new[]{ChannelDelta(bs[0],bs[2],3,4),ChannelDelta(bs[1],bs[2],3,4)};m.graphAlphaResponse=new[]{ChannelDelta(cs[0],cs[2],3,4),ChannelDelta(cs[1],cs[2],3,4)};m.nativeBlendProperties=BlendState(h.materials[1]);m.graphBlendProperties=BlendState(h.materials[2]);
                File.WriteAllText(Path.Combine(h.folder,"metrics.json"),JsonUtility.ToJson(m,true));Debug.Log("NBFX_NATIVE_FORWARD_ALPHA "+JsonUtility.ToJson(m));
                Assert.That(m.finite,Is.True);Assert.That(m.actualBackgroundAlphaMin,Is.GreaterThan(0),"Nonzero real background alpha required.");Assert.That(m.materialUnchanged,Is.True,"Rendering must preserve complete material inputs/keywords/pass toggles.");
                Assert.That(m.bcRGBA,Is.All.EqualTo(0),"Strict whole-frame RGBA B/C, including alpha; no tolerance.");Assert.That(m.nativeRepeat,Is.All.EqualTo(0));Assert.That(m.graphRepeat,Is.All.EqualTo(0));Assert.That(m.nativeVisible,Is.All.GreaterThan(128));Assert.That(m.graphVisible,Is.All.GreaterThan(128));
                Assert.That(m.nativeAlphaResponse,Is.All.GreaterThan(.01f));Assert.That(m.graphAlphaResponse,Is.All.GreaterThan(.01f),"Both low-alpha controls require strong actual alpha response.");
                foreach(var material in new[]{h.materials[1],h.materials[2]})Assert.That(ShaderUtil.GetShaderMessages(material.shader).Any(v=>v.severity.ToString()=="Error"),Is.False,material.shader.name);
            }
        }
    }
}
