using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using NBShader;

namespace NBFX.PlayerValidation.Editor
{
    // Only install into the dedicated isolated validation project's temporary
    // Assets directory. No product asmdef/main Assets/ProjectSettings mutation.
    public static class NBFXMeshPlayerBuilder
    {
        const string Package="Packages/com.xuanxuan.nb.fx/";
        const string Isolation="D:/UnityProject/NBUnityProject/.utmp/NBFXMeshValidation-20261002";
        const string Owner="D:/UnityProject/NBUnityProject/.utmp/nbp8";
        [Serializable] public sealed class SourceHash {public string absolutePath,sha256;}
        [Serializable] public sealed class BuildInputLock
        {public string identity,project;public SourceHash[] sources;public bool finalCombinationAccepted;public NBFXPlayerBuildPreparation.OwnedPlayerSettingsLock ownedPlayerSettings;}
        [Serializable] sealed class BuildReceipt
        {
            public string identity,project,unity,sourceLockSHA256,scene,output,pipeline,renderer,buildResult;
            public bool ranBuildPlayer,actualStandaloneWindows64,notRuntimeEvidence,sourceLockStillMatchesAfterBuild;
            public string sourceLockContract;public bool allInputBytesUnchangedAfterBuild,nonOwnedInputsBytesUnchangedAfterBuild,ownedPlayerSettingsAllowedAfterBuild,ownedGlobalSettingsAllowedAfterBuild;
            public int totalErrors,totalWarnings;
            public string[] retainedShaders,materialStates,sourcePaths,sourceSHA256;
            public string[] automaticFlipbookIdentities;
            public int expectedRuntimeCases,expectedRawCaptures;
        }
        static string Hash(string path)
        {using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
        static void Need(bool condition,string message)
        {if(!condition)throw new InvalidOperationException(message);}
        static string Absolute(string value)=>Path.GetFullPath(value).Replace('\\','/').TrimEnd('/');
        static void OwnOutput(string value)
        {Need(Absolute(value).StartsWith(Owner+"/",StringComparison.OrdinalIgnoreCase),"Build output must stay in owned preparation folder.");}
        static void Asset<T>(string path,T value) where T:UnityEngine.Object
        {Need(path.StartsWith("Assets/ResTemp/EditorTemp/NBFXPlayerValidation-",StringComparison.Ordinal)&&!File.Exists(path),"Only new isolated validation assets permitted.");AssetDatabase.CreateAsset(value,path);}
        static Texture2D Texture(string name,int size,Func<int,int,Color> pixel,string staging)
        {
            var t=new Texture2D(size,size,TextureFormat.RGBAHalf,false,true){name=name};
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)t.SetPixel(x,y,pixel(x,y));t.Apply(false);t.filterMode=FilterMode.Point;t.wrapMode=TextureWrapMode.Clamp;
            Asset(staging+"/"+name+".asset",t);return t;
        }
        static Type TestType(string name)
        {
            var t=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("NBFX.Baseline.Tests."+name,false)).FirstOrDefault(t=>t!=null);
            Need(t!=null,"Existing validated Editor fixture missing for configuration: "+name);return t;
        }
        static void ExistingConfigure(Material m,bool graph,string route,Texture2D noise,Texture2D mask)
        {
            var type=TestType("G4GraphScreenNoiseTests");
            var method=type.GetMethod("Configure",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
            Need(method!=null,"Actual existing Configure API absent.");
            method.Invoke(null,new object[]{m,graph,noise,mask,route,"noise-a-half"});
            m.SetFloat("_AlphaAll",.5f);m.SetFloat(graph?"_NB_DistortionIntensity":"_ScreenDistortIntensity",.35f);
        }
        static void ActualPass(Material m,bool graph,string route)
        {
            int index=m.FindPass(route=="Forward"?(graph?"Universal Forward":"UniversalForward"):route);Need(index>=0,"Actual pass unavailable after import: "+route);
            for(int i=0;i<m.passCount;i++)
            {string mode=m.shader.FindPassTagValue(0,i,new ShaderTagId("LightMode")).name;m.SetShaderPassEnabled(string.IsNullOrEmpty(mode)?"SRPDefaultUnlit":mode,false);}
            string selected=m.shader.FindPassTagValue(0,index,new ShaderTagId("LightMode")).name;
            m.SetShaderPassEnabled(string.IsNullOrEmpty(selected)?"SRPDefaultUnlit":selected,true);
        }
        static Material MakeMaterial(Shader shader,bool graph,string route,string state,Texture2D map,Texture2D noise,Texture2D mask,string staging)
        {
            var material=new Material(shader){name=(graph?"C":"B")+"-"+route+"-"+state};
            if(route=="Forward")
            {
                material.shaderKeywords=graph?new[]{"_SURFACE_TYPE_TRANSPARENT"}:new[]{"_FX_LIGHT_MODE_UNLIT"};
                material.SetTexture("_BaseMap",map);material.SetColor(graph?"_Color":"_BaseColor",Color.white);material.SetColor("_ColorA",Color.white);
                material.SetFloat("_BaseColorIntensityForTimeline",1);material.SetFloat("_AlphaAll",state=="on"?1:.35f);
                material.SetFloat("_Cull",0);material.SetFloat("_ZTest",4);material.SetFloat("_ZWrite",0);material.SetFloat("_SrcBlend",1);material.SetFloat("_DstBlend",0);material.SetFloat("_ColorMask",15);
                if(graph){material.SetFloat("_Surface",1);material.SetFloat("_SrcBlendAlpha",1);material.SetFloat("_DstBlendAlpha",0);material.SetVector("_BaseMap_ST",new Vector4(1,1,0,0));}
                material.renderQueue=3000;
            }
            else
            {
                ExistingConfigure(material,graph,route,noise,mask);
                if(state=="control")material.SetFloat("_AlphaAll",.25f);
                if(state=="strength0")material.SetFloat(graph?"_NB_DistortionIntensity":"_ScreenDistortIntensity",0);
            }
            ActualPass(material,graph,route);Asset(staging+"/"+material.name+".mat",material);return material;
        }
        static Material MakeAutomaticFlipbookMaterial(Shader shader,bool graph,Texture2D atlas,Texture2D noise,string staging)
        {
            var material=new Material(shader){name=(graph?"C":"B")+"-AutomaticFlipbook"};
            // Reuse existing proven input configuration, with Helper ownership off.
            // Actual Player OnEnable/Update alone will write bit15, current/next ST
            // and blend after this source material becomes a runtime instance.
            var method=TestType("G4GraphFlipbookTests").GetMethod("Configure",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
            Need(method!=null,"Existing Flipbook Configure API absent.");
            method.Invoke(null,new object[]{material,graph,"Forward",atlas,noise,false,false,0f});
            ActualPass(material,graph,"Forward");Asset(staging+"/"+material.name+".mat",material);return material;
        }
        public static void Build()
        {
            string project=Absolute(Path.GetDirectoryName(Application.dataPath));Need(project==Isolation,"Never operate on main or another project.");
            for(int i=0;i<SceneManager.sceneCount;i++)
            {var loadedScene=SceneManager.GetSceneAt(i);Need(!loadedScene.isLoaded||(loadedScene.name+"/"+loadedScene.path).IndexOf("TAI",StringComparison.OrdinalIgnoreCase)<0,"TAI loaded: do not import/reload/build.");}
            string lockPath=Environment.GetEnvironmentVariable("NBFX_PLAYER_SOURCE_LOCK");string output=Environment.GetEnvironmentVariable("NBFX_PLAYER_BUILD_DIR");
            Need(!string.IsNullOrEmpty(lockPath)&&!string.IsNullOrEmpty(output),"Explicit source lock/output env vars required.");OwnOutput(output);Need(!Directory.Exists(output),"Do not overwrite previous build/evidence.");
            var input=JsonUtility.FromJson<BuildInputLock>(File.ReadAllText(lockPath));Need(input!=null&&input.finalCombinationAccepted&&Absolute(input.project)==project&&!string.IsNullOrEmpty(input.identity),"Only reviewed final central combination can enter Player build.");
            Need(input.sources!=null&&input.sources.Length>0,"Source lock empty.");foreach(var source in input.sources)Need(File.Exists(source.absolutePath)&&Hash(source.absolutePath)==source.sha256,"Exact source drift: "+source.absolutePath);
            string staging="Assets/ResTemp/EditorTemp/NBFXPlayerValidation-"+input.identity;Need(!AssetDatabase.IsValidFolder(staging)&&input.identity.All(ch=>char.IsLetterOrDigit(ch)||ch=='-'),"New safe staging identity required.");
            AssetDatabase.CreateFolder("Assets/ResTemp/EditorTemp",Path.GetFileName(staging));Directory.CreateDirectory(output);
            var active=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;Need(active,"Actual active URP asset absent.");
            Need(active.rendererDataList.Length>0&&active.rendererDataList[0].rendererFeatures.OfType<NBPostProcess>().Any(f=>f&&f.isActive),"Active original NBPostProcess not configured.");
            var warmType=TestType("G4GraphGuiFeatureIntentTests");
            var warmClock=System.Diagnostics.Stopwatch.StartNew();
            try{warmType.GetMethod("WarmImportedGraphInRealUrpCamera").Invoke(Activator.CreateInstance(warmType),null);}
            finally{warmClock.Stop();File.WriteAllText(Path.Combine(output,"import-and-camera-warm-seconds.txt"),"Existing forced import plus real camera warm combined wall seconds (not pure/cold import): "+warmClock.Elapsed.TotalSeconds.ToString("R",System.Globalization.CultureInfo.InvariantCulture));}
            // Pipeline is only retained by a serialized reference and selected in
            // Player memory. This never writes Graphics/Quality/PlayerSettings.
            var config=ScriptableObject.CreateInstance<NBFXMeshPlayerConfig>();config.pipeline=active;config.buildIdentity=input.identity;config.sourceLockSHA256=Hash(lockPath);config.sourceReceiptJSON=File.ReadAllText(lockPath);
            config.currentShader=AssetDatabase.LoadAssetAtPath<Shader>(Package+"NBShaders2/Shader/NBShader.shader");config.graphShader=AssetDatabase.LoadAssetAtPath<Shader>(Package+"NBShaders2/ShaderGraph/NBShaderGraph.shadergraph");
            config.uberShader=AssetDatabase.LoadAssetAtPath<Shader>(Package+"NBPostProcessing/Shader/NBPostProcessUber.shader");config.colorBlitShader=AssetDatabase.LoadAssetAtPath<Shader>(Package+"NBPostProcessing/Shader/ColorBlit.shader");
            Need(config.currentShader&&config.graphShader&&config.uberShader&&config.colorBlitShader,"Required build-time Shader references missing.");
            config.uberRetentionMaterial=new Material(config.uberShader);config.colorBlitRetentionMaterial=new Material(config.colorBlitShader);
            Asset(staging+"/UberRetention.mat",config.uberRetentionMaterial);Asset(staging+"/ColorBlitRetention.mat",config.colorBlitRetentionMaterial);
            var gradient=Texture("Gradient",64,(x,y)=>new Color(.08f+.8f*x/63,.1f+.7f*y/63,.15f+.55f*((x+2*y)%64)/63,1),staging);
            var noise=Texture("Noise",2,(x,y)=>new Color(.75f,.5f,0,1),staging);var mask=Texture("Mask",2,(x,y)=>Color.white,staging);config.overlay=Texture("Overlay",2,(x,y)=>new Color(.65f,.15f,.8f,.75f),staging);
            var bgShader=Shader.Find("Universal Render Pipeline/Unlit");Need(bgShader,"Existing URP Unlit shader missing.");
            config.backgroundMaterial=new Material(bgShader);config.transparentBackgroundMaterial=new Material(bgShader);
            foreach(var material in new[]{config.backgroundMaterial,config.transparentBackgroundMaterial})
            {material.SetTexture("_BaseMap",gradient);material.SetFloat("_Cull",0);material.SetFloat("_ZTest",4);material.SetFloat("_ZWrite",0);}
            config.backgroundMaterial.SetColor("_BaseColor",Color.white);config.backgroundMaterial.renderQueue=2000;
            var trans=config.transparentBackgroundMaterial;trans.SetColor("_BaseColor",new Color(.75f,.85f,.65f,.35f));trans.SetFloat("_Surface",1);trans.SetFloat("_SrcBlend",5);trans.SetFloat("_DstBlend",10);trans.SetFloat("_SrcBlendAlpha",1);trans.SetFloat("_DstBlendAlpha",10);trans.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");trans.renderQueue=2900;
            Asset(staging+"/Background.mat",config.backgroundMaterial);Asset(staging+"/TransparentBackground.mat",trans);
            var cases=new List<PlayerCase>();
            foreach(string route in new[]{"Forward","NBCameraOpaqueDistortPass","NBDeferredDistortPass"})
            {
                Material B(string state)=>MakeMaterial(config.currentShader,false,route,state,gradient,noise,mask,staging);
                Material C(string state)=>MakeMaterial(config.graphShader,true,route,state,gradient,noise,mask,staging);
                var bo=B("on");var co=C("on");var bx=B("control");var cx=C("control");var bz=B("strength0");var cz=C("strength0");
                foreach(bool ortho in new[]{true,false})cases.Add(new PlayerCase{identity="PlayerBC_"+route+(ortho?"_ortho":"_perspective"),route=route,orthographic=ortho,currentOn=bo,graphOn=co,currentControl=bx,graphControl=cx,currentStrength0=bz,graphStrength0=cz});
            }
            config.cases=cases.ToArray();
            Color[] frameColors={Color.red,Color.green,Color.blue,Color.yellow};
            var atlas=Texture("AutomaticFlipbookAtlas",8,(x,y)=>frameColors[(1-y/4)*2+x/4],staging);
            var autoB=MakeAutomaticFlipbookMaterial(config.currentShader,false,atlas,noise,staging);
            var autoC=MakeAutomaticFlipbookMaterial(config.graphShader,true,atlas,noise,staging);
            config.automaticFlipbookCases=new[]{
                new PlayerFlipbookCase{identity="PlayerBC_FlipbookAutoLifecycle_ortho",orthographic=true,current=autoB,graph=autoC},
                new PlayerFlipbookCase{identity="PlayerBC_FlipbookAutoLifecycle_perspective",orthographic=false,current=autoB,graph=autoC}};
            Asset(staging+"/PlayerConfig.asset",config);
            var previousActiveScene=SceneManager.GetActiveScene();
            Scene scene=default,performanceScene=default,projectionScene=default;
            try
            {
            scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
            var host=new GameObject("Actual standalone runtime harness");var harness=host.AddComponent<NBFXMeshPlayerHarness>();harness.config=config;
            string scenePath=staging+"/PlayerScene.unity";Need(EditorSceneManager.SaveScene(scene,scenePath),"Failed saving isolated test scene.");
            performanceScene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(performanceScene);
            var perfHost=new GameObject("Independent performance entry using frozen input config");perfHost.AddComponent<NBFXMeshPerformanceSampler>().config=config;
            string performanceScenePath=staging+"/NBFXPerformance.unity";Need(EditorSceneManager.SaveScene(performanceScene,performanceScenePath),"Failed saving performance scene.");SceneManager.SetActiveScene(scene);
            projectionScene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(projectionScene);
            var runtimeProbe=new GameObject("Independent actual Runtime Graph gate and OVZ proof").AddComponent<NBFXRuntimeProjectionProbe>();NBFXRuntimeProjectionBuilder.Fill(runtimeProbe,config,staging);
            string projectionScenePath=staging+"/NBFXRuntimeProjection.unity";Need(EditorSceneManager.SaveScene(projectionScene,projectionScenePath),"Failed saving Runtime projection scene.");SceneManager.SetActiveScene(scene);NBFXPlayerBuildPreparation.SaveGeneratedAssets(staging);
            foreach(var source in input.sources)Need(Hash(source.absolutePath)==source.sha256,"Input changed while staging build: "+source.absolutePath);
            var receipt=new BuildReceipt{identity=input.identity,project=project,unity=Application.unityVersion,sourceLockSHA256=config.sourceLockSHA256,scene=scenePath,output=Absolute(output),pipeline=AssetDatabase.GetAssetPath(active),renderer=AssetDatabase.GetAssetPath(active.rendererDataList[0]),
                retainedShaders=new[]{config.currentShader.name,config.graphShader.name,config.uberShader.name,config.colorBlitShader.name,bgShader.name},materialStates=cases.SelectMany(c=>new[]{c.currentOn.name,c.graphOn.name,c.currentControl.name,c.graphControl.name,c.currentStrength0.name,c.graphStrength0.name}).Distinct().ToArray(),sourcePaths=input.sources.Select(s=>s.absolutePath).ToArray(),sourceSHA256=input.sources.Select(s=>s.sha256).ToArray(),notRuntimeEvidence=true};
            receipt.materialStates=receipt.materialStates.Concat(new[]{autoB.name,autoC.name}).ToArray();
            receipt.automaticFlipbookIdentities=config.automaticFlipbookCases.Select(c=>c.identity).ToArray();receipt.expectedRuntimeCases=8;receipt.expectedRawCaptures=140;
            try
            {
                NBFXPlayerBuildPreparation.VerifyBuildBoundary(output,staging);
                NBFXPlayerBuildAudit.RecordArtifacts(staging);
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{scenePath,performanceScenePath,projectionScenePath},locationPathName=Path.Combine(output,"NBFXMeshValidation.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
                NBFXPlayerBuildAudit.Record(report);receipt.ranBuildPlayer=true;receipt.actualStandaloneWindows64=report.summary.platform==BuildTarget.StandaloneWindows64;receipt.buildResult=report.summary.result.ToString();receipt.totalErrors=(int)report.summary.totalErrors;receipt.totalWarnings=(int)report.summary.totalWarnings;
                receipt.sourceLockStillMatchesAfterBuild=input.sources.All(source=>File.Exists(source.absolutePath)&&Hash(source.absolutePath)==source.sha256);
                Need(report.summary.result==BuildResult.Succeeded&&report.summary.totalErrors==0&&receipt.actualStandaloneWindows64,"Actual BuildPlayer failed; never use Editor tests as Player verdict.");
                Need(receipt.sourceLockStillMatchesAfterBuild,"Source/build inputs changed during BuildPlayer; retain output as unverified.");
            }
            finally{File.WriteAllText(Path.Combine(output,"build-receipt.json"),JsonUtility.ToJson(receipt,true));}
            }
            finally
            {
                if(projectionScene.IsValid()&&projectionScene.isLoaded)EditorSceneManager.CloseScene(projectionScene,true);
                if(performanceScene.IsValid()&&performanceScene.isLoaded)EditorSceneManager.CloseScene(performanceScene,true);
                if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);
                if(previousActiveScene.IsValid()&&previousActiveScene.isLoaded)SceneManager.SetActiveScene(previousActiveScene);
            }
        }
        [Serializable] public sealed class AdoptedFile { public string path,sha256; }
        [Serializable] public sealed class AdoptedMaterial { public string path,fileSHA256,serializedBefore; }
        [Serializable] public sealed class AdoptedStage
        { public string project,staging,svcReceiptSHA256,origin;public AdoptedFile[] files;public AdoptedMaterial[] materials; }
        [Serializable] public sealed class StagedReceipt
        { public string project,prepared,staging,adoptionPath,adoptionSHA256,sourceLock,sourceLockSHA256,buildIdentity,configJSON;public string[] scenes;public AdoptedFile[] files; }
        static readonly string[] StagedScenes={"PlayerScene.unity","NBFXPerformance.unity","NBFXRuntimeProjection.unity"};
        static string[] StagedIdentities()=>new[]{"Forward","NBCameraOpaqueDistortPass","NBDeferredDistortPass"}.SelectMany(r=>new[]{"PlayerBC_"+r+"_ortho","PlayerBC_"+r+"_perspective"}).ToArray();
        public static AdoptedFile[] StagedFiles(string staging)
        {
            Need(staging.StartsWith("Assets/ResTemp/EditorTemp/NBFXPlayerValidation-",StringComparison.Ordinal)&&!staging.Contains(".."),"Exact owned staging required.");
            return Directory.GetFiles(Path.Combine(Isolation,staging),"*",SearchOption.AllDirectories).OrderBy(x=>x,StringComparer.Ordinal).Select(p=>new AdoptedFile{path=Absolute(p).Substring(Isolation.Length+1),sha256=Hash(p)}).ToArray();
        }
        static void ExactFiles(string staging,AdoptedFile[] expected)
        {
            var actual=StagedFiles(staging);Need(expected!=null&&actual.Length==expected.Length,"Staging file set changed.");
            Need(actual.Select(x=>x.path+"|"+x.sha256).SequenceEqual(expected.OrderBy(x=>x.path,StringComparer.Ordinal).Select(x=>x.path+"|"+x.sha256)),"Staging file bytes changed; never regenerate or silently save inputs.");
        }
        static AdoptedStage Adoption(string path,string expectedSHA)
        {
            Need(File.Exists(path)&&Hash(path)==expectedSHA,"Exact Root-reviewed adoption receipt required.");
            var a=JsonUtility.FromJson<AdoptedStage>(File.ReadAllText(path));Need(a!=null&&a.project==Isolation&&a.files!=null&&a.files.Length==94&&a.materials!=null&&a.materials.Length==33,"Expected this specific 94-file /33-material existing stage.");return a;
        }
        static void ExactMaterialMemory(AdoptedStage a,string output)
        {
            var failures=new List<string>();var receipt=new List<string>();
            Need(a.materials.Select(x=>x.path).Distinct().Count()==33,"Duplicate material baseline.");
            foreach(var row in a.materials)
            {
                Need(row.path.StartsWith(a.staging+"/",StringComparison.Ordinal)&&row.path.EndsWith(".mat",StringComparison.Ordinal),"Material baseline escaped staging.");
                Need(Hash(Path.Combine(Isolation,row.path))==row.fileSHA256,"Material disk changed from SVC-before: "+row.path);
                var mat=AssetDatabase.LoadAssetAtPath<Material>(row.path);Need(mat&&mat.shader,"Missing actual material/shader: "+row.path);
                string actual=EditorJsonUtility.ToJson(mat);receipt.Add(row.path+"\n"+actual);
                if(actual!=row.serializedBefore)failures.Add(row.path);
                Need(!ShaderUtil.GetShaderMessages(mat.shader).Any(e=>e.severity.ToString()=="Error"),"Actual shader errors: "+row.path);
            }
            File.WriteAllText(Path.Combine(output,"material-memory-"+DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffff")+".txt"),string.Join("\n",receipt));
            Need(failures.Count==0,"Actual material input differs from SVC-before complete JSON (including valid/invalid keywords): "+string.Join(",",failures));
        }
        static NBFXMeshPlayerConfig ExistingConfig(string staging)
        {
            var c=AssetDatabase.LoadAssetAtPath<NBFXMeshPlayerConfig>(staging+"/PlayerConfig.asset");Need(c&&c.pipeline&&c.currentShader&&c.graphShader&&c.uberShader&&c.colorBlitShader,"Missing serialized config references.");
            Need(c.cases!=null&&c.cases.Select(x=>x.identity).SequenceEqual(StagedIdentities()),"Original six Player identities changed.");
            Need(c.automaticFlipbookCases!=null&&c.automaticFlipbookCases.Select(x=>x.identity).SequenceEqual(new[]{"PlayerBC_FlipbookAutoLifecycle_ortho","PlayerBC_FlipbookAutoLifecycle_perspective"}),"Original automatic Flipbook identities changed.");
            Need(AssetDatabase.GetAssetPath(c.currentShader)==Package+"NBShaders2/Shader/NBShader.shader"&&AssetDatabase.GetAssetPath(c.graphShader)==Package+"NBShaders2/ShaderGraph/NBShaderGraph.shadergraph","Shader references changed.");
            Need(c.pipeline==GraphicsSettings.currentRenderPipeline,"Runtime pipeline reference differs from current actual pipeline.");
            for(int i=0;i<c.cases.Length;i++){
                var item=c.cases[i];Need(item.route==new[]{"Forward","NBCameraOpaqueDistortPass","NBDeferredDistortPass"}[i/2]&&item.orthographic==(i%2==0),"Original camera/route changed.");
                foreach(var m in new[]{item.currentOn,item.graphOn,item.currentControl,item.graphControl,item.currentStrength0,item.graphStrength0})Need(m&&AssetDatabase.GetAssetPath(m).StartsWith(staging+"/",StringComparison.Ordinal),"Original case material missing/outside owned stage.");
            }
            for(int i=0;i<2;i++){var item=c.automaticFlipbookCases[i];Need(item.orthographic==(i==0)&&item.current&&item.graph&&AssetDatabase.GetAssetPath(item.current).StartsWith(staging+"/",StringComparison.Ordinal)&&AssetDatabase.GetAssetPath(item.graph).StartsWith(staging+"/",StringComparison.Ordinal),"Automatic Flipbook reference changed.");}
            return c;
        }
        public static string ValidateExistingStage(string adoptionPath,string adoptionSHA,string output)
        {
            NBFXPlayerBuildPreparation.Guard();var a=Adoption(adoptionPath,adoptionSHA);Directory.CreateDirectory(output);ExactFiles(a.staging,a.files);ExactMaterialMemory(a,output);var config=ExistingConfig(a.staging);
            var previous=SceneManager.GetActiveScene();Need(!string.IsNullOrEmpty(previous.path),"Owned saved bootstrap required for additive readonly scene inspection.");
            for(int i=0;i<StagedScenes.Length;i++)
            {
                Scene s=default;
                try{
                    s=EditorSceneManager.OpenScene(a.staging+"/"+StagedScenes[i],OpenSceneMode.Additive);var roots=s.GetRootGameObjects();Need(roots.Length==1&&roots[0].transform.childCount==0,"Unexpected staged scene content.");
                    var components=roots[0].GetComponents<Component>();Need(components.Length==2&&components.All(x=>x),"Unexpected staged components.");
                    NBFXMeshPlayerConfig referenced=null;
                    if(i==0){var h=roots[0].GetComponent<NBFXMeshPlayerHarness>();Need(h,"Original Player harness missing.");referenced=h.config;}
                    if(i==1){var h=roots[0].GetComponent<NBFXMeshPerformanceSampler>();Need(h,"Original performance harness missing.");referenced=h.config;}
                    if(i==2){var h=roots[0].GetComponent<NBFXRuntimeProjectionProbe>();Need(h&&h.native&&h.graph&&h.greenProbe&&h.allow&&h.denyMask&&h.denyMaskAndOVZ&&h.retainedVariants!=null&&h.retainedVariants.Length==6&&h.retainedVariants.All(m=>m&&AssetDatabase.GetAssetPath(m).StartsWith(a.staging+"/",StringComparison.Ordinal)),"Runtime probe/retention references missing.");referenced=h.config;}
                    Need(referenced==config&&!s.isDirty,"Scene config/reference changed; never save/rebuild original staged scenes.");
                }finally{if(s.IsValid()&&s.isLoaded)EditorSceneManager.CloseScene(s,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);}
            }
            ExactFiles(a.staging,a.files);ExactMaterialMemory(a,output);return EditorJsonUtility.ToJson(config);
        }
        static string ConfigWithoutReceipt(string json)
        {
            foreach(string name in new[]{"buildIdentity","sourceLockSHA256","sourceReceiptJSON"}){
                string pattern="\""+name+"\"\\s*:\\s*\"(?:\\\\.|[^\"\\\\])*\"";
                Need(System.Text.RegularExpressions.Regex.Matches(json,pattern).Count==1,"Missing/ambiguous provenance field: "+name);
                json=System.Text.RegularExpressions.Regex.Replace(json,pattern,"\""+name+"\":\"<owned-receipt>\"");
            }return json;
        }
        [Serializable] sealed class SourceObservedRow
        {public string path,expectedSHA256,actualSHA256,readError,contract;public bool exists,byteEqual,guardPassed;}
        [Serializable] sealed class SourceObservedAudit
        {public string phase,sourceLockSHA256;public SourceObservedRow[] rows;public bool allPhysicalBytesEqual,guardsEvaluated,allGuardsPassed;public string[] failures;}
        static BuildInputLock ExactInput(string sourceLock,string prepared,string phase)
        {
            var input=JsonUtility.FromJson<BuildInputLock>(File.ReadAllText(sourceLock));Need(input!=null&&input.project==Isolation&&input.finalCombinationAccepted&&input.sources!=null&&input.sources.Length>0,"Reviewed final source lock missing.");
            string player=Isolation+"/ProjectSettings/ProjectSettings.asset",global=Isolation+"/Assets/UniversalRenderPipelineGlobalSettings.asset";
            var rows=input.sources.Select(row=>{
                var observed=new SourceObservedRow{path=row.absolutePath,expectedSHA256=row.sha256,contract=Absolute(row.absolutePath)==player?"owned-PlayerSettings-finite6":Absolute(row.absolutePath)==global?"owned-Global-exact2":"strict-bytes"};
                try{observed.exists=File.Exists(row.absolutePath);if(observed.exists)observed.actualSHA256=Hash(row.absolutePath);observed.byteEqual=observed.exists&&observed.actualSHA256==observed.expectedSHA256;}catch(Exception e){observed.readError=e.ToString();}return observed;
            }).ToArray();
            string stamp=DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffff");var audit=new SourceObservedAudit{phase=phase,sourceLockSHA256=Hash(sourceLock),rows=rows,allPhysicalBytesEqual=rows.All(r=>r.byteEqual),guardsEvaluated=false};
            // Persist every actual input SHA before any content/semantic assertion.
            File.WriteAllText(Path.Combine(prepared,"source-hashes-"+phase+"-"+stamp+".json"),JsonUtility.ToJson(audit,true));
            var failures=new List<string>();foreach(var row in rows){if(row.contract=="strict-bytes"){row.guardPassed=row.byteEqual;if(!row.guardPassed)failures.Add("Exact final source drift: "+row.path);}else if(!row.exists||row.readError!=null)failures.Add("Owned source unavailable: "+row.path);}
            var setting=input.sources.Where(row=>Absolute(row.absolutePath)==player).ToArray();var gs=input.sources.Where(row=>Absolute(row.absolutePath)==global).ToArray();
            try{
                Need(setting.Length==1&&input.ownedPlayerSettings!=null,"Exactly one owned PlayerSettings row is required.");
                Need(input.sources.Count(row=>Absolute(row.absolutePath)==Absolute(input.ownedPlayerSettings.capturedBytesPath)&&row.sha256==input.ownedPlayerSettings.capturedSHA256)==1&&input.sources.Count(row=>Absolute(row.absolutePath)==Absolute(input.ownedPlayerSettings.nativePreparedPath)&&row.sha256==input.ownedPlayerSettings.nativePreparedSHA256)==1,"PlayerSettings proof files must themselves be exact locked inputs.");
                NBFXPlayerBuildPreparation.VerifyOwnedPlayerSettingsLock(input.ownedPlayerSettings,setting[0].sha256,prepared,phase);rows.Single(r=>r.contract=="owned-PlayerSettings-finite6").guardPassed=true;
            }catch(Exception e){failures.Add(e.ToString());}
            try{Need(gs.Length==1,"Exactly one owned GlobalSettings row is required.");NBFXPlayerBuildPreparation.VerifyOwnedGlobalLock(gs[0].sha256,prepared,phase);rows.Single(r=>r.contract=="owned-Global-exact2").guardPassed=true;}catch(Exception e){failures.Add(e.ToString());}
            audit.guardsEvaluated=true;audit.failures=failures.ToArray();audit.allGuardsPassed=failures.Count==0&&rows.All(r=>r.guardPassed);
            File.WriteAllText(Path.Combine(prepared,"source-contract-"+phase+"-"+stamp+".json"),JsonUtility.ToJson(audit,true));
            Need(audit.allGuardsPassed,"Exact source contract failed; complete actual hash audit retained: "+string.Join("; ",failures));return input;
        }

        public static string BindStagedInput(string stagedReceipt,string finalSourceLock)
        {
            NBFXPlayerBuildPreparation.Guard();var s=JsonUtility.FromJson<StagedReceipt>(File.ReadAllText(stagedReceipt));Need(s.project==Isolation,"Wrong staged project.");
            string output=Path.GetDirectoryName(stagedReceipt);Need(!File.Exists(Path.Combine(output,"bound-stage.json")),"Do not overwrite prior binding.");
            var adoption=Adoption(s.adoptionPath,s.adoptionSHA256);ExactFiles(s.staging,s.files);ExactMaterialMemory(adoption,output);
            var input=ExactInput(finalSourceLock,s.prepared,"bind-before");Need(input.identity==s.buildIdentity,"Prepared identity and final input lock differ.");
            var config=ExistingConfig(s.staging);string before=EditorJsonUtility.ToJson(config);Need(before==s.configJSON,"Config memory changed after stage inspection.");
            string file=Path.Combine(Isolation,s.staging,"PlayerConfig.asset");File.Copy(file,Path.Combine(output,"config-before-final-bind.bytes"),false);File.WriteAllText(Path.Combine(output,"config-before-final-bind.json"),before);
            config.buildIdentity=input.identity;config.sourceLockSHA256=Hash(finalSourceLock);config.sourceReceiptJSON=File.ReadAllText(finalSourceLock);
            Need(ConfigWithoutReceipt(before)==ConfigWithoutReceipt(EditorJsonUtility.ToJson(config)),"Only three provenance fields may change.");
            EditorUtility.SetDirty(config);AssetDatabase.SaveAssetIfDirty(config);ExactInput(finalSourceLock,s.prepared,"bind-after");
            var now=StagedFiles(s.staging);Need(now.Length==s.files.Length,"Stage file set changed during binding.");
            foreach(var f in now){var old=s.files.Single(x=>x.path==f.path);Need(f.path==s.staging+"/PlayerConfig.asset"||f.sha256==old.sha256,"Unexpected generated asset change during binding: "+f.path);}
            ExactMaterialMemory(adoption,output);s.files=now;s.sourceLock=Path.GetFullPath(finalSourceLock);s.sourceLockSHA256=Hash(finalSourceLock);s.configJSON=EditorJsonUtility.ToJson(config);
            string bound=Path.Combine(output,"bound-stage.json");File.WriteAllText(bound,JsonUtility.ToJson(s,true));return bound;
        }
        public static void BuildStaged(string boundReceipt,string newOutput)
        {
            NBFXPlayerBuildPreparation.Guard();var s=JsonUtility.FromJson<StagedReceipt>(File.ReadAllText(boundReceipt));Need(s.project==Isolation&&Hash(s.sourceLock)==s.sourceLockSHA256,"Bound source lock changed.");
            var input=ExactInput(s.sourceLock,s.prepared,"build-before");OwnOutput(newOutput);Need(!Directory.Exists(newOutput),"New actual Build output only.");
            ExactFiles(s.staging,s.files);var a=Adoption(s.adoptionPath,s.adoptionSHA256);ExactMaterialMemory(a,Path.GetDirectoryName(boundReceipt));var config=ExistingConfig(s.staging);Need(EditorJsonUtility.ToJson(config)==s.configJSON,"Bound config changed in memory.");
            Need(PlayerSettings.enableFrameTimingStats&&config.sourceLockSHA256==s.sourceLockSHA256&&config.buildIdentity==input.identity,"Final provenance/FrameTiming memory differs.");
            Need(s.scenes.SequenceEqual(StagedScenes.Select(n=>s.staging+"/"+n)),"Unexpected Build scenes.");
            string dispatch=Path.Combine(s.prepared,"actual-build-dispatched.json");Need(!File.Exists(dispatch),"Actual build already dispatched; observe, never resend.");
            Directory.CreateDirectory(newOutput);NBFXPlayerBuildPreparation.VerifyBuildBoundary(newOutput,s.staging);
            var receipt=new BuildReceipt{identity=input.identity,project=Isolation,unity=Application.unityVersion,sourceLockSHA256=s.sourceLockSHA256,scene=s.scenes[0],output=Absolute(newOutput),pipeline=AssetDatabase.GetAssetPath(config.pipeline),renderer=AssetDatabase.GetAssetPath(config.pipeline.rendererDataList[0]),
                retainedShaders=new[]{config.currentShader.name,config.graphShader.name,config.uberShader.name,config.colorBlitShader.name},materialStates=a.materials.Select(m=>m.path).ToArray(),sourcePaths=input.sources.Select(x=>x.absolutePath).ToArray(),sourceSHA256=input.sources.Select(x=>x.sha256).ToArray(),automaticFlipbookIdentities=config.automaticFlipbookCases.Select(c=>c.identity).ToArray(),expectedRuntimeCases=8,expectedRawCaptures=140,notRuntimeEvidence=true};
            NBFXPlayerBuildAudit.Begin(newOutput);string error=null;
            try{
                NBFXPlayerBuildAudit.RecordArtifacts(s.staging);ExactInput(s.sourceLock,s.prepared,"dispatch-before");ExactFiles(s.staging,s.files);
                File.WriteAllText(dispatch,"{\"utc\":\""+DateTime.UtcNow.ToString("o")+"\",\"stage\":\"immediately-before-BuildPipeline.BuildPlayer\"}");
                receipt.ranBuildPlayer=true;
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=s.scenes,locationPathName=Path.Combine(newOutput,"NBFXMeshValidation.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
                NBFXPlayerBuildAudit.Record(report);receipt.actualStandaloneWindows64=report.summary.platform==BuildTarget.StandaloneWindows64;receipt.buildResult=report.summary.result.ToString();receipt.totalErrors=(int)report.summary.totalErrors;receipt.totalWarnings=(int)report.summary.totalWarnings;
                receipt.sourceLockContract="strict-other-inputs+finite-owned-PlayerSettings-Global-v2";
                receipt.allInputBytesUnchangedAfterBuild=input.sources.All(f=>File.Exists(f.absolutePath)&&Hash(f.absolutePath)==f.sha256);
                receipt.nonOwnedInputsBytesUnchangedAfterBuild=input.sources.Where(f=>Absolute(f.absolutePath)!=Isolation+"/ProjectSettings/ProjectSettings.asset"&&Absolute(f.absolutePath)!=Isolation+"/Assets/UniversalRenderPipelineGlobalSettings.asset").All(f=>File.Exists(f.absolutePath)&&Hash(f.absolutePath)==f.sha256);
                ExactInput(s.sourceLock,s.prepared,"build-after");receipt.ownedPlayerSettingsAllowedAfterBuild=true;receipt.ownedGlobalSettingsAllowedAfterBuild=true;receipt.sourceLockStillMatchesAfterBuild=receipt.nonOwnedInputsBytesUnchangedAfterBuild&&receipt.ownedPlayerSettingsAllowedAfterBuild&&receipt.ownedGlobalSettingsAllowedAfterBuild;ExactFiles(s.staging,s.files);
                Need(report.summary.result==BuildResult.Succeeded&&report.summary.totalErrors==0&&receipt.actualStandaloneWindows64,"Actual BuildPlayer failed.");Need(receipt.sourceLockStillMatchesAfterBuild,"Input drift during actual Build; output unverified.");
            }catch(Exception e){error=e.ToString();throw;}
            finally{File.WriteAllText(Path.Combine(newOutput,"build-receipt.json"),JsonUtility.ToJson(receipt,true));NBFXPlayerBuildAudit.End(error);}
        }
    }
}
