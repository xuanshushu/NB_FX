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
    // Explicit three-feature gate slice. No full Tier, Pass or GUI claim.
    public sealed class G4GraphMaskTierTests
    {
        const string Version = "_NB_GraphGUIStateVersion";
        const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly string[] Allows = { "_NB_TierAllowMask", "_NB_TierAllowMask2", "_NB_TierAllowMask3" };
        static readonly string[] Toggles = { "_Mask_Toggle", "_Mask2_Toggle", "_Mask3_Toggle" };
        static readonly string[] MaskKeywords = { "_MASKMAP_ON", "_MASKMAP2_ON", "_MASKMAP3_ON" };
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

        static string[] MaskPolicy(string policy)
        {
            switch (policy)
            {
                case "full": return (string[])FindType("NBShader.NBShaderFeatureCatalog").GetField("RawKeywords", Static).GetValue(null);
                case "parent": return new[] { "_MASKMAP_ON" };
                case "children": return new[] { "_MASKMAP2_ON", "_MASKMAP3_ON" };
                default: return new string[0];
            }
        }

        static bool ApplyMaskGroup(Material material, string policy, out bool changed, int tier = 3)
        {
            var applier = FindType("NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier");
            var method = applier.GetMethod("ApplyGraphMaskGroup", Static); Assert.That(method, Is.Not.Null);
            object[] args = { material, Enum.ToObject(FindType("NBShader.NBShaderFeatureTier"), tier), MaskPolicy(policy), false };
            bool accepted = (bool)method.Invoke(null, args); changed = (bool)args[3]; return accepted;
        }

        [TearDown] public void Cleanup()
        {
            foreach (Object item in owned.AsEnumerable().Reverse()) if (item) Object.DestroyImmediate(item);
            owned.Clear();
        }

        static IEnumerable<TestCaseData> CPUCases()
        {
            foreach (string policy in new[] { "full", "parent", "children", "none" })
                for (int intent = 0; intent < 8; ++intent)
                    yield return new TestCaseData(policy, intent).SetName("G4TierMasks_CPU_" + policy + "_i" + intent);
        }

        [TestCaseSource(nameof(CPUCases))]
        public void ParentFilteredGroup_PreservesCompleteIntent(string policy, int intent)
        {
            var material = NewMaterial();
            for (int i = 0; i < 3; ++i) { material.SetFloat(Toggles[i], (intent >> i) & 1); material.SetFloat(Allows[i], .25f); }
            material.SetFloat("_NB_Flags0Lo16", -3.75f); material.SetFloat("_NB_Flags1Hi16", 65536.25f);
            var before = Snapshot.Read(material); bool changed;
            Assert.That(ApplyMaskGroup(material, policy, out changed), Is.True); Assert.That(changed, Is.True);
            bool parent = (intent & 1) != 0 && (policy == "full" || policy == "parent");
            Assert.That(material.GetFloat(Allows[0]), Is.EqualTo(parent ? 1 : 0));
            Assert.That(material.GetFloat(Allows[1]), Is.EqualTo(parent && (intent & 2) != 0 && policy == "full" ? 1 : 0));
            Assert.That(material.GetFloat(Allows[2]), Is.EqualTo(parent && (intent & 4) != 0 && policy == "full" ? 1 : 0));
            before.AssertSame(material, "Only the three derived Float gates may change", Allows);
            Assert.That(ApplyMaskGroup(material, policy, out changed), Is.True); Assert.That(changed, Is.False);
            before.AssertSame(material, "Idempotent projection must preserve intent", Allows);
        }

        [TestCase(0, TestName = "G4TierMasks_CPU_Restore")]
        public void LowToHighestTier_RestoresIntentAndKeepsGeneralApplyGuard(int lowTier)
        {
            var material = NewMaterial(); foreach (string toggle in Toggles) material.SetFloat(toggle, 1f);
            var before = Snapshot.Read(material); bool changed;
            Assert.That(ApplyMaskGroup(material, "none", out changed, lowTier), Is.True); Assert.That(changed, Is.True);
            Assert.That(Allows.All(p => material.GetFloat(p) == 0f), Is.True);
            Assert.That(ApplyMaskGroup(material, "full", out changed, 3), Is.True); Assert.That(changed, Is.True);
            before.AssertSame(material, "Highest policy restores gates without losing serialized intent");
            var method = FindType("NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier").GetMethods(Static).Single(m => m.Name == "Apply" && m.GetParameters().Length == 5);
            object[] args = { material, Enum.ToObject(FindType("NBShader.NBShaderFeatureTier"), 0), true, true, false };
            Assert.That(method.Invoke(null, args), Is.False); Assert.That(args[4], Is.False);
            before.AssertSame(material, "Ordinary Apply remains Graph protected");
        }

        [TestCase(1f, TestName = "G4TierMasks_CPU_RejectMarker1")]
        [TestCase(3f, TestName = "G4TierMasks_CPU_RejectMarker3")]
        public void UnknownSchemaVersion_DoesNotWriteDerivedGates(float marker)
        {
            var material = NewMaterial(); material.SetFloat(Version, marker); var before = Snapshot.Read(material);
            bool changed; Assert.That(ApplyMaskGroup(material, "none", out changed), Is.False); Assert.That(changed, Is.False);
            before.AssertSame(material, "Unknown schema cannot partially apply");
        }

        [TestCase(false, TestName = "G4TierMasks_CPU_RejectMissingGate")]
        [TestCase(true, TestName = "G4TierMasks_CPU_RejectIntegerGate")]
        public void IncompleteDerivedFloatSchema_IsAtomic(bool integer)
        {
            string source = "Shader \"Hidden/NBFX/MaskGateIncomplete\" { Properties { _NB_TierAllowMask(\"M\",Float)=1 " +
                "_NB_TierAllowMask2(\"M2\",Float)=1 " + (integer ? "_NB_TierAllowMask3(\"M3\",Integer)=1 " : "") + "} SubShader { Pass { } } }";
            var shader = ShaderUtil.CreateShaderAsset(source, false); Assert.That(shader, Is.Not.Null);
            shader.hideFlags = HideFlags.HideAndDontSave; owned.Add(shader);
            var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave }; owned.Add(material);
            var before = Snapshot.Read(material); bool changed;
            Assert.That(ApplyMaskGroup(material, "none", out changed), Is.False); Assert.That(changed, Is.False);
            before.AssertSame(material, "Missing/wrong derived schema cannot partially apply");
        }

        [Serializable] sealed class MaskTierGPUMetrics
        {
            public string stage,api,unityVersion,scope;
            public bool orthographic,finite;
            public float[] abMax,bcMax,repeatMax;
            public float aMaskResponse,bMaskResponse,cMaskResponse;
            public float aRestore,bRestore,cRestore;
            public int aVisible,bVisible,cVisible;
        }

        [TestCase("Forward",true, TestName = "G4TierMasks_GPU_Forward_ortho")] [TestCase("Forward",false, TestName = "G4TierMasks_GPU_Forward_perspective")]
        [TestCase("NBCameraOpaqueDistortPass",true, TestName = "G4TierMasks_GPU_NBCameraOpaqueDistortPass_ortho")] [TestCase("NBCameraOpaqueDistortPass",false, TestName = "G4TierMasks_GPU_NBCameraOpaqueDistortPass_perspective")]
        [TestCase("NBDeferredDistortPass",true, TestName = "G4TierMasks_GPU_NBDeferredDistortPass_ortho")] [TestCase("NBDeferredDistortPass",false, TestName = "G4TierMasks_GPU_NBDeferredDistortPass_perspective")]
        public void G4TierMasks_GPU_ActualForwardAndScreenConsumersRestoreIntent(string stage,bool ortho)
        {
            const int size=128;
            bool screen=stage!="Forward";
            var pipeline=(UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            var data=pipeline.rendererDataList[0];
            string project=Path.GetDirectoryName(Application.dataPath);
            string rendererFile=Path.Combine(project,AssetDatabase.GetAssetPath(data));byte[] beforeRenderer=File.ReadAllBytes(rendererFile);
            string folder=Path.Combine(Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR")??Path.Combine(project,"Temp/NBFXTierMasks"),stage+(ortho?"-ortho":"-perspective"));
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
            var layer1=Constant(new Color(.5f,.5f,.5f,.5f));var layer2=Constant(new Color(.75f,.75f,.75f,.75f));var layer3=Constant(new Color(.5f,.5f,.5f,.5f));
            var scene=EditorSceneManager.NewPreviewScene();
            var foreground=GameObject.CreatePrimitive(PrimitiveType.Quad);var background=GameObject.CreatePrimitive(PrimitiveType.Quad);
            var cameraObject=new GameObject("Tier Mask group real Mesh camera");var camera=cameraObject.AddComponent<Camera>();
            var cameraData=cameraObject.AddComponent<UniversalAdditionalCameraData>();
            var backdrop=new Material(Shader.Find("Universal Render Pipeline/Unlit"));owned.Add(backdrop);
            var rt=new RenderTexture(size,size,24,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear);
            var read=new Texture2D(size,size,TextureFormat.RGBAHalf,false,true);
            var previous=RenderTexture.active;bool previousAsync=ShaderUtil.allowAsyncCompilation;
            var nb=data.rendererFeatures.FirstOrDefault(f=>f&&f.GetType().FullName=="NBShader.NBPostProcess");
            Assert.That(nb,Is.Not.Null);bool nbWasActive=nb.isActive;
            G4ScreenNoiseDirectedFeature directed=null;
            try
            {
                ShaderUtil.allowAsyncCompilation=false;
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
                    material.SetShaderPassEnabled("DepthNormalsOnly",false);
                    material.SetTexture("_MaskMap",layer1);material.SetTexture("_MaskMap2",layer2);material.SetTexture("_MaskMap3",layer3);
                    material.SetVector("_MaskMapVec",new Vector4(1,0,0,0));material.SetVector("_MaskRefineVec",new Vector4(1,1,0,0));
                    material.SetVector("_MaskMapOffsetAnition",Vector4.zero);material.SetVector("_MaskMap3OffsetAnition",Vector4.zero);
                    foreach(string property in new[]{"_MaskMap","_MaskMap2","_MaskMap3"}){material.SetTextureScale(property,Vector2.one);material.SetTextureOffset(property,Vector2.zero);}
                    for(int layer=0;layer<3;layer++){material.SetFloat(Toggles[layer],1f);if(!graph)material.EnableKeyword(MaskKeywords[layer]);}

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
                string[] policies={"full","none","full"};string[] labels={"intent","stripped","restored"};
                for(int state=0;state<3;state++)
                {
                    bool changed;Assert.That(ApplyMaskGroup(c,policies[state],out changed),Is.True);
                    foreach(var material in new[]{a,b})
                        for(int layer=0;layer<3;layer++)
                            if(c.GetFloat(Allows[layer])>.5f)material.EnableKeyword(MaskKeywords[layer]);else material.DisableKeyword(MaskKeywords[layer]);
                    frames[state]=new Color[3][];repeats[state]=new Color[3][];
                    var materials=new[]{a,b,c};
                    for(int m=0;m<3;m++)
                    {
                        renderer.sharedMaterial=materials[m];for(int i=0;i<3;i++)camera.Render();
                        frames[state][m]=Capture(((char)('A'+m))+"-"+labels[state]);repeats[state][m]=Capture(((char)('A'+m))+"-"+labels[state]+"-repeat");
                    }
                }
                var record=new MaskTierGPUMetrics{stage=stage,orthographic=ortho,api=SystemInfo.graphicsDeviceType.ToString(),unityVersion=Application.unityVersion,
                    scope="Explicit Mask1/2/3 derived gates; three states preserve intent/strip/restore; actual Forward or exact NB RT pass; full-frame strict zero/finite/visible/repeat/response; no complete Tier/Pass/Controller/Player claim",
                    finite=Finite(empty)&&frames.SelectMany(s=>s).All(Finite)&&repeats.SelectMany(s=>s).All(Finite),
                    abMax=Enumerable.Range(0,3).Select(s=>Delta(frames[s][0],frames[s][1])).ToArray(),
                    bcMax=Enumerable.Range(0,3).Select(s=>Delta(frames[s][1],frames[s][2])).ToArray(),
                    repeatMax=Enumerable.Range(0,3).Select(s=>Mathf.Max(Delta(frames[s][0],repeats[s][0]),Delta(frames[s][1],repeats[s][1]),Delta(frames[s][2],repeats[s][2]))).ToArray(),
                    aMaskResponse=Delta(frames[0][0],frames[1][0]),bMaskResponse=Delta(frames[0][1],frames[1][1]),cMaskResponse=Delta(frames[0][2],frames[1][2]),
                    aRestore=Delta(frames[0][0],frames[2][0]),bRestore=Delta(frames[0][1],frames[2][1]),cRestore=Delta(frames[0][2],frames[2][2]),
                    aVisible=Visible(frames[0][0],empty),bVisible=Visible(frames[0][1],empty),cVisible=Visible(frames[0][2],empty)};
                File.WriteAllText(Path.Combine(folder,"metrics.json"),JsonUtility.ToJson(record,true));Debug.Log("NBFX_TIER_MASKS_GPU "+JsonUtility.ToJson(record));
                beforeA.AssertSame(a,"Frozen complete intent restored");beforeB.AssertSame(b,"Current complete intent restored");beforeC.AssertSame(c,"Graph complete intent preserved");
                Assert.That(record.finite,Is.True);Assert.That(record.abMax.All(v=>v==0)&&record.bcMax.All(v=>v==0)&&record.repeatMax.All(v=>v==0),Is.True);
                Assert.That(record.aRestore+record.bRestore+record.cRestore,Is.Zero);
                Assert.That(record.aVisible,Is.GreaterThan(128));Assert.That(record.bVisible,Is.GreaterThan(128));Assert.That(record.cVisible,Is.GreaterThan(128));
                Assert.That(record.aMaskResponse,Is.GreaterThan(.001f));Assert.That(record.bMaskResponse,Is.GreaterThan(.001f));Assert.That(record.cMaskResponse,Is.GreaterThan(.001f));
            }
            finally
            {
                nb.SetActive(nbWasActive);if(directed){data.rendererFeatures.Remove(directed);data.SetDirty();Object.DestroyImmediate(directed);}
                ShaderUtil.allowAsyncCompilation=previousAsync;camera.targetTexture=null;RenderTexture.active=previous;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(read);EditorSceneManager.ClosePreviewScene(scene);
                Assert.That(File.ReadAllBytes(rendererFile),Is.EqualTo(beforeRenderer),"Mask Tier GPU fixture saved renderer asset");
            }
        }


    }
}
