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
    // Existing directed Mesh harness/textures only; two representative policies, no ten-mode rerun.
    public sealed class G4GraphVATTypedProjectionRenderTests
    {
        const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
        readonly List<Object> owned=new List<Object>();
        static Type Find(string name)=>G4SpecDebugFixture.FindType(name);
        static object Tier()=>Enum.ToObject(Find("NBShader.NBShaderFeatureTier"),3);
        static string[] Raw()=>(string[])Find("NBShader.NBShaderFeatureCatalog").GetField("RawKeywords",All).GetValue(null);
        static float Delta(Color[] a,Color[] b)=>a.Zip(b,(x,y)=>Enumerable.Range(0,4).Max(i=>Mathf.Abs(x[i]-y[i]))).Max();
        static int Visible(Color[] a,Color[] clear)=>a.Zip(clear,(x,y)=>Enumerable.Range(0,3).Any(i=>Mathf.Abs(x[i]-y[i])>.001f)).Count(x=>x);
        static bool Finite(IEnumerable<Color[]> frames)=>frames.SelectMany(x=>x).All(c=>Enumerable.Range(0,4).All(i=>!float.IsNaN(c[i])&&!float.IsInfinity(c[i])));
        static bool GraphProject(Material material,string[] policy)
        {object[] args={material,Tier(),policy,false};return (bool)Find("NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier").GetMethod("ApplyGraphVATProjectionGroup",All).Invoke(null,args);}
        static void NativeProject(Material material,string[] policy)
        {
            var resolver=Find("NBShader.NBShaderMaterialIntentResolver");var resolve=resolver.GetMethods(All).Single(m=>m.Name=="Resolve"&&m.GetParameters().Length==4);var passes=Find("NBShader.NBShaderPassFeatureCatalog").GetMethod("GetDefaultAllowedPassFeatures",All).Invoke(null,new[]{Tier()});var intent=resolve.Invoke(null,new object[]{material,Tier(),policy,passes});var effective=(string[])intent.GetType().GetField("effectiveKeywords",All).GetValue(intent);
            foreach(string keyword in Raw()){if(effective.Contains(keyword))material.EnableKeyword(keyword);else material.DisableKeyword(keyword);}
        }
        [OneTimeSetUp]public void Preflight()
        {Assert.That(Path.GetFullPath(Application.dataPath),Is.EqualTo(Path.GetFullPath(@"D:\UnityProject\NBUnityProject\.utmp\NBFXMeshValidation-20261002\Assets")).IgnoreCase);for(int i=0;i<SceneManager.sceneCount;++i){var scene=SceneManager.GetSceneAt(i);Assert.That((scene.name+"/"+scene.path).IndexOf("TAI",StringComparison.OrdinalIgnoreCase),Is.LessThan(0));}}
        [TearDown]public void Cleanup(){foreach(var item in owned.AsEnumerable().Reverse())if(item)Object.DestroyImmediate(item);owned.Clear();}
        [Serializable]sealed class Metrics
        {public string scope,unity,api;public int family;public bool finite;public float[] ab,bc,repeat,restore;public float familyResponse,frameResponse;public int[] visible;}
        [TestCase(0,TestName="G4VATTier_GPU_HoudiniFamilyDenyFallbackFrame_ortho")]
        [TestCase(1,TestName="G4VATTier_GPU_TyflowFamilyDenyFallbackFrameCD_ortho")]
        public void ActualFamilyPassThroughSelectedModeFallbackAndFrameControl(int family)
        {
            using(var harness=new G4SpecDebugFixture.Harness("vat-tier-family"+family+"-ortho",true))
            {
                var mesh=Object.Instantiate(harness.renderer.GetComponent<MeshFilter>().sharedMesh);owned.Add(mesh);harness.renderer.GetComponent<MeshFilter>().sharedMesh=mesh;
                mesh.uv2=new[]{new Vector2(.20f,.66f),new Vector2(.80f,.66f),new Vector2(.20f,.86f),new Vector2(.80f,.86f)};
                var original=typeof(G4GraphVATTests);var position=(Texture2D)original.GetMethod("PositionMap",All).Invoke(null,null);owned.Add(position);var checker=(Texture2D)original.GetMethod("Checker",All).Invoke(null,null);owned.Add(checker);var constant=harness.Constant(new Color(.5f,.5f,.5f,1));var tyflow=new G4GraphTyflowVATTests();
                try
                {
                    Texture2D vat=null;if(family==1)
                    {for(int channel=1;channel<8;++channel)mesh.SetUVs(channel,channel==1?Enumerable.Range(0,4).Select(i=>new Vector4(i,4,0,0)).ToList():Enumerable.Repeat(Vector4.zero,4).ToList());vat=(Texture2D)typeof(G4GraphTyflowVATTests).GetMethod("BuildVAT",All).Invoke(tyflow,new object[]{0,"raw",mesh});}
                    for(int i=0;i<3;++i)
                    {
                        var material=harness.materials[i];original.GetMethod("Configure",All).Invoke(null,new object[]{material,i==2,"manual2",checker,position,constant,constant,constant,constant,constant});material.SetFloat("_NBShaderFeatureTier",3);material.SetFloat("_VAT_Toggle",1);material.SetFloat("_VATMode",family);material.SetFloat("_HoudiniVATSubMode",1);material.SetFloat("_TyFlowVATSubMode",1);material.SetFloat("_FlipbookBlending",1);material.SetFloat("_B_autoPlayback",0);material.SetFloat("_B_interpolate",0);material.SetFloat("_Autoplay",0);
                        if(family==1){material.SetTexture("_VATTex",vat);material.SetFloat("_ImportScale",1);material.SetFloat("_Frames",2);material.SetFloat("_Frame",0);material.SetFloat("_FrameInterpolation",0);material.SetFloat("_Loop",0);material.SetFloat("_LinearToGamma",0);material.SetFloat("_RGBAEncoded",0);material.SetFloat("_VATIncludesNormals",0);}
                    }
                    var graph=harness.materials[2];graph.SetFloat("_NB_GraphGUIStateVersion",2);string familyKey=family==0?"_VAT_HOUDINI":"_VAT_TYFLOW",subKey=family==0?"_HOUDINI_VAT_RIGIDBODY":"_TYFLOW_VAT_RELATIVE";var fallback=Raw().Where(k=>k!=subKey).ToArray();var denied=fallback.Where(k=>k!=familyKey).ToArray();
                    void Policy(string[] policy){NativeProject(harness.materials[0],policy);NativeProject(harness.materials[1],policy);Assert.That(GraphProject(graph,policy),Is.True);}
                    Color[] Capture(int index,string label)=>harness.Snap(label,harness.materials[index]);
                    Policy(fallback);Assert.That(graph.GetFloat("_NB_TierVATFamily"),Is.EqualTo(family));Assert.That(graph.GetFloat("_NB_TierVATSubMode"),Is.Zero);Assert.That(graph.GetFloat("_NB_TierAllowFlipbook"),Is.Zero);Assert.That(harness.materials[1].IsKeywordEnabled(subKey),Is.False,"Native submode keyword truly absent: implicit fallback, not manually forcing mode0");
                    var clear=harness.Snap("clear");var on=Enumerable.Range(0,3).Select(i=>Capture(i,"ABC"[i]+"-fallback")).ToArray();var repeat=Enumerable.Range(0,3).Select(i=>Capture(i,"ABC"[i]+"-fallback-repeat")).ToArray();Policy(denied);Assert.That(graph.GetFloat("_NB_TierVATFamily"),Is.EqualTo(-2));Assert.That(graph.GetFloat("_NB_TierAllowFlipbook"),Is.Zero);var off=Enumerable.Range(0,3).Select(i=>Capture(i,"ABC"[i]+"-family-denied")).ToArray();Policy(fallback);var restored=Enumerable.Range(0,3).Select(i=>Capture(i,"ABC"[i]+"-restored")).ToArray();
                    if(family==0)foreach(var material in harness.materials)material.SetFloat("_displayFrame",1);
                    else
                    {
                        // Same real Tyflow Mesh FrameCD consumer, original word2 nibble28 and actual custom2 stream.
                        int bits=(int)Find("NBShader.NBShaderFlags").GetField("CustomData2XBit",All).GetValue(null);mesh.SetUVs(2,Enumerable.Repeat(new Vector4(1,0,0,0),4).ToList());for(int i=0;i<3;++i){if(i==2){harness.materials[i].SetFloat("_NB_CustomDataFlag2Lo16",0);harness.materials[i].SetFloat("_NB_CustomDataFlag2Hi16",bits<<12);}else harness.materials[i].SetInteger("_W9ParticleCustomDataFlag2",unchecked(bits<<28));}
                    }
                    var frame=Enumerable.Range(0,3).Select(i=>Capture(i,"ABC"[i]+"-frame-control")).ToArray();var all=new[]{clear}.Concat(on).Concat(repeat).Concat(off).Concat(restored).Concat(frame).ToArray();var data=new Metrics{scope="Original VAT textures and directed harness. Single orthographic representative family, selected mode1 stripped to Native mode0 fallback, family stripped passthrough, original intended VAT/Flipbook priority, one manual/FrameCD control. No ten-mode/Pass/Player/perf/Gate claim.",family=family,unity=Application.unityVersion,api=SystemInfo.graphicsDeviceType.ToString(),finite=Finite(all),ab=new[]{Delta(on[0],on[1]),Delta(off[0],off[1]),Delta(frame[0],frame[1])},bc=new[]{Delta(on[1],on[2]),Delta(off[1],off[2]),Delta(frame[1],frame[2])},repeat=Enumerable.Range(0,3).Select(i=>Delta(on[i],repeat[i])).ToArray(),restore=Enumerable.Range(0,3).Select(i=>Delta(on[i],restored[i])).ToArray(),familyResponse=Delta(on[2],off[2]),frameResponse=Delta(on[2],frame[2]),visible=on.Concat(off).Concat(restored).Concat(frame).Select(p=>Visible(p,clear)).ToArray()};File.WriteAllText(Path.Combine(harness.folder,"vat-tier-metrics.json"),JsonUtility.ToJson(data,true));Assert.That(data.finite,Is.True);Assert.That(data.ab.Concat(data.bc).Concat(data.repeat).Concat(data.restore).All(v=>v==0),Is.True,"Original strict0 ABC/repeat/restore retained");Assert.That(data.visible.All(n=>n>128),Is.True);Assert.That(data.familyResponse,Is.GreaterThan(.01f));Assert.That(data.frameResponse,Is.GreaterThan(.005f));foreach(var material in harness.materials){Assert.That(material.GetFloat("_VAT_Toggle"),Is.EqualTo(1));Assert.That(material.GetFloat("_FlipbookBlending"),Is.EqualTo(1));Assert.That(material.GetFloat(family==0?"_HoudiniVATSubMode":"_TyFlowVATSubMode"),Is.EqualTo(1));}
                }
                finally{tyflow.Cleanup();}
            }
        }
        [Test]public void G4VATTier_GPU_DeniedPassthroughBasisVertexLighting_ortho()
        {
            // Exact existing v4 bootstrap, transient PipelineScope and point-coverage control.
            var probeType=typeof(G4LightingDeniedSemanticsProbeTests);new G4LightingDeniedSemanticsProbeTests().Guard();
            var scopeType=probeType.GetNestedType("PipelineScope",All);Assert.That(scopeType,Is.Not.Null);
            IDisposable pipeline=null;G4SpecDebugFixture.Harness h=null;GameObject pointGO=null;
            try
            {
                pipeline=(IDisposable)Activator.CreateInstance(scopeType,All,null,new object[]{UnityEngine.Rendering.Universal.LightRenderingMode.PerVertex},null);
                h=new G4SpecDebugFixture.Harness("vat-denied-basis-vertex-ortho",true);var map=h.Constant(new Color(.55f,.35f,.18f,1));var b=h.materials[1];var c=h.materials[2];
                foreach(var material in new[]{b,c})
                {
                    typeof(G4GraphLightingTests).GetMethod("Configure",All).Invoke(null,new object[]{material,material==c,1,map});material.SetFloat("_VAT_Toggle",1);material.SetFloat("_VATMode",0);material.SetFloat("_HoudiniVATSubMode",1);material.SetFloat("_FlipbookBlending",1);material.SetFloat("_BlinnPhongSpecularToggle",0);material.DisableKeyword("_SPECULAR_COLOR");
                }
                c.SetFloat("_NB_GraphGUIStateVersion",2);c.SetFloat("_NBShaderFeatureTier",3);var allowed=Raw().Where(k=>k!="_VAT_HOUDINI").ToArray();NativeProject(b,allowed);Assert.That(GraphProject(c,allowed),Is.True);Assert.That(c.GetFloat("_NB_TierVATFamily"),Is.EqualTo(-2));Assert.That(c.GetFloat("_NB_TierAllowFlipbook"),Is.Zero);
                var sun=Resources.FindObjectsOfTypeAll<Light>().Single(l=>l&&l.gameObject.scene==h.camera.scene&&l.type==LightType.Directional);sun.intensity=1.4f;
                pointGO=new GameObject("VAT denied basis v4 point",typeof(Light));SceneManager.MoveGameObjectToScene(pointGO,h.camera.scene);var point=pointGO.GetComponent<Light>();point.type=LightType.Point;point.color=Color.blue;point.intensity=4;point.range=7;point.shadows=UnityEngine.LightShadows.None;point.transform.position=new Vector3(.7f,.4f,1.3f);
                var coverage=probeType.GetMethod("PreparePointCoverage",All).Invoke(null,new object[]{h.renderer,h.camera,point});Assert.That((bool)coverage.GetType().GetField("pointCoverageValid",All).GetValue(coverage),Is.True);
                var sh=new UnityEngine.Rendering.SphericalHarmonicsL2();sh.AddDirectionalLight(Vector3.forward,new Color(.25f,.05f,.02f),1);RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Custom;RenderSettings.ambientProbe=sh;
                var clear=h.Snap("clear");var bn=h.Snap("B-denied-point-on",b);var cn=h.Snap("C-denied-point-on",c);var repeat=h.Snap("C-repeat",c);point.enabled=false;var bf=h.Snap("B-denied-point-off",b);var cf=h.Snap("C-denied-point-off",c);point.enabled=true;var restored=h.Snap("C-restored",c);
                float response=Delta(cn,cf);int changed=cn.Zip(cf,(x,y)=>Enumerable.Range(0,4).Any(i=>Mathf.Abs(x[i]-y[i])>.01f)).Count(v=>v);var record=new Metrics{scope="Same exact v4 additional-Vertex point coverage. VAT family denied must feed raw passthrough basis/position into accepted producer VAT-after/VO-before. No new lighting-tier or full mode-matrix claim.",family=-1,unity=Application.unityVersion,api=SystemInfo.graphicsDeviceType.ToString(),finite=Finite(new[]{clear,bn,cn,repeat,bf,cf,restored}),ab=new float[0],bc=new[]{Delta(bn,cn),Delta(bf,cf)},repeat=new[]{Delta(cn,repeat)},restore=new[]{Delta(cn,restored)},familyResponse=0,frameResponse=response,visible=new[]{Visible(bn,clear),Visible(cn,clear),Visible(bf,clear),Visible(cf,clear)}};File.WriteAllText(Path.Combine(h.folder,"vat-basis-vertex-metrics.json"),JsonUtility.ToJson(record,true));File.WriteAllText(Path.Combine(h.folder,"geometry.json"),JsonUtility.ToJson(coverage,true));
                Assert.That(record.finite,Is.True);Assert.That(record.bc.Concat(record.repeat).Concat(record.restore).All(v=>v==0),Is.True);Assert.That(record.visible.All(v=>v>128),Is.True);Assert.That(Shader.IsKeywordEnabled("_ADDITIONAL_LIGHTS_VERTEX")&&!Shader.IsKeywordEnabled("_ADDITIONAL_LIGHTS")&&!Shader.IsKeywordEnabled("_CLUSTER_LIGHT_LOOP"),Is.True);Assert.That(response,Is.GreaterThan(.01f));Assert.That(changed,Is.GreaterThanOrEqualTo(64));
            }
            finally{if(pointGO)Object.DestroyImmediate(pointGO);h?.Dispose();pipeline?.Dispose();}
        }
    }
}
