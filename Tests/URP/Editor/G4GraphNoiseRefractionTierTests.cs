using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace NBFX.Baseline.Tests
{
    // Original Refraction config/textures and existing SpecDebug real render harness.
    public sealed class G4GraphNoiseRefractionTierTests
    {
        const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
        readonly List<Object> owned=new List<Object>();
        static object Refraction(string name,params object[] args)=>typeof(G4GraphRefractionTests).GetMethod(name,All).Invoke(null,args);
        static Type TypeOf(string name)=>G4SpecDebugFixture.FindType(name);
        static string[] Raw()=> (string[])TypeOf("NBShader.NBShaderFeatureCatalog").GetField("RawKeywords",All).GetValue(null);
        static bool Apply(Material m,bool full)
        {object[] args={m,Enum.ToObject(TypeOf("NBShader.NBShaderFeatureTier"),3),full?Raw():Raw().Where(k=>k!="_DISTORT_REFRACTION").ToArray(),false};return(bool)TypeOf("NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier").GetMethod("ApplyGraphRefractionGroup",All).Invoke(null,args);}
        sealed class Snapshot
        {
            readonly object value;static Type Shared=>typeof(G4GraphGuiFeatureIntentTests).GetNestedType("Snapshot",BindingFlags.NonPublic);
            Snapshot(object value){this.value=value;}public static Snapshot Read(Material m)=>new Snapshot(Shared.GetMethod("Read",All).Invoke(null,new object[]{m}));
            public void Same(Material m,string label,params string[] allow)=>Shared.GetMethod("AssertSame",All).Invoke(value,new object[]{m,label,allow});
        }
        [OneTimeSetUp]public void Preflight()
        {
            Assert.That(Path.GetFullPath(Application.dataPath),Is.EqualTo(Path.GetFullPath(@"D:\UnityProject\NBUnityProject\.utmp\NBFXMeshValidation-20261002\Assets")).IgnoreCase);for(int i=0;i<SceneManager.sceneCount;++i){var s=SceneManager.GetSceneAt(i);Assert.That((s.name+"/"+s.path).IndexOf("TAI",StringComparison.OrdinalIgnoreCase),Is.LessThan(0));}
        }
        [TearDown]public void Cleanup(){foreach(var o in owned.AsEnumerable().Reverse())if(o)Object.DestroyImmediate(o);owned.Clear();}
        [TestCase(true,TestName="G4NoiseRefraction_GPU_IndependentChildTier_ortho")]
        [TestCase(false,TestName="G4NoiseRefraction_GPU_IndependentChildTier_perspective")]
        public void IndependentRefractionChildResponse(bool ortho)
        {
            using(var h=new G4SpecDebugFixture.Harness("noise-refraction-tier-"+(ortho?"ortho":"perspective"),ortho))
            {
                var empty=h.Snap("empty");var gradient=(Texture2D)Refraction("MakeGradient");owned.Add(gradient);var noise=(Texture2D)Refraction("MakeConstant",new Color(.2f,.7f,.5f,1));owned.Add(noise);var mask=(Texture2D)Refraction("MakeConstant",new Color(.6f,.3f,.7f,1));owned.Add(mask);var normal=(Texture2D)Refraction("MakeConstant",new Color(.5f,.5f,1,1));owned.Add(normal);
                for(int i=0;i<3;++i){Refraction("Configure",h.materials[i],i==2,"Forward",noise,mask,gradient);Refraction("ConfigureRefraction",h.materials[i],i==2,1.5f,"flat",normal,true);if(i==2)G4SpecDebugFixture.Validate(h.materials[i]);G4SpecDebugFixture.Harness.RestoreForward(h.materials[i],i==2);}
                Assert.That(Apply(h.materials[2],true),Is.True);var before=Snapshot.Read(h.materials[2]);var frames=new Color[3][][];var repeats=new Color[3][][];
                for(int stage=0;stage<3;++stage)
                {
                    Assert.That(Apply(h.materials[2],stage!=1),Is.True);Assert.That(h.materials[2].GetFloat("_DistortMode"),Is.EqualTo(1));Assert.That(h.materials[2].GetFloat("_NB_DistortionMode"),Is.EqualTo(0));before.Same(h.materials[2],"Only Refraction child effective gate changes; screen alias/Noise parent/raw remain.","_NB_TierAllowRefraction");frames[stage]=new Color[3][];repeats[stage]=new Color[3][];
                    for(int i=0;i<3;++i){if(i!=2)G4SpecDebugFixture.SetKeyword(h.materials[i],"_DISTORT_REFRACTION",stage!=1);frames[stage][i]=h.Snap("ABC"[i]+"-"+stage,h.materials[i]);repeats[stage][i]=h.Snap("ABC"[i]+"-"+stage+"-repeat",h.materials[i]);}
                }
                var metrics=new G4SpecDebugFixture.Metrics{caseId="noise-refraction-child-"+(ortho?"ortho":"perspective"),scope="Independent Refraction childTier only on existing main Forward: same original config/map/shared arithmetic, Noise parent remains active. No screen NBPost pipeline/complete Pass claim.",finite=G4SpecDebugFixture.Finite(empty)&&frames.SelectMany(f=>f).Concat(repeats.SelectMany(f=>f)).All(G4SpecDebugFixture.Finite),ab=frames.Select(f=>G4SpecDebugFixture.Delta(f[0],f[1])).ToArray(),bc=frames.Select(f=>G4SpecDebugFixture.Delta(f[1],f[2])).ToArray(),repeat=Enumerable.Range(0,3).SelectMany(s=>Enumerable.Range(0,3).Select(i=>G4SpecDebugFixture.Delta(frames[s][i],repeats[s][i]))).ToArray(),response=Enumerable.Range(0,3).Select(i=>G4SpecDebugFixture.Delta(frames[0][i],frames[1][i])).ToArray(),restore=Enumerable.Range(0,3).Select(i=>G4SpecDebugFixture.Delta(frames[0][i],frames[2][i])).ToArray(),visible=frames.SelectMany(f=>f).Select(f=>G4SpecDebugFixture.Visible(f,empty)).ToArray()};h.SaveAndAssert(metrics);Assert.That(metrics.response.All(v=>v>.01f),Is.True);before.Same(h.materials[2],"Full restored refraction gate and unchanged aliases/raw.");
            }
        }
    }
}
