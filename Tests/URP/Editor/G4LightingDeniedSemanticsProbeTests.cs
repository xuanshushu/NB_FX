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
    // Diagnostic only. C_fallback is a labelled temporary mode0 counterfactual,
    // not an implemented Graph Tier consumer. Capture all paths before assertions.
    public sealed class G4LightingDeniedSemanticsProbeTests
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
            public string scope="Native mode1 denied by actual resolver/projector vs clearly labelled Graph material copy mode0. Current C raw mode1 stays unchanged. Diagnostic, not current Graph Tier acceptance.",api;
            public string additionalMode;
            public bool finite,configurationRestored,captureComplete,restorationAttempted;
            public float rawOnBC,deniedVsOriginalC,deniedVsCounterfactualC,deniedVsCounterfactualPointOff;
            public float[] repeat,modeResponse,pointOnResponse,deniedPointResponse,restore;
            public int[] visible,modeResponsePixels,pointResponsePixels;public FrameState[] frames;
            public float nativeRawModeAfterDenial,originalGraphRawMode,counterfactualGraphRawMode;
            public string[] nativeDeniedKeywords;
            public GeometryControl geometry;
        }
        [TestCase(false,TestName="G4LightingDeniedProbe_BlinnPhong_PerPixel_ortho")]
        [TestCase(true,TestName="G4LightingDeniedProbe_BlinnPhong_PerVertex_ortho")]
        public void BlinnDenied(bool perVertex)
        {
            string id="lighting-denied-"+(perVertex?"vertex":"pixel");var mode=perVertex?LightRenderingMode.PerVertex:LightRenderingMode.PerPixel;
            PipelineScope pipeline=null;G4SpecDebugFixture.Harness h=null;Material fallback=null;GameObject pointGO=null;
            string folder=null;var report=new Report{additionalMode=mode.ToString(),api=SystemInfo.graphicsDeviceType.ToString()};
            try
            {
                pipeline=new PipelineScope(mode);h=new G4SpecDebugFixture.Harness(id,true);folder=h.folder;
                var map=h.Constant(new Color(.55f,.35f,.18f,1));var b=h.materials[1];var c=h.materials[2];
                Lighting("Configure",b,false,1,map);Lighting("Configure",c,true,1,map);
                b.SetFloat("_BlinnPhongSpecularToggle",0);c.SetFloat("_BlinnPhongSpecularToggle",0);
                foreach(var material in new[]{b,c})material.DisableKeyword("_SPECULAR_COLOR");
                fallback=new Material(c){hideFlags=HideFlags.HideAndDontSave};fallback.SetFloat("_FxLightMode",0);
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
                report.nativeRawModeAfterDenial=b.GetFloat("_FxLightMode");report.nativeDeniedKeywords=b.shaderKeywords.OrderBy(k=>k).ToArray();
                WriteMaterial(Path.Combine(folder,"B-denied-material.json"),b);WriteMaterial(Path.Combine(folder,"C-original-intent.json"),c);WriteMaterial(Path.Combine(folder,"C-counterfactual-mode0.json"),fallback);
                point.enabled=false;NativePolicy(b,true);Capture("B_on_pointOff",b);Capture("C_on_pointOff",c);
                NativePolicy(b,false);Capture("B_denied_pointOff",b);Capture("C_fallback_pointOff",fallback);
                point.enabled=true;Capture("B_denied_restore",b);Capture("C_fallback_restore",fallback);
                float D(string a,string z)=>G4SpecDebugFixture.Delta(frames[a],frames[z]);
                int Pixels(string a,string z)=>Enumerable.Range(0,frames[a].Length).Count(i=>Enumerable.Range(0,4).Any(k=>Mathf.Abs(frames[a][i][k]-frames[z][i][k])>.01f));
                report.rawOnBC=D("B_on","C_on");report.deniedVsOriginalC=D("B_denied","C_intent");report.deniedVsCounterfactualC=D("B_denied","C_fallback");report.deniedVsCounterfactualPointOff=D("B_denied_pointOff","C_fallback_pointOff");
                report.modeResponse=new[]{D("B_on","B_denied"),D("C_on","C_fallback")};report.pointOnResponse=new[]{D("B_on","B_on_pointOff"),D("C_on","C_on_pointOff")};report.deniedPointResponse=new[]{D("B_denied","B_denied_pointOff"),D("C_fallback","C_fallback_pointOff")};report.restore=new[]{D("B_denied","B_denied_restore"),D("C_fallback","C_fallback_restore")};
                report.modeResponsePixels=new[]{Pixels("B_on","B_denied"),Pixels("C_on","C_fallback")};report.pointResponsePixels=new[]{Pixels("B_on","B_on_pointOff"),Pixels("C_on","C_on_pointOff")};
                report.finite=repeatsFinite&&G4SpecDebugFixture.Finite(empty)&&frames.Values.All(G4SpecDebugFixture.Finite);report.repeat=repeats.ToArray();report.visible=frames.Values.Select(f=>G4SpecDebugFixture.Visible(f,empty)).ToArray();report.frames=states.ToArray();report.originalGraphRawMode=c.GetFloat("_FxLightMode");report.counterfactualGraphRawMode=fallback.GetFloat("_FxLightMode");report.captureComplete=true;
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
            }
            finally
            {
                if(pointGO)Object.DestroyImmediate(pointGO);if(fallback)Object.DestroyImmediate(fallback);h?.Dispose();
                try{report.restorationAttempted=true;pipeline?.Dispose();report.configurationRestored=pipeline!=null&&pipeline.restored;}
                finally{if(folder!=null)File.WriteAllText(Path.Combine(folder,"restoration.json"),JsonUtility.ToJson(report,true));}
            }
        }
    }
}
