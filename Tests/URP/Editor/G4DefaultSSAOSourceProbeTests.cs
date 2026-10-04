using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace NBFX.Baseline.Tests
{
    // Test-only extension of the existing VAT full-chain scene/capture core.
    // Observer never requests Depth/Normal, renders objects, or replaces final color.
    public sealed class G4DefaultSSAOSourceProbeTests
    {
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        readonly List<Object> owned = new List<Object>();
        DefaultSSAOBufferObserver observer;
        bool expectedDepthNormalsLayout = true;
        static Type FindType(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name, false)).First(t => t != null);
        static void Guard()
        {
            Assert.That(Path.GetFullPath(Application.dataPath), Is.EqualTo(Path.GetFullPath(
                @"D:\UnityProject\NBUnityProject\.utmp\NBFXMeshValidation-20261002\Assets")).IgnoreCase);
            for (int i = 0; i < SceneManager.sceneCount; ++i)
                Assert.That((SceneManager.GetSceneAt(i).name + "/" + SceneManager.GetSceneAt(i).path)
                    .IndexOf("TAI", StringComparison.OrdinalIgnoreCase), Is.LessThan(0));
        }
        [OneTimeSetUp] public void WarmExistingGraph() { Guard(); new G4GraphVATTests().ImportGraph(); }
        T Keep<T>(T value) where T : Object { owned.Add(value); return value; }
        static void ValidateDefaultState(Material m, bool graph, bool clip)
        {
            // This fixture renders a MeshRenderer: declare Native Mesh intent
            // before validation so VAT reads the same UV1 channel as Graph.
            if (!graph) m.SetFloat("_MeshSourceMode", 1);
            // The frozen shader has a test-only renamed identity, so the current
            // Native resolver intentionally does not recognize it. Resolve through
            // the real Native host, then apply that same state to the frozen code.
            if (!graph && m.shader.name == "Effects/NBShader_T00_Frozen")
            {
                var proxy = new Material(AssetDatabase.LoadAssetAtPath<Shader>(
                    "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader"));
                try
                {
                    Shader frozenShader = m.shader;
                    string before = EditorJsonUtility.ToJson(m);
                    CopySharedState(m, proxy);
                    string proxyInput = EditorJsonUtility.ToJson(proxy);
                    ValidateDefaultState(proxy, false, clip);
                    string proxyResolved = EditorJsonUtility.ToJson(proxy);
                    CopySharedState(proxy, m);
                    for (int i = 0; i < proxy.passCount; ++i)
                    {
                        string tag = proxy.shader.FindPassTagValue(i, new ShaderTagId("LightMode")).name;
                        string effective = string.IsNullOrEmpty(tag) ? "SRPDefaultUnlit" : tag;
                        m.SetShaderPassEnabled(effective, proxy.GetShaderPassEnabled(effective));
                    }
                    Assert.That(m.shader, Is.SameAs(frozenShader));
                    string folder = Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR");
                    if (!string.IsNullOrEmpty(folder))
                    {
                        var audit = new FrozenHostAudit {
                            frozenShaderPath = AssetDatabase.GetAssetPath(m.shader),
                            proxyShaderPath = AssetDatabase.GetAssetPath(proxy.shader),
                            frozenBefore = before, proxyInput = proxyInput, proxyResolved = proxyResolved,
                            frozenAfter = EditorJsonUtility.ToJson(m),
                            proxyKeywords = proxy.shaderKeywords, frozenKeywords = m.shaderKeywords,
                            nativeOnlyProperties = Enumerable.Range(0,proxy.shader.GetPropertyCount()).Select(proxy.shader.GetPropertyName).Where(n=>!m.HasProperty(n)).ToArray(),
                            nativeKeywordsMissingFromFrozen = proxy.shaderKeywords.Where(n=>!m.shader.keywordSpace.keywords.Any(k=>k.name==n)).ToArray()
                        };
                        File.WriteAllText(Path.Combine(folder,"frozen-host-validation-audit.json"),JsonUtility.ToJson(audit,true));
                    }
                }
                finally { Object.DestroyImmediate(proxy); }
                return;
            }
            if (graph)
            {
                m.SetFloat("_Surface", 0); m.SetFloat("_AlphaClip", clip ? 1 : 0);
                m.SetFloat("_ZWriteControl", 0); m.SetFloat("_CastShadows", 1);
                var gui = (ShaderGUI)Activator.CreateInstance(FindType("NBShaderEditor.NBShaderGraphGUI"));
                gui.ValidateMaterial(m);
            }
            else
            {
                // Exact current ModeBigBlockItem.TransparentMode: Opaque0/CutOff2.
                m.SetFloat("_TransparentMode", clip ? 2 : 0); m.SetFloat("_ForceZWriteToggle", 0);
                m.SetFloat("_AffectsShadows", 1);
                FindType("NBShaderEditor.NBShaderSyncService").GetMethod("SyncMaterialState", All, null,
                    new[] { typeof(Material) }, null).Invoke(null, new object[] { m });
            }
        }

        [Serializable] sealed class FrozenHostAudit
        {
            public string frozenShaderPath,proxyShaderPath,frozenBefore,proxyInput,proxyResolved,frozenAfter;
            public string[] proxyKeywords,frozenKeywords,nativeOnlyProperties,nativeKeywordsMissingFromFrozen;
        }
        static void CopySharedState(Material from, Material to)
        {
            // Bound every value write by matching declared shader property type.
            for (int i=0;i<from.shader.GetPropertyCount();++i)
            {
                string name=from.shader.GetPropertyName(i);int j=to.shader.FindPropertyIndex(name);
                if(j<0)continue;
                var type=from.shader.GetPropertyType(i);Assert.That(to.shader.GetPropertyType(j),Is.EqualTo(type),name);
                switch(type)
                {
                    case ShaderPropertyType.Color:to.SetColor(name,from.GetColor(name));break;
                    case ShaderPropertyType.Vector:to.SetVector(name,from.GetVector(name));break;
                    case ShaderPropertyType.Int:to.SetInteger(name,from.GetInteger(name));break;
                    case ShaderPropertyType.Texture:to.SetTexture(name,from.GetTexture(name));to.SetTextureScale(name,from.GetTextureScale(name));to.SetTextureOffset(name,from.GetTextureOffset(name));break;
                    default:to.SetFloat(name,from.GetFloat(name));break;
                }
            }
            to.shaderKeywords=from.shaderKeywords;to.renderQueue=from.renderQueue;
            to.SetOverrideTag("RenderType",from.GetTag("RenderType",false,""));
            to.doubleSidedGI=from.doubleSidedGI;to.globalIlluminationFlags=from.globalIlluminationFlags;to.enableInstancing=from.enableInstancing;
        }

        [TestCase("DepthNormals", true, false, TestName="DefaultSSAO_DepthNormals_SoftbodyOpaque_ortho")]
        [TestCase("Depth", true, false, TestName="DefaultSSAO_Depth_SoftbodyOpaque_ortho")]
        [TestCase("DepthNormals", false, false, TestName="DefaultSSAO_DepthNormals_SoftbodyOpaque_perspective")]
        [TestCase("Depth", false, false, TestName="DefaultSSAO_Depth_SoftbodyOpaque_perspective")]
        [TestCase("DepthNormals", true, true, TestName="DefaultSSAO_DepthNormals_SoftbodyClip_ortho")]
        [TestCase("Depth", true, true, TestName="DefaultSSAO_Depth_SoftbodyClip_ortho")]
        [TestCase("DepthNormals", false, true, TestName="DefaultSSAO_DepthNormals_SoftbodyClip_perspective")]
        [TestCase("Depth", false, true, TestName="DefaultSSAO_Depth_SoftbodyClip_perspective")]
        public void DefaultSourceAndExtraNormalsObservedBeforeOriginalAssertions(string source, bool ortho, bool clip)
        {
            Guard();
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            Assert.That(pipeline, Is.Not.Null);
            var data = pipeline.rendererDataList[0] as UniversalRendererData;
            Assert.That(data, Is.Not.Null); Assert.That(data.renderingMode.ToString(), Is.EqualTo("Forward"));
            var ssao = data.rendererFeatures.Single(f => f && f.GetType().FullName == "UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion");
            Assert.That(ssao.isActive, Is.True);
            object settings = ssao.GetType().GetField("m_Settings", All).GetValue(ssao);
            FieldInfo sourceField = settings.GetType().GetField("Source", All);
            object oldSource = sourceField.GetValue(settings);
            Assert.That((bool)settings.GetType().GetField("AfterOpaque", All).GetValue(settings), Is.False,
                "This minimal slice observes the existing before-opaque SSAO/Lit composition only.");
            string beforeSettings = EditorJsonUtility.ToJson(ssao);
            bool originalActive = ssao.isActive;
            var core = new G4GraphVATTests { ExpectedDefaultDepthNormalsOnly = expectedDepthNormalsLayout, GeometryCaseId = (expectedDepthNormalsLayout ? "ssao-source-" : "dn2-source-") + source + (clip ? "-clip" : "-opaque") };
            Texture2D mask = null;
            if (clip)
            {
                mask = Keep(new Texture2D(8,8,TextureFormat.RGBAHalf,false,true));
                for (int y=0;y<8;y++) for (int x=0;x<8;x++) mask.SetPixel(x,y,((x/2+y/2)&1)==0 ? new Color(.125f,.7f,.3f,.125f) : Color.white);
                mask.filterMode=FilterMode.Point; mask.Apply(false);
            }
            core.DefaultFullChainMaterialSetup = (m, graph) => {
                if (clip)
                {
                    m.SetTexture("_MaskMap",mask);m.SetFloat("_Mask_Toggle",1);m.SetVector("_MaskMapVec",new Vector4(1,0,0,0));m.SetFloat("_Cutoff",.5f);
                    if (!graph) m.EnableKeyword("_MASKMAP_ON");
                }
                ValidateDefaultState(m,graph,clip);
                string folder=Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR");
                if(!string.IsNullOrEmpty(folder))File.WriteAllText(Path.Combine(folder,graph?"graph-validated-material.json":m.shader.name=="Effects/NBShader_T00_Frozen"?"frozen-validated-material.json":"native-validated-material.json"),EditorJsonUtility.ToJson(m,true));
            };
            core.DefaultFullChainLightSetup = light => light.shadowNormalBias = 0;
            core.DefaultFullChainSceneSetup = (camera,actor,receiver,target) => {
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(
                    "Packages/com.xuanxuan.nb.fx/Tests/URP/Shaders/DefaultSSAOReadback.shader");
                Assert.That(shader && shader.isSupported,Is.True);
                var copy = Keep(new Material(shader));
                Assert.That(copy.shader && copy.shader.isSupported,Is.True);
                observer = Keep(ScriptableObject.CreateInstance<DefaultSSAOBufferObserver>());
                observer.targetCamera=camera;observer.copyMaterial=copy;observer.Initialize(target.width,target.height);observer.Create();
                data.rendererFeatures.Add(observer);data.SetDirty();
            };
            core.DefaultFullChainPreAssertionsObserve = (materials,actor,receiver,camera,target,read,folder) => {
                // Snapshot the unchanged material state immediately after the
                // original VAT-on/frame1 actor repeats, before source controls.
                for(int i=0;i<materials.Length;++i)
                    File.WriteAllText(Path.Combine(folder,"actor-VAT-inputs-"+"ABC"[i]+".json"),JsonUtility.ToJson(ProbeVAT(materials[i],actor,receiver),true));
                // Raw observations are written before original parity/strong-response
                // assertions. Those assertions remain unchanged and still determine XML.
                var rows = new List<Observation>();
                var bufferReadback=Keep(new Texture2D(target.width,target.height,TextureFormat.RGBAFloat,false,true));
                Color[] Capture(Material m,string label)
                {
                    actor.sharedMaterial=m;
                    for(int i=0;i<3;i++)camera.Render();
                    observer.ResetFrameEvidence(); camera.Render();
                    var shaderErrors=ShaderUtil.GetShaderMessages(observer.copyMaterial.shader).Where(e=>e.severity.ToString()=="Error").Select(e=>e.message).ToArray();
                    File.WriteAllLines(Path.Combine(folder,label+"-readback-shader-errors.txt"),shaderErrors);
                    Assert.That(shaderErrors,Is.Empty,"Readback shader must compile on the actual rendered path.");
                    var frame=Read(target,read,Path.Combine(folder,label+"-final.rgba32f"));
                    var row=new Observation {label=label,source=source,alphaClip=clip,graphExtraNormalsEnabled=m.GetShaderPassEnabled("DepthNormalsOnly"),
                        depthNormalsPassIndex=m.FindPass("DepthNormalsOnly"),
                        depthCopied=observer.depthCopied,normalsCopied=observer.normalsCopied,aoCopied=observer.aoCopied,
                        depthOnly=m.GetShaderPassEnabled("DepthOnly"),shadowCaster=m.GetShaderPassEnabled("ShadowCaster"),
                        universalForward=m.GetShaderPassEnabled("UniversalForward"),srpDefaultUnlit=m.GetShaderPassEnabled("SRPDefaultUnlit"),
                        queue=m.renderQueue,keywords=m.shaderKeywords};
                    if(ssao.isActive)Assert.That(row.depthCopied,Is.True,"Observer must see the actual requested URP depth handle.");
                    if(ssao.isActive)Assert.That(row.aoCopied,Is.True,"Active SSAO must produce a valid frame resource, not merely an active feature flag.");
                    if(ssao.isActive&&source=="DepthNormals")Assert.That(row.normalsCopied,Is.True);
                    if(row.depthCopied)Read(observer.depth,bufferReadback,Path.Combine(folder,label+"-depth.rgba32f"));
                    if(row.normalsCopied)Read(observer.normals,bufferReadback,Path.Combine(folder,label+"-normals.rgba32f"));
                    if(row.aoCopied)Read(observer.ao,bufferReadback,Path.Combine(folder,label+"-ao.rgba32f"));
                    rows.Add(row);return frame;
                }
                var a=Capture(materials[0],"source-A-raw");var b=Capture(materials[1],"source-B-raw");var c=Capture(materials[2],"source-C-raw");
                bool originalDN=materials[2].GetShaderPassEnabled("DepthNormalsOnly");
                Color[] noNormals;
                try {materials[2].SetShaderPassEnabled("DepthNormalsOnly",false);noNormals=Capture(materials[2],"source-C-extraDN0-off-diagnostic");}
                finally {materials[2].SetShaderPassEnabled("DepthNormalsOnly",originalDN);}
                var repeat=Capture(materials[2],"source-C-raw-repeat");
                Color[] bForceOff,cForceOff;
                try
                {
                    materials[1].SetFloat("_ForceZWriteToggle",2);
                    FindType("NBShaderEditor.NBShaderSyncService").GetMethod("SyncMaterialState",All,null,new[]{typeof(Material)},null).Invoke(null,new object[]{materials[1]});
                    materials[2].SetFloat("_ZWriteControl",2);
                    ((ShaderGUI)Activator.CreateInstance(FindType("NBShaderEditor.NBShaderGraphGUI"))).ValidateMaterial(materials[2]);
                    Assert.That(materials[1].GetShaderPassEnabled("DepthOnly"),Is.False);
                    Assert.That(materials[2].GetShaderPassEnabled("DepthOnly"),Is.False);
                    bForceOff=Capture(materials[1],"source-B-validated-depthForceOff");
                    cForceOff=Capture(materials[2],"source-C-validated-depthForceOff");
                }
                finally {ValidateDefaultState(materials[1],false,clip);ValidateDefaultState(materials[2],true,clip);}
                // A standard URP Lit contact object proves actual SSAO response.
                var lit=Keep(new Material(Shader.Find("Universal Render Pipeline/Lit")));lit.SetColor("_BaseColor",new Color(.3f,.3f,.3f,1));
                var control=Keep(GameObject.CreatePrimitive(PrimitiveType.Cube));SceneManager.MoveGameObjectToScene(control,camera.scene);
                control.layer=2;control.transform.position=new Vector3(.85f,.25f,0);control.transform.localScale=Vector3.one*.5f;
                control.GetComponent<MeshRenderer>().sharedMaterial=lit;
                bool actorWasEnabled=actor.enabled;actor.enabled=false;
                Color[] litOn,litOff;
                try {ssao.SetActive(true);litOn=Capture(materials[1],"independent-Lit-ssao-on");ssao.SetActive(false);litOff=Capture(materials[1],"independent-Lit-ssao-off-diagnostic");}
                finally {ssao.SetActive(originalActive);actor.enabled=actorWasEnabled;control.SetActive(false);}
                var result=new Result {scope="Current raw default material validation + actual URP source/extraDN0 controls; matched opaque/CutOff SoftBody only; NBPost temporarily excluded by original core; no all-surface/Player/perf claim",
                    source=source,orthographic=ortho,clip=clip,ab=Delta(a,b),bc=Delta(b,c),bVsNoExtraDN=Delta(b,noNormals),extraDNFinalResponse=Delta(c,noNormals),
                    validatedDepthForceOffBC=Delta(bForceOff,cForceOff),bDefaultDepthForceOffResponse=Delta(b,bForceOff),cDefaultDepthForceOffResponse=Delta(c,cForceOff),
                    repeat=Delta(c,repeat),independentLitSSAOResponse=Delta(litOn,litOff),observations=rows.ToArray()};
                File.WriteAllText(Path.Combine(folder,"actual-ssao-source-and-extraDN0.json"),JsonUtility.ToJson(result,true));
                Assert.That(result.repeat,Is.Zero);Assert.That(result.ab,Is.Zero);
                Assert.That(result.independentLitSSAOResponse,Is.GreaterThan(.001f),"SSAO active flag alone is not evidence of final composition.");
                if(source=="Depth"&&!rows.First(row=>row.label=="source-C-raw").normalsCopied)
                    Assert.That(result.extraDNFinalResponse,Is.Zero,"With no actual normals resource, an unused extra normal pass must not affect the Depth-source composition.");
            };
            try {sourceField.SetValue(settings,Enum.Parse(sourceField.FieldType,source));core.CaptureDefaultForwardDepthShadow(ortho);}
            finally
            {
                sourceField.SetValue(settings,oldSource);ssao.SetActive(originalActive);
                if(observer){data.rendererFeatures.Remove(observer);data.SetDirty();observer.ReleaseTargets();}
                foreach(var item in owned.AsEnumerable().Reverse())if(item)Object.DestroyImmediate(item);owned.Clear();observer=null;
                Assert.That(EditorJsonUtility.ToJson(ssao),Is.EqualTo(beforeSettings),"Temporary source/on-off control changed serialized SSAO settings.");
            }
        }
        static IEnumerable<TestCaseData> CompiledDepthNormalsOffCases()
        {
            foreach(string source in new[]{"DepthNormals","Depth"})foreach(bool ortho in new[]{true,false})foreach(bool clip in new[]{false,true})
                yield return new TestCaseData(source,ortho,clip).SetName("DefaultSSAOV2_NoDN0_"+source+"_Softbody"+(clip?"Clip":"Opaque")+(ortho?"_ortho":"_perspective"));
        }
        [TestCaseSource(nameof(CompiledDepthNormalsOffCases))]
        public void CompiledDepthNormalsOffDefaultPipelineV2(string source,bool ortho,bool clip)
        {
            bool previous=expectedDepthNormalsLayout;expectedDepthNormalsLayout=false;
            try{DefaultSourceAndExtraNormalsObservedBeforeOriginalAssertions(source,ortho,clip);}
            finally{expectedDepthNormalsLayout=previous;}
        }
        [TestCase(true,TestName="DefaultSSAONearLitV2_NoDN0_DepthNormals_SoftbodyOpaque_ortho")]
        [TestCase(false,TestName="DefaultSSAONearLitV2_NoDN0_DepthNormals_SoftbodyOpaque_perspective")]
        public void CompiledDepthNormalsOffNearReceiverV2(bool ortho)
        {
            bool previous=expectedDepthNormalsLayout;expectedDepthNormalsLayout=false;
            try{NearLitReceiverWithActorEnabled(ortho);}
            finally{expectedDepthNormalsLayout=previous;}
        }

        [Serializable] sealed class NearLitRow
        {
            public string label,shader;public bool actorEnabled,receiverEnabled,ssaoActive,extraDN0,depthOnly,shadowCaster,depthCopied,normalsCopied,aoCopied;
            public int receiverPixels,actorPixels;public float nearReceiverResponse,controlReceiverResponse;
        }
        [Serializable] sealed class NearLitResult
        {
            public string scope,source,geometryBasis,settingsBefore,lightType,ambientMode;
            public bool orthographic,measurementsComplete;public float radius,directLightingStrength,intensity,gap,actorMinY,actorMaxY,receiverY;
            public Vector3[] actorWorldA,actorWorldB,actorWorldC;public Vector3 receiverPosition,cubePosition,cubeScale,cameraPosition,lightDirection;
            public Color receiverColor,lightColor,ambientColor;public float lightIntensity,shadowStrength,shadowBias,shadowNormalBias;
            public int nearReceiverPixels,controlReceiverPixels;public float ab,bc,abNearReceiver,bcNearReceiver,abControlReceiver,bcControlReceiver,extraDNFinal,extraDNNearReceiver,extraDNControlReceiver;
            public float[] repeat,ssaoResponse,nearReceiverSSAOResponse,controlReceiverSSAOResponse;
            public NearLitRow[] observations;
        }
        static float NearFrac(float v)=>v-Mathf.Floor(v);
        static Vector3[] NearActorWorldVertices(Material material,MeshRenderer actor)
        {
            // Restricted source evaluation of the existing SoftBody kernel on the
            // actual point-sampled RGBAHalf input. This is not a GPU vertex dump.
            foreach(string name in new[]{"_VAT_Toggle","_frameCount","_displayFrame"})Assert.That(material.HasProperty(name),Is.True,name);
            Assert.That(material.GetFloat("_VAT_Toggle"),Is.EqualTo(1));Assert.That(material.GetFloat("_VATMode"),Is.Zero);Assert.That(material.GetFloat("_HoudiniVATSubMode"),Is.Zero);
            Assert.That(material.GetFloat("_B_autoPlayback"),Is.Zero);Assert.That(material.GetFloat("_displayFrame"),Is.EqualTo(1));Assert.That(material.GetFloat("_frameCount"),Is.EqualTo(2));Assert.That(material.GetFloat("_B_LOAD_POS_TWO_TEX"),Is.Zero);
            var texture=material.GetTexture("_posTexture") as Texture2D;Assert.That(texture&&texture.isReadable,Is.True);Assert.That(texture.format,Is.EqualTo(TextureFormat.RGBAHalf));Assert.That(texture.filterMode,Is.EqualTo(FilterMode.Point));Assert.That(texture.wrapMode,Is.EqualTo(TextureWrapMode.Clamp));
            var mesh=actor.GetComponent<MeshFilter>().sharedMesh;var vertices=mesh.vertices;var uv1=mesh.uv2;Assert.That(uv1.Length,Is.EqualTo(vertices.Length));
            var minimum=new Vector3(material.GetFloat("_boundMinX"),material.GetFloat("_boundMinY"),material.GetFloat("_boundMinZ"));var maximum=new Vector3(material.GetFloat("_boundMaxX"),material.GetFloat("_boundMaxY"),material.GetFloat("_boundMaxZ"));
            float oneMinusMaxR=1-NearFrac(maximum.x*-10),oneMinusMinB=1-(Mathf.Ceil(minimum.z*10)-minimum.z*10);bool raw=NearFrac(maximum.z*10)>=.5f;
            var result=new Vector3[vertices.Length];
            for(int i=0;i<vertices.Length;++i)
            {
                // selectedFrame=1, totalFrames=2: wrapped frame offset is zero.
                var uv=new Vector2(uv1[i].x*oneMinusMinB,1-(1-uv1[i].y)*oneMinusMaxR);
                var px=texture.GetPixel(Mathf.Clamp(Mathf.FloorToInt(uv.x*texture.width),0,texture.width-1),Mathf.Clamp(Mathf.FloorToInt(uv.y*texture.height),0,texture.height-1));
                var rgb=new Vector3(px.r,px.g,px.b);var displacement=raw?rgb:Vector3.Scale(rgb,maximum-minimum)+minimum;
                result[i]=actor.transform.TransformPoint(vertices[i]+displacement);
            }
            return result;
        }
        static Color[] NearReadSaved(string file,int pixels)
        {
            Assert.That(new FileInfo(file).Length,Is.EqualTo((long)pixels*16));var values=new Color[pixels];
            using(var read=new BinaryReader(File.OpenRead(file)))for(int i=0;i<pixels;++i)values[i]=new Color(read.ReadSingle(),read.ReadSingle(),read.ReadSingle(),read.ReadSingle());
            NearHealthy(values);return values;
        }
        static void NearHealthy(Color[] values)
        {
            Assert.That(values.Length,Is.GreaterThan(0));float cd=BitConverter.ToSingle(new byte[]{0xcd,0xcd,0xcd,0xcd},0);
            Assert.That(values.All(p=>Enumerable.Range(0,4).All(i=>!float.IsNaN(p[i])&&!float.IsInfinity(p[i])&&p[i]!=cd)),Is.True,"Near interaction readback must be finite and free of CD fill values.");
        }
        static float NearPixelDelta(Color a,Color b)=>Mathf.Max(Mathf.Abs(a.r-b.r),Mathf.Abs(a.g-b.g),Mathf.Abs(a.b-b.b),Mathf.Abs(a.a-b.a));
        static float NearRegionDelta(Color[] a,Color[] b,bool[] region)
        {float d=0;for(int i=0;i<a.Length;++i)if(region[i])d=Mathf.Max(d,NearPixelDelta(a[i],b[i]));return d;}

        [TestCase(true,TestName="DefaultSSAONearLit_DepthNormals_SoftbodyOpaque_ortho")]
        [TestCase(false,TestName="DefaultSSAONearLit_DepthNormals_SoftbodyOpaque_perspective")]
        public void NearLitReceiverWithActorEnabled(bool ortho)
        {
            Guard();var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;Assert.That(pipeline,Is.Not.Null);
            var data=pipeline.rendererDataList[0] as UniversalRendererData;Assert.That(data,Is.Not.Null);Assert.That(data.renderingMode.ToString(),Is.EqualTo("Forward"));
            var ssao=data.rendererFeatures.Single(f=>f&&f.GetType().FullName=="UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion");Assert.That(ssao.isActive,Is.True);
            object settings=ssao.GetType().GetField("m_Settings",All).GetValue(ssao);var sourceField=settings.GetType().GetField("Source",All);object oldSource=sourceField.GetValue(settings);bool oldActive=ssao.isActive;string oldSettings=EditorJsonUtility.ToJson(ssao);
            Assert.That((bool)settings.GetType().GetField("AfterOpaque",All).GetValue(settings),Is.False);
            float radius=(float)settings.GetType().GetField("Radius",All).GetValue(settings);Assert.That(radius,Is.GreaterThan(0));
            var core=new G4GraphVATTests{ExpectedDefaultDepthNormalsOnly=expectedDepthNormalsLayout,GeometryCaseId=expectedDepthNormalsLayout?"nr":"nr2"};
            core.DefaultFullChainMaterialSetup=(m,graph)=>ValidateDefaultState(m,graph,false);
            core.DefaultFullChainLightSetup=light=>light.shadowNormalBias=0;
            core.DefaultFullChainSceneSetup=(camera,actor,receiver,target)=>{
                var shader=AssetDatabase.LoadAssetAtPath<Shader>("Packages/com.xuanxuan.nb.fx/Tests/URP/Shaders/DefaultSSAOReadback.shader");Assert.That(shader&&shader.isSupported,Is.True);
                observer=Keep(ScriptableObject.CreateInstance<DefaultSSAOBufferObserver>());observer.targetCamera=camera;observer.copyMaterial=Keep(new Material(shader));observer.Initialize(target.width,target.height);observer.Create();data.rendererFeatures.Add(observer);data.SetDirty();
            };
            core.DefaultFullChainPreAssertionsObserve=(materials,actor,receiver,camera,target,read,folder)=>{
                // Original core actor-only frames already exist. No actor disable
                // occurs in this new probe; controls share the identical layout.
                Assert.That(actor.enabled&&receiver.enabled,Is.True);
                var actorOnly=new Color[3][];var empty=NearReadSaved(Path.Combine(folder,"empty-background.rgba32f"),target.width*target.height);
                for(int i=0;i<3;++i){actorOnly[i]=NearReadSaved(Path.Combine(folder,"ABC"[i]+"-forward-visible.rgba32f"),empty.Length);var input=ProbeVAT(materials[i],actor,receiver);Assert.That(input.particleBit,Is.False);File.WriteAllText(Path.Combine(folder,"n-input-"+"ABC"[i]+".json"),JsonUtility.ToJson(input,true));}
                var worlds=materials.Select(m=>NearActorWorldVertices(m,actor)).ToArray();
                for(int i=0;i<worlds[0].Length;++i){Assert.That(worlds[1][i],Is.EqualTo(worlds[0][i]));Assert.That(worlds[2][i],Is.EqualTo(worlds[0][i]));}
                float minY=worlds[0].Min(v=>v.y),maxY=worlds[0].Max(v=>v.y),gap=Mathf.Min(.02f,radius*.08f);Assert.That(gap,Is.GreaterThan(0).And.LessThan(radius));
                var receiverMaterial=receiver.sharedMaterial;Assert.That(receiverMaterial.shader.name,Is.EqualTo("Universal Render Pipeline/Lit"));Assert.That(Vector3.Dot(receiver.transform.up,Vector3.up),Is.GreaterThan(.9999f));
                var oldPosition=receiver.transform.position;var oldColor=receiverMaterial.GetColor("_BaseColor");var oldActorMaterial=actor.sharedMaterial;bool oldExtra=materials[2].GetShaderPassEnabled("DepthNormalsOnly");
                GameObject cube=null;
                try
                {
                    receiver.transform.position=new Vector3(oldPosition.x,minY-gap,oldPosition.z);receiverMaterial.SetColor("_BaseColor",new Color(.3f,.3f,.3f,1));
                    float minX=worlds[0].Min(v=>v.x),maxX=worlds[0].Max(v=>v.x),minZ=worlds[0].Min(v=>v.z),maxZ=worlds[0].Max(v=>v.z);
                    cube=Keep(GameObject.CreatePrimitive(PrimitiveType.Cube));SceneManager.MoveGameObjectToScene(cube,camera.scene);cube.layer=actor.gameObject.layer;cube.transform.localScale=Vector3.one*.4f;cube.transform.position=new Vector3(maxX+radius*2.5f+.2f,receiver.transform.position.y+.2f,(minZ+maxZ)*.5f);
                    var cubeRenderer=cube.GetComponent<MeshRenderer>();cubeRenderer.sharedMaterial=receiverMaterial;cubeRenderer.shadowCastingMode=ShadowCastingMode.Off;
                    Assert.That(cubeRenderer.bounds.max.x,Is.LessThan(receiver.bounds.max.x));Assert.That(cubeRenderer.bounds.min.x-radius,Is.GreaterThan(maxX+radius),"Independent control and NB receiver ROI must be separate.");
                    var cubeBounds=cubeRenderer.bounds;var projected=new List<Vector3>();for(int x=0;x<2;++x)for(int y=0;y<2;++y)for(int z=0;z<2;++z)projected.Add(camera.WorldToViewportPoint(new Vector3(x==0?cubeBounds.min.x:cubeBounds.max.x,y==0?cubeBounds.min.y:cubeBounds.max.y,z==0?cubeBounds.min.z:cubeBounds.max.z)));
                    float cubeU0=projected.Min(v=>v.x),cubeU1=projected.Max(v=>v.x),cubeV0=projected.Min(v=>v.y),cubeV1=projected.Max(v=>v.y);
                    var nearRegion=new bool[empty.Length];var controlRegion=new bool[empty.Length];var actorMasks=new bool[3][];
                    for(int k=0;k<3;++k)actorMasks[k]=actorOnly[k].Select((p,i)=>Mathf.Abs(p.r-empty[i].r)+Mathf.Abs(p.g-empty[i].g)+Mathf.Abs(p.b-empty[i].b)>.07f).ToArray();
                    for(int y=0;y<target.height;++y)for(int x=0;x<target.width;++x)
                    {
                        int i=y*target.width+x;float u=(x+.5f)/target.width,v=(y+.5f)/target.height;var ray=camera.ViewportPointToRay(new Vector3(u,v,0));if(Mathf.Abs(ray.direction.y)<1e-6f)continue;float distance=(receiver.transform.position.y-ray.origin.y)/ray.direction.y;if(distance<=0)continue;var at=ray.GetPoint(distance);
                        bool actorPixel=actorMasks.Any(mask=>mask[i]),cubePixel=u>=cubeU0&&u<=cubeU1&&v>=cubeV0&&v<=cubeV1;
                        bool floor=at.x>=receiver.bounds.min.x&&at.x<=receiver.bounds.max.x&&at.z>=receiver.bounds.min.z&&at.z<=receiver.bounds.max.z;
                        if(!floor||actorPixel||cubePixel)continue;
                        nearRegion[i]=at.x>=minX-radius&&at.x<=maxX+radius&&at.z>=minZ-radius&&at.z<=maxZ+radius;
                        controlRegion[i]=at.x>=cubeBounds.min.x-radius&&at.x<=cubeBounds.max.x+radius&&at.z>=cubeBounds.min.z-radius&&at.z<=cubeBounds.max.z+radius;
                    }
                    File.WriteAllBytes(Path.Combine(folder,"near-roi.bin"),nearRegion.Select(v=>(byte)(v?1:0)).ToArray());File.WriteAllBytes(Path.Combine(folder,"ctl-roi.bin"),controlRegion.Select(v=>(byte)(v?1:0)).ToArray());
                    var light=RenderSettings.sun;Assert.That(light,Is.Not.Null);
                    var result=new NearLitResult{scope="DepthNormals opaque SoftBody frame1 only; NB actor always enabled with close Lit receiver and fixed separate Lit cube. Cube receiver ROI is only an independent SSAO chain control; NB receiver interaction has its own ROI and verdict.",source="DepthNormals",geometryBasis="Source-evaluated actual point-sampled VAT inputs via HVAT_VatUV / HVAT_ApplySoftBodySamplesV1; not renderer.bounds and not an independent GPU vertex dump.",orthographic=ortho,radius=radius,directLightingStrength=(float)settings.GetType().GetField("DirectLightingStrength",All).GetValue(settings),intensity=(float)settings.GetType().GetField("Intensity",All).GetValue(settings),settingsBefore=oldSettings,gap=gap,actorMinY=minY,actorMaxY=maxY,receiverY=receiver.transform.position.y,actorWorldA=worlds[0],actorWorldB=worlds[1],actorWorldC=worlds[2],receiverPosition=receiver.transform.position,receiverColor=receiverMaterial.GetColor("_BaseColor"),cubePosition=cube.transform.position,cubeScale=cube.transform.localScale,cameraPosition=camera.transform.position,lightType=light.type.ToString(),lightDirection=light.transform.forward,lightColor=light.color,lightIntensity=light.intensity,shadowStrength=light.shadowStrength,shadowBias=light.shadowBias,shadowNormalBias=light.shadowNormalBias,ambientMode=RenderSettings.ambientMode.ToString(),ambientColor=RenderSettings.ambientLight};
                    var rows=new List<NearLitRow>();var bufferRead=Keep(new Texture2D(target.width,target.height,TextureFormat.RGBAFloat,false,true));
                    void Save(){result.observations=rows.ToArray();File.WriteAllText(Path.Combine(folder,"near.json"),JsonUtility.ToJson(result,true));}
                    Color[] Capture(int which,string label)
                    {
                        Assert.That(actor.enabled&&receiver.enabled&&cube.activeInHierarchy,Is.True);actor.sharedMaterial=materials[which];for(int i=0;i<3;++i)camera.Render();observer.ResetFrameEvidence();camera.Render();
                        var errors=new[]{observer.copyMaterial.shader,materials[which].shader,receiverMaterial.shader}.SelectMany(ShaderUtil.GetShaderMessages).Where(e=>e.severity.ToString()=="Error").Select(e=>e.message).ToArray();File.WriteAllLines(Path.Combine(folder,label+"-err.txt"),errors);Assert.That(errors,Is.Empty);
                        var frame=Read(target,read,Path.Combine(folder,label+"-f.rgba32f"));NearHealthy(frame);
                        var row=new NearLitRow{label=label,shader=materials[which].shader.name,actorEnabled=actor.enabled,receiverEnabled=receiver.enabled,ssaoActive=ssao.isActive,extraDN0=materials[which].GetShaderPassEnabled("DepthNormalsOnly"),depthOnly=materials[which].GetShaderPassEnabled("DepthOnly"),shadowCaster=materials[which].GetShaderPassEnabled("ShadowCaster"),depthCopied=observer.depthCopied,normalsCopied=observer.normalsCopied,aoCopied=observer.aoCopied};rows.Add(row);Save();
                        if(ssao.isActive){Assert.That(row.depthCopied&&row.normalsCopied&&row.aoCopied,Is.True,"Active DepthNormals must expose current depth, normals and AO resources.");}
                        if(row.depthCopied)NearHealthy(Read(observer.depth,bufferRead,Path.Combine(folder,label+"-d.rgba32f")));if(row.normalsCopied)NearHealthy(Read(observer.normals,bufferRead,Path.Combine(folder,label+"-n.rgba32f")));if(row.aoCopied)NearHealthy(Read(observer.ao,bufferRead,Path.Combine(folder,label+"-a.rgba32f")));
                        row.actorPixels=Enumerable.Range(0,frame.Length).Count(i=>actorMasks[which][i]&&NearPixelDelta(frame[i],actorOnly[which][i])<.0001f);
                        row.receiverPixels=Enumerable.Range(0,frame.Length).Count(i=>nearRegion[i]&&NearPixelDelta(frame[i],actorOnly[which][i])>.01f);Save();return frame;
                    }
                    var on=new Color[3][];var repeat=new Color[3][];var off=new Color[3][];
                    for(int i=0;i<3;++i)on[i]=Capture(i,"ABC"[i]+"on");
                    materials[2].SetShaderPassEnabled("DepthNormalsOnly",false);var noDN=Capture(2,"Cd0");materials[2].SetShaderPassEnabled("DepthNormalsOnly",oldExtra);
                    for(int i=0;i<3;++i)repeat[i]=Capture(i,"ABC"[i]+"r");
                    ssao.SetActive(false);for(int i=0;i<3;++i)off[i]=Capture(i,"ABC"[i]+"off");ssao.SetActive(true);var finalRepeat=Capture(2,"Cback");
                    result.ab=Delta(on[0],on[1]);result.bc=Delta(on[1],on[2]);result.extraDNFinal=Delta(on[2],noDN);result.extraDNNearReceiver=NearRegionDelta(on[2],noDN,nearRegion);result.extraDNControlReceiver=NearRegionDelta(on[2],noDN,controlRegion);result.repeat=Enumerable.Range(0,3).Select(i=>Delta(on[i],repeat[i])).Concat(new[]{Delta(on[2],finalRepeat)}).ToArray();result.ssaoResponse=Enumerable.Range(0,3).Select(i=>Delta(on[i],off[i])).ToArray();result.nearReceiverSSAOResponse=Enumerable.Range(0,3).Select(i=>NearRegionDelta(on[i],off[i],nearRegion)).ToArray();result.controlReceiverSSAOResponse=Enumerable.Range(0,3).Select(i=>NearRegionDelta(on[i],off[i],controlRegion)).ToArray();result.nearReceiverPixels=nearRegion.Count(v=>v);result.controlReceiverPixels=controlRegion.Count(v=>v);Save();
                    result.abNearReceiver=NearRegionDelta(on[0],on[1],nearRegion);result.bcNearReceiver=NearRegionDelta(on[1],on[2],nearRegion);result.abControlReceiver=NearRegionDelta(on[0],on[1],controlRegion);result.bcControlReceiver=NearRegionDelta(on[1],on[2],controlRegion);
                    result.measurementsComplete=true;Save();
                    foreach(var row in rows){Assert.That(row.actorPixels,Is.GreaterThan(150),row.label+" must retain visible NB actor");Assert.That(row.receiverPixels,Is.GreaterThan(8),row.label+" must retain visible near Lit receiver");}
                    Assert.That(result.nearReceiverPixels,Is.GreaterThan(8));Assert.That(result.controlReceiverPixels,Is.GreaterThan(8));foreach(float response in result.controlReceiverSSAOResponse)Assert.That(response,Is.GreaterThan(.001f),"Same-layout independent cube/receiver SSAO control must respond.");foreach(float difference in result.repeat)Assert.That(difference,Is.Zero);
                    // A/B near-receiver on/off can be zero because Native has no
                    // DN0. Record that fact; the separate cube ROI is not its proof.
                    Assert.That(result.ab,Is.Zero);Assert.That(result.bc,Is.Zero,"Distinct actor-on near-receiver default-pass interaction; retain raw failure, do not suppress the extra pass or relax parity.");
                }
                finally{ssao.SetActive(oldActive);materials[2].SetShaderPassEnabled("DepthNormalsOnly",oldExtra);actor.sharedMaterial=oldActorMaterial;receiver.transform.position=oldPosition;receiverMaterial.SetColor("_BaseColor",oldColor);if(cube)cube.SetActive(false);}
            };
            try{sourceField.SetValue(settings,Enum.Parse(sourceField.FieldType,"DepthNormals"));core.CaptureDefaultForwardDepthShadow(ortho);}
            finally
            {
                sourceField.SetValue(settings,oldSource);ssao.SetActive(oldActive);if(observer){data.rendererFeatures.Remove(observer);data.SetDirty();observer.ReleaseTargets();}foreach(var item in owned.AsEnumerable().Reverse())if(item)Object.DestroyImmediate(item);owned.Clear();observer=null;
                Assert.That(EditorJsonUtility.ToJson(ssao),Is.EqualTo(oldSettings),"Near probe changed serialized SSAO settings.");
            }
        }

        [Serializable] sealed class ScalarProbe { public string name;public bool present;public float value; }
        [Serializable] sealed class TextureProbe { public string name,format,pixelSHA256,filter,wrap;public int instance,width,height; }
        [Serializable] sealed class PassProbe { public string displayName,lightMode;public bool enabled; }
        [Serializable] sealed class VATProbe
        {
            public string shader,shaderPath,shaderSHA256,serializedMaterial;
            public string[] keywords,shaderErrors;public ScalarProbe[] values;public TextureProbe[] textures;public PassProbe[] passes;
            public int flags0,flags1,customDataFlag2;public bool particleBit,nativeVATKeyword,nativeHoudiniKeyword,nativeSoftbodyKeyword;
            public Vector3[] vertices,normals;public Vector4[] uv0,uv1;public Matrix4x4 localToWorld;public bool receiverEnabled;
        }
        static VATProbe ProbeVAT(Material m,MeshRenderer actor,MeshRenderer receiver)
        {
            var names=new[]{"_VAT_Toggle","_VATMode","_HoudiniVATSubMode","_MeshSourceMode","_NBShaderFeatureTier","_CustomData","_displayFrame","_frameCount","_B_autoPlayback","_gameTimeAtFirstFrame","_houdiniFPS","_playbackSpeed","_B_LOAD_POS_TWO_TEX","_B_UNLOAD_ROT_TEX","_boundMinX","_boundMinY","_boundMinZ","_boundMaxX","_boundMaxY","_boundMaxZ","_NB_Flags1Lo16","_NB_Flags1Hi16","_NB_CustomDataFlag2Lo16","_NB_CustomDataFlag2Hi16","_B_interpolate","_animateFirstFrame","_Surface","_ZWriteControl","_ForceZWriteToggle"};
            var values=names.Select(n=>new ScalarProbe{name=n,present=m.HasProperty(n),value=m.HasProperty(n)?m.GetFloat(n):0}).ToArray();
            var textures=new List<TextureProbe>();
            foreach(string n in new[]{"_BaseMap","_posTexture","_posTexture2","_rotTexture","_colTexture","_lookupTable"})
            {
                var t=m.GetTexture(n);var row=new TextureProbe{name=n,instance=t?t.GetInstanceID():0,width=t?t.width:0,height=t?t.height:0,filter=t?t.filterMode.ToString():"",wrap=t?t.wrapMode.ToString():""};
                if(t is Texture2D tex){row.format=tex.format.ToString();if(tex.isReadable){using(var stream=new MemoryStream()){using(var writer=new BinaryWriter(stream,System.Text.Encoding.UTF8,true))foreach(var px in tex.GetPixels()){writer.Write(px.r);writer.Write(px.g);writer.Write(px.b);writer.Write(px.a);}using(var hash=System.Security.Cryptography.SHA256.Create())row.pixelSHA256=BitConverter.ToString(hash.ComputeHash(stream.ToArray())).Replace("-","").ToLowerInvariant();}}}
                textures.Add(row);
            }
            var mesh=actor.GetComponent<MeshFilter>().sharedMesh;var uv0=new List<Vector4>();var uv1=new List<Vector4>();mesh.GetUVs(0,uv0);mesh.GetUVs(1,uv1);
            int flags1=m.HasProperty("_W9ParticleShaderFlags1")?m.GetInteger("_W9ParticleShaderFlags1"):(Mathf.RoundToInt(m.GetFloat("_NB_Flags1Lo16"))|(Mathf.RoundToInt(m.GetFloat("_NB_Flags1Hi16"))<<16));
            string path=AssetDatabase.GetAssetPath(m.shader),resolved=Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Application.dataPath),path.Replace("Packages/com.xuanxuan.nb.fx/","Packages/NB_FX/")));
            string shaderSHA;using(var hash=System.Security.Cryptography.SHA256.Create())shaderSHA=BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(resolved))).Replace("-","").ToLowerInvariant();
            var passes=new List<PassProbe>();for(int i=0;i<m.passCount;i++){string tag=m.shader.FindPassTagValue(i,new ShaderTagId("LightMode")).name;passes.Add(new PassProbe{displayName=m.GetPassName(i),lightMode=tag,enabled=m.GetShaderPassEnabled(string.IsNullOrEmpty(tag)?"SRPDefaultUnlit":tag)});}
            return new VATProbe{shader=m.shader.name,shaderPath=path,shaderSHA256=shaderSHA,serializedMaterial=EditorJsonUtility.ToJson(m),keywords=m.shaderKeywords,values=values,textures=textures.ToArray(),passes=passes.ToArray(),flags0=m.HasProperty("_W9ParticleShaderFlags")?m.GetInteger("_W9ParticleShaderFlags"):0,flags1=flags1,customDataFlag2=m.HasProperty("_W9ParticleCustomDataFlag2")?m.GetInteger("_W9ParticleCustomDataFlag2"):0,particleBit=(flags1&(1<<23))!=0,nativeVATKeyword=m.IsKeywordEnabled("_VAT"),nativeHoudiniKeyword=m.IsKeywordEnabled("_VAT_HOUDINI"),nativeSoftbodyKeyword=m.IsKeywordEnabled("_HOUDINI_VAT_SOFTBODY"),vertices=mesh.vertices,normals=mesh.normals,uv0=uv0.ToArray(),uv1=uv1.ToArray(),localToWorld=actor.localToWorldMatrix,receiverEnabled=receiver.enabled,shaderErrors=ShaderUtil.GetShaderMessages(m.shader).Where(e=>e.severity.ToString()=="Error").Select(e=>e.message).ToArray()};
        }
        [Serializable] sealed class Observation
        {
            public string label,source;public bool alphaClip,graphExtraNormalsEnabled,depthCopied,normalsCopied,aoCopied,depthOnly,shadowCaster,universalForward,srpDefaultUnlit;
            public int queue,depthNormalsPassIndex;public string[] keywords;
        }
        [Serializable] sealed class Result
        {
            public string scope,source;public bool orthographic,clip;public float ab,bc,bVsNoExtraDN,extraDNFinalResponse,repeat,independentLitSSAOResponse,
                validatedDepthForceOffBC,bDefaultDepthForceOffResponse,cDefaultDepthForceOffResponse;public Observation[] observations;
        }
        static float Delta(Color[] a,Color[] b)
        {float d=0;for(int i=0;i<a.Length;i++)d=Mathf.Max(d,Mathf.Abs(a[i].r-b[i].r),Mathf.Abs(a[i].g-b[i].g),Mathf.Abs(a[i].b-b[i].b),Mathf.Abs(a[i].a-b[i].a));return d;}
        static Color[] Read(RenderTexture target,Texture2D read,string file)
        {
            var old=RenderTexture.active;
            try
            {
                RenderTexture.active=target;read.ReadPixels(new Rect(0,0,target.width,target.height),0,0,false);read.Apply(false,false);var pixels=read.GetPixels();
                using(var w=new BinaryWriter(File.Create(file)))foreach(var px in pixels){w.Write(px.r);w.Write(px.g);w.Write(px.b);w.Write(px.a);}
                Assert.That(pixels.All(p=>!float.IsNaN(p.r)&&!float.IsInfinity(p.r)&&!float.IsNaN(p.g)&&!float.IsInfinity(p.g)&&!float.IsNaN(p.b)&&!float.IsInfinity(p.b)&&!float.IsNaN(p.a)&&!float.IsInfinity(p.a)),Is.True);
                return pixels;
            }
            finally {RenderTexture.active=old;}
        }
    }

    public sealed class DefaultSSAOBufferObserver : ScriptableRendererFeature
    {
        internal Camera targetCamera;internal Material copyMaterial;internal RenderTexture depth,normals,ao;
        internal bool depthCopied,normalsCopied,aoCopied;
        RTHandle depthHandle,normalsHandle,aoHandle;CopyPass pass;
        internal void Initialize(int width,int height)
        {
            RenderTexture Make(){var t=new RenderTexture(width,height,0,RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear);t.Create();return t;}
            depth=Make();normals=Make();ao=Make();depthHandle=RTHandles.Alloc(depth);normalsHandle=RTHandles.Alloc(normals);aoHandle=RTHandles.Alloc(ao);
        }
        internal void ResetFrameEvidence(){depthCopied=normalsCopied=aoCopied=false;}
        public override void Create(){pass=new CopyPass(this){renderPassEvent=RenderPassEvent.AfterRenderingOpaques+1};}
        public override void AddRenderPasses(ScriptableRenderer renderer,ref RenderingData data){if(data.cameraData.camera==targetCamera)renderer.EnqueuePass(pass);}
        internal void ReleaseTargets(){depthHandle?.Release();normalsHandle?.Release();aoHandle?.Release();foreach(var t in new[]{depth,normals,ao})if(t){t.Release();Object.DestroyImmediate(t);}}
        sealed class CopyPass : ScriptableRenderPass
        {
            readonly DefaultSSAOBufferObserver owner;internal CopyPass(DefaultSSAOBufferObserver value){owner=value;}
            sealed class Data{internal TextureHandle source;internal Material material;internal DefaultSSAOBufferObserver owner;internal int kind;}
            public override void RecordRenderGraph(RenderGraph graph,ContextContainer frameData)
            {
                var resources=frameData.Get<UniversalResourceData>();
                void Copy(TextureHandle source,RTHandle target,int kind)
                {
                    if(!source.IsValid())return;
                    using(var builder=graph.AddRasterRenderPass<Data>("DN0 observe actual buffer "+kind,out var data))
                    {
                        data.source=source;data.material=owner.copyMaterial;data.owner=owner;data.kind=kind;
                        builder.UseTexture(source,AccessFlags.Read);builder.SetRenderAttachment(graph.ImportTexture(target),0,AccessFlags.Write);builder.AllowPassCulling(false);
                        builder.SetRenderFunc(static(Data d,RasterGraphContext context)=>{
                            Blitter.BlitTexture(context.cmd,d.source,new Vector4(1,1,0,0),d.material,0);
                            if(d.kind==0)d.owner.depthCopied=true;else if(d.kind==1)d.owner.normalsCopied=true;else d.owner.aoCopied=true;
                        });
                    }
                }
                Copy(resources.cameraDepthTexture,owner.depthHandle,0);Copy(resources.cameraNormalsTexture,owner.normalsHandle,1);Copy(resources.ssaoTexture,owner.aoHandle,2);
            }
        }
    }
}
