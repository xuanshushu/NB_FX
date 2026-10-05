using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
    // Versioned saved-Tier tests retain the entire original v4 assertion block.
    // C_fallback remains a labelled temporary mode0 counterfactual,
    // not an implemented Graph Tier consumer. Capture all paths before assertions.
    public sealed class G4GraphLightingSavedTierTests
    {
        const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
        [OneTimeSetUp] public void Guard()
        {
            Assert.That(Path.GetFullPath(Application.dataPath),Is.EqualTo(Path.GetFullPath(@"D:\UnityProject\NBUnityProject\.utmp\NBFXMeshValidation-20261002\Assets")).IgnoreCase);
            for(int i=0;i<SceneManager.sceneCount;++i){var s=SceneManager.GetSceneAt(i);Assert.That((s.name+"/"+s.path).IndexOf("TAI",StringComparison.OrdinalIgnoreCase),Is.LessThan(0));}
            PrepareExactRunnerBootstrap();
            var active=SceneManager.GetActiveScene();Assert.That(active.IsValid()&&active.isLoaded,Is.True);
            Assert.That(Resources.FindObjectsOfTypeAll<Light>().Any(l=>l&&l.enabled&&l.gameObject.scene==active),Is.False,"Use the existing empty isolated runner scene; do not disable user lights.");
        }
        // This Unity Test Framework creates DefaultGameObjects AFTER the CLI
        // pre-run scene guard. Only its exact clean temporary bootstrap is replaced.
        // Unknown objects/components/lights are never disabled or deleted here.
        static bool DefaultComponents(GameObject go,Type[] required,params Type[] optional)
        {
            var components=go.GetComponents<Component>();
            if(components.Any(c=>c==null))return false;
            return required.All(t=>components.Count(c=>c.GetType()==t)==1) &&
                components.All(c=>required.Contains(c.GetType())||optional.Contains(c.GetType())) &&
                optional.All(t=>components.Count(c=>c.GetType()==t)<=1);
        }
        static void PrepareExactRunnerBootstrap()
        {
            var scene=SceneManager.GetActiveScene();Assert.That(scene.IsValid()&&scene.isLoaded,Is.True);
            var roots=scene.GetRootGameObjects();if(roots.Length==0)return;
            var cameraGO=roots.SingleOrDefault(go=>go.name=="Main Camera");
            var lightGO=roots.SingleOrDefault(go=>go.name=="Directional Light");
            bool recognized=SceneManager.sceneCount==1 && string.IsNullOrEmpty(scene.path) && !scene.isDirty && roots.Length==2 &&
                cameraGO!=null&&lightGO!=null&&cameraGO.activeSelf&&lightGO.activeSelf &&
                cameraGO.hideFlags==HideFlags.None&&lightGO.hideFlags==HideFlags.None &&
                cameraGO.layer==0&&lightGO.layer==0&&cameraGO.CompareTag("MainCamera")&&lightGO.CompareTag("Untagged") &&
                cameraGO.transform.childCount==0&&lightGO.transform.childCount==0 &&
                DefaultComponents(cameraGO,new[]{typeof(Transform),typeof(Camera),typeof(AudioListener)},typeof(UniversalAdditionalCameraData)) &&
                DefaultComponents(lightGO,new[]{typeof(Transform),typeof(Light)},typeof(UniversalAdditionalLightData)) &&
                cameraGO.GetComponent<Camera>().enabled&&lightGO.GetComponent<Light>().enabled&&lightGO.GetComponent<Light>().type==LightType.Directional;
            Debug.Log("NBFX_LIGHTING_BOOTSTRAP recognized="+recognized+" scene="+scene.handle+" path="+scene.path+" dirty="+scene.isDirty+" roots="+
                string.Join(";",roots.Select(go=>go.name+":"+string.Join(",",go.GetComponents<Component>().Select(c=>c?c.GetType().FullName:"missing")))));
            Assert.That(recognized,Is.True,"Only exact clean Unity runner default Camera/Light scene may be replaced. Unknown content remains untouched.");
            var empty=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            Assert.That(empty.IsValid()&&empty.isLoaded&&string.IsNullOrEmpty(empty.path)&&!empty.isDirty,Is.True);
            Assert.That(empty.GetRootGameObjects(),Is.Empty);Debug.Log("NBFX_LIGHTING_BOOTSTRAP empty scene="+empty.handle+" roots=0");
        }
        static object Lighting(string name,params object[] args)=>typeof(G4GraphLightingTests).GetMethod(name,All).Invoke(null,args);
        static void WriteMaterial(string path,Material material)=>typeof(G4GraphMeshParityTests).GetMethod("WriteMaterial",All).Invoke(null,new object[]{path,material});
        static string[] Raw()=>(string[])G4SpecDebugFixture.FindType("NBShader.NBShaderFeatureCatalog").GetField("RawKeywords",All).GetValue(null);
        static object NativePolicy(Material material,bool allowMode)
        {
            var resolver=G4SpecDebugFixture.FindType("NBShader.NBShaderMaterialIntentResolver");
            var method=resolver.GetMethods(All).Single(m=>m.Name=="Resolve"&&m.GetParameters().Length==4);
            var tier=Enum.ToObject(G4SpecDebugFixture.FindType("NBShader.NBShaderFeatureTier"),3);
            var passIds=(string[])G4SpecDebugFixture.FindType("NBShader.NBShaderPassFeatureCatalog").GetField("RawPassFeatureIds",All).GetValue(null);
            var allowed=Raw().Where(k=>allowMode||k!="_FX_LIGHT_MODE_BLINN_PHONG").ToArray();
            var result=method.Invoke(null,new object[]{material,tier,allowed,passIds});
            G4SpecDebugFixture.FindType("NBShader.NBShaderFeatureRuntime").GetMethod("ApplyResolvedIntent",All).Invoke(null,new[]{material,result});
            return result;
        }
        sealed class PipelineScope : IDisposable
        {
            internal readonly UniversalRenderPipelineAsset asset;
            internal readonly UniversalRendererData renderer;
            readonly RenderPipelineAsset oldQuality;
            readonly UniversalRenderPipelineAsset original;
            readonly List<Object> owned=new List<Object>();
            readonly Dictionary<Object,string> serialized=new Dictionary<Object,string>();
            readonly Dictionary<string,byte[]> files=new Dictionary<string,byte[]>();
            internal bool restored;
            void Remember(Object value)
            {
                if(!value||serialized.ContainsKey(value))return;serialized.Add(value,EditorJsonUtility.ToJson(value));
                string p=AssetDatabase.GetAssetPath(value);if(!string.IsNullOrEmpty(p)){p=Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Application.dataPath),p));if(File.Exists(p))files[p]=File.ReadAllBytes(p);}
            }
            internal PipelineScope(LightRenderingMode mode)
            {
                oldQuality=QualitySettings.renderPipeline;original=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;Assert.That(original,Is.Not.Null);
                try
                {
                    foreach(string name in new[]{"QualitySettings.asset","GraphicsSettings.asset"})
                    {string path=Path.Combine(Path.GetDirectoryName(Application.dataPath),"ProjectSettings",name);files[path]=File.ReadAllBytes(path);}
                    Remember(original);var data=original.rendererDataList[0] as UniversalRendererData;Assert.That(data,Is.Not.Null);Remember(data);foreach(var f in data.rendererFeatures)Remember(f);
                    asset=Object.Instantiate(original);owned.Add(asset);asset.hideFlags=HideFlags.HideAndDontSave;
                    renderer=Object.Instantiate(data);owned.Add(renderer);renderer.hideFlags=HideFlags.HideAndDontSave;
                    renderer.rendererFeatures.Clear();foreach(var f in data.rendererFeatures)if(f){var copy=Object.Instantiate(f);copy.hideFlags=HideFlags.HideAndDontSave;owned.Add(copy);renderer.rendererFeatures.Add(copy);}
                    renderer.renderingMode=RenderingMode.Forward;
                    var so=new SerializedObject(asset);var list=so.FindProperty("m_RendererDataList");Assert.That(list,Is.Not.Null);list.arraySize=1;list.GetArrayElementAtIndex(0).objectReferenceValue=renderer;
                    so.FindProperty("m_DefaultRendererIndex").intValue=0;
                    so.FindProperty("m_AdditionalLightsRenderingMode").intValue=(int)mode;
                    so.FindProperty("m_MainLightRenderingMode").intValue=(int)LightRenderingMode.PerPixel;
                    so.FindProperty("m_AdditionalLightsPerObjectLimit").intValue=4;so.ApplyModifiedPropertiesWithoutUndo();renderer.SetDirty();
                    Assert.That(asset.additionalLightsRenderingMode,Is.EqualTo(mode));Assert.That(renderer.renderingMode,Is.EqualTo(RenderingMode.Forward));
                    QualitySettings.renderPipeline=asset;
                }
                catch{Dispose();throw;}
            }
            public void Dispose()
            {
                QualitySettings.renderPipeline=oldQuality;
                try
                {
                    // A real empty render rebuilds the original pipeline after
                    // the temporary override, before cloned assets are destroyed.
                    var go=new GameObject("NB lighting probe restore camera");var camera=go.AddComponent<Camera>();
                    var rt=new RenderTexture(8,8,24,RenderTextureFormat.ARGBHalf);var previous=RenderTexture.active;
                    try{camera.enabled=false;camera.cullingMask=0;camera.clearFlags=CameraClearFlags.SolidColor;camera.targetTexture=rt;rt.Create();camera.Render();camera.Render();}
                    finally{camera.targetTexture=null;RenderTexture.active=previous;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(go);}
                    Assert.That(QualitySettings.renderPipeline,Is.EqualTo(oldQuality));Assert.That(GraphicsSettings.currentRenderPipeline,Is.EqualTo(original));
                    foreach(var pair in serialized)Assert.That(EditorJsonUtility.ToJson(pair.Key),Is.EqualTo(pair.Value),"Original pipeline/renderer/feature serialized state changed.");
                    foreach(var pair in files)Assert.That(File.ReadAllBytes(pair.Key),Is.EqualTo(pair.Value),"Original asset bytes changed: "+pair.Key);
                    restored=true;
                }
                finally{foreach(var value in owned.AsEnumerable().Reverse())if(value)Object.DestroyImmediate(value);owned.Clear();}
            }
        }
        [Serializable] sealed class GeometryControl
        {
            public bool planarQuad,flipped,pointCoverageValid;
            public int directionalLights,pointLights;
            public Vector3 rotationBefore,rotationAfter,pointPosition;
            public float pointRange,facingBefore,facingAfter;
            public uint rendererLayers;public int pointLayers;
            public Vector3[] worldVerticesBefore,worldVerticesAfter,worldNormalsBefore,worldNormalsAfter;
            public float[] distanceBefore,distanceAfter,ndotlBefore,ndotlAfter;
        }
        static GeometryControl PreparePointCoverage(MeshRenderer renderer,Camera camera,Light point)
        {
            var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;var vertices=mesh.vertices;var normals=mesh.normals;
            var g=new GeometryControl{rotationBefore=renderer.transform.localEulerAngles,pointPosition=point.transform.position,pointRange=point.range,
                rendererLayers=renderer.renderingLayerMask,pointLayers=point.renderingLayerMask};
            g.planarQuad=vertices.Length==4 && normals.Length==4 && normals.All(n=>n.sqrMagnitude>.9f && Vector3.Dot(n.normalized,normals[0].normalized)>.999f);
            void Read(out Vector3[] positions,out Vector3[] worldNormals,out float[] distances,out float[] dots)
            {
                var normalMatrix=renderer.transform.localToWorldMatrix.inverse.transpose;
                positions=vertices.Select(renderer.transform.TransformPoint).ToArray();
                worldNormals=normals.Select(n=>normalMatrix.MultiplyVector(n).normalized).ToArray();
                distances=positions.Select(p=>Vector3.Distance(point.transform.position,p)).ToArray();
                dots=new float[Math.Min(positions.Length,worldNormals.Length)];
                for(int i=0;i<dots.Length;++i)dots[i]=Vector3.Dot(worldNormals[i],(point.transform.position-positions[i]).normalized);
            }
            Read(out g.worldVerticesBefore,out g.worldNormalsBefore,out g.distanceBefore,out g.ndotlBefore);
            Vector3 view=(camera.transform.position-renderer.transform.position).normalized;
            g.facingBefore=g.worldNormalsBefore.Length==0?0:Vector3.Dot(g.worldNormalsBefore[0],view);
            // Only this known planar test actor may be flipped, after recording
            // actual back-facing normals AND all nonpositive vertex light dots.
            if(g.planarQuad && g.facingBefore<0 && g.ndotlBefore.All(v=>v<=0))
            {renderer.transform.Rotate(0,180,0,Space.Self);g.flipped=true;}
            g.rotationAfter=renderer.transform.localEulerAngles;
            Read(out g.worldVerticesAfter,out g.worldNormalsAfter,out g.distanceAfter,out g.ndotlAfter);
            g.facingAfter=g.worldNormalsAfter.Length==0?0:Vector3.Dot(g.worldNormalsAfter[0],view);
            g.pointCoverageValid=g.planarQuad&&g.facingAfter>0&&g.distanceAfter.All(v=>v<point.range)&&g.ndotlAfter.All(v=>v>.05f);
            var lights=Resources.FindObjectsOfTypeAll<Light>().Where(l=>l&&l.enabled&&l.gameObject.activeInHierarchy&&l.gameObject.scene==camera.scene).ToArray();
            g.directionalLights=lights.Count(l=>l.type==LightType.Directional);g.pointLights=lights.Count(l=>l.type==LightType.Point);
            return g;
        }
        [Serializable] sealed class FrameState
        {
            public string label,additionalMode,rendererMode;
            public bool pointEnabled,additionalPixel,additionalVertex,cluster,mainPassEnabled;
            public float rawFxMode;
            public Vector4 additionalLightShaderVector; // Raw _AdditionalLightsCount; not a claimed per-object light count.
            public string[] keywords;
        }
        [Serializable] sealed class Report
        {
            public string scope="Native mode1 denied by actual resolver/projector vs clearly labelled Graph material copy mode0. Original C raw mode1 stays unchanged. Separate actualTier fields validate the real saved-policy gate; counterfactual alone is not acceptance.",api;
            public string additionalMode;
            public bool finite,configurationRestored,captureComplete,restorationAttempted;
            public float rawOnBC,deniedVsOriginalC,deniedVsCounterfactualC,deniedVsCounterfactualPointOff;
            public float[] repeat,modeResponse,pointOnResponse,deniedPointResponse,restore;
            public int[] visible,modeResponsePixels,pointResponsePixels;public FrameState[] frames;
            public float nativeRawModeAfterDenial,originalGraphRawMode,counterfactualGraphRawMode;
            public string[] nativeDeniedKeywords;
            public GeometryControl geometry;
            public bool policyRestored;public float[] actualTierBC;public float actualTierRestore,actualTierResponse,actualDeniedGate,actualDeniedRawMode;public int actualTierResponsePixels;
        }
        [TestCase(false,TestName="G4LightingSavedTier_GPU_BlinnPhong_PerPixel_ortho")]
        [TestCase(true,TestName="G4LightingSavedTier_GPU_BlinnPhong_PerVertex_ortho")]
        public void BlinnDenied(bool perVertex)
        {
            string id="lighting-saved-tier-"+(perVertex?"vertex":"pixel");var mode=perVertex?LightRenderingMode.PerVertex:LightRenderingMode.PerPixel;
            PipelineScope pipeline=null;G4SpecDebugFixture.Harness h=null;Material fallback=null,tierGraph=null;GameObject pointGO=null;PolicyScope savedPolicy=null;
            string folder=null;var report=new Report{additionalMode=mode.ToString(),api=SystemInfo.graphicsDeviceType.ToString()};
            try
            {
                savedPolicy=new PolicyScope();savedPolicy.Allow();
                pipeline=new PipelineScope(mode);h=new G4SpecDebugFixture.Harness(id,true);folder=h.folder;
                var map=h.Constant(new Color(.55f,.35f,.18f,1));var b=h.materials[1];var c=h.materials[2];
                Lighting("Configure",b,false,1,map);Lighting("Configure",c,true,1,map);
                b.SetFloat("_BlinnPhongSpecularToggle",0);c.SetFloat("_BlinnPhongSpecularToggle",0);
                foreach(var material in new[]{b,c})material.DisableKeyword("_SPECULAR_COLOR");
                fallback=new Material(c){hideFlags=HideFlags.HideAndDontSave};fallback.SetFloat("_FxLightMode",0);
                ProjectGraph(c);tierGraph=new Material(c){hideFlags=HideFlags.HideAndDontSave};
                var cBefore=G4SpecDebugFixture.Properties(c);var cKeywords=c.shaderKeywords.OrderBy(k=>k).ToArray();
                var sun=Resources.FindObjectsOfTypeAll<Light>().Single(l=>l&&l.gameObject.scene==h.camera.scene&&l.type==LightType.Directional);sun.intensity=1.4f;
                pointGO=new GameObject("L0 additional point diagnostic",typeof(Light));SceneManager.MoveGameObjectToScene(pointGO,h.camera.scene);
                var point=pointGO.GetComponent<Light>();point.type=LightType.Point;point.color=Color.blue;point.intensity=4;point.range=7;point.shadows=LightShadows.None;point.transform.position=new Vector3(.7f,.4f,1.3f);
                report.geometry=PreparePointCoverage(h.renderer,h.camera,point);
                var sh=new SphericalHarmonicsL2();sh.AddDirectionalLight(Vector3.forward,new Color(.25f,.05f,.02f),1);RenderSettings.ambientMode=AmbientMode.Custom;RenderSettings.ambientProbe=sh;
                h.camera.GetUniversalAdditionalCameraData().SetRenderer(0);
                var frames=new Dictionary<string,Color[]>();var repeats=new List<float>();var states=new List<FrameState>();var empty=h.Snap("empty");bool repeatsFinite=true;
                void Capture(string label,Material material)
                {
                    var pixels=h.Snap(label,material);var repeat=h.Snap(label+"-repeat",material);frames.Add(label,pixels);repeats.Add(G4SpecDebugFixture.Delta(pixels,repeat));repeatsFinite&=G4SpecDebugFixture.Finite(repeat);
                    int role=material==b?1:2;states.Add(new FrameState{label=label,additionalMode=pipeline.asset.additionalLightsRenderingMode.ToString(),rendererMode=pipeline.renderer.renderingMode.ToString(),pointEnabled=point.enabled,
                        additionalLightShaderVector=Shader.GetGlobalVector("_AdditionalLightsCount"),additionalPixel=Shader.IsKeywordEnabled("_ADDITIONAL_LIGHTS"),additionalVertex=Shader.IsKeywordEnabled("_ADDITIONAL_LIGHTS_VERTEX"),cluster=Shader.IsKeywordEnabled("_CLUSTER_LIGHT_LOOP"),mainPassEnabled=material.GetShaderPassEnabled(h.tags[role]),rawFxMode=material.GetFloat("_FxLightMode"),keywords=material.shaderKeywords.OrderBy(k=>k).ToArray()});
                }
                NativePolicy(b,true);Capture("B_on",b);Capture("C_on",c);
                NativePolicy(b,false);Capture("B_denied",b);Capture("C_intent",c);Capture("C_fallback",fallback);
                savedPolicy.Allow("_FX_LIGHT_MODE_BLINN_PHONG");ProjectGraph(tierGraph);Capture("C_actualTierDenied",tierGraph);
                report.actualDeniedGate=tierGraph.GetFloat(Gate);report.actualDeniedRawMode=tierGraph.GetFloat("_FxLightMode");
                report.nativeRawModeAfterDenial=b.GetFloat("_FxLightMode");report.nativeDeniedKeywords=b.shaderKeywords.OrderBy(k=>k).ToArray();
                WriteMaterial(Path.Combine(folder,"B-denied-material.json"),b);WriteMaterial(Path.Combine(folder,"C-original-intent.json"),c);WriteMaterial(Path.Combine(folder,"C-counterfactual-mode0.json"),fallback);
                point.enabled=false;NativePolicy(b,true);Capture("B_on_pointOff",b);Capture("C_on_pointOff",c);
                NativePolicy(b,false);Capture("B_denied_pointOff",b);Capture("C_fallback_pointOff",fallback);
                Capture("C_actualTierDenied_pointOff",tierGraph);
                point.enabled=true;Capture("B_denied_restore",b);Capture("C_fallback_restore",fallback);
                savedPolicy.Allow();ProjectGraph(tierGraph);Capture("C_actualTierRestored",tierGraph);
                float D(string a,string z)=>G4SpecDebugFixture.Delta(frames[a],frames[z]);
                int Pixels(string a,string z)=>Enumerable.Range(0,frames[a].Length).Count(i=>Enumerable.Range(0,4).Any(k=>Mathf.Abs(frames[a][i][k]-frames[z][i][k])>.01f));
                report.rawOnBC=D("B_on","C_on");report.deniedVsOriginalC=D("B_denied","C_intent");report.deniedVsCounterfactualC=D("B_denied","C_fallback");report.deniedVsCounterfactualPointOff=D("B_denied_pointOff","C_fallback_pointOff");
                report.modeResponse=new[]{D("B_on","B_denied"),D("C_on","C_fallback")};report.pointOnResponse=new[]{D("B_on","B_on_pointOff"),D("C_on","C_on_pointOff")};report.deniedPointResponse=new[]{D("B_denied","B_denied_pointOff"),D("C_fallback","C_fallback_pointOff")};report.restore=new[]{D("B_denied","B_denied_restore"),D("C_fallback","C_fallback_restore")};
                report.modeResponsePixels=new[]{Pixels("B_on","B_denied"),Pixels("C_on","C_fallback")};report.pointResponsePixels=new[]{Pixels("B_on","B_on_pointOff"),Pixels("C_on","C_on_pointOff")};
                report.finite=repeatsFinite&&G4SpecDebugFixture.Finite(empty)&&frames.Values.All(G4SpecDebugFixture.Finite);report.repeat=repeats.ToArray();report.visible=frames.Values.Select(f=>G4SpecDebugFixture.Visible(f,empty)).ToArray();report.frames=states.ToArray();report.originalGraphRawMode=c.GetFloat("_FxLightMode");report.counterfactualGraphRawMode=fallback.GetFloat("_FxLightMode");report.captureComplete=true;
                report.actualTierBC=new[]{D("B_denied","C_actualTierDenied"),D("B_denied_pointOff","C_actualTierDenied_pointOff")};
                report.actualTierRestore=D("C_on","C_actualTierRestored");
                report.actualTierResponse=D("C_on","C_actualTierDenied");report.actualTierResponsePixels=Pixels("C_on","C_actualTierDenied");
                File.WriteAllText(Path.Combine(folder,"diagnostic.json"),JsonUtility.ToJson(report,true));
                // All requested diagnostic images and numbers are durable BEFORE
                // strict comparisons can fail on the already-known raw vertex path.
                Assert.That(report.geometry.pointCoverageValid&&report.geometry.directionalLights==1&&report.geometry.pointLights==1,Is.True,"Recorded actual test geometry and unique controlled lights must be valid.");
                Assert.That(G4SpecDebugFixture.Properties(c),Is.EqualTo(cBefore));Assert.That(c.shaderKeywords.OrderBy(k=>k).ToArray(),Is.EqualTo(cKeywords));
                Assert.That(report.nativeRawModeAfterDenial,Is.EqualTo(1));Assert.That(report.nativeDeniedKeywords,Does.Not.Contain("_FX_LIGHT_MODE_BLINN_PHONG").And.Not.Contain("_FX_LIGHT_MODE_UNLIT"));
                Assert.That(report.originalGraphRawMode,Is.EqualTo(1));Assert.That(report.counterfactualGraphRawMode,Is.EqualTo(0));
                foreach(var material in new[]{b,c,fallback})Assert.That(ShaderUtil.GetShaderMessages(material.shader).Any(m=>m.severity.ToString()=="Error"),Is.False);
                Assert.That(report.finite,Is.True);Assert.That(report.visible.All(v=>v>128),Is.True);Assert.That(report.frames.All(s=>s.mainPassEnabled),Is.True);
                Assert.That(report.frames.Where(s=>s.pointEnabled).All(s=>perVertex?s.additionalVertex&&!s.additionalPixel:s.additionalPixel&&!s.additionalVertex),Is.True,"Actual global additional-light keyword must match the requested real setting.");
                Assert.That(report.frames.All(s=>!s.cluster),Is.True);Assert.That(report.repeat.Concat(report.restore).All(v=>v==0),Is.True);
                Assert.That(report.modeResponse.All(v=>v>.01f),Is.True);Assert.That(report.pointOnResponse.All(v=>v>.01f),Is.True,"Same strong point-light control in B and current C; a failure is evidence, not a relaxed threshold.");
                Assert.That(report.modeResponsePixels.Concat(report.pointResponsePixels).All(v=>v>=64),Is.True,"Original lighting strong controls require at least64 changed pixels, not a single outlier.");
                Assert.That(report.rawOnBC,Is.Zero,"Existing raw B/C strict comparison.");Assert.That(report.deniedVsCounterfactualC,Is.Zero,"Proposed mode0 fallback strict comparison; no current product Tier claim.");Assert.That(report.deniedVsCounterfactualPointOff,Is.Zero);
                Assert.That(report.actualTierBC.All(v=>v==0)&&report.actualTierRestore==0,Is.True,"Actual saved Tier Graph gate path, not counterfactual acceptance.");
                Assert.That(report.actualTierResponse,Is.GreaterThan(.01f));Assert.That(report.actualTierResponsePixels,Is.GreaterThanOrEqualTo(64));
                Assert.That(report.actualDeniedGate,Is.Zero);Assert.That(report.actualDeniedRawMode,Is.EqualTo(1));
                Assert.That(tierGraph.GetFloat(Gate),Is.EqualTo(1));Assert.That(tierGraph.GetFloat("_FxLightMode"),Is.EqualTo(1));
            }
            finally
            {
                if(pointGO)Object.DestroyImmediate(pointGO);if(fallback)Object.DestroyImmediate(fallback);if(tierGraph)Object.DestroyImmediate(tierGraph);h?.Dispose();
                try{report.restorationAttempted=true;pipeline?.Dispose();report.configurationRestored=pipeline!=null&&pipeline.restored;}
                finally{try{savedPolicy?.Dispose();report.policyRestored=savedPolicy!=null&&savedPolicy.restored;}finally{if(folder!=null)File.WriteAllText(Path.Combine(folder,"restoration.json"),JsonUtility.ToJson(report,true));}}
            }
        }

        const string Gate="_NB_TierAllowLighting",Tier="_NBShaderFeatureTier";
        static Type Applier=>G4SpecDebugFixture.FindType("NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier");
        static object Level(int n)=>Enum.ToObject(G4SpecDebugFixture.FindType("NBShader.NBShaderFeatureTier"),n);
        static string Snapshot(Material m)=>EditorJsonUtility.ToJson(m)+"\n"+string.Join("|",m.shaderKeywords.OrderBy(k=>k));
        static object Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,All).Invoke(target,args);
        static object Field(object target,string name)
        {for(Type t=target.GetType();t!=null;t=t.BaseType){var f=t.GetField(name,All|BindingFlags.DeclaredOnly);if(f!=null)return f.GetValue(target);}throw new MissingFieldException(name);}
        static string[] Projections()=> (string[])(Applier.GetField("GraphSupportedProjectionProperties",All)??Applier.GetField("GraphSupportedGateProperties",All)).GetValue(null);
        static string[] Declared()=> (string[])Applier.GetField("GraphDeclaredKeywordNames",All).GetValue(null);
        static void ProjectGraph(Material m)
        {
            object[] args={m,false};Assert.That(Applier.GetMethod("ApplyGraphSavedSupportedGateTier",All).Invoke(null,args),Is.True,"Real saved policy projection");
            G4SpecDebugFixture.Sync(m);AssertKeywords(m);
        }
        static string[] Effective(Material m)
        {
            object[] args={m,null};Assert.That(Applier.GetMethod("TryReadGraphSavedSupportedGateTier",All).Invoke(null,args),Is.True);
            return (string[])args[1].GetType().GetField("effectiveKeywords").GetValue(args[1]);
        }
        static void AssertKeywords(Material m)
        {
            var effective=Effective(m);
            foreach(string k in Declared())
            {Assert.That(m.shader.keywordSpace.FindKeyword(k).isValid,Is.True,k);Assert.That(m.IsKeywordEnabled(k),Is.EqualTo(effective.Contains(k=="EVALUATE_SH_VERTEX"?"_FX_LIGHT_MODE_SIX_WAY":k)),k);}
            Assert.That(m.GetFloat(Gate),Is.EqualTo(effective.Any(k=>k.StartsWith("_FX_LIGHT_MODE_",StringComparison.Ordinal))?1f:0f));
        }
        static void ProjectNative(Material m,PolicyScope policy)
        {
            var resolver=G4SpecDebugFixture.FindType("NBShader.NBShaderMaterialIntentResolver");
            var method=resolver.GetMethods(All).Single(v=>v.Name=="Resolve"&&v.GetParameters().Length==4);
            var passes=(string[])G4SpecDebugFixture.FindType("NBShader.NBShaderPassFeatureCatalog").GetField("RawPassFeatureIds",All).GetValue(null);
            var intent=method.Invoke(null,new object[]{m,Level(3),policy.allowed,passes});
            G4SpecDebugFixture.FindType("NBShader.NBShaderFeatureRuntime").GetMethod("ApplyResolvedIntent",All).Invoke(null,new[]{m,intent});
        }
        sealed class PolicyScope:IDisposable
        {
            readonly Object instance;readonly Type type;readonly string before,path;readonly byte[] bytes;readonly bool dirty;
            internal string[] allowed;internal bool restored;
            internal PolicyScope()
            {
                type=G4SpecDebugFixture.FindType("NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelProjectSettings");
                instance=(Object)type.GetProperty("instance",All|BindingFlags.FlattenHierarchy).GetValue(null);before=EditorJsonUtility.ToJson(instance);dirty=EditorUtility.IsDirty(instance);
                path=Path.Combine(Path.GetDirectoryName(Application.dataPath),"ProjectSettings/NBShaderFeatureLevels.asset");bytes=File.Exists(path)?File.ReadAllBytes(path):null;
                // In-memory existing configuration only: no setter that normalizes/saves.
                type.GetField("m_DisableQualityTierWatcher",All).SetValue(instance,true);
            }
            internal void Allow(params string[] denied)
            {
                allowed=Raw().Except(denied).ToArray();var f=type.GetField("m_TierKeywordSets",All);var item=f.FieldType.GetElementType();var list=Array.CreateInstance(item,4);
                for(int i=0;i<4;++i){var row=Activator.CreateInstance(item);item.GetField("tier").SetValue(row,Level(i));item.GetField("allowedKeywords").SetValue(row,allowed.ToArray());list.SetValue(row,i);}
                f.SetValue(instance,list);type.GetMethod("InvalidateAllowedKeywordSetCache",All).Invoke(instance,null);
                var actual=(IEnumerable<string>)type.GetMethod("GetAllowedKeywordSetForBuildInfoNoSave",All).Invoke(instance,new[]{Level(3)});Assert.That(actual,Is.EquivalentTo(allowed));
            }
            public void Dispose()
            {
                EditorJsonUtility.FromJsonOverwrite(before,instance);type.GetMethod("InvalidateAllowedKeywordSetCache",All).Invoke(instance,null);
                if(!dirty)EditorUtility.ClearDirty(instance);
                Assert.That(EditorJsonUtility.ToJson(instance),Is.EqualTo(before));Assert.That(File.Exists(path),Is.EqualTo(bytes!=null));if(bytes!=null)Assert.That(File.ReadAllBytes(path),Is.EqualTo(bytes));restored=true;
            }
        }
        static Dictionary<string,string> RawProperties(Material m)=>G4SpecDebugFixture.Properties(m).Where(p=>!Projections().Contains(p.Key)).ToDictionary(p=>p.Key,p=>p.Value);
        [Test] public void G4LightingSavedTier_CPU_SelectedModeAndExistingKeywords()
        {
            using(var policy=new PolicyScope())
            {
                policy.Allow();var m=G4SpecDebugFixture.NewGraph();
                try
                {
                    int gate=m.shader.FindPropertyIndex(Gate);Assert.That(gate,Is.GreaterThanOrEqualTo(0));Assert.That(m.shader.GetPropertyType(gate),Is.EqualTo(ShaderPropertyType.Float));Assert.That(m.shader.GetPropertyDefaultFloatValue(gate),Is.EqualTo(1));
                    m.SetFloat(Tier,3);m.SetFloat("_BlinnPhongSpecularToggle",1);m.SetFloat("_SixWayColorAbsorptionToggle",1);m.SetFloat("_ProgramNoise_Simple_Toggle",1);
                    for(int i=0;i<6;++i){m.SetFloat(G4SpecDebugFixture.DebugProps[i],1);m.SetFloat(G4SpecDebugFixture.Parents[i],1);}
                    string[] modes={"_FX_LIGHT_MODE_UNLIT","_FX_LIGHT_MODE_BLINN_PHONG","_FX_LIGHT_MODE_HALF_LAMBERT","_FX_LIGHT_MODE_PBR","_FX_LIGHT_MODE_SIX_WAY"};
                    for(int mode=0;mode<5;++mode)
                    {
                        m.SetFloat("_FxLightMode",mode);policy.Allow();ProjectGraph(m);var raw=RawProperties(m);
                        policy.Allow(modes[mode],"_SPECULAR_COLOR","VFX_SIX_WAY_ABSORPTION");ProjectGraph(m);Assert.That(m.GetFloat(Gate),Is.Zero);Assert.That(RawProperties(m),Is.EquivalentTo(raw));
                        // Real validator and final Sync must retain the same saved-policy result.
                        G4SpecDebugFixture.Validate(m);AssertKeywords(m);Assert.That(m.GetFloat("_FxLightMode"),Is.EqualTo(mode));
                        policy.Allow(G4SpecDebugFixture.DebugKeywords);ProjectGraph(m);Assert.That(G4SpecDebugFixture.DebugKeywords.Any(m.IsKeywordEnabled),Is.False);
                        policy.Allow();ProjectGraph(m);Assert.That(m.GetFloat(Gate),Is.EqualTo(1));Assert.That(m.IsKeywordEnabled("_SPECULAR_COLOR"),Is.True);Assert.That(m.IsKeywordEnabled("VFX_SIX_WAY_ABSORPTION"),Is.EqualTo(mode==4));
                    }
                    var saved=(bool[])Applier.GetMethod("CaptureGraphDeclaredKeywordState",All).Invoke(null,new object[]{m});foreach(string k in Declared())G4SpecDebugFixture.SetKeyword(m,k,!m.IsKeywordEnabled(k));
                    Applier.GetMethod("RestoreGraphDeclaredKeywordState",All).Invoke(null,new object[]{m,saved});AssertKeywords(m);
                }
                finally{Object.DestroyImmediate(m);}
            }
        }
        [Test] public void G4LightingSavedTier_CPU_UnknownSchemaNoOwnedWrites()
        {
            using(var policy=new PolicyScope())
            {
                policy.Allow();var m=G4SpecDebugFixture.NewGraph();
                try
                {
                    ProjectGraph(m);string baseline=EditorJsonUtility.ToJson(m);
                    foreach(var invalid in new[]{Tuple.Create("_NB_GraphGUIStateVersion",0f),Tuple.Create("_NB_GraphGUIStateVersion",3f),Tuple.Create(Tier,-1f),Tuple.Create(Tier,1.5f),Tuple.Create("_FxLightMode",5f),Tuple.Create(Gate,float.NaN)})
                    {
                        EditorJsonUtility.FromJsonOverwrite(baseline,m);m.SetFloat(invalid.Item1,invalid.Item2);var before=Snapshot(m);object[] args={m,false};
                        Assert.That(Applier.GetMethod("ApplyGraphSavedSupportedGateTier",All).Invoke(null,args),Is.False);G4SpecDebugFixture.Sync(m);Assert.That(Snapshot(m),Is.EqualTo(before),invalid.Item1);
                    }
                    foreach(string type in new[]{"Float","Integer"})
                    {
                        var shader=ShaderUtil.CreateShaderAsset("Shader \"Hidden/NBFX/LightingTierSchema"+type+"\" { Properties { "+Gate+"(\"gate\","+type+")=1 } SubShader { Pass {} } }",false);var probe=new Material(shader);
                        try{Assert.That(probe.shader.GetPropertyType(probe.shader.FindPropertyIndex(Gate)),Is.EqualTo(type=="Float"?ShaderPropertyType.Float:ShaderPropertyType.Int));var before=Snapshot(probe);object[] args={probe,false};Assert.That(Applier.GetMethod("ApplyGraphSavedSupportedGateTier",All).Invoke(null,args),Is.False);G4SpecDebugFixture.Sync(probe);Assert.That(Snapshot(probe),Is.EqualTo(before));}
                        finally{Object.DestroyImmediate(probe);Object.DestroyImmediate(shader);}
                    }
                    EditorJsonUtility.FromJsonOverwrite(baseline,m);m.SetFloat("_NB_GraphGUIStateVersion",0);m.SetFloat(Gate,.25f);var initial=Snapshot(m);var legacy=new G4GraphPersistentGateTierTests();Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
                    try
                    {
                        object root=Call(legacy,"Root",(object)new[]{m});object sync=root.GetType().GetProperty("SyncService",All).GetValue(root);
                        Assert.That(Call(sync,"TryInitializeGraphSupportedGateTierState"),Is.True);Assert.That(m.GetFloat("_NB_GraphGUIStateVersion"),Is.EqualTo(2));AssertKeywords(m);Undo.FlushUndoRecordObjects();Undo.PerformUndo();Assert.That(Snapshot(m),Is.EqualTo(initial));
                    }
                    finally{Undo.RevertAllDownToGroup(group);legacy.Cleanup();}
                }
                finally{Object.DestroyImmediate(m);}
            }
        }
        [Test] public void G4LightingSavedTier_GUI_MultiModeTierUndoPassive()
        {
            using(var policy=new PolicyScope())
            {
                policy.Allow("_FX_LIGHT_MODE_SIX_WAY");var a=G4SpecDebugFixture.NewGraph();var b=G4SpecDebugFixture.NewGraph();var legacy=new G4GraphPersistentGateTierTests();NBFXMainTexGUIEventHost host=null;
                Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
                try
                {
                    a.SetFloat("_FxLightMode",1);b.SetFloat("_FxLightMode",4);a.SetFloat("_BlinnPhongSpecularToggle",1);b.SetFloat("_SixWayColorAbsorptionToggle",1);ProjectGraph(a);ProjectGraph(b);
                    object root=Call(legacy,"Root",(object)new[]{a,b});object popup=null,block=null;
                    host=ScriptableObject.CreateInstance<NBFXMainTexGUIEventHost>();host.hideFlags=HideFlags.HideAndDontSave;host.position=new Rect(15,15,600,260);host.SetupCommand="NBFX_LightingTier_"+Guid.NewGuid().ToString("N");
                    host.Setup=()=>{Assert.That(Call(root,"InitializeGraphLightModeInputs"),Is.True);block=Field(root,"_graphLightModeBlock");popup=((System.Collections.IList)Field(block,"ChildrenItemList"))[0];};host.ShowUtility();host.SendEvent(new Event{type=EventType.ExecuteCommand,commandName=host.SetupCommand});if(host.Failure!=null)throw host.Failure;Assert.That(host.Initialized,Is.True);
                    host.Draw=()=>{Call(block,"OnGUI");G4SpecDebugFixture.Sync(a);G4SpecDebugFixture.Sync(b);};
                    a.SetFloat(Gate,.25f); // Ready marker2 passive paint must preserve stale finite projection.
                    string beforeA=Snapshot(a),beforeB=Snapshot(b);
                    foreach(var kind in new[]{EventType.Layout,EventType.Repaint}){host.SendEvent(new Event{type=kind});if(host.Failure!=null)throw host.Failure;Assert.That(host.Counts.ContainsKey(kind),Is.True);}
                    Assert.That(Snapshot(a),Is.EqualTo(beforeA));Assert.That(Snapshot(b),Is.EqualTo(beforeB));
                    Assert.That(((MaterialProperty)Field(Field(popup,"PropertyInfo"),"Property")).hasMixedValue,Is.True);
                    host.Draw=()=>{if(Event.current.type==EventType.ExecuteCommand){Assert.That(Call(popup,"CommitSelectedMode",4),Is.True);Call(popup,"OnEndChange");}};host.SendEvent(new Event{type=EventType.ExecuteCommand,commandName="NBFX_Lighting_Select"});if(host.Failure!=null)throw host.Failure;
                    Undo.FlushUndoRecordObjects();Assert.That(a.GetFloat("_FxLightMode"),Is.EqualTo(4));Assert.That(a.GetFloat(Gate),Is.Zero);AssertKeywords(a);AssertKeywords(b);
                    Undo.PerformUndo();Assert.That(Snapshot(a),Is.EqualTo(beforeA));Assert.That(Snapshot(b),Is.EqualTo(beforeB));
                    object sync=root.GetType().GetProperty("SyncService",All).GetValue(root);Call(sync,"RefreshGraphMainTexPropertyReferences");
                    Undo.IncrementCurrentGroup();beforeA=Snapshot(a);beforeB=Snapshot(b);Assert.That(Call(sync,"TryApplyGraphSupportedGateTier",Level(1),null),Is.True);Undo.FlushUndoRecordObjects();Assert.That(a.GetFloat(Tier),Is.EqualTo(1));AssertKeywords(a);AssertKeywords(b);Undo.PerformUndo();Assert.That(Snapshot(a),Is.EqualTo(beforeA));Assert.That(Snapshot(b),Is.EqualTo(beforeB));
                    Call(sync,"RefreshGraphMainTexPropertyReferences");Undo.IncrementCurrentGroup();beforeA=Snapshot(a);beforeB=Snapshot(b);
                    host.Draw=()=>{if(Event.current.type==EventType.ExecuteCommand)Call(popup,"ExecuteReset",false);};host.SendEvent(new Event{type=EventType.ExecuteCommand,commandName="NBFX_Lighting_Reset"});if(host.Failure!=null)throw host.Failure;
                    Undo.FlushUndoRecordObjects();Assert.That(a.GetFloat("_FxLightMode"),Is.Zero);Assert.That(b.GetFloat("_FxLightMode"),Is.Zero);AssertKeywords(a);AssertKeywords(b);Undo.PerformUndo();Assert.That(Snapshot(a),Is.EqualTo(beforeA));Assert.That(Snapshot(b),Is.EqualTo(beforeB));
                    // Invalid second selection refuses the transaction before either target is written.
                    b.SetFloat("_NB_GraphGUIStateVersion",3);beforeA=Snapshot(a);beforeB=Snapshot(b);Assert.That(Call(sync,"TryApplyGraphSupportedGateTier",Level(0),null),Is.False);Assert.That(Snapshot(a),Is.EqualTo(beforeA));Assert.That(Snapshot(b),Is.EqualTo(beforeB));
                }
                finally{if(host){host.Draw=null;host.Setup=null;host.Close();Object.DestroyImmediate(host);}Undo.RevertAllDownToGroup(group);legacy.Cleanup();Object.DestroyImmediate(a);Object.DestroyImmediate(b);}
            }
        }
        [Serializable] sealed class KeywordGPUReport
        {
            public string caseId,scope="Saved policy B/C real projection; original raw inputs retained";public bool finite,configurationRestored,policyRestored,rawIntentPreserved=true;
            public float[] bc,repeat,response,restore;public int[] visible;public string[] keywords;public float[] gates,rawModes;
        }
        void KeywordGPU(bool sixWay)
        {
            var policy=new PolicyScope();PipelineScope pipeline=null;G4SpecDebugFixture.Harness h=null;var extra=new List<Object>();var report=new KeywordGPUReport{caseId=sixWay?"sixway-saved-tier":"spec-debug-saved-tier"};string folder=null;
            try
            {
                policy.Allow();pipeline=new PipelineScope(LightRenderingMode.PerPixel);h=new G4SpecDebugFixture.Harness(report.caseId,true);folder=h.folder;
                var b=h.materials[1];var c=h.materials[2];var frames=new List<Color[]>();var repeat=new List<float>();var pairs=new List<float>();var keys=new List<string[]>();var gates=new List<float>();var modes=new List<float>();bool finite=true;
                var empty=h.Snap("empty");
                void Pair(string label,params string[] denied)
                {
                    var beforeB=RawProperties(b);var beforeC=RawProperties(c);
                    policy.Allow(denied);ProjectNative(b,policy);ProjectGraph(c);
                    report.rawIntentPreserved&=beforeB.OrderBy(x=>x.Key).SequenceEqual(RawProperties(b).OrderBy(x=>x.Key))&&beforeC.OrderBy(x=>x.Key).SequenceEqual(RawProperties(c).OrderBy(x=>x.Key));
                    G4SpecDebugFixture.Harness.RestoreForward(b,false);G4SpecDebugFixture.Harness.RestoreForward(c,true);
                    var row=new List<Color[]>();foreach(var m in new[]{b,c}){string tag=m==b?"B":"C";var image=h.Snap(tag+"_"+label,m);var again=h.Snap(tag+"_"+label+"-repeat",m);row.Add(image);frames.Add(image);repeat.Add(G4SpecDebugFixture.Delta(image,again));finite&=G4SpecDebugFixture.Finite(again);keys.Add(m.shaderKeywords.OrderBy(k=>k).ToArray());gates.Add(m==c?m.GetFloat(Gate):-1);modes.Add(m.GetFloat("_FxLightMode"));}pairs.Add(G4SpecDebugFixture.Delta(row[0],row[1]));
                }
                if(!sixWay)
                {
                    foreach(var m in new[]{b,c}){m.SetFloat("_FxLightMode",1);m.SetFloat("_BlinnPhongSpecularToggle",1);m.SetColor("_SpecularColor",new Color(.8f,.7f,.6f,1));m.SetVector("_MaterialInfo",new Vector4(.6f,.5f,0,0));}
                    Pair("spec_on");Pair("spec_denied","_SPECULAR_COLOR");
                    // Use the existing original Fresnel debug input configuration.
                    typeof(G4GraphDebugTests).GetMethod("Configure",All).Invoke(null,new object[]{h,4,true});Pair("debug_on");Pair("debug_denied","NB_DEBUG_FRESNEL");Pair("debug_restore");
                    report.response=new[]{G4SpecDebugFixture.Delta(frames[0],frames[2]),G4SpecDebugFixture.Delta(frames[1],frames[3]),G4SpecDebugFixture.Delta(frames[4],frames[6]),G4SpecDebugFixture.Delta(frames[5],frames[7])};report.restore=new[]{G4SpecDebugFixture.Delta(frames[4],frames[8]),G4SpecDebugFixture.Delta(frames[5],frames[9])};
                }
                else
                {
                    h.renderer.transform.localScale=new Vector3(1.85f,1.33f,1);h.renderer.transform.rotation=Quaternion.Euler(10,18,14);
                    var sun=Resources.FindObjectsOfTypeAll<Light>().Single(l=>l&&l.gameObject.scene==h.camera.scene&&l.type==LightType.Directional);sun.intensity=1.4f;sun.transform.rotation=Quaternion.LookRotation(-new Vector3(.6f,.3f,.75f).normalized);
                    var map=h.Constant(new Color(.8f,.55f,.32f,1));var positive=(Texture2D)typeof(G4GraphSixWayTests).GetMethod("RigTexture",All).Invoke(null,new object[]{true,.63f});var negative=(Texture2D)typeof(G4GraphSixWayTests).GetMethod("RigTexture",All).Invoke(null,new object[]{false,.42f});extra.Add(positive);extra.Add(negative);var ramp=new Texture2D(4,4,TextureFormat.RGBAHalf,true,true);extra.Add(ramp);for(int y=0;y<4;++y)for(int x=0;x<4;++x)ramp.SetPixel(x,y,new Color(.2f+.18f*x,.9f-.14f*x,.35f+.1f*x,.8f));ramp.Apply(true);
                    RenderSettings.ambientMode=AmbientMode.Custom;RenderSettings.ambientProbe=(SphericalHarmonicsL2)typeof(G4GraphSixWayTests).GetMethod("Probe",All).Invoke(null,new object[]{new Color(.18f,.07f,.02f)});
                    foreach(var m in new[]{b,c}){typeof(G4GraphSixWayTests).GetMethod("Configure",All).Invoke(null,new object[]{m,m==c,map,positive,negative,ramp});m.SetFloat("_SixWayColorAbsorptionToggle",1);}
                    Pair("six_on");Pair("abs_denied","VFX_SIX_WAY_ABSORPTION");Pair("mode_denied","_FX_LIGHT_MODE_SIX_WAY");Pair("six_restore");
                    report.response=new[]{G4SpecDebugFixture.Delta(frames[0],frames[2]),G4SpecDebugFixture.Delta(frames[1],frames[3]),G4SpecDebugFixture.Delta(frames[0],frames[4]),G4SpecDebugFixture.Delta(frames[1],frames[5])};report.restore=new[]{G4SpecDebugFixture.Delta(frames[0],frames[6]),G4SpecDebugFixture.Delta(frames[1],frames[7])};
                    int Changed(Color[] x,Color[] y){object[] args={x,y,0,0f,.005f};typeof(G4GraphSixWayTests).GetMethod("Compare",All).Invoke(null,args);return (int)args[2];}
                    // Store control counts before asserting, as the original SixWay control does.
                    File.WriteAllText(Path.Combine(folder,"absorption-control.json"),JsonUtility.ToJson(new PixelControl{b=Changed(frames[0],frames[2]),c=Changed(frames[1],frames[3])},true));
                }
                report.bc=pairs.ToArray();report.repeat=repeat.ToArray();report.finite=finite&&G4SpecDebugFixture.Finite(empty)&&frames.All(G4SpecDebugFixture.Finite);report.visible=frames.Select(f=>G4SpecDebugFixture.Visible(f,empty)).ToArray();report.keywords=keys.Select(k=>string.Join(",",k)).ToArray();report.gates=gates.ToArray();report.rawModes=modes.ToArray();
                File.WriteAllText(Path.Combine(folder,"diagnostic.json"),JsonUtility.ToJson(report,true));
                Assert.That(report.rawIntentPreserved,Is.True,"Saved policy may not rewrite raw lighting/debug/absorption intent");
                Assert.That(report.finite,Is.True);Assert.That(report.visible.All(v=>v>128),Is.True);Assert.That(report.bc.Concat(report.repeat).Concat(report.restore).All(v=>v==0),Is.True,"Original strict zero BC/repeat/restore");
                Assert.That(report.response.All(v=>v>(sixWay?.005f:.001f)),Is.True,"Original consumer strong controls, unchanged thresholds");
                foreach(var m in new[]{b,c}){Assert.That(ShaderUtil.GetShaderMessages(m.shader).Any(e=>e.severity.ToString()=="Error"),Is.False);Assert.That(m.GetFloat("_FxLightMode"),Is.EqualTo(sixWay?4:1));}
                if(sixWay){var control=JsonUtility.FromJson<PixelControl>(File.ReadAllText(Path.Combine(folder,"absorption-control.json")));Assert.That(control.b,Is.GreaterThanOrEqualTo(64));Assert.That(control.c,Is.GreaterThanOrEqualTo(64));}
            }
            finally
            {
                foreach(var o in extra)if(o)Object.DestroyImmediate(o);h?.Dispose();
                try{pipeline?.Dispose();report.configurationRestored=pipeline!=null&&pipeline.restored;}
                finally{try{policy.Dispose();report.policyRestored=policy.restored;}finally{if(folder!=null)File.WriteAllText(Path.Combine(folder,"restoration.json"),JsonUtility.ToJson(report,true));}}
            }
        }
        [Serializable] sealed class PixelControl{public int b,c;}
        [Test] public void G4LightingSavedTier_GPU_SpecularDebug_PerPixel_ortho()=>KeywordGPU(false);
        [Test] public void G4LightingSavedTier_GPU_SixWayAbsorption_PerPixel_ortho()=>KeywordGPU(true);
    }
}
