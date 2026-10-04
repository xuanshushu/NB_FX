using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace NBFX.Baseline.Tests
{
    // Explicit two-feature Noise gate slice. No full Tier, Pass or GUI claim.
    public sealed class G4RootFresnelTierTests
    {
        const string Version = "_NB_GraphGUIStateVersion";
        const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly string[] Allows = { "_NB_TierAllowFresnel" };
        static readonly string[] Toggles = { "_noisemapEnabled", "_noiseMaskMap_Toggle" };
        static readonly string[] MaskKeywords = { "_NOISEMAP", "_NOISE_MASKMAP" };
        readonly List<Object> owned = new List<Object>();

        static Type FindType(string name)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name, false)).FirstOrDefault(t => t != null);
            Assert.That(type, Is.Not.Null, name); return type;
        }

        Material NewMaterial(bool graph = true)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(graph
                ? "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph"
                : "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader");
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            owned.Add(material); if (graph) material.SetFloat(Version, 2f); return material;
        }

        sealed class Snapshot
        {
            readonly object value;
            Snapshot(object value) { this.value = value; }
            static Type Shared => typeof(G4GraphGuiFeatureIntentTests).GetNestedType("Snapshot", BindingFlags.NonPublic);
            public static Snapshot Read(Material material) => new Snapshot(Shared.GetMethod("Read", Static).Invoke(null, new object[] { material }));
            public void AssertSame(Material material, string label, params string[] allowed)
                => Shared.GetMethod("AssertSame", Instance).Invoke(value, new object[] { material, label, allowed });
        }

        static string[] FresnelPolicy(string policy)
        {
            switch (policy)
            {
                case "full": return (string[])FindType("NBShader.NBShaderFeatureCatalog").GetField("RawKeywords", Static).GetValue(null);
                case "parent": return new[] { "_FRESNEL" };
                case "children": return new string[0];
                default: return new string[0];
            }
        }

        static bool ApplyFresnel(Material material, string policy, out bool changed, int tier = 3)
        {
            var applier = FindType("NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier");
            var method = applier.GetMethod("ApplyGraphFresnelGroup", Static); Assert.That(method, Is.Not.Null);
            object[] args = { material, Enum.ToObject(FindType("NBShader.NBShaderFeatureTier"), tier), FresnelPolicy(policy), false };
            bool accepted = (bool)method.Invoke(null, args); changed = (bool)args[3]; return accepted;
        }

        [TearDown] public void Cleanup()
        {
            foreach (Object item in owned.AsEnumerable().Reverse()) if (item) Object.DestroyImmediate(item);
            owned.Clear();
        }

        [Serializable] sealed class RootFresnelGPUMetrics
        {
            public string stage,api,unityVersion,scope;
            public bool orthographic,finite;
            public float[] abMax,bcMax,repeatMax;
            public float aFresnelResponse,bFresnelResponse,cFresnelResponse,aMaskResponse,bMaskResponse,cMaskResponse;
            public float aRestore,bRestore,cRestore;
            public int aVisible,bVisible,cVisible;
        }

        [TestCase("Forward",true,TestName="G4FresnelTier_GPU_Forward_ortho")] [TestCase("Forward",false,TestName="G4FresnelTier_GPU_Forward_perspective")]
        [TestCase("NBCameraOpaqueDistortPass",true,TestName="G4FresnelTier_GPU_NBCameraOpaqueDistortPass_ortho")] [TestCase("NBCameraOpaqueDistortPass",false,TestName="G4FresnelTier_GPU_NBCameraOpaqueDistortPass_perspective")]
        [TestCase("NBDeferredDistortPass",true,TestName="G4FresnelTier_GPU_NBDeferredDistortPass_ortho")] [TestCase("NBDeferredDistortPass",false,TestName="G4FresnelTier_GPU_NBDeferredDistortPass_perspective")]
        public void Fresnel_GPU_ActualForwardAndScreenConsumersRestoreIntent(string stage,bool ortho)
        {
            const int size=128;
            bool screen=stage!="Forward";
            var pipeline=(UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            var data=pipeline.rendererDataList[0];
            string project=Path.GetDirectoryName(Application.dataPath);
            string rendererFile=Path.Combine(project,AssetDatabase.GetAssetPath(data));byte[] beforeRenderer=File.ReadAllBytes(rendererFile);
            string folder=Path.Combine(Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR")??Path.Combine(project,"Temp/NBFXGUI2Noise"),stage+(ortho?"-ortho":"-perspective"));
            Directory.CreateDirectory(folder);
            var frozenShader=AssetDatabase.LoadAssetAtPath<Shader>("Packages/com.xuanxuan.nb.fx/Tests/Baseline/Frozen/NBShaders2/Shader/NBShader.shader");
            Assert.That(frozenShader&&frozenShader.isSupported,Is.True);
            var a=new Material(frozenShader){hideFlags=HideFlags.HideAndDontSave};owned.Add(a);
            var b=NewMaterial(false);var c=NewMaterial();
            var baseMap=(Texture2D)typeof(G4GraphScreenNoiseTests).GetMethod("MakeBackdrop",Static).Invoke(null,null);owned.Add(baseMap);
            Texture2D Constant(Color value)
            {
                var texture=new Texture2D(1,1,TextureFormat.RGBAHalf,false,true);texture.SetPixel(0,0,value);texture.Apply(false);
                texture.wrapMode=TextureWrapMode.Repeat;texture.filterMode=FilterMode.Point;owned.Add(texture);return texture;
            }
            var noise=Constant(new Color(.75f,.25f,0,.5f));var mask=Constant(new Color(.5f,.125f,.75f,1));
            var scene=EditorSceneManager.NewPreviewScene();
            var foreground=GameObject.CreatePrimitive(PrimitiveType.Quad);var background=GameObject.CreatePrimitive(PrimitiveType.Quad);
            var cameraObject=new GameObject("GUI2 real Noise pair camera");var camera=cameraObject.AddComponent<Camera>();
            var cameraData=cameraObject.AddComponent<UniversalAdditionalCameraData>();
            var backdrop=new Material(Shader.Find("Universal Render Pipeline/Unlit"));owned.Add(backdrop);
            var rt=new RenderTexture(size,size,24,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear);
            var read=new Texture2D(size,size,TextureFormat.RGBAHalf,false,true);
            var previous=RenderTexture.active;
            var nb=data.rendererFeatures.FirstOrDefault(f=>f&&f.GetType().FullName=="NBShader.NBPostProcess");
            Assert.That(nb,Is.Not.Null);bool nbWasActive=nb.isActive;
            G4ScreenNoiseDirectedFeature directed=null;
            try
            {
                foreach(var go in new[]{foreground,background,cameraObject})SceneManager.MoveGameObjectToScene(go,scene);
                foreground.layer=screen?G4GraphScreenNoiseTests.ForegroundLayer:4;
                foreground.transform.position=new Vector3(0,0,2);foreground.transform.localScale=new Vector3(2,2,1);
                var renderer=foreground.GetComponent<MeshRenderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;
                background.layer=2;background.transform.position=new Vector3(0,0,1);background.transform.localScale=new Vector3(6,6,1);
                backdrop.SetTexture("_BaseMap",baseMap);backdrop.SetColor("_BaseColor",Color.white);backdrop.SetFloat("_Cull",0);backdrop.renderQueue=2000;
                var backgroundRenderer=background.GetComponent<MeshRenderer>();backgroundRenderer.sharedMaterial=backdrop;
                backgroundRenderer.shadowCastingMode=ShadowCastingMode.Off;backgroundRenderer.enabled=stage=="NBCameraOpaqueDistortPass";
                foreach(var material in new[]{a,b,c})
                {
                    bool graph=material==c;
                    if(screen)typeof(G4GraphScreenNoiseTests).GetMethod("Configure",Static).Invoke(null,new object[]{material,graph,noise,mask,stage,"mask-half"});
                    else
                    {
                        typeof(G4GraphTextureNoiseTests).GetMethod("Configure",Static).Invoke(null,new object[]{material,graph,"base",baseMap,noise,mask});
                        material.SetFloat("_noisemapEnabled",1);material.SetFloat("_noiseMaskMap_Toggle",1);
                        material.SetFloat("_NoiseIntensity",.5f);material.SetVector("_DistortionDirection",new Vector4(.5f,.75f,0,0));
                        if(!graph){material.EnableKeyword("_NOISEMAP");material.EnableKeyword("_NOISE_MASKMAP");}
                    }
                    material.SetFloat("_VAT_Toggle",0);material.SetFloat("_FlipbookBlending",0);material.SetFloat("_FxLightMode",0);
                    material.SetFloat("_fresnelEnabled",1); material.SetVector("_FresnelUnit",new Vector4(.5f,1,.75f,0));
                    material.SetColor("_FresnelColor",Color.red); material.SetVector("_FresnelRotation",Vector4.zero);
                    if(stage=="NBCameraOpaqueDistortPass")
                    {
                        // Existing Float intensity, same A/B/C input. The
                        // original 0.5 gives insufficient final-color response.
                        material.SetFloat(graph?"_NB_DistortionIntensity":"_ScreenDistortIntensity",2f);
                    }
                    bool fade=screen||!ortho;
                    var flagType=FindType("NBShader.NBShaderFlags");var flag=Activator.CreateInstance(flagType,new object[]{material});
                    int bit=(int)flagType.GetField("FLAG_BIT_PARTICLE_FRESNEL_FADE_ON",Static).GetValue(null);
                    flagType.GetMethod(fade?"SetFlagBits":"ClearFlagBits").Invoke(flag,new object[]{bit,null,0});
                    if(graph)material.SetFloat("_FresnelMode",fade?1:0);
                    else material.EnableKeyword("_FRESNEL");

                    material.SetShaderPassEnabled("DepthNormalsOnly",false);
                }
                c.SetFloat(Version,2);c.SetVector("_NB_DistortionNoise",Vector4.zero); // Match the legacy Noise-off fallback for this comparison.
                if(!screen){c.SetShaderPassEnabled("SRPDefaultUnlit",true);c.SetShaderPassEnabled("UniversalForward",true);}
                camera.scene=scene;camera.orthographic=ortho;camera.orthographicSize=1.5f;camera.fieldOfView=45;
                camera.nearClipPlane=.1f;camera.farClipPlane=20;camera.transform.position=new Vector3(0,0,5);camera.transform.rotation=Quaternion.Euler(0,180,0);
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.allowHDR=true;camera.allowMSAA=false;
                camera.cullingMask=(1<<foreground.layer)|(1<<2);camera.targetTexture=rt;cameraData.requiresColorTexture=screen;cameraData.renderPostProcessing=false;
                rt.Create();Assert.That(rt.IsCreated()&&!rt.sRGB,Is.True);nb.SetActive(false);
                if(screen)
                {
                    directed=ScriptableObject.CreateInstance<G4ScreenNoiseDirectedFeature>();directed.hideFlags=HideFlags.HideAndDontSave;
                    directed.targetCamera=camera;directed.selectedPass=stage;directed.Create();directed.SetActive(true);data.rendererFeatures.Add(directed);data.SetDirty();
                }
                Color[] Capture(string name)
                {
                    camera.Render();RenderTexture.active=rt;read.ReadPixels(new Rect(0,0,size,size),0,0,false);read.Apply(false,false);
                    var pixels=read.GetPixels();
                    using(var stream=File.Create(Path.Combine(folder,name+".rgba-f32.gz")))
                    using(var gzip=new System.IO.Compression.GZipStream(stream,System.IO.Compression.CompressionLevel.Optimal))
                    using(var writer=new BinaryWriter(gzip))foreach(var pixel in pixels){writer.Write(pixel.r);writer.Write(pixel.g);writer.Write(pixel.b);writer.Write(pixel.a);}
                    return pixels;
                }
                float Delta(Color[] x,Color[] y)
                {
                    float max=0;for(int i=0;i<x.Length;i++)for(int channel=0;channel<4;channel++)max=Mathf.Max(max,Mathf.Abs(x[i][channel]-y[i][channel]));return max;
                }
                bool Finite(Color[] pixels)=>pixels.All(p=>Enumerable.Range(0,4).All(i=>!float.IsNaN(p[i])&&!float.IsInfinity(p[i])));
                int Visible(Color[] x,Color[] y)=>Enumerable.Range(0,x.Length).Count(i=>Enumerable.Range(0,4).Any(channel=>x[i][channel]!=y[i][channel]));
                renderer.enabled=false;for(int i=0;i<4;i++)camera.Render();var empty=Capture("background");renderer.enabled=true;
                var beforeA=Snapshot.Read(a);var beforeB=Snapshot.Read(b);var beforeC=Snapshot.Read(c);
                var frames=new Color[3][][];var repeats=new Color[3][][];
                string[] policies={"full","none","full"};string[] labels={"allowed","denied","restored"};
                for(int state=0;state<3;state++)
                {
                    bool changed;Assert.That(ApplyFresnel(c,policies[state],out changed),Is.True);
                    bool effective=c.GetFloat("_NB_TierAllowFresnel")>.5f;
                    foreach(var material in new[]{a,b})
                    { if(effective)material.EnableKeyword("_FRESNEL");else material.DisableKeyword("_FRESNEL"); }
                    frames[state]=new Color[3][];repeats[state]=new Color[3][];
                    var materials=new[]{a,b,c};
                    for(int m=0;m<3;m++)
                    {
                        renderer.sharedMaterial=materials[m];for(int i=0;i<3;i++)camera.Render();
                        frames[state][m]=Capture(((char)('A'+m))+"-"+labels[state]);repeats[state][m]=Capture(((char)('A'+m))+"-"+labels[state]+"-repeat");
                    }
                }
                var record=new RootFresnelGPUMetrics{stage=stage,orthographic=ortho,api=SystemInfo.graphicsDeviceType.ToString(),unityVersion=Application.unityVersion,
                    scope="Fresnel effective input only; Forward ortho=color, Forward perspective=alpha and exact NB RT paths=alpha. Raw intent retained; no Controller/Player/Gate claim",
                    finite=Finite(empty)&&frames.SelectMany(s=>s).All(Finite)&&repeats.SelectMany(s=>s).All(Finite),
                    abMax=Enumerable.Range(0,3).Select(s=>Delta(frames[s][0],frames[s][1])).ToArray(),
                    bcMax=Enumerable.Range(0,3).Select(s=>Delta(frames[s][1],frames[s][2])).ToArray(),
                    repeatMax=Enumerable.Range(0,3).Select(s=>Mathf.Max(Delta(frames[s][0],repeats[s][0]),Delta(frames[s][1],repeats[s][1]),Delta(frames[s][2],repeats[s][2]))).ToArray(),
                    aFresnelResponse=Delta(frames[0][0],frames[1][0]),bFresnelResponse=Delta(frames[0][1],frames[1][1]),cFresnelResponse=Delta(frames[0][2],frames[1][2]),
                    aRestore=Delta(frames[0][0],frames[2][0]),bRestore=Delta(frames[0][1],frames[2][1]),cRestore=Delta(frames[0][2],frames[2][2]),
                    aVisible=Visible(frames[0][0],empty),bVisible=Visible(frames[0][1],empty),cVisible=Visible(frames[0][2],empty)};
                File.WriteAllText(Path.Combine(folder,"metrics.json"),JsonUtility.ToJson(record,true));Debug.Log("NBFX_GUI2_NOISE_GPU "+JsonUtility.ToJson(record));
                beforeA.AssertSame(a,"Frozen complete intent restored");beforeB.AssertSame(b,"Current complete intent restored");beforeC.AssertSame(c,"Graph complete intent preserved");
                Assert.That(record.finite,Is.True);Assert.That(record.abMax.All(v=>v==0)&&record.bcMax.All(v=>v==0)&&record.repeatMax.All(v=>v==0),Is.True);
                Assert.That(record.aRestore+record.bRestore+record.cRestore,Is.Zero);
                Assert.That(record.aVisible,Is.GreaterThan(128));Assert.That(record.bVisible,Is.GreaterThan(128));Assert.That(record.cVisible,Is.GreaterThan(128));
                Assert.That(record.aFresnelResponse,Is.GreaterThan(.01f));Assert.That(record.bFresnelResponse,Is.GreaterThan(.01f));Assert.That(record.cFresnelResponse,Is.GreaterThan(.01f));
            }
            finally
            {
                nb.SetActive(nbWasActive);if(directed){data.rendererFeatures.Remove(directed);data.SetDirty();Object.DestroyImmediate(directed);}
                camera.targetTexture=null;RenderTexture.active=previous;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(read);EditorSceneManager.ClosePreviewScene(scene);
                Assert.That(File.ReadAllBytes(rendererFile),Is.EqualTo(beforeRenderer),"GUI2 GPU fixture saved renderer asset");
            }
        }

    }
}
