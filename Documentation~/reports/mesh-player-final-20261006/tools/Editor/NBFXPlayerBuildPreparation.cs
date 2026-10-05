using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace NBFX.PlayerValidation.Editor
{
    // Explicit preparation/restoration around one final build, not an automatic editor hook.
    public static class NBFXPlayerBuildPreparation
    {
        const string Clone="D:/UnityProject/NBUnityProject/.utmp/NBFXMeshValidation-20261002";
        const string Temp="Assets/ResTemp/EditorTemp/";
        [Serializable] public sealed class FileState{public string path,sha256,backup;}
        [Serializable] sealed class Journal
        {
            public string project,identity,recoveryFolder,bootstrapPath,oldSourceLockEnv,oldBuildEnv,activeTarget,backend;
            public bool oldFrameTiming,prepared,sceneWasEmpty,oldPlayerSettingsDirty;
            public string nativePlayerYamlSHA256,originalPublicDefines;
            public bool defaultProfileCaptured,defaultGlobalWasDirty;
            public string defaultProfilePath,defaultProfileSHA,defaultProfileMetaSHA,defaultProfileBeforeFingerprint,defaultGlobalPath,defaultGlobalBeforeJSON,defaultGlobalPreparedJSON,defaultGlobalPreparedSHA,defaultEnsureAssembly,defaultProfileUtilsAssembly,defaultEnsureSource;
            public string[] graphicsAPIs;public FileState[] settings;
        }
        [Serializable] sealed class Dependency{public string assetPath,absolutePath;}
        [Serializable] sealed class Dependencies{public string pipeline,renderer,globalSettings;public bool actualFrameTimingStats;public Dependency[] dependencies;}
        [Serializable] sealed class RestoreReceipt{public bool settingsBytesRestored,frameTimingRestored,sceneRestored,environmentRestored,nativePlayerYamlExact,ownedPlayerDirtyRestored,defaultProfileReferenceRestored,defaultProfilePreserved,defaultGlobalMemoryExact,defaultGlobalMissingReferenceEquivalent,defaultGlobalDirtyRestored;public string[] changed;}
        static string Root=>Path.GetFullPath(Path.GetDirectoryName(Application.dataPath)).Replace('\\','/').TrimEnd('/');
        public static string SHA(string path){using(var h=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(h.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
        static void Need(bool b,string m){if(!b)throw new InvalidOperationException(m);}
        public static void Guard()
        {
            Need(Root==Clone&&!Application.isPlaying&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating,"Only idle isolated clone EditMode is allowed.");
            for(int i=0;i<SceneManager.sceneCount;i++){var s=SceneManager.GetSceneAt(i);Need(!s.isDirty&&(s.name+"/"+s.path).IndexOf("TAI",StringComparison.OrdinalIgnoreCase)<0,"Dirty/TAI scene blocks build/import/reload.");}
        }
        static void Folder(string path)
        {
            string current="Assets";foreach(string part in path.Substring(7).Split('/')){if(!AssetDatabase.IsValidFolder(current+"/"+part))AssetDatabase.CreateFolder(current,part);current+="/"+part;}
        }
        static string AbsoluteAsset(string asset)
        {
            if(asset.StartsWith("Packages/",StringComparison.Ordinal)){var p=UnityEditor.PackageManager.PackageInfo.FindForAssetPath(asset);if(p==null)return null;int split=asset.IndexOf('/',9);return split<0?p.resolvedPath:Path.Combine(p.resolvedPath,asset.Substring(split+1));}
            return Path.Combine(Root,asset);
        }
        static UnityEngine.Object OwnedPlayerSettings()
        {
            var method=typeof(PlayerSettings).GetMethod("GetSerializedObject",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
            Need(method!=null,"Reviewed PlayerSettings accessor missing.");
            var so=method.Invoke(null,null)as SerializedObject;
            Need(so!=null&&so.targetObject&&so.targetObject.GetType()==typeof(PlayerSettings),"Unexpected native global PlayerSettings target.");
            return so.targetObject;
        }
        static void GuardNoPlayerSettingsOverride()
        {
            var profile=UnityEditor.Build.Profile.BuildProfile.GetActiveBuildProfile();if(!profile)return;
            var property=profile.GetType().GetProperty("playerSettings",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);
            Need(property!=null&&!(property.GetValue(profile)as UnityEngine.Object),"Active PlayerSettings override is not owned by this experiment.");
        }
        static string NativePlayerYaml()
        {
            var method=typeof(PlayerSettings).GetMethod("SerializeAsYAMLString",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic,
                null,new[]{typeof(PlayerSettings)},null);
            Need(method!=null,"Reviewed dedicated read-only PlayerSettings serializer missing.");
            string text=(string)method.Invoke(null,new object[]{OwnedPlayerSettings()});
            Need(!string.IsNullOrEmpty(text),"Native PlayerSettings serializer returned no data.");return text;
        }
        static string NormalizeFrameTiming(string text)
        {
            const string pattern=@"(?m)^  enableFrameTimingStats: [01](?:\r?\n|$)";
            Need(System.Text.RegularExpressions.Regex.Matches(text,pattern).Count==1,"Unknown FrameTiming native YAML layout.");
            return System.Text.RegularExpressions.Regex.Replace(text,pattern,"  enableFrameTimingStats: <owned>\\n");
        }
        static string OriginalEnvironment(string output,string name)
        {
            string value=File.ReadAllText(Path.Combine(output,name+"-env-before.txt"));
            if(value=="NULL")return null;Need(value.StartsWith("VALUE",StringComparison.Ordinal),"Invalid environment receipt.");
            return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(value.Substring(5)));
        }
        static void SaveOriginalEnvironment(string output,string name,string value)
        {File.WriteAllText(Path.Combine(output,name+"-env-before.txt"),value==null?"NULL":"VALUE"+Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value)));}
        public static void SaveGeneratedAssets(string staging)
        {
            Need(staging.StartsWith(Temp+"NBFXPlayerValidation-",StringComparison.Ordinal),"Only newly created owned validation assets may be saved.");
            string root=Path.GetFullPath(Path.Combine(Root,staging));
            Need(root.StartsWith(Path.GetFullPath(Path.Combine(Root,Temp)),StringComparison.OrdinalIgnoreCase),"Owned staging escaped expected root.");
            foreach(string guid in AssetDatabase.FindAssets("",new[]{staging}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);
                if(AssetDatabase.IsValidFolder(path))continue;
                Need(path.StartsWith(staging+"/",StringComparison.Ordinal),"Unexpected asset outside owned staging.");
                var asset=AssetDatabase.LoadMainAssetAtPath(path);if(asset)AssetDatabase.SaveAssetIfDirty(asset);
            }
        }
        [Serializable] sealed class PipelineSwitch {public bool enableInBuilds;}
        [Serializable] sealed class BuildBoundary
        {public bool runtimeConfigExists,enableInBuilds,generatedConfigExists,generatedInfoExists;public string[] foreignDirtyModels;}
        public static void VerifyBuildBoundary(string output,string staging)
        {
            string config=Path.Combine(Root,"ProjectSettings/Packages/com.unity.pipeline/RuntimePipelineConfig.json");
            bool exists=File.Exists(config);
            string[] suffixes={".asset",".mat",".prefab",".unity",".vfx",".shadergraph",".shadersubgraph"};
            // Shader/MonoScript/builtin cache dirtiness is not user model editing.
            var dirty=Resources.FindObjectsOfTypeAll<UnityEngine.Object>().Where(o=>o&&!(o is Shader)&&!(o is MonoScript)&&EditorUtility.IsDirty(o))
                .Select(AssetDatabase.GetAssetPath).Where(p=>!string.IsNullOrEmpty(p)&&(p.StartsWith("Assets/",StringComparison.Ordinal)||p.StartsWith("Packages/",StringComparison.Ordinal))&&!p.StartsWith(staging+"/",StringComparison.Ordinal)&&suffixes.Contains(Path.GetExtension(p).ToLowerInvariant())).Distinct().ToArray();
            var record=new BuildBoundary{runtimeConfigExists=exists,enableInBuilds=exists&&JsonUtility.FromJson<PipelineSwitch>(File.ReadAllText(config)).enableInBuilds,
                generatedConfigExists=File.Exists(Path.Combine(Root,"Assets/Settings/Pipeline/Resources/RuntimePipelineConfig.asset")),
                generatedInfoExists=File.Exists(Path.Combine(Root,"Assets/Settings/Pipeline/Resources/RuntimePipelineBuildInfo.asset")),foreignDirtyModels=dirty};
            File.WriteAllText(Path.Combine(output,"native-build-boundary.json"),JsonUtility.ToJson(record,true));
            Need(!record.generatedConfigExists&&!record.generatedInfoExists,"Pipeline callback would purge pre-existing generated files; refuse that unowned deletion.");
            Need(!record.runtimeConfigExists,"This run requires the observed absent authored Pipeline config, not an assumed disabled value.");
            // Load() returns null with this exact absent config; the reviewed Pipeline
            // processor returns before its two global SaveAssets calls. Foreign model
            // dirtiness is recorded for Root's independent before/after check, not cleared.

        }
        public static string Prepare(string output,string identity)
        {
            Guard();Need(identity.All(c=>char.IsLetterOrDigit(c)||c=='-'),"Safe new identity required.");Need(!File.Exists(Path.Combine(output,"build-before.json")),"Preserve old preparation.");
            Need(SceneManager.sceneCount==1&&string.IsNullOrEmpty(SceneManager.GetActiveScene().path)&&SceneManager.GetActiveScene().GetRootGameObjects().Length==0,"Start in one clean dedicated EmptyScene; never discard user content.");
            GuardNoPlayerSettingsOverride();Need(!EditorUtility.IsDirty(OwnedPlayerSettings()),"Owned PlayerSettings already dirty; preserve prior setting edits.");
            Need(EditorUserBuildSettings.activeBuildTarget==BuildTarget.StandaloneWindows64,"Use the existing Windows64 target; do not silently switch the platform.");
            Need(BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone,BuildTarget.StandaloneWindows64),"Windows64 support is not installed.");
            Need(!GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>().enableRenderCompatibilityMode,"Restore Compatibility experiment before final Player build.");
            var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;Need(pipeline,"Actual URP pipeline missing.");
            string pipelinePath=AssetDatabase.GetAssetPath(pipeline),rendererPath=AssetDatabase.GetAssetPath(pipeline.rendererDataList[0]);
            var global=UnityEditor.Rendering.EditorGraphicsSettings.GetRenderPipelineGlobalSettingsAsset<UniversalRenderPipeline>();string globalPath=AssetDatabase.GetAssetPath(global);
            var j=new Journal{project=Root,identity=identity,recoveryFolder=Temp+"NBFXPlayerRecovery-"+identity,oldFrameTiming=PlayerSettings.enableFrameTimingStats,oldSourceLockEnv=Environment.GetEnvironmentVariable("NBFX_PLAYER_SOURCE_LOCK"),oldBuildEnv=Environment.GetEnvironmentVariable("NBFX_PLAYER_BUILD_DIR"),activeTarget=EditorUserBuildSettings.activeBuildTarget.ToString(),backend=PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone).ToString(),graphicsAPIs=PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64).Select(a=>a.ToString()).ToArray(),sceneWasEmpty=true,oldPlayerSettingsDirty=EditorUtility.IsDirty(OwnedPlayerSettings()),originalPublicDefines=PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone)};
            CaptureDefaultProfile(j,global);
            Need(!AssetDatabase.IsValidFolder(j.recoveryFolder),"Recovery path already exists.");Need(j.graphicsAPIs.Contains("Direct3D11"),"Build graphics API list must already include the tested D3D11 API.");
            Directory.CreateDirectory(output);
            var paths=Directory.GetFiles(Path.Combine(Root,"ProjectSettings"),"*.asset").Select(p=>p.Substring(Root.Length+1).Replace('\\','/')).Concat(new[]{pipelinePath,rendererPath,globalPath}).Distinct().ToArray();
            j.settings=paths.Select((p,i)=>new FileState{path=p,sha256=SHA(Path.Combine(Root,p)),backup="before-"+i+".bytes"}).ToArray();
            foreach(var f in j.settings)File.Copy(Path.Combine(Root,f.path),Path.Combine(output,f.backup));
            File.WriteAllText(Path.Combine(output,"native-player-before.yaml"),NativePlayerYaml(),new System.Text.UTF8Encoding(false));
            j.nativePlayerYamlSHA256=SHA(Path.Combine(output,"native-player-before.yaml"));
            SaveOriginalEnvironment(output,"source-lock",j.oldSourceLockEnv);SaveOriginalEnvironment(output,"build",j.oldBuildEnv);
            File.WriteAllText(Path.Combine(output,"build-before.json"),JsonUtility.ToJson(j,true)); // before first Unity mutation
            PrepareDefaultProfile(output,j,global);
            Folder(j.recoveryFolder);j.bootstrapPath=j.recoveryFolder+"/EmptyBootstrap.unity";
            Need(EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),j.bootstrapPath),"Could not preserve empty bootstrap for additive build scene.");
            PlayerSettings.enableFrameTimingStats=true;
            string preparedYaml=NativePlayerYaml();File.WriteAllText(Path.Combine(output,"native-player-prepared.yaml"),preparedYaml,new System.Text.UTF8Encoding(false));
            Need(NormalizeFrameTiming(preparedYaml)==NormalizeFrameTiming(File.ReadAllText(Path.Combine(output,"native-player-before.yaml"))),
                "Public FrameTiming setter changed unrelated native settings; no explicit save or dirty clear is allowed.");
            Need(PlayerSettings.enableFrameTimingStats,"Public FrameTiming enable did not apply in memory.");j.prepared=true;
            File.WriteAllText(Path.Combine(output,"build-prepared.json"),JsonUtility.ToJson(j,true));
            var roots=new List<string>{pipelinePath,rendererPath,globalPath,"Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader","Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph"};
            for(int i=0;i<QualitySettings.names.Length;i++){var p=QualitySettings.GetRenderPipelineAssetAt(i);if(p)roots.Add(AssetDatabase.GetAssetPath(p));}
            var deps=AssetDatabase.GetDependencies(roots.ToArray(),true).Select(a=>new Dependency{assetPath=a,absolutePath=AbsoluteAsset(a)}).Where(d=>d.absolutePath!=null&&File.Exists(d.absolutePath)).ToArray();
            File.WriteAllText(Path.Combine(output,"dependencies.json"),JsonUtility.ToJson(new Dependencies{pipeline=pipelinePath,renderer=rendererPath,globalSettings=globalPath,actualFrameTimingStats=PlayerSettings.enableFrameTimingStats,dependencies=deps},true));
            return "FrameTiming prepared. Complete StageExisting and its exact serialization guard before capturing the final source lock.";
        }
        public static string SetBuildInputs(string prepared,string sourceLock,string buildOutput)
        {
            Guard();Need(File.Exists(Path.Combine(prepared,"build-prepared.json")),"Prepare first.");Need(File.Exists(sourceLock)&&!Directory.Exists(buildOutput),"Need an actual source lock and new output directory.");
            Environment.SetEnvironmentVariable("NBFX_PLAYER_SOURCE_LOCK",sourceLock);Environment.SetEnvironmentVariable("NBFX_PLAYER_BUILD_DIR",buildOutput);return "Explicit build inputs set; no BuildPlayer dispatched.";
        }
        public static string BuildOnce(string prepared)
        {
            Guard();Need(File.Exists(Path.Combine(prepared,"build-prepared.json")),"Prepare first.");
            string output=Environment.GetEnvironmentVariable("NBFX_PLAYER_BUILD_DIR");Need(!string.IsNullOrEmpty(output)&&!Directory.Exists(output),"Explicit new build output required.");
            Need(!File.Exists(Path.Combine(prepared,"build-dispatched.json")),"A dispatched/timeout build must be observed, not repeated.");
            File.WriteAllText(Path.Combine(prepared,"build-dispatched.json"),"{\"utc\":\""+DateTime.UtcNow.ToString("o")+"\"}");NBFXPlayerBuildAudit.Begin(output);string error=null;
            try{NBFXMeshPlayerBuilder.Build();return "Actual BuildPlayer returned. Inspect build-receipt and actual Player results separately.";}
            catch(Exception e){error=e.ToString();throw;}
            finally{NBFXPlayerBuildAudit.End(error);Restore(prepared);}
        }
        public static string Restore(string output)
        {
            Guard();var p=Path.Combine(output,"build-prepared.json");if(!File.Exists(p))p=Path.Combine(output,"build-before.json");var j=JsonUtility.FromJson<Journal>(File.ReadAllText(p));Need(j.project==Root,"Wrong journal.");
            File.WriteAllText(Path.Combine(output,"build-cleanup-started.json"),"{\"started\":true}");
            Need(SceneManager.sceneCount==1,"Close only the builder-owned additive scenes before restoration.");var active=SceneManager.GetActiveScene();
            Need(!active.isDirty&&active.GetRootGameObjects().Length==0&&(active.path==j.bootstrapPath||string.IsNullOrEmpty(active.path)),"Recovery scene contains unexpected content.");
            GuardNoPlayerSettingsOverride();PlayerSettings.enableFrameTimingStats=j.oldFrameTiming;RestoreDefaultProfileMemory(j);
            var changed=new List<string>();var writes=new List<Tuple<string,byte[]>>();
            foreach(var f in j.settings)
            {
                string file=Path.Combine(Root,f.path);if(SHA(file)==f.sha256)continue;
                var before=File.ReadAllBytes(Path.Combine(output,f.backup));Need(SHA(Path.Combine(output,f.backup))==f.sha256,"Backup changed.");
                // The profile GUID was explicitly preprocessed BEFORE final locking.
                // It remains byte-strict during Build; only this declared owned
                // preparation change is undone at the end.
                if(j.defaultProfileCaptured&&f.path==j.defaultGlobalPath){Need(PreparedGlobalHashes(output,j).Contains(SHA(file)),"Unexpected GlobalSettings drift beyond the exact profile GUID and original15-RID clear representations.");writes.Add(Tuple.Create(file,before));continue;}
                // Only the owned frame-timing serialized field may differ after public API restore.
                string a=System.Text.Encoding.UTF8.GetString(before),b=File.ReadAllText(file);
                if(f.path=="ProjectSettings/ProjectSettings.asset")
                {
                    const string pattern=@"(?m)^(\s*enableFrameTimingStats:) [01]\s*$";
                    Need(System.Text.RegularExpressions.Regex.Matches(a,pattern).Count==1&&System.Text.RegularExpressions.Regex.Matches(b,pattern).Count==1,"Unknown FrameTiming serialization; no blind restoration.");
                    a=System.Text.RegularExpressions.Regex.Replace(a,pattern,"$1 <owned>");b=System.Text.RegularExpressions.Regex.Replace(b,pattern,"$1 <owned>");
                    Need(j.originalPublicDefines=="","Known normalization is limited to the original empty defines case.");
                    string nativeBefore=File.ReadAllText(Path.Combine(output,"native-player-before.yaml"));Need(SHA(Path.Combine(output,"native-player-before.yaml"))==j.nativePlayerYamlSHA256,"Original native proof changed.");
                    a=KnownPlayerLayout(a,nativeBefore);b=KnownPlayerLayout(b,nativeBefore);
                }
                if(a!=b)changed.Add(f.path);else writes.Add(Tuple.Create(file,before));
            }
            Need(changed.Count==0,"Unexpected build input changes retained: "+string.Join(",",changed));foreach(var w in writes)File.WriteAllBytes(w.Item1,w.Item2);
            FinishDefaultProfileRestore(j);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            // Recovery contains only our saved empty scene. Keep generated player materials/scenes
            // for review; their later exact-manifest cleanup is a separate explicit operation.
            if(AssetDatabase.IsValidFolder(j.recoveryFolder)){var allowed=Directory.GetFiles(Path.Combine(Root,j.recoveryFolder),"*",SearchOption.AllDirectories);Need(allowed.All(f=>Path.GetFileName(f)=="EmptyBootstrap.unity"||Path.GetFileName(f)=="EmptyBootstrap.unity.meta"),"Unknown recovery content; refuse deletion.");AssetDatabase.DeleteAsset(j.recoveryFolder);}
            string originalNative=File.ReadAllText(Path.Combine(output,"native-player-before.yaml"));Need(SHA(Path.Combine(output,"native-player-before.yaml"))==j.nativePlayerYamlSHA256,"Original native YAML proof changed.");
            string currentNative=NativePlayerYaml();File.WriteAllText(Path.Combine(output,"native-player-restored.yaml"),currentNative,new System.Text.UTF8Encoding(false));
            bool nativeExact=currentNative==originalNative;
            var playerFile=j.settings.Single(f=>f.path=="ProjectSettings/ProjectSettings.asset");
            Need(nativeExact&&SHA(Path.Combine(Root,playerFile.path))==playerFile.sha256&&PlayerSettings.enableFrameTimingStats==j.oldFrameTiming&&
                PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone)==j.originalPublicDefines,
                "Restored full native YAML/public values/disk bytes differ; do not clear owned dirty.");
            if(!j.oldPlayerSettingsDirty&&EditorUtility.IsDirty(OwnedPlayerSettings()))EditorUtility.ClearDirty(OwnedPlayerSettings());
            Environment.SetEnvironmentVariable("NBFX_PLAYER_SOURCE_LOCK",OriginalEnvironment(output,"source-lock"));Environment.SetEnvironmentVariable("NBFX_PLAYER_BUILD_DIR",OriginalEnvironment(output,"build"));
            var r=new RestoreReceipt{settingsBytesRestored=j.settings.All(f=>SHA(Path.Combine(Root,f.path))==f.sha256),frameTimingRestored=PlayerSettings.enableFrameTimingStats==j.oldFrameTiming,sceneRestored=SceneManager.sceneCount==1&&string.IsNullOrEmpty(SceneManager.GetActiveScene().path)&&SceneManager.GetActiveScene().GetRootGameObjects().Length==0&&!SceneManager.GetActiveScene().isDirty,environmentRestored=Environment.GetEnvironmentVariable("NBFX_PLAYER_SOURCE_LOCK")==OriginalEnvironment(output,"source-lock")&&Environment.GetEnvironmentVariable("NBFX_PLAYER_BUILD_DIR")==OriginalEnvironment(output,"build"),nativePlayerYamlExact=nativeExact,ownedPlayerDirtyRestored=EditorUtility.IsDirty(OwnedPlayerSettings())==j.oldPlayerSettingsDirty,defaultGlobalMemoryExact=!j.defaultProfileCaptured||EditorJsonUtility.ToJson(AssetDatabase.LoadMainAssetAtPath(j.defaultGlobalPath))==j.defaultGlobalBeforeJSON,defaultGlobalMissingReferenceEquivalent=j.defaultProfileCaptured&&EditorJsonUtility.ToJson(AssetDatabase.LoadMainAssetAtPath(j.defaultGlobalPath))!=j.defaultGlobalBeforeJSON&&GlobalMemoryRestored(j,AssetDatabase.LoadMainAssetAtPath(j.defaultGlobalPath)),defaultGlobalDirtyRestored=!j.defaultProfileCaptured||EditorUtility.IsDirty(AssetDatabase.LoadMainAssetAtPath(j.defaultGlobalPath))==j.defaultGlobalWasDirty,defaultProfileReferenceRestored=!j.defaultProfileCaptured||GraphicsSettings.GetRenderPipelineSettings<URPDefaultVolumeProfileSettings>().volumeProfile==null,defaultProfilePreserved=!j.defaultProfileCaptured||(ProfileFingerprint(j.defaultProfilePath)==j.defaultProfileBeforeFingerprint&&SHA(Path.Combine(Root,j.defaultProfilePath))==j.defaultProfileSHA&&SHA(Path.Combine(Root,j.defaultProfilePath+".meta"))==j.defaultProfileMetaSHA),changed=changed.ToArray()};
            File.WriteAllText(Path.Combine(output,"build-cleanup.json"),JsonUtility.ToJson(r,true));Need(r.settingsBytesRestored&&r.frameTimingRestored&&r.sceneRestored&&r.environmentRestored&&r.nativePlayerYamlExact&&r.ownedPlayerDirtyRestored&&r.defaultProfileReferenceRestored&&r.defaultProfilePreserved&&(r.defaultGlobalMemoryExact||r.defaultGlobalMissingReferenceEquivalent)&&r.defaultGlobalDirtyRestored,"Actual build environment not restored.");return JsonUtility.ToJson(r,true);
        }
        static string KnownPlayerLayout(string text,string nativeBefore)
        {
            // Exact Unity6000.3 observed serialization only. Never normalize an
            // arbitrary settings diff or serialize a native object over disk.
            string nl=text.Contains("\r\n")?"\r\n":"\n";
            string[] lines={"  adjustIOSFPSUsingThermalState: 1","  thermalStateSeriousIOSFPS: 30","  thermalStateCriticalIOSFPS: 15"};
            foreach(var line in lines)Need(nativeBefore.Replace("\r\n","\n").Split('\n').Count(v=>v==line)==1,"Original native YAML must prove the exact existing thermal default: "+line);
            string[] keys={"adjustIOSFPSUsingThermalState:","thermalStateSeriousIOSFPS:","thermalStateCriticalIOSFPS:"};
            if(keys.All(k=>!text.Contains(k))){string anchor="  preserveFramebufferAlpha: 0"+nl;Need(text.Split(new[]{anchor},StringSplitOptions.None).Length==2,"Unknown thermal insertion anchor.");text=text.Replace(anchor,anchor+string.Join(nl,lines)+nl);}
            else foreach(var line in lines)Need(text.Replace("\r\n","\n").Split('\n').Count(v=>v==line)==1,"Changed thermal values are not known normalization.");
            string empty="  scriptingDefineSymbols: {}"+nl,expanded="  scriptingDefineSymbols:"+nl+"    Standalone: "+nl;
            if(text.Contains(empty)){Need(text.Split(new[]{empty},StringSplitOptions.None).Length==2,"Duplicate defines map.");text=text.Replace(empty,expanded);}
            Need(text.Split(new[]{expanded},StringSplitOptions.None).Length==2,"Only original empty Standalone representation is owned here.");
            return text;
        }
        static void StagedSettingsReady(string prepared,Journal j)
        {
            Need(PlayerSettings.enableFrameTimingStats&&PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone)==j.originalPublicDefines&&j.originalPublicDefines=="","Expected prepared FrameTiming and original empty defines.");
            string nativeBefore=File.ReadAllText(Path.Combine(prepared,"native-player-before.yaml"));Need(SHA(Path.Combine(prepared,"native-player-before.yaml"))==j.nativePlayerYamlSHA256,"Native before proof changed.");
            string currentNative=NativePlayerYaml();Need(currentNative==File.ReadAllText(Path.Combine(prepared,"native-player-prepared.yaml")),"Full native settings changed since public FrameTiming preparation.");
            File.WriteAllText(Path.Combine(prepared,"native-player-staged.yaml"),currentNative,new System.Text.UTF8Encoding(false));
            foreach(var f in j.settings){
                string file=Path.Combine(Root,f.path),backup=Path.Combine(prepared,f.backup);Need(SHA(backup)==f.sha256,"Backup changed.");
                if(f.path!="ProjectSettings/ProjectSettings.asset"){Need(SHA(file)==(j.defaultProfileCaptured&&f.path==j.defaultGlobalPath?j.defaultGlobalPreparedSHA:f.sha256),"Unexpected staging settings change: "+f.path);continue;}
                string[] states=FinitePlayerDiskTexts(File.ReadAllText(backup),currentNative);string[] allowed=states.Select(TextSHA).ToArray();string actual=SHA(file);int state=Array.IndexOf(allowed,actual);
                var proof=new OwnedPlayerSettingsObservation{phase="stage",path=file,capturedSHA256=f.sha256,actualSHA256=actual,representation=state<0?"unknown":new[]{"original","thermal+expanded-empty-Standalone","thermal+empty-dictionary"}[state/2]+"-frame"+(state%2),nativePreparedSHA256=SHA(Path.Combine(prepared,"native-player-prepared.yaml")),diskBytesUnchanged=actual==f.sha256,allowedDiskState=state>=0,nativeFullYamlExact=true,publicFrameTimingTrue=true};
                File.WriteAllText(Path.Combine(prepared,"player-settings-staged-"+DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffff")+".json"),JsonUtility.ToJson(proof,true));
                Need(state>=0,"Owned PlayerSettings disk contains an unrecognized field change; full native prepared equality alone is insufficient.");
            }
        }
        public static string StageExisting(string prepared,string adoptionPath,string adoptionSHA)
        {
            Guard();var j=JsonUtility.FromJson<Journal>(File.ReadAllText(Path.Combine(prepared,"build-prepared.json")));Need(j.project==Root&&j.prepared,"Existing new preparation required.");
            Need(!File.Exists(Path.Combine(prepared,"staged-ready.json"))&&!File.Exists(Path.Combine(prepared,"actual-build-dispatched.json")),"Do not overwrite a stage or dispatched actual build.");
            try{
                string config=NBFXMeshPlayerBuilder.ValidateExistingStage(adoptionPath,adoptionSHA,prepared);
                var a=JsonUtility.FromJson<NBFXMeshPlayerBuilder.AdoptedStage>(File.ReadAllText(adoptionPath));
                Need(SceneManager.sceneCount==1&&SceneManager.GetActiveScene().path==j.bootstrapPath&&SceneManager.GetActiveScene().GetRootGameObjects().Length==0,"Only the owned empty recovery scene may be saved.");
                // Original staged scene/material bytes stay exact. Do not issue
                // another Save to force Unity's asynchronous native settings flush.
                StagedSettingsReady(prepared,j);
                NBFXMeshPlayerBuilder.ValidateExistingStage(adoptionPath,adoptionSHA,prepared);
                var s=new NBFXMeshPlayerBuilder.StagedReceipt{project=Root,prepared=Path.GetFullPath(prepared),staging=a.staging,adoptionPath=Path.GetFullPath(adoptionPath),adoptionSHA256=adoptionSHA,buildIdentity=j.identity,configJSON=config,scenes=new[]{"PlayerScene.unity","NBFXPerformance.unity","NBFXRuntimeProjection.unity"}.Select(n=>a.staging+"/"+n).ToArray(),files=NBFXMeshPlayerBuilder.StagedFiles(a.staging)};
                string result=Path.Combine(prepared,"staged-ready.json");File.WriteAllText(result,JsonUtility.ToJson(s,true));return result;
            }catch(Exception e){File.WriteAllText(Path.Combine(prepared,"stage-failure-"+DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffff")+".txt"),e.ToString());Restore(prepared);throw;}
        }
        public static string BuildStagedOnce(string prepared,string boundReceipt)
        {
            Guard();var s=JsonUtility.FromJson<NBFXMeshPlayerBuilder.StagedReceipt>(File.ReadAllText(boundReceipt));Need(Path.GetFullPath(prepared)==s.prepared&&s.project==Root,"Bound stage belongs to another preparation.");
            var j=JsonUtility.FromJson<Journal>(File.ReadAllText(Path.Combine(prepared,"build-prepared.json")));Need(j.identity==s.buildIdentity&&j.project==Root,"Prepared identity changed.");
            Need(!File.Exists(Path.Combine(prepared,"actual-build-dispatched.json")),"Actual build already dispatched; only observe its outcome.");
            string output=Environment.GetEnvironmentVariable("NBFX_PLAYER_BUILD_DIR");Need(!string.IsNullOrEmpty(output)&&!Directory.Exists(output)&&Environment.GetEnvironmentVariable("NBFX_PLAYER_SOURCE_LOCK")==s.sourceLock,"Exact bound source lock and NEW output required.");
            Need(EditorUserBuildSettings.activeBuildTarget.ToString()==j.activeTarget&&PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone).ToString()==j.backend&&PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64).Select(v=>v.ToString()).SequenceEqual(j.graphicsAPIs),"Target/backend/API changed after preparation.");
            try{StagedSettingsReady(prepared,j);NBFXMeshPlayerBuilder.BuildStaged(boundReceipt,output);return "Single actual BuildPlayer returned; inspect native build receipt before any Player claim.";}
            finally{Restore(prepared);}
        }
        [Serializable] public sealed class OwnedPlayerSettingsLock
        {public string path,capturedBytesPath,capturedSHA256,nativePreparedPath,nativePreparedSHA256;public string[] allowedSHA256;}
        [Serializable] sealed class OwnedPlayerSettingsObservation
        {public string phase,path,capturedSHA256,actualSHA256,representation,nativePreparedSHA256;public bool diskBytesUnchanged,allowedDiskState,nativeFullYamlExact,publicFrameTimingTrue;}
        static string TextSHA(string text){using(var h=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(h.ComputeHash(new System.Text.UTF8Encoding(false,true).GetBytes(text))).Replace("-","").ToLowerInvariant();}
        static string[] FinitePlayerDiskTexts(string captured,string nativePrepared)
        {
            string canonical=KnownPlayerLayout(captured,nativePrepared),nl=canonical.Contains("\r\n")?"\r\n":"\n";
            string thermal="  adjustIOSFPSUsingThermalState: 1"+nl+"  thermalStateSeriousIOSFPS: 30"+nl+"  thermalStateCriticalIOSFPS: 15"+nl;
            Need(canonical.Split(new[]{thermal},StringSplitOptions.None).Length==2,"Unknown thermal block; no broad normalization.");
            string legacy=canonical.Replace(thermal,"").Replace("  scriptingDefineSymbols:"+nl+"    Standalone: "+nl,"  scriptingDefineSymbols: {}"+nl);
            string thermalOnly=canonical.Replace("  scriptingDefineSymbols:"+nl+"    Standalone: "+nl,"  scriptingDefineSymbols: {}"+nl);
            Need(captured==legacy||captured==canonical||captured==thermalOnly,"Captured disk must be exactly one of three observed representations.");
            const string frame=@"(?m)^  enableFrameTimingStats: [01](?=\r?$)";Need(System.Text.RegularExpressions.Regex.Matches(canonical,frame).Count==1,"Unknown FrameTiming field.");
            return new[]{legacy,canonical,thermalOnly}.SelectMany(layout=>new[]{0,1}.Select(v=>System.Text.RegularExpressions.Regex.Replace(layout,frame,"  enableFrameTimingStats: "+v))).ToArray();
        }
        public static void VerifyOwnedPlayerSettingsLock(OwnedPlayerSettingsLock owned,string capturedSourceSHA,string prepared,string phase)
        {
            Need(owned!=null&&owned.path==Root+"/ProjectSettings/ProjectSettings.asset"&&owned.capturedSHA256==capturedSourceSHA,"Only exact owned global PlayerSettings may have finite disk states.");
            Need(SHA(owned.capturedBytesPath)==owned.capturedSHA256&&Path.GetFullPath(owned.nativePreparedPath)==Path.GetFullPath(Path.Combine(prepared,"native-player-prepared.yaml"))&&SHA(owned.nativePreparedPath)==owned.nativePreparedSHA256,"Locked captured disk/native preparation proof changed.");
            string native=File.ReadAllText(owned.nativePreparedPath),captured=File.ReadAllText(owned.capturedBytesPath);
            var journal=JsonUtility.FromJson<Journal>(File.ReadAllText(Path.Combine(prepared,"build-prepared.json")));Need(journal.project==Root,"Wrong original preparation.");
            var original=journal.settings.Single(f=>f.path=="ProjectSettings/ProjectSettings.asset");string backup=Path.Combine(prepared,original.backup);Need(SHA(backup)==original.sha256,"Original settings backup changed.");
            Need(FinitePlayerDiskTexts(File.ReadAllText(backup),native).Contains(captured),"Captured baseline itself drifted outside original finite settings states.");
            string[] states=FinitePlayerDiskTexts(captured,native);
            string[] hashes=states.Select(TextSHA).ToArray();Need(owned.allowedSHA256!=null&&owned.allowedSHA256.SequenceEqual(hashes),"Allowed SHA set must be derived exactly from locked captured bytes.");
            string currentSHA=SHA(owned.path);int state=Array.IndexOf(hashes,currentSHA);bool nativeExact=NativePlayerYaml()==native;
            var observation=new OwnedPlayerSettingsObservation{phase=phase,path=owned.path,capturedSHA256=owned.capturedSHA256,actualSHA256=currentSHA,representation=state<0?"unknown":new[]{"original","thermal+expanded-empty-Standalone","thermal+empty-dictionary"}[state/2]+"-frame"+(state%2),nativePreparedSHA256=owned.nativePreparedSHA256,diskBytesUnchanged=currentSHA==owned.capturedSHA256,allowedDiskState=state>=0,nativeFullYamlExact=nativeExact,publicFrameTimingTrue=PlayerSettings.enableFrameTimingStats};
            File.WriteAllText(Path.Combine(prepared,"owned-settings-"+phase+"-"+DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffff")+".json"),JsonUtility.ToJson(observation,true));
            Need(state>=0&&nativeExact&&PlayerSettings.enableFrameTimingStats,"Owned disk is outside finite allowed states or complete actual native YAML differs from prepared.");
        }
        static Type LoadedType(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name,false)).Single(t=>t!=null);
        static string ProfileFingerprint(string path)
        {
            return string.Join("\n",AssetDatabase.LoadAllAssetsAtPath(path).Where(o=>o).Select(o=>{AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o,out string guid,out long id);return id+"|"+o.GetType().FullName+"|dirty="+EditorUtility.IsDirty(o)+"|"+EditorJsonUtility.ToJson(o);}).OrderBy(s=>s,StringComparer.Ordinal));
        }
        static void CaptureDefaultProfile(Journal j,UnityEngine.Object global)
        {
            var setting=GraphicsSettings.GetRenderPipelineSettings<URPDefaultVolumeProfileSettings>();Need(setting!=null&&setting.volumeProfile==null,"This scoped repair only owns the proven missing default profile reference.");
            Need(AssetDatabase.GUIDToAssetPath("931df9a20dffe450db70d0db260a7f9c")=="","Original missing GUID now resolves; review environment instead of overwriting it.");
            j.defaultProfilePath="Assets/DefaultVolumeProfile.asset";j.defaultGlobalPath=AssetDatabase.GetAssetPath(global);j.defaultGlobalBeforeJSON=EditorJsonUtility.ToJson(global);j.defaultGlobalWasDirty=EditorUtility.IsDirty(global);
            Need(!j.defaultGlobalWasDirty,"Unowned dirty GlobalSettings must not be saved.");
            Need(SHA(Path.Combine(Root,j.defaultGlobalPath))=="720fdbd129c1b423a4e3e82fb5f19ee157a2bbccdc0c5ac7c9410bdcede0931d","Reviewed missing-profile GlobalSettings baseline changed.");
            Need(AssetDatabase.AssetPathToGUID(j.defaultProfilePath)=="cefc1705ccf7a83419f2d7a799335386","Only the preserved build3 profile is adopted; never create/overwrite it.");
            j.defaultProfileSHA=SHA(Path.Combine(Root,j.defaultProfilePath));j.defaultProfileMetaSHA=SHA(Path.Combine(Root,j.defaultProfilePath+".meta"));
            Need(j.defaultProfileSHA=="170d25477980499edfd262b112d718ea70d3828f3ab03a8b92d150430995758e"&&j.defaultProfileMetaSHA=="7abf3d48c60b403ddd7b31cd01217588fbcf68c3c29338accaefba758c30c413","Preserved profile bytes changed; review before adoption.");
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(j.defaultProfilePath);Need(profile&&profile.components.All(c=>c&&c.active&&!c.GetType().IsDefined(typeof(ObsoleteAttribute),false)&&c.parameters.All(p=>p.overrideState)),"Preserved profile is not already complete; no automatic rewrite permitted.");
            var utils=LoadedType("UnityEditor.Rendering.VolumeProfileUtils");var missing=utils.GetMethod("GetTypesMissingFromDefaultProfile",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);Need(missing!=null&&((System.Collections.ICollection)missing.Invoke(null,new object[]{profile})).Count==0,"Profile lacks official default components.");
            j.defaultProfileBeforeFingerprint=ProfileFingerprint(j.defaultProfilePath);j.defaultProfileCaptured=true;
            j.defaultEnsureAssembly=LoadedType("UnityEditor.Rendering.Universal.URPPreprocessBuild").Assembly.Location;j.defaultProfileUtilsAssembly=utils.Assembly.Location;
            j.defaultEnsureSource=Path.Combine(UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.render-pipelines.universal/Editor/BuildProcessors/URPPreprocessBuild.cs").resolvedPath,"Editor/BuildProcessors/URPPreprocessBuild.cs");
            string original=File.ReadAllText(Path.Combine(Root,j.defaultGlobalPath));Need(original.Split(new[]{"931df9a20dffe450db70d0db260a7f9c"},StringSplitOptions.None).Length==2,"Original profile GUID must occur exactly once.");
            j.defaultGlobalPreparedSHA=TextSHA(original.Replace("931df9a20dffe450db70d0db260a7f9c","cefc1705ccf7a83419f2d7a799335386"));
        }
        static void PrepareDefaultProfile(string output,Journal j,UnityEngine.Object global)
        {
            var setting=GraphicsSettings.GetRenderPipelineSettings<URPDefaultVolumeProfileSettings>();var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(j.defaultProfilePath);
            // Bind existing valid input first, so official Ensure never enters its
            // null/CreateAsset branch and never overwrites the preserved asset.
            setting.volumeProfile=profile;
            var method=LoadedType("UnityEditor.Rendering.Universal.URPPreprocessBuild").GetMethod("EnsureVolumeProfile",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);Need(method!=null,"Official EnsureVolumeProfile method missing.");method.Invoke(null,null);
            Need(setting.volumeProfile==profile&&ProfileFingerprint(j.defaultProfilePath)==j.defaultProfileBeforeFingerprint,"Official preprocessing changed preserved profile content/dirty state.");
            Need(SHA(Path.Combine(Root,j.defaultProfilePath))==j.defaultProfileSHA&&SHA(Path.Combine(Root,j.defaultProfilePath+".meta"))==j.defaultProfileMetaSHA,"Profile bytes must remain unchanged.");
            EditorUtility.SetDirty(global);AssetDatabase.SaveAssetIfDirty(global);
            Need(PreparedGlobalHashes(output,j).Contains(SHA(Path.Combine(Root,j.defaultGlobalPath))),"GlobalSettings changed beyond the exact owned profile GUID and original15-RID clear representations.");
            j.defaultGlobalPreparedSHA=SHA(Path.Combine(Root,j.defaultGlobalPath)); // lock the actual prepared disk, not an assumed representation
            j.defaultGlobalPreparedJSON=EditorJsonUtility.ToJson(global);
            File.WriteAllText(Path.Combine(output,"default-profile-prepared.json"),"{\"profileReused\":true,\"officialEnsureCalled\":true,\"profileUnchanged\":true,\"globalPreparedSHA256\":\""+j.defaultGlobalPreparedSHA+"\"}");
        }
        static void RestoreDefaultProfileMemory(Journal j)
        {
            if(!j.defaultProfileCaptured)return;
            var setting=GraphicsSettings.GetRenderPipelineSettings<URPDefaultVolumeProfileSettings>();setting.volumeProfile=null;
            var global=AssetDatabase.LoadMainAssetAtPath(j.defaultGlobalPath);
            Need(setting.volumeProfile==null&&GlobalMemoryRestored(j,global),"Original missing-reference memory state not restored; no global Save.");
            Need(ProfileFingerprint(j.defaultProfilePath)==j.defaultProfileBeforeFingerprint&&SHA(Path.Combine(Root,j.defaultProfilePath))==j.defaultProfileSHA&&SHA(Path.Combine(Root,j.defaultProfilePath+".meta"))==j.defaultProfileMetaSHA,"Preserved generated profile changed; never overwrite/delete it.");
        }
        static void FinishDefaultProfileRestore(Journal j)
        {
            if(!j.defaultProfileCaptured)return;
            var global=AssetDatabase.LoadMainAssetAtPath(j.defaultGlobalPath);var original=j.settings.Single(f=>f.path==j.defaultGlobalPath);
            Need(SHA(Path.Combine(Root,j.defaultGlobalPath))==original.sha256&&GlobalMemoryRestored(j,global),"Owned Global disk/memory restoration proof failed.");
            if(!j.defaultGlobalWasDirty&&EditorUtility.IsDirty(global))EditorUtility.ClearDirty(global);
            Need(EditorUtility.IsDirty(global)==j.defaultGlobalWasDirty,"Owned Global dirty state differs.");
        }
        static string[] PreparedGlobalHashes(string output,Journal j)
        {
            var file=j.settings.Single(f=>f.path==j.defaultGlobalPath);string backup=Path.Combine(output,file.backup);Need(SHA(backup)==file.sha256,"Original Global backup changed.");
            string original=File.ReadAllText(backup),nl=original.Contains("\r\n")?"\r\n":"\n";
            string block="    m_RuntimeSettings:\n      m_List:\n      - rid: 5372388923258962114\n      - rid: 5372388923258962116\n      - rid: 5372388923258962118\n      - rid: 5372388923258962119\n      - rid: 5372388923258962121\n      - rid: 5372388923258962122\n      - rid: 5372388923258962123\n      - rid: 5372388923258962125\n      - rid: 5372388923258962130\n      - rid: 5372388923258962133\n      - rid: 5372389149372317885\n      - rid: 5372389149372317888\n      - rid: 5372389149372317891\n      - rid: 5372389149372317893\n      - rid: 5372389149372317895\n".Replace("\n",nl);
            Need(original.Split(new[]{block},StringSplitOptions.None).Length==2,"Only the exact original fifteen RuntimeSettings RIDs may be cleared.");
            Need(original.Split(new[]{"931df9a20dffe450db70d0db260a7f9c"},StringSplitOptions.None).Length==2,"Original missing Profile GUID is not unique.");
            string bound=original.Replace("931df9a20dffe450db70d0db260a7f9c","cefc1705ccf7a83419f2d7a799335386");
            string cleared=bound.Replace(block,"    m_RuntimeSettings:"+nl+"      m_List: []"+nl);
            return new[]{TextSHA(bound),TextSHA(cleared)};
        }
        static bool GlobalMemoryRestored(Journal j,UnityEngine.Object global)
        {
            string actual=EditorJsonUtility.ToJson(global);if(actual==j.defaultGlobalBeforeJSON)return true;
            // One exact managed-reference entry only. Never normalize unrelated
            // object references or ignore other GlobalSettings fields.
            if(AssetDatabase.GUIDToAssetPath("931df9a20dffe450db70d0db260a7f9c")!=""||GraphicsSettings.GetRenderPipelineSettings<URPDefaultVolumeProfileSettings>().volumeProfile!=null)return false;
            const string missing="{\"rid\":5372388923258962116,\"type\":{\"class\":\"URPDefaultVolumeProfileSettings\",\"ns\":\"UnityEngine.Rendering.Universal\",\"asm\":\"Unity.RenderPipelines.Universal.Runtime\"},\"data\":{\"m_Version\":0,\"m_VolumeProfile\":{\"fileID\":11400000,\"guid\":\"931df9a20dffe450db70d0db260a7f9c\",\"type\":2}}}",explicitNull="{\"rid\":5372388923258962116,\"type\":{\"class\":\"URPDefaultVolumeProfileSettings\",\"ns\":\"UnityEngine.Rendering.Universal\",\"asm\":\"Unity.RenderPipelines.Universal.Runtime\"},\"data\":{\"m_Version\":0,\"m_VolumeProfile\":{\"instanceID\":0}}}";
            if(j.defaultGlobalBeforeJSON.Split(new[]{missing},StringSplitOptions.None).Length!=2)return false;
            return actual==j.defaultGlobalBeforeJSON.Replace(missing,explicitNull);
        }
        [Serializable] sealed class OwnedGlobalObservation
        {public string phase,path,capturedSHA256,actualSHA256,snapshot;public bool diskBytesUnchanged,finiteDiskAllowed,nativeGlobalExact,profileMemoryAndBytesExact;}
        public static void VerifyOwnedGlobalLock(string capturedSHA,string prepared,string phase)
        {
            var j=JsonUtility.FromJson<Journal>(File.ReadAllText(Path.Combine(prepared,"build-prepared.json")));Need(j.project==Root&&j.defaultProfileCaptured&&j.defaultGlobalPath=="Assets/UniversalRenderPipelineGlobalSettings.asset","Exact prepared Global ownership required.");
            string path=Path.Combine(Root,j.defaultGlobalPath);string[] allowed=PreparedGlobalHashes(prepared,j);string stamp=DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffff");
            string snapshot=Path.Combine(prepared,"global-"+phase+"-"+stamp+".bytes");File.Copy(path,snapshot,false);string actual=SHA(snapshot);
            var global=AssetDatabase.LoadMainAssetAtPath(j.defaultGlobalPath);var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(j.defaultProfilePath);var settings=GraphicsSettings.GetRenderPipelineSettings<URPDefaultVolumeProfileSettings>();
            string memory=EditorJsonUtility.ToJson(global);File.WriteAllText(Path.Combine(prepared,"global-memory-"+phase+"-"+stamp+".json"),memory);
            var result=new OwnedGlobalObservation{phase=phase,path=path,capturedSHA256=capturedSHA,actualSHA256=actual,snapshot=snapshot,diskBytesUnchanged=actual==capturedSHA,finiteDiskAllowed=allowed.Contains(actual),nativeGlobalExact=memory==j.defaultGlobalPreparedJSON,profileMemoryAndBytesExact=settings.volumeProfile==profile&&ProfileFingerprint(j.defaultProfilePath)==j.defaultProfileBeforeFingerprint&&SHA(Path.Combine(Root,j.defaultProfilePath))==j.defaultProfileSHA&&SHA(Path.Combine(Root,j.defaultProfilePath+".meta"))==j.defaultProfileMetaSHA};
            File.WriteAllText(Path.Combine(prepared,"global-proof-"+phase+"-"+stamp+".json"),JsonUtility.ToJson(result,true));
            Need(capturedSHA==j.defaultGlobalPreparedSHA&&allowed.Contains(capturedSHA),"Captured Global baseline is not the exact prepared legal state.");
            Need(SHA(path)==actual&&result.finiteDiskAllowed&&result.nativeGlobalExact&&result.profileMemoryAndBytesExact,"Global input differs beyond its exact two disk representations / full prepared native state / preserved profile.");
        }
    }
}
